# SCDC — Lưu và thay đổi tin

Trang này mô tả cách một tin nhắn được lưu: mô hình dữ liệu, cách gửi và chống gửi trùng, cách sửa và xóa, và trạng thái tin phía client. Áp dụng cho DM và tin phòng.

**Trạng thái:** DM trong nền `900e226` đã có: ghi tin, chống trùng bằng HMAC, sequence theo cuộc trò chuyện, sửa, xóa và outbox (một ứng dụng duy nhất). Bằng chứng xem [hồ sơ DM](../../records/verification/messaging/README.md), [DM-MVP](../../records/verification/messaging/DM-MVP.md) và [kiểm tra hợp nhất](../../records/verification/messaging/merge-2026-10-13.md). Tin phòng chưa có: khi làm phải dùng không gian Messaging tương ứng với phòng và [guard Community](channel-messaging.md#channel-admission).

Các ví dụ dưới dùng DM (cặp người dùng, `conversationId`). Nội dung văn bản xem [text-policy](text-policy.md); đồng bộ xem [synchronization](synchronization.md).

Backup, khôi phục và placeholder sau khôi phục để [sau v1](../../project/overview.md#backup-scope). Lưu bền khi khởi động lại, thời hạn lưu, khóa, version, tin đã xóa và chống trùng vẫn thuộc v1.

<a id="contract-4"></a>

<a id="hợp-đồng-4--đối-tượng-dữ-liệu-và-ràng-buộc"></a>

## Mô hình dữ liệu

| Đối tượng | Trường chính | Ràng buộc |
|---|---|---|
| UserSummary | `id, username, displayName` | Không có email, phiên hay trạng thái bảo mật |
| DirectConversation | `id, participantLowId, participantHighId, createdAt, lastSequence, lastActivityAt` | Đúng hai người khác nhau; mỗi cặp chỉ có một cuộc trò chuyện; tạo đồng thời trả cùng một cuộc; DTO trả hai UserSummary trong `participants` |
| Message | `id, conversationId, authorId, clientMessageId, createSequence, version, content, createdAt, editedAt, deletedAt` | Tác giả thuộc cuộc trò chuyện; sequence tăng theo thứ tự commit |
| SendOperation | `conversationId, authorId, clientMessageId, requestFingerprint, messageId` | Duy nhất theo ba trường đầu; ghi cùng transaction với tin; vẫn giữ khi tin bị xóa |
| NotificationOutbox | `eventId, type, conversationId, messageId, version, occurredAt` | Ghi cùng transaction với thay đổi tin; chỉ chứa ID, không chứa nội dung |

- Hai người được sắp theo ID bằng cùng một quy tắc ở mọi nơi ghi, trước khi kiểm tra cặp duy nhất. Endpoint DM không tạo người thứ ba hay nhóm.
- Muốn gửi hai tin giống nhau thì dùng hai `clientMessageId` khác nhau.
- **Sửa tin:** thay nội dung và tăng `version`. Không lưu lịch sử nội dung cũ (DEC-052).
- **Xóa tin:** đặt `content=null`, giữ ID, tác giả, thời điểm và khóa thao tác (gọi là tombstone); tăng `version` ở lần xóa đầu.
- Không ghi nội dung tin vào log, audit hay sự kiện, để không vô tình tạo lịch sử sửa.
- Tin không tự hết hạn (DEC-070). v1 không có tự xóa tài khoản (DEC-103); khóa tài khoản không xóa lịch sử (DEC-104).

Sau v1: backup giữ tối đa 30 ngày (DEC-086, 109); nếu khôi phục làm mất bản sửa mới nhất thì trả placeholder (DEC-108). Nguồn chuẩn: [vòng đời dữ liệu](../../system/data-lifecycle.md#restore).

<a id="contract-7"></a>

<a id="hợp-đồng-7--gửi-thử-lại-và-sửaxóa-đồng-thời"></a>

## Gửi, gửi lại, sửa và xóa

```mermaid
sequenceDiagram
    participant UI as Giao diện
    participant DM as Nhắn tin
    participant ACC as Tài khoản
    participant DB as Kho dữ liệu Nhắn tin
    participant W as Bộ phát cập nhật
    UI->>DM: Gửi content + clientMessageId
    DM->>ACC: Kiểm tra phiên và tài khoản actor
    ACC-->>DM: Cho phép hoặc từ chối
    DM->>DM: Kiểm tra đúng người tham gia
    DM->>DB: Transaction + tra khóa thao tác
    alt Khóa đã tồn tại, cùng nội dung gửi ban đầu
        DB-->>DM: Tin hiện hành hoặc tombstone
    else Khóa mới
        DM->>ACC: Kiểm tra peer đủ điều kiện, giữ guard tới commit
        DM->>DB: Cấp sequence; ghi tin + khóa + outbox
        DM->>DB: Commit
    end
    DM-->>UI: Tin đã lưu
    W->>DB: Đọc sự kiện đã commit và trạng thái tin hiện hành
    W-->>UI: Cập nhật cho hai người có phiên hợp lệ
```

### Gửi và chống trùng

- **Cùng khóa, cùng nội dung:** trả lại tin đã lưu (cùng `messageId`). Nếu tin đã bị sửa hay xóa thì trả trạng thái hiện tại, không hồi sinh nội dung cũ.
- **Cùng khóa, khác nội dung:** `409 OPERATION_CONFLICT`. So với nội dung gửi ban đầu, không so với nội dung đã sửa.
- Ràng buộc unique và transaction đảm bảo hai request cùng khóa không tạo hai tin.
- Sequence được cấp tuần tự trong từng cuộc trò chuyện, nên thứ tự sequence cũng là thứ tự commit.
- **Người kia bị khóa sau khi tin đã lưu:** gửi lại cùng khóa chỉ đọc lại tin cũ (nếu người gửi còn quyền). Khóa mới thì phải kiểm tra người kia; không tạo tin hay outbox mới.
- Khóa chống trùng giữ suốt đời cuộc trò chuyện, kể cả khi tin đã xóa. Không tự hết hạn khóa khi giao diện vẫn cho gửi lại. Khi dọn dữ liệu, giữ lại dấu hiệu chống trùng ([vòng đời dữ liệu](../../system/data-lifecycle.md#cleanup)).

### Gửi lại

- Client **không tự gửi lại** POST khi có mạng trở lại hay khi làm mới token. Người gửi phải bấm "Thử lại" (DEC-021).
- Tải lịch sử và kết nối lại thì được tự động.
- Nếu thư viện HTTP tự gửi lại, phải tắt cho luồng gửi tin.

### Sửa và xóa

- Trước khi sửa hoặc xóa, kiểm tra lại tác giả và quyền truy cập.
- Tin không thuộc cuộc trò chuyện: 404. Không phải tác giả: 403.
- Dùng `expectedVersion`. Sai version: `409 VERSION_CONFLICT`; giao diện tải lại trang đang xem, không ghi đè âm thầm.
- Sửa tin đã xóa: `409 MESSAGE_DELETED`.
- Xóa lặp: vẫn kiểm tra tác giả; trả tombstone hiện tại, không tăng version thêm.
- Sửa thành đúng nội dung hiện tại (đã chuẩn hóa) với version đúng: không làm gì, không gắn "Đã sửa", không tạo outbox.

<a id="uuid-fingerprint-và-khóa-giao-dịch"></a>

## Fingerprint và khóa

### Sắp xếp UUID

Cặp người dùng được sắp bằng cách so từng byte UUID (không dấu) theo thứ tự network. Trong .NET dùng `Guid.ToByteArray(bigEndian:true)` ([Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-10.0)), khớp với cách so của [PostgreSQL 18](https://github.com/postgres/postgres/blob/REL_18_STABLE/src/backend/utils/adt/uuid.c). Không dùng `ToByteArray()` mặc định. Fixture dùng cặp `00000001-0000-4000-8000-000000000000` và `00000100-0000-4000-8000-000000000000` để bắt lỗi endian.

### Fingerprint

- HMAC-SHA256 với khóa ngẫu nhiên riêng, tối thiểu 32 byte. Lưu `fingerprint_version`, `key_id`, `fingerprint` trong SendOperation.
- Dữ liệu đầu vào (dạng byte): chuỗi ASCII `SCDC.Send.v1` + byte 0; UUID không gian, tác giả, client theo thứ tự network; độ dài UTF-8 của nội dung dạng UInt32 big-endian; nội dung UTF-8 đã chuẩn hóa.
- Không dùng JSON (thứ tự trường không ổn định); không đưa thời gian hay token vào. So sánh bằng hàm thời gian không đổi. Không dùng SHA-256 không khóa (nội dung ngắn dễ đoán).
- Khi gửi lại: tra SendOperation trước, lấy đúng khóa và phiên bản đã dùng để tính lại.
- Xoay khóa chỉ áp dụng cho thao tác mới; giữ khóa cũ cho mọi SendOperation còn tồn tại (sau v1: giữ thêm cho cửa sổ backup 30 ngày).
- Thiếu khóa: `503 FINGERPRINT_KEY_UNAVAILABLE`; không tạo tin mới, không trả 409.
- Khóa HMAC tách riêng khỏi khóa cursor và email; không lưu trong DB hay tài liệu.

### Khóa trong transaction (MVP)

Thứ tự khóa: dòng người dùng Identity (theo UUID) → không gian chat → tin và thao tác → outbox.

- [IAccountAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountAccessGuard.cs) (Identity cài đặt): kiểm tra người dùng đang hoạt động, đã xác minh, phiên, security stamp, hạn; giữ khóa chia sẻ trên dòng người dùng trong transaction của bên gọi.
- [IAccountPairAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs) và [ISharedDatabaseSession](../../../services/SCDC.Contracts/Persistence/ISharedDatabaseSession.cs) được DM dùng; [RelationalWorkScope](../../../services/SCDC.BuildingBlocks/Infrastructure/Persistence/RelationalWorkScope.cs) quản lý kết nối và transaction dùng chung. Messaging không JOIN bảng Identity.
- [TextMessageSender](../../../services/Modules/Messaging/Infrastructure/Services/TextMessageSender.cs) chỉ đòi người kia đủ điều kiện khi tạo tin mới. Đọc, gửi lại tin đã lưu, tác giả sửa hoặc xóa thì không cần người kia còn hoạt động.
- Bên gọi giữ guard tới commit và kiểm tra hạn phiên ngay trước commit.

**Luồng gửi:** dưới khóa người gửi và không gian, tra thao tác. Gửi lại đúng thì trả tin hiện tại. Khóa mới thì kiểm tra người kia, tăng counter của không gian, ghi tin, thao tác, outbox và cập nhật danh sách cuộc trò chuyện trong cùng transaction. Rollback thì mọi thứ biến mất; người ghi sau chỉ lấy counter khi người trước xong. Mức cô lập read-committed là đủ khi đã có khóa.

**v1:** không truyền transaction qua mạng, Messaging không đọc DB Identity hay Community. Thay bằng [hàng rào ghi D05](../../system/access-revocation.md#v1-authorization-coordination). Một lời gọi RPC kiểm tra quyền riêng lẻ không đủ giữ quyền tới commit.

<a id="vòng-đời-và-placeholder-bổ-sung"></a>

### Trạng thái nội dung

- `UserSummary.availability` (tùy chọn) dùng khi hiển thị lịch sử; tìm người vẫn chỉ trả người đang hoạt động.
- `Message.contentState`: `available`, `deleted`, hoặc `unavailable_after_restore`.
- `unavailable_after_restore` thuộc sau v1: `content=null`, `deletedAt=null`, version không thấp hơn mốc bảo vệ. Không hiểu nhầm là tác giả đã xóa. Tác giả còn quyền có thể gửi nội dung mới bằng PATCH với version hiện tại.
- Guard và worker outbox phân biệt mục đích: đọc, tác giả sửa, gửi mới. Không dùng "người kia còn hoạt động" làm điều kiện chung.

<a id="mapping-dữ-liệu-và-migration"></a>

## Bảng SQL

| Khái niệm | Trong SQL hiện tại | Ràng buộc |
|---|---|---|
| ID cuộc trò chuyện | `direct_conversations.space_id = spaces.id` | Cặp low/high duy nhất; tạo không gian và cặp cùng transaction; trùng thì đọc lại |
| Sequence | `messages.conversation_sequence bigint` (cột cũ `sequence_no` vẫn còn) | Duy nhất theo `(space_id, conversation_sequence)`, > 0; index `(space_id, conversation_sequence DESC)`; API trả trường `sequence` từ cột mới |
| Counter | `spaces.last_message_sequence` | Mặc định 0; tăng dưới khóa dòng trong transaction; không dùng identity toàn cục hay `MAX+1` |
| Tin, version | `messages.id, author_user_id, content, version, created_at, edited_at, deleted_at` | Version ≥ 1, tăng đúng một lần mỗi thay đổi; không dùng `message_edits` |
| SendOperation | `messaging.send_operations` | Khóa chính `(space_id, author_user_id, client_message_id)`; khóa ngoại tới tin cùng không gian; lưu fingerprint và phiên bản khóa; giữ khi tin bị xóa |
| Tin đã xóa | Ràng buộc cho phép `content` null khi đã xóa | Tin còn sống phải có nội dung. Giới hạn UTF-16 kiểm tra ở code, không bằng `char_length` trong DB |
| Outbox | `integration.outbox_events` (có cột lease) | Chỉ chứa ID, không gian, version; không chứa nội dung |

Migration: [tin văn bản DM](../../../database/postgres/migrations/20261009_dm_p2_text_message.sql) đã điền `conversation_sequence` theo `(sequence_no, id)` trong từng không gian rồi bật index và NOT NULL; [outbox](../../../database/postgres/migrations/20261010_dm_p4_outbox_lease.sql) thêm lease. Bằng chứng cài mới, nâng cấp và chạy lại xem [P2-T01](../../records/verification/messaging/DM-P2-T01.md) và [DM-MVP](../../records/verification/messaging/DM-MVP.md); đây chưa phải kế hoạch chuyển dữ liệu sang database v1.

**Dữ liệu cũ không có fingerprint:** không thể dựng lại fingerprint từ tin đã sửa, và không được bịa. Gửi lại với `clientMessageId` đã có trả `409 OPERATION_UNVERIFIABLE`, không tạo bản trùng; tin vẫn đọc được. Tin gửi qua writer hiện tại đều có fingerprint. Trước khi phát hành phải rà dữ liệu cũ, khóa và sổ migration.

<a id="client-state"></a>

## Trạng thái phía client

**Trạng thái tin:** `draft → sending → sent`, hoặc `sendFailed`.

- Bấm Gửi: tạo UUIDv4, chép nội dung vào tin tạm. Từ lúc này nội dung gắn với khóa không đổi nữa.
- Bấm Thử lại: dùng đúng khóa và nội dung cũ. Muốn gửi nội dung đã sửa thì là thao tác mới, khóa mới.
- Tin tạm, sự kiện và phản hồi HTTP được gộp theo người dùng + `clientMessageId` và `messageId`; giữ version lớn nhất. Sự kiện đến trước phản hồi HTTP vẫn chỉ hiện một dòng.

**Bản nháp:** lưu trong bộ nhớ của tab theo `(userId, conversationId)` (DEC-091).

- Đăng xuất, đổi tài khoản hoặc phiên hết hạn mà không làm mới được: xóa bản nháp và dữ liệu đệm.
- Tải lại trang: mất tin tạm và tin lỗi. Tin đã commit tìm lại được trong lịch sử; tin chưa commit không được coi là đã lưu.
- Hai tab cùng tài khoản không tự đồng bộ bản nháp.

**Gọi API:** hàm trong [api.js](../../../clients/WebClient/src/api.js) mặc định gửi lại sau lỗi 401. Gửi, sửa, xóa DM dùng `retry:false`; tin phòng cũng phải làm vậy. Không để làm mới token, kết nối lại, service worker hay thư viện HTTP tự gửi lại thao tác ghi. Client có thể làm mới token trước lần gửi đầu (`getAccessToken`); nếu request gửi đã đi mà thất bại thì giữ trạng thái lỗi cho người dùng xử lý. GET lịch sử được thử lại sau khi làm mới token; không thử lại thao tác dùng token hay làm mới token khi chưa rõ kết quả.

<a id="v1-message-intake"></a>

## D08 — Nhận tin ở v1

**Quyết định:** v1 ghi tin trực tiếp (phương án A), nhưng giữ khả năng chuyển sang hàng đợi (phương án B) về sau.

**A — Ghi trực tiếp (dùng cho v1):** WebClient gửi REST tới Messaging. Sau khi kiểm tra quyền, writer ghi tin, SendOperation, sequence và outbox trong một transaction của DB Messaging, rồi trả thành công. Worker xử lý outbox sau đó ([đồng bộ](synchronization.md#contract-9)). Outbox chỉ bảo vệ việc phát sự kiện cho tin đã lưu; nó không thay cho request chưa tới được Messaging.

**B — Hàng đợi (để sau):** một thành phần nhận lệnh và lưu bền trước, worker gọi writer sau. Chỉ có ích nếu thành phần nhận vẫn hoạt động khi writer ngừng. Chưa chọn công nghệ hay nơi chạy.

**Để giữ khả năng chuyển sang B:**

- Tách phần nhận request khỏi phần xử lý ghi tin. REST hiện tại và worker sau này dùng chung quy tắc nghiệp vụ.
- Giữ `clientMessageId`, nội dung ban đầu và khóa chống trùng khi gửi lại hay đổi cách truyền.
- Mọi đường ghi kiểm tra quyền tới commit ([D05, D06](../../system/access-revocation.md#v1-authorization-coordination)). Lệnh trong hàng đợi không giữ quyền mãi; mất quyền trước khi ghi thì bị từ chối.
- Writer quyết định sequence trong transaction. Thứ tự trong hàng đợi hay thời điểm client gửi không thay sequence.
- "Đã gửi" vẫn nghĩa là đã commit. Xác nhận của hàng đợi chỉ là "đã nhận lệnh", không phải "đã lưu".

**Khi thật sự chuyển sang B:** chốt hợp đồng nhận lệnh và trả kết quả, giới hạn hàng đợi, xử lý quá tải, các bước chuyển và rollback. Kiểm thử quyền, chống trùng, sequence khi hai đường cùng tồn tại; gửi lại trùng, mất phản hồi, worker khởi động lại, thu hồi quyền giữa chừng. Không phát lại lịch sử như lệnh gửi mới. Tham khảo [mẫu cân bằng tải bằng hàng đợi](https://learn.microsoft.com/en-us/azure/architecture/patterns/queue-based-load-leveling).

D08 độc lập với [D07](../../system/access-revocation.md#v1-revocation-transport). Tự động gửi lại, lưu tin chờ qua lần tải lại trang, trạng thái đã nhận hay đã xem: chưa chọn.

## Kiểm thử

Ca kiểm thử cho lưu bền, rollback, mất phản hồi, gửi lại đồng thời, sửa, xóa và xoay khóa HMAC nằm ở [TC-DM](direct-messaging.md#tests) và [TC-COM](channel-messaging.md#tests); kiểm tra nội dung ở [TC-TEXT](text-policy.md#tests).

Kết quả DM đã có: [gửi lại và đồng thời](../../records/verification/messaging/DM-P3-T02.md), [DM-MVP](../../records/verification/messaging/DM-MVP.md). Tin phòng và v1 cần bằng chứng riêng. Mỗi kết quả ghi commit, build, dữ liệu thử, thao tác, kết quả thực tế và giới hạn.

## Còn mở

- Giao thức hàng rào ghi giữa service và bằng chứng (D05).
- Migration và ràng buộc cho placeholder sau khôi phục, sổ bảo vệ độc lập (sau v1).
- Kế hoạch chuyển dữ liệu thật sang database v1.

---

Nguồn: DEC-021 (gửi lại thủ công), DEC-052 (không lưu lịch sử sửa), DEC-070, DEC-103, DEC-104 (thời hạn lưu), DEC-086, DEC-108, DEC-109, DEC-156 (backup sau v1), DEC-091 (bản nháp), [DEC-125](../../records/decisions/README.md#dec-125) (D08).

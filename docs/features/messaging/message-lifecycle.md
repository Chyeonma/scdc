# Lưu và thay đổi tin

Messaging sở hữu tin, sequence, SendOperation và outbox cho DM và tin phòng. Tài liệu này giữ model, transaction, retry, sửa/xóa và trạng thái client. [Nội dung văn bản](text-policy.md) và [đồng bộ](synchronization.md) là các cơ chế liên quan.

## Trạng thái và phạm vi

Đây là thiết kế mục tiêu, chưa có writer tin hoặc bằng chứng gửi/sửa/xóa theo hợp đồng này. Các ví dụ cặp user/recipient và `conversationId` minh họa DM; tin phòng dùng Messaging space tương ứng channel và [guard Community](channel-messaging.md#channel-admission). Không suy schema/fixture hiện có thành hành vi runtime.

<a id="contract-4"></a>

<a id="hợp-đồng-4--đối-tượng-dữ-liệu-và-ràng-buộc"></a>

## Đối tượng dữ liệu và ràng buộc

| Đối tượng | Trường chính đề xuất | Ràng buộc |
|---|---|---|
| UserSummary | `id, username, displayName` | Không trả email, phiên hoặc trạng thái bảo mật |
| DirectConversation | `id, participantLowId, participantHighId, createdAt, lastSequence, lastActivityAt` | Đúng hai người khác nhau; cặp đã chuẩn hóa duy nhất; tạo đồng thời trả cùng hội thoại; DTO trả hai UserSummary trong `participants` |
| Message | `id, conversationId, authorId, clientMessageId, createSequence, version, content, createdAt, editedAt, deletedAt` | Tác giả thuộc hội thoại; sequence tạo tin tăng theo thứ tự commit trong hội thoại |
| SendOperation | `conversationId, authorId, clientMessageId, requestFingerprint, messageId` | Duy nhất theo ba trường đầu; cùng transaction với tin; còn tồn tại sau khi tin bị xóa |
| NotificationOutbox | `eventId, type, conversationId, messageId, version, occurredAt` | Cùng transaction với thay đổi tin; chỉ tham chiếu, không lưu bản nội dung cũ |

Hai người được sắp thứ tự ID bằng một quy tắc nhất quán ở mọi writer
trước khi áp unique pair. Không tạo membership thứ ba hoặc hội thoại
nhóm bằng endpoint DM. Chủ ý gửi hai tin cùng nội dung dùng hai
`clientMessageId` khác nhau và tạo hai tin.

Sửa tin thay nội dung hiện hành và tăng `version`; không tạo bảng lịch
sử nội dung cũ (DEC-052). Xóa tin đặt `content=null`, giữ tombstone, ID,
tác giả, thời điểm và khóa thao tác; tăng `version` khi xóa lần đầu. Không lưu nội dung
tin trong log, audit hoặc payload sự kiện để vô tình tạo lịch sử sửa.
Tin không tự hết hạn DEC-070; chưa self-delete account DEC-103, khóa không xóa lịch sử DEC-104. Backup/WAL tối đa tuổi 30 ngày DEC-086/109; restore mất bản sửa mới nhất trả placeholder DEC-108. [Vòng đời dữ liệu](../../system/data-lifecycle.md#restore) là nguồn chuẩn; chỉ bản mới nhất không phải cam kết xóa ngay mọi backup hoặc dữ liệu người nhận tự sao chép.

<a id="contract-7"></a>

<a id="hợp-đồng-7--gửi-thử-lại-và-sửaxóa-đồng-thời"></a>

## Gửi, thử lại và sửa/xóa đồng thời

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

- Nếu cùng khóa nhưng payload khác, trả `409 OPERATION_CONFLICT`.
  Không lấy nội dung đã sửa để so với nội dung gửi ban đầu.
- Unique constraint và transaction ngăn hai request cùng khóa tạo hai
  tin. Mọi writer phải tuần tự hóa cấp sequence theo từng hội thoại để
  thứ tự sequence cũng là thứ tự commit của tin mới.
- Response mất sau commit: lần thử lại trả cùng `messageId`. Tin đã
  bị sửa/xóa: trả trạng thái hiện hành, không hồi sinh nội dung cũ.
- Peer bị khóa sau commit: retry cùng khóa/nội dung chỉ đọc lại tin đã lưu
  nếu actor còn quyền. Khóa chưa tồn tại phải kiểm tra peer hiện hành;
  không tạo tin mới hoặc outbox mới khi peer bị khóa.
- Giữ khóa chống trùng suốt vòng đời dữ liệu hội thoại, kể cả tombstone.
  Không tự hết hạn khóa sau vài giờ trong khi UI còn cho thử lại. Việc
  thanh lọc toàn hội thoại chưa được chọn; [vòng đời dữ liệu](../../system/data-lifecycle.md#cleanup) giữ marker khi payload được dọn, không purge hội thoại/tin live.
- Trước khi sửa/xóa, kiểm tra lại tác giả và quyền truy cập. Sai version
  trả xung đột; giao diện tải bản hiện hành, không ghi đè âm thầm. Sửa
  tin đã xóa trả `409 MESSAGE_DELETED`; xóa lặp vẫn phải kiểm tra tác giả.
- Client không tự gửi lại POST khi có mạng hoặc khi làm mới phiên.
  Tải lịch sử/kết nối lại được tự thực hiện; thử lại một tin lỗi do người
  gửi bấm, theo DEC-021. Nếu thư viện HTTP tự retry mutation phải tắt
  hành vi đó cho luồng gửi tin.

<a id="uuid-fingerprint-và-khóa-giao-dịch"></a>

### UUID, fingerprint và khóa giao dịch

Chuẩn hóa cặp user theo so sánh unsigned từng byte UUID ở thứ tự RFC/network. Trong .NET dùng `Guid.ToByteArray(bigEndian:true)` để lấy 16 byte theo [API Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.guid.tobytearray?view=net-10.0); phù hợp comparator byte trong [PostgreSQL 18 uuid.c](https://github.com/postgres/postgres/blob/REL_18_STABLE/src/backend/utils/adt/uuid.c). Không so `ToByteArray()` mặc định. Fixture dùng cặp `00000001-0000-4000-8000-000000000000` và `00000100-0000-4000-8000-000000000000` để bắt lỗi endian.

Fingerprint v1 dùng HMAC-SHA256, key ngẫu nhiên riêng tối thiểu 32 byte; lưu `fingerprint_version,key_id,fingerprint` trong SendOperation. Input binary gồm ASCII domain `SCDC.Send.v1` + byte 0, UUID space/author/client theo network order, độ dài UTF-8 content dạng UInt32 big-endian, rồi UTF-8 content đã chuẩn hóa. Không serialize JSON tùy thứ tự field, không đưa thời gian hoặc access token vào fingerprint. So sánh hash constant-time; không dùng SHA-256 không khóa để lưu dấu vết nội dung ngắn dễ đoán.

Retry tra SendOperation trước, lấy đúng key/version từng dùng để tính lại fingerprint; không so với nội dung message hiện hành sau sửa/xóa. Rotation chỉ đổi key cho thao tác mới; giữ key cũ cho mọi SendOperation còn tồn tại và backup 30 ngày liên quan. Key thiếu/không đọc được trả 503 `FINGERPRINT_KEY_UNAVAILABLE`, không tạo tin mới hoặc giả 409 payload conflict. HMAC key tách khỏi key ring cursor/email; không lưu secret trong DB/docs.

Thứ tự khóa thống nhất: Identity user rows theo UUID → chat space → message/operation → outbox. [IAccountAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountAccessGuard.cs) đã có implementation Identity: kiểm tra actor active/verified, session/stamp/expiry và giữ share lock trên user trong transaction của caller, tương thích với `LockUserAsync` dùng NO KEY UPDATE. [RelationalWorkScope](../../../services/SCDC.BuildingBlocks/Infrastructure/Persistence/RelationalWorkScope.cs) đã quản lý shared connection/transaction trong BuildingBlocks; Messaging không JOIN bảng Identity.

Writer DM chưa tích hợp các thành phần này. Hợp đồng kiểm tra/giữ trạng thái recipient cho nhánh gửi mới còn cần bổ sung, cùng cách khóa actor/recipient theo thứ tự UUID. Read/retry đã commit và author edit/delete không đòi peer active; nhánh tạo tin mới kiểm tra recipient trong cùng transaction. Caller phải giữ guard tới commit và kiểm tra hạn phiên ngay trước commit; proof race thu hồi/gửi của DM vẫn chưa có.

Gửi: dưới guard actor/space lock, tra operation; retry đúng trả tin hiện hành/tombstone/placeholder sau restore, kể cả peer đã bị khóa, nếu actor còn quyền đọc. Khóa mới: kiểm tra peer đủ điều kiện dưới guard đã giữ, tăng counter của space, insert message/operation/outbox và cập nhật projection trong cùng transaction. Khi rollback mọi thay đổi biến mất; writer sau chỉ cấp counter khi writer trước commit/rollback. Read-committed đủ cho writer đã có khóa; snapshot phân trang là mốc sequence, không phải lịch sử nội dung cũ.

Sửa/xóa dùng compare-and-update version trong transaction. Kiểm tra membership/tác giả trước cả delete lặp; message không thuộc space trả 404, tác giả khác 403. Deleted rồi PATCH trả 409 `MESSAGE_DELETED`; DELETE lặp trả tombstone hiện hành sau auth, không tăng version lần nữa. PATCH cùng nội dung chuẩn hóa hiện hành là no-op nếu expectedVersion đúng; không tạo dấu “Đã sửa” hoặc outbox mới chỉ vì click lại. Sai expectedVersion trả 409 `VERSION_CONFLICT`, UI tải lại trang đang hiển thị; không thêm HTTP thứ tám chỉ để đọc một message.

<a id="vòng-đời-và-placeholder-bổ-sung"></a>

### Vòng đời và placeholder bổ sung

`UserSummary.availability` tùy chọn phục vụ projection lịch sử; search vẫn chỉ user active đủ điều kiện. `Message.contentState` có available/deleted/unavailable_after_restore: nhánh unavailable bắt buộc contentState, content=null/deletedAt=null, version không dưới protection floor; không biến thành tác giả đã xóa. History/realtime dùng cùng schema, actor-author còn quyền được PATCH body mới bằng expectedVersion hiện hành. Migration restore_redacted_at/constraint/key ring/sổ bảo vệ theo [thiết kế chung](../../system/data-lifecycle.md#restore) chưa triển khai. Guard và outbox dispatcher phải dùng purpose read/author-mutation/send; không lấy peer active làm điều kiện chung cho mọi thao tác.

<a id="mapping-dữ-liệu-và-migration"></a>

### Mapping dữ liệu và migration

| Mục tiêu logic | Mapping SQL đề xuất | Ràng buộc/đầu việc |
|---|---|---|
| Conversation ID | `direct_conversations.space_id = spaces.id` | Unique low/high pair hiện có; tạo space/cặp cùng transaction, xử lý unique conflict bằng đọc lại |
| Sequence tạo tin | Thêm `messages.conversation_sequence bigint`; giữ `sequence_no` legacy/global nếu cần | Unique `(space_id,conversation_sequence)`, >0; history index `(space_id,conversation_sequence DESC)`; wire `sequence` lấy trường mới |
| Counter | `spaces.last_message_sequence` | Default 0, dưới row lock cấp next cùng transaction; không dùng global identity hoặc `MAX+1` ngoài khóa |
| Tin/version | `messages.id,author_user_id,content,version,created_at,edited_at,deleted_at` | DTO `createSequence` logic → wire `sequence`; version tăng đúng một lần, tối thiểu 1; không ghi message_edits |
| SendOperation | Thêm `messaging.send_operations` | PK `(space_id,author_user_id,client_message_id)`, FK cùng space/message, fingerprint/key version; giữ khi tombstone |
| Deleted text | Sửa constraint text hiện tại | Text live content khác null; deleted content null; UTF-16 ở validator, constraint DB không thay bằng char_length 2.000 |
| Outbox | `integration.outbox_events` | Payload chỉ ID/space/version; thêm lease fields hoặc bảng dispatch lease, không có nội dung tin |

Backfill `conversation_sequence` theo `(sequence_no,id)` trong từng space, cập nhật counter từ max đã backfill rồi bật index/NOT NULL; seed chưa có dữ liệu thật không chứng minh migration live an toàn. Không thể khôi phục đúng nội dung gửi ban đầu cho fingerprint từ message đã sửa; không bịa fingerprint từ bản hiện hành. Legacy không có fingerprint trả 409 `OPERATION_UNVERIFIABLE` khi retry cùng client ID đã có, không tạo bản trùng; dữ liệu vẫn đọc được theo quyền. Backend DM chưa tồn tại nên hiện chưa có thao tác gửi thật qua contract mới; nếu import/cutover dữ liệu thực thì phải rà soát tác động legacy retry trước phát hành. Không sửa `schema.sql` hay xóa dữ liệu trong lần viết docs này. Dữ liệu mới qua writer mới có fingerprint ngay từ lần gửi đầu.

<a id="client-state"></a>

## Trạng thái gửi và bản nháp phía client

State UI: `draft → sending → sent` hoặc `sendFailed`. Bấm Gửi tạo UUIDv4, copy nội dung vào tin tạm; từ thời điểm đó không đổi nội dung gắn với khóa gửi. Bấm Thử lại dùng đúng khóa/nội dung; muốn gửi bản đã đổi là thao tác mới với khóa mới. Tin tạm/sự kiện/response merge theo actor + clientMessageId và message ID; giữ version lớn nhất. Event đến trước HTTP response vẫn chỉ một dòng.

Bản nháp Map `(userId,conversationId)` chỉ ở bộ nhớ tab theo DEC-091; khi logout/đổi account/hết phiên không khôi phục được thì xóa Map và cache tin. Reload bỏ cả tin tạm/lỗi phía tab; tin đã commit được tìm lại trong lịch sử, tin chưa commit không được coi là đã lưu. Không coi hai tab cùng tài khoản là tự đồng bộ bản nháp.

Wrapper [api.js](../../../clients/WebClient/src/api.js) hiện mặc định retry request sau 401. Tích hợp gửi/sửa/xóa DM phải dùng `retry:false`; không để refresh/reconnect/service worker/HTTP library tự replay mutation. Client có thể refresh trước lần gửi đầu bằng `getAccessToken`; khi request gửi đã xảy ra mà thất bại, giữ trạng thái lỗi để người dùng chủ động xử lý. GET lịch sử có thể retry sau refresh; không retry token consume hoặc refresh đã có kết quả không rõ.

## Kiểm chứng

Các ca lưu bền, rollback, lost response, retry đồng thời, sửa/xóa và HMAC rotation được định nghĩa tại [TC-DM](direct-messaging.md#tests) và [TC-COM](channel-messaging.md#tests); validation dùng [TC-TEXT](text-policy.md#tests). Kết quả cần ghi commit/build, dataset, thao tác, actual result và giới hạn. Chưa có hồ sơ nghiệm thu Messaging riêng.

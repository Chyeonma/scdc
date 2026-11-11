# SCDC — Lịch sử và đồng bộ tin

Trang này mô tả cách client tải lịch sử tin, nhận tin mới qua realtime, bù dữ liệu khi kết nối lại, và cách server ngừng gửi tin khi người dùng mất quyền. Áp dụng chung cho DM và tin phòng.

**Trạng thái:** DM trong nền `900e226` đã có đủ: lịch sử có cursor, Hub, worker outbox, tự kết nối lại và bù nhiều trang, tải lại các trang tin cũ. Bằng chứng xem [DM-MVP](../../records/verification/messaging/DM-MVP.md) và [kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md). Đây là bằng chứng trên một ứng dụng duy nhất; tin phòng và cơ chế giữa các service v1 chưa có.

Quyền DM xem [nhắn tin riêng](direct-messaging.md#permissions). Tin phòng kiểm tra thêm phòng và epoch thành viên, xem [tin phòng](channel-messaging.md#channel-admission).

<a id="contract-8"></a>

<a id="hợp-đồng-8--lịch-sử-và-kết-nối-thời-gian-thực"></a>

## Lịch sử tin

### Quy ước dữ liệu

- `sequence` và `version` gửi dạng chuỗi số nguyên. Client so sánh bằng BigInt, không đổi sang Number (có thể mất chính xác), không so theo thứ tự chữ.
- ID do server tạo dùng UUIDv7; `clientMessageId` do client tạo dùng UUIDv4.

### Tải trang

- Mỗi trang mặc định 50 tin, tối đa 100.
- Lần mở đầu lấy trang tin mới nhất.
- `before`: lấy tối đa `limit` tin ngay trước con trỏ, rồi sắp tăng dần theo `sequence` để hiển thị.
- Không dùng `before` và `after` cùng lúc.

Phản hồi:

```text
{ items, nextCursor, hasMore, throughSequence, resumeCursor }
```

### Bù tin mới

Khi bắt đầu bù, server chốt `throughSequence` là sequence cao nhất đã commit. Các trang tiếp theo giữ nguyên mốc này và lấy `after < sequence <= throughSequence`.

- Client chỉ tiến cursor sau khi đã gộp xong cả trang.
- Sequence cao nhất nhận qua realtime không chứng minh đã đọc đủ lịch sử.
- Không suy số tin chưa đọc từ hiệu hai sequence. Trạng thái đã đọc chưa thuộc đợt này.

<a id="phân-trang-resume-và-danh-sách-hội-thoại"></a>

### Cursor

Cursor `before` và `after` được mã hóa và ký, chứa: người dùng, tài nguyên, hướng đọc, bộ lọc, vị trí, mốc chốt, giới hạn và hạn dùng (24 giờ).

- Mỗi trang vẫn kiểm tra phiên và quyền hiện tại; cursor không thay xác thực.
- Dùng cursor của người dùng, tài nguyên hoặc truy vấn khác trả `400 CURSOR_INVALID`.
- Khóa ký dùng ASP.NET Data Protection: `ApplicationName` cố định theo môi trường; purpose riêng `Messaging.History.v1`, `Messaging.UserSearch.v1`, `Messaging.Conversations.v1`. Giữ key ring qua các lần deploy.

Ba cách đọc:

| Cách | Request | Xử lý |
|---|---|---|
| Mở đầu | Không `before`, không `after` | Gọi H là counter đã commit. Lấy các tin mới nhất ≤ H, sắp tăng. Trả `nextCursor` để đọc cũ hơn và `resumeCursor` giữ mốc H |
| Đọc cũ hơn | `before` | Giữ H trong cursor. Lấy `sequence < vị trí` và ≤ H, lấy `limit+1` rồi trả `limit` tin gần nhất. Tin đã sửa hoặc xóa trả trạng thái hiện tại |
| Bù tin mới | `after` = `resumeCursor` trước đó | Request đầu chốt H mới. `nextCursor` (mode `after`) giữ cùng H, không chốt lại mỗi trang; lấy `vị trí < sequence ≤ H` |

Chi tiết:

- `after=0` là giá trị khởi đầu được phép; client không được tự đưa sequence bất kỳ.
- Tham số `through` (nếu có) chỉ để kiểm tra khớp với H trong cursor; server không nhận mốc do client tự chọn. Không dùng `through` với `before` hoặc khi không có cursor.
- `resumeCursor` chỉ khác null ở trang đầu tiên khi mở, hoặc ở trang cuối khi bù. Client chỉ lưu nó sau khi gộp thành công. Khi `hasMore` thì dùng `nextCursor`, không nhảy thẳng tới H.
- Cursor hết hạn: tải lại lịch sử và các trang đang xem, không gửi lại tin.

## Realtime

### Hub và sự kiện

Hub `/hubs/chat` chỉ có hai lệnh:

- `SubscribeConversation({conversationId})`: trả xác nhận sau khi kết nối đã được gắn với người dùng, phiên và cuộc trò chuyện. Gọi lặp không tạo thêm kết nối hay nhận trùng.
- `UnsubscribeConversation({conversationId})`.

Hai sự kiện:

- `MessageChanged {eventId, conversationId, message}`: chứa trạng thái hiện tại của tin và `version`. Client gộp theo `messageId`, chỉ áp bản có version mới hơn. `eventId` không dùng làm cursor lịch sử.
- `AccessRevoked {scope, conversationId?, reason}`: chỉ để giao diện cập nhật, không chứa nội dung tin. Server vẫn phải tự ngừng gửi, không dựa vào sự kiện này.

Worker có thể gửi lặp hoặc gộp nhiều lần sửa thành một (đọc bản mới nhất). Sự kiện không cần chứa các bản nội dung cũ. "Đã gửi" chỉ nghĩa là tin đã lưu, không nghĩa là người nhận đã nhận được.

Lỗi khi đăng ký trả HubException kèm ProblemDetails dạng JSON, không có stack trace. Đăng ký thất bại thì không tải lịch sử của cuộc trò chuyện đó. Client dựa vào `errorCode`, không đoán từ câu thông báo tiếng Anh.

Schema máy đọc: [chat-realtime.schema.json](../../contracts/chat-realtime.schema.json). Đây là schema của thông điệp ứng dụng, không mô tả giao thức SignalR.

### Mở và kết nối lại

1. Đăng ký handler và bật bộ đệm sự kiện **trước** khi gọi đăng ký cuộc trò chuyện.
2. Server kiểm tra phiên và quyền tham gia trước khi cho nhận cập nhật.
3. Bù hết các trang tin mới tới mốc đã chốt; gộp theo ID và version.
4. Tải lại các trang lịch sử đang hiển thị để thấy tin cũ bị sửa hoặc xóa. Các trang cũ trong bộ nhớ đệm nhưng chưa hiển thị thì bỏ, tải lại khi người dùng cuộn tới.
5. Gộp các sự kiện đã đệm và tiếp tục nhận. Tin gửi lỗi vẫn chờ người dùng bấm gửi lại.

Bước 4 bắt buộc: cursor tin mới không phát hiện được việc sửa hoặc xóa tin cũ. Bộ nhớ đệm lịch sử không phải nguồn chính xác. Dữ liệu đệm tách theo tài khoản và bị xóa khi đăng xuất hoặc đổi tài khoản. Nội dung và bản nháp chỉ giữ trong bộ nhớ của tab (DEC-091).

<a id="contract-9"></a>

<a id="hợp-đồng-9--lỗi-thu-hồi-phiên-và-quan-sát"></a>

## Outbox, thu hồi quyền và log

### Outbox

- Sự kiện chỉ được phát sau khi commit.
- Worker lỗi thì thử lại; nhận lặp không tạo thêm tin.
- Worker lấy sự kiện outbox bằng lease, đọc tin hiện tại qua Messaging, đọc thông tin người dùng qua Identity, và chỉ gửi tới các kết nối đã đủ quyền.

### Thu hồi quyền

Danh sách kết nối (registry) gắn `connectionId`, `userId`, `sessionId` và `spaceId`.

- Guard kiểm tra quyền trước khi gửi nội dung; đối soát lại mỗi tối đa 1 giây.
- Khi thu hồi được commit, kết nối bị bỏ khỏi định tuyến và đóng. Không chỉ gửi yêu cầu cho giao diện tự đăng xuất.
- Nếu không kiểm tra được quyền (backend lỗi) thì dừng gửi. HTTP lịch sử vẫn kiểm tra quyền; outbox còn đó để thử lại sau.
- Thời hạn: tối đa 5 giây từ lúc commit (DEC-083).

### Log

Ghi tối thiểu: request ID, mã lỗi, thời lượng, ID thao tác hoặc ID tin khi phù hợp. Không ghi mật khẩu, token, link email hay nội dung chat.

<a id="signalr-và-ranh-giới-module"></a>

## Ranh giới module

- Messaging không đọc bảng của Identity. Messaging dùng các hợp đồng do Identity cung cấp: [IAccountPairAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs) (kiểm tra cặp người dùng), [IUserSearchDirectory](../../../services/SCDC.Contracts/Identity/IUserSearchDirectory.cs), [IAuthenticatedSessionReader](../../../services/SCDC.Contracts/Identity/IAuthenticatedSessionReader.cs) và bộ đọc thông tin người dùng cũ. Trong MVP, các hợp đồng này cùng [ISharedDatabaseSession](../../../services/SCDC.Contracts/Persistence/ISharedDatabaseSession.cs) đã được dùng trong ghi tin, lịch sử và [ConversationAccess](../../../services/Modules/Messaging/Realtime/ConversationAccess.cs).
- `IUserDirectory` chỉ trả người dùng đang hoạt động, không chứng minh đã xác minh hay phiên còn hiệu lực; DM dùng guard và bộ đọc phiên riêng.
- Identity giữ dữ liệu bảo mật; Messaging giữ registry và Hub chat. Trong MVP, [ChatHub và các worker](../../../services/Modules/Messaging/MessagingModule.cs) đăng ký trong ứng dụng API chung.
- v1: Media control có Hub riêng ([D11](../media/participation-lifecycle.md#v1-media-realtime)). Hub và worker chạy trong API của service sở hữu, mỗi service một bản ([D12, D13](../../system/backend.md#v1-realtime-hosting), [D21](../../system/environments.md#v1-service-replicas)). Không thêm backplane; mở rộng nhiều bản cần quyết định riêng.

### Xác thực SignalR

SignalR giữ danh tính của lúc kết nối và không tự biết khi phiên hay quyền bị thu hồi giữa chừng. Vì vậy server phải kiểm tra dữ liệu hiện tại và đóng kết nối. `CloseOnAuthenticationExpiration` chỉ xử lý token hết hạn, không thay cho thu hồi. Tham khảo [tài liệu Microsoft](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0).

- JavaScript dùng `accessTokenFactory`.
- Token trong query của WebSocket chỉ được nhận ở đúng route Hub, và bị loại khỏi log của proxy và API.
- Định tuyến theo người dùng dùng claim `sub` (cố định), không dùng tên hiển thị.

Trong MVP, Hub, worker và [SubscriptionReconciler](../../../services/Modules/Messaging/Realtime/SubscriptionReconciler.cs) dùng `IAuthenticatedSessionReader` để kiểm tra phiên, bỏ định tuyến và đóng kết nối khi mất quyền hoặc không xác nhận được. `IRealtimeAccessRevoker` hiện chỉ nhận người dùng và không gian, chưa phải hợp đồng thu hồi theo phiên.

## Kiểm thử

Kịch bản cần đo: commit rồi mất phản hồi, hai request đồng thời, worker dừng, sự kiện trùng hoặc đảo thứ tự, kết nối lại, và người thứ ba truy cập.

DM đã có bằng chứng cho outbox và worker, sự kiện trùng và đảo thứ tự, kết nối lại nhiều trang, sửa và xóa tin cũ, thu hồi quyền ([DM-MVP](../../records/verification/messaging/DM-MVP.md)). Kết quả chỉ đúng với bản build trong hồ sơ.

Khi chuyển sang v1:

- Chạy lại [TC-DM-15 đến 17, 25, 26](direct-messaging.md#tests).
- Chạy [UC-COM-25 và TC-COM-25](channel-messaging.md#uc-com-25) cho tin phòng.
- Đo thu hồi trên kết nối đang mở, từ commit đến lúc ngừng truy cập (≤ 5 giây), kể cả khi thành phần kiểm tra quyền bị lỗi. Kiểm tra HTTP hay fixture riêng chưa đủ.
- Ghi cấu hình, key ring, dữ liệu thử, commit và phép đo vào hồ sơ.

## Còn mở

- Thiết kế phối hợp quyền và thu hồi giữa các service: [thu hồi truy cập v1](../../system/access-revocation.md#v1-authorization-coordination).
- Thông báo thu hồi sau commit cho tin phòng và giữa các service cần hợp đồng riêng; Messaging không được đọc DB Identity để thay hợp đồng.
- Thử lại các ca rollback, commit đồng thời, mất phản hồi, worker dừng, thu hồi và kết nối lại trên nền v1.
- Cơ chế thử lại của worker, cảnh báo hàng đợi tồn và việc đạt ngưỡng vẫn phải thử nghiệm.
- Chưa có kết luận đạt về tải, chất lượng hay thu hồi giữa các service.

---

Nguồn: DEC-081 (REST, SignalR, UUID, cursor), DEC-083 (thu hồi ≤ 5 giây), DEC-091 (bản nháp), DEC-116 (tách service).

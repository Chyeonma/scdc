# Lịch sử và đồng bộ tin

History/cursor, outbox, Hub, reconnect và thu hồi của Messaging dùng chung cho DM và tin phòng. Quyền DM nằm trong [nhắn tin riêng](direct-messaging.md#permissions); tin phòng bổ sung channel/membership epoch theo [tin phòng](channel-messaging.md#channel-admission).

## Trạng thái hiện tại

REST/SignalR, UUID, cursor bảo vệ và deadline chat ≤5 giây đã chốt DEC-081/083. Các schema/thuật toán dưới đây là thiết kế mục tiêu; Hub/dispatcher và proof reconnect/thu hồi chưa được ghi nhận hoàn tất. Rà soát lại các giả định transaction xuyên module khi chuyển microservice ở v1 theo DEC-116.

<a id="contract-8"></a>

<a id="hợp-đồng-8--lịch-sử-và-kết-nối-thời-gian-thực"></a>

## Lịch sử và kết nối thời gian thực

`sequence` và `version` truyền dưới dạng chuỗi số nguyên; client so sánh bằng BigInt, không chuyển sang Number có thể mất độ chính xác. ID server dùng UUIDv7, `clientMessageId` dùng UUIDv4 theo DEC-081.

Trang lịch sử đề xuất mặc định 50, tối đa 100 tin. Lần mở đầu lấy
trang tin mới nhất; `before` lấy tối đa `limit` tin gần nhất nằm trước
con trỏ, sau đó sắp tăng theo `sequence` để hiển thị; không được kết hợp
`before` và `after` trong cùng request. Response:

```text
{ items, nextCursor, hasMore, throughSequence, resumeCursor }
```

Lần bắt đầu bù tin, server chốt `throughSequence` là sequence tin mới
cao nhất đã commit. Các trang tiếp theo giữ cùng mốc và lấy
`after < sequence <= throughSequence`. Cursor chỉ tiến sau khi client
đã merge thành công cả trang; không lấy sequence cao nhất nhận qua
realtime làm bằng chứng đã đọc đủ lịch sử. So sánh version/sequence
theo giá trị số nguyên chính xác, không so theo thứ tự từ điển. Không suy số tin chưa đọc
từ hiệu hai sequence; trạng thái đã đọc chưa thuộc đợt này.

Hợp đồng nhận cập nhật đề xuất: đăng ký hội thoại đã có quyền; thông báo
`MessageChanged` chứa DTO trạng thái hiện hành và version. Client merge
theo `messageId`, chỉ áp dụng version mới hơn. Dispatcher có thể gửi
lặp hoặc gộp nhiều sửa đổi bằng cách đọc bản mới nhất; không cần lưu
các bản nội dung cũ trong sự kiện. “Đã gửi” chỉ có nghĩa đã lưu tin,
không chứng minh người nhận đã nhận thông báo.

Quy trình mở/reconnect:

1. Đăng ký handler và bật bộ đệm sự kiện trước khi đăng ký hội thoại.
2. Server kiểm tra phiên và người tham gia trước khi cho nhận cập nhật.
3. Tải/bù hết các trang tin mới tới mốc đã chốt, merge tin theo ID/version.
4. Tải lại các trang lịch sử đang hiển thị để nhận sửa/xóa tin cũ; bỏ
   hiệu lực cache trang cũ chưa hiển thị, tải lại khi người dùng cuộn tới.
5. Merge sự kiện đã đệm và tiếp tục nhận cập nhật. Tin lỗi vẫn chờ bấm thử lại.

Cursor tin mới không phát hiện được việc sửa/xóa một tin cũ. Vì vậy không
được bỏ bước 4 hoặc coi cache lịch sử offline là nguồn chính xác. Dữ liệu
đã cache phải tách theo tài khoản và được xóa khỏi phiên giao diện khi
đăng xuất/đổi tài khoản; nội dung/bản nháp chỉ ở bộ nhớ tab theo DEC-091.

<a id="contract-9"></a>

<a id="hợp-đồng-9--lỗi-thu-hồi-phiên-và-quan-sát"></a>

## Lỗi, thu hồi phiên và quan sát

Outbox chỉ được phát sau commit. Worker lỗi được thử lại; handler nhận
lặp không tạo thêm tin. Phát cập nhật cần kiểm tra phiên/người nhận còn
hợp lệ; đăng xuất hoặc thu hồi phiên phải ngừng định tuyến kết nối cũ,
không chỉ gửi yêu cầu tự đăng xuất cho giao diện. Thời hạn thu hồi đã chốt ≤5 giây (DEC-083), transport SignalR (DEC-081); cơ chế retry worker, cảnh báo backlog và đáp ứng ngưỡng vẫn phải thử nghiệm.

Log tối thiểu: request ID, mã lỗi, thời lượng, ID thao tác/tin khi phù
hợp; không ghi mật khẩu, token, email link hoặc nội dung chat. Đo các
kịch bản commit rồi mất response, hai request đồng thời, worker dừng,
sự kiện trùng/đảo thứ tự, reconnect và tài khoản thứ ba truy cập.

<a id="phân-trang-resume-và-danh-sách-hội-thoại"></a>

### Phân trang và resume

`before/after` là cursor có mã hóa/xác thực, actor/resource/direction/filter/position/frontier/limit/expiry; hạn kỹ thuật 24 giờ. ApplicationName cố định theo môi trường qua deploy; purpose Data Protection tách `Messaging.History.v1`, `Messaging.UserSearch.v1`, `Messaging.Conversations.v1`. Cursor không thay auth: mỗi trang kiểm tra phiên/quyền hiện hành. Giữ key ring qua deploy; dùng cursor của user/resource/query khác nhận 400 `CURSOR_INVALID`.

- Mở đầu không before/after: lấy latest ≤H với H là counter đã commit; items tăng sequence, nextCursor để đọc cũ hơn, resumeCursor bảo vệ mốc H.
- Đọc cũ bằng before: giữ H của cursor, query sequence <position và ≤H, lấy limit+1 rồi trả limit tin gần nhất tăng sequence; sửa/xóa được đọc ở trạng thái hiện hành.
- Bù mới: dùng resumeCursor có mode `resume` trước đó làm `after`; request đầu chốt H mới. nextCursor có mode `after` tiếp tục cùng frontier H, không chốt lại mỗi trang. `after=0` là sentinel bootstrap được phép, không cho client tự đưa sequence bất kỳ. Các nextCursor giữ cùng H/position; query position <sequence ≤H. `through` nếu có chỉ dùng kiểm tra bằng H trong protected cursor, không nhận frontier do client tự chọn; không đi với before/không cursor.
- Response có `resumeCursor`: chỉ khác null ở trang latest đầu hoặc trang cuối bù mới. Client chỉ lưu sau merge thành công; khi hasMore thì dùng nextCursor, không nhảy thẳng lên H. Cursor hết hạn tải lại lịch sử và các trang đang xem, không gửi lại tin.

<a id="signalr-và-ranh-giới-module"></a>

### SignalR và ranh giới module

Schema máy đọc cho Hub nằm trong [chat-realtime.schema.json](../../contracts/chat-realtime.schema.json), là JSON Schema thông điệp ứng dụng, không giả OpenAPI mô tả protocol transport SignalR. Hub `/hubs/chat` chỉ có `SubscribeConversation({conversationId})` và `UnsubscribeConversation({conversationId})`; subscribe trả ack sau khi đăng ký connection với actor/session/space. Handler/buffer client bật trước invoke; subscribe lặp không nhân connection hoặc nhận nhiều bản vì một lệnh lặp.

Sự kiện `MessageChanged` có `{eventId,conversationId,message}`; eventId không dùng làm cursor lịch sử. `AccessRevoked` có `{scope,conversationId?,reason}` chỉ phục vụ UI, không mang nội dung tin và không thay việc server ngừng phát. Sai subscribe trả HubException với JSON ProblemDetails theo schema, không stack trace; auth subscription thất bại không tải lịch sử tài nguyên đó. Client dùng errorCode khi được nhận, không đoán ý nghĩa từ message tiếng Anh.

Dispatcher claim outbox bằng lease, lấy message hiện hành qua Messaging, user summary qua Identity và chỉ phát tới registry connections đã đủ quyền. Registry gắn `connectionId,userId,sessionId,spaceId`; guard kiểm tra trước phát nội dung, đối soát ≤1 giây và sự kiện thu hồi commit bỏ định tuyến/đóng kết nối. Backend lỗi kiểm tra quyền phải dừng phát; HTTP history vẫn kiểm tra quyền và outbox còn để retry. SignalR disconnect/reconnect đăng ký lại, bù REST và reload các trang cũ để thấy edit/delete.

[IAccountAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountAccessGuard.cs) và shared [RelationalWorkScope](../../../services/SCDC.BuildingBlocks/Infrastructure/Persistence/RelationalWorkScope.cs) đã có trong source cho guard actor và transaction một host. Consumer DM/Hub chưa được tích hợp. Phần còn cần thiết kế/triển khai gồm guard recipient khi gửi mới, `IUserSearchDirectory` cho search/profile, `IAuthenticatedSessionReader` cho realtime và mở rộng `IRealtimeAccessRevoker` theo session/user/space.

Identity giữ dữ liệu bảo mật; Messaging giữ registry/Hub. `IUserDirectory` hiện chỉ trả active user summary, không chứng minh verified/valid session; không thay guard bằng summary khác null. Hub và outbox dispatcher còn chưa có runtime; Redis scale-out chỉ là phương án khi có quyết định riêng.

### Xác thực SignalR

SignalR giữ principal của lúc kết nối và không tự phản ánh việc thu hồi phiên/quyền trong thời gian kết nối; vì vậy cần kiểm tra dữ liệu hiện hành và đóng/bỏ định tuyến connection. Bật `CloseOnAuthenticationExpiration` chỉ giải quyết hết hạn token, không thay thế thu hồi nghiệp vụ. Xem [tài liệu xác thực SignalR của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0). JavaScript dùng `accessTokenFactory`; query token của WebSocket chỉ được nhận đúng route Hub và bị loại khỏi log proxy/API. Map user routing theo `sub` ổn định, không displayName.

Interface phiên/thu hồi theo session là phần mở rộng cần thiết kế trong `SCDC.Contracts`: `IUserDirectory` hiện chỉ trả thông tin user, `IRealtimeAccessRevoker` hiện chỉ nhận user/space ID. Không đọc DB Identity trực tiếp từ Messaging để thay interface còn thiếu. Mọi cơ chế trên vẫn phải thử nghiệm rollback, concurrent commit, mất response, worker dừng và revoke khi reconnect.

## Kiểm chứng và phần còn lại

Thử nghiệm outbox sau commit, worker dừng/restart, event trùng/đảo thứ tự, reconnect nhiều trang và sửa/xóa tin cũ theo [TC-DM-15–17/25/26](direct-messaging.md#tests) và [UC-COM-25/TC-COM-25](channel-messaging.md#uc-com-25). Thu hồi phải đo trên kết nối đang mở từ commit đến ngừng truy cập trong ≤5 giây, gồm khi authority lỗi; HTTP guard hoặc fixture riêng chưa đủ. Ghi cấu hình/key ring, dataset, build/commit và phép đo trong hồ sơ thực thi.

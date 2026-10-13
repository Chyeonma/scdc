# Lịch sử và đồng bộ tin

History/cursor, outbox, Hub, reconnect và thu hồi của Messaging dùng chung cho DM và tin phòng. Quyền DM nằm trong [nhắn tin riêng](direct-messaging.md#permissions); tin phòng bổ sung channel/membership epoch theo [tin phòng](channel-messaging.md#channel-admission).

## Trạng thái hiện tại

REST/SignalR, UUID, cursor bảo vệ và deadline chat ≤5 giây đã chốt DEC-081/083. Nền DM `900e226` đã có history/cursor, Hub, dispatcher outbox, tự reconnect/bù nhiều trang và tải lại cửa sổ tin cũ. [DM-MVP](../../records/verification/messaging/DM-MVP.md) ghi bằng chứng reconnect/worker/thu hồi theo revision; [kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md) ghi luồng nhận realtime và tải lại lịch sử DM. Đây là bằng chứng nền một host, chưa đóng toàn bộ AC/gate v1. Tin phòng và các giả định transaction/quyền xuyên service vẫn cần tích hợp, rà soát và kiểm chứng theo DEC-116.

<a id="contract-8"></a>

<a id="hợp-đồng-8--lịch-sử-và-kết-nối-thời-gian-thực"></a>

## Lịch sử và kết nối thời gian thực

`sequence` và `version` truyền dưới dạng chuỗi số nguyên; client so sánh bằng BigInt, không chuyển sang Number có thể mất độ chính xác. ID server dùng UUIDv7, `clientMessageId` dùng UUIDv4 theo DEC-081.

Trang lịch sử DM mặc định 50, tối đa 100 tin. Lần mở đầu lấy
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

Hợp đồng nhận cập nhật DM: đăng ký hội thoại đã có quyền; thông báo
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

Hợp đồng đồng bộ yêu cầu dispatcher claim outbox bằng lease, lấy message hiện hành qua Messaging, user summary qua Identity và chỉ phát tới registry connections đã đủ quyền. Registry gắn `connectionId,userId,sessionId,spaceId`; guard kiểm tra trước phát nội dung, đối soát ≤1 giây và sự kiện thu hồi commit bỏ định tuyến/đóng kết nối. Backend lỗi kiểm tra quyền phải dừng phát; HTTP history vẫn kiểm tra quyền và outbox còn để retry. SignalR disconnect/reconnect đăng ký lại, bù REST và reload các trang cũ để thấy edit/delete. Phần tích hợp thực tế của nền DM được ghi bên dưới; thông báo thu hồi sau commit và phối hợp xuyên service vẫn cần đối chiếu theo gói v1.

[IAccountPairAccessGuard](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs) và [ISharedDatabaseSession](../../../services/SCDC.Contracts/Persistence/ISharedDatabaseSession.cs) đã được tích hợp vào writer/history và [ConversationAccess](../../../services/Modules/Messaging/Realtime/ConversationAccess.cs) của DM trong monolith. Identity triển khai guard cặp actor/peer, [IUserSearchDirectory](../../../services/SCDC.Contracts/Identity/IUserSearchDirectory.cs), [IAuthenticatedSessionReader](../../../services/SCDC.Contracts/Identity/IAuthenticatedSessionReader.cs) và historical summary reader; Messaging dùng các hợp đồng này, không đọc bảng Identity trực tiếp. Các adapter mạng và phối hợp hàng rào/quyền khi tách service vẫn cần thiết kế tại [thu hồi truy cập v1](../../system/access-revocation.md#v1-authorization-coordination).

Identity giữ dữ liệu bảo mật; Messaging giữ registry/Hub chat. Nền DM đã đăng ký [ChatHub và các worker](../../../services/Modules/Messaging/MessagingModule.cs) trong API host MVP. Media control giữ Hub riêng theo [D11](../media/participation-lifecycle.md#v1-media-realtime); Hub/worker tại API của từng owner và một bản chạy/service là mục tiêu v1 theo [D12/D13](../../system/backend.md#v1-realtime-hosting)/[D21](../../system/environments.md#v1-service-replicas). `IUserDirectory` chỉ trả active user summary, không chứng minh verified/valid session; runtime DM dùng guard/session reader riêng. Không bổ sung backplane chỉ để phối hợp nhiều bản Hub trong cấu hình này; scale-out về sau cần quyết định riêng.

### Xác thực SignalR

SignalR giữ principal của lúc kết nối và không tự phản ánh việc thu hồi phiên/quyền trong thời gian kết nối; vì vậy cần kiểm tra dữ liệu hiện hành và đóng/bỏ định tuyến connection. Bật `CloseOnAuthenticationExpiration` chỉ giải quyết hết hạn token, không thay thế thu hồi nghiệp vụ. Xem [tài liệu xác thực SignalR của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0). JavaScript dùng `accessTokenFactory`; query token của WebSocket chỉ được nhận đúng route Hub và bị loại khỏi log proxy/API. Map user routing theo `sub` ổn định, không displayName.

Nền DM đã dùng `IAuthenticatedSessionReader` tại Hub, dispatcher và [SubscriptionReconciler](../../../services/Modules/Messaging/Realtime/SubscriptionReconciler.cs) để kiểm tra phiên hiện hành, bỏ định tuyến và đóng kết nối khi mất quyền hoặc không xác nhận được authority. `IRealtimeAccessRevoker` hiện vẫn chỉ có chữ ký user/space, chưa là hợp đồng thu hồi theo session được runtime DM sử dụng. Phối hợp thông báo sau commit, tin phòng và cơ chế thu hồi xuyên service v1 cần hợp đồng riêng; không đọc DB Identity trực tiếp từ Messaging để thay hợp đồng. Các ca rollback, concurrent commit, mất response, worker dừng và revoke/reconnect phải được hồi quy trên nền v1.

## Kiểm chứng và phần còn lại

Nền DM có bằng chứng agent về outbox/worker, event trùng/đảo thứ tự, reconnect nhiều trang, sửa/xóa tin cũ và thu hồi tại [DM-MVP](../../records/verification/messaging/DM-MVP.md); kết quả chỉ áp dụng build/phạm vi trong hồ sơ. Hồi quy các ca [TC-DM-15–17/25/26](direct-messaging.md#tests) khi chuyển v1 và thực thi riêng [UC-COM-25/TC-COM-25](channel-messaging.md#uc-com-25) cho tin phòng. Thu hồi phải đo trên kết nối đang mở từ commit đến ngừng truy cập trong ≤5 giây, gồm khi authority lỗi; HTTP guard hoặc fixture riêng chưa đủ. Ghi cấu hình/key ring, dataset, build/commit và phép đo trong hồ sơ thực thi; chưa có kết luận đạt toàn bộ tải/chất lượng hoặc thu hồi xuyên service v1.

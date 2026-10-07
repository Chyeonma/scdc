# SCDC — Messaging dùng chung

Nguồn chuẩn cho cơ chế xử lý tin văn bản của Messaging, dùng bởi DM và tin phòng. Thiết kế được đối chiếu ngày 2026-10-04, còn cần triển khai và kiểm chứng. Các mô tả cặp người tham gia, peer/search và route `direct-conversations` bên dưới minh họa DM; chúng không thay điều kiện membership/view/epoch của tin phòng. Quyền và hành trình riêng tại [DM](../../features/direct-messaging/README.md) và [tích hợp Community](../../features/community/specs/integration.md#responsibilities).

| Cần tìm | Đọc |
|---|---|
| Validation, state UI và TC-TEXT | [Nội dung tin](text.md#contract-6) |
| Model, fingerprint, khóa/giao dịch và migration | [Lưu trữ và đồng thời](persistence.md#contract-4) |
| History/cursor, reconnect, Hub và thu hồi | [Lịch sử và realtime](realtime.md#contract-8) |
| API/search/list riêng cho DM | [Thiết kế DM](../../features/direct-messaging/design/README.md#contracts) |

### Thiết kế tích hợp theo DEC-081/083

- HTTP dùng Bearer như Identity hiện tại; mutation chỉ qua REST. SignalR `/hubs/chat` chỉ subscribe/unsubscribe và thông báo `MessageChanged`; không tạo đường gửi tin tự retry qua Hub.
- ID server UUIDv7, clientMessageId UUIDv4. Writer chuẩn hóa thứ tự cặp UUID theo thứ tự DB, có fixture đối chiếu; không dùng username hoặc displayName làm khóa hội thoại.
- Sequence được cấp dưới khóa row/counter hội thoại trong cùng transaction với tin, SendOperation và outbox; writer thứ hai chỉ cấp sau writer trước commit/rollback. Sequence/version truyền chuỗi số nguyên, client so sánh BigInt. Counter đề xuất ánh xạ `spaces.last_message_sequence`; không dùng identity toàn cục hiện tại làm mốc commit.
- Cursor dùng cơ chế bảo vệ có mã hóa và xác thực (đề xuất ASP.NET Core Data Protection), gồm actor/resource/hướng/filter/mốc through và vị trí sort. Hạn kỹ thuật đề xuất 24 giờ; key ring phải bền qua restart/deploy. Token không dùng được giữa hai user/resource; cursor lỗi/hết hạn trả validation, client tải lại lịch sử, không tự gửi mutation.
- Tìm người trim/NFC/chuyển chữ thường để tạo search key là thiết kế đề xuất; giữ dấu, khớp substring sau chuẩn hóa, username khớp đúng xếp trước rồi username/ID. Key hiển thị/tên tài khoản trả vẫn theo hồ sơ; không chuẩn hóa NFC nội dung tin. Cần fixture Unicode để SQL/service cho cùng kết quả.
- Nội dung theo DEC-090; bộ White_Space/Default_Ignorable và control ở [text-policy.json](../../fixtures/text-policy.json), Unicode 17.0.0. Không trim/NFC hoặc bỏ ZWJ trong tin có nội dung; validation client/server dùng cùng bảng cố định.
- Hub kiểm tra phiên/quyền hiện hành khi subscribe. Dispatcher không phát nội dung cho connection/space bị thu hồi; revoker định tuyến theo session ID/space ID, bỏ subscription và đóng connection phù hợp. Dùng outbox sau commit cộng đối soát tối đa 1 giây là thiết kế đề xuất để đạt deadline thu hồi 5 giây; lỗi kiểm tra quyền phải dừng phát, không tiếp tục dùng cache không còn xác nhận.

SignalR giữ principal của lúc kết nối và không tự phản ánh việc thu hồi phiên/quyền trong thời gian kết nối; vì vậy cần kiểm tra dữ liệu hiện hành và đóng/bỏ định tuyến connection. Bật `CloseOnAuthenticationExpiration` chỉ giải quyết hết hạn token, không thay thế thu hồi nghiệp vụ. Xem [tài liệu xác thực SignalR của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0). JavaScript dùng `accessTokenFactory`; query token của WebSocket chỉ được nhận đúng route Hub và bị loại khỏi log proxy/API. Map user routing theo `sub` ổn định, không displayName.

Interface phiên/thu hồi theo session là phần mở rộng cần thiết kế trong `SCDC.Contracts`: `IUserDirectory` hiện chỉ trả thông tin user, `IRealtimeAccessRevoker` hiện chỉ nhận user/space ID. Không đọc DB Identity trực tiếp từ Messaging để thay interface còn thiếu. Mọi cơ chế trên vẫn phải thử nghiệm rollback, concurrent commit, mất response, worker dừng và revoke khi reconnect.

<a id="detailed-design"></a>

### Thiết kế chi tiết dùng chung

Phương án kỹ thuật ngày 2026-10-04, chưa có implementation. Chính sách nội dung/bản nháp đã chốt DEC-090/091; các thuật toán/mapping ở [nội dung](text.md), [lưu trữ](persistence.md) và [realtime](realtime.md) để Vg/Sáng rà soát và bàn giao, không tự ghi thêm quyết định sản phẩm.

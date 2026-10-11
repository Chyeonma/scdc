# SCDC — Thu hồi phiên và quyền truy cập

## Phạm vi

Hợp đồng xuyên Identity, Community, Messaging và Media. Chính sách tính quyền nằm tại [Community access control](../features/community/access-control.md); lifecycle phiên tại [Accounts](../features/accounts/README.md). File này giữ thời hạn hiệu lực và nghĩa vụ phối hợp.

## Quy tắc

| Đường truy cập | Điều kiện |
|---|---|
| HTTP mới | Kiểm tra session/account và quyền scope hiện hành |
| Mutation | Giữ quyền tới commit theo hợp đồng transaction; thay đổi quyền tranh mutation phải có thứ tự xác định |
| Subscribe/resume | Admission theo phiên, actor, scope và epoch hiện hành; cursor không cấp quyền |
| Chat đang kết nối | Ngừng định tuyến nội dung mới trong ≤5 giây từ commit thu hồi — DEC-083 |
| Media đang chạy | Ngừng cả phát/nhận RTP trong ≤5 giây từ commit; không xác nhận quyền thì dừng — DEC-099 |

Khóa tài khoản, revoke session, đổi mật khẩu, mất membership/View, xóa phòng hoặc thay role/ACL có thể kích hoạt thu hồi theo quy tắc của chủ đề. Thu hồi không xóa byte mà client đã tự sao chép.

## Trách nhiệm

- Identity là nguồn session/account/security stamp.
- Community là nguồn membership, ownership, role/ACL và access epoch/version.
- Messaging kiểm tra admission, guard writer/reader, registry/dispatch/reconnect và ngừng gửi tới connection không còn quyền.
- Media kiểm tra join/resume và thực thi lease/fencing ở đường truyền SFU. Đóng websocket không đủ chứng minh WebRTC đã ngừng.
- WebClient dọn state/draft/cache của scope mất quyền; server thực thi độc lập với client.

Guard trong một host dùng lock order Identity → Community → Messaging khi hợp đồng yêu cầu. Thiết kế writer/Hub tại [Messaging](../features/messaging/README.md), guard transaction tại [Community](community.md), coordinator/SFU tại [Media](../features/media/README.md). Shared lock không tự có hiệu lực xuyên service ở v1.

## Hiện trạng và kiểm chứng

Identity/Community có HTTP/transaction guard và bằng chứng theo gói Community. Consumer Messaging, Hub/dispatcher và proof deadline kết nối thật chưa được tích hợp; Media chưa có runtime. Các hồ sơ gói nền không chứng minh thu hồi realtime hoặc RTP.

Ca kiểm chứng phải ghi commit thu hồi, thời điểm cutoff, clock/trace, actor/scope/epoch và kết quả khi authority/worker/provider lỗi hoặc restart. Thử token/cursor/epoch cũ, tranh mutation/subscribe và reconnect. Tiêu chí chi tiết tại [tin phòng](../features/messaging/channel-messaging.md), [đồng bộ tin](../features/messaging/synchronization.md) và [Media](../features/media/README.md).

Restore phải giữ dấu thu hồi/floor và vô hiệu phiên/generation cũ trước mở truy cập theo [vòng đời dữ liệu](data-lifecycle.md#restore).

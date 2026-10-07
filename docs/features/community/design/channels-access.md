# SCDC — Thiết kế gói phòng text và ACL

[Kế hoạch](../delivery/channels-access/plan.md) chọn UC-COM-16/18/22 và metadata/list UC-COM-17. Nguồn chuẩn: [Channels](../specs/channels.md), [Permissions](../specs/permissions.md), [giao dịch/fingerprint](integration.md).

## Dữ liệu và nâng cấp

004 thêm name_key .NET trim/NFC/ToLowerInvariant so sánh C, unique partial theo server với channel active/nondeleted. Tên 1–100 UTF-16, topic tối đa 1.000, CRLF chuẩn hóa khi writer; giữ normalized_name legacy nhưng bỏ unique cũ. Kind text/voice bất biến; writer mới chỉ tạo text. Status active/deleted, deleted_at, version/access_version int32; một trigger version/timestamp cho mỗi update, kể cả ACL. AccessVersion dùng CAS cấu hình, metadata dùng version tổng thể của resource.

Runner yêu cầu 001/002/003 đúng checksum, mapping JSON đầy đủ cho channel legacy với kind/defaultView; tên/key theo batch 500. Legacy read-only, Messaging space archived hoặc deleted thiếu timestamp yêu cầu repair có review; không tự chọn kind/quyền, đổi tên/epoch hoặc purge nội dung. Space active/deleted giữ trạng thái tương ứng cho channel. Drain writer cũ → runner 004 → deploy mới. Bootstrap mới gồm 001–004; seed ghi key/defaultView/kind explicit.

## API và quyền

GET/POST channels, GET/PATCH channel, GET/PUT channel/access theo OpenAPI. POST default kind text/topic null, voice chưa có lifecycle Media nên bị từ chối. PATCH chỉ name/topic + expectedVersion; null topic để xóa. ACL PUT là tập đầy đủ, defaultView allow/deny, roleOverrides tối đa 21 (gồm @everyone), memberOverrides đúng membershipId; không có entry là inherit. Request được giới hạn kích thước, không tự replay mutation.

Management guard giữ Identity lease → server share/read hoặc update/mutation → channel share/update. Mọi single-channel route kiểm tra view trước quyền quản lý, hidden trả 404 kể cả manage_channels/manage_channel_access. Foundation phải valid session/active verified account/server/member. Default → role deny-wins (gồm @everyone) → personal đúng epoch → owner. Danh sách dùng cùng policy trong SQL trước LIMIT; cursor actor/server/limit/purpose/24 giờ, không hứa snapshot bất biến.

Create ghi Messaging space qua IChatSpaceLifecycle trên cùng connection/transaction; Community chỉ ghi community schema. IChannelAccessGuard nhận transaction caller, giữ share locks tới commit và trả lease có hạn session/epoch/access versions; caller phải kiểm tra lease trước commit và trạng thái space riêng của Messaging. Không sử dụng checker bool để thay admission guard. Chưa có lịch sử/send/subscription writer.

Create/metadata/ACL mutation tăng server accessVersion và ghi event outbox IDs/cause/version cùng transaction, không broadcast tên/ACL/member list. Metadata bump channel version; ACL bump channel accessVersion/version; role delete hoặc rejoin cleanup bump accessVersion các channel bị mất override để CAS snapshot cũ không ghi đè. No-op giữ mọi version/event. PUT tự làm mất view trả snapshot commit, GET tiếp theo 404.

## WebClient và bằng chứng

Detail member vào danh sách phòng theo quyền. Tạo giữ operation/body theo actor/server trước POST, retry chủ động nguyên body/key sau mất response/reload. Metadata/ACL edit giữ version/epoch đã đọc; conflict/timeout cần GET đối soát trước lưu lại. Actor/server/selection đổi hủy đọc cũ và bỏ dữ liệu quản lý; 401/403/404 đóng metadata/ACL cũ. Không hiển thị tin/composer khi gói chưa có API tin.

Quyền client chỉ điều khiển UI; máy chủ kiểm tra lại dưới khóa. Role/member catalogs chỉ tải khi actor được sửa ACL; roster phân trang, các member override hiện hữu vẫn giữ ID/epoch khi chưa ở trang hiện tại. Allow/deny/inherit được gửi như snapshot đầy đủ; không tự thay epoch cũ thành epoch mới.

Nghiệm thu API/DB/shared transaction và Chromium độc lập với thuật toán fixture. Không suy HTTP guard hoặc outbox thành proof thu hồi realtime/Media; các phụ thuộc vẫn ghi ở status.

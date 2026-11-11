# STEP-4 — Việc làm sớm

Việc của [STEP-4](step-4.md) (và một việc dọn nhánh cho STEP-3) **không phụ thuộc kiến trúc mới**, nên ai rảnh có thể nhận ngay, kể cả khi đang ở STEP-1 hoặc STEP-2. File này không phải nháp: thẻ được đồng bộ lên Trello. Cách đồng bộ xem [README](README.md).

Quy tắc khi nhận việc ở đây:

- Ưu tiên việc của bước hiện tại. Chỉ nhận thẻ ở đây khi không có thẻ nào của bước hiện tại đang chờ mình.
- Giao diện làm với **dữ liệu giả** theo [hợp đồng](../../contracts/README.md); không viết backend. Hợp đồng đổi thì giao diện phải sửa theo.
- Người review là người phụ trách service tương ứng; họ quyết định thẻ có còn đúng hướng không.

## E4-01 · Ghép hoặc bỏ nhánh worker email
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: nhánh `feat/identity` (worker gửi email, 1 commit, bỏ ngỏ từ 2026-10-05) được ghép vào nhánh chính, hoặc ghi lý do bỏ. Làm trước STEP-2 để tránh xung đột lớn khi tách Identity.
- Xong khi: không còn nhánh treo; test Identity qua; [email](../../features/accounts/email-links.md) cập nhật hiện trạng.
- Phụ thuộc: không.

## E4-02 · Giao diện tin phòng với dữ liệu giả
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Sáng
- Đầu ra: màn hình tin phòng trong WebClient dùng dữ liệu giả: lịch sử có phân trang, ô gửi, trạng thái đang gửi, lỗi, gửi lại, mất quyền, phòng bị xóa. Dùng lại thành phần DM nếu được ([tin phòng](../../features/messaging/channel-messaging.md), [thiết kế chung](../../experience/design-system.md)).
- Xong khi: chuyển được giữa dữ liệu giả và API bằng một chỗ cấu hình; Sáng review xong giao diện.
- Phụ thuộc: không.

## E4-03 · Prototype phòng thoại trên trình duyệt (MEDIA-GAP-05)
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Thái
- Đầu ra: giao diện phòng thoại và gọi riêng với dữ liệu giả: danh sách người trong phòng, micro, camera, chia sẻ màn hình; thiết bị bị từ chối, bị rút, đổi thiết bị; nhiều tab; kết nối lại ([phòng thoại](../../features/media/voice-rooms.md), [gọi riêng](../../features/media/direct-calls.md)).
- Xong khi: chạy trên các trình duyệt máy tính đã chọn; ghi lại hành vi khác nhau giữa trình duyệt.
- Phụ thuộc: không.

## E4-04 · Giao diện lời mời và duyệt tham gia với dữ liệu giả
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Vg
- Đầu ra: màn hình tạo và quản lý link mời, mời đích danh, hộp lời mời, yêu cầu tham gia và duyệt, đổi server sang riêng tư ([lời mời](../../features/community/invitations.md), [thành viên](../../features/community/memberships.md)).
- Xong khi: đủ trạng thái trống, lỗi, hết hạn, hết lượt; Vg review xong giao diện.
- Phụ thuộc: không.

## E4-05 · Review hợp đồng và giao diện Media (MEDIA-GAP-01)
- Nhãn: docs
- Người làm: ai rảnh · Review: Thái
- Đầu ra: rà OpenAPI Media, danh mục sự kiện realtime và giao diện theo DEC-099 đến 102; ghi chỗ thiếu hoặc mâu thuẫn vào mục "Còn mở" của [vòng đời tham gia](../../features/media/participation-lifecycle.md).
- Xong khi: Thái xác nhận danh sách chỗ cần sửa.
- Phụ thuộc: không.

## E4-06 · Chốt các quy tắc còn mở của Community
- Nhãn: docs
- Người làm: Vg · Review: Sáng
- Đầu ra: quyết định giới hạn tần suất yêu cầu tham gia và lời mời (OQ-003, OQ-004); thời hạn lưu phòng đã xóa (DEC-105); ghi DEC mới và cập nhật trang tính năng.
- Xong khi: các mục tương ứng trong [câu hỏi còn mở](../open-questions.md) có kết luận.
- Phụ thuộc: không.

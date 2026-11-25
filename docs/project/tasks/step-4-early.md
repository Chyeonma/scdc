# STEP-4 — Việc làm sớm

Việc của [STEP-4](step-4.md) (và một việc dọn nhánh cho STEP-3) **không phụ thuộc kiến trúc mới**, nên ai rảnh có thể nhận ngay, kể cả khi đang ở STEP-1 hoặc STEP-2. File này không phải nháp: thẻ được đồng bộ lên Trello. Cách đồng bộ xem [README](README.md).

Quy tắc khi nhận việc ở đây:

- Ưu tiên việc của bước hiện tại. Chỉ nhận thẻ ở đây khi không có thẻ nào của bước hiện tại đang chờ mình.
- Giao diện làm với **dữ liệu giả** theo [hợp đồng](../../contracts/README.md); không viết backend. Hợp đồng đổi thì giao diện phải sửa theo.
- Người review là người phụ trách service tương ứng; họ quyết định thẻ có còn đúng hướng không.

**Rà khả năng bắt đầu — 2026-11-24:** cả sáu thẻ dưới đây đủ phụ thuộc để nhận việc trong phạm vi ghi trên thẻ. Chưa có kết quả nghiệm thu; việc cập nhật mô tả không tự gán người hoặc chuyển cột Trello.

## E4-01 · Ghép hoặc bỏ nhánh worker email
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: đối chiếu nhánh `feat/identity` (worker gửi email, 1 commit, bỏ ngỏ từ 2026-10-05) với Identity hiện tại; tích hợp phần email còn phù hợp vào nhánh chính, hoặc ghi lý do bỏ và cách cung cấp worker mà S3-04 cần. Giữ các thay đổi phiên, permit và thu hồi của STEP-2; ưu tiên xử lý trước S3-04.
- Xong khi: không còn nhánh treo; test Identity qua; [email](../../features/accounts/email-links.md) cập nhật hiện trạng.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — cập nhật 2026-11-25**. Đối chiếu nhánh email cũ với host Identity đã tách ở S3-02/S3-03; tích hợp phần còn phù hợp vào các project Identity hiện tại, giữ nguyên JWT, Redis, permit/fence và dữ liệu đã chuyển.

## E4-02 · Giao diện tin phòng với dữ liệu giả
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Sáng
- Đầu ra: màn hình tin phòng trong WebClient dùng dữ liệu giả: lịch sử có phân trang, ô gửi, trạng thái đang gửi, lỗi, gửi lại, mất quyền, phòng bị xóa. Dùng lại thành phần DM nếu được ([tin phòng](../../features/messaging/channel-messaging.md), [thiết kế chung](../../experience/design-system.md)).
- Xong khi: chuyển được giữa dữ liệu giả và API bằng một chỗ cấu hình; Sáng review xong giao diện.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — 2026-11-24**. Chỉ làm giao diện và dữ liệu giả theo hợp đồng; nối API thật thuộc COM-W03 sau STEP-3.

## E4-03 · Prototype phòng thoại trên trình duyệt (MEDIA-GAP-05)
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Thái
- Đầu ra: giao diện phòng thoại và gọi riêng với dữ liệu giả: danh sách người trong phòng, micro, camera, chia sẻ màn hình; thiết bị bị từ chối, bị rút, đổi thiết bị; nhiều tab; kết nối lại ([phòng thoại](../../features/media/voice-rooms.md), [gọi riêng](../../features/media/direct-calls.md)).
- Xong khi: chạy trên các trình duyệt máy tính đã chọn; ghi lại hành vi khác nhau giữa trình duyệt.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — 2026-11-24**. Prototype dùng dữ liệu giả, không chờ S1-07 hoặc service Media; kết nối LiveKit và kiểm chứng media thật thuộc các task Media tương ứng.

## E4-04 · Giao diện lời mời và duyệt tham gia với dữ liệu giả
- Nhãn: UI/UX
- Người làm: ai rảnh · Review: Vg
- Đầu ra: màn hình tạo và quản lý link mời, mời đích danh, hộp lời mời, yêu cầu tham gia và duyệt, đổi server sang riêng tư ([lời mời](../../features/community/invitations.md), [thành viên](../../features/community/memberships.md)).
- Xong khi: đủ trạng thái trống, lỗi, hết hạn, hết lượt; Vg review xong giao diện.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — 2026-11-24**. Chỉ làm giao diện với dữ liệu giả; backend và nối API thật thuộc COM-W09, COM-W10 sau STEP-3.

## E4-05 · Review hợp đồng và giao diện Media (MEDIA-GAP-01)
- Nhãn: docs
- Người làm: ai rảnh · Review: Thái
- Đầu ra: rà OpenAPI Media, danh mục sự kiện realtime và giao diện theo DEC-099 đến 102; ghi chỗ thiếu hoặc mâu thuẫn vào mục "Còn mở" của [vòng đời tham gia](../../features/media/participation-lifecycle.md).
- Xong khi: Thái xác nhận danh sách chỗ cần sửa.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — 2026-11-24**. Đầu vào là đặc tả, OpenAPI và sự kiện realtime đã có; không cần backend Media hoặc phiên LiveKit thật.

## E4-06 · Rà quy tắc Community theo các quyết định đã chốt
- Nhãn: docs
- Người làm: Vg · Review: Sáng
- Đầu ra: đối chiếu trang thành viên, lời mời và phòng với các quyết định hiện hành; lập danh sách phần đã chốt, phần còn phải triển khai và câu hỏi thật sự chặn COM-W09, COM-W10. Giữ [DEC-105](../../records/decisions/README.md#dec-105): nội dung phòng đã xóa được giữ online, chưa đặt thời hạn xóa. Giới hạn tần suất bổ sung tiếp tục là đề xuất theo [câu hỏi còn mở](../open-questions.md#v1-decisions), không thêm điều kiện nghiệm thu v1.
- Xong khi: các trang liên quan và đầu vào COM-W09, COM-W10 nhất quán; mỗi câu hỏi chặn có người theo dõi và task xử lý. Không đóng toàn bộ OQ-003/OQ-004 khi tính năng và bằng chứng còn thiếu; không tạo DEC mới cho quy tắc đã được xác nhận.
- Phụ thuộc: không.
- Khả năng bắt đầu: **Có thể làm ngay — 2026-11-24**. Rà tài liệu và tách phần việc theo chính sách đã chốt, không chờ backend STEP-4.

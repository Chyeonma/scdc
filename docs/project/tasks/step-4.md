# STEP-4 — Tính năng trên kiến trúc mới

Thẻ Trello của [STEP-4](../planning.md#step-4). Cách đồng bộ xem [README](README.md).

Trạng thái: nháp

Mã thẻ dùng lại mã COM-W và V1-MEDIA trong [kế hoạch](../planning.md#community-work-items); chi tiết đầu ra và tiêu chí đầy đủ nằm ở đó, thẻ chỉ tóm tắt. Phần backend của các thẻ này chờ STEP-3; phần làm được ngay (giao diện với dữ liệu giả, review hợp đồng, câu hỏi còn mở) đã tách sang [việc làm sớm](step-4-early.md). Mobile, thông báo, gửi file chưa vào đây vì chưa chốt thuộc v1.

Rà lại ở buổi S3-12: cột "Phụ thuộc" trong kế hoạch còn viết theo transaction chung của MVP, phải đổi theo thiết kế STEP-2.

## COM-W01 · Tin phòng: nền dữ liệu và đọc lịch sử
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: model tin phòng, thứ tự theo không gian, cursor, đọc lịch sử có kiểm tra quyền (UC-COM-17).
- Xong khi: thử phân trang, phòng ẩn, khác server, phiên sai, epoch cũ trên database thật.
- Phụ thuộc: STEP-3.

## COM-W02 · Tin phòng: gửi và gửi lại
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: ghi tin có hàng rào quyền tới lúc commit; chống trùng; tin, sequence, outbox ghi cùng lúc (UC-COM-23).
- Xong khi: thử gửi lại, gửi đồng thời, khởi động lại, đổi quyền đúng lúc gửi.
- Phụ thuộc: COM-W01.

## COM-W03 · Tin phòng: giao diện với API thật
- Nhãn: feat, UI/UX
- Người làm: Sáng · Review: Vg
- Đầu ra: nối giao diện ở E4-02 với API thật: mở phòng → lịch sử → gửi → tải lại.
- Xong khi: trạng thái đã lưu, lỗi rõ ràng; gửi lại do người dùng bấm.
- Phụ thuộc: COM-W01, COM-W02, E4-02.

## COM-W04 · Tin phòng: nhận realtime
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: outbox worker, Chat Hub, đăng ký phòng có kiểm soát (UC-COM-25).
- Xong khi: hai tài khoản nhận tin; loại trùng; đối chiếu sau kết nối lại.
- Phụ thuộc: COM-W02.

## COM-W05 · Tin phòng: mất quyền
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: HTTP, gửi, đăng ký, bù tin đều theo quyền hiện tại (UC-COM-20 đến 22, 25).
- Xong khi: đo được kết nối ngừng trong 5 giây từ lúc commit, cho cả thu hồi phiên và đổi vai trò, ACL (DEC-083).
- Phụ thuộc: COM-W04.

## COM-W06 · Toàn luồng Community và tin phòng
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: tạo → tìm → tham gia → phòng chữ → lịch sử, gửi, nhận → mất quyền, chạy trên bốn service thật.
- Xong khi: hồi quy các chức năng cũ qua; ghi commit, môi trường, kết quả.
- Phụ thuộc: COM-W03, COM-W04, COM-W05.

## COM-W07 · Tích hợp và hồ sơ kiểm thử Community
- Nhãn: docs
- Người làm: Vg · Review: Sáng
- Đầu ra: chạy lại theo [hướng dẫn](../../guides/development.md#community); hồ sơ đối chiếu [tiêu chí chuyển tiếp](../../releases/mvp.md#community-acceptance).
- Xong khi: có hồ sơ trong `records/verification/community`.
- Phụ thuộc: COM-W06.

## COM-W08 · Xóa phòng
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: xóa phòng và không gian chat; chống trùng; chặn HTTP và Hub (UC-COM-19).
- Xong khi: chứng minh được trường hợp đang gửi tin đúng lúc xóa.
- Phụ thuộc: COM-W06.

## COM-W09 · Thông tin server, cách tham gia, duyệt
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: sửa thông tin server, đổi riêng tư, yêu cầu tham gia và duyệt (UC-COM-04, 05, 07, 08); nối giao diện E4-04.
- Xong khi: chứng minh duyệt, hủy, tham gia đồng thời; đổi sang riêng tư kết thúc yêu cầu đang chờ.
- Phụ thuộc: STEP-3, E4-04.

## COM-W10 · Lời mời
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: link mời, lượt dùng, hạn, mời đích danh (UC-COM-09 đến 13); nối giao diện E4-04.
- Xong khi: chứng minh lượt cuối, thu hồi và nhận đồng thời.
- Phụ thuộc: STEP-3, E4-04.

## COM-W11 · Chuyển chủ sở hữu và rời server
- Nhãn: feat
- Người làm: Vg · Review: Sáng
- Đầu ra: chuyển chủ, rời, dọn vai trò và ngoại lệ theo epoch (UC-COM-14, 15).
- Xong khi: chủ không tự rời được; epoch cũ bị từ chối; thu hồi HTTP và realtime.
- Phụ thuộc: COM-W05.

## COM-W12 · Sửa và xóa tin phòng
- Nhãn: feat
- Người làm: Sáng · Review: Vg
- Đầu ra: sửa, xóa tin theo quyền tác giả, placeholder, version, outbox (UC-COM-24).
- Xong khi: chứng minh tranh chấp sửa và xóa, dùng quyền hiện tại.
- Phụ thuộc: COM-W04.

## V1-MEDIA-03 · Phòng thoại (gồm COM-W13)
- Nhãn: feat
- Người làm: Thái · Review: Vg
- Đầu ra: phòng thoại từ backend tới giao diện theo [phòng thoại](../../features/media/voice-rooms.md); dùng quyền và vòng đời phòng của Community; nối giao diện E4-03.
- Xong khi: thử vào, rời, hạn mức, mất quyền, xóa phòng.
- Phụ thuộc: STEP-3, E4-03.

## V1-MEDIA-04 · Micro, camera, chia sẻ màn hình
- Nhãn: feat
- Người làm: Thái · Review: Sáng
- Đầu ra: thiết bị, nguồn phát, hiển thị nguồn theo [camera và chia sẻ](../../features/media/video-screen-sharing.md).
- Xong khi: thử từ chối quyền thiết bị, dừng nguồn từ trình duyệt, kết nối lại.
- Phụ thuộc: V1-MEDIA-03.

## V1-MEDIA-05 · Kiểm soát vào phòng, hạn mức, thu hồi
- Nhãn: feat, tech
- Người làm: Thái · Review: Vg
- Đầu ra: kiểm soát vào, hạn mức, thu hồi, dừng khi lỗi (MEDIA-GAP-02, 04).
- Xong khi: chứng minh ngắt cả phát và nhận trong 5 giây, kể cả khi worker hay mạng lỗi; thử tải theo MEDIA-GAP-06.
- Phụ thuộc: V1-MEDIA-03.

# STEP-3 — Tách từng service

Trạng thái: nháp

Các thẻ dưới đây còn chờ phụ thuộc, chưa đưa lên Trello. Các phần có thể bắt đầu ngay đã chuyển nguyên mã thẻ sang [step-3-ready.md](step-3-ready.md); không tạo mã trùng. Rà lại khi bước trước đạt tiêu chí trước khi công bố phần còn lại.

## S3-02 · 3a: Identity thành service riêng
- Nhãn: refactor, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: host Identity riêng (API và worker); ký access token bằng khóa bất đối xứng, service khác kiểm tra bằng khóa công khai qua cấu hình (D28, D29); tiếp tục công bố phiên vào Redis (D30). Lời gọi nội bộ dùng gRPC và mTLS ngay khi tách (D04, D20), không chờ S3-11 mới bật xác thực giữa service.
- Xong khi: đăng nhập, phiên, thu hồi chạy qua gateway; module còn lại trong monolith dùng Identity qua mạng; toàn bộ test qua.
- Phụ thuộc: S3-01.
- Khả năng bắt đầu: **Chờ kết thúc STEP-2 và S3-01**. Chỉ chuyển request thật sang host riêng sau khi checklist, hợp đồng và kiểm chứng ranh giới đã đạt.

## S3-04 · 3a: Worker gửi email chạy cùng Identity
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: worker gửi thư (đã ghép từ `feat/identity` ở E4-01) chạy trong service Identity (D12); cấu hình Gmail SMTP cho phát triển (D37).
- Xong khi: đăng ký, xác minh, đặt lại mật khẩu gửi thư thật trên môi trường phát triển.
- Phụ thuộc: S3-02, E4-01.
- Khả năng bắt đầu: **Chờ S3-02 và E4-01**. Phần đối chiếu, tích hợp worker trong monolith làm ở E4-01; thẻ này nghiệm thu worker trong host Identity riêng và gửi thư thật.

## S3-06 · 3b: Gọi riêng (V1-MEDIA-02)
- Nhãn: feat
- Người làm: Thái · Review: Sáng
- Đầu ra: gọi riêng từ backend tới giao diện theo [gọi riêng](../../features/media/direct-calls.md); báo cuộc gọi đến qua Media Hub.
- Xong khi: thử nhận, từ chối, hủy, hết giờ, bận, kết thúc trên hai trình duyệt; thu hồi phiên ngắt cuộc gọi.
- Phụ thuộc: S3-05, S3-02.
- Khả năng bắt đầu: **Chờ S3-05 và S3-02**. Prototype giao diện với dữ liệu giả làm ở E4-03; gọi và thu hồi phiên thật chờ backend Media/Identity.

## S3-07 · 3c: Messaging thành service riêng
- Nhãn: refactor, tech
- Người làm: Sáng · Review: Vg
- Đầu ra: host Messaging riêng gồm API, Chat Hub và outbox worker (D12, D13); dùng Identity và Community qua mạng bằng gRPC/mTLS theo D04/D20.
- Xong khi: DM, realtime, kết nối lại chạy qua gateway; test DM và Playwright `dm-mvp` qua.
- Phụ thuộc: S3-01, S3-02.
- Khả năng bắt đầu: **Chờ kết thúc STEP-2, S3-01 và S3-02**. Giữ thứ tự tách Identity trước Messaging; phương án chuyển dữ liệu có thể chuẩn bị ở S3-08.

## S3-09 · 3c: Tạo phòng giữa Community và Messaging qua mạng
- Nhãn: tech
- Người làm: Vg · Review: Sáng
- Đầu ra: luồng chuẩn bị, xác nhận, hủy của S2-09 chạy qua mạng (D14–D19).
- Xong khi: test mất phản hồi, gửi lại, hủy giữa chừng, khởi động lại giữa lúc dọn qua.
- Phụ thuộc: S3-07.
- Khả năng bắt đầu: **Chờ S3-07**. Dùng giao thức S2-09 đã có, kiểm chứng thêm lỗi mạng và phục hồi giữa hai host; không tính test adapter trong process là kết quả của thẻ này.

## S3-10 · 3d: Community là phần còn lại
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: gỡ code các module đã tách khỏi monolith; đổi tên host thành Community; database Community riêng.
- Xong khi: không còn code Identity, Messaging trong host Community; test Community và Playwright `community` qua.
- Phụ thuộc: S3-03, S3-08, S3-09.
- Khả năng bắt đầu: **Chờ S3-03, S3-08 và S3-09**. Chỉ gỡ module khỏi monolith khi host, dữ liệu và lời gọi thay thế đã được kiểm chứng.

## S3-12 · Kiểm chứng và tổng kết STEP-3
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo bốn service; bằng chứng mục 2 của [hoàn tất v1](../../releases/acceptance.md#v1-completion): build, chạy, khởi động lại, triển khai độc lập, mỗi service một database; ghi "Kết quả" trong [lộ trình](../planning.md#roadmap); ước lượng STEP-4.
- Xong khi: đạt tiêu chí xong của [STEP-3](../planning.md#step-3).
- Phụ thuộc: tất cả thẻ trên.
- Khả năng bắt đầu: **Chờ S3-01 đến S3-11**. Demo và nghiệm thu cần bốn service/database thật; checklist, host thử hoặc fixture chuẩn bị trước chưa thay được bằng chứng này.

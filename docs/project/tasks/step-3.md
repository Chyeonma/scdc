# STEP-3 — Tách từng service

Thẻ Trello của [STEP-3](../planning.md#step-3). Cách đồng bộ xem [README](README.md).

Trạng thái: nháp

Bản khung. Chi tiết phụ thuộc kết quả STEP-2 (hợp đồng thay transaction, cách kiểm tra phiên); rà lại ở buổi S2-12 rồi xóa dòng "Trạng thái: nháp". Chưa ước lượng.

Thứ tự: 3a Identity → 3c Messaging → 3d Community, nối tiếp; 3b Media làm song song từ đầu bước. Mỗi lần tách làm theo checklist S3-01.

## S3-01 · Checklist tách một service
- Nhãn: docs
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: checklist trong [hướng dẫn vận hành](../../guides/operations.md): dựng project theo cây v1 (D40, D43); chuyển code; thay phần cài đặt hợp đồng trong process bằng client gọi qua mạng; database riêng và script chuyển dữ liệu offline có đối chiếu số dòng, ID, version (D48–D52); định tuyến gateway; chạy lại test backend và Playwright; đo thời gian thu hồi; thử tắt service; đường lui trước khi mở ghi.
- Xong khi: 3a dùng checklist này; sau 3a cập nhật lại theo kinh nghiệm thực tế.
- Phụ thuộc: STEP-2 xong.

## S3-02 · 3a: Identity thành service riêng
- Nhãn: refactor, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: host Identity riêng (API và worker); ký access token bằng khóa bất đối xứng, service khác kiểm tra bằng khóa công khai qua cấu hình (D28, D29); tiếp tục công bố phiên vào Redis (D30).
- Xong khi: đăng nhập, phiên, thu hồi chạy qua gateway; module còn lại trong monolith dùng Identity qua mạng; toàn bộ test qua.
- Phụ thuộc: S3-01.

## S3-03 · 3a: Database Identity riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Vg · Review: Thái
- Đầu ra: PostgreSQL riêng cho Identity (D03); script chuyển schema `identity` từ database MVP; chạy thử trên bản sao dữ liệu.
- Xong khi: đối chiếu số dòng, ID, security stamp khớp; người dùng cũ đăng nhập được.
- Phụ thuộc: S3-02.

## S3-04 · 3a: Worker gửi email chạy cùng Identity
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: worker gửi thư (đã ghép từ `feat/identity` ở E4-01) chạy trong service Identity (D12); cấu hình Gmail SMTP cho phát triển (D37).
- Xong khi: đăng ký, xác minh, đặt lại mật khẩu gửi thư thật trên môi trường phát triển.
- Phụ thuộc: S3-02, E4-01.

## S3-05 · 3b: Khung service Media control
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: service mới theo cây v1, database Media riêng, Media Hub SignalR riêng (D11), kết nối LiveKit đã thử ở S1-07; kiểm tra JWT bằng thư viện chung.
- Xong khi: chạy qua gateway; Hub nhận kết nối có xác thực; tắt Media không ảnh hưởng chat.
- Phụ thuộc: STEP-1 xong; hợp đồng phiên từ STEP-2.

## S3-06 · 3b: Gọi riêng (V1-MEDIA-02)
- Nhãn: feat
- Người làm: Thái · Review: Sáng
- Đầu ra: gọi riêng từ backend tới giao diện theo [gọi riêng](../../features/media/direct-calls.md); báo cuộc gọi đến qua Media Hub.
- Xong khi: thử nhận, từ chối, hủy, hết giờ, bận, kết thúc trên hai trình duyệt; thu hồi phiên ngắt cuộc gọi.
- Phụ thuộc: S3-05, S3-02.

## S3-07 · 3c: Messaging thành service riêng
- Nhãn: refactor, tech
- Người làm: Sáng · Review: Vg
- Đầu ra: host Messaging riêng gồm API, Chat Hub và outbox worker (D12, D13); dùng Identity và Community qua mạng.
- Xong khi: DM, realtime, kết nối lại chạy qua gateway; test DM và Playwright `dm-mvp` qua.
- Phụ thuộc: S3-01, S3-02.

## S3-08 · 3c: Database Messaging riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: database Messaging trên instance chung (D02, D03); script chuyển schema `messaging` và outbox; chạy thử nhiều lần trên bản sao.
- Xong khi: đối chiếu tự động số dòng, sequence, version khớp; lịch sử DM cũ đọc được.
- Phụ thuộc: S3-07.

## S3-09 · 3c: Tạo phòng giữa Community và Messaging qua mạng
- Nhãn: tech
- Người làm: Vg · Review: Sáng
- Đầu ra: luồng chuẩn bị, xác nhận, hủy của S2-09 chạy qua mạng (D14–D19).
- Xong khi: test mất phản hồi, gửi lại, hủy giữa chừng, khởi động lại giữa lúc dọn qua.
- Phụ thuộc: S3-07.

## S3-10 · 3d: Community là phần còn lại
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: gỡ code các module đã tách khỏi monolith; đổi tên host thành Community; database Community riêng.
- Xong khi: không còn code Identity, Messaging trong host Community; test Community và Playwright `community` qua.
- Phụ thuộc: S3-03, S3-08, S3-09.

## S3-11 · 3e: Chuyển sang gRPC và mTLS
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: chỉ làm nếu S1-01 quyết định hoãn. Lời gọi nội bộ chuyển sang gRPC (D04), `.proto` ở `docs/contracts/grpc` (D46); mTLS giữa service (D20), cấp chứng chỉ qua Compose secrets (D36).
- Xong khi: không còn lời gọi nội bộ HTTP/JSON; service không có chứng chỉ hợp lệ bị từ chối.
- Phụ thuộc: S3-10.

## S3-12 · Kiểm chứng và tổng kết STEP-3
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo bốn service; bằng chứng mục 2 của [hoàn tất v1](../../releases/acceptance.md#v1-completion): build, chạy, khởi động lại, triển khai độc lập, mỗi service một database; ghi "Kết quả" trong [lộ trình](../planning.md#roadmap); ước lượng STEP-4.
- Xong khi: đạt tiêu chí xong của [STEP-3](../planning.md#step-3).
- Phụ thuộc: tất cả thẻ trên.

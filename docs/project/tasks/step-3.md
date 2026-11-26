# STEP-3 — Tách từng service

Thẻ Trello của [STEP-3](../planning.md#step-3). Cách đồng bộ xem [README](README.md).

Đã công bố để đồng bộ lên Trello theo yêu cầu ngày 2026-11-25.

Bản khung, chưa ước lượng chi tiết. [STEP-2 đã hoàn tất theo xác nhận của người dùng sau demo S2-12 ngày 2026-11-25](../../records/verification/environment/s2-12-2026-11-25.md). S3-01/S3-02/S3-03/S3-09/S3-10 đã hoàn tất, vào main và kiểm chứng dev; Identity và Community chạy host/database riêng. Công bố 12 thẻ để theo dõi tiến độ; các task còn lại cần rà đầu ra, công sức và phụ thuộc trước khi bắt đầu. Nhánh Media vẫn cần đối chiếu nghiệm thu S1-07 và hợp đồng phiên hiện tại.

Thứ tự triển khai hiện tại: Identity → Community → Messaging; Media làm song song từ đầu bước. Giữ mã nhóm 3a/3b/3c/3d để truy vết task cũ. Community nối RPC sang Messaging trong API cũ trước khi tách host/database Messaging. Mỗi lần tách làm theo checklist S3-01.

## S3-01 · Checklist tách một service
- Nhãn: docs
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: checklist trong [hướng dẫn vận hành](../../guides/operations.md): dựng project theo cây v1 (D40, D43); chuyển code; thay phần cài đặt hợp đồng trong process bằng client gọi qua mạng; database riêng và script chuyển dữ liệu offline có đối chiếu số dòng, ID, version (D48–D52); định tuyến gateway; chạy lại test backend và Playwright; đo thời gian thu hồi; thử tắt service; đường lui trước khi mở ghi.
- Xong khi: 3a dùng checklist này; sau 3a cập nhật lại theo kinh nghiệm thực tế.
- Phụ thuộc: STEP-2 xong.
- Kết quả: **Hoàn tất checklist, áp dụng thực tế và bổ sung sau lần tách Identity S3-02/S3-03 — 2026-11-25**. [Runbook EX-00 đến EX-08](../../guides/operations.md#service-extraction), [mẫu hồ sơ](../../records/templates/service-extraction.md) và [bản áp dụng Identity](../../records/verification/architecture/s3-01-2026-11-25.md) có bằng chứng host/RPC, dữ liệu/khóa, gateway, browser và hai nhánh phục hồi. Bổ sung catalog legacy, startup gate và điểm publisher đổi generation; S3-04/email vẫn riêng.

## S3-02 · 3a: Identity thành service riêng
- Nhãn: refactor, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: host Identity riêng (API và worker); ký access token bằng khóa bất đối xứng, service khác kiểm tra bằng khóa công khai qua cấu hình (D28, D29); tiếp tục công bố phiên vào Redis (D30).
- Xong khi: đăng nhập, phiên, thu hồi chạy qua gateway; module còn lại trong monolith dùng Identity qua mạng; toàn bộ test qua.
- Phụ thuộc: S3-01.
- Kết quả: **Hoàn tất và đã vào main — 2026-11-25** qua [PR #45](https://github.com/Chyeonma/scdc/pull/45), commit `dea6085`. Bốn project Identity, gRPC/mTLS hai chiều, consumer JWT/Redis và gateway đã chạy thật. Backend 408/408, frontend 82/82, gateway/browser 14/14 đạt; cutoff suite 389 ms. [Hồ sơ S3-02](../../records/verification/architecture/s3-02-2026-11-25.md); chuyển dữ liệu và kiểm chứng dev ở [S3-03](../../records/verification/architecture/s3-03-2026-11-25.md), cutoff dev 237 ms.

## S3-03 · 3a: Database Identity riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Vg · Review: Thái
- Đầu ra: PostgreSQL riêng cho Identity (D03); script chuyển schema `identity` từ database MVP; chạy thử trên bản sao dữ liệu.
- Xong khi: đối chiếu số dòng, ID, security stamp khớp; người dùng cũ đăng nhập được.
- Phụ thuộc: S3-02.
- Kết quả: **Hoàn tất chuyển dev, kiểm chứng và đồng bộ mã nguồn với main — 2026-11-25**. [PR #46](https://github.com/Chyeonma/scdc/pull/46) ban đầu merge vào nhánh S3-02; phần S3-03 được đưa riêng lên main qua nhánh `fix/s3-03-main-sync`. Chuyển đủ 22 bảng/459 dòng Identity, source giữ nguyên và runtime `NOLOGIN`; backup mới đã phục hồi/đối chiếu. Bộ chuyển dữ liệu 14/14, gateway hai instance 14/14; user/JWT/refresh/cursor cũ, browser, trace/restart đạt, cutoff dev 237 ms. [Hồ sơ S3-03](../../records/verification/architecture/s3-03-2026-11-25.md).

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
- Phụ thuộc: phạm vi chung STEP-1 xong; S1-07 của riêng Media và hợp đồng phiên từ STEP-2. S1-07 không chặn các service khác.

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
- Phụ thuộc: S3-01, S3-02; cần cấu trúc/host Community ở bước 1 S3-10. Theo thứ tự Community trước đã chọn, Messaging có thể tiếp tục ở API cũ; không cần đợi S3-07.
- Kết quả: **Hoàn tất và đã vào main — 2026-11-26** qua [PR #52](https://github.com/Chyeonma/scdc/pull/52), commit `425f170`, gồm cả phần RPC của bước 2. [Hồ sơ bước 2](../../records/verification/architecture/s3-10-phase-2-2026-11-26.md) ghi provision, hủy, mất phản hồi và tiếp tục journal sau restart trên ba host/database thử riêng. [Bước 3](../../records/verification/architecture/s3-10-phase-3-2026-11-26.md) đã đổi endpoint/gateway dev và kiểm chứng browser. Messaging tiếp tục ở API cũ tới S3-07/S3-08.

## S3-10 · 3d: Community thành service riêng
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: host Community riêng không tham chiếu implementation Identity/Messaging; database Community riêng. Theo thứ tự Community trước đã chọn, Messaging tạm ở host API cũ tới S3-07/S3-08.
- Xong khi: không còn code Identity, Messaging trong host Community; test Community và Playwright `community` qua.
- Phụ thuộc: S3-03, S3-09; S3-08 không chặn cutover Community, vì Messaging vẫn có thể ở host/database hiện tại.
- Kết quả: **Hoàn tất, đã vào main và chuyển dev — 2026-11-26**. [PR #50](https://github.com/Chyeonma/scdc/pull/50) hợp nhất bước 1 (`c3e5f61`); [PR #52](https://github.com/Chyeonma/scdc/pull/52) hợp nhất bước 2–3 (`425f170`). PR #51 đã đóng vì nội dung nằm trong PR #52. [Bước 1](../../records/verification/architecture/s3-10-phase-1-2026-11-25.md) tách project; [bước 2](../../records/verification/architecture/s3-10-phase-2-2026-11-26.md) nối RPC; [bước 3](../../records/verification/architecture/s3-10-phase-3-2026-11-26.md) chuyển 21 bảng/242 dòng, khóa writer cũ, đổi callback/gateway. Browser: 55 Community, 15 gateway và 14 DM MVP đạt; kiểm tra phiên/cursor/thao tác gửi lại và dữ liệu cũ trên dev đạt. Rollback trước ghi và chặn sau ghi đã diễn tập. Toàn STEP-3 còn các task riêng chưa hoàn tất.


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

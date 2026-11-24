# STEP-3 — Tách từng service

Thẻ Trello của [STEP-3](../planning.md#step-3). Cách đồng bộ xem [README](README.md).

**Trạng thái:** công bố backlog Trello ngày 2026-11-24; chưa nghiệm thu STEP-3.

Các thẻ ghi riêng phần chuẩn bị làm được ngay và phụ thuộc để triển khai đầy đủ. S3-01, S3-03, S3-05, S3-08 và S3-11 có phần chuẩn bị độc lập; việc tách service thật vẫn chờ kết quả STEP-2 và các task tiền nhiệm. Rà lại hợp đồng, ước lượng và thứ tự tại S2-12. Công bố backlog không đồng nghĩa các thẻ đều đã đủ điều kiện triển khai hoặc đã hoàn tất.

Thứ tự: 3a Identity → 3c Messaging → 3d Community, nối tiếp; 3b Media làm song song từ đầu bước. Mỗi lần tách làm theo checklist S3-01.

## S3-01 · Checklist tách một service
- Nhãn: docs
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: checklist trong [hướng dẫn vận hành](../../guides/operations.md): dựng project theo cây v1 (D40, D43); chuyển code; thay phần cài đặt hợp đồng trong process bằng client gọi qua mạng; database riêng và script chuyển dữ liệu offline có đối chiếu số dòng, ID, version (D48–D52); định tuyến gateway; chạy lại test backend và Playwright; đo thời gian thu hồi; thử tắt service; đường lui trước khi mở ghi.
- Xong khi: 3a dùng checklist này; sau 3a cập nhật lại theo kinh nghiệm thực tế.
- Phụ thuộc: STEP-2 xong.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Viết checklist từ quyết định kiến trúc và kết quả S2-01 đến S2-09; áp dụng để tách service thật chờ STEP-2 xong.
- Phần chuẩn bị: liệt kê lệnh build, migration, đối chiếu dữ liệu, cấu hình gateway, test lỗi phụ thuộc và điều kiện quay lui; ghi đầu vào, kết quả mong đợi và người thực hiện cho từng bước. Chạy rà trên bản sao cấu hình, không coi đó là bằng chứng 3a đã chạy độc lập.

## S3-02 · 3a: Identity thành service riêng
- Nhãn: refactor, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: host Identity riêng (API và worker); ký access token bằng khóa bất đối xứng, service khác kiểm tra bằng khóa công khai qua cấu hình (D28, D29); tiếp tục công bố phiên vào Redis (D30). Lời gọi nội bộ dùng gRPC và mTLS ngay khi tách (D04, D20), không chờ S3-11 mới bật xác thực giữa service.
- Xong khi: đăng nhập, phiên, thu hồi chạy qua gateway; module còn lại trong monolith dùng Identity qua mạng; toàn bộ test qua.
- Phụ thuộc: S3-01.
- Khả năng bắt đầu: **Chờ kết thúc STEP-2 và S3-01**. Chỉ chuyển request thật sang host riêng sau khi checklist, hợp đồng và kiểm chứng ranh giới đã đạt.

## S3-03 · 3a: Database Identity riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Vg · Review: Thái
- Đầu ra: PostgreSQL riêng cho Identity (D03); script chuyển schema `identity` từ database MVP; chạy thử trên bản sao dữ liệu.
- Xong khi: đối chiếu số dòng, ID, security stamp khớp; người dùng cũ đăng nhập được.
- Phụ thuộc: S3-02.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Kiểm kê dữ liệu và viết phương án chuyển offline; thực thi chuyển và nghiệm thu chờ S3-02.
- Phần chuẩn bị: lập manifest schema Identity, outbox, audit, ledger, khóa và cấu hình cần giữ; thiết kế đối chiếu số dòng, ID, security stamp, epoch và trạng thái công việc theo D51/D52. Thử công cụ trên database bản sao có dữ liệu giả; giữ nguyên database dev đang dùng.

## S3-04 · 3a: Worker gửi email chạy cùng Identity
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: worker gửi thư (đã ghép từ `feat/identity` ở E4-01) chạy trong service Identity (D12); cấu hình Gmail SMTP cho phát triển (D37).
- Xong khi: đăng ký, xác minh, đặt lại mật khẩu gửi thư thật trên môi trường phát triển.
- Phụ thuộc: S3-02, E4-01.
- Khả năng bắt đầu: **Chờ S3-02 và E4-01**. Phần đối chiếu, tích hợp worker trong monolith làm ở E4-01; thẻ này nghiệm thu worker trong host Identity riêng và gửi thư thật.

## S3-05 · 3b: Khung service Media control
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: service mới theo cây v1, database Media riêng, Media Hub SignalR riêng (D11), kết nối LiveKit đã thử ở S1-07; kiểm tra JWT bằng thư viện chung.
- Xong khi: chạy qua gateway; Hub nhận kết nối có xác thực; tắt Media không ảnh hưởng chat.
- Phụ thuộc: phạm vi chung STEP-1 xong; S1-07 của riêng Media và hợp đồng phiên từ STEP-2. S1-07 không chặn các service khác.
- Khả năng bắt đầu: **Làm phần khung ngay — 2026-11-24**. Nền gateway, Shared.Core và JWT đã có; tích hợp LiveKit và nghiệm thu đầy đủ vẫn chờ S1-07 và hợp đồng phiên của Media.
- Phần chuẩn bị: dựng cây project D40/D43, health endpoint, cấu hình gateway thử và kiểm tra JWT bằng Shared.Core trên stack riêng. Tách adapter LiveKit/phiên thành đầu vào chưa tích hợp; lời gọi giữa service khi bổ sung phải dùng gRPC/mTLS. Health hoặc JWT đạt chưa chứng minh Hub và cuộc gọi đã hoạt động.

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

## S3-08 · 3c: Database Messaging riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: database Messaging trên instance chung (D02, D03); script chuyển schema `messaging` và outbox; chạy thử nhiều lần trên bản sao.
- Xong khi: đối chiếu tự động số dòng, sequence, version khớp; lịch sử DM cũ đọc được.
- Phụ thuộc: S3-07.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Thiết kế chuyển dữ liệu và bộ đối chiếu trên database bản sao; thực thi chuyển và nghiệm thu chờ S3-07.
- Phần chuẩn bị: kiểm kê message, space, conversation, operation, sequence/version, outbox, fence/receipt và khóa đọc dữ liệu; định nghĩa đối chiếu ID, trạng thái chống trùng và sự kiện chưa phát. Dùng kết quả S2-08/S2-09; không phát lại outbox cũ hoặc tính lại fingerprint khi chuyển.

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

## S3-11 · Nền và kiểm chứng gRPC, mTLS giữa các service
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: project hợp đồng sinh từ `.proto` theo D46/D47, cấu hình gRPC/mTLS theo D04/D20 và cách cấp chứng chỉ qua Compose secrets theo D36. Áp dụng ngay trong từng lần tách service; sau S3-10 kiểm chứng toàn bộ API/worker. Chưa có quyết định hoãn gRPC hoặc mTLS.
- Xong khi: mọi lời gọi nội bộ nghiệp vụ dùng gRPC/mTLS; chứng chỉ không hợp lệ và service gọi sai quyền RPC bị từ chối; cấu hình bí mật, timeout và khởi động lại được thử trên các service thật.
- Phụ thuộc: phần chuẩn bị dùng hợp đồng S2-02 và quyết định D04/D20/D36/D46/D47; nghiệm thu toàn hệ thống chờ S3-10 và các service liên quan.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Nền hợp đồng, cấu hình và ma trận quyền RPC có thể làm trên host thử; nghiệm thu liên service chờ các host thật.
- Phần chuẩn bị: đối chiếu `.proto` với hợp đồng permit/fence/provision đã triển khai; chọn cấu hình sinh mã không trùng kiểu import; lập ma trận service/RPC, chứng chỉ tin cậy và ca thiếu/sai/hết hạn chứng chỉ. Dùng bí mật thử ngoài repo; chốt kết quả tích hợp trong từng task tách service.

## S3-12 · Kiểm chứng và tổng kết STEP-3
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo bốn service; bằng chứng mục 2 của [hoàn tất v1](../../releases/acceptance.md#v1-completion): build, chạy, khởi động lại, triển khai độc lập, mỗi service một database; ghi "Kết quả" trong [lộ trình](../planning.md#roadmap); ước lượng STEP-4.
- Xong khi: đạt tiêu chí xong của [STEP-3](../planning.md#step-3).
- Phụ thuộc: tất cả thẻ trên.
- Khả năng bắt đầu: **Chờ S3-01 đến S3-11**. Demo và nghiệm thu cần bốn service/database thật; checklist, host thử hoặc fixture chuẩn bị trước chưa thay được bằng chứng này.

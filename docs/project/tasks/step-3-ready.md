# STEP-3 — Phần có thể bắt đầu ngay

Công bố Trello ngày 2026-11-24 theo yêu cầu kiểm tra và đưa việc làm được ngay lên bảng. Mỗi thẻ ghi riêng **Khả năng bắt đầu** và **Phần chuẩn bị**; chỉ phần này có thể nhận ngay. Triển khai và nghiệm thu toàn bộ task vẫn theo phụ thuộc ghi trên thẻ.

Các task còn chờ nằm trong [step-3.md](step-3.md) và giữ nháp. Phần giao diện giả/rà tài liệu E4 tiếp tục ở [việc làm sớm](step-4-early.md).

## S3-01 · Checklist tách một service
- Nhãn: docs
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: checklist trong [hướng dẫn vận hành](../../guides/operations.md): dựng project theo cây v1 (D40, D43); chuyển code; thay phần cài đặt hợp đồng trong process bằng client gọi qua mạng; database riêng và script chuyển dữ liệu offline có đối chiếu số dòng, ID, version (D48–D52); định tuyến gateway; chạy lại test backend và Playwright; đo thời gian thu hồi; thử tắt service; đường lui trước khi mở ghi.
- Xong khi: 3a dùng checklist này; sau 3a cập nhật lại theo kinh nghiệm thực tế.
- Phụ thuộc: STEP-2 xong.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Viết checklist từ quyết định kiến trúc và kết quả S2-01 đến S2-09; áp dụng để tách service thật chờ STEP-2 xong.
- Phần chuẩn bị: liệt kê lệnh build, migration, đối chiếu dữ liệu, cấu hình gateway, test lỗi phụ thuộc và điều kiện quay lui; ghi đầu vào, kết quả mong đợi và người thực hiện cho từng bước. Chạy rà trên bản sao cấu hình, không coi đó là bằng chứng 3a đã chạy độc lập.

## S3-03 · 3a: Database Identity riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Vg · Review: Thái
- Đầu ra: PostgreSQL riêng cho Identity (D03); script chuyển schema `identity` từ database MVP; chạy thử trên bản sao dữ liệu.
- Xong khi: đối chiếu số dòng, ID, security stamp khớp; người dùng cũ đăng nhập được.
- Phụ thuộc: S3-02.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Kiểm kê dữ liệu và viết phương án chuyển offline; thực thi chuyển và nghiệm thu chờ S3-02.
- Phần chuẩn bị: lập manifest schema Identity, outbox, audit, ledger, khóa và cấu hình cần giữ; thiết kế đối chiếu số dòng, ID, security stamp, epoch và trạng thái công việc theo D51/D52. Thử công cụ trên database bản sao có dữ liệu giả; giữ nguyên database dev đang dùng.

## S3-05 · 3b: Khung service Media control
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: service mới theo cây v1, database Media riêng, Media Hub SignalR riêng (D11), kết nối LiveKit đã thử ở S1-07; kiểm tra JWT bằng thư viện chung.
- Xong khi: chạy qua gateway; Hub nhận kết nối có xác thực; tắt Media không ảnh hưởng chat.
- Phụ thuộc: phạm vi chung STEP-1 xong; S1-07 của riêng Media và hợp đồng phiên từ STEP-2. S1-07 không chặn các service khác.
- Khả năng bắt đầu: **Làm phần khung ngay — 2026-11-24**. Nền gateway, Shared.Core và JWT đã có; tích hợp LiveKit và nghiệm thu đầy đủ vẫn chờ S1-07 và hợp đồng phiên của Media.
- Phần chuẩn bị: dựng cây project D40/D43, health endpoint, cấu hình gateway thử và kiểm tra JWT bằng Shared.Core trên stack riêng. Tách adapter LiveKit/phiên thành đầu vào chưa tích hợp; lời gọi giữa service khi bổ sung phải dùng gRPC/mTLS. Health hoặc JWT đạt chưa chứng minh Hub và cuộc gọi đã hoạt động.

## S3-08 · 3c: Database Messaging riêng và chuyển dữ liệu
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: database Messaging trên instance chung (D02, D03); script chuyển schema `messaging` và outbox; chạy thử nhiều lần trên bản sao.
- Xong khi: đối chiếu tự động số dòng, sequence, version khớp; lịch sử DM cũ đọc được.
- Phụ thuộc: S3-07.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Thiết kế chuyển dữ liệu và bộ đối chiếu trên database bản sao; thực thi chuyển và nghiệm thu chờ S3-07.
- Phần chuẩn bị: kiểm kê message, space, conversation, operation, sequence/version, outbox, fence/receipt và khóa đọc dữ liệu; định nghĩa đối chiếu ID, trạng thái chống trùng và sự kiện chưa phát. Dùng kết quả S2-08/S2-09; không phát lại outbox cũ hoặc tính lại fingerprint khi chuyển.

## S3-11 · Nền và kiểm chứng gRPC, mTLS giữa các service
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: project hợp đồng sinh từ `.proto` theo D46/D47, cấu hình gRPC/mTLS theo D04/D20 và cách cấp chứng chỉ qua Compose secrets theo D36. Áp dụng ngay trong từng lần tách service; sau S3-10 kiểm chứng toàn bộ API/worker. Chưa có quyết định hoãn gRPC hoặc mTLS.
- Xong khi: mọi lời gọi nội bộ nghiệp vụ dùng gRPC/mTLS; chứng chỉ không hợp lệ và service gọi sai quyền RPC bị từ chối; cấu hình bí mật, timeout và khởi động lại được thử trên các service thật.
- Phụ thuộc: phần chuẩn bị dùng hợp đồng S2-02 và quyết định D04/D20/D36/D46/D47; nghiệm thu toàn hệ thống chờ S3-10 và các service liên quan.
- Khả năng bắt đầu: **Chuẩn bị được ngay — 2026-11-24**. Nền hợp đồng, cấu hình và ma trận quyền RPC có thể làm trên host thử; nghiệm thu liên service chờ các host thật.
- Phần chuẩn bị: đối chiếu `.proto` với hợp đồng permit/fence/provision đã triển khai; chọn cấu hình sinh mã không trùng kiểu import; lập ma trận service/RPC, chứng chỉ tin cậy và ca thiếu/sai/hết hạn chứng chỉ. Dùng bí mật thử ngoài repo; chốt kết quả tích hợp trong từng task tách service.

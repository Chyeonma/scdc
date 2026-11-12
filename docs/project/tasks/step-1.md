# STEP-1 — Khung chạy

Thẻ Trello của [STEP-1](../planning.md#step-1). File này là nguồn của **nội dung** thẻ (tiêu đề, người làm đề xuất, đầu ra, tiêu chí xong, phụ thuộc). Cột trạng thái, người được gán, hạn và bình luận quản lý trên Trello. Cách đồng bộ xem [README](README.md).

**Trạng thái:** đang thực hiện. S1-01 đã hoàn tất theo [xác nhận ngày 2026-11-12](../../records/planning/s1-01-confirmation-2026-11-12.md).

## S1-01 · Họp thống nhất lộ trình
- Nhãn: docs
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: cả nhóm đọc planning.md#roadmap; quyết định có hoãn gRPC và mTLS hay không (ghi DEC mới nếu đổi); bỏ chữ "Đề xuất" khỏi lộ trình.
- Xong khi: biên bản họp tuần ghi kết luận; planning.md đã cập nhật.
- Phụ thuộc: không. Làm đầu tiên.
- Kết quả: **Hoàn tất — 2026-11-12**, theo [xác nhận của người dùng](../../records/planning/s1-01-confirmation-2026-11-12.md); lộ trình đã được cập nhật. Việc hoãn gRPC và mTLS cần xác nhận riêng.

## S1-02 · Nginx làm cổng vào trước monolith
- Nhãn: tech
- Người làm: Vg · Review: Sáng
- Đầu ra: container Nginx định tuyến /api, /hubs tới API hiện tại và phục vụ file build WebClient (D09, D10, D22).
- Xong khi: WebClient chạy qua Nginx; DM gửi, nhận realtime vẫn đúng; SignalR (WebSocket) qua được gateway.
- Phụ thuộc: S1-01.
- Thiết kế và kiểm chứng: [Nginx trước monolith](../../system/api-conventions.md#s1-02-preparation). Nhánh `tech/s1-02-nginx-gateway`.
- Kết quả: **Đã triển khai và kiểm thử — 2026-11-12**, [7 ca gateway và 77 test frontend đạt](../../records/verification/gateway/s1-02-2026-11-12.md); chờ review theo quy trình.

## S1-03 · Service mẫu `/health` sau gateway
- Nhãn: tech
- Người làm: Sáng · Review: Vg
- Đầu ra: một project .NET nhỏ theo cây project v1 (D40, D43), chạy trong Compose, Nginx định tuyến tới.
- Xong khi: gọi /health qua gateway trả kết quả; dựng lại bằng một lệnh compose.
- Phụ thuộc: S1-02.
- Thiết kế và kiểm chứng: [service mẫu theo D40 và D43](../../system/backend.md#s1-03-sample-service). Nhánh `tech/s1-03-sample-service`, kế thừa commit S1-02 `d325070`.
- Kết quả: **Đã triển khai và kiểm thử — 2026-11-13**, [10 ca gateway và 77 test frontend đạt](../../records/verification/gateway/s1-03-2026-11-13.md); build độc lập và toàn solution đạt; chờ review theo quy trình.

## S1-04 · CI build và chạy test
- Nhãn: tech
- Người làm: Sáng · Review: Thái
- Đầu ra: workflow GitHub Actions build backend, chạy test backend và frontend trên mỗi pull request.
- Xong khi: một pull request cố ý làm hỏng test bị CI chặn.
- Phụ thuộc: không.
- Triển khai: [workflow CI](../../../.github/workflows/ci.yml) và [hướng dẫn cấu hình check bắt buộc](../../guides/continuous-integration.md). Nhánh `tech/s1-04-ci-build-test`, kế thừa commit S1-03 `eb1c300`.
- Kết quả: xem [hồ sơ kiểm chứng ngày 2026-11-13](../../records/verification/ci/s1-04-2026-11-13.md); còn phải xác nhận PR cố ý làm hỏng test bị chặn trên GitHub trước khi đánh dấu hoàn tất.

## S1-05 · Giám sát: OpenTelemetry Collector và OpenObserve
- Nhãn: tech
- Người làm: Thái · Review: Vg
- Đầu ra: Collector và OpenObserve trong Compose (D33–D35); API và service mẫu gửi log, trace.
- Xong khi: tìm được trace của một request lỗi đi qua Nginx và API.
- Phụ thuộc: S1-02, S1-03.

## S1-06 · Khung SCDC.Shared.Core và kiểm tra JWT dùng chung
- Nhãn: tech, refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: project Shared.Core (D45) với phần cấu hình kiểm tra JWT; service mẫu dùng nó để bảo vệ một endpoint.
- Xong khi: service mẫu chấp nhận token do API hiện tại cấp, từ chối token sai hoặc hết hạn.
- Phụ thuộc: S1-03.

## S1-07 · Thử LiveKit (V1-MEDIA-01)
- Nhãn: tech
- Người làm: Thái · Review: Vg duyệt phương án
- Đầu ra: LiveKit tự host trong Compose; trang thử đơn giản; đề xuất phiên bản server và SDK.
- Xong khi: hai trình duyệt nói chuyện được qua LiveKit trên máy phát triển; ghi lại khó khăn và ước lượng sơ bộ phần Media.
- Phụ thuộc: không. Làm song song từ đầu.

## S1-08 · Hướng dẫn dựng môi trường từ đầu
- Nhãn: docs
- Người làm: Thái · Review: Vg, Sáng (mỗi người tự làm theo)
- Đầu ra: cập nhật guides/development.md cho gateway, giám sát, service mẫu.
- Xong khi: mỗi thành viên dựng được môi trường trên máy sạch (hoặc xóa hết volume) chỉ bằng hướng dẫn.
- Phụ thuộc: S1-02 đến S1-05.

## S1-09 · Demo và tổng kết STEP-1
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo request đi qua gateway, trace, CI, LiveKit; ghi thời gian thực tế của STEP-1.
- Xong khi: planning.md ghi ngày xong và link bằng chứng ở cột "Kết quả"; có ước lượng sơ bộ cho STEP-2.
- Phụ thuộc: tất cả thẻ trên.

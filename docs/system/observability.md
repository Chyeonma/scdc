# SCDC — Giám sát hệ thống

## Phạm vi

Nguồn thiết kế thu thập và chuyển dữ liệu giám sát xuyên service. Ngưỡng và phương pháp đo tại [chất lượng](quality.md); thời hạn lưu và dữ liệu được phép ghi tại [vòng đời dữ liệu](data-lifecycle.md#inventory); quy trình điều tra/cảnh báo tại [vận hành](../guides/operations.md).

| Dữ liệu | Mục đích |
|---|---|
| Log | Ghi sự kiện kỹ thuật và lỗi để điều tra |
| Metric | Đo số lượng, độ trễ, tỷ lệ lỗi và tài nguyên theo thời gian |
| Trace | Theo dõi các bước xử lý liên quan của một yêu cầu xuyên service |

<a id="v1-telemetry-collection"></a>

## D33 — Đường thu thập dữ liệu giám sát

Trạng thái: đã xác nhận phương án A ngày 2026-10-12 — [DEC-151](../records/decisions/README.md#dec-151). Identity, Community, Messaging và Media control dùng OpenTelemetry cho log, metric và trace; dữ liệu đi qua Collector chung trước khi chuyển tới nơi lưu/tra cứu.

| Nội dung | Lựa chọn |
|---|---|
| Tại service | Đo/ghi dữ liệu bằng OpenTelemetry; cần triển khai instrumentation và truyền context phù hợp |
| Đường thu thập | Service → OpenTelemetry Collector chung → nơi lưu/tra cứu |
| Vai trò Collector | Nhận, xử lý và chuyển tiếp dữ liệu; cấu hình xử lý/xuất tập trung |
| Nơi lưu/tra cứu | OpenObserve OSS theo [D35](#v1-observability-tools), tự host cùng máy phát triển/demo theo [D34](#v1-observability-deployment); Collector giữ vai trò thu thập/chuyển tiếp |

Lựa chọn tập trung xử lý/chuyển tiếp khi các service dùng gRPC, outbox và Redis; đánh đổi là thêm thành phần và tài nguyên vận hành. Collector có thể cấu hình gom lô, thử lại và lọc; chưa chốt các cấu hình này. Nguồn: [OpenTelemetry — Collector](https://opentelemetry.io/docs/collector/).

Phương án B xuất trực tiếp đã được so sánh nhưng không được chọn; vẫn là cách phù hợp môi trường nhỏ theo [OpenTelemetry — không dùng Collector](https://opentelemetry.io/docs/collector/deploy/other/no-collector/). Collector không tự tạo trace, bảo đảm không mất dữ liệu hoặc chứng minh mục tiêu chất lượng; cần kiểm chứng instrumentation/context và đường thu thập thực tế.

<a id="v1-observability-deployment"></a>

## D34 — Triển khai giám sát cho phát triển/demo

Trạng thái: đã xác nhận phương án A ngày 2026-10-12 — [DEC-152](../records/decisions/README.md#dec-152). Collector, nơi lưu/tra cứu log, metric, trace và dashboard tự host trên máy Linux phát triển/demo hiện có bằng container do Docker Compose quản lý. Triển khai công khai đánh giá riêng.

Collector có container riêng, tách khỏi API và các thành phần lưu/tra cứu. D34 bổ sung các thành phần giám sát vào phạm vi Compose; không suy lựa chọn này từ D32 của Redis. OpenTelemetry có hướng dẫn [chạy Collector bằng Docker/Compose](https://opentelemetry.io/docs/collector/install/docker/); ví dụ không tự chọn image/version, cổng hoặc cấu hình cho SCDC.

| Nội dung | Lựa chọn |
|---|---|
| Collector | Container riêng trên cùng máy Linux phát triển/demo |
| Lưu/tra cứu và dashboard | OpenObserve OSS chạy single-node trong container trên cùng máy theo [D35](#v1-observability-tools) |
| Công cụ quản lý | Docker Compose; chưa mặc định cùng file/project hoặc cập nhật đồng loạt |

Lựa chọn dùng hạ tầng hiện có, không cần dịch vụ lưu/tra cứu bên ngoài cho baseline này. Nhóm tự vận hành, quản lý CPU/RAM/đĩa và phục hồi; các thành phần chịu sự cố chung của máy. Cần đo tải cùng ứng dụng, database, Redis và SFU trước khi kết luận đủ tài nguyên. Phương án B dùng dịch vụ lưu/tra cứu được quản lý đã được so sánh nhưng không được chọn.

Sản phẩm và chế độ chạy đã chọn tại D35. Image/version, số bản Collector, file/project, Docker host/máy ảo, cổng/mạng, quyền truy cập dashboard, TLS/ACL, dung lượng, lưu trữ bền, HA và phục hồi còn mở. Chính sách log hiện hành giữ nguồn chuẩn tại vòng đời dữ liệu; thời hạn metric/trace chưa chọn. Không chuyển audit bền sang telemetry. Sơ đồ triển khai đặt các thành phần giám sát trong máy phát triển/demo; chưa đổi code, Compose hoặc cấu hình.

<a id="v1-observability-tools"></a>

## D35 — Bộ công cụ giám sát

Trạng thái: đã xác nhận phương án A ngày 2026-10-12 — [DEC-153](../records/decisions/README.md#dec-153). Phát triển/demo dùng OpenObserve OSS chạy dạng một node, gộp lưu trữ, tra cứu và dashboard. Collector chung vẫn chạy riêng theo D33/D34.

| Nội dung | Lựa chọn |
|---|---|
| Sản phẩm | OpenObserve bản mã nguồn mở OSS, giấy phép AGPL-3.0 |
| Vai trò | Lưu và tra cứu log, metric, trace; cung cấp dashboard |
| Chế độ chạy | Single-node, một tiến trình OpenObserve trong container riêng trên máy phát triển/demo |
| Đường dữ liệu | Bốn service → Collector riêng → OpenObserve |

Lựa chọn giảm số thành phần và cấu hình tích hợp giữa các công cụ. Đánh đổi là lưu trữ, truy vấn và giao diện cùng nền tảng; đổi backend cần thiết kế exporter, chuyển truy vấn/dashboard và xử lý dữ liệu lịch sử. Chưa có phép đo tài nguyên hoặc bằng chứng HA. Phương án B dùng Grafana, Prometheus, Loki và Tempo đã được so sánh nhưng không được chọn.

OpenObserve nhận cả ba loại dữ liệu qua OTLP và có giao diện tích hợp. Bản single-node hỗ trợ SQLite và đĩa cục bộ; khả năng này chưa chọn storage backend hoặc volume cho SCDC. Nguồn: [kiến trúc OpenObserve](https://openobserve.ai/docs/architecture/), [OTLP](https://openobserve.ai/docs/ingestion/logs/otlp/), [repository OSS](https://github.com/openobserve/openobserve). Bản được chọn là OSS; các tính năng Enterprise chưa thuộc lựa chọn này.

D35 chưa chọn image/version, storage backend/volume cụ thể, số bản Collector, transport, TLS/credentials, quyền dashboard, tài nguyên, sampling, thử lại hoặc phục hồi. Chính sách log vẫn theo [vòng đời dữ liệu](data-lifecycle.md#inventory); cần kiểm chứng giới hạn truy vấn và xóa theo thời điểm gốc, không chỉ cấu hình retention. Thời hạn metric/trace còn mở. Sơ đồ đã ghi tên OpenObserve; chưa đổi code, SDK, Compose hoặc cấu hình.

## Phần cần thiết kế

Sản phẩm và chế độ chạy đã chọn tại [D35](#v1-observability-tools). Số bản Collector, giao thức xuất, SDK/version, sampling, tài nguyên, TLS/credentials, hàng đợi/thử lại hoặc hành vi khi đường giám sát lỗi còn mở. Vị trí/container/Compose đã chọn tại [D34](#v1-observability-deployment). Collector chung là vai trò thu thập, chưa chốt số tiến trình; gRPC/mTLS giữa service tại D04/D20 không tự chọn transport hoặc TLS cho telemetry. Cần kiểm chứng chi phí đo và giới hạn tài nguyên; sơ đồ chưa là cấu hình đã triển khai.

D33 không chuyển audit bền, outbox hoặc dữ liệu khôi phục nghiệp vụ sang log giám sát. Quy tắc lọc secrets/nội dung và thời hạn lưu giữ nguồn chuẩn ở vòng đời dữ liệu; không dựa riêng vào lọc tại Collector. Nginx, PostgreSQL, Redis và LiveKit cần tích hợp đo riêng; chưa coi SDK tại API bao phủ các thành phần này. Chưa đổi code, SDK, Compose hoặc cấu hình.

# SCDC — Giám sát hệ thống

Cách các service v1 ghi log, metric và trace, và nơi lưu để tra cứu.

**Trạng thái:** S1-05 đã có SDK ở monolith và service mẫu, Collector và OpenObserve trong Compose. Kết quả thực thi xem [hồ sơ S1-05](../records/verification/observability/s1-05-2026-11-14.md).

| Loại dữ liệu | Dùng để |
|---|---|
| Log | Ghi sự kiện kỹ thuật và lỗi để điều tra |
| Metric | Đo số lượng, độ trễ, tỷ lệ lỗi và tài nguyên theo thời gian |
| Trace | Theo dõi một yêu cầu qua nhiều service |

Mục tiêu và cách đo xem [chất lượng](quality.md). Dữ liệu được phép ghi và thời hạn lưu xem [vòng đời dữ liệu](data-lifecycle.md#inventory). Điều tra và cảnh báo xem [vận hành](../guides/operations.md).

## Sơ đồ

```text
Identity ┐
Community├─ OpenTelemetry ─→ Collector ─→ OpenObserve OSS
Messaging│   (log, metric,     (container    (lưu, tra cứu,
Media    ┘    trace)            riêng)        dashboard)
```

Tất cả chạy trong container do Docker Compose quản lý, trên máy Linux phát triển/demo hiện có. Triển khai công khai sẽ đánh giá riêng.

<a id="v1-telemetry-collection"></a>

## D33 — Thu thập qua Collector chung

Bốn service dùng OpenTelemetry để ghi log, metric và trace, gửi tới một OpenTelemetry Collector chung. Collector nhận, xử lý và chuyển tiếp tới nơi lưu.

Lý do: gom cấu hình xử lý và xuất dữ liệu về một chỗ, khi hệ thống có gRPC, outbox và Redis. Đổi lại phải vận hành thêm một thành phần. Collector có thể gom lô, thử lại và lọc dữ liệu, nhưng chưa cấu hình.

Collector không tự tạo trace và không đảm bảo không mất dữ liệu. Mỗi service tự gắn instrumentation và truyền context; cách kiểm chứng nằm bên dưới. Tham khảo: [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/).

<a id="v1-observability-deployment"></a>

## D34 — Tự host trên máy phát triển/demo

Collector, nơi lưu và dashboard chạy trong container trên máy Linux hiện có, do Docker Compose quản lý. Collector có container riêng, tách khỏi API và nơi lưu.

Lý do: dùng hạ tầng sẵn có, không cần dịch vụ ngoài. Đổi lại nhóm tự vận hành CPU, RAM, đĩa và phục hồi; mọi thứ cùng chịu sự cố của một máy. Cần đo tải chung với ứng dụng, database, Redis và LiveKit trước khi kết luận đủ tài nguyên.

<a id="v1-observability-tools"></a>

## D35 — OpenObserve OSS

Dùng OpenObserve bản mã nguồn mở (giấy phép AGPL-3.0), chạy một node trong một container. OpenObserve lưu và tra cứu cả log, metric, trace, và có sẵn dashboard. Dữ liệu nhận qua OTLP.

Lý do: ít thành phần và ít cấu hình tích hợp hơn so với bộ Grafana, Prometheus, Loki, Tempo. Đổi lại lưu trữ, truy vấn và giao diện gắn vào một sản phẩm; nếu đổi sau này phải chuyển exporter, truy vấn, dashboard và dữ liệu cũ. Tham khảo: [kiến trúc](https://openobserve.ai/docs/architecture/), [OTLP](https://openobserve.ai/docs/ingestion/logs/otlp/), [mã nguồn](https://github.com/openobserve/openobserve).

## Nguyên tắc

- Không đưa audit, outbox hay dữ liệu khôi phục nghiệp vụ vào log giám sát.
- Lọc bí mật và nội dung người dùng theo [vòng đời dữ liệu](data-lifecycle.md#inventory); không chỉ dựa vào bộ lọc của Collector.
- Xóa dữ liệu phải theo thời điểm gốc của bản ghi, không chỉ dựa vào cấu hình thời hạn lưu.
- Nginx, PostgreSQL, Redis và LiveKit cần cấu hình giám sát riêng; SDK trong API không bao phủ các thành phần này.

<a id="s1-05-implementation"></a>

## S1-05 — Triển khai trên máy phát triển

`compose.yaml` chạy OpenTelemetry Collector Contrib **0.165.0** và OpenObserve OSS **1.1.2**. Hai API dùng SDK OpenTelemetry .NET **1.21.0**, gửi OTLP/HTTP tới `otel-collector:4318`; Collector xuất ba tín hiệu vào organization `default`. File cấu hình là [collector.yaml](../../ops/observability/collector.yaml).

| Thành phần | Cấu hình |
|---|---|
| API monolith | `service.name=scdc-monolith` |
| Service mẫu | `service.name=scdc-sample` |
| Dashboard | `http://localhost:5080`; chỉ bind loopback |
| Tài khoản local mặc định | `root@scdc.local`, mật khẩu `ScdcObserve!2026Local`; đổi bằng `OPENOBSERVE_ROOT_EMAIL` và `OPENOBSERVE_ROOT_PASSWORD` trong `.env` trước lần dựng đầu |
| Dữ liệu | Volume `scdc-openobserve-data`; một node, SQLite và file local |
| Thời hạn giữ | Cấu hình OpenObserve 14 ngày cho dữ liệu giám sát; chưa kiểm chứng việc dọn sau đủ 14 ngày |
| Giới hạn bộ nhớ | Collector 256 MiB, memory limiter 128 MiB; OpenObserve 1 GiB |

Lần đầu chạy `make setup` (hoặc `make setup ENGINE=podman`); lệnh chuẩn bị cấu hình, build, khởi chạy và kiểm tra toàn bộ stack theo [hướng dẫn phát triển](../guides/development.md#setup). Các lần sau dùng `make up` với cùng engine. Đổi biến root sau khi đã tạo volume không tự đổi mật khẩu tài khoản đang lưu; cần đổi tài khoản qua OpenObserve rồi cập nhật cấu hình Collector tương ứng.

### Tín hiệu và dữ liệu được xuất

- Trace HTTP từ instrumentation ASP.NET Core, sampling 100% trên máy demo. Chỉ giữ method, route template, status code và loại lỗi; không ghi exception event hay mô tả exception tự do. Các kết nối `/hubs` được bỏ khỏi trace để tránh giữ span WebSocket dài và query credential.
- Log OTLP chỉ nhận category `SCDC.Telemetry`: bản tóm tắt HTTP có method, route template, status code, duration và trace/span ID. Log tùy ý từ module, body, query, token, header và scope không được đưa vào pipeline OTLP.
- Metric thời gian HTTP dùng `http.server.request.duration`, giới hạn thuộc tính theo method, status và route. Không đo database hay công việc nền trong task này.
- Access log Nginx chỉ có method, status, duration và upstream status. Error log dùng mức `crit` để tránh URL đầy đủ xuất hiện trong lỗi upstream.

Adapter SDK giữ trong từng Api, theo D45. Hai service không phụ thuộc tiến trình Collector để phục vụ request. Exporter SDK có timeout 1 giây và chạy theo lô; Collector có hàng đợi 256 lô, hai consumer, thử lại tối đa 30 giây. Hàng đợi ở RAM có thể mất dữ liệu khi tiến trình dừng hoặc đầy; không dùng làm outbox hay audit.

### Tra cứu request lỗi

1. Đăng nhập OpenObserve, chọn organization `default`. Logs dùng stream `scdc`; Traces cũng dùng stream `scdc`.
2. Lấy phần trace ID 32 ký tự từ `traceId` của problem response, hoặc từ header W3C `traceparent` đã gửi. Trong Logs, tìm `trace_id = '...'`; mở trace cùng ID trong Traces.
3. Đối chiếu `service_name`, `http_response_status_code`, route, thời điểm và log có status tương ứng. Metric xuất thành các stream theo tên instrument trong mục Metrics.

Nginx chuyển tiếp context W3C của request. Image Nginx hiện không tạo span riêng; bằng chứng là context đi qua gateway tới span của API. Collector không thu stdout Nginx, PostgreSQL hay outbox.

### Chạy kiểm chứng tự động

```bash
python3 scripts/test_observability.py --engine podman
```

Lệnh dựng stack riêng bằng `compose.gateway-test.yaml` và `compose.observability-test.yaml`; gateway ở `127.0.0.1:15410`, OpenObserve thử ở `127.0.0.1:15480`. Script dừng PostgreSQL thử để tạo HTTP 500, tìm log và trace qua Search API của OpenObserve, kiểm tra canary nhạy cảm không lọt vào dữ liệu, rồi thử dừng và khởi động Collector. Stack thử và volume được dọn sau khi chạy. Log nằm trong `artifacts/observability/`.

## Còn mở

- TLS, tài khoản ingestion riêng có quyền tối thiểu và quyền dashboard cho môi trường công khai.
- Sampling và giới hạn tài nguyên dưới tải thật; hiện mới kiểm chứng chức năng trên máy demo.
- HA, phục hồi kho giám sát, giám sát Nginx và hạ tầng, instrumentation công việc nền và database.
- Kiểm chứng dọn dữ liệu theo thời điểm gốc và chi phí lưu trữ thực tế.

---

Nguồn: [DEC-151](../records/decisions/README.md#dec-151) (D33), [DEC-152](../records/decisions/README.md#dec-152) (D34), [DEC-153](../records/decisions/README.md#dec-153) (D35). Các phương án đã cân nhắc ghi trong hồ sơ quyết định.

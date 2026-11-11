# SCDC — Giám sát hệ thống

Cách các service v1 ghi log, metric và trace, và nơi lưu để tra cứu.

**Trạng thái:** đã chốt thiết kế; chưa có code, SDK hay cấu hình Compose.

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

Collector không tự tạo trace và không đảm bảo không mất dữ liệu. Mỗi service phải tự gắn instrumentation và truyền context; cần kiểm tra thực tế. Tham khảo: [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/).

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

## Còn mở

- Image và phiên bản; số bản Collector; giao thức xuất, TLS và thông tin xác thực (gRPC/mTLS giữa service không tự áp dụng cho telemetry).
- Nơi lưu trữ và volume; tài nguyên; sampling; hàng đợi, thử lại và hành vi khi đường giám sát lỗi.
- Quyền truy cập dashboard; cổng và mạng; file Compose; HA và phục hồi.
- Thời hạn lưu metric và trace.
- Chi phí đo và giới hạn tài nguyên chưa được kiểm tra.

---

Nguồn: [DEC-151](../records/decisions/README.md#dec-151) (D33), [DEC-152](../records/decisions/README.md#dec-152) (D34), [DEC-153](../records/decisions/README.md#dec-153) (D35). Các phương án đã cân nhắc ghi trong hồ sơ quyết định.

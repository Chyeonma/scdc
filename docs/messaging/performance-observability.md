# P9-T02 — Hiệu năng và quan sát Messaging

## Mục tiêu và môi trường đo

Ngưỡng nghiệm thu ban đầu cho **một API instance**: 50 kết nối SignalR đồng thời, burst 20 tin với thông lượng ít nhất 10 tin/giây, một DM có 1.000 tin lịch sử, p95 send < 250 ms, history < 200 ms, search < 250 ms, inbox/unread < 200 ms và outbox lag lớn nhất < 5 giây khi dispatch 20 event. Đây là ngưỡng local để phát hiện hồi quy; phải đo lại với tải giữ ổn định, nhiều user và network thật trước khi cam kết SLO production.

Ngày 04/10/2026, đo bằng `tools/MessagingPerf` trên Windows local, .NET 10 `TestServer`, một API process, PostgreSQL 18 trong Docker (`scdc_chat_test`, cổng 5433). Harness tạo hai actor bằng Identity API, một DM, seed 1.000 tin (10% chứa từ khóa tìm kiếm), chạy API thật, tạo 50 kết nối SignalR Long Polling, dispatch 20 outbox event bằng publisher thật, sau đó dọn theo ID. Rate limit cho một sender khiến bài đo dùng burst 20 tin; thông lượng này **không chứng minh** tải liên tục 10 tin/giây. Kết quả lần chạy sau tối ưu:

| Chỉ số | Kết quả | Ngưỡng |
|---|---:|---:|
| Send, 20 mẫu | p95 29,7 ms; 30,9 tin/giây trong burst | < 250 ms; ≥ 10 tin/giây |
| History, 40 mẫu, trang 50 tin | p95 12,1 ms | < 200 ms |
| Search, 40 mẫu, trang 20 tin | p95 11,9 ms | < 250 ms |
| Inbox/unread, 40 mẫu | p95 16,4 ms | < 200 ms |
| SignalR và outbox | 50 kết nối; 20/20 event published; lag lớn nhất 2,269 giây | 50; < 5 giây |

Số liệu chịu ảnh hưởng JIT, cache và TestServer; không gồm TLS, proxy hay trình duyệt. Lệnh chạy lại từ repository root:

```powershell
docker compose -f compose.test.yaml up -d postgres-test
dotnet run --project tools/MessagingPerf/MessagingPerf.csproj
```

Harness từ chối DB khác `scdc_chat_test` và dọn dữ liệu nó tạo trong `finally`. Không chạy song song với suite test khác trên cùng fixture.

## Query plan và nút thắt

`EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)` trên 1.000 tin chọn `ix_messages_space_history` cho history (0,037 ms) và search có `LIMIT` (0,061 ms). PostgreSQL chọn quét theo timeline và lọc từ khóa vì tập dữ liệu nhỏ và cần 21 kết quả đầu; GIN `ix_messages_search` đã tồn tại nhưng chưa có lợi trong trường hợp này. Outbox claim trên 20 event dùng scan nhỏ (0,151 ms), chưa đủ bằng chứng để thêm index. Cần đo lại với nhiều space và backlog lớn trước khi thay index.

Rà soát N+1: `MessageService.ToDtosAsync` tải author, mention, attachment và thread count theo **tập ID của cả trang**; `UnreadCountReader` dùng truy vấn gộp theo space. Điểm nghẽn tìm thấy ở realtime fan-out: `SpaceUpdated` gọi kiểm tra quyền cho từng kết nối của cùng user. Trước sửa, 50 kết nối và 20 event mất 3,11 giây từ khởi tạo kết nối đến dispatch, lag lớn nhất 4,617 giây. Sau khi kiểm tra quyền một lần cho mỗi user trong từng event và xác nhận subscription trước gửi, hai số còn 0,70 và 2,269 giây. Không cache quyền qua các event; lần dispatch tiếp theo vẫn kiểm tra quyền mới nhất. Với 50 **user khác nhau**, số lần kiểm tra quyền vẫn tỷ lệ với số người nhận và cần benchmark riêng.

## Metric, dashboard và cảnh báo

Đặt `Observability__OtlpEndpoint` để bật OpenTelemetry metric và trace export. Ứng dụng dùng meter `SCDC.Messaging`; metric chỉ gắn nhãn `operation`/`outcome` cố định, không gắn user, space, message hay token. Các series chính: `scdc_messaging_request_duration_seconds` (send/history/search/unread), `scdc_messaging_send_failures_total`, `scdc_messaging_send_duplicate_retries_total`, `scdc_messaging_connections_active`, `scdc_messaging_connections_reconnected_total`, `scdc_messaging_outbox_dispatch_duration_seconds`, `scdc_messaging_outbox_delivery_lag_seconds`, `scdc_messaging_outbox_dispatch_failures_total`, `scdc_messaging_outbox_worker_failures_total`, `scdc_messaging_outbox_pending`, `scdc_messaging_outbox_quarantined`, `scdc_messaging_outbox_oldest_age_seconds` và `scdc_messaging_storage_failures_total`. Backlog được lấy mẫu mỗi 15 giây khi exporter bật. `reconnected` là cùng user/session kết nối lại trong 2 phút; kết nối tab mới có thể được tính như reconnect.

Overlay local đưa OTLP vào Collector, Prometheus và dashboard Grafana:

```powershell
docker compose -f compose.yaml -f compose.observability.yaml up -d otel-collector prometheus grafana
```

Prometheus: `http://127.0.0.1:9090`; Grafana: `http://127.0.0.1:3001/d/scdc-messaging` (tài khoản local trong Compose). Prometheus có cảnh báo outbox pending > 30 giây, quarantined > 0, lỗi API/worker, lỗi scanner/storage, reconnect tăng và mất scrape. Đây là stack local; môi trường triển khai cần secret Grafana riêng, lưu metric bền vững và route cảnh báo tới kênh trực vận hành. Trong lần kiểm chứng, collector/Prometheus/Grafana đều chạy, các rule `health=ok`, dashboard được provision và Prometheus nhận được request, outbox, connection và duplicate-retry metrics từ benchmark.

## Runbook sự cố

1. **Outbox dồn:** kiểm tra `pending`, `oldest.age`, `dispatch.failures`; xem worker log theo `EventId`, `MessageId`, `SpaceId`, `TraceId`. Xác nhận PostgreSQL, worker và Collector còn hoạt động. Query chỉ đọc:
   ```sql
   SELECT id, event_type, aggregate_id, space_id, attempt_count, available_at
   FROM integration.outbox_events
   WHERE published_at IS NULL AND event_type LIKE 'Messaging.%'
   ORDER BY available_at, occurred_at LIMIT 50;
   ```
2. **Event quá số lần thử:** chạy `MessagingOps list-failures --database <db>` với connection string lấy từ secret store; xử lý nguyên nhân publisher/permission/payload. Sau đó chạy `MessagingOps replay --database <db> --event <uuid>`. CLI dùng `IMessagingOutboxDispatcher.ReplayAsync`, khóa hàng và chỉ reset event Messaging chưa published. Theo dõi đến khi `published_at` xuất hiện; không chỉnh `attempt_count` bằng SQL tùy ý. Không replay hàng loạt khi chưa biết nguyên nhân.
3. **PostgreSQL lỗi:** kiểm tra kết nối, tài nguyên và log DB; tạm dừng tải nếu cần. Tin đã commit vẫn nằm trong message/outbox và worker sẽ thử lại khi DB hồi phục. Nếu API trả 5xx, đối chiếu `send.failures` và trace ID; không gửi lại bằng `clientMessageId` mới khi chưa kiểm tra trạng thái tin cũ.
4. **Storage/scanner lỗi:** xem `storage.failures` theo operation, kiểm tra SeaweedFS/ClamAV và upload staging. Upload lỗi không được gắn vào message; khôi phục service rồi để client thử lại theo contract upload.
5. **Reconnect tăng:** kiểm tra `connections.active`, `connections.reconnected`, proxy WebSocket/Long Polling, network và phiên Identity. Nếu nhiều user cùng bị ảnh hưởng, xác minh hub và DB trước khi tăng số instance.

Ví dụ CLI an toàn trên fixture test (không in connection string ra output):

```powershell
$env:ConnectionStrings__Database = '<connection string từ secret store>'
dotnet run --project tools/MessagingOps/MessagingOps.csproj -- list-failures --database scdc_chat_test
dotnet run --project tools/MessagingOps/MessagingOps.csproj -- replay --database scdc_chat_test --event <uuid>
```

CLI yêu cầu tên database tường minh khớp connection string; chỉ người trực vận hành được truy cập secret và thực hiện replay. Log và metric không chứa token hoặc nội dung chat mặc định; `last_error` cũ có thể chứa thông tin nhạy cảm nên CLI không in trường này.
Nếu cấu hình worker đổi `MaxAttempts`, đặt `SCDC_OUTBOX_MAX_ATTEMPTS` cùng giá trị khi chạy `list-failures`.

## Giới hạn một instance và thiết kế scale-out

Hiện registry kết nối, subscription và typing nằm trong RAM của từng API process; publisher outbox chỉ thấy kết nối trong process của nó. Vì vậy **chỉ chạy một API instance**. Trước khi scale: đưa fan-out SignalR qua backplane/managed service, thiết kế lookup session/connection phân tán để revoke đồng thời trên mọi node, đặt typing state TTL vào store phân tán (hoặc dùng event broadcast có expiry), và kiểm thử reconnect/catch-up khi chuyển node. PostgreSQL `FOR UPDATE SKIP LOCKED` cho phép nhiều worker claim event khác nhau, nhưng fan-out, session revoke và typing vẫn chưa an toàn xuyên instance. Đo lại p95 và outbox lag ở 50 user khác nhau sau khi hoàn thành thiết kế đó.

Tham chiếu exporter/metric: [ASP.NET Core metrics](https://learn.microsoft.com/en-us/aspnet/core/metrics/overview?view=aspnetcore-10.0), [OpenTelemetry .NET observability](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel).

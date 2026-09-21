# Messaging outbox operations

`MessagingOutboxWorker` chỉ xử lý các event có prefix `Messaging.`. Event Identity và event của module khác vẫn nằm trong `integration.outbox_events` để consumer sở hữu chúng xử lý; worker Messaging không đánh dấu chúng là published.

## Dispatch và giao nhận

Mỗi vòng worker lấy tối đa `BatchSize` event, nhưng khóa và transaction theo từng event:

1. Chọn event chưa published, đến hạn và chưa vượt `MaxAttempts` bằng `FOR UPDATE SKIP LOCKED` theo `available_at`, `occurred_at`, `id`.
2. Dispatch theo `event_type`. Hiện có `Messaging.MessageCreated`, phát reference event với chính `outbox_events.id` làm `eventId`.
3. Chỉ sau khi publisher hoàn tất, tăng `attempt_count` và đặt `published_at` trong cùng transaction.

Nếu process dừng trước khi phát, hoặc dừng sau khi phát nhưng trước commit đánh dấu published, transaction rollback. Event giữ `eventId` cũ và sẽ được phát lại. Đây là giao nhận ít nhất một lần; consumer phải dùng inbox/idempotency theo `eventId`, còn client deduplicate event/message ID và merge theo sequence/version.

## Retry, failure và replay

Lỗi dispatch tăng `attempt_count`, lưu lỗi đã rút gọn trong `last_error`, rồi đặt `available_at` theo exponential backoff từ `InitialRetryDelay` đến `MaxRetryDelay`. Event đạt `MaxAttempts` không còn được worker chọn tự động; đó là event cách ly để operator xem lại.

`IMessagingOutboxDispatcher` là contract vận hành nội bộ:

- `ListFailuresAsync(limit, ct)` liệt kê event Messaging đã đạt ngưỡng retry.
- `ReplayAsync(eventId, ct)` đặt lại attempt, lỗi và thời điểm sẵn sàng của một event chưa published.

Chưa mở endpoint HTTP replay vì repository chưa có authorization dành cho operator. Host quản trị sau này phải gọi contract này sau khi xác thực quyền, không được mở replay công khai.

Không cần thay schema ở P3-T02: `published_at`, `attempt_count`, `last_error`, `available_at` và PostgreSQL row lock đã đủ cho claim, recovery, quarantine và replay. Migration chỉ cần thiết nếu sau này muốn lease quan sát độc lập hoặc dead-letter retention riêng.

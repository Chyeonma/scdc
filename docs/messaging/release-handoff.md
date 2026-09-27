# P9-T03 — Bàn giao bản Messaging

Trạng thái: **release candidate nội bộ, chưa phát hành công khai**. Nhánh này
cung cấp cấu hình Compose không có seed, readiness PostgreSQL và quy trình vận
hành. Email xác thực và diễn tập backup/restore trên môi trường chạy thật còn là
điều kiện chặn phát hành; không được coi việc `docker compose config` thành công
là đã kiểm thử hệ thống chạy.

Nhánh P9-T03 hiện chồng lên P7-T02. P8 search, các task P8 khác và P9-T01/T02
chưa nằm trong base này; chỉ tạo bản phát hành tổng hợp sau khi các nhánh phụ
thuộc được tích hợp và kiểm thử lại cùng nhau.

## Dựng môi trường mới

Yêu cầu Docker Engine + Compose, dung lượng đủ cho PostgreSQL, object storage và
ClamAV, cùng một tên DNS/TLS reverse proxy nếu mở Internet. Chỉ một API instance:
typing và SignalR registry hiện giữ trạng thái trong process.

1. Sao chép `.env.release.example` thành `.env.release` và thay toàn bộ giá trị
   mẫu bằng secret duy nhất; giữ file ngoài Git. Signing key phải dài tối thiểu
   32 ký tự. Không dùng mật khẩu, signing key hoặc dữ liệu trong `compose.yaml`
   cho môi trường triển khai. Cổng PostgreSQL, S3 và ClamAV không publish ra host.
2. Kiểm tra cấu hình và khởi động từ thư mục gốc repository:

   ```text
   docker compose --env-file .env.release -f compose.release.yaml config --quiet
   docker compose --env-file .env.release -f compose.release.yaml up -d --build
   docker compose --env-file .env.release -f compose.release.yaml ps
   ```

3. Mở `http://localhost:3000/api/v1/health` để kiểm tra process, và
   `/api/v1/health/ready` để kiểm tra kết nối PostgreSQL. Nginx healthcheck gọi
   readiness qua proxy `/api/`; 200 nghĩa là API truy cập được DB, 503 là chưa
   sẵn sàng. Healthcheck này chưa kiểm tra S3/ClamAV, nên phải thử upload thật.
   Web ở `http://localhost:3000`; Swagger chỉ bật ở Development, bản release
   cố ý không mở `/swagger`.
4. `schema.sql` chỉ chạy khi volume PostgreSQL hoàn toàn mới; bản release
   **không mount `seed.sql`**. Không chạy `schema.sql` thủ công trên DB đang dùng:
   file này có lệnh `DROP SCHEMA`. Không dùng `docker compose down -v` với dữ
   liệu cần giữ.

API đọc `ConnectionStrings__Database`, `Modules__Identity__SigningKey`,
`Modules__Messaging__Attachments__Endpoint/AccessKey/SecretKey/Bucket/ClamAvHost`
và `Modules__Messaging__Outbox__*`. Release Compose bật `MessagingOutboxWorker`
theo `appsettings.json` (batch 20, poll 1 giây, retry tối đa 10 lần). Worker
cleanup staging chạy mỗi giờ; file upload chưa gắn message hết hạn sau 24 giờ.
SeaweedFS giữ file ở volume `scdc-object-data`; ClamAV phải tải xong signature
trước khi thử upload. Theo dõi log `chat-service`, `object-storage`, `clamav`
và outbox chưa publish. Identity outbox **chưa có consumer gửi email**; không
được mở đăng ký user công khai chỉ bằng Compose này.

## Nâng cấp không mất dữ liệu

Chốt version image/commit và sao lưu **cả DB lẫn object volume** trước khi
thay đổi. Giữ backup cũ cho đến khi smoke test và restore rehearsal đạt. Trên
DB đã tồn tại, chạy migration theo thứ tự; các script dưới đây dùng
`INSERT ... ON CONFLICT DO NOTHING` hoặc `CREATE ... IF NOT EXISTS`, không thay
bằng `schema.sql`:

```text
docker compose --env-file .env.release -f compose.release.yaml cp database/postgres/migrations/20260924_p6_t01_message_permissions.sql postgres:/tmp/001.sql
docker compose --env-file .env.release -f compose.release.yaml exec -T postgres psql -v ON_ERROR_STOP=1 -U scdc -d scdc_chat -f /tmp/001.sql
docker compose --env-file .env.release -f compose.release.yaml cp database/postgres/migrations/20260925_p7_t01_attachment_uploads.sql postgres:/tmp/002.sql
docker compose --env-file .env.release -f compose.release.yaml exec -T postgres psql -v ON_ERROR_STOP=1 -U scdc -d scdc_chat -f /tmp/002.sql
```

Chỉ chạy migration còn thiếu theo trạng thái DB; kiểm tra
`to_regclass('messaging.attachment_uploads')` và permission trong
`community.permissions` trước khi chạy. Với volume mới đã nạp `schema.sql`
hiện hành, không cần chạy lại hai migration. Các thay đổi additive không có
SQL rollback an toàn khi đã có dữ liệu mới. Khi nâng cấp lỗi, dừng ghi,
quay về image/commit trước nếu tương thích; nếu schema/data không tương thích,
restore **cặp** DB + object backup cùng thời điểm vào môi trường tách biệt,
kiểm tra rồi mới chuyển traffic. Không tự ý `DROP` bảng để rollback.

## Backup và diễn tập restore

Thực hiện trong cửa sổ dừng ghi để DB và file nhất quán. Thay `backup/` bằng
thư mục riêng có quyền hạn chế, nằm ngoài Git và lưu checksum. `pg_dump -Fc`
cho phép restore chọn database. Các lệnh này là runbook, chưa được xác nhận
bằng diễn tập trên Docker Engine ở máy hiện tại.

```text
docker compose --env-file .env.release -f compose.release.yaml exec -T postgres pg_dump -U scdc -d scdc_chat -Fc -f /tmp/scdc.dump
docker compose --env-file .env.release -f compose.release.yaml cp postgres:/tmp/scdc.dump ./backup/scdc.dump
docker compose --env-file .env.release -f compose.release.yaml cp object-storage:/data ./backup/object-data
```

Ghi SHA-256 của `scdc.dump` và toàn bộ cây `object-data`, thời gian UTC, commit
ứng dụng, version Postgres/SeaweedFS và số message/attachment vào manifest.
Thử phục hồi với project name riêng `scdc-restore`, để Compose tạo volume mới.
Sao chép file env riêng `.env.restore` và chọn `SCDC_WEB_PORT=3001` hoặc cổng
trống khác.
Trước lệnh `dropdb`, xác nhận `docker compose -p scdc-restore ... ps` chỉ liệt
kê container diễn tập. Database ở project này chỉ có schema vừa khởi tạo và
chưa có traffic. Không dùng `-p` của môi trường đang phục vụ:

```text
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml up -d postgres
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml ps
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml exec -T postgres dropdb -U scdc scdc_chat
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml exec -T postgres createdb -U scdc scdc_chat
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml cp ./backup/scdc.dump postgres:/tmp/scdc.dump
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml exec -T postgres pg_restore --exit-on-error -U scdc -d scdc_chat /tmp/scdc.dump
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml create object-storage
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml cp ./backup/object-data/. object-storage:/data/
docker compose -p scdc-restore --env-file .env.restore -f compose.release.yaml up -d object-storage clamav chat-service web-client
```

So sánh checksum file, số dòng `messaging.messages`, `messaging.attachments`,
`integration.outbox_events`; mở hai user test và tải một attachment đã lưu
qua cổng web diễn tập. Không restore đè lên volume đang phục vụ traffic. Ghi thời gian
khôi phục và kết quả vào biên bản; chỉ sau khi đạt mới đánh dấu P9-T03.2.

## Retention vận hành

Chốt chính sách dự kiến: `message_edits.previous_content` giữ 90 ngày;
outbox **đã published** giữ 30 ngày; outbox chưa published hoặc vượt retry
giữ đến khi operator xử lý. Upload staging chưa gắn message hết hạn sau 24 giờ
và worker dọn theo giờ. Attachment đã gắn message giữ cùng vòng đời message;
khi message/file bị xóa, cần quy trình xóa object an toàn sau thời gian phục
hồi 30 ngày. Hiện chưa có worker áp dụng chính sách 90/30 ngày cho edit,
published outbox và attachment đã gắn message. **Không tự chạy DELETE thủ công**
trước khi có job, backup và kiểm tra ràng buộc/audit; đây là launch gate còn mở.

## Smoke test qua Nginx

Kiểm tra trên bản Compose thật, không gọi trực tiếp cổng API: readiness 200;
đăng ký hai email mới, nhận thư xác minh, xác minh, đăng nhập; tạo DM, gửi
message và xem history sau reload; mở đồng thời hai browser và kiểm tra
SignalR `/hubs/chat` nhận event, ngắt/kết nối lại không nhân đôi message;
upload file sạch, tải qua URL được cấp quyền, xác nhận user ngoài conversation
không tải được; xem outbox được published và không có lỗi retry. Đổi thành
user mới hoàn toàn, không dùng account/record trong `seed.sql`.

Trên bản hiện tại, bước email không thể đạt: `RegistrationService` chỉ lưu hash
token, event Identity chỉ chứa `account_token_id`; không có raw token cho
consumer và không có email sender. `ExposeDevelopmentTokens` phải luôn là
`false` khi phát hành. Cần thiết kế luồng gửi email an toàn có retry cho verify
và reset password, cấu hình provider, rồi chạy lại toàn bộ smoke test.

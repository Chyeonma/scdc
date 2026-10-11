# SCDC — Thiết kế môi trường

## Phạm vi

Nguồn cho topology, thành phần và yêu cầu cấu hình. Các bước cài đặt/chạy/test nằm tại [hướng dẫn phát triển](../guides/development.md); thao tác phát hành tại [vận hành](../guides/operations.md).

## Development hiện tại

| Thành phần | Vai trò | Nguồn cấu hình |
|---|---|---|
| WebClient | React/Vite khi debug; Nginx phục vụ bản build trong container | [Vite](../../clients/WebClient/vite.config.js), [Nginx](../../clients/WebClient/nginx.conf), [Dockerfile](../../clients/WebClient/Dockerfile) |
| SCDC.Api | Host Identity, Community và Messaging | [Program](../../services/SCDC.Api/Program.cs), [appsettings](../../services/SCDC.Api/appsettings.json), [Dockerfile](../../services/SCDC.Api/Dockerfile) |
| PostgreSQL | Dữ liệu nghiệp vụ trong một database | [Compose](../../compose.yaml), [SQL bootstrap](../../database/postgres/schema.sql) |
| Volume DB | Giữ dữ liệu qua restart container | `scdc-postgres-data` trong Compose |
| Community keyring | Giữ khóa bảo vệ cursor qua restart | `scdc-community-keys` trong Compose |

WebClient proxy `/api`, `/hubs`, `/swagger` tới API. PostgreSQL được kiểm tra bằng `pg_isready` trước khi API khởi động. Thứ tự service khởi động không thay phép kiểm tra hành trình người dùng.

Danh sách địa chỉ, công cụ và giá trị local tại [Development](../guides/development.md#setup); danh sách khóa cấu hình tại [Configuration](../guides/development.md#configuration). Repo không lưu secret production trong tài liệu. Khi ghi hồ sơ chỉ ghi tên khóa, key ID và nguồn cấp quyền.

## Kiểm thử

Test tích hợp cần DB riêng có hậu tố `_test`, dữ liệu giả và quyền tạo/drop fixture DB. Bộ chạy Community tại [hướng dẫn Community](../guides/community-development.md). Không dùng dữ liệu local cần giữ làm fixture thử nghiệm.

## Staging và production

Chưa chọn host/domain, tài nguyên, email provider, secret store, backup tool, người trực hoặc manifest production. Compose hiện tại là Development. Các đầu vào còn mở tại [OQ-008/010/011](../project/open-questions.md#open-questions).

Thiết kế môi trường v1 phải xác định ingress/HTTPS, routing HTTP/realtime, DB và tài khoản sở hữu theo service, email worker, giám sát, backup/WAL và LiveKit/TURN. Kiến trúc service còn đề xuất tại [architecture](architecture.md#target). Không tự thêm Gateway, broker hoặc Kubernetes vào yêu cầu.

Điều kiện môi trường trước phát hành tại [gate nghiệm thu](../releases/acceptance.md#release-gates). Runbook chỉ trở thành hướng dẫn thực thi khi có manifest/lệnh đã diễn tập trên cấu hình được chọn.

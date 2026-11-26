# Community service

Community có bốn project ứng dụng và project hợp đồng gRPC riêng. API chung tiếp tục phục vụ Community bằng cùng nguồn controller và implementation; host riêng đang ở bước chuẩn bị, chưa nhận lưu lượng nghiệp vụ.

| Project | Vai trò |
|---|---|
| `SCDC.Community.Domain` | Model và bộ tính quyền thuần .NET |
| `SCDC.Community.Application` | Interface ca sử dụng, command và DTO; không tham chiếu SQL hoặc ASP.NET |
| `SCDC.Community.Infrastructure` | Implementation hiện hành, SQL/transaction, cursor, outbox, coordinator và đăng ký worker |
| `SCDC.Community.Api` | HTTP Community, Swagger, health và chế độ chuẩn bị |
| `SCDC.Community.Grpc` | Mã sinh hợp đồng, chưa cung cấp đầy đủ luồng Community–Messaging qua mạng |

Namespace cũ được giữ để tương thích. Controller và bộ lọc OpenAPI Community nằm trong project API mới; `SCDC.Api` biên dịch các file này bằng liên kết nguồn trong giai đoạn chuyển tiếp. Các adapter HTTP kỹ thuật dùng chung cũng được biên dịch vào từng host; host Community không tham chiếu assembly `SCDC.Api` hoặc implementation Messaging/Identity.

## Chạy preview riêng

Preview tạo một instance PostgreSQL và các volume thử nghiệm riêng. Nó không kết nối database dev, không dùng khóa Identity thật và không thay định tuyến gateway. Đích v1 vẫn là database Community riêng trên instance chung với Messaging.

Từ gốc repo, cần Python 3, OpenSSL và Docker Compose:

```bash
export SCDC_COMMUNITY_PREVIEW_SECRET_DIR="$HOME/.local/share/scdc/community-preview-secrets"
python3 scripts/init_community_preview_secrets.py "$SCDC_COMMUNITY_PREVIEW_SECRET_DIR" --label-for-containers
docker compose -f compose.community-preview.yaml --profile community-preview up --build -d
```

Với Podman, dùng `podman-compose` thay cho `docker compose` trong lệnh trên. Các service chỉ bật khi chọn profile `community-preview`. Script tạo mật khẩu admin/runtime độc lập, HMAC và public key JWT thử; chạy lại giữ các giá trị cũ. Mật khẩu admin chỉ mount vào PostgreSQL, API chỉ nhận mật khẩu runtime và không có private key JWT.

| URL | Kết quả ở bước 1 |
|---|---|
| `http://localhost:5082/health/live` | 200, mode `preparation`, worker chưa bật |
| `http://localhost:5082/health/database` | 200 nếu database Community riêng và role runtime hợp lệ |
| `http://localhost:5082/health/ready` | 503 `COMMUNITY_INTEGRATIONS_NOT_CONFIGURED` |
| `http://localhost:5082/swagger/community/` | Swagger Community trong Development |
| Các API nghiệp vụ Community | 503 trước khi xác thực, đọc hoặc ghi dữ liệu |

Readiness 503 là trạng thái chuẩn bị; liveness và database khỏe chưa đủ điều kiện nhận lưu lượng. Host chưa bật `CommunityAccessWorker`/`ChannelProvisionWorker`; các adapter Messaging đang thiếu luôn báo không sẵn sàng, không trả kết quả provision hoặc receipt thành công. Host chung vẫn đăng ký worker và các adapter trong process như trước.

Dừng preview, giữ dữ liệu thử:

```bash
docker compose -f compose.community-preview.yaml --profile community-preview down
```

## Kiểm tra và phần tiếp theo

`CommunityHostTests` kiểm tra host, database mới, quyền runtime, Swagger, chặn API và các adapter chưa nối. Bộ test Community/DM/Identity hiện hành kiểm tra hồi quy host chung. Build cả solution bằng `dotnet build SCDC.slnx --configuration Release`; cách chuẩn bị PostgreSQL/Redis cho test xem [hướng dẫn](../../docs/guides/development.md#community).

Baseline database thử nằm trong [database/postgres/community](../../database/postgres/community/README.md). Bước 2 nối Identity, provision và fence quyền qua mạng; bước 3 mới chuyển dữ liệu dev và gateway theo [checklist tách service](../../docs/guides/operations.md#service-extraction). S3-10 còn mở tới khi có bằng chứng chuyển và nghiệm thu.

Nguồn: [task S3-10](../../docs/project/tasks/step-3.md), [cấu trúc backend](../../docs/system/backend.md#v1-code-layout), D12, D40, D43, D47.

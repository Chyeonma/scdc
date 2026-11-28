# Identity

Identity chạy API tài khoản, phiên, permit, worker thu hồi và worker email trong host riêng.
Các endpoint HTTP vẫn là `/api/v1/auth` và `/api/v1/users` qua gateway.

| Project | Trách nhiệm |
|---|---|
| SCDC.Identity.Api | HTTP, gRPC server, DI và telemetry |
| SCDC.Identity.Application | Use case, DTO và interface của tài khoản |
| SCDC.Identity.Domain | Kiểu dữ liệu tài khoản và phiên, thuần .NET |
| SCDC.Identity.Infrastructure | PostgreSQL, Redis, JWT, worker và gRPC client |
| SCDC.Identity.Grpc | Mã sinh từ hợp đồng Identity, không có nghiệp vụ |

Namespace `SCDC.Modules.Identity` được giữ để tránh đổi hàng loạt tên kiểu;
assembly và project đã thuộc service Identity. Domain/Application không kéo
ASP.NET, Redis, SQL hoặc Protobuf. Framework adapter nằm trong Infrastructure
của từng owner; Shared.Core chỉ chứa kiểu thuần .NET.

HTTP công khai nghe cổng 8080; gRPC nội bộ nghe HTTP/2 TLS 8443 và bắt buộc chứng
chỉ client từ CA cấu hình, danh tính peer và quyền owner. Consumer không nhận
JWT private key hoặc quyền ghi phiên Redis. Deadline RPC là hai giây, không tự
retry request; coordinator xử lý lại fence bằng batch bền và idempotent.

Readiness riêng: `/api/v1/health/identity/access`. Swagger Development:
`/swagger/identity`. [Hướng dẫn phát triển](../../docs/guides/development.md),
[checklist tách service](../../docs/guides/operations.md#service-extraction),
[task STEP-3](../../docs/project/tasks/step-3.md).

S3-03 dùng [PostgreSQL Identity riêng](../../database/postgres/identity/README.md).
Source runtime cũ giữ `NOLOGIN`; startup gate yêu cầu transfer record khớp nguồn
hoặc marker volume mới. Worker và EF/Npgsql đều đọc cùng runtime password file;
API không có admin connection.

<a id="email-worker"></a>

## Email worker — E4-01

Đăng ký, quên mật khẩu và `POST /api/v1/auth/resend-verification` ghi token,
policy, delivery mã hóa và outbox cùng transaction. Worker dùng lease, kiểm
tra token trước khi gửi, gọi Gmail SMTP ngoài transaction và thử lại tối đa
5 lần. `ProviderAccepted` chỉ xác nhận SMTP đã chấp nhận thư. Reset mật khẩu
vẫn dùng coordinator thu hồi bền hiện hành.

Compose mặc định tắt gửi SMTP (`Modules:Identity:Email:Enabled=false`);
worker vẫn dọn envelope không còn hiệu lực. Key ring email dùng volume riêng
`scdc-identity-email-keys`. Ngoài Development cần `KeyCertificatePath`, có thể
cấp password certificate bằng `KeyCertificatePasswordFile`.

Lần nâng cấp đầu phải dừng và drain Identity. Job `identity-email-migration`
chạy sau cutover gate và trước host, kiểm checksum và từ chối writer cũ còn
kết nối. [Migration owner](../../database/postgres/identity/README.md#email-upgrade)
cũng chạy được bằng DbMigrator với credential admin. Runtime không có DDL.

```bash
make build
make migrate
make up
```

Gmail dev/demo dùng overlay `ops/compose/compose.email.yaml`. Cấp App Password qua file
`gmail_app_password` trong `SCDC_EMAIL_SECRET_DIR` (mặc định ngoài repo tại
`~/.local/share/scdc/email-secrets`), chỉ Identity được mount. Đặt
`SCDC_EMAIL_SENDER` và `SCDC_PUBLIC_ORIGIN`, rồi chạy:

```bash
make up COMPOSE_ARGS="-f ops/compose/compose.email.yaml"
```

Overlay tắt token Development. Host ngoài Compose dùng
`Modules:Identity:Email:AppPasswordFile`. Không đưa App Password vào repo,
CLI argv hoặc log. S3-04 tiếp tục kiểm chứng Gmail thật; giao diện mở link
xác minh/đặt lại chưa được nối trong E4-01.
[Hồ sơ kiểm chứng](../../docs/records/verification/authentication/e4-01-2026-11-26.md).

# Identity

Identity chạy API tài khoản, phiên, permit và worker thu hồi trong host riêng.
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

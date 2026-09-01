# SCDC

## Web client

Web client độc lập nằm tại `clients/WebClient`, sử dụng React, Vite và SignalR.
Chạy toàn bộ hệ thống bằng Docker hoặc Podman Compose:

```bash
podman compose up -d --build
```

Sau khi các container khởi động:

- Web client: `http://localhost:3000`.
- ChatService API: `http://localhost:5026`.
- Swagger UI: `http://localhost:5026/swagger`.

Trên web client, đăng ký hoặc đăng nhập, sau đó chọn **+** và nhập username của
người muốn trò chuyện. Client tạo một phòng có channel `general` và thêm user đó
qua membership API. Tin nhắn được đồng bộ realtime bằng SignalR, với polling định
kỳ làm phương án dự phòng.

Chạy frontend ở chế độ phát triển có hot reload:

```bash
cd clients/WebClient
npm install
npm run dev
```

Vite dev server chạy tại `http://localhost:3000` và proxy API/SignalR sang
ChatService ở `http://localhost:5026`.

## Kiến trúc

ChatService được tách thành bốn project theo Layered Architecture:

```text
ChatService.Domain          Domain model, không phụ thuộc project khác
        ↑
ChatService.Application     DTO, application service và các port/interface
        ↑
ChatService.Infrastructure  EF Core, repository, JWT, password và outbox worker
        ↑
ChatService                 API, controller, middleware và SignalR adapter
```

Chiều tham chiếu thực tế:

- `Application -> Domain`.
- `Infrastructure -> Application + Domain`.
- `ChatService -> Application + Infrastructure`.
- Domain không tham chiếu Application, Infrastructure hoặc API.

Build toàn bộ solution:

```bash
dotnet build SCDC.slnx
```

## Chạy ChatService local

Yêu cầu: .NET 10 SDK và Docker/Podman có PostgreSQL 18.

```bash
docker compose up -d postgres
dotnet tool restore
dotnet run --project services/ChatService/ChatService.csproj --launch-profile http
```

Ở môi trường Development, migration được áp dụng khi service khởi động. API chạy tại
`http://localhost:5026`, Swagger UI tại `/swagger`, OpenAPI JSON tại
`/swagger/v1/swagger.json`, SignalR Hub tại `/hubs/chat`.

Để thử endpoint cần xác thực trong Swagger, gọi `POST /api/v1/auth/register` hoặc
`POST /api/v1/auth/login`, sao chép `accessToken`, chọn **Authorize** và nhập token.
Swagger tự thêm prefix `Bearer` và lưu token trong phiên làm việc của trình duyệt.

## Chạy bằng Docker Compose

Build API image và chạy cả ChatService lẫn PostgreSQL:

```bash
docker compose up --build
```

API được publish tại `http://localhost:5026`. Docker image dùng multi-stage build,
chỉ chứa ASP.NET runtime và chạy bằng non-root user của image .NET. Dockerfile
của backend nằm tại `services/ChatService/Dockerfile`; build context vẫn là thư
mục gốc để truy cập đủ bốn project của ChatService.

## Kết nối PostgreSQL bằng DBeaver

Khi PostgreSQL đang chạy qua Compose, tạo một connection mới trong DBeaver với
driver **PostgreSQL** và các thông số dành cho máy host:

| Thuộc tính | Giá trị |
|---|---|
| Host | `localhost` |
| Port | `5432` |
| Database | `scdc_chat` |
| Username | `scdc` |
| Password | `scdc_dev` |
| Schema mặc định | `public` |
| SSL mode | `disable` |

JDBC URL tương ứng:

```text
jdbc:postgresql://localhost:5432/scdc_chat
```

Các thông tin này chỉ dành cho môi trường development. Nếu DBeaver cũng chạy
trong cùng mạng Compose thay vì chạy trên máy host, dùng hostname `postgres`.

Khi tạo migration mới:

```bash
dotnet ef migrations add <MigrationName> \
  --project services/ChatService.Infrastructure/ChatService.Infrastructure.csproj \
  --startup-project services/ChatService.Infrastructure/ChatService.Infrastructure.csproj \
  --output-dir Data/Migrations
```

Không sử dụng signing key và mật khẩu PostgreSQL trong `appsettings.Development.json`
cho production. Production phải cấp `ConnectionStrings__ChatDatabase` và
`Jwt__SigningKey` qua secret manager hoặc environment variables.

## Tài liệu

- [ChatService Minimum API](services/ChatService/docs/minimum-api.md)

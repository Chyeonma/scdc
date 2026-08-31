# SCDC

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
`http://localhost:5026`, OpenAPI tại `/openapi/v1.json`, SignalR Hub tại `/hubs/chat`.

## Chạy bằng Docker Compose

Build API image và chạy cả ChatService lẫn PostgreSQL:

```bash
docker compose up --build
```

API được publish tại `http://localhost:5026`. Docker image dùng multi-stage build,
chỉ chứa ASP.NET runtime và chạy bằng non-root user của image .NET.

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

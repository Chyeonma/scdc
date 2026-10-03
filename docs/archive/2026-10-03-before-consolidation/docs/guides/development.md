# Hướng dẫn phát triển

> Tài liệu gốc: [docs/development.md](../../development.md)

## Khởi chạy nhanh

```bash
# 1. Khởi động CSDL
docker compose up -d postgres

# 2. Build solution
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --no-restore

# 3. Chạy Backend API
dotnet run --project services/SCDC.Api/SCDC.Api.csproj --launch-profile http
```

API lắng nghe tại `http://localhost:5026`. Swagger: `http://localhost:5026/swagger`.

## Quy ước code C# (.NET 10)

- Mã nguồn nghiệp vụ: `services/Modules/<TênModule>`
- Các module **không tham chiếu trực tiếp nhau**, giao tiếp qua `SCDC.Contracts`
- Mỗi module có `DbContext` riêng, chỉ thao tác với schema PostgreSQL tương ứng
- **Không dùng** `throw Exception` cho lỗi nghiệp vụ → dùng `Result<T>` tại `SCDC.BuildingBlocks`
- Lỗi trả về client theo chuẩn **RFC 7807 ProblemDetails**

## Quy trình Git

- Nhánh chính: `main`
- Tính năng: `feature/<module>-<chức-năng>`
- Sửa lỗi: `fix/<tên-lỗi>`
- Commit: `<loại>(<phạm vi>): <mô tả>` (feat, fix, refactor, perf, test, docs)

## Kiểm thử

```bash
dotnet test SCDC.slnx --configuration Release
```

## Kết nối CSDL

```
Host: localhost | Port: 5432 | Database: scdc_chat
User: scdc | Password: scdc_dev
Schemas: identity, community, messaging, moderation, audit, integration, common
```

---

📎 Chi tiết: [Hướng dẫn gốc](../../development.md)

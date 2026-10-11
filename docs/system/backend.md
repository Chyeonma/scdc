# SCDC — Tổ chức backend

## Cấu trúc

Backend dùng .NET 10 trong [SCDC.slnx](../../SCDC.slnx). MVP có một API host; ranh giới service mục tiêu tại [kiến trúc](architecture.md#target).

```text
services/
├── SCDC.Api/               # Bootstrap, controllers, HTTP/auth/error/OpenAPI
├── SCDC.Contracts/         # Interface và kiểu giao tiếp liên module
├── SCDC.BuildingBlocks/    # Result, text policy, transaction scope, outbox
└── Modules/
    ├── Identity/
    ├── Community/
    └── Messaging/
tests/SCDC.Api.Tests/       # Test backend theo thành phần và tính năng
tools/SCDC.DbMigrator/      # Nâng cấp schema explicit
```

## Trách nhiệm

| Phần | Nội dung |
|---|---|
| Domain | Trạng thái, quy tắc và bất biến nghiệp vụ |
| Application | Điều phối use case, validation và hợp đồng persistence |
| Infrastructure | SQL/EF Core/Npgsql, bảo mật, khóa, paging và tích hợp |
| API | Đọc actor từ phiên; bind input; gọi Application; ánh xạ Result ra HTTP |
| Module bootstrap | Đăng ký DI/configuration và các interface công khai |

Identity hiện tổ chức theo Domain/Application/Infrastructure ở cấp module. Community tổ chức nghiệp vụ theo feature, dùng chung infrastructure; [thiết kế Community](community.md) giữ cấu trúc chi tiết. Messaging hiện có nền module/lifecycle; phần tin và Hub còn được mô tả trong [Messaging](../features/messaging/README.md). Chỉ tạo folder/class khi gói cần sử dụng.

## Quy tắc phụ thuộc

- Domain không phụ thuộc HTTP, UI hoặc provider.
- Module chỉ sử dụng Contracts của module khác; không tham chiếu implementation hoặc truy vấn bảng do module khác sở hữu.
- Lỗi dự kiến dùng `Result`/`Result<T>`; exception dành cho lỗi bất thường. [Quy ước HTTP](api-conventions.md) giữ cách ánh xạ lỗi.
- Gói xác định transaction, guard, lock order và retry trước khi viết mutation. Shared scope thuộc nền MVP một host; không áp nguyên trạng xuyên microservice.
- Kiểm tra quyền ở server. Guard phải giữ hiệu lực theo hợp đồng tới commit khi nghiệp vụ yêu cầu.

## Thiết kế theo chủ đề

Mỗi chủ đề ghi luồng API → Application → Domain/Persistence, dữ liệu sở hữu, contract liên module, phạm vi transaction, lỗi và các tình huống đồng thời. Tài liệu này chỉ giữ quy ước chung; không chép lại thuật toán của từng chức năng.

Nguồn triển khai: [Program](../../services/SCDC.Api/Program.cs), [CommunityModule](../../services/Modules/Community/CommunityModule.cs), [BuildingBlocks](../../services/SCDC.BuildingBlocks). Các bước chuẩn bị SQL/config/skeleton và kiểm chứng tại [workflow](../project/workflow.md#process).

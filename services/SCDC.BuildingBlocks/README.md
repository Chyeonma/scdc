# BuildingBlocks — phân loại hạ tầng legacy

Thư mục này giữ hạ tầng của host Messaging cũ trong lúc làm S3-07/S3-08.
Identity và Community không tham chiếu project này. Công cụ DbMigrator chỉ
tham chiếu Core và Npgsql. Quyết định về thư viện nhỏ dùng chung nằm tại
[D42/D45](../../docs/system/backend.md#v1-shared-code).

**Trạng thái 2026-11-28:** đã phân loại các file hiện có; Community có adapter
theo owner, Messaging còn dùng BuildingBlocks cho tới lần tách tiếp theo.
Không chuyển nguyên project này sang Shared.Core.

| Phần | Nội dung và nơi sở hữu |
|---|---|
| `Infrastructure/ModuleDatabaseConnection.cs` | Chọn connection/pool trong host legacy. Community có bản chỉ đọc `ConnectionStrings:Community`; Messaging tiếp tục dùng bản legacy. |
| `Infrastructure/DatabaseAvailability.cs` | Ánh xạ lỗi Npgsql tại adapter của host; không thuộc Domain/Core. |
| `Infrastructure/Persistence/RelationalWorkScope.cs` | Transaction cục bộ. Community có scope riêng trong Infrastructure; không truyền scope qua hợp đồng mạng. |
| `Infrastructure/Outbox/TransactionalOutbox.cs` | SQL ghi outbox. Community có bản cố định bảng `community.outbox_events`; bản nhiều owner chỉ còn hỗ trợ legacy/test. |
| `Infrastructure/Authentication/PostgresIdentityFenceOwner.cs` | SQL ghi Identity fence tại consumer. Community có bản cố định owner/schema Community; Messaging dùng bản legacy với owner Messaging. |
| `Infrastructure/Authentication/PostgresCommunityFenceOwner.cs` | SQL ghi Community fence tại consumer. Chính sách scope và schema thuộc consumer; Community không dùng lớp nhiều owner này. |
| `Infrastructure/Authentication/RedisSessionReader.cs` | Adapter đọc giao thức phiên do Identity công bố. Mỗi consumer giữ adapter và cấu hình của mình; khi đổi giao thức phải kiểm producer/consumer và thu hồi thật. |
| `Infrastructure/Transport/RpcTransport.cs` | TLS, gRPC và peer interceptor cần framework; nằm tại Api/Infrastructure của owner, không đưa vào Core. |
| `Infrastructure/Transport/ServiceRpc.cs` | Chuyển lỗi/cancellation gRPC sang hợp đồng công khai tại adapter của owner. |
| `Infrastructure/Transport/IdentityWire.cs` | Chuyển đổi kiểu Identity Protobuf và DTO; thuộc adapter gọi/nhận Identity. |
| `Infrastructure/Transport/CommunityWire.cs` | Chuyển đổi Community/Messaging Protobuf; thuộc adapter tích hợp, không thuộc Domain/Application. |
| `Infrastructure/Transport/RemoteIdentity.cs` | Client permit/lookup Identity. Community có client riêng, không đăng ký kho cursor Messaging. |

Các kiểu `Result`, `Error`, `IModuleDescriptor` và `UnicodeTextPolicy` mang
namespace `SCDC.BuildingBlocks.Application.*` nhưng source đã ở
[SCDC.Shared.Core](../Shared/SCDC.Shared.Core). Namespace không quyết định
assembly phụ thuộc. Core không tham chiếu PostgreSQL, Redis, ASP.NET hoặc gRPC.

[SCDC.Contracts](../SCDC.Contracts) chứa interface/DTO công khai. Các project
`.Grpc` chứa mã sinh từ Protobuf. Dùng `ProjectReference` tới hợp đồng trong
monorepo được giữ theo D44/D47; không cho phép tham chiếu implementation của
service khác hoặc liên kết source từ host khác.

Khi tách Messaging, chuyển SQL, transaction, outbox và adapter của nó về owner;
chỉ cân nhắc dùng chung các khối kỹ thuật nhỏ không chứa schema, model lưu trữ
hay chính sách quyền. Chạy lại test DM, thu hồi, timeout, replay và restart;
việc di chuyển file không thay cho kết quả kiểm chứng hành vi.

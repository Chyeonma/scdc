# Module Community

Đặc tả nghiệp vụ, dữ liệu/API, UX và kiểm thử được quản lý tại
[community.md](../../../docs/features/community.md).
Trang này tổng hợp hành trình sử dụng; [bảng UC](../../../docs/features/community.md#use-cases)
ghi module phụ trách từng thao tác. Community sở hữu cộng đồng, membership,
phòng, lời mời và quyền; Messaging sở hữu tin phòng/Hub chat.
Tổ chức nghiệp vụ đã thống nhất gồm năm phần:

- [Servers](../../../docs/features/community/servers.md): metadata, search, visibility, join mode và owner.
- [Memberships](../../../docs/features/community/memberships.md): membership/epoch, yêu cầu tham gia và leave/rejoin.
- [Invitations](../../../docs/features/community/invitations.md): link mời và lời mời đích danh.
- [Channels](../../../docs/features/community/channels.md): metadata/kind và vòng đời phòng.
- [Permissions](../../../docs/features/community/permissions.md): role, assignment, ACL và evaluator/checker/guard.

[Quy ước chung và tích hợp liên module](../../../docs/features/community/integration.md) quản lý transaction,
ID/version/retry, migration và phối hợp tin phòng/realtime;
[nguồn chuẩn](../../../docs/features/community/integration.md#responsibilities) chỉ rõ phần Messaging/Identity/WebClient.
Danh mục UC và bảng COM/AC/TC ở
[trang tổng quan](../../../docs/features/community.md#use-cases); bước triển khai tiếp theo ở
[gói đầu tiên](../../../docs/features/community.md#use-case-delivery).

Cấu trúc code mục tiêu ở [tổ chức nội bộ](../../../docs/features/community.md#organization):
`Features/<Phần>/{Domain,Application}` và Infrastructure dùng chung,
Permissions có Infrastructure cho checker/guard. Các phần cùng project/schema/DbContext khi triển khai;
`CommunityModule.cs` ghép DI. Nhánh `feat/community-create-view` triển khai gói đầu:
model server/membership/@everyone, CommunityDbContext, shared transaction và Identity guard,
API tạo/xem, operation HMAC, cursor Data Protection và transactional outbox.
Chức năng phòng, lời mời, tham gia/rời, quản lý vai trò và phát realtime thuộc các gói tiếp theo.

Ranh giới module và trạng thái source nằm trong [kiến trúc](../../../docs/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/development.md).

Module sở hữu schema `community`; giao tiếp liên module qua `SCDC.Contracts`.

## API gói đầu

Các route yêu cầu JWT từ Identity và tài khoản active có primary email đã xác minh:

| Route | Kết quả |
|---|---|
| `POST /api/v1/servers` | UUIDv4 `clientOperationId`, `name`, `description?`, `visibility?`; 201 khi tạo, 200 khi retry cùng dữ liệu chuẩn hóa, 409 nếu đổi dữ liệu |
| `GET /api/v1/servers` | Danh sách membership active của mình; `limit` 1–50, mặc định 20; `cursor` bảo vệ, hạn 24 giờ |
| `GET /api/v1/servers/{id}` | Detail cho member active; summary cho người ngoài public; private/inactive/unknown trả 404 |
| `GET /api/v1/servers/{id}/membership/me` | Record active/left của chính actor, kể cả own-left trong private; không phục hồi membership |

Create commit server, owner membership, @everyone, operation và một event `Community.ServerCreated.v1` cùng transaction. Event giữ `published_at=NULL`; gói chưa có dispatcher hoặc Hub. Chi tiết lỗi và DTO trong [thiết kế gói](../../../docs/features/community/create-view-design.md).

## Cấu hình

Startup yêu cầu cấu hình explicit; source không chứa HMAC key mặc định:

| Environment variable | Giá trị |
|---|---|
| `Modules__Community__Operations__ActiveKeyId` | Ví dụ `local1` |
| `Modules__Community__Operations__Keys__local1` | Base64 của ít nhất 32 byte ngẫu nhiên, lưu bằng secret configuration |
| `Modules__Community__KeyRingPath` | Đường dẫn tuyệt đối tới thư mục key ring bền, process API có quyền đọc/ghi |

Ví dụ API chạy local: đặt các biến trên, dùng `KeyRingPath` ở `$PWD/.docker-data/community-keys`, rồi chạy `dotnet run --project services/SCDC.Api`. Compose lấy HMAC key từ `COMMUNITY_OPERATION_KEY` trong `.env` và giữ key ring ở volume riêng. Giữ nguyên môi trường vì ApplicationName là `SCDC.Community.{EnvironmentName}`. Khi xoay HMAC key, thêm key mới và đổi active ID; giữ key cũ cho operation còn lưu. Mất key cũ trả 503 khi đối soát.

## Migration

`schema.sql`/`seed.sql` chỉ bootstrap DB mới. DB cần giữ dữ liệu dùng runner explicit; API không tự migrate:

```bash
# Đặt ConnectionStrings__Database tới DB cần nâng cấp trước khi chạy.
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/001-community-create-view.sql \
  /absolute/path/reviewed-server-map.json
```

Mapping JSON theo ID server, ví dụ:

```json
{
  "01990000-0000-7200-8000-000000000001": {
    "visibility": "private",
    "joinMode": "immediate"
  }
}
```

DB không có server thì bỏ argument mapping. Preflight từ chối dữ liệu legacy chưa hợp lệ (owner membership, tên, trạng thái, system/default role hoặc quyền); cần rà soát và sửa riêng trước khi chạy lại. Runner không tự đổi quyền, tên hay visibility. Ledger `common.schema_migrations` lưu checksum; chạy lại cùng bản là no-op. Giữ migration đã áp nguyên vẹn, giữ backup DB/keys khi nâng cấp.

## Kiểm thử

Integration tests Community cần PostgreSQL 18 riêng, đã bootstrap/migrate, với tên DB kết thúc `_test`, quyền tạo/drop các DB fixture. Đặt `ConnectionStrings__Database`, `Modules__Identity__SigningKey` và `Modules__Identity__ExposeDevelopmentTokens=true`, rồi chạy:

```bash
dotnet test tests/SCDC.Api.Tests --configuration Release
```

Factory dùng khóa tổng hợp và key ring tạm riêng. Kiểm thử bao gồm migration legacy/bootstrap, race qua hai transaction, fault sau insert, điều kiện Identity/expiry, fingerprint fixture, privacy, cursor và retry qua restart/rotation. Đây là kiểm chứng backend; UI và tải thực tế được nghiệm thu ở bước tiếp theo.

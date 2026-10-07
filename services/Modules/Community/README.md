# Module Community

Đặc tả nghiệp vụ, dữ liệu/API, UX và kiểm thử được quản lý tại
[Community](../../../docs/features/community/README.md).
Trang này tổng hợp hành trình sử dụng; [bảng UC](../../../docs/features/community/specs/README.md#use-cases)
ghi module phụ trách từng thao tác. Community sở hữu cộng đồng, membership,
phòng, lời mời và quyền; Messaging sở hữu tin phòng/Hub chat.
Tổ chức nghiệp vụ đã thống nhất gồm năm phần:

- [Servers](../../../docs/features/community/specs/servers.md): metadata, search, visibility, join mode và owner.
- [Memberships](../../../docs/features/community/specs/memberships.md): membership/epoch, yêu cầu tham gia và leave/rejoin.
- [Invitations](../../../docs/features/community/specs/invitations.md): link mời và lời mời đích danh.
- [Channels](../../../docs/features/community/specs/channels.md): metadata/kind và vòng đời phòng.
- [Permissions](../../../docs/features/community/specs/permissions.md): role, assignment, ACL và evaluator/checker/guard.

[Thiết kế tích hợp liên module](../../../docs/features/community/design/integration.md) quản lý transaction,
ID/version/retry, migration và phối hợp tin phòng/realtime;
[nguồn chuẩn](../../../docs/features/community/specs/integration.md#responsibilities) chỉ rõ phần Messaging/Identity/WebClient.
Danh mục UC và bảng COM/AC/TC ở
[trang tổng quan](../../../docs/features/community/specs/README.md#use-cases); bước triển khai tiếp theo ở
[gói đầu tiên](../../../docs/features/community/delivery/README.md#use-case-delivery).

Cấu trúc code mục tiêu ở [tổ chức nội bộ](../../../docs/features/community/design/README.md#organization):
`Features/<Phần>/{Domain,Application}` và Infrastructure dùng chung,
Permissions có Infrastructure cho checker/guard. Các phần cùng project/schema/DbContext khi triển khai;
`CommunityModule.cs` ghép DI. Phần implementation theo nhánh và kết quả kiểm chứng được quản lý tại [status.md](../../../docs/features/community/status.md).

Ranh giới module nằm trong [kiến trúc](../../../docs/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/development.md).

Module sở hữu schema `community`; giao tiếp liên module qua `SCDC.Contracts`.

## API gói đầu

Các route yêu cầu JWT từ Identity và tài khoản active có primary email đã xác minh:

| Route | Kết quả |
|---|---|
| `POST /api/v1/servers` | UUIDv4 `clientOperationId`, `name`, `description?`, `visibility?`; 201 khi tạo, 200 khi retry cùng dữ liệu chuẩn hóa, 409 nếu đổi dữ liệu |
| `GET /api/v1/servers` | Danh sách membership active của mình; `limit` 1–50, mặc định 20; `cursor` bảo vệ, hạn 24 giờ |
| `GET /api/v1/servers/search` | `q` 2–100 UTF-16; tìm một phần tên công khai/active, literal/case-insensitive/accent-sensitive; summary, exact-first, keyset `limit` 1–50, mặc định 20 |
| `GET /api/v1/servers/{id}` | Detail cho member active; summary cho người ngoài public; private/inactive/unknown trả 404 |
| `GET /api/v1/servers/{id}/membership/me` | Record active/left của chính actor, kể cả own-left trong private; không phục hồi membership |
| `POST /api/v1/servers/{id}/join` | Không body; public/immediate tạo hoặc kích hoạt membership mặc định; join lặp active trả cùng epoch; approval với người chưa active nhận 409 |

Create commit server, owner membership, @everyone, operation và một event `Community.ServerCreated.v1` cùng transaction. Event giữ `published_at=NULL`; gói chưa có dispatcher hoặc Hub. Chi tiết lỗi và DTO trong [thiết kế gói](../../../docs/features/community/design/create-view.md).

Nhánh `feat/community-join` kế thừa tạo/xem và bổ sung [tham gia trực tiếp](../../../docs/features/community/design/direct-join.md). Join giữ Identity guard và server write lock tới commit, tăng accessVersion và ghi một `Community.MembershipJoined.v1`. Rejoin từ record left tạo epoch mới và dọn role/override cũ; chưa có endpoint leave hoặc requests chờ duyệt. Không thêm migration ngoài baseline 001; không tự phát lại mutation sau lỗi.

Nhánh `feat/community-search` kế thừa join và bổ sung [tìm kiếm](../../../docs/features/community/design/search.md). Mỗi trang lọc lại public/active/nondeleted; member cũng chỉ nhận summary. Cursor purpose riêng, gắn actor/query đã chuẩn hóa/limit và hạn 24 giờ. Chỉ tìm/xem không tạo membership hoặc event.

## Vai trò và quyền quản lý

Nhánh `feat/community-permissions` kế thừa search và bổ sung [role/assignment](../../../docs/features/community/design/roles.md):

| Route | Quyền và kết quả |
|---|---|
| `GET /api/v1/servers/{id}/roles` | Owner hoặc `manage_channel_access`; catalog có @everyone, keyset `limit` 1–50, mặc định 20 |
| `POST /api/v1/servers/{id}/roles` | Owner; operation UUIDv4, tên và tập management permissions; 201 tạo, 200 replay cùng dữ liệu |
| `PATCH /api/v1/servers/{id}/roles/{roleId}` | Owner; tên/quyền và `expectedVersion`; no-op giữ version |
| `DELETE /api/v1/servers/{id}/roles/{roleId}?expectedVersion=…` | Owner; CAS, cascade grant/role override; legacy invite còn tham chiếu trả 409 |
| `GET /api/v1/servers/{id}/members` | Owner hoặc `manage_channel_access`; roster active phân trang, UserSummary qua Identity, không email |
| `GET /api/v1/servers/{id}/members/{userId}/roles` | Owner; custom role IDs cùng membership epoch/version |
| `PUT /api/v1/servers/{id}/members/{userId}/roles` | Owner; `membershipId`, `expectedVersion`, toàn bộ tập `roleIds` |

@everyone tự áp và được bảo vệ; tối đa 20 custom role. Tên 1–64 UTF-16, unique theo trim/NFC/ToLowerInvariant, phân biệt dấu. Catalog gồm `manage_channels`, `manage_invites`, `review_join_requests`, `manage_join_mode`, `manage_channel_access`; quyền quản lý là hợp của các grant custom hiện hành, không có hierarchy hoặc management DENY.

Guard giữ Identity lease và server lock tới commit, kiểm tra owner/quyền hiện hành. Mutation tăng accessVersion và ghi `Community.AccessChanged.v1` cùng transaction; no-op không bump/event. Assignment và user override gắn epoch bằng FK; delete role tăng version membership bị ảnh hưởng. Event chưa được dispatcher phát. Evaluator domain có 18 fixture; chưa có endpoint ACL/phòng, checker phòng hoặc bằng chứng thu hồi realtime ≤5 giây.

WebClient owner vào quản lý từ detail. Tạo role lưu operation/body trước POST để phục hồi sau reload; sửa/xóa/gán không tự replay và cần đọc lại hiện trạng sau lỗi không rõ kết quả hoặc conflict.

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

Sau baseline 001, dừng/drain writer cũ trước khi áp 002 rồi deploy writer mới:

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/002-community-search.sql
```

002 thêm `search_name` bắt buộc, backfill theo batch 500 bằng cùng policy trim/NFC/ToLowerInvariant của create/search; giữ tên hiển thị, version và membership. Dữ liệu invalid hoặc lỗi giữa migration rollback toàn bộ. Writer tạo server cũ không tương thích với cột mới. Writer sửa tên tương lai phải ghi key cùng transaction với tên.

Sau 001/002, dừng/drain writer cũ trước khi áp 003 rồi deploy writer mới:

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/003-community-roles.sql
```

003 backfill role key theo batch 500, thêm role version/FK epoch/catalog và operation `create_role`. Preflight chặn collision, tên chưa hợp lệ, quá 20 custom role hoặc quyền/override ngoài catalog; giữ dữ liệu để sửa có review, không tự đổi quyền. Bootstrap mới đã gồm 001/002/003; writer role/grant/override cũ không tương thích. Chỉ runner được áp trên DB cần giữ dữ liệu; `schema.sql` có DROP SCHEMA.

## Kiểm thử

Integration tests Community cần PostgreSQL 18 riêng, đã bootstrap/migrate, với tên DB kết thúc `_test`, quyền tạo/drop các DB fixture. Đặt `ConnectionStrings__Database`, `Modules__Identity__SigningKey` và `Modules__Identity__ExposeDevelopmentTokens=true`, rồi chạy:

```bash
dotnet test tests/SCDC.Api.Tests --configuration Release
```

Factory dùng khóa tổng hợp và key ring tạm riêng. Kiểm thử bao gồm migration legacy/bootstrap, batch vượt 500, race qua hai transaction, rollback outbox, điều kiện Identity/expiry, fingerprint/evaluator fixtures, privacy, cursor, epoch/CAS và retry qua restart/rotation. Kết quả backend/UI theo từng gói ở [hồ sơ giao hàng](../../../docs/features/community/delivery/README.md); tải và thu hồi realtime cần kiểm chứng riêng khi có runtime tương ứng.

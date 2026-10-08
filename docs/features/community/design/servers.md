# SCDC — Thiết kế Servers

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/servers.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../../contracts/community.openapi.json) chứa contract đầy đủ và metadata trạng thái từng operation. Phần API đã chạy được đối chiếu ở [status](../status.md) và hồ sơ nghiệm thu; phần ngoài các gói đó còn là mục tiêu. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

[Thiết kế gói tạo/xem](create-view.md) là đầu ra bước 3 ngày 2026-10-07: chốt bốn API, model/validation, Identity guard, migration/preflight và kiểm chứng cần cho UC-COM-01 và phần đọc của UC-COM-03. [Gói tìm kiếm](search.md) chốt key tên, migration 002, summary/keyset và discovery của UC-COM-02. Phạm vi và bằng chứng gói được dẫn chiếu từ [tiến độ](../status.md); chỉnh sửa/transfer tiếp tục theo các gói sau.

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers` | `{name,description?,visibility}` | 201 server; account active/verified; tạo server và membership owner cùng transaction |
| `PATCH /servers/{id}` | `{name?,description?,visibility?,expectedVersion}` | 200 server; chỉ owner; private không xuất hiện trong tìm kiếm |
| `GET /servers/search` | `q,cursor,limit` | 200 trang server công khai; q/khớp/phân trang theo DEC-093; không trả nội dung/phòng riêng tư |
| `PATCH /servers/{id}/join-mode` | `{joinMode,expectedVersion}` | 200 server; đúng quyền đổi chế độ, không vô hiệu nguyên tắc link cho vào ngay |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId,expectedVersion}` | 200 server; owner hiện tại, target là thành viên active/verified; chuyển ngay, không tạo pending |

### Giao dịch của thành phần

- Tạo server: Identity guard → server + owner membership + @everyone + operation + outbox một transaction; không có khoảng thời gian server thiếu owner. Hai create cùng clientOperationId chưa có server để khóa: unique operation làm một transaction thắng; bên thua rollback toàn bộ server/membership/role/outbox rồi đọc operation đã commit trong transaction mới, không chỉ bỏ lỗi insert operation và giữ server trùng.
- Public → private: update visibility và chuyển mọi pending request sang cancelled với reason server_private trong cùng transaction theo DEC-096; không xóa lịch sử request. Sender vẫn đọc được trạng thái của chính mình và nhận thông báo lý do. Existing members/invites giữ nguyên; join request cũ không được approve sau commit private.
- Chuyển owner: khóa actor/target theo thứ tự rồi server/membership; target còn active/verified và là member tại commit. Owner field là nguồn chuẩn duy nhất; target rời trước thì transfer thất bại, transfer trước thì target thành owner không được rời. Chủ cũ chỉ rời sau chuyển đã commit.

<a id="search"></a>

### Search

Search theo DEC-093 dùng key tên trim/NFC/ToLowerInvariant, exact trước rồi normalizedName/UUID để ổn định. Escape `%`, `_`, backslash cho LIKE literal; collation không bỏ dấu. Cursor Data Protection purpose `Community.Search.v1`, actor/query/limit/position/expiry 24 giờ; mỗi trang lọc lại visibility/status. Đổi public→private phải biến mất cả khi dùng cursor cũ, không dựa vào snapshot đã cấp quyền public trước đó.

Migration và proof search nằm ở [thiết kế gói](search.md) và [nghiệm thu](../delivery/search/acceptance.md). Các thay đổi writer/guard khác theo [COM-SQL-01–10](integration.md#schema-migration) vẫn cần bằng chứng theo gói riêng.

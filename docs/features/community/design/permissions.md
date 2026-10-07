# SCDC — Thiết kế Permissions

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/permissions.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../../contracts/community.openapi.json) giữ schema mục tiêu và đánh dấu từng route đã có trên feature branch. [Gói role/assignment](roles.md) chọn các route quản lý vai trò và roster; endpoint ACL/phòng vẫn là thiết kế mục tiêu. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

<a id="detailed-design"></a>

### Vai trò và cấu hình quyền

| Mã quyền kỹ thuật | Thao tác được cấp | Không tự cấp |
|---|---|---|
| `manage_channels` | Tạo/sửa/xóa phòng | Quyền xem tin của mọi phòng, đổi ACL hoặc quản lý role |
| `manage_invites` | Tạo/thu hồi link và gửi/hủy mời đích danh | Duyệt yêu cầu hoặc gán role |
| `review_join_requests` | Xem danh sách pending, duyệt/từ chối | Tạo mời hoặc quản lý role |
| `manage_join_mode` | Đổi vào ngay/chờ duyệt | Đổi visibility/name/description |
| `manage_channel_access` | Đọc/sửa cấu hình xem phòng và chọn thành viên cho ngoại lệ | Quyền đọc tin bị giới hạn hoặc quản lý role |

Owner có các quyền quản lý trên và các thao tác riêng [ACL-19](../specs/permissions.md#acl-19), [ACL-20](../specs/permissions.md#acl-20)/chuyển ownership sau kiểm tra nền; không có quyền sửa/xóa tin người khác. @everyone là role hệ thống duy nhất của server, tự áp cho mọi membership active, không cần insert member_roles cho từng người. Không đổi tên/xóa/gán tay role này hoặc cấp management permission. Role tự tạo có tập permission cho phép; effective management là hợp tập này, không phụ thuộc role position và không có hierarchy vượt owner. Giới hạn 20 role tự tạo kiểm tra dưới khóa server, không tính @everyone.

API đề xuất:

| Route bổ sung | Quyền và kết quả |
|---|---|
| `GET /servers/{id}/roles` | Owner/manage_channel_access đọc danh mục để chọn role; chỉ owner được thay role/assignment |
| `POST /servers/{id}/roles` | Owner; `{clientOperationId,name,permissions}`; 201 role |
| `PATCH /servers/{id}/roles/{roleId}` | Owner; `{name?,permissions?,expectedVersion}`; 200 role; system role không sửa |
| `DELETE /servers/{id}/roles/{roleId}?expectedVersion=...` | Owner; xóa custom role và assignment/override cùng transaction; 204 |
| `GET /servers/{id}/members` | Owner hoặc manage_channel_access; trang user summary/membership để chọn role/ngoại lệ, không có email |
| `GET /servers/{id}/members/{userId}/roles` | Owner; danh sách custom role, membershipId/version |
| `PUT /servers/{id}/members/{userId}/roles` | Owner; `{membershipId,expectedVersion,roleIds}` thay tập custom role đầy đủ, không gán role server khác |
| `GET /servers/{id}/channels/{channelId}/access` | Owner/manage_channel_access và còn quyền xem phòng; base + override/accessVersion, không trả tin |
| `PUT /servers/{id}/channels/{channelId}/access` | Cùng điều kiện; thay cấu hình xem có expectedAccessVersion; một lần cập nhật nguyên tử |

ACL snapshot có `defaultView: allow|deny`, `roleOverrides:[{roleId,effect:allow|deny}]`, `memberOverrides:[{userId,membershipId,effect:allow|deny}]`. Không có entry nghĩa inherit; không dùng số bit mask trên wire. @everyone tham gia bước role như mọi role khác; role/user duplicate hoặc thuộc server khác nhận 400. Snapshot chỉ nhận membership active đúng epoch. Owner luôn xem được theo DEC-056; UI không diễn giải override deny owner thành thu hồi quyền owner.

Xóa custom role bỏ role assignment/role override và tăng accessVersion; kết quả có thể mở hoặc đóng quyền xem tùy role đã allow/deny, phải tính lại thay vì giả mọi xóa role là thu hồi. Rời server xóa assignment/ngoại lệ của epoch hiện tại; rejoin chỉ @everyone theo DEC-087. Chuyển owner không tự gán role quản lý cho chủ cũ; quyền chủ cũ trở lại theo role hiện có.

<a id="implementation-boundary"></a>

### Evaluator, quản lý quyền và guard

Phân chia kỹ thuật đã thống nhất ngày 2026-10-06:

- Domain chứa permission catalog, role/ACL models và evaluator tính trên snapshot; evaluator không gọi Application service hoặc DB.
- Application chứa các use case quản lý role/assignment/ACL và phối hợp transaction, version/epoch.
- Infrastructure đọc snapshot hiện hành và triển khai checker/guard; guard giữ row lock theo [thiết kế transaction](integration.md#transactions), không thay bằng kết quả bool hoặc cache chưa xác nhận.

Các phần khác dùng kết quả policy/guard; điều kiện Identity qua Contracts. Đổi role/assignment/ACL cập nhật accessVersion và outbox cùng transaction với mutation. [Gói role/assignment](../delivery/roles/acceptance.md) có evaluator domain, management guard và writer role/assignment. Snapshot checker/guard phòng, ACL API và thu hồi realtime còn cần triển khai; không suy fixture evaluator thành API đã đạt.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

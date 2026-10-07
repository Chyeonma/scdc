# SCDC — Thiết kế Channels

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/channels.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../../contracts/community.openapi.json) là schema dự thảo, không phải API đang chạy. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers/{id}/channels` | `{name,topic?,kind}` | 201 phòng text/voice; đúng quyền quản lý phòng; mặc định mọi thành viên xem |
| `PATCH /servers/{id}/channels/{channelId}` | `{name?,topic?,expectedVersion}` | 200 phòng; đúng quyền quản lý phòng |
| `DELETE /servers/{id}/channels/{channelId}` | `expectedVersion` | 204 xóa logic, dừng truy cập/kết nối; không tự xóa vật lý tin/backup |
| `GET /servers/{id}/channels` | Phiên/thành viên | 200 chỉ phòng được xem; phòng deleted không được trả |

### Giao dịch của thành phần

- Xóa phòng: Community chuyển deleted/version/accessVersion, Messaging đánh dấu space deleted qua hợp đồng lifecycle, outbox thu hồi cùng transaction. Không có hai commit độc lập khiến phòng đã deleted vẫn nhận tin; không purge tin/backup ở đây.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

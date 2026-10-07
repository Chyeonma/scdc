# SCDC — Thiết kế Memberships

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/memberships.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

Phần public/immediate và rejoin fixture đã có [thiết kế gói riêng](direct-join.md). Route 202 pending và các transition request/leave bên dưới vẫn là mục tiêu; [tiến độ](../status.md) dẫn tới bằng chứng runtime.

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../../contracts/community.openapi.json) là schema dự thảo, không phải API đang chạy. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers/{id}/join` | Không có body | 200 membership nếu vào ngay; 202 yêu cầu pending nếu chờ duyệt; không join server private từ tìm kiếm |
| `GET /servers/{id}/join-requests/me` | Phiên người yêu cầu | 200 `{request: JoinRequest|null}`; chưa có trả null; không trả request người khác |
| `POST /servers/{id}/join-requests/{requestId}/approve` hoặc `/reject` | `{expectedVersion}` | 200 trạng thái cuối; đúng quyền duyệt; chỉ transition từ pending |
| `DELETE /servers/{id}/join-requests/{requestId}` | `expectedVersion` | 200 cancelled; chỉ chính người gửi hủy pending |
| `DELETE /servers/{id}/members/me?membershipId=...` | Epoch đang tham gia | 204 tự rời; owner chưa chuyển nhận 409; epoch cũ không làm rời epoch mới; chặn HTTP/realtime/media theo ngưỡng |

### Giao dịch của thành phần

- Join công khai: khóa server, xét visibility/joinMode hiện hành, rồi membership/request. Đã active trả membership hiện hành. Vào ngay tạo/reactivate membership và kết thúc pending cũ; chờ duyệt có tối đa một pending/server/user, gọi lặp trả cùng pending. Dùng [partial unique index](https://www.postgresql.org/docs/18/indexes-partial.html) cho pending; service vẫn khóa để transition nguyên tử. Join qua đường khác kết thúc request pending bằng approved/reason joined_elsewhere và membershipId hiện hành; UI hiển thị đã tham gia bằng đường khác, không giả reviewer đã duyệt.
- Approve/reject/cancel: kiểm tra actor/target và version; chỉ một transition pending thắng. Approve tạo membership cùng commit; đã joined bằng link/đường khác thì đóng pending với lý do joined_elsewhere, không tạo membership thứ hai. Request mới sau rejected/cancelled có ID mới, không sửa lại lịch sử request cũ.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

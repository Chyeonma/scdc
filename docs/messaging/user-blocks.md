# P8-T02 — User blocks

API (Bearer token, chỉ tài khoản Active):

| Method | Route | Kết quả |
|---|---|---|
| GET | `/api/v1/users/me/blocks` | Danh sách block do chính mình tạo, mới nhất trước; user không còn Active chỉ còn ID |
| PUT | `/api/v1/users/me/blocks/{userId}` | 204; lặp lại không tạo bản ghi thứ hai |
| DELETE | `/api/v1/users/me/blocks/{userId}` | 204; lặp lại vẫn thành công |

Không thể tự block. API không liệt kê ai block mình. Mở DM bị từ chối với
thông báo chung nếu có block ở một trong hai chiều. DM cũ vẫn đọc/history được,
nhưng gửi/sửa/typing và các capability tạo nội dung trả về đều bị chặn. Xóa
tin của mình và cập nhật mốc đọc cá nhân vẫn được phép. Group/channel không
đổi membership/quyền đọc/gửi; actor không thể thêm người mà họ block hoặc bị
người đó block, còn admin/owner khác có quyền vẫn thêm được. Mention giữa hai
người bị block không tạo bản ghi mention/badge/notification riêng; chữ `@` trong
tin chung vẫn hiển thị theo quyền space và không phát bù sau unblock. Chính
sách đầy đủ ở `policies.md` mục 6.

Các thao tác ghi phụ thuộc block (mở DM, gửi/sửa DM, tạo/thêm thành viên group,
block/unblock) dùng cùng PostgreSQL transaction advisory lock theo cặp user.
Khi block và gửi chạy đồng thời, thao tác lấy lock trước có hiệu lực trước;
tin đã commit trước block có thể còn được nhận. Realtime `SpaceUpdated` báo
cho hai bên DM tải lại capability mà không tiết lộ chiều block. Không có
migration: bảng `messaging.user_blocks` và index chiều ngược đã có trong
`schema.sql`.

Test integration cần PostgreSQL test ở `localhost:5433`. Chạy test block bằng:

```text
dotnet test tests/SCDC.Api.Tests/SCDC.Api.Tests.csproj --filter "FullyQualifiedName~Block_api_is_idempotent|FullyQualifiedName~Concurrent_block_and_send|FullyQualifiedName~Block_prevents_actor_from_inviting"
```

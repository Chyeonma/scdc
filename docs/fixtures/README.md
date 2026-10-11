# SCDC — Dữ liệu kiểm chứng

Fixture giữ đầu vào/kết quả mong đợi để đối chiếu implementation. Fixture không định nghĩa thêm quy tắc ngoài chủ đề và không là kết quả chạy sản phẩm.

## Danh mục

| Fixture | Chủ đề sở hữu |
|---|---|
| [Text validation](text-validation.json), [Text policy](text-policy.json) | [Nội dung văn bản](../features/messaging/text-policy.md) |
| [DM fingerprint](dm-fingerprint.json) | [Vòng đời tin](../features/messaging/message-lifecycle.md) |
| [Community permissions](community-permissions.json), [Community operations](community-operations.json) | [Quyền truy cập](../features/community/access-control.md), [Cơ chế Community](../system/community.md) |
| [Media lifecycle](media-lifecycle.json) | [Media](../features/media/README.md) |
| [Data lifecycle](data-lifecycle.json) | [Vòng đời dữ liệu](../system/data-lifecycle.md) |

Fixture dùng chung giữ một nguồn ở đây. Dữ liệu chỉ phục vụ một chủ đề có thể đặt cạnh chủ đề. Khi sửa quy tắc, cập nhật fixture và cách kiểm chứng liên quan; giữ nhãn dữ liệu giả/minh họa.

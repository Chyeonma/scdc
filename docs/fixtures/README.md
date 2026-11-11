# SCDC — Dữ liệu kiểm thử

Fixture là các file chứa dữ liệu đầu vào và kết quả mong đợi, dùng để kiểm tra code có làm đúng quy tắc hay không. Fixture không đặt thêm quy tắc mới, và cũng không phải kết quả chạy thật của sản phẩm.

## Danh mục

| Fixture | Quy tắc gốc nằm ở |
|---|---|
| [Text validation](text-validation.json), [Text policy](text-policy.json) | [Nội dung văn bản](../features/messaging/text-policy.md) |
| [DM fingerprint](dm-fingerprint.json) | [Vòng đời tin](../features/messaging/message-lifecycle.md) |
| [Community permissions](community-permissions.json), [Community operations](community-operations.json) | [Quyền truy cập](../features/community/access-control.md), [Cơ chế Community](../features/community/mechanics.md) |
| [Media lifecycle](media-lifecycle.json) | [Media](../features/media/README.md) |
| [Data lifecycle](data-lifecycle.json) | [Vòng đời dữ liệu](../system/data-lifecycle.md) |

## Quy ước

- Fixture dùng cho nhiều tính năng đặt ở thư mục này. Fixture chỉ một tính năng dùng có thể đặt cạnh tài liệu của tính năng đó.
- Khi sửa quy tắc, sửa luôn fixture và cách kiểm thử liên quan.
- Dữ liệu giả hoặc minh họa phải được ghi nhãn rõ.

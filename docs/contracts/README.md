# SCDC — Hợp đồng máy đọc

OpenAPI và JSON Schema mô tả cấu trúc dữ liệu trao đổi. Chủ đề sở hữu ý nghĩa, quyền, luồng, ngoại lệ và tiêu chí kiểm chứng; không chép lại toàn bộ schema vào Markdown.

## Danh mục

| Artefact | Chủ đề sở hữu | Trạng thái |
|---|---|---|
| [Account recovery](account-recovery.openapi.json) | [Liên kết email](../features/accounts/email-links.md), [Khôi phục mật khẩu](../features/accounts/password-recovery.md) | Contract mục tiêu; đối chiếu endpoint hiện có và resend chưa triển khai |
| [Direct Messaging](direct-messaging.openapi.json) | [DM](../features/messaging/direct-messaging.md) | Thiết kế dự thảo; chưa có DM API runtime |
| [Chat realtime](chat-realtime.schema.json) | [Đồng bộ tin](../features/messaging/synchronization.md) | Catalogue ứng dụng SignalR; không phải wire frame; chưa có Hub runtime |
| [Community](community.openapi.json) | [Community](../features/community/README.md), [Tin phòng](../features/messaging/channel-messaging.md) | Có endpoint đã triển khai và endpoint mục tiêu; xem trạng thái trong từng chủ đề |
| [Community realtime](community-realtime.schema.json) | [Tin phòng](../features/messaging/channel-messaging.md), [Community](../system/community.md) | Thiết kế mục tiêu; chưa có dispatcher/Hub tích hợp |
| [Media](media.openapi.json), [Media realtime](media-realtime.schema.json) | [Media](../features/media/README.md) | Thiết kế mục tiêu; chưa có backend/provider runtime |
| [Data protection](data-protection.schema.json) | [Vòng đời dữ liệu](../system/data-lifecycle.md#restore) | Thiết kế sổ bảo vệ; chưa có kho sổ/worker/proof restore |

## Quy ước

- API chưa triển khai dùng contract có nhãn dự thảo/mục tiêu; ghi phần cần review trước code.
- API hiện có được mô tả bởi Swagger sinh từ source. Khi triển khai hoặc thay đổi endpoint, đối chiếu contract, source, client và AC trong cùng thay đổi.
- Nếu một file chứa nhiều trạng thái, xác định trạng thái theo operation; không suy mọi endpoint đã hoạt động từ sự tồn tại của file OpenAPI.
- `$ref`, kiểu/field và ví dụ máy đọc giữ tại đây. Quyền và lỗi nghiệp vụ được giải thích tại chủ đề tương ứng.
- Cấu trúc hợp lệ và fixture đúng chỉ là kiểm tra artefact; kết quả sản phẩm cần build/môi trường và thực thi riêng.

Quy ước chung tại [API](../system/api-conventions.md). Dữ liệu biên tại [fixtures](../fixtures/README.md).

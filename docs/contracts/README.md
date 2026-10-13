# SCDC — Hợp đồng máy đọc

OpenAPI, JSON Schema và Protobuf mô tả cấu trúc dữ liệu trao đổi. Chủ đề sở hữu ý nghĩa, quyền, luồng, ngoại lệ và tiêu chí kiểm chứng; không chép lại toàn bộ schema vào Markdown.

## Danh mục

Runtime nền hợp nhất được ghi theo [snapshot MVP `900e226`](../releases/mvp.md) và [hồ sơ tích hợp ngày 2026-10-13](../records/verification/messaging/merge-2026-10-13.md). Có runtime chưa xác nhận toàn bộ contract mục tiêu v1 đã khớp; tiếp tục đối chiếu từng operation với Swagger/source và bằng chứng của gói.

| Artefact | Chủ đề sở hữu | Trạng thái |
|---|---|---|
| gRPC/Protobuf v1 | [Nguồn `.proto` — D46](../system/api-conventions.md#v1-grpc-contract-sources), [Project hợp đồng C# — D47](../system/backend.md#v1-grpc-code-generation) | Đã chọn vị trí nguồn và project hợp đồng theo DEC-164/165; chưa có file `.proto` hoặc mã sinh |
| [Account recovery](account-recovery.openapi.json) | [Liên kết email](../features/accounts/email-links.md), [Khôi phục mật khẩu](../features/accounts/password-recovery.md) | Contract mục tiêu; nền hợp nhất có verify/reset. Resend/worker ở nhánh Identity riêng, chưa tích hợp; đối chiếu theo hiện trạng email |
| [Direct Messaging](direct-messaging.openapi.json) | [DM](../features/messaging/direct-messaging.md) | Nền hợp nhất có API mở/inbox/gửi/lịch sử/sửa/xóa DM; tiếp tục đối chiếu contract theo operation và chuyển service v1 |
| [Chat realtime](chat-realtime.schema.json) | [Đồng bộ tin](../features/messaging/synchronization.md) | Catalogue thông điệp ứng dụng SignalR; nền hợp nhất có ChatHub/outbox/reconnect DM. Cần đối chiếu schema mục tiêu, không phải wire frame |
| [Community](community.openapi.json) | [Community](../features/community/README.md), [Tin phòng](../features/messaging/channel-messaging.md) | Có endpoint đã triển khai và endpoint mục tiêu; xem trạng thái trong từng chủ đề |
| [Community realtime](community-realtime.schema.json) | [Tin phòng](../features/messaging/channel-messaging.md), [Community](../system/community.md) | Thiết kế mục tiêu; thông báo Community/tin phòng chưa tích hợp vào Hub/dispatcher DM đã có |
| [Media](media.openapi.json), [Media realtime](media-realtime.schema.json) | [Media](../features/media/README.md) | Thiết kế mục tiêu; chưa có backend/provider runtime |
| [Data protection](data-protection.schema.json) | [Vòng đời dữ liệu](../system/data-lifecycle.md#restore) | Thiết kế sổ bảo vệ; chưa có kho sổ/worker/proof restore |

## Quy ước

- API chưa triển khai dùng contract có nhãn dự thảo/mục tiêu; ghi phần cần review trước code.
- API hiện có được mô tả bởi Swagger sinh từ source. Khi triển khai hoặc thay đổi endpoint, đối chiếu contract, source, client và AC trong cùng thay đổi.
- Nếu một file chứa nhiều trạng thái, xác định trạng thái theo operation; không suy mọi endpoint đã hoạt động từ sự tồn tại của file OpenAPI.
- `$ref`, kiểu/field và ví dụ máy đọc giữ tại đây. Quyền và lỗi nghiệp vụ được giải thích tại chủ đề tương ứng.
- Cấu trúc hợp lệ và fixture đúng chỉ là kiểm tra artefact; kết quả sản phẩm cần build/môi trường và thực thi riêng.

Quy ước chung tại [API](../system/api-conventions.md). Dữ liệu biên tại [fixtures](../fixtures/README.md).

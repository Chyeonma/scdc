# SCDC — Hợp đồng máy đọc

Thư mục này chứa các file OpenAPI, JSON Schema và (sau này) Protobuf, mô tả chính xác dữ liệu trao đổi giữa các bên. Ý nghĩa, quyền, luồng xử lý và tiêu chí chấp nhận được giải thích trong tài liệu tính năng; không chép lại schema vào Markdown.

**Trạng thái:** một số API đã có trong nền MVP (`900e226`), phần còn lại là thiết kế mục tiêu. Cần đối chiếu từng endpoint với Swagger và code; file tồn tại không có nghĩa mọi endpoint trong đó đã chạy.

## Danh mục

| File | Tài liệu giải thích | Trạng thái |
|---|---|---|
| gRPC/Protobuf v1 | [Nguồn `.proto`](../system/api-conventions.md#v1-grpc-contract-sources), [project hợp đồng C#](../system/backend.md#v1-grpc-code-generation) | Đã chọn vị trí, chưa có file |
| [Account recovery](account-recovery.openapi.json) | [Liên kết email](../features/accounts/email-links.md), [khôi phục mật khẩu](../features/accounts/password-recovery.md) | Đã có xác minh email và đặt lại mật khẩu. Gửi lại email và worker gửi thư nằm ở nhánh Identity riêng, chưa tích hợp |
| [Direct Messaging](direct-messaging.openapi.json) | [Nhắn tin riêng](../features/messaging/direct-messaging.md) | Đã có mở DM, hộp thư, gửi, lịch sử, sửa, xóa |
| [Chat realtime](chat-realtime.schema.json) | [Đồng bộ tin](../features/messaging/synchronization.md) | Danh mục sự kiện SignalR; đã có cho DM. Cần đối chiếu với schema mục tiêu |
| [Community](community.openapi.json) | [Community](../features/community/README.md), [tin phòng](../features/messaging/channel-messaging.md) | Có cả endpoint đã làm và endpoint mục tiêu; xem trạng thái trong từng trang |
| [Community realtime](community-realtime.schema.json) | [Tin phòng](../features/messaging/channel-messaging.md), [Community](../features/community/mechanics.md) | Thiết kế; chưa nối vào Hub |
| [Media](media.openapi.json), [Media realtime](media-realtime.schema.json) | [Media](../features/media/README.md) | Thiết kế; chưa có backend |
| [Data protection](data-protection.schema.json) | [Vòng đời dữ liệu](../system/data-lifecycle.md#restore) | Thiết kế; chưa có code |

Hồ sơ đối chiếu: [nền MVP](../releases/mvp.md), [kiểm tra hợp nhất 2026-10-13](../records/verification/messaging/merge-2026-10-13.md).

## Quy ước

- API chưa làm được ghi nhãn "dự thảo" hoặc "mục tiêu".
- API đã làm được mô tả bằng Swagger sinh từ code. Khi thêm hoặc sửa endpoint, cập nhật file hợp đồng, code, client và tiêu chí chấp nhận trong cùng một thay đổi.
- Kiểu dữ liệu, trường và ví dụ đặt ở đây. Quyền và lỗi nghiệp vụ giải thích ở tài liệu tính năng.
- Schema hợp lệ và fixture đúng mới chỉ chứng minh file đúng định dạng, chưa chứng minh sản phẩm chạy đúng.

Xem thêm: [quy ước API](../system/api-conventions.md), [fixtures](../fixtures/README.md).

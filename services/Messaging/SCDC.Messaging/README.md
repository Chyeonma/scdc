# Module Messaging

Module sở hữu schema `messaging`: space, participant, message và vòng đời tin. Community cung cấp quyền phòng; Identity cung cấp tài khoản/phiên qua [SCDC.Contracts](../../SCDC.Contracts).

Hợp đồng cấp kênh nằm trong [SCDC.Messaging.Contracts](../SCDC.Messaging.Contracts). Mã gRPC và chuyển đổi payload cấp kênh nằm trong [SCDC.Messaging.Grpc](../SCDC.Messaging.Grpc).

## Cấu trúc mã

| Vị trí | Trách nhiệm |
|---|---|
| `Domain/` | Mô hình hội thoại, thành viên và tin nhắn |
| `Application/DirectConversations/`, `Application/GroupConversations/`, `Application/Messages/` | Hợp đồng và use case theo tính năng. Handler tạo và liệt kê nhóm gọi cổng `IGroupConversationStore` |
| `Infrastructure/DirectConversations/`, `Infrastructure/GroupConversations/`, `Infrastructure/Messages/` | Adapter PostgreSQL/EF Core theo tính năng, dùng chung phiên database và kiểm tra quyền truy cập |
| `Realtime/` | SignalR, phát sự kiện từ outbox và kiểm tra lại quyền trước khi phát |
| `MessagingModule.cs` | Nối cổng với adapter trong DI của host |

REST nằm tại `services/Messaging/SCDC.Messaging.Api/Controllers/Messaging/`. Chat riêng và chat nhóm dùng chung các action gửi, đọc, sửa và xóa tin. Controller chỉ định loại hội thoại, còn kiểm tra thành viên diễn ra trong transaction Messaging.

## Tài liệu

- [Các chủ đề Messaging](../../../docs/features/messaging/README.md) gồm DM, chat nhóm, tin phòng, nội dung, lưu trữ và đồng bộ.
- [Kiến trúc](../../../docs/system/architecture.md#boundaries): ranh giới module và lifecycle liên module.
- [Phát triển](../../../docs/guides/development.md): setup và lệnh kiểm thử.

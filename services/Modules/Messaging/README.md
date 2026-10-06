# Module Messaging

Module sở hữu **DM và tin nhắn trong phòng**, gồm lưu/gửi/lịch sử,
sửa/xóa, chống trùng và Hub chat. Community cung cấp membership/quyền phòng;
Identity cung cấp điều kiện tài khoản/phiên qua `SCDC.Contracts`.

Tài liệu tổ chức theo hành trình người dùng:

- [Direct Messaging](../../../docs/features/direct-messaging.md): đặc tả DM và nguồn chuẩn cho cơ chế xử lý tin dùng chung.
- [Tích hợp tin phòng](../../../docs/features/community/integration.md#responsibilities): áp dụng cơ chế Messaging vào phòng cộng đồng, gồm [UC-COM-23](../../../docs/features/community/integration.md#uc-com-23), [UC-COM-24](../../../docs/features/community/integration.md#uc-com-24) và phối hợp realtime [UC-COM-25](../../../docs/features/community/integration.md#uc-com-25).

UC-COM là mã truy vết hành trình; tin phòng được thực hiện trong Messaging.
Source vẫn đăng ký descriptor Foundation, chưa có writer/API nghiệp vụ hoặc Hub hoạt động.

Ranh giới module và trạng thái source nằm trong [kiến trúc](../../../docs/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/development.md).

Module sở hữu schema `messaging`; giao tiếp liên module qua `SCDC.Contracts`.

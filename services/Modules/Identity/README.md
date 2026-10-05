# Module Identity

Đặc tả nghiệp vụ, dữ liệu/API, UX và kiểm thử được quản lý tại
[accounts.md](../../../docs/features/accounts.md).
Ranh giới module và trạng thái source nằm trong [kiến trúc](../../../docs/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/development.md).

Module sở hữu schema `identity`; giao tiếp liên module qua `SCDC.Contracts`.

Worker email chạy trong API, gửi bằng Gmail SMTP với App Password; mặc định tắt.
Cấu hình, migration, key ring và kiểm thử tại [identity-email.md](../../../docs/identity-email.md).

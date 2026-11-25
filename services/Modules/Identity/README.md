# Identity đã chuyển thành service

Đặc tả nghiệp vụ, dữ liệu/API, UX và kiểm thử được quản lý tại
[Tài khoản](../../../docs/features/accounts/README.md).
Ranh giới module và trạng thái source nằm trong [kiến trúc](../../../docs/system/architecture.md#boundaries).
Setup và lệnh kiểm thử nằm trong [hướng dẫn phát triển](../../../docs/guides/development.md).

Source hiện ở [services/Identity](../../Identity/README.md), gồm Api, Application,
Domain và Infrastructure. Host còn lại gọi Identity qua gRPC/mTLS và đọc phiên
từ Redis; không tham chiếu implementation Identity.

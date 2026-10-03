# Tính năng: Tài khoản

Module quản lý đăng ký, đăng nhập, xác minh email, hồ sơ người dùng và phiên đăng nhập.

## Tài liệu trong thư mục này

| File | Nội dung | Đọc khi |
|---|---|---|
| [requirements.md](requirements.md) | Usecase, quy tắc nghiệp vụ, tiêu chí chấp nhận | Bắt đầu làm tính năng |
| [wireframes.md](wireframes.md) | Phác thảo giao diện đăng ký/đăng nhập/hồ sơ | Làm Frontend |
| [api-contracts.md](api-contracts.md) | API endpoints và quy ước | Làm Backend / tích hợp |

## Quyết định đã chốt (tóm tắt)

- **DEC-054:** Đăng ký bằng email + tên tài khoản + mật khẩu; đăng nhập bằng email HOẶC tên tài khoản.
- **DEC-051:** Chưa xác minh email → chỉ dùng xác minh/khôi phục, không vào app.
- **DEC-042:** Có luồng đặt lại mật khẩu qua email.

## Trạng thái

🟡 Quy tắc cốt lõi đã chốt. Còn mở: chính sách mật khẩu, phiên, chống lạm dụng (OQ-002).

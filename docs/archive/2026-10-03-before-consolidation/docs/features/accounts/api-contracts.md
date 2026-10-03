# Tài khoản — API Contracts

> Tài liệu gốc: [SCDC-API-DM-001 § Mục 3](../../archive/project-specs/05-architecture/02-account-dm-contracts.md)

## Endpoints

| Method | Đường dẫn | Đầu vào | Kết quả |
|---|---|---|---|
| `POST` | `/api/v1/auth/register` | email, username, password | `201`: cần xác minh; không cấp phiên |
| `POST` | `/api/v1/auth/verify-email` | Token từ email | `204`: xác minh OK |
| `POST` | `/api/v1/auth/resend-verification` | email | `202`: phản hồi chung |
| `POST` | `/api/v1/auth/login` | login, password | `200`: phiên + hồ sơ; hoặc `403 EMAIL_UNVERIFIED` |
| `POST` | `/api/v1/auth/forgot-password` | email | `202`: phản hồi chung |
| `POST` | `/api/v1/auth/reset-password` | Token + newPassword | `204`: đổi mật khẩu |
| `POST` | `/api/v1/auth/refresh` | Bằng chứng phiên | Phiên mới hoặc `401` |
| `POST` | `/api/v1/auth/logout` | Phiên hiện tại | `204` |
| `GET` | `/api/v1/users/me` | Phiên hợp lệ | Hồ sơ cá nhân |
| `PATCH` | `/api/v1/users/me` | Trường hồ sơ | Hồ sơ sau cập nhật |

## Quy ước lỗi

Lỗi trả `{ code, message, fieldErrors?, requestId }`. Không đưa stack trace, token hoặc nội dung vào lỗi/log.

---

📎 Đầy đủ: [Hợp đồng API gốc](../../archive/project-specs/05-architecture/02-account-dm-contracts.md)

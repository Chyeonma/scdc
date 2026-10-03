# Đặc tả REST API tổng thể

Tài liệu này cung cấp các nguyên tắc thiết kế, quy ước giao tiếp và danh sách các endpoint REST API trong hệ thống SCDC.

---

## 1. Nguyên tắc thiết kế API

- **Giao thức:** HTTPS, định dạng truyền dữ liệu `application/json`.
- **Phiên bản hóa:** Tiền tố `/api/v1/` trong URI.
- **Xác thực:** Bearer Token (JWT) truyền qua Header `Authorization: Bearer <access_token>`.
- **Mã lỗi:** Chuẩn hóa RFC 7807 `ProblemDetails` cho mọi phản hồi lỗi (xem chi tiết tại [error-handling.md](../development/error-handling.md)).
- **Tài liệu trực quan:** Truy cập Swagger UI tại môi trường cục bộ: `http://localhost:5026/swagger`.

---

## 2. Nhóm Endpoint chính

### 2.1. Nhóm Identity (`/api/v1/identity/`)

| Phương thức | Endpoint | Mô tả | Yêu cầu xác thực |
|:---:|---|---|:---:|
| `POST` | `/api/v1/identity/auth/register` | Đăng ký tài khoản mới | Không |
| `POST` | `/api/v1/identity/auth/verify-email` | Xác thực email bằng token | Không |
| `POST` | `/api/v1/identity/auth/login` | Đăng nhập bằng email/username và mật khẩu | Không |
| `POST` | `/api/v1/identity/auth/refresh-token` | Làm mới Access Token bằng Refresh Token | Không |
| `POST` | `/api/v1/identity/auth/logout` | Đăng xuất phiên làm việc hiện tại | Có |
| `POST` | `/api/v1/identity/auth/logout-all` | Đăng xuất toàn bộ các thiết bị | Có |
| `GET` | `/api/v1/identity/users/me` | Lấy thông tin hồ sơ người dùng hiện tại | Có |
| `PUT` | `/api/v1/identity/users/me` | Cập nhật hồ sơ (display name, bio, timezone) | Có |
| `GET` | `/api/v1/identity/users/sessions` | Lấy danh sách các phiên đăng nhập đang hoạt động | Có |

### 2.2. Nhóm Community (`/api/v1/community/`)

| Phương thức | Endpoint | Mô tả | Yêu cầu xác thực |
|:---:|---|---|:---:|
| `POST` | `/api/v1/community/servers` | Tạo máy chủ cộng đồng mới | Có |
| `GET` | `/api/v1/community/servers` | Lấy danh sách máy chủ người dùng đã tham gia | Có |
| `GET` | `/api/v1/community/servers/{id}` | Lấy chi tiết máy chủ, danh mục và kênh | Có |
| `POST` | `/api/v1/community/servers/{id}/channels` | Tạo kênh mới trong máy chủ | Có (Quyền quản lý kênh) |
| `POST` | `/api/v1/community/servers/join` | Tham gia máy chủ bằng mã mời (Invite Code) | Có |

### 2.3. Nhóm Messaging (`/api/v1/messaging/`)

| Phương thức | Endpoint | Mô tả | Yêu cầu xác thực |
|:---:|---|---|:---:|
| `GET` | `/api/v1/messaging/spaces/{spaceId}/messages` | Lấy lịch sử tin nhắn (Cursor Pagination) | Có |
| `POST` | `/api/v1/messaging/spaces/{spaceId}/messages` | Gửi tin nhắn mới (kèm `clientMessageId`) | Có |
| `PUT` | `/api/v1/messaging/messages/{id}` | Sửa nội dung tin nhắn đã gửi | Có |
| `DELETE` | `/api/v1/messaging/messages/{id}` | Xóa tin nhắn | Có |
| `POST` | `/api/v1/messaging/messages/{id}/reactions` | Thả biểu cảm cảm xúc (reaction) | Có |

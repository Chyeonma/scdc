# SCDC — Nền tảng giao tiếp trên web

SCDC là ứng dụng giao tiếp trên web dành cho nhóm bạn và cộng đồng. Mỗi cộng đồng có một không gian riêng, gọi là server. Các phòng trong server giúp tổ chức trao đổi theo chủ đề.

## Mục lục

- [Bản phát hành](#bản-phát-hành): mục tiêu v1 và nền MVP.
- [Tính năng](#tính-năng): các chức năng trong phạm vi v1.
- [Thành phần chính](#thành-phần-chính): mã nguồn, dữ liệu và công cụ.
- [Khởi chạy](#khởi-chạy): chuẩn bị môi trường phát triển.
- [Tài liệu dự án](docs/README.md): yêu cầu, thiết kế, kế hoạch và vận hành.

## Bản phát hành

- **[v1](docs/releases/v1.md)** là bản hoàn thiện đầu tiên, đang được phát triển. Bản này gồm các tính năng bên dưới.
- **[MVP](docs/releases/mvp.md)** là mốc phát triển nội bộ. Nền này đã tích hợp tài khoản, cộng đồng và nhắn tin riêng.

Trạng thái triển khai chi tiết nằm trong tài liệu của từng nhóm tính năng.

## Tính năng

Phạm vi v1 gồm:

| Nhóm | Chức năng |
|---|---|
| [Tài khoản](docs/features/accounts/README.md) | Đăng ký, xác minh email, đăng nhập và quản lý hồ sơ, mật khẩu, phiên đăng nhập. |
| [Cộng đồng](docs/features/community/README.md) | Tạo và tham gia server. Quản lý thành viên, phòng và quyền truy cập. |
| [Nhắn tin](docs/features/messaging/README.md) | Nhắn riêng giữa hai người hoặc trao đổi trong phòng. Xem lịch sử, sửa và xóa tin nhắn. |
| [Thoại và video](docs/features/media/README.md) | Gọi riêng hoặc tham gia phòng thoại. Sử dụng micro, camera và chia sẻ màn hình. |

## Thành phần chính

| Thành phần | Vai trò |
|---|---|
| [clients/WebClient](clients/WebClient) | Giao diện web, dùng React và Vite. |
| [services](services) | Backend xử lý nghiệp vụ, cung cấp API và cập nhật thời gian thực. Dùng .NET. |
| [database/postgres](database/postgres) | Script SQL khởi tạo, cập nhật cấu trúc và tạo dữ liệu mẫu cho PostgreSQL. |
| [compose.yaml](compose.yaml) | Cấu hình chạy giao diện, API và cơ sở dữ liệu bằng container trong môi trường phát triển. |
| [Makefile](Makefile) | Các lệnh chạy, build, kiểm thử và kiểm tra tài liệu. |
| [docs](docs/README.md) | Tài liệu sản phẩm, thiết kế hệ thống và hướng dẫn làm việc. |

Backend hiện chạy trong một ứng dụng API. [Kiến trúc v1](docs/system/architecture.md#target) sẽ tách thành bốn dịch vụ: tài khoản, cộng đồng, nhắn tin và điều khiển cuộc gọi.

## Khởi chạy

Cần Docker hoặc Podman với Compose.

Trước lần chạy đầu, tạo khóa bằng `openssl rand -base64 32`. Lưu khóa vào biến `COMMUNITY_OPERATION_KEY` trong file `.env` tại thư mục gốc dự án. Giữ nguyên khóa và các volume dữ liệu qua những lần chạy.

Các bước chạy ứng dụng và kiểm thử nằm trong [hướng dẫn phát triển](docs/guides/development.md#setup).

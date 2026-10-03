# Danh mục Tài liệu Kỹ thuật SCDC

Tài liệu kỹ thuật của nền tảng giao tiếp thời gian thực SCDC được phân chia theo các nhóm trách nhiệm chức năng, phục vụ cho việc phát triển, kiểm thử, vận hành và quản lý dự án.

---

## 1. Kiến trúc hệ thống (Architecture)

Tài liệu mô tả thiết kế tổng quan, ranh giới các Bounded Context, luồng dữ liệu và các quyết định kiến trúc cốt lõi.

| Tài liệu | Nội dung chính |
|---|---|
| [Tổng quan kiến trúc](architecture/overview.md) | Chiến lược Modular Monolith sang Microservices, sơ đồ hệ thống và ranh giới dữ liệu. |
| [Bản thiết kế luồng dịch vụ (Service Blueprint)](architecture/service-blueprint.md) | Đặc tả 16 phần chi tiết về luồng dữ liệu giữa các dịch vụ, Sequence Diagrams và kế hoạch bóc tách. |
| [Sổ quyết định kiến trúc (ADR)](architecture/decisions/adr-001-modular-monolith-first.md) | Nhật ký các quyết định kiến trúc (Architecture Decision Records) theo chuẩn quốc tế. |

---

## 2. Đặc tả các Module nghiệp vụ (Modules)

Tài liệu chi tiết về logic nghiệp vụ, thực thể CSDL và giao diện tích hợp của từng module độc lập.

| Module | Nội dung chính | Trạng thái hiện tại |
|---|---|:---:|
| [Identity](modules/identity.md) | Vòng đời tài khoản, JWT Access Token, Refresh Token Rotation, Session đa thiết bị, Account Lockout. | Đã hoàn thành v1 |
| [Community](modules/community.md) | Quản lý Server, Channel, Category, hệ thống phân quyền ma trận Bitwise RBAC. | Đang hoàn thiện |
| [Messaging](modules/messaging.md) | Chat DM 1-1, Channel Chat, thuật toán phân trang Cursor-based Pagination, SignalR Hub. | Đang hoàn thiện |
| [File Storage](modules/file-storage.md) | Tải tệp trực tiếp qua MinIO Presigned URL, worker nén ảnh và tạo thumbnail. | Kế hoạch triển khai |
| [Calls & WebRTC](modules/voice-video.md) | Điều phối phòng thoại, cuộc gọi 1-1, chia sẻ màn hình qua LiveKit SFU. | Kế hoạch triển khai |

---

## 3. Giao diện lập trình ứng dụng (API & Contracts)

Tài liệu giao tiếp giữa Client và Backend, và giữa các dịch vụ nội bộ.

| Tài liệu | Nội dung chính |
|---|---|
| [REST API tổng thể](api/rest-api.md) | Quy ước thiết kế, phân loại endpoint, xác thực Bearer Token, Swagger UI. |
| [SignalR Realtime Hub](api/signalr-hub.md) | Kết nối WebSocket, quản lý Space Group, danh sách sự kiện client-bound và server-bound. |
| [Quy ước xử lý lỗi (Error Handling)](development/error-handling.md) | Mẫu thiết kế `Result<T>`, phân loại lỗi nghiệp vụ và chuẩn hóa RFC 7807 ProblemDetails. |

---

## 4. Hướng dẫn và Quy ước phát triển (Development)

Tài liệu phục vụ kỹ sư trong quá trình tham gia phát triển dự án.

| Tài liệu | Nội dung chính |
|---|---|
| [Hướng dẫn thiết lập môi trường](development/setup-guide.md) | Cài đặt công cụ, khởi chạy hệ thống bằng Docker Compose hoặc chạy cục bộ để debug. |
| [Phân công trách nhiệm nhóm](development/team-allocation.md) | Ma trận phân công trách nhiệm 3 thành viên, lộ trình chuyển đổi kiến trúc 5 tuần. |
| [Quy ước lập trình & Git](development/coding-conventions.md) | Chuẩn viết mã C# (.NET 10), quy tắc đặt tên, chiến lược phân nhánh và Conventional Commits. |

---

## 5. Kiểm thử và Vận hành (Testing & Operations)

| Tài liệu | Nội dung chính |
|---|---|
| [Chiến lược kiểm thử](testing/test-strategy.md) | Phân tầng kiểm thử (Unit, Integration với PostgreSQL thật, Stress test k6). |
| [Hướng dẫn triển khai](operations/deployment.md) | Đóng gói Docker container tối ưu dung lượng, cấu hình `compose.yaml` và lộ trình Kubernetes. |
| [Đặc tả CSDL PostgreSQL](../database/postgres/README.md) | Cấu trúc 7 schema, views quan sát luồng dữ liệu và script dữ liệu mẫu (seed data). |

---

## 6. Hồ sơ Quản lý Dự án (Project Management Lifecycle)

Toàn bộ hồ sơ quy trình phát triển phần mềm chuẩn mực từ giai đoạn Khởi tạo đến Kiểm thử và Nghiệm thu được lưu trữ tập trung tại thư mục [`project/`](project/README.md):

- `01-initiation`: Tiếp nhận yêu cầu, Project Brief, dự toán ngân sách và sổ quyết định.
- `02-discovery`: Phân tích đối tượng người dùng và hành trình ưu tiên.
- `03-requirements`: Đặc tả yêu cầu chức năng (DM, Community, Accounts, Media, Ma trận ACL).
- `04-ux`: Luồng người dùng và Wireframes chi tiết cho Desktop và Trình duyệt Di động.
- `05-architecture`: Phác thảo giải pháp và hợp đồng API/dữ liệu chi tiết.
- `06-planning`: Kế hoạch bàn giao theo đợt và điều kiện sẵn sàng phát triển (Ready for Dev).
- `07-quality`: Kế hoạch kiểm thử hệ thống và kịch bản kiểm thử chi tiết.

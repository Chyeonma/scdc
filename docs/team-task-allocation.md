# Kế hoạch & Phân chia công việc dự án SCDC

> **Dự án:** SCDC (Nền tảng giao tiếp thời gian thực - Real-time Communication Platform)  
> **Kiến trúc mục tiêu:** **Microservices Architecture** (Tiếp cận theo chiến lược **Monolith First**)  
> **Công nghệ nền tảng:** .NET 10, C#, PostgreSQL 18, SignalR Hub, Redis, RabbitMQ, MinIO, Docker / Kubernetes, React WebClient  
> **Đối tượng:** Nhóm 3 thành viên  

---

## 1. Triết lý kiến trúc & Chiến lược triển khai

Dự án được xây dựng dựa trên nguyên lý kiến trúc kinh điển **Monolith First** (được khởi xướng bởi *Martin Fowler*):

```text
GIAI ĐOẠN 1: TỐI ƯU TỐC ĐỘ PHÁT TRIỂN        GIAI ĐOẠN 2: BÓC TÁCH MICROSERVICES
        (Modular Monolith)                         (Distributed Services)

      ┌─────────────────────────┐               ┌──────────────────────────────────────┐
      │        SCDC.Api         │               │     API Gateway (YARP / Reverse)     │
      │ ┌──────────┬──────────┐ │               └───┬──────────────┬────────────────┬──┘
      │ │ Identity │Community │ │                   │              │                │
      │ ├──────────┴──────────┤ │      ───►         ▼              ▼                ▼
      │ │      Messaging      │ │           ┌──────────────┐┌──────────────┐┌──────────────┐
      │ └─────────────────────┘ │           │ Identity.Api ││Community.Api ││Messaging.Api │
      └─────────────────────────┘           └──────────────┘└──────────────┘└──────────────┘
```

1. **Giai đoạn 1 (Modular Monolith):** Tập trung xây dựng nghiệp vụ lõi trong cùng một solution. Các domain được tách riêng thành các Class Library (`SCDC.Identity`, `SCDC.Community`, `SCDC.Messaging`), chia schema database riêng biệt (`identity`, `community`, `messaging`), giao tiếp in-memory qua `SCDC.Contracts`. Tránh được rủi ro vỡ hệ thống do lỗi mạng và phân tán dữ liệu ở giai đoạn đầu.
2. **Giai đoạn 2 (Microservices Migration):** Bóc tách các module thành các service độc lập chạy container riêng, định tuyến qua **API Gateway (YARP)**, giao tiếp qua HTTP/gRPC và xử lý bất đồng bộ qua **RabbitMQ / Transactional Outbox**.

---

## 2. Mô hình "Tam Giác Vàng" phân công trách nhiệm

Hệ thống phân chia theo đúng thế mạnh và định hướng chuyên môn hóa của 3 thành viên:

```text
                        ┌──────────────────────────────────────────────┐
                        │        1. BẠN (LEAD & CLOUD / DEVOPS)        │
                        │    - Kiến trúc Microservices & API Gateway   │
                        │    - Docker Compose, Kubernetes (K8s), Helm  │
                        │    - Tự động hóa CI/CD (GitHub Actions)      │
                        │    - Module Identity & Community Core        │
                        └──────────────────────┬───────────────────────┘
                                               │
                       ┌───────────────────────┴───────────────────────┐
                       ▼                                               ▼
┌──────────────────────────────────────────────┐┌──────────────────────────────────────────────┐
│       2. BẠN C# (PERFORMANCE & CORE DATA)    ││     3. BẠN VIBECODE (FRONTEND & SERVICES)    │
│ - Microservice: Messaging & Chat Engine      ││ - Toàn quyền làm chủ WebClient (React/Vite)  │
│ - Tối ưu truy vấn: Cursor Pagination         ││ - Microservice: File & Storage (MinIO/S3)    │
│ - Caching Redis & Tối ưu chịu tải (k6 test)  ││ - Voice Coordinator (LiveKit WebRTC)         │
│ - Tối ưu Concurrency, P99 Latency            ││ - Background Worker (RabbitMQ / Email)       │
└──────────────────────────────────────────────┘└──────────────────────────────────────────────┘
```

---

## 3. Bản mô tả chi tiết nhiệm vụ từng thành viên

### 🧑‍💻 Thành viên 1: Bạn (Team Lead, Cloud-Native & DevOps)
* **Vai trò:** Trưởng nhóm, Thiết kế Kiến trúc tổng thể, Kỹ sư Hạ tầng & DevOps.
* **Các dịch vụ / Thành phần phụ trách:**
  1. **Kiến trúc & Hạ tầng (DevOps / SRE):**
     - Quản trị toàn bộ môi trường [compose.yaml](../compose.yaml) (Postgres, Redis, RabbitMQ, MinIO).
     - Thiết lập và quản lý **Kubernetes (K8s)** (Triển khai Pods, Services, ConfigMaps, Ingress-NGINX / MetalLB bằng k3s hoặc Minikube).
     - Xây dựng pipeline **CI/CD** tự động (GitHub Actions: Build, Unit Test, Build Docker Images và Push lên Container Registry).
     - Xây dựng **API Gateway** (sử dụng **YARP - Yet Another Reverse Proxy** của Microsoft) đóng vai trò Reverse Proxy định tuyến cho toàn bộ cụm Microservices.
  2. **Dịch vụ nghiệp vụ (Identity & Community):**
     - **Identity Service:** Hoàn thiện và duy trì vòng đời tài khoản, bảo mật JWT, Refresh Token rotation chống reuse, Session management (đã hoàn thành v1).
     - **Community Service:** Quản lý cấu trúc Server, Channel, Category, hệ thống phân quyền Bitwise RBAC (`Role Permissions` và `Channel Overrides`).
  3. **Hạ tầng Realtime Core:**
     - Thiết lập nền tảng **SignalR Hub** kèm **Redis Backplane** (`AddStackExchangeRedis`) để các node realtime có thể scale ngang (Horizontal Scale).

---

### 💻 Thành viên 2: Bạn C# (Performance & Core Messaging Engine)
* **Vai trò:** Kỹ sư Backend chuyên sâu, Chuyên gia Tối ưu hiệu năng & Dữ liệu.
* **Các dịch vụ / Thành phần phụ trách:**
  1. **Microservice cốt lõi (Messaging Service):**
     - Thiết kế và phát triển toàn bộ nghiệp vụ Chat: Direct Message (DM 1-1), Group Chat, Channel Messages trong Server.
     - Triển khai thuật toán **Cursor-based Pagination** (phân trang theo con trỏ thời gian / ID) thay thế Offset/Limit, đảm bảo tốc độ phản hồi dưới 10ms kể cả khi bảng tin nhắn đạt hàng chục triệu bản ghi.
     - Xử lý các tương tác thời gian thực: Thả reaction, ghim tin nhắn (Pin), luồng hội thoại phụ (Threads/Replies).
  2. **Tối ưu hóa chịu tải & Benchmark (High Throughput & Concurrency):**
     - Thực hiện kiểm thử chịu tải (Stress / Load Testing) bằng công cụ **k6** hoặc **JMeter**: Giả lập hàng nghìn client gửi/nhận tin nhắn đồng thời.
     - Tối ưu hóa truy vấn CSDL: Đo đạc bằng `EXPLAIN ANALYZE`, thiết kế Composite Index tối ưu trên PostgreSQL.
     - Áp dụng Caching phân tán với **Redis**: Cache danh sách tin nhắn gần nhất, cache quyền và thông tin user để giảm tải 80% cho database chính.
     - Tối ưu hóa code C#: Tận dụng `Span<T>`, `Memory<T>`, `System.Threading.Channels` để đạt Zero-Allocation và giảm tải cho Garbage Collector (GC).

---

### 🎨 Thành viên 3: Bạn Vibecode (Frontend Lead & Auxiliary Services)
* **Vai trò:** Kỹ sư Giao diện người dùng (Frontend) & Tích hợp Dịch vụ phụ trợ.
* **Các dịch vụ / Thành phần phụ trách:**
  1. **Toàn quyền làm chủ WebClient (React / Vite):**
     - Sử dụng AI (prompting) để phát triển và hoàn thiện giao diện người dùng dựa trên khung sẵn có tại [clients/WebClient](../clients/WebClient).
     - Đấu nối toàn bộ API thật từ Backend vào UI: Đăng nhập/Đăng ký, cây thư mục Server/Channel, khung chat realtime, modal cài đặt.
     - Tích hợp thư viện `@microsoft/signalr` phía client để lắng nghe tin nhắn mới, trạng thái đang gõ phím (typing indicator), và trạng thái online/offline.
     - Chăm chút trải nghiệm người dùng (UX): Hiệu ứng animation, âm thanh thông báo, bộ chọn Emoji, xem trước ảnh/tệp tải lên.
  2. **Các Microservices phụ trợ (Vệ tinh):**
     - **File & Storage Service:** Tích hợp MinIO / S3 SDK để sinh **Presigned URL** cho upload trực tiếp; worker xử lý nén ảnh thumbnail bằng `SixLabors.ImageSharp`.
     - **Voice Coordinator Service:** Tích hợp `LiveKit Server SDK` để cấp Token tham gia phòng thoại / chia sẻ màn hình WebRTC.
     - **Async Notification Worker:** Viết worker nền nhận message từ RabbitMQ (hoặc Transactional Outbox) để gửi email kích hoạt và thông báo đẩy.

---

## 4. Danh sách các Microservices trong hệ thống

Khi báo cáo đồ án, hệ thống sẽ được trình bày dưới dạng 5 Microservices và các thành phần bổ trợ:

| Tên Dịch vụ / Service | Người phụ trách | Cổng Port | Trách nhiệm chính |
|---|---|:---:|---|
| **SCDC.Gateway** | Lead | `5000` | API Gateway (YARP), Reverse Proxy, xác thực token tập trung, định tuyến request. |
| **SCDC.Identity.Api** | Lead | `5001` | Đăng ký, đăng nhập, JWT, quản lý session và hồ sơ tài khoản. |
| **SCDC.Community.Api** | Lead | `5002` | Quản lý Server, Channel, Role, thuật toán tính quyền Bitwise RBAC. |
| **SCDC.Messaging.Api** | Bạn C# | `5003` | CRUD tin nhắn, phân trang Cursor, SignalR Realtime Hub, Caching Redis. |
| **SCDC.File.Api** | Bạn Vibecode | `5004` | Presigned URL upload tệp lên MinIO, nén ảnh đại diện và tạo thumbnail. |
| **SCDC.Workers** | Bạn Vibecode | Worker ngầm | Consumer RabbitMQ xử lý gửi email, dọn dẹp token, xử lý sự kiện bất đồng bộ. |
| **SCDC.WebClient** | Bạn Vibecode | `3000` | Giao diện React / Vite phục vụ người dùng cuối (chạy qua Nginx). |

---

## 5. Lộ trình phát triển & Chuyển đổi kiến trúc

```text
Tuần 1: Setup nền tảng & Code logic lõi (Modular Monolith)
  ├── Lead: Dựng Docker Compose (Postgres, Redis), hoàn thiện Community.
  ├── Bạn C#: Dựng Message Service, thuật toán Cursor Pagination.
  └── Bạn Vibecode: Dựng MinIO Presigned URL, làm quen với React WebClient.

Tuần 2: Hoàn thiện tính năng & Đấu nối Realtime
  ├── Lead: Thiết lập SignalR Hub, kiểm tra quyền IChannelAccessChecker.
  ├── Bạn C#: Bắn sự kiện realtime sang SignalR khi có tin nhắn mới.
  └── Bạn Vibecode: Ghép luồng Auth và Chat cơ bản lên giao diện React.

Tuần 3: Bóc tách Microservices (Decoupling)
  ├── Lead: Tách host thành các project Web API riêng, cấu hình YARP Gateway.
  ├── Bạn C#: Tách độc lập Messaging API, cấu hình Redis Cache cho tin nhắn.
  └── Bạn Vibecode: Tách File Service độc lập, hoàn thiện upload ảnh trên UI.

Tuần 4: Mở rộng Cloud-Native & Tối ưu hiệu năng
  ├── Lead: Viết k8s manifests / k3s cluster, viết GitHub Actions CI/CD.
  ├── Bạn C#: Chạy k6 load testing, tối ưu Index CSDL, đo đạc P99 latency.
  └── Bạn Vibecode: Hoàn thiện toàn bộ các modal, emoji, giao diện WebClient.

Tuần 5: Đóng gói, Viết báo cáo & Chuẩn bị bảo vệ
  ├── Lead: Viết phần Kiến trúc Microservices, K8s, CI/CD, Gateway.
  ├── Bạn C#: Viết phần Báo cáo Tối ưu hóa hiệu năng, Biểu đồ Benchmark k6.
  └── Bạn Vibecode: Viết phần Giao diện người dùng, Tích hợp MinIO & LiveKit.
```

---

## 6. Điểm số và Đóng góp học thuật (Dành cho Hội đồng chấm thi)

Bản thiết kế này đáp ứng hoàn hảo các tiêu chí khắt khe nhất của đồ án tốt nghiệp / bài tập lớn:
1. **Tính học thuật cao:** Áp dụng mô hình chuẩn *Monolith First* và *Database Schema-per-service*, chứng minh năng lực kiểm soát ranh giới module trước khi phân tán.
2. **Công nghệ hiện đại:** .NET 10, C# mới nhất, PostgreSQL 18, SignalR với Redis Backplane, YARP Gateway, MinIO Object Storage, Docker & Kubernetes.
3. **Chuyên môn hóa rõ nét:** Cả 3 thành viên đều có phần việc độc lập, khối lượng tương đương và sản phẩm bàn giao có thể đo lường định lượng cụ thể.

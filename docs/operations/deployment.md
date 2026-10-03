# Hướng dẫn triển khai và vận hành hệ thống

Tài liệu này mô tả mô hình đóng gói container, triển khai môi trường Docker Compose cục bộ và định hướng Kubernetes (K8s) cho hệ thống SCDC.

---

## 1. Đóng gói Container

Mỗi dịch vụ được đóng gói bằng Multi-stage `Dockerfile` tối ưu dung lượng:
- **Build stage:** Sử dụng image `mcr.microsoft.com/dotnet/sdk:10.0` để restore dependencies và compile code.
- **Runtime stage:** Sử dụng image `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` (non-root, bảo mật tối đa, dung lượng nhỏ gọn).

---

## 2. Mô hình Compose hiện tại (`compose.yaml`)

Tệp cấu hình `compose.yaml` ở thư mục gốc khởi tạo các thành phần:
1. `postgres`: PostgreSQL 18, cổng `5432`, tự động nạp `database/postgres/schema.sql` và `seed.sql`.
2. `chat-service`: API Host (`SCDC.Api`), cổng `5026`.
3. `web-client`: Ứng dụng React chạy qua Nginx, cổng `3000`.

Lệnh vận hành:
```bash
# Khởi động dịch vụ
docker compose up -d

# Tắt dịch vụ và giữ lại dữ liệu
docker compose down

# Tắt dịch vụ và xóa sạch volume dữ liệu
docker compose down -v
```

---

## 3. Lộ trình triển khai Kubernetes (Giai đoạn 2)

Khi chuyển đổi sang kiến trúc Microservices phân tán:
- Triển khai Ingress Controller (NGINX Ingress hoặc YARP Ingress).
- Mỗi microservice (`Identity`, `Community`, `Messaging`, `File`) được đóng gói thành một `Deployment` riêng với `HorizontalPodAutoscaler` (HPA).
- Dữ liệu cấu hình và bí mật quản lý bằng `ConfigMap` và `Secret`.
- Pipeline CI/CD trên GitHub Actions tự động build Docker image và deploy vào cụm K8s khi code được merge vào nhánh `main`.

# Hướng dẫn Phát triển và Quy ước Kỹ thuật

Tài liệu này quy định phân công trách nhiệm, quy chuẩn lập trình, cơ chế xử lý lỗi và quy trình kiểm thử trong dự án SCDC.

---

## 1. Phân công trách nhiệm nhóm (3 thành viên)

| Thành viên | Trách nhiệm chuyên môn | Phạm vi công việc chính |
|---|---|---|
| **Thành viên 1** | Trưởng nhóm & Hạ tầng Cloud/DevOps | Kiến trúc hệ thống, Docker Compose, Kubernetes, CI/CD GitHub Actions, Module Identity, Module Community Core, SignalR Hub. |
| **Thành viên 2** | Kỹ sư Backend & Hiệu năng dữ liệu | Module Messaging (Chat Core), thuật toán phân trang Cursor-based Pagination, Caching Redis, Composite Index PostgreSQL, Kiểm thử chịu tải k6. |
| **Thành viên 3** | Kỹ sư Frontend & Dịch vụ vệ tinh | WebClient (React 18 / Vite), tích hợp MinIO Presigned URL (File Service), LiveKit SFU (Voice Coordinator), Worker xử lý email qua Outbox. |

### Lộ trình 5 tuần:
- **Tuần 1:** Thiết lập Docker Compose (PostgreSQL, Redis), hoàn thiện Identity và Community Core, xây dựng cấu trúc Messaging và thuật toán Cursor.
- **Tuần 2:** Đấu nối SignalR Realtime Hub với Redis Backplane; tích hợp luồng Auth và Chat lên giao diện React.
- **Tuần 3:** Tách cấu trúc chuẩn bị cho Microservices (Decoupling), cấu hình YARP Gateway, triển khai MinIO Presigned URL upload tệp.
- **Tuần 4:** Viết k8s manifests, pipeline CI/CD, thực hiện kiểm thử chịu tải k6 đo độ trễ P99, tối ưu truy vấn CSDL.
- **Tuần 5:** Hoàn thiện toàn bộ giao diện người dùng, đóng gói sản phẩm và chuẩn bị tài liệu báo cáo.

---

## 2. Quy ước lập trình C# (.NET 10)

### 2.1. Ranh giới Module
- Mã nguồn nghiệp vụ nằm trong `services/Modules/<TênModule>`.
- Các module **không tham chiếu trực tiếp nhau**. Mọi giao tiếp liên module bắt buộc thông qua interfaces tại `SCDC.Contracts`.
- Mỗi module có `DbContext` riêng và chỉ thao tác với schema PostgreSQL tương ứng.

### 2.2. Xử lý kết quả nghiệp vụ bằng `Result<T>`
Không sử dụng ném ngoại lệ (`throw Exception`) cho các lỗi nghiệp vụ thông thường. Sử dụng mẫu thiết kế **Result Pattern** tại `SCDC.BuildingBlocks`:

- `Result.Success()` hoặc `Result<T>.Success(value)`.
- `Result.Failure(error)` hoặc `Result<T>.Failure(error)`.

Bảng ánh xạ loại lỗi sang mã trạng thái HTTP:
- `Validation` $\rightarrow$ 400 Bad Request
- `NotFound` $\rightarrow$ 404 Not Found
- `Conflict` $\rightarrow$ 409 Conflict
- `Unauthorized` $\rightarrow$ 401 Unauthorized
- `Forbidden` $\rightarrow$ 403 Forbidden

Mọi lỗi trả về cho client tuân thủ chuẩn quốc tế **RFC 7807 ProblemDetails**:

```json
{
  "type": "https://errors.scdc.dev/identity/invalid-credentials",
  "title": "Invalid Credentials",
  "status": 401,
  "detail": "Email hoặc mật khẩu không chính xác.",
  "instance": "/api/v1/identity/auth/login",
  "extensions": {
    "errorCode": "Identity.InvalidCredentials"
  }
}
```

---

## 3. Quy trình Git và Commit

### 3.1. Phân nhánh
- Nhánh chính: `main` (mã nguồn ổn định).
- Nhánh tính năng: `feature/<tên-module>-<tên-chức-năng>` (ví dụ: `feature/messaging-cursor-pagination`).
- Nhánh sửa lỗi: `fix/<tên-lỗi>` (ví dụ: `fix/token-rotation-reuse`).

### 3.2. Quy chuẩn Commit Message (Conventional Commits)
Cấu trúc chuẩn: `<loại>(<phạm vi>): <mô tả ngắn>`
- `feat`: Tính năng mới.
- `fix`: Sửa lỗi.
- `refactor`: Tái cấu trúc mã nguồn.
- `perf`: Tối ưu hóa hiệu năng.
- `test`: Thêm/sửa ca kiểm thử.
- `docs`: Cập nhật tài liệu.

---

## 4. Kiểm thử tự động (Testing)

### 4.1. Kiểm thử đơn vị và tích hợp (xUnit)
```bash
# Chạy toàn bộ bộ test
dotnet test SCDC.slnx --configuration Release
```
*Lưu ý: Bộ test tích hợp `IdentityV1FlowTests` yêu cầu PostgreSQL đang chạy trên cổng 5432.*

### 4.2. Kiểm thử chịu tải (k6)
Kịch bản đo đạc hiệu năng phân trang Cursor và SignalR Hub đặt mục tiêu:
- Tỷ lệ lỗi 5xx: Dưới 0.01%.
- Thời gian phản hồi đọc tin nhắn: P95 < 20ms, P99 < 50ms.

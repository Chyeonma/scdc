# Chiến lược kiểm thử và đánh giá chất lượng

Tài liệu này xác định cấp độ kiểm thử, công cụ áp dụng và tiêu chuẩn đánh giá chất lượng hệ thống SCDC.

---

## 1. Các cấp độ kiểm thử

```text
┌──────────────────────────────────────────────┐
│           Kiểm thử Chịu tải (k6)            │  Độ trễ P99, Throughput, Concurrency
├──────────────────────────────────────────────┤
│         Kiểm thử Tích hợp (xUnit)            │  Luồng API -> Service -> PostgreSQL
├──────────────────────────────────────────────┤
│          Kiểm thử Đơn vị (Unit Tests)        │  Domain Logic, Result<T>, Validation
└──────────────────────────────────────────────┘
```

### 1.1. Kiểm thử đơn vị (Unit Tests)
- **Framework:** xUnit, FluentAssertions, Moq.
- **Phạm vi:** Kiểm tra quy tắc validation dữ liệu, thuật toán xử lý phân quyền Bitwise RBAC, định dạng token, quy tắc tính toán nghiệp vụ mà không phụ thuộc vào I/O hay CSDL ngoài.

### 1.2. Kiểm thử tích hợp (Integration Tests)
- **Thực thi:** Chạy trên môi trường CSDL PostgreSQL thực tế (kết nối qua Docker).
- **Phạm vi:** Kiểm tra toàn bộ vòng đời request từ tầng HTTP Controller, Middleware xác thực JWT, qua Application Service, xuống CSDL PostgreSQL và ghi nhận sự kiện Outbox.
- **Ví dụ hiện có:** Bộ test `IdentityV1FlowTests` kiểm tra trọn vẹn luồng Đăng ký $\rightarrow$ Xác thực email $\rightarrow$ Đăng nhập $\rightarrow$ Đổi mật khẩu $\rightarrow$ Khóa tài khoản (Lockout) $\rightarrow$ Thu hồi Token rotation.

### 1.3. Kiểm thử hiệu năng và chịu tải (Performance & Stress Testing)
- **Công cụ:** k6 / JMeter.
- **Kịch bản:**
  - Tải tin nhắn qua Cursor Pagination với bảng dữ liệu giả lập 1.000.000 bản ghi.
  - Giả lập 1.000 kết nối SignalR đồng thời phát/nhận tin nhắn trong cùng một Channel.
- **Chỉ tiêu chất lượng (SLO):**
  - Thời gian phản hồi API đọc tin nhắn: P95 < 20ms, P99 < 50ms.
  - Tỷ lệ lỗi HTTP 5xx: Dưới 0.01% dưới tải danh định.

---

## 2. Kế hoạch kiểm thử chi tiết

Các ca kiểm thử chi tiết và ma trận nghiệm thu được quản lý tập trung tại:
- [Kế hoạch kiểm thử và nghiệm thu](../project/07-quality/01-test-and-acceptance-plan.md)
- [Bộ kịch bản kiểm thử tài khoản và DM](../project/07-quality/02-account-dm-test-cases.md)

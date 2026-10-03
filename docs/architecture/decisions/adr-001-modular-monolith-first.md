# ADR 001: Áp dụng chiến lược Modular Monolith First

- **Mã:** ADR-001
- **Trạng thái:** Đã chấp thuận (Accepted)
- **Ngày:** 2026-09-27
- **Người quyết định:** Nhóm phát triển SCDC

---

## 1. Bối cảnh (Context)

Dự án SCDC đặt mục tiêu xây dựng một nền tảng giao tiếp thời gian thực với nhiều domain phức tạp (Identity, Messaging, Community, File, Calls). Mục tiêu cuối cùng là một kiến trúc Microservices phân tán có khả năng mở rộng độc lập.

Tuy nhiên, ở giai đoạn khởi đầu:
- Ranh giới nghiệp vụ giữa các domain chưa hoàn toàn ổn định và còn cần tinh chỉnh.
- Việc triển khai ngay lập tức cụm Microservices độc lập (với giao tiếp mạng qua HTTP/gRPC, xử lý giao dịch phân tán 2PC / Saga, và quản trị nhiều repo/pipeline) sẽ làm chậm đáng kể tốc độ phát triển và tăng chi phí hạ tầng không cần thiết.

---

## 2. Quyết định (Decision)

Chúng tôi quyết định áp dụng nguyên lý **Monolith First** (Martin Fowler):
1. Trong giai đoạn 1, toàn bộ hệ thống được xây dựng dưới dạng **Modular Monolith** sạch trên nền tảng .NET 10.
2. Các domain được tách thành các Class Library riêng biệt: `SCDC.Identity`, `SCDC.Community`, `SCDC.Messaging`.
3. Phân tách ranh giới dữ liệu tuyệt đối: Mỗi module sở hữu một schema CSDL riêng biệt trong PostgreSQL (`identity`, `community`, `messaging`). Tuyệt đối không cho phép truy vấn trực tiếp xuyên schema.
4. Giao tiếp giữa các module chỉ thông qua các abstractions/interface được định nghĩa tại `SCDC.Contracts`.
5. Đóng gói chạy chung trong một tiến trình host duy nhất (`SCDC.Api`).

---

## 3. Hệ quả (Consequences)

### Tích cực:
- **Tốc độ phát triển tối đa:** Dễ dàng debug, chạy thử nghiệm toàn bộ hệ thống trên môi trường local chỉ với 1 lệnh `dotnet run` hoặc 1 container Docker.
- **Bảo toàn tính toàn vẹn dữ liệu:** Không bị lỗi phân tán dữ liệu hoặc lỗi mạng giữa các services trong giai đoạn phát triển ban đầu.
- **Sẵn sàng bóc tách:** Vì ranh giới code (Contracts) và ranh giới dữ liệu (PostgreSQL Schemas) đã được cô lập hoàn toàn ngay từ đầu, việc bóc tách thành các Microservices độc lập ở giai đoạn 2 chỉ là tách host mà không cần viết lại logic nghiệp vụ.

### Tiêu cực / Đánh đổi:
- Cần kỷ luật nghiêm ngặt trong mã nguồn để ngăn chặn việc các kỹ sư vô tình tham chiếu trực tiếp project module với nhau thay vì thông qua Contracts.

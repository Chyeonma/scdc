# Tổng quan dự án SCDC

> Tài liệu này tổng hợp từ [hồ sơ gốc](../archive/project-specs/01-initiation/01-project-brief.md).

## Mục tiêu

Phát hành SCDC dưới dạng ứng dụng web giao tiếp dành cho nhóm bạn và cộng đồng. Người dùng có thể tự đăng ký, tạo hoặc tham gia cộng đồng, trao đổi bằng tin nhắn, thoại/video và chia sẻ màn hình.

## Phạm vi phiên bản đầu (MVP)

| Mã | Hạng mục | Nội dung bàn giao |
|---|---|---|
| SCP-001 | Nền tảng | Ứng dụng chạy trên trình duyệt web. |
| SCP-002 | Tài khoản | Đăng ký, đăng nhập và hồ sơ cá nhân. |
| SCP-003 | Cộng đồng | Tạo và quản lý server; tổ chức phòng; mời và quản lý thành viên. |
| SCP-004 | Phân quyền | Vai trò và quyền cơ bản đối với server và phòng. |
| SCP-005 | Nhắn tin | Gửi, nhận và xem lịch sử tin nhắn trong phòng thuộc server và hội thoại riêng giữa hai người. |
| SCP-006 | Thoại | Phòng thoại trong server cho nhiều thành viên và cuộc gọi riêng giữa hai người. |
| SCP-007 | Video và chia sẻ màn hình | Sử dụng camera và chia sẻ màn hình trong phòng thoại và cuộc gọi riêng. |
| SCP-008 | Phát hành | Mở đăng ký công khai sau khi hoàn thành kiểm thử và nghiệm thu. |

**Ngoài phạm vi:** Nhóm chat riêng ngoài server, ứng dụng desktop/mobile.

## Ràng buộc

| Nội dung | Giá trị |
|---|---|
| Kiến trúc | Microservices (Modular Monolith → tách dần) |
| Thời gian mục tiêu | ~3 tháng |
| Dự toán ban đầu | 500.000.000 VNĐ |
| Nhân sự | Vg (Lead/Architect/UX), Sáng (Backend), Thái (Frontend/QA) |
| Quy mô dự trù | 1.000 tài khoản, 100 CCU, 20 người gọi đồng thời |

## Sản phẩm tham chiếu

Discord — về trải nghiệm giao tiếp và cách tổ chức cộng đồng.

---

📎 Chi tiết đầy đủ: [Hồ sơ gốc](../archive/project-specs/01-initiation/01-project-brief.md) · [Ngân sách](budget.md) · [Nhân sự](team-and-roles.md)

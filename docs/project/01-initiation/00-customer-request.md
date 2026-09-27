# SCDC — Yêu cầu ban đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-REQ-001 |
| Phiên bản | 1.3 |
| Cập nhật | 2026-09-27 |
| Trạng thái | Đã ghi nhận yêu cầu cấp cao |

## 1. Nhu cầu

Xây dựng ứng dụng giao tiếp trực tuyến dành cho nhóm bạn và cộng đồng không
giới hạn chủ đề. Người sử dụng có thể tổ chức không gian sinh hoạt chung,
nhắn tin, tham gia phòng thoại, gọi video và chia sẻ màn hình. Người quản lý
cộng đồng có thể tổ chức phòng, quản lý thành viên và cấp quyền cơ bản.

Discord là sản phẩm tham chiếu về trải nghiệm giao tiếp và cách tổ chức
cộng đồng. Danh sách yêu cầu dưới đây xác định phạm vi cần phát triển.

## 2. Người sử dụng

| Nhóm | Nhu cầu sử dụng |
|---|---|
| Nhóm bạn | Giao tiếp chung trong các phòng của server hoặc riêng giữa hai người; sử dụng thoại/video và chia sẻ màn hình. |
| Thành viên cộng đồng | Tham gia các phòng để trao đổi và giao tiếp theo chủ đề. |
| Người tạo và quản lý cộng đồng | Tạo không gian chung, tổ chức phòng, mời và quản lý thành viên, thiết lập quyền. |

## 3. Yêu cầu cấp cao

| Mã | Nội dung |
|---|---|
| REQ-001 | Phục vụ nhóm bạn và cộng đồng, không giới hạn chủ đề. |
| REQ-002 | Có không gian cộng đồng (server), phòng, quản lý thành viên và quyền cơ bản. |
| REQ-003 | Phiên bản đầu hoạt động trên trình duyệt web. |
| REQ-004 | Cho phép nhắn tin trong phòng thuộc server. |
| REQ-005 | Cho phép nhắn tin riêng giữa hai người. |
| REQ-007 | Có phòng thoại trong server, cho phép thành viên vào và rời phòng. |
| REQ-008 | Hỗ trợ gọi riêng giữa hai người. |
| REQ-009 | Hỗ trợ camera/video và chia sẻ màn hình trong phòng thoại của server và cuộc gọi riêng giữa hai người. |
| REQ-010 | Phát hành công khai, cho phép người dùng tự đăng ký. |
| REQ-011 | Thời gian thực hiện mục tiêu khoảng 3 tháng. |
| REQ-012 | Sử dụng kiến trúc microservices. |
| REQ-013 | Bên phát triển lập dự toán trên cơ sở phạm vi và nguồn lực cần thiết. |

REQ-006 về nhóm chat riêng ngoài server đã được loại khỏi phạm vi từ phiên
bản 1.3. Mã yêu cầu này được giữ trong lịch sử và không sử dụng lại.

## 4. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Server | Không gian cộng đồng trên ứng dụng, có thành viên và các phòng; phân biệt với máy chủ hạ tầng. |
| Channel/phòng | Khu vực giao tiếp bên trong một server. |
| Tin nhắn riêng (DM) | Hội thoại riêng giữa hai người. |
| Phòng thoại | Phòng trong server mà thành viên có thể vào và rời để giao tiếp trực tiếp. |

Phạm vi bàn giao được tổng hợp tại [Project Brief](01-project-brief.md).
Những quy tắc nghiệp vụ và giới hạn cần xác định được theo dõi tại
[sổ vấn đề cần làm rõ](03-discovery-and-decision-log.md).

## 5. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Ghi nhận nhu cầu, đối tượng sử dụng và yêu cầu cấp cao. |
| 1.1 | 2026-09-27 | Chuẩn hóa mô tả yêu cầu và liên kết tài liệu. |
| 1.2 | 2026-09-27 | Tập trung nội dung vào yêu cầu khách hàng và thống nhất thuật ngữ sản phẩm. |
| 1.3 | 2026-09-27 | Loại REQ-006; giới hạn REQ-008 ở cuộc gọi riêng hai người và cập nhật ngữ cảnh video/chia sẻ màn hình tại REQ-009. |

[Mục lục hồ sơ](../README.md)

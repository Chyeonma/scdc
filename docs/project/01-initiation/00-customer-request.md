# SCDC — Yêu cầu ban đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-REQ-001 |
| Phiên bản | 1.7 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Đã ghi nhận yêu cầu cấp cao |

## 1. Nhu cầu

Xây dựng ứng dụng giao tiếp trực tuyến dành cho nhóm bạn và cộng đồng không
giới hạn chủ đề, phục vụ ba nhu cầu sử dụng:

- Kết nối với một cộng đồng hiện có và giao tiếp với các thành viên.
- Tạo một cộng đồng và tổ chức không gian giao tiếp chung.
- Nhắn tin riêng giữa hai người.

Người sử dụng có thể nhắn tin, tham gia phòng thoại, gọi video và chia sẻ
màn hình. Người quản lý cộng đồng có thể tổ chức phòng, quản lý thành viên
và cấp quyền cơ bản.

Ngày 2026-09-30, đại diện sản phẩm xác định đối tượng là một nhóm người bất
kỳ và nêu vấn đề của nhóm chat một luồng: nhiều người trao đổi khác chủ đề
khiến tin nhắn bị trôi, khó theo dõi liên tục; file tài liệu cũng gặp vấn
đề tương tự. Ưu tiên trước mắt là nhắn tin và tham gia cộng đồng, vì chức
năng nhắn tin cơ bản là nền tảng để phát triển thêm tính năng. Nhắn tin
riêng giữa hai người được ưu tiên trước; cộng đồng được tổ chức bằng nhiều
phòng theo chủ đề trước. Người dùng có thể tìm bằng tên tài khoản và nhắn
riêng ngay, không cần kết bạn hoặc cùng cộng đồng. Người dùng có thể tìm
người theo tên tài khoản hoặc tên hiển thị. Đợt nhắn tin riêng đầu hỗ trợ
văn bản, sửa và xóa tin, hiển thị trạng thái đã gửi hoặc lỗi gửi. Người
dùng tham gia cộng đồng qua liên kết mời hoặc tìm kiếm; việc gửi file trong
phòng theo chủ đề được để sang đợt sau. Quy tắc chi tiết còn cần làm rõ.

Đầu vào và các giả định cần kiểm chứng được ghi tại
[SCDC-DIS-001](../02-discovery/01-users-and-needs.md).

Discord là sản phẩm tham chiếu về trải nghiệm giao tiếp và cách tổ chức
cộng đồng. Danh sách yêu cầu dưới đây xác định phạm vi cần phát triển.

## 2. Người sử dụng

| Nhóm | Nhu cầu sử dụng |
|---|---|
| Nhóm bạn | Giao tiếp chung trong các phòng của server hoặc riêng giữa hai người; sử dụng thoại/video và chia sẻ màn hình. |
| Người nhắn tin riêng | Trao đổi trực tiếp với một người khác qua hội thoại riêng. |
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
| 1.4 | 2026-09-27 | Làm rõ ba nhu cầu sử dụng: kết nối với cộng đồng, tạo cộng đồng và nhắn tin riêng giữa hai người. |
| 1.5 | 2026-09-30 | Bổ sung vấn đề trôi tin nhắn/tài liệu; ghi nhận ưu tiên nhắn tin riêng trước và cộng đồng có nhiều phòng theo chủ đề. |
| 1.6 | 2026-09-30 | Ghi nhận tìm người bằng tên tài khoản và nhắn riêng ngay, không cần kết bạn hoặc cùng cộng đồng. |
| 1.7 | 2026-09-30 | Ghi nhận tìm theo tên tài khoản/tên hiển thị, thao tác và trạng thái DM; tham gia cộng đồng qua mời/tìm kiếm và thứ tự triển khai file. |

[Mục lục hồ sơ](../README.md)

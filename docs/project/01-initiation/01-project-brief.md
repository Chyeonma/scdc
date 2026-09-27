# SCDC — Tổng quan dự án

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-BRF-001 |
| Phiên bản | 1.5 |
| Cập nhật | 2026-09-27 |
| Trạng thái | Định hướng và phạm vi sơ bộ đã thống nhất |
| Căn cứ | [Yêu cầu ban đầu — SCDC-REQ-001](00-customer-request.md) |

## 1. Mục tiêu

Phát hành SCDC dưới dạng ứng dụng web giao tiếp dành cho nhóm bạn và cộng
đồng. Người dùng có thể tự đăng ký, tạo hoặc tham gia cộng đồng, trao đổi
bằng tin nhắn, thoại/video và chia sẻ màn hình. Người quản lý cộng đồng có
công cụ tổ chức phòng, quản lý thành viên và quyền truy cập cơ bản.

## 2. Phạm vi phiên bản đầu

| Mã | Hạng mục | Nội dung bàn giao |
|---|---|---|
| SCP-001 | Nền tảng | Ứng dụng chạy trên trình duyệt web. |
| SCP-002 | Tài khoản | Đăng ký, đăng nhập và hồ sơ cá nhân. |
| SCP-003 | Cộng đồng | Tạo và quản lý server; tổ chức phòng; mời và quản lý thành viên. |
| SCP-004 | Phân quyền | Vai trò và quyền cơ bản đối với server và phòng. |
| SCP-005 | Nhắn tin | Gửi, nhận và xem lịch sử tin nhắn trong phòng thuộc server và hội thoại riêng giữa hai người. |
| SCP-006 | Thoại | Phòng thoại trong server cho nhiều thành viên và cuộc gọi riêng giữa hai người. |
| SCP-007 | Video và chia sẻ màn hình | Sử dụng camera và chia sẻ màn hình trong phòng thoại của server và cuộc gọi riêng giữa hai người. |
| SCP-008 | Phát hành | Mở đăng ký công khai sau khi hoàn thành kiểm thử, nghiệm thu và chuẩn bị vận hành. |

Ngoài phạm vi phiên bản đầu:

- Nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này.
- Ứng dụng cài đặt riêng trên desktop và thiết bị di động.

Các chức năng bổ sung được đánh giá theo quy trình quản lý thay đổi.

## 3. Ràng buộc và cơ sở lập kế hoạch

| Nội dung | Giá trị |
|---|---|
| Kiến trúc | Microservices; ranh giới dịch vụ và mô hình triển khai được xác định trong thiết kế kỹ thuật. |
| Thời gian mục tiêu | Khoảng 3 tháng từ ngày khởi động. Ngày bắt đầu và các mốc bàn giao cụ thể chưa xác định. |
| Dự toán ban đầu | 500.000.000 VNĐ; cần ước lượng lại theo phạm vi và cơ cấu nhân sự đã xác định. |
| Nhân sự hiện có | Vg, Sáng và Thái có thể tham gia toàn thời gian. Vg là trưởng nhóm, phụ trách kiến trúc tổng thể, lựa chọn công nghệ, trực tiếp lập trình và hướng dẫn Thái; Sáng phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg; Thái là thành viên mới cần đào tạo. |
| Quy mô dự trù | 1.000 tài khoản, 100 người trực tuyến đồng thời và 20 người tham gia gọi đồng thời trên toàn hệ thống. |

Quy mô tải là giả định phục vụ dự toán. Cơ cấu nhân sự hiện có khác với giả
định ba kỹ sư có kinh nghiệm ban đầu; cần tính công sức kiêm nhiệm và hướng
dẫn khi đánh giá tiến độ, chi phí. Xem [nhân sự và chuẩn bị khảo sát](04-team-and-discovery-plan.md)
và [dự toán và giả định](02-budget-and-assumptions.md).

Ảnh hưởng của việc thu hẹp phạm vi nhắn tin và cuộc gọi đến công sức, chi
phí và tiến độ cần được lượng hóa khi cập nhật kế hoạch chi tiết.

## 4. Kết quả bàn giao

- Ứng dụng web và các dịch vụ đáp ứng phạm vi chức năng được đặc tả.
- Hồ sơ yêu cầu, thiết kế trải nghiệm và thiết kế kỹ thuật.
- Kết quả kiểm thử và hồ sơ nghiệm thu.
- Hướng dẫn triển khai, cấu hình, vận hành và bàn giao hệ thống.

Nội dung chi tiết và điều kiện chấp nhận của từng kết quả bàn giao được xác
định trong đặc tả yêu cầu và kế hoạch thực hiện.

## 5. Tiêu chí thành công sơ bộ

| Mã | Kết quả cần đạt | Cơ sở đánh giá |
|---|---|---|
| SUC-001 | Người dùng có thể đăng ký và sử dụng các hình thức giao tiếp trong phạm vi. | Kịch bản sử dụng và nghiệm thu trên môi trường phát hành. |
| SUC-002 | Người quản lý cộng đồng có thể tổ chức phòng, mời thành viên và áp dụng quyền cơ bản. | Ma trận quyền và các tình huống cho phép/từ chối truy cập. |
| SUC-003 | Tin nhắn được lưu và có thể xem lại. | Kiểm thử gửi, nhận và truy xuất lịch sử theo quy tắc nghiệp vụ. |
| SUC-004 | Thoại, video và chia sẻ màn hình hoạt động trên trình duyệt được hỗ trợ. | Kiểm thử theo quy mô phòng, điều kiện mạng và chất lượng đã xác định. |
| SUC-005 | Hệ thống đáp ứng kiến trúc microservices. | Hồ sơ kiến trúc và kết quả kiểm chứng triển khai, vận hành các dịch vụ. |
| SUC-006 | Sản phẩm sẵn sàng cho phát hành công khai. | Kết quả kiểm thử, nghiệm thu và xác nhận khả năng vận hành. |

Chỉ tiêu định lượng, môi trường kiểm thử và mức lỗi chấp nhận được cần được
xác định trước nghiệm thu.

## 6. Trách nhiệm

| Bên/vai trò | Trách nhiệm chính |
|---|---|
| Khách hàng | Xác nhận mục tiêu, yêu cầu, phạm vi và ưu tiên; xem xét thay đổi; nghiệm thu kết quả. |
| Quản lý dự án | Lập và theo dõi tiến độ, nguồn lực, ngân sách, phụ thuộc và rủi ro. |
| BA/UX/UI | Phân tích nhu cầu, đặc tả nghiệp vụ và thiết kế trải nghiệm/giao diện. |
| Tech Lead và kỹ sư | Thiết kế kỹ thuật, đánh giá khả năng tái sử dụng, phát triển, tích hợp và chuẩn bị vận hành. |
| QA | Lập kế hoạch kiểm thử, kiểm chứng chất lượng và hỗ trợ nghiệm thu. |

Vg là đại diện sản phẩm/trưởng nhóm, phụ trách kiến trúc tổng thể và lựa chọn
công nghệ, đồng thời trực tiếp lập trình cùng Sáng. Sáng phụ trách kỹ thuật
backend, phối hợp thiết kế chi tiết và phát triển theo kiến trúc tổng thể.
Thái thực hiện công việc dưới sự hướng dẫn của Vg. Phân công chi tiết
frontend, UX/UI và kiểm thử còn cần xác định tại
[hồ sơ nhân sự](04-team-and-discovery-plan.md).

## 7. Các vấn đề cần giải quyết

Các vấn đề về quy tắc nghiệp vụ, quyền truy cập, giới hạn phòng, trình duyệt,
chất lượng dịch vụ và trách nhiệm vận hành được theo dõi tại
[SCDC-LOG-001](03-discovery-and-decision-log.md). Các rủi ro về tiến độ, chi phí
và nguồn lực được quản lý tại [SCDC-EST-001](02-budget-and-assumptions.md).

## 8. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Xác lập phạm vi sơ bộ, mốc ba tháng và dự toán 500 triệu đồng. |
| 1.1 | 2026-09-27 | Chuẩn hóa vai trò và đầu ra giai đoạn khởi tạo. |
| 1.2 | 2026-09-27 | Tinh gọn tổng quan dự án, làm rõ kết quả bàn giao và dẫn chiếu nội dung chi tiết. |
| 1.3 | 2026-09-27 | Loại nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này; cập nhật SCP-005 đến SCP-007. |
| 1.4 | 2026-09-27 | Ghi nhận nhân sự toàn thời gian, nhu cầu đào tạo và yêu cầu đánh giá lại tiến độ, dự toán. |
| 1.5 | 2026-09-27 | Làm rõ vai trò kiến trúc, lựa chọn công nghệ và lập trình của Vg cùng trách nhiệm backend của Sáng. |

[Mục lục hồ sơ](../README.md)

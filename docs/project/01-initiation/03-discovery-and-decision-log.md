# SCDC — Quyết định và vấn đề cần làm rõ

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-LOG-001 |
| Phiên bản | 1.3 |
| Cập nhật | 2026-09-27 |
| Trạng thái | Đang cập nhật |

## 1. Quyết định hiện hành

| Mã | Quyết định | Căn cứ hoặc điều kiện áp dụng | Trạng thái |
|---|---|---|---|
| DEC-001 | Tái sử dụng thành phần sẵn có phù hợp với yêu cầu và kiến trúc mục tiêu. | Cần đánh giá chất lượng, khả năng tích hợp và công sức điều chỉnh trong thiết kế kỹ thuật. | Chưa xác định thành phần cụ thể |
| DEC-002 | Phục vụ nhóm bạn và cộng đồng không giới hạn chủ đề. | REQ-001. | Đã thống nhất |
| DEC-003 | Phát triển phiên bản đầu trên trình duyệt web. | REQ-003. | Đã thống nhất |
| DEC-004 | Hỗ trợ nhắn tin trong phòng thuộc server và tin riêng giữa hai người. | REQ-004, REQ-005; REQ-006 được loại khỏi phạm vi tại phiên bản 1.3. | Đã thống nhất |
| DEC-005 | Hỗ trợ phòng thoại trong server và gọi riêng giữa hai người, có video và chia sẻ màn hình. | REQ-007, REQ-008, REQ-009. | Đã thống nhất |
| DEC-006 | Phát hành công khai và cho phép người dùng tự đăng ký. | REQ-010. | Đã thống nhất |
| DEC-007 | Áp dụng kiến trúc microservices. | REQ-012; thiết kế xác định ranh giới dịch vụ và mô hình triển khai. | Đã thống nhất |
| DEC-008 | Dùng mốc khoảng 3 tháng từ ngày khởi động làm mục tiêu tiến độ. | REQ-011; cần kiểm tra AS-001, AS-002, AS-008 khi lập kế hoạch chi tiết. | Mục tiêu lập kế hoạch |
| DEC-009 | Dùng 500.000.000 VNĐ làm ngân sách dự kiến. | Dự toán COST-001 đến COST-005; các giả định và khoản loại trừ tại SCDC-EST-001. | Cơ sở lập kế hoạch |
| DEC-010 | Dùng bộ tiêu chí thành công sơ bộ làm đầu vào xây dựng điều kiện nghiệm thu. | SUC-001 đến SUC-006; cần cụ thể hóa trong đặc tả yêu cầu. | Đã thống nhất ở mức sơ bộ |

Tài liệu liên quan: [yêu cầu ban đầu](00-customer-request.md),
[Project Brief](01-project-brief.md), [dự toán và giả định](02-budget-and-assumptions.md).

## 2. Vấn đề cần làm rõ

| Mã | Nội dung cần xác định | Kết quả cần có | Thời điểm xử lý | Đầu mối dự kiến | Trạng thái |
|---|---|---|---|---|---|
| OQ-001 | Người dùng đại diện, hành trình ưu tiên, ngôn ngữ và khu vực sử dụng. | Hồ sơ người dùng và hành trình ưu tiên. | Khảo sát nhu cầu | Khách hàng, BA/UX | Chưa làm rõ |
| OQ-002 | Quy tắc đăng ký, xác thực, khôi phục tài khoản và quản lý phiên. | Luồng tài khoản và quy tắc nghiệp vụ. | Đặc tả yêu cầu | Khách hàng, BA, kỹ thuật | Chưa làm rõ |
| OQ-003 | Tạo/tham gia/rời server, lời mời, nhu cầu tìm cộng đồng và quyền riêng tư. | Luồng quản lý cộng đồng và điều kiện truy cập. | Đặc tả yêu cầu | Khách hàng, BA | Chưa làm rõ |
| OQ-004 | Vai trò và các thao tác được phép trong server/phòng. | Ma trận quyền. | Đặc tả yêu cầu | Khách hàng, BA | Chưa làm rõ |
| OQ-005 | Quy tắc gửi, nhận, lưu lịch sử; mất mạng, gửi lại; loại nội dung và tương tác. | Quy tắc nhắn tin và tiêu chí chấp nhận. | Đặc tả yêu cầu | Khách hàng, BA, UX, kỹ thuật | Chưa làm rõ |
| OQ-006 | Số người/phòng, chất lượng thoại/video, chia sẻ màn hình và mất kết nối. | Giới hạn sử dụng, hành vi cuộc gọi và tiêu chí chất lượng. | Đặc tả yêu cầu và thiết kế | Khách hàng, UX, kỹ sư media, QA | Chưa làm rõ |
| OQ-007 | Trình duyệt/thiết bị hỗ trợ, hiệu năng và cách nghiệm thu. | Ma trận hỗ trợ, chỉ tiêu chất lượng và phương pháp đo. | Đặc tả yêu cầu | Khách hàng, kỹ thuật, QA | Chưa làm rõ |
| OQ-008 | Ranh giới dịch vụ, dữ liệu, giải pháp media, triển khai và thành phần tái sử dụng. | Hồ sơ kiến trúc và đánh giá kỹ thuật. | Thiết kế kỹ thuật | Tech Lead, kỹ sư | Chưa thiết kế |
| OQ-009 | Nhân sự, ngày khởi động, tiến độ, báo giá và tính khả thi của dự toán. | Kế hoạch nguồn lực, lịch thực hiện và chi phí chi tiết. | Lập kế hoạch thực hiện | Khách hàng, quản lý dự án | Chưa xác định |
| OQ-010 | Mức sử dụng media, băng thông, lưu trữ và chi phí sau phát hành. | Mô hình chi phí vận hành. | Thiết kế vận hành | Khách hàng, kỹ thuật, vận hành | Chưa ước lượng chi tiết |
| OQ-011 | Quản trị nền tảng, xử lý vi phạm, hỗ trợ và lưu giữ dữ liệu. | Quy tắc quản trị và phân công vận hành. | Khảo sát và đặc tả yêu cầu | Khách hàng, BA, vận hành | Chưa làm rõ |
| OQ-012 | Vấn đề sử dụng ưu tiên, giá trị sản phẩm cần đem lại và cách đánh giá giá trị đó. | Mục tiêu sản phẩm và tiêu chí đánh giá có căn cứ từ khảo sát. | Khảo sát nhu cầu | Khách hàng, BA/UX | Chưa làm rõ |
| OQ-013 | Đầu mối khách hàng và bên phát triển, thẩm quyền xác nhận, người tham gia và lịch khảo sát. | Danh sách đầu mối và kế hoạch khảo sát ban đầu. | Khởi tạo | Khách hàng, quản lý dự án | Chưa xác định |

Các vai trò sẽ được gán cho nhân sự cụ thể trong kế hoạch nguồn lực. Khi
đóng một vấn đề, bổ sung kết luận, ngày xử lý và tài liệu chứa kết quả.

## 3. Điều chỉnh phạm vi ngày 2026-09-27

| Nội dung | Kết luận |
|---|---|
| Thay đổi đã xác nhận | Loại nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này. |
| Phạm vi giao tiếp còn lại | Phòng nhắn tin và phòng thoại trong server; nhắn tin và gọi riêng giữa hai người; video và chia sẻ màn hình trong các ngữ cảnh gọi này. |
| Yêu cầu và phạm vi bị ảnh hưởng | Loại REQ-006; cập nhật REQ-008, REQ-009, SCP-005, SCP-006, SCP-007 và DEC-004, DEC-005. |
| Ảnh hưởng cần đánh giá | Giảm các luồng nhóm riêng trong nghiệp vụ, UX/UI, kỹ thuật và kiểm thử; cần ước lượng lại công sức. |
| Ngân sách và tiến độ | Tiếp tục dùng mốc dự trù 500.000.000 VNĐ và khoảng 3 tháng; chưa xác định mức điều chỉnh sau thay đổi phạm vi. |

## 4. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Ghi nhận quyết định khởi tạo và các vấn đề cần làm rõ. |
| 1.1 | 2026-09-27 | Liên kết căn cứ quyết định với yêu cầu và dự toán. |
| 1.2 | 2026-09-27 | Làm rõ trạng thái quyết định, đầu ra xử lý vấn đề; chuyển quy định quản lý hồ sơ sang tài liệu quy trình. |
| 1.3 | 2026-09-27 | Cập nhật phạm vi giao tiếp; ghi ảnh hưởng thay đổi và bổ sung các vấn đề cần làm rõ khi khởi tạo, khảo sát. |

[Mục lục hồ sơ](../README.md)

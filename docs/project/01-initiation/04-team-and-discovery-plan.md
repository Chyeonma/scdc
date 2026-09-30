# SCDC — Nhân sự và chuẩn bị khảo sát

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-ORG-001 |
| Phiên bản | 0.5 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Đã ghi nhận nhân sự; kế hoạch khảo sát đang xây dựng |
| Liên quan | OQ-009, OQ-013 tại [sổ quyết định](03-discovery-and-decision-log.md) |

## 1. Nhân sự đã xác định

| Thành viên | Vai trò đã xác định | Khả năng tham gia | Cơ sở phân công |
|---|---|---|---|
| Vg | Trưởng nhóm, đại diện sản phẩm; phụ trách kiến trúc tổng thể, lựa chọn công nghệ, thiết kế UX/UI và trực tiếp lập trình; hướng dẫn Thái. | Toàn thời gian. | Thiết kế hệ thống và UX/UI, lựa chọn công nghệ, phát triển phần mềm cùng Sáng; điều phối công việc, xác nhận yêu cầu và phạm vi, hướng dẫn thành viên mới. |
| Sáng | Phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg. | Toàn thời gian. | Thiết kế chi tiết và phát triển backend theo kiến trúc tổng thể; phối hợp với Vg trong các quyết định kỹ thuật. |
| Thái | Phụ trách frontend và kiểm thử; thành viên mới được Vg hướng dẫn. | Toàn thời gian. | Phát triển giao diện theo thiết kế UX/UI của Vg và thực hiện kiểm thử; cần đào tạo, chia nhỏ công việc và rà soát kết quả. |

Khả năng tham gia toàn thời gian là cơ sở bố trí lịch. Công sức thực hiện cần
tính riêng cho điều phối, phân tích, thiết kế, đào tạo, phát triển và kiểm thử.
Nhóm hiện tại chưa đáp ứng giả định ba kỹ sư có kinh nghiệm trong dự toán ban đầu.

## 2. Phương án phối hợp đề xuất

| Hạng mục | Phụ trách đề xuất | Cách phối hợp |
|---|---|---|
| Làm rõ yêu cầu và tổ chức khảo sát | Vg | Sáng góp ý tính khả thi; Thái hỗ trợ ghi nhận và tổng hợp theo biểu mẫu. |
| Kiến trúc tổng thể và lựa chọn công nghệ | Vg | Sáng góp ý khả năng triển khai, các ràng buộc backend và tích hợp. |
| Thiết kế chi tiết backend | Sáng | Phối hợp với Vg để bảo đảm phù hợp với kiến trúc tổng thể và công nghệ đã lựa chọn. |
| Lập trình và tích hợp | Vg, Sáng và Thái | Vg và Sáng trực tiếp lập trình; Thái triển khai frontend dưới sự hướng dẫn của Vg. Phân chia công việc cụ thể khi có thiết kế và phối hợp tích hợp, rà soát mã nguồn. |
| Đào tạo thành viên mới | Vg | Chia công việc nhỏ, mô tả đầu ra và hướng dẫn cách tự kiểm tra; Sáng hỗ trợ rà soát phần kỹ thuật liên quan. |
| Thiết kế UX/UI | Vg | Xác định luồng sử dụng, màn hình và trạng thái giao diện để Thái triển khai frontend. |
| Frontend | Thái | Triển khai theo thiết kế của Vg; Vg hướng dẫn và rà soát, Sáng hỗ trợ tích hợp backend. |
| Kiểm thử | Thái | Thực hiện theo tiêu chí chấp nhận và kịch bản kiểm thử, ghi nhận lỗi và kiểm tra lại sau sửa; Vg hoặc Sáng rà soát kịch bản và kết quả, đặc biệt với phần frontend do Thái phát triển. |

Đã xác định Vg phụ trách UX/UI, Thái phụ trách frontend và kiểm thử. Cách
phối hợp trong bảng là đề xuất để lập kế hoạch chi tiết. Công sức học,
hướng dẫn, phát triển, kiểm thử và rà soát cần được phân bổ cụ thể; sản lượng
độc lập của Thái chưa được dùng làm cơ sở cam kết tiến độ.

## 3. Tình trạng chuẩn bị khảo sát

| Nội dung | Tình trạng |
|---|---|
| Người tham gia khảo sát | Chưa có người được xác định hoặc xác nhận tham gia. |
| Khả năng bố trí thời gian | Nhóm có thể sắp xếp thời gian linh hoạt. |
| Lịch khảo sát cụ thể | Chưa xác lập; phụ thuộc phương án tiếp cận và lịch của người tham gia. |
| Đầu vào từ đại diện sản phẩm | Ba nhu cầu tại SCDC-REQ-001; cập nhật 2026-09-30: vấn đề trôi tin nhắn/tài liệu, ưu tiên DM trước, cộng đồng có nhiều phòng theo chủ đề và tham gia qua liên kết mời hoặc tìm kiếm. Chi tiết tại SCDC-DIS-001. |
| Nội dung cần làm rõ | Người dùng khảo sát và chi tiết hành trình ưu tiên (OQ-001), quy tắc hiển thị/tư cách tham gia cộng đồng (OQ-003), giá trị sản phẩm và cách đánh giá (OQ-012). |
| Kết quả khảo sát | Chưa có. |

Đầu vào làm rõ nhu cầu và các câu hỏi tiếp theo được quản lý tại
[SCDC-DIS-001](../02-discovery/01-users-and-needs.md).

Đề xuất cho đợt đầu là tìm 3–5 người đang tham gia nhóm bạn hoặc cộng đồng,
trao đổi khoảng 20–30 phút mỗi người. Phương án này cần xác nhận trước khi
lập lịch và phân công thực hiện.

Mục đích là kiểm chứng các giả định về nhu cầu và tình huống sử dụng. Có thể
làm rõ yêu cầu với đại diện sản phẩm trước, sau đó đánh giá bản thiết kế
tương tác với người dùng. Phương pháp và thời điểm lấy phản hồi chưa được chốt.

## 4. Công việc chuẩn bị đề xuất

| Công việc | Kết quả cần có | Điều kiện thực hiện |
|---|---|---|
| Chuẩn bị nội dung khảo sát | Mục tiêu, nhóm nội dung cần tìm hiểu và biểu mẫu ghi nhận. | Sau khi thống nhất phương án khảo sát. |
| Tìm và mời người tham gia | Danh sách người nhận lời và thời gian có thể tham gia. | Có đầu mối phụ trách tiếp cận. |
| Xếp lịch và phân công | Lịch trao đổi, người chủ trì và người ghi nhận cho từng buổi. | Người tham gia đã xác nhận. |
| Tổng hợp kết quả | Các nhu cầu, tình huống sử dụng, khác biệt và giả định còn mở. | Có ghi nhận từ các buổi khảo sát. |

Các nhận định nội bộ được quản lý như giả định sản phẩm cho đến khi có bằng
chứng kiểm chứng. OQ-013 còn mở đối với phương án tiếp cận và lịch khảo sát.

## 5. Ảnh hưởng đến kế hoạch

- Cập nhật AS-001 theo cơ cấu nhân sự hiện có.
- Tính thời gian hướng dẫn, học và rà soát vào công sức của nhóm.
- Phân bổ công sức UX/UI của Vg, frontend và kiểm thử của Thái; làm rõ công
  việc phân tích nghiệp vụ khi lập lịch chi tiết.
- Đánh giá lại mốc ba tháng và dự toán; chi tiết tại
  [SCDC-EST-001](02-budget-and-assumptions.md).

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-27 | Ghi nhận ba thành viên toàn thời gian, nhu cầu hướng dẫn và tình trạng chuẩn bị khảo sát. |
| 0.2 | 2026-09-27 | Làm rõ trách nhiệm kiến trúc tổng thể, lựa chọn công nghệ và lập trình của Vg; phân định trách nhiệm backend và phối hợp phát triển với Sáng. |
| 0.3 | 2026-09-27 | Ghi nhận Vg phụ trách UX/UI, Thái phụ trách frontend và kiểm thử; cập nhật đầu vào làm rõ nhu cầu và công sức kiêm nhiệm. |
| 0.4 | 2026-09-30 | Dẫn chiếu đầu vào làm rõ vấn đề, ưu tiên nhắn tin riêng và tổ chức phòng theo chủ đề tại SCDC-DIS-001. |
| 0.5 | 2026-09-30 | Cập nhật hai cách tham gia cộng đồng đã được lựa chọn và nội dung khảo sát còn mở. |

[Mục lục hồ sơ](../README.md)

# SCDC — Nhân sự và phương án làm rõ nhu cầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-ORG-001 |
| Phiên bản | 0.6 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Đã ghi nhận nhân sự; không khảo sát người dùng bên ngoài trong đợt hiện tại |
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
| Làm rõ yêu cầu với đại diện sản phẩm | Vg | Sáng góp ý tính khả thi; Thái hỗ trợ ghi nhận, đối chiếu với giao diện và tiêu chí kiểm thử. |
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

## 3. Phương án làm rõ nhu cầu trong đợt hiện tại

| Nội dung | Tình trạng |
|---|---|
| Khảo sát người dùng bên ngoài | Không thực hiện trong đợt hiện tại theo DEC-030. |
| Người tham gia và lịch khảo sát | Không tuyển người hoặc lập lịch trong đợt hiện tại. |
| Đầu vào từ đại diện sản phẩm | Ba nhu cầu tại SCDC-REQ-001; cập nhật 2026-09-30: vấn đề trôi tin nhắn/tài liệu, ưu tiên DM trước, cộng đồng có nhiều phòng theo chủ đề và tham gia qua liên kết mời hoặc tìm kiếm. Chi tiết tại SCDC-DIS-001. |
| Nội dung cần làm rõ | Người dùng đại diện cụ thể và chi tiết hành trình ưu tiên (OQ-001), quy tắc cộng đồng (OQ-003), giá trị sản phẩm và cách đánh giá (OQ-012). |
| Bằng chứng từ người dùng bên ngoài | Chưa có; đầu vào hiện tại do đại diện sản phẩm cung cấp. |

Đầu vào làm rõ nhu cầu và các câu hỏi tiếp theo được quản lý tại
[SCDC-DIS-001](../02-discovery/01-users-and-needs.md).

Phương án phỏng vấn 3–5 người từng được đề xuất nhưng không áp dụng trong
đợt hiện tại. [Bộ câu hỏi](../02-discovery/02-interview-guide.md) được
giữ làm tài liệu dự phòng nếu dự án quyết định khảo sát về sau. Những nhận
định về người dùng chưa có bằng chứng bên ngoài tiếp tục được đánh dấu là
giả định trong SCDC-DIS-001.

## 4. Công việc làm rõ nhu cầu tiếp theo

| Công việc | Kết quả cần có | Điều kiện thực hiện |
|---|---|---|
| Làm rõ quy tắc còn mở với đại diện sản phẩm | Kết luận hoặc giả định có đầu mối tại SCDC-LOG-001. | Các câu hỏi ảnh hưởng đến hành trình và tiêu chí chấp nhận đã được xác định. |
| Rà soát bản nháp đặc tả | Hành vi, ngoại lệ và tiêu chí chấp nhận nhất quán với phạm vi đã thống nhất. | Có đầu vào trong SCDC-DIS-001 và quyết định tương ứng. |
| Lập kế hoạch kiểm thử và nghiệm thu | Kịch bản kiểm tra chức năng, phân quyền, mất kết nối và kết quả mong đợi. | Quy tắc đủ rõ để QA viết kịch bản. |

Các nhận định nội bộ được quản lý như giả định sản phẩm cho đến khi có bằng
chứng kiểm chứng. OQ-013 được xử lý cho đợt này bằng DEC-030; OQ-001 và
OQ-012 vẫn mở về người dùng đại diện và cách đánh giá giá trị sản phẩm.

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
| 0.6 | 2026-09-30 | Ghi nhận quyết định không khảo sát bên ngoài trong đợt hiện tại và chuyển sang làm rõ yêu cầu, rà soát đặc tả. |

[Mục lục hồ sơ](../README.md)

# SCDC — Dự toán và giả định

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-EST-001 |
| Phiên bản | 1.3 |
| Cập nhật | 2026-09-27 |
| Trạng thái | Dự toán sơ bộ phục vụ lập kế hoạch |
| Căn cứ | [Phạm vi dự án — SCDC-BRF-001](01-project-brief.md) |

## 1. Tổng mức dự toán

**500.000.000 VNĐ** cho thời gian thực hiện dự kiến khoảng 3 tháng, bao gồm
nhân sự, hạ tầng/công cụ trong thời gian dự án và dự phòng.

Dự toán sử dụng đơn giá và công sức ước tính. Giá trị hợp đồng và kế hoạch
ngân sách chi tiết cần được xác lập sau khi hoàn thiện yêu cầu, giải pháp
kỹ thuật và phương án nguồn lực.

Phạm vi phiên bản 1.3 đã loại nhóm chat riêng ngoài server và cuộc gọi trong
loại nhóm này. Chưa có ước lượng chi tiết về phần công sức giảm; mức
500.000.000 VNĐ tiếp tục là mốc dự trù để đối chiếu khi cập nhật chi phí và
tiến độ theo phạm vi mới.

## 2. Chi phí dự kiến

| Mã | Hạng mục | Cơ sở tính | Thành tiền |
|---|---|---|---:|
| COST-001 | Phát triển và kỹ thuật | 3 kỹ sư × 3 tháng × 35.000.000 VNĐ/người/tháng | 315.000.000 VNĐ |
| COST-002 | QA/kiểm thử | 2 tháng công × 25.000.000 VNĐ/tháng công | 50.000.000 VNĐ |
| COST-003 | Phân tích nghiệp vụ và UX/UI | 1 tháng công × 30.000.000 VNĐ/tháng công | 30.000.000 VNĐ |
| COST-004 | Hạ tầng và công cụ | Khoản dự trù trong thời gian phát triển, kiểm thử và phát hành ban đầu | 20.000.000 VNĐ |
| COST-005 | Dự phòng | Công việc phát sinh, tích hợp, kiểm thử và chênh lệch chi phí | 85.000.000 VNĐ |
| | **Tổng trước dự phòng** | | **415.000.000 VNĐ** |
| | **Tổng dự toán** | | **500.000.000 VNĐ** |

Đơn giá nhân sự là đầu vào ước tính, cần được thay bằng đơn giá cung cấp thực
tế khi xác lập kế hoạch chi phí.

## 3. Nguồn lực dự kiến

- Ba kỹ sư làm toàn thời gian trong ba tháng. Một kỹ sư kiêm Tech Lead,
  điều phối và chuẩn bị triển khai.
- QA tham gia từ giai đoạn yêu cầu, tổng công sức tương đương hai tháng.
- BA/UX/UI có tổng công sức tương đương một tháng, phân bổ theo nhu cầu phân
  tích, thiết kế và rà soát.

Phân công cụ thể và khả năng kiêm nhiệm được kiểm tra khi lập kế hoạch công việc.

## 4. Cơ sở ước lượng hạ tầng

Khoản COST-004 bao gồm tài nguyên chạy ứng dụng, cơ sở dữ liệu, xử lý
thoại/video, email hệ thống, lưu trữ, sao lưu, giám sát và công cụ phát triển.
Nhà cung cấp và cấu hình chưa được lựa chọn.

Để xác lập chi phí chi tiết cần có:

- Số môi trường và tài nguyên tính toán cho từng môi trường.
- Tổng thời lượng tham gia gọi, tỷ lệ sử dụng video/chia sẻ màn hình và
  lưu lượng truyền tải hằng tháng.
- Dung lượng dữ liệu, thời hạn lưu trữ và chính sách sao lưu.
- Đơn giá dịch vụ, hạn mức đi kèm, chi phí vượt hạn mức và tỷ giá thanh toán.

Khoản 20.000.000 VNĐ là dự trù ban đầu, cần kiểm tra lại bằng cấu hình và mức
sử dụng cụ thể trước khi lựa chọn dịch vụ.

## 5. Giả định lập kế hoạch

Các giả định dưới đây đang chờ kiểm chứng. Chúng là đầu vào ước tính công
sức và chi phí, chưa phải cam kết năng lực hoặc hạn mức đăng ký của sản phẩm.

| Mã | Giả định | Cách kiểm chứng | Đầu mối dự kiến |
|---|---|---|---|
| AS-001 | Có 3 kỹ sư có kinh nghiệm làm toàn thời gian trong 3 tháng. | Xác nhận nhân sự, năng lực và lịch phân bổ. | Quản lý dự án, Tech Lead |
| AS-002 | QA 2 tháng công và BA/UX/UI 1 tháng công đáp ứng khối lượng công việc. | Ước lượng theo yêu cầu, màn hình và kế hoạch kiểm thử. | BA/UX/UI, QA, quản lý dự án |
| AS-003 | Tích hợp nền tảng media có sẵn cho thoại/video và chia sẻ màn hình. | Đánh giá tính phù hợp, khả năng tích hợp, chi phí và vận hành. | Tech Lead, kỹ sư media |
| AS-004 | Quy mô dự trù 1.000 tài khoản. | Xác định nhóm người dùng ban đầu và nhu cầu lưu trữ. | Khách hàng, BA |
| AS-005 | Có khoảng 100 người trực tuyến đồng thời trên toàn hệ thống. | Xác định nhu cầu tải dự kiến; kiểm chứng khả năng đáp ứng bằng kiểm thử tải. | Khách hàng, Tech Lead, QA |
| AS-006 | Có khoảng 20 người tham gia gọi đồng thời trên toàn hệ thống. | Xác định kiểu phòng, số người/phòng và mức sử dụng; kiểm thử media theo kịch bản tương ứng. | Khách hàng, kỹ sư media, QA |
| AS-007 | Hạ tầng và công cụ cần khoảng 20.000.000 VNĐ trong thời gian dự án. | Lập mô hình chi phí theo cấu hình, mức sử dụng và báo giá. | Phụ trách vận hành |
| AS-008 | Phạm vi có thể hoàn thành trong khoảng 3 tháng. | Ước lượng công việc, phụ thuộc và lịch nguồn lực sau phân tích, thiết kế. | Quản lý dự án, nhóm kỹ thuật |

Khả năng tiết kiệm từ tái sử dụng mã nguồn được tính vào dự toán sau khi có
kết quả đánh giá kỹ thuật.

## 6. Các khoản chưa bao gồm

- Marketing và thu hút người dùng.
- Thuế và phí phát sinh theo điều kiện hợp đồng.
- Vận hành, hỗ trợ và bảo trì dài hạn sau thời gian dự án.
- Tính năng và nền tảng ngoài phạm vi phiên bản đầu.

## 7. Rủi ro

| Mã | Rủi ro | Ảnh hưởng | Biện pháp xử lý | Đầu mối dự kiến |
|---|---|---|---|---|
| RSK-001 | Phòng thoại trong server và gọi riêng hai người có video/chia sẻ màn hình; yêu cầu chất lượng chưa đầy đủ. | Tăng công sức tích hợp, kiểm thử và kéo dài tiến độ. | Làm rõ ma trận chức năng, giới hạn và điều kiện nghiệm thu trước khi ước lượng chi tiết. | Tech Lead, QA |
| RSK-002 | Chưa xác định lượng sử dụng media và quy mô phòng. | Chi phí hạ tầng vượt dự trù. | Tính chi phí theo thời lượng, dữ liệu truyền tải và theo dõi mức sử dụng. | Phụ trách vận hành |
| RSK-003 | Kiến trúc microservices phát sinh phụ thuộc giữa các dịch vụ. | Tăng công sức triển khai, kiểm thử tích hợp và vận hành. | Xác định ranh giới dịch vụ, trách nhiệm dữ liệu và công việc vận hành trong kế hoạch. | Tech Lead |
| RSK-004 | Chưa chốt nhân sự, trình duyệt hỗ trợ và trách nhiệm quản trị nền tảng công khai. | Phải điều chỉnh tiến độ hoặc phạm vi bàn giao. | Giải quyết các vấn đề liên quan trước khi xác nhận kế hoạch thực hiện. | Quản lý dự án, khách hàng |

## 8. Điều kiện cập nhật dự toán

Cập nhật khi có thay đổi phạm vi, thời gian, nguồn lực, giải pháp kỹ thuật,
mức sử dụng hoặc đơn giá dịch vụ. Mỗi điều chỉnh cần thể hiện cơ sở tính,
chênh lệch và tác động đến tiến độ; khách hàng xác nhận các thay đổi làm tăng
ngân sách hoặc thay đổi phạm vi bàn giao.

## 9. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Lập dự toán 500 triệu đồng, cơ cấu chi phí, giả định và rủi ro. |
| 1.1 | 2026-09-27 | Chuẩn hóa căn cứ và mô tả giả định. |
| 1.2 | 2026-09-27 | Làm rõ đơn vị công sức, đầu vào tính hạ tầng và trách nhiệm xử lý rủi ro; giữ nguyên tổng dự toán. |
| 1.3 | 2026-09-27 | Ghi nhận phạm vi giao tiếp thu hẹp và yêu cầu ước lượng lại; cập nhật rủi ro media. |

[Mục lục hồ sơ](../README.md)

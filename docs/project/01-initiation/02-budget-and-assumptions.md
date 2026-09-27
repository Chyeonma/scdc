# SCDC — Dự toán và giả định

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-EST-001 |
| Phiên bản | 1.5 |
| Cập nhật | 2026-09-27 |
| Trạng thái | Cần ước lượng lại theo cơ cấu nhân sự đã xác định |
| Căn cứ | [Phạm vi dự án — SCDC-BRF-001](01-project-brief.md) |

## 1. Tình trạng dự toán

**Dự toán ban đầu: 500.000.000 VNĐ**, cho thời gian mục tiêu khoảng 3 tháng,
bao gồm nhân sự, hạ tầng/công cụ và dự phòng.

Giả định ba kỹ sư có kinh nghiệm (AS-001) không còn phù hợp với đội ngũ đã
xác định: Vg, Sáng và Thái tham gia toàn thời gian, trong đó Thái cần đào tạo
dưới sự hướng dẫn của Vg. Phân công kiêm nhiệm BA/UX/UI và QA chưa hoàn thiện.
Xem [SCDC-ORG-001](04-team-and-discovery-plan.md).

Cần lập lại ước lượng theo công việc, năng lực, thời gian hướng dẫn và các
vai trò kiêm nhiệm; đồng thời tính ảnh hưởng của việc loại nhóm chat riêng
ngoài server. Chưa có dự toán điều chỉnh. Các khoản dưới đây được giữ làm
mốc đối chiếu với dự toán ban đầu.

## 2. Cơ cấu dự toán ban đầu

| Mã | Hạng mục | Cơ sở tính | Thành tiền |
|---|---|---|---:|
| COST-001 | Phát triển và kỹ thuật | 3 kỹ sư × 3 tháng × 35.000.000 VNĐ/người/tháng | 315.000.000 VNĐ |
| COST-002 | QA/kiểm thử | 2 tháng công × 25.000.000 VNĐ/tháng công | 50.000.000 VNĐ |
| COST-003 | Phân tích nghiệp vụ và UX/UI | 1 tháng công × 30.000.000 VNĐ/tháng công | 30.000.000 VNĐ |
| COST-004 | Hạ tầng và công cụ | Khoản dự trù trong thời gian phát triển, kiểm thử và phát hành ban đầu | 20.000.000 VNĐ |
| COST-005 | Dự phòng | Công việc phát sinh, tích hợp, kiểm thử và chênh lệch chi phí | 85.000.000 VNĐ |
| | **Tổng trước dự phòng** | | **415.000.000 VNĐ** |
| | **Tổng dự toán** | | **500.000.000 VNĐ** |

Các đơn giá và tháng công trong bảng thuộc phương án ước tính ban đầu, chưa
phản ánh chi phí của từng thành viên hiện tại. Đơn giá thực tế và phương án
tính công sức kiêm nhiệm cần được xác định khi cập nhật dự toán.

## 3. Nguồn lực và công sức cần ước lượng lại

- Ba thành viên có thể dành toàn thời gian cho dự án.
- Vg phụ trách kiến trúc tổng thể, lựa chọn công nghệ và trực tiếp lập trình,
  đồng thời điều phối, đại diện sản phẩm và hướng dẫn Thái; cần phân bổ
  thời gian cho từng trách nhiệm trong quỹ thời gian của một người.
- Sáng phụ trách kỹ thuật backend và trực tiếp lập trình cùng Vg; cần tính
  cả thiết kế chi tiết, tích hợp và rà soát bên cạnh phát triển tính năng.
- Thái là thành viên mới; công sức độc lập được đánh giá theo kết quả học
  và hoàn thành công việc có hướng dẫn.
- QA 2 tháng công và BA/UX/UI 1 tháng công là giả định khối lượng từ dự toán
  ban đầu; chưa có nhân sự riêng được xác định cho các vai trò này.

Khi lập lịch, tổng phân bổ của mỗi người phải bao gồm công việc kiêm nhiệm,
học, hướng dẫn và rà soát; tránh tính cùng một khoảng thời gian cho nhiều vai trò.

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

AS-001 đã được đối chiếu với cơ cấu nhân sự hiện tại và không còn phù hợp.
Các giả định còn lại đang chờ kiểm chứng. Chúng là đầu vào ước tính công sức
và chi phí, chưa phải cam kết năng lực hoặc hạn mức đăng ký của sản phẩm.

| Mã | Giả định | Cách kiểm chứng | Đầu mối dự kiến |
|---|---|---|---|
| AS-001 | Giả định ban đầu: có 3 kỹ sư có kinh nghiệm làm toàn thời gian trong 3 tháng. | Không còn phù hợp: đội ngũ gồm 3 thành viên toàn thời gian, có 1 thành viên mới cần hướng dẫn; phải ước lượng lại theo phân công và năng lực. | Vg, Sáng |
| AS-002 | QA 2 tháng công và BA/UX/UI 1 tháng công đáp ứng khối lượng công việc. | Ước lượng theo yêu cầu, màn hình, kế hoạch kiểm thử và năng lực kiêm nhiệm của nhóm. | Vg, đầu mối BA/UX/UI và QA khi được phân công |
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
| RSK-004 | Chưa chốt phân công frontend, UX/UI, QA, trình duyệt hỗ trợ và trách nhiệm quản trị nền tảng công khai. | Phải điều chỉnh tiến độ hoặc phạm vi bàn giao. | Xác định trách nhiệm, năng lực và khối lượng kiêm nhiệm trước khi xác nhận kế hoạch thực hiện. | Vg, Sáng |
| RSK-005 | Thời gian đào tạo, hướng dẫn và rà soát của thành viên mới chưa được lượng hóa. | Giảm thời gian dành cho các công việc khác và tăng độ bất định của tiến độ. | Chia công việc nhỏ, bố trí thời gian hướng dẫn và đánh giá công sức theo kết quả thực hiện. | Vg, Sáng |

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
| 1.4 | 2026-09-27 | Ghi nhận AS-001 không còn phù hợp; giữ dự toán ban đầu để đối chiếu và bổ sung công sức đào tạo, kiêm nhiệm khi ước lượng lại. |
| 1.5 | 2026-09-27 | Bổ sung công sức kiến trúc, lựa chọn công nghệ và lập trình của Vg vào cơ sở phân bổ nguồn lực. |

[Mục lục hồ sơ](../README.md)

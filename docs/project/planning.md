# SCDC — Kế hoạch và nguồn lực

Nguồn chuẩn cho nhân sự, công suất, COST/AS/RSK, dự toán và lịch theo giai đoạn. Phạm vi sản phẩm tại [tổng quan](README.md); phạm vi từng mốc tại [MVP](../releases/mvp.md) và [v1](../releases/v1.md).

<a id="team"></a>

## Nhân sự và phối hợp

### Phân công thực hiện hiện hành

Theo [DEC-117](../decisions.md#dec-117), phân công được cập nhật để mỗi người có đầu ra trọn vẹn và cân tải theo từng giai đoạn. Nhóm dùng AI hỗ trợ; gói việc bao gồm DB, backend, frontend, kiểm thử và tích hợp cần thiết. Vai trò frontend/QA riêng cho Thái và backend chung cho Sáng trước đây được thay bằng phân công dưới đây.

| Thành viên | MVP — một host Modular Monolith | v1 — microservice và bản hoàn thiện | Khả năng tham gia |
|---|---|---|---|
| Vg | Trưởng nhóm/đại diện sản phẩm; chốt quyết định quan trọng, hoàn thiện Identity gần xong và phụ trách Community từ DB tới frontend | Chủ trì ranh giới service và chính sách quyền/phiên/media; chuyển Identity, hoàn thiện Community và review phần rủi ro cao | 3–4 ngày/tuần theo DEC-088 |
| Sáng | Phụ trách Direct Messaging từ DB tới frontend; nền lưu/gửi/nhận/lịch sử dùng chung cho tin phòng | Chuyển Messaging và các hợp đồng liên quan; hoàn thiện nhắn tin, backend và frontend điều khiển lifecycle cuộc gọi | 3–4 ngày/tuần theo DEC-088 |
| Thái | Phụ trách Email Worker; bộ chạy Compose/config/CI, dữ liệu demo và công cụ kiểm tra tích hợp | Hiện thực môi trường nhiều service, định tuyến/CI; tích hợp provider/SDK media, thiết bị và hiển thị nguồn theo hợp đồng đã chốt; hoàn thiện worker/công cụ vận hành | 3–4 ngày/tuần theo DEC-088 |

Vg giữ thẩm quyền chốt phạm vi và quyết định kỹ thuật quan trọng. Phân công phát triển không tự chọn người ký nghiệm thu/mở công khai hoặc cấp quyền production; các nội dung này vẫn thuộc DEC-111/OQ-011.

### Ranh giới phối hợp

| Hạng mục | Người chịu trách nhiệm | Cách phối hợp để tránh dồn việc |
|---|---|---|
| Sản phẩm và kiến trúc | Vg | Chốt quy tắc, hợp đồng và lựa chọn quan trọng; Sáng/Thái tự xử lý chi tiết trong gói đã thống nhất, chỉ đưa lên các thay đổi ảnh hưởng phạm vi/quyền/dữ liệu/tích hợp |
| Identity và Community | Vg | Sáng cung cấp nền Messaging cho tin phòng; Thái cung cấp email worker và môi trường thử. Tận dụng code/UI đã có, tính công sức review/hướng dẫn trong quỹ của Vg |
| DM và tin phòng | Sáng | Sở hữu nền Messaging và UI DM; Vg cung cấp quyền/lifecycle phòng và làm UI Community. Không xây hai cơ chế nhắn tin khác nhau |
| Email và công cụ chạy hệ thống | Thái | Identity giữ logic cấp/consume token, hiệu lực link và dữ liệu job; Thái hiện thực delivery/retry và công cụ theo hợp đồng/policy Vg chốt |
| Microservice ở v1 | Cả ba, Vg chủ trì thiết kế | Vg: ranh giới/Identity/quyền; Sáng: Messaging/hợp đồng/dữ liệu; Thái: container/config/định tuyến/CI. Việc chuyển Community được xếp trong quỹ Vg hoặc điều chuyển một gói triển khai đã thiết kế rõ khi quá tải |
| Media ở v1 | Chia theo trách nhiệm | Vg: quyền/admission/quota/thu hồi và review; Sáng: lifecycle gọi riêng/phòng và trạng thái điều khiển; Thái: adapter provider/SDK, mic/camera/share, hiển thị nguồn và môi trường. Chốt hợp đồng trước khi làm; mỗi phần tự kiểm thử |
| Kiểm thử và hồi quy | Người sở hữu gói; người khác kiểm tra lại | Vg thử Identity/Community, Sáng thử Messaging/lifecycle, Thái thử worker/adapter/môi trường và duy trì bộ chạy. Vg/Sáng viết kỳ vọng quyền/đồng thời; Thái chuẩn bị dữ liệu và tổng hợp kết quả, không chịu một mình chất lượng cả sản phẩm |

Thái nhận gói có đầu vào/đầu ra và cách kiểm chứng rõ; thời gian học và sửa lại được tính trong công suất. Không giao cho Thái tự quyết định nghiệp vụ quyền hoặc lifecycle cuộc gọi chỉ vì dùng AI. Vg review các thay đổi nhạy cảm, Sáng kiểm tra luồng tích hợp liên quan; mỗi người phải chạy và giải thích kết quả của gói mình làm.

Cơ sở nguồn lực giữ 3–4 ngày/tuần; 8 giờ/ngày và 3,5 ngày/tuần chỉ là baseline giả định. Phân bổ 80% công suất cho công việc đã xếp, giữ 20% dự phòng; xem [cân tải theo giai đoạn](#capacity). Tính thiết kế, học/hướng dẫn, kiểm thử, review và sửa lỗi trong gói tương ứng, không cộng thêm ngoài quỹ thời gian.

### Công việc làm rõ nhu cầu tiếp theo

| Công việc | Kết quả cần có | Điều kiện thực hiện |
|---|---|---|
| Làm rõ quy tắc còn mở với đại diện sản phẩm | Kết luận hoặc giả định có đầu mối tại SCDC-LOG-001. | Các câu hỏi ảnh hưởng đến hành trình và tiêu chí chấp nhận đã được xác định. |
| Rà soát bản nháp đặc tả | Hành vi, ngoại lệ và tiêu chí chấp nhận nhất quán với phạm vi đã thống nhất. | Có đầu vào trong SCDC-DIS-001 và quyết định tương ứng. |
| Lập kế hoạch kiểm thử và nghiệm thu | Kịch bản kiểm tra chức năng, phân quyền, mất kết nối và kết quả mong đợi. | Quy tắc đủ rõ để QA viết kịch bản. |

Các nhận định nội bộ được quản lý như giả định sản phẩm cho đến khi có bằng
chứng kiểm chứng. OQ-013 được xử lý cho đợt này bằng DEC-030; OQ-001 và
OQ-012 vẫn mở về người dùng đại diện và cách đánh giá giá trị sản phẩm.

### Ảnh hưởng đến kế hoạch

- Cập nhật AS-001 theo cơ cấu nhân sự hiện có.
- Tính thời gian hướng dẫn, học và rà soát vào công sức của nhóm.
- Tính DB/backend/frontend và tự kiểm thử trong gói tính năng của Vg/Sáng;
  Thái sở hữu worker/công cụ/provider và tự kiểm chứng các đầu ra đó.
- Chỉ phân bổ 80% công suất theo giai đoạn; giữ 20% dự phòng, tính cả
  review/hướng dẫn của Vg và thời gian học/sửa lại của Thái.
- Đánh giá lại mốc ba tháng và dự toán; chi tiết tại
  [SCDC-EST-001](#budget).

<a id="budget"></a>

## Ngân sách, giả định và rủi ro

### Mô hình ngân sách minh họa cập nhật 2026-10-04

Theo DEC-088, ngân sách mang tính tượng trưng và được phép điều chỉnh để trình bày rõ. Mô hình **250.000.000 VNĐ** dưới đây do agent ước lượng cho phương án **16 tuần** trước khi tách MVP/v1 và cân lại phân công; chỉ giữ làm cơ sở dự toán cũ. Đơn giá chưa được các thành viên cung cấp, không phải mức lương thị trường/báo giá hoặc nghĩa vụ thanh toán. Mốc 500 triệu giữ làm lịch sử. Lịch mới cần ước lượng theo giai đoạn và kết quả thực tế tại [kế hoạch](#delivery); không đổi phạm vi v1 hoặc tự loại media.

| Hạng mục | Cơ sở minh họa | Số tiền |
|---|---|---:|
| Vg | 4 tháng × 20 triệu/tháng; gồm sản phẩm, kiến trúc, UX, code và hướng dẫn | 80.000.000 VNĐ |
| Sáng | 4 tháng × 20 triệu/tháng; gồm DM/nền tin phòng, lifecycle media, tích hợp, tự kiểm thử và review | 80.000.000 VNĐ |
| Thái | 4 tháng × 10 triệu/tháng; gồm worker/môi trường/provider, học, tự kiểm thử và sửa lỗi | 40.000.000 VNĐ |
| Hạ tầng/công cụ | Khoản minh họa cho môi trường, LiveKit tự host/TURN, email, backup và giám sát | 20.000.000 VNĐ |
| Dự phòng | Khoản minh họa cho biến động công sức, tích hợp và vận hành | 30.000.000 VNĐ |
| **Tổng** | Không cộng thêm BA/UX/QA như nhân sự độc lập vì đang kiêm nhiệm | **250.000.000 VNĐ** |

Mức tháng giả định đã ứng với sự tham gia 3–4 ngày/tuần, không nhân thêm tỷ lệ công suất rồi tính trùng. Khi có đơn giá thực tế, thay từng giả định và tính lại thời gian/chi phí. Chi phí hạ tầng thật cần topology, số giờ camera/share, lưu lượng TURN, lưu trữ và báo giá; 20 triệu chưa chứng minh đủ. Chưa bao gồm thuế, marketing hoặc vận hành dài hạn ngoài kỳ minh họa.

### Cơ cấu dự toán ban đầu

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

### Nguồn lực và công sức cần ước lượng lại

- Ba thành viên dự kiến 3–4 ngày/tuần mỗi người; cần tính quỹ thời gian thực tế theo DEC-088.
- Vg làm Identity còn lại/Community, giữ quyết định quan trọng, điều phối
  và review/hướng dẫn; chuyển service và media được xếp ở giai đoạn v1.
- Sáng làm DM/nền tin phòng từ DB tới frontend, sau đó chuyển Messaging
  và lifecycle/UI điều khiển media; thiết kế, tích hợp và tự kiểm thử nằm trong gói.
- Thái làm email worker/môi trường/công cụ, sau đó provider/SDK/thiết bị
  media; tính cả học, tự kiểm thử, sửa lại và review với Vg/Sáng trong quỹ thời gian.
- QA 2 tháng công và BA/UX/UI 1 tháng công là giả định khối lượng từ dự toán
  ban đầu; cần ước lượng lại theo phân công kiêm nhiệm hiện có và làm rõ
  công việc phân tích nghiệp vụ.

Khi lập lịch, tổng phân bổ của mỗi người phải bao gồm công việc kiêm nhiệm,
học, hướng dẫn và rà soát; tránh tính cùng một khoảng thời gian cho nhiều vai trò.
[Bảng công suất](#capacity) là cơ sở hiện hành; tỷ lệ cần điều chỉnh sau khi đo kết quả thực tế.

### Cơ sở ước lượng hạ tầng

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

### Mô hình chi phí vận hành minh họa

Để khớp khoản hạ tầng 20 triệu của kế hoạch, dùng **5 triệu/tháng × 4 tháng** làm khoản phân bổ tượng trưng: Web/API/worker 0,8 triệu; DB 1,2 triệu; SFU/TURN 1,5 triệu; backup 0,5 triệu; email/quan sát 0,4 triệu; dự phòng hạ tầng 0,6 triệu. Đây là số chia ngân sách, không phải giá dịch vụ hoặc bằng chứng cấu hình đủ tải. Không tính lại phần này bên ngoài tổng 250 triệu.

Chi phí thật mỗi tháng = tài nguyên cố định + GB truyền ra × đơn giá vượt hạn + GB-tháng DB/backup × đơn giá + số email × đơn giá + khoản khác được xác nhận. Ghi currency/tỷ giá, lưu lượng bao gồm trong gói, cách tính TURN và thuế khi có báo giá; không cộng cùng một lưu lượng hai lần chỉ vì đã đi qua cả SFU và TURN.

Ví dụ kỹ thuật để nhìn độ nhạy, **chưa phải mức dùng được xác nhận**: 2.000 giờ-người/tháng, trung bình 4 người/phòng, mọi người bật camera, thời lượng share bằng 20% giờ-người; bitrate giả định camera 1,5 Mbps, audio 0,064 Mbps, share 1 Mbps. Giả định mọi người đăng ký nhận mọi nguồn của người khác: giờ-luồng = giờ-người × (4−1); 1 Mbps truyền liên tục một giờ = 0,45 GB theo đơn vị thập phân. Camera khoảng 4.050 GB, audio 172,8 GB, share 540 GB, tổng 4.762,8 GB; cộng giả định 20% overhead khoảng **5.715 GB/tháng**. Simulcast/chọn nguồn/FPS/tỷ lệ bật camera/TURN làm số thực tế khác; cần đo GB thật thay hệ số minh họa.

Ở workload media 10 + 8 + 2 người, số lượt nhận camera từ người khác = 10×9 + 8×7 + 2×1 = 148; với bitrate giả định trên và hai nguồn share trong mỗi phòng/call, lưu lượng ra minh họa khoảng 265 Mbps, khoảng 319 Mbps nếu cộng 20%. Con số này không suy ra chất lượng đã đạt hoặc thay kiểm thử mạng từng máy. Ghi chỉ số SFU/NIC và hóa đơn để điều chỉnh model.

Dữ liệu cần thu thập cho OQ-010: giờ-người, tỷ lệ camera/share, số người/phòng, tỷ lệ qua TURN, bitrate/GB ra, tốc độ tăng DB/WAL, GB backup 30 ngày và email được provider nhận. Trước mở công khai phải chốt nhà cung cấp/cấu hình/hạn mức và cơ chế cảnh báo chi phí. Không tự mua dịch vụ trong quá trình hoàn thiện tài liệu.

### Giả định lập kế hoạch

AS-001 đã được đối chiếu với cơ cấu nhân sự hiện tại và không còn phù hợp.
Các giả định còn lại đang chờ kiểm chứng. Chúng là đầu vào ước tính công sức
và chi phí, chưa phải cam kết năng lực hoặc hạn mức đăng ký của sản phẩm.

| Mã | Giả định | Cách kiểm chứng | Đầu mối dự kiến |
|---|---|---|---|
| AS-001 | Giả định ban đầu: có 3 kỹ sư có kinh nghiệm làm toàn thời gian trong 3 tháng. | Không còn phù hợp: đội ngũ 3 thành viên, mỗi người 3–4 ngày/tuần, có 1 thành viên mới cần hướng dẫn; phải ước lượng lại theo phân công và năng lực. | Vg, Sáng |
| AS-002 | QA 2 tháng công và BA/UX/UI 1 tháng công đáp ứng khối lượng công việc. | Cơ sở dự toán cũ cần ước lượng lại: người sở hữu tính năng làm UX/tự kiểm thử, Vg chốt nghiệp vụ và review, Thái cung cấp dữ liệu/bộ chạy; không tính QA/UX như công suất riêng ngoài ba người. | Vg, Sáng, Thái theo phần sở hữu |
| AS-003 | Tích hợp nền tảng media có sẵn cho thoại/video và chia sẻ màn hình. | Đánh giá tính phù hợp, khả năng tích hợp, chi phí và vận hành. | Tech Lead, kỹ sư media |
| AS-004 | Quy mô dự trù 1.000 tài khoản. | Xác định nhóm người dùng ban đầu và nhu cầu lưu trữ. | Khách hàng, BA |
| AS-005 | Có khoảng 100 người trực tuyến đồng thời trên toàn hệ thống. | Xác định nhu cầu tải dự kiến; kiểm chứng khả năng đáp ứng bằng kiểm thử tải. | Khách hàng, Tech Lead, QA |
| AS-006 | Dự trù ban đầu 20 người gọi đồng thời; ngày 2026-10-04 thành giới hạn v1 theo DEC-079. | Đã chốt quy tắc 10/phòng, 20/toàn hệ thống, 2 share/phòng; khả năng đáp ứng vẫn cần kiểm thử. | Khách hàng, kỹ sư media, QA |
| AS-007 | Hạ tầng và công cụ cần khoảng 20.000.000 VNĐ trong thời gian dự án. | Lập mô hình chi phí theo cấu hình, mức sử dụng và báo giá. | Phụ trách vận hành |
| AS-008 | Phạm vi có thể hoàn thành trong khoảng 3 tháng. | Ước lượng công việc, phụ thuộc và lịch nguồn lực sau phân tích, thiết kế. | Quản lý dự án, nhóm kỹ thuật |

Khả năng tiết kiệm từ tái sử dụng mã nguồn được tính vào dự toán sau khi có
kết quả đánh giá kỹ thuật.

### Các khoản chưa bao gồm

- Marketing và thu hút người dùng.
- Thuế và phí phát sinh theo điều kiện hợp đồng.
- Vận hành, hỗ trợ và bảo trì dài hạn sau thời gian dự án.
- Tính năng và nền tảng ngoài phạm vi bản hoàn thiện v1.

### Rủi ro

| Mã | Rủi ro | Ảnh hưởng | Biện pháp xử lý | Đầu mối dự kiến |
|---|---|---|---|---|
| RSK-001 | Phòng thoại trong server và gọi riêng hai người có video/chia sẻ màn hình; yêu cầu chất lượng chưa đầy đủ. | Tăng công sức tích hợp, kiểm thử và kéo dài tiến độ. | Làm rõ ma trận chức năng, giới hạn và điều kiện nghiệm thu trước khi ước lượng chi tiết. | Tech Lead, QA |
| RSK-002 | Giới hạn phòng/media đã chốt DEC-079; lượng sử dụng, bitrate thực tế và giá hạ tầng còn thiếu. | Chi phí hạ tầng vượt dự trù. | Tính chi phí theo thời lượng, dữ liệu truyền tải và theo dõi mức sử dụng. | Phụ trách vận hành |
| RSK-003 | Chuyển source một host của MVP sang microservice ở v1 theo DEC-116 có thể vượt ước lượng. | Tăng công sức triển khai, kiểm thử tích hợp và vận hành. | Chốt ranh giới/dữ liệu và cách thay transaction/guard; dành giai đoạn V1-0 riêng, chia phần hiện thực cho cả ba và giữ hồi quy MVP. | Vg chủ trì; Sáng/Thái phối hợp |
| RSK-004 | Gói fullstack, review/hướng dẫn và công cụ/provider có công sức khác nhau; tỷ lệ kế hoạch chưa chứng minh tải thực tế. | Một người quá tải hoặc chờ việc, tiến độ bàn giao bị lệch. | Theo dõi ngày công/kết quả/chờ việc hằng tuần, tối đa một gói chính, giữ 20% dự phòng và điều chuyển đầu ra rõ; khóa cấu hình nghiệm thu/người trực trước phát hành v1. | Vg, Sáng, Thái |
| RSK-005 | Thời gian học, hướng dẫn, sửa lại và rà soát của Thái chưa được lượng hóa. | Giảm thời gian dành cho các công việc khác và tăng độ bất định của tiến độ. | Giao gói có contract/AC rõ, tính học/review trong công suất người làm/người hướng dẫn, đánh giá bằng đầu ra chạy được và giải thích kết quả. | Vg, Sáng, Thái |

### Điều kiện cập nhật dự toán

Cập nhật khi có thay đổi phạm vi, thời gian, nguồn lực, giải pháp kỹ thuật,
mức sử dụng hoặc đơn giá dịch vụ. Mỗi điều chỉnh cần thể hiện cơ sở tính,
chênh lệch và tác động đến tiến độ; khách hàng xác nhận các thay đổi làm tăng
ngân sách hoặc thay đổi phạm vi bàn giao.

<a id="delivery"></a>

## Kế hoạch bàn giao và ước lượng

Theo [DEC-116](../decisions.md#dec-116), MVP dùng một host Modular Monolith để nhóm bắt đầu làm; chuyển microservice thuộc v1. Phân công theo DEC-117 và [nhân sự](#team). Mục tiêu ba tháng, mô hình 16 tuần/250 triệu là cơ sở minh họa cũ; chưa có lịch riêng MVP/v1 hoặc số liệu năng suất để cam kết. Không bắt đầu lại toàn bộ kế hoạch sau MVP.

### Đợt bàn giao và điều kiện chuyển tiếp

| Đợt | Đầu ra | Vg | Sáng | Thái | Điều kiện chuyển |
|---|---|---|---|---|---|
| MVP-0. Chốt gói đầu | Luồng Identity đủ dùng, hợp đồng module/job email, gói triển khai và môi trường thử | Hoàn thiện phần Identity trực tiếp cần; chốt quyền và hợp đồng | Chốt gói DM/nền tin và ca thử | Dựng Compose/CI/dataset hiện có; bắt đầu worker theo job contract | Phụ thuộc trực tiếp đủ rõ; không chờ toàn bộ media/kiến trúc v1 |
| MVP-1. Làm song song | Community, DM, email và bộ chạy hoạt động từ DB tới UI | Community trọn luồng | DM trọn luồng; nền tin phòng | Email Worker và bộ chạy/kiểm tra hệ thống | Ba tính năng nền tích hợp thật, quyền và dữ liệu được kiểm chứng |
| MVP-2. Bàn giao | Chạy lại trên bản checkout/dataset được ghi rõ, lỗi chính được sửa | Review quyền/Identity/Community và điều phối | Sửa/kiểm tra Messaging và luồng liên thông | Smoke/hồi quy, dữ liệu demo và hồ sơ build/kết quả | Đạt [điều kiện MVP](../releases/mvp.md#acceptance); chưa yêu cầu microservice/media |
| V1-0. Chuyển microservice | Ranh giới được chốt, API/dữ liệu/triển khai độc lập, sửa các giả định transaction xuyên service | Thiết kế ranh giới/chính sách; chuyển Identity và phần Community liên quan | Chuyển Messaging, hợp đồng/dữ liệu và kiểm thử tích hợp | Compose nhiều service, cấu hình/định tuyến/CI và bộ chạy hồi quy | Đạt gói kiến trúc v1; luồng MVP tiếp tục hoạt động |
| V1-1. Hoàn thiện và media | Đủ Community/Messaging; gọi riêng/phòng thoại, camera và share theo scope | Community/Identity còn lại; quyền/admission/quota/thu hồi media | Nhắn tin còn lại; lifecycle và UI điều khiển cuộc gọi | Provider/SDK, thiết bị/nguồn media; worker và công cụ chạy | Các gói theo [v1](../releases/v1.md) có bằng chứng; chức năng lớn không xếp chồng ngoài công suất |
| V1-2. Ổn định/phát hành | Kiểm thử đầy đủ, dữ liệu, rollback/restore và bàn giao | Review phần rủi ro cao, sửa gói sở hữu | Kiểm thử đồng thời/chất lượng và sửa gói sở hữu | Bộ chạy, diễn tập theo runbook, tổng hợp evidence và sửa gói sở hữu | Đạt gate v1; người duyệt/người vận hành còn phải được chọn theo DEC-111/OQ-011 |

V1-1 có nhiều gói; hoàn thiện nhắn tin/Community và làm media được xếp theo phụ thuộc và công suất, không mặc định cùng lúc. Gói nào chưa đủ kỹ thuật/quyền/provider thì người đó tiếp tục một gói độc lập trong scope; không thêm tính năng ngoài scope chỉ để lấp thời gian.

<a id="capacity"></a>

### Phân bổ công suất để cân tải

Tỷ lệ dưới đây là baseline lập kế hoạch, không phải đo năng lực hoặc ước lượng thời gian hoàn thành. Mỗi hàng là quỹ của **một người trong một giai đoạn**, tổng 100%; các giai đoạn không cộng cùng một tuần. Với 3–4 ngày/tuần, 80% công việc tương ứng 2,4–3,2 ngày và 20% dự phòng là 0,6–0,8 ngày. Baseline 3,5 ngày là 2,8 ngày công việc + 0,7 ngày dự phòng.

| Giai đoạn | Người | Công việc chính | Phần phối hợp đã tính trong quỹ | Dự phòng |
|---|---|---|---|---|
| MVP | Vg | 50% Community + 15% Identity còn lại | 15% quyết định/hợp đồng/review/hướng dẫn | 20% |
| MVP | Sáng | 55% DM + 15% nền tin phòng dùng chung | 10% kiểm tra tích hợp và review gói liên quan | 20% |
| MVP | Thái | 40% Email Worker + 25% môi trường/CI | 15% dataset/smoke, học và tổng hợp kết quả | 20% |
| V1-0 — chuyển service | Vg | 60% thiết kế/chuyển Identity/Community và chính sách xuyên service | 20% review/điều phối/hướng dẫn | 20% |
| V1-0 — chuyển service | Sáng | 60% chuyển Messaging, hợp đồng và dữ liệu | 20% hồi quy/kiểm tra tích hợp | 20% |
| V1-0 — chuyển service | Thái | 60% môi trường nhiều service/định tuyến/CI | 20% bộ chạy hồi quy, dataset và học | 20% |
| V1-1 — hoàn thiện/media | Vg | 45% Community/Identity còn lại + 15% chính sách/admission/quota/thu hồi media | 20% review/điều phối/hướng dẫn | 20% |
| V1-1 — hoàn thiện/media | Sáng | 50% lifecycle/UI điều khiển cuộc gọi + 20% nhắn tin còn lại | 10% kiểm tra liên thông/đồng thời | 20% |
| V1-1 — hoàn thiện/media | Thái | 50% provider/SDK/thiết bị/hiển thị nguồn + 25% worker/công cụ vận hành | 5% tổng hợp smoke/chẩn đoán; tự kiểm thử/học tính trong gói chính | 20% |
| V1-2 — ổn định | Mỗi người | 60% kiểm chứng/sửa lỗi phần sở hữu | 20% kiểm tra chéo, hồ sơ và diễn tập được phân công | 20% |

Mỗi người giữ tối đa một gói chính đang thực hiện và một gói hỗ trợ nhỏ. Khi Vg đang chốt chuyển service, không đồng thời cam kết phát triển toàn bộ Community/media; khi Sáng làm lifecycle cuộc gọi, phần nhắn tin còn lại được xếp theo quỹ 20%, không coi là một việc toàn thời gian thứ hai. Công cụ của Thái bắt đầu ở MVP và tiếp tục mở rộng ở v1, không chờ hai người khác làm xong mới có việc.

### Điều chỉnh theo kết quả thực tế

- Cuối mỗi tuần làm việc, ghi ngày công đã dùng, gói chạy được, lỗi/việc phải làm lại, thời gian review/hướng dẫn và phần bị chặn.
- Nếu gói được ước lượng vượt quỹ 80%, giảm phần xếp trong kỳ, kéo lịch hoặc chuyển một đầu ra triển khai đã có thiết kế và cách thử rõ cho người còn công suất. Vg giữ quyết định nghiệp vụ/kiến trúc, không phải tự code mọi phần.
- Khi Vg quá tải Community hoặc Thái hoàn tất worker/bộ chạy sớm, ưu tiên chuyển cho Thái một gói từ DB tới UI đã chốt contract/AC, chẳng hạn tạo phòng text trong [UC-COM-16](../features/community/specs/channels.md#uc-com-16). Vg giữ chính sách/quyền và review; chỉ giao Sáng khi gói DM/lifecycle hiện tại đã bàn giao. Gói chuyển chủ thay thế một phần quỹ công việc hiện có, không cộng thêm nhiệm vụ hoặc tự mở rộng scope MVP.
- Nếu người hoàn thành sớm, lấy gói kế tiếp trong scope của mình; nếu chuyển gói từ người khác, ghi người sở hữu mới, phụ thuộc, người review và phần việc cũ được giảm tương ứng.
- Nếu review/hướng dẫn hoặc thử nghiệm media vượt phần đã dành, cập nhật phân bổ/lịch trước khi nhận thêm gói. Dùng dữ liệu sau 1–2 tuần để hiệu chỉnh baseline; chưa tuyên bố công việc đã cân bằng chỉ từ các tỷ lệ.

### Ước lượng và khóa lịch/chi phí

Ước lượng **ngày công còn lại** cho từng gói, gồm thiết kế, code, học/hướng dẫn, review, test, tích hợp và sửa lỗi. Identity gần xong nên chỉ tính phần còn lại. AI là công cụ của cả nhóm; không tự gán hệ số tăng năng suất hay coi viết code xong là gói đã hoàn tất.

Phương án 16 tuần cũ chỉ giữ làm cơ sở ngân sách minh họa ở [dự toán](#budget); bảng ngày công theo vai trò frontend/backend cũ không tiếp tục dùng để phân công sau DEC-117. Lịch mới cần kết quả MVP, thử nghiệm chuyển service/media, ngày nghỉ và công suất thực tế. Chi phí nhân sự/hạ tầng vẫn thay theo đơn giá và cấu hình được xác nhận; không dùng ngân sách tượng trưng làm bằng chứng đủ công sức.

Microservice là gói v1 theo DEC-116. Ranh giới cụ thể, Gateway/broker và công cụ triển khai còn cần chọn; chúng không cản trở gói MVP đang chạy trên một host.

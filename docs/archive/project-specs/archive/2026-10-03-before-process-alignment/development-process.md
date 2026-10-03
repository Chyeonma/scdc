# SCDC — Quy trình thực hiện dự án

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-PRC-001 |
| Phiên bản | 1.2 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Đề xuất áp dụng |

## 1. Phạm vi áp dụng

Quy trình xác định trách nhiệm, đầu ra và điều kiện bàn giao từ tiếp nhận
nhu cầu đến phát hành, vận hành và cải tiến SCDC. Phạm vi sản phẩm được quản
lý tại [Project Brief](01-initiation/01-project-brief.md).

Công việc được triển khai theo từng nhóm chức năng. Phân tích, thiết kế và
kiểm thử có thể diễn ra song song khi đủ đầu vào; kết quả đánh giá được phản
hồi để điều chỉnh yêu cầu và kế hoạch liên quan.

Đánh giá khả thi và lập kế hoạch bắt đầu từ giai đoạn khởi tạo, gồm nguồn
lực, thời gian, chi phí, rủi ro và phương án làm rõ nhu cầu sơ bộ. Sau khi yêu cầu và
thiết kế rõ hơn, kế hoạch được chi tiết hóa và cập nhật theo từng đợt bàn giao.

## 2. Vai trò và trách nhiệm

| Vai trò | Trách nhiệm | Nội dung xác nhận |
|---|---|---|
| Đại diện khách hàng | Cung cấp mục tiêu và yêu cầu; quyết định ưu tiên, phạm vi, ngân sách và thay đổi. | Nghiệp vụ, phạm vi và nghiệm thu kết quả. |
| Người dùng đại diện | Cung cấp nhu cầu sử dụng; tham gia đánh giá luồng thao tác và bản thiết kế tương tác. | Phản hồi về khả năng sử dụng. |
| Quản lý dự án | Điều phối công việc; quản lý tiến độ, nguồn lực, phụ thuộc, chi phí và rủi ro. | Kế hoạch thực hiện và trạng thái bàn giao. |
| Phân tích nghiệp vụ (BA) | Làm rõ nhu cầu, quy tắc, ngoại lệ; đặc tả yêu cầu và theo dõi thay đổi. | Tính đầy đủ và nhất quán của đặc tả. |
| Thiết kế UX/UI | Thiết kế hành trình, luồng thao tác, giao diện và các trạng thái tương tác. | Hồ sơ trải nghiệm và giao diện. |
| Tech Lead và kỹ sư | Đánh giá khả thi; thiết kế kiến trúc, dữ liệu, tích hợp; phát triển và rà soát mã nguồn. | Giải pháp kỹ thuật và chất lượng triển khai. |
| QA/kiểm thử | Rà soát khả năng kiểm chứng yêu cầu; lập và thực hiện kế hoạch kiểm thử. | Kết quả kiểm thử và các vấn đề chất lượng còn tồn tại. |
| Vận hành/hỗ trợ | Chuẩn bị môi trường, phát hành, giám sát, sao lưu, khôi phục và tiếp nhận sự cố. | Khả năng vận hành và phương án hỗ trợ. |

Một nhân sự có thể đảm nhiệm nhiều vai trò. Kế hoạch nguồn lực phải xác định
người chịu trách nhiệm và thẩm quyền xác nhận cho từng đầu ra.

## 3. Các giai đoạn công việc

| Giai đoạn | Chủ trì | Đầu ra | Điều kiện hoàn tất |
|---|---|---|---|
| 1. Khởi tạo | Khách hàng, BA, quản lý dự án | Yêu cầu ban đầu, Project Brief, đầu mối xác nhận, đánh giá khả thi và dự toán sơ bộ, phương án làm rõ nhu cầu, quyết định và vấn đề cần làm rõ. | Thống nhất định hướng, phạm vi sơ bộ, ràng buộc, đầu mối và cách làm rõ nhu cầu. |
| 2. Làm rõ nhu cầu | BA, UX | Mô tả người dùng dự kiến, vấn đề cần giải quyết, giá trị mong muốn, hành trình ưu tiên, giả định và mức độ kiểm chứng. | Có đầu vào để xác định nhu cầu ưu tiên và nội dung cần phân tích; giả định chưa kiểm chứng và cách đánh giá giá trị được ghi rõ. |
| 3. Đặc tả yêu cầu | BA, phối hợp khách hàng và QA | Quy tắc nghiệp vụ, luồng chính/ngoại lệ, ma trận quyền, yêu cầu chất lượng và tiêu chí chấp nhận. | Các yêu cầu có phạm vi rõ, kiểm chứng được và được khách hàng xác nhận. |
| 4. Thiết kế UX/UI | UX/UI, phối hợp BA và kỹ sư | Sơ đồ màn hình, luồng thao tác, bản thiết kế tương tác và đặc tả giao diện. | Luồng sử dụng và trạng thái giao diện đáp ứng yêu cầu; các vấn đề qua đánh giá được xử lý. |
| 5. Thiết kế kỹ thuật | Tech Lead, kỹ sư, vận hành | Kiến trúc microservices, dữ liệu, hợp đồng API/sự kiện, thiết kế chi tiết và phương án vận hành. | Giải pháp được rà soát về khả thi, tích hợp, hiệu năng và chi phí. |
| 6. Hoàn thiện kế hoạch thực hiện | Quản lý dự án, toàn nhóm | Danh sách công việc theo ưu tiên, phân công, phụ thuộc, lịch bàn giao, dự toán cập nhật và kế hoạch kiểm thử. | Kế hoạch ban đầu được chi tiết hóa theo yêu cầu và thiết kế; có nguồn lực thực hiện và các điều chỉnh cần thiết được xác nhận. |
| 7. Phát triển và tích hợp | Kỹ sư, Tech Lead | Chức năng đã triển khai, mã nguồn được rà soát, kết quả kiểm thử kỹ thuật và bản tích hợp. | Đáp ứng tiêu chí chấp nhận của phần công việc và sẵn sàng kiểm thử hệ thống. |
| 8. Kiểm thử và nghiệm thu | QA, khách hàng | Kết quả kiểm thử chức năng, tích hợp, phân quyền, hiệu năng; danh sách lỗi và kết quả nghiệm thu. | Đạt điều kiện nghiệm thu; lỗi còn tồn tại được đánh giá và thống nhất cách xử lý. |
| 9. Phát hành và bàn giao | Vận hành, Tech Lead, quản lý dự án | Bản phát hành, hướng dẫn triển khai/vận hành, phương án khôi phục và đầu mối hỗ trợ. | Kiểm tra sau triển khai đạt yêu cầu; có người tiếp nhận vận hành và quyết định mở sử dụng công khai. |
| 10. Vận hành và cải tiến | Vận hành/hỗ trợ, khách hàng, nhóm sản phẩm | Báo cáo sử dụng, chất lượng, sự cố, chi phí và danh sách cải tiến ưu tiên. | Sự cố được xử lý theo trách nhiệm; yêu cầu mới được đánh giá trước khi đưa vào kế hoạch tiếp theo. |

Các điều kiện hoàn tất áp dụng cho phần công việc được bàn giao. Những vấn
đề chưa ảnh hưởng đến phần đó có thể tiếp tục xử lý nếu đã xác định người
phụ trách, thời hạn và tác động.

Khảo sát người dùng bên ngoài là một cách làm rõ nhu cầu, không phải điều
kiện bắt buộc cho mọi đợt. Nếu bỏ qua, quyết định và giới hạn bằng chứng
phải được ghi tại sổ quyết định; đặc tả không được trình bày giả định của
nhóm như kết quả khảo sát. Đợt hiện tại áp dụng
[DEC-030](01-initiation/03-discovery-and-decision-log.md).

## 4. Nội dung hồ sơ thiết kế chi tiết

Mỗi chức năng cần liên kết được yêu cầu, thiết kế và phương pháp kiểm chứng.
Mức chi tiết được xác định theo độ phức tạp và rủi ro của chức năng.

| Thành phần | Nội dung cần có |
|---|---|
| Yêu cầu | Mục tiêu sử dụng, phạm vi và tiêu chí chấp nhận. |
| Nghiệp vụ | Đối tượng thực hiện, điều kiện trước/sau, quyền, luồng chính, ngoại lệ và giới hạn. |
| UX/UI | Màn hình, thao tác, trạng thái tải/rỗng/lỗi, mất mạng và mất quyền truy cập. |
| Dữ liệu | Thực thể, quan hệ, ràng buộc, chủ sở hữu dữ liệu và vòng đời. |
| API và sự kiện | Đầu vào/đầu ra, xác thực, phân quyền, lỗi, đối tượng nhận và quy tắc tương thích. |
| Tương tác hệ thống | Trình tự xử lý giữa giao diện và dịch vụ, gồm các nhánh lỗi và mất kết nối. |
| Độ tin cậy | Xử lý đồng thời, giao dịch, gửi lại, trùng lặp và khôi phục trạng thái. |
| Vận hành | Log, chỉ số giám sát, giới hạn tài nguyên, sao lưu và khôi phục. |
| Kiểm thử | Tình huống, dữ liệu, môi trường, điều kiện chấp nhận và cách đo chất lượng. |

## 5. Điều kiện bàn giao cho phát triển

Một phần công việc được đưa vào triển khai khi:

- Yêu cầu và tiêu chí chấp nhận đủ rõ để triển khai và kiểm thử.
- Luồng giao diện, dữ liệu và hợp đồng tích hợp đã được rà soát giữa các bên liên quan.
- Các phụ thuộc và vấn đề cản trở đã được giải quyết hoặc có phương án xử lý.
- Có người phụ trách, ước lượng công sức và phạm vi bàn giao cụ thể.
- Phương pháp kiểm thử và điều kiện nghiệm thu đã được xác định.

BA chịu trách nhiệm đặc tả; UX/UI chịu trách nhiệm thiết kế tương tác;
Tech Lead chịu trách nhiệm giải pháp; QA chịu trách nhiệm phương pháp kiểm
chứng; quản lý dự án chịu trách nhiệm kế hoạch. Khách hàng xác nhận hành vi
sản phẩm và những thay đổi ảnh hưởng đến phạm vi, thời gian hoặc chi phí.

## 6. Quản lý hồ sơ và thay đổi

Hồ sơ được lưu tập trung tại `docs/project/`, sử dụng Markdown cho bản nguồn
và PDF khi cần bản bàn giao. Phiên bản tài liệu được quản lý bằng Git; mỗi
bản cập nhật ghi ngày và nội dung thay đổi.

Các mã `REQ-*`, `SCP-*`, `SUC-*`, `DEC-*`, `COST-*`, `AS-*`, `RSK-*`, `OQ-*`
lần lượt nhận diện yêu cầu, phạm vi, tiêu chí thành công, quyết định, chi phí,
giả định, rủi ro và vấn đề cần làm rõ. Giữ mã ổn định khi cập nhật nội dung.

Đặc tả, thiết kế và kết quả kiểm thử phải dẫn chiếu đến yêu cầu tương ứng.
Trạng thái tài liệu phân biệt rõ bản dự thảo, bản đang xem xét, bản đã xác
nhận và bản đã được thay thế. Giả định có đầu mối và cách kiểm chứng riêng.

Khi có đề nghị thay đổi:

1. BA ghi mục tiêu, nội dung và phạm vi bị ảnh hưởng.
2. UX/UI, kỹ thuật và QA đánh giá tác động đến thiết kế, công sức và kiểm thử.
3. Quản lý dự án tổng hợp ảnh hưởng đến nguồn lực, tiến độ và chi phí.
4. Khách hàng quyết định đối với thay đổi phạm vi, thời gian hoặc ngân sách.
5. Nhóm cập nhật yêu cầu, thiết kế, kế hoạch và các tiêu chí kiểm chứng liên quan.

Kết quả giải quyết vấn đề và thay đổi quyết định được cập nhật tại
[sổ quyết định](01-initiation/03-discovery-and-decision-log.md).

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Xác định vai trò, các giai đoạn, đầu ra và cơ chế quản lý hồ sơ dự án. |
| 1.1 | 2026-09-27 | Làm rõ đầu ra khởi tạo, mục tiêu khảo sát và việc đánh giá khả thi, lập kế hoạch từ đầu dự án. |
| 1.2 | 2026-09-30 | Cho phép làm rõ nhu cầu từ đầu vào đại diện sản phẩm khi không khảo sát bên ngoài; yêu cầu ghi rõ giả định và giới hạn bằng chứng. |

[Mục lục hồ sơ](README.md)

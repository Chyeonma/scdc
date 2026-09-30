# SCDC — Quyết định và vấn đề cần làm rõ

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-LOG-001 |
| Phiên bản | 1.11 |
| Cập nhật | 2026-09-30 |
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
| DEC-009 | Giữ dự toán ban đầu 500.000.000 VNĐ làm mốc đối chiếu khi lập dự toán điều chỉnh. | COST-001 đến COST-005; AS-001 không còn phù hợp với nhân sự hiện có, cần tính lại công sức. | Cần ước lượng lại |
| DEC-010 | Dùng bộ tiêu chí thành công sơ bộ làm đầu vào xây dựng điều kiện nghiệm thu. | SUC-001 đến SUC-006; cần cụ thể hóa trong đặc tả yêu cầu. | Đã thống nhất ở mức sơ bộ |
| DEC-012 | Đội ngũ gồm Vg, Sáng và Thái, có thể tham gia toàn thời gian. | Danh sách nhân sự tại SCDC-ORG-001. | Đã ghi nhận |
| DEC-013 | Vg là trưởng nhóm/đại diện sản phẩm, phụ trách kiến trúc tổng thể, lựa chọn công nghệ, UX/UI, trực tiếp lập trình và hướng dẫn Thái; Sáng phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg; Thái phụ trách frontend và kiểm thử, cần đào tạo và hướng dẫn. | Vai trò đã xác định tại SCDC-ORG-001; công sức kiêm nhiệm và phân công công việc cụ thể cần được ước lượng khi lập kế hoạch. | Đã ghi nhận |
| DEC-014 | Ưu tiên nhắn tin riêng giữa hai người trước, tiếp tục hành trình tham gia cộng đồng để hình thành nền tảng cho các tính năng tiếp theo. | Đầu vào đại diện sản phẩm ngày 2026-09-30 tại SCDC-DIS-001; không thay đổi phạm vi phiên bản đầu. | Đã ghi nhận định hướng ưu tiên |
| DEC-015 | Tổ chức cộng đồng bằng nhiều phòng theo chủ đề trước. | Lựa chọn của đại diện sản phẩm ngày 2026-09-30 tại SCDC-DIS-001; chưa đưa luồng thảo luận riêng trong phòng vào ưu tiên ban đầu. | Đã xác định hướng tổ chức chủ đề |
| DEC-016 | Cho phép tìm người nhận bằng tên tài khoản và nhắn tin riêng ngay, không yêu cầu kết bạn hoặc cùng cộng đồng. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; cách khớp tên và quy tắc ngoại lệ cần đặc tả tiếp. | Đã xác định cách bắt đầu hội thoại riêng |
| DEC-017 | Đợt triển khai nhắn tin riêng đầu tiên chỉ hỗ trợ tin nhắn văn bản. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; nội dung hình ảnh và file tài liệu cần xác định ở đợt sau. | Đã xác định nội dung cho đợt đầu |
| DEC-018 | Để việc xử lý khi người nhận không muốn nhận tin từ một tài khoản cụ thể sang đợt sau. | Đại diện sản phẩm chọn phương án 3 ngày 2026-09-30 tại SCDC-DIS-001; cơ chế cụ thể chưa được xác định. | Đã xác định thứ tự triển khai |
| DEC-019 | Cho tìm người để nhắn riêng bằng tên tài khoản và tên hiển thị. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; cách khớp và phân biệt tên trùng chưa đặc tả. | Đã xác định trường tìm kiếm |
| DEC-020 | Cho sửa và xóa tin nhắn văn bản trong đợt DM đầu. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; giới hạn thời gian, quyền và phạm vi xóa cần đặc tả. | Đã xác định thao tác chính |
| DEC-021 | Hiển thị trạng thái đã gửi và lỗi gửi trong đợt DM đầu. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; ý nghĩa trạng thái và quy tắc thử lại cần đặc tả. | Đã xác định trạng thái chính |
| DEC-022 | Cho tham gia cộng đồng qua liên kết mời và tìm kiếm. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; cộng đồng hiển thị và điều kiện tham gia còn mở. | Đã xác định hai cách tiếp cận |
| DEC-023 | Để gửi file tài liệu trong phòng theo chủ đề sang đợt sau. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; thời điểm và quy tắc file chưa xác định. | Đã xác định thứ tự triển khai |

Tài liệu liên quan: [yêu cầu ban đầu](00-customer-request.md),
[Project Brief](01-project-brief.md), [dự toán và giả định](02-budget-and-assumptions.md).

## 2. Vấn đề cần làm rõ

| Mã | Nội dung cần xác định | Kết quả cần có | Thời điểm xử lý | Đầu mối dự kiến | Trạng thái |
|---|---|---|---|---|---|
| OQ-001 | Người dùng đại diện, hành trình ưu tiên, ngôn ngữ và khu vực sử dụng. | Hồ sơ người dùng và hành trình ưu tiên. | Khảo sát nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đối tượng là nhóm người bất kỳ, ưu tiên nhắn tin riêng trước rồi tham gia cộng đồng; còn mở về chi tiết hành trình, người dùng khảo sát, ngôn ngữ và khu vực |
| OQ-002 | Quy tắc đăng ký, xác thực, khôi phục tài khoản và quản lý phiên. | Luồng tài khoản và quy tắc nghiệp vụ. | Đặc tả yêu cầu | Khách hàng, BA, kỹ thuật | Chưa làm rõ |
| OQ-003 | Tạo/tham gia/rời server, lời mời, nhu cầu tìm cộng đồng và quyền riêng tư. | Luồng quản lý cộng đồng và điều kiện truy cập. | Đặc tả yêu cầu | Khách hàng, BA | Đã chọn tham gia qua liên kết mời và tìm kiếm (DEC-022); cộng đồng hiển thị, quyền riêng tư và điều kiện tham gia còn mở |
| OQ-004 | Vai trò và các thao tác được phép trong server/phòng. | Ma trận quyền. | Đặc tả yêu cầu | Khách hàng, BA | Chưa làm rõ |
| OQ-005 | Quy tắc gửi, nhận, lưu lịch sử; mất mạng, gửi lại; loại nội dung và tương tác. | Quy tắc nhắn tin và tiêu chí chấp nhận. | Đặc tả yêu cầu | Khách hàng, BA, UX, kỹ thuật | DM đầu: văn bản, tìm bằng tên tài khoản/tên hiển thị, sửa/xóa, trạng thái đã gửi/lỗi gửi (DEC-016, DEC-017, DEC-019–021); xử lý tin không mong muốn và gửi file để đợt sau (DEC-018, DEC-023). Còn mở quy tắc chi tiết và ngoại lệ |
| OQ-006 | Số người/phòng, chất lượng thoại/video, chia sẻ màn hình và mất kết nối. | Giới hạn sử dụng, hành vi cuộc gọi và tiêu chí chất lượng. | Đặc tả yêu cầu và thiết kế | Khách hàng, UX, kỹ sư media, QA | Chưa làm rõ |
| OQ-007 | Trình duyệt/thiết bị hỗ trợ, hiệu năng và cách nghiệm thu. | Ma trận hỗ trợ, chỉ tiêu chất lượng và phương pháp đo. | Đặc tả yêu cầu | Khách hàng, kỹ thuật, QA | Chưa làm rõ |
| OQ-008 | Ranh giới dịch vụ, dữ liệu, giải pháp media, triển khai và thành phần tái sử dụng. | Hồ sơ kiến trúc và đánh giá kỹ thuật. | Thiết kế kỹ thuật | Vg chủ trì, Sáng phối hợp | Chưa thiết kế |
| OQ-009 | Phân công kiêm nhiệm, công sức đào tạo, ngày khởi động, tiến độ và dự toán điều chỉnh. | Kế hoạch nguồn lực, lịch thực hiện và chi phí chi tiết theo đội ngũ hiện có. | Lập kế hoạch thực hiện | Vg, Sáng | Đã xác định 3 thành viên toàn thời gian và đầu mối UX/UI, frontend, kiểm thử; còn mở về công sức, lịch và dự toán chi tiết |
| OQ-010 | Mức sử dụng media, băng thông, lưu trữ và chi phí sau phát hành. | Mô hình chi phí vận hành. | Thiết kế vận hành | Khách hàng, kỹ thuật, vận hành | Chưa ước lượng chi tiết |
| OQ-011 | Quản trị nền tảng, xử lý vi phạm, hỗ trợ và lưu giữ dữ liệu. | Quy tắc quản trị và phân công vận hành. | Khảo sát và đặc tả yêu cầu | Khách hàng, BA, vận hành | Chưa làm rõ |
| OQ-012 | Vấn đề sử dụng ưu tiên, giá trị sản phẩm cần đem lại và cách đánh giá giá trị đó. | Mục tiêu sản phẩm và tiêu chí đánh giá có căn cứ từ khảo sát. | Khảo sát nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đại diện sản phẩm nêu vấn đề nhiều chủ đề làm trôi tin nhắn và tài liệu; còn mở về kiểm chứng với người dùng, kết quả mong muốn và cách đánh giá |
| OQ-013 | Đầu mối, thẩm quyền xác nhận, người tham gia và lịch khảo sát. | Hồ sơ nhân sự và kế hoạch khảo sát tại SCDC-ORG-001. | Khởi tạo | Vg | Đã xác định đầu mối; phương án tiếp cận và lịch khảo sát còn mở |

Vai trò đã xác định và phương án kiêm nhiệm được ghi tại
[SCDC-ORG-001](04-team-and-discovery-plan.md). Khi đóng một vấn đề, bổ sung
kết luận, ngày xử lý và tài liệu chứa kết quả.

## 3. Điều chỉnh phạm vi ngày 2026-09-27

| Nội dung | Kết luận |
|---|---|
| Thay đổi đã xác nhận | Loại nhóm chat riêng ngoài server và cuộc gọi trong loại nhóm này. |
| Phạm vi giao tiếp còn lại | Phòng nhắn tin và phòng thoại trong server; nhắn tin và gọi riêng giữa hai người; video và chia sẻ màn hình trong các ngữ cảnh gọi này. |
| Yêu cầu và phạm vi bị ảnh hưởng | Loại REQ-006; cập nhật REQ-008, REQ-009, SCP-005, SCP-006, SCP-007 và DEC-004, DEC-005. |
| Ảnh hưởng cần đánh giá | Giảm các luồng nhóm riêng trong nghiệp vụ, UX/UI, kỹ thuật và kiểm thử; cần ước lượng lại công sức. |
| Ngân sách và tiến độ | Tiếp tục dùng mốc dự trù 500.000.000 VNĐ và khoảng 3 tháng; chưa xác định mức điều chỉnh sau thay đổi phạm vi. |

## 4. Cập nhật nguồn lực ngày 2026-09-27

Đã xác định Vg, Sáng và Thái có thể tham gia toàn thời gian. Vg phụ trách
kiến trúc tổng thể, lựa chọn công nghệ, thiết kế UX/UI và trực tiếp lập trình
cùng Sáng, đồng thời giữ trách nhiệm trưởng nhóm/đại diện sản phẩm. Sáng
phụ trách kỹ thuật backend. Thái phụ trách frontend và kiểm thử, được Vg
hướng dẫn. Giả định ba kỹ sư có kinh nghiệm trong dự toán ban đầu không còn
phù hợp; mốc ba tháng và công sức cần được đánh giá lại.

Chưa có người tham gia khảo sát được xác định. Nhóm có thể bố trí thời gian
linh hoạt; phương án tìm người và lịch cụ thể chưa được chốt. OQ-013 được
giải quyết một phần, tiếp tục mở đối với kế hoạch khảo sát.

## 5. Nhu cầu sử dụng đã ghi nhận ngày 2026-09-27

Đại diện sản phẩm xác định ba nhu cầu: kết nối với cộng đồng hiện có, tạo
cộng đồng và nhắn tin riêng giữa hai người. Nội dung được cập nhật tại
[SCDC-REQ-001](00-customer-request.md), phù hợp với phạm vi hiện hành.

Đây là đầu vào làm rõ yêu cầu; chưa có kết quả nghiên cứu người dùng bên
ngoài. OQ-001 và OQ-012 được giải quyết một phần. Thứ tự ưu tiên, cách tiếp
cận cộng đồng và kết quả mong muốn của từng hành trình còn cần làm rõ.

## 6. Làm rõ nhu cầu ngày 2026-09-30

Đại diện sản phẩm giữ đối tượng sử dụng rộng là một nhóm người bất kỳ,
nêu vấn đề tin nhắn và tài liệu bị trôi khi nhiều chủ đề dùng chung một
luồng chat, đồng thời ưu tiên nhắn tin riêng trước rồi tham gia cộng đồng.
Cộng đồng được tổ chức bằng nhiều phòng theo chủ đề trước.
Đã chọn tìm người nhận bằng tên tài khoản và nhắn riêng ngay, không yêu
cầu kết bạn hoặc cùng cộng đồng (DEC-016).
Đợt triển khai DM đầu tiên chỉ hỗ trợ tin nhắn văn bản (DEC-017).
Việc xử lý khi người nhận không muốn nhận tin từ một tài khoản cụ thể
được để sang đợt sau (DEC-018).
Người dùng có thể tìm theo tên tài khoản hoặc tên hiển thị (DEC-019), sửa
và xóa tin văn bản (DEC-020), thấy trạng thái đã gửi hoặc lỗi gửi
(DEC-021). Tham gia cộng đồng bằng liên kết mời và tìm kiếm (DEC-022);
gửi file tài liệu trong phòng được để sang đợt sau (DEC-023).
Chi tiết đầu vào và nội dung cần hỏi tiếp được quản lý tại
[SCDC-DIS-001](../02-discovery/01-users-and-needs.md).

OQ-001, OQ-005 và OQ-012 được bổ sung đầu vào, tiếp tục mở. Chưa có khảo sát
người dùng bên ngoài hoặc quyết định bổ sung tính năng luồng thảo luận,
tìm kiếm tin nhắn/tài liệu hay kho tài liệu. Phạm vi bàn giao hiện hành
chưa thay đổi.

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Ghi nhận quyết định khởi tạo và các vấn đề cần làm rõ. |
| 1.1 | 2026-09-27 | Liên kết căn cứ quyết định với yêu cầu và dự toán. |
| 1.2 | 2026-09-27 | Làm rõ trạng thái quyết định, đầu ra xử lý vấn đề; chuyển quy định quản lý hồ sơ sang tài liệu quy trình. |
| 1.3 | 2026-09-27 | Cập nhật phạm vi giao tiếp; ghi ảnh hưởng thay đổi và bổ sung các vấn đề cần làm rõ khi khởi tạo, khảo sát. |
| 1.4 | 2026-09-27 | Ghi nhận nhân sự, vai trò và thời gian tham gia; cập nhật trạng thái OQ-009, OQ-013 và căn cứ dự toán. |
| 1.5 | 2026-09-27 | Làm rõ trách nhiệm kiến trúc, lựa chọn công nghệ và lập trình của Vg; cập nhật đầu mối OQ-008. |
| 1.6 | 2026-09-27 | Ghi nhận phân công UX/UI, frontend, kiểm thử và ba nhu cầu sử dụng; cập nhật DEC-013, OQ-001, OQ-009 và OQ-012. |
| 1.7 | 2026-09-30 | Ghi nhận DEC-014, DEC-015; cập nhật OQ-001, OQ-005 và OQ-012 theo vấn đề sử dụng, ưu tiên DM và lựa chọn nhiều phòng theo chủ đề. |
| 1.8 | 2026-09-30 | Ghi nhận DEC-016 về tìm bằng tên tài khoản và nhắn ngay; cập nhật OQ-005. |
| 1.9 | 2026-09-30 | Ghi nhận DEC-017 về nội dung văn bản trong đợt nhắn tin riêng đầu tiên; cập nhật OQ-005. |
| 1.10 | 2026-09-30 | Ghi nhận DEC-018 về việc để xử lý tin riêng không mong muốn sang đợt sau. |
| 1.11 | 2026-09-30 | Ghi nhận DEC-019 đến DEC-023 về tìm người, thao tác/trạng thái DM, cách tham gia cộng đồng và thứ tự triển khai file. |

[Mục lục hồ sơ](../README.md)

# SCDC — Quyết định và vấn đề còn mở

Cập nhật: 2026-10-03. Nguồn chuẩn cho các mã DEC/OQ; hành vi chi tiết nằm trong đặc tả tính năng.

Các quyết định giữ nguyên mã. Quyết định bị thay thế vẫn có dòng truy vết; vấn đề mở chỉ đóng khi có kết luận và bằng chứng tương ứng.

## Mục lục

- [Quyết định](#decisions)
- [Vấn đề còn mở](#open-questions)
- [Thay đổi trong lần hợp nhất](#changes)

<a id="decisions"></a>

## 1. Quyết định

| Mã | Quyết định | Căn cứ hoặc điều kiện áp dụng | Trạng thái |
|---|---|---|---|
| <a id="dec-001"></a>DEC-001 | Tái sử dụng thành phần sẵn có phù hợp với yêu cầu và kiến trúc mục tiêu. | Cần đánh giá chất lượng, khả năng tích hợp và công sức điều chỉnh trong thiết kế kỹ thuật. | Chưa xác định thành phần cụ thể |
| <a id="dec-002"></a>DEC-002 | Phục vụ nhóm bạn và cộng đồng không giới hạn chủ đề. | REQ-001. | Đã thống nhất |
| <a id="dec-003"></a>DEC-003 | Phát triển phiên bản đầu trên trình duyệt web. | REQ-003. | Đã thống nhất |
| <a id="dec-004"></a>DEC-004 | Hỗ trợ nhắn tin trong phòng thuộc server và tin riêng giữa hai người. | REQ-004, REQ-005; REQ-006 được loại khỏi phạm vi tại phiên bản 1.3. | Đã thống nhất |
| <a id="dec-005"></a>DEC-005 | Hỗ trợ phòng thoại trong server và gọi riêng giữa hai người, có video và chia sẻ màn hình. | REQ-007, REQ-008, REQ-009. | Đã thống nhất |
| <a id="dec-006"></a>DEC-006 | Phát hành công khai và cho phép người dùng tự đăng ký. | REQ-010. | Đã thống nhất |
| <a id="dec-007"></a>DEC-007 | Yêu cầu microservices cho phiên bản đầu trước đây. | Được thay thế ngày 2026-10-03 bởi DEC-060 theo xác nhận của đại diện sản phẩm. | Đã thay thế; không áp dụng cho MVP |
| <a id="dec-008"></a>DEC-008 | Dùng mốc khoảng 3 tháng từ ngày khởi động làm mục tiêu tiến độ. | REQ-011; cần kiểm tra AS-001, AS-002, AS-008 khi lập kế hoạch chi tiết. | Mục tiêu lập kế hoạch |
| <a id="dec-009"></a>DEC-009 | Giữ dự toán ban đầu 500.000.000 VNĐ làm mốc đối chiếu khi lập dự toán điều chỉnh. | COST-001 đến COST-005; AS-001 không còn phù hợp với nhân sự hiện có, cần tính lại công sức. | Cần ước lượng lại |
| <a id="dec-010"></a>DEC-010 | Dùng bộ tiêu chí thành công sơ bộ làm đầu vào xây dựng điều kiện nghiệm thu. | SUC-001 đến SUC-006; cần cụ thể hóa trong đặc tả yêu cầu. | Đã thống nhất ở mức sơ bộ |
| <a id="dec-012"></a>DEC-012 | Đội ngũ gồm Vg, Sáng và Thái, có thể tham gia toàn thời gian. | Danh sách nhân sự tại SCDC-ORG-001. | Đã ghi nhận |
| <a id="dec-013"></a>DEC-013 | Vg là trưởng nhóm/đại diện sản phẩm, phụ trách kiến trúc tổng thể, lựa chọn công nghệ, UX/UI, trực tiếp lập trình và hướng dẫn Thái; Sáng phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg; Thái phụ trách frontend và kiểm thử, cần đào tạo và hướng dẫn. | Vai trò đã xác định tại SCDC-ORG-001; công sức kiêm nhiệm và phân công công việc cụ thể cần được ước lượng khi lập kế hoạch. | Đã ghi nhận |
| <a id="dec-014"></a>DEC-014 | Ưu tiên nhắn tin riêng giữa hai người trước, tiếp tục hành trình tham gia cộng đồng để hình thành nền tảng cho các tính năng tiếp theo. | Đầu vào đại diện sản phẩm ngày 2026-09-30 tại SCDC-DIS-001; không thay đổi phạm vi phiên bản đầu. | Đã ghi nhận định hướng ưu tiên |
| <a id="dec-015"></a>DEC-015 | Tổ chức cộng đồng bằng nhiều phòng theo chủ đề trước. | Lựa chọn của đại diện sản phẩm ngày 2026-09-30 tại SCDC-DIS-001; chưa đưa luồng thảo luận riêng trong phòng vào ưu tiên ban đầu. | Đã xác định hướng tổ chức chủ đề |
| <a id="dec-016"></a>DEC-016 | Cho phép tìm người nhận bằng tên tài khoản và nhắn tin riêng ngay, không yêu cầu kết bạn hoặc cùng cộng đồng. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; cách tìm theo một phần tên được làm rõ tại DEC-019, ngoại lệ còn cần đặc tả. | Đã xác định cách bắt đầu hội thoại riêng |
| <a id="dec-017"></a>DEC-017 | Đợt triển khai nhắn tin riêng đầu tiên chỉ hỗ trợ tin nhắn văn bản. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; nội dung hình ảnh và file tài liệu cần xác định ở đợt sau. | Đã xác định nội dung cho đợt đầu |
| <a id="dec-018"></a>DEC-018 | Để việc xử lý khi người nhận không muốn nhận tin từ một tài khoản cụ thể sang đợt sau. | Đại diện sản phẩm chọn phương án 3 ngày 2026-09-30 tại SCDC-DIS-001; cơ chế cụ thể chưa được xác định. | Đã xác định thứ tự triển khai |
| <a id="dec-019"></a>DEC-019 | Cho tìm người để nhắn riêng theo một phần tên tài khoản hoặc tên hiển thị. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; tên tài khoản duy nhất theo DEC-054 dùng phân biệt tên hiển thị; chi tiết tìm kiếm còn cần rà soát. | Đã xác định trường và cách khớp tìm kiếm |
| <a id="dec-020"></a>DEC-020 | Trong đợt DM đầu, người gửi có thể sửa tin văn bản của mình bất cứ lúc nào với dấu “Đã sửa”, và xóa cho cả hai người với dòng “Tin nhắn đã bị xóa”. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; DEC-052 chốt chỉ giữ bản nội dung mới nhất; DEC-053 chốt giới hạn nội dung. | Đã xác định thao tác và hiển thị |
| <a id="dec-021"></a>DEC-021 | Hiển thị trạng thái đã gửi khi hệ thống đã lưu tin và lỗi gửi khi không lưu được; khi lỗi, người gửi bấm thử lại. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; hành vi khi người nhận vắng mặt ở DEC-035, kết quả chống trùng ở DEC-037; cơ chế kỹ thuật còn mở. | Đã xác định ý nghĩa trạng thái và cách thử lại |
| <a id="dec-022"></a>DEC-022 | Cho tham gia cộng đồng qua liên kết mời và tìm kiếm. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; cộng đồng hiển thị và điều kiện tham gia còn mở. | Đã xác định hai cách tiếp cận |
| <a id="dec-023"></a>DEC-023 | Để gửi file tài liệu trong phòng theo chủ đề sang đợt sau. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; thời điểm và quy tắc file chưa xác định. | Đã xác định thứ tự triển khai |
| <a id="dec-024"></a>DEC-024 | Chỉ cộng đồng công khai xuất hiện trong tìm kiếm. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền thay đổi trạng thái công khai còn cần đặc tả. | Đã xác định phạm vi hiển thị |
| <a id="dec-025"></a>DEC-025 | Cộng đồng công khai mới tạo mặc định cho vào ngay; cộng đồng có thể cấu hình chờ duyệt. Liên kết mời hợp lệ cho vào ngay dù cộng đồng yêu cầu duyệt khi tham gia qua tìm kiếm. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; người được đổi cấu hình và thời hạn mời ở DEC-029/028, quyền thu hồi ở DEC-044; phân quyền chi tiết còn mở. | Đã xác định mặc định và ngoại lệ lời mời |
| <a id="dec-026"></a>DEC-026 | Chỉ chủ sở hữu hoặc người được cấp quyền mới tạo phòng theo chủ đề. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; ma trận quyền chi tiết còn mở. | Đã xác định điều kiện tạo phòng |
| <a id="dec-027"></a>DEC-027 | Thành viên chỉ nhìn thấy phòng theo chủ đề mà mình được cấp quyền xem. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền xem mặc định được chốt tại DEC-033, ma trận quyền chi tiết còn mở. | Đã xác định nguyên tắc hiển thị |
| <a id="dec-028"></a>DEC-028 | Người tạo liên kết mời được chọn thời hạn hiệu lực. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền tạo ở DEC-031, quyền thu hồi ở DEC-044; giá trị thời hạn cụ thể còn mở. | Đã xác định khả năng đặt thời hạn |
| <a id="dec-029"></a>DEC-029 | Chủ sở hữu và người được cấp quyền có thể đổi chế độ tham gia cộng đồng giữa vào ngay và chờ duyệt. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; ma trận quyền chi tiết còn mở. | Đã xác định người được đổi chế độ |
| <a id="dec-030"></a>DEC-030 | Không tổ chức khảo sát người dùng bên ngoài trong đợt hiện tại; tiếp tục đặc tả từ đầu vào của đại diện sản phẩm và ghi rõ các giả định chưa kiểm chứng. | Đại diện sản phẩm quyết định ngày 2026-09-30; SCDC-DIS-001 là đầu vào, SCDC-FR-DM-001 và SCDC-FR-COM-001 là bản nháp đặc tả. Kiểm thử và nghiệm thu vẫn theo SCDC-PRC-001. | Áp dụng cho đợt hiện tại |
| <a id="dec-031"></a>DEC-031 | Chỉ chủ sở hữu và người được cấp quyền được tạo liên kết mời vào cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền thu hồi được chốt ở DEC-044, cách cấp/thu hồi vai trò còn mở tại OQ-004. | Đã xác định quyền tạo lời mời |
| <a id="dec-032"></a>DEC-032 | Chỉ chủ sở hữu và người được cấp quyền được duyệt yêu cầu tham gia cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quy tắc từ chối và hủy yêu cầu còn mở. | Đã xác định quyền duyệt |
| <a id="dec-033"></a>DEC-033 | Phòng theo chủ đề mới tạo mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | Đại diện sản phẩm trả lời ngày 2026-09-30; cách cấu hình quyền chi tiết còn mở. | Đã xác định quyền xem mặc định |
| <a id="dec-034"></a>DEC-034 | Tin nhắn văn bản trong phòng ở đợt đầu cho người gửi sửa và xóa như tin riêng. | Đại diện sản phẩm trả lời ngày 2026-09-30; áp dụng dấu “Đã sửa”, dòng thay thế khi xóa và trạng thái gửi của DEC-020, DEC-021. | Đã xác định thao tác tin trong phòng |
| <a id="dec-035"></a>DEC-035 | Tin nhắn đã được hệ thống lưu khi người nhận chưa mở ứng dụng phải xuất hiện trong hội thoại khi người nhận mở lại. | Đại diện sản phẩm xác nhận qua ví dụ A gửi cho B ngày 2026-09-30; áp dụng cho DM, và lịch sử phòng được xem khi thành viên có quyền truy cập. | Đã xác định hành vi ngoại tuyến |
| <a id="dec-036"></a>DEC-036 | Đợt đầu dùng cả email và tên tài khoản cho tài khoản người dùng. | Đại diện sản phẩm chọn “Cả email lẫn tên tài khoản” ngày 2026-09-30; DEC-054 ngày 2026-10-03 đã xác nhận đăng ký có email/tên tài khoản, đăng nhập bằng một trong hai với mật khẩu và quy tắc hồ sơ; trường tên hiển thị được đồng bộ tại DEC-062. | Đã được cụ thể hóa bởi DEC-054 |
| <a id="dec-037"></a>DEC-037 | Khi người gửi bấm thử lại sau kết quả không rõ, hệ thống chỉ tạo một tin nhắn cho cùng thao tác gửi. | Đại diện sản phẩm chọn ngày 2026-09-30; cách thực hiện kỹ thuật và thời hạn khóa chống trùng cần thiết kế. | Đã xác định kết quả mong muốn |
| <a id="dec-038"></a>DEC-038 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mà mình được phép xem. | Đại diện sản phẩm chọn ngày 2026-09-30; quy tắc khi rời/mất quyền còn mở. | Đã xác định quyền lịch sử khi tham gia |
| <a id="dec-039"></a>DEC-039 | Chủ sở hữu và người được cấp quyền được thay đổi danh sách người có quyền xem phòng. | Đại diện sản phẩm chọn ngày 2026-09-30; mô hình vai trò, ngoại lệ cá nhân và người quản lý vai trò được chốt tại DEC-055–058. | Đã xác định người quản lý quyền xem |
| <a id="dec-040"></a>DEC-040 | Trong đợt đầu, mọi thành viên có quyền xem phòng đều được gửi tin văn bản trong phòng đó. | Đại diện sản phẩm chọn ngày 2026-09-30; chưa có quyền chỉ đọc riêng. | Đã xác định quyền gửi mặc định |
| <a id="dec-041"></a>DEC-041 | Người dùng phải xác minh email trước khi được nhắn tin. | Đại diện sản phẩm trả lời ngày 2026-09-30; DEC-051 chốt trước xác minh chỉ dùng xác minh/khôi phục; cách gửi, thời hạn và bảo vệ liên kết còn cần thiết kế. | Đã xác định điều kiện nhắn tin |
| <a id="dec-042"></a>DEC-042 | Đợt đầu có luồng đặt lại mật khẩu qua email. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời hạn hiệu lực và quy tắc bảo vệ liên kết đặt lại cần thiết kế. | Đã xác định kênh khôi phục |
| <a id="dec-043"></a>DEC-043 | Có thể tham gia cộng đồng riêng tư bằng liên kết mời hoặc được thêm trực tiếp. | Đại diện sản phẩm trả lời ngày 2026-09-30; người có quyền thêm trực tiếp và hành vi lời mời còn mở. | Đã xác định hai cách tham gia |
| <a id="dec-044"></a>DEC-044 | Người có quyền tạo liên kết mời có thể thu hồi liên kết trước hạn. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền tạo theo DEC-031. | Đã xác định quyền thu hồi |
| <a id="dec-045"></a>DEC-045 | Thành viên thường có thể tự rời cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền truy cập sau khi rời và việc tham gia lại cần đặc tả. | Đã xác định quyền rời |
| <a id="dec-046"></a>DEC-046 | Người nhận cuộc gọi riêng phải bấm chấp nhận trước khi cuộc gọi bắt đầu. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời gian đổ chuông và cách xử lý không trả lời còn mở. | Đã xác định bước nhận cuộc gọi |
| <a id="dec-047"></a>DEC-047 | Để việc lưu cuộc gọi nhỡ trong hội thoại riêng sang đợt sau. | Đại diện sản phẩm trả lời ngày 2026-09-30; đợt đầu vẫn cần hiển thị kết quả không được nhận ở phiên gọi hiện tại. | Đã xác định thứ tự triển khai |
| <a id="dec-048"></a>DEC-048 | Thành viên nhìn thấy phòng thoại có thể vào phòng ngay. | Đại diện sản phẩm trả lời ngày 2026-09-30; người không có quyền xem phòng vẫn không được vào. | Đã xác định quyền vào phòng thoại |
| <a id="dec-049"></a>DEC-049 | Đợt đầu cho phép nhiều người chia sẻ màn hình cùng lúc trong một phòng thoại. | Đại diện sản phẩm trả lời ngày 2026-09-30; số luồng tối đa và tải cần thử nghiệm để chốt. | Đã xác định khả năng chia sẻ đồng thời |
| <a id="dec-050"></a>DEC-050 | Khi mất mạng trong cuộc gọi và mạng trở lại, ứng dụng tự kết nối lại. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời gian chờ và trường hợp không thể khôi phục còn mở. | Đã xác định cách khôi phục kết nối |
| <a id="dec-051"></a>DEC-051 | Tài khoản chưa xác minh email chỉ dùng xác minh email hoặc khôi phục mật khẩu, chưa vào các chức năng ứng dụng. | Đại diện sản phẩm chọn ngày 2026-10-03; cụ thể hóa phần còn mở của DEC-041 tại SCDC-FR-ACC-001. | Đã xác nhận |
| <a id="dec-052"></a>DEC-052 | Khi sửa tin DM, chỉ giữ nội dung mới nhất, không cung cấp lịch sử bản cũ; vẫn hiển thị “Đã sửa”. | Đại diện sản phẩm chọn ngày 2026-10-03; áp dụng cho tin phòng theo nguyên tắc sửa/xóa như DM ở DEC-034. Chính sách sao lưu/lưu giữ vẫn thuộc OQ-011. | Đã xác nhận |
| <a id="dec-053"></a>DEC-053 | Tin văn bản DM và phòng tối đa 2.000 ký tự, cho xuống dòng và emoji, từ chối tin rỗng hoặc chỉ có khoảng trắng. | Đại diện sản phẩm chọn ngày 2026-10-03; cách đếm cụm ký tự cần rà soát trong thiết kế và dữ liệu kiểm thử. | Đã xác nhận quy tắc sản phẩm |
| <a id="dec-054"></a>DEC-054 | Đăng ký bằng email, tên tài khoản và mật khẩu; đăng nhập bằng email hoặc tên tài khoản. Tên tài khoản duy nhất, chưa cho đổi ở đợt đầu; tên hiển thị được đổi; email không công khai. | Đại diện sản phẩm xác nhận bộ quy tắc ngày 2026-10-03, thay phần diễn giải chưa xác nhận của DEC-036; trường đăng ký được bổ sung bởi DEC-062. | Đã xác nhận |
| <a id="dec-055"></a>DEC-055 | Cấp quyền quản lý cộng đồng và quyền xem phòng qua vai trò, có ngoại lệ từng thành viên ở phòng. | Đại diện sản phẩm chọn ngày 2026-10-03; mô hình dữ liệu/cách cấu hình được rà soát ở SCDC-FR-ACL-001. | Đã xác nhận mô hình quyền |
| <a id="dec-056"></a>DEC-056 | Chủ sở hữu luôn được xem mọi phòng trong cộng đồng của mình, kể cả phòng giới hạn thành viên. | Đại diện sản phẩm chọn ngày 2026-10-03; không vượt qua điều kiện tài khoản/phiên, không áp dụng cho DM hoặc cộng đồng khác. | Đã xác nhận |
| <a id="dec-057"></a>DEC-057 | Khi quyền xem từ các vai trò xung đột, từ chối thắng; ngoại lệ của cá nhân được áp dụng sau cùng. | Đại diện sản phẩm chọn ngày 2026-10-03; chủ sở hữu vẫn theo DEC-056. | Đã xác nhận thứ tự ưu tiên |
| <a id="dec-058"></a>DEC-058 | Chỉ chủ sở hữu cộng đồng được tạo/sửa vai trò và gán/thu hồi vai trò của thành viên ở đợt đầu. | Đại diện sản phẩm chọn ngày 2026-10-03; người được giao các quyền quản lý khác không tự có quyền quản lý vai trò. | Đã xác nhận |
| <a id="dec-059"></a>DEC-059 | Chuẩn bị giao diện tài khoản và DM cho desktop và trình duyệt điện thoại, có bố cục thích ứng. | Đại diện sản phẩm chọn ngày 2026-10-03; danh sách trình duyệt/phiên bản, thiết bị và ngưỡng chất lượng cần xác định, không suy thành hỗ trợ media trên mọi thiết bị. | Đã xác nhận phạm vi giao diện |
| <a id="dec-060"></a>DEC-060 | Phát hành MVP bằng Modular Monolith; tách microservices ở đợt sau. | Đại diện sản phẩm xác nhận ngày 2026-10-03 trong lần hợp nhất docs; thay DEC-007, cập nhật REQ-012 và SUC-005. Ranh giới module và sở hữu dữ liệu vẫn phải được duy trì. | Đã xác nhận |
| <a id="dec-061"></a>DEC-061 | API dùng ProblemDetails với `errorCode`, `traceId` và `errors` khi có lỗi trường. | Đại diện sản phẩm chọn định dạng của API hiện tại ngày 2026-10-03. Hợp đồng đề xuất được cập nhật theo quy ước tại architecture.md; schema endpoint tương lai vẫn cần rà soát. | Đã xác nhận định dạng lỗi |
| <a id="dec-062"></a>DEC-062 | Luồng đăng ký thu email, tên tài khoản, tên hiển thị và mật khẩu; tên hiển thị có thể đổi sau. | Đại diện sản phẩm yêu cầu dùng lựa chọn đã có trong source ngày 2026-10-03. Đã đối chiếu AuthScreen, RegisterRequest và IdentityValidation: `displayName` bắt buộc. Bổ sung trường đăng ký cho DEC-054, giữ nguyên quy tắc đăng nhập và định danh. | Đã đồng bộ theo lựa chọn trong source |

Tài liệu liên quan: [yêu cầu ban đầu](project.md#requirements),
[Project Brief](project.md#scope), [dự toán và giả định](project.md#budget).

<a id="open-questions"></a>

## 2. Vấn đề còn mở

| Mã | Nội dung cần xác định | Kết quả cần có | Thời điểm xử lý | Đầu mối dự kiến | Trạng thái |
|---|---|---|---|---|---|
| <a id="oq-001"></a>OQ-001 | Người dùng đại diện, hành trình ưu tiên, ngôn ngữ và khu vực sử dụng. | Hồ sơ người dùng và hành trình ưu tiên. | Làm rõ nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đối tượng là nhóm người bất kỳ, ưu tiên nhắn tin riêng trước rồi tham gia cộng đồng. Không khảo sát bên ngoài trong đợt này (DEC-030); còn mở về người dùng đại diện cụ thể, ngôn ngữ và khu vực |
| <a id="oq-002"></a>OQ-002 | Quy tắc đăng ký, xác thực, khôi phục tài khoản và quản lý phiên. | Luồng tài khoản và quy tắc nghiệp vụ. | Đặc tả yêu cầu | Khách hàng, BA, kỹ thuật | Cập nhật 2026-10-03: đã chốt bộ định danh/hồ sơ và chỉ xác minh/khôi phục trước xác minh email (DEC-051, DEC-054); SCDC-FR-ACC-001 có đề xuất ACC-P01–05. Còn thời hạn, chính sách mật khẩu, phiên, gửi lại và chống lạm dụng cần rà soát |
| <a id="oq-003"></a>OQ-003 | Tạo/tham gia/rời server, lời mời, nhu cầu tìm cộng đồng và quyền riêng tư. | Luồng quản lý cộng đồng và điều kiện truy cập. | Đặc tả yêu cầu | Khách hàng, BA | Có liên kết mời/tìm kiếm (DEC-022); tìm kiếm chỉ hiện cộng đồng công khai (DEC-024); mặc định vào ngay, có thể cấu hình chờ duyệt, liên kết mời hợp lệ cho vào ngay (DEC-025). Người tạo liên kết chọn thời hạn (DEC-028); chủ sở hữu/người được cấp quyền đổi chế độ tham gia, tạo/thu hồi lời mời và duyệt yêu cầu (DEC-029, DEC-031, DEC-032, DEC-044). Cộng đồng riêng tư vào qua mời hoặc được thêm trực tiếp (DEC-043); thành viên thường tự rời được (DEC-045). Còn mở người có quyền thêm trực tiếp, từ chối yêu cầu và hậu quả khi rời |
| <a id="oq-004"></a>OQ-004 | Vai trò và các thao tác được phép trong server/phòng. | Ma trận quyền. | Đặc tả yêu cầu | Khách hàng, BA | Cập nhật 2026-10-03: đã chốt vai trò + ngoại lệ cá nhân, quyền xem của chủ sở hữu, thứ tự xung đột và chỉ chủ sở hữu quản lý vai trò (DEC-055–058). Ma trận SCDC-FR-ACL-001 cụ thể hóa; còn chi tiết cấu hình và hệ quả thu hồi cần kiểm chứng |
| <a id="oq-005"></a>OQ-005 | Quy tắc gửi, nhận, lưu lịch sử; mất mạng, gửi lại; loại nội dung và tương tác. | Quy tắc nhắn tin và tiêu chí chấp nhận. | Đặc tả yêu cầu | Khách hàng, BA, UX, kỹ thuật | Cập nhật 2026-10-03: chỉ giữ nội dung sửa mới nhất; giới hạn 2.000 ký tự, cho emoji/xuống dòng, không nhận tin trống (DEC-052/053). Có ngoại lệ và hợp đồng DM đề xuất; còn phép đếm ký tự, tìm kiếm, lưu giữ và rà soát cơ chế đồng thời/chống trùng |
| <a id="oq-006"></a>OQ-006 | Số người/phòng, chất lượng thoại/video, chia sẻ màn hình và mất kết nối. | Giới hạn sử dụng, hành vi cuộc gọi và tiêu chí chất lượng. | Đặc tả yêu cầu và thiết kế | Khách hàng, UX, kỹ sư media, QA | Đã chốt nhận cuộc gọi riêng, để cuộc gọi nhỡ sang sau, vào phòng thoại theo quyền xem, nhiều người chia sẻ cùng lúc, tự kết nối lại (DEC-046–050); còn mở giới hạn và ngưỡng chất lượng |
| <a id="oq-007"></a>OQ-007 | Trình duyệt/thiết bị hỗ trợ, hiệu năng và cách nghiệm thu. | Ma trận hỗ trợ, chỉ tiêu chất lượng và phương pháp đo. | Đặc tả yêu cầu | Khách hàng, kỹ thuật, QA | Cập nhật 2026-10-03: tài khoản/DM hỗ trợ desktop và trình duyệt điện thoại với bố cục thích ứng (DEC-059). Còn ma trận phiên bản/thiết bị, chỉ tiêu hiệu năng/media và độ trễ thu hồi |
| <a id="oq-008"></a>OQ-008 | Ranh giới dịch vụ, dữ liệu, giải pháp media, triển khai và thành phần tái sử dụng. | Hồ sơ kiến trúc và đánh giá kỹ thuật. | Thiết kế kỹ thuật | Vg chủ trì, Sáng phối hợp | Cập nhật 2026-10-03: có SCDC-API-DM-001 về hợp đồng và dữ liệu tài khoản/DM; MVP đã chọn Modular Monolith (DEC-060), Identity có code và OpenAPI; thiết kế DM/media và bằng chứng thử nghiệm vẫn chưa hoàn tất |
| <a id="oq-009"></a>OQ-009 | Phân công kiêm nhiệm, công sức đào tạo, ngày khởi động, tiến độ và dự toán điều chỉnh. | Kế hoạch nguồn lực, lịch thực hiện và chi phí chi tiết theo đội ngũ hiện có. | Lập kế hoạch thực hiện | Vg, Sáng | Cập nhật 2026-10-03: đã phân rã đầu ra Đợt 0 tại SCDC-READY-001; còn ngày công, lịch nguồn lực và dự toán điều chỉnh; chưa có cam kết ngày phát hành |
| <a id="oq-010"></a>OQ-010 | Mức sử dụng media, băng thông, lưu trữ và chi phí sau phát hành. | Mô hình chi phí vận hành. | Thiết kế vận hành | Khách hàng, kỹ thuật, vận hành | Chưa ước lượng chi tiết |
| <a id="oq-011"></a>OQ-011 | Quản trị nền tảng, xử lý vi phạm, hỗ trợ và lưu giữ dữ liệu. | Quy tắc quản trị và phân công vận hành. | Khảo sát và đặc tả yêu cầu | Khách hàng, BA, vận hành | Chưa làm rõ |
| <a id="oq-012"></a>OQ-012 | Vấn đề sử dụng ưu tiên, giá trị sản phẩm cần đem lại và cách đánh giá giá trị đó. | Mục tiêu sản phẩm và tiêu chí đánh giá; phân biệt giả định với bằng chứng. | Làm rõ nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đại diện sản phẩm nêu vấn đề nhiều chủ đề làm trôi tin nhắn và tài liệu. Chưa có bằng chứng từ người dùng ngoài (DEC-030); kết quả mong muốn và cách đánh giá còn mở |
| <a id="oq-013"></a>OQ-013 | Đầu mối, thẩm quyền xác nhận, người tham gia và lịch khảo sát. | Hồ sơ nhân sự và quyết định về cách làm rõ nhu cầu tại SCDC-ORG-001. | Khởi tạo | Vg | Đã xử lý 2026-09-30: đầu mối tại SCDC-ORG-001; không tuyển người hoặc lập lịch khảo sát bên ngoài trong đợt này theo DEC-030 |

Vai trò đã xác định và phương án kiêm nhiệm được ghi tại
[SCDC-ORG-001](project.md#team). Khi đóng một vấn đề, bổ sung
kết luận, ngày xử lý và tài liệu chứa kết quả.

<a id="changes"></a>

## 3. Thay đổi trong lần hợp nhất

| Ngày | Thay đổi | Tác động |
|---|---|---|
| 2026-09-27 | Loại nhóm chat riêng ngoài server và cuộc gọi trong nhóm đó | REQ-006 không được dùng lại; SCP-005/006/007 chỉ có các ngữ cảnh còn trong phạm vi |
| 2026-09-30 | Không khảo sát bên ngoài trong đợt này | DEC-030; đầu vào từ đại diện sản phẩm chưa phải bằng chứng người dùng |
| 2026-10-03 | Hợp nhất tài liệu; xác nhận DEC-060/061 và đồng bộ DEC-062 theo source | Cập nhật mục tiêu kiến trúc, API lỗi, form đăng ký; không đổi code hoặc đánh dấu nghiệm thu |

DEC-011 không nằm trong danh sách quyết định hiện hành của hồ sơ gốc; mã này không được cấp lại. OQ-013 đã xử lý theo DEC-030; các OQ khác giữ trạng thái từng dòng, không tính toàn bộ là đã đóng.

Khi thay đổi quyết định: ghi lý do, người xác nhận, ngày và mã bị thay thế; cập nhật requirement, scope, thiết kế, hợp đồng và ca kiểm thử bị ảnh hưởng trong cùng thay đổi. Lịch sử trao đổi chi tiết nằm trong [hồ sơ lưu trữ](archive/README.md).

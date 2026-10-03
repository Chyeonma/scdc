# SCDC — Quyết định và vấn đề cần làm rõ

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-LOG-001 |
| Phiên bản | 1.25 |
| Cập nhật | 2026-10-03 |
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
| DEC-016 | Cho phép tìm người nhận bằng tên tài khoản và nhắn tin riêng ngay, không yêu cầu kết bạn hoặc cùng cộng đồng. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; cách tìm theo một phần tên được làm rõ tại DEC-019, ngoại lệ còn cần đặc tả. | Đã xác định cách bắt đầu hội thoại riêng |
| DEC-017 | Đợt triển khai nhắn tin riêng đầu tiên chỉ hỗ trợ tin nhắn văn bản. | Đại diện sản phẩm chọn phương án 1 ngày 2026-09-30 tại SCDC-DIS-001; nội dung hình ảnh và file tài liệu cần xác định ở đợt sau. | Đã xác định nội dung cho đợt đầu |
| DEC-018 | Để việc xử lý khi người nhận không muốn nhận tin từ một tài khoản cụ thể sang đợt sau. | Đại diện sản phẩm chọn phương án 3 ngày 2026-09-30 tại SCDC-DIS-001; cơ chế cụ thể chưa được xác định. | Đã xác định thứ tự triển khai |
| DEC-019 | Cho tìm người để nhắn riêng theo một phần tên tài khoản hoặc tên hiển thị. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; tên tài khoản duy nhất theo DEC-054 dùng phân biệt tên hiển thị; chi tiết tìm kiếm còn cần rà soát. | Đã xác định trường và cách khớp tìm kiếm |
| DEC-020 | Trong đợt DM đầu, người gửi có thể sửa tin văn bản của mình bất cứ lúc nào với dấu “Đã sửa”, và xóa cho cả hai người với dòng “Tin nhắn đã bị xóa”. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; DEC-052 chốt chỉ giữ bản nội dung mới nhất; DEC-053 chốt giới hạn nội dung. | Đã xác định thao tác và hiển thị |
| DEC-021 | Hiển thị trạng thái đã gửi khi hệ thống đã lưu tin và lỗi gửi khi không lưu được; khi lỗi, người gửi bấm thử lại. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; hành vi khi người nhận vắng mặt ở DEC-035, kết quả chống trùng ở DEC-037; cơ chế kỹ thuật còn mở. | Đã xác định ý nghĩa trạng thái và cách thử lại |
| DEC-022 | Cho tham gia cộng đồng qua liên kết mời và tìm kiếm. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; cộng đồng hiển thị và điều kiện tham gia còn mở. | Đã xác định hai cách tiếp cận |
| DEC-023 | Để gửi file tài liệu trong phòng theo chủ đề sang đợt sau. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; thời điểm và quy tắc file chưa xác định. | Đã xác định thứ tự triển khai |
| DEC-024 | Chỉ cộng đồng công khai xuất hiện trong tìm kiếm. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền thay đổi trạng thái công khai còn cần đặc tả. | Đã xác định phạm vi hiển thị |
| DEC-025 | Cộng đồng công khai mới tạo mặc định cho vào ngay; cộng đồng có thể cấu hình chờ duyệt. Liên kết mời hợp lệ cho vào ngay dù cộng đồng yêu cầu duyệt khi tham gia qua tìm kiếm. | Đại diện sản phẩm làm rõ ngày 2026-09-30 tại SCDC-DIS-001; người được đổi cấu hình và thời hạn mời ở DEC-029/028, quyền thu hồi ở DEC-044; phân quyền chi tiết còn mở. | Đã xác định mặc định và ngoại lệ lời mời |
| DEC-026 | Chỉ chủ sở hữu hoặc người được cấp quyền mới tạo phòng theo chủ đề. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; ma trận quyền chi tiết còn mở. | Đã xác định điều kiện tạo phòng |
| DEC-027 | Thành viên chỉ nhìn thấy phòng theo chủ đề mà mình được cấp quyền xem. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền xem mặc định được chốt tại DEC-033, ma trận quyền chi tiết còn mở. | Đã xác định nguyên tắc hiển thị |
| DEC-028 | Người tạo liên kết mời được chọn thời hạn hiệu lực. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; quyền tạo ở DEC-031, quyền thu hồi ở DEC-044; giá trị thời hạn cụ thể còn mở. | Đã xác định khả năng đặt thời hạn |
| DEC-029 | Chủ sở hữu và người được cấp quyền có thể đổi chế độ tham gia cộng đồng giữa vào ngay và chờ duyệt. | Đại diện sản phẩm chọn ngày 2026-09-30 tại SCDC-DIS-001; ma trận quyền chi tiết còn mở. | Đã xác định người được đổi chế độ |
| DEC-030 | Không tổ chức khảo sát người dùng bên ngoài trong đợt hiện tại; tiếp tục đặc tả từ đầu vào của đại diện sản phẩm và ghi rõ các giả định chưa kiểm chứng. | Đại diện sản phẩm quyết định ngày 2026-09-30; SCDC-DIS-001 là đầu vào, SCDC-FR-DM-001 và SCDC-FR-COM-001 là bản nháp đặc tả. Kiểm thử và nghiệm thu vẫn theo SCDC-PRC-001. | Áp dụng cho đợt hiện tại |
| DEC-031 | Chỉ chủ sở hữu và người được cấp quyền được tạo liên kết mời vào cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền thu hồi được chốt ở DEC-044, cách cấp/thu hồi vai trò còn mở tại OQ-004. | Đã xác định quyền tạo lời mời |
| DEC-032 | Chỉ chủ sở hữu và người được cấp quyền được duyệt yêu cầu tham gia cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quy tắc từ chối và hủy yêu cầu còn mở. | Đã xác định quyền duyệt |
| DEC-033 | Phòng theo chủ đề mới tạo mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | Đại diện sản phẩm trả lời ngày 2026-09-30; cách cấu hình quyền chi tiết còn mở. | Đã xác định quyền xem mặc định |
| DEC-034 | Tin nhắn văn bản trong phòng ở đợt đầu cho người gửi sửa và xóa như tin riêng. | Đại diện sản phẩm trả lời ngày 2026-09-30; áp dụng dấu “Đã sửa”, dòng thay thế khi xóa và trạng thái gửi của DEC-020, DEC-021. | Đã xác định thao tác tin trong phòng |
| DEC-035 | Tin nhắn đã được hệ thống lưu khi người nhận chưa mở ứng dụng phải xuất hiện trong hội thoại khi người nhận mở lại. | Đại diện sản phẩm xác nhận qua ví dụ A gửi cho B ngày 2026-09-30; áp dụng cho DM, và lịch sử phòng được xem khi thành viên có quyền truy cập. | Đã xác định hành vi ngoại tuyến |
| DEC-036 | Đợt đầu dùng cả email và tên tài khoản cho tài khoản người dùng. | Đại diện sản phẩm chọn “Cả email lẫn tên tài khoản” ngày 2026-09-30; DEC-054 ngày 2026-10-03 đã xác nhận đăng ký có cả hai, đăng nhập bằng một trong hai với mật khẩu và quy tắc hồ sơ. | Đã được cụ thể hóa bởi DEC-054 |
| DEC-037 | Khi người gửi bấm thử lại sau kết quả không rõ, hệ thống chỉ tạo một tin nhắn cho cùng thao tác gửi. | Đại diện sản phẩm chọn ngày 2026-09-30; cách thực hiện kỹ thuật và thời hạn khóa chống trùng cần thiết kế. | Đã xác định kết quả mong muốn |
| DEC-038 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mà mình được phép xem. | Đại diện sản phẩm chọn ngày 2026-09-30; quy tắc khi rời/mất quyền còn mở. | Đã xác định quyền lịch sử khi tham gia |
| DEC-039 | Chủ sở hữu và người được cấp quyền được thay đổi danh sách người có quyền xem phòng. | Đại diện sản phẩm chọn ngày 2026-09-30; mô hình vai trò, ngoại lệ cá nhân và người quản lý vai trò được chốt tại DEC-055–058. | Đã xác định người quản lý quyền xem |
| DEC-040 | Trong đợt đầu, mọi thành viên có quyền xem phòng đều được gửi tin văn bản trong phòng đó. | Đại diện sản phẩm chọn ngày 2026-09-30; chưa có quyền chỉ đọc riêng. | Đã xác định quyền gửi mặc định |
| DEC-041 | Người dùng phải xác minh email trước khi được nhắn tin. | Đại diện sản phẩm trả lời ngày 2026-09-30; DEC-051 chốt trước xác minh chỉ dùng xác minh/khôi phục; cách gửi, thời hạn và bảo vệ liên kết còn cần thiết kế. | Đã xác định điều kiện nhắn tin |
| DEC-042 | Đợt đầu có luồng đặt lại mật khẩu qua email. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời hạn hiệu lực và quy tắc bảo vệ liên kết đặt lại cần thiết kế. | Đã xác định kênh khôi phục |
| DEC-043 | Có thể tham gia cộng đồng riêng tư bằng liên kết mời hoặc được thêm trực tiếp. | Đại diện sản phẩm trả lời ngày 2026-09-30; người có quyền thêm trực tiếp và hành vi lời mời còn mở. | Đã xác định hai cách tham gia |
| DEC-044 | Người có quyền tạo liên kết mời có thể thu hồi liên kết trước hạn. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền tạo theo DEC-031. | Đã xác định quyền thu hồi |
| DEC-045 | Thành viên thường có thể tự rời cộng đồng. | Đại diện sản phẩm trả lời ngày 2026-09-30; quyền truy cập sau khi rời và việc tham gia lại cần đặc tả. | Đã xác định quyền rời |
| DEC-046 | Người nhận cuộc gọi riêng phải bấm chấp nhận trước khi cuộc gọi bắt đầu. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời gian đổ chuông và cách xử lý không trả lời còn mở. | Đã xác định bước nhận cuộc gọi |
| DEC-047 | Để việc lưu cuộc gọi nhỡ trong hội thoại riêng sang đợt sau. | Đại diện sản phẩm trả lời ngày 2026-09-30; đợt đầu vẫn cần hiển thị kết quả không được nhận ở phiên gọi hiện tại. | Đã xác định thứ tự triển khai |
| DEC-048 | Thành viên nhìn thấy phòng thoại có thể vào phòng ngay. | Đại diện sản phẩm trả lời ngày 2026-09-30; người không có quyền xem phòng vẫn không được vào. | Đã xác định quyền vào phòng thoại |
| DEC-049 | Đợt đầu cho phép nhiều người chia sẻ màn hình cùng lúc trong một phòng thoại. | Đại diện sản phẩm trả lời ngày 2026-09-30; số luồng tối đa và tải cần thử nghiệm để chốt. | Đã xác định khả năng chia sẻ đồng thời |
| DEC-050 | Khi mất mạng trong cuộc gọi và mạng trở lại, ứng dụng tự kết nối lại. | Đại diện sản phẩm trả lời ngày 2026-09-30; thời gian chờ và trường hợp không thể khôi phục còn mở. | Đã xác định cách khôi phục kết nối |
| DEC-051 | Tài khoản chưa xác minh email chỉ dùng xác minh email hoặc khôi phục mật khẩu, chưa vào các chức năng ứng dụng. | Đại diện sản phẩm chọn ngày 2026-10-03; cụ thể hóa phần còn mở của DEC-041 tại SCDC-FR-ACC-001. | Đã xác nhận |
| DEC-052 | Khi sửa tin DM, chỉ giữ nội dung mới nhất, không cung cấp lịch sử bản cũ; vẫn hiển thị “Đã sửa”. | Đại diện sản phẩm chọn ngày 2026-10-03; áp dụng cho tin phòng theo nguyên tắc sửa/xóa như DM ở DEC-034. Chính sách sao lưu/lưu giữ vẫn thuộc OQ-011. | Đã xác nhận |
| DEC-053 | Tin văn bản DM và phòng tối đa 2.000 ký tự, cho xuống dòng và emoji, từ chối tin rỗng hoặc chỉ có khoảng trắng. | Đại diện sản phẩm chọn ngày 2026-10-03; cách đếm cụm ký tự cần rà soát trong thiết kế và dữ liệu kiểm thử. | Đã xác nhận quy tắc sản phẩm |
| DEC-054 | Đăng ký bằng email, tên tài khoản và mật khẩu; đăng nhập bằng email hoặc tên tài khoản. Tên tài khoản duy nhất, chưa cho đổi ở đợt đầu; tên hiển thị được đổi; email không công khai. | Đại diện sản phẩm xác nhận bộ quy tắc ngày 2026-10-03, thay phần diễn giải chưa xác nhận của DEC-036. | Đã xác nhận |
| DEC-055 | Cấp quyền quản lý cộng đồng và quyền xem phòng qua vai trò, có ngoại lệ từng thành viên ở phòng. | Đại diện sản phẩm chọn ngày 2026-10-03; mô hình dữ liệu/cách cấu hình được rà soát ở SCDC-FR-ACL-001. | Đã xác nhận mô hình quyền |
| DEC-056 | Chủ sở hữu luôn được xem mọi phòng trong cộng đồng của mình, kể cả phòng giới hạn thành viên. | Đại diện sản phẩm chọn ngày 2026-10-03; không vượt qua điều kiện tài khoản/phiên, không áp dụng cho DM hoặc cộng đồng khác. | Đã xác nhận |
| DEC-057 | Khi quyền xem từ các vai trò xung đột, từ chối thắng; ngoại lệ của cá nhân được áp dụng sau cùng. | Đại diện sản phẩm chọn ngày 2026-10-03; chủ sở hữu vẫn theo DEC-056. | Đã xác nhận thứ tự ưu tiên |
| DEC-058 | Chỉ chủ sở hữu cộng đồng được tạo/sửa vai trò và gán/thu hồi vai trò của thành viên ở đợt đầu. | Đại diện sản phẩm chọn ngày 2026-10-03; người được giao các quyền quản lý khác không tự có quyền quản lý vai trò. | Đã xác nhận |
| DEC-059 | Chuẩn bị giao diện tài khoản và DM cho desktop và trình duyệt điện thoại, có bố cục thích ứng. | Đại diện sản phẩm chọn ngày 2026-10-03; danh sách trình duyệt/phiên bản, thiết bị và ngưỡng chất lượng cần xác định, không suy thành hỗ trợ media trên mọi thiết bị. | Đã xác nhận phạm vi giao diện |

Tài liệu liên quan: [yêu cầu ban đầu](00-customer-request.md),
[Project Brief](01-project-brief.md), [dự toán và giả định](02-budget-and-assumptions.md).

## 2. Vấn đề cần làm rõ

| Mã | Nội dung cần xác định | Kết quả cần có | Thời điểm xử lý | Đầu mối dự kiến | Trạng thái |
|---|---|---|---|---|---|
| OQ-001 | Người dùng đại diện, hành trình ưu tiên, ngôn ngữ và khu vực sử dụng. | Hồ sơ người dùng và hành trình ưu tiên. | Làm rõ nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đối tượng là nhóm người bất kỳ, ưu tiên nhắn tin riêng trước rồi tham gia cộng đồng. Không khảo sát bên ngoài trong đợt này (DEC-030); còn mở về người dùng đại diện cụ thể, ngôn ngữ và khu vực |
| OQ-002 | Quy tắc đăng ký, xác thực, khôi phục tài khoản và quản lý phiên. | Luồng tài khoản và quy tắc nghiệp vụ. | Đặc tả yêu cầu | Khách hàng, BA, kỹ thuật | Cập nhật 2026-10-03: đã chốt bộ định danh/hồ sơ và chỉ xác minh/khôi phục trước xác minh email (DEC-051, DEC-054); SCDC-FR-ACC-001 có đề xuất ACC-P01–05. Còn thời hạn, chính sách mật khẩu, phiên, gửi lại và chống lạm dụng cần rà soát |
| OQ-003 | Tạo/tham gia/rời server, lời mời, nhu cầu tìm cộng đồng và quyền riêng tư. | Luồng quản lý cộng đồng và điều kiện truy cập. | Đặc tả yêu cầu | Khách hàng, BA | Có liên kết mời/tìm kiếm (DEC-022); tìm kiếm chỉ hiện cộng đồng công khai (DEC-024); mặc định vào ngay, có thể cấu hình chờ duyệt, liên kết mời hợp lệ cho vào ngay (DEC-025). Người tạo liên kết chọn thời hạn (DEC-028); chủ sở hữu/người được cấp quyền đổi chế độ tham gia, tạo/thu hồi lời mời và duyệt yêu cầu (DEC-029, DEC-031, DEC-032, DEC-044). Cộng đồng riêng tư vào qua mời hoặc được thêm trực tiếp (DEC-043); thành viên thường tự rời được (DEC-045). Còn mở người có quyền thêm trực tiếp, từ chối yêu cầu và hậu quả khi rời |
| OQ-004 | Vai trò và các thao tác được phép trong server/phòng. | Ma trận quyền. | Đặc tả yêu cầu | Khách hàng, BA | Cập nhật 2026-10-03: đã chốt vai trò + ngoại lệ cá nhân, quyền xem của chủ sở hữu, thứ tự xung đột và chỉ chủ sở hữu quản lý vai trò (DEC-055–058). Ma trận SCDC-FR-ACL-001 cụ thể hóa; còn chi tiết cấu hình và hệ quả thu hồi cần kiểm chứng |
| OQ-005 | Quy tắc gửi, nhận, lưu lịch sử; mất mạng, gửi lại; loại nội dung và tương tác. | Quy tắc nhắn tin và tiêu chí chấp nhận. | Đặc tả yêu cầu | Khách hàng, BA, UX, kỹ thuật | Cập nhật 2026-10-03: chỉ giữ nội dung sửa mới nhất; giới hạn 2.000 ký tự, cho emoji/xuống dòng, không nhận tin trống (DEC-052/053). Có ngoại lệ và hợp đồng DM đề xuất; còn phép đếm ký tự, tìm kiếm, lưu giữ và rà soát cơ chế đồng thời/chống trùng |
| OQ-006 | Số người/phòng, chất lượng thoại/video, chia sẻ màn hình và mất kết nối. | Giới hạn sử dụng, hành vi cuộc gọi và tiêu chí chất lượng. | Đặc tả yêu cầu và thiết kế | Khách hàng, UX, kỹ sư media, QA | Đã chốt nhận cuộc gọi riêng, để cuộc gọi nhỡ sang sau, vào phòng thoại theo quyền xem, nhiều người chia sẻ cùng lúc, tự kết nối lại (DEC-046–050); còn mở giới hạn và ngưỡng chất lượng |
| OQ-007 | Trình duyệt/thiết bị hỗ trợ, hiệu năng và cách nghiệm thu. | Ma trận hỗ trợ, chỉ tiêu chất lượng và phương pháp đo. | Đặc tả yêu cầu | Khách hàng, kỹ thuật, QA | Cập nhật 2026-10-03: tài khoản/DM hỗ trợ desktop và trình duyệt điện thoại với bố cục thích ứng (DEC-059). Còn ma trận phiên bản/thiết bị, chỉ tiêu hiệu năng/media và độ trễ thu hồi |
| OQ-008 | Ranh giới dịch vụ, dữ liệu, giải pháp media, triển khai và thành phần tái sử dụng. | Hồ sơ kiến trúc và đánh giá kỹ thuật. | Thiết kế kỹ thuật | Vg chủ trì, Sáng phối hợp | Cập nhật 2026-10-03: có SCDC-API-DM-001 về hợp đồng và dữ liệu tài khoản/DM; chưa chọn công nghệ, chưa duyệt thiết kế hoặc có kết quả thử nghiệm |
| OQ-009 | Phân công kiêm nhiệm, công sức đào tạo, ngày khởi động, tiến độ và dự toán điều chỉnh. | Kế hoạch nguồn lực, lịch thực hiện và chi phí chi tiết theo đội ngũ hiện có. | Lập kế hoạch thực hiện | Vg, Sáng | Cập nhật 2026-10-03: đã phân rã đầu ra Đợt 0 tại SCDC-READY-001; còn ngày công, lịch nguồn lực và dự toán điều chỉnh; chưa có cam kết ngày phát hành |
| OQ-010 | Mức sử dụng media, băng thông, lưu trữ và chi phí sau phát hành. | Mô hình chi phí vận hành. | Thiết kế vận hành | Khách hàng, kỹ thuật, vận hành | Chưa ước lượng chi tiết |
| OQ-011 | Quản trị nền tảng, xử lý vi phạm, hỗ trợ và lưu giữ dữ liệu. | Quy tắc quản trị và phân công vận hành. | Khảo sát và đặc tả yêu cầu | Khách hàng, BA, vận hành | Chưa làm rõ |
| OQ-012 | Vấn đề sử dụng ưu tiên, giá trị sản phẩm cần đem lại và cách đánh giá giá trị đó. | Mục tiêu sản phẩm và tiêu chí đánh giá; phân biệt giả định với bằng chứng. | Làm rõ nhu cầu | Khách hàng, BA/UX | Cập nhật 2026-09-30 tại SCDC-DIS-001: đại diện sản phẩm nêu vấn đề nhiều chủ đề làm trôi tin nhắn và tài liệu. Chưa có bằng chứng từ người dùng ngoài (DEC-030); kết quả mong muốn và cách đánh giá còn mở |
| OQ-013 | Đầu mối, thẩm quyền xác nhận, người tham gia và lịch khảo sát. | Hồ sơ nhân sự và quyết định về cách làm rõ nhu cầu tại SCDC-ORG-001. | Khởi tạo | Vg | Đã xử lý 2026-09-30: đầu mối tại SCDC-ORG-001; không tuyển người hoặc lập lịch khảo sát bên ngoài trong đợt này theo DEC-030 |

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
Người gửi có thể sửa tin bất cứ lúc nào và xóa cho cả hai (DEC-020).
Tìm kiếm chỉ hiển thị cộng đồng công khai (DEC-024); cộng đồng tìm được
cho vào ngay theo mặc định hoặc chờ duyệt tùy cấu hình; liên kết mời hợp
lệ luôn cho vào ngay (DEC-025). Chỉ chủ sở hữu hoặc người được cấp quyền
mới tạo phòng (DEC-026); thành viên chỉ thấy phòng được phép xem
(DEC-027). Tin đã xóa hiện dòng thay thế (DEC-020); người gửi bấm thử lại
khi lỗi gửi (DEC-021).
Tìm người theo một phần tên tài khoản hoặc tên hiển thị (DEC-019);
“Đã gửi” là hệ thống đã lưu tin (DEC-021), tin sửa có dấu “Đã sửa”
(DEC-020). Người tạo liên kết mời chọn thời hạn (DEC-028); chủ sở hữu
và người được cấp quyền đổi chế độ tham gia (DEC-029).
Chi tiết đầu vào và nội dung cần hỏi tiếp được quản lý tại
[SCDC-DIS-001](../02-discovery/01-users-and-needs.md).

OQ-001, OQ-005 và OQ-012 được bổ sung đầu vào, tiếp tục mở. Chưa có khảo sát
người dùng bên ngoài hoặc quyết định bổ sung tính năng luồng thảo luận,
tìm kiếm tin nhắn/tài liệu hay kho tài liệu. Phạm vi bàn giao hiện hành
chưa thay đổi.

## 7. Quyết định về khảo sát ngày 2026-09-30

Đại diện sản phẩm chọn bỏ qua phỏng vấn/khảo sát người dùng bên ngoài
trong đợt hiện tại (DEC-030). Nhóm tiếp tục làm rõ yêu cầu với đại diện
sản phẩm, ghi các quyết định trong SCDC-DIS-001 và phát triển bản nháp
đặc tả. Những mô tả về vấn đề người dùng và giá trị sản phẩm vẫn là giả
định chưa kiểm chứng; không ghi chúng như kết quả nghiên cứu người dùng.

OQ-013 được xử lý cho đợt này vì không cần tuyển người hoặc xếp lịch.
OQ-001 và OQ-012 tiếp tục mở về người dùng đại diện, kết quả mong muốn
và cách đánh giá. Quyết định này không thay thế kiểm thử và nghiệm thu
theo [quy trình dự án](../development-process.md).

## 8. Phiên chuẩn bị phát triển ngày 2026-10-03

Theo yêu cầu tiếp tục hoàn thiện `docs/project`, phiên này tập trung vào
Đợt 0: tài khoản, DM văn bản, ma trận quyền và đầu vào thiết kế/kiểm thử.
Đại diện sản phẩm đã chọn các phương án DEC-051 đến DEC-059 qua trao đổi
trực tiếp. Đây là quyết định sản phẩm, không phải kết quả khảo sát ngoài.

Đã bổ sung ma trận quyền, wireframe văn bản tài khoản/DM, hợp đồng dữ
liệu/API, ca kiểm thử và bảng theo dõi sẵn sàng. Các phương án kỹ thuật
và ACC-P* vẫn là đề xuất, không coi câu trả lời sản phẩm là duyệt toàn
bộ thiết kế. Chưa có mã nguồn, kết quả chạy thử hoặc nghiệm thu được ghi
nhận trong phiên tài liệu này. Các điều kiện còn lại được quản lý tại
[SCDC-READY-001](../06-planning/02-development-readiness.md).

## 9. Lịch sử phiên bản

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
| 1.12 | 2026-09-30 | Làm rõ DEC-020; ghi nhận DEC-024 đến DEC-026 về tìm kiếm, chế độ tham gia cộng đồng và quyền tạo phòng. |
| 1.13 | 2026-09-30 | Làm rõ DEC-020, DEC-021, DEC-025 và ghi nhận DEC-027 về hiển thị phòng theo quyền xem. |
| 1.14 | 2026-09-30 | Làm rõ DEC-019 đến DEC-021; ghi nhận DEC-028, DEC-029 về lời mời và quyền đổi chế độ tham gia. |
| 1.15 | 2026-09-30 | Ghi nhận DEC-030 về việc bỏ khảo sát bên ngoài trong đợt hiện tại; cập nhật OQ-001, OQ-012 và xử lý OQ-013. |
| 1.16 | 2026-09-30 | Ghi nhận DEC-031 đến DEC-034 về quyền tạo lời mời, duyệt yêu cầu, quyền xem phòng mặc định và thao tác tin trong phòng. |
| 1.17 | 2026-09-30 | Ghi nhận DEC-035 về tin đã lưu khi người nhận chưa mở ứng dụng. |
| 1.18 | 2026-09-30 | Ghi nhận DEC-036 đến DEC-040 về tài khoản, tránh tin trùng, lịch sử phòng và quyền xem/gửi. |
| 1.19 | 2026-09-30 | Ghi nhận DEC-041 về yêu cầu xác minh email trước khi nhắn tin. |
| 1.20 | 2026-09-30 | Ghi nhận DEC-042 đến DEC-044 về khôi phục mật khẩu, tham gia cộng đồng riêng tư và thu hồi lời mời. |
| 1.21 | 2026-09-30 | Ghi nhận DEC-045 về quyền tự rời cộng đồng của thành viên thường. |
| 1.22 | 2026-09-30 | Ghi nhận DEC-046 về chấp nhận cuộc gọi riêng. |
| 1.23 | 2026-09-30 | Ghi nhận DEC-047 về việc để lưu cuộc gọi nhỡ sang đợt sau. |
| 1.24 | 2026-09-30 | Ghi nhận DEC-048 đến DEC-050 về vào phòng thoại, chia sẻ đồng thời và tự kết nối lại. |
| 1.25 | 2026-10-03 | Ghi nhận DEC-051–059 và đầu ra chuẩn bị phát triển; cập nhật trạng thái vấn đề mở, giữ riêng đề xuất và quyết định. |

[Mục lục hồ sơ](../README.md)

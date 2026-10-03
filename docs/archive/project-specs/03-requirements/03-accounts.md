# SCDC — Đặc tả tài khoản nền tảng, đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-ACC-001 |
| Phiên bản | 0.3 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Đã chốt định danh và quyền trước xác minh; chi tiết bảo vệ tài khoản/phiên đang hoàn thiện |
| Căn cứ | SCP-002, REQ-010, DEC-036, DEC-041, DEC-042 tại [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Mục tiêu và lựa chọn đã xác nhận

Người dùng tự tạo tài khoản và đăng nhập để nhắn tin, tham gia cộng đồng.
Ngày 2026-10-03, đại diện sản phẩm xác nhận: đăng ký bằng email, tên tài
khoản và mật khẩu; đăng nhập bằng email **hoặc** tên tài khoản cùng mật
khẩu (DEC-054). Tên tài khoản duy nhất và chưa cho đổi ở đợt đầu; tên
hiển thị được đổi; email không công khai.

Tên hiển thị là trường hồ sơ dùng để tìm người theo DEC-019. Định danh
duy nhất của tài khoản phải tách khỏi tên hiển thị để chọn đúng người
khi nhiều người có cùng tên. Quy tắc đổi email còn mở. Trước xác minh,
tài khoản chỉ dùng xác minh email hoặc khôi phục mật khẩu, chưa được
vào các chức năng ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh
email; sau đó vẫn phải hoàn tất xác minh trước khi đăng nhập ứng dụng.

## 2. Hành trình và quy tắc dự thảo

| Mã | Quy tắc/hành vi | Tình trạng |
|---|---|---|
| ACC-001 | Màn hình đăng ký thu thập email, tên tài khoản và mật khẩu. | DEC-054 |
| ACC-002 | Màn hình đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu. | DEC-054 |
| ACC-003 | Người đã đăng nhập có thể đổi tên hiển thị của mình; tên tài khoản duy nhất, chưa cho đổi; email không công khai. | DEC-054; trường hồ sơ khác còn cần đặc tả |
| ACC-004 | API cần nhận diện tài khoản bằng định danh ổn định, không dùng tên hiển thị làm khóa. | Đề xuất kỹ thuật để đáp ứng tìm kiếm/chọn đúng người |
| ACC-005 | Người dùng cần xác minh email trước khi gửi tin riêng hoặc tin trong phòng. | DEC-041 |
| ACC-006 | Đợt đầu có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. | DEC-042 |
| ACC-007 | Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không được vào ứng dụng; đặt lại mật khẩu không thay thế xác minh email. | DEC-051; hệ quả của việc tách hai mục đích |

## 3. Tiêu chí chấp nhận dự thảo

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-01 | Người chưa có tài khoản điền thông tin đăng ký hợp lệ. | Tài khoản được tạo ở trạng thái chờ xác minh email để có thể nhắn tin. |
| AC-ACC-02 | Người dùng nhập email cùng mật khẩu hợp lệ, rồi lặp lại với tên tài khoản cùng mật khẩu. | Cả hai cách đều truy cập cùng một tài khoản. |
| AC-ACC-03 | Hai tài khoản có cùng tên hiển thị xuất hiện trong tìm kiếm. | Người tìm chọn được đúng tài khoản qua định danh phân biệt. |
| AC-ACC-04 | Người không đăng nhập cố mở hội thoại riêng hoặc cộng đồng cần tư cách thành viên. | Hệ thống yêu cầu đăng nhập và không trả dữ liệu riêng tư. |
| AC-ACC-05 | Tài khoản chưa xác minh email thử gửi tin riêng hoặc tin trong phòng; sau đó xác minh email và thử lại. | Trước xác minh hệ thống từ chối gửi tin; sau xác minh có thể gửi theo quyền tương ứng. |
| AC-ACC-06 | Người dùng yêu cầu đặt lại mật khẩu qua email và hoàn tất luồng với thông tin hợp lệ. | Người dùng đăng nhập được bằng mật khẩu mới; thông tin đặt lại đã dùng không thể dùng lại. |
| AC-ACC-07 | Tài khoản chưa xác minh nhập đúng mật khẩu hoặc gọi API ứng dụng trực tiếp. | Không được vào ứng dụng, tìm người, đọc hoặc gửi tin; có đường xác minh/khôi phục. |
| AC-ACC-08 | Người dùng xem/sửa hồ sơ; người khác tìm tài khoản đó. | Đổi được tên hiển thị; không đổi được tên tài khoản; kết quả công khai không có email. |
| AC-ACC-09 | Hai đăng ký cùng tên tài khoản chạy đồng thời. | Chỉ một tài khoản được tạo; yêu cầu còn lại nhận lỗi dữ liệu trùng, không để lại tài khoản dở dang. |
| AC-ACC-10 | Tài khoản chưa xác minh hoàn tất đặt lại mật khẩu. | Mật khẩu mới có hiệu lực nhưng tài khoản vẫn chưa được vào ứng dụng cho tới khi xác minh email. |

AC-ACC-06 áp dụng cho tài khoản đã xác minh; AC-ACC-10 bao phủ tài khoản
chưa xác minh. Toàn bộ tiêu chí vẫn cần có kết quả chạy và xác nhận
nghiệm thu, không được đánh dấu đạt chỉ vì quy tắc đã chốt.

## 4. Luồng chi tiết và đề xuất để rà soát kỹ thuật

Đăng ký → thông báo kiểm tra email → xác minh liên kết → đăng nhập → vào
ứng dụng. Sai/hết hạn/đã dùng liên kết cần phản hồi rõ và đường yêu cầu
gửi lại. Quên mật khẩu → gửi email → mở liên kết → đặt mật khẩu mới →
đăng nhập; nếu email chưa xác minh thì quay về luồng xác minh.

| Mã đề xuất | Nội dung cụ thể để rà soát | Trạng thái/đầu mối |
|---|---|---|
| ACC-P01 | Tên tài khoản 3–32 ký tự ASCII gồm chữ, số, dấu chấm và gạch dưới; không phân biệt hoa/thường khi kiểm tra trùng. Tên hiển thị 1–64 ký tự hiển thị, cho Unicode, không chỉ khoảng trắng. | Đề xuất; Vg chốt quy tắc, Sáng/Thái thống nhất phép đếm |
| ACC-P02 | Email loại bỏ khoảng trắng bao quanh, có một quy tắc chuẩn hóa/duy nhất thống nhất; xác minh và khôi phục dùng liên kết một lần, hạn 30 phút. Gửi lại vô hiệu liên kết cũ cùng mục đích. | Đề xuất; cần kiểm tra đăng ký và gửi lại đồng thời |
| ACC-P03 | Cho yêu cầu gửi lại sau 60 giây; phản hồi khôi phục/gửi lại không tiết lộ email tồn tại. Thêm giới hạn theo tài khoản và nguồn yêu cầu với ngưỡng cấu hình được. | Đề xuất; ngưỡng chống lạm dụng cần thiết kế và thử tải |
| ACC-P04 | Phiên duy trì tối đa 30 ngày, có đăng xuất phiên hiện tại và mọi thiết bị; đặt lại mật khẩu thu hồi mọi phiên. Transport và vòng đời bằng chứng phiên cần chọn trong thiết kế. | Đề xuất; Vg/Sáng rà soát, chưa cam kết cơ chế triển khai |
| ACC-P05 | Chưa cho đổi email ở đợt đầu; hồ sơ công khai tối thiểu chỉ có tên tài khoản/tên hiển thị và định danh. | Đề xuất thu hẹp thao tác hồ sơ; cần Vg xác nhận |

Chính sách mật khẩu, giới hạn yêu cầu cụ thể, khôi phục khi mất quyền
truy cập email và trạng thái tài khoản bị khóa chưa được chọn. Các mục
ACC-P* là phương án có thể xem xét, không phải DEC hoặc tiêu chí đã duyệt.
Hợp đồng đề xuất tại [SCDC-API-DM-001](../05-architecture/02-account-dm-contracts.md),
màn hình tại [SCDC-UX-DM-001](../04-ux/02-account-dm-wireframes.md).

## 5. Cần làm rõ trước khi xác nhận toàn bộ đặc tả

| Vấn đề | Quyết định cần có |
|---|---|
| Định danh | Rà soát ACC-P01/02 về ký tự, độ dài, chuẩn hóa và email duy nhất; tên tài khoản duy nhất/không đổi đã chốt. |
| Xác minh | Rà soát thời hạn/gửi lại theo ACC-P02/03; phạm vi trước xác minh đã chốt tại DEC-051. |
| Khôi phục | Thời hạn và giới hạn yêu cầu đặt lại, xử lý email không còn truy cập được. |
| Phiên | Thời hạn, đăng xuất, đăng xuất mọi thiết bị và xử lý phiên hết hạn. |
| Hồ sơ | Trường khác ngoài tên hiển thị, khả năng đổi email theo ACC-P05; email không công khai đã chốt. |
| Chống lạm dụng | Giới hạn đăng ký/đăng nhập/tìm kiếm và xử lý tài khoản bị khóa. |

Các mục trên thuộc OQ-002; không suy diễn thành quyết định đã chốt.

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo đặc tả tài khoản nền tảng từ lựa chọn dùng email và tên tài khoản. |
| 0.2 | 2026-09-30 | Bổ sung điều kiện xác minh email trước khi nhắn tin và đặt lại mật khẩu qua email. |
| 0.3 | 2026-10-03 | Chốt bộ quy tắc định danh và quyền trước xác minh; bổ sung ngoại lệ, tiêu chí và phương án chi tiết để rà soát. |

[Mục lục hồ sơ](../README.md)

# SCDC — Đặc tả tài khoản nền tảng, đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-ACC-001 |
| Phiên bản | 0.2 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Bản nháp — cần xác nhận chi tiết đăng ký, xác minh và phiên |
| Căn cứ | SCP-002, REQ-010, DEC-036, DEC-041, DEC-042 tại [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Mục tiêu và cách diễn giải lựa chọn

Người dùng tự tạo tài khoản và đăng nhập để nhắn tin, tham gia cộng đồng.
Đại diện sản phẩm chọn dùng **cả email lẫn tên tài khoản**. Bản nháp này
diễn giải lựa chọn đó thành: khi đăng ký người dùng cung cấp email, tên
tài khoản và mật khẩu; khi đăng nhập có thể dùng email **hoặc** tên tài
khoản cùng mật khẩu. Cách diễn giải cần được đại diện sản phẩm rà soát
trước khi xác nhận đặc tả.

Tên hiển thị là trường hồ sơ dùng để tìm người theo DEC-019. Định danh
duy nhất của tài khoản phải tách khỏi tên hiển thị để chọn đúng người
khi nhiều người có cùng tên. Quy tắc đổi email, đổi tên tài khoản và
đổi tên hiển thị còn mở.

## 2. Hành trình và quy tắc dự thảo

| Mã | Quy tắc/hành vi | Tình trạng |
|---|---|---|
| ACC-001 | Màn hình đăng ký thu thập email, tên tài khoản và mật khẩu. | Diễn giải DEC-036, cần xác nhận |
| ACC-002 | Màn hình đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu. | Diễn giải DEC-036, cần xác nhận |
| ACC-003 | Người đã đăng nhập có thể xem/sửa hồ sơ theo quyền của mình; tên hiển thị có thể dùng để tìm người. | Cần đặc tả trường và quy tắc đổi |
| ACC-004 | API cần nhận diện tài khoản bằng định danh ổn định, không dùng tên hiển thị làm khóa. | Đề xuất kỹ thuật để đáp ứng tìm kiếm/chọn đúng người |
| ACC-005 | Người dùng cần xác minh email trước khi gửi tin riêng hoặc tin trong phòng. | DEC-041 |
| ACC-006 | Đợt đầu có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. | DEC-042 |

## 3. Tiêu chí chấp nhận dự thảo

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-01 | Người chưa có tài khoản điền thông tin đăng ký hợp lệ. | Tài khoản được tạo ở trạng thái chờ xác minh email để có thể nhắn tin. |
| AC-ACC-02 | Người dùng nhập email cùng mật khẩu hợp lệ, rồi lặp lại với tên tài khoản cùng mật khẩu. | Cả hai cách đều truy cập cùng một tài khoản. |
| AC-ACC-03 | Hai tài khoản có cùng tên hiển thị xuất hiện trong tìm kiếm. | Người tìm chọn được đúng tài khoản qua định danh phân biệt. |
| AC-ACC-04 | Người không đăng nhập cố mở hội thoại riêng hoặc cộng đồng cần tư cách thành viên. | Hệ thống yêu cầu đăng nhập và không trả dữ liệu riêng tư. |
| AC-ACC-05 | Tài khoản chưa xác minh email thử gửi tin riêng hoặc tin trong phòng; sau đó xác minh email và thử lại. | Trước xác minh hệ thống từ chối gửi tin; sau xác minh có thể gửi theo quyền tương ứng. |
| AC-ACC-06 | Người dùng yêu cầu đặt lại mật khẩu qua email và hoàn tất luồng với thông tin hợp lệ. | Người dùng đăng nhập được bằng mật khẩu mới; thông tin đặt lại đã dùng không thể dùng lại. |

## 4. Cần làm rõ trước khi xác nhận

| Vấn đề | Quyết định cần có |
|---|---|
| Định danh | Quy tắc tên tài khoản/email duy nhất, ký tự, độ dài, chữ hoa/thường và khả năng đổi. |
| Xác minh | Cách gửi/xác nhận thông tin xác minh, thời hạn hiệu lực, gửi lại và các hành vi được phép trước xác minh ngoài nhắn tin. |
| Khôi phục | Thời hạn và giới hạn yêu cầu đặt lại, xử lý email không còn truy cập được. |
| Phiên | Thời hạn, đăng xuất, đăng xuất mọi thiết bị và xử lý phiên hết hạn. |
| Hồ sơ | Trường công khai, quyền sửa và việc có hiển thị email cho người khác hay không. |
| Chống lạm dụng | Giới hạn đăng ký/đăng nhập/tìm kiếm và xử lý tài khoản bị khóa. |

Các mục trên thuộc OQ-002; không suy diễn thành quyết định đã chốt.

## 5. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo đặc tả tài khoản nền tảng từ lựa chọn dùng email và tên tài khoản. |
| 0.2 | 2026-09-30 | Bổ sung điều kiện xác minh email trước khi nhắn tin và đặt lại mật khẩu qua email. |

[Mục lục hồ sơ](../README.md)

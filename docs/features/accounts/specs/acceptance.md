# SCDC — Tiêu chí và kiểm thử tài khoản

AC/TC xác định điều kiện cần kiểm chứng. Assertion trong source và khoảng trống tại [status](../status.md); kết quả chạy phải gắn build/commit.

<a id="acceptance"></a>

## Tiêu chí chấp nhận

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
| AC-ACC-11 | Đăng ký/sửa hồ sơ với định danh ở biên độ dài, hoa/thường và khoảng trắng đầu/cuối. | Username/email có cùng quy tắc chuẩn hóa và unique; tên hiển thị Unicode đếm UTF-16; không cho đổi username/email. |
| AC-ACC-12 | Mật khẩu ở biên 8/128, thiếu chữ/số; sau đó đăng nhập sai 5 lần. | Từ chối mật khẩu trái policy; từ lần sai thứ 5 khóa 15 phút, kể cả nhập đúng trong lúc khóa; không làm mất mật khẩu hay dữ liệu tài khoản. |
| AC-ACC-13 | Refresh gần hạn phiên 30 ngày rồi thử sau hạn. | Rotation không kéo dài phiên; phiên hết hạn yêu cầu đăng nhập; access token theo mốc 15 phút, khi đo phải ghi clock skew. |
| AC-ACC-14 | Gửi lại verify/reset trước/sau 60 giây và dùng link cũ/mới. | Không cấp thêm token trong cooldown; sau khi cấp mới link cũ cùng mục đích không dùng được; mục đích verify/reset độc lập. |
| AC-ACC-15 | Hai request đồng thời dùng một link verify hoặc reset. | Chỉ một thao tác consume thành công; request còn lại không thực hiện thay đổi; liên kết dùng lại bị từ chối. |
| AC-ACC-16 | Thu hồi phiên khác, phiên hiện tại, logout-all và đổi/reset mật khẩu. | Từ chối request mới từ đúng các phiên bị thu hồi; đổi/reset thu hồi tất cả; phiên khác còn sống khi chỉ thu hồi một phiên. |
| AC-ACC-17 | Sửa các trường hồ sơ đã chốt; thử sửa username/email hoặc tải avatar. | Tên hiển thị/bio/locale/timezone cập nhật hợp lệ; v1 không cung cấp thao tác đổi định danh hoặc tải avatar; không tuyên bố tính năng chỉ từ cột SQL. |
| AC-ACC-18 | Tài khoản mất quyền truy cập email tìm cách khôi phục. | Chỉ có luồng qua email đã đăng ký; thông tin trợ giúp không hứa có khôi phục thủ công hoặc qua kênh khác trong v1. |
| AC-ACC-19 | Đăng ký/quên mật khẩu ngoài Development và nhận email thật. | Response không có token sử dụng được; nhận link đúng domain/mục đích/hạn, hoàn tất luồng; tiếp nhận outbox không được ghi thành email đã giao. |
| AC-ACC-20 | Mở link qua GET, gửi lại/consume/reset đồng thời và reset mật khẩu sai policy | GET không consume; một token dùng một lần; token verify/reset độc lập; reset sai không mất token |
| AC-ACC-21 | Email unknown/verified/unavailable/cooldown gọi resend/forgot | Production cùng 202/body accepted; không trả trạng thái tài khoản/token hoặc retry time riêng |
| AC-ACC-22 | Đổi và đặt lại mật khẩu với mật khẩu mới trùng mật khẩu hiện tại. | Đổi bị từ chối bằng `Identity.PasswordUnchanged`, không đổi dữ liệu hoặc thu hồi phiên/token reset; reset hợp lệ thành công, consume token, đổi stamp và thu hồi mọi phiên. Cả hai tuân policy mật khẩu; trạng thái xác minh không đổi (DEC-113). |

AC-ACC-06 áp dụng cho tài khoản đã xác minh; AC-ACC-10 bao phủ tài khoản
chưa xác minh. Toàn bộ tiêu chí vẫn cần có kết quả chạy và xác nhận
nghiệm thu, không được đánh dấu đạt chỉ vì quy tắc đã chốt.

Đăng ký hợp lệ ở AC-ACC-01 bao gồm tên hiển thị bắt buộc theo DEC-062. Chưa đánh dấu tiêu chí đạt chỉ vì có code hoặc ca test.

<a id="tests"></a>

## Ca kiểm thử

Dữ liệu: A là tài khoản đã xác minh; U chưa xác minh; A1/A2 là hai phiên của A. Chuẩn bị email thử/hoặc token Development, DB thử riêng, phiên trình duyệt độc lập và khả năng gửi request đồng thời.
| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-01 | Đăng ký hợp lệ; dùng email và tên tài khoản đăng nhập sau xác minh | Cả hai cách vào cùng tài khoản; tên tài khoản không đổi | AC-ACC-01, AC-ACC-02 |
| TC-ACC-02 | U nhập đúng mật khẩu; thử mở hội thoại/tìm người/API ứng dụng | Không truy cập ứng dụng; có đường xác minh/khôi phục | AC-ACC-07, DEC-051 |
| TC-ACC-03 | U đặt lại mật khẩu rồi đăng nhập trước/sau xác minh | Khôi phục không tự xác minh; chỉ sau xác minh mới vào ứng dụng | AC-ACC-10 |
| TC-ACC-04 | Hai request đăng ký cùng tên tài khoản đồng thời | Một tài khoản; request còn lại lỗi trùng; không có dữ liệu dở dang | AC-ACC-09 |
| TC-ACC-05 | A đổi tên hiển thị; B tìm tên đó; thử đổi tên tài khoản | Tên hiển thị mới xuất hiện; tên tài khoản giữ nguyên; dữ liệu công khai không có email | AC-ACC-03, AC-ACC-08 |
| TC-ACC-06 | Dùng liên kết xác minh/reset sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác trái phép; không lộ token trong UI/log | AC-ACC-06; thời hạn 30 phút theo DEC-065 |
| TC-ACC-07 | Gửi yêu cầu khôi phục/gửi lại với email tồn tại và không tồn tại | Phản hồi công khai có cùng ý nghĩa; không tiết lộ tài khoản | ACC-P03; cooldown đã chốt, limiter bổ sung hoãn DEC-089 |
| TC-ACC-08 | Phiên hết hạn/đăng xuất, rồi dùng lại bằng chứng phiên hoặc kết nối cũ | Request mới bị từ chối; kết nối chat ngừng nhận dữ liệu trong ≤5 giây sau commit thu hồi | AC-ACC-04/16, DEC-083; cơ chế realtime còn cần triển khai |
| TC-ACC-09 | Biên username 2/3/32/33; tên hiển thị 0/1/64/65 UTF-16; hai email/username chỉ khác hoa thường; khoảng trắng đầu/cuối | Chuẩn hóa/validation nhất quán; một định danh duy nhất; không lưu tài khoản dở dang | AC-ACC-11 |
| TC-ACC-10 | Mật khẩu 7/8/128/129, chỉ chữ/chỉ số; 5 lần sai rồi mật khẩu đúng trước/sau hết khóa | Policy và lockout theo DEC-064; thử đúng sau hết khóa có thể đăng nhập | AC-ACC-12 |
| TC-ACC-11 | Đồng hồ thử tại biên hạn access/phiên và refresh trước hạn phiên | Không gia hạn phiên bằng refresh; ghi clock skew 30 giây khi đo JWT | AC-ACC-13 |
| TC-ACC-12 | Gửi lại verify/reset ở giây 59/60; hai request đồng thời; dùng link cũ/mới | Cooldown nhất quán, tối đa một link mới; mục đích độc lập | AC-ACC-14; cần implementation resend/limiter |
| TC-ACC-13 | Hai request dùng cùng verify token và reset token | Chỉ một consume thành công; request còn lại lỗi token | AC-ACC-15; verify concurrency cần kiểm chứng |
| TC-ACC-14 | A1 thu hồi A2 rồi gọi API trên hai phiên; lặp với phiên hiện tại/logout-all/đổi/reset mật khẩu | Đúng phạm vi phiên bị thu hồi; không suy test HTTP thành test realtime | AC-ACC-16 |
| TC-ACC-15 | Sửa từng trường hồ sơ và dữ liệu vượt giới hạn; xem thông tin trợ giúp khi mất email | Trường hợp lệ lưu được; định danh không đổi; không hứa khôi phục khác email | AC-ACC-17/18 |
| TC-ACC-16 | Ngoài Development, nhận email verify/reset từ worker; dừng worker rồi retry; cấp link mới trước khi email cũ được giao | Không lộ token ở response/log; email dùng link đúng; link bị thay thế không còn hợp lệ | AC-ACC-19; cần worker/provider |
| TC-ACC-17 | Consume và resend tranh khóa; reset sai mật khẩu rồi dùng lại token đúng | Kết quả theo thứ tự commit; rollback không cấp mail; reset sai không consume | AC-ACC-14/15/20 |
| TC-ACC-18 | GET link, fragment/URL sau đọc, unknown/verified/unavailable/cooldown; worker lease hết khi provider đã nhận | GET không đổi DB; URL bỏ token; response không lộ trạng thái; email lặp vẫn một consume | AC-ACC-19/20/21 |
| TC-ACC-19 | A có A1/A2 và token reset còn hạn: A1 đổi sang mật khẩu hiện tại rồi dùng token reset để đặt lại chính mật khẩu đó; lặp reset cho U sau khi triển khai ACC-GAP-01 | Change trả 400 `Identity.PasswordUnchanged`; mật khẩu/stamp/phiên/token reset giữ nguyên. Reset trả 204, đổi stamp, A1/A2 và refresh token cũ bị từ chối, token reset dùng lại bị từ chối; vẫn đăng nhập được bằng mật khẩu đó. U vẫn chưa xác minh và chưa được login. Ca chưa chạy; chưa có assertion tự động. | AC-ACC-22, UC-ACC-06/08, DEC-113 |

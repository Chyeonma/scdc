# SCDC — Quy tắc và quyền tài khoản

Nguồn chuẩn cho ACC-001–016 và ACL-01/02; giữ các quyết định DEC được dẫn chiếu.

<a id="requirements"></a>

## Phạm vi và quy tắc

### Mục tiêu và lựa chọn đã xác nhận

Người dùng tự tạo tài khoản và đăng nhập để nhắn tin, tham gia cộng đồng.
Ngày 2026-10-03, đại diện sản phẩm xác nhận: đăng ký bằng email, tên tài khoản, tên hiển thị và mật khẩu; đăng nhập bằng email **hoặc** tên tài khoản cùng mật
khẩu (DEC-054). Tên tài khoản duy nhất và chưa cho đổi ở đợt đầu; tên
hiển thị được đổi; email không công khai.

Tên hiển thị là trường hồ sơ dùng để tìm người theo DEC-019. Định danh
duy nhất của tài khoản phải tách khỏi tên hiển thị để chọn đúng người
khi nhiều người có cùng tên. v1 chưa cho đổi email theo DEC-063. Trước xác minh,
tài khoản chỉ dùng xác minh email hoặc khôi phục mật khẩu, chưa được
vào các chức năng ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh
email; sau đó vẫn phải hoàn tất xác minh trước khi đăng nhập ứng dụng.

### Hành trình và quy tắc

| Mã | Quy tắc/hành vi | Tình trạng |
|---|---|---|
| ACC-001 | Màn hình đăng ký thu thập email, tên tài khoản, tên hiển thị và mật khẩu. | DEC-054, DEC-062 |
| ACC-002 | Màn hình đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu. | DEC-054 |
| ACC-003 | Người đã đăng nhập sửa tên hiển thị, giới thiệu ngắn, ngôn ngữ và múi giờ của mình; tên tài khoản/email không cho đổi; email không công khai. | DEC-054, DEC-063, DEC-066 |
| ACC-004 | API cần nhận diện tài khoản bằng định danh ổn định, không dùng tên hiển thị làm khóa. | Đề xuất kỹ thuật để đáp ứng tìm kiếm/chọn đúng người |
| ACC-005 | Người dùng cần xác minh email trước khi gửi tin riêng hoặc tin trong phòng. | DEC-041 |
| ACC-006 | Đợt đầu có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. | DEC-042 |
| ACC-007 | Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không được vào ứng dụng; đặt lại mật khẩu không thay thế xác minh email. | DEC-051; hệ quả của việc tách hai mục đích |
| ACC-008 | Tên tài khoản 3–32 ký tự ASCII chữ/số/`_`/`.`; duy nhất không phân biệt hoa/thường, chưa cho đổi. Tên hiển thị Unicode dài 1–64 đơn vị UTF-16 sau trim, không chỉ khoảng trắng; không bắt buộc duy nhất. | DEC-054, DEC-063, DEC-068 |
| ACC-009 | Email được bỏ khoảng trắng đầu/cuối, chuẩn hóa chữ thường để kiểm tra duy nhất; chưa cho đổi trong v1. Không tự bỏ dấu chấm hoặc phần `+tag` trong email. | DEC-063; không áp dụng quy tắc riêng của một nhà cung cấp email |
| ACC-010 | Mật khẩu 8–128 theo `.Length` của .NET, có ít nhất một chữ và một số theo `char.IsLetter`/`char.IsDigit`; không tự trim hoặc đổi nội dung mật khẩu. Sai 5 lần khóa đăng nhập 15 phút. | DEC-064; giữ chính sách hiện tại |
| ACC-011 | Access token 15 phút; phiên tối đa 30 ngày từ lúc đăng nhập, refresh không kéo dài thời hạn phiên. Có đăng xuất phiên hiện tại, thu hồi từng phiên và đăng xuất mọi thiết bị; đổi/đặt lại mật khẩu thu hồi mọi phiên. | DEC-065 |
| ACC-012 | Liên kết xác minh/reset dùng một lần, hạn 30 phút. Yêu cầu gửi lại cùng mục đích cách nhau ít nhất 60 giây; cấp liên kết mới vô hiệu liên kết cũ cùng mục đích. Không tự xác minh email khi reset mật khẩu. | DEC-065, DEC-051 |
| ACC-013 | Hồ sơ cho sửa `displayName`, `bio`, `locale`, `timezone`; bio tối đa 500 theo phép đếm hiện tại, locale 1–16 và timezone 1–64. v1 chưa thêm tải ảnh đại diện. | DEC-066; các giới hạn giữ theo API hiện tại |
| ACC-014 | Khôi phục chỉ qua email đã đăng ký, gồm tài khoản chưa xác minh theo DEC-051. Mất quyền truy cập email chưa có kênh khôi phục khác hoặc quy trình thủ công trong v1. | DEC-067; không thay đổi điều kiện xác minh trước khi vào ứng dụng |
| ACC-015 | v1 chưa có tự xóa account; khóa chặn ứng dụng/thu hồi phiên nhưng giữ lịch sử, mở khóa cần phiên đăng nhập mới. Khóa không thay lockout 15 phút. | DEC-103/104; công cụ/thẩm quyền khóa còn OQ-011 |
| ACC-016 | Đổi mật khẩu khi đã đăng nhập từ chối mật khẩu mới trùng mật khẩu hiện tại; đặt lại qua liên kết cho phép trùng nếu vẫn đúng policy. Reset thành công vẫn dùng token một lần, đổi security stamp và thu hồi mọi phiên dù mật khẩu trùng. | DEC-113; giữ hành vi hiện tại, không yêu cầu kiểm tra lịch sử mật khẩu |


[Vòng đời dữ liệu](../../../data-lifecycle.md#inventory) chốt TTL log/audit/chi tiết terminal và restore DEC-106–109; giữ active refresh family/cooldown/stamp, không dọn marker thu hồi theo TTL payload. Self-delete chưa thuộc v1; enum Deleted hiện tại không chứng minh có luồng xóa/anonymize.

Đợt tài khoản bao gồm đăng ký/xác minh, đăng nhập, khôi phục mật khẩu và hồ sơ. Code hiện còn có đổi mật khẩu và quản lý phiên/thiết bị. MFA, recovery code và external identity để Identity v2; chưa có API cho các phần này. Gửi lại xác minh và giao email thật là phần chưa hoàn thiện.

DEC-062 đồng bộ trường `displayName` bắt buộc theo form và API hiện tại; DEC-054 tiếp tục quản lý cách đăng nhập và quy tắc định danh. Username/email thuộc định danh, tên hiển thị thuộc hồ sơ có thể đổi.

<a id="permissions"></a>

## Quyền

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003, DEC-065/066 |
| ACL-02 | Khóa/mở khóa tài khoản qua quy trình kỹ thuật | Có quyết định và người thực hiện được phân quyền đúng tài khoản/phạm vi, có audit theo RB-ACCOUNT | Chưa được cấp quyền, sai phạm vi hoặc thiếu quyết định; quyền vận hành không cho đọc DM/sửa/xóa tin thay tác giả | ACC-015, DEC-104/112; người/vai trò/công cụ cụ thể còn OQ-011 |

Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không có phiên truy cập ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh email. Quyền tìm người/DM ở [đặc tả DM](../../direct-messaging/specs/requirements.md#permissions); quyền phòng ở [Community Permissions](../../community/specs/permissions.md#permissions).

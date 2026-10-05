# SCDC — Tài khoản

Cập nhật: 2026-10-05. Phạm vi: REQ-010, SCP-002. Quy tắc ACC, use case UC-ACC, tiêu chí AC-ACC, màn hình ACC-S, ca TC-ACC và ACL-01/02.

Nghiệp vụ cốt lõi và chính sách tài khoản đã xác nhận theo DEC-063–067; phép đếm UTF-16 theo DEC-068. Identity có implementation và test tự động; gửi lại xác minh, giao email và một số hành vi còn thiếu hoặc khác yêu cầu. Đối chiếu source ngày 2026-10-04 ở mục implementation-review. Các ca TC chưa có kết quả thực thi được ghi nhận trong hồ sơ này.

Ngày 2026-10-05 bổ sung use case và ma trận đối chiếu API, giao diện, AC/TC và assertion trong test hiện có. Đây là kết quả đọc source; chưa chạy kiểm thử sản phẩm hoặc xác nhận nghiệm thu. Các lựa chọn chưa chốt được ghi riêng tại [điểm cần xác nhận](#use-case-review).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền](#permissions)
- [Use case Identity](#use-cases)
- [Giao diện](#ux)
- [API hiện tại](#api-current)
- [Trạng thái, dữ liệu và đối chiếu implementation](#implementation-review)
- [Quyết định và thiết kế còn lại](#gaps)
- [Thiết kế chi tiết tài khoản](#detailed-design)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)

<a id="requirements"></a>

## 1. Phạm vi và quy tắc

### Mục tiêu và lựa chọn đã xác nhận

Người dùng tự tạo tài khoản và đăng nhập để nhắn tin, tham gia cộng đồng.
Ngày 2026-10-03, đại diện sản phẩm xác nhận: đăng ký bằng email, tên tài khoản, tên hiển thị và mật khẩu; đăng nhập bằng email **hoặc** tên tài khoản cùng mật
khẩu (DEC-054). Tên tài khoản duy nhất và chưa cho đổi ở đợt đầu; tên
hiển thị được đổi; email không công khai.

Tên hiển thị là trường hồ sơ dùng để tìm người theo DEC-019. Định danh
duy nhất của tài khoản phải tách khỏi tên hiển thị để chọn đúng người
khi nhiều người có cùng tên. MVP chưa cho đổi email theo DEC-063. Trước xác minh,
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
| ACC-009 | Email được bỏ khoảng trắng đầu/cuối, chuẩn hóa chữ thường để kiểm tra duy nhất; chưa cho đổi trong MVP. Không tự bỏ dấu chấm hoặc phần `+tag` trong email. | DEC-063; không áp dụng quy tắc riêng của một nhà cung cấp email |
| ACC-010 | Mật khẩu 8–128 theo `.Length` của .NET, có ít nhất một chữ và một số theo `char.IsLetter`/`char.IsDigit`; không tự trim hoặc đổi nội dung mật khẩu. Sai 5 lần khóa đăng nhập 15 phút. | DEC-064; giữ chính sách hiện tại |
| ACC-011 | Access token 15 phút; phiên tối đa 30 ngày từ lúc đăng nhập, refresh không kéo dài thời hạn phiên. Có đăng xuất phiên hiện tại, thu hồi từng phiên và đăng xuất mọi thiết bị; đổi/đặt lại mật khẩu thu hồi mọi phiên. | DEC-065 |
| ACC-012 | Liên kết xác minh/reset dùng một lần, hạn 30 phút. Yêu cầu gửi lại cùng mục đích cách nhau ít nhất 60 giây; cấp liên kết mới vô hiệu liên kết cũ cùng mục đích. Không tự xác minh email khi reset mật khẩu. | DEC-065, DEC-051 |
| ACC-013 | Hồ sơ cho sửa `displayName`, `bio`, `locale`, `timezone`; bio tối đa 500 theo phép đếm hiện tại, locale 1–16 và timezone 1–64. MVP chưa thêm tải ảnh đại diện. | DEC-066; các giới hạn giữ theo API hiện tại |
| ACC-014 | Khôi phục chỉ qua email đã đăng ký, gồm tài khoản chưa xác minh theo DEC-051. Mất quyền truy cập email chưa có kênh khôi phục khác hoặc quy trình thủ công trong MVP. | DEC-067; không thay đổi điều kiện xác minh trước khi vào ứng dụng |
| ACC-015 | MVP chưa có tự xóa account; khóa chặn ứng dụng/thu hồi phiên nhưng giữ lịch sử, mở khóa cần phiên đăng nhập mới. Khóa không thay lockout 15 phút. | DEC-103/104; công cụ/thẩm quyền khóa còn OQ-011 |
| ACC-016 | Đổi mật khẩu khi đã đăng nhập từ chối mật khẩu mới trùng mật khẩu hiện tại; đặt lại qua liên kết cho phép trùng nếu vẫn đúng policy. Reset thành công vẫn dùng token một lần, đổi security stamp và thu hồi mọi phiên dù mật khẩu trùng. | DEC-113; giữ hành vi hiện tại, không yêu cầu kiểm tra lịch sử mật khẩu |


[Vòng đời dữ liệu](../data-lifecycle.md#inventory) chốt TTL log/audit/chi tiết terminal và restore DEC-106–109; giữ active refresh family/cooldown/stamp, không dọn marker thu hồi theo TTL payload. Self-delete chưa thuộc MVP; enum Deleted hiện tại không chứng minh có luồng xóa/anonymize.

Đợt tài khoản bao gồm đăng ký/xác minh, đăng nhập, khôi phục mật khẩu và hồ sơ. Code hiện còn có đổi mật khẩu và quản lý phiên/thiết bị. MFA, recovery code và external identity để Identity v2; chưa có API cho các phần này. Gửi lại xác minh và giao email thật là phần chưa hoàn thiện.

DEC-062 đồng bộ trường `displayName` bắt buộc theo form và API hiện tại; DEC-054 tiếp tục quản lý cách đăng nhập và quy tắc định danh. Username/email thuộc định danh, tên hiển thị thuộc hồ sơ có thể đổi.

<a id="permissions"></a>

## 2. Quyền

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003, DEC-065/066 |
| ACL-02 | Khóa/mở khóa tài khoản qua quy trình kỹ thuật | Có quyết định và người thực hiện được phân quyền đúng tài khoản/phạm vi, có audit theo RB-ACCOUNT | Chưa được cấp quyền, sai phạm vi hoặc thiếu quyết định; quyền vận hành không cho đọc DM/sửa/xóa tin thay tác giả | ACC-015, DEC-104/112; người/vai trò/công cụ cụ thể còn OQ-011 |

Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không có phiên truy cập ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh email. Quyền tìm người/DM ở [đặc tả DM](direct-messaging.md#permissions); quyền phòng ở [Community](community.md#permissions).

<a id="use-cases"></a>

## 3. Use case Identity

Các UC mô tả hành vi mục tiêu của tài khoản trong MVP, áp dụng quy tắc ACC ở [phạm vi](#requirements). UC-ACC-01–11 phục vụ người dùng; trình duyệt thực hiện refresh thay người dùng và hệ thống email hỗ trợ giao liên kết. UC-ACC-12 phục vụ quy trình kỹ thuật khóa/mở khóa đã chốt, không có trang quản trị riêng. Chỉ người sở hữu phiên được xem/sửa hồ sơ riêng và quản lý phiên của tài khoản đó; quyền thao tác kỹ thuật được xét riêng theo ACL-02.

Luồng thành công dưới đây giả định dữ liệu hợp lệ và dịch vụ sẵn sàng. Ngoại lệ lỗi dịch vụ phải báo rõ, giữ dữ liệu không nhạy cảm đang nhập và không báo thành công khi chưa biết kết quả. Luồng verify/reset không tự thử lại thao tác dùng token khi mất response. Việc có endpoint hoặc assertion được ghi ở [ma trận đối chiếu](#use-case-coverage), tách khỏi kết quả chạy test.

| Use case | Mục tiêu | Màn hình/thành phần |
|---|---|---|
| [UC-ACC-01](#uc-acc-01) | Đăng ký tài khoản | ACC-S01, ACC-S03 |
| [UC-ACC-02](#uc-acc-02) | Xác minh email | ACC-S03 |
| [UC-ACC-03](#uc-acc-03) | Yêu cầu gửi lại xác minh | ACC-S03 |
| [UC-ACC-04](#uc-acc-04) | Đăng nhập | ACC-S02 |
| [UC-ACC-05](#uc-acc-05) | Yêu cầu khôi phục mật khẩu | ACC-S04 |
| [UC-ACC-06](#uc-acc-06) | Đặt lại mật khẩu qua liên kết | ACC-S05 |
| [UC-ACC-07](#uc-acc-07) | Xem và sửa hồ sơ riêng | ACC-S06 |
| [UC-ACC-08](#uc-acc-08) | Đổi mật khẩu khi đã đăng nhập | ACC-S07 |
| [UC-ACC-09](#uc-acc-09) | Làm mới phiên truy cập | Wrapper API của trình duyệt |
| [UC-ACC-10](#uc-acc-10) | Xem và thu hồi một phiên | ACC-S08 |
| [UC-ACC-11](#uc-acc-11) | Đăng xuất phiên hiện tại hoặc mọi thiết bị | ACC-S08, thao tác đăng xuất |
| [UC-ACC-12](#uc-acc-12) | Khóa/mở khóa tài khoản qua quy trình kỹ thuật | RB-ACCOUNT; chưa có API/CLI hoặc trang quản trị |

<a id="use-case-rules"></a>

### Truy vết quy tắc tài khoản

Bảng này xác định UC thực hiện từng quy tắc và ranh giới cần kiểm chứng cùng module khác. Giới hạn cụ thể giữ ở bảng ACC; UC không đặt lại một bộ policy khác.

| Quy tắc | Use case thực hiện | Ranh giới / điều kiện cần giữ |
|---|---|---|
| ACC-001 | UC-ACC-01 | Đủ bốn trường đăng ký; chưa cấp phiên trước xác minh |
| ACC-002 | UC-ACC-04 | Email hoặc username nhận diện cùng tài khoản |
| ACC-003 | UC-ACC-07 | Sửa hồ sơ của mình; username/email chỉ đọc; email riêng tư |
| ACC-004 | UC-ACC-01/07/10/12 | ID ổn định cho tài khoản/phiên; chọn người nhận theo ID/username thuộc tích hợp DM |
| ACC-005 | UC-ACC-02/04 | Xác minh trước truy cập ứng dụng; kiểm tra quyền gửi tin thuộc DM/Community |
| ACC-006 | UC-ACC-05/06 | Yêu cầu và thực hiện reset qua email |
| ACC-007 | UC-ACC-01/02/04/05/06 | Pending chỉ xác minh/khôi phục; reset không tự xác minh |
| ACC-008 | UC-ACC-01/07 | Username unique không phân biệt hoa thường; displayName trim/UTF-16 được trùng |
| ACC-009 | UC-ACC-01/04/05 | Email trim/chữ thường nhất quán; không đổi email, không bỏ dấu chấm hoặc `+tag` |
| ACC-010 | UC-ACC-01/04/06/08 | Policy mật khẩu không trim; lockout áp dụng đăng nhập, tách khóa quản trị |
| ACC-011 | UC-ACC-04/06/08/09/10/11 | Hạn token/phiên, rotation và đúng phạm vi thu hồi; realtime có kiểm chứng riêng |
| ACC-012 | UC-ACC-01/02/03/05/06 | Token đúng purpose, một lần, 30 phút; cấp lại/cooldown 60 giây và độc lập verify/reset |
| ACC-013 | UC-ACC-07 | Chỉ sửa displayName/bio/locale/timezone theo giới hạn; không tải avatar MVP |
| ACC-014 | UC-ACC-05/06 | Khôi phục cả pending qua email; không hứa kênh thủ công khi mất email |
| ACC-015 | UC-ACC-12 | Khóa chặn ứng dụng/thu hồi phiên, giữ lịch sử; mở khóa cần login mới; self-delete ngoài MVP |
| ACC-016 | UC-ACC-06/08 | Đổi từ chối trùng, reset cho phép trùng; reset vẫn thu hồi phiên theo DEC-113 |

<a id="uc-acc-01"></a>

### UC-ACC-01 — Đăng ký tài khoản

**Tác nhân:** Người chưa có tài khoản; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng mở ACC-S01; không cần đăng nhập. Tính hợp lệ và duy nhất của dữ liệu được kiểm tra trong luồng đăng ký.

**Luồng chính:**

1. Người dùng nhập email, username, tên hiển thị và mật khẩu, rồi gửi đăng ký.
2. Hệ thống chuẩn hóa và kiểm tra dữ liệu theo ACC-008–010, tạo tài khoản chờ xác minh cùng hồ sơ, mật khẩu băm và yêu cầu gửi liên kết xác minh.
3. Hệ thống trả kết quả tạo tài khoản; giao diện hướng dẫn kiểm tra email và cung cấp đường gửi lại xác minh/khôi phục mật khẩu.

**Ngoại lệ:** Dữ liệu sai trả lỗi trường; username/email trùng trả xung đột. Hai đăng ký cùng định danh chỉ tạo một tài khoản; yêu cầu thất bại không để lại tài khoản dở dang. Tiếp nhận yêu cầu email chưa chứng minh email đã giao; thất bại giao email được xử lý qua UC-ACC-03.

**Kết quả sau cùng:** Tài khoản ở `PendingVerification`, chưa có phiên ứng dụng. Liên kết xác minh dùng một lần, hạn 30 phút; lần cấp đầu tính vào cooldown gửi lại. Không tự xác minh tài khoản bằng token Development trên giao diện sản phẩm.

<a id="uc-acc-02"></a>

### UC-ACC-02 — Xác minh email

**Tác nhân:** Người kiểm soát hộp thư của tài khoản đăng ký.

**Điều kiện trước:** Người dùng có liên kết xác minh; thao tác không đòi hỏi đăng nhập ứng dụng.

**Luồng chính:**

1. Người dùng mở liên kết. Trang hướng dẫn xác minh; chỉ mở trang bằng GET chưa làm thay đổi tài khoản.
2. Người dùng bấm xác minh; hệ thống kiểm tra đúng mục đích, email đích, thời hạn, trạng thái token và tài khoản, rồi dùng token một lần.
3. Hệ thống xác minh email, chuyển tài khoản đang chờ xác minh sang active và hướng dẫn đăng nhập.

**Ngoại lệ:** Token sai mục đích, hết hạn, đã dùng hoặc bị thay thế trả lỗi liên kết không dùng được và đường yêu cầu lại. Hai request dùng cùng token chỉ một request thành công; consume và resend tuân thứ tự commit. Khi response mất sau commit, dùng lại token bị từ chối; người dùng có thể thử đăng nhập hoặc yêu cầu lại. Xác minh không tự mở khóa tài khoản bị khóa theo DEC-104.

**Kết quả sau cùng:** Email đã xác minh, token không dùng lại được; chưa cấp phiên đăng nhập. Token reset còn hiệu lực được giữ độc lập. Không hiện token kỹ thuật trên màn hình; cách đọc và xóa fragment trong URL thuộc [thiết kế liên kết](#detailed-design).

<a id="uc-acc-03"></a>

### UC-ACC-03 — Yêu cầu gửi lại xác minh

**Tác nhân:** Người chưa hoàn tất xác minh email; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng biết email đăng ký. Yêu cầu công khai không cần phiên ứng dụng và phải có email hợp lệ.

**Luồng chính:**

1. Người dùng nhập email và yêu cầu gửi lại từ ACC-S03.
2. Nếu tài khoản đủ điều kiện và đã cách lần cấp token verify trước ít nhất 60 giây, hệ thống vô hiệu token verify cũ, cấp token mới hạn 30 phút và tiếp nhận yêu cầu email trong cùng transaction.
3. Hệ thống trả `202 {accepted:true}`; giao diện hướng dẫn kiểm tra email, không xác nhận email đã giao hoặc tài khoản tồn tại.

**Ngoại lệ:** Email không tồn tại, đã xác minh, tài khoản không đủ điều kiện hoặc đang cooldown đều có cùng phản hồi công khai. Trong cooldown không cấp token, không vô hiệu liên kết đang dùng. Hai yêu cầu đồng thời chỉ cấp tối đa một token mới; không tiết lộ thời gian chờ riêng của email. Email cũ đến trễ vẫn chứa link đã bị vô hiệu. Dữ liệu email sai trả lỗi validation; limiter bổ sung vẫn hoãn theo DEC-089.

**Kết quả sau cùng:** Khi thực sự cấp mới, chỉ link verify mới có hiệu lực và token reset không bị thay đổi. Khi không đủ điều kiện cấp, dữ liệu token được giữ nguyên. Người dùng vẫn chưa có phiên ứng dụng.

<a id="uc-acc-04"></a>

### UC-ACC-04 — Đăng nhập

**Tác nhân:** Người có tài khoản, đang cần truy cập ứng dụng.

**Điều kiện trước:** Người dùng mở ACC-S02; để thành công, tài khoản active, email đã xác minh và không trong lockout.

**Luồng chính:**

1. Người dùng nhập email hoặc username cùng mật khẩu và gửi đăng nhập.
2. Hệ thống chuẩn hóa định danh, kiểm tra mật khẩu, lockout, trạng thái tài khoản và xác minh email.
3. Hệ thống tạo phiên tối đa 30 ngày, cấp access token 15 phút và refresh token; giao diện vào ứng dụng và kiểm tra quyền của đích truy cập.

**Ngoại lệ:** Định danh không tồn tại hoặc mật khẩu sai trả lỗi thông tin đăng nhập; lần sai thứ 5 khóa đăng nhập 15 phút, kể cả khi nhập đúng trong thời gian khóa. Đúng mật khẩu nhưng chưa xác minh trả `Identity.EmailNotVerified` và dẫn tới xác minh/khôi phục. Tài khoản bị khóa/không khả dụng không được cấp phiên ứng dụng. Login chạy cùng đổi/reset mật khẩu phải kiểm tra lại dưới khóa; phiên tạo trước commit đổi mật khẩu không còn hiệu lực sau commit đó.

**Kết quả sau cùng:** Email và username đăng nhập vào cùng một tài khoản. Login thành công xóa bộ đếm sai/lockout; login thất bại không cấp phiên. Khóa đăng nhập tạm thời không đổi account thành trạng thái bị khóa quản trị.

<a id="uc-acc-05"></a>

### UC-ACC-05 — Yêu cầu khôi phục mật khẩu

**Tác nhân:** Người quên mật khẩu; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng có email hợp lệ để gửi yêu cầu; để nhận và dùng liên kết, cần kiểm soát hộp thư đã đăng ký. Tài khoản chờ xác minh cũng được khôi phục theo DEC-051/067.

**Luồng chính:**

1. Người dùng nhập email tại ACC-S04 và gửi yêu cầu.
2. Với tài khoản đủ điều kiện và ngoài cooldown 60 giây, hệ thống vô hiệu token reset cũ cùng mục đích, cấp token reset mới hạn 30 phút và tiếp nhận yêu cầu email.
3. Hệ thống trả phản hồi accepted; giao diện hướng dẫn kiểm tra email, không tiết lộ trạng thái tài khoản hoặc hứa email đã giao.

**Ngoại lệ:** Unknown/unavailable/cooldown có cùng phản hồi công khai; trong cooldown không cấp thêm token hoặc làm mất link hiện tại. Hai request đồng thời chỉ cấp tối đa một token mới. Dữ liệu sai trả lỗi validation. Mất quyền truy cập email chưa có khôi phục thủ công hoặc kênh khác trong MVP.

**Kết quả sau cùng:** Khi cấp mới, token reset cũ không dùng được; token verify độc lập. Mật khẩu, trạng thái xác minh và phiên hiện tại chưa thay đổi chỉ vì gửi yêu cầu. Limiter bổ sung vẫn hoãn DEC-089.

<a id="uc-acc-06"></a>

### UC-ACC-06 — Đặt lại mật khẩu qua liên kết

**Tác nhân:** Người kiểm soát hộp thư khôi phục của tài khoản.

**Điều kiện trước:** Người dùng có liên kết reset; không cần đăng nhập ứng dụng. Token phải đúng mục đích, còn hạn, chưa dùng/bị thay thế và tài khoản đủ điều kiện.

**Luồng chính:**

1. Người dùng mở liên kết; GET không dùng token. Người dùng nhập và gửi mật khẩu mới.
2. Hệ thống kiểm tra policy mật khẩu trước khi consume; dưới khóa kiểm tra lại token, rồi cập nhật mật khẩu, security stamp, reset lockout và thu hồi mọi phiên.
3. Hệ thống đánh dấu token đã dùng và hoàn tất toàn bộ thay đổi; giao diện xóa phiên/cache liên quan và hướng dẫn đăng nhập lại.

**Ngoại lệ:** Mật khẩu trái policy trả lỗi và giữ token hợp lệ để sửa lại. Token sai/hết hạn/đã dùng/bị thay thế trả lỗi chung và đường yêu cầu mới. Hai consume chỉ một thành công. Đổi mật khẩu bằng UC-ACC-08 trước đó làm token reset cũ không còn dùng được. Mật khẩu mới trùng mật khẩu hiện tại được chấp nhận nếu hợp lệ theo ACC-016/DEC-113; token vẫn bị consume và mọi phiên vẫn bị thu hồi.

**Kết quả sau cùng:** Mật khẩu được lưu theo policy đã chốt, mọi phiên cũ bị thu hồi, không tự đăng nhập. Trạng thái xác minh email giữ nguyên; tài khoản pending vẫn phải thực hiện UC-ACC-02. Token verify còn hạn độc lập với reset.

<a id="uc-acc-07"></a>

### UC-ACC-07 — Xem và sửa hồ sơ riêng

**Tác nhân:** Người đã đăng nhập.

**Điều kiện trước:** Phiên của chính tài khoản còn hạn/chưa thu hồi; tài khoản active và đã xác minh.

**Luồng chính:**

1. Người dùng mở ACC-S06; hệ thống tải hồ sơ riêng, gồm username/email chỉ đọc.
2. Người dùng sửa `displayName`, `bio`, `locale`, `timezone` và gửi lưu.
3. Hệ thống kiểm tra dữ liệu theo ACC-008/013, cập nhật hồ sơ của chính người gọi và trả hồ sơ đã lưu; giao diện cập nhật nội dung hiển thị.

**Ngoại lệ:** Dữ liệu sai trả lỗi trường; lưu lỗi giữ bản đang sửa. Phiên không hợp lệ yêu cầu đăng nhập; không lấy user ID do người gọi cung cấp để sửa hồ sơ người khác. Các tên hiển thị trùng nhau được phép; việc chọn người trong DM dùng ID/username ổn định. Quy tắc chuẩn hóa và UTF-16 phải nhất quán HTTP–service–DB.

**Kết quả sau cùng:** Các trường hợp lệ được lưu; username/email giữ nguyên. Hồ sơ công khai chỉ có ID/username/tên hiển thị, không có email. MVP chưa có thao tác tải avatar; chưa áp dụng allowlist locale/timezone hoặc kiểm soát version mới khi chưa chốt contract.

<a id="uc-acc-08"></a>

### UC-ACC-08 — Đổi mật khẩu khi đã đăng nhập

**Tác nhân:** Người đã đăng nhập và biết mật khẩu hiện tại.

**Điều kiện trước:** Phiên của chính tài khoản hợp lệ; tài khoản active và đã xác minh.

**Luồng chính:**

1. Người dùng mở ACC-S07, nhập mật khẩu hiện tại và mật khẩu mới hợp lệ khác mật khẩu hiện tại.
2. Hệ thống kiểm tra mật khẩu hiện tại, cập nhật mật khẩu/security stamp, reset lockout, vô hiệu token reset đang có và thu hồi mọi phiên trong cùng transaction.
3. Giao diện xóa phiên/cache liên quan và về đăng nhập; người dùng đăng nhập lại bằng mật khẩu mới.

**Ngoại lệ:** Sai mật khẩu hiện tại hoặc mật khẩu mới trái policy trả lỗi; không đổi mật khẩu, vô hiệu token reset hay thu hồi phiên. Mật khẩu mới trùng hiện tại bị từ chối bằng `Identity.PasswordUnchanged` theo ACC-016/DEC-113 và không gây thay đổi dữ liệu. Login/refresh chạy đồng thời không được giữ phiên dùng mật khẩu/stamp cũ sau commit.

**Kết quả sau cùng:** Mật khẩu mới có hiệu lực; mọi thiết bị, gồm thiết bị thực hiện, phải đăng nhập lại. Token reset cấp trước đổi mật khẩu không còn dùng được; thao tác không đổi trạng thái xác minh email.

<a id="uc-acc-09"></a>

### UC-ACC-09 — Làm mới phiên truy cập

**Tác nhân:** Trình duyệt thay người đã đăng nhập.

**Điều kiện trước:** Trình duyệt có refresh token; để thành công, token chưa dùng/thu hồi/hết hạn, phiên còn hiệu lực và tài khoản active.

**Luồng chính:**

1. Khi access token cần làm mới, trình duyệt gửi refresh token hiện hành.
2. Hệ thống kiểm tra token/phiên dưới khóa, đánh dấu token cũ đã dùng và cấp access/refresh token mới cùng phiên.
3. Trình duyệt thay token phiên; các request đồng thời trong tab và các tab chia sẻ phiên phối hợp để chỉ thực hiện một rotation cần thiết.

**Ngoại lệ:** Refresh token sai hoặc phiên hết hạn/thu hồi không được cấp lại; client xóa phiên tương ứng và về đăng nhập. Dùng lại token đã rotation thu hồi chính phiên đó với lỗi reuse. Nếu response rotation mất, không tự gửi lại token cũ. Refresh đang chờ không được khôi phục phiên đã logout hoặc ghi đè login mới. Khi thiếu Web Locks, mỗi tab giữ phiên riêng theo implementation hiện tại.

**Kết quả sau cùng:** Hạn phiên giữ nguyên mốc tối đa 30 ngày từ login; refresh không gia hạn phiên. Access token mới có thời hạn theo ACC-011; khi đo biên hết hạn JWT phải ghi clock skew hiện tại 30 giây.

<a id="uc-acc-10"></a>

### UC-ACC-10 — Xem và thu hồi một phiên

**Tác nhân:** Người đã đăng nhập.

**Điều kiện trước:** Người gọi có phiên hợp lệ của chính tài khoản.

**Luồng chính:**

1. Người dùng mở ACC-S08; hệ thống trả các phiên của mình còn hạn/chưa thu hồi và đánh dấu phiên hiện tại.
2. Người dùng chọn một phiên cần thu hồi; hệ thống kiểm tra quyền sở hữu, thu hồi phiên đó và refresh token liên quan.
3. Giao diện cập nhật danh sách. Thu hồi phiên khác giữ phiên người gọi; thu hồi phiên hiện tại xóa phiên giao diện và dẫn về đăng nhập.

**Ngoại lệ:** ID phiên không tồn tại hoặc thuộc tài khoản khác trả `Identity.SessionNotFound`, không làm lộ phiên người khác. Thu hồi lặp phiên của mình đã bị thu hồi không tạo lại phiên. Tải danh sách lỗi phải hiển thị lỗi/thử lại; danh sách rỗng phải được phản ánh đúng, không thay bằng thiết bị mẫu. Phiên người gọi đã hết hạn/thu hồi phải đăng nhập lại.

**Kết quả sau cùng:** Chỉ phiên được chọn bị thu hồi; request HTTP tiếp theo của phiên đó bị từ chối. Kết nối chat đang mở phải ngừng nhận dữ liệu theo DEC-083; thực hiện và kiểm chứng realtime thuộc tích hợp DM, chưa có bằng chứng chỉ từ test Identity HTTP.

<a id="uc-acc-11"></a>

### UC-ACC-11 — Đăng xuất phiên hiện tại hoặc mọi thiết bị

**Tác nhân:** Người dùng muốn kết thúc phiên truy cập.

**Điều kiện trước:** Logout phiên hiện tại dùng refresh token đang giữ và không yêu cầu Bearer; logout-all cần một phiên xác thực hợp lệ của chính tài khoản.

**Luồng chính:**

1. Người dùng chọn đăng xuất phiên hiện tại hoặc tất cả thiết bị.
2. Hệ thống thu hồi phiên tương ứng: logout thu hồi phiên gắn với refresh token; logout-all thu hồi mọi phiên của tài khoản, gồm phiên gọi.
3. Giao diện xóa phiên và dữ liệu riêng/cache của phiên đó, đóng kết nối liên quan và về đăng nhập; các tab chia sẻ phiên nhận thay đổi.

**Ngoại lệ:** Logout với token không nhận diện được hoặc phiên đã thu hồi vẫn hoàn tất theo hành vi 204 hiện tại. Logout-all bằng phiên không hợp lệ bị từ chối. Nếu request logout lỗi/mất kết nối, client vẫn xóa phiên local nhưng không được tuyên bố đã thu hồi thành công trên server hoặc mọi thiết bị; cần phân biệt với kết quả thành công. Refresh đang chờ không được khôi phục phiên đã xóa.

**Kết quả sau cùng:** Khi server xác nhận thành công, đúng phạm vi phiên bị thu hồi; logout một phiên không ảnh hưởng phiên độc lập khác. Logout-all yêu cầu mọi thiết bị đăng nhập lại. Hành vi trên HTTP và việc dừng kết nối chat phải có bằng chứng riêng.

<a id="uc-acc-12"></a>

### UC-ACC-12 — Khóa/mở khóa tài khoản qua quy trình kỹ thuật

**Tác nhân:** Người quyết định và người thực hiện được phân quyền theo [RB-ACCOUNT](../operations-runbook.md#account-support). Người/vai trò cụ thể còn OQ-011; tài liệu này không chỉ định hoặc cấp quyền cho bất kỳ ai.

**Điều kiện trước:** Có yêu cầu/hồ sơ xử lý, user ID ổn định, môi trường/phạm vi đích và quyết định khóa hoặc mở khóa. Người thực hiện có quyền tương ứng; công cụ kỹ thuật phải được triển khai và kiểm chứng trước sử dụng.

**Luồng chính:**

1. Người thực hiện mở hồ sơ, đối chiếu quyết định, user ID, trạng thái/version hiện hành và phạm vi được cấp quyền; không thu thập mật khẩu, token hoặc nội dung DM vào hồ sơ.
2. Khi khóa, công cụ ghi trạng thái khóa và audit/receipt cần thiết, đổi security stamp và thu hồi toàn bộ phiên/token ứng dụng cùng transaction. Kiểm tra HTTP và cutoff chat/media theo [quy tắc khóa tài khoản](../data-lifecycle.md#account-state).
3. Khi mở khóa theo quyết định, công cụ ghi trạng thái phù hợp và audit. Người dùng phải đăng nhập mới, hoàn tất xác minh nếu còn thiếu và được kiểm tra quyền hiện hành; phiên cũ không được khôi phục.
4. Người thực hiện ghi kết quả đối soát vào hồ sơ: đúng tài khoản/phạm vi, hiệu lực thu hồi, dữ liệu lịch sử được giữ và quyền của các actor khác không bị cấp thêm.

**Ngoại lệ:** Thiếu quyền/quyết định, sai user ID hoặc trạng thái/version không còn phù hợp thì từ chối thao tác và đối chiếu lại; không thay đổi tài khoản. Lỗi transaction không để lại thay đổi từng phần. Mất response sau commit phải đối soát trạng thái/audit trước thử lại; chưa kiểm chứng cutoff thì chưa ghi đạt. Không dùng reset/verify để mở khóa; mất email không tạo kênh khôi phục thủ công.

**Kết quả sau cùng:** Tài khoản bị khóa không truy cập ứng dụng hoặc gửi/gọi; các phiên cũ bị từ chối và kết nối chat/media dừng trong ≤5 giây từ commit theo DEC-083/099. Người khác còn quyền vẫn đọc lịch sử và sửa/xóa tin của chính mình; không gửi DM/gọi mới tới peer bị khóa. Hồ sơ, tin, membership và ownership không bị xóa/chuyển vì thao tác khóa. Mở khóa không làm sống lại phiên hoặc tự xác minh email. Quyền kỹ thuật không cho đọc DM, sửa/xóa thay tác giả; MVP không có self-delete hoặc trang quản trị riêng.

<a id="use-case-coverage"></a>

### Đối chiếu use case với code và test

Rà soát ngày 2026-10-05. Prefix API là `/api/v1`. `Lifecycle` là test `Identity_v1_supports_the_complete_password_account_lifecycle` trong [IdentityV1FlowTests](../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs); các test có tên riêng khác nằm trong [IdentityConcurrencyTests](../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). `Client` là [api.test.js](../../clients/WebClient/tests/api.test.js), gồm assertion rotation giữa tab, storage event bị bỏ lỡ, 401 đến trễ, logout khi refresh đang chờ, login mới và trường hợp thiếu Web Locks. Các test này dùng browser/fetch mô phỏng, chưa chứng minh UI hoặc trình duyệt thật.

Source nghiệp vụ: [RegistrationService](../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [UserDirectory](../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs). Giao diện/wrapper: [AuthScreen](../../clients/WebClient/src/components/AuthScreen.jsx), [UserSettingsModal](../../clients/WebClient/src/components/UserSettingsModal.jsx), [api.js](../../clients/WebClient/src/api.js).

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-01 | AC-01/09/11/12/19; TC-01/04/09/10/16 | `RegisterAsync`; `POST /auth/register`; form đăng ký | Lifecycle: status 201, token Development, từ chối login trước verify | ACC-GAP-04/05/07; đăng ký trùng đồng thời/Unicode/policy mật khẩu chưa có test; UI đang tự verify bằng token Development |
| UC-ACC-02 | AC-05/07/15/19/20; TC-01/02/06/13/17/18 | `VerifyEmailAsync`; `POST /auth/verify-email`; wrapper verify | Lifecycle: verify 204, login sau verify, `emailVerified=true` | ACC-GAP-03/05; chưa có test verify đồng thời/hết hạn/dùng lại; chưa có trang mở liên kết và xác minh chủ động |
| UC-ACC-03 | AC-14/19/20/21; TC-07/12/16/17/18 | Resend chỉ có thiết kế/OpenAPI mục tiêu | Chưa có test cho resend | ACC-GAP-02/05; thiếu endpoint, cooldown, email và UI |
| UC-ACC-04 | AC-02/04/07/12; TC-01/02/08/10 | `LoginAsync`; `POST /auth/login`; form login | Lifecycle: login username trước/sau verify; `Parallel_wrong_passwords_are_counted_and_trigger_lockout`; hai test login/đổi mật khẩu tranh khóa | Login bằng email, biên hết lockout và UI dẫn tới xác minh chưa có test; form hiện chỉ báo lỗi chung |
| UC-ACC-05 | AC-10/14/18/19/21; TC-03/07/12/15/16/18 | `ForgotPasswordAsync`; `POST /auth/forgot-password`; form quên mật khẩu | Lifecycle: accepted và token reset cho account đã verify | ACC-GAP-01/02/05; chưa cấp token cho pending; chưa có cooldown/email thật hoặc test phản hồi các trạng thái |
| UC-ACC-06 | AC-06/10/12/15/16/20/22; TC-03/06/10/13/14/17/18/19 | `ResetPasswordAsync`; `POST /auth/reset-password`; wrapper reset | Lifecycle: reset 204 với mật khẩu khác, access cũ bị từ chối, mật khẩu cũ không đăng nhập được; `Concurrent_reset_requests_can_consume_a_token_only_once` | Chưa có trang reset; thiếu test pending, biên hạn token, reset sai policy giữ token, verify/reset độc lập và reset mật khẩu trùng |
| UC-ACC-07 | AC-03/08/11/17; TC-05/09/15 | `GetAsync`, `UpdateProfileAsync`; `GET/PATCH /users/me`; `UserDirectory`; form hồ sơ | Lifecycle: đọc username/emailVerified và PATCH trả 200 | Chưa assertion giá trị hồ sơ sau lưu/quyền riêng tư/biên UTF-16; UI chưa cho sửa locale hoặc xem email chỉ đọc; tìm người DM thuộc scope tích hợp |
| UC-ACC-08 | AC-12/16/17/22; TC-10/14/17/19 | `ChangePasswordAsync`; `POST /auth/change-password`; form đổi mật khẩu | Lifecycle: đổi 204, access cũ bị từ chối; `Changing_password_invalidates_previously_issued_reset_tokens`; hai test login/đổi mật khẩu tranh khóa | UI chưa xóa phiên ngay sau thành công; thiếu test sai mật khẩu hiện tại/policy/trùng |
| UC-ACC-09 | AC-13/16; TC-08/11/14 | `RefreshAsync`; `POST /auth/refresh`; wrapper refresh | Lifecycle: rotation/reuse và access bị từ chối; Client: phối hợp refresh/không khôi phục logout hoặc ghi đè login mới | Chưa có test biên hạn phiên/access, response rotation bị mất hoặc trình duyệt thật; thu hồi realtime chưa triển khai |
| UC-ACC-10 | AC-04/16; TC-08/14 | `GetSessionsAsync`, `RevokeSessionAsync`; `GET /auth/sessions`, `DELETE /auth/sessions/{id}`; tab phiên | Lifecycle: đánh dấu current, revoke phiên khác và access phiên đó bị từ chối | Chưa assertion phiên người gọi vẫn sống ngay sau revoke, revoke phiên hiện tại/quyền sở hữu; UI dùng phiên mẫu khi lỗi/rỗng và chưa có thao tác revoke phiên hiện tại |
| UC-ACC-11 | AC-04/16; TC-08/14 | `LogoutAsync`, `LogoutAllAsync`; `POST /auth/logout`, `POST /auth/logout-all`; wrapper xóa phiên local | Lifecycle: logout/logout-all và từ chối access; Client: logout giữa tab không bị refresh khôi phục | UI ghi “tất cả thiết bị khác” trong khi logout-all gồm phiên gọi; thiếu test logout token không nhận diện, lỗi mạng/phạm vi phiên và cleanup dữ liệu riêng/realtime |
| UC-ACC-12 | ACC-015, ACL-02; [AC-DATA-02/03, TC-DATA-01](../data-lifecycle.md#acceptance) | RB-ACCOUNT và enum/status trong Identity; chưa có API/CLI khóa/mở khóa | Chưa có test quy trình khóa/mở khóa; kiểm tra Bearer hiện có chỉ là một phần guard | Công cụ, phân quyền, audit/receipt và status policy cụ thể còn OQ-011; cần chứng minh thu hồi/cutoff, giữ lịch sử và unlock không khôi phục phiên |

Trong ma trận, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`; dùng dạng ngắn để dễ đọc. Assertion status 200/204 không đủ chứng minh mọi hậu điều kiện. Toàn bộ UC/AC/TC vẫn chưa có kết quả chạy trong bước tài liệu này; các điểm thiếu là đầu vào cho gói triển khai và kiểm thử sau khi người dùng xác nhận.

<a id="use-case-review"></a>

### Điểm cần xác nhận và phụ thuộc triển khai

| Nội dung | Căn cứ hiện có | Trạng thái / việc cần làm |
|---|---|---|
| Mật khẩu mới trùng mật khẩu hiện tại — UC-ACC-06/08 | Người dùng chọn giữ hành vi hiện tại ngày 2026-10-05: đổi từ chối trùng, reset cho phép trùng. | Đã chốt [DEC-113](../decisions.md#dec-113); ACC-016, AC-ACC-22 và TC-ACC-19 ghi rõ kết quả/thu hồi phiên. Chưa có assertion tự động cho trường hợp trùng. |
| Email dùng được — UC-ACC-01/03/05 | ACC-GAP-05 và thiết kế delivery/envelope đã có; chưa có worker/provider. | OQ-002/OQ-008: cần chọn provider, public origin/domain và nơi lưu key trước triển khai giao email thật; tiếp nhận outbox chưa chứng minh email đã giao. |
| Limiter bổ sung | Cooldown 60 giây và lockout 5 lần/15 phút đã chốt; ngưỡng theo nguồn/tài khoản chỉ là đề xuất DEC-089. | Giữ trạng thái hoãn; không tự thêm ngưỡng thành điều kiện nghiệm thu của UC. |
| Thu hồi trên kết nối đang mở — UC-ACC-08–11 | HTTP đã kiểm tra session/stamp; DEC-083 yêu cầu kết nối chat ngừng nhận dữ liệu trong ≤5 giây sau commit thu hồi. | Tích hợp và kiểm chứng cùng DM; test Identity HTTP và Client mô phỏng chưa đủ chứng minh. |
| Khóa/mở khóa quản trị | DEC-104/112 và RB-ACCOUNT đã chốt hành vi/phạm vi; chưa có công cụ thật. | Theo dõi ở OQ-011/runbook; không thêm UI quản trị hoặc tự coi verify/reset là thao tác mở khóa. |

Rà soát độ phủ use case ngày 2026-10-05: 12 UC có đủ tác nhân, điều kiện trước, luồng chính, ngoại lệ và kết quả sau cùng; 16 quy tắc ACC được truy vết ở [bảng quy tắc](#use-case-rules), 22 AC và 19 TC tài khoản được dẫn chiếu trong [ma trận](#use-case-coverage). Các lựa chọn còn mở ở bảng trên thuộc thiết kế/triển khai; hồ sơ ghi rõ phạm vi hành vi đã chốt và không coi chúng là chức năng đã chạy hoặc bằng chứng nghiệm thu sản phẩm.

<a id="ux"></a>

## 4. Giao diện và trạng thái

```text
ACC-S01 · Đăng ký
┌────────────────────────────────┐
│ Tạo tài khoản                  │
│ Email             [          ] │
│ Tên tài khoản     [          ] │
│ Tên hiển thị      [          ] │
│ Mật khẩu          [          ] │
│ [Tạo tài khoản]                 │
│ Đã có tài khoản? [Đăng nhập]    │
└────────────────────────────────┘

ACC-S02 · Đăng nhập
┌────────────────────────────────┐
│ Email hoặc tên tài khoản       │
│ [                            ] │
│ Mật khẩu          [          ] │
│ [Đăng nhập]                    │
│ [Quên mật khẩu] / [Đăng ký]    │
└────────────────────────────────┘
```

Tên trường đăng ký/đăng nhập đã xác nhận tại DEC-054, bổ sung trường tên hiển thị theo DEC-062. Không hiển thị
token kỹ thuật lên màn hình sản phẩm.

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S01 Đăng ký | Đang nhập, dữ liệu sai, đang gửi, thất bại | Giữ email/tên tài khoản/tên hiển thị đã nhập; lỗi tại trường phù hợp; tránh bấm gửi lặp trong khi chờ |
| ACC-S03 Xác minh email | Đã tiếp nhận yêu cầu, mở liên kết, đang xác minh, thành công, liên kết không dùng được | Hướng dẫn kiểm tra email; có đường gửi lại, tối thiểu 60 giây giữa hai yêu cầu cùng mục đích; liên kết mới vô hiệu liên kết cũ; không hứa email đã giao khi mới tiếp nhận |
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Không mất ngữ cảnh lý do cần đăng nhập; chỉ về hội thoại sau khi xác thực thành công và kiểm tra quyền |
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Phản hồi không tiết lộ email có tài khoản hay không; không hứa email đã được giao nếu mới tiếp nhận yêu cầu |
| ACC-S05 Đặt lại mật khẩu | Liên kết hợp lệ/không dùng được; mật khẩu mới; hoàn tất | Không hiển thị token; thành công dẫn đến đăng nhập, mọi phiên cũ bị thu hồi; nếu chưa xác minh thì vẫn phải xác minh |
| ACC-S06 Hồ sơ | Tải/lưu/lỗi; dữ liệu không hợp lệ | Giữ bản đang sửa khi lưu lỗi; tên tài khoản/email chỉ đọc; các trường sửa theo ACC-013 |
| ACC-S07 Đổi mật khẩu | Đang nhập/đang lưu/sai mật khẩu hiện tại/thành công | Khi thành công xóa phiên giao diện và về đăng nhập; mọi thiết bị phải đăng nhập lại |
| ACC-S08 Phiên/thiết bị | Tải danh sách, thu hồi một phiên, đăng xuất mọi thiết bị | Đánh dấu phiên hiện tại; thu hồi phiên khác không đăng xuất phiên hiện tại; thu hồi phiên hiện tại/đăng xuất tất cả về đăng nhập |

Theo DEC-051, ACC-S03 là điểm dừng của tài khoản chưa xác minh: chỉ
có xác minh và đường khôi phục mật khẩu, chưa vào ứng dụng. Đặt lại
mật khẩu không tự bỏ qua bước xác minh.

Hồ sơ hiện tại cho sửa `displayName`, `bio`, `locale`, `timezone`; username/email không có endpoint đổi. Cần thể hiện tải/lưu/lỗi và trạng thái phiên bị thu hồi sau đổi mật khẩu. Tài khoản hỗ trợ bố cục desktop/trình duyệt điện thoại theo DEC-059; ma trận trình duyệt đã chốt DEC-082; phiên bản cụ thể/thiết bị/kích thước và tiêu chí tiếp cận còn OQ-007. Thiết kế thị giác và prototype vẫn cần rà soát; form hiện tại nằm trong [AuthScreen](../../clients/WebClient/src/components/AuthScreen.jsx).

<a id="api-current"></a>

## 5. API hiện tại và dữ liệu

Prefix `/api/v1`. Bảng này mô tả code có trong repo; schema đầy đủ được sinh ở `/swagger/v1/swagger.json` khi chạy Development. Lỗi theo [ProblemDetails chung](../architecture.md#contracts).

| Method / đường dẫn | Xác thực và đầu vào JSON | Kết quả thành công |
|---|---|---|
| `POST /auth/register` | Public; `username, displayName, email, password` | 201 `RegistrationResponse`; chưa cấp phiên |
| `POST /auth/verify-email` | Public; `token` | 204 |
| `POST /auth/login` | Public; `login, password, deviceName?` | 200 `AuthResponse` |
| `POST /auth/refresh` | Public; `refreshToken` | 200 `AuthResponse` với token mới |
| `POST /auth/logout` | Public; `refreshToken` | 204 |
| `POST /auth/logout-all` | Bearer; không có body | 204 |
| `POST /auth/forgot-password` | Public; `email` | 202 `PasswordResetRequestedResponse` |
| `POST /auth/reset-password` | Public; `token, newPassword` | 204 |
| `POST /auth/change-password` | Bearer; `currentPassword, newPassword` | 204; thu hồi phiên, cần đăng nhập lại |
| `GET /auth/sessions` | Bearer | 200 danh sách `SessionResponse` của mình |
| `DELETE /auth/sessions/{sessionId}` | Bearer | 204; chỉ thu hồi phiên của mình |
| `GET /users/me` | Bearer | 200 `UserAccountResponse` riêng tư |
| `PATCH /users/me` | Bearer; `displayName, bio?, locale, timezone` | 200 hồ sơ sau cập nhật |

### Các response

- `RegistrationResponse`: `userId, username, email, verificationRequired, developmentVerificationToken`.
- `AuthResponse`: `accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user`.
- `PasswordResetRequestedResponse`: `accepted, developmentResetToken`.
- `UserAccountResponse`: `id, username, displayName, email, emailVerified, status, bio, avatarObjectKey, locale, timezone, createdAt, updatedAt, version`.
- `SessionResponse`: `id, deviceName, userAgent, lastSeenIp, createdAt, lastSeenAt, expiresAt, isCurrent`.

Email nằm trong hồ sơ riêng `users/me`, không phải hồ sơ công khai cho tìm kiếm. Token Development chỉ có giá trị khi `ExposeDevelopmentTokens` bật; không sử dụng chúng làm giao diện sản phẩm.

### Giá trị đang áp dụng trong implementation

| Nội dung | Hành vi hiện tại |
|---|---|
| Username | 3–32 ký tự ASCII chữ/số/`_`/`.`; chuẩn hóa chữ thường để kiểm tra trùng |
| Display name | Bắt buộc; service trim, độ dài 1–64 theo `.Length` của .NET (UTF-16), phù hợp DEC-068 |
| Email | Hợp lệ, tối đa 254; service trim và chuẩn hóa chữ thường |
| Password | 8–128; có ít nhất một chữ và một số |
| Hồ sơ | Bio tối đa 500, locale 1–16, timezone 1–64; cập nhật không đổi username/email |
| Access token / phiên | Mặc định 15 phút / 30 ngày; cấu hình được |
| Token verify/reset | Mặc định 30 phút, token dùng một lần; cấu hình được |
| Lockout | Mặc định sau 5 lần sai, khóa 15 phút; cấu hình được |
| Refresh | Rotation; reuse thu hồi phiên |
| Thu hồi | Request Bearer kiểm tra session, trạng thái tài khoản và security stamp; đổi/reset mật khẩu thu hồi phiên |

Thời hạn/mật khẩu đã được chọn tại DEC-064/065. Bảng vẫn mô tả implementation; rate limit bổ sung, email và các chênh lệch bên dưới chưa có bằng chứng nghiệm thu.

### Lỗi cần tích hợp

| Mã hiện tại | HTTP | Cách xử lý |
|---|---|---|
| `Identity.UsernameAlreadyExists`, `Identity.EmailAlreadyExists`, `Identity.RegistrationConflict` | 409 | Báo xung đột đăng ký |
| `Identity.InvalidCredentials` | 401 | Báo thông tin đăng nhập sai |
| `Identity.EmailNotVerified` | 403 | Dẫn tới luồng xác minh |
| `Identity.AccountUnavailable` | 403 | Không cho vào ứng dụng |
| `Identity.AccountLocked` | 429 | Báo khóa tạm |
| `Identity.InvalidOrExpiredToken` | 400 | Liên kết không dùng được |
| `Identity.InvalidRefreshToken`, `Identity.RefreshTokenReuseDetected` | 401 | Xóa phiên phía client, đăng nhập lại |
| `Identity.UserNotFound`, `Identity.SessionNotFound` | 404 | Báo không còn tài nguyên |
| `Identity.RegistrationInvalid`, `Identity.PasswordInvalid`, `Identity.ProfileInvalid` và lỗi trường | 400 | Hiển thị `errors` tại trường; validation MVC có thể trả `Common.ValidationFailed` trước service |
| `Identity.CurrentPasswordInvalid`, `Identity.PasswordUnchanged` | 400 | Báo tại trường mật khẩu hiện tại/mới; không xóa phiên khi đổi mật khẩu bị từ chối; trường hợp mật khẩu trùng theo DEC-113 |

Nguồn: [controllers](../../services/SCDC.Api/Controllers/Identity/AuthController.cs), [request schema](../../services/SCDC.Api/Controllers/Identity/IdentityRequests.cs), [response schema](../../services/Modules/Identity/Application/IdentityModels.cs), [validation](../../services/Modules/Identity/Application/IdentityValidation.cs), [options](../../services/Modules/Identity/Infrastructure/IdentityOptions.cs).

<a id="implementation-review"></a>

### Trạng thái tài khoản và phiên

Bảng tài khoản mô tả yêu cầu đã chốt. `Suspended`, `Disabled`, `Deleted` có trong enum/SQL; DEC-104/112 đã chốt hành vi khóa/mở khóa qua quy trình kỹ thuật có phân quyền/audit và chưa có UI quản trị riêng. [RB-ACCOUNT](../operations-runbook.md#account-support) là thiết kế mục tiêu; chưa có công cụ chuyển trạng thái được triển khai. Không tự suy Deleted thành tính năng xóa tài khoản MVP DEC-103.

| Trạng thái đầu | Thao tác/điều kiện | Kết quả cần có |
|---|---|---|
| Chưa có tài khoản | Đăng ký hợp lệ, email/username không trùng | Tạo tài khoản chờ xác minh và yêu cầu email; chưa cấp phiên ứng dụng |
| Chờ xác minh | Liên kết xác minh còn hạn, đúng mục đích, chưa dùng | Email được xác minh, tài khoản active; đăng nhập để nhận phiên |
| Chờ xác minh | Đăng nhập với đúng mật khẩu | 403 `Identity.EmailNotVerified`, không cấp phiên |
| Chờ xác minh | Khôi phục/đặt lại mật khẩu qua email | Đổi mật khẩu, vẫn chờ xác minh; source hiện chưa cấp liên kết cho trường hợp này |
| Active | Đăng nhập đúng, không bị khóa | Cấp một phiên cùng access/refresh token |
| Active | Đặt lại/đổi mật khẩu thành công | Mật khẩu mới có hiệu lực; mọi phiên cũ bị thu hồi; email giữ trạng thái xác minh trước đó |
| Active | Đủ 5 lần sai theo DEC-064 | Khóa đăng nhập 15 phút; account vẫn active, lockout nằm trong security state |

| Trạng thái phiên | Thao tác | Kết quả theo yêu cầu và source hiện tại |
|---|---|---|
| Còn hạn/chưa thu hồi | Refresh bằng token chưa dùng | Token cũ được đánh dấu đã dùng, cấp token mới; hạn phiên giữ nguyên |
| Còn hạn | Dùng lại refresh token đã rotation | Thu hồi chính phiên đó; 401 `Identity.RefreshTokenReuseDetected` |
| Còn hạn | Logout hoặc thu hồi phiên của mình | Phiên/token của phiên bị thu hồi; logout với token không nhận diện được vẫn trả 204 |
| Còn hạn | Logout-all, đổi/reset mật khẩu | Thu hồi mọi phiên của tài khoản; request xác thực tiếp theo bị từ chối |
| Đã hết hạn/thu hồi | Dùng access hoặc refresh token cũ | Không cấp lại phiên; cần đăng nhập mới |

HTTP Bearer hiện kiểm tra session, trạng thái active và security stamp trong DB trên mỗi request; JWT cho phép clock skew 30 giây theo source. Chưa có kết nối realtime để kiểm chứng ngưỡng thu hồi trên kết nối mở. Request đã được xác thực trước thay đổi phiên/quyền cần được xem xét riêng trong thiết kế đồng thời.

### Dữ liệu và quyền sở hữu

| Bảng | Trách nhiệm và ràng buộc |
|---|---|
| `identity.users` | ID ổn định, username và trạng thái; unique username chuẩn hóa |
| `identity.user_profiles` | Một hồ sơ mỗi user; tên hiển thị không phải khóa đăng nhập |
| `identity.user_emails` | Unique email chuẩn hóa; tối đa một email primary; `verified_at` tách khỏi mật khẩu |
| `identity.password_credentials` | Hash mật khẩu, phiên bản và thời điểm đổi; không lưu mật khẩu thô |
| `identity.user_security_states` | Bộ đếm sai, thời hạn khóa, security stamp |
| `identity.auth_sessions` / `refresh_tokens` | Phiên/thu hồi và chuỗi rotation; DB giữ hash token |
| `identity.account_tokens` | Token băm, mục đích, target email, hạn và thời điểm dùng; token verify/reset không thay thế nhau |
| `audit.security_events` / `integration.outbox_events` | Ghi cùng thay đổi nghiệp vụ; outbox chưa đồng nghĩa email được gửi |

Module khác lấy `UserSummary(id, username, displayName)` qua `IUserDirectory`; interface hiện hỗ trợ tìm ID, username chính xác và nhiều ID, chưa có tìm một phần tên. Hồ sơ công khai không trả email, mật khẩu, security stamp hoặc thông tin phiên.

Nguồn đối chiếu: [RegistrationService](../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [IdentityModule](../../services/Modules/Identity/IdentityModule.cs), [IdentityDbContext](../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs), [IUserDirectory](../../services/SCDC.Contracts/Identity/IUserDirectory.cs).

### Chênh lệch và bằng chứng còn thiếu

Kết quả đọc source ngày 2026-10-04, không phải kết quả chạy test. ACC-GAP theo dõi chênh lệch kỹ thuật, không tạo quyết định sản phẩm mới.

| Mã | Yêu cầu/căn cứ | Source hiện tại | Việc phải hoàn tất |
|---|---|---|---|
| ACC-GAP-01 | Khôi phục trước xác minh — ACC-007/014, AC-ACC-10 | `ForgotPasswordAsync` không tạo token nếu `VerifiedAt` null | Cho cấp token đúng yêu cầu; chứng minh reset không tự xác minh bằng TC-ACC-03 |
| ACC-GAP-02 | Gửi lại sau 60 giây, vô hiệu link cũ — ACC-012 | Chưa có resend verification hoặc cooldown reset; reset request mới đã vô hiệu token reset cũ | Thiết kế/cài đặt cooldown và resend; kiểm chứng request đồng thời |
| ACC-GAP-03 | Xác minh bằng token dùng một lần — ACC-012 | Verify đọc token/lưu nhưng không có khóa hoặc conditional consume như reset | Thử verify đồng thời; nếu nhiều request cùng thành công thì sửa consume; chưa có bằng chứng |
| ACC-GAP-04 | Chuẩn hóa định danh nhất quán — ACC-008/009 | Service trim nhưng MVC kiểm tra regex/độ dài trước service | Khóa thứ tự chuẩn hóa/validation HTTP–service–DB; fixture khoảng trắng/hoa thường |
| ACC-GAP-05 | Giao liên kết email dùng được — ACC-012 | Outbox chỉ chứa email/token ID; DB giữ hash, chưa có worker/provider | Thiết kế đưa link/token gửi được tới worker, trạng thái gửi/retry và kiểm thử email thật |
| ACC-GAP-06 | Chống lạm dụng đăng ký/login/reset/resend | Có lockout; chưa có rate limiter theo nguồn yêu cầu/gửi email | Chọn ngưỡng/cửa sổ cấu hình được; đo lỗi 429 và phản hồi không tiết lộ email |
| ACC-GAP-07 | Tên hiển thị UTF-16 — ACC-008, DEC-068 | Service dùng `.Length`; MVC và DB có giới hạn riêng | Fixture tiếng Việt tổ hợp/emoji và biên 64; kiểm chứng HTTP–DB |

Rà soát ngày 2026-10-05 bổ sung chênh lệch giao diện tại [ma trận use case](#use-case-coverage): tự verify bằng token Development, thiếu trang verify/reset, chưa cho sửa locale, chưa xóa phiên ngay sau đổi mật khẩu, danh sách phiên dùng dữ liệu mẫu khi lỗi/rỗng và nhãn logout-all sai phạm vi. Các chênh lệch này cần triển khai/kiểm chứng cùng UC tương ứng; chưa sửa mã trong bước tài liệu.

<a id="gaps"></a>

## 6. Quyết định đã chốt và thiết kế còn lại

Đăng ký → tiếp nhận yêu cầu email → xác minh liên kết → đăng nhập → ứng dụng. Quên mật khẩu → tiếp nhận yêu cầu → mở liên kết → đặt mật khẩu mới → đăng nhập; chưa xác minh thì vẫn quay về xác minh. Liên kết hết hạn/đã dùng có đường yêu cầu lại, tuân cooldown.

Giữ các mã ACC-P để truy vết; phần đã xác nhận dẫn tới DEC, phần thiết kế chưa được rà soát tiếp tục có nhãn đề xuất.

| Mã phương án cũ | Kết luận | Trạng thái |
|---|---|---|
| ACC-P01 | Định danh theo ACC-008/009; tên hiển thị đếm UTF-16 | Đã chốt DEC-063/068; còn kiểm chứng chuẩn hóa |
| ACC-P02 | Verify/reset một lần, 30 phút; cấp lại vô hiệu link cũ cùng mục đích | Đã chốt DEC-065; đã có thiết kế consume/resend, còn triển khai và kiểm chứng |
| ACC-P03 | Cooldown 60 giây; reset/resend không tiết lộ email tồn tại; rate limit bổ sung theo nguồn/tài khoản | Cooldown chốt DEC-065; ngưỡng bổ sung được hoãn DEC-089 |
| ACC-P04 | Phiên tối đa 30 ngày; đổi/reset thu hồi mọi phiên | Đã chốt DEC-065; transport hiện tại Bearer/refresh JSON, thiết kế thu hồi realtime tại DM còn cần triển khai |
| ACC-P05 | Hồ sơ theo ACC-013; không đổi username/email; công khai chỉ ID/username/displayName; khôi phục chỉ qua email | Đã chốt DEC-063/066/067 |

### Hợp đồng gửi lại cần triển khai

`POST /api/v1/auth/resend-verification` là endpoint đề xuất, chưa có controller/service. Đầu vào `{email}` hợp lệ; thành công `202 {accepted: true}` cùng ý nghĩa cho email tồn tại/không tồn tại/đã xác minh/không đủ điều kiện. Với tài khoản chờ xác minh đủ điều kiện, trong một transaction khóa tài khoản, kiểm tra cooldown, vô hiệu token verify cũ rồi tạo token/outbox mới. Hai request đồng thời chỉ tạo tối đa một token mới trong cooldown; không vô hiệu token reset khi gửi lại verify.

Cooldown theo email yêu cầu cần có cùng phản hồi công khai cho các trạng thái tài khoản để tránh tiết lộ tồn tại; limiter theo nguồn có thể trả 429 ProblemDetails và `Retry-After` mà không phụ thuộc email tồn tại. Schema/mã lỗi và ngưỡng bổ sung là thiết kế cần rà soát. Không trả token thô ngoài Development.

### Email và thiết kế còn lại

Repo chưa có worker/provider; payload token ID không đủ dựng lại token từ hash. Thiết kế dưới đây là phương án kỹ thuật để rà soát, chưa có implementation:

1. Identity sinh token ngẫu nhiên, ghi hash/mục đích/hạn 30 phút trong `account_tokens`; cùng transaction tạo `EmailDelivery` và outbox tham chiếu delivery ID. Delivery giữ recipient, template version, token ID và envelope liên kết được mã hóa, không ghi token thô vào payload outbox/audit/log. Domain liên kết lấy từ cấu hình tin cậy, không từ header/URL do người gọi gửi.
2. Worker lấy delivery bằng lease có hạn để nhiều worker không đồng thời xử lý cùng lần gửi; trước mỗi lần gửi kiểm tra token còn hiệu lực/chưa dùng/chưa bị thay thế và tài khoản đúng trạng thái. Giải mã ngay trước gọi provider. Khóa mã hóa được quản lý ngoài DB, có key version và quy trình phục hồi; chọn công cụ/key store cùng topology.
3. Trạng thái `Pending → Sending → ProviderAccepted`, hoặc `RetryPending / Failed / Suppressed`. `ProviderAccepted` chỉ nghĩa provider đã nhận yêu cầu; delivered/bounce cập nhật từ callback được xác thực nếu provider hỗ trợ, không tự coi đã vào hộp thư. Callback lặp cập nhật có điều kiện và không chứa link/token trong log.
4. Retry kỹ thuật đề xuất tối đa 5 lần với khoảng chờ 10/30/90/300 giây và jitter; dừng trước hạn token, khi token bị thay thế/đã dùng hoặc lỗi recipient không thể retry. Provider timeout sau khi có thể đã nhận cho phép email lặp; dùng delivery ID làm idempotency key nếu provider hỗ trợ. Mỗi link vẫn chỉ dùng một lần.
5. Xóa envelope ngay sau provider accepted hoặc suppressed/failed cuối; job dọn tối đa 1 phút sau hạn token cho delivery bị bỏ dở. Chỉ giữ metadata cần đối soát theo chính sách vận hành. Không giữ transaction/khóa tài khoản trong lúc gọi dịch vụ email bên ngoài.

Token mới có thể được cấp trong lúc provider đang giao email cũ; không bảo đảm thu hồi email đã gửi. Liên kết cũ phải bị từ chối phía server và UI chỉ dẫn yêu cầu lại. Kiểm thử rollback không gửi mail, worker crash trước/sau provider accept, email giao lặp/đảo thứ tự, verify/reset song song và cleanup envelope. Provider/domain, cơ chế khóa, schema/migration và kết quả thử email thật vẫn là đầu việc trước phát hành.

### Chuẩn hóa và giới hạn truy cập đề xuất

Định danh phải đi qua cùng pipeline trước validation HTTP/service/DB: email bỏ khoảng trắng đầu/cuối rồi chuyển chữ thường theo DEC-063; username trim rồi kiểm tra ASCII/quy tắc duy nhất; tên hiển thị trim, từ chối toàn khoảng trắng, đếm UTF-16. Không trim/chuẩn hóa mật khẩu hoặc nội dung tin ngoài bước CRLF→LF đã đặc tả ở DM. Locale/timezone giữ validation độ dài hiện tại; việc giới hạn danh sách locale/zone hỗ trợ cần lựa chọn riêng, không tự đổi policy trong bước thiết kế.

Theo DEC-089, người dùng chọn giữ limiter bổ sung là đề xuất để quyết định sau: đăng ký 5 lần/giờ/IP; login 30 lần/5 phút/IP bên cạnh lockout 5 lần/tài khoản; forgot/resend gộp 10 lần/giờ/IP và tối đa 5 email/giờ/tài khoản cho mỗi mục đích, vẫn tuân cooldown 60 giây. Key tài khoản limiter dùng hash/HMAC của định danh chuẩn hóa; phản hồi accepted/cooldown không phân biệt tài khoản tồn tại. Limiter theo IP trả 429 ProblemDetails có `Retry-After`; kiểm thử mạng dùng chung và IPv6 trước khóa ngưỡng. Không coi limiter là biện pháp thay thế transaction consume token hoặc kiểm tra phiên.

Thiết kế còn mở: ACC-GAP-01–07; ngưỡng limiter/email pipeline cần rà soát và kiểm chứng; thu hồi realtime và cấu hình thiết bị ở OQ-007/008. OQ-002 giữ mở cho các đầu ra này. Các chính sách đã chốt không phải bằng chứng implementation hoặc nghiệm thu đã đạt.

<a id="detailed-design"></a>

### Thiết kế chi tiết tài khoản

Phương án ngày 2026-10-04 để triển khai DEC-063–067; thuật toán/schema dưới đây được soạn trong phạm vi tài liệu, chưa thay source. [OpenAPI luồng xác minh/khôi phục](../contracts/account-recovery.openapi.json) có 4 thao tác, ghi rõ resend chưa có endpoint và các endpoint hiện có còn chênh lệch. Swagger sinh từ source vẫn là nguồn cho API hiện đang chạy.

#### HTTP xác minh, gửi lại và khôi phục

| Thao tác | Request | Response mục tiêu | Ngoại lệ/hiệu lực |
|---|---|---|---|
| `POST /auth/resend-verification` | `{email}` | 202 `{accepted:true}` | Cùng body cho unknown/verified/unavailable/cooldown; không trả `userId`, thời điểm cooldown riêng hoặc token |
| `POST /auth/forgot-password` | `{email}` | 202 `{accepted:true}` | Cho cả pending verification; token reset không xác minh email; Development có trường token riêng như source |
| `POST /auth/verify-email` | `{token}` | 204 | 400 `Identity.InvalidOrExpiredToken` nếu sai purpose/hết hạn/đã dùng/bị thay thế; không tự login |
| `POST /auth/reset-password` | `{token,newPassword}` | 204 | Kiểm tra mật khẩu trước consume; reset thành công đổi stamp, reset lockout và thu hồi mọi phiên |

Validation 400 theo ProblemDetails chung; lỗi limiter theo nguồn 429 `Common.RateLimitExceeded` và `Retry-After` là mã đề xuất. Ngưỡng limiter bổ sung còn DEC-089. HTTP 202 chỉ xác nhận yêu cầu được tiếp nhận, không chứng minh tài khoản tồn tại hoặc email đã giao. Dùng `Cache-Control: no-store` cho response có thông tin tài khoản/token; không ghi request body của các route auth vào telemetry.

#### Cấp và dùng token đồng thời

Token hiện tại sinh từ 48 byte ngẫu nhiên, chuyển base64url và DB giữ SHA-256 hash; giữ cơ chế này. Đề xuất thêm `identity.account_token_policies` khóa `(user_id,purpose)` với `last_issued_at,active_token_id`; lần đăng ký đầu cũng ghi policy để resend không bỏ cooldown 60 giây. Clock server UTC và điều kiện `expires_at > now`; tại đúng mốc hết hạn từ chối.

1. Cấp lại: chuẩn hóa email, tra user qua Identity, mở transaction và `LockUserAsync` trước đọc trạng thái/token/policy. Kiểm tra purpose, điều kiện tài khoản, cooldown và limiter gửi email nếu sau này được chọn; nếu không cấp thì trả accepted giống nhau. Đọc DB dưới khóa, không dựa vào kiểm tra trước transaction.
2. Khi đủ điều kiện, đánh dấu token cũ cùng purpose không còn hợp lệ, tạo token mới + policy + EmailDelivery + outbox + audit trong cùng transaction. Việc cấp lại verify không thu hồi reset và ngược lại; audit chỉ ghi ID/purpose/lý do, không plaintext/hash token.
3. Consume: tìm user từ hash/purpose chỉ để định tuyến khóa; sau đó mở transaction, khóa user, đọc lại token/trạng thái/target email và thời hạn. Token phải vẫn là active của policy. Verify cập nhật email/account; reset cập nhật hash/stamp và thu hồi phiên. Đánh dấu token đã dùng và kết thúc các token cũ cùng purpose; commit toàn bộ hoặc không thay đổi gì.
4. Hai consume chỉ một thành công; consume và resend tranh cùng khóa, thứ tự commit quyết định link có hiệu lực. Resend sau verify không tạo token verify mới. Reset không vô hiệu verify còn hạn. Reset sai policy không làm mất token hợp lệ.
5. Change-password, login, refresh và revoke giữ thứ tự khóa user trước các bản ghi credential/session/token như thiết kế Identity hiện tại. Khi transaction lỗi, không có mail từ outbox chưa commit; response mất sau consume không cho dùng lại token. UI có thể thử login với trạng thái hiện hành, không hứa consume lặp trả thành công.

`ConsumedAt` hiện được dùng cả cho consume và vô hiệu token cũ; lý do phân biệt trong audit, không suy mọi `ConsumedAt` là người dùng đã bấm link. Migration policy cần xác lập active token theo dữ liệu đang có dưới khóa, vô hiệu các bản dư và lấy `last_issued_at` từ token gần nhất; không tự coi policy mới rỗng là được gửi ngay.

#### Mô hình EmailDelivery và worker

| Trường đề xuất | Quy tắc |
|---|---|
| `id,user_id,account_token_id,purpose,recipient,template_version` | Identity sở hữu; duy nhất delivery theo token/template; không dùng email làm aggregate ID public |
| `protected_envelope,envelope_expires_at` | Link/token được mã hóa và xác thực; hạn không vượt hạn token 30 phút; nullable sau purge |
| `status,attempt_count,next_attempt_at` | Pending/Sending/RetryPending/ProviderAccepted/Failed/Suppressed; số lần gửi thực tế, không tăng chỉ vì poll |
| `lease_owner,lease_until,provider_message_id,last_error_code` | Lease kỹ thuật đề xuất 30 giây, provider timeout 10 giây; lỗi chỉ mã an toàn, không lưu response chứa bí mật |
| `created_at,accepted_at,delivered_at,bounced_at` | ProviderAccepted/delivered khác nhau; callback lặp cập nhật có điều kiện |

Worker claim bằng transaction ngắn, ghi lease rồi commit trước gọi provider; hết lease có thể xử lý lại, nên không cam kết email giao đúng một lần. Payload outbox chỉ delivery ID/user ID/purpose, không có envelope/token; recipient chỉ có trong bản ghi Identity cần thiết và request provider. Việc tiếp nhận sau timeout dùng delivery ID đối soát nếu provider hỗ trợ.

Thiết kế envelope chọn ASP.NET Core Data Protection với purpose `Identity.EmailDelivery.v1` và hạn token; cursor dùng purpose khác. Key ring riêng theo môi trường, được bảo vệ khi lưu, lưu bền qua restart và có bản phục hồi cùng cấu hình. Xóa key làm dữ liệu đã bảo vệ bằng key không giải mã được theo [tài liệu Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0); phải đối chiếu tuổi envelope/cursor và backup 30 ngày trước dọn key. Cách lưu key/certificate/secret store cụ thể cần topology, không lưu khóa trong SQL payload hoặc repo.

Worker kiểm tra token trước gửi; cleanup và giới hạn retry theo phương án ở trên. Bảo vệ envelope không thay việc server từ chối link bị thay thế/hết hạn; khi restore không gửi lại delivery quá hạn hoặc đã bị thu hồi sau recovery point.

#### Liên kết email và trạng thái trình duyệt

Thiết kế route SPA `/auth/verify#token=…` và `/auth/reset#token=…` từ public origin cấu hình. Fragment không đi trong HTTP request URI theo [tài liệu URI fragment](https://developer.mozilla.org/en-US/docs/Web/URI/Reference/Fragment); app lấy một lần vào bộ nhớ rồi `history.replaceState` bỏ khỏi URL. Trang này không có analytics/script bên thứ ba, dùng `Referrer-Policy: no-referrer`, không ghi toàn bộ URL vào lỗi; fragment không thay bảo vệ khỏi script trên trang.

Trang verify hiển thị nút xác minh để chỉ POST khi người dùng thực hiện thao tác; GET mở link không consume. Reset chỉ POST khi người dùng điền mật khẩu mới hợp lệ. Link sai/hết hạn/đã dùng đều báo “Liên kết không còn sử dụng được” và đường yêu cầu mới, không hiện token. Thành công verify/reset dẫn đến login; reset của pending account vẫn cần verify. Đây là chi tiết UX kỹ thuật cần rà soát cùng prototype.

Đổi/reset mật khẩu, logout hoặc đổi tài khoản xóa state UI, nội dung DM đang cache và đóng Hub của phiên đó. Source [api.js](../../clients/WebClient/src/api.js) hiện chia sẻ token phiên qua localStorage khi có Web Locks; DEC-091 chỉ nói nội dung DM/bản nháp, không tuyên bố đã đổi cơ chế lưu token. Refresh token đã rotation mà response mất không tự retry token cũ vì có thể kích hoạt reuse; client về login theo lỗi hiện tại.

#### Hồ sơ và chuẩn hóa khi bàn giao

Chốt pipeline normalize → validate → transaction → constraint; validation HTTP không chạy bộ quy tắc khác service. Username/email unique ở DB xử lý cả đăng ký đồng thời. DisplayName trim/UTF-16 như ACC-008; bio hiện trim và rỗng thành null; locale/timezone giữ validation độ dài hiện tại, chưa tự thêm allowlist locale/zone chỉ vì có giá trị trông hợp lệ. Source đang dùng version hồ sơ ở response; cập nhật optimistic concurrency là hướng thiết kế, chưa thêm `expectedVersion` vào API hiện có khi chưa thay contract.

Đầu ra triển khai tài khoản: thêm resend/policy/delivery migration + worker; sửa reset pending/verify consume; cập nhật shared validator và auth UI; mở rộng hợp đồng thu hồi session cho chat. Kết quả của các gói này phải ghi vào ACC-GAP và TC, không đánh dấu đã hoàn thành chỉ vì có schema trong docs.

<a id="acceptance"></a>

## 7. Tiêu chí chấp nhận

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
| AC-ACC-17 | Sửa các trường hồ sơ đã chốt; thử sửa username/email hoặc tải avatar. | Tên hiển thị/bio/locale/timezone cập nhật hợp lệ; MVP không cung cấp thao tác đổi định danh hoặc tải avatar; không tuyên bố tính năng chỉ từ cột SQL. |
| AC-ACC-18 | Tài khoản mất quyền truy cập email tìm cách khôi phục. | Chỉ có luồng qua email đã đăng ký; thông tin trợ giúp không hứa có khôi phục thủ công hoặc qua kênh khác trong MVP. |
| AC-ACC-19 | Đăng ký/quên mật khẩu ngoài Development và nhận email thật. | Response không có token sử dụng được; nhận link đúng domain/mục đích/hạn, hoàn tất luồng; tiếp nhận outbox không được ghi thành email đã giao. |
| AC-ACC-20 | Mở link qua GET, gửi lại/consume/reset đồng thời và reset mật khẩu sai policy | GET không consume; một token dùng một lần; token verify/reset độc lập; reset sai không mất token |
| AC-ACC-21 | Email unknown/verified/unavailable/cooldown gọi resend/forgot | Production cùng 202/body accepted; không trả trạng thái tài khoản/token hoặc retry time riêng |
| AC-ACC-22 | Đổi và đặt lại mật khẩu với mật khẩu mới trùng mật khẩu hiện tại. | Đổi bị từ chối bằng `Identity.PasswordUnchanged`, không đổi dữ liệu hoặc thu hồi phiên/token reset; reset hợp lệ thành công, consume token, đổi stamp và thu hồi mọi phiên. Cả hai tuân policy mật khẩu; trạng thái xác minh không đổi (DEC-113). |

AC-ACC-06 áp dụng cho tài khoản đã xác minh; AC-ACC-10 bao phủ tài khoản
chưa xác minh. Toàn bộ tiêu chí vẫn cần có kết quả chạy và xác nhận
nghiệm thu, không được đánh dấu đạt chỉ vì quy tắc đã chốt.

Đăng ký hợp lệ ở AC-ACC-01 bao gồm tên hiển thị bắt buộc theo DEC-062. Chưa đánh dấu tiêu chí đạt chỉ vì có code hoặc ca test.

<a id="tests"></a>

## 8. Ca kiểm thử

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

### Đối chiếu bộ test tự động hiện có

Đọc source ngày 2026-10-04; chưa chạy test trong lần hoàn thiện tài liệu này. Một test có assertion chỉ là bằng chứng có ca tự động, không phải kết quả đạt.

| Test hiện có | Hành vi có assertion | Coverage còn thiếu |
|---|---|---|
| `Identity_v1_supports_the_complete_password_account_lifecycle` | Đăng ký, từ chối login trước verify, verify, login bằng username, hồ sơ, rotation/reuse, reset/đổi mật khẩu, phiên/revoke/logout-all/logout | Login bằng email; reset trước verify; boundary/cooldown/resend; link hết hạn/verify đồng thời; email thật |
| `Changing_password_invalidates_previously_issued_reset_tokens` | Đổi mật khẩu vô hiệu token reset cũ | Token verify phải độc lập; kiểm thử email đến trễ |
| `A_concurrent_login_cannot_survive_a_password_change` / `A_login_waiting_for_a_password_change_rechecks_the_password` | Hai thứ tự chạy login và đổi/reset; phiên/mật khẩu cũ không còn dùng được | Hành trình nhiều tab và kết nối realtime |
| `Parallel_wrong_passwords_are_counted_and_trigger_lockout` | Đếm đúng request sai đồng thời, khóa đăng nhập | Biên hết khóa và giới hạn theo nguồn |
| `Concurrent_reset_requests_can_consume_a_token_only_once` | Chỉ một reset dùng cùng token thành công | Verify đồng thời và resend |

Nguồn: [IdentityV1FlowTests](../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [IdentityConcurrencyTests](../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). Các TC vẫn là Chưa chạy; ca phụ thuộc implementation thiếu ghi rõ ACC-GAP tương ứng. Dữ liệu mật khẩu/token phải dùng môi trường thử riêng.

Ngoài các ca trên, kiểm tra trường `displayName` thiếu/rỗng/vượt 64, password sai policy, rotation/reuse refresh, logout-all, revoke session và đổi mật khẩu. Bộ test tự động trong repo: [IdentityV1FlowTests](../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [IdentityConcurrencyTests](../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). Cần đối chiếu coverage thực tế với AC/TC, không suy từ tên test.

Chạy hành trình trên desktop và trình duyệt điện thoại theo DEC-059; ma trận theo DEC-082; ghi phiên bản/thiết bị/build thực tế khi chạy. Mỗi kết quả ghi theo [mẫu nghiệm thu](../release-operations.md#testing).

# SCDC — Use case tài khoản

Hành vi mục tiêu và truy vết ACC. Đối chiếu source/test tại [status](../status.md#use-case-coverage).

<a id="use-cases"></a>

## Use case Identity

Các UC mô tả hành vi mục tiêu của tài khoản trong v1, áp dụng quy tắc ACC ở [phạm vi](requirements.md#requirements). UC-ACC-01–11 phục vụ người dùng; trình duyệt thực hiện refresh thay người dùng và hệ thống email hỗ trợ giao liên kết. UC-ACC-12 phục vụ quy trình kỹ thuật khóa/mở khóa đã chốt, không có trang quản trị riêng. Chỉ người sở hữu phiên được xem/sửa hồ sơ riêng và quản lý phiên của tài khoản đó; quyền thao tác kỹ thuật được xét riêng theo ACL-02.

Luồng thành công dưới đây giả định dữ liệu hợp lệ và dịch vụ sẵn sàng. Ngoại lệ lỗi dịch vụ phải báo rõ, giữ dữ liệu không nhạy cảm đang nhập và không báo thành công khi chưa biết kết quả. Luồng verify/reset không tự thử lại thao tác dùng token khi mất response. Việc có endpoint hoặc assertion được ghi ở [ma trận đối chiếu](../status.md#use-case-coverage), tách khỏi kết quả chạy test.

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
| ACC-013 | UC-ACC-07 | Chỉ sửa displayName/bio/locale/timezone theo giới hạn; không tải avatar v1 |
| ACC-014 | UC-ACC-05/06 | Khôi phục cả pending qua email; không hứa kênh thủ công khi mất email |
| ACC-015 | UC-ACC-12 | Khóa chặn ứng dụng/thu hồi phiên, giữ lịch sử; mở khóa cần login mới; self-delete ngoài v1 |
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

**Kết quả sau cùng:** Email đã xác minh, token không dùng lại được; chưa cấp phiên đăng nhập. Token reset còn hiệu lực được giữ độc lập. Không hiện token kỹ thuật trên màn hình; cách đọc và xóa fragment trong URL thuộc [thiết kế liên kết](../design/README.md#detailed-design).

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

**Ngoại lệ:** Unknown/unavailable/cooldown có cùng phản hồi công khai; trong cooldown không cấp thêm token hoặc làm mất link hiện tại. Hai request đồng thời chỉ cấp tối đa một token mới. Dữ liệu sai trả lỗi validation. Mất quyền truy cập email chưa có khôi phục thủ công hoặc kênh khác trong v1.

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

**Kết quả sau cùng:** Các trường hợp lệ được lưu; username/email giữ nguyên. Hồ sơ công khai chỉ có ID/username/tên hiển thị, không có email. v1 chưa có thao tác tải avatar; chưa áp dụng allowlist locale/timezone hoặc kiểm soát version mới khi chưa chốt contract.

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

**Tác nhân:** Người quyết định và người thực hiện được phân quyền theo [RB-ACCOUNT](../../../operations-runbook.md#account-support). Người/vai trò cụ thể còn OQ-011; tài liệu này không chỉ định hoặc cấp quyền cho bất kỳ ai.

**Điều kiện trước:** Có yêu cầu/hồ sơ xử lý, user ID ổn định, môi trường/phạm vi đích và quyết định khóa hoặc mở khóa. Người thực hiện có quyền tương ứng; công cụ kỹ thuật phải được triển khai và kiểm chứng trước sử dụng.

**Luồng chính:**

1. Người thực hiện mở hồ sơ, đối chiếu quyết định, user ID, trạng thái/version hiện hành và phạm vi được cấp quyền; không thu thập mật khẩu, token hoặc nội dung DM vào hồ sơ.
2. Khi khóa, công cụ ghi trạng thái khóa và audit/receipt cần thiết, đổi security stamp và thu hồi toàn bộ phiên/token ứng dụng cùng transaction. Kiểm tra HTTP và cutoff chat/media theo [quy tắc khóa tài khoản](../../../data-lifecycle.md#account-state).
3. Khi mở khóa theo quyết định, công cụ ghi trạng thái phù hợp và audit. Người dùng phải đăng nhập mới, hoàn tất xác minh nếu còn thiếu và được kiểm tra quyền hiện hành; phiên cũ không được khôi phục.
4. Người thực hiện ghi kết quả đối soát vào hồ sơ: đúng tài khoản/phạm vi, hiệu lực thu hồi, dữ liệu lịch sử được giữ và quyền của các actor khác không bị cấp thêm.

**Ngoại lệ:** Thiếu quyền/quyết định, sai user ID hoặc trạng thái/version không còn phù hợp thì từ chối thao tác và đối chiếu lại; không thay đổi tài khoản. Lỗi transaction không để lại thay đổi từng phần. Mất response sau commit phải đối soát trạng thái/audit trước thử lại; chưa kiểm chứng cutoff thì chưa ghi đạt. Không dùng reset/verify để mở khóa; mất email không tạo kênh khôi phục thủ công.

**Kết quả sau cùng:** Tài khoản bị khóa không truy cập ứng dụng hoặc gửi/gọi; các phiên cũ bị từ chối và kết nối chat/media dừng trong ≤5 giây từ commit theo DEC-083/099. Người khác còn quyền vẫn đọc lịch sử và sửa/xóa tin của chính mình; không gửi DM/gọi mới tới peer bị khóa. Hồ sơ, tin, membership và ownership không bị xóa/chuyển vì thao tác khóa. Mở khóa không làm sống lại phiên hoặc tự xác minh email. Quyền kỹ thuật không cho đọc DM, sửa/xóa thay tác giả; v1 không có self-delete hoặc trang quản trị riêng.

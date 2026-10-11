# SCDC — Khôi phục và đổi mật khẩu

Phạm vi UC-ACC-05/06/08. Khôi phục qua email không cần phiên ứng dụng; đổi mật khẩu cần phiên hợp lệ và mật khẩu hiện tại. Cả reset/đổi thành công thu hồi mọi phiên, không tự xác minh email.

## Mục lục

- [Quy tắc](#rules)
- [Hành trình](#flows)
- [Giao diện](#ux)
- [API, dữ liệu và transaction](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-006 | Đợt đầu có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. | DEC-042 |
| ACC-014 | Khôi phục chỉ qua email đã đăng ký, gồm tài khoản chưa xác minh theo DEC-051. Mất quyền truy cập email chưa có kênh khôi phục khác hoặc quy trình thủ công trong v1. | DEC-067; không thay đổi điều kiện xác minh trước khi vào ứng dụng |
| ACC-016 | Đổi mật khẩu khi đã đăng nhập từ chối mật khẩu mới trùng mật khẩu hiện tại; đặt lại qua liên kết cho phép trùng nếu vẫn đúng policy. Reset thành công vẫn dùng token một lần, đổi security stamp và thu hồi mọi phiên dù mật khẩu trùng. | DEC-113; giữ hành vi hiện tại, không yêu cầu kiểm tra lịch sử mật khẩu |

Mật khẩu giữ nguyên nội dung, không trim; policy 8–128 `.Length`, ít nhất một chữ/số theo [ACC-010](sessions.md#rules). Token/reset cooldown, độc lập verify/reset, email worker và fragment theo [ACC-012](email-links.md). MFA, recovery code và external identity để Identity v2; v1 chưa có API.

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-05"></a>

<a id="uc-uc-acc-05--yêu-cầu-khôi-phục-mật-khẩu"></a>

### UC-ACC-05 — Yêu cầu khôi phục mật khẩu

**Tác nhân:** Người quên mật khẩu; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng có email hợp lệ để gửi yêu cầu; để nhận và dùng liên kết, cần kiểm soát hộp thư đã đăng ký. Tài khoản chờ xác minh cũng được khôi phục theo DEC-051/067.

**Luồng chính:**

1. Người dùng nhập email tại ACC-S04 và gửi yêu cầu.
2. Với tài khoản đủ điều kiện và ngoài cooldown 60 giây, hệ thống vô hiệu token reset cũ cùng mục đích, cấp token reset mới hạn 30 phút và tiếp nhận yêu cầu email.
3. Hệ thống trả phản hồi accepted; giao diện hướng dẫn kiểm tra email, không tiết lộ trạng thái tài khoản hoặc hứa email đã giao.

**Ngoại lệ:** Unknown/unavailable/cooldown có cùng phản hồi công khai; trong cooldown không cấp thêm token hoặc làm mất link hiện tại. Hai request đồng thời chỉ cấp tối đa một token mới. Dữ liệu sai trả lỗi validation. Mất quyền truy cập email chưa có khôi phục thủ công hoặc kênh khác trong v1.

**Kết quả sau cùng:** Khi cấp mới, token reset cũ không dùng được; token verify độc lập. Mật khẩu, trạng thái xác minh và phiên hiện tại chưa thay đổi chỉ vì gửi yêu cầu. Limiter bổ sung vẫn hoãn DEC-089.

<a id="uc-uc-acc-06"></a>

<a id="uc-uc-acc-06--đặt-lại-mật-khẩu-qua-liên-kết"></a>

### UC-ACC-06 — Đặt lại mật khẩu qua liên kết

**Tác nhân:** Người kiểm soát hộp thư khôi phục của tài khoản.

**Điều kiện trước:** Người dùng có liên kết reset; không cần đăng nhập ứng dụng. Token phải đúng mục đích, còn hạn, chưa dùng/bị thay thế và tài khoản đủ điều kiện.

**Luồng chính:**

1. Người dùng mở liên kết; GET không dùng token. Người dùng nhập và gửi mật khẩu mới.
2. Hệ thống kiểm tra policy mật khẩu trước khi consume; dưới khóa kiểm tra lại token, rồi cập nhật mật khẩu, security stamp, reset lockout và thu hồi mọi phiên.
3. Hệ thống đánh dấu token đã dùng và hoàn tất toàn bộ thay đổi; giao diện xóa phiên/cache liên quan và hướng dẫn đăng nhập lại.

**Ngoại lệ:** Mật khẩu trái policy trả lỗi và giữ token hợp lệ để sửa lại. Token sai/hết hạn/đã dùng/bị thay thế trả lỗi chung và đường yêu cầu mới. Hai consume chỉ một thành công. Đổi mật khẩu bằng UC-ACC-08 trước đó làm token reset cũ không còn dùng được. Mật khẩu mới trùng mật khẩu hiện tại được chấp nhận nếu hợp lệ theo ACC-016/DEC-113; token vẫn bị consume và mọi phiên vẫn bị thu hồi.

**Kết quả sau cùng:** Mật khẩu được lưu theo policy đã chốt, mọi phiên cũ bị thu hồi, không tự đăng nhập. Trạng thái xác minh email giữ nguyên; tài khoản pending vẫn phải thực hiện UC-ACC-02. Token verify còn hạn độc lập với reset.

<a id="uc-uc-acc-08"></a>

<a id="uc-uc-acc-08--đổi-mật-khẩu-khi-đã-đăng-nhập"></a>

### UC-ACC-08 — Đổi mật khẩu khi đã đăng nhập

**Tác nhân:** Người đã đăng nhập và biết mật khẩu hiện tại.

**Điều kiện trước:** Phiên của chính tài khoản hợp lệ; tài khoản active và đã xác minh.

**Luồng chính:**

1. Người dùng mở ACC-S07, nhập mật khẩu hiện tại và mật khẩu mới hợp lệ khác mật khẩu hiện tại.
2. Hệ thống kiểm tra mật khẩu hiện tại, cập nhật mật khẩu/security stamp, reset lockout, vô hiệu token reset đang có và thu hồi mọi phiên trong cùng transaction.
3. Giao diện xóa phiên/cache liên quan và về đăng nhập; người dùng đăng nhập lại bằng mật khẩu mới.

**Ngoại lệ:** Sai mật khẩu hiện tại hoặc mật khẩu mới trái policy trả lỗi; không đổi mật khẩu, vô hiệu token reset hay thu hồi phiên. Mật khẩu mới trùng hiện tại bị từ chối bằng `Identity.PasswordUnchanged` theo ACC-016/DEC-113 và không gây thay đổi dữ liệu. Login/refresh chạy đồng thời không được giữ phiên dùng mật khẩu/stamp cũ sau commit.

**Kết quả sau cùng:** Mật khẩu mới có hiệu lực; mọi thiết bị, gồm thiết bị thực hiện, phải đăng nhập lại. Token reset cấp trước đổi mật khẩu không còn dùng được; thao tác không đổi trạng thái xác minh email.

<a id="ux"></a>

## Giao diện

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Phản hồi không tiết lộ email có tài khoản hay không; không hứa email đã được giao nếu mới tiếp nhận yêu cầu |
| ACC-S05 Đặt lại mật khẩu | Liên kết hợp lệ/không dùng được; mật khẩu mới; hoàn tất | Không hiển thị token; thành công dẫn đến đăng nhập, mọi phiên cũ bị thu hồi; nếu chưa xác minh thì vẫn phải xác minh |
| ACC-S07 Đổi mật khẩu | Đang nhập/đang lưu/sai mật khẩu hiện tại/thành công | Khi thành công xóa phiên giao diện và về đăng nhập; mọi thiết bị phải đăng nhập lại |

Trang reset chỉ POST khi người dùng gửi mật khẩu mới hợp lệ; không consume bằng GET. Link sai/hết hạn/đã dùng có cùng thông báo và đường yêu cầu lại. Đổi/reset thành công xóa phiên/cache và về login; pending vẫn quay về xác minh.

<a id="contracts"></a>

## API, dữ liệu và transaction

Prefix `/api/v1`; các endpoint sau có trong code được đối chiếu ngày 2026-10-04. Mục tiêu reset pending/cooldown/email chưa hoàn thiện.

| Method / đường dẫn | Xác thực và đầu vào JSON | Kết quả thành công |
|---|---|---|
| `POST /auth/forgot-password` | Public; `email` | 202 `PasswordResetRequestedResponse` |
| `POST /auth/reset-password` | Public; `token, newPassword` | 204 |
| `POST /auth/change-password` | Bearer; `currentPassword, newPassword` | 204; thu hồi phiên, cần đăng nhập lại |

- `PasswordResetRequestedResponse`: `accepted, developmentResetToken`.

| Mã hiện tại | HTTP | Cách xử lý |
|---|---|---|
| `Identity.InvalidOrExpiredToken` | 400 | Liên kết không dùng được |
| `Identity.RegistrationInvalid`, `Identity.PasswordInvalid`, `Identity.ProfileInvalid` và lỗi trường | 400 | Hiển thị `errors` tại trường; validation MVC có thể trả `Common.ValidationFailed` trước service |
| `Identity.CurrentPasswordInvalid`, `Identity.PasswordUnchanged` | 400 | Báo tại trường mật khẩu hiện tại/mới; không xóa phiên khi đổi mật khẩu bị từ chối; trường hợp mật khẩu trùng theo DEC-113 |

| Bảng | Trách nhiệm và ràng buộc |
|---|---|
| `identity.password_credentials` | Hash mật khẩu, phiên bản và thời điểm đổi; không lưu mật khẩu thô |
| `identity.user_security_states` | Bộ đếm sai, thời hạn khóa, security stamp |
| `identity.auth_sessions` / `refresh_tokens` | Phiên/thu hồi và chuỗi rotation; DB giữ hash token |
| `identity.account_tokens` | Token băm, mục đích, target email, hạn và thời điểm dùng; token verify/reset không thay thế nhau |

Reset kiểm tra policy trước consume; dưới khóa user đọc lại token đúng purpose/target/hạn/active, rồi đổi hash/stamp, reset lockout, consume token và thu hồi phiên cùng transaction. Change-password khóa user trước credentials/sessions/tokens; thành công vô hiệu reset token cũ. Verify token độc lập với reset. Tranh khóa login/refresh không được giữ phiên/stamp cũ sau commit. Thiết kế consume/email đầy đủ ở [email-links](email-links.md#token-transaction).

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-06 | Người dùng yêu cầu đặt lại mật khẩu qua email và hoàn tất luồng với thông tin hợp lệ. | Người dùng đăng nhập được bằng mật khẩu mới; thông tin đặt lại đã dùng không thể dùng lại. |
| AC-ACC-10 | Tài khoản chưa xác minh hoàn tất đặt lại mật khẩu. | Mật khẩu mới có hiệu lực nhưng tài khoản vẫn chưa được vào ứng dụng cho tới khi xác minh email. |
| AC-ACC-18 | Tài khoản mất quyền truy cập email tìm cách khôi phục. | Chỉ có luồng qua email đã đăng ký; thông tin trợ giúp không hứa có khôi phục thủ công hoặc qua kênh khác trong v1. |
| AC-ACC-22 | Đổi và đặt lại mật khẩu với mật khẩu mới trùng mật khẩu hiện tại. | Đổi bị từ chối bằng `Identity.PasswordUnchanged`, không đổi dữ liệu hoặc thu hồi phiên/token reset; reset hợp lệ thành công, consume token, đổi stamp và thu hồi mọi phiên. Cả hai tuân policy mật khẩu; trạng thái xác minh không đổi (DEC-113). |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-03 | U đặt lại mật khẩu rồi đăng nhập trước/sau xác minh | Khôi phục không tự xác minh; chỉ sau xác minh mới vào ứng dụng | AC-ACC-10 |
| TC-ACC-19 | A có A1/A2 và token reset còn hạn: A1 đổi sang mật khẩu hiện tại rồi dùng token reset để đặt lại chính mật khẩu đó; lặp reset cho U sau khi triển khai ACC-GAP-01 | Change trả 400 `Identity.PasswordUnchanged`; mật khẩu/stamp/phiên/token reset giữ nguyên. Reset trả 204, đổi stamp, A1/A2 và refresh token cũ bị từ chối, token reset dùng lại bị từ chối; vẫn đăng nhập được bằng mật khẩu đó. U vẫn chưa xác minh và chưa được login. Ca chưa chạy; chưa có assertion tự động. | AC-ACC-22, UC-ACC-06/08, DEC-113 |

AC-ACC-06 áp dụng tài khoản đã xác minh, AC-ACC-10 bao phủ pending. TC-ACC-15 kiểm tra trợ giúp khi mất email cùng hồ sơ ở [profile](profile.md#acceptance); TC-ACC-06/07/12/13/16/17/18 và AC-ACC-14/15/19/20/21 ở [email-links](email-links.md#acceptance).

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source/test ngày 2026-10-04/05; lần tổ chức tài liệu này không chạy lại test hoặc xác nhận nghiệm thu. Trong bảng, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`. Assertion 200/204 chưa chứng minh mọi hậu điều kiện.

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-05 | AC-10/14/18/19/21; TC-03/07/12/15/16/18 | `ForgotPasswordAsync`; `POST /auth/forgot-password`; form quên mật khẩu | Lifecycle: accepted và token reset cho account đã verify | ACC-GAP-01/02/05; chưa cấp token cho pending; chưa có cooldown/email thật hoặc test phản hồi các trạng thái |
| UC-ACC-06 | AC-06/10/12/15/16/20/22; TC-03/06/10/13/14/17/18/19 | `ResetPasswordAsync`; `POST /auth/reset-password`; wrapper reset | Lifecycle: reset 204 với mật khẩu khác, access cũ bị từ chối, mật khẩu cũ không đăng nhập được; `Concurrent_reset_requests_can_consume_a_token_only_once` | Chưa có trang reset; thiếu test pending, biên hạn token, reset sai policy giữ token, verify/reset độc lập và reset mật khẩu trùng |
| UC-ACC-08 | AC-12/16/17/22; TC-10/14/17/19 | `ChangePasswordAsync`; `POST /auth/change-password`; form đổi mật khẩu | Lifecycle: đổi 204, access cũ bị từ chối; `Changing_password_invalidates_previously_issued_reset_tokens`; hai test login/đổi mật khẩu tranh khóa | UI chưa xóa phiên ngay sau thành công; thiếu test sai mật khẩu hiện tại/policy/trùng |

| Mã | Yêu cầu/căn cứ | Source hiện tại | Việc phải hoàn tất |
|---|---|---|---|
| ACC-GAP-01 | Khôi phục trước xác minh — ACC-007/014, AC-ACC-10 | `ForgotPasswordAsync` không tạo token nếu `VerifiedAt` null | Cho cấp token đúng yêu cầu; chứng minh reset không tự xác minh bằng TC-ACC-03 |

| Test hiện có | Hành vi có assertion | Coverage còn thiếu |
|---|---|---|
| `Changing_password_invalidates_previously_issued_reset_tokens` | Đổi mật khẩu vô hiệu token reset cũ | Token verify phải độc lập; kiểm thử email đến trễ |
| `Concurrent_reset_requests_can_consume_a_token_only_once` | Chỉ một reset dùng cùng token thành công | Verify đồng thời và resend |

DEC-113 đã chốt ngày 2026-10-05: change từ chối trùng, reset cho phép trùng; chưa có assertion tự động cho trường hợp trùng. UI chưa có trang reset và chưa xóa phiên ngay sau đổi mật khẩu. ACC-GAP-02/05 về cooldown/email nằm ở [email-links](email-links.md#status).

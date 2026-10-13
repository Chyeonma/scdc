# SCDC — Đăng ký và xác minh email

Phạm vi REQ-010/SCP-002; UC-ACC-01–03. Identity tạo tài khoản chờ xác minh; người dùng xác minh rồi đăng nhập. MVP chọn luồng nền, v1 hoàn thiện các nhánh theo hồ sơ release.

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
| ACC-001 | Màn hình đăng ký thu thập email, tên tài khoản, tên hiển thị và mật khẩu. | DEC-054, DEC-062 |
| ACC-008 | Tên tài khoản 3–32 ký tự ASCII chữ/số/`_`/`.`; duy nhất không phân biệt hoa/thường, chưa cho đổi. Tên hiển thị Unicode dài 1–64 đơn vị UTF-16 sau trim, không chỉ khoảng trắng; không bắt buộc duy nhất. | DEC-054, DEC-063, DEC-068 |
| ACC-009 | Email được bỏ khoảng trắng đầu/cuối, chuẩn hóa chữ thường để kiểm tra duy nhất; chưa cho đổi trong v1. Không tự bỏ dấu chấm hoặc phần `+tag` trong email. | DEC-063; không áp dụng quy tắc riêng của một nhà cung cấp email |

Tên tài khoản duy nhất và chưa cho đổi; tên hiển thị được đổi và không bắt buộc duy nhất. Email không công khai, không đổi trong v1. Mật khẩu tuân [ACC-010](sessions.md#rules); verify/reset dùng [ACC-012 và cơ chế liên kết email](email-links.md). Tài khoản pending chỉ dùng xác minh hoặc khôi phục, chưa có phiên ứng dụng theo [ACC-005/007](account-state.md#rules).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-01"></a>

<a id="uc-uc-acc-01--đăng-ký-tài-khoản"></a>

### UC-ACC-01 — Đăng ký tài khoản

**Tác nhân:** Người chưa có tài khoản; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng mở ACC-S01; không cần đăng nhập. Tính hợp lệ và duy nhất của dữ liệu được kiểm tra trong luồng đăng ký.

**Luồng chính:**

1. Người dùng nhập email, username, tên hiển thị và mật khẩu, rồi gửi đăng ký.
2. Hệ thống chuẩn hóa và kiểm tra dữ liệu theo ACC-008–010, tạo tài khoản chờ xác minh cùng hồ sơ, mật khẩu băm và yêu cầu gửi liên kết xác minh.
3. Hệ thống trả kết quả tạo tài khoản; giao diện hướng dẫn kiểm tra email và cung cấp đường gửi lại xác minh/khôi phục mật khẩu.

**Ngoại lệ:** Dữ liệu sai trả lỗi trường; username/email trùng trả xung đột. Hai đăng ký cùng định danh chỉ tạo một tài khoản; yêu cầu thất bại không để lại tài khoản dở dang. Tiếp nhận yêu cầu email chưa chứng minh email đã giao; thất bại giao email được xử lý qua UC-ACC-03.

**Kết quả sau cùng:** Tài khoản ở `PendingVerification`, chưa có phiên ứng dụng. Liên kết xác minh dùng một lần, hạn 30 phút; lần cấp đầu tính vào cooldown gửi lại. Không tự xác minh tài khoản bằng token Development trên giao diện sản phẩm.

<a id="uc-uc-acc-02"></a>

<a id="uc-uc-acc-02--xác-minh-email"></a>

### UC-ACC-02 — Xác minh email

**Tác nhân:** Người kiểm soát hộp thư của tài khoản đăng ký.

**Điều kiện trước:** Người dùng có liên kết xác minh; thao tác không đòi hỏi đăng nhập ứng dụng.

**Luồng chính:**

1. Người dùng mở liên kết. Trang hướng dẫn xác minh; chỉ mở trang bằng GET chưa làm thay đổi tài khoản.
2. Người dùng bấm xác minh; hệ thống kiểm tra đúng mục đích, email đích, thời hạn, trạng thái token và tài khoản, rồi dùng token một lần.
3. Hệ thống xác minh email, chuyển tài khoản đang chờ xác minh sang active và hướng dẫn đăng nhập.

**Ngoại lệ:** Token sai mục đích, hết hạn, đã dùng hoặc bị thay thế trả lỗi liên kết không dùng được và đường yêu cầu lại. Hai request dùng cùng token chỉ một request thành công; consume và resend tuân thứ tự commit. Khi response mất sau commit, dùng lại token bị từ chối; người dùng có thể thử đăng nhập hoặc yêu cầu lại. Xác minh không tự mở khóa tài khoản bị khóa theo DEC-104.

**Kết quả sau cùng:** Email đã xác minh, token không dùng lại được; chưa cấp phiên đăng nhập. Token reset còn hiệu lực được giữ độc lập. Không hiện token kỹ thuật trên màn hình; cách đọc và xóa fragment trong URL thuộc [thiết kế liên kết](email-links.md#contracts).

<a id="uc-uc-acc-03"></a>

<a id="uc-uc-acc-03--yêu-cầu-gửi-lại-xác-minh"></a>

### UC-ACC-03 — Yêu cầu gửi lại xác minh

**Tác nhân:** Người chưa hoàn tất xác minh email; hệ thống email hỗ trợ gửi liên kết.

**Điều kiện trước:** Người dùng biết email đăng ký. Yêu cầu công khai không cần phiên ứng dụng và phải có email hợp lệ.

**Luồng chính:**

1. Người dùng nhập email và yêu cầu gửi lại từ ACC-S03.
2. Nếu tài khoản đủ điều kiện và đã cách lần cấp token verify trước ít nhất 60 giây, hệ thống vô hiệu token verify cũ, cấp token mới hạn 30 phút và tiếp nhận yêu cầu email trong cùng transaction.
3. Hệ thống trả `202 {accepted:true}`; giao diện hướng dẫn kiểm tra email, không xác nhận email đã giao hoặc tài khoản tồn tại.

**Ngoại lệ:** Email không tồn tại, đã xác minh, tài khoản không đủ điều kiện hoặc đang cooldown đều có cùng phản hồi công khai. Trong cooldown không cấp token, không vô hiệu liên kết đang dùng. Hai yêu cầu đồng thời chỉ cấp tối đa một token mới; không tiết lộ thời gian chờ riêng của email. Email cũ đến trễ vẫn chứa link đã bị vô hiệu. Dữ liệu email sai trả lỗi validation; limiter bổ sung vẫn hoãn theo DEC-089.

**Kết quả sau cùng:** Khi thực sự cấp mới, chỉ link verify mới có hiệu lực và token reset không bị thay đổi. Khi không đủ điều kiện cấp, dữ liệu token được giữ nguyên. Người dùng vẫn chưa có phiên ứng dụng.

<a id="ux"></a>

## Giao diện

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
```

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S01 Đăng ký | Đang nhập, dữ liệu sai, đang gửi, thất bại | Giữ email/tên tài khoản/tên hiển thị đã nhập; lỗi tại trường phù hợp; tránh bấm gửi lặp trong khi chờ |
| ACC-S03 Xác minh email | Đã tiếp nhận yêu cầu, mở liên kết, đang xác minh, thành công, liên kết không dùng được | Hướng dẫn kiểm tra email; có đường gửi lại, tối thiểu 60 giây giữa hai yêu cầu cùng mục đích; liên kết mới vô hiệu liên kết cũ; không hứa email đã giao khi mới tiếp nhận |

ACC-S03 là điểm dừng của tài khoản chưa xác minh; có đường khôi phục mật khẩu. Không tự xác minh bằng token Development trong giao diện sản phẩm. Link được đọc/xóa khỏi URL và consume bằng thao tác chủ động theo [liên kết email](email-links.md#browser-links). Desktop/điện thoại theo DEC-059 và ma trận DEC-082; prototype, thiết bị/kích thước và tiêu chí tiếp cận còn OQ-007.

<a id="contracts"></a>

## API, dữ liệu và transaction

Prefix hiện tại `/api/v1`; Swagger sinh từ source ở `/swagger/v1/swagger.json` khi chạy Development.

| Method / đường dẫn | Xác thực và đầu vào JSON | Kết quả thành công |
|---|---|---|
| `POST /auth/register` | Public; `username, displayName, email, password` | 201 `RegistrationResponse`; chưa cấp phiên |
| `POST /auth/verify-email` | Public; `token` | 204 |

- `RegistrationResponse`: `userId, username, email, verificationRequired, developmentVerificationToken`.

Token Development chỉ được trả khi `ExposeDevelopmentTokens` bật, không dùng làm UX sản phẩm. Email service trim/chữ thường, giới hạn 254; HTTP/service/DB phải thống nhất normalize → validate → transaction → constraint.

<a id="api-hợp-đồng-gửi-lại-cần-triển-khai"></a>

### Hợp đồng gửi lại cần triển khai

`POST /api/v1/auth/resend-verification` là endpoint đề xuất, chưa có controller/service. Đầu vào `{email}` hợp lệ; thành công `202 {accepted: true}` cùng ý nghĩa cho email tồn tại/không tồn tại/đã xác minh/không đủ điều kiện. Với tài khoản chờ xác minh đủ điều kiện, trong một transaction khóa tài khoản, kiểm tra cooldown, vô hiệu token verify cũ rồi tạo token/outbox mới. Hai request đồng thời chỉ tạo tối đa một token mới trong cooldown; không vô hiệu token reset khi gửi lại verify.

Cooldown theo email yêu cầu cần có cùng phản hồi công khai cho các trạng thái tài khoản để tránh tiết lộ tồn tại; limiter theo nguồn có thể trả 429 ProblemDetails và `Retry-After` mà không phụ thuộc email tồn tại. Schema/mã lỗi và ngưỡng bổ sung là thiết kế cần rà soát. Không trả token thô ngoài Development.

| Bảng | Trách nhiệm và ràng buộc |
|---|---|
| `identity.users` | ID ổn định, username và trạng thái; unique username chuẩn hóa |
| `identity.user_profiles` | Một hồ sơ mỗi user; tên hiển thị không phải khóa đăng nhập |
| `identity.user_emails` | Unique email chuẩn hóa; tối đa một email primary; `verified_at` tách khỏi mật khẩu |
| `identity.password_credentials` | Hash mật khẩu, phiên bản và thời điểm đổi; không lưu mật khẩu thô |
| `identity.account_tokens` | Token băm, mục đích, target email, hạn và thời điểm dùng; token verify/reset không thay thế nhau |

Đăng ký phải ghi user/hồ sơ/email/mật khẩu băm/token/yêu cầu email cùng transaction; unique username/email xử lý cả request đồng thời. Lần cấp token đầu ghi cooldown. Verify và resend giữ khóa/consume theo [cơ chế email dùng chung](email-links.md#token-transaction); source verify hiện còn phải kiểm chứng concurrent consume.

| Mã hiện tại | HTTP | Cách xử lý |
|---|---|---|
| `Identity.UsernameAlreadyExists`, `Identity.EmailAlreadyExists`, `Identity.RegistrationConflict` | 409 | Báo xung đột đăng ký |
| `Identity.InvalidOrExpiredToken` | 400 | Liên kết không dùng được |
| `Identity.RegistrationInvalid`, `Identity.PasswordInvalid`, `Identity.ProfileInvalid` và lỗi trường | 400 | Hiển thị `errors` tại trường; validation MVC có thể trả `Common.ValidationFailed` trước service |

Định danh phải đi qua cùng pipeline trước validation HTTP/service/DB: email bỏ khoảng trắng đầu/cuối rồi chuyển chữ thường theo DEC-063; username trim rồi kiểm tra ASCII/quy tắc duy nhất; tên hiển thị trim, từ chối toàn khoảng trắng, đếm UTF-16. Không trim/chuẩn hóa mật khẩu hoặc nội dung tin ngoài bước CRLF→LF đã đặc tả ở DM. Locale/timezone giữ validation độ dài hiện tại; việc giới hạn danh sách locale/zone hỗ trợ cần lựa chọn riêng, không tự đổi policy trong bước thiết kế.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-01 | Người chưa có tài khoản điền thông tin đăng ký hợp lệ. | Tài khoản được tạo ở trạng thái chờ xác minh email để có thể nhắn tin. |
| AC-ACC-05 | Tài khoản chưa xác minh email thử gửi tin riêng hoặc tin trong phòng; sau đó xác minh email và thử lại. | Trước xác minh hệ thống từ chối gửi tin; sau xác minh có thể gửi theo quyền tương ứng. |
| AC-ACC-09 | Hai đăng ký cùng tên tài khoản chạy đồng thời. | Chỉ một tài khoản được tạo; yêu cầu còn lại nhận lỗi dữ liệu trùng, không để lại tài khoản dở dang. |
| AC-ACC-11 | Đăng ký/sửa hồ sơ với định danh ở biên độ dài, hoa/thường và khoảng trắng đầu/cuối. | Username/email có cùng quy tắc chuẩn hóa và unique; tên hiển thị Unicode đếm UTF-16; không cho đổi username/email. |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-01 | Đăng ký hợp lệ; dùng email và tên tài khoản đăng nhập sau xác minh | Cả hai cách vào cùng tài khoản; tên tài khoản không đổi | AC-ACC-01, AC-ACC-02 |
| TC-ACC-04 | Hai request đăng ký cùng tên tài khoản đồng thời | Một tài khoản; request còn lại lỗi trùng; không có dữ liệu dở dang | AC-ACC-09 |
| TC-ACC-09 | Biên username 2/3/32/33; tên hiển thị 0/1/64/65 UTF-16; hai email/username chỉ khác hoa thường; khoảng trắng đầu/cuối | Chuẩn hóa/validation nhất quán; một định danh duy nhất; không lưu tài khoản dở dang | AC-ACC-11 |

AC-ACC-01 bao gồm displayName bắt buộc theo DEC-062. AC/TC dùng chung cho link/email/cooldown ở [email-links](email-links.md#acceptance); truy cập pending ở [account-state](account-state.md#acceptance).

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source/test ngày 2026-10-04/05; lần tổ chức tài liệu này không chạy lại test hoặc xác nhận nghiệm thu. Trong bảng, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`. Assertion 200/204 chưa chứng minh mọi hậu điều kiện.

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-01 | AC-01/09/11/12/19; TC-01/04/09/10/16 | `RegisterAsync`; `POST /auth/register`; form đăng ký | Lifecycle: status 201, token Development, từ chối login trước verify | ACC-GAP-04/05/07; đăng ký trùng đồng thời/Unicode/policy mật khẩu chưa có test; UI đang tự verify bằng token Development |
| UC-ACC-02 | AC-05/07/15/19/20; TC-01/02/06/13/17/18 | `VerifyEmailAsync`; `POST /auth/verify-email`; wrapper verify | Lifecycle: verify 204, login sau verify, `emailVerified=true` | ACC-GAP-03/05; chưa có test verify đồng thời/hết hạn/dùng lại; chưa có trang mở liên kết và xác minh chủ động |
| UC-ACC-03 | AC-14/19/20/21; TC-07/12/16/17/18 | Resend chỉ có thiết kế/OpenAPI mục tiêu | Chưa có test cho resend | ACC-GAP-02/05; thiếu endpoint, cooldown, email và UI |

| Mã | Yêu cầu/căn cứ | Source hiện tại | Việc phải hoàn tất |
|---|---|---|---|
| ACC-GAP-04 | Chuẩn hóa định danh nhất quán — ACC-008/009 | Service trim nhưng MVC kiểm tra regex/độ dài trước service | Khóa thứ tự chuẩn hóa/validation HTTP–service–DB; fixture khoảng trắng/hoa thường |
| ACC-GAP-07 | Tên hiển thị UTF-16 — ACC-008, DEC-068 | Service dùng `.Length`; MVC và DB có giới hạn riêng | Fixture tiếng Việt tổ hợp/emoji và biên 64; kiểm chứng HTTP–DB |

ACC-GAP-02/03/05 và việc chọn provider/domain/key store theo dõi tại [email-links](email-links.md#status). ACC-P01: định danh ACC-008/009 và tên hiển thị UTF-16 đã chốt DEC-063/068; chuẩn hóa còn cần kiểm chứng.

Source: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthScreen](../../../clients/WebClient/src/components/AuthScreen.jsx), [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs).

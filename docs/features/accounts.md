# SCDC — Tài khoản

Cập nhật: 2026-10-04. Phạm vi: REQ-010, SCP-002. Quy tắc ACC, tiêu chí AC-ACC, màn hình ACC-S, ca TC-ACC và ACL-01.

Nghiệp vụ cốt lõi và chính sách tài khoản đã xác nhận theo DEC-063–067; phép đếm UTF-16 theo DEC-068. Identity có implementation và test tự động; gửi lại xác minh, giao email và một số hành vi còn thiếu hoặc khác yêu cầu. Đối chiếu source ngày 2026-10-04 ở mục implementation-review. Các ca TC chưa có kết quả thực thi được ghi nhận trong hồ sơ này.

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền](#permissions)
- [Giao diện](#ux)
- [API hiện tại](#api-current)
- [Trạng thái, dữ liệu và đối chiếu implementation](#implementation-review)
- [Quyết định và thiết kế còn lại](#gaps)
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


Đợt tài khoản bao gồm đăng ký/xác minh, đăng nhập, khôi phục mật khẩu và hồ sơ. Code hiện còn có đổi mật khẩu và quản lý phiên/thiết bị. MFA, recovery code và external identity để Identity v2; chưa có API cho các phần này. Gửi lại xác minh và giao email thật là phần chưa hoàn thiện.

DEC-062 đồng bộ trường `displayName` bắt buộc theo form và API hiện tại; DEC-054 tiếp tục quản lý cách đăng nhập và quy tắc định danh. Username/email thuộc định danh, tên hiển thị thuộc hồ sơ có thể đổi.

<a id="permissions"></a>

## 2. Quyền

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003, DEC-065/066 |

Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không có phiên truy cập ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh email. Quyền tìm người/DM ở [đặc tả DM](direct-messaging.md#permissions); quyền phòng ở [Community](community.md#permissions).

<a id="ux"></a>

## 3. Giao diện và trạng thái

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

## 4. API hiện tại và dữ liệu

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

Nguồn: [controllers](../../services/SCDC.Api/Controllers/Identity/AuthController.cs), [request schema](../../services/SCDC.Api/Controllers/Identity/IdentityRequests.cs), [response schema](../../services/Modules/Identity/Application/IdentityModels.cs), [validation](../../services/Modules/Identity/Application/IdentityValidation.cs), [options](../../services/Modules/Identity/Infrastructure/IdentityOptions.cs).

<a id="implementation-review"></a>

### Trạng thái tài khoản và phiên

Bảng tài khoản mô tả yêu cầu đã chốt. `Suspended`, `Disabled`, `Deleted` có trong enum/SQL; luồng chuyển các trạng thái này chưa được đặc tả thành tính năng quản trị MVP.

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

<a id="gaps"></a>

## 5. Quyết định đã chốt và thiết kế còn lại

Đăng ký → tiếp nhận yêu cầu email → xác minh liên kết → đăng nhập → ứng dụng. Quên mật khẩu → tiếp nhận yêu cầu → mở liên kết → đặt mật khẩu mới → đăng nhập; chưa xác minh thì vẫn quay về xác minh. Liên kết hết hạn/đã dùng có đường yêu cầu lại, tuân cooldown.

Giữ các mã ACC-P để truy vết; phần đã xác nhận dẫn tới DEC, phần thiết kế chưa được rà soát tiếp tục có nhãn đề xuất.

| Mã phương án cũ | Kết luận | Trạng thái |
|---|---|---|
| ACC-P01 | Định danh theo ACC-008/009; tên hiển thị đếm UTF-16 | Đã chốt DEC-063/068; còn kiểm chứng chuẩn hóa |
| ACC-P02 | Verify/reset một lần, 30 phút; cấp lại vô hiệu link cũ cùng mục đích | Đã chốt DEC-065; còn thiết kế consume/resend |
| ACC-P03 | Cooldown 60 giây; reset/resend không tiết lộ email tồn tại; rate limit bổ sung theo nguồn/tài khoản | Cooldown chốt DEC-065; ngưỡng bổ sung cần thiết kế |
| ACC-P04 | Phiên tối đa 30 ngày; đổi/reset thu hồi mọi phiên | Đã chốt DEC-065; transport hiện tại Bearer/refresh JSON, thu hồi realtime cần thiết kế |
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

Định danh phải đi qua cùng pipeline trước validation HTTP/service/DB: email bỏ khoảng trắng đầu/cuối rồi chuyển chữ thường theo DEC-063; username trim rồi kiểm tra ASCII/quy tắc duy nhất; tên hiển thị trim, từ chối toàn khoảng trắng, đếm UTF-16. Không trim/chuẩn hóa mật khẩu hoặc nội dung tin ngoài bước CRLF→LF đã đặc tả ở DM. Locale/timezone cần kiểm tra cả định dạng hỗ trợ để không nhận giá trị chỉ đúng độ dài.

Limiter bổ sung là cấu hình kỹ thuật dự kiến, chưa được xác nhận thành chính sách sản phẩm: đăng ký 5 lần/giờ/IP; login 30 lần/5 phút/IP bên cạnh lockout 5 lần/tài khoản; forgot/resend 10 lần/giờ/IP và tối đa 5 email/giờ/tài khoản, vẫn tuân cooldown 60 giây. Key tài khoản limiter dùng hash/HMAC của định danh chuẩn hóa; phản hồi accepted/cooldown không phân biệt tài khoản tồn tại. Limiter theo IP trả 429 ProblemDetails có `Retry-After`; kiểm thử mạng dùng chung và IPv6 trước khóa ngưỡng. Không coi limiter là biện pháp thay thế transaction consume token hoặc kiểm tra phiên.

Thiết kế còn mở: ACC-GAP-01–07; ngưỡng limiter/email pipeline cần rà soát và kiểm chứng; thu hồi realtime và cấu hình thiết bị ở OQ-007/008. OQ-002 giữ mở cho các đầu ra này. Các chính sách đã chốt không phải bằng chứng implementation hoặc nghiệm thu đã đạt.

<a id="acceptance"></a>

## 6. Tiêu chí chấp nhận

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

AC-ACC-06 áp dụng cho tài khoản đã xác minh; AC-ACC-10 bao phủ tài khoản
chưa xác minh. Toàn bộ tiêu chí vẫn cần có kết quả chạy và xác nhận
nghiệm thu, không được đánh dấu đạt chỉ vì quy tắc đã chốt.

Đăng ký hợp lệ ở AC-ACC-01 bao gồm tên hiển thị bắt buộc theo DEC-062. Chưa đánh dấu tiêu chí đạt chỉ vì có code hoặc ca test.

<a id="tests"></a>

## 7. Ca kiểm thử

Dữ liệu: A là tài khoản đã xác minh; U chưa xác minh; A1/A2 là hai phiên của A. Chuẩn bị email thử/hoặc token Development, DB thử riêng, phiên trình duyệt độc lập và khả năng gửi request đồng thời.
| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-01 | Đăng ký hợp lệ; dùng email và tên tài khoản đăng nhập sau xác minh | Cả hai cách vào cùng tài khoản; tên tài khoản không đổi | AC-ACC-01, AC-ACC-02 |
| TC-ACC-02 | U nhập đúng mật khẩu; thử mở hội thoại/tìm người/API ứng dụng | Không truy cập ứng dụng; có đường xác minh/khôi phục | AC-ACC-07, DEC-051 |
| TC-ACC-03 | U đặt lại mật khẩu rồi đăng nhập trước/sau xác minh | Khôi phục không tự xác minh; chỉ sau xác minh mới vào ứng dụng | AC-ACC-10 |
| TC-ACC-04 | Hai request đăng ký cùng tên tài khoản đồng thời | Một tài khoản; request còn lại lỗi trùng; không có dữ liệu dở dang | AC-ACC-09 |
| TC-ACC-05 | A đổi tên hiển thị; B tìm tên đó; thử đổi tên tài khoản | Tên hiển thị mới xuất hiện; tên tài khoản giữ nguyên; dữ liệu công khai không có email | AC-ACC-03, AC-ACC-08 |
| TC-ACC-06 | Dùng liên kết xác minh/reset sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác trái phép; không lộ token trong UI/log | AC-ACC-06; thời hạn 30 phút theo DEC-065 |
| TC-ACC-07 | Gửi yêu cầu khôi phục/gửi lại với email tồn tại và không tồn tại | Phản hồi công khai có cùng ý nghĩa; không tiết lộ tài khoản | ACC-P03; cooldown đã chốt, rate limit bổ sung chờ thiết kế |
| TC-ACC-08 | Phiên hết hạn/đăng xuất, rồi dùng lại bằng chứng phiên hoặc kết nối cũ | Không đọc/gửi/nhận dữ liệu tiếp theo sau thời hạn thu hồi đã chốt | AC-ACC-04; cơ chế và ngưỡng chờ thiết kế phiên |
| TC-ACC-09 | Biên username 2/3/32/33; tên hiển thị 0/1/64/65 UTF-16; hai email/username chỉ khác hoa thường; khoảng trắng đầu/cuối | Chuẩn hóa/validation nhất quán; một định danh duy nhất; không lưu tài khoản dở dang | AC-ACC-11 |
| TC-ACC-10 | Mật khẩu 7/8/128/129, chỉ chữ/chỉ số; 5 lần sai rồi mật khẩu đúng trước/sau hết khóa | Policy và lockout theo DEC-064; thử đúng sau hết khóa có thể đăng nhập | AC-ACC-12 |
| TC-ACC-11 | Đồng hồ thử tại biên hạn access/phiên và refresh trước hạn phiên | Không gia hạn phiên bằng refresh; ghi clock skew 30 giây khi đo JWT | AC-ACC-13 |
| TC-ACC-12 | Gửi lại verify/reset ở giây 59/60; hai request đồng thời; dùng link cũ/mới | Cooldown nhất quán, tối đa một link mới; mục đích độc lập | AC-ACC-14; cần implementation resend/limiter |
| TC-ACC-13 | Hai request dùng cùng verify token và reset token | Chỉ một consume thành công; request còn lại lỗi token | AC-ACC-15; verify concurrency cần kiểm chứng |
| TC-ACC-14 | A1 thu hồi A2 rồi gọi API trên hai phiên; lặp với phiên hiện tại/logout-all/đổi/reset mật khẩu | Đúng phạm vi phiên bị thu hồi; không suy test HTTP thành test realtime | AC-ACC-16 |
| TC-ACC-15 | Sửa từng trường hồ sơ và dữ liệu vượt giới hạn; xem thông tin trợ giúp khi mất email | Trường hợp lệ lưu được; định danh không đổi; không hứa khôi phục khác email | AC-ACC-17/18 |
| TC-ACC-16 | Ngoài Development, nhận email verify/reset từ worker; dừng worker rồi retry; cấp link mới trước khi email cũ được giao | Không lộ token ở response/log; email dùng link đúng; link bị thay thế không còn hợp lệ | AC-ACC-19; cần worker/provider |

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

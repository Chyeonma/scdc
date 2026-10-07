# SCDC — Thiết kế tài khoản

API/dữ liệu được đối chiếu source ngày 2026-10-04; thiết kế email/token còn phần đề xuất. Implementation và bằng chứng tại [status](../status.md).

<a id="api-current"></a>

## API hiện tại và dữ liệu

Prefix `/api/v1`. Bảng này mô tả code có trong repo; schema đầy đủ được sinh ở `/swagger/v1/swagger.json` khi chạy Development. Lỗi theo [ProblemDetails chung](../../../architecture.md#contracts).

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

Nguồn: [controllers](../../../../services/SCDC.Api/Controllers/Identity/AuthController.cs), [request schema](../../../../services/SCDC.Api/Controllers/Identity/IdentityRequests.cs), [response schema](../../../../services/Modules/Identity/Application/IdentityModels.cs), [validation](../../../../services/Modules/Identity/Application/IdentityValidation.cs), [options](../../../../services/Modules/Identity/Infrastructure/IdentityOptions.cs).

<a id="implementation-review"></a>

### Trạng thái tài khoản và phiên

Bảng tài khoản mô tả yêu cầu đã chốt. `Suspended`, `Disabled`, `Deleted` có trong enum/SQL; DEC-104/112 đã chốt hành vi khóa/mở khóa qua quy trình kỹ thuật có phân quyền/audit và chưa có UI quản trị riêng. [RB-ACCOUNT](../../../operations-runbook.md#account-support) là thiết kế mục tiêu; chưa có công cụ chuyển trạng thái được triển khai. Không tự suy Deleted thành tính năng xóa tài khoản v1 DEC-103.

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

Nguồn đối chiếu: [RegistrationService](../../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [IdentityModule](../../../../services/Modules/Identity/IdentityModule.cs), [IdentityDbContext](../../../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs), [IUserDirectory](../../../../services/SCDC.Contracts/Identity/IUserDirectory.cs).

<a id="gaps"></a>

## Quyết định đã chốt và thiết kế còn lại

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

Phương án ngày 2026-10-04 để triển khai DEC-063–067; thuật toán/schema dưới đây được soạn trong phạm vi tài liệu, chưa thay source. [OpenAPI luồng xác minh/khôi phục](../../../contracts/account-recovery.openapi.json) có 4 thao tác, ghi rõ resend chưa có endpoint và các endpoint hiện có còn chênh lệch. Swagger sinh từ source vẫn là nguồn cho API hiện đang chạy.

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

Đổi/reset mật khẩu, logout hoặc đổi tài khoản xóa state UI, nội dung DM đang cache và đóng Hub của phiên đó. Source [api.js](../../../../clients/WebClient/src/api.js) hiện chia sẻ token phiên qua localStorage khi có Web Locks; DEC-091 chỉ nói nội dung DM/bản nháp, không tuyên bố đã đổi cơ chế lưu token. Refresh token đã rotation mà response mất không tự retry token cũ vì có thể kích hoạt reuse; client về login theo lỗi hiện tại.

#### Hồ sơ và chuẩn hóa khi bàn giao

Chốt pipeline normalize → validate → transaction → constraint; validation HTTP không chạy bộ quy tắc khác service. Username/email unique ở DB xử lý cả đăng ký đồng thời. DisplayName trim/UTF-16 như ACC-008; bio hiện trim và rỗng thành null; locale/timezone giữ validation độ dài hiện tại, chưa tự thêm allowlist locale/zone chỉ vì có giá trị trông hợp lệ. Source đang dùng version hồ sơ ở response; cập nhật optimistic concurrency là hướng thiết kế, chưa thêm `expectedVersion` vào API hiện có khi chưa thay contract.

Đầu ra triển khai tài khoản: thêm resend/policy/delivery migration + worker; sửa reset pending/verify consume; cập nhật shared validator và auth UI; mở rộng hợp đồng thu hồi session cho chat. Kết quả của các gói này phải ghi vào ACC-GAP và TC, không đánh dấu đã hoàn thành chỉ vì có schema trong docs.

# SCDC — Tài khoản

Cập nhật: 2026-10-03. Phạm vi: REQ-010, SCP-002. Quy tắc ACC, tiêu chí AC-ACC, màn hình ACC-S, ca TC-ACC và ACL-01.

Nghiệp vụ cốt lõi đã xác nhận; Identity có implementation và test tự động. Chính sách chi tiết và email production còn thiếu. Các ca TC dưới đây chưa có kết quả chạy được ghi nhận; không coi trạng thái có code là nghiệm thu.

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền](#permissions)
- [Giao diện](#ux)
- [API hiện tại](#api-current)
- [Đề xuất và khoảng trống](#gaps)
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
khi nhiều người có cùng tên. Quy tắc đổi email còn mở. Trước xác minh,
tài khoản chỉ dùng xác minh email hoặc khôi phục mật khẩu, chưa được
vào các chức năng ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh
email; sau đó vẫn phải hoàn tất xác minh trước khi đăng nhập ứng dụng.

### Hành trình và quy tắc dự thảo

| Mã | Quy tắc/hành vi | Tình trạng |
|---|---|---|
| ACC-001 | Màn hình đăng ký thu thập email, tên tài khoản, tên hiển thị và mật khẩu. | DEC-054, DEC-062 |
| ACC-002 | Màn hình đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu. | DEC-054 |
| ACC-003 | Người đã đăng nhập có thể đổi tên hiển thị của mình; tên tài khoản duy nhất, chưa cho đổi; email không công khai. | DEC-054; trường hồ sơ khác còn cần đặc tả |
| ACC-004 | API cần nhận diện tài khoản bằng định danh ổn định, không dùng tên hiển thị làm khóa. | Đề xuất kỹ thuật để đáp ứng tìm kiếm/chọn đúng người |
| ACC-005 | Người dùng cần xác minh email trước khi gửi tin riêng hoặc tin trong phòng. | DEC-041 |
| ACC-006 | Đợt đầu có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. | DEC-042 |
| ACC-007 | Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không được vào ứng dụng; đặt lại mật khẩu không thay thế xác minh email. | DEC-051; hệ quả của việc tách hai mục đích |


Đợt tài khoản bao gồm đăng ký/xác minh, đăng nhập, khôi phục mật khẩu và hồ sơ. Code hiện còn có đổi mật khẩu và quản lý phiên/thiết bị. MFA, recovery code và external identity để Identity v2; chưa có API cho các phần này. Gửi lại xác minh và giao email thật là phần chưa hoàn thiện.

DEC-062 đồng bộ trường `displayName` bắt buộc theo form và API hiện tại; DEC-054 tiếp tục quản lý cách đăng nhập và quy tắc định danh. Username/email thuộc định danh, tên hiển thị thuộc hồ sơ có thể đổi.

<a id="permissions"></a>

## 2. Quyền

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003; chi tiết trường và phiên chờ OQ-002 |

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
| ACC-S03 Xác minh email | Đã yêu cầu gửi email, mở liên kết, đang xác minh, thành công, liên kết không dùng được | Hướng dẫn kiểm tra email; liên kết sai/hết hạn/đã dùng có đường quay về đăng nhập; gửi lại phụ thuộc quy tắc tài khoản |
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Không mất ngữ cảnh lý do cần đăng nhập; chỉ về hội thoại sau khi xác thực thành công và kiểm tra quyền |
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Phản hồi không tiết lộ email có tài khoản hay không; không hứa email đã được giao nếu mới tiếp nhận yêu cầu |
| ACC-S05 Đặt lại mật khẩu | Liên kết hợp lệ/không dùng được; mật khẩu mới; hoàn tất | Không hiển thị token; thành công dẫn đến đăng nhập; tác động đến phiên chờ đặc tả tài khoản |

Theo DEC-051, ACC-S03 là điểm dừng của tài khoản chưa xác minh: chỉ
có xác minh và đường khôi phục mật khẩu, chưa vào ứng dụng. Đặt lại
mật khẩu không tự bỏ qua bước xác minh.

Hồ sơ hiện tại cho sửa `displayName`, `bio`, `locale`, `timezone`; username/email không có endpoint đổi. Cần thể hiện tải/lưu/lỗi và trạng thái phiên bị thu hồi sau đổi mật khẩu. Tài khoản hỗ trợ bố cục desktop/trình duyệt điện thoại theo DEC-059; trình duyệt/phiên bản/kích thước và tiêu chí tiếp cận còn OQ-007. Thiết kế thị giác và prototype vẫn cần rà soát; form hiện tại nằm trong [AuthScreen](../../clients/WebClient/src/components/AuthScreen.jsx).

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
| Display name | Bắt buộc; service trim, độ dài 1–64 theo `.Length` của .NET (UTF-16), không phải phép đếm grapheme đã duyệt |
| Email | Hợp lệ, tối đa 254; service trim và chuẩn hóa chữ thường |
| Password | 8–128; có ít nhất một chữ và một số |
| Hồ sơ | Bio tối đa 500, locale 1–16, timezone 1–64; cập nhật không đổi username/email |
| Access token / phiên | Mặc định 15 phút / 30 ngày; cấu hình được |
| Token verify/reset | Mặc định 30 phút, token dùng một lần; cấu hình được |
| Lockout | Mặc định sau 5 lần sai, khóa 15 phút; cấu hình được |
| Refresh | Rotation; reuse thu hồi phiên |
| Thu hồi | Request Bearer kiểm tra session, trạng thái tài khoản và security stamp; đổi/reset mật khẩu thu hồi phiên |

Các giá trị trên là bằng chứng hành vi hiện tại, chưa tự đóng toàn bộ OQ-002. Bảo vệ email, rate limit, trải nghiệm và nghiệm thu cần rà soát riêng.

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

<a id="gaps"></a>

## 5. Đề xuất và khoảng trống

### Luồng chi tiết và đề xuất để rà soát kỹ thuật

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

Implementation đã có password policy, thời hạn và lockout như bảng API hiện tại. Chưa chốt đầy đủ chính sách sản phẩm về giới hạn yêu cầu, mất quyền truy cập email và bảo vệ tài khoản. Các mục
ACC-P* là phương án có thể xem xét, không phải DEC hoặc tiêu chí đã duyệt.
API hiện tại ở mục API của tài liệu này; hợp đồng công khai phục vụ tìm người được quản lý trong đặc tả DM.

### Cần làm rõ trước khi xác nhận toàn bộ đặc tả

| Vấn đề | Quyết định cần có |
|---|---|
| Định danh | Rà soát ACC-P01/02 về ký tự, độ dài, chuẩn hóa và email duy nhất; tên tài khoản duy nhất/không đổi đã chốt. |
| Xác minh | Rà soát thời hạn/gửi lại theo ACC-P02/03; phạm vi trước xác minh đã chốt tại DEC-051. |
| Khôi phục | Thời hạn và giới hạn yêu cầu đặt lại, xử lý email không còn truy cập được. |
| Phiên | Thời hạn, đăng xuất, đăng xuất mọi thiết bị và xử lý phiên hết hạn. |
| Hồ sơ | Trường khác ngoài tên hiển thị, khả năng đổi email theo ACC-P05; email không công khai đã chốt. |
| Chống lạm dụng | Giới hạn đăng ký/đăng nhập/tìm kiếm và xử lý tài khoản bị khóa. |

Các mục trên thuộc OQ-002; không suy diễn thành quyết định đã chốt.


`POST /api/v1/auth/resend-verification` là endpoint đề xuất, chưa có controller/service. Chính sách gửi lại sau 60 giây, vô hiệu liên kết cũ và giới hạn gửi lại vẫn cần thiết kế/rà soát.

Repo ghi outbox yêu cầu email nhưng chưa có worker/email provider. Cần thiết kế cách cung cấp token thô/liên kết gửi được cho worker, vì DB chỉ lưu hash token; không coi outbox hiện tại là email đã được giao. Chính sách thu hồi kết nối realtime cần thiết kế khi Messaging được triển khai.

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
| TC-ACC-06 | Dùng liên kết xác minh/reset sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác trái phép; không lộ token trong UI/log | AC-ACC-06; thời hạn cụ thể chờ ACC-P02 |
| TC-ACC-07 | Gửi yêu cầu khôi phục/gửi lại với email tồn tại và không tồn tại | Phản hồi công khai có cùng ý nghĩa; không tiết lộ tài khoản | ACC-P03 — ca đề xuất chờ xác nhận |
| TC-ACC-08 | Phiên hết hạn/đăng xuất, rồi dùng lại bằng chứng phiên hoặc kết nối cũ | Không đọc/gửi/nhận dữ liệu tiếp theo sau thời hạn thu hồi đã chốt | AC-ACC-04; cơ chế và ngưỡng chờ thiết kế phiên |

Ngoài các ca trên, kiểm tra trường `displayName` thiếu/rỗng/vượt 64, password sai policy, rotation/reuse refresh, logout-all, revoke session và đổi mật khẩu. Bộ test tự động trong repo: [IdentityV1FlowTests](../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [IdentityConcurrencyTests](../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). Cần đối chiếu coverage thực tế với AC/TC, không suy từ tên test.

Chạy hành trình trên desktop và trình duyệt điện thoại theo DEC-059; danh sách trình duyệt/phiên bản chờ OQ-007. Mỗi kết quả ghi theo [mẫu nghiệm thu](../release-operations.md#testing).

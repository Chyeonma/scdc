# SCDC — Đăng nhập và phiên truy cập

Phạm vi UC-ACC-04/09/10/11. Identity sở hữu login, refresh rotation, phiên/thu hồi và security stamp. Browser điều phối token giữa các request/tab; thu hồi kết nối chat/media cần bằng chứng riêng.

## Mục lục

- [Quy tắc](#rules)
- [Hành trình](#flows)
- [Giao diện](#ux)
- [API và dữ liệu](#contracts)
- [Đồng thời và thu hồi](#concurrency)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-002 | Màn hình đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu. | DEC-054 |
| ACC-010 | Mật khẩu 8–128 theo `.Length` của .NET, có ít nhất một chữ và một số theo `char.IsLetter`/`char.IsDigit`; không tự trim hoặc đổi nội dung mật khẩu. Sai 5 lần khóa đăng nhập 15 phút. | DEC-064; giữ chính sách hiện tại |
| ACC-011 | Access token 15 phút; phiên tối đa 30 ngày từ lúc đăng nhập, refresh không kéo dài thời hạn phiên. Có đăng xuất phiên hiện tại, thu hồi từng phiên và đăng xuất mọi thiết bị; đổi/đặt lại mật khẩu thu hồi mọi phiên. | DEC-065 |

ACC-P04: phiên tối đa 30 ngày, đổi/reset thu hồi tất cả (DEC-065); transport hiện tại Bearer/refresh JSON, thu hồi realtime còn cần triển khai. Định danh tuân ACC-008/009 ở [đăng ký](registration-verification.md#rules); account active/verified và quyền sở hữu theo [account-state](account-state.md).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-04"></a>

<a id="uc-uc-acc-04--đăng-nhập"></a>

### UC-ACC-04 — Đăng nhập

**Tác nhân:** Người có tài khoản, đang cần truy cập ứng dụng.

**Điều kiện trước:** Người dùng mở ACC-S02; để thành công, tài khoản active, email đã xác minh và không trong lockout.

**Luồng chính:**

1. Người dùng nhập email hoặc username cùng mật khẩu và gửi đăng nhập.
2. Hệ thống chuẩn hóa định danh, kiểm tra mật khẩu, lockout, trạng thái tài khoản và xác minh email.
3. Hệ thống tạo phiên tối đa 30 ngày, cấp access token 15 phút và refresh token; giao diện vào ứng dụng và kiểm tra quyền của đích truy cập.

**Ngoại lệ:** Định danh không tồn tại hoặc mật khẩu sai trả lỗi thông tin đăng nhập; lần sai thứ 5 khóa đăng nhập 15 phút, kể cả khi nhập đúng trong thời gian khóa. Đúng mật khẩu nhưng chưa xác minh trả `Identity.EmailNotVerified` và dẫn tới xác minh/khôi phục. Tài khoản bị khóa/không khả dụng không được cấp phiên ứng dụng. Login chạy cùng đổi/reset mật khẩu phải kiểm tra lại dưới khóa; phiên tạo trước commit đổi mật khẩu không còn hiệu lực sau commit đó.

**Kết quả sau cùng:** Email và username đăng nhập vào cùng một tài khoản. Login thành công xóa bộ đếm sai/lockout; login thất bại không cấp phiên. Khóa đăng nhập tạm thời không đổi account thành trạng thái bị khóa quản trị.

<a id="uc-uc-acc-09"></a>

<a id="uc-uc-acc-09--làm-mới-phiên-truy-cập"></a>

### UC-ACC-09 — Làm mới phiên truy cập

**Tác nhân:** Trình duyệt thay người đã đăng nhập.

**Điều kiện trước:** Trình duyệt có refresh token; để thành công, token chưa dùng/thu hồi/hết hạn, phiên còn hiệu lực và tài khoản active.

**Luồng chính:**

1. Khi access token cần làm mới, trình duyệt gửi refresh token hiện hành.
2. Hệ thống kiểm tra token/phiên dưới khóa, đánh dấu token cũ đã dùng và cấp access/refresh token mới cùng phiên.
3. Trình duyệt thay token phiên; các request đồng thời trong tab và các tab chia sẻ phiên phối hợp để chỉ thực hiện một rotation cần thiết.

**Ngoại lệ:** Refresh token sai hoặc phiên hết hạn/thu hồi không được cấp lại; client xóa phiên tương ứng và về đăng nhập. Dùng lại token đã rotation thu hồi chính phiên đó với lỗi reuse. Nếu response rotation mất, không tự gửi lại token cũ. Refresh đang chờ không được khôi phục phiên đã logout hoặc ghi đè login mới. Khi thiếu Web Locks, mỗi tab giữ phiên riêng theo implementation hiện tại.

**Kết quả sau cùng:** Hạn phiên giữ nguyên mốc tối đa 30 ngày từ login; refresh không gia hạn phiên. Access token mới có thời hạn theo ACC-011; khi đo biên hết hạn JWT phải ghi clock skew hiện tại 30 giây.

<a id="uc-uc-acc-10"></a>

<a id="uc-uc-acc-10--xem-và-thu-hồi-một-phiên"></a>

### UC-ACC-10 — Xem và thu hồi một phiên

**Tác nhân:** Người đã đăng nhập.

**Điều kiện trước:** Người gọi có phiên hợp lệ của chính tài khoản.

**Luồng chính:**

1. Người dùng mở ACC-S08; hệ thống trả các phiên của mình còn hạn/chưa thu hồi và đánh dấu phiên hiện tại.
2. Người dùng chọn một phiên cần thu hồi; hệ thống kiểm tra quyền sở hữu, thu hồi phiên đó và refresh token liên quan.
3. Giao diện cập nhật danh sách. Thu hồi phiên khác giữ phiên người gọi; thu hồi phiên hiện tại xóa phiên giao diện và dẫn về đăng nhập.

**Ngoại lệ:** ID phiên không tồn tại hoặc thuộc tài khoản khác trả `Identity.SessionNotFound`, không làm lộ phiên người khác. Thu hồi lặp phiên của mình đã bị thu hồi không tạo lại phiên. Tải danh sách lỗi phải hiển thị lỗi/thử lại; danh sách rỗng phải được phản ánh đúng, không thay bằng thiết bị mẫu. Phiên người gọi đã hết hạn/thu hồi phải đăng nhập lại.

**Kết quả sau cùng:** Chỉ phiên được chọn bị thu hồi; request HTTP tiếp theo của phiên đó bị từ chối. Kết nối chat đang mở phải ngừng nhận dữ liệu theo DEC-083; thực hiện và kiểm chứng realtime thuộc tích hợp DM, chưa có bằng chứng chỉ từ test Identity HTTP.

<a id="uc-uc-acc-11"></a>

<a id="uc-uc-acc-11--đăng-xuất-phiên-hiện-tại-hoặc-mọi-thiết-bị"></a>

### UC-ACC-11 — Đăng xuất phiên hiện tại hoặc mọi thiết bị

**Tác nhân:** Người dùng muốn kết thúc phiên truy cập.

**Điều kiện trước:** Logout phiên hiện tại dùng refresh token đang giữ và không yêu cầu Bearer; logout-all cần một phiên xác thực hợp lệ của chính tài khoản.

**Luồng chính:**

1. Người dùng chọn đăng xuất phiên hiện tại hoặc tất cả thiết bị.
2. Hệ thống thu hồi phiên tương ứng: logout thu hồi phiên gắn với refresh token; logout-all thu hồi mọi phiên của tài khoản, gồm phiên gọi.
3. Giao diện xóa phiên và dữ liệu riêng/cache của phiên đó, đóng kết nối liên quan và về đăng nhập; các tab chia sẻ phiên nhận thay đổi.

**Ngoại lệ:** Logout với token không nhận diện được hoặc phiên đã thu hồi vẫn hoàn tất theo hành vi 204 hiện tại. Logout-all bằng phiên không hợp lệ bị từ chối. Nếu request logout lỗi/mất kết nối, client vẫn xóa phiên local nhưng không được tuyên bố đã thu hồi thành công trên server hoặc mọi thiết bị; cần phân biệt với kết quả thành công. Refresh đang chờ không được khôi phục phiên đã xóa.

**Kết quả sau cùng:** Khi server xác nhận thành công, đúng phạm vi phiên bị thu hồi; logout một phiên không ảnh hưởng phiên độc lập khác. Logout-all yêu cầu mọi thiết bị đăng nhập lại. Hành vi trên HTTP và việc dừng kết nối chat phải có bằng chứng riêng.

<a id="ux"></a>

## Giao diện

```text
ACC-S02 · Đăng nhập
┌────────────────────────────────┐
│ Email hoặc tên tài khoản       │
│ [                            ] │
│ Mật khẩu          [          ] │
│ [Đăng nhập]                    │
│ [Quên mật khẩu] / [Đăng ký]    │
└────────────────────────────────┘
```

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Không mất ngữ cảnh lý do cần đăng nhập; chỉ về hội thoại sau khi xác thực thành công và kiểm tra quyền |
| ACC-S08 Phiên/thiết bị | Tải danh sách, thu hồi một phiên, đăng xuất mọi thiết bị | Đánh dấu phiên hiện tại; thu hồi phiên khác không đăng xuất phiên hiện tại; thu hồi phiên hiện tại/đăng xuất tất cả về đăng nhập |

<a id="contracts"></a>

## API và dữ liệu

Prefix `/api/v1`; bảng mô tả code được đối chiếu ngày 2026-10-04.

| Method / đường dẫn | Xác thực và đầu vào JSON | Kết quả thành công |
|---|---|---|
| `POST /auth/login` | Public; `login, password, deviceName?` | 200 `AuthResponse` |
| `POST /auth/refresh` | Public; `refreshToken` | 200 `AuthResponse` với token mới |
| `POST /auth/logout` | Public; `refreshToken` | 204 |
| `POST /auth/logout-all` | Bearer; không có body | 204 |
| `GET /auth/sessions` | Bearer | 200 danh sách `SessionResponse` của mình |
| `DELETE /auth/sessions/{sessionId}` | Bearer | 204; chỉ thu hồi phiên của mình |

- `AuthResponse`: `accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user`.

- `SessionResponse`: `id, deviceName, userAgent, lastSeenIp, createdAt, lastSeenAt, expiresAt, isCurrent`.

| Trạng thái phiên | Thao tác | Kết quả theo yêu cầu và source hiện tại |
|---|---|---|
| Còn hạn/chưa thu hồi | Refresh bằng token chưa dùng | Token cũ được đánh dấu đã dùng, cấp token mới; hạn phiên giữ nguyên |
| Còn hạn | Dùng lại refresh token đã rotation | Thu hồi chính phiên đó; 401 `Identity.RefreshTokenReuseDetected` |
| Còn hạn | Logout hoặc thu hồi phiên của mình | Phiên/token của phiên bị thu hồi; logout với token không nhận diện được vẫn trả 204 |
| Còn hạn | Logout-all, đổi/reset mật khẩu | Thu hồi mọi phiên của tài khoản; request xác thực tiếp theo bị từ chối |
| Đã hết hạn/thu hồi | Dùng access hoặc refresh token cũ | Không cấp lại phiên; cần đăng nhập mới |

HTTP Bearer hiện kiểm tra session, trạng thái active và security stamp trong DB trên mỗi request; JWT cho phép clock skew 30 giây theo source. Chưa có kết nối realtime để kiểm chứng ngưỡng thu hồi trên kết nối mở. Request đã được xác thực trước thay đổi phiên/quyền cần được xem xét riêng trong thiết kế đồng thời.

| Bảng | Trách nhiệm và ràng buộc |
|---|---|
| `identity.user_security_states` | Bộ đếm sai, thời hạn khóa, security stamp |
| `identity.auth_sessions` / `refresh_tokens` | Phiên/thu hồi và chuỗi rotation; DB giữ hash token |

| Mã hiện tại | HTTP | Cách xử lý |
|---|---|---|
| `Identity.InvalidCredentials` | 401 | Báo thông tin đăng nhập sai |
| `Identity.EmailNotVerified` | 403 | Dẫn tới luồng xác minh |
| `Identity.AccountUnavailable` | 403 | Không cho vào ứng dụng |
| `Identity.AccountLocked` | 429 | Báo khóa tạm |
| `Identity.InvalidRefreshToken`, `Identity.RefreshTokenReuseDetected` | 401 | Xóa phiên phía client, đăng nhập lại |
| `Identity.UserNotFound`, `Identity.SessionNotFound` | 404 | Báo không còn tài nguyên |

<a id="concurrency"></a>

## Đồng thời và thu hồi

Login/refresh/đổi hoặc reset mật khẩu khóa user trước credential/session/token. Login phải đọc lại mật khẩu/stamp sau khi tranh khóa; phiên tạo trước commit đổi/reset không được sống sau commit. Refresh rotation chỉ có một người thực hiện giữa các request/tab chia sẻ phiên; response mất không tự replay refresh token cũ.

Đổi/reset mật khẩu, logout hoặc đổi tài khoản xóa state UI, nội dung DM đang cache và đóng Hub của phiên đó. Source [api.js](../../../clients/WebClient/src/api.js) hiện chia sẻ token phiên qua localStorage khi có Web Locks; DEC-091 chỉ nói nội dung DM/bản nháp, không tuyên bố đã đổi cơ chế lưu token. Refresh token đã rotation mà response mất không tự retry token cũ vì có thể kích hoạt reuse; client về login theo lỗi hiện tại.

HTTP kiểm tra session/stamp/expiry không chứng minh thu hồi trên Hub đang mở. DEC-083 yêu cầu chat ngừng nhận dữ liệu ≤5 giây từ commit; kiểm chứng cùng DM. Thu hồi media theo DEC-099 được triển khai và đo riêng.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-02 | Người dùng nhập email cùng mật khẩu hợp lệ, rồi lặp lại với tên tài khoản cùng mật khẩu. | Cả hai cách đều truy cập cùng một tài khoản. |
| AC-ACC-04 | Người không đăng nhập cố mở hội thoại riêng hoặc cộng đồng cần tư cách thành viên. | Hệ thống yêu cầu đăng nhập và không trả dữ liệu riêng tư. |
| AC-ACC-12 | Mật khẩu ở biên 8/128, thiếu chữ/số; sau đó đăng nhập sai 5 lần. | Từ chối mật khẩu trái policy; từ lần sai thứ 5 khóa 15 phút, kể cả nhập đúng trong lúc khóa; không làm mất mật khẩu hay dữ liệu tài khoản. |
| AC-ACC-13 | Refresh gần hạn phiên 30 ngày rồi thử sau hạn. | Rotation không kéo dài phiên; phiên hết hạn yêu cầu đăng nhập; access token theo mốc 15 phút, khi đo phải ghi clock skew. |
| AC-ACC-16 | Thu hồi phiên khác, phiên hiện tại, logout-all và đổi/reset mật khẩu. | Từ chối request mới từ đúng các phiên bị thu hồi; đổi/reset thu hồi tất cả; phiên khác còn sống khi chỉ thu hồi một phiên. |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-08 | Phiên hết hạn/đăng xuất, rồi dùng lại bằng chứng phiên hoặc kết nối cũ | Request mới bị từ chối; kết nối chat ngừng nhận dữ liệu trong ≤5 giây sau commit thu hồi | AC-ACC-04/16, DEC-083; cơ chế realtime còn cần triển khai |
| TC-ACC-10 | Mật khẩu 7/8/128/129, chỉ chữ/chỉ số; 5 lần sai rồi mật khẩu đúng trước/sau hết khóa | Policy và lockout theo DEC-064; thử đúng sau hết khóa có thể đăng nhập | AC-ACC-12 |
| TC-ACC-11 | Đồng hồ thử tại biên hạn access/phiên và refresh trước hạn phiên | Không gia hạn phiên bằng refresh; ghi clock skew 30 giây khi đo JWT | AC-ACC-13 |
| TC-ACC-14 | A1 thu hồi A2 rồi gọi API trên hai phiên; lặp với phiên hiện tại/logout-all/đổi/reset mật khẩu | Đúng phạm vi phiên bị thu hồi; không suy test HTTP thành test realtime | AC-ACC-16 |

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source/test ngày 2026-10-04/05; lần tổ chức tài liệu này không chạy lại test hoặc xác nhận nghiệm thu. Trong bảng, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`. Assertion 200/204 chưa chứng minh mọi hậu điều kiện.

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-04 | AC-02/04/07/12; TC-01/02/08/10 | `LoginAsync`; `POST /auth/login`; form login | Lifecycle: login username trước/sau verify; `Parallel_wrong_passwords_are_counted_and_trigger_lockout`; hai test login/đổi mật khẩu tranh khóa | Login bằng email, biên hết lockout và UI dẫn tới xác minh chưa có test; form hiện chỉ báo lỗi chung |
| UC-ACC-09 | AC-13/16; TC-08/11/14 | `RefreshAsync`; `POST /auth/refresh`; wrapper refresh | Lifecycle: rotation/reuse và access bị từ chối; Client: phối hợp refresh/không khôi phục logout hoặc ghi đè login mới | Chưa có test biên hạn phiên/access, response rotation bị mất hoặc trình duyệt thật; thu hồi realtime chưa triển khai |
| UC-ACC-10 | AC-04/16; TC-08/14 | `GetSessionsAsync`, `RevokeSessionAsync`; `GET /auth/sessions`, `DELETE /auth/sessions/{id}`; tab phiên | Lifecycle: đánh dấu current, revoke phiên khác và access phiên đó bị từ chối | Chưa assertion phiên người gọi vẫn sống ngay sau revoke, revoke phiên hiện tại/quyền sở hữu; UI dùng phiên mẫu khi lỗi/rỗng và chưa có thao tác revoke phiên hiện tại |
| UC-ACC-11 | AC-04/16; TC-08/14 | `LogoutAsync`, `LogoutAllAsync`; `POST /auth/logout`, `POST /auth/logout-all`; wrapper xóa phiên local | Lifecycle: logout/logout-all và từ chối access; Client: logout giữa tab không bị refresh khôi phục | UI ghi “tất cả thiết bị khác” trong khi logout-all gồm phiên gọi; thiếu test logout token không nhận diện, lỗi mạng/phạm vi phiên và cleanup dữ liệu riêng/realtime |

Rà soát ngày 2026-10-05. Prefix API là `/api/v1`. `Lifecycle` là test `Identity_v1_supports_the_complete_password_account_lifecycle` trong [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs); các test có tên riêng khác nằm trong [IdentityConcurrencyTests](../../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). `Client` là [api.test.js](../../../clients/WebClient/tests/api.test.js), gồm assertion rotation giữa tab, storage event bị bỏ lỡ, 401 đến trễ, logout khi refresh đang chờ, login mới và trường hợp thiếu Web Locks. Các test này dùng browser/fetch mô phỏng, chưa chứng minh UI hoặc trình duyệt thật.

Source nghiệp vụ: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [UserDirectory](../../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs). Giao diện/wrapper: [AuthScreen](../../../clients/WebClient/src/components/AuthScreen.jsx), [UserSettingsModal](../../../clients/WebClient/src/components/UserSettingsModal.jsx), [api.js](../../../clients/WebClient/src/api.js).

### Assertion hiện có

| Test hiện có | Hành vi có assertion | Coverage còn thiếu |
|---|---|---|
| `Identity_v1_supports_the_complete_password_account_lifecycle` | Đăng ký, từ chối login trước verify, verify, login bằng username, hồ sơ, rotation/reuse, reset/đổi mật khẩu, phiên/revoke/logout-all/logout | Login bằng email; reset trước verify; boundary/cooldown/resend; link hết hạn/verify đồng thời; email thật |
| `A_concurrent_login_cannot_survive_a_password_change` / `A_login_waiting_for_a_password_change_rechecks_the_password` | Hai thứ tự chạy login và đổi/reset; phiên/mật khẩu cũ không còn dùng được | Hành trình nhiều tab và kết nối realtime |
| `Parallel_wrong_passwords_are_counted_and_trigger_lockout` | Đếm đúng request sai đồng thời, khóa đăng nhập | Biên hết khóa và giới hạn theo nguồn |

Client test dùng browser/fetch mô phỏng; chưa chứng minh trình duyệt thật. Chưa có test biên hạn/clock skew, response rotation bị mất hoặc realtime cutoff. Danh sách phiên UI đang dùng dữ liệu mẫu khi lỗi/rỗng; nhãn logout-all hiện sai phạm vi. ACC-GAP-06 về limiter theo dõi ở [email-links](email-links.md#status).

# SCDC — Đăng nhập và phiên

Người dùng đăng nhập bằng email hoặc tên tài khoản. Mỗi lần đăng nhập tạo một phiên; người dùng xem được các phiên của mình, thu hồi từng phiên, hoặc đăng xuất mọi thiết bị.

**Trạng thái:** đăng nhập, làm mới token, xem và thu hồi phiên, đăng xuất đã có trong nền `900e226`. Hub DM đã kiểm tra phiên và cắt kết nối khi phiên bị thu hồi. Thu hồi cho tin phòng, Media và giữa các service v1 chưa có; thiết kế dùng chung cho mọi service ở [thu hồi truy cập](../../system/access-revocation.md).

Identity sở hữu đăng nhập, làm mới token, phiên, thu hồi và security stamp. Trình duyệt phối hợp token giữa các request và các tab.

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-002 | Đăng nhập bằng email hoặc tên tài khoản, cùng mật khẩu. |
| ACC-010 | Mật khẩu dài 8–128 (`.Length` của .NET), có ít nhất một chữ (`char.IsLetter`) và một số (`char.IsDigit`). Không cắt khoảng trắng hay đổi nội dung mật khẩu. Sai 5 lần thì khóa đăng nhập 15 phút. |
| ACC-011 | Access token sống 15 phút. Phiên sống tối đa 30 ngày kể từ lúc đăng nhập; làm mới token không kéo dài phiên. Có: đăng xuất phiên hiện tại, thu hồi từng phiên, đăng xuất mọi thiết bị. Đổi hoặc đặt lại mật khẩu thu hồi mọi phiên. |

Định danh theo [ACC-008, 009](registration-verification.md#rules). Tài khoản phải hoạt động và đã xác minh ([trạng thái tài khoản](account-state.md)).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-04"></a>

<a id="uc-uc-acc-04--đăng-nhập"></a>

### UC-ACC-04 — Đăng nhập

**Ai làm:** người có tài khoản.

**Điều kiện để thành công:** tài khoản hoạt động, email đã xác minh, không đang bị khóa đăng nhập tạm.

**Các bước:**

1. Người dùng mở màn hình Đăng nhập (ACC-S02), nhập email hoặc tên tài khoản và mật khẩu.
2. Hệ thống chuẩn hóa định danh, kiểm tra mật khẩu, khóa tạm, trạng thái tài khoản và xác minh email.
3. Tạo phiên (tối đa 30 ngày), cấp access token (15 phút) và refresh token. Giao diện vào ứng dụng và kiểm tra quyền với trang đích.

**Ngoại lệ:**

- Không có tài khoản hoặc sai mật khẩu: báo sai thông tin đăng nhập. Lần sai thứ 5 khóa 15 phút; trong thời gian khóa, nhập đúng cũng bị từ chối.
- Đúng mật khẩu nhưng chưa xác minh: `Identity.EmailNotVerified`, dẫn tới xác minh hoặc khôi phục.
- Tài khoản bị khóa hoặc không dùng được: không cấp phiên.
- Đăng nhập chạy cùng lúc với đổi hoặc đặt lại mật khẩu: kiểm tra lại dưới khóa; phiên tạo trước khi đổi mật khẩu commit sẽ không còn hiệu lực sau đó.

**Kết quả:** email và tên tài khoản cùng vào một tài khoản. Đăng nhập thành công thì xóa bộ đếm sai. Khóa đăng nhập tạm không phải là khóa tài khoản.

<a id="uc-uc-acc-09"></a>

<a id="uc-uc-acc-09--làm-mới-phiên-truy-cập"></a>

### UC-ACC-09 — Làm mới token

**Ai làm:** trình duyệt, thay cho người dùng.

**Điều kiện để thành công:** refresh token chưa dùng, chưa thu hồi, chưa hết hạn; phiên còn hiệu lực; tài khoản hoạt động.

**Các bước:**

1. Khi access token cần làm mới, trình duyệt gửi refresh token hiện tại.
2. Hệ thống kiểm tra token và phiên dưới khóa, đánh dấu token cũ đã dùng, cấp cặp token mới trong cùng phiên.
3. Trình duyệt thay token. Các request đồng thời trong một tab, và các tab dùng chung phiên, phối hợp để chỉ làm mới một lần.

**Ngoại lệ:**

- Refresh token sai, hoặc phiên hết hạn hay bị thu hồi: không cấp mới; client xóa phiên và về đăng nhập.
- Dùng lại refresh token đã được thay: thu hồi luôn phiên đó (lỗi reuse).
- Mất phản hồi khi làm mới: không tự gửi lại token cũ.
- Lần làm mới đang chờ không được khôi phục phiên đã đăng xuất, hay ghi đè phiên đăng nhập mới.
- Trình duyệt không có Web Locks: mỗi tab giữ phiên riêng.

**Kết quả:** hạn phiên giữ nguyên (tối đa 30 ngày từ lúc đăng nhập). Khi đo biên hết hạn JWT, ghi lại độ lệch đồng hồ cho phép hiện tại (30 giây).

<a id="uc-uc-acc-10"></a>

<a id="uc-uc-acc-10--xem-và-thu-hồi-một-phiên"></a>

### UC-ACC-10 — Xem và thu hồi một phiên

**Ai làm:** người đã đăng nhập, với phiên hợp lệ.

**Các bước:**

1. Người dùng mở màn hình Phiên (ACC-S08); hệ thống liệt kê các phiên còn hạn của mình, đánh dấu phiên hiện tại.
2. Người dùng chọn một phiên để thu hồi; hệ thống kiểm tra phiên đó thuộc về người gọi, rồi thu hồi phiên và refresh token liên quan.
3. Giao diện cập nhật danh sách. Thu hồi phiên khác thì phiên hiện tại vẫn giữ; thu hồi phiên hiện tại thì về đăng nhập.

**Ngoại lệ:**

- Phiên không tồn tại hoặc thuộc người khác: `Identity.SessionNotFound` (không lộ phiên của người khác).
- Thu hồi lại phiên đã thu hồi: không tạo lại phiên.
- Tải danh sách lỗi: hiện lỗi và nút thử lại. Danh sách trống: hiện trống, không hiện thiết bị mẫu.

**Kết quả:** chỉ phiên được chọn bị thu hồi; request HTTP tiếp theo của phiên đó bị từ chối. Kết nối chat đang mở phải ngừng nhận trong 5 giây (DEC-083); việc này thuộc tích hợp DM, test HTTP của Identity không chứng minh được.

<a id="uc-uc-acc-11"></a>

<a id="uc-uc-acc-11--đăng-xuất-phiên-hiện-tại-hoặc-mọi-thiết-bị"></a>

### UC-ACC-11 — Đăng xuất

**Ai làm:** người dùng muốn kết thúc phiên.

**Điều kiện:** đăng xuất phiên hiện tại chỉ cần refresh token (không cần Bearer). Đăng xuất mọi thiết bị cần phiên hợp lệ.

**Các bước:**

1. Người dùng chọn đăng xuất phiên này, hoặc đăng xuất mọi thiết bị.
2. Đăng xuất: thu hồi phiên gắn với refresh token. Mọi thiết bị: thu hồi mọi phiên, kể cả phiên đang dùng.
3. Giao diện xóa phiên, dữ liệu riêng và dữ liệu đệm, đóng kết nối, về đăng nhập. Các tab dùng chung phiên cũng nhận thay đổi.

**Ngoại lệ:**

- Đăng xuất với token không nhận ra hoặc phiên đã thu hồi: vẫn trả 204.
- Đăng xuất mọi thiết bị với phiên không hợp lệ: bị từ chối.
- Request đăng xuất lỗi hoặc mất mạng: client vẫn xóa phiên ở máy, nhưng không được báo là đã thu hồi thành công trên server.
- Lần làm mới token đang chờ không được khôi phục phiên đã xóa.

**Kết quả:** khi server xác nhận, đúng phạm vi phiên bị thu hồi; đăng xuất một phiên không ảnh hưởng phiên khác. Đăng xuất mọi thiết bị buộc mọi nơi đăng nhập lại. Việc dừng kết nối chat cần bằng chứng riêng.

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

| Màn hình | Trạng thái | Phản hồi |
|---|---|---|
| ACC-S02 Đăng nhập | Sai thông tin, chưa xác minh, hết phiên, lỗi dịch vụ | Giữ ngữ cảnh lý do phải đăng nhập; chỉ về trang trước sau khi đăng nhập và kiểm tra quyền |
| ACC-S08 Phiên | Tải danh sách, thu hồi một phiên, đăng xuất mọi thiết bị | Đánh dấu phiên hiện tại; thu hồi phiên khác không đăng xuất phiên này; thu hồi phiên này hoặc tất cả thì về đăng nhập |

<a id="contracts"></a>

## API và dữ liệu

Tiền tố `/api/v1`.

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `POST /auth/login` | Không cần đăng nhập; `login, password, deviceName?` | 200 `AuthResponse` |
| `POST /auth/refresh` | Không cần đăng nhập; `refreshToken` | 200 `AuthResponse` với token mới |
| `POST /auth/logout` | Không cần đăng nhập; `refreshToken` | 204 |
| `POST /auth/logout-all` | Bearer; không có body | 204 |
| `GET /auth/sessions` | Bearer | 200 danh sách `SessionResponse` của mình |
| `DELETE /auth/sessions/{sessionId}` | Bearer | 204; chỉ phiên của mình |

- `AuthResponse`: `accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user`.
- `SessionResponse`: `id, deviceName, userAgent, lastSeenIp, createdAt, lastSeenAt, expiresAt, isCurrent`.

| Phiên | Thao tác | Kết quả |
|---|---|---|
| Còn hạn | Làm mới bằng token chưa dùng | Token cũ đánh dấu đã dùng, cấp token mới; hạn phiên không đổi |
| Còn hạn | Dùng lại refresh token đã được thay | Thu hồi phiên đó; 401 `Identity.RefreshTokenReuseDetected` |
| Còn hạn | Đăng xuất hoặc thu hồi phiên của mình | Phiên và token bị thu hồi |
| Còn hạn | Đăng xuất mọi thiết bị, đổi hoặc đặt lại mật khẩu | Thu hồi mọi phiên; request tiếp theo bị từ chối |
| Hết hạn hoặc đã thu hồi | Dùng token cũ | Không cấp lại; phải đăng nhập mới |

**Kiểm tra phiên trên HTTP:** mỗi request Bearer kiểm tra phiên, trạng thái tài khoản và security stamp trong DB. JWT cho phép lệch đồng hồ 30 giây.

**Kiểm tra phiên trên Hub DM:** `IAuthenticatedSessionReader` và `SubscriptionReconciler` đối chiếu phiên và tài khoản, ngừng gửi và đóng kết nối khi không còn hợp lệ hoặc không xác nhận được. Bằng chứng ([hồ sơ DM](../../records/verification/messaging/DM-MVP.md)) không áp dụng cho tin phòng, Media hay v1.

Request đã xác thực trước khi phiên hoặc quyền đổi vẫn cần guard tới commit.

| Bảng | Nội dung |
|---|---|
| `identity.user_security_states` | Số lần sai, thời hạn khóa tạm, security stamp |
| `identity.auth_sessions`, `refresh_tokens` | Phiên, thu hồi, chuỗi làm mới; chỉ lưu hash token |

| Mã lỗi | HTTP | Xử lý |
|---|---|---|
| `Identity.InvalidCredentials` | 401 | Báo sai thông tin đăng nhập |
| `Identity.EmailNotVerified` | 403 | Dẫn tới xác minh |
| `Identity.AccountUnavailable` | 403 | Không cho vào ứng dụng |
| `Identity.AccountLocked` | 429 | Báo đang khóa tạm |
| `Identity.InvalidRefreshToken`, `Identity.RefreshTokenReuseDetected` | 401 | Xóa phiên ở client, đăng nhập lại |
| `Identity.UserNotFound`, `Identity.SessionNotFound` | 404 | Báo không còn tài nguyên |

<a id="concurrency"></a>

## Đồng thời và thu hồi

- Đăng nhập, làm mới token, đổi và đặt lại mật khẩu đều khóa người dùng trước, rồi mới tới mật khẩu, phiên, token.
- Đăng nhập phải đọc lại mật khẩu và stamp sau khi chờ khóa; phiên tạo trước khi đổi mật khẩu commit không được sống sau đó.
- Chỉ một nơi làm mới token cho mỗi phiên (giữa các request và các tab). Mất phản hồi thì không gửi lại token cũ (có thể bị coi là dùng lại), mà về đăng nhập.
- Đổi hay đặt lại mật khẩu, đăng xuất, đổi tài khoản: xóa trạng thái giao diện, dữ liệu DM đệm và đóng Hub của phiên đó.
- [api.js](../../../clients/WebClient/src/api.js) chia sẻ token qua localStorage khi có Web Locks. (DEC-091 chỉ nói về nội dung DM và bản nháp, không nói về cách lưu token.)
- HTTP từ chối phiên cũ không chứng minh Hub đang mở đã ngừng. Chat phải ngừng trong 5 giây kể từ commit (DEC-083), kiểm thử cùng DM. Media theo DEC-099, kiểm thử riêng.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-02 | Đăng nhập bằng email, rồi bằng tên tài khoản | Cả hai vào cùng một tài khoản |
| AC-ACC-04 | Người chưa đăng nhập mở cuộc trò chuyện hoặc cộng đồng cần là thành viên | Yêu cầu đăng nhập, không trả dữ liệu riêng |
| AC-ACC-12 | Mật khẩu dài 8 và 128, thiếu chữ hoặc số; rồi đăng nhập sai 5 lần | Từ chối mật khẩu sai quy tắc; từ lần sai thứ 5 khóa 15 phút, kể cả nhập đúng trong lúc khóa; dữ liệu tài khoản không mất |
| AC-ACC-13 | Làm mới token gần hạn 30 ngày, rồi sau hạn | Làm mới không kéo dài phiên; hết hạn phải đăng nhập; access token theo mốc 15 phút (ghi độ lệch đồng hồ khi đo) |
| AC-ACC-16 | Thu hồi phiên khác, phiên hiện tại, đăng xuất mọi thiết bị, đổi và đặt lại mật khẩu | Request mới từ đúng các phiên bị thu hồi bị từ chối; đổi hay đặt lại mật khẩu thu hồi tất cả; thu hồi một phiên không ảnh hưởng phiên khác |

Ký hiệu: A là tài khoản đã xác minh, A1 và A2 là hai phiên của A. Dùng DB thử riêng và các cửa sổ trình duyệt riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-08 | Phiên hết hạn hoặc đã đăng xuất; dùng lại token hoặc kết nối cũ | Request mới bị từ chối; kết nối chat ngừng nhận trong 5 giây. Bằng chứng DM không áp cho tin phòng hay Media | AC-ACC-04, 16 |
| TC-ACC-10 | Mật khẩu dài 7, 8, 128, 129; chỉ chữ; chỉ số. Sai 5 lần rồi nhập đúng trước và sau khi hết khóa | Quy tắc và khóa tạm đúng; sau khi hết khóa đăng nhập được | AC-ACC-12 |
| TC-ACC-11 | Chỉnh đồng hồ thử ở biên hạn access token và hạn phiên; làm mới trước hạn phiên | Làm mới không kéo dài phiên; ghi độ lệch 30 giây khi đo JWT | AC-ACC-13 |
| TC-ACC-14 | A1 thu hồi A2 rồi gọi API trên cả hai; lặp với phiên hiện tại, mọi thiết bị, đổi và đặt lại mật khẩu | Đúng phiên bị thu hồi; test HTTP không thay test realtime | AC-ACC-16 |

Kết quả phải ghi kèm commit và build.

<a id="status"></a>

## Hiện trạng

Đối chiếu với [nền MVP `900e226`](../../releases/mvp.md) ngày 2026-10-13. Bảng test Identity rà soát ngày 2026-10-05. Lần cập nhật này không chạy lại test. Phản hồi 200 hay 204 chưa chứng minh mọi kết quả sau đó.

Ký hiệu cột test:

- **Lifecycle:** test `Identity_v1_supports_the_complete_password_account_lifecycle` trong [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs).
- **Client:** [api.test.js](../../../clients/WebClient/tests/api.test.js), dùng trình duyệt và fetch mô phỏng: làm mới giữa các tab, sự kiện storage bị lỡ, 401 đến trễ, đăng xuất khi đang làm mới, đăng nhập mới, thiếu Web Locks.
- Các test có tên khác nằm trong [IdentityConcurrencyTests](../../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs).

| UC | Liên quan | Code hiện có | Test tự động | Còn thiếu |
|---|---|---|---|---|
| UC-ACC-04 | AC-ACC-02, 04, 07, 12; TC-ACC-01, 02, 08, 10 | `LoginAsync`; `POST /auth/login`; form đăng nhập | Lifecycle: đăng nhập bằng tên tài khoản trước và sau xác minh. `Parallel_wrong_passwords_are_counted_and_trigger_lockout`. Hai test đăng nhập và đổi mật khẩu tranh khóa | Chưa test đăng nhập bằng email, biên hết khóa, giao diện dẫn tới xác minh. Form hiện chỉ báo lỗi chung |
| UC-ACC-09 | AC-ACC-13, 16; TC-ACC-08, 11, 14 | `RefreshAsync`; `POST /auth/refresh`; hàm làm mới; DM kết nối lại lấy token mới | Lifecycle: làm mới và dùng lại; Client: phối hợp làm mới, không khôi phục phiên đã đăng xuất; DM: token hết hạn, làm mới, đăng ký lại | Biên hạn phiên và độ lệch đồng hồ; mất phản hồi khi làm mới. Ca DM dùng cấu hình riêng, không phải bằng chứng v1 |
| UC-ACC-10 | AC-ACC-04, 16; TC-ACC-08, 14 | `GetSessionsAsync`, `RevokeSessionAsync`; `GET /auth/sessions`, `DELETE /auth/sessions/{id}`; tab phiên dùng API thật, có lỗi và thử lại | Lifecycle: đánh dấu phiên hiện tại, thu hồi phiên khác; DM: giao diện thu hồi đúng A2 và cắt kết nối cũ | Chưa đủ ca thu hồi phiên hiện tại và kiểm tra quyền sở hữu. Giao diện chưa có nút thu hồi riêng phiên hiện tại (dùng đăng xuất) |
| UC-ACC-11 | AC-ACC-04, 16; TC-ACC-08, 14 | `LogoutAsync`, `LogoutAllAsync`; hàm xóa phiên ở máy; giao diện ghi rõ gồm cả thiết bị này | Lifecycle: đăng xuất, đăng xuất mọi thiết bị; Client: đăng xuất giữa các tab không bị làm mới khôi phục; DM: đối chiếu phiên cũ | Lỗi mạng khi đăng xuất, token không nhận ra. Bằng chứng DM không áp cho tin phòng hay Media |

| Test | Đã kiểm tra | Còn thiếu |
|---|---|---|
| `Identity_v1_supports_the_complete_password_account_lifecycle` | Đăng ký, chặn đăng nhập trước xác minh, xác minh, đăng nhập bằng tên tài khoản, hồ sơ, làm mới và dùng lại, đặt lại và đổi mật khẩu, phiên, thu hồi, đăng xuất | Đăng nhập bằng email; đặt lại trước xác minh; biên thời gian; gửi lại; link hết hạn; xác minh đồng thời; email thật |
| `A_concurrent_login_cannot_survive_a_password_change`, `A_login_waiting_for_a_password_change_rechecks_the_password` | Hai thứ tự giữa đăng nhập và đổi hay đặt lại mật khẩu; mật khẩu và phiên cũ không dùng được | Không chứng minh nhiều tab hay realtime |
| `Parallel_wrong_passwords_are_counted_and_trigger_lockout` | Đếm đúng request sai đồng thời, khóa đăng nhập | Biên hết khóa; giới hạn theo nguồn |

**Realtime trong nền `900e226`:** [AuthenticatedSessionReader](../../../services/Modules/Identity/Infrastructure/Services/AuthenticatedSessionReader.cs), [SubscriptionReconciler](../../../services/Modules/Messaging/Realtime/SubscriptionReconciler.cs) và [RealtimeHubTests](../../../tests/SCDC.Api.Tests/Messaging/RealtimeHubTests.cs) kiểm tra kết nối cũ sau thu hồi, đăng xuất mọi thiết bị, đổi stamp, hết hạn, bị vô hiệu, chưa xác minh. Một số trạng thái dựng bằng SQL trên DB thử, không phải quy trình khóa tài khoản thật. [Hồ sơ DM](../../records/verification/messaging/DM-MVP.md) ghi agent chạy giao diện thu hồi đúng A2, đo thời gian cắt, và làm mới token khi hết hạn. [Kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md) chạy lại trên nền. Các kết quả này không xác nhận nghiệm thu tài khoản, tải v1 hay thu hồi quyền Community và Media.

Code: [AuthenticationService](../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [UserAccountService](../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [UserDirectory](../../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs), [AuthScreen](../../../clients/WebClient/src/components/AuthScreen.jsx), [UserSettingsModal](../../../clients/WebClient/src/components/UserSettingsModal.jsx), [api.js](../../../clients/WebClient/src/api.js).

Giới hạn tần suất (ACC-GAP-06) xem [liên kết email](email-links.md#status).

---

Nguồn: DEC-054 (ACC-002), DEC-064 (ACC-010), DEC-065 (ACC-011), DEC-083 (chat ≤ 5 giây), DEC-099 (media), DEC-091 (bản nháp).

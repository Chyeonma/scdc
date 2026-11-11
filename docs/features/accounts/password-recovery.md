# SCDC — Khôi phục và đổi mật khẩu

Người quên mật khẩu có thể đặt lại qua link gửi tới email, không cần đăng nhập. Người đã đăng nhập có thể đổi mật khẩu nếu biết mật khẩu hiện tại. Cả hai cách, khi thành công, đều đăng xuất mọi thiết bị và không tự xác minh email.

**Trạng thái:** API quên, đặt lại, đổi mật khẩu đã có trong nền `900e226`. Chưa có trang đặt lại mật khẩu; giao diện chưa đăng xuất ngay sau khi đổi mật khẩu. Đặt lại cho tài khoản chưa xác minh, thời gian chờ và gửi email thật nằm ở nhánh Identity riêng ([hiện trạng email](email-links.md#status)).

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-006 | Có luồng yêu cầu và thực hiện đặt lại mật khẩu qua email. |
| ACC-014 | Chỉ khôi phục qua email đã đăng ký, kể cả tài khoản chưa xác minh. v1 không có kênh khôi phục khác hay xử lý thủ công khi mất email. |
| ACC-016 | Đổi mật khẩu (đã đăng nhập): từ chối nếu mật khẩu mới trùng mật khẩu hiện tại. Đặt lại qua link: cho phép trùng, nhưng vẫn dùng hết token, đổi security stamp và đăng xuất mọi phiên. Không kiểm tra lịch sử mật khẩu. |

- Mật khẩu giữ nguyên, không cắt khoảng trắng; dài 8–128 (`.Length`), có ít nhất một chữ và một số ([ACC-010](sessions.md#rules)).
- Token, thời gian chờ, worker email và cách đưa token qua fragment URL: xem [ACC-012](email-links.md).
- MFA, mã khôi phục và đăng nhập bằng tài khoản ngoài để Identity v2.

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-05"></a>

<a id="uc-uc-acc-05--yêu-cầu-khôi-phục-mật-khẩu"></a>

### UC-ACC-05 — Yêu cầu đặt lại mật khẩu

**Ai làm:** người quên mật khẩu.

**Điều kiện trước:** có email hợp lệ để gửi yêu cầu; muốn dùng link thì phải truy cập được hộp thư đã đăng ký. Tài khoản chưa xác minh cũng được.

**Các bước:**

1. Người dùng nhập email ở màn hình Quên mật khẩu (ACC-S04) và gửi.
2. Nếu tài khoản đủ điều kiện và đã qua thời gian chờ 60 giây: hủy token đặt lại cũ, cấp token mới hạn 30 phút, đưa yêu cầu gửi email vào hàng.
3. Trả "đã tiếp nhận"; giao diện hướng dẫn kiểm tra email. Không tiết lộ email có tài khoản hay không, không hứa email đã được giao.

**Ngoại lệ:**

- Email không tồn tại, tài khoản không dùng được, hoặc đang trong thời gian chờ: cùng một phản hồi. Trong thời gian chờ không cấp token mới, link cũ vẫn dùng được.
- Hai yêu cầu đồng thời chỉ cấp tối đa một token.
- Dữ liệu sai: lỗi validation.

**Kết quả:** token đặt lại cũ hết hiệu lực (nếu có token mới); token xác minh email không bị ảnh hưởng. Mật khẩu, trạng thái xác minh và phiên chưa đổi. Giới hạn tần suất bổ sung vẫn hoãn (DEC-089).

<a id="uc-uc-acc-06"></a>

<a id="uc-uc-acc-06--đặt-lại-mật-khẩu-qua-liên-kết"></a>

### UC-ACC-06 — Đặt lại mật khẩu qua link

**Ai làm:** người truy cập được hộp thư của tài khoản.

**Điều kiện trước:** có link đặt lại; không cần đăng nhập. Token đúng mục đích, còn hạn, chưa dùng, chưa bị thay; tài khoản đủ điều kiện.

**Các bước:**

1. Mở link (mở link không dùng token). Nhập mật khẩu mới và gửi.
2. Kiểm tra mật khẩu mới hợp lệ trước. Sau đó, dưới khóa, kiểm tra lại token; đổi mật khẩu, đổi security stamp, xóa khóa đăng nhập tạm và đăng xuất mọi phiên.
3. Đánh dấu token đã dùng, commit tất cả cùng lúc. Giao diện xóa phiên và dữ liệu đệm, hướng dẫn đăng nhập lại.

**Ngoại lệ:**

- Mật khẩu không hợp lệ: báo lỗi, token vẫn dùng được để thử lại.
- Token sai, hết hạn, đã dùng hoặc bị thay: một lỗi chung kèm đường yêu cầu link mới.
- Hai lần dùng cùng token: chỉ một lần thành công.
- Nếu trước đó đã đổi mật khẩu (UC-ACC-08), token đặt lại cũ không dùng được nữa.
- Mật khẩu mới trùng mật khẩu cũ: được chấp nhận; token vẫn bị dùng và mọi phiên vẫn bị đăng xuất.

**Kết quả:** mật khẩu mới có hiệu lực, mọi phiên cũ bị đăng xuất, không tự đăng nhập. Trạng thái xác minh email không đổi; tài khoản chưa xác minh vẫn phải làm UC-ACC-02.

<a id="uc-uc-acc-08"></a>

<a id="uc-uc-acc-08--đổi-mật-khẩu-khi-đã-đăng-nhập"></a>

### UC-ACC-08 — Đổi mật khẩu khi đã đăng nhập

**Ai làm:** người đã đăng nhập và biết mật khẩu hiện tại.

**Điều kiện trước:** phiên hợp lệ; tài khoản hoạt động và đã xác minh.

**Các bước:**

1. Mở màn hình Đổi mật khẩu (ACC-S07), nhập mật khẩu hiện tại và mật khẩu mới (hợp lệ, khác mật khẩu hiện tại).
2. Trong một transaction: kiểm tra mật khẩu hiện tại, đổi mật khẩu và security stamp, xóa khóa đăng nhập tạm, hủy token đặt lại đang có, đăng xuất mọi phiên.
3. Giao diện xóa phiên và dữ liệu đệm, chuyển về đăng nhập.

**Ngoại lệ:**

- Sai mật khẩu hiện tại hoặc mật khẩu mới không hợp lệ: báo lỗi; không đổi gì.
- Mật khẩu mới trùng hiện tại: `Identity.PasswordUnchanged`; không đổi gì.
- Đăng nhập hay làm mới token chạy cùng lúc không được giữ lại phiên dùng mật khẩu cũ sau khi commit.

**Kết quả:** mật khẩu mới có hiệu lực; mọi thiết bị, kể cả thiết bị đang dùng, phải đăng nhập lại. Token đặt lại cấp trước đó không dùng được. Trạng thái xác minh email không đổi.

<a id="ux"></a>

## Giao diện

| Màn hình | Trạng thái | Phản hồi |
|---|---|---|
| ACC-S04 Quên mật khẩu | Nhập email, đang gửi, đã tiếp nhận | Không tiết lộ email có tài khoản hay không; không hứa email đã được giao |
| ACC-S05 Đặt lại mật khẩu | Link hợp lệ hoặc không; nhập mật khẩu mới; hoàn tất | Không hiện token. Thành công thì chuyển tới đăng nhập; chưa xác minh thì vẫn phải xác minh |
| ACC-S07 Đổi mật khẩu | Đang nhập, đang lưu, sai mật khẩu hiện tại, thành công | Thành công thì xóa phiên và về đăng nhập |

Trang đặt lại chỉ gửi POST khi người dùng bấm gửi mật khẩu mới hợp lệ; không dùng token khi mở trang (GET). Link sai, hết hạn hay đã dùng đều hiện cùng một thông báo kèm nút yêu cầu link mới.

<a id="contracts"></a>

## API và dữ liệu

Tiền tố `/api/v1`. Các endpoint dưới đã có trong nền `900e226`.

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `POST /auth/forgot-password` | Không cần đăng nhập; `email` | 202 `PasswordResetRequestedResponse` (`accepted, developmentResetToken`) |
| `POST /auth/reset-password` | Không cần đăng nhập; `token, newPassword` | 204 |
| `POST /auth/change-password` | Bearer; `currentPassword, newPassword` | 204; đăng xuất mọi phiên |

| Mã lỗi | HTTP | Xử lý |
|---|---|---|
| `Identity.InvalidOrExpiredToken` | 400 | Link không dùng được |
| `Identity.RegistrationInvalid`, `Identity.PasswordInvalid`, `Identity.ProfileInvalid` | 400 | Hiện lỗi theo trường. Validation của MVC có thể trả `Common.ValidationFailed` trước |
| `Identity.CurrentPasswordInvalid`, `Identity.PasswordUnchanged` | 400 | Báo ở trường mật khẩu; không đăng xuất khi đổi bị từ chối |

| Bảng | Nội dung |
|---|---|
| `identity.password_credentials` | Hash mật khẩu, phiên bản, thời điểm đổi; không lưu mật khẩu thô |
| `identity.user_security_states` | Số lần sai, thời hạn khóa đăng nhập, security stamp |
| `identity.auth_sessions`, `refresh_tokens` | Phiên, thu hồi và chuỗi làm mới token; chỉ lưu hash token |
| `identity.account_tokens` | Hash token, mục đích, email đích, hạn, thời điểm dùng. Token xác minh và đặt lại độc lập |

**Đặt lại:** kiểm tra mật khẩu trước; khóa dòng người dùng, đọc lại token (đúng mục đích, đúng email, còn hạn, còn hiệu lực); rồi trong cùng transaction đổi hash và stamp, xóa khóa đăng nhập tạm, đánh dấu token đã dùng, đăng xuất mọi phiên.

**Đổi mật khẩu:** khóa người dùng trước, rồi mới tới mật khẩu, phiên và token; thành công thì hủy token đặt lại cũ.

Đăng nhập hay làm mới token tranh khóa không được giữ phiên dùng stamp cũ sau commit. Thiết kế đầy đủ việc dùng token và email xem [liên kết email](email-links.md#token-transaction).

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-06 | Tài khoản đã xác minh yêu cầu đặt lại mật khẩu và hoàn tất | Đăng nhập được bằng mật khẩu mới; link đã dùng không dùng lại được |
| AC-ACC-10 | Tài khoản chưa xác minh đặt lại mật khẩu | Mật khẩu mới có hiệu lực nhưng vẫn chưa vào được ứng dụng cho tới khi xác minh |
| AC-ACC-18 | Người mất truy cập email tìm cách khôi phục | Chỉ có luồng qua email đã đăng ký; trợ giúp không hứa kênh khác |
| AC-ACC-22 | Đổi và đặt lại với mật khẩu mới trùng mật khẩu hiện tại | Đổi: bị từ chối (`Identity.PasswordUnchanged`), không đổi gì. Đặt lại: thành công, dùng hết token, đổi stamp, đăng xuất mọi phiên. Trạng thái xác minh không đổi |

Ký hiệu: A là tài khoản đã xác minh, U chưa xác minh, A1 và A2 là hai phiên của A. Dùng DB và email thử riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-03 | U đặt lại mật khẩu, đăng nhập trước và sau khi xác minh | Đặt lại không tự xác minh; chỉ sau xác minh mới vào được | AC-ACC-10 |
| TC-ACC-19 | A có A1, A2 và một token đặt lại. A1 đổi sang chính mật khẩu hiện tại; rồi dùng token đặt lại về chính mật khẩu đó. Lặp với U sau khi xong ACC-GAP-01 | Đổi: 400 `Identity.PasswordUnchanged`, không gì thay đổi. Đặt lại: 204, stamp đổi, A1, A2 và refresh token cũ bị từ chối, token không dùng lại được; vẫn đăng nhập được bằng mật khẩu đó. U vẫn chưa xác minh. Chưa chạy, chưa có test tự động | AC-ACC-22 |

Ca khác: TC-ACC-15 (trợ giúp khi mất email) ở [hồ sơ](profile.md#acceptance); TC-ACC-06, 07, 12, 13, 16, 17, 18 và AC-ACC-14, 15, 19, 20, 21 ở [liên kết email](email-links.md#acceptance).

Kết quả phải ghi kèm commit và build.

<a id="status"></a>

## Hiện trạng

Đối chiếu với [nền MVP `900e226`](../../releases/mvp.md) ngày 2026-10-13. Bảng test Identity giữ phần rà soát ngày 2026-10-04 và 05. Lần cập nhật này không chạy lại test. Phản hồi 200 hay 204 chưa chứng minh mọi kết quả sau đó.

| UC | Liên quan | Code hiện có | Test tự động | Còn thiếu |
|---|---|---|---|---|
| UC-ACC-05 | AC-ACC-10, 14, 18, 19, 21; TC-ACC-03, 07, 12, 15, 16, 18 | `ForgotPasswordAsync`; `POST /auth/forgot-password`; form quên mật khẩu | Trả "đã tiếp nhận" và token cho tài khoản đã xác minh | ACC-GAP-01, 02, 05. Chưa cấp token cho tài khoản chưa xác minh; chưa có thời gian chờ, email thật, test phản hồi cho các trạng thái |
| UC-ACC-06 | AC-ACC-06, 10, 12, 15, 16, 20, 22; TC-ACC-03, 06, 10, 13, 14, 17, 18, 19 | `ResetPasswordAsync`; `POST /auth/reset-password`; hàm gọi API | Đặt lại 204 với mật khẩu khác; access token cũ bị từ chối; mật khẩu cũ không đăng nhập được; `Concurrent_reset_requests_can_consume_a_token_only_once` | Chưa có trang đặt lại. Thiếu test cho: tài khoản chưa xác minh, hạn token, mật khẩu sai quy tắc vẫn giữ token, token xác minh và đặt lại độc lập, mật khẩu trùng |
| UC-ACC-08 | AC-ACC-12, 16, 17, 22; TC-ACC-10, 14, 17, 19 | `ChangePasswordAsync`; `POST /auth/change-password`; form đổi mật khẩu | Đổi 204, access token cũ bị từ chối; `Changing_password_invalidates_previously_issued_reset_tokens`; hai test đăng nhập và đổi mật khẩu tranh khóa | Giao diện chưa đăng xuất ngay; thiếu test sai mật khẩu hiện tại, sai quy tắc, trùng |

| Mã | Yêu cầu | Code hiện tại | Cần làm |
|---|---|---|---|
| ACC-GAP-01 | Khôi phục trước khi xác minh (ACC-007, 014, AC-ACC-10) | `ForgotPasswordAsync` trong `900e226` không tạo token khi `VerifiedAt` là null; nhánh Identity đã sửa ([email](email-links.md#status)) | Ghép nhánh Identity; chứng minh đặt lại không tự xác minh (TC-ACC-03) |

| Test hiện có | Đã kiểm tra | Còn thiếu |
|---|---|---|
| `Changing_password_invalidates_previously_issued_reset_tokens` | Đổi mật khẩu hủy token đặt lại cũ | Token xác minh phải độc lập; email đến trễ |
| `Concurrent_reset_requests_can_consume_a_token_only_once` | Chỉ một lần đặt lại thành công với cùng token | Xác minh đồng thời; gửi lại email |

ACC-GAP-02 và 05 (thời gian chờ, email) xem [liên kết email](email-links.md#status).

---

Nguồn: DEC-042 (ACC-006), DEC-051, DEC-067 (ACC-014), DEC-089 (hoãn giới hạn tần suất), DEC-113 (ACC-016, chốt 2026-10-05).

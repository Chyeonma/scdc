# SCDC — Đăng ký và xác minh email

Người dùng tạo tài khoản bằng email, tên tài khoản (username), tên hiển thị và mật khẩu. Tài khoản mới ở trạng thái chờ xác minh; người dùng bấm link trong email để xác minh rồi mới đăng nhập được.

**Trạng thái:** API đăng ký và xác minh đã có trong nền `900e226`. Giao diện hiện tự xác minh bằng token Development thay vì email thật; chưa có trang mở link xác minh. Gửi lại email xác minh nằm ở nhánh Identity riêng ([hiện trạng email](email-links.md#status)).

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-001 | Đăng ký cần: email, tên tài khoản, tên hiển thị và mật khẩu. |
| ACC-008 | **Tên tài khoản:** 3–32 ký tự, chỉ gồm chữ ASCII, số, `_` và `.`; duy nhất (không phân biệt hoa thường); không đổi được. **Tên hiển thị:** Unicode, 1–64 đơn vị UTF-16 sau khi cắt khoảng trắng hai đầu, không được chỉ có khoảng trắng; được trùng với người khác. |
| ACC-009 | **Email:** cắt khoảng trắng hai đầu, chuyển chữ thường để kiểm tra trùng; không đổi được trong v1. Không bỏ dấu chấm hay phần `+tag` (không áp quy tắc riêng của nhà cung cấp email nào). |

- Email không công khai.
- Mật khẩu theo [ACC-010](sessions.md#rules). Link xác minh và đặt lại theo [ACC-012](email-links.md).
- Tài khoản chưa xác minh chỉ dùng được xác minh và khôi phục, chưa có phiên ứng dụng ([ACC-005, 007](account-state.md#rules)).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-01"></a>

<a id="uc-uc-acc-01--đăng-ký-tài-khoản"></a>

### UC-ACC-01 — Đăng ký

**Ai làm:** người chưa có tài khoản. Không cần đăng nhập.

**Các bước:**

1. Người dùng mở màn hình Đăng ký (ACC-S01), nhập email, tên tài khoản, tên hiển thị, mật khẩu rồi gửi.
2. Hệ thống chuẩn hóa và kiểm tra dữ liệu (ACC-008 đến 010). Tạo tài khoản chờ xác minh, hồ sơ, mật khẩu đã băm, và yêu cầu gửi link xác minh, tất cả trong một transaction.
3. Trả kết quả; giao diện hướng dẫn kiểm tra email, có nút gửi lại email và nút quên mật khẩu.

**Ngoại lệ:**

- Dữ liệu sai: lỗi theo trường. Tên tài khoản hoặc email trùng: lỗi xung đột.
- Hai người đăng ký cùng tên cùng lúc: chỉ tạo một tài khoản. Lỗi không để lại tài khoản dở dang.
- Đưa yêu cầu email vào hàng chưa có nghĩa email đã tới; nếu không nhận được thì dùng UC-ACC-03.

**Kết quả:** tài khoản ở trạng thái `PendingVerification`, chưa có phiên. Link xác minh dùng một lần, hạn 30 phút; lần gửi đầu cũng tính thời gian chờ gửi lại. Giao diện sản phẩm không được tự xác minh bằng token Development.

<a id="uc-uc-acc-02"></a>

<a id="uc-uc-acc-02--xác-minh-email"></a>

### UC-ACC-02 — Xác minh email

**Ai làm:** người truy cập được hộp thư đã đăng ký. Không cần đăng nhập.

**Các bước:**

1. Người dùng mở link. Trang hiện hướng dẫn; chỉ mở trang (GET) chưa thay đổi gì.
2. Người dùng bấm Xác minh. Hệ thống kiểm tra token: đúng mục đích, đúng email, còn hạn, chưa dùng; tài khoản đủ điều kiện. Rồi đánh dấu token đã dùng.
3. Email được xác minh, tài khoản chuyển sang hoạt động; giao diện hướng dẫn đăng nhập.

**Ngoại lệ:**

- Token sai mục đích, hết hạn, đã dùng hoặc bị thay: báo link không dùng được, kèm nút yêu cầu link mới.
- Hai lần bấm cùng token: chỉ một lần thành công. Xác minh và gửi lại theo thứ tự commit.
- Mất phản hồi sau khi đã xác minh: bấm lại sẽ bị từ chối; người dùng thử đăng nhập hoặc yêu cầu link mới.
- Xác minh không mở khóa tài khoản đang bị khóa (DEC-104).

**Kết quả:** email đã xác minh, token không dùng lại được, chưa có phiên đăng nhập. Token đặt lại mật khẩu (nếu có) không bị ảnh hưởng. Token không hiện trên màn hình; cách đọc và xóa token khỏi URL xem [liên kết email](email-links.md#contracts).

<a id="uc-uc-acc-03"></a>

<a id="uc-uc-acc-03--yêu-cầu-gửi-lại-xác-minh"></a>

### UC-ACC-03 — Gửi lại email xác minh

**Ai làm:** người chưa xác minh xong. Không cần đăng nhập; cần nhập email hợp lệ.

**Các bước:**

1. Người dùng nhập email ở màn hình Xác minh (ACC-S03) và bấm gửi lại.
2. Nếu tài khoản đủ điều kiện và đã cách lần gửi trước ít nhất 60 giây: trong một transaction, hủy token xác minh cũ, cấp token mới hạn 30 phút, đưa email vào hàng.
3. Trả `202 {accepted:true}`; giao diện hướng dẫn kiểm tra email. Không xác nhận email đã tới hay tài khoản có tồn tại.

**Ngoại lệ:**

- Email không tồn tại, đã xác minh, tài khoản không đủ điều kiện, hoặc đang trong thời gian chờ: cùng một phản hồi. Trong thời gian chờ không cấp token mới và link cũ vẫn dùng được.
- Hai yêu cầu đồng thời: tối đa một token mới. Không tiết lộ thời gian chờ còn lại.
- Email cũ đến muộn vẫn chứa link đã bị hủy.
- Email sai định dạng: lỗi validation. Giới hạn tần suất bổ sung vẫn hoãn (DEC-089).

**Kết quả:** nếu cấp mới, chỉ link mới dùng được; token đặt lại mật khẩu không bị ảnh hưởng. Nếu không cấp, token giữ nguyên. Người dùng vẫn chưa có phiên.

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
│ [Tạo tài khoản]                │
│ Đã có tài khoản? [Đăng nhập]   │
└────────────────────────────────┘
```

| Màn hình | Trạng thái | Phản hồi |
|---|---|---|
| ACC-S01 Đăng ký | Đang nhập, dữ liệu sai, đang gửi, thất bại | Giữ email, tên tài khoản, tên hiển thị đã nhập; lỗi hiện ở đúng trường; chặn bấm gửi lặp |
| ACC-S03 Xác minh email | Đã tiếp nhận, mở link, đang xác minh, thành công, link không dùng được | Hướng dẫn kiểm tra email; nút gửi lại (cách nhau tối thiểu 60 giây); link mới hủy link cũ; không hứa email đã tới |

- ACC-S03 là nơi tài khoản chưa xác minh dừng lại; có đường tới khôi phục mật khẩu.
- Link được đọc và xóa khỏi URL, chỉ dùng khi người dùng bấm ([liên kết email](email-links.md#browser-links)).
- Hỗ trợ máy tính và điện thoại (DEC-059, DEC-082). Prototype, thiết bị và tiêu chí truy cập còn mở (OQ-007).

<a id="contracts"></a>

## API và dữ liệu

Tiền tố `/api/v1`. Swagger: `/swagger/v1/swagger.json` khi chạy Development.

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `POST /auth/register` | Không cần đăng nhập; `username, displayName, email, password` | 201 `RegistrationResponse`; chưa cấp phiên |
| `POST /auth/verify-email` | Không cần đăng nhập; `token` | 204 |

`RegistrationResponse`: `userId, username, email, verificationRequired, developmentVerificationToken`. Token Development chỉ trả về khi bật `ExposeDevelopmentTokens`.

<a id="api-hợp-đồng-gửi-lại-cần-triển-khai"></a>

### API gửi lại (chưa có trong nền)

`POST /api/v1/auth/resend-verification` chưa có trong `900e226`; đã làm ở nhánh Identity nhưng chưa ghép ([hiện trạng email](email-links.md#status)).

- **Đầu vào:** `{email}` hợp lệ.
- **Thành công:** `202 {accepted: true}`, giống nhau cho mọi trường hợp (có tài khoản, không có, đã xác minh, không đủ điều kiện).
- **Xử lý:** với tài khoản chờ xác minh đủ điều kiện, trong một transaction: khóa tài khoản, kiểm tra thời gian chờ, hủy token cũ, tạo token mới và outbox. Không đụng token đặt lại mật khẩu.
- Thời gian chờ theo email phải cho cùng phản hồi ở mọi trạng thái, để không lộ email có tồn tại hay không. Giới hạn theo nguồn (IP) có thể trả 429 kèm `Retry-After`.
- Không trả token ngoài môi trường Development.
- Schema, mã lỗi và ngưỡng bổ sung cần review.

### Bảng

| Bảng | Nội dung |
|---|---|
| `identity.users` | ID cố định, tên tài khoản, trạng thái; tên tài khoản (đã chuẩn hóa) duy nhất |
| `identity.user_profiles` | Một hồ sơ mỗi người dùng |
| `identity.user_emails` | Email (đã chuẩn hóa) duy nhất; tối đa một email chính; `verified_at` tách khỏi mật khẩu |
| `identity.password_credentials` | Hash mật khẩu, phiên bản, thời điểm đổi |
| `identity.account_tokens` | Hash token, mục đích, email đích, hạn, thời điểm dùng; token xác minh và đặt lại độc lập |

Đăng ký ghi người dùng, hồ sơ, email, mật khẩu, token và yêu cầu email trong một transaction. Ràng buộc unique trên tên tài khoản và email xử lý cả trường hợp đăng ký đồng thời. Lần cấp token đầu ghi mốc thời gian chờ. Cách khóa và dùng token xem [liên kết email](email-links.md#token-transaction).

### Mã lỗi

| Mã | HTTP | Xử lý |
|---|---|---|
| `Identity.UsernameAlreadyExists`, `Identity.EmailAlreadyExists`, `Identity.RegistrationConflict` | 409 | Báo trùng |
| `Identity.InvalidOrExpiredToken` | 400 | Link không dùng được |
| `Identity.RegistrationInvalid`, `Identity.PasswordInvalid`, `Identity.ProfileInvalid` | 400 | Hiện lỗi theo trường. Validation MVC có thể trả `Common.ValidationFailed` trước |

### Chuẩn hóa

Thứ tự: chuẩn hóa → kiểm tra → transaction → ràng buộc DB; HTTP, service và DB dùng cùng quy tắc.

- Email: cắt khoảng trắng, chuyển chữ thường, tối đa 254 ký tự.
- Tên tài khoản: cắt khoảng trắng, rồi kiểm tra ký tự ASCII và tính duy nhất.
- Tên hiển thị: cắt khoảng trắng, từ chối nếu chỉ có khoảng trắng, đếm UTF-16.
- Mật khẩu: không cắt, không chuẩn hóa.
- Ngôn ngữ và múi giờ: chỉ kiểm tra độ dài; giới hạn theo danh sách giá trị cần quyết định riêng.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-01 | Người chưa có tài khoản điền thông tin hợp lệ (gồm tên hiển thị) | Tài khoản được tạo, chờ xác minh email |
| AC-ACC-05 | Tài khoản chưa xác minh thử gửi tin; sau đó xác minh rồi thử lại | Trước xác minh bị từ chối; sau xác minh gửi được theo quyền |
| AC-ACC-09 | Hai người đăng ký cùng tên tài khoản cùng lúc | Chỉ một tài khoản được tạo; người kia nhận lỗi trùng; không có dữ liệu dở dang |
| AC-ACC-11 | Đăng ký hoặc sửa hồ sơ với giá trị ở biên độ dài, khác hoa thường, có khoảng trắng hai đầu | Tên tài khoản và email chuẩn hóa và kiểm tra trùng giống nhau; tên hiển thị đếm UTF-16; không đổi được tên tài khoản và email |

Ký hiệu: A là tài khoản đã xác minh, U chưa xác minh. Dùng DB và email thử riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-01 | Đăng ký hợp lệ; sau xác minh đăng nhập bằng email và bằng tên tài khoản | Cả hai cách vào cùng một tài khoản; tên tài khoản không đổi | AC-ACC-01, 02 |
| TC-ACC-04 | Hai request đăng ký cùng tên tài khoản đồng thời | Một tài khoản; request kia lỗi trùng; không có dữ liệu dở dang | AC-ACC-09 |
| TC-ACC-09 | Tên tài khoản dài 2, 3, 32, 33; tên hiển thị 0, 1, 64, 65 đơn vị UTF-16; hai email hoặc tên chỉ khác hoa thường; khoảng trắng hai đầu | Chuẩn hóa và kiểm tra nhất quán; chỉ một định danh; không có dữ liệu dở dang | AC-ACC-11 |

Ca về link, email và thời gian chờ xem [liên kết email](email-links.md#acceptance); truy cập khi chưa xác minh xem [trạng thái tài khoản](account-state.md#acceptance).

Kết quả phải ghi kèm commit và build.

<a id="status"></a>

## Hiện trạng

Đối chiếu với [nền MVP `900e226`](../../releases/mvp.md) ngày 2026-10-13. Bảng test Identity giữ phần rà soát ngày 2026-10-04 và 05. Lần cập nhật này không chạy lại test. Phản hồi 200 hay 204 chưa chứng minh mọi kết quả sau đó.

| UC | Liên quan | Code hiện có | Test tự động | Còn thiếu |
|---|---|---|---|---|
| UC-ACC-01 | AC-ACC-01, 09, 11, 12, 19; TC-ACC-01, 04, 09, 10, 16 | `RegisterAsync`; `POST /auth/register`; form đăng ký | Trả 201, có token Development, chặn đăng nhập trước khi xác minh | ACC-GAP-04, 05, 07. Chưa có test đăng ký trùng đồng thời, Unicode, quy tắc mật khẩu. Giao diện đang tự xác minh bằng token Development |
| UC-ACC-02 | AC-ACC-05, 07, 15, 19, 20; TC-ACC-01, 02, 06, 13, 17, 18 | `VerifyEmailAsync`; `POST /auth/verify-email`; hàm gọi API | Xác minh 204, đăng nhập được sau đó, `emailVerified=true` | ACC-GAP-03, 05. Chưa có test xác minh đồng thời, hết hạn, dùng lại; chưa có trang mở link |
| UC-ACC-03 | AC-ACC-14, 19, 20, 21; TC-ACC-07, 12, 16, 17, 18 | Chưa có trong nền; có ở nhánh Identity ([email](email-links.md#status)) | Test nằm ở nhánh Identity chưa ghép | ACC-GAP-02, 05. Cần ghép endpoint, thời gian chờ, email; làm giao diện và kiểm thử |

| Mã | Yêu cầu | Code hiện tại | Cần làm |
|---|---|---|---|
| ACC-GAP-04 | Chuẩn hóa định danh nhất quán (ACC-008, 009) | Service cắt khoảng trắng, nhưng MVC kiểm tra regex và độ dài trước service | Chốt thứ tự chuẩn hóa và kiểm tra ở HTTP, service, DB; fixture khoảng trắng và hoa thường |
| ACC-GAP-07 | Tên hiển thị đếm UTF-16 (ACC-008, DEC-068) | Service dùng `.Length`; MVC và DB có giới hạn riêng | Fixture tiếng Việt tổ hợp, emoji, biên 64; kiểm tra từ HTTP tới DB |

ACC-GAP-02, 03, 05 và việc chọn nhà cung cấp email, domain, kho khóa xem [liên kết email](email-links.md#status).

Code: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthScreen](../../../clients/WebClient/src/components/AuthScreen.jsx), [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs).

---

Nguồn: REQ-010, SCP-002, DEC-054, DEC-062 (thông tin đăng ký), DEC-063 (định danh), DEC-068 (UTF-16), DEC-059, DEC-082 (thiết bị), DEC-089 (hoãn giới hạn tần suất), DEC-104 (khóa tài khoản).

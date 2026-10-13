# SCDC — Hồ sơ và định danh công khai

Phạm vi UC-ACC-07. Identity sở hữu hồ sơ riêng; module khác nhận projection ID/username/displayName qua Contracts. Tên hiển thị không là khóa đăng nhập hoặc khóa hội thoại.

## Mục lục

- [Quy tắc](#rules)
- [Hành trình](#flows)
- [Giao diện, API và dữ liệu](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-003 | Người đã đăng nhập sửa tên hiển thị, giới thiệu ngắn, ngôn ngữ và múi giờ của mình; tên tài khoản/email không cho đổi; email không công khai. | DEC-054, DEC-063, DEC-066 |
| ACC-004 | API cần nhận diện tài khoản bằng định danh ổn định, không dùng tên hiển thị làm khóa. | Đề xuất kỹ thuật để đáp ứng tìm kiếm/chọn đúng người |
| ACC-013 | Hồ sơ cho sửa `displayName`, `bio`, `locale`, `timezone`; bio tối đa 500 theo phép đếm hiện tại, locale 1–16 và timezone 1–64. v1 chưa thêm tải ảnh đại diện. | DEC-066; các giới hạn giữ theo API hiện tại |

Tên hiển thị là trường tìm người theo DEC-019, trim, dài 1–64 UTF-16 và được trùng; username/email không đổi theo [ACC-008/009](registration-verification.md#rules). ACC-P05: hồ sơ ACC-013, projection công khai chỉ ID/username/displayName, khôi phục chỉ qua email (DEC-063/066/067). Quyền ACL-01 của chủ tài khoản ở [account-state](account-state.md#permissions).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-07"></a>

<a id="uc-uc-acc-07--xem-và-sửa-hồ-sơ-riêng"></a>

### UC-ACC-07 — Xem và sửa hồ sơ riêng

**Tác nhân:** Người đã đăng nhập.

**Điều kiện trước:** Phiên của chính tài khoản còn hạn/chưa thu hồi; tài khoản active và đã xác minh.

**Luồng chính:**

1. Người dùng mở ACC-S06; hệ thống tải hồ sơ riêng, gồm username/email chỉ đọc.
2. Người dùng sửa `displayName`, `bio`, `locale`, `timezone` và gửi lưu.
3. Hệ thống kiểm tra dữ liệu theo ACC-008/013, cập nhật hồ sơ của chính người gọi và trả hồ sơ đã lưu; giao diện cập nhật nội dung hiển thị.

**Ngoại lệ:** Dữ liệu sai trả lỗi trường; lưu lỗi giữ bản đang sửa. Phiên không hợp lệ yêu cầu đăng nhập; không lấy user ID do người gọi cung cấp để sửa hồ sơ người khác. Các tên hiển thị trùng nhau được phép; việc chọn người trong DM dùng ID/username ổn định. Quy tắc chuẩn hóa và UTF-16 phải nhất quán HTTP–service–DB.

**Kết quả sau cùng:** Các trường hợp lệ được lưu; username/email giữ nguyên. Hồ sơ công khai chỉ có ID/username/tên hiển thị, không có email. v1 chưa có thao tác tải avatar; chưa áp dụng allowlist locale/timezone hoặc kiểm soát version mới khi chưa chốt contract.

<a id="contracts"></a>

## Giao diện, API và dữ liệu

| Màn hình | Trạng thái và thao tác | Phản hồi/kết quả |
|---|---|---|
| ACC-S06 Hồ sơ | Tải/lưu/lỗi; dữ liệu không hợp lệ | Giữ bản đang sửa khi lưu lỗi; tên tài khoản/email chỉ đọc; các trường sửa theo ACC-013 |

| Method / đường dẫn | Xác thực và đầu vào JSON | Kết quả thành công |
|---|---|---|
| `GET /users/me` | Bearer | 200 `UserAccountResponse` riêng tư |
| `PATCH /users/me` | Bearer; `displayName, bio?, locale, timezone` | 200 hồ sơ sau cập nhật |

- `UserAccountResponse`: `id, username, displayName, email, emailVerified, status, bio, avatarObjectKey, locale, timezone, createdAt, updatedAt, version`.
- `SessionResponse`: `id, deviceName, userAgent, lastSeenIp, createdAt, lastSeenAt, expiresAt, isCurrent`.

Email nằm trong hồ sơ riêng `users/me`, không phải hồ sơ công khai cho tìm kiếm. Token Development chỉ có giá trị khi `ExposeDevelopmentTokens` bật; không sử dụng chúng làm giao diện sản phẩm.

| Bảng | Trách nhiệm và ràng buộc |
|---|---|
| `identity.users` | ID ổn định, username và trạng thái; unique username chuẩn hóa |
| `identity.user_profiles` | Một hồ sơ mỗi user; tên hiển thị không phải khóa đăng nhập |
| `identity.user_emails` | Unique email chuẩn hóa; tối đa một email primary; `verified_at` tách khỏi mật khẩu |

Module khác lấy `UserSummary(id, username, displayName)` qua `IUserDirectory`; interface hiện hỗ trợ tìm ID, username chính xác và nhiều ID, chưa có tìm một phần tên. Hồ sơ công khai không trả email, mật khẩu, security stamp hoặc thông tin phiên.

Nguồn đối chiếu: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [IdentityModule](../../../services/Modules/Identity/IdentityModule.cs), [IdentityDbContext](../../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs), [IUserDirectory](../../../services/SCDC.Contracts/Identity/IUserDirectory.cs).

<a id="normalization-hồ-sơ-và-chuẩn-hóa-khi-bàn-giao"></a>

### Hồ sơ và chuẩn hóa khi bàn giao

Chốt pipeline normalize → validate → transaction → constraint; validation HTTP không chạy bộ quy tắc khác service. Username/email unique ở DB xử lý cả đăng ký đồng thời. DisplayName trim/UTF-16 như ACC-008; bio hiện trim và rỗng thành null; locale/timezone giữ validation độ dài hiện tại, chưa tự thêm allowlist locale/zone chỉ vì có giá trị trông hợp lệ. Source đang dùng version hồ sơ ở response; cập nhật optimistic concurrency là hướng thiết kế, chưa thêm `expectedVersion` vào API hiện có khi chưa thay contract.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-03 | Hai tài khoản có cùng tên hiển thị xuất hiện trong tìm kiếm. | Người tìm chọn được đúng tài khoản qua định danh phân biệt. |
| AC-ACC-08 | Người dùng xem/sửa hồ sơ; người khác tìm tài khoản đó. | Đổi được tên hiển thị; không đổi được tên tài khoản; kết quả công khai không có email. |
| AC-ACC-17 | Sửa các trường hồ sơ đã chốt; thử sửa username/email hoặc tải avatar. | Tên hiển thị/bio/locale/timezone cập nhật hợp lệ; v1 không cung cấp thao tác đổi định danh hoặc tải avatar; không tuyên bố tính năng chỉ từ cột SQL. |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-05 | A đổi tên hiển thị; B tìm tên đó; thử đổi tên tài khoản | Tên hiển thị mới xuất hiện; tên tài khoản giữ nguyên; dữ liệu công khai không có email | AC-ACC-03, AC-ACC-08 |
| TC-ACC-15 | Sửa từng trường hồ sơ và dữ liệu vượt giới hạn; xem thông tin trợ giúp khi mất email | Trường hợp lệ lưu được; định danh không đổi; không hứa khôi phục khác email | AC-ACC-17/18 |

AC-ACC-11/TC-ACC-09 kiểm chứng biên displayName và định danh ở [đăng ký](registration-verification.md#acceptance). Tìm người một phần tên thuộc DM, không suy từ interface lookup hiện có.

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source/test ngày 2026-10-04/05; lần tổ chức tài liệu này không chạy lại test hoặc xác nhận nghiệm thu. Trong bảng, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`. Assertion 200/204 chưa chứng minh mọi hậu điều kiện.

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-07 | AC-03/08/11/17; TC-05/09/15 | `GetAsync`, `UpdateProfileAsync`; `GET/PATCH /users/me`; `UserDirectory`; form hồ sơ | Lifecycle: đọc username/emailVerified và PATCH trả 200 | Chưa assertion giá trị hồ sơ sau lưu/quyền riêng tư/biên UTF-16; UI chưa cho sửa locale hoặc xem email chỉ đọc; tìm người DM thuộc scope tích hợp |

ACC-GAP-07 được định nghĩa tại [đăng ký/xác minh](registration-verification.md#status); cần cùng fixture UTF-16 cho register/profile. UI chưa cho sửa locale hoặc xem email chỉ đọc; chưa assertion giá trị sau lưu/quyền riêng tư/biên UTF-16.

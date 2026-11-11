# SCDC — Hồ sơ người dùng

Người dùng xem và sửa hồ sơ của mình: tên hiển thị, giới thiệu, ngôn ngữ, múi giờ. Người khác chỉ thấy ID, username và tên hiển thị.

**Trạng thái:** API đọc và sửa hồ sơ đã có trong nền `900e226`. Giao diện chưa cho sửa ngôn ngữ và chưa hiện email (chỉ đọc). Chưa có test kiểm tra giá trị sau khi lưu.

Identity sở hữu hồ sơ. Module khác chỉ nhận ID, username và tên hiển thị qua hợp đồng (Contracts). Tên hiển thị không dùng để đăng nhập hay làm khóa cuộc trò chuyện.

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-003 | Người đã đăng nhập sửa được tên hiển thị, giới thiệu, ngôn ngữ và múi giờ của mình. Không đổi được username và email. Email không công khai. |
| ACC-004 | API nhận diện tài khoản bằng ID cố định, không dùng tên hiển thị. |
| ACC-013 | Các trường sửa được: `displayName`, `bio` (tối đa 500), `locale` (1–16 ký tự), `timezone` (1–64 ký tự). v1 chưa có tải ảnh đại diện. |

- Tên hiển thị dùng để tìm người, được cắt khoảng trắng hai đầu, dài 1–64 đơn vị UTF-16, và **được phép trùng**. Username và email không đổi được ([ACC-008, 009](registration-verification.md#rules)).
- Thông tin công khai chỉ gồm ID, username và tên hiển thị. Khôi phục tài khoản chỉ qua email (ACC-P05).
- Quyền của chủ tài khoản (ACL-01) xem [trạng thái tài khoản](account-state.md#permissions).

<a id="flows"></a>

## Hành trình

<a id="uc-uc-acc-07"></a>

<a id="uc-uc-acc-07--xem-và-sửa-hồ-sơ-riêng"></a>

### UC-ACC-07 — Xem và sửa hồ sơ

**Ai làm:** người đã đăng nhập.

**Điều kiện trước:** phiên còn hạn và chưa bị thu hồi; tài khoản đang hoạt động và đã xác minh.

**Các bước:**

1. Người dùng mở màn hình hồ sơ (ACC-S06). Hệ thống hiện hồ sơ, username và email ở dạng chỉ đọc.
2. Người dùng sửa tên hiển thị, giới thiệu, ngôn ngữ, múi giờ rồi bấm lưu.
3. Hệ thống kiểm tra dữ liệu (ACC-008, 013), lưu hồ sơ của chính người gọi và trả về hồ sơ đã lưu; giao diện cập nhật.

**Ngoại lệ:**

- Dữ liệu sai: báo lỗi theo từng trường. Lưu lỗi: giữ nguyên bản đang sửa.
- Phiên không hợp lệ: yêu cầu đăng nhập lại.
- Server không dùng user ID do client gửi; chỉ sửa hồ sơ của chính người gọi.
- Tên hiển thị trùng với người khác vẫn được; khi chọn người trong DM thì dùng ID hoặc username.
- Cách chuẩn hóa và đếm UTF-16 phải giống nhau ở HTTP, service và DB.

**Kết quả:** các trường hợp lệ được lưu; username và email không đổi. Thông tin công khai không có email.

<a id="contracts"></a>

## Giao diện, API và dữ liệu

| Màn hình | Trạng thái | Phản hồi |
|---|---|---|
| ACC-S06 Hồ sơ | Đang tải, đang lưu, lỗi, dữ liệu không hợp lệ | Lưu lỗi thì giữ bản đang sửa; username và email chỉ đọc |

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/me` | Bearer | 200 `UserAccountResponse` |
| `PATCH /users/me` | Bearer; `displayName`, `bio?`, `locale`, `timezone` | 200 hồ sơ sau khi lưu |

- `UserAccountResponse`: `id, username, displayName, email, emailVerified, status, bio, avatarObjectKey, locale, timezone, createdAt, updatedAt, version`.
- `SessionResponse`: `id, deviceName, userAgent, lastSeenIp, createdAt, lastSeenAt, expiresAt, isCurrent`.

Email chỉ có trong `/users/me` (hồ sơ riêng), không có trong kết quả tìm kiếm. Token Development chỉ xuất hiện khi bật `ExposeDevelopmentTokens`; không dùng chúng trong giao diện sản phẩm.

| Bảng | Vai trò |
|---|---|
| `identity.users` | ID cố định, username và trạng thái; username (đã chuẩn hóa) là duy nhất |
| `identity.user_profiles` | Một hồ sơ mỗi người dùng |
| `identity.user_emails` | Email (đã chuẩn hóa) là duy nhất; tối đa một email chính; `verified_at` tách khỏi mật khẩu |

### Module khác lấy thông tin người dùng thế nào

- `IUserDirectory` trả `UserSummary(id, username, displayName)`: tìm theo ID, theo username chính xác, hoặc nhiều ID.
- [IUserSearchDirectory](../../../services/SCDC.Contracts/Identity/IUserSearchDirectory.cs): tìm theo một phần username hoặc tên hiển thị, dùng trong DM.
- [IHistoricalUserSummaryReader](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs): lấy thông tin tác giả hoặc người kia trong lịch sử, kể cả khi tài khoản không còn hoạt động.

Có thông tin người dùng không có nghĩa là có quyền truy cập. Thông tin công khai không bao giờ chứa email, mật khẩu, security stamp hay thông tin phiên.

Code đối chiếu: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [IdentityModule](../../../services/Modules/Identity/IdentityModule.cs), [IdentityDbContext](../../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs), [IUserDirectory](../../../services/SCDC.Contracts/Identity/IUserDirectory.cs).

<a id="normalization-hồ-sơ-và-chuẩn-hóa-khi-bàn-giao"></a>

### Chuẩn hóa dữ liệu

Thứ tự xử lý: chuẩn hóa → kiểm tra → transaction → ràng buộc DB. Lớp HTTP không dùng bộ quy tắc khác với service.

- Username và email duy nhất được đảm bảo bằng ràng buộc DB, kể cả khi hai người đăng ký cùng lúc.
- Tên hiển thị: cắt khoảng trắng hai đầu, đếm UTF-16 như ACC-008.
- Giới thiệu: cắt khoảng trắng; rỗng thì lưu null.
- Ngôn ngữ và múi giờ: chỉ kiểm tra độ dài, chưa kiểm tra theo danh sách giá trị hợp lệ.
- Phản hồi có `version` của hồ sơ, nhưng API chưa nhận `expectedVersion` để chống ghi đè.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-03 | Hai tài khoản trùng tên hiển thị xuất hiện khi tìm kiếm | Người tìm chọn đúng tài khoản nhờ thông tin phân biệt |
| AC-ACC-08 | Người dùng xem, sửa hồ sơ; người khác tìm tài khoản đó | Đổi được tên hiển thị; không đổi được username; kết quả công khai không có email |
| AC-ACC-17 | Sửa các trường hồ sơ; thử đổi username, email hoặc tải ảnh đại diện | Tên hiển thị, giới thiệu, ngôn ngữ, múi giờ cập nhật được; không có thao tác đổi định danh hay tải ảnh. Cột SQL có sẵn không có nghĩa là có tính năng |

Ký hiệu: A là tài khoản đã xác minh, U chưa xác minh, A1 và A2 là hai phiên của A. Dùng DB và email thử riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-05 | A đổi tên hiển thị; B tìm tên đó; A thử đổi username | Tên mới xuất hiện; username không đổi; không lộ email | AC-ACC-03, 08 |
| TC-ACC-15 | Sửa từng trường, nhập dữ liệu quá dài; xem trợ giúp khi mất email | Trường hợp lệ được lưu; định danh không đổi; không hứa khôi phục qua kênh khác ngoài email | AC-ACC-17, 18 |

Giới hạn tên hiển thị và định danh (AC-ACC-11, TC-ACC-09) xem [đăng ký](registration-verification.md#acceptance). Tìm người theo một phần tên thuộc DM.

Kết quả phải ghi kèm commit và build.

<a id="status"></a>

## Hiện trạng

Đối chiếu với [nền MVP `900e226`](../../releases/mvp.md) ngày 2026-10-13. Bảng test Identity giữ phần rà soát ngày 2026-10-04 và 05. Lần cập nhật này không chạy lại test. Phản hồi 200 hay 204 chưa chứng minh mọi kết quả sau đó.

| UC | Liên quan | Code hiện có | Test tự động | Còn thiếu |
|---|---|---|---|---|
| UC-ACC-07 | AC-ACC-03, 08, 11, 17; TC-ACC-05, 09, 15 | `GetAsync`, `UpdateProfileAsync`; `GET` và `PATCH /users/me`; `UserDirectory`; form hồ sơ | Đọc username, `emailVerified`; PATCH trả 200 | Chưa kiểm tra giá trị sau khi lưu, quyền riêng tư và giới hạn UTF-16. Giao diện chưa cho sửa ngôn ngữ, chưa hiện email chỉ đọc. Tìm người và thông tin lịch sử DM có [bằng chứng riêng](../../records/verification/messaging/README.md) |

ACC-GAP-07 định nghĩa tại [đăng ký](registration-verification.md#status): cần dùng chung fixture UTF-16 cho đăng ký và hồ sơ.

## Còn mở

- Kiểm tra ngôn ngữ và múi giờ theo danh sách giá trị hợp lệ.
- Thêm `expectedVersion` để chống ghi đè khi hai nơi cùng sửa: cần đổi hợp đồng API.

---

Nguồn: DEC-019 (tìm theo tên hiển thị), DEC-054, DEC-063, DEC-066, DEC-067 (hồ sơ, định danh, khôi phục). ACC-004 là đề xuất kỹ thuật để chọn đúng người.

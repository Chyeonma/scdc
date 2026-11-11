# SCDC — Link email và gửi email

Cơ chế dùng chung cho link xác minh email và link đặt lại mật khẩu: cách cấp token, thời gian chờ gửi lại, cách dùng token an toàn, cách gửi email qua worker, và cách trình duyệt xử lý link.

Luồng người dùng xem [đăng ký và xác minh](registration-verification.md) và [mật khẩu](password-recovery.md).

**Trạng thái:**

- Nền MVP `900e226`: chưa có gửi lại xác minh, chưa có thời gian chờ đầy đủ, chưa có worker gửi email. Đăng ký trên máy dùng token Development.
- Nhánh `feat/identity@60f4db3`: đã có đầy đủ (token, worker, Gmail SMTP) nhưng **chưa ghép** vào nền. Chi tiết ở [hiện trạng](#status).

**Người phụ trách:** Vg phụ trách nghiệp vụ tài khoản, cấp và dùng token; Thái phụ trách gửi email, mẫu thư, worker, thử lại và kiểm thử phần gửi thư. Dữ liệu và worker vẫn nằm trong Identity; không có service email riêng.

Backup key ring và khôi phục từ backup để [sau v1](../../project/overview.md#backup-scope). v1 vẫn phải giữ key ring qua khởi động lại, bảo vệ và xoay khóa, và worker tiếp tục được sau khi khởi động lại.

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-012 | Link xác minh và đặt lại dùng một lần, hạn 30 phút. Hai lần yêu cầu gửi lại cùng loại cách nhau ít nhất 60 giây. Cấp link mới thì link cũ cùng loại hết hiệu lực. Đặt lại mật khẩu không tự xác minh email. |

- Phản hồi không tiết lộ email có tài khoản hay không (ACC-P03).
- Giới hạn tần suất bổ sung (theo IP, theo tài khoản) vẫn là đề xuất, hoãn theo DEC-089; xem [cuối trang](#limiter-đề-xuất).

<a id="contracts"></a>

<a id="api-thiết-kế-chi-tiết-tài-khoản"></a>

## API

[OpenAPI](../../contracts/account-recovery.openapi.json) có 4 thao tác. Ghi chú về endpoint gửi lại và chênh lệch với code được lập từ commit cũ `main@f65408b`. API đang chạy lấy theo Swagger của bản triển khai.

<a id="api-http-xác-minh-gửi-lại-và-khôi-phục"></a>

| Thao tác | Request | Response | Ghi chú |
|---|---|---|---|
| `POST /auth/resend-verification` | `{email}` | 202 `{accepted:true}` | Giống nhau cho mọi trường hợp (không có tài khoản, đã xác minh, không dùng được, đang chờ). Không trả `userId`, thời gian chờ hay token |
| `POST /auth/forgot-password` | `{email}` | 202 `{accepted:true}` | Áp dụng cả cho tài khoản chưa xác minh; không xác minh email. Development có thêm trường token |
| `POST /auth/verify-email` | `{token}` | 204 | `400 Identity.InvalidOrExpiredToken` nếu sai loại, hết hạn, đã dùng hoặc bị thay. Không tự đăng nhập |
| `POST /auth/reset-password` | `{token, newPassword}` | 204 | Kiểm tra mật khẩu trước khi dùng token. Thành công thì đổi stamp, xóa khóa đăng nhập tạm, đăng xuất mọi phiên |

- Lỗi validation: 400 ProblemDetails. Giới hạn theo nguồn (đề xuất): 429 `Common.RateLimitExceeded` kèm `Retry-After`.
- 202 chỉ nghĩa là đã nhận yêu cầu; không nghĩa là tài khoản tồn tại hay email đã tới.
- Response có thông tin tài khoản hoặc token dùng `Cache-Control: no-store`. Không ghi body của các route xác thực vào telemetry.

<a id="token-transaction"></a>

<a id="token-cấp-và-dùng-token-đồng-thời"></a>

## Cấp và dùng token

**Token:** 48 byte ngẫu nhiên, mã hóa base64url; DB chỉ lưu SHA-256. Giữ nguyên cơ chế này.

**Bảng `identity.account_token_policies`:** khóa `(user_id, purpose)`, lưu `last_issued_at` và `active_token_id`. Ngay khi đăng ký cũng ghi bảng này, để gửi lại không bỏ qua thời gian chờ 60 giây. (Bảng đã có ở nhánh Identity.) Giờ server theo UTC; điều kiện còn hạn là `expires_at > now`, đúng mốc hết hạn thì từ chối.

### Cấp lại token

1. Chuẩn hóa email, tìm người dùng, mở transaction và khóa dòng người dùng (`LockUserAsync`) **trước** khi đọc trạng thái, token và policy.
2. Kiểm tra loại token, điều kiện tài khoản, thời gian chờ. Không đủ điều kiện thì trả "đã nhận" như mọi trường hợp khác. Luôn đọc lại dưới khóa, không dựa vào kiểm tra trước transaction.
3. Đủ điều kiện: hủy token cũ cùng loại; tạo token mới, cập nhật policy, tạo EmailDelivery, outbox và audit trong cùng transaction.
4. Gửi lại xác minh không đụng token đặt lại, và ngược lại. Audit chỉ ghi ID, loại và lý do; không ghi token hay hash.

### Dùng token

1. Tìm người dùng từ hash và loại token (chỉ để biết khóa ai). Mở transaction, khóa người dùng, đọc lại token, trạng thái, email đích và hạn.
2. Token phải đúng là token đang hiệu lực trong policy.
3. Xác minh: cập nhật email và tài khoản. Đặt lại: cập nhật hash mật khẩu, stamp, đăng xuất mọi phiên.
4. Đánh dấu token đã dùng, hủy các token cũ cùng loại. Commit tất cả hoặc không gì cả.

### Các trường hợp đồng thời

- Hai lần dùng cùng token: chỉ một thành công.
- Dùng token và gửi lại tranh cùng khóa; thứ tự commit quyết định link nào còn hiệu lực.
- Gửi lại sau khi đã xác minh: không tạo token mới.
- Đặt lại không hủy token xác minh còn hạn. Đặt lại với mật khẩu sai quy tắc không làm mất token.
- Đổi mật khẩu, đăng nhập, làm mới token, thu hồi phiên đều khóa người dùng trước rồi mới tới mật khẩu, phiên, token.
- Transaction lỗi: outbox chưa commit nên không có email. Mất phản hồi sau khi dùng token: token không dùng lại được; giao diện có thể cho thử đăng nhập.

**Lưu ý:** cột `ConsumedAt` được dùng cả khi người dùng bấm link lẫn khi token bị hủy; lý do ghi trong audit. Migration tạo policy phải xác định token đang hiệu lực từ dữ liệu có sẵn (dưới khóa), hủy các token thừa, và lấy `last_issued_at` từ token gần nhất; không coi policy rỗng là được gửi ngay.

<a id="delivery"></a>

<a id="delivery-email-và-thiết-kế-còn-lại"></a>

## Gửi email

Đây là thiết kế mục tiêu; nhánh `feat/identity` đã làm phần lớn ([hiện trạng](#status)).

1. **Tạo:** Identity tạo token, ghi hash, loại, hạn 30 phút vào `account_tokens`; cùng transaction tạo `EmailDelivery` và outbox (chỉ chứa ID delivery). Delivery giữ người nhận, phiên bản mẫu thư, ID token và một "phong bì" (envelope) chứa link đã mã hóa. Không ghi token thô vào outbox, audit hay log. Domain của link lấy từ cấu hình, không lấy từ header hay URL của request.
2. **Gửi:** worker giành delivery bằng lease có hạn, để hai worker không gửi cùng một thư. Trước mỗi lần gửi, kiểm tra token còn hiệu lực và tài khoản đúng trạng thái. Giải mã envelope ngay trước khi gọi nhà cung cấp. Khóa mã hóa quản lý ngoài DB, có phiên bản, lưu bền qua khởi động lại.
3. **Trạng thái:** `Pending → Sending → ProviderAccepted`, hoặc `RetryPending`, `Failed`, `Suppressed`. `ProviderAccepted` chỉ nghĩa nhà cung cấp đã nhận, không nghĩa đã vào hộp thư. Trạng thái đã giao hay bị trả về cập nhật từ callback (nếu nhà cung cấp hỗ trợ); callback lặp không gây lỗi.
4. **Thử lại:** tối đa 5 lần, chờ 10, 30, 90, 300 giây có thêm jitter. Dừng khi token sắp hết hạn, bị thay, đã dùng, hoặc lỗi người nhận không thể thử lại. Timeout sau khi nhà cung cấp có thể đã nhận có thể gây thư lặp; dùng ID delivery làm khóa chống trùng nếu nhà cung cấp hỗ trợ. Mỗi link vẫn chỉ dùng được một lần.
5. **Dọn:** xóa envelope ngay khi nhà cung cấp đã nhận, hoặc khi thất bại cuối. Job dọn xóa envelope của delivery bỏ dở trong vòng 1 phút sau khi token hết hạn. Không giữ transaction hay khóa tài khoản khi gọi dịch vụ email.

Token mới có thể được cấp khi thư cũ đang được gửi; không thể thu hồi thư đã gửi. Link cũ bị server từ chối, giao diện hướng dẫn yêu cầu lại.

<a id="delivery-mô-hình-emaildelivery-và-worker"></a>

### Bảng EmailDelivery

| Trường | Quy tắc |
|---|---|
| `id, user_id, account_token_id, purpose, recipient, template_version` | Identity sở hữu; mỗi token và mẫu thư một delivery; không dùng email làm ID công khai |
| `protected_envelope, envelope_expires_at` | Link đã mã hóa và xác thực; hạn không quá hạn token; null sau khi xóa |
| `status, attempt_count, next_attempt_at` | Trạng thái như trên; số lần gửi thật, không tăng khi chỉ kiểm tra |
| `lease_owner, lease_until, provider_message_id, last_error_code` | Lease đề xuất 30 giây, timeout nhà cung cấp 10 giây; chỉ lưu mã lỗi an toàn |
| `created_at, accepted_at, delivered_at, bounced_at` | Phân biệt đã nhận và đã giao; callback lặp cập nhật có điều kiện |

- Worker giành việc bằng transaction ngắn, ghi lease rồi commit **trước** khi gọi nhà cung cấp. Hết lease thì worker khác có thể làm lại, nên không cam kết mỗi thư gửi đúng một lần.
- Outbox chỉ chứa ID delivery, ID người dùng và loại; địa chỉ người nhận chỉ có trong bản ghi Identity và request tới nhà cung cấp.

**Mã hóa envelope:** ASP.NET Core Data Protection, purpose `Identity.EmailDelivery.v1` (cursor dùng purpose khác). Key ring riêng theo môi trường, được bảo vệ, lưu bền qua khởi động lại. Xóa khóa làm dữ liệu cũ không giải mã được ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0)), nên phải kiểm tra tuổi của envelope và cursor trước khi dọn khóa. Không lưu khóa trong SQL hay repo. Backup key ring và kiểm thử khôi phục để sau v1; khi đó không gửi lại thư đã quá hạn hoặc đã bị thu hồi.

<a id="v1-email-delivery-model"></a>

### D37 — Gửi bằng Gmail cho phát triển và demo

**Quyết định:** phát triển và demo v1 dùng Gmail cá nhân hiện có qua SMTP, đăng nhập bằng email và App Password. Identity sở hữu token, delivery và outbox; worker chạy cùng API Identity ([D12](../../system/backend.md#v1-application-workers)).

| Mục | Chọn |
|---|---|
| Dịch vụ | Gmail cá nhân; Google chuyển thư tới người nhận |
| Giao thức | SMTP, email và App Password |
| Đường đi | Worker Identity → Gmail SMTP → máy chủ thư người nhận |
| Bí mật | App Password cấp riêng cho container Identity ([D36](../../system/environments.md#v1-secret-provisioning)) |
| Phạm vi | Phát triển và demo; bản công khai đánh giá riêng |

**Lý do:** dùng tài khoản sẵn có, không phải tự vận hành máy chủ thư. **Đổi lại:** phụ thuộc Google, mạng và hạn mức tài khoản.

Lưu ý:

- Gmail hỗ trợ SMTP qua TLS ([Google](https://developers.google.com/workspace/gmail/imap/imap-smtp)).
- Gmail cá nhân có thể bị giới hạn khi gửi quá 500 thư/ngày, rồi phải chờ 1–24 giờ ([hạn mức](https://support.google.com/mail/answer/22839?hl=en)). Đây không phải ngân sách gửi đã tính cho SCDC.
- App Password cần bật xác minh hai bước và bị thu hồi khi đổi mật khẩu Google ([App Password](https://support.google.com/accounts/answer/185833?hl=en)); cần quy trình cập nhật.
- D37 không thêm service, broker hay thay đổi giao thức giữa bốn service.

<a id="browser-links"></a>

<a id="browser-liên-kết-email-và-trạng-thái-trình-duyệt"></a>

## Link trên trình duyệt

- Link có dạng `/auth/verify#token=…` và `/auth/reset#token=…`. Phần sau `#` (fragment) không được gửi lên server trong URL ([MDN](https://developer.mozilla.org/en-US/docs/Web/URI/Reference/Fragment)).
- App đọc token một lần vào bộ nhớ, rồi dùng `history.replaceState` để xóa khỏi URL.
- Trang này không có analytics hay script bên thứ ba, dùng `Referrer-Policy: no-referrer`, không ghi toàn bộ URL vào log lỗi. Fragment không bảo vệ được trước script chạy trên trang.
- **Xác minh:** có nút Xác minh; chỉ gửi POST khi người dùng bấm. Mở link (GET) không dùng token.
- **Đặt lại:** chỉ gửi POST khi người dùng nhập mật khẩu mới hợp lệ.
- Link sai, hết hạn, đã dùng: cùng thông báo "Liên kết không còn sử dụng được" kèm nút yêu cầu lại; không hiện token.
- Thành công: chuyển tới đăng nhập. Tài khoản chưa xác minh đặt lại mật khẩu xong vẫn phải xác minh.

Chi tiết này cần review cùng prototype.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-14 | Gửi lại link xác minh hoặc đặt lại trước và sau 60 giây; dùng link cũ và mới | Trong thời gian chờ không cấp thêm; sau khi cấp mới, link cũ cùng loại không dùng được; hai loại độc lập |
| AC-ACC-15 | Hai request đồng thời dùng cùng một link | Chỉ một thành công; request kia không thay đổi gì; dùng lại bị từ chối |
| AC-ACC-19 | Đăng ký hoặc quên mật khẩu ngoài Development và nhận email thật | Response không chứa token; email có link đúng domain, loại, hạn; hoàn tất được luồng. Đã vào outbox không được ghi là đã giao |
| AC-ACC-20 | Mở link (GET); gửi lại, dùng, đặt lại đồng thời; đặt lại với mật khẩu sai quy tắc | GET không dùng token; mỗi token dùng một lần; hai loại độc lập; mật khẩu sai không làm mất token |
| AC-ACC-21 | Gửi lại hoặc quên mật khẩu với email không tồn tại, đã xác minh, không dùng được, đang chờ | Production luôn trả 202 cùng body; không lộ trạng thái, token hay thời gian chờ |

Ký hiệu: A là tài khoản đã xác minh, U chưa xác minh. Dùng DB và email thử riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-06 | Dùng link sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác; không lộ token trong giao diện hay log | AC-ACC-06 |
| TC-ACC-07 | Khôi phục hoặc gửi lại với email có và không có tài khoản | Phản hồi giống nhau | ACC-P03 |
| TC-ACC-12 | Gửi lại ở giây 59 và 60; hai request đồng thời; dùng link cũ và mới | Thời gian chờ nhất quán; tối đa một link mới; hai loại độc lập | AC-ACC-14 |
| TC-ACC-13 | Hai request dùng cùng token xác minh, cùng token đặt lại | Chỉ một thành công; request kia báo lỗi token | AC-ACC-15 |
| TC-ACC-16 | Ngoài Development: nhận email từ worker; dừng worker rồi chạy lại; cấp link mới trước khi thư cũ tới | Không lộ token trong response hay log; link đúng; link bị thay không dùng được | AC-ACC-19 (cần Gmail thật) |
| TC-ACC-17 | Dùng token và gửi lại tranh khóa; đặt lại với mật khẩu sai rồi dùng lại token với mật khẩu đúng | Kết quả theo thứ tự commit; rollback không gửi thư; mật khẩu sai không làm mất token | AC-ACC-14, 15, 20 |
| TC-ACC-18 | Mở link (GET); kiểm tra URL sau khi đọc; các trạng thái email; lease hết hạn khi nhà cung cấp đã nhận | GET không đổi DB; URL không còn token; không lộ trạng thái; thư lặp vẫn chỉ dùng được một lần | AC-ACC-19, 20, 21 |

Ngoài ra v1 cần thử: rollback không gửi thư; worker chết trước và sau khi nhà cung cấp nhận; thư lặp hoặc đến sai thứ tự; xoay khóa; dọn envelope. Kết quả phải ghi kèm commit và build.

<a id="status"></a>

## Hiện trạng

Đối chiếu ngày 2026-10-13, phân biệt ba nguồn:

- **[Nền MVP `900e226`](../../releases/mvp.md):** chưa có gửi lại xác minh, chưa có thời gian chờ đặt lại đầy đủ, chưa có worker gửi email. Tích hợp DM không chứng minh gửi email thật.
- **Commit cũ `main@f65408b70ba5f0dd5a09be2091ddba350f0e180c`:** nguồn của bảng thiếu sót dưới đây (trước khi ghép DM). Các thiếu sót vẫn còn ở `900e226`.
- **Nhánh `feat/identity@60f4db3cfc72877c536829fa0a8495b6f957b085`:** đã có phần email; chưa ghép vào nền.

### Thiếu sót trong nền

| Mã | Yêu cầu | Trong `900e226` | Cần làm |
|---|---|---|---|
| ACC-GAP-02 | Gửi lại sau 60 giây, hủy link cũ (ACC-012) | Chưa có gửi lại xác minh hay thời gian chờ đặt lại; yêu cầu đặt lại mới đã hủy token cũ | Đối chiếu code nhánh Identity; kiểm thử thời gian chờ, cấp đồng thời, hai loại độc lập |
| ACC-GAP-03 | Token xác minh dùng một lần (ACC-012) | Xác minh đọc và lưu token nhưng không khóa hay dùng có điều kiện như đặt lại | Đối chiếu code nhánh Identity; kiểm thử xác minh và đặt lại đồng thời, tranh khóa với gửi lại |
| ACC-GAP-05 | Gửi được link qua email (ACC-012) | Outbox chỉ chứa email và ID token; DB chỉ có hash; chưa có worker | Đối chiếu worker và cấu hình v1; thử Gmail thật, lỗi gửi, worker chạy tiếp sau khởi động lại |
| ACC-GAP-06 | Chống lạm dụng đăng ký, đăng nhập, đặt lại, gửi lại | Có khóa đăng nhập tạm; chưa có giới hạn theo nguồn | Ngưỡng vẫn là đề xuất (DEC-089); khi chọn cần cấu hình và kiểm thử 429 không lộ email |

### Code trên nhánh `feat/identity`

| Phần | Đã có | File |
|---|---|---|
| Cấp và dùng token | Policy theo người dùng và loại; thời gian chờ 60 giây; hủy token cũ cùng loại. Xác minh và đặt lại khóa người dùng rồi đọc lại token và policy; đặt lại kiểm tra mật khẩu trước, không tự xác minh tài khoản | `services/Modules/Identity/Infrastructure/Services/RegistrationService.cs` |
| Delivery và outbox | Cùng transaction với token và audit; outbox chỉ có ID và loại; token trong envelope Data Protection; key ring lưu trên đĩa, có cấu hình certificate bảo vệ ngoài Development | `services/Modules/Identity/Infrastructure/Email/AccountEmailQueue.cs`, `services/Modules/Identity/IdentityModule.cs` |
| Worker và Gmail SMTP | Worker nền trong Identity; giành việc bằng lease và `FOR UPDATE SKIP LOCKED`; kiểm tra token trước khi gửi; gọi SMTP ngoài transaction; thử lại tối đa 5 lần có jitter; xóa envelope; trạng thái `ProviderAccepted`. Dùng `System.Net.Mail.SmtpClient`, cổng 587, TLS | `IdentityEmailWorker.cs`, `IdentityEmailProcessor.cs`, `GmailSmtpEmailSender.cs` trong `services/Modules/Identity/Infrastructure/Email/` |
| Schema | Bảng policy và delivery; điền token hiện hành và mốc cấp để giữ thời gian chờ | `database/postgres/migrations/001_identity_email.sql` |

`tests/SCDC.Api.Tests/Identity/IdentityCompletionTests.cs` trên nhánh có các ca: thời gian chờ 59 và 60 giây, gửi lại đồng thời, xác minh đồng thời, hết hạn, rollback, lease, hai worker, thử lại, timeout, lỗi cuối, xóa envelope. Các test thay SMTP bằng `RecordingSender`, nên không chứng minh Gmail hay hộp thư đã nhận. Lần rà soát này chỉ đọc code, không chạy test.

### Chưa có bằng chứng

- Gửi Gmail thật; dùng token và gửi lại đồng thời; đặt lại đồng thời; worker chết sau khi SMTP đã nhận; xoay khóa; dọn envelope trong 1 phút khi worker dừng hoặc tải lớn.
- Callback đã giao hay bị trả về (schema mới có `ProviderAccepted`).
- Hướng dẫn trên nhánh cấp mật khẩu qua biến môi trường; chưa đáp ứng cách cấp bí mật bằng file theo D36.

## Còn mở

- Cấu hình v1, domain của link, bảo vệ và giữ key ring qua khởi động lại (OQ-002, OQ-008).
- Nhà cung cấp email cho bản công khai; SDK, địa chỉ gửi, webhook, hạn mức ứng dụng; công cụ bắt thư khi chạy trên máy.
- Gói triển khai cần đối chiếu giao diện xác thực và chứng minh token xác minh và đặt lại độc lập.

<a id="limiter-đề-xuất"></a>

## Giới hạn tần suất (đề xuất, hoãn)

Theo DEC-089, các ngưỡng sau là đề xuất để quyết định sau, không phải điều kiện nghiệm thu:

- Đăng ký: 5 lần/giờ/IP.
- Đăng nhập: 30 lần/5 phút/IP, bên cạnh khóa 5 lần sai/tài khoản.
- Quên mật khẩu và gửi lại: tổng 10 lần/giờ/IP; tối đa 5 thư/giờ/tài khoản cho mỗi loại; vẫn giữ thời gian chờ 60 giây.

Khóa theo tài khoản dùng hash hoặc HMAC của định danh đã chuẩn hóa; phản hồi không phân biệt tài khoản có tồn tại hay không. Giới hạn theo IP trả 429 ProblemDetails kèm `Retry-After`; cần thử với mạng dùng chung và IPv6 trước khi chốt. Giới hạn tần suất không thay cho transaction khi dùng token hay kiểm tra phiên.

---

Nguồn: DEC-051, DEC-065 (ACC-012), DEC-063 đến 067 (API), DEC-089 (hoãn giới hạn), DEC-156 (backup sau v1), [DEC-155](../../records/decisions/README.md#dec-155) (D37), [DEC-171](../../records/decisions/README.md#dec-171) (phân công).

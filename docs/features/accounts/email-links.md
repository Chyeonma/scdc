# SCDC — Liên kết email và giao email

Cơ chế Identity dùng chung cho xác minh và khôi phục mật khẩu. Luồng người dùng ở [đăng ký/xác minh](registration-verification.md) và [mật khẩu](password-recovery.md); tài liệu này sở hữu policy token, cooldown, consume, EmailDelivery/worker và bảo vệ liên kết. Phương án ngày 2026-10-04 còn cần triển khai/kiểm chứng.

## Mục lục

- [Quy tắc](#rules)
- [API mục tiêu](#contracts)
- [Token và transaction](#token-transaction)
- [Giao email](#delivery)
- [Liên kết trên trình duyệt](#browser-links)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-012 | Liên kết xác minh/reset dùng một lần, hạn 30 phút. Yêu cầu gửi lại cùng mục đích cách nhau ít nhất 60 giây; cấp liên kết mới vô hiệu liên kết cũ cùng mục đích. Không tự xác minh email khi reset mật khẩu. | DEC-065, DEC-051 |

ACC-P02: verify/reset một lần, 30 phút, cấp mới vô hiệu link cũ cùng mục đích (DEC-065); consume/resend còn phải triển khai và kiểm chứng. ACC-P03: cooldown 60 giây đã chốt, phản hồi không tiết lộ email tồn tại; limiter bổ sung hoãn DEC-089.

<a id="contracts"></a>

<a id="api-thiết-kế-chi-tiết-tài-khoản"></a>

## API mục tiêu

Phương án ngày 2026-10-04 để triển khai DEC-063–067; thuật toán/schema dưới đây được soạn trong phạm vi tài liệu, chưa thay source. [OpenAPI luồng xác minh/khôi phục](../../contracts/account-recovery.openapi.json) có 4 thao tác, ghi rõ resend chưa có endpoint và các endpoint hiện có còn chênh lệch. Swagger sinh từ source vẫn là nguồn cho API hiện đang chạy.

<a id="api-http-xác-minh-gửi-lại-và-khôi-phục"></a>

### HTTP xác minh, gửi lại và khôi phục

| Thao tác | Request | Response mục tiêu | Ngoại lệ/hiệu lực |
|---|---|---|---|
| `POST /auth/resend-verification` | `{email}` | 202 `{accepted:true}` | Cùng body cho unknown/verified/unavailable/cooldown; không trả `userId`, thời điểm cooldown riêng hoặc token |
| `POST /auth/forgot-password` | `{email}` | 202 `{accepted:true}` | Cho cả pending verification; token reset không xác minh email; Development có trường token riêng như source |
| `POST /auth/verify-email` | `{token}` | 204 | 400 `Identity.InvalidOrExpiredToken` nếu sai purpose/hết hạn/đã dùng/bị thay thế; không tự login |
| `POST /auth/reset-password` | `{token,newPassword}` | 204 | Kiểm tra mật khẩu trước consume; reset thành công đổi stamp, reset lockout và thu hồi mọi phiên |

Validation 400 theo ProblemDetails chung; lỗi limiter theo nguồn 429 `Common.RateLimitExceeded` và `Retry-After` là mã đề xuất. Ngưỡng limiter bổ sung còn DEC-089. HTTP 202 chỉ xác nhận yêu cầu được tiếp nhận, không chứng minh tài khoản tồn tại hoặc email đã giao. Dùng `Cache-Control: no-store` cho response có thông tin tài khoản/token; không ghi request body của các route auth vào telemetry.

<a id="token-transaction"></a>

<a id="token-cấp-và-dùng-token-đồng-thời"></a>

## Token và transaction

Token hiện tại sinh từ 48 byte ngẫu nhiên, chuyển base64url và DB giữ SHA-256 hash; giữ cơ chế này. Đề xuất thêm `identity.account_token_policies` khóa `(user_id,purpose)` với `last_issued_at,active_token_id`; lần đăng ký đầu cũng ghi policy để resend không bỏ cooldown 60 giây. Clock server UTC và điều kiện `expires_at > now`; tại đúng mốc hết hạn từ chối.

1. Cấp lại: chuẩn hóa email, tra user qua Identity, mở transaction và `LockUserAsync` trước đọc trạng thái/token/policy. Kiểm tra purpose, điều kiện tài khoản, cooldown và limiter gửi email nếu sau này được chọn; nếu không cấp thì trả accepted giống nhau. Đọc DB dưới khóa, không dựa vào kiểm tra trước transaction.
2. Khi đủ điều kiện, đánh dấu token cũ cùng purpose không còn hợp lệ, tạo token mới + policy + EmailDelivery + outbox + audit trong cùng transaction. Việc cấp lại verify không thu hồi reset và ngược lại; audit chỉ ghi ID/purpose/lý do, không plaintext/hash token.
3. Consume: tìm user từ hash/purpose chỉ để định tuyến khóa; sau đó mở transaction, khóa user, đọc lại token/trạng thái/target email và thời hạn. Token phải vẫn là active của policy. Verify cập nhật email/account; reset cập nhật hash/stamp và thu hồi phiên. Đánh dấu token đã dùng và kết thúc các token cũ cùng purpose; commit toàn bộ hoặc không thay đổi gì.
4. Hai consume chỉ một thành công; consume và resend tranh cùng khóa, thứ tự commit quyết định link có hiệu lực. Resend sau verify không tạo token verify mới. Reset không vô hiệu verify còn hạn. Reset sai policy không làm mất token hợp lệ.
5. Change-password, login, refresh và revoke giữ thứ tự khóa user trước các bản ghi credential/session/token như thiết kế Identity hiện tại. Khi transaction lỗi, không có mail từ outbox chưa commit; response mất sau consume không cho dùng lại token. UI có thể thử login với trạng thái hiện hành, không hứa consume lặp trả thành công.

`ConsumedAt` hiện được dùng cả cho consume và vô hiệu token cũ; lý do phân biệt trong audit, không suy mọi `ConsumedAt` là người dùng đã bấm link. Migration policy cần xác lập active token theo dữ liệu đang có dưới khóa, vô hiệu các bản dư và lấy `last_issued_at` từ token gần nhất; không tự coi policy mới rỗng là được gửi ngay.

<a id="delivery"></a>

<a id="delivery-email-và-thiết-kế-còn-lại"></a>

## Giao email

Repo chưa có worker/provider; payload token ID không đủ dựng lại token từ hash. Thiết kế dưới đây là phương án kỹ thuật để rà soát, chưa có implementation:

1. Identity sinh token ngẫu nhiên, ghi hash/mục đích/hạn 30 phút trong `account_tokens`; cùng transaction tạo `EmailDelivery` và outbox tham chiếu delivery ID. Delivery giữ recipient, template version, token ID và envelope liên kết được mã hóa, không ghi token thô vào payload outbox/audit/log. Domain liên kết lấy từ cấu hình tin cậy, không từ header/URL do người gọi gửi.
2. Worker lấy delivery bằng lease có hạn để nhiều worker không đồng thời xử lý cùng lần gửi; trước mỗi lần gửi kiểm tra token còn hiệu lực/chưa dùng/chưa bị thay thế và tài khoản đúng trạng thái. Giải mã ngay trước gọi provider. Khóa mã hóa được quản lý ngoài DB, có key version và quy trình phục hồi; chọn công cụ/key store cùng topology.
3. Trạng thái `Pending → Sending → ProviderAccepted`, hoặc `RetryPending / Failed / Suppressed`. `ProviderAccepted` chỉ nghĩa provider đã nhận yêu cầu; delivered/bounce cập nhật từ callback được xác thực nếu provider hỗ trợ, không tự coi đã vào hộp thư. Callback lặp cập nhật có điều kiện và không chứa link/token trong log.
4. Retry kỹ thuật đề xuất tối đa 5 lần với khoảng chờ 10/30/90/300 giây và jitter; dừng trước hạn token, khi token bị thay thế/đã dùng hoặc lỗi recipient không thể retry. Provider timeout sau khi có thể đã nhận cho phép email lặp; dùng delivery ID làm idempotency key nếu provider hỗ trợ. Mỗi link vẫn chỉ dùng một lần.
5. Xóa envelope ngay sau provider accepted hoặc suppressed/failed cuối; job dọn tối đa 1 phút sau hạn token cho delivery bị bỏ dở. Chỉ giữ metadata cần đối soát theo chính sách vận hành. Không giữ transaction/khóa tài khoản trong lúc gọi dịch vụ email bên ngoài.

Token mới có thể được cấp trong lúc provider đang giao email cũ; không bảo đảm thu hồi email đã gửi. Liên kết cũ phải bị từ chối phía server và UI chỉ dẫn yêu cầu lại. Kiểm thử rollback không gửi mail, worker crash trước/sau provider accept, email giao lặp/đảo thứ tự, verify/reset song song và cleanup envelope. Provider/domain, cơ chế khóa, schema/migration và kết quả thử email thật vẫn là đầu việc trước phát hành.

<a id="delivery-mô-hình-emaildelivery-và-worker"></a>

### Mô hình EmailDelivery và worker

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

<a id="browser-links"></a>

<a id="browser-liên-kết-email-và-trạng-thái-trình-duyệt"></a>

## Liên kết trên trình duyệt

Thiết kế route SPA `/auth/verify#token=…` và `/auth/reset#token=…` từ public origin cấu hình. Fragment không đi trong HTTP request URI theo [tài liệu URI fragment](https://developer.mozilla.org/en-US/docs/Web/URI/Reference/Fragment); app lấy một lần vào bộ nhớ rồi `history.replaceState` bỏ khỏi URL. Trang này không có analytics/script bên thứ ba, dùng `Referrer-Policy: no-referrer`, không ghi toàn bộ URL vào lỗi; fragment không thay bảo vệ khỏi script trên trang.

Trang verify hiển thị nút xác minh để chỉ POST khi người dùng thực hiện thao tác; GET mở link không consume. Reset chỉ POST khi người dùng điền mật khẩu mới hợp lệ. Link sai/hết hạn/đã dùng đều báo “Liên kết không còn sử dụng được” và đường yêu cầu mới, không hiện token. Thành công verify/reset dẫn đến login; reset của pending account vẫn cần verify. Đây là chi tiết UX kỹ thuật cần rà soát cùng prototype.

## Limiter đề xuất

Theo DEC-089, người dùng chọn giữ limiter bổ sung là đề xuất để quyết định sau: đăng ký 5 lần/giờ/IP; login 30 lần/5 phút/IP bên cạnh lockout 5 lần/tài khoản; forgot/resend gộp 10 lần/giờ/IP và tối đa 5 email/giờ/tài khoản cho mỗi mục đích, vẫn tuân cooldown 60 giây. Key tài khoản limiter dùng hash/HMAC của định danh chuẩn hóa; phản hồi accepted/cooldown không phân biệt tài khoản tồn tại. Limiter theo IP trả 429 ProblemDetails có `Retry-After`; kiểm thử mạng dùng chung và IPv6 trước khóa ngưỡng. Không coi limiter là biện pháp thay thế transaction consume token hoặc kiểm tra phiên.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-14 | Gửi lại verify/reset trước/sau 60 giây và dùng link cũ/mới. | Không cấp thêm token trong cooldown; sau khi cấp mới link cũ cùng mục đích không dùng được; mục đích verify/reset độc lập. |
| AC-ACC-15 | Hai request đồng thời dùng một link verify hoặc reset. | Chỉ một thao tác consume thành công; request còn lại không thực hiện thay đổi; liên kết dùng lại bị từ chối. |
| AC-ACC-19 | Đăng ký/quên mật khẩu ngoài Development và nhận email thật. | Response không có token sử dụng được; nhận link đúng domain/mục đích/hạn, hoàn tất luồng; tiếp nhận outbox không được ghi thành email đã giao. |
| AC-ACC-20 | Mở link qua GET, gửi lại/consume/reset đồng thời và reset mật khẩu sai policy | GET không consume; một token dùng một lần; token verify/reset độc lập; reset sai không mất token |
| AC-ACC-21 | Email unknown/verified/unavailable/cooldown gọi resend/forgot | Production cùng 202/body accepted; không trả trạng thái tài khoản/token hoặc retry time riêng |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-06 | Dùng liên kết xác minh/reset sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác trái phép; không lộ token trong UI/log | AC-ACC-06; thời hạn 30 phút theo DEC-065 |
| TC-ACC-07 | Gửi yêu cầu khôi phục/gửi lại với email tồn tại và không tồn tại | Phản hồi công khai có cùng ý nghĩa; không tiết lộ tài khoản | ACC-P03; cooldown đã chốt, limiter bổ sung hoãn DEC-089 |
| TC-ACC-12 | Gửi lại verify/reset ở giây 59/60; hai request đồng thời; dùng link cũ/mới | Cooldown nhất quán, tối đa một link mới; mục đích độc lập | AC-ACC-14; cần implementation resend/limiter |
| TC-ACC-13 | Hai request dùng cùng verify token và reset token | Chỉ một consume thành công; request còn lại lỗi token | AC-ACC-15; verify concurrency cần kiểm chứng |
| TC-ACC-16 | Ngoài Development, nhận email verify/reset từ worker; dừng worker rồi retry; cấp link mới trước khi email cũ được giao | Không lộ token ở response/log; email dùng link đúng; link bị thay thế không còn hợp lệ | AC-ACC-19; cần worker/provider |
| TC-ACC-17 | Consume và resend tranh khóa; reset sai mật khẩu rồi dùng lại token đúng | Kết quả theo thứ tự commit; rollback không cấp mail; reset sai không consume | AC-ACC-14/15/20 |
| TC-ACC-18 | GET link, fragment/URL sau đọc, unknown/verified/unavailable/cooldown; worker lease hết khi provider đã nhận | GET không đổi DB; URL bỏ token; response không lộ trạng thái; email lặp vẫn một consume | AC-ACC-19/20/21 |

Ngoài các ca trên, thử rollback không gửi mail, worker crash trước/sau provider accept, email lặp/đảo thứ tự, key rotation/restore và cleanup envelope. Tiếp nhận outbox hoặc ProviderAccepted chưa chứng minh email vào hộp thư.

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source ngày 2026-10-04/05. Repo chưa có resend controller/service, cooldown reset đầy đủ hoặc email worker/provider; token ID trong outbox không đủ dựng lại token từ hash. Verify concurrency chưa được chứng minh. Không có kết quả chạy TC hoặc nghiệm thu email thật trong hồ sơ này.

| Mã | Yêu cầu/căn cứ | Source hiện tại | Việc phải hoàn tất |
|---|---|---|---|
| ACC-GAP-02 | Gửi lại sau 60 giây, vô hiệu link cũ — ACC-012 | Chưa có resend verification hoặc cooldown reset; reset request mới đã vô hiệu token reset cũ | Thiết kế/cài đặt cooldown và resend; kiểm chứng request đồng thời |
| ACC-GAP-03 | Xác minh bằng token dùng một lần — ACC-012 | Verify đọc token/lưu nhưng không có khóa hoặc conditional consume như reset | Thử verify đồng thời; nếu nhiều request cùng thành công thì sửa consume; chưa có bằng chứng |
| ACC-GAP-05 | Giao liên kết email dùng được — ACC-012 | Outbox chỉ chứa email/token ID; DB giữ hash, chưa có worker/provider | Thiết kế đưa link/token gửi được tới worker, trạng thái gửi/retry và kiểm thử email thật |
| ACC-GAP-06 | Chống lạm dụng đăng ký/login/reset/resend | Có lockout; chưa có rate limiter theo nguồn yêu cầu/gửi email | Chọn ngưỡng/cửa sổ cấu hình được; đo lỗi 429 và phản hồi không tiết lộ email |

OQ-002/OQ-008: chọn provider, public origin/domain và nơi lưu key trước khi giao email thật. Các ngưỡng limiter bổ sung vẫn hoãn theo DEC-089; không tự nâng chúng thành điều kiện nghiệm thu. Gói triển khai phải cập nhật auth UI và chứng minh token verify/reset độc lập.

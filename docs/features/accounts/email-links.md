# SCDC — Liên kết email và giao email

Cơ chế Identity dùng chung cho xác minh và khôi phục mật khẩu. Luồng người dùng ở [đăng ký/xác minh](registration-verification.md) và [mật khẩu](password-recovery.md); tài liệu này sở hữu policy token, cooldown, consume, EmailDelivery/worker và bảo vệ liên kết. [Hiện trạng](#status) phân biệt nền MVP `900e226`, snapshot lịch sử `main@f65408b` và triển khai trên nhánh Identity chưa tích hợp.

Backup key ring và khôi phục từ backup thuộc [giai đoạn sau v1](../../project/overview.md#backup-scope) theo DEC-156. v1 vẫn giữ key ring bền qua restart, bảo vệ/rotation khóa và phục hồi worker từ dữ liệu hiện hành; khôi phục mật khẩu không thuộc phần hoãn này.

## Mục lục

- [Quy tắc](#rules)
- [API mục tiêu](#contracts)
- [Token và transaction](#token-transaction)
- [Giao email](#delivery)
- [D37 — Mô hình giao email thật](#v1-email-delivery-model)
- [Liên kết trên trình duyệt](#browser-links)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-012 | Liên kết xác minh/reset dùng một lần, hạn 30 phút. Yêu cầu gửi lại cùng mục đích cách nhau ít nhất 60 giây; cấp liên kết mới vô hiệu liên kết cũ cùng mục đích. Không tự xác minh email khi reset mật khẩu. | DEC-065, DEC-051 |

ACC-P02: verify/reset một lần, 30 phút, cấp mới vô hiệu link cũ cùng mục đích (DEC-065); mức triển khai và kiểm chứng consume/resend theo [hiện trạng](#status). ACC-P03: cooldown 60 giây đã chốt, phản hồi không tiết lộ email tồn tại; limiter bổ sung hoãn DEC-089.

<a id="contracts"></a>

<a id="api-thiết-kế-chi-tiết-tài-khoản"></a>

## API mục tiêu

API mục tiêu thực hiện DEC-063–067. [OpenAPI luồng xác minh/khôi phục](../../contracts/account-recovery.openapi.json) có 4 thao tác; chú thích thiếu endpoint resend và chênh lệch source được lập từ snapshot lịch sử `main@f65408b`. Nền `900e226` vẫn chưa tích hợp email/resend từ nhánh Identity theo [hiện trạng](#status). Swagger sinh từ source của bản triển khai là nguồn cho API đang chạy.

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

Token hiện tại sinh từ 48 byte ngẫu nhiên, chuyển base64url và DB giữ SHA-256 hash; giữ cơ chế này. Thiết kế dùng `identity.account_token_policies` khóa `(user_id,purpose)` với `last_issued_at,active_token_id`; lần đăng ký đầu cũng ghi policy để resend không bỏ cooldown 60 giây. Bảng này đã có trên nhánh Identity theo [hiện trạng](#status). Clock server UTC và điều kiện `expires_at > now`; tại đúng mốc hết hạn từ chối.

1. Cấp lại: chuẩn hóa email, tra user qua Identity, mở transaction và `LockUserAsync` trước đọc trạng thái/token/policy. Kiểm tra purpose, điều kiện tài khoản, cooldown và limiter gửi email nếu sau này được chọn; nếu không cấp thì trả accepted giống nhau. Đọc DB dưới khóa, không dựa vào kiểm tra trước transaction.
2. Khi đủ điều kiện, đánh dấu token cũ cùng purpose không còn hợp lệ, tạo token mới + policy + EmailDelivery + outbox + audit trong cùng transaction. Việc cấp lại verify không thu hồi reset và ngược lại; audit chỉ ghi ID/purpose/lý do, không plaintext/hash token.
3. Consume: tìm user từ hash/purpose chỉ để định tuyến khóa; sau đó mở transaction, khóa user, đọc lại token/trạng thái/target email và thời hạn. Token phải vẫn là active của policy. Verify cập nhật email/account; reset cập nhật hash/stamp và thu hồi phiên. Đánh dấu token đã dùng và kết thúc các token cũ cùng purpose; commit toàn bộ hoặc không thay đổi gì.
4. Hai consume chỉ một thành công; consume và resend tranh cùng khóa, thứ tự commit quyết định link có hiệu lực. Resend sau verify không tạo token verify mới. Reset không vô hiệu verify còn hạn. Reset sai policy không làm mất token hợp lệ.
5. Change-password, login, refresh và revoke giữ thứ tự khóa user trước các bản ghi credential/session/token như thiết kế Identity hiện tại. Khi transaction lỗi, không có mail từ outbox chưa commit; response mất sau consume không cho dùng lại token. UI có thể thử login với trạng thái hiện hành, không hứa consume lặp trả thành công.

`ConsumedAt` hiện được dùng cả cho consume và vô hiệu token cũ; lý do phân biệt trong audit, không suy mọi `ConsumedAt` là người dùng đã bấm link. Migration policy cần xác lập active token theo dữ liệu đang có dưới khóa, vô hiệu các bản dư và lấy `last_issued_at` từ token gần nhất; không tự coi policy mới rỗng là được gửi ngay.

<a id="delivery"></a>

<a id="delivery-email-và-thiết-kế-còn-lại"></a>

## Giao email

Nhánh `feat/identity` đã có worker và Gmail SMTP; hiện trạng theo nhánh tại [phần đối chiếu source](#status). Thiết kế dưới đây là hợp đồng mục tiêu cần đối chiếu với implementation; token ID trong outbox tự nó không đủ dựng lại token từ hash.

1. Identity sinh token ngẫu nhiên, ghi hash/mục đích/hạn 30 phút trong `account_tokens`; cùng transaction tạo `EmailDelivery` và outbox tham chiếu delivery ID. Delivery giữ recipient, template version, token ID và envelope liên kết được mã hóa, không ghi token thô vào payload outbox/audit/log. Domain liên kết lấy từ cấu hình tin cậy, không từ header/URL do người gọi gửi.
2. Worker lấy delivery bằng lease có hạn để nhiều worker không đồng thời xử lý cùng lần gửi; trước mỗi lần gửi kiểm tra token còn hiệu lực/chưa dùng/chưa bị thay thế và tài khoản đúng trạng thái. Giải mã ngay trước gọi provider. Khóa mã hóa được quản lý ngoài DB, có key version và lưu bền qua restart; chọn công cụ/key store cùng topology. Backup/phục hồi khóa từ backup áp dụng sau v1.
3. Trạng thái `Pending → Sending → ProviderAccepted`, hoặc `RetryPending / Failed / Suppressed`. `ProviderAccepted` chỉ nghĩa provider đã nhận yêu cầu; delivered/bounce cập nhật từ callback được xác thực nếu provider hỗ trợ, không tự coi đã vào hộp thư. Callback lặp cập nhật có điều kiện và không chứa link/token trong log.
4. Retry kỹ thuật đề xuất tối đa 5 lần với khoảng chờ 10/30/90/300 giây và jitter; dừng trước hạn token, khi token bị thay thế/đã dùng hoặc lỗi recipient không thể retry. Provider timeout sau khi có thể đã nhận cho phép email lặp; dùng delivery ID làm idempotency key nếu provider hỗ trợ. Mỗi link vẫn chỉ dùng một lần.
5. Xóa envelope ngay sau provider accepted hoặc suppressed/failed cuối; job dọn tối đa 1 phút sau hạn token cho delivery bị bỏ dở. Chỉ giữ metadata cần đối soát theo chính sách vận hành. Không giữ transaction/khóa tài khoản trong lúc gọi dịch vụ email bên ngoài.

Token mới có thể được cấp trong lúc provider đang giao email cũ; không bảo đảm thu hồi email đã gửi. Liên kết cũ phải bị từ chối phía server và UI chỉ dẫn yêu cầu lại. Kiểm thử rollback không gửi mail, worker crash trước/sau provider accept, email giao lặp/đảo thứ tự, verify/reset song song và cleanup envelope. [D37](#v1-email-delivery-model) đã chọn Gmail cho phát triển/demo; domain liên kết, cấu hình khóa, khả năng áp dụng schema/migration và kết quả thử email thật cần đối chiếu trước phát hành.

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

Thiết kế envelope chọn ASP.NET Core Data Protection với purpose `Identity.EmailDelivery.v1` và hạn token; cursor dùng purpose khác. Key ring riêng theo môi trường, được bảo vệ khi lưu và lưu bền qua restart. Xóa key làm dữ liệu đã bảo vệ bằng key không giải mã được theo [tài liệu Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-management?view=aspnetcore-10.0); v1 phải đối chiếu tuổi envelope/cursor trước dọn key. Backup key ring cùng cấu hình, kiểm thử restore và đối chiếu cửa sổ backup 30 ngày thuộc sau v1. Cách lưu key/certificate/secret store cụ thể cần topology, không lưu khóa trong SQL payload hoặc repo.

Worker kiểm tra token trước gửi; cleanup và giới hạn retry theo phương án ở trên. Bảo vệ envelope không thay việc server từ chối link bị thay thế/hết hạn. Khi triển khai restore backup sau v1, không gửi lại delivery quá hạn hoặc đã bị thu hồi sau recovery point.

<a id="v1-email-delivery-model"></a>

### D37 — Mô hình giao email thật

Trạng thái: đã xác nhận phương án A ngày 2026-10-12 — [DEC-155](../../records/decisions/README.md#dec-155). Phát triển/demo v1 dùng tiếp Gmail cá nhân (`@gmail.com`) qua SMTP, xác thực bằng email và App Password. Identity sở hữu token/delivery/outbox; worker v1 cùng API theo [D12](../../system/backend.md#v1-application-workers). Mốc triển khai và tiêu chí email giữ theo [MVP](../../releases/mvp.md) và [v1](../../releases/v1.md).

| Nội dung | Lựa chọn |
|---|---|
| Dịch vụ | Gmail cá nhân hiện có; Google giao thư tới người nhận |
| Giao thức và xác thực | SMTP, email tài khoản và App Password |
| Đường giao thư | Worker Identity → Gmail SMTP → mail server người nhận |
| Cấp bí mật | App Password cấp riêng cho container Identity theo [D36](../../system/environments.md#v1-secret-provisioning) |
| Phạm vi | Phát triển/demo v1; bản công khai đánh giá riêng |

Lựa chọn tận dụng tài khoản hiện có, không tự vận hành SMTP giao thư trực tiếp. Đánh đổi là phụ thuộc Google, mạng và hạn mức tài khoản. Phương án B tự vận hành SMTP đã được so sánh nhưng không được chọn. Phần Gmail trong Identity đã có trên nhánh tính năng, chưa tích hợp nền `900e226`; [hiện trạng](#status) ghi nguồn và bằng chứng gửi thư còn thiếu.

Gmail công bố SMTP hỗ trợ TLS; endpoint, cổng/chế độ TLS và thư viện triển khai v1 cần đối chiếu từ implementation hiện có. Nguồn: [Google — SMTP](https://developers.google.com/workspace/gmail/imap/imap-smtp).

Gmail cá nhân có hạn mức gửi: Google cho biết gửi quá 500 email/ngày có thể bị giới hạn, sau đó có thể phải chờ 1–24 giờ. Đây chưa là ngân sách gửi hoặc bằng chứng đáp ứng tải của SCDC. App Password yêu cầu xác minh hai bước và bị thu hồi khi đổi mật khẩu tài khoản Google; cần quy trình cập nhật credential. Nguồn: [hạn mức Gmail](https://support.google.com/mail/answer/22839?hl=en), [App Password](https://support.google.com/accounts/answer/185833?hl=en).

Lựa chọn cho bản công khai cần đối chiếu tải và hạn mức sau. D37 chưa chốt dịch vụ cho môi trường công khai, SDK/cấu hình v1, địa chỉ gửi/alias, domain liên kết, webhook hoặc hạn mức ứng dụng. Công cụ bắt thư local cũng chưa chọn.

`ProviderAccepted` giữ nghĩa đã tiếp nhận tại bên giao thư, chưa chứng minh delivered/inbox; retry và dữ liệu khôi phục vẫn theo thiết kế ở trên. D37 không bổ sung service nghiệp vụ thứ năm, broker hoặc thay giao thức nội bộ giữa bốn service. Sơ đồ đã ghi Gmail bên ngoài máy demo; lần cập nhật tài liệu này chưa đổi code, hợp đồng máy đọc, cấu hình hoặc gửi thư.

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
| TC-ACC-12 | Gửi lại verify/reset ở giây 59/60; hai request đồng thời; dùng link cũ/mới | Cooldown nhất quán, tối đa một link mới; mục đích độc lập | AC-ACC-14; cần kết quả chạy theo [hiện trạng](#status) |
| TC-ACC-13 | Hai request dùng cùng verify token và reset token | Chỉ một consume thành công; request còn lại lỗi token | AC-ACC-15; cần kết quả chạy verify/reset gắn build |
| TC-ACC-16 | Ngoài Development, nhận email verify/reset từ worker; dừng worker rồi retry; cấp link mới trước khi email cũ được giao | Không lộ token ở response/log; email dùng link đúng; link bị thay thế không còn hợp lệ | AC-ACC-19; cần kiểm thử Gmail thật |
| TC-ACC-17 | Consume và resend tranh khóa; reset sai mật khẩu rồi dùng lại token đúng | Kết quả theo thứ tự commit; rollback không cấp mail; reset sai không consume | AC-ACC-14/15/20 |
| TC-ACC-18 | GET link, fragment/URL sau đọc, unknown/verified/unavailable/cooldown; worker lease hết khi provider đã nhận | GET không đổi DB; URL bỏ token; response không lộ trạng thái; email lặp vẫn một consume | AC-ACC-19/20/21 |

Ngoài các ca trên, v1 thử rollback không gửi mail, worker crash trước/sau provider accept, email lặp/đảo thứ tự, key rotation và cleanup envelope. Các ca backup/restore key áp dụng sau v1. Tiếp nhận outbox hoặc ProviderAccepted chưa chứng minh email vào hộp thư.

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu hiện trạng ngày 2026-10-13 phân biệt nền bàn giao với các snapshot đã rà soát ngày 2026-10-12:

- [Nền MVP `900e226`](../../releases/mvp.md): đã hợp nhất Community/DM và tích hợp phiên với Hub DM, nhưng chưa có resend, cooldown reset đầy đủ hoặc worker/provider email. Đăng ký local còn dùng token Development; tích hợp DM không chứng minh giao email thật.
- Snapshot lịch sử `main` tại `f65408b70ba5f0dd5a09be2091ddba350f0e180c`: nguồn của bảng gap email trước tích hợp DM; không dùng tên ref này để chỉ nền hiện tại. Các gap email trong bảng vẫn còn ở `900e226`.
- `feat/identity` và `origin/feat/identity` tại `60f4db3cfc72877c536829fa0a8495b6f957b085` trong lần rà soát: đã có triển khai email và các test nêu sau bảng. Phần này chưa tích hợp vào `900e226`.

Các khoảng trống giữ nguyên mã để đối chiếu với bản phát hành; năng lực có trong source nhánh không tự đóng khoảng trống hoặc xác nhận AC/TC đã đạt.

| Mã | Yêu cầu/căn cứ | Source nền `900e226` kế thừa gap email | Việc còn phải đối chiếu/kiểm chứng |
|---|---|---|---|
| ACC-GAP-02 | Gửi lại sau 60 giây, vô hiệu link cũ — ACC-012 | Chưa có resend verification hoặc cooldown reset; reset request mới đã vô hiệu token reset cũ | Đối chiếu triển khai nhánh Identity với bản phát hành; kiểm chứng cooldown, cấp lại đồng thời và mục đích độc lập |
| ACC-GAP-03 | Xác minh bằng token dùng một lần — ACC-012 | Verify đọc token/lưu nhưng không có khóa hoặc conditional consume như reset | Đối chiếu khóa/consume trong nhánh Identity; kiểm chứng verify/reset đồng thời và consume tranh khóa với resend |
| ACC-GAP-05 | Giao liên kết email dùng được — ACC-012 | Outbox chỉ chứa email/token ID; DB giữ hash, chưa có worker/provider | Đối chiếu delivery/worker và cấu hình triển khai v1; kiểm thử Gmail thật, lỗi gửi và worker tiếp tục sau restart |
| ACC-GAP-06 | Chống lạm dụng đăng ký/login/reset/resend | Có lockout; chưa có rate limiter theo nguồn yêu cầu/gửi email | Ngưỡng bổ sung vẫn là đề xuất theo DEC-089; khi chọn cần cấu hình, kiểm thử 429 và phản hồi không tiết lộ email |

Triển khai email trên nhánh `feat/identity` tại commit trên:

| Phần | Năng lực trong source | Căn cứ trên nhánh |
|---|---|---|
| Cấp và dùng token | Resend/forgot dùng policy theo user/purpose, cooldown mặc định 60 giây và vô hiệu token cũ cùng mục đích. Verify/reset khóa user rồi đọc lại token và active policy; reset kiểm tra mật khẩu trước consume và không tự verify tài khoản pending. | `services/Modules/Identity/Infrastructure/Services/RegistrationService.cs` |
| Delivery và outbox | Cùng transaction với token/audit; outbox chỉ giữ ID/purpose. Token gửi email nằm trong envelope Data Protection. Key ring lưu filesystem; có cấu hình certificate bảo vệ key ring ngoài Development. | `services/Modules/Identity/Infrastructure/Email/AccountEmailQueue.cs`, `services/Modules/Identity/IdentityModule.cs` |
| Worker và Gmail SMTP | Worker nền trong Identity; claim bằng lease và `FOR UPDATE SKIP LOCKED`, kiểm tra token trước gửi, gọi SMTP ngoài transaction. Có retry tối đa 5 lần, backoff/jitter, purge envelope và trạng thái `ProviderAccepted`. Sender dùng `System.Net.Mail.SmtpClient`, cổng 587 và TLS. | `services/Modules/Identity/Infrastructure/Email/IdentityEmailWorker.cs`, `IdentityEmailProcessor.cs`, `GmailSmtpEmailSender.cs` trong cùng thư mục |
| Schema/migration | Có bảng policy/delivery, backfill token hiện hành và timestamp cấp để giữ cooldown. | `database/postgres/migrations/001_identity_email.sql` |

`tests/SCDC.Api.Tests/Identity/IdentityCompletionTests.cs` có các ca cooldown 59/60 giây, resend đồng thời, verify đồng thời, hết hạn, rollback, lease, hai worker, retry/timeout/lỗi cuối và purge envelope. Các ca này thay SMTP sender bằng `RecordingSender`, nên không chứng minh email được Gmail hoặc hộp thư người nhận tiếp nhận. `docs/identity-email.md` trên nhánh có hướng dẫn và báo cáo test/smoke; lần rà soát này chỉ đọc source, không chạy lại test và không xác nhận nghiệm thu.

Chưa có bằng chứng nghiệm thu tại đây cho gửi Gmail thật, consume–resend đồng thời, reset đồng thời, crash sau SMTP accept, rotation khóa hoặc cleanup tối đa một phút khi worker dừng/tải lớn. Backup key và kiểm thử restore thuộc sau v1. Schema nhánh có `ProviderAccepted`, chưa có receipt delivered/bounce; tính năng callback vẫn phụ thuộc provider. Hướng dẫn nhánh cấp credential qua biến môi trường; chưa chứng minh đã đáp ứng nguồn file bí mật theo D36.

[D37](#v1-email-delivery-model) đã chốt Gmail cho phát triển/demo. Cấu hình v1, public origin/domain và bảo vệ/giữ key ring qua restart còn cần đối chiếu theo OQ-002/OQ-008; provider cho bản công khai chưa chọn. Các ngưỡng limiter bổ sung vẫn hoãn theo DEC-089; không tự nâng chúng thành điều kiện nghiệm thu. Gói triển khai cần đối chiếu auth UI và chứng minh token verify/reset độc lập.

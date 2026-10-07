# SCDC — Trạng thái tài khoản

Tổ chức lại: 2026-10-07. Các bảng source/test bên dưới là đối chiếu ngày 2026-10-04/05; không tự ghi nhận một lần chạy mới hoặc kết quả của mọi revision sau đó.

Cập nhật: 2026-10-06. Phạm vi: REQ-010, SCP-002. Quy tắc ACC, use case UC-ACC, tiêu chí AC-ACC, màn hình ACC-S, ca TC-ACC và ACL-01/02.

Đặc tả đầy đủ cho [v1](../../releases/v1.md). [MVP](../../releases/mvp.md) chọn các luồng nền để làm trước, tận dụng implementation hiện có; không yêu cầu hoàn tất toàn bộ UC ngay ở gói đầu. MVP chạy trong một API host; shared transaction/guard liên module được rà soát khi chuyển sang [microservice ở v1](../../architecture.md#target) theo DEC-116.

Nghiệp vụ cốt lõi và chính sách tài khoản đã xác nhận theo DEC-063–067; phép đếm UTF-16 theo DEC-068. Identity có implementation và test tự động; gửi lại xác minh, giao email và một số hành vi còn thiếu hoặc khác yêu cầu. Đối chiếu source ngày 2026-10-04 tại [API, trạng thái và dữ liệu](design/README.md#implementation-review). Các ca TC chưa có kết quả thực thi được ghi nhận trong hồ sơ này.

Ngày 2026-10-05 bổ sung use case và ma trận đối chiếu API, giao diện, AC/TC và assertion trong test hiện có. Đây là kết quả đọc source; chưa chạy kiểm thử sản phẩm hoặc xác nhận nghiệm thu. Các lựa chọn chưa chốt được ghi riêng tại [điểm cần xác nhận](#use-case-review).

<a id="use-case-coverage"></a>

### Đối chiếu use case với code và test

Rà soát ngày 2026-10-05. Prefix API là `/api/v1`. `Lifecycle` là test `Identity_v1_supports_the_complete_password_account_lifecycle` trong [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs); các test có tên riêng khác nằm trong [IdentityConcurrencyTests](../../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). `Client` là [api.test.js](../../../clients/WebClient/tests/api.test.js), gồm assertion rotation giữa tab, storage event bị bỏ lỡ, 401 đến trễ, logout khi refresh đang chờ, login mới và trường hợp thiếu Web Locks. Các test này dùng browser/fetch mô phỏng, chưa chứng minh UI hoặc trình duyệt thật.

Source nghiệp vụ: [RegistrationService](../../../services/Modules/Identity/Infrastructure/Services/RegistrationService.cs), [AuthenticationService](../../../services/Modules/Identity/Infrastructure/Services/AuthenticationService.cs), [UserAccountService](../../../services/Modules/Identity/Infrastructure/Services/UserAccountService.cs), [UserDirectory](../../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs). Giao diện/wrapper: [AuthScreen](../../../clients/WebClient/src/components/AuthScreen.jsx), [UserSettingsModal](../../../clients/WebClient/src/components/UserSettingsModal.jsx), [api.js](../../../clients/WebClient/src/api.js).

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-01 | AC-01/09/11/12/19; TC-01/04/09/10/16 | `RegisterAsync`; `POST /auth/register`; form đăng ký | Lifecycle: status 201, token Development, từ chối login trước verify | ACC-GAP-04/05/07; đăng ký trùng đồng thời/Unicode/policy mật khẩu chưa có test; UI đang tự verify bằng token Development |
| UC-ACC-02 | AC-05/07/15/19/20; TC-01/02/06/13/17/18 | `VerifyEmailAsync`; `POST /auth/verify-email`; wrapper verify | Lifecycle: verify 204, login sau verify, `emailVerified=true` | ACC-GAP-03/05; chưa có test verify đồng thời/hết hạn/dùng lại; chưa có trang mở liên kết và xác minh chủ động |
| UC-ACC-03 | AC-14/19/20/21; TC-07/12/16/17/18 | Resend chỉ có thiết kế/OpenAPI mục tiêu | Chưa có test cho resend | ACC-GAP-02/05; thiếu endpoint, cooldown, email và UI |
| UC-ACC-04 | AC-02/04/07/12; TC-01/02/08/10 | `LoginAsync`; `POST /auth/login`; form login | Lifecycle: login username trước/sau verify; `Parallel_wrong_passwords_are_counted_and_trigger_lockout`; hai test login/đổi mật khẩu tranh khóa | Login bằng email, biên hết lockout và UI dẫn tới xác minh chưa có test; form hiện chỉ báo lỗi chung |
| UC-ACC-05 | AC-10/14/18/19/21; TC-03/07/12/15/16/18 | `ForgotPasswordAsync`; `POST /auth/forgot-password`; form quên mật khẩu | Lifecycle: accepted và token reset cho account đã verify | ACC-GAP-01/02/05; chưa cấp token cho pending; chưa có cooldown/email thật hoặc test phản hồi các trạng thái |
| UC-ACC-06 | AC-06/10/12/15/16/20/22; TC-03/06/10/13/14/17/18/19 | `ResetPasswordAsync`; `POST /auth/reset-password`; wrapper reset | Lifecycle: reset 204 với mật khẩu khác, access cũ bị từ chối, mật khẩu cũ không đăng nhập được; `Concurrent_reset_requests_can_consume_a_token_only_once` | Chưa có trang reset; thiếu test pending, biên hạn token, reset sai policy giữ token, verify/reset độc lập và reset mật khẩu trùng |
| UC-ACC-07 | AC-03/08/11/17; TC-05/09/15 | `GetAsync`, `UpdateProfileAsync`; `GET/PATCH /users/me`; `UserDirectory`; form hồ sơ | Lifecycle: đọc username/emailVerified và PATCH trả 200 | Chưa assertion giá trị hồ sơ sau lưu/quyền riêng tư/biên UTF-16; UI chưa cho sửa locale hoặc xem email chỉ đọc; tìm người DM thuộc scope tích hợp |
| UC-ACC-08 | AC-12/16/17/22; TC-10/14/17/19 | `ChangePasswordAsync`; `POST /auth/change-password`; form đổi mật khẩu | Lifecycle: đổi 204, access cũ bị từ chối; `Changing_password_invalidates_previously_issued_reset_tokens`; hai test login/đổi mật khẩu tranh khóa | UI chưa xóa phiên ngay sau thành công; thiếu test sai mật khẩu hiện tại/policy/trùng |
| UC-ACC-09 | AC-13/16; TC-08/11/14 | `RefreshAsync`; `POST /auth/refresh`; wrapper refresh | Lifecycle: rotation/reuse và access bị từ chối; Client: phối hợp refresh/không khôi phục logout hoặc ghi đè login mới | Chưa có test biên hạn phiên/access, response rotation bị mất hoặc trình duyệt thật; thu hồi realtime chưa triển khai |
| UC-ACC-10 | AC-04/16; TC-08/14 | `GetSessionsAsync`, `RevokeSessionAsync`; `GET /auth/sessions`, `DELETE /auth/sessions/{id}`; tab phiên | Lifecycle: đánh dấu current, revoke phiên khác và access phiên đó bị từ chối | Chưa assertion phiên người gọi vẫn sống ngay sau revoke, revoke phiên hiện tại/quyền sở hữu; UI dùng phiên mẫu khi lỗi/rỗng và chưa có thao tác revoke phiên hiện tại |
| UC-ACC-11 | AC-04/16; TC-08/14 | `LogoutAsync`, `LogoutAllAsync`; `POST /auth/logout`, `POST /auth/logout-all`; wrapper xóa phiên local | Lifecycle: logout/logout-all và từ chối access; Client: logout giữa tab không bị refresh khôi phục | UI ghi “tất cả thiết bị khác” trong khi logout-all gồm phiên gọi; thiếu test logout token không nhận diện, lỗi mạng/phạm vi phiên và cleanup dữ liệu riêng/realtime |
| UC-ACC-12 | ACC-015, ACL-02; [AC-DATA-02/03, TC-DATA-01](../../data-lifecycle.md#acceptance) | RB-ACCOUNT và enum/status trong Identity; chưa có API/CLI khóa/mở khóa | Chưa có test quy trình khóa/mở khóa; kiểm tra Bearer hiện có chỉ là một phần guard | Công cụ, phân quyền, audit/receipt và status policy cụ thể còn OQ-011; cần chứng minh thu hồi/cutoff, giữ lịch sử và unlock không khôi phục phiên |

Trong ma trận, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`; dùng dạng ngắn để dễ đọc. Assertion status 200/204 không đủ chứng minh mọi hậu điều kiện. Toàn bộ UC/AC/TC vẫn chưa có kết quả chạy trong bước tài liệu này; các điểm thiếu là đầu vào cho gói triển khai và kiểm thử sau khi người dùng xác nhận.

<a id="use-case-review"></a>

### Điểm cần xác nhận và phụ thuộc triển khai

| Nội dung | Căn cứ hiện có | Trạng thái / việc cần làm |
|---|---|---|
| Mật khẩu mới trùng mật khẩu hiện tại — UC-ACC-06/08 | Người dùng chọn giữ hành vi hiện tại ngày 2026-10-05: đổi từ chối trùng, reset cho phép trùng. | Đã chốt [DEC-113](../../decisions.md#dec-113); ACC-016, AC-ACC-22 và TC-ACC-19 ghi rõ kết quả/thu hồi phiên. Chưa có assertion tự động cho trường hợp trùng. |
| Email dùng được — UC-ACC-01/03/05 | ACC-GAP-05 và thiết kế delivery/envelope đã có; chưa có worker/provider. | OQ-002/OQ-008: cần chọn provider, public origin/domain và nơi lưu key trước triển khai giao email thật; tiếp nhận outbox chưa chứng minh email đã giao. |
| Limiter bổ sung | Cooldown 60 giây và lockout 5 lần/15 phút đã chốt; ngưỡng theo nguồn/tài khoản chỉ là đề xuất DEC-089. | Giữ trạng thái hoãn; không tự thêm ngưỡng thành điều kiện nghiệm thu của UC. |
| Thu hồi trên kết nối đang mở — UC-ACC-08–11 | HTTP đã kiểm tra session/stamp; DEC-083 yêu cầu kết nối chat ngừng nhận dữ liệu trong ≤5 giây sau commit thu hồi. | Tích hợp và kiểm chứng cùng DM; test Identity HTTP và Client mô phỏng chưa đủ chứng minh. |
| Khóa/mở khóa quản trị | DEC-104/112 và RB-ACCOUNT đã chốt hành vi/phạm vi; chưa có công cụ thật. | Theo dõi ở OQ-011/runbook; không thêm UI quản trị hoặc tự coi verify/reset là thao tác mở khóa. |

Rà soát độ phủ use case ngày 2026-10-05: 12 UC có đủ tác nhân, điều kiện trước, luồng chính, ngoại lệ và kết quả sau cùng; 16 quy tắc ACC được truy vết ở [bảng quy tắc](specs/use-cases.md#use-case-rules), 22 AC và 19 TC tài khoản được dẫn chiếu trong [ma trận](#use-case-coverage). Các lựa chọn còn mở ở bảng trên thuộc thiết kế/triển khai; hồ sơ ghi rõ phạm vi hành vi đã chốt và không coi chúng là chức năng đã chạy hoặc bằng chứng nghiệm thu sản phẩm.

### Chênh lệch và bằng chứng còn thiếu

Kết quả đọc source ngày 2026-10-04, không phải kết quả chạy test. ACC-GAP theo dõi chênh lệch kỹ thuật, không tạo quyết định sản phẩm mới.

| Mã | Yêu cầu/căn cứ | Source hiện tại | Việc phải hoàn tất |
|---|---|---|---|
| ACC-GAP-01 | Khôi phục trước xác minh — ACC-007/014, AC-ACC-10 | `ForgotPasswordAsync` không tạo token nếu `VerifiedAt` null | Cho cấp token đúng yêu cầu; chứng minh reset không tự xác minh bằng TC-ACC-03 |
| ACC-GAP-02 | Gửi lại sau 60 giây, vô hiệu link cũ — ACC-012 | Chưa có resend verification hoặc cooldown reset; reset request mới đã vô hiệu token reset cũ | Thiết kế/cài đặt cooldown và resend; kiểm chứng request đồng thời |
| ACC-GAP-03 | Xác minh bằng token dùng một lần — ACC-012 | Verify đọc token/lưu nhưng không có khóa hoặc conditional consume như reset | Thử verify đồng thời; nếu nhiều request cùng thành công thì sửa consume; chưa có bằng chứng |
| ACC-GAP-04 | Chuẩn hóa định danh nhất quán — ACC-008/009 | Service trim nhưng MVC kiểm tra regex/độ dài trước service | Khóa thứ tự chuẩn hóa/validation HTTP–service–DB; fixture khoảng trắng/hoa thường |
| ACC-GAP-05 | Giao liên kết email dùng được — ACC-012 | Outbox chỉ chứa email/token ID; DB giữ hash, chưa có worker/provider | Thiết kế đưa link/token gửi được tới worker, trạng thái gửi/retry và kiểm thử email thật |
| ACC-GAP-06 | Chống lạm dụng đăng ký/login/reset/resend | Có lockout; chưa có rate limiter theo nguồn yêu cầu/gửi email | Chọn ngưỡng/cửa sổ cấu hình được; đo lỗi 429 và phản hồi không tiết lộ email |
| ACC-GAP-07 | Tên hiển thị UTF-16 — ACC-008, DEC-068 | Service dùng `.Length`; MVC và DB có giới hạn riêng | Fixture tiếng Việt tổ hợp/emoji và biên 64; kiểm chứng HTTP–DB |

Rà soát ngày 2026-10-05 bổ sung chênh lệch giao diện tại [ma trận use case](#use-case-coverage): tự verify bằng token Development, thiếu trang verify/reset, chưa cho sửa locale, chưa xóa phiên ngay sau đổi mật khẩu, danh sách phiên dùng dữ liệu mẫu khi lỗi/rỗng và nhãn logout-all sai phạm vi. Các chênh lệch này cần triển khai/kiểm chứng cùng UC tương ứng; chưa sửa mã trong bước tài liệu.

### Đối chiếu bộ test tự động hiện có

Đọc source ngày 2026-10-04; chưa chạy test trong lần hoàn thiện tài liệu này. Một test có assertion chỉ là bằng chứng có ca tự động, không phải kết quả đạt.

| Test hiện có | Hành vi có assertion | Coverage còn thiếu |
|---|---|---|
| `Identity_v1_supports_the_complete_password_account_lifecycle` | Đăng ký, từ chối login trước verify, verify, login bằng username, hồ sơ, rotation/reuse, reset/đổi mật khẩu, phiên/revoke/logout-all/logout | Login bằng email; reset trước verify; boundary/cooldown/resend; link hết hạn/verify đồng thời; email thật |
| `Changing_password_invalidates_previously_issued_reset_tokens` | Đổi mật khẩu vô hiệu token reset cũ | Token verify phải độc lập; kiểm thử email đến trễ |
| `A_concurrent_login_cannot_survive_a_password_change` / `A_login_waiting_for_a_password_change_rechecks_the_password` | Hai thứ tự chạy login và đổi/reset; phiên/mật khẩu cũ không còn dùng được | Hành trình nhiều tab và kết nối realtime |
| `Parallel_wrong_passwords_are_counted_and_trigger_lockout` | Đếm đúng request sai đồng thời, khóa đăng nhập | Biên hết khóa và giới hạn theo nguồn |
| `Concurrent_reset_requests_can_consume_a_token_only_once` | Chỉ một reset dùng cùng token thành công | Verify đồng thời và resend |

Nguồn: [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [IdentityConcurrencyTests](../../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). Các TC vẫn là Chưa chạy; ca phụ thuộc implementation thiếu ghi rõ ACC-GAP tương ứng. Dữ liệu mật khẩu/token phải dùng môi trường thử riêng.

Ngoài các ca trên, kiểm tra trường `displayName` thiếu/rỗng/vượt 64, password sai policy, rotation/reuse refresh, logout-all, revoke session và đổi mật khẩu. Bộ test tự động trong repo: [IdentityV1FlowTests](../../../tests/SCDC.Api.Tests/Identity/IdentityV1FlowTests.cs), [IdentityConcurrencyTests](../../../tests/SCDC.Api.Tests/Identity/IdentityConcurrencyTests.cs). Cần đối chiếu coverage thực tế với AC/TC, không suy từ tên test.

Chạy hành trình trên desktop và trình duyệt điện thoại theo DEC-059; ma trận theo DEC-082; ghi phiên bản/thiết bị/build thực tế khi chạy. Mỗi kết quả ghi theo [mẫu nghiệm thu](../../release-operations.md#testing).

# SCDC — Trạng thái và khóa tài khoản

Phạm vi UC-ACC-12 và điều kiện tài khoản dùng chung cho mọi chức năng. Khóa quản trị khác lockout login 15 phút. Quyền kỹ thuật không cho đọc DM hoặc sửa/xóa tin thay tác giả.

## Mục lục

- [Quy tắc và quyền](#rules)
- [Trạng thái, dữ liệu và API](#states)
- [Khóa và mở khóa](#flows)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc/hành vi | Căn cứ |
|---|---|---|
| ACC-005 | Người dùng cần xác minh email trước khi gửi tin riêng hoặc tin trong phòng. | DEC-041 |
| ACC-007 | Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không được vào ứng dụng; đặt lại mật khẩu không thay thế xác minh email. | DEC-051; hệ quả của việc tách hai mục đích |
| ACC-015 | v1 chưa có tự xóa account; khóa chặn ứng dụng/thu hồi phiên nhưng giữ lịch sử, mở khóa cần phiên đăng nhập mới. Khóa không thay lockout 15 phút. | DEC-103/104; công cụ/thẩm quyền khóa còn OQ-011 |

[Vòng đời dữ liệu](../../system/data-lifecycle.md#inventory) chốt TTL log/audit/chi tiết terminal và restore DEC-106–109; giữ active refresh family/cooldown/stamp, không dọn marker thu hồi theo TTL payload. Self-delete chưa thuộc v1; enum Deleted hiện tại không chứng minh có luồng xóa/anonymize.

<a id="permissions"></a>

### Quyền

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-01 | Đọc/sửa hồ sơ riêng, quản lý phiên của mình | Đã xác thực; đối tượng thuộc chính tài khoản | Dùng định danh của tài khoản khác | ACC-003, DEC-065/066 |
| ACL-02 | Khóa/mở khóa tài khoản qua quy trình kỹ thuật | Có quyết định và người thực hiện được phân quyền đúng tài khoản/phạm vi, có audit theo RB-ACCOUNT | Chưa được cấp quyền, sai phạm vi hoặc thiếu quyết định; quyền vận hành không cho đọc DM/sửa/xóa tin thay tác giả | ACC-015, DEC-104/112; người/vai trò/công cụ cụ thể còn OQ-011 |

Tài khoản chưa xác minh chỉ dùng xác minh/khôi phục, không có phiên truy cập ứng dụng (DEC-051). Đặt lại mật khẩu không tự xác minh email. Quyền tìm người/DM ở [đặc tả DM](../messaging/direct-messaging.md#permissions); quyền phòng ở [Community Permissions](../community/access-control.md#permissions).

<a id="states"></a>

## Trạng thái, dữ liệu và API

Bảng tài khoản mô tả yêu cầu đã chốt. `Suspended`, `Disabled`, `Deleted` có trong enum/SQL; DEC-104/112 đã chốt hành vi khóa/mở khóa qua quy trình kỹ thuật có phân quyền/audit và chưa có UI quản trị riêng. [RB-ACCOUNT](../../guides/operations.md#account-support) là thiết kế mục tiêu; chưa có công cụ chuyển trạng thái được triển khai. Không tự suy Deleted thành tính năng xóa tài khoản v1 DEC-103.

| Trạng thái đầu | Thao tác/điều kiện | Kết quả cần có |
|---|---|---|
| Chưa có tài khoản | Đăng ký hợp lệ, email/username không trùng | Tạo tài khoản chờ xác minh và yêu cầu email; chưa cấp phiên ứng dụng |
| Chờ xác minh | Liên kết xác minh còn hạn, đúng mục đích, chưa dùng | Email được xác minh, tài khoản active; đăng nhập để nhận phiên |
| Chờ xác minh | Đăng nhập với đúng mật khẩu | 403 `Identity.EmailNotVerified`, không cấp phiên |
| Chờ xác minh | Khôi phục/đặt lại mật khẩu qua email | Đổi mật khẩu, vẫn chờ xác minh; source hiện chưa cấp liên kết cho trường hợp này |
| Active | Đăng nhập đúng, không bị khóa | Cấp một phiên cùng access/refresh token |
| Active | Đặt lại/đổi mật khẩu thành công | Mật khẩu mới có hiệu lực; mọi phiên cũ bị thu hồi; email giữ trạng thái xác minh trước đó |
| Active | Đủ 5 lần sai theo DEC-064 | Khóa đăng nhập 15 phút; account vẫn active, lockout nằm trong security state |

Identity sở hữu trạng thái user, email verified và security stamp; HTTP Bearer kiểm tra mỗi request. Chưa có API/CLI chuyển trạng thái hoặc trang quản trị. Định danh/status trong enum/SQL không chứng minh luồng tự xóa/anonymize.

<a id="flows"></a>

## Khóa và mở khóa

<a id="uc-uc-acc-12"></a>

<a id="uc-uc-acc-12--khóamở-khóa-tài-khoản-qua-quy-trình-kỹ-thuật"></a>

### UC-ACC-12 — Khóa/mở khóa tài khoản qua quy trình kỹ thuật

**Tác nhân:** Người quyết định và người thực hiện được phân quyền theo [RB-ACCOUNT](../../guides/operations.md#account-support). Người/vai trò cụ thể còn OQ-011; tài liệu này không chỉ định hoặc cấp quyền cho bất kỳ ai.

**Điều kiện trước:** Có yêu cầu/hồ sơ xử lý, user ID ổn định, môi trường/phạm vi đích và quyết định khóa hoặc mở khóa. Người thực hiện có quyền tương ứng; công cụ kỹ thuật phải được triển khai và kiểm chứng trước sử dụng.

**Luồng chính:**

1. Người thực hiện mở hồ sơ, đối chiếu quyết định, user ID, trạng thái/version hiện hành và phạm vi được cấp quyền; không thu thập mật khẩu, token hoặc nội dung DM vào hồ sơ.
2. Khi khóa, công cụ ghi trạng thái khóa và audit/receipt cần thiết, đổi security stamp và thu hồi toàn bộ phiên/token ứng dụng cùng transaction. Kiểm tra HTTP và cutoff chat/media theo [quy tắc khóa tài khoản](../../system/data-lifecycle.md#account-state).
3. Khi mở khóa theo quyết định, công cụ ghi trạng thái phù hợp và audit. Người dùng phải đăng nhập mới, hoàn tất xác minh nếu còn thiếu và được kiểm tra quyền hiện hành; phiên cũ không được khôi phục.
4. Người thực hiện ghi kết quả đối soát vào hồ sơ: đúng tài khoản/phạm vi, hiệu lực thu hồi, dữ liệu lịch sử được giữ và quyền của các actor khác không bị cấp thêm.

**Ngoại lệ:** Thiếu quyền/quyết định, sai user ID hoặc trạng thái/version không còn phù hợp thì từ chối thao tác và đối chiếu lại; không thay đổi tài khoản. Lỗi transaction không để lại thay đổi từng phần. Mất response sau commit phải đối soát trạng thái/audit trước thử lại; chưa kiểm chứng cutoff thì chưa ghi đạt. Không dùng reset/verify để mở khóa; mất email không tạo kênh khôi phục thủ công.

**Kết quả sau cùng:** Tài khoản bị khóa không truy cập ứng dụng hoặc gửi/gọi; các phiên cũ bị từ chối và kết nối chat/media dừng trong ≤5 giây từ commit theo DEC-083/099. Người khác còn quyền vẫn đọc lịch sử và sửa/xóa tin của chính mình; không gửi DM/gọi mới tới peer bị khóa. Hồ sơ, tin, membership và ownership không bị xóa/chuyển vì thao tác khóa. Mở khóa không làm sống lại phiên hoặc tự xác minh email. Quyền kỹ thuật không cho đọc DM, sửa/xóa thay tác giả; v1 không có self-delete hoặc trang quản trị riêng.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-ACC-07 | Tài khoản chưa xác minh nhập đúng mật khẩu hoặc gọi API ứng dụng trực tiếp. | Không được vào ứng dụng, tìm người, đọc hoặc gửi tin; có đường xác minh/khôi phục. |

Mọi AC/TC dưới đây cần kết quả chạy gắn commit/build; danh mục ca và assertion trong source không phải kết quả nghiệm thu. A là tài khoản đã xác minh, U chưa xác minh, A1/A2 là hai phiên của A. Dùng DB/email thử riêng và phiên trình duyệt độc lập.

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-02 | U nhập đúng mật khẩu; thử mở hội thoại/tìm người/API ứng dụng | Không truy cập ứng dụng; có đường xác minh/khôi phục | AC-ACC-07, DEC-051 |

UC-ACC-12 áp dụng AC-DATA-02/03 và TC-DATA-01 trong [vòng đời dữ liệu](../../system/data-lifecycle.md#acceptance). Phải chứng minh khóa/thu hồi/cutoff, giữ lịch sử và unlock không khôi phục phiên; verify/reset không là thao tác mở khóa.

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu source/test ngày 2026-10-04/05; lần tổ chức tài liệu này không chạy lại test hoặc xác nhận nghiệm thu. Trong bảng, `AC-01` là `AC-ACC-01`, `TC-01` là `TC-ACC-01`. Assertion 200/204 chưa chứng minh mọi hậu điều kiện.

| UC | AC / TC liên quan | Source / API hiện có | Assertion tự động hiện có | Chênh lệch hoặc bằng chứng cần bổ sung |
|---|---|---|---|---|
| UC-ACC-12 | ACC-015, ACL-02; [AC-DATA-02/03, TC-DATA-01](../../system/data-lifecycle.md#acceptance) | RB-ACCOUNT và enum/status trong Identity; chưa có API/CLI khóa/mở khóa | Chưa có test quy trình khóa/mở khóa; kiểm tra Bearer hiện có chỉ là một phần guard | Công cụ, phân quyền, audit/receipt và status policy cụ thể còn OQ-011; cần chứng minh thu hồi/cutoff, giữ lịch sử và unlock không khôi phục phiên |

DEC-104/112 và RB-ACCOUNT đã chốt hành vi/phạm vi; công cụ, người/vai trò, audit/receipt và status policy cụ thể còn OQ-011. Bearer guard hiện có chỉ là một phần, chưa có test quy trình khóa/mở khóa.

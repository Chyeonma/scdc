# SCDC — Trạng thái và khóa tài khoản

Trang này mô tả các trạng thái của một tài khoản (chờ xác minh, đang hoạt động, bị khóa) và quy trình khóa, mở khóa tài khoản. Các điều kiện này áp dụng cho mọi chức năng.

**Trạng thái:** quy tắc đã chốt. Chưa có công cụ khóa hay mở khóa: chưa có API, CLI hay trang quản trị. Các giá trị `Suspended`, `Disabled`, `Deleted` có trong enum và SQL nhưng chưa có luồng nào dùng.

Cần phân biệt hai loại khóa:

- **Khóa đăng nhập tạm thời:** sau 5 lần nhập sai mật khẩu, khóa 15 phút. Tài khoản vẫn hoạt động.
- **Khóa tài khoản:** do người vận hành thực hiện theo quyết định. Trang này nói về loại này.

<a id="rules"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| ACC-005 | Phải xác minh email trước khi gửi tin riêng hoặc tin trong phòng. |
| ACC-007 | Tài khoản chưa xác minh chỉ dùng được chức năng xác minh và khôi phục, không vào được ứng dụng. Đặt lại mật khẩu không thay cho xác minh email. |
| ACC-015 | v1 không có chức năng tự xóa tài khoản. Khóa tài khoản chặn vào ứng dụng và thu hồi mọi phiên nhưng giữ lịch sử. Sau khi mở khóa, người dùng phải đăng nhập lại. Khóa tài khoản không thay cho khóa đăng nhập 15 phút. |

Thời hạn lưu log, audit và dữ liệu đã kết thúc xem [vòng đời dữ liệu](../../system/data-lifecycle.md#inventory). Các dấu hiệu thu hồi (security stamp, refresh token đang dùng, thời gian chờ) không bị xóa theo thời hạn đó. Backup và khôi phục làm [sau v1](../../project/overview.md#backup-scope).

<a id="permissions"></a>

### Quyền

| Mã | Thao tác | Được phép khi | Bị từ chối khi |
|---|---|---|---|
| ACL-01 | Xem, sửa hồ sơ của mình; quản lý phiên của mình | Đã đăng nhập, và đối tượng thuộc chính tài khoản đó | Dùng ID của tài khoản khác |
| ACL-02 | Khóa, mở khóa tài khoản qua quy trình kỹ thuật | Có quyết định; người thực hiện được phân quyền đúng tài khoản và phạm vi; có ghi audit theo RB-ACCOUNT | Chưa được cấp quyền, sai phạm vi, hoặc thiếu quyết định |

Quyền vận hành không cho phép đọc DM hay sửa, xóa tin thay người viết. Quyền tìm người và DM xem [nhắn tin riêng](../messaging/direct-messaging.md#permissions); quyền phòng xem [quyền truy cập](../community/access-control.md#permissions).

<a id="states"></a>

## Các trạng thái

| Trạng thái hiện tại | Sự kiện | Kết quả |
|---|---|---|
| Chưa có tài khoản | Đăng ký hợp lệ, email và username chưa ai dùng | Tạo tài khoản chờ xác minh, gửi email; chưa cấp phiên |
| Chờ xác minh | Bấm link xác minh còn hạn, đúng mục đích, chưa dùng | Email được xác minh, tài khoản hoạt động; đăng nhập để nhận phiên |
| Chờ xác minh | Đăng nhập đúng mật khẩu | `403 Identity.EmailNotVerified`, không cấp phiên |
| Chờ xác minh | Đặt lại mật khẩu qua email | Mật khẩu đổi, vẫn chờ xác minh. Code hiện chưa gửi link cho trường hợp này |
| Hoạt động | Đăng nhập đúng, không bị khóa | Cấp một phiên gồm access token và refresh token |
| Hoạt động | Đổi hoặc đặt lại mật khẩu thành công | Mật khẩu mới có hiệu lực; mọi phiên cũ bị thu hồi; trạng thái xác minh email giữ nguyên |
| Hoạt động | Sai mật khẩu 5 lần | Khóa đăng nhập 15 phút; tài khoản vẫn hoạt động |

Identity sở hữu trạng thái tài khoản, trạng thái xác minh email và security stamp. Mỗi request HTTP kiểm tra token Bearer.

<a id="flows"></a>

## Khóa và mở khóa

<a id="uc-uc-acc-12"></a>

<a id="uc-uc-acc-12--khóamở-khóa-tài-khoản-qua-quy-trình-kỹ-thuật"></a>

### UC-ACC-12 — Khóa và mở khóa tài khoản

**Ai làm:** người quyết định và người thực hiện được phân quyền theo [RB-ACCOUNT](../../guides/operations.md#account-support). Chưa chỉ định người cụ thể (OQ-011).

**Điều kiện trước:** có yêu cầu và hồ sơ xử lý, user ID, môi trường và phạm vi đích, quyết định khóa hoặc mở khóa. Người thực hiện có quyền tương ứng. Công cụ phải được làm và kiểm thử trước khi dùng.

**Các bước:**

1. Mở hồ sơ, đối chiếu quyết định, user ID, trạng thái và phiên bản hiện tại, phạm vi được cấp quyền. Không ghi mật khẩu, token hay nội dung DM vào hồ sơ.
2. **Khi khóa:** trong cùng một transaction, ghi trạng thái khóa và audit, đổi security stamp, thu hồi mọi phiên và token. Kiểm tra HTTP bị từ chối và kết nối chat, media bị cắt theo [quy tắc khóa](../../system/data-lifecycle.md#account-state).
3. **Khi mở khóa:** ghi trạng thái và audit. Người dùng phải đăng nhập lại, hoàn tất xác minh nếu còn thiếu, và được kiểm tra quyền hiện tại. Phiên cũ không được khôi phục.
4. Ghi kết quả đối chiếu vào hồ sơ: đúng tài khoản và phạm vi, thu hồi có hiệu lực, lịch sử được giữ, người khác không được thêm quyền.

**Ngoại lệ:**

- Thiếu quyền hoặc quyết định, sai user ID, trạng thái hoặc phiên bản đã đổi: từ chối và đối chiếu lại; tài khoản không đổi.
- Lỗi transaction không để lại thay đổi dở dang.
- Mất phản hồi sau khi commit: đối chiếu trạng thái và audit trước khi thử lại.
- Chưa kiểm tra việc cắt kết nối thì chưa ghi là đạt.
- Không dùng đặt lại mật khẩu hay xác minh email để mở khóa. Mất email không tạo kênh khôi phục thủ công.

**Kết quả:**

- Tài khoản bị khóa không vào được ứng dụng, không gửi tin, không gọi. Phiên cũ bị từ chối; kết nối chat và media dừng trong 5 giây kể từ commit.
- Người khác vẫn đọc được lịch sử và sửa, xóa tin của chính họ; nhưng không gửi DM hay gọi mới cho tài khoản bị khóa.
- Hồ sơ, tin nhắn, tư cách thành viên và quyền chủ sở hữu không bị xóa hay chuyển.
- Mở khóa không khôi phục phiên cũ và không tự xác minh email.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-07 | Tài khoản chưa xác minh đăng nhập đúng mật khẩu, hoặc gọi API ứng dụng trực tiếp | Không vào được ứng dụng, không tìm người, không đọc hay gửi tin; có đường xác minh và khôi phục |

Ký hiệu: A là tài khoản đã xác minh, U là tài khoản chưa xác minh, A1 và A2 là hai phiên của A. Dùng DB và email thử riêng, mỗi phiên một cửa sổ trình duyệt riêng.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-ACC-02 | U đăng nhập đúng mật khẩu; thử mở cuộc trò chuyện, tìm người, gọi API | Không vào được; có đường xác minh và khôi phục | AC-ACC-07 |

UC-ACC-12 dùng AC-DATA-02, 03 và TC-DATA-01 trong [vòng đời dữ liệu](../../system/data-lifecycle.md#acceptance). Phải chứng minh: khóa thì thu hồi và cắt kết nối, lịch sử được giữ, mở khóa không khôi phục phiên.

Kết quả phải ghi kèm commit và build. Danh sách ca hay assertion trong code không phải kết quả nghiệm thu.

<a id="status"></a>

## Hiện trạng

Đối chiếu với [nền MVP `900e226`](../../releases/mvp.md) ngày 2026-10-13. Bảng test của Identity giữ phần rà soát ngày 2026-10-04 và 05. Lần cập nhật này không chạy lại test.

| UC | Liên quan | Code hiện có | Test tự động | Còn thiếu |
|---|---|---|---|---|
| UC-ACC-12 | ACC-015, ACL-02; [AC-DATA-02, 03, TC-DATA-01](../../system/data-lifecycle.md#acceptance) | RB-ACCOUNT, enum trạng thái trong Identity; chưa có API hay CLI khóa | Chưa có; kiểm tra Bearer chỉ là một phần | Công cụ, phân quyền, audit và quy tắc trạng thái (OQ-011); bằng chứng thu hồi, giữ lịch sử, mở khóa không khôi phục phiên |

Nền `900e226` đã kiểm tra phiên và tài khoản cho DM. [Hồ sơ DM](../../records/verification/messaging/DM-MVP.md) có các ca người kia bị vô hiệu hóa, lịch sử, sửa tin của tác giả, và cắt kết nối cũ; một số trạng thái được dựng thẳng trong DB thử. Đó không phải công cụ khóa được phép dùng, cũng không chứng minh hoàn tất UC-ACC-12, thu hồi quyền phòng hay Media.

## Còn mở

- Công cụ, người và vai trò thực hiện, audit, quy tắc trạng thái cụ thể (OQ-011).
- Giá trị `Deleted` trong enum không có nghĩa v1 có chức năng xóa hay ẩn danh tài khoản.

---

Nguồn: DEC-041 (ACC-005), DEC-051 (ACC-007), DEC-064 (khóa đăng nhập), DEC-065, DEC-066 (ACL-01), DEC-083, DEC-099 (cắt kết nối), DEC-103, DEC-104, DEC-112 (khóa tài khoản, không có trang quản trị), DEC-106, DEC-107 (thời hạn lưu), DEC-108, DEC-109, DEC-156 (backup sau v1).

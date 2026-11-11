<a id="scdc--vòng-đời-dữ-liệu"></a>

# SCDC — Vòng đời dữ liệu

Trang này quy định dữ liệu nào được giữ bao lâu, ai được đọc, điều gì xảy ra khi khóa tài khoản, xóa tin hay xóa phòng, cách dọn dữ liệu kỹ thuật, và cách bảo vệ dữ liệu khi khôi phục từ backup. Áp dụng cho Tài khoản, DM, Community và Media. Các trang tính năng dẫn về đây.

**Trạng thái (2026-10-13, nền `900e226`):** chính sách đã chốt; chưa có worker dọn dữ liệu, sổ bảo vệ hay kết quả khôi phục.

- **Thuộc v1:** mọi chính sách dữ liệu online (mục 1 đến 5).
- **Sau v1** ([DEC-156](../records/decisions/README.md#dec-156)): backup, khôi phục, sổ bảo vệ độc lập, placeholder sau khôi phục (mục 6). Thiết kế giữ lại để làm sau.
- Transaction và guard cần xem lại khi tách microservice (DEC-116).

<a id="mục-lục"></a>

<a id="policy"></a>

<a id="1-phạm-vi-và-quyết-định"></a>

## 1. Chính sách

| Dữ liệu | Quy tắc |
|---|---|
| Tin đang lưu | DM và tin phòng không tự hết hạn (DEC-070). Chỉ giữ nội dung mới nhất; xóa thì để lại tombstone (DEC-020, 034, 052) |
| Tài khoản | v1 không có tự xóa tài khoản (DEC-103). Khóa tài khoản không xóa tin; chặn dùng ứng dụng và tương tác mới; mở khóa phải đăng nhập lại (DEC-104) |
| Phòng đã xóa | Không truy cập, không khôi phục (DEC-077). Giữ nội dung, chưa đặt hạn xóa hẳn (DEC-105). v1 không có xóa server (DEC-094) |
| Bộ nhớ tab | Bản nháp DM chỉ ở RAM (DEC-091); đóng tab, tải lại, đăng xuất thì mất. Token phiên theo thiết kế Tài khoản |
| Log, audit | Log kỹ thuật 14 ngày; audit 90 ngày; IP và user-agent trong audit 7 ngày (DEC-106) |
| Dữ liệu kỹ thuật đã kết thúc | Dọn phần nội dung sau 7 ngày; giữ dấu chống trùng, tombstone và chuỗi refresh token đang dùng (DEC-107) |
| Backup (sau v1) | Mục tiêu RPO ≤ 15 phút, RTO ≤ 4 giờ (DEC-086); mỗi bản backup và WAL giữ tối đa 30 ngày (DEC-109). Không hứa khôi phục được mọi thời điểm trong 30 ngày |
| Khôi phục (sau v1) | Áp lại các thao tác xóa và thu hồi. Nếu mất bản sửa mới nhất, hiện "Nội dung chưa khôi phục được", không đưa bản cũ trở lại (DEC-108) |

**Cách hiểu:**

- Các mốc thời gian giữ log và dọn dữ liệu không thay đổi hạn của token hay thời gian thu hồi chat, media.
- "Giữ dữ liệu" không cho người đã rời, đã mất quyền hay người vận hành được đọc.
- "Xóa nội dung" nghĩa là bỏ khỏi bảng và đường phục vụ của ứng dụng; không hứa ghi đè ngay mọi byte cũ trên đĩa, WAL hay backup.

**Không có trong v1:** tự xóa tài khoản, tự xóa tài khoản chưa xác minh, xóa cộng đồng, xuất toàn bộ dữ liệu. Quản trị qua quy trình kỹ thuật có phân quyền và audit, không có trang quản trị (DEC-112). Người có quyền khóa, mở khóa và người trực chưa chọn (OQ-011); xem [RB-ACCOUNT](../guides/operations.md#account-support).

<a id="inventory"></a>

<a id="2-ma-trận-dữ-liệu-và-đường-truy-cập"></a>

## 2. Bảng dữ liệu

Thời hạn gắn với **loại dữ liệu** và **sự kiện bắt đầu tính**, không dùng một TTL chung cho cả DB. Job dọn chỉ xử lý dữ liệu của module mình; không xóa trực tiếp dòng Identity đang được Messaging hay Community tham chiếu.

| Dữ liệu | Ai được đọc | Giữ online | Backup và dấu tối thiểu |
|---|---|---|---|
| Người dùng, email, username, hash mật khẩu, hồ sơ | Chủ tài khoản; thông tin công khai được phép (không email, không hash) | Giữ tài khoản. Khóa không xóa hay ẩn danh, không giải phóng username và email | Không có job tự xóa tài khoản |
| Phiên, chuỗi refresh token | Identity; chủ phiên (không thấy token) | Còn hạn tới khi hết hạn hoặc bị thu hồi; chi tiết đã kết thúc giữ thêm 7 ngày. Không dọn token đã dùng của chuỗi đang dùng | Sau khi dọn, token cũ vẫn không hợp lệ; security stamp và mốc phiên không mất |
| Token xác minh, đặt lại | Identity và worker đúng loại | Hiệu lực 30 phút, chờ 60 giây; hash và metadata đã kết thúc giữ thêm 7 ngày | Link từ backup không được hoạt động lại sau khôi phục |
| Email delivery, envelope | Worker Identity; không trả ra API hay log | Envelope không dùng được nữa thì xóa ngay; metadata giữ 7 ngày | Khôi phục không gửi lại thư cũ; hạn vẫn tính theo UTC gốc |
| DM và tin phòng | Người dùng đang hoạt động có quyền đọc (kể cả tin của tác giả đã bị khóa) | Không tự hết hạn; chỉ nội dung mới nhất | Backup có thể chứa bản trước khi sửa hay xóa; sổ bảo vệ chỉ lưu ID, version, hành động |
| Tin tác giả đã xóa | Người có quyền chỉ thấy tombstone | `content=null` từ lúc commit; giữ ID, sequence, version, khóa chống trùng | Dọn dữ liệu hay khôi phục không làm mất tombstone và chống trùng |
| Phòng đã xóa và tin trong đó | Không có API hay giao diện đọc lại trong v1 | Giữ nội dung, chưa có hạn xóa | Giữ ID và epoch đã xóa |
| Thành viên, vai trò, ACL, lời mời | Người có quyền tại thời điểm thao tác | Rời thì mất quyền ngay; vai trò và ngoại lệ của epoch cũ được dọn; metadata lịch sử chưa có hạn riêng | Giữ dấu thu hồi và epoch để chặn dùng lại. Link của người tạo đã mất quyền không tự mất hiệu lực (DEC-097) |
| Khóa chống trùng (DM, Community, Media) | Module thực thi; không có endpoint đọc | Giữ ID, hash, phiên bản khóa, kết quả tối thiểu khi còn cần chặn gửi lại | Không dọn theo hạn 14 hay 90 ngày của log và audit |
| Cuộc gọi, lượt tham gia, nguồn phát đã kết thúc | Theo quyền; không tạo cuộc gọi nhỡ trong DM | Chi tiết giữ 7 ngày từ khi kết thúc và đã dừng hẳn; giữ dấu vô hiệu và chống trùng | Khôi phục thì kết thúc trạng thái media cũ, chặn LiveKit cũ rồi mới cấp mới |
| Outbox, inbox đã xử lý | Worker của module; ID và trạng thái cho vận hành | Nội dung giữ 7 ngày sau khi kết thúc; giữ biên nhận chống trùng | Không chạy lại thao tác, email hay thông báo từ bản cũ |
| Log kỹ thuật | Người kỹ thuật được phân quyền | 14 ngày; không chứa nội dung chat hay token | Kho log có thời hạn riêng, không theo backup DB |
| Audit bảo mật và quản lý | Người có thẩm quyền, đúng mục đích | 90 ngày; bỏ IP và user-agent sau 7 ngày | Audit không thay sổ bảo vệ |
| Bản nháp, dữ liệu đệm ở client | Tab của người đang dùng | Trong RAM theo phiên; mất quyền hay đăng xuất thì dọn | Không lưu nội dung DM xuống trình duyệt |
| Cache, registry, cursor ở server | Module và kết nối đúng quyền | Chỉ để tối ưu; luôn kiểm tra quyền hiện tại; cursor không chứa nội dung | Khôi phục thì đổi thế hệ và hủy cache; cursor cũ không mang quyền |

### Log và audit được ghi gì

- **Được ghi:** ID và loại sự kiện, ID người thực hiện và đối tượng, ID phạm vi, trạng thái, version, thời điểm, mã lý do, trace ID.
- **Không được ghi:** mật khẩu; access, refresh, media hay link token; hash token; email envelope; nội dung tin; âm thanh, hình, màn hình; body request; query hay fragment URL nhạy cảm. Lý do dùng mã, không cho nhập tự do (để không chép DM vào audit).
- Áp dụng cho cả reverse proxy, `access_token` trong query SignalR, nhà cung cấp, TURN và log chẩn đoán của SDK, không chỉ `ILogger` của API.
- IP và user-agent của phiên (hiện ở màn hình thiết bị) khác với bản trong audit; giữ theo vòng đời phiên.
- Không sửa audit bằng tay. Job dọn được phép ẩn trường nhạy cảm hay xóa dòng tới hạn, và tự ghi audit số lượng và hành động (không chép lại trường vừa dọn).

### Cách tính thời hạn

- Tính từ thời điểm nghiệp vụ gốc; không đặt lại khi khôi phục, chuyển kho, gửi lại hay sao chép.
- Backup có thể vẫn chứa dữ liệu đã dọn online cho tới khi bản backup hết hạn. Tuổi bản backup khác tuổi dữ liệu bên trong.
- Dọn ở ứng dụng không chứng minh byte đã mất khỏi đĩa hay WAL; phải kiểm tra riêng.

<a id="tuổi-backup-và-cửa-sổ-pitr"></a>

### Tuổi backup và khả năng khôi phục theo thời điểm (sau v1)

- Mỗi bản backup và đoạn WAL giữ tối đa 30 × 24 giờ (DEC-109), không kéo dài dù cần một bản base cũ.
- Tuổi tính từ lúc bắt đầu base backup hoặc lúc đóng đoạn WAL (theo catalogue đã xác thực), không theo lúc sao chép hay tải lên.
- Giữ các base backup đã kiểm tra cùng chuỗi WAL của chúng trong giới hạn tuổi. Khi một bộ hết hạn, bỏ bộ đó và các đoạn WAL không còn được bộ hợp lệ nào cần.
- Phải luôn có base mới hợp lệ trước khi bộ cũ hết hạn.
- Thời điểm sớm nhất khôi phục được là base hợp lệ sớm nhất có WAL liên tục, không nhất thiết là "30 ngày trước". Báo cáo phải ghi thời điểm sớm nhất, muộn nhất và đoạn WAL bị thiếu.
- Chưa có base mới đúng hạn thì báo chưa đạt; không tự giữ backup quá tuổi hay xóa WAL đang cần.

<a id="account-state"></a>

<a id="3-khóa-tài-khoản-lịch-sử-và-định-danh"></a>

## 3. Khóa tài khoản và lịch sử

"Khóa tài khoản" (DEC-104) là trạng thái suspended hoặc disabled, mất quyền dùng ứng dụng. Khác với khóa đăng nhập tạm (sai 5 lần, khóa 15 phút, DEC-064): khóa tạm không xóa phiên đang có. Trạng thái cụ thể và quyền người thao tác còn phải review.

| Người dùng / tình huống | Đọc lịch sử | Gửi mới | Sửa, xóa tin của mình |
|---|---|---|---|
| Người bị khóa | Không | Không | Không |
| Người đang hoạt động; người kia trong DM bị khóa | Có, trong cuộc trò chuyện của mình | Không gửi DM, không gọi; tìm người không trả về người bị khóa | Có |
| Người đang hoạt động trong phòng có quyền xem; tác giả cũ bị khóa | Có | Có, theo quyền | Chỉ tin của mình; không sửa, xóa thay tác giả bị khóa |
| Mất quyền xem, rời server, hoặc phòng bị xóa | Không | Không | Không |
| Vừa được mở khóa | Sau khi đăng nhập, xác minh và kiểm tra lại quyền | Theo trạng thái hiện tại | Theo quyền hiện tại; phiên cũ không sống lại |

- Guard tách hai điều kiện: **người thực hiện** phải dùng được ứng dụng; **người kia** phải hoạt động để nhận tương tác mới. Đọc lịch sử và tác giả sửa, xóa không cần người kia còn hoạt động.
- Khóa tài khoản: thu hồi mọi phiên và token, tăng stamp, ngắt chat và media trong 5 giây. Hồ sơ, tin và tư cách thành viên không bị xóa.
- Gửi lại một tin đã commit (đúng khóa và nội dung) chỉ đọc lại kết quả nếu người gửi còn quyền; người kia bị khóa không ảnh hưởng. Khóa mới thì bị chặn như gửi mới.
- **Hiển thị tác giả trong lịch sử:** `IHistoricalUserSummaryReader` trả ID, username, tên hiển thị và trạng thái `unavailable` cho người đã là tác giả hay người tham gia trong phạm vi được phép xem. (`IUserDirectory` chỉ trả người đang hoạt động nên không đủ.) Không trả lỗi làm mất cả trang tin; không đưa lý do khóa hay email vào. Tìm người và mở DM mới vẫn chỉ dùng người đang hoạt động.
- Khóa không tự chuyển chủ server, xóa server, hay thu hồi lời mời; mở khóa không phục hồi quyền. Chủ server bị khóa vẫn là chủ trong DB nhưng không thao tác được. Trường hợp chủ không thể quay lại cần quy trình riêng; người vận hành không được tự chuyển chủ.

<a id="deletion"></a>

<a id="4-xóa-tin-xóa-phòng-và-dữ-liệu-tạm"></a>

## 4. Xóa tin, xóa phòng, dữ liệu tạm

**Xóa tin:** kiểm tra phiên và quyền; giữ khóa không gian, tin, thao tác; đặt `content=null` và tăng version đúng một lần. Giữ ID, sequence, tác giả, thời gian, tombstone và fingerprint. Outbox chỉ chứa ID và version. Không dọn bản ghi chống trùng (nếu không, gửi lại cùng `clientMessageId` sẽ tạo tin mới). Bảng cũ `message_edits.previous_content` không được ghi trong v1; migration, seed và import phải loại bỏ bản cũ khỏi DB trước khi dùng dữ liệu thật (DEC-052).

**Xóa phòng:** đánh dấu đã xóa và tăng epoch trong transaction (phối hợp Community, Messaging, Media); guard chặn API, realtime, vào và vào lại. Nội dung giữ lại (DEC-105); v1 không có khôi phục hay đọc kho lưu trữ. Job dọn token và log không được dùng cascade xóa `spaces` và `messages` của phòng. Rời server hay mất quyền xem chỉ thu hồi quyền và dọn vai trò, ngoại lệ của epoch cũ; không xóa tin người đó đã viết.

**Phía client:** khi bị thu hồi, WebClient xóa bản nháp, tin tạm, quyền media và dừng thiết bị của phạm vi đó; server tự ngừng gửi dù client không hợp tác. Không thể thu hồi ảnh chụp, clipboard hay dữ liệu người dùng tự sao chép. Không chuyển token đăng nhập trong localStorage thành nơi lưu DM.

<a id="cleanup"></a>

<a id="5-worker-dọn-dữ-liệu-kỹ-thuật--thiết-kế-đề-xuất"></a>

## 5. Worker dọn dữ liệu kỹ thuật (đề xuất)

**Nguyên tắc:**

- Chỉ áp các thời hạn ở mục 1. Mỗi dòng có `terminal_at`, `effective_until`, `purged_at` theo UTC; đủ điều kiện khi `now >= retain_until`.
- Chuỗi refresh token đang dùng giữ đủ các token đã dùng hay bị thay để phát hiện dùng lại. Chỉ dọn cả chuỗi khi phiên đã kết thúc và không còn ai cần.
- Thời gian chờ, `last_issued_at`, security stamp và dấu chặn không bao giờ bị dọn.
- Email envelope xóa ngay khi token hết hiệu lực hoặc delivery kết thúc; job quét mỗi tối đa 60 giây.
- Không dọn outbox chưa được xác nhận, inbox đang xử lý, delivery chưa kết thúc, chỉ vì đã quá 7 ngày. Media chưa dừng hẳn vẫn giữ chỗ.

**Cách chạy:**

- Chạy theo lô nhỏ, chọn ứng viên bằng index `(retain_until, id)`, giành việc bằng lease có epoch. Mỗi module khóa và đọc lại trạng thái trước khi dọn.
- Dọn một lô rồi chết và chạy lại phải cho cùng kết quả.
- Không tải hết bảng, không log danh sách hay nội dung; chỉ log số lượng, loại, mốc và trace.
- Không gọi LiveKit hay nhà cung cấp email trong transaction dọn.
- Token hết hạn bị từ chối dù worker chưa chạy. Đường đọc metadata, log, audit nên ẩn ngay khi tới hạn, không chờ job. Job chạy trễ phải được báo và đo.

**Tách nội dung khỏi dấu:** outbox, inbox, thao tác có thể bỏ phần nội dung lớn nhưng giữ `operationId, eventId, resourceId, version, fingerprint, keyVersion, terminal, revoked` để chống chạy lại. Các dấu này chưa có hạn riêng, không gộp vào hạn 90 ngày của audit. Muốn dọn dấu phải có cách từ chối mọi ID và thế hệ cũ, và phải có quyết định riêng.

**Khóa ngoại:** nhiều bảng Messaging và Community tham chiếu `identity.users` với RESTRICT; refresh token tham chiếu chính nó với RESTRICT. Vì vậy worker không xóa cứng người dùng hay dọn chuỗi token bằng cascade. Dọn `identity.account_tokens` không đặt lại thời gian chờ. Schema, index, migration và job chưa làm.

<a id="restore"></a>

<a id="6-bảo-vệ-dữ-liệu-sau-restore--thiết-kế-cần-proof"></a>

## 6. Bảo vệ dữ liệu khi khôi phục (sau v1)

Toàn bộ mục này để [sau v1](../project/overview.md#backup-scope). v1 không có quy trình khôi phục từ backup; hàng rào quyền, epoch và dấu chống trùng trong DB vẫn theo [thiết kế thu hồi](access-revocation.md#v1-authorization-coordination).

**Vấn đề:** khôi phục PostgreSQL dùng base backup và chuỗi WAL, có thể về một thời điểm trước hiện tại ([PITR](https://www.postgresql.org/docs/18/continuous-archiving.html)). Như vậy có thể đưa lại nội dung đã xóa hay quyền đã thu hồi. Backup DB không thể là nguồn duy nhất ghi các hành động phải áp lại.

<a id="sổ-bảo-vệ-độc-lập"></a>

### Sổ bảo vệ độc lập

Một kho tách khỏi DB được khôi phục, lưu bản ghi tối thiểu có chữ ký hoặc chuỗi hash và checkpoint:

- **Nội dung bản ghi:** ID thao tác hay sự kiện, loại, ID đối tượng (người dùng, phiên, tin, không gian, phòng, tư cách thành viên), thế hệ, mốc version hay epoch, thời điểm, trạng thái (prepared, committed, aborted).
- **Không chứa:** nội dung tin, email, token, lý do nhập tự do.
- **Thời hạn:** giữ đến khi không còn bản backup, thế hệ hay lần gửi lại nào có thể đưa trạng thái cũ trở lại. Không phải audit 90 ngày.
- Chưa chọn kho, công cụ, quyền, khóa, dung lượng. Schema mục tiêu: [data-protection.schema.json](../contracts/data-protection.schema.json).

**Các hành động cần ghi:** xóa và sửa tin; thu hồi phiên, mọi phiên, đổi stamp mật khẩu; khóa tài khoản; rời server, mất quyền xem, epoch thành viên; đổi vai trò, ACL, chủ sở hữu (làm đổi `accessVersion`); xóa phòng; thu hồi hay hủy lời mời; kết thúc media và chặn thế hệ cũ.

- Sửa tin chỉ ghi ID tin và mốc version, không ghi nội dung. Nếu DB khôi phục có version thấp hơn mốc, không coi nội dung đó là mới nhất.
- Mốc quyền Community đặt ở server, phòng, tư cách thành viên. Nếu chưa khôi phục được cấu hình vai trò, ACL, chủ tương ứng với mốc, thì giữ phạm vi đó đóng cho tới khi đối chiếu xong; chủ từ bản backup cũ không được bỏ qua bước này. Mốc không tự dựng lại cấu hình đã mất. Phải kiểm thử cả chuyển chủ và vai trò deny, không chỉ rời server.

<a id="ghi-sổ-và-trường-hợp-kết-quả-không-rõ"></a>

### Ghi sổ khi kết quả không rõ

Giao thức đề xuất (chưa chứng minh tính nguyên tử giữa hai kho):

1. Trước thao tác xóa, thu hồi hay sửa: ghi bền một ý định "prepared" vào sổ (đối tượng và version mong đợi). Prepared không phải là đã commit và không được tự chạy lại.
2. Trong transaction chính: kiểm tra guard và `expectedVersion`, đổi DB, ghi dấu thao tác cùng outbox. Rollback đã xác nhận thì ghi "aborted"; dấu đã commit là bằng chứng kết quả DB.
3. Sau commit: ghi "committed" vào sổ rồi mới trả thành công. Mất phản hồi hay biên nhận thì có thể đã commit: đọc dấu và gửi lại cùng ID, không tạo thao tác mới, không báo "đã rollback" khi chưa biết. Thời gian thu hồi vẫn tính từ commit DB.
4. Khi khôi phục mà gặp prepared chưa có kết luận: đối chiếu dấu, WAL và bằng chứng tin cậy. Không xác định được thì giữ phạm vi đóng; không tự áp ý định chưa commit để xóa, cũng không tự coi là aborted để trả quyền. RTO 4 giờ phải được thử cả trường hợp này.

- Outbox gửi sổ sau là không đủ: có lúc DB đã xóa nhưng sổ chưa ghi.
- API không được báo đã bảo vệ khi biên nhận chưa bền.
- Sổ lỗi: từ chối thao tác mới cần bảo vệ (trước commit) hoặc trả kết quả chưa rõ (sau commit); không bỏ qua để "ghi sau".

<a id="thứ-tự-khôi-phục-để-diễn-tập"></a>

### Thứ tự khôi phục (để diễn tập)

1. Đóng cổng vào, worker email và outbox, và việc cho vào media. Không khôi phục đè lên DB production đang nhận ghi. Ghi lại mốc khôi phục, timeline, build, schema và mốc của sổ.
2. Khôi phục base và WAL vào môi trường riêng; kiểm tra chuỗi WAL, checksum, dấu, cấu hình và key ring. Không tự gửi email, thông báo hay nối lại cuộc gọi cũ.
3. Kiểm tra sổ đầy đủ tới mốc; đối chiếu các prepared chưa kết luận. Thiếu sổ, checkpoint, khóa hay còn phạm vi mơ hồ thì giữ đóng phần đó và ghi rõ.
4. Áp các bản ghi committed (chạy lặp an toàn): tin đã xóa vẫn null; phòng đã xóa vẫn không xem, không vào được; lời mời đã thu hồi vẫn vô hiệu; epoch, stamp, mốc không giảm. Tin mất bản sửa mới nhất thì bỏ nội dung thấp hơn mốc và hiện placeholder. Chạy dọn dữ liệu theo thời điểm gốc (bỏ IP audit quá 7 ngày, nội dung tới hạn) trước khi mở. Không phát tin hay outbox thấp hơn mốc, không thông báo đổ chuông cũ.
5. Thu hồi mọi phiên, token, link được khôi phục; bỏ envelope và lệnh worker cũ; người dùng đăng nhập lại. Kết thúc mọi cuộc gọi, lượt tham gia, nguồn, quyền media cũ; chặn node và phòng LiveKit cũ, chứng minh không còn luồng media trước khi tính lại chỗ. Đổi thế hệ cache, registry, cursor.
6. Kiểm tra quyền và tombstone qua API; thử token, cursor, epoch cũ phải bị từ chối; đối chiếu chống trùng, version và dữ liệu mất theo RPO. Chỉ mở lại khi đạt; ghi mốc, số bản ghi áp lại, phạm vi bị giữ đóng, kết quả, người xác nhận.

RPO cho phép mất một phần thay đổi sau mốc khôi phục, nhưng không cho phép phục vụ nội dung hay quyền mà sổ đã ghi là xóa hay thu hồi. Không dùng bản cũ trong backup hay log để tự dựng lại nội dung mới.

<a id="placeholder-và-version-sau-restore"></a>

### Placeholder và version sau khôi phục

- Tin có thêm trường `contentState`: `available`, `deleted`, `unavailable_after_restore`. Thiếu trường thì suy theo `deletedAt` như cũ.
- **`unavailable_after_restore`** bắt buộc có trường; `content=null`, `deletedAt=null`; giao diện hiện "Nội dung chưa khôi phục được". Đây không phải tác giả đã xóa; không bịa `deletedAt`. Client cũ phải được nâng cấp trước khi phát hành.
- Migration đề xuất thêm `messaging.messages.restore_redacted_at` và mốc bảo vệ.
- Nếu version trong backup thấp hơn mốc sửa F: đặt version = F, xóa nội dung, đánh dấu unavailable, giữ ID, sequence, tác giả, thời điểm tạo. Áp lặp không tăng version. Nếu version ≥ F và nội dung đúng bản hiện tại thì giữ. Xóa đã commit thắng sửa.
- Tác giả còn quyền có thể sửa tin unavailable bằng nội dung mới (version lớn hơn F) hoặc xóa như bình thường. Người khác không được thay; worker không dựng nội dung từ bản cũ. Cache realtime chỉ gộp version ≥ mốc.

<a id="implementation"></a>

<a id="7-đối-chiếu-source-và-gói-triển-khai"></a>

## 7. Đối chiếu với code

| Hiện có | Còn thiếu |
|---|---|
| `identity.users` có suspended, disabled, deleted; username duy nhất ở mọi trạng thái | Không có API tự xóa hay khóa; không có job xóa tài khoản theo tuổi |
| `IUserDirectory` chỉ trả người đang hoạt động; nền đã có `IHistoricalUserSummaryReader` cho DM | DM đã dùng thông tin lịch sử và guard tách đọc, gửi. Tin phòng và v1 còn phải làm |
| Audit có IP, user-agent, metadata, ghi chú chỉ thêm | Cần danh sách trường được phép, ẩn và xóa theo chính sách, chặn sửa tay |
| Bảng tài khoản, phiên, refresh có hạn, thu hồi, khóa ngoại | Chưa có worker dọn, dọn chuỗi token, index, dấu kết thúc; chưa có bằng chứng phát hiện dùng lại và thời gian chờ sau khi dọn |
| `message_edits.previous_content` trong SQL và seed | Migration bỏ khỏi dữ liệu phục vụ (DEC-052) |
| Outbox, inbox trong schema; DM có worker outbox | Chưa có worker dọn và bằng chứng chống chạy lại sau khi dọn |
| Media chưa có module | Kết thúc, đang thoát và chặn theo [Media](../features/media/participation-lifecycle.md); không dọn trước khi dừng hẳn |

Code: [schema.sql](../../database/postgres/schema.sql), [IdentityEnums](../../services/Modules/Identity/Domain/IdentityEnums.cs), [IdentityData](../../services/Modules/Identity/Infrastructure/IdentityData.cs), [UserDirectory](../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs), [HistoricalUserSummaryReader](../../services/Modules/Identity/Infrastructure/Services/HistoricalUserSummaryReader.cs), [hồ sơ DM-MVP](../records/verification/messaging/DM-MVP.md).

| Gói | Cần có |
|---|---|
| DATA-GAP-01 | Review chính sách (DEC-103 đến 109), bảng dữ liệu, mapping với code; tần suất và ngân sách dọn; quyền người chạy job |
| DATA-GAP-02 | Mở rộng thông tin lịch sử và guard cho tin phòng và v1; chứng minh công cụ khóa, mở khóa không làm sống lại phiên; tác giả sửa, xóa khi người kia bị khóa |
| DATA-GAP-03 | Migration, worker, index, phần nội dung kết thúc, dấu chống trùng; rollback và chết giữa lô; khóa ngoại của chuỗi token; phát hiện dùng lại; thời gian chờ |
| DATA-GAP-04 (sau v1) | Chọn kho sổ, khóa, checkpoint; chứng minh giao thức hai kho với prepared, committed, aborted, không rõ |
| DATA-GAP-05 (sau v1) | Diễn tập khôi phục: mốc, thế hệ, email, media, hạn mức, mất bản sửa; đo RPO, RTO |
| DATA-GAP-06 | v1: thời hạn log và audit đúng chính sách. Sau v1: cấu hình backup, WAL, xác nhận khả năng khôi phục |

<a id="acceptance"></a>

<a id="8-tiêu-chí-và-kiểm-thử"></a>

## 8. Tiêu chí và ca kiểm thử

**Trạng thái:** chưa có hồ sơ cho toàn bộ ca DATA. Một số nhánh (người kia bị khóa, thông tin lịch sử, tombstone và gửi lại DM) có bằng chứng ở [DM-MVP](../records/verification/messaging/DM-MVP.md), nhưng chưa đóng toàn bộ.

**Phạm vi:**

- v1: các ca online (quyền, tombstone, chống trùng, dọn dữ liệu, bảo vệ log).
- Sau v1: AC-DATA-10 đến 13, 15, 16 và TC-DATA-07 đến 12. AC-DATA-14 tách: thời hạn log và audit (v1), đo backup và RPO, RTO (sau v1). TC-DATA-03 trong v1 chỉ chạy phần xóa, gửi lại, dọn.
- Dùng tài khoản và tin giả. [data-lifecycle.json](../fixtures/data-lifecycle.json) có 22 trường hợp thời hạn, 12 truy cập, 15 khôi phục, 10 schema tin, 6 kịch bản lỗi; hash và chữ ký trong bản ghi mẫu là giả.

| Mã | Cần chứng minh |
|---|---|
| AC-DATA-01 | Không có API hay giao diện tự xóa tài khoản, không có job xóa tài khoản hay server theo thời hạn chưa chọn |
| AC-DATA-02 | Khóa tài khoản chặn người đó, chặn gửi và gọi mới tới họ; người đang hoạt động vẫn đọc lịch sử đúng phạm vi và sửa, xóa tin của mình |
| AC-DATA-03 | Mở khóa phải đăng nhập lại; phiên, token, stamp cũ không sống lại |
| AC-DATA-04 | Phòng đã xóa giữ nội dung nhưng mọi API, realtime, media bị chặn; không có khôi phục phòng |
| AC-DATA-05 | Xóa tin: `content=null`, tombstone và bản ghi thao tác còn; gửi lại cùng `clientMessageId` không tạo tin |
| AC-DATA-06 | Dọn log, token, outbox không xóa tin đang có, phòng đã xóa hay dấu chống trùng |
| AC-DATA-07 | Token, thời gian chờ, phát hiện dùng lại vẫn đúng trước và sau khi dọn; chuỗi refresh đang dùng không mất token đã dùng |
| AC-DATA-08 | Worker đọc lại trạng thái, đúng biên UTC; rollback, chết, chạy lại không dọn bản ghi đang dùng hay chưa xác nhận |
| AC-DATA-09 | Không có nội dung, token hay query nhạy cảm trong log, audit, sổ, log chẩn đoán của nhà cung cấp |
| AC-DATA-10 | Sổ lỗi hay mất biên nhận: kết quả được đối chiếu; không tự coi prepared là committed hay bỏ qua dấu chưa rõ |
| AC-DATA-11 | Khôi phục áp lại xóa, thu hồi, epoch phòng, mốc ACL và chủ trước khi mở; áp lặp không hạ version hay quyền; chủ cũ không bỏ qua được |
| AC-DATA-12 | Mất sổ, checkpoint, hoặc prepared không rõ: phạm vi giữ đóng; không tự xóa từ ý định chưa commit |
| AC-DATA-13 | Khôi phục không gửi email hay đổ chuông cũ; phiên xác thực và media cũ không vào lại; chỉ trả chỗ khi LiveKit cũ đã dừng |
| AC-DATA-14 | RPO, RTO, thời hạn backup, log, audit được đo và cấu hình thật, không suy từ việc có job |
| AC-DATA-15 | Mất bản sửa mới nhất: `content=null`, `contentState=unavailable_after_restore`, `deletedAt=null`, giữ ID và sequence, version ≥ mốc; không gửi lại nội dung cũ |
| AC-DATA-16 | Tác giả sửa hay xóa tin unavailable bằng thao tác mới; người khác không thay được; áp lặp không hạ nội dung hay version mới |

| Mã ca | Tình huống | Cho |
|---|---|---|
| TC-DATA-01 | Khóa người kia; đọc lịch sử, gửi, gọi, sửa và xóa tin của mình; gửi lại tin đã commit và khóa mới; mở khóa rồi thử token cũ | AC-DATA-02, 03, 05 |
| TC-DATA-02 | Xóa phòng, đẩy đồng hồ nhiều tháng, chạy dọn, thử mọi đường đọc và vào | AC-DATA-04, 06 |
| TC-DATA-03 | Xóa tin rồi gửi lại cùng khóa; dọn outbox và biên nhận; khôi phục và gửi lại | AC-DATA-05, 11 |
| TC-DATA-04 | Chuỗi refresh đang dùng có token đã dùng quá 7 ngày; chuỗi đã kết thúc trước, đúng, sau mốc | AC-DATA-07, 08 |
| TC-DATA-05 | Delivery và outbox đang xử lý, media đang thoát, ứng viên đổi sang đang dùng khi job đang giành | AC-DATA-06, 08 |
| TC-DATA-06 | Token trong URL, query, header, body, lỗi nhà cung cấp; nội dung chat trong lý do; quét log chẩn đoán | AC-DATA-09 |
| TC-DATA-07 | Lỗi ở từng bước prepared, commit DB, biên nhận, phản hồi, aborted; ý định và dấu trùng | AC-DATA-10, 12 |
| TC-DATA-08 | Khôi phục về trước khi xóa, thu hồi, rời, xóa phòng, sửa; áp mốc hai lần | AC-DATA-11, 12 |
| TC-DATA-09 | Khôi phục khi còn phiên, link, đổ chuông, LiveKit đang chạy; chặn RoomService rồi mở lại | AC-DATA-13 |
| TC-DATA-10 | Diễn tập base, WAL, phụ thuộc catalogue, thời hạn; ghi RPO, RTO, mốc | AC-DATA-14 |
| TC-DATA-11 | Mất bản sửa F; khôi phục version thấp hơn, bằng, cao hơn F; áp hai lần; thử giao diện | AC-DATA-15 |
| TC-DATA-12 | Tác giả PATCH, DELETE tin unavailable; người ngoài thử; worker áp mốc cũ sau khi đã sửa mới | AC-DATA-16 |

<a id="gaps"></a>

<a id="9-đầu-vào-còn-mở"></a>

## 9. Còn mở

- **v1:** thông tin lịch sử, quyền của worker, lưu bền key ring, thời hạn lưu online (các mục DATA-GAP).
- **Sau v1:** công cụ và kho backup, khả năng khôi phục theo thời điểm, sổ bảo vệ, khôi phục khóa, placeholder và bằng chứng.
- Người trực, thẩm quyền thao tác, người duyệt (DEC-111, OQ-011).

Checklist và mẫu hồ sơ xem [nghiệm thu](../releases/acceptance.md) và [runbook](../guides/operations.md).

---

Nguồn: DEC-020, 034, 052, 064, 070, 077, 086, 091, 094, 097, 103 đến 109, 112, 116, [DEC-156](../records/decisions/README.md#dec-156).

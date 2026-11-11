# SCDC — Lời mời

Có hai cách mời người vào server: **link mời** (ai có link đều dùng được, có hạn và giới hạn lượt) và **mời đích danh** (gửi cho một người cụ thể, người đó phải bấm nhận). Phần Invitations của Community sở hữu lời mời, hạn, lượt dùng, token và các thao tác nhận, từ chối, thu hồi.

<a id="implementation"></a>

**Trạng thái:** chỉ có thiết kế, chưa có code được nghiệm thu cho link mời (UC-COM-09, 10, 11) và mời đích danh (UC-COM-12, 13). Xem [kế hoạch](../../project/planning.md#community-work-items). Bảng API và cách lưu token bên dưới là thiết kế mục tiêu; schema và fixture không thay được bằng chứng nhận, thu hồi hay tranh lượt cuối.

Điều kiện chung: [cơ chế Community](mechanics.md#use-case-conditions). Cách chạy: [hướng dẫn Community](../../guides/development.md#community). Tổng quan: [Community](README.md).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-004"></a> COM-004 | Link mời còn hiệu lực cho vào ngay, kể cả khi server bật chờ duyệt. |
| <a id="com-005"></a> COM-005 | Người tạo link chọn thời hạn. |
| <a id="com-010"></a> COM-010 | Chủ server hoặc người có quyền mời (`manage_invites`) tạo được link. |
| <a id="com-019"></a> COM-019 | Server riêng tư vào được bằng link mời hoặc được mời trực tiếp. |
| <a id="com-020"></a> COM-020 | Người có quyền mời thu hồi được link trước hạn; link đã thu hồi không dùng được. |
| <a id="com-031"></a> COM-031 | Chủ server hoặc người có quyền mời gửi lời mời đích danh vào server riêng tư; người nhận bấm nhận mới thành thành viên. |
| <a id="com-032"></a> COM-032 | Link mời có hạn 1 giờ, 1 ngày, 7 ngày (mặc định) hoặc không hết hạn; số lượt là số nguyên dương hoặc không giới hạn. |
| <a id="com-036"></a> COM-036 | Mời đích danh có hạn 7 ngày. Người nhận từ chối được; người có quyền mời hủy được khi còn chờ. Đã kết thúc thì không nhận được nữa. |
| <a id="com-041"></a> COM-041 | Lời mời đã tạo không mất hiệu lực khi người tạo rời server hay mất quyền. Nhưng người thực hiện tạo, hủy, thu hồi luôn được kiểm tra quyền hiện tại. |

<a id="use-cases"></a>

<a id="use-case"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung: [quy ước tích hợp](mechanics.md#use-case-conditions). Quy tắc, AC, TC của từng ca: [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-09"></a>

<a id="uc-com-09--tạo-xem-và-sao-chép-link-mời"></a>

### UC-COM-09 — Tạo, xem và sao chép link mời

**Ai làm:** chủ server hoặc thành viên có `manage_invites`, phiên và tư cách thành viên hợp lệ.

**Khi nào:** mở màn hình Lời mời (COM-S05) để tạo link hoặc sao chép lại link còn dùng được.

**Các bước:**

1. Chọn hạn và số lượt ([COM-032](#com-032)). Hệ thống kiểm tra quyền và dữ liệu, ghi link cùng bản ghi chống trùng.
2. Sau commit, trả thông tin và URL mời. Danh sách quản lý chỉ có thông tin, không có token.
3. Khi sao chép lại: kiểm tra quyền hiện tại và link còn hiệu lực, đọc token đã bảo vệ, trả đúng URL.

**Ngoại lệ:**

- Tạo lặp cùng khóa và nội dung không tạo link khác.
- Link hết hạn, đã thu hồi hay hết lượt thì không trả lại URL.
- Thiếu khóa giải mã thì báo lỗi phụ thuộc, không tự đổi token.
- Người tạo đã rời hay mất quyền: link vẫn hiệu lực, nhưng người đó không còn lấy được link hay thu hồi được.

**Kết quả:** link có hạn và lượt đã chọn, để chia sẻ cho [UC-COM-11](#uc-com-11). Tạo hay sao chép link chưa tạo thành viên, và link không cấp vai trò tùy chỉnh.

<a id="uc-com-10"></a>

<a id="uc-com-10--thu-hồi-link-mời"></a>

### UC-COM-10 — Thu hồi link mời

**Ai làm:** chủ server hoặc thành viên hiện có `manage_invites` (không cần là người tạo).

**Khi nào:** chọn Thu hồi ở COM-S05.

**Các bước:**

1. Xác nhận link cần thu hồi, gửi `expectedVersion`.
2. Kiểm tra quyền và version; đánh dấu đã thu hồi và xử lý token trong cùng transaction.
3. Sau commit, danh sách hiện link đã thu hồi; xem trước hay tham gia bằng link đó bị từ chối.

**Ngoại lệ:**

- Version cũ hoặc người thực hiện đã mất quyền: từ chối.
- Thu hồi tranh với tham gia: theo thứ tự commit. Tham gia commit trước thì giữ; thu hồi trước thì chặn.
- Mất phản hồi: đọc lại thông tin, không tự gửi lại.

**Kết quả:** link không còn tạo thành viên mới. Người đã vào bằng link không bị đưa ra khỏi server.

<a id="uc-com-11"></a>

<a id="uc-com-11--xem-trước-và-tham-gia-bằng-link-mời"></a>

### UC-COM-11 — Xem trước và tham gia bằng link

**Ai làm:** người dùng đủ điều kiện, đang giữ link mời.

**Khi nào:** mở link và chọn Tham gia ở COM-S02.

**Các bước:**

1. Giao diện lấy token từ link. Xem trước kiểm tra link còn hiệu lực và trả thông tin server, hạn và lượt còn lại; không trả phòng hay tin.
2. Người dùng xác nhận. Dưới khóa, hệ thống kiểm tra lại hạn, thu hồi, lượt; tạo hoặc kích hoạt tư cách thành viên và tăng lượt trong cùng transaction.
3. Kết thúc yêu cầu tham gia hay lời mời đích danh đang chờ (lý do `joined_elsewhere`). Sau commit, mở server và các phòng được xem; bỏ qua chế độ chờ duyệt.

**Ngoại lệ:**

- Xem trước không tiêu lượt, không giữ chỗ.
- Token sai, hết hạn, đã thu hồi: không tạo thành viên, không lộ nội dung server riêng tư.
- Hai người tranh lượt cuối: chỉ một người vào; rollback không tiêu lượt.
- Người đã là thành viên dùng link: không tiêu thêm lượt.
- Người tạo đã rời hay mất quyền: link vẫn dùng được.

**Kết quả:** có một tư cách thành viên với vai trò mặc định; tham gia lại thì dùng epoch mới. Link hết hiệu lực không dùng để khôi phục tư cách đã rời.

<a id="uc-com-12"></a>

<a id="uc-com-12--gửi-xem-và-hủy-lời-mời-đích-danh"></a>

### UC-COM-12 — Gửi, xem, hủy lời mời đích danh

**Ai làm:** chủ server hoặc thành viên có `manage_invites`.

**Điều kiện trước:** server riêng tư; người được mời chưa là thành viên.

**Khi nào:** mở quản lý lời mời (COM-S11), gửi mời hoặc hủy mời đang chờ.

**Các bước:**

1. Chọn người nhận theo ID. Hệ thống kiểm tra và tạo lời mời đang chờ, hạn 7 ngày, ghi bản ghi chống trùng.
2. Người có quyền xem danh sách lời mời. Người nhận được thông báo để mở [UC-COM-13](#uc-com-13). Chưa nhận thì chưa là thành viên.
3. Hủy: người có quyền hiện tại chọn hủy; hệ thống kiểm tra version và trạng thái, chuyển sang đã hủy, báo người nhận sau commit.

**Ngoại lệ:**

- Người nhận đã là thành viên: lỗi xung đột.
- Tạo lặp khi lời mời còn chờ và còn hạn: trả lại lời mời đó, không kéo dài hạn. Lời mời đã quá hạn được kết thúc trước khi tạo mới.
- Hủy tranh với nhận hay từ chối: chỉ một thao tác thắng.
- Người tạo đã mất quyền thì không hủy được, nhưng lời mời vẫn hiệu lực.

**Kết quả:** lời mời ở trạng thái đang chờ hoặc đã hủy. Tạo hay hủy không cấp quyền đọc phòng. Lời mời đã kết thúc không mở lại được bằng khóa tạo cũ.

<a id="uc-com-13"></a>

<a id="uc-com-13--xem-chấp-nhận-hoặc-từ-chối-lời-mời-đích-danh"></a>

### UC-COM-13 — Nhận hoặc từ chối lời mời đích danh

**Ai làm:** đúng người được mời, đủ điều kiện dùng ứng dụng.

**Khi nào:** mở hộp lời mời (COM-S11), chọn Nhận hoặc Từ chối.

**Các bước:**

1. Hệ thống chỉ trả lời mời của chính người dùng, kèm thông tin server được phép biết; chưa nhận thì không tải phòng hay tin.
2. **Nhận:** kiểm tra người nhận, hạn, trạng thái; chuyển sang đã nhận và tạo tư cách thành viên với vai trò mặc định trong cùng transaction. Không cần duyệt thêm.
3. **Từ chối:** kiểm tra `expectedVersion`, chuyển sang đã từ chối.
4. Giao diện hiện trạng thái cuối; nếu nhận thì mở các phòng được xem ([UC-COM-17](channels.md#uc-com-17)).

**Ngoại lệ:**

- Người khác nhận thay: bị từ chối.
- Hết hạn, đã hủy, đã từ chối: không nhận được. Nhận, từ chối, hủy cùng lúc: chỉ một thắng.
- Nhận lặp chỉ trả lại đúng tư cách đã tạo; không đưa người đã rời vào lại.
- Đã vào bằng cách khác: lời mời đang chờ kết thúc (`joined_elsewhere`).
- Người tạo mất quyền không ảnh hưởng lời mời còn hiệu lực.

**Kết quả:** nhận thì có một tư cách thành viên và lời mời ở trạng thái đã nhận. Từ chối hay hết hạn thì không tạo thành viên. Người nhận vẫn xem được trạng thái lời mời của mình.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện

| Màn hình | Ai dùng | Nội dung |
|---|---|---|
| COM-S05 Lời mời | Chủ server, người có quyền mời | Tạo link; chọn hạn (1 giờ, 1 ngày, 7 ngày, không hết hạn) và số lượt; sao chép; xem; thu hồi |
| COM-S11 Lời mời đích danh | Người nhận; người mời | Người nhận xem đúng server, nhận hoặc từ chối. Người mời hủy khi còn chờ. Chưa nhận thì không tải phòng hay tin |
| COM-S02 Xem trước | Người có link | Xem trước và tham gia ([servers](servers.md#ux)); chưa vào thì không tải phòng hay tin |

<a id="trạng-thái-lời-mời-đích-danh"></a>

### Trạng thái lời mời đích danh

| Trạng thái | Sự kiện | Kết quả |
|---|---|---|
| Đang chờ | Đúng người nhận bấm nhận, còn hiệu lực | Đã nhận; tạo tư cách thành viên cùng transaction |
| Đang chờ | Người nhận từ chối; người có quyền hủy; đủ 7 ngày | Đã từ chối; đã hủy; hết hạn. Không tạo thành viên; không nhận được nữa |

Màn hình hẹp và trạng thái chung: [giao diện chung](mechanics.md#ux).

<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="invite-contracts"></a>

<a id="invite-thiết-kế-dữ-liệuapi"></a>

### API (mục tiêu)

Quy ước chung: [Community](mechanics.md#contracts). Hợp đồng đầy đủ: [OpenAPI Community](../../contracts/community.openapi.json). Transaction: [transaction chung](mechanics.md#transactions).

| Method và đường dẫn | Đầu vào | Kết quả và quyền |
|---|---|---|
| `POST /servers/{id}/invites` | `{expiresInSeconds, maxUses?}` | 201 thông tin và link. `expiresInSeconds` là 3600, 86400, 604800 hoặc null; bỏ trống thì 604800. Cần quyền mời |
| `DELETE /servers/{id}/invites/{inviteId}` | ID link, `expectedVersion` | 204; cần quyền; tham gia sau đó bị từ chối |
| `POST /invites/preview` | `{token}` | 200 thông tin server khi link hợp lệ; không tạo thành viên, không tiêu lượt, không trả phòng hay tin |
| `POST /invites/join` | `{token}` | 200 tư cách thành viên. Token nằm trong body; kiểm tra hạn, thu hồi, lượt dưới khóa; không ghi body hay token vào log |
| `POST /servers/{id}/member-invitations` | `{recipientUserId}` | 201 lời mời đang chờ; cần quyền mời; chưa tạo thành viên |
| `POST /member-invitations/{invitationId}/accept` | Không có body | 200 tư cách thành viên; đúng người nhận, còn hiệu lực |
| `POST /member-invitations/{invitationId}/reject` | `{expectedVersion}` | 200 đã từ chối; đúng người nhận, còn chờ và còn hạn |
| `DELETE /servers/{id}/member-invitations/{invitationId}` | `expectedVersion` | 200 đã hủy; cần quyền mời; chỉ khi còn chờ |

<a id="invite-giao-dịch-của-thành-phần"></a>

#### Mời đích danh

- Chỉ cho server riêng tư (DEC-074). Mỗi server và người nhận chỉ có một lời mời đang chờ; hạn 7 ngày; người nhận phải đang hoạt động và đã xác minh.
- Người nhận đã là thành viên: `409 ALREADY_MEMBER`. Lời mời còn chờ và còn hạn: gửi lặp trả lại lời mời đó, không kéo dài hạn.
- Nhận và tạo thành viên trong cùng transaction. Nhận lặp chỉ trả tư cách nếu vẫn đúng `membershipId` đã tạo; không đưa người đã rời vào lại.
- Vào bằng cách khác thì lời mời đang chờ bị hủy (`joined_elsewhere`). Đã kết thúc hay hết hạn thì không mở lại.

<a id="invite-invite-secret"></a>

<a id="invite-link-mời-hiệu-lực-và-secret"></a>

#### Link mời: token và hiệu lực

**Token:**

- 32 byte ngẫu nhiên, mã hóa base64url; DB tra bằng SHA-256.
- Link dạng `/invite#token=…`; trang lấy token vào bộ nhớ rồi xóa khỏi URL. API nhận token trong body (không đặt trong đường dẫn như bản nháp cũ `/invites/{token}/join`).
- Không ghi body, token hay URL mời vào log, audit hay outbox.

**Hiệu lực (DEC-097):**

- Link và lời mời không phụ thuộc người tạo còn là thành viên hay còn quyền. Thu hồi và hủy vẫn cần người thực hiện có quyền hiện tại.
- Gửi lại lời mời đích danh không làm mới hạn 7 ngày. Lời mời quá hạn được chuyển sang hết hạn dưới khóa trước khi tạo mới. Partial index chỉ lọc trạng thái "đang chờ", không dùng `now()`.

**Xem trước và tham gia:**

- Xem trước chỉ trả thông tin server khi link hợp lệ; không có phòng hay tin, không tiêu lượt, không giữ chỗ. Xem trước thành công không đảm bảo còn lượt khi bấm tham gia.
- Tham gia khóa server và link; kiểm tra `expiresAt > now`, chưa thu hồi, còn lượt; rồi mới tạo thành viên. Chỉ tăng lượt khi tạo mới hoặc kích hoạt lại. Rollback không tiêu lượt. Người đã là thành viên gọi lại không tiêu lượt. Hai người tranh lượt cuối chỉ một người vào.

**Sao chép lại link (đề xuất):**

- Ngoài hash, giữ token trong envelope Data Protection `Community.InviteSecret.v1` để người có quyền lấy lại link khi lỡ mất phản hồi tạo.
- Danh sách không chứa token. `GET /servers/{id}/invites/{inviteId}/link` chỉ trả URL cho người có `manage_invites` khi link còn dùng được, kèm `Cache-Control: no-store`.
- Xóa envelope khi thu hồi, hết hạn, hết lượt. Link không hết hạn cần giữ key ring qua khởi động lại và triển khai. Thiếu khóa: 503, không tự đổi token. Backup khóa để [sau v1](../../project/overview.md#backup-scope).

Thay đổi schema: [COM-SQL-01 đến 10](mechanics.md#schema-migration). Chưa được coi là đã làm khi chưa có migration và bằng chứng.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-04"></a> AC-COM-04 | Mở link mời còn hiệu lực của server đang bật chờ duyệt | Vào ngay |
| <a id="ac-com-05"></a> AC-COM-05 | Mở link mời đã hết hạn | Không vào được |
| <a id="ac-com-09"></a> AC-COM-09 | Chủ server, người có quyền, thành viên thường lần lượt thử tạo link | Hai người đầu tạo được; thành viên thường bị từ chối |
| <a id="ac-com-19"></a> AC-COM-19 | Tìm server riêng tư; rồi dùng link hợp lệ hoặc được mời trực tiếp | Không thấy trong tìm kiếm; hai cách kia vào được theo điều kiện |
| <a id="ac-com-20"></a> AC-COM-20 | Người có quyền thu hồi link còn hạn; người khác mở link | Không vào được |
| <a id="ac-com-31"></a> AC-COM-31 | Mời A vào server riêng tư; B nhận thay; A chưa nhận rồi nhận | B bị từ chối; A chưa nhận thì chưa có quyền phòng; sau khi nhận có một tư cách thành viên, không cần duyệt |
| <a id="ac-com-32"></a> AC-COM-32 | Link ở biên hạn và lượt; tham gia đồng thời; thành viên gọi lặp | Không vượt số lượt; không tăng lượt cho thành viên đã có; hết hạn hay đã thu hồi thì không vào được |
| <a id="ac-com-41"></a> AC-COM-41 | Người tạo link hay lời mời rời server hoặc mất quyền | Lời mời vẫn hiệu lực tới khi hết hạn, hết lượt hay bị thu hồi; người tạo không tự hủy được nữa |

Tiêu chí liên quan nhiều phần được định nghĩa ở trang chính; xem [bảng truy vết](traceability.md#use-case-coverage).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dữ liệu và cách ghi bằng chứng: [dùng chung](../messaging/channel-messaging.md#evidence). Mỗi ca cần test cho từng endpoint và kiểm tra quyền qua API.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-06"></a> TC-COM-06 | Link hợp lệ, hết hạn, đã thu hồi; dùng link ở server chờ duyệt | Link hợp lệ tạo thành viên ngay; các link khác bị từ chối; yêu cầu đang chờ kết thúc cùng transaction | [AC-COM-04](#ac-com-04), [05](#ac-com-05), [20](#ac-com-20) |
| <a id="tc-com-07"></a> TC-COM-07 | N và M tạo, thu hồi link, đổi cách tham gia | Chỉ người có quyền làm được; link hợp lệ vẫn bỏ qua chờ duyệt | [AC-COM-08](servers.md#ac-com-08), [09](#ac-com-09), [20](#ac-com-20) |
| <a id="tc-com-08"></a> TC-COM-08 | M mời đích danh P; N nhận thay; P nhận hai lần hoặc đồng thời | N bị từ chối; trước khi nhận chưa là thành viên; nhận tạo đúng một tư cách | [AC-COM-19](#ac-com-19), [31](#ac-com-31) |
| <a id="tc-com-13"></a> TC-COM-13 | Dùng link đúng lúc hết hạn, hết lượt; nhiều request tranh lượt cuối; thành viên tham gia lặp | Không vượt lượt, không nhân tư cách; rollback không tiêu lượt | [AC-COM-32](#ac-com-32) |
| <a id="tc-com-17"></a> TC-COM-17 | Nhận, hủy, từ chối đồng thời; ở mốc 7 ngày; gọi sau khi đã kết thúc | Một thao tác thắng; một tư cách nếu nhận thắng; hết hạn không nhận được | [AC-COM-31](#ac-com-31), [AC-COM-35](memberships.md#ac-com-35) |
| <a id="tc-com-21"></a> TC-COM-21 | Người tạo rời hay mất quyền; người nhận vẫn nhận hoặc dùng link; người tạo thử hủy hay thu hồi | Lời mời vẫn dùng được nếu hợp lệ; kiểm tra quyền người thực hiện hiện tại | [AC-COM-41](#ac-com-41) |
| <a id="tc-com-24"></a> TC-COM-24 | Xem trước rồi tranh lượt cuối; sao chép link khi thiếu khóa; thu hồi hay hết hạn đúng mốc | Xem trước không giữ chỗ; một tư cách và một lượt; thiếu khóa không tạo token khác | [AC-COM-32](#ac-com-32) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Phụ thuộc chung: [kế hoạch](../../project/planning.md#community-work-items), [migration](mechanics.md#schema-migration), [vòng đời dữ liệu](../../system/data-lifecycle.md).

- **Lời mời:** hạn, lượt, hiệu lực theo người tạo đã chốt (DEC-075, 087, 097); đã có thiết kế schema, token, transaction. Còn: làm code, migration, kho khóa, kiểm thử (OQ-003, 004, 008).
- **Server riêng tư:** đã chốt (DEC-074, 087, 096); đã thiết kế schema, hộp lời mời, thông báo. Còn: rà soát, làm, kiểm thử. v1 không có xóa server (DEC-094).

---

Nguồn: DEC-025 (COM-004), DEC-028 (COM-005), DEC-031 (COM-010), DEC-043 (COM-019), DEC-044 (COM-020), DEC-074 (COM-031), DEC-075 (COM-032), DEC-087 (COM-036), DEC-097 (COM-041).

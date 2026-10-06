# SCDC — Invitations — Link mời và lời mời đích danh

Cập nhật: 2026-10-06. Thành phần nội bộ của module Community. Quy tắc nghiệp vụ đã xác nhận theo các DEC dẫn chiếu; use case và thiết kế kỹ thuật là bản dự thảo để rà soát. Source còn Foundation, chưa có kết quả chạy UC-COM.

Sở hữu hiệu lực, trạng thái và secret của link mời/lời mời đích danh. Khi join/accept, phối hợp Memberships để tạo tư cách thành viên trong cùng transaction với lượt dùng/transition.

[Tổng quan và truy vết Community](../community.md#use-cases) · [Kế hoạch triển khai](../community.md#use-case-delivery).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Use case](#use-cases)
- [UX và trạng thái](#ux)
- [Thiết kế dữ liệu/API](#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-004"></a> COM-004 | Liên kết mời hợp lệ cho vào ngay, kể cả khi cộng đồng bật chế độ chờ duyệt. | DEC-025 |
| <a id="com-005"></a> COM-005 | Người tạo liên kết mời chọn thời hạn hiệu lực. | DEC-028 |
| <a id="com-010"></a> COM-010 | Chủ sở hữu hoặc người được cấp quyền có thể tạo liên kết mời. | DEC-031 |
| <a id="com-019"></a> COM-019 | Cộng đồng riêng tư có thể tham gia bằng liên kết mời hoặc được thêm trực tiếp. | DEC-043 |
| <a id="com-020"></a> COM-020 | Người có quyền tạo liên kết mời có thể thu hồi liên kết trước hạn; liên kết đã thu hồi không còn dùng để tham gia. | DEC-044 |
| <a id="com-031"></a> COM-031 | Chủ sở hữu/người có quyền tạo mời gửi lời mời đích danh vào cộng đồng riêng tư; người nhận chấp nhận mới trở thành thành viên. | DEC-074 |
| <a id="com-032"></a> COM-032 | Link mời chọn hạn 1 giờ/1 ngày/7 ngày/không hết hạn, mặc định 7 ngày; maxUses nguyên dương hoặc không giới hạn; người có quyền tạo mời được thu hồi. | DEC-075 |
| <a id="com-036"></a> COM-036 | Mời đích danh hạn 7 ngày; đúng người nhận từ chối, người có quyền tạo mời hủy pending; sau trạng thái cuối không accept được. | DEC-087 |
| <a id="com-041"></a> COM-041 | Lời mời đã tạo không tự vô hiệu khi creator rời/mất quyền; actor tạo/duyệt/hủy/thu hồi vẫn kiểm tra quyền hiện hành. | DEC-097 |

<a id="use-cases"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](integration.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](../community.md#use-case-coverage).

<a id="uc-com-09"></a>

### UC-COM-09 — Tạo, xem và sao chép link mời

**Tác nhân:** Owner hoặc thành viên có manage_invites.

**Điều kiện trước:** Actor có phiên/membership hợp lệ và quyền mời hiện hành trong server.

**Kích hoạt:** Actor mở COM-S05 để tạo link hoặc sao chép lại link còn dùng được.

**Luồng chính:**

1. Actor chọn hạn/lượt theo [COM-032](#com-032); hệ thống kiểm tra quyền/dữ liệu và ghi invite cùng operation tạo.
2. Sau commit, trả metadata và URL mời; danh sách quản lý chỉ chứa metadata, không chứa token.
3. Khi cần sao chép lại, hệ thống kiểm tra quyền hiện hành và hiệu lực invite, đọc secret được bảo vệ rồi trả đúng URL theo thiết kế.

**Ngoại lệ:** Tạo lặp cùng khóa/payload không tạo link khác. Link hết hạn/thu hồi/hết lượt không được cấp lại URL sử dụng. Key thiếu trả lỗi phụ thuộc, không đổi token âm thầm. Creator rời/mất manage_invites không tự vô hiệu link, nhưng không còn quyền lấy link hoặc thu hồi chỉ vì từng tạo.

**Kết quả sau cùng:** Link đã tạo có hạn/lượt đã chọn, có thể chia sẻ để người nhận dùng [UC-COM-11](#uc-com-11). Việc tạo/copy link chưa tạo membership và không cấp custom role qua link.

<a id="uc-com-10"></a>

### UC-COM-10 — Thu hồi link mời

**Tác nhân:** Owner hoặc thành viên hiện có manage_invites, không cần là creator.

**Điều kiện trước:** Phiên/membership/quyền hợp lệ; actor biết invite thuộc server và version đã tải.

**Kích hoạt:** Actor chọn thu hồi tại COM-S05.

**Luồng chính:**

1. Actor xác nhận invite cần thu hồi và gửi expectedVersion.
2. Hệ thống kiểm tra quyền/version, đánh dấu revoked và xử lý secret theo thiết kế trong transaction.
3. Sau commit, danh sách hiển thị invite đã thu hồi; preview/join tiếp theo không sử dụng được link đó.

**Ngoại lệ:** Version cũ hoặc actor đã mất quyền từ chối. Revoke tranh join tuân thứ tự commit: join đã commit trước được giữ; revoke thắng trước chặn join. Mất response đọc lại metadata, không tự phát lại mutation.

**Kết quả sau cùng:** Link không cấp membership mới. Thành viên đã tham gia bằng link không bị rời server vì thao tác thu hồi link.

<a id="uc-com-11"></a>

### UC-COM-11 — Xem trước và tham gia bằng link mời

**Tác nhân:** Người dùng đủ điều kiện ứng dụng đang giữ link mời.

**Điều kiện trước:** Phiên hợp lệ; hiệu lực token/server được kiểm tra riêng khi preview và khi join.

**Kích hoạt:** Người dùng mở link và chọn tham gia tại COM-S02.

**Luồng chính:**

1. Giao diện lấy token theo thiết kế link; preview kiểm tra hiệu lực và trả summary server cùng hạn/lượt còn lại, không trả phòng/tin.
2. Người dùng xác nhận join; hệ thống kiểm tra lại hạn/thu hồi/lượt dưới khóa, tạo/kích hoạt membership và tăng lượt trong cùng transaction.
3. Kết thúc pending join request hoặc mời đích danh đang chờ theo joined_elsewhere. Sau commit, mở server và phòng được phép, bỏ qua join mode chờ duyệt.

**Ngoại lệ:** Preview không tiêu lượt hoặc giữ chỗ. Token sai/hết hạn/thu hồi không cấp membership hoặc lộ nội dung private. Hai người tranh lượt cuối chỉ một người vào; rollback không tiêu lượt. Người đang active join bằng link còn hợp lệ không tiêu thêm lượt. Creator đã rời/mất quyền không làm link mất hiệu lực.

**Kết quả sau cùng:** Có một membership active với role mặc định; rejoin dùng epoch mới. Link không còn hợp lệ không trở thành cách phục hồi membership đã left.

<a id="uc-com-12"></a>

### UC-COM-12 — Gửi, xem và hủy lời mời đích danh

**Tác nhân:** Owner hoặc thành viên có manage_invites.

**Điều kiện trước:** Phiên/membership/quyền hợp lệ; tạo mời đích danh vào server private, recipient chưa là thành viên active.

**Kích hoạt:** Actor mở quản lý lời mời tại COM-S11, gửi mời hoặc hủy mời pending.

**Luồng chính:**

1. Actor chọn recipient theo ID; hệ thống kiểm tra điều kiện và tạo mời pending hạn 7 ngày, ghi operation tạo.
2. Actor xem danh sách mời theo quyền; recipient nhận thông báo để mở [UC-COM-13](#uc-com-13). Chưa accept chưa tạo membership.
3. Khi actor có quyền hiện hành chọn hủy pending, hệ thống kiểm tra version/trạng thái rồi chuyển cancelled và thông báo đúng recipient sau commit.

**Ngoại lệ:** Recipient đã active nhận xung đột. Pending còn hạn được trả lại khi tạo lặp, không kéo dài hạn; pending quá hạn được kết thúc trước khi tạo mời mới theo thiết kế. Cancel tranh accept/reject chỉ một transition thắng. Lời mời không tự vô hiệu khi creator rời/mất quyền; creator đó không được hủy nếu mất manage_invites.

**Kết quả sau cùng:** Mời pending hoặc cancelled có trạng thái theo dõi được; tạo/hủy mời không cấp quyền đọc phòng. Trạng thái terminal của khóa tạo cũ không bị mở lại.

<a id="uc-com-13"></a>

### UC-COM-13 — Xem, chấp nhận hoặc từ chối lời mời đích danh

**Tác nhân:** Đúng recipient của lời mời.

**Điều kiện trước:** Recipient đủ điều kiện ứng dụng; đọc inbox của mình. Accept/reject kiểm tra pending còn hạn và server/actor hiện hành.

**Kích hoạt:** Recipient mở inbox/COM-S11 rồi chọn accept hoặc reject.

**Luồng chính:**

1. Hệ thống chỉ trả lời mời của recipient cùng summary server được phép biết; chưa nhận không tải phòng/tin.
2. Khi accept, kiểm tra recipient/expiry/trạng thái rồi chuyển accepted và tạo/kích hoạt membership với role mặc định cùng transaction, không cần reviewer duyệt thêm.
3. Khi reject, kiểm tra expectedVersion rồi chuyển rejected. Sau commit, giao diện hiển thị trạng thái cuối; accept mở phòng được xem qua [UC-COM-17](channels.md#uc-com-17).

**Ngoại lệ:** Người khác nhận thay bị từ chối. Expired/cancelled/rejected không accept được; accept/reject/cancel đồng thời chỉ một transition thắng. Accept lặp chỉ trả membership còn đúng epoch đã tạo, không rejoin người đã rời. Joined bằng đường khác kết thúc pending mời theo joined_elsewhere. Creator mất quyền không ảnh hưởng hiệu lực mời còn hợp lệ.

**Kết quả sau cùng:** Accept có một membership active và mời accepted; reject/expiry không tạo membership. Recipient vẫn xem được trạng thái lời mời của mình theo contract.

<a id="ux"></a>

## UX và trạng thái

COM-S11 Lời mời đích danh theo DEC-074: người nhận xem đúng cộng đồng, chấp nhận hoặc từ chối; người mời được hủy khi còn chờ theo DEC-087; chưa nhận không tải phòng/tin.

Preview/join link dùng [COM-S02](servers.md#ux); chưa accept không tải phòng/tin.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S05 Lời mời | Chủ sở hữu/người có quyền tạo lời mời | Tạo, chọn 1 giờ/1 ngày/7 ngày/không hết hạn, đặt lượt nguyên dương hoặc không giới hạn, sao chép, xem và thu hồi |

### Trạng thái lời mời đích danh

| Đối tượng/trạng thái | Thao tác | Kết quả và quyền |
|---|---|---|
| Mời đích danh pending | Đúng người nhận chấp nhận, lời mời còn hợp lệ | Accepted và tạo membership cùng transaction; không thêm bước duyệt |
| Mời đích danh pending | Người nhận từ chối / người có quyền tạo mời hủy / đủ 7 ngày | Rejected / Cancelled / Expired; không tạo membership, không chấp nhận sau trạng thái cuối |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](integration.md#ux).

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../contracts/community.openapi.json) là schema dự thảo, không phải API đang chạy. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers/{id}/invites` | `{expiresInSeconds,maxUses?}` | 201 metadata/link; expiresInSeconds là 3600/86400/604800/null, bỏ trường dùng mặc định 604800; quyền tạo mời |
| `DELETE /servers/{id}/invites/{inviteId}` | ID link và expectedVersion | 204 thu hồi; đúng quyền, join sau thu hồi bị từ chối |
| `POST /invites/preview` | `{token}` | 200 summary khi link hợp lệ; không tạo membership/tiêu lượt; không trả phòng/tin |
| `POST /invites/join` | `{token}` | 200 membership; token trong body, kiểm tra hạn/thu hồi/lượt dưới khóa; không ghi body/token vào log |
| `POST /servers/{id}/member-invitations` | `{recipientUserId}` | 201 mời pending; owner/quyền tạo mời; chưa tạo membership |
| `POST /member-invitations/{invitationId}/accept` | Không có body | 200 membership; đúng người nhận, mời còn hợp lệ; không thêm bước duyệt |
| `POST /member-invitations/{invitationId}/reject` | `{expectedVersion}` | 200 rejected; đúng người nhận; chỉ pending còn hạn |
| `DELETE /servers/{id}/member-invitations/{invitationId}` | `expectedVersion` | 200 cancelled; quyền tạo mời; chỉ pending |

### Giao dịch của thành phần

- Mời đích danh: chỉ vào private theo DEC-074; unique pending/server/recipient, hạn 7 ngày, accept đúng recipient active/verified. Đang active thì create mời nhận 409 ALREADY_MEMBER; pending còn hạn thì POST lặp trả cùng mời, không kéo dài hạn. Accept và membership cùng transaction; accepted lặp chỉ trả membership nếu còn đúng membershipId đã tạo, không rejoin sau người nhận đã rời. Join bằng đường khác hủy mời pending với reason joined_elsewhere; sau trạng thái cuối hoặc expiry, lời mời cũ không được mở lại.

<a id="invite-secret"></a>

### Link mời, hiệu lực và secret

Link token kỹ thuật đề xuất là 32 byte ngẫu nhiên base64url, DB tra bằng SHA-256. Route SPA `/invite#token=…` lấy token vào RAM rồi bỏ fragment khỏi URL; API preview/join nhận token trong body. Điều chỉnh route draft cũ `/invites/{token}/join` để secret không nằm trong request path; không ghi body/token/URL mời trong log, audit hoặc outbox.

Theo DEC-097, hiệu lực link/mời đích danh không phụ thuộc creator còn là member/còn manage_invites. Revoke/cancel vẫn cần actor hiện có quyền; creator đã mất quyền không tự được hủy chỉ vì từng tạo. Không tự refresh hạn 7 ngày khi gọi lại POST mời đích danh. Pending quá hạn được chuyển expired dưới khóa trước khi tạo mời mới; partial index chỉ dùng trạng thái pending, không đặt `now()` trong predicate.

Preview chỉ trả server summary khi token còn hợp lệ, không có phòng/tin, không tiêu lượt và không giữ chỗ. Join khóa server/invite và kiểm tra `expiresAt > now`, revokedAt null, số lượt trước tạo membership; chỉ tăng lượt cho membership mới/reactivate. Rollback không tiêu lượt. Thành viên đang active gọi lại không tiêu lượt; link sai/không còn hợp lệ không cấp quyền mới. Người tranh lượt cuối chỉ một người join thành công; không hứa preview thành công là join chắc chắn còn lượt.

Để người có quyền sao chép lại link khi response tạo bị mất, đề xuất giữ token trong envelope Data Protection `Community.InviteSecret.v1`, ngoài hash tra cứu. Metadata/list không có token; `GET /servers/{id}/invites/{inviteId}/link` trả URL chỉ cho manage_invites khi link còn dùng được, `Cache-Control:no-store`. Purge envelope khi thu hồi/hết hạn/hết lượt; link không hết hạn cần giữ key ring tương ứng và backup liên quan. Key thiếu trả 503, không đổi token/link âm thầm.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

<a id="acceptance"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-04"></a> AC-COM-04 | Người dùng mở liên kết mời còn hiệu lực của cộng đồng đang bật chờ duyệt. | Người dùng được vào ngay. |
| <a id="ac-com-05"></a> AC-COM-05 | Người dùng mở liên kết mời đã hết hạn. | Người dùng không thể dùng liên kết đó để tham gia. |
| <a id="ac-com-09"></a> AC-COM-09 | Chủ sở hữu, người được cấp quyền và thành viên thường lần lượt thử tạo liên kết mời. | Hai nhóm đầu tạo được liên kết; thành viên thường bị từ chối. |
| <a id="ac-com-19"></a> AC-COM-19 | Người dùng thử tìm cộng đồng riêng tư, rồi dùng liên kết mời hợp lệ hoặc được thêm trực tiếp. | Cộng đồng không xuất hiện trong tìm kiếm; hai cách còn lại cho phép trở thành thành viên theo điều kiện tương ứng. |
| <a id="ac-com-20"></a> AC-COM-20 | Người có quyền tạo lời mời thu hồi một liên kết còn hạn; người khác mở liên kết đó. | Hệ thống không cho tham gia bằng liên kết đã thu hồi. |
| <a id="ac-com-31"></a> AC-COM-31 | Mời đích danh A vào cộng đồng riêng tư; B cố chấp nhận thay; A chưa nhận rồi nhận | B bị từ chối; A chưa nhận không có quyền phòng; sau nhận có một membership và không cần duyệt thêm |
| <a id="ac-com-32"></a> AC-COM-32 | Link ở biên hạn/lượt; join đồng thời và gọi lặp của thành viên | Không vượt maxUses, không tăng lượt cho thành viên hiện hành; hết hạn/thu hồi không join được |
| <a id="ac-com-41"></a> AC-COM-41 | Creator của link/mời đích danh rời hoặc mất manage_invites | Mời đã phát hành còn hiệu lực tới hạn/lượt/thu hồi; creator không còn quyền tự hủy, đúng actor hiện hành mới xử lý theo DEC-097 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](../community.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Chưa có kết quả chạy AC-COM.

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](integration.md#evidence). Các ca bên dưới đều chưa chạy; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-06"></a> TC-COM-06 | Link hợp lệ/hết hạn/thu hồi; dùng link trong cộng đồng chờ duyệt | Link hợp lệ tạo membership ngay; các link khác bị từ chối; pending cũ kết thúc cùng giao dịch | [AC-COM-04](#ac-com-04), [AC-COM-05](#ac-com-05), [AC-COM-20](#ac-com-20) |
| <a id="tc-com-07"></a> TC-COM-07 | N/M tạo/thu hồi link và đổi join mode | Chỉ quyền tương ứng; link hợp lệ vẫn bỏ qua chờ duyệt | [AC-COM-08](servers.md#ac-com-08), [AC-COM-09](#ac-com-09), [AC-COM-20](#ac-com-20) |
| <a id="tc-com-08"></a> TC-COM-08 | M mời đích danh P; N nhận thay; P nhận hai lần/đồng thời | N bị từ chối; trước nhận không là thành viên; nhận tạo một membership | [AC-COM-19](#ac-com-19), [AC-COM-31](#ac-com-31) |
| <a id="tc-com-13"></a> TC-COM-13 | Dùng link đúng lúc hết hạn/hết lượt; nhiều request tranh lượt cuối; thành viên join lặp | Không vượt lượt, không nhân membership; rollback không tiêu lượt | [AC-COM-32](#ac-com-32) |
| <a id="tc-com-17"></a> TC-COM-17 | Mời đích danh accept/cancel/reject đồng thời, ở cutoff 7 ngày và gọi sau trạng thái cuối | Một transition thắng; một membership nếu accept thắng; hết hạn không accept được | [AC-COM-31](#ac-com-31), [AC-COM-35](memberships.md#ac-com-35) |
| <a id="tc-com-21"></a> TC-COM-21 | Creator leave/mất quyền; recipient accept hoặc join link; creator cố cancel/revoke | Mời vẫn dùng nếu hợp lệ; kiểm tra quyền actor hiện hành | [AC-COM-41](#ac-com-41) |
| <a id="tc-com-24"></a> TC-COM-24 | Preview link rồi tranh lượt cuối; copy link thiếu key, revoke/expiry đúng cutoff | Preview không giữ chỗ; một membership/lượt; thiếu key không tạo token khác | [AC-COM-32](#ac-com-32) |

<a id="gaps"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../community.md#use-case-delivery), [migration](integration.md#schema-migration) và [vòng đời dữ liệu](../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Lời mời | Hạn/lượt/issuer theo DEC-075/087/097; đã có schema/secret/transaction, còn proof và migration/key store. | OQ-003, OQ-004, OQ-008 |
| Cộng đồng riêng tư | Đã chốt DEC-074/087/096; schema/inbox/notification có thiết kế; còn review/proof. Không xóa server v1 theo DEC-094. | OQ-003, OQ-008 |

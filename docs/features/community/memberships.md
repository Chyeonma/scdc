# Thành viên và yêu cầu tham gia

Community / Memberships sở hữu tư cách, membership epoch và các transition join/request/leave. Quy tắc, luồng sử dụng, UX, thiết kế dữ liệu/API và tiêu chí kiểm chứng của chủ đề được quản lý tại trang này.

<a id="implementation"></a>

## Trạng thái theo khả năng

Đối chiếu hiện trạng ngày 2026-10-08. Nền Community đã hợp nhất vào `main`; bằng chứng dẫn dưới đây gắn revision cụ thể, không phải kết quả chạy lại cho mọi thay đổi sau đó.

| Khả năng | Trạng thái và giới hạn | Bằng chứng |
|---|---|---|
| Public/immediate join — phần UC-COM-06 | API/WebClient đã kiểm chứng; join lặp/đồng thời giữ cùng epoch | [Tham gia trực tiếp](../../records/verification/community/direct-join.md) |
| Rejoin từ record left | Đã kiểm chứng qua fixture: epoch mới, dọn role/override cũ; chưa có leave API | [Tham gia trực tiếp](../../records/verification/community/direct-join.md) |
| Approval/request/leave — UC-COM-07/08/15, nhánh approval UC-COM-06 | Thiết kế mục tiêu; chưa triển khai | [Kế hoạch](../../project/planning.md#community-work-items) |

Metadata cộng đồng và owner tại [Servers](servers.md); assignment/ngoại lệ phải dùng đúng epoch theo [Access control](access-control.md).

## Mục lục

- [Quy tắc](#requirements)
- [Use case](#use-cases)
- [UX và trạng thái](#ux)
- [Thiết kế dữ liệu và xử lý](#technical)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

[Điều kiện chung và cơ chế Community](../../system/community.md#use-case-conditions) · [Hướng dẫn chạy và kiểm thử](../../guides/community-development.md) · [Mục lục Community](README.md).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-001"></a> COM-001 | Người dùng có thể tham gia qua tìm kiếm hoặc liên kết mời. | DEC-022 |
| <a id="com-011"></a> COM-011 | Chủ sở hữu hoặc người được cấp quyền có thể duyệt yêu cầu tham gia. | DEC-032 |
| <a id="com-021"></a> COM-021 | Thành viên thường có thể tự rời cộng đồng. | DEC-045 |
| <a id="com-030"></a> COM-030 | Người có quyền duyệt được từ chối; người gửi xem trạng thái/hủy pending, sau từ chối/hủy được gửi mới. | DEC-073 |
| <a id="com-035"></a> COM-035 | Rejoin theo join mode hiện hành và vai trò mặc định, không phục hồi role cũ; có quyền xem lại lịch sử phòng hiện được phép xem. | DEC-087, DEC-038 |

<a id="use-cases"></a>

<a id="use-case"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-06"></a>

<a id="uc-com-06--tham-gia-cộng-đồng-công-khai-vào-ngay"></a>

### UC-COM-06 — Tham gia cộng đồng công khai vào ngay

**Module phụ trách:** Community / Memberships.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Người dùng đủ điều kiện ứng dụng chưa là thành viên active của server đích.

**Điều kiện trước:** Server public/active và join mode vào ngay tại thời điểm join.

**Kích hoạt:** Người dùng bấm Tham gia tại COM-S02.

**Luồng chính:**

1. Hệ thống kiểm tra lại visibility, join mode, actor và tư cách hiện hành dưới cùng transaction.
2. Tạo hoặc kích hoạt lại membership với membershipId mới; chỉ áp vai trò mặc định. Kết thúc pending cũ bằng reason joined_elsewhere theo thiết kế transition.
3. Sau commit, trả membership và mở các phòng được phép qua [UC-COM-17](channels.md#uc-com-17).

**Ngoại lệ:** Join lặp/đồng thời của người đang active trả tư cách hiện hành, không nhân membership. Nếu mode đã đổi sang chờ duyệt thì xử lý [UC-COM-07](#uc-com-07); nếu đã private thì đường join công khai bị chặn. Mất response được đối soát qua [UC-COM-03](servers.md#uc-com-03) trước thao tác tiếp.

**Kết quả sau cùng:** Có một membership active; lần rejoin không phục hồi role/ngoại lệ cá nhân cũ. Lịch sử phòng đọc theo view hiện hành.

<a id="uc-com-07"></a>

<a id="uc-com-07--gửi-xem-và-hủy-yêu-cầu-tham-gia"></a>

### UC-COM-07 — Gửi, xem và hủy yêu cầu tham gia

**Module phụ trách:** Community / Memberships.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Người dùng đủ điều kiện ứng dụng chưa là thành viên active; chỉ sender quản lý yêu cầu của mình.

**Điều kiện trước:** Gửi mới vào server public/active có join mode chờ duyệt. Xem/hủy cần quyền đối với request của chính mình, không đòi đã là thành viên.

**Kích hoạt:** Người dùng gửi yêu cầu, mở trạng thái hoặc chọn hủy pending tại COM-S02/06.

**Luồng chính:**

1. Khi gửi yêu cầu, hệ thống kiểm tra join mode/visibility và trả request pending duy nhất của cặp server/user; nếu chưa có thì tạo mới.
2. Sender xem trạng thái mới nhất; pending chưa cấp quyền phòng. Nếu chưa từng có request thì trạng thái trả null.
3. Khi sender hủy, hệ thống kiểm tra expectedVersion và chuyển pending sang cancelled; giao diện hiển thị trạng thái cuối. Sau rejected/cancelled, sender có thể gửi request mới nếu server còn cho phép.

**Ngoại lệ:** Gửi lặp trả cùng pending. Hủy tranh approve/reject/private switch chỉ một transition thắng. Sau server chuyển private, sender vẫn đọc được cancelled/server_private, nhưng không gửi mới qua đường công khai. Join qua đường khác đóng pending với reason joined_elsewhere; UI hiển thị đường tham gia đó.

**Kết quả sau cùng:** Request và trạng thái của chính sender có thể theo dõi; pending/rejected/cancelled không tự tạo membership. Request mới có ID mới, không mở lại request terminal.

<a id="uc-com-08"></a>

<a id="uc-com-08--duyệt-hoặc-từ-chối-yêu-cầu-tham-gia"></a>

### UC-COM-08 — Duyệt hoặc từ chối yêu cầu tham gia

**Module phụ trách:** Community / Memberships.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có review_join_requests.

**Điều kiện trước:** Phiên/server/membership hợp lệ; request còn pending và actor còn quyền review tại thời điểm xử lý.

**Kích hoạt:** Reviewer mở COM-S06, chọn approve hoặc reject.

**Luồng chính:**

1. Hệ thống chỉ trả danh sách request cho reviewer đủ quyền.
2. Reviewer chọn request và gửi quyết định cùng expectedVersion; hệ thống đọc lại trạng thái và quyền.
3. Approve chuyển trạng thái và tạo/kích hoạt membership cùng transaction; reject chỉ chuyển request sang rejected. Sau commit, sender và reviewer đủ quyền nhận trạng thái mới.

**Ngoại lệ:** Request đã cancelled/rejected/resolved không chuyển lần nữa; xung đột tải lại trạng thái. Target phải đủ điều kiện khi approve. Nếu target đã joined bằng đường khác, đóng request theo joined_elsewhere và giữ membership hiện hành. Private switch và sender cancel không thể cùng thắng với approve.

**Kết quả sau cùng:** Approve có một membership active với role mặc định; reject không cấp quyền thành viên. Request cuối trạng thái được giữ để sender theo dõi.

<a id="uc-com-15"></a>

<a id="uc-com-15--rời-cộng-đồng"></a>

### UC-COM-15 — Rời cộng đồng

**Module phụ trách:** Community / Memberships.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Thành viên thường rời tư cách của chính mình.

**Điều kiện trước:** Phiên/tài khoản hợp lệ, có membershipId của lần tham gia muốn rời; actor hiện không phải owner.

**Kích hoạt:** Thành viên chọn rời và xác nhận từ COM-S03.

**Luồng chính:**

1. Hệ thống kiểm tra lại membershipId và tư cách owner hiện hành.
2. Chuyển membership sang left, dọn assignment/ngoại lệ epoch hiện tại và ghi thay đổi quyền trong cùng transaction.
3. Sau commit, chặn truy cập member content, thu hồi subscription theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25) và đóng vùng nội dung/draft của scope trên giao diện.

**Ngoại lệ:** Owner phải hoàn tất [UC-COM-14](servers.md#uc-com-14) trước khi rời. Leave lặp cùng epoch đã left trả thành công; leave cũ sau rejoin nhận MEMBERSHIP_CHANGED, không rời epoch mới. HTTP/send và leave tranh nhau được xử lý bằng guard/giao dịch theo thiết kế.

**Kết quả sau cùng:** Mất quyền nội dung server, DM độc lập vẫn dùng được và tin đã viết được giữ. Muốn rejoin phải đi [UC-COM-06](#uc-com-06), [UC-COM-07](#uc-com-07), [UC-COM-11](invitations.md#uc-com-11), [UC-COM-13](invitations.md#uc-com-13) theo điều kiện hiện hành; dùng membershipId mới và role mặc định.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

COM-S02/06 phục vụ gửi/xem/hủy và review request; COM-S03 phục vụ leave. Trạng thái luồng được quản lý tại bảng dưới đây; bố cục khám phá ở [Servers](servers.md#ux).

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S06 Yêu cầu tham gia | Chủ sở hữu/người có quyền duyệt | Danh sách và thao tác duyệt/từ chối; người gửi xem trạng thái/hủy pending; sau từ chối/hủy được gửi mới |

<a id="trạng-thái-yêu-cầu-tham-gia"></a>

### Trạng thái yêu cầu tham gia

| Đối tượng/trạng thái | Thao tác | Kết quả và quyền |
|---|---|---|
| Chưa có yêu cầu | Gửi vào cộng đồng công khai chờ duyệt | Pending; chưa có membership/quyền phòng |
| Pending | Người có quyền duyệt chấp nhận | Approved và tạo membership cùng transaction |
| Pending | Người có quyền duyệt từ chối | Rejected; không tạo membership |
| Pending | Chính người gửi hủy | Cancelled; không tạo membership |
| Rejected/Cancelled | Người dùng gửi lại | Tạo yêu cầu mới, không phục hồi pending cũ |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](../../system/community.md#ux).


<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="membership-contracts"></a>

<a id="membership-thiết-kế-dữ-liệuapi"></a>

### Approval, requests và leave — thiết kế mục tiêu

Phần [public/immediate và rejoin hiện hành](#publicimmediate-và-rejoin) được mô tả riêng bên dưới. Bảng và transaction trong phần này chỉ mô tả nhánh approval/request/leave mục tiêu; chưa có runtime được nghiệm thu.

HTTP mục tiêu và quy ước chung ở [tích hợp](../../system/community.md#contracts); [OpenAPI Community](../../contracts/community.openapi.json) chứa contract đầy đủ và metadata trạng thái từng operation. Join public/immediate đã có proof; pending/approval/leave còn mục tiêu theo [trạng thái theo khả năng](#implementation). Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](../../system/community.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers/{id}/join` — nhánh approval | Không có body | Mục tiêu: 202 pending cho người chưa active trong public/approval; runtime hiện hành trả 409 `JOIN_APPROVAL_REQUIRED` |
| `GET /servers/{id}/join-requests/me` | Phiên người yêu cầu | 200 `{request: JoinRequest|null}`; chưa có trả null; không trả request người khác |
| `POST /servers/{id}/join-requests/{requestId}/approve` hoặc `/reject` | `{expectedVersion}` | 200 trạng thái cuối; đúng quyền duyệt; chỉ transition từ pending |
| `DELETE /servers/{id}/join-requests/{requestId}` | `expectedVersion` | 200 cancelled; chỉ chính người gửi hủy pending |
| `DELETE /servers/{id}/members/me?membershipId=...` | Epoch đang tham gia | 204 tự rời; owner chưa chuyển nhận 409; epoch cũ không làm rời epoch mới; chặn HTTP/realtime/media theo ngưỡng |

<a id="membership-giao-dịch-của-thành-phần"></a>

#### Giao dịch của thành phần

- Join công khai: khóa server, xét visibility/joinMode hiện hành, rồi membership/request. Đã active trả membership hiện hành. Vào ngay tạo/reactivate membership và kết thúc pending cũ; chờ duyệt có tối đa một pending/server/user, gọi lặp trả cùng pending. Dùng [partial unique index](https://www.postgresql.org/docs/18/indexes-partial.html) cho pending; service vẫn khóa để transition nguyên tử. Join qua đường khác kết thúc request pending bằng approved/reason joined_elsewhere và membershipId hiện hành; UI hiển thị đã tham gia bằng đường khác, không giả reviewer đã duyệt.
- Approve/reject/cancel: kiểm tra actor/target và version; chỉ một transition pending thắng. Approve tạo membership cùng commit; đã joined bằng link/đường khác thì đóng pending với lý do joined_elsewhere, không tạo membership thứ hai. Request mới sau rejected/cancelled có ID mới, không sửa lại lịch sử request cũ.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](../../system/community.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

<a id="publicimmediate-và-rejoin"></a>

### Public/immediate và rejoin — hiện hành

<a id="join-api-và-lỗi"></a>

#### API và lỗi

`POST /api/v1/servers/{serverId}/join` không body; actor lấy từ JWT sub/sid/sst. 200 Membership sau commit. Không clientOperationId: server lock và unique `(serverId,userId)` bảo vệ join lặp; retry chủ động trả tư cách hiện hành.

| HTTP / errorCode | Hành vi |
|---|---|
| 400 VALIDATION_FAILED | Body hoặc đầu vào sai; không nhận userId/role từ client |
| 401 / SESSION_INVALID | Thiếu phiên hoặc guard/expiry không hợp lệ |
| 403 ACCOUNT_ACCESS_DENIED | Account không active hoặc primary email chưa xác minh |
| 404 RESOURCE_NOT_FOUND | Private/inactive/deleted/unknown; không lộ metadata |
| 409 JOIN_APPROVAL_REQUIRED | Người chưa active gặp approval; không ghi membership/request |
| 409 VERSION_LIMIT_REACHED | Version cần tăng đạt giới hạn integer; không wrap hoặc ghi dở |
| 503 ACCESS_CHECK_UNAVAILABLE / COMMUNITY_TEMPORARILY_UNAVAILABLE | Guard/DB/lock tạm lỗi; đối soát bằng GET |

Member active trên public nhận cùng membership kể cả mode approval, không ghi event/version lần nữa. Private chặn route join công khai; active membership trong private vẫn đọc được qua API riêng.

JWT middleware có thể từ chối account inactive bằng 401 trước application guard; guard account/email không đủ điều kiện trả 403. Hai lớp đều không ghi membership.

<a id="join-giao-dịch"></a>

#### Giao dịch

1. Shared work scope READ COMMITTED; Identity guard giữ user share lock và session expiry.
2. SELECT server FOR UPDATE, đọc lại active/deleted/visibility/mode sau khóa. Thứ tự Identity → server → child records áp dụng cho mọi mutation tương lai.
3. Đọc own membership dưới server lock. Active trả record hiện hành; chưa active gặp approval dừng conflict.
4. Kiểm tra overflow server version/accessVersion và membership version cần tăng. Join mới dùng UUIDv7/version 1; rejoin dùng UUIDv7 mới, membership version +1, reset joinedAt/leftAt/nickname/timeout/inviter.
5. Rejoin xóa member_roles và channel_user_overrides của cặp server/user cùng transaction. @everyone áp ngầm. Writer role/ACL và FK/CAS epoch đã có ở gói quản lý quyền/phòng; proof rejoin fixture không thay kiểm chứng leave API hoặc subscription cũ.
6. Tăng accessVersion đúng một lần; server metadata version do trigger tăng. Ghi một Community.MembershipJoined.v1 (aggregate community.server, version từ UPDATE), payload serverId/userId/membershipId/membershipVersion/accessVersion; published_at NULL, chưa phát Hub.
7. Đọc Membership, kiểm tra lại thời gian lease và commit. Mọi cleanup/write/event rollback cùng nhau khi lỗi; không replay mutation trong service.

Không thêm migration: các bảng/cột cần dùng đã có sau migration 001. Khi bổ sung UC-COM-07/08 phải thêm schema/transition pending joined_elsewhere và kiểm chứng lại join.

<a id="join-webclient"></a>

#### WebClient

Public summary qua URL chia sẻ hoặc discovery là điểm vào. Nonmember/left public immediate thấy Tham gia; active/private/approval không có nút join trực tiếp.

Mutation dùng retry:false, bound actor, AbortController, timeout 15 giây và chặn double submit. Đổi route/actor bỏ response cũ. Thành công tải detail/own list bằng GET; lỗi read sau commit không tự POST lại.

Mất mạng/401/503/500 giữ trạng thái cần kiểm tra: Kiểm tra kết quả chỉ GET detail/membership. Active mở detail/cập nhật list; chưa active sau GET thành công và còn public/immediate mới cho người dùng bấm Tham gia lại. Reload chỉ GET; 404 bỏ summary cũ; conflict approval đọc lại mode rồi dừng join. Không giả đã vào phòng khi chưa có phòng/lịch sử.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-02"></a> AC-COM-02 | Người dùng tham gia cộng đồng công khai mới tạo qua tìm kiếm. | Người dùng được vào ngay theo cấu hình mặc định. |
| <a id="ac-com-03"></a> AC-COM-03 | Người dùng yêu cầu tham gia qua tìm kiếm khi cộng đồng bật chờ duyệt. | Yêu cầu được ghi nhận; người dùng chưa là thành viên cho đến khi được duyệt. |
| <a id="ac-com-10"></a> AC-COM-10 | Cộng đồng chờ duyệt có một yêu cầu; ba nhóm người ở [AC-COM-09](invitations.md#ac-com-09) lần lượt thử duyệt. | Chủ sở hữu và người được cấp quyền duyệt được; thành viên thường bị từ chối. |
| <a id="ac-com-21"></a> AC-COM-21 | Thành viên thường chọn rời cộng đồng. | Tư cách thành viên chấm dứt; thành viên không còn quyền truy cập nội dung dành cho thành viên. |
| <a id="ac-com-30"></a> AC-COM-30 | Người có quyền duyệt từ chối; người gửi hủy pending hoặc gửi lại sau từ chối/hủy | Trạng thái đúng, chưa duyệt chưa có membership; một pending hoạt động mỗi cặp; thao tác đồng thời không cùng thắng |
| <a id="ac-com-35"></a> AC-COM-35 | Thành viên rời tham gia lại; mời đích danh hết 7 ngày/đã hủy/từ chối được dùng lại | Rejoin theo join mode hiện hành, role mặc định; lịch sử theo quyền; mời cuối trạng thái không accept được |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [trạng thái theo khả năng](#implementation).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](../messaging/channel-messaging.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-03"></a> TC-COM-03 | P join cộng đồng vào ngay; gọi join lặp/đồng thời | Một membership, có quyền mặc định sau commit | [AC-COM-02](#ac-com-02) |
| <a id="tc-com-04"></a> TC-COM-04 | P gửi yêu cầu chờ duyệt, xem trạng thái; N/M thử duyệt/từ chối | Chỉ đúng quyền xử lý; pending chưa đọc phòng được | [AC-COM-03](#ac-com-03), [AC-COM-10](#ac-com-10), [AC-COM-30](#ac-com-30) |
| <a id="tc-com-05"></a> TC-COM-05 | P hủy pending đồng thời với M duyệt/từ chối; gửi yêu cầu mới sau trạng thái cuối | Một thao tác thắng; trạng thái/membership nhất quán; cho yêu cầu mới sau hủy/từ chối | [AC-COM-30](#ac-com-30) |
| <a id="tc-com-11"></a> TC-COM-11 | N tự rời trong khi đọc/gửi/nhận cập nhật | Chat thu hồi trong ≤5 giây theo DEC-083; DM độc lập còn dùng được | [AC-COM-21](#ac-com-21) |
| <a id="tc-com-16"></a> TC-COM-16 | Thành viên có role riêng rời rồi vào lại theo từng join mode | Role mặc định, không phục hồi role cũ; lịch sử hiện theo quyền mới | [AC-COM-35](#ac-com-35) |
| <a id="tc-com-22"></a> TC-COM-22 | Leave/rejoin, gửi lại PUT role/ACL/leave và event revoke của epoch cũ | Role/override cũ không khôi phục, epoch cũ bị chặn; event cũ không xóa cache epoch mới | [AC-COM-35](#ac-com-35), [AC-COM-42](access-control.md#ac-com-42) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../../project/planning.md#community-work-items), [migration](../../system/community.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Yêu cầu tham gia | DEC-073/096 đã chốt; có transition/schema, còn kiểm chứng approve/cancel/private switch; limiter bổ sung chưa chốt. | OQ-003, OQ-004 |
| Rời cộng đồng | Epoch/clear role/override/transfer có thiết kế; mất quyền dọn cache/role/override epoch cũ, giữ tin; dấu revoke/access floor bảo vệ restore theo [vòng đời](../../system/data-lifecycle.md#restore), metadata khác còn review. | OQ-003, OQ-011 |

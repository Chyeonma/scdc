# SCDC — Memberships — Thành viên và yêu cầu tham gia

Cập nhật: 2026-10-07. Đặc tả nghiệp vụ và tiêu chí kiểm chứng; tiến độ hiện tại tại [status.md](../status.md).

Sở hữu tư cách thành viên, membership epoch và yêu cầu tham gia. Các đường join/approve/accept dùng chung hành vi tạo hoặc kích hoạt membership; leave phối hợp dọn quyền của epoch hiện tại.

[Tổng quan Community](../README.md) · [Truy vết UC/COM/AC/TC](README.md#use-cases) · [Kế hoạch triển khai](../delivery/README.md#use-case-delivery).

Thiết kế kỹ thuật của phần này: [dữ liệu/API và giao dịch](../design/memberships.md#contracts).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Use case](#use-cases)
- [UX và trạng thái](#ux)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-001"></a> COM-001 | Người dùng có thể tham gia qua tìm kiếm hoặc liên kết mời. | DEC-022 |
| <a id="com-011"></a> COM-011 | Chủ sở hữu hoặc người được cấp quyền có thể duyệt yêu cầu tham gia. | DEC-032 |
| <a id="com-021"></a> COM-021 | Thành viên thường có thể tự rời cộng đồng. | DEC-045 |
| <a id="com-030"></a> COM-030 | Người có quyền duyệt được từ chối; người gửi xem trạng thái/hủy pending, sau từ chối/hủy được gửi mới. | DEC-073 |
| <a id="com-035"></a> COM-035 | Rejoin theo join mode hiện hành và vai trò mặc định, không phục hồi role cũ; có quyền xem lại lịch sử phòng hiện được phép xem. | DEC-087, DEC-038 |

<a id="use-cases"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](integration.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](README.md#use-case-coverage).

<a id="uc-com-06"></a>

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

### UC-COM-15 — Rời cộng đồng

**Module phụ trách:** Community / Memberships.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Thành viên thường rời tư cách của chính mình.

**Điều kiện trước:** Phiên/tài khoản hợp lệ, có membershipId của lần tham gia muốn rời; actor hiện không phải owner.

**Kích hoạt:** Thành viên chọn rời và xác nhận từ COM-S03.

**Luồng chính:**

1. Hệ thống kiểm tra lại membershipId và tư cách owner hiện hành.
2. Chuyển membership sang left, dọn assignment/ngoại lệ epoch hiện tại và ghi thay đổi quyền trong cùng transaction.
3. Sau commit, chặn truy cập member content, thu hồi subscription theo [UC-COM-25](integration.md#uc-com-25) và đóng vùng nội dung/draft của scope trên giao diện.

**Ngoại lệ:** Owner phải hoàn tất [UC-COM-14](servers.md#uc-com-14) trước khi rời. Leave lặp cùng epoch đã left trả thành công; leave cũ sau rejoin nhận MEMBERSHIP_CHANGED, không rời epoch mới. HTTP/send và leave tranh nhau được xử lý bằng guard/giao dịch theo thiết kế.

**Kết quả sau cùng:** Mất quyền nội dung server, DM độc lập vẫn dùng được và tin đã viết được giữ. Muốn rejoin phải đi [UC-COM-06](#uc-com-06), [UC-COM-07](#uc-com-07), [UC-COM-11](invitations.md#uc-com-11), [UC-COM-13](invitations.md#uc-com-13) theo điều kiện hiện hành; dùng membershipId mới và role mặc định.

<a id="ux"></a>

## UX và trạng thái

COM-S02/06 phục vụ gửi/xem/hủy và review request; COM-S03 phục vụ leave. Trạng thái luồng được quản lý tại bảng dưới đây; bố cục khám phá ở [Servers](servers.md#ux).

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S06 Yêu cầu tham gia | Chủ sở hữu/người có quyền duyệt | Danh sách và thao tác duyệt/từ chối; người gửi xem trạng thái/hủy pending; sau từ chối/hủy được gửi mới |

### Trạng thái yêu cầu tham gia

| Đối tượng/trạng thái | Thao tác | Kết quả và quyền |
|---|---|---|
| Chưa có yêu cầu | Gửi vào cộng đồng công khai chờ duyệt | Pending; chưa có membership/quyền phòng |
| Pending | Người có quyền duyệt chấp nhận | Approved và tạo membership cùng transaction |
| Pending | Người có quyền duyệt từ chối | Rejected; không tạo membership |
| Pending | Chính người gửi hủy | Cancelled; không tạo membership |
| Rejected/Cancelled | Người dùng gửi lại | Tạo yêu cầu mới, không phục hồi pending cũ |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](integration.md#ux).

<a id="acceptance"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-02"></a> AC-COM-02 | Người dùng tham gia cộng đồng công khai mới tạo qua tìm kiếm. | Người dùng được vào ngay theo cấu hình mặc định. |
| <a id="ac-com-03"></a> AC-COM-03 | Người dùng yêu cầu tham gia qua tìm kiếm khi cộng đồng bật chờ duyệt. | Yêu cầu được ghi nhận; người dùng chưa là thành viên cho đến khi được duyệt. |
| <a id="ac-com-10"></a> AC-COM-10 | Cộng đồng chờ duyệt có một yêu cầu; ba nhóm người ở [AC-COM-09](invitations.md#ac-com-09) lần lượt thử duyệt. | Chủ sở hữu và người được cấp quyền duyệt được; thành viên thường bị từ chối. |
| <a id="ac-com-21"></a> AC-COM-21 | Thành viên thường chọn rời cộng đồng. | Tư cách thành viên chấm dứt; thành viên không còn quyền truy cập nội dung dành cho thành viên. |
| <a id="ac-com-30"></a> AC-COM-30 | Người có quyền duyệt từ chối; người gửi hủy pending hoặc gửi lại sau từ chối/hủy | Trạng thái đúng, chưa duyệt chưa có membership; một pending hoạt động mỗi cặp; thao tác đồng thời không cùng thắng |
| <a id="ac-com-35"></a> AC-COM-35 | Thành viên rời tham gia lại; mời đích danh hết 7 ngày/đã hủy/từ chối được dùng lại | Rejoin theo join mode hiện hành, role mặc định; lịch sử theo quyền; mời cuối trạng thái không accept được |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](README.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [tiến độ](../status.md).

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](integration.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-03"></a> TC-COM-03 | P join cộng đồng vào ngay; gọi join lặp/đồng thời | Một membership, có quyền mặc định sau commit | [AC-COM-02](#ac-com-02) |
| <a id="tc-com-04"></a> TC-COM-04 | P gửi yêu cầu chờ duyệt, xem trạng thái; N/M thử duyệt/từ chối | Chỉ đúng quyền xử lý; pending chưa đọc phòng được | [AC-COM-03](#ac-com-03), [AC-COM-10](#ac-com-10), [AC-COM-30](#ac-com-30) |
| <a id="tc-com-05"></a> TC-COM-05 | P hủy pending đồng thời với M duyệt/từ chối; gửi yêu cầu mới sau trạng thái cuối | Một thao tác thắng; trạng thái/membership nhất quán; cho yêu cầu mới sau hủy/từ chối | [AC-COM-30](#ac-com-30) |
| <a id="tc-com-11"></a> TC-COM-11 | N tự rời trong khi đọc/gửi/nhận cập nhật | Chat thu hồi trong ≤5 giây theo DEC-083; DM độc lập còn dùng được | [AC-COM-21](#ac-com-21) |
| <a id="tc-com-16"></a> TC-COM-16 | Thành viên có role riêng rời rồi vào lại theo từng join mode | Role mặc định, không phục hồi role cũ; lịch sử hiện theo quyền mới | [AC-COM-35](#ac-com-35) |
| <a id="tc-com-22"></a> TC-COM-22 | Leave/rejoin, gửi lại PUT role/ACL/leave và event revoke của epoch cũ | Role/override cũ không khôi phục, epoch cũ bị chặn; event cũ không xóa cache epoch mới | [AC-COM-35](#ac-com-35), [AC-COM-42](permissions.md#ac-com-42) |

<a id="gaps"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../delivery/README.md#use-case-delivery), [migration](../design/integration.md#schema-migration) và [vòng đời dữ liệu](../../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Yêu cầu tham gia | DEC-073/096 đã chốt; có transition/schema, còn kiểm chứng approve/cancel/private switch; limiter bổ sung chưa chốt. | OQ-003, OQ-004 |
| Rời cộng đồng | Epoch/clear role/override/transfer có thiết kế; mất quyền dọn cache/role/override epoch cũ, giữ tin; dấu revoke/access floor bảo vệ restore theo [vòng đời](../../../data-lifecycle.md#restore), metadata khác còn review. | OQ-003, OQ-011 |

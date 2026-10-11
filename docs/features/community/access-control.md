# Vai trò và quyền truy cập Community

Community / Permissions sở hữu role, assignment, thuật toán view, ACL snapshot và evaluator/checker/guard. Quy tắc, luồng sử dụng, UX, thiết kế dữ liệu/API và tiêu chí kiểm chứng của chủ đề được quản lý tại trang này.

<a id="implementation"></a>

## Trạng thái theo khả năng

Đối chiếu hiện trạng ngày 2026-10-08. Nền Community đã hợp nhất vào `main`; bằng chứng dẫn dưới đây gắn revision cụ thể, không phải kết quả chạy lại cho mọi thay đổi sau đó.

| Khả năng | Trạng thái và giới hạn | Bằng chứng |
|---|---|---|
| Role/assignment — phần UC-COM-20/21 | Backend/WebClient và management guard đã kiểm chứng | [Vai trò](../../records/verification/community/roles.md) |
| View policy HTTP và ACL — phần UC-COM-17/22 | Đã kiểm chứng list/detail/metadata/ACL và epoch/CAS | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Channel admission guard | Đã kiểm chứng giữ lock tới caller commit và race role/ACL; chưa có writer tin/Hub sử dụng | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Thu hồi chat ≤5 giây — UC-COM-25 | Chưa có runtime và bằng chứng trên kết nối đang mở | [Tin phòng](../messaging/channel-messaging.md) |

Policy/HTTP guard và outbox đã lưu không đủ chứng minh deadline thu hồi realtime; Media cần bằng chứng riêng.

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
| <a id="com-016"></a> COM-016 | Chủ sở hữu và người được cấp quyền được thay đổi danh sách người có quyền xem phòng. | DEC-039 |
| <a id="com-025"></a> COM-025 | Quyền quản lý và xem phòng cấp qua vai trò; phòng có ngoại lệ cho từng thành viên. | DEC-055 |
| <a id="com-026"></a> COM-026 | Chủ sở hữu luôn xem được mọi phòng trong cộng đồng của mình. | DEC-056 |
| <a id="com-027"></a> COM-027 | Quyền xem giữa các vai trò có từ chối thì từ chối thắng; ngoại lệ cá nhân áp dụng sau cùng, trừ quyền chủ sở hữu. | DEC-057, DEC-056 |
| <a id="com-028"></a> COM-028 | Chỉ chủ sở hữu được tạo/sửa vai trò và gán/thu hồi vai trò thành viên. | DEC-058 |
| <a id="com-037"></a> COM-037 | @everyone tự áp cho mọi thành viên, không xóa/không có quyền quản lý; tối đa 20 vai trò tự tạo, tên 1–64 UTF-16; quyền quản lý cộng dồn. | DEC-092, DEC-058 |
| <a id="com-042"></a> COM-042 | Sửa/xóa/đổi cấu hình xem phòng có sẵn cần quyền xem phòng và đúng quyền quản lý; tạo mới chỉ cần quyền tạo/quản lý phòng. Quyền quản lý không mở phòng bị ẩn. | DEC-098 |

<a id="permissions"></a>

<a id="ma-trận-quyền"></a>

## Ma trận quyền

“Người được cấp quyền” là người có quyền đúng thao tác đang xét; quyền tạo lời mời không tự cho quyền duyệt thành viên hoặc quản lý vai trò. Actor, phiên và tư cách thành viên được kiểm tra ở máy chủ. Quyền Accounts/DM độc lập với vai trò cộng đồng.

| Mã | Thao tác | Điều kiện được phép | Điều kiện bổ sung hoặc giới hạn | Căn cứ |
|---|---|---|---|---|
| <a id="acl-06"></a> ACL-06 | Thấy cộng đồng trong tìm kiếm | Cộng đồng công khai | Cộng đồng riêng tư không xuất hiện | DEC-024 |
| <a id="acl-07"></a> ACL-07 | Tham gia qua tìm kiếm | Cộng đồng công khai | Vào ngay hoặc chờ duyệt theo cấu hình; chưa duyệt chưa có tư cách thành viên | DEC-025 |
| <a id="acl-08"></a> ACL-08 | Tham gia bằng liên kết mời | Liên kết hợp lệ, còn hiệu lực | Bỏ qua chế độ chờ duyệt; liên kết hết hạn/thu hồi bị từ chối | DEC-025, DEC-044 |
| <a id="acl-09"></a> ACL-09 | Tạo/thu hồi lời mời | Chủ sở hữu hoặc người có vai trò cho quyền tạo lời mời | Hạn/lượt theo [COM-032](invitations.md#com-032); kiểm tra thu hồi và giới hạn trong transaction join | DEC-028, DEC-031, DEC-044, DEC-055 |
| <a id="acl-10"></a> ACL-10 | Duyệt yêu cầu tham gia | Chủ sở hữu hoặc người được cấp quyền duyệt | Người có quyền duyệt được từ chối; người gửi hủy yêu cầu đang chờ theo DEC-073 | DEC-032 |
| <a id="acl-11"></a> ACL-11 | Đổi chế độ tham gia | Chủ sở hữu hoặc người được cấp quyền đổi chế độ | Không thay đổi nguyên tắc liên kết mời hợp lệ cho vào ngay | DEC-029 |
| <a id="acl-12"></a> ACL-12 | Tạo/sửa/xóa phòng | Chủ sở hữu hoặc người có quyền quản lý phòng | Sửa/xóa cần quyền xem phòng theo DEC-098; phòng mới mặc định mọi thành viên xem; xóa chặn HTTP/realtime/media, chưa có khôi phục | DEC-026, DEC-033, DEC-077/098 |
| <a id="acl-13"></a> ACL-13 | Xem phòng và lịch sử | Là thành viên và có quyền xem phòng; chủ sở hữu luôn xem được | Thành viên mới được xem tin cũ; thứ tự vai trò/ngoại lệ cá nhân ở [thuật toán quyền xem](#view-permissions) | DEC-027, DEC-038, DEC-055–057 |
| <a id="acl-14"></a> ACL-14 | Gửi tin trong phòng | Có quyền xem phòng, phiên hợp lệ và đã xác minh email | Đợt đầu không có quyền chỉ đọc riêng | DEC-040, DEC-041 |
| <a id="acl-15"></a> ACL-15 | Sửa/xóa tin trong phòng | Có quyền truy cập phòng và là tác giả tin | Không cấp quyền sửa/xóa tin của người khác trong đặc tả đợt đầu | DEC-034, [AC-COM-13](../messaging/channel-messaging.md#ac-com-13) |
| <a id="acl-16"></a> ACL-16 | Đổi cấu hình quyền xem phòng | Chủ sở hữu hoặc người có vai trò cho quyền quản lý danh sách xem | Cần quyền xem phòng đang cấu hình theo DEC-098; không đồng nghĩa được quản lý vai trò | DEC-039, DEC-055, DEC-058/098 |
| <a id="acl-17"></a> ACL-17 | Tự rời cộng đồng | Thành viên thường | Chủ sở hữu phải chuyển ngay cho thành viên active/đã xác minh trước khi rời — DEC-076 | DEC-045 |
| <a id="acl-18"></a> ACL-18 | Mời đích danh vào cộng đồng riêng tư | Chủ sở hữu/người có quyền tạo mời gửi; đúng người nhận chấp nhận | Chưa chấp nhận thì chưa có tư cách thành viên/quyền phòng | DEC-043, DEC-074 |
| <a id="acl-19"></a> ACL-19 | Tạo/sửa vai trò, gán/thu hồi vai trò thành viên | Chỉ chủ sở hữu cộng đồng | Không ủy quyền quản lý vai trò trong đợt đầu | DEC-058 |
| <a id="acl-20"></a> ACL-20 | Tạo/sửa cộng đồng | Tài khoản đã xác minh được tạo; chủ sở hữu mới được sửa tên/mô tả/công khai | Quyền đổi chế độ vào ngay/chờ duyệt vẫn là [ACL-11](#acl-11) | DEC-072 |

Tư cách chủ sở hữu không tự cho quyền đọc DM, sửa tin của người khác hoặc
truy cập nội dung ngoài cộng đồng của mình. Chủ sở hữu luôn xem được
mọi phòng trong cộng đồng của mình (DEC-056), nhưng vẫn phải thỏa điều
kiện tài khoản/phiên. Vai trò được quản lý riêng theo [ACL-19](#acl-19).

<a id="view-permissions"></a>

<a id="thứ-tự-tính-quyền-xem-đã-chốt"></a>

## Thứ tự tính quyền xem đã chốt

1. Kiểm tra phiên hợp lệ, tài khoản đủ điều kiện, cộng đồng/phòng còn
   tồn tại và người dùng còn là thành viên. Không có điều kiện nền thì
   từ chối trước khi xét quyền.
2. Nếu là chủ sở hữu của chính cộng đồng này, cho xem (DEC-056).
3. Lấy quyền xem mặc định của phòng: phòng mới cho mọi thành viên xem
   trừ khi được đặt giới hạn (DEC-033).
4. Áp cấu hình quyền xem theo các vai trò người dùng có trong phòng:
   có từ chối thì từ chối; nếu không có từ chối nhưng có cho phép thì
   cho phép; không có cấu hình riêng thì giữ mặc định (DEC-055/057).
5. Áp ngoại lệ cá nhân sau cùng: cho phép hoặc từ chối thay kết quả
   vai trò; không đặt ngoại lệ thì giữ kết quả bước 4 (DEC-057).
6. Đợt đầu, có quyền xem và đã xác minh email thì được gửi tin; không
   thêm quyền chỉ đọc độc lập (DEC-040/041).

Thiết kế dữ liệu dùng ba trạng thái “kế thừa/cho phép/từ chối” cho cấu hình quyền xem. @everyone và giới hạn vai trò đã chốt DEC-092; cách lưu được mô tả ở [thiết kế chi tiết](#permission-detailed-design). Quyền quản lý là hợp các quyền cho phép từ vai trò tự tạo; không có DENY quản lý hoặc ngoại lệ quản lý cá nhân trong v1. DEC-057 quyết định xung đột **quyền xem phòng**.

| Mặc định/va chạm | Ngoại lệ cá nhân | Kết quả xem (thành viên thường) |
|---|---|---|
| Mọi thành viên xem; không cấu hình vai trò | Kế thừa | Cho phép |
| Vai trò cho phép và vai trò từ chối | Kế thừa | Từ chối |
| Vai trò từ chối | Cho phép | Cho phép |
| Vai trò cho phép | Từ chối | Từ chối |
| Phòng giới hạn; không có cấu hình cho phép | Kế thừa | Từ chối |

<a id="revocation"></a>

<a id="quyền-thay-đổi-khi-đang-sử-dụng"></a>

## Quyền thay đổi khi đang sử dụng

| Tình huống | Kết quả phải kiểm chứng | Tiêu chí liên quan |
|---|---|---|
| Thu hồi quyền xem phòng đang mở | Lần đọc/gửi tiếp theo bị từ chối; không nhận thêm nội dung phòng qua kết nối cũ trong ≤5 giây cho chat theo DEC-083; media ≤5 giây theo DEC-099, cần proof riêng | [AC-COM-11](#ac-com-11), [AC-COM-13](../messaging/channel-messaging.md#ac-com-13); DEC-083, OQ-007 |
| Thành viên thường rời cộng đồng | Không còn truy cập nội dung dành cho thành viên; giao diện rời phòng đang mở | [AC-COM-21](memberships.md#ac-com-21) |
| Mất quyền quản lý | Các thao tác quản lý tiếp theo bị kiểm tra lại phía máy chủ | [ACL-09](#acl-09) đến [ACL-12](#acl-12), [ACL-16](#acl-16) |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách thành viên và quyền trước khi tải lịch sử hoặc đăng ký nhận tin | AC-ACC-04, [AC-COM-13](../messaging/channel-messaging.md#ac-com-13) |
| Thu hồi quyền một phòng | Không tự thu hồi quyền ở phòng khác hoặc DM độc lập | Suy ra từ phạm vi quyền phòng; cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã nhìn thấy hoặc tự sao chép.
Việc ngừng hiển thị dữ liệu đã tải trong giao diện, cách xử lý bộ nhớ đệm
và độ trễ thu hồi cần được cụ thể hóa trong thiết kế và OQ-007/OQ-011.


Quy tắc có thẩm quyền là DEC-055–058: giữa vai trò có DENY thì DENY thắng, cá nhân áp dụng cuối; chủ sở hữu được xem sau khi thỏa điều kiện nền. Cách mã hóa bằng bit mask 64-bit từng được đề xuất là lựa chọn biểu diễn dữ liệu; nó không được thay thứ tự ưu tiên đã chốt. Không suy “toàn quyền tuyệt đối” thành quyền sửa/xóa tin của người khác hoặc đọc DM.

<a id="use-cases"></a>

<a id="use-case"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-20"></a>

<a id="uc-com-20--tạo-sửa-và-xóa-vai-trò-tự-tạo"></a>

### UC-COM-20 — Tạo, sửa và xóa vai trò tự tạo

**Module phụ trách:** Community / Permissions.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Chỉ owner hiện hành.

**Điều kiện trước:** Actor/server/membership hợp lệ; role sửa/xóa thuộc server và là custom role, đã tải version.

**Kích hoạt:** Owner mở COM-S08 để quản lý định nghĩa vai trò.

**Luồng chính:**

1. Owner đọc danh mục, nhập tên và chọn management permission của custom role theo [COM-037](#com-037).
2. Hệ thống kiểm tra owner/dữ liệu/giới hạn; tạo role theo operation key hoặc sửa role với expectedVersion.
3. Khi xóa, hệ thống bỏ role cùng assignment/role override, cập nhật phiên bản quyền trong cùng transaction; sau commit, tính lại quyền và thông báo tải lại.

**Ngoại lệ:** Chỉ một create thắng khi tranh slot thứ 20. Tên trùng, permission sai/trùng, version cũ hoặc actor mất ownership bị từ chối. @everyone không sửa/xóa/gán tay/cấp management. Xóa role deny có thể mở view, xóa role allow có thể mất view; phải tính lại kết quả thực.

**Kết quả sau cùng:** Danh mục custom role hợp lệ; management là hợp quyền allow, không có DENY quản lý/hierarchy vượt owner. Mất view do thay đổi role phải thu hồi theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

<a id="uc-com-21"></a>

<a id="uc-com-21--gán-hoặc-thu-hồi-vai-trò-thành-viên"></a>

### UC-COM-21 — Gán hoặc thu hồi vai trò thành viên

**Module phụ trách:** Community / Permissions.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Chỉ owner hiện hành.

**Điều kiện trước:** Actor/target đủ điều kiện liên quan; target còn membership active của server. Owner đã tải membershipId/version và tập role hiện tại.

**Kích hoạt:** Owner chọn thành viên tại COM-S08 và lưu tập custom role.

**Luồng chính:**

1. Owner đọc roster và tập vai trò của target; roster chỉ có user summary và membership, không có email.
2. Owner thêm/bỏ role rồi gửi toàn bộ tập custom role cùng membershipId/expectedVersion.
3. Hệ thống kiểm tra owner, epoch/version và role cùng server rồi thay assignment nguyên tử, cập nhật quyền; sau commit, target tải lại quyền/subscription theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Ngoại lệ:** Target đã leave/rejoin, role khác server/trùng hoặc system role không được gán. Actor có manage_channels/manage_channel_access vẫn không được thay assignment. Membership/version cũ không áp vào lần tham gia mới.

**Kết quả sau cùng:** Tập role của đúng epoch được lưu; @everyone tự áp theo membership active. Management permission không tự mở view của phòng bị ẩn.

<a id="uc-com-22"></a>

<a id="uc-com-22--xem-và-thay-cấu-hình-quyền-xem-phòng"></a>

### UC-COM-22 — Xem và thay cấu hình quyền xem phòng

**Module phụ trách:** Community / Permissions.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có manage_channel_access và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; actor đã tải ACL snapshot/accessVersion. Member override chỉ dùng membership active đúng epoch.

**Kích hoạt:** Actor mở COM-S09 và lưu mặc định phòng, role override hoặc member override.

**Luồng chính:**

1. Hệ thống kiểm tra quyền đọc ACL, trả cấu hình và danh mục role/member phục vụ chọn ngoại lệ trong scope được phép.
2. Actor chỉnh snapshot và gửi expectedAccessVersion; hệ thống kiểm tra view/manage_channel_access cùng role/member/epoch hợp lệ.
3. Thay snapshot nguyên tử, tăng accessVersion và tính lại view theo default → role deny thắng → cá nhân sau cùng; owner theo [COM-026](#com-026). Sau commit, thu hồi scope mất view theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Ngoại lệ:** Role/member duplicate/khác server/epoch cũ hoặc accessVersion cũ rollback toàn cấu hình. Management permission không cho đọc ACL của hidden channel. Actor tự mất view qua cấu hình hợp lệ vẫn nhận kết quả commit, nhưng request/subscription tiếp theo bị chặn. DENY owner không vượt quyền owner sau điều kiện nền.

**Kết quả sau cùng:** ACL nhất quán và áp dụng cho đọc/gửi/cập nhật; cấu hình ACL không cấp quyền quản lý role hoặc quyền sửa/xóa tin người khác.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

```text
COM-S09· Quyền xem phòng
┌─────────────────────────────────────────────────┐
│ Quyền xem #hoc-tap                               │
│ Mặc định: [Mọi thành viên / Giới hạn]             │
│                                                 │
│ Vai trò          Kế thừa / Cho phép / Từ chối     │
│ • Thành viên A   [            lựa chọn         ] │
│                                                 │
│ Ngoại lệ cá nhân Kế thừa / Cho phép / Từ chối     │
│ • @nguoi_dung    [            lựa chọn         ] │
│                                                 │
│ Từ chối giữa vai trò thắng; cá nhân ưu tiên cuối. │
│ Chủ sở hữu luôn xem được phòng.                  │
│ [Hủy]                                    [Lưu]  │
└─────────────────────────────────────────────────┘
```

Bố cục COM-S09 là cách diễn đạt đề xuất của thuật toán quyền. Chỉ chủ
sở hữu sửa định nghĩa hoặc gán vai trò; quyền cấu hình xem phòng không
cho phép tự gán vai trò. Cách tránh người quản lý vô tình tự mất quyền
và phản hồi sau lưu cần kiểm thử trong prototype.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S08 Vai trò | Chỉ chủ sở hữu | Danh sách vai trò, quyền quản lý theo thao tác, gán/thu hồi thành viên; người khác gọi API cũng bị từ chối |
| COM-S09 Quyền xem phòng | Chủ sở hữu/người có quyền quản lý danh sách xem | Mặc định phòng, cấu hình vai trò, ngoại lệ cá nhân; kết quả theo DEC-057; lưu có version để không ghi đè cấu hình mới hơn |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](../../system/community.md#ux).

<a id="acl-editor-ui"></a>

### Giao diện cấu hình ACL

Role/member catalogs chỉ tải khi actor có view và quyền sửa ACL. Roster phân trang; member override hiện hữu giữ nguyên ID/membership epoch kể cả khi thành viên chưa nằm trong trang đang tải. Allow/deny/inherit được gửi như snapshot đầy đủ; không tự thay epoch cũ thành epoch mới.

Draft giữ accessVersion và epoch đã đọc. Conflict/timeout cần GET đối soát trước lần lưu mới; không tự PUT lại. Actor/server/selection đổi hủy đọc cũ và bỏ dữ liệu quản lý; 401/403/404 đóng ACL cũ. Quyền client chỉ điều khiển UI; máy chủ kiểm tra lại dưới khóa.


<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="permission-contracts"></a>

<a id="permission-thiết-kế-dữ-liệuapi"></a>

### Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](../../system/community.md#contracts); [OpenAPI Community](../../contracts/community.openapi.json) giữ schema máy đọc và đánh dấu phạm vi từng route. [Gói role/assignment](access-control.md) chọn quản lý vai trò và roster; [gói phòng text/ACL](channels.md) bổ sung endpoint ACL, view policy và guard transaction. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](../../system/community.md#transactions).

<a id="permission-detailed-design"></a>

<a id="permission-vai-trò-và-cấu-hình-quyền"></a>

#### Vai trò và cấu hình quyền

| Mã quyền kỹ thuật | Thao tác được cấp | Không tự cấp |
|---|---|---|
| `manage_channels` | Tạo/sửa/xóa phòng | Quyền xem tin của mọi phòng, đổi ACL hoặc quản lý role |
| `manage_invites` | Tạo/thu hồi link và gửi/hủy mời đích danh | Duyệt yêu cầu hoặc gán role |
| `review_join_requests` | Xem danh sách pending, duyệt/từ chối | Tạo mời hoặc quản lý role |
| `manage_join_mode` | Đổi vào ngay/chờ duyệt | Đổi visibility/name/description |
| `manage_channel_access` | Đọc/sửa cấu hình xem phòng và chọn thành viên cho ngoại lệ | Quyền đọc tin bị giới hạn hoặc quản lý role |

Owner có các quyền quản lý trên và các thao tác riêng [ACL-19](#acl-19), [ACL-20](#acl-20)/chuyển ownership sau kiểm tra nền; không có quyền sửa/xóa tin người khác. @everyone là role hệ thống duy nhất của server, tự áp cho mọi membership active, không cần insert member_roles cho từng người. Không đổi tên/xóa/gán tay role này hoặc cấp management permission. Role tự tạo có tập permission cho phép; effective management là hợp tập này, không phụ thuộc role position và không có hierarchy vượt owner. Giới hạn 20 role tự tạo kiểm tra dưới khóa server, không tính @everyone.

<a id="acl-api"></a>

API role/assignment/ACL trong phạm vi đã triển khai:

| Route bổ sung | Quyền và kết quả |
|---|---|
| `GET /servers/{id}/roles` | Owner/manage_channel_access đọc danh mục để chọn role; chỉ owner được thay role/assignment |
| `POST /servers/{id}/roles` | Owner; `{clientOperationId,name,permissions}`; 201 role |
| `PATCH /servers/{id}/roles/{roleId}` | Owner; `{name?,permissions?,expectedVersion}`; 200 role; system role không sửa |
| `DELETE /servers/{id}/roles/{roleId}?expectedVersion=...` | Owner; xóa custom role và assignment/override cùng transaction; 204 |
| `GET /servers/{id}/members` | Owner hoặc manage_channel_access; trang user summary/membership để chọn role/ngoại lệ, không có email |
| `GET /servers/{id}/members/{userId}/roles` | Owner; danh sách custom role, membershipId/version |
| `PUT /servers/{id}/members/{userId}/roles` | Owner; `{membershipId,expectedVersion,roleIds}` thay tập custom role đầy đủ, không gán role server khác |
| `GET /servers/{id}/channels/{channelId}/access` | Owner/manage_channel_access và còn quyền xem phòng; base + override/accessVersion, không trả tin |
| `PUT /servers/{id}/channels/{channelId}/access` | Cùng điều kiện; thay cấu hình xem có expectedAccessVersion; một lần cập nhật nguyên tử |

ACL snapshot có `defaultView: allow|deny`, `roleOverrides:[{roleId,effect:allow|deny}]`, `memberOverrides:[{userId,membershipId,effect:allow|deny}]`. Không có entry nghĩa inherit; không dùng số bit mask trên wire. Tối đa 21 role overrides, gồm @everyone. Role/user duplicate hoặc thuộc server khác nhận 400; member override chỉ nhận membership active đúng epoch. Policy áp dụng theo [thứ tự tính view](#view-permissions).

PUT ACL tăng channel accessVersion/version và server accessVersion, ghi access-change outbox cùng transaction. Role delete/rejoin cleanup cũng tăng accessVersion của channel bị mất override để CAS snapshot cũ không ghi đè. No-op giữ version/event. Actor tự làm mất view nhận snapshot đã commit; GET tiếp theo trả 404.

Xóa custom role bỏ role assignment/role override và tăng accessVersion; kết quả có thể mở hoặc đóng quyền xem tùy role đã allow/deny, phải tính lại thay vì giả mọi xóa role là thu hồi. Rời server xóa assignment/ngoại lệ của epoch hiện tại; rejoin chỉ @everyone theo DEC-087. Chuyển owner không tự gán role quản lý cho chủ cũ; quyền chủ cũ trở lại theo role hiện có.

<a id="permission-implementation-boundary"></a>

<a id="permission-evaluator-quản-lý-quyền-và-guard"></a>

#### Evaluator, quản lý quyền và guard

Phân chia kỹ thuật đã thống nhất ngày 2026-10-06:

- Domain chứa permission catalog, role/ACL models và evaluator tính trên snapshot; evaluator không gọi Application service hoặc DB.
- Application chứa các use case quản lý role/assignment/ACL và phối hợp transaction, version/epoch.
- Infrastructure đọc snapshot hiện hành và triển khai checker/guard; guard giữ row lock theo [thiết kế transaction](../../system/community.md#transactions), không thay bằng kết quả bool hoặc cache chưa xác nhận.

Các phần khác dùng kết quả policy/guard; điều kiện Identity qua Contracts. Đổi role/assignment/ACL cập nhật accessVersion và outbox cùng transaction với mutation. [Gói role/assignment](../../records/verification/community/roles.md) có evaluator domain, management guard và writer role/assignment. [Gói phòng/ACL](channels.md) đọc policy hiện hành và giữ khóa server/channel trong transaction caller qua IChannelAccessGuard; kiểm chứng HTTP và race role/ACL, chưa có consumer tin/subscription hoặc thu hồi realtime.

Đối chiếu baseline/mapping mục tiêu ở [COM-SQL-01–10](../../system/community.md#schema-migration); migration/proof được ghi theo từng gói ở [trạng thái theo khả năng](#implementation).

### Lưu trữ, giao dịch và UI vai trò

<a id="role-dữ-liệu-và-migration"></a>

### Dữ liệu và migration

003 thêm roles.name_key theo cùng .NET trim/NFC/ToLowerInvariant của search, unique C theo server; name 64 UTF-16, role.version int32 với một trigger tăng version/timestamp. Giữ normalized_name legacy nhưng bỏ unique theo locale cũ. @everyone được create writer ghi key explicit. Assignment/member override thêm membership_id, FK `(server_id,user_id,membership_id)`; join/rejoin dọn grants trước đổi epoch. Không suy epoch từ user khi xử lý request cũ.

Runner yêu cầu ledger/checksum 001/002, cùng advisory lock và table locks. Key backfill theo batch 500, validate tên/collision/20-role cap/catalog và chỉ channel_view override. Dữ liệu không hợp lệ báo cần sửa có review, rollback; không đổi tên/quyền/epoch âm thầm. Bootstrap hiện hành cho DB mới gồm 001–004; seed dùng đúng catalog/key/epoch. Khi nâng cấp DB ở baseline 001/002: drain writer cũ → migrate 003 rồi các migration còn thiếu → deploy writer tương thích; không chạy bootstrap có DROP SCHEMA trên DB cần giữ dữ liệu.

Operation create_role scope là serverId; fingerprint name sau trim + permission mask byte theo thứ tự bit đã công bố. Permission array thứ tự khác cho cùng fingerprint, duplicate/unknown bị chặn. HMAC keys/rotation dùng cơ chế create-server. Retry kiểm tra owner hiện hành và resource còn tồn tại; role đã xóa không được tạo lại bằng key cũ.

<a id="role-api-và-giao-dịch"></a>

### API và giao dịch

Các route/DTO theo OpenAPI: GET/POST roles, PATCH/DELETE role, GET members, GET/PUT member roles. Role/member pages mặc định 20, tối đa 50, keyset UUID tăng; cursor purpose riêng cho từng collection, actor/server/limit/position/24 giờ. Target roleIds chỉ custom cùng server, tối đa 20, không duplicate; PUT thay tập đầy đủ, @everyone không nằm trong roleIds. Version wire là chuỗi số dương; storage hiện hành int32, overflow dừng trước ghi.

Identity guard khóa account actor → server FOR UPDATE cho mutation/FOR SHARE cho read → membership/role. Server lock ổn định ownership, authorization và limit/CAS tới commit; lease kiểm tra lại trước commit. Private/nonmember/inactive trả 404; member thiếu quyền trả 403. Role/member catalog đọc bởi owner hoặc manage_channel_access; writer owner-only, không coi manage_channel_access là quyền quản lý role. Roster lấy UserSummary qua Identity contract, user null khi directory không còn account active; không trả email, không cần session của target để gán role cho membership active.

Create/sửa/xóa/assignment đổi dữ liệu bump server accessVersion và outbox Community.AccessChanged.v1 cùng transaction. Event chỉ scope/server/accessVersion/cause/resource IDs, không có tên/member roster hoặc nội dung; chưa dispatcher/Hub. Assignment tăng membership.version; delete role cascade assignment/role override và tăng version membership bị đổi, để request cũ không ghi đè. Role update tăng role.version đúng một lần. No-op CAS hợp lệ trả hiện trạng, không bump/outbox. DELETE dùng expectedVersion, role đã mất trả 404; legacy invite còn giữ custom role nhận 409 ROLE_IN_USE, không âm thầm đổi lời mời.

Management guard dùng catalog/evaluator hiện hành; ServerDetail/own list đọc assignment đúng epoch. Evaluator domain thuần giữ foundation → owner → default/role deny-wins → personal; management union độc lập view. Fixture domain và view HTTP/ACL/guard đã có bằng chứng ở gói phòng; writer tin, Hub và deadline thu hồi vẫn chưa được kiểm chứng.

<a id="role-webclient"></a>

### WebClient

Detail của owner có lối vào quản lý vai trò và thành viên. Role create giữ operation/body theo actor/server trong sessionStorage trước POST; lỗi không rõ giữ cùng key để retry chủ động, có thể phục hồi sau reload. Sửa/xóa/gán dùng version/epoch đã tải, retry:false; mất response hoặc conflict yêu cầu GET hiện trạng và người dùng lưu lại. Không tự replay mutation; actor/server đổi bỏ response cũ và reset state.

Tập role tải đầy đủ (tối đa 21 kể cả system), roster có phân trang. @everyone chỉ hiển thị trạng thái bảo vệ; chọn target tải epoch/version rồi gán role. Khi lỗi mất quyền, bỏ dữ liệu quản lý cũ; không dùng UI làm nguồn authorization. Mobile/keyboard/loading/empty/error được kiểm tra bằng Chromium với API thật.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-06"></a> AC-COM-06 | Một thành viên không có quyền xem phòng X đăng nhập. | Phòng X không hiện trong danh sách phòng của thành viên đó. |
| <a id="ac-com-11"></a> AC-COM-11 | Tạo phòng mới không đặt giới hạn xem; sau đó giới hạn quyền xem của một thành viên. | Ban đầu mọi thành viên thấy phòng; sau thay đổi, thành viên bị giới hạn không còn thấy hoặc truy cập được phòng. |
| <a id="ac-com-16"></a> AC-COM-16 | Chủ sở hữu, người được cấp quyền và thành viên thường thử đổi danh sách người được xem phòng. | Hai nhóm đầu thực hiện được; thành viên thường bị từ chối ở máy chủ. |
| <a id="ac-com-17"></a> AC-COM-17 | Một thành viên có quyền xem phòng gửi tin văn bản; một người không có quyền xem thử gửi. | Người thứ nhất gửi được; người thứ hai bị từ chối ở máy chủ. |
| <a id="ac-com-25"></a> AC-COM-25 | Thành viên có hai vai trò cho phép/từ chối xem cùng phòng, chưa đặt ngoại lệ cá nhân. | Thành viên không thấy, đọc, gửi hoặc nhận cập nhật phòng đó. |
| <a id="ac-com-26"></a> AC-COM-26 | Đặt ngoại lệ cho phép cá nhân ở ca [AC-COM-25](#ac-com-25); sau đó đổi thành từ chối. | Cho phép cá nhân thắng từ chối vai trò; khi cá nhân bị từ chối thì không xem/gửi/nhận được. |
| <a id="ac-com-27"></a> AC-COM-27 | Chủ sở hữu mở phòng giới hạn có cấu hình từ chối vai trò/cá nhân. | Vẫn xem được trong cộng đồng của mình nếu tài khoản/phiên hợp lệ. |
| <a id="ac-com-28"></a> AC-COM-28 | Thành viên được quyền tạo phòng thử sửa hoặc gán vai trò; chủ sở hữu làm cùng thao tác. | Thành viên bị từ chối; chủ sở hữu thực hiện được. |
| <a id="ac-com-36"></a> AC-COM-36 | Tạo role thứ 20/21, sửa/xóa @everyone, hai role cấp quyền quản lý khác nhau | Tối đa 20 custom role; @everyone không sửa/xóa/cấp management; quyền quản lý là hợp theo DEC-092 |
| <a id="ac-com-42"></a> AC-COM-42 | Manager có manage_channels/manage_channel_access nhưng không view phòng | Không thấy metadata/phòng/tin hoặc sửa/xóa/ACL phòng đó; owner vẫn quản lý, tạo mới theo management permission theo DEC-098 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [trạng thái theo khả năng](#implementation).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](../messaging/channel-messaging.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-acl-01"></a> TC-ACL-01 | Phòng mới chưa giới hạn; N đã xác minh | N xem và gửi được; thấy lịch sử cũ | [AC-COM-11](#ac-com-11), [AC-COM-15](channels.md#ac-com-15), [AC-COM-17](#ac-com-17) |
| <a id="tc-acl-02"></a> TC-ACL-02 | N có cả R-allow và R-deny, không có ngoại lệ cá nhân | Không xem, không gửi hoặc nhận nội dung phòng | DEC-057 |
| <a id="tc-acl-03"></a> TC-ACL-03 | Như [TC-ACL-02](#tc-acl-02) nhưng cá nhân N được cho phép | N xem/gửi được nếu thỏa điều kiện tài khoản/thành viên | DEC-057 |
| <a id="tc-acl-04"></a> TC-ACL-04 | Vai trò cho phép, ngoại lệ cá nhân từ chối | N không xem/gửi được | DEC-057 |
| <a id="tc-acl-05"></a> TC-ACL-05 | O mở phòng giới hạn, gồm cả cấu hình từ chối theo vai trò/cá nhân | O vẫn xem được trong cộng đồng của mình; không vượt điều kiện phiên/tài khoản | DEC-056 |
| <a id="tc-acl-06"></a> TC-ACL-06 | M được quyền tạo phòng nhưng thử tạo/sửa/gán/thu hồi vai trò | Máy chủ từ chối; O thực hiện được | DEC-058 |
| <a id="tc-acl-07"></a> TC-ACL-07 | N đang mở phòng; O thu hồi quyền hoặc N rời cộng đồng | API từ chối; ngừng nhận nội dung phòng theo ngưỡng thu hồi; DM độc lập vẫn hoạt động | [AC-COM-11](#ac-com-11), [AC-COM-21](memberships.md#ac-com-21) |
| <a id="tc-acl-08"></a> TC-ACL-08 | Chạy 18 fixture nền/owner/role/cá nhân/quyền quản lý/kind | Khớp [community-permissions.json](../../fixtures/community-permissions.json); không coi fixture đã khớp là API đã đạt | [AC-COM-06](#ac-com-06), [AC-COM-17](#ac-com-17), [AC-COM-36](#ac-com-36), [AC-COM-42](#ac-com-42) |
| <a id="tc-acl-09"></a> TC-ACL-09 | O tạo hai custom role tranh slot thứ 20; gọi sửa/xóa/gán @everyone | Một role mới, role còn lại ROLE_LIMIT_REACHED; system role bị bảo vệ | [AC-COM-36](#ac-com-36) |
| <a id="tc-acl-10"></a> TC-ACL-10 | PUT ACL với role/member khác server, duplicate, epoch cũ; actor làm mất view của mình | Cấu hình sai rollback toàn bộ; cấu hình hợp lệ commit, request/subscription tiếp theo bị chặn | [AC-COM-16](#ac-com-16), [AC-COM-42](#ac-com-42) |
| <a id="tc-acl-11"></a> TC-ACL-11 | Xóa role deny hoặc allow khi user đang xem phòng | Tính lại kết quả thực, không suy mọi xóa là deny; view mất thì thu hồi ≤5 giây | DEC-057/083 |
| <a id="tc-acl-12"></a> TC-ACL-12 | M biết ID phòng bị ẩn và gọi GET/PATCH/DELETE/GET-ACL/PUT-ACL | 404 không metadata; đúng management permission vẫn cần view; owner không vượt điều kiện nền | [AC-COM-42](#ac-com-42) |


Các race guard/outbox còn được kiểm chứng tại [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](../messaging/channel-messaging.md#tc-com-25), [TC-COM-26](../../system/community.md#tc-com-26); Channels/Messaging phải chạy ca tích hợp bên cạnh fixture quyền.

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Các mục dưới đây giữ yêu cầu kiểm chứng còn lại, không phải bảng tiến độ. Phần đã triển khai/migration/test được quản lý tại [trạng thái theo khả năng](#implementation), [đối chiếu test](../../records/verification/community/README.md) và hồ sơ nghiệm thu; migration mục tiêu theo [thiết kế](../../system/community.md#schema-migration), retention theo [vòng đời dữ liệu](../../system/data-lifecycle.md).

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Cấp quyền | Phần chưa chứng minh: writer lịch sử/tin dùng guard, subscription và deadline thu hồi; approval/lời mời/transfer/leave/Media có proof theo gói riêng. Không suy kết quả policy/HTTP thành toàn bộ UC đạt. | OQ-004, OQ-007, OQ-008 |

<a id="đầu-việc-rà-soát-và-kiểm-chứng-phân-quyền"></a>

### Đầu việc rà soát và kiểm chứng phân quyền

| Mã theo dõi | Thiết kế đã có và việc còn lại | Phần bị ảnh hưởng | Đầu mối dự kiến |
|---|---|---|---|
| ACL-O1 | DEC-092/098 đã chốt; migration, role limit/union/system role và policy view HTTP có proof ở gói role/phòng. Còn tích hợp quyền vào mời/duyệt/join mode và thu hồi tin/realtime | Tạo phòng, mời, duyệt, đổi cấu hình/quyền xem; quản lý role theo DEC-058 | Vg; Sáng đánh giá thiết kế |
| ACL-O2 | Snapshot/accessVersion/epoch, UI ACL và guard giữ lock tới caller commit đã có proof. Còn writer/history Messaging và Hub thật dùng guard, revoke tranh gửi/subscribe và đối soát reconnect; giữ DEC-057/098 | Danh sách phòng, lịch sử, gửi, thời gian thực | Vg, Sáng |
| ACL-O3 | Chuyển ngay DEC-076 có lock order/transaction; còn proof transfer/leave | Một owner, quyền chủ cũ/target, không xóa server v1 | Vg |
| ACL-O4 | Có schema/inbox/expiry/issuer theo DEC-074/087/097; còn review và concurrency proof | Cộng đồng riêng tư | Vg |
| ACL-O5 | Guard/epoch/outbox lưu DB đã có proof; registry/dispatcher/revoker đang là thiết kế, chưa có runtime. Cần đo chat ≤5 giây trên kết nối đang mở; media deadline DEC-099 cần proof SFU riêng | Thiết kế đồng bộ quyền, kiểm thử chất lượng | Vg, Sáng, Thái |

Các mục trên tiếp tục thuộc OQ-003/OQ-004/OQ-007. DM hai người không phụ
thuộc mô hình vai trò cộng đồng; có thể rà soát phần ACL-01 đến ACL-05
riêng trong gói bàn giao DM.

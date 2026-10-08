# SCDC — Permissions — Vai trò, ACL và kiểm tra quyền

Cập nhật: 2026-10-08. Đặc tả nghiệp vụ và tiêu chí kiểm chứng; tiến độ hiện tại tại [status.md](../status.md).

Sở hữu role, role assignment, ACL và policy tính quyền. Evaluator tính trên snapshot, kiểm thử độc lập; checker/guard đọc trạng thái hiện hành và giữ quyền đến commit. Điều kiện tài khoản/phiên do Identity cung cấp qua Contracts.

[Tổng quan Community](../README.md) · [Truy vết UC/COM/AC/TC](README.md#use-cases) · [Kế hoạch triển khai](../delivery/README.md#use-case-delivery).

Thiết kế kỹ thuật của phần này: [dữ liệu/API và giao dịch](../design/permissions.md#contracts).

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
| <a id="com-016"></a> COM-016 | Chủ sở hữu và người được cấp quyền được thay đổi danh sách người có quyền xem phòng. | DEC-039 |
| <a id="com-025"></a> COM-025 | Quyền quản lý và xem phòng cấp qua vai trò; phòng có ngoại lệ cho từng thành viên. | DEC-055 |
| <a id="com-026"></a> COM-026 | Chủ sở hữu luôn xem được mọi phòng trong cộng đồng của mình. | DEC-056 |
| <a id="com-027"></a> COM-027 | Quyền xem giữa các vai trò có từ chối thì từ chối thắng; ngoại lệ cá nhân áp dụng sau cùng, trừ quyền chủ sở hữu. | DEC-057, DEC-056 |
| <a id="com-028"></a> COM-028 | Chỉ chủ sở hữu được tạo/sửa vai trò và gán/thu hồi vai trò thành viên. | DEC-058 |
| <a id="com-037"></a> COM-037 | @everyone tự áp cho mọi thành viên, không xóa/không có quyền quản lý; tối đa 20 vai trò tự tạo, tên 1–64 UTF-16; quyền quản lý cộng dồn. | DEC-092, DEC-058 |
| <a id="com-042"></a> COM-042 | Sửa/xóa/đổi cấu hình xem phòng có sẵn cần quyền xem phòng và đúng quyền quản lý; tạo mới chỉ cần quyền tạo/quản lý phòng. Quyền quản lý không mở phòng bị ẩn. | DEC-098 |

<a id="permissions"></a>

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
| <a id="acl-15"></a> ACL-15 | Sửa/xóa tin trong phòng | Có quyền truy cập phòng và là tác giả tin | Không cấp quyền sửa/xóa tin của người khác trong đặc tả đợt đầu | DEC-034, [AC-COM-13](integration.md#ac-com-13) |
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

Thiết kế dữ liệu dùng ba trạng thái “kế thừa/cho phép/từ chối” cho cấu hình quyền xem. @everyone và giới hạn vai trò đã chốt DEC-092; cách lưu được mô tả ở [thiết kế chi tiết](../design/permissions.md#detailed-design). Quyền quản lý là hợp các quyền cho phép từ vai trò tự tạo; không có DENY quản lý hoặc ngoại lệ quản lý cá nhân trong v1. DEC-057 quyết định xung đột **quyền xem phòng**.

| Mặc định/va chạm | Ngoại lệ cá nhân | Kết quả xem (thành viên thường) |
|---|---|---|
| Mọi thành viên xem; không cấu hình vai trò | Kế thừa | Cho phép |
| Vai trò cho phép và vai trò từ chối | Kế thừa | Từ chối |
| Vai trò từ chối | Cho phép | Cho phép |
| Vai trò cho phép | Từ chối | Từ chối |
| Phòng giới hạn; không có cấu hình cho phép | Kế thừa | Từ chối |

<a id="revocation"></a>

## Quyền thay đổi khi đang sử dụng

| Tình huống | Kết quả phải kiểm chứng | Tiêu chí liên quan |
|---|---|---|
| Thu hồi quyền xem phòng đang mở | Lần đọc/gửi tiếp theo bị từ chối; không nhận thêm nội dung phòng qua kết nối cũ trong ≤5 giây cho chat theo DEC-083; media ≤5 giây theo DEC-099, cần proof riêng | [AC-COM-11](#ac-com-11), [AC-COM-13](integration.md#ac-com-13); DEC-083, OQ-007 |
| Thành viên thường rời cộng đồng | Không còn truy cập nội dung dành cho thành viên; giao diện rời phòng đang mở | [AC-COM-21](memberships.md#ac-com-21) |
| Mất quyền quản lý | Các thao tác quản lý tiếp theo bị kiểm tra lại phía máy chủ | [ACL-09](#acl-09) đến [ACL-12](#acl-12), [ACL-16](#acl-16) |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách thành viên và quyền trước khi tải lịch sử hoặc đăng ký nhận tin | AC-ACC-04, [AC-COM-13](integration.md#ac-com-13) |
| Thu hồi quyền một phòng | Không tự thu hồi quyền ở phòng khác hoặc DM độc lập | Suy ra từ phạm vi quyền phòng; cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã nhìn thấy hoặc tự sao chép.
Việc ngừng hiển thị dữ liệu đã tải trong giao diện, cách xử lý bộ nhớ đệm
và độ trễ thu hồi cần được cụ thể hóa trong thiết kế và OQ-007/OQ-011.


Quy tắc có thẩm quyền là DEC-055–058: giữa vai trò có DENY thì DENY thắng, cá nhân áp dụng cuối; chủ sở hữu được xem sau khi thỏa điều kiện nền. Cách mã hóa bằng bit mask 64-bit từng được đề xuất là lựa chọn biểu diễn dữ liệu; nó không được thay thứ tự ưu tiên đã chốt. Không suy “toàn quyền tuyệt đối” thành quyền sửa/xóa tin của người khác hoặc đọc DM.

<a id="use-cases"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](integration.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-20"></a>

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

**Kết quả sau cùng:** Danh mục custom role hợp lệ; management là hợp quyền allow, không có DENY quản lý/hierarchy vượt owner. Mất view do thay đổi role phải thu hồi theo [UC-COM-25](integration.md#uc-com-25).

<a id="uc-com-21"></a>

### UC-COM-21 — Gán hoặc thu hồi vai trò thành viên

**Module phụ trách:** Community / Permissions.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Chỉ owner hiện hành.

**Điều kiện trước:** Actor/target đủ điều kiện liên quan; target còn membership active của server. Owner đã tải membershipId/version và tập role hiện tại.

**Kích hoạt:** Owner chọn thành viên tại COM-S08 và lưu tập custom role.

**Luồng chính:**

1. Owner đọc roster và tập vai trò của target; roster chỉ có user summary và membership, không có email.
2. Owner thêm/bỏ role rồi gửi toàn bộ tập custom role cùng membershipId/expectedVersion.
3. Hệ thống kiểm tra owner, epoch/version và role cùng server rồi thay assignment nguyên tử, cập nhật quyền; sau commit, target tải lại quyền/subscription theo [UC-COM-25](integration.md#uc-com-25).

**Ngoại lệ:** Target đã leave/rejoin, role khác server/trùng hoặc system role không được gán. Actor có manage_channels/manage_channel_access vẫn không được thay assignment. Membership/version cũ không áp vào lần tham gia mới.

**Kết quả sau cùng:** Tập role của đúng epoch được lưu; @everyone tự áp theo membership active. Management permission không tự mở view của phòng bị ẩn.

<a id="uc-com-22"></a>

### UC-COM-22 — Xem và thay cấu hình quyền xem phòng

**Module phụ trách:** Community / Permissions.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có manage_channel_access và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; actor đã tải ACL snapshot/accessVersion. Member override chỉ dùng membership active đúng epoch.

**Kích hoạt:** Actor mở COM-S09 và lưu mặc định phòng, role override hoặc member override.

**Luồng chính:**

1. Hệ thống kiểm tra quyền đọc ACL, trả cấu hình và danh mục role/member phục vụ chọn ngoại lệ trong scope được phép.
2. Actor chỉnh snapshot và gửi expectedAccessVersion; hệ thống kiểm tra view/manage_channel_access cùng role/member/epoch hợp lệ.
3. Thay snapshot nguyên tử, tăng accessVersion và tính lại view theo default → role deny thắng → cá nhân sau cùng; owner theo [COM-026](#com-026). Sau commit, thu hồi scope mất view theo [UC-COM-25](integration.md#uc-com-25).

**Ngoại lệ:** Role/member duplicate/khác server/epoch cũ hoặc accessVersion cũ rollback toàn cấu hình. Management permission không cho đọc ACL của hidden channel. Actor tự mất view qua cấu hình hợp lệ vẫn nhận kết quả commit, nhưng request/subscription tiếp theo bị chặn. DENY owner không vượt quyền owner sau điều kiện nền.

**Kết quả sau cùng:** ACL nhất quán và áp dụng cho đọc/gửi/cập nhật; cấu hình ACL không cấp quyền quản lý role hoặc quyền sửa/xóa tin người khác.

<a id="ux"></a>

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

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](integration.md#ux).

<a id="acceptance"></a>

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


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [tiến độ](../status.md).

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](integration.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-acl-01"></a> TC-ACL-01 | Phòng mới chưa giới hạn; N đã xác minh | N xem và gửi được; thấy lịch sử cũ | [AC-COM-11](#ac-com-11), [AC-COM-15](channels.md#ac-com-15), [AC-COM-17](#ac-com-17) |
| <a id="tc-acl-02"></a> TC-ACL-02 | N có cả R-allow và R-deny, không có ngoại lệ cá nhân | Không xem, không gửi hoặc nhận nội dung phòng | DEC-057 |
| <a id="tc-acl-03"></a> TC-ACL-03 | Như [TC-ACL-02](#tc-acl-02) nhưng cá nhân N được cho phép | N xem/gửi được nếu thỏa điều kiện tài khoản/thành viên | DEC-057 |
| <a id="tc-acl-04"></a> TC-ACL-04 | Vai trò cho phép, ngoại lệ cá nhân từ chối | N không xem/gửi được | DEC-057 |
| <a id="tc-acl-05"></a> TC-ACL-05 | O mở phòng giới hạn, gồm cả cấu hình từ chối theo vai trò/cá nhân | O vẫn xem được trong cộng đồng của mình; không vượt điều kiện phiên/tài khoản | DEC-056 |
| <a id="tc-acl-06"></a> TC-ACL-06 | M được quyền tạo phòng nhưng thử tạo/sửa/gán/thu hồi vai trò | Máy chủ từ chối; O thực hiện được | DEC-058 |
| <a id="tc-acl-07"></a> TC-ACL-07 | N đang mở phòng; O thu hồi quyền hoặc N rời cộng đồng | API từ chối; ngừng nhận nội dung phòng theo ngưỡng thu hồi; DM độc lập vẫn hoạt động | [AC-COM-11](#ac-com-11), [AC-COM-21](memberships.md#ac-com-21) |
| <a id="tc-acl-08"></a> TC-ACL-08 | Chạy 18 fixture nền/owner/role/cá nhân/quyền quản lý/kind | Khớp [community-permissions.json](../../../fixtures/community-permissions.json); không coi fixture đã khớp là API đã đạt | [AC-COM-06](#ac-com-06), [AC-COM-17](#ac-com-17), [AC-COM-36](#ac-com-36), [AC-COM-42](#ac-com-42) |
| <a id="tc-acl-09"></a> TC-ACL-09 | O tạo hai custom role tranh slot thứ 20; gọi sửa/xóa/gán @everyone | Một role mới, role còn lại ROLE_LIMIT_REACHED; system role bị bảo vệ | [AC-COM-36](#ac-com-36) |
| <a id="tc-acl-10"></a> TC-ACL-10 | PUT ACL với role/member khác server, duplicate, epoch cũ; actor làm mất view của mình | Cấu hình sai rollback toàn bộ; cấu hình hợp lệ commit, request/subscription tiếp theo bị chặn | [AC-COM-16](#ac-com-16), [AC-COM-42](#ac-com-42) |
| <a id="tc-acl-11"></a> TC-ACL-11 | Xóa role deny hoặc allow khi user đang xem phòng | Tính lại kết quả thực, không suy mọi xóa là deny; view mất thì thu hồi ≤5 giây | DEC-057/083 |
| <a id="tc-acl-12"></a> TC-ACL-12 | M biết ID phòng bị ẩn và gọi GET/PATCH/DELETE/GET-ACL/PUT-ACL | 404 không metadata; đúng management permission vẫn cần view; owner không vượt điều kiện nền | [AC-COM-42](#ac-com-42) |


Các race guard/outbox còn được kiểm chứng tại [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](integration.md#tc-com-25), [TC-COM-26](integration.md#tc-com-26); Channels/Messaging phải chạy ca tích hợp bên cạnh fixture quyền.

<a id="gaps"></a>

## Việc còn lại

Các mục dưới đây giữ yêu cầu kiểm chứng còn lại, không phải bảng tiến độ. Phần đã triển khai/migration/test trên feature được quản lý tại [status](../status.md), [đối chiếu test](../delivery/verification.md) và hồ sơ nghiệm thu; migration mục tiêu theo [thiết kế](../design/integration.md#schema-migration), retention theo [vòng đời dữ liệu](../../../data-lifecycle.md).

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Cấp quyền | Phần chưa chứng minh: writer lịch sử/tin dùng guard, subscription và deadline thu hồi; approval/lời mời/transfer/leave/Media có proof theo gói riêng. Không suy kết quả policy/HTTP thành toàn bộ UC đạt. | OQ-004, OQ-007, OQ-008 |

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

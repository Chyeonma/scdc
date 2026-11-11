# SCDC — Vai trò và quyền

Trong server, chủ server tạo vai trò và gán cho thành viên. Vai trò cấp quyền quản lý (tạo phòng, mời, duyệt…) và quyết định ai xem được phòng nào. Mỗi phòng còn có thể đặt ngoại lệ cho từng người.

Phần Permissions của Community sở hữu vai trò, gán vai trò, thuật toán quyền xem, cấu hình ACL của phòng, và các thành phần tính, kiểm tra, guard quyền.

<a id="implementation"></a>

**Trạng thái** (nền `900e226`, 2026-10-13):

| Chức năng | Trạng thái | Bằng chứng |
|---|---|---|
| Vai trò và gán vai trò (một phần UC-COM-20, 21) | Đã có backend, giao diện, guard quản lý; đã kiểm thử | [Vai trò](../../records/verification/community/roles.md) |
| Quyền xem qua HTTP và ACL (một phần UC-COM-17, 22) | Đã kiểm thử danh sách, chi tiết, thông tin phòng, ACL, epoch, CAS | [Phòng chữ và ACL](../../records/verification/community/channels-access.md) |
| Guard cho phòng | Đã kiểm thử: giữ khóa tới khi bên gọi commit, tranh chấp với đổi vai trò và ACL. Chưa có ghi tin hay đăng ký realtime nào dùng. DM có guard riêng | [Phòng chữ và ACL](../../records/verification/community/channels-access.md) |
| Ngắt chat trong 5 giây khi mất quyền (UC-COM-25) | Chưa có | [Tin phòng](../messaging/channel-messaging.md) |

Guard HTTP và outbox chưa chứng minh được thời hạn thu hồi realtime. Media cần bằng chứng riêng.

Điều kiện chung: [cơ chế Community](mechanics.md#use-case-conditions). Cách chạy: [hướng dẫn](../../guides/development.md#community).

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-016"></a> COM-016 | Chủ server và người có quyền được thay đổi ai xem được phòng. |
| <a id="com-025"></a> COM-025 | Quyền quản lý và quyền xem phòng cấp qua vai trò; mỗi phòng có thể đặt ngoại lệ cho từng thành viên. |
| <a id="com-026"></a> COM-026 | Chủ server luôn xem được mọi phòng trong server của mình. |
| <a id="com-027"></a> COM-027 | Giữa các vai trò, nếu có "từ chối" thì từ chối thắng. Ngoại lệ cá nhân áp dụng sau cùng (trừ chủ server). |
| <a id="com-028"></a> COM-028 | Chỉ chủ server tạo, sửa vai trò và gán, thu hồi vai trò. |
| <a id="com-037"></a> COM-037 | @everyone tự áp cho mọi thành viên, không xóa được, không có quyền quản lý. Tối đa 20 vai trò tự tạo; tên 1–64 UTF-16. Quyền quản lý từ nhiều vai trò được cộng gộp. |
| <a id="com-042"></a> COM-042 | Sửa, xóa, đổi quyền xem một phòng có sẵn cần **cả** quyền xem phòng đó **và** quyền quản lý. Tạo phòng mới chỉ cần quyền quản lý. Quyền quản lý không mở được phòng bị ẩn. |

<a id="permissions"></a>

<a id="ma-trận-quyền"></a>

## Ma trận quyền

"Người có quyền" nghĩa là có đúng quyền của thao tác đó; quyền tạo lời mời không cho quyền duyệt hay quản lý vai trò. Người thực hiện, phiên và tư cách thành viên luôn được kiểm tra ở server. Quyền tài khoản và DM (ACL-01 đến 05) độc lập với vai trò trong server.

| Mã | Thao tác | Được phép khi | Giới hạn |
|---|---|---|---|
| <a id="acl-06"></a> ACL-06 | Thấy server trong tìm kiếm | Server công khai | Server riêng tư không hiện |
| <a id="acl-07"></a> ACL-07 | Tham gia qua tìm kiếm | Server công khai | Vào ngay hay chờ duyệt theo cấu hình; chưa duyệt thì chưa là thành viên |
| <a id="acl-08"></a> ACL-08 | Tham gia bằng link mời | Link hợp lệ, còn hạn | Bỏ qua chờ duyệt; link hết hạn hay đã thu hồi bị từ chối |
| <a id="acl-09"></a> ACL-09 | Tạo, thu hồi lời mời | Chủ server hoặc vai trò có quyền mời | Hạn và lượt theo [COM-032](invitations.md#com-032); kiểm tra thu hồi và giới hạn trong transaction tham gia |
| <a id="acl-10"></a> ACL-10 | Duyệt yêu cầu tham gia | Chủ server hoặc người có quyền duyệt | Được từ chối; người gửi tự hủy được (DEC-073) |
| <a id="acl-11"></a> ACL-11 | Đổi cách tham gia | Chủ server hoặc người có quyền | Link hợp lệ vẫn cho vào ngay |
| <a id="acl-12"></a> ACL-12 | Tạo, sửa, xóa phòng | Chủ server hoặc người có quyền quản lý phòng | Sửa, xóa cần quyền xem phòng (DEC-098). Phòng mới mặc định ai cũng xem. Xóa chặn HTTP, realtime, media; không khôi phục |
| <a id="acl-13"></a> ACL-13 | Xem phòng và lịch sử | Là thành viên và có quyền xem; chủ server luôn xem được | Thành viên mới xem được tin cũ; thứ tự tính ở [quyền xem](#view-permissions) |
| <a id="acl-14"></a> ACL-14 | Gửi tin trong phòng | Có quyền xem, phiên hợp lệ, email đã xác minh | Đợt đầu không có quyền chỉ đọc |
| <a id="acl-15"></a> ACL-15 | Sửa, xóa tin trong phòng | Có quyền vào phòng và là tác giả | Không ai sửa, xóa được tin người khác ([AC-COM-13](../messaging/channel-messaging.md#ac-com-13)) |
| <a id="acl-16"></a> ACL-16 | Đổi quyền xem phòng | Chủ server hoặc vai trò có quyền quản lý quyền xem | Cần quyền xem phòng đó (DEC-098); không có nghĩa được quản lý vai trò |
| <a id="acl-17"></a> ACL-17 | Tự rời server | Thành viên thường | Chủ server phải chuyển quyền chủ trước (DEC-076) |
| <a id="acl-18"></a> ACL-18 | Mời đích danh vào server riêng tư | Chủ server hoặc người có quyền mời gửi; đúng người nhận bấm nhận | Chưa nhận thì chưa là thành viên, chưa có quyền phòng |
| <a id="acl-19"></a> ACL-19 | Tạo, sửa vai trò; gán, thu hồi vai trò | Chỉ chủ server | Đợt đầu không ủy quyền quản lý vai trò |
| <a id="acl-20"></a> ACL-20 | Tạo, sửa server | Tài khoản đã xác minh tạo được; chỉ chủ server sửa tên, mô tả, công khai | Đổi cách tham gia theo [ACL-11](#acl-11) |

Chủ server **không** được đọc DM, sửa tin người khác hay truy cập nội dung ngoài server của mình. Chủ server luôn xem được mọi phòng của mình (DEC-056) nhưng vẫn phải có tài khoản và phiên hợp lệ.

<a id="view-permissions"></a>

<a id="thứ-tự-tính-quyền-xem-đã-chốt"></a>

## Cách tính quyền xem phòng

1. **Điều kiện nền:** phiên hợp lệ, tài khoản đủ điều kiện, server và phòng còn tồn tại, người dùng còn là thành viên. Không đủ thì từ chối ngay.
2. **Chủ server:** cho xem (DEC-056).
3. **Mặc định của phòng:** phòng mới cho mọi thành viên xem, trừ khi đặt giới hạn (DEC-033).
4. **Theo vai trò:** trong các vai trò người dùng có, nếu có "từ chối" thì từ chối; không có từ chối mà có "cho phép" thì cho phép; không có cấu hình thì giữ mặc định (DEC-055, 057).
5. **Ngoại lệ cá nhân (sau cùng):** cho phép hay từ chối thay cho kết quả bước 4; không đặt thì giữ bước 4 (DEC-057).
6. Có quyền xem và đã xác minh email thì gửi được tin; đợt đầu không có quyền chỉ đọc riêng (DEC-040, 041).

Mỗi cấu hình có ba giá trị: kế thừa, cho phép, từ chối. Quyền **quản lý** là hợp các quyền "cho phép" của các vai trò tự tạo; v1 không có "từ chối" hay ngoại lệ cá nhân cho quyền quản lý. DEC-057 chỉ áp cho **quyền xem**.

| Mặc định hoặc vai trò | Ngoại lệ cá nhân | Kết quả (thành viên thường) |
|---|---|---|
| Mọi thành viên xem; không cấu hình vai trò | Kế thừa | Cho phép |
| Có vai trò cho phép và vai trò từ chối | Kế thừa | Từ chối |
| Vai trò từ chối | Cho phép | Cho phép |
| Vai trò cho phép | Từ chối | Từ chối |
| Phòng giới hạn; không có cho phép | Kế thừa | Từ chối |

Cách lưu (bit mask hay bảng) chỉ là biểu diễn dữ liệu, không thay thứ tự trên. "Toàn quyền" không bao gồm sửa, xóa tin người khác hay đọc DM.

<a id="revocation"></a>

<a id="quyền-thay-đổi-khi-đang-sử-dụng"></a>

### Khi quyền thay đổi lúc đang dùng

| Tình huống | Phải kiểm thử | Liên quan |
|---|---|---|
| Bị thu hồi quyền xem phòng đang mở | Đọc, gửi tiếp theo bị từ chối; kết nối cũ ngừng nhận nội dung trong 5 giây (chat, DEC-083); media trong 5 giây (DEC-099, cần bằng chứng riêng) | [AC-COM-11](#ac-com-11), [AC-COM-13](../messaging/channel-messaging.md#ac-com-13), OQ-007 |
| Thành viên rời server | Không còn truy cập nội dung thành viên; giao diện đóng phòng đang mở | [AC-COM-21](memberships.md#ac-com-21) |
| Mất quyền quản lý | Thao tác quản lý tiếp theo bị server kiểm tra lại | [ACL-09](#acl-09) đến [ACL-12](#acl-12), [ACL-16](#acl-16) |
| Mất kết nối rồi mở lại | Kiểm tra lại phiên, tư cách, quyền trước khi tải lịch sử hay đăng ký | AC-ACC-04, [AC-COM-13](../messaging/channel-messaging.md#ac-com-13) |
| Bị thu hồi quyền một phòng | Không ảnh hưởng phòng khác hay DM | Cần kiểm thử tích hợp |

Không hứa xóa được nội dung người dùng đã thấy hay đã tự sao chép. Cách xóa dữ liệu đã tải trên giao diện, bộ nhớ đệm và độ trễ thu hồi cần cụ thể hóa (OQ-007, OQ-011).

<a id="use-cases"></a>

<a id="use-case"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung: [quy ước tích hợp](mechanics.md#use-case-conditions). Quy tắc, AC, TC: [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-20"></a>

<a id="uc-com-20--tạo-sửa-và-xóa-vai-trò-tự-tạo"></a>

### UC-COM-20 — Tạo, sửa, xóa vai trò

**Ai làm:** chỉ chủ server hiện tại.

**Điều kiện trước:** vai trò cần sửa hay xóa thuộc server, là vai trò tự tạo, đã tải version.

**Khi nào:** mở COM-S08 để quản lý vai trò.

**Các bước:**

1. Xem danh sách; nhập tên và chọn quyền quản lý cho vai trò ([COM-037](#com-037)).
2. Kiểm tra chủ server, dữ liệu, giới hạn. Tạo mới dùng khóa chống trùng; sửa dùng `expectedVersion`.
3. Xóa: bỏ vai trò cùng các gán vai trò và cấu hình ACL theo vai trò đó, cập nhật version quyền trong cùng transaction. Sau commit, tính lại quyền và báo tải lại.

**Ngoại lệ:**

- Tranh nhau tạo vai trò thứ 20: chỉ một thắng.
- Tên trùng, quyền sai hay trùng, version cũ, mất quyền chủ: từ chối.
- @everyone không sửa, xóa, gán tay được, không cấp quyền quản lý được.
- Xóa vai trò "từ chối" có thể mở quyền xem; xóa vai trò "cho phép" có thể làm mất quyền xem. Phải tính lại kết quả thật.

**Kết quả:** danh sách vai trò hợp lệ. Quyền quản lý là hợp các "cho phép", không có "từ chối" hay thứ bậc vượt chủ server. Ai mất quyền xem do thay đổi vai trò thì bị thu hồi theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

<a id="uc-com-21"></a>

<a id="uc-com-21--gán-hoặc-thu-hồi-vai-trò-thành-viên"></a>

### UC-COM-21 — Gán, thu hồi vai trò

**Ai làm:** chỉ chủ server hiện tại.

**Điều kiện trước:** người được gán là thành viên đang hoạt động; chủ server đã tải `membershipId`, version và vai trò hiện có của họ.

**Khi nào:** chọn thành viên ở COM-S08 và lưu danh sách vai trò.

**Các bước:**

1. Xem danh sách thành viên và vai trò của người được chọn (chỉ có thông tin cơ bản, không có email).
2. Thêm, bỏ vai trò; gửi **toàn bộ** danh sách vai trò tự tạo kèm `membershipId` và `expectedVersion`.
3. Kiểm tra chủ server, epoch, version, vai trò thuộc cùng server; thay danh sách cùng lúc, cập nhật quyền. Sau commit, người đó tải lại quyền và đăng ký realtime ([UC-COM-25](../messaging/channel-messaging.md#uc-com-25)).

**Ngoại lệ:** người đó đã rời rồi vào lại, vai trò khác server hay trùng, vai trò hệ thống: không gán được. Người có `manage_channels` hay `manage_channel_access` vẫn không gán được. Version cũ không áp vào lần tham gia mới.

**Kết quả:** danh sách vai trò của đúng epoch được lưu; @everyone tự áp. Quyền quản lý không mở được phòng bị ẩn.

<a id="uc-com-22"></a>

<a id="uc-com-22--xem-và-thay-cấu-hình-quyền-xem-phòng"></a>

### UC-COM-22 — Xem và đổi quyền xem phòng

**Ai làm:** chủ server, hoặc thành viên có `manage_channel_access` **và** quyền xem phòng đó.

**Điều kiện trước:** đã tải cấu hình ACL và `accessVersion`. Ngoại lệ cá nhân chỉ dùng tư cách đang hoạt động, đúng epoch.

**Khi nào:** mở COM-S09, lưu mặc định của phòng, cấu hình theo vai trò hay ngoại lệ cá nhân.

**Các bước:**

1. Kiểm tra quyền đọc ACL; trả cấu hình và danh sách vai trò, thành viên để chọn.
2. Chỉnh cấu hình, gửi `expectedAccessVersion`. Kiểm tra quyền xem và `manage_channel_access`, vai trò và thành viên hợp lệ.
3. Thay cấu hình cùng lúc, tăng `accessVersion`, tính lại quyền xem (mặc định → vai trò, từ chối thắng → cá nhân sau cùng; chủ server theo [COM-026](#com-026)). Sau commit, thu hồi những ai mất quyền xem ([UC-COM-25](../messaging/channel-messaging.md#uc-com-25)).

**Ngoại lệ:**

- Vai trò hay thành viên trùng, khác server, epoch cũ, hoặc `accessVersion` cũ: rollback toàn bộ.
- Quyền quản lý không cho đọc ACL của phòng bị ẩn.
- Tự làm mình mất quyền xem bằng cấu hình hợp lệ: vẫn commit, nhưng request và đăng ký tiếp theo bị chặn.
- "Từ chối" chủ server không có tác dụng (khi đủ điều kiện nền).

**Kết quả:** ACL nhất quán, áp cho đọc, gửi, cập nhật. Cấu hình ACL không cấp quyền quản lý vai trò hay sửa, xóa tin người khác.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện

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

| Màn hình | Ai dùng | Nội dung |
|---|---|---|
| COM-S08 Vai trò | Chỉ chủ server | Danh sách vai trò, quyền quản lý, gán và thu hồi; người khác gọi API cũng bị từ chối |
| COM-S09 Quyền xem phòng | Chủ server, người có quyền quản lý quyền xem | Mặc định, theo vai trò, ngoại lệ cá nhân; có version để không ghi đè cấu hình mới hơn |

Bố cục COM-S09 là đề xuất. Cách tránh người quản lý vô tình tự mất quyền, và phản hồi sau khi lưu, cần thử trên prototype. Màn hình hẹp: [giao diện chung](mechanics.md#ux).

<a id="acl-editor-ui"></a>

### Trình sửa ACL

- Danh sách vai trò và thành viên chỉ tải khi người dùng có quyền xem phòng và quyền sửa ACL. Danh sách thành viên phân trang; ngoại lệ hiện có giữ nguyên ID và epoch kể cả khi người đó không nằm trong trang đang xem.
- Gửi **toàn bộ** cấu hình (cho phép, từ chối, kế thừa); không tự thay epoch cũ bằng epoch mới.
- Bản nháp giữ `accessVersion` và epoch đã đọc. Xung đột hay timeout: GET đối chiếu trước khi lưu lại; không tự PUT lại.
- Đổi người dùng, server, phòng: hủy đọc cũ, bỏ dữ liệu quản lý. 401, 403, 404: đóng ACL cũ.
- Quyền phía client chỉ để ẩn hiện giao diện; server luôn kiểm tra lại dưới khóa.

<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="permission-contracts"></a>

<a id="permission-thiết-kế-dữ-liệuapi"></a>

Quy ước chung: [Community](mechanics.md#contracts). Schema: [OpenAPI](../../contracts/community.openapi.json). Transaction: [transaction chung](mechanics.md#transactions).

<a id="permission-detailed-design"></a>

<a id="permission-vai-trò-và-cấu-hình-quyền"></a>

### Các quyền quản lý

| Mã | Cho phép | Không cho phép |
|---|---|---|
| `manage_channels` | Tạo, sửa, xóa phòng | Xem tin của mọi phòng, đổi ACL, quản lý vai trò |
| `manage_invites` | Tạo, thu hồi link; gửi, hủy mời đích danh | Duyệt yêu cầu, gán vai trò |
| `review_join_requests` | Xem yêu cầu đang chờ, duyệt, từ chối | Tạo lời mời, quản lý vai trò |
| `manage_join_mode` | Đổi vào ngay hay chờ duyệt | Đổi công khai, tên, mô tả |
| `manage_channel_access` | Đọc, sửa quyền xem phòng; chọn thành viên cho ngoại lệ | Đọc tin của phòng bị giới hạn, quản lý vai trò |

- Chủ server có mọi quyền trên, cộng các thao tác riêng [ACL-19](#acl-19), [ACL-20](#acl-20) và chuyển quyền chủ; không sửa, xóa được tin người khác.
- **@everyone:** vai trò hệ thống duy nhất của server, tự áp cho mọi thành viên (không cần ghi `member_roles` cho từng người). Không đổi tên, xóa, gán tay, hay cấp quyền quản lý.
- **Vai trò tự tạo:** có tập quyền "cho phép"; quyền thực tế là hợp các tập này; không phụ thuộc thứ tự vai trò; không có thứ bậc vượt chủ server. Giới hạn 20 (không tính @everyone), kiểm tra dưới khóa server.

<a id="acl-api"></a>

### API vai trò và ACL (đã làm)

| Route | Quyền và kết quả |
|---|---|
| `GET /servers/{id}/roles` | Chủ server hoặc `manage_channel_access` đọc để chọn; chỉ chủ server thay đổi được |
| `POST /servers/{id}/roles` | Chủ server; `{clientOperationId, name, permissions}`; 201 |
| `PATCH /servers/{id}/roles/{roleId}` | Chủ server; `{name?, permissions?, expectedVersion}`; 200; vai trò hệ thống không sửa được |
| `DELETE /servers/{id}/roles/{roleId}?expectedVersion=...` | Chủ server; xóa vai trò cùng gán và cấu hình ACL liên quan; 204 |
| `GET /servers/{id}/members` | Chủ server hoặc `manage_channel_access`; trang thông tin cơ bản và tư cách, không có email |
| `GET /servers/{id}/members/{userId}/roles` | Chủ server; vai trò tự tạo, `membershipId`, version |
| `PUT /servers/{id}/members/{userId}/roles` | Chủ server; `{membershipId, expectedVersion, roleIds}` thay toàn bộ danh sách |
| `GET /servers/{id}/channels/{channelId}/access` | Chủ server hoặc `manage_channel_access`, **và** còn quyền xem phòng; trả cấu hình và `accessVersion`, không trả tin |
| `PUT /servers/{id}/channels/{channelId}/access` | Cùng điều kiện; thay cấu hình với `expectedAccessVersion`; cùng lúc |

**Cấu hình ACL:**

```text
defaultView: allow | deny
roleOverrides:   [{ roleId, effect: allow | deny }]
memberOverrides: [{ userId, membershipId, effect: allow | deny }]
```

- Không có mục nghĩa là kế thừa. Không dùng bit mask trên API.
- Tối đa 21 cấu hình theo vai trò (gồm @everyone).
- Vai trò hay người dùng trùng, hoặc thuộc server khác: 400. Ngoại lệ cá nhân chỉ nhận tư cách đang hoạt động, đúng epoch.

**Version và sự kiện:**

- PUT ACL tăng `accessVersion` và version của phòng, `accessVersion` của server, ghi outbox cùng transaction.
- Xóa vai trò hay dọn dữ liệu khi tham gia lại cũng tăng `accessVersion` của phòng bị mất cấu hình, để bản cũ không ghi đè được.
- Không đổi gì thì không tăng version, không ghi sự kiện.
- Tự làm mất quyền xem: nhận cấu hình đã commit; GET tiếp theo trả 404.
- Xóa vai trò có thể mở hoặc đóng quyền xem; phải tính lại.
- Rời server xóa gán vai trò và ngoại lệ của epoch hiện tại; tham gia lại chỉ có @everyone (DEC-087). Chuyển chủ không tự gán vai trò quản lý cho chủ cũ.

<a id="permission-implementation-boundary"></a>

<a id="permission-evaluator-quản-lý-quyền-và-guard"></a>

### Bộ tính quyền, quản lý quyền, guard

Phân chia (đã thống nhất 2026-10-06):

- **Domain:** danh mục quyền, model vai trò và ACL, bộ tính quyền trên bản chụp dữ liệu; không gọi service hay DB.
- **Application:** các ca sử dụng quản lý vai trò, gán, ACL; phối hợp transaction, version, epoch.
- **Infrastructure:** đọc bản chụp hiện tại; cài đặt bộ kiểm tra và guard. Guard giữ khóa dòng ([transaction](mechanics.md#transactions)), không thay bằng kết quả đúng sai hay cache.

Đổi vai trò, gán, ACL cập nhật `accessVersion` và outbox cùng transaction. Gói vai trò đã có bộ tính quyền, guard quản lý, ghi vai trò và gán ([hồ sơ](../../records/verification/community/roles.md)). Gói phòng đọc quyền hiện tại và giữ khóa server, phòng trong transaction của bên gọi qua `IChannelAccessGuard` ([phòng](channels.md)); đã kiểm thử HTTP và tranh chấp, nhưng chưa có ghi tin, đăng ký realtime hay thu hồi realtime dùng.

Schema đích: [COM-SQL-01 đến 10](mechanics.md#schema-migration).

<a id="role-dữ-liệu-và-migration"></a>

### Dữ liệu và migration 003

- Thêm `roles.name_key` (cắt khoảng trắng, NFC, `ToLowerInvariant`, giống tìm kiếm), unique theo server với collation C. Tên tối đa 64 UTF-16. `role.version` int32 với một trigger tăng version và thời gian. Giữ cột `normalized_name` cũ nhưng bỏ unique theo locale cũ. @everyone được ghi khóa rõ ràng khi tạo server.
- Gán vai trò và ngoại lệ cá nhân thêm `membership_id`, khóa ngoại `(server_id, user_id, membership_id)`. Tham gia lần đầu và tham gia lại dọn quyền cũ trước khi đổi epoch. Không suy epoch từ người dùng khi xử lý request cũ.
- **Chạy:** cần ledger và checksum 001, 002; advisory lock và table lock. Điền khóa theo lô 500; kiểm tra tên, trùng, giới hạn 20, danh mục quyền, chỉ ngoại lệ `channel_view`. Dữ liệu không hợp lệ thì báo cần sửa có review và rollback; không tự đổi tên, quyền hay epoch.
- Database mới đã gồm 001 đến 004. Nâng cấp từ 001, 002: dừng writer cũ → chạy 003 và các migration còn thiếu → triển khai writer mới. Không chạy file có DROP SCHEMA trên database cần giữ.
- **Chống trùng `create_role`:** phạm vi là `serverId`; fingerprint gồm tên sau khi cắt và mask quyền một byte. Mảng quyền khác thứ tự cho cùng fingerprint; quyền trùng hay lạ bị chặn. Gửi lại kiểm tra chủ server và vai trò còn tồn tại; vai trò đã xóa không tạo lại bằng khóa cũ.

<a id="role-api-và-giao-dịch"></a>

### API và transaction vai trò

- **Phân trang:** vai trò và thành viên mặc định 20, tối đa 50, theo UUID tăng dần; cursor riêng cho từng danh sách, gắn người dùng, server, giới hạn, vị trí, hạn 24 giờ.
- **Danh sách vai trò gán:** chỉ vai trò tự tạo cùng server, tối đa 20, không trùng; PUT thay toàn bộ; không có @everyone.
- Version gửi dạng chuỗi số dương; lưu int32; tràn số thì dừng trước khi ghi.
- **Thứ tự khóa:** tài khoản người thực hiện → server (`FOR UPDATE` khi ghi, `FOR SHARE` khi đọc) → tư cách thành viên, vai trò. Khóa server giữ ổn định quyền chủ, quyền, giới hạn, CAS tới commit; kiểm tra lease lại trước commit.
- Server riêng tư, không phải thành viên, không hoạt động: 404. Thành viên thiếu quyền: 403.
- Đọc danh sách vai trò, thành viên: chủ server hoặc `manage_channel_access`. Ghi: chỉ chủ server.
- Danh sách thành viên lấy thông tin qua hợp đồng Identity; tài khoản không còn hoạt động thì `user` là null. Không trả email; không cần phiên của người được gán.
- **Sự kiện:** tạo, sửa, xóa, gán tăng `accessVersion` của server và ghi `Community.AccessChanged.v1` cùng transaction (chỉ phạm vi, server, `accessVersion`, lý do, ID; không có tên hay danh sách). **Chưa có worker hay Hub nào xử lý sự kiện này**; Hub DM cũng không.
- Gán vai trò tăng version tư cách; xóa vai trò xóa gán và cấu hình liên quan và tăng version tư cách bị ảnh hưởng. Sửa vai trò tăng version đúng một lần. Không đổi gì thì trả hiện trạng, không tăng, không ghi sự kiện.
- DELETE dùng `expectedVersion`; vai trò không còn: 404. Lời mời cũ còn giữ vai trò tự tạo: `409 ROLE_IN_USE`, không tự sửa lời mời.

<a id="role-webclient"></a>

### Giao diện vai trò

- Chủ server có lối vào quản lý vai trò và thành viên.
- Tạo vai trò: lưu thao tác và nội dung vào `sessionStorage` trước khi POST; lỗi không rõ thì giữ khóa để gửi lại thủ công, kể cả sau khi tải lại trang.
- Sửa, xóa, gán: dùng version và epoch đã tải, `retry:false`. Mất phản hồi hay xung đột: GET hiện trạng, người dùng lưu lại. Đổi người dùng hay server: bỏ phản hồi cũ.
- Danh sách vai trò tải đủ (tối đa 21); danh sách thành viên phân trang. @everyone chỉ hiện trạng thái được bảo vệ.
- Mất quyền thì bỏ dữ liệu quản lý cũ. Giao diện không phải nguồn quyền.
- Màn hình hẹp, bàn phím, trạng thái tải, trống, lỗi đã kiểm tra bằng Chromium với API thật.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-06"></a> AC-COM-06 | Thành viên không có quyền xem phòng X đăng nhập | Phòng X không có trong danh sách |
| <a id="ac-com-11"></a> AC-COM-11 | Tạo phòng không giới hạn; sau đó giới hạn một thành viên | Ban đầu ai cũng thấy; sau đó người bị giới hạn không thấy, không vào được |
| <a id="ac-com-16"></a> AC-COM-16 | Chủ server, người có quyền, thành viên thường thử đổi quyền xem phòng | Hai người đầu làm được; thành viên thường bị server từ chối |
| <a id="ac-com-17"></a> AC-COM-17 | Người có quyền xem gửi tin; người không có quyền thử gửi | Người đầu gửi được; người sau bị từ chối |
| <a id="ac-com-25"></a> AC-COM-25 | Thành viên có hai vai trò: một cho phép, một từ chối xem cùng phòng; không có ngoại lệ | Không thấy, đọc, gửi, nhận cập nhật phòng đó |
| <a id="ac-com-26"></a> AC-COM-26 | Như [AC-COM-25](#ac-com-25), thêm ngoại lệ cá nhân cho phép; rồi đổi thành từ chối | Cho phép cá nhân thắng từ chối vai trò; từ chối cá nhân thì không xem, gửi, nhận được |
| <a id="ac-com-27"></a> AC-COM-27 | Chủ server mở phòng giới hạn có cấu hình từ chối theo vai trò và cá nhân | Vẫn xem được (nếu tài khoản và phiên hợp lệ) |
| <a id="ac-com-28"></a> AC-COM-28 | Thành viên có quyền tạo phòng thử sửa hay gán vai trò; chủ server làm cùng thao tác | Thành viên bị từ chối; chủ server làm được |
| <a id="ac-com-36"></a> AC-COM-36 | Tạo vai trò thứ 20 và 21; sửa, xóa @everyone; hai vai trò cấp quyền quản lý khác nhau | Tối đa 20; @everyone không sửa, xóa, cấp quyền quản lý được; quyền quản lý cộng gộp |
| <a id="ac-com-42"></a> AC-COM-42 | Người có `manage_channels`, `manage_channel_access` nhưng không có quyền xem phòng | Không thấy thông tin, phòng, tin; không sửa, xóa, đổi ACL phòng đó được. Chủ server vẫn quản lý được; tạo phòng mới chỉ cần quyền quản lý |

Tiêu chí liên quan nhiều phần: [bảng truy vết](traceability.md#use-case-coverage).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dữ liệu và cách ghi bằng chứng: [dùng chung](../messaging/channel-messaging.md#evidence). Mỗi ca cần test cho từng endpoint và kiểm tra quyền qua API.

| Mã ca | Tình huống | Kết quả | Cho |
|---|---|---|---|
| <a id="tc-acl-01"></a> TC-ACL-01 | Phòng mới không giới hạn; N đã xác minh | N xem và gửi được; thấy lịch sử cũ | [AC-COM-11](#ac-com-11), [AC-COM-15](channels.md#ac-com-15), [AC-COM-17](#ac-com-17) |
| <a id="tc-acl-02"></a> TC-ACL-02 | N có cả vai trò cho phép và từ chối, không có ngoại lệ | Không xem, gửi, nhận được | DEC-057 |
| <a id="tc-acl-03"></a> TC-ACL-03 | Như [TC-ACL-02](#tc-acl-02), thêm ngoại lệ cá nhân cho phép | N xem, gửi được (nếu đủ điều kiện) | DEC-057 |
| <a id="tc-acl-04"></a> TC-ACL-04 | Vai trò cho phép, ngoại lệ cá nhân từ chối | N không xem, gửi được | DEC-057 |
| <a id="tc-acl-05"></a> TC-ACL-05 | O mở phòng giới hạn có cấu hình từ chối theo vai trò và cá nhân | O vẫn xem được; không vượt điều kiện phiên và tài khoản | DEC-056 |
| <a id="tc-acl-06"></a> TC-ACL-06 | M có quyền tạo phòng thử tạo, sửa, gán, thu hồi vai trò | Server từ chối; O làm được | DEC-058 |
| <a id="tc-acl-07"></a> TC-ACL-07 | N đang mở phòng; O thu hồi quyền hoặc N rời server | API từ chối; ngừng nhận nội dung đúng hạn; DM vẫn chạy | [AC-COM-11](#ac-com-11), [AC-COM-21](memberships.md#ac-com-21) |
| <a id="tc-acl-08"></a> TC-ACL-08 | Chạy 18 trường hợp trong fixture (nền, chủ, vai trò, cá nhân, quản lý, loại phòng) | Khớp [community-permissions.json](../../fixtures/community-permissions.json); fixture khớp chưa có nghĩa API đạt | [AC-COM-06](#ac-com-06), [17](#ac-com-17), [36](#ac-com-36), [42](#ac-com-42) |
| <a id="tc-acl-09"></a> TC-ACL-09 | O tạo hai vai trò cùng lúc tranh chỗ thứ 20; thử sửa, xóa, gán @everyone | Một vai trò mới, cái kia `ROLE_LIMIT_REACHED`; @everyone được bảo vệ | [AC-COM-36](#ac-com-36) |
| <a id="tc-acl-10"></a> TC-ACL-10 | PUT ACL với vai trò hay thành viên khác server, trùng, epoch cũ; tự làm mất quyền xem | Cấu hình sai thì rollback hết; cấu hình hợp lệ thì commit, request và đăng ký sau bị chặn | [AC-COM-16](#ac-com-16), [42](#ac-com-42) |
| <a id="tc-acl-11"></a> TC-ACL-11 | Xóa vai trò từ chối hoặc cho phép khi người dùng đang xem phòng | Tính lại kết quả thật; mất quyền xem thì ngắt trong 5 giây | DEC-057, 083 |
| <a id="tc-acl-12"></a> TC-ACL-12 | M biết ID phòng bị ẩn, gọi GET, PATCH, DELETE, GET ACL, PUT ACL | 404, không lộ thông tin; quyền quản lý vẫn cần quyền xem | [AC-COM-42](#ac-com-42) |

Tranh chấp guard và outbox còn được kiểm thử ở [TC-COM-22](memberships.md#tc-com-22), [TC-COM-25](../messaging/channel-messaging.md#tc-com-25), [TC-COM-26](mechanics.md#tc-com-26). Phòng và tin phòng phải chạy thêm ca tích hợp ngoài fixture quyền.

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Đã làm và đã kiểm thử xem [hồ sơ](../../records/verification/community/README.md). Migration: [thiết kế](mechanics.md#schema-migration). Thời hạn lưu: [vòng đời dữ liệu](../../system/data-lifecycle.md).

**Chưa chứng minh:** ghi tin và lịch sử dùng guard, đăng ký realtime và thời hạn thu hồi; duyệt, lời mời, chuyển chủ, rời, Media có bằng chứng theo gói riêng. Kết quả chính sách quyền qua HTTP không có nghĩa toàn bộ ca sử dụng đạt (OQ-004, 007, 008).

<a id="đầu-việc-rà-soát-và-kiểm-chứng-phân-quyền"></a>

| Mã | Đã có và còn lại | Ảnh hưởng | Theo dõi |
|---|---|---|---|
| ACL-O1 | Đã chốt (DEC-092, 098); migration, giới hạn vai trò, cộng gộp, vai trò hệ thống, quyền xem qua HTTP có bằng chứng. Còn: áp quyền vào mời, duyệt, cách tham gia; thu hồi tin và realtime | Tạo phòng, mời, duyệt, đổi quyền xem; quản lý vai trò | Vg; Sáng đánh giá |
| ACL-O2 | Bản chụp, `accessVersion`, epoch, giao diện ACL, guard giữ khóa có bằng chứng. Còn: ghi và đọc tin của Messaging, Hub thật dùng guard; thu hồi tranh với gửi và đăng ký; đối chiếu khi kết nối lại | Danh sách phòng, lịch sử, gửi, realtime | Vg, Sáng |
| ACL-O3 | Chuyển chủ ngay (DEC-076) có thứ tự khóa và transaction; còn bằng chứng chuyển chủ và rời | Một chủ server; quyền chủ cũ và người nhận; không xóa server | Vg |
| ACL-O4 | Đã có schema, hộp lời mời, hạn, hiệu lực theo người tạo (DEC-074, 087, 097); còn review và bằng chứng đồng thời | Server riêng tư | Vg |
| ACL-O5 | Guard, epoch, outbox có bằng chứng; registry, worker, bộ thu hồi cho quyền phòng chưa có. Hub DM có rồi nhưng không thay được. Cần đo chat phòng ngắt trong 5 giây; media (DEC-099) cần bằng chứng LiveKit riêng | Đồng bộ quyền, kiểm thử chất lượng | Vg, Sáng, Thái |

DM giữa hai người không phụ thuộc vai trò trong server; ACL-01 đến 05 có thể rà soát riêng trong gói DM. Theo dõi tiếp tại OQ-003, 004, 007.

---

Nguồn: DEC-024 đến 029, DEC-031 đến 034, DEC-038 đến 041, DEC-043, DEC-044, DEC-055 đến 058, DEC-072 đến 074, DEC-076, DEC-077, DEC-083, DEC-087, DEC-092, DEC-097 đến 099.

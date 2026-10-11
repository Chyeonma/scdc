# Phòng và vòng đời phòng

Community / Channels sở hữu metadata, kind, danh sách theo quyền và vòng đời phòng. Messaging sở hữu chat space và nội dung tin. Quy tắc, luồng sử dụng, UX, thiết kế dữ liệu/API và tiêu chí kiểm chứng của chủ đề được quản lý tại trang này.

<a id="implementation"></a>

## Trạng thái theo khả năng

Đối chiếu hiện trạng ngày 2026-10-08. Nền Community đã hợp nhất vào `main`; bằng chứng dẫn dưới đây gắn revision cụ thể, không phải kết quả chạy lại cho mọi thay đổi sau đó.

| Khả năng | Trạng thái và giới hạn | Bằng chứng |
|---|---|---|
| Tạo text — phần UC-COM-16 | Backend/WebClient đã kiểm chứng; space/channel/operation/outbox nguyên tử | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| List/detail — phần UC-COM-17; sửa metadata — UC-COM-18 | Đã kiểm chứng; UC-COM-17 chưa có lịch sử tin | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Cấu hình ACL HTTP — phần UC-COM-22 | Đã kiểm chứng; thuật toán, snapshot và quyền quản lý tại [Access control](access-control.md) | [Phòng text/ACL](../../records/verification/community/channels-access.md) |
| Xóa phòng — UC-COM-19; voice/Media | Thiết kế mục tiêu; chưa triển khai lifecycle/runtime | [Kế hoạch](../../project/planning.md#community-work-items) |
| Lịch sử, gửi/nhận, Hub và mất quyền | Chưa tích hợp; Community MVP chưa đạt | [Tin phòng](../messaging/channel-messaging.md) |

Quy tắc view/ACL chỉ định nghĩa tại [Access control](access-control.md#view-permissions); giao tiếp qua [Messaging](../messaging/channel-messaging.md).

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
| <a id="com-007"></a> COM-007 | Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng theo chủ đề. | DEC-026 |
| <a id="com-008"></a> COM-008 | Thành viên chỉ thấy phòng mình được cấp quyền xem. | DEC-027 |
| <a id="com-012"></a> COM-012 | Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | DEC-033 |
| <a id="com-015"></a> COM-015 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mình được phép xem. | DEC-038 |
| <a id="com-034"></a> COM-034 | Tên phòng 1–100, chủ đề tối đa 1.000; owner/người có quyền quản lý phòng tạo/sửa/xóa; xóa ngừng truy cập tin/media, chưa khôi phục trong v1. | DEC-077 |

<a id="use-cases"></a>

<a id="use-case"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-16"></a>

<a id="uc-com-16--tạo-phòng"></a>

### UC-COM-16 — Tạo phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity, Messaging (lifecycle chat space), WebClient; Media cho phòng voice ở v1.

**Tác nhân:** Owner hoặc thành viên có manage_channels.

**Điều kiện trước:** Server/membership/phiên hợp lệ; tạo mới chỉ cần manage_channels, không đòi xem một phòng khác.

**Kích hoạt:** Actor mở COM-S04, nhập tên/chủ đề và chọn kind text/voice.

**Luồng chính:**

1. Actor nhập dữ liệu theo [COM-034](#com-034), [COM-039](servers.md#com-039) và gửi thao tác tạo.
2. Hệ thống kiểm tra quyền, tên unique trong server và kind; tạo metadata cùng chat space qua hợp đồng lifecycle trong cùng transaction theo thiết kế. Phòng mới mặc định mọi thành viên view.
3. Sau commit, trả phòng vừa tạo và tải lại danh sách theo quyền; voice gọi lifecycle Media theo thiết kế riêng khi tích hợp.

**Ngoại lệ:** Tên sai/trùng hoặc actor mất quyền không tạo phòng dở dang. Retry cùng khóa/payload không tạo thêm space/phòng. Phụ thuộc lifecycle không đáp ứng phải rollback; tạo metadata voice chưa chứng minh gọi/media hoạt động.

**Kết quả sau cùng:** Có một phòng active đúng kind; text có thể dùng [UC-COM-17](#uc-com-17), [UC-COM-23](../messaging/channel-messaging.md#uc-com-23). Phòng read-only và text bên trong voice nằm ngoài hợp đồng v1.

<a id="uc-com-17"></a>

<a id="uc-com-17--xem-phòng-được-phép-và-lịch-sử-tin-văn-bản"></a>

### UC-COM-17 — Xem phòng được phép và lịch sử tin văn bản

**Module phụ trách:** Community / Channels (danh sách, metadata); Messaging (lịch sử tin).

**Phối hợp:** Identity, WebClient; Messaging đọc lịch sử qua guard quyền của Community.

**Tác nhân:** Thành viên active; owner vẫn phải thỏa điều kiện nền.

**Điều kiện trước:** Actor/server/membership hợp lệ; phòng còn active và actor có view tại mỗi lần đọc.

**Kích hoạt:** Thành viên chọn server, mở phòng hoặc tải lịch sử cũ tại COM-S03.

**Luồng chính:**

1. Community tính quyền theo thuật toán view và chỉ trả các phòng được xem trên từng trang.
2. Khi người dùng chọn phòng, kiểm tra lại quyền rồi trả metadata. Với text, Messaging tải lịch sử có phân trang qua guard Community.
3. Giao diện hiển thị tin hiện hành, dấu đã sửa/tombstone; thành viên mới được xem lịch sử cũ khi còn view. Voice chuyển sang hành trình Media.

**Ngoại lệ:** Không có phòng được xem hoặc chưa có tin hiển thị trạng thái rỗng. Hidden/deleted channel trả 404 kể cả biết ID hoặc có management permission; owner không vượt điều kiện nền. Tác giả cũ inactive không làm mất trang lịch sử của người đọc đủ quyền, projection theo thiết kế vòng đời dữ liệu. Mất view khi đang đọc chuyển [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Kết quả sau cùng:** Có danh sách/nội dung đúng quyền hiện hành; đọc lịch sử không phục hồi phòng đã deleted và không cấp quyền mới.

<a id="uc-com-18"></a>

<a id="uc-com-18--sửa-thông-tin-phòng"></a>

### UC-COM-18 — Sửa thông tin phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải metadata và version.

**Kích hoạt:** Actor sửa tên/chủ đề từ phần quản lý phòng.

**Luồng chính:**

1. Actor chỉnh tên/chủ đề theo [COM-034](#com-034), [COM-039](servers.md#com-039) và gửi expectedVersion.
2. Hệ thống kiểm tra quyền quản lý cùng view, dữ liệu/version và tên unique rồi cập nhật metadata.
3. Sau commit, trả phòng hiện hành và thông báo tải lại thông tin cho các client đủ quyền.

**Ngoại lệ:** Hidden/deleted channel không lộ metadata; thiếu manage_channels trong phòng được biết từ chối. Tên trùng/version cũ không ghi đè, lỗi giữ form để người dùng sửa. Kind không thay đổi qua PATCH trong v1; ACL dùng [UC-COM-22](access-control.md#uc-com-22).

**Kết quả sau cùng:** Metadata phòng được cập nhật; lịch sử/kind không bị đổi bởi thao tác sửa tên/chủ đề.

<a id="uc-com-19"></a>

<a id="uc-com-19--xóa-phòng"></a>

### UC-COM-19 — Xóa phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity, Messaging (lifecycle/thu hồi), WebClient; Media cho phòng voice ở v1.

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải version và được biết phòng.

**Kích hoạt:** Actor chọn xóa và xác nhận phòng cần xóa.

**Luồng chính:**

1. Hệ thống kiểm tra lại view/manage_channels/version.
2. Chuyển channel và Messaging space sang deleted, tăng version quyền và ghi sự kiện thu hồi cùng transaction qua hợp đồng lifecycle; voice phối hợp lifecycle Media theo thiết kế.
3. Sau commit, phòng biến mất khỏi danh sách, chặn đọc/gửi/join/resume; thu hồi kết nối và giao diện đóng nội dung theo [UC-COM-25](../messaging/channel-messaging.md#uc-com-25).

**Ngoại lệ:** Quyền/version cũ hoặc deleted không biến thành phòng mới. Delete tranh send/media admission tuân guard và thứ tự commit; nội dung đã commit trước vẫn giữ theo retention. Lỗi lifecycle rollback toàn bộ; proof cutoff media cần thực hiện riêng.

**Kết quả sau cùng:** Phòng không truy cập/khôi phục trong v1, giữ ID và nội dung theo DEC-105. Thao tác này không xóa toàn server hoặc purge backup.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

```text
COM-S03 · Màn hình rộng tham chiếu 1280 × 800
┌─────────────┬─────────────────────┬────────────────────────────────┐
│ Cộng đồng   │ Tên cộng đồng [⋯]   │ Tên phòng / chủ đề             │
│             │                     │                                │
│ • Nhóm A    │ Phòng theo chủ đề   │ Lịch sử tin                    │
│ • Nhóm B    │ # chung             │                                │
│             │ # học-tập           │ Tin đã bị xóa                  │
│ [Khám phá]  │                     │                  Tin của tôi   │
│             │ [+ Tạo phòng]*      │                  Đã sửa [⋯]   │
│             │ [Quản lý]*          ├────────────────────────────────┤
│             │                     │ [Nhập tin…              ] [Gửi]│
└─────────────┴─────────────────────┴────────────────────────────────┘
* Chỉ hiện thao tác người dùng được phép thực hiện.
```

Danh sách chỉ chứa phòng được phép xem. Thành viên mới được xem lịch sử
cũ của phòng đó. Không có cấu hình chỉ đọc trong đợt đầu; người xem được
phòng thì gửi được sau khi thỏa điều kiện tài khoản. Giới hạn tin,
sửa/xóa, lỗi và chủ động thử lại giống [wireframe DM](../messaging/direct-messaging.md#ux).

Trạng thái riêng: chưa có phòng được xem, phòng chưa có tin, tải lỗi,
lời mời không dùng được, yêu cầu chờ duyệt, mất quyền khi đang mở và
rời cộng đồng. Sau mất quyền/rời, đóng vùng nội dung phòng và tải lại
danh sách theo quyền; không tiếp tục gửi hoặc nhận tin qua kết nối cũ.

Phòng bị xóa đóng vùng nội dung và cuộc gọi theo DEC-077.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S04 Tạo phòng | Chủ sở hữu/người có quyền tạo phòng | Tên 1–100/chủ đề tối đa 1.000; mặc định mọi thành viên xem; lưu lỗi giữ dữ liệu, tạo thành công về phòng mới |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](../../system/community.md#ux).


<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="channel-dữ-liệu-và-nâng-cấp"></a>

### Dữ liệu và nâng cấp

004 thêm name_key .NET trim/NFC/ToLowerInvariant so sánh C, unique partial theo server với channel active/nondeleted. Tên 1–100 UTF-16, topic tối đa 1.000, CRLF chuẩn hóa khi writer; giữ normalized_name legacy nhưng bỏ unique cũ. Kind text/voice bất biến; writer mới chỉ tạo text. Status active/deleted, deleted_at, version/access_version int32; một trigger version/timestamp cho mỗi update, kể cả ACL. AccessVersion dùng CAS cấu hình, metadata dùng version tổng thể của resource.

Runner yêu cầu 001/002/003 đúng checksum, mapping JSON đầy đủ cho channel legacy với kind/defaultView; tên/key theo batch 500. Legacy read-only, Messaging space archived hoặc deleted thiếu timestamp yêu cầu repair có review; không tự chọn kind/quyền, đổi tên/epoch hoặc purge nội dung. Space active/deleted giữ trạng thái tương ứng cho channel. Drain writer cũ → runner 004 → deploy mới. Bootstrap mới gồm 001–004; seed ghi key/defaultView explicit và kind mặc định text.

<a id="channel-api-và-quyền"></a>

### API và quyền

GET/POST channels và GET/PATCH channel theo OpenAPI. POST default kind text/topic null, voice chưa có lifecycle Media nên bị từ chối. PATCH chỉ name/topic + expectedVersion; null topic để xóa. [Cấu hình ACL](access-control.md#acl-api) có API, snapshot và CAS riêng. Request được giới hạn kích thước, không tự replay mutation.

Management guard giữ Identity lease → server share/read hoặc update/mutation → channel share/update. Mọi single-channel route kiểm tra view trước quyền quản lý; phòng bị ẩn, đã xóa hoặc thuộc server khác trả 404, kể cả actor có manage_channels/manage_channel_access. Foundation phải valid session/active verified account/server/member. Tính view theo [policy chuẩn](access-control.md#view-permissions). Danh sách dùng cùng policy trong SQL trước LIMIT; cursor actor/server/limit/purpose/24 giờ, không hứa snapshot bất biến.

Create ghi Messaging space qua IChatSpaceLifecycle trên cùng connection/transaction; Community chỉ ghi community schema. Reader/writer tin phải dùng [channel admission guard](../../system/community.md#transactions); chưa có lịch sử/send/subscription writer tiêu thụ hợp đồng này.

Create/metadata mutation tăng server accessVersion và ghi event outbox IDs/cause/version cùng transaction, không broadcast tên/ACL/member list. Metadata bump channel version; metadata no-op giữ version/event. Version và sự kiện khi đổi ACL hoặc dọn override được định nghĩa tại [Access control](access-control.md#acl-api).

<a id="channel-webclient-và-bằng-chứng"></a>

### WebClient và bằng chứng

Detail member vào danh sách phòng theo quyền. Tạo giữ operation/body theo actor/server trước POST, retry chủ động nguyên body/key sau mất response/reload. Sửa metadata giữ version đã đọc; conflict/timeout cần GET đối soát trước lưu lại. Actor/server/selection đổi hủy đọc cũ và bỏ dữ liệu quản lý; 401/403/404 đóng metadata cũ. Không hiển thị tin/composer khi gói chưa có API tin.

Giao diện sửa quyền, catalog role/member và giữ draft/epoch được mô tả tại [ACL editor](access-control.md#acl-editor-ui). Máy chủ kiểm tra lại quyền dưới khóa cho từng thao tác.

Nghiệm thu API/DB/shared transaction và Chromium độc lập với thuật toán fixture tại [hồ sơ nghiệm thu](../../records/verification/community/channels-access.md). Không suy HTTP guard hoặc outbox thành proof thu hồi realtime/Media; phụ thuộc và việc tiếp theo ở [kế hoạch Community](../../project/planning.md#community-work-items).

### Lifecycle xóa và voice — thiết kế mục tiêu

<a id="delete-giao-dịch-của-thành-phần"></a>

#### Giao dịch của thành phần

- Xóa phòng: Community chuyển deleted/version/accessVersion, Messaging đánh dấu space deleted qua hợp đồng lifecycle, outbox thu hồi cùng transaction. Không có hai commit độc lập khiến phòng đã deleted vẫn nhận tin; không purge tin/backup ở đây.

Voice cần Media admission/lifecycle. Đề xuất gọi `IMediaRoomLifecycle` tạo room DB rỗng cùng transaction, đánh dấu closing khi xóa; interface/migration chưa có, SFU provision/dừng sau commit. Cutoff Media theo DEC-099 cần bằng chứng riêng. Kind voice có thể được bảo toàn khi migrate legacy, nhưng writer hiện hành chỉ tạo text.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-07"></a> AC-COM-07 | Thành viên không có quyền tạo phòng thử tạo phòng. | Hệ thống từ chối thao tác; chủ sở hữu hoặc người được cấp quyền thực hiện được. |
| <a id="ac-com-14"></a> AC-COM-14 | Tin được lưu trong phòng khi thành viên đang vắng mặt; thành viên mở lại phòng sau đó và vẫn có quyền xem. | Thành viên thấy tin trong lịch sử phòng. |
| <a id="ac-com-15"></a> AC-COM-15 | Thành viên mới tham gia mở phòng có tin từ trước và mình được phép xem. | Thành viên thấy lịch sử cũ của phòng. |
| <a id="ac-com-34"></a> AC-COM-34 | Người đúng/sai quyền sửa/xóa phòng đang có tin/cuộc gọi | Deleted không đọc/gửi/nhận/tiếp tục gọi được; chặn race writer; không có khôi phục v1 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [trạng thái theo khả năng](#implementation).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](../messaging/channel-messaging.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-09"></a> TC-COM-09 | Thành viên mới/mất mạng mở lại phòng có tin cũ | Lịch sử đầy đủ nếu còn quyền xem | [AC-COM-14](#ac-com-14), [AC-COM-15](#ac-com-15) |
| <a id="tc-com-15"></a> TC-COM-15 | Xóa phòng cùng lúc gửi/join media; thử ID phòng cũ sau xóa | Không phục hồi phòng, không đọc/nhận/gọi sau thu hồi; tin đã commit giữ theo retention | [AC-COM-34](#ac-com-34) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../../project/planning.md#community-work-items), [migration](../../system/community.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

Lifecycle create text cùng Messaging, danh sách/metadata/view guard/tên Unicode đã có bằng chứng trong [gói phòng/ACL](../../records/verification/community/channels-access.md). Còn lifecycle delete nguyên tử và race gửi/xóa, lịch sử/writer Messaging dùng guard, Hub/thu hồi; voice phụ thuộc Media lifecycle và proof cutoff riêng. Phạm vi đã đạt và phần chưa chứng minh được đối chiếu tại [verification](../../records/verification/community/README.md), tiến độ chỉ ghi tại [trạng thái theo khả năng](#implementation).

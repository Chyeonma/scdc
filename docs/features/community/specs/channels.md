# SCDC — Channels — Phòng và vòng đời phòng

Cập nhật: 2026-10-08. Đặc tả nghiệp vụ và tiêu chí kiểm chứng; tiến độ hiện tại tại [status.md](../status.md).

Sở hữu tên, topic, kind và trạng thái vòng đời phòng. Tạo/xóa phối hợp Messaging/Media qua hợp đồng lifecycle; view và ACL theo Permissions.

[Tổng quan Community](../README.md) · [Truy vết UC/COM/AC/TC](README.md#use-cases) · [Kế hoạch triển khai](../delivery/README.md#use-case-delivery).

Thiết kế kỹ thuật của phần này: [dữ liệu/API và giao dịch](../design/channels.md#contracts).

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
| <a id="com-007"></a> COM-007 | Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng theo chủ đề. | DEC-026 |
| <a id="com-008"></a> COM-008 | Thành viên chỉ thấy phòng mình được cấp quyền xem. | DEC-027 |
| <a id="com-012"></a> COM-012 | Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn quyền. | DEC-033 |
| <a id="com-015"></a> COM-015 | Thành viên mới vào cộng đồng được xem lịch sử cũ của phòng mình được phép xem. | DEC-038 |
| <a id="com-034"></a> COM-034 | Tên phòng 1–100, chủ đề tối đa 1.000; owner/người có quyền quản lý phòng tạo/sửa/xóa; xóa ngừng truy cập tin/media, chưa khôi phục trong v1. | DEC-077 |

<a id="use-cases"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](integration.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-16"></a>

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

**Kết quả sau cùng:** Có một phòng active đúng kind; text có thể dùng [UC-COM-17](#uc-com-17), [UC-COM-23](integration.md#uc-com-23). Phòng read-only và text bên trong voice nằm ngoài hợp đồng v1.

<a id="uc-com-17"></a>

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

**Ngoại lệ:** Không có phòng được xem hoặc chưa có tin hiển thị trạng thái rỗng. Hidden/deleted channel trả 404 kể cả biết ID hoặc có management permission; owner không vượt điều kiện nền. Tác giả cũ inactive không làm mất trang lịch sử của người đọc đủ quyền, projection theo thiết kế vòng đời dữ liệu. Mất view khi đang đọc chuyển [UC-COM-25](integration.md#uc-com-25).

**Kết quả sau cùng:** Có danh sách/nội dung đúng quyền hiện hành; đọc lịch sử không phục hồi phòng đã deleted và không cấp quyền mới.

<a id="uc-com-18"></a>

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

**Ngoại lệ:** Hidden/deleted channel không lộ metadata; thiếu manage_channels trong phòng được biết từ chối. Tên trùng/version cũ không ghi đè, lỗi giữ form để người dùng sửa. Kind không thay đổi qua PATCH trong v1; ACL dùng [UC-COM-22](permissions.md#uc-com-22).

**Kết quả sau cùng:** Metadata phòng được cập nhật; lịch sử/kind không bị đổi bởi thao tác sửa tên/chủ đề.

<a id="uc-com-19"></a>

### UC-COM-19 — Xóa phòng

**Module phụ trách:** Community / Channels.

**Phối hợp:** Identity, Messaging (lifecycle/thu hồi), WebClient; Media cho phòng voice ở v1.

**Tác nhân:** Owner hoặc thành viên có manage_channels và view phòng đích.

**Điều kiện trước:** Actor/server/membership/phòng hợp lệ; đã tải version và được biết phòng.

**Kích hoạt:** Actor chọn xóa và xác nhận phòng cần xóa.

**Luồng chính:**

1. Hệ thống kiểm tra lại view/manage_channels/version.
2. Chuyển channel và Messaging space sang deleted, tăng version quyền và ghi sự kiện thu hồi cùng transaction qua hợp đồng lifecycle; voice phối hợp lifecycle Media theo thiết kế.
3. Sau commit, phòng biến mất khỏi danh sách, chặn đọc/gửi/join/resume; thu hồi kết nối và giao diện đóng nội dung theo [UC-COM-25](integration.md#uc-com-25).

**Ngoại lệ:** Quyền/version cũ hoặc deleted không biến thành phòng mới. Delete tranh send/media admission tuân guard và thứ tự commit; nội dung đã commit trước vẫn giữ theo retention. Lỗi lifecycle rollback toàn bộ; proof cutoff media cần thực hiện riêng.

**Kết quả sau cùng:** Phòng không truy cập/khôi phục trong v1, giữ ID và nội dung theo DEC-105. Thao tác này không xóa toàn server hoặc purge backup.

<a id="ux"></a>

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
sửa/xóa, lỗi và chủ động thử lại giống [wireframe DM](../../direct-messaging/specs/ux.md#ux).

Trạng thái riêng: chưa có phòng được xem, phòng chưa có tin, tải lỗi,
lời mời không dùng được, yêu cầu chờ duyệt, mất quyền khi đang mở và
rời cộng đồng. Sau mất quyền/rời, đóng vùng nội dung phòng và tải lại
danh sách theo quyền; không tiếp tục gửi hoặc nhận tin qua kết nối cũ.

Phòng bị xóa đóng vùng nội dung và cuộc gọi theo DEC-077.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S04 Tạo phòng | Chủ sở hữu/người có quyền tạo phòng | Tên 1–100/chủ đề tối đa 1.000; mặc định mọi thành viên xem; lưu lỗi giữ dữ liệu, tạo thành công về phòng mới |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](integration.md#ux).

<a id="acceptance"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-07"></a> AC-COM-07 | Thành viên không có quyền tạo phòng thử tạo phòng. | Hệ thống từ chối thao tác; chủ sở hữu hoặc người được cấp quyền thực hiện được. |
| <a id="ac-com-14"></a> AC-COM-14 | Tin được lưu trong phòng khi thành viên đang vắng mặt; thành viên mở lại phòng sau đó và vẫn có quyền xem. | Thành viên thấy tin trong lịch sử phòng. |
| <a id="ac-com-15"></a> AC-COM-15 | Thành viên mới tham gia mở phòng có tin từ trước và mình được phép xem. | Thành viên thấy lịch sử cũ của phòng. |
| <a id="ac-com-34"></a> AC-COM-34 | Người đúng/sai quyền sửa/xóa phòng đang có tin/cuộc gọi | Deleted không đọc/gửi/nhận/tiếp tục gọi được; chặn race writer; không có khôi phục v1 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [tiến độ](../status.md).

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](integration.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-09"></a> TC-COM-09 | Thành viên mới/mất mạng mở lại phòng có tin cũ | Lịch sử đầy đủ nếu còn quyền xem | [AC-COM-14](#ac-com-14), [AC-COM-15](#ac-com-15) |
| <a id="tc-com-15"></a> TC-COM-15 | Xóa phòng cùng lúc gửi/join media; thử ID phòng cũ sau xóa | Không phục hồi phòng, không đọc/nhận/gọi sau thu hồi; tin đã commit giữ theo retention | [AC-COM-34](#ac-com-34) |

<a id="gaps"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../delivery/README.md#use-case-delivery), [migration](../design/integration.md#schema-migration) và [vòng đời dữ liệu](../../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

Lifecycle create text cùng Messaging, danh sách/metadata/view guard/tên Unicode đã có bằng chứng trong [gói phòng/ACL](../delivery/channels-access/acceptance.md). Còn lifecycle delete nguyên tử và race gửi/xóa, lịch sử/writer Messaging dùng guard, Hub/thu hồi; voice phụ thuộc Media lifecycle và proof cutoff riêng. Phạm vi đã đạt và phần chưa chứng minh được đối chiếu tại [verification](../delivery/verification.md), tiến độ chỉ ghi tại [status](../status.md).

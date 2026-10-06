# SCDC — Cộng đồng, phòng và phân quyền

Cập nhật: 2026-10-06. Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Quy tắc COM, ACL-06–20, use case UC-COM, tiêu chí AC-COM, màn hình COM-S và ca TC-COM/TC-ACL.

Quy tắc tham gia/quyền cốt lõi đã xác nhận; các luồng DEC-072–077/087 và bổ sung vai trò/tìm kiếm/tên/phạm vi/visibility/lời mời/quản lý phòng DEC-092–098 được cụ thể hóa bên dưới. Thiết kế dữ liệu/API là bản dự thảo để rà soát. Community/Messaging mới có nền module; wireframe và ca kiểm thử chưa phải kết quả triển khai hoặc nghiệm thu.

Đã thống nhất tổ chức ngày 2026-10-06: năm thành phần nghiệp vụ nội bộ trong cùng SCDC.Community. Tài liệu chi tiết được chuyển về từng thành phần; trang này giữ scope, điều hướng, truy vết và kế hoạch. Source hiện vẫn chỉ đăng ký module Foundation.

## Mục lục

- [Phạm vi và hành trình](#requirements)
- [Năm thành phần và cấu trúc code](#organization)
- [Quyền và thu hồi](community/permissions.md#permissions)
- [Use case và truy vết quy tắc](#use-cases)
- [Giao diện](#ux)
- [Hợp đồng và thiết kế](community/integration.md#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Kế hoạch và gói đầu tiên](#use-case-delivery)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

## Phạm vi và hành trình

Người dùng có thể tìm cộng đồng công khai hoặc dùng liên kết mời để tham
gia. Trong cộng đồng, các phòng theo chủ đề giúp tách cuộc trò chuyện;
thành viên chỉ nhìn thấy phòng mình được cấp quyền xem và nhắn tin văn bản
trong phòng mình được phép nhắn.

Đợt này tập trung vào cách tham gia, hiển thị phòng và nhắn tin văn bản.
Việc gửi file tài liệu trong phòng được để sang đợt sau theo
DEC-023. Các tính năng thoại/video và chia sẻ màn hình thuộc phạm vi
[Project Brief](../project.md#scope) nhưng chưa được đặc
tả trong tài liệu này.

### Hành trình tham gia

1. Người dùng tìm cộng đồng. Kết quả chỉ gồm cộng đồng công khai.
2. Nếu cộng đồng cho vào ngay, người dùng tham gia trực tiếp. Đây là chế
   độ mặc định của cộng đồng công khai mới tạo.
3. Nếu cộng đồng bật chế độ chờ duyệt, người dùng gửi yêu cầu tham gia.
   Chủ sở hữu hoặc người được cấp quyền có thể duyệt yêu cầu.
4. Người dùng có liên kết mời hợp lệ được vào ngay, kể cả khi cộng đồng
   đang bật chế độ chờ duyệt. Người tạo liên kết mời chọn thời hạn hiệu
   lực của liên kết. Chỉ chủ sở hữu hoặc người được cấp quyền tạo liên
   kết mời.
5. Sau khi tham gia, thành viên chỉ thấy những phòng mình được cấp quyền
   xem. Phòng mới mặc định cho mọi thành viên xem được, trừ khi giới hạn
   quyền. Chỉ chủ sở hữu hoặc người được cấp quyền có thể tạo phòng mới.
6. Mọi thành viên có quyền xem phòng đều gửi được tin văn bản trong đợt
   đầu; người gửi được sửa và xóa tin của mình như tin riêng. Thành viên
   mới được xem lịch sử cũ của phòng nếu còn quyền xem.
7. Cộng đồng riêng tư không xuất hiện trong tìm kiếm; người dùng có thể
   tham gia bằng liên kết mời hoặc được thêm trực tiếp. Thành viên thường
   có thể tự rời cộng đồng.

Các quy tắc COM giữ nguyên mã và nội dung, nguồn chuẩn ở từng thành phần theo [bảng truy vết](#use-case-rules). Mỗi UC/COM/AC/TC chi tiết được định nghĩa một nơi; ca xuyên phần được dẫn chiếu từ bảng tổng hợp.

<a id="organization"></a>

## Năm thành phần và cấu trúc code

| Thành phần | Trách nhiệm | Use case | Đặc tả |
|---|---|---|---|
| Servers | Metadata, search, visibility, join mode, ownership | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-02](community/servers.md#uc-com-02), [UC-COM-03](community/servers.md#uc-com-03), [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-05](community/servers.md#uc-com-05), [UC-COM-14](community/servers.md#uc-com-14) | [Servers](community/servers.md) |
| Memberships | Membership/epoch, join, requests, approve/reject/cancel, leave/rejoin | [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-08](community/memberships.md#uc-com-08), [UC-COM-15](community/memberships.md#uc-com-15) | [Memberships](community/memberships.md) |
| Invitations | Link và mời đích danh, expiry/lượt/secret/transition | [UC-COM-09](community/invitations.md#uc-com-09), [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13) | [Invitations](community/invitations.md) |
| Channels | Metadata/kind/vòng đời phòng, danh sách theo view | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-19](community/channels.md#uc-com-19) | [Channels](community/channels.md) |
| Permissions | Role, assignment, ACL, evaluator/checker/guard | [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21), [UC-COM-22](community/permissions.md#uc-com-22) | [Permissions](community/permissions.md) |

[Tích hợp dùng chung](community/integration.md) quản lý transaction, ID/version/retry, migration, lỗi, tin phòng và realtime; [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) do Messaging thực hiện, [UC-COM-25](community/integration.md#uc-com-25) phối hợp các module. Đây là tài liệu hỗ trợ năm phần nghiệp vụ, không thêm module độc lập.

### Cấu trúc code mục tiêu

Source hiện chỉ có CommunityModule.cs và project reference; cây dưới đây là bố cục đã thống nhất để dùng khi triển khai. Nghiệp vụ đặt theo feature, Domain/Application ở trong từng phần; các phần cùng một assembly, schema community và CommunityDbContext.

```text
services/Modules/Community/
├── CommunityModule.cs
├── Features/
│   ├── Servers/{Domain,Application}/
│   ├── Memberships/{Domain,Application}/
│   ├── Invitations/{Domain,Application}/
│   ├── Channels/{Domain,Application}/
│   └── Permissions/{Domain,Application,Infrastructure}/
└── Infrastructure/
    ├── Persistence/       # CommunityDbContext và mapping
    ├── Idempotency/       # operation key, fingerprint, key access
    └── Outbox/            # ghi/dispatch thay đổi Community
```

Controllers đặt tại services/SCDC.Api/Controllers/Community theo từng nhóm; test theo feature trong tests/SCDC.Api.Tests/Community. CommunityModule.cs là điểm ghép DI của module. Khi bắt đầu code, tạo file cùng chức năng thực tế thay vì đưa thư mục rỗng vào Git.

### Ranh giới và phụ thuộc

- Servers giữ owner_user_id là nguồn chuẩn duy nhất; Memberships kiểm tra target còn membership active khi transfer. Permissions đọc ownership để tính policy, không có bản owner riêng.
- Memberships cung cấp hành vi tạo/kích hoạt tư cách chung cho join, approve và accept. Invitations tiêu thụ lời mời/lượt cùng transaction với membership; không lặp một writer membership khác.
- Permissions Domain tính quyền trên snapshot và không gọi Application service các phần khác. Application orchestration phối hợp stores/policies; Infrastructure dựng snapshot/guard bằng dữ liệu Community. Tránh vòng gọi service giữa Channels, Memberships và Permissions.
- Các feature nội bộ dùng chung transaction/DbContext; chia thư mục không chia commit nghiệp vụ. Tạo server phải ghi owner/@everyone/operation cùng commit; accept lời mời phải ghi membership/lượt/transition cùng commit; xóa phòng phối hợp Messaging lifecycle.
- Giao tiếp Identity/Messaging/Media qua SCDC.Contracts, mỗi module chỉ đọc/ghi dữ liệu mình sở hữu. Cơ chế guard/lock order và snapshot được quản lý tại [thiết kế tích hợp](community/integration.md#transactions).

<a id="permissions"></a>

## Quyền và thu hồi

[Ma trận ACL-06–20](community/permissions.md#permissions), role/assignment/ACL và các fixture nằm ở Permissions. Các use case khác dẫn chiếu policy và guard từ phần này; tài khoản/phiên do Identity kiểm tra.

<a id="view-permissions"></a>

[Thuật toán quyền xem](community/permissions.md#view-permissions) và [thu hồi khi đang dùng](community/permissions.md#revocation) là nguồn chuẩn; [transaction guard](community/integration.md#transactions) giữ kiểm tra tới commit.

<a id="use-cases"></a>

## Use case Community

Bổ sung ngày 2026-10-06. Các UC tổng hợp hành vi mục tiêu từ [quy tắc COM](#requirements), [ma trận ACL](community/permissions.md#permissions) và các quyết định đã dẫn chiếu; phần use case là bản dự thảo để rà soát trước triển khai. Community/Messaging vẫn ở Foundation, toàn bộ UC-COM chưa có implementation hoặc kết quả kiểm thử sản phẩm. Thuật toán, lỗi, giao dịch và schema kỹ thuật tiếp tục được quản lý tại [thiết kế chi tiết](community/integration.md#detailed-design).

Community thực hiện quản lý server, membership, phòng, lời mời và quyền. [UC-COM-17](community/channels.md#uc-com-17) đọc lịch sử qua Messaging; [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) do Messaging thực hiện trên quyền Community; [UC-COM-25](community/integration.md#uc-com-25) phối hợp Identity/Community/Messaging và WebClient. Chức năng vào phòng thoại/gọi/video thuộc [đặc tả media](voice-video.md), không được coi đã triển khai khi tạo được metadata phòng voice.

[Điều kiện và ngoại lệ dùng chung](community/integration.md#use-case-conditions) áp dụng cho toàn bộ UC; nội dung UC nằm ở thành phần chủ trì dưới đây.

| Use case | Mục tiêu | Màn hình/thành phần |
|---|---|---|
| <a id="uc-com-01"></a> [UC-COM-01](community/servers.md#uc-com-01) | Tạo cộng đồng | COM-S10 |
| <a id="uc-com-02"></a> [UC-COM-02](community/servers.md#uc-com-02) | Tìm và xem cộng đồng công khai | COM-S01/02 |
| <a id="uc-com-03"></a> [UC-COM-03](community/servers.md#uc-com-03) | Xem cộng đồng đang tham gia và tư cách của mình | COM-S03, danh sách cộng đồng |
| <a id="uc-com-04"></a> [UC-COM-04](community/servers.md#uc-com-04) | Sửa thông tin và visibility cộng đồng | COM-S10 |
| <a id="uc-com-05"></a> [UC-COM-05](community/servers.md#uc-com-05) | Đổi chế độ tham gia | COM-S07 |
| <a id="uc-com-06"></a> [UC-COM-06](community/memberships.md#uc-com-06) | Tham gia cộng đồng công khai vào ngay | COM-S02 |
| <a id="uc-com-07"></a> [UC-COM-07](community/memberships.md#uc-com-07) | Gửi, xem và hủy yêu cầu tham gia | COM-S02/06 |
| <a id="uc-com-08"></a> [UC-COM-08](community/memberships.md#uc-com-08) | Duyệt hoặc từ chối yêu cầu tham gia | COM-S06 |
| <a id="uc-com-09"></a> [UC-COM-09](community/invitations.md#uc-com-09) | Tạo, xem và sao chép link mời | COM-S05 |
| <a id="uc-com-10"></a> [UC-COM-10](community/invitations.md#uc-com-10) | Thu hồi link mời | COM-S05 |
| <a id="uc-com-11"></a> [UC-COM-11](community/invitations.md#uc-com-11) | Xem trước và tham gia bằng link mời | COM-S02 |
| <a id="uc-com-12"></a> [UC-COM-12](community/invitations.md#uc-com-12) | Gửi, xem và hủy lời mời đích danh | COM-S11, quản lý lời mời |
| <a id="uc-com-13"></a> [UC-COM-13](community/invitations.md#uc-com-13) | Xem, chấp nhận hoặc từ chối lời mời đích danh | COM-S11, inbox người nhận |
| <a id="uc-com-14"></a> [UC-COM-14](community/servers.md#uc-com-14) | Chuyển chủ sở hữu | COM-S12 |
| <a id="uc-com-15"></a> [UC-COM-15](community/memberships.md#uc-com-15) | Rời cộng đồng | COM-S03, menu cộng đồng |
| <a id="uc-com-16"></a> [UC-COM-16](community/channels.md#uc-com-16) | Tạo phòng | COM-S04 |
| <a id="uc-com-17"></a> [UC-COM-17](community/channels.md#uc-com-17) | Xem phòng được phép và lịch sử tin văn bản | COM-S03 |
| <a id="uc-com-18"></a> [UC-COM-18](community/channels.md#uc-com-18) | Sửa thông tin phòng | Quản lý phòng từ COM-S03 |
| <a id="uc-com-19"></a> [UC-COM-19](community/channels.md#uc-com-19) | Xóa phòng | Quản lý phòng từ COM-S03 |
| <a id="uc-com-20"></a> [UC-COM-20](community/permissions.md#uc-com-20) | Tạo, sửa và xóa vai trò tự tạo | COM-S08 |
| <a id="uc-com-21"></a> [UC-COM-21](community/permissions.md#uc-com-21) | Gán hoặc thu hồi vai trò thành viên | COM-S08 |
| <a id="uc-com-22"></a> [UC-COM-22](community/permissions.md#uc-com-22) | Xem và thay cấu hình quyền xem phòng | COM-S09 |
| <a id="uc-com-23"></a> [UC-COM-23](community/integration.md#uc-com-23) | Gửi và chủ động thử lại tin văn bản | COM-S03, composer; Messaging |
| <a id="uc-com-24"></a> [UC-COM-24](community/integration.md#uc-com-24) | Sửa hoặc xóa tin của mình | COM-S03, menu tin; Messaging |
| <a id="uc-com-25"></a> [UC-COM-25](community/integration.md#uc-com-25) | Nhận cập nhật, kết nối lại và xử lý mất quyền | COM-S03, Hub chat và thông báo theo người nhận |

<a id="use-case-rules"></a>

## Truy vết quy tắc cộng đồng

Giới hạn và thứ tự ưu tiên giữ ở bảng COM/ACL; bảng này chỉ xác định UC áp dụng. [COM-009](community/integration.md#com-009) và phần scope của [COM-039](community/servers.md#com-039) được ghi thành giới hạn, không tạo UC chức năng ngoài MVP.

| Quy tắc | Use case áp dụng | Điểm cần đối chiếu |
|---|---|---|
| [COM-001](community/memberships.md#com-001) | [UC-COM-02](community/servers.md#uc-com-02), [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-11](community/invitations.md#uc-com-11) | Tìm kiếm hoặc lời mời dẫn tới đúng nhánh tham gia |
| [COM-002](community/servers.md#com-002) | [UC-COM-02](community/servers.md#uc-com-02), [UC-COM-04](community/servers.md#uc-com-04) | Chỉ public xuất hiện trong tìm kiếm |
| [COM-003](community/servers.md#com-003) | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-05](community/servers.md#uc-com-05), [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07) | Mặc định vào ngay; approval tạo pending |
| [COM-004](community/invitations.md#com-004) | [UC-COM-11](community/invitations.md#uc-com-11) | Link hợp lệ bỏ qua chờ duyệt |
| [COM-005](community/invitations.md#com-005) | [UC-COM-09](community/invitations.md#uc-com-09) | Người tạo chọn hạn link |
| [COM-006](community/servers.md#com-006) | [UC-COM-05](community/servers.md#uc-com-05) | Đúng quyền đổi join mode |
| [COM-007](community/channels.md#com-007) | [UC-COM-16](community/channels.md#uc-com-16) | Đúng quyền tạo phòng |
| [COM-008](community/channels.md#com-008) | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-25](community/integration.md#uc-com-25) | Danh sách, nội dung và cập nhật đều theo view |
| [COM-009](community/integration.md#com-009) | [UC-COM-23](community/integration.md#uc-com-23) | Composer chỉ gửi văn bản; file trong phòng ở đợt sau |
| [COM-010](community/invitations.md#com-010) | [UC-COM-09](community/invitations.md#uc-com-09) | Đúng quyền tạo lời mời |
| [COM-011](community/memberships.md#com-011) | [UC-COM-08](community/memberships.md#uc-com-08) | Đúng quyền duyệt |
| [COM-012](community/channels.md#com-012) | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-22](community/permissions.md#uc-com-22) | Phòng mới mặc định cho mọi thành viên xem |
| [COM-013](community/integration.md#com-013) | [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) | Gửi, sửa, xóa và trạng thái tương tự DM |
| [COM-014](community/integration.md#com-014) | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-25](community/integration.md#uc-com-25) | Tin lưu bền đọc lại khi còn quyền |
| [COM-015](community/channels.md#com-015) | [UC-COM-17](community/channels.md#uc-com-17) | Thành viên mới được xem lịch sử cũ theo view |
| [COM-016](community/permissions.md#com-016) | [UC-COM-22](community/permissions.md#uc-com-22) | Đúng quyền thay ACL |
| [COM-017](community/integration.md#com-017) | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-23](community/integration.md#uc-com-23) | Có view thì gửi text khi đủ điều kiện tài khoản |
| [COM-018](community/integration.md#com-018) | [UC-COM-23](community/integration.md#uc-com-23) | Thử lại cùng thao tác không tạo tin trùng |
| [COM-019](community/invitations.md#com-019) | [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13) | Private tham gia bằng lời mời hợp lệ |
| [COM-020](community/invitations.md#com-020) | [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11) | Link thu hồi không cấp membership |
| [COM-021](community/memberships.md#com-021) | [UC-COM-15](community/memberships.md#uc-com-15) | Thành viên thường tự rời |
| [COM-022](community/integration.md#com-022) | [UC-COM-23](community/integration.md#uc-com-23) | Chưa xác minh không được dùng ứng dụng/gửi tin |
| [COM-023](community/integration.md#com-023) | [UC-COM-24](community/integration.md#uc-com-24) | Sửa chỉ giữ nội dung hiện hành |
| [COM-024](community/integration.md#com-024) | [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) | Validation nội dung dùng chung DM |
| [COM-025](community/permissions.md#com-025) | [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21), [UC-COM-22](community/permissions.md#uc-com-22) | Vai trò quản lý và ngoại lệ view cá nhân |
| [COM-026](community/permissions.md#com-026) | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-22](community/permissions.md#uc-com-22) | Owner luôn view sau điều kiện nền |
| [COM-027](community/permissions.md#com-027) | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-22](community/permissions.md#uc-com-22), [UC-COM-25](community/integration.md#uc-com-25) | Role deny thắng, cá nhân sau cùng |
| [COM-028](community/permissions.md#com-028) | [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21) | Chỉ owner quản lý vai trò/assignment |
| [COM-029](community/servers.md#com-029) | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-04](community/servers.md#uc-com-04) | Tạo bởi account đã xác minh, metadata chỉ owner sửa |
| [COM-030](community/memberships.md#com-030) | [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-08](community/memberships.md#uc-com-08) | Hủy/từ chối và gửi yêu cầu mới |
| [COM-031](community/invitations.md#com-031) | [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13) | Nhận mời đích danh mới tạo membership |
| [COM-032](community/invitations.md#com-032) | [UC-COM-09](community/invitations.md#uc-com-09), [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11) | Hạn/lượt và thu hồi link |
| [COM-033](community/servers.md#com-033) | [UC-COM-14](community/servers.md#uc-com-14), [UC-COM-15](community/memberships.md#uc-com-15) | Chuyển ngay; owner phải chuyển trước rời |
| [COM-034](community/channels.md#com-034) | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-19](community/channels.md#uc-com-19) | Tạo/sửa/xóa phòng và vòng đời |
| [COM-035](community/memberships.md#com-035) | [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-13](community/invitations.md#uc-com-13), [UC-COM-15](community/memberships.md#uc-com-15) | Rejoin dùng epoch mới và role mặc định |
| [COM-036](community/invitations.md#com-036) | [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13) | Mời đích danh hết hạn hoặc terminal không accept được |
| [COM-037](community/permissions.md#com-037) | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21) | @everyone và giới hạn custom role, union management |
| [COM-038](community/servers.md#com-038) | [UC-COM-02](community/servers.md#uc-com-02) | Search/UTF-16/khớp/phân trang |
| [COM-039](community/servers.md#com-039) | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-20](community/permissions.md#uc-com-20) | Tên Unicode; xóa toàn server ngoài MVP |
| [COM-040](community/servers.md#com-040) | [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-08](community/memberships.md#uc-com-08) | Private switch kết thúc pending nguyên tử |
| [COM-041](community/invitations.md#com-041) | [UC-COM-09](community/invitations.md#uc-com-09), [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13) | Hiệu lực mời độc lập creator; actor thao tác xét quyền hiện hành |
| [COM-042](community/permissions.md#com-042) | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-19](community/channels.md#uc-com-19), [UC-COM-22](community/permissions.md#uc-com-22) | Quản lý phòng có sẵn cần view |

<a id="use-case-coverage"></a>

## Đối chiếu use case với API và kiểm thử

API trong bảng là hợp đồng mục tiêu tại [community.openapi.json](../contracts/community.openapi.json), chưa phải endpoint hoạt động. Các đường dẫn dùng prefix `/api/v1`; `{id}` là serverId, `{channelId}` là phòng đích. AC và TC dẫn tới [tiêu chí chấp nhận](#acceptance) và [ca kiểm thử](#tests) hiện có. Mọi UC-COM đang ở trạng thái **chưa triển khai/chưa chạy**; bảng là kế hoạch truy vết, không phải bằng chứng đạt.

| Use case | API/thành phần mục tiêu | AC-COM | TC hiện có và phụ thuộc |
|---|---|---|---|
| [UC-COM-01](community/servers.md#uc-com-01) | POST /servers | [AC-COM-29](community/servers.md#ac-com-29), [AC-COM-36](community/permissions.md#ac-com-36), [AC-COM-38](community/servers.md#ac-com-38) | [TC-COM-02](community/servers.md#tc-com-02), [TC-COM-19](community/servers.md#tc-com-19), [TC-COM-23](community/integration.md#tc-com-23), [TC-COM-26](community/integration.md#tc-com-26); account guard, server/owner/@everyone/operation nguyên tử |
| [UC-COM-02](community/servers.md#uc-com-02) | GET /servers/search; GET /servers/{id} | [AC-COM-01](community/servers.md#ac-com-01), [AC-COM-19](community/invitations.md#ac-com-19), [AC-COM-37](community/servers.md#ac-com-37) | [TC-COM-01](community/servers.md#tc-com-01), [TC-COM-18](community/servers.md#tc-com-18); search key/cursor/visibility |
| [UC-COM-03](community/servers.md#uc-com-03) | GET /servers; GET /servers/{id}; GET /servers/{id}/membership/me | [AC-COM-06](community/permissions.md#ac-com-06), [AC-COM-19](community/invitations.md#ac-com-19), [AC-COM-21](community/memberships.md#ac-com-21), [AC-COM-35](community/memberships.md#ac-com-35) | [TC-COM-11](community/memberships.md#tc-com-11), [TC-COM-16](community/memberships.md#tc-com-16), [TC-COM-22](community/memberships.md#tc-com-22); cần bổ sung assertion list/detail/own status sau left |
| [UC-COM-04](community/servers.md#uc-com-04) | PATCH /servers/{id} | [AC-COM-29](community/servers.md#ac-com-29), [AC-COM-38](community/servers.md#ac-com-38), [AC-COM-39](community/servers.md#ac-com-39), [AC-COM-40](community/servers.md#ac-com-40) | [TC-COM-02](community/servers.md#tc-com-02), [TC-COM-19](community/servers.md#tc-com-19), [TC-COM-20](community/servers.md#tc-com-20), [TC-COM-27](community/servers.md#tc-com-27); private switch tranh approve/cancel |
| [UC-COM-05](community/servers.md#uc-com-05) | PATCH /servers/{id}/join-mode | [AC-COM-08](community/servers.md#ac-com-08) | [TC-COM-07](community/invitations.md#tc-com-07); quyền/version và ảnh hưởng request pending cần assertion riêng |
| [UC-COM-06](community/memberships.md#uc-com-06) | POST /servers/{id}/join → membership | [AC-COM-02](community/memberships.md#ac-com-02), [AC-COM-35](community/memberships.md#ac-com-35) | [TC-COM-03](community/memberships.md#tc-com-03), [TC-COM-16](community/memberships.md#tc-com-16), [TC-COM-22](community/memberships.md#tc-com-22); membership epoch/unique/role mặc định |
| [UC-COM-07](community/memberships.md#uc-com-07) | POST /servers/{id}/join → pending; GET .../join-requests/me; DELETE .../join-requests/{requestId} | [AC-COM-03](community/memberships.md#ac-com-03), [AC-COM-30](community/memberships.md#ac-com-30), [AC-COM-40](community/servers.md#ac-com-40) | [TC-COM-04](community/memberships.md#tc-com-04), [TC-COM-05](community/memberships.md#tc-com-05), [TC-COM-20](community/servers.md#tc-com-20); pending unique/transition |
| [UC-COM-08](community/memberships.md#uc-com-08) | GET .../join-requests; POST .../{requestId}/approve hoặc /reject | [AC-COM-10](community/memberships.md#ac-com-10), [AC-COM-30](community/memberships.md#ac-com-30), [AC-COM-40](community/servers.md#ac-com-40) | [TC-COM-04](community/memberships.md#tc-com-04), [TC-COM-05](community/memberships.md#tc-com-05), [TC-COM-20](community/servers.md#tc-com-20); guard actor/target và membership cùng commit |
| [UC-COM-09](community/invitations.md#uc-com-09) | GET/POST .../invites; GET .../invites/{inviteId}/link | [AC-COM-09](community/invitations.md#ac-com-09), [AC-COM-32](community/invitations.md#ac-com-32), [AC-COM-41](community/invitations.md#ac-com-41) | [TC-COM-07](community/invitations.md#tc-com-07), [TC-COM-21](community/invitations.md#tc-com-21), [TC-COM-23](community/integration.md#tc-com-23), [TC-COM-24](community/invitations.md#tc-com-24); secret/key ring/operation |
| [UC-COM-10](community/invitations.md#uc-com-10) | DELETE .../invites/{inviteId} | [AC-COM-20](community/invitations.md#ac-com-20), [AC-COM-32](community/invitations.md#ac-com-32), [AC-COM-41](community/invitations.md#ac-com-41) | [TC-COM-06](community/invitations.md#tc-com-06), [TC-COM-07](community/invitations.md#tc-com-07), [TC-COM-21](community/invitations.md#tc-com-21), [TC-COM-24](community/invitations.md#tc-com-24); revoke tranh join |
| [UC-COM-11](community/invitations.md#uc-com-11) | POST /invites/preview; POST /invites/join | [AC-COM-04](community/invitations.md#ac-com-04), [AC-COM-05](community/invitations.md#ac-com-05), [AC-COM-19](community/invitations.md#ac-com-19), [AC-COM-20](community/invitations.md#ac-com-20), [AC-COM-32](community/invitations.md#ac-com-32), [AC-COM-35](community/memberships.md#ac-com-35), [AC-COM-41](community/invitations.md#ac-com-41) | [TC-COM-06](community/invitations.md#tc-com-06), [TC-COM-13](community/invitations.md#tc-com-13), [TC-COM-16](community/memberships.md#tc-com-16), [TC-COM-21](community/invitations.md#tc-com-21), [TC-COM-24](community/invitations.md#tc-com-24); lượt cuối/rollback, pending joined_elsewhere |
| [UC-COM-12](community/invitations.md#uc-com-12) | GET/POST .../member-invitations; DELETE .../{invitationId} | [AC-COM-19](community/invitations.md#ac-com-19), [AC-COM-31](community/invitations.md#ac-com-31), [AC-COM-35](community/memberships.md#ac-com-35), [AC-COM-41](community/invitations.md#ac-com-41) | [TC-COM-08](community/invitations.md#tc-com-08), [TC-COM-17](community/invitations.md#tc-com-17), [TC-COM-21](community/invitations.md#tc-com-21), [TC-COM-23](community/integration.md#tc-com-23); unique pending/expiry/operation |
| [UC-COM-13](community/invitations.md#uc-com-13) | GET /member-invitations; POST .../{invitationId}/accept hoặc /reject | [AC-COM-19](community/invitations.md#ac-com-19), [AC-COM-31](community/invitations.md#ac-com-31), [AC-COM-35](community/memberships.md#ac-com-35), [AC-COM-41](community/invitations.md#ac-com-41) | [TC-COM-08](community/invitations.md#tc-com-08), [TC-COM-17](community/invitations.md#tc-com-17), [TC-COM-21](community/invitations.md#tc-com-21); đúng recipient/terminal/epoch |
| [UC-COM-14](community/servers.md#uc-com-14) | POST /servers/{id}/ownership-transfer; GET .../members | [AC-COM-33](community/servers.md#ac-com-33) | [TC-COM-14](community/servers.md#tc-com-14); Identity→server→membership lock order |
| [UC-COM-15](community/memberships.md#uc-com-15) | DELETE /servers/{id}/members/me | [AC-COM-21](community/memberships.md#ac-com-21), [AC-COM-35](community/memberships.md#ac-com-35) | [TC-COM-11](community/memberships.md#tc-com-11), [TC-COM-14](community/servers.md#tc-com-14), [TC-COM-16](community/memberships.md#tc-com-16), [TC-COM-22](community/memberships.md#tc-com-22); epoch/clear role/override/revocation |
| [UC-COM-16](community/channels.md#uc-com-16) | POST /servers/{id}/channels | [AC-COM-07](community/channels.md#ac-com-07), [AC-COM-11](community/permissions.md#ac-com-11), [AC-COM-38](community/servers.md#ac-com-38), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-01](community/permissions.md#tc-acl-01), [TC-ACL-06](community/permissions.md#tc-acl-06), [TC-ACL-08](community/permissions.md#tc-acl-08); [TC-COM-19](community/servers.md#tc-com-19), [TC-COM-23](community/integration.md#tc-com-23), [TC-COM-26](community/integration.md#tc-com-26), [TC-COM-27](community/servers.md#tc-com-27); lifecycle Messaging/Media |
| [UC-COM-17](community/channels.md#uc-com-17) | GET .../channels; GET .../channels/{channelId}; GET .../messages | [AC-COM-06](community/permissions.md#ac-com-06), [AC-COM-11](community/permissions.md#ac-com-11), [AC-COM-13](community/integration.md#ac-com-13), [AC-COM-14](community/channels.md#ac-com-14), [AC-COM-15](community/channels.md#ac-com-15), [AC-COM-25](community/permissions.md#ac-com-25), [AC-COM-26](community/permissions.md#ac-com-26), [AC-COM-27](community/permissions.md#ac-com-27), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-01](community/permissions.md#tc-acl-01), [TC-ACL-02](community/permissions.md#tc-acl-02), [TC-ACL-03](community/permissions.md#tc-acl-03), [TC-ACL-04](community/permissions.md#tc-acl-04), [TC-ACL-05](community/permissions.md#tc-acl-05), [TC-ACL-08](community/permissions.md#tc-acl-08), [TC-ACL-12](community/permissions.md#tc-acl-12); [TC-COM-09](community/channels.md#tc-com-09), [TC-COM-12](community/integration.md#tc-com-12), [TC-COM-25](community/integration.md#tc-com-25), [TC-COM-27](community/servers.md#tc-com-27); lịch sử theo guard/projection |
| [UC-COM-18](community/channels.md#uc-com-18) | PATCH .../channels/{channelId} | [AC-COM-34](community/channels.md#ac-com-34), [AC-COM-38](community/servers.md#ac-com-38), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-12](community/permissions.md#tc-acl-12); [TC-COM-19](community/servers.md#tc-com-19); cần assertion sửa tên/topic/version riêng |
| [UC-COM-19](community/channels.md#uc-com-19) | DELETE .../channels/{channelId} | [AC-COM-34](community/channels.md#ac-com-34), [AC-COM-39](community/servers.md#ac-com-39), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-12](community/permissions.md#tc-acl-12); [TC-COM-15](community/channels.md#tc-com-15), [TC-COM-25](community/integration.md#tc-com-25), [TC-COM-27](community/servers.md#tc-com-27); lifecycle/retention, cutoff media riêng |
| [UC-COM-20](community/permissions.md#uc-com-20) | GET/POST .../roles; PATCH/DELETE .../roles/{roleId} | [AC-COM-28](community/permissions.md#ac-com-28), [AC-COM-36](community/permissions.md#ac-com-36), [AC-COM-38](community/servers.md#ac-com-38) | [TC-ACL-06](community/permissions.md#tc-acl-06), [TC-ACL-08](community/permissions.md#tc-acl-08), [TC-ACL-09](community/permissions.md#tc-acl-09), [TC-ACL-11](community/permissions.md#tc-acl-11); [TC-COM-19](community/servers.md#tc-com-19), [TC-COM-23](community/integration.md#tc-com-23), [TC-COM-26](community/integration.md#tc-com-26); limit/system role/version |
| [UC-COM-21](community/permissions.md#uc-com-21) | GET .../members; GET/PUT .../members/{userId}/roles | [AC-COM-28](community/permissions.md#ac-com-28), [AC-COM-35](community/memberships.md#ac-com-35), [AC-COM-36](community/permissions.md#ac-com-36) | [TC-ACL-06](community/permissions.md#tc-acl-06), [TC-ACL-08](community/permissions.md#tc-acl-08); [TC-COM-16](community/memberships.md#tc-com-16), [TC-COM-22](community/memberships.md#tc-com-22), [TC-COM-25](community/integration.md#tc-com-25); đúng epoch/union quyền |
| [UC-COM-22](community/permissions.md#uc-com-22) | GET/PUT .../channels/{channelId}/access; GET .../roles; GET .../members | [AC-COM-11](community/permissions.md#ac-com-11), [AC-COM-16](community/permissions.md#ac-com-16), [AC-COM-25](community/permissions.md#ac-com-25), [AC-COM-26](community/permissions.md#ac-com-26), [AC-COM-27](community/permissions.md#ac-com-27), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-02](community/permissions.md#tc-acl-02), [TC-ACL-03](community/permissions.md#tc-acl-03), [TC-ACL-04](community/permissions.md#tc-acl-04), [TC-ACL-05](community/permissions.md#tc-acl-05), [TC-ACL-08](community/permissions.md#tc-acl-08), [TC-ACL-10](community/permissions.md#tc-acl-10), [TC-ACL-12](community/permissions.md#tc-acl-12); [TC-COM-22](community/memberships.md#tc-com-22), [TC-COM-25](community/integration.md#tc-com-25); ACL snapshot/guard |
| [UC-COM-23](community/integration.md#uc-com-23) | POST .../channels/{channelId}/messages | [AC-COM-12](community/integration.md#ac-com-12), [AC-COM-17](community/permissions.md#ac-com-17), [AC-COM-18](community/integration.md#ac-com-18), [AC-COM-22](community/integration.md#ac-com-22), [AC-COM-24](community/integration.md#ac-com-24) | [TC-COM-10](community/integration.md#tc-com-10), [TC-COM-12](community/integration.md#tc-com-12), [TC-COM-25](community/integration.md#tc-com-25), [TC-COM-27](community/servers.md#tc-com-27); TC-TEXT ở DM, HMAC/sequence/outbox |
| [UC-COM-24](community/integration.md#uc-com-24) | PATCH/DELETE .../messages/{messageId} | [AC-COM-12](community/integration.md#ac-com-12), [AC-COM-13](community/integration.md#ac-com-13), [AC-COM-23](community/integration.md#ac-com-23), [AC-COM-24](community/integration.md#ac-com-24) | [TC-COM-10](community/integration.md#tc-com-10); TC-TEXT ở DM, tác giả/version/tombstone |
| [UC-COM-25](community/integration.md#uc-com-25) | /hubs/chat; REST bù lịch sử và đọc lại resource | [AC-COM-06](community/permissions.md#ac-com-06), [AC-COM-11](community/permissions.md#ac-com-11), [AC-COM-13](community/integration.md#ac-com-13), [AC-COM-14](community/channels.md#ac-com-14), [AC-COM-18](community/integration.md#ac-com-18), [AC-COM-21](community/memberships.md#ac-com-21), [AC-COM-25](community/permissions.md#ac-com-25), [AC-COM-26](community/permissions.md#ac-com-26), [AC-COM-34](community/channels.md#ac-com-34), [AC-COM-35](community/memberships.md#ac-com-35), [AC-COM-42](community/permissions.md#ac-com-42) | [TC-ACL-07](community/permissions.md#tc-acl-07), [TC-ACL-11](community/permissions.md#tc-acl-11); [TC-COM-09](community/channels.md#tc-com-09), [TC-COM-11](community/memberships.md#tc-com-11), [TC-COM-15](community/channels.md#tc-com-15), [TC-COM-22](community/memberships.md#tc-com-22), [TC-COM-25](community/integration.md#tc-com-25); cần proof dispatch/reconnect/notification routing và DEC-083 |

42 AC-COM và 42 quy tắc COM đều được truy vết; [COM-009](community/integration.md#com-009), [COM-039](community/servers.md#com-039) có phần giới hạn scope. Các TC hiện có cần bổ sung assertion cho từng endpoint/nhánh khi triển khai, đặc biệt [UC-COM-03](community/servers.md#uc-com-03), [UC-COM-05](community/servers.md#uc-com-05), [UC-COM-18](community/channels.md#uc-com-18) và định tuyến thông báo [UC-COM-25](community/integration.md#uc-com-25). [TC-COM-23](community/integration.md#tc-com-23), [TC-COM-26](community/integration.md#tc-com-26) cùng fixtures operation/quyền kiểm chứng cơ chế dùng chung; fixture khớp không chứng minh UC/API đã đạt. Phương pháp ghi kết quả và đo tải/thu hồi theo [nghiệm thu](../release-operations.md#testing).

<a id="ux"></a>

## Giao diện

Màn hình và trạng thái theo [Servers](community/servers.md#ux), [Memberships](community/memberships.md#ux), [Invitations](community/invitations.md#ux), [Channels](community/channels.md#ux) và [Permissions](community/permissions.md#ux). Phạm vi trình duyệt, màn hình hẹp và trạng thái chung tại [tích hợp UX](community/integration.md#ux).

<a id="contracts"></a>

## Hợp đồng và thiết kế

[OpenAPI Community](../contracts/community.openapi.json) giữ schema máy đọc được; [schema realtime](../contracts/community-realtime.schema.json) giữ catalogue Hub chat. Các route/mutation theo từng thành phần trong đặc tả của phần đó.

<a id="detailed-design"></a>

[Thiết kế tích hợp](community/integration.md#contracts) quản lý ID/version, transaction/guard, operation retry, danh sách, lỗi và COM-SQL-01–10. [Permissions](community/permissions.md#detailed-design) giữ role/ACL snapshot; [Invitations](community/invitations.md#invite-secret) giữ token/link secret; [Servers](community/servers.md#search) giữ thiết kế search. Các artefact vẫn là mục tiêu, chưa có runtime API/Hub Community.

<a id="acceptance"></a>

## Tiêu chí chấp nhận

42 AC-COM giữ nguyên nội dung, được đặt ở thành phần chủ trì và dẫn chiếu từ [ma trận UC/API/AC/TC](#use-case-coverage).

| Mã | Nguồn chuẩn |
|---|---|
| [AC-COM-01](community/servers.md#ac-com-01) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-01) |
| [AC-COM-02](community/memberships.md#ac-com-02) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-02) |
| [AC-COM-03](community/memberships.md#ac-com-03) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-03) |
| [AC-COM-04](community/invitations.md#ac-com-04) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-04) |
| [AC-COM-05](community/invitations.md#ac-com-05) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-05) |
| [AC-COM-06](community/permissions.md#ac-com-06) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-06) |
| [AC-COM-07](community/channels.md#ac-com-07) | [Channels — Phòng và vòng đời phòng](community/channels.md#ac-com-07) |
| [AC-COM-08](community/servers.md#ac-com-08) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-08) |
| [AC-COM-09](community/invitations.md#ac-com-09) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-09) |
| [AC-COM-10](community/memberships.md#ac-com-10) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-10) |
| [AC-COM-11](community/permissions.md#ac-com-11) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-11) |
| [AC-COM-12](community/integration.md#ac-com-12) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-12) |
| [AC-COM-13](community/integration.md#ac-com-13) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-13) |
| [AC-COM-14](community/channels.md#ac-com-14) | [Channels — Phòng và vòng đời phòng](community/channels.md#ac-com-14) |
| [AC-COM-15](community/channels.md#ac-com-15) | [Channels — Phòng và vòng đời phòng](community/channels.md#ac-com-15) |
| [AC-COM-16](community/permissions.md#ac-com-16) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-16) |
| [AC-COM-17](community/permissions.md#ac-com-17) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-17) |
| [AC-COM-18](community/integration.md#ac-com-18) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-18) |
| [AC-COM-19](community/invitations.md#ac-com-19) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-19) |
| [AC-COM-20](community/invitations.md#ac-com-20) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-20) |
| [AC-COM-21](community/memberships.md#ac-com-21) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-21) |
| [AC-COM-22](community/integration.md#ac-com-22) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-22) |
| [AC-COM-23](community/integration.md#ac-com-23) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-23) |
| [AC-COM-24](community/integration.md#ac-com-24) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#ac-com-24) |
| [AC-COM-25](community/permissions.md#ac-com-25) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-25) |
| [AC-COM-26](community/permissions.md#ac-com-26) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-26) |
| [AC-COM-27](community/permissions.md#ac-com-27) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-27) |
| [AC-COM-28](community/permissions.md#ac-com-28) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-28) |
| [AC-COM-29](community/servers.md#ac-com-29) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-29) |
| [AC-COM-30](community/memberships.md#ac-com-30) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-30) |
| [AC-COM-31](community/invitations.md#ac-com-31) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-31) |
| [AC-COM-32](community/invitations.md#ac-com-32) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-32) |
| [AC-COM-33](community/servers.md#ac-com-33) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-33) |
| [AC-COM-34](community/channels.md#ac-com-34) | [Channels — Phòng và vòng đời phòng](community/channels.md#ac-com-34) |
| [AC-COM-35](community/memberships.md#ac-com-35) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#ac-com-35) |
| [AC-COM-36](community/permissions.md#ac-com-36) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-36) |
| [AC-COM-37](community/servers.md#ac-com-37) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-37) |
| [AC-COM-38](community/servers.md#ac-com-38) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-38) |
| [AC-COM-39](community/servers.md#ac-com-39) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-39) |
| [AC-COM-40](community/servers.md#ac-com-40) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#ac-com-40) |
| [AC-COM-41](community/invitations.md#ac-com-41) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#ac-com-41) |
| [AC-COM-42](community/permissions.md#ac-com-42) | [Permissions — Vai trò, ACL và kiểm tra quyền](community/permissions.md#ac-com-42) |

<a id="tests"></a>

## Ca kiểm thử

[TC-ACL-01–12](community/permissions.md#tests) thuộc Permissions. 27 TC-COM nằm ở nguồn chủ trì dưới đây; ca xuyên phần được dẫn chiếu cùng AC trong bảng UC. Tất cả vẫn chưa chạy. [Dữ liệu và cách ghi bằng chứng](community/integration.md#evidence) áp dụng chung.

| Mã | Nguồn chuẩn |
|---|---|
| [TC-COM-01](community/servers.md#tc-com-01) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-01) |
| [TC-COM-02](community/servers.md#tc-com-02) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-02) |
| [TC-COM-03](community/memberships.md#tc-com-03) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-03) |
| [TC-COM-04](community/memberships.md#tc-com-04) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-04) |
| [TC-COM-05](community/memberships.md#tc-com-05) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-05) |
| [TC-COM-06](community/invitations.md#tc-com-06) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-06) |
| [TC-COM-07](community/invitations.md#tc-com-07) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-07) |
| [TC-COM-08](community/invitations.md#tc-com-08) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-08) |
| [TC-COM-09](community/channels.md#tc-com-09) | [Channels — Phòng và vòng đời phòng](community/channels.md#tc-com-09) |
| [TC-COM-10](community/integration.md#tc-com-10) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#tc-com-10) |
| [TC-COM-11](community/memberships.md#tc-com-11) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-11) |
| [TC-COM-12](community/integration.md#tc-com-12) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#tc-com-12) |
| [TC-COM-13](community/invitations.md#tc-com-13) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-13) |
| [TC-COM-14](community/servers.md#tc-com-14) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-14) |
| [TC-COM-15](community/channels.md#tc-com-15) | [Channels — Phòng và vòng đời phòng](community/channels.md#tc-com-15) |
| [TC-COM-16](community/memberships.md#tc-com-16) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-16) |
| [TC-COM-17](community/invitations.md#tc-com-17) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-17) |
| [TC-COM-18](community/servers.md#tc-com-18) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-18) |
| [TC-COM-19](community/servers.md#tc-com-19) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-19) |
| [TC-COM-20](community/servers.md#tc-com-20) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-20) |
| [TC-COM-21](community/invitations.md#tc-com-21) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-21) |
| [TC-COM-22](community/memberships.md#tc-com-22) | [Memberships — Thành viên và yêu cầu tham gia](community/memberships.md#tc-com-22) |
| [TC-COM-23](community/integration.md#tc-com-23) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#tc-com-23) |
| [TC-COM-24](community/invitations.md#tc-com-24) | [Invitations — Link mời và lời mời đích danh](community/invitations.md#tc-com-24) |
| [TC-COM-25](community/integration.md#tc-com-25) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#tc-com-25) |
| [TC-COM-26](community/integration.md#tc-com-26) | [Community — Giao dịch và tích hợp dùng chung](community/integration.md#tc-com-26) |
| [TC-COM-27](community/servers.md#tc-com-27) | [Servers — Cộng đồng và chủ sở hữu](community/servers.md#tc-com-27) |

<a id="use-case-delivery"></a>

## Kế hoạch và gói triển khai đầu tiên

Áp dụng [quy trình dự án](../project.md#process) cho từng nhóm UC: rà soát luồng/ngoại lệ và AC/TC → đối chiếu UX, API, dữ liệu/giao dịch → xác định phụ thuộc và gói việc → triển khai cùng kiểm thử → tích hợp và ghi bằng chứng. Các bước được lặp theo nhóm chức năng; không đợi hoàn tất mọi module mới kiểm thử luồng đầu tiên.

| Nhóm | Use case | Đầu vào kỹ thuật cần có | Đầu ra cần kiểm chứng |
|---|---|---|---|
| 1. Server và tư cách | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-02](community/servers.md#uc-com-02), [UC-COM-03](community/servers.md#uc-com-03), [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-06](community/memberships.md#uc-com-06) | Account guard, shared transaction, migration server/membership/@everyone/version/operation và search | Tạo nguyên tử, join trực tiếp, list/detail đúng quyền và tên Unicode; private switch và đóng pending của [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-06](community/memberships.md#uc-com-06) hoàn thiện cùng request ở nhóm 3 |
| 2. Vai trò và phòng | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-19](community/channels.md#uc-com-19), [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21), [UC-COM-22](community/permissions.md#uc-com-22) | Permission evaluator/catalog, migration role/ACL/epoch, channel guard và Messaging lifecycle | Role limit, assignment/ACL nguyên tử, hidden channel, create/delete và race quyền; voice cần Media lifecycle riêng |
| 3. Tham gia và lời mời | [UC-COM-05](community/servers.md#uc-com-05), [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-08](community/memberships.md#uc-com-08), [UC-COM-09](community/invitations.md#uc-com-09), [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13), [UC-COM-14](community/servers.md#uc-com-14), [UC-COM-15](community/memberships.md#uc-com-15) | Role/quyền từ nhóm 2, request/invitation migration, token protection/key ring, guard actor/target | Pending/approve/cancel/private switch, lượt cuối, accept/expiry, transfer/leave/rejoin và retry |
| 4. Tin phòng và cập nhật | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24), [UC-COM-25](community/integration.md#uc-com-25) | Messaging writer/history dùng chung DM, outbox/Hub/registry/revoker và frontend API | Lưu bền/chống trùng/tác giả, reconnect/routing, thu hồi chat ≤5 giây, scope cache đúng epoch |

Các nhóm gồm API và trạng thái frontend tương ứng, có kiểm thử quyền/đồng thời ngay trong gói. Hiện các guard/lifecycle/shared transaction và migrations còn cần triển khai theo COM-SQL-01–10; scope room deleted/restore/media và các đầu vào chưa chốt tiếp tục được theo dõi ở [vấn đề còn mở](#gaps), không đánh dấu đã nghiệm thu từ danh mục UC.

### Gói triển khai đầu tiên

Mục tiêu gói đầu: [UC-COM-01](community/servers.md#uc-com-01) và [UC-COM-03](community/servers.md#uc-com-03) — tạo server, xem danh sách/detail và membership của chính mình. Gói tiếp theo bổ sung [UC-COM-06](community/memberships.md#uc-com-06) tham gia trực tiếp để kiểm thử hai thành viên và chuẩn bị role/phòng. Search, sửa metadata/private switch, transfer và requests hoàn thiện trong các gói tương ứng; một UC chỉ hoàn tất khi đủ các nhánh đã đặc tả.

| Bước | Việc cần làm | Kết quả review/kiểm chứng |
|---|---|---|
| 1. Thiết kế lát cắt | Đối chiếu OpenAPI create/list/detail/own membership với UC, DTO/lỗi/actor; chốt account guard và shared transaction | Các phần chỉ đọc dữ liệu sở hữu, quyền tới commit và lock order rõ |
| 2. Nền persistence | Migration additive server visibility/join mode/access version, membershipId/version, @everyone và operation dedup; mapping CommunityDbContext | Có preflight dữ liệu legacy, một nguồn tăng version và rollback an toàn |
| 3. Application/API | Tạo Servers/Memberships/Permissions Domain cần cho lát cắt; handlers create/read và DI/controllers theo cấu trúc đã thống nhất | Tạo server+owner+@everyone+operation nguyên tử; list/detail theo membership hiện hành |
| 4. Kiểm thử và UI | API integration với PostgreSQL; nối tạo/xem server với trạng thái frontend | Chưa xác minh/phiên sai bị chặn; private nonmember 404; retry create không trùng; rollback không để server dở; status đọc không cấp lại membership |

Evaluator Permissions được triển khai ở mức cần cho gói hiện hành và đối chiếu fixture khi mở role/ACL. Cơ chế outbox/revoker cho các mutation thu hồi phải hoàn thiện trước khi gói đó được coi đạt; tin phòng/realtime/Media có bằng chứng riêng theo phụ thuộc.

<a id="gaps"></a>

## Việc còn lại

Nội dung OQ và ACL-O giữ nguyên tại đặc tả chủ trì: [Servers](community/servers.md#gaps), [Memberships](community/memberships.md#gaps), [Invitations](community/invitations.md#gaps), [Channels](community/channels.md#gaps), [Permissions](community/permissions.md#gaps), [tích hợp](community/integration.md#gaps). Việc tách tài liệu không thay trạng thái Foundation hoặc đóng các OQ; vẫn cần review/migration/mock/proof và kết quả nghiệm thu.

Các quyết định DEC-* tiếp tục quản lý tại [sổ quyết định](../decisions.md#decisions); readiness và trách nhiệm tại [dự án](../project.md#readiness).

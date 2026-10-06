# SCDC — Cộng đồng, phòng và phân quyền

Cập nhật: 2026-10-07. Phạm vi: REQ-002/004, SCP-003/004 và phần tin phòng của SCP-005. Quy tắc COM, ACL-06–20, use case UC-COM, tiêu chí AC-COM, màn hình COM-S và ca TC-COM/TC-ACL.

Tài liệu này tổ chức theo **hành trình sử dụng cộng đồng**: tạo/tham gia, mở phòng và giao tiếp. Mã UC-COM dùng để truy vết hành trình; module thực hiện được ghi trong từng use case và [bảng phân công](#use-cases). Community sở hữu cộng đồng/thành viên/phòng/quyền; Messaging sở hữu tin nhắn và Hub chat. [Nguồn chuẩn và phối hợp liên module](community/integration.md#responsibilities) xác định nơi tra cứu mỗi loại quy tắc.

Đặc tả đầy đủ cho [v1](../releases/v1.md). [MVP](../releases/mvp.md) chọn các gói tạo/tham gia/phòng text để làm trước trong một API host; bắt đầu theo [gói đầu tiên](#use-case-delivery), không yêu cầu triển khai ngay cả 25 UC. Thiết kế tích hợp xuyên module được rà soát khi chuyển sang [microservice ở v1](../architecture.md#target) theo DEC-116.

Quy tắc tham gia/quyền cốt lõi đã xác nhận; các luồng DEC-072–077/087 và bổ sung vai trò/tìm kiếm/tên/phạm vi/visibility/lời mời/quản lý phòng DEC-092–098 được cụ thể hóa bên dưới. Thiết kế dữ liệu/API là bản dự thảo để rà soát. Community/Messaging mới có nền module; wireframe và ca kiểm thử chưa phải kết quả triển khai hoặc nghiệm thu.

Đã thống nhất tổ chức ngày 2026-10-06: năm thành phần nghiệp vụ nội bộ trong cùng SCDC.Community. Tài liệu chi tiết được chuyển về từng thành phần; trang này giữ scope, điều hướng, truy vết và kế hoạch. CommunityModule trên `main` vẫn đăng ký descriptor Foundation. Backend tạo/xem đã được triển khai riêng trên `feat/community-create-view`; [kết quả bước 4](community/create-view-backend.md) ghi phạm vi, bằng chứng và tình trạng Git.

## Mục lục

- [Phạm vi và hành trình](#requirements)
- [Năm thành phần và cấu trúc code](#organization)
- [Quyền và thu hồi](community/permissions.md#permissions)
- [Use case theo hành trình và module phụ trách](#use-cases)
- [Nguồn chuẩn và phối hợp liên module](community/integration.md#responsibilities)
- [Giao diện](#ux)
- [Hợp đồng và thiết kế](community/integration.md#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Kế hoạch và gói đầu tiên](#use-case-delivery)
- [Thiết kế kỹ thuật gói tạo/xem](community/create-view-design.md)
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

[Quy ước chung và tích hợp liên module](community/integration.md) quản lý transaction, ID/version/retry, migration, lỗi và các luồng phối hợp tin phòng/realtime. [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) do Messaging thực hiện; [UC-COM-25](community/integration.md#uc-com-25) phối hợp Identity, Community, Messaging và WebClient. Năm phần trong bảng trên là cấu trúc nội bộ của Community; luồng tích hợp được mô tả riêng với [module phụ trách và nguồn chuẩn](community/integration.md#responsibilities).

### Cấu trúc code mục tiêu

Source `main` hiện mới có nền project và CommunityModule đăng ký descriptor Foundation; chưa có các feature hoặc CommunityDbContext. Cây dưới đây là bố cục mục tiêu đã thống nhất. Nghiệp vụ đặt theo feature, Domain/Application ở trong từng phần; các phần cùng một assembly, schema community và CommunityDbContext.

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

## Use case theo hành trình cộng đồng và module phụ trách

Bổ sung ngày 2026-10-06. Các UC tổng hợp hành vi mục tiêu từ [quy tắc COM](#requirements), [ma trận ACL](community/permissions.md#permissions) và các quyết định đã dẫn chiếu; phần use case là bản dự thảo để rà soát trước triển khai. Community/Messaging trên `main` vẫn ở Foundation. [Backend gói đầu](community/create-view-backend.md) đã có implementation và kiểm thử riêng trên nhánh feature; phần UC còn lại và UI chưa được nghiệm thu. Thuật toán, lỗi, giao dịch và schema kỹ thuật tiếp tục được quản lý tại [thiết kế chi tiết](community/integration.md#detailed-design).

Community thực hiện quản lý server, membership, phòng, lời mời và quyền. [UC-COM-17](community/channels.md#uc-com-17) đọc lịch sử qua Messaging; [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24) do Messaging thực hiện trên quyền Community; [UC-COM-25](community/integration.md#uc-com-25) phối hợp Identity/Community/Messaging và WebClient. Chức năng vào phòng thoại/gọi/video thuộc [đặc tả media](voice-video.md), không được coi đã triển khai khi tạo được metadata phòng voice.

[Điều kiện và ngoại lệ dùng chung](community/integration.md#use-case-conditions) áp dụng cho toàn bộ UC; nội dung UC nằm ở thành phần chủ trì dưới đây.

Module phụ trách xác định phần triển khai nghiệp vụ, độc lập với vị trí lưu tài liệu và mã UC. Identity kiểm tra tài khoản/phiên, WebClient thực hiện giao diện; các phối hợp bổ sung được ghi tại từng UC. UC-COM-17 tách trách nhiệm metadata/phòng của Community và lịch sử tin của Messaging; UC-COM-25 là luồng tích hợp, Messaging phụ trách Hub/dispatcher còn mỗi module giữ điều kiện và dữ liệu mình sở hữu.

| Use case | Mục tiêu | Module phụ trách | Màn hình |
|---|---|---|---|
| <a id="uc-com-01"></a> [UC-COM-01](community/servers.md#uc-com-01) | Tạo cộng đồng | Community / Servers | COM-S10 |
| <a id="uc-com-02"></a> [UC-COM-02](community/servers.md#uc-com-02) | Tìm và xem cộng đồng công khai | Community / Servers | COM-S01/02 |
| <a id="uc-com-03"></a> [UC-COM-03](community/servers.md#uc-com-03) | Xem cộng đồng đang tham gia và tư cách của mình | Community / Servers | COM-S03, danh sách cộng đồng |
| <a id="uc-com-04"></a> [UC-COM-04](community/servers.md#uc-com-04) | Sửa thông tin và visibility cộng đồng | Community / Servers | COM-S10 |
| <a id="uc-com-05"></a> [UC-COM-05](community/servers.md#uc-com-05) | Đổi chế độ tham gia | Community / Servers | COM-S07 |
| <a id="uc-com-06"></a> [UC-COM-06](community/memberships.md#uc-com-06) | Tham gia cộng đồng công khai vào ngay | Community / Memberships | COM-S02 |
| <a id="uc-com-07"></a> [UC-COM-07](community/memberships.md#uc-com-07) | Gửi, xem và hủy yêu cầu tham gia | Community / Memberships | COM-S02/06 |
| <a id="uc-com-08"></a> [UC-COM-08](community/memberships.md#uc-com-08) | Duyệt hoặc từ chối yêu cầu tham gia | Community / Memberships | COM-S06 |
| <a id="uc-com-09"></a> [UC-COM-09](community/invitations.md#uc-com-09) | Tạo, xem và sao chép link mời | Community / Invitations | COM-S05 |
| <a id="uc-com-10"></a> [UC-COM-10](community/invitations.md#uc-com-10) | Thu hồi link mời | Community / Invitations | COM-S05 |
| <a id="uc-com-11"></a> [UC-COM-11](community/invitations.md#uc-com-11) | Xem trước và tham gia bằng link mời | Community / Invitations | COM-S02 |
| <a id="uc-com-12"></a> [UC-COM-12](community/invitations.md#uc-com-12) | Gửi, xem và hủy lời mời đích danh | Community / Invitations | COM-S11, quản lý lời mời |
| <a id="uc-com-13"></a> [UC-COM-13](community/invitations.md#uc-com-13) | Xem, chấp nhận hoặc từ chối lời mời đích danh | Community / Invitations | COM-S11, inbox người nhận |
| <a id="uc-com-14"></a> [UC-COM-14](community/servers.md#uc-com-14) | Chuyển chủ sở hữu | Community / Servers | COM-S12 |
| <a id="uc-com-15"></a> [UC-COM-15](community/memberships.md#uc-com-15) | Rời cộng đồng | Community / Memberships | COM-S03, menu cộng đồng |
| <a id="uc-com-16"></a> [UC-COM-16](community/channels.md#uc-com-16) | Tạo phòng | Community / Channels | COM-S04 |
| <a id="uc-com-17"></a> [UC-COM-17](community/channels.md#uc-com-17) | Xem phòng được phép và lịch sử tin văn bản | Community (phòng); Messaging (lịch sử) | COM-S03 |
| <a id="uc-com-18"></a> [UC-COM-18](community/channels.md#uc-com-18) | Sửa thông tin phòng | Community / Channels | Quản lý phòng từ COM-S03 |
| <a id="uc-com-19"></a> [UC-COM-19](community/channels.md#uc-com-19) | Xóa phòng | Community / Channels | Quản lý phòng từ COM-S03 |
| <a id="uc-com-20"></a> [UC-COM-20](community/permissions.md#uc-com-20) | Tạo, sửa và xóa vai trò tự tạo | Community / Permissions | COM-S08 |
| <a id="uc-com-21"></a> [UC-COM-21](community/permissions.md#uc-com-21) | Gán hoặc thu hồi vai trò thành viên | Community / Permissions | COM-S08 |
| <a id="uc-com-22"></a> [UC-COM-22](community/permissions.md#uc-com-22) | Xem và thay cấu hình quyền xem phòng | Community / Permissions | COM-S09 |
| <a id="uc-com-23"></a> [UC-COM-23](community/integration.md#uc-com-23) | Gửi và chủ động thử lại tin văn bản | Messaging | COM-S03, composer |
| <a id="uc-com-24"></a> [UC-COM-24](community/integration.md#uc-com-24) | Sửa hoặc xóa tin của mình | Messaging | COM-S03, menu tin |
| <a id="uc-com-25"></a> [UC-COM-25](community/integration.md#uc-com-25) | Nhận cập nhật, kết nối lại và xử lý mất quyền | Tích hợp liên module; Messaging (Hub/dispatcher) | COM-S03, Hub chat và thông báo theo người nhận |

<a id="use-case-rules"></a>

## Truy vết quy tắc cộng đồng

Giới hạn và thứ tự ưu tiên giữ ở bảng COM/ACL; bảng này chỉ xác định UC áp dụng. [COM-009](community/integration.md#com-009) và phần scope của [COM-039](community/servers.md#com-039) được ghi thành giới hạn, không tạo UC chức năng ngoài v1.

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
| [COM-039](community/servers.md#com-039) | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-20](community/permissions.md#uc-com-20) | Tên Unicode; xóa toàn server ngoài v1 |
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

Hai artefact tổng hợp hợp đồng của hành trình cộng đồng, gồm cả thao tác tin phòng do Messaging thực hiện. Prefix route `/servers/...` và mã COM/UC-COM phục vụ giao diện/truy vết; quyền sở hữu dữ liệu và nơi triển khai theo [bảng phân công](#use-cases), [ranh giới module](../architecture.md#boundaries).

<a id="detailed-design"></a>

[Thiết kế tích hợp](community/integration.md#contracts) quản lý ID/version, transaction/guard, operation retry, danh sách, lỗi và COM-SQL-01–10. [Permissions](community/permissions.md#detailed-design) giữ role/ACL snapshot; [Invitations](community/invitations.md#invite-secret) giữ token/link secret; [Servers](community/servers.md#search) giữ thiết kế search. Các artefact vẫn là mục tiêu cho toàn phạm vi; [backend gói đầu](community/create-view-backend.md) triển khai bốn route trên nhánh feature. Hub Community chưa có runtime.

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

Trước mỗi bước, trình bày phạm vi, đầu ra và nội dung cần quyết định để người dùng duyệt. Sau mỗi bước, báo những gì đã làm, kết quả kiểm tra và phần còn thiếu. Việc duyệt một bước chỉ áp dụng cho phạm vi bước đó.

Theo thỏa thuận ngày 2026-10-07, các phần đã hoàn thành và kiểm tra được commit/push theo tiến độ: tài liệu độc lập trên `main`, code trên nhánh theo gói chức năng (gói đầu là `feat/community-create-view`). Quyền commit/push không thay thế việc duyệt bước tiếp theo hoặc duyệt merge nhánh code.

Các nhóm dưới đây là lộ trình tổng thể; nhóm 1 được bắt đầu bằng [gói tạo/xem cộng đồng](#first-package), rồi bổ sung tìm kiếm, tham gia và chỉnh sửa theo các gói tiếp theo.

| Nhóm | Use case | Đầu vào kỹ thuật cần có | Đầu ra cần kiểm chứng |
|---|---|---|---|
| 1. Server và tư cách | [UC-COM-01](community/servers.md#uc-com-01), [UC-COM-02](community/servers.md#uc-com-02), [UC-COM-03](community/servers.md#uc-com-03), [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-06](community/memberships.md#uc-com-06) | Account guard, shared transaction, migration server/membership/@everyone/version/operation và search | Tạo nguyên tử, join trực tiếp, list/detail đúng quyền và tên Unicode; private switch và đóng pending của [UC-COM-04](community/servers.md#uc-com-04), [UC-COM-06](community/memberships.md#uc-com-06) hoàn thiện cùng request ở nhóm 3 |
| 2. Vai trò và phòng | [UC-COM-16](community/channels.md#uc-com-16), [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-18](community/channels.md#uc-com-18), [UC-COM-19](community/channels.md#uc-com-19), [UC-COM-20](community/permissions.md#uc-com-20), [UC-COM-21](community/permissions.md#uc-com-21), [UC-COM-22](community/permissions.md#uc-com-22) | Permission evaluator/catalog, migration role/ACL/epoch, channel guard và Messaging lifecycle | Role limit, assignment/ACL nguyên tử, hidden channel, create/delete và race quyền; voice cần Media lifecycle riêng |
| 3. Tham gia và lời mời | [UC-COM-05](community/servers.md#uc-com-05), [UC-COM-06](community/memberships.md#uc-com-06), [UC-COM-07](community/memberships.md#uc-com-07), [UC-COM-08](community/memberships.md#uc-com-08), [UC-COM-09](community/invitations.md#uc-com-09), [UC-COM-10](community/invitations.md#uc-com-10), [UC-COM-11](community/invitations.md#uc-com-11), [UC-COM-12](community/invitations.md#uc-com-12), [UC-COM-13](community/invitations.md#uc-com-13), [UC-COM-14](community/servers.md#uc-com-14), [UC-COM-15](community/memberships.md#uc-com-15) | Role/quyền từ nhóm 2, request/invitation migration, token protection/key ring, guard actor/target | Pending/approve/cancel/private switch, lượt cuối, accept/expiry, transfer/leave/rejoin và retry |
| 4. Tin phòng và cập nhật | [UC-COM-17](community/channels.md#uc-com-17), [UC-COM-23](community/integration.md#uc-com-23), [UC-COM-24](community/integration.md#uc-com-24), [UC-COM-25](community/integration.md#uc-com-25) | Messaging writer/history dùng chung DM, outbox/Hub/registry/revoker và frontend API | Lưu bền/chống trùng/tác giả, reconnect/routing, thu hồi chat ≤5 giây, scope cache đúng epoch |

Các nhóm gồm API và trạng thái frontend tương ứng, có kiểm thử quyền/đồng thời ngay trong gói. Hiện các guard/lifecycle/shared transaction và migrations còn cần triển khai theo COM-SQL-01–10; scope room deleted/restore/media và các đầu vào chưa chốt tiếp tục được theo dõi ở [vấn đề còn mở](#gaps), không đánh dấu đã nghiệm thu từ danh mục UC.

<a id="first-package"></a>

### Gói triển khai đầu tiên — tạo và xem cộng đồng

Ngày 2026-10-07, người dùng đồng ý bắt đầu bước 1 và chọn gói tạo cộng đồng, xem danh sách và xem chi tiết. Bước này xác định phạm vi và tiêu chí hoàn thành; các quyết định nghiệp vụ và thiết kế kỹ thuật được rà soát ở bước 2/3. Source Community vẫn Foundation, chưa có kết quả triển khai hoặc nghiệm thu gói.

Luồng cần bàn giao: **đăng nhập bằng tài khoản đủ điều kiện → tạo cộng đồng → mở chi tiết → thấy cộng đồng trong danh sách của mình → tải lại và vẫn đọc được dữ liệu đã lưu**.

| Phần trong gói | Phạm vi được chọn | Truy vết |
|---|---|---|
| Tạo cộng đồng | Tên, mô tả tùy chọn, public/private; người tạo trở thành owner và thành viên active; khởi tạo @everyone theo đặc tả. Tạo server chưa bao gồm tạo phòng đầu tiên. | [UC-COM-01](community/servers.md#uc-com-01) |
| Danh sách của mình | Chỉ cộng đồng có membership active của người dùng; phân trang và trạng thái chưa có cộng đồng. Đây là danh sách đã tham gia, không phải tìm kiếm cộng đồng công khai. | Phần danh sách của [UC-COM-03](community/servers.md#uc-com-03) |
| Chi tiết và tư cách của mình | Metadata cộng đồng, membership hiện hành và quyền quản lý hiệu lực; đọc dữ liệu không tạo hoặc phục hồi membership. | Phần đọc detail/tư cách của [UC-COM-03](community/servers.md#uc-com-03) |
| Giao diện và kiểm chứng | Form tạo, danh sách, chi tiết; trạng thái đang tải/rỗng/lỗi và thử lại thao tác chưa rõ kết quả; gọi API trực tiếp để kiểm tra điều kiện và quyền. | [UX Servers](community/servers.md#ux), [điều kiện chung](community/integration.md#use-case-conditions) |

UC-COM-03 là phạm vi bàn giao từng phần: tải phòng/lịch sử qua [UC-COM-17](community/channels.md#uc-com-17), đối soát sau leave/rejoin và thay đổi membership đồng thời được kiểm chứng khi triển khai các luồng tương ứng. Các nhánh này vẫn giữ nguyên trong đặc tả nguồn; hoàn thành gói đầu chưa đủ để đánh dấu toàn bộ UC-COM-03 đạt.

Phần để sau gói đầu: tìm kiếm/trải nghiệm khám phá công khai (UC-COM-02), sửa thông tin/chế độ tham gia (UC-COM-04/05), tham gia/rời và lời mời (UC-COM-06–15), phòng/vai trò/ACL (UC-COM-16–22), tin phòng và realtime (UC-COM-23–25). Route detail dùng chung vẫn kiểm chứng projection summary public theo [thiết kế gói](community/create-view-design.md#api), không coi toàn bộ UC-COM-02 đã triển khai. Ownership và @everyone cần cho việc tạo vẫn nằm trong gói đầu; giao diện quản lý vai trò nằm ở gói sau. Media thuộc v1 theo [phạm vi MVP](../releases/mvp.md). Việc để sau gói đầu không tự loại use case khỏi toàn bộ MVP.

### Tiêu chí hoàn thành gói đầu

Các dòng dưới đây chọn phần cần kiểm chứng từ đặc tả nguồn, không thay thế hoặc đánh dấu đạt toàn bộ AC/TC liên quan. Bước 2 rà soát nghiệp vụ và bổ sung tình huống cụ thể; bước 3 chốt request/response, lỗi và cách thử. Khi nghiệm thu, mỗi dòng cần có bằng chứng và kết quả riêng.

| Phần cần kiểm chứng | Kết quả cần quan sát | Nguồn đặc tả/ca kiểm thử |
|---|---|---|
| Tạo hợp lệ | Tài khoản đủ điều kiện tạo được public/private; đọc lại đúng metadata và trạng thái ban đầu đã chốt. | [UC-COM-01](community/servers.md#uc-com-01), phần tạo của [AC-COM-29](community/servers.md#ac-com-29)/[TC-COM-02](community/servers.md#tc-com-02) |
| Điều kiện tài khoản/phiên | Phiên không hợp lệ hoặc tài khoản không đủ điều kiện bị chặn cả khi gọi API trực tiếp. | [Điều kiện chung](community/integration.md#use-case-conditions), [COM-029](community/servers.md#com-029) |
| Dữ liệu biên | Chấp nhận/từ chối tên, mô tả và Unicode đúng quy tắc; lỗi trả về không để lại cộng đồng dở dang. | Phần tên server của [AC-COM-38](community/servers.md#ac-com-38)/[TC-COM-19](community/servers.md#tc-com-19), phần tạo của [AC-COM-29](community/servers.md#ac-com-29) |
| Tạo nguyên tử | Server, owner membership, @everyone và dữ liệu thao tác cùng commit; lỗi giữa chừng rollback toàn bộ phần tạo. | [UC-COM-01](community/servers.md#uc-com-01), [giao dịch](community/integration.md#transactions), phần tạo của [TC-COM-02](community/servers.md#tc-com-02) |
| Thử lại/đồng thời | Mất response rồi thử lại cùng khóa/payload chỉ có một server; cùng khóa khác payload bị từ chối theo hợp đồng. | [UC-COM-01](community/servers.md#uc-com-01), phần tạo server của [TC-COM-23](community/integration.md#tc-com-23) |
| Danh sách/detail/tư cách | Người tạo thấy cộng đồng, tư cách owner và quyền hiệu lực; danh sách rỗng/phân trang đúng; đọc không ghi hoặc phục hồi membership. | Phần được chọn của [UC-COM-03](community/servers.md#uc-com-03), [thiết kế danh sách](community/integration.md#contracts) |
| Quyền đọc | Tài khoản khác không nhận member detail hoặc tư cách của người tạo; người ngoài mở cộng đồng private nhận 404. | [UC-COM-02](community/servers.md#uc-com-02) (quy tắc che private), [UC-COM-03](community/servers.md#uc-com-03), [điều kiện chung](community/integration.md#use-case-conditions) |
| Luồng giao diện và lưu bền | Thực hiện trọn luồng qua UI/API với dữ liệu DB; reload hoặc restart API vẫn đọc được cộng đồng; trạng thái lỗi và thử lại đúng kết quả đã lưu. | [UX Servers](community/servers.md#ux), [điều kiện bàn giao MVP](../releases/mvp.md#acceptance) |

Dữ liệu kiểm chứng cần có: một tài khoản đủ điều kiện làm người tạo, một tài khoản đủ điều kiện chưa tham gia để kiểm tra quyền, tài khoản chưa xác minh và phiên không hợp lệ; cộng đồng public/private và đủ dữ liệu để kiểm tra phân trang. Đây là kế hoạch dữ liệu thử, chưa tạo tài khoản hoặc dữ liệu trong bước 1. Kết quả gói được ghi theo [mẫu nghiệm thu](../release-operations.md#testing), kèm bản build, thao tác API/UI, kết quả từng dòng và phần UC còn lại.

### Các bước thực hiện đã thống nhất

| Bước | Đầu ra cần bàn giao | Trạng thái |
|---|---|---|
| 1. Chốt phạm vi và thứ tự | Gói đầu UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03; tiêu chí hoàn thành, phần để sau và thứ tự phụ thuộc | Đã xác định phạm vi trong tài liệu ngày 2026-10-07 |
| 2. Rà soát nghiệp vụ gói đầu | [Kết quả rà soát](community/servers.md#first-package-business): điều kiện tạo, trạng thái ban đầu, ownership/membership/@everyone, dữ liệu hợp lệ, luồng lỗi và ngoại lệ; ghi riêng đầu vào kỹ thuật còn cần chốt | Đã rà soát theo các quyết định hiện có ngày 2026-10-07; chưa có kết quả chạy |
| 3. Chốt thiết kế kỹ thuật | [Thiết kế gói tạo/xem](community/create-view-design.md): model/schema/migration, API/DTO/lỗi, transaction/retry, hợp đồng Identity và kế hoạch kiểm thử | Đã đối chiếu source và xác định thiết kế ngày 2026-10-07; chưa có kết quả runtime |
| 4. Triển khai backend | Persistence, application, DI/API và kiểm thử quyền/tạo nguyên tử/thử lại/đọc dữ liệu | Đã triển khai trên `feat/community-create-view`; [bằng chứng backend](community/create-view-backend.md): 63 kiểm thử Release đạt, chưa merge/push |
| 5. Giao diện và nghiệm thu gói đầu | Nối form/danh sách/detail với API; chạy luồng thật và ghi bằng chứng theo tiêu chí gói | Chờ người dùng duyệt; backend đã có trên nhánh feature |
| 6. Mở rộng Community theo gói | Tiếp theo UC-COM-06 tham gia trực tiếp; bổ sung search/quản lý, role/quyền, phòng và các đường tham gia/rời/lời mời theo phụ thuộc. Phạm vi từng gói được duyệt riêng. | Chưa bắt đầu |
| 7. Tích hợp Messaging và realtime | Messaging lưu/đọc/gửi tin phòng trên quyền Community; sau đó kiểm chứng Hub, reconnect và xử lý mất quyền | Chưa bắt đầu |

Evaluator Permissions được triển khai ở mức cần cho gói hiện hành và đối chiếu fixture khi mở role/ACL. Cơ chế outbox/revoker cho các mutation thu hồi phải hoàn thiện trước khi gói đó được coi đạt; tin phòng/realtime/Media có bằng chứng riêng theo phụ thuộc.

<a id="gaps"></a>

## Việc còn lại

Nội dung OQ và ACL-O giữ nguyên tại đặc tả chủ trì: [Servers](community/servers.md#gaps), [Memberships](community/memberships.md#gaps), [Invitations](community/invitations.md#gaps), [Channels](community/channels.md#gaps), [Permissions](community/permissions.md#gaps), [tích hợp](community/integration.md#gaps). Việc tách tài liệu không thay trạng thái Foundation hoặc đóng các OQ; vẫn cần review/migration/mock/proof và kết quả nghiệm thu.

Các quyết định DEC-* tiếp tục quản lý tại [sổ quyết định](../decisions.md#decisions); readiness và trách nhiệm tại [dự án](../project.md#readiness).

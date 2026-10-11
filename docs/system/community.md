# Cấu trúc và cơ chế dùng chung Community

Trang này giữ tổ chức code và hợp đồng dùng chung của Community: điều kiện actor, ID/version/epoch, transaction/guard, operation, cursor, schema và migration. Quy tắc chức năng, UX riêng và trạng thái theo khả năng ở [các chủ đề Community](../features/community/README.md); nội dung tin/Hub ở [Messaging](../features/messaging/channel-messaging.md).

## Mục lục

- [Module phụ trách](#responsibilities)
- [Điều kiện chung](#use-case-conditions)
- [Tổ chức code](#organization)
- [Hợp đồng API](#contracts)
- [ID, tên và phiên bản](#detailed-design)
- [Giao dịch và guard](#transactions)
- [Operation và retry](#operations)
- [Danh sách](#collections)
- [Schema và migration](#schema-migration)
- [UX chung](#ux)
- [Kiểm chứng chung](#tests)

<a id="responsibilities"></a>

<a id="module-phụ-trách-và-nguồn-chuẩn"></a>

## Module phụ trách và nguồn chuẩn

Tài liệu được đặt theo hành trình người dùng. Mỗi use case ghi module thực hiện và các phần phối hợp; khi triển khai, mỗi module giữ dữ liệu và nghiệp vụ thuộc ranh giới của mình.

| Nội dung | Module phụ trách | Nguồn chuẩn |
|---|---|---|
| Cộng đồng, membership, lời mời, metadata/phân quyền phòng | Community | [Servers](../features/community/servers.md), [Memberships](../features/community/memberships.md), [Invitations](../features/community/invitations.md), [Channels](../features/community/channels.md), [Permissions](../features/community/access-control.md) |
| Nội dung tin, lưu/gửi/lịch sử, sửa/xóa, chống trùng, sequence/cursor và tombstone dùng chung | Messaging | [Cơ chế Messaging](../features/messaging/README.md), [thiết kế chi tiết](../features/messaging/README.md#detailed-design), [ca TC-TEXT](../features/messaging/text-policy.md#tests) |
| Áp dụng cơ chế nhắn tin vào phòng, kiểm tra view/membership, route và định tuyến cập nhật | Messaging phối hợp Community/Identity/WebClient | [UC-COM-23/24/25](../features/messaging/channel-messaging.md#use-cases), [tin phòng và realtime](../features/messaging/channel-messaging.md#channel-messaging), [OpenAPI](../contracts/community.openapi.json), [catalogue realtime](../contracts/community-realtime.schema.json) |
| Điều kiện tài khoản và phiên | Identity | [Accounts](../features/accounts/README.md); Community/Messaging dùng hợp đồng Identity |
| Giao diện, trạng thái gửi, cập nhật và đối soát sau reconnect | WebClient | [UX Community](#ux), [State/merge dùng chung](../features/messaging/text-policy.md) |

COM-013/014/017/018/022/023/024 và AC/TC của tin phòng được định nghĩa tại [Tin phòng](../features/messaging/channel-messaging.md). Cơ chế nhắn tin dùng nguồn Messaging; thứ tự quyền và thu hồi của Community ở [Permissions](../features/community/access-control.md#view-permissions).

UC-COM-17 tại [Channels](../features/community/channels.md#uc-com-17) mô tả một luồng mở phòng: Community trả danh sách/metadata theo view, Messaging trả lịch sử qua guard Community. UC-COM-23/24 có module thực hiện chính là Messaging. UC-COM-25 phối hợp các module: Messaging giữ Hub/dispatcher, Community cung cấp quyền/lifecycle và thay đổi cần thu hồi, Identity cung cấp trạng thái phiên, WebClient đồng bộ giao diện. DM giữ quyền truy cập riêng theo [đặc tả DM](../features/messaging/direct-messaging.md#permissions).

<a id="use-case-conditions"></a>

<a id="điều-kiện-và-ngoại-lệ-dùng-chung"></a>

### Điều kiện và ngoại lệ dùng chung

- Mọi actor dùng ứng dụng phải có tài khoản active, email đã xác minh và phiên hợp lệ theo DEC-051. Thao tác quản lý còn đòi membership active và quyền đúng thao tác ở thời điểm thực hiện; owner cũng chịu điều kiện nền. Hệ thống lấy actor từ phiên, không nhận actor tùy ý từ client.
- Luồng chính giả định dữ liệu hợp lệ và phụ thuộc sẵn sàng. Validation, trạng thái tài nguyên và quyền đều kiểm tra phía server theo COM/ACL. Private server không được biết hoặc phòng không được xem trả 404 theo hợp đồng mục tiêu; thiếu quyền quản lý trong scope được biết trả 403. Không trả metadata/nội dung bị ẩn trong lỗi hoặc thông báo.
- Các thao tác có version dùng version đã tải; ACL dùng accessVersion, leave/gán role/ngoại lệ dùng đúng membershipId. Xung đột trả 409 và yêu cầu đọc lại hiện trạng, không tự đổi version/epoch rồi ghi đè.
- Không tự phát lại mutation sau refresh, timeout hoặc mất kết nối. Khi kết quả không rõ, tải lại trạng thái trước khi người dùng quyết định tiếp. Riêng thao tác tạo giữ clientOperationId và payload ban đầu cho lần thử lại chủ động; gửi tin giữ clientMessageId theo thiết kế DM. Cùng khóa khác payload hoặc tài nguyên đã deleted/terminal không tạo lại tài nguyên bằng khóa cũ.
- Khi transaction rollback, không để lại dữ liệu nghiệp vụ dở dang hoặc phát cập nhật như đã thành công. Khi không xác nhận được quyền, dừng thao tác/phát nội dung theo hợp đồng; lỗi phụ thuộc không trở thành quyền truy cập.
- Thu hồi chat đo từ commit, deadline ≤5 giây theo DEC-083; HTTP tiếp theo kiểm tra quyền hiện hành. Media có proof riêng theo DEC-099. Vòng đời nội dung, cleanup và bảo vệ sau restore theo [data-lifecycle.md](data-lifecycle.md).
<a id="organization"></a>

<a id="năm-thành-phần-và-cấu-trúc-code"></a>

## Năm thành phần và cấu trúc code

| Thành phần | Trách nhiệm | Use case | Đặc tả |
|---|---|---|---|
| Servers | Metadata, search, visibility, join mode, ownership | [UC-COM-01](../features/community/servers.md#uc-com-01), [UC-COM-02](../features/community/servers.md#uc-com-02), [UC-COM-03](../features/community/servers.md#uc-com-03), [UC-COM-04](../features/community/servers.md#uc-com-04), [UC-COM-05](../features/community/servers.md#uc-com-05), [UC-COM-14](../features/community/servers.md#uc-com-14) | [Servers](../features/community/servers.md) |
| Memberships | Membership/epoch, join, requests, approve/reject/cancel, leave/rejoin | [UC-COM-06](../features/community/memberships.md#uc-com-06), [UC-COM-07](../features/community/memberships.md#uc-com-07), [UC-COM-08](../features/community/memberships.md#uc-com-08), [UC-COM-15](../features/community/memberships.md#uc-com-15) | [Memberships](../features/community/memberships.md) |
| Invitations | Link và mời đích danh, expiry/lượt/secret/transition | [UC-COM-09](../features/community/invitations.md#uc-com-09), [UC-COM-10](../features/community/invitations.md#uc-com-10), [UC-COM-11](../features/community/invitations.md#uc-com-11), [UC-COM-12](../features/community/invitations.md#uc-com-12), [UC-COM-13](../features/community/invitations.md#uc-com-13) | [Invitations](../features/community/invitations.md) |
| Channels | Metadata/kind/vòng đời phòng, danh sách theo view | [UC-COM-16](../features/community/channels.md#uc-com-16), [UC-COM-17](../features/community/channels.md#uc-com-17), [UC-COM-18](../features/community/channels.md#uc-com-18), [UC-COM-19](../features/community/channels.md#uc-com-19) | [Channels](../features/community/channels.md) |
| Permissions | Role, assignment, ACL, evaluator/checker/guard | [UC-COM-20](../features/community/access-control.md#uc-com-20), [UC-COM-21](../features/community/access-control.md#uc-com-21), [UC-COM-22](../features/community/access-control.md#uc-com-22) | [Permissions](../features/community/access-control.md) |

[Thiết kế tích hợp liên module](community.md) quản lý transaction, ID/version/retry, migration, lỗi và các luồng phối hợp tin phòng/realtime. [UC-COM-23](../features/messaging/channel-messaging.md#uc-com-23), [UC-COM-24](../features/messaging/channel-messaging.md#uc-com-24) do Messaging thực hiện; [UC-COM-25](../features/messaging/channel-messaging.md#uc-com-25) phối hợp Identity, Community, Messaging và WebClient. Năm phần trong bảng trên là cấu trúc nội bộ của Community; luồng tích hợp được mô tả riêng với [module phụ trách và nguồn chuẩn](#responsibilities).

<a id="cấu-trúc-code-mục-tiêu"></a>

### Cấu trúc code mục tiêu

Cây dưới đây là bố cục nội bộ đã thống nhất. Đối chiếu phần đang có trên từng nhánh tại [tiến độ](../features/community/README.md). Nghiệp vụ đặt theo feature, Domain/Application ở trong từng phần; các phần cùng một assembly, schema community và CommunityDbContext.

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

Controllers đặt tại services/SCDC.Api/Controllers/Community theo từng nhóm; test theo feature trong tests/SCDC.Api.Tests/Community. CommunityModule.cs là điểm ghép DI của module. Tạo file cùng chức năng thực tế, không đưa thư mục rỗng vào Git.

<a id="ranh-giới-và-phụ-thuộc"></a>

### Ranh giới và phụ thuộc

- Servers giữ owner_user_id là nguồn chuẩn duy nhất; Memberships kiểm tra target còn membership active khi transfer. Permissions đọc ownership để tính policy, không có bản owner riêng.
- Memberships cung cấp hành vi tạo/kích hoạt tư cách chung cho join, approve và accept. Invitations tiêu thụ lời mời/lượt cùng transaction với membership; không lặp một writer membership khác.
- Permissions Domain tính quyền trên snapshot và không gọi Application service các phần khác. Application orchestration phối hợp stores/policies; Infrastructure dựng snapshot/guard bằng dữ liệu Community. Tránh vòng gọi service giữa Channels, Memberships và Permissions.
- Các feature nội bộ dùng chung transaction/DbContext; chia thư mục không chia commit nghiệp vụ. Tạo server phải ghi owner/@everyone/operation cùng commit; accept lời mời phải ghi membership/lượt/transition cùng commit; xóa phòng phối hợp Messaging lifecycle.
- Giao tiếp Identity/Messaging/Media qua SCDC.Contracts, mỗi module chỉ đọc/ghi dữ liệu mình sở hữu. Cơ chế guard/lock order và snapshot được quản lý tại [thiết kế tích hợp](#transactions).

<a id="contracts"></a>

## Hợp đồng API

Prefix `/api/v1`; chỉ phần được chọn trong gói triển khai có bằng chứng runtime tương ứng. Actor lấy từ phiên, ID server UUIDv7 theo DEC-081; time UTC; request cập nhật dùng `expectedVersion` để tránh ghi đè. POST tạo server/channel/role/link/mời đích danh thêm `clientOperationId` UUIDv4; field đầy đủ ở OpenAPI. `ServerSummary` công khai gồm `id,name,description,visibility,joinMode,version`; chỉ thành viên được nhận chi tiết phòng/thành viên theo quyền.

[OpenAPI Community](../contracts/community.openapi.json) và [catalogue realtime](../contracts/community-realtime.schema.json) giữ schema máy đọc của hành trình, gồm cả tin phòng do Messaging thực hiện. Mỗi endpoint/sự kiện ghi actor/quyền, lỗi ProblemDetails, transaction, version và retry. Các route chưa có runtime vẫn được đánh dấu là mục tiêu; contract/fixture không phải bằng chứng thực thi.

<a id="detailed-design"></a>

<a id="danh-tính-tên-và-phiên-bản"></a>

### Danh tính, tên và phiên bản

Server/channel/role/request/invitation dùng UUIDv7; actor lấy từ phiên. Tên được trim, kiểm tra Unicode hợp lệ, độ dài UTF-16 và không chỉ trắng/vô hình theo bảng [text-policy](../fixtures/text-policy.json). Giữ cách viết/emoji của tên; không dùng tên làm định danh. Khóa so sánh dùng .NET NFC + ToLowerInvariant của tên đã trim, so sánh ordinal/DB collation C; tên cộng đồng không có unique constraint, tên phòng/vai trò unique trong server theo DEC-095. Nội dung tin vẫn không NFC/trim.

Giới hạn sau trim: server 2–100, channel 1–100, role 1–64 UTF-16; description/topic tối đa 1.000 UTF-16 theo DEC-072/077/093. Tên là một dòng, không control/NUL; description/topic là văn bản thuần, rỗng thành null. Validation client/server dùng cùng thứ tự chuẩn hóa. `slug` là cột nội bộ: writer tạo server sinh từ UUID, không thêm trường slug do người dùng nhập.

`version` của server/channel/role/membership/request/invitation truyền chuỗi số nguyên dương. Server có `accessVersion` tăng khi membership, role/assignment, ACL hoặc trạng thái truy cập đổi. Channel có `accessVersion` riêng cho cấu hình xem; PUT ACL dùng expectedAccessVersion, không lẫn version metadata. Client gửi đúng version đã tải, 409 thì đọc lại; không tự thay version rồi ghi đè.

Membership giữ unique `(serverId,userId)`. Lần tham gia đầu tạo `membershipId` UUIDv7/version 1; rejoin từ left tạo epoch mới và tăng version. Join lặp khi còn active giữ nguyên epoch/version. Gán role/ngoại lệ cá nhân gắn membershipId để cấu hình cũ không áp vào lần tham gia mới. Trạng thái v1 chỉ active/left; kicked/banned/timeout/nickname trong schema chưa là chức năng v1 được đặc tả.

<a id="transactions"></a>

<a id="giao-dịch-và-guard-dùng-chung"></a>

### Giao dịch và guard dùng chung

Thứ tự khóa: Identity user rows theo UUID network order → server → membership/role/request/invite/channel → Messaging space/message/operation → outbox. Lookup ban đầu chỉ định tuyến; đọc lại trạng thái/permission sau khi có khóa. Guard Identity kiểm tra actor session và tài khoản target khi cần; không JOIN Identity từ Community.

Phương án v1 tuần tự hóa mutation Community bằng `FOR UPDATE` trên server. `IChannelAccessGuard` giữ `FOR SHARE` trên server và channel tới commit của caller; lease trả epoch/access versions/hạn session, caller phải kiểm tra hạn ngay trước commit và trạng thái space/tác giả của Messaging. [Gói phòng/ACL](../features/community/channels.md) đã kiểm chứng role/ACL mutation chờ transaction đọc kết thúc và ACL revoke trước admission bị chặn. Writer tin/rời/xóa/Hub chưa có proof trong gói này. Cơ chế dùng [row lock PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS); `IChannelAccessChecker.CheckAsync` chỉ trả bool, không giữ transaction/lease.

Shared connection/transaction do BuildingBlocks quản lý, mỗi module chỉ đọc/ghi schema mình sở hữu. `IChannelAccessGuard` và phần create channel của `IChatSpaceLifecycle` đã được hợp nhất từ feat/community-channels vào `main`. Delete/Media lifecycle, `ICommunityAccessReader` và định tuyến thu hồi server/channel/membership còn là thiết kế mục tiêu. Server lock cần đo contention ở workload đã chốt; proof race không thay đo tải.

Quy tắc transition, pending và retry theo epoch được định nghĩa tại [Memberships](../features/community/memberships.md#technical) và [Invitations](../features/community/invitations.md#technical). Guard giữ điều kiện quyền đến commit; không thay các quy tắc riêng bằng một kết quả kiểm tra quyền trước transaction.

Các transaction riêng theo chức năng: [Servers](../features/community/servers.md#technical), [Memberships](../features/community/memberships.md#membership-contracts), [Invitations](../features/community/invitations.md#invite-contracts), [Channels](../features/community/channels.md#technical) và [Permissions](../features/community/access-control.md#permission-contracts).

<a id="operations"></a>

<a id="operation-fingerprint-và-retry"></a>

### Operation fingerprint và retry

Các POST tạo server/channel/role/link/mời đích danh có `clientOperationId` UUIDv4, copy payload một lần. Unique operation theo actor + kind + scope + client ID; fingerprint HMAC trên payload chuẩn hóa, không chứa token thô trong DB/audit. Retry cùng ID/payload trả resource hiện hành sau kiểm tra quyền; khác payload 409 OPERATION_CONFLICT; resource đã deleted/terminal không tạo lại bằng khóa cũ. Lưu operation metadata, không lưu toàn request có bí mật. Rotation/HMAC/key retention theo cơ chế ở DM.

Input fingerprint kỹ thuật v1: domain ASCII `SCDC.Community.Write.v1` + byte 0, operation kind ASCII + byte 0, UUID actor/scope/client theo network order, UInt32 big-endian độ dài canonical body byte, rồi body. Create-server dùng scope UUID toàn byte 0. Body lấy DTO đã normalize/default, không có clientOperationId và không serialize JSON. String là UInt32 big-endian độ dài UTF-8 + byte UTF-8; nullable có byte 0 cho null hoặc byte 1 + giá trị. Nullable số là marker rồi UInt32 big-endian. Name giữ cách viết sau trim; description/topic rỗng thành null.

| Operation kind | Thứ tự field trong canonical body | Phạm vi runtime |
|---|---|---|
| `create_server` | name string, description nullable string, visibility byte (public=1/private=2) | Đã triển khai |
| `create_channel` | name string, topic nullable string, kind byte (text=1/voice=2) | Đã triển khai text; voice là mục tiêu |
| `create_role` | name string, permission mask một byte; bit 0–4 theo thứ tự 5 code trong [danh mục quyền](../features/community/access-control.md#permission-detailed-design) | Đã triển khai |
| `create_invite` | expiresInSeconds nullable UInt32 (default 604800), maxUses nullable UInt32 | Thiết kế mục tiêu |
| `create_member_invitation` | recipientUserId UUID 16 byte network order | Thiết kế mục tiêu |

Mask chỉ biểu diễn fingerprint; wire vẫn là permission array, không thay thuật toán quyền. Duplicate/unknown permission bị validation trước fingerprint. [community-operations.json](../fixtures/community-operations.json) có byte/hash kỳ vọng và key giả, gồm Unicode/emoji, đổi thứ tự permission và default/null. Writer server/channel/role đã có bằng chứng trong [hồ sơ Community](../records/verification/community/README.md); hai operation lời mời còn cần writer và kiểm chứng riêng. Fixture không thay bằng chứng API.

Join/transition/transfer/leave/PUT/PATCH/DELETE không tự replay sau 401/timeout. Client dùng `retry:false` của api.js cho mutation; response không rõ thì tải trạng thái hiện hành, người dùng quyết định tiếp. GET có thể retry sau refresh. Leave lặp trả 204 khi đã left, nhưng phải có membershipId của lần tham gia đang rời để một request cũ không làm người dùng rời lần rejoin mới.

<a id="collections"></a>

<a id="danh-sách-và-quyền-được-biết"></a>

### Danh sách và quyền được biết

`GET /servers` chỉ server người gọi đang là member; `GET /servers/{id}` trả public summary hoặc member detail theo quyền hiện hành, private nonmember nhận 404 trừ preview bằng lời mời hợp lệ. `GET /servers/{id}/channels` chỉ phòng được xem; không có endpoint quản lý lộ metadata phòng bị ẩn theo DEC-098. Sửa/xóa/đọc hoặc thay ACL cần view hiện hành và management permission; actor tự làm mất view qua PUT thì nhận kết quả commit nhưng các request tiếp theo bị chặn. Roster phục vụ role/ngoại lệ chỉ trả user summary/membership cho owner/manage_channel_access. Danh sách pending/mời/role chỉ đúng actor/quyền, không broadcast toàn server.

Phân trang collection quản lý đề xuất mặc định 20/tối đa 50 theo cursor riêng actor/server/filter; inbox mời đích danh của người nhận và status yêu cầu của chính người gửi còn đọc được sau thay đổi visibility để giải thích trạng thái. Các danh sách đang thay đổi có dedup ID/refresh từ đầu; không hứa snapshot bất biến. GET/read cần một transaction snapshot với kiểm tra quyền Community nhất quán; auth session hiện hành vẫn kiểm tra từ Identity.

<a id="account-transactions"></a>

<a id="account-transaction-và-identity-guard"></a>

## Transaction và Identity guard

Chọn một connection PostgreSQL và transaction `READ COMMITTED` cho từng application operation. CommunityDbContext enlist transaction đó; `IAccountAccessGuard` trong Contracts nhận actor và `DbTransaction`, không phụ thuộc EF hoặc implementation Identity. Identity dùng connection thuộc transaction để khóa/kiểm tra schema mình; JWT DbContext của middleware không tự mở một transaction thứ hai cho guard. Không chạy query song song trên connection chung. EF Core hỗ trợ share connection và transaction giữa các context; bằng chứng shared transaction của gói tạo/phòng được dẫn từ hồ sơ kiểm chứng. [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Guard lấy `FOR SHARE` trên `identity.users`, rồi đọc lại user active/nondeleted, primary email verified, session thuộc user/chưa revoked/chưa hết hạn và stamp khớp claims. Các writer bảo mật hiện lấy `FOR NO KEY UPDATE` trên user trước khi sửa các record con; quy ước này phải được giữ cho writer mới. Share lock xung đột với no-key-update và giữ đến cuối transaction; race với writer bảo mật đã được kiểm chứng trong phạm vi gói tạo/xem; điều đó không chứng minh session revocation trên Hub. [PostgreSQL row locks](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS).

Khóa bảo vệ trạng thái có writer, không ngăn thời gian trôi. Guard giữ session expiry và kiểm tra thời gian lại ngay trước commit/hoàn tất read; nếu hết hạn thì dừng/rollback. Không kéo transaction qua tương tác người dùng hoặc gọi dịch vụ ngoài. Chọn lock timeout 2 giây, command timeout 5 giây và cancellation từ request; đây là cấu hình ban đầu để kiểm chứng, không phải kết quả đo tải.
<a id="security-keys"></a>

<a id="security-operation-cursor-và-khóa"></a>

## Operation, cursor và khóa

Operation dùng định dạng binary/HMAC-SHA256 và [fixture Community](../fixtures/community-operations.json) đã có ở [thiết kế operation](#operations). Scope của create server là UUID zero, duy nhất theo `(actor_user_id,kind,scope_id,client_operation_id)`. So hash constant-time. Record lưu `fingerprint_version=1`, `key_id`, hash 32 byte, resource ID và UTC; không lưu JWT hoặc toàn request. Resource ID của bảng operation chung được writer kiểm chứng cùng commit; không dùng FK đa hình giả sang mọi loại tài nguyên.

Khóa HMAC riêng cho Community, ngẫu nhiên tối thiểu 32 byte, cấu hình active key ID và tập key ID → key qua secret configuration. Startup kiểm tra active key; không có khóa Development mặc định trong source. Retry dùng key cũ của record, rotation chỉ áp thao tác mới; giữ key cho record còn tồn tại và backup liên quan theo [quy tắc khóa Messaging](../features/messaging/message-lifecycle.md#uuid-fingerprint-và-khóa-giao-dịch). Gói không tự đặt TTL/xóa operation khi metadata retention còn mở.

### Khóa cursor và cấu hình

Data Protection có ApplicationName cố định theo môi trường và key-ring path/volume bền qua restart/deploy, tách khỏi HMAC key và JWT signing key. Cần cấu hình explicit vì key mặc định có thể chỉ nằm trong bộ nhớ khi không có nơi lưu phù hợp. [ASP.NET Core key management](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0). Hết hạn/tamper trả 400; key-ring không truy cập được trả 503, không giả cursor hợp lệ hoặc chuyển sang token không bảo vệ.

Cấu hình HMAC/key ring, runner, database riêng và lệnh chạy lại ở [hướng dẫn Community](../guides/community-development.md#configuration). API không tự migrate; SQL đã áp giữ nguyên checksum.

<a id="schema-migration"></a>

<a id="schema-và-migration"></a>

### Schema và migration

Bảng này đối chiếu baseline trước chuỗi migration Community với thiết kế đầy đủ, không mô tả schema hiện hành. `main` đã nhận ledger 001–004 từ `feat/community-channels` cho các gói đã chọn; phần triển khai và proof theo [status](../features/community/README.md) và [đối chiếu test](../records/verification/community/README.md). Requests/invitations/delete/realtime ngoài các gói đó tiếp tục là mục tiêu.

| Mã | Baseline main trước migration Community | Mapping/đầu việc thiết kế |
|---|---|---|
| <a id="com-sql-01"></a> COM-SQL-01 | servers chưa có visibility/join_mode; description varchar(500), slug bắt buộc | Thêm visibility/join_mode/access_version, description 1.000; slug nội bộ sinh từ ID; backfill visibility/join mode được rà soát theo dữ liệu thực |
| <a id="com-sql-02"></a> COM-SQL-02 | channel name regex slug, tối thiểu 2 ASCII; topic 500; visibility có read-only | Tên Unicode 1–100 UTF-16, topic 1.000; kind text/voice/default_view/deleted_at/version/access_version riêng; unique tên phòng đang active, ID đã deleted không khôi phục; không đưa read-only vào v1 |
| <a id="com-sql-03"></a> COM-SQL-03 | roles tên 50, unique generated lower; default index chỉ “tối đa một” | Role tên 64, key chuẩn hóa cùng service; đúng một @everyone tạo cùng server; system restrictions và giới hạn 20 dưới server lock |
| <a id="com-sql-04"></a> COM-SQL-04 | member_roles/user_overrides gắn user nhưng chưa có epoch | Thêm membership_id/version, FK theo epoch; clear assignment/override khi leave; chống ABA rejoin |
| <a id="com-sql-05"></a> COM-SQL-05 | Không có join_requests hoặc member_invitations | Thêm trạng thái/version/lý do/expiry, unique pending có điều kiện, membershipId đã tạo; transition có khóa và CAS |
| <a id="com-sql-06"></a> COM-SQL-06 | invites giữ hash/lượt/hạn, default_role_id có thể khác default | Thêm version/protected_token; không cấp custom role qua link; use_count/membership cùng transaction |
| <a id="com-sql-07"></a> COM-SQL-07 | channels FK sang Messaging space; baseline chưa có nghiệp vụ writer | Create/delete qua IChatSpaceLifecycle trong shared transaction; gói phòng đã có create, delete còn mục tiêu; Community không đọc/ghi bảng Messaging trực tiếp |
| <a id="com-sql-08"></a> COM-SQL-08 | Trigger servers/spaces tự tăng version; channels/roles chỉ touch timestamp | Một nguồn tăng version cho mỗi resource, tránh trigger và service cùng tăng; wire chuỗi số, accessVersion tăng đúng khi quyền đổi |
| <a id="com-sql-09"></a> COM-SQL-09 | Permission catalog/role overrides có thể chứa quyền ngoài v1 | Migrate danh mục 5 management codes + channel_view; chỉ channel_view nhận override allow/deny; không suy seed permissions thành tính năng |
| <a id="com-sql-10"></a> COM-SQL-10 | Không có operation dedup hoặc realtime dispatcher Community | Thêm operation key `(actor,kind,scope,client_id)` với scope không null (create-server dùng UUID zero), fingerprint/key/version/resource ID; outbox metadata/lease, revoker/guards; key ring bền và thử restart |

Nguồn hiện hành trên `main`: [schema.sql](../../database/postgres/schema.sql), [CommunityModule](../../services/Modules/Community/CommunityModule.cs), [IChannelAccessGuard](../../services/SCDC.Contracts/Community/IChannelAccessGuard.cs); revision code/migration của từng gói theo [các chủ đề Community](../features/community/README.md). Baseline trước migration còn trong fixture `tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql`. Trước bật unique key mới, dò collision Unicode/case và mapping legacy đã review; không tự đổi tên/xóa dữ liệu thật. Giữ tin/phòng deleted theo [vòng đời dữ liệu](data-lifecycle.md#deletion): không tự hết hạn tin DEC-070, phòng deleted giữ nội dung chưa hạn purge DEC-105; backup tuổi tối đa 30 ngày DEC-109. Role/override epoch cũ dọn theo rule membership, không phục hồi từ backup; migration không tự xóa nội dung.

<a id="errors"></a>

<a id="lỗi-và-kiểm-chứng"></a>

### Lỗi và kiểm chứng

| HTTP / mã đề xuất | UI/kết quả |
|---|---|
| 400 VALIDATION_FAILED / NAME_INVALID / ACCESS_CONFIG_INVALID | Giữ form, chỉ trường sai; không cắt Unicode/đổi dữ liệu âm thầm |
| 404 RESOURCE_NOT_FOUND | Dùng cho private/nonmember/hidden channel; không trả metadata bị ẩn |
| 403 PERMISSION_DENIED | Biết scope nhưng thiếu quyền quản lý/tác giả; dừng thao tác |
| 409 VERSION_CONFLICT / MEMBERSHIP_CHANGED | Tải lại version/epoch và quyền; không tự replay |
| 409 ROLE_LIMIT_REACHED / NAME_CONFLICT / SYSTEM_ROLE_IMMUTABLE | Giải thích đúng giới hạn/quy tắc đã chốt; không sửa @everyone |
| 409 REQUEST_NOT_PENDING / INVITATION_NOT_PENDING / OWNER_MUST_TRANSFER | Hiện trạng thái hiện hành, không tạo lại transition cũ |
| 400 INVITE_INVALID hoặc 409 INVITE_EXHAUSTED | Không cấp membership/lộ metadata private; preview không bảo đảm lượt |
| 503 KEY_UNAVAILABLE / ACCESS_CHECK_UNAVAILABLE | Dừng thao tác/phát nội dung; không bỏ qua guard hoặc tạo lại link |

Review contract/schema/fixture với UX và AC/TC trước tích hợp. Proof cần bao gồm lượt cuối, approve/cancel/accept tranh nhau, 21 role tạo đồng thời, stale epoch sau rejoin, role deny bị xóa, revoke tranh gửi/subscribe, owner transfer/leave và migration Unicode. Kết quả theo gói được quản lý tại [các chủ đề Community](../features/community/README.md); kiểm tra fixture chỉ đối chiếu giá trị thiết kế.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

Bộ đếm tên/mô tả/chủ đề dùng UTF-16 theo DEC-092/093 để thống nhất API; giới hạn và validation theo [COM-029](../features/community/servers.md#com-029), [COM-034](../features/community/channels.md#com-034), [COM-037](../features/community/access-control.md#com-037), [COM-038](../features/community/servers.md#com-038), [COM-039](../features/community/servers.md#com-039).

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-082 đã chốt desktop
Chrome/Edge/Firefox/Safari và Chrome Android/Safari iOS cho Community;
phiên bản/OS/thiết bị/build và bằng chứng khả dụng còn cần khóa tại OQ-007.
Media v1 cam kết trên desktop, điện thoại ở đợt sau.

Vg sở hữu UI Community và quyền/lifecycle phòng; Sáng cung cấp nền Messaging và đối chiếu hợp đồng tin phòng. Thái cung cấp dataset/bộ chạy kiểm tra theo [DEC-117](../project/planning.md#team). Các ca dưới đây mô tả yêu cầu; bằng chứng đã ghi nhận được dẫn chiếu từ [tiến độ](../features/community/README.md).

<a id="tests"></a>

## Kiểm chứng chung

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-23"></a> TC-COM-23 | POST create response mất; manual retry cùng/khác payload; resource sau đó deleted | Một resource cùng operation, payload khác conflict; không phục hồi resource deleted bằng retry | Hợp đồng operation |
| <a id="tc-com-26"></a> TC-COM-26 | Migration tên legacy/collision, system role/permission catalog/trigger version | Không đổi/xóa dữ liệu thật tự động; một nguồn tăng version và schema phù hợp writer | COM-SQL-01–10 |

Dữ liệu, test thực tế và giới hạn bằng chứng ở [hồ sơ Community](../records/verification/community/README.md); cách chạy ở [hướng dẫn Community](../guides/community-development.md).

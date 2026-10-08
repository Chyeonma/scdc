# SCDC — Thiết kế Integration

Thiết kế kỹ thuật của thành phần; quy tắc, use case và AC/TC ở [đặc tả](../specs/integration.md). Phần được triển khai/kiểm chứng hiện tại được quản lý tại [tiến độ](../status.md); các route và cơ chế ngoài gói đã kiểm chứng tiếp tục là thiết kế mục tiêu.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

OpenAPI và các cơ chế dưới đây mô tả thiết kế cho toàn hành trình. Thiết kế của bốn route gói đầu được chốt riêng tại [tạo/xem](create-view.md); tình trạng implementation và bằng chứng tại [tiến độ](../status.md).

| Nhóm | Dữ liệu/hành vi tối thiểu | Phụ thuộc |
|---|---|---|
| Cộng đồng | Tạo/sửa, công khai/riêng tư, chủ sở hữu, vòng đời | DEC-072/094/096; chưa có xóa toàn bộ cộng đồng trong v1 |
| Tìm/tham gia | Chỉ tìm công khai, vào ngay/chờ duyệt, trạng thái yêu cầu và tư cách thành viên | [COM-001](../specs/memberships.md#com-001), [COM-002](../specs/servers.md#com-002), [COM-003](../specs/servers.md#com-003), [COM-004](../specs/invitations.md#com-004), DEC-073; cần transaction chống request lặp |
| Lời mời | Tạo, thời hạn, kiểm tra, tiêu thụ và thu hồi; ghi thành viên nhất quán khi gọi lặp | [COM-005](../specs/invitations.md#com-005), [COM-010](../specs/invitations.md#com-010), [COM-020](../specs/invitations.md#com-020), [COM-032](../specs/invitations.md#com-032); cần tiêu thụ lượt nhất quán |
| Phòng | Tạo, danh sách theo quyền, chủ đề và cấu hình xem | [COM-007](../specs/channels.md#com-007), [COM-008](../specs/channels.md#com-008), [COM-012](../specs/channels.md#com-012); trường/quyền theo DEC-077; kiểm tra xóa và thu hồi đồng thời |
| Vai trò/quyền | Chủ sở hữu quản lý vai trò; mặc định, kế thừa/cho phép/từ chối, ngoại lệ cá nhân và version cấu hình | DEC-055–058, ACL-O1/O2 |
| Tin phòng | Lưu/lịch sử/sửa/xóa/thử lại; chỉ người có quyền, tác giả sửa/xóa | [COM-013](../specs/integration.md#com-013), [COM-014](../specs/integration.md#com-014), [COM-015](../specs/channels.md#com-015), [COM-016](../specs/permissions.md#com-016), [COM-017](../specs/integration.md#com-017), [COM-018](../specs/integration.md#com-018), [COM-022](../specs/integration.md#com-022), [COM-023](../specs/integration.md#com-023), [COM-024](../specs/integration.md#com-024); tham chiếu cơ chế tin DM |
| Thu hồi | Mất quyền hoặc rời phải chặn lần đọc/gửi tiếp và kết nối cập nhật theo ngưỡng đã chốt | ACL-O5, OQ-007 |

Prefix `/api/v1`; chỉ phần được chọn trong gói triển khai có bằng chứng runtime tương ứng. Actor lấy từ phiên, ID server UUIDv7 theo DEC-081; time UTC; request cập nhật dùng `expectedVersion` để tránh ghi đè. POST tạo server/channel/role/link/mời đích danh thêm `clientOperationId` UUIDv4; field đầy đủ ở OpenAPI. `ServerSummary` công khai gồm `id,name,description,visibility,joinMode,version`; chỉ thành viên được nhận chi tiết phòng/thành viên theo quyền.

Danh mục quyền quản lý tối thiểu: quản lý phòng, tạo/thu hồi mời, duyệt/từ chối yêu cầu, đổi join mode và cấu hình quyền xem phòng. Chỉ owner quản lý vai trò/chuyển ownership/sửa metadata server. @everyone không có quyền quản lý, tối đa 20 vai trò tự tạo theo DEC-092. Tên mã permission và phiên bản cấu hình cụ thể hóa bên dưới; không thêm quyền xóa tin người khác.

Unique membership `(serverId,userId)`, unique pending request và cập nhật có điều kiện bảo vệ join/approve/accept lặp. Chuyển owner khóa server và hai membership; target phải vẫn là thành viên active/verified tại commit. Mời có giới hạn khóa record, tạo membership và tăng lượt cùng transaction; rollback không tiêu lượt. Xóa phòng kiểm tra lại quyền và version, chuyển trạng thái deleted rồi phát sự kiện thu hồi sau commit; giữ tombstone để API cũ không phục hồi phòng.

Lỗi đề xuất: validation 400; thiếu phiên 401; trái quyền quản lý 403; tài nguyên không được biết 404; request/version/owner conflict 409; vượt limiter 429; phụ thuộc tạm lỗi 503. [OpenAPI cộng đồng](../../../contracts/community.openapi.json) bổ sung schema, role/access API và tin phòng; giữ phạm vi mục tiêu; đối chiếu route đã kiểm chứng tại [tiến độ](../status.md).

Mỗi endpoint/sự kiện cần schema request/response, actor/quyền, lỗi theo [ProblemDetails](../../../architecture.md#contracts), giao dịch, version và retry. Community sở hữu metadata/thành viên/quyền; Messaging sở hữu tin. Admission tin dùng `IChannelAccessGuard` trong transaction caller, không đọc/JOIN schema Community. Interface `IChannelAccessChecker` bool legacy không đủ bảo vệ race và chưa có implementation.

Cơ chế tin dùng chung được thiết kế tại [Messaging](../../../shared/messaging/README.md). Dữ liệu biên TC-TEXT áp dụng cả gửi/sửa tin phòng; không chép một phiên bản quy tắc ký tự hoặc chống trùng khác ở đây.

Phương án gốc ngày 2026-10-04 giữ thiết kế mục tiêu. Các gói đã triển khai được ghi tại [status.md](../status.md); [gói phòng/ACL](channels-access.md) chọn migration 004, view policy và hai contract guard/create lifecycle. [OpenAPI](../../../contracts/community.openapi.json) đánh dấu phạm vi từng route; [schema realtime](../../../contracts/community-realtime.schema.json), [fixture quyền](../../../fixtures/community-permissions.json) và [fixture fingerprint](../../../fixtures/community-operations.json) tiếp tục giữ hợp đồng/fixture mục tiêu. Không dùng artefact thiết kế để suy runtime ngoài gói đã kiểm chứng.

<a id="detailed-design"></a>

### Danh tính, tên và phiên bản

Server/channel/role/request/invitation dùng UUIDv7; actor lấy từ phiên. Tên được trim, kiểm tra Unicode hợp lệ, độ dài UTF-16 và không chỉ trắng/vô hình theo bảng [text-policy](../../../fixtures/text-policy.json). Giữ cách viết/emoji của tên; không dùng tên làm định danh. Khóa so sánh đề xuất là NFC + ToLowerInvariant của tên đã trim, so sánh ordinal/DB collation cố định; tên cộng đồng không có unique constraint, tên phòng/vai trò unique trong server theo DEC-095. Nội dung tin vẫn không NFC/trim.

Giới hạn sau trim: server 2–100, channel 1–100, role 1–64 UTF-16; description/topic tối đa 1.000 UTF-16 theo DEC-072/077/093. Đề xuất tên là một dòng, không control/NUL; description/topic là văn bản thuần, rỗng thành null. Validation client/server dùng cùng thứ tự chuẩn hóa. `slug` hiện bắt buộc trong SQL chỉ là cột nội bộ: writer có thể sinh từ UUID, không thêm trường slug do người dùng nhập.

`version` của server/channel/role/membership/request/invitation truyền chuỗi số nguyên dương. Server có `accessVersion` tăng khi membership, role/assignment, ACL hoặc trạng thái truy cập đổi. Channel có `accessVersion` riêng cho cấu hình xem; PUT ACL dùng expectedAccessVersion, không lẫn version metadata. Client gửi đúng version đã tải, 409 thì đọc lại; không tự thay version rồi ghi đè.

Membership giữ unique `(serverId,userId)` nhưng mỗi lần tham gia tạo `membershipId` UUIDv7 mới và tăng version; trạng thái chỉ active/left thuộc v1. Gán role/ngoại lệ cá nhân gắn membershipId để bản cấu hình cũ không áp nhầm khi người dùng rời rồi vào lại. Kicked/banned/timeout/nickname trong schema chưa là chức năng v1 được đặc tả.

<a id="transactions"></a>

### Giao dịch và guard dùng chung

Thứ tự khóa: Identity user rows theo UUID network order → server → membership/role/request/invite/channel → Messaging space/message/operation → outbox. Lookup ban đầu chỉ định tuyến; đọc lại trạng thái/permission sau khi có khóa. Guard Identity kiểm tra actor session và tài khoản target khi cần; không JOIN Identity từ Community.

Phương án v1 tuần tự hóa mutation Community bằng `FOR UPDATE` trên server. `IChannelAccessGuard` giữ `FOR SHARE` trên server và channel tới commit của caller; lease trả epoch/access versions/hạn session, caller phải kiểm tra hạn ngay trước commit và trạng thái space/tác giả của Messaging. [Gói phòng/ACL](channels-access.md) đã kiểm chứng role/ACL mutation chờ transaction đọc kết thúc và ACL revoke trước admission bị chặn. Writer tin/rời/xóa/Hub chưa có proof trong gói này. Cơ chế dùng [row lock PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS); `IChannelAccessChecker.CheckAsync` chỉ trả bool, không giữ transaction/lease.

Shared connection/transaction do BuildingBlocks quản lý, mỗi module chỉ đọc/ghi schema mình sở hữu. `IChannelAccessGuard` và phần create channel của `IChatSpaceLifecycle` có implementation trên feat/community-channels. Delete/Media lifecycle, `ICommunityAccessReader` và định tuyến thu hồi server/channel/membership còn là thiết kế mục tiêu. Server lock cần đo contention ở workload đã chốt; proof race không thay đo tải.

Máy chủ kiểm tra quyền ở thời điểm commit. Thiết kế unique một pending mỗi cặp user/cộng đồng, unique membership và cập nhật có điều kiện để hai thao tác duyệt/hủy không cùng thắng. Gọi lặp join/accept của người đã là thành viên trả tư cách hiện hành, không nhân đôi. Dùng lời mời link hợp lệ khi đang pending join phải kết thúc pending trong cùng giao dịch tạo membership; chi tiết lịch sử và retention thuộc OQ-011. Link có hạn/lượt theo DEC-075; mời đích danh hạn 7 ngày theo DEC-087. Khi link hết lượt, request join mới bị từ chối; người đã là thành viên không tiêu tốn lượt mới.

Các transaction riêng theo chức năng: [Servers](servers.md#contracts), [Memberships](memberships.md#contracts), [Invitations](invitations.md#contracts), [Channels](channels.md#contracts) và [Permissions](permissions.md#contracts).

<a id="operations"></a>

### Operation fingerprint và retry

Các POST tạo server/channel/role/link/mời đích danh có `clientOperationId` UUIDv4, copy payload một lần. Unique operation theo actor + kind + scope + client ID; fingerprint HMAC trên payload chuẩn hóa, không chứa token thô trong DB/audit. Retry cùng ID/payload trả resource hiện hành sau kiểm tra quyền; khác payload 409 OPERATION_CONFLICT; resource đã deleted/terminal không tạo lại bằng khóa cũ. Lưu operation metadata, không lưu toàn request có bí mật. Rotation/HMAC/key retention theo cơ chế ở DM.

Input fingerprint kỹ thuật v1: domain ASCII `SCDC.Community.Write.v1` + byte 0, operation kind ASCII + byte 0, UUID actor/scope/client theo network order, UInt32 big-endian độ dài canonical body byte, rồi body. Create-server dùng scope UUID toàn byte 0. Body lấy DTO đã normalize/default, không có clientOperationId và không serialize JSON. String là UInt32 big-endian độ dài UTF-8 + byte UTF-8; nullable có byte 0 cho null hoặc byte 1 + giá trị. Nullable số là marker rồi UInt32 big-endian. Name giữ cách viết sau trim; description/topic rỗng thành null.

| Operation kind | Thứ tự field trong canonical body |
|---|---|
| `create_server` | name string, description nullable string, visibility byte (public=1/private=2) |
| `create_channel` | name string, topic nullable string, kind byte (text=1/voice=2) |
| `create_role` | name string, permission mask một byte; bit 0–4 theo thứ tự 5 code trong [danh mục quyền](permissions.md#detailed-design) |
| `create_invite` | expiresInSeconds nullable UInt32 (default 604800), maxUses nullable UInt32 |
| `create_member_invitation` | recipientUserId UUID 16 byte network order |

Mask chỉ biểu diễn fingerprint; wire vẫn là permission array, không thay thuật toán quyền. Duplicate/unknown permission bị validation trước fingerprint. [community-operations.json](../../../fixtures/community-operations.json) có byte/hash kỳ vọng và key giả, gồm Unicode/emoji, đổi thứ tự permission và default/null. Cần đối chiếu writer .NET với fixture khi triển khai; chưa coi mô tả/fixture là proof API.

Join/transition/transfer/leave/PUT/PATCH/DELETE không tự replay sau 401/timeout. Client dùng `retry:false` của api.js cho mutation; response không rõ thì tải trạng thái hiện hành, người dùng quyết định tiếp. GET có thể retry sau refresh. Leave lặp trả 204 khi đã left, nhưng phải có membershipId của lần tham gia đang rời để một request cũ không làm người dùng rời lần rejoin mới.

<a id="collections"></a>

### Danh sách và quyền được biết

`GET /servers` chỉ server người gọi đang là member; `GET /servers/{id}` trả public summary hoặc member detail theo quyền hiện hành, private nonmember nhận 404 trừ preview bằng lời mời hợp lệ. `GET /servers/{id}/channels` chỉ phòng được xem; không có endpoint quản lý lộ metadata phòng bị ẩn theo DEC-098. Sửa/xóa/đọc hoặc thay ACL cần view hiện hành và management permission; actor tự làm mất view qua PUT thì nhận kết quả commit nhưng các request tiếp theo bị chặn. Roster phục vụ role/ngoại lệ chỉ trả user summary/membership cho owner/manage_channel_access. Danh sách pending/mời/role chỉ đúng actor/quyền, không broadcast toàn server.

Phân trang collection quản lý đề xuất mặc định 20/tối đa 50 theo cursor riêng actor/server/filter; inbox mời đích danh của người nhận và status yêu cầu của chính người gửi còn đọc được sau thay đổi visibility để giải thích trạng thái. Các danh sách đang thay đổi có dedup ID/refresh từ đầu; không hứa snapshot bất biến. GET/read cần một transaction snapshot với kiểm tra quyền Community nhất quán; auth session hiện hành vẫn kiểm tra từ Identity.

<a id="channel-messaging"></a>

### Tin phòng và realtime

REST tin text dùng `/servers/{id}/channels/{channelId}/messages` với GET/POST/PATCH/DELETE tương tự DM. Message wire dùng `channelId` thay `conversationId`; `sequence` theo Messaging space, fingerprint/send operation/tombstone/version và cursor/resume dùng chung thiết kế DM. Voice channel chỉ có media, chưa mở luồng text bên trong voice; kind không đổi bằng PATCH trong v1. Quyền quản lý/owner không thay kiểm tra tác giả khi sửa/xóa.

Hub vẫn `/hubs/chat`; schema riêng [community-realtime.schema.json](../../../contracts/community-realtime.schema.json) bổ sung SubscribeChannel/UnsubscribeChannel và ChannelMessageChanged. Subscribe kiểm tra phiên/membership/quyền hiện hành; registry gắn user/session/server/channel/membershipId/accessVersion. Dispatcher chỉ phát nội dung cho connection còn đủ điều kiện, reconciliation ≤1 giây và deadline chat ≤5 giây từ commit thu hồi theo DEC-083. Lỗi kiểm tra quyền dừng phát.

Thông báo `CommunityChanged` chỉ chứa eventId/serverId/accessVersion và yêu cầu UI tải lại metadata/quyền; không mang tên phòng bị ẩn, danh sách thành viên/role hay nội dung tin. `MembershipChanged`, `JoinRequestChanged`, `MemberInvitationChanged` định tuyến đến đúng người và manager còn quyền theo user/session, không cần người ngoài subscribe server. Server tự gỡ subscription khi mất quyền, không trông chờ client xử lý thông báo. Client merge version theo từng resource; sự kiện thu hồi có membershipId cũ không được xóa cache của epoch rejoin mới. Reconnect subscribe lại rồi REST bù trang/tải lại tin cũ để nhận edit/delete.

Mất quyền/rời/xóa phòng xóa cache/tin tạm của scope tương ứng và đóng composer; không ảnh hưởng DM. Draft tin phòng đề xuất dùng RAM tab cùng cơ chế DM, không lưu nội dung xuống browser storage; chưa có quyết định riêng nếu muốn lưu bền. Media nhận revocation từ cùng commit, thu hồi ≤5 giây/fail-close theo DEC-099; admission/lease/quota gate nằm trong [thiết kế media](../../voice-video/design/README.md#detailed-design), không tự coi chat proof là media proof. Voice channel đề xuất gọi IMediaRoomLifecycle tạo room DB rỗng cùng transaction, đánh dấu closing khi xóa; interface/migration chưa có, SFU provision/dừng sau commit.

<a id="schema-migration"></a>

### Schema và migration

Bảng này đối chiếu baseline code `main` trước chuỗi migration Community với thiết kế đầy đủ, không mô tả schema hiện hành của `feat/community-channels`. Feature đã có ledger 001–004 cho các gói đã chọn; phần triển khai và proof theo [status](../status.md) và [đối chiếu test](../delivery/verification.md). Requests/invitations/delete/realtime ngoài các gói đó tiếp tục là mục tiêu.

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

Nguồn baseline trên main: [schema.sql](../../../../database/postgres/schema.sql), [CommunityModule](../../../../services/Modules/Community/CommunityModule.cs), [IChannelAccessChecker](../../../../services/SCDC.Contracts/Community/IChannelAccessChecker.cs); revision code/migration được nghiệm thu trên feature theo [status.md](../status.md). Trước bật unique key mới, dò collision Unicode/case và mapping legacy đã review; không tự đổi tên/xóa dữ liệu thật. Giữ tin/phòng deleted theo [vòng đời dữ liệu](../../../data-lifecycle.md#deletion): không tự hết hạn tin DEC-070, phòng deleted giữ nội dung chưa hạn purge DEC-105; backup tuổi tối đa 30 ngày DEC-109. Role/override epoch cũ dọn theo rule membership, không phục hồi từ backup; migration không tự xóa nội dung.

<a id="errors"></a>

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

Review contract/schema/fixture với UX và AC/TC trước tích hợp. Proof cần bao gồm lượt cuối, approve/cancel/accept tranh nhau, 21 role tạo đồng thời, stale epoch sau rejoin, role deny bị xóa, revoke tranh gửi/subscribe, owner transfer/leave và migration Unicode. Kết quả theo gói được quản lý tại [status.md](../status.md); kiểm tra fixture chỉ đối chiếu giá trị thiết kế.

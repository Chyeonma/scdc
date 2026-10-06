# SCDC — Community — Giao dịch và tích hợp dùng chung

Cập nhật: 2026-10-06. Tài liệu dùng chung của năm thành phần Community. Quy tắc nghiệp vụ đã xác nhận theo các DEC dẫn chiếu; use case và thiết kế kỹ thuật là bản dự thảo để rà soát. Source còn Foundation, chưa có kết quả chạy UC-COM.

Nguồn chuẩn cho điều kiện dùng chung, ID/version, transaction/lock order, operation retry, migration, lỗi, tin phòng và realtime. Tin nhắn/Hub thuộc Messaging; tài liệu này mô tả phần phối hợp Community.

[Tổng quan và truy vết Community](../community.md#use-cases) · [Kế hoạch triển khai](../community.md#use-case-delivery).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Use case](#use-cases)
- [UX và trạng thái](#ux)
- [Thiết kế dữ liệu/API](#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

## Phạm vi và quy tắc

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| <a id="com-009"></a> COM-009 | Gửi file tài liệu trong phòng được xếp vào đợt sau. | DEC-023 |
| <a id="com-013"></a> COM-013 | Tin văn bản trong phòng được người gửi sửa và xóa như tin riêng: sửa bất cứ lúc nào với dấu “Đã sửa”, xóa cho mọi người với dòng thay thế; trạng thái gửi và thử lại áp dụng tương tự. | DEC-034 |
| <a id="com-014"></a> COM-014 | Tin đã được lưu trong phòng vẫn xem lại được khi thành viên có quyền mở phòng sau lúc vắng mặt. | DEC-035, SCP-005 |
| <a id="com-017"></a> COM-017 | Trong đợt đầu, mọi thành viên có quyền xem phòng đều được gửi tin văn bản trong phòng đó. | DEC-040 |
| <a id="com-018"></a> COM-018 | Thử lại cùng thao tác gửi trong phòng không tạo tin trùng. | DEC-037 |
| <a id="com-022"></a> COM-022 | Người gửi phải xác minh email trước khi gửi tin trong phòng. | DEC-041, SCDC-FR-ACC-001 |
| <a id="com-023"></a> COM-023 | Tin đã sửa chỉ giữ nội dung mới nhất; không cung cấp lịch sử bản cũ. | DEC-052, nguyên tắc tương tự DM tại DEC-034 |
| <a id="com-024"></a> COM-024 | Tin văn bản tối đa 2.000 đơn vị UTF-16 sau CRLF/CR → LF; cho xuống dòng/emoji; từ chối UTF-16 lỗi và tin rỗng/chỉ trắng hoặc vô hình. Dùng chung [quy tắc nội dung DM](../direct-messaging.md#detailed-design). | DEC-053, DEC-068, DEC-090 |

<a id="use-cases"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](../community.md#use-case-coverage).

<a id="uc-com-23"></a>

### UC-COM-23 — Gửi và chủ động thử lại tin văn bản

**Tác nhân:** Thành viên active có view phòng text, tài khoản đã xác minh và phiên hợp lệ.

**Điều kiện trước:** Phòng text active; điều kiện Identity/Community được giữ tới commit Messaging theo thiết kế guard.

**Kích hoạt:** Người dùng bấm gửi từ composer hoặc chủ động thử lại thao tác gửi chưa rõ kết quả.

**Luồng chính:**

1. Client giữ payload và clientMessageId của thao tác; Messaging chuẩn hóa/kiểm tra nội dung theo [COM-024](#com-024) và thiết kế DM.
2. Messaging kiểm tra quyền, xử lý operation chống trùng rồi lưu tin, sequence theo space và outbox trong cùng transaction.
3. Sau commit, trả tin hiện hành và hiển thị đã gửi. Khi người dùng thử lại cùng khóa/payload, trả cùng tin hiện hành, không tạo tin/outbox mới.

**Ngoại lệ:** Chưa xác minh/mất phiên/mất view/deleted/voice channel bị chặn. Cùng clientMessageId khác payload trả xung đột; thiếu fingerprint key dừng thao tác. Response mất giữ trạng thái chưa rõ/lỗi để người dùng đối soát hoặc thử lại chủ động. Revoke tranh send không chen giữa kiểm tra quyền và commit.

**Kết quả sau cùng:** Một tin cho mỗi thao tác hợp lệ, lưu bền trước trạng thái đã gửi. Nội dung và retry dùng cùng cơ chế DM; file và quyền chỉ đọc độc lập chưa thuộc tin phòng v1.

<a id="uc-com-24"></a>

### UC-COM-24 — Sửa hoặc xóa tin của mình

**Tác nhân:** Tác giả tin, còn view phòng text và đủ điều kiện ứng dụng.

**Điều kiện trước:** Phòng active, tin thuộc phòng; tác giả đã tải message version. Quyền tác giả và view kiểm tra lại khi thực hiện.

**Kích hoạt:** Tác giả chọn sửa hoặc xóa từ menu tin tại COM-S03.

**Luồng chính:**

1. Khi sửa, tác giả gửi nội dung mới và expectedVersion; Messaging kiểm tra validation dùng chung DM và quyền tác giả.
2. Khi xóa, tác giả gửi expectedVersion; Messaging bỏ content và giữ tombstone/ID/sequence cùng operation chống trùng.
3. Thay đổi tin/version/outbox commit nguyên tử; client đủ quyền nhận nội dung hiện hành cùng dấu đã sửa hoặc dòng thay thế tin đã xóa.

**Ngoại lệ:** Owner/manager không sửa/xóa tin người khác. Tin đã xóa không được sửa, stale version hoặc mất view yêu cầu đọc lại/chặn thao tác. Sửa không hợp lệ giữ form; response không rõ không tự replay mutation. Nội dung cũ không được trả lại từ receipt của thao tác gửi.

**Kết quả sau cùng:** Sửa chỉ giữ nội dung mới nhất; xóa không làm mất khóa chống trùng hoặc phục hồi tin khi retry send. Lịch sử bản sửa cũ không được cung cấp.

<a id="uc-com-25"></a>

### UC-COM-25 — Nhận cập nhật, kết nối lại và xử lý mất quyền

**Tác nhân:** Người dùng đang sử dụng Community; client và hệ thống realtime hỗ trợ đồng bộ/thu hồi.

**Điều kiện trước:** Subscribe phòng cần actor/membership/view hiện hành. Thông báo request/invitation định tuyến theo đúng user/session và manager còn quyền, không đòi người ngoài subscribe server.

**Kích hoạt:** Mở phòng, có sự kiện đã commit, kết nối lại hoặc thay đổi quyền/phiên/membership/phòng.

**Luồng chính:**

1. Client subscribe phòng text qua Hub chat; server kiểm tra điều kiện và gắn subscription với session, server/channel, membershipId và accessVersion.
2. Dispatcher phát bản tin hiện hành cho connection còn quyền; thông báo CommunityChanged chỉ yêu cầu tải lại metadata/quyền. Request/invitation/membership gửi đúng đối tượng được biết.
3. Khi reconnect, kiểm tra quyền và subscribe lại rồi REST bù tin, tải lại trang cũ để nhận edit/delete; merge theo ID/version và chỉ tiến resume cursor sau khi merge đủ trang.
4. Khi quyền bị thu hồi, server tự gỡ subscription/chặn nội dung mới; client đóng nội dung/composer và dọn cache/draft của scope. Thu hồi chat được kiểm chứng trong ≤5 giây từ commit.

**Ngoại lệ:** Mất quyền/phiên không được reconnect vào scope cũ; kiểm tra quyền lỗi thì dừng phát. Sự kiện trùng/đảo thứ tự không nhân tin hoặc hạ version. Revoke membershipId cũ không xóa cache epoch rejoin mới. Client không hợp tác vẫn bị server ngừng phát; không broadcast roster/ACL/request/invitation cho toàn server.

**Kết quả sau cùng:** Client đủ quyền hội tụ về trạng thái đã lưu, scope mất quyền không tiếp tục nhận nội dung; DM và phòng khác vẫn theo quyền riêng. Cutoff media được kiểm chứng riêng theo đặc tả Media.

<a id="ux"></a>

## UX và trạng thái

Bộ đếm tên/mô tả/chủ đề dùng UTF-16 theo DEC-092/093 để thống nhất API; giới hạn và validation theo [COM-029](servers.md#com-029), [COM-034](channels.md#com-034), [COM-037](permissions.md#com-037), [COM-038](servers.md#com-038), [COM-039](servers.md#com-039).

Đề xuất màn hình hẹp dùng lần lượt danh sách cộng đồng → danh sách
phòng → hội thoại; quản lý mở thành trang riêng. DEC-082 đã chốt desktop
Chrome/Edge/Firefox/Safari và Chrome Android/Safari iOS cho Community;
phiên bản/OS/thiết bị/build và bằng chứng khả dụng còn cần khóa tại OQ-007.
Media v1 cam kết trên desktop, điện thoại ở đợt sau.

Vg sở hữu UI Community và quyền/lifecycle phòng; Sáng cung cấp nền Messaging và đối chiếu hợp đồng tin phòng. Thái cung cấp dataset/bộ chạy kiểm tra theo [DEC-117](../../project.md#team). Chưa có prototype, kết quả rà soát hoặc kiểm thử khả dụng được ghi nhận trong tài liệu này.

<a id="contracts"></a>

## Thiết kế dữ liệu/API

Repo có schema `community`/`messaging` và seed, nhưng chưa có controller/service hoặc Swagger runtime nghiệp vụ Community. OpenAPI mục tiêu và thiết kế dưới đây đã được bổ sung; wireframe/schema không tự chứng minh endpoint hoạt động.

| Nhóm | Dữ liệu/hành vi tối thiểu | Phụ thuộc |
|---|---|---|
| Cộng đồng | Tạo/sửa, công khai/riêng tư, chủ sở hữu, vòng đời | DEC-072/094/096; chưa có xóa toàn bộ cộng đồng trong v1 |
| Tìm/tham gia | Chỉ tìm công khai, vào ngay/chờ duyệt, trạng thái yêu cầu và tư cách thành viên | [COM-001](memberships.md#com-001), [COM-002](servers.md#com-002), [COM-003](servers.md#com-003), [COM-004](invitations.md#com-004), DEC-073; cần transaction chống request lặp |
| Lời mời | Tạo, thời hạn, kiểm tra, tiêu thụ và thu hồi; ghi thành viên nhất quán khi gọi lặp | [COM-005](invitations.md#com-005), [COM-010](invitations.md#com-010), [COM-020](invitations.md#com-020), [COM-032](invitations.md#com-032); cần tiêu thụ lượt nhất quán |
| Phòng | Tạo, danh sách theo quyền, chủ đề và cấu hình xem | [COM-007](channels.md#com-007), [COM-008](channels.md#com-008), [COM-012](channels.md#com-012); trường/quyền theo DEC-077; kiểm tra xóa và thu hồi đồng thời |
| Vai trò/quyền | Chủ sở hữu quản lý vai trò; mặc định, kế thừa/cho phép/từ chối, ngoại lệ cá nhân và version cấu hình | DEC-055–058, ACL-O1/O2 |
| Tin phòng | Lưu/lịch sử/sửa/xóa/thử lại; chỉ người có quyền, tác giả sửa/xóa | [COM-013](#com-013), [COM-014](#com-014), [COM-015](channels.md#com-015), [COM-016](permissions.md#com-016), [COM-017](#com-017), [COM-018](#com-018), [COM-022](#com-022), [COM-023](#com-023), [COM-024](#com-024); tham chiếu cơ chế tin DM |
| Thu hồi | Mất quyền hoặc rời phải chặn lần đọc/gửi tiếp và kết nối cập nhật theo ngưỡng đã chốt | ACL-O5, OQ-007 |

Prefix `/api/v1`; chưa có endpoint nghiệp vụ Community chạy được. Actor lấy từ phiên, ID server UUIDv7 theo DEC-081; time UTC; request cập nhật dùng `expectedVersion` để tránh ghi đè. POST tạo server/channel/role/link/mời đích danh thêm `clientOperationId` UUIDv4; field đầy đủ ở OpenAPI. `ServerSummary` công khai gồm `id,name,description,visibility,joinMode,version`; chỉ thành viên được nhận chi tiết phòng/thành viên theo quyền.

Danh mục quyền quản lý tối thiểu: quản lý phòng, tạo/thu hồi mời, duyệt/từ chối yêu cầu, đổi join mode và cấu hình quyền xem phòng. Chỉ owner quản lý vai trò/chuyển ownership/sửa metadata server. @everyone không có quyền quản lý, tối đa 20 vai trò tự tạo theo DEC-092. Tên mã permission và phiên bản cấu hình cụ thể hóa bên dưới; không thêm quyền xóa tin người khác.

Unique membership `(serverId,userId)`, unique pending request và cập nhật có điều kiện bảo vệ join/approve/accept lặp. Chuyển owner khóa server và hai membership; target phải vẫn là thành viên active/verified tại commit. Mời có giới hạn khóa record, tạo membership và tăng lượt cùng transaction; rollback không tiêu lượt. Xóa phòng kiểm tra lại quyền và version, chuyển trạng thái deleted rồi phát sự kiện thu hồi sau commit; giữ tombstone để API cũ không phục hồi phòng.

Lỗi đề xuất: validation 400; thiếu phiên 401; trái quyền quản lý 403; tài nguyên không được biết 404; request/version/owner conflict 409; vượt limiter 429; phụ thuộc tạm lỗi 503. [OpenAPI cộng đồng](../../contracts/community.openapi.json) bổ sung schema, role/access API và tin phòng; vẫn là hợp đồng mục tiêu, chưa có endpoint chạy được.

Mỗi endpoint/sự kiện cần schema request/response, actor/quyền, lỗi theo [ProblemDetails](../../architecture.md#contracts), giao dịch, version và retry. Community sở hữu metadata/thành viên/quyền; Messaging sở hữu tin. Messaging kiểm tra quyền qua `IChannelAccessChecker`, không đọc/JOIN schema Community.

Cơ chế tin dùng chung được thiết kế tại [DM](../direct-messaging.md#contracts). Dữ liệu biên TC-TEXT áp dụng cả gửi/sửa tin phòng; không chép một phiên bản quy tắc ký tự hoặc chống trùng khác ở đây.

Phương án ngày 2026-10-04; chưa sửa source/SQL hoặc có mock/proof. [OpenAPI](../../contracts/community.openapi.json), [schema realtime](../../contracts/community-realtime.schema.json), [fixture quyền](../../fixtures/community-permissions.json) và [fixture fingerprint](../../fixtures/community-operations.json) là artefact bàn giao thiết kế. Các lựa chọn sản phẩm đã xác nhận dẫn tới DEC; những thuật toán/interface/migration dưới đây còn cần rà soát kỹ thuật.

<a id="use-case-conditions"></a>

### Điều kiện và ngoại lệ dùng chung

- Mọi actor dùng ứng dụng phải có tài khoản active, email đã xác minh và phiên hợp lệ theo DEC-051. Thao tác quản lý còn đòi membership active và quyền đúng thao tác ở thời điểm thực hiện; owner cũng chịu điều kiện nền. Hệ thống lấy actor từ phiên, không nhận actor tùy ý từ client.
- Luồng chính giả định dữ liệu hợp lệ và phụ thuộc sẵn sàng. Validation, trạng thái tài nguyên và quyền đều kiểm tra phía server theo COM/ACL. Private server không được biết hoặc phòng không được xem trả 404 theo hợp đồng mục tiêu; thiếu quyền quản lý trong scope được biết trả 403. Không trả metadata/nội dung bị ẩn trong lỗi hoặc thông báo.
- Các thao tác có version dùng version đã tải; ACL dùng accessVersion, leave/gán role/ngoại lệ dùng đúng membershipId. Xung đột trả 409 và yêu cầu đọc lại hiện trạng, không tự đổi version/epoch rồi ghi đè.
- Không tự phát lại mutation sau refresh, timeout hoặc mất kết nối. Khi kết quả không rõ, tải lại trạng thái trước khi người dùng quyết định tiếp. Riêng thao tác tạo giữ clientOperationId và payload ban đầu cho lần thử lại chủ động; gửi tin giữ clientMessageId theo thiết kế DM. Cùng khóa khác payload hoặc tài nguyên đã deleted/terminal không tạo lại tài nguyên bằng khóa cũ.
- Khi transaction rollback, không để lại dữ liệu nghiệp vụ dở dang hoặc phát cập nhật như đã thành công. Khi không xác nhận được quyền, dừng thao tác/phát nội dung theo hợp đồng; lỗi phụ thuộc không trở thành quyền truy cập.
- Thu hồi chat đo từ commit, deadline ≤5 giây theo DEC-083; HTTP tiếp theo kiểm tra quyền hiện hành. Media có proof riêng theo DEC-099. Vòng đời nội dung, cleanup và bảo vệ sau restore theo [data-lifecycle.md](../../data-lifecycle.md).

<a id="detailed-design"></a>

### Danh tính, tên và phiên bản

Server/channel/role/request/invitation dùng UUIDv7; actor lấy từ phiên. Tên được trim, kiểm tra Unicode hợp lệ, độ dài UTF-16 và không chỉ trắng/vô hình theo bảng [text-policy](../../fixtures/text-policy.json). Giữ cách viết/emoji của tên; không dùng tên làm định danh. Khóa so sánh đề xuất là NFC + ToLowerInvariant của tên đã trim, so sánh ordinal/DB collation cố định; tên cộng đồng không có unique constraint, tên phòng/vai trò unique trong server theo DEC-095. Nội dung tin vẫn không NFC/trim.

Giới hạn sau trim: server 2–100, channel 1–100, role 1–64 UTF-16; description/topic tối đa 1.000 UTF-16 theo DEC-072/077/093. Đề xuất tên là một dòng, không control/NUL; description/topic là văn bản thuần, rỗng thành null. Validation client/server dùng cùng thứ tự chuẩn hóa. `slug` hiện bắt buộc trong SQL chỉ là cột nội bộ: writer có thể sinh từ UUID, không thêm trường slug do người dùng nhập.

`version` của server/channel/role/membership/request/invitation truyền chuỗi số nguyên dương. Server có `accessVersion` tăng khi membership, role/assignment, ACL hoặc trạng thái truy cập đổi. Channel có `accessVersion` riêng cho cấu hình xem; PUT ACL dùng expectedAccessVersion, không lẫn version metadata. Client gửi đúng version đã tải, 409 thì đọc lại; không tự thay version rồi ghi đè.

Membership giữ unique `(serverId,userId)` nhưng mỗi lần tham gia tạo `membershipId` UUIDv7 mới và tăng version; trạng thái chỉ active/left thuộc v1. Gán role/ngoại lệ cá nhân gắn membershipId để bản cấu hình cũ không áp nhầm khi người dùng rời rồi vào lại. Kicked/banned/timeout/nickname trong schema chưa là chức năng v1 được đặc tả.

<a id="transactions"></a>

### Giao dịch và guard dùng chung

Thứ tự khóa: Identity user rows theo UUID network order → server → membership/role/request/invite/channel → Messaging space/message/operation → outbox. Lookup ban đầu chỉ định tuyến; đọc lại trạng thái/permission sau khi có khóa. `IAccountAccessGuard` đã đề xuất ở DM kiểm tra actor session và tài khoản target khi cần; không JOIN Identity từ Community.

Phương án v1 tuần tự hóa mutation Community bằng `FOR UPDATE` trên server. `IChannelAccessGuard` mới giữ `FOR SHARE` trên server và channel tới commit Messaging, nên thay role/ACL/rời/xóa không chen giữa kiểm tra và lưu tin. Cơ chế [row lock PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS) hỗ trợ xung đột share/write này; đây là suy luận thiết kế cần proof, không là kết quả đo. `IChannelAccessChecker.CheckAsync` hiện chỉ trả bool, không giữ transaction/lease nên chưa đủ chống race.

Shared connection/transaction do BuildingBlocks quản lý, mỗi module chỉ đọc/ghi schema mình sở hữu. Hợp đồng cần bổ sung `IChannelAccessGuard`, `IChatSpaceLifecycle`, `ICommunityAccessReader` và định tuyến thu hồi server/channel/membership; tất cả chưa có implementation. Server lock đơn giản hóa correctness v1 nhưng cần đo contention ở workload đã chốt; không tự suy là đủ cho mọi tải.

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
| `create_role` | name string, permission mask một byte; bit 0–4 theo thứ tự 5 code trong bảng danh mục quyền ở trên |
| `create_invite` | expiresInSeconds nullable UInt32 (default 604800), maxUses nullable UInt32 |
| `create_member_invitation` | recipientUserId UUID 16 byte network order |

Mask chỉ biểu diễn fingerprint; wire vẫn là permission array, không thay thuật toán quyền. Duplicate/unknown permission bị validation trước fingerprint. [community-operations.json](../../fixtures/community-operations.json) có byte/hash kỳ vọng và key giả, gồm Unicode/emoji, đổi thứ tự permission và default/null. Cần đối chiếu writer .NET với fixture khi triển khai; chưa coi mô tả/fixture là proof API.

Join/transition/transfer/leave/PUT/PATCH/DELETE không tự replay sau 401/timeout. Client dùng `retry:false` của api.js cho mutation; response không rõ thì tải trạng thái hiện hành, người dùng quyết định tiếp. GET có thể retry sau refresh. Leave lặp trả 204 khi đã left, nhưng phải có membershipId của lần tham gia đang rời để một request cũ không làm người dùng rời lần rejoin mới.

<a id="collections"></a>

### Danh sách và quyền được biết

`GET /servers` chỉ server người gọi đang là member; `GET /servers/{id}` trả public summary hoặc member detail theo quyền hiện hành, private nonmember nhận 404 trừ preview bằng lời mời hợp lệ. `GET /servers/{id}/channels` chỉ phòng được xem; không có endpoint quản lý lộ metadata phòng bị ẩn theo DEC-098. Sửa/xóa/đọc hoặc thay ACL cần view hiện hành và management permission; actor tự làm mất view qua PUT thì nhận kết quả commit nhưng các request tiếp theo bị chặn. Roster phục vụ role/ngoại lệ chỉ trả user summary/membership cho owner/manage_channel_access. Danh sách pending/mời/role chỉ đúng actor/quyền, không broadcast toàn server.

Phân trang collection quản lý đề xuất mặc định 20/tối đa 50 theo cursor riêng actor/server/filter; inbox mời đích danh của người nhận và status yêu cầu của chính người gửi còn đọc được sau thay đổi visibility để giải thích trạng thái. Các danh sách đang thay đổi có dedup ID/refresh từ đầu; không hứa snapshot bất biến. GET/read cần một transaction snapshot với kiểm tra quyền Community nhất quán; auth session hiện hành vẫn kiểm tra từ Identity.

<a id="channel-messaging"></a>

### Tin phòng và realtime

REST tin text dùng `/servers/{id}/channels/{channelId}/messages` với GET/POST/PATCH/DELETE tương tự DM. Message wire dùng `channelId` thay `conversationId`; `sequence` theo Messaging space, fingerprint/send operation/tombstone/version và cursor/resume dùng chung thiết kế DM. Voice channel chỉ có media, chưa mở luồng text bên trong voice; kind không đổi bằng PATCH trong v1. Quyền quản lý/owner không thay kiểm tra tác giả khi sửa/xóa.

Hub vẫn `/hubs/chat`; schema riêng [community-realtime.schema.json](../../contracts/community-realtime.schema.json) bổ sung SubscribeChannel/UnsubscribeChannel và ChannelMessageChanged. Subscribe kiểm tra phiên/membership/quyền hiện hành; registry gắn user/session/server/channel/membershipId/accessVersion. Dispatcher chỉ phát nội dung cho connection còn đủ điều kiện, reconciliation ≤1 giây và deadline chat ≤5 giây từ commit thu hồi theo DEC-083. Lỗi kiểm tra quyền dừng phát.

Thông báo `CommunityChanged` chỉ chứa eventId/serverId/accessVersion và yêu cầu UI tải lại metadata/quyền; không mang tên phòng bị ẩn, danh sách thành viên/role hay nội dung tin. `MembershipChanged`, `JoinRequestChanged`, `MemberInvitationChanged` định tuyến đến đúng người và manager còn quyền theo user/session, không cần người ngoài subscribe server. Server tự gỡ subscription khi mất quyền, không trông chờ client xử lý thông báo. Client merge version theo từng resource; sự kiện thu hồi có membershipId cũ không được xóa cache của epoch rejoin mới. Reconnect subscribe lại rồi REST bù trang/tải lại tin cũ để nhận edit/delete.

Mất quyền/rời/xóa phòng xóa cache/tin tạm của scope tương ứng và đóng composer; không ảnh hưởng DM. Draft tin phòng đề xuất dùng RAM tab cùng cơ chế DM, không lưu nội dung xuống browser storage; chưa có quyết định riêng nếu muốn lưu bền. Media nhận revocation từ cùng commit, thu hồi ≤5 giây/fail-close theo DEC-099; admission/lease/quota gate nằm trong [thiết kế media](../voice-video.md#detailed-design), không tự coi chat proof là media proof. Voice channel đề xuất gọi IMediaRoomLifecycle tạo room DB rỗng cùng transaction, đánh dấu closing khi xóa; interface/migration chưa có, SFU provision/dừng sau commit.

<a id="schema-migration"></a>

### Schema và migration

| Mã | SQL/source hiện tại | Mapping/đầu việc thiết kế |
|---|---|---|
| <a id="com-sql-01"></a> COM-SQL-01 | servers chưa có visibility/join_mode; description varchar(500), slug bắt buộc | Thêm visibility/join_mode/access_version, description 1.000; slug nội bộ sinh từ ID; backfill visibility/join mode được rà soát theo dữ liệu thực |
| <a id="com-sql-02"></a> COM-SQL-02 | channel name regex slug, tối thiểu 2 ASCII; topic 500; visibility có read-only | Tên Unicode 1–100 UTF-16, topic 1.000; kind text/voice/default_view/deleted_at/version/access_version riêng; unique tên phòng đang active, ID đã deleted không khôi phục; không đưa read-only vào v1 |
| <a id="com-sql-03"></a> COM-SQL-03 | roles tên 50, unique generated lower; default index chỉ “tối đa một” | Role tên 64, key chuẩn hóa cùng service; đúng một @everyone tạo cùng server; system restrictions và giới hạn 20 dưới server lock |
| <a id="com-sql-04"></a> COM-SQL-04 | member_roles/user_overrides gắn user nhưng chưa có epoch | Thêm membership_id/version, FK theo epoch; clear assignment/override khi leave; chống ABA rejoin |
| <a id="com-sql-05"></a> COM-SQL-05 | Không có join_requests hoặc member_invitations | Thêm trạng thái/version/lý do/expiry, unique pending có điều kiện, membershipId đã tạo; transition có khóa và CAS |
| <a id="com-sql-06"></a> COM-SQL-06 | invites giữ hash/lượt/hạn, default_role_id có thể khác default | Thêm version/protected_token; không cấp custom role qua link; use_count/membership cùng transaction |
| <a id="com-sql-07"></a> COM-SQL-07 | channels FK sang Messaging space; chưa có nghiệp vụ writer | Create/delete qua IChatSpaceLifecycle trong shared transaction; Community không đọc/ghi bảng Messaging trực tiếp |
| <a id="com-sql-08"></a> COM-SQL-08 | Trigger servers/spaces tự tăng version; channels/roles chỉ touch timestamp | Một nguồn tăng version cho mỗi resource, tránh trigger và service cùng tăng; wire chuỗi số, accessVersion tăng đúng khi quyền đổi |
| <a id="com-sql-09"></a> COM-SQL-09 | Permission catalog/role overrides có thể chứa quyền ngoài v1 | Migrate danh mục 5 management codes + channel_view; chỉ channel_view nhận override allow/deny; không suy seed permissions thành tính năng |
| <a id="com-sql-10"></a> COM-SQL-10 | Không có operation dedup hoặc realtime dispatcher Community | Thêm operation key `(actor,kind,scope,client_id)` với scope không null (create-server dùng UUID zero), fingerprint/key/version/resource ID; outbox metadata/lease, revoker/guards; key ring bền và thử restart |

Nguồn: [schema.sql](../../../database/postgres/schema.sql), [CommunityModule](../../../services/Modules/Community/CommunityModule.cs), [IChannelAccessChecker](../../../services/SCDC.Contracts/Community/IChannelAccessChecker.cs). Không sửa schema/seed trong đợt tài liệu này. Trước bật unique key mới, dò collision Unicode/case và mapping tên legacy; không tự đổi tên/xóa dữ liệu thật. Giữ tin/phòng deleted theo [vòng đời dữ liệu](../../data-lifecycle.md#deletion): không tự hết hạn tin DEC-070, phòng deleted giữ nội dung chưa hạn purge DEC-105; backup tuổi tối đa 30 ngày DEC-109. Role/override epoch cũ dọn theo rule membership, không phục hồi từ backup; migration không tự xóa nội dung.

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

Review contract/schema/fixture với UX và AC/TC trước tích hợp. Proof cần bao gồm lượt cuối, approve/cancel/accept tranh nhau, 21 role tạo đồng thời, stale epoch sau rejoin, role deny bị xóa, revoke tranh gửi/subscribe, owner transfer/leave và migration Unicode. Các ca chưa có kết quả chạy trên sản phẩm; kiểm tra fixture chỉ đối chiếu giá trị thiết kế.

<a id="acceptance"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-12"></a> AC-COM-12 | Thành viên được phép nhắn gửi tin văn bản trong phòng, rồi sửa và xóa tin của mình. | Tin được lưu và thấy lại; sửa có dấu “Đã sửa”; xóa hiện “Tin nhắn đã bị xóa” cho các thành viên có quyền xem phòng. |
| <a id="ac-com-13"></a> AC-COM-13 | Thành viên cố sửa/xóa tin của người khác hoặc đọc/gửi tin ở phòng không có quyền. | Hệ thống từ chối, kể cả khi gọi trực tiếp API. |
| <a id="ac-com-18"></a> AC-COM-18 | Kết quả gửi tin phòng lần đầu không rõ, người gửi bấm thử lại cho chính tin đó. | Phòng chỉ có một tin tương ứng với thao tác gửi. |
| <a id="ac-com-22"></a> AC-COM-22 | Thành viên chưa xác minh email nhưng có quyền xem phòng thử gửi tin. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| <a id="ac-com-23"></a> AC-COM-23 | Thành viên sửa tin nhiều lần; người khác tải lại. | Chỉ nội dung hiện hành cùng dấu “Đã sửa”, không có lịch sử nội dung cũ. |
| <a id="ac-com-24"></a> AC-COM-24 | Thành viên gửi/sửa tin tại biên 2.000/2.001 UTF-16 sau chuẩn hóa xuống dòng; gửi tin chỉ trắng/vô hình hoặc Unicode lỗi. | Nhận đến 2.000 UTF-16, từ chối vượt giới hạn/tin trống/Unicode lỗi; giữ tiếng Việt, emoji và ZWJ trong tin có nội dung theo DEC-090. |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](../community.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Chưa có kết quả chạy AC-COM.

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](#evidence). Các ca bên dưới đều chưa chạy; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-10"></a> TC-COM-10 | Gửi/sửa/xóa tin phòng; N sửa tin O bằng API; gửi lại khi mất response | Kiểm tra tác giả, tombstone, một tin mỗi thao tác; dữ liệu UTF-16 dùng chung DM | [AC-COM-12](#ac-com-12), [AC-COM-13](#ac-com-13), [AC-COM-18](#ac-com-18), [AC-COM-23](#ac-com-23), [AC-COM-24](#ac-com-24) |
| <a id="tc-com-12"></a> TC-COM-12 | U chưa xác minh giả membership và gọi API đọc/gửi | Chặn truy cập ứng dụng phía server, không suy U được đọc phòng | [AC-COM-22](#ac-com-22), DEC-051 |
| <a id="tc-com-23"></a> TC-COM-23 | POST create response mất; manual retry cùng/khác payload; resource sau đó deleted | Một resource cùng operation, payload khác conflict; không phục hồi resource deleted bằng retry | Hợp đồng operation |
| <a id="tc-com-25"></a> TC-COM-25 | Channel guard tranh role/ACL/leave/delete và send/subscribe/dispatch | Guard giữ tới commit; thu hồi chat ≤5 giây, không mất/trùng/trái quyền | [AC-COM-11](permissions.md#ac-com-11), [AC-COM-18](#ac-com-18), [AC-COM-34](channels.md#ac-com-34), [AC-COM-42](permissions.md#ac-com-42), DEC-083 |
| <a id="tc-com-26"></a> TC-COM-26 | Migration tên legacy/collision, system role/permission catalog/trigger version | Không đổi/xóa dữ liệu thật tự động; một nguồn tăng version và schema phù hợp writer | COM-SQL-01–10 |

<a id="evidence"></a>

## Dữ liệu và bằng chứng kiểm thử

Dữ liệu: O là chủ sở hữu; M được cấp một quyền quản lý cụ thể; N là thành viên thường. Chuẩn bị phòng mở/phòng giới hạn, vai trò R-allow/R-deny, người ngoài cộng đồng, lời mời hợp lệ/hết hạn/thu hồi và yêu cầu chờ duyệt. Tài khoản thử phải có trạng thái xác minh/phiên được kiểm soát.

Các tiêu chí mới dẫn tới DEC-072–077/087/092–098; toàn bộ AC-COM chưa có kết quả thực thi được ghi nhận. Ca chat thu hồi đo ≤5 giây theo DEC-083; media thu hồi ≤5 giây/fail-close theo DEC-099, chưa có kết quả SFU.

Các ca này bao phủ quyền đã chốt; cần bổ sung ca cho từng AC-COM khi hoàn thiện backend/API. Chạy trực tiếp API để kiểm tra quyền, không chỉ nhìn nút UI. Ca nội dung tin dùng [TC-TEXT](../direct-messaging.md#tests). Ca gửi lại/đồng thời cũng cần chạy cho tin phòng sau khi chọn hợp đồng.

Ca [AC-COM-22](#ac-com-22) kiểm tra điều kiện chưa xác minh phía máy chủ; DEC-051 vẫn chặn truy cập ứng dụng từ trước, không suy rằng người chưa xác minh được đọc phòng. Chat thu hồi đã chốt DEC-083; ca media theo DEC-099 và MEDIA-GAP ở đặc tả media vẫn “Chưa chạy”, không đánh dấu đạt. Kết quả theo [mẫu nghiệm thu](../../release-operations.md#testing).

<a id="gaps"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../community.md#use-case-delivery), [migration](#schema-migration) và [vòng đời dữ liệu](../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Nhắn tin phòng | Dùng chung text/HMAC/sequence/cursor DM; REST/realtime đã có schema, chưa có writer/mock/proof; mất quyền không xóa nội dung, room deleted giữ chưa đặt hạn theo DEC-105; restore placeholder dùng chung schema DEC-108, còn DATA-GAP proof. | OQ-005, OQ-011 |

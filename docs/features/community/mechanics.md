# SCDC — Cơ chế dùng chung của Community

Trang này mô tả phần kỹ thuật dùng chung của module Community: ai chịu trách nhiệm phần nào, tổ chức code, quy ước ID và phiên bản, transaction và guard, chống trùng khi tạo, phân trang, schema và migration, mã lỗi. Quy tắc và giao diện của từng chức năng nằm ở [các trang Community](README.md); tin trong phòng ở [Messaging](../messaging/channel-messaging.md).

**Trạng thái:** áp dụng cho nền MVP (một ứng dụng, một database). Đã có: tạo, xem, tìm server; tham gia; vai trò; phòng chữ; ACL; migration 001 đến 004. Chưa có: yêu cầu tham gia, lời mời, xóa phòng, realtime Community.

<a id="responsibilities"></a>

<a id="module-phụ-trách-và-nguồn-chuẩn"></a>

## Ai phụ trách phần nào

| Nội dung | Module | Định nghĩa tại |
|---|---|---|
| Cộng đồng, thành viên, lời mời, thông tin và phân quyền phòng | Community | [Servers](servers.md), [Memberships](memberships.md), [Invitations](invitations.md), [Channels](channels.md), [Permissions](access-control.md) |
| Nội dung tin; lưu, gửi, lịch sử, sửa, xóa; chống trùng; sequence, cursor, tombstone | Messaging | [Nhắn tin](../messaging/README.md), [thiết kế chi tiết](../messaging/README.md#detailed-design), [TC-TEXT](../messaging/text-policy.md#tests) |
| Áp cơ chế nhắn tin vào phòng; kiểm tra quyền xem và tư cách thành viên; định tuyến cập nhật | Messaging, phối hợp Community, Identity, WebClient | [UC-COM-23 đến 25](../messaging/channel-messaging.md#use-cases), [tin phòng](../messaging/channel-messaging.md#channel-messaging), [OpenAPI](../../contracts/community.openapi.json), [realtime](../../contracts/community-realtime.schema.json) |
| Tài khoản và phiên | Identity | [Tài khoản](../accounts/README.md) |
| Giao diện, trạng thái gửi, đồng bộ sau kết nối lại | WebClient | [Giao diện chung](#ux) |

- COM-013, 014, 017, 018, 022, 023, 024 và các tiêu chí của tin phòng được định nghĩa ở [tin phòng](../messaging/channel-messaging.md). Thứ tự quyền và thu hồi ở [quyền xem](access-control.md#view-permissions).
- **Mở một phòng (UC-COM-17):** Community trả danh sách và thông tin phòng theo quyền xem; Messaging trả lịch sử qua guard của Community.
- **UC-COM-23, 24:** Messaging thực hiện. **UC-COM-25:** Messaging làm Hub và worker; Community cung cấp quyền, vòng đời và thay đổi cần thu hồi; Identity cung cấp phiên; WebClient đồng bộ giao diện.
- DM có quyền riêng ([DM](../messaging/direct-messaging.md#permissions)).
- v1: sự kiện Community → Messaging theo [D27](../../system/api-conventions.md#v1-integration-event-transport).

<a id="use-case-conditions"></a>

<a id="điều-kiện-và-ngoại-lệ-dùng-chung"></a>

### Điều kiện chung cho mọi ca sử dụng

- **Người thực hiện:** tài khoản hoạt động, email đã xác minh, phiên hợp lệ (DEC-051). Thao tác quản lý còn cần là thành viên đang hoạt động và có đúng quyền tại thời điểm thực hiện; chủ server cũng phải đủ các điều kiện này. Server lấy người thực hiện từ phiên, không nhận từ client.
- **Kiểm tra phía server:** dữ liệu, trạng thái tài nguyên và quyền đều kiểm tra ở server theo COM và ACL. Server riêng tư mà người gọi không được biết, hoặc phòng không được xem: trả 404. Biết phạm vi nhưng thiếu quyền quản lý: 403. Không lộ thông tin bị ẩn qua lỗi.
- **Phiên bản:** thao tác dùng version đã tải; ACL dùng `accessVersion`; rời server, gán vai trò, ngoại lệ cá nhân dùng đúng `membershipId`. Xung đột trả 409; client phải đọc lại, không tự sửa version rồi ghi đè.
- **Không tự gửi lại** thao tác ghi sau khi làm mới token, timeout hay mất kết nối. Kết quả không rõ thì tải lại trạng thái. Riêng thao tác tạo giữ `clientOperationId` và nội dung ban đầu để người dùng chủ động gửi lại; gửi tin giữ `clientMessageId`. Cùng khóa khác nội dung, hoặc tài nguyên đã xóa hay đã kết thúc, thì không tạo lại.
- **Rollback** không được phát cập nhật như đã thành công. Tạo phòng v1 theo [D14](channels.md#v1-channel-provisioning) và [D19](channels.md#v1-channel-creation-interruption).
- **Không xác nhận được quyền** thì dừng thao tác hoặc dừng gửi nội dung; lỗi phụ thuộc không biến thành quyền truy cập.
- **Thu hồi chat:** tối đa 5 giây từ commit (DEC-083); request HTTP tiếp theo kiểm tra quyền hiện tại. Media kiểm thử riêng (DEC-099). Vòng đời nội dung xem [vòng đời dữ liệu](../../system/data-lifecycle.md).

<a id="organization"></a>

<a id="năm-thành-phần-và-cấu-trúc-code"></a>

## Cấu trúc code

Community chia thành năm phần:

| Phần | Trách nhiệm | Ca sử dụng | Trang |
|---|---|---|---|
| Servers | Thông tin server, tìm kiếm, công khai hay riêng tư, cách tham gia, chủ sở hữu | [UC-COM-01](servers.md#uc-com-01), [02](servers.md#uc-com-02), [03](servers.md#uc-com-03), [04](servers.md#uc-com-04), [05](servers.md#uc-com-05), [14](servers.md#uc-com-14) | [Servers](servers.md) |
| Memberships | Tư cách thành viên và epoch; tham gia, xin duyệt, duyệt, từ chối, hủy; rời và tham gia lại | [UC-COM-06](memberships.md#uc-com-06), [07](memberships.md#uc-com-07), [08](memberships.md#uc-com-08), [15](memberships.md#uc-com-15) | [Memberships](memberships.md) |
| Invitations | Link mời và mời đích danh; hạn, lượt, bí mật, chuyển trạng thái | [UC-COM-09](invitations.md#uc-com-09), [10](invitations.md#uc-com-10), [11](invitations.md#uc-com-11), [12](invitations.md#uc-com-12), [13](invitations.md#uc-com-13) | [Invitations](invitations.md) |
| Channels | Thông tin, loại và vòng đời phòng; danh sách theo quyền xem | [UC-COM-16](channels.md#uc-com-16), [17](channels.md#uc-com-17), [18](channels.md#uc-com-18), [19](channels.md#uc-com-19) | [Channels](channels.md) |
| Permissions | Vai trò, gán vai trò, ACL; bộ tính quyền, bộ kiểm tra, guard | [UC-COM-20](access-control.md#uc-com-20), [21](access-control.md#uc-com-21), [22](access-control.md#uc-com-22) | [Permissions](access-control.md) |

UC-COM-23, 24 ([tin phòng](../messaging/channel-messaging.md#uc-com-23)) do Messaging làm; UC-COM-25 phối hợp nhiều module.

<a id="cấu-trúc-code-mục-tiêu"></a>

### Cây thư mục

Code nhóm theo chức năng; Domain và Application nằm trong từng phần. Tất cả cùng một assembly, schema `community` và `CommunityDbContext`. Tiến độ từng phần xem [Community](README.md).

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
    ├── Idempotency/       # khóa thao tác, fingerprint, truy cập khóa
    └── Outbox/            # ghi và gửi thay đổi Community
```

Controller ở `services/SCDC.Api/Controllers/Community`; test ở `tests/SCDC.Api.Tests/Community`. `CommunityModule.cs` đăng ký DI. Chỉ tạo file khi có code thật; không commit thư mục rỗng.

<a id="ranh-giới-và-phụ-thuộc"></a>

### Ranh giới giữa các phần

- **Chủ sở hữu:** chỉ lưu ở `servers.owner_user_id`. Memberships kiểm tra người nhận còn là thành viên khi chuyển chủ. Permissions đọc giá trị này để tính quyền, không lưu bản riêng.
- **Tạo tư cách thành viên:** chỉ Memberships ghi. Tham gia, duyệt và nhận lời mời đều dùng chung hành vi này. Invitations dùng lời mời và trừ lượt trong cùng transaction.
- **Tính quyền:** Domain của Permissions tính trên một bản chụp dữ liệu, không gọi service của phần khác. Tránh vòng gọi giữa Channels, Memberships và Permissions.
- **Transaction:** các phần dùng chung transaction và DbContext. Tạo server ghi chủ, @everyone và thao tác trong một commit; nhận lời mời ghi thành viên, lượt, trạng thái trong một commit; xóa phòng phối hợp với Messaging.
- **Module khác:** gọi Identity, Messaging, Media qua `SCDC.Contracts`; mỗi module chỉ đọc ghi dữ liệu của mình.

<a id="contracts"></a>

## API

- Tiền tố `/api/v1`. Chỉ phần đã làm trong gói mới có bằng chứng chạy.
- Người thực hiện lấy từ phiên. ID server dạng UUIDv7 (DEC-081). Thời gian UTC.
- Cập nhật gửi `expectedVersion` để không ghi đè. POST tạo server, phòng, vai trò, link mời, mời đích danh gửi thêm `clientOperationId` (UUIDv4).
- `ServerSummary` công khai: `id, name, description, visibility, joinMode, version`. Chỉ thành viên mới nhận chi tiết phòng và thành viên, theo quyền.

Schema đầy đủ: [OpenAPI Community](../../contracts/community.openapi.json) và [realtime](../../contracts/community-realtime.schema.json) (gồm cả tin phòng do Messaging làm). Route chưa làm được ghi là mục tiêu.

<a id="detailed-design"></a>

<a id="danh-tính-tên-và-phiên-bản"></a>

### ID, tên và phiên bản

**ID:** server, phòng, vai trò, yêu cầu, lời mời dùng UUIDv7.

**Tên:**

- Cắt khoảng trắng hai đầu; kiểm tra Unicode hợp lệ; đếm độ dài UTF-16; không được chỉ có khoảng trắng hay ký tự vô hình (theo [text-policy](../../fixtures/text-policy.json)).
- Giữ nguyên cách viết và emoji. Không dùng tên làm định danh.
- Khóa so sánh: chuẩn hóa NFC rồi `ToLowerInvariant` (.NET), so sánh ordinal (collation C trong DB).
- Tên cộng đồng được trùng. Tên phòng và tên vai trò là duy nhất trong một server (DEC-095).
- Nội dung tin không áp dụng NFC hay cắt khoảng trắng.

| Trường | Độ dài (UTF-16, sau khi cắt) |
|---|---|
| Tên server | 2–100 |
| Tên phòng | 1–100 |
| Tên vai trò | 1–64 |
| Mô tả server, chủ đề phòng | Tối đa 1.000 (rỗng thì lưu null) |

Tên chỉ một dòng, không có ký tự điều khiển hay NUL. Mô tả và chủ đề là văn bản thuần. Client và server chuẩn hóa theo cùng thứ tự. Cột `slug` chỉ dùng nội bộ, sinh từ UUID.

**Phiên bản:**

- `version` của server, phòng, vai trò, thành viên, yêu cầu, lời mời gửi dạng chuỗi số nguyên dương.
- Server có `accessVersion`, tăng khi thành viên, vai trò, gán vai trò, ACL hay trạng thái truy cập thay đổi.
- Phòng có `accessVersion` riêng cho cấu hình xem. PUT ACL dùng `expectedAccessVersion`, không lẫn với version thông tin phòng.
- Gặp 409 thì đọc lại; không tự thay version rồi ghi đè.

**Tư cách thành viên và epoch:**

- Mỗi cặp `(serverId, userId)` có một bản ghi.
- Lần đầu tham gia tạo `membershipId` (UUIDv7), version 1. Tham gia lại sau khi rời tạo epoch mới và tăng version. Tham gia lặp khi đang là thành viên thì giữ nguyên.
- Gán vai trò và ngoại lệ cá nhân gắn với `membershipId`, nên cấu hình cũ không áp vào lần tham gia mới.
- v1 chỉ có hai trạng thái: active và left. Các trạng thái kicked, banned, timeout, nickname trong schema chưa phải chức năng v1.

<a id="transactions"></a>

<a id="giao-dịch-và-guard-dùng-chung"></a>

## Transaction và guard

### Thứ tự khóa (MVP)

Dòng người dùng Identity (theo UUID, thứ tự network) → server → thành viên, vai trò, yêu cầu, lời mời, phòng → không gian, tin, thao tác của Messaging → outbox.

- Lần tra cứu đầu chỉ để biết khóa gì; sau khi có khóa phải đọc lại trạng thái và quyền.
- Guard Identity kiểm tra phiên người thực hiện, và tài khoản người kia khi cần. Community không JOIN bảng Identity.

### Cách giữ quyền tới commit (MVP)

- Mọi thao tác ghi của Community khóa server bằng `FOR UPDATE`.
- `IChannelAccessGuard` giữ `FOR SHARE` trên server và phòng tới khi bên gọi commit. Guard trả lease (epoch, các phiên bản quyền, hạn phiên); bên gọi phải kiểm tra hạn ngay trước commit, và tự kiểm tra trạng thái không gian, tác giả của Messaging.
- `IChannelAccessChecker.CheckAsync` chỉ trả đúng hay sai, không giữ transaction hay lease.
- Đã kiểm chứng ([gói phòng và ACL](channels.md)): thay đổi vai trò hay ACL phải chờ transaction đọc xong; thu hồi ACL trước khi vào thì bị chặn. Chưa kiểm chứng cho ghi tin, rời server, xóa phòng, Hub.
- Cơ chế dựa trên [khóa dòng PostgreSQL](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS). Khóa server cần đo tranh chấp với tải đã chốt.
- Đã có trong `main`: `IChannelAccessGuard`, phần tạo phòng của `IChatSpaceLifecycle`. Chưa có: xóa phòng, vòng đời Media, `ICommunityAccessReader`, định tuyến thu hồi cho server, phòng, thành viên.

### Khi tách service (v1)

Transaction và guard dùng chung chỉ đúng khi một ứng dụng dùng một database. Ở v1:

- Quyền tới commit theo [D05](../../system/access-revocation.md#v1-authorization-coordination).
- Tạo phòng chỉ báo xong khi dữ liệu nền đã chuẩn bị ([D14](channels.md#v1-channel-provisioning)). Community lưu tiến độ và điều phối ([D15](channels.md#v1-channel-creation-coordination)); gọi gRPC và nhận kết quả sau khi bên kia commit ([D16](channels.md#v1-channel-preparation-transport)); chờ ngắn, hủy, bù ([D19](channels.md#v1-channel-creation-interruption)).
- Một lời gọi gRPC không thay được transaction chung.

Quy tắc chuyển trạng thái và gửi lại theo epoch: [Memberships](memberships.md#technical), [Invitations](invitations.md#technical). Transaction riêng của từng chức năng: [Servers](servers.md#technical), [Memberships](memberships.md#membership-contracts), [Invitations](invitations.md#invite-contracts), [Channels](channels.md#technical), [Permissions](access-control.md#permission-contracts).

<a id="account-transactions"></a>

<a id="account-transaction-và-identity-guard"></a>

### Transaction chung và guard Identity (MVP)

- Mỗi thao tác dùng một kết nối PostgreSQL và một transaction `READ COMMITTED`. `CommunityDbContext` tham gia transaction đó ([EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)).
- `IAccountAccessGuard` (trong Contracts) nhận người thực hiện và `DbTransaction`; không phụ thuộc EF hay code Identity. Identity dùng kết nối của transaction để khóa và kiểm tra schema của mình.
- Không chạy truy vấn song song trên kết nối chung.
- Guard lấy `FOR SHARE` trên `identity.users`, rồi đọc lại: người dùng hoạt động, chưa xóa, email chính đã xác minh, phiên thuộc người dùng, chưa thu hồi, chưa hết hạn, stamp khớp. Các thao tác bảo mật của Identity lấy `FOR NO KEY UPDATE` trên người dùng trước; writer mới phải giữ quy ước này.
- Khóa không ngăn thời gian trôi: guard kiểm tra hạn phiên lại ngay trước commit; hết hạn thì rollback.
- Không giữ transaction trong lúc chờ người dùng hay gọi dịch vụ ngoài.
- Cấu hình ban đầu: chờ khóa 2 giây, timeout lệnh 5 giây, hủy theo request. Đây là giá trị để thử, chưa đo tải.

<a id="operations"></a>

<a id="operation-fingerprint-và-retry"></a>

## Chống trùng khi tạo

**Áp dụng cho:** POST tạo server, phòng, vai trò, link mời, mời đích danh.

- Client gửi `clientOperationId` (UUIDv4) và giữ nguyên nội dung.
- Khóa duy nhất: người thực hiện + loại thao tác + phạm vi + `clientOperationId`. Tạo server dùng phạm vi UUID toàn số 0.
- Fingerprint HMAC trên nội dung đã chuẩn hóa; không lưu token thô.
- **Gửi lại cùng khóa, cùng nội dung:** trả tài nguyên hiện tại (sau khi kiểm tra quyền). **Khác nội dung:** `409 OPERATION_CONFLICT`. **Tài nguyên đã xóa hoặc kết thúc:** không tạo lại.
- **Tạo phòng (v1):** xử lý theo [D17, D19](channels.md#v1-channel-creation-response). Kết quả chưa rõ thì giữ khóa và nội dung; sau khi đã xác nhận hủy, Thử lại là thao tác mới với khóa mới. Có thể tra tiến trình bằng GET theo người thực hiện + `create_channel` + server + `clientOperationId` ([D18](channels.md#v1-channel-operation-recovery)); GET không cần gửi lại nội dung và không kích hoạt xử lý.

**Các thao tác khác** (tham gia, chuyển trạng thái, chuyển chủ, rời, PUT, PATCH, DELETE) không tự gửi lại sau 401 hay timeout. Client dùng `retry:false`; kết quả không rõ thì tải lại trạng thái. GET được thử lại sau khi làm mới token. Rời lặp trả 204 nếu đã rời, nhưng phải gửi `membershipId` của lần tham gia đang rời, để request cũ không làm người dùng rời lần tham gia mới.

### Cách tính fingerprint (v1)

Dữ liệu đầu vào dạng byte, theo thứ tự:

1. Chuỗi ASCII `SCDC.Community.Write.v1` + byte 0.
2. Loại thao tác (ASCII) + byte 0.
3. UUID người thực hiện, phạm vi, `clientOperationId` theo thứ tự network.
4. Độ dài phần thân (UInt32 big-endian), rồi phần thân.

Phần thân lấy từ DTO đã chuẩn hóa và điền mặc định, không gồm `clientOperationId`, không dùng JSON:

- Chuỗi: độ dài UTF-8 (UInt32 big-endian) + byte UTF-8.
- Giá trị có thể null: byte 0 nếu null, hoặc byte 1 + giá trị. Số có thể null: dấu đó rồi UInt32 big-endian.
- Tên giữ cách viết sau khi cắt khoảng trắng; mô tả và chủ đề rỗng thì là null.

| Loại thao tác | Các trường theo thứ tự | Đã làm? |
|---|---|---|
| `create_server` | name, description (có thể null), visibility (public=1, private=2) | Có |
| `create_channel` | name, topic (có thể null), kind (text=1, voice=2) | Có cho phòng chữ; phòng thoại là mục tiêu |
| `create_role` | name, mask quyền một byte (bit 0–4 theo thứ tự 5 mã trong [danh mục quyền](access-control.md#permission-detailed-design)) | Có |
| `create_invite` | expiresInSeconds (có thể null, mặc định 604800), maxUses (có thể null) | Chưa |
| `create_member_invitation` | recipientUserId (UUID 16 byte) | Chưa |

Mask chỉ dùng cho fingerprint; API vẫn nhận mảng quyền. Quyền trùng hay không hợp lệ bị từ chối trước khi tính. [community-operations.json](../../fixtures/community-operations.json) có byte và hash mong đợi với khóa giả (gồm Unicode, emoji, đổi thứ tự quyền, mặc định và null). Writer server, phòng, vai trò đã có bằng chứng ([hồ sơ Community](../../records/verification/community/README.md)); hai loại lời mời chưa có.

<a id="security-keys"></a>

<a id="security-operation-cursor-và-khóa"></a>

### Lưu thao tác và khóa

- Bản ghi thao tác lưu: `fingerprint_version=1`, `key_id`, hash 32 byte, ID tài nguyên, thời gian UTC. Không lưu JWT hay toàn bộ request. So sánh hash bằng hàm thời gian không đổi.
- ID tài nguyên được kiểm tra cùng commit; không dùng khóa ngoại "đa hình" giả.
- **Khóa HMAC:** riêng cho Community, ngẫu nhiên tối thiểu 32 byte; cấu hình khóa đang dùng và tập khóa qua secret. Khởi động thì kiểm tra có khóa; code không chứa khóa mặc định.
- Gửi lại dùng khóa cũ của bản ghi; xoay khóa chỉ áp cho thao tác mới; giữ khóa cũ khi còn bản ghi dùng nó ([quy tắc khóa Messaging](../messaging/message-lifecycle.md#uuid-fingerprint-và-khóa-giao-dịch)).
- Chưa đặt thời hạn xóa bản ghi thao tác.

### Khóa của cursor

- Data Protection dùng `ApplicationName` cố định theo môi trường và key ring lưu trên volume bền qua khởi động lại và triển khai; tách khỏi khóa HMAC và khóa ký JWT.
- Phải cấu hình rõ, vì mặc định key có thể chỉ nằm trong bộ nhớ ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0)).
- Cursor hết hạn hay bị sửa: 400. Không đọc được key ring: 503; không giả cursor hợp lệ.

Cấu hình và cách chạy xem [hướng dẫn Community](../../guides/development.md#community-configuration). API không tự chạy migration.

<a id="collections"></a>

<a id="danh-sách-và-quyền-được-biết"></a>

## Danh sách

- `GET /servers`: chỉ các server người gọi đang là thành viên.
- `GET /servers/{id}`: tóm tắt công khai hoặc chi tiết thành viên, theo quyền. Server riêng tư mà không phải thành viên: 404, trừ khi xem trước bằng lời mời hợp lệ.
- `GET /servers/{id}/channels`: chỉ các phòng được xem. Không có endpoint quản lý nào lộ thông tin phòng bị ẩn (DEC-098).
- Sửa, xóa, đọc hay thay ACL cần quyền xem hiện tại và quyền quản lý. Nếu tự làm mất quyền xem qua PUT, thao tác vẫn commit nhưng các request sau bị chặn.
- Danh sách thành viên (để gán vai trò hay ngoại lệ) chỉ cho chủ server và người có `manage_channel_access`.
- Danh sách yêu cầu, lời mời, vai trò chỉ cho đúng người và đúng quyền; không phát cho cả server.

**Phân trang (đề xuất):** mặc định 20, tối đa 50, cursor riêng theo người gọi, server và bộ lọc. Hộp lời mời của người nhận và trạng thái yêu cầu của người gửi vẫn đọc được sau khi server đổi chế độ, để giải thích trạng thái. Danh sách đang thay đổi thì loại trùng theo ID hoặc tải lại từ đầu; không hứa bản chụp cố định. Đọc dùng một transaction để kiểm tra quyền nhất quán.

<a id="schema-migration"></a>

<a id="schema-và-migration"></a>

## Schema và migration

Bảng dưới so sánh schema **trước** chuỗi migration Community với thiết kế đầy đủ; không mô tả schema hiện tại. `main` đã có ledger 001 đến 004. Tiến độ xem [Community](README.md) và [đối chiếu test](../../records/verification/community/README.md). Yêu cầu tham gia, lời mời, xóa phòng, realtime vẫn là mục tiêu.

| Mã | Trước migration | Cần làm |
|---|---|---|
| <a id="com-sql-01"></a> COM-SQL-01 | `servers` chưa có visibility, join_mode; description 500; slug bắt buộc | Thêm visibility, join_mode, access_version; description 1.000; slug sinh từ ID; điền visibility và join mode từ dữ liệu thật đã review |
| <a id="com-sql-02"></a> COM-SQL-02 | Tên phòng dạng slug, tối thiểu 2 ký tự ASCII; topic 500; có chế độ chỉ đọc | Tên Unicode 1–100 UTF-16, topic 1.000; thêm kind (text, voice), default_view, deleted_at, version, access_version riêng; tên duy nhất trong các phòng đang hoạt động; không khôi phục ID đã xóa; bỏ chế độ chỉ đọc khỏi v1 |
| <a id="com-sql-03"></a> COM-SQL-03 | Tên vai trò 50; unique theo chữ thường; index mặc định chỉ đảm bảo "tối đa một" | Tên 64, khóa chuẩn hóa giống service; đúng một @everyone tạo cùng server; giới hạn hệ thống và tối đa 20 vai trò dưới khóa server |
| <a id="com-sql-04"></a> COM-SQL-04 | Gán vai trò và ngoại lệ cá nhân gắn với người dùng, chưa có epoch | Thêm membership_id, version, khóa ngoại theo epoch; xóa gán và ngoại lệ khi rời; tránh lỗi tham gia lại dùng cấu hình cũ |
| <a id="com-sql-05"></a> COM-SQL-05 | Chưa có bảng yêu cầu tham gia và mời đích danh | Thêm trạng thái, version, lý do, hạn; unique có điều kiện cho yêu cầu đang chờ; lưu membershipId đã tạo; chuyển trạng thái có khóa và CAS |
| <a id="com-sql-06"></a> COM-SQL-06 | `invites` có hash, lượt, hạn; vai trò mặc định có thể khác @everyone | Thêm version, protected_token; link không cấp vai trò tùy chỉnh; tăng lượt và tạo thành viên trong cùng transaction |
| <a id="com-sql-07"></a> COM-SQL-07 | `channels` có khóa ngoại sang không gian Messaging; chưa có writer | MVP: tạo và xóa qua `IChatSpaceLifecycle` trong transaction chung (tạo đã có, xóa chưa). v1 theo [D14](channels.md#v1-channel-provisioning); Community không đọc ghi DB Messaging |
| <a id="com-sql-08"></a> COM-SQL-08 | Trigger tự tăng version cho servers, spaces; channels, roles chỉ cập nhật thời gian | Mỗi tài nguyên chỉ một nơi tăng version (không để cả trigger và service cùng tăng); API gửi chuỗi số; accessVersion tăng đúng khi quyền đổi |
| <a id="com-sql-09"></a> COM-SQL-09 | Danh mục quyền và ngoại lệ có thể chứa quyền ngoài v1 | Chuyển sang 5 mã quản lý + `channel_view`; chỉ `channel_view` có ngoại lệ allow hay deny; không coi quyền trong seed là tính năng |
| <a id="com-sql-10"></a> COM-SQL-10 | Chưa có chống trùng thao tác hay worker realtime cho Community | Thêm khóa thao tác `(actor, kind, scope, client_id)` với scope không null (tạo server dùng UUID 0), fingerprint, khóa, version, ID tài nguyên; outbox có lease; bộ thu hồi, guard; key ring bền và thử khởi động lại |

- Code hiện tại: [schema.sql](../../../database/postgres/schema.sql), [CommunityModule](../../../services/Modules/Community/CommunityModule.cs), [IChannelAccessGuard](../../../services/SCDC.Contracts/Community/IChannelAccessGuard.cs). Schema trước migration còn trong `tests/SCDC.Api.Tests/Community/Fixtures/legacy-schema.sql`.
- Trước khi bật khóa duy nhất mới, dò trùng Unicode, hoa thường và kiểm tra mapping cũ đã review. Không tự đổi tên hay xóa dữ liệu thật.
- Tin và phòng đã xóa giữ theo [vòng đời dữ liệu](../../system/data-lifecycle.md#deletion): tin không tự hết hạn (DEC-070); phòng đã xóa giữ nội dung, chưa có hạn xóa (DEC-105).
- Vai trò và ngoại lệ của epoch cũ dọn theo quy tắc thành viên. Khôi phục từ backup (sau v1) không được phục hồi quyền cũ. Migration không tự xóa nội dung.

<a id="errors"></a>

<a id="lỗi-và-kiểm-chứng"></a>

## Mã lỗi

| HTTP và mã (đề xuất) | Giao diện |
|---|---|
| 400 `VALIDATION_FAILED`, `NAME_INVALID`, `ACCESS_CONFIG_INVALID` | Giữ form, chỉ ra trường sai; không tự cắt hay đổi Unicode |
| 404 `RESOURCE_NOT_FOUND` | Server riêng tư, không phải thành viên, phòng bị ẩn; không lộ thông tin |
| 403 `PERMISSION_DENIED` | Biết phạm vi nhưng thiếu quyền quản lý hoặc không phải tác giả |
| 409 `VERSION_CONFLICT`, `MEMBERSHIP_CHANGED` | Tải lại version, epoch, quyền; không tự gửi lại |
| 409 `ROLE_LIMIT_REACHED`, `NAME_CONFLICT`, `SYSTEM_ROLE_IMMUTABLE` | Giải thích đúng giới hạn; không sửa được @everyone |
| 409 `REQUEST_NOT_PENDING`, `INVITATION_NOT_PENDING`, `OWNER_MUST_TRANSFER` | Hiện trạng thái hiện tại; không chuyển lại trạng thái cũ |
| 400 `INVITE_INVALID` hoặc 409 `INVITE_EXHAUSTED` | Không cấp tư cách, không lộ thông tin server riêng; xem trước không giữ lượt |
| 503 `KEY_UNAVAILABLE`, `ACCESS_CHECK_UNAVAILABLE` | Dừng thao tác hoặc dừng gửi; không bỏ qua guard, không tạo lại link |

Trước khi tích hợp, review hợp đồng, schema, fixture cùng giao diện và tiêu chí. Bằng chứng cần có: lượt cuối của link, duyệt, hủy, nhận lời mời tranh nhau, tạo 21 vai trò đồng thời, epoch cũ sau khi tham gia lại, xóa vai trò deny, thu hồi tranh với gửi và đăng ký, chuyển chủ và rời, migration Unicode.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện chung

- Bộ đếm tên, mô tả, chủ đề dùng UTF-16 giống API (DEC-092, 093). Giới hạn xem [COM-029](servers.md#com-029), [COM-034](channels.md#com-034), [COM-037](access-control.md#com-037), [COM-038](servers.md#com-038), [COM-039](servers.md#com-039).
- Màn hình hẹp (đề xuất): lần lượt danh sách cộng đồng → danh sách phòng → cuộc trò chuyện; phần quản lý mở thành trang riêng.
- Trình duyệt hỗ trợ cho Community (DEC-082): Chrome, Edge, Firefox, Safari trên máy tính; Chrome Android, Safari iOS. Phiên bản và thiết bị cụ thể còn mở (OQ-007). Media v1 chỉ trên máy tính.
- Người phụ trách: Vg (giao diện Community, quyền, vòng đời phòng); Sáng (tin phòng và giao diện liên quan). Xem [phân công](../../project/planning.md#team).

<a id="tests"></a>

## Ca kiểm thử chung

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-23"></a> TC-COM-23 | Mất phản hồi khi POST tạo; gửi lại cùng và khác nội dung; tài nguyên sau đó bị xóa | Một tài nguyên cho mỗi thao tác; khác nội dung thì xung đột; gửi lại không hồi sinh tài nguyên đã xóa | Chống trùng khi tạo |
| <a id="tc-com-26"></a> TC-COM-26 | Migration với tên cũ, trùng tên, vai trò hệ thống, danh mục quyền, trigger version | Không tự đổi hay xóa dữ liệu thật; mỗi tài nguyên một nơi tăng version; schema khớp writer | COM-SQL-01 đến 10 |

Dữ liệu, kết quả và giới hạn bằng chứng xem [hồ sơ Community](../../records/verification/community/README.md); cách chạy xem [hướng dẫn Community](../../guides/development.md#community).

---

Nguồn: DEC-051, DEC-070, DEC-081, DEC-082, DEC-083, DEC-092, DEC-093, DEC-095, DEC-098, DEC-099, DEC-105, [DEC-171](../../records/decisions/README.md#dec-171).

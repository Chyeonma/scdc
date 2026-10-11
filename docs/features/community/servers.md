# Cộng đồng và chủ sở hữu

Community / Servers sở hữu metadata, visibility, discovery, join mode và ownership. Quy tắc, luồng sử dụng, UX, thiết kế dữ liệu/API và tiêu chí kiểm chứng của chủ đề được quản lý tại trang này.

<a id="implementation"></a>

## Trạng thái theo khả năng

Đối chiếu hiện trạng ngày 2026-10-08. Nền Community đã hợp nhất vào `main`; bằng chứng dẫn dưới đây gắn revision cụ thể, không phải kết quả chạy lại cho mọi thay đổi sau đó.

| Khả năng | Trạng thái và giới hạn | Bằng chứng |
|---|---|---|
| Tạo cộng đồng — UC-COM-01 | Backend/WebClient đã kiểm chứng | [Tạo/xem](../../records/verification/community/create-view.md) |
| Danh sách/detail/tư cách — phần UC-COM-03 | Đã kiểm chứng phần đọc; chưa đóng đối soát leave/rejoin và tin phòng | [Tạo/xem](../../records/verification/community/create-view.md) |
| Tìm kiếm/public summary — UC-COM-02 | Đã có discovery → public/immediate join; nhánh approval chưa có | [Tìm kiếm](../../records/verification/community/search.md) |
| Sửa metadata/visibility, join mode, chuyển owner — UC-COM-04/05/14 | Thiết kế mục tiêu; chưa triển khai | [Kế hoạch](../../project/planning.md#community-work-items) |

Quản lý tư cách tham gia tại [Memberships](memberships.md); quyền quản lý tại [Access control](access-control.md).

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
| <a id="com-002"></a> COM-002 | Chỉ cộng đồng công khai xuất hiện trong kết quả tìm kiếm. | DEC-024 |
| <a id="com-003"></a> COM-003 | Cộng đồng công khai mới tạo mặc định cho vào ngay; có thể cấu hình chờ duyệt. | DEC-025 |
| <a id="com-006"></a> COM-006 | Chủ sở hữu hoặc người được cấp quyền có thể đổi chế độ vào ngay/chờ duyệt. | DEC-029 |
| <a id="com-029"></a> COM-029 | Tài khoản đã xác minh được tạo cộng đồng; tên 2–100, mô tả tối đa 1.000; chọn công khai/riêng tư, mặc định công khai. Chủ sở hữu sửa các trường này. | DEC-072 |
| <a id="com-033"></a> COM-033 | Chủ sở hữu chuyển ngay cho thành viên đã xác minh/active, không cần người nhận chấp nhận; chủ cũ vẫn là thành viên và chỉ được rời sau chuyển. | DEC-076 |
| <a id="com-038"></a> COM-038 | Search chỉ cộng đồng công khai theo một phần tên, q 2–100 UTF-16; case-insensitive/accent-sensitive, tên khớp đúng trước; trang 20/tối đa 50. Trường tên/mô tả/chủ đề đếm UTF-16. | DEC-093 |
| <a id="com-039"></a> COM-039 | Tên Unicode được trim và không trống/vô hình; tên server được trùng, tên phòng/vai trò unique trong server theo case-insensitive/accent-sensitive. v1 chưa có xóa server. | DEC-094/095 |
| <a id="com-040"></a> COM-040 | Chuyển sang private hủy pending join requests và báo lý do; giữ member và lời mời hợp lệ. | DEC-096 |

<a id="use-cases"></a>

<a id="use-case"></a>

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](../../system/community.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-01"></a>

<a id="uc-com-01--tạo-cộng-đồng"></a>

### UC-COM-01 — Tạo cộng đồng

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (tài khoản/phiên), Memberships/Permissions trong Community và WebClient (giao diện).

**Tác nhân:** Người dùng đủ điều kiện ứng dụng.

**Điều kiện trước:** Phiên hợp lệ; người dùng chưa cần là thành viên cộng đồng nào.

**Kích hoạt:** Người dùng chọn tạo cộng đồng tại COM-S10.

**Luồng chính:**

1. Người dùng nhập tên, mô tả tùy chọn và chọn public/private theo [COM-029](#com-029), [COM-039](#com-039).
2. Hệ thống kiểm tra dữ liệu và quyền, tạo server, membership owner và @everyone trong cùng transaction; ghi khóa thao tác tạo theo thiết kế retry.
3. Sau commit, giao diện mở cộng đồng vừa tạo và tải thông tin/tư cách hiện hành.

**Ngoại lệ:** Dữ liệu không hợp lệ trả lỗi trường. Create đồng thời hoặc thử lại cùng khóa/payload chỉ có một server. Khi mất response, người dùng thử lại bằng khóa cũ; xung đột payload không tạo server khác. Không để lại server thiếu owner hoặc @everyone khi rollback.

**Kết quả sau cùng:** Người tạo là owner và thành viên active; public mới mặc định vào ngay. Use case tạo server không yêu cầu tự tạo phòng đầu tiên; tạo phòng thực hiện bằng [UC-COM-16](channels.md#uc-com-16).

<a id="uc-com-02"></a>

<a id="uc-com-02--tìm-và-xem-cộng-đồng-công-khai"></a>

### UC-COM-02 — Tìm và xem cộng đồng công khai

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Người dùng đủ điều kiện ứng dụng, gồm người chưa tham gia cộng đồng đích.

**Điều kiện trước:** Phiên hợp lệ; người dùng mở khu vực khám phá.

**Kích hoạt:** Người dùng tìm theo tên hoặc mở kết quả công khai tại COM-S01/02.

**Luồng chính:**

1. Người dùng nhập từ khóa; hệ thống kiểm tra và tìm literal theo [COM-038](#com-038), lọc public/active trên từng trang.
2. Giao diện hiển thị summary công khai, ưu tiên tên khớp đúng và cho tải trang tiếp theo.
3. Người dùng mở summary, xem join mode hiện hành rồi chọn [UC-COM-06](memberships.md#uc-com-06) hoặc [UC-COM-07](memberships.md#uc-com-07).

**Ngoại lệ:** Query/cursor không hợp lệ trả lỗi; không có kết quả hiển thị trạng thái rỗng. Server đã đổi private không xuất hiện từ cursor cũ, người ngoài mở trực tiếp nhận 404. Preview lời mời hợp lệ dùng [UC-COM-11](invitations.md#uc-com-11), [UC-COM-13](invitations.md#uc-com-13).

**Kết quả sau cùng:** Chỉ xem thông tin được công khai; chưa có membership, phòng hoặc nội dung tin.

<a id="uc-com-03"></a>

<a id="uc-com-03--xem-cộng-đồng-đang-tham-gia-và-tư-cách-của-mình"></a>

### UC-COM-03 — Xem cộng đồng đang tham gia và tư cách của mình

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Người dùng đủ điều kiện ứng dụng.

**Điều kiện trước:** Phiên hợp lệ; quyền đối với server đích được kiểm tra khi đọc.

**Kích hoạt:** Người dùng mở danh sách cộng đồng, chọn một server hoặc kiểm tra trạng thái sau thao tác có kết quả không rõ.

**Luồng chính:**

1. Hệ thống trả danh sách server người dùng đang là thành viên active, có phân trang.
2. Khi chọn server, hệ thống trả member detail, membershipId/version và quyền quản lý hiệu lực theo tư cách hiện hành.
3. Giao diện hiển thị thao tác phù hợp và tải các phòng qua [UC-COM-17](channels.md#uc-com-17); khi cần đối soát join/leave, người dùng đọc tư cách của chính mình.

**Ngoại lệ:** Chưa tham gia server nào hiển thị danh sách rỗng. Sau leave, server không còn trong danh sách active nhưng người dùng vẫn đọc được membership đã left của chính mình theo contract. Quyền đọc status không cấp lại member detail/private content. Danh sách thay đổi được dedup ID và tải lại trang đầu.

**Kết quả sau cùng:** Giao diện có trạng thái tham gia và quyền hiện hành, không tạo hoặc phục hồi membership bằng thao tác đọc.

<a id="uc-com-04"></a>

<a id="uc-com-04--sửa-thông-tin-và-visibility-cộng-đồng"></a>

### UC-COM-04 — Sửa thông tin và visibility cộng đồng

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hiện hành.

**Điều kiện trước:** Điều kiện nền hợp lệ; đã tải server và version. Quyền owner kiểm tra lại khi ghi.

**Kích hoạt:** Owner mở COM-S10 và lưu tên, mô tả hoặc visibility mới.

**Luồng chính:**

1. Owner chỉnh các trường theo [COM-029](#com-029), [COM-039](#com-039) và gửi kèm expectedVersion.
2. Hệ thống kiểm tra quyền/dữ liệu/version rồi cập nhật. Nếu public→private, đồng thời chuyển mọi join request còn pending sang cancelled với reason server_private theo [COM-040](#com-040).
3. Sau commit, trả server hiện hành; thông báo cho sender về request bị hủy và yêu cầu các client đủ quyền tải lại metadata.

**Ngoại lệ:** Actor mất ownership hoặc version cũ không được ghi đè. Approve và private switch tranh nhau có một thứ tự commit: membership đã tạo trước switch được giữ, request đã bị hủy không approve được. Private→public không tự mở lại request terminal.

**Kết quả sau cùng:** Metadata/visibility và trạng thái request nhất quán; thành viên hiện có và lời mời hợp lệ được giữ. Xóa toàn bộ server nằm ngoài v1 theo [COM-039](#com-039).

<a id="uc-com-05"></a>

<a id="uc-com-05--đổi-chế-độ-tham-gia"></a>

### UC-COM-05 — Đổi chế độ tham gia

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (tài khoản/phiên), WebClient (giao diện).

**Tác nhân:** Owner hoặc thành viên có manage_join_mode.

**Điều kiện trước:** Server/membership active, phiên hợp lệ; đã tải version và join mode.

**Kích hoạt:** Actor chọn vào ngay/chờ duyệt tại COM-S07.

**Luồng chính:**

1. Actor chọn join mode mới và gửi expectedVersion.
2. Hệ thống kiểm tra manage_join_mode/version và cập nhật server.
3. Sau commit, giao diện tải lại chế độ; lần join công khai tiếp theo xét mode hiện hành.

**Ngoại lệ:** Mất quyền hoặc version cũ từ chối thao tác. Quyền đổi join mode không cấp quyền sửa tên/mô tả/visibility. Đổi mode không tự duyệt request cũ; review thực hiện qua [UC-COM-08](memberships.md#uc-com-08) theo trạng thái pending.

**Kết quả sau cùng:** Join công khai áp dụng mode mới; link hợp lệ vẫn cho vào ngay theo [COM-004](invitations.md#com-004). Server private vẫn yêu cầu đường tham gia bằng lời mời.

<a id="uc-com-14"></a>

<a id="uc-com-14--chuyển-chủ-sở-hữu"></a>

### UC-COM-14 — Chuyển chủ sở hữu

**Module phụ trách:** Community / Servers.

**Phối hợp:** Identity (actor/target), Memberships trong Community và WebClient (giao diện).

**Tác nhân:** Owner hiện hành; target là thành viên active/đã xác minh.

**Điều kiện trước:** Actor và target đủ điều kiện, target còn membership active; owner đã tải server version.

**Kích hoạt:** Owner chọn người nhận và xác nhận chuyển ngay tại COM-S12.

**Luồng chính:**

1. Owner chọn target theo ID từ roster được phép biết và gửi expectedVersion.
2. Hệ thống kiểm tra lại actor/target/membership/version dưới cùng transaction rồi đổi owner và phiên bản quyền.
3. Sau commit, trả server hiện hành; giao diện hai bên tải lại quyền. Chủ cũ vẫn là thành viên và có thể thực hiện [UC-COM-15](memberships.md#uc-com-15).

**Ngoại lệ:** Target đã rời hoặc không đủ điều kiện thì chuyển thất bại. Target leave và transfer được tuần tự hóa: leave trước chặn transfer, transfer trước biến target thành owner không được leave. Actor mất ownership/version cũ phải tải lại; không tự thử lại sau kết quả không rõ.

**Kết quả sau cùng:** Luôn đúng một owner, chuyển có hiệu lực ngay và không cần target accept. Chủ cũ giữ role hiện có, không tự được gán role quản lý để thay quyền owner.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## UX và trạng thái

```text
COM-S01 · Khám phá                 COM-S02 · Trang cộng đồng / lời mời
┌───────────────────────────┐     ┌─────────────────────────────────┐
│ Tìm cộng đồng [         ] │     │ Tên cộng đồng                   │
│                           │     │ Mô tả                           │
│ Tên cộng đồng công khai   │     │                                 │
│ Mô tả ngắn          [Xem] │     │ [Tham gia] hoặc [Gửi yêu cầu]    │
│                           │     │ hoặc “Yêu cầu đang chờ duyệt”    │
└───────────────────────────┘     └─────────────────────────────────┘
```

Tìm kiếm chỉ hiện cộng đồng công khai. Từ tìm kiếm, nút tham gia tuân
chế độ vào ngay/chờ duyệt. Liên kết mời hợp lệ mở trang xác nhận đúng
cộng đồng rồi cho vào ngay; không chuyển thành chờ duyệt. Liên kết
hết hạn/thu hồi/không hợp lệ không có nút tham gia hoạt động và không
hiển thị nội dung riêng tư. Chưa là thành viên thì không tải tin phòng.

COM-S10 Tạo/sửa cộng đồng theo DEC-072: thu tên/mô tả/công khai; chủ sở hữu mới được sửa.

COM-S12 Chuyển chủ sở hữu: chọn thành viên active/đã xác minh, xác nhận chuyển ngay; chủ cũ vẫn là thành viên và được rời sau đó.

| Màn hình | Người thực hiện | Nội dung và trạng thái cần thiết |
|---|---|---|
| COM-S07 Chế độ tham gia | Chủ sở hữu/người có quyền đổi chế độ | Vào ngay/chờ duyệt; ghi rõ liên kết mời hợp lệ vẫn cho vào ngay |

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](../../system/community.md#ux).


<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

### Metadata server và ownership — thiết kế mục tiêu

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `PATCH /servers/{id}` | `{name?,description?,visibility?,expectedVersion}` | 200 server; chỉ owner; private không xuất hiện trong tìm kiếm |
| `PATCH /servers/{id}/join-mode` | `{joinMode,expectedVersion}` | 200 server; đúng quyền đổi chế độ, không vô hiệu nguyên tắc link cho vào ngay |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId,expectedVersion}` | 200 server; owner hiện tại, target là thành viên active/verified; chuyển ngay, không tạo pending |

- Public → private: update visibility và chuyển mọi pending request sang cancelled với reason server_private trong cùng transaction theo DEC-096; không xóa lịch sử request. Sender vẫn đọc được trạng thái của chính mình và nhận thông báo lý do. Existing members/invites giữ nguyên; join request cũ không được approve sau commit private.
- Chuyển owner: khóa actor/target theo thứ tự rồi server/membership; target còn active/verified và là member tại commit. Owner field là nguồn chuẩn duy nhất; target rời trước thì transfer thất bại, transfer trước thì target thành owner không được rời. Chủ cũ chỉ rời sau chuyển đã commit.
<a id="create-models"></a>

<a id="create-model-và-trách-nhiệm"></a>

### Model và trách nhiệm

| Model/thành phần | Dữ liệu hoặc trách nhiệm được chọn |
|---|---|
| Server / Servers | `id`, `ownerUserId`, `name`, `description`, `visibility`, `joinMode`, `status`, timestamps, `version`, `accessVersion`; ownership chỉ lấy từ server |
| Membership / Memberships | `(serverId,userId)` duy nhất, `membershipId`, active/left, joined/left timestamps, `version`; không dựng membership từ thao tác GET |
| Role / Permissions | Khởi tạo một @everyone: `isDefault=true`, `isSystem=true`, không management permission. Tư cách owner tính từ server, không tạo role owner |
| CommunityOperation / Infrastructure | Khóa actor/kind/scope/client, fingerprint version/key/hash, resource ID, thời điểm commit; thao tác tạo server dùng kind `create_server` và scope UUID zero |
| CommunityDbContext / Infrastructure | Mapping các bảng Community cần cho gói; writer dùng connection/transaction chung. Reader không tracking, trả DTO từ query có quyền |
| Identity guard / Contracts + Identity | Actor gồm user/session/security stamp từ claims đã xác thực; Identity đọc schema của mình trong transaction được truyền vào |
| Relational work scope / BuildingBlocks | Sở hữu một connection/transaction, enlist DbContext, commit/rollback và dispose; không chứa nghiệp vụ Community/Identity |
| Transactional outbox / BuildingBlocks | Ghi envelope vào `integration.outbox_events` cùng transaction; đây là hạ tầng dùng chung, không cho Community ghi schema Messaging/Identity |
| API host | Controller xác định actor, ánh xạ request/Result/Location; orchestration nghiệp vụ nằm trong Community |

ID nghiệp vụ và membership dùng UUIDv7; `clientOperationId` phải là UUIDv4 có đúng variant RFC. Timestamps UTC. Version trên wire là chuỗi số nguyên dương. Gói giữ `servers.version` integer và trigger hiện có; `access_version` và `server_members.version` bổ sung cũng là integer dương. Tất cả khởi tạo 1; tạo owner/@everyone là snapshot ban đầu, không tăng accessVersion thành nhiều lần. Cận của schema wire không làm mở rộng cận integer trong DB; xử lý tăng version ở các mutation sau phải kiểm tra overflow.

Owner nhận năm management code trong [catalogue](access-control.md#permission-detailed-design) và quyền ownership riêng. Với thành viên khác, projection chỉ hợp các code được catalogue hỗ trợ từ custom role được gán; không suy grant từ @everyone hoặc mã permission legacy. Read model lấy assignment đúng epoch và policy hiện hành; writer role/ACL được mô tả tại [Access control](access-control.md).

<a id="create-api"></a>

<a id="create-api-dữ-liệu-và-lỗi"></a>

### API, dữ liệu và lỗi

Prefix `/api/v1`. [OpenAPI Community](../../contracts/community.openapi.json) giữ schema máy đọc được; bốn thao tác dưới đây đã có bằng chứng trong gói tạo/xem; các route mục tiêu khác được phân biệt trong bảng trạng thái đầu trang.

| Thao tác | Đầu vào | Kết quả được chọn |
|---|---|---|
| `POST /servers` | `clientOperationId`, `name`, `description?`, `visibility?` | 201 + `ServerDetail` + Location khi tạo mới; 200 + detail hiện hành + cùng Location khi retry hợp lệ. Chỉ trả thành công sau commit |
| `GET /servers` | `cursor?`, `limit?` | 200 `MyServerPage`; chỉ server active/nondeleted có membership active của actor; limit mặc định 20, nhận 1–50 |
| `GET /servers/{serverId}` | UUID tài nguyên | Member active nhận `ServerDetail`; người ngoài public nhận `ServerSummary`; private hoặc server không được biết nhận 404 |
| `GET /servers/{serverId}/membership/me` | UUID tài nguyên; actor từ phiên | Chỉ membership của actor; không có record thì 404. Khi record đã left, vẫn trả status của chính mình kể cả server private, không trả private detail hoặc phục hồi membership |

`ServerSummary` chỉ có `id,name,description,visibility,joinMode,version`; `ServerDetail` thêm `ownerUserId,accessVersion,myMembership,effectivePermissions`. Route detail dùng chung public/member view theo contract; kiểm chứng projection public để tránh lộ dữ liệu. Search/discovery được mô tả riêng bên dưới. Own-left được kiểm chứng bằng fixture; bằng chứng đó không chứng minh leave API đã triển khai.

Thứ tự validation được chọn:

1. Kiểm tra JSON/field/enum/UUID; không nhận owner, actor, slug, version hoặc trạng thái do client chỉ định. Visibility bỏ trống thành public; private cũng lưu joinMode immediate, nhưng visibility vẫn chặn đường tham gia trực tiếp.
2. Tên: kiểm tra Unicode hợp lệ, trim theo tập White_Space của [text-policy](../../fixtures/text-policy.json), đếm 2–100 UTF-16; từ chối control, CR/LF, U+2028/U+2029 và tên chỉ trắng/vô hình. Giữ case, tiếng Việt, emoji và các scalar ignorable trong tên có nội dung; không NFC tên hiển thị. `normalized_name` legacy không thay key `search_name` .NET dùng cho tìm kiếm.
3. Mô tả: văn bản thuần, chuẩn hóa CRLF/CR thành LF; không trim nội dung có chữ, không NFC; bỏ trống/null/chuỗi rỗng thành null, kiểm tra Unicode/NUL và tối đa 1.000 UTF-16 sau chuẩn hóa. Cùng thứ tự chuẩn hóa ở UI/backend và fingerprint; không chép giới hạn 2.000 của nội dung tin sang mô tả.
4. Sinh slug nội bộ từ UUID dạng 32 ký tự hex viết thường; không dùng tên làm định danh hoặc yêu cầu client nhập slug. Dữ liệu chuẩn hóa mới được đưa vào writer/fingerprint.

| HTTP / errorCode được chọn | Tình huống và hành vi |
|---|---|
| 400 `VALIDATION_FAILED` | Field/Unicode/độ dài/enum/UUID không hợp lệ; ProblemDetails có `errors` theo field, giữ form |
| 400 `CURSOR_INVALID` | Cursor sai actor/purpose/limit, bị sửa hoặc hết hạn; tải lại từ trang đầu |
| 401 | JWT middleware dùng lỗi chuẩn host; guard phát hiện session/stamp không hợp lệ trả `SESSION_INVALID` |
| 403 `ACCOUNT_ACCESS_DENIED` | Tài khoản không active/nondeleted hoặc chưa có primary email đã xác minh; không ghi dữ liệu |
| 403 `PERMISSION_DENIED` | Retry resource public đã biết nhưng actor không còn membership active để nhận detail; private không còn được biết dùng 404. Không tạo lại server |
| 404 `RESOURCE_NOT_FOUND` | Không tồn tại, không được biết hoặc không có membership riêng; không mang metadata bị ẩn |
| 409 `OPERATION_CONFLICT` | Cùng khóa tạo nhưng payload chuẩn hóa khác; không tạo/sửa tài nguyên |
| 503 `FINGERPRINT_KEY_UNAVAILABLE` / `CURSOR_KEY_UNAVAILABLE` | Không đọc được khóa cần dùng; không bỏ qua so sánh hoặc giả conflict |
| 503 `ACCESS_CHECK_UNAVAILABLE` / `COMMUNITY_TEMPORARILY_UNAVAILABLE` | Guard/DB tạm lỗi, lock timeout, deadlock hoặc không xác nhận commit; UI giữ thao tác, người dùng chủ động đối soát/thử lại bằng khóa cũ |

Không chuyển mọi lỗi DB thành validation/409. Lỗi constraint do writer vi phạm invariant hoặc lỗi không dự kiến dùng cơ chế 500 của host; chi tiết SQL chỉ ở log nội bộ. Request bị hủy không tiếp tục thao tác mới. Timeout sau khi commit có kết quả không rõ; operation record là nguồn đối soát, không hứa rollback nếu commit đã được DB chấp nhận.

### Giao dịch tạo và đọc

[Work scope và Identity guard](../../system/community.md#account-transactions) dùng chung cho các thao tác dưới đây.

Thứ tự tạo:

1. Normalize/validate, mở work scope và giữ Identity guard. Tra operation theo actor + `create_server` + UUID zero + client ID.
2. Nếu operation đã có, dùng fingerprint version/key đã lưu để so payload; đúng thì kiểm tra quyền và đọc tài nguyên hiện hành, không ghi lại operation/outbox; khác thì 409. Thiếu resource là lỗi nhất quán, không tạo lại server bằng khóa cũ.
3. Nếu chưa có, lấy active HMAC key, sinh server/membership/role/event ID và ghi server + owner membership + @everyone + operation + outbox trong cùng transaction. Khóa đầu vào mới không dùng để khóa một server chưa tồn tại.
4. Hai create cùng operation có thể cùng chạy sau lookup. Unique operation xác định một bên thắng. Bên gặp đúng constraint duplicate phải rollback **toàn bộ** transaction/DbContext, mở transaction mới, lấy lại guard và đọc operation đã commit để xử lý retry; không chỉ rollback savepoint rồi giữ server thứ hai. Không diễn giải constraint khác thành retry.
5. Kiểm tra thời gian/điều kiện guard, commit, trả DTO/Location. Deadlock/timeout trả lỗi tạm; không tự replay mutation bằng execution strategy. Client retry chủ động giữ nguyên khóa và payload chuẩn hóa.

Read dùng guard và một SQL projection cho toàn bộ metadata/membership/permissions của trang/detail, để quyết định quyền và dữ liệu Community cùng snapshot của statement. Nếu reader phải dùng nhiều statement, phải thay bằng transaction snapshot phù hợp và kiểm chứng trước merge; không ghép kết quả từ những thời điểm quyền khác nhau. Mỗi trang kiểm tra lại quyền, không lấy quyền từ cursor. `READ COMMITTED` tạo snapshot cho từng statement; cách dùng một projection là lựa chọn của gói để giữ dữ liệu nhất quán. [PostgreSQL isolation](https://www.postgresql.org/docs/18/transaction-iso.html#XACT-READ-COMMITTED).

Outbox ghi event nội bộ `Community.ServerCreated.v1`, aggregate server/version, payload tối thiểu server ID/owner ID/accessVersion; chỉ có một event cho một thao tác tạo. Hạ tầng outbox dùng schema integration chung. Gói chỉ ghi bền, chưa có dispatcher/Hub; UI cập nhật từ HTTP rồi đọc lại. Event chờ không được đánh dấu published khi chưa phát; gói realtime bổ sung delivery/routing riêng.

### Cursor danh sách của mình

List của mình sort `server.id DESC`, keyset `id < lastServerId`, lấy limit + 1 và chỉ active server/member. Cursor bảo vệ bằng Data Protection, purpose `Community.MyServers.v1`, gồm actor ID, limit, lastServerId, expiry 24 giờ và version format. Next cursor chỉ sinh khi còn trang. Không dùng offset hoặc thời điểm tên thay đổi làm vị trí sort; concurrent join/leave vẫn có thể đổi tập kết quả, UI dedup ID/refresh từ đầu.
<a id="create-migration"></a>

<a id="create-migration-và-dữ-liệu-legacy"></a>

### Migration và dữ liệu legacy

Migration 001 được lưu tại `database/postgres/migrations/001-community-create-view.sql`. Runner explicit ghi module/version/checksum/appliedAt vào ledger `common.schema_migrations`, khóa ledger khi apply, transaction cho migration và dừng khi checksum khác. Không gọi `EnsureCreated` hoặc chạy bootstrap từ API startup. Runner/SQL/ledger đã có bằng chứng trong hồ sơ tạo/xem; không diễn giải điều đó thành đã nâng cấp DB ứng dụng.

| Bảng/phần | Thay đổi được chọn cho migration gói |
|---|---|
| servers | Thêm visibility public/private, join_mode immediate/approval, access_version ≥1; description rộng đến 1.000. Giữ ID/status/slug/version/trigger hiện có; server mới ghi active, public/default request và immediate |
| name/description | Bổ sung `common.utf16_length(text)` immutable cho DB UTF-8, đếm Unicode scalar ngoài BMP thành 2 đơn vị. Thay check tên server bằng 2–100 UTF-16 sau normalize; description ≤1.000 UTF-16. Service vẫn kiểm tra blank/control/Unicode theo policy |
| server_members | Thêm membership_id UUIDv7 duy nhất và version ≥1; giữ unique `(server_id,user_id)`. Backfill epoch cho record hiện có được duyệt; không đổi kicked/banned thành active/left tự động |
| owner invariant | FK deferred từ `(servers.id,owner_user_id)` tới `(server_members.server_id,user_id)` và constraint trigger deferred kiểm tra owner membership active tại commit. Create được ghi server trước member, kiểm tra cuối transaction |
| @everyone invariant | Giữ unique default-role index; constraint trigger deferred đảm bảo mỗi server có đúng một default/system @everyone, owner member tồn tại; check role hệ thống chỉ là default @everyone và không có management grant. Trigger theo dõi server/member/role/role_permissions để không có đường sửa bỏ invariant |
| operations | Thêm bảng Community operation với composite PK, fingerprint/key/version/resource/createdAt và checks cần thiết; baseline 001 chỉ nhận create_server/scope zero; các migration 003/004 mở rộng operation kinds |
| indexes | Dùng active membership index `(user_id,server_id)` và server PK cho list keyset; bổ sung index membership_id. Không thay key/index search/channel/role ngoài phần gói cần |
| outbox | Dùng bảng integration.outbox_events hiện có qua hạ tầng chung; aggregate_version vẫn tương thích integer của server; chưa thêm lease/dispatcher/realtime |

`char_length` PostgreSQL đếm ký tự, không phải UTF-16. Hàm UTF-16 được chọn có giá trị `char_length + số scalar > U+FFFF`; DB UTF-8 không nhận surrogate không hợp lệ, service vẫn phải chặn trước. Ví dụ tên chỉ một emoji ngoài BMP có 2 UTF-16 và phải vượt qua cận tối thiểu; constraint legacy trước migration không xử lý được trường hợp này. Fixture .NET/SQL kiểm chứng tính nhất quán. [PostgreSQL string functions](https://www.postgresql.org/docs/18/functions-string.html).

Preflight phải hoàn thành trước thay constraint/backfill:

1. Kiểm tra owner có membership active, tên/mô tả hợp lệ theo policy, membership epoch/status, default/system role/grant và collision khi chuyển tên default role thành @everyone. Báo ID và lỗi cụ thể; dừng trên dữ liệu không có mapping được duyệt.
2. Cột visibility/joinMode cũ chưa tồn tại nên không suy public cho mọi server legacy. Backfill qua mapping theo server ID được duyệt; DB không có server thì không cần mapping. Server mới lấy default từ request, không lấy lựa chọn backfill của dữ liệu cũ.
3. Không tự sửa tên, xóa role/grant, đổi trạng thái thành viên hoặc mở visibility. Seed demo legacy cũng phải có mapping hoặc thay bằng seed mới ở bootstrap Development. Không chạy schema.sql có DROP SCHEMA trên DB cần giữ dữ liệu.
4. Apply/validate trong transaction, lưu ledger; chạy lại cùng checksum là no-op. Deployment phải có bản backup/preflight và kế hoạch quay lại trước migration. Sau khi có write mới, giữ schema mở rộng khi rollback app; không tự DROP cột/bảng hoặc hạ description xuống 500 làm mất dữ liệu.

Bootstrap `schema.sql`/`seed.sql` cho DB mới được đồng bộ với schema sau chuỗi migration đã áp dụng, ledger ghi cùng baseline/checksum để không apply lại. Test migration vẫn bắt đầu từ snapshot schema cũ/fixture legacy riêng để chứng minh đường upgrade; cập nhật bootstrap không thay bằng chứng migration.

### Tìm kiếm và discovery

<a id="search-key-tên-và-migration"></a>

### Key tên và migration

`UnicodeTextPolicy.NormalizeNameKey` dùng trim theo text-policy → NFC → ToLowerInvariant. Không đổi tên hiển thị và không bỏ dấu. NFC chuẩn hóa các biểu diễn Unicode tương đương; ToLowerInvariant dùng casing invariant theo [.NET Normalize](https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize?view=net-10.0), [.NET ToLowerInvariant](https://learn.microsoft.com/en-us/dotnet/api/system.string.tolowerinvariant?view=net-10.0).

Thêm `community.servers.search_name text COLLATE "C" NOT NULL`; không dùng `normalized_name` generated theo locale PostgreSQL làm key .NET. Create ghi key cùng tên trong transaction; writer sửa tên tương lai phải cập nhật cả hai. So sánh/sort key theo C để giữ dấu và thứ tự byte ổn định, theo [PostgreSQL collation](https://www.postgresql.org/docs/18/collation.html). Exact rank 0, partial rank 1, key tăng dần rồi UUID tăng dần.

Migration `002-community-search.sql` dùng temp mapping key do runner .NET tính theo batch tối đa 500 record. Cùng advisory lock ledger của 001 và table lock, kiểm tra prerequisite 001/checksum, validate tên theo policy, apply DDL/backfill trong transaction; không đổi tên/metadata version/accessVersion/epoch. Bootstrap hiện hành cho DB mới ghi ledger 001–004; seed có key explicit. Không đổi migration/checksum 001 đã áp dụng.

Backfill tạm tắt riêng trigger tăng version/updated_at. Constraint triggers owner/@everyone vẫn bật; sau UPDATE, `SET CONSTRAINTS ALL IMMEDIATE` kiểm tra các event đang chờ trước ALTER TABLE tiếp theo, rồi khôi phục DEFERRED trong transaction. Cơ chế kiểm tra hồi tố theo [PostgreSQL SET CONSTRAINTS](https://www.postgresql.org/docs/18/sql-set-constraints.html); lỗi vẫn rollback cả DDL, dữ liệu và trạng thái trigger.

Apply migration 002 với writer cũ đã dừng/drain trước deploy writer mới: NOT NULL search_name khiến writer tạo server cũ không còn tương thích. API không tự migrate. Dữ liệu cần sửa được báo ID và rollback, không sửa âm thầm. Không chạy bootstrap có DROP SCHEMA lên DB cần giữ dữ liệu. Khi rollback app, giữ schema và dùng writer tương thích; không xóa cột/backfill.

Partial btree `(search_name,id)` chỉ public/active/nondeleted hỗ trợ filter/keyset; contains với leading wildcard vẫn có thể quét/sort nhiều kết quả. Gói chưa có workload/load proof, không hứa index này đủ cho mọi quy mô hoặc thêm pg_trgm trước khi đo.

<a id="search-api-và-cursor"></a>

### API và cursor

GET `/api/v1/servers/search`: q bắt buộc, trim rồi validate 2–100 UTF-16/Unicode hợp lệ/có nội dung/một dòng trước normalize; limit 1–50, mặc định 20. 200 `{items: ServerSummary[],nextCursor}`. Luôn sáu field summary, không owner/membership/quyền/phòng. Search không tạo membership/event.

LIKE dùng parameter và escape `%`, `_`, backslash, không coi user input là pattern/SQL theo [PostgreSQL LIKE](https://www.postgresql.org/docs/18/functions-matching.html). Một SQL statement lọc public/active/nondeleted và keyset `(rank,key,id)`, lấy limit+1; không OFFSET hoặc tải toàn bộ server lên RAM ứng dụng.

Cursor purpose `Community.Search.v1`, payload version/actor/queryKey/limit/lastRank/lastKey/lastId/expiry 24 giờ. Q khác cách viết nhưng cùng normalized key dùng được cursor; actor/queryKey/limit/purpose khác, tamper/expiry/position lỗi trả 400 CURSOR_INVALID. Key ring dùng chung cơ chế bền của list nhưng purpose riêng; lỗi storage trả 503 CURSOR_KEY_UNAVAILABLE.

Identity guard giữ user share lock/session lease trong shared work scope; đọc Community bằng một statement snapshot, kiểm tra lease trước hoàn tất read. Mỗi trang kiểm tra lại public/active; cursor không cấp quyền cố định. Public summary đã tải có thể cũ đến lần refresh; mở detail/join luôn kiểm tra trạng thái hiện hành, chưa có realtime xóa cache public.

Lỗi: 400 VALIDATION_FAILED theo q/limit; 401 middleware hoặc SESSION_INVALID; 403 ACCOUNT_ACCESS_DENIED; 400 CURSOR_INVALID; 503 ACCESS_CHECK_UNAVAILABLE/CURSOR_KEY_UNAVAILABLE/COMMUNITY_TEMPORARILY_UNAVAILABLE. Không chuyển dependency failure thành danh sách rỗng.

<a id="search-webclient"></a>

### WebClient

Nút Khám phá từ own list mở `#discover`; submit hợp lệ lưu q trong hash URL `#discover?q=...`, chỉ GET. Empty query hiển thị hướng dẫn, không gọi API search. Input validate cùng text-policy/UTF-16 trước submit, không cắt hoặc lowercase tên người dùng nhập.

Kết quả có tên/mô tả/visibility/joinMode và nút Xem. Mở detail giữ đích quay lại kết quả/cùng từ khóa; public/immediate dùng nút join của gói trước, approval chỉ giải thích trạng thái tới khi có requests. Own list được tải lại sau join.

Query/actor/route đổi abort request và bỏ response cũ; state kết quả gắn query đã submit và actor. Pagination dedup UUID. Lỗi page/cursor bỏ kết quả cũ, cho tải lại từ trang đầu; không ghép trang của hai query/cursor. Reload URL tự đọc lại GET từ trang đầu; không tự POST join. Browser không giữ kết quả vào storage.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-01"></a> AC-COM-01 | Một cộng đồng công khai và một cộng đồng riêng tư tồn tại; người dùng tìm cộng đồng. | Kết quả chỉ hiện cộng đồng công khai. |
| <a id="ac-com-08"></a> AC-COM-08 | Chủ sở hữu hoặc người được cấp quyền đổi chế độ tham gia. | Cách tham gia qua tìm kiếm áp dụng chế độ mới; liên kết mời hợp lệ vẫn cho vào ngay. |
| <a id="ac-com-29"></a> AC-COM-29 | Tài khoản đã xác minh tạo cộng đồng, chủ sở hữu sửa tên/mô tả/công khai; thành viên gọi cùng API sửa | Tạo/sửa theo giới hạn DEC-072; thành viên không có quyền sửa; riêng tư không còn xuất hiện trong tìm kiếm |
| <a id="ac-com-33"></a> AC-COM-33 | Owner chuyển cho thành viên active/verified trong lúc target/chủ cũ thử rời | Chuyển ngay, không cần nhận chấp nhận; luôn đúng một owner; chủ cũ được rời sau chuyển thành công |
| <a id="ac-com-37"></a> AC-COM-37 | Tìm tên Unicode có/không dấu, biên q và phân trang; đổi server public sang private giữa các trang | Đúng q 2–100/trang 20–50/exact-first; case-insensitive/accent-sensitive; private không xuất hiện từ cursor cũ theo DEC-093 |
| <a id="ac-com-38"></a> AC-COM-38 | Tên có emoji/tiếng Việt, khoảng trắng, trống/vô hình, hai tên chỉ khác case/dấu | Đếm UTF-16/trim; server được trùng; active channel/role unique case-insensitive/accent-sensitive theo DEC-095 |
| <a id="ac-com-39"></a> AC-COM-39 | Owner tìm thao tác xóa toàn server | Không có thao tác/API xóa server trong v1 theo DEC-094; vẫn có xóa phòng/chuyển owner |
| <a id="ac-com-40"></a> AC-COM-40 | Public có pending đổi sang private trong lúc reviewer duyệt | Một thứ tự commit xác định; pending còn lại cancelled/server_private, sender biết lý do; members/invites được giữ theo DEC-096 |


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](traceability.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Kết quả thực thi được quản lý trong hồ sơ nghiệm thu, dẫn chiếu từ [trạng thái theo khả năng](#implementation).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](../messaging/channel-messaging.md#evidence). Các ca bên dưới là đặc tả kiểm chứng; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

| Mã ca | Thao tác và dữ liệu | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| <a id="tc-com-01"></a> TC-COM-01 | Tìm công khai/riêng tư; đổi cộng đồng sang riêng tư rồi tìm lại | Chỉ kết quả công khai, không lộ phòng/tin cho người ngoài | [AC-COM-01](#ac-com-01), [AC-COM-19](invitations.md#ac-com-19), [AC-COM-29](#ac-com-29) |
| <a id="tc-com-02"></a> TC-COM-02 | Tạo cộng đồng với biên tên/mô tả; N sửa metadata của O qua API | Giới hạn theo DEC-072; chỉ O sửa được; không để lại membership owner dở dang | [AC-COM-29](#ac-com-29) |
| <a id="tc-com-14"></a> TC-COM-14 | Chuyển owner đồng thời với target rời/chủ cũ rời; target mất trạng thái active | Luôn một owner; target đủ điều kiện tại chuyển; chủ cũ chỉ rời sau chuyển thành công | [AC-COM-33](#ac-com-33) |
| <a id="tc-com-18"></a> TC-COM-18 | Search q biên, tiếng Việt/case/dấu, ký tự %/_/backslash; private switch với cursor cũ | Tìm literal/exact rank/phân trang đúng; không lộ private | [AC-COM-37](#ac-com-37) |
| <a id="tc-com-19"></a> TC-COM-19 | Tên 1/2/100/101 UTF-16, emoji/combining; role 64/65; collision key sau trim/case/NFC | Unicode hợp lệ, đúng unique theo DEC-095; không để lại dữ liệu dở dang | [AC-COM-38](#ac-com-38) |
| <a id="tc-com-20"></a> TC-COM-20 | Public→private tranh approve/cancel; sender đọc status sau chuyển | Pending còn lại cancelled/server_private cùng commit; membership đã commit trước được giữ | [AC-COM-40](#ac-com-40) |
| <a id="tc-com-27"></a> TC-COM-27 | Tìm UI/API xóa server hoặc text message trong voice channel | Không mở scope ngoài DEC-094; voice/text kind theo contract, media kiểm chứng riêng | [AC-COM-39](#ac-com-39), hợp đồng kind |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../../project/planning.md#community-work-items), [migration](../../system/community.md#schema-migration) và [vòng đời dữ liệu](../../system/data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

Private switch cần proof cùng Memberships; transfer cần proof với target leave/Identity guard. Tên/search Unicode, version/operation và mapping legacy của create/search đã có bằng chứng tại [đối chiếu test](../../records/verification/community/README.md); không dùng chúng để đóng migration/writer của private switch hoặc transfer. Hiện trạng của chủ đề ghi tại [trạng thái theo khả năng](#implementation).

# SCDC — Servers — Cộng đồng và chủ sở hữu

Cập nhật: 2026-10-07. Thành phần nội bộ của module Community. Quy tắc nghiệp vụ đã xác nhận theo các DEC dẫn chiếu; gói tạo/xem đã được rà soát nghiệp vụ bên dưới, thiết kế kỹ thuật tiếp tục là bản dự thảo. Source còn Foundation, chưa có kết quả chạy UC-COM.

Sở hữu metadata, visibility, join mode, tìm kiếm và owner của server. Chuyển owner phối hợp Memberships; public→private kết thúc request qua cùng transaction.

[Tổng quan và truy vết Community](../community.md#use-cases) · [Kế hoạch triển khai](../community.md#use-case-delivery).

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Use case](#use-cases)
- [Rà soát nghiệp vụ gói tạo/xem](#first-package-business)
- [UX và trạng thái](#ux)
- [Thiết kế dữ liệu/API](#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Việc còn lại](#gaps)

<a id="requirements"></a>

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

## Use case

Điều kiện, version/epoch, retry và lỗi dùng chung theo [quy ước tích hợp](integration.md#use-case-conditions). Quy tắc/AC/TC áp dụng cho từng UC ở [bảng truy vết](../community.md#use-case-coverage).

<a id="uc-com-01"></a>

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

<a id="first-package-business"></a>

### Rà soát nghiệp vụ gói tạo/xem — 2026-10-07

Đây là đầu ra bước 2 của [gói đầu tiên](../community.md#first-package): đối chiếu UC-COM-01 và phần đọc được chọn của UC-COM-03 với các quyết định đã xác nhận. Các bảng dưới đây cụ thể hóa quy tắc hiện có và tình huống cần kiểm chứng; không ghi nhận kết quả chạy hoặc quyết định sản phẩm mới. API/schema, cách chuẩn hóa dữ liệu và cơ chế giao dịch được rà soát ở bước 3.

| Nội dung | Quy tắc áp dụng trong gói đầu | Nguồn chuẩn |
|---|---|---|
| Người thực hiện | Tài khoản active, email đã xác minh, phiên hợp lệ; tạo không yêu cầu membership trước đó. Actor lấy từ phiên và phải còn đủ điều kiện khi thao tác được chấp nhận. | [Điều kiện chung](integration.md#use-case-conditions), [DEC-051](../../decisions.md#dec-051), [COM-029](#com-029) |
| Tên | Trim đầu/cuối, 2–100 UTF-16, cho tiếng Việt và emoji; từ chối tên trống/chỉ trắng/vô hình hoặc Unicode không hợp lệ. Hai cộng đồng được dùng cùng tên. | [COM-029](#com-029), [COM-038](#com-038), [COM-039](#com-039) |
| Mô tả | Tùy chọn, tối đa 1.000 UTF-16; nội dung vượt giới hạn bị từ chối. | [COM-029](#com-029), [COM-038](#com-038) |
| Visibility và tham gia | Cho chọn public/private, mặc định public. Public mới mặc định vào ngay. Private không xuất hiện trong tìm kiếm; việc vào private vẫn theo đường lời mời được đặc tả cho gói sau. | [COM-002](#com-002), [COM-003](#com-003), [COM-029](#com-029), [hành trình tham gia](../community.md#requirements) |
| Người tạo | Sau thành công, người tạo là owner và thành viên active của chính cộng đồng; ownership lấy từ server, không dựng thêm role owner làm nguồn quyền thứ hai. | [UC-COM-01](#uc-com-01), [ranh giới nội bộ](../community.md#organization), [quyền owner](permissions.md#detailed-design) |
| Vai trò mặc định | Có @everyone, tự áp cho membership active, không có quyền quản lý và không cần gán thủ công cho người tạo. Gói đầu chưa mở quản lý custom role. | [COM-037](permissions.md#com-037), [thiết kế vai trò](permissions.md#detailed-design) |
| Phòng ban đầu | Tạo cộng đồng không tự tạo phòng. Giao diện chi tiết của gói đầu hiển thị metadata/tư cách; danh sách phòng và lịch sử được nối khi triển khai UC-COM-17. | [UC-COM-01](#uc-com-01), [phạm vi gói](../community.md#first-package) |
| Commit và thử lại | Thành công phải có đủ server, owner membership, @everyone và dữ liệu thao tác; lỗi giữa chừng không để lại phần tạo dở. Cùng thao tác/payload chỉ tạo một server, kể cả hai request đồng thời; cùng khóa khác payload là xung đột. | [UC-COM-01](#uc-com-01), [điều kiện chung](integration.md#use-case-conditions), [operation retry](integration.md#operations) |
| Danh sách của mình | Chỉ trả các cộng đồng người gọi có membership active; có phân trang và trạng thái rỗng. Danh sách đổi thì dedup theo ID và tải lại từ đầu, không hứa snapshot bất biến. | [UC-COM-03](#uc-com-03), [quy tắc collection](integration.md#collections) |
| Chi tiết và tư cách | Member detail chỉ dành cho thành viên active; status riêng chỉ của người gọi. Đọc status không cấp quyền member detail hoặc tạo/phục hồi membership. Người ngoài private nhận 404; public summary không chứa owner/membership/quyền quản lý riêng. | [UC-COM-03](#uc-com-03), [quy tắc collection](integration.md#collections), [OpenAPI](../../contracts/community.openapi.json) |
| Quyền của owner | Tư cách owner cấp các quyền quản lý đã đặc tả trong phạm vi cộng đồng sau kiểm tra điều kiện nền. Nó không cấp quyền đọc DM hoặc sửa/xóa tin của người khác; các thao tác quản lý ngoài gói đầu chưa được coi đã triển khai. | [Ma trận ACL](permissions.md#permissions), [quyền owner](permissions.md#detailed-design) |

#### Tình huống cần kiểm chứng cho gói

Các dòng này bổ sung tình huống cụ thể cho [tiêu chí gói đầu](../community.md#first-package) và phần được chọn của AC/TC hiện có. Chúng chưa chạy; cách dựng dữ liệu, assertion, HTTP/lỗi và bằng chứng sẽ được chốt ở bước 3 rồi thực thi ở bước 4/5.

| Tình huống | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|
| Tạo public, bỏ lựa chọn visibility; tạo private | Public mặc định và cho vào ngay; private được tạo; mỗi lần tạo người gọi có membership active và là owner. | [UC-COM-01](#uc-com-01), phần tạo của [AC-COM-29](#ac-com-29) |
| Tài khoản chưa xác minh, inactive, phiên thiếu/hết hiệu lực hoặc actor không khớp phiên | Từ chối cả khi gọi API trực tiếp; không tạo server/membership/role/operation như đã thành công. Tài khoản mất điều kiện trước khi commit cũng không được hoàn tất thao tác tạo. | [Điều kiện chung](integration.md#use-case-conditions), [giao dịch/guard](integration.md#transactions) |
| Tên 1/2/100/101 UTF-16 sau trim, tiếng Việt, emoji; tên chỉ trắng/vô hình; hai server cùng tên | Nhận đúng giới hạn, giữ cách viết hợp lệ và cho trùng tên; từ chối tên sai mà không lưu phần tạo dở. | [AC-COM-38](#ac-com-38), phần tên server của [TC-COM-19](#tc-com-19) |
| Mô tả bỏ trống, 1.000/1.001 UTF-16 | Nhận mô tả tùy chọn và đúng giới hạn; quá giới hạn trả lỗi để sửa form, không tạo dữ liệu dở dang. | [COM-029](#com-029), phần tạo của [TC-COM-02](#tc-com-02) |
| Lỗi khi ghi membership/@everyone hoặc phần còn lại của giao dịch tạo | Toàn bộ phần tạo rollback; danh sách không hiện server thiếu owner hoặc @everyone. | [UC-COM-01](#uc-com-01), phần tạo của [TC-COM-02](#tc-com-02) |
| Server đã commit nhưng response mất; thử lại cùng khóa/payload hoặc gửi đồng thời | Cùng một server được trả lại, không nhân server/membership/@everyone; không tự gửi lại mutation sau timeout/refresh. | [UC-COM-01](#uc-com-01), phần tạo server của [TC-COM-23](integration.md#tc-com-23) |
| Sau mất response, đổi tên/mô tả/visibility rồi dùng lại khóa cũ | Báo xung đột; giữ nguyên kết quả cũ, không tạo server thứ hai hoặc sửa server đã tạo. | [Operation retry](integration.md#operations) |
| Người tạo mở list/detail/status, tài khoản khác gọi cùng API; danh sách rỗng và nhiều trang | Người tạo đọc đúng tư cách và quyền; tài khoản khác không nhận member detail/status của người tạo, private bị che. Đọc/list không tạo hoặc phục hồi membership. | [UC-COM-03](#uc-com-03), [collection](integration.md#collections) |
| Tải lại UI hoặc restart API sau tạo | Cộng đồng vẫn được đọc từ dữ liệu đã lưu; chỉ báo thành công sau xác nhận commit, lỗi giữ dữ liệu form để người dùng xử lý. | [UX](#ux), [điều kiện bàn giao MVP](../../releases/mvp.md#acceptance) |

#### Đầu vào còn lại cho bước thiết kế kỹ thuật

- Đối chiếu contract tạo/list/detail và `GET /servers/{serverId}/membership/me` với các tình huống trên; phân biệt quyền đọc public summary, member detail và status của chính mình. Phần tìm kiếm và trải nghiệm khám phá UC-COM-02 vẫn ở gói sau.
- Chuẩn hóa và lỗi trường: tên một dòng/control, thứ tự trim/kiểm tra Unicode, mô tả rỗng thành null, giới hạn UTF-16 và cách biểu diễn lỗi đang là thiết kế mục tiêu, chưa phải lựa chọn kỹ thuật đã nghiệm thu.
- Public mới mặc định `immediate` đã có căn cứ nghiệp vụ. Giá trị `joinMode` lưu cho private không mở đường tham gia trực tiếp; bước 3 phải chọn biểu diễn nhất quán với OpenAPI và luồng chuyển visibility sau này.
- Chốt model/schema, migration trên dữ liệu legacy, version ban đầu, cursor, Identity guard, transaction, dedup/khóa và dữ liệu operation/outbox cần cho việc tạo. Gói đầu chưa có Hub/realtime; việc ghi outbox và phát sự kiện cần được xác định riêng.
- Bổ sung assertion riêng cho phần đọc của UC-COM-03, các biên dữ liệu và race tạo/account; không suy rằng chạy TC-COM-02/19/23 là đã bao phủ các nhánh sửa metadata/role/channel hoặc toàn bộ UC-COM-03.

<a id="uc-com-04"></a>

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

Phạm vi màn hình hẹp/trình duyệt và trạng thái chung theo [tích hợp UX](integration.md#ux).

<a id="contracts"></a>

## Thiết kế dữ liệu/API

HTTP mục tiêu và quy ước chung ở [tích hợp](integration.md#contracts); [OpenAPI Community](../../contracts/community.openapi.json) là schema dự thảo, không phải API đang chạy. Mỗi use case ứng dụng phối hợp dữ liệu của các phần trong [transaction chung](integration.md#transactions).

| Method / đường dẫn | Đầu vào | Kết quả và kiểm tra quyền |
|---|---|---|
| `POST /servers` | `{name,description?,visibility}` | 201 server; account active/verified; tạo server và membership owner cùng transaction |
| `PATCH /servers/{id}` | `{name?,description?,visibility?,expectedVersion}` | 200 server; chỉ owner; private không xuất hiện trong tìm kiếm |
| `GET /servers/search` | `q,cursor,limit` | 200 trang server công khai; q/khớp/phân trang theo DEC-093; không trả nội dung/phòng riêng tư |
| `PATCH /servers/{id}/join-mode` | `{joinMode,expectedVersion}` | 200 server; đúng quyền đổi chế độ, không vô hiệu nguyên tắc link cho vào ngay |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId,expectedVersion}` | 200 server; owner hiện tại, target là thành viên active/verified; chuyển ngay, không tạo pending |

### Giao dịch của thành phần

- Tạo server: Identity guard → server + owner membership + @everyone + operation + outbox một transaction; không có khoảng thời gian server thiếu owner. Hai create cùng clientOperationId chưa có server để khóa: unique operation làm một transaction thắng; bên thua rollback toàn bộ server/membership/role/outbox rồi đọc operation đã commit trong transaction mới, không chỉ bỏ lỗi insert operation và giữ server trùng.
- Public → private: update visibility và chuyển mọi pending request sang cancelled với reason server_private trong cùng transaction theo DEC-096; không xóa lịch sử request. Sender vẫn đọc được trạng thái của chính mình và nhận thông báo lý do. Existing members/invites giữ nguyên; join request cũ không được approve sau commit private.
- Chuyển owner: khóa actor/target theo thứ tự rồi server/membership; target còn active/verified và là member tại commit. Owner field là nguồn chuẩn duy nhất; target rời trước thì transfer thất bại, transfer trước thì target thành owner không được rời. Chủ cũ chỉ rời sau chuyển đã commit.

<a id="search"></a>

### Search

Search theo DEC-093 dùng key tên trim/NFC/ToLowerInvariant, exact trước rồi normalizedName/UUID để ổn định. Escape `%`, `_`, backslash cho LIKE literal; collation không bỏ dấu. Cursor Data Protection purpose `Community.Search.v1`, actor/query/limit/position/expiry 24 giờ; mỗi trang lọc lại visibility/status. Đổi public→private phải biến mất cả khi dùng cursor cũ, không dựa vào snapshot đã cấp quyền public trước đó.

Các cột/ràng buộc/mapping cần thay theo [COM-SQL-01–10](integration.md#schema-migration). Thiết kế chưa được coi triển khai trước khi có migration và proof của writer/guard.

<a id="acceptance"></a>

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


Các tiêu chí liên quan nhiều phần có một nguồn chuẩn ở thành phần chủ trì; [ma trận UC/AC/TC](../community.md#use-case-coverage) dẫn tới tất cả tiêu chí cần kiểm chứng. Chưa có kết quả chạy AC-COM.

<a id="tests"></a>

## Ca kiểm thử

Dùng [dữ liệu và cách ghi bằng chứng chung](integration.md#evidence). Các ca bên dưới đều chưa chạy; cần bổ sung assertion cho từng endpoint/nhánh và kiểm tra quyền bằng API.

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

## Việc còn lại

Trạng thái phụ thuộc chung theo [kế hoạch triển khai](../community.md#use-case-delivery), [migration](integration.md#schema-migration) và [vòng đời dữ liệu](../../data-lifecycle.md). Các đầu vào review/mock/proof còn mở, không đánh dấu nghiệm thu từ tài liệu/fixture.

Private switch cần proof cùng Memberships; transfer cần proof với target leave/Identity guard. Tên/search Unicode, version và operation cần migration và đối chiếu dữ liệu legacy.

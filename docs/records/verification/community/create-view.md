# Hồ sơ Community — create-view

Hồ sơ giữ phạm vi được chọn và kết quả kiểm chứng tại revision ghi trong từng phần. Những mô tả nhánh, thiết kế chưa triển khai hoặc chưa hợp nhất phản ánh thời điểm bàn giao. Quy tắc hiện hành và trạng thái theo khả năng ở [Community](../../../features/community/README.md); không suy bằng chứng cũ thành kết quả của code mới.

## Phạm vi tại thời điểm triển khai

<a id="scope-first-package"></a>

<a id="scope-phạm-vi-gói-đầu-tiên--tạo-và-xem-cộng-đồng"></a>

### Phạm vi gói đầu tiên — tạo và xem cộng đồng

Phạm vi gói được duyệt ngày 2026-10-07, chọn UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03. Các bước và trạng thái hiện tại tại [các chủ đề Community](../../../features/community/README.md); bằng chứng được ghi tại hồ sơ nghiệm thu của gói.

Luồng cần bàn giao: **đăng nhập bằng tài khoản đủ điều kiện → tạo cộng đồng → mở chi tiết → thấy cộng đồng trong danh sách của mình → tải lại và vẫn đọc được dữ liệu đã lưu**.

| Phần trong gói | Phạm vi được chọn | Truy vết |
|---|---|---|
| Tạo cộng đồng | Tên, mô tả tùy chọn, public/private; người tạo trở thành owner và thành viên active; khởi tạo @everyone theo đặc tả. Tạo server chưa bao gồm tạo phòng đầu tiên. | [UC-COM-01](../../../features/community/servers.md#uc-com-01) |
| Danh sách của mình | Chỉ cộng đồng có membership active của người dùng; phân trang và trạng thái chưa có cộng đồng. Đây là danh sách đã tham gia, không phải tìm kiếm cộng đồng công khai. | Phần danh sách của [UC-COM-03](../../../features/community/servers.md#uc-com-03) |
| Chi tiết và tư cách của mình | Metadata cộng đồng, membership hiện hành và quyền quản lý hiệu lực; đọc dữ liệu không tạo hoặc phục hồi membership. | Phần đọc detail/tư cách của [UC-COM-03](../../../features/community/servers.md#uc-com-03) |
| Giao diện và kiểm chứng | Form tạo, danh sách, chi tiết; trạng thái đang tải/rỗng/lỗi và thử lại thao tác chưa rõ kết quả; gọi API trực tiếp để kiểm tra điều kiện và quyền. | [UX Servers](../../../features/community/servers.md#ux), [điều kiện chung](../../../system/community.md#use-case-conditions) |

UC-COM-03 là phạm vi bàn giao từng phần: tải phòng/lịch sử qua [UC-COM-17](../../../features/community/channels.md#uc-com-17), đối soát sau leave/rejoin và thay đổi membership đồng thời được kiểm chứng khi triển khai các luồng tương ứng. Các nhánh này vẫn giữ nguyên trong đặc tả nguồn; hoàn thành gói đầu chưa đủ để đánh dấu toàn bộ UC-COM-03 đạt.

Phần để sau gói đầu: tìm kiếm/trải nghiệm khám phá công khai (UC-COM-02), sửa thông tin/chế độ tham gia (UC-COM-04/05), tham gia/rời và lời mời (UC-COM-06–15), phòng/vai trò/ACL (UC-COM-16–22), tin phòng và realtime (UC-COM-23–25). Route detail dùng chung vẫn kiểm chứng projection summary public theo [thiết kế gói](../../../features/community/servers.md#create-api), không coi toàn bộ UC-COM-02 đã triển khai. Ownership và @everyone cần cho việc tạo vẫn nằm trong gói đầu; giao diện quản lý vai trò nằm ở gói sau. Media thuộc v1 theo [phạm vi MVP](../../../releases/mvp.md). Việc để sau gói đầu không tự loại use case khỏi toàn bộ MVP.

<a id="scope-tiêu-chí-hoàn-thành-gói-đầu"></a>

### Tiêu chí hoàn thành gói đầu

Các dòng dưới đây chọn phần cần kiểm chứng từ đặc tả nguồn, không thay thế hoặc đánh dấu đạt toàn bộ AC/TC liên quan. Bước 2 rà soát nghiệp vụ và bổ sung tình huống cụ thể; bước 3 chốt request/response, lỗi và cách thử. Khi nghiệm thu, mỗi dòng cần có bằng chứng và kết quả riêng.

| Phần cần kiểm chứng | Kết quả cần quan sát | Nguồn đặc tả/ca kiểm thử |
|---|---|---|
| Tạo hợp lệ | Tài khoản đủ điều kiện tạo được public/private; đọc lại đúng metadata và trạng thái ban đầu đã chốt. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), phần tạo của [AC-COM-29](../../../features/community/servers.md#ac-com-29)/[TC-COM-02](../../../features/community/servers.md#tc-com-02) |
| Điều kiện tài khoản/phiên | Phiên không hợp lệ hoặc tài khoản không đủ điều kiện bị chặn cả khi gọi API trực tiếp. | [Điều kiện chung](../../../system/community.md#use-case-conditions), [COM-029](../../../features/community/servers.md#com-029) |
| Dữ liệu biên | Chấp nhận/từ chối tên, mô tả và Unicode đúng quy tắc; lỗi trả về không để lại cộng đồng dở dang. | Phần tên server của [AC-COM-38](../../../features/community/servers.md#ac-com-38)/[TC-COM-19](../../../features/community/servers.md#tc-com-19), phần tạo của [AC-COM-29](../../../features/community/servers.md#ac-com-29) |
| Tạo nguyên tử | Server, owner membership, @everyone và dữ liệu thao tác cùng commit; lỗi giữa chừng rollback toàn bộ phần tạo. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), [giao dịch](../../../system/community.md#transactions), phần tạo của [TC-COM-02](../../../features/community/servers.md#tc-com-02) |
| Thử lại/đồng thời | Mất response rồi thử lại cùng khóa/payload chỉ có một server; cùng khóa khác payload bị từ chối theo hợp đồng. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), phần tạo server của [TC-COM-23](../../../system/community.md#tc-com-23) |
| Danh sách/detail/tư cách | Người tạo thấy cộng đồng, tư cách owner và quyền hiệu lực; danh sách rỗng/phân trang đúng; đọc không ghi hoặc phục hồi membership. | Phần được chọn của [UC-COM-03](../../../features/community/servers.md#uc-com-03), [thiết kế danh sách](../../../system/community.md#contracts) |
| Quyền đọc | Tài khoản khác không nhận member detail hoặc tư cách của người tạo; người ngoài mở cộng đồng private nhận 404. | [UC-COM-02](../../../features/community/servers.md#uc-com-02) (quy tắc che private), [UC-COM-03](../../../features/community/servers.md#uc-com-03), [điều kiện chung](../../../system/community.md#use-case-conditions) |
| Luồng giao diện và lưu bền | Thực hiện trọn luồng qua UI/API với dữ liệu DB; reload hoặc restart API vẫn đọc được cộng đồng; trạng thái lỗi và thử lại đúng kết quả đã lưu. | [UX Servers](../../../features/community/servers.md#ux), [điều kiện bàn giao MVP](../../../releases/mvp.md#acceptance) |

Dữ liệu kiểm chứng cần có: một tài khoản đủ điều kiện làm người tạo, một tài khoản đủ điều kiện chưa tham gia để kiểm tra quyền, tài khoản chưa xác minh và phiên không hợp lệ; cộng đồng public/private và đủ dữ liệu để kiểm tra phân trang. Đây là kế hoạch dữ liệu thử, chưa tạo tài khoản hoặc dữ liệu trong bước 1. Kết quả gói được ghi theo [mẫu nghiệm thu](../../../releases/acceptance.md#testing), kèm bản build, thao tác API/UI, kết quả từng dòng và phần UC còn lại.

<a id="scope-first-package-business"></a>

<a id="scope-rà-soát-nghiệp-vụ-gói-tạoxem--2026-10-07"></a>

### Rà soát nghiệp vụ gói tạo/xem — 2026-10-07

Đầu ra rà soát nghiệp vụ ngày 2026-10-07. Các tình huống dưới đây bổ sung kiểm chứng cho phạm vi gói đã chọn; đây là đầu vào thiết kế, không phải kết quả chạy.

| Nội dung | Quy tắc áp dụng trong gói đầu | Nguồn chuẩn |
|---|---|---|
| Người thực hiện | Tài khoản active, email đã xác minh, phiên hợp lệ; tạo không yêu cầu membership trước đó. Actor lấy từ phiên và phải còn đủ điều kiện khi thao tác được chấp nhận. | [Điều kiện chung](../../../system/community.md#use-case-conditions), [DEC-051](../../decisions/README.md#dec-051), [COM-029](../../../features/community/servers.md#com-029) |
| Tên | Trim đầu/cuối, 2–100 UTF-16, cho tiếng Việt và emoji; từ chối tên trống/chỉ trắng/vô hình hoặc Unicode không hợp lệ. Hai cộng đồng được dùng cùng tên. | [COM-029](../../../features/community/servers.md#com-029), [COM-038](../../../features/community/servers.md#com-038), [COM-039](../../../features/community/servers.md#com-039) |
| Mô tả | Tùy chọn, tối đa 1.000 UTF-16; nội dung vượt giới hạn bị từ chối. | [COM-029](../../../features/community/servers.md#com-029), [COM-038](../../../features/community/servers.md#com-038) |
| Visibility và tham gia | Cho chọn public/private, mặc định public. Public mới mặc định vào ngay. Private không xuất hiện trong tìm kiếm; việc vào private vẫn theo đường lời mời được đặc tả cho gói sau. | [COM-002](../../../features/community/servers.md#com-002), [COM-003](../../../features/community/servers.md#com-003), [COM-029](../../../features/community/servers.md#com-029), [hành trình tham gia](../../../features/community/README.md#mục-lục) |
| Người tạo | Sau thành công, người tạo là owner và thành viên active của chính cộng đồng; ownership lấy từ server, không dựng thêm role owner làm nguồn quyền thứ hai. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), [ranh giới nội bộ](../../../system/community.md#organization), [quyền owner](../../../features/community/access-control.md#permission-detailed-design) |
| Vai trò mặc định | Có @everyone, tự áp cho membership active, không có quyền quản lý và không cần gán thủ công cho người tạo. Gói đầu chưa mở quản lý custom role. | [COM-037](../../../features/community/access-control.md#com-037), [thiết kế vai trò](../../../features/community/access-control.md#permission-detailed-design) |
| Phòng ban đầu | Tạo cộng đồng không tự tạo phòng. Giao diện chi tiết của gói đầu hiển thị metadata/tư cách; danh sách phòng và lịch sử được nối khi triển khai UC-COM-17. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), [phạm vi gói](#scope-first-package) |
| Commit và thử lại | Thành công phải có đủ server, owner membership, @everyone và dữ liệu thao tác; lỗi giữa chừng không để lại phần tạo dở. Cùng thao tác/payload chỉ tạo một server, kể cả hai request đồng thời; cùng khóa khác payload là xung đột. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), [điều kiện chung](../../../system/community.md#use-case-conditions), [operation retry](../../../system/community.md#operations) |
| Danh sách của mình | Chỉ trả các cộng đồng người gọi có membership active; có phân trang và trạng thái rỗng. Danh sách đổi thì dedup theo ID và tải lại từ đầu, không hứa snapshot bất biến. | [UC-COM-03](../../../features/community/servers.md#uc-com-03), [quy tắc collection](../../../system/community.md#collections) |
| Chi tiết và tư cách | Member detail chỉ dành cho thành viên active; status riêng chỉ của người gọi. Đọc status không cấp quyền member detail hoặc tạo/phục hồi membership. Người ngoài private nhận 404; public summary không chứa owner/membership/quyền quản lý riêng. | [UC-COM-03](../../../features/community/servers.md#uc-com-03), [quy tắc collection](../../../system/community.md#collections), [OpenAPI](../../../contracts/community.openapi.json) |
| Quyền của owner | Tư cách owner cấp các quyền quản lý đã đặc tả trong phạm vi cộng đồng sau kiểm tra điều kiện nền. Nó không cấp quyền đọc DM hoặc sửa/xóa tin của người khác; các thao tác quản lý ngoài gói đầu chưa được coi đã triển khai. | [Ma trận ACL](../../../features/community/access-control.md#permissions), [quyền owner](../../../features/community/access-control.md#permission-detailed-design) |

<a id="scope-tình-huống-cần-kiểm-chứng-cho-gói"></a>

#### Tình huống cần kiểm chứng cho gói

Các dòng này bổ sung tình huống cụ thể cho [tiêu chí gói đầu](#scope-first-package) và phần được chọn của AC/TC hiện có. Đây là đầu vào kiểm chứng; kết quả thực thi nằm trong [hồ sơ nghiệm thu](create-view.md). HTTP/lỗi, dữ liệu và cách kiểm chứng theo [thiết kế gói](#design-verification), thực thi ở bước 4/5.

| Tình huống | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|
| Tạo public, bỏ lựa chọn visibility; tạo private | Public mặc định và cho vào ngay; private được tạo; mỗi lần tạo người gọi có membership active và là owner. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), phần tạo của [AC-COM-29](../../../features/community/servers.md#ac-com-29) |
| Tài khoản chưa xác minh, inactive, phiên thiếu/hết hiệu lực hoặc actor không khớp phiên | Từ chối cả khi gọi API trực tiếp; không tạo server/membership/role/operation như đã thành công. Tài khoản mất điều kiện trước khi commit cũng không được hoàn tất thao tác tạo. | [Điều kiện chung](../../../system/community.md#use-case-conditions), [giao dịch/guard](../../../system/community.md#transactions) |
| Tên 1/2/100/101 UTF-16 sau trim, tiếng Việt, emoji; tên chỉ trắng/vô hình; hai server cùng tên | Nhận đúng giới hạn, giữ cách viết hợp lệ và cho trùng tên; từ chối tên sai mà không lưu phần tạo dở. | [AC-COM-38](../../../features/community/servers.md#ac-com-38), phần tên server của [TC-COM-19](../../../features/community/servers.md#tc-com-19) |
| Mô tả bỏ trống, 1.000/1.001 UTF-16 | Nhận mô tả tùy chọn và đúng giới hạn; quá giới hạn trả lỗi để sửa form, không tạo dữ liệu dở dang. | [COM-029](../../../features/community/servers.md#com-029), phần tạo của [TC-COM-02](../../../features/community/servers.md#tc-com-02) |
| Lỗi khi ghi membership/@everyone hoặc phần còn lại của giao dịch tạo | Toàn bộ phần tạo rollback; danh sách không hiện server thiếu owner hoặc @everyone. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), phần tạo của [TC-COM-02](../../../features/community/servers.md#tc-com-02) |
| Server đã commit nhưng response mất; thử lại cùng khóa/payload hoặc gửi đồng thời | Cùng một server được trả lại, không nhân server/membership/@everyone; không tự gửi lại mutation sau timeout/refresh. | [UC-COM-01](../../../features/community/servers.md#uc-com-01), phần tạo server của [TC-COM-23](../../../system/community.md#tc-com-23) |
| Sau mất response, đổi tên/mô tả/visibility rồi dùng lại khóa cũ | Báo xung đột; giữ nguyên kết quả cũ, không tạo server thứ hai hoặc sửa server đã tạo. | [Operation retry](../../../system/community.md#operations) |
| Người tạo mở list/detail/status, tài khoản khác gọi cùng API; danh sách rỗng và nhiều trang | Người tạo đọc đúng tư cách và quyền; tài khoản khác không nhận member detail/status của người tạo, private bị che. Đọc/list không tạo hoặc phục hồi membership. | [UC-COM-03](../../../features/community/servers.md#uc-com-03), [collection](../../../system/community.md#collections) |
| Tải lại UI hoặc restart API sau tạo | Cộng đồng vẫn được đọc từ dữ liệu đã lưu; chỉ báo thành công sau xác nhận commit, lỗi giữ dữ liệu form để người dùng xử lý. | [UX](../../../features/community/servers.md#ux), [điều kiện bàn giao MVP](../../../releases/mvp.md#acceptance) |

<a id="scope-đầu-vào-kỹ-thuật-từ-rà-soát-nghiệp-vụ"></a>

#### Đầu vào kỹ thuật từ rà soát nghiệp vụ

Các đầu vào dưới đây được ghi ở lần rà soát bước 2 và cụ thể hóa tại [thiết kế gói bước 3](../../../features/community/servers.md); kết quả thực thi nằm trong [hồ sơ nghiệm thu](create-view.md).

- Đối chiếu contract tạo/list/detail và `GET /servers/{serverId}/membership/me` với các tình huống trên; phân biệt quyền đọc public summary, member detail và status của chính mình. Phần tìm kiếm và trải nghiệm khám phá UC-COM-02 vẫn ở gói sau.
- Chuẩn hóa và lỗi trường: tên một dòng/control, thứ tự trim/kiểm tra Unicode, mô tả rỗng thành null, giới hạn UTF-16 và cách biểu diễn lỗi được chốt tại tài liệu thiết kế gói; bằng chứng tương ứng nằm trong hồ sơ nghiệm thu.
- Public mới mặc định `immediate` đã có căn cứ nghiệp vụ. Giá trị `joinMode` lưu cho private được chọn tại [thiết kế API gói](../../../features/community/servers.md#create-api); giá trị này không mở đường tham gia trực tiếp vào private.
- Chốt model/schema, migration trên dữ liệu legacy, version ban đầu, cursor, Identity guard, transaction, dedup/khóa và dữ liệu operation/outbox cần cho việc tạo. Scope gói đầu chỉ ghi outbox; dispatcher/Hub/realtime được duyệt và kiểm chứng riêng.
- Bổ sung assertion riêng cho phần đọc của UC-COM-03, các biên dữ liệu và race tạo/account; không suy rằng chạy TC-COM-02/19/23 là đã bao phủ các nhánh sửa metadata/role/channel hoặc toàn bộ UC-COM-03.

## Kết quả theo revision

Ngày kiểm chứng: 2026-10-07. Hồ sơ bằng chứng cho phạm vi [gói tạo/xem](#scope-first-package); không ghi trạng thái hiện tại của toàn module. Trạng thái theo nhánh và bước tiếp theo tại [các chủ đề Community](../../../features/community/README.md).

| Phần được thử | Commit implementation | Phạm vi |
|---|---|---|
| Backend | `79fa627` (gồm nền `f9abd87`, migration `05e61c6`) | UC-COM-01 và phần đọc đã chọn của UC-COM-03 |
| WebClient | `4203e05` (gồm nền API/draft `2b9d51e`) | Create/list/detail và xử lý lỗi/actor của gói |

Các commit trên nằm trong nhánh `feat/community-create-view`. Kết quả bên dưới gắn revision này; thay đổi code sau đó cần kiểm chứng phù hợp. Hồ sơ ghi bằng chứng kỹ thuật, không thay quyết định phát hành.

<a id="results-phạm-vi-bằng-chứng"></a>

### Phạm vi bằng chứng

Gói đầu có bằng chứng cho UC-COM-01, phần danh sách/detail/tư cách được chọn của UC-COM-03 và projection summary public của route dùng chung. Không dùng hồ sơ này để đánh dấu toàn bộ UC-COM-02/03 hoặc search, join/leave/rejoin, phòng, role/ACL, invitations và realtime đạt. Phạm vi và tiêu chí từng dòng ở [kế hoạch](#scope-first-package).

<a id="results-backend"></a>

<a id="results-bằng-chứng-backend"></a>

### Bằng chứng backend

| Route | Hành vi đã kiểm chứng |
|---|---|
| `POST /api/v1/servers` | Tạo server public/private với tên và mô tả theo policy; owner membership active và @everyone; 201/Location sau commit, retry hợp lệ 200, đổi payload cùng khóa 409 |
| `GET /api/v1/servers` | Chỉ server/member active của actor; keyset ID giảm dần, limit 1–50, cursor bảo vệ gắn actor/limit/purpose và hết hạn 24 giờ |
| `GET /api/v1/servers/{id}` | Member detail với quyền hiện hành; người ngoài public chỉ nhận sáu trường summary; private/inactive/unknown trả 404 |
| `GET /api/v1/servers/{id}/membership/me` | Membership active/left của chính actor, kể cả own-left trong private; GET không tạo hoặc phục hồi membership |

Gói thực hiện UC-COM-01 ở backend và phần list/detail/tư cách của UC-COM-03. Route summary public giữ quy tắc che dữ liệu; chưa có search/discovery, join/leave API, phòng, quản lý role/ACL, lời mời, Messaging hoặc realtime.

Các lựa chọn trong [thiết kế bước 3](../../../features/community/servers.md) đã được thực hiện: UUIDv7/UUIDv4, version string, giới hạn UTF-16, HMAC theo fixture, Data Protection key ring explicit, one-statement read projection, shared PostgreSQL transaction và Identity share guard. Metadata version vẫn do trigger tăng.

Create ghi server, owner membership, @everyone, operation và đúng một event `Community.ServerCreated.v1` trong cùng transaction. Event giữ `published_at=NULL`; chưa có dispatcher. Unique operation loser rollback toàn bộ context/transaction, mở transaction mới rồi đọc winner. Retry đọc detail hiện hành dưới quyền hiện hành; không tạo lại tài nguyên đã mất quyền hoặc thiếu key.

Migration có runner explicit, preflight và ledger/checksum. DB cũ cần mapping visibility/joinMode được rà soát theo server ID; role/grant/status legacy không được tự sửa. Deferred constraints giữ owner active và @everyone không có management grant. Bootstrap/seed DB mới được đồng bộ; API không tự migrate hoặc gọi bootstrap.

<a id="results-kiểm-thử-backend"></a>

#### Kiểm thử backend

Chạy PostgreSQL 18 trong container thử nghiệm riêng, database `scdc_community_test` ở cổng 15432. Migration tests tạo/drop các DB fixture riêng kết thúc `_test`; không dùng DB ứng dụng. Các tài khoản/khóa là dữ liệu tổng hợp.

| Nhóm | Kết quả |
|---|---|
| Identity guard/work scope | 7 ca đạt: revoked/stamp/expiry/unverified/inactive, share lock chặn writer, rollback |
| Migration/bootstrap | 4 ca đạt: legacy mapping explicit, preflight giữ dữ liệu/schema khi fail, checksum/no-op, UTF-16 SQL, seed và deferred invariant |
| API/transaction | 31 ca đạt: route/Location, Unicode và biên dữ liệu, quyền đọc, pagination, restart/rotation, fault injection, unique-key race, tranh logout/đổi mật khẩu, expiry trước commit, timeout 503 và invariants |
| Fingerprint/cursor | 5 ca đạt: HMAC khớp fixture độc lập, Unicode lỗi, expiry/purpose/storage/restart |
| Hồi quy host/Identity | 16 ca hiện có đạt; bổ sung kiểm tra Swagger trả đúng union summary/detail với trường bắt buộc |

Lệnh kiểm chứng cuối trên nhánh backend:

```bash
# Đặt ConnectionStrings__Database tới PostgreSQL thử nghiệm đã chuẩn bị.
# Đặt Modules__Identity__SigningKey và Modules__Identity__ExposeDevelopmentTokens=true.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1
```

Kết quả: **63 passed, 0 failed, 0 skipped**. Build Release không warning/error. Compose config hợp lệ với khóa tổng hợp. Image API build thành công; smoke HTTP trên container chạy bằng user ứng dụng thực hiện register/verify/login, create/list, restart, đọc cursor từ volume và retry cùng operation thành công. Phần này ghi kiểm chứng backend; kiểm thử tải ngoài phạm vi. Bằng chứng UI được ghi riêng bên dưới.


<a id="results-cấu-hình-thử-nghiệm-backend"></a>

#### Cấu hình thử nghiệm backend

Chuyển sang nhánh backend để đọc hướng dẫn runtime trong `services/Modules/Community/README.md`. HMAC cần key ID và base64 secret ít nhất 32 byte; key ring cần thư mục bền đọc/ghi được. Compose dùng `COMMUNITY_OPERATION_KEY` từ `.env` local và volume `scdc-community-keys`. Giữ HMAC key cũ khi rotation để đọc operation còn lưu; giữ key ring và ApplicationName theo môi trường qua restart.

<a id="results-frontend"></a>

<a id="results-bằng-chứng-webclient"></a>

### Bằng chứng WebClient

<a id="results-hành-vi-bàn-giao"></a>

#### Hành vi bàn giao

WebClient dùng bốn API của [backend bước 4](). Sau đăng nhập, người dùng thấy danh sách cộng đồng có membership active của mình. Form tạo nhận tên, mô tả và public/private; tạo thành công mở chi tiết, tải lại danh sách từ API và giữ đường dẫn `#community/{id}` qua reload. Không tạo phòng mặc định. Chi tiết hiển thị metadata, tư cách của chính người dùng và quyền quản lý hiệu lực do API trả về; quyền hiện tại chỉ được hiển thị, chưa có giao diện quản lý role/ACL.

Có trạng thái đang tải, rỗng, lỗi và nút tải lại. Danh sách dùng cursor của API với limit 20; lỗi cursor yêu cầu người dùng tải lại từ đầu. Server ID, owner, membership và permissions không được suy ra từ dữ liệu mẫu. Người ngoài public chỉ thấy summary; private không đủ quyền không hiển thị metadata. Refresh detail xóa dữ liệu cũ trước khi kiểm tra lại quyền. Chuyển tài khoản hoặc đăng xuất unmount state theo actor, hủy request và bỏ response đến muộn.

Tên và mô tả dùng cùng quy tắc với [thiết kế](../../../features/community/servers.md): White_Space cố định Unicode 17, giới hạn UTF-16, từ chối Unicode lỗi/NUL, tên một dòng, CRLF/CR → LF cho mô tả. Không trim mô tả hoặc đổi NFC. Kiểm thử frontend đối chiếu fixture `docs/fixtures/community-operations.json` và `docs/fixtures/text-policy.json`; backend vẫn là nơi quyết định validation và quyền.

Yêu cầu tạo gồm UUIDv4 và payload chuẩn hóa được ghi vào `sessionStorage` theo actor **trước POST**. Sau mất mạng, timeout 15 giây, 401, 409 hoặc lỗi máy chủ, khóa và nội dung giữ nguyên, form khóa sửa. Reload hiển thị “Tiếp tục yêu cầu”; chỉ nút “Kiểm tra và thử lại” gửi lại cùng operation. Không tự POST lại sau refresh token, reload hoặc kết nối lại. Submit đang chạy chặn nhấn hai lần. Đóng form hủy request phía trình duyệt và giữ draft; không coi thao tác đóng là hủy giao dịch máy chủ. Storage bị chặn không gửi POST; bản lưu hỏng không bị âm thầm thay thế. Lỗi validation 400 giải phóng operation và giữ nội dung form cho người dùng sửa. Chỉ xóa bản lưu khớp operation sau khi xác nhận thành công.

SessionStorage giữ được khi reload trong tab hiện tại; đóng tab hoặc xóa dữ liệu trình duyệt có thể mất yêu cầu chưa xác nhận. Khi đó cần kiểm tra danh sách trước khi tạo mới. Draft của tài khoản A không được tái sử dụng cho B. Khi không có Web Locks, cơ chế Identity hiện có giữ session trong bộ nhớ từng tab; reload yêu cầu đăng nhập lại, rồi cùng actor có thể tiếp tục draft của tab.

Tin nhắn trực tiếp hiện có được giữ ở đích riêng với nhãn giao diện mẫu. Luồng Community không dùng server/phòng mẫu, không thử kết nối Hub chưa triển khai, không hiện trạng thái realtime thành công giả. Trang đăng nhập giới thiệu các chức năng đang có.

<a id="results-kiểm-thử-webclient"></a>

#### Kiểm thử WebClient

API .NET 10 tại cổng 15026, PostgreSQL 18 trong container `scdc-community-step5-db`, database mới `scdc_community_step5_test` ở cổng 15432. Chỉ DB thử nghiệm được bootstrap; khóa và tài khoản tổng hợp. Vite tại cổng 15300 proxy sang API này. Backend không đổi ở bước 5; 63 ca Release của bước 4 được dẫn chiếu, không cộng vào số ca chạy frontend.

| Kiểm tra | Kết quả |
|---|---|
| Node/WebClient | **16 passed**, gồm 6 ca Identity session cũ, 4 ca actor-bound request/không tự replay POST, 6 ca canonical text/fixture/draft/UUID |
| Chromium với API và DB thật | **14 passed, 0 failed, 0 skipped** |
| Build/proxy production | Vite build và Dockerfile WebClient thành công với build context `clients/WebClient`; 4 ca trọng yếu qua nginx đạt, ca đóng form đang gửi trên image cuối cũng đạt |
| Dependency | Lockfile thêm Playwright 1.63.0; cập nhật riêng source-map-js 1.2.1 → 1.2.2; npm báo 0 vulnerability sau cập nhật |
| Đối chiếu PostgreSQL | Mỗi server trong ca mất response có đúng một operation và một `Community.ServerCreated.v1`; không có server thiếu owner active hoặc thiếu/thừa create event |

14 ca trình duyệt: đăng nhập → empty → private create → detail/list/reload; mất response sau commit và retry 200; local validation/server field errors; public outsider/private 404; 21 server phân trang và cursor lỗi; danh sách 503/recovery; đổi actor và draft isolation; POST 401 không replay; keyboard/focus/mobile; double submit/503; storage bị chặn/hỏng; response detail muộn và refresh mất quyền; đăng ký/xác minh development/login/public create bằng UI; đóng form đang gửi giữ được draft và focus.

Các ca thành công, mất response sau commit, quyền public/private, pagination, đổi actor và đăng ký đều gọi API thật. Các lỗi 400/401/503, cursor hết hạn, storage fault và snapshot mất quyền được tiêm chủ động vào trình duyệt để kiểm chứng cách xử lý UI. Riêng snapshot own-left/private là response fixture; chưa có leave API trong gói này. Quyền own-left thực tế, điều kiện tài khoản/phiên, race, transaction và các invariant đã được kiểm chứng độc lập ở backend bước 4. Không suy ra leave/rejoin/realtime đã hoạt động từ các ca UI.

Ảnh giao diện được tạo tại `artifacts/community-step5-detail.png`, output/trace nằm trong `artifacts/community-step5/` và được Git ignore. Không đưa session/token, dữ liệu DB hoặc key ring vào Git.

<a id="results-reproduce"></a>

<a id="results-chạy-lại"></a>

### Chạy lại

Chuyển sang `feat/community-create-view`. Chuẩn bị PostgreSQL thử nghiệm riêng, schema/migration đúng bước 4 và API trỏ vào DB đó; không dùng DB ứng dụng. API cần bật `Modules__Identity__ExposeDevelopmentTokens=true` trong môi trường Development để tạo tài khoản tổng hợp.

Với Node 24 và Chromium của [Playwright](https://playwright.dev/docs/test-cli):

```bash
cd clients/WebClient
npm ci
npm test
npm run build
npx playwright install chromium
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác, cùng clients/WebClient:
SCDC_E2E_URL=http://127.0.0.1:15300 \
SCDC_E2E_DATABASE=scdc_community_step5_test npm run test:e2e
```

`playwright.config.js` yêu cầu URL localhost và tên DB kết thúc `_test`. Tên DB này xác nhận cấu hình người chạy cung cấp; test không tự truy vấn connection string của API. Phải kiểm tra API đã trỏ vào DB thử nghiệm trước khi chạy. Test tạo tài khoản/server mới với tên riêng, không xóa dữ liệu để dọn DB ứng dụng.

Trong môi trường không có Node/Chromium trên host, dùng [image Playwright cùng phiên bản](https://playwright.dev/docs/docker) `mcr.microsoft.com/playwright:v1.63.0-noble`, mount repo, working directory `/repo/clients/WebClient`, network host cho localhost API. Trên Fedora có thể dùng `--security-opt label=disable` cho container kiểm thử để tránh thay nhãn SELinux của repo. Đây là cách đã chạy kiểm chứng ở bước 5.

## Đối chiếu trước triển khai — 2026-10-07

Bảng dưới đây ghi source tại thời điểm chốt thiết kế, trước khi có code của gói.

<a id="design-source"></a>

<a id="design-đối-chiếu-source"></a>

### Đối chiếu source

| Hiện có | Hệ quả cho gói |
|---|---|
| [CommunityModule](../../../../services/Modules/Community/CommunityModule.cs) chỉ đăng ký descriptor; project chưa có EF provider | Bổ sung DbContext, services/DI và controller trong bước 4; dùng cùng phiên bản provider với Identity, hiện là 10.0.3 |
| [IdentityModule](../../../../services/Modules/Identity/IdentityModule.cs) kiểm tra session/stamp trong JWT middleware; [IUserDirectory](../../../../services/SCDC.Contracts/Identity/IUserDirectory.cs) chỉ trả user summary | Middleware và directory chưa giữ điều kiện tài khoản đến commit; bổ sung guard do Identity triển khai |
| [IdentityDbContext.LockUserAsync](../../../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs) dùng `FOR NO KEY UPDATE`; các mutation bảo mật hiện gọi khóa user trước khi sửa email/stamp/session | Guard lấy `FOR SHARE` trên cùng user, kiểm tra lại dưới khóa; mọi writer làm đổi điều kiện tài khoản phải giữ cùng quy ước |
| [schema.sql](../../../../database/postgres/schema.sql) có server/member/role và trigger tăng server version; chưa có visibility, join mode, membership epoch hoặc operation | Migration bổ sung đúng phần gói cần; giữ trigger server làm nguồn tăng metadata version |
| [seed.sql](../../../../database/postgres/seed.sql) dùng system role `Owner`/`Member` và mã permission legacy | Seed hiện tại chưa phải dữ liệu kiểm chứng gói mới; đồng bộ bootstrap/seed riêng cho DB Development mới, preflight dữ liệu cũ trước migration |
| [ApiControllerBase](../../../../services/SCDC.Api/Controllers/ApiControllerBase.cs) và [ApiErrorMapper](../../../../services/SCDC.Api/Errors/ApiErrorMapper.cs) đã có Result/ProblemDetails | Dùng cơ chế lỗi hiện có; không tạo định dạng lỗi Community riêng |


## Kế hoạch kiểm chứng ban đầu

<a id="design-verification"></a>

<a id="design-kiểm-chứng-và-gói-code"></a>

### Kiểm chứng và gói code

| Nhóm | Bằng chứng bắt buộc ở bước 4/5 |
|---|---|
| Contract/validation | Bốn route đúng schema/status/Location, unknown field/actor/owner bị chặn; tên/mô tả biên UTF-16, emoji/combining/ZWJ, multiline/control và visibility/default theo policy |
| Mapping/migration | SQL và .NET đếm UTF-16 khớp; preflight legacy không sửa dữ liệu khi fail; deferred owner/@everyone reject dữ liệu dở; migration/ledger chạy lại đúng, bootstrap mới tương đương |
| Identity | Unverified/inactive/session revoked/stamp mismatch bị chặn; create tranh logout/password reset/disable/expiry có kết quả phù hợp dưới guard. Giữ các kiểm thử Identity hiện có |
| Create/retry | Fault injection giữa các insert rollback toàn bộ; hai connection cùng operation tạo đúng một server và event; payload khác 409; lost response/restart/rotation/thiếu key không tạo trùng |
| Read/privacy | Owner detail và five-code permissions; thành viên hợp lệ đọc projection đúng; public nonmember không lộ detail/status người khác; private 404; own left status không tạo membership; list rỗng/keyset đúng |
| Cursor | Cross-actor/limit/purpose/tamper/expiry bị chặn; key ring bền qua restart; dữ liệu trang không chứa server/member inactive ở snapshot query; dedup/refresh khi tập kết quả đổi |
| Outbox/lưu bền | Chỉ một event cùng commit, rollback không có event; reload/restart API vẫn đọc được kết quả, không báo đã phát realtime khi chưa có dispatcher |

Dùng PostgreSQL riêng cho integration/race tests, hai connection và barrier xác định thứ tự; không dùng EF InMemory để chứng minh lock/constraint. Fixture có cả DB trắng sau bootstrap mới và snapshot legacy trước migration. Build/test backend ở bước 4, thao tác UI/API và hồ sơ [nghiệm thu](../../../releases/acceptance.md#testing) ở bước 5; bảng trên là kế hoạch kiểm chứng; kết quả từng phần ở [hồ sơ nghiệm thu](create-view.md).

Gói code dùng `feat/community-create-view` từ main đã có tài liệu/phụ thuộc. Thứ tự commit kiểm chứng được: work scope + Identity guard; persistence/migration và preflight; create/read API; các kiểm chứng còn lại. Mỗi phần có kiểm tra phù hợp, commit và push theo tiến độ; không merge nhánh code nếu chưa duyệt. Tài liệu độc lập tiếp tục trên main theo [quy ước Git](../../../guides/development.md#conventions).

Đầu vào bước 4 đã xác định trong tài liệu này. Các ngưỡng timeout/cursor là cấu hình thiết kế cần kiểm chứng; migration dữ liệu thật chỉ chạy khi có mapping/preflight hợp lệ. Hoàn thành thiết kế không đóng các OQ/ACL-O, không xác nhận API hoặc quyền/realtime đã hoạt động.

# SCDC — Kế hoạch gói tạo và xem cộng đồng

<a id="first-package"></a>

## Phạm vi gói đầu tiên — tạo và xem cộng đồng

Phạm vi gói được duyệt ngày 2026-10-07, chọn UC-COM-01 và phần danh sách/detail/tư cách của UC-COM-03. Các bước và trạng thái hiện tại tại [status.md](../../status.md); bằng chứng được ghi tại hồ sơ nghiệm thu của gói.

Luồng cần bàn giao: **đăng nhập bằng tài khoản đủ điều kiện → tạo cộng đồng → mở chi tiết → thấy cộng đồng trong danh sách của mình → tải lại và vẫn đọc được dữ liệu đã lưu**.

| Phần trong gói | Phạm vi được chọn | Truy vết |
|---|---|---|
| Tạo cộng đồng | Tên, mô tả tùy chọn, public/private; người tạo trở thành owner và thành viên active; khởi tạo @everyone theo đặc tả. Tạo server chưa bao gồm tạo phòng đầu tiên. | [UC-COM-01](../../specs/servers.md#uc-com-01) |
| Danh sách của mình | Chỉ cộng đồng có membership active của người dùng; phân trang và trạng thái chưa có cộng đồng. Đây là danh sách đã tham gia, không phải tìm kiếm cộng đồng công khai. | Phần danh sách của [UC-COM-03](../../specs/servers.md#uc-com-03) |
| Chi tiết và tư cách của mình | Metadata cộng đồng, membership hiện hành và quyền quản lý hiệu lực; đọc dữ liệu không tạo hoặc phục hồi membership. | Phần đọc detail/tư cách của [UC-COM-03](../../specs/servers.md#uc-com-03) |
| Giao diện và kiểm chứng | Form tạo, danh sách, chi tiết; trạng thái đang tải/rỗng/lỗi và thử lại thao tác chưa rõ kết quả; gọi API trực tiếp để kiểm tra điều kiện và quyền. | [UX Servers](../../specs/servers.md#ux), [điều kiện chung](../../specs/integration.md#use-case-conditions) |

UC-COM-03 là phạm vi bàn giao từng phần: tải phòng/lịch sử qua [UC-COM-17](../../specs/channels.md#uc-com-17), đối soát sau leave/rejoin và thay đổi membership đồng thời được kiểm chứng khi triển khai các luồng tương ứng. Các nhánh này vẫn giữ nguyên trong đặc tả nguồn; hoàn thành gói đầu chưa đủ để đánh dấu toàn bộ UC-COM-03 đạt.

Phần để sau gói đầu: tìm kiếm/trải nghiệm khám phá công khai (UC-COM-02), sửa thông tin/chế độ tham gia (UC-COM-04/05), tham gia/rời và lời mời (UC-COM-06–15), phòng/vai trò/ACL (UC-COM-16–22), tin phòng và realtime (UC-COM-23–25). Route detail dùng chung vẫn kiểm chứng projection summary public theo [thiết kế gói](../../design/create-view.md#api), không coi toàn bộ UC-COM-02 đã triển khai. Ownership và @everyone cần cho việc tạo vẫn nằm trong gói đầu; giao diện quản lý vai trò nằm ở gói sau. Media thuộc v1 theo [phạm vi MVP](../../../../releases/mvp.md). Việc để sau gói đầu không tự loại use case khỏi toàn bộ MVP.

## Tiêu chí hoàn thành gói đầu

Các dòng dưới đây chọn phần cần kiểm chứng từ đặc tả nguồn, không thay thế hoặc đánh dấu đạt toàn bộ AC/TC liên quan. Bước 2 rà soát nghiệp vụ và bổ sung tình huống cụ thể; bước 3 chốt request/response, lỗi và cách thử. Khi nghiệm thu, mỗi dòng cần có bằng chứng và kết quả riêng.

| Phần cần kiểm chứng | Kết quả cần quan sát | Nguồn đặc tả/ca kiểm thử |
|---|---|---|
| Tạo hợp lệ | Tài khoản đủ điều kiện tạo được public/private; đọc lại đúng metadata và trạng thái ban đầu đã chốt. | [UC-COM-01](../../specs/servers.md#uc-com-01), phần tạo của [AC-COM-29](../../specs/servers.md#ac-com-29)/[TC-COM-02](../../specs/servers.md#tc-com-02) |
| Điều kiện tài khoản/phiên | Phiên không hợp lệ hoặc tài khoản không đủ điều kiện bị chặn cả khi gọi API trực tiếp. | [Điều kiện chung](../../specs/integration.md#use-case-conditions), [COM-029](../../specs/servers.md#com-029) |
| Dữ liệu biên | Chấp nhận/từ chối tên, mô tả và Unicode đúng quy tắc; lỗi trả về không để lại cộng đồng dở dang. | Phần tên server của [AC-COM-38](../../specs/servers.md#ac-com-38)/[TC-COM-19](../../specs/servers.md#tc-com-19), phần tạo của [AC-COM-29](../../specs/servers.md#ac-com-29) |
| Tạo nguyên tử | Server, owner membership, @everyone và dữ liệu thao tác cùng commit; lỗi giữa chừng rollback toàn bộ phần tạo. | [UC-COM-01](../../specs/servers.md#uc-com-01), [giao dịch](../../design/integration.md#transactions), phần tạo của [TC-COM-02](../../specs/servers.md#tc-com-02) |
| Thử lại/đồng thời | Mất response rồi thử lại cùng khóa/payload chỉ có một server; cùng khóa khác payload bị từ chối theo hợp đồng. | [UC-COM-01](../../specs/servers.md#uc-com-01), phần tạo server của [TC-COM-23](../../specs/integration.md#tc-com-23) |
| Danh sách/detail/tư cách | Người tạo thấy cộng đồng, tư cách owner và quyền hiệu lực; danh sách rỗng/phân trang đúng; đọc không ghi hoặc phục hồi membership. | Phần được chọn của [UC-COM-03](../../specs/servers.md#uc-com-03), [thiết kế danh sách](../../design/integration.md#contracts) |
| Quyền đọc | Tài khoản khác không nhận member detail hoặc tư cách của người tạo; người ngoài mở cộng đồng private nhận 404. | [UC-COM-02](../../specs/servers.md#uc-com-02) (quy tắc che private), [UC-COM-03](../../specs/servers.md#uc-com-03), [điều kiện chung](../../specs/integration.md#use-case-conditions) |
| Luồng giao diện và lưu bền | Thực hiện trọn luồng qua UI/API với dữ liệu DB; reload hoặc restart API vẫn đọc được cộng đồng; trạng thái lỗi và thử lại đúng kết quả đã lưu. | [UX Servers](../../specs/servers.md#ux), [điều kiện bàn giao MVP](../../../../releases/mvp.md#acceptance) |

Dữ liệu kiểm chứng cần có: một tài khoản đủ điều kiện làm người tạo, một tài khoản đủ điều kiện chưa tham gia để kiểm tra quyền, tài khoản chưa xác minh và phiên không hợp lệ; cộng đồng public/private và đủ dữ liệu để kiểm tra phân trang. Đây là kế hoạch dữ liệu thử, chưa tạo tài khoản hoặc dữ liệu trong bước 1. Kết quả gói được ghi theo [mẫu nghiệm thu](../../../../release-operations.md#testing), kèm bản build, thao tác API/UI, kết quả từng dòng và phần UC còn lại.

<a id="first-package-business"></a>

## Rà soát nghiệp vụ gói tạo/xem — 2026-10-07

Đầu ra rà soát nghiệp vụ ngày 2026-10-07. Các tình huống dưới đây bổ sung kiểm chứng cho phạm vi gói đã chọn; đây là đầu vào thiết kế, không phải kết quả chạy.

| Nội dung | Quy tắc áp dụng trong gói đầu | Nguồn chuẩn |
|---|---|---|
| Người thực hiện | Tài khoản active, email đã xác minh, phiên hợp lệ; tạo không yêu cầu membership trước đó. Actor lấy từ phiên và phải còn đủ điều kiện khi thao tác được chấp nhận. | [Điều kiện chung](../../specs/integration.md#use-case-conditions), [DEC-051](../../../../decisions.md#dec-051), [COM-029](../../specs/servers.md#com-029) |
| Tên | Trim đầu/cuối, 2–100 UTF-16, cho tiếng Việt và emoji; từ chối tên trống/chỉ trắng/vô hình hoặc Unicode không hợp lệ. Hai cộng đồng được dùng cùng tên. | [COM-029](../../specs/servers.md#com-029), [COM-038](../../specs/servers.md#com-038), [COM-039](../../specs/servers.md#com-039) |
| Mô tả | Tùy chọn, tối đa 1.000 UTF-16; nội dung vượt giới hạn bị từ chối. | [COM-029](../../specs/servers.md#com-029), [COM-038](../../specs/servers.md#com-038) |
| Visibility và tham gia | Cho chọn public/private, mặc định public. Public mới mặc định vào ngay. Private không xuất hiện trong tìm kiếm; việc vào private vẫn theo đường lời mời được đặc tả cho gói sau. | [COM-002](../../specs/servers.md#com-002), [COM-003](../../specs/servers.md#com-003), [COM-029](../../specs/servers.md#com-029), [hành trình tham gia](../../README.md#requirements) |
| Người tạo | Sau thành công, người tạo là owner và thành viên active của chính cộng đồng; ownership lấy từ server, không dựng thêm role owner làm nguồn quyền thứ hai. | [UC-COM-01](../../specs/servers.md#uc-com-01), [ranh giới nội bộ](../../design/README.md#organization), [quyền owner](../../design/permissions.md#detailed-design) |
| Vai trò mặc định | Có @everyone, tự áp cho membership active, không có quyền quản lý và không cần gán thủ công cho người tạo. Gói đầu chưa mở quản lý custom role. | [COM-037](../../specs/permissions.md#com-037), [thiết kế vai trò](../../design/permissions.md#detailed-design) |
| Phòng ban đầu | Tạo cộng đồng không tự tạo phòng. Giao diện chi tiết của gói đầu hiển thị metadata/tư cách; danh sách phòng và lịch sử được nối khi triển khai UC-COM-17. | [UC-COM-01](../../specs/servers.md#uc-com-01), [phạm vi gói](#first-package) |
| Commit và thử lại | Thành công phải có đủ server, owner membership, @everyone và dữ liệu thao tác; lỗi giữa chừng không để lại phần tạo dở. Cùng thao tác/payload chỉ tạo một server, kể cả hai request đồng thời; cùng khóa khác payload là xung đột. | [UC-COM-01](../../specs/servers.md#uc-com-01), [điều kiện chung](../../specs/integration.md#use-case-conditions), [operation retry](../../design/integration.md#operations) |
| Danh sách của mình | Chỉ trả các cộng đồng người gọi có membership active; có phân trang và trạng thái rỗng. Danh sách đổi thì dedup theo ID và tải lại từ đầu, không hứa snapshot bất biến. | [UC-COM-03](../../specs/servers.md#uc-com-03), [quy tắc collection](../../design/integration.md#collections) |
| Chi tiết và tư cách | Member detail chỉ dành cho thành viên active; status riêng chỉ của người gọi. Đọc status không cấp quyền member detail hoặc tạo/phục hồi membership. Người ngoài private nhận 404; public summary không chứa owner/membership/quyền quản lý riêng. | [UC-COM-03](../../specs/servers.md#uc-com-03), [quy tắc collection](../../design/integration.md#collections), [OpenAPI](../../../../contracts/community.openapi.json) |
| Quyền của owner | Tư cách owner cấp các quyền quản lý đã đặc tả trong phạm vi cộng đồng sau kiểm tra điều kiện nền. Nó không cấp quyền đọc DM hoặc sửa/xóa tin của người khác; các thao tác quản lý ngoài gói đầu chưa được coi đã triển khai. | [Ma trận ACL](../../specs/permissions.md#permissions), [quyền owner](../../design/permissions.md#detailed-design) |

### Tình huống cần kiểm chứng cho gói

Các dòng này bổ sung tình huống cụ thể cho [tiêu chí gói đầu](#first-package) và phần được chọn của AC/TC hiện có. Đây là đầu vào kiểm chứng; kết quả thực thi nằm trong [hồ sơ nghiệm thu](acceptance.md). HTTP/lỗi, dữ liệu và cách kiểm chứng theo [thiết kế gói](../../design/create-view.md#verification), thực thi ở bước 4/5.

| Tình huống | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|
| Tạo public, bỏ lựa chọn visibility; tạo private | Public mặc định và cho vào ngay; private được tạo; mỗi lần tạo người gọi có membership active và là owner. | [UC-COM-01](../../specs/servers.md#uc-com-01), phần tạo của [AC-COM-29](../../specs/servers.md#ac-com-29) |
| Tài khoản chưa xác minh, inactive, phiên thiếu/hết hiệu lực hoặc actor không khớp phiên | Từ chối cả khi gọi API trực tiếp; không tạo server/membership/role/operation như đã thành công. Tài khoản mất điều kiện trước khi commit cũng không được hoàn tất thao tác tạo. | [Điều kiện chung](../../specs/integration.md#use-case-conditions), [giao dịch/guard](../../design/integration.md#transactions) |
| Tên 1/2/100/101 UTF-16 sau trim, tiếng Việt, emoji; tên chỉ trắng/vô hình; hai server cùng tên | Nhận đúng giới hạn, giữ cách viết hợp lệ và cho trùng tên; từ chối tên sai mà không lưu phần tạo dở. | [AC-COM-38](../../specs/servers.md#ac-com-38), phần tên server của [TC-COM-19](../../specs/servers.md#tc-com-19) |
| Mô tả bỏ trống, 1.000/1.001 UTF-16 | Nhận mô tả tùy chọn và đúng giới hạn; quá giới hạn trả lỗi để sửa form, không tạo dữ liệu dở dang. | [COM-029](../../specs/servers.md#com-029), phần tạo của [TC-COM-02](../../specs/servers.md#tc-com-02) |
| Lỗi khi ghi membership/@everyone hoặc phần còn lại của giao dịch tạo | Toàn bộ phần tạo rollback; danh sách không hiện server thiếu owner hoặc @everyone. | [UC-COM-01](../../specs/servers.md#uc-com-01), phần tạo của [TC-COM-02](../../specs/servers.md#tc-com-02) |
| Server đã commit nhưng response mất; thử lại cùng khóa/payload hoặc gửi đồng thời | Cùng một server được trả lại, không nhân server/membership/@everyone; không tự gửi lại mutation sau timeout/refresh. | [UC-COM-01](../../specs/servers.md#uc-com-01), phần tạo server của [TC-COM-23](../../specs/integration.md#tc-com-23) |
| Sau mất response, đổi tên/mô tả/visibility rồi dùng lại khóa cũ | Báo xung đột; giữ nguyên kết quả cũ, không tạo server thứ hai hoặc sửa server đã tạo. | [Operation retry](../../design/integration.md#operations) |
| Người tạo mở list/detail/status, tài khoản khác gọi cùng API; danh sách rỗng và nhiều trang | Người tạo đọc đúng tư cách và quyền; tài khoản khác không nhận member detail/status của người tạo, private bị che. Đọc/list không tạo hoặc phục hồi membership. | [UC-COM-03](../../specs/servers.md#uc-com-03), [collection](../../design/integration.md#collections) |
| Tải lại UI hoặc restart API sau tạo | Cộng đồng vẫn được đọc từ dữ liệu đã lưu; chỉ báo thành công sau xác nhận commit, lỗi giữ dữ liệu form để người dùng xử lý. | [UX](../../specs/servers.md#ux), [điều kiện bàn giao MVP](../../../../releases/mvp.md#acceptance) |

### Đầu vào kỹ thuật từ rà soát nghiệp vụ

Các đầu vào dưới đây được ghi ở lần rà soát bước 2 và cụ thể hóa tại [thiết kế gói bước 3](../../design/create-view.md); kết quả thực thi nằm trong [hồ sơ nghiệm thu](acceptance.md).

- Đối chiếu contract tạo/list/detail và `GET /servers/{serverId}/membership/me` với các tình huống trên; phân biệt quyền đọc public summary, member detail và status của chính mình. Phần tìm kiếm và trải nghiệm khám phá UC-COM-02 vẫn ở gói sau.
- Chuẩn hóa và lỗi trường: tên một dòng/control, thứ tự trim/kiểm tra Unicode, mô tả rỗng thành null, giới hạn UTF-16 và cách biểu diễn lỗi được chốt tại tài liệu thiết kế gói; bằng chứng tương ứng nằm trong hồ sơ nghiệm thu.
- Public mới mặc định `immediate` đã có căn cứ nghiệp vụ. Giá trị `joinMode` lưu cho private được chọn tại [thiết kế API gói](../../design/create-view.md#api); giá trị này không mở đường tham gia trực tiếp vào private.
- Chốt model/schema, migration trên dữ liệu legacy, version ban đầu, cursor, Identity guard, transaction, dedup/khóa và dữ liệu operation/outbox cần cho việc tạo. Scope gói đầu chỉ ghi outbox; dispatcher/Hub/realtime được duyệt và kiểm chứng riêng.
- Bổ sung assertion riêng cho phần đọc của UC-COM-03, các biên dữ liệu và race tạo/account; không suy rằng chạy TC-COM-02/19/23 là đã bao phủ các nhánh sửa metadata/role/channel hoặc toàn bộ UC-COM-03.

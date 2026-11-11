# SCDC — Server và chủ sở hữu

Người dùng tạo server (cộng đồng), tìm server công khai, xem các server mình đã tham gia. Chủ server sửa thông tin, chọn công khai hay riêng tư, chọn cách tham gia (vào ngay hay chờ duyệt), và chuyển quyền chủ cho người khác.

Phần Servers của Community sở hữu thông tin server, chế độ công khai, tìm kiếm, cách tham gia và quyền chủ sở hữu.

<a id="implementation"></a>

**Trạng thái** (nền `900e226`, 2026-10-13):

| Chức năng | Trạng thái | Bằng chứng |
|---|---|---|
| Tạo server (UC-COM-01) | Đã có backend và giao diện; đã kiểm thử | [Tạo và xem](../../records/verification/community/create-view.md) |
| Danh sách, chi tiết, tư cách (một phần UC-COM-03) | Phần đọc đã kiểm thử; chưa đối chiếu khi rời, tham gia lại, tin phòng | [Tạo và xem](../../records/verification/community/create-view.md) |
| Tìm kiếm, thông tin công khai (UC-COM-02) | Đã có tìm → tham gia server công khai vào ngay; chưa có nhánh chờ duyệt | [Tìm kiếm](../../records/verification/community/search.md) |
| Sửa thông tin và chế độ, đổi cách tham gia, chuyển chủ (UC-COM-04, 05, 14) | Chỉ có thiết kế | [Kế hoạch](../../project/planning.md#community-work-items) |

Tư cách thành viên: [Memberships](memberships.md). Quyền quản lý: [quyền](access-control.md). Điều kiện chung: [cơ chế Community](mechanics.md#use-case-conditions). Cách chạy: [hướng dẫn](../../guides/development.md#community).

Backup để [sau v1](../../project/overview.md#backup-scope). Migration v1 vẫn cần kiểm tra trước, giữ đúng dữ liệu, có kế hoạch rollback ứng dụng.

<a id="requirements"></a>

<a id="phạm-vi-và-quy-tắc"></a>

## Quy tắc

| Mã | Quy tắc |
|---|---|
| <a id="com-002"></a> COM-002 | Chỉ server công khai xuất hiện khi tìm kiếm. |
| <a id="com-003"></a> COM-003 | Server công khai mới tạo mặc định cho vào ngay; có thể đổi sang chờ duyệt. |
| <a id="com-006"></a> COM-006 | Chủ server hoặc người có quyền đổi được cách tham gia. |
| <a id="com-029"></a> COM-029 | Tài khoản đã xác minh tạo được server. Tên 2–100, mô tả tối đa 1.000; chọn công khai hay riêng tư (mặc định công khai). Chỉ chủ server sửa được các trường này. |
| <a id="com-033"></a> COM-033 | Chủ server chuyển quyền chủ ngay cho một thành viên đang hoạt động và đã xác minh; người nhận không cần đồng ý. Chủ cũ vẫn là thành viên, và chỉ rời được sau khi đã chuyển. |
| <a id="com-038"></a> COM-038 | Tìm kiếm chỉ server công khai, theo một phần tên. Từ khóa 2–100 UTF-16; không phân biệt hoa thường nhưng **có** phân biệt dấu; tên khớp đúng lên trước; mỗi trang 20, tối đa 50. Tên, mô tả, chủ đề đều đếm UTF-16. |
| <a id="com-039"></a> COM-039 | Tên Unicode được cắt khoảng trắng hai đầu, không được trống hay chỉ có ký tự vô hình. Tên server được trùng; tên phòng và vai trò là duy nhất trong server (không phân biệt hoa thường, có phân biệt dấu). v1 không có xóa server. |
| <a id="com-040"></a> COM-040 | Đổi sang riêng tư thì hủy mọi yêu cầu tham gia đang chờ và báo lý do; giữ thành viên và lời mời còn hiệu lực. |

<a id="use-cases"></a>

<a id="use-case"></a>

## Ca sử dụng

Điều kiện, version, epoch, gửi lại và lỗi dùng chung: [quy ước tích hợp](mechanics.md#use-case-conditions). Quy tắc, AC, TC: [bảng truy vết](traceability.md#use-case-coverage).

<a id="uc-com-01"></a>

<a id="uc-com-01--tạo-cộng-đồng"></a>

### UC-COM-01 — Tạo server

**Ai làm:** người dùng đủ điều kiện, phiên hợp lệ. Chưa cần là thành viên server nào.

**Khi nào:** chọn Tạo server ở COM-S10.

**Các bước:**

1. Nhập tên, mô tả (tùy chọn), chọn công khai hay riêng tư ([COM-029](#com-029), [COM-039](#com-039)).
2. Kiểm tra dữ liệu và quyền; trong cùng transaction tạo server, tư cách chủ, vai trò @everyone, và bản ghi chống trùng.
3. Sau commit, mở server vừa tạo và tải thông tin.

**Ngoại lệ:** dữ liệu sai: lỗi theo trường. Tạo đồng thời hay gửi lại cùng khóa và nội dung: chỉ một server. Mất phản hồi thì gửi lại bằng khóa cũ; khác nội dung thì không tạo server khác. Rollback không để lại server thiếu chủ hay thiếu @everyone.

**Kết quả:** người tạo là chủ và là thành viên. Server công khai mới mặc định vào ngay. Không tự tạo phòng đầu tiên; tạo phòng là [UC-COM-16](channels.md#uc-com-16).

<a id="uc-com-02"></a>

<a id="uc-com-02--tìm-và-xem-cộng-đồng-công-khai"></a>

### UC-COM-02 — Tìm và xem server công khai

**Ai làm:** người dùng đủ điều kiện, kể cả người chưa tham gia server đó.

**Khi nào:** tìm theo tên hoặc mở kết quả ở COM-S01, COM-S02.

**Các bước:**

1. Nhập từ khóa; hệ thống tìm đúng chuỗi ([COM-038](#com-038)), chỉ lấy server công khai đang hoạt động.
2. Hiện thông tin công khai, tên khớp đúng lên trước; có tải trang tiếp.
3. Mở một server, xem cách tham gia hiện tại, rồi chọn [UC-COM-06](memberships.md#uc-com-06) hoặc [UC-COM-07](memberships.md#uc-com-07).

**Ngoại lệ:** từ khóa hay cursor sai: lỗi. Không có kết quả: trạng thái trống. Server đã đổi sang riêng tư không còn hiện từ cursor cũ; người ngoài mở trực tiếp nhận 404. Xem trước bằng lời mời: [UC-COM-11](invitations.md#uc-com-11), [UC-COM-13](invitations.md#uc-com-13).

**Kết quả:** chỉ xem thông tin công khai; chưa là thành viên, chưa thấy phòng hay tin.

<a id="uc-com-03"></a>

<a id="uc-com-03--xem-cộng-đồng-đang-tham-gia-và-tư-cách-của-mình"></a>

### UC-COM-03 — Xem server đã tham gia và tư cách của mình

**Ai làm:** người dùng đủ điều kiện.

**Khi nào:** mở danh sách server, chọn một server, hoặc kiểm tra lại sau một thao tác không rõ kết quả.

**Các bước:**

1. Trả danh sách server người dùng đang là thành viên, có phân trang.
2. Khi chọn server: trả chi tiết, `membershipId`, version và quyền quản lý hiện có.
3. Giao diện hiện các thao tác phù hợp, tải phòng qua [UC-COM-17](channels.md#uc-com-17). Khi cần đối chiếu tham gia hay rời, đọc tư cách của chính mình.

**Ngoại lệ:** chưa tham gia server nào: danh sách trống. Sau khi rời, server không còn trong danh sách, nhưng người dùng vẫn đọc được tư cách "đã rời" của mình; điều đó không cho xem chi tiết hay nội dung riêng. Danh sách thay đổi: loại trùng theo ID, tải lại trang đầu.

**Kết quả:** giao diện có trạng thái tham gia và quyền hiện tại. Đọc không tạo hay khôi phục tư cách.

<a id="uc-com-04"></a>

<a id="uc-com-04--sửa-thông-tin-và-visibility-cộng-đồng"></a>

### UC-COM-04 — Sửa thông tin và chế độ công khai

**Ai làm:** chủ server hiện tại.

**Điều kiện trước:** đã tải server và version; quyền chủ kiểm tra lại khi ghi.

**Khi nào:** lưu tên, mô tả hay chế độ mới ở COM-S10.

**Các bước:**

1. Sửa các trường ([COM-029](#com-029), [COM-039](#com-039)), gửi kèm `expectedVersion`.
2. Kiểm tra quyền, dữ liệu, version; cập nhật. Nếu đổi từ công khai sang riêng tư: cùng lúc chuyển mọi yêu cầu đang chờ sang đã hủy, lý do `server_private` ([COM-040](#com-040)).
3. Sau commit, trả server hiện tại; báo người gửi yêu cầu bị hủy; báo các client có quyền tải lại thông tin.

**Ngoại lệ:** mất quyền chủ hay version cũ: không ghi đè. Duyệt và đổi sang riêng tư tranh nhau: theo thứ tự commit; tư cách đã tạo trước được giữ; yêu cầu đã bị hủy không duyệt được nữa. Đổi lại sang công khai không mở lại yêu cầu cũ.

**Kết quả:** thông tin, chế độ và yêu cầu nhất quán; thành viên và lời mời còn hiệu lực được giữ. Xóa server không có trong v1.

<a id="uc-com-05"></a>

<a id="uc-com-05--đổi-chế-độ-tham-gia"></a>

### UC-COM-05 — Đổi cách tham gia

**Ai làm:** chủ server hoặc thành viên có `manage_join_mode`.

**Khi nào:** chọn vào ngay hay chờ duyệt ở COM-S07.

**Các bước:**

1. Chọn cách mới, gửi `expectedVersion`.
2. Kiểm tra quyền và version, cập nhật.
3. Giao diện tải lại; lần tham gia công khai tiếp theo theo cách mới.

**Ngoại lệ:** mất quyền hay version cũ: từ chối. Quyền này không cho sửa tên, mô tả, chế độ công khai. Đổi cách tham gia không tự duyệt yêu cầu cũ; duyệt qua [UC-COM-08](memberships.md#uc-com-08).

**Kết quả:** tham gia công khai áp cách mới; link mời hợp lệ vẫn cho vào ngay ([COM-004](invitations.md#com-004)). Server riêng tư vẫn chỉ vào bằng lời mời.

<a id="uc-com-14"></a>

<a id="uc-com-14--chuyển-chủ-sở-hữu"></a>

### UC-COM-14 — Chuyển quyền chủ

**Ai làm:** chủ server hiện tại. Người nhận là thành viên đang hoạt động, đã xác minh.

**Khi nào:** chọn người nhận và xác nhận ở COM-S12.

**Các bước:**

1. Chọn người nhận theo ID từ danh sách được phép xem, gửi `expectedVersion`.
2. Trong cùng transaction, kiểm tra lại người thực hiện, người nhận, tư cách, version; đổi chủ và version quyền.
3. Sau commit, trả server; giao diện hai bên tải lại quyền. Chủ cũ vẫn là thành viên và có thể rời ([UC-COM-15](memberships.md#uc-com-15)).

**Ngoại lệ:** người nhận đã rời hay không đủ điều kiện: thất bại. Người nhận rời và chuyển chủ xảy ra cùng lúc: rời trước thì chặn chuyển; chuyển trước thì người nhận thành chủ và không rời được. Mất quyền chủ hay version cũ: tải lại; không tự thử lại khi không rõ kết quả.

**Kết quả:** luôn đúng một chủ; chuyển có hiệu lực ngay, người nhận không cần đồng ý. Chủ cũ giữ các vai trò đang có, không được tự gán vai trò quản lý.

<a id="ux"></a>

<a id="ux-và-trạng-thái"></a>

## Giao diện

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

- Tìm kiếm chỉ hiện server công khai. Nút tham gia theo cách vào ngay hay chờ duyệt.
- Link mời hợp lệ mở trang xác nhận đúng server rồi cho vào ngay (không chuyển thành chờ duyệt). Link hết hạn, đã thu hồi, không hợp lệ: không có nút tham gia, không hiện nội dung riêng.
- Chưa là thành viên thì không tải tin phòng.

| Màn hình | Ai dùng | Nội dung |
|---|---|---|
| COM-S07 Cách tham gia | Chủ server, người có quyền | Vào ngay hay chờ duyệt; ghi rõ link mời hợp lệ vẫn cho vào ngay |
| COM-S10 Tạo, sửa server | Người tạo; chỉ chủ server sửa | Tên, mô tả, công khai hay riêng tư |
| COM-S12 Chuyển quyền chủ | Chủ server | Chọn thành viên đang hoạt động, đã xác minh; xác nhận chuyển ngay. Chủ cũ vẫn là thành viên và rời được sau đó |

Màn hình hẹp: [giao diện chung](mechanics.md#ux).

<a id="technical"></a>

## Thiết kế dữ liệu và xử lý

<a id="metadata-server-và-ownership--thiết-kế-mục-tiêu"></a>

### Sửa thông tin và chuyển chủ (mục tiêu, chưa làm)

| Method và đường dẫn | Đầu vào | Kết quả và quyền |
|---|---|---|
| `PATCH /servers/{id}` | `{name?, description?, visibility?, expectedVersion}` | 200; chỉ chủ server; riêng tư thì không còn hiện khi tìm |
| `PATCH /servers/{id}/join-mode` | `{joinMode, expectedVersion}` | 200; cần quyền đổi cách tham gia; link hợp lệ vẫn cho vào ngay |
| `POST /servers/{id}/ownership-transfer` | `{newOwnerUserId, expectedVersion}` | 200; chủ hiện tại; người nhận là thành viên đang hoạt động, đã xác minh; chuyển ngay |

- **Công khai → riêng tư:** cập nhật chế độ và chuyển mọi yêu cầu đang chờ sang đã hủy (`server_private`) trong cùng transaction (DEC-096). Không xóa lịch sử yêu cầu; người gửi vẫn đọc được và được báo lý do. Thành viên và lời mời giữ nguyên. Yêu cầu cũ không duyệt được sau commit.
- **Chuyển chủ:** khóa người thực hiện, người nhận theo thứ tự, rồi server và tư cách. Người nhận phải đang hoạt động, đã xác minh, là thành viên tại lúc commit. Trường chủ ở `servers` là nguồn duy nhất.

<a id="create-models"></a>

<a id="create-model-và-trách-nhiệm"></a>

### Mô hình (tạo và xem, đã làm)

| Thành phần | Nội dung |
|---|---|
| Server | `id, ownerUserId, name, description, visibility, joinMode, status`, thời gian, `version, accessVersion`. Chủ chỉ lấy từ đây |
| Membership | Duy nhất theo `(serverId, userId)`; `membershipId`; active hay left; thời điểm tham gia, rời; `version`. GET không tạo tư cách |
| Role | Một @everyone: `isDefault=true, isSystem=true`, không có quyền quản lý. Không có vai trò "chủ" (chủ tính từ server) |
| CommunityOperation | Khóa (người thực hiện, loại, phạm vi, client); phiên bản fingerprint, khóa, hash; ID tài nguyên; thời điểm commit. Tạo server dùng loại `create_server`, phạm vi UUID 0 |
| CommunityDbContext | Mapping các bảng cần thiết; writer dùng kết nối và transaction chung; reader không tracking, trả DTO từ truy vấn có kiểm tra quyền |
| Guard Identity | Người thực hiện (người dùng, phiên, stamp) từ claims; Identity đọc schema của mình trong transaction được truyền vào |
| Relational work scope (BuildingBlocks) | Giữ một kết nối và transaction; commit, rollback; không có nghiệp vụ |
| Transactional outbox (BuildingBlocks) | Ghi vào `integration.outbox_events` cùng transaction; không cho Community ghi schema khác |
| API host | Controller lấy người thực hiện, chuyển request và `Result`; nghiệp vụ nằm trong Community |

- ID nghiệp vụ và tư cách dùng UUIDv7; `clientOperationId` là UUIDv4 đúng variant RFC. Thời gian UTC.
- Version gửi dạng chuỗi số nguyên dương. `servers.version` (integer, có trigger), `access_version` và `server_members.version` đều bắt đầu từ 1. Tạo chủ và @everyone là trạng thái ban đầu, không tăng `accessVersion` nhiều lần. Các thao tác sau phải kiểm tra tràn số.
- Chủ server có 5 quyền quản lý ([danh mục](access-control.md#permission-detailed-design)) và quyền chủ riêng. Thành viên khác: quyền là hợp các quyền từ vai trò tự tạo được gán (đúng epoch); không suy từ @everyone hay mã quyền cũ.

<a id="create-api"></a>

<a id="create-api-dữ-liệu-và-lỗi"></a>

### API tạo và xem (đã làm)

Tiền tố `/api/v1`. Schema: [OpenAPI](../../contracts/community.openapi.json).

| Thao tác | Đầu vào | Kết quả |
|---|---|---|
| `POST /servers` | `clientOperationId, name, description?, visibility?` | Tạo mới: 201 + `ServerDetail` + `Location`. Gửi lại hợp lệ: 200 + chi tiết hiện tại + cùng `Location`. Chỉ trả sau commit |
| `GET /servers` | `cursor?, limit?` | 200 `MyServerPage`: server đang hoạt động mà người gọi là thành viên; mặc định 20, 1–50 |
| `GET /servers/{serverId}` | UUID | Thành viên: `ServerDetail`. Người ngoài, server công khai: `ServerSummary`. Riêng tư hay không được biết: 404 |
| `GET /servers/{serverId}/membership/me` | UUID | Chỉ tư cách của người gọi; không có thì 404. Đã rời vẫn trả trạng thái (kể cả server riêng tư), nhưng không trả chi tiết riêng hay khôi phục tư cách |

- `ServerSummary`: `id, name, description, visibility, joinMode, version`. `ServerDetail` thêm `ownerUserId, accessVersion, myMembership, effectivePermissions`.
- Trạng thái "đã rời" được kiểm thử bằng fixture; chưa chứng minh API rời.

**Thứ tự kiểm tra dữ liệu:**

1. JSON, trường, enum, UUID. Không nhận chủ, người thực hiện, slug, version hay trạng thái từ client. Không gửi chế độ thì là công khai; server riêng tư cũng lưu cách tham gia "vào ngay" nhưng chế độ riêng tư vẫn chặn tham gia trực tiếp.
2. **Tên:** Unicode hợp lệ; cắt khoảng trắng theo `White_Space` ([text-policy](../../fixtures/text-policy.json)); 2–100 UTF-16; từ chối ký tự điều khiển, CR, LF, U+2028, U+2029, tên chỉ có khoảng trắng hay ký tự vô hình. Giữ hoa thường, tiếng Việt, emoji; không NFC tên hiển thị. (`normalized_name` cũ không thay khóa `search_name`.)
3. **Mô tả:** văn bản thuần; CRLF, CR thành LF; không cắt nội dung có chữ, không NFC; rỗng thành null; kiểm tra Unicode, NUL; tối đa 1.000 UTF-16. Giao diện, backend, fingerprint dùng cùng thứ tự.
4. **Slug:** sinh từ UUID (32 ký tự hex thường); không dùng tên làm định danh.

| HTTP và mã | Khi nào |
|---|---|
| 400 `VALIDATION_FAILED` | Trường, Unicode, độ dài, enum, UUID sai; có `errors` theo trường |
| 400 `CURSOR_INVALID` | Cursor sai người, mục đích, giới hạn; bị sửa hay hết hạn. Tải lại từ trang đầu |
| 401 | Middleware JWT; guard phát hiện phiên hay stamp sai: `SESSION_INVALID` |
| 403 `ACCOUNT_ACCESS_DENIED` | Tài khoản không hoạt động hay chưa có email chính đã xác minh |
| 403 `PERMISSION_DENIED` | Gửi lại tạo server công khai nhưng người gọi không còn là thành viên. Riêng tư thì 404. Không tạo lại |
| 404 `RESOURCE_NOT_FOUND` | Không tồn tại, không được biết, không có tư cách; không lộ thông tin |
| 409 `OPERATION_CONFLICT` | Cùng khóa tạo, khác nội dung |
| 503 `FINGERPRINT_KEY_UNAVAILABLE`, `CURSOR_KEY_UNAVAILABLE` | Không đọc được khóa |
| 503 `ACCESS_CHECK_UNAVAILABLE`, `COMMUNITY_TEMPORARILY_UNAVAILABLE` | Guard hay DB lỗi tạm, chờ khóa quá hạn, deadlock, không xác nhận được commit. Người dùng đối chiếu hay gửi lại bằng khóa cũ |

Không biến mọi lỗi DB thành 400 hay 409. Lỗi ràng buộc do writer sai hay lỗi bất ngờ dùng 500; chi tiết SQL chỉ ở log nội bộ. Timeout sau commit là kết quả không rõ; bản ghi thao tác là nguồn đối chiếu.

<a id="giao-dịch-tạo-và-đọc"></a>

### Transaction tạo và đọc

Dùng chung [work scope và guard Identity](mechanics.md#account-transactions).

**Tạo:**

1. Chuẩn hóa và kiểm tra; mở work scope, giữ guard Identity. Tra thao tác theo người thực hiện + `create_server` + UUID 0 + `clientOperationId`.
2. **Đã có thao tác:** dùng phiên bản fingerprint và khóa đã lưu để so nội dung. Giống thì kiểm tra quyền và đọc tài nguyên; không ghi lại thao tác hay outbox. Khác thì 409. Thiếu tài nguyên là lỗi nhất quán; không tạo lại.
3. **Chưa có:** lấy khóa HMAC đang dùng; sinh ID; ghi server, tư cách chủ, @everyone, thao tác, outbox trong cùng transaction.
4. Hai request cùng khóa có thể cùng qua bước tra cứu; ràng buộc unique quyết định một bên thắng. Bên thua phải rollback **toàn bộ** transaction và DbContext, mở transaction mới, lấy lại guard, đọc thao tác đã commit để xử lý như gửi lại. Không chỉ rollback savepoint mà giữ server thứ hai. Không coi ràng buộc khác là gửi lại.
5. Kiểm tra thời hạn guard, commit, trả DTO và `Location`. Deadlock hay timeout: lỗi tạm; không tự chạy lại.

**Đọc:** dùng guard và **một** câu SQL projection cho toàn bộ thông tin, tư cách, quyền của một trang hay chi tiết, để quyền và dữ liệu cùng một thời điểm (`READ COMMITTED` chụp theo từng câu lệnh; [PostgreSQL](https://www.postgresql.org/docs/18/transaction-iso.html#XACT-READ-COMMITTED)). Mỗi trang kiểm tra lại quyền, không lấy quyền từ cursor.

**Outbox:** ghi một sự kiện `Community.ServerCreated.v1` (server, chủ, `accessVersion`) cho mỗi lần tạo. Chưa có worker hay Hub nào xử lý; giao diện cập nhật qua HTTP. Sự kiện chưa phát thì không đánh dấu đã phát.

<a id="cursor-danh-sách-của-mình"></a>

### Cursor danh sách server của mình

Sắp theo `server.id` giảm dần; keyset `id < lastServerId`; lấy `limit + 1`; chỉ server và tư cách đang hoạt động. Cursor bảo vệ bằng Data Protection, purpose `Community.MyServers.v1`, gồm người dùng, giới hạn, `lastServerId`, hạn 24 giờ, phiên bản định dạng. Chỉ trả cursor khi còn trang. Không dùng offset hay tên làm vị trí. Tham gia, rời đồng thời có thể đổi danh sách; giao diện loại trùng theo ID và tải lại từ đầu.

<a id="create-migration"></a>

<a id="create-migration-và-dữ-liệu-legacy"></a>

### Migration 001 và dữ liệu cũ

File `database/postgres/migrations/001-community-create-view.sql`. Công cụ chạy ghi module, version, checksum, thời điểm vào ledger `common.schema_migrations`, khóa ledger khi chạy, chạy trong transaction, dừng khi checksum khác. API không tự tạo schema.

| Phần | Thay đổi |
|---|---|
| `servers` | Thêm `visibility` (public, private), `join_mode` (immediate, approval), `access_version ≥ 1`; mô tả lên 1.000. Server mới: active, chế độ theo request, immediate |
| Tên, mô tả | Thêm hàm `common.utf16_length(text)` (đếm ký tự ngoài BMP là 2). Kiểm tra tên 2–100 và mô tả ≤ 1.000 theo UTF-16. Service vẫn kiểm tra trống, ký tự điều khiển, Unicode |
| `server_members` | Thêm `membership_id` (UUIDv7, duy nhất), `version ≥ 1`; giữ unique `(server_id, user_id)`. Điền epoch cho dữ liệu cũ đã duyệt; không tự đổi kicked, banned thành active hay left |
| Ràng buộc chủ | Khóa ngoại deferred từ `(servers.id, owner_user_id)` tới tư cách; trigger deferred kiểm tra chủ là thành viên đang hoạt động lúc commit |
| Ràng buộc @everyone | Trigger deferred đảm bảo mỗi server có đúng một @everyone, không có quyền quản lý |
| Thao tác | Bảng thao tác Community (khóa chính ghép, fingerprint, khóa, version, tài nguyên). 001 chỉ nhận `create_server`; 003, 004 mở rộng |
| Index | Index tư cách đang hoạt động `(user_id, server_id)`; index `membership_id` |
| Outbox | Dùng `integration.outbox_events` có sẵn; chưa có lease, worker, realtime cho sự kiện Community |

`char_length` của PostgreSQL đếm ký tự, không đếm UTF-16. Hàm mới bằng `char_length` + số ký tự > U+FFFF. Ví dụ tên chỉ gồm một emoji ngoài BMP có độ dài 2 và hợp lệ. Fixture .NET và SQL kiểm tra hai bên khớp ([PostgreSQL](https://www.postgresql.org/docs/18/functions-string.html)).

**Kiểm tra trước khi đổi ràng buộc:**

1. Chủ có tư cách đang hoạt động; tên, mô tả hợp lệ; epoch và trạng thái tư cách; vai trò mặc định, hệ thống, quyền; trùng tên khi đổi vai trò mặc định thành @everyone. Báo ID và lỗi; dừng nếu không có mapping đã duyệt.
2. Server cũ chưa có chế độ, nên không tự cho là công khai. Điền qua file mapping theo ID đã duyệt.
3. Không tự sửa tên, xóa vai trò hay quyền, đổi trạng thái thành viên, hay mở chế độ công khai. Không chạy `schema.sql` (có DROP SCHEMA) trên database cần giữ.
4. Chạy và kiểm tra trong transaction, ghi ledger; chạy lại cùng checksum thì không làm gì. Khi rollback ứng dụng, giữ schema mới; không DROP cột hay hạ mô tả về 500.

`schema.sql` và `seed.sql` cho database mới được đồng bộ với schema sau chuỗi migration; ledger ghi baseline và checksum. Test migration vẫn bắt đầu từ schema cũ riêng để chứng minh đường nâng cấp.

<a id="tìm-kiếm-và-discovery"></a>

### Tìm kiếm

<a id="search-key-tên-và-migration"></a>

#### Khóa tìm kiếm và migration 002

- `UnicodeTextPolicy.NormalizeNameKey`: cắt khoảng trắng → NFC → `ToLowerInvariant` ([Normalize](https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize?view=net-10.0), [ToLowerInvariant](https://learn.microsoft.com/en-us/dotnet/api/system.string.tolowerinvariant?view=net-10.0)). Không đổi tên hiển thị, không bỏ dấu.
- Cột `community.servers.search_name text COLLATE "C" NOT NULL`, ghi cùng tên trong transaction. Không dùng `normalized_name` (theo locale PostgreSQL). Collation C giữ dấu và thứ tự byte ổn định ([collation](https://www.postgresql.org/docs/18/collation.html)).
- Xếp hạng: khớp đúng = 0, khớp một phần = 1; rồi khóa tăng dần; rồi UUID tăng dần.

**Migration 002** (`002-community-search.sql`):

- Công cụ .NET tính khóa theo lô tối đa 500, đưa vào bảng tạm. Dùng cùng advisory lock của ledger và table lock; kiểm tra 001 và checksum; kiểm tra tên; chạy DDL và điền dữ liệu trong transaction. Không đổi tên, version, `accessVersion`, epoch.
- Khi điền, tạm tắt trigger tăng version và `updated_at`. Trigger kiểm tra chủ và @everyone vẫn bật; sau UPDATE, `SET CONSTRAINTS ALL IMMEDIATE` kiểm tra ngay rồi trả về DEFERRED ([SET CONSTRAINTS](https://www.postgresql.org/docs/18/sql-set-constraints.html)). Lỗi thì rollback cả DDL, dữ liệu và trạng thái trigger.
- **Phải dừng writer cũ trước** khi chạy, vì `search_name NOT NULL` làm writer cũ không tạo server được. Dữ liệu cần sửa: báo ID và rollback.
- Index partial btree `(search_name, id)` cho server công khai, đang hoạt động. Tìm "chứa" có ký tự đại diện ở đầu vẫn có thể quét nhiều; chưa đo tải, chưa thêm `pg_trgm`.

<a id="search-api-và-cursor"></a>

#### API tìm kiếm

`GET /api/v1/servers/search`:

- `q` bắt buộc; cắt khoảng trắng rồi kiểm tra 2–100 UTF-16, Unicode hợp lệ, có nội dung, một dòng. `limit` 1–50, mặc định 20.
- Trả `200 {items: ServerSummary[], nextCursor}`; luôn đủ sáu trường; không có chủ, tư cách, quyền, phòng. Không tạo tư cách hay sự kiện.
- LIKE dùng tham số và escape `%`, `_`, `\` ([LIKE](https://www.postgresql.org/docs/18/functions-matching.html)). Một câu SQL lọc và keyset `(rank, key, id)`, lấy `limit + 1`; không OFFSET.
- Cursor purpose `Community.Search.v1`: phiên bản, người dùng, khóa từ khóa, giới hạn, vị trí cuối, hạn 24 giờ. Từ khóa viết khác nhưng cùng khóa chuẩn hóa thì dùng chung cursor được. Sai người, khóa, giới hạn, purpose, bị sửa, hết hạn: `400 CURSOR_INVALID`. Lỗi key ring: `503 CURSOR_KEY_UNAVAILABLE`.
- Guard Identity giữ khóa và lease; đọc bằng một câu lệnh; kiểm tra lease trước khi xong. Mỗi trang kiểm tra lại công khai và đang hoạt động. Thông tin công khai đã tải có thể cũ tới lần tải lại; mở chi tiết hay tham gia luôn kiểm tra trạng thái hiện tại.
- Lỗi: 400 `VALIDATION_FAILED`, 401 hay `SESSION_INVALID`, 403 `ACCOUNT_ACCESS_DENIED`, 400 `CURSOR_INVALID`, 503 `ACCESS_CHECK_UNAVAILABLE`, `CURSOR_KEY_UNAVAILABLE`, `COMMUNITY_TEMPORARILY_UNAVAILABLE`. Không biến lỗi phụ thuộc thành danh sách trống.

<a id="search-webclient"></a>

#### Giao diện tìm kiếm

- Nút Khám phá mở `#discover`. Gửi từ khóa hợp lệ thì lưu vào URL `#discover?q=...`, chỉ GET. Từ khóa trống: hiện hướng dẫn, không gọi API. Kiểm tra cùng quy tắc UTF-16 trước khi gửi; không cắt hay đổi chữ thường từ khóa người dùng nhập.
- Kết quả có tên, mô tả, chế độ, cách tham gia, nút Xem. Mở chi tiết giữ đường quay lại cùng từ khóa. Server công khai vào ngay có nút Tham gia; chờ duyệt thì chỉ giải thích (tới khi có chức năng yêu cầu). Danh sách server của mình tải lại sau khi tham gia.
- Đổi từ khóa, người dùng hay trang thì hủy request cũ và bỏ phản hồi cũ. Phân trang loại trùng theo UUID. Lỗi trang hay cursor thì bỏ kết quả cũ, cho tải lại từ đầu; không ghép trang của hai từ khóa. Tải lại URL thì GET lại từ đầu; không tự POST tham gia. Không lưu kết quả vào bộ nhớ trình duyệt.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| <a id="ac-com-01"></a> AC-COM-01 | Có một server công khai và một riêng tư; người dùng tìm | Chỉ hiện server công khai |
| <a id="ac-com-08"></a> AC-COM-08 | Chủ server hay người có quyền đổi cách tham gia | Tham gia qua tìm kiếm theo cách mới; link mời hợp lệ vẫn cho vào ngay |
| <a id="ac-com-29"></a> AC-COM-29 | Tài khoản đã xác minh tạo server; chủ sửa tên, mô tả, chế độ; thành viên gọi cùng API sửa | Tạo và sửa theo giới hạn; thành viên không sửa được; riêng tư không còn hiện khi tìm |
| <a id="ac-com-33"></a> AC-COM-33 | Chủ chuyển quyền cho thành viên hợp lệ, trong khi người nhận hay chủ cũ thử rời | Chuyển ngay, không cần đồng ý; luôn đúng một chủ; chủ cũ rời được sau khi chuyển xong |
| <a id="ac-com-37"></a> AC-COM-37 | Tìm tên Unicode có và không dấu; biên từ khóa; phân trang; đổi server sang riêng tư giữa các trang | Đúng 2–100, trang 20–50, khớp đúng lên trước; không phân biệt hoa thường, có phân biệt dấu; server riêng tư không hiện từ cursor cũ |
| <a id="ac-com-38"></a> AC-COM-38 | Tên có emoji, tiếng Việt, khoảng trắng, trống, vô hình; hai tên chỉ khác hoa thường hay dấu | Đếm UTF-16, cắt khoảng trắng; tên server được trùng; tên phòng và vai trò duy nhất (không phân biệt hoa thường, có phân biệt dấu) |
| <a id="ac-com-39"></a> AC-COM-39 | Chủ tìm cách xóa toàn bộ server | Không có thao tác hay API xóa server trong v1; vẫn có xóa phòng và chuyển chủ |
| <a id="ac-com-40"></a> AC-COM-40 | Server công khai có yêu cầu đang chờ, đổi sang riêng tư trong lúc người duyệt đang duyệt | Theo đúng một thứ tự commit; yêu cầu còn lại bị hủy (`server_private`), người gửi biết lý do; thành viên và lời mời được giữ |

Tiêu chí liên quan nhiều phần: [bảng truy vết](traceability.md#use-case-coverage).

<a id="tests"></a>

<a id="ca-kiểm-thử"></a>

## Ca kiểm thử

Dữ liệu và cách ghi bằng chứng: [dùng chung](../messaging/channel-messaging.md#evidence). Mỗi ca cần test cho từng endpoint và kiểm tra quyền qua API.

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| <a id="tc-com-01"></a> TC-COM-01 | Tìm server công khai và riêng tư; đổi một server sang riêng tư rồi tìm lại | Chỉ có kết quả công khai; người ngoài không thấy phòng hay tin | [AC-COM-01](#ac-com-01), [AC-COM-19](invitations.md#ac-com-19), [AC-COM-29](#ac-com-29) |
| <a id="tc-com-02"></a> TC-COM-02 | Tạo server ở biên tên và mô tả; N sửa thông tin server của O qua API | Đúng giới hạn; chỉ O sửa được; không có tư cách chủ dở dang | [AC-COM-29](#ac-com-29) |
| <a id="tc-com-14"></a> TC-COM-14 | Chuyển chủ cùng lúc người nhận hay chủ cũ rời; người nhận mất trạng thái hoạt động | Luôn một chủ; người nhận hợp lệ lúc chuyển; chủ cũ chỉ rời được sau khi chuyển xong | [AC-COM-33](#ac-com-33) |
| <a id="tc-com-18"></a> TC-COM-18 | Từ khóa ở biên, tiếng Việt, hoa thường, dấu, ký tự `%`, `_`, `\`; đổi sang riêng tư khi đang dùng cursor | Tìm đúng chuỗi, xếp hạng khớp đúng, phân trang đúng; không lộ server riêng tư | [AC-COM-37](#ac-com-37) |
| <a id="tc-com-19"></a> TC-COM-19 | Tên dài 1, 2, 100, 101 UTF-16; emoji, dấu tổ hợp; vai trò 64 và 65; trùng khóa sau khi cắt, đổi hoa thường, NFC | Unicode hợp lệ; tính duy nhất đúng; không có dữ liệu dở dang | [AC-COM-38](#ac-com-38) |
| <a id="tc-com-20"></a> TC-COM-20 | Đổi sang riêng tư tranh với duyệt và hủy; người gửi đọc trạng thái sau đó | Yêu cầu còn lại bị hủy cùng commit; tư cách đã commit trước được giữ | [AC-COM-40](#ac-com-40) |
| <a id="tc-com-27"></a> TC-COM-27 | Tìm giao diện hay API xóa server; gửi tin chữ trong phòng thoại | Không mở phạm vi ngoài DEC-094; loại phòng theo hợp đồng; media kiểm thử riêng | [AC-COM-39](#ac-com-39) |

<a id="gaps"></a>

<a id="việc-còn-lại"></a>

## Còn mở

Phụ thuộc chung: [kế hoạch](../../project/planning.md#community-work-items), [migration](mechanics.md#schema-migration), [vòng đời dữ liệu](../../system/data-lifecycle.md).

- Đổi sang riêng tư cần bằng chứng cùng Memberships.
- Chuyển chủ cần bằng chứng với việc người nhận rời và guard Identity.
- Tên và tìm kiếm Unicode, version, chống trùng, mapping dữ liệu cũ của tạo và tìm đã có bằng chứng ([đối chiếu test](../../records/verification/community/README.md)); những bằng chứng đó không áp cho đổi sang riêng tư hay chuyển chủ.

---

Nguồn: DEC-024 (COM-002), DEC-025 (COM-003), DEC-029 (COM-006), DEC-072 (COM-029), DEC-076 (COM-033), DEC-093 (COM-038), DEC-094, DEC-095 (COM-039), DEC-096 (COM-040), DEC-156.

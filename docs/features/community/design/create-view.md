# SCDC — Thiết kế gói tạo và xem cộng đồng

Thiết kế chốt ngày 2026-10-07 cho [gói tạo/xem](../delivery/create-view/plan.md#first-package), dựa trên [rà soát nghiệp vụ](../delivery/create-view/plan.md#first-package-business). Bảng đối chiếu source ghi thời điểm bước 3; trạng thái hiện tại tại [status.md](../status.md), bằng chứng tại hồ sơ nghiệm thu của gói.

Phạm vi: UC-COM-01 và phần list/detail/tư cách của UC-COM-03. Quy tắc sản phẩm giữ ở [Servers](../specs/servers.md#requirements), [Permissions](../specs/permissions.md#permissions) và [điều kiện chung](../specs/integration.md#use-case-conditions). Tài liệu này chốt cách thực hiện gói; thuật toán dùng chung dẫn chiếu [thiết kế tích hợp](integration.md#detailed-design), không thay phạm vi các gói sau.

## Mục lục

- [Đối chiếu source](#source)
- [Model và trách nhiệm](#models)
- [API, dữ liệu và lỗi](#api)
- [Transaction và Identity guard](#transactions)
- [Operation, cursor và khóa](#keys)
- [Migration và dữ liệu legacy](#migration)
- [Kiểm chứng và gói code](#verification)

<a id="source"></a>

## Đối chiếu source

| Hiện có | Hệ quả cho gói |
|---|---|
| [CommunityModule](../../../../services/Modules/Community/CommunityModule.cs) chỉ đăng ký descriptor; project chưa có EF provider | Bổ sung DbContext, services/DI và controller trong bước 4; dùng cùng phiên bản provider với Identity, hiện là 10.0.3 |
| [IdentityModule](../../../../services/Modules/Identity/IdentityModule.cs) kiểm tra session/stamp trong JWT middleware; [IUserDirectory](../../../../services/SCDC.Contracts/Identity/IUserDirectory.cs) chỉ trả user summary | Middleware và directory chưa giữ điều kiện tài khoản đến commit; bổ sung guard do Identity triển khai |
| [IdentityDbContext.LockUserAsync](../../../../services/Modules/Identity/Infrastructure/Persistence/IdentityDbContext.cs) dùng `FOR NO KEY UPDATE`; các mutation bảo mật hiện gọi khóa user trước khi sửa email/stamp/session | Guard lấy `FOR SHARE` trên cùng user, kiểm tra lại dưới khóa; mọi writer làm đổi điều kiện tài khoản phải giữ cùng quy ước |
| [schema.sql](../../../../database/postgres/schema.sql) có server/member/role và trigger tăng server version; chưa có visibility, join mode, membership epoch hoặc operation | Migration bổ sung đúng phần gói cần; giữ trigger server làm nguồn tăng metadata version |
| [seed.sql](../../../../database/postgres/seed.sql) dùng system role `Owner`/`Member` và mã permission legacy | Seed hiện tại chưa phải dữ liệu kiểm chứng gói mới; đồng bộ bootstrap/seed riêng cho DB Development mới, preflight dữ liệu cũ trước migration |
| [ApiControllerBase](../../../../services/SCDC.Api/Controllers/ApiControllerBase.cs) và [ApiErrorMapper](../../../../services/SCDC.Api/Errors/ApiErrorMapper.cs) đã có Result/ProblemDetails | Dùng cơ chế lỗi hiện có; không tạo định dạng lỗi Community riêng |

<a id="models"></a>

## Model và trách nhiệm

| Model/thành phần | Dữ liệu hoặc trách nhiệm được chọn |
|---|---|
| Server / Servers | `id`, `ownerUserId`, `name`, `description`, `visibility`, `joinMode`, `status`, timestamps, `version`, `accessVersion`; ownership chỉ lấy từ server |
| Membership / Memberships | `(serverId,userId)` duy nhất, `membershipId`, active/left, joined/left timestamps, `version`; không dựng membership từ thao tác GET |
| Role / Permissions | Khởi tạo một @everyone: `isDefault=true`, `isSystem=true`, không management permission. Tư cách owner tính từ server, không tạo role owner |
| CommunityOperation / Infrastructure | Khóa actor/kind/scope/client, fingerprint version/key/hash, resource ID, thời điểm commit; chỉ hỗ trợ `create_server` trong gói |
| CommunityDbContext / Infrastructure | Mapping các bảng Community cần cho gói; writer dùng connection/transaction chung. Reader không tracking, trả DTO từ query có quyền |
| Identity guard / Contracts + Identity | Actor gồm user/session/security stamp từ claims đã xác thực; Identity đọc schema của mình trong transaction được truyền vào |
| Relational work scope / BuildingBlocks | Sở hữu một connection/transaction, enlist DbContext, commit/rollback và dispose; không chứa nghiệp vụ Community/Identity |
| Transactional outbox / BuildingBlocks | Ghi envelope vào `integration.outbox_events` cùng transaction; đây là hạ tầng dùng chung, không cho Community ghi schema Messaging/Identity |
| API host | Controller xác định actor, ánh xạ request/Result/Location; orchestration nghiệp vụ nằm trong Community |

ID nghiệp vụ và membership dùng UUIDv7; `clientOperationId` phải là UUIDv4 có đúng variant RFC. Timestamps UTC. Version trên wire là chuỗi số nguyên dương. Gói giữ `servers.version` integer và trigger hiện có; `access_version` và `server_members.version` bổ sung cũng là integer dương. Tất cả khởi tạo 1; tạo owner/@everyone là snapshot ban đầu, không tăng accessVersion thành nhiều lần. Cận của schema wire không làm mở rộng cận integer trong DB; xử lý tăng version ở các mutation sau phải kiểm tra overflow.

Owner nhận năm management code trong [catalogue](permissions.md#detailed-design) và quyền ownership riêng. Với thành viên khác, projection chỉ hợp các code được catalogue hỗ trợ từ custom role được gán; không suy grant từ @everyone hoặc mã permission legacy. Gói chưa có API gán role/ACL, nhưng read model giữ đúng schema quyền cho dữ liệu hợp lệ đã có.

<a id="api"></a>

## API, dữ liệu và lỗi

Prefix `/api/v1`. [OpenAPI Community](../../../contracts/community.openapi.json) giữ schema máy đọc được; bốn thao tác sau được chọn cho bước 4. Status của toàn bộ contract vẫn là thiết kế, không phải API đang chạy.

| Thao tác | Đầu vào | Kết quả được chọn |
|---|---|---|
| `POST /servers` | `clientOperationId`, `name`, `description?`, `visibility?` | 201 + `ServerDetail` + Location khi tạo mới; 200 + detail hiện hành + cùng Location khi retry hợp lệ. Chỉ trả thành công sau commit |
| `GET /servers` | `cursor?`, `limit?` | 200 `MyServerPage`; chỉ server active/nondeleted có membership active của actor; limit mặc định 20, nhận 1–50 |
| `GET /servers/{serverId}` | UUID tài nguyên | Member active nhận `ServerDetail`; người ngoài public nhận `ServerSummary`; private hoặc server không được biết nhận 404 |
| `GET /servers/{serverId}/membership/me` | UUID tài nguyên; actor từ phiên | Chỉ membership của actor; không có record thì 404. Khi record đã left, vẫn trả status của chính mình kể cả server private, không trả private detail hoặc phục hồi membership |

`ServerSummary` chỉ có `id,name,description,visibility,joinMode,version`; `ServerDetail` thêm `ownerUserId,accessVersion,myMembership,effectivePermissions`. Route detail dùng chung public/member view theo contract; kiểm chứng projection public để tránh lộ dữ liệu. Điều này không bao gồm search, màn hình khám phá hoặc nghiệm thu toàn bộ UC-COM-02. Membership left có thể dựng bằng fixture để kiểm chứng read contract; gói không thêm endpoint leave/rejoin.

Thứ tự validation được chọn:

1. Kiểm tra JSON/field/enum/UUID; không nhận owner, actor, slug, version hoặc trạng thái do client chỉ định. Visibility bỏ trống thành public; private cũng lưu joinMode immediate, nhưng visibility vẫn chặn đường tham gia trực tiếp.
2. Tên: kiểm tra Unicode hợp lệ, trim theo tập White_Space của [text-policy](../../../fixtures/text-policy.json), đếm 2–100 UTF-16; từ chối control, CR/LF, U+2028/U+2029 và tên chỉ trắng/vô hình. Giữ case, tiếng Việt, emoji và các scalar ignorable trong tên có nội dung; không NFC tên hiển thị. `normalized_name` hiện có chưa là search key đã chốt cho gói tìm kiếm.
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

<a id="transactions"></a>

## Transaction và Identity guard

Chọn một connection PostgreSQL và transaction `READ COMMITTED` cho từng application operation. CommunityDbContext enlist transaction đó; `IAccountAccessGuard` trong Contracts nhận actor và `DbTransaction`, không phụ thuộc EF hoặc implementation Identity. Identity dùng connection thuộc transaction để khóa/kiểm tra schema mình; JWT DbContext của middleware không tự mở một transaction thứ hai cho guard. Không chạy query song song trên connection chung. EF Core hỗ trợ share connection và transaction giữa các context; đây là cơ sở triển khai, chưa là bằng chứng của writer gói. [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Guard lấy `FOR SHARE` trên `identity.users`, rồi đọc lại user active/nondeleted, primary email verified, session thuộc user/chưa revoked/chưa hết hạn và stamp khớp claims. Các writer bảo mật hiện lấy `FOR NO KEY UPDATE` trên user trước khi sửa các record con; quy ước này phải được giữ cho writer mới. Share lock xung đột với no-key-update và giữ đến cuối transaction; lựa chọn guard này là suy luận thiết kế từ source và cơ chế lock, cần race test ở bước 4. [PostgreSQL row locks](https://www.postgresql.org/docs/18/explicit-locking.html#LOCKING-ROWS).

Khóa bảo vệ trạng thái có writer, không ngăn thời gian trôi. Guard giữ session expiry và kiểm tra thời gian lại ngay trước commit/hoàn tất read; nếu hết hạn thì dừng/rollback. Không kéo transaction qua tương tác người dùng hoặc gọi dịch vụ ngoài. Chọn lock timeout 2 giây, command timeout 5 giây và cancellation từ request; đây là cấu hình ban đầu để kiểm chứng, không phải kết quả đo tải.

Thứ tự tạo:

1. Normalize/validate, mở work scope và giữ Identity guard. Tra operation theo actor + `create_server` + UUID zero + client ID.
2. Nếu operation đã có, dùng fingerprint version/key đã lưu để so payload; đúng thì kiểm tra quyền và đọc tài nguyên hiện hành, không ghi lại operation/outbox; khác thì 409. Thiếu resource là lỗi nhất quán, không tạo lại server bằng khóa cũ.
3. Nếu chưa có, lấy active HMAC key, sinh server/membership/role/event ID và ghi server + owner membership + @everyone + operation + outbox trong cùng transaction. Khóa đầu vào mới không dùng để khóa một server chưa tồn tại.
4. Hai create cùng operation có thể cùng chạy sau lookup. Unique operation xác định một bên thắng. Bên gặp đúng constraint duplicate phải rollback **toàn bộ** transaction/DbContext, mở transaction mới, lấy lại guard và đọc operation đã commit để xử lý retry; không chỉ rollback savepoint rồi giữ server thứ hai. Không diễn giải constraint khác thành retry.
5. Kiểm tra thời gian/điều kiện guard, commit, trả DTO/Location. Deadlock/timeout trả lỗi tạm; không tự replay mutation bằng execution strategy. Client retry chủ động giữ nguyên khóa và payload chuẩn hóa.

Read dùng guard và một SQL projection cho toàn bộ metadata/membership/permissions của trang/detail, để quyết định quyền và dữ liệu Community cùng snapshot của statement. Nếu reader phải dùng nhiều statement, phải thay bằng transaction snapshot phù hợp và kiểm chứng trước merge; không ghép kết quả từ những thời điểm quyền khác nhau. Mỗi trang kiểm tra lại quyền, không lấy quyền từ cursor. `READ COMMITTED` tạo snapshot cho từng statement; cách dùng một projection là lựa chọn của gói để giữ dữ liệu nhất quán. [PostgreSQL isolation](https://www.postgresql.org/docs/18/transaction-iso.html#XACT-READ-COMMITTED).

Outbox ghi event nội bộ `Community.ServerCreated.v1`, aggregate server/version, payload tối thiểu server ID/owner ID/accessVersion; chỉ có một event cho một thao tác tạo. Hạ tầng outbox dùng schema integration chung. Gói chỉ ghi bền, chưa có dispatcher/Hub; UI cập nhật từ HTTP rồi đọc lại. Event chờ không được đánh dấu published khi chưa phát; gói realtime bổ sung delivery/routing riêng.

<a id="keys"></a>

## Operation, cursor và khóa

Operation dùng định dạng binary/HMAC-SHA256 và [fixture Community](../../../fixtures/community-operations.json) đã có ở [thiết kế operation](integration.md#operations). Scope của create server là UUID zero, duy nhất theo `(actor_user_id,kind,scope_id,client_operation_id)`. So hash constant-time. Record lưu `fingerprint_version=1`, `key_id`, hash 32 byte, resource ID và UTC; không lưu JWT hoặc toàn request. Resource ID của bảng operation chung được writer kiểm chứng cùng commit; không dùng FK đa hình giả sang mọi loại tài nguyên.

Khóa HMAC riêng cho Community, ngẫu nhiên tối thiểu 32 byte, cấu hình active key ID và tập key ID → key qua secret configuration. Startup kiểm tra active key; không có khóa Development mặc định trong source. Retry dùng key cũ của record, rotation chỉ áp thao tác mới; giữ key cho record còn tồn tại và backup liên quan theo [quy tắc khóa DM](../../direct-messaging.md#detailed-design). Gói không tự đặt TTL/xóa operation khi metadata retention còn mở.

List của mình sort `server.id DESC`, keyset `id < lastServerId`, lấy limit + 1 và chỉ active server/member. Cursor bảo vệ bằng Data Protection, purpose `Community.MyServers.v1`, gồm actor ID, limit, lastServerId, expiry 24 giờ và version format. Next cursor chỉ sinh khi còn trang. Không dùng offset hoặc thời điểm tên thay đổi làm vị trí sort; concurrent join/leave vẫn có thể đổi tập kết quả, UI dedup ID/refresh từ đầu.

Data Protection có ApplicationName cố định theo môi trường và key-ring path/volume bền qua restart/deploy, tách khỏi HMAC key và JWT signing key. Cần cấu hình explicit vì key mặc định có thể chỉ nằm trong bộ nhớ khi không có nơi lưu phù hợp. [ASP.NET Core key management](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0). Hết hạn/tamper trả 400; key-ring không truy cập được trả 503, không giả cursor hợp lệ hoặc chuyển sang token không bảo vệ.

<a id="migration"></a>

## Migration và dữ liệu legacy

Chọn migration SQL có phiên bản tại `database/postgres/migrations/001-community-create-view.sql` khi triển khai bước 4. Runner explicit ghi module/version/checksum/appliedAt vào ledger `common.schema_migrations`, khóa ledger khi apply, transaction cho migration và dừng khi checksum khác. Không gọi `EnsureCreated` hoặc chạy bootstrap từ API startup. Runner/SQL/ledger chưa tồn tại trong source và là phần việc bước 4.

| Bảng/phần | Thay đổi được chọn cho migration gói |
|---|---|
| servers | Thêm visibility public/private, join_mode immediate/approval, access_version ≥1; description rộng đến 1.000. Giữ ID/status/slug/version/trigger hiện có; server mới ghi active, public/default request và immediate |
| name/description | Bổ sung `common.utf16_length(text)` immutable cho DB UTF-8, đếm Unicode scalar ngoài BMP thành 2 đơn vị. Thay check tên server bằng 2–100 UTF-16 sau normalize; description ≤1.000 UTF-16. Service vẫn kiểm tra blank/control/Unicode theo policy |
| server_members | Thêm membership_id UUIDv7 duy nhất và version ≥1; giữ unique `(server_id,user_id)`. Backfill epoch cho record hiện có được duyệt; không đổi kicked/banned thành active/left tự động |
| owner invariant | FK deferred từ `(servers.id,owner_user_id)` tới `(server_members.server_id,user_id)` và constraint trigger deferred kiểm tra owner membership active tại commit. Create được ghi server trước member, kiểm tra cuối transaction |
| @everyone invariant | Giữ unique default-role index; constraint trigger deferred đảm bảo mỗi server có đúng một default/system @everyone, owner member tồn tại; check role hệ thống chỉ là default @everyone và không có management grant. Trigger theo dõi server/member/role/role_permissions để không có đường sửa bỏ invariant |
| operations | Thêm bảng Community operation với composite PK, fingerprint/key/version/resource/createdAt và checks cần thiết; writer gói chỉ nhận create_server/scope zero |
| indexes | Dùng active membership index `(user_id,server_id)` và server PK cho list keyset; bổ sung index membership_id. Không thay key/index search/channel/role ngoài phần gói cần |
| outbox | Dùng bảng integration.outbox_events hiện có qua hạ tầng chung; aggregate_version vẫn tương thích integer của server; chưa thêm lease/dispatcher/realtime |

`char_length` PostgreSQL đếm ký tự, không phải UTF-16. Hàm UTF-16 được chọn có giá trị `char_length + số scalar > U+FFFF`; DB UTF-8 không nhận surrogate không hợp lệ, service vẫn phải chặn trước. Ví dụ tên chỉ một emoji ngoài BMP có 2 UTF-16 và phải vượt qua cận tối thiểu, điều mà check hiện tại không cho. Đây là thiết kế cần đối chiếu bằng fixture .NET/SQL. [PostgreSQL string functions](https://www.postgresql.org/docs/18/functions-string.html).

Preflight phải hoàn thành trước thay constraint/backfill:

1. Kiểm tra owner có membership active, tên/mô tả hợp lệ theo policy, membership epoch/status, default/system role/grant và collision khi chuyển tên default role thành @everyone. Báo ID và lỗi cụ thể; dừng trên dữ liệu không có mapping được duyệt.
2. Cột visibility/joinMode cũ chưa tồn tại nên không suy public cho mọi server legacy. Backfill qua mapping theo server ID được duyệt; DB không có server thì không cần mapping. Server mới lấy default từ request, không lấy lựa chọn backfill của dữ liệu cũ.
3. Không tự sửa tên, xóa role/grant, đổi trạng thái thành viên hoặc mở visibility. Seed demo legacy cũng phải có mapping hoặc thay bằng seed mới ở bootstrap Development. Không chạy schema.sql có DROP SCHEMA trên DB cần giữ dữ liệu.
4. Apply/validate trong transaction, lưu ledger; chạy lại cùng checksum là no-op. Deployment phải có bản backup/preflight và kế hoạch quay lại trước migration. Sau khi có write mới, giữ schema mở rộng khi rollback app; không tự DROP cột/bảng hoặc hạ description xuống 500 làm mất dữ liệu.

Bootstrap `schema.sql`/`seed.sql` cho DB mới được đồng bộ với schema cuối của migration ở bước 4, ledger ghi cùng baseline/checksum để không apply lại. Test migration vẫn bắt đầu từ snapshot schema cũ/fixture legacy riêng để chứng minh đường upgrade; cập nhật bootstrap không thay bằng chứng migration.

<a id="verification"></a>

## Kiểm chứng và gói code

| Nhóm | Bằng chứng bắt buộc ở bước 4/5 |
|---|---|
| Contract/validation | Bốn route đúng schema/status/Location, unknown field/actor/owner bị chặn; tên/mô tả biên UTF-16, emoji/combining/ZWJ, multiline/control và visibility/default theo policy |
| Mapping/migration | SQL và .NET đếm UTF-16 khớp; preflight legacy không sửa dữ liệu khi fail; deferred owner/@everyone reject dữ liệu dở; migration/ledger chạy lại đúng, bootstrap mới tương đương |
| Identity | Unverified/inactive/session revoked/stamp mismatch bị chặn; create tranh logout/password reset/disable/expiry có kết quả phù hợp dưới guard. Giữ các kiểm thử Identity hiện có |
| Create/retry | Fault injection giữa các insert rollback toàn bộ; hai connection cùng operation tạo đúng một server và event; payload khác 409; lost response/restart/rotation/thiếu key không tạo trùng |
| Read/privacy | Owner detail và five-code permissions; thành viên hợp lệ đọc projection đúng; public nonmember không lộ detail/status người khác; private 404; own left status không tạo membership; list rỗng/keyset đúng |
| Cursor | Cross-actor/limit/purpose/tamper/expiry bị chặn; key ring bền qua restart; dữ liệu trang không chứa server/member inactive ở snapshot query; dedup/refresh khi tập kết quả đổi |
| Outbox/lưu bền | Chỉ một event cùng commit, rollback không có event; reload/restart API vẫn đọc được kết quả, không báo đã phát realtime khi chưa có dispatcher |

Dùng PostgreSQL riêng cho integration/race tests, hai connection và barrier xác định thứ tự; không dùng EF InMemory để chứng minh lock/constraint. Fixture có cả DB trắng sau bootstrap mới và snapshot legacy trước migration. Build/test backend ở bước 4, thao tác UI/API và hồ sơ [nghiệm thu](../../../release-operations.md#testing) ở bước 5; bảng trên là kế hoạch kiểm chứng; kết quả từng phần ở [hồ sơ nghiệm thu](../delivery/create-view/acceptance.md).

Gói code dùng `feat/community-create-view` từ main đã có tài liệu/phụ thuộc. Thứ tự commit kiểm chứng được: work scope + Identity guard; persistence/migration và preflight; create/read API; các kiểm chứng còn lại. Mỗi phần có kiểm tra phù hợp, commit và push theo tiến độ; không merge nhánh code nếu chưa duyệt. Tài liệu độc lập tiếp tục trên main theo [quy ước Git](../../../development.md#conventions).

Đầu vào bước 4 đã xác định trong tài liệu này. Các ngưỡng timeout/cursor là cấu hình thiết kế cần kiểm chứng; migration dữ liệu thật chỉ chạy khi có mapping/preflight hợp lệ. Hoàn thành thiết kế không đóng các OQ/ACL-O, không xác nhận API hoặc quyền/realtime đã hoạt động.

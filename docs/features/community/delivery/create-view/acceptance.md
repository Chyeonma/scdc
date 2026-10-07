# SCDC — Nghiệm thu gói tạo và xem cộng đồng

Ngày kiểm chứng: 2026-10-07. Hồ sơ bằng chứng cho phạm vi [gói tạo/xem](plan.md#first-package); không ghi trạng thái hiện tại của toàn module. Trạng thái theo nhánh và bước tiếp theo tại [status.md](../../status.md).

| Phần được thử | Commit implementation | Phạm vi |
|---|---|---|
| Backend | `79fa627` (gồm nền `f9abd87`, migration `05e61c6`) | UC-COM-01 và phần đọc đã chọn của UC-COM-03 |
| WebClient | `4203e05` (gồm nền API/draft `2b9d51e`) | Create/list/detail và xử lý lỗi/actor của gói |

Các commit trên nằm trong nhánh `feat/community-create-view`. Kết quả bên dưới gắn revision này; thay đổi code sau đó cần kiểm chứng phù hợp. Hồ sơ ghi bằng chứng kỹ thuật, không thay quyết định phát hành.

## Phạm vi bằng chứng

Gói đầu có bằng chứng cho UC-COM-01, phần danh sách/detail/tư cách được chọn của UC-COM-03 và projection summary public của route dùng chung. Không dùng hồ sơ này để đánh dấu toàn bộ UC-COM-02/03 hoặc search, join/leave/rejoin, phòng, role/ACL, invitations và realtime đạt. Phạm vi và tiêu chí từng dòng ở [kế hoạch](plan.md#first-package).

<a id="backend"></a>

## Bằng chứng backend

| Route | Hành vi đã kiểm chứng |
|---|---|
| `POST /api/v1/servers` | Tạo server public/private với tên và mô tả theo policy; owner membership active và @everyone; 201/Location sau commit, retry hợp lệ 200, đổi payload cùng khóa 409 |
| `GET /api/v1/servers` | Chỉ server/member active của actor; keyset ID giảm dần, limit 1–50, cursor bảo vệ gắn actor/limit/purpose và hết hạn 24 giờ |
| `GET /api/v1/servers/{id}` | Member detail với quyền hiện hành; người ngoài public chỉ nhận sáu trường summary; private/inactive/unknown trả 404 |
| `GET /api/v1/servers/{id}/membership/me` | Membership active/left của chính actor, kể cả own-left trong private; GET không tạo hoặc phục hồi membership |

Gói thực hiện UC-COM-01 ở backend và phần list/detail/tư cách của UC-COM-03. Route summary public giữ quy tắc che dữ liệu; chưa có search/discovery, join/leave API, phòng, quản lý role/ACL, lời mời, Messaging hoặc realtime.

Các lựa chọn trong [thiết kế bước 3](../../design/create-view.md) đã được thực hiện: UUIDv7/UUIDv4, version string, giới hạn UTF-16, HMAC theo fixture, Data Protection key ring explicit, one-statement read projection, shared PostgreSQL transaction và Identity share guard. Metadata version vẫn do trigger tăng.

Create ghi server, owner membership, @everyone, operation và đúng một event `Community.ServerCreated.v1` trong cùng transaction. Event giữ `published_at=NULL`; chưa có dispatcher. Unique operation loser rollback toàn bộ context/transaction, mở transaction mới rồi đọc winner. Retry đọc detail hiện hành dưới quyền hiện hành; không tạo lại tài nguyên đã mất quyền hoặc thiếu key.

Migration có runner explicit, preflight và ledger/checksum. DB cũ cần mapping visibility/joinMode được rà soát theo server ID; role/grant/status legacy không được tự sửa. Deferred constraints giữ owner active và @everyone không có management grant. Bootstrap/seed DB mới được đồng bộ; API không tự migrate hoặc gọi bootstrap.

### Kiểm thử backend

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


### Cấu hình thử nghiệm backend

Chuyển sang nhánh backend để đọc hướng dẫn runtime trong `services/Modules/Community/README.md`. HMAC cần key ID và base64 secret ít nhất 32 byte; key ring cần thư mục bền đọc/ghi được. Compose dùng `COMMUNITY_OPERATION_KEY` từ `.env` local và volume `scdc-community-keys`. Giữ HMAC key cũ khi rotation để đọc operation còn lưu; giữ key ring và ApplicationName theo môi trường qua restart.

<a id="frontend"></a>

## Bằng chứng WebClient

### Hành vi bàn giao

WebClient dùng bốn API của [backend bước 4](). Sau đăng nhập, người dùng thấy danh sách cộng đồng có membership active của mình. Form tạo nhận tên, mô tả và public/private; tạo thành công mở chi tiết, tải lại danh sách từ API và giữ đường dẫn `#community/{id}` qua reload. Không tạo phòng mặc định. Chi tiết hiển thị metadata, tư cách của chính người dùng và quyền quản lý hiệu lực do API trả về; quyền hiện tại chỉ được hiển thị, chưa có giao diện quản lý role/ACL.

Có trạng thái đang tải, rỗng, lỗi và nút tải lại. Danh sách dùng cursor của API với limit 20; lỗi cursor yêu cầu người dùng tải lại từ đầu. Server ID, owner, membership và permissions không được suy ra từ dữ liệu mẫu. Người ngoài public chỉ thấy summary; private không đủ quyền không hiển thị metadata. Refresh detail xóa dữ liệu cũ trước khi kiểm tra lại quyền. Chuyển tài khoản hoặc đăng xuất unmount state theo actor, hủy request và bỏ response đến muộn.

Tên và mô tả dùng cùng quy tắc với [thiết kế](../../design/create-view.md): White_Space cố định Unicode 17, giới hạn UTF-16, từ chối Unicode lỗi/NUL, tên một dòng, CRLF/CR → LF cho mô tả. Không trim mô tả hoặc đổi NFC. Kiểm thử frontend đối chiếu fixture `docs/fixtures/community-operations.json` và `docs/fixtures/text-policy.json`; backend vẫn là nơi quyết định validation và quyền.

Yêu cầu tạo gồm UUIDv4 và payload chuẩn hóa được ghi vào `sessionStorage` theo actor **trước POST**. Sau mất mạng, timeout 15 giây, 401, 409 hoặc lỗi máy chủ, khóa và nội dung giữ nguyên, form khóa sửa. Reload hiển thị “Tiếp tục yêu cầu”; chỉ nút “Kiểm tra và thử lại” gửi lại cùng operation. Không tự POST lại sau refresh token, reload hoặc kết nối lại. Submit đang chạy chặn nhấn hai lần. Đóng form hủy request phía trình duyệt và giữ draft; không coi thao tác đóng là hủy giao dịch máy chủ. Storage bị chặn không gửi POST; bản lưu hỏng không bị âm thầm thay thế. Lỗi validation 400 giải phóng operation và giữ nội dung form cho người dùng sửa. Chỉ xóa bản lưu khớp operation sau khi xác nhận thành công.

SessionStorage giữ được khi reload trong tab hiện tại; đóng tab hoặc xóa dữ liệu trình duyệt có thể mất yêu cầu chưa xác nhận. Khi đó cần kiểm tra danh sách trước khi tạo mới. Draft của tài khoản A không được tái sử dụng cho B. Khi không có Web Locks, cơ chế Identity hiện có giữ session trong bộ nhớ từng tab; reload yêu cầu đăng nhập lại, rồi cùng actor có thể tiếp tục draft của tab.

Tin nhắn trực tiếp hiện có được giữ ở đích riêng với nhãn giao diện mẫu. Luồng Community không dùng server/phòng mẫu, không thử kết nối Hub chưa triển khai, không hiện trạng thái realtime thành công giả. Trang đăng nhập giới thiệu các chức năng đang có.

### Kiểm thử WebClient

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

<a id="reproduce"></a>

## Chạy lại

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

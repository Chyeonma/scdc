<a id="scdc--hướng-dẫn-phát-triển"></a>

# SCDC — Hướng dẫn phát triển

Cách cài đặt, chạy, cấu hình và kiểm thử SCDC trên máy phát triển.

**Cập nhật:** nền `900e226` ([MVP](../releases/mvp.md)); hướng dẫn gateway S1-02 cập nhật ngày 2026-11-12. Riêng Community (database thử, kiểm thử, migration) xem [mục 8](#community).

Các lệnh chạy từ thư mục gốc repo, trừ khi ghi khác. Cấu hình và dữ liệu mẫu chỉ dùng cho môi trường phát triển.

<a id="mục-lục"></a>

## Mục lục

- [Chuẩn bị và khởi chạy](#setup)
- [Docker Desktop, Docker Engine và Podman](#container-engines)
- [Lệnh ngắn](#short-commands)
- [Cấu hình](#configuration)
- [Tài khoản để tích hợp](#identity)
- [Database và dữ liệu mẫu](#database)
- [Quy ước code và Git](#conventions)
- [Kiểm thử](#testing)
- [Xử lý lỗi thường gặp](#troubleshooting)
- [Community: database thử, kiểm thử và migration](#community)

<a id="setup"></a>

<a id="1-chuẩn-bị-và-khởi-chạy"></a>

## 1. Chuẩn bị và khởi chạy

**Cần có:** Docker hoặc Podman, kèm Compose. Nếu debug trên máy: .NET SDK 10 và Node.js 24 với npm (cùng major với Dockerfile frontend).

Backend .NET 10 (`SCDC.slnx`), frontend React 19 và Vite.

<a id="chạy-bằng-compose"></a>

### Chạy bằng Compose

Trước lần chạy đầu: đặt `COMMUNITY_OPERATION_KEY` cố định trong `.env` ([README](../../README.md#khởi-chạy)), rồi tạo khóa fingerprint DM:

```bash
make init-dm-key
docker compose up -d --build
```

| Dịch vụ | Địa chỉ |
|---|---|
| WebClient | `http://localhost:3000` |
| Swagger qua gateway (Development) | `http://localhost:3000/swagger` |
| OpenAPI JSON qua gateway (Development) | `http://localhost:3000/swagger/v1/swagger.json` |
| Health của Nginx | `http://localhost:3000/healthz` |
| Health của service mẫu S1-03 qua gateway | `http://localhost:3000/health` |
| Health của API qua gateway | `http://localhost:3000/api/v1/health` |
| API trực tiếp để debug | `http://localhost:5026` |
| PostgreSQL | `localhost:5432` |

Compose dựng PostgreSQL, API và web.

S1-03 thêm container `sample-service`, build từ bốn project trong `services/Sample/`. `GET /health` qua gateway trả JSON có `service: "sample-service"`, `status` và `checkedAt` UTC. Service mẫu chạy độc lập với monolith và database; cổng 8080 chỉ dùng trong mạng Compose. Chi tiết cấu trúc và contract tại [service mẫu S1-03](../system/backend.md#s1-03-sample-service).

- **Đã có:** tài khoản; Community (tạo, xem, tìm, tham gia server công khai, vai trò, phòng chữ, ACL); DM (hộp thư, gửi, lịch sử, gửi lại, sửa, xóa, ChatHub, outbox, kết nối lại). Xem [hồ sơ tích hợp](../records/verification/messaging/merge-2026-10-13.md).
- **Chưa có:** tin phòng, realtime Community. Worker email ở nhánh Identity riêng ([hiện trạng email](../features/accounts/email-links.md#status)).
- Giữ key ring Community và volume khóa DM cùng với dữ liệu.
- Database mới đã có migration Community 001 đến 004. Database đã có dữ liệu phải [nâng cấp](#migration) và đối chiếu migration DM ([hồ sơ](../records/verification/messaging/DM-MVP.md)).

<a id="gateway"></a>

### Nginx trước API hiện tại — S1-02

Container `web-client` vừa phục vụ file build React vừa làm gateway. Trình duyệt dùng cùng địa chỉ cổng 3000 cho giao diện, REST `/api/v1` và SignalR `/hubs/chat`. Cổng 5026 phục vụ debug API trực tiếp; các kiểm chứng gateway phải đi qua WebClient.

[Cấu hình Nginx](../../clients/WebClient/nginx.conf) được chép thành template trong image. Khi container bắt đầu chạy, entrypoint của image Nginx điền DNS lấy từ `/etc/resolv.conf`. Gateway phân giải lại `chat-service` với cache 5 giây để khôi phục sau khi API được tạo lại và đổi IP. Chạy `nginx -t` **trong container đã khởi động**, sau bước điền template:

```bash
podman compose exec web-client nginx -t
curl --fail http://localhost:3000/healthz
curl --fail http://localhost:3000/health
curl --fail http://localhost:3000/api/v1/health
```

Dùng `docker compose` nếu chọn Docker. `/healthz` chỉ kiểm tra Nginx; `/health` kiểm tra tiến trình service mẫu; `/api/v1/health` hiện chưa kiểm tra database của monolith. Khi upstream chưa sẵn sàng, các đường dẫn tương ứng có thể trả lỗi gateway; file giao diện vẫn được phục vụ.

Kiểm thử S1-02 cần Node.js 24, npm và trình duyệt Playwright. Cài Chromium bằng `npx playwright install chromium` trong `clients/WebClient`, hoặc đặt `SCDC_E2E_BROWSER_CHANNEL=chrome` nếu đã có Google Chrome.

```bash
make test-gateway ENGINE=podman
# Hoặc Docker:
make test-gateway
```

Lệnh cài dependency frontend, build [stack thử riêng](../../compose.gateway-test.yaml), khởi tạo khóa DM và chạy project E2E `gateway`. Mỗi lần chạy có project `scdc-gateway-<mã-lần-chạy>`, database `scdc_gateway_test` và volume riêng; chỉ Nginx được publish ở `127.0.0.1:15310`. Bộ kiểm thử thử lại API sau restart, chủ động đổi IP container API và service mẫu, giữ nguyên container Nginx; kiểm tra service mẫu vẫn trả health khi monolith dừng. Kết quả nằm trong `artifacts/gateway/`; stack và volume thử được dọn khi kết thúc.

Đổi cổng hoặc giữ stack để điều tra lỗi sau khi đã cài dependency:

```bash
python3 scripts/test_gateway.py --engine podman --port 15311 --keep
```

Script in tên project trong kết quả. Dọn đúng stack đã giữ bằng `podman compose -p <tên-project> -f compose.gateway-test.yaml down --volumes`; dùng cùng `GATEWAY_WEB_PORT` nếu đã đổi cổng. Chi tiết phạm vi xem [S1-02](../system/api-conventions.md#s1-02-preparation).

<a id="container-engines"></a>

<a id="docker-desktop-docker-engine-và-podman"></a>

### Docker Desktop, Docker Engine và Podman

Cả ba dùng chung [compose.yaml](../../compose.yaml) và Dockerfile. Makefile mặc định dùng Docker; thêm `ENGINE=podman` để dùng Podman. Máy cài cả hai thì chỉ rõ engine.

| Môi trường | Chuẩn bị | Lệnh khởi chạy qua Make |
|---|---|---|
| Docker Desktop trên Windows | Mở Docker Desktop, dùng Linux containers; để chạy Make trong WSL, bật WSL integration cho distro và cài Make/Python/Git trong distro | `make up` |
| Docker Engine với CLI | Engine đang chạy, CLI kết nối được và đã cài Compose plugin | `make up` |
| Podman | Podman hoạt động và có Compose provider; Windows/macOS cần Podman machine đang chạy | `make up ENGINE=podman` |

- [Docker Desktop](https://docs.docker.com/compose/install/) đã gồm Engine, CLI và Compose.
- [`podman compose`](https://docs.podman.io/en/latest/markdown/podman-compose.1.html) gọi một Compose provider bên ngoài (`podman-compose` hoặc `docker-compose`); chọn bằng `PODMAN_COMPOSE_PROVIDER` nếu cần.
- [Podman machine](https://docs.podman.io/en/latest/markdown/podman-machine.1.html): chưa có thì `podman machine init`, rồi `podman machine start`.

Kiểm tra cấu hình trước khi khởi chạy và dùng cùng engine cho mọi thao tác:

```bash
make compose-check ENGINE=podman
make up ENGINE=podman
make status ENGINE=podman
make logs ENGINE=podman
make down ENGINE=podman
```

- `export ENGINE=podman` trong shell để mọi lệnh `make` dùng Podman.
- Gọi thẳng provider: `make up COMPOSE="podman-compose"` (dùng cùng giá trị cho các lệnh khác).
- `compose-check` chỉ kiểm tra cấu hình; vẫn phải chạy stack để biết build, healthcheck và thứ tự khởi động có ổn không.
- Docker và Podman có volume riêng; đổi engine không mang theo dữ liệu PostgreSQL.

Tên image ghi đầy đủ registry (`docker.io/library/...`, `mcr.microsoft.com/...`) để Podman không phải đoán ([quy tắc tên image](https://docs.podman.io/en/latest/markdown/podman-pull.1.html)).

**Windows dùng PowerShell:** Makefile cần shell POSIX, nên muốn dùng `make` thì chạy trong WSL. Không dùng WSL thì gọi thẳng CLI và script từ thư mục gốc (cần Python và Git trên Windows):

```powershell
docker compose up -d --build
docker compose ps
docker compose down
# Thay bằng podman compose nếu dùng Podman:
podman compose up -d --build
py -3 scripts/check_docs.py
py -3 scripts/sync_docs.py --source main --target feat/identity --dry-run
py -3 scripts/sync_docs.py --source main --target feat/identity
```

Nếu Python của máy cung cấp lệnh `python` thay vì `py`, thay `py -3` bằng `python`. Đồng bộ docs chỉ cần Python/Git; không phụ thuộc Docker hay Podman.

<a id="debug-backend-local"></a>

### Debug backend local

API chạy trên máy cần HMAC và key ring Community; gửi tin DM cần thêm thư mục khóa fingerprint ([cấu hình](#configuration)). Khi kiểm thử, dùng [database thử riêng](#local-environment).

```bash
docker compose up -d postgres
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --no-restore
dotnet run --project services/SCDC.Api/SCDC.Api.csproj --launch-profile http
```

Nếu API đã chạy từ Compose, dừng riêng container API trước khi dùng cổng 5026:

```bash
docker compose stop chat-service
```

<a id="frontend-local--terminal-khác"></a>

### Frontend local — terminal khác

```bash
cd clients/WebClient
npm ci
npm run dev
```

Vite chạy ở cổng 3000 và chuyển tiếp `/api`, `/hubs`, `/swagger` tới API ở 5026. Nếu container web đang chiếm cổng 3000 thì dừng nó trước.

<a id="short-commands"></a>

<a id="lệnh-ngắn-và-đồng-bộ-docs-giữa-các-nhánh"></a>

### Lệnh ngắn

[Makefile](../../Makefile) gom các lệnh hay dùng. Gõ `make` hoặc `make help` để xem danh sách. Công cụ đồng bộ tài liệu cần `make`, Python 3 và Git đủ mới (hỗ trợ `switch`, `restore`, `rev-parse --end-of-options`).

| Lệnh | Tác dụng |
|---|---|
| `make up` / `make down` | Build/chạy hoặc dừng stack Compose; `down` giữ volume |
| `make status` / `make logs` | Xem container hoặc log API/PostgreSQL |
| `make db` | Chạy riêng PostgreSQL |
| `make compose-check` | Kiểm tra cấu hình Compose qua engine đã chọn |
| `make api` / `make web` | Chạy backend/frontend local; frontend cài dependency bằng `npm ci` |
| `make build` | Build backend và frontend |
| `make test` | Test backend và frontend; backend cần PostgreSQL Development |
| `make test-api` / `make test-web` | Test riêng backend/frontend |
| `make test-gateway` | Test gateway, các transport SignalR và DM trên stack riêng; hỗ trợ `ENGINE=podman` |
| `make docs-check` | Kiểm tra link và anchor tài liệu |
| `make test-tools` | Kiểm thử công cụ đồng bộ trong các repo Git tạm |

<a id="docs-sync"></a>

**Đồng bộ docs giữa các nhánh:** với [quy ước nhánh ngắn](../project/workflow.md#branches), chỉ cần `git pull --rebase origin main` trước khi mở PR. Công cụ `make docs-sync` chỉ còn dùng cho nhánh cũ sống lâu (ví dụ `feat/identity`): nó **chép đè toàn bộ** `docs/` từ nhánh nguồn sang nhánh đích, không trộn nội dung.

```bash
make docs-sync-preview FROM=main TO=feat/identity   # xem file sẽ thêm, sửa, xóa
make docs-sync FROM=main TO=feat/identity           # chép và đưa vào staging; tự xem diff rồi commit
```

Cần working tree sạch; công cụ không tự stash, commit hay push. Chi tiết: `python3 scripts/sync_docs.py --help`.

<a id="configuration"></a>

<a id="2-cấu-hình"></a>

## 2. Cấu hình

Backend đọc `appsettings.json`; launch profile `http` bật Development và nạp `appsettings.Development.json`. Compose truyền cấu hình qua biến môi trường.

| Khóa | Giá trị local / vai trò |
|---|---|
| `ConnectionStrings:Database` | `Host=localhost;Port=5432;Database=scdc_chat;Username=scdc;Password=scdc_dev` khi API chạy local; host `postgres` trong Compose |
| `Modules:Identity:Issuer` / `Audience` | `SCDC` / `SCDC.WebClient` |
| `Modules:Identity:SigningKey` | Khóa local có trong cấu hình Development; cấu hình ngoài Development cần khóa riêng đủ dài |
| `Modules:Identity:ExposeDevelopmentTokens` | `true` trong Development, dùng token trả về để thử xác minh/reset; giá trị mặc định `false` |
| `Modules:Community:Operations:ActiveKeyId` / `Keys:<id>` | ID active và key base64 ít nhất 32 byte ngẫu nhiên; giữ key cũ cho operation còn lưu, xem [mục 8](#community-configuration) |
| `Modules:Community:KeyRingPath` | Thư mục keyring bền, API đọc/ghi được; giữ qua restart để cursor vẫn đọc được |
| `Modules:Identity:UserSearch:CursorKeyRingPath` / `ApplicationName` | Keyring bảo vệ cursor tìm người/inbox/lịch sử DM; provider riêng với Community, giữ qua restart. Compose lưu trong `identity-user-search-keys` |
| `Modules:Messaging:FingerprintKeyDirectory` / `ActiveFingerprintKeyId` | Thư mục chứa `<keyId>.key` dạng byte, tối thiểu 32 byte ngẫu nhiên; thiếu khóa chặn gửi/retry. Compose dùng `make init-dm-key` và volume `scdc-messaging-keys` |
| `Cors:AllowedOrigins` | Local hiện có `http://localhost:3000`, `http://localhost:5173`; Vite của repo mặc định 3000 |

Thời hạn token, phiên và khóa đăng nhập xem [Tài khoản](../features/accounts/README.md). Khi đặt bằng biến môi trường, thay `:` bằng `__`, ví dụ `ConnectionStrings__Database`.

Nguồn: [appsettings.json](../../services/SCDC.Api/appsettings.json), [cấu hình Development](../../services/SCDC.Api/appsettings.Development.json), [Vite config](../../clients/WebClient/vite.config.js).

<a id="identity"></a>

<a id="3-tạo-tài-khoản-để-tích-hợp"></a>

## 3. Tạo tài khoản để tích hợp

1. Đăng ký qua form hoặc `POST /api/v1/auth/register` với `username`, `displayName`, `email`, `password`.
2. Trong Development, lấy `developmentVerificationToken` từ response đăng ký và gửi `POST /api/v1/auth/verify-email` với `{ "token": "..." }`.
3. Đăng nhập bằng email hoặc username. Dùng `accessToken` làm Bearer token khi gọi endpoint yêu cầu xác thực.
4. Quên mật khẩu dùng `POST /api/v1/auth/forgot-password`; response Development có thể chứa `developmentResetToken`. Dùng token đó cùng `newPassword` ở endpoint reset.

Token Development chỉ để thử trên máy. Nền chưa có gửi lại email hay worker email; phần này ở nhánh `feat/identity@60f4db3` ([hiện trạng](../features/accounts/email-links.md#status)).

Frontend quản lý phiên trong [api.js](../../clients/WebClient/src/api.js): có Web Locks thì đồng bộ phiên giữa các tab và chỉ một tab làm mới token; không có thì mỗi tab giữ token riêng. Hàm gọi API mặc định thử lại sau 401; các thao tác mở DM, gửi, sửa, xóa dùng `retry: false`. Gửi lại tin do người dùng bấm (DEC-021); kết nối lại thì tự bù dữ liệu qua REST.

<a id="database"></a>

<a id="4-database-và-dữ-liệu-mẫu"></a>

## 4. Database và dữ liệu mẫu

Database trên máy: `scdc_chat`, user `scdc`, mật khẩu `scdc_dev`, cổng 5432, tắt SSL. Các schema xem [database](../system/database.md).

- `schema.sql` tạo schema, ràng buộc, index, trigger, view.
- `seed.sql` có dữ liệu mẫu; mật khẩu và token trong đó chỉ minh họa, không đăng nhập được. Tạo tài khoản mới qua API.
- Compose gắn hai file vào `/docker-entrypoint-initdb.d/`; chúng **chỉ chạy khi volume mới**. Sửa SQL không cập nhật volume đã có dữ liệu. Không dùng script này như migration.

Các view để quan sát: `identity.v_user_accounts`, `identity.v_active_sessions`, `messaging.v_space_overview`, `messaging.v_message_timeline`. Ví dụ:

```sql
SELECT * FROM identity.v_user_accounts ORDER BY username;
SELECT * FROM identity.v_active_sessions;
SELECT * FROM messaging.v_space_overview ORDER BY last_activity_at DESC;
SELECT * FROM integration.outbox_events ORDER BY occurred_at;
```

Schema và seed có cả bảng cho tính năng chưa làm. Dữ liệu thử cho từng tính năng ghi trong trang tính năng; chạy test tích hợp trên database riêng.

<a id="conventions"></a>

<a id="5-quy-ước-code-và-git"></a>

## 5. Quy ước code và Git

- Nghiệp vụ đặt tại `services/Modules/<Module>`; hợp đồng liên module nằm trong `SCDC.Contracts`.
- Mỗi module sở hữu dữ liệu riêng. Không tham chiếu implementation hoặc truy vấn trực tiếp dữ liệu module khác.
- Lỗi nghiệp vụ trả `Result` / `Result<T>` tại `SCDC.BuildingBlocks`; dùng `Success` hoặc `Failure`, không ném exception cho lỗi dự kiến.
- Controllers ánh xạ lỗi theo [hợp đồng lỗi chung](../system/api-conventions.md#contracts). JSON lỗi phải khớp schema và hành vi frontend.
- Tên nhánh: `feat/<module>-<gói>`, `fix/<tên-lỗi>`; nhánh ổn định là `main`. Mỗi nhánh một gói có đầu ra kiểm tra được (code, migration, test, tài liệu). Tài liệu độc lập có thể commit thẳng lên `main`.
- Commit: `<loại>(<phạm vi>): <mô tả>`, dùng `feat`, `fix`, `refactor`, `perf`, `test`, `docs`.
- Xong một phần review được thì kiểm tra, commit, push. Merge vào `main` sau khi review; được push không có nghĩa được merge.

Khi review: đối chiếu yêu cầu và AC bị ảnh hưởng, quyền, API, dữ liệu, ngoại lệ, test, tài liệu. Thay đổi phạm vi hay quyết định thì cập nhật [hồ sơ quyết định](../records/decisions/README.md) và các trang liên quan. Tài liệu viết theo [quy ước](writing.md).

<a id="testing"></a>

<a id="6-kiểm-thử"></a>

## 6. Kiểm thử

<a id="backend"></a>

### Backend

```bash
dotnet test SCDC.slnx --configuration Release
```

Bộ test gồm Identity, Community, DM, response API, quyền, chống trùng, migration, transaction, realtime.

- Dùng PostgreSQL riêng, tên database kết thúc `_test`, user có quyền tạo và xóa database fixture.
- Đặt connection string, signing key và token Development theo [cấu hình](#configuration); DM xem [hồ sơ DM](../records/verification/messaging/DM-MVP.md).
- **Không** chạy trên `scdc_chat` hay database cần giữ.

<a id="frontend--trong-clientswebclient"></a>

### Frontend — trong clients/WebClient

```bash
npm test
npm run build
```

Test Node: hàm gọi API, quản lý phiên, Community, DM. Playwright chạy với API và DB thật; bộ Community và `mergeMvp.spec.js` cần biến `_test` ([hướng dẫn Community](#frontend-tests)). Mọi bộ E2E dùng chung `playwright.config.js`, chọn bằng `npm run test:e2e -- <project>`: `community` và các bộ DM `dm-mvp`, `dm-baseline`, `dm-search`, `dm-open`, `dm-inbox`, `dm-send`, `dm-history`, `dm-retry`, `dm-sequence`, `dm-realtime`. Bộ DM cần fixture do script nghiệm thu tạo trong `.dm-acceptance/`; hồ sơ trước 2026-11-11 còn ghi lệnh `--config playwright.dm-*.config.js` cũ. Kết quả lịch sử tại [Community](../records/verification/community/README.md) và [DM](../records/verification/messaging/README.md).

<a id="tài-liệu"></a>

### Tài liệu

```bash
python3 scripts/check_docs.py
```

`make docs-check` kiểm tra link và anchor trong tài liệu hiện hành và các README; không kiểm tra archive.

- Mỗi lần chạy ca kiểm thử phải ghi build, môi trường, dữ liệu, kết quả thực tế và bằng chứng ([mẫu hồ sơ](../records/templates/release-record.md)). Quy trình xem [nghiệm thu](../releases/acceptance.md#testing) và [điều kiện phát hành](../releases/acceptance.md#release-gates).
- Lệnh trong trang này chỉ dùng trên máy; không thay manifest production ([runbook](operations.md)).
- Chưa có kịch bản kiểm thử tải (k6). Ngưỡng và cách đo xem [chất lượng](../system/quality.md#quality-targets). Fixture trong `docs/fixtures` là dữ liệu biên, không phải kết quả test.

<a id="troubleshooting"></a>

<a id="7-xử-lý-lỗi-thường-gặp"></a>

## 7. Xử lý lỗi thường gặp

| Hiện tượng | Kiểm tra / xử lý |
|---|---|
| API không khởi động | Kiểm tra connection string, signing key, môi trường và log; khởi động PostgreSQL trước |
| Trùng cổng 3000/5026/5432 | Kiểm tra service Compose và tiến trình local, dừng thành phần trùng trước khi chạy |
| Login tài khoản seed thất bại | Seed không chứa password đăng nhập thực; tạo và xác minh tài khoản qua API |
| Login trả 403 | Xem `errorCode`; chưa xác minh cần hoàn tất verify, không tự coi mọi 403 là phiên hết hạn |
| Không thấy Swagger | Swagger chỉ bật khi môi trường Development |
| Sửa SQL nhưng dữ liệu không đổi | Script init không chạy lại trên volume đã khởi tạo; xác định cách cập nhật dữ liệu trước khi thực hiện |
| DM hay gửi tin không hoạt động | Kiểm tra đúng bản nền, phiên và email, proxy API và Hub, khóa fingerprint. Không dùng tài khoản seed. Tin phòng chưa có |

Quan sát container local:

```bash
docker compose ps
docker compose logs chat-service postgres
```

<a id="community"></a>

<a id="8-community-database-thử-và-migration"></a>

## 8. Community: database thử, kiểm thử và migration

Hướng dẫn chạy Community trên máy với database thử riêng: build, test backend, chạy API, chạy WebClient và test trình duyệt, nâng cấp database đã có dữ liệu.

**Cập nhật:** 2026-10-13, theo nền `900e226` (gồm tạo và xem server, tham gia, tìm kiếm, quyền, migration 001 đến 004). Các lệnh dưới đây để chạy lại, không phải kết quả mới. Phạm vi xem [Community](../features/community/README.md); bằng chứng xem [hồ sơ Community](../records/verification/community/README.md) và [hồ sơ hợp nhất](../records/verification/messaging/merge-2026-10-13.md).

<a id="chuẩn-bị"></a>

### Chuẩn bị

Cần .NET SDK 10, Node 24/npm, Python 3, OpenSSL và PostgreSQL 18 qua Docker hoặc Podman. Chạy từ root repo trong Bash/zsh hoặc WSL; mở terminal riêng cho API và Vite. Dùng checkout chứa nền `900e226` và ghi revision của lần thử:

```bash
git status --short --branch
git rev-parse HEAD
```

Không reset nhánh. Ghi kết quả `git rev-parse HEAD` vào hồ sơ của lần chạy.

<a id="local-environment"></a>

<a id="database-thử-nghiệm-riêng"></a>

### Database thử nghiệm riêng

Dùng một container riêng cho dữ liệu thử: tên `scdc-community-docs-db`, database `scdc_community_docs_test`, cổng `127.0.0.1:15432`. Nếu tên hoặc cổng đã bị dùng, đổi sang giá trị khác và sửa đồng bộ mọi biến bên dưới. Bộ test sẽ tạo, xóa database fixture và ghi dữ liệu thử, nên user PostgreSQL cần quyền làm việc đó.

```bash
export SCDC_CONTAINER_ENGINE=podman # Đổi thành docker nếu dùng Docker.
"$SCDC_CONTAINER_ENGINE" run --rm -d --name scdc-community-docs-db \
  -e POSTGRES_DB=scdc_community_docs_test -e POSTGRES_USER=scdc \
  -e POSTGRES_PASSWORD=scdc_docs_test_only \
  -p 127.0.0.1:15432:5432 docker.io/library/postgres:18-alpine
"$SCDC_CONTAINER_ENGINE" exec scdc-community-docs-db \
  pg_isready -U scdc -d scdc_community_docs_test
```

Chờ `pg_isready` báo "accepting connections" (gọi lại nếu chưa sẵn sàng). Rồi tạo schema cho **database mới của container này**:

```bash
"$SCDC_CONTAINER_ENGINE" exec -i scdc-community-docs-db \
  psql -X -v ON_ERROR_STOP=1 -U scdc -d scdc_community_docs_test \
  < database/postgres/schema.sql
```

Schema đã gồm ledger 001 đến 004. Không cần seed; tài khoản thử tạo qua API.

**Cảnh báo:** `schema.sql` có DROP SCHEMA. Database có dữ liệu cần giữ phải dùng [phần nâng cấp](#migration), không dùng lệnh trên.

<a id="community-configuration"></a>

<a id="cấu-hình-api-và-test"></a>

### Cấu hình API và test

Đặt trong terminal dùng để build/test/chạy API:

```bash
export ConnectionStrings__Database='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc;Password=scdc_docs_test_only'
export Modules__Identity__SigningKey="$(openssl rand -base64 48)"
export Modules__Identity__ExposeDevelopmentTokens=true
export Modules__Community__Operations__ActiveKeyId=docs_test
export Modules__Community__Operations__Keys__docs_test="$(openssl rand -base64 32)"
export Modules__Community__KeyRingPath="$(mktemp -d /tmp/scdc-community-docs-keys.XXXXXX)"
```

Lưu ý về khóa:

- Các khóa trên chỉ dùng cho lần thử này. Không đưa khóa hay token vào commit hoặc file công khai.
- Giữ nguyên signing key, HMAC key và thư mục keyring khi khởi động lại API trong cùng lần thử. Chạy lại khối lệnh sinh khóa sẽ làm token, cursor và thao tác cũ không đọc được.
- Test tự tạo HMAC và keyring riêng, nhưng dùng cấu hình DB và Identity ở trên.
- `KeyRingPath` phải là đường dẫn tuyệt đối; API cần quyền đọc ghi.
- Ở môi trường cần giữ dữ liệu: lưu HMAC key và keyring bằng secret hoặc volume bền. Khi đổi khóa đang dùng, giữ lại khóa cũ để đối chiếu thao tác đã lưu. Mất khóa cũ thì API trả 503, không tạo lại tài nguyên.
- Giữ nguyên `EnvironmentName`, vì Data Protection dùng tên `SCDC.Community.{EnvironmentName}`.
- [Compose](../../compose.yaml) cần `COMMUNITY_OPERATION_KEY` (base64, ít nhất 32 byte) trong `.env` hoặc biến môi trường, và có volume keyring riêng.

<a id="backend-tests"></a>

<a id="build-và-kiểm-thử-backend"></a>

### Build và kiểm thử backend

```bash
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --configuration Release --no-restore -m:1
dotnet test SCDC.slnx --configuration Release --no-build --no-restore -m:1 \
  --logger 'trx;LogFileName=community-local.trx' \
  --results-directory artifacts/community-local/backend
```

- Tên database phải kết thúc bằng `_test`; không trỏ bộ test vào database ứng dụng.
- Test migration tự tạo và xóa database fixture (có kiểm tra trước, rollback, checksum). Chạy test không nâng cấp database ứng dụng.
- File TRX là kết quả của lần chạy này, không thay số liệu trong hồ sơ cũ.

Sau đó chạy API trong cùng terminal:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:15026 \
  dotnet run --project services/SCDC.Api --configuration Release \
  --no-build --no-launch-profile
```

Swagger: `http://127.0.0.1:15026/swagger`. Health: `/api/v1/health` (không kiểm tra DB; muốn kiểm tra kết nối thì gọi một API nghiệp vụ). API không tự chạy migration.

<a id="frontend-tests"></a>

<a id="webclient-và-browser-tests"></a>

### WebClient và browser tests

Terminal thứ hai:

```bash
cd clients/WebClient
npm ci
npm test
npm run build
npx playwright install chromium
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 \
  npm run dev -- --host 127.0.0.1 --port 15300 --strictPort
```

Vite chạy ở `http://127.0.0.1:15300` và chuyển tiếp `/api`, `/hubs` tới API. Nếu Linux thiếu thư viện cho Chromium, có thể chạy trong image `mcr.microsoft.com/playwright:v1.63.0-noble` (Node 24, Chromium).

Terminal thứ ba (API và Vite vẫn chạy):

```bash
cd clients/WebClient
SCDC_E2E_URL=http://127.0.0.1:15300 \
SCDC_E2E_DATABASE=scdc_community_docs_test \
  npm run test:e2e -- community --output ../../artifacts/community-local/browser
```

- `SCDC_E2E_DATABASE` chỉ là khai báo để bộ test tự bảo vệ, không đổi kết nối của API. Phải tự kiểm tra API đang trỏ đúng database `_test`.
- Playwright không tự chạy API hay Vite, chỉ chấp nhận `localhost` hoặc `127.0.0.1`, chạy một worker, không thử lại.
- Ghi vào hồ sơ: phiên bản Node, trình duyệt, hệ điều hành, commit, exit code, kết quả thực tế, trace hoặc ảnh cần thiết.

<a id="kiểm-tra-nhanh-bằng-tay"></a>

### Kiểm tra nhanh bằng tay

1. Tạo owner, member và outsider bằng luồng [Identity local](development.md#identity), xác minh email rồi đăng nhập. Token Development phục vụ local; không dùng dữ liệu đăng nhập từ seed.
2. Owner tạo cộng đồng public/immediate; member tìm/preview rồi join. Outsider không được thấy dữ liệu riêng của cộng đồng private.
3. Owner mở phần phòng từ detail, tạo phòng text, sửa tên/topic và reload. Member thấy metadata phòng có view.
4. Owner cấu hình role/ACL deny rồi personal allow; member kiểm tra lại list/detail và gọi trực tiếp API. Kiểm tra version conflict và GET đối soát trước khi lưu lại.
5. Chạy browser suite cho lost response, pending operation qua reload, stale selection/actor, pagination và roster epoch; ghi riêng lỗi API tiêm với thao tác dùng API thật.

Các bước trên chỉ kiểm tra Community, phòng và quyền. Tin phòng, nhận realtime theo quyền phòng và thu hồi quyền phòng trên kết nối thật chưa có, sẽ làm trong v1. Kết quả ở đây không chứng minh các [tiêu chí Community chuyển sang v1](../releases/mvp.md#community-acceptance); xem [phần chưa chứng minh](../records/verification/community/README.md#unverified).

<a id="migration"></a>

<a id="nâng-cấp-db-đã-có-dữ-liệu"></a>

### Nâng cấp DB đã có dữ liệu

Trước khi chạy migration:

1. Dùng connection string riêng cho database cần nâng cấp.
2. Chạy kiểm tra trước, dừng các writer cũ, chuẩn bị kế hoạch rollback ứng dụng (về bản còn tương thích schema).
3. Đọc ledger `common.schema_migrations` và [thiết kế migration](../features/community/mechanics.md#schema-migration). Không sửa SQL hay checksum đã áp.

Ví dụ cho database cũ chưa áp 001 đến 004 (dùng `tools/SCDC.DbMigrator`):

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/001-community-create-view.sql \
  /absolute/path/reviewed-server-map.json
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/002-community-search.sql
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/003-community-roles.sql
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/004-community-channels-access.sql \
  /absolute/path/reviewed-channel-map.json
```

- Các file mapping phải do bạn tạo và review; repo không có sẵn.
- **Server map:** khai báo `visibility` và `joinMode` cho từng ID server.
- **Channel map:** khai báo mọi ID phòng cũ, với `kind` (`text` hoặc `voice`) và `defaultView` (`allow` hoặc `deny`). Không tự suy từ visibility cũ.
- Database không có dữ liệu cũ tương ứng thì bỏ tham số map.
- Bước kiểm tra trước sẽ từ chối nếu mapping thiếu, thừa, trùng hoặc có trạng thái không hỗ trợ. Sửa dữ liệu bằng thay đổi đã review rồi chạy lại; không dùng `schema.sql` để sửa.
- Chỉ triển khai writer mới khi ledger và migration đã hợp lệ.
- Đây không phải quy trình phát hành production đã được duyệt.

Ví dụ server map (`reviewed-server-map.json`):

```json
{
  "01990000-0000-7200-8000-000000000001": {
    "visibility": "private",
    "joinMode": "immediate"
  }
}
```

Ví dụ channel map (`reviewed-channel-map.json`):

```json
{
  "01990000-0000-7200-8000-000000000002": {
    "kind": "text",
    "defaultView": "allow"
  }
}
```

Thay ID và giá trị bằng mapping của DB được nâng cấp; các mẫu không phải dữ liệu dùng chung cho mọi môi trường.

<a id="kết-thúc-lần-thử"></a>

### Kết thúc lần thử

Dừng API và Vite bằng Ctrl+C. Container chạy với `--rm` và không có volume, nên dừng là xóa luôn database thử:

```bash
"$SCDC_CONTAINER_ENGINE" stop scdc-community-docs-db
```

Xóa thư mục keyring tạm (tạo bằng `mktemp`) nếu không cần nữa. Giữ TRX, trace và ảnh cần review trong `artifacts/community-local/`; không commit database, khóa hay token. Kiểm tra tài liệu từ thư mục gốc repo:

```bash
python3 scripts/check_docs.py
```

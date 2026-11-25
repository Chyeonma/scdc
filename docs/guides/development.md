<a id="scdc--hướng-dẫn-phát-triển"></a>

# SCDC — Hướng dẫn phát triển

Cách cài đặt, chạy, cấu hình và kiểm thử SCDC trên máy phát triển.

**Cập nhật:** 2026-11-20, bổ sung database user và migration S2-03 cho hướng dẫn dựng môi trường S1-08 cho gateway, service mẫu, JWT và giám sát. Riêng Community (database thử, kiểm thử, migration) xem [mục 8](#community).

Các lệnh chạy từ thư mục gốc repo, trừ khi ghi khác. Cấu hình và dữ liệu mẫu chỉ dùng cho môi trường phát triển.

<a id="mục-lục"></a>

## Mục lục

- [Chuẩn bị và khởi chạy](#setup)
- [Docker Desktop, Docker Engine và Podman](#container-engines)
- [Lệnh thường dùng](#short-commands)
- [Cấu hình](#configuration)
- [Tài khoản để tích hợp](#identity)
- [Database và dữ liệu mẫu](#database)
- [Quy ước code và Git](#conventions)
- [Kiểm thử](#testing)
- [CI trên GitHub Actions](continuous-integration.md)
- [Xử lý lỗi thường gặp](#troubleshooting)
- [Community: database thử, kiểm thử và migration](#community)

<a id="setup"></a>

<a id="1-chuẩn-bị-và-khởi-chạy"></a>

## 1. Chuẩn bị và khởi chạy

**Cần có:** Git, Make, Docker với Compose hoặc Podman với Compose provider, Python 3, OpenSSL và curl. Clone repo rồi mở terminal tại thư mục chứa `Makefile` và `compose.yaml`. Các lệnh dưới dùng Bash, zsh hoặc WSL; Windows dùng Linux containers và bật WSL integration nếu chọn Docker Desktop.

Backend và frontend được build trong image. .NET SDK 10 và Node.js 24 chỉ cần khi debug hoặc kiểm thử trực tiếp trên máy; Python 3 cần cho các công cụ kiểm thử và tài liệu.

<a id="chạy-bằng-compose"></a>

### Chạy bằng Compose

**Lần đầu**, dựng môi trường bằng một lệnh:

```bash
make setup
```

Nếu chọn Podman, dùng `make setup ENGINE=podman`, rồi thêm `ENGINE=podman` vào mọi lệnh Make bên dưới. Kiểm tra engine theo [mục công cụ container](#container-engines) nếu không kết nối được.

`setup` thực hiện lần lượt:

1. Tạo `.env` với khóa Community ngẫu nhiên nếu file chưa có. File hiện hữu được giữ nguyên; khóa không in ra terminal.
2. Kiểm tra cấu hình Compose, build image và khởi chạy stack.
3. Compose tự tạo khóa DM và JWT bằng hai job `messaging-keygen`, `identity-keygen`; API chờ các job thành công rồi mới chạy. Các job không ghi đè khóa hiện hữu.
4. Chạy `make check`: đợi giao diện và các endpoint health trả HTTP 200, kiểm tra PostgreSQL nhận kết nối và cấu hình Nginx hợp lệ. Lệnh thất bại nếu một bước không đạt.

Nếu đã có `.env` nhưng thiếu `COMMUNITY_OPERATION_KEY`, tạo khóa bằng `openssl rand -base64 32`, thêm vào file dưới dạng `COMMUNITY_OPERATION_KEY=<khóa-vừa-tạo>`, rồi chạy lại. Giữ nguyên file này cùng volume dữ liệu qua những lần chạy.

Xem trạng thái sau khi dựng:

```bash
make status
```

Hai job tạo khóa có trạng thái `Exited (0)` là bình thường; các container PostgreSQL, API, service mẫu, WebClient/Nginx, Collector và OpenObserve phải đang chạy. LiveKit thuộc S1-07, chưa có trong stack này.

| Dịch vụ | Địa chỉ |
|---|---|
| WebClient | `http://localhost:3000` |
| Swagger qua gateway (Development) | `http://localhost:3000/swagger` |
| OpenAPI JSON qua gateway | `http://localhost:3000/swagger/v1/swagger.json` |
| Health Nginx, service mẫu, API | `http://localhost:3000/healthz`, `/health`, `/api/v1/health` |
| Endpoint thử JWT S1-06 | `http://localhost:3000/sample/protected`; cần bearer access token do Identity cấp |
| OpenObserve local | `http://localhost:5080`; mặc định `root@scdc.local` / `ScdcObserve!2026Local` trên volume mới |
| API trực tiếp để debug | `http://localhost:5026` |
| PostgreSQL | `localhost:5432` |

`make check` kiểm tra hạ tầng và health; để kiểm tra nghiệp vụ, tạo và xác minh hai tài khoản theo [mục 3](#identity), rồi thử gửi DM qua hai cửa sổ trình duyệt. Tài khoản seed không có mật khẩu đăng nhập thực.

Để kiểm tra giám sát, gửi một request có trace ID dễ tìm:

```bash
curl --fail -H 'traceparent: 00-11111111111111111111111111111111-2222222222222222-01' \
  http://localhost:3000/health
```

Đăng nhập OpenObserve, chọn organization `default`; trong Logs và Traces chọn stream `scdc`, khoảng thời gian vừa chạy, tìm `trace_id = '11111111111111111111111111111111'`. Sau khi Collector gửi dữ liệu, kết quả phải có `service_name=scdc-sample`. Cách đổi tài khoản và tra cứu xem [giám sát](../system/observability.md#s1-05-implementation).

**Dừng rồi chạy lại:**

```bash
make down
make up
make check
```

`down` giữ volume; `up` dùng lại image, dữ liệu và khóa. Khi sửa code, chạy `make build` rồi `make up` để dùng image mới. Có thể chạy lại `make setup` để build và kiểm tra toàn bộ; khóa và cấu hình hiện hữu vẫn được giữ.

Database mới tự nạp schema và seed. Với database đã có dữ liệu, dùng [hướng dẫn nâng cấp](#migration); script khởi tạo không tự chạy lại. Phạm vi tính năng hiện có xem [MVP](../releases/mvp.md); tin phòng và realtime Community chưa có.

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
npm --prefix clients/WebClient ci
python3 scripts/test_gateway.py --engine docker
```

Bộ này cần thêm Python 3. Dùng `--engine podman` nếu chọn Podman. Script build [stack thử riêng](../../compose.gateway-test.yaml), khởi tạo khóa DM và chạy project E2E `gateway`. Mỗi lần chạy có project `scdc-gateway-<mã-lần-chạy>`, database `scdc_gateway_test` và volume riêng; chỉ Nginx được publish ở `127.0.0.1:15310`. Bộ kiểm thử thử lại API sau restart, chủ động đổi IP container API và service mẫu, giữ nguyên container Nginx; kiểm tra service mẫu vẫn trả health khi monolith dừng. Kết quả nằm trong `artifacts/gateway/`; stack và volume thử được dọn khi kết thúc.

Đổi cổng hoặc giữ stack để điều tra lỗi sau khi đã cài dependency:

```bash
python3 scripts/test_gateway.py --engine podman --port 15311 --keep
```

Script in tên project trong kết quả. Dọn đúng stack đã giữ bằng `podman compose -p <tên-project> -f compose.gateway-test.yaml down --volumes`; dùng cùng `GATEWAY_WEB_PORT` nếu đã đổi cổng. Chi tiết phạm vi xem [S1-02](../system/api-conventions.md#s1-02-preparation).

<a id="container-engines"></a>

<a id="docker-desktop-docker-engine-và-podman"></a>

### Docker Desktop, Docker Engine và Podman

Cả ba dùng chung [compose.yaml](../../compose.yaml) và Dockerfile. Chọn một engine và dùng nó cho cả tạo khóa, chạy, xem log và dừng; Docker và Podman không dùng chung volume dữ liệu.

| Môi trường | Chuẩn bị | Kiểm tra trước khi dựng |
|---|---|---|
| Docker Desktop | Mở ứng dụng; Windows dùng Linux containers và bật WSL integration | `docker info` và `docker compose version` |
| Docker Engine với CLI | Engine đang chạy, CLI kết nối được và đã cài Compose plugin | `docker info` và `docker compose version` |
| Podman | Cài Compose provider; Windows/macOS cần Podman machine đang chạy | `podman info` và `podman compose version` |

- [Docker Desktop](https://docs.docker.com/compose/install/) đã gồm Engine, CLI và Compose.
- [`podman compose`](https://docs.podman.io/en/latest/markdown/podman-compose.1.html) gọi một Compose provider bên ngoài (`podman-compose` hoặc `docker-compose`); chọn bằng `PODMAN_COMPOSE_PROVIDER` nếu cần.
- [Podman machine](https://docs.podman.io/en/latest/markdown/podman-machine.1.html): chưa có thì `podman machine init`, rồi `podman machine start`.

`make setup` gọi Compose và kiểm tra stack thực tế. Make không thay engine hoặc Compose provider; cần cài và khởi động chúng trước. Tên image ghi đầy đủ registry (`docker.io/library/...`, `mcr.microsoft.com/...`) để Podman không phải đoán ([quy tắc tên image](https://docs.podman.io/en/latest/markdown/podman-pull.1.html)).

<a id="debug-backend-local"></a>

### Debug backend local

Phần này là tùy chọn; cần .NET SDK 10. API chạy trên máy không tự đọc `.env` của Compose. Cần cấu hình khóa JWT theo [mục dưới](#configuration), HMAC và keyring Community, cùng thư mục khóa fingerprint DM.

Giữ một bộ khóa riêng cho cách chạy local; không trộn với dữ liệu đã tạo bằng khóa Compose. Nếu cần môi trường thử có cấu hình đầy đủ, làm theo [database thử riêng](#local-environment) và [cấu hình API](#community-configuration) ở mục 8. Để gửi DM trên môi trường local đó, đặt thêm trong cùng terminal:

```bash
export Modules__Messaging__FingerprintKeyDirectory="$(pwd)/.local/messaging-keys"
export Modules__Messaging__ActiveFingerprintKeyId=dm-local-v1
mkdir -p "$Modules__Messaging__FingerprintKeyDirectory"
if [ ! -e "$Modules__Messaging__FingerprintKeyDirectory/dm-local-v1.key" ]; then
  (umask 077; openssl rand 32 > "$Modules__Messaging__FingerprintKeyDirectory/dm-local-v1.key")
fi
```

Sau khi đã đặt kết nối và các khóa, build và chạy:

```bash
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

Phần này cần Node.js 24 và npm. Vite chạy ở cổng 3000 và chuyển tiếp `/api`, `/hubs`, `/swagger` tới API ở 5026. Nếu container web đang chiếm cổng 3000 thì chạy `docker compose stop web-client` trước. Khi dùng Vite, các kiểm tra Nginx và service mẫu vẫn phải thực hiện trên stack Compose.

<a id="short-commands"></a>

<a id="lệnh-ngắn-và-đồng-bộ-docs-giữa-các-nhánh"></a>

<a id="lệnh-ngắn"></a>

### Lệnh thường dùng

Các lệnh trong [Makefile](../../Makefile) chạy từ gốc repo. Thêm `ENGINE=podman` nếu dùng Podman; `make` hoặc `make help` hiển thị danh sách.

| Lệnh | Tác dụng |
|---|---|
| `make setup` | Tạo cấu hình khi chưa có, build, khởi chạy và kiểm tra toàn bộ stack |
| `make up` | Chạy lại stack bằng image hiện có; không chủ động build lại |
| `make build` | Build lại image; chạy `make up` sau đó để cập nhật container |
| `make check` | Kiểm tra health, kết nối PostgreSQL và cấu hình Nginx |
| `make status` | Xem container, gồm hai job tạo khóa đã thoát |
| `make logs` | Xem 100 dòng cuối của mọi service |
| `make logs SERVICE=chat-service` | Xem log của một service; thay tên bằng `postgres`, `otel-collector`, `openobserve` khi cần |
| `make down` | Dừng stack, giữ volume |
| `make docs-check` | Kiểm tra link và anchor tài liệu; cần Python 3 |

Kiểm tra một stack khác trên cùng máy bằng `COMPOSE_ARGS="-p scdc-demo"` và `ENV_FILE=.env.demo`; dùng cùng giá trị cho mọi lệnh. `make check` dùng các cổng local trong bảng địa chỉ; tùy chọn project không tự đổi cổng. Makefile dành cho môi trường phát triển, không tự chạy kiểm thử trên database ứng dụng.

<a id="docs-sync"></a>

**Đồng bộ docs giữa các nhánh:** với [quy ước nhánh ngắn](../project/workflow.md#branches), chỉ cần `git pull --rebase origin main` trước khi mở PR. Công cụ dưới chỉ dùng cho nhánh cũ sống lâu (ví dụ `feat/identity`): nó **chép đè toàn bộ** `docs/` từ nhánh nguồn sang nhánh đích, không trộn nội dung. Cần Python 3 và Git.

```bash
python3 scripts/sync_docs.py --source main --target feat/identity --dry-run
python3 scripts/sync_docs.py --source main --target feat/identity
```

Cần working tree sạch; công cụ không tự stash, commit hay push. Chi tiết: `python3 scripts/sync_docs.py --help`.

<a id="configuration"></a>

<a id="2-cấu-hình"></a>

## 2. Cấu hình

Backend đọc `appsettings.json`; launch profile `http` bật Development và nạp `appsettings.Development.json`. Compose truyền cấu hình qua biến môi trường.

| Khóa | Giá trị local / vai trò |
|---|---|
| `ConnectionStrings:Identity` | Database `scdc_chat`, user `scdc_identity`, mật khẩu local `scdc_identity_dev` |
| `ConnectionStrings:Community` | Cùng database, user `scdc_community`, mật khẩu local `scdc_community_dev` |
| `ConnectionStrings:Messaging` | Cùng database, user `scdc_messaging`, mật khẩu local `scdc_messaging_dev` |
| `Authentication:Jwt:Issuer` / `Audience` | `SCDC` / `SCDC.WebClient`, dùng chung cho bên cấp và bên xác minh |
| `Authentication:Jwt:PublicKeyDirectory` / `KeyIds` | Thư mục public key SPKI và danh sách ID tin cậy; file `<kid>.pub.pem`, nạp khi khởi động |
| `Modules:Identity:PrivateKeyPath` / `SigningKeyId` | File private key PKCS#8 chỉ cấp cho Identity và key ID đang ký. Compose tự khởi tạo trong volume bền qua `identity-keygen` |
| `Modules:Identity:ExposeDevelopmentTokens` | `true` trong Development, dùng token trả về để thử xác minh/reset; giá trị mặc định `false` |
| `Modules:Community:Operations:ActiveKeyId` / `Keys:<id>` | ID active và key base64 ít nhất 32 byte ngẫu nhiên; giữ key cũ cho operation còn lưu, xem [mục 8](#community-configuration) |
| `Modules:Community:KeyRingPath` | Thư mục keyring bền, API đọc/ghi được; giữ qua restart để cursor vẫn đọc được |
| `Modules:Identity:UserSearch:CursorKeyRingPath` / `ApplicationName` | Keyring bảo vệ cursor tìm người/inbox/lịch sử DM; provider riêng với Community, giữ qua restart. Compose lưu trong `identity-user-search-keys` |
| `Modules:Messaging:FingerprintKeyDirectory` / `ActiveFingerprintKeyId` | Thư mục chứa `<keyId>.key` dạng byte, tối thiểu 32 byte ngẫu nhiên; thiếu khóa chặn gửi/retry. Compose tự tạo khóa qua job `messaging-keygen`, giữ trong volume `scdc-messaging-keys` |
| `Cors:AllowedOrigins` | Local hiện có `http://localhost:3000`, `http://localhost:5173`; Vite của repo mặc định 3000 |

Thời hạn token, phiên và khóa đăng nhập xem [Tài khoản](../features/accounts/README.md). Khi đặt bằng biến môi trường, thay `:` bằng `__`, ví dụ `ConnectionStrings__Identity`. Host là `localhost` khi chạy API trên máy, `postgres` trong Compose. Mỗi pool mặc định tối đa 20 kết nối; có thể đặt `Maximum Pool Size` trong connection string. Ba biến `IDENTITY_DB_PASSWORD`, `COMMUNITY_DB_PASSWORD`, `MESSAGING_DB_PASSWORD` ghi đè mật khẩu local trong Compose; giữ đồng bộ với role đã cấp. Tài khoản `scdc` chỉ dùng quản trị/migration.

Nguồn: [appsettings.json](../../services/SCDC.Api/appsettings.json), [cấu hình Development](../../services/SCDC.Api/appsettings.Development.json), [Vite config](../../clients/WebClient/vite.config.js).

### Khóa JWT khi debug API trên máy

Compose tự tạo khóa RSA ở lần chạy đầu; các lần sau giữ nguyên khóa trong `scdc-identity-private-keys` và `scdc-identity-public-keys`. Service mẫu chỉ nhận volume công khai chỉ đọc. Không xóa các volume khóa khi khởi động lại thông thường.

Khi chạy API trực tiếp, tạo bộ khóa local riêng trước:

```bash
export Modules__Identity__PrivateKeyPath="$(pwd)/.local/jwt/private/identity-local-v1.pem"
export Modules__Identity__SigningKeyId=identity-local-v1
export Authentication__Jwt__PublicKeyDirectory="$(pwd)/.local/jwt/public"
export Authentication__Jwt__KeyIds__0=identity-local-v1
dotnet run --project services/SCDC.Api --no-launch-profile -- --initialize-jwt-keys
```

Lệnh có thể chạy lại và không ghi đè khóa hiện hữu; private file tạo với quyền 0600 trên Unix. Giữ các biến trên trong terminal chạy API; khi chạy sample chỉ cấp các biến `Authentication__Jwt__*` và public key. Kiểm thử backend tự sinh RSA fixture riêng. Cách đổi khóa và giới hạn kiểm tra phiên của sample xem [D28–D29](../system/api-conventions.md#v1-user-token-signing).

<a id="identity"></a>

<a id="3-tạo-tài-khoản-để-tích-hợp"></a>

## 3. Tạo tài khoản để tích hợp

1. Đăng ký qua form hoặc `POST /api/v1/auth/register` với `username`, `displayName`, `email`, `password`.
2. Trong Development, lấy `developmentVerificationToken` từ response đăng ký và gửi `POST /api/v1/auth/verify-email` với `{ "token": "..." }`.
3. Đăng nhập bằng email hoặc username. Dùng `accessToken` làm Bearer token khi gọi endpoint yêu cầu xác thực.
4. Quên mật khẩu dùng `POST /api/v1/auth/forgot-password`; response Development có thể chứa `developmentResetToken`. Dùng token đó cùng `newPassword` ở endpoint reset.

Token Development chỉ để thử trên máy. Nền chưa có gửi lại email hay worker email; phần này ở nhánh `feat/identity@60f4db3` ([hiện trạng](../features/accounts/email-links.md#status)).

Image không chứa `appsettings.Development.json`; Compose local bật `Modules__Identity__ExposeDevelopmentTokens` trực tiếp để luồng xác minh này dùng được. Khi debug bằng launch profile `http`, cấu hình Development được đọc từ file trên máy.

Frontend quản lý phiên trong [api.js](../../clients/WebClient/src/api.js): có Web Locks thì đồng bộ phiên giữa các tab và chỉ một tab làm mới token; không có thì mỗi tab giữ token riêng. Hàm gọi API mặc định thử lại sau 401; các thao tác mở DM, gửi, sửa, xóa dùng `retry: false`. Gửi lại tin do người dùng bấm (DEC-021); kết nối lại thì tự bù dữ liệu qua REST.

<a id="database"></a>

<a id="4-database-và-dữ-liệu-mẫu"></a>

## 4. Database và dữ liệu mẫu

Database trên máy: `scdc_chat`, cổng 5432, tắt SSL. API dùng ba user riêng ở [cấu hình](#configuration); tài khoản quản trị local là `scdc` / `scdc_dev`. Các schema xem [database](../system/database.md).

- `schema.sql` tạo schema, ràng buộc, index, trigger, view.
- `seed.sql` có dữ liệu mẫu; mật khẩu và token trong đó chỉ minh họa, không đăng nhập được. Tạo tài khoản mới qua API.
- Compose chạy schema, seed và script cấp quyền [init-module-roles.sh](../../database/postgres/init-module-roles.sh) trong `/docker-entrypoint-initdb.d/`; chúng **chỉ chạy khi volume mới**. Sửa SQL không cập nhật volume đã có dữ liệu. Không dùng script này như migration.

Hai view nội bộ Identity vẫn dùng để quan sát. Hai view Messaging đọc chéo đã bỏ ở S2-05; quan sát bằng bảng nội bộ hoặc API nghiệp vụ. Ví dụ:

```sql
SELECT * FROM identity.v_user_accounts ORDER BY username;
SELECT * FROM identity.v_active_sessions;
SELECT id, space_type, last_message_sequence FROM messaging.spaces;
SELECT id, space_id, conversation_sequence, version FROM messaging.messages ORDER BY space_id, conversation_sequence;
SELECT * FROM identity.outbox_events ORDER BY occurred_at;
SELECT * FROM community.outbox_events ORDER BY occurred_at;
SELECT * FROM messaging.outbox_events ORDER BY occurred_at;
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

Giám sát S1-05 dùng `python3 scripts/test_observability.py --engine docker` (hoặc `--engine podman`): kiểm tra log, metric, trace, dữ liệu nhạy cảm và Collector dừng trên stack riêng. Cần Python 3; cách đọc kết quả và giới hạn xem [giám sát](../system/observability.md#s1-05-implementation).

Pull request được kiểm tra bằng workflow [CI build và kiểm thử](continuous-integration.md): build toàn solution, chạy test backend với PostgreSQL riêng, chạy test và build WebClient. Hướng dẫn cấu hình check `CI` bắt buộc trước khi merge nằm trong cùng trang.

<a id="backend"></a>

### Backend

```bash
dotnet test SCDC.slnx --configuration Release
```

Bộ test gồm Identity, Community, DM, response API, quyền, chống trùng, migration, transaction, realtime.

- Dùng PostgreSQL riêng, tên database kết thúc `_test`, user có quyền tạo và xóa database fixture.
- Đặt connection string và token Development theo [cấu hình](#configuration); test factory tự cấp khóa RSA. DM vẫn cần khóa fingerprint theo [hồ sơ DM](../records/verification/messaging/DM-MVP.md).
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

Script kiểm tra link và anchor trong tài liệu hiện hành và các README; không kiểm tra archive.

- Mỗi lần chạy ca kiểm thử phải ghi build, môi trường, dữ liệu, kết quả thực tế và bằng chứng ([mẫu hồ sơ](../records/templates/release-record.md)). Quy trình xem [nghiệm thu](../releases/acceptance.md#testing) và [điều kiện phát hành](../releases/acceptance.md#release-gates).
- Lệnh trong trang này chỉ dùng trên máy; không thay manifest production ([runbook](operations.md)).
- Chưa có kịch bản kiểm thử tải (k6). Ngưỡng và cách đo xem [chất lượng](../system/quality.md#quality-targets). Fixture trong `docs/fixtures` là dữ liệu biên, không phải kết quả test.

<a id="troubleshooting"></a>

<a id="7-xử-lý-lỗi-thường-gặp"></a>

## 7. Xử lý lỗi thường gặp

| Hiện tượng | Kiểm tra / xử lý |
|---|---|
| API không khởi động | Kiểm tra connection string, cặp khóa RSA, key ID, môi trường và log; khởi động PostgreSQL trước |
| Compose báo thiếu `COMMUNITY_OPERATION_KEY` | `make setup` chỉ tạo `.env` khi file chưa có; nếu file hiện hữu thiếu khóa thì bổ sung khóa, rồi chạy lại |
| Job tạo khóa đã thoát | `Exited (0)` là thành công; mã khác 0 thì xem `make logs SERVICE=identity-keygen` hoặc `make logs SERVICE=messaging-keygen` |
| Trùng cổng 3000/5026/5432 | Kiểm tra service Compose và tiến trình local, dừng thành phần trùng trước khi chạy |
| Gateway trả 502 khi vừa khởi động | Chờ hai API sẵn sàng; xem `docker compose logs --tail=100 chat-service sample-service web-client` |
| OpenObserve chưa có log hoặc trace | Kiểm tra khoảng thời gian, organization `default`, stream `scdc`; xem `make logs SERVICE=otel-collector` và `make logs SERVICE=openobserve` |
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

Schema đã gồm ledger 001 đến 004 và S2-03. Cấp ba role cho database thử vừa tạo:

```bash
"$SCDC_CONTAINER_ENGINE" exec -i \
  -e IDENTITY_DB_PASSWORD=scdc_identity_dev \
  -e COMMUNITY_DB_PASSWORD=scdc_community_dev \
  -e MESSAGING_DB_PASSWORD=scdc_messaging_dev scdc-community-docs-db \
  psql -X -U scdc -d scdc_community_docs_test < database/postgres/module-roles.sql
```

Không cần seed; tài khoản thử tạo qua API. Role API không được tạo database; fixture dùng tài khoản quản trị riêng.

**Cảnh báo:** `schema.sql` có DROP SCHEMA. Database có dữ liệu cần giữ phải dùng [phần nâng cấp](#migration), không dùng lệnh trên.

<a id="community-configuration"></a>

<a id="cấu-hình-api-và-test"></a>

### Cấu hình API và test

Đặt trong terminal dùng để build/test/chạy API:

```bash
export ConnectionStrings__TestDatabase='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc;Password=scdc_docs_test_only'
export ConnectionStrings__Identity='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc_identity;Password=scdc_identity_dev;Pooling=false'
export ConnectionStrings__Community='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc_community;Password=scdc_community_dev'
export ConnectionStrings__Messaging='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc_messaging;Password=scdc_messaging_dev;Maximum Pool Size=50'
export SCDC_TEST_JWT_DIR="$(mktemp -d /tmp/scdc-community-jwt.XXXXXX)"
export Modules__Identity__PrivateKeyPath="$SCDC_TEST_JWT_DIR/private.pem"
export Authentication__Jwt__PublicKeyDirectory="$SCDC_TEST_JWT_DIR/public"
export Authentication__Jwt__KeyIds__0=identity-local-v1
export Modules__Identity__SigningKeyId=identity-local-v1
dotnet run --project services/SCDC.Api --no-launch-profile -- --initialize-jwt-keys
export Modules__Identity__ExposeDevelopmentTokens=true
export Modules__Community__Operations__ActiveKeyId=docs_test
export Modules__Community__Operations__Keys__docs_test="$(openssl rand -base64 32)"
export Modules__Community__KeyRingPath="$(mktemp -d /tmp/scdc-community-docs-keys.XXXXXX)"
```

Lưu ý về khóa:

- Các khóa trên chỉ dùng cho lần thử này. Không đưa khóa hay token vào commit hoặc file công khai.
- Giữ nguyên cặp khóa RSA, HMAC key và thư mục keyring khi khởi động lại API trong cùng lần thử. Dùng thư mục khóa mới sẽ làm token, cursor và thao tác cũ không đọc được; lệnh `--initialize-jwt-keys` không ghi đè khóa hiện hữu.
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

- Cấu hình thử ở trên tắt pooling của Identity và dành tối đa 50 kết nối Messaging để chạy ca giữ 40 transaction đồng thời; API local thông thường giữ mặc định 20 mỗi pool.
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
3. Đọc ledger `community.schema_migrations` (trước S2-03 là `common.schema_migrations`) và [thiết kế migration](../features/community/mechanics.md#schema-migration). Không sửa SQL hay checksum đã áp.

Ví dụ cho database cũ chưa áp 001 đến 004 (dùng `tools/SCDC.DbMigrator`):

```bash
# Tài khoản quản trị của đúng DB cần nâng cấp; không dùng role API.
export ConnectionStrings__Migration='Host=127.0.0.1;Port=5432;Database=scdc_chat;Username=scdc;Password=scdc_dev'
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

<a id="s2-03-upgrade"></a>

**Nâng cấp S2-03 sau khi đã áp 001–004:**

1. Dừng API và worker, giữ PostgreSQL chạy; lưu backup bằng `pg_dump` để có đường khôi phục. Không dùng `schema.sql` trên volume này.
2. Đặt `ConnectionStrings__Migration` bằng tài khoản quản trị của DB cần nâng cấp rồi chạy:

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/20261120_s2-03_module_databases.sql
```

Migration chuyển outbox theo tiền tố Identity/Community/Messaging, giữ legacy `MessageCreated` tại Messaging, chuyển nguyên bảng audit và ledger, bố trí helper theo owner. Toàn bộ chạy trong một transaction; so sánh đầy đủ các dòng outbox/inbox trước khi bỏ bảng cũ. Event chưa rõ owner sẽ làm rollback. Nếu inbox có dữ liệu, truyền thêm file JSON đã review, ví dụ `{"consumer-name":"messaging"}`; consumer thiếu mapping làm rollback. Chạy lại cùng SQL là no-op; SQL khác checksum bị từ chối.

Volume cũ có thể còn `identity.schema_migrations` với hai cột `id text` và `applied_at timestamptz`. CLI nhận diện cấu trúc này và đổi tên nguyên bảng thành `identity.legacy_schema_migrations` trong cùng transaction, giữ lịch sử và timestamp; ledger mới dùng module/version/checksum. Cấu trúc khác bị từ chối để kiểm tra trước. Script cấp quyền không cho runtime đọc/ghi ledger lưu trữ này. Không sửa SQL/checksum S2-03 đã áp.

Nếu constraint profile cũ dùng `common.utf16_length_identity_legacy(text)`, CLI chuyển nguyên hàm sang Identity, giữ OID và constraint. Các đối tượng khác ngoài phạm vi vẫn làm transaction rollback khi bỏ schema cũ.

3. Cấp role và cập nhật cấu hình. Script hiện hành là bản S2-11: nếu nâng từ nền cũ, tiếp tục áp S2-06, S2-07 và S2-09 bằng CLI trước khi chạy script này; xem [baseline S2-11](#s2-11-upgrade). Với Compose local, nạp lại cấu hình PostgreSQL rồi chạy script cấp quyền:

```bash
docker compose up -d postgres
docker compose exec -T postgres sh /docker-entrypoint-initdb.d/003-module-roles.sh
make build
make up
make check
```

Dùng `podman compose` và `ENGINE=podman` nếu chọn Podman. Script cấp quyền đọc ba biến mật khẩu từ container, chạy lại được và cũng đặt lại mật khẩu theo các biến đó. Nếu kết nối bằng PostgreSQL ngoài Compose, đặt ba biến mật khẩu rồi chạy `psql -X -f database/postgres/module-roles.sql` bằng tài khoản quản trị. API bắt buộc đủ ba connection string; `ConnectionStrings:Database` không còn được dùng. Test thêm `ConnectionStrings:TestDatabase` cho fixture; CLI chỉ đọc `ConnectionStrings:Migration`.

4. Đối chiếu ledger của cả ba schema, số dòng/event ID/trạng thái lease, audit và chạy API nghiệp vụ. [Hồ sơ S2-03](../records/verification/architecture/s2-03-2026-11-20.md) ghi bằng chứng và quyền chéo tạm. Schema mới không tương thích writer cũ; muốn quay về writer cũ phải dừng writer mới và khôi phục backup đã kiểm tra. Không có down migration tự động.

<a id="s2-04-s2-05-upgrade"></a>

**Nâng cấp S2-04 và S2-05:**

Sau S2-03, dừng writer và dùng `ConnectionStrings__Migration` của đúng database cần nâng cấp. Giữ backup đã kiểm tra trước khi đổi schema. Chạy từng migration, không dùng `schema.sql` trên volume có dữ liệu:

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/20261122_s2-04_drop-community-fk.sql
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/20261122_s2-05_drop-messaging-fk-views.sql
```

S2-04 bỏ 8 FK Community → Identity và 1 FK channel → Messaging space. S2-05 bỏ 16 FK Messaging → Identity, hai view đọc chéo và 7 FK/tables rỗng của Moderation. Hai lệnh chạy độc lập sau S2-03, đổi thứ tự được. Ledger ghi `community/2026112204` và `messaging/2026112205`; chạy lại cùng checksum là no-op.

Nếu `moderation.actions` hoặc `moderation.message_reports` có dữ liệu, S2-05 rollback toàn bộ: giữ bảng, FK, view và ledger để chuẩn bị kế hoạch bảo toàn dữ liệu riêng. Không xóa dữ liệu để ép migration qua. FK khác danh sách đã chốt, đối tượng Moderation lạ hoặc view có phụ thuộc chưa xử lý cũng làm rollback. Các khóa được giữ tới commit để không bỏ sót bản ghi ghi đồng thời.

Schema mới giữ ID, NULL, version, thời điểm, nội dung và trạng thái operation/outbox. Các FK nội bộ, bất biến owner/member và cặp DM vẫn được bảo vệ. Khi Identity không còn account/profile, lịch sử DM dùng placeholder có ID cũ và `availability=unavailable`; actor hoặc peer không được cấp quyền ghi từ placeholder. Community giữ membership/owner ID và trả `user=null` khi không tìm được người dùng.

Với script hiện hành S2-11, tiếp tục nâng S2-06/S2-07/S2-09 trước khi cấp quyền và chạy API theo bước S2-03. Writer cũ còn dùng quyền chéo phải dừng trước khi chuyển sang bộ quyền mới. [Hồ sơ S2-04/S2-05](../records/verification/architecture/s2-04-s2-05-2026-11-22.md) ghi kiểm thử dữ liệu và các ca lỗi. Không có down migration tự động.

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

<a id="s2-06-upgrade"></a>

## Nâng cấp S2-06 — Redis xác thực

Cần Python 3 cho `make init`. Lệnh tạo credential Redis tại `$HOME/.local/share/scdc/redis-secrets` (thư mục 0700); đặt `SCDC_REDIS_SECRET_DIR` để chọn nơi khác ngoài repo. File được mount riêng bằng Compose secrets, không đưa mật khẩu vào `.env`, URL hoặc lệnh chạy. Chạy lại giữ nguyên credential. Không xóa hoặc thay credential khi đang chạy; xoay khóa cần đồng bộ ACL, file của ứng dụng và khởi động lại theo kế hoạch.

Trên máy bật SELinux, `make init` dùng `chcon` gán nhãn `container_file_t` cho ba file secret để các container đọc được bind mount. Thư mục host vẫn giữ quyền 0700; không cần tắt SELinux hoặc tắt label của container.

Volume PostgreSQL hiện hữu phải được nâng cấp offline; bootstrap `schema.sql` chỉ dành cho volume mới:

1. Dừng ứng dụng và mọi writer. Nếu chưa có S2-03/04/05, làm theo các hướng dẫn nâng cấp tương ứng trước.
2. Dùng connection migrator/quản trị qua `ConnectionStrings__Migration` và chạy:

   ```bash
   dotnet run --project tools/SCDC.DbMigrator -- database/postgres/migrations/20261123_s2-06_identity_access.sql
   ```

3. Chạy lại `database/postgres/module-roles.sql` theo hướng dẫn cấp ba role để cấp quyền cho bảng/sequence mới. Runtime tiếp tục không có DDL hoặc quyền ledger.
4. `make init`, `make build`, `make up`; dùng cùng `ENGINE=podman` và các tham số env/project nếu môi trường dùng Podman.
5. Chờ `make check`: `/api/v1/health/access` phải 200. Thử đăng nhập, refresh, gửi/nhận DM, thu hồi phiên và khởi động lại.

Migration có ledger/checksum Identity, chạy lại là no-op nếu checksum khớp. Giữ dữ liệu nghiệp vụ; chỉ thêm scope/journal và bootstrap floor cho hai owner. Không có down migration tự động. Giữ schema bổ sung khi quay lại ứng dụng cũ chỉ là khả năng tương thích cấu trúc, chưa thay bằng chứng rollback đã kiểm tra.

Chạy backend local cần Redis thật, ví dụ `Authentication__Sessions__Endpoint=127.0.0.1:6379`, credential username/file tương ứng khi bật ACL. CI dùng Redis riêng không publish ra ngoài runner. Không trỏ fixture vào database phát triển: tên database thử chính phải kết thúc `_test`, user setup cần quyền tạo/xóa database fixture. Các ca Redis access tự dùng database và prefix riêng.

Khi Redis lỗi, HTTP cần đăng nhập trả 503 và Hub ngừng phát; giữ phiên, không tự gửi lại mutation. Lệnh thu hồi đã nhận có thể trả 202 và được worker tiếp tục. Dùng operation ID/GET để đối chiếu, không kết luận rollback chỉ vì request hết thời gian. Sau Redis restart, readiness chỉ mở sau khi rebuild từ PostgreSQL hiện tại, không từ snapshot quyền cũ.

<a id="s2-07-upgrade"></a>

## Nâng cấp S2-07 — Transaction và policy Community

S2-07 cần baseline S2-06. Dừng API/worker và mọi writer, giữ backup đã kiểm tra khôi phục; dùng connection quản trị của đúng database qua `ConnectionStrings__Migration`. Không chạy bootstrap `schema.sql` trên volume có dữ liệu.

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/20261123_s2-07_community_access.sql
```

Migration ghi ledger `community/2026112307`, tạo ba bảng authority `access_scopes`, `access_changes`, `access_change_acks`, nạp scope membership/lifecycle hiện hữu và floor tương ứng ở Community/Messaging. Receipt authority Community cho phép batch rỗng tại Messaging khi thay đổi chỉ mất quyền quản lý; vẫn phải ghi receipt thật trước ACK. Chạy lại cùng checksum là no-op; checksum khác bị từ chối. Dữ liệu nghiệp vụ giữ nguyên, không có down migration tự động.

Với script hiện hành S2-11, tiếp tục nâng S2-09 rồi chạy `database/postgres/module-roles.sql`; sau đó build/chạy API và WebClient cùng phiên bản. Dùng cùng engine, env và project của stack hiện hữu theo [S2-03](#s2-03-upgrade). Với Podman, thêm `ENGINE=podman` cho Make. Runtime không có quyền xóa scope/journal hoặc sửa/xóa ACK. Quyền chéo tạm được thu hồi theo [S2-11](#s2-11-upgrade).

Mutation role, gán role, tạo/sửa channel và ACL phải gửi `X-SCDC-Membership-Id` lấy từ `myMembership.membershipId` của actor trên server đang hiển thị; khác `membershipId` của thành viên đích trong body. Tạo/join server và GET không cần header này. Thiếu/sai dạng trả 400; membership đã đổi trả 409 và cần tải lại trước lần lưu mới.

Thay đổi role/ACL làm mất quyền có thể trả 202 cùng `operationId`, `phase=fencing` và `statusUrl`. Policy hiện hành chưa đổi; không gửi lại mutation tự động. Dùng GET `/api/v1/servers/{serverId}/access-operations/{operationId}` để đọc tiến trình; actor của operation hoặc chủ server hiện tại phải có phiên và membership đang hoạt động. GET không kích hoạt worker. WebClient giữ tham chiếu trong sessionStorage theo actor/server, kể cả sau reload, và có nút kiểm tra tiến trình.

Worker tiếp tục sau restart, nhận ACK thật từ Community/Messaging rồi xin lại permit của đúng session gốc trước final commit. Nếu actor mất phiên/quyền hoặc expectedVersion không còn đúng, policy dự kiến bị hủy; các floor đã cài vẫn giữ, scope mở lại ở epoch mới. Thử ACK thất lạc, owner lỗi, đăng xuất actor khi pending và recovery trước khi mở traffic mới. Bằng chứng tại [hồ sơ S2-07](../records/verification/architecture/s2-07-2026-11-23.md).

<a id="s2-09-upgrade"></a>

## Nâng cấp S2-09 — Tạo phòng bằng transaction riêng

Baseline là S2-07; không cần chờ S2-08. Volume còn ở S2-06 phải áp dụng S2-07 trước. Dừng API, worker và mọi writer; giữ bản sao dữ liệu đã kiểm tra theo quy trình nâng cấp offline. Không chạy `schema.sql` vào volume có dữ liệu.

```bash
dotnet run --project tools/SCDC.DbMigrator -- \
  database/postgres/migrations/20261124_s2-09_channel_provisioning.sql
```

CLI dùng `ConnectionStrings__Migration` của đúng database, ghi checksum `2026112409` vào ledger Community và Messaging trong cùng transaction. Chạy lại là no-op nếu checksum khớp; hai ledger lệch nhau hoặc checksum khác thì dừng. Migration giữ ID, fingerprint/key/version cũ, timestamp và dữ liệu nghiệp vụ; tiếp nhận phòng chữ cũ thành COMPLETED/READY mà không phát lại sự kiện tạo phòng. DM không nằm trong bảng provision phòng.

Chạy lại `database/postgres/module-roles.sql` để cấp quyền bảng/hàm mới và thu hồi toàn bộ quyền chéo tạm theo [S2-11](#s2-11-upgrade). Runtime không được DELETE hai bảng `channel_provisions`: dấu hủy phải còn sau cleanup. Build và chạy API/WebClient cùng phiên bản; giao diện cũ không có luồng đối chiếu và đổi khóa sau hủy. Không tự rollback về writer tạo phòng cũ hoặc chạy down migration; phải dừng writer và xác minh tương thích dữ liệu trước khi chọn phương án rollback.

Community giữ tên phòng trong PREPARING, không đưa phòng vào list/detail. RPC adapter trong process có deadline 2 giây và chỉ chạy ngoài transaction Community; Messaging tự mở connection/transaction của mình. Sau 5 giây chưa hoàn tất, request/worker tranh chốt CANCELLED; chỉ báo `409 CHANNEL_CREATION_CANCELLED` khi quyết định hủy đã commit. Owner chưa dọn được thì `cleanupPending` vẫn còn và worker tiếp tục. Lease worker 6 giây, hết lease có thể nhận lại sau restart. Worker lấy theo `next_attempt_at`; Confirm/Cancel lỗi có backoff tối đa 30 giây để công việc cũ không chiếm hết lượt của thao tác mới.

Khi POST mất phản hồi hoặc trả `503 OPERATION_OUTCOME_UNKNOWN`, giữ nguyên khóa/nội dung và GET `/api/v1/servers/{serverId}/channel-operations/{clientOperationId}`. GET chỉ đọc trạng thái, không tiếp tục công việc. COMPLETED thì đọc phòng hiện tại; CANCELLED thì thao tác thử lại dùng khóa mới; PREPARING thì tiếp tục đối chiếu. Không suy rằng 404 nghĩa POST chưa tới.

Theo dõi `community.channel_provisions`: `phase` 0/1/2 là PREPARING/COMPLETED/CANCELLED; `delivery_pending` biểu thị Confirm/Cancel chưa được owner xác nhận. Đối chiếu log theo operation ID khi work bị hoãn. Không sửa phase hay xóa dấu hủy thủ công. Confirm chậm có thể được thay bằng `ChannelReadyPermit` hiện tại do Community cấp; Messaging vẫn kiểm floor Identity, membership, ACL, lifecycle và hạn trước commit. Permit kích hoạt space không thay quyền cho từng lần đọc/gửi/đăng ký sau này.

<a id="s2-08-upgrade"></a>

## Chạy S2-08 — Transaction DM riêng

S2-08 dùng các bảng Identity/fence hiện có của S2-06 và không thêm migration. Triển khai cùng baseline Community S2-07/S2-09 theo mục trên khi nâng cấp toàn ứng dụng. API tiếp tục cần ba connection string riêng, Redis và các key ring hiện hành. Script S2-11 thu hồi toàn bộ grant tạm sau khi các writer đã chuyển xong.

Để chạy `dm-mvp` trên Linux/macOS, dùng stack `compose.gateway-test.yaml` riêng, web localhost 15300 và database `scdc_gateway_test`; cấp psql qua PATH và PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD trỏ đúng database thử. Chọn run mới tối đa 12 ký tự, xuất `DM_WEB_URL` và `DM_MVP_RUN`. Tạo tài khoản/hội thoại bằng `scripts/dm-acceptance/mvp-local.py -Action Setup -Run <run>` rồi `-Action Fixtures`. Setup trên host có thể dùng `DM_MVP_ENGINE=podman` cùng `DM_MVP_PG_CONTAINER=scdc-gateway-<project>-postgres-1` khi không cài psql. Không tái sử dụng/reset run đã có.

Trong `clients/WebClient`, chạy `npm ci` rồi `npx playwright test --project dm-mvp`; Python 3 và psql phải có trong môi trường chạy browser test. Driver chỉ ghi trạng thái vào tài khoản thuộc manifest của run; tin thử được gửi bằng API thật. Windows vẫn dùng PowerShell driver cũ. [Hồ sơ S2-08](../records/verification/architecture/s2-08-2026-11-24.md) ghi cấu hình và kết quả thực tế.

<a id="s2-11-upgrade"></a>

## S2-11 — Thu hẹp quyền database

S2-11 không thêm DDL hoặc phiên bản ledger. Dùng API đã có S2-07 đến S2-10 và database đã áp S2-06/S2-07/S2-09; dừng API và worker trước khi thay quyền trên volume hiện hữu. Volume dev còn ở S2-06 phải nâng S2-07/S2-09 trước. Script mới từ chối baseline thiếu bảng hoặc ledger thay vì thu hồi quyền của writer cũ.

Chạy bằng quản trị sau khi đối chiếu bản sao dữ liệu và baseline:

```bash
docker compose exec -T postgres sh /docker-entrypoint-initdb.d/003-module-roles.sh
```

Podman dùng `podman compose`. PostgreSQL ngoài Compose dùng `psql -X -f database/postgres/module-roles.sql` cùng ba biến mật khẩu ở bước S2-03. Script chạy trong một transaction, chạy lại được, thu hồi cả quyền SELECT/UPDATE cột ID và EXECUTE đã cấp chéo. Nó cũng đặt lại mật khẩu theo cấu hình; giữ cùng secret và connection string khi khởi chạy lại.

Chạy bộ backend với ba login runtime thật và kiểm tra SELECT/UPDATE ngoài schema bị từ chối `42501`; kiểm tra cả quyền bảng, cột, sequence và hàm trong catalog. Giữ quyền ledger, journal, fence và receipt theo chính sách owner. Sau đó build/chạy API/WebClient cùng phiên bản và kiểm tra đăng nhập, Community, DM, realtime và thu hồi. [Hồ sơ S2-11](../records/verification/architecture/s2-11-2026-11-25.md) ghi kết quả, phép đo và giới hạn; phần tổng kết/demo STEP-2 thuộc S2-12.

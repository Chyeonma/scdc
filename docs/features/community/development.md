# SCDC — Chạy local và kiểm thử Community

Cập nhật: 2026-10-08. Bộ chạy cho `main` sau khi hợp nhất `feat/community-channels`, kế thừa create/view, join, search và permissions. Backend/WebClient Community và migration 001–004 đã có trên `main`. Phạm vi chạy được tại [status](status.md), bằng chứng theo revision tại [verification](delivery/verification.md). Các lệnh dưới đây là hướng dẫn chạy lại; kết quả lần hợp nhất ghi riêng tại [kiểm chứng main](delivery/verification.md#main-merge).

## Chuẩn bị

Cần .NET SDK 10, Node 24/npm, Python 3, OpenSSL và PostgreSQL 18 qua Docker hoặc Podman. Chạy từ root repo trong Bash/zsh hoặc WSL; mở terminal riêng cho API và Vite. Chọn nhánh code đã có tại local, sau khi lưu công việc đang sửa:

```bash
git switch main
git status --short --branch
```

Không reset nhánh để chạy theo hướng dẫn. Ghi `git rev-parse HEAD` trong hồ sơ của lần chạy; đối chiếu commit nền ở acceptance của gói.

<a id="local-environment"></a>

## Database thử nghiệm riêng

Tên/cổng dưới đây dành riêng cho dữ liệu tổng hợp: container `scdc-community-docs-db`, DB `scdc_community_docs_test`, PostgreSQL tại `127.0.0.1:15432`. Nếu tên/cổng đã được dùng, chọn tên/cổng khác và đổi đồng bộ mọi biến bên dưới. Suite Community tạo/drop DB fixture và ghi dữ liệu thử; user PostgreSQL của bộ chạy này có quyền thực hiện các thao tác đó.

```bash
export SCDC_CONTAINER_ENGINE=podman # Đổi thành docker nếu dùng Docker.
"$SCDC_CONTAINER_ENGINE" run --rm -d --name scdc-community-docs-db \
  -e POSTGRES_DB=scdc_community_docs_test -e POSTGRES_USER=scdc \
  -e POSTGRES_PASSWORD=scdc_docs_test_only \
  -p 127.0.0.1:15432:5432 docker.io/library/postgres:18-alpine
"$SCDC_CONTAINER_ENGINE" exec scdc-community-docs-db \
  pg_isready -U scdc -d scdc_community_docs_test
```

Chờ `pg_isready` báo accepting connections, gọi lại lệnh kiểm tra nếu container chưa sẵn sàng. Bootstrap **DB mới của container này** bằng schema trên `main`:

```bash
"$SCDC_CONTAINER_ENGINE" exec -i scdc-community-docs-db \
  psql -X -v ON_ERROR_STOP=1 -U scdc -d scdc_community_docs_test \
  < database/postgres/schema.sql
```

Schema trên `main` gồm ledger 001–004. Không cần seed để đăng ký các tài khoản thử qua API. `schema.sql` có DROP SCHEMA; DB đã có dữ liệu cần giữ phải dùng runner ở [phần nâng cấp](#migration), không dùng lệnh bootstrap này.

<a id="configuration"></a>

## Cấu hình API và test

Đặt trong terminal dùng để build/test/chạy API:

```bash
export ConnectionStrings__Database='Host=127.0.0.1;Port=15432;Database=scdc_community_docs_test;Username=scdc;Password=scdc_docs_test_only'
export Modules__Identity__SigningKey="$(openssl rand -base64 48)"
export Modules__Identity__ExposeDevelopmentTokens=true
export Modules__Community__Operations__ActiveKeyId=docs_test
export Modules__Community__Operations__Keys__docs_test="$(openssl rand -base64 32)"
export Modules__Community__KeyRingPath="$(mktemp -d /tmp/scdc-community-docs-keys.XXXXXX)"
```

Các key trên chỉ dùng cho bộ chạy thử mới này. Giữ nguyên signing key, HMAC key và thư mục keyring khi restart API trong cùng lần thử. Chạy lại block sinh key sẽ thay cấu hình, có thể làm token/cursor/operation cũ không đọc được. Test factory tạo HMAC/keyring tổng hợp riêng nhưng đọc cấu hình DB/Identity. Không đưa giá trị key/token vào commit hoặc artifact công khai.

Ở môi trường cần giữ dữ liệu, lưu HMAC key và keyring bằng secret/volume bền; giữ key cũ khi đổi active ID để đối soát operation đã lưu. Mất HMAC key cũ trả 503, không tạo lại resource. Giữ EnvironmentName vì Data Protection dùng `SCDC.Community.{EnvironmentName}`. Compose trên `main` yêu cầu `COMMUNITY_OPERATION_KEY` base64 ít nhất 32 byte từ `.env` hoặc môi trường và có volume keyring riêng; hướng dẫn kỹ thuật ở [README module](../../../services/Modules/Community/README.md).

<a id="backend-tests"></a>

## Build và kiểm thử backend

```bash
dotnet restore SCDC.slnx
dotnet build SCDC.slnx --configuration Release --no-restore -m:1
dotnet test SCDC.slnx --configuration Release --no-build --no-restore -m:1 \
  --logger 'trx;LogFileName=community-local.trx' \
  --results-directory artifacts/community-local/backend
```

DB phải kết thúc `_test`; đừng trỏ suite vào DB ứng dụng. Migration tests tạo/drop fixture DB, có preflight, rollback và checksum tests; không nâng cấp DB ứng dụng bằng cách chạy suite. TRX ghi kết quả của lần chạy này, không thay số ca ở acceptance lịch sử. Sau test, khởi chạy API trong cùng terminal/cấu hình:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:15026 \
  dotnet run --project services/SCDC.Api --configuration Release \
  --no-build --no-launch-profile
```

Swagger ở `http://127.0.0.1:15026/swagger`, health ở `/api/v1/health`. Health không truy vấn DB; cần gọi API nghiệp vụ để kiểm tra kết nối/schema. API không tự migrate.

<a id="frontend-tests"></a>

## WebClient và browser tests

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

Vite ở `http://127.0.0.1:15300` proxy `/api` và `/hubs` tới API, nên browser dùng cùng origin. Nếu Linux thiếu thư viện hệ thống cho Chromium, chuẩn bị môi trường Playwright trước; môi trường đã dùng trong acceptance là image `mcr.microsoft.com/playwright:v1.63.0-noble` với Node 24/Chromium. Terminal thứ ba, API/Vite vẫn chạy:

```bash
cd clients/WebClient
SCDC_E2E_URL=http://127.0.0.1:15300 \
SCDC_E2E_DATABASE=scdc_community_docs_test \
  npm run test:e2e -- --output ../../artifacts/community-local/browser
```

`SCDC_E2E_DATABASE` là khai báo bảo vệ bộ chạy, không đổi connection string của API. Phải kiểm tra API thực sự trỏ tới cùng DB `_test`. Playwright không tự khởi động API/Vite, chỉ chấp nhận localhost/127.0.0.1, chạy một worker và không retry. Ghi Node/browser/OS, code commit, exit code, actual result và trace/ảnh cần thiết trong hồ sơ.

## Kiểm tra nhanh bằng tay

1. Tạo owner, member và outsider bằng luồng [Identity local](../../development.md#identity), xác minh email rồi đăng nhập. Token Development phục vụ local; không dùng dữ liệu đăng nhập từ seed.
2. Owner tạo cộng đồng public/immediate; member tìm/preview rồi join. Outsider không được thấy dữ liệu riêng của cộng đồng private.
3. Owner mở phần phòng từ detail, tạo phòng text, sửa tên/topic và reload. Member thấy metadata phòng có view.
4. Owner cấu hình role/ACL deny rồi personal allow; member kiểm tra lại list/detail và gọi trực tiếp API. Kiểm tra version conflict và GET đối soát trước khi lưu lại.
5. Chạy browser suite cho lost response, pending operation qua reload, stale selection/actor, pagination và roster epoch; ghi riêng lỗi API tiêm với thao tác dùng API thật.

Bộ chạy hiện hành dừng ở nền Community/phòng/quyền. Chưa có composer/lịch sử tin, Hub/dispatcher hoặc thu hồi kết nối ≤5 giây; không đánh dấu [gate MVP](../../releases/mvp.md#community-acceptance) đạt từ smoke nền này. Các proof chưa có nằm tại [bảng đối chiếu](delivery/verification.md#unverified).

<a id="migration"></a>

## Nâng cấp DB đã có dữ liệu

Chọn connection string riêng cho DB cần nâng cấp, backup DB/keys và drain writer cũ trước khi áp migration còn thiếu. Đọc ledger `common.schema_migrations` và README module; không sửa SQL/checksum đã áp. Runner ở `tools/SCDC.DbMigrator` đã có trên `main`. Ví dụ chuỗi cho baseline legacy chưa áp 001–004:

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

Đường dẫn mapping là file đã review, cần thay trước khi chạy; không phải file có sẵn trong repo. Server map khai báo visibility/joinMode theo từng ID. Channel map khai báo mọi ID legacy với kind `text`/`voice` và defaultView `allow`/`deny`; không tự suy từ visibility cũ. DB không có đối tượng legacy tương ứng thì bỏ argument map. Preflight từ chối thiếu/thừa mapping, collision hoặc trạng thái không hỗ trợ; sửa dữ liệu bằng thay đổi đã review rồi chạy lại, không tự repair bằng bootstrap. Chỉ deploy writer mới sau khi ledger/migration hợp lệ. Các lệnh này không cấu thành quy trình phát hành production đã được duyệt.

## Kết thúc lần thử

Dừng API/Vite bằng Ctrl+C. Container ở bộ chạy này dùng `--rm`, không mount volume DB cần giữ; dừng sẽ bỏ DB tổng hợp của lần thử:

```bash
"$SCDC_CONTAINER_ENGINE" stop scdc-community-docs-db
```

Dọn riêng thư mục keyring thử đã sinh bằng `mktemp` nếu không cần restart/đối soát lần thử này nữa. Giữ TRX/trace/ảnh cần review trong `artifacts/community-local/`; không commit DB/key/token. Kiểm tra tài liệu từ root repo:

```bash
python3 scripts/check_docs.py
```

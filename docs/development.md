# SCDC — Hướng dẫn phát triển

Cập nhật: 2026-10-05. Hướng dẫn thực hành theo source hiện tại. Vai trò, lịch và ngân sách được quản lý tại [kế hoạch dự án](project/planning.md).

Các lệnh dưới đây chạy từ root repo trừ khi có ghi thư mục khác. Cấu hình và dữ liệu mẫu dành cho local Development.

## Mục lục

- [Chuẩn bị và khởi chạy](#setup)
- [Docker Desktop, Docker Engine và Podman](#container-engines)
- [Lệnh ngắn và đồng bộ docs giữa các nhánh](#short-commands)
- [Cấu hình](#configuration)
- [Tài khoản để tích hợp](#identity)
- [Database và dữ liệu mẫu](#database)
- [Quy ước code và Git](#conventions)
- [Kiểm thử](#testing)
- [Xử lý lỗi thường gặp](#troubleshooting)

<a id="setup"></a>

## 1. Chuẩn bị và khởi chạy

Chuẩn bị Docker hoặc Podman với Compose để chạy stack. Nếu debug local, cần .NET SDK 10 và Node.js 24/npm (cùng major với Dockerfile frontend). Repo dùng `SCDC.slnx`, backend .NET 10, frontend React 19/Vite.

### Chạy bằng Compose

```bash
docker compose up -d --build
```

| Dịch vụ | Địa chỉ |
|---|---|
| WebClient | `http://localhost:3000` |
| Backend / Swagger | `http://localhost:5026/swagger` |
| OpenAPI JSON (Development) | `http://localhost:5026/swagger/v1/swagger.json` |
| Health | `http://localhost:5026/api/v1/health` |
| PostgreSQL | `localhost:5432` |

Compose khởi tạo PostgreSQL, API và web. Identity gọi backend thật; giao diện chat/cộng đồng chưa có backend nghiệp vụ.

<a id="container-engines"></a>

### Docker Desktop, Docker Engine và Podman

Cả ba môi trường dùng chung [compose.yaml](../compose.yaml) và Dockerfile. Makefile chọn Docker mặc định; dùng `ENGINE=podman` để chọn Podman. Chọn engine rõ ràng khi máy cài cả hai.

| Môi trường | Chuẩn bị | Lệnh khởi chạy qua Make |
|---|---|---|
| Docker Desktop trên Windows | Mở Docker Desktop, dùng Linux containers; để chạy Make trong WSL, bật WSL integration cho distro và cài Make/Python/Git trong distro | `make up` |
| Docker Engine với CLI | Engine đang chạy, CLI kết nối được và đã cài Compose plugin | `make up` |
| Podman | Podman hoạt động và có Compose provider; Windows/macOS cần Podman machine đang chạy | `make up ENGINE=podman` |

[Docker Desktop](https://docs.docker.com/compose/install/) đã bao gồm Engine, CLI và Compose. CLI cần kết nối được một engine đang chạy. [`podman compose`](https://docs.podman.io/en/latest/markdown/podman-compose.1.html) gọi Compose provider bên ngoài như `podman-compose` hoặc `docker-compose`; chọn provider qua `PODMAN_COMPOSE_PROVIDER` nếu cần. Với [Podman machine](https://docs.podman.io/en/latest/markdown/podman-machine.1.html), tạo bằng `podman machine init` nếu chưa có, rồi `podman machine start`.

Kiểm tra cấu hình trước khi khởi chạy và dùng cùng engine cho mọi thao tác:

```bash
make compose-check ENGINE=podman
make up ENGINE=podman
make status ENGINE=podman
make logs ENGINE=podman
make down ENGINE=podman
```

Có thể đặt `export ENGINE=podman` trong shell để các lệnh `make up`, `make down` dùng Podman mặc định. Nếu gọi trực tiếp provider, dùng `make up COMPOSE="podman-compose"`; cùng override áp dụng cho các tác vụ container còn lại. `compose-check` chỉ kiểm tra cấu hình qua provider; vẫn cần chạy stack để xác nhận build, healthcheck và thứ tự khởi động trên môi trường thực tế. Docker và Podman quản lý volume riêng; đổi engine không chuyển dữ liệu PostgreSQL.

Tên image trong Compose/Dockerfile chỉ rõ registry (`docker.io/library/...` và `mcr.microsoft.com/...`) để tránh Podman phải phân giải tên rút gọn theo cấu hình từng máy; xem [quy tắc tên image của Podman](https://docs.podman.io/en/latest/markdown/podman-pull.1.html).

**Windows dùng PowerShell:** Makefile hiện dùng shell POSIX cho `help`, nên chạy qua WSL nếu muốn dùng `make`. Nếu không dùng WSL, có thể gọi trực tiếp CLI và script từ root repo; Python/Git phải có trên Windows:

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

### Debug backend local

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

### Frontend local — terminal khác

```bash
cd clients/WebClient
npm ci
npm run dev
```

Vite nghe ở cổng 3000 và proxy `/api`, `/hubs`, `/swagger` về API 5026. Nếu web-client Compose đang chiếm cổng 3000, dừng riêng service đó trước khi chạy Vite.

<a id="short-commands"></a>

### Lệnh ngắn và đồng bộ docs giữa các nhánh

[Makefile](../Makefile) gom các lệnh thường dùng; chạy từ root repo. Dùng `make help` hoặc chỉ `make` để xem danh sách. Công cụ đồng bộ cần `make`, Python 3 và Git hỗ trợ `switch`/`restore` cùng `rev-parse --end-of-options`. Các lệnh khác dùng dependency backend/frontend/container đã nêu ở phần chuẩn bị.

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
| `make docs-check` | Kiểm tra link và anchor tài liệu |
| `make test-tools` | Kiểm thử công cụ đồng bộ trong các repo Git tạm |

Đồng bộ tài liệu từ một nhánh nguồn vào nhánh đang làm:

```bash
make docs-sync-preview FROM=main
make docs-sync FROM=main
```

Hoặc chỉ định nhánh đích local đã tồn tại:

```bash
make docs-sync-preview FROM=main TO=feat/identity
make docs-sync FROM=main TO=feat/identity
```

`FROM` mặc định là `main`; `TO` mặc định là nhánh hiện tại. Lệnh preview so sánh bản đã commit của hai nhánh và liệt kê file thêm/sửa/xóa. Khi thực hiện, công cụ chuyển sang `TO` nếu cần, chép toàn bộ `docs/` (gồm archive) từ commit nguồn rồi đưa thay đổi vào staging. File tracked chỉ có ở đích sẽ bị xóa để khớp nguồn; đây là chép snapshot, không hòa trộn nội dung hai nhánh. Phạm vi chép là `docs/`; README ở root và code không được lấy từ nguồn.

Những thay đổi chưa commit ở nguồn chưa được đồng bộ. Khi dùng `origin/main`, chạy `git fetch origin` trước để cập nhật ref remote; công cụ dùng ref đã có ở local. Công cụ dừng khi docs có thay đổi/file mới/ignored, khi đang merge/rebase hoặc khi working tree có thay đổi trước việc chuyển nhánh. Nếu đang giữ nguyên nhánh, thay đổi code ngoài docs được giữ nguyên. Khi chuyển nhánh, code hiện ra là code vốn có của nhánh đích.

Sau đồng bộ, bạn đang ở nhánh đích và có thể review/lưu thay đổi:

```bash
git diff --cached -- docs/
git commit -m "docs: sync documentation from main"
git push
```

Commit theo phạm vi staging đã review; nếu trước đó có code staged thì commit sẽ bao gồm cả code đó. Công cụ không tự stash, commit hoặc push. Trước khi dùng `TO`, commit bộ công cụ; các lệnh `make` khả dụng trên những nhánh đã nhận Makefile và script. Có thể gọi trực tiếp `python3 scripts/sync_docs.py --source main --target feat/identity --dry-run` khi cần; cách mở rộng lệnh ngắn là thêm target và recipe trong Makefile.

Khi đổi bố cục tài liệu, cập nhật liên kết trong `README.md` ở root và README module của nhánh nhận. `docs-sync` chỉ chép `docs/`; README ngoài thư mục này giữ hướng dẫn runtime theo nhánh và được cập nhật liên kết riêng. Sau chuyển đổi, chạy `make docs-check` để kiểm tra cả tài liệu và README liên quan.

<a id="configuration"></a>

## 2. Cấu hình

Backend đọc `appsettings.json`; launch profile `http` bật Development và nạp `appsettings.Development.json`. Compose truyền cấu hình qua biến môi trường.

| Khóa | Giá trị local / vai trò |
|---|---|
| `ConnectionStrings:Database` | `Host=localhost;Port=5432;Database=scdc_chat;Username=scdc;Password=scdc_dev` khi API chạy local; host `postgres` trong Compose |
| `Modules:Identity:Issuer` / `Audience` | `SCDC` / `SCDC.WebClient` |
| `Modules:Identity:SigningKey` | Khóa local có trong cấu hình Development; cấu hình ngoài Development cần khóa riêng đủ dài |
| `Modules:Identity:ExposeDevelopmentTokens` | `true` trong Development, dùng token trả về để thử xác minh/reset; giá trị mặc định `false` |
| `Cors:AllowedOrigins` | Local hiện có `http://localhost:3000`, `http://localhost:5173`; Vite của repo mặc định 3000 |

Các thời hạn token/phiên và lockout hiện tại nằm trong [đặc tả Accounts](features/accounts/design/README.md#api-current). Khi override bằng environment variable, dùng `__` thay dấu `:`, ví dụ `ConnectionStrings__Database`.

Nguồn: [appsettings.json](../services/SCDC.Api/appsettings.json), [cấu hình Development](../services/SCDC.Api/appsettings.Development.json), [Vite config](../clients/WebClient/vite.config.js).

<a id="identity"></a>

## 3. Tạo tài khoản để tích hợp

1. Đăng ký qua form hoặc `POST /api/v1/auth/register` với `username`, `displayName`, `email`, `password`.
2. Trong Development, lấy `developmentVerificationToken` từ response đăng ký và gửi `POST /api/v1/auth/verify-email` với `{ "token": "..." }`.
3. Đăng nhập bằng email hoặc username. Dùng `accessToken` làm Bearer token khi gọi endpoint yêu cầu xác thực.
4. Quên mật khẩu dùng `POST /api/v1/auth/forgot-password`; response Development có thể chứa `developmentResetToken`. Dùng token đó cùng `newPassword` ở endpoint reset.

Token Development chỉ phục vụ kiểm thử local; chúng không thuộc trải nghiệm sản phẩm. Hiện chưa có endpoint gửi lại xác minh và chưa có worker gửi email. Quy trình gửi email thật cần hoàn thiện trước phát hành; xem [Accounts](features/accounts/design/README.md#gaps).

Frontend quản lý phiên ở [api.js](../clients/WebClient/src/api.js). Khi có Web Locks, nó đồng bộ phiên giữa tab và khóa refresh; khi thiếu Web Locks, token giữ trong tab. Wrapper hiện có thử lại sau 401; khi tích hợp gửi tin phải xử lý rõ theo DEC-021 để không tự gửi lại mutation ngoài thao tác người dùng.

<a id="database"></a>

## 4. Database và dữ liệu mẫu

Database local: `scdc_chat`, user `scdc`, password `scdc_dev`, port 5432. SSL tắt cho cấu hình local. Các schema hiện có được mô tả trong [kiến trúc](architecture.md#boundaries).

`schema.sql` tạo schema, constraint, index, trigger và view. `seed.sql` có dữ liệu mẫu cho nhiều domain; password/token mẫu chỉ minh họa, không dùng đăng nhập. Tạo tài khoản mới bằng API để thử luồng Identity.

Compose mount hai script vào `/docker-entrypoint-initdb.d/`; chúng chỉ chạy khi khởi tạo database trên volume mới. Sửa SQL không tự cập nhật volume đang có dữ liệu. Chưa có quy trình migration production được hoàn thiện; không dùng script tạo lại schema như migration cho môi trường chứa dữ liệu cần giữ.

Các view để quan sát: `identity.v_user_accounts`, `identity.v_active_sessions`, `messaging.v_space_overview`, `messaging.v_message_timeline`. Ví dụ:

```sql
SELECT * FROM identity.v_user_accounts ORDER BY username;
SELECT * FROM identity.v_active_sessions;
SELECT * FROM messaging.v_space_overview ORDER BY last_activity_at DESC;
SELECT * FROM integration.outbox_events ORDER BY occurred_at;
```

Schema/seed chứa cấu trúc cho cả tính năng chưa triển khai. Dữ liệu thử chức năng, tình huống mất mạng và đồng thời được ghi trong từng đặc tả; dùng database Development riêng khi chạy test tích hợp.

<a id="conventions"></a>

## 5. Quy ước code và Git

- Nghiệp vụ đặt tại `services/Modules/<Module>`; hợp đồng liên module nằm trong `SCDC.Contracts`.
- Mỗi module sở hữu dữ liệu riêng. Không tham chiếu implementation hoặc truy vấn trực tiếp dữ liệu module khác.
- Lỗi nghiệp vụ trả `Result` / `Result<T>` tại `SCDC.BuildingBlocks`; dùng `Success` hoặc `Failure`, không ném exception cho lỗi dự kiến.
- Controllers ánh xạ lỗi theo [hợp đồng lỗi chung](architecture.md#contracts). JSON lỗi phải khớp schema và hành vi frontend.
- Nhánh code: `feat/<module>-<gói-chức-năng>`, `fix/<tên-lỗi>`; nhánh ổn định `main`. Mỗi nhánh phục vụ một gói có đầu ra kiểm chứng được, gồm code, migration, kiểm thử và tài liệu liên quan. Tài liệu độc lập có thể commit trực tiếp trên `main` theo thỏa thuận làm việc hiện tại.
- Commit: `<loại>(<phạm vi>): <mô tả>`, dùng `feat`, `fix`, `refactor`, `perf`, `test`, `docs`.
- Khi hoàn thành một phần có thể review, kiểm tra rồi commit và push lên nhánh tương ứng. Merge nhánh code vào `main` sau review/kiểm chứng; quyền commit/push không tự cấp quyền merge.

Khi review thay đổi, đối chiếu requirement và AC bị ảnh hưởng, quyền/API/dữ liệu, ngoại lệ, test phù hợp và docs hiện hành. Thay đổi phạm vi hoặc quyết định cần cập nhật [sổ quyết định](decisions.md) và các tài liệu phụ thuộc. Không chỉ sửa bản archive.

<a id="testing"></a>

## 6. Kiểm thử

### Backend

```bash
dotnet test SCDC.slnx --configuration Release
```

`IdentityV1FlowTests` và `IdentityConcurrencyTests` cần PostgreSQL Development với schema repo. Các nhóm test hiện có: vòng đời tài khoản/phiên, xử lý đồng thời, response/ProblemDetails và Result. Factory dùng môi trường Development, cấu hình DB local và token thử.

### Frontend — trong clients/WebClient

```bash
npm test
npm run build
```

Test hiện có kiểm tra wrapper API và quản lý phiên; build kiểm tra đóng gói frontend. Không coi chúng là bằng chứng toàn bộ hành trình chat/cộng đồng/media đã chạy.

### Tài liệu

```bash
python3 scripts/check_docs.py
```

Lệnh kiểm tra link local và anchor trong docs hiện hành, README repo và README kỹ thuật; archive được giữ nguyên như bản lịch sử. Bộ ca TC/AC trong đặc tả vẫn cần ghi build, môi trường, dữ liệu, thực tế và bằng chứng mỗi lần chạy. Quy trình nghiệm thu ở [phát hành và vận hành](release-operations.md#testing).

Ghi kết quả bằng [mẫu hồ sơ](templates/release-record.md); quyết định phát hành/lỗi tồn theo DEC-110/111 và [các gate](release-operations.md#release-gates). [Runbook](operations-runbook.md) đối chiếu compose/health/config hiện có với đầu vào production cần bổ sung; các lệnh local trong file này không thay manifest production đã diễn tập.

Hiện chưa có kịch bản k6 trong repo. Ngưỡng chat/media, browser và backup đã chốt DEC-082/083/085/086; baseline workload/phương pháp đo ở [mục tiêu chất lượng](release-operations.md#quality-targets). Khóa build/cấu hình/dataset/thiết bị trước viết và chạy test tải; fixture JSON tại `docs/fixtures` là dữ liệu biên, không phải kết quả product test. Kiểm tra tài liệu không tạo bằng chứng nghiệm thu phần mềm.

<a id="troubleshooting"></a>

## 7. Xử lý lỗi thường gặp

| Hiện tượng | Kiểm tra / xử lý |
|---|---|
| API không khởi động | Kiểm tra connection string, signing key, môi trường và log; khởi động PostgreSQL trước |
| Trùng cổng 3000/5026/5432 | Kiểm tra service Compose và tiến trình local, dừng thành phần trùng trước khi chạy |
| Login tài khoản seed thất bại | Seed không chứa password đăng nhập thực; tạo và xác minh tài khoản qua API |
| Login trả 403 | Xem `errorCode`; chưa xác minh cần hoàn tất verify, không tự coi mọi 403 là phiên hết hạn |
| Không thấy Swagger | Swagger chỉ bật khi môi trường Development |
| Sửa SQL nhưng dữ liệu không đổi | Script init không chạy lại trên volume đã khởi tạo; xác định cách cập nhật dữ liệu trước khi thực hiện |
| Chat UI hiện dữ liệu nhưng không gọi được DM API | DM backend chưa triển khai; dùng đặc tả đề xuất để làm tích hợp tiếp theo |

Quan sát container local:

```bash
docker compose ps
docker compose logs chat-service postgres
```

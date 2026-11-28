# Compose và các bước vận hành

[`compose.yaml`](../compose.yaml) mô tả chín thành phần chạy thường trực:
WebClient/Nginx, Identity, Community, Messaging (`chat-service`), hai PostgreSQL,
Redis, OpenTelemetry Collector và OpenObserve. File này dùng image; không chứa
job tạo khóa, migration, gate chuyển dữ liệu hoặc service mẫu.

## Các file cấu hình

| File | Mục đích |
|---|---|
| [`compose.yaml`](../compose.yaml) | Runtime, network mặc định, volume bền, secret và healthcheck |
| [`compose.dev.yaml`](../compose.dev.yaml) | Build từ nguồn, Development, token thử và cổng debug local |
| [`compose.prepare.yaml`](compose/compose.prepare.yaml) | Công cụ chạy một lần; script gọi đích danh bằng `run --rm --no-deps` |
| [`compose.sample.yaml`](compose/compose.sample.yaml) | Service mẫu và đường health/JWT demo, bật bằng `SAMPLE=1` |
| [`compose.email.yaml`](compose/compose.email.yaml) | Cấu hình SMTP tùy chọn cho worker Identity |
| [`compose.community-preview.yaml`](compose/compose.community-preview.yaml) | Stack preview riêng |
| [`compose.community-rollback.yaml`](compose/compose.community-rollback.yaml) | Cấu hình rollback, chỉ dùng sau rollback dữ liệu được kiểm chứng |
| [`tests/compose/`](../tests/compose/) | Stack kiểm thử riêng, có dữ liệu và project riêng |

File bổ sung ghép bằng `-f` dùng đường dẫn tương đối từ file gốc `compose.yaml`.
Các stack kiểm thử/preview chạy độc lập giữ quy tắc đường dẫn riêng của chúng.
Không có file override được tự động nạp: Make và script chọn file một cách tường minh.

## Khởi chạy và cập nhật

```bash
make setup                 # Lần đầu: init, build, chuẩn bị, migrate, kiểm tra, chạy
make down                  # Dừng, giữ volume và khóa
make up                    # Kiểm tra rồi chạy lại, không build hoặc migrate
make build                 # Build phiên bản nguồn hiện tại
make migrate               # Kiểm cutover, nâng cấp DB; để ứng dụng ở trạng thái dừng
make up
make check
```

Make và script mặc định dùng Podman. Thêm `ENGINE=docker` cho mọi lệnh Make
hoặc đặt biến môi trường `ENGINE=docker` khi gọi script trực tiếp để dùng Docker.
[`setup.sh`](setup.sh), [`migrate.sh`](migrate.sh), [`preflight.sh`](preflight.sh)
gọi chung bộ điều phối [`compose.py`](compose.py); Make chuẩn bị `.env` và các
file secret trước khi gọi script. `make preflight` chỉ kiểm trạng thái cutover
và checksum migration email đã áp; yêu cầu hạ tầng đang chạy, không thay đổi
cấu hình container, chạy migration hoặc bật ứng dụng.

Luồng setup: build image → dừng các writer → bật PostgreSQL/Redis và chờ sẵn
sàng → tạo khóa nếu chưa có → chuẩn bị Community DB → kiểm gate Identity và
Community → áp migration email → preflight → bật runtime → kiểm health.

Luồng up: dừng các writer → bật hạ tầng và chờ sẵn sàng → preflight → bật
runtime. Cập nhật đang theo mô hình offline; lệnh có thể ngắt HTTP và SignalR
trong lúc kiểm tra. Gate hoặc migration lỗi giữ toàn bộ writer ở trạng thái dừng.
Hạ tầng được giữ để đối chiếu và khắc phục.

**Dùng Make/script làm lối khởi động ứng dụng.** `docker compose up` trực tiếp
chỉ chạy cấu hình runtime, không thực hiện preflight. Chuyển file không thay
thế các kiểm tra nguồn đã khóa, lineage của dữ liệu đích và checksum migration.
`make up` từ chối schema email chưa nâng cấp; dùng `make migrate` trước.
Hiện `make migrate` áp migration email của Identity; chuyển dữ liệu và các
migration khác vẫn thực hiện theo runbook của owner.
Các API chỉ nhận credential runtime; credential quản trị chỉ cấp cho PostgreSQL
và công cụ cần thiết. Không tự chuyển dữ liệu MVP đang có.

Volume và project mặc định giữ nguyên tên `scdc`, nên stack hiện có tiếp tục dùng
dữ liệu và khóa cũ. Giữ cùng `COMPOSE_PROJECT_NAME` hoặc `COMPOSE_ARGS="-p ..."`
cho mọi lệnh nếu đã đặt tên khác. `make down` không xóa volume. Công cụ chạy
xong tự xóa container; `make status` hiển thị runtime.

## Tùy chọn

```bash
make setup SAMPLE=1        # Bật demo S1-03/S1-06 và health qua service mẫu
make up SAMPLE=1           # Giữ cùng tùy chọn ở các lần khởi chạy sau
make up DEV=0              # Chạy image đã chuẩn bị, không nạp cấu hình Development
make up COMPOSE_ARGS="-f ops/compose/compose.email.yaml"
```

Runtime dùng `${SCDC_IMAGE_PREFIX:-scdc}-<service>:${SCDC_IMAGE_TAG:-latest}`.
Build local và các công cụ dùng cùng image/tag. `DEV=0` yêu cầu image đã có;
cấu hình mặc định vẫn phục vụ phát triển/demo, chưa phải manifest production
đã được nghiệm thu. Ngoài Development, Identity cần origin HTTPS và certificate
bảo vệ key ring email theo [hướng dẫn Identity](../services/Identity/README.md).
`setup` chỉ build ở chế độ `DEV=1`.

`/healthz` kiểm Nginx; `/health` mặc định chuyển tới liveness Messaging. Khi
`SAMPLE=1`, `/health` chuyển sang service mẫu và `/sample/protected` bật để
demo JWT. Bình thường `/sample/protected` trả 404. Health của từng owner giữ
các đường hiện có; liveness không thay readiness database/phiên/quyền.

`ENV_FILE` chọn file môi trường; `COMPOSE_ARGS` nhận tùy chọn project và file
bổ sung. `SCDC_WEB_ORIGIN` và `SCDC_OBSERVE_ORIGIN` chọn địa chỉ cho
`make check` nếu đã đổi cổng. `SCDC_STARTUP_TIMEOUT` là thời gian chờ hạ tầng,
mặc định 120 giây. Các port debug chỉ được công bố khi nạp `compose.dev.yaml`.

## Rollback Community

Thực hiện [runbook dữ liệu](../database/postgres/community/README.md) khi writer
đã dừng. Chỉ sau khi `community-transfer --rollback-before-writes` thành công:

```bash
SCDC_COMMUNITY_ROLLBACK_IMAGE=<image-cu-da-kiem-chung> make up ROLLBACK=1
```

Lệnh dùng gate Community với `--rollback`, giữ target Community tắt, đổi
gateway/callback về host ghép cũ và không build lại image rollback. Giữ
`ROLLBACK=1` cho các lệnh quản lý stack đó. Readiness Community qua gateway
chuyển sang `/api/v1/health/access` của host ghép cũ; Swagger của host cũ ở
`/swagger`, không có trang Swagger Community riêng. `SCDC_COMMUNITY_SOURCE_DATABASE`
chọn database nguồn khi khác tên mặc định `scdc_chat`. `setup` và `migrate` từ chối chế độ
rollback. Gate chặn rollback sau ghi vẫn giữ nguyên; cấu hình Compose không
tự phục hồi hoặc chuyển dữ liệu.

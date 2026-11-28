# Database Identity — S3-03

Identity dùng PostgreSQL 18 riêng (`identity-postgres`), database `scdc_identity`,
admin `scdc_identity_admin` và runtime `scdc_identity`. Instance `postgres` cũ
tiếp tục phục vụ Community/Messaging. Consumer không nhận connection Identity.

`init.sh` chỉ chạy trên volume mới: cài `pg_trgm`, schema owner Identity,
baseline `2026112503` với SHA-256 của `schema.sql`, role và grant. Mật khẩu admin
và runtime được cấp một lần ngoài repo qua
[provisioner](../../../scripts/init_identity_database_secrets.py) và Compose
secrets. Identity đọc `ConnectionStrings:IdentityPasswordFile`; không đồng thời
đặt inline password. `make init` giữ nguyên credential đang có.

`schema.sql` là schema-only dump đã rà từ baseline S2-11, không có dòng người
dùng. `scdc_migrations.service_baselines` ghi baseline service mới;
`identity.schema_migrations` rỗng trên volume mới. Khi chuyển từ MVP, ledger
nguồn được giữ nguyên làm lineage; công cụ ghi thêm transfer record và baseline
đích, không giả vờ các migration MVP đã chạy lại ở đây.

## Chuyển offline từ MVP

Dừng gateway, API, worker/job đang ghi, đợi transaction/pool đóng, backup DB,
Redis và toàn bộ khóa/cấu hình. Đích phải là baseline Identity trên instance
khác, chưa có dữ liệu nghiệp vụ. Không chạy `schema.sql` hoặc entrypoint lên
volume đang có dữ liệu.

Chuẩn bị hai biến môi trường **admin**, lấy credential từ cấu hình private:
`ConnectionStrings__TransferSource`, `ConnectionStrings__TransferTarget`.
Các connection này dùng host/port truy cập được từ CLI; container arguments
phải trỏ đúng instance/database đó. Không truyền mật khẩu trong CLI argv.

```bash
dotnet build tools/SCDC.DbMigrator -c Release
dotnet tools/SCDC.DbMigrator/bin/Release/net10.0/SCDC.DbMigrator.dll identity-transfer \
  --engine podman --source-container scdc-postgres-1 \
  --target-container scdc-identity-postgres-1 \
  --artifact-directory artifacts/identity-cutover-<lan-chay-moi> --quiesced
```

Lệnh chạy từ gốc repo. Công cụ kiểm PostgreSQL 18, system identifier/OID/DB name
qua cả Npgsql và container, checksum S2-03/S2-06 nguồn, baseline đích, role,
client còn mở và khóa chạy đồng thời. `--quiesced` không thay việc dừng writer.

Nguồn chuyển sang `NOLOGIN`, các bảng Identity được khóa SHARE trong snapshot;
đích cũng `NOLOGIN` khi chuyển. Dump chỉ schema `identity`, restore transaction
đơn, giữ helper/constraint/index/view, mọi bảng, giá trị/ID/security stamp,
timestamp/version/epoch, audit/outbox/inbox, journal/ACK/lease/publication và
sequence `last_value/is_called`. Manifest SHA-256 từng bảng dùng toàn bộ hàng
JSON sắp xếp ổn định theo UTC; schema/sequence được đối chiếu riêng. Chỉ chuẩn
hóa hai biểu thức CHECK email/UTF-16 legacy đã rà, do PG18 gộp AND tương đương
khi restore. Tên NOT NULL được lấy từ catalog nguồn và giữ lại ở đích, kể cả
khi ledger legacy đổi tên làm tên ngầm định của PG18 bị thêm hậu tố.

Đích chỉ được xóa schema baseline **rỗng đã kiểm tra** trước restore. Target có
dữ liệu mà không có transfer record khớp bị từ chối. Sau đối chiếu, grant và
record `ready` cùng `LOGIN` đích được commit; nguồn vẫn `NOLOGIN`. Runtime không
đọc/sửa ledger hoặc metadata, không sửa/xóa audit/ACK, không xóa scope/journal.
Source schema và dữ liệu được giữ nguyên.

Chạy lại với artifact directory mới và nguồn khớp trả no-op; không ghi đè dữ
liệu mới ở đích. Sau restore bị ngắt, đích tiếp tục `NOLOGIN`; chỉ retry tự động
nếu đích rỗng hoặc manifest đã restore khớp chính xác nguồn đã ghi nhận. Nguồn
đổi, đích khác hoặc container sai đều bị từ chối. Archive/diagnostic/manifest
nằm trong directory 0700, file 0600; không đưa dump hoặc credential lên Git/CI.

## Phục hồi

Dừng và drain cả hai phía. Khi đích còn đúng manifest đã chuyển, thêm
`--rollback-before-writes` vào cùng lệnh với artifact directory mới. Công cụ
đối chiếu lại, khóa runtime đích và mở lại runtime nguồn. Nếu tiếp tục cutover
ngay khi nguồn chưa đổi, lệnh bình thường xác minh lại rồi mở đích, không copy
đè. Nếu nguồn đã nhận ghi mới, phải lập đích mới hoặc đối chiếu có review.

Sau khi đích đã ghi **hoặc Identity startup đã đổi generation/sequence**,
rollback này bị từ chối; nguồn giữ `NOLOGIN`. Ưu tiên sửa tiến trên đích. Quay
lại nguồn sau ghi cần kế hoạch chuyển chênh lệch cho DB/Redis/fence/epoch/khóa,
không chỉ đổi connection string. Nếu lỗi xảy ra trước khi có transfer record,
giữ writer đóng và rà private evidence; không tự mở nguồn chỉ từ exit code.

Kiểm thử trên hai instance thử, mỗi container có `POSTGRES_DB` kết thúc `_test`:

```bash
python3 scripts/test_identity_transfer.py --engine podman \
  --source-container <pg-nguon-thu> --source-port <cong-host-nguon> \
  --target-container <pg-dich-thu> --target-port <cong-host-dich>
```

Script chỉ tạo/xóa database fixture riêng, không chạm database ứng dụng.
CI chạy cùng 14 kiểm tra bằng hai PostgreSQL service riêng. CLI migration SQL
cũ vẫn dành cho monolith; không chạy các migration đa owner đó trên database
Identity v1. Nâng baseline service sau này cần migration owner được review.

Nguồn: [D03 và D48–D52](../../../docs/system/database.md),
[checklist](../../../docs/guides/operations.md#service-extraction).

<a id="email-upgrade"></a>

## Nâng cấp email — E4-01

Giữ nguyên baseline `2026112503` và schema chuyển dữ liệu S3-03. Migration
`20261126_e4-01_email_upgrade.sql` (v2, `2026112602`) chạy sau bootstrap/transfer,
tạo mới hoặc nhận diện hai bảng policy/delivery có đúng schema đã rà. Migration
v1 `20261126_e4-01_email_worker.sql` giữ nguyên checksum và vẫn được hỗ trợ.
V2 giữ delivery cũ, hợp nhất policy mà không lùi cooldown và không giả lập
ledger v1 khi chỉ nhận diện bảng legacy.
Token legacy chỉ còn hash không thể dựng lại email; yêu cầu gửi lại sẽ tạo
một delivery mới.

Dừng và drain Identity trước khi nâng cấp; giữ backup database và key ring.
Cấp `ConnectionStrings__Migration` bằng credential admin tới database Identity,
rồi chạy từ gốc repo:

```bash
dotnet build tools/SCDC.DbMigrator -c Release
dotnet tools/SCDC.DbMigrator/bin/Release/net10.0/SCDC.DbMigrator.dll \
  database/postgres/identity/20261126_e4-01_email_upgrade.sql
```

Công cụ dùng transaction, advisory lock có giới hạn chờ và ledger checksum.
Chạy lại cùng checksum là no-op; checksum khác hoặc writer cũ còn kết nối bị
từ chối. Runtime chỉ nhận DML trên hai bảng mới, vẫn không sửa ledger/audit/ACK.
Readiness Identity kiểm tra hai bảng email. Compose có job tương đương
`identity-email-migration` trong `ops/compose/compose.prepare.yaml`; `make setup` /
`make migrate` áp migration, `make up` chỉ kiểm checksum bằng `--check`. Chỉ công cụ này nhận admin secret, API và worker dùng
runtime credential.

Kiểm chứng job trên container thử có POSTGRES_DB kết thúc `_test`, cấp
`SCDC_EMAIL_TEST_ADMIN_PASSWORD` qua môi trường:

```bash
python3 scripts/test_identity_email_upgrade.py --engine podman --container <pg-thu>
```

Script tạo/xóa database fixture riêng, kiểm tra nâng mới, chạy lại và từ chối
checksum khác. [Hồ sơ E4-01](../../../docs/records/verification/authentication/e4-01-2026-11-26.md).

Schema legacy khác cột/ràng buộc đã rà sẽ bị từ chối. Envelope legacy còn hạn
chưa có ledger v1 cần kế hoạch chuyển key/envelope riêng; migration không
tự hủy thư đang chờ. [Kiểm chứng nhận diện legacy](../../../docs/records/verification/authentication/e4-01-legacy-adoption-2026-11-26.md).

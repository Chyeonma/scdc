# Database Community — S3-10

Dev dùng database `scdc_community` trên cùng instance PostgreSQL với Messaging (`scdc_chat`). Runtime mới là `scdc_community_service`; runtime nguồn `scdc_community` giữ `NOLOGIN`. Tách login giúp khóa nguồn mà không khóa đích trên cùng cluster. Schema nguồn không bị xóa.

| File | Vai trò |
|---|---|
| [schema.sql](schema.sql), [baseline.sql](baseline.sql) | Baseline sạch 21 bảng, sáu permission và lineage tám migration; không dùng để nâng volume có dữ liệu |
| [runtime-grants.sql](runtime-grants.sql) | Grant owner, giữ ledger/lineage riêng và hạn chế fence/receipt/journal; job thay tên role sang runtime service |
| [service-baseline.sql](service-baseline.sql) | Metadata admin phiên bản `2026112610`, checksum và transfer record; runtime không truy cập |
| [bootstrap-target.sh](bootstrap-target.sh) | Tạo đích rỗng/login mới từ secret; không chép dữ liệu cũ hoặc mở login đích |
| [init-source.sh](init-source.sh) | Entrypoint chỉ trên volume mới: chép seed Community sang DB riêng, khóa login nguồn; từ chối đích đã tồn tại |
| [cutover-gate.sh](cutover-gate.sh) | Job chỉ đọc, xác nhận nguồn đóng và đích đúng lineage; chế độ rollback yêu cầu record `rolled_back` |
| [init.sh](init.sh) | Giữ initializer preview độc lập ở bước 1 |

## Chuyển offline dữ liệu đang dùng

1. Chạy `make init` để cấp credential Community riêng và nhận lại HMAC hiện hữu từ `.env`. Không tạo lại `.env`, đổi HMAC hoặc keyring. Script chứng chỉ `--add-community` dùng CA cũ, giữ nguyên chứng chỉ/private key Identity và Messaging.
2. Dừng gateway, Identity, API/worker Community/Messaging, drain mọi connection của các database và hai runtime Community. Chụp snapshot DB, roles, Redis, cấu hình và keyring vào nơi private. Không dùng `down --volumes`.
3. Job `community-database-init` tạo đích rỗng đã rà. Đích có dữ liệu hoặc metadata không khớp phải dừng; không chạy `schema.sql` hay initializer nguồn bằng tay để sửa.
4. Đặt `ConnectionStrings__TransferSource`, `ConnectionStrings__TransferTarget` là connection admin từ cấu hình private. Nguồn `scdc_chat`, đích `scdc_community` cùng host/port. Không đưa mật khẩu vào argv hoặc log.

```bash
dotnet build tools/SCDC.DbMigrator -c Release
dotnet tools/SCDC.DbMigrator/bin/Release/net10.0/SCDC.DbMigrator.dll community-transfer \
  --engine podman --source-container scdc-postgres-1 \
  --target-container scdc-postgres-1 \
  --artifact-directory artifacts/community-cutover-<lan-moi> --quiesced
```

Công cụ kiểm instance/OID/database qua Npgsql và PostgreSQL trong container, checksum nguồn/đích, role và connection còn mở. Hai database phải khác nhau; cùng instance được hỗ trợ. Source runtime chuyển `NOLOGIN`; snapshot giữ SHARE lock. Dump chỉ schema Community, restore transaction đơn, so sánh SHA-256 mọi hàng, toàn bộ DDL và sequence/`is_called`. Giữ ID, timestamp, fingerprint/key ID, version/epoch, lease, ACK, tombstone và công việc pending. Hai biểu thức CHECK catalog varchar/text tương đương của PG18 được chuẩn hóa đúng mẫu; tên NOT NULL lấy trực tiếp từ catalog nguồn.

Chỉ sau đối chiếu mới commit grant, record `ready` và mở login đích. Lỗi giữ writer đóng; không tự mở nguồn từ exit code. Chạy lại với artifact directory mới không chép đè đích đã ready, kể cả đích đã ghi mới. Đích/source bị đổi hoặc bản restore dở không khớp thì dừng. Dump, manifest và diagnostic là file private 0600, thư mục 0700; không commit hoặc upload chúng.

5. Đổi callback Identity và gateway cùng đợt với cấu hình owner: `Hosting:CommunityEnabled=false` trên API cũ, Community integrated dùng DB mới và cùng keyring/HMAC, Identity nhận peer Community riêng. Compose mặc định chứa cấu hình này. `community-cutover-gate` phải qua trước khi khởi động writer.
6. `make up`, `make check`, kiểm phiên/refresh/cursor cũ, replay operation, Community/DM qua gateway và backlog journal. Ghi mốc mở writer; không quay route về nguồn sau khi đã có ghi mới.

## Rollback

Dừng và drain cả hai phía. Nếu đích còn đúng manifest đã chuyển, chạy lại CLI với `--rollback-before-writes` và artifact directory mới. Lệnh xác minh trước khi khóa runtime đích, ghi `rolled_back`, rồi mở runtime nguồn. Kiểm tra nguồn đọc được bằng role cũ. Không chạy rollback chỉ từ lỗi HTTP hoặc đổi connection string bằng tay.

Sau khi CLI rollback thành công, cấu hình phục hồi gồm host cũ đăng ký Community, keyring/HMAC cũ, callback Identity về chat-service và gateway gửi Community về host đó. Đặt `SCDC_COMMUNITY_ROLLBACK_IMAGE` tới tag/digest của image host cũ đã build và kiểm chứng trước lần dọn nguồn ngày 2026-11-28. Overlay yêu cầu biến này và xóa cấu hình build; checkout hiện tại chỉ chứa host Messaging và từ chối bật lại Community:

```bash
SCDC_COMMUNITY_ROLLBACK_IMAGE=<image-cu-da-kiem-chung> make up ROLLBACK=1
```

Override cần Docker Compose 2.24.4+ cho `!override` và `!reset`; dùng cùng project/`.env` như dev. Target Community nằm ở profile tắt. Image rollback phải có sẵn trước triển khai; thiếu biến thì Compose dừng, không dùng ngầm image hiện hành. Hồ sơ S3-10 là bằng chứng của cấu hình trước lần dọn nguồn này, không xác nhận image rollback mới được chọn. Gate rollback vẫn yêu cầu record đã xác minh, nguồn `LOGIN`, đích `NOLOGIN` và không còn connection runtime đích; đổi manifest đơn thuần không vượt qua được gate.

Nếu đích đã ghi hoặc sequence/generation thay đổi, CLI rollback từ chối và giữ nguồn đóng. Ưu tiên sửa tiến. Quay lại sau ghi cần mapping chênh lệch cho dữ liệu/fence/journal/khóa và diễn tập riêng. Tách Community không đổi quy trình generation Redis của Identity. Sau rollback, có thể chạy lại cutover nếu nguồn chưa đổi; nguồn đã ghi mới thì cần đích mới hoặc đối chiếu có review.

## Kiểm chứng

`python3 scripts/test_community_transfer.py` nhận container/port test rõ ràng, chỉ cho `POSTGRES_DB` kết thúc `_test`. CI dùng hai database trên cùng PostgreSQL để kiểm manifest, retry, gián đoạn, role/client, gate và hai nhánh rollback. [Hồ sơ dev](../../../docs/records/verification/architecture/s3-10-phase-3-2026-11-26.md) ghi dữ liệu thật đã chuyển, browser và giới hạn.

Nguồn: [Community service](../../../services/Community/README.md), [D02/D48–D52](../../../docs/system/database.md), [checklist EX-00 đến EX-08](../../../docs/guides/operations.md#service-extraction).

# Tổ chức Compose theo vòng đời vận hành — 2026-11-28

## Phạm vi

Từ main `fef5b6c`, tách chín service thường trực khỏi bảy công cụ chuẩn bị và
service mẫu. `compose.yaml` dùng image; `compose.dev.yaml` bổ sung build và
debug. Make gọi các script setup, migration và preflight; công cụ nằm trong
`ops/compose/compose.prepare.yaml`, chạy bằng `run --rm --no-deps`.

Giữ nguyên tên project mặc định `scdc`, các khóa volume và cấu hình owner.
Không chuyển dữ liệu dev, không deploy stack đang dùng và không đưa phần dọn
nguồn Community chưa commit vào gói này. [Hướng dẫn vận hành Compose](../../../../ops/README.md)
ghi lối khởi động được hỗ trợ, thứ tự và các tùy chọn.

## Kiểm chứng cục bộ

- 77 test script Python đạt, gồm 11 test bộ điều phối: thứ tự setup, preflight,
  lỗi gate/schema/migration giữ writer dừng, rollback, image dựng sẵn, giữ
  env/project/đường dẫn có dấu cách, và down không xóa volume.
- Docker Compose provider 5.6.0 qua Podman rootless: cấu hình hợp lệ ở sáu tổ
  hợp dev/image dựng sẵn, có/không sample, email và rollback. File runtime
  đứng riêng có chín service, không có build/profile hoặc dependency tới tool.
- `make setup ENGINE=podman` đạt trên project thử riêng, secret riêng, image
  riêng, cổng loopback ngẫu nhiên và ba database có hậu tố `_test`. Build bốn
  image, chuẩn bị khóa, Community DB, gate, migration email và health đạt.
- Trên database thử, lần lượt mở login nguồn Identity, mở login nguồn Community
  và đổi checksum migration email: mỗi `make up` đều thất bại và toàn bộ bốn
  container ứng dụng/gateway vẫn dừng. Khôi phục trạng thái thử, `make migrate`,
  `make up` và health đạt; migrate không tự bật writer.
- Rollback khi chưa có record `rolled_back` bị gate từ chối trước khi bật writer.
  Cấu hình rollback yêu cầu image cũ đã xác minh, không build lại host ghép.
- Chuẩn bị lần hai giữ nguyên hash khóa JWT/fingerprint/cursor. Sau `down` và
  `up`, các hash tiếp tục khớp; health cả ba owner đạt. Chỉ chín container runtime
  chạy, không còn container job đã thoát.
- Sample bật riêng bằng `SAMPLE=1`: `/health` trả service mẫu;
  `/sample/protected` từ chối anonymous bằng 401. Khi tắt sample, endpoint trả
  404 và `/health` chuyển về Messaging.
- `DEV=0` chạy image đã build và qua health khi overlay thử cung cấp tường minh
  môi trường Development cho email. Đây không phải kiểm chứng cấu hình production.
- Kiểm tra liên kết Markdown và `git diff --check` đạt.

Log và kết quả máy đọc nằm tại `artifacts/runtime-compose-2026-11-28/` trong
checkout kiểm chứng, không commit. Secret, dữ liệu và key hash không đưa vào
hồ sơ Git. Stack thử được dọn theo project riêng sau kiểm chứng.

## Giới hạn

`docker compose up` trực tiếp chỉ chạy runtime; người vận hành phải dùng
Make/script để thực hiện preflight trước khi mở ứng dụng. Readiness của API
không thay gate kiểm nguồn/đích bằng credential quản trị.

Không chạy lại browser hoặc tải/telemetry toàn stack trong gói này. Không
diễn tập lại một lần rollback dữ liệu thành công; bộ thử xác nhận cấu hình và
nhánh từ chối khi chưa đủ điều kiện. Runbook chuyển dữ liệu và giới hạn rollback
sau ghi tiếp tục áp dụng.

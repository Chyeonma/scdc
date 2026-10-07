# SCDC — Nghiệm thu gói phòng văn bản và ACL

Ngày kiểm chứng: 2026-10-07. Phạm vi [kế hoạch](plan.md), [thiết kế](../../design/channels-access.md). Hồ sơ ghi kết quả theo revision; tiến độ hiện tại tại [status.md](../../status.md).

| Phần | Commit trên feat/community-channels | Bằng chứng |
|---|---|---|
| Backend, contract guard/lifecycle và migration 004 | `eaaa2e3`, kế thừa permissions | 191 ca Release đạt: 158 hồi quy và 33 ca mới; bổ sung assertion ACL/CAS trong ca rejoin hiện hữu |
| WebClient | `f8d582d` | 24 ca Node, 50 ca Chromium (11 ca phòng mới), production build đạt |

## Hành vi bàn giao

Member active xem danh sách và metadata phòng hiện hành được cấp view. Lọc quyền trước LIMIT; mọi trang kiểm tra lại trạng thái, không hứa snapshot bất biến. Cursor UUID keyset gắn actor/server/limit/purpose, hạn 24 giờ. Hidden/deleted/cross-server trả 404, không lộ metadata hoặc ACL.

Owner hoặc manage_channels tạo phòng **text**, default view allow; metadata, Messaging space, operation và access-change outbox cùng transaction. Messaging ghi space qua IChatSpaceLifecycle, Community chỉ ghi schema mình sở hữu. Operation UUIDv4 được scope actor/server, fingerprint canonical; retry cùng body/key trả resource hiện hành 200, khác body conflict, resource đã deleted không được tái tạo bằng key cũ.

PATCH name/topic cần view và owner/manage_channels, expectedVersion; kind bất biến. Tên trim 1–100 UTF-16, key NFC/ToLowerInvariant so sánh C, unique theo server với phòng active; giữ tên hiển thị/phân biệt dấu. Topic tối đa 1.000 UTF-16, CRLF chuẩn hóa, null để xóa. No-op giữ version/event.

GET/PUT ACL cần view và owner/manage_channel_access. PUT thay snapshot đầy đủ với expectedAccessVersion: default allow/deny, role overrides cùng server (gồm @everyone), member overrides active đúng epoch. Role deny-wins, personal override áp cuối; owner sau điều kiện nền. Tự mất view có thể commit, lần đọc sau bị chặn. ACL mutation tăng channel version/accessVersion; role delete/rejoin dọn override cũng tăng channel accessVersion, chặn snapshot cũ ghi đè.

IChannelAccessGuard giữ Identity/server/channel share locks trong transaction caller, trả epoch/access versions/hạn phiên. Caller phải kiểm tra lease ngay trước commit và trạng thái space/tác giả thuộc Messaging. Đã có proof role/ACL mutation chờ transaction đọc kết thúc; chưa có writer tin hoặc Hub tiêu thụ guard.

WebClient vào phòng từ detail, tạo/sửa metadata và cấu hình allow/deny/inherit cho role/member. Pending create lưu trước POST theo actor/server, giữ body/key qua reload. Sửa/ACL không tự replay; conflict hoặc kết quả không rõ cần GET đối soát rồi thao tác lưu mới. Đổi actor/server/selection bỏ phản hồi cũ; 401/403/404 đóng dữ liệu quản lý. Roster phân trang vẫn giữ epoch của override hiện hữu và draft ACL.

## Kiểm chứng backend và dữ liệu

PostgreSQL 18 trong container riêng scdc-community-channels-db, DB scdc_community_channels_test tại 127.0.0.1:15432. Migration tests tạo/drop DB fixture kết thúc _test. Chỉ dữ liệu/khóa tổng hợp; không nâng cấp DB ứng dụng.

```bash
# Đặt ConnectionStrings__Database và Identity cho DB thử nghiệm riêng.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1 \
  --logger 'trx;LogFileName=community-channels.trx' \
  --results-directory artifacts/community-channels/backend
```

**191 passed, 0 failed, 0 skipped**; Release build không warning/error. Bằng chứng mới:

- Policy qua list/detail API: default, @everyone, nhiều role allow/deny, personal allow/deny, owner và điều kiện account/session; hidden manager không có quyền sửa/ACL. Lọc trước LIMIT và quyền đổi giữa hai trang.
- Unicode/NFC/case/accent, giới hạn tên/topic, immutable kind, body/unmapped fields, collision và no-op; metadata/ACL CAS, cross-server role/member, duplicate và stale epoch.
- Create cùng operation đồng thời chỉ một resource/event, replay qua factory restart/key rotation/scope server; mất key HMAC cũ trả 503. Hai ACL writer cùng expectedVersion chỉ một thắng.
- Shared guard giữ khóa tới caller commit, role/ACL mutation phải chờ; ACL revoke trước admission bị chặn. Lifecycle fault sau space insert, outbox fault và lease hết hạn rollback space/channel/operation/ACL/version. Role delete/rejoin cleanup chặn ACL snapshot/epoch cũ; rejoin outbox fault giữ channel accessVersion cũ.
- Upgrade 001/002/003 → 004 qua **501 channel**, vượt batch 500: mapping text/voice và allow/deny đã review, giữ display/timestamp/ACL/space status. Thiếu mapping, read-only, archived/collision bị chặn; SQL fault rollback DDL/backfill/trigger/ledger; checksum replay no-op, bootstrap/seed đồng bộ.

Đối chiếu DB sau backend/Chromium: 0 active channel thiếu key, 0 channel space mồ côi, 0 personal override sai epoch, 0 outbox đã publish. Outbox chưa có dispatcher.

## Kiểm chứng WebClient

API Release tại 127.0.0.1:15026, Vite tại 127.0.0.1:15300 proxy tới API. Node 24/Chromium từ image mcr.microsoft.com/playwright:v1.63.0-noble.

| Kiểm tra | Kết quả |
|---|---|
| npm test | 24/24, gồm 3 ca channel validation/pending storage mới |
| Chromium | 50/50, gồm 11 ca phòng/ACL mới và 39 hồi quy |
| npm run build | Production build thành công |

Ca mới: mobile 390px tạo Unicode/sửa/reload; @everyone deny → member 404/list trống → personal allow; mất create response sau commit/reload rồi replay đúng operation/body; metadata và ACL conflict từ API writer thật cần GET đối soát; manager chỉ có quyền ACL tự mất view; 401/503 không PUT lại; selection/actor đổi bỏ phản hồi chậm; phân trang phòng/cursor error; validation/storage failure/name conflict; roster hơn 20 giữ override epoch và draft.

CRUD/conflict/quyền dùng API thật. 401/503/cursor error/storage failure được tiêm; delayed/lost response lấy từ API thật trước khi giữ hoặc bỏ phản hồi. TRX/browser output ở artifacts/community-channels/; ảnh channels-mobile.png đã xem, không tràn ngang. Artifacts/token/DB/key ring không commit; API/Vite/PostgreSQL thử nghiệm và key ring API được dọn sau kiểm chứng.

## Chạy lại và giới hạn

Checkout feat/community-channels. DB mới bootstrap 001–004. DB cần giữ dữ liệu: drain writer cũ → runner 004 kèm reviewed channel map → deploy writer mới theo [README module](../../../../../services/Modules/Community/README.md) **của nhánh code**. API không tự migrate; 001–003/checksum giữ nguyên. Mapping từng channel gồm kind text/voice và defaultView allow/deny; không tự suy từ visibility legacy hoặc repair read-only/archived. Không chạy schema.sql có DROP SCHEMA trên DB cần giữ dữ liệu.

```bash
cd clients/WebClient
npm test
npm run build
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác; API trỏ tới DB _test riêng:
SCDC_E2E_URL=http://127.0.0.1:15300 SCDC_E2E_DATABASE=scdc_community_channels_test \
  npm run test:e2e -- --output ../../artifacts/community-channels/browser
```

Đạt phần text create của UC-COM-16, metadata/list của UC-COM-17, metadata edit UC-COM-18 và HTTP ACL của UC-COM-22. Có bằng chứng AC-COM-06/07/16/27, phần view HTTP của AC-COM-11/25/26/42 và TC-ACL-10/12 (trừ subscription/DELETE). Chưa đóng toàn bộ UC có phụ thuộc tin/Media/thu hồi.

UC-COM-19 xóa phòng, voice/Media lifecycle, lịch sử/gửi/sửa/xóa tin, Hub/dispatcher/UC-COM-25 và thu hồi ≤5 giây chưa triển khai. Chưa đo RAM/tải thực tế. Code chưa merge main; người dùng tự push nhánh code/tài liệu. Đề xuất duyệt riêng gói xóa phòng trước khi mở gói lịch sử/gửi tin và realtime.

Kiểm tra tài liệu trên main: 87 Markdown/2.161 liên kết nội bộ, 0 lỗi. OpenAPI resolve 530 local refs, giữ nguyên component schemas; chỉ cập nhật metadata sáu route trong gói, DELETE vẫn là mục tiêu.

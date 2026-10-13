# Hồ sơ Community — roles

Hồ sơ giữ phạm vi được chọn và kết quả kiểm chứng tại revision ghi trong từng phần. Những mô tả nhánh, thiết kế chưa triển khai hoặc chưa hợp nhất phản ánh thời điểm bàn giao. Quy tắc hiện hành và trạng thái theo khả năng ở [Community](../../../features/community/README.md); không suy bằng chứng cũ thành kết quả của code mới.

## Phạm vi tại thời điểm triển khai

Người dùng duyệt tiếp tục ngày 2026-10-07. Code ở `feat/community-permissions`, kế thừa create/view → join → search; tài liệu độc lập trên main. Gói chọn role/assignment và nền policy của [UC-COM-20/21](../../../features/community/access-control.md#use-cases).

<a id="scope-phạm-vi"></a>

### Phạm vi

- Owner tạo/sửa/xóa custom role, tối đa 20; tên 1–64 UTF-16 sau trim, unique theo key trim/NFC/ToLowerInvariant, phân biệt dấu.
- @everyone tự áp cho membership active; không sửa/xóa/gán tay/cấp management. Catalog giữ đúng năm management codes; union allow, không hierarchy hoặc DENY quản lý.
- Owner đọc roster và thay toàn bộ tập role của target active với membershipId/expectedVersion. Owner/manage_channel_access được đọc role catalog và roster; không trả email.
- Migration 003: role version/key, FK assignment và member override theo epoch, operation create_role có scope server; bootstrap/seed và nâng cấp dữ liệu có kiểm chứng.
- Guard Identity + server lock giữ đến commit, kiểm tra ownership/quyền hiện hành; mutation cùng accessVersion/outbox, no-op không tăng version. Create retry cùng key/body, PATCH/PUT/DELETE CAS và không tự replay.
- WebClient quản lý role và gán cho thành viên bằng API thật; validation, conflict, pending create/reload, lỗi/actor đổi và mobile.
- Evaluator domain đối chiếu 18 fixture quyền nền/owner/role/cá nhân/kind. Chưa mở ACL/channel API, history, Hub/dispatcher hay proof thu hồi ≤5 giây; UC-COM-25 vẫn là phụ thuộc riêng của UC-COM-20/21.

<a id="scope-kiểm-chứng"></a>

### Kiểm chứng

Owner-only/cross-server/system protection; Unicode/collision/catalog/cap; operation replay/rotation/restart; stale role version/epoch và hai writer; lost response/no replay; rollback outbox/expiry; server lock và slot thứ 20; migration preserve/no-op/checksum/preflight/rollback/FK epoch. Backend Release, Node, Chromium, production build và docs-check.

Dẫn chiếu COM-028/037, ACL-19, AC-COM-17/36/38 và TC-ACL-03/08/09; view/ACL/realtime chỉ đạt phần evaluator, không suy fixture thành API/thu hồi đã đạt. [Thiết kế](../../../features/community/access-control.md) · [Trạng thái](../../../features/community/README.md).

## Kết quả theo revision

Ngày kiểm chứng: 2026-10-07. Phạm vi [kế hoạch](roles.md), [thiết kế](../../../features/community/access-control.md). Hồ sơ ghi bằng chứng theo revision; [các chủ đề Community](../../../features/community/README.md) giữ tiến độ hiện tại.

| Phần | Commit trên feat/community-permissions | Bằng chứng |
|---|---|---|
| Backend, evaluator và migration | `9a513d1`, kế thừa search | 158 ca Release đạt: 111 hồi quy và 47 ca role/policy/migration mới |
| WebClient | `eae64c3`, kế thừa backend trên | 21 ca Node, 39 ca Chromium (8 ca role mới), production build đạt |

<a id="results-hành-vi-bàn-giao"></a>

### Hành vi bàn giao

Owner hiện hành tạo/sửa/xóa custom role và gán/gỡ role cho membership active. Tối đa 20 custom role; @everyone tự áp, không sửa/xóa/gán tay/cấp management. Năm management codes giữ đúng catalog, union allow không hierarchy. Tên 1–64 UTF-16 sau trim, unique theo key trim/NFC/ToLowerInvariant trong server, phân biệt dấu; tên hiển thị giữ nguyên.

GET role catalog và roster cho owner hoặc manage_channel_access; GET/PUT tập role của target và mọi writer chỉ owner. Roster chỉ membership/UserSummary, không email; user có thể null nếu Identity directory không còn account active. Role/member pages UUID keyset, limit 1–50/mặc định 20, cursor purpose riêng gắn actor/server/limit/position và hạn 24 giờ.

PATCH/DELETE dùng expectedVersion; PUT thay toàn bộ tập custom role với membershipId/expectedVersion. CAS/epoch cũ bị chặn; no-op với version hiện hành không tăng version/event. Assignment tăng membership version; delete role cascade grant/role override và tăng version membership bị đổi. Legacy invite còn tham chiếu role trả 409 ROLE_IN_USE. Version wire là chuỗi số dương, storage hiện hành int32.

Identity guard và server lock giữ tới commit; authorization/ownership kiểm tra từ dữ liệu hiện hành. Mutation tăng server accessVersion và ghi Community.AccessChanged.v1 nguyên tử, chỉ ID/cause/version. Create operation có scope server, HMAC theo fixture, retry cùng body/key trả role hiện hành; resource đã xóa không được tạo lại bằng key cũ. Chưa có dispatcher/Hub phát event.

WebClient owner mở quản lý từ detail, tạo/sửa/xóa và chọn thành viên để gán/gỡ role. Create giữ operation/body theo actor/server trước POST, phục hồi sau reload. PATCH/DELETE/PUT không tự replay; conflict hoặc kết quả không rõ yêu cầu GET để kiểm tra rồi người dùng lưu tiếp. Đổi actor/route bỏ response cũ và dữ liệu quản lý.

<a id="results-kiểm-chứng-backend"></a>

### Kiểm chứng backend

PostgreSQL 18 trong container riêng scdc-community-roles-db, DB scdc_community_roles_test tại cổng 15432. Migration tests tạo/drop DB fixture riêng kết thúc _test. Chỉ dữ liệu/khóa tổng hợp; không nâng cấp DB ứng dụng.

```bash
# Đặt ConnectionStrings__Database và Identity cho DB thử nghiệm riêng.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1 \
  --logger 'trx;LogFileName=community-roles.trx' \
  --results-directory artifacts/community-roles/backend
```

**158 passed, 0 failed, 0 skipped**; Release build không warning/error. Bằng chứng mới:

- 18 fixture evaluator nền/owner/role deny-wins/cá nhân/kind/management và fingerprint fixture create_role; permission order không đổi HMAC, unknown/system management không cấp quyền.
- Owner-only/cross-server/system protection, quản lý union và roster không email; actor unverified/revoked bị chặn. Reader manage_channel_access không trở thành writer hoặc đọc tập role target.
- Unicode/NFC/case/accent, giới hạn UTF-16, tên/perms invalid, collision; tranh slot thứ 20 chỉ một create thắng. Cùng operation đồng thời trả 201/200, không trùng resource/event.
- Role/member keyset và cursor gắn actor/server/limit/purpose, restart; nullable UserSummary. Full-set assignment/no-op, stale role version/epoch và hai PATCH/PUT writer chỉ một thắng.
- Assignment và user override FK chặn epoch cũ sau rejoin. Delete role với override allow/deny dọn grant/override, tăng membership version; không diễn giải đây là proof view/Hub đã đạt.
- Outbox fault và lease hết hạn rollback role/operation/grant/version; delete fault rollback assignment/override. Owner đổi trong khi chờ server lock bị chặn, lock timeout trả 503 không ghi operation.
- Replay qua factory restart/rotation, scope server khác nhau, thiếu HMAC key cũ trả 503; role đã xóa trả conflict, không tái tạo.
- Upgrade 001/002→003 qua **502 role** (501 system + 1 custom), giữ display/updated_at/quyền/epoch, đi qua ranh giới batch 500; bootstrap/seed đồng bộ ledger/FK. Checksum khác/thiếu baseline/collision/catalog/tên/cap bị chặn; SQL fault rollback DDL/backfill và phục hồi trigger.

<a id="results-kiểm-chứng-webclient"></a>

### Kiểm chứng WebClient

API Release tại 127.0.0.1:15026, Vite tại 127.0.0.1:15300 proxy tới API. Node 24/Chromium từ image mcr.microsoft.com/playwright:v1.63.0-noble.

| Kiểm tra | Kết quả |
|---|---|
| npm test | 21/21, gồm 3 ca role validation/pending storage mới |
| Chromium | 39/39, gồm 8 ca role mới và 31 hồi quy create/view/join/search |
| npm run build | Production build thành công |
| Đối chiếu DB sau Chromium | 0 server quá 20 custom role; 0 grant sai epoch; 0 outbox đã publish |

Ca mới gồm: mobile 390px tạo Unicode → gán/gỡ → đọc effectivePermissions của target → sửa/xóa/protect system/reload; mất create response sau commit rồi retry cùng body/key trả 200; invalid input và name conflict NFC; version conflict từ PATCH thật bên ngoài UI; 401/503 không tự PATCH lại; storage bị chặn không POST và actor đổi bỏ response tạo cũ; chọn thành viên mới bỏ response cũ. CRUD/conflict/effective permission dùng API thật. 401/503/storage được tiêm; delayed/lost response lấy từ API thật trước khi giữ hoặc bỏ phản hồi.

TRX và browser output ở artifacts/community-roles/; ảnh roles-mobile.png đã xem, kiểm tra không tràn ngang. Artifacts/token/DB/key ring không commit. Môi trường kiểm thử đã dừng và dọn sau kiểm chứng.

<a id="results-chạy-lại-và-phạm-vi-đạt"></a>

### Chạy lại và phạm vi đạt

Checkout feat/community-permissions. DB mới dùng bootstrap 001/002/003; DB cần giữ dữ liệu drain writer cũ → runner 003 → deploy writer mới theo [README module](../../../../services/Modules/Community/README.md) của nhánh code. Không chạy schema.sql có DROP SCHEMA trên DB cần giữ dữ liệu. API không tự migrate; 001/002/checksum giữ nguyên.

```bash
cd clients/WebClient
npm test
npm run build
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác, API thực sự trỏ tới DB _test riêng:
SCDC_E2E_URL=http://127.0.0.1:15300 SCDC_E2E_DATABASE=scdc_community_roles_test \
  npm run test:e2e -- --output ../../artifacts/community-roles/browser
```

Đạt phần quản lý role/assignment HTTP/UI của UC-COM-20/21: AC-COM-36, phần role của AC-COM-38, TC-ACL-08 (18 fixture), TC-ACL-09 và epoch/CAS. AC-COM-17/TC-ACL-03 mới có kết quả evaluator domain, chưa có API gửi/phòng để nghiệm thu. UC-COM-22/25, channel checker/guard, lịch sử/Messaging lifecycle, Hub/dispatcher và proof thu hồi ≤5 giây chưa triển khai; không đánh dấu toàn bộ UC-COM-20/21 đã đóng. Chưa đo RAM/tải thực tế. Code chưa merge main; người dùng tự push tài liệu/code.

# Hồ sơ Community — search

Hồ sơ giữ phạm vi được chọn và kết quả kiểm chứng tại revision ghi trong từng phần. Những mô tả nhánh, thiết kế chưa triển khai hoặc chưa hợp nhất phản ánh thời điểm bàn giao. Quy tắc hiện hành và trạng thái theo khả năng ở [Community](../../../features/community/README.md); không suy bằng chứng cũ thành kết quả của code mới.

## Phạm vi tại thời điểm triển khai

Người dùng cho phép tiếp tục ngày 2026-10-07. Gói chọn tìm kiếm/summary của [UC-COM-02](../../../features/community/servers.md#uc-com-02), nối vào join public/immediate đã có trên `feat/community-join`; code ở `feat/community-search`.

<a id="scope-phạm-vi-và-đầu-ra"></a>

### Phạm vi và đầu ra

- GET `/api/v1/servers/search?q=...&limit=...&cursor=...`: q 2–100 UTF-16 sau trim, tìm literal một phần tên; case-insensitive/accent-sensitive theo DEC-093.
- Exact-name trước, rồi key tên/UUID; trang mặc định 20, tối đa 50; chỉ summary public/active/nondeleted, kể cả khi actor đã là member.
- Key tên trim/NFC/ToLowerInvariant thống nhất giữa create, query và backfill; migration 002 giữ nguyên tên hiển thị/version/visibility/owner/epoch.
- Cursor Data Protection theo actor/query chuẩn hóa/limit/position/expiry; từng trang lọc lại trạng thái hiện hành.
- Khu vực Khám phá có tìm kiếm, tải/rỗng/lỗi/phân trang; mở kết quả → detail/join → own list và quay lại cùng từ khóa.
- Không tìm rỗng để liệt kê toàn bộ, không tạo membership khi chỉ tìm/xem; không thêm UC-COM-07/08, metadata management, room hoặc realtime.

<a id="scope-tiêu-chí-kiểm-chứng"></a>

### Tiêu chí kiểm chứng

| Nhóm | Bằng chứng |
|---|---|
| Privacy/Identity | Private/inactive/deleted không xuất hiện, member chỉ nhận summary; anonymous/unverified/session invalid bị chặn |
| Query và Unicode | Q 1/2/100/101 UTF-16, emoji/combining/Unicode lỗi/trống; case/NFC tương đương nhưng dấu khác; `%`, `_`, backslash và SQL metacharacters tìm literal |
| Thứ tự/phân trang | Duplicate names, exact-first, keyset/tie UUID; limit 20/50; cursor không dùng với actor/query/limit/purpose khác hoặc tamper/expiry |
| Thay đổi hiện hành | Unseen server đổi private/inactive giữa trang bị loại từ cursor cũ; không có snapshot public cố định |
| Migration | Upgrade 001 → 002 và bootstrap/seed tương đương; backfill cùng key .NET, giữ version/data, replay checksum no-op, dữ liệu invalid hoặc fault rollback; không sửa checksum 001 |
| WebClient | API thật search → preview → join/list; rỗng/validation/pagination, request muộn/query/actor đổi, lỗi/refresh từ đầu, keyboard/mobile, approval không mở join trực tiếp |
| Hồi quy | Backend Release, Node, Chromium, production build; docs-check và diff sạch |

Dẫn chiếu [COM-038](../../../features/community/servers.md#com-038), [AC-COM-01/37](../../../features/community/servers.md#acceptance), [TC-COM-01/18](../../../features/community/servers.md#tests) và đường tìm kiếm của [AC-COM-02](../../../features/community/memberships.md#ac-com-02). Private switch dùng fixture/SQL có khóa; chưa chứng minh API UC-COM-04 hoặc pending transition. Đường approval của UC-COM-02 còn UC-COM-07.

[Thiết kế](../../../features/community/servers.md) · [Trạng thái](../../../features/community/README.md).

## Kết quả theo revision

Ngày kiểm chứng: 2026-10-07. Phạm vi [kế hoạch](search.md), [thiết kế](../../../features/community/servers.md). Hồ sơ ghi bằng chứng theo revision; [các chủ đề Community](../../../features/community/README.md) giữ tiến độ hiện tại.

| Phần | Commit trên feat/community-search | Bằng chứng |
|---|---|---|
| Backend và migration | `7a883b7`; fixture đổi tên/assertion ở `b10851b` | 111 ca Release đạt, gồm 30 ca search/migration/key mới và 81 ca hồi quy |
| WebClient | `b5be14f`, kế thừa backend trên | 18 ca Node, 31 ca Chromium (7 ca search mới), production build đạt |

<a id="results-hành-vi-bàn-giao"></a>

### Hành vi bàn giao

GET `/api/v1/servers/search` tìm literal một phần tên, q 2–100 UTF-16 sau trim; case-insensitive/accent-sensitive theo key trim/NFC/ToLowerInvariant. Exact-name trước, rồi key C/UUID; trang mặc định 20, tối đa 50, keyset. Kết quả luôn là sáu field ServerSummary, chỉ public/active/nondeleted; member không nhận detail qua search. Tìm/xem không tạo membership hoặc outbox event.

Cursor Data Protection purpose riêng, gắn actor/query đã chuẩn hóa/limit/position và hạn 24 giờ. Từng trang lọc lại trạng thái hiện hành. Lỗi Identity, cursor hoặc database trả lỗi tương ứng, không chuyển thành kết quả rỗng.

WebClient có khu vực Khám phá từ own list. Submit hợp lệ lưu q trong hash URL, tải/empty/error/pagination; mở kết quả → preview → join trực tiếp đã có. Sau join có thể quay lại cùng từ khóa hoặc own list đã cập nhật. Đổi actor/query/route bỏ phản hồi cũ; lỗi trang/cursor cho tải lại từ đầu. Reload URL chỉ đọc GET, không tự join.

Migration 002 lưu search_name và backfill bằng runner .NET theo batch 500; giữ tên hiển thị/version/accessVersion/epoch. Chỉ trigger version/timestamp được tắt tạm trong transaction; owner/@everyone invariant vẫn được kiểm tra trước ALTER tiếp theo. Deploy cần drain writer cũ, migrate 002 rồi chạy writer mới theo [README module](../../../../services/Modules/Community/README.md) trên nhánh code. API không tự migrate; migration/checksum 001 giữ nguyên.

<a id="results-kiểm-chứng-backend"></a>

### Kiểm chứng backend

PostgreSQL 18 trong container riêng `scdc-community-search-db`, DB `scdc_community_search_test`, cổng 15432. Migration tests tạo/drop DB fixture riêng kết thúc `_test`; upgrade 001→002 có 501 server để đi qua hai batch. Chỉ dữ liệu và khóa tổng hợp, không bootstrap/migrate DB ứng dụng.

```bash
# Đặt ConnectionStrings__Database và cấu hình Identity cho DB thử nghiệm riêng.
dotnet test SCDC.slnx --configuration Release --no-restore -m:1 \
  --logger 'trx;LogFileName=community-search.trx' \
  --results-directory artifacts/community-search/backend
```

**111 passed, 0 failed, 0 skipped**; Release build 0 warning/error. Các bằng chứng mới:

- Owner/nonmember đều chỉ thấy public summary; private/inactive/deleted bị loại; tìm không tạo membership/event.
- Case/NFC, dấu tiếng Việt, duplicate exact-first và tie UUID; q 1/2/100/101 UTF-16, emoji/combining, invisible/control/NUL và UTF-16 lỗi qua application service; limit 0/51 bị chặn.
- `%`, `_`, backslash và SQL metacharacters tìm literal qua API/PostgreSQL thật.
- Hai trang keyset qua duplicate names, factory restart, q chuẩn hóa tương đương; actor/query/limit/purpose/tamper khác bị chặn. Codec kiểm tra expiry, ApplicationName, position và storage unavailable.
- SQL fixture đổi unseen server sang private/inactive/deleted giữa hai trang; cursor cũ loại chúng, direct private đọc 404. Đây là proof đọc lại trạng thái, chưa phải API metadata/private-switch hoặc race pending của UC-COM-04.
- Anonymous/unverified/revoked bị chặn; table lock timeout trả 503, sau release đọc được kết quả.
- Migration backfill 501 record giữ display/updated_at/versions/access/visibility/join mode/membership epoch; bootstrap/seed đồng bộ ledger; cùng checksum no-op, checksum khác bị chặn. Thiếu 001, tên invalid hoặc fault SQL rollback; trigger được phục hồi. Fixture đổi tên ghi cả key và chứng minh retry trả tên mới/search theo tên mới, chưa triển khai PATCH metadata.

<a id="results-kiểm-chứng-webclient"></a>

### Kiểm chứng WebClient

API Release tại `127.0.0.1:15026`, Vite `127.0.0.1:15300` proxy tới API, cùng DB riêng. Node 24, Chromium từ image Playwright `mcr.microsoft.com/playwright:v1.63.0-noble`.

| Kiểm tra | Kết quả |
|---|---|
| `npm test` | 18/18; 16 ca hồi quy và 2 ca search validation |
| Chromium | 31/31; 24 ca create/view/join hồi quy và 7 ca search mới |
| `npm run build` | Production build thành công |
| Đối chiếu DB sau Chromium | 0 server thiếu search key; 0 event join trùng membershipId |

7 ca mới gồm: public-only/exact-first → preview/join/own list/reload trên mobile 390px; NFC/accent/literal characters; invalid query/keyboard/invalid URL; 22 kết quả phân trang và reset khi cursor lỗi; query đổi khi response cũ bị giữ; actor đổi khi lỗi cũ chưa về; 503/retry/reload chỉ GET. Thành công, Unicode, privacy và keyset dùng API thật. Cursor 400, dependency 503 và lỗi muộn của actor cũ được tiêm để kiểm chứng UI; delayed query giữ response lấy từ API thật. Approval chỉ có bằng chứng UI tiêm của [gói join](direct-join.md), chưa có writer request.

TRX và browser output ở `artifacts/community-search/`; ảnh `artifacts/community-search/discovery-mobile.png` đã xem và không tràn ngang. Artifacts, token, DB và key ring không commit. API/WebClient đã dừng, container và thư mục khóa thử nghiệm đã dọn sau kiểm chứng.

<a id="results-chạy-lại-và-phạm-vi-đạt"></a>

### Chạy lại và phạm vi đạt

Checkout `feat/community-search`, chuẩn bị DB thử nghiệm mới bằng bootstrap hoặc nâng cấp 001→002 theo README module; API Development cần ExposeDevelopmentTokens để tạo tài khoản tổng hợp. Cấu hình Identity/HMAC/key ring phải riêng cho thử nghiệm.

```bash
cd clients/WebClient
npm test
npm run build
SCDC_DEV_API_TARGET=http://127.0.0.1:15026 npm run dev -- --port 15300 --strictPort
# Terminal khác, cùng thư mục:
SCDC_E2E_URL=http://127.0.0.1:15300 SCDC_E2E_DATABASE=scdc_community_search_test \
  npm run test:e2e -- --output ../../artifacts/community-search/browser
```

E2E_DATABASE chỉ xác nhận tên DB thử; người chạy phải kiểm tra API thực sự trỏ vào DB riêng. Không chạy schema.sql có DROP SCHEMA lên DB cần giữ dữ liệu.

Đạt search/summary của UC-COM-02, AC-COM-01/37 và AC-COM-02 trên public/immediate; TC-COM-01/18 đạt phần search/privacy, TC-COM-03 kế thừa join. UC-COM-02 còn đường UC-COM-07 approval; UC-COM-04/pending, role/ACL, phòng và realtime chưa được nghiệm thu từ gói này. Không đo tải hoặc khẳng định btree đủ cho mọi quy mô; contains có thể quét/sort nhiều record. Code chưa merge main, người dùng tự push nhánh tài liệu/code.

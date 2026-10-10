# DM-P2-T02 — lịch sử phân trang và tin khi vắng mặt

Trạng thái: **Chờ người dùng test FE/BE**. Ngày10/10/2026 người dùng xác nhận “Đồng ý; C02 PASS thì làm P2-T02”. C02 P2-T01 đã được agent test PASS FE/BE theo yêu cầu; P2-T01 được nghiệm thu, push xác nhận `9401ab5` và merge `--no-ff` vào `message` tại `ea941a7`. Chỉ triển khai P2-T02 trong lần này. Không coi kết quả agent dưới đây là người dùng đã nghiệm thu P2-T02.

## Bản chạy và phạm vi

| Trường | Giá trị |
|---|---|
| Task branch / PR target | `feat/dm-p2-t02-message-history` → `message` |
| Base đã nghiệm thu | `ea941a7` |
| Code, helper và tests | `a66f7d9a7c90077e736d245b696c89fe749cf9ea` |
| Repository chạy | `E:\Project\SCDC\dm-message-integration` |
| Frontend / API / Swagger | <http://localhost:15300> / <http://localhost:15026/api/v1> / <http://localhost:15026/swagger> |
| Docker project / DB | `scdc-dm-acceptance` / `scdc_dm_acceptance_test`, PostgreSQL18 |
| FE asset đang phục vụ | `/assets/index-68H9GSBu.js` |
| API image | `sha256:93050df47fc5eaf79e90eac85b9512b23a96b1e940de5d1ea2c741961b8a8467` |
| FE image | `sha256:2ca48fda90d069e3d7cc66c2b49bc9e52ad13f7a67bd008050cd00c0233a7c83` |
| Migration | Không thêm migration; dùng schema/counter/index đã nghiệm thu P2-T01 |
| Key ring | Giữ `.dm-acceptance/keyrings/cursor`, application `SCDC.UserSearch.dm-acceptance`, purpose `Messaging.History.v1` |
| Gate | Chưa merge P2-T02; dừng chờ người dùng PASS/FAIL FE và BE trên bản bàn giao |

GET `/direct-conversations/{id}/messages` hỗ trợ latest, before, after, through: mặc định50/tối đa100, items tăng sequence, nextCursor/hasMore/throughSequence/resumeCursor. Actor/session và membership được kiểm tra từng trang. Reader khóa space FOR SHARE sau guard Identity; writer FOR UPDATE giữ đến commit, nên reader không chốt frontier trước một writer đang giữ counter. B đọc được tin của A khi offline rồi đăng nhập/reload, kể cả peer unavailable; current DTO trả nội dung hiện hành hoặc tombstone.

Cursor protected gắn actor, conversation, direction, scope, limit, position, frontier và expiry24h. Before giữ H; resume bắt đầu catchup mới và chốt H mới, after-next giữ H xuyên các trang. `after=0` là bootstrap duy nhất; không nhận sequence tùy ý. Through chỉ kiểm tra bằng H trong protected cursor, không tự đặt H. Resume chỉ ở latest hoặc trang catchup cuối. Sequence/version giữ dạng chuỗi, FE dùng BigInt.

FE tải latest khi mở/reload, có Tải tin cũ hơn, giữ điểm cuộn, error/retry và Làm mới tin nhắn. **Làm mới tải lại50 tin mới nhất**, bấm tải cũ để đọc lại các trang trước. Tin mới đã gửi trong lúc GET cũ đang chờ không bị response cũ xóa. Cache/draft chỉ ở RAM, tách actor, abort/generation loại response đã hết hiệu lực. Chọn một người theo yêu cầu hiện hành. Nhận tức thời, Hub, reconnect và full retry composer thuộc P3/P4.

Giới hạn legacy: row thiếu author/client ID không đáp ứng MessageResponse hiện hành được trả503 và không bị bỏ qua âm thầm; task này không dựng author hay UUID giả để chuyển đổi dữ liệu legacy không tương thích.

## Mở môi trường và dữ liệu mẫu

Chạy trong PowerShell terminal, truyền Action rõ ràng. Dùng script ở **dm-message-integration** cho bản P2-T02. Checkout `E:\Project\SCDC\scdc` hiện còn ở P0 (`ca4e5c7`); chạy Start/build từ checkout đó sẽ phục vụ lại bản cũ, không có API tìm người. Các checkout dùng chung Docker project/cổng, nên chỉ khởi động từ thư mục bàn giao dưới đây:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Status
```

Docker Desktop cần chạy Linux engine. Bản hiện tại đã build/chạy trên15300/15026; dùng Ctrl+F5 nếu tab còn asset cũ. Khi cần dựng lại đúng checkout:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start
```

Run **p2huser** đã tạo riêng cho bạn, không dùng run agent `p2hist`. Mật khẩu các tài khoản mẫu active: `DmDemo2026!Local`.

| Alias | Username | Dùng để test |
|---|---|---|
| A | `dm_demo_an_p2huser` | Gửi, đọc H121, cursor |
| B | `dm_demo_bao_p2huser` | Người nhận tin khi offline |
| C | `dm_demo_chi_p2huser` | Outsider D-AB/D-HIST |
| S12 | `dm_demo_search12_p2huser` | Peer D-HIST; display Người tìm12 |
| S13 | `dm_demo_search13_p2huser` | Peer D-BIG; display Người tìm13 |

- D-HIST A/S12: `01a122f3-c7ee-7884-b120-008e2cb4bd7c`, đúng121 tin `HIST-001`…`HIST-121`, từng tin qua POST writer thật. IDs/UUID/sequence/version/label tại `.dm-acceptance/runs/p2huser/history-h121.json`.
- D-BIG A/S13: `01a122f3-c7fc-7822-9865-654de005555f`, hai tin `BIG-LOW`/`BIG-HIGH`, sequence chính xác `9007199254740992`/`9007199254740993`; manifest `history-bigint.json`. Chỉ setup counter trên pair trống do run sở hữu trong DB test đã xác minh, rồi gửi hai tin qua writer; không có endpoint bypass production.
- D-AB A/B: dành cho bạn mở và gửi C02; agent không gửi vào pair của run này.
- Baseline `dm_demo_an`/`dm_demo_bao` vẫn giữ nguyên hai tin đã báo thiếu. Browser B đã đọc được cả hai, cùng ID sau reload, không gửi lại.

H121 được đọc cả FE và BE nên không tạo mutation thêm. C02 bạn gửi một lần từ FE; BE có thể replay chính UUID/body FE để chứng minh idempotence. Test độc lập dùng run mới và UUID mới, không reset baseline/volume. Nếu cần tạo dataset mới trên một máy khác hoặc lượt mới:

```powershell
# Chọn suffix ngắn mới; không Setup lại run đang có dữ liệu.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Setup -Run p2hnew
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Fixtures -Run p2hnew
```

## C01 — latest50, before50 và before21

**FE:** Vào `http://localhost:15300`, Ctrl+F5. Đăng xuất tài khoản hiện tại nếu có, đăng nhập username **`dm_demo_an_p2huser`**, password **`DmDemo2026!Local`**. Kiểm tra góc dưới bên trái hiển thị `@dm_demo_an_p2huser` → bấm dấu + → nhập đúng `dm_demo_search12_p2huser` → chọn dòng có username này → Mở hội thoại. Chờ hết Đang tải tin nhắn. Ban đầu đúng50 dòng HIST-072…121. Cuộn để thấy HIST-072, ghi vị trí dòng này, bấm Tải tin cũ hơn: thêm HIST-022…071 và dòng đang xem giữ vị trí. Bấm lần nữa: thêm HIST-001…021, tổng121 dòng đúng thứ tự, không còn nút tải cũ. Reload mở lại: latest50 được tải từ server; bấm hai lần tải cũ lại đủ121.

**BE:** Copy cả block sau. Helper tự login/logout ở RAM và không in token. Không giữ cookie/token trong file response:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
$p1 = powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser | ConvertFrom-Json
$p2 = powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -Before $p1.Body.nextCursor | ConvertFrom-Json
$p3 = powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -Before $p2.Body.nextCursor | ConvertFrom-Json
@($p1,$p2,$p3) | ForEach-Object {
  [pscustomobject]@{Http=$_.Status;Count=@($_.Body.items).Count;HasMore=$_.Body.hasMore;Through=$_.Body.throughSequence;First=$_.Body.items[0].content;Last=$_.Body.items[-1].content}
} | Format-Table
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Compare -Run p2huser
```

Expected: HTTP200 cả ba; count50/50/21; hasMore true/true/false; through="121" cả ba. First/last: HIST-072/121, HIST-022/071, HIST-001/021. `$p3.Body.nextCursor` null; resumeCursor chỉ khác null ở p1. Compare union121 unique IDs bằng DB theo conversation_sequence và đọc không thay dữ liệu. Bằng chứng helper: `history-compare-S12.json` trong run.

## C02 — B offline, đọc sau login/reload và restart

**Trước FE:** B chưa login/đã logout ở cửa sổ riêng. Mở pair và chụp baseline N tin bằng helper; việc Open không gửi tin:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Open -Run p2huser -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run p2huser -PeerAlias B
```

**FE:** Login A → dấu + tìm đúng `dm_demo_bao_p2huser` → Mở hội thoại → gửi nguyên `OFFLINE-HISTORY-C02`, chờ Đã gửi. Network POST `/messages` phải200; ghi clientMessageId và message.id/sequence/version từ request/response. Mở cửa sổ Incognito khác login B → dấu + tìm `dm_demo_an_p2huser` → Mở hội thoại. Thấy đúng nội dung và người gửi An từ GET history. Reload B: cùng ID vẫn hiện, không có bản gửi lặp. Khi A gửi tin tiếp lúc B đang mở, B dùng Làm mới tin nhắn để đọc (task này chưa realtime).

**BE:** Lấy tin đã gửi và replay **chính operation FE**; không tạo UUID mới trong block này:

```powershell
$read = powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -PeerAlias B -ReaderAlias B | ConvertFrom-Json
$dto = @($read.Body.items | Where-Object content -CEQ 'OFFLINE-HISTORY-C02')[-1]
$dto | Select-Object id,clientMessageId,sequence,version,content
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run p2huser -PeerAlias B -ClientMessageId $dto.clientMessageId -Content 'OFFLINE-HISTORY-C02'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run p2huser -PeerAlias B
# Chứng minh cursor đã cấp của H121 còn dùng được sau restart API; giữ volume/key ring.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action RestartProof -Run p2huser
$again = powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -PeerAlias B -ReaderAlias B | ConvertFrom-Json
@($again.Body.items | Where-Object id -EQ $dto.id) | Select-Object id,sequence,version,content
```

Expected: send/replay/history200, cùng id/sequence/version/content; delta từ N là một message, một SendOperation, một outbox. Replay và GET/reload/restart không tăng các count. RestartProof200, cursorPreserved=true và DB ID/version không đổi. Reload B sau restart vẫn thấy tin này. Nếu lặp lại case, ghi baseline N mới, không kỳ vọng tổng luôn1.

## C03 — cursor chéo actor/DM, tamper và outsider

**BE:** Block chạy sáu biến thể, không sửa message:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action CursorChecks -Run p2huser
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -Before a -After b -ExpectedStatus 400
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -ReaderAlias C -ExpectedStatus 404
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -Anonymous -ExpectedStatus 401
```

Expected: other-DM, tampered, wrong-direction và member S12 dùng cursor của A →400 CURSOR_INVALID. Before+after →400 Common.ValidationFailed. C →404 RESOURCE_NOT_FOUND, không items/participants/frontier. Anonymous →401 Common.Unauthorized. Scope actor khác chỉ test400 khi actor đó thật sự là member; outsider phải404. Helper ghi `history-cursor-checks.json` chỉ status/errorCode, không token/cursor.

**FE:** Login A mở H121. DevTools Console copy fixture dưới đây rồi bấm Làm mới tin nhắn. Fixture chỉ thay nextCursor của response latest, không đổi DB/body tin. Sau khi đủ50 dòng, bấm Tải tin cũ hơn:

```javascript
window.__dmHistoryOriginalFetch = window.fetch;
window.fetch = async (...args) => {
  const response = await window.__dmHistoryOriginalFetch(...args);
  const url = new URL(typeof args[0] === 'string' ? args[0] : args[0].url, location.href);
  if (url.pathname.endsWith('/messages') && !url.searchParams.has('before') && !url.searchParams.has('after') && response.ok) {
    const body = await response.clone().json(); body.nextCursor = 'invalid-client-cursor-fixture';
    return new Response(JSON.stringify(body), { status: response.status, headers: { 'Content-Type': 'application/json' } });
  }
  return response;
};
```

Expected: request before tới API thật400 CURSOR_INVALID; UI báo lỗi và giữ50 dòng. Tắt fixture bằng block sau, bấm Thử tải lại: tải latest sạch200, lỗi mất, đúng H121, không gửi tin hoặc chuyển DM:

```javascript
window.fetch = window.__dmHistoryOriginalFetch;
delete window.__dmHistoryOriginalFetch;
```

## C04 — tải cũ lỗi/retry và bigint

**FE:** A mở H121 latest50. Ghi IDs và vị trí dòng đang xem. Console bật fixture503 **chỉ ở browser cho request before**, rồi bấm Tải tin cũ hơn:

```javascript
window.__dmHistoryOriginalFetch = window.fetch;
window.fetch = (...args) => {
  const url = new URL(typeof args[0] === 'string' ? args[0] : args[0].url, location.href);
  if (url.pathname.endsWith('/messages') && url.searchParams.has('before'))
    return Promise.resolve(new Response(JSON.stringify({ status: 503, errorCode: 'AUTHORITY_UNAVAILABLE' }), { status: 503, headers: { 'Content-Type': 'application/problem+json' } }));
  return window.__dmHistoryOriginalFetch(...args);
};
```

Expected: UI báo AUTHORITY_UNAVAILABLE;50 tin vẫn đủ, điểm cuộn được giữ. Tắt bằng block restore ở C03 rồi Thử tải lại: thêm50 tin, tổng100; tải lần nữa121. Mở DM với `dm_demo_search13_p2huser`: đúng hai dòng BIG-LOW trước BIG-HIGH. Network sequence là hai chuỗi khác nhau, không Number rounding.

**BE/DB bigint:**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Page -Run p2huser -PeerAlias S13
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Compare -Run p2huser -PeerAlias S13
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\history.ps1 -Action Compare -Run p2huser
```

Expected:200, sequence "9007199254740992", "9007199254740993", version "1"; through="9007199254740993", hai ID unique bằng DB. H121 vẫn121. Fixture này đã tạo; không sửa counter bằng SQL tay vào pair app/baseline.

**BE503 thật:** browser fixture phía trên không phải bằng chứng API/DB503. Integration test sau tạo fixture qua PostgreSQL thật, cấp before cursor hợp lệ, dựng API host test với kết nối authority không truy cập được, gửi GET before →503 AUTHORITY_UNAVAILABLE, không body tin; cleanup chỉ dữ liệu test sở hữu. Không tắt DB chung hoặc cài fault production:

```powershell
docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml --profile tools run --rm backend-tests dotnet test SCDC.slnx --configuration Release --artifacts-path /artifacts --results-directory /artifacts/test-results --logger "trx;LogFileName=dm-history-fault.trx" --filter "FullyQualifiedName~History_auth_database_failure_returns_503_and_no_body"
```

Expected: một test PASS, không skipped. Runtime15300/15026 vẫn chạy sạch để bạn tiếp tục đọc history. Luôn restore fetch sau test; reload cũng loại fixture browser.

## SQL chỉ đọc và bằng chứng

Helper Compare dùng transaction READ ONLY. Có thể xem trực tiếp scope H121; đây là tên bảng/column thực tế:

```powershell
$sql = @"
BEGIN READ ONLY;
SELECT count(*) AS messages, count(DISTINCT id) AS unique_ids, count(DISTINCT conversation_sequence) AS unique_sequences
FROM messaging.messages WHERE space_id='01a122f3-c7ee-7884-b120-008e2cb4bd7c';
SELECT id, conversation_sequence::text AS sequence, version::text AS version
FROM messaging.messages WHERE space_id='01a122f3-c7ee-7884-b120-008e2cb4bd7c' ORDER BY conversation_sequence;
SELECT last_message_sequence::text, last_message_id, last_activity_at FROM messaging.spaces
WHERE id='01a122f3-c7ee-7884-b120-008e2cb4bd7c';
COMMIT;
"@
$sql | docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml exec -T postgres psql -X -q -U scdc_dm_test -d scdc_dm_acceptance_test -v ON_ERROR_STOP=1
```

Expected121/121/121, sequence1…121 và counter121. Lệnh không in nội dung riêng hoặc key/token. Snapshot send-text cung cấp message/operation/outbox/counter/lastMessageId/activity cho đối soát C02.

Bằng chứng agent trong runtime ignored `.dm-acceptance`: `backend-artifacts/test-results/dm-acceptance.trx`, `e2e-p2-t02/results.json`, run `p2hist` có history-c02-browser/history-baseline-receiver/history-restart-proof/history-cursor-checks/history-compare-S12. Run p2huser có các manifest và compare-S12/S13. Không đưa token/key/HAR chat vào Git.

## Kết quả và gate

| Nhóm | Agent | Người dùng |
|---|---|---|
| Backend thực PostgreSQL |70/70 PASS, không skipped; H121, frontier catchup cố định, current edit/tombstone/unavailable peer, guard từng trang, expiry/restart host, held-commit/rollback và authority503 |Chưa xác nhận |
| Client/API wrapper |46/46 PASS; merge ID/version/bigint, pending operation, actor refresh và cursor encoding/cancellation |Chưa xác nhận |
| FE build |PASS; Docker FE asset như bảng bản chạy |Chưa xác nhận |
| Browser |7/7 PASS: C01–C04, baseline B open/reload, response latest đến sau send và logout loại response cũ |Chưa xác nhận |
| Restart API thật |PASS; protected before cursor200 cùng DTO/ID, DB không đổi |Chưa xác nhận |
| Dữ liệu mẫu người dùng |H121 qua writer, compare50/50/21; bigint2 qua writer, API/DB cùng IDs |Chưa xác nhận |

Lệnh chạy lại (không cập nhật fixture/manifest hoặc restart khi browser đang chạy):

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action BackendTests
Push-Location .\clients\WebClient
try {
  npm.cmd test
  $env:DM_P2_HISTORY_RUN='p2hist'
  npx.cmd playwright test --config playwright.dm-p2-t02.config.js
} finally { Pop-Location }
```

Các vòng E2E ghi thêm hai tin C02 vào pair agent A/B và một tin race vào A/S14, không đổi H121/user run/baseline; chạy lại không kỳ vọng pair agent luôn rỗng. Browser503 là fixture frontend; authority503 là test host backend riêng như mô tả. Viewport desktop Edge/Chromium không thay test thiết bị thật.

Bạn ghi cho từng C01–C04: commit/build, FE PASS/FAIL, BE PASS/FAIL, bước lỗi expected/actual, run và IDs/status cần thiết. Sau khi bạn PASS cả FE/BE P2-T02 mới cập nhật nghiệm thu, merge vào `message`, smoke/push và xét task tiếp. Không tự merge vì agent đã test hoặc vì bạn đang hỏi thao tác.

## Xác minh Git và bàn giao

Code đã push thành công; `git ls-remote --heads origin feat/dm-p2-t02-message-history message` xác minh remote code SHA `a66f7d9a7c90077e736d245b696c89fe749cf9ea` và `message` SHA `ea941a7504f0230038ca391bb935832c3d1daa05`. Commit hồ sơ bàn giao tiếp theo chỉ đổi docs; metadata ignored `runs/p2hist/p2-t02-build.json` ghi `handoverCommit`/`remoteCommit` thực tế sau push và xác minh HEAD cuối. Không tạo PR trong lượt này; target nếu tạo là `message`.

P2-T01 commit nghiệm thu `9401ab5` là ancestor của `message` merge `ea941a7`; smoke writer/read trên bản kế thừa đạt trong70 backend tests. Baseline A/B vẫn2 message/2operation/2outbox/counter2 sau triển khai history; browser B thấy đúng hai ID cũ, trước/sau reload. P2-T02 chưa là ancestor của `message`, vì còn chờ người dùng test.

## Kiểm tra lại C01 theo phản hồi không thấy người dùng — 10/10/2026

Người dùng báo không tìm thấy `dm_demo_search12_p2huser`. Đọc DB xác nhận tài khoản cùng ID `01a122f3-a7fc-730a-93aa-4ceed62e9946`, status1 và email đã verified, không bị xóa hoặc thiếu seed. Runtime tại15300/15026 đang dùng image khác bản bàn giao, FE asset `index-B7nUc0-P.js`; GET `/users/search?q=dm_demo_search12_p2huser&limit=20` trả404 Common.NotFound cả API trực tiếp và proxy FE. Checkout `scdc` vẫn ở P0 `ca4e5c7`. Đây là lỗi bản chạy không có route tìm người, không phải không có tài khoản mẫu.

Đã build/start từ `E:\Project\SCDC\dm-message-integration`, giữ PostgreSQL volume và các ID/tin có sẵn. Compose working_dir đã xác minh thuộc đúng thư mục này; image mới cập nhật ở bảng bản chạy (manifest image thay đổi khi rebuild, source sản phẩm vẫn `a66f7d9`). API search qua FE trả200 với đúng một kết quả username/ID trên.

Browser Edge/Chromium chạy riêng `C01` trên **run p2huser của người dùng**, login An → dấu + tìm đúng username → chọn/mở hội thoại →50/100/121 tin, thứ tự/ID/label và điểm cuộn: **1/1 PASS, 2,6 giây**. Không gửi thêm tin hoặc Setup lại run. Report giữ ở `.dm-acceptance/runs/p2huser/browser-c01-recheck.json`, chẩn đoán tại `search-runtime-recheck.json`. Gate người dùng vẫn Chờ xác nhận; không merge hay triển khai task kế.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration\clients\WebClient
$env:DM_P2_HISTORY_RUN='p2huser'
npx.cmd playwright test --config playwright.dm-p2-t02.config.js --grep '^C01'
```

## Người dùng nghiệm thu P2-T02 — 10/10/2026

Người dùng xác nhận: “P2-T02 đã PASS FE/BE; nghiệm thu và làm P3-T01.” Bản được nghiệm thu: `9a2b345`, code `a66f7d9`, FE asset `index-68H9GSBu.js`. Các trạng thái Chờ/Chưa xác nhận phía trên ghi theo thời điểm bàn giao và được thay thế bởi xác nhận này. Được phép tích hợp riêng P2-T02 vào `message`, triển khai duy nhất P3-T01 và dừng chờ người dùng test task đó.

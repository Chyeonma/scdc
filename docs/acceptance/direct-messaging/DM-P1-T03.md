# DM-P1-T03 — inbox dữ liệu thật

Trạng thái: **Người dùng đã nghiệm thu**, ngày09/10/2026, bản bàn giao `9e47033`. Người dùng đã xác nhận “P1-T02 đã PASS FE và BE; làm tiếp P1-T03” trên build `ea0c311`. P1-T02 đã merge `--no-ff` vào `message` tại `e5170b4`; base P1-T03 là `1a2ec28c8fd87c7ddb2fc1eb81198f017d7a9f63`. P1-T03 chưa merge, chưa triển khai P2.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / truy vết | P1-T03.1–.5; AC-DM-01/12, lát cắt inbox của TC-DM-01/12; không nghiệm thu gửi/lịch sử/Hub |
| Branch / repository | `feat/dm-p1-t03-conversation-inbox`; repo gốc `E:\Project\SCDC\scdc`, worktree chạy `E:\Project\SCDC\dm-message-integration` |
| Commit implementation/build | `970f03b88e5bf2388926fc153dca2623f1be3d56`; commit docs sau đó không đổi code đã test |
| Remote task SHA | Xác minh bằng `git ls-remote`; SHA bàn giao cuối cùng nằm trong `.dm-acceptance/runs/baseline/p1-t03-build.json` và `git rev-parse HEAD` |
| PR / integration | Chưa tạo PR; target `message`. Chưa merge P1-T03; giữ main |
| Web / API / Swagger | http://localhost:15300 / http://localhost:15026/api/v1 / http://localhost:15026/swagger |
| DB / user / Compose | `scdc_dm_acceptance_test` / `scdc_dm_test` / `scdc-dm-acceptance`; PostgreSQL localhost15432 |
| Schema | Migration additive `20261009_dm_p1_inbox.sql`; giữ migration cặp/member của P1-T02 |
| Config / keys | `.env.dm-test` ngoài Git; cùng keyring persistent, purpose `Messaging.Conversations.v1` tách search, TTL cursor24h |
| Dataset / browser | Run `baseline`, 28 tài khoản; Edge headless thật Windows1440×1000, API .NET10 và PostgreSQL18 trong Docker Linux |

Inbox chỉ lấy active membership của actor, space DM chưa xóa và pair chứa actor. Mỗi trang kiểm tra actor/session/stamp/expiry qua Identity Contracts trong shared transaction; khóa space và kiểm tra lại membership trước projection. Read không đòi peer còn active; peer không khả dụng chỉ có summary công khai và `availability="unavailable"`.

Thứ tự `lastActivityAt DESC NULLS LAST`, rồi UUID theo thứ tự PostgreSQL ASC; default20, max50. Cursor protected gắn actor/limit/scope/vị trí, TTL cố định từ trang đầu. Chuỗi rỗng, sửa cursor, dùng chéo actor/limit/purpose hoặc hết hạn trả400 `CURSOR_INVALID`; limit sai400 `Common.ValidationFailed`; phiên sai401; provider không xác nhận được quyền/dữ liệu503 `AUTHORITY_UNAVAILABLE`. Danh sách có thể đổi giữa các trang, client dedup theo ID và có **Làm mới hội thoại**.

FE tải inbox khi login/reload, có loading/empty/error/retry/tải thêm. RAM list gắn actor, cleanup logout và hủy/bỏ response cũ. Mở DM thành công refresh trang đầu, giữ hội thoại vừa mở được chọn. Reload tải trang đầu và chọn hội thoại đầu tiên; danh sách đầy đủ vẫn lấy bằng Tải thêm, không lưu DM vào localStorage. Chỉ chọn một người theo yêu cầu hiện hành.

**Người vừa nhắn tin** lấy peer từ DM có `lastActivityAt` khác null, loại actor, dedup ID, cùng lựa chọn với search. Baseline chưa có tin nên mục này rỗng dù inbox có DM. Thứ tự bằng tin commit thật còn chờ P2-T01; test metadata giả lập trong fixture backend chỉ chứng minh query ordering, không thay proof gửi tin. Chưa có khung gửi tin trong P1-T03.

## Chuẩn bị, chạy và dữ liệu

Docker Desktop phải chạy Linux engine. Dùng đúng worktree; khởi động từ checkout P0 sẽ build bundle cũ.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
Set-ExecutionPolicy -Scope Process Bypass
git branch --show-current
git rev-parse HEAD
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Status
# Chỉ khi cần rebuild hoặc bổ sung migration:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action Upgrade
# Tạo/mở lại đúng25DM qua API; không tạo tin; giữ ID có sẵn:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action Fixtures
# Dừng / chạy lại, giữ volume và keys:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Stop
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start -NoBuild
```

Không chạy lại `schema.sql` trên volume cũ hoặc `Setup` baseline đã có manifest DM. Upgrade chỉ áp migration additive. Fixtures idempotent; nếu A có thêm DM ngoài25, helper giữ dữ liệu và báo cần run riêng, không xóa hội thoại để ép count.

Mật khẩu local chung **`DmDemo2026!Local`**. A=`dm_demo_an`, B=`dm_demo_bao`, C=`dm_demo_chi`, K=`dm_demo_khoa`, U=`dm_demo_pending`; S01–S23=`dm_demo_search01`…`dm_demo_search23`. B/C cùng tên **Bảo Demo**, phân biệt bằng username. K active/verified chưa có DM; không bấm Mở hội thoại bằng K trong case empty. U chưa verified, login403 `Identity.EmailNotVerified`, không có token đọc inbox.

| Alias | ID thật / số DM |
|---|---|
| A | `01a114bc-1e4d-74c7-87eb-8db1734f8aea`;25 |
| B | `01a114bc-2678-7d79-b136-4d944d6a85fc`;1 |
| C | `01a114bc-2b41-7c64-8622-aa1694b2de9b`;1 |
| D-AB | `01a11c8f-534c-761c-8d4a-4930c95bee58` |
| D-AC | `01a11e8f-7921-7efd-a475-dd2b408f9994` |
| D-AS01…23 | Resolve từ manifest `conversations`, không tự đặt UUID |

Baseline sau fixture:25 pair/50 membership/0 message, không orphan DM; A25/B1/C1/K0. Những DM của P1-T02 và DM người dùng mở với C được giữ ID. Các ca FE/BE dưới đây chỉ GET hoặc mở lại DM có sẵn, delta pair/member/message =0. Tài khoản integration được tạo riêng, cleanup chỉ own IDs.

Khởi tạo chung cho các recipe BE, chạy trong cùng PowerShell để token chỉ nằm trong RAM:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
Set-ExecutionPolicy -Scope Process Bypass
Import-Module ./scripts/dm-acceptance/Common.psm1 -Force -DisableNameChecking
Assert-DmDatabase
Assert-DmApiTarget
```

Swagger: POST `/auth/login` với `login=dm_demo_an`, password local, deviceName `DM-P1-T03-manual`; Authorize với token đang dùng, GET `/direct-conversations` không cursor hoặc cursor nhận từ response. Không chụp body login/token vào hồ sơ. Recipe PowerShell bên dưới tự logout.

## C01 — inbox theo membership; AC-DM-01/12

Baseline/lane: `baseline`, FE và BE đọc cùng25DM đã commit; không tạo thêm. Người dùng **Chưa xác nhận**.

FE:
1. Mở http://localhost:15300, Ctrl+F5, đăng nhập A. Mặc định ở Direct Messages; sidebar hiện20DM.
2. Bấm **Tải thêm hội thoại**:25DM, có `Bảo Demo (@dm_demo_bao)` và `Bảo Demo (@dm_demo_chi)` cùng23 peer S.
3. Chọn dòng `@dm_demo_bao`: header Bảo Demo/@dm_demo_bao, **Chưa có tin nhắn.** Chọn dòng `@dm_demo_chi`: header Bảo Demo/@dm_demo_chi. Không nhầm hai người trùng tên.
4. Dùng profile/browser riêng cho B rồi C, password chung. B chỉ một dòng An Demo từ D-AB; C chỉ một dòng An Demo từ D-AC; không có DM A/B trong list C.
5. Reload mỗi profile: A tải20, B/C vẫn1; A có thể tải thêm để đủ25.

BE/DB:

```powershell
foreach ($alias in 'A','B','C') {
 powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action Compare -ActorAlias $alias
 if ($LASTEXITCODE -ne 0) { throw "Compare failed for $alias" }
}
# In SQL tương đương helper, chỉ đọc actor C:
$manifest=Get-DmManifest baseline
$actorId=([guid]($manifest.accounts | Where-Object alias -eq 'C').id).ToString()
Invoke-DmReadSql @"
SELECT s.id,d.user_low_id,d.user_high_id,s.last_message_sequence,s.last_activity_at
FROM messaging.space_members m JOIN messaging.spaces s ON s.id=m.space_id
JOIN messaging.direct_conversations d ON d.space_id=s.id
WHERE m.user_id='$actorId' AND m.membership_status=1 AND m.left_at IS NULL
AND s.space_type=1 AND s.status<>3 AND s.deleted_at IS NULL
AND '$actorId'::uuid IN (d.user_low_id,d.user_high_id)
ORDER BY (s.last_activity_at IS NOT NULL) DESC,s.last_activity_at DESC,s.id;
"@
```

Expected: `apiEqualsDb=true`, A count25/pageSizes20,5; B count1=D-AB; C count1=D-AC. Participants đúng hai public summaries; empty DM `lastSequence="0"`, activity null, không email hoặc lastMessage giả. SQL C chỉ D-AC. Agent: API/DB và Edge PASS. Ghi build/profile và actual counts/IDs để xác nhận; không lưu token. Không có fault cần cleanup.

## C02 — phân trang và persistence

Baseline/lane như C01, không A/K;25DM, mọi activity null. Người dùng **Chưa xác nhận**.

FE:
1. Login A hoặc reload:20 dòng. Bấm Tải thêm:25 dòng, mỗi username chỉ một lần, nút tải thêm biến mất.
2. Chọn một DM, bấm **Làm mới hội thoại**: trang đầu20; bấm tải thêm lại:25, không duplicate.
3. Nhấn+, tìm `dm_demo_bao`, chọn, bấm Mở hội thoại. Header đúng Bảo; chỉ một lựa chọn. Inbox refresh và dòng Bảo được chọn; không tạo pair mới.
4. Reload: danh sách20 vẫn tải từ DB; tải thêm đủ25. Active selection sau reload là dòng đầu, không yêu cầu nhớ người vừa chọn.

BE:

```powershell
$session=Connect-DmActor A baseline 'P1-T03-C02'
try {
 $first=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $session.accessToken
 Assert-DmStatus $first 200 'First page'
 $second=Invoke-DmRequest GET ('/direct-conversations?limit=20&cursor='+[Uri]::EscapeDataString($first.Body.nextCursor)) -AccessToken $session.accessToken
 Assert-DmStatus $second 200 'Second page'
 $ids=@($first.Body.items.id)+@($second.Body.items.id)
 [pscustomobject]@{first=@($first.Body.items).Count;second=@($second.Body.items).Count;unique=@($ids|Sort-Object -Unique).Count;lastCursor=$second.Body.nextCursor}
 foreach ($limit in 0,51) {
  $r=Invoke-DmRequest GET "/direct-conversations?limit=$limit" -AccessToken $session.accessToken
  Assert-DmStatus $r 400 'Invalid limit'
  $r.Body.errorCode
 }
} finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
# Khởi động lại API/FE, giữ dataset, đồng thời dùng cursor lấy trước restart:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action Persistence
```

Expected:first20/second5/unique25/lastCursor null; limit0/51=400 `Common.ValidationFailed`. Persistence API/FE restart vẫn A25, cursor cũ200 còn5; sau restart browser reload rồi tải thêm lại. Helper Compare đối chiếu toàn bộ ID/order SQL. Không có writer nên thứ tự toàn bộ theo ID ASC, không tuyên bố snapshot khi sau này có activity mới. Agent: query/page/DB/browser PASS; persistence ghi ở bảng bằng chứng. Giữ manifest; GET/restart delta0.

## C03 — empty khác lỗi và retry; lỗi API thật

Baseline: K không DM, A25DM. Lane BE và FE dùng các session độc lập, token không ghi file. Người dùng **Chưa xác nhận**.

FE empty:
1. Login K, thấy **Chưa có hội thoại. Nhấn + để tìm người nhận.**,0dòng.
2. Nhấn+: **Người vừa nhắn tin** → **Chưa có người vừa nhắn tin.**. Tìm Bảo và chọn được đúng một người; chọn C thay B. Đóng modal, không bấm Mở hội thoại bằng K.
3. Login A profile riêng, modal gần đây cũng rỗng vì chưa có tin, dù inbox A25.

BE empty:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action Compare -ActorAlias K
```

Expected K count0, pageSizes[0], apiEqualsDb=true; GET200 `items=[]`, `nextCursor=null`.

Fault FE/BE thật: chỉ thay connection của API acceptance sang port1 đóng, DB thật và volume vẫn giữ. Trước khi bật: login A trên FE, đóng modal. Đừng login/refresh token trong lúc fault. Trong PowerShell đã import Common ở trên, chạy toàn bộ:

```powershell
$session=Connect-DmActor A baseline 'P1-T03-C03-fault'
try {
 try {
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action FaultOn
  if ($LASTEXITCODE -ne 0) { throw 'FaultOn failed' }
  $r=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $session.accessToken
  Assert-DmStatus $r 503 'Fault inbox'
  [pscustomobject]@{status=$r.Status;errorCode=$r.Body.errorCode}
  Read-Host 'FE: reload -> loi, 0 dong; nhan + -> loi nguoi vua nhan tin. Giu modal dang mo. Enter de khoi phuc'
 } finally {
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action FaultOff
  if ($LASTEXITCODE -ne 0) { throw 'FaultOff failed; run FaultOff again before continuing' }
 }
 $r=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $session.accessToken
 Assert-DmStatus $r 200 'Recovery inbox'
 [pscustomobject]@{status=$r.Status;items=@($r.Body.items).Count}
} finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$session.refreshToken} }
# Recipe agent tu dong fault503 -> restore200, khong doi DB:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/inbox.ps1 -Action FaultProbe
```

Expected: BE503 `AUTHORITY_UNAVAILABLE`, không `items` giả; FE lỗi **Không tải được hội thoại. Hãy thử lại.**,0dòng sau reload, không empty-success/mock. Modal gần đây có lỗi riêng, không gán người mẫu. Sau restore: **Thử lại hội thoại** →20, tải thêm25; modal **Thử lại người vừa nhắn tin** →empty đúng baseline. Nếu PowerShell bị đóng khi fault: chạy riêng `inbox.ps1 -Action FaultOff`. Agent: cả injected client fault (được ghi riêng) và fault API/database connection thật có bằng chứng; không gọi mock là backend proof. Counts25:50:0 giữ nguyên sau fault.

## C04 — actor switch, cursor và revoke

Baseline A25/C1, C không thuộc D-AB. Lane đọc; membership/space/message delta0. Người dùng **Chưa xác nhận**.

BE trong PowerShell:

```powershell
$a=Connect-DmActor A baseline 'P1-T03-C04-A'
$c=Connect-DmActor C baseline 'P1-T03-C04-C'
try {
 $first=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $a.accessToken
 Assert-DmStatus $first 200 'A first'
 $q='/direct-conversations?limit=20&cursor='+[Uri]::EscapeDataString($first.Body.nextCursor)
 $bad=Invoke-DmRequest GET $q -AccessToken $c.accessToken
 Assert-DmStatus $bad 400 'Cross actor'
 $bad.Body.errorCode # CURSOR_INVALID
 $good=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $c.accessToken
 Assert-DmStatus $good 200 'C own inbox'
 $good.Body.items.id # chi D-AC
 $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
 $revoked=Invoke-DmRequest GET '/direct-conversations?limit=20' -AccessToken $a.accessToken
 Assert-DmStatus $revoked 401 'Same revoked A token'
 $anon=Invoke-DmRequest GET '/direct-conversations?limit=20'
 Assert-DmStatus $anon 401 'Anonymous'
} finally {
 $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
 $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$c.refreshToken}
}
```

FE delay fixture, không delay server hoặc gửi mutation: login A, DevTools → Sources → Snippets (hoặc Console), chạy đoạn sau. Nó giữ response GET thật trong RAM và cố ý bỏ abort để kiểm tra phòng vệ; không in auth/body:

```javascript
window.dmNativeFetch = window.fetch;
window.dmHeld = false;
window.dmGateReady = false;
window.fetch = async (url, options) => {
 if (String(url).startsWith('/api/v1/direct-conversations?') && !window.dmHeld) {
  window.dmHeld = true;
  const response = await window.dmNativeFetch(url, { ...options, signal: undefined });
  window.dmGateReady = true;
  await new Promise(resolve => { window.dmRelease = resolve; });
  return response;
 }
 return window.dmNativeFetch(url, options);
};
```

1. Bấm **Làm mới hội thoại**; kiểm tra `window.dmGateReady` là true. Đây là response A đã nhận từ backend nhưng chưa giao cho loader.
2. **Cài đặt** → **Đăng xuất**, login C trong cùng tab. List chỉ1 dòng An Demo, header @dm_demo_an; A không còn selection/list.
3. Console chạy `window.dmRelease(); window.fetch = window.dmNativeFetch;`. List C vẫn1, không25 và không xuất hiện Bảo/S của A.
4. Reload C: vẫn1. Nếu bỏ giữa chừng, reload hủy snippet; không có fault DB để tắt. Đối soát bằng Compare C ở C01.

Expected: cursor A + C.token400 `CURSOR_INVALID`, C sạch200 chỉD-AC; chính token A đã logout401. Response A về trễ không ghi vào C, kể cả fetch cố ý bỏ abort. Agent: Edge thật + REST/PostgreSQL PASS. Lưu IDs/count và thứ tự thao tác đã lọc token; không cần HAR.

## Bằng chứng agent và biến thể bổ sung

| Kiểm tra | Kết quả / artifact ngoài Git |
|---|---|
| Backend PostgreSQL thật | 50/50; gồm43 regression predecessor +7 inbox tests; `runs/baseline/p1-t03-backend.trx` |
| Cursor | Actor/limit/tamper/empty/4097/purpose sai400; TTL24h hết hạn400; host mới cùng keyring dùng cursor cũ200 |
| Query / permission | Default20+5, limit1/50, no duplicate, outsider/empty, deleted space bỏ ở trang kế, locked actor401 nhưng unavailable peer vẫn public |
| Guard | Anonymous/revoked/wrong sid/wrong stamp401; provider failure ở auth middleware503, không fallback |
| Activity ordering | Tie timestamp rồi null tail qua nhiều trang trong fixture riêng; metadata giả lập đã restore,0message; proof writer thật còn chờ P2 |
| Frontend API unit / build | 12/12; Vite và Docker FE/API build đạt |
| Edge E2E | 12/12;6 search regression +6 inbox, real fault503/retry, late A fetch ignoring abort, C reload, single select/open existing |
| API/DB helper | Compare A25/B1/C1/K0; Smoke cursor chéo400/anon401/counts giữ25:50:0 |
| Restart / fault | Persistence giữ25 IDs và cursor20+5; FaultProbe503 AUTHORITY_UNAVAILABLE → recovery200; DB/keys giữ nguyên |
| Docs / PS5.1 | Check docs/link, parity20prompt/80case, parser và recipe; không ghi token vào artifact |

Các lần sửa trong task: fixture deleted_at cần status3 cùng constraint schema; bổ sung using cho test; helper đổi tên hàm để tránh alias compare và sửa array JSON trên PS5.1. Một phiên test bị helper compare in nhầm đã được logout trong finally. Suite và recipe cuối đã chạy lại. Trạng thái kết quả agent không thay phần người dùng.

Lệnh kiểm tra độc lập:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action BackendTests
Set-Location clients\WebClient
npm.cmd test
npm.cmd run build
npx.cmd playwright test --config playwright.dm-p1-t03.config.js
```

Không restart/build API/FE giữa E2E; suite tự bật/tắt fault ở ca cuối. Trace/HAR/video/screenshot tắt để không lưu login/token. Report nằm ở `.dm-acceptance/e2e-p1-t03/results.json`; mọi evidence path tính từ worktree.

API image `sha256:422b623719f9ffd33b8017f583b8a47481bd05dff3faf52bb640fe13c298ffc7`; FE image `sha256:c97d31565802f9cf3758033007155ce7a3eab71a18505eb0331065570ae69686`; bundle đang phục vụ `index-CdQ_UjkV.js`. Config/migration, ID fixtures, recipe và các suite kể trên đã được chạy; latest backend50/50 và Edge12/12 không skip. Hồ sơ docs/link:92 Markdown/2212 local links/0errors; catalogue20task/80case giữ parity.

## Người dùng tự kiểm tra và gate

| Case | FE / BE / DB agent | FE người dùng | BE người dùng |
|---|---|---|---|
| C01 | PASS theo recipe trên | Chưa xác nhận | Chưa xác nhận |
| C02 | PASS theo recipe trên | Chưa xác nhận | Chưa xác nhận |
| C03 | PASS; client injection và fault API thật ghi riêng | Chưa xác nhận | Chưa xác nhận |
| C04 | PASS; response A thật được giữ ở client boundary | Chưa xác nhận | Chưa xác nhận |

Quyền push task/merge message đã có. P1-T03 chỉ push nhánh task; merge `--no-ff` vào message sau người dùng PASS FE/BE trên build này, fetch kiểm tra remote SHA/ancestry và smoke sau merge. Chưa được người dùng giao P2-T01. Chức năng gửi/lịch sử/Hub và thứ tự gần đây bằng tin thật chưa được nghiệm thu.

Phản hồi cần ghi: `PASS DM-P1-T03 | FE: đạt | BE: đạt | Build: <SHA>`; nếu FAIL, kèm case/bước/expected/actual. Agent sửa cùng branch và bàn giao lại; không chuyển task khi chưa có PASS.

## Xác nhận người dùng09/10/2026

Người dùng phản hồi: “tôi nghiệm thu và thực hiện DM-P4-T01”, sau khi được thông báo bản P1-T03 `9e47033` còn chờ nghiệm thu. Ghi nhận nghiệm thu bản đã bàn giao; các bảng Chưa xác nhận phía trên giữ hồ sơ lúc bàn giao, không tự bịa log chi tiết từng bước. Code triển khai là `970f03b`, bản bàn giao `9e47033` chỉ thêm docs. Quyền tích hợp P1-T03 vào message đã có; chưa có code hoặc bằng chứng nghiệm thu P2/P3 để bắt đầu phần phụ thuộc P4-T01.

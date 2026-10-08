# DM-P1-T01 — tìm và chọn người nhận

Trạng thái: **Chờ người dùng test FE/BE**. Chỉ thực hiện P1-T01. P0 đã được bạn trả lời “PASS FE/BE và đồng ý baseline P0” ngày 07/10/2026; đã merge/push vào `message` tại `170d959`. P1 chưa được người dùng PASS, chưa merge và chưa thực hiện P1-T02.

## Bản chạy và phạm vi

| Trường | Giá trị |
|---|---|
| Branch / base | `feat/dm-p1-t01-user-search` / `message` tại `170d95933190d71df74de6ebcc537a5a8c41ce56` |
| Worktree thực thi | `E:\Project\SCDC\dm-message-integration`; repository gốc `E:\Project\SCDC\scdc` giữ nguyên thay đổi người dùng |
| Commit implementation đầu tiên (07/10) | `ca75a0d3ed96b14249d1794f91340808638d33c2` |
| Commit bàn giao / build | Bản sửa chọn nhiều người ngày08/10: đọc `git rev-parse HEAD` trong worktree hoặc `.dm-acceptance/runs/baseline/p1-build.json`. Metadata ghi commit bàn giao, image IDs, test counts và schema hash. Backend/schema vẫn từ implementation07/10; frontend đã dựng lại và chạy6 E2E trên source mới |
| Remote / PR target | `origin/feat/dm-p1-t01-user-search`; base `message`. Implementation ban đầu `ca75a0d`; bản sửa08/10 được push trên cùng nhánh, remote SHA bàn giao phải trùng metadata. Chưa tạo PR, chưa merge P1 |
| Frontend | http://localhost:15300 |
| API / Swagger | http://localhost:15026/api/v1 / http://localhost:15026/swagger |
| Proxy Swagger | http://localhost:15300/swagger |
| PostgreSQL | `localhost:15432`, DB `scdc_dm_acceptance_test`, user `scdc_dm_test`; password riêng trong `.env.dm-test` |
| Stack / volume | `scdc-dm-acceptance` / `scdc-dm-acceptance_dm-postgres-data`; giữ volume/IDs P0 |
| Schema upgrade | `20261007_dm_p1_user_search.sql` bổ sung search key; backfill bằng .NET, không chạy lại schema.sql trên volume cũ |
| Dataset | Run `baseline`, 28 user thật; manifest `.dm-acceptance/runs/baseline/manifest.json` không chứa token |
| DM baseline | `direct_conversations=0`, `space_members=0`, `messages=0`; search/select không tạo các hàng này |
| Keyring | `.dm-acceptance/keyrings/cursor` mount persist; application `SCDC.UserSearch.dm-acceptance`, purpose `Messaging.UserSearch.v1`; cursor encrypted/authenticated, TTL24h |

Search do **Identity** thực hiện qua `IUserSearchDirectory`; recipient phải active, chưa deleted, email primary verified, khác actor. Actor/session/stamp/expiry được kiểm tra ở middleware và directory. Response chỉ `id`, `username`, `displayName`; không email, bio hoặc security state.

Search key là trim → NFC → .NET `ToLowerInvariant`, giữ dấu; displayName gốc được giữ. Key được cập nhật cùng profile trong `IdentityDbContext.SaveChangesAsync`. SQL dùng escaped `LIKE` và `COLLATE "C"`; rank username exact trước, rồi normalized username/UUID. Phân trang lấy limit+1, mặc định20/tối đa50. Profile thay đổi giữa trang có thể thay membership; refresh từ đầu. Cursor bind actor/normalized q/limit/position và deadline của trang đầu; cursor không cấp quyền truy cập.

UI có loading, empty, error/thử lại, tải thêm, phân biệt username và lựa chọn theo ID. Điều chỉnh 08/10/2026: chọn đồng thời nhiều người, hiện danh sách đã chọn và số lượng; đổi q/tải thêm/lỗi không xóa lựa chọn. Bấm người đã chọn lần nữa hoặc nút × để bỏ riêng; Bỏ chọn tất cả dọn hết. Lựa chọn chỉ trong bộ nhớ modal, đóng/mở lại hoặc đổi actor sẽ dọn; không tạo conversation ID. Các phần inbox/chat mẫu cũ ngoài modal chưa là DM runtime; AC-DM-01 mới có lát cắt tìm/chọn. Tạo hội thoại thuộc P1-T02.

## Yêu cầu bổ sung và kết quả C01 ngày 08/10/2026

Người dùng báo tìm `Bảo` không ra kết quả ở FE15300. Chẩn đoán runtime: container được chạy từ checkout `scdc` P0; Swagger không có search và request trả404. Đã dựng lại P1 qua `user-search.ps1 -Action Upgrade` ở worktree `dm-message-integration`, giữ 28 tài khoản/IDs, cursor keys và volume; migration idempotent. Smoke API/DB C01–C04 và browser C01 đã chạy PASS trước yêu cầu chọn nhiều người. Proof runtime cũ ở `.dm-acceptance/runs/baseline/p1-c01-recovery.json`; không thay xác nhận người dùng.

Lượt test tiếp: người dùng thấy2 kết quả nhưng chọn người thứ hai làm mất người thứ nhất, xác nhận modal chỉ có dòng “Đã chọn Bảo Demo (@...)”. Đây là giao diện chọn đơn cũ; backend và FE container hiện tại đúng P1 chọn nhiều người. Agent chạy lại2 ca C01 trên trình duyệt sạch đều PASS. Tab đã mở/cached entry có thể còn giữ bundle cũ; không thể thay JS đang chạy chỉ bằng cập nhật container.

Đã bổ sung `Cache-Control: no-store` cho entry `index.html`, tắt ETag/If-Modified-Since ở entry, giữ headers bảo vệ và cache asset có hash. Nginx `-t` PASS; root, index và SPA fallback trả200/no-store kể cả conditional headers cũ; JS hash mới vẫn200/immutable. Chạy lại6 E2E trên FE sau sửa cache. User cần đóng tab cũ, mở URL mới bên dưới hoặc Ctrl+F5 để nhận entry/bundle mới; user chưa xác nhận PASS.

Mở bản mới: http://localhost:15300/?dm-ui=multi-select-20261008 . Login A và tìm `Bảo`; chọn B/C phải hiện **Đã chọn 2 người**, hai thẻ username có nút × và **Bỏ chọn tất cả**. Nếu vẫn chỉ có dòng tên một người, đang chạy giao diện cũ; ghi URL/actor thực tế và không dùng kết quả đó để nghiệm thu build mới.

Người dùng chốt “người bạn gần nhất” là **người vừa nhắn tin**, cần lịch sử hội thoại thật. Phần này được đưa vào P1-T03 (inbox/modal lấy peer theo `lastActivityAt`) và kiểm thử thứ tự sau writer P2-T01. Hiện DB có0 hội thoại/0 tin; chưa hiển thị danh sách gần đây từ mock hoặc lượt chọn. [Kế hoạch](../../plans/direct-messaging.md) và [prompt đầy đủ](../../plans/direct-messaging-prompts.md) đã cập nhật phụ thuộc/test. Chọn nhiều người hiện là lựa chọn UI; contract một DM/hai participant giữ nguyên, group chat chưa được triển khai.

## Chuẩn bị, start/stop và tài khoản

Mở PowerShell tại worktree thực thi. Không đổi sang compose.yaml hoặc DB ứng dụng chính để chạy các ca dưới đây.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
git branch --show-current
git rev-parse HEAD
# Stack hiện đã chạy và migration đã áp dụng. Lần dựng/upgrade tiếp theo:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/user-search.ps1 -Action Upgrade
# Start/restart không xóa volume hoặc keyring:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Setup -Run baseline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Status
# Dùng khi cần; không chạy Stop trước lúc test FE/BE:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Restart
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Stop
```

`Upgrade` kiểm tra DB/target, build API/FE, chạy migration idempotent, backfill khóa trong batch có row lock, chuyển sang API mới rồi reconcile lần nữa. Không ghi lại displayName/IDs hoặc regenerate keys. Fresh DB có column trong schema; các account tạo qua API có key ngay. Với DB có seed/profiles cũ phải chạy initializer trước khi phục vụ search. Helper chỉ cho DB acceptance; production cần deployment migration/command tương ứng, không dùng helper này.

| Alias | Username | Tên hiển thị / trạng thái |
|---|---|---|
| A | `dm_demo_an` | An Demo, active/verified; actor chính |
| B | `dm_demo_bao` | Bảo Demo, active/verified |
| C | `dm_demo_chi` | Bảo Demo, active/verified, trùng tên B |
| U | `dm_demo_pending` | Pending/unverified; login403 |
| K | `dm_demo_khoa` | Active/verified trong demo; không khóa account này ở P1 |
| S01–S23 | `dm_demo_search01` … `dm_demo_search23` | Active/verified, toàn bộ kết quả `dm_demo_search` trong run này |

Password fixture: **`DmDemo2026!Local`**. Email example.test không nhận thư thật. IDs lấy từ manifest/response. A/B dùng profile browser riêng nếu test hai actor. Dữ liệu disabled/deleted/unverified-active và các tên có ký tự `%/_/\` được tạo trong integration fixture riêng, kiểm tra rồi dọn đúng các ID của fixture; không có production bypass/admin API hoặc helper khóa K.

## Thiết lập backend chung — token giữ trong bộ nhớ

Trong cửa sổ PowerShell mới, chạy nguyên block dưới đây. Không in `$a`, `$b`, header hoặc token ra terminal/ảnh/report.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
Import-Module ./scripts/dm-acceptance/Common.psm1 -Force -DisableNameChecking
Assert-DmDatabase
Assert-DmApiTarget
$manifest = Get-DmManifest -Run baseline
$a = Connect-DmActor -Alias A -Run baseline -DeviceName 'DM-P1-user-BE-A'
$b = Connect-DmActor -Alias B -Run baseline -DeviceName 'DM-P1-user-BE-B'
function Search-DmUser([string]$Q, [int]$Limit=20, [string]$Cursor, $Actor=$a) {
    $path = '/users/search?q=' + [Uri]::EscapeDataString($Q) + '&limit=' + $Limit
    if ($PSBoundParameters.ContainsKey('Cursor')) { $path += '&cursor=' + [Uri]::EscapeDataString($Cursor) }
    Invoke-DmRequest -Method GET -Path $path -AccessToken $Actor.accessToken
}
$countsSql = 'SELECT (SELECT count(*) FROM messaging.direct_conversations), (SELECT count(*) FROM messaging.space_members), (SELECT count(*) FROM messaging.messages);'
$baselineCounts = Invoke-DmReadSql $countsSql
$baselineCounts # Kỳ vọng 0|0|0; ghi baseline thực tế nếu đã có dữ liệu từ task khác.
```

Swagger: login A qua `POST /auth/login`, copy accessToken chỉ trong máy → Authorize → `GET /users/search`. Điền q/limit/cursor tương ứng; JSON và status phải giống helper. Không chụp accessToken trong ảnh. Swagger sinh từ source có endpoint thật; file OpenAPI DM chỉ đánh dấu search có implementation, các operation DM khác vẫn dự thảo.

## DM-P1-T01-C01 — tên trùng và chọn đúng người

Điều kiện: A/B/C như bảng; lane FE và BE dùng cùng dữ liệu, không có mutation search. Baseline counts đã chụp.

1. FE: mở http://localhost:15300, login A. Bấm biểu tượng **Direct Messages** bên trái → nút **+** cạnh TIN NHẮN TRỰC TIẾP (tooltip “Tạo cuộc trò chuyện trực tiếp (DM)”). Modal **Tìm người nhận** mở.
2. Nhập `Bảo`. Mong đợi đúng hai dòng Bảo Demo, kèm `@dm_demo_bao` và `@dm_demo_chi`; không email.
3. Bấm `@dm_demo_bao`: phần **Người đã chọn** hiện Bảo và “Đã chọn 1 người”. Bấm thêm `@dm_demo_chi`: hiện2 thẻ khác username, “Đã chọn 2 người”; cả hai dòng kết quả có dấu ✓. Modal vẫn mở, inbox không có dòng mới.
4. Đổi q thành `dm_demo_bao`: chỉ có B trong kết quả, cả hai thẻ B/C vẫn giữ. Bấm kết quả B lần nữa → chỉ còn thẻ C; bấm B lại → có2 thẻ, không trùng B. Bấm × có nhãn **Bỏ chọn @dm_demo_chi** → chỉ còn B. Bấm **Bỏ chọn tất cả** → hết thẻ và kết quả B hết dấu ✓.
5. Chọn B lại; đổi q `dm_demo_search`, chọn S01, bấm **Tải thêm** rồi chọn S23 → có3 thẻ B/S01/S23. Đổi thành `dm_demo_an`, rồi `dm_demo_pending`: kết quả rỗng nhưng3 thẻ vẫn giữ. Thử chặn request search như C04: lỗi và retry không xóa3 người đã chọn.
6. Bấm **Đóng**, mở lại modal → không có người đã chọn; reload hoặc logout/login tài khoản khác cũng không giữ lựa chọn cũ. Đây là bản nháp trong modal, không phải danh sách người vừa nhắn tin.
7. BE: chạy block sau, ghi status/items và IDs so manifest. Chọn/bỏ chọn không phát sinh POST tạo hội thoại; GET search không đổi response hoặc DB counts.

```powershell
$r = Search-DmUser 'Bảo'
$r.status                         # 200
$r.body.items | Format-Table -AutoSize # B và C; chỉ id/username/displayName
$manifest.accounts | Where-Object { $_.alias -in @('B','C') } | Select-Object alias,id,username | Format-Table -AutoSize
$exact = Search-DmUser 'DM_DEMO_BAO'
$exact.status                     # 200
$exact.body.items[0].username      # dm_demo_bao
foreach ($q in 'dm_demo_an','dm_demo_pending') {
    $s = Search-DmUser $q
    [pscustomobject]@{q=$q;status=$s.status;count=@($s.body.items).Count} | Format-List # 200,0
}
Invoke-DmReadSql $countsSql        # Không đổi so baseline
```

DB đối soát tên/IDs/trạng thái, chỉ đọc:

```powershell
$identitySql = @'
SELECT u.id,u.username,p.display_name,p.display_name_search_key,u.status,
       e.verified_at IS NOT NULL AS verified
FROM identity.users u
JOIN identity.user_profiles p ON p.user_id=u.id
JOIN identity.user_emails e ON e.user_id=u.id AND e.is_primary
WHERE u.username IN ('dm_demo_an','dm_demo_bao','dm_demo_chi','dm_demo_pending')
ORDER BY u.username COLLATE "C";
'@
Invoke-DmReadSql $identitySql
```

Bằng chứng: build/run, ảnh modal kết quả/selection không token, JSON public fields, IDs khớp manifest và counts không đổi. Agent: **Đạt** qua real API, browser và DB. Người dùng: **Chưa xác nhận**. Sau ca giữ fixture, không tạo DM hoặc reset DB.

## DM-P1-T01-C02 — 20 + 3 kết quả

1. FE: nhập `dm_demo_search`, đợi20 dòng `search01`…`search20`. Bấm **Tải thêm** →23 dòng, xuất hiện `search21`…`search23`, không trùng, nút Tải thêm biến mất.
2. Đổi sang q không kết quả rồi trở lại `dm_demo_search`: trang đầu20 dòng, không cộng dồn trang cũ. Reload, mở modal và tìm lại: hành vi tương tự.
3. BE: dùng đúng cursor trả về; giữ q/limit/actor của trang đầu.

```powershell
$page1 = Search-DmUser 'dm_demo_search'
$cursorA = $page1.body.nextCursor  # Giữ trong bộ nhớ, không tự dựng/giải mã
$page2 = Search-DmUser 'dm_demo_search' -Cursor $cursorA
[pscustomobject]@{status1=$page1.status;count1=@($page1.body.items).Count;status2=$page2.status;count2=@($page2.body.items).Count;lastCursorIsNull=($null -eq $page2.body.nextCursor)} | Format-List
# 200/20, 200/3, True
$union = @($page1.body.items.id) + @($page2.body.items.id)
@($union | Select-Object -Unique).Count # 23
$page1.body.items.username
$page2.body.items.username
$all = Search-DmUser 'dm_demo_search' -Limit 50
@($all.body.items).Count              # 23; nextCursor null
```

```powershell
$pageSql = @'
SELECT u.id,u.username
FROM identity.users u JOIN identity.user_profiles p ON p.user_id=u.id
WHERE u.status=1 AND u.deleted_at IS NULL
  AND EXISTS (SELECT 1 FROM identity.user_emails e WHERE e.user_id=u.id AND e.is_primary AND e.verified_at IS NOT NULL)
  AND u.normalized_username COLLATE "C" LIKE '%dm\_demo\_search%' ESCAPE '\'
ORDER BY u.normalized_username COLLATE "C",u.id;
'@
Invoke-DmReadSql $pageSql # 23 rows; IDs/order khớp union trang1+2
Invoke-DmReadSql $countsSql
```

Agent: **Đạt**, gồm limit50 và restart cursor. Người dùng: **Chưa xác nhận**. Giữ dataset; không xóa tin/hội thoại để lặp ca. Profile thay đổi trong lúc test thì tìm lại từ trang đầu trước đối soát.

## DM-P1-T01-C03 — biên, Unicode và wildcard literal

1. FE: `x` → hướng dẫn minimum, không có kết quả cũ. `dm` → kết quả thật; `BẢO`, `Bảo`, `Ba` + U+0309 + `o` → B/C.
2. `Bao` → **B vẫn khớp qua username `dm_demo_bao`**, C không xuất hiện chỉ nhờ displayName có dấu. Không coi có B là lỗi bỏ dấu.
3. Paste64 ASCII `a` → query hợp lệ, empty; paste65 `a` → báo tối đa64 và giữ input để sửa. Emoji32 =64 UTF-16 hợp lệ; emoji33 =66 bị từ chối.
4. BE: chạy cả status hợp lệ/lỗi; `%`, `_`, backslash được tìm như text, không wildcard SQL.

```powershell
foreach ($q in 'x','dm',('a'*64),('a'*65),'BẢO','Bảo',('Ba'+[char]0x0309+'o'),'Bao','%%','__','\\') {
    $r = Search-DmUser $q
    [pscustomobject]@{q=$q;units=$q.Length;status=$r.status;errorCode=$(if($r.status -eq 400){$r.body.errorCode}else{''});count=$(if($r.status -eq 200){@($r.body.items).Count}else{0})} | Format-List
}
# x/65a: 400 Common.ValidationFailed; các q khác:200.
# Bao: B; BẢO/Bảo/NFD: B,C. '__' khớp 0 vì username chỉ một underscore liên tiếp.
# %% và hai backslash:0; không trả toàn bộ users như wildcard.
$emoji=[char]::ConvertFromUtf32(0x1F600)
(Search-DmUser ($emoji*32)).status   # 200, 64 UTF-16
(Search-DmUser ($emoji*33)).status   # 400, 66 UTF-16
foreach ($limit in 0,51) {
    $r = Search-DmUser 'dm_demo_search' -Limit $limit
    [pscustomobject]@{limit=$limit;status=$r.status;errorCode=$r.body.errorCode} | Format-List #400 Common.ValidationFailed
}
$missing = Invoke-DmRequest GET '/users/search' -AccessToken $a.accessToken
$badNumber = Invoke-DmRequest GET '/users/search?q=dm&limit=abc' -AccessToken $a.accessToken
$missing.status; $badNumber.status #400 Common.ValidationFailed
Invoke-DmReadSql $countsSql
```

Integration fixture tạo tên `P%_\ <run>` và decoy `PXYY <run>` để chứng minh literal dương tính, cùng tên NFC/NFD và profile update `ĐẶNG`. Fixture chỉ trong DB `_test`, dọn đúng IDs sau test; không yêu cầu người dùng tự đổi tên B/C. UTF-16 lỗi `a\ud800` được kiểm tra trực tiếp qua Contracts vì URL không biểu diễn surrogate lỗi nguyên trạng; có proof directory validation, không giả đây là request browser.

Agent: **Đạt** theo backend integration/real HTTP/E2E. Người dùng: **Chưa xác nhận**. Giữ source displayName gốc, không sửa fixture để làm số lượng kết quả khớp kỳ vọng sai.

## DM-P1-T01-C04 — cursor, lỗi API và phản hồi cũ

Điều kiện: có `$cursorA` từ C02; `$a` và `$b` là hai login độc lập. Không login B rồi dùng token A làm proof chéo actor.

```powershell
$badQ = Search-DmUser 'Bảo' -Cursor $cursorA
$badLimit = Search-DmUser 'dm_demo_search' -Limit 10 -Cursor $cursorA
$badActor = Search-DmUser 'dm_demo_search' -Cursor $cursorA -Actor $b
$badToken = Search-DmUser 'dm_demo_search' -Cursor ($cursorA+'tampered')
$emptyToken = Search-DmUser 'dm_demo_search' -Cursor ''
foreach ($r in $badQ,$badLimit,$badActor,$badToken,$emptyToken) {
    [pscustomobject]@{status=$r.status;errorCode=$r.body.errorCode} | Format-List #400 CURSOR_INVALID
}
(Search-DmUser 'dm_demo_search' -Actor $b).status #200: search mới của B, không cursor A
$anonymous = Invoke-DmRequest GET '/users/search?q=dm_demo_search'
$anonymous.status; $anonymous.body.errorCode #401 Common.Unauthorized
# Revoke đúng session BE-A và dùng token cũ; FE-A vẫn là session riêng.
$logoutA = Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
$revokedA = Search-DmUser 'dm_demo_search' -Actor $a
$logoutA.status; $revokedA.status; $revokedA.body.errorCode #204/401/Common.Unauthorized
Invoke-DmReadSql $countsSql #không đổi
```

FE lỗi mạng tự kiểm tra:

1. Trong modal, nhập `zzz_no_dm_person` → empty state.
2. Edge/Chrome F12 → Network request blocking, bật pattern `*api/v1/users/search*`. Đổi q sang `dm_demo_search` → “Không tải được kết quả. Hãy thử lại.”, input giữ nguyên, không có người mock.
3. Tắt request blocking, bấm **Thử lại** →20 kết quả thật. Đây là lỗi mạng ở client, không phải server503.
4. Ca server503 và delay có chủ ý được chạy bằng Playwright route fixture, không có developer controls trong UI sản phẩm. Tự chạy browser có cửa sổ:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration\clients\WebClient
npm.cmd run test:dm-p1 -- --headed -g C04
# Gồm: injected503 → retry API thật; response Bảo bị giữ đến sau response dm_demo_bao.
# Cả hai phải pass; report phân loại rõ fault client, không là proof backend503.
```

Sau delay, query/results cuối thuộc `dm_demo_bao`, không xuất hiện C trong kết quả từ response cũ; danh sách người đã chọn trước đó giữ nguyên. Runner tắt route fixture trong test, logout session riêng sau mỗi ca; không trace/HAR/token dump. Cursor TTL được kiểm tra bằng TimeProvider +25h trong host integration (không đổi đồng hồ hệ thống), cursor invalid và session vẫn hợp lệ. Cursor dùng cùng session sống qua restart API thật:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/user-search.ps1 -Action Persistence
Get-Content .dm-acceptance/runs/baseline/p1-search-persistence.json -Raw -Encoding UTF8
```

Agent: **Đạt**. Người dùng: **Chưa xác nhận**. Sau test phải tắt request blocking, giữ keyring/data/manifest, không reset password hoặc xóa volume để làm token/cursor hợp lệ lại.

## Đầu ra và bằng chứng agent

| Phần | Proof đã chạy | Kết quả | Người dùng |
|---|---|---|---|
| Backend toàn bộ (07/10, BE source không đổi) | .NET10/container + PostgreSQL thật, `.dm-acceptance/backend-artifacts/test-results/dm-acceptance.trx` | 31/31 | Chưa xác nhận |
| Frontend unit | `npm.cmd test`, API encode/cursor/abort và503 không retry/mock | 8/8 | Chưa xác nhận |
| FE production build / API publish | Vite build + Docker .NET publish | Đạt | Chưa xác nhận |
| C01/C02/C03/C04 browser | Edge154.0.4258.53, 6 Playwright tests trên FE mới08/10, `.dm-acceptance/e2e-p1/results.json`; `browser-metadata.json` và ảnh modal `multi-recipient-selection.png` không token | 6/6 | Chưa xác nhận |
| HTTP/DB smoke C01–C04 (chạy lại08/10) | `.dm-acceptance/runs/baseline/p1-search-smoke.json` | Đạt, counts0/0/0 không đổi | Chưa xác nhận |
| Cursor restart | Cùng actor/session/cursor qua restart API thật; `p1-search-persistence.json` | Đạt, trang2 vẫn3 IDs | Chưa xác nhận |
| Disabled/deleted/unverified-active | Dedicated integration fixture, SQL trạng thái đúng enum/constraint; API search không trả recipient không hợp lệ | Đạt | Chưa xác nhận; manual K disabled thuộc P6-T02 |
| UTF-16 lỗi / guard Contracts | Gọi directory với surrogate lỗi, sid/stamp sai; không qua mock HTTP | Validation400 / unauthorized | Chưa xác nhận |

Helper setup đã sửa đọc commit với safe.directory chỉ áp dụng cho đúng path trong từng lệnh, preflight trước khi ghi manifest; không đổi Git config toàn máy. Stop/start/setup/restart và upgrade idempotent đã chạy lại trên stack acceptance.

Bản sửa08/10: giữ nhiều recipient theo ID, không mất khi đổi q/tải thêm/lỗi; xóa riêng hoặc tất cả; đóng modal dọn draft. Unit8/8, build và browser6/6 PASS; không đổi backend/schema. Những proof cursor-restart/integration07/10 là lịch sử cho source BE không đổi, không ghi đã chạy lại toàn bộ31 tests ngày08/10. User chưa test/PASS build mới.

Lỗi đã sửa/kiểm chứng: explicit `cursor=` trước đây bị MVC biến thành null; nay trả400 CURSOR_INVALID. Test fixture dùng đúng Disabled3/Deleted4 và kỳ vọng `Bao` phân biệt khớp username với bỏ dấu displayName. Không còn test fail trên build bàn giao. npm advisory cũ `source-map-js` được ghi ở P0, không cập nhật dependencies ngoài scope P1.

Keyring local lưu file theo [Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0); local filesystem key chưa có chứng chỉ bảo vệ at rest. Cấu hình production phải chọn application name theo môi trường, volume/ACL/key protection và backup; task này không phát hành production hoặc nghiệm thu restore/rotation của toàn DM. Stack chính có khai báo volume cursor bền và image tạo thư mục cho APP_UID; chưa chạy/upgrade DB `scdc_chat` trong phiên này.

## Cleanup và phản hồi người dùng

Cuối phiên BE, logout B; A đã logout trong C04. Nếu bỏ qua C04, logout cả hai. Không in sessions.

```powershell
$null = Invoke-DmRequest POST /auth/logout @{refreshToken=$b.refreshToken}
# Nếu chưa logout A ở C04:
# $null = Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
```

Ghi rõ kết quả từng C01–C04, FE và BE, build đã test. Mẫu phản hồi:

```text
PASS DM-P1-T01 | Build: <commit từ p1-build.json/git rev-parse HEAD>
FE C01/C02/C03/C04: đạt
BE C01/C02/C03/C04 và DB: đạt
```

```text
FAIL DM-P1-T01 | Build: <commit>
Case/bước: ... | Thao tác: ... | Mong đợi: ... | Thực tế: ...
Sửa P1-T01 rồi bàn giao để tôi test lại.
```

User PASS/FAIL P1: **Chưa có**. Quyền push/merge vào `message` đã cấp; chỉ merge sau user PASS đúng build và xác minh remote. Chưa giao P1-T02. Task dừng ở **Chờ người dùng test**.

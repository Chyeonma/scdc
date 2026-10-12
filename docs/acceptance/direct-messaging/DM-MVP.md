# Chat riêng MVP — bàn giao một lần cuối

**Chờ người dùng test FE và BE toàn MVP.** Ngày12/10/2026 người dùng yêu cầu rút ngắn prompt và xác nhận: “Làm liên tục toàn bộ phần MVP còn lại; tôi test FE/BE một lần cuối.” Agent đã gộp P4/P5/P6/P7 vào một branch, làm/test/sửa liên tục. Không dùng quy trình nghiệm thu từng task của bản prompt lưu; chưa ghi người dùng PASS hoặc merge gói này vào `message`.

## Build, scope và Git flow

| Trường | Giá trị |
|---|---|
| Branch / base / PR target | `feat/dm-mvp-completion` / accepted `origin/message` `9de920a` / `message` |
| Code commit | `f0e8dd1`; bản cuối/remote SHA/PR cập nhật cuối hồ sơ |
| Worktree dùng để chạy | `E:\Project\SCDC\dm-message-integration`; giữ checkout gốc `scdc` và thay đổi người dùng |
| Clean build proof | Detached worktree `E:\Project\SCDC\dm-mvp-build` tại code commit, clean status; Docker build API và FE từ context sạch rồi Start-NoBuild với volume/keyring cũ |
| FE / API / Swagger / DB | <http://localhost:15300> / <http://localhost:15026/api/v1> / <http://localhost:15026/swagger> / localhost15432 |
| Compose / database | `scdc-dm-acceptance` / `scdc_dm_acceptance_test`, PostgreSQL18 |
| API image | `sha256:028e1c1cd3255a55d32ae0276b5c9019f01d013c0787f6c4a3ea8787df3a1117` |
| FE image / asset | `sha256:923a68141f29ada01315270c1862e9b4a2aa4ef17739c8599f61dc34ebc87084` / `index-C2CfOtsO.js` |
| Migration | `20261010_dm_p4_outbox_lease.sql`, additive/replayable; existing edited/deleted/version columns dùng cho mutations, không thêm body history |
| Keys / config | Giữ cursor/HMAC ring và DB volume; active HMAC `dm-local-v1`; JWT15min mặc định, lease15s, backoff tới32s, một API host MVP |
| Browser | Edge Chromium thật headless Windows, hai/ba context độc lập; touch/coarse viewport390×844 được ghi là emulation; không HAR/trace/video/token dump |
| Dataset bạn test | **`mvpuser` FE**, **`mvpbe` BE**; usernames/IDs/UUID thật và baseline cuối hồ sơ |
| Trạng thái | Agent kiểm chứng theo phạm vi / Người dùng **Chưa xác nhận**; một draft PR, chưa merge `message` |

Draft PR **[31](https://github.com/Chyeonma/scdc/pull/31)**, base `message`, head `feat/dm-mvp-completion`. Build source/docs đã push và `git ls-remote` xác minh `5b94f797171eed55dcb10f6b030166cd7725c5d2`; source executable `f0e8dd1`. Commit cập nhật metadata sau đó không đổi API/FE/schema/images. SHA HEAD/remote cuối được ghi ở `.dm-acceptance/runs/mvpuser/git-handover.json` và final response, có thể kiểm lại bằng lệnh dưới đây; không có merge trước user PASS.

```powershell
git -c safe.directory=E:/Project/SCDC/dm-message-integration -C E:/Project/SCDC/dm-message-integration rev-parse HEAD
git -c safe.directory=E:/Project/SCDC/dm-message-integration -C E:/Project/SCDC/dm-message-integration ls-remote origin refs/heads/feat/dm-mvp-completion refs/heads/message
```

Bạn có thể tìm/mở đúng một DM, gửi/nhận text thật, đọc lại lịch sử, retry thủ công không trùng, nhận live events, tự reconnect/bù đủ trang, sửa/xóa tin của mình, xử lý version conflict và tombstone. Draft tách conversation/actor trong RAM; Enter/ShiftEnter/IME và pointer coarse xử lý riêng. Server kiểm tra quyền/session trước read/send/mutation/subscribe/dispatch. Peer unavailable vẫn cho actor đọc/sửa/xóa/retry committed, chặn send mới. UI phiên lấy dữ liệu thật; không dùng fallback session giả.

MVP này chưa là toàn sản phẩm v1 hoặc bản public release. P8 restore/protection/retention, full-system load với Community, email/domain/người ký phát hành và thiết bị/bàn phím mobile vật lý vẫn riêng. Các policy v1 không bị bỏ; không tự ký PASS gate chưa đo. Source search/open/inbox/send/history P0–P3 đã được người dùng nghiệm thu; P4–P7 gộp vào lần nghiệm thu cuối này.

## Chạy đúng bản và dữ liệu

Mở Docker Desktop/Linux engine. Không dùng Code Runner chạy `.ps1` không tham số; truyền `-Action`. Script từ checkout `scdc` cũ có thể dựng source cũ lên cùng cổng, nên dùng worktree dưới đây.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
$mvp=Join-Path (Get-Location) 'scripts\dm-acceptance\mvp.ps1'
$rt=Join-Path (Get-Location) 'scripts\dm-acceptance\realtime.ps1'
function M([string]$Action,[string]$Case='C01',[string]$Run='mvpuser') {
  & powershell -NoProfile -ExecutionPolicy Bypass -File $mvp -Action $Action -Run $Run -Case $Case
  if($LASTEXITCODE -ne 0){throw "MVP $Action failed"}
}
$fe=Get-Content .dm-acceptance\runs\mvpuser\mvp-fixtures.json -Raw -Encoding UTF8|ConvertFrom-Json
$be=Get-Content .dm-acceptance\runs\mvpbe\mvp-fixtures.json -Raw -Encoding UTF8|ConvertFrom-Json
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dm-acceptance.ps1 -Action Status
```

Rebuild nếu source/image thay đổi: `powershell -NoProfile -ExecutionPolicy Bypass -File $rt -Action Upgrade`; Start-NoBuild không rebuild. Stop: `dm-acceptance.ps1 -Action Stop`; Restart: `-Action Restart -NoBuild`. Những lệnh này giữ DB/keyring. Không drop DB hoặc dùng `down -v` để lặp case.

Mật khẩu demo: **`DmDemo2026!Local`**. A luôn **`dm_demo_an_mvpuser`**. Bên nhận từng lane:

| Lane | Peer alias / username FE | Dùng cho |
|---|---|---|
| C01 | B / `dm_demo_bao_mvpuser` | Happy path |
| C02 | C / `dm_demo_chi_mvpuser` | Recovery101 |
| C03 | S01 / `dm_demo_search01_mvpuser` | Edit/no-op/validation |
| C04 | S02 / `dm_demo_search02_mvpuser` | Delete/replay |
| C05 | S03 / `dm_demo_search03_mvpuser` | Hai phiên A xung đột |
| C06 | S04 / `dm_demo_search04_mvpuser` | Worker pause |
| C07 | S05 / `dm_demo_search05_mvpuser` | Outsider/negative matrix |
| C08 | S06 / `dm_demo_search06_mvpuser` | Thu hồi exact session A2 |
| C09 | S07 / `dm_demo_search07_mvpuser` | Draft/IME desktop |
| C10 | K / `dm_demo_khoa_mvpuser` | Peer disabled |
| C02P1/C02P2/C02LIVE | S08/S09/S10 | Gián đoạn giữa trang và live chen giữa |
| MOBILE/EDITOLD/DELETEOLD | S11/S12/S13 | Mobile viewport và tin cũ offline |

BE dùng suffix **`mvpbe`**, ví dụ `dm_demo_an_mvpbe`. U là `dm_demo_pending_<run>` theo manifest, login bị chặn. Outsider C là Chi cho mọi cặp trừ C02; với C02 dùng S23 vì Chi là member. Email `example.test` chỉ dữ liệu local.

Mỗi lane FE/BE được tạo bằng API thật và bàn giao trống: messages/operations/createOutbox/outbox0, counter`"0"`, editHistory/bodyCopies0. Không dùng global counts của DB chứa nhiều run. `X/Y` manifest chỉ là UUID đã chuẩn bị; send FE tự tạo UUID trừ khi bạn arm fixture. Replay FE phải lấy UUID **thực sự đã gửi** từ Snapshot/response, không tự dùng X khác rồi kỳ vọng không thêm tin.

Mở cặp: hai browser profile độc lập (Chrome+Edge/InPrivate hoặc hai profile); cùng profile/tabs dùng chung account session. B login trước, +→nhập đúng username An→chọn đúng dòng username→Mở hội thoại. A làm tương tự với peer của case. Chờ **Đã kết nối cập nhật**, hết đồng bộ và đúng tên người nhận trước nhập. “Đã gửi” nghĩa DB đã lưu, không là receipt nhận/đọc.

## Bộ testcase FE/BE cuối

Tất cả kết quả người dùng: **Chưa xác nhận**. Agent evidence được ghi ở mục sau; mỗi ca dùng lane riêng.

### MVP-C01 — Happy path và persistence

1. A/B mở C01 trống; A gửi đúng `MVP-M01` một lần. A một dòng Đã gửi, B một dòng mới không reload; đối chiếu data-message-id/sequence/version trong Elements và WS frame. B không GET polling để nhận tin.
2. Reload B/A, mở lại đúng cặp nếu cần: một ID cũ/version1/body cũ. Restart stack rồi mở lại: ID giữ.
3. `M Snapshot C01`:1message,1operation,1createOutbox, counter1, bodyCopies/editHistory0, pending cuối0.
4. BE độc lập: `node scripts\dm-acceptance\mvp-backend.mjs mvpbe C01`; actual POST/WS DTO khớp, outsider subscribe404/no frame. Lặp trên run mới nếu đã có tin.

### MVP-C02 — Automatic reconnect101, interrupted pages và live event

1. A/B mở C02 trống. B mở DevTools Network chọn Offline; để chắc transport đóng, cài [realtime-faults.js](../../../scripts/dm-acceptance/realtime-faults.js) trước login rồi gọi `.on({conversationId:...})` và `.disconnect()` sau chọn Offline. Đây là transport fault local, không là fake REST response.
2. PowerShell `M Seed101 C02`:101 POST thật `RC-001…RC-101`, receipt ID/sequence lưu `seed-C02.json`. B chưa nhận live khi offline.
3. Network Online: SDK tự nối lại/subscribe/GET after. Đợi hết đồng bộ: đủ101 dòng đúng thứ tự, không duplicate; Network GET after sizes50/50/1, through101 cố định; resumeCursor chỉ trang cuối. Không bấm Gửi lại.
4. Variants dùng C02P1/C02P2 mới, Seed101 riêng. Agent runner giữ request trang2/3 trước trả, ngắt transport rồi nối lại: progress chỉ từ trang đã merge, tập ID cuối khớp101. Dùng E2E runner để tạo barrier lặp được, không giả manual throttle là proof interruption đúng điểm.
5. C02LIVE mới: agent giữ trang1, writer gửi `RC-102` và actual WS frame tới trước release; after vẫn through101/50/50/1, FE cuối102 dòng. Live event không đẩy protected frontier qua dữ liệu chưa đọc.
6. BE: `node scripts\dm-acceptance\mvp-backend.mjs mvpbe C02` dùng actual101 writers và protected pages. Query Snapshot FE/BE đều101 hoặc102 theo variant,1operation/createOutbox mỗi message.

### MVP-C03 — Sửa, no-op, Cancel và validation

1. A/S01 mở C03. A gửi `EDIT-ORIGINAL`; chọn Sửa→`EDIT-V2`→Lưu thay đổi, rồi `EDIT-V3`. B/A thấy body cuối và Đã sửa không reload; version1→2→3, ID/sequence không đổi.
2. Sửa lần nữa giữ `EDIT-V3` và Lưu: version/editedAt/outbox không đổi. Sửa thành `CANCELLED` rồi Hủy sửa: không PATCH, body cuối giữ.
3. Edit2.001 ký tự `a`: lỗi giới hạn, không mutation.2.000, emoji/CRLF/LF dùng actual API/validator corpus; không trim body. B không có menu sửa A.
4. Snapshot cuối happy path:1message/1operation/createOutbox1/outbox3/counter1/version3/editHistory0/bodyCopies0. BE helper C03 thử stale version409, non-author403 và validation400. Không có API/content history cũ.

### MVP-C04 — Xóa, tombstone và original retry

1. A/S02 mở C04, gửi `DELETE-ORIGINAL`. Xóa→Hủy xóa giữ body; Xóa→Xác nhận xóa: hai phía “Tin nhắn đã bị xóa”, đúng ID/sequence/tác giả/vị trí, version2/contentnull. Không còn Sửa/body cũ.
2. DELETE lặp cùng author kể cả version1: current tombstone/version2, không thêm outbox. PATCH tombstone409 `MESSAGE_DELETED`; B DELETE A403 `AUTHOR_REQUIRED`, outsider404.
3. Replay original POST đúng original actor/UUID/`DELETE-ORIGINAL`:200 cùng tombstone; không hồi sinh hoặc thêm message/createOutbox. Reload vẫn tombstone.
4. Snapshot:1/1/create1/outbox2/counter1/version2. BE helper C04 thực hiện đủ những request này, giữ IDs/versions/counts.

### MVP-C05 — Conflict và current tin cũ sau offline

1. Hai profile A1/A2 cùng login An, mở C05; A1 gửi `RACE-ORIGINAL`. Cả hai editor bắt đầu từ version1. A1 lưu `RACE-WINNER`; A2 lưu nội dung cũ:409 `VERSION_CONFLICT`, UI tải current và không ghi đè. Repeat edit-delete từ same expectedVersion trong BE deterministic barrier test; một transition thắng, loser409.
2. Runner FE cố tình drop actual frames ở A2 để giữ version đang edit; không dùng fake successful PATCH. Stale delete giữ version lúc mở confirmation, không tự lấy version mới để xóa âm thầm.
3. EDITOLD lane: B đã thấy `OLD-M01` và giữ cửa sổ, offline; A sửa `CURRENT-OLD-M01`, Seed101 rồi B online:102 ID, M01 version2/body current bằng API PATCH thật, không chỉ SQL fixture.
4. DELETEOLD lane: tương tự bằng DELETE thật; B đủ102 và M01 tombstone. Stale local application frame version1 có nhãn không thay version2/tombstone. Original send retry vẫn current.
5. BE helper C05 gửi parallel edit/delete; deterministic wait-chain/barrier nằm trong integration `MessageMutationTests`, không coi Promise.all riêng là chứng minh đã chờ DB lock.

### MVP-C06 — Worker pause, retry và crash lease

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $rt -Action Pause -Run mvpuser
# Chờ pause-ack nonce khớp, API/PG/Hub vẫn chạy.
# A/S04 đã mở C06 online; A gửi WORKER-STOP-C06.
M Snapshot C06
# Giữ pause ít nhất30s. B chưa actual frame, bấm Làm mới tin nhắn vẫn đọc được1.
powershell -NoProfile -ExecutionPolicy Bypass -File $rt -Action Resume -Run mvpuser
```

A Sent/POST200; B GET200 while pause;1/1/create1 pending1. Resume actual event, hai UI vẫn1 dòng; connection online là transport/subscription, không delivery receipt. BE helper C06 tự pause30s/resume, query counts/frame thật.

Crash/real requeue/rollback variants có recipe và runner trong [P4-T01](DM-P4-T01.md), regression thực hiện lại trên build MVP với automatic reconnect. Dùng run/lane mới; không ghép script P4 với fixture MVP không tương thích. Actor/IDs của mỗi run nằm manifest. Kill sau committed claim, lease15s, restart→sameevent attempts2/published/lease cleared; API/worker chung host nên kill API khác pause dispatcher độc lập. Rollback trước commit không message/operation/outbox/event. Cleanup Resume/ReleaseClaim/FaultOff đúng owned event, giữ volume.

### MVP-C07 — Negative matrix và đổi account

1. C không member C07; dùng token C thật GET/POST/PATCH/DELETE/Subscribe C07:404 `RESOURCE_NOT_FOUND`, không participants/body/email/stack. B member nhưng mutation tin A403 `AUTHOR_REQUIRED`; no-token401; U login403 là ca riêng.
2. Isolated state unverified của **session đã cấp** bị JWT/guard/routing chặn; không mint bypass token. Backend có disabled/stamp/expiry/logout-all/unverified của old connection thật.
3. FE A có draft/history→logout→C login cùng tab; late request/frame A không hiện lại draft/history. Draft chỉ RAM, không body trong browser storage. Component tests actor/selection fence và manual retry immutable; không fallback mock khi lỗi quyền.
4. BE helper C07 chạy negative routes/pending login; DB denied mutation không tăng message/version/outbox. Toàn ma trận cần đọc report/status theo từng đường, không dùng một404 để suy mọi route đã PASS.

### MVP-C08 — Thu hồi đúng A2 và access credential expiry

1. A1/A2 login An hai profile độc lập, cùng mở C08. A1 Cài đặt→Phiên đăng nhập→Thu hồi đúng A2 (data-session-id khớp actual sid từ token giữ RAM); không revoke helper login khác thay A2.
2. A2 giữ browser cũ: UI clear RAM/về login trong≤5s, không login mới để thay proof. Oldtoken HTTP/Hub401; A1 gửi `AFTER-REVOKE`, A2 cũ không có payload. Actor/session IDs/timestamp metadata được lưu, không token.
3. Agent đo request-start→clear=conservative upper bound vì commit nằm giữa request-start và response204; không giả exact PostgreSQL commit timestamp. Backend còn các mode logout-all/stamp/session expiry/disabled/unverified, không chỉ AccessRevoked.
4. JWT access credential hết hạn nhưng refresh session còn hợp lệ: đóng transport cũ rồi product reconnect controller lấy token mới/resubscribe, giữ sid; không giả rằng refresh session đã revoke. Natural-expiry proof host cô lập1min, dispatcher tắt, không writer; host chính vẫn15min.
5. BE helper C08 revoke exact old A2 và assert oldnegotiate401/no payload/cutoff bound. K/authority behavior ở C10/negative guards; full load/SLA benchmark toàn hệ thống P8 không bị suy từ lần đo này.

Lặp natural JWT expiry proof trên **run agent đã có**, không làm writer vào run bàn giao. Cổng15027 cần trống; launcher chỉ tạo/xóa đúng container proof mới, dispatcher tắt. Nhánh chính vẫn dùng JWT15min và giữ API/FE đang chạy:

```powershell
$proofContainer='scdc-dm-expiry-proof-'+[guid]::NewGuid().ToString('N').Substring(0,8)
try {
 docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml run -d --no-deps --name $proofContainer -p 127.0.0.1:15027:8080 -e Modules__Identity__AccessTokenMinutes=1 -e Modules__Messaging__Dispatcher__Enabled=false chat-service
 if($LASTEXITCODE -ne 0){throw 'Proof host start failed'}
 # Chờ health200 ở15027 trước lệnh tiếp; script chạy khoảng1phút.
 (Invoke-WebRequest http://localhost:15027/api/v1/health -UseBasicParsing).StatusCode
 node scripts\dm-acceptance\access-expiry-proof.mjs mvpagentc
 if($LASTEXITCODE -ne 0){throw 'Expiry proof failed'}
} finally {
 if($proofContainer -notmatch '^scdc-dm-expiry-proof-[a-f0-9]{8}$'){throw 'Unexpected cleanup target'}
 docker rm -f $proofContainer
}
```

### MVP-C09 — Composer, IME và RAM drafts

1. C09 draft `Tiếng Việt 👩‍💻`; composition Enter không POST. Desktop Shift+Enter newline, Enter gửi; pointer coarse/mobile Enter newline, nút Gửi gửi. Agent synthetic composition events kiểm tra guard, không claim bàn phím IME vật lý.
2. Chuyển AB↔C09 khi chưa Gửi, **chờ đúng người nhận/đồng bộ xong**: draft riêng từng cặp giữ. Reload/logout/đổi account mất draft; sent còn DB. Failed operation giữ original UUID/body khác draft mới, reconnect không auto POST/PATCH/DELETE.
3. MOBILE lane: actual Edge touch/coarse emulation390×844; Enter không tăng DB, Send tăng1. Bạn cần test thêm bàn phím/thiết bị thật ở lần nghiệm thu cuối; viewport không thay proof vật lý.
4. Snapshot C09/MOBILE số message tăng đúng thao tác Gửi. Lịch sử/tag/errors/edit/delete/Load older dùng được desktop/narrow; không developer fault controls trong product bundle.

### MVP-C10 — Peer K disabled và build/restart chung

```powershell
# A/K mở C10, A gửi K-ORIGINAL trước lock.
powershell -NoProfile -ExecutionPolicy Bypass -File $mvp -Action State -Run mvpuser -Case C10 -Alias K -State Disabled
# A bấm Làm mới hội thoại: compose/send mới bị chặn. K phiên cũ bị thu hồi.
# A vẫn đọc lịch sử, sửa K-CURRENT và xóa chính tin A; original POST replay trả current tombstone.
powershell -NoProfile -ExecutionPolicy Bypass -File $mvp -Action State -Run mvpuser -Case C10 -Alias K -State Unlock
```

Disabled là status3, không phải suspended2/temporary lockout. Helper chỉ DB_test và username/UUID thuộc run; stamp/session revoked cùng transaction, không admin API public. Unlock không phục hồi token cũ; K phải login mới. A read/edit/delete/retry committed được, send mới404, UI dùng public availability không nêu lý do riêng.

Sau toàn bộ test restart stack, đối chiếu từng Snapshot/IDs/version/tombstone/keys/cursor còn hạn. Migration fresh/upgraded/replay và clean checkout build đã được kiểm chứng; không purge history/dedup/marker để “làm sạch” case. Cursor hết hạn rebootstrap authorized snapshot/window; không dùng live sequence làm protected frontier.

## BE copy được và query chỉ đọc

Mỗi helper case dùng run/lane BE trống và tự login/actualHub/API/PG/cleanup/logout, chỉ in metadata:

```powershell
# Chạy từng case, không chạy lại cùng lane sau đã có message:
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C01
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C02
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C03
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C04
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C05
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C06
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C07
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C08
node scripts\dm-acceptance\mvp-backend.mjs mvpbe C10
M Snapshot C03 mvpbe
```

Swagger send/edit/delete là REST thật; Hub invoke dùng helper. Ví dụ replay **FE C04 đã xóa**, không tạo thêm message:

```powershell
Import-Module .\scripts\dm-acceptance\Common.psm1 -Force -DisableNameChecking
$p=@($fe.pairs|Where-Object case -eq C04)[0]
$s=M Snapshot C04|ConvertFrom-Json
$op=$s.rows[0].clientMessageId; $mid=$s.rows[0].id
$a=Connect-DmActor A mvpuser 'MVP-replay'
try{
 $r=Invoke-DmRequest POST "/direct-conversations/$($p.conversationId)/messages" @{clientMessageId=$op;content='DELETE-ORIGINAL'} -AccessToken $a.accessToken
 Assert-DmStatus $r 200 'Original replay'
 [pscustomobject]@{status=$r.Status;id=$r.Body.id;version=$r.Body.version;deleted=($null -ne $r.Body.deletedAt);contentIsNull=($null -eq $r.Body.content)}
}finally{$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}}
$space=([guid]$p.conversationId).ToString()
Invoke-DmReadSql "SELECT id,client_message_id,conversation_sequence::text,version::text,edited_at,deleted_at,content IS NULL AS content_null FROM messaging.messages WHERE space_id='$space' ORDER BY conversation_sequence;"
Invoke-DmReadSql "SELECT id,aggregate_id,aggregate_version,payload,published_at,attempt_count,lease_token,lease_expires_at,last_error FROM integration.outbox_events WHERE space_id='$space' ORDER BY occurred_at,id;"
```

Không echo `$a`/auth response hoặc token/key. Snapshot/helper output chứa counts/status/IDs/version/lease, không full message body. Stored outbox payload chỉ ID/conversation/version; event tải current DTO, eventId không là cursor. `published_at` là dispatcher xử lý, không proof người nhận đọc.

Run mới để lặp, không xóa dữ liệu:

```powershell
M Setup C01 mvprepeat1
M Fixtures C01 mvprepeat1
Push-Location clients\WebClient
$env:DM_MVP_RUN='mvprepeat1'
npx.cmd playwright test --config playwright.dm-mvp.config.js
Pop-Location
# Node/unit/build:
Push-Location clients\WebClient
npm.cmd test
npm.cmd run build
Pop-Location
# Full backend: phải độc lập với E2E/Seed/BETools vì runtime worker pause.
powershell -NoProfile -ExecutionPolicy Bypass -File $rt -Action Pause
$code=0
try { powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dm-acceptance.ps1 -Action BackendTests; $code=$LASTEXITCODE }
finally { powershell -NoProfile -ExecutionPolicy Bypass -File $rt -Action Resume }
if($code -ne 0){throw 'Backend suite failed'}
```

## Agent evidence, lỗi đã sửa và giới hạn

Kết quả cuối được cập nhật cùng remote SHA cuối hồ sơ. Đã có PostgreSQL92/92, Node59/59/build, actual E2E MVP14/14 (`mvpagentc`), actual production PATCH/DELETE old-window recovery, conservative UI revoke cutoff927ms, clean checkout builds. Worker/P4 regression, BE9case, natural expiry và restart smoke phải có evidence chạy thật; không lấy source helper thay PASS. Các lần lỗi đầu được giữ local làm lịch sử, không dùng thay kết quả cuối.

Source/harness sửa trong quá trình kiểm chứng: timestamp mutations được round theo PostgreSQL microseconds để no-op/replay giữ cùng timestamp; JWT validator thêm deleted/primaryverified checks; editor/deleteconfirmation không render cho pending row chưa có ID; delete giữ expectedVersion lúc mở confirmation; draft-switch runner chờ đúng conversation, stale injected frame chọn actual message cũ nhưng qua transport mới. Mutation response/event giữ newest version và không xóa draft mới. Source không tự replay mutations.

Physical mobile/IME, P8 full-load/restore/email/public-release vẫn chưa kiểm chứng trong gói này. Mỗi gate chỉ đóng cho scope/evidence ghi rõ. Observer reconciliation song song có deadline mỗi connection; fresh guard giữ Identity/session/space locks qua dispatch, quyền không lấy từ SignalR group cache. At-least-once có thể lặp frame khi publish-mark lỗi; merge ID/version giữ một dòng. MVP một API host, không benchmark khả năng scale từ test functional.

## Git/dataset cuối và quyền nghiệm thu

Remote SHA/PR, bảng ID thực tế và kết quả final regression được bổ sung tại đây sau push. Một PR base `message`; các commits docs/helper sau code commit không đổi executable source/image nếu ghi rõ. `message` chưa merge gói, quyền merge/push đã có nhưng vẫn chờ người dùng PASS FE/BE bản cuối.

Bạn chỉ cần phản hồi một lần: **“Chat riêng MVP PASS FE và BE trên build <SHA bàn giao>; nghiệm thu.”** Nếu FAIL, ghi case/lane/account/bước/expected/actual; sửa cùng branch và giao build mới. Không coi im lặng hoặc “đang test” là PASS; không publish/main từ nghiệm thu này.


### ID thực tế của bộ bàn giao

Mỗi lane FE/BE ở bảng này được query xác minh0message/operation/outbox, counter0; browser smoke A/B hai run có composer và online, không writer. Manifest và baseline JSON ngoàiGit giữ IDs. X/Y dùng khi đã arm hoặc helper dùng đúng UUID; UI tự tạo UUID thì replay lấy từ response/Snapshot.

| Lane | conversationId FE mvpuser | conversationId BE mvpbe | X BE (UUIDv4) |
|---|---|---|---|
| C01 | `01a12ffa-e134-7608-85da-1a1069bcb51e` | `01a12ffb-3700-72d4-993f-4cce9681c4c9` | `658d76d3-8805-4fff-b6bb-d0294c1c0792` |
| C02 | `01a12ffa-e148-78a2-a794-cd203c7db0db` | `01a12ffb-3713-715c-9a68-f60e4f6997a9` | `ad76dbd0-fccf-4291-995c-93ce235ba20a` |
| C03 | `01a12ffa-e169-7553-962e-106bbc3c2274` | `01a12ffb-3735-7b84-9bc6-a10e061ffe06` | `16d8d7c2-135c-4f9d-9103-44f1791ecff3` |
| C04 | `01a12ffa-e179-7c16-b232-48065805dcdf` | `01a12ffb-3745-753c-bc0e-ffe684155f47` | `e4fbc5e4-4dbe-4b1f-83fb-7eaaf86f511e` |
| C05 | `01a12ffa-e189-7c48-a4a5-2bb8393d9b20` | `01a12ffb-3754-717b-9690-7ac75efb4f69` | `7287bdd1-7546-4249-aa1f-2b073e842bbe` |
| C06 | `01a12ffa-e198-797b-985f-16f80312a91f` | `01a12ffb-3762-7c73-8587-e3da982a8fcf` | `2315082d-8bb8-4497-aea5-29594a4c7f39` |
| C07 | `01a12ffa-e1a7-7ca8-a370-d1615da906dd` | `01a12ffb-3771-7cef-9f2c-bc22cf2c8a62` | `d8dfaf37-ec5b-4938-b85c-b5813647cc8e` |
| C08 | `01a12ffa-e1b9-724e-8d9f-388eb3700042` | `01a12ffb-377f-7da3-b4e9-14ed29f61433` | `0f7def24-edf0-409e-9eab-699fcf3bfee5` |
| C09 | `01a12ffa-e1c8-7a44-9fe5-79eaefa9d8e3` | `01a12ffb-378d-79c4-b20f-45c955eef808` | `122dd073-c872-478b-b4d5-938d90d87006` |
| C10 | `01a12ffa-e1d7-7ba2-86e2-2d8e719e917c` | `01a12ffb-379d-7f67-aa7b-51a29a79bb6c` | `8b27f501-9d04-4b64-9126-0e270b0ed1ff` |
| C02P1 | `01a12ffa-e1e7-7f28-82d3-208ac73a66e0` | `01a12ffb-37af-784f-acaf-aef3e21fa8fe` | `e6590b28-51e8-47d6-97c3-372d7af9eade` |
| C02P2 | `01a12ffa-e1f5-71a3-b0ee-bec3b0104408` | `01a12ffb-37bd-709e-ba9b-1e1cebae21c4` | `776e8159-4549-425e-9ebc-79cbce87900a` |
| C02LIVE | `01a12ffa-e203-7e34-b599-3b9155e2e54c` | `01a12ffb-37cb-7e43-8ecb-a7c2ae1d393d` | `66240862-4243-4409-8537-46d7c4052b8f` |
| MOBILE | `01a12ffa-e211-70e2-a9ad-d64ca771b150` | `01a12ffb-37d9-7179-8f2a-7f1076a41ba7` | `14d02122-74f6-4452-91a3-419e2af192ba` |
| EDITOLD | `01a12ffa-e220-7d0d-8b05-32d4dcfc9486` | `01a12ffb-37e8-74a7-af96-58cb86cf7797` | `d88992be-bdc2-4f14-a077-1347fba6962f` |
| DELETEOLD | `01a12ffa-e22e-7d38-9e44-743f6d002b06` | `01a12ffb-37f7-7e6d-8fdd-87a229c5d806` | `badad925-3964-4376-910c-45be90f37b86` |

### Kết quả final agent

| Kiểm tra | Bằng chứng local | Kết quả | Người dùng |
|---|---|---|---|
| Backend PostgreSQL18 integration | `.dm-acceptance/backend-artifacts/test-results/dm-acceptance.trx` | 92/92 PASS | Chưa xác nhận |
| Node / Vite build | `.dm-acceptance/59 tests; asset index-C2CfOtsO.js` | 59/59 PASS + build | Chưa xác nhận |
| MVP actual browsers | `.dm-acceptance/runs/mvpagentc/e2e-results.json + mvp-browser-*.json` | 14/14 PASS | Chưa xác nhận |
| Worker/routing/crash actual browsers | `.dm-acceptance/runs/mvpp4final/e2e-results.json + browser-*.json` | 8/8 PASS | Chưa xác nhận |
| BE helpers actual API/WS/PG | `.dm-acceptance/runs/mvpagentbe/mvp-backend-C01..C08,C10.json` | 9/9 PASS | Chưa xác nhận |
| JWT expiry1min actual refresh/resubscribe | `.dm-acceptance/runs/mvpagentc/natural-access-expiry.json` | PASS: same sid,1 rotation,2 online,0 writer | Chưa xác nhận |
| Restart current DTO/keyring/cursor | `.dm-acceptance/runs/mvpagentbe/restart-smoke.json` | PASS:50latest/H101/cursor valid/keys same | Chưa xác nhận |
| Fresh/upgrade/replay migration | `.dm-acceptance/runs/p4proof/realtime-migration.json` | PASS:2 replays/DB,pending metadata retained | Chưa xác nhận |
| Log redaction / cleanup | `.dm-acceptance/mvp-log-scan.json; no pause/hold/fault trigger` | PASS sample querytoken/JWT/body counts0 | Chưa xác nhận |
| Handover actual FE / baseline32lanes | `.dm-acceptance/runs/mvpuser,mvpbe/handover-browser-smoke.json + baseline-mvp.json` | PASS:32 empty lanes, composer/online | Chưa xác nhận |

Source thực thi cuối `f0e8dd1b3051b0d6b49d04e748028a08a522d144`. Các helper/docs commit sau đó không đổi source API/FE/schema đã clean-build và test. Remote SHA/PR được ghi sau push; chưa merge message và không User PASS.

# DM-P4-T01 — Hub, outbox và hợp nhất sự kiện

**Mốc bàn giao P4-T01: agent đã kiểm chứng, chưa có user PASS riêng.** Ngày12/10/2026 người dùng yêu cầu “Làm liên tục toàn bộ phần MVP còn lại; tôi test FE/BE một lần cuối.” Source P4 được kế thừa vào `feat/dm-mvp-completion`; thực hiện P4-T02/P5/P6/P7 liên tục, không dừng ở gate từng task của hồ sơ này. Hồ sơ/asset/test dưới đây ghi build P4 trước mở rộng; bản bàn giao cuối và gate người dùng nằm ở `DM-MVP.md`. `message` chưa merge gói MVP trước PASS cuối.

Người dùng đã xác nhận: “P3-T02 đã PASS FE/BE; nghiệm thu và làm P4-T01.” Build được nghiệm thu `d4f1669`, code `4102955`. Commit ghi nhận `9504a9f` đã push; merge `--no-ff` và push `message` tại `9de920abda42fa26b15303a8705e61f612c4ff52`. Smoke sau merge Node53/53, history H121 đủ121 ID và API/proxy200. P4 bắt đầu từ đúng baseline này, không reuse/merge nhánh Messaging cũ.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / phase | DM-P4-T01 / P4; năm subtask theo kế hoạch |
| Branch / base / PR target | `feat/dm-p4-t01-realtime-outbox` / `9de920a` / `message` |
| Code / PR / remote SHA | Xem mục Git bàn giao cuối hồ sơ |
| Checkout thực thi | `E:\Project\SCDC\dm-message-integration`, worktree của repo `scdc`; checkout gốc và thay đổi người dùng được giữ |
| FE / API / Swagger | <http://localhost:15300> / <http://localhost:15026/api/v1> / <http://localhost:15026/swagger> |
| Hub | `/hubs/chat`, qua FE15300 hoặc API15026; mutation chỉ REST |
| Compose / DB | `scdc-dm-acceptance` / `scdc_dm_acceptance_test`, PostgreSQL18 cổng15432, user `scdc_dm_test` |
| Migration | `20261010_dm_p4_outbox_lease.sql`; additive, hai nullable lease fields, constraint và partial index; replay hai lần |
| Config | MVP một API host. Lease15s; backoff1/2/4/8/16/32s; guard timeout2s khi delivery, reconcile định kỳ1s |
| Keys | Giữ cursor/HMAC ring local, active HMAC `dm-local-v1`; không commit key bytes, config hay token |
| API image đang chạy | `sha256:56a6d0c16951ccf17db0a8ddd7ec14c0cc754b88cf1d863220465bfbf54b8274` từ `docker inspect .Image` |
| FE image / asset | `sha256:f66b18c6a79f8a78cf04416e7503e3d9a559653f151afaceff623a31087d592b` / `index-BDCh9-Y8.js` |
| Browser | Edge Chromium headless Windows,1440×1000, các context/login độc lập,1worker; không trace/video/HAR |
| Run agent | `p4verified` đầy đủ8 E2E; `p4route` bổ sung C04 browser C; `p4befinal` bốn Hub proofs; `p4proof` migration |
| Run bạn test | **`p4user` dành FE**, **`p4be` dành BE**; mỗi run28 accounts và8 cặp DM trống |
| Xác nhận của bạn | C01–C04 và ca bổ sung: **Chưa xác nhận** |

## Những gì đã triển khai

Hub đọc actor/session/stamp/expiry từ JWT, kiểm tra Identity qua contract công khai và registry theo connection/session/conversation. Subscribe lặp an toàn; unsubscribe ACK bảo đảm delivery đã được tuần tự hóa trước bỏ subscription. Token query chỉ được nhận ở GET đúng `/hubs/chat`, không được dùng để xác thực REST. Outsider nhận HubException với JSON ProblemDetails404 `RESOURCE_NOT_FOUND`; SignalR có thể thêm prefix phương thức và `HubException:` trước JSON. Không bật detailed errors.

Dispatcher claim một event bằng transaction và `FOR UPDATE SKIP LOCKED`; claim có lease token và deadline DB. Event chỉ đọc được sau commit của writer. Khi phát, từng recipient được kiểm tra session/member bằng fresh guard và đọc DTO message hiện hành; không phát body từ outbox. Mark published đúng lease token; failure dùng fixed error code và backoff; crash để lease hết hạn. `published_at` nghĩa là dispatcher xử lý event, không là bằng chứng người dùng nhận/đọc. Không có recipient đang subscribe vẫn xử lý event; người mở sau đọc REST history.

FE có handler/buffer trước start/subscribe, merge theo message ID và author+clientMessageId, version/sequence là decimal string được so sánh bằng BigInt. Event xác nhận persistence có thể đến trước HTTP response và hợp nhất tin tạm thành Sent. Trạng thái “Đã kết nối cập nhật” phản ánh transport/subscription; không là delivery/read receipt. AccessRevoked session/user xóa RAM và phiên local. Đổi DM hoặc chuyển sang Community bỏ subscription DM cũ.

Hiện mất kết nối cần bấm **Kết nối lại**, rồi GET latest. Reconnect tự động, catchup nhiều trang, resume frontier và tải lại cửa sổ cache thuộc **P4-T02**. Production edit/delete API thuộc **P5**; SQL current/tombstone trong hồ sơ này là technical fixture giới hạn run. Thu hồi tất cả đường account/member và SLA dưới áp lực thuộc **P6**. Không đánh dấu toàn AC-DM-12/21, TC-DM-12/23/25, UC hoặc phase hoàn tất.

## Chuẩn bị và lệnh chạy đã kiểm chứng

Mở Docker Desktop/Linux engine. Chạy PowerShell ở đúng worktree dưới đây. Chạy file bằng Code Runner không có `-Action` sẽ hỏi tham số; script ở checkout gốc vẫn có thể dựng source cũ lên cùng cổng.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
$runner = Join-Path (Get-Location) 'scripts\dm-acceptance\realtime.ps1'
$fe = Get-Content .dm-acceptance\runs\p4user\realtime-fixtures.json -Raw -Encoding UTF8 | ConvertFrom-Json
$be = Get-Content .dm-acceptance\runs\p4be\realtime-fixtures.json -Raw -Encoding UTF8 | ConvertFrom-Json
function DM([string]$Action,[string]$Case='C01',[string]$Run='p4user') {
  & powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action $Action -Run $Run -Case $Case
  if($LASTEXITCODE -ne 0){throw "DM $Action failed"}
}
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Status
```

Sau thay đổi source hoặc khi dựng lại máy khác, dùng `realtime.ps1 -Action Upgrade`: apply lease migration hai lần, build API/FE rồi Start. Lệnh này **không reset data**. Stop toàn stack bằng `dm-acceptance.ps1 -Action Stop`; Restart bằng `-Action Restart -NoBuild`. Pause/Resume bên dưới chỉ điều khiển dispatcher, giữ API/DB/Hub chạy. Nếu process bị kill, start lại service theo recipe CRASH.

```powershell
# Chỉ dùng khi cần rebuild bản này, không cần chạy lại mỗi ca:
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Upgrade

# Lặp test độc lập: đặt tên run mới, không dùng lại run đã có message:
$newRun='p4repeat1'
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Setup -Run $newRun
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Fixtures -Run $newRun
```

`Setup`/`Fixtures` từ chối manifest/fixture đã tồn tại; không xóa hoặc reset lane. DB chính/volume không bị drop. Migration proof chỉ tạo và xóa đúng hai DB mới có tên random thuộc helper; không chạy full schema vào DB nghiệm thu đang chứa dữ liệu.

## Tài khoản, lane và baseline

Mật khẩu local cho tài khoản verified: **`DmDemo2026!Local`**. Email `example.test` không là hộp thư thật. Tất cả ca dưới đây đăng nhập **An: `dm_demo_an_p4user`**; bên nhận dùng đúng username ở bảng. Mở hai browser profile hoặc Chrome thường + Edge/InPrivate; hai tab cùng profile dùng chung login nên không thay được hai actor độc lập.

| Case | Bên nhận FE `p4user` | Alias | Body An gửi |
|---|---|---|---|
| C01 | `dm_demo_bao_p4user` | B | `REALTIME-C01` |
| C02 | `dm_demo_chi_p4user` | C | `WORKER-STOP-C02` |
| C03 | `dm_demo_search01_p4user` | S01 | `EVENT-FIRST-C03` |
| C04 | `dm_demo_search02_p4user` | S02 | `ROUTE-DENY-C04` |
| CRASH | `dm_demo_search03_p4user` | S03 | `LEASE-CRASH-P4` |
| ROLLBACK | `dm_demo_search04_p4user` | S04 | `ROLLBACK-P4` |
| SESSION | `dm_demo_search05_p4user` | S05 | `AFTER-SESSION-REVOKE-P4` |
| CURRENT | `dm_demo_search06_p4user` | S06 | `BODY-BEFORE-TOMBSTONE-P4` |

BE dùng cùng aliases nhưng suffix **`p4be`**, ví dụ `dm_demo_an_p4be`, `dm_demo_bao_p4be`. Outsider C là `dm_demo_chi_<run>` cho C01/C03/C04; **C02 dùng outsider S23** vì Chi là member của cặp C02. Người nhận của C03/C04 là S01/S02, không phải Bảo; đừng tìm “Bảo” cho tất cả ca.

ID/UUID thực tế được liệt kê trong mục Dữ liệu bàn giao cuối hồ sơ và `.dm-acceptance/runs/<run>/realtime-fixtures.json`. `X`/`Y` là UUIDv4 đã tạo, `conversationId` do API trả; message/event ID chỉ tồn tại sau send. Baseline **từng cặp** ở cả FE/BE: messages0, operations0, outbox0, pending0, counter`"0"`, bodyCopies0. Sau một send hợp lệ:1/1/1/counter`"1"`. Không dùng global counts của DB chứa các run trước.

Lấy lane và snapshot đúng scope:

```powershell
$p=@($fe.pairs | Where-Object case -eq C01)[0]
$p | Format-List case,peerUsername,conversationId,X,Y
DM Snapshot C01
# Query này là read-only, không in body/token/key.
```

Ở FE: bấm dấu **+**, nhập đúng username bên nhận, chọn dòng có **username chính xác** rồi Mở hội thoại. Bên nhận tìm `dm_demo_an_p4user`. Chờ hết tải lịch sử và dòng **Đã kết nối cập nhật**. Ban đầu cặp này chưa có tin. Dataset khác có thể cùng tên hiển thị; luôn đối chiếu username.

## DM-P4-T01-C01 — Hai browser nhận tin thật không reload

AC-DM-02 và lát cắt TC-DM-03. Run FE `p4user/C01`, BE `p4be/C01`. Không fault. Agent dùng `p4verified/C01` và `p4befinal/C01`.

| Bước | Thao tác cụ thể | Kỳ vọng / thực tế agent | Agent | Bạn |
|---|---|---|---|---|
| FE1 | Login Bảo ở browser B; mở An. Login An ở browser A; mở Bảo. Chờ hai dòng kết nối online | Hub start + subscribe ACK thật; hai timeline0 | PASS | Chưa xác nhận |
| FE2 | A nhập đúng `REALTIME-C01`, bấm Gửi một lần; B giữ nguyên trang, không bấm làm mới | A một dòng Đã gửi; B một dòng mới cùng body/ID | PASS | Chưa xác nhận |
| FE3 | DevTools Elements: dòng `.dm-text-message` có data-message-id/sequence/version. Network lọc messages; ở B không có GET history thêm khi nhận tin | A/B cùng ID, sequence`"1"`, version`"1"`; actual WS frame, không polling | PASS | Chưa xác nhận |
| FE4 | Reload B rồi A, mở lại cặp nếu cần | Mỗi bên một dòng ID cũ lấy từ server | PASS | Chưa xác nhận |
| BE1 | `DM HubProof C01 p4be` | POST200; A/B subscribe ACK; current MessageChanged khớp REST ID/clientUUID/sequence/version; outsider404/no frame | PASS | Chưa xác nhận |
| DB | `DM Snapshot C01` cho FE; `DM Snapshot C01 p4be` cho BE | Delta1/1/1, bodyCopies0, pending0, publishedtrue, leaseToken/expiry null | PASS | Chưa xác nhận |

Timing proof ghi send-start, actual POST200, browser receipt và render timestamps; POST200 xác nhận commit. Không instrument exact DB commit instant nên **không công bố số đo commit→render chính xác hoặc SLA tải**. Agent B history reads trước reload=0. Bằng chứng: `p4verified/browser-C01.json`, `p4befinal/hub-proof-C01.json`.

## DM-P4-T01-C02 — Pause worker30s, đọc DB và phát lại cùng event

AC-DM-02, TC-DM-16 phần dispatcher/retry. Dùng cặp **An/Chi** của C02, không dùng cặp C01 đã có tin.

```powershell
DM Pause C02
# Chờ lệnh thành công: pause-ack nonce khớp; worker đã kết thúc lượt dispatch trước.
# Thực hiện send và đọc lịch sử bên dưới, giữ pause ít nhất30s.
DM Snapshot C02
```

| Bước | Thao tác cụ thể | Kỳ vọng / thực tế agent | Agent | Bạn |
|---|---|---|---|---|
| FE1 | Hai browser mở C02 online; chạy Pause; A gửi `WORKER-STOP-C02` | A Sent/POST200 dù worker pause; B0 dòng, chưa có actual frame | PASS | Chưa xác nhận |
| FE2 | B bấm **Làm mới tin nhắn**; đợi30s trong khi worker vẫn pause | B đọc được đúng1 dòng từ DB. Subscription vẫn online; không tự bịa delivery/read receipt | PASS | Chưa xác nhận |
| DB1 | Snapshot trước Resume | messages/operations/outbox1, counter`"1"`, pending1, publishedfalse, lease null | PASS | Chưa xác nhận |
| FE3 | `DM Resume C02`; giữ hai browser mở | Nhận actual event; A/B vẫn1 dòng cùng ID | PASS | Chưa xác nhận |
| BE | `DM HubProof C02 p4be` trên lane BE còn trống | Tự pause có ACK, send200, GET Chi200, chờ30s, resume; real republish cùng eventID; records vẫn1 | PASS | Chưa xác nhận |
| DB2 | Snapshot sau phát lại | pending0/publishedtrue; attempt tăng, lease null; không thêm writer/operation/event | PASS | Chưa xác nhận |

Để bạn phát lại **đúng event FE** sau Resume, không bấm Gửi lần nữa:

```powershell
$s=(DM Snapshot C02 | ConvertFrom-Json)
$eid=$s.events[0].id
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Requeue -Run p4user -Case C02 -EventId $eid
DM Snapshot C02
```

Requeue là fixture local, giữ event/message ID cũ, chỉ đưa metadata event về pending. B vẫn một dòng sau nhận event lần hai. Cleanup bắt buộc `DM Resume C02`, không tắt DB/API. Bằng chứng `p4verified/browser-C02.json`, `p4befinal/hub-proof-C02.json`; 30s là fault duration, không là SLA.

## DM-P4-T01-C03 — Event đến trước response, duplicate/cũ và version mới

AC-DM-21/TC-DM-23 lát cắt ordering và TC-DM-16. Không claim reconnect nhiều trang. SQL current là fixture kỹ thuật, không là edit API P5.

Trước login ở cả A/B: mở DevTools Console và dán toàn bộ [realtime-faults.js](../../../scripts/dm-acceptance/realtime-faults.js). Script phải cài **trước** login/open tạo WebSocket. Nếu đang login, logout qua UI trước, dán script rồi login; không reload sau khi cài vì script chỉ ở RAM. Dùng PowerShell để copy file, rồi Ctrl+V vào Console:

```powershell
Get-Content .\scripts\dm-acceptance\realtime-faults.js -Raw -Encoding UTF8 | Set-Clipboard
$p=@($fe.pairs | Where-Object case -eq C03)[0]
$p | Format-List conversationId,X,peerUsername
```

Ở cả hai Console sau login, thay UUID từ `$p.conversationId`:

```javascript
window.dmHubFixture.on({conversationId:'<conversationId-C03-FE>'});
```

Mở cặp An/S01 và chờ online. Trên A dán toàn bộ [sequence-fixture.js](../../../scripts/dm-acceptance/sequence-fixture.js), rồi toàn bộ [retry-faults.js](../../../scripts/dm-acceptance/retry-faults.js). Hai file cũng có thể copy bằng `Get-Content ... -Raw -Encoding UTF8 | Set-Clipboard` lần lượt. Sau đó:

```javascript
window.dmSequenceFixture.arm('<X-C03-FE>');
window.dmRetryFixture.on({conversationId:'<conversationId-C03-FE>',mode:'delay',delayMs:60000});
```

| Bước | Thao tác cụ thể | Kỳ vọng / thực tế agent | Agent | Bạn |
|---|---|---|---|---|
| FE1 | A gửi `EVENT-FIRST-C03`; giữ HTTP success response trong fixture | API đã200/commit; actual Hub event đến A/B trước khi release response cho UI; A/B một dòng Sent | PASS | Chưa xác nhận |
| FE2 | A nhập draft mới `DRAFT-AFTER-EVENT-C03`; chưa gửi | Draft mới giữ nguyên khi reply cũ tới | PASS | Chưa xác nhận |
| FE3 | Cả A/B chạy `window.dmHubFixture.duplicate();` hai lần | Hai **local application frame injections**, cùng ID/version; vẫn1 dòng, không là proof dispatcher mới | PASS | Chưa xác nhận |
| BE1 | Snapshot lấy mid/eid; technical Current Edited và Requeue thật như dưới | Worker phát current DTO version`"2"`, body `CURRENT-P4-C03`; vẫn1message/operation/outbox | PASS | Chưa xác nhận |
| FE4 | Cả A/B inject stale version1 như dưới; rồi release HTTP version1 trên A | Giữ version2/body current, một dòng; draft mới còn nguyên | PASS | Chưa xác nhận |
| BE2 | `DM HubProof C03 p4be` | Real POST/event + current version2 republish; riêng HTTP-before-release được chứng minh bằng browser FE, helper này không delay HTTP | PASS | Chưa xác nhận |
| DB | Snapshot, GET history và data-message-id/version của A/B | ID duy nhất giống DB/current GET;1/1/1; version2; bodyCopies0 | PASS | Chưa xác nhận |

Trong PowerShell khi message đã có:

```powershell
$s=(DM Snapshot C03 | ConvertFrom-Json)
$mid=$s.rows[0].id; $eid=$s.events[0].id
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Current -Run p4user -Case C03 -MessageId $mid -State Edited
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Requeue -Run p4user -Case C03 -EventId $eid
```

Ở A/B Console sau actual version2 tới:

```javascript
window.dmHubFixture.duplicate({version:'1',content:'STALE-LOCAL-INJECTION'});
window.dmHubFixture.stats(); // Chỉ metadata ID/version/timestamp, không token/body.
// Chỉ trên A:
window.dmRetryFixture.release();
```

Để kiểm tra reply tới trước deadline15s, release trước15s. Nếu bạn thao tác lâu hơn, deadline có thể hết nhưng actual event đã xác nhận persistence nên row vẫn Sent; không kết luận đã quan sát reply muộn trước timeout. Ca này có thể test riêng theo hai biến thể/run mới. Agent đã quan sát ordering và version2 thắng response1 trên run tự động.

Cleanup: A `window.dmRetryFixture.off(); window.dmSequenceFixture.off();`; cả A/B `window.dmHubFixture.off();`. Bằng chứng `p4verified/browser-C03.json`, `p4befinal/hub-proof-C03.json`; injected frames được gắn nhãn riêng với actual receipts.

## DM-P4-T01-C04 — Outsider404 và bỏ subscription cũ

AC-DM-12/TC-DM-12 phần read/send/subscribe; edit/delete guard còn chờ P5. Cặp **An/S02**. Outsider **Chi** có DM khác với An, không là member cặp này.

| Bước | Thao tác cụ thể | Kỳ vọng / thực tế agent | Agent | Bạn |
|---|---|---|---|---|
| FE1 | A và S02 mở C04 online. Browser C login `dm_demo_chi_p4user`, xem sidebar inbox | C không có cặp An/S02; DM An/Chi riêng là hợp lệ, không nhầm hai conversation ID | PASS | Chưa xác nhận |
| FE2 | S02 bấm + tìm `dm_demo_search23_p4user`, chọn đúng dòng, Mở hội thoại; giữ session/browser này | Cùng transport; Unsubscribe C04 ACK trước Subscribe cặp mới; không tạo WebSocket mới | PASS | Chưa xác nhận |
| FE3 | A gửi `ROUTE-DENY-C04`; S02 vẫn xem cặp S02/S23 | A Sent; subscription C04 cũ của S02 không có payload; C không nhận payload | PASS | Chưa xác nhận |
| FE4 | S02 bấm + tìm An, mở lại C04 | GET history200, đúng một dòng của A | PASS | Chưa xác nhận |
| BE | `DM HubProof C04 p4be` | Actual C invoke404 `RESOURCE_NOT_FOUND`, JSON không private participants/stack; B unsubscribe ACK + subscribe DM khác cùng socket; A200; C/B-old no frame; B resubscribe/history200 | PASS | Chưa xác nhận |
| DB | Snapshot C04 |1/1/1, pending0, bodyCopies0; denied subscribe không thêm membership/message | PASS | Chưa xác nhận |

Để bạn quan sát frames của subscription cũ trong FE, cài `realtime-faults.js` trước login S02, `.on({conversationId:'<C04-FE.id>'})` rồi giữ config ID cũ khi chuyển sang S23. Sau khi A gửi và Snapshot published, `.stats().actual` phải rỗng. Agent kiểm tra thêm browser C actual negotiate/invoke và inbox/API bằng `p4route/browser-C04.json`; helper BE dùng account riêng `p4be`, không đổi login C của FE.

## Ca bổ sung bắt buộc

### CRASH — Kill worker sau committed claim

Dùng cặp An/S03 mới, body `LEASE-CRASH-P4`. API host và worker chung process ở MVP; SIGKILL service làm Hub ngắt nhưng PG vẫn chạy. Pause độc lập API ở C02 là ca khác.

```powershell
DM Pause CRASH
# Hai browser mở CRASH online, An gửi LEASE-CRASH-P4; B chưa nhận event.
$s=(DM Snapshot CRASH | ConvertFrom-Json); $eid=$s.events[0].id
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action HoldClaim -Run p4user -Case CRASH -EventId $eid
DM Resume CRASH
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action ClaimStatus -Run p4user -Case CRASH -EventId $eid
# Chỉ kill sau ClaimStatus trả JSON eventId/LeaseToken/LeaseExpiresAt thực tế.
docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml kill -s SIGKILL chat-service
DM Snapshot CRASH
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action ReleaseClaim -Run p4user -Case CRASH -EventId $eid
docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml start chat-service
# Chờ API200, không đọc config/log token:
(Invoke-WebRequest http://localhost:15026/api/v1/health -UseBasicParsing).StatusCode
```

Kỳ vọng: sau kill pending1, lease token đúng claim, B offline/no actual frame. Restart trước deadline15s và bấm **Kết nối lại** trên B, chờ online; latest history đọc được tin. Khi lease hết hạn, actual worker phát cùng event ID; attempts2, publishedtrue, lease null,1/1/1/counter1. Nếu bạn thao tác quá15s rồi mới kết nối, event có thể đã published khi không có subscriber; history vẫn đúng nhưng **không chứng minh được live recovery frame**, dùng run mới để lặp nhanh hoặc chạy E2E tự động. Agent đã chứng minh actual frame trên run `p4verified/CRASH`, không dùng synthetic crash.

Cleanup luôn ReleaseClaim đúng eid, start API, Resume; không xóa volume. Kết quả agent **PASS**; bạn **Chưa xác nhận**. Bằng chứng `p4verified/browser-CRASH.json` có committed lease, pending khi killed, actual event ID, attempts2.

### ROLLBACK — Writer lỗi trước commit, không có event

Mở An/S04 online, baseline0. `DM FaultOn ROLLBACK`; A gửi `ROLLBACK-P4` một lần. POST503 `AUTHORITY_UNAVAILABLE`; A có Thử gửi lại, B0 dòng/no frame. `DM Snapshot ROLLBACK` vẫn0/0/0/counter0; mọi insert/counter cùng transaction đã rollback. `DM FaultOff ROLLBACK` rồi chờ: không tự gửi lại; chỉ thử gửi lại khi bạn chủ động bấm. Agent chạy cleanup và giữ lane0; **PASS**, bạn **Chưa xác nhận**. Proof `p4verified/browser-ROLLBACK.json`; PostgreSQL integration còn kiểm tra rollback no publish và lỗi mark-published/backoff.

### SESSION — Thu hồi đúng phiên cũ, không login mới để chứng minh

Mở An/S05 online; có thể gõ draft chưa gửi ở S05. Trong DevTools Network của S05 giữ access/refresh token **chỉ trong RAM**, không lưu ra hồ sơ. Swagger/REST `POST /api/v1/auth/logout` với `{"refreshToken":"<refreshToken-phiên-S05-đang-mở>"}` trả204. Cần token của **đúng session đang subscribe**, token của login helper mới không thu hồi session FE cũ.

Giữ nguyên browser S05, không login lại. Reconciler gửi AccessRevoked/đóng connection; UI xóa RAM và về màn hình đăng nhập. An gửi `AFTER-SESSION-REVOKE-P4`; phiên S05 cũ không nhận payload. POST `/hubs/chat/negotiate?negotiateVersion=1` với Bearer **access token cũ** trả401. DB message vẫn1 cho history hợp lệ khi login phiên mới sau này. Agent **PASS** trên phiên cũ thật; bạn **Chưa xác nhận**. Proof `p4verified/browser-SESSION.json`, integration revoked session. Timestamp logout204 là commit-confirmed bound, không exact DB commit t0; chưa claim toàn bộ đường revoke/SLA≤5s dưới tải P6.

### CURRENT — Pending event chỉ phát tombstone hiện hành

Mở An/S06 online, Pause CURRENT, An gửi `BODY-BEFORE-TOMBSTONE-P4` và thấy Sent. Lấy actual mid bằng Snapshot CURRENT; chạy Current Deleted đúng run/case/mid; Resume. B nhận version`"2"`, contentnull, dòng “Tin nhắn đã bị xóa”; không nhận body gốc. A cũng merge thành tombstone. Replay REST **cùng actor, X và body gốc** trả200 cùng ID/current version2/contentnull, không phục hồi body, không thêm message/operation/outbox. Local stale version1 injection không thay tombstone. Agent **PASS**, bạn **Chưa xác nhận**. Proof `p4verified/browser-CURRENT.json`; fixture SQL này không chứng minh API edit/delete P5 đã có.

```powershell
DM Pause CURRENT
# An send đúng body qua FE, sau đó:
$s=(DM Snapshot CURRENT | ConvertFrom-Json); $mid=$s.rows[0].id
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Current -Run p4user -Case CURRENT -MessageId $mid -State Deleted
DM Resume CURRENT
DM Snapshot CURRENT
```

## REST/Swagger và SQL chỉ đọc

Hub không xuất hiện như REST operation trong Swagger. Dùng `HubProof` để invoke/capture ACK/error/frame thật. Request REST send qua Swagger: Bearer An đúng run, POST `/api/v1/direct-conversations/{conversationId}/messages`, body `{"clientMessageId":"<X-thực-tế>","content":"<body-case>"}`. GET cùng path bằng bên nhận trả200 current DTO; bằng outsider trả404 `RESOURCE_NOT_FOUND`; anonymous401. Không dán token vào URL REST.

Bạn có thể replay **thao tác FE đã gửi** và đọc history bằng PowerShell, không tạo message mới. Ví dụ C01:

```powershell
Import-Module .\scripts\dm-acceptance\Common.psm1 -Force -DisableNameChecking
$p=@($fe.pairs | Where-Object case -eq C01)[0]
# FE không arm X ở C01: lấy clientMessageId thật từ snapshot, không tự dùng X để replay.
$s=(DM Snapshot C01 | ConvertFrom-Json); $op=$s.rows[0].clientMessageId
$a=Connect-DmActor A p4user 'P4-replay'; $b=Connect-DmActor B p4user 'P4-history'; $c=Connect-DmActor C p4user 'P4-outsider'
try {
  $replay=Invoke-DmRequest POST "/direct-conversations/$($p.conversationId)/messages" @{clientMessageId=$op;content='REALTIME-C01'} -AccessToken $a.accessToken
  $history=Invoke-DmRequest GET "/direct-conversations/$($p.conversationId)/messages" -AccessToken $b.accessToken
  $deny=Invoke-DmRequest GET "/direct-conversations/$($p.conversationId)/messages" -AccessToken $c.accessToken
  Assert-DmStatus $replay 200 'Replay'; Assert-DmStatus $history 200 'B history'; Assert-DmStatus $deny 404 'Outsider'
  [pscustomobject]@{replayStatus=$replay.Status;id=$replay.Body.id;version=$replay.Body.version;historyCount=$history.Body.items.Count;outsiderCode=$deny.Body.errorCode}
} finally {
  foreach($auth in @($a,$b,$c)){$null=Invoke-DmRequest POST /auth/logout @{refreshToken=$auth.refreshToken}}
}
DM Snapshot C01
```

Không echo `$a/$b/$c` hoặc auth response. Replay phải giữ đúng original UUID/content/actor; **X manifest chỉ được dùng khi đã arm cho send đó**. BE HubProof dùng X và yêu cầu lane message0; không chạy HubProof hai lần trên cùng case rồi kỳ vọng baseline0. Lặp dùng run mới.

Query read-only đầy đủ Snapshot đã triển khai tại [realtime.ps1](../../../scripts/dm-acceptance/realtime.ps1). Có thể dùng Common module, thay `$p` đúng lane:

```powershell
$space=([guid]$p.conversationId).ToString()
Invoke-DmReadSql "SELECT id,author_user_id,client_message_id,conversation_sequence::text,version::text,edited_at,deleted_at FROM messaging.messages WHERE space_id='$space' ORDER BY conversation_sequence;"
Invoke-DmReadSql "SELECT id,aggregate_id,event_type,payload,published_at,attempt_count,lease_token,lease_expires_at,last_error FROM integration.outbox_events WHERE space_id='$space' ORDER BY occurred_at,id;"
Invoke-DmReadSql "SELECT count(*) AS operations FROM messaging.send_operations WHERE space_id='$space';"
```

Expected stored payload chỉ conversation/message/version tham chiếu, không content/body. Hub current DTO có body hiện hành trong transport; không lưu DTO đó vào log/hồ sơ. Không dùng eventId hoặc highest live sequence làm resume cursor.

## Kết quả agent và phạm vi còn mở

| Ca / AC / TC | Thực tế / bằng chứng | Kết quả agent | Bạn |
|---|---|---|---|
| C01 / AC02 / TC03 | Hai actual browsers cùng ID, B không reload/GET polling; reload history giữ ID | PASS | Chưa xác nhận |
| C02 / TC16 | Pause ACK30s, Sent/history200, pending1; Resume/requeue cùng actual event, không duplicate | PASS | Chưa xác nhận |
| C03 / AC21 / TC23 ordering | Actual event trước release HTTP, local duplicates/stale có nhãn; current2 thắng response1, draft mới giữ nguyên | PASS | Chưa xác nhận |
| C04 / AC12 / TC12 subscribe | Outsider404/no payload; unsubscribe ACK/room switch cùng transport; resub/history200 | PASS | Chưa xác nhận |
| DM-SQL-06 dispatcher | Actual SIGKILL sau committed claim, expiry recovery attempts2, same event ID, lease cleared | PASS | Chưa xác nhận |
| Rollback / retry/backoff | No message/event khi writer rollback; lỗi mark-published retry đúng fixed code/availableAt, không claim trước due | PASS | Chưa xác nhận |
| Current/tombstone / TC08 cơ chế | Worker đọc current DTO, replay old writer không phục hồi body; production edit/delete chưa có | PASS cơ chế fixture | Chưa xác nhận |
| Session cũ / TC23 phần actor/session | Logout đúng session, old negotiation401/no content; UI clear RAM/login | PASS lát cắt | Chưa xác nhận |
| Late/wrong room/actor; decimal strings | Node tests giữ version/sequence trên2^53, buffer trước subscribe/history, bỏ event cũ/actor cũ | PASS component | Chưa xác nhận |
| Backend integration | PostgreSQL18 thật; TRX82 executed82 passed82 failed0, gồm6 test RealtimeHub mới qua TestServer WebSocket | PASS82/82 | Chưa xác nhận |
| FE unit/build | Node57/57; Vite build asset `index-BDCh9-Y8.js` | PASS | Chưa xác nhận |
| Actual browser | `p4verified`8/8, gồm C01–C04/rollback/session/current/crash; C04 browser C bổ sung ở `p4route` | PASS | Chưa xác nhận |
| Hub BE độc lập | `p4befinal`C01–C04 actual API15026/WebSocket/PG; cleanup logout toàn bộ sessions | PASS4/4 | Chưa xác nhận |
| Migration | DB fresh/current + upgrade accepted schema; hai replays mỗi DB; pending metadata/timestamps/attempt3 giữ nguyên, lease nullable null/index/check đủ | PASS | Chưa xác nhận |
| Log scan | API/Nginx: query token0, JWT-like0, sample body0; chỉ lưu counts, không raw log | PASS mẫu đã chạy | Chưa xác nhận |
| Reconnect nhiều trang/cachewindow / TC17/25 | Chưa triển khai P4-T02; không thay bằng latest/manual button | Chưa chạy trong P4-T01 | Chưa xác nhận |
| Full revoke đường account/member/SLA/load / P6 | Core fresh guard/session tested; ma trận đầy đủ và SLA dưới áp lực chưa chạy | Chưa chạy phần P6 | Chưa xác nhận |
| Mobile bàn phím/thiết bị | Edge viewport không chứng minh thiết bị mobile thật | Chưa chạy | Chưa xác nhận |

Bằng chứng local nằm ngoài Git: `.dm-acceptance/backend-artifacts/test-results/dm-acceptance.trx`, `runs/p4verified/e2e-results.json`, `runs/p4verified/browser-*.json`, `runs/p4route/browser-C04.json`, `runs/p4befinal/hub-proof-*.json`, `runs/p4proof/realtime-migration.json`, `p4-log-scan.json`. Metadata IDs/version/status/counts được giữ; auth/token/body private/key bytes không đưa vào hồ sơ.

Lệnh tự động chạy lại dùng dataset **mới**, không dùng p4user/p4be sau khi đã test thủ công:

```powershell
DM Setup C01 p4again
DM Fixtures C01 p4again
Push-Location clients\WebClient
$env:DM_P4_REALTIME_RUN='p4again'
npx.cmd playwright test --config playwright.dm-p4-t01.config.js
Pop-Location
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dm-acceptance\test-realtime-migration.ps1 -ProofRun p4proof2
# Backend full suite: pause runtime worker trước vì tests tự drive dispatcher.
DM Pause
try { powershell -NoProfile -ExecutionPolicy Bypass -File scripts\dm-acceptance.ps1 -Action BackendTests }
finally { DM Resume }
```

Các lỗi runner trong lúc phát triển đã sửa: username filter khớp prefix nhiều run → exact suffix; SQL `version` ambiguous → `m.version`; crash health path `/health` → `/api/v1/health`. Lỗi sản phẩm sửa: session AccessRevoked chỉ xóa chat RAM nhưng giữ UI authenticated → clear phiên local; regression session và đầy đủ E2E đã chạy trên asset mới. Lần lỗi trước không được dùng thay bằng chứng PASS cuối.

## Cleanup, rủi ro và xác nhận

Fault controls chỉ Development của stack nghiệm thu, không thêm endpoint/product controls. Sau các ca: Resume, ReleaseClaim đúng event nếu đã arm, FaultOff ROLLBACK, off các browser fixtures, logout helper sessions. Giữ manifest/evidence/data. Production mặc định không có control directory; registry singleton chỉ hỗ trợ một API host MVP. At-least-once có thể lặp frame khi send thành công nhưng mark published lỗi; UI merge version/ID giữ một dòng. Một event chậm có thể chặn worker đơn đến timeout/expiry; không claim đã benchmark tải.

Migration additive tương thích writer cũ, giữ pending events/body-free payloads. Nếu rollback code, giữ hai lease columns/index và ngừng worker mới trước chuyển runtime; không drop database/volume hoặc xóa messages để rollback task.

Người dùng chưa xác nhận PASS/FAIL FE và BE trên bản bàn giao này. Quyền commit/push nhánh task và merge `message` đã được cấp; merge vẫn chờ **bạn PASS cả FE/BE** trên đúng build và xác minh remote task SHA. `message` hiện giữ `9de920a`; chưa có merge P4-T01. P4-T02 chưa có branch/PR hoặc quyền thực thi bước kế trong task này.

Bạn có thể phản hồi: **“DM-P4-T01 PASS FE và BE trên build <SHA bàn giao>; nghiệm thu.”** Nếu FAIL, ghi case/bước, account/run, expected/actual; task tiếp tục sửa trên nhánh này và giao build mới để bạn test lại.

## Dữ liệu và Git bàn giao

Theo quyền mới12/10/2026, không tạo PR/merge riêng P4-T01. Source P4 được kế thừa và mở rộng trong code commit `f0e8dd1` trên `feat/dm-mvp-completion`; [biên bản MVP](DM-MVP.md) giữ commit/image/remote proof/dataset ID cuối và một lần user test FE/BE. Ảnh build/agent evidence P4 phía trên là mốc trước mở rộng, không thay kết quả final MVP hoặc tự ghi người dùng PASS.

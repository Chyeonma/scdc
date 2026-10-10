# DM-P3-T02 — sequence, transaction, fingerprint và key rotation

Trạng thái: **Chờ người dùng test FE/BE**. Ngày10/10/2026, người dùng xác nhận “P3-T01 đã PASS FE/BE; nghiệm thu và làm P3-T02.” P3-T01 đã merge `--no-ff`, push và xác minh `message` tại `a3fea4e56f86169a0e583b0ed5eaec1898fccf63`. P3-T02 bắt đầu từ đúng base này, chưa merge và chưa thực hiện P4.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / phase | DM-P3-T02 / P3; đủ5 subtask trong kế hoạch |
| Branch / base / PR target | `test/dm-p3-t02-concurrency-and-keys` / `a3fea4e` / `message` |
| Code commit / PR / remote proof | Code `4102955f4d16c49513802c6be97f456b7747113c`; [draft PR30](https://github.com/Chyeonma/scdc/pull/30); SHA cuối hồ sơ xem Git bàn giao |
| Checkout chạy | `E:\Project\SCDC\dm-message-integration` (worktree của repo scdc; giữ thay đổi người dùng ở checkout gốc) |
| FE / API / Swagger | <http://localhost:15300> / <http://localhost:15026/api/v1> / <http://localhost:15026/swagger> |
| Compose / PostgreSQL | `scdc-dm-acceptance` / `scdc_dm_acceptance_test`, PostgreSQL18, cổng15432 |
| FE asset / image | `index-pm6lhpkq.js` / `sha256:7903952bfd9a736797f3aed205170891f7ea167117bc69e90a9f84ffead59928` |
| API image | `sha256:93050df47fc5eaf79e90eac85b9512b23a96b1e940de5d1ea2c741961b8a8467` |
| Product/schema | Giữ source đã nghiệm thu P3-T01/P2; bổ sung integration test, E2E và harness local. Reuse migration `20261009_dm_p2_text_message.sql`; không tạo migration mới |
| Config | Key mặc định `dm-local-v1`; thử K1/K2 bằng mount local riêng rồi Restore. Giữ cursor/HMAC ring và volume |
| Browser | Edge Chromium headless trên Windows,1440×1000,1worker; hai browser context/login độc lập; không trace/video/HAR |
| Run agent | `p3seqfinal` browser; `p3seqagentbe` runner; `p3seq` migration/vector và lịch sử lỗi harness |
| Run người dùng | **`p3sequser` FE**, **`p3seqbe` BE**; không dùng lẫn lane |
| Người dùng | C01–C04 và bổ sung: **Chưa xác nhận** |

## Chuẩn bị và lệnh chạy

Mở Docker Desktop/Linux engine trước. Chạy PowerShell với `-Action`; Code Runner chạy file không truyền tham số sẽ hỏi Action. Checkout `E:\Project\SCDC\scdc` vẫn giữ baseline và thay đổi của bạn; phải chạy script từ integration dưới đây để tránh dựng bản cũ lên cùng cổng.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
$repo = (Get-Location).Path
$runner = Join-Path $repo 'scripts\dm-acceptance\concurrency.ps1'
$send = Join-Path $repo 'scripts\dm-acceptance\send-text.ps1'
$fe = Get-Content .dm-acceptance\runs\p3sequser\sequence-fixtures.json -Raw -Encoding UTF8 | ConvertFrom-Json
$be = Get-Content .dm-acceptance\runs\p3seqbe\sequence-fixtures.json -Raw -Encoding UTF8 | ConvertFrom-Json
function DM([string]$Action,[string]$Case='C01',[string]$Run='p3sequser') {
  & powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action $Action -Run $Run -Case $Case
  if($LASTEXITCODE -ne 0){throw "DM $Action failed"}
}
function Send-BE([string]$Case,[string]$Op,[string]$Text,[int]$Status=200) {
  $p=@($be.pairs | Where-Object case -eq $Case)[0]
  $uuid=[guid]::NewGuid(); if($Op -in @('X','Y')){$uuid=[guid]$p.$Op}
  & powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run p3seqbe -PeerAlias $p.peerAlias -ClientMessageId $uuid -Content $Text -ExpectedStatus $Status
  if($LASTEXITCODE -ne 0){throw 'Send-BE failed'}
}
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Status
```

Start/Stop/Restart đã kiểm chứng trên stack này, giữ nguyên dữ liệu/key. Nếu cần build từ checkout task, bỏ `-NoBuild`. Khi cần dừng hoặc khởi động lại:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Stop
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Restart
```

Hai run người dùng đã được tạo, không Setup lại. Lượt kiểm thử độc lập mới dùng suffix khác và tạo cả Setup/Fixtures; runner từ chối ghi đè manifest, fixture hoặc X đã commit:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Setup -Run p3seqnext
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Fixtures -Run p3seqnext
```

Password local tất cả tài khoản active: **`DmDemo2026!Local`**. FE đăng nhập A=`dm_demo_an_p3sequser`. Mở hai profile Edge/Chrome riêng hoặc hai trình duyệt, đăng nhập A độc lập ở A1/A2; không dùng hai tab chung localStorage để chứng minh hai phiên. Trong mỗi profile: bấm **+ → nhập nguyên username peer → chọn đúng dòng @username → Mở hội thoại → chờ hết Đang tải tin nhắn**. B/C cùng display name Bảo Demo; phân biệt username.

**Lane FE `p3sequser`**: A=`dm_demo_an_p3sequser`, actorId=`01a12690-e35c-79e9-af54-a0774c3480b3`.

| Case / peer | Username | Conversation ID | X / Y (clientMessageId) |
|---|---|---|---|
| C01 / B | `dm_demo_bao_p3sequser` | `01a12691-27c1-759a-b578-1ba454551170` | X=`56625a80-446b-4bf2-bf26-ea051c181164`; Y=`30202cb7-005d-47e1-9d43-e6938c595ec2` |
| C02 / C | `dm_demo_chi_p3sequser` | `01a12691-27da-77bb-869b-ea255fb3e9d2` | X=`c47b2456-4744-4073-9fdb-b474d9a058f7`; Y=`17790d75-b52e-42db-a7bf-08f10fd09ef1` |
| C03 / S01 | `dm_demo_search01_p3sequser` | `01a12691-27ee-7334-bb67-a08159287293` | X=`d6976184-b56e-40b5-83b5-76a6c9188155`; Y=`deee565c-6360-4312-80b7-03cbbcf3cdfa` |
| C04 / S02 | `dm_demo_search02_p3sequser` | `01a12691-2800-75b6-b356-cde06975efa3` | X=`76317168-be05-4f24-860c-da4d008c637b`; Y=`c01f18d9-0395-4063-bf6e-f93f167a4998` |
| R101 / S03 | `dm_demo_search03_p3sequser` | `01a12691-2812-78cd-ab9c-182aa02a635c` | X=`20b78339-f9f1-49e3-b987-33a2bf2645fa`; Y=`ca04186d-7c8a-423d-9080-3dff1c34adb1` |

**Lane BE `p3seqbe`**: A=`dm_demo_an_p3seqbe`, actorId=`01a12691-39ba-7d09-a083-e36ddc150c6e`.

| Case / peer | Username | Conversation ID | X / Y (clientMessageId) |
|---|---|---|---|
| C01 / B | `dm_demo_bao_p3seqbe` | `01a12691-8003-7f8d-a5e7-02cd7a89e72d` | X=`8065079a-4ff7-4e07-bc17-a5a0d494c8cf`; Y=`9e86cb57-dfff-4d29-8a52-dc9b3de37c50` |
| C02 / C | `dm_demo_chi_p3seqbe` | `01a12691-8022-70de-9c93-e52ec45cb520` | X=`9adc1024-61d1-4b27-9756-97c414961246`; Y=`1c96bc15-6f7e-49a0-83e1-c6206f239498` |
| C03 / S01 | `dm_demo_search01_p3seqbe` | `01a12691-803a-7b44-af11-ff963e92ac2f` | X=`11c7aaaa-1b11-43c2-a711-f1ce6d93c01f`; Y=`5e192f5d-c84d-4222-a16e-a65f9fb95237` |
| C04 / S02 | `dm_demo_search02_p3seqbe` | `01a12691-804c-76f2-bb60-ab992525ed93` | X=`b10e5607-31b5-45e1-b7d1-74b7d6efff5d`; Y=`8a980b14-dbc1-45d9-8330-975291dea11e` |
| R101 / S03 | `dm_demo_search03_p3seqbe` | `01a12691-805e-74bf-967a-bfdf6ed2a003` | X=`614e1d19-cd15-42d4-a54a-b7d5d0894bba`; Y=`02858ee5-72d5-4141-8f7b-7f6681830320` |


Baseline bàn giao C01–C04 ở cả hai run: message=operation=outbox=counter=0. FE R101 đã có101 tin `R101-001…101`; BE R101 còn trống để bạn tự chạy writer. Dataset khác/P2 H121/bigint và các key cũ được giữ lại. Evidence và manifest ở `.dm-acceptance/runs/<run>/`, ngoài Git.

## Fixture FE dùng cho C01–C04

[sequence-fixture.js](../../../scripts/dm-acceptance/sequence-fixture.js) gán đúng một UUID trước khi UI tạo operation bất biến, không sửa body/UUID trên request sau đó. [retry-faults.js](../../../scripts/dm-acceptance/retry-faults.js) chỉ giữ/bỏ response của đúng một POST trên localhost15300; API và DB vẫn chạy thật. Không có nút developer trong sản phẩm.

Copy file JS vào clipboard bằng terminal, rồi dán nguyên nội dung vào Console của profile cần dùng:

```powershell
Get-Content .\scripts\dm-acceptance\sequence-fixture.js -Raw -Encoding UTF8 | Set-Clipboard
# Dán vào Console A1 và A2, rồi chạy arm() với UUID tương ứng trong bảng.
# Khi ca yêu cầu drop/delay, copy file thứ hai và dán vào các Console cần dùng:
Get-Content .\scripts\dm-acceptance\retry-faults.js -Raw -Encoding UTF8 | Set-Clipboard
```

Để lấy chính xác UUID/space từng ca mà không gõ lại: `$p=@($fe.pairs | Where-Object case -eq 'C01')[0]; $p | Format-List`. Trong Console thay giá trị từ bảng bằng chuỗi UUID thật:

```javascript
window.dmSequenceFixture.arm('UUID-X-HOẶC-Y-TRONG-BẢNG');
// Tắt mọi injection sau ca, ở cả A1 và A2:
window.dmRetryFixture?.off(); window.dmSequenceFixture?.off();
```

Không reload/logout khi cần giữ operation failed để Retry: thao tác này chỉ ở RAM tab. Reload vẫn đọc tin đã commit từ server.

## DM-P3-T02-C01 — hai writer commit theo khóa

AC-DM-03, TC-DM-15; DM-SQL-02/03/06. Lane FE `p3sequser/C01`; lane BE `p3seqbe/C01`. Agent PASS trên hai lane độc lập (`p3seqfinal`, `p3seqagentbe`); người dùng **Chưa xác nhận**.

FE làm theo thứ tự:

1. A1/A2 đăng nhập độc lập, mở peer C01; nhập sẵn A1=`SEQ-X-C01`, A2=`SEQ-Y-C01`, chưa Gửi. Dán sequence-fixture vào cả hai Console, arm X ở A1, Y ở A2. Mở DevTools Network nếu muốn xem status, không chụp header Authorization.
2. Terminal chạy `DM Snapshot C01` (baseline0), rồi `DM Hold C01`. Phải thấy `held:true`, PID/gate/trigger riêng của ca. Trigger chặn X **trước INSERT outbox và trước commit**, khi X đã giữ space lock; không chặn mọi DM.
3. Bấm Gửi ở A1; chạy `DM Inspect C01` để xem gate. Bấm Gửi ở A2; UI cả hai đang gửi, chưa sent. Chạy lệnh dưới đây để xác nhận chuỗi chờ, chụp MVCC probe rồi release. Chuẩn bị các cửa sổ trước Hold; FE timeout15s nên thực hiện bước3 nhanh, không dừng chụp ảnh giữa chừng.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Inspect -Run p3sequser -Case C01 -ExpectedWaiters 2
DM Snapshot C01
DM Release C01
DM Snapshot C01
```

4. Probe trước release vẫn0/0/0/counter0; sau release POST X/Y200, X.sequence=`"1"`, Y.sequence=`"2"`, ID khác nhau, mỗi UI chuyển sent. Snapshot sau=2/2/2/counter2; `outboxContainsBody=0`. Đăng nhập B=`dm_demo_bao_p3sequser` ở profile khác, mở A rồi reload: hai tin X,Y đúng thứ tự.
5. Tắt sequence fixture ở cả hai profile. Không Retry nếu muốn giữ kết quả nguyên ca. Nếu vượt15s, UI có thể unknown dù DB commit; ghi thời điểm, GET history và đối soát UUID trước kết luận. Lượt lặp dùng run mới, không xóa tin.

BE tự kiểm tra (runner đã chạy PASS; hai login API độc lập, không phải hai request chung session):

```powershell
DM Prove C01 p3seqbe
DM Snapshot C01 p3seqbe
Get-Content .dm-acceptance\runs\p3seqbe\sequence-be-C01.json -Raw -Encoding UTF8
```

Runner giữ X, đợi `pg_blocking_pids` thấy X rồi Y, kiểm probe chưa commit; release; xác nhận statuses `[200,200]`, sequences `["1","2"]`, delta2 message/operation/outbox/counter. GET after **protected resumeCursor baseline** phải bằng tập ID writer, không mất commit trễ. Browser và backend integration còn mở reader trong lúc giữ X, chứng minh reader là waiter thứ3; reader không được đọc frontier chưa sealed. Poll chỉ chờ điều kiện PG, không dùng thời gian sleep làm bằng chứng.

Biến thể response trả ngược thứ tự đã PASS browser: delay response X60s sau API200 thật, Y được nhận trước; A1 Làm mới tin nhắn thấy hai tin, release response X cũ vẫn đúng hai dòng sequence1,2. Để tự lặp biến thể, dùng run mới; ở A1 bật delay trước Gửi X bằng retry-fixture như C03 nhưng `mode:'delay',delayMs:60000`; sau DB commit, bấm Làm mới tin nhắn rồi `window.dmRetryFixture.release()` trước timeout15s. DB vẫn chỉ2 bộ records.

## DM-P3-T02-C02 — rollback X, Y tiếp tục commit

TC-DM-15/05; DM-SQL-02/03/06. FE/BE dùng peer C02, baseline0. Agent PASS browser/API/PG và runner; người dùng **Chưa xác nhận**.

Lặp bước C01 với **C02**, arm đúng X/Y C02; A1 nhập `ROLLBACK-X-C02`, A2 nhập `COMMIT-Y-C02`. Chuẩn bị draft trước Hold. `DM Hold C02` cài cùng loại barrier, sau release cố ý ném SQLSTATE58000 tại outbox của X trong transaction đang mở:

```powershell
DM Snapshot C02
DM Hold C02
# Sau khi bấm Gửi X rồi Y ở hai profile:
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Inspect -Run p3sequser -Case C02 -ExpectedWaiters 2
DM Snapshot C02
DM Release C02
DM Snapshot C02
# BE riêng, không dùng lane FE:
DM Prove C02 p3seqbe
```

Kỳ vọng: trước release0; X trả **503 AUTHORITY_UNAVAILABLE**, UI X lỗi/có **Thử gửi lại**, không sent giả; Y200 sequence=`"1"`. Sau release=1message/1operation/1outbox/counter1, không UUID X trong messages/operations/outbox; không tự Retry X. C=`dm_demo_chi_p3sequser` mở A/reload chỉ thấy `COMMIT-Y-C02`. Trigger đã gỡ, lock giải phóng. BE báo statuses `[503,200]`, sequence `["1"]`, history bằng đúng ID Y; evidence `sequence-be-C02.json`. Tắt Console fixture ở A1/A2.

## DM-P3-T02-C03 — K1→K2 và thiếu key

TC-DM-24/06/07; DM-SQL-03/04/06. FE/BE dùng C03, baseline0. Agent PASS actual API config restart, hai UI profile và DB. Người dùng **Chưa xác nhận**. Lệnh Keys khởi động lại API/proxy dùng chung stack, vì vậy thực hiện ca này riêng, không chạy mutation ca khác cùng lúc.

FE cần A1/A2 đều giữ cùng failed operation X; A2 giữ failed để sau khi A1 thành sent vẫn thử được thiếu old key:

1. Terminal chạy modeK1 dưới đây. A1/A2 login độc lập, mở C03. Dán cả hai JS vào mỗi Console, arm **cùng X C03** ở cả hai. Bật drop bằng câu Console được tạo dưới đây, nhập `KEY-ROTATE-C03` và Gửi từng profile. POST thật đều200 cùngID; fixture bỏ reply sau commit, UI cả hai có Thử gửi lại. Snapshot chỉ1/1/1/counter1. Xem `window.dmRetryFixture.last()` lấy `messageId` (không token/body).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode K1
$p=@($fe.pairs | Where-Object case -eq 'C03')[0]
"window.dmRetryFixture.on({conversationId:'$($p.conversationId)',mode:'drop'});" | Set-Clipboard
# Dán câu này vào Console A1 và A2 trước lần gửi ban đầu.
DM Snapshot C03
```

2. ChuyểnK2, ở **A1** bấm Thử gửi lại (không arm UUID mới):200 cùngID, một dòng sent. A2 giữ lỗi chưa Retry. Ở A1 arm **Y C03**, nhập `KEY-NEW-C03`, Gửi mới200. KeyStatus:2operations, keyIds chứa p3-k1/p3-k2, validHashes2, fingerprintVersion1. Counter2.
3. MissingK1: ở **A2** Retry X trả **503 FINGERPRINT_KEY_UNAVAILABLE**, vẫn UUID/body/ID cũ, không409 và không thêm sent. Snapshot không đổi2.
4. MissingK2: ở A1 nhập `KEY-MISSING-ACTIVE-C03`, Gửi tin mới (UUID mới do UI tạo) trả **503 FINGERPRINT_KEY_UNAVAILABLE**; không thêm record/counter và không sent giả. Không Retry tin mới này sau Restore trong phạm vi ca, vì Retry khi key đã có sẽ là mutation thứ3.
5. Restore giữ K1/K2 ở key ring mặc định ngoài Git, restartAPI/proxy; A2 Retry X200 đúngID cũ, count vẫn2. Làm mới/reload history thấy đúnghai tin; operation active-missing chưa commit mất khỏi RAM sau reload. Tắt fixtures ở cả hai profile.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode K2
DM KeyStatus C03
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode MissingK1
DM Snapshot C03
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode MissingK2
DM Snapshot C03
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode Restore
DM KeyStatus C03
```

Chạy từng lệnh ở đúng bước FE; không chạy cả block trước khi thao tác UI.

BE lane riêng, copy nguyên block sau khi đã nạp hàm ở mục chuẩn bị; từng Send-BE login/logout giữ token trong RAM, cùng actor/UUID/body lấy từ manifest:

```powershell
try {
  DM Snapshot C03 p3seqbe
  powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3seqbe -Case C03 -KeyMode K1
  if($LASTEXITCODE -ne 0){throw 'K1 failed'}
  Send-BE C03 X 'KEY-ROTATE-C03'
  powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3seqbe -Case C03 -KeyMode K2
  if($LASTEXITCODE -ne 0){throw 'K2 failed'}
  Send-BE C03 X 'KEY-ROTATE-C03'
  Send-BE C03 Y 'KEY-NEW-C03'
  DM KeyStatus C03 p3seqbe
  powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3seqbe -Case C03 -KeyMode MissingK1
  if($LASTEXITCODE -ne 0){throw 'MissingK1 failed'}
  Send-BE C03 X 'KEY-ROTATE-C03' 503
  powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3seqbe -Case C03 -KeyMode MissingK2
  if($LASTEXITCODE -ne 0){throw 'MissingK2 failed'}
  Send-BE C03 NEW 'KEY-MISSING-ACTIVE-C03' 503
} finally {
  powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3seqbe -Case C03 -KeyMode Restore
}
Send-BE C03 X 'KEY-ROTATE-C03'
Send-BE C03 Y 'KEY-NEW-C03'
DM Snapshot C03 p3seqbe
```

Expect lỗi như FE; Send helper kiểm status và không có DB delta khi rejected. Ghi lại ID O1/O2 từ DTO, version=`"1"`; replay sau restore phải giữID/sequence. Key directory đầy đủ copy key ring hiện có để giữ retry của dataset cũ; missing mode là mount thư mục tổng hợp khác, không xóa key thật. Restore giữ file `p3-k1.key`/`p3-k2.key` ngoài Git để các operation đã test tiếp tục retry được. Không in/commit keybytes/config chứa secret.

## DM-P3-T02-C04 — legacy unverifiable, UUID/network order và bigint

TC-DM-24/08; DM-SQL-01/03/04/05. FE/BE dùng C04, baseline0. Agent PASS browser/API/PG, migration fresh/existing/replay và vectors. Người dùng **Chưa xác nhận**.

FE:

1. A1 mở C04, dán hai fixtureJS; arm X C04, bật drop scopedconversation như C03. Nhập `LEGACY-ORIGINAL-C04`, Gửi. API200/counter1 nhưng UI lỗi do dropped response; giữ tab, không reload.
2. Trong Console `window.dmRetryFixture.last().messageId` lấyID thật. Chạy helperLegacy dưới đây vớiID này. Helper chỉ nhận message đúng space+author+createdBy của run; tạo tình huống legacy đã sửa hiện hành, replay migration hai lần. Đây là **fixture SQL test**, không phải endpoint edit/delete production.

```powershell
$mid=Read-Host 'Message ID từ dmRetryFixture.last()'
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Legacy -Run p3sequser -Case C04 -MessageId $mid
DM KeyStatus C04
DM Snapshot C04
```

3. A1 Retry giữUUID/body cũ: **409 OPERATION_UNVERIFIABLE**, không fakehash tính từ body hiện hành, không thêmID. KeyStatus=operations1/validHashes0, keyId/fingerprintVersion=null. Làm mới tin nhắn rồi reload: cùngID đọc được `LEGACY-CURRENT-C04`, version=`"2"`, chỉ1tin/counter1. Tắt fixture.

BE riêng:

```powershell
Send-BE C04 X 'LEGACY-ORIGINAL-C04'
$snap=DM Snapshot C04 p3seqbe | ConvertFrom-Json
$mid=$snap.messages[0].id
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Legacy -Run p3seqbe -Case C04 -MessageId $mid
Send-BE C04 X 'LEGACY-ORIGINAL-C04' 409
Send-BE C04 X 'LEGACY-CURRENT-C04' 409
DM KeyStatus C04 p3seqbe
DM Snapshot C04 p3seqbe
# Đọc current history bằng API, auth trong RAM, không in token:
Import-Module .\scripts\dm-acceptance\Common.psm1 -Force -DisableNameChecking
$s=Connect-DmActor A p3seqbe 'P3-T02-user-history'
try {
  $p=@($be.pairs | Where-Object case -eq 'C04')[0]
  $h=Invoke-DmRequest GET "/direct-conversations/$($p.conversationId)/messages?limit=50" -AccessToken $s.accessToken
  Assert-DmStatus $h 200 'Legacy history'
  $h.Body.items | Select-Object id,clientMessageId,content,sequence,version
} finally { $null=Invoke-DmRequest POST /auth/logout @{refreshToken=$s.refreshToken} }
```

Hai retry409 đều errorCode OPERATION_UNVERIFIABLE; đọc200 cùngID/currentbody/version2. Chỉ1 bộ message/operation/outbox; oldbody không được migration sao chép vào operation/outbox. Helper thay currentbody kỹ thuật thuộc run; không thử tính lại hash để hợp thức hóa legacy.

Fingerprint và migration proof độc lập (không gửi tin vào lane FE/BE):

```powershell
DM Fingerprint C04 p3seqbe
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\test-text-migration.ps1 -ProofRun p3seqbe
Get-Content .dm-acceptance\runs\p3seqbe\migration.json -Raw -Encoding UTF8
```

Fingerprint:6vectorHMAC-SHA256 PASS bằng Python stdlib độc lập và test C# dùng production hasher; UUID pair **00000001-0000-4000-8000-000000000000 < 00000100-0000-4000-8000-000000000000** khớp PostgreSQL network order, bẫy little-endian cho thứ tự ngược. Hash đầu vào prefix `SCDC.Send.v1\0`,3UUID networkbytes,UInt32BE UTF8length,body CRLF/CR→LF giữ nguyên khoảng trắng/NFC. So digest dùng `CryptographicOperations.FixedTimeEquals`; flip byte0/15/31 đều409 conflict, không DBdelta, restorehash→retry200. Đây là proof hành vi/path, **không phải đo timing để chứng minh constant time**.

Migration report: upgrade2legacy messages sequence1,2,2unverifiable operations, retainedCounter=`"42"`, oldBodyCopies0,outboxRows0; freshmessage0/cột và bảng có; replayPassed/tombstoneAccepted=true. Runner tạo rồi dọn đúnghai DB proof tên random của chính lượt đó; **không drop `scdc_dm_acceptance_test`, không xóa volume ứng dụng**. Nguồn baseline upgrade `6e9ea045aaea5b87734265f0cbbb050c01842203`, migration acceptedP2; giữ compatible legacy/currentcontent.

Bigint production DTO sequence/version là decimalstring; backend test current/tombstone dùng giá trị vượt2^53, Node merge dùngBigInt, E2E P2 đã nghiệm thu. Không coi UUID fixture haiuser là UUIDv4 để send; fixture này chỉ kiểm ordering/comparator.

## Bổ sung — R101, quyền/cursor, payload và SQL invariants

FE R101: login peer **dm_demo_search03_p3sequser**, bấm+ tìm **dm_demo_an_p3sequser**, mởDM; latest50=`R101-052…101`. Bấm **Tải tin cũ hơn** thêm50=`R101-002…051`, bấm lần nữa thêm1=`R101-001`: tổng101 đúng thứ tự, nút tải cũ hết. Reload lại latest50; tải thêm lại đủ101, IDs không đổi. Đây là history UI thật, chưa là Hub reconnect.

BE R101 trống; runner thực hiện101POST thật, chụp protectedresume trước writer, đọc after50/50/1, chỉ trả resume ở trang cuối, IDunion bằng writer:

```powershell
DM Catchup R101 p3seqbe
Get-Content .dm-acceptance\runs\p3seqbe\r101-proof.json -Raw -Encoding UTF8
DM Snapshot R101 p3seqbe
```

Agent browser còn ngắt transport trước mỗi trang fetch rồi dùng đúng cursor qua API khi online; mỗi lần giữ cursor cũ cho đến trang thành công, không tự replay mutation. B dùng protectedcursor của A trả **400 CURSOR_INVALID**; integration76 kiểm cursor bị sửa/hết hạn/sai actor/limit/direction/frontier và guard bị revoke/currentmembership. **TC-DM-25 phần REST/cursor đã PASS; client Hub reconnect/catchup/resume tự động Chưa chạy, chờ P4**, không đánh dấu toànTC25 hay UC hoàn tất.

Các request payload biên có sẵn generator, raw surrogate giữ đúng `\uD800`/`\uDC00` trong JSON, không thay bằng ký tự U+FFFD. Tạo lane bổ sung riêng S04 (không thay countsC01–C04) và toàn bộ fileUTF8/UUIDv4 mới:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Open -Run p3seqbe -PeerAlias S04
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Payloads -Run p3seqbe -Lane p3edges
$payload=Join-Path $repo '.dm-acceptance\runs\p3seqbe\payloads-p3edges'
foreach($c in (Get-Content (Join-Path $payload 'cases.json') -Raw -Encoding UTF8 | ConvertFrom-Json)) {
  powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run p3seqbe -PeerAlias S04 -RequestFile (Join-Path $payload ($c.id+'.json')) -ExpectedStatus $c.expectedStatus
  if($LASTEXITCODE -ne 0){throw "Payload failed: $($c.id)"}
}
```

Expected: max-ascii2000/max-emoji1000→200; ascii2001/emoji1001→400; CRLF/CR normalize trước đếmUTF16; blank/invisible→400 CONTENT_EMPTY; rawsurrogate/NUL→400 CONTENT_INVALID; quá2.000UTF16→400 CONTENT_TOO_LONG; tiếngViệtNFC/NFD đều giữ body gốc, không trim/NFC; HTML là text. Mọi rejectedsend có message/op/outbox/counter/activity delta0. Corpus đã PASS trong bộ76backend/53Node; generator/runner reuseacceptedP2, ca lặp mutation dùng lane mới.

SQL chỉ đọc, space lấy từ fixture (C01 ví dụ; thaycase/run để kiểmC02–C04). FileSQL không in content/hashbytes/token/key:

```powershell
$p=@($fe.pairs | Where-Object case -eq 'C01')[0]
Get-Content .\docs\fixtures\dm-p3-t02-invariants.sql -Raw -Encoding UTF8 | docker compose --project-name scdc-dm-acceptance --env-file .env.dm-test -f compose.dm-test.yaml exec -T postgres psql -X -U scdc_dm_test -d scdc_dm_acceptance_test -v "space_id=$($p.conversationId)"
```

| Invariant / TC | Proof đã chạy / giới hạn |
|---|---|
| DM-SQL-01 / TC-DM-02 | 40 concurrent open trong suite76 cùngspace, đúngpair/members; exactUUID pair C#/Python/Postgres nhất quán |
| DM-SQL-02 / TC-DM-15 | C01/C02:space lock giữ cảreader; probe0; commitseq1,2 hoặcrollback→Yseq1; protectedafter union bằng writer |
| DM-SQL-03 | Message/sendoperation cùngtx; replay giữID/version/body current; missinghashlegacy409; missingkey503; hashmismatch409; orphan0 |
| DM-SQL-04 | Unicode/text corpus UTF16normalize, strictsurrogate, khôngtrim/NFC; fingerprint6vector; DTOstringBigInt, khôngcastNumber |
| DM-SQL-05 | Operation chỉhash/keymetadata; legacykhôngbackfillbodycũ; current/tombstone replaykhônghồi sinhbody; khônglogbody/key/token |
| DM-SQL-06 | Outboxcùngtx:commit2/rollback1, payloadchỉconversation/message/version; outboxbody0; dispatcher/lease/crash/recovery **chờP4**, Chưa chạy |

SQL report kỳ vọng orphanOperations/orphanOutbox/sequenceDuplicates/outboxBodies/remainingSequenceBarriers đều0. C03 hash_bytes32/keyversion1; C04hash/keyversionnull. Counterkhớpca, không dùng MAX+1 ngoài khóa.

## Cleanup, lỗi harness và bằng chứng

Sau C01/C02 luôn Release; helper tự release sau120s nhưng FE có timeout15s nên không dựa TTL để nhận sent. Release có xử lý orphan: chỉ terminate đúngPID psql trong testDB đang sở hữu advisorykey đúng của fixture, rồi gỡ đúngtrigger/function random; không đụng backend session khác. Sau keys luôn Restore. Khi tắt bằng Ctrl+C, thực hiện cleanup này trước test lại:

```powershell
DM Release C01
DM Release C02
powershell -NoProfile -ExecutionPolicy Bypass -File $runner -Action Keys -Run p3sequser -Case C03 -KeyMode Restore
# Nếu dùng lane BE thì thay -Run p3seqbe và DM Release ... p3seqbe.
```

Chỉ Release ca đã Hold; chưa cóstate thì báo Holdfirst. TắtJS ở cảhaiConsole. Không xóaDB/volume để lặp test; giữ evidence và chọn run mới.

| Ca agent | Expected / actual | Evidence local ngoài Git | Kết quả / người dùng |
|---|---|---|---|
| C01 browser | X,Y,readerwaiters1/2/3; probe0; statuses200/200; seq1/2;2records/counter2; B reload2; GET đến trước replyX khôngduplicate | `runs/p3seqfinal/sequence-c01.json` | PASS / Chưa xác nhận |
| C02 browser | waiters1/2/3; X503AUTHORITY_UNAVAILABLE,Y200seq1; probe0→1; receiverreload1, khôngautretryX | `runs/p3seqfinal/sequence-c02.json` | PASS / Chưa xác nhận |
| C03 browser/API | K1old200sameID,K2new; missingold/active503noDBdelta; defaultrestoreold200;2hashes/keyIDs | `runs/p3seqfinal/sequence-c03.json` | PASS / Chưa xác nhận |
| C04 browser/API | original/currentretry409unverifiable; sameIDcurrentversion2readable;1record/hashnull; migration2replays | `runs/p3seqfinal/sequence-c04.json` | PASS / Chưa xác nhận |
| C01/C02 BE runner | độc lập2login; locks/probe/sequence/DBdelta/historyunion đúng | `runs/p3seqagentbe/sequence-be-C01.json`, `sequence-be-C02.json` | PASS / Chưa xác nhận |
| R101 | REST50/50/1; browserlatest50→100→101;3transportofflineprobes; crossactor400 | `runs/p3seqfinal/r101-proof.json`, `sequence-r101-browser.json` | PASS phần REST/history / Hub Chưa chạy |
| Migration/vector | upgrade2/nullhash2/counter42/replay; fresh0/tombstone;6HMAC/exactPGUUID | `runs/p3seq/migration.json`, `fingerprint-proof.json`; backendTRX | PASS / Chưa xác nhận |
| Backend | 76/76 PostgreSQL thật, gồm guards/current/replay/concurrency/cursor/hash | `.dm-acceptance/backend-artifacts/test-results/dm-acceptance.trx` | PASS |
| Frontend | 53/53 Node; Vitebuildassetkhôngđổi;5/5 EdgeE2E2.3min | `.dm-acceptance/e2e-p3-t02/results.json` | PASS |

Các path `runs/` trong bảng nằm dưới `.dm-acceptance/`. Không gửi HAR/authresponse/keyrings vàoGit. Proof JSON chỉ samplecontent/ID/status/counts/cursor; protectedcursor không phải token đăng nhập.

Các lượt đầu gặp lỗi **harness**: tiến trìnhPython Windows kế thừa stdoutpipe làm Hold không trả quyền điều khiển; Node `execFileSync` ngăn readerrequest dispatch; tái dùng lane đã có mutation khiến expectedcounter sai. Đã sửa stream riêng/StartProcessHidden, helperNode bất đồng bộ, chọnrunmới và chạy lại đủ5/5. Đã kiểm cleanup orphan và Hold/Release từ Node. Không đổi code sản phẩm để vượt test. Lượt handover userC01–C04 vẫn trống; không lấy PASS từ các lượt lỗi.

Chạy lại kỹ thuật theo thứ tự, không chạy backendglobalcounts đồng thời setup/send/keyE2E:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action BackendTests
# E2E cần một run hoàn toàn mới, ví dụ p3seqnext đã Setup/Fixtures ở trên.
Set-Location .\clients\WebClient
npm.cmd test
npm.cmd run build
$env:DM_P3_SEQUENCE_RUN='p3seqnext'
npx.cmd playwright test -c playwright.dm-p3-t02.config.js
```

## Git bàn giao và quyền bước tiếp

- Code/test/helper commit **`4102955f4d16c49513802c6be97f456b7747113c`** đã push; `ls-remote` cùng SHA. Commit hồ sơ tiếp theo chỉ cập nhật docs, không đổi source/schema/config/asset đã test.
- [Draft PR30](https://github.com/Chyeonma/scdc/pull/30), base `message`, head `test/dm-p3-t02-concurrency-and-keys`; trạng thái Chờ người dùng test FE/BE.
- Remote `message` vẫn **`a3fea4e56f86169a0e583b0ed5eaec1898fccf63`**, đã chứa predecessor P3-T01; code P3-T02 chưa là ancestor của message. Không merge stacked branch cũ.
- SHA bàn giao cuối là HEAD sau commit docs, được đối chiếu `ls-remote` và ghi ở `.dm-acceptance/runs/p3sequser/handover-git.json`, cùng phản hồi bàn giao. Dùng lệnh dưới đây để lấy SHA hiện tại; không tự lấy SHA image làm SHA Git.
- Smoke sau Stop/Start/Restart: userA tìm/mở đúng B, composer có, C01 trống; userS03 đọc latest50 R101; `handover-browser-smoke.json`. `handover-baselines.json`: C01–C04 cả hai run vẫn0, FE R101101/BE R1010, faulttrigger0. H121 P2 vẫn121 qua API/DB sau restart; so sánh bigint/current đã nghiệm thu được giữ.
- Checkout gốc `scdc` và outer `E:\Project\SCDC` được giữ nguyên branch/thay đổi người dùng. Các remote refs `message` và task được fetch về outer để `git branch -a` thấy; không đổi checkout của bạn.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
git -c safe.directory=E:/Project/SCDC/dm-message-integration rev-parse HEAD
git -c safe.directory=E:/Project/SCDC/dm-message-integration ls-remote --heads origin refs/heads/test/dm-p3-t02-concurrency-and-keys refs/heads/message
git -c safe.directory=E:/Project/SCDC/dm-message-integration log -1 --oneline
```

HEAD phải bằng dòng nhánh task remote, code4102955 là ancestor của HEAD. Branch tích hợp nằm trong repo scdc/worktree dm-p1-merge; chạy `git branch -a` ở outer là repository khác, cần fetch để thấy remote refs của cùng GitHubrepo.

Người dùng C01–C04/FE/BE trên bản P3-T02: **Chưa xác nhận**. Chưa merge task này vào `message`, chưa tạo branch/PR hoặc code P4. Ghi phản hồi theo mẫu dưới đây sau khi tự chạy cả hai lane. Nếu FAIL, giữ taskbranch, reproduce/sửa/regression và bàn giao build mới. Không coi im lặng hoặc câu hỏi là PASS.

```text
PASS DM-P3-T02 | FE: đạt | BE: đạt | Build: <SHA bàn giao>
# Hoặc:
FAIL DM-P3-T02 | Build: <SHA> | Ca: <C01/C02/C03/C04/TC>
Bước: ... | Expected: ... | Actual/status/errorCode/UUID/ID: ...
```

Sau PASS mới fetch/xác minh build đã duyệt, merge riêngtask `--no-ff` vào`message`, smoke/push/ancestry rồi đi task được giao. Quyền merge đã cấp có điều kiện; không merge main/phát hành. Gate UC/realtime/thiết bị mobile thật/dispatcher còn mở, không suy PASS toànphase từ task này.

# DM-P3-T01 — retry chủ động khi chưa rõ kết quả

Trạng thái hiện tại: **Người dùng PASS FE/BE**, build `156cb32` (code `ab93864`), xác nhận ngày10/10/2026. Đã merge/push `message` tại `a3fea4e56f86169a0e583b0ed5eaec1898fccf63`; smoke Node53/53, H121/bigint và proxyhealth PASS. Các phần Chờ phía dưới lưu mốc bàn giao trước nghiệm thu; xem xác nhận cuối hồ sơ. P3-T02 được giao riêng, không suy PASS realtime/P4.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / phase / scope | DM-P3-T01 / P3 / manual retry, fault transport, đối soát UI/API/DB |
| Branch / base / PR target | `feat/dm-p3-t01-manual-retry` / `b4d814d` / `message` |
| Commit code đã test | `ab93864e8a9f1d93ee95af6ad582c0bac06c425e` |
| Remote code / PR | Push + ls-remote cùng SHA code; [draft PR29](https://github.com/Chyeonma/scdc/pull/29), base `message` |
| Repository chạy | `E:\Project\SCDC\dm-message-integration` |
| Web / API / Swagger | <http://localhost:15300> / <http://localhost:15026/api/v1> / <http://localhost:15026/swagger> |
| Compose / DB | `scdc-dm-acceptance` / `scdc_dm_acceptance_test`, PostgreSQL18, cổng15432 |
| FE asset / image | `/assets/index-pm6lhpkq.js` / `sha256:7903952bfd9a736797f3aed205170891f7ea167117bc69e90a9f84ffead59928` |
| API image | `sha256:93050df47fc5eaf79e90eac85b9512b23a96b1e940de5d1ea2c741961b8a8467` |
| Schema/config | Không thêm migration hoặc đổi config API; schema gửi/lịch sử đã nghiệm thu P2, giữ key ring và volume |
| Browser agent | Edge Chromium headless, Windows, 1440×1000, 1 worker; không trace/video/HAR chứa phiên |
| Dataset agent / người dùng | `p3retry` / **`p3user`**; manifest và evidence trong `.dm-acceptance/runs/<run>/`, ngoài Git |
| Người dùng | Tất cả ca **Chưa xác nhận** trên bản P3 này |

Mỗi thao tác gửi giữ bất biến actor/conversation/UUIDv4/nội dung đã normalize và revision draft. **Thử gửi lại** dùng đúng thao tác cũ; **Gửi tin mới** lấy draft hiện tại và UUID mới. **Soạn tin mới** xóa riêng draft để dễ nhập tin khác, giữ dòng lỗi cũ. POST không tự replay sau401, refresh, mạng trở lại hoặc timeout. GET vẫn được refresh theo policy Identity.

Thời hạn chờ15 giây trả trạng thái chưa xác nhận, không khẳng định rollback. Response phải khớp actor/conversation/UUID/ID; merge theo ID/version chuỗi số bằng BigInt. Reply muộn sau timeout không đổi trạng thái, không xóa draft mới. Draft/failed operation chỉ ở RAM tab; logout/reload làm mất thao tác chưa xác nhận, tin đã commit đọc lại được qua GET. Phiên bị revoke vẫn bị guard backend chặn.

## Mở đúng bản FE/BE

Docker Desktop phải đang chạy Linux engine. Dùng terminal PowerShell và truyền `-Action`; không chạy file bằng Code Runner thiếu tham số. Checkout `E:\Project\SCDC\scdc` còn ở P0; dựng từ đó sẽ thay bản trên cùng cổng bằng code cũ.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File "E:\Project\SCDC\dm-message-integration\scripts\dm-acceptance.ps1" -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Status
```

Bản P3 đã build/chạy tại15300. Ctrl+F5 để nạp asset mới. Nếu muốn dựng lại checkout branch P3, dùng cùng lệnh Start và bỏ `-NoBuild`. Các lệnh Stop/Restart dưới đây chỉ tác động stack acceptance, giữ volume/key ring:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Stop
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Restart
```

Start/build/up đã chạy; Stop/Start/Restart được kiểm chứng trong smoke bàn giao. Đã sửa thông báo Init cũ nhắc P0 thành thông báo theo checkout; dòng Init không quyết định database/phase, bản code build từ checkout mới quyết định tính năng. Không Setup lại run đã có: helper retry từ chối việc reset manifest. Lượt test độc lập khác dùng suffix mới ngắn:

```powershell
# p3user đã tạo. Chỉ chạy hai dòng này khi cần dataset mới, ví dụ p3next.
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\retry.ps1 -Action Setup -Run p3next
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\retry.ps1 -Action Fixtures -Run p3next
```

## Dữ liệu mẫu đã tạo cho người dùng

Password tất cả tài khoản active: **`DmDemo2026!Local`**. Đăng nhập A bằng **`dm_demo_an_p3user`**, ID `01a1265c-63f2-7890-a880-aceeaedc8fcb`. Trong từng ca: bấm **+** → nhập nguyên username trong bảng → chọn dòng có đúng `@username` → **Mở hội thoại** → chờ hết **Đang tải tin nhắn**. B và C đều có display name Bảo Demo; phân biệt theo username.

| Case / alias peer | Username tìm trong modal | Conversation ID thật |
|---|---|---|
| C01 / B | `dm_demo_bao_p3user` | `01a1265c-f60c-7762-bc73-0573f970a22c` |
| C02 / C | `dm_demo_chi_p3user` | `01a1265c-f623-775c-ab1d-35dba0f80985` |
| C03 / S01 | `dm_demo_search01_p3user` | `01a1265c-f65c-7245-a957-4e9a4adee6b6` |
| C04 / S02 | `dm_demo_search02_p3user` | `01a1265c-f670-7f73-b522-3b138d5541ee` |
| DELAY / S03 | `dm_demo_search03_p3user` | `01a1265c-f684-7ebd-afc4-048ba3e8fa96` |
| CURRENT / S04 | `dm_demo_search04_p3user` | `01a1265c-f699-71f1-8c01-64def5b2d16b` |
| BE-C03 / S10 | `dm_demo_search10_p3user` | `01a1265c-f6ab-7265-8e86-e782851181e4` |
| BE-CONCURRENT / S11 | `dm_demo_search11_p3user` | `01a1265c-f6bd-771a-825a-ff96722802cc` |

Tám cặp đều đã xác minh **0 message / 0 operation / 0 outbox / counter0**, activity/message cuối null. Agent không gửi vào `p3user`. Baseline đã lưu `retry-baseline.json`; alias/ID tại `manifest.json` và `retry-fixtures.json`. Fixture/lượt độc lập phải lấy ID từ manifest mới. Dữ liệu P0/P2 đã nghiệm thu được giữ nguyên.

## Chuẩn bị thao tác FE, BE và đối soát

Các lệnh dưới đây chạy trong thư mục integration. Khởi tạo biến PowerShell một lần:

```powershell
$retry = '.\scripts\dm-acceptance\retry.ps1'
$send = '.\scripts\dm-acceptance\send-text.ps1'
$history = '.\scripts\dm-acceptance\history.ps1'
$run = 'p3user'
$payload = '.\.dm-acceptance\runs\p3user\retry-payloads'
```

Snapshot chỉ đọc, không chứa token/content tin; cho biết `messageCount`, `operationCount`, `outboxCount`, `lastSequence`, ID/UUID/version, activity, outbox metadata. Ví dụ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Snapshot -Run $run -PeerAlias B
```

Backend Send/Concurrent tự login A bằng tài khoản fixture, giữ token trong RAM, logout cuối lệnh; không cần copy token vào tài liệu. Nếu BE replay bước FE, điền đúng UUID từ FE và nguyên nội dung đã gửi; UUID mới sẽ tạo thêm tin, không phải retry. GET đọc thực tế:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $history -Action Page -Run $run -ActorAlias A -PeerAlias C -ReaderAlias C
```

Để gây lỗi transport, chép **toàn bộ** file local vào Clipboard rồi dán trong DevTools → Console trên localhost15300:

```powershell
Get-Content .\scripts\dm-acceptance\retry-faults.js -Raw -Encoding UTF8 | Set-Clipboard
```

Snippet chỉ tồn tại trong tab test, không nằm trong asset sản phẩm. `on(...)` nhắm đúng **một** request của conversation; `drop` gọi API thật, đợi200/commit rồi mất phản hồi phía JS; `delay` giữ response thật; `401` chặn trước POST; `get401` chặn một GET để kiểm thử refresh; `getdelay` giữ một response GET thật. Hai loại401 là **fault fetch giả lập**, không phải chứng minh token đã bị revoke.

`window.dmRetryFixture.last()` trả UUID/conversation/message ID/status an toàn để đối soát; không trả body/token. Với fault fetch401, request đầu bị chặn nên không xuất hiện như POST thật trong Network; dùng marker để ghi lần thử, Network đếm các POST thực tế sau đó. DevTools local không thay thế proof API/PostgreSQL của các ca commit/rollback.

Sau mỗi ca dùng Console `window.dmRetryFixture?.off()`; thao tác này phục hồi fetch và giải phóng response đang giữ. Reload cũng gỡ snippet, đồng thời xóa failed operation RAM. Giữ tab trong ca retry, không reload trước bước Retry.

## DM-P3-T01-C01 — rollback trước commit và mạng trở lại

AC-DM-06/08/21; TC-DM-05. Actor A, peer B; FE gửi một thao tác, BE replay cùng thao tác. Baseline0/0/0/counter0; nội dung chính xác **`RETRY-ROLLBACK-C01`**.

1. Login A, mở B theo bảng. F12 → Network, lọc `messages`, bật Preserve log. Chụp Snapshot B.
2. PowerShell bật fault outbox **trước commit**, chỉ cho cặp A/B run này:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action FaultOn -Run $run -PeerAlias B
   ```

3. FE nhập `RETRY-ROLLBACK-C01`, bấm **Gửi** đúng một lần. Network POST trả **503 `AUTHORITY_UNAVAILABLE`**. Dòng tin thành chưa xác nhận, có **Thử gửi lại**. Trong Network → Payload ghi `clientMessageId` (O_UI), giữ nguyên nội dung. Snapshot B vẫn0/0/0/counter0, activity/message cuối null.
4. DevTools Network chọn Offline, sau đó No throttling; chờ2 giây. Không bấm Retry. Vẫn một lần POST đã thử; DB vẫn0. Mạng online không tự gửi.
5. Tắt fault, rồi mới bấm **Thử gửi lại**; thử double-click nút. Chỉ một POST được gửi từ click kép, body/UUID như lần503. Response200, đúng một dòng **Đã gửi**, khung soạn cũ rỗng; DB1/1/1/counter1.

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action FaultOff -Run $run -PeerAlias B
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Snapshot -Run $run -PeerAlias B
   ```

6. BE đối soát, thay UUID duy nhất từ bước3:

   ```powershell
   $o = Read-Host 'UUID clientMessageId của POST503 ở C01'
   powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias B -ClientMessageId $o -Content 'RETRY-ROLLBACK-C01' -ExpectedStatus 200
   powershell -NoProfile -ExecutionPolicy Bypass -File $history -Action Page -Run $run -ActorAlias A -PeerAlias B -ReaderAlias B
   ```

   Cùng ID/sequence/version1 như FE; B GET thấy một tin; counts/counter giữ1, outbox không có body. Nếu lệnh lỗi giữa ca, vẫn chạy FaultOff trước tiếp tục; không cần API hoạt động để gỡ trigger.

Agent: PASS browser + API/PostgreSQL, lỗi503/rollback0, offline→online không replay, double-click chỉ mộtPOST, BE replay không delta. Evidence `p3retry/retry-c01.json`, `RetryTransportTests.Rollback_503...`. Người dùng FE/BE: **Chưa xác nhận**.

## DM-P3-T01-C02 — commit thành công nhưng mất phản hồi

AC-DM-08/21; TC-DM-06. Actor A, peer C. Baseline0/0/0/counter0; **`LOST-RESPONSE-C02`**. FE tạo thao tác; BE retry/song song chính UUID đó.

1. Mở `dm_demo_chi_p3user`; dán snippet vào Console. Bật đúng cặp:

   ```javascript
   window.dmRetryFixture.on({conversationId:'01a1265c-f623-775c-ab1d-35dba0f80985', mode:'drop'})
   ```

2. FE nhập `LOST-RESPONSE-C02` → Gửi. Network API thật trả200, nhưng UI chưa xác nhận và có Retry. Console:

   ```javascript
   const c02 = window.dmRetryFixture.last(); c02
   ```

   `status=200`, có `messageId`, `clientMessageId=O_UI`. Snapshot C **đã1/1/1/counter1**. Đây là mất phản hồi sau commit, khác lỗi rollback C01.
3. Không click gì, chờ2 giây: không có POST gửi tự động. Bấm **Thử gửi lại**, rồi BE gửi hai retry thực sự đồng thời:

   ```powershell
   $o = Read-Host 'clientMessageId từ c02'
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Concurrent -Run $run -PeerAlias C -ClientMessageId $o -Content 'LOST-RESPONSE-C02'
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Snapshot -Run $run -PeerAlias C
   ```

   Hai status200, hai ID bằng `c02.messageId`; `counts.messages=operations=createOutbox=1` của thao tác. Tổng pair vẫn1/1/1/counter1. UI đúng một dòng Đã gửi; double-click Retry không nhân đôi.
4. Mở browser/profile riêng, login **`dm_demo_chi_p3user`**, chọn An `dm_demo_an_p3user`; thấy tin cùng ID qua GET. Reload người nhận vẫn thấy một tin. Trên A tắt snippet rồi **Làm mới tin nhắn**: vẫn một dòng. Có thể bấm Retry lại ở cửa sổ A thứ hai chỉ khi cửa sổ đó còn chính failed operation; backend Concurrent phía trên đã kiểm chứng hai request đồng thời.

Agent: PASS lost response bằng fetch local sau API commit thật; PASS2 retry đồng thời và reader GET, một ID/operation/create-outbox. Backend còn dùng DelegatingHandler thật với host API/PostgreSQL để làm mất reply sau commit. Evidence `retry-c02.json`, `retry-concurrent-C.json`, test `Transport_loses_committed_response...`. Người dùng FE/BE: **Chưa xác nhận**.

## DM-P3-T01-C03 — giữ thao tác cũ, payload conflict, tin mới cùng nội dung

AC-DM-08; TC-DM-07. FE dùng A/S01; BE độc lập A/S10, mỗi pair baseline0. **`ORIGINAL-C03`** và **`ALTERED-C03`** khác nhau, giữ chính xác chữ hoa/gạch nối, không thêm khoảng trắng.

**FE từng bước:**

1. Mở S01, dán snippet, bật `drop` cho `01a1265c-f65c-7245-a957-4e9a4adee6b6`. Gửi `ORIGINAL-C03`. UI lỗi nhưng Snapshot S01 đã1. Ghi UUID O_FE từ marker; dòng lỗi vẫn ORIGINAL.
2. Bấm **Soạn tin mới**, nhập `ALTERED-C03`, bấm **Gửi tin mới**. Tin ALTERED được lưu bằng UUID mới; ORIGINAL vẫn có Retry. Counts2/2/2/counter2.
3. Nhập lại `ORIGINAL-C03` vào khung soạn nhưng **chưa bấm Gửi**. Bấm **Thử gửi lại** tại dòng ORIGINAL. POST giữ O_FE và ORIGINAL, response cùng ID bản đầu. Dòng lỗi được xác nhận; counts vẫn2. **Draft ORIGINAL đang soạn mới phải còn nguyên**, dù chữ giống thao tác cũ, vì revision draft đã khác.
4. Bấm **Gửi** của khung soạn. Đây là thao tác thứ ba, UUID mới; counts3/3/3/counter3. UI có hai tin ORIGINAL với ID/sequence khác, một tin ALTERED. Tắt snippet. Lượt mới/khác dùng UUID mới, không coi hai tin cố ý này là lỗi trùng.

**BE độc lập với FE, file payload đã tạo:**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -RequestFile "$payload\original.json" -ExpectedStatus 200
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -RequestFile "$payload\original.json" -ExpectedStatus 200
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -RequestFile "$payload\altered.json" -ExpectedStatus 409
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -RequestFile "$payload\new-same-body.json" -ExpectedStatus 200
powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Snapshot -Run $run -PeerAlias S10
```

Payload1 `{clientMessageId:'7c8e7c59-b35a-4d12-b22f-965b96ff4e44',content:'ORIGINAL-C03'}` →200; lặp→200 cùngID. Payload2 cùng O1/body ALTERED→**409 `OPERATION_CONFLICT`**, không đổi body/counts/counter/activity. Payload3 O2=`7c8e7c59-b35a-4d12-b22f-965b96ff4e45`, body ORIGINAL→200 ID khác. Lane BE cuối cùng2/2/2/counter2. Helper kiểm tra rejected request không thay Snapshot. File gốc UTF8, không sửa UUID file1 để “retry”.

Ca âm tính trên chính lane BE:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -Anonymous -Content 'NEGATIVE-P3' -ExpectedStatus 401
powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S10 -SenderAlias C -Content 'NEGATIVE-P3' -ExpectedStatus 404
```

Không auth→401; C ngoài A/S10→404; cả hai không thêm tin/operation/outbox/counter. Agent PASS FE revision/UUID và BE200/409/200 + negative401/404, evidence `retry-c03.json`, `retry-payloads/*.result.txt`, `retry-anonymous.txt`, `retry-outsider.txt`. Lane agent có dữ liệu các lượt E2E nên đối chiếu **delta**, lane người dùng S10 đang0. Người dùng FE/BE: **Chưa xác nhận**.

## DM-P3-T01-C04 — POST401 không replay; GET được refresh

AC-DM-21; TC-DM-23. Actor A/S02; baseline0/0/0/counter0. **`NO-AUTO-REPLAY-C04`**. Đây là401 giả lập để giữ session hợp lệ và failed operation trong RAM.

1. Mở S02, dán snippet. Console:

   ```javascript
   window.dmRetryFixture.on({conversationId:'01a1265c-f670-7f73-b522-3b138d5541ee', mode:'401'})
   ```

2. Nhập NO-AUTO-REPLAY-C04 → Gửi. UI báo401/có Retry; `const c04 = window.dmRetryFixture.last(); c04` có UUID,status401,messageIdnull. Fault chặn trước network nên backend chưa nhận POST này; Snapshot S02 vẫn0.
3. Không Retry. Bật fault cho **một GET**, rồi bấm **Làm mới tin nhắn**:

   ```javascript
   window.dmRetryFixture.on({conversationId:'01a1265c-f670-7f73-b522-3b138d5541ee', mode:'get401'})
   ```

   GET đầu bị401 giả lập → `/auth/refresh` thật200 → GET thật200. Tin lỗi vẫn ở UI; **không có POST /messages thật**, counts vẫn0. Chờ2 giây hoặc Offline→Online cũng không tạo POST. UUID cũ lấy từ biến c04, không lấy marker GET mới có UUIDnull.
4. Bấm **Thử gửi lại**. Chỉ lúc này có POST thật200 với `c04.clientMessageId` và NO-AUTO-REPLAY-C04; UI một dòng Đã gửi; Snapshot1/1/1/counter1. BE replay:

   ```powershell
   $o = Read-Host 'clientMessageId của c04, không dùng marker GET'
   powershell -NoProfile -ExecutionPolicy Bypass -File $send -Action Send -Run $run -PeerAlias S02 -ClientMessageId $o -Content 'NO-AUTO-REPLAY-C04' -ExpectedStatus 200
   ```

   ID không đổi, counts không tăng. Console `window.dmRetryFixture.off()`.

**Biến thể phiên bị thu hồi thật:** agent gọi `/auth/logout` thật cho phiên đang mở, giữ FE chưa logout; POST thật trả401, không DBdelta/replay. Bấm Làm mới→GET401→refresh bị từ chối→logout frontend; RAM draft/failed bị xóa. Login lại không tự gửi; nhập lại và Gửi là thao tác mới, UUID mới. Người dùng có thể dùng Edge A1 và Chrome A2 độc lập: A2 login cùng tài khoản, Cài đặt → Phiên đăng nhập → Thu hồi đúng phiên A1 (chỉ thao tác khi đã xác định ID/thời điểm phiên). Trên A1 gửi nội dung `ACTUAL-REVOKED-P3`, quan sát401; refresh rồi login lại, phải nhập và gửi mới. Không dùng biến thể này để đòi retry giữ RAM xuyên logout.

Agent PASS cả injected401/GET-refresh/manual retry và actual revoke/clearRAM/newUUID. Evidence `retry-c04.json`, `retry-actual-revoke.json`; backend guard revoked trong72 tests. Người dùng FE/BE: **Chưa xác nhận**.

## Ca bổ sung DELAY, CURRENT, MERGE và LOGOUT

### DELAY — timeout15 giây và phản hồi cũ

1. Login A, mở S03, dán snippet; `window.dmRetryFixture.on({conversationId:'01a1265c-f684-7ebd-afc4-048ba3e8fa96',mode:'delay',delayMs:60000})`. Gửi **`DELAYED-P3`**.
2. Chờ15 giây thật. UI “Quá15giây…” + Retry; marker status200/messageId và Snapshot S03 đã1. Không tự gửi lần2. Ghi `$mid` và `$o` bằng Read-Host từ marker.
3. Fixture kỹ thuật **chỉ DB thử/message thuộc actor/run**, mô phỏng current edit để kiểm tra retry; endpoint edit/delete sản phẩm thuộc P5:

   ```powershell
   $mid = Read-Host 'messageId của DELAY'
   $o = Read-Host 'clientMessageId của DELAY'
   powershell -NoProfile -ExecutionPolicy Bypass -File $retry -Action Current -Run $run -PeerAlias S03 -MessageId $mid -State Edited
   ```

4. FE nhập draft **`MY-NEXT-DRAFT-P3`**, bấm Retry tại tin DELAY. Response200 cùngID/version`2`/content`CURRENT-EDITED-P3`; UI một dòng hiện hành, draft MY-NEXT-DRAFT còn nguyên. Console `window.dmRetryFixture.release()` giải phóng responsev1 đầu; UI vẫn version2/current, draft giữ nguyên.
5. BE replay original body DELAYED-P3 bằng `$o` → cùng ID/currentv2; Snapshot1/1/1/counter1. Tắt snippet. Nếu giữ60giây tự hết trước bước4, proof late reply vẫn được đơn vị kiểm chứng; để lặp theo đúng barrier hãy dùng run mới và thao tác trong60giây.

### CURRENT — retry không khôi phục nội dung đã sửa/xóa (TC-DM-08)

Mở S04, baseline0. Dán snippet, bật `drop` cho `01a1265c-f699-71f1-8c01-64def5b2d16b`; gửi **`CURRENT-Edited-ORIGINAL-P3`**. Sau UI lỗi và marker có ID, chạy Current với peerS04, ID đó, StateEdited. Retry original UUID/body→200 cùngID/version2/`CURRENT-EDITED-P3`, UI một dòng hiện hành. Snapshot1.

Làm lần2 bằng `drop` và **`CURRENT-Deleted-ORIGINAL-P3`**, UUID mới. Current `-State Deleted` với ID mới, rồi Retry→200 cùngID/version2/contentnull/deletedAtkhácnull, UI **Tin nhắn đã bị xóa.** Snapshot cuối2/2/2/counter2. BE Send replay từng UUID với đúng original body, không gửi body “CURRENT-EDITED-P3”. Không có bản body cũ trong outbox/message_edits. Technical fixture không tạo event sửa/xóa; create outbox/operation/counter không tăng. Agent PASS hai biến thể trong `retry-current.json` và backend current/peer-disabled test. Người dùng: **Chưa xác nhận**.

### MERGE — GET hiện hành đến trước POST cũ (lát cắt TC-DM-23)

Dùng A/S11 riêng, baseline0. Bật delay60giây cho `01a1265c-f6bd-771a-825a-ff96722802cc`, gửi **`GET-BEFORE-POST-P3`**. Ngay khi marker có messageId (API200/commit), chạy CurrentEdited cho peerS11/ID này, rồi bấm Làm mới tin nhắn trước khi giải phóng response. GET thật trảversion2, UI một dòng `CURRENT-EDITED-P3`. Console release responsePOSTv1 → vẫn một dòng/currentv2; Snapshot1. Tắt snippet. Agent PASS `retry-get-before-post.json`.

**Hub event trước response và reconnect/catchup/resume nhiều trang chưa có runtime trong P3-T01**, bị chặn bởi P4. GET proof không được ghi là Hub proof hoặc hoàn tất toàn AC-DM-21/TC-DM-23. Shared merge đơn vị đã kiểm chứng ID/actor/UUID/version; test Hub thực tế phải bổ sung ở phase phụ thuộc.

**Chiều ngược GET cũ đến sau retry mới:** trên S11 gửi `GET-OLD-AFTER-RETRY-P3` với mode drop, lưu marker vào biến riêng `const older = window.dmRetryFixture.last()`. Bật `getdelay` cùng conversation, delayMs60000; bấm Làm mới và chờ marker GET status200. CurrentEdited với messageId từ `older`/peerS11; bấm Retry tại dòng lỗi → currentv2. Console release GETv1 → UI vẫn currentv2, đúng một dòng. Pair tăng thêm1; không tính tin của biến thể trước là duplicate. Evidence `retry-get-after-retry.json`. Loader latest giữ version cao hơn của các ID nằm trong trang trả về, vẫn bỏ trang cũ nằm ngoài latest50; proof H121 được chạy lại sau thay đổi này.

### LOGOUT — xóa RAM, chặn reply cũ và tách actor

Agent bật delay sau commit, logout A bằng UI, giải phóng reply cũ, login C và mở B (cặp C/B riêng): không thấy tin/draft của A; localStorage/sessionStorage không có body/draft. Người dùng dùng run mới hoặc một pair trống khác, gửi `LOGOUT-PRIVATE-P3` với delay rồi Cài đặt → Đăng xuất. Login `dm_demo_chi_p3user`, mở B; không có pending/draft/privatebody của An. Tin A đã commit vẫn được đọc trong pair của A khi A login lại. `retry-logout.json`; người dùng **Chưa xác nhận**.

## SQL chỉ đọc thực tế

Snapshot chạy query này (thay ID bằng pair tương ứng từ manifest, không dùng GUID minh họa khác):

```sql
SELECT s.id, s.last_message_sequence::text, s.last_message_id, s.last_activity_at,
 (SELECT count(*) FROM messaging.messages WHERE space_id=s.id) AS messages,
 (SELECT count(*) FROM messaging.send_operations WHERE space_id=s.id) AS operations,
 (SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id) AS outbox,
 (SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id
   AND (payload ? 'content' OR payload ? 'body')) AS outbox_with_body
FROM messaging.spaces s WHERE s.id='01a1265c-f60c-7762-bc73-0573f970a22c';
SELECT id, author_user_id, client_message_id, conversation_sequence::text,
 version::text, edited_at, deleted_at
FROM messaging.messages WHERE space_id='01a1265c-f60c-7762-bc73-0573f970a22c'
ORDER BY conversation_sequence;
```

Để chạy SQL file chỉ đọc bằng helper chính, lưu dưới `.dm-acceptance` rồi truyền `dm-acceptance.ps1 -Action Db -SqlFile <đường dẫn>`; helper mở transaction READ ONLY. Không đưa body/token/key vào ảnh/HAR/report. Với C01/C02/C04 kỳ vọng tăng1; FE-C03 tăng3; BE-C03 tăng2; retry/conflict/401/404 tăng0. Mỗi operation chỉ một create outbox metadata, không có content. Snapshot trước/sau và ID cụ thể là proof, không dùng tổng toàn DB đang có nhiều fixture.

## Kết quả agent và người dùng

| Ca / AC / TC | Thao tác và proof | Kết quả agent | Người dùng FE / BE |
|---|---|---|---|
| C01 /06,08,21 /05 | SQL58000 trước outbox→API503, rollback0, offline→online, double-click retry, DBdelta1 | PASS | Chưa xác nhận / Chưa xác nhận |
| C02 /08,21 /06 | API200/commit thật rồi fetch mất reply; concurrent2retry cùngUUID/ID; reader GET | PASS | Chưa xác nhận / Chưa xác nhận |
| C03 /08 /07 | Draft mới độc lập, retry original, revision giữ draft cùngchữ, O1/bodykhác409, O2/bodygiống200, negative401/404 | PASS | Chưa xác nhận / Chưa xác nhận |
| C04 /21 /23 | Injected POST401 + real GET refresh; manualclick cùngkey/body; actual session revoke401/clearRAM | PASS | Chưa xác nhận / Chưa xác nhận |
| DELAY /08,21 /23 | Deadline15giây thật, retrycurrentv2, replyv1muộn không ghi đè/draft | PASS | Chưa xác nhận / Chưa xác nhận |
| CURRENT /08 /08 | Technical owned edit/tombstone, replay original trảcurrent, không resurrect | PASS | Chưa xác nhận / Chưa xác nhận |
| MERGE /21 /23 | GETcurrentv2 trước POSTv1; một dòng newestversion | PASS lát cắt GET | Chưa xác nhận / Chưa xác nhận |
| LOGOUT | UI logout→actor C, late reply bỏ, body/draft không storage | PASS | Chưa xác nhận / Chưa xác nhận |
| Backend |72/72, PostgreSQL thật;2 test transport mới +70regression | PASS | Chưa xác nhận |
| Client / build |53/53 Node; Vite build; API preflight abort trước/sau refresh; latest GET không regress version | PASS | Chưa xác nhận |
| Browser |10/10 ca Edge với FE/API/DB, 2,4 phút; report `.dm-acceptance/e2e-p3-t01/results.json` | PASS | Chưa xác nhận |
| Regression P2 |3/3 Edge: H12150/100/121 + scroll anchor, B đọc hai tin An/reload, delayed GET giữ send mới; 8,2 giây | PASS | P2 đã nghiệm thu; P3 chưa xác nhận |
| Hub / reconnect full | Cần runtime P4 | Bị chặn bởi task phụ thuộc | Chưa áp dụng ở task này |

Lệnh chạy lại tự động (agent run riêng; không chạy trên p3user đang nghiệm thu):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action BackendTests
Set-Location .\clients\WebClient
npm.cmd test
npm.cmd run build
npx.cmd playwright test --config playwright.dm-p3-t01.config.js
```

Backend fixture có phép đếm toàn DB trong vài regression; chạy backend trước E2E/setup/send, tránh chạy hai bộ mutation đồng thời. E2E dùng UUID mới mỗi lượt, so delta từ Snapshot, không reset volume. Fault beforeoutbox được gỡ trong finally; snippet off sau từng browsercase. Bằng chứng không chứa token/key; technical-current trên đúng testDB/actor/pair/message sở hữu.

## Lỗi và kiểm tra lại

Test backend mới ban đầu dùng Assert.Equal trên hai JsonElement ID nên báo khác dù UUID giống nhau; đã sửa so GetGuid, chạy lại72/72PASS. Một lượt E2E LOGOUT đo delta trong lúc agent chạy payload BE cùng laneS10 nên thấy tăng2 thay vì1; đã dừng mutation ngoài bộ test và chạy lại đầy đủ. Khi rà race đã bổ sung giữ newer-version qua latest GET cũ, kiểm chứng riêng bằng unit/browser và regression P2. Không còn lỗi sản phẩm đã biết trong phạm vi retry này; gate người dùng và Hub phase vẫn mở.

## Git bàn giao và quyền bước tiếp

- Code/test/helper commit **`ab93864e8a9f1d93ee95af6ad582c0bac06c425e`**, đã push; `git ls-remote --heads origin refs/heads/feat/dm-p3-t01-manual-retry` trả đúng SHA này ở lần push code. Commit hồ sơ tiếp theo chỉ cập nhật docs; source FE/API/schema và asset không đổi. Bằng chứng remote cuối bằng HEAD bàn giao tại `.dm-acceptance/runs/p3user/retry-handover-git.json`; bản SHA cuối được ghi trong phản hồi bàn giao và có thể đối chiếu lại bằng lệnh dưới đây.
- [Draft PR29](https://github.com/Chyeonma/scdc/pull/29) có base `message`, trạng thái Chờ người dùng test. `message` remote vẫn **`b4d814d73541afc7f13981115155dd27cec1949e`**, đã có P2-T02, chưa có P3.
- Runtime smoke sau Stop/Start/Restart: search qua FE200, asset`index-pm6lhpkq.js`,8 pair p3user vẫn0,0 fault trigger; `retry-handover-smoke.json`. H121/bigint của p2huser vẫn khớp sau restart; `.dm-acceptance/runs/p2huser/p3-restart-smoke.json`. Init đã chạy với thông báo theo checkout.
- Người dùng P3-T01 PASS/FAIL: **Chưa có**. Không dùng kết quả agent điền PASS cho người dùng.
- P3 chưa merge `message`. Chỉ merge `--no-ff` sau PASS cả FE/BE trên build này, fetch/xác minh đúng SHA và smoke; không merge vào main/phát hành.
- Không thực hiện DM-P3-T02 hoặc P4 trong task này. Nội dung phản hồi nghiệm thu nên gồm build, case FE/BE PASS/FAIL, bước lỗi và UUID/ID đã lọc secret.
- Kết luận: **Chờ người dùng test DM-P3-T01**.

## Người dùng nghiệm thu — 10/10/2026

Người dùng xác nhận: “P3-T01 đã PASS FE/BE; nghiệm thu và làm P3-T02.” Build nghiệm thu `156cb32275feecc2b24a6d7bc0199056d5b821af`, code `ab93864`, FE asset `index-pm6lhpkq.js`. Các trạng thái Chờ/Chưa xác nhận phía trên là mốc bàn giao và được thay thế bởi xác nhận này. Được phép tích hợp riêng P3-T01 vào `message`, thực hiện duy nhất P3-T02 rồi dừng để người dùng test.

```powershell
git -c safe.directory=E:/Project/SCDC/dm-message-integration rev-parse HEAD
git -c safe.directory=E:/Project/SCDC/dm-message-integration ls-remote --heads origin refs/heads/feat/dm-p3-t01-manual-retry refs/heads/message
```

Chạy tại integration: SHA dòng nhánh task phải trùng HEAD, code commit ở trên phải là ancestor. Nhánh task làm trong worktree integration để giữ các thay đổi đang có ở checkout scdc và outer main; `git branch -a` tại repository khác chỉ hiện remote sau fetch, không tự đổi checkout đang chạy.

# DM-P2-T01 — Gửi văn bản và validation

Trạng thái: **Chờ người dùng test FE và BE**. Kết quả tự động bên dưới là kết quả agent, chưa thay nghiệm thu của người dùng.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / branch | DM-P2-T01 / `feat/dm-p2-t01-persist-text-message` |
| Base đã nghiệm thu | `origin/message` tại `6e9ea045aaea5b87734265f0cbbb050c01842203`; P1-T03 PASS FE/BE trong phiên |
| Chỉ đạo đi tiếp | “Làm P2-T01 trước, tiếp tục nghiệm thu FE/BE từng task như đã thống nhất.” |
| Commit triển khai / bàn giao | `3aa251b471b7c9e236b49ea67ec3920b78399b46` (code đã test); commit biên bản chỉ thay docs; metadata runtime ở `.dm-acceptance/runs/p2proof/p2-t01-build.json` |
| Worktree chạy | `E:\Project\SCDC\dm-message-integration` |
| FE / BE | <http://localhost:15300> / <http://localhost:15026/swagger> |
| Database / Compose | `scdc_dm_acceptance_test` / `scdc-dm-acceptance`, PostgreSQL `localhost:15432` |
| Migration | `database/postgres/migrations/20261009_dm_p2_text_message.sql`; additive và chạy lặp được |
| Git-flow | Push task; chưa merge `message`. Sau người dùng PASS FE/BE mới merge `--no-ff` và smoke. P2-T02 và P4 chưa thực hiện |

Đọc cùng [kế hoạch](../../plans/direct-messaging.md#dm-p2-t01-gửi-văn-bản-và-validation), [prompt nguyên task](../../plans/direct-messaging-prompts.md#dm-p2-t01), [AC/TC](../../features/direct-messaging/specs/acceptance.md) và [contract](../../contracts/direct-messaging.openapi.json).

## Thay đổi và giới hạn

POST `/api/v1/direct-conversations/{id}/messages` nhận `clientMessageId` UUIDv4 và `content`. Identity guard khóa user/session trước khóa space; kiểm tra actor, phiên và đúng hai thành viên. Send mới cần peer đủ điều kiện. Sequence tăng dưới khóa space và được trả dạng chuỗi decimal. Message, SendOperation, outbox và `last_message_id/last_message_sequence/last_activity_at` cùng transaction. Outbox chỉ chứa ID/context/version; chưa có dispatcher hoặc Hub.

Body đổi CRLF→LF, CR→LF, không trim/NFC. Client/server dùng Unicode17 pinned trong `text-policy.json`; tối đa 2.000 UTF-16 sau chuẩn hóa, từ chối surrogate lỗi/NUL và chuỗi chỉ gồm ký tự trống/vô hình/control. HMAC-SHA256 dùng keyring riêng, UUID network bytes, phiên bản1. Retry dùng key gốc, so fingerprint gốc rồi trả DTO hiện hành kể cả đã sửa/xóa; thiếu key503, legacy không có fingerprint409, đổi body với cùng UUID409. Không hash nội dung đã sửa để tái dựng operation cũ.

FE có textarea, bộ đếm, Enter desktop/Shift+Enter/IME, trạng thái Đang gửi/Đã gửi/lỗi và nút Thử gửi lại giữ UUID/body. Không tự replay POST sau 401, mạng lỗi hoặc làm mới inbox. Tin tạm được thay bằng DTO theo `clientMessageId`; draft/rows chỉ trong RAM theo actor/conversation, được dọn khi logout/reload. Sau lỗi chưa xác nhận, composer giữ và khóa nội dung để retry cùng thao tác. Chọn người vẫn chỉ một người.

**Reload giữ inbox và người vừa nhắn từ DB. Tải nội dung tin cũ sau reload hoặc ở máy người nhận thuộc P2-T02; realtime thuộc P4.** P2-T01 hiển thị phản hồi gửi của tab hiện tại, thông báo lịch sử chưa tải khi counter khác0. Không dùng việc chưa có GET history để đánh trượt quyền đọc của endpoint chưa triển khai.

## Dữ liệu bàn giao và cách mở bản chạy

Mật khẩu local tất cả tài khoản mẫu: `DmDemo2026!Local`. Token/key chỉ ở runtime ignored hoặc RAM, không đưa vào biên bản/Git/HAR.

| Run / lane | A | B | C | Pair dùng |
|---|---|---|---|---|
| `baseline` — test FE gửi | `dm_demo_an` | `dm_demo_bao` | `dm_demo_chi` | A/B: 0 tin lúc bàn giao; A có25 DM từ P1 |
| `baseline` — test BE độc lập | `dm_demo_an` | `dm_demo_search01` | `dm_demo_chi` | A/S01: 0 tin; tách với FE A/B |
| `p2manual` — người vừa nhắn | `dm_demo_an_p2manual` | `dm_demo_bao_p2manual` | `dm_demo_chi_p2manual` | A/B và A/C mới, 0 tin lúc bàn giao |
| Agent E2E cuối | `p2final` và `p2recent3` | Alias từ manifest | Alias từ manifest | Giữ bằng chứng và dữ liệu; không dùng làm baseline rỗng để lặp test |

U=`dm_demo_pending` chưa xác minh, login403 `Identity.EmailNotVerified`; K=`dm_demo_khoa` active/verified. S01–S23=`dm_demo_search01`…`dm_demo_search23`. B/C cùng displayName Bảo Demo, phân biệt bằng `@username`.

Mở PowerShell, copy nguyên khối:

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run baseline -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run baseline -PeerAlias S01
```

Docker Desktop phải ở trạng thái Running với Linux engine. Chạy đúng worktree trên; clone `scdc` còn ở baseline trước P2. Khi cần build lại/migration trên checkout task, dùng `send-text.ps1 -Action Upgrade`; không chạy `schema.sql` trên volume hiện có.

Snapshot trả conversationId/messageId/clientMessageId/sequence/counter/activity/counts/outboxPayloads thật, không hardcode ID. Các helper tự login đúng actor, không in token và logout sau thao tác. Payload lane có UUID riêng từng file; chạy lại cùng file là replay. Chọn lane mới cho một bộ send độc lập, không ghi đè lane cũ.

Tạo bộ file raw JSON cho BE (32 file; nếu lane đã có, dùng `be2` và đổi đường dẫn tương ứng):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Payloads -Run baseline -Lane be
```

File ở `.dm-acceptance\runs\baseline\payloads-be\`: `m01/m02/m03/m04.json`, `max-ascii.json`, `too-long-ascii.json`, `max-emoji.json`, `too-long-emoji.json` và toàn bộ [26 corpus](../../fixtures/text-validation.json). Lone surrogate được giữ dưới dạng escape `\uD800` trong file, không đổi sang U+FFFD.

## DM-P2-T01-C01 — Gửi và chỉ báo sent sau phản hồi thành công

AC-DM-02/21; TC-DM-03/23; DM-SQL-02–06. FE A/B, BE chỉ replay thao tác FE. Baseline snapshot N tin/operation/outbox và counterN (lúc bàn giao0).

1. Login FE bằng `dm_demo_an`. Bấm `+`, nhập `Bảo`, chọn dòng `@dm_demo_bao`, bấm Mở hội thoại. Có khung “Nội dung tin nhắn”.
2. Mở DevTools Network, có thể chọn Slow3G để quan sát pending. Nhập chính xác `M01: Chào Bảo, mình là An.` rồi bấm Gửi một lần. Trước phản hồi thấy “Đang gửi…” và nút Gửi bị khóa; sau200 thấy một dòng “Đã gửi”, textarea rỗng.
3. Trong request POST `/messages`, lấy nguyên `clientMessageId` và body. Response có `id`, `conversationId`, `author.id=A`, `sequence="N+1"`, `version="1"`, content nguyên văn, editedAt/deletedAt null. Không yêu cầu B nhận realtime ở task này.
4. Đối chiếu Snapshot A/B: message/operation/outbox tăng đúng1, counterN+1, activity khác null, outboxContainsBody0. Chỉ payload `messageId/conversationId/version`; không có body.
5. Replay BE cùng actor/UUID/body từ FE, ví dụ thay UUID bằng giá trị thực vừa lấy:

```powershell
$operation = [guid](Read-Host 'Dán clientMessageId từ POST của FE')
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -ClientMessageId $operation -Content 'M01: Chào Bảo, mình là An.'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -ClientMessageId $operation -Content 'M01: Chào Bảo, mình là An.'
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -ClientMessageId $operation -Content 'M01: nội dung khác' -ExpectedStatus 409
```

Hai replay200 trả cùng messageId/sequence và không tăng counts/activity. Body khác409 `OPERATION_CONFLICT`, không ghi thêm. UUIDv4 mới cùng body là send mới và được lưu thành tin riêng.

Agent: test browser giữ phản hồi HTTP thật sau khi backend commit để quan sát pending, replay cùng UUID, DB1/1/1. Người dùng: **Chưa xác nhận**.

## DM-P2-T01-C02 — Biên UTF-16, CRLF và khoảng trắng

AC-DM-11; TC-TEXT-02/03/04, TC-DM-21. FE tiếp tục A/B; BE A/S01 độc lập. Mỗi request mới dùng UUIDv4 mới; replay cùng file không tăng tin.

| Input | Mong đợi |
|---|---|
| `a` ×2.000 |200, length2.000, tăng1/1/1 |
| `a` ×2.001 |400 `CONTENT_TOO_LONG`, counts/counter/activity không đổi |
| `😀` ×1.000 |200, 2.000 UTF-16 |
| `😀` ×1.001 |400 `CONTENT_TOO_LONG`, 2.002 UTF-16 |
| `a\r\nb` |200 content `a\nb`, length3 |
| `a\rb` |200 content `a\nb`, length3 |
| `a` ×1.998 +CRLF+`b` |raw2.001, normalized2.000,200 |
| `  M03: giữ khoảng trắng  ` |200, giữ chính xác hai dấu cách đầu/cuối |
| `e` +U+0301 và `é` |Giữ hai chuỗi khác nhau, không NFC |

FE: copy content từ file JSON tương ứng, nhập vào textarea; 2.001/2.002 bị chặn trước POST, nội dung được giữ và có thông báo lỗi. Có thể tạo chuỗi clipboard bằng `('a' * 2000) | Set-Clipboard`; emoji lấy từ file UTF-8 để không nhầm độ dài. Dùng Shift+Enter xuống dòng; Enter desktop gửi, IME composition chưa gửi.

BE copy nguyên khối:

```powershell
$payloads = '.\.dm-acceptance\runs\baseline\payloads-be'
foreach ($case in 'max-ascii','max-emoji','newlines','crlf','cr','crlf-max','m03','vietnamese-combining','vietnamese-precomposed') {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias S01 -RequestFile "$payloads\$case.json"
  if ($LASTEXITCODE -ne 0) { throw "FAIL $case" }
}
foreach ($case in 'too-long-ascii','too-long-emoji') {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias S01 -RequestFile "$payloads\$case.json" -ExpectedStatus 400
  if ($LASTEXITCODE -ne 0) { throw "FAIL $case" }
}
```

BE lane tổng9 send mới hợp lệ,2 từ chối: delta message/operation/outbox/counter =9 nếu lane mới và pair chưa có các UUID đó. Count chỉ scope A/S01. Agent: toàn bộ26 corpus qua REST/PostgreSQL,26 client corpus cùng tab/newline-only, browser biên và chuẩn hóa. Người dùng: **Chưa xác nhận**.

## DM-P2-T01-C03 — Trống, vô hình, Unicode sai, HTML

AC-DM-11/19; TC-TEXT-01/05, TC-DM-21. FE A/B; BE A/S01. So snapshot trước/sau từng request lỗi.

| File/input | HTTP / errorCode |
|---|---|
| `empty`, `spaces`, `newline-only`, `tab-only`, `nbsp-only`, `zero-width-only`, `variation-selector-only`, `whitespace-invisible`, `hangul-filler-only`, `control-only` |400 `CONTENT_EMPTY` |
| `unpaired-high`, `unpaired-low`, `high-then-letter`, `nul-in-text` |400 `CONTENT_INVALID` |
| `invisible-with-letter` |200, giữ ký tự vô hình cùng chữ |
| `m04` HTML/script |200, render literal text; không tạo img/script, không chạy mã |

FE thử empty/space/tab/zero-width bằng content của file. Nút Gửi hiển thị lỗi và không POST, draft còn nguyên. Nhập `m04` từ file JSON: chữ `<img...><script...>` phải hiện nguyên văn; DevTools `window.dmXss` vẫn undefined. Lone surrogate/NUL dùng file raw qua BE vì textarea/trình duyệt có thể tự thay đổi các ký tự này.

```powershell
$payloads = '.\.dm-acceptance\runs\baseline\payloads-be'
foreach ($case in 'empty','spaces','newline-only','tab-only','nbsp-only','zero-width-only','variation-selector-only','whitespace-invisible','hangul-filler-only','control-only','unpaired-high','unpaired-low','high-then-letter','nul-in-text') {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias S01 -RequestFile "$payloads\$case.json" -ExpectedStatus 400
  if ($LASTEXITCODE -ne 0) { throw "FAIL $case" }
}
foreach ($case in 'invisible-with-letter','m04') {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias S01 -RequestFile "$payloads\$case.json"
  if ($LASTEXITCODE -ne 0) { throw "FAIL $case" }
}
```

C03 BE delta2 send mới; kết hợp C02 delta11. Không coi số invalid HTTP là số mutation. Agent: raw surrogate thực400 CONTENT_INVALID, DB không đổi; HTML browser literal. Người dùng: **Chưa xác nhận**.

## DM-P2-T01-C04 — Outsider và lỗi trước commit

AC-DM-09/12/21; TC-DM-14/23; DM-SQL-03–06. C/no token gửi A/B phải lần lượt404 RESOURCE_NOT_FOUND /401 Common.Unauthorized, không lộ tin/participant. U login bị từ chối403; integration còn thử token của actor bị mất verification/disabled/revoke để chứng minh401 tại send.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -SenderAlias C -Content 'C04: outsider' -ExpectedStatus 404
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -Anonymous -Content 'C04: anonymous' -ExpectedStatus 401
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run baseline -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action FaultOn -Run baseline -PeerAlias B
```

Fault này là trigger test **trước INSERT outbox, trước commit**, chỉ pair baseline A/B và đúng DB nghiệm thu. Đã insert message ở transaction nhưng rollback phải xóa cả message/operation, trả counter/activity/projection về baseline; không có điều khiển fault trong sản phẩm.

FE đang login A: nhập `C04: rollback giữ bản nháp`, bấm Gửi. Network503 AUTHORITY_UNAVAILABLE; row lỗi, textarea giữ nội dung, chưa Đã gửi. Bấm làm mới hội thoại không tự gửi lại. Snapshot vẫn đúng N/operation/outbox/counter/activity trước fault.

```powershell
try {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run baseline -PeerAlias B -Content 'C04: BE rollback' -ExpectedStatus 503
  if ($LASTEXITCODE -ne 0) { throw 'Fault probe failed' }
} finally {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action FaultOff
}
```

Sau FaultOff, bấm Thử gửi lại cho row FE lỗi:200, cùng UUID/body ban đầu, chỉ một row sent và delta1/1/1. Luôn FaultOff kể cả test fail; helper FaultOff không phụ thuộc API còn hoạt động. Agent: ca browser/API/PostgreSQL thực, lỗi không tăng dữ liệu, retry đúng operation. Người dùng: **Chưa xác nhận**.

## DM-RECENT-01 — Người vừa nhắn bằng writer thật

Run bàn giao `p2manual`, pair A/B và A/C0tin. Không dùng baseline đã gửi C01; không xóa tin của run agent. Password giống baseline, username có hậu tố `_p2manual`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Open -Run p2manual -ActorAlias A -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Open -Run p2manual -ActorAlias A -PeerAlias C
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run p2manual -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Snapshot -Run p2manual -PeerAlias C
```

1. Login A=`dm_demo_an_p2manual`. Mở dấu `+`: Người vừa nhắn tin rỗng. Mở B/C mà chưa gửi vẫn không thêm người vào đây.
2. A mở B=`dm_demo_bao_p2manual`, gửi `RECENT01: chào Bảo`, chờ200/sent. Mở lại `+`: chỉ B; A/B count1, activity khác null; A/C0, activity null.
3. A mở C=`dm_demo_chi_p2manual`, gửi `RECENT02: chào Chi`. Mở lại `+`: C trước B. Hai tên Bảo Demo nhưng hai username/ID khác nhau.
4. Browser/profile độc lập login B, mở A, gửi `RECENT03: trả lời An`. A bấm làm mới inbox hoặc reload rồi mở `+`: B trước C. B chỉ thấy A từ pair của mình; C chỉ thấy A từ pair A/C.
5. Chọn B rồi tìm đúng username B: không nhân đôi. Chọn C thay B, chỉ1chip. Bỏ C thì không còn selection; không tạo group DM.
6. Chụp Snapshot: A/B2tin, A/C1tin, operation/outbox tương ứng2 và1. Tổng delta3. Đối chiếu GET inbox A và timestamps thật; backend ở ca này chỉ đọc để không thêm mutation.
7. Fault A/C rồi BE gửi lỗi, kiểm tra thứ tự vẫn B,C và counts không đổi:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action FaultOn -Run p2manual -PeerAlias C
try {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Send -Run p2manual -PeerAlias C -Content 'RECENT04: rollback' -ExpectedStatus 503
  if ($LASTEXITCODE -ne 0) { throw 'Recent fault probe failed' }
} finally {
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action FaultOff
}
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\inbox.ps1 -Action Page -Run p2manual -ActorAlias A
```

Agent proof trên run `p2recent3`: dữ liệu gửi thật A→B/A→C/B→A, đọc modal/inbox sau mỗi commit, reload và fault503 không đổi order; evidence `.dm-acceptance/e2e-p2-t01/recent.json`. Agent PASS trên suite cuối 6/6. Người dùng: **Chưa xác nhận**.

## SQL đọc và bằng chứng bổ sung

Các Snapshot đã thực thi đọc qua transaction READ ONLY; không in body hoặc key. Với DBeaver/psql có thể resolve pair từ username như sau:

```sql
BEGIN READ ONLY;
SELECT d.space_id AS conversation_id,s.last_message_id,s.last_message_sequence::text,
       s.last_activity_at,
       (SELECT count(*) FROM messaging.messages m WHERE m.space_id=d.space_id) AS messages,
       (SELECT count(*) FROM messaging.send_operations o WHERE o.space_id=d.space_id) AS operations,
       (SELECT count(*) FROM integration.outbox_events e WHERE e.space_id=d.space_id) AS outbox
FROM messaging.direct_conversations d JOIN messaging.spaces s ON s.id=d.space_id
WHERE d.user_low_id=least((SELECT id FROM identity.users WHERE username='dm_demo_an'),
                         (SELECT id FROM identity.users WHERE username='dm_demo_bao'))
  AND d.user_high_id=greatest((SELECT id FROM identity.users WHERE username='dm_demo_an'),
                            (SELECT id FROM identity.users WHERE username='dm_demo_bao'));
SELECT e.aggregate_id,e.aggregate_version,e.payload
FROM integration.outbox_events e JOIN messaging.spaces s ON s.id=e.space_id
WHERE s.created_by_user_id=(SELECT id FROM identity.users WHERE username='dm_demo_an');
COMMIT;
```

Đổi username đúng run/peer cần đọc; không dùng global count để kết luận delta. Đối chiếu UTF-16 từ response content/.NET/JS, không dùng PostgreSQL `char_length` như UTF-16.

| Bằng chứng | Kết quả agent | Người dùng |
|---|---|---|
| Backend PostgreSQL thật |62/62 PASS; 26 corpus trong một test REST,6 HMAC vectors,40 cùng UUID,20 hai chiều,held-commit lock chain,rollback,authorization,current edited/deleted retry,legacy409,key missing/rotation,bigint exact |Chưa xác nhận |
| Node client/API |42/42 PASS;26 corpus và2blank whitespace probes,mutation401/503 không replay và đổi actor khi preflight |Chưa xác nhận |
| Build FE/BE |Release .NET và Vite/Docker thành công |Chưa xác nhận |
| Migration fresh/upgrade/replay |PASS trên2DB test mới riêng, đã dọn;2legacy seq1,2, counter42 giữ nguyên,2operation không fingerprint,0body cũ/0outbox; tombstoneNULL hợp lệ |Chưa xác nhận |
| Browser |6/6 PASS trên Edge/Chromium: C01, C02/C03, C04, RECENT01, keyboard/draft/logout và touch viewport emulation |Chưa xác nhận |
| Thiết bị/IME thật |Browser IME event và touch viewport emulation; chưa thay test bàn phím/thiết bị thật của người dùng |Chưa xác nhận |

Lệnh chạy lại backend/migration/client:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance.ps1 -Action BackendTests
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\test-text-migration.ps1
Push-Location .\clients\WebClient
try { npm.cmd test } finally { Pop-Location }
```

Để chạy lại E2E độc lập, tạo run mới, không dùng run chứa tin của lượt trước:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Setup -Run p2send4
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\dm-acceptance\send-text.ps1 -Action Setup -Run p2recent4
$env:DM_P2_SEND_RUN='p2send4'
$env:DM_P2_RECENT_RUN='p2recent4'
Push-Location .\clients\WebClient
try { npx.cmd playwright test --config playwright.dm-p2-t01.config.js } finally { Pop-Location }
```

Trạng thái nghiệm thu: người dùng **Chưa xác nhận** C01/C02/C03/C04/RECENT FE và BE trên build P2. Khi có PASS FE/BE mới cập nhật biên bản, merge task vào `message`, chạy smoke và chờ giao task kế. Không suy PASS từ câu hỏi hoặc im lặng.

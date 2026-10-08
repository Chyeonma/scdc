# DM-P1-T02 — tạo hoặc lấy một DM duy nhất

Trạng thái: **Chờ người dùng test FE và BE**, ngày09/10/2026. P1-T01 đã được người dùng PASS và tích hợp vào `message`; P1-T02 chỉ được push nhánh task, chưa merge. Phản hồi mới: “Trước tiên chức năng chọn nhiều người sẽ chưa hoạt động, Chỉ chọn 1 người hoạt động, và tạo DM”. Chọn người thứ hai thay người thứ nhất; chỉ bấm **Mở hội thoại** mới gửi POST.

## Task và bản chạy

| Trường | Giá trị |
|---|---|
| Task / truy vết | P1-T02.1–.5; AC-DM-01/12/13, TC-DM-02/12/24, DM-SQL-01 |
| Branch / base | `feat/dm-p1-t02-open-conversation` / `origin/message` tại `9f5afbc0b032bfef2407bb0e1305b756eeb30725` |
| Repository / worktree | Repo gốc `E:\Project\SCDC\scdc`; thực thi ở `E:\Project\SCDC\dm-message-integration` |
| Commit implementation/build | `e53f4676147859cc1c576d0e1cd5192df5cd2806`; push và `git ls-remote` đã xác minh cùng SHA |
| Commit bàn giao / remote cuối | Commit docs tiếp theo chỉ ghi hồ sơ này; `git rev-parse HEAD` và `.dm-acceptance/runs/baseline/p1-t02-build.json` ghi handoverCommit/remoteTaskSha cuối cùng, không đổi code đã test |
| PR / integration | Chưa tạo PR; target `message`, chưa merge P1-T02, không thay main |
| FE | http://localhost:15300/?dm-ui=p1-t02-20261009 |
| API / Swagger | http://localhost:15026/api/v1 / http://localhost:15026/swagger |
| DB / user / project | `scdc_dm_acceptance_test` / `scdc_dm_test` / `scdc-dm-acceptance`; PostgreSQL localhost15432 |
| Migration | `database/postgres/migrations/20261009_dm_p1_open_conversation.sql`, additive/idempotent |
| Dataset | Run `baseline`, 28 tài khoản và IDs P0 được giữ; manifest `.dm-acceptance/runs/baseline/manifest.json` |
| Browser / môi trường | Edge headless thật trên Windows, viewport1440×1000; API .NET10 và PostgreSQL thật trong Docker Linux |

API `POST /direct-conversations` trả200 cho cả create/get sau commit, ID mới UUIDv7; participants đúng hai người, `lastSequence="0"`, `lastActivityAt=null` khi chưa có tin. Chuẩn hóa cặp theo UUID network bytes. Identity guard giữ SHARE lock user theo thứ tự UUID rồi session, kiểm tra actor/session/stamp/expiry và eligibility. Hai DbContext dùng cùng NpgsqlConnection/DbTransaction ReadCommitted; owner giữ quyền commit/rollback. Messaging chỉ đọc bảng Messaging, lấy public user qua Contracts.

Unique conflict chỉ ở constraint cặp: rollback toàn space/member của request thua, clear tracker, transaction mới kiểm tra lại guard và đọc cặp thắng. Deferred constraints bảo đảm space DM có pair và đúng hai active membership của pair; thêm thành viên thứ ba, xóa một thành viên hoặc commit space DM mồ côi đều bị23514. Mở lại DM đã có vẫn được nếu peer bị khóa, DTO chỉ `availability="unavailable"`, không lộ lý do. Actor bị khóa/phiên thu hồi vẫn401.

Trong UI, DM dùng response thật và giữ danh sách đã mở trong RAM theo actor; bỏ DM/tin mẫu ở Home, chưa kết nối Hub DM hoặc cho gửi tin giả. Reload cần tìm peer và mở lại bằng POST; loader inbox là P1-T03. Người vừa nhắn tin cần P1-T03/P2-T01. Gửi/lịch sử thuộc P2; chưa có tin để nghiệm thu chức năng đó.

## Chuẩn bị và chạy

Docker Desktop phải đang chạy Linux engine. Đóng tab cũ hoặc Ctrl+F5 để nhận bundle mới. Dùng worktree bên dưới, tránh khởi động image P0 từ checkout `scdc`.

```powershell
Set-Location E:\Project\SCDC\dm-message-integration
Set-ExecutionPolicy -Scope Process Bypass
git branch --show-current
git rev-parse HEAD
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start -NoBuild
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Status
# Chỉ khi cần rebuild/áp migration trên stack đã có:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Upgrade
# Dừng/khởi động lại, giữ volume và key:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Restart
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Stop
```

Không chạy lại `schema.sql` trên volume cũ: file đó dựng schema mới và có DROP. `Upgrade` dùng migration additive. Không cần `Setup` lại baseline hằng ngày; manifest có dữ liệu DM cần giữ để đối soát.

| Alias | Username / tên | ID |
|---|---|---|
| A | dm_demo_an / An Demo | 01a114bc-1e4d-74c7-87eb-8db1734f8aea |
| B | dm_demo_bao / Bảo Demo | 01a114bc-2678-7d79-b136-4d944d6a85fc |
| C | dm_demo_chi / Bảo Demo | 01a114bc-2b41-7c64-8622-aa1694b2de9b |
| U | dm_demo_pending / pending, chưa verified | 01a114bc-304f-79a0-b4d8-b44bfa642958 |
| Snn | dm_demo_search01…23 / Người tìm nn | Resolve từ manifest/helper theo alias S01…S23 |

Mật khẩu local cho tất cả: `DmDemo2026!Local`. U không được login ứng dụng. K vẫn active/verified; bài test khóa dùng fixture integration riêng rồi cleanup, không khóa account baseline.

Helper mỗi lệnh Open/Concurrent đăng nhập phiên riêng, giữ token trong RAM và logout khi kết thúc; không in token. `Snapshot` chỉ đọc SQL. `FaultOn/FaultOff` chỉ tạo/gỡ test trigger trong DB acceptance và chỉ tác động pair đã chọn; không có API/developer controls production. `RequireNewPair` từ chối pair đã tồn tại, không xóa dữ liệu để ép ca mới.

## C01 — A/B mở hai chiều, reload và phân biệt B/C

Lane C01 dùng chung pair A/B cho FE và BE. Agent đã tạo D-AB `01a11c8f-534c-761c-8d4a-4930c95bee58`; người dùng đang nghiệm thu đường get/reopen. Baseline pair1/member2/message0; các bước replay tăng0. Để nghiệm thu create mới trên UI, dùng bổ sung A/C nếu Snapshot còn0; không ghi replay thành create.

| Bước | Thao tác | Kỳ vọng | Agent / người dùng |
|---|---|---|---|
| FE1 | A login → Direct Messages → dấu+ → nhập Bảo → chọn @dm_demo_bao rồi @dm_demo_chi | Hai dòng tìm được nhưng chỉ một thẻ @dm_demo_chi được chọn; chưa gửi POST/chưa có item DM mới | Edge đạt / Chưa xác nhận |
| FE2 | Chọn lại @dm_demo_bao, bấm Mở hội thoại; xem Network POST | 200, ID D-AB; tiêu đề Bảo Demo, topic @dm_demo_bao; “Chưa có tin nhắn.”; một item, không tin mẫu | Edge đạt / Chưa xác nhận |
| FE3 | B login ở browser profile độc lập, tìm dm_demo_an, chọn An, bấm Mở hội thoại | 200 cùng D-AB, peer An Demo/@dm_demo_an | Edge A/B đạt / Chưa xác nhận |
| FE4 | Reload A, về Direct Messages, tìm và mở lại B | GET inbox chưa có nên danh sách RAM chưa tự nạp; POST lại200 cùng D-AB, một item | Edge đạt / Chưa xác nhận |
| FE5 | A chọn @dm_demo_chi rồi Mở hội thoại | D-AC khác D-AB; cùng tên Bảo nhưng username @dm_demo_chi; pair riêng đúng2 membership | Dành người dùng, chưa tạo A/C / Chưa xác nhận |

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -ActorAlias A -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias B -PeerAlias A
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias B
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -ActorAlias A -PeerAlias C
# Sau FE5, BE replay cùng cặp, không tạo lane thứ hai:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias C
```

Mọi Open200 cùng cặp phải cùng ID, participants đúng hai IDs, không email/security state. Snapshot A/B cuối pair1/member2/message0/orphan0; delta0. A/C nếu ban đầu0: sau FE5 pair+1/member+2, BE replay delta0. Bấm dấu+ mở B thêm lần nữa trong cùng phiên phải vẫn một item D-AB.

## C02 — 40 request cùng barrier, hai chiều

Agent đã chạy pair A/S02: baseline0 → pair1/member2; 40 request20 mỗi chiều, cùng ID `01a11c95-b372-79da-bd62-def51ade8219`. Để bạn kiểm tra create mới, dành pair **A/S10**. Chạy runner trước khi mở UI; nếu Snapshot đã1, đổi cả recipe sang S12 hoặc alias chưa dùng. Không xóa pair.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S10
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Concurrent -PeerAlias S10 -RequireNewPair
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias S10
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias S10 -PeerAlias A
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S10
```

BE mong đợi: `requests=40`, `directions=[20,20]`, một conversationId; before pair0/member0, after pair1/member2/orphan0/message0. `actorSpaceCount` tăng1 nếu không có thao tác khác cùng actor. Mọi response200, không500/409; hai Open sau dùng cùng ID. Runner có start barrier thật; fixture backend còn giữ advisory gate sau insert space và chỉ thả khi đủ40 writer đang chờ, buộc unique conflict xảy ra thay vì phụ thuộc lịch thread. Unit/provider test dùng UUID khác thứ tự little-endian để so chuẩn hóa với PostgreSQL.

FE: A tìm `dm_demo_search10` → Mở hội thoại, S10 ở profile riêng tìm `dm_demo_an` → Mở hội thoại. Cả hai POST cùng ID runner; A mở lại S10 hai lần không duplicate. Chọn một người được giữ khi loading, double click không gửi POST thứ hai. Proof runner lưu `.dm-acceptance/runs/baseline/p1-t02-concurrent-A-S10.json`. Người dùng FE/BE: **Chưa xác nhận**.

## C03 — self, pending, UUID sai, revoked session

FE A: tìm `dm_demo_an` và `dm_demo_pending` đều không có kết quả. Khi chưa chọn người, Mở hội thoại bị disable. Nếu search thành công rồi backend từ chối/mất kết nối, modal giữ lựa chọn, không tạo item giả.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias A -ExpectedStatus 400
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias U -ExpectedStatus 404
# REST biến thể, token chỉ giữ trong biến:
Import-Module ./scripts/dm-acceptance/Common.psm1 -Force -DisableNameChecking
$a = Connect-DmActor A baseline 'P1-T02-C03'
try {
    $zero = Invoke-DmRequest POST /direct-conversations @{peerUserId='00000000-0000-0000-0000-000000000000'} -AccessToken $a.accessToken
    $bad = Invoke-DmRequest POST /direct-conversations @{peerUserId='not-a-uuid'} -AccessToken $a.accessToken
    $missing = Invoke-DmRequest POST /direct-conversations @{} -AccessToken $a.accessToken
    foreach ($r in $zero,$bad,$missing) { Assert-DmStatus $r 400 'Invalid UUID'; if ($r.Body.errorCode -ne 'Common.ValidationFailed') { throw 'Wrong error code' } }
    $unknown = Invoke-DmRequest POST /direct-conversations @{peerUserId=[guid]::NewGuid().ToString()} -AccessToken $a.accessToken
    Assert-DmStatus $unknown 404 'Unknown peer'
    if ($unknown.Body.errorCode -ne 'RESOURCE_NOT_FOUND') { throw 'Wrong unknown-peer code' }
    $anonymous = Invoke-DmRequest POST /direct-conversations @{peerUserId='01a114bc-2678-7d79-b136-4d944d6a85fc'}
    Assert-DmStatus $anonymous 401 'Anonymous'
    $null = Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken}
    $revoked = Invoke-DmRequest POST /direct-conversations @{peerUserId='01a114bc-2678-7d79-b136-4d944d6a85fc'} -AccessToken $a.accessToken
    Assert-DmStatus $revoked 401 'Same revoked token'
    if ($revoked.Body.errorCode -ne 'Common.Unauthorized') { throw 'Wrong revoked code' }
    $zero,$bad,$missing,$unknown,$anonymous,$revoked | ForEach-Object { [pscustomobject]@{status=$_.Status;errorCode=$_.Body.errorCode} }
} finally { $null = Invoke-DmRequest POST /auth/logout @{refreshToken=$a.refreshToken} }
```

Self400 `INVALID_PEER`; pending/unknown404 `RESOURCE_NOT_FOUND`; không tiết lộ lý do peer. Snapshot trước/sau phải giữ pair/space/member counts. Logout đúng phiên REST A không thu hồi phiên FE độc lập. Integration test thêm disabled/deleted/unverified peer, actor disabled, stamp sai, session sai owner/hết hạn; DM đã có vẫn đọc được peer unavailable. Các fixture đó cleanup riêng, không sửa U/K baseline. Chưa có endpoint detail/inbox để test C đọc D-AB; kiểm thử outsider đó thuộc P1-T03/P2. Agent API/provider đạt, người dùng **Chưa xác nhận**.

## C04 — lỗi sau insert space, trước pair/commit

Dành pair **A/S11** còn0. Lỗi được gây ở PostgreSQL trigger BEFORE INSERT pair, sau SaveChanges space; response thực503 `AUTHORITY_UNAVAILABLE`, không phải mô phỏng fetch. FE và BE dùng cùng pair/fault; cả hai đều thất bại nên delta0. Sau tắt fault, FE retry tạo+1, BE replay delta0.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action FaultOn -PeerAlias S11 -RequireNewPair
```

FE A tìm `dm_demo_search11`, chọn người, bấm Mở hội thoại. Network phải503 `AUTHORITY_UNAVAILABLE`; modal vẫn mở, thẻ @dm_demo_search11 còn, alert “Không mở được hội thoại. Hãy thử lại.”, không item S11 mới. Trong lúc fault đang bật, chạy BE rồi Snapshot:

```powershell
try {
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -PeerAlias S11 -ExpectedStatus 503
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11
} finally {
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action FaultOff
}
```

Snapshot sau503 phải pair0/member0/message0/orphan0, `actorSpaceCount` bằng trước fault; không ID thành công/chưa commit. Bấm lại Mở hội thoại trong chính modal FE: 200, tên Người tìm11 và @dm_demo_search11, một item, Chưa có tin nhắn. Sau FE retry chạy BE:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -PeerAlias S11
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11
# Nếu dừng test giữa chừng, luôn chạy:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action FaultOff
```

Cuối pair1/member2/message0, cùng ID FE/BE, actorSpace+1. Agent đã chạy provider rollback/retry, FE Edge với pair A/S03 và REST helper A/S04; người dùng **Chưa xác nhận**. Pair fault của agent đã gỡ, không có injection đang hoạt động lúc bàn giao.

## SQL chỉ đọc để đối soát

Chạy sau mỗi case; query liệt kê đúng DM có actor A, không lấy token/message content. Low/high phải `ordered=true`, `members=2`, `active_members=2`, `member_ids` đúng pair, `messages=0`, `last_sequence=0`. Query orphan phải0. S10/S11 dùng Snapshot helper nếu cần scope một cặp.

```powershell
Import-Module ./scripts/dm-acceptance/Common.psm1 -Force -DisableNameChecking
Invoke-DmReadSql @'
SELECT d.space_id,d.user_low_id,d.user_high_id,(d.user_low_id<d.user_high_id) AS ordered,
 s.space_type,coalesce(s.last_message_sequence,0) AS last_sequence,s.last_activity_at,
 (SELECT count(*) FROM messaging.space_members m WHERE m.space_id=d.space_id) AS members,
 (SELECT count(*) FROM messaging.space_members m WHERE m.space_id=d.space_id AND membership_status=1 AND left_at IS NULL) AS active_members,
 (SELECT string_agg(user_id::text,',' ORDER BY user_id) FROM messaging.space_members m WHERE m.space_id=d.space_id) AS member_ids,
 (SELECT count(*) FROM messaging.messages t WHERE t.space_id=d.space_id) AS messages
FROM messaging.direct_conversations d JOIN messaging.spaces s ON s.id=d.space_id
WHERE '01a114bc-1e4d-74c7-87eb-8db1734f8aea' IN (d.user_low_id,d.user_high_id)
ORDER BY d.space_id;
SELECT count(*) AS orphan_direct_spaces FROM messaging.spaces s
WHERE space_type=1 AND NOT EXISTS(SELECT 1 FROM messaging.direct_conversations d WHERE d.space_id=s.id);
'@
```

## Bản dữ liệu và image lúc bàn giao

DB hiện có7 pair/14 membership/0 message; không có fault trigger của acceptance. Pair A/C, A/S10 và A/S11 vẫn0 để người dùng test tạo mới. Manifest đã bổ sung7 ID hội thoại commit từ POST API, giữ28 account và user IDs cũ. Agent test các pair còn lại; không lấy tổng7 làm kỳ vọng một case.

API image `sha256:6b8de3952ee88aa1b5956d73d364f18f28a97e05a97971a00d92a8ec7ed3087f`. FE E2E dùng image `sha256:0b14f47a991c5d22652025f975dcf15632010483db0eb68dda56e139be24af58`; Upgrade thay attestation/tag nên container bàn giao được align tag mới sau khi đối chiếu SHA256 toàn bộ site assets, entry và cấu hình Nginx giống hệt. Entry đang phục vụ `index-DqK4M8fD.js`, no-store; image IDs cuối nằm ở metadata. Bản backend test được giữ thêm `.dm-acceptance/runs/baseline/p1-t02-backend.trx` để không bị lệnh test sau ghi đè.

## Bằng chứng agent và gate

| Kiểm tra | Kết quả / bằng chứng |
|---|---|
| Backend PostgreSQL thật | 43/43; `.dm-acceptance/backend-artifacts/test-results/dm-acceptance.trx` |
| Shared transaction | Cùng connection object, DbTransaction, `pg_backend_pid` và `txid_current` ở Identity/Messaging |
| Race |40 writer bị giữ sau insert space rồi thả; cùng ID, đúng1 space/+2 member; runner HTTP thật A/S02 cũng40/40 |
| Mất kết nối trước controller | Provider kết nối port đóng trong host test trả503 AUTHORITY_UNAVAILABLE ngay ở auth middleware; counts DB không đổi |
| Guard/revoke | Logout chờ guard đang giữ lock rồi401 với token cũ; revoke commit trước guard recheck khiến request401, không space mới |
| UUID/constraint | Network-order đối chiếu PostgreSQL; commit orphan/third member/delete one member đều23514, DB giữ cặp đúng |
| Frontend API unit/build |10/10; Vite và Docker build đạt |
| Edge E2E |9/9, gồm search regression theo override chọn một, create/get/reload/reverse, lỗi DB thật/retry, double click và late response sau đóng; `.dm-acceptance/e2e-p1-t02/results.json` |
| Recipe PowerShell |Smoke C01/C03, Concurrent A/S02, Fault A/S04 đã chạy thật; Snapshot/SQL chỉ đọc đối soát |
| Migration |Đã áp additive rồi chạy lại trên volume acceptance, giữ user IDs; fresh schema trên DB probe riêng có3 deferred triggers, probe đã dọn; constraints hoạt động qua PostgreSQL tests |

Test đầu phát hiện validation attribute sai target ở positional record và cleanup lúc provider đã đóng transaction; đã sửa và regression đạt. Test frontend đầu dùng tên mẫu sai so với manifest, sửa lấy tên thật. Một lượt bị agent restart API khi E2E còn chạy; đã sửa FaultOff để cleanup không cần API và chạy lại trên stack ổn định, suite cuối9/9. Dữ liệu DM của agent giữ để đối soát; tài khoản integration chỉ xóa trong scope fixture own IDs, không drop DB/volume.

Xác nhận người dùng C01/C02/C03/C04 FE và BE: **Chưa xác nhận**. Quyền push task đã có; merge `--no-ff` vào `message` chỉ sau PASS trên build bàn giao, fetch lại remote, smoke và xác minh ancestry. Chưa giao/triển khai P1-T03; chưa nghiệm thu toàn UC DM. Ghi phản hồi `PASS DM-P1-T02 | FE: đạt | BE: đạt | Build: <SHA>` hoặc FAIL kèm case/bước/expected/actual.

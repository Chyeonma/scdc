# DM-P0-T01 — bản chạy và hướng dẫn người dùng kiểm tra

Trạng thái task: **Chờ người dùng test FE/BE và review baseline**. Phần agent và người dùng ghi riêng. Người dùng chưa xác nhận PASS; không bắt đầu DM-P1-T01. Scope P0 là môi trường, Identity baseline và contract/thiết kế DM; search/send/history/Hub DM chưa triển khai, chat nhìn thấy trên FE vẫn là mock.

## Bản chạy và dữ liệu thật

| Trường | Giá trị |
|---|---|
| Branch / base | `chore/dm-p0-t01-acceptance-baseline` / `fe3c54a` |
| Commit bàn giao | Đọc `git rev-parse HEAD` sau commit P0; runtime metadata ở `.dm-acceptance/runs/baseline/build.json` |
| Web | http://localhost:15300 |
| API | http://localhost:15026/api/v1 |
| Swagger Identity/Health | http://localhost:15026/swagger |
| Proxy Swagger qua FE | http://localhost:15300/swagger |
| PostgreSQL | `localhost:15432`, DB `scdc_dm_acceptance_test`, user `scdc_dm_test`; password local trong `.env.dm-test` |
| Compose / volume | `scdc-dm-acceptance` / `scdc-dm-acceptance_dm-postgres-data` |
| Schema | Repo schema hiện hành, chỉ init volume mới, không seed.sql; SHA256 `6C68AFA1D973D25A08AA67282843D529CD59299013E0121BD4B2AF9B7D36D7DB` |
| Runtime manifest | `.dm-acceptance/runs/baseline/manifest.json`, ngoài Git, không token |
| Fixture | 28 user thật: A/B/C/K/S01–23 active/verified (27), U pending/unverified (1) |
| DM rows | direct_conversations/messages/message_edits đều0 trong DB thử P0 |
| Key/config | `.env.dm-test`, `.dm-acceptance/keyrings/hmac`, cursor dir riêng; P0 Foundation chưa tiêu thụ cursor/HMAC config |
| PR/push/merge | Chưa thực hiện; chỉ branch/commit local trong phạm vi P0 |

Mật khẩu chung của **tài khoản fixture local**: `DmDemo2026!Local`. Email example.test không nhận thư thật. A/B/C/K/S được verify bằng Development token qua API thật; đây không phải bằng chứng gửi email sản phẩm.

| Alias | Username | User ID thật | Trạng thái |
|---|---|---|---|
| A | dm_demo_an | `01a114bc-1e4d-74c7-87eb-8db1734f8aea` | active/verified, displayName An Demo |
| B | dm_demo_bao | `01a114bc-2678-7d79-b136-4d944d6a85fc` | active/verified, displayName Bảo Demo |
| C | dm_demo_chi | `01a114bc-2b41-7c64-8622-aa1694b2de9b` | active/verified, displayName Bảo Demo |
| U | dm_demo_pending | `01a114bc-304f-79a0-b4d8-b44bfa642958` | pending, email chưa verify, không có app session |
| K | dm_demo_khoa | `01a114bc-358f-7a7c-a875-e0932d642384` | active/verified; P0 không khóa K |
| S01–23 | dm_demo_search01…23 | Theo manifest28aliases | active/verified; P0 chưa có search API |

IDs trên thuộc run baseline máy này. Khi dựng trên máy/run mới, lấy ID từ manifest/response, không dùng bảng trên làm input seed. P0 không tạo D-AB/D-AC/D-AK/D-HIST hoặc H121/R101 vì DM API chưa có.

## Lệnh khởi chạy, dừng và đối soát

PowerShell từ `E:\Project\SCDC\scdc`; Docker Desktop dùng Linux containers đã chạy. Không dùng compose.yaml cho ca acceptance. Init giữ config/key hiện có; muốn đổi cổng trước khi start thì chỉnh đúng các `DM_*_PORT` trong `.env.dm-test` ngoài Git.

```powershell
Set-Location E:\Project\SCDC\scdc
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Init
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Setup -Run baseline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Status
```

Nếu đã build, `Start -NoBuild` dùng image hiện có. Script kiểm cổng/target DB/container; không dừng tiến trình khác để chiếm cổng. Setup chỉ adopt đúng account/ID/email/state của manifest do helper sở hữu; trùng account không có ownership sẽ dừng, không overwrite. Setup bị ngắt ngay sau register/verify có thể để manifest incomplete: đọc lỗi và phục hồi Development token/state có kiểm chứng, không xóa DB hoặc tự cấp quyền để tiếp tục. Run cô lập có thể dùng hậu tố ngắn như `r02`; username thực tế nằm trong manifest.

```powershell
# Dừng riêng stack acceptance, giữ volume và keys
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Stop
# Chạy lại mà không build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Start -NoBuild
# Restart và chờ API/proxy/DB sẵn sàng
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Restart
# Đối soát SQL chỉ đọc, không in hash/token/password
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Db
```

`schema.sql` có DROP SCHEMA CASCADE: **chỉ mount init trên volume mới**, không dùng làm migration/restart/repair. Script không có hành động drop/down-v/remove-volume. Query helper dùng BEGIN READ ONLY, cả khi nhận SqlFile tùy chọn. Health HTTP200 chỉ mô tả host/modules; helper kiểm PostgreSQL bằng truy vấn thật rồi register/login để chứng minh kết nối DB.

## Chuẩn bị REST trong bộ nhớ

Trong terminal PowerShell của bạn, import module một lần. Không in `$a/$b` nguyên object hoặc export transcript/HAR chưa lọc token. Token mới hết hạn sau thời hạn Identity hiện có; khi cần login lại thì ghi đó là phiên mới, không dùng phiên mới để chứng minh token đã revoke bị chặn.

```powershell
Import-Module ./scripts/dm-acceptance/Common.psm1 -Force -DisableNameChecking
$manifest = Get-DmManifest -Run baseline
$manifest.accounts | Select-Object alias,id,username,state
$a = Connect-DmActor -Alias A -Run baseline -DeviceName 'DM-user-BE-A'
$b = Connect-DmActor -Alias B -Run baseline -DeviceName 'DM-user-BE-B'
$a.user | Select-Object id,username,displayName,emailVerified,status
```

File [baseline.http](../../../scripts/dm-acceptance/baseline.http) là corpus Identity/Health cho REST Client; token placeholders phải được điền **local**, không commit. Các lệnh PowerShell dưới đây trực tiếp dùng token trong memory và ID của response nên không cần tự đoán ID/bảng.

## DM-P0-T01-C01 — login và lưu hồ sơ rồi đọc lại

Điều kiện trước: A active/verified, API/proxy healthy; A browser profile riêng và `$a` là lane BE riêng. FE/BE đều PATCH nhưng không gửi tin/không tạo user mới; ghi version/delta từng lane, không cộng hai PATCH rồi kỳ vọng một thay đổi.

1. FE: mở http://localhost:15300; nhập username `dm_demo_an`, password mẫu, bấm nút Đăng nhập của form. Mong đợi dock hiển thị `@dm_demo_an`; Network POST login200, GET me200 cùng A.id.
2. FE: bấm nút bánh răng **Cài đặt** dưới dock → **Hồ sơ của tôi**. Giữ displayName `An Demo`, nhập Bio `Kiểm tra hồ sơ DM-P0-T01-C01`, bấm **Lưu thay đổi**. Mong đợi PATCH `/api/v1/users/me`200 với bio mới; lỗi phải hiện thật, không giả lưu thành công.
3. FE: reload, mở lại Cài đặt. Mong đợi đúng username/displayName/bio đã lưu, không rỗng vì chỉ giữ local state.
4. BE: chạy nguyên khối dưới đây; cả GET/PATCH/GET200, A.id không đổi, bio đúng tuyệt đối; DB query tiếp theo cũng khớp.

```powershell
$beforeA = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $a.accessToken
$patchA = Invoke-DmRequest -Method PATCH -Path '/users/me' -AccessToken $a.accessToken -Body @{
    displayName='An Demo'; bio='Kiểm tra hồ sơ DM-P0-T01-C01'
    locale='vi-VN'; timezone='Asia/Ho_Chi_Minh'
}
$afterA = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $a.accessToken -ViaWeb
$beforeA.Status
$patchA.Status
$afterA.Status
$afterA.Body | Select-Object id,username,displayName,bio,locale,timezone
Invoke-DmReadSql "SELECT u.id,u.username,p.bio FROM identity.users u JOIN identity.user_profiles p ON p.user_id=u.id WHERE u.normalized_username='dm_demo_an';"
```

PASS khi FE sau reload/API/DB đều có bio mẫu và ID đúng A; không đổi B/C/user/email. Bằng chứng: build, lane, ảnh hồ sơ sau reload không có token, HTTP statuses và SQL result. Sau ca giữ bio mẫu để đối soát; không reset account/user.

## DM-P0-T01-C02 — U pending không được cấp phiên

Điều kiện trước: U status0, verified_at=null; không dùng token Development để verify U. Browser U riêng, chưa có phiên A/B.

1. FE: mở web ở profile U, nhập `dm_demo_pending`/password mẫu, bấm Đăng nhập. Mong đợi error banner về chưa verify, form auth vẫn hiện, không user dock/inbox ứng dụng.
2. FE: kiểm Network login403, errorCode `Identity.EmailNotVerified`; response không accessToken/refreshToken. Không dùng text thông báo để đoán mọi403 là cùng lỗi.
3. BE: chụp sessions trước/sau rồi gọi login U và anonymous GET me cả direct/proxy bằng khối dưới. Không đưa token Bearer của A vào ca anonymous.

```powershell
$uBefore = Get-DmUserSnapshot -Username dm_demo_pending
$pending = Invoke-DmRequest -Method POST -Path '/auth/login' -Body @{
    login='dm_demo_pending'; password='DmDemo2026!Local'; deviceName='DM-user-U'
}
$anonymous = Invoke-DmRequest -Method GET -Path '/users/me'
$anonymousProxy = Invoke-DmRequest -Method GET -Path '/users/me' -ViaWeb
$uAfter = Get-DmUserSnapshot -Username dm_demo_pending
$pending.Status
$pending.Body | Select-Object status,errorCode
$anonymous.Status
$anonymousProxy.Status
$uBefore | Select-Object id,status,verified_at,active_sessions
$uAfter | Select-Object id,status,verified_at,active_sessions
```

Kỳ vọng: pending403 `Identity.EmailNotVerified`; anonymous direct/proxy401 `Common.Unauthorized`; active_sessions U vẫn0 và email chưa verify. PASS cần FE không vào ứng dụng và API/DB đúng; lưu statuses/IDs/counts, không verification token. Sau ca U giữ nguyên pending.

## DM-P0-T01-C03 — logout A không ảnh hưởng B

Điều kiện trước: FE A1/B1 ở hai browser profiles độc lập, không chỉ hai tab dùng cùng localStorage. Lane BE dùng `$a/$b` đã login ở trên và kiểm đúng những session đó; token A_BE không đại diện cho phiên A_FE đã logout.

1. FE: login A1/B1, đọc dock đúng username. A1 mở Cài đặt → **Đăng xuất** (không nút logout-all); POST `/auth/logout`204. Reload A1 thấy form login.
2. FE: reload B1, dock vẫn `@dm_demo_bao` và GET me200. Không chuyển B1 thành A hoặc logout B vì A1 logout.
3. BE: GET me A/B trước logout đều200. Logout chính `$a.refreshToken`; dùng lại **chính `$a.accessToken` cũ** GET me phải401; `$b.accessToken` còn200/B.id. Không login A mới giữa logout và kiểm token cũ.

```powershell
$meA = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $a.accessToken
$meB = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $b.accessToken
$outA = Invoke-DmRequest -Method POST -Path '/auth/logout' -Body @{refreshToken=$a.refreshToken}
$oldA = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $a.accessToken
$liveB = Invoke-DmRequest -Method GET -Path '/users/me' -AccessToken $b.accessToken
$meA.Status
$meB.Status
$outA.Status
$oldA.Status
$liveB.Status
$liveB.Body | Select-Object id,username
Invoke-DmReadSql "SELECT u.username,s.id AS session_id,s.device_name,s.revoked_at FROM identity.auth_sessions s JOIN identity.users u ON u.id=s.user_id WHERE s.device_name IN ('DM-user-BE-A','DM-user-BE-B') ORDER BY s.created_at;"
```

Kỳ vọng200/200/204/401/200 theo thứ tự; session BE A revoked, B chưa revoked. Không yêu cầu tổng active sessions A=0 vì FE/BE có nhiều login khác nhau. Cleanup sau khi đã chụp B còn200: logout `$b` bằng request204; đóng profiles nếu không dùng tiếp, không xóa user.

```powershell
$cleanupB = Invoke-DmRequest -Method POST -Path '/auth/logout' -Body @{refreshToken=$b.refreshToken}
$cleanupB.Status
$a = $null
$b = $null
```

## DM-P0-T01-C04 — setup lặp và restart giữ ID/dữ liệu

Điều kiện trước: run baseline đã setup đủ28; không đang thao tác profile/login/migration ở terminal khác. Copy SQL counts/IDs baseline trước ca, không xóa volume.

1. FE: login A/C ở profile riêng, ghi username/ID qua GET me; C displayName Bảo Demo nhưng username khác B.
2. BE: chạy Persistence dưới; helper query28ID/profile/email trước, chạy Setup hai lần, restart riêng stack acceptance, đợi direct/proxy/DB sẵn sàng, query lại và so khớp toàn bộ. Mong đợi dòng `C04 PASS` của **agent/helper**, không thay xác nhận của bạn.
3. FE: sau restart reload/login lại khi cần, mở hồ sơ A/C. Mong đợi IDs không đổi, bio A vẫn mẫu; U vẫn pending. Hành vi dùng login mới phải ghi rõ phiên mới.
4. BE: đọc artifact/manifest và Db; đúng28accountfixture/27verified/1pending, không duplicate username/email. DM rows vẫn0. Script Setup không tạo thêm login session để kiểm user nên counts session không tăng do riêng hai lần Setup.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Persistence -Run baseline
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Db
Get-Content .dm-acceptance/runs/baseline/persistence.json -Raw -Encoding UTF8
```

PASS khi HTTP/profile và PostgreSQL snapshot giữ IDs/trạng thái/bio, config/HMAC key không bị regenerate; cursor ring P0 chưa cấp cursor nên chưa có cursor restart proof. Bằng chứng restart/IDs/counts/hash schema và HTTP statuses, không key bytes. Sau ca giữ volumes/config/run; không chạy schema.sql lại.

## Kiểm thử agent và bằng chứng

| Kiểm chứng | Loại proof | Kết quả agent | Người dùng |
|---|---|---|---|
| Fresh Compose build/start, API/proxy và PostgreSQL18.6 | Docker/.NET10/Node24/nginx + SQL thật | Đạt | Chưa xác nhận |
| Backend smoke C01/C02/C03 + health/anonymous direct/proxy | HTTP thật và DB snapshots | Đạt; `.dm-acceptance/runs/baseline/backend-smoke.json` | Chưa xác nhận |
| Setup lặp2 lần và restart C04 | Compose restart + snapshot28records thật | Đạt; `.dm-acceptance/runs/baseline/persistence.json` | Chưa xác nhận |
| Helper ownership/read-only/path/key guard | PostgreSQL/container thật; manifest tạm khôi phục trong finally | Đạt; `helper-safety.json`, 28users không đổi, SQL read-only=on, Init giữ key/config | Chưa xác nhận |
| Stop/Start giữ volume/key | Compose stop/start riêng stack + healthy readback | Đạt; stack được để chạy cho user test | Chưa xác nhận |
| FE unit tests | Node built-in tests, session/fetch simulation | 6/6; không thay BE proof | Chưa xác nhận |
| FE production build | Vite build; cả host và Node24 Docker build | Đạt | Chưa xác nhận |
| Backend full suite | Container .NET SDK10, PostgreSQL test thật | 16/16; `.dm-acceptance/backend-artifacts/test-results/dm-p0-baseline.trx` | Chưa xác nhận |
| FE E2E4ca | Edge Chromium trên Windows, live API/proxy/DB | 4/4, Edge154.0.4258.53; `.dm-acceptance/e2e/results.json` | Chưa xác nhận |
| DM contract/migration/guard/key/reuse design | [Baseline thiết kế](../../plans/dm-p0-baseline-design.md); contract extensions | Agent chọn; chưa implementation/proof DM | Chưa review |
| Docs links/schema/corpus/PowerShell | Kiểm tra kỹ thuật | 37files/1822links,0 lỗi; JSON7operations/error status mapping; PowerShell5.1 parse/UTF-8 BOM; JS syntax | Chưa xác nhận |

Lệnh tái lập:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance.ps1 -Action BackendTests
Set-Location clients/WebClient
npm.cmd ci
npm.cmd test
npm.cmd run build
npm.cmd run test:dm-p0
```

Windows E2E mặc định dùng Edge đã cài (`DM_BROWSER_CHANNEL=msedge`), không tự mở cửa sổ tương tác. Máy khác cần browser tương thích: `npx.cmd playwright install chromium`, cấu hình channel/browser khi thích hợp. Evidence JSON ở `.dm-acceptance/e2e`; không bật trace/HAR/storage-state export vì có auth bodies. E2E metadata có browserVersion/run/userId, không tokens. Node host máy này22.19.0, image frontend Node24; báo đúng môi trường, không suy đã test mobile/IME.

Host `dotnet restore` ban đầu gặp NU1301/SSL credential trong sandbox; dùng container .NET10 cho backend validation. Không báo lỗi đó thành PASS host restore. Docker/npm cache cần quyền runtime; npm local cache đã chuyển vào `.dm-acceptance/npm-cache` ngoài Git.

`npm audit` hiện báo một advisory high ở dependency gián tiếp `source-map-js`; report local `.dm-acceptance/npm-audit.json`. Không chạy audit fix hoặc coi P0 là security/release approval; đánh giá dependency ở gate phát hành. Playwright1.63.0 được pin trong package/lock, không thay package ứng dụng bằng cập nhật diện rộng.

## Ngoài P0 và quyền bước kế

Chưa có DM REST7operations/Hub/counters/cursors/HMAC reader/guard UoW thực thi; không có send/reconnect/edit/delete/revoke-stream≤5s proof. Email thật, khóa K qua test helper, retention/restore/load/browser matrix/mobile và release owners vẫn thuộc các task tương ứng. Health Foundation và mock DM là đúng hiện trạng P0, không nghiệm thu nhắn tin.

Review [baseline thiết kế](../../plans/dm-p0-baseline-design.md): bảy REST operations, Hub DTO, mã lỗi cụ thể, shared UoW/lock order, migration slices và key storage. Xác nhận task P0 có hai phần: tự test FE/BE C01–C04 và đồng ý baseline để triển khai sau. Không coi im lặng hoặc agent PASS là user PASS.

```text
PASS DM-P0-T01 | Build: <commit/build đã test>
FE C01/C02/C03/C04: đạt
BE C01/C02/C03/C04: đạt
Baseline contract/UoW/migration/key design: đồng ý
Task kế: chỉ làm khi được tôi giao hoặc quyền đi tiếp đã được cấp.
```

```text
FAIL DM-P0-T01 | Build: <commit/build> | Case/bước: ...
Thao tác: ... | Mong đợi: ... | Thực tế: ...
Sửa cùng task rồi bàn giao lại; chưa thực hiện DM-P1-T01.
```

Người dùng FE/BE: **Chưa xác nhận**. Baseline review: **Chưa xác nhận**. Merge/publish: chưa thực hiện. Task dừng ở **Chờ người dùng test**.

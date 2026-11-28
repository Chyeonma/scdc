# Community — cô lập mã nguồn và phân loại BuildingBlocks

Hoàn thiện phần dọn dependency sau cutover Community và ghép với main
`311d567` (CI/Trello và Compose runtime đã tích hợp). Không chuyển dữ liệu hoặc
triển khai lại stack dev. Hồ sơ S3-10 cũ giữ nguyên; các kiểm chứng dưới đây
chạy trên project/container, secret, volume và database `_test` riêng.

## Ranh giới sau thay đổi

| Thành phần | Kết quả |
|---|---|
| Community API | Sở hữu controller, lỗi, xác thực HTTP, Swagger, telemetry và RPC receiver; bỏ toàn bộ `Compile Include` lấy source từ `SCDC.Api` |
| Community Infrastructure | Sở hữu connection, transaction, outbox, Redis session reader, gRPC adapter và fence; SQL cố định owner/schema Community |
| Dependency Community | Chỉ project Community, Core thuần .NET, Contracts và hợp đồng `.Grpc`; không tham chiếu BuildingBlocks hoặc implementation service khác |
| Host Messaging | Không reference implementation Community hoặc liên kết source của Community; từ chối `Hosting:CommunityEnabled=true` |
| Component test | Ghép owner tường minh trong fixture, ngoài production; test mạng dùng ba host và database riêng |
| DbMigrator | Chỉ tham chiếu Core và Npgsql, không kéo Redis, framework hoặc transport service qua BuildingBlocks |

Theo D44/D45, monorepo tiếp tục dùng `ProjectReference` cho hợp đồng và thư viện
nhỏ. Adapter cần framework nằm tại owner. Một số adapter giao thức được lặp;
SQL, model lưu trữ, transaction và quyền nghiệp vụ không chuyển vào Core.
Namespace `SCDC.BuildingBlocks.Application.*` còn trên các kiểu thuần .NET
trong assembly Core, không phải reference tới project BuildingBlocks.
[Bảng phân loại từng file](../../../../services/SCDC.BuildingBlocks/README.md)
ghi phần legacy Messaging và hướng chuyển tiếp.

README, kiến trúc, backend, database, môi trường, hướng dẫn và trang tính năng
thống nhất ba host: Identity, Community, Messaging trong `SCDC.Api`. Media
chưa tích hợp, S3-07/S3-08 còn mở. Câu hỏi mở và bảng công việc STEP-4 được
đối chiếu theo permit/fence và transaction cục bộ, không dùng transaction
xuyên service. Nội dung MVP và hồ sơ cũ là bằng chứng lịch sử.

## Đồng bộ với main

Giữ Compose mới: chín service thường trực, cấu hình dev và công cụ chuẩn bị
riêng; Make/script vẫn chạy gate trước khi bật ứng dụng. Ghép các phần trùng
trong hướng dẫn phát triển, môi trường và overlay rollback, giữ cả ranh giới
Community mới lẫn luồng preflight. Lựa chọn Podman mặc định đang có trong phần
làm dở được giữ và đồng bộ vào help, script và hướng dẫn; Docker dùng
`ENGINE=docker`.

Stack gateway thử được bổ sung migration email và volume keyring của Identity.
Trước đó schema baseline chưa có `account_token_policies`/`email_deliveries`,
readiness Identity trả 503 và worker dùng đường dẫn keyring không ghi được.
Không bỏ kiểm tra readiness để cho browser chạy qua.

Health Community khi rollback được định tuyến tới `/api/v1/health/access`
của host ghép cũ; `/health/ready` chỉ có trên host Community độc lập. Có test
cấu hình mặc định/rollback để giữ đúng endpoint, image đã chỉ định và trạng thái
Community target tắt.

## Kiểm chứng

- `dotnet build SCDC.slnx -c Release --no-restore -m:1 --verbosity quiet`:
  0 warning, 0 error.
- 81 test script Python đạt, gồm graph dependency/source link và ranh giới
  DbMigrator, cùng kiểm tra cấu hình rollback. Kiểm tra Markdown và
  `git diff --check` đạt.
- Backend: **465/465 đạt**, 0 lỗi, 0 bỏ qua, khoảng 3 phút 47 giây; chạy DLL
  Release trong SDK Microsoft có Python, PostgreSQL/Redis thử riêng. Các ca
  gồm component, ba host thật, mTLS, thu hồi, timeout, cancellation, replay,
  restart và quyền SQL theo owner. Assertion của ca lost ACK giữ yêu cầu 201,
  bổ sung body lỗi để chẩn đoán khi không đạt.
- Browser qua stack gRPC/mTLS thật: **15 gateway, 55 Community, 14 DM MVP**
  đều đạt. Bao gồm xác thực, điều hướng Community, tạo/quản lý phòng, DM,
  ba transport SignalR, thu hồi và đổi IP sau restart. Cutoff logout ở ca
  gateway 155 ms trong lượt này.
- Dockerfile Community build/publish thành công từ các đường COPY được giới
  hạn vào Community, Core, Contracts và hợp đồng gRPC. Image chạy không có
  `SCDC.Api.dll`, `SCDC.BuildingBlocks.dll`, `SCDC.Identity.Infrastructure.dll`
  hoặc `SCDC.Modules.Messaging.dll`. Endpoint health của Messaging chỉ công
  bố module Messaging; image hiện tại từ chối bật Community.
- Stack runtime của main dùng các image đã build ở trên: đủ chín container
  chạy, không có container job tồn đọng; chuẩn bị, migration, preflight và
  health ba owner đạt.
- Rollback runtime được diễn tập riêng như mục dưới; các stack và volume thử
  đã được dọn theo đúng project.

Bằng chứng cục bộ ở `artifacts/community-finalization-2026-11-28/` (không
commit): `backend-standard-sdk/backend.trx`, `source-runtime-boundaries.json`,
`runtime/runtime-result.json`, `rollback/*/result.json`. Browser ở
`artifacts/gateway/scdc-gateway-20261128121349-2700187/`; log và khóa thử giữ
ngoài Git.

## Diễn tập image compatibility

Image cục bộ `localhost/scdc-community-compat:311d567-abbb7afb629a`, ID
`abbb7afb629a6f51bfaa79c1716d91139c0c7a73a995d2c221bf652def759b78`, chứa host
ghép trước lần dọn nguồn. Image này đã được build trong kiểm chứng Compose
trước đó; không build lại từ host Messaging hiện tại để giả làm rollback.

Trên source/target `_test` riêng, CLI chuyển Community, đối chiếu manifest và
chạy `--rollback-before-writes` trước khi mở writer. Gate xác nhận record
`rolled_back`, nguồn LOGIN, đích NOLOGIN và không có connection runtime đích.
`make up ROLLBACK=1` dùng image compatibility, `make check` đạt, module health
có Community/Messaging. Qua gateway, đăng ký → xác minh → đăng nhập → tạo
server đạt; SQL xác nhận server nằm ở nguồn và target tiếp tục khóa ghi.

Triển khai thực cần giữ image compatibility đã kiểm chứng và đặt
`SCDC_COMMUNITY_ROLLBACK_IMAGE` rõ ràng. Tên database nguồn khác mặc định chọn
bằng `SCDC_COMMUNITY_SOURCE_DATABASE`. Quy trình và giới hạn rollback sau ghi
trong [runbook](../../../../database/postgres/community/README.md) vẫn áp dụng;
không đổi route/connection string để vượt gate.

## Giới hạn và các lượt chẩn đoán

Không deploy dev hoặc chạy lại tải/telemetry toàn stack trong gói này. Các
ca browser và rollback ở trên chứng minh môi trường thử đã nêu, không thay
cho nghiệm thu production hoặc rollback dữ liệu dev.

Lượt backend trên runtime host Fedora bị testhost dừng sau 393 ca; dump được
GDB báo SIGSEGV, chưa xác định nguyên nhân gốc, không tính lượt đó là đạt.
Lượt SDK đầu thiếu Python cho test sinh Redis ACL và có một assertion lost
ACK không đạt khi đang giới hạn CPU/chạy cùng browser. Sau khi dùng SDK có
đủ Python, bỏ giới hạn CPU nhân tạo và tách các lượt nặng, bộ đầy đủ đạt
465/465; không bỏ test hoặc nới TTL/quyền để đạt. Log/dump lỗi giữ ở artifacts
để điều tra riêng môi trường host nếu cần.

BuildingBlocks vẫn phục vụ Messaging legacy. Gói này không hoàn tất tách
host/database Messaging, tích hợp Media hay các tính năng STEP-4.

Nguồn: [ranh giới nguồn](../../../system/backend.md#source-isolation),
[Community](../../../../services/Community/README.md),
[D42–D45](../../../system/backend.md#v1-shared-code),
[luồng Compose](../../../../ops/README.md).

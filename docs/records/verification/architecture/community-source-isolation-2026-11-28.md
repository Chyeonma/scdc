# Community — cô lập mã nguồn và phân loại BuildingBlocks

Hồ sơ ghi việc dọn dependency sau cutover Community, trên nền `f16d829`.
Phạm vi gồm tài liệu hiện hành, source và build; không chuyển dữ liệu hoặc
triển khai lại stack dev. Hồ sơ S3-10 cũ được giữ nguyên.

## Ranh giới sau thay đổi

| Thành phần | Kết quả |
|---|---|
| Community API | Sở hữu controller, lỗi, xác thực HTTP, Swagger, telemetry và RPC receiver; bỏ toàn bộ `Compile Include` lấy source từ `SCDC.Api` |
| Community Infrastructure | Sở hữu connection, transaction, outbox, Redis session reader, gRPC adapter và fence; bộ ghi dữ liệu cố định owner/schema Community |
| Dependency Community | Chỉ các project Community, Core thuần .NET, Contracts và hợp đồng `.Grpc`; không tham chiếu BuildingBlocks hoặc implementation service khác |
| Host Messaging | Bỏ reference Application/Infrastructure Community, source link controller/filter/receiver và đăng ký Community trong production |
| Component test | Ghép các owner tường minh trong fixture; không đưa composition thử nghiệm vào production |
| DbMigrator | Chỉ tham chiếu Core và Npgsql; không kéo Redis, framework hoặc transport service qua BuildingBlocks |

Theo D44/D45, monorepo tiếp tục dùng `ProjectReference` cho hợp đồng và thư viện
nhỏ. Adapter cần framework nằm tại owner. Một số adapter giao thức được lặp;
không đưa SQL, model lưu trữ, transaction hoặc quyền nghiệp vụ vào Core.
Namespace `SCDC.BuildingBlocks.Application.*` còn trên các kiểu thuần .NET
trong assembly Core, không phải reference tới project BuildingBlocks.
[Bảng phân loại từng file](../../../../services/SCDC.BuildingBlocks/README.md)
ghi phần legacy Messaging và hướng chuyển tiếp.

README, kiến trúc, backend, database, môi trường, hướng dẫn phát triển/vận hành
và trang tính năng thống nhất ba host hiện hành: Identity, Community và
Messaging trong `SCDC.Api`. Media chưa tích hợp; S3-07/S3-08 còn mở. Nội dung
MVP và các hồ sơ cũ là bằng chứng lịch sử, không phải topology hiện hành.

## Kiểm chứng

- `dotnet build SCDC.slnx -c Release --no-restore -m:1 --verbosity quiet`:
  đạt, 0 warning và 0 error.
- `python3 -m unittest discover -s scripts/tests -q`: 45 test đạt, gồm ba test
  mới kiểm graph dependency, source link và dependency công cụ migration.
  Workflow CI hiện có tự discover các test này.
- `IdentityNetworkTests` và `IdentityEmailWorkerTests`: 37/37 đạt sau khi
  fixture Identity chuyển sang ba Kestrel host và ba database riêng. Peer
  `chat-service` chỉ có owner Messaging; `community-service` có Community và
  delegation `ChannelRead` giới hạn.
- Hồi quy backend đầy đủ: **465/465 đạt**, 0 lỗi và 0 bỏ qua, khoảng 4 phút
  38 giây. Chạy Release không build lại, lưu TRX và bật chẩn đoán crash/hang;
  không xảy ra crash. Các ca gồm component, ba host thật, mTLS, thu hồi,
  timeout/cancellation, replay và restart.
- Build/publish độc lập: copy đúng các thư mục nguồn mà Dockerfile Community
  cần vào thư mục tạm mới, bỏ `bin`/`obj`, rồi chạy
  `dotnet publish services/Community/SCDC.Community.Api/SCDC.Community.Api.csproj -c Release -o publish --ignore-failed-sources /p:UseAppHost=false`.
  Restore và publish đạt; cây đầu vào không có API cũ, BuildingBlocks hoặc
  implementation Identity/Messaging. Đầu ra có đủ năm assembly Community,
  không có `SCDC.Api.dll`, `SCDC.BuildingBlocks.dll`,
  `SCDC.Identity.Infrastructure.dll` hoặc `SCDC.Modules.Messaging.dll`.
- Compose mặc định hợp lệ. Overlay rollback chỉ hợp lệ khi có biến image;
  cấu hình kết quả dùng đúng image đã chỉ định và không còn khóa `build`.
- `make docs-check`: 130 file Markdown hiện hành, 2.967 liên kết cục bộ,
  0 lỗi. `git diff --check` đạt.

Hai lỗi ở lượt hồi quy đầu là test Identity còn gọi API tạo server và callback
Community vào host Messaging cũ. Fixture đã được sửa theo topology thực,
không khôi phục dependency production để làm test đạt. Bộ test mạng Community
còn xác nhận bearer header hoạt động và query token không cấp quyền REST.

Log cục bộ nằm tại `artifacts/community-source-isolation-2026-11-28/` (không
commit): `full-final/backend.trx`, `full-final/backend.log`,
`identity-network/backend.trx`, `minimal-source-final.json`, `compose.json`.
Database thử có hậu tố `_test`, chạy PostgreSQL/Redis thử riêng và được xóa
sau mỗi lượt; không dùng database dev.

## Giới hạn và vận hành

- Đây là kiểm chứng cục bộ, không phải kết quả workflow CI từ xa hay một lần
  deploy. Không chạy lại browser, frontend hoặc tải/telemetry toàn stack trong
  gói này; gateway và giao diện không đổi.
- Publish từ cây tối thiểu chứng minh ranh giới build; chưa build/chạy image
  container mới trong stack dev.
- Host Messaging mới từ chối `Hosting:CommunityEnabled=true`. Rollback
  cutover cần image host ghép cũ đã kiểm chứng, đặt bằng
  `SCDC_COMMUNITY_ROLLBACK_IMAGE`; overlay tắt build. Chưa chọn hoặc diễn tập
  runtime image rollback đó trong lần này. Gate dữ liệu và giới hạn rollback
  trước khi ghi trong [runbook](../../../../database/postgres/community/README.md)
  vẫn áp dụng.
- BuildingBlocks tiếp tục phục vụ Messaging legacy; việc dọn này không hoàn
  tất tách host/database Messaging, tích hợp Media hoặc các tính năng STEP-4.

Nguồn: [ranh giới nguồn](../../../system/backend.md#source-isolation),
[Community](../../../../services/Community/README.md),
[D42–D45](../../../system/backend.md#v1-shared-code).

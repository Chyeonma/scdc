# SCDC — Mốc nội bộ MVP và bàn giao sang v1

Cập nhật: 2026-10-13. Bản nguồn bàn giao: `900e226` — hợp nhất Community và Direct Messaging trong một API host Modular Monolith. Đây là snapshot nền phát triển để tiếp tục [v1](v1.md); trạng thái và bằng chứng dưới đây áp dụng theo revision được ghi nhận.

## Vai trò của mốc MVP

MVP là mốc làm việc và tích hợp nội bộ. **v1 là sản phẩm hoàn thiện đầu tiên**, có phạm vi và điều kiện bàn giao tại [hồ sơ v1](v1.md). Nền MVP gồm Identity, Community và Direct Messaging theo DEC-114/116; phân công theo DEC-117.

Theo định hướng được xác nhận ngày 2026-10-13, mốc MVP ghi nhận phần đã có, giới hạn kiểm chứng và công việc chuyển tiếp. Nhóm tập trung hoàn thiện tài liệu và triển khai v1 từ bản nguồn hiện tại; không cần hoàn tất một vòng chỉnh sửa hoặc nghiệm thu toàn MVP riêng trước khi bắt đầu v1. Phần còn thiếu của các luồng đã chọn tiếp tục trong v1 với quy tắc, UC/AC/TC hiện có.

Thiết kế trước code thực hiện [theo từng gói việc](../project/planning.md#package-design). Gói v1 bắt đầu khi đầu ra, phụ thuộc trực tiếp, thiết kế và cách kiểm chứng đủ rõ theo [workflow](../project/workflow.md#readiness). Các phần độc lập có thể tiếp tục trong khi những quyết định còn lại được hoàn thiện.

## Trạng thái nền bàn giao

| Phần | Hiện có tại `900e226` | Phần chuyển tiếp sang v1 |
|---|---|---|
| Identity | API đăng ký/xác minh, đăng nhập, hồ sơ, đổi/reset mật khẩu và quản lý phiên; luồng local dùng token Development | Hoàn thiện UI xác minh/reset, các gap tài khoản và email thật; kiểm chứng đầy đủ vòng đời theo [Accounts](../features/accounts/README.md) |
| Community | Tạo/xem/tìm server, join public/immediate, role/assignment; tạo/list/detail/sửa phòng text, ACL HTTP và guard transaction | Lịch sử/gửi/nhận tin phòng, Hub/reconnect và thu hồi quyền; approval/request, invitation, leave/transfer, quản lý server mở rộng, xóa phòng và voice theo [Community](../features/community/README.md) |
| Direct Messaging | Tìm người, mở hội thoại/inbox, gửi/lịch sử, retry chủ động, sửa/xóa; ChatHub/outbox và reconnect | Hồi quy nền đã có sau khi tách service; hoàn thiện bằng chứng quyền/đồng thời, thiết bị và tải theo [DM](../features/messaging/direct-messaging.md) và [hồ sơ DM](../records/verification/messaging/README.md) |
| Môi trường và công cụ | Compose chạy WebClient, API và PostgreSQL; SQL bootstrap, migration Community explicit, bộ test và volume khóa | Môi trường bốn service, tích hợp email worker, định tuyến/secrets/CI, migration runner, giám sát và công cụ vận hành theo [v1](v1.md) |
| Media | Chưa có runtime trong bản bàn giao; đã có [đặc tả mục tiêu](../features/media/README.md) | Gọi riêng, phòng thoại, camera/chia sẻ màn hình và kiểm chứng provider/quyền/quota/cutoff theo các gói v1 |

Email hoàn thiện được ghi nhận riêng tại `feat/identity@60f4db3`: resend/cooldown, consume có khóa, envelope Data Protection và worker Gmail SMTP. Phần này chưa tích hợp vào `900e226`; cần đối chiếu và đưa vào v1 theo [hiện trạng email](../features/accounts/email-links.md#status). Gửi email thật và nghiệm thu Accounts còn cần bằng chứng.

Các bảng trên là snapshot bàn giao nội bộ. Khi phát triển v1, cập nhật trạng thái tại tài liệu chủ đề và ghi kết quả mới trong `records/verification/` theo build; giữ nguyên phạm vi và kết quả của hồ sơ lịch sử.

## Bằng chứng hiện có

[Hồ sơ tích hợp ngày 2026-10-13](../records/verification/messaging/merge-2026-10-13.md) ghi kết quả trên bản hợp nhất, với PostgreSQL và database kiểm thử riêng:

| Kiểm tra | Kết quả đã ghi nhận |
|---|---|
| Build/test backend | 269 đạt, 0 lỗi, 0 bỏ qua |
| Node tests frontend | 77 đạt, 0 lỗi |
| Build frontend production | Đạt |
| Browser Community | 14 đạt |
| Browser tích hợp Community/DM | Mở hội thoại, gửi/nhận realtime giữa hai tài khoản, chuyển Community và reload lịch sử DM đạt |
| SQL bootstrap/seed và cấu hình Compose | Đạt trong phạm vi ghi tại hồ sơ tích hợp |

Đây là bằng chứng do agent thực thi trên môi trường ghi trong hồ sơ. Các gói Community và DM còn có [kết quả Community](../records/verification/community/README.md) và [kết quả DM](../records/verification/messaging/README.md) theo revision riêng; số test giữa các hồ sơ không cộng dồn. Kết quả DM không chứng minh tin phòng hoặc cơ chế xuyên service v1. Nghiệm thu sản phẩm thực hiện theo [điều kiện v1](v1.md#acceptance).

<a id="community-scope"></a>

## Đối chiếu phạm vi Community đã chọn

DEC-118 ngày 2026-10-07 chọn luồng tạo/tìm/tham gia → phòng text → lịch sử/gửi/nhận → xử lý mất quyền, giữ role/ACL đã có. Đây là căn cứ truy vết phần nền và phần chuyển tiếp:

| Nhóm luồng | UC liên quan | Trạng thái bàn giao |
|---|---|---|
| Tạo/tìm/xem và tham gia public/immediate | UC-COM-01/02; phần UC-COM-03/06 | Đã có nền API/UI và bằng chứng theo gói; các nhánh mở rộng tiếp tục trong v1 |
| Phòng text và quyền | Phần text UC-COM-16/17; UC-COM-18/20/21/22 | Đã có metadata/phòng/role/ACL HTTP và guard; lịch sử tin và tác động trên kết nối chat còn thiếu |
| Gửi/nhận và mất quyền | UC-COM-23; phần chat UC-COM-25 | Chuyển tiếp sang v1: writer tin phòng, lịch sử, Hub/reconnect và proof thu hồi trên kết nối thật |

Chi tiết hành vi và AC/TC giữ tại [các chủ đề Community](../features/community/README.md), [tin phòng](../features/messaging/channel-messaging.md) và [bảng công việc](../project/planning.md#community-work-items). Phần chưa hoàn thành vẫn thuộc phạm vi sản phẩm v1.

<a id="community-acceptance"></a>

### Các tiêu chí Community chuyển tiếp

Các mã COM-MVP giữ để đối chiếu tài liệu và hồ sơ cũ. Phần chưa chứng minh được chuyển sang kiểm chứng v1 theo UC/AC tương ứng; chúng không là điều kiện phải đóng toàn bộ trước khi bắt đầu một gói v1 độc lập.

| Mã tham chiếu | Nội dung cần kiểm chứng khi hoàn thiện v1 |
|---|---|
| COM-MVP-01 | Toàn luồng tạo/tìm/join/phòng/lịch sử/gửi/nhận trên DB/API/Hub thật; dữ liệu đã commit còn sau reload/restart |
| COM-MVP-02 | Tin/sequence/outbox nguyên tử; lost response, retry và đồng thời không tạo tin trùng; UI báo đã gửi sau xác nhận lưu |
| COM-MVP-03 | Phân trang/reconnect đủ tin đã commit, dedup và cursor đúng actor/space/quyền |
| COM-MVP-04 | Quyền/phiên/epoch hiện hành trên HTTP, ghi tin và subscribe/resume; kiểm chứng quyền tranh mutation tới commit |
| COM-MVP-05 | Kết nối chat ngừng truy cập trong ≤5 giây từ commit thu hồi theo DEC-083, gồm lỗi/restart và fail-close |
| COM-MVP-06 | Giao diện bỏ response/cache của actor mất quyền, retry đúng hợp đồng và hồi quy nền Community đã có |
| COM-MVP-07 | Kết quả gắn commit/build/schema/config/dataset/UC/AC/TC; ghi giới hạn, lỗi và người thực hiện/kiểm tra |

<a id="packages"></a>

## Bàn giao công việc sang v1

Mã gói MVP giữ làm đầu mối truy vết; phân công tiếp tục theo [DEC-117 và kế hoạch](../project/planning.md#team). Công việc còn lại được thiết kế và kiểm chứng trong các gói v1.

| Gói nền | Đầu mối | Phần tiếp tục trong v1 |
|---|---|---|
| MVP-ID | Vg; Thái phụ trách delivery/worker | Chuyển Identity, tích hợp email từ nhánh riêng, hoàn thiện vòng đời tài khoản và UI/AC còn thiếu |
| MVP-COM | Vg; Sáng phụ trách nền tin phòng | Chuyển Community/quyền, tích hợp tin phòng và hoàn thiện các nhánh Community còn lại |
| MVP-DM | Sáng | Chuyển persistence/hợp đồng/Hub/outbox của Messaging, giữ hành vi DM đã có và kiểm chứng lại các đường xuyên service |
| MVP-SYS | Thái; Vg/Sáng cung cấp migration và kỳ vọng nghiệp vụ | Phát triển Compose nhiều service, migration runner/bộ chạy, secrets, CI, giám sát và công cụ vận hành |

Các gói chuyển service V1-ARCH-01–03 và gói Media được định nghĩa tại [v1](v1.md). Mỗi người tự kiểm thử phần sở hữu và có người khác kiểm tra lại theo [workflow](../project/workflow.md#review).

## Source, dữ liệu và cách chạy lại

- Dùng `900e226` làm mốc đối chiếu nền hợp nhất; giữ lịch sử migration và các hồ sơ kiểm chứng liên quan khi phát triển tiếp.
- Stack local dùng [compose.yaml](../../compose.yaml). Đặt `COMMUNITY_OPERATION_KEY` bền trong `.env` theo [README](../../README.md#khởi-chạy), rồi chạy `make init-dm-key` và `docker compose up -d --build`; dùng cùng engine cho các thao tác container.
- Giữ volume PostgreSQL, keyring Identity/Community và khóa fingerprint DM cùng dữ liệu. Tài khoản seed không dùng đăng nhập; tạo tài khoản thử qua API theo [Development](../guides/development.md#identity).
- Database đã có dữ liệu cần đối chiếu migration/ledger của từng gói; hướng dẫn Community tại [migration explicit](../guides/community-development.md#migration), cấu hình và bằng chứng migration DM tại [hồ sơ DM](../records/verification/messaging/DM-MVP.md). `schema.sql` là bootstrap, không dùng để tạo lại database cần giữ dữ liệu.
- v1 [giữ dữ liệu MVP](../system/database.md#v1-mvp-data-transition) và [chuyển offline](../system/database.md#v1-mvp-data-cutover) theo DEC-169/170; mapping và bước chuyển được hoàn thiện trong gói tương ứng. Backup/restore thuộc [giai đoạn sau v1](v1.md#backup-scope).

<a id="acceptance"></a>

## Bắt đầu v1

Từ snapshot này, MVP được dùng làm nền phát triển nội bộ. Hồ sơ ghi nhận phần đã có và phần chưa hoàn thành, không xác nhận đã đạt toàn bộ phạm vi MVP từng đề xuất hoặc nghiệm thu sản phẩm v1.

Hoàn thiện [tài liệu tổng quan v1](v1.md) và bắt đầu các gói đủ đầu vào có thể thực hiện song song:

1. Chọn gói v1 có phụ thuộc trực tiếp đủ rõ, chốt đầu ra/thiết kế/cách thử và triển khai theo [quy trình](../project/workflow.md#process). Phần việc còn thiếu của MVP được xếp vào gói v1 tương ứng, không tạo thêm vòng hoàn thiện MVP riêng.
2. Giữ dữ liệu và ranh giới sở hữu; rà soát lại shared transaction/guard khi chuyển microservice, rồi hồi quy các luồng đã có.
3. Ghi bằng chứng mới theo build và áp dụng [điều kiện hoàn tất v1](v1.md#acceptance), [nghiệm thu/phát hành](acceptance.md) khi bàn giao sản phẩm hoàn thiện đầu tiên.

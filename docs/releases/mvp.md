# SCDC — Mốc MVP và bàn giao sang v1

MVP là mốc nội bộ: bản chụp phần nền đã làm, dùng làm điểm xuất phát cho [v1](v1.md). Trang này ghi những gì đã có, bằng chứng đã có, và việc còn chuyển sang v1.

**Bản chụp:** commit `900e226`, ngày 2026-10-13. Community và nhắn tin riêng (DM) đã được ghép vào một ứng dụng API duy nhất (modular monolith). Trạng thái và bằng chứng dưới đây chỉ đúng với bản này.

## MVP và v1 khác nhau thế nào

- **MVP** gồm Identity, Community và DM, dùng để làm việc và tích hợp trong nhóm.
- **v1** là sản phẩm hoàn thiện đầu tiên, có phạm vi và điều kiện bàn giao riêng tại [hồ sơ v1](v1.md).

Nhóm quyết định (2026-10-13) không làm thêm một vòng hoàn thiện hay nghiệm thu riêng cho MVP. Phần còn thiếu được làm tiếp trong các gói v1, dùng lại quy tắc, ca sử dụng và tiêu chí đã có. Mỗi gói v1 [được thiết kế trước khi code](../project/planning.md#package-design) và bắt đầu khi đủ [điều kiện](../project/workflow.md#readiness); các gói độc lập có thể chạy song song.

## Đã có và còn lại

| Phần | Đã có tại `900e226` | Chuyển sang v1 |
|---|---|---|
| Tài khoản | API đăng ký, xác minh, đăng nhập, hồ sơ, đổi và đặt lại mật khẩu, quản lý phiên. Chạy trên máy dùng token Development thay email | Giao diện xác minh và đặt lại mật khẩu, các phần còn thiếu, gửi email thật ([Tài khoản](../features/accounts/README.md)) |
| Cộng đồng | Tạo, xem, tìm server; tham gia server công khai; vai trò và gán vai trò; tạo, xem, sửa phòng chữ; ACL qua HTTP; guard trong transaction | Tin phòng (lịch sử, gửi, nhận), Hub, kết nối lại, thu hồi quyền; duyệt yêu cầu, lời mời, rời, chuyển chủ; xóa phòng; phòng thoại ([Cộng đồng](../features/community/README.md)) |
| Nhắn tin riêng | Tìm người, mở cuộc trò chuyện, hộp thư, gửi, lịch sử, gửi lại thủ công, sửa, xóa; ChatHub, outbox, kết nối lại | Chạy lại sau khi tách service; thêm bằng chứng về quyền, đồng thời, thiết bị và tải ([DM](../features/messaging/direct-messaging.md), [hồ sơ DM](../records/verification/messaging/README.md)) |
| Môi trường | Compose chạy WebClient, API, PostgreSQL; SQL khởi tạo; migration Community; bộ test; volume lưu khóa | Môi trường bốn service, worker email, định tuyến, bí mật, CI, công cụ migration, giám sát, công cụ vận hành |
| Media | Chưa có code; đã có [thiết kế](../features/media/README.md) | Gọi riêng, phòng thoại, camera, chia sẻ màn hình, kiểm thử với LiveKit |

**Email:** nhánh `feat/identity@60f4db3` đã có gửi lại email có thời gian chờ, dùng link có khóa, mã hóa bằng Data Protection và worker Gmail SMTP. Phần này chưa được ghép vào `900e226`; xem [hiện trạng email](../features/accounts/email-links.md#status).

Khi làm v1, cập nhật trạng thái trong tài liệu tính năng và ghi kết quả mới vào `records/verification/`. Không sửa trang này hay hồ sơ cũ.

## Bằng chứng

[Hồ sơ tích hợp 2026-10-13](../records/verification/messaging/merge-2026-10-13.md), chạy trên PostgreSQL với database thử riêng:

| Kiểm tra | Kết quả |
|---|---|
| Build và test backend | 269 đạt, 0 lỗi, 0 bỏ qua |
| Test Node frontend | 77 đạt, 0 lỗi |
| Build frontend production | Đạt |
| Test trình duyệt Community | 14 đạt |
| Test trình duyệt Community và DM | Đạt: mở cuộc trò chuyện, gửi nhận realtime giữa hai tài khoản, chuyển sang Community, tải lại lịch sử DM |
| SQL khởi tạo, dữ liệu mẫu, cấu hình Compose | Đạt trong phạm vi ghi ở hồ sơ |

Bằng chứng do agent chạy trên môi trường ghi trong hồ sơ. Hồ sơ [Community](../records/verification/community/README.md) và [DM](../records/verification/messaging/README.md) có kết quả riêng theo phiên bản của chúng; không cộng dồn số test giữa các hồ sơ. Kết quả DM không chứng minh tin phòng hay cơ chế giữa các service. Nghiệm thu sản phẩm theo [điều kiện v1](v1.md#acceptance).

<a id="community-scope"></a>

## Phạm vi Community đã chọn

Ngày 2026-10-07 nhóm chọn luồng: tạo, tìm, tham gia → phòng chữ → lịch sử, gửi, nhận → xử lý mất quyền; giữ vai trò và ACL đã làm (DEC-118).

| Nhóm luồng | Ca sử dụng | Trạng thái |
|---|---|---|
| Tạo, tìm, xem, tham gia server công khai | UC-COM-01, 02; một phần UC-COM-03, 06 | Đã có API, giao diện và bằng chứng; các nhánh mở rộng làm trong v1 |
| Phòng chữ và quyền | Phần phòng chữ của UC-COM-16, 17; UC-COM-18, 20, 21, 22 | Đã có thông tin phòng, vai trò, ACL qua HTTP và guard; còn thiếu lịch sử tin và tác động lên kết nối chat |
| Gửi, nhận và mất quyền | UC-COM-23; phần chat của UC-COM-25 | Chuyển sang v1: ghi tin phòng, lịch sử, Hub, kết nối lại và bằng chứng thu hồi trên kết nối thật |

Chi tiết xem [Community](../features/community/README.md), [tin phòng](../features/messaging/channel-messaging.md) và [bảng công việc](../project/planning.md#community-work-items). Phần chưa xong vẫn thuộc v1.

<a id="community-acceptance"></a>

### Tiêu chí Community chuyển sang v1

Các mã COM-MVP giữ lại để đối chiếu hồ sơ cũ. Chúng được kiểm thử trong v1 theo ca sử dụng tương ứng, và không chặn việc bắt đầu một gói v1 độc lập.

| Mã | Cần kiểm thử khi hoàn thiện v1 |
|---|---|
| COM-MVP-01 | Toàn luồng tạo, tìm, tham gia, vào phòng, xem lịch sử, gửi, nhận trên DB, API và Hub thật; dữ liệu còn sau khi tải lại hoặc khởi động lại |
| COM-MVP-02 | Tin, sequence và outbox ghi cùng lúc; mất phản hồi, gửi lại, gửi đồng thời không tạo tin trùng; giao diện chỉ báo đã gửi khi đã lưu |
| COM-MVP-03 | Phân trang và kết nối lại lấy đủ tin đã commit, không trùng, cursor đúng người, không gian và quyền |
| COM-MVP-04 | Kiểm tra quyền, phiên và epoch hiện tại khi gọi HTTP, ghi tin, đăng ký và bù tin; quyền giữ ổn định tới khi commit |
| COM-MVP-05 | Kết nối chat ngừng nhận trong 5 giây sau khi thu hồi được commit (DEC-083), kể cả khi lỗi hoặc khởi động lại; không xác nhận được quyền thì dừng |
| COM-MVP-06 | Giao diện bỏ phản hồi và dữ liệu đệm của người mất quyền; gửi lại đúng hợp đồng; các chức năng Community cũ vẫn chạy |
| COM-MVP-07 | Kết quả ghi rõ commit, build, schema, cấu hình, dữ liệu thử, mã UC, AC, TC; ghi giới hạn, lỗi, người chạy và người kiểm tra |

<a id="packages"></a>

## Các gói chuyển sang v1

Mã gói MVP giữ lại để truy vết. Bảng dưới là phân công lúc bàn giao (DEC-117); phân công hiện hành xem [kế hoạch](../project/planning.md#team).

| Gói | Người phụ trách lúc bàn giao | Làm tiếp trong v1 |
|---|---|---|
| MVP-ID | Vg; Thái làm worker gửi email | Chuyển Identity sang service, ghép email từ nhánh riêng, hoàn thiện vòng đời tài khoản và giao diện |
| MVP-COM | Vg; Sáng làm phần nền tin phòng | Chuyển Community và quyền sang service, ghép tin phòng, hoàn thiện các nhánh còn lại |
| MVP-DM | Sáng | Chuyển lưu trữ, hợp đồng, Hub và outbox của Messaging; giữ hành vi DM; kiểm thử lại các luồng giữa service |
| MVP-SYS | Thái; Vg và Sáng cung cấp migration và kết quả mong đợi | Compose nhiều service, công cụ migration, bí mật, CI, giám sát, công cụ vận hành |

Các gói trên là phân công giai đoạn MVP. Lộ trình v1 xem [kế hoạch](../project/planning.md#roadmap). Mỗi người tự kiểm thử phần mình và có người khác kiểm tra lại ([quy trình](../project/workflow.md#review)).

## Chạy lại bản MVP

- Dùng commit `900e226` làm mốc đối chiếu. Giữ lịch sử migration và hồ sơ kiểm thử khi phát triển tiếp.
- Chạy bằng [compose.yaml](../../compose.yaml): đặt `COMMUNITY_OPERATION_KEY` cố định trong `.env` ([README](../../README.md#khởi-chạy)), chạy `make init-dm-key`, rồi `docker compose up -d --build`. Dùng cùng một engine (Docker hoặc Podman) cho mọi lệnh container.
- Giữ volume PostgreSQL, key ring Identity và Community, khóa fingerprint DM cùng với dữ liệu.
- Tài khoản trong dữ liệu mẫu không dùng để đăng nhập; tạo tài khoản thử qua API ([hướng dẫn](../guides/development.md#identity)).
- `schema.sql` chỉ để khởi tạo mới, không dùng để tạo lại database đang có dữ liệu. Database đã có dữ liệu phải chạy migration theo từng gói: [Community](../guides/development.md#migration), [DM](../records/verification/messaging/DM-MVP.md).
- v1 [giữ dữ liệu MVP](../system/database.md#v1-mvp-data-transition) và [chuyển offline](../system/database.md#v1-mvp-data-cutover) (DEC-169, 170). Backup để [sau v1](v1.md#backup-scope).

<a id="acceptance"></a>

## Bắt đầu v1

Trang này ghi nhận phần đã có và phần chưa xong. Nó không xác nhận MVP đã đạt toàn bộ phạm vi từng đề xuất, cũng không phải nghiệm thu v1.

1. Chọn gói v1 có phụ thuộc đã rõ; chốt đầu ra, thiết kế, cách kiểm thử rồi làm theo [quy trình](../project/workflow.md#process). Việc còn thiếu của MVP xếp vào gói v1 tương ứng.
2. Giữ dữ liệu và ranh giới sở hữu. Xem lại transaction và guard dùng chung khi tách service, rồi chạy lại các luồng đã có.
3. Ghi bằng chứng mới theo build. Khi bàn giao, áp dụng [điều kiện hoàn tất v1](v1.md#acceptance) và [nghiệm thu](acceptance.md).

---

Nguồn: DEC-114, DEC-116 (MVP một ứng dụng, v1 microservice), DEC-117 (phân công lúc bàn giao), DEC-118 (phạm vi Community).

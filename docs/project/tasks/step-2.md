# STEP-2 — Tách ranh giới ngay trong monolith

Thẻ Trello của [STEP-2](../planning.md#step-2). Cách đồng bộ xem [README](README.md).

**Trạng thái:** đã công bố để đồng bộ Trello ngày 2026-11-22. S2-01 đến S2-05 hoàn tất; S2-06 đến S2-12 chưa làm. Bỏ bước review theo chỉ đạo của người dùng; vẫn kiểm tra bằng build, test và CI.

S2-01 hoàn tất theo [xác nhận ngày 2026-11-17](../../records/planning/progress-2026-11-17.md); S2-02 xong thiết kế, S2-03 đã vào `main` tại `9d6d375`, CI đạt 299 backend và 77 frontend. [Tổng kết S1-09](../../records/verification/environment/s1-09-2026-11-22.md#step-2-estimate) ghi ước lượng sơ bộ 20,5–35 ngày công tại thời điểm trước khi làm S2-04/S2-05. Còn khoảng 18,5–31 ngày công theo các task chưa làm. Bảng dưới giữ snapshot trước triển khai.

## Hiện trạng (snapshot 2026-11-14, xác minh schema ngày 2026-11-15)

Không đồng bộ; là đầu vào cho các thẻ bên dưới.

| Loại chỗ dính | Số lượng | Ở đâu |
|---|---|---|
| Một connection string chung | 1 | `ConnectionStrings:Database` trong `SCDC.Api/Program.cs` và `CommunityModule.cs`; Identity/Messaging cùng dùng kết nối qua `ISharedDatabaseSession` |
| Khóa ngoại khác schema | 32 | Community → `identity.users` (8), Community → `messaging.spaces` (1), Messaging → `identity.users` (16), `moderation` → identity, community, messaging (7). Nguồn: `database/postgres/schema.sql`, migration `001` |
| View đọc chéo schema | 2 | `messaging.v_space_overview` (đọc identity, community), `messaging.v_message_timeline` (đọc identity); code không dùng |
| Phụ thuộc transaction xuyên module | 4 hợp đồng nghiệp vụ và 1 nhóm persistence | `IAccountAccessGuard` (Community gọi ở 3 file), `IAccountPairAccessGuard` dùng ngầm `ISharedDatabaseSession` (Messaging gọi ở 6 file), `IChatSpaceLifecycle` (Community gọi khi tạo phòng), `IChannelAccessGuard` (wrapper đã cài, chưa có caller dùng interface); nhóm persistence còn có `ISharedDatabaseTransaction` |
| Hợp đồng đọc trực tiếp | 4 | `IUserDirectory` (Community: vai trò), `IHistoricalUserSummaryReader` (Messaging: 3 file), `IAuthenticatedSessionReader` (ChatHub, outbox, đăng ký realtime), `IUserSearchDirectory` (chỉ API dùng) |
| Hợp đồng chưa ai dùng | 2 | `IChannelAccessChecker`, `IRealtimeAccessRevoker` |
| Outbox dùng chung | 1 bảng, 3 owner | Identity và Messaging map EF, Community dùng `TransactionalOutbox`; Messaging dispatcher chỉ nhận `Messaging.MessageChanged` |
| Audit đang hoạt động | 1 bảng | `audit.security_events` được Identity ghi, catalog có 37 dòng; không phải schema chưa dùng |
| Moderation chưa dùng | 2 bảng | Không có caller; catalog có 0 dòng ở `message_reports` và `actions`; preflight lại trước khi loại bảng |

Code của từng module không có SQL đọc thẳng bảng của module khác; chỗ dính nằm ở schema, view và các hợp đồng trên.

## S2-01 · Chốt cách xử lý từng chỗ dính
- Nhãn: docs, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: bảng trong [database](../../system/database.md#s2-01-boundaries) ghi, cho từng khóa ngoại, view và hợp đồng ở bảng hiện trạng: bỏ, giữ dạng ID không ràng buộc, thay bằng bản sao dữ liệu, hay thay bằng lời gọi. Quyết định số phận schema `moderation`, bảo toàn audit đang được Identity dùng và hai hợp đồng chưa ai dùng. Ghi rõ owner của từng nhóm sự kiện trong `integration.outbox_events`.
- Xong khi: mỗi dòng trong bảng hiện trạng có cách xử lý và người làm; đã đối chiếu đủ phạm vi Messaging.
- Phụ thuộc: không; làm được ngay trong lúc làm STEP-1.
- Kết quả: **Hoàn tất — 2026-11-17**, theo xác nhận tiến độ của người dùng. [Hồ sơ ngày 2026-11-15](../../records/verification/architecture/s2-01-2026-11-15.md) bao phủ 32 FK, 2 view, 11 nhóm hợp đồng và dữ liệu phụ trợ.

## S2-02 · Thiết kế thay transaction chung
- Nhãn: docs, tech
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: thiết kế cho bốn hợp đồng đang nhận transaction (`IAccountAccessGuard`, `IAccountPairAccessGuard`, `IChatSpaceLifecycle`, `IChannelAccessGuard`) theo hàng rào ghi (D05–D07) và outbox (D27): ai giữ quyền, kiểm tra lúc nào, xử lý khi bên kia lỗi hoặc chậm, gọi lặp không gây hại. Cập nhật [thu hồi truy cập](../../system/access-revocation.md) và [cơ chế Community](../../features/community/mechanics.md#transactions).
- Xong khi: có ví dụ luồng cho gửi DM, đổi vai trò, tạo phòng; hợp đồng và các tình huống lỗi được kiểm tra nhất quán.
- Phụ thuộc: S1-01, S2-01; giữ gRPC/mTLS theo D04/D20, chưa có quyết định hoãn.
- Kết quả: **Hoàn tất thiết kế — 2026-11-20**. [Hồ sơ S2-02](../../records/verification/architecture/s2-02-2026-11-20.md) đối chiếu phạm vi/quyền, permit/fence, điều phối và Redis, ba luồng DM/đổi vai trò/tạo phòng, hợp đồng Protobuf/REST và 22 ca kiểm thử cần triển khai. Chưa đổi runtime; bỏ điều kiện chờ review theo chỉ đạo ngày 2026-11-22.

## S2-03 · Database user và connection string riêng cho mỗi module
- Nhãn: tech, refactor
- Người làm: Vg · Review: Thái
- Đầu ra: ba connection string (Identity, Community, Messaging) thay cho `ConnectionStrings:Database`; ba user PostgreSQL. Theo S2-01, tách outbox theo owner, chuyển audit Identity cùng dữ liệu, bố trí hàm kỹ thuật và ledger theo owner. Giai đoạn đầu cấp thêm quyền đọc chéo tạm thời, ghi rõ danh sách; S2-11 thu hẹp lại.
- Bổ sung S2-02: tạo bảng `access_fences` và `fence_receipts` trong từng schema owner, dùng khóa scope chuẩn hóa và epoch không lùi.
- Xong khi: ứng dụng và test chạy với ba user; đối chiếu đủ event ID, trạng thái công việc và audit trước/sau; Compose và [hướng dẫn phát triển](../../guides/development.md) đã cập nhật.
- Phụ thuộc: S2-01, S2-02.
- Kết quả: **Hoàn tất — 2026-11-20; đã vào main ngày 2026-11-22.** Commit `9d6d375`; [CI GitHub đạt](https://github.com/Chyeonma/scdc/actions/runs/45001598412) 299 backend và 77 frontend. [Hồ sơ S2-03](../../records/verification/architecture/s2-03-2026-11-20.md) ghi migration, quyền tạm và smoke Compose. Bỏ bước review theo chỉ đạo; volume dev hiện có chưa được nâng cấp.

## S2-04 · Bỏ khóa ngoại khác schema của Community
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: migration SQL (D49) bỏ 8 khóa ngoại sang `identity.users` và khóa ngoại `community.channels` → `messaging.spaces`. Code xử lý được ID người dùng không còn tồn tại (xem [vòng đời dữ liệu](../../system/data-lifecycle.md)).
- Xong khi: migration chạy được trên database thử có dữ liệu MVP; test Community qua.
- Phụ thuộc: S2-01, S2-03 (nền ledger và user riêng).
- Kết quả: **Hoàn tất triển khai — 2026-11-22**. [Hồ sơ S2-04/S2-05](../../records/verification/architecture/s2-04-s2-05-2026-11-22.md): migration bảo toàn dữ liệu, bootstrap và CLI cập nhật, 317/317 test backend đạt. Volume dev hiện có chưa được nâng cấp.

## S2-05 · Bỏ khóa ngoại và view khác schema của Messaging
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: migration bỏ 16 khóa ngoại sang `identity.users` và hai view đọc chéo; bỏ 7 FK của `moderation`, preflight và loại bảng rỗng theo S2-01. Nếu Moderation có dữ liệu, dừng để duyệt cách bảo toàn trước khi loại bảng.
- Xong khi: migration chạy được trên dữ liệu MVP; test DM qua.
- Phụ thuộc: S2-01, S2-03 (nền ledger và user riêng).
- Kết quả: **Hoàn tất triển khai — 2026-11-22**. [Hồ sơ S2-04/S2-05](../../records/verification/architecture/s2-04-s2-05-2026-11-22.md): migration bảo toàn dữ liệu, bootstrap và CLI cập nhật, 317/317 test backend đạt. Volume dev hiện có chưa được nâng cấp.

## S2-06 · Phiên kiểm tra qua Redis
- Nhãn: tech
- Người làm: Vg · Review: Sáng
- Đầu ra: Identity công bố phiên vào Redis (D30, D31); bản thay thế `IAuthenticatedSessionReader` đọc Redis thay vì database Identity. Redis vào Compose.
- Bổ sung S2-02: Identity cấp permit theo mục đích, lưu tiến trình thu hồi/ACK, phối hợp deny Redis trước final commit; bootstrap và version/generation chống phục hồi phiên cũ.
- Xong khi: ChatHub, outbox và đăng ký realtime dùng bản mới; thu hồi phiên vẫn ngắt kết nối DM; test mất Redis cho kết quả đúng theo thiết kế.
- Phụ thuộc: S1-06, S2-02, S2-03.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 5–8 ngày công.

## S2-07 · Community không dùng transaction của Identity
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `MembershipService`, `ServerService`, `CommunityManagementGuard` kiểm tra tài khoản theo thiết kế S2-02, không truyền `DbTransaction` sang Identity. `RoleService` lấy tên người dùng qua hợp đồng không phụ thuộc database chung.
- Bổ sung S2-02: Community lưu policy dự kiến và ACK; tính phạm vi quyền hiệu lực, actor membership header và revalidation trước final commit; cập nhật client tương ứng.
- Xong khi: test Community qua, kể cả các ca tài khoản bị khóa giữa chừng.
- Phụ thuộc: S2-02, S2-03, S2-06.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 3–5 ngày công.

## S2-08 · Messaging không dùng transaction của Identity
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: 6 file Messaging đang dùng `IAccountPairAccessGuard` và `ISharedDatabaseSession` (gửi, sửa, lịch sử, hộp thư, mở hội thoại, ConversationAccess) chuyển sang thiết kế S2-02. `IHistoricalUserSummaryReader` không còn đọc qua kết nối chung.
- Xong khi: toàn bộ test DM và Playwright `dm-mvp` qua.
- Phụ thuộc: S2-02, S2-03, S2-06.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 3–5 ngày công.

## S2-09 · Tạo phòng không dùng transaction chung
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `ChannelService` không gọi `IChatSpaceLifecycle` trong transaction của Community; dùng luồng chuẩn bị, xác nhận, hủy của D14–D19 (dạng chạy trong một ứng dụng).
- Xong khi: tạo phòng chạy đúng; các ca mất phản hồi, gửi lại, hủy giữa chừng có test.
- Phụ thuộc: S2-02, S2-03, S2-04, S2-05, S2-06.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 3–5 ngày công.

## S2-10 · Dọn hợp đồng giữa module
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `SCDC.Contracts` không còn `DbTransaction`, `ISharedDatabaseSession`; các lời gọi có timeout và gọi lặp không gây hại; bỏ hoặc giữ hợp đồng chưa dùng theo S2-01.
- Xong khi: build qua; không còn chỗ nào trong code truyền transaction qua ranh giới module.
- Phụ thuộc: S2-07, S2-08, S2-09.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 1–2 ngày công.

## S2-11 · Test tình huống lỗi và thu hẹp quyền database
- Nhãn: tech
- Người làm: Sáng · Review: Vg
- Đầu ra: test khi bên kia không trả lời, sự kiện đến hai lần, sai thứ tự, mất Redis; đo thời gian thu hồi quyền DM (yêu cầu 5 giây). Bỏ các quyền đọc chéo tạm thời của S2-03.
- Xong khi: mỗi user database chỉ có quyền trên schema của mình mà test vẫn qua; có số đo thời gian thu hồi.
- Phụ thuộc: S2-10.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 3–5 ngày công.

## S2-12 · Demo và tổng kết STEP-2
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo DM và Community chạy với ba user database tách quyền; ghi bằng chứng vào cột "Kết quả" của [lộ trình](../planning.md#roadmap); ước lượng STEP-3.
- Xong khi: đạt tiêu chí xong của [STEP-2](../planning.md#step-2).
- Phụ thuộc: tất cả thẻ trên.
- Trạng thái: **Chưa làm**. Ước lượng sơ bộ: 0,5–1 ngày công.

# STEP-2 — Tách ranh giới ngay trong monolith

Thẻ Trello của [STEP-2](../planning.md#step-2). Cách đồng bộ xem [README](README.md).

Trạng thái: nháp

Chưa đồng bộ lên Trello. Rà lại ở buổi S1-09, cập nhật theo kết quả S1-01 (giao thức giữa service) và S1-06 (thư viện JWT), rồi xóa dòng "Trạng thái: nháp" để đưa thẻ lên. Chưa ước lượng: ước lượng dựa trên thời gian thực tế của STEP-1.

## Hiện trạng (khảo sát code ngày 2026-11-11)

Không đồng bộ; là đầu vào cho các thẻ bên dưới.

| Loại chỗ dính | Số lượng | Ở đâu |
|---|---|---|
| Một connection string chung | 1 | `ConnectionStrings:Database` dùng cho cả ba module (`SCDC.Api/Program.cs`, `IdentityModule.cs`, `CommunityModule.cs`) |
| Khóa ngoại khác schema | 32 | Community → `identity.users` (8), Community → `messaging.spaces` (1), Messaging → `identity.users` (16), `moderation` → identity, community, messaging (7). Nguồn: `database/postgres/schema.sql`, migration `001` |
| View đọc chéo schema | 2 | `messaging.v_space_overview` (đọc identity, community), `messaging.v_message_timeline` (đọc identity); code không dùng |
| Hợp đồng nhận transaction của bên gọi | 4 | `IAccountAccessGuard` (Community gọi ở 3 file), `IAccountPairAccessGuard` và `ISharedDatabaseSession` (Messaging gọi ở 6 file), `IChatSpaceLifecycle` (Community gọi khi tạo phòng), `IChannelAccessGuard` (đã cài, chưa có bên gọi) |
| Hợp đồng đọc trực tiếp | 4 | `IUserDirectory` (Community: vai trò), `IHistoricalUserSummaryReader` (Messaging: 3 file), `IAuthenticatedSessionReader` (ChatHub, outbox, đăng ký realtime), `IUserSearchDirectory` (chỉ API dùng) |
| Hợp đồng chưa ai dùng | 2 | `IChannelAccessChecker`, `IRealtimeAccessRevoker` |

Code của từng module không có SQL đọc thẳng bảng của module khác; chỗ dính nằm ở schema, view và các hợp đồng trên.

## S2-01 · Chốt cách xử lý từng chỗ dính
- Nhãn: docs, tech
- Người làm: Vg · Review: Sáng
- Đầu ra: bảng trong [database](../../system/database.md) ghi, cho từng khóa ngoại, view và hợp đồng ở bảng hiện trạng: bỏ, giữ dạng ID không ràng buộc, thay bằng bản sao dữ liệu, hay thay bằng lời gọi. Quyết định số phận schema `moderation`, `audit` (chưa có code dùng) và hai hợp đồng chưa ai dùng. Ghi rõ bảng `integration.outbox_events` thuộc module nào.
- Xong khi: mỗi dòng trong bảng hiện trạng có cách xử lý và người làm; Sáng đồng ý phần Messaging.
- Phụ thuộc: không; làm được ngay trong lúc làm STEP-1.

## S2-02 · Thiết kế thay transaction chung
- Nhãn: docs, tech
- Người làm: Vg · Review: Sáng, Thái
- Đầu ra: thiết kế cho bốn hợp đồng đang nhận transaction (`IAccountAccessGuard`, `IAccountPairAccessGuard`, `IChatSpaceLifecycle`, `IChannelAccessGuard`) theo hàng rào ghi (D05–D07) và outbox (D27): ai giữ quyền, kiểm tra lúc nào, xử lý khi bên kia lỗi hoặc chậm, gọi lặp không gây hại. Cập nhật [thu hồi truy cập](../../system/access-revocation.md) và [cơ chế Community](../../features/community/mechanics.md#transactions).
- Xong khi: có ví dụ luồng cho gửi DM, đổi vai trò, tạo phòng; cả nhóm đọc và hiểu được.
- Phụ thuộc: S1-01 (gọi giữa module bằng gì).

## S2-03 · Database user và connection string riêng cho mỗi module
- Nhãn: tech, refactor
- Người làm: Vg · Review: Thái
- Đầu ra: ba connection string (Identity, Community, Messaging) thay cho `ConnectionStrings:Database`; ba user PostgreSQL. Giai đoạn đầu cấp thêm quyền đọc chéo tạm thời, ghi rõ danh sách; S2-11 thu hẹp lại.
- Xong khi: ứng dụng và test chạy với ba user; Compose và [hướng dẫn phát triển](../../guides/development.md) đã cập nhật.
- Phụ thuộc: S2-01.

## S2-04 · Bỏ khóa ngoại khác schema của Community
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: migration SQL (D49) bỏ 8 khóa ngoại sang `identity.users` và khóa ngoại `community.channels` → `messaging.spaces`. Code xử lý được ID người dùng không còn tồn tại (xem [vòng đời dữ liệu](../../system/data-lifecycle.md)).
- Xong khi: migration chạy được trên database thử có dữ liệu MVP; test Community qua.
- Phụ thuộc: S2-01.

## S2-05 · Bỏ khóa ngoại và view khác schema của Messaging
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: migration bỏ 16 khóa ngoại sang `identity.users`; bỏ hoặc chuyển hai view đọc chéo; xử lý các khóa ngoại của schema `moderation` theo S2-01.
- Xong khi: migration chạy được trên dữ liệu MVP; test DM qua.
- Phụ thuộc: S2-01.

## S2-06 · Phiên kiểm tra qua Redis
- Nhãn: tech
- Người làm: Vg · Review: Sáng
- Đầu ra: Identity công bố phiên vào Redis (D30, D31); bản thay thế `IAuthenticatedSessionReader` đọc Redis thay vì database Identity. Redis vào Compose.
- Xong khi: ChatHub, outbox và đăng ký realtime dùng bản mới; thu hồi phiên vẫn ngắt kết nối DM; test mất Redis cho kết quả đúng theo thiết kế.
- Phụ thuộc: S1-06, S2-02.

## S2-07 · Community không dùng transaction của Identity
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `MembershipService`, `ServerService`, `CommunityManagementGuard` kiểm tra tài khoản theo thiết kế S2-02, không truyền `DbTransaction` sang Identity. `RoleService` lấy tên người dùng qua hợp đồng không phụ thuộc database chung.
- Xong khi: test Community qua, kể cả các ca tài khoản bị khóa giữa chừng.
- Phụ thuộc: S2-02.

## S2-08 · Messaging không dùng transaction của Identity
- Nhãn: refactor
- Người làm: Sáng · Review: Vg
- Đầu ra: 6 file Messaging đang dùng `IAccountPairAccessGuard` và `ISharedDatabaseSession` (gửi, sửa, lịch sử, hộp thư, mở hội thoại, ConversationAccess) chuyển sang thiết kế S2-02. `IHistoricalUserSummaryReader` không còn đọc qua kết nối chung.
- Xong khi: toàn bộ test DM và Playwright `dm-mvp` qua.
- Phụ thuộc: S2-02, S2-06.

## S2-09 · Tạo phòng không dùng transaction chung
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `ChannelService` không gọi `IChatSpaceLifecycle` trong transaction của Community; dùng luồng chuẩn bị, xác nhận, hủy của D14–D19 (dạng chạy trong một ứng dụng).
- Xong khi: tạo phòng chạy đúng; các ca mất phản hồi, gửi lại, hủy giữa chừng có test.
- Phụ thuộc: S2-02, S2-04.

## S2-10 · Dọn hợp đồng giữa module
- Nhãn: refactor
- Người làm: Vg · Review: Sáng
- Đầu ra: `SCDC.Contracts` không còn `DbTransaction`, `ISharedDatabaseSession`; các lời gọi có timeout và gọi lặp không gây hại; bỏ hoặc giữ hợp đồng chưa dùng theo S2-01.
- Xong khi: build qua; không còn chỗ nào trong code truyền transaction qua ranh giới module.
- Phụ thuộc: S2-07, S2-08, S2-09.

## S2-11 · Test tình huống lỗi và thu hẹp quyền database
- Nhãn: tech
- Người làm: Sáng · Review: Vg
- Đầu ra: test khi bên kia không trả lời, sự kiện đến hai lần, sai thứ tự, mất Redis; đo thời gian thu hồi quyền DM (yêu cầu 5 giây). Bỏ các quyền đọc chéo tạm thời của S2-03.
- Xong khi: mỗi user database chỉ có quyền trên schema của mình mà test vẫn qua; có số đo thời gian thu hồi.
- Phụ thuộc: S2-10.

## S2-12 · Demo và tổng kết STEP-2
- Nhãn: docs
- Người làm: Vg · Cả nhóm tham gia
- Đầu ra: demo DM và Community chạy với ba user database tách quyền; ghi bằng chứng vào cột "Kết quả" của [lộ trình](../planning.md#roadmap); ước lượng STEP-3.
- Xong khi: đạt tiêu chí xong của [STEP-2](../planning.md#step-2).
- Phụ thuộc: tất cả thẻ trên.

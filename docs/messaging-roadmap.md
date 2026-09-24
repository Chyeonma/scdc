# SCDC — Roadmap phát triển, trọng tâm Messaging

Ngày rà soát source: **18/09/2026**. Đây là kế hoạch triển khai đề xuất dựa trên repository hiện tại, không phải danh sách chức năng đã hoàn thành.

## 1. Phạm vi và hiện trạng

SCDC hiện hướng tới mạng xã hội cộng đồng có server, channel, DM và chat nhóm, theo mô hình modular monolith. Roadmap tập trung vào **nhắn tin từ backend đến giao diện**, chỉ phân rã Identity, Community và Moderation ở mức cần thiết để hỗ trợ Messaging. News feed, bài đăng, kết bạn/follow, voice/video call và mã hóa đầu cuối chưa nằm trong phạm vi này.

**Cách hiểu phạm vi:** tất cả phase đều phục vụ việc hoàn thiện Messaging, nhưng không phải mọi task chỉ sửa module Messaging. P4-T02 là dependency Community; P8-T03 thuộc Moderation; phần session/email thuộc Identity và P9 là kiểm thử/vận hành cho bản chat. Chỉ làm phần liên quan đến chat, không mở rộng thành roadmap toàn bộ mạng xã hội.

| Thành phần | Hiện trạng quan sát trong source | Phần cần làm tiếp |
|---|---|---|
| Nền tảng backend | .NET 10, module registration, `Result<T>`, `ProblemDetails`, Swagger, health | Dùng lại khi xây vertical slice |
| Identity | Có backend đăng ký, xác minh email, login, refresh, logout, profile, session, password lifecycle và test | Tích hợp tài khoản thật với chat; xử lý session realtime; email delivery trước phát hành công khai |
| Community | Mới có module foundation và schema | Server, thành viên, channel, quyền và thu hồi quyền |
| Messaging backend | `MessagingModule` chỉ đăng ký descriptor; chưa có API nghiệp vụ, persistence hay Hub | Triển khai toàn bộ use case chat |
| Messaging database | Đã có bảng, constraint và index cho nhiều chức năng | Mapping, transaction, authorization và kiểm thử; có bảng không đồng nghĩa đã có tính năng |
| WebClient | Có UI DM/channel, composer, message, thread, pin, reaction, report; phần lớn dùng mock/local state | Nối API thật và quản lý loading/error/retry |
| Realtime | Client đã dùng SignalR, gọi `/hubs/chat` và `SubscribeChannel`; backend chưa map Hub | Chốt contract, triển khai Hub, outbox, reconnect và đồng bộ dữ liệu |
| Hạ tầng | Compose có API, React/Nginx, PostgreSQL; Vite/Nginx đã có proxy `/hubs` | Kiểm thử WebSocket thực tế; bổ sung object storage ở phase attachment |
| Kiểm thử | Có test nền tảng response và Identity; chưa có test Messaging | Bổ sung theo từng slice, không dồn đến cuối |

### Nguồn đối chiếu

- [README](../README.md), [kiến trúc](architecture/overview.md), [Identity v1](identity/identity-v1.md), [quy ước lỗi](api/error-handling.md).
- [Messaging module](../services/Modules/Messaging/MessagingModule.cs), [Community module](../services/Modules/Community/CommunityModule.cs), [API host](../services/SCDC.Api/Program.cs).
- [Schema PostgreSQL](../database/postgres/schema.sql), [hướng dẫn database](../database/postgres/README.md).
- [IUserDirectory](../services/SCDC.Contracts/Identity/IUserDirectory.cs), [IChannelAccessChecker](../services/SCDC.Contracts/Community/IChannelAccessChecker.cs), [IRealtimeAccessRevoker](../services/SCDC.Contracts/Messaging/IRealtimeAccessRevoker.cs).
- [App.jsx](../clients/WebClient/src/App.jsx), [API client](../clients/WebClient/src/api.js), [MessageComposer](../clients/WebClient/src/components/MessageComposer.jsx), [các component](../clients/WebClient/src/components).

### Khoảng trống cần xử lý sớm trên frontend

- Gửi tin hiện thêm message giả vào local state, gọi `/channels/{id}/messages` rồi bỏ qua lỗi bằng `.catch(() => {})`; chưa có xác nhận lưu thành công.
- ID tin tạm và `clientMessageId` chưa liên kết để hợp nhất phản hồi HTTP với event realtime, có nguy cơ hiển thị trùng.
- Khi SignalR kết nối lỗi, client vẫn đặt trạng thái `online`; reconnect chưa subscribe lại và tải bù.
- Sửa/xóa, reaction, pin, thread và tìm kiếm hiện chủ yếu xử lý trên dữ liệu cục bộ.
- Composer mới tạo metadata file; chưa upload file thật. Reply/attachment chưa được truyền đầy đủ trong request gửi tin hiện tại.

## 2. Cách sử dụng backlog

- **Phase** là mốc bàn giao; **task** là chức năng/use case; checkbox có ID là **subtask** có thể đưa vào issue tracker.
- Checkbox `[x]` chỉ phần việc đã được kiểm tra theo đầu ra của task; `[ ]` là phần chưa nghiệm thu. Task tài liệu hoàn tất không có nghĩa backend tương ứng đã triển khai. Những nền tảng đã có được ghi ở mục 1, không tính lại là việc chưa làm.
- Nhãn: **BE** backend, **FE** frontend, **DB** database, **QA** kiểm thử, **OPS** vận hành.
- Ưu tiên: **P0** cần cho chat cơ bản; **P1** hoàn thiện trải nghiệm; **P2** mở rộng sau bản cơ bản.
- Mỗi task đi xuyên suốt: contract → domain rule → application handler → persistence → endpoint → UI liên quan → test. Không tạo toàn bộ CRUD rỗng trước.
- Mỗi task đi kèm Git flow tại mục 17: tự tạo hoặc tiếp tục branch của task, commit, push và mở/cập nhật PR khi được giao triển khai. Các subtask mặc định dùng chung branch của task.
- Chủ sở hữu, thời lượng và ngày hoàn thành để team bổ sung khi biết nhân lực; thứ tự dưới đây thể hiện dependency, không phải cam kết lịch.

## 3. Tổng hợp các phase và mốc bàn giao

| Phase | Trọng tâm | Ưu tiên | Phụ thuộc | Đầu ra |
|---|---|---|---|---|
| P0 | Chốt rule, contract và điều kiện tích hợp | P0 | Nền tảng hiện có | Backlog và contract có thể triển khai |
| P1 | DM và danh sách hội thoại | P0 | P0, Identity | Hai user thật mở được cùng một DM |
| P2 | Gửi tin và lịch sử | P0 | P1 | DM lưu PostgreSQL, refresh không mất tin |
| P3 | Outbox và realtime | P0 | P2 | Hai trình duyệt nhận tin, reconnect tải bù |
| P4 | Chat nhóm và server channel | P0 cho bản cộng đồng | P3; nhánh channel cần Community | Ba loại không gian dùng chung luồng message |
| P5 | Read state, unread, typing và tùy chỉnh hội thoại | P1 | P3; áp dụng cho P4 khi có | Chat dùng được hằng ngày, đồng bộ nhiều thiết bị |
| P6 | Sửa/xóa, reply/thread, reaction, pin, mention | P1 | P3; quyền mở rộng từ P4 | Tương tác với message thật |
| P7 | File, ảnh và attachment | P1 | P2–P3, object storage | Upload/download có kiểm soát quyền |
| P8 | Tìm kiếm, block và report/moderation | P1/P2 | P4, P6; Moderation cho report | Tra cứu và xử lý nội dung vi phạm |
| P9 | Kiểm thử phát hành và vận hành | P0 trước phát hành | Các phase thuộc bản phát hành | Bản release có số đo và quy trình vận hành |

**Mốc A — MVP DM nội bộ:** P0 → P1 → P2 → P3, kèm các kiểm tra P9 phù hợp phạm vi. Không cần đợi hoàn tất Community để làm DM.

**Mốc B — MVP cộng đồng:** Mốc A + P4 + P5 + sửa/xóa cơ bản của P6, hoàn tất kiểm tra quyền và vận hành tương ứng.

**Mốc C — Messaging đầy đủ:** Mốc B + phần còn lại của P6, P7, P8 và cổng nghiệm thu P9. Có thể đổi thứ tự attachment và tương tác theo ưu tiên sản phẩm.

Nhánh Community có thể được phát triển đồng thời với P1–P3 sau khi chốt contract P0. Các tiêu chí bảo mật, transaction và test là yêu cầu của từng phase; P9 là kiểm tra tổng thể trước phát hành.

## 4. P0 — Chốt nghiệp vụ và chuẩn tích hợp

### P0-T01 — Chính sách nhắn tin [BE, FE]

Đầu ra ngày 19/09/2026: [Chính sách Messaging v1](messaging/policies.md), gồm ma trận quyền, rule từ chối và 18 ca nghiệm thu thiết kế. Nội dung task đã hoàn tất ở mức tài liệu; trạng thái Git/PR theo báo cáo bàn giao, chưa merge.

- [x] **P0-T01.1** Chốt enum nghiệp vụ khớp SQL: `space_type` 1=DM, 2=group, 3=channel; xác định ý nghĩa status, role và message type còn lại.
- [x] **P0-T01.2** Chốt ai được mở DM, gửi tin, xem lịch sử; mặc định đề xuất không cho tự nhắn và mỗi cặp user chỉ có một DM.
- [x] **P0-T01.3** Chốt lịch sử khi vào/rời/được thêm lại nhóm; đề xuất bản đầu cho thành viên hiện tại xem toàn bộ lịch sử, thành viên đã rời không truy cập được.
- [x] **P0-T01.4** Chốt giới hạn nội dung theo SQL: text sau trim dài 1–10.000 ký tự; quy tắc attachment-only và system message; client không tự tạo system message.
- [x] **P0-T01.5** Chốt quyền sửa/xóa/pin, policy block, lưu lịch sử chỉnh sửa và thời hạn lưu dữ liệu; ghi rõ các quyết định thay đổi schema nếu có.

**Nghiệm thu:** có ma trận actor × loại space × hành động và rule xử lý trường hợp bị từ chối.

### P0-T02 — Contract API, dữ liệu và sự kiện [BE, FE, DB]

Đầu ra ngày 19/09/2026: [Contract Messaging v1](messaging/contracts.md). Hoàn tất ở mức thiết kế API/DTO/cursor/event và contract liên module; các mở rộng được ghi rõ chưa triển khai. PR nối tiếp P0-T01 khi dependency chưa merge.

- [x] **P0-T02.1** Chốt route dùng chung `/api/v1/spaces/{spaceId}/messages` cho DM/group/channel; cập nhật client đang gọi `/channels/...` khi triển khai P2.
- [x] **P0-T02.2** Chốt `MessageDto`, `SpaceSummaryDto`, cursor, request gửi tin và error code theo `Result<T>`/`ProblemDetails` hiện có.
- [x] **P0-T02.3** Truyền `sequenceNo` kiểu chuỗi thập phân qua JSON để tránh mất chính xác `bigint` trên JavaScript; chốt cách so sánh ở FE.
- [x] **P0-T02.4** Chốt event envelope gồm `eventId`, `eventType`, `spaceId`, `occurredAt`, payload và version khi cần; phân biệt event ID với message ID.
- [x] **P0-T02.5** Chốt ownership: Identity cung cấp user/session; Community cung cấp channel permission; Messaging sở hữu message và membership DM/group.
- [x] **P0-T02.6** Đánh giá contract còn thiếu: cấp chat space cho channel, quyền quản lý tin, session bị thu hồi. Interface hiện tại chỉ có `CanRead`/`CanSend` và revoke theo user/space.

**Nghiệm thu:** có ví dụ request/response/event, không để FE suy đoán DTO từ mock; mọi mở rộng contract được ghi rõ là đề xuất mới.

### P0-T03 — Điều kiện chạy và dữ liệu kiểm thử [DB, QA]

Đầu ra ngày 19/09/2026: [Fixture Messaging v1](messaging/testing-fixtures.md), `appsettings.Testing.json` và `compose.test.yaml`. Hoàn tất điều kiện test riêng và thiết kế actor/migration; runtime test Messaging bắt đầu từ P1/P2. PR nối tiếp P0-T02 khi dependency chưa merge.

- [x] **P0-T03.1** Chuẩn bị tài khoản thật qua Identity và fixture database riêng cho test chat; seed password/token hiện chỉ để minh họa.
- [x] **P0-T03.2** Chốt cách cập nhật SQL cho database đã có dữ liệu. Không chạy lại `schema.sql` để nâng cấp vì script có `DROP SCHEMA`.
- [x] **P0-T03.3** Chuẩn bị tình huống test hai người dùng và user thứ ba không có quyền; thêm người thứ ba vào nhóm ở P4.

**Nghiệm thu:** có thể lặp lại kịch bản test độc lập mà không ảnh hưởng dữ liệu đang sử dụng.

## 5. P1 — DM và danh sách hội thoại

### P1-T01 — Tạo hoặc lấy DM [BE, DB]

Đầu ra ngày 19/09/2026: vertical slice DM với `POST /api/v1/conversations/direct`, `GET /api/v1/spaces/{spaceId}`, persistence Messaging và integration test A/B/C. Runtime test chờ PostgreSQL test container theo P0-T03.

- [x] **P1-T01.1** Thêm domain/application/persistence tối thiểu cho `spaces`, `direct_conversations`, `space_members`, `space_user_states` khi slice cần.
- [x] **P1-T01.2** Tra cứu người nhận qua `IUserDirectory`; kiểm tra tồn tại và rule P0; lấy người gửi từ identity đã xác thực.
- [x] **P1-T01.3** Chuẩn hóa cặp UUID theo thứ tự tương thích PostgreSQL và unique constraint `(user_low_id, user_high_id)`.
- [x] **P1-T01.4** Tạo space, direct conversation và hai membership trong một transaction; xử lý hai request đồng thời bằng trả cùng DM, không để space mồ côi.
- [x] **P1-T01.5** Mở endpoint tạo/lấy DM và truy vấn chi tiết có kiểm tra thành viên.

**Nghiệm thu:** A mở DM với B và B mở với A nhận cùng `spaceId`; C không đọc được; không tạo DM với chính mình.

### P1-T02 — Inbox và chọn người trò chuyện [BE, FE]

- [x] **P1-T02.1** API danh sách hội thoại có phân trang, user summary, last message và last activity; chỉ trả không gian user có quyền.
- [x] **P1-T02.2** Dùng `IUserDirectory.FindByIdsAsync` để lấy tác giả/người nhận theo batch, tránh gọi một lần cho mỗi dòng.
- [x] **P1-T02.3** Tích hợp `CreateDmModal`, `SubSidebar`, `ChatHeader`; tra cứu chính xác username qua API adapter được bảo vệ, chưa cần xây tìm kiếm toàn bộ user.
- [x] **P1-T02.4** Bỏ ID giả ở luồng DM; có loading, empty, error, retry và xử lý hội thoại vừa bị thu hồi quyền.

**Nghiệm thu:** đăng nhập bằng tài khoản thật, mở DM và tải lại vẫn thấy đúng hội thoại; không hiện hội thoại của user khác.

## 6. P2 — Gửi và đọc tin nhắn bền vững

### P2-T01 — Gửi text, transaction và idempotency [BE, DB]

- [x] **P2-T01.1** Map `messages`; validate nội dung, loại tin và trạng thái space; kiểm tra quyền ở server cho từng request.
- [x] **P2-T01.2** Bắt buộc `clientMessageId` cho message từ user, dùng unique index `(space_id, author_user_id, client_message_id)`.
- [x] **P2-T01.3** Retry cùng ID và cùng payload trả lại message đã lưu; cùng ID khác payload trả conflict theo contract.
- [x] **P2-T01.4** Cùng transaction: ghi message, cập nhật last-message projection và ghi `integration.outbox_events`; P3 mới thực hiện phát event.
- [x] **P2-T01.5** Chốt cơ chế tuần tự hóa ghi trong cùng space trước khi cấp sequence, ví dụ khóa hàng space; test transaction commit lệch thứ tự để cursor không bỏ sót tin.
- [x] **P2-T01.6** Giới hạn tần suất/kích thước request, trả lỗi có `errorCode`; không nhận `authorUserId` từ client làm danh tính gửi.

**Nghiệm thu:** gửi đồng thời/retry không tạo tin trùng; rollback không để lại message hoặc outbox riêng lẻ; last message đúng khi gửi đồng thời.

### P2-T02 — History và cursor [BE, DB, FE]

- [x] **P2-T02.1** API lấy lịch sử theo `beforeSequence`, có `limit` tối đa và cursor trang tiếp theo; tận dụng index `(space_id, sequence_no DESC)`.
- [x] **P2-T02.2** Bổ sung chế độ tải bù `afterSequence` có phân trang, chốt quy tắc không dùng đồng thời before/after.
- [x] **P2-T02.3** Kiểm tra quyền trước truy vấn; xác định tombstone cho tin đã xóa; không trả nội dung đã xóa trong DTO thường.
- [x] **P2-T02.4** FE tải trang đầu, cuộn lên tải thêm, giữ vị trí cuộn và hủy/bỏ kết quả request cũ khi đổi space.
- [x] **P2-T02.5** Kiểm thử khoảng trống sequence, page boundary và gửi tin trong khi đang tải lịch sử.

**Nghiệm thu:** lịch sử sắp xếp ổn định, không trùng hoặc bỏ sót tin trong các kịch bản trên; refresh vẫn có dữ liệu.

### P2-T03 — Composer thật và trạng thái gửi [FE, QA]

- [x] **P2-T03.1** Một message đang soạn có một `clientMessageId` cố định cho mọi lần retry; bỏ cách sinh ID khác nhau cho tin tạm/request.
- [x] **P2-T03.2** Hiển thị pending/sent/failed, retry và giữ nội dung khi lỗi; trạng thái sent nghĩa là server đã lưu, không đồng nghĩa người nhận đã đọc.
- [x] **P2-T03.3** Hợp nhất tin tạm với DTO server; không dùng `Date.now()` làm thứ tự tin đã xác nhận.
- [x] **P2-T03.4** Bỏ cơ chế nuốt lỗi gửi tin; hiện thông báo từ `ProblemDetails`; tạm ẩn/vô hiệu hóa attachment/reply ở luồng thật đến khi slice tương ứng hoàn thành.
- [x] **P2-T03.5** Tách state gọi API/message khỏi `App.jsx` ở mức cần thiết; giữ mock chỉ cho demo tách biệt, không trộn vào hội thoại thật.

**Nghiệm thu:** API lỗi không hiện tin như đã gửi thành công; mất phản hồi sau commit rồi retry vẫn chỉ có một tin.

## 7. P3 — Realtime và phát sự kiện tin cậy

### P3-T01 — SignalR Hub và quyền subscribe [BE, FE]

- [x] **P3-T01.1** Đăng ký SignalR và map `/hubs/chat`; cấu hình xác thực token cho transport được dùng, giới hạn nhận token từ query vào đúng đường dẫn Hub.
- [x] **P3-T01.2** Chốt `SubscribeSpace`/`UnsubscribeSpace` và cập nhật client đang dùng `SubscribeChannel`; kiểm tra quyền trước khi join group.
- [x] **P3-T01.3** Quản lý user/session/connection cho nhiều tab, thiết bị; có luồng cập nhật inbox khi user chưa mở hội thoại.
- [x] **P3-T01.4** Triển khai `IRealtimeAccessRevoker`; bổ sung contract session nếu cần để logout/revoke session đóng đúng kết nối và ngăn subscribe lại.
- [x] **P3-T01.5** Ràng buộc gửi event với quyền hiện hành; test race revoke/broadcast và event cũ còn chờ outbox, không chỉ kiểm tra quyền lúc join.

**Nghiệm thu:** user ngoài space không nhận payload; token/session hết hiệu lực và quyền bị thu hồi được xử lý theo policy đã chốt.

### P3-T02 — Outbox worker và chống xử lý lặp [BE, DB, OPS]

- [x] **P3-T02.1** Worker đọc event tới hạn theo batch; chốt transaction/lock hoặc claim có phục hồi khi nhiều worker cùng chạy.
- [x] **P3-T02.2** Phát `MessageCreated`; chỉ đánh dấu published sau khi bước phát hoàn tất; ghi attempt, lỗi và thời điểm retry.
- [x] **P3-T02.3** Có backoff, ngưỡng retry và cơ chế xem/replay event lỗi; đề xuất bổ sung schema nếu cần trạng thái cách ly hoặc lease.
- [x] **P3-T02.4** Quy định giao nhận ít nhất một lần: consumer có side effect dùng inbox/idempotency; FE deduplicate bằng event/message ID và version.
- [x] **P3-T02.5** Tách dispatch theo event type để không nuốt event Identity; chưa bật gửi email thật cho đến khi có consumer email tương ứng.
- [x] **P3-T02.6** Test crash trước phát, sau phát nhưng trước đánh dấu, restart worker và event đến lệch thứ tự.

**Nghiệm thu:** lỗi phát không làm mất dữ liệu đã lưu; retry không nhân đôi UI/side effect. Published chỉ xác nhận đã xử lý phát, không bảo đảm mọi client đã nhận.

### P3-T03 — Reconnect và đồng bộ UI [FE, QA]

- [x] **P3-T03.1** Kết nối lại phải subscribe lại, refresh token khi cần và hiển thị đúng connecting/online/offline.
- [x] **P3-T03.2** Chốt thứ tự subscribe + tải snapshot/tải bù và buffer event trong lúc đồng bộ để không có khoảng trống dữ liệu.
- [x] **P3-T03.3** Hợp nhất HTTP response, history và event theo ID; sắp xếp bằng sequence, không dựa vào thời điểm event tới.
- [x] **P3-T03.4** Tải bù tin mới có phân trang; tải lại snapshot phần đang hiển thị để đồng bộ sửa/xóa vì `afterSequence` đơn thuần không bao phủ thay đổi tin cũ.
- [ ] **P3-T03.5** Kiểm thử hai trình duyệt, nhiều tab, rớt mạng, API restart và chuyển space liên tục; kiểm tra cả proxy Vite/Nginx (đã có kiểm thử tự động cursor/snapshot và production build Vite; kiểm thử thủ công đa trình duyệt/API restart còn cần chạy trên môi trường tích hợp).

**Nghiệm thu:** người nhận thấy tin không cần refresh; nối lại không mất/trùng tin và không hiển thị online giả khi Hub lỗi.

## 8. P4 — Chat nhóm và server channel

### P4-T01 — Vòng đời nhóm [BE, DB, FE]

- [x] **P4-T01.1** Tạo group cùng space và membership trong transaction; validate tên, số thành viên, user trùng/không tồn tại.
- [x] **P4-T01.2** Thêm/xóa thành viên, rời nhóm, đổi tên/avatar metadata và chuyển chủ nhóm; không để nhóm hoạt động thiếu owner hợp lệ.
- [x] **P4-T01.3** Chốt quyền owner/admin/member và tranh chấp hai thao tác thành viên đồng thời; giới hạn nhóm theo `max_members`.
- [x] **P4-T01.4** Áp dụng quyền vào gửi/history/subscribe; revoke mọi kết nối liên quan khi rời hoặc bị xóa khỏi nhóm.
- [x] **P4-T01.5** Nối UI tạo nhóm, danh sách thành viên và cài đặt; phát event membership và system message nếu đã chốt ở P0 (UI nhận `SpaceUpdated`; system message chưa được chốt payload/UX riêng nên không tự tạo nội dung hệ thống).

**Nghiệm thu:** ba tài khoản chat nhóm thật; người bị xóa không đọc/gửi/nhận tin mới; chính sách xem lịch sử khớp P0.

### P4-T02 — Community tối thiểu để chat channel [BE, DB, FE]

- [x] **P4-T02.1** Community triển khai tạo/list server, join qua invite, list member và tạo/list channel.
- [x] **P4-T02.2** Chốt role/permission và override; triển khai `IChannelAccessChecker.CheckAsync(userId, spaceId)` đúng rule quyền đọc/gửi.
- [x] **P4-T02.3** Thêm contract cấp chat space loại channel qua Messaging; phối hợp tạo `community.channels` và space có transaction hoặc cơ chế bù/retry rõ ràng. Space được cấp trước qua `IChannelSpaceProvisioner`; nếu ghi channel thất bại thì space được retire ngay, tránh resource mồ côi.
- [x] **P4-T02.4** Xử lý leave/kick/ban, đổi quyền, archive/delete channel; gọi revoke theo contract và ngăn truy cập HTTP tương ứng.
- [x] **P4-T02.5** Nối `ServerRail`, `SubSidebar`, modal server/channel và member list với API thật.

**Nghiệm thu:** channel gắn đúng space; người không có quyền không thể đọc tin bằng cách đoán ID; tạo lỗi không để resource mồ côi.

### P4-T03 — Dùng chung luồng message cho ba loại space [BE, FE, QA]

- [x] **P4-T03.1** Phân nhánh authorization: DM/group theo membership Messaging, channel qua Community; không suy quyền channel từ `space_members`. `SpaceMessageAccess` áp dụng cùng quyết định đọc/gửi cho HTTP và subscribe.
- [x] **P4-T03.2** Dùng lại send/history/outbox/realtime; không sao chép ba bộ xử lý tin nhắn riêng. WebClient dùng một composer/sender/history cho ba loại space.
- [x] **P4-T03.3** Test ma trận quyền DM/group/channel; FE phản ánh read-only nhưng backend vẫn là nơi quyết định quyền. Test bao phủ private/read-only/archived, override, revoke và outbox.

**Nghiệm thu:** một contract message thống nhất hoạt động ở cả ba loại space và kiểm thử được các trường hợp mất quyền.

## 9. P5 — Read state, unread và trải nghiệm hội thoại

### P5-T01 — Đã đọc và bộ đếm chưa đọc [BE, DB, FE]

- [x] **P5-T01.1** Upsert `space_user_states.last_read_sequence` chỉ tăng; kiểm tra mốc đọc thuộc space và không vượt phạm vi user được đọc.
- [x] **P5-T01.2** FE chỉ gửi mốc khi nội dung đã hiển thị theo policy, không tự mark read cho tab ẩn; gom request cập nhật để tránh spam.
- [x] **P5-T01.3** Đếm unread bằng số message thỏa điều kiện sau mốc đọc; không lấy `lastSequence - lastReadSequence` vì sequence là toàn cục và có khoảng trống.
- [x] **P5-T01.4** Chốt loại trừ tin của chính mình, system message, tin đã xóa và thread reply; đồng bộ badge nhiều tab/thiết bị.
- [x] **P5-T01.5** Nếu cần delivered/read chính xác từng tin, bổ sung client acknowledgment qua `receipts`; không coi thao tác broadcast là delivered. P5-T01 chỉ lưu mốc đọc theo space do client chủ động xác nhận; chưa hiển thị delivered/read từng tin nên chưa cần `receipts`.

**Nghiệm thu:** đọc trên một thiết bị cập nhật thiết bị khác; request cũ không kéo lùi mốc; bộ đếm đúng khi các space gửi xen kẽ.

### P5-T02 — Typing, tùy chỉnh inbox và thông báo trong ứng dụng [BE, FE]

- [x] **P5-T02.1** Typing start/stop có kiểm tra quyền, throttle và tự hết hạn; không lưu mỗi keystroke vào DB/outbox.
- [x] **P5-T02.2** API mute, `notification_level`, hide và pin hội thoại qua `space_user_states`; phân biệt pin hội thoại với pin message.
- [x] **P5-T02.3** Chốt hội thoại ẩn có xuất hiện lại khi nhận tin mới không; mute chỉ ảnh hưởng thông báo, không tự đánh dấu đã đọc.
- [x] **P5-T02.4** Thêm badge/thông báo trong ứng dụng theo preference và quyền; push/email notification là backlog riêng nếu cần sau này.

**Nghiệm thu:** typing tự biến mất khi mất mạng; tùy chỉnh tồn tại sau refresh và không làm sai unread.

## 10. P6 — Tương tác với tin nhắn

### P6-T01 — Sửa và xóa [BE, DB, FE]

- [ ] **P6-T01.1** API sửa có kiểm tra tác giả/quyền, nội dung và expected version; xung đột trả `409` theo contract.
- [ ] **P6-T01.2** Ghi `message_edits`, tăng version, cập nhật `edited_at` và outbox trong cùng transaction.
- [ ] **P6-T01.3** Xóa mềm bằng `deleted_at`/`deleted_by_user_id`; chốt tombstone, last-message preview, pin, reply preview và attachment sau xóa.
- [ ] **P6-T01.4** Phát `MessageUpdated`/`MessageDeleted`; FE không cho event version cũ ghi đè trạng thái mới.
- [ ] **P6-T01.5** Lịch sử chỉnh sửa chỉ mở cho actor được phép; không lộ nội dung đã xóa qua search, event hay preview.

**Nghiệm thu:** không sửa/xóa tin người khác trái quyền; cập nhật đồng thời được xử lý rõ ràng và các màn hình nhất quán sau reconnect.

### P6-T02 — Reply và thread [BE, DB, FE]

- [ ] **P6-T02.1** Gửi `replyToMessageId`/`threadRootId`; xác minh message cùng space và quyền đọc; tận dụng composite FK hiện có.
- [ ] **P6-T02.2** Chốt thread một cấp, root hợp lệ, policy root bị xóa; API list reply có cursor.
- [ ] **P6-T02.3** Xác định thread reply có xuất hiện trong timeline chính/unread/last message không; tính thread count từ dữ liệu thật hoặc projection được cập nhật an toàn.
- [ ] **P6-T02.4** Nối reply composer, `ThreadPanel`, preview và jump-to-message; tải vùng lịch sử quanh tin đích nếu chưa có trong client.

**Nghiệm thu:** không reply xuyên space; thread tải lại còn dữ liệu; root bị xóa không làm hỏng panel.

### P6-T03 — Reaction và pin message [BE, DB, FE]

- [ ] **P6-T03.1** API thêm/bỏ reaction idempotent theo `(message_id, user_id, reaction_key)`; chuẩn hóa và giới hạn reaction key.
- [ ] **P6-T03.2** Trả count/userReacted từ server, phát event cập nhật và hoàn nguyên UI khi request lỗi.
- [ ] **P6-T03.3** API pin/unpin/list pin có quyền riêng; mở rộng contract Community vì `CanSend` không mặc nhiên là quyền pin/xóa tin của người khác.
- [ ] **P6-T03.4** Nối `MessageItem` và `PinnedPanel`, triển khai jump-to-message đang để trống.

**Nghiệm thu:** request lặp không tăng count sai; người thiếu quyền không pin được; tin đã xóa không lộ nội dung trong pinned panel.

### P6-T04 — Mention người dùng [BE, DB, FE]

- [ ] **P6-T04.1** Chốt cú pháp và structured payload mention, lưu `messaging.mentions`; không tin danh sách ID client gửi mà không validate.
- [ ] **P6-T04.2** Gợi ý người dùng trong phạm vi được phép; không gửi nội dung thông báo cho người không có quyền đọc space.
- [ ] **P6-T04.3** Đồng bộ mention khi sửa/xóa; xử lý notification idempotent và tôn trọng mute/notification level.
- [ ] **P6-T04.4** Render highlight và badge; role mention, `@everyone` để backlog riêng vì schema hiện chỉ có user mention.

**Nghiệm thu:** mention không bị nhân đôi khi retry và không trở thành đường làm lộ hội thoại riêng.

## 11. P7 — Attachment, ảnh và file

### P7-T01 — Upload và metadata [BE, DB, OPS]

- [ ] **P7-T01.1** Chọn/cấu hình MinIO hoặc S3-compatible storage; thêm cấu hình local và secret theo môi trường. Compose hiện chưa có storage service.
- [ ] **P7-T01.2** Thiết kế upload-init/upload/complete hoặc upload qua API; nếu upload trước gửi tin, cần upload session/staging vì `attachments.message_id` hiện bắt buộc.
- [ ] **P7-T01.3** Chốt giới hạn số file/dung lượng/loại file; xác minh kích thước, checksum và định dạng ở server, không chỉ dựa MIME client.
- [ ] **P7-T01.4** Gắn file vào message có kiểm tra ownership và quyền gửi; hỗ trợ attachment-only theo message type đã thống nhất.
- [ ] **P7-T01.5** Quét/cách ly file theo `scan_status`; dọn upload bỏ dở và file mồ côi, xử lý retry complete/gửi tin không tạo bản sao.

**Nghiệm thu:** file thật nằm trong storage, DB chỉ lưu metadata; file của user khác không thể bị gắn vào tin tùy ý.

### P7-T02 — Hiển thị và tải xuống [BE, FE, QA]

- [ ] **P7-T02.1** API download kiểm tra quyền và trạng thái file/message; URL ký có thời hạn nếu dùng, xác định cửa sổ hiệu lực khi quyền bị thu hồi.
- [ ] **P7-T02.2** Composer giữ file thật để upload, hiển thị progress/cancel/retry; phân biệt upload thành công và gửi message thành công.
- [ ] **P7-T02.3** Preview ảnh, filename, size và download; render tên file an toàn, xử lý file lỗi/đang quét/bị chặn.
- [ ] **P7-T02.4** Test mất mạng, file quá lớn, MIME giả, gửi lại cùng file và user mất quyền sau upload.

**Nghiệm thu:** gửi file giữa hai tài khoản và tải được sau refresh; không thể tải file chỉ bằng cách đoán attachment ID.

## 12. P8 — Tìm kiếm, block và moderation

### P8-T01 — Tìm kiếm lịch sử [BE, DB, FE]

- [ ] **P8-T01.1** API search theo space, nội dung, tác giả và khoảng thời gian; phân trang và giới hạn truy vấn.
- [ ] **P8-T01.2** Dùng `search_vector`/GIN đã có; đánh giá tìm kiếm tiếng Việt có/không dấu với cấu hình `simple` hiện tại trước khi chọn mở rộng.
- [ ] **P8-T01.3** Filter quyền và soft delete trước khi trả kết quả; không tìm chỉ trên tập message FE đã tải như hiện tại.
- [ ] **P8-T01.4** FE debounce, loading/empty/error và jump-to-message; test sau sửa/xóa và sau thu hồi quyền.

**Nghiệm thu:** tìm được tin cũ chưa tải vào trình duyệt, không trả kết quả ngoài quyền.

### P8-T02 — Chặn người dùng [BE, DB, FE]

- [ ] **P8-T02.1** API block/unblock/list block qua `user_blocks`, idempotent và không cho tự block.
- [ ] **P8-T02.2** Áp dụng policy P0: đề xuất chặn DM mới/gửi DM khi một bên block; lịch sử cũ và hành vi trong nhóm/channel phải được quyết định riêng.
- [ ] **P8-T02.3** Áp dụng rule vào HTTP, realtime, invite nhóm và notification phù hợp phạm vi; cập nhật UI khi trạng thái block thay đổi.
- [ ] **P8-T02.4** Test gửi đồng thời với block và unblock, tránh chỉ vô hiệu hóa nút trên giao diện.

**Nghiệm thu:** không vượt qua block bằng gọi API trực tiếp; hành vi nhóm/channel khớp policy công bố.

### P8-T03 — Report và xử lý tin vi phạm [BE, FE, QA]

- [ ] **P8-T03.1** Tạo module Moderation theo ranh giới kiến trúc hiện có; triển khai report qua `moderation.message_reports` và nối `ReportModal`.
- [ ] **P8-T03.2** Chỉ cho report tin được phép xem; chốt reason, chống spam/lặp và tránh log nội dung nhạy cảm không cần thiết.
- [ ] **P8-T03.3** Queue review và hành động moderator qua contract Messaging/Community; không truy cập implementation hoặc ghi thẳng bảng của module khác.
- [ ] **P8-T03.4** Ghi action/audit, phát event xóa/ẩn/thu hồi quyền; bổ sung trạng thái/schema nếu chọn hành vi ẩn khác với soft delete hiện tại.

**Nghiệm thu:** report tồn tại thật, moderator xử lý đúng phạm vi quyền, có dấu vết quyết định và UI được đồng bộ.

## 13. P9 — Kiểm thử phát hành và vận hành

### P9-T01 — Kiểm thử xuyên suốt [QA, BE, FE]

- [ ] **P9-T01.1** Bổ sung test Messaging trong `tests/SCDC.Api.Tests` với PostgreSQL thật: constraint, mapping, rollback, idempotency, cursor và concurrency.
- [ ] **P9-T01.2** Thiết lập test frontend/E2E phù hợp; `package.json` hiện chưa có script test. Bao phủ login → DM → gửi → nhận → refresh → reconnect.
- [ ] **P9-T01.3** Với phạm vi đã phát hành, bao phủ group/channel, quyền bị thu hồi, sửa/xóa, unread, file và report tương ứng.
- [ ] **P9-T01.4** Kiểm tra truy cập chéo space, XSS khi render content/filename, rate limit, session hết hạn và log không chứa token/nội dung chat mặc định.
- [ ] **P9-T01.5** Kiểm tra dependency module: chỉ qua Contracts/BuildingBlocks, không có reference trực tiếp Identity ↔ Community ↔ Messaging.

**Nghiệm thu:** toàn bộ kịch bản bắt buộc của mốc release đạt; lỗi mất tin, lộ tin và nhân đôi dữ liệu phải được xử lý trước bàn giao.

### P9-T02 — Hiệu năng và quan sát [BE, DB, OPS]

- [ ] **P9-T02.1** Chốt tải mục tiêu: số kết nối đồng thời, tin/giây, kích thước history và ngưỡng p95; ghi rõ môi trường đo trước khi nghiệm thu.
- [ ] **P9-T02.2** Đo send/history/search/unread và outbox lag; kiểm tra query plan, N+1 và index theo dữ liệu đại diện.
- [ ] **P9-T02.3** Metrics/log cho số kết nối, lỗi gửi, duplicate retry, độ trễ dispatch và event quá số lần retry; liên kết bằng trace/event/message ID.
- [ ] **P9-T02.4** Dashboard/cảnh báo và runbook khi outbox dồn, PostgreSQL/storage lỗi hoặc reconnect tăng đột biến.
- [ ] **P9-T02.5** Bắt đầu với một API instance; trước khi scale nhiều instance, thiết kế fan-out SignalR, connection revocation và trạng thái typing xuyên instance.

**Nghiệm thu:** có báo cáo so với ngưỡng tải đã chốt, xác định được nút thắt và xử lý được event lỗi mà không sửa DB thủ công tùy tiện.

### P9-T03 — Đóng gói và bàn giao [OPS, BE, FE]

- [ ] **P9-T03.1** Cập nhật Swagger, hướng dẫn chạy, cấu hình storage/worker, SQL nâng cấp không phá dữ liệu và bước rollback phù hợp.
- [ ] **P9-T03.2** Diễn tập backup/restore PostgreSQL và file; chốt retention cho message edit, attachment và outbox đã xử lý.
- [ ] **P9-T03.3** Kiểm tra bản Compose qua Nginx, WebSocket, health/readiness và cấu hình secrets của môi trường triển khai.
- [ ] **P9-T03.4** Trước phát hành công khai: hoàn thiện email delivery/Identity cần thiết, loại bỏ dữ liệu demo khỏi luồng thật và chạy smoke test với user mới.

**Nghiệm thu:** người khác có thể dựng môi trường, chạy kịch bản chat và phục hồi sự cố theo tài liệu bàn giao.

## 14. API và realtime contract đề xuất

Các route/event sau là **đề xuất để chốt ở P0**, chưa có trong backend. Prefix HTTP: `/api/v1`.

| Chức năng | Route hoặc thao tác đề xuất | Phase |
|---|---|---|
| Mở DM | `POST /conversations/direct` với `recipientUserId` | P1 |
| Inbox/chi tiết | `GET /spaces`, `GET /spaces/{spaceId}` | P1 |
| Gửi tin | `POST /spaces/{spaceId}/messages` | P2 |
| Lịch sử/tải bù | `GET /spaces/{spaceId}/messages?beforeSequence=...&limit=...` hoặc `afterSequence` | P2 |
| Realtime | `/hubs/chat`: `SubscribeSpace`, `UnsubscribeSpace` | P3 |
| Nhóm | `POST /conversations/group`, `PATCH /conversations/group/{spaceId}` và API membership theo rule | P4 |
| Đọc/tùy chỉnh | `PUT /spaces/{spaceId}/read-state`, `PATCH /spaces/{spaceId}/preferences` | P5 |
| Sửa/xóa | `PATCH` / `DELETE /spaces/{spaceId}/messages/{messageId}` | P6 |
| Thread | `GET /spaces/{spaceId}/messages/{messageId}/thread`; gửi bằng endpoint chung có `threadRootId` | P6 |
| Reaction | `PUT` / `DELETE /spaces/{spaceId}/messages/{messageId}/reactions/{reactionKey}` | P6 |
| Pin | `GET /spaces/{spaceId}/pins`; `PUT` / `DELETE /spaces/{spaceId}/pins/{messageId}` | P6 |
| File | Upload session/complete và download attachment; chốt route sau quyết định storage | P7 |
| Search | `GET /spaces/{spaceId}/messages/search` | P8 |
| Block | `GET /users/me/blocks`; `PUT` / `DELETE /users/me/blocks/{userId}` | P8 |
| Report | `POST /message-reports` | P8 |

Event cốt lõi: `MessageCreated`, `MessageUpdated`, `MessageDeleted`. Event mở rộng dự kiến: `ReadStateUpdated`, `ReactionChanged`, `PinChanged`, `MembershipChanged`, `TypingChanged`, `SpaceUpdated`. Tên và payload cần thống nhất với FE; typing là dữ liệu tạm thời, không cần outbox bền vững.

## 15. Những điểm kỹ thuật cần giữ xuyên suốt

1. **SQL là source of truth:** tận dụng constraint/index hiện có; bổ sung script nâng cấp và tài liệu khi schema thay đổi, không tự bật EF migration trái quy ước project.
2. **Không vượt ranh giới module:** Messaging dùng `IUserDirectory` và `IChannelAccessChecker`; phối hợp tạo channel/space và moderation bằng contract rõ ràng.
3. **Sequence không phải bộ đếm unread:** `sequence_no` là toàn cục, có khoảng trống; không giả định transaction commit đúng thứ tự cấp sequence nếu chưa có cơ chế bảo đảm.
4. **Quyền kiểm tra ở mọi đường đọc/ghi:** gồm history, search, thread, pin, file, HTTP, subscribe và dispatch sau thu hồi quyền.
5. **Persist trước, realtime sau:** message và outbox cùng transaction; event có thể lặp; client phải tải lại/tải bù được từ database.
6. **Schema chưa bao phủ mọi policy:** staging upload, retry cách ly, session revoke, quyền quản lý tin và notification bền vững có thể cần contract/schema mới; không coi là sẵn có.

## 16. Definition of Done cho mỗi task

- [ ] Rule và phạm vi quyền rõ ràng; có lỗi nghiệp vụ nhất quán với API hiện tại.
- [ ] Luồng backend chạy với PostgreSQL thật; transaction/concurrency/idempotency được kiểm tra nếu có ghi dữ liệu.
- [ ] FE dùng API thật cho phần đã bàn giao; có loading/empty/error/retry thích hợp.
- [ ] Có test happy path, input sai và thiếu quyền; thêm test race/retry/reconnect cho luồng có rủi ro đó.
- [ ] Contract, Swagger và tài liệu thay đổi được cập nhật; không ghi nhận hoàn thành chỉ vì đã có bảng hoặc mock UI.
- [ ] Demo được bằng ít nhất hai tài khoản; với nhóm/quyền dùng thêm tài khoản thứ ba.
- [ ] Hoàn tất checklist Git của task ở mục 17; ghi branch, commit, kết quả kiểm tra và PR hoặc blocker cụ thể. Chỉ ghi `Merged` sau khi xác minh đã merge.

**Thứ tự lấy việc đầu tiên:** P0-T01/T02 → P0-T03 → P1-T01 → P1-T02 → P2-T01 → P2-T02/T03 → P3-T01/T02/T03. Đây là đường ngắn nhất từ source hiện tại đến DM có lưu trữ và realtime để demo.

## 17. Git flow bắt buộc đi kèm từng task

### 17.1. Quy ước và phạm vi tự động

Theo yêu cầu người dùng, khi được giao **triển khai một task**, agent chủ động làm trọn luồng: kiểm tra repository → tạo/tiếp tục branch → triển khai → kiểm tra → commit → push → mở/cập nhật PR nếu có công cụ và quyền truy cập. Không cần hỏi lại cho các bước này. Việc thêm quy trình này không tự khởi chạy toàn bộ roadmap.

Repository tại thời điểm rà soát có `main`, remote `origin` trỏ đến `Chyeonma/scdc` và một số remote-tracking branch `feat/...`; chưa thấy `develop`. Chọn luồng feature branch → PR → `main`, không tự tạo thêm nhánh tích hợp dài hạn. Thông tin branch cục bộ có thể cũ; phải fetch trước khi bắt đầu task.

- **Một task = một branch = một PR** mặc định. Subtask `.1`, `.2`, … dùng chung branch và có thể có commit riêng khi tạo thành thay đổi hợp lý.
- Tên branch theo bảng 17.3, chữ thường, có mã task để truy vết. Nếu branch đã tồn tại, kiểm tra rồi tiếp tục; không reset/ghi đè để tạo lại.
- Branch lấy từ `origin/main` mới nhất khi dependency đã merge. Nếu dependency chưa merge, ưu tiên task độc lập; khi cần làm nối tiếp, tạo stacked branch từ branch dependency và PR nhắm vào branch đó, ghi rõ quan hệ rồi đổi base sau khi dependency merge.
- Với task chỉ thiết kế/tài liệu như P0-T01/T02, đầu ra là tài liệu đã được kiểm tra và vẫn commit/push/PR bình thường.
- Không tự merge PR, push thẳng `main`, force-push, xóa branch hoặc deploy theo quy trình mặc định này. Chỉ làm các bước đó khi có yêu cầu riêng; quyền tạo branch/commit/push/PR đã được người dùng cung cấp.
- Bản sửa lỗi của task đang mở tiếp tục branch đó. Lỗi phát hiện sau merge dùng branch mới `fix/msg-pN-tNN-<slug>` và PR mới.

### 17.2. Checklist Git áp dụng cho mỗi task

Khi bắt đầu task `P2-T01`, tạo phần theo dõi với các mã `P2-T01-G01`…`P2-T01-G07` dưới đây. Đây là checklist quy trình, không thay thế subtask nghiệp vụ.

| Mã hậu tố | Hành động tự thực hiện | Điều kiện hoàn tất |
|---|---|---|
| G01 | Đọc hướng dẫn repo; kiểm tra status, branch, remote; fetch; rà code và branch liên quan | Xác định đúng base/dependency, không làm lại phần đã có trên branch khác |
| G02 | Tạo hoặc tiếp tục branch theo bảng; nếu working tree có thay đổi ngoài task thì dùng worktree cô lập | Đang ở đúng branch, không mất/ghi đè thay đổi có sẵn |
| G03 | Triển khai subtask, cập nhật test/tài liệu, đánh dấu checkbox theo bằng chứng | Diff đúng phạm vi và tiêu chí nghiệm thu có thể kiểm tra |
| G04 | Review diff; chạy build/test phù hợp, kiểm tra whitespace và nội dung nhạy cảm | Ghi rõ lệnh, kết quả, kiểm tra chưa chạy và lý do |
| G05 | Stage đúng file/hunk của task và commit theo quy ước | Có commit SHA; không đưa file không liên quan hoặc secret vào commit |
| G06 | Push branch lên `origin`, thiết lập upstream lần đầu; cập nhật remote sau commit sửa lỗi | Xác nhận push thành công và remote branch chứa commit vừa tạo |
| G07 | Tạo/cập nhật PR, theo dõi CI nếu truy cập được, báo kết quả | Có URL PR/branch, commit, test, CI và trạng thái còn chờ |

**Khi có trở ngại:** sửa lỗi build/test/CI trong phạm vi task rồi commit và push bản sửa. Nếu chưa thể hoàn tất, có thể push phần việc an toàn lên branch riêng và mở draft PR, ghi rõ lỗi; không đánh dấu task hoàn thành. Nếu mạng, SSH, công cụ PR hoặc quyền truy cập chặn thao tác, giữ commit local, báo bước thất bại và lệnh cần chạy tiếp; không báo đã push/PR khi chưa thành công. Không lặp lại yêu cầu cho phép push vốn đã được người dùng đồng ý.

**Bảo vệ thay đổi đang có:** không tự stash, reset hoặc checkout đè file người dùng. Khi dùng worktree, đặt nó trong vị trí được phép ghi và không đưa thư mục worktree vào commit. Nếu roadmap chưa có trong base, đưa đúng phần tài liệu cần thiết vào nhánh một cách có kiểm soát; không commit lại snapshot cũ làm mất cập nhật của task khác.

### 17.3. Branch tương ứng từng task

Mỗi dòng đều phải thực hiện G01–G07; tên PR chứa cùng mã task.

| Task | Branch mặc định | Đầu ra chính của PR |
|---|---|---|
| P0-T01 | `docs/msg-p0-t01-policies` | Ma trận quyền và rule nhắn tin |
| P0-T02 | `docs/msg-p0-t02-contracts` | Contract API, DTO, event và ownership |
| P0-T03 | `test/msg-p0-t03-fixtures` | Môi trường và fixture kiểm thử |
| P1-T01 | `feat/msg-p1-t01-direct-conversation` | Tạo/lấy DM và persistence |
| P1-T02 | `feat/msg-p1-t02-inbox` | Inbox và UI chọn người nhận |
| P2-T01 | `feat/msg-p2-t01-send-message` | Gửi tin, transaction, idempotency |
| P2-T02 | `feat/msg-p2-t02-history` | Lịch sử và cursor |
| P2-T03 | `feat/msg-p2-t03-composer` | Composer thật, trạng thái gửi và retry |
| P3-T01 | `feat/msg-p3-t01-signalr-access` | Hub, subscribe và revoke |
| P3-T02 | `feat/msg-p3-t02-outbox` | Worker outbox và retry |
| P3-T03 | `feat/msg-p3-t03-reconnect` | Reconnect và đồng bộ UI |
| P4-T01 | `feat/msg-p4-t01-group-chat` | Nhóm và membership |
| P4-T02 | `feat/msg-p4-t02-community-chat-access` | Dependency server/channel/quyền |
| P4-T03 | `feat/msg-p4-t03-unified-spaces` | Luồng message chung ba loại space |
| P5-T01 | `feat/msg-p5-t01-read-state` | Đã đọc, unread và receipt nếu cần |
| P5-T02 | `feat/msg-p5-t02-conversation-preferences` | Typing, mute, hide và notification |
| P6-T01 | `feat/msg-p6-t01-edit-delete` | Sửa/xóa, version và event |
| P6-T02 | `feat/msg-p6-t02-reply-thread` | Reply, thread và điều hướng |
| P6-T03 | `feat/msg-p6-t03-reactions-pins` | Reaction và pin message |
| P6-T04 | `feat/msg-p6-t04-mentions` | Mention và notification tương ứng |
| P7-T01 | `feat/msg-p7-t01-attachment-upload` | Storage, upload và metadata |
| P7-T02 | `feat/msg-p7-t02-attachment-download` | UI attachment và download |
| P8-T01 | `feat/msg-p8-t01-search` | Search có phân quyền |
| P8-T02 | `feat/msg-p8-t02-user-blocks` | Block/unblock và policy |
| P8-T03 | `feat/msg-p8-t03-moderation` | Report và xử lý vi phạm |
| P9-T01 | `test/msg-p9-t01-release-coverage` | Test xuyên suốt và kiểm tra quyền |
| P9-T02 | `perf/msg-p9-t02-observability` | Đo tải, metric và runbook |
| P9-T03 | `chore/msg-p9-t03-release-readiness` | Cấu hình và tài liệu bàn giao |

### 17.4. Commit, PR và kiểm tra

Commit theo dạng `<type>(<scope>): <task-id> <nội dung>`. Ví dụ:

```text
feat(messaging): P2-T01 persist messages with idempotency
test(messaging): P2-T01 cover concurrent send retries
docs(messaging): P0-T02 define message API contracts
fix(web): P3-T03 resubscribe spaces after reconnect
```

PR title ví dụ: `[P2-T01] Gửi tin nhắn với transaction và idempotency`. Nội dung PR phải có vấn đề/hành vi sau thay đổi, subtask đã làm, dependency/PR base, kiểm tra thực tế và giới hạn còn lại. Không ghi test pass khi chưa chạy. Khi dùng CLI tạo PR nhiều dòng, ghi body vào file rồi dùng `--body-file`.

Các lệnh kiểm tra lựa chọn theo thay đổi:

```powershell
git diff --check
dotnet build SCDC.slnx
dotnet test tests/SCDC.Api.Tests/SCDC.Api.Tests.csproj
npm --prefix clients/WebClient run build
```

Chuẩn bị dependency/database theo hướng dẫn repo trước khi chạy. Chỉ sửa tài liệu thì kiểm tra link, mã task và diff, không bắt buộc chạy toàn bộ backend/frontend. Khi đã có CI, dùng CI của repository làm cổng kiểm tra tương ứng; không mặc định CI đã tồn tại.

Ví dụ khởi tạo và push branch **khi đã xác minh working tree, dependency và remote**; thực hiện từng bước, dừng xử lý nếu lệnh lỗi:

```powershell
git fetch origin
git switch -c feat/msg-p2-t01-send-message origin/main
# Triển khai và kiểm tra trước khi stage đúng các file/hunk của task.
git diff --cached --check
git diff --cached
git commit -m "feat(messaging): P2-T01 persist messages with idempotency"
git push -u origin feat/msg-p2-t01-send-message
```

### 17.5. Trạng thái và mẫu báo cáo bàn giao

Luồng trạng thái: `Todo → In progress → PR draft/Ready for review → Merged`. Dùng `Blocked` kèm nguyên nhân nếu không thể tiếp tục; `Pushed` không đồng nghĩa đã nghiệm thu hay đã merge. Checkbox subtask chỉ phản ánh phần việc đã kiểm tra, còn trạng thái PR theo dõi riêng.

Mỗi task ghi kết quả theo mẫu sau trong báo cáo bàn giao hoặc PR:

```text
Task: P2-T01
Subtask hoàn tất: ...
Branch / base: ...
Commit: ...
Kiểm tra đã chạy và kết quả: ...
Push: thành công / bị chặn + lý do
PR: URL / chưa tạo + lý do
CI: pass / fail / pending / chưa có hoặc không truy cập được
Trạng thái: Ready for review / PR draft / Blocked / Merged
Phần còn lại: ...
```

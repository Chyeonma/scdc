# Chính sách Messaging v1 — P0-T01

Ngày: **19/09/2026**. Phạm vi: DM, group và server channel. Đây là quyết định thiết kế v1 để các task sau triển khai; không xác nhận backend đã thực thi các rule này. Thay đổi policy sau review cần cập nhật tài liệu và các ca nghiệm thu liên quan.

Nguồn đối chiếu: [schema](../../database/postgres/schema.sql), [seed](../../database/postgres/seed.sql), [Identity enums](../../services/Modules/Identity/Domain/IdentityEnums.cs), [kiến trúc](../architecture/overview.md), [quy ước lỗi](../api/error-handling.md), [roadmap](../messaging-roadmap.md).

## 1. Quy ước enum — P0-T01.1

SQL hiện chủ yếu giới hạn tập giá trị số, chưa định nghĩa đủ tên nghiệp vụ. Bảng dưới chốt tên để backend/frontend dùng thống nhất; tên mới là quyết định của tài liệu này, không phải enum đã có trong code. Giá trị không biết phải bị từ chối, không tự chuyển sang giá trị mặc định.

| Cột | Giá trị và ý nghĩa v1 | Căn cứ / lưu ý |
|---|---|---|
| `spaces.space_type` | 1 Direct, 2 Group, 3 Channel | Đã có comment SQL và FK theo loại |
| `spaces.status` | 1 Active, 2 Archived, 3 Deleted | Chốt theo constraint `archived_at`/`deleted_at` |
| `space_members.member_role` | 1 Member, 2 Admin, 3 Owner | Seed dùng 3 cho chủ nhóm, 1 cho hai bên DM; Admin=2 là quyết định v1 |
| `space_members.membership_status` | 1 Active, 2 Left, 3 Removed | Status 1 bắt buộc `left_at IS NULL`; 2/3 phải có `left_at` |
| `messages.message_type` | 1 Text, 2 System, 3 Attachment, 4 Reserved | Seed dùng 1/2/3; 4 chưa có use case, v1 không cho tạo |
| `space_user_states.notification_level` | 0 None, 1 MentionsOnly, 2 AllMessages | Chốt v1; mute có hiệu lực tạm thời cao hơn mức này |
| `attachments.scan_status` | 0 Pending, 1 Clean, 2 Rejected, 3 Failed | Seed dùng 1 cho file mẫu; chỉ Clean được tải xuống |

`version` là số nguyên từ 1, không phải enum. `sequence_no` là thứ tự toàn cục, có khoảng trống; không dùng phép trừ sequence để tính unread.

Identity đã định nghĩa `PendingVerification=0`, `Active=1`, `Suspended=2`, `Disabled=3`, `Deleted=4`. Tài khoản thực hiện thao tác Messaging phải Active, có session hợp lệ. Role của server do Community quản lý, không ánh xạ số role DM/group sang role server.

### Vòng đời space và member

- Active cho phép đọc và thao tác theo ma trận quyền. Archived cho đọc/history/search/file, cập nhật read state và tùy chỉnh inbox; cấm gửi/sửa/reaction/pin, thêm thành viên và thao tác nội dung thông thường. Vẫn cho xóa tin của mình, xóa theo quyền moderation, report, block và rời nhóm.
- Group Owner được archive/unarchive nhóm; Channel dùng quyền `manage_channels` do Community cấp. DM v1 không có thao tác archive toàn cục; dùng hide/mute theo user. Unarchive chỉ về Active nếu chưa Deleted.
- Deleted là trạng thái kết thúc trong v1, cấm truy cập thông thường và unsubscribe các connection. Không có API khôi phục/xóa space toàn cục trong phạm vi MVP DM; lifecycle này dành cho group/channel sau đó.
- Khi đổi status phải cập nhật timestamp cùng transaction: archive đặt `archived_at`; unarchive xóa `archived_at`; delete đặt `deleted_at`. SQL hiện không ép đủ tất cả chiều của rule này nên application phải bảo đảm.
- Group có đúng một Owner Active khớp `group_conversations.owner_user_id`. Owner phải chuyển quyền cho thành viên Active trước khi rời; nếu là thành viên cuối thì đóng/xóa nhóm. Admin không tự nâng mình thành Owner, không xóa Owner hoặc Admin khác; Owner quản lý Admin và chuyển quyền.
- DM giữ đúng hai thành viên Member Active khớp cặp direct conversation; không có thêm người, kick hay chuyển owner. Ẩn DM không thay membership và không xóa lịch sử.
- Group Active → Left khi tự rời, Active → Removed khi bị xóa. Thêm lại đưa về Active, cập nhật `joined_at`, xóa `left_at`/`removed_by_user_id`, mặc định Member. Không giữ đặc quyền Admin của lần tham gia trước.

## 2. Mở DM, gửi và đọc — P0-T01.2

### Điều kiện mở DM

1. Người gọi và người nhận đều Active, khác user ID. V1 không yêu cầu kết bạn/follow và không có hàng chờ message request.
2. Không có block ở bất kỳ chiều nào. Nếu có, từ chối mở/tạo qua thao tác mở DM; DM đã tồn tại vẫn đọc được theo chính sách block ở mục 6.
3. Một cặp user chỉ có một DM, bất kể thứ tự yêu cầu. Chuẩn hóa UUID theo thứ tự PostgreSQL và dùng unique constraint; tạo đồng thời phải trả cùng hội thoại, không để space mồ côi.
4. Cặp DM đã Deleted không tạo bản thứ hai để vượt unique constraint; từ chối mở lại. Khôi phục là use case ngoài v1.
5. Tác giả lấy từ session, không tin `authorUserId` hay role do client gửi. Tra cứu user không đủ để xác nhận Active: cần contract Identity cung cấp eligibility, vì `IUserDirectory` hiện chỉ trả ID/username/display name.

### Ma trận actor × space × hành động

Áp dụng cho **space Active, tin chưa xóa**, trừ khi ghi khác. Mọi ô cho phép đều cần tài khoản/session hợp lệ và quyền đọc hiện hành. Không có quyền đọc thì các quyền khác không có hiệu lực. Các giới hạn Archived, block và file scan áp dụng sau ma trận và có thể từ chối thao tác.

| Actor / loại space | Đọc, search, file, subscribe | Gửi, reply, thread | Sửa tin mình | Xóa tin mình | Xóa tin người khác | Pin/unpin message | Reaction |
|---|---|---|---|---|---|---|---|
| Một trong hai thành viên DM | Có | Có | Có | Có | Không | Có | Có |
| Thành viên group Active | Có | Có | Có | Có | Không | Không | Có |
| Admin/Owner group Active | Có | Có | Có | Có | Có, trừ System | Có | Có |
| Thành viên channel | `read_messages` | Thêm `send_messages` | Thêm `send_messages` + `edit_own_messages` | Có khi còn quyền đọc | Thêm `delete_messages`, trừ System | Thêm `pin_messages` | Thêm `add_reactions` |
| Người ngoài DM/group, member Left/Removed | Không | Không | Không | Không | Không | Không | Không |
| Người ngoài server, bị kick/ban, hoặc không có quyền đọc channel | Không | Không | Không | Không | Không | Không | Không |
| Chưa đăng nhập / session không hợp lệ | Không | Không | Không | Không | Không | Không | Không |

- Channel upload còn cần `send_messages` + `attach_files`. Download chỉ cần quyền đọc và file Clean; không cần `attach_files`.
- `pin_messages` là permission mới cần bổ sung ở P4/P6; seed hiện chưa có. Quyền owner/admin server phải được Community trả thành quyền hiệu lực; Messaging không tự bypass dựa vào tên role.
- Subscribe cho phép nhận event trong phạm vi quyền; quyền phải được kiểm tra lại khi dispatch/tải bù và thu hồi khi membership/session thay đổi. Không phát nội dung chỉ vì connection từng join group thành công.
- Reaction/pin chỉ dành cho message còn tồn tại và không phải System. Bỏ reaction của mình cũng dùng cùng quyền reaction; không cho thay reaction của user khác.
- Report cần quyền đọc tin tại thời điểm tạo; quyền review bằng chứng của moderator là quyền riêng trong Moderation, không tự cấp quyền duyệt toàn bộ DM/group.
- Read state, hide/mute và pin hội thoại là trạng thái cá nhân; không cần quyền pin message và không ảnh hưởng người khác.

### Khi quyền hoặc tài khoản thay đổi

- Thu hồi quyền đọc: HTTP/history/search/file từ chối, Hub unsubscribe, dừng notification có nội dung; FE xóa dữ liệu space khỏi state hiển thị. Không thể thu hồi bản sao/screenshot đã tải trước đó.
- Chỉ thu hồi quyền gửi: vẫn đọc/nhận được; cấm gửi/sửa/reply/thread/upload/typing. Xóa tin mình và các quyền reaction/pin độc lập vẫn theo ma trận.
- Người nhận DM không còn Active: không gửi thêm cho người đó; người gửi Active vẫn đọc lịch sử của mình. Người có tài khoản không Active không được dùng Messaging kể cả history.
- Gửi và revoke/block đồng thời phải có điểm quyết định transaction rõ ràng: thao tác được chấp nhận trước khi revoke/block có hiệu lực có thể đã lưu; thao tác sau thời điểm đó phải bị từ chối. Không hứa thu hồi dữ liệu đã gửi đến client.

## 3. Lịch sử tham gia — P0-T01.3

| Trường hợp | Chính sách lịch sử |
|---|---|
| Hai bên DM | Xem toàn bộ lịch sử DM còn được giữ lại, kể cả khi hội thoại bị hide/mute |
| Thành viên mới vào group | Xem toàn bộ lịch sử nhóm, bao gồm tin trước `joined_at` |
| Thành viên tự rời/bị xóa | Mất toàn bộ quyền đọc/gửi/subscribe/download; không giữ quyền xem chỉ các tin trước khi rời |
| Thành viên được thêm lại | Xem toàn bộ lịch sử, kể cả khoảng vắng mặt; phục hồi mốc đọc cũ nếu có, không tự đánh dấu đã đọc khoảng đó |
| Thành viên channel có quyền đọc | Xem toàn bộ lịch sử channel; không giới hạn theo ngày join server |
| Thành viên bị thu hồi rồi cấp lại quyền channel | Truy cập lại toàn bộ lịch sử còn giữ, trừ tin đã xóa/nội dung đã purge |

`space_members` hiện chỉ giữ trạng thái mới nhất của một user/space. Chính sách trên không cần bảng nhiều khoảng membership. Audit join/leave nếu cần không được dùng thay quyền hiện hành. Lựa chọn tương lai “chỉ xem từ lúc tham gia” sẽ cần thiết kế thêm trước khi thay policy.

## 4. Nội dung và loại tin — P0-T01.4

- Text: loại 1, content bắt buộc; chuẩn hóa CRLF/CR thành LF, trim khoảng trắng đầu/cuối trước khi lưu, từ chối chuỗi rỗng. Nội dung sau chuẩn hóa tối đa **10.000 Unicode code point**, không đếm byte, UTF-16 code unit hay ký tự hiển thị ghép. Backend kiểm tra quyết định; FE đếm tương ứng để hướng dẫn người dùng. Giới hạn này tương thích `char_length` SQL khi lưu chuỗi đã chuẩn hóa.
- Attachment: loại 3, ít nhất một file đã complete, thuộc người gửi, hợp lệ và Clean trước khi message được gửi. Caption tùy chọn, sau chuẩn hóa không quá 10.000 code point; rỗng lưu NULL. Không tin `attachment_count` metadata. Loại 1 không kèm file; có file thì dùng loại 3.
- V1 cho sửa text hoặc caption, không thay danh sách file của tin đã gửi. Xóa caption được phép cho loại 3; xóa hết text của loại 1 phải dùng thao tác xóa message.
- System: loại 2, chỉ handler nội bộ tạo cho sự kiện được cho phép; dùng `author_user_id=NULL`, `client_message_id=NULL`, metadata có event định danh/idempotency. Không nhận loại 2 từ API gửi tin của user. User không sửa/xóa/reaction/pin System; việc ẩn nội dung System chỉ qua quy trình quản trị được audit riêng nếu bổ sung sau này.
- Reserved: loại 4 bị từ chối ở v1 kể cả SQL chấp nhận. Reply và thread không phải loại 4; chúng là quan hệ trên Text/Attachment.
- Reply/thread phải trỏ tới message cùng space mà user đọc được. V1 thread một cấp: root là tin Text/Attachment không có `thread_root_id`; không tạo reply mới vào tin đã xóa. Root bị xóa giữ tombstone và các reply cũ còn đọc theo quyền space.
- Plain text là định dạng mặc định; không render HTML người dùng. Metadata từ client phải qua allowlist; không cho chèn event hệ thống, tác giả hoặc dữ liệu quyền.
- Trước khi P7 có upload thật, chỉ bật Text; không giả lập message Attachment thành công bằng metadata ở FE. Giới hạn số file/byte và MIME sẽ được chốt tại P7, không phải giới hạn đã được triển khai trong P0.

## 5. Sửa, xóa, pin và lịch sử chỉnh sửa — P0-T01.5

- **Sửa:** chỉ tác giả khi vẫn có quyền sửa theo ma trận; không đặt cửa sổ thời gian trong v1. Admin/moderator không viết lại nội dung của người khác. Gửi expected version; tranh chấp sửa/sửa hoặc sửa/xóa phải trả conflict, không im lặng ghi đè.
- **Xóa:** xóa mềm cho mọi người, không có chế độ “xóa riêng tin này cho tôi” trong v1. Tác giả còn quyền đọc được xóa tin mình không giới hạn thời gian, kể cả channel read-only hoặc Archived. Group Admin/Owner và channel có `delete_messages` được xóa tin người khác theo phạm vi quyền. Tin System là ngoại lệ nêu ở mục 4.
- **Xóa lặp:** sau kiểm tra quyền và danh tính đối tượng, thao tác xóa đã hoàn tất được coi là thành công; không tạo thêm event/audit giống nhau do retry. Tin đã xóa không sửa lại hoặc phục hồi trong v1.
- **Tombstone:** giữ message ID, sequence, reply/thread relationships; DTO thường không chứa content cũ, metadata có nội dung, file URL, reaction hay mention của tin đã xóa. Preview, search, pin, notification và outbox chưa dispatch phải áp dụng cùng rule. Gỡ pin và ẩn file ngay khi xóa.
- **Pin:** pin message là trạng thái chung; hai bên DM đều được pin/unpin; group chỉ Admin/Owner; channel cần `pin_messages`. Pin/unpin idempotent, không biến thành quyền sửa/xóa message. Pin hội thoại ở inbox là riêng từng user.
- **Lịch sử sửa:** mỗi lần thay đổi thực sự lưu `previous_content` và version cũ trong `message_edits`, cập nhật message/version/outbox cùng transaction. No-op không tạo version mới. V1 không cung cấp endpoint lịch sử sửa cho thành viên, tác giả hay admin nhóm; bằng chứng chỉ truy cập qua luồng Moderation có quyền riêng và audit khi tính năng đó được triển khai.

## 6. Block và thông báo trực tiếp — P0-T01.5

| Hành động khi A block B (hoặc ngược lại) | Kết quả |
|---|---|
| Tạo/mở DM bằng thao tác open-direct | Từ chối với thông báo chung, không tiết lộ ai block ai |
| Gửi DM, sửa nội dung DM, thêm reaction/pin, typing | Chặn cả hai chiều để không dùng chúng như kênh liên lạc thay thế |
| Đọc/search/download lịch sử DM đã có | Cho phép nếu còn đủ quyền; block không xóa lịch sử |
| Xóa tin DM của mình, bỏ reaction của mình, unpin | Cho phép dọn nội dung/trạng thái nếu đủ quyền cơ bản; sự kiện xóa phải đồng bộ để không giữ nội dung đã xóa trên phía còn lại |
| Read receipt của DM | Cho cập nhật mốc đọc cá nhân, không gửi delivered/read receipt mới cho bên kia trong thời gian block |
| Unblock | Chỉ người tạo block được gỡ; gửi lại chỉ khi không còn block ở cả hai chiều; không phát bù typing/receipt đã bị suppress |
| Cùng group/channel | Membership/quyền không đổi; vẫn nhìn thấy tin của nhau và tương tác theo quyền chung |
| Mention giữa hai user bị block trong group/channel | Không tạo notification trực tiếp cho người block/bị block; nội dung chung vẫn hiển thị và không được dùng block để vượt quyền moderation |
| Thêm người vào group | Người thực hiện không được thêm target nếu có block ở một trong hai chiều giữa họ; thành viên khác có quyền vẫn có thể thêm, không kiểm tra mọi cặp trong nhóm |

Block không thay thế kick/ban hoặc kiểm soát quyền channel. Không tự rời nhóm, xóa tin cũ hoặc che nội dung phục vụ moderation. User chỉ xem/sửa danh sách block do chính mình tạo; không API liệt kê ai đã block mình.

## 7. Thời hạn lưu dữ liệu — P0-T01.5

Đây là mặc định sản phẩm v1, chưa có job purge trong source. Không chạy cleanup dữ liệu trong task tài liệu này. P6/P7/P8/P9 phải triển khai và đo độ trễ cleanup trước khi công bố retention này cho người dùng.

| Dữ liệu | Thời hạn v1 và thời điểm bắt đầu |
|---|---|
| Message chưa xóa và file của nó | Giữ cho đến khi message/space bị xóa; chưa có auto-expire cho hội thoại đang hoạt động |
| Nội dung message/file đã xóa | Ẩn ngay khi xóa; purge nội dung và object sau 30 ngày từ thời điểm message hoặc space bị xóa, lấy mốc sớm hơn |
| `message_edits` | Purge từng bản sau 90 ngày từ `edited_at`, hoặc hạn purge message đã xóa nếu sớm hơn |
| Tombstone ID/sequence và quan hệ reply/thread | Giữ không có TTL trong v1 để bảo toàn FK/cursor; không giữ content/file/metadata nội dung sau purge |
| Snapshot report | Giữ trong khi report chưa kết thúc; purge snapshot/details chứa nội dung sau 180 ngày từ `resolved_at`; hồ sơ chỉ còn ID, loại quyết định và timestamp |
| Audit hành động moderation không chứa nội dung chat | Giữ 365 ngày từ lúc tạo rồi purge theo job riêng |
| Payload chat trong outbox/inbox | Không giữ bản sao nội dung quá hạn purge message; ưu tiên event tham chiếu ID và hydrate theo trạng thái hiện hành; event đã xử lý dọn trong 7 ngày |
| Backup có dữ liệu đã purge | Chu kỳ lưu tối đa 30 ngày; restore phải áp dụng lại trạng thái xóa/purge trước khi phục vụ người dùng |

Report giữ bằng chứng riêng trong `message_snapshot`, không kéo dài retention bản chat gốc. Mọi truy cập bằng chứng phải qua quyền review có audit. Không sao chép nội dung chat hoặc token vào application log. Event lỗi không được trở thành kho giữ payload vô hạn: cách ly rồi loại nội dung theo hạn trên, lưu ID/lỗi tối thiểu phục vụ điều tra.

**Tương thích SQL khi purge:** FK `ON DELETE RESTRICT` ở message/reply/report không cho xóa vật lý tùy tiện. V1 ưu tiên redact content của tombstone Text thành chuỗi không rỗng cố định như `[deleted]` để vẫn thỏa `ck_messages_text`, xóa metadata nhạy cảm và object file. Client hiển thị tombstone theo `deleted_at`, không hiển thị chuỗi lưu trữ. Audit job purge cần lưu mốc hoàn tất; phải thiết kế trường `purged_at`/job ledger và script nâng cấp ở task triển khai, không dùng `schema.sql` reset database.

## 8. Thứ tự từ chối và kết quả phía người dùng

Không chốt route/DTO/error-code cụ thể ở đây; P0-T02 sẽ đặt tên contract theo [quy ước lỗi](../api/error-handling.md). Application trả Result; API ánh xạ HTTP. Hub dùng cùng rule nhưng trả lỗi giao thức Hub phù hợp, không giả định HTTP status cho từng invocation.

| Kiểm tra theo thứ tự | Kết quả mong muốn |
|---|---|
| Session thiếu/hết hiệu lực/đã revoke | Unauthorized (`401` cho HTTP); dừng nhận realtime |
| Actor không Active nhưng session được nhận diện | Forbidden (`403`); không trả dữ liệu chat |
| ID sai định dạng, enum/content sai | Validation (`400`), không phản ánh sự tồn tại resource |
| Space/message không tồn tại, Deleted, hoặc actor không có quyền đọc | NotFound (`404`) với nội dung chung để không phân biệt resource riêng tư |
| Đọc được nhưng thiếu quyền hành động; block hoặc người nhận DM không khả dụng | Forbidden (`403`), thông báo chung; không tiết lộ người block hay trạng thái tài khoản người nhận |
| Space Archived và hành động không thuộc ngoại lệ; tin đã xóa khi sửa/reply/pin | Conflict (`409`) sau khi đã kiểm tra quyền đọc |
| Expected version cũ hoặc idempotency key khác payload | Conflict (`409`), client tải lại trước khi tiếp tục |
| Vượt giới hạn request | TooManyRequests (`429`), UI cho retry theo hướng dẫn server |

Không trả preview/content cùng lỗi. Rate limit có thể chạy trước lookup để bảo vệ hệ thống nhưng không thay chính sách bảo mật resource. Lỗi mở DM với người nhận không tồn tại/không khả dụng dùng phản hồi chung; không cho phân biệt block với account bị khóa.

## 9. Khoảng trống triển khai và thay đổi cần thiết

| Quyết định | SQL/contract hiện có | Việc chuyển cho task sau |
|---|---|---|
| Enum 1/2/3 và Reserved=4 | Tập số hợp lệ đã có; tên/chuyển trạng thái chưa đầy đủ | P1/P2 map enum và domain validation; không cần đổi số SQL |
| Active account và session | `IUserDirectory` chỉ có user summary | P0-T02 thiết kế eligibility/session contract; Identity sở hữu quyết định |
| Quyền sửa/xóa/pin channel | `IChannelAccessChecker` chỉ có CanRead/CanSend; seed chưa có pin | P0-T02/P4/P6 mở rộng capability contract; thêm `pin_messages` qua SQL nâng cấp/seed, default deny nếu chưa cấp |
| Chủ nhóm duy nhất và trạng thái đầy đủ | FK/check chưa ép tất cả rule | P4 transaction/locking; cân nhắc unique owner Active và constraint timestamp qua script nâng cấp |
| Tất cả lịch sử khi tham gia lại | Một dòng membership/user/space | P4 dùng lại schema; không thêm bảng lịch sử chỉ để phân quyền |
| Attachment-only phải có file Clean | FK chỉ bảo đảm file thuộc message | P7 upload staging/complete, ownership, kiểm tra file trước publish; bổ sung schema staging nếu cần |
| Block dùng ngay từ DM đầu tiên | `user_blocks` đã có, chưa có use case | P1/P2 kiểm tra block trong authorization; P8 bổ sung UI/API quản lý và ca race đầy đủ |
| Soft delete, edit history và retention | Có `deleted_at`, `message_edits`, snapshot; chưa có purge tracking/worker | P6/P7/P8/P9 triển khai redact/cleanup/retention config và ledger; xử lý FK, backup và payload event |

P0-T01 chỉ thêm tài liệu, không sửa schema, seed, backend hoặc frontend. Các rule là tiêu chí cho slice tương ứng; chưa đánh dấu các slice đó hoàn thành.

## 10. Ca nghiệm thu để triển khai test sau này

Các ca sau đã được rà soát ở mức thiết kế, **chưa chạy runtime** vì backend Messaging còn foundation.

| ID | Tình huống | Kết quả phải đạt | Task triển khai |
|---|---|---|---|
| POL-01 | A tạo DM B, B đồng thời tạo DM A | Cùng space; không có space mồ côi | P1-T01 |
| POL-02 | Tự DM, target không khả dụng, A block B | Từ chối theo bảng lỗi, không lộ chiều block | P1-T01 |
| POL-03 | C đoán ID tin/file/space DM A–B | Không đọc/history/search/subscribe/download được | P1–P3, P7–P8 |
| POL-04 | D vào group sau khi có lịch sử | Đọc được tin trước ngày vào | P4-T01 |
| POL-05 | D rời/bị kick rồi gọi HTTP và giữ socket cũ | Không nhận nội dung mới và không truy cập lịch sử/file | P3–P4, P7 |
| POL-06 | D được thêm lại sau một khoảng vắng mặt | Đọc toàn bộ lịch sử; không tự đánh dấu tin đã đọc | P4–P5 |
| POL-07 | Channel chỉ có quyền đọc | Đọc/xóa tin mình được; gửi/sửa/typing bị từ chối | P4/P6 |
| POL-08 | Group member pin; admin sửa tin người khác | Đều từ chối; admin có thể xóa tin thường người khác | P6 |
| POL-09 | Text rỗng/toàn whitespace, 10.000/10.001 code point; emoji ngoài BMP | Rỗng và quá giới hạn bị từ chối; FE/BE thống nhất cách đếm | P2 |
| POL-10 | Client gửi System/Reserved hoặc Attachment không có file | Từ chối dù SQL có thể chấp nhận một số dữ liệu | P2/P7 |
| POL-11 | Hai lần sửa cùng version; sửa đua với xóa | Một kết quả được chấp nhận, bên stale nhận conflict; không phục hồi tin đã xóa | P6-T01 |
| POL-12 | Xóa tin được pin và có reply, rồi reconnect/search | Chỉ tombstone, không lộ nội dung/file; reply giữ quan hệ | P3/P6/P8 |
| POL-13 | Block khi có DM và group chung | DM ngừng liên lạc; lịch sử còn đọc; group không đổi quyền; mention không thông báo trực tiếp | P5/P8 |
| POL-14 | Owner nhóm muốn rời mà chưa chuyển quyền | Từ chối, hoặc đóng nhóm nếu là thành viên cuối | P4-T01 |
| POL-15 | Archive space rồi gửi/sửa/pin và đọc/xóa tin mình | Ba thao tác đầu bị chặn; hai thao tác sau hợp lệ | P4/P6 |
| POL-16 | Message xóa đã quá 30 ngày và có FK/report | Nội dung gốc/file purge, tombstone còn hợp lệ; snapshot theo retention riêng | P7–P9 |
| POL-17 | Logout/revoke, quyền đổi, outbox còn event pending | Không phát payload ngoài quyền hiện hành; client tải bù theo quyền mới | P3 |
| POL-18 | Không có `pin_messages` nhưng có `send_messages` | Channel pin bị từ chối; không suy quyền từ CanSend | P4/P6 |

## 11. Đối chiếu hoàn thành P0-T01

| Subtask | Bằng chứng trong tài liệu |
|---|---|
| P0-T01.1 | Mục 1: enum, lifecycle và sự khác biệt với constraint hiện có |
| P0-T01.2 | Mục 2 và 8: mở DM, ma trận quyền và từ chối |
| P0-T01.3 | Mục 3: lịch sử mới vào/rời/bị xóa/thêm lại |
| P0-T01.4 | Mục 4: text, Unicode, attachment-only, System và Reserved |
| P0-T01.5 | Mục 5–7 và 9: sửa/xóa/pin, block, edit history, retention và khoảng trống schema |

Kết quả P0-T01 là tài liệu thiết kế sẵn sàng review. Bằng chứng Git (commit, push, PR và CI) được ghi trong PR/báo cáo bàn giao; không coi trạng thái tài liệu là bằng chứng runtime đã hoàn thiện.

<a id="scdc--vòng-đời-dữ-liệu"></a>

# SCDC — Vòng đời dữ liệu

Cập nhật: 2026-10-06. Phạm vi xuyên Accounts/DM/Community/Media; OQ-011. Đây là chính sách sản phẩm và thiết kế kỹ thuật để rà soát, chưa có worker retention, sổ bảo vệ dữ liệu độc lập hoặc kết quả restore trong repo.

Đây là phạm vi dữ liệu đầy đủ của [v1](../releases/v1.md). [MVP](../releases/mvp.md#acceptance) chọn gói và tiêu chí áp dụng; không cần triển khai mọi worker/restore/media trước gói đầu. Các quy tắc quyền, lưu tin và nội dung được luồng MVP sử dụng vẫn giữ theo nguồn chuẩn; thiết kế transaction/guard cần rà soát khi chuyển sang microservice ở v1 theo DEC-116.

<a id="mục-lục"></a>

## Mục lục

- [Phạm vi và quyết định](#policy)
- [Ma trận dữ liệu](#inventory)
- [Khóa tài khoản và lịch sử](#account-state)
- [Xóa tin, phòng và bộ nhớ tạm](#deletion)
- [Dọn dữ liệu kỹ thuật](#cleanup)
- [Bảo vệ dữ liệu sau restore](#restore)
- [Đối chiếu source và gói triển khai](#implementation)
- [Tiêu chí và kiểm thử](#acceptance)
- [Đầu vào còn mở](#gaps)

<a id="policy"></a>

<a id="1-phạm-vi-và-quyết-định"></a>

## 1. Phạm vi và quyết định

| Nội dung | Quy tắc/nguồn chuẩn |
|---|---|
| Tin đang lưu | Không tự hết hạn DM/tin phòng (DEC-070); chỉ bản nội dung mới nhất, xóa thay bằng tombstone (DEC-020/034/052) |
| Tài khoản | Chưa có tự xóa tài khoản v1 (DEC-103); khóa không xóa tin, chặn sử dụng/tương tác mới và mở khóa cần đăng nhập mới (DEC-104) |
| Phòng đã xóa | Không truy cập/khôi phục (DEC-077); giữ nội dung online chưa đặt hạn purge (DEC-105); không có xóa server v1 (DEC-094) |
| Backup | RPO ≤15 phút/RTO ≤4 giờ (DEC-086); tuổi tối đa mỗi backup/WAL 30 ngày DEC-109, không hứa PITR đủ mọi thời điểm tròn 30 ngày |
| Bộ nhớ tab | Bản nháp DM chỉ RAM (DEC-091); đóng tab/reload/đăng xuất mất bản nháp; token phiên vẫn theo thiết kế Accounts hiện tại |
| Log/audit | Log kỹ thuật 14 ngày, audit 90 ngày, IP/user-agent audit 7 ngày theo DEC-106; policy online, backup riêng |
| Dữ liệu kỹ thuật terminal | Dọn payload/chi tiết sau 7 ngày, giữ dedup/tombstone cần thiết, refresh family active theo DEC-107 |
| Restore | Áp lại xóa/thu hồi; mất bản sửa mới nhất thì “Nội dung chưa khôi phục được”, không đưa bản cũ trở lại theo DEC-108 |

Các mốc giữ log/dọn dữ liệu không tự thay thời hạn hiệu lực của token hoặc ngưỡng thu hồi chat/media. “Giữ dữ liệu” không cấp quyền đọc cho người đã rời/mất quyền hoặc cho người vận hành. “Xóa nội dung online” trong tài liệu là bỏ nội dung khỏi các bảng/đường phục vụ của ứng dụng; không hứa ghi đè mọi byte cũ trên đĩa, WAL hoặc backup ngay cùng thời điểm.

Không bổ sung tự xóa tài khoản, tự xóa tài khoản chưa xác minh, xóa cộng đồng hoặc công cụ xuất toàn bộ dữ liệu vào v1 trong đợt này. DEC-112 bổ sung phạm vi quản trị kỹ thuật có phân quyền/audit, chưa có UI riêng. Thẩm quyền/người khóa/mở khóa hoặc người trực vận hành vẫn chưa được chọn tại OQ-011; xem [RB-ACCOUNT](../guides/operations.md#account-support).

<a id="inventory"></a>

<a id="2-ma-trận-dữ-liệu-và-đường-truy-cập"></a>

## 2. Ma trận dữ liệu và đường truy cập

Các thời hạn gắn **loại dữ liệu và sự kiện bắt đầu tính**, không dùng một TTL chung cho cả DB. Job dọn chỉ xử lý schema do module sở hữu, gọi Contracts khi cần phối hợp; không xóa trực tiếp hàng Identity có FK Messaging/Community.

| Dữ liệu | Được phục vụ cho ai | Giữ online / trigger | Backup và dấu tối thiểu |
|---|---|---|---|
| User/email/username/password hash/profile | Chủ tài khoản và projection công khai được phép; không công khai email/hash | Giữ tài khoản; khóa không xóa/anonymize, không giải phóng username/email | Backup theo DEC-086; không có job tự xóa account |
| Phiên và refresh-token family | Identity và chủ phiên qua DTO không token | Active tới expiry/revoke; chi tiết terminal thêm 7 ngày DEC-107; không dọn token used của family active | Sau cleanup, token cũ vẫn không hợp lệ; security stamp/session floor không mất |
| Verify/reset token | Identity/worker đúng purpose | Hiệu lực 30 phút, cooldown 60 giây; hash/metadata terminal thêm 7 ngày DEC-107 | Link phục hồi từ backup không được hoạt động lại sau restore |
| Email delivery/envelope | Worker Identity; không trả envelope ra API/log | Envelope không còn khả năng dùng phải xóa khi terminal; metadata terminal 7 ngày DEC-107 | Restore không gửi lại email cũ; expiry vẫn tính UTC ban đầu |
| DM/tin phòng live | Actor active có quyền đọc scope; gồm tin tác giả bị khóa | Không tự hết hạn; chỉ nội dung hiện hành | Backup có thể có nội dung trước sửa/xóa; sổ bảo vệ chỉ ID/version/action |
| Tin tác giả chủ động xóa | Hai bên/member đúng quyền chỉ thấy tombstone | Content=null từ commit; giữ message ID/sequence/version và send-operation | Không tự mất tombstone/dedup vì dọn payload hoặc restore |
| Phòng deleted và các tin trong phòng | Không có API/UI đọc lại trong v1 | Giữ nội dung, chưa có hạn purge DEC-105 | Giữ ID/deleted epoch; backup 30 ngày không tự purge online |
| Membership/role/ACL/invite | Actor/member đúng quyền ở thời điểm thao tác | Rời mất quyền ngay, role/override của epoch cũ được dọn theo Community; metadata lịch sử chưa đặt hạn riêng | Dấu revoke/epoch cần giữ để chặn replay/restore; link issuer đã mất quyền không tự vô hiệu theo DEC-097 |
| DM/Community/Media operation key | Module thực thi dedup; không endpoint đọc nội dung fingerprint | Giữ ID/hash/key version/kết quả tối thiểu trong khi tài nguyên hoặc retry cũ còn cần chặn | Không dọn theo TTL log 14/audit 90 ngày |
| Call/participation/source terminal | Binding/room theo quyền; không tạo lịch sử missed call trong DM | Chi tiết 7 ngày từ terminal/quiesced DEC-107; giữ marker invalidation/dedup | Restore kết thúc trạng thái media cũ, fence SFU rồi mới cấp mới |
| Outbox/inbox đã xử lý | Worker đúng module, ID/trạng thái cho vận hành | Payload terminal thêm 7 ngày DEC-107, giữ dedup receipt cần thiết | Không replay mutation/email/notify từ snapshot cũ |
| Log kỹ thuật | Đầu mối kỹ thuật được phân quyền | 14 ngày từ event DEC-106; không chứa body chat/token | Retention kho log độc lập, không kế thừa backup DB |
| Audit bảo mật/quản lý | Người có thẩm quyền đúng mục đích | 90 ngày DEC-106; bỏ IP/user-agent thô sau 7 ngày | Backup audit DB theo DEC-086; audit không thay sổ chống phục hồi dữ liệu |
| Draft/cache client | Tab của actor đang dùng scope | RAM theo phiên; mất quyền/logout dọn scope tương ứng | Không backup server hoặc lưu nội dung DM xuống browser storage |
| Cache server/registry/cursor | Module/connection đúng quyền | Cache chỉ tối ưu; authorization luôn hiện hành, cursor không chứa nội dung tin | Restore đổi generation/invalidate caches; cursor cũ không mang quyền |

Không dùng audit như kho bản nội dung cũ. Trường được phép: event ID/type, actor/target ID, scope ID, trạng thái/version, timestamp, reason code, trace ID. Không ghi mật khẩu, access/refresh/media/link token, hash token, email envelope, nội dung tin, audio/video/screen, request body hoặc URL query/fragment nhạy cảm. Reason dùng mã/ghi chú hạn chế, không cho free text chép DM vào audit. Việc lọc log áp dụng cả reverse proxy, SignalR query access_token, provider/TURN và SDK diagnostic, không chỉ ILogger của API.

IP/user-agent trong phiên hiện có phục vụ màn hình thiết bị, khác bản sao audit: giữ theo vòng đời phiên và thời hạn chi tiết terminal được chọn. Không suy mốc IP audit thành xóa IP phiên đang hoạt động. Audit đề xuất không sửa bằng thao tác tay; job retention có phép redact trường nhạy cảm/purge hàng tới hạn và audit riêng số lượng/action, không sao chép trường vừa bị dọn.

Các TTL online tính từ timestamp nghiệp vụ, không reset khi restore, chuyển kho, retry hoặc copy. Backup vẫn có thể chứa trường/nội dung đã bị dọn online cho tới khi artefact hết tuổi; tuổi artefact không phải tuổi từng dữ liệu chứa bên trong. Dọn payload ứng dụng cũng không là bằng chứng mọi byte đã mất khỏi storage/WAL; catalogue và đường phục vụ phải được kiểm chứng riêng.

<a id="tuổi-backup-và-cửa-sổ-pitr"></a>

### Tuổi backup và cửa sổ PITR

DEC-109 chọn tuổi artefact tối đa 30×24 giờ, không kéo dài vì cần một anchor base cũ. Tuổi gốc lấy base backup start time/WAL segment close time trong catalogue được xác thực, không lấy thời điểm copy/upload mới để reset tuổi. Giữ base backup đã kiểm tra và chuỗi WAL tương ứng **trong giới hạn tuổi này**; khi một set hết hạn, bỏ set/các đoạn không còn dependency của set hợp lệ khác. Công cụ/topology cần chứng minh luôn có base mới hợp lệ trước set cũ hết hạn.

Mốc PITR sớm nhất là base hợp lệ sớm nhất có WAL liên tục, không tự bằng now−30 ngày. Backup hằng ngày có thể làm cửa sổ thực tế ngắn hơn 30 ngày; báo earliest/latest recoverable point và missing segment, không ghi “phục hồi mọi thời điểm 30 ngày” khi chỉ giữ artefact ≤30 ngày. Nếu chưa có base mới đúng hạn thì báo chưa đạt backup/RPO/RTO và xử lý, không tự giữ artefact quá tuổi hoặc xóa WAL còn cần bởi base hợp lệ.

<a id="account-state"></a>

<a id="3-khóa-tài-khoản-lịch-sử-và-định-danh"></a>

## 3. Khóa tài khoản, lịch sử và định danh

“Khóa tài khoản” ở DEC-104 là trạng thái suspended/disabled làm mất truy cập ứng dụng. Lockout 5 lần sai/15 phút DEC-064 chỉ là điều kiện đăng nhập tạm thời; không tự xóa phiên đang hợp lệ hoặc đổi thành khóa toàn tài khoản. Status/policy cụ thể và quyền người thao tác còn phải review.

| Actor / đối tượng | Đọc lịch sử | Tạo/gửi mới | Sửa/xóa tin của actor |
|---|---|---|---|
| Actor bị khóa | Không | Không | Không |
| Actor active, peer DM bị khóa | Có, đúng hội thoại của actor | Không tạo DM/gửi mới/gọi peer; tìm người không trả peer inactive | Có trên tin của chính actor; trạng thái peer không khóa thao tác này |
| Actor active trong channel được view; tác giả cũ bị khóa | Có | Tin mới của actor theo view và quyền gửi hiện hành | Chỉ tin actor là tác giả; không sửa/xóa thay tác giả bị khóa |
| Actor mất view/rời server hoặc channel deleted | Không | Không | Không qua scope đã mất quyền |
| Tài khoản vừa mở khóa | Sau đăng nhập/xác minh và kiểm tra lại quyền | Theo trạng thái hiện hành | Theo tác giả/quyền hiện hành; phiên cũ không sống lại |

Guard tách **actor dùng ứng dụng** khỏi **peer phải active để nhận tương tác mới**. Read history và author edit/delete không đòi peer DM còn active. Caller/callee mới và send mới phải đủ điều kiện; call đang chạy thu hồi theo DEC-099. Khóa tài khoản thu hồi toàn bộ session/token ứng dụng, stamp tăng, chat/media cutoff ≤5 giây từ commit; profile/tin/membership không bị xóa vì khóa.

Retry send đã commit với đúng khóa/nội dung ban đầu là đọc lại kết quả hiện hành khi actor còn quyền, không tạo tin hoặc outbox mới. Peer bị khóa không làm mất kết quả này; khóa chưa tồn tại vẫn bị chặn như gửi mới. Không dùng nội dung cũ từ receipt để trả lại bản đã sửa/xóa hoặc unavailable sau restore.

Projection lịch sử đề xuất thêm `IHistoricalUserSummaryReader` chỉ trả ID/username/displayName và trạng thái `unavailable` nếu cần UI, cho user đã là tác giả/participant của scope người đọc được phép biết. Existing `IUserDirectory` chỉ lọc Active nên không đủ cho lịch sử khi tác giả bị khóa. Không fallback thành UserNotFound làm mất cả trang tin; không đưa lý do khóa/email vào DTO. Search/DM mới vẫn dùng directory active và guard verify; không dùng projection lịch sử tìm mọi account bị khóa.

Không tự chuyển ownership, xóa server, thu hồi mọi lời mời do người bị khóa tạo hoặc phục hồi quyền khi mở khóa. Owner bị khóa vẫn là owner lưu trong DB; khả năng owner thao tác bị chặn, những quyền quản lý hợp lệ của thành viên khác vẫn theo Community. Xử lý tình huống owner không thể quay lại cần quy trình sản phẩm riêng, không tự cấp người vận hành quyền chuyển owner.

<a id="deletion"></a>

<a id="4-xóa-tin-xóa-phòng-và-dữ-liệu-tạm"></a>

## 4. Xóa tin, xóa phòng và dữ liệu tạm

Xóa tin của tác giả kiểm tra phiên/quyền, giữ khóa space/message/operation, đổi content=null và tăng version đúng một lần. Message ID/sequence/author/timestamps/tombstone và send fingerprint còn; payload outbox chỉ ID/version. Không dọn send-operation khiến retry cùng clientMessageId tạo tin mới. Legacy `message_edits.previous_content` không được writer v1 ghi; migration/seed/import phải loại bản cũ khỏi DB phục vụ trước dùng dữ liệu thật theo DEC-052.

Xóa phòng đánh dấu deleted/epoch mới trong transaction Community/Messaging/Media lifecycle đề xuất, chặn API/realtime/join/resume bằng guard hiện hành. Content trong room giữ theo DEC-105, không có đường “khôi phục phòng” hoặc “đọc kho lưu trữ” v1. Job dọn token/log không đi qua cascade để xóa `spaces/messages` của phòng này. Rời server/mất view chỉ thu hồi quyền và dọn role/override epoch cũ; không xóa tin mà người đó từng viết.

Chat/media receipt ngừng phục vụ sau cutoff không chứng minh byte cũ trong client đã biến mất. WebClient dọn draft/tin tạm/grant/local capture thuộc scope khi nhận thu hồi hoặc HTTP xác nhận mất quyền; server ngừng gửi nội dung mới độc lập dù client không hợp tác. Không hứa thu hồi ảnh chụp, clipboard hoặc dữ liệu người dùng đã tự sao chép. Token auth localStorage hiện tại không được chuyển thành lưu DM bền trong đợt này.

<a id="cleanup"></a>

<a id="5-worker-dọn-dữ-liệu-kỹ-thuật--thiết-kế-đề-xuất"></a>

## 5. Worker dọn dữ liệu kỹ thuật — thiết kế đề xuất

Chỉ áp dụng các TTL đã được chọn ở mục policy. Mỗi hàng có `terminal_at`/`effective_until`/`purged_at` rõ, UTC; eligibility `now >= retain_until`, không tính từ createdAt của job. Family refresh active giữ đầy đủ liên kết used/replaced để phát hiện reuse; chỉ khi cả session terminal và không còn nhiệm vụ cần dữ liệu thì mới dọn family theo thứ tự FK. Cooldown/last_issued_at/security stamp/deny marker không nằm trong payload được dọn.

Email envelope đề xuất bỏ ngay khi token không còn hợp lệ/delivery terminal; job nền quét ≤60 giây, không dùng mốc 7 ngày để giữ liên kết thô thêm. Payload provider không log; email đang in-flight không hứa thu hồi thư đã gửi nhưng link server từ chối. Không dọn outbox chưa ack/inbox đang xử lý/delivery chưa terminal chỉ vì tuổi 7 ngày. Media chưa quiesced vẫn giữ chỗ/command để kiểm soát, không xóa participation làm node cũ trở thành ghost.

Worker dự kiến chạy theo batch nhỏ, chọn candidate bằng index `(retain_until,id)`, claim lease có epoch; từng module khóa/đọc lại trạng thái trước purge. Expire một batch rồi crash/retry phải idempotent. Không lấy hết bảng hoặc log danh sách bản ghi/nội dung; số lượng, loại, cutoff và trace là đủ. Không gọi SFU/email provider trong transaction purge. Pending cleanup không sửa deadline bảo mật; token hết hạn bị từ chối dù worker chưa chạy. Đường đọc metadata/log/audit đề xuất lọc/redact ngay theo timestamp gốc khi tới hạn, không chờ job mới ẩn trường hết thời hạn. Job trễ phải báo và đo; lọc DTO không là bằng chứng payload đã dọn khỏi storage.

Tách payload khỏi marker: outbox/inbox/operation có thể bỏ payload/kết quả lớn nhưng giữ `operationId,eventId,resourceId,version,fingerprint/keyVersion,terminal/revoked` cần chống replay. Minimized marker chưa đặt TTL riêng nếu tài nguyên/tombstone hoặc tác dụng thu hồi còn cần; không gộp vào audit 90 ngày. Muốn dọn marker phải có phương án từ chối mọi ID cũ/generation cũ và quyết định riêng, không mặc định sau 7 ngày tạo lại được.

Đối chiếu PostgreSQL FK: nhiều bảng Messaging/Community dùng RESTRICT tới `identity.users`; refresh token có self-reference RESTRICT. Vì vậy worker không hard-delete user hoặc dọn token family bằng cascade giả định. Dọn `identity.account_tokens` không reset policy chống gửi lại; audit user_id không FK không có nghĩa được giữ dữ liệu vô hạn. Schema/index/migration/job vẫn chưa thực hiện.

<a id="restore"></a>

<a id="6-bảo-vệ-dữ-liệu-sau-restore--thiết-kế-cần-proof"></a>

## 6. Bảo vệ dữ liệu sau restore — thiết kế cần proof

Theo [PostgreSQL 18 PITR](https://www.postgresql.org/docs/18/continuous-archiving.html), phục hồi dùng base backup và chuỗi WAL, có thể chọn mốc trước hiện tại. Restore có thể đưa lại trạng thái/nội dung trước xóa hoặc thu hồi. Backup DB vì vậy không thể là nguồn duy nhất của sổ các hành động phải áp lại sau recovery point.

<a id="sổ-bảo-vệ-độc-lập"></a>

### Sổ bảo vệ độc lập

Đề xuất một kho độc lập với DB/timeline được restore, giữ record tối thiểu có chữ ký/hash chain và checkpoint: operation/event ID, kind, target user/session/message/space/channel/membership ID, authority generation, version/epoch floor, thời điểm và trạng thái prepared/committed/aborted. Không có body tin/email/token, không sao chép reason free text. Sổ không phải audit có TTL 90 ngày; marker có hiệu lực giữ tới khi không còn backup/generation/retry nào có thể phục hồi trạng thái cũ. Store/công cụ/quyền/key và công suất còn phải chọn.

Các hành động cần bảo vệ: delete/edit nội dung tin, revoke session/all-sessions/password-stamp, khóa tài khoản, leave/lost-view/member epoch, thay role/ACL/ownership làm đổi server accessVersion, channel deleted, revoke/cancel invitation và media terminal/generation fence. Edit chỉ ghi message ID và version floor; không giữ bản nội dung cũ hoặc mới trong sổ. Khi DB khôi phục có version thấp hơn floor, không được dùng nội dung thấp hơn như thể là bản mới nhất.

Community access floor đặt ở server/channel/membership phù hợp. Nếu chưa phục hồi được cấu hình role/ACL/owner tương ứng floor, giữ scope đó chưa mở cho đến khi đối chiếu dữ liệu tin cậy; không cho owner từ bản backup cũ bypass bước này. Dấu floor không đủ để tự dựng lại cấu hình quyền đã mất và không là thao tác gán quyền mới. Phải proof cả ownership transfer/role deny, không chỉ user leave. [Schema sổ bảo vệ](../contracts/data-protection.schema.json) là catalogue record mục tiêu, hash/signature/phase verification còn cần triển khai.

<a id="ghi-sổ-và-trường-hợp-kết-quả-không-rõ"></a>

### Ghi sổ và trường hợp kết quả không rõ

Thiết kế giao thức đề xuất, chưa chứng minh atomicity giữa hai kho:

1. Writer xác thực sơ bộ và tạo prepared intent bền trong kho độc lập trước mutation có tác dụng xóa/thu hồi/sửa. Intent ghi expected version/target, không coi prepared là mutation đã commit hoặc tự replay nó.
2. Trong transaction chính: kiểm tra/giữ guard và expectedVersion, thay đổi DB, ghi privacy-operation marker cùng outbox. Intent không thay kiểm tra quyền/transaction hiện tại. Rollback được xác nhận thì đánh dấu aborted; commit marker là bằng chứng kết quả DB.
3. Sau commit, hoàn tất receipt committed ở kho độc lập rồi trả thành công. Nếu response/receipt mất, thao tác có thể đã commit: đọc marker và retry cùng ID, không tạo một mutation mới hoặc thông báo “đã rollback” khi chưa biết. Thu hồi vẫn tính từ commit DB, không từ lúc sổ nhận receipt.
4. Restore gặp prepared chưa có kết luận phải đối chiếu marker/WAL/bằng chứng tin cậy. Nếu không xác nhận được committed hay aborted thì giữ scope liên quan chưa mở truy cập, không tự áp một intent chưa commit để xóa dữ liệu, cũng không tự coi nó aborted để phục hồi quyền. RTO 4 giờ phải được thử cả trường hợp này; chưa có proof thì chưa xác nhận đạt.

Outbox đơn thuần gửi sổ về sau không đủ: có cửa sổ DB đã xóa/thu hồi nhưng chưa ghi bền ngoài recovery point. API không được hứa đã hoàn tất bảo vệ restore khi receipt chưa bền. Kho sổ lỗi: từ chối mutation mới cần bảo vệ trước commit hoặc trả kết quả chưa rõ sau commit, không bỏ qua để “ghi sau”. Hạ tầng/race proof là điều kiện phát hành, không tuyên bố giao thức này đã chạy.

<a id="thứ-tự-khôi-phục-để-diễn-tập"></a>

### Thứ tự khôi phục để diễn tập

1. Đóng ingress, worker email/outbox và admission; không dùng restore DB chung production đang nhận ghi. Ghi recovery point, timeline/build/schema và watermark sổ độc lập trước sự cố.
2. Restore base/WAL vào môi trường cô lập; xác nhận chuỗi WAL/checksum/marker và cấu hình/key ring phù hợp. Không auto-send email/notification, không tự reconnect cuộc gọi cũ.
3. Kiểm tra tính đầy đủ sổ tới watermark; đối chiếu prepared chưa kết luận. Thiếu sổ/checkpoint/key hoặc còn scope mơ hồ thì giữ phần ảnh hưởng chưa mở, ghi rõ hạn chế và không tự ký đạt RTO.
4. Áp record committed idempotent: content deleted vẫn null/tombstone, channel deleted vẫn không view/join, invitations revoked vẫn invalid, epoch/stamp/floor không giảm. Với edit mất bản mới nhất, bỏ bản content thấp hơn floor và trả placeholder DEC-108. Chạy retention theo timestamp gốc, bỏ audit IP đã quá 7 ngày và payload đã tới hạn trước mở đường đọc; migration legacy không để bảng lịch sử sửa cũ quay lại serving. Không phát tin/outbox thấp hơn floor hoặc thông báo ringing cũ.
5. Thu hồi mọi auth session/token/link được phục hồi, bỏ email envelope và worker command cũ; user đăng nhập mới. Kết thúc mọi call/participation/source/grant cũ, fence node/room SFU và chứng minh không còn RTP trước tính lại capacity. Cache/registry/cursor generation đổi, không nhận quyền từ cache trước restore.
6. Kiểm tra quyền và tombstone qua API, thử token/cursor/epoch cũ bị từ chối; đối chiếu chống trùng/version và dữ liệu mất theo RPO. Chỉ mở lại sau tiêu chí đạt, ghi recovery point/watermark/replay count/quarantined scopes/kết quả/người xác nhận.

RPO cho phép thiếu một phần thay đổi sau recovery point theo mục tiêu đã chốt; không dùng RPO làm lý do phục vụ nội dung/quyền đã được sổ ghi là xóa/thu hồi. Sổ có thể cho biết message/version bị mất mà không có nội dung để dựng lại. Không dùng bản sửa cũ trong backup hoặc lấy từ log để tự bù body mới; bản body mới chỉ có thể phục hồi từ nguồn được phép thực sự có bản đó.

<a id="placeholder-và-version-sau-restore"></a>

### Placeholder và version sau restore

Schema DM/ChannelMessage bổ sung `contentState` tùy chọn: `available`, `deleted`, `unavailable_after_restore`. Thiếu field dùng quy tắc cũ theo deletedAt; **nhánh unavailable phải có field**, content=null và deletedAt=null, UI hiển thị đúng “Nội dung chưa khôi phục được”. Đây không phải tác giả bấm xóa; không bịa deletedAt hoặc đánh dấu đã xóa để giấu lý do dữ liệu mất. Realtime/history dùng cùng schema; client cũ chưa hiểu nhánh này phải được nâng cấp trước phát hành.

Migration đề xuất `messaging.messages.restore_redacted_at` và protection floor, constraint cho phép content=null khi restore redacted. Nếu backup version thấp hơn committed edit floor F, set version=F, null content và marker unavailable, giữ ID/sequence/author/createdAt; replay lặp không tăng version. Nếu recovered version ≥F và body đúng bản hiện hành thì giữ body, không lấy floor thấp hơn ghi đè. Delete committed thắng nhánh edit: content null, tombstone, version không dưới floor.

Tác giả active còn quyền được chủ động sửa tin unavailable bằng body mới hợp lệ/expectedVersion hiện hành, tăng version lớn hơn floor và xóa marker; hoặc xóa như tin thường. Người khác không được phục hồi/sửa thay tác giả; worker không dựng nội dung từ bản cũ. Realtime cache chỉ merge version ≥floor; cursor/generation trước restore không làm client nhận bản content cũ. Fixture và proof còn thuộc triển khai.

<a id="implementation"></a>

<a id="7-đối-chiếu-source-và-gói-triển-khai"></a>

## 7. Đối chiếu source và gói triển khai

| Hiện có | Chênh lệch / thiết kế đề xuất |
|---|---|
| `identity.users` có suspended/disabled/deleted; username unique ở mọi status | Không có API self-delete/suspend trong scope đã triển khai; không tự purge account theo tuổi |
| `IUserDirectory` chỉ Active | Cần projection lịch sử riêng để không làm mất trang tin của tác giả bị khóa; guard actor/read/send khác nhau |
| Audit có IP/user-agent/metadata và append-only comment | Cần allowlist metadata, redact/purge theo policy, không cho thao tác tay sửa audit |
| Account/session/refresh tables có expiry/revoke và FK | Chưa có retention worker/family cleanup, index/terminal marker và proof reuse/cooldown sau purge |
| `message_edits.previous_content` trong SQL/seed | Migration dữ liệu serving theo DEC-052; không tự xóa backup hoặc dùng SQL comment làm bằng chứng đã dọn |
| Outbox/inbox trong schema | Chưa có dispatcher/retention/receipt độc lập để chống replay sau purge/restore |
| Media vẫn chưa có module/provider | Terminal/draining và fencing theo [Media](../features/media/participation-lifecycle.md), không dọn theo TTL trước quiescence |

Nguồn: [schema.sql](../../database/postgres/schema.sql), [IdentityEnums](../../services/Modules/Identity/Domain/IdentityEnums.cs), [IdentityData](../../services/Modules/Identity/Infrastructure/IdentityData.cs), [UserDirectory](../../services/Modules/Identity/Infrastructure/Services/UserDirectory.cs).

| Gói | Đầu ra và proof cần có |
|---|---|
| DATA-GAP-01 | Review policy DEC-103–109/matrix/source mapping, cadence/budget dọn và phạm vi quyền người chạy job |
| DATA-GAP-02 | Projection lịch sử/guard theo DEC-104, mở khóa không sống lại phiên; actor edit/delete khi peer bị khóa |
| DATA-GAP-03 | Migration worker/index/terminal payload/dedup marker; batch rollback/crash, family FK/reuse và cooldown |
| DATA-GAP-04 | Chọn kho sổ/key/checkpoint, proof hai kho và prepared/committed/aborted/unknown; không có gap mất hành động bảo vệ |
| DATA-GAP-05 | Diễn tập restore/floor/generation/email/media/quota và trường hợp mất bản sửa; đo RPO/RTO, lưu bằng chứng |
| DATA-GAP-06 | Cấu hình backup/WAL/log/audit retention đúng policy, xác nhận cửa sổ có thể restore và dọn dependency an toàn |

<a id="acceptance"></a>

<a id="8-tiêu-chí-và-kiểm-thử"></a>

## 8. Tiêu chí và kiểm thử

Tất cả ca chưa chạy. Dữ liệu thử phải dùng tài khoản/tin giả; fixture chỉ là kỳ vọng và phép tính policy, không kết quả chạy DB/job/restore.

[data-lifecycle.json](../fixtures/data-lifecycle.json) có 22 vector retention, 12 vector truy cập, 15 vector restore, 10 trường hợp schema message và 6 kịch bản fault/transition. Record mẫu chỉ minh họa hình dạng; hash/signature giả không được dùng làm bằng chứng xác thực.

| Mã | Hành vi cần chứng minh |
|---|---|
| AC-DATA-01 | Không có API/UI self-delete hoặc purge account/server theo TTL chưa được chọn |
| AC-DATA-02 | Khóa account chặn actor, gửi/gọi mới tới peer; actor active vẫn đọc lịch sử đúng scope và sửa/xóa tin của mình |
| AC-DATA-03 | Mở khóa yêu cầu đăng nhập mới; phiên/token/stamp đã thu hồi không phục hồi |
| AC-DATA-04 | Phòng deleted giữ nội dung chưa đặt hạn purge nhưng mọi API/realtime/media bị chặn; không có restore phòng |
| AC-DATA-05 | Xóa tin content=null, tombstone/operation còn; retry cùng clientMessageId không tạo thêm tin |
| AC-DATA-06 | Dọn log/token/outbox không xóa tin live, phòng deleted hoặc marker chống trùng cần thiết |
| AC-DATA-07 | Token/cooldown/reuse còn đúng trước/sau cleanup; refresh family active không bị dọn token used |
| AC-DATA-08 | Worker đọc lại trạng thái, đúng biên UTC/cutoff, rollback/crash/retry không dọn bản ghi active/chưa ack |
| AC-DATA-09 | Không có nội dung/token/query nhạy cảm trong log/audit/sổ/provider diagnostic |
| AC-DATA-10 | Kho sổ lỗi/receipt mất tạo kết quả được đối chiếu, không tự coi prepared là committed hoặc bỏ qua marker chưa rõ |
| AC-DATA-11 | Restore áp lại delete/revoke/channel epoch/ACL/ownership floor trước mở; replay lặp không hạ version/quyền; scope chưa có cấu hình hiện hành không được owner cũ bypass |
| AC-DATA-12 | Mất sổ/checkpoint hoặc prepared không rõ thì scope chưa mở, không auto-xóa từ intent chưa commit |
| AC-DATA-13 | Restore không gửi email/ringing cũ; auth/media phiên cũ không vào lại; capacity chỉ nhả khi SFU cũ quiesced |
| AC-DATA-14 | RPO/RTO và thời hạn backup/log/audit được đo/cấu hình thực tế, không suy từ job tồn tại |
| AC-DATA-15 | Restore mất bản sửa mới nhất trả content=null/contentState unavailable/deletedAt null, giữ ID/sequence và version ≥floor; không gửi lại body cũ |
| AC-DATA-16 | Tác giả sửa/xóa tin unavailable bằng thao tác mới; người khác không được thay nội dung; replay restore không hạ body/version mới |

| Mã test | Tình huống | Dẫn chiếu |
|---|---|---|
| TC-DATA-01 | Suspend peer rồi đọc lịch sử, gửi/gọi, edit/delete tin actor; retry send đã commit và khóa mới; unlock thử token cũ | AC-DATA-02/03/05 |
| TC-DATA-02 | Delete channel, đẩy đồng hồ nhiều tháng, chạy cleanup, thử mọi đường đọc/join | AC-DATA-04/06 |
| TC-DATA-03 | Delete message rồi replay send key; cleanup outbox/receipt, restore và replay lại | AC-DATA-05/11 |
| TC-DATA-04 | Refresh family active có used token >7 ngày; family terminal trước/đúng/sau cutoff | AC-DATA-07/08 |
| TC-DATA-05 | Delivery/outbox đang xử lý, media draining và candidate đổi active trong khi job claim | AC-DATA-06/08 |
| TC-DATA-06 | Token trong URL/query/header/body/provider error, chat text trong reason; scan diagnostic | AC-DATA-09 |
| TC-DATA-07 | Fault từng bước prepared/DB commit/receipt/response/abort; duplicate intent/marker | AC-DATA-10/12 |
| TC-DATA-08 | Restore trước delete/revoke/leave/channel-delete/edit, replay floors hai lần | AC-DATA-11/12 |
| TC-DATA-09 | Restore có session/link/ringing/SFU còn sống, chặn RoomService rồi mở lại | AC-DATA-13 |
| TC-DATA-10 | Diễn tập base/WAL/catalogue dependency/retention, ghi RPO/RTO/watermark | AC-DATA-14 |
| TC-DATA-11 | Mất edit F, restore version thấp/equal/higher F, replay hai lần và thử schema UI | AC-DATA-15 |
| TC-DATA-12 | Author PATCH/DELETE tin unavailable, outsider thử, worker replay floor cũ sau edit mới | AC-DATA-16 |

<a id="gaps"></a>

<a id="9-đầu-vào-còn-mở"></a>

## 9. Đầu vào còn mở

DEC-103–109 đã chốt phạm vi/hành vi, TTL online và tuổi artefact backup/WAL. Tool/store, cửa sổ PITR thực tế, kho sổ/key ring, scope quyền worker, projection/placeholder migration và proof còn DATA-GAP. DEC-112 chốt quản trị kỹ thuật/no admin UI; người trực/thẩm quyền thao tác và người duyệt DEC-111 vẫn mở OQ-011. [Nghiệm thu/runbook](../releases/acceptance.md) có checklist và mẫu hồ sơ; chính sách dữ liệu không thay phân công. File này là nguồn chuẩn vòng đời, các đặc tả tính năng dẫn về đây.

# Contract Messaging v1 — P0-T02

Ngày: **19/09/2026**. Phụ thuộc [chính sách P0-T01](policies.md). Đây là **đặc tả thiết kế**, chưa phải API/Hub/interface đã triển khai. P0-T02 chỉ thay đổi tài liệu; không thêm endpoint rỗng hoặc sửa frontend trước khi có vertical slice.

Nguồn: [schema SQL](../../database/postgres/schema.sql), [quy ước lỗi](../api/error-handling.md), [kiến trúc](../architecture/overview.md), [API client](../../clients/WebClient/src/api.js), [App.jsx](../../clients/WebClient/src/App.jsx), [Contracts](../../services/SCDC.Contracts), [roadmap](../messaging-roadmap.md).

## 1. Quy ước chung

- HTTP prefix `/api/v1`, JSON camelCase; thành công trả DTO trực tiếp, không bọc `success/data`. Phân trang trả DTO page có `items`.
- HTTP xác thực bằng bearer token; user/session lấy từ identity đã xác thực, không từ request body. Kiểm tra eligibility, membership và quyền theo P0-T01 ở mỗi use case.
- ID là chuỗi UUID; timestamp là ISO 8601 UTC có hậu tố `Z`; trường nullable luôn có mặt với `null`, collection không có phần tử trả `[]`. Không serialize EF entity, object storage key, token hay metadata nội bộ.
- Enum HTTP dùng số đã chốt ở P0-T01, không suy diễn từ chuỗi label UI. Unknown enum trong request bị từ chối; response type chưa hỗ trợ không được render như text thông thường.
- Nội dung chuẩn hóa/giới hạn theo P0-T01; unknown request field bị từ chối để tránh client tưởng dữ liệu đã được lưu. Các field tương lai chỉ nhận khi slice đã hỗ trợ.
- `sequenceNo`, `lastMessageSequence`, `lastReadSequence`, `highWatermark` và mọi cursor sequence đều là **chuỗi thập phân**, tuyệt đối không JSON number. `version`/count/limit dùng integer trong phạm vi an toàn của JSON/JavaScript; count v1 giới hạn tối đa 2.147.483.647.
- Tất cả sequence dùng dạng chuẩn `0` hoặc `[1-9][0-9]*`, tối đa `9223372036854775807`; message sequence thực phải >0. `0` chỉ cho watermark space rỗng và `afterSequence` bắt đầu. Không nhận dấu, khoảng trắng, số mũ, số thực, leading zero. Backend parse Int64; FE dùng `BigInt` để so sánh rồi giữ string trong state/JSON, không dùng `Number` hoặc so sánh từ điển.
- `message.version` chỉ thứ tự thay đổi **message đó**, không phải thứ tự stream hoặc quyền. `eventId`, `message.id`, `clientMessageId` là ba định danh khác nhau.

## 2. Route và request/response cốt lõi — P0-T02.1/.2

Một route message dùng chung DM/group/channel; `spaceId` không mặc nhiên là channel. `/channels/{id}/messages` trong client cũ phải chuyển sang route dưới ở P2, không duy trì hai backend send handler.

| Use case | Method + route sau prefix | Request | Thành công | Slice |
|---|---|---|---|---|
| Tra cứu chính xác người nhận DM | `GET /conversations/direct/recipient?username=...` | Query `username`, không hỗ trợ tìm kiếm gần đúng/toàn bộ user | `200` UserSummaryDto | P1 |
| Tạo/lấy DM | `POST /conversations/direct` | `{recipientUserId}` | Mới: `201` + SpaceSummaryDto và Location `/api/v1/spaces/{id}`; có sẵn: `200` cùng DTO | P1 |
| Danh sách hội thoại | `GET /spaces?limit=50&cursor=...` | Query, cursor tùy chọn | `200` SpacePageDto | P1 |
| Chi tiết space | `GET /spaces/{spaceId}` | Không body | `200` SpaceSummaryDto | P1 |
| Gửi tin | `POST /spaces/{spaceId}/messages` | SendMessageRequest | Mới: `201` MessageDto + Location message; retry hợp lệ: `200` DTO hiện hành | P2 |
| Đọc một tin | `GET /spaces/{spaceId}/messages/{messageId}` | Không body | `200` MessageDto hoặc tombstone | P2 |
| History/tải bù | `GET /spaces/{spaceId}/messages` | Quy tắc query mục 4 | `200` MessagePageDto | P2 |
| Sửa tin | `PATCH /spaces/{spaceId}/messages/{messageId}` | `{content, expectedVersion}` | `200` MessageDto | P6 |
| Xóa tin | `DELETE /spaces/{spaceId}/messages/{messageId}?expectedVersion=...` | Không body; version bắt buộc | `204`; xóa lặp có quyền vẫn `204` | P6 |
| Read state cá nhân | `PUT /spaces/{spaceId}/read-state` | `{lastReadSequence}` | `200` `{spaceId,lastReadSequence,lastReadAt}` | P5 |

Routes group/membership, reaction/pin, search, block/report và file theo mục 14 roadmap là phạm vi phase sau, **chưa chốt toàn bộ request/response trong P0-T02**. DTO nền dưới có điểm mở rộng để các slice đó không phải suy đoán dữ liệu từ mock.

### SendMessageRequest

| Field | Kiểu / bắt buộc | Rule |
|---|---|---|
| `clientMessageId` | UUID / có | Sinh một lần cho thao tác gửi; giữ nguyên khi retry |
| `messageType` | integer / có | 1 Text; 3 Attachment chỉ từ P7; 2/4 từ client bị từ chối |
| `content` | string hoặc null / có | Text phải không rỗng sau chuẩn hóa; Attachment có thể null |
| `replyToMessageId` | UUID hoặc null / không | Default null; chỉ từ P6; phải đọc được và cùng space |
| `threadRootId` | UUID hoặc null / không | Default null; chỉ từ P6; root không phải thread reply |
| `attachmentIds` | UUID[] / không | Default []; chỉ từ P7; ID upload đã complete, chưa gắn vào tin khác |

Không nhận `spaceId`, `authorUserId`, timestamp, version, sequence hoặc metadata tự do trong body. Cả reply và thread có thể xuất hiện: nếu cùng có, reply phải là root hoặc một reply thuộc đúng thread; không trộn hai thread. Plain reply ngoài thread chỉ có `replyToMessageId`.

Ví dụ gửi Text vào space `01990000-0000-7300-8000-000000000001`:

```json
{
  "clientMessageId": "21990000-0000-4000-8000-000000000001",
  "messageType": 1,
  "content": "Chào bạn!",
  "replyToMessageId": null,
  "threadRootId": null,
  "attachmentIds": []
}
```

**Idempotency:** key là `(spaceId, actorUserId, clientMessageId)`. Fingerprint gồm nội dung sau chuẩn hóa, message type, reply/thread ID và tập attachment ID chuẩn hóa theo UUID (không duplicate). Cùng key khác fingerprint trả `Messaging.IdempotencyConflict`. Cùng key cùng fingerprint trả bản ghi hiện hành, kể cả tombstone nếu đã xóa, không tạo outbox/event mới. Quyền hiện hành và block được kiểm tra trước retry lookup; retry không vượt quyền vừa bị thu hồi.

Fingerprint **không được tính lại từ content hiện hành** vì tin có thể đã sửa/xóa. P2 lưu SHA-256 của payload gốc ở `messages.idempotency_payload_hash` cùng transaction với message, space projection và outbox; hash không chứa plaintext cùng tombstone. Text v1 canonical hóa message type và content đã chuẩn hóa; reply/thread/attachment sẽ được thêm vào canonical payload khi slice tương ứng được triển khai. Attachment upload ID trở thành attachment ID khi gắn message; mapping/staging chưa có và thuộc P7.

## 3. DTO cốt lõi — P0-T02.2/.3

### MessageDto

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `id`, `spaceId` | UUID | ID server và không gian |
| `clientMessageId` | UUID hoặc null | Null cho System; dùng đối soát optimistic message cùng actor/space |
| `sequenceNo` | decimal string >0 | Thứ tự toàn cục PostgreSQL |
| `messageType` | integer | 1/2/3 theo policy |
| `author` | UserSummaryDto hoặc null | `{id, username, displayName}`; System null, không kèm email/status account |
| `content` | string hoặc null | Null cho tombstone hoặc attachment không caption |
| `version` | integer >=1 | Tăng khi sửa/xóa message; không tăng chỉ vì reaction/pin |
| `createdAt`, `editedAt`, `deletedAt` | UTC string; hai field sau nullable | Timestamp server |
| `replyToMessageId`, `threadRootId` | UUID hoặc null | Chỉ quan hệ ID, không nhúng bản sao nội dung có thể bị xóa |
| `attachments` | AttachmentSummaryDto[] | Rỗng trước P7 và với tombstone |
| `reactions` | ReactionSummaryDto[] | Rỗng trước P6 và với tombstone |
| `isPinned` | boolean | Pin message chung; false trước P6/với tombstone |
| `threadCount` | integer >=0 | Số reply chưa xóa; 0 trước P6; không chứa nội dung reply |

AttachmentSummaryDto: `{id: UUID, name: string, mimeType: string, sizeBytes: decimal string, width: integer|null, height: integer|null}`; `sizeBytes` >0, kích thước ảnh >0 nếu có. Chỉ trả file Clean được phép xem; URL tải được lấy riêng sau authorization, không nhúng URL ký vào event lưu lâu. ReactionSummaryDto: `{reactionKey: string, count: integer, reactedByMe: boolean}` — đây là dữ liệu **theo người xem**, không broadcast cùng một DTO cho mọi user. Mention rendering/summary sẽ bổ sung có version ở P6, không lấy metadata thô làm contract.

Ví dụ response `201` (sequence cố ý vượt Number.MAX_SAFE_INTEGER để làm mẫu kiểm tra):

```json
{
  "id": "01990000-0000-7700-8000-000000000001",
  "spaceId": "01990000-0000-7300-8000-000000000001",
  "clientMessageId": "21990000-0000-4000-8000-000000000001",
  "sequenceNo": "9007199254740993",
  "messageType": 1,
  "author": {
    "id": "01990000-0000-7000-8000-000000000001",
    "username": "alice",
    "displayName": "Alice"
  },
  "content": "Chào bạn!",
  "version": 1,
  "createdAt": "2026-09-19T09:00:00Z",
  "editedAt": null,
  "deletedAt": null,
  "replyToMessageId": null,
  "threadRootId": null,
  "attachments": [],
  "reactions": [],
  "isPinned": false,
  "threadCount": 0
}
```

Tombstone giữ id, spaceId, sequence, type, version, author và timestamps/quan hệ ID; `content=null`, `attachments=[]`, `reactions=[]`, `isPinned=false`. Không trả chuỗi `[deleted]` trong DB hoặc lịch sử content. FE render theo `deletedAt`. `threadCount` vẫn có thể đếm reply còn đọc được; không làm mất thread khi root bị xóa.

### SpaceSummaryDto và inbox

| Field | Kiểu / rule |
|---|---|
| `id`, `spaceType`, `status`, `version` | UUID, enum số, enum số, integer >=1 |
| `name` | string; DM lấy tên người đối thoại, group/channel lấy tên chính thức |
| `peer` | UserSummaryDto cho DM; null cho group/channel |
| `serverId` | UUID cho channel; null cho DM/group |
| `lastMessage` | MessagePreviewDto hoặc null; `{id,sequenceNo,messageType,author,content,deletedAt}`; preview tối đa 160 code point và null content nếu đã xóa |
| `lastMessageSequence`, `lastActivityAt` | decimal string hoặc null; UTC string hoặc null khi chưa có tin |
| `lastReadSequence`, `unreadCount` | decimal string hoặc null; integer >=0; trước P5 lần lượt null/0, không thể hiện đã có unread thật |
| `preferences` | `{notificationLevel: integer, mutedUntil: UTC|null, isHidden: boolean, isPinned: boolean}`; mặc định 2/null/false/false khi chưa có state |
| `capabilities` | `{canRead,canSend,canEditOwn,canDeleteOwn,canDeleteOthers,canPin,canReact,canAttach}` đều boolean theo actor; gợi ý UI, backend vẫn kiểm tra mỗi thao tác |

`GET /spaces` trả inbox **DM/group** còn đọc được, chưa Deleted, không hidden; channel nằm trong danh sách Community và dùng `GET /spaces/{id}` khi mở. Query thêm `includeHidden=true` để quản lý hội thoại ẩn. `limit` mặc định 50, khoảng 1–100; sort `last_activity_at DESC NULLS LAST, id DESC`, không dùng flag pin vào sort cursor v1; FE có thể nhóm các hội thoại đã tải theo pin.

SpacePageDto là `{items: SpaceSummaryDto[], nextCursor: string|null, hasMore: boolean}`. Cursor opaque do server phát, gắn version/filter/order key `(lastActivityAt nullable, id)`; chỉ dùng lại với cùng filter. Không phải token cấp quyền. Cursor sai/chỉnh sửa trả `Messaging.InvalidCursor`; keyset có activity thay đổi không hứa snapshot cố định: FE deduplicate ID và refresh trang đầu khi inbox thay đổi. Cơ chế encode/validate nằm ở P1, không buộc FE decode.

Ví dụ response tạo DM `201` trước khi có tin:

```json
{
  "id": "01990000-0000-7300-8000-000000000001",
  "spaceType": 1,
  "status": 1,
  "version": 1,
  "name": "Bob",
  "peer": {"id": "01990000-0000-7000-8000-000000000002", "username": "bob", "displayName": "Bob"},
  "serverId": null,
  "lastMessage": null,
  "lastMessageSequence": null,
  "lastActivityAt": null,
  "lastReadSequence": null,
  "unreadCount": 0,
  "preferences": {"notificationLevel": 2, "mutedUntil": null, "isHidden": false, "isPinned": false},
  "capabilities": {"canRead": true, "canSend": true, "canEditOwn": true, "canDeleteOwn": true, "canDeleteOthers": false, "canPin": true, "canReact": true, "canAttach": true}
}
```

Capabilities thể hiện quyền nghiệp vụ; tính năng chưa có phải còn được kiểm soát bằng khả năng hỗ trợ của bản client/server, ví dụ canAttach=true không có nghĩa P7 đã triển khai. Với DM bị block, canSend/canEditOwn/canPin/canReact/canAttach=false cho thao tác tạo mới; ngoại lệ cleanup unpin/bỏ reaction/xóa vẫn theo policy và kiểm tra riêng.

## 4. Message cursor, ordering và reconnect — P0-T02.2/.3

`limit` mặc định 50, hợp lệ 1–100. Query:

- Không before/after: lấy tối đa limit tin mới nhất; trả items **tăng dần sequence** để FE render. nextBeforeSequence là sequence nhỏ nhất trang khi còn trang cũ.
- `beforeSequence=S`: chỉ lấy sequence <S, lấy limit tin gần S nhất rồi trả tăng dần. Không yêu cầu S còn là message đang hiện; cursor là biên số.
- `afterSequence=S`: lấy sequence >S theo tăng dần, có `throughSequence=H` tùy chọn để chặn trên. Lần đầu server chọn H là sequence lớn nhất đã commit của space (0 nếu rỗng); các trang tiếp theo dùng **cùng H**.
- before và after cùng xuất hiện hoặc through không đi cùng after: `400`. after >through: `400`. S có thể bằng H và trả trang rỗng. Request `lastReadSequence` thì khác: phải trỏ tới message thực thuộc space, không chấp nhận biên tùy ý.
- History/tải bù bao gồm tombstone và thread reply để stream sequence không bỏ sót; FE phân nhóm thread khi render timeline chính. P5 tính unread theo policy đã chốt cho thread, không lấy số phần tử tải bù làm unread.

MessagePageDto: `{items: MessageDto[], hasMore: boolean, nextBeforeSequence: string|null, nextAfterSequence: string|null, highWatermark: string}`. Chỉ cursor đúng hướng có giá trị khi hasMore=true; hướng kia luôn null. highWatermark của tải bù giữ H; history trả max committed sequence tại lúc đọc, không dùng nó để bỏ qua các trang chưa tải.

Ví dụ tải bù đã đến cuối: `GET /api/v1/spaces/01990000-0000-7300-8000-000000000001/messages?afterSequence=9007199254740993&throughSequence=9007199254740993&limit=50`:

```json
{
  "items": [],
  "hasMore": false,
  "nextBeforeSequence": null,
  "nextAfterSequence": null,
  "highWatermark": "9007199254740993"
}
```

**Cam kết ordering cần thực thi ở P2:** mọi writer trong cùng space, kể cả System, phải khóa/tuần tự hóa cùng space **trước khi cấp sequence** và giữ đến commit. Nhờ đó max sequence đã commit trong space là watermark an toàn; transaction rollback tạo gap hợp lệ. Không giả định IDENTITY tự bảo đảm thứ tự commit. Nếu đổi cơ chế writer, phải giữ cam kết này hoặc thiết kế cursor mới trước khi triển khai.

**Reconnect:** đăng ký listener → subscribe space → buffer event → tải bù từ cursor đã đồng bộ đầy đủ với H cố định qua mọi trang → reload MessageDto của phần đang hiển thị để nhận sửa/xóa/reaction/pin cũ → hợp nhất event buffered → chuyển sang nhận trực tiếp. Chỉ nâng catch-up cursor sau khi đã tải đủ trang đến H; event đến lẻ tẻ không được nâng cursor này làm bỏ sót tin khác. Lần mở đầu chưa có cache dùng history trang mới nhất và phân trang cũ khi cuộn.

HTTP response và realtime cùng một message: merge theo message ID; optimistic match bằng `(spaceId, author.id, clientMessageId)`. So sánh message version khi thay content/tombstone; phản hồi version cũ không ghi đè version mới. Reaction/pin chưa có version riêng: sự kiện liên quan chỉ invalidation, serialize/coalesce refetch từng message và bỏ response cũ theo request generation. `afterSequence` không thể phục hồi riêng các edit/delete của tin cũ; bắt buộc reload snapshot khi reconnect. API là nguồn dữ liệu bền vững, realtime không phải kho event replay cho client.

## 5. Lỗi và concurrency — P0-T02.2

Các mã dưới là **đề xuất mới** sẽ triển khai bằng Result/Error tại slice tương ứng. Lỗi auth/validation middleware hiện có vẫn theo error convention của host; không đổi mã Identity chỉ để có prefix Messaging.

| HTTP / ErrorType | errorCode nghiệp vụ | Trường hợp / hành vi FE |
|---|---|---|
| 400 Validation | `Messaging.ValidationFailed` | Body/enum/content/limit không hợp lệ; errors theo camelCase field |
| 400 Validation | `Messaging.InvalidCursor` | Cursor sai định dạng/range/filter; tải lại trang đầu |
| 400 Validation | `Messaging.SelfConversationNotAllowed` | Tự mở DM |
| 401 Unauthorized | Theo authentication host | Refresh một lần theo api.js; thất bại thì kết thúc session |
| 403 Forbidden | `Messaging.AccountUnavailable` | Actor không Active; không trả dữ liệu |
| 403 Forbidden | `Messaging.DirectConversationUnavailable` | Target không tồn tại/không Active hoặc block; cùng phản hồi không tiết lộ nguyên nhân |
| 403 Forbidden | `Messaging.ActionNotAllowed` | Có quyền đọc nhưng thiếu quyền hành động, hoặc bị block trên DM đã có |
| 404 NotFound | `Messaging.ResourceNotFound` | Resource thiếu/Deleted/không được đọc; cùng body chung |
| 409 Conflict | `Messaging.SpaceNotWritable` | Archived, khi thao tác không thuộc ngoại lệ |
| 409 Conflict | `Messaging.MessageDeleted` | Sửa/reply/pin tin đã xóa |
| 409 Conflict | `Messaging.VersionConflict` | Expected version cũ; GET lại trước khi quyết định retry |
| 409 Conflict | `Messaging.IdempotencyConflict` | Cùng clientMessageId nhưng payload khác; không tự retry bằng ID mới |
| 429 TooManyRequests | `Messaging.RateLimited` | Tôn trọng header Retry-After nếu có |
| 503 ServiceUnavailable | `Messaging.DependencyUnavailable` | Dependency không sẵn sàng; không coi thiếu quyền là lỗi tạm thời |

Ví dụ validation, content-type `application/problem+json`:

```json
{
  "type": "https://scdc.dev/problems/validation",
  "title": "Validation failed.",
  "status": 400,
  "detail": "Nội dung tin nhắn không hợp lệ.",
  "instance": "/api/v1/spaces/01990000-0000-7300-8000-000000000001/messages",
  "errorCode": "Messaging.ValidationFailed",
  "traceId": "contract-example-trace",
  "errors": {"content": ["Nội dung phải có từ 1 đến 10000 code point sau chuẩn hóa."]}
}
```

Sửa dùng expectedVersion >=1 trong body; DELETE dùng query để tránh body DELETE. Xóa tin đã xóa trả 204 sau authorization dù expectedVersion cũ; sửa tin đã xóa trả MessageDeleted. Khi tin chưa xóa, mismatch version trả VersionConflict. Không retry PATCH bằng nội dung cũ tự động. Mất response POST: retry đúng key/payload, giữ trạng thái pending/failed đến khi server xác nhận.

## 6. Hub và event envelope — P0-T02.4

Hub đề xuất `/hubs/chat`; authenticated connection gắn userId/sessionId từ claims, không nhận hai ID này từ client. JWT hiện có claim `sid`. Server kiểm tra session khi connect/subscribe/dispatch và xử lý revoke; token refresh không tự phục hồi membership đã mất.

| Hub method | Request | Kết quả |
|---|---|---|
| `SubscribeSpace` | `spaceId: UUID` | HubResult với value `{spaceId, highWatermark}`; lặp không tạo subscription trùng |
| `UnsubscribeSpace` | `spaceId: UUID` | HubResult với value `{spaceId}`; idempotent, không tiết lộ có space không |

HubResult là union: thành công `{ok:true,value:object,error:null}`; lỗi `{ok:false,value:null,error:{errorCode,message,traceId}}`. Không trả HTTP ProblemDetails cho invocation; negotiate/connect HTTP vẫn dùng authentication host. Unknown exception chỉ trả lỗi chung và traceId. Ví dụ subscribe lỗi:

```json
{
  "ok": false,
  "value": null,
  "error": {"errorCode": "Messaging.ResourceNotFound", "message": "Không tìm thấy tài nguyên.", "traceId": "hub-example-trace"}
}
```

**Envelope chung** cho client event: `{eventId,eventType,schemaVersion,spaceId,occurredAt,aggregateVersion,payload}`. eventId là UUID của outbox với event bền vững; typing/control tạm thời sinh ID riêng. schemaVersion=1 là phiên bản payload, khác aggregateVersion. aggregateVersion nullable, chỉ dùng khi event gắn version message/aggregate cụ thể; không suy ra thứ tự toàn bộ space. occurredAt UTC là lúc mutation xảy ra, không phải thời điểm retry phát.

**Quyết định v1:** MessageCreated/Updated/Deleted đều là **thông báo tham chiếu**, không chứa MessageDto. Client gọi GET message có authorization để lấy trạng thái mới nhất; event có thể đến sau sửa/xóa. Cách này không broadcast `reactedByMe` của user khác, giảm bản sao nội dung trong outbox và tránh hydrate payload cũ sau purge. P3 cần coalesce request và đo tải; batch-get là mở rộng sau nếu số đo yêu cầu.

| eventType | payload v1 | Audience và xử lý |
|---|---|---|
| `MessageCreated` | `{messageId, sequenceNo}` | Subscriber còn quyền; refetch DTO, merge optimistic |
| `MessageUpdated` | `{messageId, sequenceNo}` | Subscriber còn quyền; refetch, giữ version mới nhất |
| `MessageDeleted` | `{messageId, sequenceNo, deletedAt}` | Subscriber còn quyền; có thể dựng tombstone ngay bằng aggregateVersion; refetch để xác nhận |
| `SpaceUpdated` | `{}` | Các connection của user còn quyền có space trong inbox; invalidation SpaceSummaryDto/trang đầu, không broadcast user preferences |
| `SpaceAccessRevoked` | `{}` | Chỉ user bị revoke với subscription đã biết; bỏ cache và subscription; không nội dung message |
| `SessionRevoked` | `{}`; `spaceId=null` | Chỉ connection của session bị revoke, rồi disconnect; không coi event này là biện pháp bảo vệ duy nhất |

Message event bắt buộc spaceId !=null, aggregateVersion integer >=1. SpaceUpdated/SpaceAccessRevoked/SessionRevoked dùng aggregateVersion=null, refetch quyền hiện hành thay vì sắp thứ tự bằng timestamp. Event user-targeted không được đưa vào group broadcast toàn space. SpaceUpdated đi tới các thiết bị đủ quyền ngay cả khi chưa subscribe timeline, bảo đảm inbox nhận tin mới.

Ví dụ một lần tạo message và event tương ứng (event ID khác message ID):

```json
{
  "eventId": "01990000-0000-7900-8000-000000000001",
  "eventType": "MessageCreated",
  "schemaVersion": 1,
  "spaceId": "01990000-0000-7300-8000-000000000001",
  "occurredAt": "2026-09-19T09:00:00Z",
  "aggregateVersion": 1,
  "payload": {"messageId": "01990000-0000-7700-8000-000000000001", "sequenceNo": "9007199254740993"}
}
```

Outbox cùng transaction nghiệp vụ; retry giữ eventId, giao nhận ít nhất một lần. FE dùng cache eventId có giới hạn bộ nhớ và idempotent merge theo message ID/version; không giả định cache giữ vô hạn. Published không có nghĩa delivered/read. Không dựa vào thứ tự event tới. Event không được hỗ trợ về schemaVersion/type: không áp dụng payload, đánh dấu space cần resync; log metadata tối thiểu.

ReadStateUpdated/ReactionChanged/PinChanged/MembershipChanged/TypingChanged thuộc phase sau: dùng cùng envelope nhưng payload sẽ chốt theo slice. Reaction/pin invalidation không dùng message.version để giả lập revision; typing là best-effort có TTL, không ghi outbox. P3 phải đổi listener cũ đang coi MessageCreated payload là MessageDto và đổi `SubscribeChannel` sang `SubscribeSpace`.

## 7. Ownership và contract liên module — P0-T02.5/.6

| Module/adapter | Sở hữu và trách nhiệm | Không được làm |
|---|---|---|
| Identity | User profile/eligibility, session validity, logout/revoke | Quyết định quyền channel/DM membership |
| Community | Server/member/channel/role/override; tính quyền hiệu lực và lifecycle channel | Ghi thẳng bảng message/space của Messaging |
| Messaging | Space, DM/group membership, message, interactions, block/read state và realtime delivery | Đọc Identity/Community DbContext để bypass contract |
| API host | HTTP DTO/ProblemDetails, claims adapter, DI và orchestration kỹ thuật | Nhét domain rule/SQL vào controller |
| Hạ tầng integration | Transaction enlistment, outbox/inbox/dispatch | Coi publish thành xác nhận người dùng đã nhận/đọc |

HTTP DTO và application models đặt trong adapter/module tương ứng, **không đưa toàn bộ HTTP MessageDto vào SCDC.Contracts**. SCDC.Contracts chỉ interface và DTO giao tiếp liên module, không chứa EF/DbTransaction/ClaimsPrincipal/HTTP types. BuildingBlocks mới là nơi abstraction kỹ thuật chung khi có nhu cầu.

### Contract đã có và quyết định mở rộng

| Contract | Hiện tại | Quyết định thiết kế mới / slice |
|---|---|---|
| `IUserDirectory` | FindById/Username/Ids, UserSummary(Id,Username,DisplayName); Identity có implementation | Giữ nguyên để hiển thị/batch user; không coi null/non-null là Active |
| `IChannelAccessChecker` | CheckAsync(userId,spaceId) → CanRead/CanSend; chưa implementation Community | Mở rộng ChannelAccessDecision thêm CanEditOwn, CanDeleteOthers, CanPin, CanReact, CanAttach; giá trị mặc định false. Quyền cơ sở từ Community, Messaging áp dụng lifecycle/block/type/ownership tiếp |
| `IRealtimeAccessRevoker` | RevokeAsync(userId,spaceIds); chưa implementation Messaging | Giữ nghĩa revoke **đúng các space**; collection rỗng là no-op, không phải logout-all |
| `IUserMessagingEligibility` | Chưa có | Identity cung cấp CheckAsync(userId,ct) → `{UserId, CanUseMessaging}`; false chung cho missing/non-Active, không public trạng thái tài khoản |
| `IIdentitySessionValidator` | Chưa có | Identity cung cấp ValidateAsync(userId,sessionId,securityStamp,ct) → `{IsValid}`; kiểm tra session owner, expiry/revoke và stamp từ claims, không nhận raw token qua contract |
| `IRealtimeSessionRevoker` | Chưa có | Messaging cung cấp RevokeSessionAsync(userId,sessionId,ct), RevokeUserAsync(userId,ct); idempotent, tác động đúng session hoặc tất cả connection user |
| `IChannelSpaceProvisioner` | Chưa có | Messaging cung cấp EnsureAsync(spaceId,creatorUserId,ct), ChangeStatusAsync(spaceId,targetStatus,expectedVersion,ct) cho workflow Community nội bộ; type cố định Channel |

Các chữ ký trên là mô tả, **chưa thêm file C# hay registration**. DTO lỗi liên module dùng outcome có kiểu riêng, ví dụ provision trả `{SpaceId,Version,Created,Failure}` với Failure enum None/Conflict/NotFound/InvalidState; không HTTP status, không exception cho lỗi nghiệp vụ dự kiến. SCDC.Contracts hiện chưa tham chiếu BuildingBlocks; không tự thêm dependency chỉ để trả Result. API/application adapter ánh xạ outcome sang Result khi triển khai.

**Eligibility/session:** fail closed khi không xác minh được; dependency lỗi trả 503, không biến thành user không tồn tại. Không cache positive eligibility/session vô hạn. Identity là nguồn quyết định; revoke kết nối là bước dọn transport, không thay validation. Cần kiểm thử policy cùng race gửi/revoke ở P3, không hứa atomicity chỉ nhờ hai lần gọi interface.

**Channel capability mapping:** CanRead=read_messages; CanSend=CanRead && send_messages; CanEditOwn=CanSend && edit_own_messages; CanDeleteOthers=CanRead && delete_messages; CanPin=CanRead && pin_messages; CanReact=CanRead && add_reactions; CanAttach=CanSend && attach_files. CanDeleteOwn là rule Messaging dựa quyền đọc, không yêu cầu quyền xóa người khác. Decision.Denied phải đặt tất cả false. Không suy pin từ CanSend; thêm seed/SQL nâng cấp permission mới ở P4/P6, mặc định deny.

**Cấp space cho channel:** Community tạo UUID spaceId một lần cho logical create và dùng làm khóa idempotency nội bộ. API/application workflow chạy **một transaction PostgreSQL** để gọi provisioner tạo space loại 3 rồi Community tạo channel có FK. Hai persistence context enlist cùng transaction thông qua coordinator kỹ thuật ở host/BuildingBlocks, không truyền EF transaction qua Contracts. Lỗi bất kỳ rollback cả hai; không commit space trước rồi hy vọng channel thành công. Ensure trả lại space cùng ID/type/creator đã có; khác type/creator là Conflict, không nhận quyền user tạo lại tùy ý. External create-channel retry cần giữ logical ID trong idempotency workflow Community, sẽ chốt ở P4.

**Lifecycle/revoke:** Community cập nhật quyền/membership/channel và ghi event revoke trong cùng transaction. Sau commit gọi revoker để thu hồi nhanh, outbox retry để phục hồi khi process crash. Identity làm tương tự với session/account và IRealtimeSessionRevoker. Revoke không có connection vẫn thành công; mọi dispatch/HTTP đều phải kiểm tra nguồn quyền hiện hành nên sự chậm trễ outbox không trở thành quyền được đọc. Với revoke chỉ quyền gửi, giữ read subscription và gửi SpaceUpdated để UI tải lại capability; không gọi revoke toàn quyền đọc sai phạm vi.

## 8. Tác động dữ liệu và checklist chuyển giao

| Thiếu hiện tại | Task tiếp nhận | Điều kiện trước nghiệm thu |
|---|---|---|
| Original send fingerprint trong messages | P2-T01 | Đã lưu SHA-256 payload text canonical cùng transaction; P6/P7 phải mở rộng canonical payload trước khi nhận reply/thread/attachment |
| Serialization bigint chưa áp dụng DTO | P2 | OpenAPI field string có pattern/range; test roundtrip giá trị >2^53 |
| Lock ordering/cursor watermark chưa có | P2 | Tất cả writer cùng cơ chế, test commit đảo thứ tự/rollback |
| DTO/query và FE mapping vẫn mock | P1–P3 | Route spaces, optimistic key cố định, listener dùng reference event |
| Channel permissions và Identity/session extension | P1/P3/P4/P6 | Contract typed, deny mặc định, không truy cập DbContext chéo module |
| Upload staging không có | P7 | Ownership và ID mapping không nhầm attachment đã gắn với upload chưa gửi |
| Provision transaction coordinator chưa có | P4 | Cùng PostgreSQL transaction thật, không orphan khi lỗi; không làm sẵn framework ở P0 |

Ca kiểm tra contract cần thực hiện trong slice: retry gửi sau edit/delete; JSON sequence 9007199254740993 còn nguyên; before/after loại trừ nhau; trang catch-up có H cố định; event tới trước HTTP/đến lặp; tombstone không lộ preview; reaction DTO không dùng chung reactedByMe; revoke một session không logout thiết bị khác; revoke rỗng không thành logout-all; provision channel lỗi rollback cả hai module. Đây là **test plan**, không báo runtime pass ở P0.

## 9. Đối chiếu hoàn thành

| Subtask | Bằng chứng |
|---|---|
| P0-T02.1 | Mục 2: route message chung và lộ trình thay route FE |
| P0-T02.2 | Mục 2–5: request/DTO, page/cursor, idempotency, lỗi và ví dụ JSON |
| P0-T02.3 | Mục 1/3/4: decimal string, range, BigInt và ordering |
| P0-T02.4 | Mục 6: Hub, envelope, payload tham chiếu, ID/version/delivery |
| P0-T02.5 | Mục 7: ownership và ranh giới HTTP/module/integration |
| P0-T02.6 | Mục 7/8: hiện có so với mở rộng eligibility/session, capability, provisioning và revoke |

Đầu ra là contract sẵn sàng review, phụ thuộc P0-T01. Các ví dụ JSON phải parse được và liên kết nguồn tồn tại; chưa cần build/runtime test vì không đổi code. Thay đổi contract khi triển khai phải cập nhật tài liệu này và ví dụ cùng PR.

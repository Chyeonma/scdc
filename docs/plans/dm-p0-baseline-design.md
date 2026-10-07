# DM-P0-T01 — baseline contract và thiết kế triển khai

Trạng thái: **agent đã chọn thiết kế cho gói; chờ người dùng review/test P0**. Đây là contract mục tiêu, chưa có API/Hub/writer DM trên branch này. Nguồn nghiệp vụ vẫn là [DM](../features/direct-messaging.md), [Accounts](../features/accounts.md), [kiến trúc](../architecture.md), [lifecycle](../data-lifecycle.md). Không đổi scope hoặc policy sản phẩm.

Base source `fe3c54a`; branch `chore/dm-p0-t01-acceptance-baseline`. Không cherry-pick/merge code DM cũ ở P0. Các ref dưới đây là commit local đã đọc, không suy thành bằng chứng runtime của gói mới.

## Reuse, replace và defer

| Nguồn / commit | Phần có thể reuse sau rà soát | Phải replace hoặc defer |
|---|---|---|
| Main `fe3c54a` | Identity register/verify/login/profile/logout/session guard; Result/ProblemDetails; API host; Dockerfile/nginx; test Identity | Messaging/Community chỉ Foundation. Chat UI mock và Hub client cũ chưa chứng minh DM |
| `origin/feat/msg-p1-t01-direct-conversation` / `16779b380a6d60f72c1370b4209c08b6df0a2042` | Unique pair handling, atomic space/member insert, concurrency-test ideas | Route `/conversations/direct`, body recipientUserId, 201/200 và SpaceSummary thay bằng `/direct-conversations`, peerUserId, 200, DirectConversation. Guid order/guard cần proof mới |
| `origin/feat/msg-p1-t02-inbox` / `30b7a850d7a5f5af1496572b50c253ac427da2a3` | Materialize/projection query và membership filtering | DTO/preferences/read state của gói cũ, cursor/list contract, inactive peer historical summary |
| `origin/feat/msg-p2-t01-send-message` / `78f13451525be444d5fd5b23a4ded67ef18d2c2a` | Transaction writer và test rollback | Body trim/10.000 rune, SHA không khóa, fingerprint gắn message, identity sequence, peer guard trước retry, 201 và `/spaces` đều không phù hợp |
| `origin/feat/msg-p2-t02-history` / `01edf23095688d0462d370f87b6d1deaca3bf8ab` | Keyset/query test ideas | Cursor/frontier/committed per-space sequence và binding actor/limit/resource; không reuse global identity làm checkpoint |
| `origin/feat/msg-p3-t01-signalr-access` / `08b212097a82f0c54f98ab8badfa67d2cabb55f4` | Hub registry/access-test structure | Group membership không thay guard; dùng SubscribeConversation/MessageChanged; mutation REST, deadline từ revoke commit |
| `origin/feat/msg-p3-t02-outbox` / `c44609ddbbc8dd93d35144ca4877a7722591e805` | Worker/lease/retry patterns, PostgreSQL integration tests | Payload chỉ tham chiếu, reread current DTO, dispatch authorization và expiry/revoke proof |
| `origin/feat/msg-p3-t03-reconnect` / `095fee9f348b882533a336be7b967e33b3a8d6c3` | Reconnect handling/test ideas | Partial-page snapshot, resume sau merge toàn bộ, tin cũ edit/delete reload, không dùng max live sequence làm checkpoint |
| `origin/feat/msg-p6-t01-edit-delete` / `b8613b0b6d1580ed82a43768a07ca6a4a8816fa7` | Optimistic version/race-test ideas | Source còn ghi PreviousContent và trim/10.000 rune: loại bỏ body history; tombstone/no-op/author/replay hiện trạng theo contract mới |
| Các branch group/file/thread/read-state/block/moderation/media | Không lấy vào P0–P7 | Ngoài phạm vi DM text nội bộ; không merge cả chuỗi branch |

## Bảy thao tác REST và Hub

Prefix `/api/v1`; actor từ phiên. [OpenAPI DM](../contracts/direct-messaging.openapi.json) là nguồn schema máy đọc; Swagger runtime P0 chỉ có Identity/Health.

| Thao tác | Request | Success / task thực thi |
|---|---|---|
| GET `/users/search` | q 2–64 UTF-16, cursor, limit mặc định20/tối đa50 | 200 UserSearchPage; P1-T01 |
| POST `/direct-conversations` | `{peerUserId}` | 200 cùng DirectConversation cho pair; P1-T02 |
| GET `/direct-conversations` | cursor, limit20/50 | 200 DirectConversationPage chỉ member; P1-T03 |
| GET `/direct-conversations/{id}/messages` | limit50/100; latest hoặc before hoặc after/through đúng binding | 200 MessagePage; P2-T02 |
| POST `/direct-conversations/{id}/messages` | `{clientMessageId:UUIDv4,content}` | 200 current Message sau commit/replay; P2-T01 |
| PATCH `/direct-conversations/{id}/messages/{messageId}` | `{expectedVersion:decimal-string,content}` | 200 current Message; no-op không increment; P5-T01 |
| DELETE `/direct-conversations/{id}/messages/{messageId}` | expectedVersion query dạng decimal string | 200 tombstone, repeat không increment; P5-T02 |

Không thêm GET một message, API `/spaces`, send Hub hoặc read/unread vào gói. `sequence/version/lastSequence/throughSequence` là chuỗi số, FE so sánh BigInt/comparator chính xác; server IDs UUIDv7. Message DTO có `content`, không có `normalizedContent`; placeholder restore theo schema hiện hành.

Hub `/hubs/chat`: invoke `SubscribeConversation({conversationId})` / `UnsubscribeConversation({conversationId})`; ack theo [realtime schema](../contracts/chat-realtime.schema.json). Event `MessageChanged={eventId,conversationId,message}` và `AccessRevoked`. Handler/buffer bật trước subscribe; ack sau đăng ký guard/registry. Event ID không phải cursor. P0 không MapHub và không quảng bá mock UI thành realtime thật.

## Mã lỗi cụ thể cho các task kế

Các mã dưới đây là **lựa chọn baseline mục tiêu**, chưa hoạt động trong Messaging; user review ở P0. ProblemDetails cùng envelope hiện có. Validate UUID/version/query/body trước nghiệp vụ; không lộ token/content/private participants. Trong phần nghiệp vụ kiểm actor/member trước đọc trạng thái resource/message, rồi author trước version/deleted/no-op.

| Trường hợp | HTTP / errorCode mục tiêu |
|---|---|
| Thiếu token, session expired/revoked, actor không eligible | 401 `Common.Unauthorized` |
| Model/parser/query/version/UUID không hợp lệ; before+after | 400 `Common.ValidationFailed` |
| Open self với UUID hợp lệ | 400 `INVALID_PEER` |
| Peer không tồn tại hoặc không eligible khi tạo pair mới; send mới tới peer không eligible | 404 `RESOURCE_NOT_FOUND` |
| Conversation không tồn tại hoặc caller không member; message không thuộc conversation ở URL | 404 `RESOURCE_NOT_FOUND` |
| Member B sửa/xóa message của author A | 403 `AUTHOR_REQUIRED` |
| Content lỗi Unicode/NUL, empty/invisible hoặc quá dài | 400 `CONTENT_INVALID` / `CONTENT_EMPTY` / `CONTENT_TOO_LONG`; malformed JSON surrogate bị parser chặn dùng `Common.ValidationFailed` |
| Cursor khác actor/query/limit/resource/direction, tampered/expired | 400 `CURSOR_INVALID` |
| Same operation key khác initial normalized payload | 409 `OPERATION_CONFLICT` |
| Legacy operation không fingerprint đủ chứng minh | 409 `OPERATION_UNVERIFIABLE` |
| PATCH/DELETE version cũ ở tin còn sống | 409 `VERSION_CONFLICT` |
| PATCH tin đã xóa, sau auth/author, kể cả version cũ | 409 `MESSAGE_DELETED` |
| Key fingerprint đã dùng không đọc được | 503 `FINGERPRINT_KEY_UNAVAILABLE` |
| Dependency authority không xác nhận được | 503 `AUTHORITY_UNAVAILABLE`, fail closed; không fallback cho phép |

DELETE lặp tombstone sau actor/member/author guard trả cùng current DTO, không tăng version/outbox. PATCH no-op cần expectedVersion đúng. Operation replay có actor/member guard rồi trả hiện trạng, kể cả peer disabled, không kiểm peer như một send mới. Pair đã có được mở lại bằng quyền read; chỉ pair mới cần peer eligible. Search/inbox/history của caller valid không yêu cầu peer của history active.

HTTP Identity P0 giữ nguyên source: pending login403 `Identity.EmailNotVerified`, verify204, register201, login/GET/PATCH me200, logout204 và token cũ401 `Common.Unauthorized`. Không sửa mã Identity theo taxonomy DM. Hub invoke denial encode ProblemDetails vào HubException cùng status/code đã chọn; unauthenticated handshake HTTP401. Nếu không authorize scope thì không bootstrap history của scope đó.

## Shared UoW, guard và thứ tự khóa

Chọn MVP một PostgreSQL database/host; BuildingBlocks sở hữu transaction scope, Contracts sở hữu interface, Identity triển khai guard/session/search/historical summaries; Messaging sở hữu space/message/outbox/registry. Không JOIN hoặc dùng IdentityDbContext từ Messaging.

P1-T02/P2-T01 triển khai scope scoped dùng **một NpgsqlConnection và DbTransaction**; mỗi DbContext enlist `UseTransactionAsync` vào scope. Chỉ owner commit/rollback. Không mở transaction riêng trong guard/worker hoặc dùng ambient scope không chứng minh enlistment. Isolation ReadCommitted với khóa writer; dispose/failure rollback. Không giữ DB transaction qua network dispatch.

Thứ tự: Identity user rows theo UUID network unsigned (actor và recipient liên quan) → auth session row khi cần → chat space → message/operation → outbox. Guard giữ `FOR SHARE` user/session tới commit, cạnh tranh đúng với `LockUserAsync`/revoke session. Revoke/disable mutation phải lấy user trước session theo cùng thứ tự; cần bổ sung vào Identity tại task tương ứng. Không dùng FOR KEY SHARE thay FOR SHARE để bỏ qua conflict với NO KEY UPDATE.

Contracts mục tiêu: `IAccountAccessGuard` (purpose Read/AuthorMutation/NewSend, giữ lease tới UoW commit); `IAuthenticatedSessionReader` (actor/session/stamp/expiry); `IUserSearchDirectory` (search); `IHistoricalUserSummaryReader` (peer/author inactive, chỉ public fields); `IRealtimeAccessRevoker` mở rộng user/session/space. Đây là interface **sẽ tạo theo task**, chưa có trong assembly P0. `IUserDirectory` hiện chỉ active summary, không đủ chứng minh verified/session/member.

Send mới: khóa actor/recipient theo thứ tự thống nhất, space, tra SendOperation; replay đúng không cần recipient eligible. Nếu chưa có operation mới kiểm recipient dưới guard, tăng per-space counter, insert message/operation/outbox/projection, commit cùng transaction. Hai writer chờ space lock; rollback không để operation/sequence/outbox dở. P3-T02 phải chạy barrier commit/rollback/rotation chứng minh; P6-T02 chạy revoke-first/writer-first và dispatch deadline≤5s. P0 chưa có các proof này.

## Migration theo lát cắt

| Task | Migration/đầu ra cần thực hiện | Proof bắt buộc |
|---|---|---|
| P0 | Dùng schema hiện hành trên **volume DB thử mới**, không seed.sql. Không chạy lại schema.sql vì có DROP SCHEMA CASCADE | DB riêng,28 fixture, không dữ liệu DM. Init không là migration production |
| P1-T02 | Pair uniqueness/network UUID, exact2membership; điều kiện shared UoW và historical projection | Concurrent open/rollback; không orphan |
| P2-T01 | Counter `spaces.last_message_sequence`, `messages.conversation_sequence` per-space; SendOperation durable với fingerprint_version/key_id/hash/messageId; tombstone-compatible constraints; metadata-only outbox | Writer atomicity, UTF-16 fixtures, replay current DTO; global sequence legacy giữ riêng |
| P2-T02 | Index unique `(space_id,conversation_sequence)`, latest/before/after, persistent protected cursor | Backfill per-space từ `(sequence_no,id)` có ordering rõ; không hứa tái tạo commit order lịch sử cũ. Fresh/upgrade/idempotent, bigint |
| P3-T02 | Legacy operation fingerprint thiếu dùng null/unverifiable, không hash body hiện hành; key-version lookup | Rotation+missing-key503; migrated reader giữ data |
| P4-T01 | Outbox lease/status/attempt/index theo worker và metadata references | Stop/crash/retry/dedup/current DTO+guard |
| P5-T01/T02 | Author CAS/version, nullable body tombstone; purge legacy `previous_content` khỏi serving/migration data theo scope đã review | Không lưu body cũ, repeat/no-op/409/race. Migration không replay destructive bootstrap |
| P6-T02 | Session/user revoke hooks phối hợp guard/registry; không public admin helper | Continuous stream cutoff≤5s từ commit |
| P8-T01 | restore_redacted_at/protection floors/journal protocol/retention worker | Independent journal, unknown fail closed, no resurrection, RPO/RTO thật |

Migrations task sau là incremental SQL/version/checksum, preflight/transaction/backup theo tác động. P0 không chuyển schema rộng hoặc xóa legacy trên DB ứng dụng. Outbox/journal không giữ body; edit/delete mới không ghi bảng `message_edits.previous_content`. Constraints SQL char_length không thay service UTF-16 validation.

## Key ring, HMAC và config

Cursor: Data Protection encrypted/authenticated, ApplicationName theo môi trường và purpose riêng `Messaging.UserSearch.v1`, `Messaging.Conversations.v1`, `Messaging.History.v1`; TTL24h, bind actor/resource/query/filter/direction/limit/frontier. Keys persist qua restart/deploy; không delete old key còn cursor/backup cần. MVP một API host; multi-host/Redis không thuộc P0.

Fingerprint HMAC-SHA256 v1 theo [fixture](../fixtures/dm-fingerprint.json), key≥32bytes riêng; network UUID bytes + domain + length big-endian + UTF8 normalized content, constant-time compare. Local helper tạo `.dm-acceptance/keyrings/hmac/dm-local-v1.key` ngoài Git; cursor dir riêng `.dm-acceptance/keyrings/cursor`. Compose mount riêng, HMAC read-only. P0 **chỉ dự trữ config và lưu local key**, Foundation chưa tiêu thụ key hoặc cấp cursor, chưa chứng minh rotation/DP thực thi. JWT key riêng trong `.env.dm-test`, không dùng làm HMAC/cursor secret.

Ngưỡng text2.000UTF-16, no trim/NFC body, CRLF/CR→LF, Unicode17/invisible theo fixture. Mutation không auto replay khi401/refresh/offline/reconnect; wrapper hiện tại còn cần sửa tại P2/P3. Draft/cache body RAM/tab/actor, logout/reload cleanup thuộc P6.

## Đầu vào và proof còn mở

- User review lựa chọn baseline, error codes, migration/UoW/lock order tại P0; chưa gọi là đã nghiệm thu khi chưa phản hồi.
- Guard/transaction/counter/cursor/HMAC/outbox/race implementation+runtime proof thuộc P1–P6, không tick từ tài liệu.
- Provider email, independent restore journal storage/keys/worker authority, retention và release owners chưa chốt ở P0; P8/v1 vẫn Bị chặn nếu thiếu đầu vào. Restore thu hồi toàn bộ sessions/link từ backup theo lifecycle, không chỉ những session có journal revoke.
- SDK/keyring/SQL/error changes cần cập nhật source/contract/tests/docs cùng task; không sửa policy để khớp code cũ.

Bản chạy và hướng dẫn người dùng: [biên bản DM-P0-T01](../acceptance/direct-messaging/DM-P0-T01.md).

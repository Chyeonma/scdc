# Prompt rút gọn — hoàn thiện chat riêng MVP

Cập nhật12/10/2026 theo xác nhận: **“Làm liên tục toàn bộ phần MVP còn lại; tôi test FE/BE một lần cuối.”** Quy trình này thay gate từng task của bản cũ. Agent tự kiểm tra/sửa theo từng bước, làm liên tục và chỉ bàn giao để bạn test một lần cuối.

**Một prompt, ba phase nội bộ, một branch và một PR.** Giữ P0–P3 đã nghiệm thu; tích hợp source P4-T01 đã agent kiểm chứng vào bản chung, không tự ghi người dùng PASS task đó. Phạm vi hoàn thiện: chat/reconnect/sửa/xóa/quyền/phiên/composer và regression nội bộ P7. P8 là điều kiện v1 riêng theo [hồ sơ MVP](../releases/mvp.md), còn gate restore, email thật và tải toàn hệ thống.

| Phase | Task cũ gộp | Đầu ra |
|---|---|---|
| A | P4-T01/T02 + P5-T01/T02/T03 | Realtime/reconnect, sửa/xóa và xung đột không mất/trùng/hồi sinh tin |
| B | P6-T01/T02/T03 | Quyền/session, peer disabled, composer/IME/mobile/drafts |
| C | P7-T01 | Regression build chung, dữ liệu FE/BE riêng và bàn giao cuối |

Copy toàn bộ khối sau; không cần ghép prompt khác.

```text
Hoàn thiện toàn bộ phần chat riêng MVP SCDC còn lại, làm liên tục đến bản bàn giao cuối.
Repository gốc E:\Project\SCDC\scdc; worktree hiện tại E:\Project\SCDC\dm-message-integration.
Branch chung feat/dm-mvp-completion; PR target message.

Tôi đã cho phép làm liên tục toàn bộ MVP còn lại và chỉ test FE/BE một lần cuối. Không dừng xin nghiệm thu từng task/phase; tự test, reproduce, sửa và tiếp tục. Không tự ghi tôi PASS, không merge bản MVP vào message trước lần PASS FE/BE cuối, không publish/merge main.

A. Hiện trạng, policy và Git
1. Đọc AGENTS.md áp dụng, git status/branches/worktrees; giữ thay đổi người dùng, không reset/stash hoặc stage toàn repo. Đọc docs/plans/direct-messaging.md, docs/features/direct-messaging.md, docs/contracts/direct-messaging.openapi.json, docs/contracts/chat-realtime.schema.json và phần Identity/guard/data-lifecycle/release-operations liên quan. Fixtures dm-acceptance-cases.json/dm-demo-plan.json/dm-fingerprint.json/text-policy.json/text-validation.json là nguồn test/policy.
2. P0–P3 đã nghiệm thu; P3-T02 buildd4f1669/code4102955 đã user PASS, merge/push message tại9de920a. P4-T01 source đang có với82/82BE,57/57Node,8/8E2E và4/4Hub proofs; user chưa nghiệm thu riêng, kế thừa vào gói này. Quyền làm liên tục mới thay yêu cầu PASS mỗi task trước khi làm phụ thuộc. Không làm lại chức năng đã có hoặc merge chuỗi nhánh Messaging cũ.
3. DM hai người, UI chọn một người; text tối đa2.000 UTF-16, CRLF/CR→LF, không trim/NFC body. Session/member/author guard ở server; read/author edit-delete không đòi peer active, send mới có. Retry committed giữ original UUID/content/actor và trả current DTO dù peer disabled nếu actor còn quyền.
4. Mutations chỉ REST, không auto replay khi refresh/reconnect. Không body cũ trong edit/outbox/log hoặc API version-history. Draft/cache chỉ RAM tab theo actor/conversation, logout/reload xóa. Sequence/version decimal strings so bằng BigInt; eventId/highest live sequence không là frontier lịch sử. Sent là persistence, không read/delivery receipt.
5. MVP một API host. Không mở group/chat phòng/file/reaction/read-state/block/moderation/media/email/microservice. P8 restore/protection/retention/full-system benchmark/release riêng; không tự khai v1/production-ready hoặc thay policy để né gate.
6. Dùng một branch feat/dm-mvp-completion từ worktree chứa P4 hiện tại/base tích hợp9de920a; quyền mới cho làm phụ thuộc trong gói, không cần merge P4 chưa user PASS vào message. Commit conventional có task ID theo phase; stage explicit files. Một draft PR base message, không tạo nhiều PR/gate từng task. Push đã được phép; verify git ls-remote SHA=HEAD. Giữ message trước lần user PASS cuối.

B. Phase A — chat hoàn chỉnh (làm liên tục)
A1 — P4-T01/P4-T02, realtime và recovery:
  A1.1 Giữ Hub auth exact route/fresh guard/ACK, body-free outbox và lease/recovery đã kiểm chứng; sửa regression nếu có.
  A1.2 Automatic reconnect/backoff; handlers/buffer trước subscribe/ACK. Reconnect chỉ subscribe và GET, không replay mutation; trạng thái connecting/offline/recovering/online thật.
  A1.3 GET after bù nhiều trang với through cố định; merge trang xong mới tiến frontier, chỉ nhận resumeCursor trang cuối. Event live không nhảy frontier quá dữ liệu chưa đọc. Ngắt sau trang1/2 tiếp tục đúng; cursor invalid/expired tải lại hợp lệ.
  A1.4 Refresh cửa sổ tin cũ đã xem; không để latest50 xóa101 tin vừa bù/cửa sổ cũ. Actor/selection generation bỏ late response/event sau switch/logout. Proof current/tombstone thật chạy lại sau A2/A3.
  A1.5 R101/interruption/event chen giữa/worker restart/cursor faults: actual FE/API/DB, không mock thành proof server.
A2 — P5-T01, sửa tin:
  A2.1 PATCH /direct-conversations/{id}/messages/{messageId} với content/expectedVersion; actor/member/author guard. Non-author403; outsider/cross-space404; wrongversion409 VERSION_CONFLICT.
  A2.2 Compare-update/version/outbox/projection cùng transaction; cùng normalized body+đúng version là no-op, không tăng version/editedAt/outbox; không lưu body cũ.
  A2.3 UI author Sửa/Save/Cancel, Đã sửa, validation/error/conflict giữ current đã xác nhận; B/A2 nhận current không reload, edit reply không xóa composer draft.
  A2.4 Test sửa nhiều lần/no-op/2.000–2.001UTF16/CRLF/emoji/outsider/rollback/offline bằng API thật.
A3 — P5-T02/T03, xóa và race:
  A3.1 DELETE cùng URL với expectedVersion query; auth trước delete lặp. Lần đầu contentnull/deletedAt/version/outbox cùng tx, giữ ID/sequence/SendOperation. Delete lặp không tăng version/outbox; PATCH deleted409 MESSAGE_DELETED.
  A3.2 UI Confirm/Cancel/error/tombstone giữ vị trí/tác giả/thời gian; bỏ body/menu sửa/cache. Retry original send trả current tombstone, không phục hồi body hoặc thêm message/createoutbox.
  A3.3 Deterministic edit-edit/edit-delete cùng version: một thắng, loser409; GET/event/DB/FE current khớp, old event không backslide.
  A3.4 B offline giữ M01 cũ; A edit/delete M01 và gửi101 mới. Recovery đủ101 và current M01/tombstone, kể cả ngắt giữa trang; dùng production APIs, không SQL fixture thay proof.

C. Phase B — quyền/phiên/composer (không chờ user test giữa phase)
B1 — P6-T01:
  B1.1 Ma trận A/B/C/U/no-token x search/open/list/history/send/edit/delete/subscribe/retry với status/errorCode cụ thể theo contract. U login403 khác state unverified của session đã cấp; isolated transition helper, không bypass token sản phẩm.
  B1.2 C biết DM/message/cursor vẫn bị chặn; B không mutation tin A; errors/log/outbox/DOM/storage không private participants/email/body cũ/token/key.
  B1.3 Logout A→login C cùng tab, held GET/late event của A không phục hồi cache/draft. Authority unavailable fail closed, không fallback mock.
B2 — P6-T02:
  B2.1 Revoke exact old connected session/logout-all/stamp/expiry/disabled; reconcile≤1s thiết kế, đo committed revoke→last content≤5s, không chỉ AccessRevoked. Old token HTTP/Hub bị chặn; không login session mới để thay proof phiên cũ.
  B2.2 Helper K disabled/unlock chỉ DB_test/run owned, không public admin API; tách disabled khỏi temporary lockout, ghi status/stamp/session/timing metadata không token/body.
  B2.3 K disabled: A đọc/sửa/xóa tin A và retry committed vẫn được, send mới bị chặn; unlock K cần session mới. Race revoke/send/subscribe/dispatch có UoW/lock order đúng, authority lỗi ngừng phát.
B3 — P6-T03:
  B3.1 Desktop Enter gửi/ShiftEnter newline, coarse/mobile Enter newline, Send luôn gửi; IME composition không POST sớm; Unicode/LF/UTF16 đúng.
  B3.2 Draft RAM theo actor/conversation giữ khi AB↔AC; reload/logout/actor switch xóa. Failed operation immutable tách draft mới; reconnect không POST bất ngờ.
  B3.3 Layout desktop/narrow/focus/errors/edit/delete/history/retry dùng được. Actual browser+mobile viewport ghi đúng loại; tôi test bàn phím/thiết bị thật ở lần cuối, không giả viewport là mobile thực.

D. Phase C — regression và bàn giao cuối P7-T01
C1. Chốt một build/schema/config/API/FE; migration fresh/upgraded/replay, keys/cursor/operations/IDs bền qua restart; không drop DB/volume ứng dụng.
C2. PostgreSQL integration thật, Node/build, actual browser WebSocket qua proxy; negative matrix, races/offline101/interruption/worker crash/key rotation/revoke deadline/log-storage scan. Tránh global fixture/dispatcher races khi chạy suite.
C3. Tạo run FE và BE riêng, còn nguyên baseline, alias/username/ID/UUID thật; helper/REST/Swagger và SQL read-only. Mỗi case có before/after counts, version/IDs, currentbody/tombstone, actual vs injected evidence, cleanup.
C4. docs/acceptance/direct-messaging/DM-MVP.md theo template: commit/remoteSHA/PR/image/asset/config/schema/ports, mọi testcase dưới đây, kết quả agent và “Người dùng: Chưa xác nhận”. Các biên bản cũ giữ lịch sử, không giả user PASS.
C5. Kiểm tra start/stop/restart/setup, FE15300/API15026/DB15432; PowerShell có -Action và đúng worktree, Docker Desktop/Linux engine. Cleanup pause/claim/fault/browser injection, giữ data/manifest/evidence; không log tokens/body private/key.

E. Testcase cuối cụ thể, mỗi case đủ FE/BE/DB và lane độc lập
MVP-C01: A/B hai profile; tìm đúng username→mở→A gửi MVP-M01→B nhận không reload, cùng ID/sequence/version. Reload/restart còn history1. C GET/POST/subscribe404 RESOURCE_NOT_FOUND; DB1message/operation/createoutbox, không orphan.
MVP-C02: B offline; A gửi RC-001…RC-101. Recovery50/50/1; ngắt sau trang1 rồi nối lại, lặp sau trang2. RC-102 live chen giữa không nhảy frontier. FE ID set=REST/DB, đúng order/no duplicate; failed send không auto POST.
MVP-C03: A gửi EDIT-ORIGINAL→EDIT-V2→EDIT-V3, B/A2 thấy Đã sửa current. Save EDIT-V3+đúngversion no-op (version/editedAt/outbox không tăng), Cancel không đổi.2.001UTF16 bị chặn,2.000/CRLF/emoji đúng. B PATCH A403, C/cross-space404; không body cũ/version-history.
MVP-C04: A gửi DELETE-ORIGINAL→confirmdelete, B tombstone đúng ID/sequence/vị trí. Delete lặp không tăng version/outbox; B delete A403, PATCH tombstone409 MESSAGE_DELETED. Original send replay cùng UUID/body trả cùng tombstone, không hồi sinh ID/body cũ.
MVP-C05: Deterministic A1/A2 edit-edit rồi edit-delete cùng expectedVersion: một thắng/loser409 VERSION_CONFLICT. B offline giữ M01 cũ, A edit/delete và gửi101; reconnect đủ101+currentM01/tombstone. Late stale event/oldHTTP không ghi đè version mới.
MVP-C06: Pause worker30s; A200/Sent, B GET200/no live; resume/requeue sameevent no duplicate. SIGKILL sau committedlease→expiryrecovery sameevent/attempt tăng/lease clear. Writer rollback không message/operation/outbox/event, không tự replay.
MVP-C07: A/B/C/U/no-token toàn ma trận; unverified transition của session thật isolated. Đổi A→C với heldGET/lateevent không lộ RAM A. Logs/outbox/storage không token/key/email/body cũ/private metadata.
MVP-C08: A1 revoke exact subscribed A2; commit→lastcontent≤5s, oldtoken HTTP/Hub401, UI clear. Logout-all/expiry/stamp/disabled/authority fault tương tự. K disabled: A read/edit/delete/retry committed được, send mới không; unlock không phục hồi oldsession.
MVP-C09: IME tiếng Việt/emoji/ZWJ, compositionEnter khôngPOST; desktopEnter/ShiftEnter và mobileEnter/Send đúng. Draft AB↔AC giữ; reload/logout xóa; failedUUID/content không đổi vì draft mới; narrow/desktop thật và tôi test thiết bị cuối.
MVP-C10: Rebuild/restart build chung, keys/cursor còn hạn/DB IDs/tombstone giữ; regression P0–P7 scope. Log redaction+cleanup. Restore/email thật/full-system benchmark/P8 chưa làm ghi v1 gate, không production-ready.
Mỗi case phải bàn giao nguyên lệnh/request copy được với ID thật từ manifest; data nhập/UUID/expectedVersion/status/errorCode/baseline/delta và FE từng click rõ. Không coi source/mock là PASS, không bỏ variant bắt buộc; giới hạn thiết bị/provider ghi đúng và có recipe cuối.

F. Điểm dừng duy nhất
Tự kiểm thử/sửa liên tục đến bản MVP chạy được. Push một branch và verify remoteSHA=HEAD; draft PR base message có scope/migration/tests/knownlimits. Dừng ở “Chờ người dùng test FE/BE toàn MVP”, không xin nghiệm thu task riêng. Tôi FAIL: sửa cùngbranch, regression, build mới. Tôi PASS buildcuối: fetch/verify remote task/message, merge --no-ff riêng gói, smoke/push/verify ancestry. Conflict đổi hành vi/schema thì bàn giao phần ảnh hưởng để test lại; không merge main/publish tự động.
```

## Đối chiếu bản cũ

[20 prompt từng task đã lưu](direct-messaging-prompts-by-task.md) giữ80 ví dụ và traceability. Gate từng task trong bản lưu là lịch sử, được thay bằng quyền làm liên tục ngày12/10/2026. [Kế hoạch chi tiết](direct-messaging.md) vẫn giữ mapping AC/TC/task; giảm số bàn giao và lặp prompt, giữ policy/kiểm chứng của scope.

P8-T01/T02/T03 giữ riêng cho v1, không nằm trong lần hoàn thiện MVP này. Không suy user duyệt MVP là ký phát hành hoặc PASS toàn sản phẩm.

<!-- Historical anchors redirect readers to the archived per-task prompts above. -->
<a id="dm-p0-t01"></a><a id="dm-p1-t01"></a><a id="dm-p1-t02"></a><a id="dm-p1-t03"></a>
<a id="dm-p2-t01"></a><a id="dm-p2-t02"></a><a id="dm-p3-t01"></a><a id="dm-p3-t02"></a>
<a id="dm-p4-t01"></a><a id="dm-p4-t02"></a><a id="dm-p5-t01"></a><a id="dm-p5-t02"></a><a id="dm-p5-t03"></a>
<a id="dm-p6-t01"></a><a id="dm-p6-t02"></a><a id="dm-p6-t03"></a><a id="dm-p7-t01"></a>
<a id="dm-p8-t01"></a><a id="dm-p8-t02"></a><a id="dm-p8-t03"></a>

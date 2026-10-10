# Kế hoạch triển khai nhắn tin riêng SCDC

Cập nhật: **09/10/2026**. Baseline đã đọc: `main` tại `fe3c54a` trong repository `scdc/`. **DM chưa có backend nghiệp vụ hoặc Hub runtime trên main**; giao diện hiện dùng dữ liệu mẫu và local state. Các nhánh Messaging cũ có code để tham khảo, nhưng phải đối chiếu đặc tả hiện hành trước tái sử dụng. P0 đã chuẩn bị bản chạy và dữ liệu Identity riêng, được người dùng PASS FE/BE và đồng ý baseline theo [biên bản P0](../acceptance/direct-messaging/DM-P0-T01.md). Ngày 07/10/2026, đã tạo và push nhánh tích hợp `message` từ `origin/main` tại `2096e0b`; phần tăng từ baseline `fe3c54a` là tài liệu/README. P0 đã được người dùng PASS FE/BE và đồng ý baseline trong phiên, merge/push vào `message` tại `170d959` trước khi tạo P1-T01.

Mục tiêu là bàn giao từng task có thể kiểm tra qua **frontend và backend thật**. Sau mỗi task, người dùng tự test với bộ dữ liệu được cung cấp, xác nhận kết quả rồi mới thực hiện task tiếp theo. Agent tự chạy test kỹ thuật trước bàn giao; kết quả tự động không thay xác nhận của người dùng.

[Prompt từng task](direct-messaging-prompts.md) có **80 ví dụ test case chi tiết**, bốn ca trong mỗi khối prompt hoàn chỉnh, với điều kiện trước, dữ liệu, bước FE/BE, kỳ vọng UI/API/DB, bằng chứng và cleanup. [Catalogue máy đọc](../fixtures/dm-acceptance-cases.json) giữ cùng case ID/nội dung; đây là kế hoạch chưa chạy, không thay toàn bộ AC/TC. Khi triển khai, agent phải cung cấp lệnh/request/SQL thực tế và ghi kết quả từng bước theo [mẫu biên bản](../templates/dm-task-acceptance.md).

## Phạm vi và nguồn chuẩn

Nguồn nghiệp vụ: [DM](../features/direct-messaging.md), [Accounts](../features/accounts.md), [MVP](../releases/mvp.md), [kiến trúc](../architecture.md), [vòng đời dữ liệu](../data-lifecycle.md), [nghiệm thu](../release-operations.md). Contract cần đối chiếu: [OpenAPI DM](../contracts/direct-messaging.openapi.json), [schema realtime](../contracts/chat-realtime.schema.json), [fixture text](../fixtures/text-validation.json), [text policy](../fixtures/text-policy.json), [fingerprint](../fixtures/dm-fingerprint.json).

Các task P0–P7 dành cho gói DM văn bản nội bộ: tìm người, một DM/cặp, inbox, gửi/lịch sử/retry, realtime/reconnect, sửa/xóa, quyền/phiên và UI. P8 hoàn thiện bảo vệ restore, đo tải và phụ thuộc phát hành theo v1. Đạt P7 chỉ là đạt gói nội bộ đã ghi; không tuyên bố toàn MVP hoặc v1 đã nghiệm thu vì còn Community, Identity/email và các gate tương ứng.

Không đưa group chat, tin phòng, attachment, reaction, pin, mention, typing, read/unread, block, moderation hay media vào gói DM này. Việc dùng lại nền Messaging không tự mở rộng scope. Microservice thuộc v1, không tách service trong P0–P7.

Các quy tắc phải giữ từ lần triển khai đầu:

- Text tối đa **2.000 UTF-16 sau CRLF/CR → LF**; không trim/NFC body; từ chối chuỗi lỗi/chỉ trắng/vô hình theo fixture; hiển thị text an toàn.
- Không lưu nội dung sửa cũ trong `message_edits`, outbox, log hoặc audit; xóa giữ tombstone và dedup marker.
- Một cặp user có một DM; UUID server v7, `clientMessageId` v4; sequence/version truyền chuỗi số nguyên, so sánh chính xác.
- “Đã gửi” là DB đã commit. Bấm retry giữ khóa và nội dung ban đầu; HTTP refresh, reconnect hoặc mạng trở lại không tự replay mutation.
- Read/author edit/delete không yêu cầu peer còn active; **send mới** phải kiểm tra peer. Actor luôn cần phiên/quyền hợp lệ.
- Nội dung/nháp/cache ở bộ nhớ tab theo actor/conversation; logout/đổi actor/reload cleanup đúng policy.
- Reconnect bù đủ trang mới và tải lại cửa sổ lịch sử cũ để nhận edit/delete; server ngừng dữ liệu tới phiên bị thu hồi trong **≤5 giây** từ commit.

Thuật toán cursor, key ring, shared transaction/guard và schema mới trong docs đang là thiết kế đề xuất. P0 chốt thiết kế gói; task thay đổi thiết kế phải ghi lý do, tác động và cập nhật contract cùng source, không tự đổi quy tắc sản phẩm.

## Quy trình bắt buộc cho mỗi task

Trạng thái: `Chưa làm → Đang làm → Agent kiểm tra đạt → Chờ người dùng test → Người dùng PASS → Được phép bước kế`. Nếu người dùng FAIL, giữ task hiện tại, sửa và bàn giao bản mới để test lại. Task bị chặn phải ghi phụ thuộc; không tự bỏ ca bắt buộc hoặc chuyển sang task khác.

Nếu người dùng giao thực hiện toàn kế hoạch theo chế độ dừng sau mỗi task, phản hồi PASS FE/BE là tín hiệu đi task kế trong phạm vi đã giao; không hỏi thêm cùng một xác nhận. Nếu chỉ giao duy nhất một task bằng prompt riêng, PASS xác nhận task đó, còn task kế cần được giao. Quyền merge/push đã cấp được giữ xuyên các task đúng phạm vi.

1. Đọc task, nguồn chuẩn, kết quả task trước và kiểm tra working tree. Chỉ làm task được giao.
2. Tạo nhánh đúng tên; triển khai subtask, migration và UI phù hợp. Các lỗi UI phải có phản hồi thật; không thay backend thiếu bằng mock để ghi đạt.
3. Agent chạy test đúng phạm vi: backend với PostgreSQL riêng, frontend/unit, build và E2E các luồng liên quan. Đối soát DB cho transaction/quyền/chống trùng.
4. Chuẩn bị bản chạy được, dữ liệu mẫu và hướng dẫn cụ thể: URL, tài khoản, ID trả về, request, từng thao tác UI, kết quả mong đợi, cách thử lỗi và câu lệnh kiểm tra DB chỉ đọc.
5. Ghi biên bản theo [mẫu nghiệm thu task](../templates/dm-task-acceptance.md), trạng thái **Chờ người dùng test**. Chỉ ghi ca agent đã chạy; phần người dùng còn trống.
6. **Dừng task và chờ phản hồi.** Không tự merge, tạo task kế hoặc tiếp tục phase. Không xem im lặng, câu hỏi hoặc “đang test” là PASS.
7. Quyền push nhánh task và merge vào `message` đã được cấp trong phiên. Sau PASS FE/BE, kiểm tra nhánh task đã push thành công và remote SHA đúng build được duyệt, merge vào `message`, chạy smoke rồi push và kiểm tra remote. Trước task kế, baseline phải chứa task trước; thay đổi build ảnh hưởng bằng chứng phải ghi và kiểm tra lại phần liên quan.

Mẫu phản hồi người dùng:

```text
PASS DM-P1-T01 | FE: đạt | BE: đạt | Build: <commit>
Merge vào message theo quyền đã cấp. Giao thực hiện DM-P1-T02.
```

```text
FAIL DM-P1-T01 | FE hoặc BE: <ca bị lỗi>
Thao tác: ... | Mong đợi: ... | Thực tế: ...
Sửa task này rồi bàn giao để tôi test lại.
```

`PASS` được áp dụng theo phạm vi công việc đã giao ở trên; không cần lặp lại quyền đi tiếp hoặc merge đã có trong phiên. Nếu quyền merge chưa được cấp, chuẩn bị PR/kết quả cụ thể trước khi xin quyền merge. Người dùng test các lát cắt không tự trở thành người ký phát hành production đang mở theo DEC-111.

## Môi trường và dữ liệu mẫu

P0 sẽ tạo môi trường riêng `compose.dm-test.yaml`, project Compose `scdc-dm-acceptance`, DB **`scdc_dm_acceptance_test`**. Cổng dự kiến: frontend **15300**, API **15026**, PostgreSQL **15432**; kiểm tra xung đột trước chạy và ghi cổng thực tế. Test không dùng `scdc_chat` hoặc volume ứng dụng hiện tại. Không xóa volume/dữ liệu cũ để dựng demo.

Các đường dẫn dự kiến sau khi P0 bàn giao: Web `http://localhost:15300`, Swagger `http://localhost:15026/swagger`, API `http://localhost:15026/api/v1`. `.env.dm-test`, token, key và runtime output nằm ngoài Git. Development token chỉ dùng bởi helper local để setup/verify; không dùng UI tự kích hoạt làm bằng chứng email thật. P0 giữ hành vi Identity hiện có ngoài phạm vi cần setup DM.

### Tài khoản

Fixture máy đọc: [dm-demo-plan.json](../fixtures/dm-demo-plan.json). **Fixture này là kế hoạch; P0 đã tạo 28 user trong DB thử, ID thật nằm trong biên bản P0 và manifest local.** Mật khẩu mẫu dùng riêng môi trường local: `DmDemo2026!Local`.

| Alias | Username | Email thử | Display name | Trạng thái và mục đích |
|---|---|---|---|---|
| A | `dm_demo_an` | `dm-an@example.test` | An Demo | Active, verified; người gửi, có A1/A2 để thử nhiều phiên |
| B | `dm_demo_bao` | `dm-bao@example.test` | Bảo Demo | Active, verified; người nhận B1/B2 |
| C | `dm_demo_chi` | `dm-chi@example.test` | Bảo Demo | Active, verified; trùng tên hiển thị, outsider của DM A–B |
| U | `dm_demo_pending` | `dm-pending@example.test` | Chưa xác minh Demo | Pending, unverified; login ứng dụng bị từ chối |
| K | `dm_demo_khoa` | `dm-khoa@example.test` | Khóa Demo | Setup active/verified; chỉ chuyển disabled trong ca kiểm thử có helper riêng |
| S01–S23 | `dm_demo_search01` đến `dm_demo_search23` | `dm-search01@example.test` đến `dm-search23@example.test` | Người tìm 01 đến Người tìm 23 | Search phân trang, từ khóa `dm_demo_search` |

Helper P0 phải tạo user qua API Identity, lấy ID thật và verify A/B/C/K/S; U giữ pending. Ghi manifest local với user ID và URL, không in access/refresh/verify token vào biên bản hoặc commit. Run lặp chỉ dùng dataset cùng run đã xác nhận; nếu username tồn tại nhưng không đúng fixture thì dừng, không ghi đè account tùy ý. Khi cần ca cô lập, tạo run mới với hậu tố và bảng tài khoản thực tế tương ứng.

K được khóa/mở trong task P6 bằng fixture helper chỉ cho DB `_test`, có đối soát status/stamp/revoke. Không thêm API quản trị công khai hoặc bypass auth production. Chưa có helper thì ca khóa tài khoản là Bị chặn, không ghi đã đạt.

### Hội thoại và nội dung

Tạo D-AB từ POST của A với peer B; D-AC từ A với C; D-AK từ A với K khi K còn active; D-HIST từ A với S01, dành riêng H121. IDs phải lấy từ response, không hardcode UUID server. C không được tham gia D-AB chỉ vì biết ID. Người dùng mở A/B/C bằng các profile trình duyệt riêng; A1/A2 là hai phiên của A, không dùng hai tab chia sẻ token để mô phỏng hai phiên độc lập.

| Dataset | Dữ liệu | Cách dùng |
|---|---|---|
| M01 | `Chào Bảo, đây là tin thử M01.` | Gửi/lịch sử cơ bản |
| M02 | `Dòng 1\r\nDòng 2 👩‍💻` | CRLF → LF, emoji/UTF-16 |
| M03 | `  Giữ khoảng trắng có chủ ý  ` | Không trim body |
| M04 | `<script>alert('DM test')</script>` | Hiển thị nguyên text, không chạy script |
| L2000/L2001 | `a` lặp 2.000/2.001 lần | Biên nhận/từ chối qua UI và REST |
| E2000/E2002 | `😀` lặp 1.000/1.001 lần | 2.000/2.002 UTF-16, không đếm emoji là một |
| EMPTY/INVISIBLE/INVALID | Chuỗi rỗng, trắng, `\u200b`, surrogate lỗi/NUL từ fixture | Validation âm tính; lỗi JSON parser phải là ProblemDetails, không ghi body vào log |
| H121 | `HIST-001` đến `HIST-121` | Latest/before, limit 50, đủ ba trang |
| R101 | `RECONNECT-001` đến `RECONNECT-101` | B offline rồi bù ba trang, sửa/xóa một tin cũ trước đó |

Hai khóa v4 cố định chỉ dùng trong ca retry: O1 `7c8e7c59-b35a-4d12-b22f-965b96ff4e44`; O2 `7c8e7c59-b35a-4d12-b22f-965b96ff4e45`. Cùng O1/content là cùng thao tác; đổi content với O1 phải conflict; muốn gửi nội dung giống một lần nữa dùng O2. Mỗi run/ca khác phải dùng ID mới trừ ca chủ ý replay.

P0 cung cấp helper PowerShell dùng được trên máy hiện tại: setup/login/capture ID, tạo chuỗi biên, gọi request song song và fault scenario. Mỗi task bổ sung recipe vào helper/hồ sơ, kèm lệnh chính xác sau khi implementation có thật. Backend script đăng nhập và giữ token trong bộ nhớ; biên bản chỉ ghi actor/request/errorCode/ID không nhạy cảm.

Mỗi recipe ghi baseline trước khi chạy và kiểm tra số tăng thêm, không đếm toàn bộ dữ liệu tích lũy như dữ liệu mới. H121 dùng D-HIST chưa có tin để kiểm tra 50/50/21; nếu ca đã chạy thì dùng run/pair fixture mới. R101 dùng D-AB, chốt resumeCursor và tập message ID trước offline, đối soát đúng **101 tin mới** ngoài phần history đã có. Không xóa DB để làm lại ca.

### Request tham chiếu

Đây là contract để triển khai, chưa phải endpoint hoạt động trên `main`. Prefix `/api/v1`; Bearer của actor được lấy từ login trong helper hoặc Authorize Swagger.

```http
GET /api/v1/users/search?q=dm_demo_bao&limit=20

POST /api/v1/direct-conversations
Content-Type: application/json
{"peerUserId":"<B.id>"}

POST /api/v1/direct-conversations/<D-AB.id>/messages
Content-Type: application/json
{"clientMessageId":"7c8e7c59-b35a-4d12-b22f-965b96ff4e44","content":"Chào Bảo, đây là tin thử M01."}

GET /api/v1/direct-conversations/<D-AB.id>/messages?limit=50

PATCH /api/v1/direct-conversations/<D-AB.id>/messages/<M01.id>
Content-Type: application/json
{"expectedVersion":"<version hiện hành>","content":"M01 đã sửa."}

DELETE /api/v1/direct-conversations/<D-AB.id>/messages/<M01.id>?expectedVersion=<version hiện hành>
```

Mã lỗi/status lấy từ OpenAPI/đặc tả và được khóa ở P0. Đọc endpoint không tồn tại có thể là 404 trước task triển khai, không ghi đó là test quyền đã đạt. C gọi ID không có quyền phải không lộ metadata/nội dung; bản bàn giao ghi status/errorCode cụ thể theo contract đã chốt.

## Git flow cho từng nhiệm vụ

Mỗi task một branch, một PR và một biên bản. Không commit tất cả working tree; hiện có thay đổi tài liệu từ checklist trước, phải giữ nguyên và chỉ stage file thuộc task. Không tự stash/reset/revert thay đổi người dùng. Nếu working tree cần cô lập, dùng worktree riêng trong workspace hoặc yêu cầu xử lý đúng phần thật sự xung đột.

| Việc | Quy tắc |
|---|---|
| Nhánh tích hợp | `message`, đã push từ `origin/main` tại `2096e0b`; giữ thay đổi tài liệu mới trên nhánh này khi tích hợp P0 |
| Base | Task mới từ `origin/message` đã kiểm tra và chứa predecessor đã PASS. P0 đã tồn tại từ `main` tại `fe3c54a`, giữ baseline đã test; chưa rebase/merge khi đang chờ test. Không tự dựa vào tip nhánh Messaging cũ |
| Branch | Tên chính xác ở từng task bên dưới; nhánh đang tồn tại thì kiểm tra commit/PR và tiếp tục đúng task |
| Commit | `feat(messaging): DM-Px-Tyy <kết quả>`; hạ tầng dùng `chore`, kiểm thử dùng `test`, docs dùng `docs`, sửa lỗi dùng `fix` |
| Subtask | Các subtask cùng task dùng chung branch/PR; commit có thể ghi mã như `DM-P1-T01.2`, không tạo branch riêng cho mỗi checkbox |
| Tái sử dụng | Đọc diff/contract của nhánh cũ; lấy phần tương thích có chọn lọc và ghi provenance. Không merge cả chuỗi nhánh P1–P9 chỉ vì cùng chủ đề |
| Push / PR | Push nhánh task được giao lên `origin`, kiểm tra remote SHA trùng HEAD và ghi biên bản. PR nếu tạo có base `message`, tiêu đề có task ID, phạm vi/migration/FE/BE/dataset/hướng dẫn và status Chờ test. Thiếu auth hoặc push lỗi: giữ local, báo rõ, chưa merge |
| Người dùng FAIL | Sửa trên branch task; commit `fix(messaging): DM-Px-Tyy ...`, cập nhật build/recipe, chạy regression liên quan và dừng chờ test lại |
| Người dùng PASS | Ghi xác nhận FE/BE và build; fetch/kiểm tra remote SHA đúng build đã duyệt, merge riêng task vào `message` với `--no-ff` để giữ lịch sử task. Chạy smoke/kiểm tra liên quan, push `message`, xác minh remote SHA và ancestry. Conflict làm đổi hành vi/config/schema phải bàn giao lại trước khi coi là đạt |
| Đồng bộ / main | Không force push. Nếu remote `message` đổi đồng thời, fetch và kiểm tra lại trước push. Việc tích hợp `message` vào `main` hoặc phát hành cần yêu cầu riêng |
| Task kế | Chỉ bắt đầu khi có quyền đi tiếp; base phải chứa predecessor. Không lấy PASS của build cũ làm PASS cho thay đổi hành vi mới |
| Task mới khi predecessor chưa merge | Dừng phần phụ thuộc; chỉ dùng stacked branch nếu người dùng cho phép rõ. Ghi base/PR dependency và chạy lại sau rebase |

Ví dụ lệnh tham chiếu, chỉ chạy lúc được giao task và working tree đã xử lý an toàn:

```powershell
git fetch origin message
# Chỉ tạo task khi predecessor đã PASS và nằm trong origin/message.
git switch -c feat/dm-p1-t01-user-search origin/message
# Triển khai, test, stage danh sách file thuộc task rồi commit.
git commit -m "feat(messaging): DM-P1-T01 search verified recipients"
git push -u origin feat/dm-p1-t01-user-search
git rev-parse HEAD
git ls-remote --heads origin feat/dm-p1-t01-user-search
# Hai SHA phải trùng; bàn giao, DỪNG chờ người dùng PASS FE/BE.
# Chỉ chạy đoạn sau khi PASS đúng build và working tree an toàn.
git fetch origin message feat/dm-p1-t01-user-search
git switch message
git merge --ff-only origin/message
git merge --no-ff origin/feat/dm-p1-t01-user-search -m "merge(dm): DM-P1-T01 into message"
# Chạy smoke/kiểm tra liên quan; nếu đổi hành vi phải bàn giao test lại.
git push origin message
git rev-parse message
git ls-remote --heads origin message
git merge-base --is-ancestor origin/feat/dm-p1-t01-user-search origin/message
# SHA message phải trùng remote, ancestry exit 0; sau đó mới xét task kế.
```

Chi tiết lệnh chạy, setup và response phải được agent kiểm chứng lúc triển khai. Các lệnh trong kế hoạch không phải bằng chứng đã chạy.

## Tổng hợp phase và task

**DM-P0-T01 đã được người dùng PASS và tích hợp vào message; DM-P1-T01 đã được người dùng PASS FE/BE build `9d995d0` ngày08/10/2026 và tích hợp vào message tại `d18d9a4`; DM-P1-T02 được người dùng PASS FE/BE build ea0c311 ngày09/10/2026; P1-T03 đã được người dùng nghiệm thu build9e47033 ngày09/10/2026; 16 task còn lại Chưa làm**. Phụ thuộc mặc định là task ngay trước trong bảng; mỗi mũi chuyển phải qua xác nhận FE/BE của người dùng. Không cam kết lịch khi chưa có kết quả task đầu.

| Phase | Task theo thứ tự | Mốc bàn giao |
|---|---|---|
| P0 | DM-P0-T01 | Contract gói, môi trường cô lập, dataset và baseline Identity test được |
| P1 | DM-P1-T01 → T02 → T03 | Tìm đúng người, mở một DM/cặp, xem inbox thật |
| P2 | DM-P2-T01 → T02 | Gửi lưu DB, validation, lịch sử phân trang |
| P3 | DM-P3-T01 → T02 | Retry đúng, không trùng, sequence/rotation/race được kiểm chứng |
| P4 | DM-P4-T01 → T02 | Realtime và reconnect bù lịch sử đúng |
| P5 | DM-P5-T01 → T02 → T03 | Sửa/xóa và xung đột không hồi sinh nội dung |
| P6 | DM-P6-T01 → T02 → T03 | Quyền/phiên/khóa, cutoff, mobile/IME/draft đúng |
| P7 | DM-P7-T01 | Gói DM nội bộ được người dùng nghiệm thu trên build chung |
| P8 | DM-P8-T01 → T02 → T03 | Restore bảo vệ, tải và phụ thuộc phát hành; kết quả DM không thay nghiệm thu toàn v1 |

## Phase P0 chuẩn bị bản chạy và điều kiện kiểm tra

### DM-P0-T01 môi trường dữ liệu và contract gói

Bản P0 đã được agent dựng/test và có [biên bản cùng hướng dẫn FE/BE](../acceptance/direct-messaging/DM-P0-T01.md), [baseline thiết kế](dm-p0-baseline-design.md). Trạng thái **Người dùng PASS FE/BE và baseline P0**; đã tích hợp vào `message` tại `170d959` ngày07/10/2026.

Branch `chore/dm-p0-t01-acceptance-baseline`. Nguồn: contract-4–10, DM-SQL-01–06, hồ sơ MVP. Phụ thuộc: không có task DM trước; Identity hiện có và công cụ local.

- [ ] P0-T01.1 Đọc source/nhánh cũ; lập ma trận reuse/replace/defer, chốt 7 REST operations và Hub schema cho gói; không coi endpoint cũ `/spaces` là contract mới.
- [ ] P0-T01.2 Chốt migration theo lát cắt, shared UoW/guard, key ring/HMAC tách biệt, sequence và mục chưa quyết định; không đổi policy sản phẩm.
- [ ] P0-T01.3 Dựng Compose test riêng/cổng/volume/config và proxy FE–API; kiểm tra PostgreSQL thật và startup.
- [ ] P0-T01.4 Tạo helper fixture A/B/C/U/K/S01–23, manifest ID, login nhiều actor và request corpus; không cài account sample vào production seed.
- [ ] P0-T01.5 Chạy baseline Identity, frontend tests/build; bàn giao smoke, cách start/stop không xóa volume và mẫu biên bản. Không làm search/send trong task này.

**Bạn test FE:** A/B/C login bằng username; đọc/sửa hồ sơ rồi reload; U login bị chặn. Logout A không đổi tài khoản B ở profile riêng. UI DM hiện còn mock phải được ghi đúng, không nghiệm thu mock là DM.

**Bạn test BE:** Health 200, GET `/users/me` đúng actor; không token bị 401; U không được cấp phiên ứng dụng. So khớp user ID từ API/manifest, DB riêng và cổng; chạy lại helper không tạo user trùng. Review contract tuyến/DTO/error cùng agent.

**Gate:** FE/BE baseline chạy thật, dataset tái lập và bạn duyệt thiết kế gói. Task này chỉ nghiệm thu nền/Identity; DM API vẫn chưa có. Biên bản ghi thiếu email thật và Development setup rõ ràng.

## Phase P1 tìm người và mở hội thoại

### DM-P1-T01 tìm người nhận

Branch `feat/dm-p1-t01-user-search`. AC-DM-01/16; TC-DM-01/18. Phụ thuộc P0-T01 được duyệt.

- [x] P1-T01.1 Bổ sung contract search do Identity thực hiện, projection không email/security state; kiểm tra actor/session và recipient active/verified.
- [x] P1-T01.2 Query q 2–64 UTF-16, case-insensitive/giữ dấu, substring literal cho `%`, `_`, `\`; rank username exact rồi username/ID; cursor bind actor/q/limit và key ring bền.
- [x] P1-T01.3 Thêm GET `/users/search`, DTO/ProblemDetails/OpenAPI; kiểm tra q sai, cursor sai và phiên không hợp lệ.
- [x] P1-T01.4 Nối UI tìm người thật: loading/empty/error/retry, chống response muộn khi đổi q, hiển thị username với tên trùng; chọn nhiều người theo ID, giữ lựa chọn khi đổi q/tải thêm/lỗi, bỏ từng người hoặc tất cả; chỉ hiển thị selection, chưa bịa conversation ID.
- [x] P1-T01.5 Integration/Unicode/pagination test và E2E tìm kiếm; bàn giao request/response đối chiếu.

**Bạn test FE:** A tìm `Bảo` thấy B/C phân biệt username; tìm `dm_demo_bao` ưu tiên B; `dm_demo_search` có trang tiếp; không tìm thấy A/U; search không có kết quả và lỗi API có thông báo. Ca K disabled có integration fixture ở task này và lượt người dùng E2E ở P6-T02 khi có helper khóa.

**Bạn test BE:** Cùng q qua Swagger/helper; thử 1/2/64/65 UTF-16, `%/_/\`, cursor đổi q hoặc actor; JSON không có email. DB đối chiếu kết quả/rank đủ người và không ghi membership khi search.

**Gate:** Đã PASS FE/BE build `9d995d0` theo xác nhận người dùng ngày08/10/2026. Đã merge/push vào `message` tại `d18d9a4`, remote/ancestry và smoke đạt; chưa nghiệm thu tạo DM, P1-T02 đã được người dùng PASS FE/BE build ea0c311 ngày09/10/2026 và merge message tại e5170b4.

### DM-P1-T02 tạo hoặc lấy một hội thoại duy nhất

Branch `feat/dm-p1-t02-open-conversation`. AC-DM-01/12/13; TC-DM-02/12/24; DM-SQL-01.

- [x] P1-T02.1 Triển khai shared transaction/UoW và Identity guard actor/peer giữ khóa tới commit; khóa user theo cùng thứ tự UUID. Messaging gọi Contracts, không query bảng Identity trực tiếp.
- [x] P1-T02.2 Model/mapping/migration space/cặp/member tối thiểu; UUID v7 server, low/high theo network bytes/DB; giữ unique pair và rollback toàn bộ khi lỗi.
- [x] P1-T02.3 POST `/direct-conversations` create-or-get 200; reject self/peer không hợp lệ, không rò metadata; giải quyết unique conflict bằng đọc lại an toàn.
- [x] P1-T02.4 UI chỉ chọn một người, bấm Mở hội thoại để mở conversation với hai người từ response, empty state thật; đồng thời click/loading/lỗi không tạo item mẫu.
- [x] P1-T02.5 Test A→B/B→A đồng thời, UUID endian, lỗi giữa transaction, guard cạnh revoke; recipe parallel request và SQL đếm pair/member/space.

**Bạn test FE:** A chọn B, B chọn A, reload/mở lại cùng D-AB; tên đúng và history rỗng. A chọn C ra D-AC khác. Không có thao tác tự nhắn.

**Bạn test BE:** Hai POST song song nhận cùng ID; đúng hai membership, một pair, không space mồ côi; self/peer pending bị từ chối. Disabled peer được test bằng integration fixture, lượt người dùng E2E ở P6-T02. C không được lấy D-AB từ inbox sau task kế. Task này test outsider với các API đã có, không giả endpoint detail chưa có.

**Gate:** Người dùng PASS FE/BE build ea0c311 ngày09/10/2026; [biên bản và lệnh C01–C04](../acceptance/direct-messaging/DM-P1-T02.md). Đã đủ điều kiện tích hợp vào message và triển khai inbox P1-T03 theo yêu cầu người dùng. Checkbox trên chỉ xác nhận implementation/test của agent. Theo yêu cầu09/10/2026, tạm chỉ chọn một người; chọn người khác thay lựa chọn trước, chọn nhiều người chưa hoạt động.

### DM-P1-T03 inbox dữ liệu thật

Branch `feat/dm-p1-t03-conversation-inbox`. AC-DM-01/12, contract danh sách; cần D-AB/D-AC.

- [x] P1-T03.1 GET `/direct-conversations` chỉ actor member; projection participant, activity và pagination 20/50/cursor protected.
- [x] P1-T03.2 Kiểm tra auth từng trang, dedup ID trên UI và refresh trang đầu khi danh sách thay đổi; không hứa snapshot cố định.
- [x] P1-T03.3 Thay DM mock bằng loader/inbox thật; loading/empty/error/retry và chọn conversation sau reload. Modal chọn người có mục Người vừa nhắn tin: lấy peer từ hội thoại của actor có lastActivityAt khác null, theo lastActivityAt DESC rồi conversation ID ASC, dedup peer ID, bỏ actor; hiện displayName/username, cho chọn/bỏ chọn chung với kết quả search. Hội thoại chưa có tin không vào mục này; API lỗi có retry, không fallback mock/localStorage lượt chọn.
- [x] P1-T03.4 Cache list theo actor và cleanup logout; response cũ không ghi dữ liệu vào actor mới.
- [x] P1-T03.5 Seed thêm DM A–S để test >20 hội thoại qua API; E2E empty/list/pagination/actor switch.

**Bạn test FE:** A thấy D-AB/D-AC và đủ các trang; B chỉ thấy DM của B; C không thấy D-AB; tài khoản mới inbox rỗng, API lỗi không fallback mock.

**Bạn test BE:** Compare list với membership, kiểm tra limit/cursor dùng chéo actor; U/no token không đọc list. Không dùng lastActivity làm cursor lịch sử tin.

Yêu cầu bổ sung ngày 08/10/2026: “người bạn gần nhất” là **người vừa nhắn tin**, không phải người vừa chọn. P1-T03 dựng UI/loader theo `lastActivityAt`; khi chưa có writer thì test empty state và phân quyền, ghi rõ thứ tự có tin còn chờ P2-T01. Không seed tin trực tiếp vào DB để tuyên bố nghiệm thu. P2-T01 chạy lại mục gần đây bằng writer thật và bàn giao người dùng test; chưa chạy task đó trong lần sửa P1-T01.

**Gate:** Người dùng đã nghiệm thu build9e47033 ngày09/10/2026; [biên bản và recipe C01–C04](../acceptance/direct-messaging/DM-P1-T03.md). Đã merge --no-ff vào message tại acce838; source giống build đã nghiệm thu, smoke API/DB PASS. P2/P3 chưa được triển khai; người dùng giao P4-T01 nhưng cần xác định phạm vi các tiền đề còn thiếu trước khi làm phần phụ thuộc.

## Phase P2 gửi lưu bền và lịch sử

### DM-P2-T01 gửi văn bản và validation

**Đã triển khai và agent test; chờ người dùng nghiệm thu FE/BE.** [Biên bản P2-T01](../acceptance/direct-messaging/DM-P2-T01.md) có lệnh/sample và C01–C04/RECENT. Checkbox dưới đây chỉ trạng thái triển khai; chưa merge message hoặc giao P2-T02.

Branch `feat/dm-p2-t01-persist-text-message`. AC-DM-02/09/11/19/21; TC-TEXT-01–05, TC-DM-03/14/21/23; DM-SQL-02–06.

- [x] P2-T01.1 Migration counter/conversation_sequence, SendOperation/fingerprint, tombstone-compatible constraint và outbox ID/version; không ghi `message_edits.previous_content`.
- [x] P2-T01.2 Validator client/server theo fixture Unicode; HMAC key/version tách key ring; same-key retry trả trạng thái hiện hành, payload khác conflict ngay từ writer đầu tiên.
- [x] P2-T01.3 Guard actor/member/peer, thứ tự khóa thống nhất; counter + message + operation + outbox/projection cùng transaction, rollback không để trạng thái dở.
- [x] P2-T01.4 POST `/direct-conversations/{id}/messages`; API wrapper `retry:false`; UI sending/sent/error, tạm và response merge cùng ID; chỉ text, không API Hub mutation.
- [x] P2-T01.5 Backend/FE/E2E send và biên content; bàn giao SQL đọc message/operation/outbox và hiện trạng chưa có realtime/history đầy đủ. Kiểm thử Người vừa nhắn tin trên run riêng D-AB/D-AC ban đầu rỗng: commit thật A→B, A→C, B→A; mục gần đây của A lần lượt chỉ B, C→B, B→C. Refresh inbox/modal sau mỗi commit, đối chiếu API và DB; gửi lỗi trước commit không đổi thứ tự.

**Bạn test FE:** A gửi M01/M02/M03/M04 thấy sent sau response; L2000/E2000 nhận, L2001/E2002/EMPTY bị từ chối; HTML không chạy. DB lỗi/fault trước commit hiện lỗi, không báo sent.

**Bạn test BE:** POST cùng corpus; response sequence/version là chuỗi, content CRLF chuẩn hóa đúng; DB một message/operation/outbox, không payload body trong outbox/log. C/no token bị chặn; cùng O1 khác body không tạo tin mới. B xem bằng GET history ở task kế; task này đối chiếu response/DB trước.

**Gate:** Writer lưu bền/validation/guard/idempotency nền đạt. Không trì hoãn chống trùng tới P3; P3 mở rộng fault/UI retry và proof.

### DM-P2-T02 lịch sử phân trang và tin khi vắng mặt

Branch `feat/dm-p2-t02-message-history`. AC-DM-03/07/12/18/21; TC-DM-03/04/15/20/25.

- [x] P2-T02.1 GET messages latest/before/after bootstrap theo counter committed; default 50/max100, items tăng sequence, protected cursor và mốc through đúng contract.
- [x] P2-T02.2 Key ring bền; cursor gắn actor/conversation/direction/filter/limit/expiry; reject before+after, cursor bị sửa/dùng chéo; auth từng trang.
- [x] P2-T02.3 UI load latest/load older/scroll ổn định, loading/error/retry không bỏ tin đang thấy; không dùng sequence difference làm unread.
- [x] P2-T02.4 Tạo H121 qua writer, B offline rồi login; bind message ID thật vào manifest.
- [x] P2-T02.5 Test lịch sử không thiếu/trùng, rollback/late commit, bigint lớn và restart key ring; UI/API/DB cùng tập ID.

**Bạn test FE:** B mở lại D-AB thấy M01 dù offline lúc gửi; A mở D-HIST thấy H121 đủ ba trang, scroll không nhảy; reload API không mất history; lỗi tải thêm vẫn giữ trang đang thấy.

**Bạn test BE:** latest/before trả đúng 50/50/21 trên dataset riêng; cursor chéo user/DM/bị sửa bị từ chối; C không đọc D-AB; sequence sort số đúng. Tắt/bật API, DB còn tin và cursor chưa hết hạn còn dùng được.

**Gate:** Gửi–đọc–reload chạy với DB thật. Bạn PASS rồi mở fault/retry.

## Phase P3 lỗi thử lại và đồng thời

### DM-P3-T01 retry chủ động khi chưa rõ kết quả

Branch `feat/dm-p3-t01-manual-retry`. AC-DM-06/08/21; TC-DM-05–08/23.

- [x] P3-T01.1 UI failed action giữ clientMessageId/content bất biến; nút Thử lại không tạo ID mới; nội dung muốn đổi phải là thao tác mới.
- [x] P3-T01.2 Tắt automatic mutation replay ở 401/refresh/network/reconnect; GET vẫn được refresh theo policy.
- [x] P3-T01.3 Fault harness local/test: rollback trước commit, drop response sau commit, delayed response/401; không thêm fault controls vào flow sản phẩm.
- [x] P3-T01.4 Correlate tin tạm/response theo actor+clientMessageId; retry cùng khóa trả ID hiện hành, không duplicate dòng.
- [x] P3-T01.5 E2E/network fault và DB count, recipes để người dùng lặp lỗi; tắt fault xác nhận recovery.

Triển khai P3-T01 và kiểm chứng agent tại [biên bản P3-T01](../acceptance/direct-messaging/DM-P3-T01.md); checkbox ghi phần implementation, **chưa là người dùng PASS**. P2-T02 được người dùng PASS FE/BE ngày10/10/2026, đã merge/push vào `message` tại `b4d814d`. P3-T01 đã được người dùng PASS FE/BE build `156cb32` ngày10/10/2026, merge/push vào `message` tại `a3fea4e`. P3-T02 được giao riêng; P4 chưa thực hiện. Actual Hub event/reconnect trong AC-DM-21/TC-DM-23 chờ P4; hiện chứng minh merge bằng GET thật và response retry.

**Bạn test FE:** Fault trước commit → failed; bật mạng không tự gửi; bấm Retry mới sent. Drop response sau commit → failed/unknown; Retry đúng một dòng. Nhập cùng nội dung lần mới vẫn tạo tin thứ hai với khóa mới.

**Bạn test BE:** O1 lặp/song song chỉ một message/operation/create outbox; O1/body khác 409 OPERATION_CONFLICT; O2/body giống là tin mới. 401 không có POST tự replay trong Network. Fault trước commit DB không có tin.

**Gate:** Cả FE/BE chứng minh retry không trùng và không tự gửi lại. Bạn PASS rồi mở proof concurrency/key.

### DM-P3-T02 sequence transaction fingerprint và key rotation

Branch `test/dm-p3-t02-concurrency-and-keys`. TC-DM-02/15/24/25; fixtures fingerprint; DM-SQL-01–06.

- [x] P3-T02.1 Deterministic barrier giữ writer thứ nhất chưa commit, writer thứ hai chờ; kiểm tra counter/commit và rollback, không dùng sleep ngẫu nhiên làm proof.
- [x] P3-T02.2 Test fingerprint byte-order/hash/constant-time path, CRLF normalization; key rotation gửi mới/retry cũ, thiếu key 503 không tạo tin.
- [x] P3-T02.3 Migration legacy giữ đọc lịch sử; operation không fingerprint không bịa từ content hiện hành, retry trả OPERATION_UNVERIFIABLE; migration chạy lại không phá DB.
- [x] P3-T02.4 Kiểm tra UI khi hai send/response đảo thứ tự và key unavailable; lỗi có thử lại chủ động, không mất tin/counter.
- [x] P3-T02.5 Bàn giao runner song song/barrier, fixture key tổng hợp không commit key thật, SQL invariants và report.

**Bạn test FE:** A1/A2 gửi hai tin đồng thời, B reload thấy đúng hai tin theo sequence; ngắt key mới rồi retry cũ có kết quả đúng, key thiếu hiện lỗi và không thêm dòng sent giả.

**Bạn test BE:** Chạy runner giữ transaction theo hướng dẫn; không có trang history bỏ sót commit trễ; rollback không tạo operation/outbox; hash khớp fixture, retry key cũ hoạt động sau restart/rotation; legacy retry không trùng.

**Gate:** Đồng thời/keys/migration có proof API/DB và UI tương ứng; bạn PASS rồi mới realtime.

P3-T02 đã triển khai5subtask và agent kiểm chứng backend76/76, Node53/53, build và Edge5/5; [biên bản P3-T02](../acceptance/direct-messaging/DM-P3-T02.md) có run FE/BE riêng, UUID/ID và từng recipe. Checkbox chỉ ghi implementation. **Chờ người dùng test FE/BE P3-T02**, chưa merge message/chưa làm P4. TC-DM-25 REST/cursor đã kiểm chứng; Hub reconnect/catchup tự động và dispatcher DM-SQL-06 còn chờ P4, không coi toànTC/UC đã PASS.

## Phase P4 realtime và reconnect

### DM-P4-T01 Hub outbox và merge sự kiện

Branch `feat/dm-p4-t01-realtime-outbox`. AC-DM-02/12/21; TC-DM-03/12/16/23.

- [ ] P4-T01.1 Map `/hubs/chat` với auth token đúng route, session reader/registry theo user/session/conversation; SubscribeConversation/UnsubscribeConversation đúng schema.
- [ ] P4-T01.2 Dispatcher lease/claim sau commit, retry/backoff/recovery; payload tham chiếu, lúc phát đọc current message/version và kiểm tra quyền connection.
- [ ] P4-T01.3 FE handler/buffer đăng ký trước subscribe, merge ID/clientMessageId/version chính xác; event trước response/trùng/đảo thứ tự không nhân tin.
- [ ] P4-T01.4 UI trạng thái connection thật; dừng worker làm DB lưu tin nhưng không giả realtime online delivery; không thêm “đã đọc/đã nhận”.
- [ ] P4-T01.5 Test worker crash/restart, unsubscribe, outsider subscribe, late event; recipe trực tiếp Hub qua browser runner/helper.

**Bạn test FE:** A/B mở D-AB, A gửi B thấy không reload; response/event đảo thứ tự chỉ một dòng; worker dừng rồi chạy lại khôi phục cập nhật; C không nhận tin D-AB.

**Bạn test BE:** DB outbox commit/publish đúng; rollback không có event; subscribe outsider bị từ chối không rò metadata; retry worker không tạo message mới; logs không token/body.

**Gate:** Hai browser nhận tin thật và quyền subscribe/dispatch đạt. Thu hồi đầy đủ vẫn phải qua P6; bạn PASS rồi reconnect.

### DM-P4-T02 reconnect nhiều trang và sửa xóa tin cũ

Branch `feat/dm-p4-t02-reconnect-history`. AC-DM-15/21; TC-DM-16/17/25; contract-8.

- [ ] P4-T02.1 Reconnect subscribe lại, buffer trước subscribe/ack và GET bù tới through cố định; không dùng eventId/highest live sequence làm frontier đã đọc đủ.
- [ ] P4-T02.2 Sau merge mỗi trang mới tiến cursor; chỉ nhận resumeCursor cuối; gián đoạn giữa ba trang tiếp tục đúng, cursor hết hạn tải lại.
- [ ] P4-T02.3 Reload cửa sổ lịch sử đang xem, invalidation cache cũ; wire cùng Message schema để chuẩn bị edit/delete ở P5.
- [ ] P4-T02.4 Offline UI, GET recovery, tin failed vẫn chờ bấm retry; response actor cũ không ghi lại cache.
- [ ] P4-T02.5 R101 và interruption runner, duplicate/out-of-order events và cursor faults. Edit/delete server chưa có thì dùng fixture ở test thành phần, ghi rõ; proof E2E tin cũ chạy lại sau P5.

**Bạn test FE:** B offline, A gửi R101, B reconnect nhận đủ ba trang; mất mạng sau trang 1/2 rồi nối lại không mất/trùng; failed message không tự gửi.

**Bạn test BE:** H/after/frontier/nextCursor qua ba request đúng; cursor sửa/chéo/expired bị chặn; compare set ID DB = FE. Phiên invalid không subscribe hay history được.

**Gate:** Reconnect send/history thật đạt; nhánh edit/delete tin cũ chưa đánh dấu đạt tại task này và là gate bắt buộc ở P5-T03.

## Phase P5 sửa xóa và chống hồi sinh

### DM-P5-T01 sửa tin của mình

Branch `feat/dm-p5-t01-edit-message`. AC-DM-04/10/11/19; TC-DM-09/13; TC-TEXT-06.

- [ ] P5-T01.1 PATCH expectedVersion; actor/member/author guard, same text đúng version là no-op; validation chung và không yêu cầu peer active.
- [ ] P5-T01.2 Compare-and-update/version + outbox cùng transaction; không lưu content cũ trong bất kỳ edit/event/log payload.
- [ ] P5-T01.3 UI menu chỉ author, edit/cancel/save/error/conflict; giữ bản xác nhận gần nhất khi lỗi, hiển thị “Đã sửa”.
- [ ] P5-T01.4 Dispatch DTO mới và merge version; B/A2 nhận text mới; update inbox projection nếu có mà không giữ bản cũ.
- [ ] P5-T01.5 API/UI/DB test sửa nhiều lần/no-op/biên/outsider/non-author, offline reload và recipe.

**Bạn test FE:** A sửa M01 hai lần, B/A2 thấy mới nhất; Cancel không đổi; B không có menu sửa A; text biên bị từ chối và không làm mất bản đã lưu.

**Bạn test BE:** B/C PATCH A bị chặn; expectedVersion cũ conflict; no-op không tăng version/outbox/editedAt; DB/event/log không có body cũ, GET không có version-history API.

**Gate:** Edit đúng tác giả/phiên bản và chỉ nội dung mới nhất. Bạn PASS rồi delete.

### DM-P5-T02 xóa tin giữ tombstone

Branch `feat/dm-p5-t02-delete-message`. AC-DM-05/10; TC-DM-08/10/13.

- [ ] P5-T02.1 DELETE expectedVersion, auth trước cả delete lặp; lần đầu content null/deletedAt/version, giữ ID/sequence/SendOperation.
- [ ] P5-T02.2 Outbox/current DTO giữ tombstone không body; lần xóa lặp trả current tombstone không tăng version/outbox; PATCH tombstone MESSAGE_DELETED.
- [ ] P5-T02.3 UI confirmation/cancel/error; giữ vị trí/thời gian với “Tin nhắn đã bị xóa”, bỏ body/menu sửa ở client/cache.
- [ ] P5-T02.4 Retry send ban đầu sau delete trả tombstone cùng ID; HMAC so với original operation, không dựng lại body.
- [ ] P5-T02.5 DB invariant/migration/contract tests và UI hai actor.

**Bạn test FE:** A xóa M01, B thấy dòng thay thế tại đúng vị trí; Cancel không xóa; API lỗi giữ tin xác nhận gần nhất; reload vẫn tombstone.

**Bạn test BE:** DELETE lặp đúng author an toàn, non-author bị chặn; PATCH deleted conflict; replay O1 trả cùng tombstone, DB content null và operation còn nguyên, không message/create-event mới.

**Gate:** Xóa không mất vị trí hoặc hồi sinh body. Bạn PASS rồi race/reconnect proof.

### DM-P5-T03 xung đột edit delete và reconnect tin cũ

Branch `test/dm-p5-t03-mutation-races`. AC-DM-14/15/21; TC-DM-08/11/17/23/25.

- [ ] P5-T03.1 A1/A2 edit-edit và edit-delete từ cùng version, barrier deterministic; một thắng, một conflict, UI tải current.
- [ ] P5-T03.2 Replay original send sau nhiều edit/delete, cùng key khác body; không sinh message/outbox tạo mới. Nhánh peer disabled được kiểm chứng ở P6-T02 với helper khóa riêng, không đặt phụ thuộc ngược vào task này.
- [ ] P5-T03.3 B offline với tin cũ trong cửa sổ; A sửa/xóa, thêm R101; reconnect bù hết trang và reload cửa sổ cũ bằng API thật.
- [ ] P5-T03.4 Event cũ sau version mới/tombstone không thay body; no-op/delete lặp cùng kiểm tra tác giả.
- [ ] P5-T03.5 Bàn giao conflict/lost response/reconnect recipes, DB counts và E2E proof cho nhánh còn chờ ở P4-T02.

**Bạn test FE:** Hai phiên A sửa/xóa cùng lúc, không ghi đè âm thầm; B reconnect thấy text mới/tombstone của tin cũ và đủ R101; Retry original không kéo body cũ về.

**Bạn test BE:** Runner chứng minh một version transition thắng, expectedVersion loser conflict; compare latest/version/tombstone DB với REST/event; không lưu content cũ hoặc tăng create counts khi replay.

**Gate:** Đóng phần reconnect edit/delete còn thiếu P4, có UI/API/DB thật. Bạn PASS rồi kiểm chứng bảo mật hệ thống.

## Phase P6 quyền phiên và trải nghiệm

### DM-P6-T01 ma trận quyền và giới hạn dữ liệu riêng

Branch `test/dm-p6-t01-access-matrix`. AC-DM-09/12; ACL-02–05; TC-DM-12–14.

- [ ] P6-T01.1 Ma trận A/B/C/U/no token × search/open/list/history/send/edit/delete/subscribe/retry; kiểm tra từng đường auth không chỉ menu UI.
- [ ] P6-T01.2 C thử ID D-AB/message/cursor/Hub; B thử mutation A; không rò participants/body/email qua lỗi hoặc timing payload rõ rệt.
- [ ] P6-T01.3 Phiên stale/expired/unverified chuyển trạng thái trong fixture: validator và guard chặn; không tạo token bypass cho sản phẩm để test U.
- [ ] P6-T01.4 UI logout/login C trong cùng tab, response A muộn/GET lỗi/storage fault không hiện history của A hoặc fallback mock.
- [ ] P6-T01.5 Rà logs/outbox/DOM/storage, test auth/replay toàn đường và hồ sơ ma trận.

**Bạn test FE:** C không thấy D-AB, biết URL/ID cũng không xem; B không sửa/xóa A; U login bị chặn. Đổi A→C không còn tin/nháp A kể cả request A về muộn.

**Bạn test BE:** Dùng tokens A/B/C hợp lệ gọi ma trận; U không có phiên là ca 401 riêng, không giả đó là đủ proof unverified. Runner isolated thay verified/state của phiên thử để kiểm tra validator/guard; C subscribe không nhận nội dung.

**Gate:** Mọi route/routing đúng quyền và không rò dữ liệu. Bạn PASS rồi cutoff/khóa peer.

### DM-P6-T02 thu hồi phiên khóa tài khoản và deadline

Branch `feat/dm-p6-t02-session-revocation`. DEC-083/104; TC-DM-26; contract guard/read/send/author-mutation.

- [ ] P6-T02.1 Session reader/revoker theo session/user, registry cleanup và auth expiry; event sau commit + reconciliation tối đa 1 giây theo thiết kế; fail closed nếu đọc quyền lỗi.
- [ ] P6-T02.2 Fixture helper K disabled/unlock chỉ `_test`, ghi status/stamp/revoke và receipt kiểm thử; không public admin API, không nhầm lockout 15 phút với disabled.
- [ ] P6-T02.3 Guard read/author mutation/send mới tách purpose: K disabled vẫn cho A đọc/sửa/xóa tin A, chặn send mới tới K; retry key đã commit trả current nếu A còn quyền.
- [ ] P6-T02.4 Race revoke với send/subscribe/dispatch giữ UoW/lock order; hết phiên/logout-all không reconnect bằng token cũ.
- [ ] P6-T02.5 Timestamp commit/cutoff ≤5 giây, fault authority/worker, UI status và scripts/DB proof.

**Bạn test FE:** A1 thu hồi A2 từ UI sessions: A2 dừng nhận dữ liệu; logout-all không tự phục hồi. A–K đã có history, khóa K: A còn đọc/sửa/xóa tin mình nhưng không gửi mới; unlock K cần login mới.

**Bạn test BE:** Token A2 cũ HTTP/Hub bị chặn; runner đo commit→last delivery, không chỉ event AccessRevoked; race không có mutation sau revoke theo thứ tự guard. Old-key retry với peer disabled chỉ đọc kết quả, không tạo operation mới.

**Gate:** Deadline đạt qua dữ liệu thực ở server/browser; thiếu công cụ khóa hoặc timestamp proof là Bị chặn, không đánh dấu PASS toàn task.

### DM-P6-T03 mobile IME draft và cache theo actor

Branch `feat/dm-p6-t03-composer-and-drafts`. AC-DM-17/20; TC-DM-19/22; UX-DM.

- [ ] P6-T03.1 Desktop Enter gửi/Shift+Enter xuống dòng, mobile Enter xuống dòng, Send luôn gửi; composition IME không gửi sớm.
- [ ] P6-T03.2 Map nháp theo actor/conversation trong bộ nhớ, giữ khi chuyển D-AB↔D-AC; reload/logout/actor switch xóa, không localStorage/IndexedDB nội dung.
- [ ] P6-T03.3 Tách draft chưa gửi khỏi failed operation; sent vẫn DB, failed không tự replay; request muộn không phục hồi cache sau logout.
- [ ] P6-T03.4 Layout wide/narrow, keyboard/focus/errors bằng chữ, load older/retry/edit/delete sử dụng được; không thêm developer controls vào flow người dùng.
- [ ] P6-T03.5 E2E với IME/multiple profiles/mobile viewport và lượt kiểm tra thiết bị thật được ghi rõ; không coi giả viewport là đủ proof bàn phím mobile.

**Bạn test FE:** Nhập tiếng Việt IME/emoji/nhiều dòng; các phím đúng desktop và điện thoại; chuyển DM giữ nháp, reload/logout mất nháp; A/B không chia sẻ draft, C không nhận draft A.

**Bạn test BE:** Network: composition/chuyển DM/reload không có POST bất ngờ; send payload đúng LF/UTF-16; số message tăng đúng thao tác Gửi, không tăng vì auto reconnect; DB history còn sau reload.

**Gate:** FE thiết bị/thao tác và BE counts/payload cùng đạt. Browser/OS/thiết bị thực tế ghi trong biên bản; ma trận toàn v1 kiểm tra tiếp ở P8.

## Phase P7 nghiệm thu gói DM nội bộ

### DM-P7-T01 regression bản tích hợp và bàn giao

Branch `test/dm-p7-t01-internal-acceptance`. AC-DM-01–21, TC-DM-01–26, TC-TEXT-01–06 trong phạm vi DM; không giả gửi tin phòng đã đạt vì dùng validator chung.

- [ ] P7-T01.1 Chốt commit/config/schema/dataset, rebuild từ checkout sạch, migrations fresh/existing, key ring/HMAC bền qua restart, proxy Web–API–Hub thật.
- [ ] P7-T01.2 Regression mọi task trên build chung; kiểm tra message/operation/outbox/version/tombstone, no old body và không tự hết hạn history.
- [ ] P7-T01.3 Happy path A/B, outsider C, pending U, disabled K; offline, 101 tin, worker restart, mutation race, cursor/keys/revoke và browser thật.
- [ ] P7-T01.4 Chứng minh restart ứng dụng không mất DB/key/cursor trong hạn; log không secrets/body; retention chưa triển khai không được tự purge marker/history.
- [ ] P7-T01.5 Hồ sơ mẫu task/release, lỗi tồn, known limits/email dependency/P8; bàn giao commands và dataset cho người dùng tự chạy từ đầu.

**Bạn test FE:** A tìm B→mở DM→gửi→B nhận→reload→sửa→xóa→offline/reconnect; repeat trên narrow/mobile; C/U/K đúng hành vi; tự restart rồi mở history.

**Bạn test BE:** Chạy collection + SQL read-only trên cùng build, compare IDs/counts/current body, auth negative/race/cutoff; runtime qua proxy. Fail case không được bỏ khỏi report.

**Gate:** Bạn PASS gói DM văn bản nội bộ, xác nhận build và lỗi tồn. P8 chỉ thực hiện khi được giao tiếp; email thật, restore và tải còn mở phải hiện trong kết luận, không mở công khai từ PASS P7.

## Phase P8 hoàn thiện DM cho điều kiện v1

### DM-P8-T01 bảo vệ dữ liệu khi restore và retention

Branch `feat/dm-p8-t01-data-protection`. DEC-106–109; AC-DATA/TC-DATA và Message.contentState trong đặc tả. Phụ thuộc: chọn kho sổ/key/checkpoint và quy trình vận hành tại OQ-011/OQ-008; thiếu đầu vào thì chuẩn bị phương án cụ thể và chờ quyết định cần thiết.

- [ ] P8-T01.1 Thiết kế/triển khai sổ bảo vệ độc lập chỉ ID/version/revocation markers; prepared/DB commit/receipt/abort, idempotency/watermark, đối soát trạng thái unknown; outbox ghi sau đơn thuần không đủ.
- [ ] P8-T01.2 Mutation cần bảo vệ không bypass khi kho sổ lỗi; receipt mất có kết quả chưa rõ đúng contract; cursor/keys/dedup/deny marker phục hồi tương thích.
- [ ] P8-T01.3 Restore DB cũ trong môi trường riêng rồi áp floor/tombstone/thu hồi trước mở; bản sửa mới mất không phục vụ body cũ, trả unavailable_after_restore/content null và UI “Nội dung chưa khôi phục được”.
- [ ] P8-T01.4 Writer author được viết body mới hoặc xóa placeholder theo version/quyền; cleanup terminal/outbox theo policy, giữ active families/dedup, log14/audit90/IP7 ngày đúng phạm vi sở hữu.
- [ ] P8-T01.5 Diễn tập fault từng bước, sổ/key/checkpoint thiếu, prepared chưa rõ và legacy retry; backup tuổi30 ngày, RPO≤15 phút/RTO≤4 giờ đo cùng hạ tầng liên quan, không restore đè môi trường đang dùng.

**Bạn test FE:** Trước backup A có M01; sau backup sửa/xóa/thu hồi; restore riêng: deleted không hồi sinh, edited mất bản mới là placeholder, old session bị chặn; author viết mới/tombstone theo quyền.

**Bạn test BE:** So sổ/DB/watermark và current versions, replay O1 không dựng body cũ; thiếu sổ/unknown giữ scope chưa mở; đo RPO/RTO và kiểm tra artefact age. Cleanup không xóa marker đang còn cần.

**Gate:** Restore/protection có proof cả UI/API/data; không tự ký đạt khi chưa chọn kho sổ hoặc không đối soát được trạng thái.

### DM-P8-T02 tải trình duyệt và observability

Branch `perf/dm-p8-t02-load-and-observability`. DEC-082/083 và [quality targets](../release-operations.md#quality-targets). Mục tiêu toàn chat cần Community workload, không dùng DM-only thay toàn benchmark.

- [ ] P8-T02.1 Chốt workload/thiết bị/resources/clock/sample; chuẩn bị synthetic load và metrics không body/token; nhãn riêng validation/fault và lỗi hệ thống.
- [ ] P8-T02.2 Đo DM send→save→recipient, API/backlog/connections, error/correctness, p50/p95/p99 và cutoff; fixture gần 100.000 tin khi scope benchmark đã chọn.
- [ ] P8-T02.3 Chạy DM load trước, tích hợp 100 online/60 phút sau warm-up 5 phút theo workload toàn chat khi Community sẵn sàng; API/gửi→lưu p95≤500 ms, commit→UI online p95≤1 giây, lỗi dịch vụ <1% và thu hồi≤5 giây theo docs, không tự giảm target.
- [ ] P8-T02.4 Desktop/mobile browser matrix theo phiên bản ổn định tại nghiệm thu; recording build/OS/device/network, IME/cursor/UI under load.
- [ ] P8-T02.5 Alert backlog/error/revoke, dashboards/report, user recipe vừa quan sát UI vừa chạy runner và đối chiếu IDs DB.

**Bạn test FE:** Trong khi runner load chạy, A/B thao tác send/edit/delete/history/reconnect trên browser thật; trạng thái không sai, không duplicate và UI dùng được.

**Bạn test BE:** Review raw aggregates/sample/clock/workload và DB correctness, thử alert; benchmark DM đạt không tự đánh dấu toàn workload Community đạt. Mọi target bắt buộc thiếu proof giữ chưa đạt/chưa đánh giá.

**Gate:** Bạn xác nhận phạm vi DM đã đo; gate toàn chat chỉ PASS khi Community tích hợp và ngưỡng toàn bộ đạt.

### DM-P8-T03 tích hợp phụ thuộc Identity và hồ sơ phát hành

Branch `chore/dm-p8-t03-release-handoff`. Phụ thuộc: gói Identity/email, cấu hình domain/key store, quyền vận hành/người ký và các gate v1 liên quan. Đây là tích hợp các gói đã có, không tự làm toàn bộ email/media/microservice trong task DM.

- [ ] P8-T03.1 Tích hợp bản Identity/email đã duyệt; account mới nhận verify/reset link thật; Development token tắt trên bản release.
- [ ] P8-T03.2 HTTPS/CORS/proxy/Hub/config/secrets, migrations và CI; deploy/rollback rehearsal giữ protection/epoch/key và DB/object dependency nếu có.
- [ ] P8-T03.3 Smoke user mới: register→email→verify→login→DM; logout/reset/revoke, old token và third-party access đúng; DM không có self-delete/admin-read.
- [ ] P8-T03.4 Tổng hợp hồ sơ RLS gates, lỗi tồn/người phụ trách, scope chưa xong; chọn người có thẩm quyền theo quy trình, không suy người dùng duyệt task là người ký production.
- [ ] P8-T03.5 Bàn giao artefact/runbook/dataset và kết quả frontend/backend, dừng trước publish/mở public nếu chưa được phép rõ ràng.

**Bạn test FE:** Dùng hai email thử được kiểm soát nhận link thật, tạo account mới rồi gửi/nhận/sửa/xóa/reconnect qua Web release; reset/logout không hồi sinh phiên cũ.

**Bạn test BE:** Swagger chỉ dùng môi trường phù hợp, release có helper REST collection thay nếu tắt Swagger; health/readiness, auth/cutoff, migration/rollback và logs không secrets; đối chiếu release gates.

**Gate:** Bạn PASS phần DM trên build release và ghi phần v1 khác còn mở. Publish/merge/quyết định phát hành vẫn theo phạm vi quyền đã cấp; không tự phát hành từ kế hoạch này.

## Prompt và hồ sơ cho lần thực hiện

[Bộ prompt từng task](direct-messaging-prompts.md) có prompt chung và 20 prompt riêng. Mỗi prompt chỉ chạy một task, bàn giao bản FE/BE test được rồi dừng. [Mẫu biên bản](../templates/dm-task-acceptance.md) yêu cầu tách kết quả agent và người dùng, ghi build/actor/dataset/UI/request/DB proof và lỗi.

Task đầu cần giao khi bắt đầu là **DM-P0-T01**. Nếu nhánh cũ có phần tương thích, reuse trong task đúng phạm vi để giảm công viết lại; vẫn phải qua cùng gate frontend/backend. Kế hoạch này giữ mọi task Chưa làm cho đến khi thực thi và được kiểm tra theo quy trình trên.

### Xác nhận chuyển P2 ngày10/10/2026

P2-T01 được người dùng nghiệm thu có điều kiện C02 PASS. Agent đã tái kiểm thử C02 PASS FE/BE; người dùng trả lời “Đồng ý; C02 PASS thì làm P2-T02”. Tích hợp P2-T01 vào `message`, chỉ thực hiện P2-T02 và bàn giao FE/BE trước task kế. Xem [biên bản P2-T01](../acceptance/direct-messaging/DM-P2-T01.md).

### Bàn giao lịch sử P2-T02 ngày10/10/2026

Base `message` đã tích hợp P2-T01 tại `ea941a7`. P2-T02 đã có API history, UI loader, H121 qua writer và kiểm thử kỹ thuật; [biên bản P2-T02](../acceptance/direct-messaging/DM-P2-T02.md) ghi kết quả và cách người dùng test. Các checkbox P2-T02 là việc triển khai đã làm; gate người dùng FE/BE vẫn **Chờ xác nhận**, chưa merge và không tự làm P3/P4.

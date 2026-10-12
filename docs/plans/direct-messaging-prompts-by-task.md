# Prompt đầy đủ cho từng task nhắn tin riêng SCDC

Nhánh tích hợp của gói DM là `message`. Quyền push nhánh task và merge vào `message` đã được cấp trong phiên; merge chỉ sau khi tôi PASS FE và BE trên build bàn giao.

Mỗi task bên dưới có **một khối prompt hoàn chỉnh**. Copy toàn bộ nội dung trong khối của task cần làm rồi gửi cho agent. Không cần ghép prompt chung, tự điền task ID hoặc branch. Mỗi prompt chứa phạm vi, 5 subtask, Git flow, dữ liệu mẫu, kiểm tra FE/BE, đầu ra và yêu cầu dừng chờ bạn test.

Nguồn phân chia công việc là [kế hoạch DM](direct-messaging.md). Agent đọc source/contract trong repository bằng các đường dẫn đã có trong prompt; bạn không cần copy thêm nội dung tài liệu vào chat. Prompt chỉ giao một task, không khởi chạy toàn kế hoạch.

Mỗi task có **4 ví dụ test case chi tiết ngay trong khối prompt**, tổng cộng **80 ca**: điều kiện trước, dữ liệu, bước FE/BE, expected UI/API/DB, bằng chứng và cleanup. Bản máy đọc nằm tại [dm-acceptance-cases.json](../fixtures/dm-acceptance-cases.json). Đây là kế hoạch chưa thực thi; agent phải bàn giao lệnh/SQL cụ thể trên bản chạy khi làm task.

## Chọn task cần thực hiện

Bắt đầu với DM-P0-T01. Task kế chỉ triển khai sau khi task trước đã được bạn kiểm tra FE/BE và có quyền đi tiếp trong phạm vi đã giao. Agent đối chiếu trạng thái thực tế khi nhận prompt, không tự ghi PASS.

| Task | Prompt đầy đủ |
|---|---|
| DM-P0-T01 | [môi trường dữ liệu và contract gói](#dm-p0-t01) |
| DM-P1-T01 | [tìm người nhận](#dm-p1-t01) |
| DM-P1-T02 | [tạo hoặc lấy một hội thoại duy nhất](#dm-p1-t02) |
| DM-P1-T03 | [inbox dữ liệu thật](#dm-p1-t03) |
| DM-P2-T01 | [gửi văn bản và validation](#dm-p2-t01) |
| DM-P2-T02 | [lịch sử phân trang và tin khi vắng mặt](#dm-p2-t02) |
| DM-P3-T01 | [retry chủ động khi chưa rõ kết quả](#dm-p3-t01) |
| DM-P3-T02 | [sequence transaction fingerprint và key rotation](#dm-p3-t02) |
| DM-P4-T01 | [Hub outbox và merge sự kiện](#dm-p4-t01) |
| DM-P4-T02 | [reconnect nhiều trang và sửa xóa tin cũ](#dm-p4-t02) |
| DM-P5-T01 | [sửa tin của mình](#dm-p5-t01) |
| DM-P5-T02 | [xóa tin giữ tombstone](#dm-p5-t02) |
| DM-P5-T03 | [xung đột edit delete và reconnect tin cũ](#dm-p5-t03) |
| DM-P6-T01 | [ma trận quyền và giới hạn dữ liệu riêng](#dm-p6-t01) |
| DM-P6-T02 | [thu hồi phiên khóa tài khoản và deadline](#dm-p6-t02) |
| DM-P6-T03 | [mobile IME draft và cache theo actor](#dm-p6-t03) |
| DM-P7-T01 | [regression bản tích hợp và bàn giao](#dm-p7-t01) |
| DM-P8-T01 | [bảo vệ dữ liệu khi restore và retention](#dm-p8-t01) |
| DM-P8-T02 | [tải trình duyệt và observability](#dm-p8-t02) |
| DM-P8-T03 | [tích hợp phụ thuộc Identity và hồ sơ phát hành](#dm-p8-t03) |

<a id="dm-p0-t01"></a>

## DM P0 T01 môi trường dữ liệu và contract gói

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P0-T01: môi trường dữ liệu và contract gói.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task đầu không có predecessor DM; dùng Identity hiện có để setup và kiểm tra baseline.
Branch `chore/dm-p0-t01-acceptance-baseline`. Nguồn: contract-4–10, DM-SQL-01–06, hồ sơ MVP. Phụ thuộc: không có task DM trước; Identity hiện có và công cụ local.
Branch cụ thể: chore/dm-p0-t01-acceptance-baseline. Nhánh tích hợp/PR target: message. P0 hiện có branch từ main fe3c54a; message được tạo từ origin/main 2096e0b (thay đổi tài liệu), không tự rebase/merge vào P0 đang chờ test. Khi tích hợp P0 sau PASS phải đối chiếu và giữ các cập nhật docs trên message; nếu thay đổi hành vi/config/schema, chạy kiểm tra liên quan và bàn giao lại. Nếu bắt đầu P0 ở checkout mới, dùng message đã kiểm tra làm base; task đầu không cần predecessor DM.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. DM-P0-T01 là task đầu, không có predecessor DM; kiểm tra baseline Identity và môi trường hiện có.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P0-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P0-T01.1 Đọc source/nhánh cũ; lập ma trận reuse/replace/defer, chốt 7 REST operations và Hub schema cho gói; không coi endpoint cũ `/spaces` là contract mới.
- P0-T01.2 Chốt migration theo lát cắt, shared UoW/guard, key ring/HMAC tách biệt, sequence và mục chưa quyết định; không đổi policy sản phẩm.
- P0-T01.3 Dựng Compose test riêng/cổng/volume/config và proxy FE–API; kiểm tra PostgreSQL thật và startup.
- P0-T01.4 Tạo helper fixture A/B/C/U/K/S01–23, manifest ID, login nhiều actor và request corpus; không cài account sample vào production seed.
- P0-T01.5 Chạy baseline Identity, frontend tests/build; bàn giao smoke, cách start/stop không xóa volume và mẫu biên bản. Không làm search/send trong task này.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A/B/C login bằng username; đọc/sửa hồ sơ rồi reload; U login bị chặn. Logout A không đổi tài khoản B ở profile riêng. UI DM hiện còn mock phải được ghi đúng, không nghiệm thu mock là DM.
Backend: Health 200, GET `/users/me` đúng actor; không token bị 401; U không được cấp phiên ứng dụng. So khớp user ID từ API/manifest, DB riêng và cổng; chạy lại helper không tạo user trùng. Review contract tuyến/DTO/error cùng agent.
Điều kiện đạt: FE/BE baseline chạy thật, dataset tái lập và bạn duyệt thiết kế gói. Task này chỉ nghiệm thu nền/Identity; DM API vẫn chưa có. Biên bản ghi thiếu email thật và Development setup rõ ràng.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P0-T01-C01 — Login và lưu hồ sơ qua UI/API
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- DB test riêng chạy; A đã setup active/verified
- Browser profile A1 chưa login
Dữ liệu cụ thể: A=dm_demo_an; password DmDemo2026!Local; bio mới "Kiểm tra hồ sơ DM-P0-T01-C01"; giữ displayName An Demo.
Bước kiểm tra frontend:
1. Mở Web URL của bản test, nhập username/password A và bấm Đăng nhập.
2. Mở Cài đặt người dùng/Hồ sơ; điền bio mẫu, lưu rồi reload trang.
3. Mở lại Hồ sơ, kiểm tra bio và username A; ghi user ID từ response GET me trong Network.
Bước kiểm tra backend:
1. POST /api/v1/auth/login {"login":"dm_demo_an","password":"DmDemo2026!Local","deviceName":"DM-A1"}; giữ token trong bộ nhớ.
2. Bearer A: GET /api/v1/users/me; PATCH cùng route {"displayName":"An Demo","bio":"Kiểm tra hồ sơ DM-P0-T01-C01","locale":"vi-VN","timezone":"Asia/Ho_Chi_Minh"}; GET lại.
Frontend mong đợi: Login thành công, đúng An Demo; bio sau reload khớp toàn bộ mẫu, không chuyển sang user khác.
Backend mong đợi: Login 200; GET/PATCH/GET 200; id và username không đổi, bio mới được đọc lại từ API.
Đối soát DB chỉ đọc:
- Query user/profile theo A.id: đúng một user và bio hiện hành khớp mẫu.
- Không tạo user/phiên của B khi A sửa hồ sơ.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P0-T01-C02 — Pending U không được cấp phiên ứng dụng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- U còn pending/unverified
- Chưa có phiên ứng dụng của U trong manifest
Dữ liệu cụ thể: U=dm_demo_pending; password DmDemo2026!Local.
Bước kiểm tra frontend:
1. Mở profile browser riêng U, vào màn hình Đăng nhập.
2. Nhập username/password U đúng rồi bấm Đăng nhập.
3. Kiểm tra thông báo chưa xác minh và vẫn ở màn hình auth; không dùng token Development để tự kích hoạt U.
Bước kiểm tra backend:
1. POST /api/v1/auth/login {"login":"dm_demo_pending","password":"DmDemo2026!Local"}.
2. GET /api/v1/users/me không Authorization; thử cả Web proxy và API URL.
Frontend mong đợi: U không vào ứng dụng, không thấy dữ liệu mẫu như thể login thành công.
Backend mong đợi: Login U: 403 Identity.EmailNotVerified, không accessToken/refreshToken; GET me không token: 401 ProblemDetails.
Đối soát DB chỉ đọc:
- Đếm auth_sessions của U trước/sau: số phiên active không tăng.
- VerifiedAt của email U vẫn null; user chưa chuyển active.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P0-T01-C03 — Logout A không làm logout B và token A bị chặn
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1 và B1 là browser profile độc lập, đã login
- Ghi session ID A1/B1 và token A1 trước logout trong bộ nhớ
Dữ liệu cụ thể: A/B password mẫu; hai profile không chia sẻ localStorage.
Bước kiểm tra frontend:
1. Mở A1 và B1, kiểm tra tên account khác nhau.
2. Ở A1 bấm Đăng xuất; reload A1 phải về auth.
3. Ở B1 reload rồi mở Hồ sơ; B vẫn login và không xuất hiện dữ liệu A.
Bước kiểm tra backend:
1. Trước logout gọi GET /api/v1/users/me bằng A1 và B1, cả hai 200.
2. Sau logout A1, dùng token A1 cũ GET me; dùng token B1 GET me.
Frontend mong đợi: A1 về auth; B1 vẫn đọc hồ sơ B bình thường.
Backend mong đợi: Token A1 cũ 401; token B1 còn 200 với username dm_demo_bao; không refresh tự khôi phục A1.
Đối soát DB chỉ đọc:
- Session A1 có dấu revoke; session B1 không bị revoke vì logout A1.
- Không xóa user/profile/history khi logout.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P0-T01-C04 — Fixture chạy lại và restart không tạo user trùng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Helper setup P0 đã được agent cung cấp lệnh thật
- Run chứa A/B/C/U/K/S01-S23, manifest và volume riêng
Dữ liệu cụ thể: 28 user fixture; A/B/C/K/S verified, U pending; ghi IDs trước restart.
Bước kiểm tra frontend:
1. Login A, mở Hồ sơ để ghi A.id qua Network.
2. Chạy lại đúng lệnh setup cùng run được bàn giao; restart API/Web bằng lệnh không xóa volume.
3. Login lại A và B; kiểm tra username/displayName và IDs vẫn đúng.
Bước kiểm tra backend:
1. GET /api/v1/health trước/sau restart; GET me bằng phiên còn hợp lệ hoặc login mới khi cần.
2. Chạy helper setup cùng run hai lần; đối chiếu manifest IDs và DB connection thực tế.
Frontend mong đợi: Account cũ dùng được sau restart; không tự tạo user mới hoặc đổi password/account ngoài fixture.
Backend mong đợi: Health 200 khi sẵn sàng; manifest cùng run giữ IDs; helper không ghi đè user có username trùng mà không khớp fixture.
Đối soát DB chỉ đọc:
- Query normalized usernames trong run: 28 user fixture, mỗi username/email một record; U vẫn pending.
- DB thực tế scdc_dm_acceptance_test, không scdc_chat; không tăng records sau setup lặp.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P0-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P0-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P0-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P1-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P0-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p1-t01"></a>

## DM P1 T01 tìm người nhận

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P1-T01: tìm người nhận.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P0-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p1-t01-user-search`. AC-DM-01/16; TC-DM-01/18. Phụ thuộc P0-T01 được duyệt.
Branch cụ thể: feat/dm-p1-t01-user-search. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P1-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P1-T01.1 Bổ sung contract search do Identity thực hiện, projection không email/security state; kiểm tra actor/session và recipient active/verified.
- P1-T01.2 Query q 2–64 UTF-16, case-insensitive/giữ dấu, substring literal cho `%`, `_`, `\`; rank username exact rồi username/ID; cursor bind actor/q/limit và key ring bền.
- P1-T01.3 Thêm GET `/users/search`, DTO/ProblemDetails/OpenAPI; kiểm tra q sai, cursor sai và phiên không hợp lệ.
- P1-T01.4 Nối UI tìm người thật: loading/empty/error/retry, chống response muộn khi đổi q, hiển thị username với tên trùng; chọn nhiều người theo ID, giữ lựa chọn khi đổi q/tải thêm/lỗi, bỏ từng người hoặc tất cả; chỉ hiển thị selection, chưa bịa conversation ID.
- P1-T01.5 Integration/Unicode/pagination test và E2E tìm kiếm; bàn giao request/response đối chiếu.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A tìm `Bảo` thấy B/C phân biệt username; tìm `dm_demo_bao` ưu tiên B; `dm_demo_search` có trang tiếp; không tìm thấy A/U; search không có kết quả và lỗi API có thông báo. Ca K disabled có integration fixture ở task này và lượt người dùng E2E ở P6-T02 khi có helper khóa.
Backend: Cùng q qua Swagger/helper; thử 1/2/64/65 UTF-16, `%/_/\`, cursor đổi q hoặc actor; JSON không có email. DB đối chiếu kết quả/rank đủ người và không ghi membership khi search.
Điều kiện đạt: Search FE/BE đúng policy; chưa nghiệm thu tạo DM. Bạn PASS mới làm P1-T02.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P1-T01-C01 — Phân biệt hai người cùng tên hiển thị
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A đã login; B/C active và cùng displayName Bảo Demo
- Search API/UI thuộc task này đã chạy
Dữ liệu cụ thể: q="Bảo"; B username dm_demo_bao; C username dm_demo_chi.
Bước kiểm tra frontend:
1. Mở khu vực Tìm người từ DM, nhập Bảo.
2. Đợi response tìm kiếm; đọc từng kết quả, kiểm tra hai username khác nhau.
3. Chọn B rồi chọn thêm C: có hai người đã chọn theo ID/username. Đổi q thành dm_demo_bao vẫn giữ cả B/C; bấm B lần nữa bỏ B, bấm lại thêm B; nút Bỏ chọn @dm_demo_chi bỏ C, Bỏ chọn tất cả dọn hết. Không tạo conversation ở task search.
Bước kiểm tra backend:
1. Bearer A: GET /api/v1/users/search?q=B%E1%BA%A3o&limit=20.
2. GET /api/v1/users/search?q=dm_demo_bao&limit=20; so IDs với manifest B/C.
Frontend mong đợi: Tìm Bảo có B/C phân biệt bằng username; chọn đồng thời B/C, không trùng ID, giữ lựa chọn khi đổi q/tải thêm/lỗi API. Bỏ riêng hoặc bỏ tất cả hoạt động; đóng modal rồi mở lại không giữ lựa chọn. Query exact username ưu tiên B.
Backend mong đợi: 200; items của q Bảo chứa B/C đủ điều kiện; không chứa A/U/email/security state; query exact B xếp B trước.
Đối soát DB chỉ đọc:
- Đối chiếu items IDs với Identity; search không insert conversation/membership.
- Nếu fixture có account ngoài B/C cùng tên, so membership kết quả theo query, không khẳng định tổng items=2 ngoài run cô lập.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T01-C02 — Search đủ 23 kết quả qua hai trang
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- S01-S23 active/verified và là toàn bộ kết quả q trong run cô lập
- A không thuộc nhóm kết quả
Dữ liệu cụ thể: q="dm_demo_search"; limit=20.
Bước kiểm tra frontend:
1. Nhập dm_demo_search và mở trang kết quả đầu.
2. Bấm tải trang tiếp, ghi username/ID xuất hiện mới.
3. Quay lại/refesh search từ đầu; không có dòng lặp hay mất item vì response muộn.
Bước kiểm tra backend:
1. GET /api/v1/users/search?q=dm_demo_search&limit=20 bằng A; lưu nextCursor.
2. GET cùng q/limit, cursor URL-encode từ response trước; lấy hợp tập IDs.
Frontend mong đợi: Thấy đủ search01 đến search23, không trùng; loading/load more hết khi hết cursor.
Backend mong đợi: Trang 1: 20 items; trang 2: 3; union=23 ID duy nhất; trang cuối nextCursor=null.
Đối soát DB chỉ đọc:
- Đếm query Identity cùng filter: 23 records đúng trạng thái.
- Không dùng q chứa underscore như wildcard; escape LIKE để substring literal.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T01-C03 — Biên q và không bỏ dấu
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A đã login; fixture không có tên không dấu trùng riêng
- Có q Bảo và q Bao để so hai tập
Dữ liệu cụ thể: Queries: "x", "dm", "a" x64, "a" x65, "BẢO", "Bảo", "Bao". Bao vẫn tìm thấy B qua username dm_demo_bao không dấu; không được trả C chỉ vì bỏ dấu của displayName Bảo Demo.
Bước kiểm tra frontend:
1. Nhập x, kiểm tra hướng dẫn minimum không trả results giả.
2. Nhập BẢO rồi Bảo, so B/C; nhập Bao, không tự trả Bảo chỉ vì bỏ dấu.
3. Thử paste chuỗi 64/65 a; lỗi rõ và giữ query để sửa.
Bước kiểm tra backend:
1. GET users/search với các q nêu trên, limit20; dùng generator để tạo 64/65 ASCII a.
2. So response BẢO/Bảo theo manifest, q Bao phân biệt dấu; kiểm tra case-insensitive username.
Frontend mong đợi: UI phản ánh query invalid/empty/result thật, không hiện cache của query trước làm kết quả mới.
Backend mong đợi: q 1/65 UTF-16: 400 validation; q 2/64 hợp lệ: 200 kể cả items rỗng; BẢO và Bảo cùng tập trên fixture; Bao không tự khớp Bảo.
Đối soát DB chỉ đọc:
- Không ghi gì vào DB khi q invalid hoặc query không có kết quả.
- Record/displayName có dấu không bị normalize mất dấu trong source data.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T01-C04 — Cursor đổi query/actor và lỗi mạng không fallback mock
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Đã có nextCursor từ q dm_demo_search, actor A, limit20
- Browser fault runner có thể trả 503 riêng GET search
Dữ liệu cụ thể: Cursor trang 1 của A; B.token riêng; q không kết quả zzz_no_dm_person.
Bước kiểm tra frontend:
1. Tìm zzz_no_dm_person, kiểm tra empty state.
2. Bật fault GET search 503, tìm dm_demo_search; đọc lỗi và bấm thử lại sau khi tắt fault.
3. Gõ Bảo rồi lập tức dm_demo_bao với response Bảo bị delay; kết quả cuối phải thuộc q cuối, danh sách người đã chọn không bị response cũ ghi đè.
Bước kiểm tra backend:
1. GET users/search giữ cursor A nhưng đổi q=Bảo hoặc limit=10.
2. Dùng B.token với cursor A; sau đó gọi GET search không cursor để phục hồi.
Frontend mong đợi: Empty và error là hai trạng thái riêng; không hiện người mock; response query cũ không ghi đè.
Backend mong đợi: Cursor chéo query/limit/actor: 400 CURSOR_INVALID theo contract; request mới không cursor: 200; injected503 chỉ là fault test, không kết quả search thành công.
Đối soát DB chỉ đọc:
- Search/fault không tạo conversation/message; items phục hồi đúng query cuối.
- Không lộ nội dung cursor đã giải mã hay email trong ProblemDetails.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P1-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P1-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P1-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P1-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P1-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p1-t02"></a>

## DM P1 T02 tạo hoặc lấy một hội thoại duy nhất

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P1-T02: tạo hoặc lấy một hội thoại duy nhất.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

Yêu cầu bổ sung09/10/2026: tạm chỉ chọn một người; chọn người thứ hai thay người thứ nhất. Chọn nhiều người chưa hoạt động. Chỉ bấm Mở hội thoại mới POST; người vừa nhắn tin ở P1-T03/P2-T01.
Bản bàn giao hiện chạy tại worktree E:\Project\SCDC\dm-message-integration; dùng git rev-parse HEAD và metadata p1-t02-build.json đối chiếu. Giữ thay đổi checkout gốc scdc.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P1-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p1-t02-open-conversation`. AC-DM-01/12/13; TC-DM-02/12/24; DM-SQL-01.
Branch cụ thể: feat/dm-p1-t02-open-conversation. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P1-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P1-T02.1 Triển khai shared transaction/UoW và Identity guard actor/peer giữ khóa tới commit; khóa user theo cùng thứ tự UUID. Messaging gọi Contracts, không query bảng Identity trực tiếp.
- P1-T02.2 Model/mapping/migration space/cặp/member tối thiểu; UUID v7 server, low/high theo network bytes/DB; giữ unique pair và rollback toàn bộ khi lỗi.
- P1-T02.3 POST `/direct-conversations` create-or-get 200; reject self/peer không hợp lệ, không rò metadata; giải quyết unique conflict bằng đọc lại an toàn.
- P1-T02.4 UI chỉ chọn một người rồi bấm Mở hội thoại để mở conversation với hai người từ response, empty state thật; đồng thời click/loading/lỗi không tạo item mẫu.
- P1-T02.5 Test A→B/B→A đồng thời, UUID endian, lỗi giữa transaction, guard cạnh revoke; recipe parallel request và SQL đếm pair/member/space.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A chọn B, B chọn A, reload/mở lại cùng D-AB; tên đúng và history rỗng. A chọn C ra D-AC khác. Không có thao tác tự nhắn.
Backend: Hai POST song song nhận cùng ID; đúng hai membership, một pair, không space mồ côi; self/peer pending bị từ chối. Disabled peer được test bằng integration fixture, lượt người dùng E2E ở P6-T02. C không được lấy D-AB từ inbox sau task kế. Task này test outsider với các API đã có, không giả endpoint detail chưa có.
Điều kiện đạt: Tạo/lấy đúng qua UI/API/DB, atomic/race đạt. Bạn PASS mới làm inbox.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P1-T02-C01 — A mở B và B mở A nhận cùng DM
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Chụp Snapshot A/B; build bàn giao đã có D-AB từ agent nên lane này nghiệm thu get/reopen; tạo mới bổ sung A/C nếu Snapshot0.
- A/B active verified; chỉ chọn một người; chưa có send/history/inbox loader.
Dữ liệu cụ thể: A.id=01a114bc-1e4d-74c7-87eb-8db1734f8aea; B.id=01a114bc-2678-7d79-b136-4d944d6a85fc; D-AB=01a11c8f-534c-761c-8d4a-4930c95bee58; C.id=01a114bc-2b41-7c64-8622-aa1694b2de9b.
Bước kiểm tra frontend:
1. A login dm_demo_an tại FE15300 bằng DmDemo2026!Local → Direct Messages → dấu+ → tìm Bảo; chọn B rồi C, chỉ một thẻ @dm_demo_chi còn, chưa có POST.
2. Chọn lại B/@dm_demo_bao → bấm Mở hội thoại → Network POST200 cùng D-AB; tiêu đề Bảo Demo, @dm_demo_bao và Chưa có tin nhắn.
3. B login ở profile độc lập → tìm dm_demo_an → chọn An → Mở hội thoại; POST cùng ID. Reload A cần tìm/mở B lại (inbox loader chưa có), cùng ID; mở thêm một lần không duplicate.
4. Chụp Snapshot A/C trước; A chọn C/@dm_demo_chi rồi Mở hội thoại; ID khác D-AB, đúng peer username. BE replay cùng cặp, ghi delta mới chỉ khi baseline0.
Bước kiểm tra backend:
1. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias B
2. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias B
3. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias B -PeerAlias A
4. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias C # sau FE tạo/reopen C, không thêm lane
Frontend mong đợi: Hai bên mở đúng peer, cùng D-AB; không có tin mẫu để giả lịch sử.
Backend mong đợi: Mọi POST 200; cùng id, participants đúng A/B; không user C/email/security state.
Đối soát DB chỉ đọc:
- Unique pair A/B có một record direct_conversations và một space.
- Hai membership đúng A/B, không membership thứ ba hoặc space mồ côi.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; người dùng xác nhận trên build bàn giao. Thiếu bằng chứng ghi Bị chặn; sai bước ghi FAIL, không tự đi task kế.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T02-C02 — Mở đồng thời không tạo pair trùng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Dùng pair A/S10 chưa có DM; Snapshot trước phải0, runner chạy trước UI. Nếu đã có, đổi đồng nhất S12 hoặc peer chưa dùng; RequireNewPair sẽ từ chối.
- Runner helper giữ token RAM, barrier40 request; không chạy đồng thời fault cho cùng pair.
Dữ liệu cụ thể: 20 POST A→S10 và20 POST S10→A; dm_demo_an / dm_demo_search10; password DmDemo2026!Local. Agent trước đó đã chạy A/S02, không lấy replay S02 làm proof create mới.
Bước kiểm tra frontend:
1. Chạy runner trước; ghi conversationId duy nhất và before/after counts.
2. A tìm dm_demo_search10 → chọn → Mở hội thoại; S10 profile riêng tìm dm_demo_an → Mở hội thoại; cả hai POST200 cùng ID runner.
3. A mở lại S10 hai lần, sidebar chỉ một item cùng ID; double click đang loading không gửi request thứ hai.
Bước kiểm tra backend:
1. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S10
2. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Concurrent -PeerAlias S10 -RequireNewPair
3. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias S10
4. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias S10 -PeerAlias A
5. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S10
Frontend mong đợi: Không có conversation trùng cho cùng pair; UI không duplicate sau load/open lại.
Backend mong đợi: 40 request hợp lệ đều resolve một ID duy nhất; create unique conflict được xử lý thành get, không trả space thiếu dữ liệu.
Đối soát DB chỉ đọc:
- Pair/space đúng một; membership đúng hai; không orphan do request thua unique constraint.
- Đối chiếu low/high UUID byte-order với DB và fixture endian.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; người dùng xác nhận trên build bàn giao. Thiếu bằng chứng ghi Bị chặn; sai bước ghi FAIL, không tự đi task kế.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T02-C03 — Self và peer pending bị từ chối
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A hợp lệ; U.id known nhưng pending
- Đếm spaces/pairs trước từng request
Dữ liệu cụ thể: peerUserId=A.id và U.id; không mint token U để bypass auth.
Bước kiểm tra frontend:
1. A search username của mình, UI không cho mở self.
2. Search dm_demo_pending, U không xuất hiện.
3. Thử recipe gọi REST self/pending được agent cung cấp, quay lại UI kiểm tra không có conversation giả mới.
Bước kiểm tra backend:
1. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias A -ExpectedStatus 400
2. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -ActorAlias A -PeerAlias U -ExpectedStatus 404
3. POST peerUserId="00000000-0000-0000-0000-000000000000", "not-a-uuid" và body{} với Bearer A:400 Common.ValidationFailed; valid UUID ngẫu nhiên404 RESOURCE_NOT_FOUND.
4. Login phiên REST riêng A; logout đúng refreshToken; dùng chính accessToken cũ POST B.id:401 Common.Unauthorized. Nguyên PowerShell có trong biên bản DM-P1-T02.md; không login mới thay token bị revoke.
Frontend mong đợi: Self/U không được mở từ search, lỗi REST không thêm item giả.
Backend mong đợi: Self400 INVALID_PEER; U/unknown404 RESOURCE_NOT_FOUND; missing/null/bad/zero UUID400 Common.ValidationFailed; anonymous/revoked401 Common.Unauthorized. Không create pair/space/member.
Đối soát DB chỉ đọc:
- Spaces/pairs/member counts không tăng ở hai request bị từ chối.
- User U vẫn pending và không có session được cấp.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; người dùng xác nhận trên build bàn giao. Thiếu bằng chứng ghi Bị chặn; sai bước ghi FAIL, không tự đi task kế.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T02-C04 — Lỗi giữa transaction rollback nguyên tử
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Pair A/S11 mới; Snapshot phải0, FaultOn -RequireNewPair refuse pair cũ; đổi sang alias chưa dùng nếu cần.
- Test trigger chỉ DB scdc_dm_acceptance_test, đúng pair A/S11, BEFORE INSERT pair sau SaveChanges space; không developer API production.
Dữ liệu cụ thể: A/S11: dm_demo_an, dm_demo_search11; peer ID resolve manifest bởi helper; chụp actorSpaceCount, pair/member/message/orphan trước ca.
Bước kiểm tra frontend:
1. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action FaultOn -PeerAlias S11 -RequireNewPair; A tìm dm_demo_search11 → chọn → Mở hội thoại.
2. Network503 AUTHORITY_UNAVAILABLE; alert Không mở được hội thoại. Hãy thử lại.; giữ một thẻ @dm_demo_search11, không item/ID giả. BE cũng thất bại cùng pair; Snapshot không tăng.
3. FaultOff trong finally; bấm lại Mở hội thoại trên modal cũ:200, Người tìm11/@dm_demo_search11, một item. BE replay ID/pair đã FE tạo, không tăng thêm.
Bước kiểm tra backend:
1. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11
2. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -PeerAlias S11 -ExpectedStatus 503 # trong fault
3. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11 # pair0/member0, actorSpace không tăng
4. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action FaultOff # luôn chạy trong finally
5. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Open -PeerAlias S11 # sau FE retry, cùng ID
6. powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dm-acceptance/open-conversation.ps1 -Action Snapshot -PeerAlias S11 # pair1/member2/message0/orphan0
Frontend mong đợi: Request lỗi giữ selection/error; mở lại thành công với ID trả từ server.
Backend mong đợi: Fault503 AUTHORITY_UNAVAILABLE, rollback toàn space/pair/member, không trả success. Sau FaultOff, FE retry tạo200; BE replay200 cùng ID và không thêm hàng.
Đối soát DB chỉ đọc:
- Trong fault: pair0/member0/message0, orphan0, actorSpaceCount bằng trước.
- Sau FE retry và BE replay: pair1/member2/message0/orphan0, actorSpace+1; người FE/BE cùng ID.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; người dùng xác nhận trên build bàn giao. Thiếu bằng chứng ghi Bị chặn; sai bước ghi FAIL, không tự đi task kế.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P1-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P1-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P1-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P1-T03, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P1-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p1-t03"></a>

## DM P1 T03 inbox dữ liệu thật

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P1-T03: inbox dữ liệu thật.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P1-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p1-t03-conversation-inbox`. AC-DM-01/12, contract danh sách; cần D-AB/D-AC.
Branch cụ thể: feat/dm-p1-t03-conversation-inbox. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P1-T03.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P1-T03.1 GET `/direct-conversations` chỉ actor member; projection participant, activity và pagination 20/50/cursor protected.
- P1-T03.2 Kiểm tra auth từng trang, dedup ID trên UI và refresh trang đầu khi danh sách thay đổi; không hứa snapshot cố định.
- P1-T03.3 Thay DM mock bằng loader/inbox thật; loading/empty/error/retry và chọn conversation sau reload. Modal chọn người có mục Người vừa nhắn tin: lấy peer từ hội thoại của actor có lastActivityAt khác null, theo lastActivityAt DESC rồi conversation ID ASC, dedup peer ID, bỏ actor; hiện displayName/username, cho chọn/bỏ chọn chung với kết quả search. Hội thoại chưa có tin không vào mục này; API lỗi có retry, không fallback mock/localStorage lượt chọn.
- P1-T03.4 Cache list theo actor và cleanup logout; response cũ không ghi dữ liệu vào actor mới.
- P1-T03.5 Seed thêm DM A–S để test >20 hội thoại qua API; E2E empty/list/pagination/actor switch.

Điều chỉnh09/10/2026: hiện chỉ chọn một người; chọn người khác thay lựa chọn trước. Giữ override này khi dùng chung lựa chọn search/gần đây.

Yêu cầu bổ sung ngày 08/10/2026: “người bạn gần nhất” là **người vừa nhắn tin**, không phải người vừa chọn. P1-T03 dựng UI/loader theo `lastActivityAt`; khi chưa có writer thì test empty state và phân quyền, ghi rõ thứ tự có tin còn chờ P2-T01. Không seed tin trực tiếp vào DB để tuyên bố nghiệm thu. P2-T01 chạy lại mục gần đây bằng writer thật và bàn giao người dùng test; chưa chạy task đó trong lần sửa P1-T01.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A thấy D-AB/D-AC và đủ các trang; B chỉ thấy DM của B; C không thấy D-AB; tài khoản mới inbox rỗng, API lỗi không fallback mock.
Backend: Compare list với membership, kiểm tra limit/cursor dùng chéo actor; U/no token không đọc list. Không dùng lastActivity làm cursor lịch sử tin.
Điều kiện đạt: Inbox thật đúng quyền; bạn PASS rồi mới gửi tin.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P1-T03-C01 — Inbox phân biệt A B C theo membership
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Baseline A có 25 DM A/B, A/C, A/S01-S23; B/C chỉ có DM với A; K chưa có DM
- A/B/C profile riêng
Dữ liệu cụ thể: Baseline 25 DM chưa có messages; D-AB A/B và D-AC A/C giữ ID đã tạo.
Bước kiểm tra frontend:
1. Login A mở inbox, tải thêm từ 20 lên 25; ghi B/C cùng các peer S và ID.
2. Login B mở inbox, chỉ có peer A từ D-AB.
3. Login C mở inbox, chỉ có D-AC; không thấy D-AB biết ID.
Bước kiểm tra backend:
1. GET /api/v1/direct-conversations?limit=20 lần lượt bằng A/B/C.
2. So items.id và participants với manifest/membership query.
Frontend mong đợi: A thấy 25 DM sau tải thêm; B/C mỗi bên một DM với A đúng của mình; không có INITIAL_DMS.
Backend mong đợi: 200; A có 25 DM, B {D-AB}, C {D-AC}; lastSequence="0", lastActivityAt=null, không lastMessage giả.
Đối soát DB chỉ đọc:
- Membership join actor cho đúng tập IDs ở từng response.
- GET list không tạo membership hoặc cấp C quyền D-AB.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T03-C02 — Inbox trên 20 hội thoại tải đủ và không trùng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A có D-AB/D-AC và 23 DM A/S01-S23, không D-AK trong run case
- Tất cả tạo qua POST writer
Dữ liệu cụ thể: Run cô lập chỉ tạo 25 DM của A: A/B, A/C và A/S01..S23; không tạo A/K trong ca này. limit=20; ghi thứ tự activity/id trước tải.
Bước kiểm tra frontend:
1. Mở inbox A, ghi 20 items trang đầu.
2. Bấm tải thêm, ghi 5 items còn lại.
3. Reload/làm mới trả trang đầu20; bấm Tải thêm hội thoại để đủ25 ID duy nhất. Mở peer giữ selection cho tới lần reload mới.
Bước kiểm tra backend:
1. GET direct-conversations?limit=20; lưu nextCursor.
2. GET trang kế cùng limit/cursor; union IDs rồi so memberships A.
Frontend mong đợi: Đủ 25 conversation; không duplicate do render list hoặc refresh.
Backend mong đợi: Trang 1=20/trang2=5, nextCursor cuối null; union=25. Nếu activity thay đổi trong ca, refresh/dedup theo contract, không hứa snapshot cố định.
Đối soát DB chỉ đọc:
- Query active memberships A:25; không bỏ DM empty chỉ vì lastActivity null.
- Số DM không tăng vì GET pagination.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T03-C03 — User mới inbox rỗng và API lỗi có retry
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- K active/verified và chưa có DM trong baseline; không mở DM bằng K ở case này
- Có fault GET list 503 và tài khoản A có DM
Dữ liệu cụ thể: K=dm_demo_khoa empty case; A=dm_demo_an nonempty recovery case.
Bước kiểm tra frontend:
1. Login K, mở inbox, thấy hướng dẫn tìm người và không item mẫu; modal Người vừa nhắn tin rỗng.
2. Login A profile khác, bật fault GET list503 rồi mở inbox.
3. Tắt fault, bấm thử lại, danh sách thật của A trở lại.
Bước kiểm tra backend:
1. GET direct-conversations bằng K nhận empty; lưu response.
2. Bearer A GET list trong fault rồi request sạch sau fault.
Frontend mong đợi: Empty khác error; lỗi có retry; không dùng INITIAL_DMS để lấp response empty/error.
Backend mong đợi: K:200 items=[] nextCursor=null; fault API thật:503 AUTHORITY_UNAVAILABLE; phục hồi:200 đúng actor.
Đối soát DB chỉ đọc:
- GET không tạo DM/membership K khi inbox rỗng.
- Fault không làm mất pairs/memberships/messages của A.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P1-T03-C04 — Actor switch và cursor không rò inbox
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A có nextCursor và response list A bị delay
- C session riêng, chưa được giao D-AB
Dữ liệu cụ thể: Cursor A + C.token, request A trễ sau logout.
Bước kiểm tra frontend:
1. Bật delay GET list A rồi mở inbox A.
2. Logout A/login C trong cùng tab trước response A trả về.
3. Đợi response cũ rồi reload; inbox chỉ thuộc C, không lóe D-AB hoặc giữ selection A.
Bước kiểm tra backend:
1. Bearer C GET direct-conversations với cursor của A.
2. GET mới không cursor bằng C; so participants và IDs; token A cũ GET list sau logout.
Frontend mong đợi: Response A cũ không ghi vào C; list/selection A bị cleanup.
Backend mong đợi: Cursor chéo actor:400 CURSOR_INVALID; C GET sạch200 đúng tập; token A đã revoke401.
Đối soát DB chỉ đọc:
- Membership C không tăng vì actor switch/cursor error.
- Không có server action tạo conversation từ stale response FE.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P1-T03-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P1-T03.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P1-T03; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P2-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P1-T03, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p2-t01"></a>

## DM P2 T01 gửi văn bản và validation

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P2-T01: gửi văn bản và validation.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P1-T03. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p2-t01-persist-text-message`. AC-DM-02/09/11/19/21; TC-TEXT-01–05, TC-DM-03/14/21/23; DM-SQL-02–06.
Branch cụ thể: feat/dm-p2-t01-persist-text-message. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

Bản triển khai09/10/2026 và recipe test thật: docs/acceptance/direct-messaging/DM-P2-T01.md. P2-T01 đã được người dùng nghiệm thu ngày10/10/2026 và merge message tại ea941a7. Chọn người theo override09/10/2026: chỉ một người.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P2-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P2-T01.1 Migration counter/conversation_sequence, SendOperation/fingerprint, tombstone-compatible constraint và outbox ID/version; không ghi `message_edits.previous_content`.
- P2-T01.2 Validator client/server theo fixture Unicode; HMAC key/version tách key ring; same-key retry trả trạng thái hiện hành, payload khác conflict ngay từ writer đầu tiên.
- P2-T01.3 Guard actor/member/peer, thứ tự khóa thống nhất; counter + message + operation + outbox/projection cùng transaction, rollback không để trạng thái dở.
- P2-T01.4 POST `/direct-conversations/{id}/messages`; API wrapper `retry:false`; UI sending/sent/error, tạm và response merge cùng ID; chỉ text, không API Hub mutation.
- P2-T01.5 Backend/FE/E2E send và biên content; bàn giao SQL đọc message/operation/outbox và hiện trạng chưa có realtime/history đầy đủ. Kiểm thử Người vừa nhắn tin trên run riêng D-AB/D-AC ban đầu rỗng: commit thật A→B, A→C, B→A; mục gần đây của A lần lượt chỉ B, C→B, B→C. Refresh inbox/modal sau mỗi commit, đối chiếu API và DB; gửi lỗi trước commit không đổi thứ tự.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

F2. Regression bổ sung DM-RECENT-01 theo yêu cầu08/10/2026 — Chưa chạy
- Dùng run/lane riêng: D-AB và D-AC mới, cùng28 tài khoản mẫu nhưng0 tin của hai hội thoại này; không xóa tin của run trước. Agent cung cấp lệnh tạo/lấy pair, IDs/counters/counts baseline và collection/SQL thật trước bàn giao.
- Mở modal của A khi chưa có tin: Người vừa nhắn tin rỗng; mở hội thoại mà chưa gửi không được thêm người vào mục này.
- A gửi "RECENT01: chào Bảo" vào D-AB với UUIDv4 mới, đợi200/commit rồi refresh inbox/modal A: chỉ B (@dm_demo_bao), chưa có C. GET list A có D-AB.lastActivityAt khác null; D-AC vẫn null.
- A gửi "RECENT02: chào Chi" vào D-AC bằng UUIDv4 khác; đợi200 rồi refresh: C (@dm_demo_chi) trước B. Hai người cùng displayName nhưng khác ID/username, không gộp theo tên.
- B gửi "RECENT03: trả lời An" vào D-AB bằng UUIDv4 thứ ba; đợi200 rồi refresh A: B trước C. B/C mở modal riêng: mỗi người chỉ thấy A từ hội thoại của mình; C không thấy peer B qua D-AB.
- Chọn B từ mục gần đây, tìm dm_demo_bao: đã chọn đúng1 B, không thêm bản sao. Chọn C thì thay B, chỉ có1 thẻ theo yêu cầu09/10/2026; bỏ C ở kết quả search cũng bỏ C trong selection. Không tạo group DM.
- Query API và DB đối soát đúng membership, lastActivityAt/order và ID; delta tin của run=3, D-AB=2, D-AC=1. Gửi lỗi có chủ ý trước commit không thêm tin hoặc đổi lastActivityAt/thứ tự; mỗi request mới dùng UUIDv4 mới, recipe fault có bật/tắt và scope run.
- Lỗi tải danh sách có retry, không hiện người mock/lượt chọn localStorage. Reload giữ lịch sử gần đây từ server; logout/đổi actor dọn cache/selection. Refresh chủ động; không yêu cầu realtime chưa tới phase.
- Lưu build, FE/BE actual, IDs/timestamps/counts không token. Chỉ PASS khi người dùng test; proof P1-T03 chỉ empty/permission chưa thay proof thứ tự bằng tin commit thật ở đây.

G. Các ca tôi tự kiểm tra
Frontend: A gửi M01/M02/M03/M04 thấy sent sau response; L2000/E2000 nhận, L2001/E2002/EMPTY bị từ chối; HTML không chạy. DB lỗi/fault trước commit hiện lỗi, không báo sent.
Backend: POST cùng corpus; response sequence/version là chuỗi, content CRLF chuẩn hóa đúng; DB một message/operation/outbox, không payload body trong outbox/log. C/no token bị chặn; cùng O1 khác body không tạo tin mới. B xem bằng GET history ở task kế; task này đối chiếu response/DB trước.
Điều kiện đạt: Writer lưu bền/validation/guard/idempotency nền đạt. Không trì hoãn chống trùng tới P3; P3 mở rộng fault/UI retry và proof.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P2-T01-C01 — Gửi M01 chỉ báo sent sau commit
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-AB hợp lệ, A login; thao tác UI sinh UUIDv4 O_UI mới chưa từng dùng trong pair/run.
- Chụp counts message/operation/outbox và counter N; backend trong ca này chỉ replay thao tác UI.
Dữ liệu cụ thể: content="Chào Bảo, đây là tin thử M01."; O_UI lấy chính xác từ body POST do UI gửi, không thay bằng O1.
Bước kiểm tra frontend:
1. A mở D-AB, nhập đúng M01 rồi bấm Gửi một lần.
2. Quan sát Đang gửi trong thời gian response bị delay; chưa hiện Đã gửi trước commit/ack.
3. Sau response, ghi một dòng sent, ID/version/sequence từ Network; không yêu cầu B realtime/history ở task này.
Bước kiểm tra backend:
1. Bearer A POST /api/v1/direct-conversations/<D-AB.id>/messages với đúng body đã bắt từ Network: {"clientMessageId":"<O_UI>","content":"Chào Bảo, đây là tin thử M01."}. Đây là replay, không phải gửi mới.
2. Gọi lại đúng body O_UI lần nữa; đối soát writer records theo actor+O_UI và ID của UI.
Frontend mong đợi: Chỉ một dòng M01, sent sau response thành công; không sinh message giả khác ID.
Backend mong đợi: 200 Message với conversationId D-AB, author.id A, clientMessageId O_UI, version='1', sequence chuỗi số N+1, content đúng M01; mọi replay trả cùng ID.
Đối soát DB chỉ đọc:
- Delta message=1, send_operation=1, create outbox=1, counter=N+1.
- Outbox payload chỉ ID/version/context, không body M01; không row message_edits chứa body cũ.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T01-C02 — Biên UTF16 và chuẩn hóa CRLF giữ khoảng trắng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- O1 case trước đã tách; mỗi input dùng UUIDv4 mới
- Agent bàn giao file input để paste và generator UTF16
Dữ liệu cụ thể: L2000 a×2000; L2001 a×2001; E2000 😀×1000; E2002 😀×1001; M02 CRLF; M03 spaces.
Bước kiểm tra frontend:
1. Paste từng L/E vào composer, ghi counter; bấm Gửi nếu UI cho phép.
2. Paste M02 từ fixture rồi gửi; hiển thị hai dòng và 👩‍💻 đúng.
3. Gửi M03 giữ hai spaces mỗi đầu; inspect response content để xác nhận không trim, không chỉ dựa mắt.
Bước kiểm tra backend:
1. POST messages riêng mỗi input, kể cả input UI đã ngăn gửi, bằng helper JSON có UUID mới.
2. Compare response.content (không có trường normalizedContent), JavaScript string.length và .NET UTF-16 length của fixture; M02 mong đợi "Dòng 1\nDòng 2 👩‍💻".
Frontend mong đợi: L2000/E2000 gửi được; L2001/E2002 báo vượt giới hạn và giữ draft để sửa; M02/M03 nguyên nội dung sau LF normalization.
Backend mong đợi: Valid:200; invalid length:400 CONTENT_TOO_LONG; M02 CRLF→LF, M03 không trim; invalid không được cắt body rồi lưu.
Đối soát DB chỉ đọc:
- Số rows tăng chỉ bằng số input hợp lệ thực sự commit.
- Không message/operation/outbox cho request vượt limit; DB body M02/M03 khớp normalized value.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T01-C03 — Empty invisible invalid và HTML text an toàn
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Mỗi raw request có ID mới; helper hỗ trợ raw JSON không tự sanitize surrogate
- Có fault/browser input file cho invisible
Dữ liệu cụ thể: Inputs: "", " \t\n ", "\u200b", NUL, unpaired surrogate; M04="<script>alert('DM test')</script>".
Bước kiểm tra frontend:
1. Thử Gửi empty/whitespace/invisible và ghi lỗi rõ, không dòng sent.
2. Gửi M04 như text, xem nội dung nguyên chữ và không có alert/dialog/script chạy.
3. Reload chưa thuộc history ở task này: đối soát bằng response/DB, không giả tin lưu được từ UI local state.
Bước kiểm tra backend:
1. POST empty/white/zero-width/NUL qua helper; raw JSON body surrogate sai bằng file fixture.
2. POST M04 UUID mới; kiểm tra Content-Type JSON/ProblemDetails và lỗi parser không chứa body request.
Frontend mong đợi: Không gửi được empty/invisible; HTML hiện literal text, không chạy code.
Backend mong đợi: Empty/invisible400 CONTENT_EMPTY; NUL400 CONTENT_INVALID; malformed surrogate400 CONTENT_INVALID hoặc parser validation đã chốt, không500; M04 valid200.
Đối soát DB chỉ đọc:
- Rows chỉ tăng M04, không invalid input; outbox/log không ghi body lỗi.
- Không sửa nội dung HTML thành markup hoặc replace surrogate thành U+FFFD để lưu.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T01-C04 — Outsider và fault trước commit không tạo tin
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- C không member D-AB; chuẩn bị UUID mới cho từng thử
- Fault writer rollback deterministic trong test, counter/messages snapshot
Dữ liệu cụ thể: C attempts content=OUTSIDER-P2; A content=ROLLBACK-P2; fault trả503 theo harness test.
Bước kiểm tra frontend:
1. C mở UI DM list không có D-AB; thử recipe direct request do agent bàn giao.
2. Ở A bật fault trước commit, nhập ROLLBACK-P2 rồi Gửi.
3. Tắt fault nhưng chưa bấm Retry trong task này; quan sát không báo sent hoặc tự gửi lại.
Bước kiểm tra backend:
1. Bearer C POST D-AB messages body UUID/content OUTSIDER-P2.
2. Bearer A POST body ROLLBACK-P2 trong fault; query DB sau transaction kết thúc; no-token POST cũng thử.
Frontend mong đợi: C không thấy tin/private detail; A giữ error/failed, không sent.
Backend mong đợi: C bị deny theo status/errorCode cố định chốt P0, không metadata/body; no token401; rollback fault503 không success.
Đối soát DB chỉ đọc:
- Delta rows/operation/outbox=0 cho C, no-token và rollback A; counter không tăng do rollback.
- Giữ nguyên message đã có trước ca, không dùng lỗi để xóa history.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P2-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P2-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P2-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P2-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P2-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p2-t02"></a>

## DM P2 T02 lịch sử phân trang và tin khi vắng mặt

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P2-T02: lịch sử phân trang và tin khi vắng mặt.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P2-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p2-t02-message-history`. AC-DM-03/07/12/18/21; TC-DM-03/04/15/20/25.
Bản triển khai và recipe test thật ngày10/10/2026: docs/acceptance/direct-messaging/DM-P2-T02.md. Bằng chứng agent tách khỏi gate người dùng; chưa merge trước khi tôi PASS FE/BE.
Branch cụ thể: feat/dm-p2-t02-message-history. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P2-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P2-T02.1 GET messages latest/before/after bootstrap theo counter committed; default 50/max100, items tăng sequence, protected cursor và mốc through đúng contract.
- P2-T02.2 Key ring bền; cursor gắn actor/conversation/direction/filter/limit/expiry; reject before+after, cursor bị sửa/dùng chéo; auth từng trang.
- P2-T02.3 UI load latest/load older/scroll ổn định, loading/error/retry không bỏ tin đang thấy; không dùng sequence difference làm unread.
- P2-T02.4 Tạo H121 qua writer, B offline rồi login; bind message ID thật vào manifest.
- P2-T02.5 Test lịch sử không thiếu/trùng, rollback/late commit, bigint lớn và restart key ring; UI/API/DB cùng tập ID.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: B mở lại D-AB thấy M01 dù offline lúc gửi; A mở D-HIST thấy H121 đủ ba trang, scroll không nhảy; reload API không mất history; lỗi tải thêm vẫn giữ trang đang thấy.
Backend: latest/before trả đúng 50/50/21 trên dataset riêng; cursor chéo user/DM/bị sửa bị từ chối; C không đọc D-AB; sequence sort số đúng. Tắt/bật API, DB còn tin và cursor chưa hết hạn còn dùng được.
Điều kiện đạt: Gửi–đọc–reload chạy với DB thật. Bạn PASS rồi mở fault/retry.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P2-T02-C01 — Latest và before trả 50 50 21 tin đúng thứ tự
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-HIST riêng chỉ có H121, A là member
- HIST-001...121 được gửi qua writer, ghi IDs/sequences
Dữ liệu cụ thể: limit50; latest HIST-072...121, before1 HIST-022...071, before2 HIST-001...021.
Bước kiểm tra frontend:
1. A mở D-HIST, ghi 50 dòng mới nhất.
2. Bấm Tải tin cũ hơn hai lần, mỗi lần ghi IDs hiện thêm và vị trí scroll.
3. Đến đầu lịch sử thấy HIST-001; không còn tải thêm, tổng121 dòng không trùng.
Bước kiểm tra backend:
1. GET /api/v1/direct-conversations/<D-HIST.id>/messages?limit=50; lưu nextCursor.
2. GET same route before=<cursor trang trước URL-encode>&limit=50 hai lần; đối chiếu union IDs/sequence.
Frontend mong đợi: Scroll ổn định; giữ đủ121 tin; newest/older nhóm đúng label theo fixture.
Backend mong đợi: 200 từng trang; items luôn tăng sequence trong trang; counts50/50/21; hasMore true/true/false, nextCursor cuối null.
Đối soát DB chỉ đọc:
- Query count space=D-HIST:121, unique ID/sequence=121.
- Union API IDs bằng toàn tập DB; không bỏ tin vì dùng sequence identity toàn cục.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T02-C02 — Tin gửi khi B vắng mặt vẫn thấy sau reload và restart
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-AB có baseline N tin; B đã logout/đóng ứng dụng
- A dùng UUID mới và content OFFLINE-HISTORY-C02
Dữ liệu cụ thể: Một tin mới khi B không subscribe; key ring/DB volume riêng còn nguyên.
Bước kiểm tra frontend:
1. Đóng B, A gửi OFFLINE-HISTORY-C02 và chờ sent.
2. B mở/login lại, vào D-AB, thấy content dù không nhận realtime lúc vắng.
3. Reload B rồi restart API không xóa volume, mở lại history; tin vẫn có cùng ID.
Bước kiểm tra backend:
1. POST messages bằng A UUID mới, ghi message.id/sequence/version.
2. B GET messages trước/sau restart; dùng protected cursor còn hạn đã lưu để kiểm tra key ring.
Frontend mong đợi: Tin offline hiện từ API thật, không từ sự kiện đã mất; reload/restart không tạo bản khác.
Backend mong đợi: Send200, history200; cùng message ID/content/version; cursor chưa expired vẫn dùng được sau restart.
Đối soát DB chỉ đọc:
- Delta message/operation/create event=1 cho send; reload/restart không tăng.
- Key ring tồn tại qua restart và DB rows không bị recreate từ seed.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T02-C03 — Cursor chéo DM actor và bị sửa bị chặn
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A có before/resume cursor D-AB
- D-AC tồn tại; C không member D-AB
Dữ liệu cụ thể: Original cursor; tampered cursor đổi một ký tự giữa string; actor C.
Bước kiểm tra frontend:
1. A mở history D-AB và dùng load older bình thường.
2. Bật fixture response cursor sai trong test browser, bấm load older; thấy lỗi và giữ tin đang xem.
3. Chọn tải lại sạch; UI không tự gửi tin hoặc nhảy sang D-AC.
Bước kiểm tra backend:
1. A GET D-AC history với cursor D-AB; A GET D-AB với tampered cursor hoặc before+after cùng request.
2. C GET D-AB bằng token C và cursor A; A GET sạch không cursor để hồi phục.
Frontend mong đợi: Error tải thêm không xóa timeline đã có; recovery tải đúng history actor/resource.
Backend mong đợi: Cursor sai scope/tampered/direction400 CURSOR_INVALID; before+after400 validation; C resource deny theo contract P0 không body/participants; GET sạch200.
Đối soát DB chỉ đọc:
- Không insert/update message/operation do cursor rejection.
- Không decrypt cursor vào error/log hoặc trả resource details cho C.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P2-T02-C04 — Lỗi tải thêm và biên sequence lớn không mất tin
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-HIST H121; runner fault chỉ request before tiếp theo503
- Fixture biệt lập hỗ trợ sequence lớn hơn 9007199254740991 mà DB giữ đúng string
Dữ liệu cụ thể: Technical sequence fixture: 9007199254740992, 9007199254740993; không sửa counter app bằng SQL tay.
Bước kiểm tra frontend:
1. A mở 50 tin latest; bật fault trước lần Load older.
2. Bấm tải thêm, ghi lỗi, các dòng đang thấy và scroll không bị xóa.
3. Tắt fault rồi Retry; mở fixture bigint riêng, ghi thứ tự hai tin, không merge mất một vì Number rounding.
Bước kiểm tra backend:
1. GET history before trong fault503 rồi request lại sạch.
2. Runner DB test tạo big-sequence fixture qua test setup đã bàn giao; GET history và so literal sequence strings.
Frontend mong đợi: Error chỉ ở phần tải thêm; Retry giữ đủ data; hai sequence lớn có thứ tự khác nhau và hai dòng.
Backend mong đợi: Fault503, recovery200; sequence/version là decimal string, FE dùng BigInt hoặc comparator chính xác; không rounding Number.
Đối soát DB chỉ đọc:
- H121 vẫn121; GET không thay rows.
- Fixture bigint giữ unique sequence và counter hợp lệ; đối chiếu bằng bigint SQL, không coi hai giá trị là một.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P2-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P2-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P2-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P3-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P2-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p3-t01"></a>

## DM P3 T01 retry chủ động khi chưa rõ kết quả

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P3-T01: retry chủ động khi chưa rõ kết quả.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P2-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p3-t01-manual-retry`. AC-DM-06/08/21; TC-DM-05–08/23.
Branch cụ thể: feat/dm-p3-t01-manual-retry. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P3-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P3-T01.1 UI failed action giữ clientMessageId/content bất biến; nút Thử lại không tạo ID mới; nội dung muốn đổi phải là thao tác mới.
- P3-T01.2 Tắt automatic mutation replay ở 401/refresh/network/reconnect; GET vẫn được refresh theo policy.
- P3-T01.3 Fault harness local/test: rollback trước commit, drop response sau commit, delayed response/401; không thêm fault controls vào flow sản phẩm.
- P3-T01.4 Correlate tin tạm/response theo actor+clientMessageId; retry cùng khóa trả ID hiện hành, không duplicate dòng.
- P3-T01.5 E2E/network fault và DB count, recipes để người dùng lặp lỗi; tắt fault xác nhận recovery.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Fault trước commit → failed; bật mạng không tự gửi; bấm Retry mới sent. Drop response sau commit → failed/unknown; Retry đúng một dòng. Nhập cùng nội dung lần mới vẫn tạo tin thứ hai với khóa mới.
Backend: O1 lặp/song song chỉ một message/operation/create outbox; O1/body khác 409 OPERATION_CONFLICT; O2/body giống là tin mới. 401 không có POST tự replay trong Network. Fault trước commit DB không có tin.
Điều kiện đạt: Cả FE/BE chứng minh retry không trùng và không tự gửi lại. Bạn PASS rồi mở proof concurrency/key.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P3-T01-C01 — Rollback rồi có mạng không tự gửi lại
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Fault writer trước commit trả503 chỉ cho request ca; A mở D-AB
- Chụp counts N và Network log; fault auto-retry tắt
Dữ liệu cụ thể: content="RETRY-ROLLBACK-C01"; UI clientMessageId lấy từ POST đầu, đặt O_UI.
Bước kiểm tra frontend:
1. A bật fault, nhập content và Gửi; chờ error/failed.
2. Tắt fault, chuyển Offline→Online, đợi qua chu kỳ reconnect cấu hình; không bấm Retry, kiểm tra Network không thêm POST.
3. Bấm Thử lại một lần, so UUID/content với POST đầu; sau success một dòng sent.
Bước kiểm tra backend:
1. Capture POST đầu lỗi: body O_UI/content; query DB sau rollback trước khi retry.
2. Sau click Retry kiểm tra POST body cùng O_UI; replay REST cùng O_UI nếu muốn đối soát, không dùng UUID mới.
Frontend mong đợi: Failed được giữ, hồi mạng không tự send; manual retry thay tin tạm đúng một dòng.
Backend mong đợi: Fault503; no automatic POST; Retry200 cùng operation ID; repeat body200 same message ID.
Đối soát DB chỉ đọc:
- Trước Retry: delta message/operation/outbox/counter=0.
- Sau Retry: mỗi create count+1, counter+1; REST replay không tăng.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T01-C02 — Mất response sau commit retry không tạo trùng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Harness commit xong rồi drop response chỉ ca này, không rollback
- B mở D-AB nếu realtime đã có thì ghi đúng trạng thái phase; chưa dùng event proof trước P4
Dữ liệu cụ thể: content="LOST-RESPONSE-C02"; O_UI từ request UI; snapshot N.
Bước kiểm tra frontend:
1. Bật drop-response-after-commit, A bấm Gửi một lần.
2. UI timeout/unknown, giữ tin và nút Retry; đọc query DB để xác nhận commit dù FE chưa biết.
3. Tắt fault rồi Retry hai lần có kiểm soát; timeline chỉ một dòng cùng message ID.
Bước kiểm tra backend:
1. Capture first POST body O_UI; query operation/message sau commit.
2. POST lại cùng O_UI/content, kể cả hai request song song bằng runner; lấy response IDs.
Frontend mong đợi: Không tự đổi UUID; unknown/failed chuyển sent, không hai dòng khi response tới muộn.
Backend mong đợi: First response bị mất, không ghi là server rollback; replay200 và mọi response cùng ID.
Đối soát DB chỉ đọc:
- Delta message=1, operation=1, create outbox=1 cho toàn operation.
- Một pair author+clientMessageId; kiểm tra counts không tăng ở retry/late response.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T01-C03 — Đổi body giữ khóa conflict còn khóa mới tạo tin mới
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- O1 đã commit content ORIGINAL-C03 trong run BE; FE có failed O_UI tương đương
- Chụp count sau original
Dữ liệu cụ thể: Original=ORIGINAL-C03; altered=ALTERED-C03; O1/O2 từ fixture.
Bước kiểm tra frontend:
1. Trên UI failed tin ORIGINAL-C03, giữ retry body bất biến.
2. Nếu muốn nội dung ALTERED-C03, soạn tin mới, không sửa ngầm body gắn failed ID.
3. Gửi thêm ORIGINAL-C03 bằng thao tác Gửi mới; ghi ID khác và hai dòng hợp lệ.
Bước kiểm tra backend:
1. POST D-AB messages O1/content ORIGINAL-C03, rồi O1/content ALTERED-C03.
2. POST O2/content ORIGINAL-C03; đọc history IDs và operation records.
Frontend mong đợi: Retry không ghi đè original; cùng text gửi chủ ý lần mới có dòng mới/UUID mới.
Backend mong đợi: Original200; altered cùng O1:409 OPERATION_CONFLICT; O2 cùng content200, ID khác O1.
Đối soát DB chỉ đọc:
- Sau ba POST: đúng2 messages/2 operations/2 create outboxes; không ALTERED-C03 row.
- Fingerprint O1 vẫn từ payload gửi ban đầu, không cập nhật bởi conflict.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T01-C04 — 401 refresh không tự replay POST
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A có session sẽ bị revoked hoặc request send nhận injected401 theo ca
- GET có thể refresh; fault chỉ target một mutation
Dữ liệu cụ thể: content="NO-AUTO-REPLAY-C04"; O_UI captured; POST retry:false.
Bước kiểm tra frontend:
1. A soạn content; bật 401 cho request gửi ca rồi bấm Gửi.
2. Quan sát auth/error UI; cho refresh/đăng nhập lại và mạng online; không bấm Retry, kiểm tra Network chỉ một POST send.
3. Sau phục hồi session và xác nhận quyền D-AB, bấm Retry chủ động; kiểm tra body/UUID như trước.
Bước kiểm tra backend:
1. Record first POST401 và các /auth/refresh/GET nếu có.
2. Query trước manual retry; sau click Retry record POST200 với đúng O_UI; không lấy login mới làm chứng minh token cũ còn hợp lệ.
Frontend mong đợi: Không tự chuyển sent vì refresh thành công; người dùng quyết định retry.
Backend mong đợi: POST đã nhận401 không tự gọi lại; DB chưa commit ca này; manual retry sau session hợp lệ200.
Đối soát DB chỉ đọc:
- Delta trước manual retry=0; sau thành công=1 create.
- Không có worker/service-worker/API wrapper replay mutation ngoài click người dùng.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P3-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P3-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P3-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P3-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P3-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p3-t02"></a>

## DM P3 T02 sequence transaction fingerprint và key rotation

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P3-T02: sequence transaction fingerprint và key rotation.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P3-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `test/dm-p3-t02-concurrency-and-keys`. TC-DM-02/15/24/25; fixtures fingerprint; DM-SQL-01–06.
Branch cụ thể: test/dm-p3-t02-concurrency-and-keys. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P3-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P3-T02.1 Deterministic barrier giữ writer thứ nhất chưa commit, writer thứ hai chờ; kiểm tra counter/commit và rollback, không dùng sleep ngẫu nhiên làm proof.
- P3-T02.2 Test fingerprint byte-order/hash/constant-time path, CRLF normalization; key rotation gửi mới/retry cũ, thiếu key 503 không tạo tin.
- P3-T02.3 Migration legacy giữ đọc lịch sử; operation không fingerprint không bịa từ content hiện hành, retry trả OPERATION_UNVERIFIABLE; migration chạy lại không phá DB.
- P3-T02.4 Kiểm tra UI khi hai send/response đảo thứ tự và key unavailable; lỗi có thử lại chủ động, không mất tin/counter.
- P3-T02.5 Bàn giao runner song song/barrier, fixture key tổng hợp không commit key thật, SQL invariants và report.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A1/A2 gửi hai tin đồng thời, B reload thấy đúng hai tin theo sequence; ngắt key mới rồi retry cũ có kết quả đúng, key thiếu hiện lỗi và không thêm dòng sent giả.
Backend: Chạy runner giữ transaction theo hướng dẫn; không có trang history bỏ sót commit trễ; rollback không tạo operation/outbox; hash khớp fixture, retry key cũ hoạt động sau restart/rotation; legacy retry không trùng.
Điều kiện đạt: Đồng thời/keys/migration có proof API/DB và UI tương ứng; bạn PASS rồi mới realtime.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P3-T02-C01 — Giữ transaction để chứng minh sequence theo commit
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1/A2 cùng user nhưng session độc lập; D-AB counter N
- Runner barrier giữ writer X dưới lock trước commit; writer Y phải chờ
Dữ liệu cụ thể: X=SEQ-X-C01; Y=SEQ-Y-C01; hai UUID mới.
Bước kiểm tra frontend:
1. A1 gửi X qua runner/UI đã gắn barrier; giữ chưa commit.
2. A2 gửi Y trong khi X đang chờ; quan sát Y chưa sent dù request bắt đầu.
3. Release X, đợi cả hai success; B reload history, X rồi Y đúng sequence.
Bước kiểm tra backend:
1. Start two POST messages X/Y với barrier deterministic; trước release query committed history.
2. Release X commit; thu responses, GET history after cursor baseline và compare.
Frontend mong đợi: Không báo sent cho Y trước lock/commit; history có hai tin sau release, không bỏ X.
Backend mong đợi: Trước release không thấy X/Y committed; sau commit X.sequence=N+1,Y=N+2 với schema counter hiện hành, IDs khác nhau.
Đối soát DB chỉ đọc:
- Message/operation/outbox mới đúng2; counter tăng2; không sequence cấp bằng MAX+1 ngoài khóa.
- History union bằng committed writer set.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T02-C02 — Rollback writer đầu giải phóng lock không để orphan
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Runner barrier như C01 nhưng chủ ý rollback X
- D-AB baseline N, Y payload riêng
Dữ liệu cụ thể: X=ROLLBACK-X-C02; Y=COMMIT-Y-C02; UUID mới.
Bước kiểm tra frontend:
1. A1 gửi X và giữ transaction, A2 gửi Y phải chờ.
2. Trigger rollback X, quan sát A1 error; Y hoàn tất.
3. B reload history, chỉ Y; bấm Retry X mới là quyết định người dùng khác bước ca.
Bước kiểm tra backend:
1. Two POST barrier; rollback X, commit Y; capture statuses và history.
2. Query operation/outbox của X/Y sau transactions kết thúc; không retry X trong runner tự động.
Frontend mong đợi: X failed, Y sent; không dòng sent X hoặc mất Y do khóa bị giữ mãi.
Backend mong đợi: X lỗi đúng harness, Y200; Y sequence/counter đúng committed state sau rollback, không history gap làm mất tin.
Đối soát DB chỉ đọc:
- Không row X/message/operation/outbox; một bộ records Y; lock được release.
- Counter bằng baseline+1 cho ca biệt lập; không orphan khi transaction fail.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T02-C03 — Rotation key giữ retry cũ và thiếu key trả503
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- O1 gửi original với HMAC key K1 tổng hợp; keys ngoài Git
- Có key K2 riêng và recipe reload/restart config không mất K1
Dữ liệu cụ thể: original=KEY-ROTATE-C03; new payload=KEY-NEW-C03; O2 chưa dùng.
Bước kiểm tra frontend:
1. Giữ một tin failed/unknown O_UI gửi dưới K1 để thử retry.
2. Đổi active key sang K2, giữ K1 đọc được; retry tin cũ thành sent một dòng.
3. Tạm làm K1 không đọc được, thử retry operation K1; UI lỗi rõ, không đổi ID; phục hồi key và Retry.
Bước kiểm tra backend:
1. Send O1/K1; rotate activeK2, POST original O1; send O2 mới.
2. Remove availability K1 ở môi trường test, retry O1; restore K1 rồi retry lại; restart API giữ key ring/key version.
Frontend mong đợi: Retry cũ không tạo tin mới; thiếu key không hiện success hoặc conflict giả.
Backend mong đợi: Replay K1 sau rotation200 sameID; operation mới key_id K2; missingK1:503 FINGERPRINT_KEY_UNAVAILABLE, không409 payload conflict; restore200.
Đối soát DB chỉ đọc:
- Two original operations đúng O1/O2; không thêm row khi key thiếu/retry.
- Fingerprint_version/key_id/hash khớp fixture, không raw content trong operation; key bytes không nằm DB/docs.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P3-T02-C04 — Legacy retry không bịa fingerprint và bigint/UUID fixture đúng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Migration fixture chứa legacy message có clientMessageId nhưng không send fingerprint
- New writer chưa tạo operation cho legacy; user pair fixture bắt lỗi endian
Dữ liệu cụ thể: UUID pair 00000001-0000-4000-8000-000000000000 và 00000100-0000-4000-8000-000000000000; legacy original operation alias LEGACY-O.
Bước kiểm tra frontend:
1. A mở legacy DM, xem tin hiện hành bằng history thật.
2. Gọi recipe legacy replay; UI/error không tự chuyển thành send ID mới.
3. Mở lại lịch sử và fixture hai UUID, kiểm tra vẫn đúng peer/tin, không duplicate.
Bước kiểm tra backend:
1. POST legacy key/content theo fixture; expect unverifiable, GET history remains readable.
2. Run UUID ordering/HMAC byte fixture và migration fresh/existing/idempotent runner; output numeric aggregates không keys/body riêng.
Frontend mong đợi: Legacy history vẫn đọc theo quyền; error replay không hồi sinh body hay thêm dòng mới.
Backend mong đợi: Legacy retry409 OPERATION_UNVERIFIABLE; không tính fingerprint từ message đã sửa; UUID comparator khớp low/high DB.
Đối soát DB chỉ đọc:
- Legacy count/IDs/body hiện hành không đổi do replay; migration chạy lại không nhân rows/counter.
- Unique pair ordering và bigint wire fixtures đều đạt, không castingNumber làm mất precision.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P3-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P3-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P3-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P4-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P3-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p4-t01"></a>

## DM P4 T01 Hub outbox và merge sự kiện

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P4-T01: Hub outbox và merge sự kiện.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P3-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p4-t01-realtime-outbox`. AC-DM-02/12/21; TC-DM-03/12/16/23.
Branch cụ thể: feat/dm-p4-t01-realtime-outbox. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P4-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P4-T01.1 Map `/hubs/chat` với auth token đúng route, session reader/registry theo user/session/conversation; SubscribeConversation/UnsubscribeConversation đúng schema.
- P4-T01.2 Dispatcher lease/claim sau commit, retry/backoff/recovery; payload tham chiếu, lúc phát đọc current message/version và kiểm tra quyền connection.
- P4-T01.3 FE handler/buffer đăng ký trước subscribe, merge ID/clientMessageId/version chính xác; event trước response/trùng/đảo thứ tự không nhân tin.
- P4-T01.4 UI trạng thái connection thật; dừng worker làm DB lưu tin nhưng không giả realtime online delivery; không thêm “đã đọc/đã nhận”.
- P4-T01.5 Test worker crash/restart, unsubscribe, outsider subscribe, late event; recipe trực tiếp Hub qua browser runner/helper.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A/B mở D-AB, A gửi B thấy không reload; response/event đảo thứ tự chỉ một dòng; worker dừng rồi chạy lại khôi phục cập nhật; C không nhận tin D-AB.
Backend: DB outbox commit/publish đúng; rollback không có event; subscribe outsider bị từ chối không rò metadata; retry worker không tạo message mới; logs không token/body.
Điều kiện đạt: Hai browser nhận tin thật và quyền subscribe/dispatch đạt. Thu hồi đầy đủ vẫn phải qua P6; bạn PASS rồi reconnect.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P4-T01-C01 — Hai browser nhận MessageChanged không reload
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1/B1 login và subscribe D-AB bằng Hub thật
- Worker/registry running, capture subscribe ack
Dữ liệu cụ thể: content="REALTIME-C01"; UI ID captured; không chứng minh đã đọc/đã nhận.
Bước kiểm tra frontend:
1. B mở D-AB trước, A cũng mở D-AB.
2. A Gửi REALTIME-C01, ghi gửi→response và commit→B render timestamps.
3. B không reload vẫn thấy một dòng; A cũng một dòng, tên/ID/body đúng.
Bước kiểm tra backend:
1. Inspect /hubs/chat authenticated connection, SubscribeConversation({conversationId:D-AB}) ack.
2. Capture POST message response và MessageChanged payload, đối chiếu message.id/clientMessageId/version, eventId chỉ là event ID.
Frontend mong đợi: A sent sau persistence; B nhận dòng mới không reload; không hiển thị read receipt tự bịa.
Backend mong đợi: Send200; event DTO cùng current message/ID/version, routing chỉ connections đủ quyền; mutation chỉ REST.
Đối soát DB chỉ đọc:
- Message/operation/create outbox delta1; published/lease state đúng; event sau commit.
- Không chứa body trong outbox stored payload; dispatcher lấy current message để gửi.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T01-C02 — Worker dừng thì history vẫn đúng và bật lại phát bù
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A/B subscribe D-AB, runner có lệnh dừng riêng dispatcher không tắt DB/API
- Capture baseline outbox/messages
Dữ liệu cụ thể: content="WORKER-STOP-C02"; dừng worker30 giây phục vụ fault, không target SLA.
Bước kiểm tra frontend:
1. Dừng worker theo lệnh đã bàn giao, A gửi tin và thấy sent.
2. B chưa nhận realtime, bấm reload history vẫn thấy tin từ DB.
3. Start worker, B/A không duplicate khi outbox được publish lại; kiểm tra connection states phản ánh transport thật.
Bước kiểm tra backend:
1. POST message khi dispatcher stopped, GET history bằng B200.
2. Query outbox pending; bật worker, record publish/event và lease/attempts; re-run dispatch same event trong test.
Frontend mong đợi: Sent đúng DB dù realtime chậm; load history được; worker recovery không thêm dòng thứ hai.
Backend mong đợi: Send200, history200, outbox pending lúc stopped; publish sau worker start, không gọi lại POST writer.
Đối soát DB chỉ đọc:
- Message/operation/create event chỉ1; repeated dispatch không tạo message.
- Outbox processed/published, lease không bị giữ mãi sau worker crash.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T01-C03 — Event trước response duplicate và cũ không nhân tin
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Browser fault harness reorder/delay HTTP chỉ case; backend event thật từ message đã commit
- A/B subscribed
Dữ liệu cụ thể: content="EVENT-FIRST-C03"; O_UI; application event duplicate injection có nhãn fixture.
Bước kiểm tra frontend:
1. Delay HTTP response gửi của A nhưng để MessageChanged thật đến trước.
2. A gửi và nhận event trước ack, kiểm tra một dòng khi response muộn tới.
3. Harness phát lại event cùng ID/version hai lần; nhìn A/B vẫn một dòng, không tăng badge/count giả.
Bước kiểm tra backend:
1. Capture actual POST/current MessageChanged và delay release; record order event→response.
2. Inject duplicate application event in isolated browser runner; compare GET history DB and FE unique IDs; không coi injected event là proof dispatcher thực.
Frontend mong đợi: Tin tạm được hợp nhất bằng actor+clientMessageId/ID; không backslide hoặc duplicate.
Backend mong đợi: DB write1; repeat event cùng version bỏ qua/merge idempotent; eventId không dùng làm resume cursor.
Đối soát DB chỉ đọc:
- Message/operation/outbox create count1; không rows do injected event.
- Tập FE IDs/version cuối bằng API current snapshot.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T01-C04 — C subscribe bị chặn và unsubscribe không còn nhận
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- C không member D-AB; A/B connected
- Có Hub invoke runner session-aware, không thêm send qua Hub
Dữ liệu cụ thể: SubscribeConversation payload {"conversationId":"<D-AB.id>"}; content ROUTE-DENY-C04.
Bước kiểm tra frontend:
1. C mở DM UI, không có D-AB; chạy recipe Hub subscribe C và ghi lỗi.
2. B unsubscribe D-AB qua chuyển conversation và ack; giữ connection ở DM khác.
3. A gửi ROUTE-DENY-C04; C không có payload, B subscription cũ không nhận; B mở lại D-AB đọc history hợp lệ.
Bước kiểm tra backend:
1. Invoke SubscribeConversation D-AB bằng C, kiểm tra HubException ProblemDetails schema không stack/private participants.
2. Invoke UnsubscribeConversation bằng B rồi A POST; inspect registry target recipients và B resubscribe/history.
Frontend mong đợi: C không nhìn/nhận D-AB; B unsubscribe không còn nhận room event cho subscription cũ.
Backend mong đợi: C invoke deny theo Hub contract; B unsubscribe ack; message gửi hợp lệ200, chỉ target current authorized subscriptions.
Đối soát DB chỉ đọc:
- Registry không có C/D-AB hoặc B subscription đã bỏ; message create1 vẫn lưu cho A/B history.
- Không coi group membership SignalR cũ là đủ auth để dispatch.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P4-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P4-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P4-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P4-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P4-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p4-t02"></a>

## DM P4 T02 reconnect nhiều trang và sửa xóa tin cũ

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P4-T02: reconnect nhiều trang và sửa xóa tin cũ.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P4-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p4-t02-reconnect-history`. AC-DM-15/21; TC-DM-16/17/25; contract-8.
Branch cụ thể: feat/dm-p4-t02-reconnect-history. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P4-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P4-T02.1 Reconnect subscribe lại, buffer trước subscribe/ack và GET bù tới through cố định; không dùng eventId/highest live sequence làm frontier đã đọc đủ.
- P4-T02.2 Sau merge mỗi trang mới tiến cursor; chỉ nhận resumeCursor cuối; gián đoạn giữa ba trang tiếp tục đúng, cursor hết hạn tải lại.
- P4-T02.3 Reload cửa sổ lịch sử đang xem, invalidation cache cũ; wire cùng Message schema để chuẩn bị edit/delete ở P5.
- P4-T02.4 Offline UI, GET recovery, tin failed vẫn chờ bấm retry; response actor cũ không ghi lại cache.
- P4-T02.5 R101 và interruption runner, duplicate/out-of-order events và cursor faults. Edit/delete server chưa có thì dùng fixture ở test thành phần, ghi rõ; proof E2E tin cũ chạy lại sau P5.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: B offline, A gửi R101, B reconnect nhận đủ ba trang; mất mạng sau trang 1/2 rồi nối lại không mất/trùng; failed message không tự gửi.
Backend: H/after/frontier/nextCursor qua ba request đúng; cursor sửa/chéo/expired bị chặn; compare set ID DB = FE. Phiên invalid không subscribe hay history được.
Điều kiện đạt: Reconnect send/history thật đạt; nhánh edit/delete tin cũ chưa đánh dấu đạt tại task này và là gate bắt buộc ở P5-T03.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P4-T02-C01 — B offline rồi bù đủ101 tin ba trang
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- B đã load D-AB và lưu resumeCursor R0 ở frontier H0
- A/B từng subscribe, helper seed qua writer chụp baseline IDs
Dữ liệu cụ thể: R101 RECONNECT-001...101, limit50, added IDs manifest R_set.
Bước kiểm tra frontend:
1. B bật Offline/đóng Hub rồi A tạo101 tin qua writer.
2. B Online, kiểm tra subscribe lại và requests after tự bù.
3. Đợi merge hoàn tất; đối chiếu timeline đủ101 tin mới, không lặp những tin baseline.
Bước kiểm tra backend:
1. Using B: GET messages?after=<R0>&limit=50, lưu nextCursor/through H1.
2. Hai GET after=<nextCursor>&limit=50; không tự đưa sequence arbitrary; compare current R_set.
Frontend mong đợi: Offline/connecting/online phản ánh thật; đủ101 mới ngoài baseline; chưa tự retry failed send.
Backend mong đợi: Pages50/50/1, cùng through H1; resumeCursor mới chỉ publish ở page cuối; items.ID union đúng R_set.
Đối soát DB chỉ đọc:
- DB có101 new operations/messages/create events từ seed; GET không tạo thêm.
- FE/new REST set bằng DB new committed set, không lấy hiệu sequence làm count.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T02-C02 — Mất mạng sau trang1 rồi resume không bỏ50 tin cuối
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Data R101 như ca riêng, R0/H1 known
- Harness ngắt transport ngay sau page1 merge
Dữ liệu cụ thể: Break after page1 hoặc page2, chạy lần độc lập cho mỗi điểm ngắt.
Bước kiểm tra frontend:
1. B Online để catchup, page1 merge xong thì harness Offline.
2. Đợi state offline, kiểm tra resume không nhảy lên H1 khi chưa đủ.
3. Online lại, bù phần thiếu/refresh đúng contract; cuối có đủ101 duy nhất.
Bước kiểm tra backend:
1. Capture after page1 nextCursor và resumeCursor null trong response hasMore=true.
2. Retry catchup sau reconnect; giữ frontier khi continuation cursor hợp lệ hoặc bootstrap sạch theo contract khi cursor invalid.
Frontend mong đợi: Không coi50 tin đã merge là đủ101; UI không bị thiếu nhóm tin cuối.
Backend mong đợi: Frontier không tiến vì event live max; last resume chỉ sau all-page merge; repeated page idempotent.
Đối soát DB chỉ đọc:
- DB R_set không đổi bởi GET/reconnect; FE counts101/unique101 cuối ca.
- Client checkpoint logs không lưu body/token và không tuyên bố page success trước merge.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T02-C03 — Event live giữa lúc bù không làm nhảy cursor
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- B offline có backlog101; A có thể gửi thêm LIVE-C03 lúc B đang nhận page1
- Buffer handlers bật trước subscribe ack
Dữ liệu cụ thể: Backlog frontier H1; live message H2=H1+1 sau snapshot.
Bước kiểm tra frontend:
1. B reconnect, hold response page1 để A gửi LIVE-C03.
2. Release page1 rồi bù pages2/3; kiểm tra live event được đệm/merge nhưng không nhảy checkpoint qua pages chưa đọc.
3. Tải thêm cycle/snapshot theo protocol, nhìn backlog và live mỗi tin một dòng.
Bước kiểm tra backend:
1. Inspect subscriptions established trước GET, page responses giữ through=H1.
2. Record MessageChanged LIVE-C03 và final history/checkpoint; next catchup từ frontier H1 nếu live H2 chưa covered.
Frontend mong đợi: Không mất backlog giữa page boundaries; event trước/giữa GET được merge với version đúng.
Backend mong đợi: All three catchup pages through H1; highest realtime H2 không dùng để bỏ qua H1 pages; final FE has102 new IDs once.
Đối soát DB chỉ đọc:
- 101 backlog+1 live message,102 operations/create records; không duplicates.
- DB committed sequence order và FE sort precision consistent.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P4-T02-C04 — Cursor expired recovery và failed send không tự gửi
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- B có old resumeCursor; test config/fixture làm cursor expired không chờ24h thật
- Một failed operation F_UI chưa commit nằm trong tab B
Dữ liệu cụ thể: ExpiredCursor đã biết; unsent content KEEP-FAILED-C04.
Bước kiểm tra frontend:
1. B gửi trong rollback fault để giữ failed F_UI, tắt fault và Offline.
2. Làm cursor expired bằng helper test rồi Online; UI tải lại history/current window.
3. Không bấm Retry, Network không POST F_UI; sau history phục hồi bấm Retry chủ động để gửi một lần.
Bước kiểm tra backend:
1. GET messages after expired cursor:400 CURSOR_INVALID; GET latest sạch200 và reload pages đang xem.
2. Track POST F_UI attempts: first fault, không automatic replay, manual Retry mới có request; inspect operation records.
Frontend mong đợi: Lỗi cursor tự phục hồi bằng GET, failed vẫn failed đến khi bấm; không mất quyền hoặc dùng cache cũ làm current.
Backend mong đợi: 400 invalid cursor không kéo mutation retry; old token invalid nếu fixture đổi thì auth chặn, không tự subscribe trái quyền.
Đối soát DB chỉ đọc:
- Trước manual retry không row F_UI; sau manual successful commit một row.
- Chỉ chứng minh reconnect new history ở phase này; edit/delete old-window E2E chờ P5-T03.
Bằng chứng cần lưu:
- Ghi commit/build, run và actor; lưu kết quả từng bước FE/BE.
- Lưu HTTP status/errorCode, tập ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault nếu có; giữ dữ liệu và manifest của run để đối soát. Ca cần trạng thái ban đầu khác dùng dữ liệu/run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P4-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P4-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P4-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P5-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P4-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p5-t01"></a>

## DM P5 T01 sửa tin của mình

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P5-T01: sửa tin của mình.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P4-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p5-t01-edit-message`. AC-DM-04/10/11/19; TC-DM-09/13; TC-TEXT-06.
Branch cụ thể: feat/dm-p5-t01-edit-message. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P5-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P5-T01.1 PATCH expectedVersion; actor/member/author guard, same text đúng version là no-op; validation chung và không yêu cầu peer active.
- P5-T01.2 Compare-and-update/version + outbox cùng transaction; không lưu content cũ trong bất kỳ edit/event/log payload.
- P5-T01.3 UI menu chỉ author, edit/cancel/save/error/conflict; giữ bản xác nhận gần nhất khi lỗi, hiển thị “Đã sửa”.
- P5-T01.4 Dispatch DTO mới và merge version; B/A2 nhận text mới; update inbox projection nếu có mà không giữ bản cũ.
- P5-T01.5 API/UI/DB test sửa nhiều lần/no-op/biên/outsider/non-author, offline reload và recipe.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A sửa M01 hai lần, B/A2 thấy mới nhất; Cancel không đổi; B không có menu sửa A; text biên bị từ chối và không làm mất bản đã lưu.
Backend: B/C PATCH A bị chặn; expectedVersion cũ conflict; no-op không tăng version/outbox/editedAt; DB/event/log không có body cũ, GET không có version-history API.
Điều kiện đạt: Edit đúng tác giả/phiên bản và chỉ nội dung mới nhất. Bạn PASS rồi delete.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P5-T01-C01 — Sửa hai lần, chỉ giữ nội dung hiện tại
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A gửi EDIT-ORIGINAL, nhận id M_E, version='1'; B mở D-AB online, A2 có phiên riêng.
- Chụp counts và editedAt ban đầu.
Dữ liệu cụ thể: Lần 1 "EDIT-FIRST"; lần 2 "EDIT-SECOND"; expectedVersion lấy từ response trước.
Bước kiểm tra frontend:
1. A chọn Sửa M_E, thay bằng EDIT-FIRST rồi Lưu.
2. B và A2 quan sát nội dung đổi, có trạng thái đã sửa, cùng ID/vị trí.
3. A sửa lần hai EDIT-SECOND; không hiện body cũ trong lịch sử sửa hoặc UI.
Bước kiểm tra backend:
1. Lane BE riêng: POST tạo M_E_BE với UUID mới; PATCH /api/v1/direct-conversations/<D-AB.id>/messages/<M_E_BE.id> {"expectedVersion":"1","content":"EDIT-FIRST"}.
2. PATCH cùng URL {"expectedVersion":"2","content":"EDIT-SECOND"}; GET trang history chứa ID để đọc hiện trạng.
Frontend mong đợi: FE chỉ còn EDIT-SECOND, ID/sequence giữ nguyên; mọi client online cùng version mới.
Backend mong đợi: PATCH lần lượt 200 version='2' và '3'; content đúng; editedAt không null; GET hiện version='3', không trả EDIT-ORIGINAL/EDIT-FIRST.
Đối soát DB chỉ đọc:
- Per lane: cùng 1 message, sequence không đổi, version tăng đúng 2; counter tạo tin không tăng do PATCH.
- Có 2 sự kiện sửa với metadata ID/version; không body sửa cũ trong message_edits, outbox, audit/log.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T01-C02 — Hủy sửa và lưu nguyên nội dung là no-op
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_N thuộc A chưa xóa; biết version V, content, editedAt E và outbox count.
- Không có mutation đồng thời.
Dữ liệu cụ thể: M_N.content="  Giữ khoảng trắng có chủ ý  "; PATCH cùng chuỗi, cùng expectedVersion.
Bước kiểm tra frontend:
1. A mở Sửa, nhập KHONG-LUU rồi Hủy.
2. Mở lại kiểm tra vẫn chuỗi ban đầu đầy đủ khoảng trắng.
3. Giữ nguyên nội dung, bấm Lưu nếu UI cho phép; nếu UI chặn no-op ghi không có PATCH, vẫn kiểm tra BE.
Bước kiểm tra backend:
1. GET history ghi version V, editedAt E.
2. PATCH /api/v1/direct-conversations/<D-AB.id>/messages/<M_N.id> {"expectedVersion":"<V>","content":"  Giữ khoảng trắng có chủ ý  "}.
Frontend mong đợi: Hủy không đổi tin; no-op không tạo dấu sửa mới, không thêm dòng, không mất khoảng trắng.
Backend mong đợi: 200 cùng version V/content/editedAt E; không phát event sửa mới.
Đối soát DB chỉ đọc:
- Message ID/sequence/version/editedAt nguyên trạng.
- Delta outbox/journal mutation=0; không tạo lịch sử body.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T01-C03 — Tác giả khác và ID thuộc hội thoại khác bị từ chối
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_A thuộc A trong D-AB; M_AC thuộc A trong D-AC; B là member D-AB, C outsider.
- Chụp cả hai tin.
Dữ liệu cụ thể: B cố sửa M_A; A cố PATCH M_AC qua URL D-AB.
Bước kiểm tra frontend:
1. B mở D-AB; menu tin A không có quyền Sửa.
2. Dùng Network/helper gửi request cưỡng bức bằng phiên B, không sửa token thành A.
3. A và C mở D-AC kiểm tra M_AC còn nguyên.
Bước kiểm tra backend:
1. Bearer B PATCH D-AB/messages/M_A với expectedVersion hiện tại và content="UNAUTHORIZED-EDIT": 403.
2. Bearer A PATCH D-AB/messages/M_AC: 404; C PATCH D-AB/messages/M_A trả đúng status/errorCode denial đã chốt tại P0.
Frontend mong đợi: UI không cho B sửa tin A; lỗi request không cập nhật lạc quan thành nội dung trái quyền.
Backend mong đợi: B=403; sai resource=404; outsider theo contract P0 cụ thể, không tiết lộ body/metadata tin.
Đối soát DB chỉ đọc:
- Cả M_A/M_AC không đổi version/content.
- Không event sửa, không journal protection mới do request bị từ chối.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T01-C04 — Validation và version cũ giữ nội dung thật
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_V thuộc A version V; A2 đã tải V; A1 có quyền sửa.
- Đã có file payload biên UTF-16 và fixture lỗi.
Dữ liệu cụ thể: content="" hoặc a×2001; A1 sửa "EDIT-WINNER"; A2 stale sửa "EDIT-LOSER".
Bước kiểm tra frontend:
1. A thử xóa hết nội dung hoặc dán 2001 chữ a trong editor; không lưu thành công.
2. A1 lưu EDIT-WINNER; A2 vẫn giữ editor từ V.
3. A2 Lưu EDIT-LOSER; nhận xung đột, tải hiện trạng và thấy EDIT-WINNER.
Bước kiểm tra backend:
1. PATCH M_V với version hiện tại, content="" =>400 CONTENT_EMPTY; content a×2001 =>400 CONTENT_TOO_LONG.
2. Lane BE riêng tạo tin version1, PATCH WINNER expectedVersion1 =>200/version2; PATCH LOSER expectedVersion1 =>409 VERSION_CONFLICT; GET history hiện WINNER.
Frontend mong đợi: Validation giữ editor cho người dùng sửa; xung đột không tự ghi đè, không auto retry PATCH.
Backend mong đợi: Input lỗi không đổi version; stale PATCH 409 VERSION_CONFLICT; history trả winner.
Đối soát DB chỉ đọc:
- Per lane chỉ lần sửa hợp lệ tăng version/outbox đúng 1.
- Không lưu loser/body lỗi; không tăng sequence tạo tin.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P5-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P5-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P5-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P5-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P5-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p5-t02"></a>

## DM P5 T02 xóa tin giữ tombstone

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P5-T02: xóa tin giữ tombstone.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P5-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p5-t02-delete-message`. AC-DM-05/10; TC-DM-08/10/13.
Branch cụ thể: feat/dm-p5-t02-delete-message. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P5-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P5-T02.1 DELETE expectedVersion, auth trước cả delete lặp; lần đầu content null/deletedAt/version, giữ ID/sequence/SendOperation.
- P5-T02.2 Outbox/current DTO giữ tombstone không body; lần xóa lặp trả current tombstone không tăng version/outbox; PATCH tombstone MESSAGE_DELETED.
- P5-T02.3 UI confirmation/cancel/error; giữ vị trí/thời gian với “Tin nhắn đã bị xóa”, bỏ body/menu sửa ở client/cache.
- P5-T02.4 Retry send ban đầu sau delete trả tombstone cùng ID; HMAC so với original operation, không dựng lại body.
- P5-T02.5 DB invariant/migration/contract tests và UI hai actor.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A xóa M01, B thấy dòng thay thế tại đúng vị trí; Cancel không xóa; API lỗi giữ tin xác nhận gần nhất; reload vẫn tombstone.
Backend: DELETE lặp đúng author an toàn, non-author bị chặn; PATCH deleted conflict; replay O1 trả cùng tombstone, DB content null và operation còn nguyên, không message/create-event mới.
Điều kiện đạt: Xóa không mất vị trí hoặc hồi sinh body. Bạn PASS rồi race/reconnect proof.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P5-T02-C01 — Xóa giữ tombstone đúng ID, sequence và vị trí
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A gửi DELETE-TARGET trong D-AB, B online; có tin trước/sau để xác định vị trí.
- Biết ID M_D, version V và counter N.
Dữ liệu cụ thể: DELETE /api/v1/direct-conversations/<D-AB.id>/messages/<M_D.id>?expectedVersion=<V>.
Bước kiểm tra frontend:
1. A chọn Xóa M_D và xác nhận.
2. B quan sát placeholder tin đã xóa, không còn DELETE-TARGET.
3. A reload/lấy history: tombstone vẫn ở giữa hai tin, không nhảy vị trí hoặc tạo tin mới.
Bước kiểm tra backend:
1. Lane BE riêng tạo M_D_BE; DELETE đúng URL với expectedVersion hiện tại.
2. GET history chứa ID; so sánh response DELETE với current row/page.
Frontend mong đợi: Body biến mất trên A/B; tombstone giữ cùng ID/vị trí, không có undo khôi phục body.
Backend mong đợi: 200 Message: content=null, deletedAt không null, version=V+1 dạng chuỗi; ID/sequence/createdAt giữ nguyên.
Đối soát DB chỉ đọc:
- Per lane đúng 1 version tăng/1 event xóa; counter=N.
- Không hard-delete message hoặc SendOperation; không body đã xóa ở outbox/log/audit.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T02-C02 — Hủy xóa và lỗi trước commit không mất tin
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_KEEP của A đang có body KEEP-ME, version V.
- Helper có fault rollback DELETE trước commit, không áp dụng production.
Dữ liệu cụ thể: Lỗi test DELETE 503 trước commit; không phải response mất sau commit.
Bước kiểm tra frontend:
1. A mở xác nhận Xóa, bấm Hủy; Network không có DELETE.
2. Bật fault trước commit, A xác nhận Xóa.
3. Thấy lỗi; sau tắt fault đọc lại tin trước khi bấm thao tác xóa mới.
Bước kiểm tra backend:
1. Chụp DB rồi gọi DELETE với version V khi fault đang bật; ghi 503 từ helper.
2. Tắt fault; GET history còn content KEEP-ME/version V; không tự DELETE lại.
Frontend mong đợi: Hủy giữ tin; lỗi rollback không làm UI giữ tombstone giả hoặc tự replay.
Backend mong đợi: Fault recipe trả 503, transaction rollback; GET trả body/version cũ.
Đối soát DB chỉ đọc:
- Delta version/outbox/journal committed=0.
- Không deletedAt, không tăng counter; prepared record nếu có phải được reconcile đúng trạng thái aborted.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T02-C03 — Xóa lặp và PATCH tombstone
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_T của A được xóa thành tombstone version V_T; B member không phải author.
- Đọc hiện trạng và outbox count sau lần xóa đầu.
Dữ liệu cụ thể: Lặp DELETE với expectedVersion=V_T; PATCH content="RESURRECT".
Bước kiểm tra frontend:
1. A thử thao tác lại trên tombstone; UI không mở editor để phục hồi body.
2. B không có action xóa tin A, kể cả tombstone.
3. Reload cả A/B: chỉ một placeholder, không có body phục hồi.
Bước kiểm tra backend:
1. Bearer A DELETE M_T?expectedVersion=<V_T> =>200 cùng tombstone.
2. Bearer A PATCH M_T {"expectedVersion":"<V_T>","content":"RESURRECT"} =>409 MESSAGE_DELETED; Bearer B DELETE cùng ID =>403.
Frontend mong đợi: Không thêm tombstone, không đổi vị trí, không khôi phục tin.
Backend mong đợi: DELETE lặp 200 cùng version/deletedAt; PATCH 409 MESSAGE_DELETED; B403.
Đối soát DB chỉ đọc:
- Delta outbox/version cho cả các request lặp/từ chối=0.
- Content null, marker dedup còn nguyên.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T02-C04 — Replay send ban đầu sau xóa không làm sống lại tin
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Lane BE riêng D-AB: send O1 body ORIGINAL-DELETE =>200 rồi DELETE tin đó.
- Đã lưu original request O1, tombstone ID/version; không dùng O1 của ca khác trong cùng pair.
Dữ liệu cụ thể: O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; original content="ORIGINAL-DELETE".
Bước kiểm tra frontend:
1. A/B mở hội thoại có tombstone và ghi counts.
2. Chạy replay BE bên dưới khi UI đang online.
3. Quan sát không xuất hiện ORIGINAL-DELETE lần nữa; reload vẫn tombstone.
Bước kiểm tra backend:
1. POST messages đúng original O1/content ORIGINAL-DELETE =>200 current tombstone cùng ID.
2. POST cùng O1 nhưng content="CHANGED-DELETE" =>409 OPERATION_CONFLICT; GET history xác nhận tombstone.
Frontend mong đợi: Không thêm tin, body cũ không trở lại dù replay send thành công.
Backend mong đợi: Replay original trả tombstone hiện tại; fingerprint so với body ban đầu, không so với content null; khác body409.
Đối soát DB chỉ đọc:
- Delta message/send_operation/counter/create-outbox=0.
- Tombstone/version không đổi; fingerprint marker vẫn tồn tại.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P5-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P5-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P5-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P5-T03, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P5-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p5-t03"></a>

## DM P5 T03 xung đột edit delete và reconnect tin cũ

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P5-T03: xung đột edit delete và reconnect tin cũ.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P5-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `test/dm-p5-t03-mutation-races`. AC-DM-14/15/21; TC-DM-08/11/17/23/25.
Branch cụ thể: test/dm-p5-t03-mutation-races. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P5-T03.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P5-T03.1 A1/A2 edit-edit và edit-delete từ cùng version, barrier deterministic; một thắng, một conflict, UI tải current.
- P5-T03.2 Replay original send sau nhiều edit/delete, cùng key khác body; không sinh message/outbox tạo mới. Nhánh peer disabled được kiểm chứng ở P6-T02 với helper khóa riêng, không đặt phụ thuộc ngược vào task này.
- P5-T03.3 B offline với tin cũ trong cửa sổ; A sửa/xóa, thêm R101; reconnect bù hết trang và reload cửa sổ cũ bằng API thật.
- P5-T03.4 Event cũ sau version mới/tombstone không thay body; no-op/delete lặp cùng kiểm tra tác giả.
- P5-T03.5 Bàn giao conflict/lost response/reconnect recipes, DB counts và E2E proof cho nhánh còn chờ ở P4-T02.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Hai phiên A sửa/xóa cùng lúc, không ghi đè âm thầm; B reconnect thấy text mới/tombstone của tin cũ và đủ R101; Retry original không kéo body cũ về.
Backend: Runner chứng minh một version transition thắng, expectedVersion loser conflict; compare latest/version/tombstone DB với REST/event; không lưu content cũ hoặc tăng create counts khi replay.
Điều kiện đạt: Đóng phần reconnect edit/delete còn thiếu P4, có UI/API/DB thật. Bạn PASS rồi kiểm chứng bảo mật hệ thống.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P5-T03-C01 — Hai edit cùng version chỉ một winner
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1/A2 phiên độc lập đã tải M_R version V.
- Helper barrier test sắp hai PATCH cùng expectedVersion, có manifest timestamp.
Dữ liệu cụ thể: Content A1="RACE-LEFT"; A2="RACE-RIGHT".
Bước kiểm tra frontend:
1. A1/A2 cùng mở editor trên M_R.
2. Dùng barrier recipe cho hai Lưu tới server cùng khoảng tranh chấp.
3. Phiên thua thấy conflict rồi đọc hiện trạng; A/B/A2 đồng nhất winner.
Bước kiểm tra backend:
1. Lane BE riêng gửi song song hai PATCH cùng ID/version V, content LEFT/RIGHT.
2. Ghi hai HTTP response, GET history; không giả định LEFT luôn thắng.
Frontend mong đợi: Chỉ một body cuối; loser không tự retry overwrite.
Backend mong đợi: Đúng một200 version V+1, một409 VERSION_CONFLICT; GET có content của response200.
Đối soát DB chỉ đọc:
- Delta version=1, mutation-outbox=1.
- Không lưu loser hoặc lịch sử body cũ; counter tạo tin không đổi.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T03-C02 — Delete commit trước edit: tombstone luôn thắng
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_RD của A version V; A2 mở editor stale.
- Barrier kiểm soát DELETE COMMIT xong mới thả PATCH stale; không gọi đây là race không xác định.
Dữ liệu cụ thể: DELETE expectedVersion=V; PATCH expectedVersion=V content="TOO-LATE".
Bước kiểm tra frontend:
1. A2 nhập TOO-LATE nhưng giữ editor chưa Lưu.
2. A1 xóa và đợi ack/DB commit.
3. A2 Lưu; đóng editor khi thấy đã xóa, UI giữ tombstone.
Bước kiểm tra backend:
1. Lane BE riêng DELETE =>200 tombstone V+1, xác nhận commit.
2. Sau đó PATCH stale =>409 MESSAGE_DELETED; GET history cùng tombstone.
Frontend mong đợi: Không body TOO-LATE xuất hiện sau event/response xóa.
Backend mong đợi: DELETE200; PATCH sau tombstone409 MESSAGE_DELETED; không chấp nhận VERSION_CONFLICT thay thế kỳ vọng cụ thể này.
Đối soát DB chỉ đọc:
- Delta version/outbox đúng1 từ DELETE.
- Không lưu body stale hoặc phát event sửa sau xóa.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T03-C03 — Reconnect sửa/xóa tin cũ đồng thời bù 101 tin mới
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- B đã tải cửa sổ cũ gồm OLD-EDIT và OLD-DELETE, lưu resume R0; Hub B offline.
- A sửa OLD-EDIT, xóa OLD-DELETE, gửi R101 sau R0.
Dữ liệu cụ thể: RECONNECT-001..101; limit=50; baseline IDs/version của hai tin cũ.
Bước kiểm tra frontend:
1. Ngắt mạng B bằng recipe giữ phiên hiện tại.
2. A thực hiện hai mutation cũ và gửi R101 bằng helper real API.
3. B online: bù đủ 101 tin, tải lại cửa sổ cũ, thấy body mới/tombstone; không chỉ append tin.
Bước kiểm tra backend:
1. GET after R0: 3 trang cùng throughSequence H1, 50/50/1, chỉ checkpoint cuối.
2. GET cửa sổ before/through cũ để đối chiếu version mới của OLD-EDIT và OLD-DELETE.
Frontend mong đợi: 101 ID mới đúng một lần; tin cũ được cập nhật, body đã xóa không còn.
Backend mong đợi: History mới đầy đủ; history cửa sổ cũ có edited version/tombstone hiện tại; không cung cấp bản body trước sửa.
Đối soát DB chỉ đọc:
- Đúng101 send operations mới; hai mutation cũ không tăng sequence.
- Mọi phiên đối chiếu cùng ID/version với DB; không giữ body cũ trong cache sau reconcile.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P5-T03-C04 — Event cũ và replay send không đảo ngược version
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Tin O1 ORIGINAL-RACE đã edit rồi delete; lưu DTO các version và tombstone hiện tại.
- Helper client test có thể giao event cũ/duplicate, ghi rõ synthetic-event khác live proof.
Dữ liệu cụ thể: Thứ tự nhận: tombstone V3, edit V2, create V1, duplicate V3.
Bước kiểm tra frontend:
1. A/B mở tombstone hiện tại.
2. Inject thứ tự event trên qua helper merge test, không sửa DB.
3. Replay original send qua BE; UI vẫn tombstone, không thêm dòng.
Bước kiểm tra backend:
1. POST đúng O1/content ORIGINAL-RACE =>200 DTO tombstone hiện tại.
2. GET history current; đối chiếu không có thêm DB mutation vì synthetic events hay replay.
Frontend mong đợi: Merge dùng ID/version chuỗi chính xác; thấp hơn hoặc trùng version không phục hồi body.
Backend mong đợi: Replay cùng original trả current tombstone; không response create DTO cũ.
Đối soát DB chỉ đọc:
- Delta message/operation/outbox/version/counter=0.
- Không body ORIGINAL-RACE hoặc body edit trở lại writer/log.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P5-T03-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P5-T03.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P5-T03; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P6-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P5-T03, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p6-t01"></a>

## DM P6 T01 ma trận quyền và giới hạn dữ liệu riêng

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P6-T01: ma trận quyền và giới hạn dữ liệu riêng.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P5-T03. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `test/dm-p6-t01-access-matrix`. AC-DM-09/12; ACL-02–05; TC-DM-12–14.
Branch cụ thể: test/dm-p6-t01-access-matrix. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P6-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P6-T01.1 Ma trận A/B/C/U/no token × search/open/list/history/send/edit/delete/subscribe/retry; kiểm tra từng đường auth không chỉ menu UI.
- P6-T01.2 C thử ID D-AB/message/cursor/Hub; B thử mutation A; không rò participants/body/email qua lỗi hoặc timing payload rõ rệt.
- P6-T01.3 Phiên stale/expired/unverified chuyển trạng thái trong fixture: validator và guard chặn; không tạo token bypass cho sản phẩm để test U.
- P6-T01.4 UI logout/login C trong cùng tab, response A muộn/GET lỗi/storage fault không hiện history của A hoặc fallback mock.
- P6-T01.5 Rà logs/outbox/DOM/storage, test auth/replay toàn đường và hồ sơ ma trận.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: C không thấy D-AB, biết URL/ID cũng không xem; B không sửa/xóa A; U login bị chặn. Đổi A→C không còn tin/nháp A kể cả request A về muộn.
Backend: Dùng tokens A/B/C hợp lệ gọi ma trận; U không có phiên là ca 401 riêng, không giả đó là đủ proof unverified. Runner isolated thay verified/state của phiên thử để kiểm tra validator/guard; C subscribe không nhận nội dung.
Điều kiện đạt: Mọi route/routing đúng quyền và không rò dữ liệu. Bạn PASS rồi cutoff/khóa peer.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P6-T01-C01 — Outsider biết ID vẫn không đọc/ghi/subscribe được
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-AB có SECRET-AB của A; C login riêng và chỉ thuộc D-AC.
- P0 đã chốt status/errorCode cụ thể cho từng outsider route.
Dữ liệu cụ thể: C biết D-AB.id/M_SECRET.id; không cấp membership để dựng test.
Bước kiểm tra frontend:
1. C mở inbox: không thấy D-AB.
2. C dán URL D-AB vào address bar; nhận trạng thái không có quyền/không tồn tại theo thiết kế, không thấy SECRET-AB.
3. C dùng helper gọi Hub subscribe D-AB; A gửi thêm SECRET-AFTER, C không nhận payload.
Bước kiểm tra backend:
1. Bearer C GET D-AB/messages; POST tin mới; PATCH/DELETE M_SECRET: mỗi request bị từ chối đúng contract đã chốt, không trả Message body.
2. C SubscribeConversation D-AB bị từ chối; A gửi hợp lệ; kiểm tra stream C không có MessageChanged của D-AB.
Frontend mong đợi: Không secret, participant/private preview hoặc draft của D-AB lọt vào UI C.
Backend mong đợi: REST/Hub deny nhất quán; không trả payload riêng dù biết ID; không dùng chỉ menu ẩn làm bằng chứng.
Đối soát DB chỉ đọc:
- Delta mutation do C=0; chỉ message mới hợp lệ của A làm counts tăng.
- Membership D-AB chỉ A/B, subscription C không được nhận dispatch.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T01-C02 — Member được đọc nhưng chỉ author được sửa/xóa
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- M_AUTH của A và M_B của B trong D-AB; C không thuộc.
- D-AC hợp lệ cho A/C; đã lấy cursor A của D-AC.
Dữ liệu cụ thể: B PATCH/DELETE M_AUTH; C dùng cursor của A tại D-AC.
Bước kiểm tra frontend:
1. B mở D-AB và đọc được M_AUTH.
2. B có menu Sửa/Xóa M_B của mình, không có các menu đó ở M_AUTH.
3. C mở D-AC hợp lệ; không thể dùng cursor của A để vượt ràng buộc actor.
Bước kiểm tra backend:
1. Bearer B GET D-AB/messages =>200 có M_AUTH; PATCH/DELETE M_AUTH với version đúng =>403.
2. Bearer C GET D-AC/messages với cursor A đã lấy =>400 CURSOR_INVALID; không thử ở resource C không có quyền để tránh che lỗi cursor bởi guard.
Frontend mong đợi: B vẫn đọc được; mutation trái author báo lỗi và giữ tin A; C không thấy dữ liệu từ cursor A.
Backend mong đợi: Read200; author-deny403; cross-actor cursor trên resource được phép400 CURSOR_INVALID.
Đối soát DB chỉ đọc:
- Content/version M_AUTH không đổi; không mutation-outbox.
- Cursor lỗi không làm DB thay đổi hoặc mở quyền đọc khác.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T01-C03 — Pending và phiên không hợp lệ không vượt guard
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- U chưa verify, chưa có ứng dụng session.
- Helper backend test có case actor/session đã tồn tại rồi đổi eligibility trong DB thử; không tạo bypass production.
Dữ liệu cụ thể: login U=dm_demo_pending; password DmDemo2026!Local; không tự cấp token U.
Bước kiểm tra frontend:
1. U đăng nhập qua form; thấy yêu cầu xác minh, không vào DM.
2. Thử route DM khi chưa login; quay về login, không dữ liệu nhạy cảm.
3. A/B vẫn dùng DM bình thường để chứng minh không tắt auth cả ứng dụng.
Bước kiểm tra backend:
1. POST /api/v1/auth/login U =>403 Identity.EmailNotVerified, không access/refresh token; GET DM không Bearer =>401.
2. Chạy recipe test session hợp lệ rồi actor không còn eligible; REST/subscribe/dispatch đều deny theo status đã chốt; token cũ không tiếp tục payload.
Frontend mong đợi: Pending không có inbox/composer nội bộ; cache/draft từ actor trước không xuất hiện.
Backend mong đợi: Không token401; pending-login403; guard actor hiện tại áp dụng cho HTTP và Hub, không chỉ kiểm JWT chữ ký.
Đối soát DB chỉ đọc:
- Không session ứng dụng mới cho login U bị từ chối.
- Không DM mutation/subscription được cấp cho actor mất eligibility.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T01-C04 — Đổi A sang C không nhận response/cache/draft A
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A đang mở D-AB, có draft DRAFT-PRIVATE-A; helper delay GET history A.
- C có phiên riêng; logout thật thu hồi A chứ không chỉ xóa storage.
Dữ liệu cụ thể: GET A bị giữ rồi thả sau logout A/login C.
Bước kiểm tra frontend:
1. A mở D-AB, nhập draft không gửi; bắt đầu GET bị delay.
2. Logout A, login C cùng tab, mở D-AC.
3. Thả response A; UI C không hiện SECRET-AB/DRAFT-PRIVATE-A, kể cả quay lại route cũ.
Bước kiểm tra backend:
1. Replay GET bằng đúng access token A trước logout =>401; C GET inbox/history hợp lệ =>200 chỉ phạm vi C.
2. Kiểm tra local/session storage và cache memory qua recipe dev/test: không body/draft/token A phục hồi; không in token.
Frontend mong đợi: Không lộ nội dung khi response cũ về muộn; draft A bị cleanup theo actor.
Backend mong đợi: Old token401; token C không được đọc D-AB; delayed-response bị bỏ theo actor generation.
Đối soát DB chỉ đọc:
- Không mutation bởi nhập draft/đổi route.
- Logout marker/session revocation đúng; data message đã gửi của A không bị xóa.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P6-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P6-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P6-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P6-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P6-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p6-t02"></a>

## DM P6 T02 thu hồi phiên khóa tài khoản và deadline

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P6-T02: thu hồi phiên khóa tài khoản và deadline.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P6-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p6-t02-session-revocation`. DEC-083/104; TC-DM-26; contract guard/read/send/author-mutation.
Branch cụ thể: feat/dm-p6-t02-session-revocation. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P6-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P6-T02.1 Session reader/revoker theo session/user, registry cleanup và auth expiry; event sau commit + reconciliation tối đa 1 giây theo thiết kế; fail closed nếu đọc quyền lỗi.
- P6-T02.2 Fixture helper K disabled/unlock chỉ `_test`, ghi status/stamp/revoke và receipt kiểm thử; không public admin API, không nhầm lockout 15 phút với disabled.
- P6-T02.3 Guard read/author mutation/send mới tách purpose: K disabled vẫn cho A đọc/sửa/xóa tin A, chặn send mới tới K; retry key đã commit trả current nếu A còn quyền.
- P6-T02.4 Race revoke với send/subscribe/dispatch giữ UoW/lock order; hết phiên/logout-all không reconnect bằng token cũ.
- P6-T02.5 Timestamp commit/cutoff ≤5 giây, fault authority/worker, UI status và scripts/DB proof.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A1 thu hồi A2 từ UI sessions: A2 dừng nhận dữ liệu; logout-all không tự phục hồi. A–K đã có history, khóa K: A còn đọc/sửa/xóa tin mình nhưng không gửi mới; unlock K cần login mới.
Backend: Token A2 cũ HTTP/Hub bị chặn; runner đo commit→last delivery, không chỉ event AccessRevoked; race không có mutation sau revoke theo thứ tự guard. Old-key retry với peer disabled chỉ đọc kết quả, không tạo operation mới.
Điều kiện đạt: Deadline đạt qua dữ liệu thực ở server/browser; thiếu công cụ khóa hoặc timestamp proof là Bị chặn, không đánh dấu PASS toàn task.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P6-T02-C01 — Thu hồi A2, ngừng payload trong tối đa 5 giây
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1/A2 là hai login session độc lập; B liên tục gửi TEST-REVOKE-001... mỗi 200ms, A2 subscribed.
- Ghi sessionId A2; đồng hồ/trace xác định DB commit t0 và payload receipt.
Dữ liệu cụ thể: A1 DELETE /api/v1/auth/sessions/<A2.sessionId>; stream bắt đầu 5s trước và tiếp tục ít nhất 15s sau t0.
Bước kiểm tra frontend:
1. A2 mở D-AB; A1 mở danh sách phiên chọn đúng A2.
2. A1 thu hồi A2 khi stream B đang chạy; A2 chuyển trạng thái phiên hết hiệu lực.
3. Ghi thời điểm payload cuối; A1 vẫn nhận/đọc bình thường; không chỉ chụp AccessRevoked.
Bước kiểm tra backend:
1. Bearer A1 DELETE session A2 =>204; trace DB commit t0.
2. Dùng đúng token cũ A2 GET/POST/subscribe =>401/Hub deny; đối soát stream: không payload sau t0+5s, A1 vẫn200.
Frontend mong đợi: A2 không tiếp tục body mới quá deadline; UI bỏ dữ liệu theo policy; A1 không bị logout nhầm.
Backend mong đợi: HTTP A2 bị reject sau commit; dispatch deadline≤5s; lượng gửi đủ để chứng minh không phải tình cờ không có event.
Đối soát DB chỉ đọc:
- Revocation đúng session A2, A1 còn active.
- Không mutation A2 sau revoke; log chỉ ID/timestamp, không raw token/body.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Dừng stream; login A2 mới nếu cần task khác, không phục hồi token cũ. Giữ timeline t0/receipt/clock uncertainty; nếu sai số không đủ kết luận thì Bị chặn.

TEST CASE DM-P6-T02-C02 — Logout-all không làm sống lại phiên cũ
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A có A1/A2, mỗi phiên có access/refresh token lưu ngoài Git.
- B online và có D-AB để thử gửi sau revoke.
Dữ liệu cụ thể: POST /api/v1/auth/logout-all bằng A1; sau đó login mới A3.
Bước kiểm tra frontend:
1. A1 logout tất cả; A2 nhận trạng thái hết phiên.
2. B gửi một tin sau thời điểm revoke; A2 không nhận body mới.
3. A đăng nhập lại A3, đọc được tin đã gửi, A1/A2 cũ vẫn vô hiệu.
Bước kiểm tra backend:
1. POST /api/v1/auth/logout-all =>204; đúng token A1/A2 cũ GET /users/me =>401; thử refresh cũ bị từ chối theo Identity contract.
2. Login A3 =>200 phiên mới, GET history200; token A1/A2 tiếp tục401, Hub cũ không hồi sinh.
Frontend mong đợi: Phiên mới hoạt động; phiên cũ không được tự hồi phục/replay mutation.
Backend mong đợi: Revoke tất cả phiên tồn tại lúc commit; refresh family cũ không cấp lại session; deadline stream≤5s.
Đối soát DB chỉ đọc:
- Old sessions/family markers revoked được giữ.
- A3 là session mới khác ID; không thay message hoặc reset dedup keys.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T02-C03 — Khóa K: chặn send mới, vẫn giữ read và quyền author của A
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- D-AK tạo khi K active; A đã gửi O_K ORIGINAL-K và một tin A khác để sửa/xóa.
- Helper chỉ DB thử khóa K và revoke đúng stamp/session; A vẫn active.
Dữ liệu cụ thể: Helper disable K; O_K lấy từ request gốc; NEW-K dùng UUID mới.
Bước kiểm tra frontend:
1. A mở D-AK, helper khóa K; UI báo không gửi mới được.
2. A vẫn đọc lịch sử, sửa/xóa tin do A viết.
3. A retry thao tác O_K đã commit: nhận current DTO; helper mở K, K phải login mới.
Bước kiểm tra backend:
1. Bearer A POST NEW-K khi K disabled: deny đúng status/errorCode P0; GET history200, author PATCH/DELETE200.
2. Replay đúng O_K/body gốc =>200 current DTO, không insert; K token trước khóa401 ngay cả sau mở; login mới K theo contract200.
Frontend mong đợi: Không mất history khi peer bị khóa; không che quyền author bằng trạng thái peer; không tạo tin mới.
Backend mong đợi: Send mới kiểm peer; replay operation đã commit được đọc hiện trạng sau guard actor/member; read/author mutation không yêu cầu peer active.
Đối soát DB chỉ đọc:
- Không operation/message cho NEW-K; replay không tăng counter.
- Status/stamp K thay đổi đúng, old session revoked; edit/delete của A có đúng versions.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Mở K bằng helper thử có kiểm status/stamp/revoke; đăng nhập phiên mới. Không dùng API admin công khai để reset.

TEST CASE DM-P6-T02-C04 — Thứ tự commit revoke và lỗi authority đều không cấp quyền giả
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Recipe barrier test có shared UoW/guard lock và fault authority timeout.
- Mới ghi baseline N/counts, chưa có message TEST-AUTHORITY.
Dữ liệu cụ thể: Nhánh 1: revoke COMMIT trước thả writer; nhánh 2: writer COMMIT trước revoke; nhánh 3: authority unavailable.
Bước kiểm tra frontend:
1. Giữ send ở barrier trước kiểm quyền rồi revoke session, thả writer; UI báo hết phiên.
2. Ở run riêng cho send commit trước revoke, UI nhận ack nhưng sau đó dừng phiên.
3. Bật authority fault và thử đọc/subscribe/gửi: thấy lỗi, không dữ liệu mới; tắt fault.
Bước kiểm tra backend:
1. Chạy từng nhánh recipe độc lập; revoke-first request401/no write; writer-first một200 đã commit hợp lệ rồi revoke.
2. Authority-fault trả đúng failure contract đã chốt (không fallback allow); đo stream no payload sau deadline, không auto mutation replay.
Frontend mong đợi: Không hiển thị tin chưa commit hoặc lộ body khi không xác định được authority.
Backend mong đợi: Ordering chứng minh transaction/guard; lỗi authority fail closed; trạng thái/errorCode từng fault được chốt trước bàn giao.
Đối soát DB chỉ đọc:
- Revoke-first delta message=0; writer-first delta=1.
- Authority-fault delta=0; không cấp subscription/dispatch khi guard không xác nhận.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P6-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P6-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P6-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P6-T03, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P6-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p6-t03"></a>

## DM P6 T03 mobile IME draft và cache theo actor

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P6-T03: mobile IME draft và cache theo actor.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P6-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p6-t03-composer-and-drafts`. AC-DM-17/20; TC-DM-19/22; UX-DM.
Branch cụ thể: feat/dm-p6-t03-composer-and-drafts. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P6-T03.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P6-T03.1 Desktop Enter gửi/Shift+Enter xuống dòng, mobile Enter xuống dòng, Send luôn gửi; composition IME không gửi sớm.
- P6-T03.2 Map nháp theo actor/conversation trong bộ nhớ, giữ khi chuyển D-AB↔D-AC; reload/logout/actor switch xóa, không localStorage/IndexedDB nội dung.
- P6-T03.3 Tách draft chưa gửi khỏi failed operation; sent vẫn DB, failed không tự replay; request muộn không phục hồi cache sau logout.
- P6-T03.4 Layout wide/narrow, keyboard/focus/errors bằng chữ, load older/retry/edit/delete sử dụng được; không thêm developer controls vào flow người dùng.
- P6-T03.5 E2E với IME/multiple profiles/mobile viewport và lượt kiểm tra thiết bị thật được ghi rõ; không coi giả viewport là đủ proof bàn phím mobile.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Nhập tiếng Việt IME/emoji/nhiều dòng; các phím đúng desktop và điện thoại; chuyển DM giữ nháp, reload/logout mất nháp; A/B không chia sẻ draft, C không nhận draft A.
Backend: Network: composition/chuyển DM/reload không có POST bất ngờ; send payload đúng LF/UTF-16; số message tăng đúng thao tác Gửi, không tăng vì auto reconnect; DB history còn sau reload.
Điều kiện đạt: FE thiết bị/thao tác và BE counts/payload cùng đạt. Browser/OS/thiết bị thực tế ghi trong biên bản; ma trận toàn v1 kiểm tra tiếp ở P8.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P6-T03-C01 — Desktop Enter, Shift+Enter và IME đúng ý định
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Chrome desktop bản ghi rõ; A mở D-AB; B online.
- Có bộ gõ thật hỗ trợ composition (ví dụ Japanese IME), không chỉ dispatch key event giả.
Dữ liệu cụ thể: Tin 1 "DESKTOP-ENTER"; tin 2 "Dòng IME 1\n日本語".
Bước kiểm tra frontend:
1. Nhập DESKTOP-ENTER, Enter: đúng một POST và một tin.
2. Nhập Dòng IME 1, Shift+Enter: xuống dòng, không POST.
3. Gõ 日本語 ở composition, Enter xác nhận IME: chưa gửi; sau kết thúc composition Enter gửi một tin chứa đủ hai dòng.
Bước kiểm tra backend:
1. GET history đối chiếu hai ID/content với Network.
2. Replay từng body/UUID đã commit =>200 cùng ID, không tạo thêm; đếm Network mutations riêng, không dùng replay để đánh giá Enter.
Frontend mong đợi: Enter desktop gửi; Shift+Enter và Enter lúc composing không gửi sớm; tiếng Nhật không mất ký tự.
Backend mong đợi: Đúng2 send commits mới per lane, body newline LF và Unicode nguyên vẹn.
Đối soát DB chỉ đọc:
- Delta message/operation/counter=2.
- Không bản text chưa composition hoặc send trùng.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T03-C02 — Mobile Enter xuống dòng, nút Gửi mới gửi
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Thiết bị Android Chrome và iPhone Safari thật, ghi OS/browser/bàn phím.
- A mở D-AB; recipe hỗ trợ quan sát Network/server trace theo IDs.
Dữ liệu cụ thể: MOBILE-ENTER dòng1, dòng2; thử bàn phím tiếng Việt/emoji.
Bước kiểm tra frontend:
1. Trên mỗi thiết bị nhập dòng1, Enter: thêm newline không send.
2. Nhập dòng2 có tiếng Việt/emoji, đóng/mở bàn phím kiểm composer không mất draft.
3. Bấm Gửi một lần: đúng một Message mỗi thiết bị; B đối chiếu hai tin.
Bước kiểm tra backend:
1. GET history theo ID bắt từ response và compare content newline LF.
2. Đếm POST/new operations theo device lane, không lấy viewport desktop làm bằng chứng mobile IME.
Frontend mong đợi: Enter mobile không gửi; nút Gửi dễ thao tác khi bàn phím mở, content đủ.
Backend mong đợi: Mỗi device lane một200 Message, không duplicate; server validation đúng UTF-16.
Đối soát DB chỉ đọc:
- Delta=1 message/operation per device lane.
- Không mutation chỉ vì Enter/đóng bàn phím.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T03-C03 — Draft nhớ theo hội thoại trong tab, mất sau reload/logout
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A mở D-AB/D-AC chưa gửi; chụp counter và message count.
- Không dùng localStorage/sessionStorage/IndexedDB để giữ body draft.
Dữ liệu cụ thể: Draft AB="NHAP-RIENG-AB"; AC="NHAP-RIENG-AC".
Bước kiểm tra frontend:
1. Nhập draft AB, chuyển AC: không thấy AB; nhập draft AC.
2. Quay AB thấy đúng draft AB; quay AC thấy draft AC; không POST.
3. Reload: cả draft mất; nhập lại draft rồi logout/login: draft không trở lại.
Bước kiểm tra backend:
1. Quan sát Network từ nhập/chuyển/reload/logout, không POST messages.
2. GET history trước/sau + đọc storage bằng recipe: không body draft, không tin mới, chỉ logout đúng request Identity.
Frontend mong đợi: Nhớ draft trong RAM theo conversation; reload/logout cleanup; history đã gửi vẫn đọc được.
Backend mong đợi: Không send operation cho draft; không tự gửi khi route/reload/login.
Đối soát DB chỉ đọc:
- Delta messages/send_operations/counter/outbox=0.
- Session revoke logout đúng; dữ liệu tin đã gửi không bị cleanup nhầm.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P6-T03-C04 — Draft/cache tách actor và tab, response muộn không hồi phục
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- A1/A2 login độc lập trong profile/tab khác; A1 có draft PRIVATE-A1, A2 không có.
- Helper giữ GET history A1; C sẽ login cùng tab A1 sau logout.
Dữ liệu cụ thể: A1 draft D-AB; A2 draft riêng SECOND-TAB; delayed GET A1.
Bước kiểm tra frontend:
1. A1 nhập PRIVATE-A1; A2 mở cùng DM không thấy draft A1, nhập SECOND-TAB.
2. A1 logout/login C; C không thấy draft/cache A; thả GET A1 cũ.
3. A2 vẫn phiên A2/draft SECOND-TAB nếu chỉ logout A1, C reload vẫn không hiện nội dung A.
Bước kiểm tra backend:
1. GET với token A1 cũ401; A2 GET200 và C GET phạm vi C200.
2. Đối soát generation token/cache keys và storage không body draft; trace delayed-response bị bỏ, không mutation tự động.
Frontend mong đợi: Không đồng bộ draft giữa tab/actor; revoke riêng A1 không logout nhầm A2; stale response không nạp lại cache.
Backend mong đợi: Guards đúng session/actor; không send do switch/reconnect.
Đối soát DB chỉ đọc:
- Delta messages/operations=0.
- A1 revoked, A2 còn active; membership không đổi.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P6-T03-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P6-T03.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P6-T03; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P7-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P6-T03, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p7-t01"></a>

## DM P7 T01 regression bản tích hợp và bàn giao

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P7-T01: regression bản tích hợp và bàn giao.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P6-T03. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `test/dm-p7-t01-internal-acceptance`. AC-DM-01–21, TC-DM-01–26, TC-TEXT-01–06 trong phạm vi DM; không giả gửi tin phòng đã đạt vì dùng validator chung.
Branch cụ thể: test/dm-p7-t01-internal-acceptance. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P7-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P7-T01.1 Chốt commit/config/schema/dataset, rebuild từ checkout sạch, migrations fresh/existing, key ring/HMAC bền qua restart, proxy Web–API–Hub thật.
- P7-T01.2 Regression mọi task trên build chung; kiểm tra message/operation/outbox/version/tombstone, no old body và không tự hết hạn history.
- P7-T01.3 Happy path A/B, outsider C, pending U, disabled K; offline, 101 tin, worker restart, mutation race, cursor/keys/revoke và browser thật.
- P7-T01.4 Chứng minh restart ứng dụng không mất DB/key/cursor trong hạn; log không secrets/body; retention chưa triển khai không được tự purge marker/history.
- P7-T01.5 Hồ sơ mẫu task/release, lỗi tồn, known limits/email dependency/P8; bàn giao commands và dataset cho người dùng tự chạy từ đầu.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: A tìm B→mở DM→gửi→B nhận→reload→sửa→xóa→offline/reconnect; repeat trên narrow/mobile; C/U/K đúng hành vi; tự restart rồi mở history.
Backend: Chạy collection + SQL read-only trên cùng build, compare IDs/counts/current body, auth negative/race/cutoff; runtime qua proxy. Fail case không được bỏ khỏi report.
Điều kiện đạt: Bạn PASS gói DM văn bản nội bộ, xác nhận build và lỗi tồn. P8 chỉ thực hiện khi được giao tiếp; email thật, restore và tải còn mở phải hiện trong kết luận, không mở công khai từ PASS P7.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P7-T01-C01 — Luồng tích hợp tìm người đến tombstone bằng FE thật
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Run mới A/B/C đã login; pair A-B chưa có DM; C outsider.
- Task P0–P6 đã được người dùng xác nhận trên baseline tương ứng.
Dữ liệu cụ thể: Search dm_demo_bao; content="INTERNAL-E2E"; edit="INTERNAL-EDIT".
Bước kiểm tra frontend:
1. A tìm đúng username B, mở DM; B mở inbox thấy cùng conversation ID.
2. A gửi INTERNAL-E2E; B nhận không reload; A sửa INTERNAL-EDIT, B thấy version mới.
3. A xóa, A/B reload thấy tombstone; C mở ID trực tiếp bị từ chối.
Bước kiểm tra backend:
1. POST lại open A/B và B/A =>200 cùng ID; không tạo pair mới.
2. GET history A/B cùng tombstone ID/sequence/version3; C REST/Hub deny đúng contract.
Frontend mong đợi: Search/inbox/send/edit/delete/reload chạy nối tiếp thật, không mock; C không dữ liệu riêng.
Backend mong đợi: Open idempotent; create version1, edit2, delete3; history DTO hiện trạng; guards đúng.
Đối soát DB chỉ đọc:
- Đúng1 conversation/cặp,2members,1message,1operation; counter tăng1.
- Đúng3 mutation events metadata-only; body cũ không được lưu sau sửa/xóa.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P7-T01-C02 — Mất response rồi reconnect không trùng hoặc mất sửa/xóa
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- B offline sau baseline R0 và đã tải một tin cũ OLD-P7.
- Recipe drop HTTP response sau commit cho một tin A; R101 chạy API thật.
Dữ liệu cụ thể: FAILED-ACK-P7 original UUID O_P7; OLD-P7 bị sửa/xóa; 101 tin RECONNECT.
Bước kiểm tra frontend:
1. A gửi FAILED-ACK-P7, response bị mất: UI failed/unknown; mạng lại không tự retry.
2. B offline, A sửa/xóa OLD-P7 rồi gửi R101; B online bù3trang và reload cửa sổ cũ.
3. A bấm Retry chính O_P7; đối soát A/B đúng một FAILED-ACK-P7 và 101 tin mới.
Bước kiểm tra backend:
1. POST replay đúng O_P7/body =>200 cùng ID đã commit.
2. GET history sau R0 với đúng fixed frontier, đủ mọi ID phát sinh; OLD-P7 là tombstone hiện tại; không suy ra tổng delta chỉ101 nếu FAILED-ACK cũng sau R0.
Frontend mong đợi: Không tự send; explicit retry không duplicate; không body cũ sau reconnect.
Backend mong đợi: Mọi thao tác xác minh bằng actual HTTP/DB; cursor cuối chỉ sau all pages; versions monotonic.
Đối soát DB chỉ đọc:
- Delta messages =101+1 nếu FAILED-ACK cũng trong run mới; replay delta0.
- Edit/delete tin cũ không tăng counter; outbox recovery không lưu body.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P7-T01-C03 — Dựng mới và nâng cấp baseline giữ dữ liệu/key
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Hai DB thử riêng: fresh và upgrade có messages/operations/cursors từ baseline.
- Backup và key ring/HMAC lưu ngoài Git; không dùng DB ứng dụng chính.
Dữ liệu cụ thể: Chụp IDs/counts/versions/checkpoint trước migration và restart.
Bước kiểm tra frontend:
1. Fresh checkout/build startup, A/B chạy smoke search/open/send/history.
2. DB upgrade chạy migration theo lệnh bàn giao, mở FE đọc tin có sẵn.
3. Restart API/worker/FE, history vẫn đọc; replay original operation từ trước upgrade không tạo thêm.
Bước kiểm tra backend:
1. GET history trước/sau upgrade/restart cùng IDs/version; cursor chưa hết hạn vẫn được chấp nhận theo binding.
2. POST replay operation cũ cùng body =>200 same current ID; kiểm migration history/schema tương ứng source.
Frontend mong đợi: Fresh và upgrade không rỗng giả hoặc mất body đã commit; không lỗi cursor vì key ring mất khi restart.
Backend mong đợi: Schema upgrade hoạt động; persistent keyring/HMAC old-key lookup; không reset counters.
Đối soát DB chỉ đọc:
- Counts/IDs/versions trước upgrade giữ nguyên trừ dữ liệu smoke được ghi rõ delta.
- Migration records đúng; không drop tables/volumes để che lỗi.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Dừng đúng Compose project thử, giữ volumes/keyfiles/backup. Fresh và upgrade ghi hai bộ kết quả riêng.

TEST CASE DM-P7-T01-C04 — Ma trận nghiệm thu có trace, chưa tuyên bố toàn v1
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Có inventory AC/TC DM hiện hành và kết quả ca từng task.
- Ghi môi trường thật/mock/fixture/thiết bị, build đã test; thiếu dependency phải Bị chặn.
Dữ liệu cụ thể: Map từng AC-DM/TC-DM hiện hành tới task/case/proof; không dùng tổng số dự đoán thay đọc docs.
Bước kiểm tra frontend:
1. Người dùng chạy lại happy path A/B và các ca C-deny, lỗi/retry trên build tích hợp.
2. Chạy mobile composer/IME trên thiết bị thật; kiểm draft đổi actor.
3. Ghi từng kết quả trong biên bản, không gộp thành PASS khi còn ca bắt buộc chưa chạy.
Bước kiểm tra backend:
1. Chạy backend regression PostgreSQL, FE build/unit/E2E đúng scope; lưu command/exit/result.
2. Đối chiếu coverage từng AC/TC với case IDs, source commit và DB proof; ghi thiếu Community/email/load/restore tương ứng gate P8/v1.
Frontend mong đợi: Người dùng có hướng dẫn tái lập và nhìn được điểm lỗi; acceptance nội bộ có phạm vi cụ thể.
Backend mong đợi: Ca bắt buộc có bằng chứng; không dùng mocked HTTP thay BE integration; không publish/merge từ kết quả test tự động.
Đối soát DB chỉ đọc:
- Regression không chạy vào DB chính.
- Counts fixture/run traceable; không secret/body trong log; không chỉnh trạng thái PASS thay người dùng.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P7-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P7-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P7-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P8-T01, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P7-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p8-t01"></a>

## DM P8 T01 bảo vệ dữ liệu khi restore và retention

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P8-T01: bảo vệ dữ liệu khi restore và retention.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P7-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `feat/dm-p8-t01-data-protection`. DEC-106–109; AC-DATA/TC-DATA và Message.contentState trong đặc tả. Phụ thuộc: chọn kho sổ/key/checkpoint và quy trình vận hành tại OQ-011/OQ-008; thiếu đầu vào thì chuẩn bị phương án cụ thể và chờ quyết định cần thiết.
Branch cụ thể: feat/dm-p8-t01-data-protection. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P8-T01.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P8-T01.1 Thiết kế/triển khai sổ bảo vệ độc lập chỉ ID/version/revocation markers; prepared/DB commit/receipt/abort, idempotency/watermark, đối soát trạng thái unknown; outbox ghi sau đơn thuần không đủ.
- P8-T01.2 Mutation cần bảo vệ không bypass khi kho sổ lỗi; receipt mất có kết quả chưa rõ đúng contract; cursor/keys/dedup/deny marker phục hồi tương thích.
- P8-T01.3 Restore DB cũ trong môi trường riêng rồi áp floor/tombstone/thu hồi trước mở; bản sửa mới mất không phục vụ body cũ, trả unavailable_after_restore/content null và UI “Nội dung chưa khôi phục được”.
- P8-T01.4 Writer author được viết body mới hoặc xóa placeholder theo version/quyền; cleanup terminal/outbox theo policy, giữ active families/dedup, log14/audit90/IP7 ngày đúng phạm vi sở hữu.
- P8-T01.5 Diễn tập fault từng bước, sổ/key/checkpoint thiếu, prepared chưa rõ và legacy retry; backup tuổi30 ngày, RPO≤15 phút/RTO≤4 giờ đo cùng hạ tầng liên quan, không restore đè môi trường đang dùng.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Trước backup A có M01; sau backup sửa/xóa/thu hồi; restore riêng: deleted không hồi sinh, edited mất bản mới là placeholder, old session bị chặn; author viết mới/tombstone theo quyền.
Backend: So sổ/DB/watermark và current versions, replay O1 không dựng body cũ; thiếu sổ/unknown giữ scope chưa mở; đo RPO/RTO và kiểm tra artefact age. Cleanup không xóa marker đang còn cần.
Điều kiện đạt: Restore/protection có proof cả UI/API/data; không tự ký đạt khi chưa chọn kho sổ hoặc không đối soát được trạng thái.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P8-T01-C01 — Restore trước edit: không hồi sinh body cũ
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Backup thử tại tB chứa M_RESTORE body BEFORE-BACKUP version1.
- Sau backup A sửa AFTER-BACKUP thành version2; journal ngoài backup giữ protection floor, thử trên DB restore riêng.
Dữ liệu cụ thể: tB < tEdit; journal committed floor M_RESTORE/version2; edited body mới không có trong backup.
Bước kiểm tra frontend:
1. A/B mở app restore chỉ sau reconcile protection hoàn tất.
2. Tìm M_RESTORE: thấy nội dung không khả dụng sau khôi phục, không BEFORE-BACKUP, không tombstone xóa.
3. A nhập nội dung NEW-AFTER-RESTORE bằng edit hợp lệ; B thấy hiện trạng mới.
Bước kiểm tra backend:
1. Trước mở traffic, replay journal edit floor; GET history M_RESTORE =>content=null, contentState=unavailable_after_restore, deletedAt=null, version>=2.
2. PATCH expectedVersion hiện trạng, content NEW-AFTER-RESTORE =>200/new version; GET cùng ID/sequence không lộ BEFORE-BACKUP.
Frontend mong đợi: Không hiển thị body từ backup đã bị sửa; placeholder đúng nghĩa, author có thể sửa/xóa.
Backend mong đợi: Floor ngăn stale content; không giả deletedAt cho edit; bản body sau edit chỉ có khi recovery point thật sự có nguồn hợp lệ.
Đối soát DB chỉ đọc:
- Restored message giữ ID/sequence, version không thấp hơn floor; old body bị loại trước read.
- Journal chỉ IDs/version/status, không AFTER-BACKUP/BEFORE-BACKUP plaintext.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Giữ DB gốc và backup thử; chỉ dừng restore stack. Không restore đè app chính.

TEST CASE DM-P8-T01-C02 — Restore trước delete/revoke giữ tombstone và phiên bị thu hồi
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Backup tại tB trước DELETE M_DEL và revoke session A2.
- Sau tB hai mutation committed được ghi journal độc lập; original SendOperation payload/UUID đã lưu.
Dữ liệu cụ thể: tB < tDelete,tRevoke; A2 old token giữ ngoài Git.
Bước kiểm tra frontend:
1. Khởi động restore staging với traffic bị khóa tới khi apply floors.
2. A1/B mở history: M_DEL tombstone, không body từ backup.
3. A2 dùng đúng phiên cũ: bị deny; đăng nhập mới nếu hợp lệ là phiên khác.
Bước kiểm tra backend:
1. Replay original send UUID/body M_DEL =>200 current tombstone không insert.
2. Old A2 token GET401/Hub deny sau restore; kiểm security markers trước ready, không dùng login mới để chứng minh token cũ vô hiệu.
Frontend mong đợi: Body xóa không xuất hiện dù backup chứa nó; phiên revoke không sống lại.
Backend mong đợi: Tombstone version>=delete floor; dedup/revoke markers survives restore; replay original không tạo message mới.
Đối soát DB chỉ đọc:
- Content M_DEL null, deletedAt đúng trạng thái xóa, SendOperation giữ.
- Session/family revoke floor áp dụng trước ready và được enforce dispatch.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T01-C03 — Journal unknown hoặc thiếu phạm vi: khóa đọc, không suy đoán
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Recipe journal prepared trước writer commit, committed/aborted và unknown crash; journal recovery point độc lập DB backup.
- Status/errorCode maintenance/fail-closed đã chốt cụ thể trong contract vận hành trước bàn giao.
Dữ liệu cụ thể: Fault1 commit DB rồi mất finalize; fault2 prepared rồi rollback; fault3 mất journal scope.
Bước kiểm tra frontend:
1. A/B mở scope unknown lúc restore; thấy trạng thái chưa sẵn sàng, không body cũ.
2. Chạy reconcile với nguồn authoritative: committed apply floor, aborted mới cho phép bỏ protection đúng.
3. Mất journal scope: tiếp tục blocked, không bấm bỏ qua bảo vệ để mở chat.
Bước kiểm tra backend:
1. GET/messages và subscribe scope unknown bị deny theo status đã chốt, không trả payload; không coi bất kỳ503/403 là PASS.
2. Đối chiếu independent journal persistence và DB transaction receipts để phân loại committed/aborted; nếu không chứng minh được giữ unknown/closed.
Frontend mong đợi: Không body từ trạng thái không biết; UI lỗi có khả năng thử lại sau reconcile, không gửi tự động.
Backend mong đợi: Không lấy outbox async làm nguồn journal bảo vệ duy nhất; unknown/missing không fallback allow.
Đối soát DB chỉ đọc:
- Floor/protection status đúng từng nhánh; no body trong journal.
- Không ready/open scope unknown; không tạo mutation do người dùng thử lại GET.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T01-C04 — Retention và đo RPO/RTO không xóa marker còn hiệu lực
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- DB/backup thử có clock-controlled timestamps; quy tắc lifecycle đã đọc theo docs.
- Có artifacts tuổi7/14/30/90ngày và active refresh family, send dedup/revoke markers.
Dữ liệu cụ thể: Detail terminal≥7d; log≥14d; audit≥90d; IP/user-agent thô của audit≥7d; backup artifact tuổi tối đa30d. Tạo bộ trước/đúng/sau retain_until UTC; không áp TTL IP audit lên phiên active.
Bước kiểm tra frontend:
1. Trước cleanup A/B mở history/tombstone và lưu snapshot.
2. Chạy cleanup recipe thử với clock injection, mở lại history và login/retry.
3. Chạy restore drill từ backup phù hợp; ghi downtime thực đo thay lời hứa.
Bước kiểm tra backend:
1. Kiểm TTL before/at/after từ docs, lệnh preview/count rồi chạy scoped cleanup.
2. Replay original operation và old revoked token sau cleanup: same current DTO và401; đo mất dữ liệu≤15min, phục hồi≤4h bằng timestamp và artifacts.
Frontend mong đợi: History/tombstone và quyền hiện tại còn đúng sau cleanup; không hồi sinh body/phiên.
Backend mong đợi: RPO≤15min/RTO≤4h được đo; giữ active families và markers có hiệu lực, không TTL mù toàn bảng.
Đối soát DB chỉ đọc:
- Detail/log/audit/IP/artifacts hết hạn được cleanup đúng policy; marker cần bảo vệ không mất.
- Lưu counts trước/sau/TTL boundary/backup inventory; không chứa secret/body trong report.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Khôi phục clock test, không thay clock máy/production; lưu báo cáo và restore artifacts hợp lệ.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P8-T01-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P8-T01.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P8-T01; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P8-T02, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P8-T01, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p8-t02"></a>

## DM P8 T02 tải trình duyệt và observability

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P8-T02: tải trình duyệt và observability.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P8-T01. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `perf/dm-p8-t02-load-and-observability`. DEC-082/083 và [quality targets](../release-operations.md#quality-targets). Mục tiêu toàn chat cần Community workload, không dùng DM-only thay toàn benchmark.
Branch cụ thể: perf/dm-p8-t02-load-and-observability. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P8-T02.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P8-T02.1 Chốt workload/thiết bị/resources/clock/sample; chuẩn bị synthetic load và metrics không body/token; nhãn riêng validation/fault và lỗi hệ thống.
- P8-T02.2 Đo DM send→save→recipient, API/backlog/connections, error/correctness, p50/p95/p99 và cutoff; fixture gần 100.000 tin khi scope benchmark đã chọn.
- P8-T02.3 Chạy DM load trước, tích hợp 100 online/60 phút sau warm-up 5 phút theo workload toàn chat khi Community sẵn sàng; API/gửi→lưu p95≤500 ms, commit→UI online p95≤1 giây, lỗi dịch vụ <1% và thu hồi≤5 giây theo docs, không tự giảm target.
- P8-T02.4 Desktop/mobile browser matrix theo phiên bản ổn định tại nghiệm thu; recording build/OS/device/network, IME/cursor/UI under load.
- P8-T02.5 Alert backlog/error/revoke, dashboards/report, user recipe vừa quan sát UI vừa chạy runner và đối chiếu IDs DB.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Trong khi runner load chạy, A/B thao tác send/edit/delete/history/reconnect trên browser thật; trạng thái không sai, không duplicate và UI dùng được.
Backend: Review raw aggregates/sample/clock/workload và DB correctness, thử alert; benchmark DM đạt không tự đánh dấu toàn workload Community đạt. Mọi target bắt buộc thiếu proof giữ chưa đạt/chưa đánh giá.
Điều kiện đạt: Bạn xác nhận phạm vi DM đã đo; gate toàn chat chỉ PASS khi Community tích hợp và ngưỡng toàn bộ đạt.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P8-T02-C01 — Đo tải đúng scope và các percentile từ timestamp thật
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Seed thử đề xuất1000users/500DM/20communities/50rooms/100kpreload; ghi cấu hình máy và commit.
- Benchmark chat v1 cần Community tương ứng; nếu chỉ DM thì báo riêng và giữ gate whole-chat Bị chặn.
Dữ liệu cụ thể: Baseline whole-chat: 100 online gồm80người/40DM và20người ở phòng được phép; mỗi người1tin/10s (~10tin/s, ~36000tin/60min), history1trang/30s, author edit/delete1tin/5min. Warmup5min + đo60min; mỗi send UUID mới. Seed1000users/500DM/20communities/50text rooms/100000tin; ít nhất2hội thoại/phòng có≥10000tin.
Bước kiểm tra frontend:
1. Trong tải A/B FE thật gửi và quan sát nhận một tập tin đo có ID trace.
2. Ghi thao tác gửi, HTTP ack, DB commit, thời điểm UI B merge/render; dùng đồng hồ đã hiệu chỉnh.
3. Mở history/inbox/search trong tải để ghi lỗi/độ trễ người dùng, không dùng load-client chỉ server thay UI nhận.
Bước kiểm tra backend:
1. Chạy load recipe với args/concurrency/mix/duration/seed rõ, lưu raw samples đã lọc sensitive.
2. Tính riêng p95 API request→response≤500ms, thao tác send→DBcommit≤500ms và DBcommit→online UI merge/render≤1s; unexpected5xx/timeout<1% trên request hợp lệ. Báo p50/p95/p99/số mẫu/tỷ lệ lỗi; clock uncertainty phải đủ kết luận.
Frontend mong đợi: Online UI nhận đủ IDs, không duplicate/hole và đáp ứng p95; không coi Hub receipt là UI render.
Backend mong đợi: Kết quả60min sau warmup, thông số thực tế; báo DM riêng khi thiếu Community, không tự ghi v1 đạt.
Đối soát DB chỉ đọc:
- Counter/message/operation khớp committed sends; không tăng do retry/GET.
- Worker/outbox backlog có timeline, dữ liệu measurement không body/token.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Dừng load/stream/fixture worker đúng run, giữ báo cáo; không xóa data/volume để làm đẹp benchmark.

TEST CASE DM-P8-T02-C02 — Fault dưới tải: đo lỗi trung thực và giữ correctness
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Có workload DM và baseline IDs/counts; fault recipe riêng mất response sau commit, rollback, worker pause.
- Bảng error classification chốt trước chạy, không loại lỗi bất ngờ khỏi mẫu số sau khi thấy kết quả.
Dữ liệu cụ thể: Mỗi fault10operations có UUID riêng; 30 logical sends: commit-lost10, rollback10, worker-paused10.
Bước kiểm tra frontend:
1. A/B mở FE thật trong tải; người dùng thấy trạng thái unknown/failed đúng loại lỗi.
2. Không auto mutation retry khi online; bấm retry cho các operation được chọn, giữ UUID/body.
3. Sau phục hồi worker/reconnect, UI và history đủ tập ID committed, không duplicate.
Bước kiểm tra backend:
1. Commit-lost10 replay=>same10IDs; rollback10 retry thành công=>10IDs; worker-pause10 commit=>10IDs sau dispatch.
2. Đối soát dự kiến30 unique logical sends nếu tất cả10rollback đã được retry; đếm attempts/commits/errors riêng, báo lỗi fault-injected và unexpected theo phân loại đã chốt.
Frontend mong đợi: Không sent giả trước commit; realtime phục hồi không tạo thêm dòng.
Backend mong đợi: Retry logical exactly-one; rollback không có row trước retry; lỗi injected không che unexpected faults khác.
Đối soát DB chỉ đọc:
- Delta messages/operations cuối=30 cho scope này; counter+30.
- Outbox drained sau phục hồi, không body log, no orphan operations/counters.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T02-C03 — Ma trận trình duyệt và thiết bị trên bản nghiệm thu
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Danh sách2stable versions tại thời điểm nghiệm thu của Chrome/Edge/Firefox/Safari; nguồn/version/date ghi rõ.
- Android Chrome/iOS Safari thiết bị thật; nếu không có browser/device thì Bị chặn, không giả lập thành PASS.
Dữ liệu cụ thể: M01/M02/M03/M04; emoji biên2000/2002UTF16; IME; draft/logout; realtime/reconnect.
Bước kiểm tra frontend:
1. Mỗi browser chạy login/search/open/send M02/M03/M04; ghi emoji/newline/spaces và script không chạy.
2. Chạy Enter/Shift+Enter/IME, draft đổi DM/actor và lỗi validation.
3. Thiết bị mobile chạy Enter newline/nút Gửi/keyboard/reconnect thật; lưu device/version từng ca.
Bước kiểm tra backend:
1. GET history IDs của từng lane/browser so content/version với original normalized bodies.
2. Replay UUID/body tương ứng không duplicate; invalid payloads REST400 đúng policy, không lệ thuộc UI validation.
Frontend mong đợi: Text render đúng/an toàn, composer đúng thiết bị; reconnect merge cùng ID/version.
Backend mong đợi: API/DB kết quả giống nhau giữa browser/device; viewport chỉ là kiểm layout, không thay mobile IME.
Đối soát DB chỉ đọc:
- Counts theo từng lane và accepted/rejected input; không lưu script như HTML thực thi.
- Không body draft trong persistent storage; không auto mutation replay do reconnect.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T02-C04 — Observability và cảnh báo thử có thể vận hành
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Metrics/traces/dashboard/cảnh báo receiver thử local đã cấu hình; không gửi email/Slack tới người ngoài.
- Danh sách metrics/thresholds/runbook do task chốt, không ghi body/token/key trong labels.
Dữ liệu cụ thể: Pause worker60s; authority fault; revoke stream ≤5s; IDs/timestamps dùng correlation.
Bước kiểm tra frontend:
1. A/B online; pause worker, A gửi commit thành công nhưng B chưa có realtime.
2. Dashboard cho backlog/age tăng; phục hồi worker, B nhận không duplicate.
3. Chạy revoke với continuous stream; dashboard thể hiện deadline bằng commit/dispatch timestamps, mở runbook từ alert thử.
Bước kiểm tra backend:
1. Kiểm sample outbox backlog/age, dispatch delay, guard failure, send latency/error rate; alert tới controlled test sink theo threshold.
2. Tìm log/traces bằng correlation IDs; kiểm redaction không raw JWT/refresh/key/body; tắt authority fault và replay only khi người dùng yêu cầu.
Frontend mong đợi: Người vận hành xác định được lỗi/recipe; UI không ghi sent/received sai khi worker bị pause.
Backend mong đợi: Alert thử có firing/resolved proof; revoke metric đo từ commit≤5s, không từ thời điểm hiển thị AccessRevoked.
Đối soát DB chỉ đọc:
- Pause không mất outbox committed; resume drain/correct lease.
- Không ghi payload riêng vào labels/audit/log; metrics không query vào DB production.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Resume worker/tắt fault, kiểm alert resolved, dừng receiver thử; không mở kênh gửi thông báo thật nếu chưa được giao.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P8-T02-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P8-T02.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P8-T02; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không bắt đầu DM-P8-T03, không tạo branch/PR task kế.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P8-T02, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

<a id="dm-p8-t03"></a>

## DM P8 T03 tích hợp phụ thuộc Identity và hồ sơ phát hành

Copy toàn bộ khối dưới đây.

```text
Thực hiện duy nhất DM-P8-T03: tích hợp phụ thuộc Identity và hồ sơ phát hành.
Repository: E:\Project\SCDC\scdc.
Tôi cần tự test frontend và backend bằng dữ liệu mẫu bạn chuẩn bị trước bước tiếp theo.

A. Phạm vi, phụ thuộc và branch
Task trước: DM-P8-T02. Kiểm tra PASS FE/BE và quyền đi tiếp đã có trong phiên/biên bản. Thiếu xác nhận thì chuẩn bị phần độc lập và dừng phần phụ thuộc, không tự điền PASS hoặc hỏi lại quyền đã cấp.
Branch `chore/dm-p8-t03-release-handoff`. Phụ thuộc: gói Identity/email, cấu hình domain/key store, quyền vận hành/người ký và các gate v1 liên quan. Đây là tích hợp các gói đã có, không tự làm toàn bộ email/media/microservice trong task DM.
Branch cụ thể: chore/dm-p8-t03-release-handoff. Nhánh tích hợp/PR target: message. Base là origin/message đã kiểm tra và đã tích hợp task trước được tôi PASS FE/BE; nếu task trước chưa merge, không tự dùng stacked branch khi chưa được phép.
Giữ quy tắc DM hiện hành: text tối đa 2.000 UTF-16 sau CRLF/CR thành LF, không trim/NFC body; không lưu body sửa cũ trong edit/outbox/log. Mutation không tự replay sau refresh/reconnect; retry giữ clientMessageId/content ban đầu. Actor/member/author kiểm tra ở server; read/author edit-delete không đòi peer active, send mới kiểm tra peer. Draft/cache theo actor/conversation trong RAM tab. MVP một API host, không tự mở rộng scope sang group/chat phòng/file/reaction/read-state/block/moderation/media/microservice.
Nếu reuse nhánh cũ, ghi source commit và đối chiếu contract/policy mới; không merge cả chuỗi nhánh cũ. Các subtask cùng task dùng chung branch/PR. Commit conventional chứa task ID, stage danh sách file thuộc task; PR hướng message có migration, dữ liệu, FE/BE test và trạng thái Chờ người dùng test. Quyền push nhánh task và merge vào message đã được cấp; không chuyển PR sang main hoặc merge nhánh Messaging cũ ngoài task.

B. Tài liệu và source
Đọc AGENTS.md áp dụng và các tài liệu/source trong repository:
- docs/plans/direct-messaging.md, mục task này và các điều kiện chung của kế hoạch.
- docs/features/direct-messaging.md; docs/contracts/direct-messaging.openapi.json; docs/contracts/chat-realtime.schema.json.
- docs/features/accounts.md; docs/architecture.md; docs/data-lifecycle.md; docs/release-operations.md theo phần task sử dụng.
- docs/fixtures/dm-demo-plan.json, dm-acceptance-cases.json, text-policy.json, text-validation.json và dm-fingerprint.json.
- Source/migration/test liên quan trong services/, clients/WebClient/, database/, tests/.
Nội dung thực hiện và quy trình dừng được ghi đầy đủ trong prompt này; không cần yêu cầu tôi ghép thêm một prompt chung.

C. Quy trình làm việc và Git flow
1. Làm việc trong repository `E:\Project\SCDC\scdc`. Đọc AGENTS nếu có, working tree, nhánh hiện hành, source liên quan và đặc tả chuẩn. Giữ thay đổi người dùng, không stage toàn repo, không reset/stash tùy ý.
2. Xác minh task trước đã được người dùng test FE và BE và cho phép đi tiếp. P0-T01 không có predecessor. Chưa có xác nhận thì chỉ đọc/chuẩn bị task được giao; không triển khai phần phụ thuộc.
3. Dùng branch cụ thể ghi dưới đây theo kế hoạch từ baseline tích hợp đã duyệt. Có thể tái sử dụng code nhánh cũ sau đối chiếu, ghi nguồn commit; không merge cả chuỗi nhánh cũ hoặc đổi policy DM theo docs cũ.
4. Hoàn tất subtask task hiện tại, UI/API/DB liên quan, migration và test phù hợp. Test chỉ chạy trên DB thử riêng; dữ liệu fixture không thay quyền production hoặc bypass guard.
5. Chạy backend integration với PostgreSQL thật, frontend tests/build và E2E liên quan; chuẩn bị dữ liệu alias/ID thật, recipe lỗi, URL, tài khoản, REST/Swagger/helper và query DB chỉ đọc. Không báo đã chạy khi chỉ có source hoặc môi trường mô phỏng.
6. Ghi `docs/acceptance/direct-messaging/DM-P8-T03.md` theo `docs/templates/dm-task-acceptance.md`, bao gồm bước test FE/BE cụ thể để tôi tự làm. Cung cấp commit/build, kết quả agent, phần bị chặn và phần còn chờ tôi xác nhận.
7. Commit đúng file/task; push nhánh task lên origin (quyền đã cấp), kiểm tra git ls-remote để remote SHA trùng HEAD và ghi vào biên bản. PR nếu tạo phải có base message; thiếu auth hoặc push lỗi thì ghi rõ và giữ kết quả local, không merge. Dừng chờ tôi test; không xem việc giao prompt này là giao tất cả task.
8. Chỉ sau phản hồi PASS cả FE và BE trên build bàn giao: fetch origin/message và nhánh task, kiểm tra remote task SHA vẫn đúng build đã duyệt; tích hợp riêng task này vào message bằng merge --no-ff (hoặc PR giữ merge commit). Giải quyết conflict bằng cách giữ cập nhật docs/contract đã được duyệt, không tự chọn toàn bộ ours/theirs; chạy smoke và kiểm tra liên quan, nếu hành vi/config/schema thay đổi thì bàn giao lại chờ tôi test. Push message không force; xác minh remote SHA trùng commit merge và task là ancestor của origin/message. Nếu message thay đổi đồng thời, fetch và kiểm tra lại; không ghi đè remote. Việc merge message vào main/phát hành cần yêu cầu riêng. Chỉ bắt đầu task kế khi được giao trong phạm vi phiên.
8. Kết thúc task ở Chờ người dùng test. Dừng và chờ tôi phản hồi PASS/FAIL; không tạo task kế hoặc tự đi tiếp. Tôi FAIL thì sửa cùng task, chạy lại phần liên quan và bàn giao để tôi test lại.

Các kiểm chứng cần thiết chưa chạy được phải ghi Bị chặn cùng lý do và đầu ra đã chuẩn bị. Không thay bằng mock hoặc lược bớt test để đạt gate. Không tự mở rộng sang email, Community, media hoặc microservice ngoài phụ thuộc đã ghi của task.

D. Toàn bộ 5 subtask cần hoàn tất
- P8-T03.1 Tích hợp bản Identity/email đã duyệt; account mới nhận verify/reset link thật; Development token tắt trên bản release.
- P8-T03.2 HTTPS/CORS/proxy/Hub/config/secrets, migrations và CI; deploy/rollback rehearsal giữ protection/epoch/key và DB/object dependency nếu có.
- P8-T03.3 Smoke user mới: register→email→verify→login→DM; logout/reset/revoke, old token và third-party access đúng; DM không có self-delete/admin-read.
- P8-T03.4 Tổng hợp hồ sơ RLS gates, lỗi tồn/người phụ trách, scope chưa xong; chọn người có thẩm quyền theo quy trình, không suy người dùng duyệt task là người ký production.
- P8-T03.5 Bàn giao artefact/runbook/dataset và kết quả frontend/backend, dừng trước publish/mở public nếu chưa được phép rõ ràng.

E. Môi trường và dữ liệu để tôi test
Dữ liệu mẫu và bản chạy:
- Dùng DB scdc_dm_acceptance_test, Compose project scdc-dm-acceptance, không dùng scdc_chat hoặc xóa volume ứng dụng.
- Cổng dự kiến Web15300/API15026/PostgreSQL15432. Kiểm tra xung đột và bàn giao URL thực tế, ví dụ http://localhost:15300 và http://localhost:15026/swagger.
- Password mẫu local: DmDemo2026!Local. A=dm_demo_an/dm-an@example.test; B=dm_demo_bao/dm-bao@example.test; C=dm_demo_chi/dm-chi@example.test. A/B/C active/verified; B/C cùng tên Bảo Demo, C là outsider của DM A-B.
- U=dm_demo_pending/dm-pending@example.test, pending/unverified. K=dm_demo_khoa/dm-khoa@example.test, setup active/verified, chỉ disabled/revoke trong ca có helper test tương ứng. S01-S23=dm_demo_search01...23 để search phân trang.
- D-AB=A/B; D-AC=A/C; D-AK=A/K lúc K active; D-HIST=A/S01. Lấy user/conversation/message ID từ API response/manifest, không hardcode UUID server. A1/A2, B1/B2 dùng profile browser với phiên độc lập.
- Nội dung M01 chào Bảo, M02 CRLF/emoji, M03 giữ khoảng trắng, M04 HTML như text; biên a x2000/x2001 và emoji x1000/x1001, empty/invisible/invalid theo dm-demo-plan.json và text-validation.json.
- H121 là 121 tin trong D-HIST rỗng để thử 50/50/21; R101 là 101 tin mới D-AB sau baseline/resumeCursor đã chụp. Ghi số tăng thêm theo run, không xóa DB để lặp ca.
- O1=7c8e7c59-b35a-4d12-b22f-965b96ff4e44; O2=7c8e7c59-b35a-4d12-b22f-965b96ff4e45 chỉ cho ca retry có chủ ý. Run/send độc lập dùng UUIDv4 mới.
- Fixture dự kiến chưa đồng nghĩa đã tạo DB. P0 tạo helper PowerShell setup/login/IDs; task sau dùng và bổ sung recipe thích hợp. Giữ token/key/runtime manifest ngoài Git, chỉ verify bằng Development token ở môi trường test.
Chỉ chuẩn bị dữ liệu cần cho task. Không tạo API/bypass quản trị production để dựng fixture; helper khóa K chưa có thì ghi rõ ca còn chờ task tương ứng.

F. Kiểm tra kỹ thuật trước bàn giao
Tự chạy backend integration với PostgreSQL thật, frontend tests/build và E2E phù hợp task. Kiểm tra cả thành công, lỗi/quyền/đồng thời và DB invariants theo scope. Chỉ bổ sung regression khi thay đổi/lỗi cần chứng minh.
Ghi đúng loại proof: mock/fetch, fault fixture, mobile viewport không thay API/DB/browser/thiết bị thật. Không log token/key/body riêng; fault harness chỉ test/local, không đưa developer controls vào flow sản phẩm.

G. Các ca tôi tự kiểm tra
Frontend: Dùng hai email thử được kiểm soát nhận link thật, tạo account mới rồi gửi/nhận/sửa/xóa/reconnect qua Web release; reset/logout không hồi sinh phiên cũ.
Backend: Swagger chỉ dùng môi trường phù hợp, release có helper REST collection thay nếu tắt Swagger; health/readiness, auth/cutoff, migration/rollback và logs không secrets; đối chiếu release gates.
Điều kiện đạt: Bạn PASS phần DM trên build release và ghi phần v1 khác còn mở. Publish/merge/quyết định phát hành vẫn theo phạm vi quyền đã cấp; không tự phát hành từ kế hoạch này.

G1. Cách chạy các ví dụ chi tiết
Các case C01–C04 dưới đây là kế hoạch, trạng thái Chưa chạy. Đọc thêm AC/TC hiện hành để bổ sung biến thể bắt buộc; bốn ví dụ không giới hạn phạm vi regression.
- Trước test, bàn giao lệnh PowerShell/REST/SQL chỉ đọc thực thi được; resolve mọi alias/ID/version/cursor từ API/manifest. Không chỉ ghi tên script/helper chưa tồn tại hoặc để tôi tự đoán tên bảng. Token/key nằm ngoài Git và được lọc khỏi ảnh/HAR/report.
- Nếu FE và BE cùng tạo mutation mới, dùng hai lane/run riêng và chụp baseline/delta riêng. Nếu BE replay thao tác FE, dùng chính xác UUID/body/actor của request UI. Không gửi UUID khác rồi kỳ vọng chỉ một row. Ghi rõ lane và số mutation dự kiến trước bàn giao.
- Ca revoke dùng đúng token/session cũ đã bị thu hồi, không login mới thay thế. A1/A2 phải là hai login session độc lập.
- Status/errorCode chưa chốt phải được agent chốt theo contract trước bàn giao, ghi một kỳ vọng cụ thể cho mỗi request; không nhận “403 hoặc 404 đều được” làm PASS. DTO Message dùng content, không có normalizedContent; đọc current message qua trang history đã có.
- Payload lặp a/emoji cần file JSON UTF-8 có UUIDv4 mới; CRLF/CR thành LF rồi đếm UTF-16; không trim/NFC. Raw surrogate sai dùng file fixture, không thay bằng U+FFFD. Cung cấp nguyên request/file để tôi chạy.
- Fault/barrier phải có lệnh bật/tắt, scope actor/run và điểm trước/sau commit. Injection ở client phải ghi fixture, khác bằng chứng REST/PostgreSQL/Hub thật. Không yêu cầu API chưa tới phase như thể đã có; ghi Chưa chạy và test E2E lại khi phase đó hoàn tất.
- Chỉ count dữ liệu trong scope run/lane. Không drop DB/xóa volume hoặc thay DB ứng dụng chính để lặp test. Thiếu helper/runtime/device/provider thì Bị chặn, không tự bỏ case. Email example.test không phải hộp thư thật.
- PASS khi FE, BE, DB và bằng chứng bắt buộc khớp trên đúng build và tôi xác nhận; agent tự test không thay người dùng test. Hoàn tất case phải tắt fault/stream/clock injection, giữ bằng chứng và manifest.

TEST CASE DM-P8-T03-C01 — Email thật register/verify/login rồi DM
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Có Identity email provider và hộp thư thật do người dùng kiểm soát, staging hostname/redirect rõ.
- example.test/Development token chỉ fixture; chưa có provider/mailbox thì Bị chặn, không tự đánh dấu verified.
Dữ liệu cụ thể: Account V mới, username run-specific; email/password thực tế cung cấp riêng ngoài Git.
Bước kiểm tra frontend:
1. Người dùng register V trên FE; chưa verify không vào ứng dụng DM.
2. Mở email xác minh nhận thật, kiểm link đúng host/TTL rồi xác minh theo UX hiện hành.
3. Login V, tìm B, mở DM, gửi REAL-MAIL-DM; B nhận và history persist.
Bước kiểm tra backend:
1. Đối chiếu API register/verification/login với accounts contract và actual request, ghi exact route/body/status khi bàn giao, không giả định provider đã có.
2. POST open/message từ phiên V valid=>200; GET history đúng ID, không login/DM access trước email verified.
Frontend mong đợi: Email tới hộp thư thật; verify UI chạy đúng và DM sau verify hoạt động.
Backend mong đợi: Bằng chứng real delivery/verification; Development auto-verify không đủ gate; DM đúng actor/guard.
Đối soát DB chỉ đọc:
- V chuyển verified bởi flow thật, session tạo đúng sau verify; một logical DM message.
- Không commit mật khẩu/link/token mail; outbox mail log không lộ verification token.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Dùng account thử có consent; giữ chỉ metadata đã lọc nhạy cảm. Không gửi tới hộp thư không do người dùng kiểm soát.

TEST CASE DM-P8-T03-C02 — Reset mật khẩu thật thu hồi phiên và không consume bởi mail scanner GET
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- V verified đang có V1/V2 phiên riêng; nhận email reset thật.
- Backend/frontend reset UX và tokens TTL/one-time theo accounts docs; thao tác mail chỉ tới mailbox đã cho phép.
Dữ liệu cụ thể: Reset request V; mở link GET trước, submit POST sau; NEW_PASSWORD giữ ngoài Git.
Bước kiểm tra frontend:
1. V1 yêu cầu quên mật khẩu; nhận reset email; mở link chỉ render form, chưa đổi mật khẩu.
2. Nhập mật khẩu mới và submit một lần; V2 online đang DM phải hết phiên theo deadline.
3. Login mật khẩu cũ thất bại, mới thành công; gửi DM bằng phiên mới, không auto gửi lại mutation từ V2.
Bước kiểm tra backend:
1. GET link không consume reset token; submit operation với đúng contract thành công; submit lại bị từ chối theo Identity error đã chốt.
2. Đúng V1/V2 old tokens GET401/Hub deny; new login valid, kiểm reset không tự verified account pending ở fixture riêng.
Frontend mong đợi: Link scanner/GET không phá reset; UI phiên cũ dừng; không leak token URL trong analytics/log.
Backend mong đợi: One-time token áp dụng ở mutation; revoke old families/session, stream≤5s; reset không thay email-verified status sai.
Đối soát DB chỉ đọc:
- Password hash/current security stamp đổi; old sessions revoked giữ markers.
- Không token reset/plain password trong Git/log; new session IDs khác.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T03-C03 — Diễn tập deploy và rollback giữ data, key và protection floors
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- Staging riêng; backup/journal/keyring còn đủ; migration compatibility/rollback method chốt theo release docs.
- Runbook exact commands có kiểm tra target và người chịu trách nhiệm; chưa thực hiện production publish.
Dữ liệu cụ thể: Trước deploy chụp IDs/versions/counter/cursor, một edit/delete/revoke floor và original dedup operation.
Bước kiểm tra frontend:
1. Deploy build staging được giao; A/B smoke search/open/send/history và tombstone.
2. Rollback theo phương án đã review, mở FE/API kiểm cùng tin/phiên hiện trạng.
3. Nếu schema rollback không tương thích, dùng forward-fix được ghi rõ; không tự DROP/restore backup stale.
Bước kiểm tra backend:
1. GET old cursor còn hạn đúng keyring; replay original committed operation200 same current state.
2. Old revoked token401; history giữ protection floors; đọc migration/backup inventory và health qua reverse proxy theo runbook.
Frontend mong đợi: Data/quyền không đi lùi sau deploy/rollback; UI đúng trạng thái hiện tại.
Backend mong đợi: Host/proxy/auth hoạt động; keys không mất; không hồi sinh body/phiên do rollback.
Đối soát DB chỉ đọc:
- Version/counter không giảm, dedup/journal/revoke markers giữ.
- Không đè DB chính; health không lộ secrets/PII, backup artifact tuân retention.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

TEST CASE DM-P8-T03-C04 — Hồ sơ phát hành có gates, quyền và bằng chứng cụ thể
Trạng thái ví dụ: Chưa chạy (planned-not-executed).
Điều kiện trước:
- P7internal acceptance và từng P8 proof có build/runtime thật; người ký release/quyền deploy được xác định.
- Có danh sách Community/Identity/email/security/restore/load/browser gates còn mở.
Dữ liệu cụ thể: Release checklist, report metrics, mail proof, restore drill, user FE/BE confirmations cùng build.
Bước kiểm tra frontend:
1. Người dùng smoke staging A/B search/send/edit/delete/reconnect và C-deny.
2. Review hồ sơ từng gate: Đạt/Bị chặn/Chưa chạy/Chưa đạt với evidence, không tick hộ trạng thái user.
3. Xác nhận scope DM và dependency; dừng ở bàn giao nếu chưa được giao quyền publish.
Bước kiểm tra backend:
1. Chạy collection7DMoperations+Hub guard trên staging qua public entrypoint; health/auth/inbox hiện trạng đúng.
2. Kiểm config prod/staging về docs/Swagger theo policy đã chốt; read-only inventory verifies migrations/keys/journal/retention; release gate nào chưa đủ thì chưa release.
Frontend mong đợi: Người dùng có build để test và biết rủi ro còn mở; không gọi toàn MVP/v1 hoàn thành chỉ vì DM PASS.
Backend mong đợi: Evidence trace commit/schema/config/tests; publish/merge chỉ trong quyền đã giao, không tự diễn giải acceptance là production approval.
Đối soát DB chỉ đọc:
- Không thay message/session/marker chỉ vì ký checklist.
- Không secret/raw token trong hồ sơ; trạng thái phát hành không che dependency thiếu.
Bằng chứng cần lưu:
- Ghi commit/build, run, lane FE/BE và actor; lưu kết quả từng bước.
- Lưu HTTP status/errorCode, ID/version/counts và bằng chứng browser/DB đã lọc token/secret.
Điều kiện PASS: Toàn bộ kỳ vọng FE/BE/DB đúng; các biến thể trong case đều được kiểm tra, có bằng chứng trên build này. Nếu sai, ghi bước, expected/actual và FAIL; thiếu điều kiện kiểm tra ghi Bị chặn, không ghi PASS.
Sau case: Tắt fault; giữ manifest/bằng chứng run. Ca cần baseline khác dùng run mới, không drop DB hoặc xóa volume ứng dụng.

H. Bàn giao bắt buộc
- Biên bản phải có từng DM-P8-T03-C01 đến C04, baseline/lane, lệnh và SQL thực tế, expected/actual, bằng chứng, kết quả agent và phần tôi Chưa xác nhận. Bổ sung AC/TC bắt buộc chưa được các ví dụ bao phủ; không gộp nhiều biến thể thành PASS nếu còn biến thể chưa chạy.
- Bản FE/API/DB chạy được và lệnh start/stop/restart/setup đã kiểm chứng; URL/cổng, tài khoản local, manifest run/ID thật và mẫu request sử dụng được.
- docs/acceptance/direct-messaging/DM-P8-T03.md theo docs/templates/dm-task-acceptance.md. Tách kết quả agent đã chạy khỏi phần tôi còn chờ xác nhận; ghi commit/build/config/schema/branch/PR.
- Hướng dẫn FE từng thao tác và BE Swagger/REST/PowerShell, SQL chỉ đọc, baseline counts, expected results và ca âm tính để tôi làm theo.
- Kết quả test/build, lỗi tồn, phần Bị chặn và phạm vi chưa thuộc task; không giả Đạt nếu chưa có runtime hoặc test thật.
- Task ở Chờ người dùng test, không tự đánh dấu Người dùng PASS hoặc toàn UC/phase hoàn thiện.

I. Dừng sau task và xử lý FAIL
Dừng ở Chờ người dùng test DM-P8-T03; chờ tôi phản hồi PASS/FAIL cả FE và BE trên build đã bàn giao. Im lặng, câu hỏi hoặc “đang test” không là PASS. Không tự bắt đầu task ngoài kế hoạch hoặc mở phát hành công khai.
Nếu tôi FAIL, reproduce FE/BE, sửa trên branch này, chạy regression liên quan, cập nhật commit/build/recipe và bàn giao để tôi test lại. Không giữ PASS build cũ cho hành vi đã thay đổi.
Áp dụng quyền commit/push/merge đã cấp đúng điều kiện, không xin lại cùng quyền. Prompt này chỉ giao DM-P8-T03, không giao thực hiện toàn kế hoạch hoặc tự publish.
```

## Phản hồi sau khi tự test

Xác nhận đúng build đã kiểm tra. Sau PASS task hiện tại, copy prompt đầy đủ của task kế để giao thực hiện; quyền merge đã cấp trong phiên vẫn áp dụng đúng phạm vi.

```text
PASS <TASK-ID> | FE: đạt | BE: đạt | Build: <commit đã test>
```

```text
FAIL <TASK-ID> | Build: <commit đã test> | Ca: <FE hoặc BE>
Thao tác: ... | Mong đợi: ... | Thực tế: ...
Sửa task này rồi bàn giao để tôi test lại. Chưa làm task tiếp theo.
```

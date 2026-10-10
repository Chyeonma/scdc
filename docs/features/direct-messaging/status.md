# SCDC — Trạng thái DM

Tổ chức lại: 2026-10-07. Đây là nguồn trạng thái và khoảng trống triển khai DM; cơ chế dùng chung không tự chứng minh một tính năng đã hoàn tất.

Cập nhật: 2026-10-10. Phạm vi: REQ-005, SCP-005. Quy tắc DM, AC-DM, ACL-02–05, màn hình DM-S, UX-DM và TC-DM/TEXT.

Đặc tả đầy đủ cho [v1](../../releases/v1.md); [MVP](../../releases/mvp.md) chọn luồng nhắn tin nền để bắt đầu trong một API host. Các quy tắc áp dụng của luồng được chọn vẫn giữ; transaction/guard xuyên Identity–Messaging được rà soát khi chuyển sang [microservice ở v1](../../architecture.md#target) theo DEC-116.

Quy tắc cốt lõi đã xác nhận. UX và hợp đồng được dẫn từ [tổng quan](README.md) còn đề xuất; P1-T02 đã được người dùng PASS FE/BE và tích hợp vào message. GET inbox P1-T03 được người dùng nghiệm thu build9e47033 ngày09/10/2026; gửi văn bản P2-T01 và lịch sử P2-T02 đã được người dùng nghiệm thu ngày10/10/2026. P3-T01 đã triển khai và chờ người dùng test FE/BE; Hub chưa triển khai. Các ca TC chưa có kết quả chạy được ghi nhận.

Module thực hiện DM là **Messaging**. [Cơ chế Messaging](../../shared/messaging/README.md), [thiết kế chi tiết](../../shared/messaging/README.md#detailed-design) và [ca TC-TEXT](../../shared/messaging/text.md#tests) đồng thời là nguồn chuẩn cho cơ chế xử lý tin dùng chung với tin phòng. Phần áp dụng vào cộng đồng, quyền phòng và phối hợp realtime được mô tả tại [tích hợp Community](../community/specs/integration.md#responsibilities); DM có điều kiện truy cập riêng, không dùng role/ACL cộng đồng.

<a id="gaps"></a>

## Vấn đề còn mở

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Đã chốt độ dài, phân trang và khớp tại DEC-069; search/cursor và frontend thật đã có proof P1-T01, được người dùng PASS; mở DM P1-T02 đã được người dùng nghiệm thu; inbox P1-T03 đã nghiệm thu; gửi P2-T01 đã nghiệm thu ngày10/10/2026. | OQ-005 |
| Lưu giữ | DEC-103–109 chốt account lock/no self-delete, TTL và restore placeholder; có [chính sách chung](../../data-lifecycle.md), còn review/migration/sổ độc lập/worker/restore proof. | OQ-005/011, DATA-GAP |
| Thử lại/đồng thời | Rà soát và thử nghiệm hợp đồng chống trùng, khóa theo hội thoại, xung đột sửa/xóa và dọn dữ liệu. | OQ-005, OQ-008 |
| Giới hạn nội dung | Đã chốt 2.000 UTF-16, xuống dòng/emoji và từ chối trống; đã có bảng text-policy và fixture theo DEC-090; client/server26 corpus đã được agent kiểm chứng trong P2-T01; người dùng đã nghiệm thu ngày10/10/2026. | OQ-005 |
| Chất lượng | Ngưỡng và ma trận đã chốt DEC-082/083; còn cấu hình/build/thiết bị và kết quả đo. | OQ-007 |

Đã bổ sung [OpenAPI dự thảo](../../contracts/direct-messaging.openapi.json) ngày2026-10-04; baseline P0 đã được duyệt, search P1-T01 được người dùng PASS và POST mở DM P1-T02 đã được người dùng nghiệm thu, GET inbox P1-T03 đã được người dùng nghiệm thu; phần tin nhắn/Hub vẫn dự thảo. Schema dùng `x-scdc-utf16-length` vì minLength/maxLength của JSON Schema không tự biểu diễn phép đếm UTF-16. `clientMessageId` UUIDv4, ID server UUIDv7 theo DEC-081; ví dụ là dữ liệu minh họa, không phải ID của dữ liệu thật. Cách biểu diễn SQL hiện tại dùng chat space/`sequence_no`; thiết kế logic dùng conversation/sequence. Cần rà soát ánh xạ, unique key theo tác giả và commit order trước triển khai, không coi seed/schema hiện tại là đã chứng minh hợp đồng đề xuất.

Các quyết định chưa chốt được giữ ở OQ-005/OQ-007/OQ-008/OQ-011. Chọn framework hoặc mô hình lưu không thay thế việc kiểm chứng lost response, concurrent commit, worker dừng và reconnect.


## Gói tìm người DM-P1-T01 — 07/10/2026

P0 đã được người dùng PASS FE/BE và đồng ý baseline; tích hợp vào `message` tại `170d959`. P1-T01 bổ sung GET `/api/v1/users/search` do Identity thực hiện và UI tìm/chọn người bằng API thật. Bằng chứng, dataset và các bước người dùng test ở [biên bản P1](../../acceptance/direct-messaging/DM-P1-T01.md); [kế hoạch](../../plans/direct-messaging.md) và [20 prompt](../../plans/direct-messaging-prompts.md) giữ quy trình dừng từng task. Search được người dùng xác nhận PASS FE/BE ngày08/10/2026 trên build `9d995d0`; đã merge/push riêng task vào `message` tại `d18d9a4`; commit ghi nhận PASS `5dd16c9` là ancestor và smoke sau merge đạt. P1-T02 đã được người dùng PASS FE/BE build ea0c31109/10/2026, [biên bản](../../acceptance/direct-messaging/DM-P1-T02.md); đã merge vào message tại e5170b4. Điều chỉnh 08/10/2026: UI cho chọn nhiều người, giữ lựa chọn khi đổi q/tải thêm/lỗi và bỏ chọn riêng hoặc tất cả. Người dùng chốt mục gần đây là người vừa nhắn tin; UI/dữ liệu thuộc P1-T03, proof thứ tự bằng tin thật thuộc P2-T01, hiện chưa triển khai. AC-DM-01 chỉ đạt lát cắt tìm/chọn; Mở hội thoại P1-T02 đã được người dùng nghiệm thu; gửi/lịch sử và Hub thuộc task sau. Các khoảng trống phía trên tiếp tục áp dụng cho phần chưa triển khai.

Điều chỉnh09/10/2026 theo người dùng: tạm chỉ chọn một người rồi bấm Mở hội thoại. DM thật dùng ID/participant từ POST API, sidebar theo actor trong RAM; chưa có loader inbox sau reload. Người vừa nhắn tin, gửi và lịch sử chưa triển khai. [P1-T02](../../acceptance/direct-messaging/DM-P1-T02.md) có lệnh kiểm thử HTTP/DB, 40 request đồng thời và fault rollback acceptance.

## Gói inbox DM-P1-T03 — 09/10/2026

GET `/api/v1/direct-conversations` trả DM thật theo membership, phân trang20/50, cursor protected theo actor/limit và TTL24h. FE tải lại inbox khi login/reload, có tải thêm/làm mới/error/retry, RAM tách actor và bỏ response cũ sau logout. Chỉ chọn một người theo yêu cầu09/10/2026. Modal Người vừa nhắn tin dùng `lastActivityAt` khác null, chưa có writer nên baseline đang rỗng; proof với tin thật chờ P2-T01. [Biên bản P1-T03](../../acceptance/direct-messaging/DM-P1-T03.md) cung cấp lệnh và dữ liệu25DM. Người dùng đã nghiệm thu build9e47033 ngày09/10/2026. P1-T03 đã tích hợp tại acce838; P2/P3 chưa có code, P4-T01 được giao nhưng phần phụ thuộc cần các tiền đề này.

## Gói gửi văn bản DM-P2-T01 — 09/10/2026

POST `/api/v1/direct-conversations/{id}/messages` lưu message/sequence/SendOperation/HMAC/outbox/projection cùng transaction. Validator Unicode17 và giới hạn2.000 UTF-16, retry cùng UUID/body không nhân đôi, body khác409, thiếu key503. Guard actor/session/member và peer cho send mới; retry committed trả trạng thái hiện hành dù peer unavailable. FE có khung nhập thật, pending/sent/error, retry chủ động và draft RAM theo actor/conversation. Người vừa nhắn tin được kiểm chứng bằng writer thật A→B/A→C/B→A, reload và fault rollback.

[Biên bản P2-T01](../../acceptance/direct-messaging/DM-P2-T01.md) có62backend/42client/6browser tests, fresh/upgrade/replay migration và C01–C04/RECENT với lệnh mẫu. **Đã được người dùng nghiệm thu ngày10/10/2026**, sau C02 PASS FE/BE theo yêu cầu; cho phép merge `message` và thực hiện P2-T02. AC-DM-02 mới đạt phần gửi, phần người nhận/lịch sử thuộc P2-T02; chưa tuyên bố hoàn tất toàn use case. Full lost-response/retry UI thuộc P3; dispatcher/Hub/reconnect thuộc P4. Các đoạn P1 ở trên ghi trạng thái theo mốc bàn giao P1.

## Lịch sử DM-P2-T02 — 10/10/2026

P2-T01 đã merge/push vào `message` tại `ea941a7`, chứa xác nhận `9401ab5`. P2-T02 dùng branch `feat/dm-p2-t02-message-history` từ đúng base này. GET history latest/before/after/through kiểm tra actor/session/membership từng trang, chốt counter sau khóa space, cursor Data Protection `Messaging.History.v1` hạn24h và giữ key ring. Trang mặc định50/tối đa100, sequence/version là chuỗi số; current DTO gồm tombstone và author unavailable.

FE tự tải latest khi mở/reload, tải tin cũ và giữ điểm cuộn, có lỗi/retry và Làm mới tin nhắn. Bảo đọc được tin An đã lưu qua GET; nhận tức thời vẫn thuộc P4. RAM tách actor, abort/generation bỏ response cũ; merge ID/version chính xác bằng BigInt, giữ send mới nếu response latest cũ về muộn. Làm mới tải lại50 tin mới nhất; các trang cũ tải lại bằng Tải tin cũ hơn.

[Biên bản P2-T02](../../acceptance/direct-messaging/DM-P2-T02.md) cung cấp dataset H121/bigint, các lệnh và từng case FE/BE cụ thể. **Chờ người dùng test FE/BE P2-T02**, chưa merge `message` và chưa làm P3/P4. Không suy toàn bộ AC/TC hoặc use case hoàn tất từ lát cắt lịch sử này.

### Nghiệm thu lịch sử ngày10/10/2026

Người dùng xác nhận P2-T02 PASS FE/BE trên bản `9a2b345` (code `a66f7d9`), đồng ý tích hợp vào `message` và làm P3-T01. Các đoạn chờ nghiệm thu phía trên là mốc bàn giao trước xác nhận.

## Retry chủ động DM-P3-T01 — 10/10/2026

P2-T02 đã merge/push vào `message` tại `b4d814d`. Branch `feat/dm-p3-t01-manual-retry` từ base này giữ operation UUID/content/draft revision bất biến; Retry dùng operation cũ, composer cho tạo operation mới, không tự replay POST sau401/network/timeout. Deadline15 giây nghĩa là chưa xác nhận kết quả. Actor/conversation/UUID phải khớp response; latest GET giữ version cao hơn đã biết của ID trong trang, reply muộn không xóa draft mới. Logout xóa RAM theo policy.

Fault controls chỉ ở local scripts/tests: rollback trước commit, drop reply sau API200/commit thật, delayed POST/GET và injected401; guard revoked còn được kiểm tra bằng phiên bị thu hồi thật. [Biên bản P3-T01](../../acceptance/direct-messaging/DM-P3-T01.md) có C01–C04, TC-DM-08/current edit-tombstone bằng fixture SQL thuộc run, race GET/retry, URL/ID/account của dataset p3user và các lệnh FE/BE/DB chi tiết. **Chờ người dùng test FE/BE**, chưa merge P3 vào message. Actual Hub event/reconnect/resume của AC-DM-21/TC-DM-23 chờ P4; không coi proof GET hoặc acceptance riêng task là toàn UC/phase hoàn tất.

Người dùng đã nghiệm thu P3-T01 PASS FE/BE build `156cb32` ngày10/10/2026, cho phép tích hợp `message` và thực hiện P3-T02. Các trạng thái chờ của P3-T01 phía trên ghi theo thời điểm bàn giao trước xác nhận.

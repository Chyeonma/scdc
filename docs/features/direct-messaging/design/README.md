# SCDC — Thiết kế DM

API tìm người/hội thoại và các điều kiện riêng của DM. Model, fingerprint, transaction, history và Hub dùng chung tại [Messaging](../../../shared/messaging/README.md). Các hợp đồng vẫn là thiết kế mục tiêu.

<a id="contracts"></a>

## Hợp đồng API, dữ liệu và đồng bộ đề xuất

Thiết kế bên dưới cụ thể hóa hành vi đã chốt, chưa phải API đã triển khai hoặc đã duyệt. Tài khoản sở hữu danh tính/phiên; Messaging sở hữu hội thoại/tin/thao tác. Giao tiếp qua `SCDC.Contracts` trong Modular Monolith; DM không đọc bảng tài khoản và không phụ thuộc Community.

[OpenAPI dự thảo](../../../contracts/direct-messaging.openapi.json) biểu diễn schema HTTP bên dưới; trạng thái dự thảo cho các endpoint chưa triển khai; GET search đã được duyệt P1-T01, POST tạo/lấy DM có implementation P1-T02 và chờ người dùng test. REST/SignalR, UUID và cursor bảo vệ đã chốt DEC-081; mã lỗi/schema vẫn cần rà soát cùng mock/thử nghiệm. Giới hạn UTF-16 được ghi bằng extension, không dùng minLength/maxLength để thay thế validation server.

Prefix đề xuất `/api/v1`. API hiện tại của Accounts ở [đặc tả tài khoản](../../accounts/design/README.md#api-current). Lỗi DM dùng [ProblemDetails chung](../../../architecture.md#contracts) theo DEC-061, validation 400; mã lỗi DM được nêu là đề xuất, chưa phải mã đang hoạt động.

`sequence`/`version` truyền dưới dạng chuỗi số nguyên; thời điểm theo UTC. Actor lấy từ phiên. ID/cursor/transport đã chọn DEC-081; schema, mock và mapping SQL cần rà soát trước triển khai.

<a id="contract-5"></a>

### Hợp đồng 5 — API DM đề xuất

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/search?q=...&cursor=...&limit=...` | Một phần tên tài khoản/tên hiển thị | `200 {items: UserSummary[], nextCursor}`; không có kết quả trả mảng rỗng |
| `POST /direct-conversations` | `{ peerUserId }` | `200 { id, participants }`; tạo hoặc lấy hội thoại duy nhất của cặp |
| `GET /direct-conversations?cursor=...&limit=...` | Con trỏ danh sách | Các hội thoại của chính người gọi; không trả hội thoại người khác |
| `GET /direct-conversations/{id}/messages` | `before`, hoặc `after` với mốc `through`; `limit` | Trang tin; cấu trúc và thứ tự ở [hợp đồng lịch sử](../../../shared/messaging/realtime.md#contract-8) |
| `POST /direct-conversations/{id}/messages` | `{ clientMessageId, content }` | `200 MessageDto` sau commit, gồm cả lần gửi đầu và lần thử lại |
| `PATCH /direct-conversations/{id}/messages/{messageId}` | `{ content, expectedVersion }` | `200 MessageDto` với nội dung mới; sai version trả `409 VERSION_CONFLICT` |
| `DELETE /direct-conversations/{id}/messages/{messageId}?expectedVersion=...` | Phiên bản đang thấy | `200 MessageDto` dạng tombstone; xóa lại tin đã xóa trả cùng tombstone |

`MessageDto` gồm `id, conversationId, author: UserSummary, clientMessageId,
sequence, version, content, createdAt, editedAt, deletedAt`.

```json
{
  "clientMessageId": "7c8e7c59-b35a-4d12-b22f-965b96ff4e44",
  "content": "Chào bạn!"
}
```

`clientMessageId` được tạo **một lần khi người dùng bấm gửi**, giữ nguyên
trong tin tạm và mọi lần “Thử lại”. Response và sự kiện có cùng ID này
để client thay tin tạm bằng đúng tin đã lưu. Ví dụ trên minh họa cấu trúc;
định dạng `clientMessageId` là UUIDv4 theo DEC-081; server tạo UUIDv7 cho conversation/message.

Quy tắc tìm người đã chốt DEC-069: chuỗi tìm từ 2 đến 64 đơn vị UTF-16,
không phân biệt hoa/thường, không tự bỏ dấu; khớp một phần, ưu tiên khớp
đúng tên tài khoản, sau đó thứ tự tên tài khoản và ID để phân trang ổn
định. Trang mặc định 20, tối đa 50. Chỉ trả người đủ điều kiện nhận DM;
không trả chính người tìm. Các giới hạn/cách khớp đã xác nhận; thứ tự username/ID và cursor ổn định là thiết kế cần rà soát. Không dùng endpoint tìm kiếm để lộ email.

### Đối chiếu SQL trước triển khai

Đọc schema ngày 2026-10-04; chưa có writer Messaging để kiểm chứng. Các dòng này là đầu việc kỹ thuật, không thay đổi SQL trong lần hoàn thiện docs.

| Mã | Schema hiện tại | Đầu ra cần có |
|---|---|---|
| DM-SQL-01 | `direct_conversations` unique `(user_low_id,user_high_id)`, kiểm tra low < high | Chọn cùng thứ tự UUID giữa writer/DB; tạo space/cặp cùng transaction; chứng minh mở đồng thời trả cùng DM |
| DM-SQL-02 | `messages.sequence_no` là identity toàn cục, cấp trước commit và có thể có khoảng trống | Thiết kế khóa/counter theo hội thoại bảo đảm thứ tự commit; identity hiện tại không chứng minh bù tin không sót |
| DM-SQL-03 | Unique `(space_id,author_user_id,client_message_id)` đã có | Bổ sung SendOperation/fingerprint còn tồn tại sau xóa; unique tin đơn thuần chưa giải quyết payload khác hoặc thử lại sau sửa/xóa |
| DM-SQL-04 | Constraint văn bản dùng `char_length` tối đa 10.000; text message yêu cầu content khác null | Ràng buộc 2.000 UTF-16 ở service; điều chỉnh constraint để tombstone có content null; không dùng char_length thay phép đếm đã chốt |
| DM-SQL-05 | Có `message_edits.previous_content` trong schema/seed | Writer v1 không ghi nội dung cũ theo DEC-052; chọn migration/cleanup phù hợp trước dùng dữ liệu thật |
| DM-SQL-06 | Có outbox chung nhưng chưa có dispatcher Messaging | Transaction tin/khóa/outbox; sự kiện chỉ tham chiếu; dispatcher đọc bản hiện hành, merge ID/version, kiểm tra quyền/phiên |

Nguồn: [schema.sql](../../../../database/postgres/schema.sql). Các trường/schema cho nhóm chat, file, reactions/read state trong seed không tự mở rộng scope DM văn bản.

<a id="contract-10"></a>

### Hợp đồng 10 — Điều kiện chốt hợp đồng

Vg/Sáng rà soát các thuật toán ID/fingerprint/cursor/transaction guard và schema chi tiết bên dưới; chính sách UTF-16/bản nháp đã chốt DEC-068/090/091. Thái đối
chiếu mỗi lỗi/trạng thái với [wireframe](../specs/ux.md#ux)
và [bộ ca kiểm thử](../specs/acceptance.md#tests).
Thay đổi đường dẫn/trường sau khi chốt phải cập nhật đồng thời mock,
frontend, backend và dữ liệu thử; không coi tài liệu này là bằng chứng
đã có API hoặc đã chạy thử nghiệm.

#### Checklist bàn giao thiết kế

Review Accounts + DM + OpenAPI/schema/fixture cùng một phiên bản; đối chiếu từng errorCode với UI và TC. Prototype/mock và proof cho concurrent commit, revoke race, key rotation, partial-page reconnect, lost response và email worker thuộc gói triển khai. DEC-103/104 chốt không self-delete/giữ lịch sử khi peer bị khóa; guard read/author edit/delete không yêu cầu peer active, send/call mới vẫn kiểm tra peer. Cần IHistoricalUserSummaryReader và UI availability; không cấp quyền đọc DM cho quản trị. Restore/cleanup theo [vòng đời dữ liệu](../../../data-lifecycle.md#restore), còn DATA-GAP proof.

Các mã lỗi dưới đây là thiết kế mục tiêu, không phải mã đã có trong backend DM:

| HTTP / errorCode | Cách xử lý giao diện |
|---|---|
| 400 `CONTENT_INVALID` / `CONTENT_EMPTY` / `CONTENT_TOO_LONG` | Giữ nội dung để sửa; không tự gửi lại hoặc cắt tin |
| 400 `CURSOR_INVALID` | Bỏ cursor, tải lại lịch sử hoặc tìm kiếm đúng scope; không gửi lại mutation |
| 409 `OPERATION_CONFLICT` | Báo thao tác gửi có nội dung không khớp; không tự đổi khóa rồi gửi |
| 409 `OPERATION_UNVERIFIABLE` | Báo không thể xác nhận lần gửi cũ; đối chiếu lịch sử, không tự tạo tin mới |
| 409 `VERSION_CONFLICT` / `MESSAGE_DELETED` | Tải lại bản hiện hành của trang đang xem; không ghi đè hoặc khôi phục tin đã xóa |
| 503 `FINGERPRINT_KEY_UNAVAILABLE` | Giữ tin lỗi, báo dịch vụ tạm không xử lý được; chỉ retry khi người dùng chọn |
| 401 / 403 | Xử lý phiên hoặc quyền hiện hành; ngừng subscription trái quyền, không tự replay thao tác |

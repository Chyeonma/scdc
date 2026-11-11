# SCDC — Nhắn tin riêng (DM)

Người dùng tìm một người khác, mở cuộc trò chuyện riêng, gửi và xem lại tin nhắn văn bản. Người gửi sửa và xóa được tin của mình. Không cần kết bạn hay cùng server.

<a id="status"></a>

**Trạng thái:** nền MVP `900e226` đã có đủ: tìm người, mở cuộc trò chuyện, hộp thư, gửi, lịch sử, gửi lại, sửa, xóa, ChatHub, outbox, tự kết nối lại và bù tin (một ứng dụng duy nhất). Bằng chứng: [hồ sơ DM](../../records/verification/messaging/README.md) (người dùng xác nhận các gói P0 đến P3), [DM-MVP](../../records/verification/messaging/DM-MVP.md) (agent chạy), [kiểm tra hợp nhất 2026-10-13](../../records/verification/messaging/merge-2026-10-13.md). Mỗi hồ sơ chỉ đúng với phiên bản và phạm vi của nó; chưa phải nghiệm thu toàn bộ DM hay v1. v1 phải chạy lại sau khi tách service.

**Người phụ trách:** Sáng (giao diện, backend, công cụ kiểm thử); Vg rà soát quyền và hợp đồng. Xem [phân công](../../project/planning.md#team).

- DM chỉ dựa trên hai người tham gia, không dùng vai trò hay ACL của server.
- Identity sở hữu danh tính và phiên; Messaging dùng hợp đồng trong `SCDC.Contracts`, không đọc bảng Identity.
- Cơ chế chung với tin phòng: [lưu và thay đổi tin](message-lifecycle.md), [đồng bộ](synchronization.md), [nội dung](text-policy.md).
- Backup, khôi phục, placeholder sau khôi phục để [sau v1](../../project/overview.md#backup-scope). Thời hạn lưu, sửa, xóa, chống trùng, đồng bộ sau kết nối lại vẫn thuộc v1.

<a id="requirements"></a>

<a id="phạm-vi-hành-trình-và-quy-tắc"></a>

## Phạm vi và quy tắc

<a id="mục-tiêu-và-phạm-vi-đợt-đầu"></a>

### Phạm vi

- Đợt đầu: tin nhắn văn bản. Tin và lịch sử được lưu để xem lại (SCP-005, SUC-003; [tổng quan](../../project/overview.md#scope)).
- Gửi file đã vào kế hoạch (DEC-173): Thái làm phần file, Sáng ghép vào tin nhắn ([gửi file](../files/README.md)); phạm vi và đợt còn chốt.
- Chặn tin nhắn không mong muốn: để đợt sau (DEC-018).

<a id="hành-trình-chính"></a>

### Hành trình chính

1. Đăng nhập, tìm người nhận theo một phần tên tài khoản hoặc tên hiển thị.
2. Chọn đúng người trong kết quả, mở cuộc trò chuyện. Không cần kết bạn hay cùng server.
3. Nhập và gửi tin. Khi hệ thống đã lưu, hiện "Đã gửi"; không lưu được thì hiện lỗi.
4. Người nhận mở cuộc trò chuyện và thấy tin, kể cả tin được lưu lúc họ không mở ứng dụng.
5. Người gửi sửa tin của mình bất cứ lúc nào; cả hai thấy nội dung mới và dấu "Đã sửa".
6. Người gửi xóa tin của mình cho cả hai; chỗ đó hiện "Tin nhắn đã bị xóa".
7. Gửi lỗi thì người gửi tự bấm Thử lại; ứng dụng không tự gửi lại. Dù lần đầu không rõ kết quả, cùng một thao tác chỉ tạo một tin.

<a id="quy-tắc-đã-xác-định"></a>

### Quy tắc

| Mã | Quy tắc |
|---|---|
| DM-001 | Tìm người nhận theo một phần tên tài khoản hoặc tên hiển thị. |
| DM-002 | Nhắn được ngay; không cần kết bạn hay cùng server. |
| DM-003 | Đợt đầu chỉ có tin văn bản. |
| DM-004 | Người gửi sửa tin của mình lúc nào cũng được; hiện "Đã sửa". |
| DM-005 | Người gửi xóa tin của mình cho cả hai; hiện "Tin nhắn đã bị xóa". |
| DM-006 | "Đã gửi" nghĩa là đã lưu. Không lưu được thì báo lỗi; người gửi bấm Thử lại. |
| DM-007 | Chặn tin từ một người cụ thể: để đợt sau. |
| DM-008 | Người nhận thấy tin đã lưu khi mở lại ứng dụng sau lúc vắng mặt. |
| DM-009 | Bấm Thử lại cùng một thao tác không tạo tin trùng, kể cả khi lần đầu không rõ kết quả. |
| DM-010 | Phải xác minh email mới gửi được. |
| DM-011 | Sửa chỉ giữ nội dung mới nhất; không có lịch sử bản cũ; vẫn hiện "Đã sửa". |
| DM-012 | Tối đa 2.000 đơn vị UTF-16; cho xuống dòng và emoji; từ chối tin trống. |
| DM-013 | Từ khóa tìm người 2–64 UTF-16, khớp một phần tên tài khoản hay tên hiển thị; không phân biệt hoa thường, có phân biệt dấu; tên tài khoản khớp đúng lên trước; mỗi trang 20, tối đa 50. |
| DM-014 | Tin không tự hết hạn trong v1. Không có tự xóa tài khoản; khóa tài khoản không xóa lịch sử. Backup và khôi phục để sau v1. |
| DM-015 | Máy tính: Enter gửi, Shift+Enter xuống dòng. Điện thoại: Enter xuống dòng, nút Gửi để gửi. |
| DM-016 | Chuẩn hóa CRLF, CR thành LF trước khi đếm; từ chối UTF-16 lỗi và tin chỉ có khoảng trắng hay ký tự vô hình; giữ Unicode và ZWJ trong tin có nội dung. |
| DM-017 | Bản nháp chỉ ở bộ nhớ tab, giữ khi chuyển cuộc trò chuyện; tải lại, đóng tab, đăng xuất thì mất. Không lưu vào localStorage hay IndexedDB. |

Các quyết định: [hồ sơ quyết định](../../records/decisions/README.md#decisions).

<a id="exceptions"></a>

<a id="ngoại-lệ-và-cách-xử-lý-đề-xuất"></a>

### Ngoại lệ

| Tình huống | Xử lý |
|---|---|
| Không tìm thấy | Danh sách trống, thông báo rõ; không tự tạo người hay cuộc trò chuyện |
| Trùng tên hiển thị | Hiện thêm tên tài khoản; chọn bằng ID; không hiện email |
| Chọn chính mình | Không tạo DM một người; báo lỗi dữ liệu |
| Mở DM đã có | Trả cùng cuộc trò chuyện của cặp đó, kể cả khi hai bên mở cùng lúc |
| Người kia bị khóa | Chặn gửi và gọi mới; người đang hoạt động vẫn đọc lịch sử và sửa, xóa tin của mình ([vòng đời dữ liệu](../../system/data-lifecycle.md#account-state)) |
| Mất phản hồi khi gửi | Giữ tin tạm; Thử lại dùng cùng mã thao tác; chỉ một tin được lưu |
| Cố ý gửi cùng nội dung lần nữa | Là thao tác mới, mã mới; tạo tin mới (không chống trùng theo nội dung) |
| Thử lại một tin đã bị sửa hay xóa sau lần gửi đầu | Trả cùng ID với trạng thái hiện tại; không khôi phục nội dung ban đầu |
| Sửa tin đã xóa | Từ chối. Xóa lặp của chính tác giả trả trạng thái đã xóa |
| Tin bị xóa sau khi đã được đọc | Bỏ nội dung khỏi dữ liệu và giao diện khi đồng bộ; không thể thu hồi bản người nhận tự sao chép |
| Phiên hết hạn hay mất mạng | Không tự gửi; xác thực lại, đồng bộ lịch sử, người gửi tự bấm Thử lại |

Không lưu nội dung cũ trong lịch sử sửa, sự kiện hay log. Cơ chế chi tiết: [API DM](#contracts).

<a id="permissions"></a>

<a id="quyền-truy-cập"></a>

## Quyền

| Mã | Thao tác | Được phép khi | Bị từ chối khi |
|---|---|---|---|
| ACL-02 | Tìm người và mở DM | Đã đăng nhập, đã xác minh; chọn đúng người nhận | Chưa đăng nhập hay chưa xác minh |
| ACL-03 | Đọc lịch sử DM | Là một trong hai người, phiên hợp lệ | Người thứ ba, kể cả quản lý server mà hai người cùng ở |
| ACL-04 | Gửi tin DM | Là người tham gia, phiên hợp lệ, đã xác minh | Chưa xác minh hay không thuộc cuộc trò chuyện |
| ACL-05 | Sửa, xóa tin DM | Có quyền vào cuộc trò chuyện và là tác giả | Người nhận sửa tin người gửi; người thứ ba |

DM không phụ thuộc vai trò hay tư cách trong server. Đợt này không có chặn người gửi (DEC-018). Server kiểm tra quyền trước khi trả lịch sử, gửi, sửa, xóa, đăng ký nhận, kể cả khi trả lại kết quả của lần gửi lặp.

<a id="ux"></a>

<a id="giao-diện-và-trạng-thái"></a>

## Giao diện

<a id="phạm-vi-bàn-giao"></a>

### Yêu cầu chung

- Phác thảo dưới đây xác định vùng, thao tác, phản hồi; chưa chốt màu, chữ hay hiệu ứng.
- Kích thước tham chiếu: 1280 × 800 (rộng) và 390 × 844 (hẹp); chưa phải danh sách thiết bị hỗ trợ.
- Bố cục thích ứng cho máy tính và trình duyệt điện thoại (DEC-059).
- Mỗi màn hình có nhãn trường, thứ tự Tab hợp lý, focus nhìn thấy được, thông báo lỗi bằng chữ. Trạng thái gửi không chỉ dựa vào màu. Tiêu chí truy cập đầy đủ còn mở (OQ-007).

<a id="tìm-người-và-hội-thoại-trên-màn-hình-rộng"></a>

### Màn hình rộng

```text
DM-S01 / DM-S02 · Tham chiếu 1280 × 800
┌─────────────────────────┬─────────────────────────────────────────┐
│ Hội thoại               │ Tên hiển thị · @ten_tai_khoan            │
│ [Tìm người…           ] │                                         │
│                         │ [Tải tin cũ hơn]                        │
│ Tên hiển thị            │                                         │
│ @ten_tai_khoan          │ Người kia · thời gian                   │
│                         │ Nội dung tin                            │
│ Hội thoại gần đây       │                                         │
│ • Người A               │                 Nội dung tin của tôi    │
│ • Người B               │                 Đã gửi · Đã sửa [⋯]    │
│                         │                                         │
│                         │                 Tin lỗi [Thử lại]       │
│                         ├─────────────────────────────────────────┤
│                         │ [Nhập tin nhắn…                        ]│
│                         │ [                                      ]│
│                         │                         123/giới hạn [Gửi]│
└─────────────────────────┴─────────────────────────────────────────┘
```

- Kết quả tìm luôn có tên tài khoản để phân biệt tên hiển thị trùng; API trả ID của người được chọn; không có email.
- Bộ đếm 2.000 ký tự dùng cùng quy tắc với server.
- Chưa chọn ai: hướng dẫn tìm người. Cuộc trò chuyện mới: lời nhắc gửi tin đầu tiên (không dùng tin giả).

<a id="màn-hình-hẹp-và-thao-tác-tin"></a>

### Màn hình hẹp, sửa và xóa

- Mỗi lần chỉ hiện một vùng: danh sách → cuộc trò chuyện. Có nút quay lại; quay lại không gửi hay xóa bản nháp.
- Bản nháp giữ theo tài khoản và cuộc trò chuyện trong bộ nhớ tab (DEC-091).

```text
DM-S03 · Sửa tin                     DM-S04 · Xóa tin
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ Sửa tin nhắn                │     │ Xóa tin nhắn này?           │
│ [Nội dung hiện tại        ] │     │ Cả hai người sẽ thấy        │
│ [                         ] │     │ “Tin nhắn đã bị xóa”.       │
│ [Hủy]               [Lưu]  │     │ [Hủy]                 [Xóa] │
└─────────────────────────────┘     └─────────────────────────────┘
```

- Menu sửa, xóa chỉ có trên tin của mình; server vẫn kiểm tra tác giả.
- Hủy sửa không đổi nội dung đã lưu.
- Xóa xong: giữ vị trí và thời gian, thay bằng dòng "Tin nhắn đã bị xóa", bỏ nội dung cũ khỏi màn hình. Tin đã xóa không sửa được, không khôi phục được.

<a id="trạng-thái-giao-diện-gắn-với-tiêu-chí-chấp-nhận"></a>

### Các trạng thái

| Mã | Trạng thái | Hiển thị | Cho |
|---|---|---|---|
| UX-DM-01 | Đang tìm, không có kết quả, lỗi tìm | Hiện tiến trình; "Không tìm thấy người phù hợp"; lỗi có nút thử lại; giữ từ khóa | AC-DM-01 |
| UX-DM-02 | Đang tải, lỗi lịch sử | Không xóa tin đang thấy; báo lỗi riêng cho phần tải thêm; giữ vị trí cuộn | AC-DM-03 |
| UX-DM-03 | Đang gửi | Tin tạm với "Đang gửi"; chưa hiện "Đã gửi" | AC-DM-02 |
| UX-DM-04 | Gửi lỗi hay không rõ | Tin tạm với "Chưa gửi được" và nút Thử lại; không tự gửi khi có mạng | AC-DM-06, 08 |
| UX-DM-05 | Thử lại thành công | Thay tin tạm bằng tin đã lưu; chỉ một dòng | AC-DM-08 |
| UX-DM-06 | Mất mạng, có mạng lại | Báo gián đoạn; đồng bộ lịch sử; tin lỗi vẫn chờ người gửi bấm | AC-DM-07, 08 |
| UX-DM-07 | Phiên hết hạn | Yêu cầu đăng nhập lại; chỉ tải lại khi đã kiểm tra quyền; không tự gửi lại | AC-ACC-04 |
| UX-DM-08 | Sửa hay xóa lỗi | Giữ nội dung đã xác nhận gần nhất; tin không biến mất; xung đột thì cho tải bản hiện tại | AC-DM-04, 05 |
| UX-DM-09 | Chưa xác minh email | Không gửi được; hướng dẫn xác minh | AC-DM-09 |
| UX-DM-10 | Người kia bị khóa | Vẫn đọc lịch sử; không gửi hay gọi mới; không hiện lý do khóa hay email | DEC-104 |
| UX-DM-11 | Tin chưa khôi phục được (sau v1) | Hiện "Nội dung chưa khôi phục được", giữ vị trí và tác giả; tác giả viết lại hay xóa được; không tải bản cũ | DEC-108, 156 |

- Nút Gửi luôn gửi. Máy tính: Enter gửi, Shift+Enter xuống dòng. Điện thoại: Enter xuống dòng (DEC-071).
- Không gửi khi bộ gõ (IME) đang ghép chữ. Bàn phím ảo phải thử trên thiết bị thật.
- Đợt văn bản không có: đã đọc, đã nhận, file, chặn tài khoản, cuộc gọi.

<a id="bàn-giao-và-nội-dung-còn-cần-rà-soát"></a>

### Còn cần trước nghiệm thu

- Hồ sơ DM đã có kiểm tra chức năng giao diện và API theo gói; chưa phải nghiên cứu khả dụng, chưa áp cho file, thông báo, mobile.
- Trước nghiệm thu v1: chốt trình duyệt, kích thước, thiết bị; kiểm thử link email, UTF-16, bàn phím và bộ gõ, xung đột trên bản chạy được chọn.
- Giao diện cộng đồng: [Community](../community/mechanics.md#ux).

<a id="contracts"></a>

<a id="hợp-đồng-api-dữ-liệu-và-đồng-bộ-đề-xuất"></a>

## API và dữ liệu

- Các endpoint DM văn bản đã có trong nền `900e226` ([controllers Messaging](../../../services/SCDC.Api/Controllers/Messaging), [tìm người](../../../services/SCDC.Api/Controllers/Identity/UsersController.cs)).
- [OpenAPI](../../contracts/direct-messaging.openapi.json) vẫn ghi `design-draft`; đó là trạng thái của file, không có nghĩa endpoint chưa làm. Giới hạn UTF-16 ghi bằng extension `x-scdc-utf16-length` (JSON Schema `minLength`, `maxLength` không đếm UTF-16).
- [Baseline DM-P0-T01](../../archive/2026-10-planning/messaging/dm-p0-baseline-design.md): danh mục lỗi, thay hay dùng lại, migration theo lát cắt, transaction và guard.
- Tiền tố `/api/v1`. Lỗi theo [ProblemDetails](../../system/api-conventions.md#contracts) (DEC-061). Tài khoản: [Tài khoản](../accounts/README.md).
- `sequence` và `version` gửi dạng chuỗi số; thời gian UTC; người thực hiện lấy từ phiên.
- Hiện dùng `SCDC.Contracts` trong một ứng dụng. Khi chuyển v1, hợp đồng mạng và quyền phải thiết kế riêng, không dùng nguyên transaction chung.

<a id="contract-5"></a>

<a id="hợp-đồng-5--api-dm-đề-xuất"></a>

### Endpoint

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/search?q=...&cursor=...&limit=...` | Một phần tên tài khoản hay tên hiển thị | `200 {items: UserSummary[], nextCursor}`; không có thì mảng rỗng |
| `POST /direct-conversations` | `{ peerUserId }` | `200 { id, participants }`; tạo hoặc trả cuộc trò chuyện duy nhất của cặp |
| `GET /direct-conversations?cursor=...&limit=...` | Cursor | Các cuộc trò chuyện của chính người gọi |
| `GET /direct-conversations/{id}/messages` | `before`, hoặc `after` với `through`; `limit` | Một trang tin ([lịch sử](synchronization.md#contract-8)) |
| `POST /direct-conversations/{id}/messages` | `{ clientMessageId, content }` | `200 MessageDto` sau commit (cả lần đầu và lần gửi lại) |
| `PATCH /direct-conversations/{id}/messages/{messageId}` | `{ content, expectedVersion }` | `200 MessageDto`; sai version: `409 VERSION_CONFLICT` |
| `DELETE /direct-conversations/{id}/messages/{messageId}?expectedVersion=...` | Version đang thấy | `200 MessageDto` dạng tombstone; xóa lặp trả cùng tombstone |

`MessageDto`: `id, conversationId, author: UserSummary, clientMessageId, sequence, version, content, createdAt, editedAt, deletedAt`.

```json
{
  "clientMessageId": "7c8e7c59-b35a-4d12-b22f-965b96ff4e44",
  "content": "Chào bạn!"
}
```

`clientMessageId` (UUIDv4) được tạo **một lần khi bấm Gửi**, giữ nguyên trong tin tạm và mọi lần Thử lại. Phản hồi và sự kiện mang cùng ID để client thay đúng tin tạm. Server tạo UUIDv7 cho cuộc trò chuyện và tin (DEC-081).

**Tìm người (DEC-069):** 2–64 UTF-16; không phân biệt hoa thường; không bỏ dấu; khớp một phần; tên tài khoản khớp đúng lên trước, rồi theo tên tài khoản và ID. Trang 20, tối đa 50. Chỉ trả người đủ điều kiện nhận DM, không trả chính người tìm, không có email. Bằng chứng: [P1-T01](../../records/verification/messaging/DM-P1-T01.md).

<a id="search-and-conversation-pagination"></a>

### Phân trang

- **Danh sách cuộc trò chuyện:** 20, tối đa 50; sắp `last_activity_at DESC NULLS LAST`, rồi ID tăng dần; cursor gắn người gọi và vị trí. Danh sách thay đổi liên tục, nên client loại trùng theo ID và tải lại trang đầu khi có tin mới. Bù tin dùng `conversation_sequence`, không dùng thời điểm hoạt động.
- **Tìm người:** khóa NFC và `ToLowerInvariant`; LIKE escape `%`, `_`, `\` để tìm đúng chuỗi. Cursor gắn từ khóa đã chuẩn hóa; đổi từ khóa hay giới hạn mà giữ cursor thì lỗi validation. Hồ sơ người dùng đổi giữa các trang có thể đổi kết quả, nên tìm lại từ đầu. Collation bỏ dấu không phù hợp DEC-069.

<a id="đối-chiếu-sql-trước-triển-khai"></a>

### Đối chiếu SQL

Truy vết từ schema ngày 2026-10-04 tới nền `900e226`. Khi chuyển database v1 phải giữ dữ liệu và khóa, và kiểm thử lại.

| Mã | Trước khi làm DM | Hiện tại và yêu cầu |
|---|---|---|
| DM-SQL-01 | `direct_conversations` unique `(user_low_id, user_high_id)`, `low < high` | Writer chuẩn hóa UUID, tạo không gian và cặp cùng transaction. Bằng chứng mở đồng thời: [P1-T02](../../records/verification/messaging/DM-P1-T02.md) |
| DM-SQL-02 | `messages.sequence_no` là identity toàn cục, có thể có khoảng trống | Đã thêm `conversation_sequence` và counter theo không gian. Bằng chứng thứ tự commit: [P3-T02](../../records/verification/messaging/DM-P3-T02.md) |
| DM-SQL-03 | Unique `(space_id, author_user_id, client_message_id)` | Đã có SendOperation và fingerprint, giữ cả khi tin đã xóa; gửi lại trả tin hiện tại hay tombstone |
| DM-SQL-04 | Ràng buộc dùng `char_length` tối đa 10.000; tin phải có nội dung | Kiểm tra 2.000 UTF-16 ở code; ràng buộc cho phép tombstone có nội dung null |
| DM-SQL-05 | Có `message_edits.previous_content` | Writer không ghi nội dung cũ (DEC-052); bảng cũ còn trong bootstrap; dữ liệu thật phải rà soát |
| DM-SQL-06 | Có outbox chung, chưa có worker Messaging | Đã có transaction tin, khóa, outbox; worker có lease. Bằng chứng: [DM-MVP](../../records/verification/messaging/DM-MVP.md). Truyền giữa service v1 còn phải làm |

Code: [schema.sql](../../../database/postgres/schema.sql), [migration](../../../database/postgres/migrations), [MessagingModule](../../../services/Modules/Messaging/MessagingModule.cs). Bảng cho nhóm chat, file, reaction, trạng thái đã đọc trong seed không mở rộng phạm vi DM.

<a id="contract-10"></a>

<a id="hợp-đồng-10--điều-kiện-chốt-hợp-đồng"></a>

### Mã lỗi

Đã có trong backend: [kiểm tra nội dung](../../../services/Modules/Messaging/Application/TextContent.cs), [gửi và gửi lại](../../../services/Modules/Messaging/Infrastructure/Services/TextMessageSender.cs), [sửa và xóa](../../../services/Modules/Messaging/Infrastructure/Services/MessageMutator.cs), [cursor](../../../services/Modules/Messaging/Infrastructure/Services/MessageHistory.cs). Có mã lỗi không có nghĩa mọi nhánh giao diện đã kiểm thử.

| HTTP và mã | Giao diện |
|---|---|
| 400 `CONTENT_INVALID`, `CONTENT_EMPTY`, `CONTENT_TOO_LONG` | Giữ nội dung để sửa; không tự gửi lại hay cắt |
| 400 `CURSOR_INVALID` | Bỏ cursor, tải lại đúng phạm vi; không gửi lại thao tác ghi |
| 409 `OPERATION_CONFLICT` | Báo nội dung không khớp với lần gửi trước; không tự đổi khóa |
| 409 `OPERATION_UNVERIFIABLE` | Báo không xác nhận được lần gửi cũ; xem lại lịch sử; không tự tạo tin mới |
| 409 `VERSION_CONFLICT`, `MESSAGE_DELETED` | Tải lại bản hiện tại; không ghi đè, không khôi phục tin đã xóa |
| 503 `FINGERPRINT_KEY_UNAVAILABLE` | Giữ tin lỗi; báo dịch vụ tạm lỗi; chỉ thử lại khi người dùng bấm |
| 401, 403 | Xử lý phiên hay quyền; ngừng đăng ký trái quyền; không tự chạy lại |

<a id="checklist-bàn-giao-thiết-kế"></a>

### Kiểm tra trước khi chốt hợp đồng

- Rà soát Tài khoản, DM, OpenAPI, schema, fixture cùng một phiên bản; đối chiếu từng mã lỗi với giao diện và ca kiểm thử. Đổi đường dẫn hay trường phải sửa cùng lúc mock, frontend, backend, dữ liệu thử.
- Thuật toán ID, fingerprint, cursor, guard: Vg và Sáng rà soát. Quy tắc UTF-16 và bản nháp đã chốt (DEC-068, 090, 091).
- Người kia bị khóa (DEC-103, 104): đọc lịch sử và tác giả sửa, xóa không cần người kia hoạt động; gửi và gọi mới vẫn kiểm tra. Nền đã có [IHistoricalUserSummaryReader](../../../services/SCDC.Contracts/Identity/IAccountPairAccessGuard.cs) và [hiển thị trạng thái](../../../clients/WebClient/src/components/DmChat.jsx); chạy lại khi chuyển v1. Không cấp quyền đọc DM cho quản trị.
- Dọn dữ liệu online ([vòng đời](../../system/data-lifecycle.md#cleanup)) thuộc v1; khôi phục để sau v1.
- Bằng chứng cho commit đồng thời, thu hồi tranh chấp, xoay khóa, kết nối lại giữa trang, mất phản hồi, worker email thuộc gói làm.

<a id="acceptance"></a>

<a id="tiêu-chí-chấp-nhận"></a>

## Tiêu chí chấp nhận

Rà soát cùng [ngoại lệ](#exceptions) và [còn mở](#gaps).

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-DM-01 | Tìm bằng một phần tên tài khoản hay tên hiển thị, rồi chọn kết quả | Mở được cuộc trò chuyện với đúng người, không cần kết bạn hay cùng server |
| AC-DM-02 | Gửi tin và hệ thống lưu thành công | Người gửi thấy "Đã gửi"; người nhận thấy nội dung khi mở |
| AC-DM-03 | Hai người đóng rồi mở lại cuộc trò chuyện | Lịch sử vẫn hiển thị |
| AC-DM-04 | Người gửi sửa một tin cũ | Cả hai thấy nội dung mới và "Đã sửa" |
| AC-DM-05 | Người gửi xóa một tin | Cả hai thấy "Tin nhắn đã bị xóa" ở đúng chỗ |
| AC-DM-06 | Gửi thất bại | Tin báo lỗi; bấm Thử lại được |
| AC-DM-07 | A gửi khi B chưa mở ứng dụng; tin đã lưu; sau đó B mở | B thấy tin trong lịch sử |
| AC-DM-08 | Lần gửi đầu không rõ kết quả; bấm Thử lại | Chỉ có một tin |
| AC-DM-09 | Tài khoản chưa xác minh thử gửi | Bị từ chối, có hướng dẫn xác minh |
| AC-DM-10 | Sửa một tin nhiều lần, mở lại trên hai thiết bị | Chỉ có nội dung mới nhất và "Đã sửa"; không có cách xem bản cũ |
| AC-DM-11 | Gửi, sửa tin dài 2.000 và 2.001 UTF-16; nhiều dòng; emoji; chỉ khoảng trắng | Nhận tới 2.000; từ chối quá dài hay trống; client và server đếm giống nhau |
| AC-DM-12 | Người thứ ba biết ID và gọi API đọc, gửi, sửa, xóa, đăng ký | Không truy cập được; không lộ nội dung hay người tham gia |
| AC-DM-13 | Hai bên cùng lúc mở DM với nhau | Cùng một cuộc trò chuyện; không có bản trùng |
| AC-DM-14 | Hai phiên cùng sửa, hoặc một sửa một xóa, từ cùng version cũ | Không ghi đè âm thầm; bên thua nhận xung đột và tải được bản hiện tại |
| AC-DM-15 | B mất mạng; A sửa, xóa tin cũ; B kết nối lại | B thấy nội dung hiện tại hay dòng thay thế, không dùng bản đệm cũ |
| AC-DM-16 | Tìm với từ khóa dài 1, 2, 64, 65; khác hoa thường, dấu; nhiều trang | Đúng giới hạn; phân biệt dấu; tên tài khoản khớp đúng lên trước; không lộ email hay chính mình |
| AC-DM-17 | Enter, Shift+Enter trên máy tính và điện thoại; gõ tiếng Việt bằng bộ gõ | Máy tính: Enter gửi, Shift+Enter xuống dòng; điện thoại: Enter xuống dòng; nút Gửi luôn gửi; đang ghép chữ thì không gửi |
| AC-DM-18 | Mở lại tin cũ chưa bị xóa | Tin không mất vì hết hạn |
| AC-DM-19 | Gửi chỉ ký tự vô hình hay variation selector; surrogate lỗi; CRLF; emoji có ZWJ | Đúng quy tắc nội dung; giữ emoji, chữ, xuống dòng hợp lệ; không tự sửa hay cắt |
| AC-DM-20 | Gõ chưa gửi rồi đổi cuộc trò chuyện; tải lại, đóng tab, đăng xuất | Bản nháp giữ trong cùng tab; tải lại, đóng, đăng xuất thì mất; không lưu vào trình duyệt |
| AC-DM-21 | Làm mới token khi POST đã lỗi; phản hồi và sự kiện đến sai thứ tự; kết nối lại phải bù nhiều trang | Không tự gửi lại; chỉ một dòng mỗi tin; `resumeCursor` chỉ tiến khi đã gộp đủ trang |

AC-DM-12 đến 15 là tiêu chí về bảo vệ và đồng thời; chưa phải kết quả đã đạt.

<a id="tests"></a>

<a id="dữ-liệu-và-ca-kiểm-thử"></a>

## Ca kiểm thử

<a id="dữ-liệu-và-môi-trường-cần-chuẩn-bị"></a>

### Dữ liệu thử

| Mã | Dùng để |
|---|---|
| A, B | Hai tài khoản đã xác minh, không cùng server, trùng tên hiển thị nhưng khác tên tài khoản |
| C | Người thứ ba, không thuộc DM của A và B |
| U | Tài khoản chưa xác minh |
| A1, A2 | Hai phiên của A, để thử sửa, xóa đồng thời và thu hồi phiên |

Tạo dữ liệu riêng cho từng ca; không dùng người thật. Cần email thử, hai cửa sổ trình duyệt độc lập, cách ngắt mạng hay làm mất phản hồi sau commit, và quyền quan sát dữ liệu và sự kiện. Không đưa API tạo lỗi vào bản công khai. Người làm dữ liệu thử và người rà soát cơ chế tạo lỗi là hai người khác nhau.

<a id="dm-hành-vi-giao-dịch-và-khôi-phục"></a>

### Hành vi, transaction, khôi phục

| Mã ca | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-DM-01 | A tìm một phần tên của B; chọn giữa hai người trùng tên hiển thị | Phân biệt bằng tên tài khoản; mở đúng người | AC-DM-01, AC-ACC-03 |
| TC-DM-02 | A và B cùng lúc mở DM với nhau | Cùng một ID, đúng hai người, không có dữ liệu thừa | AC-DM-13 |
| TC-DM-03 | A gửi; B đang mở; đóng rồi mở lại cả hai | "Đã gửi" sau khi lưu; cùng nội dung và ID; lịch sử còn | AC-DM-02, 03 |
| TC-DM-04 | B không mở ứng dụng khi A gửi; B mở lại | Thấy tin; không phụ thuộc realtime | AC-DM-07 |
| TC-DM-05 | Gây lỗi trước commit; có mạng lại | Không có tin trong DB hay sự kiện; tin lỗi giữ trên giao diện, không tự gửi | AC-DM-06 |
| TC-DM-06 | Commit xong nhưng mất phản hồi; bấm Thử lại nhiều lần, kể cả hai request đồng thời | Một tin, một ID; giao diện một dòng | AC-DM-08 |
| TC-DM-07 | Cùng khóa nhưng khác nội dung; rồi cùng nội dung với khóa mới | Lần đầu xung đột; lần sau là tin mới | AC-DM-08, [gửi](message-lifecycle.md#contract-7) |
| TC-DM-08 | Tin đã gửi bị sửa hay xóa; thử lại thao tác gửi ban đầu | Cùng ID, trả trạng thái hiện tại; không khôi phục bản cũ | DM-009, 011 |
| TC-DM-09 | A sửa nhiều lần trên A1; B và A2 mở lại | Chỉ nội dung mới nhất và "Đã sửa"; không có lịch sử sửa | AC-DM-04, 10 |
| TC-DM-10 | A xóa; xóa lại; thử sửa tin đã xóa | Hai bên thấy dòng thay thế; xóa lại an toàn; sửa bị từ chối | AC-DM-05 |
| TC-DM-11 | A1 và A2 sửa cùng version; rồi một sửa một xóa | Chỉ một thao tác áp dụng; bên kia nhận xung đột | AC-DM-14 |
| TC-DM-12 | C gọi API và đăng ký với ID DM và tin của A, B | Không đọc, gửi, sửa, xóa, nhận được; không lộ người tham gia | AC-DM-12 |
| TC-DM-13 | B sửa, xóa tin của A qua API | Server từ chối; tin không đổi | DM-004, 005, ACL-05 |
| TC-DM-14 | U gửi thẳng qua API | Bị từ chối dù client giả trạng thái | AC-DM-09 |
| TC-DM-15 | Hai request gửi; giữ transaction đầu chưa commit khi request thứ hai chạy | Phân trang và bù tin không bỏ sót; sequence theo thứ tự commit | AC-DM-03, [gửi](message-lifecycle.md#contract-7), [lịch sử](synchronization.md#contract-8) |
| TC-DM-16 | Dừng worker sau commit rồi bật lại; sự kiện trùng hay sai thứ tự | Lịch sử vẫn có tin; cập nhật phục hồi; không nhân đôi, không quay về bản cũ | AC-DM-02, 03 |
| TC-DM-17 | B offline; A gửi hơn một trang tin và sửa, xóa tin cũ B từng xem | B bù hết trang mới, tải lại tin cũ, bỏ dữ liệu đệm cũ | AC-DM-15 |

Kiểm tra nội dung cũ chỉ xét dữ liệu đang phục vụ, sự kiện và log. Backup kiểm thử sau v1 ([TC-DATA](../../system/data-lifecycle.md#acceptance)).

**Các ca bổ sung (chưa chạy):**

| Mã ca | Tình huống | Kết quả | Cho |
|---|---|---|---|
| TC-DM-18 | Tìm ở biên, hoa thường, dấu, NFC; từ khóa chứa `%`, `_`, `\`; đổi từ khóa hay giới hạn với cursor cũ | Tìm đúng chuỗi; không lộ email; cursor gắn từ khóa | AC-DM-16 |
| TC-DM-19 | Enter, Shift+Enter, điện thoại, bộ gõ | Đúng theo thiết bị; đang ghép chữ thì không gửi | AC-DM-17 |
| TC-DM-20 | Lịch sử tin cũ sau thời gian dài | Không tự hết hạn (DEC-070) | AC-DM-18 |
| TC-DM-21 | Fixture ký tự vô hình, surrogate lỗi, NUL, CRLF khi gửi, sửa, và với tin phòng | Đúng quy tắc; không ghi hay phát tin không hợp lệ | AC-DM-19 |
| TC-DM-22 | Đổi cuộc trò chuyện, tải lại, đăng xuất, đổi tài khoản, hai tab | Bản nháp và dữ liệu đệm chỉ trong tab, đúng tài khoản; tin đã gửi lưu ở DB | AC-DM-20 |
| TC-DM-23 | Gửi bị 401 hay timeout; sự kiện tới trước phản hồi; làm mới token đồng thời | Không tự gửi lại; khi bấm Thử lại giữ đúng UUID và nội dung; một dòng | AC-DM-08, 21 |
| TC-DM-24 | Xoay khóa HMAC, thiếu khóa; thứ tự byte UUID so với DB | Gửi lại dùng khóa cũ; thiếu khóa không tạo tin; cặp UUID nhất quán | AC-DM-13 |
| TC-DM-25 | 101 tin mới với limit 50; mất kết nối sau từng trang; cursor hết hạn, bị sửa, dùng chéo người | Gộp đủ 3 trang rồi mới tiến; không bỏ tin; cursor sai phạm vi bị từ chối | AC-DM-15, 21 |
| TC-DM-26 | Thu hồi phiên tranh với gửi, đăng ký, phát sự kiện | Thứ tự rõ ràng; không có thao tác mới sau thu hồi; chat ngừng trong 5 giây | DEC-083, AC-DM-12, 21 |

<a id="ma-trận-giao-diện-và-cách-ghi-kết-quả"></a>

### Thiết bị và cách ghi kết quả

- Chạy hành trình tài khoản và DM trên máy tính và trình duyệt điện thoại (DEC-059): xoay màn hình, bàn phím ảo, cuộn lịch sử, focus khi lỗi, menu sửa và xóa. Hai kích thước wireframe không thay được danh sách thiết bị thật (DEC-082).
- Mỗi lần chạy ghi: mã ca, build, cấu hình, dữ liệu, trình duyệt và thiết bị, bước làm, kỳ vọng, thực tế, bằng chứng, người chạy, lỗi liên quan.
- Mỗi lần chạy mới bắt đầu ở trạng thái **Chưa chạy**. Kết quả của nền không tự thành "Đạt" cho build v1. Ca phụ thuộc quy tắc chưa chốt ghi "Chờ chốt quy tắc".

<a id="gaps"></a>

<a id="vấn-đề-còn-mở"></a>

## Còn mở

| Nội dung | Còn lại | Theo dõi |
|---|---|---|
| Tìm người | Đã có bằng chứng ([P1-T01](../../records/verification/messaging/DM-P1-T01.md)); còn chạy lại và tích hợp hợp đồng mạng sau khi tách v1 | OQ-005 |
| Thời hạn lưu | Đã chốt (DEC-103 đến 109); còn review, migration, worker ([vòng đời](../../system/data-lifecycle.md)). Sổ bảo vệ, placeholder, backup để sau v1 | OQ-005, 011, DATA-GAP |
| Gửi lại, đồng thời | Có bằng chứng ([P3-T02](../../records/verification/messaging/DM-P3-T02.md), [DM-MVP](../../records/verification/messaging/DM-MVP.md)); còn chạy lại giữa service, kiểm thử dọn dữ liệu | OQ-005, 008 |
| Giới hạn nội dung | Có bằng chứng ([P2-T01](../../records/verification/messaging/DM-P2-T01.md)); sửa tin có trong DM-MVP. Tin phòng và build v1 kiểm thử riêng ([text-policy](text-policy.md)) | OQ-005 |
| Chất lượng | Ngưỡng và thiết bị đã chốt (DEC-082, 083); còn cấu hình, build, thiết bị, kết quả đo | OQ-007 |

Bảng SQL và seed không chứng minh mapping, khóa duy nhất hay thứ tự commit; kết quả phải lấy từ hồ sơ build đã chạy, và không tự áp cho database v1. Chọn framework hay cách lưu không thay được việc kiểm thử mất phản hồi, commit đồng thời, worker dừng, kết nối lại.

**Bàn giao:** gói MVP-DM ở [hồ sơ MVP](../../releases/mvp.md#packages); phần hoàn thiện ở [v1](../../releases/v1.md). Hồ sơ theo gói: [hồ sơ DM](../../records/verification/messaging/README.md); kế hoạch cũ: [kế hoạch DM](../../archive/2026-10-planning/messaging/direct-messaging.md).

---

Nguồn: REQ-005, SCP-005, DEC-016 đến 021, DEC-035, DEC-037, DEC-041, DEC-052, DEC-053, DEC-059, DEC-061, DEC-068 đến 071, DEC-081 đến 083, DEC-090, DEC-091, DEC-103, DEC-104, DEC-108, DEC-109, DEC-156, [DEC-171](../../records/decisions/README.md#dec-171), DEC-173.

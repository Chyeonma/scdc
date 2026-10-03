# SCDC — Nhắn tin riêng giữa hai người

Cập nhật: 2026-10-04. Phạm vi: REQ-005, SCP-005. Quy tắc DM, AC-DM, ACL-02–05, màn hình DM-S, UX-DM và TC-DM/TEXT.

Quy tắc cốt lõi đã xác nhận. UX và hợp đồng dưới đây còn đề xuất; Messaging mới ở nền module, chưa có DM API/Hub. Các ca TC chưa có kết quả chạy được ghi nhận.

## Mục lục

- [Phạm vi và quy tắc](#requirements)
- [Quyền](#permissions)
- [Giao diện](#ux)
- [Hợp đồng đề xuất](#contracts)
- [Tiêu chí chấp nhận](#acceptance)
- [Ca kiểm thử](#tests)
- [Vấn đề còn mở](#gaps)

<a id="requirements"></a>

## 1. Phạm vi, hành trình và quy tắc

### Mục tiêu và phạm vi đợt đầu

Người dùng đã đăng nhập có thể tìm một người khác, bắt đầu hội thoại
riêng, gửi và xem lại tin nhắn văn bản. Người gửi có thể sửa hoặc xóa tin
của mình. Tin nhắn và lịch sử hội thoại được lưu để xem lại theo SCP-005
và SUC-003 trong [Project Brief](../project.md#scope).

Đợt đầu ưu tiên tin nhắn văn bản. Hình ảnh, file tài liệu và cách xử lý
khi người nhận không muốn nhận tin từ một tài khoản cụ thể được xếp vào
đợt sau theo DEC-017 và DEC-018. Việc chọn đợt triển khai không thay đổi
phạm vi phiên bản đầu trong Project Brief.

### Hành trình chính

1. Người dùng đăng nhập và tìm người nhận bằng một phần tên tài khoản
   hoặc tên hiển thị.
2. Người dùng chọn đúng tài khoản trong kết quả và mở hội thoại riêng.
   Không cần kết bạn hoặc cùng cộng đồng để bắt đầu.
3. Người dùng nhập và gửi tin nhắn văn bản. Khi hệ thống đã lưu tin, giao
   diện hiển thị “Đã gửi”; tin không lưu được có trạng thái lỗi gửi.
4. Người nhận mở hội thoại và xem tin nhắn, kể cả tin đã được lưu khi họ
   chưa mở ứng dụng. Khi mở lại hội thoại, hai bên có thể xem lịch sử còn
   hiệu lực.
5. Người gửi có thể sửa tin của mình bất cứ lúc nào. Nội dung mới và dấu
   “Đã sửa” được thể hiện cho cả hai bên.
6. Người gửi có thể xóa tin của mình cho cả hai bên. Vị trí tin được thay
   bằng dòng “Tin nhắn đã bị xóa”.
7. Nếu gửi lỗi, người gửi chủ động bấm thử lại. Ứng dụng không tự thử lại.
   Dù kết quả lần gửi đầu chưa rõ, cùng thao tác chỉ tạo một tin trong
   hội thoại.

### Quy tắc đã xác định

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| DM-001 | Tìm người nhận theo một phần tên tài khoản hoặc tên hiển thị. | DEC-016, DEC-019 |
| DM-002 | Có thể bắt đầu nhắn ngay; không cần kết bạn hoặc cùng cộng đồng. | DEC-016 |
| DM-003 | Đợt đầu gửi tin nhắn văn bản. | DEC-017 |
| DM-004 | Người gửi sửa tin của mình bất cứ lúc nào; hiển thị dấu “Đã sửa”. | DEC-020 |
| DM-005 | Người gửi xóa tin của mình cho cả hai người; hiện dòng “Tin nhắn đã bị xóa”. | DEC-020 |
| DM-006 | “Đã gửi” nghĩa là hệ thống đã lưu tin; nếu không lưu được thì báo lỗi và người gửi bấm thử lại. | DEC-021 |
| DM-007 | Trường hợp người nhận không muốn nhận tin từ một tài khoản cụ thể được xử lý ở đợt sau. | DEC-018 |
| DM-008 | Người nhận thấy tin đã được hệ thống lưu khi mở lại ứng dụng sau lúc vắng mặt. | DEC-035 |
| DM-009 | Bấm thử lại cùng một thao tác gửi không tạo tin trùng, kể cả khi kết quả lần đầu không rõ. | DEC-037 |
| DM-010 | Người gửi phải xác minh email trước khi gửi tin riêng. | DEC-041, SCDC-FR-ACC-001 |
| DM-011 | Chỉ giữ nội dung mới nhất sau khi sửa, không cung cấp lịch sử phiên bản cũ; vẫn hiện dấu “Đã sửa”. | DEC-052 |
| DM-012 | Mỗi tin tối đa 2.000 đơn vị UTF-16; cho xuống dòng/emoji; từ chối tin rỗng/chỉ có khoảng trắng. | DEC-053, DEC-068 |
| DM-013 | Từ khóa tìm người 2–64 UTF-16, một phần username/displayName; không phân biệt hoa/thường, giữ dấu; ưu tiên username khớp đúng; trang mặc định 20, tối đa 50. | DEC-069 |
| DM-014 | Tin không tự hết hạn trong MVP; sửa/xóa vẫn theo quy tắc đã chốt, sao lưu/xóa tài khoản có chính sách riêng. | DEC-070 |
| DM-015 | Desktop Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng, nút Gửi gửi tin. | DEC-071 |

Các quyết định DEC-* được ghi tại
[sổ quyết định](../decisions.md#decisions).

<a id="exceptions"></a>

### Ngoại lệ và cách xử lý đề xuất

| Tình huống | Kết quả để backend, frontend và QA rà soát |
|---|---|
| Không tìm thấy người | Danh sách rỗng và thông báo rõ; không tự tạo người nhận hoặc hội thoại |
| Trùng tên hiển thị | Hiển thị thêm tên tài khoản duy nhất; chọn bằng ID; không đưa email vào kết quả |
| Chọn chính mình | Không tạo DM một người; trả lỗi dữ liệu; tính năng ghi chú cá nhân không thuộc yêu cầu hiện tại |
| Mở DM đã có | Trả cùng hội thoại của cặp hai người, kể cả khi hai bên mở đồng thời |
| Người nhận không còn đủ điều kiện sử dụng | Chặn gửi mới theo trạng thái tài khoản; việc đọc lịch sử/xóa tài khoản cần chính sách lưu giữ ở OQ-011 |
| Mất phản hồi gửi | Giữ tin tạm; người gửi bấm thử lại cùng mã thao tác; chỉ một tin được lưu |
| Chủ ý gửi cùng nội dung lần nữa | Là thao tác mới với mã mới; được tạo tin mới, không chống trùng bằng nội dung đơn thuần |
| Thử lại tin đã sửa/xóa sau lần gửi đầu | Trả cùng ID với trạng thái hiện hành, không tạo lại nội dung gửi ban đầu |
| Sửa tin đã xóa | Từ chối; không phục hồi nội dung; xóa lặp của chính tác giả trả trạng thái đã xóa |
| Tin bị xóa sau khi đã được đọc | Bỏ nội dung khỏi dữ liệu đang phục vụ và giao diện khi đồng bộ; không cam kết thu hồi bản người nhận tự sao chép |
| Phiên hết hạn/mất mạng | Không gửi tin tự động; xác thực lại, đồng bộ lịch sử và để người gửi chủ động thử lại tin lỗi |

Các cơ chế ID thao tác, lưu giữ khóa chống trùng, phiên bản sửa/xóa và
phân trang được mô tả tại [hợp đồng DM](direct-messaging.md#contracts).
Đây là thiết kế đề xuất để kiểm chứng hành vi đã chốt, không chọn ngầm
công nghệ triển khai. Không lưu nội dung cũ trong lịch sử sửa, sự kiện
hoặc log; dữ liệu sao lưu tiếp tục theo chính sách OQ-011 cần xác định.

<a id="permissions"></a>

## 2. Quyền truy cập

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-02 | Tìm người và mở DM | Đã đăng nhập sau xác minh; chọn đúng định danh người nhận | Chưa đăng nhập hoặc chưa xác minh | DEC-016, DEC-019, DEC-051 |
| ACL-03 | Đọc lịch sử DM | Là một trong hai người của hội thoại và phiên còn hợp lệ | Người thứ ba, kể cả người quản lý một cộng đồng mà hai bên tham gia | REQ-005, AC-ACC-04; suy ra từ phạm vi hội thoại riêng |
| ACL-04 | Gửi tin DM | Là người tham gia hội thoại, phiên hợp lệ, đã xác minh email | Chưa xác minh hoặc không thuộc hội thoại | DM-002, DM-010 |
| ACL-05 | Sửa/xóa tin DM | Có quyền truy cập hội thoại và là tác giả tin | Người nhận sửa/xóa tin của người gửi; người thứ ba | DEC-020 |

DM không phụ thuộc vai trò hoặc tư cách thành viên cộng đồng. Không thêm chặn người gửi trong đợt này (DEC-018). Máy chủ kiểm tra quyền trước khi trả lịch sử, gửi/sửa/xóa hoặc đăng ký cập nhật, kể cả khi trả lại kết quả gửi lặp.

<a id="ux"></a>

## 3. Giao diện và trạng thái

### Phạm vi bàn giao

Các phác thảo dưới đây xác định vùng giao diện, thao tác và phản hồi để
thảo luận với frontend và QA. Chưa chốt màu sắc, kiểu chữ hoặc thiết kế
tương tác trực quan. Kích thước tham chiếu đề xuất: màn hình rộng 1280 ×
800 và màn hình hẹp 390 × 844; đây chưa phải ma trận thiết bị hỗ trợ.

DEC-059 xác nhận mục tiêu bố cục thích ứng cho desktop và trình duyệt
điện thoại của tài khoản/DM. Mỗi màn hình phải có nhãn trường, thứ tự đi bằng bàn phím hợp lý, focus
nhìn thấy được và thông báo lỗi bằng chữ. Trạng thái gửi không chỉ dựa
vào màu. Tiêu chí tiếp cận đầy đủ tiếp tục thuộc OQ-007.

### Tìm người và hội thoại trên màn hình rộng

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

Kết quả tìm kiếm luôn đi kèm tên tài khoản để phân biệt tên hiển thị
trùng nhau; API trả định danh ổn định của người được chọn. Không đưa
email vào kết quả. Đây là cách thể hiện đề xuất cho AC-ACC-03/AC-DM-01.

Giới hạn 2.000 ký tự theo DEC-053; bộ đếm dùng cùng quy tắc với máy chủ. Khi chưa chọn ai, vùng chính hướng dẫn tìm người để
bắt đầu. Hội thoại mới có lời nhắc gửi tin đầu tiên và không dùng tin giả.

### Màn hình hẹp và thao tác tin

Màn hình hẹp chỉ hiện một vùng chính mỗi lần: danh sách/tìm người → hội
thoại. Có nút quay lại danh sách; quay lại không tự gửi hoặc tự xóa bản
nháp đang nhập. Chính sách giữ bản nháp khi tải lại/đăng xuất chưa chốt;
đề xuất chỉ giữ trong bộ nhớ của phiên giao diện.

```text
DM-S03 · Sửa tin                     DM-S04 · Xóa tin
┌─────────────────────────────┐     ┌─────────────────────────────┐
│ Sửa tin nhắn                │     │ Xóa tin nhắn này?           │
│ [Nội dung hiện tại        ] │     │ Cả hai người sẽ thấy        │
│ [                         ] │     │ “Tin nhắn đã bị xóa”.       │
│ [Hủy]               [Lưu]  │     │ [Hủy]                 [Xóa] │
└─────────────────────────────┘     └─────────────────────────────┘
```

Menu sửa/xóa chỉ có trên tin của mình; máy chủ vẫn kiểm tra tác giả.
Hủy sửa không thay nội dung đã lưu. Xóa thành công giữ vị trí và thời
gian của tin với dòng thay thế, bỏ nội dung cũ khỏi giao diện đang xem.
Tin đã xóa không có thao tác sửa. Các quy tắc này được chi tiết hóa trong
đặc tả DM; prototype không tự thêm khôi phục tin đã xóa.

### Trạng thái giao diện gắn với tiêu chí chấp nhận

| Mã | Trạng thái | Hiển thị và thao tác đề xuất | Căn cứ |
|---|---|---|---|
| UX-DM-01 | Đang tìm/không có kết quả/lỗi tìm | Hiện tiến trình; “Không tìm thấy người phù hợp”; lỗi có nút thử lại; giữ từ khóa | AC-DM-01 |
| UX-DM-02 | Đang tải/lỗi lịch sử | Không xóa tin đang thấy; báo lỗi riêng cho phần tải thêm; giữ vị trí cuộn | AC-DM-03 |
| UX-DM-03 | Đang gửi | Giữ tin tạm và hiển thị “Đang gửi”; chưa hiện “Đã gửi” | AC-DM-02 |
| UX-DM-04 | Gửi lỗi hoặc chưa rõ kết quả | Giữ tin tạm với “Chưa gửi được” và “Thử lại”; không tự gửi khi có mạng | AC-DM-06, AC-DM-08 |
| UX-DM-05 | Thử lại thành công | Thay tin tạm bằng tin đã lưu; chỉ một tin xuất hiện | AC-DM-08 |
| UX-DM-06 | Mất mạng/kết nối trở lại | Thông báo gián đoạn; đồng bộ lịch sử; tin đang lỗi vẫn chờ người gửi bấm thử lại | AC-DM-07, AC-DM-08 |
| UX-DM-07 | Phiên hết hạn | Yêu cầu đăng nhập lại; chỉ tải lại hội thoại khi đã kiểm tra quyền; không gửi lại tin tự động | AC-ACC-04 |
| UX-DM-08 | Sửa/xóa thất bại | Giữ nội dung xác nhận gần nhất; lỗi không làm tin biến mất; cho tải bản hiện tại nếu xung đột | AC-DM-04, AC-DM-05 |
| UX-DM-09 | Chưa xác minh email | Không gửi tin; có hướng dẫn xác minh theo luồng tài khoản | AC-DM-09 |

Nút “Gửi” gửi tin trên mọi thiết bị. Desktop dùng Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng theo DEC-071. Không gửi trong khi IME đang composition; hành vi bàn phím ảo phải kiểm thử trên ma trận thiết bị. Không đưa đọc/đã nhận, file, chặn tài khoản hoặc
cuộc gọi vào wireframe của đợt DM văn bản.

### Bàn giao và nội dung còn cần rà soát

Vg rà soát bố cục và trạng thái; Thái dựng prototype theo mã màn hình,
Sáng đối chiếu các phản hồi với hợp đồng API. Chưa có kết quả rà soát
hoặc kiểm thử khả dụng được ghi nhận trong tài liệu này.

Trước khi giao frontend cần xác nhận ma trận trình duyệt/kích thước,
thiết kế liên kết email, fixture UTF-16, hành vi bàn phím/IME và cách xử lý
xung đột. Chính sách đếm/tìm kiếm và phím gửi đã chốt tại DEC-068/069/071. Wireframe cộng đồng là đầu ra riêng tại
[SCDC-UX-COM-001](community.md#ux).

<a id="contracts"></a>

## 4. Hợp đồng API, dữ liệu và đồng bộ đề xuất

Thiết kế bên dưới cụ thể hóa hành vi đã chốt, chưa phải API đã triển khai hoặc đã duyệt. Tài khoản sở hữu danh tính/phiên; Messaging sở hữu hội thoại/tin/thao tác. Giao tiếp qua `SCDC.Contracts` trong Modular Monolith; DM không đọc bảng tài khoản và không phụ thuộc Community.

[OpenAPI dự thảo](../contracts/direct-messaging.openapi.json) biểu diễn schema HTTP bên dưới; trạng thái `design-draft`, endpoint chưa triển khai. REST/SignalR, UUID và cursor bảo vệ đã chốt DEC-081; mã lỗi/schema vẫn cần rà soát cùng mock/thử nghiệm. Giới hạn UTF-16 được ghi bằng extension, không dùng minLength/maxLength để thay thế validation server.

Prefix đề xuất `/api/v1`. API hiện tại của Accounts ở [đặc tả tài khoản](accounts.md#api-current). Lỗi DM dùng [ProblemDetails chung](../architecture.md#contracts) theo DEC-061, validation 400; mã lỗi DM được nêu là đề xuất, chưa phải mã đang hoạt động.

`sequence`/`version` truyền dưới dạng chuỗi số nguyên; thời điểm theo UTC. Actor lấy từ phiên. ID/cursor/transport đã chọn DEC-081; schema, mock và mapping SQL cần rà soát trước triển khai.

<a id="contract-4"></a>

### Hợp đồng 4 — Đối tượng dữ liệu và ràng buộc

| Đối tượng | Trường chính đề xuất | Ràng buộc |
|---|---|---|
| UserSummary | `id, username, displayName` | Không trả email, phiên hoặc trạng thái bảo mật |
| DirectConversation | `id, participantLowId, participantHighId, createdAt, lastSequence` | Đúng hai người khác nhau; cặp đã chuẩn hóa duy nhất; tạo đồng thời trả cùng hội thoại |
| Message | `id, conversationId, authorId, clientMessageId, createSequence, version, content, createdAt, editedAt, deletedAt` | Tác giả thuộc hội thoại; sequence tạo tin tăng theo thứ tự commit trong hội thoại |
| SendOperation | `conversationId, authorId, clientMessageId, requestFingerprint, messageId` | Duy nhất theo ba trường đầu; cùng transaction với tin; còn tồn tại sau khi tin bị xóa |
| NotificationOutbox | `eventId, type, conversationId, messageId, version, occurredAt` | Cùng transaction với thay đổi tin; chỉ tham chiếu, không lưu bản nội dung cũ |

Hai người được sắp thứ tự ID bằng một quy tắc nhất quán ở mọi writer
trước khi áp unique pair. Không tạo membership thứ ba hoặc hội thoại
nhóm bằng endpoint DM. Chủ ý gửi hai tin cùng nội dung dùng hai
`clientMessageId` khác nhau và tạo hai tin.

Sửa tin thay nội dung hiện hành và tăng `version`; không tạo bảng lịch
sử nội dung cũ (DEC-052). Xóa tin đặt `content=null`, giữ tombstone, ID,
tác giả, thời điểm và khóa thao tác; tăng `version`. Không lưu nội dung
tin trong log, audit hoặc payload sự kiện để vô tình tạo lịch sử sửa.
Chính sách dữ liệu trong sao lưu, thời hạn giữ tin và xóa tài khoản vẫn
thuộc OQ-011; quyết định “chỉ giữ bản mới nhất” không phải cam kết xóa
ngay mọi bản sao lưu hoặc dữ liệu người nhận đã tự sao chép.

<a id="contract-5"></a>

### Hợp đồng 5 — API DM đề xuất

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/search?q=...&cursor=...&limit=...` | Một phần tên tài khoản/tên hiển thị | `200 {items: UserSummary[], nextCursor}`; không có kết quả trả mảng rỗng |
| `POST /direct-conversations` | `{ peerUserId }` | `200 { id, participants }`; tạo hoặc lấy hội thoại duy nhất của cặp |
| `GET /direct-conversations?cursor=...&limit=...` | Con trỏ danh sách | Các hội thoại của chính người gọi; không trả hội thoại người khác |
| `GET /direct-conversations/{id}/messages` | `before`, hoặc `after` với mốc `through`; `limit` | Trang tin; cấu trúc và thứ tự ở [hợp đồng lịch sử](#contract-8) |
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

<a id="contract-6"></a>

### Hợp đồng 6 — Chuẩn hóa và kiểm tra nội dung

Theo DEC-053/068, tin tối đa 2.000 đơn vị UTF-16, nhận xuống dòng và emoji, từ chối tin chỉ có khoảng trắng. Phép đếm đã chốt; chuẩn hóa và bộ ký tự trắng/vô hình bên dưới là thiết kế cần kiểm chứng:

1. Chuẩn hóa CRLF/CR thành LF; không cắt khoảng trắng đầu/cuối của một
   tin có nội dung và không diễn giải HTML/Markdown.
2. Đếm đơn vị UTF-16 sau chuẩn hóa xuống dòng bằng `.Length` của .NET
   và `string.length` của JavaScript. LF tính một đơn vị; emoji ngoài BMP
   tính hai, emoji ghép/ký tự tổ hợp có thể tính nhiều đơn vị. Server
   quyết định hợp lệ; client không được cắt giữa một cặp surrogate.
3. Tin trống hoặc chỉ gồm ký tự khoảng trắng/xuống dòng bị từ chối.
   Bộ ký tự trắng/vô hình và cách xử lý surrogate không hợp lệ phải được khóa
   cùng dữ liệu kiểm thử trước khi hoàn tất thiết kế chi tiết.
4. Sửa tin áp dụng cùng giới hạn. Vượt giới hạn trả `400 CONTENT_TOO_LONG`;
   trống trả `400 CONTENT_EMPTY`; không lưu tin hoặc phát sự kiện.

Trường `requestFingerprint` tính từ nội dung đã chuẩn hóa và ngữ cảnh
thao tác, dùng dấu vân tay có khóa để đối chiếu, không giữ bản văn bản
cũ trong bảng chống trùng. Phải quản lý phiên bản/khóa đủ lâu để các
yêu cầu thử lại còn đối chiếu được; chọn thuật toán ở thiết kế chi tiết.

<a id="contract-7"></a>

### Hợp đồng 7 — Gửi, thử lại và sửa/xóa đồng thời

```mermaid
sequenceDiagram
    participant UI as Giao diện
    participant DM as Nhắn tin
    participant ACC as Tài khoản
    participant DB as Kho dữ liệu Nhắn tin
    participant W as Bộ phát cập nhật
    UI->>DM: Gửi content + clientMessageId
    DM->>ACC: Kiểm tra phiên và điều kiện tài khoản
    ACC-->>DM: Cho phép hoặc từ chối
    DM->>DM: Kiểm tra đúng người tham gia
    DM->>DB: Transaction + tra khóa thao tác
    alt Khóa đã tồn tại, cùng nội dung gửi ban đầu
        DB-->>DM: Tin hiện hành hoặc tombstone
    else Khóa mới
        DM->>DB: Cấp sequence; ghi tin + khóa + outbox
        DM->>DB: Commit
    end
    DM-->>UI: Tin đã lưu
    W->>DB: Đọc sự kiện đã commit và trạng thái tin hiện hành
    W-->>UI: Cập nhật cho hai người có phiên hợp lệ
```

- Nếu cùng khóa nhưng payload khác, trả `409 OPERATION_CONFLICT`.
  Không lấy nội dung đã sửa để so với nội dung gửi ban đầu.
- Unique constraint và transaction ngăn hai request cùng khóa tạo hai
  tin. Mọi writer phải tuần tự hóa cấp sequence theo từng hội thoại để
  thứ tự sequence cũng là thứ tự commit của tin mới.
- Response mất sau commit: lần thử lại trả cùng `messageId`. Tin đã
  bị sửa/xóa: trả trạng thái hiện hành, không hồi sinh nội dung cũ.
- Giữ khóa chống trùng suốt vòng đời dữ liệu hội thoại, kể cả tombstone.
  Không tự hết hạn khóa sau vài giờ trong khi UI còn cho thử lại. Việc
  thanh lọc toàn hội thoại cần chốt với chính sách lưu giữ ở OQ-011.
- Trước khi sửa/xóa, kiểm tra lại tác giả và quyền truy cập. Sai version
  trả xung đột; giao diện tải bản hiện hành, không ghi đè âm thầm. Sửa
  tin đã xóa trả `409 MESSAGE_DELETED`; xóa lặp vẫn phải kiểm tra tác giả.
- Client không tự gửi lại POST khi có mạng hoặc khi làm mới phiên.
  Tải lịch sử/kết nối lại được tự thực hiện; thử lại một tin lỗi do người
  gửi bấm, theo DEC-021. Nếu thư viện HTTP tự retry mutation phải tắt
  hành vi đó cho luồng gửi tin.

<a id="contract-8"></a>

### Hợp đồng 8 — Lịch sử và kết nối thời gian thực

Trang lịch sử đề xuất mặc định 50, tối đa 100 tin. Lần mở đầu lấy
trang tin mới nhất; `before` lấy tối đa `limit` tin gần nhất nằm trước
con trỏ, sau đó sắp tăng theo `sequence` để hiển thị; không được kết hợp
`before` và `after` trong cùng request. Response:

```text
{ items, nextCursor, hasMore, throughSequence }
```

Lần bắt đầu bù tin, server chốt `throughSequence` là sequence tin mới
cao nhất đã commit. Các trang tiếp theo giữ cùng mốc và lấy
`after < sequence <= throughSequence`. Cursor chỉ tiến sau khi client
đã merge thành công cả trang; không lấy sequence cao nhất nhận qua
realtime làm bằng chứng đã đọc đủ lịch sử. So sánh version/sequence
theo giá trị số nguyên chính xác, không so theo thứ tự từ điển. Không suy số tin chưa đọc
từ hiệu hai sequence; trạng thái đã đọc chưa thuộc đợt này.

Hợp đồng nhận cập nhật đề xuất: đăng ký hội thoại đã có quyền; thông báo
`MessageChanged` chứa DTO trạng thái hiện hành và version. Client merge
theo `messageId`, chỉ áp dụng version mới hơn. Dispatcher có thể gửi
lặp hoặc gộp nhiều sửa đổi bằng cách đọc bản mới nhất; không cần lưu
các bản nội dung cũ trong sự kiện. “Đã gửi” chỉ có nghĩa đã lưu tin,
không chứng minh người nhận đã nhận thông báo.

Quy trình mở/reconnect:

1. Đăng ký handler và bật bộ đệm sự kiện trước khi đăng ký hội thoại.
2. Server kiểm tra phiên và người tham gia trước khi cho nhận cập nhật.
3. Tải/bù hết các trang tin mới tới mốc đã chốt, merge tin theo ID/version.
4. Tải lại các trang lịch sử đang hiển thị để nhận sửa/xóa tin cũ; bỏ
   hiệu lực cache trang cũ chưa hiển thị, tải lại khi người dùng cuộn tới.
5. Merge sự kiện đã đệm và tiếp tục nhận cập nhật. Tin lỗi vẫn chờ bấm thử lại.

Cursor tin mới không phát hiện được việc sửa/xóa một tin cũ. Vì vậy không
được bỏ bước 4 hoặc coi cache lịch sử offline là nguồn chính xác. Dữ liệu
đã cache phải tách theo tài khoản và được xóa khỏi phiên giao diện khi
đăng xuất/đổi tài khoản; chính sách lưu trên thiết bị chờ rà soát tài khoản.

<a id="contract-9"></a>

### Hợp đồng 9 — Lỗi, thu hồi phiên và quan sát

Outbox chỉ được phát sau commit. Worker lỗi được thử lại; handler nhận
lặp không tạo thêm tin. Phát cập nhật cần kiểm tra phiên/người nhận còn
hợp lệ; đăng xuất hoặc thu hồi phiên phải ngừng định tuyến kết nối cũ,
không chỉ gửi yêu cầu tự đăng xuất cho giao diện. Thời hạn thu hồi đã chốt ≤5 giây (DEC-083), transport SignalR (DEC-081); cơ chế retry worker, cảnh báo backlog và đáp ứng ngưỡng vẫn phải thử nghiệm.

Log tối thiểu: request ID, mã lỗi, thời lượng, ID thao tác/tin khi phù
hợp; không ghi mật khẩu, token, email link hoặc nội dung chat. Đo các
kịch bản commit rồi mất response, hai request đồng thời, worker dừng,
sự kiện trùng/đảo thứ tự, reconnect và tài khoản thứ ba truy cập.

### Thiết kế tích hợp theo DEC-081/083

- HTTP dùng Bearer như Identity hiện tại; mutation chỉ qua REST. SignalR `/hubs/chat` chỉ subscribe/unsubscribe và thông báo `MessageChanged`; không tạo đường gửi tin tự retry qua Hub.
- ID server UUIDv7, clientMessageId UUIDv4. Writer chuẩn hóa thứ tự cặp UUID theo thứ tự DB, có fixture đối chiếu; không dùng username hoặc displayName làm khóa hội thoại.
- Sequence được cấp dưới khóa row/counter hội thoại trong cùng transaction với tin, SendOperation và outbox; writer thứ hai chỉ cấp sau writer trước commit/rollback. Sequence/version truyền chuỗi số nguyên, client so sánh BigInt. Counter đề xuất ánh xạ `spaces.last_message_sequence`; không dùng identity toàn cục hiện tại làm mốc commit.
- Cursor dùng cơ chế bảo vệ có mã hóa và xác thực (đề xuất ASP.NET Core Data Protection), gồm actor/resource/hướng/filter/mốc through và vị trí sort. Hạn kỹ thuật đề xuất 24 giờ; key ring phải bền qua restart/deploy. Token không dùng được giữa hai user/resource; cursor lỗi/hết hạn trả validation, client tải lại lịch sử, không tự gửi mutation.
- Tìm người trim/NFC/chuyển chữ thường để tạo search key là thiết kế đề xuất; giữ dấu, khớp substring sau chuẩn hóa, username khớp đúng xếp trước rồi username/ID. Key hiển thị/tên tài khoản trả vẫn theo hồ sơ; không chuẩn hóa NFC nội dung tin. Cần fixture Unicode để SQL/service cho cùng kết quả.
- Nội dung chuẩn hóa CRLF/CR thành LF rồi đếm UTF-16. Bộ khoảng trắng đề xuất dùng Unicode White_Space (`0009–000D`, `0020`, `0085`, `00A0`, `1680`, `2000–200A`, `2028–2029`, `202F`, `205F`, `3000`); từ chối chuỗi chỉ gồm tập này. Ký tự vô hình khác và surrogate không hợp lệ cần khóa fixture trước bàn giao; không âm thầm bỏ nội dung để vượt validation.
- Hub kiểm tra phiên/quyền hiện hành khi subscribe. Dispatcher không phát nội dung cho connection/space bị thu hồi; revoker định tuyến theo session ID/space ID, bỏ subscription và đóng connection phù hợp. Dùng outbox sau commit cộng đối soát tối đa 1 giây là thiết kế đề xuất để đạt deadline thu hồi 5 giây; lỗi kiểm tra quyền phải dừng phát, không tiếp tục dùng cache không còn xác nhận.

SignalR giữ principal của lúc kết nối và không tự phản ánh việc thu hồi phiên/quyền trong thời gian kết nối; vì vậy cần kiểm tra dữ liệu hiện hành và đóng/bỏ định tuyến connection. Bật `CloseOnAuthenticationExpiration` chỉ giải quyết hết hạn token, không thay thế thu hồi nghiệp vụ. Xem [tài liệu xác thực SignalR của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0). JavaScript dùng `accessTokenFactory`; query token của WebSocket chỉ được nhận đúng route Hub và bị loại khỏi log proxy/API. Map user routing theo `sub` ổn định, không displayName.

Interface phiên/thu hồi theo session là phần mở rộng cần thiết kế trong `SCDC.Contracts`: `IUserDirectory` hiện chỉ trả thông tin user, `IRealtimeAccessRevoker` hiện chỉ nhận user/space ID. Không đọc DB Identity trực tiếp từ Messaging để thay interface còn thiếu. Mọi cơ chế trên vẫn phải thử nghiệm rollback, concurrent commit, mất response, worker dừng và revoke khi reconnect.

### Đối chiếu SQL trước triển khai

Đọc schema ngày 2026-10-04; chưa có writer Messaging để kiểm chứng. Các dòng này là đầu việc kỹ thuật, không thay đổi SQL trong lần hoàn thiện docs.

| Mã | Schema hiện tại | Đầu ra cần có |
|---|---|---|
| DM-SQL-01 | `direct_conversations` unique `(user_low_id,user_high_id)`, kiểm tra low < high | Chọn cùng thứ tự UUID giữa writer/DB; tạo space/cặp cùng transaction; chứng minh mở đồng thời trả cùng DM |
| DM-SQL-02 | `messages.sequence_no` là identity toàn cục, cấp trước commit và có thể có khoảng trống | Thiết kế khóa/counter theo hội thoại bảo đảm thứ tự commit; identity hiện tại không chứng minh bù tin không sót |
| DM-SQL-03 | Unique `(space_id,author_user_id,client_message_id)` đã có | Bổ sung SendOperation/fingerprint còn tồn tại sau xóa; unique tin đơn thuần chưa giải quyết payload khác hoặc thử lại sau sửa/xóa |
| DM-SQL-04 | Constraint văn bản dùng `char_length` tối đa 10.000; text message yêu cầu content khác null | Ràng buộc 2.000 UTF-16 ở service; điều chỉnh constraint để tombstone có content null; không dùng char_length thay phép đếm đã chốt |
| DM-SQL-05 | Có `message_edits.previous_content` trong schema/seed | Writer MVP không ghi nội dung cũ theo DEC-052; chọn migration/cleanup phù hợp trước dùng dữ liệu thật |
| DM-SQL-06 | Có outbox chung nhưng chưa có dispatcher Messaging | Transaction tin/khóa/outbox; sự kiện chỉ tham chiếu; dispatcher đọc bản hiện hành, merge ID/version, kiểm tra quyền/phiên |

Nguồn: [schema.sql](../../database/postgres/schema.sql). Các trường/schema cho nhóm chat, file, reactions/read state trong seed không tự mở rộng scope DM văn bản.

<a id="contract-10"></a>

### Hợp đồng 10 — Điều kiện chốt hợp đồng

Vg/Sáng rà soát mô hình ID, phiên, phép đếm ký tự, truy vấn tìm người,
thứ tự commit, dấu vân tay chống trùng và thu hồi kết nối. Thái đối
chiếu mỗi lỗi/trạng thái với [wireframe](direct-messaging.md#ux)
và [bộ ca kiểm thử](direct-messaging.md#tests).
Thay đổi đường dẫn/trường sau khi chốt phải cập nhật đồng thời mock,
frontend, backend và dữ liệu thử; không coi tài liệu này là bằng chứng
đã có API hoặc đã chạy thử nghiệm.

<a id="acceptance"></a>

## 5. Tiêu chí chấp nhận

Các tiêu chí dưới đây mô tả hành vi quan sát được, cần rà soát cùng các
[ngoại lệ](#exceptions) và [nội dung còn mở](#gaps) trước khi xác nhận đặc tả.

| Mã | Tình huống kiểm tra | Kết quả mong đợi |
|---|---|---|
| AC-DM-01 | Người dùng đã đăng nhập tìm bằng một phần tên tài khoản hoặc tên hiển thị, rồi chọn kết quả. | Mở được hội thoại riêng với đúng tài khoản mà không cần quan hệ kết bạn hoặc cùng cộng đồng. |
| AC-DM-02 | Một người gửi tin văn bản và hệ thống lưu thành công. | Người gửi thấy trạng thái “Đã gửi”; người nhận thấy nội dung khi mở hội thoại. |
| AC-DM-03 | Hai người đóng rồi mở lại hội thoại sau khi đã trao đổi. | Lịch sử tin nhắn còn hiệu lực được hiển thị. |
| AC-DM-04 | Người gửi sửa một tin cũ của mình. | Nội dung mới và dấu “Đã sửa” hiển thị cho cả hai người, kể cả khi tin được gửi từ trước. |
| AC-DM-05 | Người gửi xóa một tin của mình. | Cả hai người thấy dòng “Tin nhắn đã bị xóa” tại vị trí tin đó. |
| AC-DM-06 | Lần gửi tin thất bại. | Tin được báo lỗi; người gửi có thể bấm thử lại. |
| AC-DM-07 | A gửi tin khi B chưa mở ứng dụng; hệ thống đã lưu tin. Sau đó B mở ứng dụng và hội thoại với A. | B thấy tin đã lưu trong lịch sử hội thoại. |
| AC-DM-08 | Kết quả gửi lần đầu không rõ; người gửi bấm “Thử lại” cho chính tin đó. | Hội thoại chỉ có một tin tương ứng với thao tác gửi. |
| AC-DM-09 | Tài khoản chưa xác minh email thử gửi tin riêng. | Hệ thống từ chối gửi và chỉ dẫn bước xác minh email. |
| AC-DM-10 | Sửa một tin nhiều lần rồi mở lại hội thoại trên hai thiết bị. | Chỉ nội dung mới nhất và dấu “Đã sửa” được cung cấp; không có API/giao diện đọc phiên bản cũ. |
| AC-DM-11 | Gửi/sửa tin gồm 2.000/2.001 đơn vị UTF-16, nhiều dòng, emoji, tin chỉ khoảng trắng. | Nhận đến 2.000; từ chối vượt/trống; client/server đếm đúng DEC-068. |
| AC-DM-12 | Người thứ ba biết ID hội thoại/tin và gọi API đọc/gửi/sửa/xóa hoặc đăng ký nhận cập nhật. | Không được truy cập; phản hồi không tiết lộ nội dung hay người tham gia. |
| AC-DM-13 | Hai phía đồng thời mở hội thoại với nhau. | Cùng một hội thoại hai người; không tạo bản trùng hoặc hội thoại mồ côi. |
| AC-DM-14 | Hai phiên cùng sửa hoặc sửa/xóa một tin từ cùng phiên bản cũ. | Không ghi đè âm thầm; yêu cầu thua tranh chấp nhận xung đột và có thể tải trạng thái hiện hành. |
| AC-DM-15 | B mất mạng; A sửa/xóa tin cũ; B kết nối lại và xem tin đó. | B thấy nội dung hiện hành hoặc dòng thay thế, không tiếp tục dùng bản cache cũ làm kết quả chính xác. |
| AC-DM-16 | Tìm với từ khóa ở biên 1/2/64/65, khác hoa/thường/dấu và nhiều trang kết quả. | Giới hạn theo DEC-069; giữ phân biệt dấu; username khớp đúng được ưu tiên; không lộ email hay trả chính người tìm. |
| AC-DM-17 | Gửi bằng Enter/Shift+Enter trên desktop và bàn phím điện thoại; nhập tiếng Việt bằng IME. | Desktop Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng; nút Gửi luôn gửi; composition không vô tình gửi tin. |
| AC-DM-18 | Mở lại lịch sử tin cũ chưa bị người gửi xóa. | Tin không bị mất chỉ vì hết một thời hạn tự động; backup/xóa tài khoản được kiểm chứng riêng. |

AC-DM-12 đến AC-DM-15 cụ thể hóa bảo vệ hội thoại và hành vi đồng thời
để rà soát; chưa phải kết quả kiểm thử đã đạt. AC-DM-16–18 cụ thể hóa DEC-069–071.

<a id="tests"></a>

## 6. Dữ liệu và ca kiểm thử

### Dữ liệu và môi trường cần chuẩn bị

| Mã dữ liệu | Mục đích |
|---|---|
| A, B | Hai tài khoản đã xác minh, không cùng cộng đồng, có tên hiển thị trùng nhưng tên tài khoản khác |
| C | Tài khoản thứ ba, không tham gia DM của A/B |
| U | Tài khoản chưa xác minh; kiểm tra cả xác minh và khôi phục mật khẩu |
| A1, A2 | Hai phiên của A để thử sửa/xóa đồng thời và thu hồi phiên |

Tạo dữ liệu thử riêng theo từng ca; không dùng thông tin người thật.
Môi trường cần email thử để nhận/xác minh liên kết, hai phiên trình duyệt
độc lập, khả năng ngắt kết nối hoặc làm mất phản hồi sau commit, và quan
sát dữ liệu/sự kiện bằng quyền kiểm thử. Không đưa API tạo lỗi vào sản
phẩm công khai. Thái chuẩn bị dữ liệu; Vg/Sáng rà soát cơ chế tạo lỗi và
bằng chứng kiểm tra giao dịch.

### DM: hành vi, giao dịch và khôi phục

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-DM-01 | A tìm một phần tên của B; chọn giữa hai người trùng tên hiển thị | Phân biệt bằng tên tài khoản, mở đúng người; không cần kết bạn/cùng cộng đồng | AC-DM-01, AC-ACC-03 |
| TC-DM-02 | A và B đồng thời mở DM với nhau | Cùng một ID hội thoại, đúng hai người, không có dữ liệu mồ côi | AC-DM-13 |
| TC-DM-03 | A gửi; B đang mở; đóng rồi mở lại cả hai phiên | “Đã gửi” sau lưu; cùng nội dung/ID; lịch sử còn sau mở lại | AC-DM-02, AC-DM-03 |
| TC-DM-04 | B không mở ứng dụng khi A gửi; B mở lại | Thấy tin đã lưu; không phụ thuộc sự kiện realtime lúc B vắng mặt | AC-DM-07 |
| TC-DM-05 | Làm lỗi trước commit; mạng trở lại | Không có tin trong DB hoặc sự kiện; UI giữ tin lỗi, không tự gửi lại | AC-DM-06 |
| TC-DM-06 | Commit thành công nhưng mất response; bấm thử lại nhiều lần, gồm hai request đồng thời | Chỉ một tin/ID và một thao tác tạo tin; UI chỉ một dòng sau merge | AC-DM-08 |
| TC-DM-07 | Cùng khóa gửi nhưng payload khác; sau đó gửi cùng nội dung với khóa mới | Trường hợp đầu xung đột; trường hợp sau là tin mới hợp lệ | [hợp đồng gửi/đồng thời](#contract-7), AC-DM-08 |
| TC-DM-08 | Tin gửi đã sửa/xóa; thử lại thao tác gửi ban đầu | Cùng ID, trả nội dung hiện hành/tombstone; không phục hồi bản cũ | DM-009, DM-011; [hợp đồng gửi/đồng thời](#contract-7) |
| TC-DM-09 | A sửa tin nhiều lần trên A1; B và A2 mở lại | Chỉ nội dung mới nhất, dấu “Đã sửa”; không có endpoint hoặc dữ liệu lịch sử nội dung sửa | AC-DM-04, AC-DM-10 |
| TC-DM-10 | A xóa tin; thử xóa lại và thử sửa tin đã xóa | Hai bên thấy dòng thay thế; xóa lặp an toàn; sửa bị từ chối | AC-DM-05; [hợp đồng gửi/đồng thời](#contract-7) |
| TC-DM-11 | A1/A2 sửa cùng version; lặp với một sửa và một xóa | Chỉ thao tác hợp lệ theo version được áp dụng; bên còn lại nhận xung đột, không ghi đè âm thầm | AC-DM-14 |
| TC-DM-12 | C gọi trực tiếp các API và đăng ký nhận cập nhật với ID DM/tin của A/B | Không đọc/gửi/sửa/xóa/nhận tin; không lộ người tham gia | AC-DM-12 |
| TC-DM-13 | B sửa/xóa tin của A qua API, không chỉ qua UI | Máy chủ từ chối; tin không đổi | DM-004, DM-005, ACL-05 |
| TC-DM-14 | U gửi trực tiếp qua API | Từ chối dù client giả quyền hoặc trạng thái xác minh | AC-DM-09 |
| TC-DM-15 | Hai request gửi tin khác nhau; giữ transaction thứ nhất chưa commit trong khi request thứ hai chạy | Phân trang/bù tin không bỏ sót bản commit trễ; sequence trong hội thoại tuân thứ tự commit | [gửi/đồng thời](#contract-7) và [lịch sử](#contract-8), AC-DM-03 |
| TC-DM-16 | Dừng worker sau commit rồi bật lại; phát sự kiện trùng/đảo thứ tự | Lịch sử vẫn có tin; cập nhật phục hồi; client merge theo ID/version, không nhân đôi hoặc quay lại bản cũ | AC-DM-02, AC-DM-03; [hợp đồng lịch sử/thu hồi](#contract-8) |
| TC-DM-17 | B offline; A gửi hơn một trang tin mới và sửa/xóa tin cũ đang nằm trong cửa sổ B từng xem | B bù hết trang mới, tải lại tin cũ và bỏ cache hết hiệu lực; không sót tin hoặc giữ nội dung đã xóa | AC-DM-15 |

Ca kiểm tra nội dung cũ chỉ xét dữ liệu nghiệp vụ đang phục vụ, sự kiện
và log trong thiết kế; vòng đời bản sao lưu phải kiểm chứng riêng sau
khi chốt OQ-011. Không coi thu hồi bản người nhận tự sao chép là điều
kiện đạt của xóa/sửa tin.

### Dữ liệu biên nội dung

| Mã ca | Dữ liệu | Kết quả mong đợi theo DEC-053 |
|---|---|---|
| TC-TEXT-01 | Chuỗi rỗng, dấu cách, tab hoặc chỉ xuống dòng | Bị từ chối; không lưu và không phát sự kiện |
| TC-TEXT-02 | 1, 1.999, 2.000 và 2.001 đơn vị UTF-16 | Ba giá trị đầu được nhận, 2.001 bị từ chối |
| TC-TEXT-03 | Tiếng Việt có dấu, ký tự tổ hợp, emoji đơn và emoji ghép | Hiển thị đúng; đếm nhất quán client/server theo UTF-16 đã chốt tại DEC-068 |
| TC-TEXT-04 | CRLF và LF cùng nội dung; khoảng trắng đầu/cuối của tin có chữ | Chuẩn hóa xuống dòng; giữ khoảng trắng có chủ ý; không sinh xung đột chống trùng do chuẩn hóa khác nhau |
| TC-TEXT-05 | Nội dung trông như HTML/script và ký tự đặc biệt | Hiển thị như văn bản, không chạy mã hoặc diễn giải thành giao diện |
| TC-TEXT-06 | Lặp các dữ liệu trên khi sửa tin và gửi tin phòng | Cùng quy tắc nội dung, không có đường bỏ qua giới hạn |

Dữ liệu UTF-16 cố định: `a` = 1; `ế` dựng sẵn = 1; `e` + dấu sắc tổ hợp = 2; `😀` = 2; `👩‍💻` = 5; LF = 1. Chuỗi 1.000 `😀` có độ dài 2.000; 1.001 có độ dài 2.002. Chuẩn hóa CRLF thành LF trước khi kiểm tra theo thiết kế đề xuất; không tự chuẩn hóa NFC hoặc cắt khoảng trắng của tin có nội dung. Fixture này dùng cho client/backend/SQL, không yêu cầu hiển thị mỗi emoji là một đơn vị.

Fixture máy đọc được: [text-validation.json](../fixtures/text-validation.json); độ dài đã kiểm tra bằng UTF-16, chưa chạy qua API/client.

Bổ sung TC-DM-18 cho biên tìm kiếm/hoa thường/dấu/cursor theo AC-DM-16, TC-DM-19 cho bàn phím và IME theo AC-DM-17, TC-DM-20 cho tin không tự hết hạn theo AC-DM-18. Các ca này cũng ở trạng thái Chưa chạy.

Các ca TEXT áp dụng AC-DM-11 và AC-COM-24. Thái chuẩn bị fixture cố định
với số đơn vị UTF-16 kỳ vọng; Vg/Sáng khóa bộ ký tự trắng/vô hình trước
khi dùng fixture để kết luận đạt.

### Ma trận giao diện và cách ghi kết quả

Theo DEC-059, chạy hành trình tài khoản/DM trên desktop và trình duyệt
điện thoại; có kiểm tra đổi chiều màn hình, bàn phím ảo, cuộn lịch sử,
focus khi lỗi và menu sửa/xóa. Hai kích thước wireframe là dữ liệu thiết
kế, chưa thay thế danh sách OS/thiết bị và phiên bản cụ thể của ma trận DEC-082 tại nghiệm thu.

Mỗi lần chạy ghi: mã ca, build, cấu hình, dữ liệu, trình duyệt/thiết bị,
bước tái hiện, kỳ vọng, thực tế, bằng chứng, người thực hiện và lỗi liên
quan. Trạng thái ban đầu của **mọi ca là Chưa chạy**; ca phụ thuộc đề
xuất chưa xác nhận thêm ghi chú “Chờ chốt quy tắc”, không tính là đạt.

<a id="gaps"></a>

## 7. Vấn đề còn mở

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Đã chốt độ dài, phân trang và khớp tại DEC-069; còn schema/cursor và kiểm chứng truy vấn. | OQ-005 |
| Lưu giữ | Không tự hết hạn tin theo DEC-070; backup 30 ngày đã chốt DEC-086; còn hành vi xóa tài khoản và khôi phục sau xóa; không giữ lịch sử sửa đã chốt. | OQ-005, OQ-011 |
| Thử lại/đồng thời | Rà soát và thử nghiệm hợp đồng chống trùng, khóa theo hội thoại, xung đột sửa/xóa và dọn dữ liệu. | OQ-005, OQ-008 |
| Giới hạn nội dung | Đã chốt 2.000 UTF-16, xuống dòng/emoji và từ chối trống; còn bộ ký tự trắng/vô hình và fixture dùng chung. | OQ-005 |
| Chất lượng | Ngưỡng và ma trận đã chốt DEC-082/083; còn cấu hình/build/thiết bị và kết quả đo. | OQ-007 |

Đã bổ sung [OpenAPI dự thảo](../contracts/direct-messaging.openapi.json) ngày 2026-10-04; chưa xác nhận thiết kế hoặc có mock/API/Hub chạy được. Schema dùng `x-scdc-utf16-length` vì minLength/maxLength của JSON Schema không tự biểu diễn phép đếm UTF-16. `clientMessageId` UUIDv4, ID server UUIDv7 theo DEC-081; ví dụ là dữ liệu minh họa, không phải ID của dữ liệu thật. Cách biểu diễn SQL hiện tại dùng chat space/`sequence_no`; thiết kế logic dùng conversation/sequence. Cần rà soát ánh xạ, unique key theo tác giả và commit order trước triển khai, không coi seed/schema hiện tại là đã chứng minh hợp đồng đề xuất.

Các quyết định chưa chốt được giữ ở OQ-005/OQ-007/OQ-008/OQ-011. Chọn framework hoặc mô hình lưu không thay thế việc kiểm chứng lost response, concurrent commit, worker dừng và reconnect.

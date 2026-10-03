# SCDC — Hợp đồng và dữ liệu đề xuất cho tài khoản, DM

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-API-DM-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Thiết kế hợp đồng để rà soát — chưa phải API đã triển khai hoặc được duyệt |
| Căn cứ | [Tài khoản](../03-requirements/03-accounts.md), [DM](../03-requirements/01-direct-messaging.md), [ma trận quyền](../03-requirements/05-access-control-matrix.md), [phác thảo kiến trúc](01-solution-outline.md) |

## 1. Ranh giới và cách sử dụng

Tài liệu cụ thể hóa gói tài khoản + DM để backend, frontend và kiểm thử
thống nhất một hợp đồng. Các đường dẫn, trường và cơ chế bên dưới là
**đề xuất kỹ thuật**, không mô tả mã nguồn ngoài `docs/project` và không
chọn framework hay nhà cung cấp. Kiến trúc microservices vẫn theo DEC-007.

Tài khoản sở hữu danh tính, thông tin xác minh, phiên và kênh email.
Nhắn tin sở hữu hội thoại, tư cách tham gia, tin và kết quả thao tác gửi.
Nhắn tin hỏi dịch vụ tài khoản qua hợp đồng nội bộ; không truy vấn bảng
tài khoản. DM không phụ thuộc dịch vụ cộng đồng hoặc quyền phòng.

## 2. Quy ước chung

- Prefix đề xuất `/api/v1`. ID là chuỗi định danh ổn định, không dùng email
  hoặc tên hiển thị làm khóa. Actor luôn lấy từ phiên đã xác thực.
- Thời điểm dùng chuỗi ISO 8601 theo UTC. `sequence` và `version` truyền
  dưới dạng chuỗi số nguyên; client không chuyển sang số dấu phẩy động.
- Lỗi trả `{ code, message, fieldErrors?, requestId }`. Không đưa stack
  trace, token hoặc nội dung tin vào lỗi/log; giao diện ánh xạ theo `code`.
- `401`: cần đăng nhập lại; `403`: thao tác bị chặn khi được phép biết tài
  nguyên; `404`: không tồn tại hoặc không được biết hội thoại/tin đó;
  `409`: xung đột phiên bản/khóa thao tác; `422`: dữ liệu không hợp lệ;
  `429`: vượt giới hạn, có thời gian thử lại; `503`: phụ thuộc tạm lỗi.
- Kiểm tra quyền trước khi trả dữ liệu, kể cả trả kết quả của yêu cầu gửi
  lặp. Lỗi dịch vụ tài khoản không được diễn giải thành cho phép truy cập.
- Chưa xác minh email không có phiên truy cập các chức năng ứng dụng;
  chỉ dùng luồng xác minh/khôi phục theo DEC-051. Các endpoint đăng ký,
  đăng nhập, xác minh và khôi phục được thiết kế riêng để tiếp cận luồng này.

## 3. Hợp đồng tài khoản tối thiểu

| Method và đường dẫn đề xuất | Đầu vào | Kết quả chính | Luồng liên quan |
|---|---|---|---|
| `POST /auth/register` | `email, username, password` | `201`: thông báo cần xác minh; không cấp phiên ứng dụng | ACC-S01 |
| `POST /auth/verify-email` | Token dùng một lần nhận từ email | `204`: xác minh thành công; đăng nhập để mở ứng dụng | ACC-S03 |
| `POST /auth/resend-verification` | `email` | `202`: phản hồi chung, không tiết lộ tài khoản tồn tại | ACC-S03 |
| `POST /auth/login` | `login, password` | `200`: phiên + hồ sơ riêng; chưa xác minh trả `403 EMAIL_UNVERIFIED` | ACC-S02 |
| `POST /auth/forgot-password` | `email` | `202`: phản hồi chung cả khi không tìm thấy tài khoản | ACC-S04 |
| `POST /auth/reset-password` | Token dùng một lần + `newPassword` | `204`: đổi mật khẩu; tác động thu hồi phiên theo hồ sơ tài khoản | ACC-S05 |
| `POST /auth/refresh` | Bằng chứng phiên theo transport được chọn | Phiên mới hoặc `401 SESSION_INVALID` | Phiên hết hạn |
| `POST /auth/logout` | Phiên hiện tại | `204`: thu hồi phiên; lặp lại an toàn | Đăng xuất |
| `GET /users/me` | Phiên hợp lệ | Hồ sơ của chính người đang đăng nhập | Hồ sơ riêng |
| `PATCH /users/me` | Các trường hồ sơ đã được chốt | Hồ sơ sau cập nhật; không nhận actor hoặc quyền từ client | Hồ sơ riêng |

Transport phiên (cookie hay token), thời hạn, quy tắc gửi lại email và
bảo vệ endpoint cần được Vg/Sáng rà soát trước khi đóng gói tài khoản.
Không tự biến cách lưu phiên trong tài liệu khác thành quyết định ở đây.
Token email không được dùng làm phiên đăng nhập. Không ghi token email
vào log; email worker phải có cách nhận liên kết gửi được mà không
phải khôi phục token từ giá trị băm.

Đề xuất nội dung hồ sơ công khai tối thiểu: `id, username, displayName`;
email chỉ thuộc hồ sơ riêng. Bộ trường và quy tắc tên tài khoản tiếp tục
theo đặc tả tài khoản khi được xác nhận.

## 4. Đối tượng dữ liệu và ràng buộc

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

## 5. API DM đề xuất

| Method và đường dẫn | Đầu vào | Kết quả |
|---|---|---|
| `GET /users/search?q=...&cursor=...&limit=...` | Một phần tên tài khoản/tên hiển thị | `200 {items: UserSummary[], nextCursor}`; không có kết quả trả mảng rỗng |
| `POST /direct-conversations` | `{ peerUserId }` | `200 { id, participants }`; tạo hoặc lấy hội thoại duy nhất của cặp |
| `GET /direct-conversations?cursor=...&limit=...` | Con trỏ danh sách | Các hội thoại của chính người gọi; không trả hội thoại người khác |
| `GET /direct-conversations/{id}/messages` | `before`, hoặc `after` với mốc `through`; `limit` | Trang tin; cấu trúc và thứ tự ở mục 8 |
| `POST /direct-conversations/{id}/messages` | `{ clientMessageId, content }` | `200 MessageDto` sau commit, gồm cả lần gửi đầu và lần thử lại |
| `PATCH /direct-conversations/{id}/messages/{messageId}` | `{ content, expectedVersion }` | `200 MessageDto` với nội dung mới; sai version trả `409 VERSION_CONFLICT` |
| `DELETE /direct-conversations/{id}/messages/{messageId}?expectedVersion=...` | Phiên bản đang thấy | `200 MessageDto` dạng tombstone; xóa lại tin đã xóa trả cùng tombstone |

`MessageDto` gồm `id, conversationId, author: UserSummary, clientMessageId,
sequence, version, content, createdAt, editedAt, deletedAt`.

```json
{
  "clientMessageId": "operation-generated-by-client",
  "content": "Chào bạn!"
}
```

`clientMessageId` được tạo **một lần khi người dùng bấm gửi**, giữ nguyên
trong tin tạm và mọi lần “Thử lại”. Response và sự kiện có cùng ID này
để client thay tin tạm bằng đúng tin đã lưu. Ví dụ trên minh họa cấu trúc;
định dạng ID cụ thể sẽ được khóa trong rà soát hợp đồng.

Đề xuất giới hạn kỹ thuật cho tìm người: chuỗi tìm từ 2 đến 64 ký tự,
không phân biệt hoa/thường, không tự bỏ dấu; khớp một phần, ưu tiên khớp
đúng tên tài khoản, sau đó thứ tự tên tài khoản và ID để phân trang ổn
định. Trang mặc định 20, tối đa 50. Chỉ trả người đủ điều kiện nhận DM;
không trả chính người tìm. Các giá trị này là đề xuất cần rà soát OQ-005,
chưa phải quyết định sản phẩm. Không dùng endpoint tìm kiếm để lộ email.

## 6. Chuẩn hóa và kiểm tra nội dung

Theo DEC-053, tin tối đa 2.000 ký tự, nhận xuống dòng và emoji, từ chối
tin chỉ có khoảng trắng. Đề xuất cách đếm thống nhất:

1. Chuẩn hóa CRLF/CR thành LF; không cắt khoảng trắng đầu/cuối của một
   tin có nội dung và không diễn giải HTML/Markdown.
2. Đếm theo cụm ký tự hiển thị (grapheme): emoji ghép tính là một cụm;
   một LF tính là một ký tự. Client và server dùng cùng quy tắc, server
   là nơi quyết định hợp lệ. Không dùng số byte hoặc số đơn vị UTF-16.
3. Tin trống hoặc chỉ gồm ký tự khoảng trắng/xuống dòng bị từ chối.
   Bộ ký tự vô hình và phiên bản thuật toán tách cụm phải được khóa
   cùng dữ liệu kiểm thử trước khi hoàn tất thiết kế chi tiết.
4. Sửa tin áp dụng cùng giới hạn. Vượt giới hạn trả `422 CONTENT_TOO_LONG`;
   trống trả `422 CONTENT_EMPTY`; không lưu tin hoặc phát sự kiện.

Trường `requestFingerprint` tính từ nội dung đã chuẩn hóa và ngữ cảnh
thao tác, dùng dấu vân tay có khóa để đối chiếu, không giữ bản văn bản
cũ trong bảng chống trùng. Phải quản lý phiên bản/khóa đủ lâu để các
yêu cầu thử lại còn đối chiếu được; chọn thuật toán ở thiết kế chi tiết.

## 7. Gửi, thử lại và sửa/xóa đồng thời

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

## 8. Lịch sử và kết nối thời gian thực

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

## 9. Lỗi, thu hồi phiên và quan sát

Outbox chỉ được phát sau commit. Worker lỗi được thử lại; handler nhận
lặp không tạo thêm tin. Phát cập nhật cần kiểm tra phiên/người nhận còn
hợp lệ; đăng xuất hoặc thu hồi phiên phải ngừng định tuyến kết nối cũ,
không chỉ gửi yêu cầu tự đăng xuất cho giao diện. Thời hạn thu hồi, cơ
chế retry worker, cảnh báo backlog và transport realtime cần thử nghiệm
trước khi ký xác nhận kỹ thuật.

Log tối thiểu: request ID, mã lỗi, thời lượng, ID thao tác/tin khi phù
hợp; không ghi mật khẩu, token, email link hoặc nội dung chat. Đo các
kịch bản commit rồi mất response, hai request đồng thời, worker dừng,
sự kiện trùng/đảo thứ tự, reconnect và tài khoản thứ ba truy cập.

## 10. Điều kiện chốt hợp đồng

Vg/Sáng rà soát mô hình ID, phiên, phép đếm ký tự, truy vấn tìm người,
thứ tự commit, dấu vân tay chống trùng và thu hồi kết nối. Thái đối
chiếu mỗi lỗi/trạng thái với [wireframe](../04-ux/02-account-dm-wireframes.md)
và [bộ ca kiểm thử](../07-quality/02-account-dm-test-cases.md).
Thay đổi đường dẫn/trường sau khi chốt phải cập nhật đồng thời mock,
frontend, backend và dữ liệu thử; không coi tài liệu này là bằng chứng
đã có API hoặc đã chạy thử nghiệm.

## 11. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Đề xuất hợp đồng tài khoản/DM, quyền sở hữu dữ liệu, chống trùng, phiên bản tin, lịch sử và đồng bộ khi reconnect. |

[Mục lục hồ sơ](../README.md)

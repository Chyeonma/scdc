# SCDC — Đặc tả nhắn tin riêng giữa hai người, đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-DM-001 |
| Phiên bản | 0.5 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Bản nháp — chờ làm rõ quy tắc và xác nhận tiêu chí chấp nhận |
| Căn cứ | REQ-005, SCP-005, SUC-003 và [SCDC-DIS-001](../02-discovery/01-users-and-needs.md) |

## 1. Mục tiêu và phạm vi đợt đầu

Người dùng đã đăng nhập có thể tìm một người khác, bắt đầu hội thoại
riêng, gửi và xem lại tin nhắn văn bản. Người gửi có thể sửa hoặc xóa tin
của mình. Tin nhắn và lịch sử hội thoại được lưu để xem lại theo SCP-005
và SUC-003 trong [Project Brief](../01-initiation/01-project-brief.md).

Đợt đầu ưu tiên tin nhắn văn bản. Hình ảnh, file tài liệu và cách xử lý
khi người nhận không muốn nhận tin từ một tài khoản cụ thể được xếp vào
đợt sau theo DEC-017 và DEC-018. Việc chọn đợt triển khai không thay đổi
phạm vi phiên bản đầu trong Project Brief.

## 2. Hành trình chính

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

## 3. Quy tắc đã xác định

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

Các quyết định DEC-* được ghi tại
[sổ quyết định](../01-initiation/03-discovery-and-decision-log.md).

## 4. Tiêu chí chấp nhận dự thảo

Các tiêu chí dưới đây mô tả hành vi quan sát được, cần rà soát cùng các
quy tắc còn mở ở mục 5 trước khi xác nhận đặc tả.

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

## 5. Quy tắc cần làm rõ trước khi xác nhận

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Phân biệt các tài khoản trùng tên hiển thị; xử lý khi không tìm thấy. | OQ-005 |
| Sửa tin | Có giữ lịch sử các lần sửa hay không. | OQ-005 |
| Xóa tin | Người nhận đã xem trước đó có còn thấy nội dung cũ trong thông báo hoặc nơi khác hay không. | OQ-005 |
| Thử lại | Cơ chế kỹ thuật và thời hạn nhận diện cùng thao tác gửi; kết quả khi người dùng chủ ý gửi lại một tin mới có cùng nội dung. | OQ-005 |
| Giới hạn nội dung | Tin rỗng, độ dài tối đa, xuống dòng và ký tự đặc biệt. | OQ-005 |

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo bản nháp hành trình, quy tắc và tiêu chí chấp nhận cho đợt nhắn tin riêng đầu. |
| 0.2 | 2026-09-30 | Làm rõ tìm theo một phần tên, “Đã gửi” khi hệ thống lưu tin và dấu “Đã sửa”. |
| 0.3 | 2026-09-30 | Bổ sung quy tắc người nhận thấy tin đã lưu khi mở lại ứng dụng. |
| 0.4 | 2026-09-30 | Bổ sung yêu cầu thử lại không tạo tin trùng. |
| 0.5 | 2026-09-30 | Bổ sung điều kiện xác minh email trước khi gửi tin riêng. |

[Mục lục hồ sơ](../README.md)

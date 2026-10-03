# SCDC — Đặc tả nhắn tin riêng giữa hai người, đợt đầu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-DM-001 |
| Phiên bản | 0.6 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Đã bổ sung quy tắc nội dung và ngoại lệ; chờ rà soát thiết kế và tiêu chí chấp nhận |
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
| DM-011 | Chỉ giữ nội dung mới nhất sau khi sửa, không cung cấp lịch sử phiên bản cũ; vẫn hiện dấu “Đã sửa”. | DEC-052 |
| DM-012 | Mỗi tin tối đa 2.000 ký tự; cho xuống dòng và emoji; từ chối tin rỗng/chỉ có khoảng trắng. | DEC-053 |

Các quyết định DEC-* được ghi tại
[sổ quyết định](../01-initiation/03-discovery-and-decision-log.md).

## 4. Tiêu chí chấp nhận dự thảo

Các tiêu chí dưới đây mô tả hành vi quan sát được, cần rà soát cùng các
ngoại lệ ở mục 5 và nội dung còn mở ở mục 6 trước khi xác nhận đặc tả.

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
| AC-DM-11 | Gửi hoặc sửa tin gồm 2.000 và 2.001 ký tự, tin nhiều dòng, emoji, tin chỉ có khoảng trắng. | Chấp nhận đến 2.000; từ chối vượt giới hạn và tin trống; phép đếm client/server nhất quán theo thiết kế đã chốt. |
| AC-DM-12 | Người thứ ba biết ID hội thoại/tin và gọi API đọc/gửi/sửa/xóa hoặc đăng ký nhận cập nhật. | Không được truy cập; phản hồi không tiết lộ nội dung hay người tham gia. |
| AC-DM-13 | Hai phía đồng thời mở hội thoại với nhau. | Cùng một hội thoại hai người; không tạo bản trùng hoặc hội thoại mồ côi. |
| AC-DM-14 | Hai phiên cùng sửa hoặc sửa/xóa một tin từ cùng phiên bản cũ. | Không ghi đè âm thầm; yêu cầu thua tranh chấp nhận xung đột và có thể tải trạng thái hiện hành. |
| AC-DM-15 | B mất mạng; A sửa/xóa tin cũ; B kết nối lại và xem tin đó. | B thấy nội dung hiện hành hoặc dòng thay thế, không tiếp tục dùng bản cache cũ làm kết quả chính xác. |

AC-DM-12 đến AC-DM-15 cụ thể hóa bảo vệ hội thoại và hành vi đồng thời
để rà soát; chưa phải kết quả kiểm thử đã đạt.

## 5. Ngoại lệ và cách xử lý đề xuất

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
phân trang được mô tả tại [hợp đồng DM](../05-architecture/02-account-dm-contracts.md).
Đây là thiết kế đề xuất để kiểm chứng hành vi đã chốt, không chọn ngầm
công nghệ triển khai. Không lưu nội dung cũ trong lịch sử sửa, sự kiện
hoặc log; dữ liệu sao lưu tiếp tục theo chính sách OQ-011 cần xác định.

## 6. Quy tắc cần làm rõ trước khi xác nhận toàn bộ đặc tả

| Nội dung | Câu hỏi còn mở | Liên quan |
|---|---|---|
| Kết quả tìm kiếm | Chốt độ dài từ khóa, phân trang và cách khớp hoa/thường/dấu; cách phân biệt đã có đề xuất bằng tên tài khoản. | OQ-005 |
| Lưu giữ | Thời hạn giữ tin, sao lưu và hành vi khi tài khoản không còn sử dụng; không giữ lịch sử nội dung sửa đã chốt. | OQ-005, OQ-011 |
| Thử lại/đồng thời | Rà soát và thử nghiệm hợp đồng chống trùng, khóa theo hội thoại, xung đột sửa/xóa và dọn dữ liệu. | OQ-005, OQ-008 |
| Giới hạn nội dung | Đã chốt 2.000 ký tự, xuống dòng/emoji và từ chối trống; cần khóa phép đếm cụm ký tự và bộ dữ liệu biên dùng chung. | OQ-005 |
| Chất lượng | Độ trễ gửi/nhận, tải, thu hồi phiên và ma trận trình duyệt để nghiệm thu. | OQ-007 |

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo bản nháp hành trình, quy tắc và tiêu chí chấp nhận cho đợt nhắn tin riêng đầu. |
| 0.2 | 2026-09-30 | Làm rõ tìm theo một phần tên, “Đã gửi” khi hệ thống lưu tin và dấu “Đã sửa”. |
| 0.3 | 2026-09-30 | Bổ sung quy tắc người nhận thấy tin đã lưu khi mở lại ứng dụng. |
| 0.4 | 2026-09-30 | Bổ sung yêu cầu thử lại không tạo tin trùng. |
| 0.5 | 2026-09-30 | Bổ sung điều kiện xác minh email trước khi gửi tin riêng. |
| 0.6 | 2026-10-03 | Chốt nội dung mới nhất và giới hạn 2.000 ký tự; bổ sung ngoại lệ, tiêu chí bảo vệ/đồng thời và liên kết thiết kế DM. |

[Mục lục hồ sơ](../README.md)

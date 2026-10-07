# SCDC — Quy tắc và quyền DM

Nguồn hành vi riêng của DM; quy tắc kỹ thuật nội dung dùng chung tại [Messaging](../../../shared/messaging/text.md).

<a id="requirements"></a>

## Phạm vi, hành trình và quy tắc

### Mục tiêu và phạm vi đợt đầu

Người dùng đã đăng nhập có thể tìm một người khác, bắt đầu hội thoại
riêng, gửi và xem lại tin nhắn văn bản. Người gửi có thể sửa hoặc xóa tin
của mình. Tin nhắn và lịch sử hội thoại được lưu để xem lại theo SCP-005
và SUC-003 trong [Project Brief](../../../project/README.md#scope).

Đợt đầu ưu tiên tin nhắn văn bản. Hình ảnh, file tài liệu và cách xử lý
khi người nhận không muốn nhận tin từ một tài khoản cụ thể được xếp vào
đợt sau theo DEC-017 và DEC-018. Việc chọn đợt triển khai không thay đổi
phạm vi bản hoàn thiện v1 trong Project Brief.

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
| DM-014 | Tin không tự hết hạn trong v1; sửa/xóa theo quy tắc đã chốt. Account chưa có self-delete, khóa không xóa lịch sử; backup/restore theo chính sách vòng đời. | DEC-070/103/104/108/109 |
| DM-015 | Desktop Enter gửi, Shift+Enter xuống dòng; điện thoại Enter xuống dòng, nút Gửi gửi tin. | DEC-071 |
| DM-016 | Chuẩn hóa CRLF/CR thành LF trước đếm; từ chối UTF-16 lỗi và tin chỉ khoảng trắng/vô hình; giữ Unicode/ZWJ trong tin có nội dung. | DEC-090 |
| DM-017 | Bản nháp chưa Gửi chỉ ở bộ nhớ tab, giữ khi chuyển hội thoại; reload/đóng tab/logout mất bản nháp; không lưu nội dung xuống localStorage/IndexedDB. | DEC-091 |

Các quyết định DEC-* được ghi tại
[sổ quyết định](../../../decisions.md#decisions).

<a id="exceptions"></a>

### Ngoại lệ và cách xử lý đề xuất

| Tình huống | Kết quả để backend, frontend và QA rà soát |
|---|---|
| Không tìm thấy người | Danh sách rỗng và thông báo rõ; không tự tạo người nhận hoặc hội thoại |
| Trùng tên hiển thị | Hiển thị thêm tên tài khoản duy nhất; chọn bằng ID; không đưa email vào kết quả |
| Chọn chính mình | Không tạo DM một người; trả lỗi dữ liệu; tính năng ghi chú cá nhân không thuộc yêu cầu hiện tại |
| Mở DM đã có | Trả cùng hội thoại của cặp hai người, kể cả khi hai bên mở đồng thời |
| Người nhận bị khóa | Chặn gửi/gọi mới; actor active vẫn đọc lịch sử và sửa/xóa tin của mình theo quyền. Không self-delete v1; DEC-103/104 và [vòng đời dữ liệu](../../../data-lifecycle.md#account-state) |
| Mất phản hồi gửi | Giữ tin tạm; người gửi bấm thử lại cùng mã thao tác; chỉ một tin được lưu |
| Chủ ý gửi cùng nội dung lần nữa | Là thao tác mới với mã mới; được tạo tin mới, không chống trùng bằng nội dung đơn thuần |
| Thử lại tin đã sửa/xóa sau lần gửi đầu | Trả cùng ID với trạng thái hiện hành, không tạo lại nội dung gửi ban đầu |
| Sửa tin đã xóa | Từ chối; không phục hồi nội dung; xóa lặp của chính tác giả trả trạng thái đã xóa |
| Tin bị xóa sau khi đã được đọc | Bỏ nội dung khỏi dữ liệu đang phục vụ và giao diện khi đồng bộ; không cam kết thu hồi bản người nhận tự sao chép |
| Phiên hết hạn/mất mạng | Không gửi tin tự động; xác thực lại, đồng bộ lịch sử và để người gửi chủ động thử lại tin lỗi |

Các cơ chế ID thao tác, lưu giữ khóa chống trùng, phiên bản sửa/xóa và
phân trang được mô tả tại [hợp đồng DM](../design/README.md#contracts).
Đây là thiết kế đề xuất để kiểm chứng hành vi đã chốt, không chọn ngầm
công nghệ triển khai. Không lưu nội dung cũ trong lịch sử sửa, sự kiện
hoặc log; backup/restore theo [vòng đời dữ liệu](../../../data-lifecycle.md#restore), còn proof DATA-GAP.

<a id="permissions"></a>

## Quyền truy cập

| Mã | Thao tác | Điều kiện được phép | Trường hợp bị từ chối | Căn cứ |
|---|---|---|---|---|
| ACL-02 | Tìm người và mở DM | Đã đăng nhập sau xác minh; chọn đúng định danh người nhận | Chưa đăng nhập hoặc chưa xác minh | DEC-016, DEC-019, DEC-051 |
| ACL-03 | Đọc lịch sử DM | Là một trong hai người của hội thoại và phiên còn hợp lệ | Người thứ ba, kể cả người quản lý một cộng đồng mà hai bên tham gia | REQ-005, AC-ACC-04; suy ra từ phạm vi hội thoại riêng |
| ACL-04 | Gửi tin DM | Là người tham gia hội thoại, phiên hợp lệ, đã xác minh email | Chưa xác minh hoặc không thuộc hội thoại | DM-002, DM-010 |
| ACL-05 | Sửa/xóa tin DM | Có quyền truy cập hội thoại và là tác giả tin | Người nhận sửa/xóa tin của người gửi; người thứ ba | DEC-020 |

DM không phụ thuộc vai trò hoặc tư cách thành viên cộng đồng. Không thêm chặn người gửi trong đợt này (DEC-018). Máy chủ kiểm tra quyền trước khi trả lịch sử, gửi/sửa/xóa hoặc đăng ký cập nhật, kể cả khi trả lại kết quả gửi lặp.

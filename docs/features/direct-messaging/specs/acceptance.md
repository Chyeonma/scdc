# SCDC — Tiêu chí và kiểm thử DM

AC-DM/TC-DM cho hành trình DM; TC-TEXT ở [kiểm thử nội dung chung](../../../shared/messaging/text.md#tests). Kết quả triển khai dẫn từ [status](../status.md).

<a id="acceptance"></a>

## Tiêu chí chấp nhận

Các tiêu chí dưới đây mô tả hành vi quan sát được, cần rà soát cùng các
[ngoại lệ](requirements.md#exceptions) và [nội dung còn mở](../status.md#gaps) trước khi xác nhận đặc tả.

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
| AC-DM-19 | Gửi chỉ zero-width/variation selector, surrogate lỗi, CRLF và emoji có ZWJ | Quy tắc DEC-090/text-policy thống nhất; giữ emoji/chữ và xuống dòng hợp lệ; không thay/cắt nội dung lỗi |
| AC-DM-20 | Gõ chưa Gửi rồi đổi hội thoại, reload/đóng tab/logout | Giữ bản nháp trong cùng tab/hội thoại; reload/đóng/logout mất; không lưu nội dung DM trên storage trình duyệt |
| AC-DM-21 | Refresh khi POST đã thất bại, response/event đảo thứ tự; reconnect bù nhiều trang | Không tự replay mutation; một dòng/tin; resumeCursor chỉ tiến sau merge đủ trang |

AC-DM-12 đến AC-DM-15 cụ thể hóa bảo vệ hội thoại và hành vi đồng thời
để rà soát; chưa phải kết quả kiểm thử đã đạt. AC-DM-16–18 cụ thể hóa DEC-069–071.

<a id="tests"></a>

## Dữ liệu và ca kiểm thử

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
| TC-DM-07 | Cùng khóa gửi nhưng payload khác; sau đó gửi cùng nội dung với khóa mới | Trường hợp đầu xung đột; trường hợp sau là tin mới hợp lệ | [hợp đồng gửi/đồng thời](../../../shared/messaging/persistence.md#contract-7), AC-DM-08 |
| TC-DM-08 | Tin gửi đã sửa/xóa; thử lại thao tác gửi ban đầu | Cùng ID, trả nội dung hiện hành/tombstone; không phục hồi bản cũ | DM-009, DM-011; [hợp đồng gửi/đồng thời](../../../shared/messaging/persistence.md#contract-7) |
| TC-DM-09 | A sửa tin nhiều lần trên A1; B và A2 mở lại | Chỉ nội dung mới nhất, dấu “Đã sửa”; không có endpoint hoặc dữ liệu lịch sử nội dung sửa | AC-DM-04, AC-DM-10 |
| TC-DM-10 | A xóa tin; thử xóa lại và thử sửa tin đã xóa | Hai bên thấy dòng thay thế; xóa lặp an toàn; sửa bị từ chối | AC-DM-05; [hợp đồng gửi/đồng thời](../../../shared/messaging/persistence.md#contract-7) |
| TC-DM-11 | A1/A2 sửa cùng version; lặp với một sửa và một xóa | Chỉ thao tác hợp lệ theo version được áp dụng; bên còn lại nhận xung đột, không ghi đè âm thầm | AC-DM-14 |
| TC-DM-12 | C gọi trực tiếp các API và đăng ký nhận cập nhật với ID DM/tin của A/B | Không đọc/gửi/sửa/xóa/nhận tin; không lộ người tham gia | AC-DM-12 |
| TC-DM-13 | B sửa/xóa tin của A qua API, không chỉ qua UI | Máy chủ từ chối; tin không đổi | DM-004, DM-005, ACL-05 |
| TC-DM-14 | U gửi trực tiếp qua API | Từ chối dù client giả quyền hoặc trạng thái xác minh | AC-DM-09 |
| TC-DM-15 | Hai request gửi tin khác nhau; giữ transaction thứ nhất chưa commit trong khi request thứ hai chạy | Phân trang/bù tin không bỏ sót bản commit trễ; sequence trong hội thoại tuân thứ tự commit | [gửi/đồng thời](../../../shared/messaging/persistence.md#contract-7) và [lịch sử](../../../shared/messaging/realtime.md#contract-8), AC-DM-03 |
| TC-DM-16 | Dừng worker sau commit rồi bật lại; phát sự kiện trùng/đảo thứ tự | Lịch sử vẫn có tin; cập nhật phục hồi; client merge theo ID/version, không nhân đôi hoặc quay lại bản cũ | AC-DM-02, AC-DM-03; [hợp đồng lịch sử/thu hồi](../../../shared/messaging/realtime.md#contract-8) |
| TC-DM-17 | B offline; A gửi hơn một trang tin mới và sửa/xóa tin cũ đang nằm trong cửa sổ B từng xem | B bù hết trang mới, tải lại tin cũ và bỏ cache hết hiệu lực; không sót tin hoặc giữ nội dung đã xóa | AC-DM-15 |

Ca kiểm tra nội dung cũ chỉ xét dữ liệu nghiệp vụ đang phục vụ, sự kiện
và log trong thiết kế; vòng đời bản sao lưu kiểm chứng riêng theo
[DEC-103–109 và TC-DATA](../../../data-lifecycle.md#acceptance). Không coi thu hồi bản người nhận tự sao chép là điều
kiện đạt của xóa/sửa tin.

Các ca thiết kế bổ sung, trạng thái Chưa chạy:

| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| TC-DM-18 | Search biên/case/dấu/NFC, q chứa %/_/backslash, đổi q/limit với cursor cũ | Substring literal; đúng DEC-069, không lộ email; cursor gắn query | AC-DM-16 |
| TC-DM-19 | Enter/Shift+Enter, mobile và IME | Theo thiết bị; composition không gửi sớm | AC-DM-17 |
| TC-DM-20 | Lịch sử tin cũ sau thời gian dài | Không tự hết hạn tin theo DEC-070 | AC-DM-18 |
| TC-DM-21 | Fixture invisible/invalid surrogate/NUL/CRLF khi send/edit và tin phòng | Đúng text-policy; không ghi/phát tin không hợp lệ | AC-DM-19 |
| TC-DM-22 | Đổi hội thoại, reload, logout, đổi account và hai tab | Bản nháp/cache chỉ trong tab, đúng account; sent lưu ở DB | AC-DM-20 |
| TC-DM-23 | Send 401/timeout; event trước response; refresh concurrent/lost response | Không tự gửi lại; UUIDv4/nội dung giữ đúng khi bấm retry; một dòng | AC-DM-08/21 |
| TC-DM-24 | HMAC key rotation/thiếu key; UUID byte-order đối chiếu DB | Retry dùng key cũ; thiếu key không tạo tin; cặp UUID nhất quán | AC-DM-13, hợp đồng fingerprint |
| TC-DM-25 | 101 tin mới với limit 50, mất kết nối sau từng trang, cursor hết hạn/bị sửa/dùng chéo user | Merge 3 trang trước tiến resume; không bỏ tin; cursor trái scope bị từ chối | AC-DM-15/21 |
| TC-DM-26 | Session revoke commit tranh send/subscribe/dispatch | Thứ tự guard/commit xác định; không có thao tác mới sau revoke, chat ngừng dữ liệu ≤5 giây | DEC-083, AC-DM-12/21 |

### Ma trận giao diện và cách ghi kết quả

Theo DEC-059, chạy hành trình tài khoản/DM trên desktop và trình duyệt
điện thoại; có kiểm tra đổi chiều màn hình, bàn phím ảo, cuộn lịch sử,
focus khi lỗi và menu sửa/xóa. Hai kích thước wireframe là dữ liệu thiết
kế, chưa thay thế danh sách OS/thiết bị và phiên bản cụ thể của ma trận DEC-082 tại nghiệm thu.

Mỗi lần chạy ghi: mã ca, build, cấu hình, dữ liệu, trình duyệt/thiết bị,
bước tái hiện, kỳ vọng, thực tế, bằng chứng, người thực hiện và lỗi liên
quan. Trạng thái ban đầu của **mọi ca là Chưa chạy**; ca phụ thuộc đề
xuất chưa xác nhận thêm ghi chú “Chờ chốt quy tắc”, không tính là đạt.

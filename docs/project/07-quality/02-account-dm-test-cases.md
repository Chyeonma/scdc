# SCDC — Ca kiểm thử chuẩn bị cho tài khoản, DM và quyền phòng

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-QA-DM-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Ca kiểm thử thiết kế — tất cả chưa chạy |
| Căn cứ | [Kế hoạch kiểm thử](01-test-and-acceptance-plan.md), [tài khoản](../03-requirements/03-accounts.md), [DM](../03-requirements/01-direct-messaging.md), [ma trận quyền](../03-requirements/05-access-control-matrix.md) |

## 1. Dữ liệu và môi trường cần chuẩn bị

| Mã dữ liệu | Mục đích |
|---|---|
| A, B | Hai tài khoản đã xác minh, không cùng cộng đồng, có tên hiển thị trùng nhưng tên tài khoản khác |
| C | Tài khoản thứ ba, không tham gia DM của A/B |
| U | Tài khoản chưa xác minh; kiểm tra cả xác minh và khôi phục mật khẩu |
| A1, A2 | Hai phiên của A để thử sửa/xóa đồng thời và thu hồi phiên |
| O, M, N | Chủ sở hữu, thành viên được giao quyền và thành viên thường trong cộng đồng mẫu |
| R-allow, R-deny | Hai vai trò có cấu hình quyền xem trái nhau cho cùng phòng |
| P-open, P-limited | Phòng mặc định mọi thành viên xem và phòng giới hạn |

Tạo dữ liệu thử riêng theo từng ca; không dùng thông tin người thật.
Môi trường cần email thử để nhận/xác minh liên kết, hai phiên trình duyệt
độc lập, khả năng ngắt kết nối hoặc làm mất phản hồi sau commit, và quan
sát dữ liệu/sự kiện bằng quyền kiểm thử. Không đưa API tạo lỗi vào sản
phẩm công khai. Thái chuẩn bị dữ liệu; Vg/Sáng rà soát cơ chế tạo lỗi và
bằng chứng kiểm tra giao dịch.

## 2. Tài khoản

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-ACC-01 | Đăng ký hợp lệ; dùng email và tên tài khoản đăng nhập sau xác minh | Cả hai cách vào cùng tài khoản; tên tài khoản không đổi | AC-ACC-01, AC-ACC-02 |
| TC-ACC-02 | U nhập đúng mật khẩu; thử mở hội thoại/tìm người/API ứng dụng | Không truy cập ứng dụng; có đường xác minh/khôi phục | AC-ACC-07, DEC-051 |
| TC-ACC-03 | U đặt lại mật khẩu rồi đăng nhập trước/sau xác minh | Khôi phục không tự xác minh; chỉ sau xác minh mới vào ứng dụng | AC-ACC-10 |
| TC-ACC-04 | Hai request đăng ký cùng tên tài khoản đồng thời | Một tài khoản; request còn lại lỗi trùng; không có dữ liệu dở dang | AC-ACC-09 |
| TC-ACC-05 | A đổi tên hiển thị; B tìm tên đó; thử đổi tên tài khoản | Tên hiển thị mới xuất hiện; tên tài khoản giữ nguyên; dữ liệu công khai không có email | AC-ACC-03, AC-ACC-08 |
| TC-ACC-06 | Dùng liên kết xác minh/reset sai, hết hạn hoặc đã dùng | Không hoàn tất thao tác trái phép; không lộ token trong UI/log | AC-ACC-06; thời hạn cụ thể chờ ACC-P02 |
| TC-ACC-07 | Gửi yêu cầu khôi phục/gửi lại với email tồn tại và không tồn tại | Phản hồi công khai có cùng ý nghĩa; không tiết lộ tài khoản | ACC-P03 — ca đề xuất chờ xác nhận |
| TC-ACC-08 | Phiên hết hạn/đăng xuất, rồi dùng lại bằng chứng phiên hoặc kết nối cũ | Không đọc/gửi/nhận dữ liệu tiếp theo sau thời hạn thu hồi đã chốt | AC-ACC-04; cơ chế và ngưỡng chờ thiết kế phiên |

## 3. DM: hành vi, giao dịch và khôi phục

| Mã ca | Tiền điều kiện và thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-DM-01 | A tìm một phần tên của B; chọn giữa hai người trùng tên hiển thị | Phân biệt bằng tên tài khoản, mở đúng người; không cần kết bạn/cùng cộng đồng | AC-DM-01, AC-ACC-03 |
| TC-DM-02 | A và B đồng thời mở DM với nhau | Cùng một ID hội thoại, đúng hai người, không có dữ liệu mồ côi | AC-DM-13 |
| TC-DM-03 | A gửi; B đang mở; đóng rồi mở lại cả hai phiên | “Đã gửi” sau lưu; cùng nội dung/ID; lịch sử còn sau mở lại | AC-DM-02, AC-DM-03 |
| TC-DM-04 | B không mở ứng dụng khi A gửi; B mở lại | Thấy tin đã lưu; không phụ thuộc sự kiện realtime lúc B vắng mặt | AC-DM-07 |
| TC-DM-05 | Làm lỗi trước commit; mạng trở lại | Không có tin trong DB hoặc sự kiện; UI giữ tin lỗi, không tự gửi lại | AC-DM-06 |
| TC-DM-06 | Commit thành công nhưng mất response; bấm thử lại nhiều lần, gồm hai request đồng thời | Chỉ một tin/ID và một thao tác tạo tin; UI chỉ một dòng sau merge | AC-DM-08 |
| TC-DM-07 | Cùng khóa gửi nhưng payload khác; sau đó gửi cùng nội dung với khóa mới | Trường hợp đầu xung đột; trường hợp sau là tin mới hợp lệ | Hợp đồng mục 7, AC-DM-08 |
| TC-DM-08 | Tin gửi đã sửa/xóa; thử lại thao tác gửi ban đầu | Cùng ID, trả nội dung hiện hành/tombstone; không phục hồi bản cũ | DM-009, DM-011; hợp đồng mục 7 |
| TC-DM-09 | A sửa tin nhiều lần trên A1; B và A2 mở lại | Chỉ nội dung mới nhất, dấu “Đã sửa”; không có endpoint hoặc dữ liệu lịch sử nội dung sửa | AC-DM-04, AC-DM-10 |
| TC-DM-10 | A xóa tin; thử xóa lại và thử sửa tin đã xóa | Hai bên thấy dòng thay thế; xóa lặp an toàn; sửa bị từ chối | AC-DM-05; hợp đồng mục 7 |
| TC-DM-11 | A1/A2 sửa cùng version; lặp với một sửa và một xóa | Chỉ thao tác hợp lệ theo version được áp dụng; bên còn lại nhận xung đột, không ghi đè âm thầm | AC-DM-14 |
| TC-DM-12 | C gọi trực tiếp các API và đăng ký nhận cập nhật với ID DM/tin của A/B | Không đọc/gửi/sửa/xóa/nhận tin; không lộ người tham gia | AC-DM-12 |
| TC-DM-13 | B sửa/xóa tin của A qua API, không chỉ qua UI | Máy chủ từ chối; tin không đổi | DM-004, DM-005, ACL-05 |
| TC-DM-14 | U gửi trực tiếp qua API | Từ chối dù client giả quyền hoặc trạng thái xác minh | AC-DM-09 |
| TC-DM-15 | Hai request gửi tin khác nhau; giữ transaction thứ nhất chưa commit trong khi request thứ hai chạy | Phân trang/bù tin không bỏ sót bản commit trễ; sequence trong hội thoại tuân thứ tự commit | Hợp đồng mục 7–8, AC-DM-03 |
| TC-DM-16 | Dừng worker sau commit rồi bật lại; phát sự kiện trùng/đảo thứ tự | Lịch sử vẫn có tin; cập nhật phục hồi; client merge theo ID/version, không nhân đôi hoặc quay lại bản cũ | AC-DM-02, AC-DM-03; hợp đồng mục 8–9 |
| TC-DM-17 | B offline; A gửi hơn một trang tin mới và sửa/xóa tin cũ đang nằm trong cửa sổ B từng xem | B bù hết trang mới, tải lại tin cũ và bỏ cache hết hiệu lực; không sót tin hoặc giữ nội dung đã xóa | AC-DM-15 |

Ca kiểm tra nội dung cũ chỉ xét dữ liệu nghiệp vụ đang phục vụ, sự kiện
và log trong thiết kế; vòng đời bản sao lưu phải kiểm chứng riêng sau
khi chốt OQ-011. Không coi thu hồi bản người nhận tự sao chép là điều
kiện đạt của xóa/sửa tin.

## 4. Dữ liệu biên nội dung

| Mã ca | Dữ liệu | Kết quả mong đợi theo DEC-053 |
|---|---|---|
| TC-TEXT-01 | Chuỗi rỗng, dấu cách, tab hoặc chỉ xuống dòng | Bị từ chối; không lưu và không phát sự kiện |
| TC-TEXT-02 | 1, 1.999, 2.000 và 2.001 ký tự | Ba giá trị đầu được nhận, 2.001 bị từ chối |
| TC-TEXT-03 | Tiếng Việt có dấu, ký tự tổ hợp, emoji đơn và emoji ghép | Hiển thị đúng; đếm nhất quán client/server theo thuật toán được chốt |
| TC-TEXT-04 | CRLF và LF cùng nội dung; khoảng trắng đầu/cuối của tin có chữ | Chuẩn hóa xuống dòng; giữ khoảng trắng có chủ ý; không sinh xung đột chống trùng do chuẩn hóa khác nhau |
| TC-TEXT-05 | Nội dung trông như HTML/script và ký tự đặc biệt | Hiển thị như văn bản, không chạy mã hoặc diễn giải thành giao diện |
| TC-TEXT-06 | Lặp các dữ liệu trên khi sửa tin và gửi tin phòng | Cùng quy tắc nội dung, không có đường bỏ qua giới hạn |

Các ca TEXT áp dụng AC-DM-11 và AC-COM-24. Thái chuẩn bị fixture cố định
với số cụm ký tự kỳ vọng; Vg/Sáng khóa quy tắc cụm/ký tự vô hình trước
khi dùng fixture để kết luận đạt.

## 5. Ca quyền phòng đã có quyết định

| Mã ca | Tình huống | Kết quả | Dẫn chiếu |
|---|---|---|---|
| TC-ACL-01 | Phòng mới chưa giới hạn; N đã xác minh | N xem và gửi được; thấy lịch sử cũ | AC-COM-11, AC-COM-15, AC-COM-17 |
| TC-ACL-02 | N có cả R-allow và R-deny, không có ngoại lệ cá nhân | Không xem, không gửi hoặc nhận nội dung phòng | DEC-057 |
| TC-ACL-03 | Như TC-ACL-02 nhưng cá nhân N được cho phép | N xem/gửi được nếu thỏa điều kiện tài khoản/thành viên | DEC-057 |
| TC-ACL-04 | Vai trò cho phép, ngoại lệ cá nhân từ chối | N không xem/gửi được | DEC-057 |
| TC-ACL-05 | O mở phòng giới hạn, gồm cả cấu hình từ chối theo vai trò/cá nhân | O vẫn xem được trong cộng đồng của mình; không vượt điều kiện phiên/tài khoản | DEC-056 |
| TC-ACL-06 | M được quyền tạo phòng nhưng thử tạo/sửa/gán/thu hồi vai trò | Máy chủ từ chối; O thực hiện được | DEC-058 |
| TC-ACL-07 | N đang mở phòng; O thu hồi quyền hoặc N rời cộng đồng | API từ chối; ngừng nhận nội dung phòng theo ngưỡng thu hồi; DM độc lập vẫn hoạt động | AC-COM-11, AC-COM-21 |

## 6. Ma trận giao diện và cách ghi kết quả

Theo DEC-059, chạy hành trình tài khoản/DM trên desktop và trình duyệt
điện thoại; có kiểm tra đổi chiều màn hình, bàn phím ảo, cuộn lịch sử,
focus khi lỗi và menu sửa/xóa. Hai kích thước wireframe là dữ liệu thiết
kế, chưa thay thế danh sách trình duyệt/phiên bản/thiết bị cần chốt ở OQ-007.

Mỗi lần chạy ghi: mã ca, build, cấu hình, dữ liệu, trình duyệt/thiết bị,
bước tái hiện, kỳ vọng, thực tế, bằng chứng, người thực hiện và lỗi liên
quan. Trạng thái ban đầu của **mọi ca là Chưa chạy**; ca phụ thuộc đề
xuất chưa xác nhận thêm ghi chú “Chờ chốt quy tắc”, không tính là đạt.

## 7. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Chuẩn bị dữ liệu, ca chức năng/ngoại lệ/đồng thời, biên nội dung và quyền phòng; chưa thực hiện kiểm thử. |

[Mục lục hồ sơ](../README.md)

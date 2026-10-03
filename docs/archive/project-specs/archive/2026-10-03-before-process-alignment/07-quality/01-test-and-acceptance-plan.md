# SCDC — Kế hoạch kiểm thử và nghiệm thu

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-QA-001 |
| Phiên bản | 0.7 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Kế hoạch dự thảo — chưa thực hiện kiểm thử, chưa chốt ngưỡng nghiệm thu |
| Căn cứ | [Project Brief](../01-initiation/01-project-brief.md), [đặc tả tài khoản](../03-requirements/03-accounts.md), [đặc tả DM](../03-requirements/01-direct-messaging.md), [đặc tả cộng đồng](../03-requirements/02-community-join-and-channels.md) |

## 1. Phạm vi kiểm chứng

Thái chuẩn bị và điều phối kiểm thử, Vg và Sáng rà soát tình huống liên
quan đến UX, backend và kiến trúc. Kết quả kiểm thử phải nối tới mã tiêu
chí chấp nhận, môi trường, dữ liệu thử, phiên bản sản phẩm và lỗi phát hiện.
Không coi tiêu chí dự thảo là đã đạt khi chưa có kết quả thực thi.
Bộ ca và dữ liệu chuẩn bị cho tài khoản/DM/quyền phòng tại
[SCDC-QA-DM-001](02-account-dm-test-cases.md); mọi ca hiện là chưa chạy.

| Nhóm | Tình huống cần kiểm chứng | Căn cứ |
|---|---|---|
| DM chức năng | Tìm theo hai trường, chọn đúng người, gửi/lưu/xem lại, sửa/xóa, lỗi gửi và chủ động thử lại; từ chối gửi khi email chưa xác minh. | AC-DM-01 đến AC-DM-15 |
| DM kết nối | A gửi khi B chưa mở ứng dụng; B thấy tin đã lưu khi mở lại. Kết quả lần đầu không rõ, thử lại cùng thao tác chỉ một tin. | AC-DM-07, AC-DM-08 |
| Cộng đồng | Chỉ tìm được cộng đồng công khai; vào ngay/chờ duyệt; mời hợp lệ/hết hạn/thu hồi; quyền tạo mời, duyệt, tạo phòng; vào cộng đồng riêng tư và tự rời. | AC-COM-01 đến AC-COM-11, AC-COM-19 đến AC-COM-21 |
| Phòng/tin | Quyền xem mặc định, thành viên mới thấy lịch sử, người có quyền xem được gửi sau xác minh email, chủ sở hữu/người được cấp quyền đổi danh sách xem, gửi lại không trùng và từ chối thao tác trái quyền ở API. | AC-COM-11 đến AC-COM-18, AC-COM-22 đến AC-COM-28 |
| Tài khoản | Đăng ký với email/tên tài khoản, đăng nhập bằng một trong hai; chưa xác minh bị từ chối gửi, đặt lại mật khẩu qua email. Phiên và bảo vệ tài khoản còn chờ đặc tả. | AC-ACC-01 đến AC-ACC-10, OQ-002 |
| Media | Gọi riêng cần chấp nhận, phòng thoại vào theo quyền xem, nhiều nguồn chia sẻ đồng thời, tự kết nối lại; quyền thiết bị và tải dự kiến. Chỉ có tiêu chí chức năng khung, chưa có ngưỡng chất lượng. | AC-MEDIA-01 đến AC-MEDIA-09; chờ OQ-006/OQ-007 |
| Vận hành | Cài đặt/triển khai, giám sát, sao lưu/khôi phục, lỗi dịch vụ và mở đăng ký công khai. | Chờ OQ-007/OQ-010/OQ-011 và SCP-008 |

## 2. Các cấp kiểm thử

| Cấp | Mục đích | Điều kiện ghi kết quả |
|---|---|---|
| Thành phần | Quy tắc quyền theo vai trò/ngoại lệ cá nhân, nội dung 2.000 ký tự, chuyển trạng thái tin, xử lý mời/yêu cầu và ngoại lệ dữ liệu. | Có trường hợp đúng/sai, dữ liệu biên và kết quả rõ. |
| Tích hợp | Web–API–dịch vụ–dữ liệu; cập nhật thời gian thực và tải lại lịch sử; quyền đổi trong phiên. | Có môi trường tích hợp và định danh bản chạy. |
| Hệ thống | Hành trình người dùng đầu đến cuối trên trình duyệt được hỗ trợ. | Tài khoản/DM cần desktop và trình duyệt điện thoại theo DEC-059; danh sách cụ thể ở OQ-007 phải được xác nhận. |
| Chất lượng/vận hành | Tải, lỗi mạng, sao lưu/khôi phục, giám sát và chất lượng cuộc gọi. | Có ngưỡng đo và mức tải được xác nhận, không dùng giả định quy mô làm tiêu chí đạt. |
| Nghiệm thu | Đại diện khách hàng thực hiện hoặc xác nhận kịch bản trọng yếu. | Ghi rõ đạt/chưa đạt, lỗi tồn, người và ngày xác nhận. |

## 3. Bộ tình huống ưu tiên để chuẩn bị dữ liệu

1. Hai tài khoản không cùng cộng đồng: tìm người, mở DM, gửi tin, đăng
   xuất người nhận, gửi tiếp, đăng nhập lại và đọc lịch sử.
2. Gửi thất bại, người gửi bấm thử lại; xác nhận không có hai bản tin khi
   kết quả lần gửi đầu không rõ, cả trong DM và phòng.
3. Sửa/xóa tin của mình trong DM và trong phòng; tài khoản khác thử cùng
   thao tác bằng API. Kiểm tra nội dung hiển thị trên hai phiên.
4. Hai cộng đồng có chế độ tham gia khác nhau; liên kết mời hợp lệ, hết
   hạn; chủ sở hữu, người có quyền và thành viên thường thử tạo mời/duyệt.
5. Phòng mới hiện cho mọi thành viên; thành viên mới đọc được lịch sử cũ;
   người có quyền xem gửi được tin. Sau khi giới hạn quyền, thành viên
   mất quyền không đọc được lịch sử hay nhận cập nhật qua kết nối mở.
6. Khi media được đặc tả, thử quyền micro/camera, cuộc gọi riêng, phòng
   nhiều người, chia sẻ màn hình, ngắt mạng và khôi phục.

## 4. Điều kiện nghiệm thu dự kiến

Trước mỗi đợt bàn giao cần có: tiêu chí chấp nhận đã được xác nhận, môi
trường và dữ liệu thử, kết quả chạy, danh sách lỗi với mức độ và quyết
định xử lý. Trước phát hành công khai cần thêm kết quả phân quyền, kiểm
thử tải/media theo ngưỡng đã chốt, khôi phục dữ liệu, tài liệu vận hành
và người nhận hỗ trợ. Ngưỡng lỗi cho phép, chất lượng dịch vụ, trình
duyệt hỗ trợ và người ký nghiệm thu còn phải xác định; chưa thể tuyên bố
đạt điều kiện phát hành.

Đợt hiện tại không khảo sát người dùng bên ngoài theo DEC-030. Việc đó
không thay thế kiểm thử khả dụng nội bộ, kiểm thử hệ thống hoặc nghiệm
thu của đại diện khách hàng.

## 5. Mẫu ghi nhận kết quả

| Trường | Nội dung phải ghi |
|---|---|
| Mã tình huống và yêu cầu | AC-*, SCP-* hoặc OQ-* đã được giải quyết. |
| Bản chạy/môi trường | Phiên bản triển khai, trình duyệt, thiết bị và dữ liệu thử. |
| Kết quả | Đạt/chưa đạt/chưa chạy; bằng chứng và thời điểm. |
| Lỗi và xử lý | Mã lỗi, mức ảnh hưởng, người phụ trách và kết quả kiểm tra lại. |
| Xác nhận | Người thực hiện và người xác nhận theo cấp kiểm thử. |

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Xác định phạm vi, cấp kiểm thử, tình huống ưu tiên và dữ liệu cần ghi khi nghiệm thu. |
| 0.2 | 2026-09-30 | Bổ sung tiêu chí tài khoản dự thảo, thử lại không trùng và quyền lịch sử/xem/gửi phòng. |
| 0.3 | 2026-09-30 | Bổ sung kiểm thử xác minh email, đặt lại mật khẩu, cộng đồng riêng tư, thu hồi mời và tự rời. |
| 0.4 | 2026-09-30 | Liên kết tiêu chí xác minh email ở hai loại tin nhắn vào ma trận kiểm thử. |
| 0.5 | 2026-09-30 | Liên kết bộ tiêu chí chức năng khung cho media, giữ ngưỡng chất lượng là vấn đề mở. |
| 0.6 | 2026-09-30 | Bổ sung kiểm thử nhận cuộc gọi, vào phòng, chia sẻ nhiều nguồn và tự kết nối lại. |
| 0.7 | 2026-10-03 | Liên kết ca kiểm thử chi tiết; mở rộng truy vết tiêu chí tài khoản/DM/quyền và mục tiêu desktop/điện thoại. |

[Mục lục hồ sơ](../README.md)

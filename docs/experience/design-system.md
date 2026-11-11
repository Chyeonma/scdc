# SCDC — Quy ước giao diện

Trang này mô tả các thành phần giao diện dùng chung và quy tắc tương tác. Giá trị thực tế (màu, cỡ chữ, khoảng cách) nằm trong [styles.css](../../clients/WebClient/src/styles.css); trang này không chép lại.

**Trạng thái:** giao diện dùng CSS và component trong WebClient. Chưa có bộ component hay prototype được nghiệm thu riêng.

## Thành phần dùng chung

| Nhóm | Nội dung |
|---|---|
| Bố cục | Thanh server bên trái, thanh phòng, vùng nội dung, panel và hộp thoại |
| Chữ và màu | Biến CSS `--text-*`, `--bg-*`, `--accent*` và màu trạng thái |
| Hình dạng | Bo góc, đổ bóng; trạng thái hover, active, disabled |
| Form | Nhãn, giá trị, lỗi từng trường, lỗi chung, trạng thái đang xử lý và kết quả |
| Thao tác | Nút, hộp thoại, xác nhận khi cần, thông báo trạng thái |

Component nằm trong [components](../../clients/WebClient/src/components); cấu trúc frontend xem [frontend](../system/frontend.md). Khi sửa một thành phần dùng chung, kiểm tra lại các màn hình dùng nó.

## Quy tắc tương tác

- Nhãn và thông báo nói rõ thao tác hoặc trạng thái. Không hiện chi tiết kỹ thuật nếu không giúp người dùng quyết định.
- Phân biệt rõ bốn trạng thái: đang tải, trống, lỗi và mất quyền. Không dùng dữ liệu mẫu để che lỗi API.
- Mỗi form có hành vi cho: đang xử lý, thất bại, không rõ kết quả và thử lại.
- Bàn phím, focus, bộ gõ tiếng Việt (IME), co giãn màn hình và tên cho trình đọc màn hình cần được kiểm tra trên danh sách thiết bị đã chốt. Chưa có kết quả kiểm tra khả năng truy cập cho toàn hệ thống.
- Kiểm tra nội dung tin theo [nội dung văn bản](../features/messaging/text-policy.md); phím gửi theo [nhắn tin riêng](../features/messaging/direct-messaging.md); bản nháp và trạng thái tin theo [vòng đời tin](../features/messaging/message-lifecycle.md).

## Bản thiết kế

Wireframe, mockup hoặc prototype đặt cạnh tài liệu tính năng, hoặc link tới nguồn thiết kế có ghi phiên bản. Ghi rõ đó là bản nháp hay đã review, và áp dụng cho màn hình nào. Quy tắc và tiêu chí chấp nhận vẫn nằm trong tài liệu tính năng; ảnh thiết kế không thay thế quy tắc.

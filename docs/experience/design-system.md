# SCDC — Quy ước giao diện

## Nguồn và trạng thái

UI hiện dùng CSS và component trong WebClient; chưa có bộ component/prototype được nghiệm thu độc lập. [styles.css](../../clients/WebClient/src/styles.css) là nguồn giá trị token thực tế; tài liệu này giải thích vai trò và yêu cầu rà soát, không sao chép mã màu/kích thước.

## Thành phần dùng chung

| Nhóm | Nội dung |
|---|---|
| Bề mặt/bố cục | Server rail, sidebar, vùng nội dung, panel và modal |
| Typography/màu | Token `--text-*`, `--bg-*`, `--accent*` và màu trạng thái trong CSS |
| Hình dạng | Token radius/shadow; trạng thái hover/active/disabled |
| Form | Label, giá trị, lỗi theo trường, lỗi chung, pending và kết quả thao tác |
| Tác vụ | Nút, modal, xác nhận khi nghiệp vụ yêu cầu, thông báo trạng thái |

Component hiện tại tại [components](../../clients/WebClient/src/components). Cấu trúc frontend tại [frontend](../system/frontend.md). Khi thay thành phần dùng chung, kiểm tra các chủ đề sử dụng nó.

## Quy tắc tương tác

- Label và thông báo nêu rõ thao tác/trạng thái; không đưa chi tiết triển khai vào luồng sản phẩm nếu không giúp người dùng quyết định.
- Phân biệt tải, rỗng, lỗi và mất quyền. Dữ liệu mẫu không thay trạng thái lỗi API.
- Thiết kế mỗi form/tác vụ có hành vi khi đang xử lý, thất bại, kết quả không rõ và retry phù hợp chủ đề.
- Thao tác bàn phím, focus, IME, responsive và tên truy cập cần được review trên ma trận thiết bị đã khóa. Chưa có bằng chứng accessibility toàn hệ thống.
- Validation nội dung theo [text policy](../features/messaging/text-policy.md); phím gửi theo [DM](../features/messaging/direct-messaging.md); draft và trạng thái tin theo [vòng đời tin](../features/messaging/message-lifecycle.md).

## Artefact UI/UX

Wireframe/mockup/prototype thuộc chủ đề đặt cạnh tài liệu hoặc dẫn tới nguồn thiết kế có phiên bản. Ghi trạng thái nháp/đã review và phạm vi màn hình. Quy tắc, validation và tiêu chí chấp nhận vẫn ở tài liệu chủ đề; ảnh thiết kế không là nguồn quy tắc thứ hai.

# Tính năng: Cộng đồng (Server & Channel)

Module quản lý Server (cộng đồng), Channel (phòng theo chủ đề), thành viên, lời mời và phân quyền.

## Tài liệu trong thư mục này

| File | Nội dung | Đọc khi |
|---|---|---|
| [requirements.md](requirements.md) | Usecase tham gia, quy tắc, 28 tiêu chí chấp nhận | Bắt đầu làm tính năng |
| [wireframes.md](wireframes.md) | Phác thảo giao diện cộng đồng | Làm Frontend |
| [access-control.md](access-control.md) | Ma trận quyền, thuật toán tính quyền xem | Backend phân quyền |

## Quyết định đã chốt (tóm tắt)

- **DEC-024:** Chỉ cộng đồng công khai xuất hiện trong tìm kiếm.
- **DEC-025:** Mặc định vào ngay; có thể cấu hình chờ duyệt; link mời hợp lệ luôn cho vào.
- **DEC-055–058:** Phân quyền qua vai trò + ngoại lệ cá nhân; chủ sở hữu luôn xem mọi phòng; từ chối thắng giữa các vai trò.

## Trạng thái

🟡 Một phần. Còn mở: tạo/sửa cộng đồng, từ chối yêu cầu, tham gia lại, chuyển chủ sở hữu.

# SCDC — Yêu cầu media

Phạm vi và hành vi đã chốt; thiết kế provider chưa tự ghi nhận bằng chứng đạt.

<a id="requirements"></a>

## Phạm vi, hành trình và yêu cầu

### Phạm vi đã thống nhất

Bản hoàn thiện v1 có phòng thoại nhiều thành viên trong server và cuộc gọi
riêng giữa hai người. Cả hai ngữ cảnh có video và chia sẻ màn hình.
Cuộc gọi trong nhóm chat riêng ngoài server không thuộc bản hoàn thiện v1.
Sản phẩm chạy trên trình duyệt web; ma trận trình duyệt desktop đã chốt DEC-082; phiên bản/OS/thiết bị thực tế phải ghi ở lần nghiệm thu. Ngày 2026-10-04, người dùng chốt 20 người tham gia gọi đồng thời toàn hệ thống là giới hạn v1 (DEC-079). Đây là thay đổi từ giả định dự toán AS-006 sang quy tắc cần kiểm chứng, chưa phải kết quả đo.

### Hành trình cần đặc tả

| Hành trình | Luồng chính cần quyết định | Ngoại lệ bắt buộc xem xét |
|---|---|---|
| Gọi riêng | Một người gọi, người nhận phải chấp nhận trước khi bắt đầu; hai bên bật/tắt micro/camera/chia sẻ và kết thúc. | Người nhận vắng mặt, bận cuộc gọi, từ chối, không cấp quyền thiết bị, mất mạng. |
| Phòng thoại | Thành viên thấy phòng được phép xem thì vào ngay, vào/rời, biết ai đang trong phòng, điều khiển thiết bị. | Mất quyền/phòng bị xóa, phòng đầy, lỗi kết nối, nhiều người vào/ra đồng thời. |
| Video và chia sẻ màn hình | Bật/tắt nguồn video hoặc màn hình trong cuộc gọi/phòng, người khác xem và nhận biết nguồn đang chia sẻ. | Không có thiết bị/quyền truy cập, ngừng chia sẻ từ trình duyệt, đổi nguồn, mạng yếu. |

Quy tắc đã được đại diện sản phẩm xác định: nhận cuộc gọi riêng cần thao
tác chấp nhận (DEC-046); cuộc gọi nhỡ không lưu vào hội thoại ở đợt đầu
(DEC-047); quyền xem phòng thoại cho phép vào ngay (DEC-048); nhiều người
có thể chia sẻ màn hình đồng thời trong phòng thoại (DEC-049); ứng dụng
tự kết nối lại khi mạng trở lại (DEC-050). Giới hạn và thời gian chờ đã chốt DEC-078–080; chất lượng đã chốt DEC-085, chưa đo.

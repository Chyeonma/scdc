# SCDC — Giao diện media

Trạng thái, thiết bị và bố cục mục tiêu cho cuộc gọi riêng và phòng thoại.

<a id="ux"></a>

## Giao diện và trạng thái

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Gọi riêng | Thao tác gọi từ hội thoại và màn hình cuộc gọi đến. | Người nhận bấm chấp nhận trước khi bắt đầu; người gọi biết trạng thái chờ/đã nhận/không nhận. Cuộc gọi nhỡ chưa lưu vào hội thoại ở đợt đầu. |
| Vào phòng thoại | Danh sách phòng và màn hình cuộc gọi nhóm. | Thành viên có quyền xem phòng vào ngay; người không có quyền không thấy/không vào được. |
| Thiết bị và chia sẻ | Điều khiển micro, camera, chia sẻ màn hình và danh sách nguồn. | Phản hồi khi không được cấp quyền; nhiều người có thể chia sẻ đồng thời trong phòng, mỗi nguồn phân biệt được. |
| Mất mạng | Trạng thái cuộc gọi đang gián đoạn. | Báo đang kết nối lại; tự thử khi mạng trở lại; báo rõ nếu không khôi phục được. |

Phòng tối đa 10, toàn hệ thống tối đa 20 người (gồm gọi riêng), 2 màn hình/phòng; mỗi người một camera/màn hình. Đổ chuông/reconnect tối đa 30 giây. Bố cục nhiều nguồn và ngưỡng theo DEC-085; kết quả và thiết bị cụ thể còn OQ-007.

Wireframe media dưới đây là thiết kế đề xuất; chưa có prototype/kết quả rà soát. Cần thiết kế các trạng thái chờ nhận, từ chối/bận, đang kết nối, trong cuộc gọi, mất mạng/reconnect, không cấp quyền thiết bị, nguồn chia sẻ dừng, phòng đầy và mất quyền. Trạng thái thiết bị phải phản ánh thực tế, không chỉ trạng thái nút bấm.

DEC-082 chốt media trên desktop Chrome/Edge/Firefox/Safari, 2 phiên bản ổn định gần nhất tại nghiệm thu; media điện thoại ở đợt sau. Không dùng phiên bản phát triển của trình duyệt thay phiên bản ổn định; ghi OS, thiết bị và phiên bản thực tế của từng ca.

### Màn hình đề xuất

```text
MEDIA-S01 · Gọi đến               MEDIA-S02 · Cuộc gọi / phòng thoại
┌────────────────────────────┐   ┌────────────────────────────────────┐
│ Tên hiển thị · @username   │   │ Phòng / tên người · Đang kết nối  │
│ Đang gọi đến               │   │ [Nguồn màn hình 1] [Nguồn 2]       │
│ [Từ chối]       [Nhận]     │   │ [Camera / tên các thành viên]     │
└────────────────────────────┘   │ Mic · Camera · Chia sẻ · [Rời]    │
                                 └────────────────────────────────────┘
```

MEDIA-S03 thể hiện mất mạng/đang reconnect và thời gian còn lại trong 30 giây; hết hạn có nút gọi/vào lại. MEDIA-S04 báo người nhận bận/vắng mặt, phòng đầy hoặc toàn hệ thống hết chỗ. MEDIA-S05 báo thiết bị/quyền chia sẻ không dùng được hoặc hai nguồn màn hình đã đầy. Không hiển thị mic/camera/share là đang phát trước khi thiết bị/provider thực sự xác nhận. Hai nguồn màn hình có nhãn tác giả và thao tác chọn xem; không tự dừng nguồn người khác.

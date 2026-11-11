# SCDC — Thoại, video và chia sẻ màn hình

Người dùng có thể gọi riêng cho một người, hoặc vào phòng thoại trong server để nói chuyện nhóm, bật camera và chia sẻ màn hình. Gọi nhóm ngoài server không thuộc v1.

**Trạng thái:** đã thiết kế, chưa có code backend. Âm thanh và video truyền qua LiveKit tự host.

**Người phụ trách:** Thái (xem [phân công](../../project/planning.md#team)). Phạm vi Media trên mobile chưa chốt, xem [Mobile](../../experience/mobile.md).

## Các trang

| Trang | Nội dung |
|---|---|
| [Cuộc gọi riêng](direct-calls.md) | Gọi, đổ chuông, nhận, từ chối, hủy, kết thúc; giao diện, API và tiêu chí chấp nhận |
| [Phòng thoại](voice-rooms.md) | Ai được vào, vào và rời phòng, liên hệ với vòng đời phòng trong server |
| [Micro, camera và chia sẻ màn hình](video-screen-sharing.md) | Bật, tắt nguồn phát; giới hạn số nguồn |
| [Tham gia, hạn mức và kết nối](participation-lifecycle.md) | Cơ chế chung: kiểm tra quyền, đếm hạn mức, kết nối lại, cấp quyền LiveKit, ngắt khi mất quyền |

Hai loại gọi dùng chung cơ chế điều khiển nguồn phát, hạn mức và LiveKit. Cơ chế chung được mô tả một lần trong trang [Tham gia, hạn mức và kết nối](participation-lifecycle.md).

## Hợp đồng máy đọc

- API: [media.openapi.json](../../contracts/media.openapi.json)
- Sự kiện realtime: [media-realtime.schema.json](../../contracts/media-realtime.schema.json)

Sửa schema trực tiếp trong các file trên, không sửa bản chép trong Markdown.

## Kế hoạch và kiểm thử

Các gói V1-MEDIA-01 đến 05 nằm trong [kế hoạch Media](../../project/planning.md#v1-media-packages). Kết quả kiểm thử của mỗi gói phải ghi rõ commit, phiên bản LiveKit và môi trường chạy. Fixture và danh sách ca kiểm thử chưa chứng minh tính năng đã đạt.

---

Nguồn: REQ-007, REQ-008, REQ-009, SCP-006, SCP-007, [DEC-171](../../records/decisions/README.md#dec-171), [v1](../../releases/v1.md).

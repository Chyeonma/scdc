# Thoại, Video & Chia sẻ màn hình — Yêu cầu

> Tài liệu gốc: [SCDC-FR-MEDIA-001](../../archive/project-specs/03-requirements/04-voice-video-screen-sharing.md)

> ⚠️ **Đây là khung đặc tả, chưa đủ để triển khai.**

## Hành trình cần đặc tả

| Hành trình | Luồng chính | Ngoại lệ |
|---|---|---|
| Gọi riêng | Người nhận chấp nhận; hai bên bật/tắt micro/camera | Vắng mặt, bận, từ chối, mất mạng |
| Phòng thoại | Vào/rời tự do; biết ai đang trong phòng | Mất quyền, phòng đầy, lỗi kết nối |
| Video/chia sẻ | Bật/tắt nguồn; nhiều người chia sẻ cùng lúc | Không có thiết bị, mạng yếu |

## Tiêu chí chấp nhận khung

| Mã | Tình huống | Kết quả |
|---|---|---|
| AC-MEDIA-01 | Hai người gọi riêng | Nhận/từ chối; khi nhận nghe được nhau |
| AC-MEDIA-02 | Vào phòng thoại | Vào ngay; không quyền bị từ chối |
| AC-MEDIA-03 | Bật/tắt camera | Người khác thấy/ngừng thấy đúng |
| AC-MEDIA-04 | Chia sẻ màn hình | Người khác thấy nguồn khi bật |
| AC-MEDIA-05 | Trình duyệt từ chối quyền thiết bị | Báo trạng thái rõ |
| AC-MEDIA-06 | Gọi riêng, người nhận bấm chấp nhận | Cuộc gọi bắt đầu sau khi chấp nhận |
| AC-MEDIA-07 | Gọi nhưng không nhận | Không lưu cuộc gọi nhỡ đợt đầu |
| AC-MEDIA-08 | Hai người cùng chia sẻ màn hình | Cả hai nguồn hoạt động |
| AC-MEDIA-09 | Mất mạng rồi có lại | Tự kết nối lại |

## Quyết định còn thiếu

- Số người/phòng tối đa
- Chất lượng âm thanh/hình ảnh tối thiểu
- Giải pháp media (LiveKit?) và chi phí
- Thời gian chờ khi mất mạng
- Ma trận trình duyệt hỗ trợ

---

📎 Đầy đủ: [Đặc tả gốc](../../archive/project-specs/03-requirements/04-voice-video-screen-sharing.md)

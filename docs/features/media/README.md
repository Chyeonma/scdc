# SCDC — Thoại, video và chia sẻ màn hình

Phạm vi REQ-007/008/009, SCP-006/007 của [v1](../../releases/v1.md). Gọi riêng hai người và phòng thoại trong Community cùng dùng điều khiển nguồn, quota và provider. Cuộc gọi nhóm ngoài server không thuộc v1.

## Mục lục

| Chủ đề | Nội dung |
|---|---|
| [Cuộc gọi riêng](direct-calls.md) | Điều kiện DM, ringing/accept/reject/cancel/end, UX, API, dữ liệu/transaction, AC và hiện trạng |
| [Phòng thoại](voice-rooms.md) | View/membership/epoch, vào/rời, tích hợp vòng đời channel, UX/API/AC và hiện trạng |
| [Micro, video và chia sẻ](video-screen-sharing.md) | Capture, mic/camera/share, reservation/publish, quota nguồn, UX/API/AC và hiện trạng |
| [Participation, quota và kết nối](participation-lifecycle.md) | Guard/claims/counter, reconnect, grant, realtime, LiveKit admission/cutoff, restart/restore và MEDIA-GAP |

## Phạm vi đọc và bàn giao

Mỗi chủ đề giữ hành vi, UX, API/dữ liệu/transaction, AC/TC và hiện trạng tương ứng. Các cơ chế participation/SFU dùng chung có một chủ sở hữu trong lifecycle. Contract máy đọc ở [OpenAPI](../../contracts/media.openapi.json) và [catalogue realtime](../../contracts/media-realtime.schema.json); không chỉnh schema bằng bản sao trong Markdown.

Gói V1-MEDIA-01–05 và phân công ở [hồ sơ v1](../../releases/v1.md#gói-media-sau-nền-mvp). Kết quả gắn commit/provider build/môi trường vào hồ sơ kiểm chứng của gói; fixture/danh mục test chưa tự chứng minh đạt. Các mô tả hiện trạng được giữ theo lần đối chiếu nguồn, việc tổ chức tài liệu này không chạy lại test.

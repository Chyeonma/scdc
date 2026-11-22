# Kiểm chứng cuộc gọi riêng Media

Base: `6bed091a2215ff40c57088ed2d14fcee14c60a87` (main khi bắt đầu). Nhánh: `feat/media-direct-calls`. Ngày kiểm tra: 2026-11-23 (Asia/Ho_Chi_Minh).

Môi trường: Linux, Docker Compose project thử riêng `scdc-call-test`, PostgreSQL 18.7, .NET runtime 10.0.15, Node 24.22.1, Playwright 1.63.0 với Chromium có sẵn `chromium-1248`. Dùng `compose.yaml` + `compose.media.yaml`; không khởi động observability. Dữ liệu nằm trong volume thử riêng. Không dùng database của stack chạy thử cũ. LiveKit server **1.13.11**, JS SDK **2.25.0**.

## Kết quả

- Docker build thành công API chính, Sample, Media độc lập và WebClient bật Media.
- Frontend unit: **77/77**, không bỏ test.
- Media domain/grant unit: **16/16**, gồm quyền thao tác theo vai trò/session/tab, version chính xác, hạn đổ chuông và chữ ký/grant có thời hạn.
- E2E voice/video: hai browser context đăng nhập hai tài khoản thật, qua gateway → Media API/SignalR → PostgreSQL → LiveKit thật. Ban đầu micro/camera tắt; bật thiết bị, video hiển thị ở cả hai phía; Chromium RTP stats xác nhận nhận audio và video. Mẫu đo lượt cuối: caller audio **44.962 byte**, video **5.212 byte**; recipient audio **64.104 byte**, video **50.762 byte**. Kết thúc làm đóng PeerConnection và gỡ mọi track ở hai phía.
- Hủy/từ chối: cả hai giao diện cập nhật. Replay create/action không tạo cuộc gọi mới; dùng lại operation ID cho route khác nhận `Media.OperationConflict` 409.
- Đổ chuông 30 giây: tự kết thúc `no_answer`; nhận sau deadline bị 409; hai người có thể gọi lại.
- Hai tab người nhận chạy accept đồng thời: một response 200, một response 409. Chỉ tab thắng nối LiveKit; tab còn lại thấy trạng thái công khai, không xin được grant hoặc kết thúc thay tab sở hữu. Người ngoài đọc/xin grant nhận 404; gọi người đang bận nhận `Media.PeerBusy`; grant giả nhận 401.
- Thu hồi phiên: logout thật của người nhận làm kết thúc cuộc gọi với `session_revoked` trong ngưỡng 10 giây của test; PeerConnection đóng; dùng grant SFU cũ để nối lại bị 409.
- Kiểm tra Compose hợp lệ, `git diff --check` sạch; kiểm tra tài liệu: 108 file, 2.692 liên kết nội bộ, không lỗi.

Camera/micro trong E2E là thiết bị giả của Chromium, không phải camera vật lý. SFU và truyền RTP là thật, không mock provider/transport. Screenshot local: `artifacts/media/two-browser-call.png` (artifact không commit).

Kết quả cuối có đủ **5 ca E2E riêng biệt đã pass** trên bản build cuối. Lượt tổng cuối có 4 ca pass và ca video vượt mốc chờ mở camera 5 giây (giao diện đang chờ thiết bị, chưa báo lỗi). Điều chỉnh test đợi mở micro/camera tối đa 15 giây, chạy lại ca video: pass trong 19,6 giây. Không đổi assertion RTP hoặc bỏ bước kiểm tra. Một lượt trước bị SIGTERM từ môi trường sau hai test đầu; đã chạy lại các ca còn lại. Bản ghi JSON local: `artifacts/media/results.json` và `artifacts/media/video-results.json`. Các lệnh chạy và phạm vi API/giới hạn nằm ở [hướng dẫn](../features/media/local-direct-calls.md).

## Giới hạn nghiệm thu

Đây là kiểm chứng direct-call local trên một instance Media/SFU. Chưa nghiệm thu thiết bị vật lý, Internet/TURN, mobile, phòng thoại nhóm, các participation/source API, distributed quota hoặc cưỡng chế ngắt trong 5 giây khi SFU/control plane bị phân hoạch. Không đánh dấu toàn bộ Media v1 hoàn tất. Schema Media bootstrap chỉ áp dụng volume mới; không dùng file init để nâng cấp volume có dữ liệu.

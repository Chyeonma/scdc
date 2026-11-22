# Chạy cuộc gọi riêng trên máy phát triển

Bản triển khai này thêm gọi thoại/video 1–1 vào DM: gọi đến, nhận, từ chối, hủy, hết thời gian chờ 30 giây, kết thúc, bật/tắt micro và camera. Hai thiết bị bắt đầu với camera/micro **tắt**. Người dùng phải có tài khoản active, email đã xác minh và đang mở ứng dụng có kết nối Media. Không có ghi âm hoặc dịch vụ egress.

## Khởi động

Yêu cầu Docker Compose, Python 3. Chạy tại thư mục gốc repo:

```sh
python3 scripts/init_media_env.py
docker compose -f compose.yaml -f compose.media.yaml up -d --build web-client
```

Nếu đã có `.env`, script giữ nguyên file đó. Thêm những biến thiếu dựa trên `.env.example`; dùng giá trị ngẫu nhiên bền vững, không commit file `.env`. `COMMUNITY_OPERATION_KEY` phải là base64 của ít nhất 32 byte; LiveKit secret ít nhất 32 ký tự.

Mở http://localhost:3000 bằng hai hồ sơ trình duyệt hoặc một cửa sổ thường và một cửa sổ ẩn danh. Đăng nhập hai tài khoản khác nhau đã xác minh email. Tạo/mở DM, nhấn **Gọi**, nhận cuộc gọi ở cửa sổ kia rồi bật micro/camera. Trình duyệt cần được cấp quyền thiết bị. Bản development hiện tại có trả verification token từ API đăng ký; dùng `/api/v1/auth/verify-email` để xác minh tài khoản thử, giống các E2E hiện có.

Compose cơ sở vẫn chạy được khi không dùng overlay Media; nút Gọi chỉ xuất hiện khi frontend build với `VITE_MEDIA_ENABLED=true`. Khi chạy Vite trực tiếp, đặt biến này và cấu hình proxy tương tự các route trong `nginx.conf`.

LiveKit server được ghim `v1.13.11`, JS SDK `2.25.0`. Mặc định quảng bá ICE về `127.0.0.1`, phục vụ trình duyệt trên chính máy Docker. Các cổng RTC là TCP 7881 và UDP 50000–50020. Không public cổng signaling/admin 7880. Khi đưa lên server thật phải cấu hình địa chỉ có thể truy cập, HTTPS/WSS và TURN phù hợp; cấu hình localhost này chưa phải cấu hình triển khai Internet.

## Cấu trúc và luồng

- `services/Media/SCDC.Media.Domain`: trạng thái và quyền thao tác cuộc gọi.
- `services/Media/SCDC.Media.Application`: interface database, Identity authority và nhà cung cấp media.
- `services/Media/SCDC.Media.Infrastructure`: PostgreSQL, HTTP adapter Identity và LiveKit RoomService/JWT.
- `services/Media/SCDC.Media.Api`: API, SignalR, registry từng tab, admission proxy và worker dọn phòng.
- `clients/WebClient/src/media`: trạng thái realtime và giao diện trong frontend chung.
- `database/media/001-direct-calls.sql`: database riêng, không FK/query xuyên Identity, Community hoặc Messaging.

Media chỉ giữ khóa JWT công khai của Identity. Mỗi lần đăng ký kết nối, thao tác và kiểm tra cuộc gọi đang diễn ra, Media gọi API `/users/me` hiện có bằng token người dùng để xác nhận session còn hợp lệ. Đây là adapter HTTP tạm thời, có thể thay bằng authority gRPC sau này; Media không tham chiếu assembly hay database của monolith. Identity bearer chỉ ở bộ nhớ registry, không ghi vào database hoặc gửi cho LiveKit.

PostgreSQL giữ cuộc gọi, quyền giữ chỗ của hai người và receipt chống lặp thao tác. Giao dịch có advisory lock để một người chỉ có một cuộc gọi, và tối đa 20 người trong các cuộc gọi active/chờ xác nhận dọn phòng. Hai tab nhận đồng thời dùng cùng expectedVersion: chỉ một tab thắng; quyền tham gia gắn với user/session/clientInstanceId của tab đó. UUIDv4 instance chỉ nằm trong RAM, call ID là UUIDv7 và version truyền dưới dạng chuỗi số nguyên.

Khi nhận, backend tạo phòng LiveKit tối đa hai người. Chỉ hai tab sở hữu cuộc gọi được xin JWT tham gia ngắn hạn. Nginx chuyển `/livekit/*` qua admission proxy; proxy xác minh JWT, trạng thái cuộc gọi, session và quyền sở hữu trước khi nối signaling tới SFU. Kết thúc/thu hồi phiên làm đóng signaling và xóa phòng. Nếu chưa xóa được phòng, database vẫn giữ chỗ; worker thử lại thay vì cấp cuộc gọi mới cho người đó. Worker restart kết thúc các cuộc gọi cũ để tránh phục hồi registry/token không còn tồn tại.

Một cuộc gọi active cho phép mất kết nối tối đa 30 giây. Đóng/reload tab tạo instance mới, không chiếm cuộc gọi của instance cũ. Tab mới chỉ xem trạng thái; chờ cuộc gọi cũ kết thúc trước khi gọi lại. Có nút Nối lại khi SDK mất kết nối hoàn toàn. SFU có thể cấp token reconnect không giữ custom claim của ứng dụng; nếu reconnect tự động bị từ chối, dùng Nối lại để xin grant mới trong khoảng chờ.

## API của bản triển khai này

Đây là tập con thực thi cho local direct calls, chưa thực thi toàn bộ `media.openapi.json`/schema realtime của thiết kế v1. Prefix `/api/v1/media`; mọi endpoint trừ health yêu cầu JWT Identity. Lỗi trả `{code,detail}`. Các response liên quan token/state có `Cache-Control: no-store`.

| Endpoint | Nội dung |
|---|---|
| `GET /health` | Liveness của service |
| SignalR `/hubs/media`, `RegisterMediaClient(clientInstanceId)` | Đăng ký/heartbeat tab mỗi 5 giây; xác minh session, không tự join phòng |
| `GET /state?clientInstanceId=UUIDv4` | `{call:null}` hoặc snapshot cuộc gọi hiện tại/gần nhất |
| `GET /calls/{id}?clientInstanceId=UUIDv4` | Snapshot, người ngoài nhận 404 |
| `POST /calls` | `{peerUserId,clientInstanceId,clientOperationId}` |
| `POST /calls/{id}/accept`, `/reject`, `/cancel`, `/end` | `{expectedVersion,clientInstanceId,clientOperationId}` |
| `POST /calls/{id}/connection` | `{clientInstanceId}` → `{callId,serverUrl,token}`; token không lưu browser storage |

Snapshot gồm `id,callerUserId,peerUserId,callerName,peerName,status,endReason,version,ringExpiresAt,ownsCall`. Sự kiện `IncomingCall`/`CallChanged` chỉ mang `{callId,status,version}` để client đọc lại snapshot đã kiểm quyền; tab khác không nhận grant. GET đồng bộ mỗi 5 giây cũng phục hồi sự kiện bị lỡ. Mutation không tự replay; nếu mất response, nút **Kiểm tra và thử lại** dùng lại operation ID/payload trong RAM. Receipt trả trạng thái hiện tại, không phục hồi trạng thái cũ. Đổi payload/route cho operation đã dùng nhận 409.

## Phạm vi còn lại so với thiết kế v1

Không đánh dấu toàn bộ Media v1 hoàn tất. Bản này chỉ chạy một instance Media + một SFU; registry, admission lease và kiểm tra authority ở RAM. Chưa có distributed lease/outbox cho nhiều replica, gRPC authority, phòng thoại trong Community, UI chia sẻ màn hình, điều khiển nguồn phát theo participation API hoặc SFU-side track/lease enforcement. Khả năng publish các loại nguồn SDK không thay thế hạn mức nguồn phát của thiết kế.

Stock LiveKit không bảo đảm ngắt media trong 5 giây khi control plane/SFU bị phân hoạch: đóng signaling không đồng nghĩa ngắt ICE đang chạy nếu SFU không xác nhận DeleteRoom. Worker revalidate và retry là best effort. Không công bố đạt SLA thu hồi/quota phân tán v1; vẫn cần cơ chế cưỡng chế lease phía SFU được mô tả trong `participation-lifecycle.md`. Receipt/call history chưa có job retention. Schema SQL hiện là bootstrap cho volume mới, chưa có công cụ nâng cấp database Media đang sử dụng. Không chạy song song nhiều Media instance với recovery toàn bộ database hiện tại.

## Kiểm tra lại

```sh
dotnet test tests/SCDC.Media.Tests
cd clients/WebClient
npm ci
npm test
SCDC_MEDIA_URL=http://localhost:3000 npx playwright test --project media
```

Cài Chromium của Playwright trước, hoặc đặt `SCDC_BROWSER_EXECUTABLE` trỏ tới Chromium sẵn có. E2E dùng camera/micro giả của Chromium nhưng đi qua API, SignalR, database và LiveKit thật, kiểm tra cả số byte RTP audio/video nhận ở hai phía. Dùng database thử riêng; các test tạo tài khoản ngẫu nhiên. Bản ghi kết quả thực tế nằm trong [media-direct-calls verification](../../verification/media-direct-calls.md).

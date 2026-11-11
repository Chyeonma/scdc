# SCDC — Cuộc gọi riêng

Hai người có thể gọi cho nhau từ cuộc trò chuyện riêng (DM). Không cần kết bạn hay cùng server. Người nhận phải bấm nhận thì cuộc gọi mới bắt đầu.

**Trạng thái:** đã thiết kế, chưa có code (backend, API, LiveKit).

v1 không có gọi nhóm ngoài server, và không lưu cuộc gọi nhỡ vào lịch sử DM.

<a id="flows"></a>

## Quy tắc

- Cả hai người phải đủ điều kiện nhắn tin riêng với nhau. Không gọi cho chính mình.
- Khi người nhận bấm nhận, phiên của người gọi vẫn phải còn hiệu lực.
- Sau khi bắt đầu, nếu một bên rời, mất phiên hoặc mất quyền thì cả cuộc gọi kết thúc.
- Ban đầu micro và camera tắt ([điều khiển nguồn](video-screen-sharing.md#rules)).
- Không có chức năng đuổi, tắt tiếng hay ghi âm người khác.

### Nhiều tab và nhiều thiết bị

- Cuộc gọi đến được báo trên mọi phiên máy tính đang mở của người nhận. Lần bấm nhận đầu tiên thắng; các nơi khác ngừng đổ chuông và không tự chuyển cuộc gọi sang.
- Mỗi tab có một mã riêng `clientInstanceId` (UUIDv4, chỉ giữ trong bộ nhớ của tab). Lượt tham gia gắn với người dùng, phiên, tab và epoch kết nối. Các mã này chỉ để phân biệt phiên, không thay cho xác thực.
- Mỗi lượt tham gia chỉ có một kết nối LiveKit. Sao chép token sang tab khác không chiếm được kết nối đang chạy.
- Quyền LiveKit (grant) chỉ trả cho đúng tab sở hữu. Thông tin trạng thái gửi chung không chứa grant, token, mã phiên hay security stamp.

### Các trạng thái

| Trạng thái | Sự kiện | Kết quả |
|---|---|---|
| Bắt đầu gọi | Kiểm tra hai tài khoản, phiên và điều kiện DM | Đổ chuông nếu người nhận khả dụng; báo bận hoặc vắng mặt nếu không. Chưa ai nghe được gì |
| Đang đổ chuông | Người nhận bấm nhận trong 30 giây | Kiểm tra lại phiên, trạng thái và bận; cấp đủ hai chỗ trong một transaction rồi bắt đầu kết nối |
| Đang đổ chuông | Từ chối, người gọi hủy, hoặc hết 30 giây | Kết thúc (từ chối, đã hủy, không trả lời). Không ghi cuộc gọi nhỡ |

Sau khi nhận, cả hai đi theo [vòng đời tham gia](participation-lifecycle.md#flows): đang kết nối, đã kết nối, kết nối lại (tối đa 30 giây), kết thúc.

<a id="ux"></a>

## Giao diện

Người gọi bấm nút gọi trong cuộc trò chuyện. Người gọi thấy trạng thái chờ, đã nhận hoặc không nhận.

```text
MEDIA-S01 · Gọi đến
┌────────────────────────────┐
│ Tên hiển thị · @username   │
│ Đang gọi đến               │
│ [Từ chối]       [Nhận]     │
└────────────────────────────┘
```

- MEDIA-S04 báo người nhận bận, vắng mặt, hoặc hệ thống hết chỗ.
- Không tự thử lại khi chưa rõ kết quả nhận hay tạo cuộc gọi.
- Sau khi nhận: màn hình cuộc gọi MEDIA-S02 (có điều khiển nguồn); MEDIA-S03 khi đang kết nối lại.

Wireframe là đề xuất, chưa có prototype hay review.

<a id="contracts"></a>

## API

Schema mục tiêu: [media.openapi.json](../../contracts/media.openapi.json). Chưa endpoint nào được làm. Tiền tố `/api/v1`. Server tự xác định người gọi từ phiên đăng nhập. Mã lỗi xem [danh mục lỗi chung](participation-lifecycle.md#errors).

| Endpoint | Đầu vào → Trả về | Điều kiện |
|---|---|---|
| POST `/media/calls` | `peerUserId`, mã thao tác, mã tab → CallSnapshot | Hai người đủ điều kiện, người nhận đang online trên máy tính, cả hai đang rảnh |
| GET `/media/calls/{callId}` | CallSnapshot | Chỉ hai người trong cuộc gọi; kiểm tra phiên và quyền trước |
| POST `/media/calls/{callId}/accept` | mã thao tác, mã tab, `expectedVersion` → CallSnapshot | Người nhận |
| POST `/media/calls/{callId}/reject` | như trên | Người nhận |
| POST `/media/calls/{callId}/cancel` | như trên | Người gọi, khi đang đổ chuông |
| POST `/media/calls/{callId}/end` | như trên | Một trong hai người, sau khi đã nhận |

GET `/media/state`, cấp quyền LiveKit và heartbeat xem [API chung](participation-lifecycle.md#contracts).

### Dữ liệu

Bảng đề xuất `calls`: người gọi, người nhận, phiên và tab của người gọi, phiên và tab của người nhận (sau khi nhận), trạng thái, lý do kết thúc, `ringExpiresAt`, `version`, `roomId`. Không lưu thành tin DM.

Phòng, lượt tham gia, hạn mức và outbox dùng [mô hình chung](participation-lifecycle.md#data).

### Xử lý

1. **Tạo cuộc gọi:** kiểm tra hai người và trạng thái online; khóa trạng thái bận của cả hai; tạo cuộc gọi `ringing` với `ringExpiresAt` = giờ server + 30 giây. Nếu bận hoặc vắng mặt thì trả lỗi, không cấp gì. Khi đổ chuông, cả hai bị tính là bận để tránh gọi chồng.
2. **Nhận cuộc gọi:** kiểm tra người nhận, phiên người gọi còn hiệu lực, và chưa hết giờ. Khóa cuộc gọi và kiểm tra `expectedVersion`. Nếu hệ thống còn dưới hai chỗ, giữ trạng thái đổ chuông và trả `Media.SystemFull`, không tạo lượt tham gia lẻ. Nếu đủ chỗ, cấp hai lượt tham gia và chuyển sang đang kết nối trong cùng một commit. Người gọi lấy quyền LiveKit qua API trạng thái, không nhận token qua sự kiện realtime.

Mọi thao tác ghi dùng chung [thứ tự khóa và chống trùng](participation-lifecycle.md#transaction). Lệnh gửi LiveKit chạy sau commit, nên rollback không để lại gì trên LiveKit. Cùng một thao tác dùng lại `clientOperationId`; giao diện không tự thử lại. Khi chưa rõ kết quả, client đọc lại trạng thái, không tự nhận hay tạo lượt mới.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-01 | Hai người gọi cho nhau rồi bật micro | Người nhận nhận hoặc từ chối được; sau khi nhận và bật micro, hai bên nghe được nhau; ban đầu thiết bị tắt |
| AC-MEDIA-06 | A gọi B; B chưa nhận, sau đó nhận | Trước khi B nhận, cuộc gọi chưa bắt đầu; sau khi nhận, hai bên vào cuộc gọi |
| AC-MEDIA-07 | A gọi B nhưng B không nhận | Cuộc gọi không bắt đầu; không có mục cuộc gọi nhỡ trong lịch sử |
| AC-MEDIA-14 | Nhận, từ chối, hủy, hết giờ xảy ra cùng lúc hoặc trên nhiều thiết bị | Chỉ có một kết quả cuối; không cuộc gọi nào bắt đầu sau khi đã hủy hoặc hết giờ |
| AC-MEDIA-16 | Người nhận online trên hai thiết bị, cả hai bấm nhận | Lần nhận đầu thắng; thiết bị kia ngừng đổ chuông hoặc báo bận, không chiếm cuộc gọi |

| Mã | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-MEDIA-01 | Gọi người khả dụng, vắng mặt, bận; nhận, từ chối, hủy, hết 30 giây | Đúng trạng thái; chưa nhận thì chưa nghe; không lưu cuộc gọi nhỡ | AC-MEDIA-01, 06, 07, 14 |
| TC-MEDIA-11 | Nhận trên hai thiết bị cùng phiên bản, rồi tab thứ ba thử vào | Một bên thắng; nơi khác ngừng đổ chuông hoặc báo bận; không tự chuyển | AC-MEDIA-16 |

TC-MEDIA-09 (callback LiveKit, nhận và hủy cùng lúc) và TC-MEDIA-05 (bận trên nhiều thiết bị) xem [vòng đời tham gia](participation-lifecycle.md#acceptance).

Chưa ca nào được chạy. Khi chạy, ghi rõ commit, phiên bản LiveKit, môi trường và thiết bị theo [phương pháp đo](../../system/quality.md#quality-targets).

<a id="status"></a>

## Còn mở

- Quy tắc "đang đổ chuông thì cả hai bị tính là bận" cần xem lại cùng thiết kế nhiều thiết bị.
- OQ-006 và MEDIA-GAP-01, 02, 05: giới hạn tần suất, bằng chứng cho các trường hợp nhận, hủy, hết giờ, nhiều thiết bị xảy ra cùng lúc. Định nghĩa xem [vòng đời tham gia](participation-lifecycle.md#status).

---

Nguồn: REQ-007, REQ-008, REQ-009, SCP-006, SCP-007, DEC-046, DEC-047, DEC-078 (phạm vi), DEC-100 (nhiều thiết bị), DEC-101 (thiết bị ban đầu tắt). Đối chiếu tài liệu ngày 2026-10-06.

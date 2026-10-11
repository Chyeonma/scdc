# SCDC — Cuộc gọi riêng

Phạm vi REQ-007/008/009 và SCP-006/007 của v1. Hai người gọi từ hội thoại riêng; không cần kết bạn hoặc cùng Community, người nhận phải accept trước khi nghe media. Cuộc gọi nhóm ngoài server không thuộc v1; cuộc gọi nhỡ không ghi vào lịch sử DM (DEC-046, DEC-047, DEC-078).

## Mục lục

- [Điều kiện và luồng](#flows)
- [Giao diện](#ux)
- [API, dữ liệu và transaction](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="flows"></a>

## Điều kiện và luồng

Gọi riêng kiểm tra cả hai user đủ điều kiện DM, không gọi chính mình và không đòi kết bạn/cùng cộng đồng. User/session phía gọi phải còn hợp lệ khi accept; accept gắn phiên phía nhận. Nếu một bên mất phiên/quyền tài khoản hoặc rời sau accept, kết thúc cả cuộc gọi hai người. Trong phòng thoại chỉ kết thúc participation bị ảnh hưởng; xóa phòng kết thúc tất cả. Quyền quản lý phòng không cấp quyền tắt/bật thiết bị người khác; v1 không thêm kick/mute/ghi âm cuộc gọi từ quyền quản lý.

`clientInstanceId` là UUIDv4 trong RAM tab, không dùng ID session chung nhiều tab thay nó. Participation gắn user, session, client instance và connection epoch. Các ID này phục vụ phân biệt phiên, không thay Bearer hoặc kiểm tra quyền. Mỗi participation có một kết nối provider hợp lệ; sao chép token sang tab khác không được thay kết nối đang hoạt động. Grant chỉ trả cho phiên/tab sở hữu; snapshot chung không chứa grant, token, session ID hoặc security stamp.

DEC-100: báo cuộc gọi đến cho các phiên desktop khả dụng; lần accept đầu thắng, nơi khác ngừng đổ chuông, không tự chiếm/chuyển participation. Ringing tối đa 30 giây; hai participation chỉ được cấp sau accept. Thiết bị ban đầu tắt theo [DEC-101 và điều khiển nguồn](video-screen-sharing.md#rules).

| Luồng/trạng thái | Trigger | Kết quả |
|---|---|---|
| Gọi riêng / requested | Kiểm tra hai tài khoản, phiên và điều kiện DM | Ringing nếu người nhận khả dụng; busy/unavailable nếu không; không cấp quyền nghe media trước accept |
| Ringing | Người nhận accept trong 30 giây | Kiểm tra lại phiên/trạng thái/busy, cấp đủ hai slot trong một giao dịch rồi connecting |
| Ringing | Reject, caller cancel hoặc hết 30 giây | Rejected/cancelled/no-answer; không tạo lịch sử cuộc gọi nhỡ trong DM |

Sau accept, cả hai theo [vòng đời participation](participation-lifecycle.md#flows): connecting, connected, reconnect tối đa 30 giây, ended và provider draining. Một phía rời hoặc mất phiên/quyền sau accept kết thúc cả cuộc gọi.

<a id="ux"></a>

## Giao diện

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Gọi riêng | Thao tác gọi từ hội thoại và màn hình cuộc gọi đến. | Người nhận bấm chấp nhận trước khi bắt đầu; người gọi biết trạng thái chờ/đã nhận/không nhận. Cuộc gọi nhỡ chưa lưu vào hội thoại ở đợt đầu. |

```text
MEDIA-S01 · Gọi đến
┌────────────────────────────┐
│ Tên hiển thị · @username   │
│ Đang gọi đến               │
│ [Từ chối]       [Nhận]     │
└────────────────────────────┘
```

MEDIA-S04 báo người nhận bận/vắng mặt hoặc hệ thống hết chỗ; không tự retry accept/create khi kết quả chưa rõ. Sau nhận dùng MEDIA-S02 với điều khiển nguồn, MEDIA-S03 khi reconnect theo các chủ đề liên quan. Wireframe là đề xuất, chưa có prototype/kết quả review.

<a id="contracts"></a>

## API, dữ liệu và transaction

[media.openapi.json](../../contracts/media.openapi.json) là schema HTTP mục tiêu, mọi endpoint chưa triển khai. Prefix `/api/v1`; actor/session do server lấy, lỗi theo [catalogue dùng chung](participation-lifecycle.md#errors).

| Endpoint công khai | Đầu vào/kết quả | Điều kiện |
|---|---|---|
| POST `/media/calls` | peerUserId + operation/instance → CallSnapshot | Hai user đủ điều kiện, presence desktop, claims trống |
| GET `/media/calls/{callId}` | CallSnapshot | Chỉ hai user, kiểm tra phiên/quyền trước trả metadata |
| POST `/media/calls/{callId}/accept`, `/reject`, `/cancel`, `/end` | operation/instance/expectedVersion → CallSnapshot | Accept/reject phía nhận; cancel phía gọi khi ringing; end một trong hai binding khi đã accept |

GET `/media/state` và grant/heartbeat của participation ở [hợp đồng lifecycle](participation-lifecycle.md#contracts). Snapshot không chứa JWT/grant/session ID/security stamp cho các tab khác.

| Bảng đề xuất | Trường/ràng buộc cần review |
|---|---|
| `calls` | caller/callee, caller session/instance, callee binding sau accept, state, reason, ringExpiresAt, version, roomId; không lưu thành tin DM/lịch sử cuộc gọi nhỡ |

Room/participation/claims/capacity/operation/outbox có một model dùng chung ở [lifecycle](participation-lifecycle.md#data). Call direct chỉ có hai user đã accept; hai slot phải được cấp atomically.

1. Tạo call kiểm tra hai user và presence desktop; khóa hai user claims rồi tạo `ringing` với ringExpiresAt = thời điểm server +30 giây. Nếu bận/vắng mặt trả lỗi tương ứng, không cấp participation/token. Ringing chiếm ngữ cảnh hai user để tránh lời gọi chồng; quy tắc này còn cần review cùng lựa chọn nhiều thiết bị.
2. Accept kiểm tra người nhận, phiên gọi vẫn hoạt động và `now < ringExpiresAt`. Khóa call, kiểm tra expectedVersion; nếu toàn hệ thống còn dưới hai chỗ thì cả call giữ ringing, trả `Media.SystemFull`, không tạo một participation lẻ. Đủ chỗ thì cấp hai participation, chuyển hai claim và call/room sang connecting trong cùng commit. Caller lấy participation/grant qua state, không đính JWT vào sự kiện gửi nhiều tab.

Tất cả writer giữ cùng [thứ tự khóa và idempotency](participation-lifecycle.md#transaction). Lệnh provider chạy sau commit; rollback không tạo side effect SFU. `clientOperationId` dùng lại cho cùng thao tác, UI `retry:false`; khi chưa rõ kết quả đọc state, không tự accept hay cấp participation mới.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-01 | Hai người đã đăng nhập thực hiện cuộc gọi riêng rồi bật micro. | Người nhận có thể nhận hoặc từ chối; sau nhận và bật micro, hai bên nghe được nhau; ban đầu thiết bị tắt theo DEC-101. |
| AC-MEDIA-06 | A gọi riêng cho B; B chưa bấm nhận, sau đó bấm chấp nhận. | Trước khi B chấp nhận, cuộc gọi chưa bắt đầu; sau khi B chấp nhận, hai bên vào cuộc gọi. |
| AC-MEDIA-07 | A gọi riêng cho B nhưng B không nhận trong đợt đầu. | Cuộc gọi không bắt đầu; không tạo mục cuộc gọi nhỡ trong lịch sử hội thoại. |
| AC-MEDIA-14 | Accept/reject/cancel/timeout xảy ra đồng thời hoặc trên nhiều thiết bị | Chỉ một trạng thái cuối; không có cuộc gọi bắt đầu sau cancel/timeout; không tạo cuộc gọi nhỡ trong lịch sử DM |
| AC-MEDIA-16 | Người nhận online ở hai thiết bị, cả hai bấm nhận | Lần accept đầu thắng; thiết bị còn lại ngừng đổ chuông/báo bận, không chiếm hoặc chuyển phiên theo DEC-100 |

Mọi AC-MEDIA/TC-MEDIA chưa có kết quả chạy được ghi nhận. Dùng tài khoản thử, provider/môi trường và thiết bị có phiên bản cụ thể; kết quả phải gắn commit/provider build/môi trường theo [phương pháp đo](../../system/quality.md#quality-targets).

| Mã | Thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-MEDIA-01 | Gọi người khả dụng/vắng mặt/bận; accept/reject/cancel/timeout 30 giây | Đúng trạng thái, chưa accept chưa nghe; không lưu missed call | AC-MEDIA-01/06/07/14 |
| TC-MEDIA-11 | Accept hai thiết bị cùng version, rồi tab thứ ba join | Một bên thắng, nơi khác ngừng ring/busy; không tự handoff | AC-MEDIA-16 |

TC-MEDIA-09 kiểm tra provider callback/accept-cancel và TC-MEDIA-05 về claim đa thiết bị ở [lifecycle](participation-lifecycle.md#acceptance).

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu tài liệu ngày 2026-10-06: chưa có backend/API/provider thực thi gọi riêng; contract, coordinator và UX là thiết kế mục tiêu. OQ-006, MEDIA-GAP-01/02/05 còn review limiter và proof accept/cancel/timeout/multi-device race. Định nghĩa các MEDIA-GAP và bằng chứng cần có ở [lifecycle](participation-lifecycle.md#status). Chưa có kết quả AC/TC hoặc nghiệm thu media/provider.

# SCDC — Phòng thoại trong Community

Phạm vi phòng thoại nhiều thành viên của v1. Thành viên có quyền View vào ngay; không có quyền không thấy/không vào được (DEC-048). Video và chia sẻ dùng chung cơ chế nguồn với gọi riêng; không thêm quyền kick/mute/ghi âm từ quyền quản lý phòng.

## Mục lục

- [Quy tắc và luồng](#flows)
- [Giao diện](#ux)
- [API, dữ liệu và transaction](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="flows"></a>

## Quy tắc và luồng

Community kiểm tra member, membership epoch, channel loại voice chưa deleted và quyền View hiện hành. Identity kiểm tra active/verified và session/stamp. Media cấp participation/room/quota; LiveKit không tự tạo membership hoặc bỏ qua quyền Community.

| Luồng/trạng thái | Trigger | Kết quả |
|---|---|---|
| Phòng thoại / chưa tham gia | Join với quyền xem | Kiểm tra member/phòng còn tồn tại, giới hạn 10/20 và một phiên tham gia/người; allocate slot rồi connecting |

Giới hạn 10 người/phòng và 20 toàn hệ thống gồm reserved/connected/reconnect/draining; mỗi user có một ngữ cảnh gọi. Room đầy từ chối, không để token/slot dở dang. Mất quyền chỉ kết thúc participation bị ảnh hưởng; xóa phòng kết thúc tất cả, cutoff ≤5 giây từ commit. Reconnect giữ chỗ tối đa 30 giây theo [lifecycle](participation-lifecycle.md#flows).

<a id="ux"></a>

## Giao diện

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Vào phòng thoại | Danh sách phòng và màn hình cuộc gọi nhóm. | Thành viên có quyền xem phòng vào ngay; người không có quyền không thấy/không vào được. |

MEDIA-S02 hiển thị room/người/nguồn và nút rời theo [điều khiển nguồn](video-screen-sharing.md#ux). MEDIA-S04 báo room/system full; MEDIA-S03 báo reconnect và hạn còn lại. GET room chưa có người phải trả snapshot rỗng, không giả 404. Wireframe/prototype chưa được kiểm chứng.

<a id="contracts"></a>

## API, dữ liệu và transaction

Schema mục tiêu tại [media.openapi.json](../../contracts/media.openapi.json); mọi endpoint chưa triển khai. Prefix `/api/v1`.

| Endpoint công khai | Đầu vào/kết quả | Điều kiện |
|---|---|---|
| GET `/servers/{serverId}/channels/{channelId}/media` | RoomSnapshot | Member có view; không trả danh sách nguồn/provider secret cho người mất quyền |
| POST cùng đường dẫn + `/join` | operation/instance → ParticipationSnapshot | Channel voice, guard view, capacity và một ngữ cảnh/user |

Leave, state/grant/heartbeat của participation dùng [hợp đồng lifecycle](participation-lifecycle.md#contracts); camera/share dùng [hợp đồng nguồn](video-screen-sharing.md#contracts). Mọi request đọc snapshot cũng kiểm tra quyền hiện hành.

Đề xuất thêm `IMediaRoomLifecycle` trong Contracts: tạo room DB rỗng cho voice channel cùng transaction Community create, đánh dấu closing khi xóa; chưa provision SFU khi room rỗng. Nhờ vậy GET phòng thoại chưa có người vẫn trả snapshot rỗng, không giả 404 phòng không tồn tại. Migration backfill room cho voice channel hiện có; vòng đời Media/Community qua shared scope, không tham chiếu implementation hoặc gọi SFU trong transaction. Interface/schema/source hiện chưa có.

Room/participation/epoch/counter là model dùng chung ở [lifecycle](participation-lifecycle.md#data); room generation đang mở của mỗi channel là duy nhất. Không gọi provider trong transaction Community create/delete.

3. Join phòng kiểm tra guard view, loại voice và epoch; cấp đúng một chỗ khi global <20, room <10 và user không có claim. Rời/kết thúc chuyển participation sang ended và chỗ provider sang draining; hết quyền ngay, chỗ chỉ tái cấp sau bằng chứng provider đã ngừng.

Thứ tự khóa/guard/counter và idempotency dùng chung ở [lifecycle](participation-lifecycle.md#transaction). Chỗ draining chỉ tái cấp khi có bằng chứng SFU ngừng; HTTP leave/websocket close chưa đủ. Các điều kiện transaction hiện giả định một API host, phải review theo microservice v1.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-02 | Thành viên có quyền xem phòng thoại trong cộng đồng chọn vào phòng. | Vào ngay và rời được; thành viên không có quyền xem bị từ chối. |

Mọi AC-MEDIA/TC-MEDIA chưa có kết quả chạy được ghi nhận. Dùng tài khoản thử, provider/môi trường và thiết bị có phiên bản cụ thể; kết quả phải gắn commit/provider build/môi trường theo [phương pháp đo](../../system/quality.md#quality-targets).

| Mã | Thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-MEDIA-02 | Join/rời phòng với member có/không quyền xem | Chỉ đúng quyền tham gia; rời giải phóng slot | AC-MEDIA-02 |

AC-MEDIA-10/11/15, TC-MEDIA-05/06/08 kiểm chứng đa thiết bị, quota và thu hồi ở [lifecycle](participation-lifecycle.md#acceptance); nguồn share ở [video-screen-sharing](video-screen-sharing.md#acceptance).

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu tài liệu ngày 2026-10-06: View, giới hạn và cutoff/fail-close đã chốt; room lifecycle/reservation/draining, interface và schema còn là thiết kế. Chưa có module Media/API/provider hoặc migration/guard/SFU proof. OQ-004/006 và MEDIA-GAP-02/03/04 theo dõi ở [lifecycle](participation-lifecycle.md#status); chưa có kết quả chạy AC/TC hoặc nghiệm thu.

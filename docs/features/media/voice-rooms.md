# SCDC — Phòng thoại

Phòng thoại là một loại phòng trong server. Thành viên có quyền xem phòng thì bấm vào là tham gia ngay; người không có quyền thì không thấy và không vào được.

**Trạng thái:** đã thiết kế, chưa có code. Chưa có module Media, API, LiveKit hay migration.

Video và chia sẻ màn hình dùng chung cơ chế với cuộc gọi riêng, xem [micro, camera và chia sẻ màn hình](video-screen-sharing.md). Quyền quản lý phòng không cho phép đuổi, tắt tiếng hay ghi âm người khác; v1 không có các chức năng đó.

<a id="flows"></a>

## Quy tắc

Khi người dùng vào phòng, hệ thống kiểm tra:

- **Identity:** tài khoản đang hoạt động, đã xác minh, phiên đăng nhập còn hiệu lực.
- **Community:** là thành viên (đúng epoch), phòng là loại thoại, chưa bị xóa, và có quyền xem.
- **Media:** còn chỗ, và người dùng chưa ở cuộc gọi hay phòng thoại nào khác.

LiveKit không tự quyết định ai được vào; nó chỉ làm theo Media.

| Giới hạn | Giá trị |
|---|---|
| Người mỗi phòng | 10 |
| Người toàn hệ thống | 20 |
| Phòng hoặc cuộc gọi mỗi người cùng lúc | 1 |
| Giữ chỗ khi mất kết nối | Tối đa 30 giây |

Số người được tính gồm cả người đang kết nối, đang ở trong phòng, đang kết nối lại và đang thoát. Phòng đầy thì từ chối, không cấp token hay giữ chỗ dở dang.

- **Mất quyền** (rời server, bị đổi quyền): chỉ người đó bị ngắt.
- **Phòng bị xóa:** tất cả bị ngắt.
- Thời gian ngắt tối đa 5 giây kể từ lúc commit.

Chi tiết kết nối lại xem [vòng đời tham gia](participation-lifecycle.md#flows).

<a id="ux"></a>

## Giao diện

- Người dùng bấm vào phòng thoại trong danh sách phòng để vào ngay; màn hình cuộc gọi nhóm (MEDIA-S02) hiện tên phòng, người tham gia, nguồn phát và nút rời. Điều khiển nguồn xem [video và chia sẻ](video-screen-sharing.md#ux).
- MEDIA-S04 báo phòng đầy hoặc hệ thống đầy.
- MEDIA-S03 báo đang kết nối lại và thời gian còn lại.
- Phòng chưa có ai thì hiện danh sách trống, không báo lỗi "không tìm thấy".

Wireframe và prototype chưa được kiểm tra.

<a id="contracts"></a>

## API

Schema mục tiêu: [media.openapi.json](../../contracts/media.openapi.json). Chưa endpoint nào được làm. Tiền tố `/api/v1`.

| Endpoint | Trả về | Điều kiện |
|---|---|---|
| GET `/servers/{serverId}/channels/{channelId}/media` | RoomSnapshot | Thành viên có quyền xem. Người mất quyền không nhận danh sách nguồn hay bí mật của LiveKit |
| POST `/servers/{serverId}/channels/{channelId}/media/join` | ParticipationSnapshot | Phòng thoại, có quyền xem, còn chỗ, chưa ở cuộc gọi khác. Gửi kèm mã thao tác và mã tab |

Rời phòng, trạng thái, cấp quyền LiveKit và heartbeat dùng [API chung](participation-lifecycle.md#contracts). Camera và chia sẻ dùng [API nguồn phát](video-screen-sharing.md#contracts). Mọi request đọc đều kiểm tra lại quyền hiện tại.

### Dữ liệu và xử lý

Phòng, lượt tham gia, epoch và bộ đếm dùng chung một mô hình, xem [vòng đời tham gia](participation-lifecycle.md#data). Mỗi phòng thoại chỉ có một "thế hệ" phòng đang mở.

**Khi tạo phòng thoại:** Media chuẩn bị bản ghi phòng trống trước khi Community báo tạo xong. Bước này chưa tạo phòng trên LiveKit, chưa giữ chỗ, chưa cấp token. Community lưu tiến độ và điều phối; Media nhận lệnh qua gRPC và trả kết quả sau khi commit. Xem [tạo phòng](../community/channels.md#v1-channel-provisioning), [điều phối](../community/channels.md#v1-channel-creation-coordination) và [truyền lệnh](../community/channels.md#v1-channel-preparation-transport).

**Khi vào phòng:** kiểm tra quyền xem, loại phòng và epoch; cấp đúng một chỗ khi toàn hệ thống dưới 20, phòng dưới 10 và người dùng chưa ở cuộc gọi nào.

**Khi rời hoặc kết thúc:** lượt tham gia chuyển sang kết thúc, chỗ trên LiveKit chuyển sang đang thoát. Quyền mất ngay, nhưng chỗ chỉ được cấp lại khi có bằng chứng LiveKit đã ngừng luồng; request rời hay websocket đóng chưa đủ.

Không gọi LiveKit bên trong transaction tạo hoặc xóa phòng của Community. Thứ tự khóa và chống trùng xem [vòng đời tham gia](participation-lifecycle.md#transaction).

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-02 | Thành viên có quyền xem phòng thoại bấm vào phòng | Vào ngay và rời được; người không có quyền bị từ chối |

| Mã | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-MEDIA-02 | Vào và rời phòng với người có và không có quyền xem | Chỉ người có quyền vào được; rời thì giải phóng chỗ | AC-MEDIA-02 |

Nhiều thiết bị, hạn mức và thu hồi quyền (AC-MEDIA-10, 11, 15; TC-MEDIA-05, 06, 08) xem [vòng đời tham gia](participation-lifecycle.md#acceptance). Chia sẻ màn hình xem [video và chia sẻ](video-screen-sharing.md#acceptance).

Chưa ca nào được chạy. Khi chạy, dùng tài khoản thử, ghi rõ commit, phiên bản LiveKit, môi trường và thiết bị theo [phương pháp đo](../../system/quality.md#quality-targets).

<a id="status"></a>

## Còn mở

- Hợp đồng gRPC chi tiết khi tạo phòng, cấu trúc dữ liệu, chuyển dữ liệu cũ và phục hồi khi lỗi.
- Hợp đồng xóa phòng giữa Community và Media. Thiết kế cũ (`IMediaRoomLifecycle` trong cùng transaction) giả định một ứng dụng và một database, không dùng được cho v1.
- Quy tắc transaction hiện giả định một ứng dụng, phải xem lại cho microservice.
- Các mục OQ-004, OQ-006, MEDIA-GAP-02, 03, 04, theo dõi tại [vòng đời tham gia](participation-lifecycle.md#status).

---

Nguồn: DEC-048 (quyền vào phòng), [DEC-132 đến 134](../../records/decisions/README.md#dec-132) (tạo phòng). Đối chiếu tài liệu ngày 2026-10-06.

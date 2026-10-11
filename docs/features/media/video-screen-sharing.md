# SCDC — Micro, video và chia sẻ màn hình

Điều khiển thiết bị/nguồn dùng chung cho gọi riêng và phòng thoại trong v1. Media sở hữu nguồn, reservation, permit và trạng thái; thiết bị và provider phải xác nhận nguồn thực tế, không chỉ trạng thái nút UI.

## Mục lục

- [Quy tắc](#rules)
- [Giao diện và thao tác](#ux)
- [API, dữ liệu và đồng thời](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Hiện trạng](#status)

<a id="rules"></a>

## Quy tắc

Một mic/camera/screen mỗi participation; mỗi người tối đa một camera/màn hình, tối đa hai nguồn share/phòng (DEC-049, DEC-079). DEC-101: mic/camera ban đầu tắt, chưa share, cho vào chỉ để nghe khi chưa cấp quyền thiết bị. DEC-102: screen chỉ hình, audio screen phải dừng; tiếng nói qua mic, âm thanh tab/ứng dụng để sau. Người sở hữu bật nguồn bằng thao tác chủ động, không remote unmute người khác.

<a id="ux"></a>

## Giao diện và thao tác

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Thiết bị và chia sẻ | Điều khiển micro, camera, chia sẻ màn hình và danh sách nguồn. | Phản hồi khi không được cấp quyền; nhiều người có thể chia sẻ đồng thời trong phòng, mỗi nguồn phân biệt được. |

```text
MEDIA-S02 · Cuộc gọi / phòng thoại
┌────────────────────────────────────┐
│ Phòng / tên người · Đang kết nối   │
│ [Nguồn màn hình 1] [Nguồn 2]       │
│ [Camera / tên các thành viên]     │
│ Mic · Camera · Chia sẻ · [Rời]    │
└────────────────────────────────────┘
```

MEDIA-S05 báo thiết bị/quyền capture không dùng được hoặc hai màn hình đã đầy; hai nguồn có nhãn tác giả và thao tác chọn xem. Trạng thái UI phải chờ capture/provider xác nhận. Wireframe là thiết kế đề xuất, chưa có prototype/kết quả review.

<a id="device-điều-khiển-thiết-bị-và-bố-cục"></a>

### Điều khiển thiết bị và bố cục

Luồng bật nguồn: người dùng bấm → capture cục bộ/preview → xin reservation → SDK publish qua quota gate → provider xác nhận live → UI hiển thị đang phát. Không được xin quyền capture trong background. Mic/camera mặc định tắt theo DEC-101; nếu không có/quyền bị từ chối vẫn giữ trạng thái tắt, báo thao tác thử lại, không buộc rời chỉ vì thiếu thiết bị đầu vào. Chia sẻ chỉ hình theo DEC-102; mọi audio track do screen capture trả về phải dừng, grant/gate không nhận screen_share_audio.

Chia sẻ gọi `getDisplayMedia` trực tiếp trong thao tác bấm trước await network, người dùng chọn màn hình/cửa sổ/tab. [Bản nháp Screen Capture W3C](https://www.w3.org/TR/2026/WD-screen-capture-20260827/) yêu cầu chọn/cấp quyền mỗi lần và không bảo đảm trả audio; khả năng browser thực tế phải kiểm thử. Sau capture mới xin quota; lỗi đầy/cancel/expiry thì stop mọi local track vừa mở, không để preview giữ capture ngầm. Khi browser phát `ended`, ngừng publish và gửi stop; quota chỉ nhả sau provider xác nhận ngừng.

Mic mute/camera off phải tác động track thực tế, không chỉ đổi icon; thay camera/mic ngừng track cũ trước tạo nguồn mới, không vượt quota. Mute là trạng thái desired và confirmed riêng; nếu mất phản hồi, UI hiện đang xử lý/chưa xác nhận. Không cho server remote unmute người khác; quyền bật thiết bị thuộc người sở hữu. Rời/đăng xuất/thu hồi dừng local capture ngay và server cutoff độc lập, kể cả tab không hợp tác.

Reconnect giữ lựa chọn mic/camera trong RAM nhưng chỉ phát lại khi đúng participation/lease/source epoch; nguồn local đã ended phải bấm cấp lại. Soft resume có thể giữ share còn sống trong hạn; full reconnect hoặc đã ended thì không tự mở picker/chọn lại màn hình. Bố cục đề xuất: tối đa hai màn hình có tên tác giả, chọn nguồn chính xem lớn, nguồn còn lại thumbnail; danh sách tối đa 10 người/camera với nhãn thiết bị; không tự ẩn thông báo ended/full/reconnecting khi đổi layout. Không lưu token, source/participation hoặc lựa chọn thiết bị media vào localStorage/IndexedDB trong thiết kế này.

<a id="contracts"></a>

## API, dữ liệu và đồng thời

Schema mục tiêu ở [media.openapi.json](../../contracts/media.openapi.json); source API chưa triển khai. Các đường `/sources` dưới đây dùng prefix `/api/v1/media/participations/{participationId}`; grant/lifecycle/heartbeat ở [participation-lifecycle](participation-lifecycle.md#contracts).

| Endpoint công khai | Đầu vào/kết quả | Điều kiện |
|---|---|---|
| POST cùng đường dẫn + `/sources` | kind + operation/instance/version → SourceSnapshot | Participation hợp lệ, reservation quota; không tự mở thiết bị |
| POST cùng đường dẫn + `/sources/{sourceId}/stop` | operation/instance/sourceVersion → SourceSnapshot | Chỉ nguồn của chính participation; không cấp quyền unmute người khác |
| POST cùng đường dẫn + `/sources/{sourceId}/mute` | muted + operation/instance/sourceVersion → SourceSnapshot | Mic/camera; phản ánh cả desired và provider-confirmed state, screen dùng stop |

| Bảng đề xuất | Trường/ràng buộc cần review |
|---|---|
| `sources` | id, participation, kind, epoch, state, publishDeadline, providerTrackSid; một nguồn mic/camera/screen mỗi participation; screen reserved/live/draining tính vào tối đa 2/room |

Source reservation + source epoch phải được kiểm tra trước publish; permit chỉ cấp binding sở hữu, gắn connection/source epoch và publishDeadline. Repeat operation không cấp nonce/source mới hoặc kéo dài deadline; cơ chế chi tiết ở [transaction](participation-lifecycle.md#transaction).

`canPublishSources` là giới hạn loại nguồn, không bằng chứng mỗi người chỉ có một track mỗi loại. [Đường xử lý participant của LiveKit v1.13.7](https://github.com/livekit/livekit/blob/v1.13.7/pkg/rtc/participant.go) là điểm tham khảo cho publish; thiết kế SCDC còn cần kiểm soát reservation trước khi track được truyền. Webhook sau publish rồi xóa nguồn thừa có thể đã làm vượt hạn trước lúc xử lý, không đủ chứng minh DEC-079.

**Phương án thử nghiệm đề xuất:** bộ kiểm soát nằm trong đường join/resume/publish và chuyển tiếp của SFU, có thể cần extension/fork LiveKit. Đây không phải tính năng stock đã xác nhận. Chưa chọn hoặc triển khai fork; phải đánh giá công sức duy trì và tác động lịch trước khóa build. Bộ kiểm soát bắt buộc:

- Đối chiếu room generation, participation, connection epoch, grant nonce và binding khi mở kết nối; từ chối identity đã connected ở kết nối khác, không tự kick để thay thế.
- Trước publish, đòi source reservation + source epoch còn hiệu lực, bind track client ID → SID đúng loại và loại track audio/video; chặn nguồn unknown, SID giả, nhiều track cùng loại và đường RTP/SDP không có reservation. Một camera nhiều lớp simulcast vẫn là một nguồn, không mở thêm camera vì đổi codec/RID.
- Screen chỉ có tối đa hai reservation/live/draining mỗi room; pending reservation hết sau 10 giây nếu chưa publish. Hết reservation không tự release nguồn đã live; phải ngừng truyền/ack rồi mới tái cấp. Không tin label nguồn từ browser là bằng chứng quyền.

Ngừng/cancel/error phải stop local capture; slot live/draining chỉ nhả khi provider xác nhận dừng. Bộ gate/lease dùng chung ở [admission và thu hồi](participation-lifecycle.md#admission). Webhook hậu kiểm không chứng minh quota trước truyền.

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-03 | Người tham gia bật/tắt camera trong cuộc gọi riêng hoặc phòng thoại. | Người khác thấy/ngừng thấy hình theo trạng thái thực tế. |
| AC-MEDIA-04 | Người tham gia bắt đầu/dừng chia sẻ màn hình trong cuộc gọi riêng hoặc phòng thoại. | Người khác thấy nguồn chia sẻ khi đang bật và biết khi nguồn dừng. |
| AC-MEDIA-05 | Trình duyệt từ chối quyền micro/camera/màn hình hoặc nguồn chia sẻ kết thúc. | Giao diện báo trạng thái rõ; không hiển thị sai rằng nguồn vẫn đang hoạt động. |
| AC-MEDIA-08 | Hai thành viên cùng bắt đầu chia sẻ màn hình trong một phòng thoại. | Cả hai nguồn chia sẻ cùng hoạt động và người trong phòng có thể nhận biết, xem từng nguồn. |
| AC-MEDIA-12 | Hai nguồn màn hình đang phát; người thứ ba chia sẻ; một nguồn dừng rồi thử lại | Từ chối khi đầy; sau giải phóng nguồn có thể chia sẻ; mỗi người tối đa một nguồn |
| AC-MEDIA-17 | Join/accept mà không cấp quyền mic/camera | Vào được để nghe; mic/camera/share tắt, không tự xin quyền/mở capture theo DEC-101 |
| AC-MEDIA-18 | Browser trả audio track khi chia sẻ màn hình | Không truyền audio màn hình, track đó được dừng; mic độc lập theo DEC-102 |
| AC-MEDIA-21 | Client sửa yêu cầu publish để thêm camera/screen/nguồn unknown | Gate chặn trước truyền; không vượt nguồn/người hoặc 2 share/room, kể cả reservation/draining |
| AC-MEDIA-24 | Publish reservation hết hạn, UI hủy capture hoặc phản hồi API không rõ | Không rò quota/local track; lặp operation không cấp source/grant mới hoặc kéo dài deadline |

Mọi AC-MEDIA/TC-MEDIA chưa có kết quả chạy được ghi nhận. Dùng tài khoản thử, provider/môi trường và thiết bị có phiên bản cụ thể; kết quả phải gắn commit/provider build/môi trường theo [phương pháp đo](../../system/quality.md#quality-targets).

| Mã | Thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-MEDIA-03 | Bật/tắt mic/camera, từ chối quyền thiết bị, mất thiết bị | UI và media phản ánh trạng thái thực; lỗi có đường thử lại | AC-MEDIA-03/05 |
| TC-MEDIA-04 | Hai người share, người thứ ba thử; dừng share từ UI/trình duyệt | Đúng nguồn/trạng thái/giới hạn; nguồn dừng giải phóng slot | AC-MEDIA-04/08/12 |
| TC-MEDIA-12 | Không có mic/camera; chưa cấp quyền; join rồi bật từng nguồn | Ban đầu chỉ nghe, capture chỉ mở theo thao tác; UI khớp provider | AC-MEDIA-17 |
| TC-MEDIA-13 | Capture screen có audio, hai share rồi yêu cầu thứ ba | Dừng audio screen, giữ tiếng mic độc lập, không vượt 2 share | AC-MEDIA-18/21 |
| TC-MEDIA-16 | Client tự sửa SDP/AddTrack/source label, publish nhiều camera hoặc screen_audio | Chặn trước chuyển tiếp bằng gate, không dùng webhook hậu kiểm làm bằng chứng | AC-MEDIA-21 |

TC-MEDIA-19 kiểm tra fixture/deadline/HMAC/repeat operation ở [lifecycle](participation-lifecycle.md#acceptance).

<a id="status"></a>

## Hiện trạng và khoảng trống

Đối chiếu tài liệu ngày 2026-10-06: quota 1 nguồn/người và 2 share/phòng, thiết bị ban đầu tắt và screen không audio đã chốt. Source/permit/gate/layout là thiết kế; SDK, quota gate và capture matrix thực tế chưa có bằng chứng. OQ-006, MEDIA-GAP-03/05 cần proof trên browser/thiết bị desktop; nút mic/camera UI mẫu không chứng minh nguồn đang phát. Các MEDIA-GAP theo dõi ở [lifecycle](participation-lifecycle.md#status); mọi AC/TC còn chưa chạy.

# SCDC — Micro, camera và chia sẻ màn hình

Trong cuộc gọi riêng hoặc phòng thoại, người dùng có thể bật micro, camera và chia sẻ màn hình. Trang này mô tả quy tắc, giao diện và cách hệ thống kiểm soát số nguồn phát.

**Trạng thái:** đã thiết kế, chưa có code. Nút micro và camera trong giao diện mẫu không chứng minh có luồng thật.

Media sở hữu nguồn phát, giữ chỗ và trạng thái. Trạng thái "đang phát" phải được trình duyệt và LiveKit xác nhận, không chỉ dựa vào trạng thái nút.

<a id="rules"></a>

## Quy tắc

- Mỗi người có tối đa một micro, một camera và một màn hình chia sẻ.
- Mỗi phòng có tối đa **hai** màn hình chia sẻ cùng lúc.
- Khi vào, micro và camera tắt, chưa chia sẻ. Chưa cấp quyền thiết bị vẫn vào được để nghe.
- Chia sẻ màn hình chỉ có hình. Âm thanh của màn hình bị dừng; nói chuyện qua micro. Chia sẻ âm thanh tab hay ứng dụng để sau.
- Chỉ người sở hữu mới bật được nguồn của mình, bằng thao tác chủ động. Không ai bật micro người khác từ xa.

<a id="ux"></a>

## Giao diện

```text
MEDIA-S02 · Cuộc gọi / phòng thoại
┌────────────────────────────────────┐
│ Phòng / tên người · Đang kết nối   │
│ [Nguồn màn hình 1] [Nguồn 2]       │
│ [Camera / tên các thành viên]      │
│ Mic · Camera · Chia sẻ · [Rời]     │
└────────────────────────────────────┘
```

- Có nút micro, camera, chia sẻ màn hình và danh sách nguồn.
- Báo rõ khi không được cấp quyền.
- Nhiều người chia sẻ cùng lúc thì mỗi nguồn có tên người chia sẻ; người xem chọn được nguồn để xem.
- MEDIA-S05 báo thiết bị hoặc quyền không dùng được, hoặc đã đủ hai màn hình chia sẻ.
- Giao diện chỉ hiện "đang phát" khi trình duyệt và LiveKit xác nhận.

Wireframe là đề xuất, chưa có prototype hay review.

<a id="device-điều-khiển-thiết-bị-và-bố-cục"></a>

### Điều khiển thiết bị

**Bật một nguồn:** người dùng bấm → trình duyệt mở thiết bị và xem trước → xin giữ chỗ → SDK phát qua bộ kiểm soát hạn mức → LiveKit xác nhận đang phát → giao diện hiện "đang phát".

- Không xin quyền thiết bị khi người dùng chưa bấm.
- Không có thiết bị hoặc bị từ chối quyền: giữ trạng thái tắt, báo cách thử lại, không bắt rời phòng.
- Chia sẻ màn hình: gọi `getDisplayMedia` ngay trong thao tác bấm, trước mọi lời gọi mạng. Người dùng chọn màn hình, cửa sổ hoặc tab. Theo [bản nháp W3C Screen Capture](https://www.w3.org/TR/2026/WD-screen-capture-20260827/), trình duyệt hỏi quyền mỗi lần và không đảm bảo có âm thanh; cần thử trên từng trình duyệt.
- Mở thiết bị xong mới xin hạn mức. Nếu đầy, hủy hoặc hết hạn thì dừng mọi track vừa mở, không để bản xem trước giữ thiết bị.
- Khi trình duyệt báo nguồn kết thúc (`ended`), ngừng phát và gửi lệnh dừng. Hạn mức chỉ được trả khi LiveKit xác nhận đã ngừng.

**Tắt tiếng, tắt camera, đổi thiết bị:**

- Tắt tiếng hay tắt camera phải tác động lên track thật, không chỉ đổi biểu tượng.
- Đổi thiết bị: dừng track cũ trước khi mở track mới, không vượt hạn mức.
- Trạng thái mong muốn và trạng thái đã xác nhận lưu riêng. Mất phản hồi thì hiện "đang xử lý".
- Rời, đăng xuất hoặc bị thu hồi: dừng thiết bị ngay ở client; server tự cắt độc lập, kể cả khi tab không hợp tác.

**Kết nối lại:**

- Lựa chọn micro và camera giữ trong bộ nhớ, nhưng chỉ phát lại khi đúng lượt tham gia, lease và epoch nguồn. Nguồn đã kết thúc thì người dùng phải bấm cấp lại.
- Kết nối lại nhẹ có thể giữ màn hình chia sẻ còn sống. Kết nối lại hoàn toàn thì không tự mở hộp chọn màn hình.
- Không lưu token, nguồn, lượt tham gia hay lựa chọn thiết bị vào localStorage hoặc IndexedDB.

**Bố cục đề xuất:** tối đa hai màn hình chia sẻ có tên người chia sẻ, chọn một nguồn xem lớn, nguồn kia thu nhỏ; danh sách tối đa 10 người hoặc camera có nhãn thiết bị. Không ẩn thông báo kết thúc, đầy hay đang kết nối lại khi đổi bố cục.

<a id="contracts"></a>

## API

Schema mục tiêu: [media.openapi.json](../../contracts/media.openapi.json). Chưa endpoint nào được làm. Đường dẫn bắt đầu bằng `/api/v1/media/participations/{participationId}`. Cấp quyền, vòng đời và heartbeat xem [vòng đời tham gia](participation-lifecycle.md#contracts).

| Endpoint | Đầu vào → Trả về | Điều kiện |
|---|---|---|
| POST `.../sources` | loại nguồn, mã thao tác, mã tab, version → SourceSnapshot | Lượt tham gia hợp lệ, giữ được hạn mức. Không tự mở thiết bị |
| POST `.../sources/{sourceId}/stop` | mã thao tác, mã tab, `sourceVersion` → SourceSnapshot | Chỉ nguồn của chính mình |
| POST `.../sources/{sourceId}/mute` | `muted`, mã thao tác, mã tab, `sourceVersion` → SourceSnapshot | Chỉ micro và camera; màn hình dùng stop. Trả cả trạng thái mong muốn và đã xác nhận |

Bảng đề xuất `sources`: id, lượt tham gia, loại, epoch, trạng thái, `publishDeadline`, `providerTrackSid`. Mỗi lượt tham gia có tối đa một nguồn mỗi loại. Màn hình chia sẻ ở trạng thái giữ chỗ, đang phát hay đang thoát đều tính vào giới hạn 2 mỗi phòng.

### Kiểm soát hạn mức trước khi phát

Trước khi phát, phải có giữ chỗ và epoch nguồn còn hiệu lực. Giấy phép phát chỉ cấp cho đúng tab sở hữu, gắn với epoch kết nối, epoch nguồn và hạn `publishDeadline`. Gửi lặp cùng thao tác không cấp nguồn mới hay kéo dài hạn. Chi tiết xem [transaction](participation-lifecycle.md#transaction).

**Vì sao LiveKit gốc chưa đủ:** quyền `canPublishSources` chỉ giới hạn loại nguồn, không giới hạn số track mỗi loại. Xử lý qua webhook (phát xong mới xóa nguồn thừa) để lọt luồng trước khi chặn. Tham khảo [mã xử lý participant của LiveKit v1.13.7](https://github.com/livekit/livekit/blob/v1.13.7/pkg/rtc/participant.go).

**Phương án đề xuất (chưa chọn):** đặt bộ kiểm soát trong đường vào, kết nối lại, phát và chuyển tiếp của LiveKit. Có thể phải viết extension hoặc fork LiveKit. Phải đánh giá công sức duy trì và ảnh hưởng tới lịch trước khi chốt. Bộ kiểm soát phải:

- Khi mở kết nối: đối chiếu thế hệ phòng, lượt tham gia, epoch kết nối, nonce của giấy phép và tab sở hữu. Từ chối nếu danh tính đó đã kết nối ở nơi khác, không tự đuổi kết nối cũ.
- Trước khi phát: đòi giữ chỗ và epoch nguồn còn hiệu lực; gắn ID track của client với SID đúng loại (âm thanh hay hình). Chặn nguồn lạ, SID giả, nhiều track cùng loại và luồng RTP/SDP không có giữ chỗ. Một camera có nhiều lớp simulcast vẫn là một nguồn.
- Màn hình chia sẻ: tối đa hai (giữ chỗ, đang phát hoặc đang thoát) mỗi phòng. Giữ chỗ chưa phát hết hạn sau 10 giây. Hết hạn giữ chỗ không tự trả nguồn đang phát; phải ngừng truyền và có xác nhận rồi mới cấp lại.
- Không tin nhãn nguồn do trình duyệt gửi lên.

Dừng, hủy hay lỗi đều phải dừng thiết bị ở client. Chỗ đang phát hay đang thoát chỉ được trả khi LiveKit xác nhận dừng. Bộ kiểm soát dùng chung xem [vào phòng và thu hồi](participation-lifecycle.md#admission).

<a id="acceptance"></a>

## Tiêu chí chấp nhận và ca kiểm thử

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-MEDIA-03 | Bật, tắt camera trong cuộc gọi hoặc phòng thoại | Người khác thấy hoặc ngừng thấy hình theo trạng thái thật |
| AC-MEDIA-04 | Bắt đầu, dừng chia sẻ màn hình | Người khác thấy nguồn khi đang bật và biết khi nguồn dừng |
| AC-MEDIA-05 | Trình duyệt từ chối quyền, hoặc nguồn chia sẻ kết thúc | Giao diện báo rõ; không hiện sai là nguồn vẫn đang phát |
| AC-MEDIA-08 | Hai người cùng chia sẻ màn hình trong một phòng | Cả hai nguồn cùng hoạt động; người xem phân biệt và chọn được từng nguồn |
| AC-MEDIA-12 | Đã có hai màn hình chia sẻ; người thứ ba thử; một nguồn dừng rồi thử lại | Từ chối khi đầy; có chỗ thì chia sẻ được; mỗi người tối đa một nguồn |
| AC-MEDIA-17 | Vào phòng hoặc nhận cuộc gọi mà không cấp quyền micro, camera | Vào được để nghe; mọi nguồn tắt; không tự xin quyền |
| AC-MEDIA-18 | Trình duyệt trả kèm âm thanh khi chia sẻ màn hình | Không truyền âm thanh màn hình, track đó bị dừng; micro hoạt động độc lập |
| AC-MEDIA-21 | Client bị sửa để phát thêm camera, màn hình hoặc nguồn lạ | Bị chặn trước khi truyền; không vượt giới hạn mỗi người hoặc 2 màn hình mỗi phòng |
| AC-MEDIA-24 | Giữ chỗ hết hạn, giao diện hủy, hoặc phản hồi API không rõ | Không rò hạn mức hay track; gửi lặp không cấp nguồn mới hay kéo dài hạn |

| Mã | Thao tác | Cần quan sát | Cho |
|---|---|---|---|
| TC-MEDIA-03 | Bật, tắt micro và camera; từ chối quyền; rút thiết bị | Giao diện và luồng media khớp trạng thái thật; lỗi có cách thử lại | AC-MEDIA-03, 05 |
| TC-MEDIA-04 | Hai người chia sẻ, người thứ ba thử; dừng từ giao diện và từ trình duyệt | Đúng nguồn, trạng thái, giới hạn; nguồn dừng thì trả chỗ | AC-MEDIA-04, 08, 12 |
| TC-MEDIA-12 | Không có micro, camera; chưa cấp quyền; vào rồi bật từng nguồn | Ban đầu chỉ nghe; chỉ mở thiết bị khi bấm; giao diện khớp LiveKit | AC-MEDIA-17 |
| TC-MEDIA-13 | Chia sẻ màn hình có âm thanh; hai người chia sẻ rồi người thứ ba thử | Dừng âm thanh màn hình, micro vẫn chạy; không vượt 2 | AC-MEDIA-18, 21 |
| TC-MEDIA-16 | Client tự sửa SDP, AddTrack, nhãn nguồn; phát nhiều camera hoặc âm thanh màn hình | Bị chặn trước khi chuyển tiếp; webhook xử lý sau không được tính là bằng chứng | AC-MEDIA-21 |

TC-MEDIA-19 (fixture, hạn phát, HMAC, gửi lặp) xem [vòng đời tham gia](participation-lifecycle.md#acceptance).

Chưa ca nào được chạy. Khi chạy, ghi rõ commit, phiên bản LiveKit, môi trường và thiết bị theo [phương pháp đo](../../system/quality.md#quality-targets).

<a id="status"></a>

## Còn mở

- Chọn cách kiểm soát hạn mức trong LiveKit (extension hay fork) và đánh giá công sức.
- SDK, bộ kiểm soát hạn mức và khả năng chia sẻ màn hình của từng trình duyệt chưa có bằng chứng.
- OQ-006, MEDIA-GAP-03, 05: cần bằng chứng trên trình duyệt và thiết bị máy tính. Theo dõi tại [vòng đời tham gia](participation-lifecycle.md#status).

---

Nguồn: DEC-049, DEC-079 (giới hạn nguồn), DEC-101 (thiết bị ban đầu tắt), DEC-102 (màn hình không kèm âm thanh). Đối chiếu tài liệu ngày 2026-10-06.

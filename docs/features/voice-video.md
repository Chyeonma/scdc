# SCDC — Thoại, video và chia sẻ màn hình

Cập nhật: 2026-10-04. Phạm vi: REQ-007/008/009, SCP-006/007, DEC-046–050 và AC-MEDIA.

Media thuộc MVP. Điều kiện gọi, giới hạn 10/20/2 và reconnect 30 giây đã chốt DEC-078–080. Tài liệu có luồng trạng thái, UX, hợp đồng và ca kiểm thử đề xuất; LiveKit tự host, media desktop và ngưỡng chất lượng đã chốt DEC-082/084/085; provider/backend/API chưa triển khai, thiết kế admission/thu hồi cần thử nghiệm.

## Mục lục

- [Phạm vi và yêu cầu](#requirements)
- [Giao diện và trạng thái](#ux)
- [Dữ liệu và tích hợp](#contracts)
- [Tiêu chí và kiểm thử](#acceptance)
- [Quyết định còn thiếu](#gaps)

<a id="requirements"></a>

## 1. Phạm vi, hành trình và yêu cầu

### Phạm vi đã thống nhất

Phiên bản đầu có phòng thoại nhiều thành viên trong server và cuộc gọi
riêng giữa hai người. Cả hai ngữ cảnh có video và chia sẻ màn hình.
Cuộc gọi trong nhóm chat riêng ngoài server không thuộc phiên bản đầu.
Sản phẩm chạy trên trình duyệt web; ma trận trình duyệt desktop đã chốt DEC-082; phiên bản/OS/thiết bị thực tế phải ghi ở lần nghiệm thu. Ngày 2026-10-04, người dùng chốt 20 người tham gia gọi đồng thời toàn hệ thống là giới hạn MVP (DEC-079). Đây là thay đổi từ giả định dự toán AS-006 sang quy tắc cần kiểm chứng, chưa phải kết quả đo.

### Hành trình cần đặc tả

| Hành trình | Luồng chính cần quyết định | Ngoại lệ bắt buộc xem xét |
|---|---|---|
| Gọi riêng | Một người gọi, người nhận phải chấp nhận trước khi bắt đầu; hai bên bật/tắt micro/camera/chia sẻ và kết thúc. | Người nhận vắng mặt, bận cuộc gọi, từ chối, không cấp quyền thiết bị, mất mạng. |
| Phòng thoại | Thành viên thấy phòng được phép xem thì vào ngay, vào/rời, biết ai đang trong phòng, điều khiển thiết bị. | Mất quyền/phòng bị xóa, phòng đầy, lỗi kết nối, nhiều người vào/ra đồng thời. |
| Video và chia sẻ màn hình | Bật/tắt nguồn video hoặc màn hình trong cuộc gọi/phòng, người khác xem và nhận biết nguồn đang chia sẻ. | Không có thiết bị/quyền truy cập, ngừng chia sẻ từ trình duyệt, đổi nguồn, mạng yếu. |

Quy tắc đã được đại diện sản phẩm xác định: nhận cuộc gọi riêng cần thao
tác chấp nhận (DEC-046); cuộc gọi nhỡ không lưu vào hội thoại ở đợt đầu
(DEC-047); quyền xem phòng thoại cho phép vào ngay (DEC-048); nhiều người
có thể chia sẻ màn hình đồng thời trong phòng thoại (DEC-049); ứng dụng
tự kết nối lại khi mạng trở lại (DEC-050). Giới hạn và thời gian chờ đã chốt DEC-078–080; chất lượng đã chốt DEC-085, chưa đo.

<a id="ux"></a>

## 2. Giao diện và trạng thái

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Gọi riêng | Thao tác gọi từ hội thoại và màn hình cuộc gọi đến. | Người nhận bấm chấp nhận trước khi bắt đầu; người gọi biết trạng thái chờ/đã nhận/không nhận. Cuộc gọi nhỡ chưa lưu vào hội thoại ở đợt đầu. |
| Vào phòng thoại | Danh sách phòng và màn hình cuộc gọi nhóm. | Thành viên có quyền xem phòng vào ngay; người không có quyền không thấy/không vào được. |
| Thiết bị và chia sẻ | Điều khiển micro, camera, chia sẻ màn hình và danh sách nguồn. | Phản hồi khi không được cấp quyền; nhiều người có thể chia sẻ đồng thời trong phòng, mỗi nguồn phân biệt được. |
| Mất mạng | Trạng thái cuộc gọi đang gián đoạn. | Báo đang kết nối lại; tự thử khi mạng trở lại; báo rõ nếu không khôi phục được. |

Phòng tối đa 10, toàn hệ thống tối đa 20 người (gồm gọi riêng), 2 màn hình/phòng; mỗi người một camera/màn hình. Đổ chuông/reconnect tối đa 30 giây. Bố cục nhiều nguồn và ngưỡng theo DEC-085; kết quả và thiết bị cụ thể còn OQ-007.

Wireframe media dưới đây là thiết kế đề xuất; chưa có prototype/kết quả rà soát. Cần thiết kế các trạng thái chờ nhận, từ chối/bận, đang kết nối, trong cuộc gọi, mất mạng/reconnect, không cấp quyền thiết bị, nguồn chia sẻ dừng, phòng đầy và mất quyền. Trạng thái thiết bị phải phản ánh thực tế, không chỉ trạng thái nút bấm.

DEC-082 chốt media trên desktop Chrome/Edge/Firefox/Safari, 2 phiên bản ổn định gần nhất tại nghiệm thu; media điện thoại ở đợt sau. Không dùng phiên bản phát triển của trình duyệt thay phiên bản ổn định; ghi OS, thiết bị và phiên bản thực tế của từng ca.

### Màn hình đề xuất

```text
MEDIA-S01 · Gọi đến               MEDIA-S02 · Cuộc gọi / phòng thoại
┌────────────────────────────┐   ┌────────────────────────────────────┐
│ Tên hiển thị · @username   │   │ Phòng / tên người · Đang kết nối  │
│ Đang gọi đến               │   │ [Nguồn màn hình 1] [Nguồn 2]       │
│ [Từ chối]       [Nhận]     │   │ [Camera / tên các thành viên]     │
└────────────────────────────┘   │ Mic · Camera · Chia sẻ · [Rời]    │
                                 └────────────────────────────────────┘
```

MEDIA-S03 thể hiện mất mạng/đang reconnect và thời gian còn lại trong 30 giây; hết hạn có nút gọi/vào lại. MEDIA-S04 báo người nhận bận/vắng mặt, phòng đầy hoặc toàn hệ thống hết chỗ. MEDIA-S05 báo thiết bị/quyền chia sẻ không dùng được hoặc hai nguồn màn hình đã đầy. Không hiển thị mic/camera/share là đang phát trước khi thiết bị/provider thực sự xác nhận. Hai nguồn màn hình có nhãn tác giả và thao tác chọn xem; không tự dừng nguồn người khác.

<a id="contracts"></a>

## 3. Dữ liệu, API và tích hợp cần thiết kế

MVP vẫn chạy Modular Monolith theo DEC-060; một nhà cung cấp media bên ngoài có thể được tích hợp nếu thử nghiệm phù hợp. LiveKit tự host đã được chọn tại DEC-084; chưa có service/manifest hoặc kết quả tích hợp trong repo.

| Hợp đồng cần có | Hành vi cần xác định |
|---|---|
| Gọi riêng | Tạo lời gọi, thông báo đến, nhận/từ chối/hủy/kết thúc, điều kiện ai gọi ai, timeout/bận |
| Phòng thoại | Quyền vào/rời, danh sách người, giới hạn, mất quyền và vòng đời phòng |
| Thiết bị/chia sẻ | Micro/camera, chọn nguồn, nhiều nguồn cùng lúc, nguồn bị thu hồi hoặc dừng |
| Cấp quyền media | Kiểm tra phiên và quyền trước khi cấp thông tin kết nối; ngăn truy cập bằng token/phiên đã thu hồi |
| Khôi phục | Retry/reconnect, thời gian chờ, đồng bộ danh sách và trạng thái thiết bị |
| Quan sát/chi phí | Số người/luồng, băng thông, chất lượng, lỗi kết nối và chi phí theo mức dùng |

Bảng contract/state là thiết kế đề xuất, chưa có schema máy đọc được hoặc endpoint media triển khai. `calls` từng xuất hiện trong docs kỹ thuật cũ nhưng không phải schema hiện tại trong SQL. Trạng thái gọi nhỡ không lưu vào hội thoại ở đợt đầu theo DEC-047; không tự đưa log lịch sử cuộc gọi vào scope.

API tương lai áp dụng [ProblemDetails chung](../architecture.md#contracts). Media phải kiểm tra phiên, quyền Community khi dùng phòng và điều kiện DM khi gọi riêng; thời hạn thu hồi trên kết nối phải được thiết kế và thử nghiệm.

### Luồng trạng thái và giữ chỗ

Đây là thiết kế đề xuất cho các quy tắc đã chốt; bảng không xác nhận backend/provider đã có.

| Luồng/trạng thái | Trigger | Kết quả |
|---|---|---|
| Gọi riêng / requested | Kiểm tra hai tài khoản, phiên và điều kiện DM | Ringing nếu người nhận khả dụng; busy/unavailable nếu không; không cấp quyền nghe media trước accept |
| Ringing | Người nhận accept trong 30 giây | Kiểm tra lại phiên/trạng thái/busy, cấp đủ hai slot trong một giao dịch rồi connecting |
| Ringing | Reject, caller cancel hoặc hết 30 giây | Rejected/cancelled/no-answer; không tạo lịch sử cuộc gọi nhỡ trong DM |
| Phòng thoại / chưa tham gia | Join với quyền xem | Kiểm tra member/phòng còn tồn tại, giới hạn 10/20 và một phiên tham gia/người; allocate slot rồi connecting |
| Connecting | Provider xác nhận kết nối | Connected; điều khiển mic/camera/share theo trạng thái thực |
| Connected | Mất mạng | Reconnecting; giữ slot tối đa 30 giây, vẫn tính tải 10/20 |
| Reconnecting | Mạng trở lại trong hạn, phiên/quyền còn hợp lệ | Tái kết nối bằng đúng participation ID, không tạo thêm slot; đồng bộ thiết bị/danh sách |
| Reconnecting | Hết 30 giây hoặc mất phiên/quyền | Ended; giải phóng slot, không tự khôi phục participation cũ; có thể join/call mới |
| Connected/connecting/reconnecting | Rời/kết thúc, phòng deleted, session/quyền bị thu hồi | Kết thúc participation, thu hồi quyền provider và thông báo trạng thái; ngưỡng thu hồi còn OQ-007 |

Đề xuất coordinator kiểm tra atomically một participation mỗi user và hạn mức toàn hệ thống, không dùng riêng số kết nối trên một tab. Ringing chưa có luồng media không tính người đang gọi, nhưng cần khóa ngữ cảnh gọi để accept/cancel trên nhiều thiết bị chỉ có một kết quả. Slot giữ chỗ reconnect vẫn tính vào hạn mức. Khách gửi heartbeat/rejoin không kéo dài thời hạn 30 giây của cùng lần gián đoạn. Khi đạt 2 nguồn màn hình, yêu cầu chia sẻ thứ ba bị từ chối rõ; dừng một nguồn mới cho cấp slot khác. Gọi riêng có hai người, mỗi người một màn hình nên tối đa hai nguồn; không thêm người thứ ba.

### Hợp đồng điều khiển đề xuất

Prefix `/api/v1`; schema HTTP/sự kiện dưới đây chưa được máy đọc hóa/khóa. Actor lấy từ phiên, API trả ProblemDetails chung; ID cuộc gọi, participation và lease ổn định. Media provider không được tự quyết định bỏ qua Identity/Community.

| Thao tác | Đầu vào/đầu ra tối thiểu | Quyền và đồng thời |
|---|---|---|
| Tạo lời gọi riêng | peerUserId, clientOperationId → callId, ringing/busy/unavailable, expiresAt | Điều kiện DM; không phát token media trước accept |
| Accept/reject/cancel | callId, expectedVersion → trạng thái hiện hành | Đúng người nhận/người gọi theo thao tác; chỉ một transition thắng |
| Join phòng thoại | channelId, clientOperationId → participationId, trạng thái, connection grant | Phiên/member/view, room chưa deleted, giới hạn 10/20, một participation/user |
| Rời/kết thúc | participationId/callId → ended | Chính người tham gia rời, một bên kết thúc gọi riêng kết thúc cả hai; lặp an toàn |
| Bật camera/share | participationId, loại nguồn → sourceId/trạng thái hoặc lỗi đầy | Đúng participation còn hợp lệ, một camera/màn hình/người, tối đa 2 share/phòng |
| Reconnect | participationId → grant mới/trạng thái hết hạn | Trong 30 giây; kiểm tra lại phiên/quyền; không nhân đôi slot |
| State snapshot | callId/channelId → trạng thái và participants/source list theo quyền | Khôi phục sau mất sự kiện; không dùng client cached list làm nguồn quyền |

Grant có thời hạn gắn với phiên/participation; source provider phải ngừng khi participation ended. Lifecycle grant, thu hồi active publisher/subscriber, callback provider và idempotency của thao tác cần thử nghiệm; việc xóa token phía UI không tự chứng minh thu hồi media. Log chỉ ID/trạng thái/lỗi/chỉ số, không token hoặc nội dung âm thanh/hình ảnh.

### LiveKit tự host: admission và thu hồi

Lựa chọn provider đã chốt, giải pháp dưới đây là thiết kế cần thử nghiệm. Theo [tài liệu token/grant LiveKit](https://docs.livekit.io/frontends/reference/tokens-grants/), self-host không vô hiệu token cũ chỉ bằng remove participant. Không dùng TTL ngắn hoặc xóa token phía client làm bằng chứng chặn rejoin.

Đề xuất tất cả signaling WebSocket/HTTP join đi qua admission proxy. Proxy xác thực JWT LiveKit và gọi endpoint nội bộ của monolith để kiểm tra participation ID, session/security stamp, room/call, quyền và hạn reconnect; reject khi đã ended/thu hồi. Participant identity của SFU ánh xạ tới participation gắn với session, không chỉ user ID. Token được SFU refresh vẫn phải chịu cùng kiểm tra participation khi join lại. Endpoint signaling trực tiếp của SFU không được công khai để bypass proxy; port WebRTC/TURN chỉ phục vụ media sau signaling hợp lệ.

Thu hồi dùng sự kiện sau commit để loại subscription và gọi RoomService remove participant, đồng thời admission từ chối mọi token của participation đã ended. Phải thử token cũ/token SFU refresh, URL SFU trực tiếp, reconnect trong/ngoài 30 giây, proxy/authorizer lỗi và nhiều thiết bị. Mục tiêu thu hồi media ≤5 giây là đề xuất đồng bộ chat, cần xác nhận/kiểm chứng riêng nếu dùng làm ngưỡng nghiệm thu media; DEC-083 đã chốt ngưỡng chat.

TLS/domain, reverse proxy, TURN/UDP, region, cấu hình server và secrets cần được thiết kế theo [hướng dẫn self-host LiveKit](https://docs.livekit.io/transport/self-hosting/). Không tự coi Docker Development là topology production. Nếu admission hoặc active revocation không chứng minh được thì phần media chưa đủ điều kiện phát hành; giữ DEC-084 và báo rủi ro, không tự đổi sang Cloud.

<a id="acceptance"></a>

## 4. Tiêu chí chấp nhận và kiểm thử

Các tiêu chí chức năng và ngưỡng DEC-079/082/085 là mục tiêu đã chốt. Để kết luận đạt cần môi trường/thiết bị cụ thể và bằng chứng theo [phương pháp đo](../release-operations.md#quality-targets).

| Mã | Tình huống | Kết quả mong đợi ở mức khung |
|---|---|---|
| AC-MEDIA-01 | Hai người đã đăng nhập thực hiện cuộc gọi riêng. | Người nhận có thể nhận hoặc từ chối; khi nhận, hai bên nghe được nhau. |
| AC-MEDIA-02 | Thành viên có quyền xem phòng thoại trong cộng đồng chọn vào phòng. | Vào ngay và rời được; thành viên không có quyền xem bị từ chối. |
| AC-MEDIA-03 | Người tham gia bật/tắt camera trong cuộc gọi riêng hoặc phòng thoại. | Người khác thấy/ngừng thấy hình theo trạng thái thực tế. |
| AC-MEDIA-04 | Người tham gia bắt đầu/dừng chia sẻ màn hình trong cuộc gọi riêng hoặc phòng thoại. | Người khác thấy nguồn chia sẻ khi đang bật và biết khi nguồn dừng. |
| AC-MEDIA-05 | Trình duyệt từ chối quyền micro/camera/màn hình hoặc nguồn chia sẻ kết thúc. | Giao diện báo trạng thái rõ; không hiển thị sai rằng nguồn vẫn đang hoạt động. |
| AC-MEDIA-06 | A gọi riêng cho B; B chưa bấm nhận, sau đó bấm chấp nhận. | Trước khi B chấp nhận, cuộc gọi chưa bắt đầu; sau khi B chấp nhận, hai bên vào cuộc gọi. |
| AC-MEDIA-07 | A gọi riêng cho B nhưng B không nhận trong đợt đầu. | Cuộc gọi không bắt đầu; không tạo mục cuộc gọi nhỡ trong lịch sử hội thoại. |
| AC-MEDIA-08 | Hai thành viên cùng bắt đầu chia sẻ màn hình trong một phòng thoại. | Cả hai nguồn chia sẻ cùng hoạt động và người trong phòng có thể nhận biết, xem từng nguồn. |
| AC-MEDIA-09 | Người tham gia đang gọi riêng hoặc ở phòng thoại bị mất mạng, sau đó mạng trở lại. | Ứng dụng tự thử kết nối lại; khi khôi phục thành công, giao diện và âm thanh/hình ảnh trở về đúng trạng thái. |
| AC-MEDIA-10 | Hai thiết bị của một người đồng thời join hai phòng/call | Chỉ một participation được cấp; UI còn lại nhận busy/conflict |
| AC-MEDIA-11 | Phòng đã có 10 người, request thứ 11; toàn hệ thống đã có 20 người, request mới | Từ chối vượt giới hạn, gồm slot reconnect đang giữ; không cấp token/slot dở dang |
| AC-MEDIA-12 | Hai nguồn màn hình đang phát; người thứ ba chia sẻ; một nguồn dừng rồi thử lại | Từ chối khi đầy; sau giải phóng nguồn có thể chia sẻ; mỗi người tối đa một nguồn |
| AC-MEDIA-13 | Mất mạng, reconnect ở giây 29 và sau giây 30 | Trong hạn giữ đúng participation/slot; quá hạn ended, giải phóng chỗ, chỉ join/call mới |
| AC-MEDIA-14 | Accept/reject/cancel/timeout xảy ra đồng thời hoặc trên nhiều thiết bị | Chỉ một trạng thái cuối; không có cuộc gọi bắt đầu sau cancel/timeout; không tạo cuộc gọi nhỡ trong lịch sử DM |

Mọi ca AC-MEDIA chưa có kết quả chạy được ghi nhận. Chuẩn bị hai người gọi riêng, phòng nhiều thành viên, người không có quyền, hai nguồn chia sẻ, trình duyệt/thiết bị đã chọn và khả năng ngắt mạng/từ chối quyền thiết bị. Với mỗi AC, ghi thao tác, kết quả thực tế và chỉ số theo ngưỡng đã chốt.

Thử phòng đầy, mất quyền khi đang gọi, người nhận bận, nguồn chia sẻ tự dừng và lỗi nhà cung cấp sau khi quy tắc được xác nhận. Các tình huống này là đầu vào kiểm thử, chưa tự đặt kết quả nghiệp vụ chưa có quyết định.

### Ca kiểm thử media

Mọi TC-MEDIA ở trạng thái Chưa chạy. Cần provider/môi trường, ma trận thiết bị và ngưỡng chất lượng DEC-085; dữ liệu dùng tài khoản thử.

| Mã | Thao tác | Kết quả cần quan sát | Dẫn chiếu |
|---|---|---|---|
| TC-MEDIA-01 | Gọi người khả dụng/vắng mặt/bận; accept/reject/cancel/timeout 30 giây | Đúng trạng thái, chưa accept chưa nghe; không lưu missed call | AC-MEDIA-01/06/07/14 |
| TC-MEDIA-02 | Join/rời phòng với member có/không quyền xem | Chỉ đúng quyền tham gia; rời giải phóng slot | AC-MEDIA-02 |
| TC-MEDIA-03 | Bật/tắt mic/camera, từ chối quyền thiết bị, mất thiết bị | UI và media phản ánh trạng thái thực; lỗi có đường thử lại | AC-MEDIA-03/05 |
| TC-MEDIA-04 | Hai người share, người thứ ba thử; dừng share từ UI/trình duyệt | Đúng nguồn/trạng thái/giới hạn; nguồn dừng giải phóng slot | AC-MEDIA-04/08/12 |
| TC-MEDIA-05 | Một user trên hai thiết bị join/call đồng thời | Một participation/user, không gấp đôi tải | AC-MEDIA-10 |
| TC-MEDIA-06 | Biên 10/phòng, 20/toàn hệ thống, slot reconnect | Không vượt giới hạn khi request đồng thời/rollback | AC-MEDIA-11 |
| TC-MEDIA-07 | Ngắt mạng, trở lại ở 29 giây và sau 30 giây | Tự reconnect đúng hạn/ID, hết hạn giải phóng slot | AC-MEDIA-09/13 |
| TC-MEDIA-08 | Thu hồi phiên/quyền, xóa phòng khi đang phát/xem | Chặn API và provider theo ngưỡng thu hồi cần chốt; không chỉ ẩn UI | DEC-077; OQ-007 |
| TC-MEDIA-09 | Provider lỗi, callback lặp/đến trễ, accept/cancel đồng thời | Không hồi sinh participation/call đã kết thúc, không rò slot | AC-MEDIA-14; cần thiết kế provider |
| TC-MEDIA-10 | Chạy tải 20 người, 2 nguồn share/phòng trên cấu hình mạng được chốt | Đo mục tiêu DEC-085, tỷ lệ kết nối, CPU/băng thông/chi phí; ghi cấu hình/build và kết quả thực tế | OQ-007/010 |

<a id="gaps"></a>

## 5. Quyết định còn thiếu

| Nhóm | Cần quyết định | Liên quan |
|---|---|---|
| Bắt đầu cuộc gọi | Đã chốt người nhận phải chấp nhận trước khi bắt đầu, chưa lưu cuộc gọi nhỡ vào lịch sử hội thoại; đã cụ thể hóa điều kiện/busy/unavailable/30 giây tại DEC-078; còn schema/multi-device. | OQ-006 |
| Phòng thoại | Đã chốt quyền xem là đủ để vào; quản lý phòng theo DEC-077, giới hạn 10/20 và đầy theo DEC-079; còn dữ liệu/coordinator và thu hồi. | OQ-004, OQ-006 |
| Video/chia sẻ | Đã chốt nhiều người chia sẻ cùng lúc trong phòng; giới hạn 2 share/phòng, một camera/share/người tại DEC-079; còn layout và tích hợp provider; gọi riêng hai người, mỗi người một nguồn share như state design. | OQ-006 |
| Mất kết nối | Đã chốt tự kết nối lại; 30 giây/giữ chỗ/ended đã chốt DEC-080; còn lịch retry/heartbeat/coordinator để kiểm chứng. | OQ-006 |
| Chất lượng | Đã chốt DEC-085; còn môi trường, thiết bị/version, hiệu chỉnh đồng hồ và kết quả đo. | OQ-007 |
| Kỹ thuật/chi phí | LiveKit tự host DEC-084; còn admission/thu hồi, hạ tầng/TURN, mức dùng và chi phí. | OQ-008, OQ-010 |

Vg và Sáng cần thử nghiệm giải pháp media theo kịch bản đã chốt trước
khi xác nhận lịch, chi phí và điều kiện nghiệm thu. Thái chuẩn bị giao
diện/trạng thái và kịch bản kiểm thử cùng nhóm.

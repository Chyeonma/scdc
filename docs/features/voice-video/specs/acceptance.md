# SCDC — Tiêu chí và kiểm thử media

AC-MEDIA/TC-MEDIA xác định điều kiện phải thử; trạng thái triển khai và các đầu vào còn thiếu tại [status](../status.md).

<a id="acceptance"></a>

## Tiêu chí chấp nhận và kiểm thử

Các tiêu chí chức năng và ngưỡng DEC-079/082/085 là mục tiêu đã chốt. Để kết luận đạt cần môi trường/thiết bị cụ thể và bằng chứng theo [phương pháp đo](../../../release-operations.md#quality-targets).

| Mã | Tình huống | Kết quả mong đợi ở mức khung |
|---|---|---|
| AC-MEDIA-01 | Hai người đã đăng nhập thực hiện cuộc gọi riêng rồi bật micro. | Người nhận có thể nhận hoặc từ chối; sau nhận và bật micro, hai bên nghe được nhau; ban đầu thiết bị tắt theo DEC-101. |
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
| AC-MEDIA-15 | Thu hồi phiên/quyền hoặc xóa phòng khi đang gửi/nhận RTP | Cả phát/nhận ngừng và token cũ/SFU refresh bị chặn vào lại ≤5 giây từ commit theo DEC-099 |
| AC-MEDIA-16 | Người nhận online ở hai thiết bị, cả hai bấm nhận | Lần accept đầu thắng; thiết bị còn lại ngừng đổ chuông/báo bận, không chiếm hoặc chuyển phiên theo DEC-100 |
| AC-MEDIA-17 | Join/accept mà không cấp quyền mic/camera | Vào được để nghe; mic/camera/share tắt, không tự xin quyền/mở capture theo DEC-101 |
| AC-MEDIA-18 | Browser trả audio track khi chia sẻ màn hình | Không truyền audio màn hình, track đó được dừng; mic độc lập theo DEC-102 |
| AC-MEDIA-19 | Authority/worker/network điều khiển lỗi trong khi WebRTC vẫn chạy | Lease hết thì node ngừng media liên quan trong budget DEC-099; không dựa riêng websocket/remove ack |
| AC-MEDIA-20 | Client dùng URL SFU trực tiếp, token cũ, đường resume hoặc clone kết nối đang hoạt động | Không bypass admission/current guard; không thay kết nối đang hoạt động hoặc cấp thêm chỗ |
| AC-MEDIA-21 | Client sửa yêu cầu publish để thêm camera/screen/nguồn unknown | Gate chặn trước truyền; không vượt nguồn/người hoặc 2 share/room, kể cả reservation/draining |
| AC-MEDIA-22 | Webhook/observation trùng, đến trễ hoặc dùng epoch cũ | Bỏ sự kiện lỗi thời, không hồi sinh ended hoặc sửa counter/connection mới |
| AC-MEDIA-23 | Restart/restore DB trong khi node SFU cũ còn sống | Admission dừng tới khi fence và chứng minh node/generation cũ không truyền; token cũ không phục hồi quyền |
| AC-MEDIA-24 | Publish reservation hết hạn, UI hủy capture hoặc phản hồi API không rõ | Không rò quota/local track; lặp operation không cấp source/grant mới hoặc kéo dài deadline |

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
| TC-MEDIA-08 | Thu hồi phiên/quyền, xóa phòng khi đang phát/xem | Chặn API và cả phát/nhận provider ≤5 giây từ commit; không chỉ ẩn UI | AC-MEDIA-15, DEC-077/099 |
| TC-MEDIA-09 | Provider lỗi, callback lặp/đến trễ, accept/cancel đồng thời | Không hồi sinh participation/call đã kết thúc, không rò slot | AC-MEDIA-14; cần thiết kế provider |
| TC-MEDIA-10 | Chạy tải 20 người, 2 nguồn share/phòng trên cấu hình mạng được chốt | Đo mục tiêu DEC-085, tỷ lệ kết nối, CPU/băng thông/chi phí; ghi cấu hình/build và kết quả thực tế | OQ-007/010 |
| TC-MEDIA-11 | Accept hai thiết bị cùng version, rồi tab thứ ba join | Một bên thắng, nơi khác ngừng ring/busy; không tự handoff | AC-MEDIA-16 |
| TC-MEDIA-12 | Không có mic/camera; chưa cấp quyền; join rồi bật từng nguồn | Ban đầu chỉ nghe, capture chỉ mở theo thao tác; UI khớp provider | AC-MEDIA-17 |
| TC-MEDIA-13 | Capture screen có audio, hai share rồi yêu cầu thứ ba | Dừng audio screen, giữ tiếng mic độc lập, không vượt 2 share | AC-MEDIA-18/21 |
| TC-MEDIA-14 | Giữ đường RTP nhưng chặn authorizer, RoomService/outbox; đo clock skew/lease | Cả gửi/nhận ngừng ≤5 giây, không renew bằng cache/response trễ | AC-MEDIA-19 |
| TC-MEDIA-15 | Token refresh/cũ, direct IP/routes, resume ngoài hạn, clone identity | Từ chối mọi bypass; không thay active connection | AC-MEDIA-20 |
| TC-MEDIA-16 | Client tự sửa SDP/AddTrack/source label, publish nhiều camera hoặc screen_audio | Chặn trước chuyển tiếp bằng gate, không dùng webhook hậu kiểm làm bằng chứng | AC-MEDIA-21 |
| TC-MEDIA-17 | Lặp/out-of-order webhook/observation; replay SID/epoch cũ | Counter và trạng thái mới giữ đúng, signature sai bị từ chối | AC-MEDIA-22 |
| TC-MEDIA-18 | Restart/restore DB/SFU, remove chưa ack, thử join mới | Chỗ draining không tái cấp, fence node cũ trước mở admission | AC-MEDIA-23 |
| TC-MEDIA-19 | Fixture deadline/quota/HMAC, source expiry và repeat operation | Đúng bytes/kết quả, transaction không cấp thêm secret/chỗ/nguồn; không kéo dài deadline | AC-MEDIA-24, MEDIA-GAP-01/02 |

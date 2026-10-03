# SCDC — Đặc tả khung thoại, video và chia sẻ màn hình

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-MEDIA-001 |
| Phiên bản | 0.4 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Khung đặc tả — chưa đủ quy tắc hoặc ngưỡng chất lượng để triển khai |
| Căn cứ | REQ-007, REQ-008, REQ-009, SCP-006, SCP-007 và [Project Brief](../01-initiation/01-project-brief.md) |

## 1. Phạm vi đã thống nhất

Phiên bản đầu có phòng thoại nhiều thành viên trong server và cuộc gọi
riêng giữa hai người. Cả hai ngữ cảnh có video và chia sẻ màn hình.
Cuộc gọi trong nhóm chat riêng ngoài server không thuộc phiên bản đầu.
Sản phẩm chạy trên trình duyệt web; ma trận trình duyệt và thiết bị chưa
được chốt. Giả định 20 người tham gia gọi đồng thời toàn hệ thống ở
[SCDC-EST-001](../01-initiation/02-budget-and-assumptions.md) chỉ dùng
để dự trù, chưa phải giới hạn sản phẩm hoặc mức nghiệm thu.

## 2. Hành trình cần đặc tả

| Hành trình | Luồng chính cần quyết định | Ngoại lệ bắt buộc xem xét |
|---|---|---|
| Gọi riêng | Một người gọi, người nhận phải chấp nhận trước khi bắt đầu; hai bên bật/tắt micro/camera/chia sẻ và kết thúc. | Người nhận vắng mặt, bận cuộc gọi, từ chối, không cấp quyền thiết bị, mất mạng. |
| Phòng thoại | Thành viên thấy phòng được phép xem thì vào ngay, vào/rời, biết ai đang trong phòng, điều khiển thiết bị. | Mất quyền/phòng bị xóa, phòng đầy, lỗi kết nối, nhiều người vào/ra đồng thời. |
| Video và chia sẻ màn hình | Bật/tắt nguồn video hoặc màn hình trong cuộc gọi/phòng, người khác xem và nhận biết nguồn đang chia sẻ. | Không có thiết bị/quyền truy cập, ngừng chia sẻ từ trình duyệt, đổi nguồn, mạng yếu. |

Quy tắc đã được đại diện sản phẩm xác định: nhận cuộc gọi riêng cần thao
tác chấp nhận (DEC-046); cuộc gọi nhỡ không lưu vào hội thoại ở đợt đầu
(DEC-047); quyền xem phòng thoại cho phép vào ngay (DEC-048); nhiều người
có thể chia sẻ màn hình đồng thời trong phòng thoại (DEC-049); ứng dụng
tự kết nối lại khi mạng trở lại (DEC-050). Số người/luồng tối đa, thời
gian chờ và chất lượng tối thiểu chưa được chốt.

## 3. Tiêu chí chấp nhận khung

Các tiêu chí này mô tả mục tiêu chức năng; chưa đủ để nghiệm thu chất
lượng cho đến khi chốt quy mô, trình duyệt và ngưỡng đo.

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

## 4. Quyết định còn thiếu trước thiết kế chi tiết

| Nhóm | Cần quyết định | Liên quan |
|---|---|---|
| Bắt đầu cuộc gọi | Đã chốt người nhận phải chấp nhận trước khi bắt đầu, chưa lưu cuộc gọi nhỡ vào lịch sử hội thoại; còn ai được gọi ai, cách từ chối, thông báo khi vắng mặt/bận. | OQ-006 |
| Phòng thoại | Đã chốt quyền xem là đủ để vào; còn ai tạo phòng, quyền nói, số người tối đa mỗi phòng và hành vi khi đầy. | OQ-004, OQ-006 |
| Video/chia sẻ | Đã chốt nhiều người chia sẻ cùng lúc trong phòng; còn số luồng tối đa, quyền bật/chia sẻ và cách chọn nguồn hiển thị. | OQ-006 |
| Mất kết nối | Đã chốt tự kết nối lại; còn thời gian chờ, số lần thử, trạng thái trong lúc thử và xử lý khi không khôi phục được. | OQ-006 |
| Chất lượng | Chỉ tiêu âm thanh/hình ảnh, độ trễ, tỷ lệ kết nối thành công, thiết bị/mạng thử. | OQ-007 |
| Kỹ thuật/chi phí | Giải pháp media, băng thông, hạ tầng, giám sát và chi phí theo tải. | OQ-008, OQ-010 |

Vg và Sáng cần thử nghiệm giải pháp media theo kịch bản đã chốt trước
khi xác nhận lịch, chi phí và điều kiện nghiệm thu. Thái chuẩn bị giao
diện/trạng thái và kịch bản kiểm thử cùng nhóm.

## 5. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Lập khung phạm vi, hành trình, tiêu chí chức năng và danh sách quyết định media còn thiếu. |
| 0.2 | 2026-09-30 | Bổ sung điều kiện người nhận chấp nhận trước khi cuộc gọi riêng bắt đầu. |
| 0.3 | 2026-09-30 | Ghi nhận để lưu cuộc gọi nhỡ trong hội thoại sang đợt sau. |
| 0.4 | 2026-09-30 | Chốt quyền vào phòng thoại, chia sẻ nhiều màn hình đồng thời và tự kết nối lại. |

[Mục lục hồ sơ](../README.md)

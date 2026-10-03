# SCDC — Thoại, video và chia sẻ màn hình

Cập nhật: 2026-10-03. Phạm vi: REQ-007/008/009, SCP-006/007, DEC-046–050 và AC-MEDIA.

Media thuộc MVP. Đây vẫn là khung đặc tả: chưa có backend/provider/API, giới hạn hoặc ngưỡng chất lượng được xác nhận. Các bảng dưới đây ghi rõ điều đã chốt và đầu vào cần bổ sung, không thay bằng giả định triển khai.

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
Sản phẩm chạy trên trình duyệt web; ma trận trình duyệt và thiết bị chưa
được chốt. Giả định 20 người tham gia gọi đồng thời toàn hệ thống ở
[SCDC-EST-001](../project.md#budget) chỉ dùng
để dự trù, chưa phải giới hạn sản phẩm hoặc mức nghiệm thu.

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
tự kết nối lại khi mạng trở lại (DEC-050). Số người/luồng tối đa, thời
gian chờ và chất lượng tối thiểu chưa được chốt.

<a id="ux"></a>

## 2. Giao diện và trạng thái

| Bước | Vùng giao diện dự kiến | Phản hồi cần thể hiện |
|---|---|---|
| Gọi riêng | Thao tác gọi từ hội thoại và màn hình cuộc gọi đến. | Người nhận bấm chấp nhận trước khi bắt đầu; người gọi biết trạng thái chờ/đã nhận/không nhận. Cuộc gọi nhỡ chưa lưu vào hội thoại ở đợt đầu. |
| Vào phòng thoại | Danh sách phòng và màn hình cuộc gọi nhóm. | Thành viên có quyền xem phòng vào ngay; người không có quyền không thấy/không vào được. |
| Thiết bị và chia sẻ | Điều khiển micro, camera, chia sẻ màn hình và danh sách nguồn. | Phản hồi khi không được cấp quyền; nhiều người có thể chia sẻ đồng thời trong phòng, mỗi nguồn phân biệt được. |
| Mất mạng | Trạng thái cuộc gọi đang gián đoạn. | Báo đang kết nối lại; tự thử khi mạng trở lại; báo rõ nếu không khôi phục được. |

Giới hạn số người/luồng, thời gian chờ và bố cục khi nhiều nguồn chia sẻ
đang chờ OQ-006/OQ-007. Bản khung này chưa đủ để chốt wireframe media.

Wireframe media chưa hoàn thiện. Cần thiết kế các trạng thái chờ nhận, từ chối/bận, đang kết nối, trong cuộc gọi, mất mạng/reconnect, không cấp quyền thiết bị, nguồn chia sẻ dừng, phòng đầy và mất quyền. Trạng thái thiết bị phải phản ánh thực tế, không chỉ trạng thái nút bấm.

DEC-059 chốt thiết bị cho Accounts/DM; không tự mở rộng thành media hỗ trợ mọi trình duyệt điện thoại. Ma trận media phải chốt riêng ở OQ-007.

<a id="contracts"></a>

## 3. Dữ liệu, API và tích hợp cần thiết kế

MVP vẫn chạy Modular Monolith theo DEC-060; một nhà cung cấp media bên ngoài có thể được tích hợp nếu thử nghiệm phù hợp. LiveKit từng được nêu là phương án, chưa phải lựa chọn đã duyệt hoặc service đang có.

| Hợp đồng cần có | Hành vi cần xác định |
|---|---|
| Gọi riêng | Tạo lời gọi, thông báo đến, nhận/từ chối/hủy/kết thúc, điều kiện ai gọi ai, timeout/bận |
| Phòng thoại | Quyền vào/rời, danh sách người, giới hạn, mất quyền và vòng đời phòng |
| Thiết bị/chia sẻ | Micro/camera, chọn nguồn, nhiều nguồn cùng lúc, nguồn bị thu hồi hoặc dừng |
| Cấp quyền media | Kiểm tra phiên và quyền trước khi cấp thông tin kết nối; ngăn truy cập bằng token/phiên đã thu hồi |
| Khôi phục | Retry/reconnect, thời gian chờ, đồng bộ danh sách và trạng thái thiết bị |
| Quan sát/chi phí | Số người/luồng, băng thông, chất lượng, lỗi kết nối và chi phí theo mức dùng |

Chưa có schema dữ liệu hoặc endpoint media được duyệt. `calls` từng xuất hiện trong docs kỹ thuật cũ nhưng không phải schema hiện tại trong SQL. Trạng thái gọi nhỡ không lưu vào hội thoại ở đợt đầu theo DEC-047; không tự đưa log lịch sử cuộc gọi vào scope.

API tương lai áp dụng [ProblemDetails chung](../architecture.md#contracts). Media phải kiểm tra phiên, quyền Community khi dùng phòng và điều kiện DM khi gọi riêng; thời hạn thu hồi trên kết nối phải được thiết kế và thử nghiệm.

<a id="acceptance"></a>

## 4. Tiêu chí chấp nhận và kiểm thử

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

Mọi ca AC-MEDIA chưa có kết quả chạy được ghi nhận. Chuẩn bị hai người gọi riêng, phòng nhiều thành viên, người không có quyền, hai nguồn chia sẻ, trình duyệt/thiết bị đã chọn và khả năng ngắt mạng/từ chối quyền thiết bị. Với mỗi AC, ghi thao tác, kết quả thực tế và chỉ số theo ngưỡng đã chốt.

Thử phòng đầy, mất quyền khi đang gọi, người nhận bận, nguồn chia sẻ tự dừng và lỗi nhà cung cấp sau khi quy tắc được xác nhận. Các tình huống này là đầu vào kiểm thử, chưa tự đặt kết quả nghiệp vụ chưa có quyết định.

<a id="gaps"></a>

## 5. Quyết định còn thiếu

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

# SCDC — Chất lượng và cách đo

Trang này ghi các mục tiêu chất lượng của v1 (tốc độ, độ tin cậy, chất lượng media) và cách đo để kết luận đạt hay chưa.

**Trạng thái:** mục tiêu đã chốt; chưa có kết quả đo nào. Mục tiêu backup và khôi phục được hoãn sang sau v1 ([phạm vi backup](../project/overview.md#backup-scope)). Tiêu chí của MVP xem [hồ sơ MVP](../releases/mvp.md).

<a id="quality-targets"></a>

<a id="ma-trận-hỗ-trợ-và-mục-tiêu-đo-đã-xác-nhận"></a>

## Trình duyệt và thiết bị hỗ trợ

- **Máy tính:** Chrome, Edge, Firefox, Safari; hai phiên bản ổn định mới nhất tại thời điểm khóa bản build nghiệm thu.
- **Điện thoại:** Chrome trên Android và Safari trên iOS, cho tài khoản, DM và Community.
- **Media (thoại, video):** v1 chỉ cam kết trên máy tính.

Vg thống nhất danh sách. Mỗi người ghi lại cho phần của mình: hệ điều hành, thiết bị, kích thước màn hình, phiên bản trình duyệt, bản build và ngày khóa. Cần kiểm tra bàn phím, bộ gõ, focus, thông báo lỗi, cuộn trang và trạng thái thiết bị trên máy thật; hai kích thước wireframe không thay được.

## Mục tiêu

| Chỉ số | Mục tiêu | Cách đo |
|---|---|---|
| Thời gian API, gửi đến lưu | p95 ≤ 500 ms | Đo từ lúc client gửi request đến lúc nhận phản hồi. Với gửi tin: từ thao tác gửi đến lúc commit, dùng trace có hiệu chỉnh đồng hồ; không dùng `createdAt` làm thời điểm commit. |
| Lưu đến hiển thị | p95 ≤ 1 giây | Từ lúc commit đến lúc giao diện người nhận hiển thị. Chỉ tính người nhận đang online và đã đăng ký nhận hợp lệ; nhận khi offline đo riêng qua lịch sử. |
| Lỗi dịch vụ chat | < 1% | Tỷ lệ lỗi 5xx hoặc timeout trên request hợp lệ. Lỗi 4xx của ca cố ý sai tính riêng. |
| Thu hồi chat | ≤ 5 giây | Từ lúc commit thu hồi phiên hoặc quyền đến lúc kết nối ngừng nhận nội dung mới. Request HTTP mới luôn kiểm tra trạng thái hiện tại. |
| Thu hồi media | ≤ 5 giây; không xác nhận được quyền thì dừng | Từ lúc commit đến khi cả gửi và nhận luồng media dừng, và token cũ không vào lại được. Đo cả khi các thành phần điều phối bị lỗi. Websocket đóng không phải là bằng chứng. |
| Tính đúng | Không mất tin, không trùng tin, không truy cập trái quyền | Thử mất phản hồi, ghi đồng thời, gửi lại, worker dừng, sửa, xóa, kết nối lại và người thứ ba truy cập. Một lỗi là ca không đạt. |
| Vào cuộc gọi | p95 ≤ 5 giây; thành công ≥ 99% | Từ lúc bấm vào hoặc nhận cuộc gọi đến lúc kết nối và nhận được luồng. Không bắt buộc cấp quyền micro, camera ngay. Cần ít nhất 100 lần hợp lệ; trường hợp bận hoặc đầy đo riêng. |
| Độ trễ media | Âm thanh p95 ≤ 300 ms; video và chia sẻ p95 ≤ 500 ms | Dùng tín hiệu âm thanh và khung hình thử có dấu thời gian, không dùng nội dung thật. Ghi độ lệch đồng hồ và phương pháp đo. |
| Chất lượng hình | Camera 720p/30fps; chia sẻ màn hình 720p/15fps | Điều kiện mạng mỗi máy: tải lên ≥ 10 Mbps, tải xuống ≥ 20 Mbps, RTT ≤ 100 ms, mất gói ≤ 1%. Ghi độ phân giải và FPS thực tế, không suy từ cấu hình. |
| Khôi phục DB (sau v1) | RPO ≤ 15 phút; RTO ≤ 4 giờ; bản backup giữ ≤ 30 ngày | Giữ làm mục tiêu cho đợt backup sau v1. Khi làm, đo thời gian khôi phục thực tế và áp lại cơ chế bảo vệ của DEC-108. |

## Mobile, thông báo và gửi file

Ba phần này chưa có tiêu chí riêng. Khi chốt gói, người phụ trách đề xuất hành trình, thiết bị, tính đúng, quyền, lỗi và cách đo để Vg duyệt. Không tự áp tiêu chí của trình duyệt hay media trên máy tính cho app mobile, và không coi kết quả MVP là bằng chứng cho phần bổ sung.

<a id="measurement-readiness"></a>

## Chuẩn bị đo

Trước khi đo cần chốt: bản build, phiên bản trình duyệt và thiết bị, môi trường, dữ liệu thử, tải thử và công cụ đo. Đồng hồ các máy phải được hiệu chỉnh. Các kịch bản dưới đây là đề xuất để nhóm rà soát trước khi chạy.

### Chat

- 100 người online liên tục 60 phút, sau 5 phút khởi động.
- Dữ liệu sẵn: 1.000 tài khoản, 500 DM, 20 cộng đồng với 50 phòng chữ, 100.000 tin. Hai cuộc trò chuyện có ít nhất 10.000 tin để thử tải nhiều trang.
- 80 người ở 40 DM, 20 người ở các phòng có đúng quyền.
- Mỗi người gửi 1 tin mỗi 10 giây (khoảng 10 tin/giây, 36.000 tin trong 60 phút), tải một trang lịch sử mỗi 30 giây, sửa hoặc xóa một tin của mình mỗi 5 phút.
- Không dùng tài khoản hoặc DM thật.

Các ca lỗi chạy riêng: mất phản hồi, rollback transaction, worker dừng 30 giây, kết nối lại, gửi, sửa, xóa đồng thời, và thu hồi quyền khi đang nhận tin. Sau đó đối chiếu toàn bộ mã thao tác, mã tin, nội dung, tin đã xóa và quyền với số thao tác đã commit.

Ghi CPU, RAM, DB, mạng, số kết nối; báo p50, p95, p99, số mẫu, tỷ lệ lỗi và hàng đợi tồn. Kịch bản này là mức cơ sở, không có nghĩa mọi dạng tải đã được thử.

### Media

- Phòng A 10 người, phòng B 8 người và một cuộc gọi riêng 2 người, tổng 20.
- Bật micro và camera cho tất cả; mỗi phòng hai người chia sẻ màn hình, cuộc gọi riêng cả hai chia sẻ.
- Chạy 60 phút. Thử phòng đầy và kết nối lại riêng.
- Khi mạng yếu hơn điều kiện trên, giao diện vẫn phải báo rõ chất lượng giảm, lỗi hoặc đang kết nối lại.

Kiểm tra thu hồi quyền media tách riêng khỏi đo tải: thu hồi giữa lúc đang truyền, ghi thời điểm ở cả bên gửi và bên nhận, thử dùng lại token cũ, cắt kết nối tới các thành phần điều phối nhưng giữ WebRTC, và thử client bị sửa để phát thêm nguồn. Chi tiết xem [thiết kế media](../features/media/participation-lifecycle.md).

### Quy tắc báo cáo

- Mỗi phép đo ghi điểm bắt đầu và kết thúc, số mẫu thành công và thất bại, thời gian khởi động và các mẫu bị loại kèm lý do.
- Báo theo từng hành trình và nhóm thiết bị, bên cạnh số tổng. Không trộn người nhận offline hoặc máy ngoài điều kiện mạng vào số liệu chính.
- Timeout và lỗi 4xx bất thường trên request hợp lệ vẫn phải ghi và giải thích.
- Nếu độ lệch đồng hồ hoặc công cụ đo chưa đủ tin cậy, kết quả chưa được xác nhận.

## Còn mở

- Quy tắc kết luận chất lượng hình: tỷ lệ thời gian tối thiểu phải đạt độ phân giải và FPS, cách lấy mẫu và tổng hợp ([OQ-007](../project/open-questions.md#oq-007)). Phải chốt trước nghiệm thu; chưa chốt thì chưa kết luận được.
- Tất cả các ca đo chưa chạy.

---

Nguồn: DEC-082 (thiết bị), DEC-083 (chat), DEC-085 và DEC-101 (media), DEC-099 (thu hồi media), DEC-086 và DEC-109 (khôi phục), DEC-156 (hoãn backup), [DEC-171](../records/decisions/README.md#dec-171), [DEC-173](../records/decisions/README.md#dec-173).

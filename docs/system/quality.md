# SCDC — Chất lượng và phương pháp đo

Ngưỡng đầy đủ áp dụng cho v1. MVP chọn tiêu chí theo phạm vi tại [hồ sơ MVP](../releases/mvp.md); chưa có kết quả đo hệ thống trong lần cấu trúc lại tài liệu.

<a id="quality-targets"></a>

<a id="ma-trận-hỗ-trợ-và-mục-tiêu-đo-đã-xác-nhận"></a>

### Ma trận hỗ trợ và mục tiêu đo đã xác nhận

DEC-082: desktop Chrome/Edge/Firefox/Safari, hai phiên bản ổn định gần nhất tại lúc khóa build nghiệm thu; điện thoại Chrome Android/Safari iOS cho Accounts/DM/Community; media v1 chỉ cam kết desktop. Thái ghi OS, thiết bị, kích thước viewport, phiên bản browser, build và ngày khóa ma trận. Kiểm tra bàn phím/IME, focus, thông báo lỗi, cuộn và trạng thái thiết bị; hai kích thước wireframe không thay thế ca trên thiết bị thật.

| Chỉ số | Mục tiêu | Điều kiện/cách ghi nhận |
|---|---|---|
| API / gửi→lưu | p95 ≤500 ms | DEC-083; đo API client request→response; gửi→lưu từ thao tác gửi đến commit theo trace được hiệu chỉnh đồng hồ, không dùng createdAt làm commit time |
| Lưu→hiển thị online | p95 ≤1 giây | Commit timestamp→UI merge/render; người nhận online và subscribe hợp lệ; tin nhận offline đo riêng hành vi lịch sử |
| Lỗi dịch vụ chat | <1% | Unexpected 5xx/timeout trên request hợp lệ; 4xx của ca cố ý sai tách riêng, không che bằng loại mọi request lỗi |
| Thu hồi chat đang mở | ≤5 giây | Từ commit thu hồi phiên/quyền đến lúc connection không nhận nội dung mới; request HTTP mới kiểm tra trạng thái hiện hành |
| Thu hồi media đang mở | ≤5 giây; không xác nhận quyền thì dừng | DEC-099; từ commit tới cả phát/nhận RTP ngừng và token cũ/SFU refresh không vào lại; đo khi authority/outbox/RoomService lỗi, không lấy websocket đóng làm bằng chứng |
| Tính đúng | Không mất/trùng tin, không truy cập trái quyền | Lost response, concurrent commit, replay, worker dừng, sửa/xóa/reconnect và third-party access; một lỗi làm ca không đạt |
| Vào cuộc gọi | p95 ≤5 giây; thành công ≥99% | DEC-085/101; từ bấm join/accept tới connected/khả năng nhận luồng, không bắt cấp mic/camera ban đầu; ≥100 lần hợp lệ, busy/full đo riêng; bật nguồn và cấp quyền capture đo riêng |
| Độ trễ media | Audio p95 ≤300 ms; video/share p95 ≤500 ms | Dùng tín hiệu âm thanh và frame timestamp thử, không nội dung người dùng; ghi độ lệch đồng hồ/phương pháp đo |
| Hình ảnh | Camera mục tiêu 720p/30fps; share 720p/15fps | Mạng mỗi máy upload ≥10/download ≥20 Mbps, RTT ≤100 ms, loss ≤1%; ghi resolution/FPS thực tế và tỷ lệ thời gian đạt, không suy từ cấu hình capture |
| Khôi phục DB | RPO ≤15 phút; RTO ≤4 giờ; tuổi artefact backup/WAL ≤30 ngày | DEC-086/109; đo actual earliest/latest recovery point, không hứa PITR mọi mốc tròn 30 ngày; restore áp lại bảo vệ DEC-108 |

Kịch bản chat theo DEC-083: 100 người online liên tục 60 phút sau warm-up 5 phút; riêng các ca chức năng âm tính/fault injection có nhãn. Workload kỹ thuật đề xuất: 1.000 tài khoản, 500 DM, 20 cộng đồng/50 phòng text và 100.000 tin preload, trong đó hai hội thoại/phòng có ít nhất 10.000 tin để thử nhiều trang. Trong 100 người online, 80 người ở 40 DM và 20 người ở các phòng có đúng quyền; mỗi người gửi một tin/10 giây, khoảng 10 tin/giây và 36.000 tin mới trong 60 phút. Mỗi client tải trang lịch sử/30 giây; chèn sửa/xóa một tin của chính mình/5 phút. Ghi độ dài nội dung/thời điểm, không dùng tài khoản/DM thật.

Test correctness/fault tách các mốc mất response, transaction rollback, worker dừng 30 giây, reconnect, concurrent send/edit/delete và revoke trong khi stream; không loại lỗi bất thường do hệ thống gây ra khỏi báo cáo. Đối soát toàn bộ operation ID/message ID, nội dung hiện hành, tombstone và quyền với số thao tác hợp lệ đã commit. Ghi CPU/RAM/DB/NIC, region, số connection và concurrency từng bước; báo p50/p95/p99/số mẫu/tỷ lệ lỗi/backlog. Đồng hồ test phải được hiệu chỉnh và độ lệch ghi trong báo cáo. Workload này là baseline để nhóm rà soát, không suy 100 online là mọi hình thức tải đều đã thử.

Kịch bản media đề xuất: phòng A 10, phòng B 8 và một DM hai người, tổng 20; thử mic/camera toàn bộ, hai màn hình/phòng và hai màn hình cuộc gọi riêng. Chạy 60 phút và thử join đầy/reconnect riêng. Mạng yếu ngoài điều kiện DEC-085 vẫn phải có trạng thái giảm chất lượng/lỗi/reconnect rõ, không suy ngưỡng good-network thành cam kết mọi mạng. Chưa có kết quả đo cho các mục tiêu này.

Mỗi phép đo ghi điểm bắt đầu/kết thúc, số mẫu thành công/thất bại, mẫu số tỷ lệ, thời gian warm-up và phần loại trừ có lý do. Báo kết quả theo hành trình và nhóm browser/thiết bị bên cạnh tổng thể; không trộn offline hoặc máy ngoài điều kiện mạng để che kết quả người nhận online/good-network. Timeout và 4xx bất thường của request hợp lệ vẫn phải được giải thích/ghi lỗi, không tự loại vì không phải 5xx. Độ lệch clock hoặc instrumentation chưa đủ tin cậy thì kết quả chưa được xác nhận; không lấy createdAt hoặc cấu hình capture làm phép đo thay thế.

Proof quyền media tách khỏi tải good-network: phát tín hiệu thử có timestamp, thu hồi giữa lúc RTP chạy; ghi commit/cutoff ở cả sender và receiver, thử SFU refresh token/direct route/clone epoch. Cắt kết nối authority/worker/RoomService nhưng giữ WebRTC, đo lease TTL/clock skew/watchdog theo [thiết kế media](../features/media/participation-lifecycle.md). Thử publish dư camera/screen hoặc screen_audio từ client đã sửa; không được truyền trước lúc hậu kiểm. Giữ reservation/reconnect/draining trong phép đếm; không coi remove request thành ack dừng. Tất cả ca chưa chạy.

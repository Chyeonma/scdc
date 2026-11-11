# SCDC — Quy trình làm việc

Nhóm làm việc theo từng gói, mỗi gói có đầu ra kiểm tra được. Trang này mô tả các bước làm một gói, điều kiện để bắt đầu, cách review, quản lý nhiệm vụ trên Trello và họp hằng tuần.

**Áp dụng từ:** 2026-11-01. Vg giữ phạm vi, kiến trúc và triển khai chung; người phụ trách từng service tự lập kế hoạch chi tiết và tạo nhiệm vụ trong phạm vi đã thống nhất.

Phân công và công suất xem [kế hoạch](planning.md); phạm vi sản phẩm xem [tổng quan](overview.md#scope); đầu ra bàn giao xem [v1](../releases/v1.md).

<a id="process"></a>

## Các bước làm một gói

| Bước | Đầu ra |
|---|---|
| 1. Chọn gói | Mục tiêu, trong và ngoài phạm vi, UC và AC áp dụng, phụ thuộc, người phụ trách |
| 2. Thiết kế hành vi và giao diện | Quy tắc, luồng chính và ngoại lệ, màn hình và trạng thái, kết quả mong đợi |
| 3. Thiết kế kỹ thuật | API, dữ liệu, module, transaction, xử lý đồng thời và gửi lại, migration |
| 4. Kiểm tra nhất quán | Giao diện có đủ dữ liệu; API đáp ứng luồng; DB giữ đúng ràng buộc; quyền và lỗi có cách kiểm thử |
| 5. Chuẩn bị nền | SQL, migration, dữ liệu mẫu, cấu hình, môi trường và khung code |
| 6. Kiểm tra nền | Dựng và chạy được môi trường; hợp đồng và schema khớp thiết kế |
| 7. Làm | Code nghiệp vụ, test thành phần và tích hợp, review code |
| 8. Ghi kết quả | Commit, build, phiên bản tài liệu và hợp đồng, môi trường, kết quả thực tế, lỗi, phần chưa chứng minh |

Giao diện, API và dữ liệu được điều chỉnh qua lại trong gói. Các gói độc lập có thể làm song song khi hợp đồng chung đã rõ. Chỉ viết phần tài liệu phục vụ trực tiếp gói đang làm.

<a id="topic"></a>

## Một trang tính năng gồm gì

1. Mục đích, phạm vi và người thực hiện.
2. Khái niệm, trạng thái, quyền, quy tắc.
3. Luồng chính và ngoại lệ, giao diện, link tới wireframe hoặc prototype.
4. Tiêu chí chấp nhận và ca kiểm thử.
5. API, sự kiện, dữ liệu và link tới hợp đồng máy đọc.
6. Xử lý backend, transaction, đồng thời, gửi lại, migration.
7. Yêu cầu môi trường riêng và phụ thuộc trực tiếp.
8. Trạng thái, câu hỏi còn mở và link tới bằng chứng.

Tính năng nhỏ dùng một file. Tính năng có tài liệu phụ (ảnh, wireframe, fixture) dùng một thư mục có `README.md` và thư mục con `ui/`. Quy tắc giao diện viết trong tài liệu chính; `ui/` chỉ chứa hình. Không tạo thêm cây thư mục specs, design, delivery riêng cho cùng tính năng.

Cách viết xem [quy ước viết tài liệu](../guides/writing.md).

<a id="readiness"></a>

## Điều kiện bắt đầu một gói

Bảng dưới là điều kiện cần có trước khi làm, không phải bảng tiến độ.

| Mã | Điều kiện |
|---|---|
| READY-01 | Tài khoản, phiên và việc gửi email mà gói cần đã có hợp đồng rõ |
| READY-02 | Hành vi, ngoại lệ, quyền và nội dung tin của gói đã rõ |
| READY-03 | Chính sách Community áp dụng cho gói chỉ được định nghĩa ở một nơi |
| READY-04 | Giao diện có đủ các trạng thái cần thiết: đang tải, trống, lỗi, mất mạng, mất quyền |
| READY-05 | API, dữ liệu, transaction và migration đã được đối chiếu với giao diện và tiêu chí chấp nhận |
| READY-06 | Các giả định kỹ thuật ảnh hưởng trực tiếp tới gói có cách kiểm tra |
| READY-07 | Có dữ liệu thử, công cụ chạy, kết quả mong đợi và cách ghi bằng chứng |
| READY-08 | Có người làm, người kiểm tra, ước lượng, và phụ thuộc nằm trong công suất |

Câu hỏi chặn gói phải được trả lời, hoặc có cách xử lý, trước khi làm. Câu hỏi ngoài phạm vi gói ghi ở trang tính năng hoặc [câu hỏi còn mở](open-questions.md). Được phân công làm không có nghĩa được quyền merge hay phát hành.

<a id="review"></a>

## Review và kiểm thử

Người phụ trách tự chuẩn bị yêu cầu, giao diện, thiết kế kỹ thuật, dữ liệu thử, tự kiểm thử và viết tài liệu. Một thành viên khác kiểm tra lại; tên người review ghi trên thẻ Trello. Phạm vi của từng người xem [phân công](planning.md#team).

**Trạng thái tài liệu:** nháp → đã review → đã xác nhận → đã thay thế.

**Trạng thái làm:** chưa làm → đang làm → có code chưa kiểm thử → đã kiểm thử trong phạm vi ghi nhận. Một tính năng có thể có nhiều phần ở các trạng thái khác nhau.

Đọc code test, kiểm tra schema hay fixture, chạy test và nghiệm thu là bốn mức bằng chứng khác nhau. Kết quả chạy ghi kèm commit, build và môi trường, lưu tại `records/verification/`. Trang tính năng chỉ ghi trạng thái hiện tại và link. Điều kiện phát hành xem [nghiệm thu](../releases/acceptance.md).

<a id="trello"></a>

### Quản lý nhiệm vụ trên Trello

- Mỗi người tự chia nhỏ công việc của mình, tạo thẻ, ước lượng, cập nhật tiến độ và gắn bằng chứng.
- Cả nhóm dùng chung một bảng để thấy phụ thuộc.
- Mỗi thẻ theo [mẫu](../records/templates/trello-task.md): service hoặc mã gói, đầu ra, phạm vi, UC và AC nếu có, người làm và người review, ước lượng, hạn dự kiến, phụ thuộc, tiêu chí, kết quả.
- Các cột: Cần làm → Đang làm → Chờ kiểm tra → Hoàn tất. Thẻ bị chặn ghi rõ nguyên nhân, người cần xử lý và bước tiếp theo.
- Chỉ chuyển sang Hoàn tất khi đạt tiêu chí và có kết quả kiểm tra. Số thẻ hoàn tất không phải tỷ lệ hoàn thành sản phẩm.

Vg tổng hợp ưu tiên và mốc chung. Thay đổi phạm vi, kiến trúc hoặc hợp đồng chung phải thống nhất trước khi làm. Chi tiết trong phạm vi đã chốt do người phụ trách tự quyết.

<a id="weekly-meeting"></a>

### Họp hằng tuần

Nhóm họp mỗi tuần; ngày giờ do nhóm sắp xếp. Mỗi người trình bày:

- Việc đã làm, kèm kết quả chạy được.
- Việc chưa xong.
- Vấn đề gặp phải và nguyên nhân.
- Phụ thuộc vào người khác.
- Kế hoạch tuần tới.

Nhóm giải quyết vấn đề trước mắt và thống nhất cách phối hợp; Vg quyết định các nội dung chung.

Ghi [biên bản ngắn](../records/templates/weekly-meeting.md): quyết định, người xử lý, hạn và link tới thẻ hoặc bằng chứng. Sau họp, mỗi người cập nhật thẻ của mình; thay đổi nghiệp vụ hay thiết kế cập nhật vào trang tính năng; quyết định quan trọng thêm vào [hồ sơ quyết định](../records/decisions/README.md).

Vướng mắc làm người khác phải dừng việc thì báo ngay trên thẻ hoặc kênh nhóm, không chờ họp tuần.

<a id="technical-evidence"></a>

### Các tình huống kỹ thuật cần kiểm thử

Chọn những tình huống áp dụng cho gói:

- Người ngoài hoặc người đã mất quyền không đọc, gửi hay đăng ký nhận tin được.
- Transaction bị rollback không để lại dữ liệu hay sự kiện dở dang.
- Mất phản hồi sau khi commit, hoặc gửi lại đồng thời, không tạo thêm thay đổi.
- Phân trang và kết nối lại không bỏ sót tin đã commit, không hiện bản tin cũ hơn.
- Worker dừng rồi chạy lại không thực hiện một việc hai lần.
- Thu hồi phiên hoặc quyền đạt [yêu cầu thu hồi](../system/access-revocation.md) trên HTTP và trên kết nối thật.

<a id="changes"></a>

## Quản lý thay đổi

- Trang tính năng mô tả hành vi hiện tại. Hồ sơ quyết định giữ lý do, ngày, người xác nhận và quyết định bị thay thế.
- Giữ nguyên các mã REQ, SCP, SUC, UC, AC, TC, DEC, OQ, COST, AS, RSK. Khi sửa một quy tắc, sửa luôn các hợp đồng bị ảnh hưởng trong cùng thay đổi.
- Thay đổi phạm vi, thời gian hoặc ngân sách do đại diện sản phẩm xác nhận. Chi tiết trong gói đã thống nhất do người phụ trách xử lý.
- Hồ sơ kết quả cũ giữ nguyên phạm vi và phiên bản đã kiểm thử.

---

Nguồn: SCDC-PRC-001, SCDC-READY-001, [DEC-171](../records/decisions/README.md#dec-171), DEC-172.

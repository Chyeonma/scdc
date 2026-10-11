# SCDC — Quy trình phát triển

SCDC-PRC-001. Áp dụng theo từng gói có đầu ra kiểm chứng được. Phân công và công suất nằm tại [kế hoạch](planning.md); phạm vi tại [MVP](../releases/mvp.md) và [v1](../releases/v1.md).

## Mục lục

- [Trình tự thực hiện](#process)
- [Nội dung tài liệu chủ đề](#topic)
- [Điều kiện triển khai](#readiness)
- [Rà soát và kiểm chứng](#review)
- [Quản lý thay đổi](#changes)

<a id="process"></a>

## Trình tự thực hiện

| Bước | Đầu ra |
|---|---|
| 1. Chọn gói | Mục tiêu, phạm vi/ngoài phạm vi, UC/AC áp dụng, phụ thuộc và người phụ trách |
| 2. Thiết kế hành vi và UI/UX | Quy tắc, luồng chính/ngoại lệ, màn hình/trạng thái và kết quả mong đợi |
| 3. Thiết kế kỹ thuật | API, dữ liệu, module, transaction, đồng thời/retry và migration |
| 4. Rà soát tính nhất quán | UI đủ dữ liệu; API đáp ứng luồng; DB giữ bất biến; quyền và lỗi có cách kiểm chứng |
| 5. Chuẩn bị nền thực thi | SQL/migration/seed, cấu hình/môi trường và khung mã nguồn cần cho gói |
| 6. Kiểm tra nền | Dựng/chạy được môi trường; hợp đồng và schema khớp với thiết kế |
| 7. Triển khai | Code nghiệp vụ, kiểm thử thành phần/tích hợp và rà soát mã nguồn |
| 8. Ghi kết quả | Commit/build, phiên bản tài liệu/hợp đồng, môi trường, actual result, lỗi và phần chưa chứng minh |

UX, API và dữ liệu được điều chỉnh qua lại trong gói. Các gói độc lập có thể làm song song sau khi hợp đồng chung đủ rõ. Chỉ hoàn thiện phần tài liệu trực tiếp phục vụ gói; media, microservice và vận hành công khai được chuẩn bị theo mốc tương ứng.

<a id="topic"></a>

## Nội dung tài liệu chủ đề

1. Mục đích, phạm vi và người thực hiện.
2. Khái niệm, trạng thái, quyền, quy tắc và bất biến.
3. Luồng chính/ngoại lệ, UI/UX và liên kết wireframe/prototype.
4. Tiêu chí chấp nhận và tình huống kiểm thử.
5. API/sự kiện, dữ liệu và liên kết hợp đồng máy đọc.
6. Luồng backend, transaction, đồng thời, retry và migration.
7. Yêu cầu môi trường riêng, phạm vi gói và phụ thuộc trực tiếp.
8. Trạng thái theo khả năng, câu hỏi còn mở và liên kết bằng chứng.

Chủ đề nhỏ dùng một file Markdown; chủ đề có artefact riêng dùng một thư mục với `README.md` và `ui/` hoặc fixture. Quy tắc UX nằm trong tài liệu chính; `ui/` chứa ảnh/wireframe/prototype. Không tạo thêm các cây specs/design/delivery cho cùng chủ đề.

<a id="readiness"></a>

## Điều kiện triển khai

SCDC-READY-001. Bảng dưới đây giữ điều kiện chuẩn bị, không phải bảng tiến độ hoặc kết quả nghiệm thu.

| Mã | Điều kiện |
|---|---|
| READY-01 | Tài khoản/phiên và job email trực tiếp phục vụ gói có hợp đồng rõ |
| READY-02 | Hành vi, ngoại lệ, quyền và nội dung tin của gói đủ rõ |
| READY-03 | Chính sách Community áp dụng cho gói có một nguồn định nghĩa |
| READY-04 | UI/UX có trạng thái tải/rỗng/lỗi/mất mạng/mất quyền cần thiết |
| READY-05 | API, dữ liệu, transaction và migration được đối chiếu với UI/AC |
| READY-06 | Các giả định kỹ thuật trực tiếp ảnh hưởng gói có phương án kiểm chứng |
| READY-07 | Có dữ liệu thử, bộ chạy, expected result và cách ghi bằng chứng |
| READY-08 | Có người làm/kiểm tra lại, ước lượng và phụ thuộc trong quỹ công suất |

Câu hỏi chặn gói phải được giải quyết hoặc có phương án xử lý trước triển khai. Các câu hỏi ngoài phạm vi gói được giữ tại chủ đề hoặc [danh mục OQ](open-questions.md). Phân công phát triển không tự cấp quyền merge hoặc phát hành.

<a id="review"></a>

## Rà soát và kiểm chứng

Người phụ trách chuẩn bị thiết kế và tự kiểm thử; một thành viên khác kiểm tra lại. Vg chủ trì sản phẩm/kiến trúc/quyền; Sáng phụ trách Messaging; Thái phụ trách worker/môi trường/bộ chạy theo [phân công](planning.md#team). Đây là vai trò kiêm nhiệm trong nhóm ba người.

Trạng thái tài liệu: nháp / đã rà soát / đã được xác nhận / đã thay thế. Trạng thái triển khai: chưa làm / đang làm / có code chưa kiểm chứng / đã kiểm chứng trong phạm vi ghi nhận. Ghi theo khả năng; một chủ đề có thể chứa nhiều trạng thái.

Đọc source test, kiểm tra schema/fixture, chạy test và nghiệm thu là các loại bằng chứng khác nhau. Kết quả thực thi gắn commit/build/môi trường và lưu tại `records/verification/`; tài liệu chủ đề chỉ giữ trạng thái hiện tại và liên kết. Điều kiện phát hành tại [nghiệm thu](../releases/acceptance.md).

<a id="technical-evidence"></a>

### Tình huống kỹ thuật

Chọn các tình huống trực tiếp áp dụng cho gói:

- Người ngoài hoặc actor mất quyền không đọc/gửi/subscribe được.
- Transaction rollback không tạo dữ liệu hoặc sự kiện một phần.
- Mất response sau commit và retry đồng thời không tạo thêm mutation.
- Phân trang/reconnect không bỏ tin đã commit hoặc hạ version nội dung.
- Worker dừng rồi chạy lại không nhân đôi hiệu lực nghiệp vụ.
- Thu hồi phiên/quyền đáp ứng [hợp đồng thu hồi](../system/access-revocation.md) trên đường HTTP và kết nối thật.

<a id="changes"></a>

## Quản lý thay đổi

Tài liệu chủ đề định nghĩa hành vi hiện hành. Hồ sơ quyết định giữ lý do, ngày, người xác nhận và quyết định bị thay thế. Giữ mã REQ/SCP/SUC/UC/AC/TC/DEC/OQ/COST/AS/RSK; cập nhật nguồn nội dung và các hợp đồng bị ảnh hưởng trong cùng thay đổi.

Thay đổi phạm vi, thời gian hoặc ngân sách do đại diện sản phẩm xác nhận. Chi tiết triển khai trong gói đã thống nhất do người phụ trách xử lý và rà soát theo tác động. Hồ sơ kết quả lịch sử giữ nguyên phạm vi và revision đã kiểm chứng.

Tiêu đề dùng danh từ hoặc tên hành động trực tiếp, ví dụ “Mục lục”, “Phạm vi”, “Dữ liệu”, “Kiểm thử”. Dùng câu ngắn, thuật ngữ nhất quán; tránh lời dẫn hội thoại, nhận xét cảm tính và thông tin lặp. Giả định và đề xuất phải có nhãn; chưa có bằng chứng thì ghi rõ chưa kiểm chứng.

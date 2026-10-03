# SCDC — Theo dõi Đợt 0 và bàn giao cho phát triển

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-READY-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Đã bổ sung đầu vào; chưa xác nhận hoàn tất Đợt 0 |
| Căn cứ | [Kế hoạch theo đợt](01-delivery-plan.md), [quy trình](../development-process.md), [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Kết quả phiên tài liệu ngày 2026-10-03

Theo yêu cầu của đại diện sản phẩm, phiên này tiếp tục hoàn thiện hồ sơ
để chuẩn bị phát triển. Đã xác nhận DEC-051 đến DEC-059, cập nhật đặc tả
tài khoản/DM/cộng đồng, và bổ sung đầu vào UX, API/dữ liệu, phân quyền
và kiểm thử. Không có kết quả triển khai hay kiểm thử sản phẩm được ghi
nhận; chưa chuyển trạng thái dự án thành đang phát triển.

Các câu trả lời xác nhận hành vi sản phẩm tương ứng; chúng không đồng
nghĩa duyệt các đề xuất kỹ thuật, wireframe hoặc toàn bộ tiêu chí nghiệm
thu. Không mở lại quyết định đã chốt trong những lần rà soát tiếp theo
trừ khi có yêu cầu thay đổi cụ thể.

## 2. Bảng điều kiện sẵn sàng

| Mã | Điều kiện/đầu ra | Tình trạng và bằng chứng | Việc còn lại/đầu mối dự kiến |
|---|---|---|---|
| READY-01 | Tài khoản tối thiểu | Đã chốt định danh, email không công khai, quyền trước xác minh; có luồng và AC-ACC-01–10 tại [đặc tả](../03-requirements/03-accounts.md) | Rà soát ACC-P01–05, chính sách mật khẩu, phiên và chống lạm dụng; Vg/Sáng |
| READY-02 | DM và ngoại lệ | Đã chốt giới hạn 2.000 ký tự và không giữ lịch sử sửa; có AC-DM-01–15 và bảng ngoại lệ tại [đặc tả](../03-requirements/01-direct-messaging.md) | Khóa phép đếm/tìm kiếm/lưu giữ; rà soát trường hợp đồng thời; Vg/Sáng, Thái đối chiếu |
| READY-03 | Ma trận quyền cốt lõi | Đã chốt vai trò, ngoại lệ cá nhân, chủ sở hữu và thứ tự quyền tại [ma trận](../03-requirements/05-access-control-matrix.md) | Thiết kế dữ liệu/cấu hình/thu hồi; các luồng cộng đồng còn mở không chặn thiết kế DM độc lập |
| READY-04 | UX hai hành trình | Có wireframe văn bản [tài khoản/DM](../04-ux/02-account-dm-wireframes.md) và [cộng đồng](../04-ux/03-community-wireframes.md) | Vg rà soát, Thái dựng prototype; chốt ma trận trình duyệt/thiết bị và trạng thái còn mở |
| READY-05 | API/dữ liệu DM | Có [hợp đồng đề xuất](../05-architecture/02-account-dm-contracts.md) với schema logic, lỗi, chống trùng, lịch sử và cập nhật | Vg/Sáng khóa transport/ID/phiên, rà soát hợp đồng cùng Thái; chưa có OpenAPI/mock được xác nhận |
| READY-06 | Thử nghiệm kỹ thuật | Có kịch bản cần chứng minh tại mục 4; chưa chạy | Chọn công nghệ và môi trường, chạy và ghi bằng chứng; Vg/Sáng |
| READY-07 | Kiểm thử | Có [ca kiểm thử](../07-quality/02-account-dm-test-cases.md) và dữ liệu dự kiến; tất cả chưa chạy | Thái chuẩn bị fixture, Vg/Sáng rà soát; chốt ngưỡng chất lượng và ghi kết quả |
| READY-08 | Công việc/nguồn lực | Có các gói cụ thể tại mục 3; chưa có ngày công/lịch được xác nhận | Vg/Sáng/Thái ước lượng trong quỹ thời gian thật, gồm hướng dẫn và kiểm thử |

Không tính phần trăm từ số dòng hoàn tất. Mỗi gói chỉ sẵn sàng khi có
yêu cầu, thiết kế, kiểm chứng và người phụ trách tương ứng. Quyết định
media, chi phí toàn dự án và vận hành vẫn phải hoàn thành ở các đợt
liên quan; không tự loại chúng khỏi phiên bản đầu để đánh dấu Đợt 0 xong.

## 3. Gói việc đủ cụ thể để ước lượng

| Gói | Đầu ra | Phụ thuộc | Thực hiện / rà soát dự kiến | Điều kiện hoàn tất |
|---|---|---|---|---|
| PREP-01 | Chốt đặc tả tài khoản còn lại và dữ liệu biên | ACC-P*, OQ-002 | Vg, Sáng / Thái đối chiếu kiểm thử | Quy tắc không còn chỗ diễn giải khác nhau ở luồng bàn giao; AC cập nhật |
| PREP-02 | Prototype tài khoản/DM và cộng đồng theo mã màn hình | Wireframe, quyết định liên quan | Vg thiết kế, Thái dựng / Sáng rà soát phản hồi API | Có trạng thái rỗng/lỗi/mất mạng/mất quyền; ghi kết quả rà soát |
| PREP-03 | Hợp đồng API có schema máy đọc được và mock | SCDC-API-DM-001, PREP-01 | Sáng, Vg / Thái dùng mock | Request/response/lỗi thống nhất; không trả dữ liệu ngoài quyền |
| PREP-04 | Thử nghiệm DM lưu bền/chống trùng/phân trang | PREP-03, lựa chọn công nghệ | Sáng / Vg, Thái quan sát | Các kịch bản mục 4 có bằng chứng, rủi ro được ghi nhận |
| PREP-05 | Thử nghiệm cập nhật, reconnect và thu hồi phiên | PREP-04, thiết kế phiên | Sáng, Vg / Thái | Không trùng/sót dữ liệu; kết nối bị thu hồi đúng ngưỡng đã chốt |
| PREP-06 | Ma trận thiết bị và fixture kiểm thử | DEC-059, AC và ca kiểm thử | Thái / Vg, Sáng | Danh sách trình duyệt/phiên bản, kích thước, dữ liệu và kết quả mong đợi rõ |
| PREP-07 | Ước lượng và lịch đợt tài khoản/DM | PREP-01–06 đủ rõ | Vg, Sáng, Thái | Ngày công, người làm/rà soát, thời gian hướng dẫn và phụ thuộc không trùng quỹ thời gian |

Đây là phân công dự kiến theo vai trò hiện có, chưa phải xác nhận các
thành viên đã nhận việc. Chưa điền ngày công hay ngày phát hành khi chưa
có đầu vào. Gói cộng đồng chi tiết và media tiếp tục theo kế hoạch đợt 2/3.

## 4. Bằng chứng thử nghiệm kỹ thuật cần có

1. Hai người xác thực được, mở cùng một DM khi tạo đồng thời; người thứ
   ba không truy cập được API hoặc kênh cập nhật.
2. Gửi tin lưu bền; rollback không phát sự kiện; mất response sau commit
   và gửi lại đồng thời chỉ tạo một tin.
3. Phân trang không bỏ sót tin khi có giao dịch commit trễ; reconnect
   bù nhiều trang và cập nhật sửa/xóa tin cũ, không dùng cache hết hiệu lực.
4. Worker dừng rồi hoạt động lại; sự kiện lặp/đảo thứ tự không nhân đôi
   hoặc đưa giao diện về phiên bản tin cũ.
5. Thu hồi phiên chặn HTTP và kết nối đang mở theo ngưỡng cần thống
   nhất; chốt cách đo, không tuyên bố thu hồi tức thời khi chưa chứng minh.

Mỗi bằng chứng ghi môi trường, bản thử, dữ liệu, thao tác, kết quả, hạn
chế và người rà soát. Viết kế hoạch hoặc sơ đồ không thay thế chạy thử.

## 5. Thứ tự xử lý tiếp theo

Ưu tiên rà soát các phương án tài khoản và phép đếm/tìm kiếm của DM,
đồng thời dựng prototype từ wireframe. Sau đó chọn giải pháp/transport
và khóa hợp đồng, chuẩn bị fixture và thử nghiệm. Chỉ xác nhận gói
phát triển khi các phụ thuộc trực tiếp được giải quyết; không cần chờ
đặc tả toàn bộ media để rà soát thiết kế DM.

Các nội dung còn mở được tập trung tại OQ-002–OQ-011; bảng này dẫn chiếu
và theo dõi đầu ra, không tạo một nguồn quyết định sản phẩm khác.

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-10-03 | Ghi kết quả phiên tài liệu, điều kiện Đợt 0, gói việc chuẩn bị và bằng chứng còn thiếu. |

[Mục lục hồ sơ](../README.md)

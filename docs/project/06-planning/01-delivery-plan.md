# SCDC — Kế hoạch thực hiện theo đợt

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-PLAN-001 |
| Phiên bản | 0.1 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Kế hoạch khung — chưa có lịch hoặc dự toán được xác nhận |
| Căn cứ | [Project Brief](../01-initiation/01-project-brief.md), [nhân sự](../01-initiation/04-team-and-discovery-plan.md), [khoảng trống yêu cầu](../03-requirements/00-requirements-coverage.md), [phác thảo kỹ thuật](../05-architecture/01-solution-outline.md) |

## 1. Cơ sở và nguyên tắc lập lịch

Ba người Vg, Sáng và Thái có thể tham gia toàn thời gian. Vg kiêm điều
phối, đại diện sản phẩm, kiến trúc, UX/UI, lập trình và hướng dẫn Thái;
Sáng phụ trách backend và tích hợp; Thái phụ trách frontend và kiểm thử.
Thời gian kiêm nhiệm phải lấy từ quỹ thời gian của chính từng người,
không tính như một vị trí bổ sung.

Mục tiêu khoảng ba tháng từ ngày khởi động và mốc 500 triệu đồng là cơ sở
đối chiếu, chưa phải lịch/chi phí cam kết. Ngày khởi động, tốc độ làm
việc, giới hạn media và đơn giá thực tế chưa xác định. Các đợt dưới đây
có thứ tự phụ thuộc, có thể chồng lấp ở phần việc đã đủ đầu vào.

## 2. Đợt bàn giao và điều kiện chuyển tiếp

| Đợt | Kết quả cần có | Chủ trì dự kiến | Phụ thuộc và điều kiện chuyển |
|---|---|---|---|
| 0. Chốt nền | Quy tắc tài khoản tối thiểu, ngoại lệ DM, ma trận quyền cốt lõi; wireframe hai hành trình; hợp đồng API và kiểm thử thử nghiệm lưu/nhận tin. | Vg chủ trì sản phẩm/kiến trúc/UX; Sáng đánh giá backend; Thái rà soát giao diện/kiểm thử. | OQ-002, OQ-004, OQ-005; chưa bắt đầu phát triển tính năng phụ thuộc vào quy tắc còn mở. |
| 1. Nhắn tin riêng | Đăng nhập đủ dùng, tìm người, hội thoại riêng, gửi/nhận/lịch sử, sửa/xóa, lỗi và thử lại; kiểm thử tích hợp và tiêu chí AC-DM. | Sáng và Vg phát triển; Thái frontend/kiểm thử. | Đợt 0 đủ thiết kế; kiểm tra tin đã lưu khi người nhận mở lại, quyền sửa/xóa và gửi lại không trùng. |
| 2. Cộng đồng và phòng | Tạo/tham gia cộng đồng, tìm kiếm/lời mời/chờ duyệt, phòng theo chủ đề, quyền và nhắn tin phòng; tiêu chí AC-COM. | Vg chốt quy tắc/quyền; Sáng backend; Thái frontend/kiểm thử. | Đợt 1 ổn định phần nhắn tin dùng chung; OQ-003/OQ-004 được chốt cho các luồng triển khai. |
| 3. Thoại/video/chia sẻ màn hình | Gọi riêng, phòng thoại, camera và chia sẻ màn hình theo phạm vi SCP-006/007; kiểm thử chất lượng và chi phí. | Vg chủ trì giải pháp; Sáng tích hợp; Thái giao diện/kiểm thử. | OQ-006/OQ-008 và thử nghiệm media; chốt giới hạn/thiết bị trước khi cam kết công sức. |
| 4. Ổn định và phát hành | Kiểm thử hệ thống, phân quyền, tải, sao lưu/khôi phục, xử lý lỗi, tài liệu vận hành và nghiệm thu. | Thái điều phối kiểm thử với Vg; Vg quyết định kỹ thuật/sản phẩm; Sáng sửa lỗi/vận hành. | Đạt điều kiện ở [kế hoạch kiểm thử](../07-quality/01-test-and-acceptance-plan.md); có người trực vận hành và quyết định mở công khai. |

Đợt 1 và 2 là ưu tiên trải nghiệm; đợt 3 vẫn nằm trong phạm vi phiên bản
đầu. Nếu ước lượng vượt mốc ba tháng hoặc ngân sách, Vg phải trình phương
án điều chỉnh thời gian, nguồn lực hoặc phạm vi cho đại diện khách hàng
xác nhận theo quy trình thay đổi. Không tự loại media khỏi phiên bản đầu.

## 3. Bảng công việc để ước lượng

Ước lượng mỗi dòng bằng ngày công còn lại, tách thiết kế, thực hiện, rà
soát, kiểm thử và sửa lỗi. Ghi người làm, người rà soát và phụ thuộc; cập
nhật sau mỗi đợt. Chưa điền ngày công vì đặc tả và năng lực chưa đủ rõ.

| Nhóm việc | Đầu ra cần ước lượng | Người dự kiến | Dữ liệu còn thiếu |
|---|---|---|---|
| Sản phẩm/UX | Chốt quy tắc, luồng, wireframe và đánh giá nội bộ. | Vg, Thái hỗ trợ | OQ-002 đến OQ-007 |
| Tài khoản | Đăng ký, đăng nhập, hồ sơ, phiên, bảo vệ tài khoản và kiểm thử. | Sáng, Vg, Thái | OQ-002 |
| Nhắn tin | Dữ liệu, API, cập nhật thời gian thực, UI, xử lý lỗi và kiểm thử. | Sáng, Vg, Thái | OQ-005 |
| Cộng đồng/quyền | Thành viên, mời/duyệt, phòng, phân quyền và kiểm thử. | Sáng, Vg, Thái | OQ-003, OQ-004 |
| Media | Thử nghiệm, tích hợp, UI và kiểm thử chất lượng. | Vg, Sáng, Thái | OQ-006, OQ-008, OQ-010 |
| Nền tảng/vận hành | Môi trường, triển khai, giám sát, sao lưu và khôi phục. | Vg, Sáng | OQ-007, OQ-010, OQ-011 |
| Chất lượng/phát hành | Bộ kiểm thử, sửa lỗi, nghiệm thu và bàn giao. | Thái, Vg, Sáng | Tiêu chí chất lượng và ma trận trình duyệt |
| Hướng dẫn/đào tạo | Hướng dẫn Thái, rà soát code và tài liệu bàn giao. | Vg, Sáng, Thái | Năng lực hiện tại và số vòng rà soát |

## 4. Cách khóa lịch và chi phí

1. Chốt ngày bắt đầu, số ngày làm việc/tháng, thời gian nghỉ và tỷ lệ
   phân bổ thực tế cho từng người. Tổng tỷ lệ công việc của mỗi người
   không vượt quá 100% trong cùng kỳ.
2. Ước lượng từng nhóm việc sau khi có quy tắc/thiết kế đủ rõ. Ghi khoảng
   ước lượng và rủi ro; giữ riêng công sức hướng dẫn, tích hợp, sửa lỗi,
   kiểm thử và chuẩn bị vận hành.
3. Xếp phụ thuộc thành lịch, xác định đường găng; so với mốc khoảng ba
   tháng và công suất ba người. Xem xét riêng rủi ro media.
4. Tính chi phí nhân sự từ ngày công và đơn giá đã xác nhận, chi phí hạ
   tầng từ cấu hình/mức dùng dự kiến, cộng dự phòng minh bạch. Đối chiếu
   với mốc 500 triệu đồng tại [SCDC-EST-001](../01-initiation/02-budget-and-assumptions.md).
5. Khi có đủ số liệu, lập lịch ngày cụ thể, dự toán điều chỉnh, người
   duyệt và ngưỡng xử lý sai lệch. Cho đến lúc đó, tài liệu này là khung
   triển khai, không phải cam kết ngày phát hành.

## 5. Theo dõi và điều chỉnh

Mỗi đợt ghi phần đã hoàn thành theo tiêu chí chấp nhận, lỗi còn mở, ngày
công thực tế, công việc còn lại và thay đổi phạm vi. Vg tổng hợp tác động
đến mốc bàn giao; các thay đổi ảnh hưởng phạm vi, thời gian hoặc ngân
sách theo [SCDC-PRC-001](../development-process.md).

## 6. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Tạo kế hoạch khung theo đợt, phân công dự kiến và cách cập nhật lịch/chi phí. |

[Mục lục hồ sơ](../README.md)

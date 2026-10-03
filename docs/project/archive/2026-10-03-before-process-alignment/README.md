# SCDC — Hồ sơ dự án

| Thuộc tính | Giá trị |
|---|---|
| Mã hồ sơ | SCDC-PROJECT |
| Phiên bản | 1.24 |
| Cập nhật | 2026-10-03 |
| Giai đoạn | Khởi tạo |

SCDC là ứng dụng giao tiếp trên web dành cho nhóm bạn và cộng đồng, gồm
nhắn tin, thoại/video, chia sẻ màn hình và quản lý cộng đồng.

## Danh mục hồ sơ

| Mã | Tài liệu | Nội dung quản lý | Giai đoạn sử dụng | Trạng thái |
|---|---|---|---|---|
| SCDC-REQ-001 | [Yêu cầu ban đầu](01-initiation/00-customer-request.md) | Nhu cầu, người sử dụng, yêu cầu cấp cao và ràng buộc của khách hàng. | Tiếp nhận nhu cầu, khởi tạo; đầu vào làm rõ nhu cầu. | Đã ghi nhận |
| SCDC-BRF-001 | [Tổng quan dự án — Project Brief](01-initiation/01-project-brief.md) | Mục tiêu, phạm vi phiên bản đầu, kết quả bàn giao và tiêu chí thành công. | Khởi tạo; cơ sở kiểm soát phạm vi các bước sau. | Phạm vi sơ bộ đã thống nhất |
| SCDC-EST-001 | [Dự toán và giả định](01-initiation/02-budget-and-assumptions.md) | Chi phí, nguồn lực, quy mô dự trù và rủi ro. | Đánh giá khả thi sơ bộ; cập nhật khi phân tích, thiết kế và lập kế hoạch. | Cần ước lượng lại theo nhân sự hiện có |
| SCDC-LOG-001 | [Quyết định và vấn đề cần làm rõ](01-initiation/03-discovery-and-decision-log.md) | Các quyết định, nội dung còn mở và trách nhiệm xử lý. | Từ khởi tạo và xuyên suốt dự án. | Đang cập nhật — đã ghi DEC-051–059 ngày 2026-10-03 |
| SCDC-ORG-001 | [Nhân sự và phương án làm rõ nhu cầu](01-initiation/04-team-and-discovery-plan.md) | Thành viên, vai trò và cách làm rõ nhu cầu trong đợt hiện tại. | Khởi tạo; đầu vào kế hoạch nguồn lực và đặc tả. | Đã ghi nhận nhân sự; không khảo sát bên ngoài đợt hiện tại |
| SCDC-PRC-001 | [Quy trình thực hiện dự án](development-process.md) | Giai đoạn công việc, trách nhiệm, đầu ra và điều kiện bàn giao. | Tổ chức thực hiện toàn bộ vòng đời dự án. | Đề xuất áp dụng |
| SCDC-DIS-001 | [Làm rõ người dùng và nhu cầu ưu tiên](02-discovery/01-users-and-needs.md) | Đầu vào, câu hỏi và kết quả làm rõ người dùng, vấn đề và hành trình ưu tiên. | Làm rõ nhu cầu và chuẩn bị đặc tả. | Đầu vào từ đại diện sản phẩm — chưa kiểm chứng bên ngoài |
| SCDC-DIS-002 | [Bộ câu hỏi khảo sát nhu cầu giao tiếp](02-discovery/02-interview-guide.md) | Mục tiêu, câu hỏi và mẫu ghi nhận phỏng vấn người dùng. | Dự phòng nếu sau này khảo sát. | Bản nháp lưu dự phòng — không áp dụng đợt hiện tại |
| SCDC-FR-INDEX-001 | [Phạm vi đặc tả và khoảng trống](03-requirements/00-requirements-coverage.md) | Độ bao phủ phiên bản đầu và yêu cầu chưa đủ đầu vào. | Đặc tả yêu cầu và lập kế hoạch. | Theo dõi khoảng trống và đầu ra Đợt 0 |
| SCDC-FR-DM-001 | [Đặc tả nhắn tin riêng giữa hai người, đợt đầu](03-requirements/01-direct-messaging.md) | Hành trình, quy tắc và tiêu chí chấp nhận dự thảo cho nhắn tin văn bản giữa hai người. | Đặc tả yêu cầu. | Đã bổ sung quy tắc/ngoại lệ — chờ rà soát kỹ thuật và tiêu chí |
| SCDC-FR-COM-001 | [Đặc tả tham gia cộng đồng và phòng theo chủ đề](03-requirements/02-community-join-and-channels.md) | Tham gia, lời mời, quyền phòng, nhắn tin văn bản và tiêu chí chấp nhận dự thảo. | Đặc tả yêu cầu. | Bổ sung mô hình quyền — còn luồng quản lý cộng đồng chưa đủ |
| SCDC-FR-ACC-001 | [Đặc tả tài khoản nền tảng](03-requirements/03-accounts.md) | Đăng ký/đăng nhập dùng email và tên tài khoản; những quy tắc còn mở. | Đặc tả yêu cầu. | Đã chốt định danh/quyền trước xác minh — còn chính sách chi tiết |
| SCDC-FR-MEDIA-001 | [Khung đặc tả thoại, video và chia sẻ màn hình](03-requirements/04-voice-video-screen-sharing.md) | Phạm vi, hành trình, tiêu chí chức năng khung và các quyết định media còn thiếu. | Đặc tả yêu cầu. | Khung — chưa đủ triển khai |
| SCDC-UX-001 | [Luồng trải nghiệm cho nhắn tin, cộng đồng và cuộc gọi](04-ux/01-core-user-flows.md) | Các bước, màn hình và trạng thái giao diện dự kiến. | Thiết kế UX/UI. | Có wireframe văn bản riêng — chưa có prototype được rà soát |
| SCDC-ARC-001 | [Phác thảo giải pháp kỹ thuật](05-architecture/01-solution-outline.md) | Ranh giới dịch vụ, luồng dữ liệu, hợp đồng và các quyết định kỹ thuật còn mở. | Thiết kế kỹ thuật. | Có hợp đồng DM đề xuất — chưa chọn công nghệ/duyệt thiết kế |
| SCDC-PLAN-001 | [Kế hoạch thực hiện theo đợt](06-planning/01-delivery-plan.md) | Thứ tự bàn giao, phụ thuộc, phân công dự kiến và cơ sở cập nhật lịch/chi phí. | Lập kế hoạch. | Có bảng theo dõi Đợt 0 — chưa xác nhận lịch/chi phí |
| SCDC-QA-001 | [Kế hoạch kiểm thử và nghiệm thu](07-quality/01-test-and-acceptance-plan.md) | Phạm vi, cấp kiểm thử, tình huống ưu tiên và mẫu ghi nhận kết quả. | Chuẩn bị kiểm thử/nghiệm thu. | Có ca kiểm thử chi tiết — chưa chạy |
| SCDC-FR-ACL-001 | [Ma trận quyền cốt lõi](03-requirements/05-access-control-matrix.md) | Quyền theo thao tác, vai trò, ngoại lệ cá nhân và thu hồi. | Đặc tả và thiết kế phân quyền. | Đã chốt quy tắc cốt lõi; chi tiết thiết kế còn mở |
| SCDC-UX-DM-001 | [Wireframe tài khoản và DM](04-ux/02-account-dm-wireframes.md) | Bố cục, màn hình và trạng thái gắn tiêu chí chấp nhận. | Thiết kế frontend/prototype. | Wireframe văn bản đề xuất — chờ rà soát |
| SCDC-UX-COM-001 | [Wireframe cộng đồng và quyền](04-ux/03-community-wireframes.md) | Tham gia, phòng, quản lý vai trò và cấu hình xem. | Thiết kế frontend/prototype. | Wireframe văn bản đề xuất — còn luồng chưa chốt |
| SCDC-API-DM-001 | [Hợp đồng và dữ liệu tài khoản/DM](05-architecture/02-account-dm-contracts.md) | API, dữ liệu, lỗi, chống trùng, phiên bản và đồng bộ lịch sử. | Rà soát thiết kế, chuẩn bị mock và thử nghiệm. | Đề xuất kỹ thuật — chưa duyệt/triển khai |
| SCDC-READY-001 | [Theo dõi Đợt 0 và bàn giao phát triển](06-planning/02-development-readiness.md) | Điều kiện sẵn sàng, gói việc, phụ thuộc và bằng chứng còn thiếu. | Chuẩn bị phát triển. | Có đầu vào mới — chưa hoàn tất Đợt 0 |
| SCDC-QA-DM-001 | [Ca kiểm thử tài khoản/DM và quyền](07-quality/02-account-dm-test-cases.md) | Dữ liệu, bước thử, kỳ vọng và truy vết yêu cầu. | Chuẩn bị kiểm thử. | Tất cả chưa chạy |

## Tình trạng dự án

Hồ sơ vẫn ở giai đoạn khởi tạo, đang chi tiết hóa yêu cầu và chuẩn bị
Đợt 0. Có tài liệu thiết kế/kiểm thử không đồng nghĩa đã triển khai,
đã thử nghiệm hoặc đã nghiệm thu. Trạng thái từng điều kiện bàn giao
được theo dõi tại [SCDC-READY-001](06-planning/02-development-readiness.md).

Phạm vi phiên bản đầu: web, tài khoản, DM hai người, cộng đồng/phòng,
phân quyền, thoại/video và chia sẻ màn hình. Nhóm chat riêng ngoài server
và cuộc gọi trong nhóm đó không thuộc phạm vi. Ưu tiên DM văn bản trước,
sau đó cộng đồng; media vẫn thuộc phiên bản đầu. File và xử lý tin riêng
không mong muốn để đợt sau theo các quyết định hiện hành.

Nhân sự toàn thời gian: Vg phụ trách sản phẩm, kiến trúc, UX/UI, lập
trình và hướng dẫn Thái; Sáng phụ trách backend, lập trình cùng Vg;
Thái phụ trách frontend và kiểm thử. Mốc khoảng ba tháng và dự toán ban
đầu 500 triệu đồng cần ước lượng lại; chưa xác định ngày khởi động,
ngày công hoặc lịch bàn giao được xác nhận.

### Cập nhật phiên ngày 2026-10-03

Đại diện sản phẩm đã xác nhận DEC-051–059:

- Trước xác minh email chỉ dùng xác minh/khôi phục, chưa vào ứng dụng.
- Đăng ký bằng email/tên tài khoản/mật khẩu; đăng nhập bằng email hoặc
  tên tài khoản. Tên tài khoản duy nhất, chưa cho đổi; tên hiển thị được
  đổi; email không công khai.
- Tin văn bản tối đa 2.000 ký tự, nhận xuống dòng/emoji, không nhận tin
  trống; sửa chỉ giữ nội dung mới nhất, không cung cấp lịch sử bản cũ.
- Quyền cộng đồng qua vai trò, có ngoại lệ từng người ở phòng. Từ chối
  giữa các vai trò thắng, ngoại lệ cá nhân áp dụng cuối; chủ sở hữu luôn
  xem mọi phòng và là người duy nhất quản lý/gán/thu hồi vai trò.
- Tài khoản/DM có bố cục thích ứng trên desktop và trình duyệt điện thoại.

Đã bổ sung ma trận quyền, wireframe văn bản tài khoản/DM và cộng đồng,
hợp đồng dữ liệu/API DM, ca kiểm thử và bảng điều kiện bàn giao. Các
quyết định sản phẩm đã chốt được tách khỏi phương án kỹ thuật đề xuất.

Chưa hoàn tất Đợt 0: còn chính sách tài khoản chi tiết, phép đếm/tìm kiếm
và lưu giữ tin, ma trận trình duyệt/thiết bị và ngưỡng chất lượng, rà soát
prototype/hợp đồng, lựa chọn công nghệ và bằng chứng thử nghiệm. Cộng
đồng còn luồng tạo/sửa, thêm trực tiếp, từ chối yêu cầu và vòng đời chủ
sở hữu; media còn giới hạn, chất lượng và giải pháp/chi phí.

Đợt này không khảo sát người dùng bên ngoài theo DEC-030. Đầu vào là từ
đại diện sản phẩm; chưa có bằng chứng nghiên cứu bên ngoài. Việc đó không
thay thế kiểm thử và nghiệm thu. OQ-001/OQ-012 tiếp tục mở về người dùng
đại diện, giá trị mong muốn và cách đánh giá.

Hồ sơ hiện hành được quản lý tập trung tại đây. Mỗi tài liệu chịu trách
nhiệm phần ghi trong danh mục; sổ quyết định quản lý DEC/OQ, còn bảng
sẵn sàng quản lý tình trạng đầu ra, tránh tạo nhiều nguồn quyết định.

## Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 1.0 | 2026-09-27 | Thiết lập hồ sơ khởi tạo dự án. |
| 1.1 | 2026-09-27 | Chuẩn hóa cấu trúc và quy ước quản lý hồ sơ. |
| 1.2 | 2026-09-27 | Hợp nhất danh mục hồ sơ và phân định nội dung quản lý của từng tài liệu. |
| 1.3 | 2026-09-27 | Cập nhật phạm vi giao tiếp, ánh xạ tài liệu với quy trình và tình trạng khởi tạo. |
| 1.4 | 2026-09-27 | Bổ sung hồ sơ nhân sự, tình trạng chuẩn bị khảo sát và yêu cầu ước lượng lại nguồn lực. |
| 1.5 | 2026-09-27 | Cập nhật trách nhiệm kiến trúc, lựa chọn công nghệ và lập trình của Vg cùng trách nhiệm backend của Sáng. |
| 1.6 | 2026-09-27 | Ghi nhận phân công UX/UI, frontend, kiểm thử và tình trạng làm rõ ba nhu cầu sử dụng. |
| 1.7 | 2026-09-30 | Bổ sung bản nháp làm rõ người dùng và nhu cầu ưu tiên để chuẩn bị khảo sát. |
| 1.8 | 2026-09-30 | Cập nhật vấn đề trôi tin nhắn/tài liệu, ưu tiên nhắn tin riêng trước và lựa chọn nhiều phòng theo chủ đề. |
| 1.9 | 2026-09-30 | Ghi nhận cách bắt đầu nhắn tin riêng bằng tên tài khoản, không cần kết bạn hoặc cùng cộng đồng. |
| 1.10 | 2026-09-30 | Ghi nhận đợt nhắn tin riêng đầu tiên chỉ hỗ trợ văn bản. |
| 1.11 | 2026-09-30 | Ghi nhận để việc xử lý tin riêng không mong muốn sang đợt sau. |
| 1.12 | 2026-09-30 | Cập nhật tìm người, sửa/xóa và trạng thái DM; tham gia cộng đồng qua mời/tìm kiếm; để gửi file sang đợt sau. |
| 1.13 | 2026-09-30 | Làm rõ sửa/xóa tin, cộng đồng công khai trong tìm kiếm, chế độ tham gia tùy cấu hình và quyền tạo phòng. |
| 1.14 | 2026-09-30 | Làm rõ hiển thị tin đã xóa, thử lại khi gửi lỗi, cách vào cộng đồng và quyền xem phòng. |
| 1.15 | 2026-09-30 | Bổ sung bản nháp đặc tả nhắn tin riêng và tiêu chí chấp nhận đợt đầu. |
| 1.16 | 2026-09-30 | Cập nhật quy tắc tìm người, trạng thái DM, lời mời; bổ sung bản nháp đặc tả tham gia cộng đồng và phòng theo chủ đề. |
| 1.17 | 2026-09-30 | Bổ sung bộ câu hỏi và mẫu ghi nhận khảo sát nhu cầu giao tiếp. |
| 1.18 | 2026-09-30 | Ghi nhận không khảo sát người dùng bên ngoài trong đợt hiện tại; cập nhật trạng thái hồ sơ và phương án làm rõ nhu cầu. |
| 1.19 | 2026-09-30 | Ghi nhận các quyết định tiếp theo; bổ sung bản đồ khoảng trống yêu cầu, luồng UX, phác thảo kỹ thuật, kế hoạch theo đợt và kiểm thử. |
| 1.20 | 2026-09-30 | Bổ sung bản nháp tài khoản và các quyết định về chống tin trùng, lịch sử phòng, quyền xem/gửi. |
| 1.21 | 2026-09-30 | Ghi nhận xác minh/khôi phục tài khoản và các quy tắc tham gia/rời cộng đồng tiếp theo. |
| 1.22 | 2026-09-30 | Bổ sung khung yêu cầu thoại, video và chia sẻ màn hình. |
| 1.23 | 2026-09-30 | Ghi nhận năm quyết định media và cập nhật luồng/kiểm thử khung. |
| 1.24 | 2026-10-03 | Đồng bộ DEC-051–059; bổ sung ma trận quyền, wireframe, hợp đồng DM, ca kiểm thử và bảng theo dõi Đợt 0. |

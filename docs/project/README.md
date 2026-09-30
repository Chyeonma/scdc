# SCDC — Hồ sơ dự án

| Thuộc tính | Giá trị |
|---|---|
| Mã hồ sơ | SCDC-PROJECT |
| Phiên bản | 1.12 |
| Cập nhật | 2026-09-30 |
| Giai đoạn | Khởi tạo |

SCDC là ứng dụng giao tiếp trên web dành cho nhóm bạn và cộng đồng, gồm
nhắn tin, thoại/video, chia sẻ màn hình và quản lý cộng đồng.

## Danh mục hồ sơ

| Mã | Tài liệu | Nội dung quản lý | Giai đoạn sử dụng | Trạng thái |
|---|---|---|---|---|
| SCDC-REQ-001 | [Yêu cầu ban đầu](01-initiation/00-customer-request.md) | Nhu cầu, người sử dụng, yêu cầu cấp cao và ràng buộc của khách hàng. | Tiếp nhận nhu cầu, khởi tạo; đầu vào khảo sát. | Đã ghi nhận |
| SCDC-BRF-001 | [Tổng quan dự án — Project Brief](01-initiation/01-project-brief.md) | Mục tiêu, phạm vi phiên bản đầu, kết quả bàn giao và tiêu chí thành công. | Khởi tạo; cơ sở kiểm soát phạm vi các bước sau. | Phạm vi sơ bộ đã thống nhất |
| SCDC-EST-001 | [Dự toán và giả định](01-initiation/02-budget-and-assumptions.md) | Chi phí, nguồn lực, quy mô dự trù và rủi ro. | Đánh giá khả thi sơ bộ; cập nhật khi phân tích, thiết kế và lập kế hoạch. | Cần ước lượng lại theo nhân sự hiện có |
| SCDC-LOG-001 | [Quyết định và vấn đề cần làm rõ](01-initiation/03-discovery-and-decision-log.md) | Các quyết định, nội dung còn mở và trách nhiệm xử lý. | Từ khởi tạo và xuyên suốt dự án. | Đang cập nhật |
| SCDC-ORG-001 | [Nhân sự và chuẩn bị khảo sát](01-initiation/04-team-and-discovery-plan.md) | Thành viên, vai trò, khả năng tham gia và công việc chuẩn bị khảo sát. | Khởi tạo; đầu vào kế hoạch khảo sát và kế hoạch nguồn lực. | Nhân sự đã ghi nhận; kế hoạch khảo sát đang xây dựng |
| SCDC-PRC-001 | [Quy trình thực hiện dự án](development-process.md) | Giai đoạn công việc, trách nhiệm, đầu ra và điều kiện bàn giao. | Tổ chức thực hiện toàn bộ vòng đời dự án. | Đề xuất áp dụng |
| SCDC-DIS-001 | [Làm rõ người dùng và nhu cầu ưu tiên](02-discovery/01-users-and-needs.md) | Đầu vào, câu hỏi và kết quả làm rõ người dùng, vấn đề và hành trình ưu tiên. | Chuẩn bị khảo sát và khảo sát nhu cầu. | Đã ghi nhận đầu vào từ đại diện sản phẩm — đang làm rõ |

## Tình trạng dự án

Đã có hồ sơ tiếp nhận nhu cầu và phạm vi sơ bộ. Phạm vi giao tiếp gồm các
phòng trong server và hội thoại riêng giữa hai người. Nhóm chat riêng ngoài
server và cuộc gọi trong loại nhóm này không thuộc phiên bản đầu.

Đã xác định ba thành viên toàn thời gian: Vg là trưởng nhóm/đại diện sản
phẩm, phụ trách kiến trúc tổng thể, lựa chọn công nghệ, UX/UI và trực tiếp
lập trình; Sáng phụ trách kỹ thuật backend, trực tiếp lập trình cùng Vg;
Thái phụ trách frontend và kiểm thử, được Vg hướng dẫn. Công sức kiêm nhiệm
và phân công công việc cụ thể cần được xác định trong kế hoạch thực hiện.

Đã ghi nhận ba nhu cầu sử dụng từ đại diện sản phẩm: kết nối với cộng đồng,
tạo cộng đồng và nhắn tin riêng giữa hai người. Ngày 2026-09-30, đại diện
sản phẩm ưu tiên nhắn tin riêng trước rồi tham gia cộng đồng, đồng thời nêu vấn
đề tin nhắn và tài liệu bị trôi khi nhiều chủ đề dùng chung một luồng chat.
Đã chọn tổ chức cộng đồng bằng nhiều phòng theo chủ đề trước. Người dùng
có thể tìm bằng tên tài khoản hoặc tên hiển thị và nhắn riêng ngay, không
cần kết bạn hoặc cùng cộng đồng. Đợt DM đầu hỗ trợ văn bản, sửa/xóa tin
và hiển thị đã gửi/lỗi gửi. Người dùng tham gia cộng đồng qua liên kết
mời hoặc tìm kiếm; gửi file trong phòng và xử lý tin riêng không mong muốn
được để sang đợt sau. Quy tắc chi tiết và kết quả mong muốn của từng
hành trình còn cần làm rõ.

Chưa có người tham gia khảo sát. Nhóm có thể bố trí thời gian linh hoạt;
phương án tiếp cận và lịch khảo sát còn mở trong OQ-013. Khảo sát tiếp theo
cần làm rõ người dùng, hành trình ưu tiên và giá trị sản phẩm (OQ-001, OQ-012).

SCDC-DIS-001 đã ghi nhận câu trả lời ban đầu của đại diện sản phẩm, các
giả định và câu hỏi tiếp theo. Chưa có kết quả khảo sát người dùng bên
ngoài; giai đoạn dự án vẫn là khởi tạo.

Giả định ba kỹ sư có kinh nghiệm trong dự toán ban đầu không còn phù hợp.
Cần đánh giá lại công sức, tiến độ và chi phí theo nguồn lực hiện có;
ngày khởi động dự án và kế hoạch chi tiết chưa được xác lập.

Hồ sơ hiện hành được quản lý tập trung tại đây. Mỗi tài liệu chịu trách nhiệm
cho nội dung được chỉ rõ trong danh mục; các tài liệu khác dẫn chiếu đến nội
dung đó để tránh duy trì nhiều bản không thống nhất.

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

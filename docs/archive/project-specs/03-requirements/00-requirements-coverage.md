# SCDC — Phạm vi đặc tả và khoảng trống cần xử lý

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-INDEX-001 |
| Phiên bản | 0.5 |
| Cập nhật | 2026-10-03 |
| Trạng thái | Bản nháp theo dõi độ bao phủ — chưa xác nhận yêu cầu |
| Căn cứ | [Project Brief](../01-initiation/01-project-brief.md), [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Mục đích

Theo dõi các phần của phiên bản đầu đã có đặc tả kiểm chứng được và các phần
chưa đủ đầu vào. Bản nháp DM và cộng đồng chưa tương đương với việc toàn bộ
phạm vi sản phẩm đã được đặc tả hoặc có thể nghiệm thu.

| Phạm vi | Đầu ra hiện có | Khoảng trống chính | Trạng thái |
|---|---|---|---|
| SCP-001 Web | DEC-059; [wireframe tài khoản/DM](../04-ux/02-account-dm-wireframes.md) | Đã chốt desktop và trình duyệt điện thoại cho tài khoản/DM; còn ma trận trình duyệt/phiên bản/thiết bị, tiếp cận và hiệu năng (OQ-007). | Một phần, chưa chốt ngưỡng |
| SCP-002 Tài khoản | [SCDC-FR-ACC-001](03-accounts.md) | Đã chốt bộ định danh/hồ sơ, quyền trước xác minh và khôi phục; có ACC-P01–05 để rà soát. Còn chính sách mật khẩu, chuẩn hóa, thời hạn, phiên và chống lạm dụng (OQ-002). | Quy tắc cốt lõi đã chốt; chi tiết còn mở |
| SCP-003 Cộng đồng | [SCDC-FR-COM-001](02-community-join-and-channels.md), [wireframe](../04-ux/03-community-wireframes.md) | Còn tạo/sửa cộng đồng, vai trò thêm trực tiếp, từ chối/hủy yêu cầu, tham gia lại và chuyển chủ sở hữu (OQ-003). | Một phần, bản nháp |
| SCP-004 Phân quyền | [SCDC-FR-ACL-001](05-access-control-matrix.md) | Đã chốt vai trò + ngoại lệ cá nhân, quyền chủ sở hữu, thứ tự xung đột và quản lý vai trò; còn dữ liệu/cấu hình, giới hạn và thu hồi đồng thời (OQ-004/OQ-007). | Quy tắc cốt lõi đã chốt; thiết kế chưa duyệt |
| SCP-005 Nhắn tin | [DM](01-direct-messaging.md), [cộng đồng](02-community-join-and-channels.md), [hợp đồng DM](../05-architecture/02-account-dm-contracts.md) | Đã chốt 2.000 ký tự và chỉ giữ bản sửa mới nhất; có thiết kế chống trùng/đồng bộ. Còn phép đếm, tìm kiếm, lưu giữ và kiểm chứng thiết kế (OQ-005/OQ-008/OQ-011). | Đã chi tiết hóa; còn rà soát và thử nghiệm |
| SCP-006 Thoại | [Khung media](04-voice-video-screen-sharing.md) | Đã có hành trình/tiêu chí khung; còn luồng gọi riêng/phòng thoại, số người/phòng, quyền tham gia, mất kết nối và chỉ tiêu chất lượng (OQ-006). | Khung, chưa đủ triển khai |
| SCP-007 Video/chia sẻ màn hình | [Khung media](04-voice-video-screen-sharing.md) | Đã có tiêu chí chức năng khung; còn quy tắc camera/micro/chia sẻ, quyền thiết bị, giới hạn và tình huống lỗi (OQ-006). | Khung, chưa đủ triển khai |
| SCP-008 Phát hành | Project Brief, [quy trình](../development-process.md) | Điều kiện vận hành, quản trị, sao lưu/khôi phục, hỗ trợ, đo chất lượng và quyết định mở công khai (OQ-007, OQ-010, OQ-011). | Chưa đặc tả |

Đầu ra chuẩn bị và điều kiện còn thiếu theo gói tài khoản/DM được theo
dõi tại [SCDC-READY-001](../06-planning/02-development-readiness.md).
Chưa có kết quả kiểm thử sản phẩm để đánh dấu yêu cầu đã đạt.

## 2. Thứ tự hoàn thiện đề xuất

1. Chốt các ngoại lệ DM và nhắn tin phòng, tài khoản tối thiểu, cùng ma trận
   quyền cần thiết cho hai hành trình ưu tiên. Đây là điều kiện để thiết
   kế API, dữ liệu và kiểm thử tích hợp có thể xác nhận.
2. Chốt tạo/quản lý cộng đồng, mời, duyệt, rời và quyền phòng. Hoàn thiện
   wireframe và tiêu chí chấp nhận liên quan.
3. Đặc tả thoại/video/chia sẻ màn hình và chỉ tiêu chất lượng; thực hiện
   thử nghiệm kỹ thuật media trước khi cam kết lịch và chi phí cuối cùng.
4. Chốt yêu cầu phi chức năng, vận hành và điều kiện phát hành công khai.

Thứ tự trên là đề xuất lập kế hoạch, không thay đổi phạm vi phiên bản đầu
hoặc quyết định thay mặt đại diện sản phẩm. Mỗi phần chỉ chuyển sang “đã
xác nhận” khi quy tắc, tiêu chí chấp nhận và đầu mối xác nhận đã rõ theo
[SCDC-PRC-001](../development-process.md).

## 3. Lịch sử phiên bản

| Phiên bản | Ngày | Nội dung |
|---|---|---|
| 0.1 | 2026-09-30 | Lập bản đồ độ bao phủ đặc tả phiên bản đầu và thứ tự xử lý khoảng trống. |
| 0.2 | 2026-09-30 | Cập nhật tài khoản, lịch sử phòng, quyền gửi/xem và tránh tin trùng. |
| 0.3 | 2026-09-30 | Cập nhật xác minh/khôi phục tài khoản và các cách tham gia/rời cộng đồng. |
| 0.4 | 2026-09-30 | Liên kết khung đặc tả media và ghi rõ các khoảng trống kỹ thuật/chất lượng. |
| 0.5 | 2026-10-03 | Cập nhật độ bao phủ sau DEC-051–059 và liên kết các đầu ra chuẩn bị phát triển. |

[Mục lục hồ sơ](../README.md)

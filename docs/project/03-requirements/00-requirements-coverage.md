# SCDC — Phạm vi đặc tả và khoảng trống cần xử lý

| Thuộc tính | Giá trị |
|---|---|
| Mã tài liệu | SCDC-FR-INDEX-001 |
| Phiên bản | 0.4 |
| Cập nhật | 2026-09-30 |
| Trạng thái | Bản nháp theo dõi độ bao phủ — chưa xác nhận yêu cầu |
| Căn cứ | [Project Brief](../01-initiation/01-project-brief.md), [sổ quyết định](../01-initiation/03-discovery-and-decision-log.md) |

## 1. Mục đích

Theo dõi các phần của phiên bản đầu đã có đặc tả kiểm chứng được và các phần
chưa đủ đầu vào. Bản nháp DM và cộng đồng chưa tương đương với việc toàn bộ
phạm vi sản phẩm đã được đặc tả hoặc có thể nghiệm thu.

| Phạm vi | Đầu ra hiện có | Khoảng trống chính | Trạng thái |
|---|---|---|---|
| SCP-001 Web | Project Brief | Ma trận trình duyệt, kích thước màn hình, hỗ trợ truy cập và tiêu chí hiệu năng (OQ-007). | Chưa đặc tả |
| SCP-002 Tài khoản | [SCDC-FR-ACC-001](03-accounts.md) | Đã chọn email/tên tài khoản, xác minh trước khi nhắn và đặt lại qua email; còn chi tiết xác minh/khôi phục, phiên, hồ sơ và chống lạm dụng (OQ-002). | Một phần, bản nháp |
| SCP-003 Cộng đồng | [SCDC-FR-COM-001](02-community-join-and-channels.md) | Đã chọn vào cộng đồng riêng tư qua mời/được thêm, thu hồi mời và tự rời; còn tạo/sửa cộng đồng, vai trò thêm trực tiếp, từ chối yêu cầu và hệ quả khi rời (OQ-003). | Một phần, bản nháp |
| SCP-004 Phân quyền | [SCDC-FR-COM-001](02-community-join-and-channels.md) | Đã chốt một số quyền xem/gửi/quản lý; còn ma trận đầy đủ, cấp và thu hồi quyền, hành vi quyền xung đột (OQ-004). | Một phần, bản nháp |
| SCP-005 Nhắn tin | [DM](01-direct-messaging.md), [cộng đồng](02-community-join-and-channels.md) | Đã chốt kết quả thử lại không trùng và lịch sử phòng cho thành viên mới; còn giới hạn nội dung, thứ tự, lưu giữ và các ngoại lệ OQ-005. | Một phần, bản nháp |
| SCP-006 Thoại | [Khung media](04-voice-video-screen-sharing.md) | Đã có hành trình/tiêu chí khung; còn luồng gọi riêng/phòng thoại, số người/phòng, quyền tham gia, mất kết nối và chỉ tiêu chất lượng (OQ-006). | Khung, chưa đủ triển khai |
| SCP-007 Video/chia sẻ màn hình | [Khung media](04-voice-video-screen-sharing.md) | Đã có tiêu chí chức năng khung; còn quy tắc camera/micro/chia sẻ, quyền thiết bị, giới hạn và tình huống lỗi (OQ-006). | Khung, chưa đủ triển khai |
| SCP-008 Phát hành | Project Brief, [quy trình](../development-process.md) | Điều kiện vận hành, quản trị, sao lưu/khôi phục, hỗ trợ, đo chất lượng và quyết định mở công khai (OQ-007, OQ-010, OQ-011). | Chưa đặc tả |

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

[Mục lục hồ sơ](../README.md)

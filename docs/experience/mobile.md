# SCDC — Ứng dụng mobile

SCDC sẽ có ứng dụng mobile. Nhóm đã thống nhất làm ngày 2026-11-01; nền tảng, công nghệ, chức năng và đợt bàn giao đang được xác định.

**Trạng thái:** chưa có code, bản build hay nghiệm thu nào. Việc WebClient chạy trên trình duyệt điện thoại là chuyện khác, xem [chất lượng](../system/quality.md#quality-targets).

## Phân công

Vg phụ trách phần chung: phạm vi, kiến trúc client và API, khung ứng dụng, điều hướng và quy ước giao diện. Sau khi chọn nền tảng, mỗi người làm phần mobile thuộc mảng của mình theo [phân công](../project/planning.md#team). Ví dụ: Sáng phụ trách thông báo, nhưng không phụ trách toàn bộ app mobile.

## Cần thiết kế

1. **Nền tảng:** hệ điều hành, phiên bản, thiết bị và cách phát hành app.
2. **Chức năng đợt đầu** của từng mảng. Media trên mobile cần quyết định riêng.
3. **Khung app:** điều hướng, trạng thái tải, trống, lỗi và quy ước giao diện chung.
4. **Đăng nhập:** nơi lưu thông tin đăng nhập, đăng xuất, thu hồi phiên và khôi phục khi mở lại app.
5. **Vòng đời app:** chạy nền và chạy chính, kết nối lại, quyền thông báo và thiết bị, mở đúng màn hình từ thông báo hoặc link.
6. **Kỹ thuật:** công nghệ, tổ chức code, build, kiểm thử và người chuẩn bị bản phát hành.
7. **Chất lượng:** danh sách thiết bị, tiêu chí, kiểm thử tích hợp và công sức còn lại.

## Kế hoạch

Theo gói [EXT-MOBILE-01](../project/planning.md#extension-packages): Vg thống nhất đầu vào chung, mỗi người tự tạo nhiệm vụ Trello cho phần của mình. Một chức năng chỉ được đưa vào mốc bàn giao khi hành vi, hợp đồng API và cách kiểm thử đã rõ.

Các tiêu chí đã có cho trình duyệt và Media trên desktop không tự áp dụng cho app mobile.

Câu hỏi mở: [OQ-014](../project/open-questions.md#oq-014).

---

Nguồn: REQ-014, SCP-009, [DEC-171](../records/decisions/README.md#dec-171), [DEC-173](../records/decisions/README.md#dec-173).

# Nhắn tin riêng (DM) — Yêu cầu và Tiêu chí chấp nhận

> Tài liệu gốc: [SCDC-FR-DM-001](../../archive/project-specs/03-requirements/01-direct-messaging.md)

## 1. Hành trình chính

1. Người dùng tìm người nhận bằng **một phần tên tài khoản hoặc tên hiển thị**.
2. Chọn đúng tài khoản → mở hội thoại riêng. **Không cần kết bạn hoặc cùng cộng đồng.**
3. Gửi tin nhắn văn bản. Khi hệ thống lưu → hiển thị "Đã gửi"; lỗi → hiện trạng thái lỗi.
4. Người nhận mở hội thoại và xem tin, kể cả tin đã gửi khi vắng mặt.
5. Người gửi **sửa** tin bất cứ lúc nào → hiện dấu "Đã sửa" cho cả hai.
6. Người gửi **xóa** tin → hiện "Tin nhắn đã bị xóa" cho cả hai.
7. Nếu gửi lỗi → người gửi bấm "Thử lại". Cùng thao tác chỉ tạo **một tin**.

## 2. Quy tắc nghiệp vụ

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| DM-001 | Tìm theo một phần tên tài khoản hoặc tên hiển thị | DEC-016, DEC-019 |
| DM-002 | Nhắn ngay, không cần kết bạn | DEC-016 |
| DM-003 | Đợt đầu: tin nhắn văn bản | DEC-017 |
| DM-004 | Sửa tin bất cứ lúc nào; hiện "Đã sửa" | DEC-020 |
| DM-005 | Xóa cho cả hai; hiện "Tin nhắn đã bị xóa" | DEC-020 |
| DM-006 | "Đã gửi" = hệ thống đã lưu; lỗi → thử lại | DEC-021 |
| DM-009 | Thử lại không tạo tin trùng | DEC-037 |
| DM-010 | Phải xác minh email trước khi gửi | DEC-041 |
| DM-011 | Sửa chỉ giữ bản mới nhất | DEC-052 |
| DM-012 | Tối đa 2.000 ký tự; cho xuống dòng/emoji; từ chối trống | DEC-053 |

## 3. Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-DM-01 | Tìm và chọn người nhận | Mở hội thoại riêng không cần kết bạn |
| AC-DM-02 | Gửi tin thành công | Hiện "Đã gửi"; người nhận thấy khi mở |
| AC-DM-03 | Đóng rồi mở lại hội thoại | Lịch sử còn hiệu lực được hiển thị |
| AC-DM-04 | Sửa tin cũ | Nội dung mới + "Đã sửa" cho cả hai |
| AC-DM-05 | Xóa tin | "Tin nhắn đã bị xóa" cho cả hai |
| AC-DM-06 | Gửi thất bại | Báo lỗi; có nút thử lại |
| AC-DM-07 | A gửi khi B offline; B mở lại | B thấy tin |
| AC-DM-08 | Thử lại sau kết quả không rõ | Chỉ 1 tin trong hội thoại |
| AC-DM-09 | Chưa xác minh email thử gửi | Từ chối; chỉ dẫn xác minh |
| AC-DM-10 | Sửa nhiều lần | Chỉ nội dung mới nhất + "Đã sửa" |
| AC-DM-11 | Tin 2.000 / 2.001 ký tự / trống | Nhận ≤2.000; từ chối vượt/trống |
| AC-DM-12 | Người thứ ba gọi API đọc/gửi | Không được truy cập |
| AC-DM-13 | Hai phía mở DM đồng thời | Cùng một hội thoại |
| AC-DM-14 | Sửa/xóa đồng thời | Không ghi đè âm thầm; báo xung đột |
| AC-DM-15 | B mất mạng; A sửa/xóa; B kết nối lại | B thấy nội dung hiện hành |

## 4. Ngoại lệ

| Tình huống | Xử lý |
|---|---|
| Không tìm thấy người | Danh sách rỗng, thông báo rõ |
| Trùng tên hiển thị | Hiển thị thêm tên tài khoản |
| Chọn chính mình | Không tạo DM một người |
| Mở DM đã có | Trả cùng hội thoại |
| Mất phản hồi gửi | Giữ tin tạm; thử lại cùng mã |
| Sửa tin đã xóa | Từ chối |

---

📎 Đầy đủ: [Đặc tả gốc](../../archive/project-specs/03-requirements/01-direct-messaging.md)

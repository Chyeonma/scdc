# Tài khoản — Yêu cầu và Tiêu chí chấp nhận

> Tài liệu gốc: [SCDC-FR-ACC-001](../../archive/project-specs/03-requirements/03-accounts.md)

## 1. Hành trình chính

1. Người dùng đăng ký bằng **email, tên tài khoản và mật khẩu**.
2. Hệ thống gửi email xác minh. Tài khoản ở trạng thái "chờ xác minh".
3. Người dùng mở liên kết xác minh → email được xác nhận.
4. Đăng nhập bằng **email hoặc tên tài khoản** cùng mật khẩu → vào ứng dụng.
5. Trong ứng dụng, người dùng có thể đổi **tên hiển thị** (tên tài khoản không đổi được).

## 2. Quy tắc nghiệp vụ

| Mã | Quy tắc | Căn cứ |
|---|---|---|
| ACC-001 | Đăng ký thu thập email, tên tài khoản và mật khẩu | DEC-054 |
| ACC-002 | Đăng nhập nhận email hoặc tên tài khoản cùng mật khẩu | DEC-054 |
| ACC-003 | Tên hiển thị được đổi; tên tài khoản duy nhất, chưa cho đổi; email không công khai | DEC-054 |
| ACC-005 | Phải xác minh email trước khi gửi tin | DEC-041 |
| ACC-006 | Có luồng đặt lại mật khẩu qua email | DEC-042 |
| ACC-007 | Chưa xác minh → chỉ dùng xác minh/khôi phục, không vào app | DEC-051 |

## 3. Tiêu chí chấp nhận

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| AC-ACC-01 | Đăng ký hợp lệ | Tài khoản ở trạng thái chờ xác minh |
| AC-ACC-02 | Đăng nhập bằng email và bằng tên tài khoản | Cả hai cách truy cập cùng một tài khoản |
| AC-ACC-03 | Hai tài khoản cùng tên hiển thị | Người tìm chọn đúng qua định danh phân biệt |
| AC-ACC-04 | Chưa đăng nhập cố truy cập | Yêu cầu đăng nhập, không trả dữ liệu riêng tư |
| AC-ACC-05 | Chưa xác minh thử gửi tin; rồi xác minh và thử lại | Trước: từ chối; Sau: cho phép |
| AC-ACC-06 | Đặt lại mật khẩu thành công | Đăng nhập được bằng mật khẩu mới |
| AC-ACC-07 | Chưa xác minh gọi API ứng dụng | Không vào được; có đường xác minh |
| AC-ACC-08 | Xem/sửa hồ sơ | Đổi được tên hiển thị; không đổi được tên tài khoản |
| AC-ACC-09 | Hai đăng ký cùng tên tài khoản đồng thời | Chỉ một được tạo; yêu cầu kia nhận lỗi trùng |
| AC-ACC-10 | Chưa xác minh hoàn tất đặt lại mật khẩu | Mật khẩu mới OK nhưng vẫn phải xác minh email |

## 4. Đề xuất kỹ thuật (chưa duyệt)

| Mã | Nội dung | Trạng thái |
|---|---|---|
| ACC-P01 | Tên tài khoản 3–32 ký tự ASCII; tên hiển thị 1–64 Unicode | Đề xuất |
| ACC-P02 | Liên kết xác minh/khôi phục: 1 lần, hạn 30 phút | Đề xuất |
| ACC-P03 | Gửi lại sau 60s; không tiết lộ email tồn tại | Đề xuất |
| ACC-P04 | Phiên tối đa 30 ngày; đăng xuất tất cả thiết bị | Đề xuất |
| ACC-P05 | Chưa cho đổi email đợt đầu | Đề xuất |

---

📎 Đầy đủ: [Đặc tả gốc](../../archive/project-specs/03-requirements/03-accounts.md)

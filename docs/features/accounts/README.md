# SCDC — Tài khoản

Người dùng đăng ký bằng email, xác minh, đăng nhập, quản lý hồ sơ, mật khẩu và các phiên đăng nhập. Module Identity sở hữu toàn bộ dữ liệu này.

**Trạng thái:** Identity đã chạy host/database riêng, quản lý tài khoản, phiên, permit và thu hồi qua Redis/fence. E4-01 đã tích hợp worker email, gửi lại xác minh và thời gian chờ vào Identity hiện tại ([hồ sơ](../../records/verification/authentication/e4-01-2026-11-26.md)). Dev mặc định vẫn dùng token Development; bật và kiểm chứng Gmail thật thuộc S3-04. Giao diện mở link email còn cần nối. v1 tiếp tục hoàn thiện vòng đời tài khoản.

**Người phụ trách:** Vg; Thái phụ trách phần gửi email (xem [phân công](../../project/planning.md#team)).

## Các trang

| Trang | Nội dung | Mã |
|---|---|---|
| [Đăng ký và xác minh](registration-verification.md) | Form, API, transaction, tiêu chí chấp nhận | UC-ACC-01 đến 03; ACC-001, 008, 009 |
| [Đăng nhập và phiên](sessions.md) | Làm mới token, đăng xuất, thu hồi phiên trên HTTP và realtime | UC-ACC-04, 09 đến 11; ACC-002, 010, 011 |
| [Khôi phục và đổi mật khẩu](password-recovery.md) | Đặt lại mật khẩu, không cho dùng lại mật khẩu cũ | UC-ACC-05, 06, 08; ACC-006, 014, 016 |
| [Hồ sơ](profile.md) | Thông tin riêng và công khai, kiểm tra dữ liệu | UC-ACC-07; ACC-003, 004, 013 |
| [Trạng thái và khóa tài khoản](account-state.md) | Khóa, mở khóa và công cụ vận hành | UC-ACC-12; ACC-005, 007, 015 |
| [Liên kết và gửi email](email-links.md) | Cơ chế dùng chung cho link xác minh và đặt lại mật khẩu, worker gửi thư | ACC-012 |

Mỗi trang gồm quy tắc, luồng, giao diện, API, dữ liệu, tiêu chí chấp nhận và hiện trạng.

## Hiện trạng và bằng chứng

- Các bảng ghi nền MVP là snapshot lịch sử. Hiện trạng host/database và email cập nhật theo S3-02/S3-03 và E4-01; xem hồ sơ tương ứng.
- Kết quả kiểm thử được ghi trong hồ sơ của từng gói, kèm commit và môi trường. Danh sách ca kiểm thử hay fixture không tự có nghĩa là đã đạt.
- [Hồ sơ tích hợp](../../records/verification/messaging/merge-2026-10-13.md) không thay cho nghiệm thu toàn bộ tài khoản hoặc gửi email thật.
- Gói MVP-ID xem [hồ sơ MVP](../../releases/mvp.md#packages). Khi tách service ở v1, transaction và guard dùng chung phải được thiết kế và kiểm thử lại.

<a id="use-case-rules"></a>

## Bảng truy vết quy tắc

| Quy tắc | Use case | Định nghĩa tại |
|---|---|---|
| ACC-001 | UC-ACC-01 | [Đăng ký và xác minh](registration-verification.md#rules) |
| ACC-002 | UC-ACC-04 | [Đăng nhập và phiên](sessions.md#rules) |
| ACC-003 | UC-ACC-07 | [Hồ sơ](profile.md#rules) |
| ACC-004 | UC-ACC-01, 07, 10, 12 | [Hồ sơ](profile.md#rules) |
| ACC-005 | UC-ACC-02, 04 | [Trạng thái tài khoản](account-state.md#rules) |
| ACC-006 | UC-ACC-05, 06 | [Khôi phục mật khẩu](password-recovery.md#rules) |
| ACC-007 | UC-ACC-01, 02, 04, 05, 06 | [Trạng thái tài khoản](account-state.md#rules) |
| ACC-008 | UC-ACC-01, 07 | [Đăng ký và xác minh](registration-verification.md#rules) |
| ACC-009 | UC-ACC-01, 04, 05 | [Đăng ký và xác minh](registration-verification.md#rules) |
| ACC-010 | UC-ACC-01, 04, 06, 08 | [Đăng nhập và phiên](sessions.md#rules) |
| ACC-011 | UC-ACC-04, 06, 08, 09, 10, 11 | [Đăng nhập và phiên](sessions.md#rules) |
| ACC-012 | UC-ACC-01, 02, 03, 05, 06 | [Liên kết email](email-links.md#rules) |
| ACC-013 | UC-ACC-07 | [Hồ sơ](profile.md#rules) |
| ACC-014 | UC-ACC-05, 06 | [Khôi phục mật khẩu](password-recovery.md#rules) |
| ACC-015 | UC-ACC-12 | [Trạng thái tài khoản](account-state.md#rules) |
| ACC-016 | UC-ACC-06, 08 | [Khôi phục mật khẩu](password-recovery.md#rules) |

---

Nguồn: REQ-010, SCP-002, [DEC-116](../../records/decisions/README.md#dec-116), [DEC-171](../../records/decisions/README.md#dec-171).

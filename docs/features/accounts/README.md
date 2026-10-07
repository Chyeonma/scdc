# SCDC — Tài khoản

Phạm vi REQ-010/SCP-002; Identity sở hữu danh tính, hồ sơ, mật khẩu, email và phiên. [MVP](../../releases/mvp.md) chọn luồng nền; [v1](../../releases/v1.md) hoàn thiện vòng đời tài khoản.

| Cần tìm | Đọc |
|---|---|
| Trạng thái và phần còn thiếu | [Trạng thái](status.md) |
| Quy tắc, hành trình và kiểm chứng | [Đặc tả](specs/README.md) |
| API, dữ liệu và cơ chế kỹ thuật | [Thiết kế](design/README.md) |
| Chọn gói và ghi bằng chứng | [Bàn giao](delivery/README.md) |

## Quy ước duy trì

`specs/` giữ hành vi mục tiêu và AC/TC; `design/` giữ API, dữ liệu và cách thực hiện. `status.md` tập trung đối chiếu implementation, khoảng trống và liên kết bằng chứng. Phạm vi MVP/v1 nằm trong hồ sơ release; mỗi gói được thực hiện giữ kế hoạch và bằng chứng gắn commit trong `delivery/<gói>/`. Chỉ tạo hồ sơ gói khi đã có scope; danh mục test hoặc fixture không tự ghi nhận kết quả đạt. Giữ nguyên mã truy vết khi cập nhật tài liệu.

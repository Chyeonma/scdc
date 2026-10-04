# Mẫu hồ sơ sự cố SCDC

Mẫu trống, không phải sự cố thực tế. Dùng cùng [runbook](../operations-runbook.md) và [phân loại ảnh hưởng](../release-operations.md#defects). Chỉ ghi ID/mã lỗi/bằng chứng đã lọc; không mật khẩu, token, DM, audio/video hoặc giá trị secrets.

## 1. Nhận diện và tiếp nhận

| Trường | Giá trị |
|---|---|
| Mã sự cố / người tạo / trạng thái | Chưa điền / chưa tiếp nhận |
| Môi trường / build-schema / dependency | Chưa điền |
| Mức ảnh hưởng / hành trình và phạm vi | Chưa phân loại |
| Mốc đầu tiên có ảnh hưởng / độ chắc chắn | Chưa xác định |
| Phát hiện / xác nhận / người nhận xử lý | Chưa điền |
| Người điều phối / người kỹ thuật / người quyết định | Chưa xác nhận |
| Người thay thế / lần cập nhật tiếp theo | Chưa xác nhận |
| Tín hiệu/trace/error/scope ID | Chưa điền |
| Người dùng bị ảnh hưởng / dữ liệu có nguy cơ | Chưa đánh giá — dùng số lượng và ID hạn chế truy cập |

## 2. Nhật ký xử lý

| Thời điểm / người | Nhận định hoặc hành động | Runbook / thay đổi / ID | Kết quả và bằng chứng | Quyết định / người duyệt nếu cần |
|---|---|---|---|---|
| Chưa điền | Chưa thực thi | Chưa điền | Chưa có | Chưa xác nhận |

Ghi cả thao tác thất bại hoặc kết quả chưa rõ, thời gian chờ người/dependency và phạm vi đã cô lập. Chỉ báo “đã phục hồi” khi kiểm tra hành trình và quyền thực tế đạt, không chỉ khi process restart hoặc health trả 200.

## 3. Phục hồi và mở lại

| Nội dung | Kết quả |
|---|---|
| Cách cô lập/giảm ảnh hưởng / thời điểm | Chưa thực thi |
| Rollback/forward fix/restore / build đích và compatibility | Chưa quyết định |
| Dữ liệu đã commit, thao tác chưa rõ và kết quả đối soát | Chưa đánh giá |
| Xóa/thu hồi/floor/epoch/media fencing nếu liên quan | Chưa kiểm chứng |
| Smoke/chất lượng/quyền sau xử lý và bằng chứng | Chưa thực thi |
| Recovery point/RPO/RTO nếu restore | Chưa đo — đính kèm [mẫu restore](release-record.md#restore-record) |
| Scope còn đóng, dữ liệu thiếu và hạn chế | Chưa đánh giá |
| Người cho phép mở lại / thời điểm | Chưa xác nhận |
| Tổng thời gian ảnh hưởng/phát hiện/chờ/xử lý/theo dõi | Chưa đo |

## 4. Kết thúc và ngăn lặp

| Nội dung | Kết quả |
|---|---|
| Nguyên nhân đã kiểm chứng / giả thuyết còn mở | Chưa xác định |
| Thay đổi nào khắc phục / kiểm tra lại | Chưa điền |
| Hành động ngăn lặp / người / hạn / bằng chứng hoàn thành | Chưa giao |
| Nội dung cần cập nhật trong runbook/kiểm thử/cảnh báo | Chưa tổng hợp |
| Người nhận công việc còn lại / người xác nhận kết thúc | Chưa xác nhận |

Mốc xử lý và lần cập nhật là ghi nhận thực tế, không tạo SLA 24/7. Việc thông báo cho người dùng/đầu mối được quyết định theo quy trình đã chọn; mẫu này không tự gửi hoặc ủy quyền gửi thông báo.

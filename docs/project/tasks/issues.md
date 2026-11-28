# Các vấn đề phát sinh

File này giữ nội dung các thẻ `ISSUE-…`, đồng bộ một chiều bằng cùng công cụ với task STEP. Thẻ mới vào cột **Issue (Vấn đề phát sinh)**, cấu hình qua `TRELLO_ISSUE_LIST_ID`; cột, người nhận và hạn của thẻ đã có tiếp tục quản lý trên Trello. Không tạo nhãn STEP cho file này.

## ISSUE-001 · CI chạy test quá lâu cho thay đổi nhỏ

- Phát hiện: 2026-11-28, khi đưa riêng một tài liệu Markdown lên main qua [PR #57](https://github.com/Chyeonma/scdc/pull/57).
- Hiện tượng: Backend chạy 6 phút 25 giây, riêng test 5 phút 21 giây; chuyển database 1 phút 7 giây, Frontend 9 giây. Tác giả chỉ sửa tài liệu vẫn phải chờ toàn bộ trước khi merge. [Lần chạy làm bằng chứng](https://github.com/Chyeonma/scdc/actions/runs/46132372510).
- Nguyên nhân: workflow chưa chọn job theo file thay đổi; check tổng hợp CI bắt buộc cả ba job thành công ở mọi PR và main push.
- Ảnh hưởng: phản hồi chậm cho thay đổi tài liệu/giao diện, sử dụng runner cho các kiểm tra không liên quan; chạy lại sau merge tiếp tục phát sinh công việc dư.
- Hướng xử lý: phân loại bằng Git diff đầy đủ; Markdown thuần chỉ chạy kiểm tra nhẹ; frontend chạy test/build frontend; backend chạy toàn bộ backend hiện có; database/migrator chạy thêm chuyển dữ liệu. Hợp đồng dùng chung, fixture, workflow và đường dẫn không xác định chạy đầy đủ. Giữ check CI bắt buộc và từ chối khi job cần thiết thất bại, bị hủy hoặc bị bỏ qua sai.
- Xong khi: kiểm thử bộ phân loại và cổng CI đạt; chứng minh PR tài liệu không chạy các job nặng, thay đổi backend vẫn cần backend và lỗi bắt buộc vẫn chặn merge. Đo thời gian trước/sau trên GitHub, giữ lệnh chạy đầy đủ thủ công.
- Kết quả: đang triển khai và kiểm chứng; chưa tự chuyển thẻ sang Hoàn tất. Hướng dẫn cập nhật tại [CI](../../guides/continuous-integration.md).

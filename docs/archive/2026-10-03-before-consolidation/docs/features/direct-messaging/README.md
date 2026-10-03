# Tính năng: Nhắn tin riêng (Direct Messaging)

Module nhắn tin 1-1 giữa hai người dùng. Đợt đầu hỗ trợ **tin nhắn văn bản**.

## Tài liệu trong thư mục này

| File | Nội dung | Đọc khi |
|---|---|---|
| [requirements.md](requirements.md) | Usecase, quy tắc, tiêu chí chấp nhận, ngoại lệ | Bắt đầu làm tính năng |
| [wireframes.md](wireframes.md) | Phác thảo giao diện chat | Làm Frontend |
| [api-contracts.md](api-contracts.md) | API endpoints, dữ liệu, cơ chế chống trùng | Làm Backend / tích hợp |
| [test-cases.md](test-cases.md) | Kịch bản kiểm thử chi tiết | QA / Tester |

## Quyết định đã chốt (tóm tắt)

- **DEC-016:** Tìm người và nhắn ngay, không cần kết bạn.
- **DEC-017:** Đợt đầu chỉ hỗ trợ tin văn bản.
- **DEC-020:** Người gửi sửa/xóa tin của mình.
- **DEC-037:** Thử lại không tạo tin trùng (Idempotency).
- **DEC-052:** Sửa tin chỉ giữ bản mới nhất, không lưu lịch sử.
- **DEC-053:** Tối đa 2.000 ký tự, cho emoji/xuống dòng, không nhận tin trống.

## Trạng thái

✅ Đã chi tiết hóa đặc tả và API contracts. Còn rà soát phép đếm ký tự và thử nghiệm kỹ thuật.

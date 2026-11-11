# SCDC — Quy ước viết tài liệu

Tài liệu viết cho người chưa thuộc dự án: thành viên mới, người chấm, người review. Đọc đoạn đầu của một trang phải hiểu trang đó nói về gì.

## Cấu trúc một trang

1. **Mở đầu 1–3 câu thường:** chủ đề là gì, phục vụ ai.
2. **Trạng thái:** đã có, đang làm, hay mới thiết kế. Một dòng.
3. **Phần đã chốt:** quy tắc, luồng, thiết kế. Chỉ viết điều đã quyết định.
4. **Còn mở:** gom mọi điểm chưa quyết định vào một mục cuối trang, hoặc vào [câu hỏi mở](../project/open-questions.md).
5. **Nguồn:** một dòng cuối liệt kê mã yêu cầu và quyết định (REQ, DEC…).

## Câu chữ

- Mỗi câu một ý, có chủ ngữ. Tránh câu kiểu ghi chú ("Thời hạn chưa chốt.").
- Hạn chế dấu "/". Viết rõ "và", "hoặc". Giữ "/" cho đường dẫn, API và cặp quen thuộc như UI/UX.
- Dùng tiếng Việt khi có từ thông dụng: bằng chứng (proof), phạm vi (scope), người sở hữu (owner). Thuật ngữ kỹ thuật không có từ tương đương (outbox, epoch, lease) giữ nguyên và giải thích tại [thuật ngữ](../glossary.md).
- Không đặt mã DEC/REQ giữa câu nếu không cần. Đặt cuối câu trong ngoặc, hoặc ở dòng "Nguồn".
- Không chép lý do và các phương án của quyết định vào trang chính; ghi kết luận và link tới [hồ sơ quyết định](../records/decisions/README.md).
- Phân công người chỉ ghi tại [kế hoạch](../project/planning.md#team). Trang tính năng chỉ ghi tên người phụ trách chính.

## Giữ ổn định

- Không xóa hoặc đổi `<a id="...">` và mã UC, AC, TC, ACL…: nhiều trang và hồ sơ đang link tới.
- Chạy `make docs-check` sau khi sửa.

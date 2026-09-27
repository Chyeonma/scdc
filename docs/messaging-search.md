# P8-T01: tìm kiếm lịch sử tin nhắn

`GET /api/v1/spaces/{spaceId}/messages/search` yêu cầu bearer token. Tham số:

| Tên | Quy tắc |
|---|---|
| `q` | Nội dung, sau trim dài 2–120 ký tự, tối đa 12 từ; tùy chọn nếu có tác giả hoặc khoảng ngày. |
| `authorUserId` | UUID tác giả; tùy chọn. |
| `from`, `to` | Cả hai hoặc không có; ISO-8601 có timezone, `from` bao gồm và `to` không bao gồm; khoảng tối đa 366 ngày. |
| `limit` | 1–50, mặc định 20. |
| `beforeSequence` | Cursor sequence dương từ `nextBeforeSequence` của trang trước. |

Phản hồi `{ items: MessageDto[], hasMore, nextBeforeSequence }`, mới nhất trước. Search bao gồm tin thường và thread reply, loại tin hệ thống và tin đã xóa mềm. Quyền đọc space được xác minh trước truy vấn; space không tồn tại, ngoài quyền hoặc đã bị xóa cùng trả 404. Kết quả chỉ thuộc `spaceId` đã yêu cầu. FE debounce 300 ms, hủy request cũ khi đổi query/filter, có trạng thái chờ/rỗng/lỗi, phân trang và dùng luồng jump-to-message để mở tin cũ chưa nằm trong bộ nhớ trình duyệt.

Nội dung dùng `search_vector` generated column và GIN index `ix_messages_search` đã có, với `plainto_tsquery('simple', q)`. Các từ trong `q` được tìm theo kiểu AND, không phải substring/prefix. PostgreSQL `simple` giữ dấu: ví dụ `Chào` tìm được `Chào`, còn `chao` không tìm được `Chào`. Bài test tích hợp xác nhận hành vi này. Chưa thêm `unaccent` hoặc index mới vì cần quyết định chính sách chuẩn hóa dấu và chi phí lưu/index riêng; không cần migration DB cho task này. Cột generated cập nhật khi sửa nội dung; `deleted_at IS NULL` được áp trước khi trả kết quả.

# Đặc tả Module Messaging

## 1. Trách nhiệm nghiệp vụ

Module Messaging phụ trách toàn bộ lõi trò chuyện (Chat Engine), bao gồm tin nhắn trực tiếp hai người (Direct Message - DM), tin nhắn nhóm (Group Chat) và tin nhắn kênh cộng đồng (Channel Message).

Module sở hữu:
- Lưu trữ và truy vấn dòng thời gian tin nhắn.
- Phân trang dữ liệu hiệu năng cao theo con trỏ thời gian / ID (Cursor-based Pagination).
- Tương tác tin nhắn: Sửa, xóa, thả reaction, ghim tin nhắn (Pin).
- Phát sự kiện thời gian thực qua SignalR Hub kèm Redis Backplane.

---

## 2. Các thực thể chính (Entities)

Schema CSDL: `messaging`

| Bảng | Trách nhiệm |
|---|---|
| `messaging.spaces` | Không gian trò chuyện (DM, Group, Channel). Dùng làm định danh phân vùng và nhóm SignalR. |
| `messaging.space_members` | Thành viên tham gia không gian trò chuyện và thời điểm đọc tin gần nhất. |
| `messaging.messages` | Bảng lưu tin nhắn chính (`id`, `space_id`, `sender_id`, `content`, `sequence_no`, `created_at`). |
| `messaging.message_edits` | Bản ghi trạng thái sửa tin (chỉ lưu cờ `is_edited` và thời gian sửa gần nhất theo quyết định thiết kế). |
| `messaging.message_reactions` | Cảm xúc người dùng thả vào tin nhắn (emoji code, user_id). |
| `messaging.message_attachments` | Danh sách tệp đính kèm trong tin nhắn. |
| `messaging.message_pins` | Danh sách tin nhắn được ghim trong không gian chat. |

---

## 3. Thuật toán phân trang con trỏ (Cursor-based Pagination)

Hệ thống không sử dụng `OFFSET / LIMIT` truyền thống do độ trễ truy vấn tăng tuyến tính khi số lượng tin nhắn đạt hàng triệu bản ghi. Thay vào đó, truy vấn sử dụng con trỏ cặp `(sequence_no, id)` kết hợp Composite Index:

```sql
SELECT id, space_id, sender_id, content, sequence_no, created_at
FROM messaging.messages
WHERE space_id = @spaceId
  AND sequence_no < @cursorSequenceNo
ORDER BY sequence_no DESC
LIMIT @limit;
```

**Ưu điểm:**
- Thời gian phản hồi truy vấn ổn định dưới 10ms bất kể độ sâu của trang dữ liệu.
- Tránh hiện tượng lặp hoặc sót tin nhắn khi có tin nhắn mới được gửi vào phòng trong lúc người dùng đang cuộn trang.

---

## 4. Chống trùng lặp tin nhắn (Idempotency)

Mỗi yêu cầu gửi tin từ client phải đính kèm `clientMessageId` (UUID v4).
- Server lưu cặp `(space_id, client_message_id)` với ràng buộc `UNIQUE`.
- Trong trường hợp mạng chập chờn khiến client gửi lại yêu cầu (retry), server sẽ nhận diện được khóa trùng và trả về bản ghi tin nhắn đã tạo trước đó mà không nhân bản dữ liệu.

---

## 5. Trạng thái phát triển hiện tại

- Schema PostgreSQL: Đã hoàn thiện trong `database/postgres/schema.sql`.
- Module Descriptor: Đã đăng ký trong `services/Modules/Messaging/MessagingModule.cs`.
- Nghiệp vụ cốt lõi: Đang triển khai CRUD tin nhắn, phân trang Cursor và SignalR Hub.

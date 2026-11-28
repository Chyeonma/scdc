# Chat nhóm

Trong mục nhắn tin của WebClient, chọn **Chat nhóm**, nhấn **+**, nhập tên nhóm và chọn từ 2 đến 9 tài khoản. Người tạo là chủ nhóm. Thành viên có thể mở nhóm, tải lịch sử, gửi tin văn bản theo thời gian thực, sửa hoặc xóa tin mình đã gửi. Danh sách thành viên được xác lập khi tạo nhóm.

| API | Hợp đồng |
|---|---|
| `POST /api/v1/group-conversations` | Nhận `id` UUID v4 do client tạo, `name` dài 1–100 đơn vị UTF-16 sau khi bỏ khoảng trắng hai đầu và `memberIds` gồm 2–9 ID khác nhau ngoài người tạo. Gửi lại cùng ID, tên và tập thành viên trả về nhóm hiện tại. Thay đổi nội dung với cùng ID trả `409 OPERATION_CONFLICT`. |
| `GET /api/v1/group-conversations?limit=20&cursor=…` | Trả `items` và `nextCursor` cho các nhóm mà người gọi là thành viên đang hoạt động. `limit` từ 1 đến 50. Cursor lấy từ phản hồi trước. |
| `GET /api/v1/group-conversations/{id}/messages` | Dùng hợp đồng [lịch sử và kết nối lại](synchronization.md), với cursor riêng cho chat nhóm. |
| `POST /api/v1/group-conversations/{id}/messages` | Dùng `clientMessageId` và `content` theo [vòng đời tin](message-lifecycle.md). Có thể gửi thêm `replyToMessageId` của tin đang hiển thị trong cùng nhóm. Khi thử lại, giữ nguyên ID và tin được trả lời. |
| `PATCH` và `DELETE /api/v1/group-conversations/{id}/messages/{messageId}` | Chỉ tác giả có quyền sửa hoặc xóa, với `expectedVersion` theo [vòng đời tin](message-lifecycle.md). |
| `GET`, `PUT`, `DELETE /api/v1/group-conversations/{id}/messages/{messageId}/like` | Đọc, đặt hoặc bỏ lượt thích của người gọi. Phản hồi gồm `count` và `likedByMe`; lịch sử trả hai trường này trong `likes` của tin đang hiển thị. |

Mỗi nhóm trả `id`, `name`, `ownerUserId`, `memberCount`, `createdAt`, `lastSequence` và `lastActivityAt`. Tin nhóm cập nhật qua `SubscribeConversation` và `MessageChanged` tại `/hubs/chat`. API và Hub trả `RESOURCE_NOT_FOUND` cho người ngoài nhóm.

Thành viên đang mở nhóm gửi `ReportGroupTyping` và nhận `GroupTyping` để hiển thị trạng thái đang nhập tạm thời. `GroupLikesChanged` thông báo cho WebClient đọc lại lượt thích của tin. Rê chuột hoặc dùng Tab để mở thao tác trên tin. Trên điện thoại, bấm **…**.

Messaging dùng hợp đồng Identity hiện có để kiểm tra phiên của người tạo và xin permit cho từng người được thêm. Messaging kiểm tra tất cả permit trong cùng một transaction trước khi ghi vào `messaging.spaces`, `messaging.group_conversations` và `messaging.space_members`.

## Chạy thử

Lần đầu, dùng [hướng dẫn khởi chạy Compose](../../guides/development.md#setup) để chuẩn bị hạ tầng và tài khoản. Khi stack đã chạy, build và cập nhật hai container chứa thay đổi từ thư mục gốc của repo.

```sh
docker compose up -d --build --no-deps chat-service web-client
docker compose logs --tail=100 chat-service
```

Mở `http://localhost:3000`, đăng nhập bằng tài khoản đã xác minh và tạo nhóm với hai tài khoản đã xác minh khác. Mở nhóm từ một tài khoản thành viên khác để kiểm tra tin nhắn thời gian thực. Readiness của Messaging có tại `http://localhost:5026/api/v1/health/access`.

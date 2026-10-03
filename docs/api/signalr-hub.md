# Đặc tả SignalR Realtime Hub

Tài liệu này quy định giao thức kết nối thời gian thực, cơ chế quản lý phòng (Group) và danh sách các sự kiện SignalR trong SCDC.

---

## 1. Kết nối và Xác thực

- **Hub Endpoint:** `/hubs/chat`
- **Xác thực:** SignalR nhận JWT Token thông qua query string `access_token` khi khởi tạo kết nối WebSocket (chuẩn của thư viện `@microsoft/signalr`).
- **Mở rộng ngang (Horizontal Scaling):** Hub sử dụng **Redis Backplane** (`AddStackExchangeRedis`) để đồng bộ sự kiện giữa nhiều server node khác nhau.

---

## 2. Quản lý phân vùng kết nối (Space Groups)

Khi client mở một cuộc hội thoại (DM hoặc Channel), client gửi yêu cầu tham gia Group tương ứng trên Hub:
- Tên Group SignalR tương ứng với `spaceId` (UUID của Space).
- Khi người dùng gửi tin hoặc tương tác, server phát sự kiện tới nhóm:
  `Clients.Group(spaceId).SendAsync(...)`

---

## 3. Danh sách sự kiện Realtime (Events)

### 3.1. Phía Server phát tới Client (`Client-bound Events`)

| Tên sự kiện | Payload | Mô tả |
|---|---|---|
| `ReceiveMessage` | `MessageDto` | Tin nhắn mới vừa được gửi vào space. |
| `MessageEdited` | `{ messageId, content, editedAt }` | Tin nhắn đã được chỉnh sửa nội dung. |
| `MessageDeleted` | `{ messageId, spaceId }` | Tin nhắn đã bị xóa. |
| `ReactionAdded` | `{ messageId, userId, emoji }` | Có thành viên thả reaction vào tin nhắn. |
| `ReactionRemoved` | `{ messageId, userId, emoji }` | Thành viên gỡ reaction khỏi tin nhắn. |
| `UserTyping` | `{ spaceId, userId, username }` | Người dùng đang gõ phím. |
| `UserPresenceChanged` | `{ userId, status, lastSeenAt }` | Trạng thái trực tuyến (online/offline/idle). |

### 3.2. Phía Client gọi lên Server (`Server-bound Methods`)

| Tên phương thức | Tham số | Mô tả |
|---|---|---|
| `JoinSpace` | `Guid spaceId` | Tham gia lắng nghe sự kiện của không gian chat. |
| `LeaveSpace` | `Guid spaceId` | Rời khỏi nhóm lắng nghe của không gian chat. |
| `SendTypingIndicator` | `Guid spaceId` | Bắn tín hiệu đang nhập tin nhắn. |

---

## 4. Cơ chế phục hồi kết nối (Reconnection Strategy)

Phía Client (React WebClient) kích hoạt tính năng tự động kết nối lại của SignalR:
```typescript
const connection = new HubConnectionBuilder()
  .withUrl("/hubs/chat", { accessTokenFactory: () => getAccessToken() })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
  .build();
```
Sau khi kết nối lại thành công (`onreconnected`), client thực hiện truy vấn HTTP để lấy các tin nhắn bị thiếu trong khoảng thời gian mất mạng (bù dữ liệu theo con trỏ `sequence_no`).

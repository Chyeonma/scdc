# ChatService Minimum API

Trạng thái: Draft 0.1

Phạm vi: MVP cho xác thực, server, text channel và nhắn tin thời gian thực.

Tài liệu này là API contract cho phiên bản đầu tiên của ChatService. Các tính năng như role tùy chỉnh, lời mời, attachment, reaction, thread, voice/video và read receipt chưa thuộc phạm vi này.

## 1. Quy ước chung

### Base URL

```text
/api/v1
```

Ví dụ khi chạy local:

```text
http://localhost:5026/api/v1
```

SignalR Hub không nằm dưới prefix `/api/v1`:

```text
/hubs/chat
```

### Định dạng dữ liệu

- Request và response dùng `application/json`.
- ID của resource là UUID và được biểu diễn bằng chuỗi.
- Thời gian dùng UTC theo ISO 8601, ví dụ `2026-08-23T08:30:00Z`.
- Tên thuộc tính JSON dùng `camelCase`.
- Thuộc tính không có giá trị có thể là `null`; thuộc tính không được hỗ trợ không nên gửi lên.

### Xác thực

Các endpoint được đánh dấu **Authenticated** yêu cầu access token:

```http
Authorization: Bearer <access-token>
```

Access token là JWT có thời gian sống ngắn. Refresh token được dùng để lấy access token mới và phải được rotate sau mỗi lần refresh.

Các claim JWT tối thiểu:

| Claim | Ý nghĩa |
|---|---|
| `sub` | ID của user |
| `name` | Username |
| `jti` | ID duy nhất của access token |
| `iat` | Thời điểm phát hành |
| `exp` | Thời điểm hết hạn |

Quyền trong từng server không được lấy trực tiếp từ JWT vì membership có thể thay đổi trong khi token còn hiệu lực.

### Mã trạng thái chung

| Status | Khi sử dụng |
|---|---|
| `200 OK` | Đọc hoặc cập nhật thành công |
| `201 Created` | Tạo resource thành công |
| `204 No Content` | Xóa, logout hoặc thao tác không cần response body |
| `400 Bad Request` | JSON hoặc dữ liệu đầu vào không hợp lệ |
| `401 Unauthorized` | Thiếu token, token sai hoặc hết hạn |
| `403 Forbidden` | Đã đăng nhập nhưng không có quyền |
| `404 Not Found` | Resource không tồn tại hoặc user không được phép biết resource tồn tại |
| `409 Conflict` | Trùng username/email hoặc trạng thái resource xung đột |
| `429 Too Many Requests` | Vượt quá rate limit |
| `500 Internal Server Error` | Lỗi không được dự kiến ở server |

### Error response

Lỗi dùng `application/problem+json` theo `ProblemDetails`.

```json
{
  "type": "https://scdc.dev/problems/validation-error",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "The request contains invalid fields.",
  "instance": "/api/v1/auth/register",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
  "errors": {
    "username": ["Username must contain between 3 and 32 characters."],
    "password": ["Password does not meet the required policy."]
  }
}
```

Client không nên dùng `title` hoặc `detail` để quyết định logic. Khi cần xử lý theo loại lỗi, client dùng `type`, `status` và tên field trong `errors`.

## 2. Resource model

### User

```json
{
  "id": "7f574071-3f68-4c3f-a14f-44729b4183c7",
  "username": "mikalz",
  "displayName": "Mikal",
  "createdAt": "2026-08-23T08:30:00Z"
}
```

Email chỉ xuất hiện trong response của chính user, không nằm trong public user model.

### CurrentUser

```json
{
  "id": "7f574071-3f68-4c3f-a14f-44729b4183c7",
  "email": "mikal@example.com",
  "username": "mikalz",
  "displayName": "Mikal",
  "createdAt": "2026-08-23T08:30:00Z"
}
```

### Server

`role` là quyền của user hiện tại trong server đó.

```json
{
  "id": "2d330320-e907-4022-b413-b55d1f47b8c9",
  "name": "SCDC Community",
  "ownerId": "7f574071-3f68-4c3f-a14f-44729b4183c7",
  "role": "owner",
  "createdAt": "2026-08-23T09:00:00Z"
}
```

Role của MVP:

- `owner`: chủ server, có thể thêm thành viên và tạo channel.
- `member`: có thể đọc channel và gửi message.

### Channel

```json
{
  "id": "2fe16cb2-9a20-4cf1-ac7d-f91a157ebf8e",
  "serverId": "2d330320-e907-4022-b413-b55d1f47b8c9",
  "name": "general",
  "createdAt": "2026-08-23T09:05:00Z"
}
```

MVP chỉ hỗ trợ text channel.

### Message

```json
{
  "id": "2b85c3be-d9f1-49ad-b88f-e646839e5464",
  "channelId": "2fe16cb2-9a20-4cf1-ac7d-f91a157ebf8e",
  "author": {
    "id": "7f574071-3f68-4c3f-a14f-44729b4183c7",
    "username": "mikalz",
    "displayName": "Mikal"
  },
  "content": "Hello, world!",
  "createdAt": "2026-08-23T09:10:00Z",
  "editedAt": null
}
```

Message bị xóa được soft-delete trong database nhưng không xuất hiện trong API lấy lịch sử của MVP.

## 3. Danh sách endpoint

| Method | Endpoint | Auth | Mục đích |
|---|---|---:|---|
| `GET` | `/health` | No | Kiểm tra tiến trình API đang hoạt động |
| `POST` | `/auth/register` | No | Đăng ký và đăng nhập ngay |
| `POST` | `/auth/login` | No | Đăng nhập |
| `POST` | `/auth/refresh` | No | Rotate refresh token |
| `POST` | `/auth/logout` | No | Thu hồi refresh token |
| `GET` | `/users/me` | Yes | Lấy thông tin user hiện tại |
| `PATCH` | `/users/me` | Yes | Sửa display name |
| `GET` | `/servers` | Yes | Liệt kê server đã tham gia |
| `POST` | `/servers` | Yes | Tạo server |
| `GET` | `/servers/{serverId}` | Yes | Lấy server |
| `POST` | `/servers/{serverId}/members` | Owner | Thêm thành viên bằng username |
| `DELETE` | `/servers/{serverId}/members/me` | Member | Rời server |
| `GET` | `/servers/{serverId}/channels` | Member | Liệt kê channel |
| `POST` | `/servers/{serverId}/channels` | Owner | Tạo text channel |
| `GET` | `/channels/{channelId}/messages` | Member | Lấy lịch sử message |
| `POST` | `/channels/{channelId}/messages` | Member | Gửi message |
| `PATCH` | `/messages/{messageId}` | Author | Sửa message |
| `DELETE` | `/messages/{messageId}` | Author | Xóa message |

Tất cả đường dẫn trong bảng được ghép sau `/api/v1`.

### Health

```http
GET /api/v1/health
```

Response `200 OK`:

```json
{
  "status": "healthy",
  "timestamp": "2026-08-26T01:16:44.2491125Z"
}
```

Endpoint này chỉ xác nhận tiến trình API đang phản hồi. Khi PostgreSQL được thêm vào, health check cho database sẽ được cấu hình riêng.

## 4. Authentication API

### Register

```http
POST /api/v1/auth/register
Content-Type: application/json
```

```json
{
  "email": "mikal@example.com",
  "username": "mikalz",
  "displayName": "Mikal",
  "password": "correct horse battery staple"
}
```

Validation tối thiểu:

- `email`: email hợp lệ, tối đa 254 ký tự và duy nhất sau khi normalize.
- `username`: 3–32 ký tự, chỉ gồm chữ cái Latin, chữ số, `_` và `.`, duy nhất không phân biệt hoa thường.
- `displayName`: 1–64 ký tự.
- `password`: 8–128 ký tự; policy chính xác do Identity cấu hình.

Response `201 Created`:

```json
{
  "user": {
    "id": "7f574071-3f68-4c3f-a14f-44729b4183c7",
    "email": "mikal@example.com",
    "username": "mikalz",
    "displayName": "Mikal",
    "createdAt": "2026-08-23T08:30:00Z"
  },
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAt": "2026-08-23T08:45:00Z",
  "refreshToken": "4b8d03...",
  "refreshTokenExpiresAt": "2026-09-22T08:30:00Z"
}
```

Lỗi đặc thù:

- `409`: email hoặc username đã tồn tại.

### Login

```http
POST /api/v1/auth/login
Content-Type: application/json
```

```json
{
  "login": "mikalz",
  "password": "correct horse battery staple"
}
```

`login` chấp nhận username hoặc email.

Response `200 OK` có cùng schema với response của Register.

Lỗi đặc thù:

- `401`: thông tin đăng nhập không hợp lệ. Response không tiết lộ username/email có tồn tại hay không.
- `429`: đăng nhập sai quá nhiều lần.

### Refresh token

```http
POST /api/v1/auth/refresh
Content-Type: application/json
```

```json
{
  "refreshToken": "4b8d03..."
}
```

Response `200 OK`:

```json
{
  "accessToken": "eyJhbGciOi...",
  "accessTokenExpiresAt": "2026-08-23T09:00:00Z",
  "refreshToken": "new-rotated-token...",
  "refreshTokenExpiresAt": "2026-09-22T08:45:00Z"
}
```

Refresh token cũ bị thu hồi ngay khi refresh thành công. Server chỉ lưu hash của refresh token.

Lỗi đặc thù:

- `401`: token không tồn tại, hết hạn, bị thu hồi hoặc đã được sử dụng.

### Logout

```http
POST /api/v1/auth/logout
Content-Type: application/json
```

```json
{
  "refreshToken": "new-rotated-token..."
}
```

Response: `204 No Content`.

Logout có tính idempotent: gửi lại token đã bị thu hồi vẫn trả về `204`. Access token hiện tại tiếp tục hợp lệ đến khi hết hạn; vì vậy access token nên có thời gian sống ngắn.

Đối với web browser, phiên bản production nên truyền refresh token bằng cookie `Secure`, `HttpOnly`, `SameSite` phù hợp thay vì cho JavaScript lưu token. Nếu áp dụng cookie, request/response của Refresh và Logout sẽ được điều chỉnh nhưng ý nghĩa endpoint không đổi.

## 5. User API

### Get current user

**Authenticated**

```http
GET /api/v1/users/me
Authorization: Bearer <access-token>
```

Response `200 OK`: một `CurrentUser`.

### Update current user

**Authenticated**

```http
PATCH /api/v1/users/me
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "displayName": "Mikal Nguyen"
}
```

Response `200 OK`: `CurrentUser` sau khi cập nhật.

Username và email chưa được phép sửa trong MVP.

## 6. Server và membership API

### List joined servers

**Authenticated**

```http
GET /api/v1/servers
Authorization: Bearer <access-token>
```

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "2d330320-e907-4022-b413-b55d1f47b8c9",
      "name": "SCDC Community",
      "ownerId": "7f574071-3f68-4c3f-a14f-44729b4183c7",
      "role": "owner",
      "createdAt": "2026-08-23T09:00:00Z"
    }
  ]
}
```

### Create server

**Authenticated**

```http
POST /api/v1/servers
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "name": "SCDC Community"
}
```

Validation:

- `name`: 2–100 ký tự sau khi trim.

Response `201 Created`: một `Server` và header:

```http
Location: /api/v1/servers/2d330320-e907-4022-b413-b55d1f47b8c9
```

User tạo server tự động trở thành `owner`. Có thể tạo channel `general` tự động trong cùng transaction.

### Get server

**Member**

```http
GET /api/v1/servers/{serverId}
Authorization: Bearer <access-token>
```

Response `200 OK`: một `Server`.

### Add member

**Owner**

Đây là cơ chế membership đơn giản cho MVP. Invite link sẽ thay thế hoặc bổ sung endpoint này ở phiên bản sau.

```http
POST /api/v1/servers/{serverId}/members
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "username": "another_user"
}
```

Response `201 Created`:

```json
{
  "user": {
    "id": "35f15a67-c02d-4079-bbb4-b2030ba95727",
    "username": "another_user",
    "displayName": "Another User",
    "createdAt": "2026-08-23T09:20:00Z"
  },
  "role": "member",
  "joinedAt": "2026-08-23T09:25:00Z"
}
```

Lỗi đặc thù:

- `404`: server hoặc username không tồn tại.
- `409`: user đã là thành viên.

### Leave server

**Member**

```http
DELETE /api/v1/servers/{serverId}/members/me
Authorization: Bearer <access-token>
```

Response: `204 No Content`.

Owner không thể rời server trong MVP. Owner phải xóa server hoặc chuyển quyền sở hữu khi các API đó được bổ sung.

## 7. Channel API

### List channels

**Member**

```http
GET /api/v1/servers/{serverId}/channels
Authorization: Bearer <access-token>
```

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "2fe16cb2-9a20-4cf1-ac7d-f91a157ebf8e",
      "serverId": "2d330320-e907-4022-b413-b55d1f47b8c9",
      "name": "general",
      "createdAt": "2026-08-23T09:05:00Z"
    }
  ]
}
```

### Create channel

**Owner**

```http
POST /api/v1/servers/{serverId}/channels
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "name": "backend"
}
```

Validation:

- `name`: 2–100 ký tự.
- Chỉ gồm chữ cái thường, chữ số và dấu `-`.
- Không trùng trong cùng server.

Response `201 Created`: một `Channel`.

Lỗi đặc thù:

- `409`: tên channel đã tồn tại trong server.

## 8. Message API

### Get message history

**Member**

```http
GET /api/v1/channels/{channelId}/messages?before={cursor}&limit=50
Authorization: Bearer <access-token>
```

Query parameters:

| Parameter | Bắt buộc | Mô tả |
|---|---:|---|
| `before` | No | Cursor opaque trả về từ lần gọi trước; bỏ qua để lấy message mới nhất |
| `limit` | No | Mặc định `50`, nhỏ nhất `1`, lớn nhất `100` |

Response `200 OK`:

```json
{
  "items": [
    {
      "id": "2b85c3be-d9f1-49ad-b88f-e646839e5464",
      "channelId": "2fe16cb2-9a20-4cf1-ac7d-f91a157ebf8e",
      "author": {
        "id": "7f574071-3f68-4c3f-a14f-44729b4183c7",
        "username": "mikalz",
        "displayName": "Mikal"
      },
      "content": "Hello, world!",
      "createdAt": "2026-08-23T09:10:00Z",
      "editedAt": null
    }
  ],
  "nextCursor": "MjAyNi0wOC0yM1QwOToxMDowMFp8MmI4NWMzYmU...",
  "hasMore": true
}
```

Quy tắc phân trang:

- `items` được sắp xếp mới nhất trước (`createdAt DESC`, sau đó `id DESC`).
- `nextCursor` là cursor để lấy trang cũ hơn; client xem nó như chuỗi opaque.
- `nextCursor` là `null` và `hasMore` là `false` khi không còn dữ liệu.
- Không dùng page number vì message mới có thể được thêm liên tục.

### Send message

**Member**

```http
POST /api/v1/channels/{channelId}/messages
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "content": "Hello, world!"
}
```

Validation:

- `content`: 1–2.000 ký tự sau khi kiểm tra chuỗi chỉ gồm whitespace.
- Server lưu nội dung gốc; client chịu trách nhiệm render an toàn và không thực thi HTML tùy ý.

Response `201 Created`: một `Message`.

Sau khi database transaction thành công, server phát sự kiện SignalR `MessageCreated` đến group của channel.

### Edit message

**Author**

```http
PATCH /api/v1/messages/{messageId}
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "content": "Updated message"
}
```

Response `200 OK`: `Message` sau khi sửa, với `editedAt` khác `null`.

Sau khi cập nhật thành công, server phát `MessageUpdated`.

### Delete message

**Author**

```http
DELETE /api/v1/messages/{messageId}
Authorization: Bearer <access-token>
```

Response: `204 No Content`.

Xóa có tính idempotent đối với author. Sau khi xóa thành công lần đầu, server phát `MessageDeleted`.

## 9. SignalR contract

### Kết nối

```text
/hubs/chat
```

Client dùng access token JWT khi tạo SignalR connection. Với ASP.NET Core SignalR client, cấu hình token qua `AccessTokenProvider`/`accessTokenFactory`; transport có thể truyền token bằng query string khi WebSocket không hỗ trợ header tùy chỉnh.

Connection không được xác thực phải bị từ chối. Khi access token hết hạn, client lấy token mới rồi reconnect.

### Client gọi Hub

#### SubscribeChannel

```text
SubscribeChannel(channelId: string): Task
```

Server phải kiểm tra user đang là member của server chứa channel trước khi thêm connection vào group.

#### UnsubscribeChannel

```text
UnsubscribeChannel(channelId: string): Task
```

SignalR group không tồn tại bền vững qua reconnect. Client phải subscribe lại các channel cần thiết sau khi reconnect thành công.

### Server phát đến client

#### MessageCreated

Payload: một `Message`.

```text
MessageCreated(message: Message)
```

#### MessageUpdated

Payload: một `Message`.

```text
MessageUpdated(message: Message)
```

#### MessageDeleted

```text
MessageDeleted(payload: {
  messageId: string,
  channelId: string,
  deletedAt: string
})
```

Client nên coi sự kiện realtime là thông báo cập nhật giao diện. REST API và database vẫn là nguồn dữ liệu chính khi reconnect hoặc đồng bộ lại lịch sử.

## 10. Quy tắc authorization

| Hành động | Quyền tối thiểu |
|---|---|
| Xem server và channel | Member của server |
| Xem lịch sử message | Member của server chứa channel |
| Subscribe SignalR channel | Member của server chứa channel |
| Gửi message | Member của server chứa channel |
| Sửa/xóa message | Author của message |
| Tạo channel | Owner của server |
| Thêm member | Owner của server |
| Rời server | Member nhưng không phải owner |

Mọi quyền phải được kiểm tra phía server. Client gửi `serverId`, `channelId`, `messageId` hoặc role không phải bằng chứng authorization.

## 11. Yêu cầu phi chức năng tối thiểu

- Log có structured fields như `traceId`, `userId`, `channelId`; không log password, access token hoặc refresh token.
- Rate-limit Register, Login, Refresh và Send message.
- Access token đề xuất sống 15 phút; refresh token đề xuất sống 30 ngày.
- Refresh token được tạo bằng secure random, chỉ lưu dạng hash và rotate sau mỗi lần dùng.
- Mọi endpoint production chỉ phục vụ qua HTTPS.
- Giao dịch tạo/sửa message phải hoàn tất trước khi phát SignalR event.
- CORS chỉ cho phép origin đã cấu hình; không dùng wildcard cùng credentials.
- Endpoint list message cần index database theo `(ChannelId, CreatedAt, Id)`.
- API không trả stack trace hoặc chi tiết exception nội bộ cho client.

## 12. Ngoài phạm vi MVP

Các API sau sẽ được thiết kế sau khi vertical slice auth + chat hoạt động ổn định:

- Xác minh email và quên/reset password.
- Xóa account và quản lý nhiều session.
- Invite link và duyệt yêu cầu tham gia.
- Role/permission tùy chỉnh và moderator.
- Sửa/xóa server, chuyển owner, xóa/kéo thả channel.
- Direct message và group direct message.
- Attachment, reaction, reply, thread, pin và search.
- Presence, typing indicator và read receipt.
- Push notification.
- Voice/video và bot.

## 13. Definition of Done cho vertical slice đầu tiên

API tối thiểu được xem là hoàn thành khi:

1. Hai user có thể đăng ký và đăng nhập độc lập.
2. User A tạo server và tự động trở thành owner.
3. User A thêm User B vào server và tạo channel.
4. Cả hai user lấy được lịch sử channel qua REST.
5. Cả hai kết nối SignalR và subscribe channel sau khi được kiểm tra membership.
6. User A gửi message qua REST và User B nhận `MessageCreated` realtime.
7. Author sửa/xóa được message; user khác nhận event và không thể sửa/xóa message đó.
8. Refresh token được rotate; token cũ không thể dùng lại.
9. Integration test bao phủ luồng thành công cùng các lỗi `401`, `403`, `404`, `409` và validation `400` quan trọng.

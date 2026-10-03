# Nhắn tin riêng (DM) — API Contracts

> Tài liệu gốc: [SCDC-API-DM-001](../../archive/project-specs/05-architecture/02-account-dm-contracts.md)

## Endpoints

| Method | Đường dẫn | Mô tả |
|---|---|---|
| `GET` | `/api/v1/users/search?q=...&cursor=...&limit=...` | Tìm người theo tên tài khoản/tên hiển thị |
| `POST` | `/api/v1/direct-conversations` | Tạo hoặc lấy hội thoại với `{ peerUserId }` |
| `GET` | `/api/v1/direct-conversations?cursor=...&limit=...` | Danh sách hội thoại của mình |
| `GET` | `/api/v1/direct-conversations/{id}/messages` | Lịch sử tin nhắn (Cursor Pagination) |
| `POST` | `/api/v1/direct-conversations/{id}/messages` | Gửi tin `{ clientMessageId, content }` |
| `PATCH` | `/api/v1/direct-conversations/{id}/messages/{msgId}` | Sửa tin `{ content, expectedVersion }` |
| `DELETE` | `/api/v1/direct-conversations/{id}/messages/{msgId}` | Xóa tin `?expectedVersion=...` |

## Dữ liệu chính

**MessageDto:** `id, conversationId, author, clientMessageId, sequence, version, content, createdAt, editedAt, deletedAt`

## Cơ chế chống trùng (Idempotency)

- Client tạo `clientMessageId` (UUID) **1 lần khi bấm gửi**, giữ nguyên khi "Thử lại".
- Server dùng Unique Constraint `(conversationId, clientMessageId)` để tránh tạo tin trùng.
- Cùng khóa nhưng payload khác → `409 OPERATION_CONFLICT`.

## Cơ chế phiên bản (Optimistic Concurrency)

- Sửa/xóa gửi `expectedVersion`.
- Version không khớp → `409 VERSION_CONFLICT` → client tải lại bản mới.

---

📎 Đầy đủ: [Hợp đồng API gốc](../../archive/project-specs/05-architecture/02-account-dm-contracts.md)

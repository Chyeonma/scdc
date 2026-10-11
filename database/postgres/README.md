# SCDC — PostgreSQL

Thiết kế schema, quan hệ và quy ước dữ liệu tại [Thiết kế database](../../docs/system/database.md). SQL có cấu trúc cho cả tính năng chưa triển khai.

## Mục lục

- [Kết nối local](#kết-nối-local)
- [SQL và migration](#sql-và-migration)
- [Truy vấn quan sát](#truy-vấn-quan-sát)

## Kết nối local

Thông số dưới đây dùng cho stack Compose Development:

| Thuộc tính | Giá trị |
|---|---|
| Driver | PostgreSQL |
| Host / port | `localhost:5432` |
| Database | `scdc_chat` |
| Username / password | `scdc` / `scdc_dev` |
| SSL mode | `disable` |

Trong DBeaver, mở `Schemas` và chọn `identity`, `community`, `messaging`, `moderation`, `audit`, `integration`, `common`.

## SQL và migration

- [schema.sql](schema.sql): bootstrap schema, constraint, index, trigger và view; có DROP SCHEMA.
- [seed.sql](seed.sql): dữ liệu minh họa, password/token không dùng đăng nhập.
- [migrations](migrations): thay đổi có ledger/checksum cho DB cần giữ dữ liệu. Runner, thứ tự và mapping legacy tại [hướng dẫn migration](../../docs/guides/community-development.md#migration).

Compose chỉ chạy script init khi volume mới được khởi tạo. Sửa SQL không cập nhật volume có sẵn. Dùng [database thử riêng](../../docs/guides/community-development.md#local-environment) cho suite tích hợp.

## Truy vấn quan sát

Các view hỗ trợ đọc tài khoản, phiên, space và timeline. Chạy trên DB local hoặc DB thử phù hợp:

```sql
SELECT * FROM identity.v_user_accounts ORDER BY username;
SELECT * FROM identity.v_active_sessions;

SELECT
    id,
    session_id,
    parent_token_id,
    replaced_by_token_id,
    used_at,
    revoked_at
FROM identity.refresh_tokens
ORDER BY session_id, created_at;

SELECT *
FROM messaging.v_space_overview
ORDER BY last_activity_at DESC;

SELECT *
FROM messaging.v_message_timeline
ORDER BY sequence_no;

SELECT *
FROM integration.outbox_events
ORDER BY occurred_at;
```

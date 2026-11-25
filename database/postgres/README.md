# SCDC — PostgreSQL

Thiết kế schema, quan hệ và quy ước dữ liệu tại [Thiết kế database](../../docs/system/database.md). SQL có cấu trúc cho cả tính năng chưa triển khai.

## Identity riêng — S3-03

Từ S3-03, Identity chạy trên instance `identity-postgres`, database
`scdc_identity`. [Baseline, chuyển offline và phục hồi](identity/README.md)
quy định lệnh thật và quyền nguồn/đích. Các script monolith trên trang này tiếp tục
dùng cho baseline nguồn và fixture; không chạy chúng trên database Identity
riêng. Community/Messaging vẫn ở instance `postgres` tới các task tương ứng.

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
| Quản trị / migration | `scdc` / `scdc_dev` |
| User API ở instance cũ | `scdc_community`, `scdc_messaging`; `scdc_identity` giữ `NOLOGIN` sau chuyển |
| SSL mode | `disable` |

Trong DBeaver, mở `Schemas` và chọn `identity`, `community`, `messaging`. S2-05 chỉ loại Moderation khi cả hai bảng rỗng; gặp dữ liệu thì migration dừng và giữ nguyên database.

## SQL và migration

- [schema.sql](schema.sql): bootstrap schema, constraint, index, trigger và view; có DROP SCHEMA.
- [module-roles.sql](module-roles.sql): cấp ba role runtime chỉ trong schema owner và thu hồi quyền chéo đã cấp trước S2-11, gồm quyền cột và hàm. Script kiểm tra baseline S2-07/S2-09 trước khi thay quyền; Compose gọi qua [init-module-roles.sh](init-module-roles.sh). Mật khẩu local mặc định là `<tên_role>_dev`; có thể đặt ba biến `IDENTITY_DB_PASSWORD`, `COMMUNITY_DB_PASSWORD`, `MESSAGING_DB_PASSWORD`.
- [seed.sql](seed.sql): dữ liệu minh họa, password/token không dùng đăng nhập.
- [migrations](migrations): thay đổi có ledger/checksum cho DB cần giữ dữ liệu. Runner, thứ tự và mapping legacy tại [hướng dẫn migration](../../docs/guides/development.md#migration).
- Sau S2-03, chạy S2-04 và S2-05 theo [hướng dẫn ranh giới schema](../../docs/guides/development.md#s2-04-s2-05-upgrade). Hai migration độc lập, không sửa dữ liệu nghiệp vụ; bootstrap mới đã gồm cả hai.
- S2-09 sau S2-07 thêm tiến trình tạo phòng riêng của Community/Messaging; [hướng dẫn nâng cấp offline](../../docs/guides/development.md#s2-09-upgrade). Chạy lại role script sau migration; giữ dấu hủy sau cleanup, không DELETE hai bảng provision bằng runtime.
- S2-11 không thêm DDL/migration: dừng writer, nâng cấp đủ baseline S2-07/S2-09, chạy role script bằng quản trị rồi dùng API S2-10 trở lên. [Hướng dẫn thu hẹp quyền](../../docs/guides/development.md#s2-11-upgrade) và [kiểm chứng](../../docs/records/verification/architecture/s2-11-2026-11-25.md).

Compose chỉ chạy script init khi volume mới được khởi tạo. Sửa SQL không cập nhật volume có sẵn. Dùng [database thử riêng](../../docs/guides/development.md#local-environment) cho suite tích hợp.

## Truy vấn quan sát

Identity giữ view nội bộ để đọc tài khoản/phiên. Hai view Messaging đọc chéo đã bỏ ở S2-05; quan sát bằng bảng Messaging hoặc API nghiệp vụ:

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

SELECT id, space_type, last_message_sequence, last_activity_at
FROM messaging.spaces
ORDER BY last_activity_at DESC;

SELECT id, space_id, conversation_sequence, version, deleted_at
FROM messaging.messages
ORDER BY space_id, conversation_sequence;

SELECT *
FROM messaging.outbox_events
ORDER BY occurred_at;
```

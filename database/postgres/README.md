# SCDC PostgreSQL database

Database `scdc_chat` duoc chia theo domain:

| Schema | Noi dung |
|---|---|
| `identity` | User, profile, email, password credential, MFA, session va token |
| `community` | Server, member, channel, role, permission, invite va ban |
| `messaging` | DM, group chat, message, attachment, reaction va read state |
| `moderation` | Message report va moderation action |
| `audit` | Security event append-only |
| `integration` | Transactional outbox va inbox idempotency |
| `common` | Trigger/function dung chung |

## DBeaver

- Driver: PostgreSQL
- Host: `localhost`
- Port: `5432`
- Database: `scdc_chat`
- Username: `scdc`
- Password: `scdc_dev`
- SSL mode: `disable` cho moi truong local

Sau khi ket noi, mo `Schemas` va chon hien thi `identity`, `community`,
`messaging`, `moderation`, `audit`, `integration`. Cac view de xem nhanh:

- `identity.v_user_accounts`
- `identity.v_active_sessions`
- `messaging.v_space_overview`
- `messaging.v_message_timeline`

## Script

- `schema.sql`: tao lai toan bo schema, constraint, index, trigger va view.
- `seed.sql`: du lieu mau cho dang ky, login/logout, token rotation, DM, group
  chat, server channel, role, permission, attachment, reaction va outbox.

Du lieu password/token trong `seed.sql` chi de minh hoa va khong dang nhap duoc.

## Query de quan sat luong du lieu

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

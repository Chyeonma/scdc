-- Additive: retain all existing users, pairs, memberships and messages.
BEGIN;
CREATE INDEX IF NOT EXISTS ix_dm_inbox_order ON messaging.spaces
    ((last_activity_at IS NOT NULL) DESC, last_activity_at DESC, id ASC)
    WHERE space_type=1 AND status<>3 AND deleted_at IS NULL;
COMMIT;

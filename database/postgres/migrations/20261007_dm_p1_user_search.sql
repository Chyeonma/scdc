-- Additive/idempotent upgrade. Existing profile text and identity IDs are preserved.
-- After this SQL, run SCDC.Api --initialize-user-search-keys before serving searches.
BEGIN;
ALTER TABLE identity.user_profiles
    ADD COLUMN IF NOT EXISTS display_name_search_key text COLLATE "C" NOT NULL DEFAULT '';
CREATE INDEX IF NOT EXISTS ix_users_search_order ON identity.users (normalized_username COLLATE "C", id)
    WHERE status = 1 AND deleted_at IS NULL;
COMMIT;

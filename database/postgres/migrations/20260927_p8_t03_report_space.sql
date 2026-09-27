-- Apply once to existing installations; schema.sql covers fresh databases.
CREATE TABLE IF NOT EXISTS moderation.reviewers (
    user_id uuid PRIMARY KEY REFERENCES identity.users (id) ON DELETE CASCADE,
    granted_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
ALTER TABLE moderation.message_reports ADD COLUMN IF NOT EXISTS space_id uuid;
UPDATE moderation.message_reports AS report
SET space_id = message.space_id
FROM messaging.messages AS message
WHERE report.message_id = message.id AND report.space_id IS NULL;
ALTER TABLE moderation.message_reports ALTER COLUMN space_id SET NOT NULL;
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_message_reports_space'
          AND conrelid = 'moderation.message_reports'::regclass) THEN
        ALTER TABLE moderation.message_reports ADD CONSTRAINT fk_message_reports_space
            FOREIGN KEY (space_id) REFERENCES messaging.spaces (id) ON DELETE RESTRICT;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS ix_message_reports_space_pending
    ON moderation.message_reports (space_id, created_at) WHERE status IN (0, 1);

-- Additive/replayable; preserve pending events, body-free payloads and all historical messages.
BEGIN;
ALTER TABLE integration.outbox_events ADD COLUMN IF NOT EXISTS lease_token uuid;
ALTER TABLE integration.outbox_events ADD COLUMN IF NOT EXISTS lease_expires_at timestamptz;
ALTER TABLE integration.outbox_events DROP CONSTRAINT IF EXISTS ck_outbox_events_lease;
ALTER TABLE integration.outbox_events ADD CONSTRAINT ck_outbox_events_lease CHECK
 ((lease_token IS NULL) = (lease_expires_at IS NULL));
CREATE INDEX IF NOT EXISTS ix_outbox_events_dispatch ON integration.outbox_events(available_at,lease_expires_at,occurred_at,id)
 WHERE published_at IS NULL AND event_type='Messaging.MessageChanged';
COMMIT;

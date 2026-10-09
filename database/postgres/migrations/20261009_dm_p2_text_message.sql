-- Additive and replayable. Never replay schema.sql over an existing volume.
BEGIN;
ALTER TABLE messaging.messages ADD COLUMN IF NOT EXISTS conversation_sequence bigint;
WITH ordered AS (
 SELECT m.id, coalesce((SELECT max(existing.conversation_sequence) FROM messaging.messages existing
  WHERE existing.space_id=m.space_id),0) + row_number() OVER (PARTITION BY m.space_id ORDER BY m.sequence_no,m.id) AS seq
 FROM messaging.messages m WHERE m.conversation_sequence IS NULL
)
UPDATE messaging.messages m SET conversation_sequence=o.seq FROM ordered o
WHERE m.id=o.id AND m.conversation_sequence IS NULL;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE messaging.messages ALTER COLUMN conversation_sequence SET NOT NULL;
ALTER TABLE messaging.messages DROP CONSTRAINT IF EXISTS ck_messages_conversation_sequence;
ALTER TABLE messaging.messages ADD CONSTRAINT ck_messages_conversation_sequence CHECK (conversation_sequence>0);
CREATE UNIQUE INDEX IF NOT EXISTS ux_messages_conversation_sequence ON messaging.messages(space_id,conversation_sequence);
CREATE INDEX IF NOT EXISTS ix_messages_conversation_history ON messaging.messages(space_id,conversation_sequence DESC);
UPDATE messaging.spaces s SET last_message_sequence=greatest(coalesce(s.last_message_sequence,0),
 coalesce((SELECT max(m.conversation_sequence) FROM messaging.messages m WHERE m.space_id=s.id),0))
WHERE s.last_message_sequence IS DISTINCT FROM greatest(coalesce(s.last_message_sequence,0),
 coalesce((SELECT max(m.conversation_sequence) FROM messaging.messages m WHERE m.space_id=s.id),0));
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE messaging.spaces ALTER COLUMN last_message_sequence SET DEFAULT 0;
ALTER TABLE messaging.spaces ALTER COLUMN last_message_sequence SET NOT NULL;
ALTER TABLE messaging.messages DROP CONSTRAINT IF EXISTS ck_messages_text;
ALTER TABLE messaging.messages ADD CONSTRAINT ck_messages_text CHECK (
 message_type<>1 OR (deleted_at IS NULL AND content IS NOT NULL AND char_length(content)>0)
 OR (deleted_at IS NOT NULL AND content IS NULL));
CREATE TABLE IF NOT EXISTS messaging.send_operations (
 space_id uuid NOT NULL, author_user_id uuid NOT NULL, client_message_id uuid NOT NULL,
 message_id uuid NOT NULL, fingerprint_version integer, key_id varchar(64), fingerprint bytea,
 created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
 PRIMARY KEY(space_id,author_user_id,client_message_id),
 CONSTRAINT fk_send_operations_message FOREIGN KEY(space_id,message_id)
  REFERENCES messaging.messages(space_id,id) ON DELETE RESTRICT
);
ALTER TABLE messaging.send_operations DROP CONSTRAINT IF EXISTS ck_send_operations_fingerprint;
ALTER TABLE messaging.send_operations ADD CONSTRAINT ck_send_operations_fingerprint CHECK (
  (fingerprint_version IS NULL AND key_id IS NULL AND fingerprint IS NULL)
  OR (fingerprint_version IS NOT NULL AND fingerprint_version=1 AND key_id IS NOT NULL AND fingerprint IS NOT NULL AND octet_length(fingerprint)=32));
-- Legacy sends are readable but cannot be verified from today's edited content.
INSERT INTO messaging.send_operations(space_id,author_user_id,client_message_id,message_id,created_at)
SELECT space_id,author_user_id,client_message_id,id,created_at FROM messaging.messages
WHERE author_user_id IS NOT NULL AND client_message_id IS NOT NULL
ON CONFLICT(space_id,author_user_id,client_message_id) DO NOTHING;
COMMIT;

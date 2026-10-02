-- P9-T01: preserve default/Owner channel upload behavior while requiring an explicit capability.
-- Apply once to an existing database before deploying the CanAttach checks.
\set ON_ERROR_STOP on
BEGIN;

INSERT INTO community.permissions (code, description)
VALUES ('attach_files', 'Attach files to messages in a channel')
ON CONFLICT (code) DO NOTHING;

INSERT INTO community.role_permissions (role_id, permission_code)
SELECT id, 'attach_files'
FROM community.roles
WHERE is_default OR (is_system AND name = 'Owner')
ON CONFLICT (role_id, permission_code) DO NOTHING;

COMMIT;

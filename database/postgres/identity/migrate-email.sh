#!/bin/sh
set -eu
# Owner upgrade after cutover, before starting the new Identity host. Never a runtime credential.
export PGPASSWORD="$(cat /run/secrets/identity_database_admin_password)"
email_checksum=$(sha256sum /opt/scdc/identity/20261126_e4-01_email_worker.sql | cut -d ' ' -f 1)
psql -X -v ON_ERROR_STOP=1 -v email_checksum="$email_checksum" <<'SQL'
BEGIN;
SET LOCAL lock_timeout='5s';
SELECT pg_advisory_xact_lock(1935893324,1);
SELECT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE module='identity' AND version=2026112601) AS applied \gset
\if :applied
SELECT checksum=:'email_checksum' AS matching FROM identity.schema_migrations WHERE module='identity' AND version=2026112601 \gset
\if :matching
\else
DO $$ BEGIN RAISE EXCEPTION 'E4-01 checksum differs from the applied migration'; END $$;
\endif
\else
\i /opt/scdc/identity/20261126_e4-01_email_worker.sql
INSERT INTO identity.schema_migrations(module,version,checksum) VALUES('identity',2026112601,:'email_checksum');
\endif
COMMIT;
SQL
unset PGPASSWORD

#!/bin/sh
set -eu
# Owner upgrade after cutover, before starting the new Identity host. Never a runtime credential.
export PGPASSWORD="$(cat /run/secrets/identity_database_admin_password)"
email_checksum=$(sha256sum /opt/scdc/identity/20261126_e4-01_email_upgrade.sql | cut -d ' ' -f 1)
# Read-only preflight uses the same migration checksum as the applying path.
if [ "${1:-}" = --check ]; then
  ready=$(psql -XAt -v ON_ERROR_STOP=1 -v email_checksum="$email_checksum" <<'SQL'
SELECT EXISTS(SELECT 1 FROM identity.schema_migrations
  WHERE module='identity' AND version=2026112602 AND checksum=:'email_checksum');
SQL
)
  test "$ready" = t || { echo 'Identity email schema is not current; run make migrate before starting writers.' >&2; exit 1; }
  exit 0
fi
[ "$#" -eq 0 ] || { echo 'Usage: migrate-email.sh [--check]' >&2; exit 2; }
psql -X -v ON_ERROR_STOP=1 -v email_checksum="$email_checksum" <<'SQL'
BEGIN;
SET LOCAL lock_timeout='5s';
SELECT pg_advisory_xact_lock(1935893324,1);
SELECT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE module='identity' AND version=2026112602) AS applied \gset
\if :applied
SELECT checksum=:'email_checksum' AS matching FROM identity.schema_migrations WHERE module='identity' AND version=2026112602 \gset
\if :matching
\else
DO $$ BEGIN RAISE EXCEPTION 'E4-01 checksum differs from the applied migration'; END $$;
\endif
\else
\i /opt/scdc/identity/20261126_e4-01_email_upgrade.sql
INSERT INTO identity.schema_migrations(module,version,checksum) VALUES('identity',2026112602,:'email_checksum');
\endif
COMMIT;
SQL
unset PGPASSWORD

#!/bin/sh
set -eu
# Entrypoint only, on a NEW source volume after schema/seed; never used for dev migration.
export PGUSER="$POSTGRES_USER" PGDATABASE="$POSTGRES_DB"
unset PGHOST
export PGPASSWORD="${POSTGRES_PASSWORD:-}"
export COMMUNITY_DATABASE_NAME="${COMMUNITY_DATABASE_NAME:-scdc_community}"
existing=$(psql -XAt -v ON_ERROR_STOP=1 -v target="$COMMUNITY_DATABASE_NAME" <<'SQL'
SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname=:'target');
SQL
)
test "$existing" = f || { echo 'Fresh initializer refuses an existing Community target.' >&2; exit 1; }
sh /opt/scdc/community/bootstrap-target.sh
psql -X -v ON_ERROR_STOP=1 -c 'ALTER ROLE scdc_community NOLOGIN'
source_id=$(psql -XAt -v ON_ERROR_STOP=1 -c "SELECT system_identifier::text||':'||(SELECT oid::text FROM pg_database WHERE datname=current_database())||':'||current_database() FROM pg_control_system()")
umask 077
archive=$(mktemp)
trap 'rm -f "$archive"' EXIT HUP INT TERM
pg_dump -Fc --schema=community --no-owner --no-privileges > "$archive"
export PGDATABASE="$COMMUNITY_DATABASE_NAME"
psql -X -v ON_ERROR_STOP=1 -c 'DROP SCHEMA community CASCADE'
pg_restore --single-transaction --exit-on-error --no-owner --no-privileges --dbname="$PGDATABASE" "$archive"
sed 's/scdc_community/scdc_community_service/g' /opt/scdc/community/runtime-grants.sql | psql -X -v ON_ERROR_STOP=1
psql -X -v ON_ERROR_STOP=1 -v source_id="$source_id" <<'SQL'
BEGIN;
UPDATE scdc_migrations.service_baselines SET origin='fresh',source_id=:'source_id' WHERE service='community';
ALTER ROLE scdc_community_service LOGIN;
COMMIT;
SQL
echo 'Fresh Community seed copied to its own database; legacy writer frozen.'

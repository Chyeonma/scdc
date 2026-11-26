#!/bin/sh
set -eu
# Administrator job only. Creates an empty target; never copies a used database.
: "${COMMUNITY_DATABASE_NAME:=scdc_community}"
case "$COMMUNITY_DATABASE_NAME" in ''|*[!a-z0-9_]*) echo 'Invalid Community database name' >&2; exit 1;; esac
test "$COMMUNITY_DATABASE_NAME" != "$PGDATABASE"
export COMMUNITY_SERVICE_PASSWORD="$(cat /run/secrets/community_database_password)"
exists=$(psql -XAt -v ON_ERROR_STOP=1 -v target="$COMMUNITY_DATABASE_NAME" <<'SQL'
SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname=:'target');
SQL
)
if [ "$exists" = t ]; then
  PGDATABASE="$COMMUNITY_DATABASE_NAME" psql -XAt -v ON_ERROR_STOP=1 <<'SQL'
DO $$ BEGIN
  IF NOT EXISTS(SELECT 1 FROM scdc_migrations.service_baselines WHERE service='community' AND version=2026112610) THEN
    RAISE EXCEPTION 'Existing target has no matching Community baseline; no data was changed';
  END IF;
END $$;
SQL
  exit 0
fi
psql -X -v ON_ERROR_STOP=1 -v target="$COMMUNITY_DATABASE_NAME" <<'SQL'
\getenv runtime_password COMMUNITY_SERVICE_PASSWORD
BEGIN;
DO $$ BEGIN
 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='scdc_community_service') THEN
   RAISE EXCEPTION 'Community service role already exists without its database; review before provisioning';
 END IF;
END $$;
CREATE ROLE scdc_community_service NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
ALTER ROLE scdc_community_service PASSWORD :'runtime_password';
COMMIT;
CREATE DATABASE :"target";
SQL
unset COMMUNITY_SERVICE_PASSWORD
export PGDATABASE="$COMMUNITY_DATABASE_NAME"
psql -X -v ON_ERROR_STOP=1 -f /opt/scdc/community/schema.sql
psql -X -v ON_ERROR_STOP=1 -f /opt/scdc/community/baseline.sql
checksum=$(sha256sum /opt/scdc/community/schema.sql | cut -d ' ' -f 1)
psql -X -v ON_ERROR_STOP=1 -v baseline_checksum="$checksum" -v baseline_origin=transfer -v source_id='' -f /opt/scdc/community/service-baseline.sql
sed 's/scdc_community/scdc_community_service/g' /opt/scdc/community/runtime-grants.sql | psql -X -v ON_ERROR_STOP=1
echo 'Empty Community target initialized; runtime remains NOLOGIN until verified transfer.'

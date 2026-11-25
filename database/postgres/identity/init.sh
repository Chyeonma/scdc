#!/bin/sh
set -eu
# Only a new, empty Identity volume runs this entrypoint; never a cutover tool.
export IDENTITY_DATABASE_PASSWORD="$(cat /run/secrets/identity_database_password)"
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
\getenv runtime_password IDENTITY_DATABASE_PASSWORD
CREATE ROLE scdc_identity LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
ALTER ROLE scdc_identity PASSWORD :'runtime_password';
CREATE EXTENSION IF NOT EXISTS pg_trgm;
SQL
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /opt/scdc/identity/schema.sql
baseline_checksum=$(sha256sum /opt/scdc/identity/schema.sql | cut -d ' ' -f 1)
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v baseline_checksum="$baseline_checksum" -f /opt/scdc/identity/baseline.sql
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -f /opt/scdc/identity/runtime-grants.sql
unset IDENTITY_DATABASE_PASSWORD

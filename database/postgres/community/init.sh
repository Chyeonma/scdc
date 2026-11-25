#!/bin/sh
set -eu
# Only used by the isolated preview PostgreSQL on a fresh volume.
COMMUNITY_PREVIEW_RUNTIME_PASSWORD=$(cat /run/secrets/community_preview_password)
export COMMUNITY_PREVIEW_RUNTIME_PASSWORD
psql -X --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set ON_ERROR_STOP=1 <<'SQL'
\getenv runtime_password COMMUNITY_PREVIEW_RUNTIME_PASSWORD
BEGIN;
CREATE ROLE scdc_community LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
ALTER ROLE scdc_community PASSWORD :'runtime_password';
\ir /opt/scdc/community/runtime-grants.sql
COMMIT;
SQL
unset COMMUNITY_PREVIEW_RUNTIME_PASSWORD

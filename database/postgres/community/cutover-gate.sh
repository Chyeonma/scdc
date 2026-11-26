#!/bin/sh
set -eu
# Read-only administrator job, required before starting any Community writer.
: "${COMMUNITY_DATABASE_NAME:=scdc_community}"
source_id=$(psql -XAt -v ON_ERROR_STOP=1 -c "SELECT system_identifier::text||':'||(SELECT oid::text FROM pg_database WHERE datname=current_database())||':'||current_database() FROM pg_control_system()")
if [ "${1:-}" = --rollback ]; then
  export PGDATABASE="$COMMUNITY_DATABASE_NAME"
  restored=$(psql -XAt -v ON_ERROR_STOP=1 -v source_id="$source_id" <<'SQL'
SELECT EXISTS(SELECT 1 FROM scdc_migrations.community_transfer WHERE service='community' AND status='rolled_back' AND source_id=:'source_id')
 AND (SELECT rolcanlogin FROM pg_roles WHERE rolname='scdc_community')
 AND (SELECT NOT rolcanlogin FROM pg_roles WHERE rolname='scdc_community_service')
 AND NOT EXISTS(SELECT 1 FROM pg_stat_activity WHERE usename='scdc_community_service' AND backend_type='client backend');
SQL
)
  test "$restored" = t || { echo 'Rollback gate refused: guarded rollback has not restored a single source writer.' >&2; exit 1; }
  echo 'Community rollback gate passed; target frozen and source enabled by verified rollback.'
  exit 0
fi
frozen=$(psql -XAt -v ON_ERROR_STOP=1 -c "SELECT NOT rolcanlogin FROM pg_roles WHERE rolname='scdc_community'")
test "$frozen" = t || { echo 'Community source is writable; run the guarded offline transfer first.' >&2; exit 1; }
fresh=false
marker=$(psql -XAt -v ON_ERROR_STOP=1 -c "SELECT to_regclass('scdc_source_metadata.fresh_s3_volume') IS NOT NULL")
if [ "$marker" = t ]; then fresh=$(psql -XAt -v ON_ERROR_STOP=1 -c 'SELECT singleton FROM scdc_source_metadata.fresh_s3_volume'); fi
export PGDATABASE="$COMMUNITY_DATABASE_NAME"
checksum=$(sha256sum /opt/scdc/community/schema.sql | cut -d ' ' -f 1)
ready=$(psql -XAt -v ON_ERROR_STOP=1 -v source_id="$source_id" -v fresh="$fresh" -v checksum="$checksum" <<'SQL'
SELECT EXISTS(SELECT 1 FROM scdc_migrations.service_baselines b WHERE b.service='community' AND b.version=2026112610 AND b.checksum=:'checksum' AND (
 (b.origin='fresh' AND :'fresh' IN ('t','true') AND b.source_id=:'source_id') OR
 (b.origin='transfer' AND EXISTS(SELECT 1 FROM scdc_migrations.community_transfer t WHERE t.service='community' AND t.status='ready' AND t.source_id=:'source_id'))))
 AND (SELECT rolcanlogin AND NOT(rolsuper OR rolcreatedb OR rolcreaterole OR rolbypassrls) FROM pg_roles WHERE rolname='scdc_community_service')
 AND NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname IN ('identity','messaging'));
SQL
)
test "$ready" = t || { echo 'Community target does not match the frozen source; keep writers stopped.' >&2; exit 1; }
echo 'Community cutover gate passed; source frozen and target matched.'

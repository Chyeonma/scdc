#!/bin/sh
set -eu
# Administrator-only, read-only job. No source credentials reach an API host.
source_query() {
  PGPASSWORD="$SOURCE_DATABASE_PASSWORD" PGHOST=postgres PGUSER="$SOURCE_DATABASE_USER" PGDATABASE="$SOURCE_DATABASE_NAME" \
    psql -X -A -t -v ON_ERROR_STOP=1
}
source_id=$(printf '%s\n' "SELECT system_identifier::text||':'||(SELECT oid::text FROM pg_database WHERE datname=current_database())||':'||current_database() FROM pg_control_system()" | source_query)
frozen=$(printf '%s\n' "SELECT NOT rolcanlogin FROM pg_roles WHERE rolname='scdc_identity'" | source_query)
if [ "$frozen" != t ]; then
  echo 'Identity source is writable. Complete the offline transfer before starting the new host.' >&2
  exit 1
fi
fresh=false
marker=$(printf '%s\n' "SELECT to_regclass('scdc_source_metadata.fresh_s3_volume') IS NOT NULL" | source_query)
if [ "$marker" = t ]; then
  fresh=$(printf '%s\n' "SELECT singleton FROM scdc_source_metadata.fresh_s3_volume" | source_query)
fi
export PGPASSWORD="$(cat /run/secrets/identity_database_admin_password)"
export PGHOST=identity-postgres PGUSER=scdc_identity_admin PGDATABASE="$TARGET_DATABASE_NAME"
ready=$(psql -X -A -t -v ON_ERROR_STOP=1 -v source_id="$source_id" -v fresh="$fresh" <<'SQL'
SELECT EXISTS(SELECT 1 FROM scdc_migrations.service_baselines b
  WHERE b.service='identity' AND (
    (b.origin='fresh' AND :'fresh' IN ('t','true')) OR
    (b.origin='transfer' AND EXISTS(SELECT 1 FROM scdc_migrations.identity_transfer t
       WHERE t.service='identity' AND t.status='ready' AND t.source_id=:'source_id'))))
  AND (SELECT rolcanlogin FROM pg_roles WHERE rolname='scdc_identity');
SQL
)
unset PGPASSWORD
if [ "$ready" != t ]; then
  echo 'Identity target is not ready for this source. Run the guarded offline transfer; keep writers stopped.' >&2
  exit 1
fi
echo 'Identity cutover gate passed; source frozen and target baseline matched.'

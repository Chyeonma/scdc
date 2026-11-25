#!/bin/sh
set -eu
# Fresh S3 source volumes no longer host an Identity writer. Existing volumes
# are frozen by the guarded transfer, not by re-running entrypoint scripts.
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -c 'ALTER ROLE scdc_identity NOLOGIN'
psql -X -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<'SQL'
CREATE SCHEMA scdc_source_metadata;
REVOKE ALL ON SCHEMA scdc_source_metadata FROM PUBLIC;
CREATE TABLE scdc_source_metadata.fresh_s3_volume(singleton boolean PRIMARY KEY CHECK(singleton));
INSERT INTO scdc_source_metadata.fresh_s3_volume VALUES(true);
SQL

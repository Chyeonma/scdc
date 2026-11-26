-- Administrator metadata; the runtime role cannot access this schema.
CREATE SCHEMA scdc_migrations;
REVOKE ALL ON SCHEMA scdc_migrations FROM PUBLIC;
CREATE TABLE scdc_migrations.service_baselines (
    service text PRIMARY KEY CHECK(service='community'),
    version bigint NOT NULL,
    checksum char(64) NOT NULL,
    origin text NOT NULL CHECK(origin IN ('fresh','transfer')),
    source_id text,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE TABLE scdc_migrations.community_transfer (
    service text PRIMARY KEY CHECK(service='community'),
    source_id text NOT NULL,
    source_hash char(64) NOT NULL,
    archive_hash char(64),
    status text NOT NULL CHECK(status IN ('copying','ready','rolled_back')),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
INSERT INTO scdc_migrations.service_baselines(service,version,checksum,origin,source_id)
VALUES('community',2026112610,:'baseline_checksum',:'baseline_origin',NULLIF(:'source_id',''));

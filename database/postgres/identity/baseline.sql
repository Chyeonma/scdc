-- This is a new service baseline, not a claim that MVP migrations ran here.
CREATE SCHEMA scdc_migrations;
CREATE TABLE scdc_migrations.service_baselines (
    service text PRIMARY KEY CHECK(service='identity'),
    version bigint NOT NULL,
    checksum char(64) NOT NULL,
    origin text NOT NULL CHECK(origin IN ('fresh','transfer')),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE TABLE scdc_migrations.identity_transfer (
    service text PRIMARY KEY CHECK(service='identity'),
    source_id text NOT NULL,
    source_hash char(64) NOT NULL,
    archive_hash char(64),
    status text NOT NULL CHECK(status IN ('copying','ready','rolled_back')),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
INSERT INTO scdc_migrations.service_baselines(service,version,checksum,origin)
VALUES('identity',2026112503,:'baseline_checksum','fresh');

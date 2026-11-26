-- Run as the database owner after schema.sql and baseline.sql, with scdc_community already created.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA community FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA community FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA community FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA community FROM PUBLIC;
GRANT USAGE ON SCHEMA community TO scdc_community;
GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA community TO scdc_community;
GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA community TO scdc_community;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA community TO scdc_community;
REVOKE ALL ON community.schema_migrations FROM scdc_community;
DO $$ BEGIN
 IF to_regclass('community.legacy_schema_migrations') IS NOT NULL THEN
  REVOKE ALL ON community.legacy_schema_migrations FROM scdc_community;
 END IF;
END $$;
REVOKE DELETE ON community.access_fences,community.access_scopes,community.access_changes,community.channel_provisions FROM scdc_community;
REVOKE UPDATE,DELETE ON community.fence_receipts,community.access_change_acks FROM scdc_community;

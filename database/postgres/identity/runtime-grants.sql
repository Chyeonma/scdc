-- Run as the database administrator after bootstrap/restore, never as the app.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL ON SCHEMA identity FROM PUBLIC;
REVOKE ALL ON ALL TABLES IN SCHEMA identity FROM PUBLIC;
REVOKE ALL ON ALL SEQUENCES IN SCHEMA identity FROM PUBLIC;
REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA identity FROM PUBLIC;
GRANT USAGE ON SCHEMA identity TO scdc_identity;
GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA identity TO scdc_identity;
GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA identity TO scdc_identity;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA identity TO scdc_identity;
REVOKE ALL ON identity.schema_migrations FROM scdc_identity;
DO $$ BEGIN
    IF to_regclass('identity.legacy_schema_migrations') IS NOT NULL THEN
        REVOKE ALL ON identity.legacy_schema_migrations FROM scdc_identity;
    END IF;
END $$;
REVOKE UPDATE,DELETE ON identity.security_events FROM scdc_identity;
REVOKE DELETE ON identity.access_scopes,identity.access_changes,identity.session_publications FROM scdc_identity;
REVOKE UPDATE,DELETE ON identity.access_change_acks FROM scdc_identity;
REVOKE ALL ON SCHEMA scdc_migrations FROM PUBLIC,scdc_identity;
REVOKE ALL ON ALL TABLES IN SCHEMA scdc_migrations FROM PUBLIC,scdc_identity;

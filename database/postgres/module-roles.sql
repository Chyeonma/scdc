\set ON_ERROR_STOP on
\getenv identity_password IDENTITY_DB_PASSWORD
\getenv community_password COMMUNITY_DB_PASSWORD
\getenv messaging_password MESSAGING_DB_PASSWORD

BEGIN;
SELECT format('CREATE ROLE %I LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS', name)
FROM unnest(ARRAY['scdc_identity','scdc_community','scdc_messaging']) AS name
WHERE NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=name) \gexec
ALTER ROLE scdc_identity PASSWORD :'identity_password';
ALTER ROLE scdc_community PASSWORD :'community_password';
ALTER ROLE scdc_messaging PASSWORD :'messaging_password';
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
DO $$ DECLARE owner text; app_role text; BEGIN
    FOREACH owner IN ARRAY ARRAY['identity','community','messaging'] LOOP
        app_role:='scdc_'||owner;
        EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I',owner,app_role);
        EXECUTE format('GRANT SELECT,INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA %I TO %I',owner,app_role);
        EXECUTE format('GRANT USAGE,SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I',owner,app_role);
        EXECUTE format('REVOKE EXECUTE ON ALL FUNCTIONS IN SCHEMA %I FROM PUBLIC',owner);
        EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA %I TO %I',owner,app_role);
        EXECUTE format('REVOKE ALL ON %I.schema_migrations FROM %I',owner,app_role);
        IF owner='identity' AND to_regclass('identity.legacy_schema_migrations') IS NOT NULL THEN
            EXECUTE format('REVOKE ALL ON identity.legacy_schema_migrations FROM %I',app_role);
        END IF;
        -- Future migrations must grant rights explicitly; no runtime DDL/default grants.
    END LOOP;
END $$;
REVOKE UPDATE,DELETE ON identity.security_events FROM scdc_identity;
-- These inspection views still read other modules and are removed in S2-05.
-- Keep them for administrators without granting a view-owner privilege bypass.
DO $$ DECLARE view_name text; BEGIN
    FOREACH view_name IN ARRAY ARRAY['v_space_overview','v_message_timeline'] LOOP
        IF to_regclass('messaging.'||view_name) IS NOT NULL THEN
            EXECUTE format('REVOKE ALL ON messaging.%I FROM scdc_messaging',view_name);
        END IF;
    END LOOP;
END $$;
REVOKE DELETE ON community.access_fences FROM scdc_community;
REVOKE DELETE ON messaging.access_fences FROM scdc_messaging;
REVOKE UPDATE,DELETE ON community.fence_receipts FROM scdc_community;
REVOKE UPDATE,DELETE ON messaging.fence_receipts FROM scdc_messaging;

DO $$ BEGIN
    IF to_regclass('identity.access_scopes') IS NOT NULL THEN
        REVOKE DELETE ON identity.access_scopes,identity.access_changes,identity.session_publications FROM scdc_identity;
        REVOKE UPDATE,DELETE ON identity.access_change_acks FROM scdc_identity;
    END IF;
END $$;

DO $$ BEGIN
    IF to_regclass('community.access_scopes') IS NOT NULL THEN
        REVOKE DELETE ON community.access_scopes,community.access_changes FROM scdc_community;
        REVOKE UPDATE,DELETE ON community.access_change_acks FROM scdc_community;
    END IF;
END $$;

-- Temporary S2-03 grants. Remove only after the replacement flows pass S2-11.
GRANT USAGE ON SCHEMA identity TO scdc_community,scdc_messaging;
GRANT SELECT ON identity.users,identity.auth_sessions,identity.user_emails,identity.user_security_states
    TO scdc_community,scdc_messaging;
GRANT SELECT ON identity.user_profiles TO scdc_messaging;
-- PostgreSQL row locks require UPDATE on at least one column. No business-field updates.
GRANT UPDATE(id) ON identity.users TO scdc_community,scdc_messaging;
GRANT UPDATE(id) ON identity.auth_sessions TO scdc_messaging;
GRANT USAGE ON SCHEMA messaging TO scdc_community;
GRANT INSERT ON messaging.spaces TO scdc_community;
-- The deferred DM invariant trigger also runs on channel-space inserts. It reads
-- id/type and attempts a row lock before returning for space_type=3.
GRANT SELECT(id,space_type),UPDATE(id) ON messaging.spaces TO scdc_community;
GRANT EXECUTE ON FUNCTION messaging.assert_direct_conversation_membership(uuid) TO scdc_community;
COMMIT;

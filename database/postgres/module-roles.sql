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
-- S2-11 requires the replacement writers and their schema before removing legacy access.
DO $$ BEGIN
    IF to_regclass('identity.access_scopes') IS NULL
        OR to_regclass('community.access_scopes') IS NULL
        OR to_regclass('community.channel_provisions') IS NULL
        OR to_regclass('messaging.channel_provisions') IS NULL THEN
        RAISE EXCEPTION 'Apply S2-06, S2-07 and S2-09 before the S2-11 role script';
    END IF;
    IF NOT EXISTS(SELECT 1 FROM community.schema_migrations WHERE module='community' AND version=2026112307)
        OR NOT EXISTS(SELECT 1 FROM community.schema_migrations WHERE module='community' AND version=2026112409)
        OR NOT EXISTS(SELECT 1 FROM messaging.schema_migrations WHERE module='messaging' AND version=2026112409) THEN
        RAISE EXCEPTION 'S2-11 requires matching S2-07/S2-09 ledgers';
    END IF;
END $$;
DO $$ DECLARE owner text; app_role text; other_owner text; BEGIN
    FOREACH owner IN ARRAY ARRAY['identity','community','messaging'] LOOP
        app_role:='scdc_'||owner;
        -- S2-11: also revoke privileges already installed by earlier role scripts.
        -- Table-level REVOKE includes grants on individual columns (such as UPDATE(id)).
        FOREACH other_owner IN ARRAY ARRAY['identity','community','messaging'] LOOP
            IF other_owner<>owner THEN
                EXECUTE format('REVOKE ALL ON SCHEMA %I FROM %I',other_owner,app_role);
                EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM %I',other_owner,app_role);
                EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM %I',other_owner,app_role);
                EXECUTE format('REVOKE ALL ON ALL FUNCTIONS IN SCHEMA %I FROM %I',other_owner,app_role);
            END IF;
        END LOOP;
        EXECUTE format('REVOKE ALL ON SCHEMA %I FROM PUBLIC',owner);
        EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM PUBLIC',owner);
        EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM PUBLIC',owner);
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

DO $$ BEGIN
    IF to_regclass('community.channel_provisions') IS NOT NULL THEN
        REVOKE DELETE ON community.channel_provisions FROM scdc_community;
        REVOKE DELETE ON messaging.channel_provisions FROM scdc_messaging;
    END IF;
END $$;
COMMIT;

-- Run through SCDC.DbMigrator with the API/workers stopped. One transaction.
-- pg_temp.module_inbox_map is supplied by the runner after review; bootstrap is empty.
CREATE TEMP TABLE IF NOT EXISTS module_inbox_map(consumer_name text PRIMARY KEY, owner text NOT NULL
    CHECK(owner IN ('identity','community','messaging'))) ON COMMIT DROP;
LOCK TABLE integration.outbox_events, integration.inbox_events, audit.security_events,
    common.schema_migrations IN ACCESS EXCLUSIVE MODE;
DO $$ BEGIN
    IF EXISTS(SELECT 1 FROM integration.outbox_events
        WHERE event_type NOT LIKE 'Identity.%' AND event_type NOT LIKE 'Community.%'
          AND event_type NOT LIKE 'Messaging.%' AND event_type <> 'MessageCreated') THEN
        RAISE EXCEPTION 'Unknown outbox event type; review ownership before migration';
    END IF;
    IF EXISTS(SELECT 1 FROM integration.inbox_events i
        LEFT JOIN pg_temp.module_inbox_map m USING(consumer_name) WHERE m.consumer_name IS NULL) THEN
        RAISE EXCEPTION 'Inbox consumer requires an explicit reviewed owner map';
    END IF;
    IF EXISTS(SELECT 1 FROM common.schema_migrations WHERE module <> 'community') THEN
        RAISE EXCEPTION 'Unexpected migration ledger module; review ownership first';
    END IF;
    IF (SELECT count(*) FROM common.schema_migrations WHERE module='community' AND
        (version,checksum) IN (
          (1,'b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4'),
          (2,'fe07dcc516bfce7eedf257c8fb42d7baa615c716aba9034de9f4e9cebe40ccff'),
          (3,'9c109521897297c26a2f1b920194b8ef725b2828be98d1ffb3134f8896e1069d'),
          (4,'b5518a26af4b4987b0c2b8aed09909afdfb6d7884c7133cf78fa330af253206c'))) <> 4 THEN
        RAISE EXCEPTION 'Apply supported Community migrations 001-004 first';
    END IF;
END $$;

-- LIKE INCLUDING ALL preserves column defaults, checks and dispatch indexes.
DO $$ DECLARE owner text; BEGIN
    FOREACH owner IN ARRAY ARRAY['identity','community','messaging'] LOOP
        EXECUTE format('CREATE TABLE %I.outbox_events (LIKE integration.outbox_events INCLUDING ALL)',owner);
        EXECUTE format('CREATE TABLE %I.inbox_events (LIKE integration.inbox_events INCLUDING ALL)',owner);
        EXECUTE format('INSERT INTO %I.outbox_events SELECT * FROM integration.outbox_events WHERE '
            || 'event_type LIKE %L OR (event_type=''MessageCreated'' AND %L=''messaging'')',
            owner,initcap(owner)||'.%',owner);
        EXECUTE format('INSERT INTO %I.inbox_events SELECT i.* FROM integration.inbox_events i '
            || 'JOIN pg_temp.module_inbox_map m USING(consumer_name) WHERE m.owner=%L',owner,owner);
    END LOOP;
END $$;

-- Compare whole rows, including event IDs, payload, attempts, leases and timestamps.
DO $$ BEGIN
    IF EXISTS ((SELECT * FROM integration.outbox_events EXCEPT ALL
        (SELECT * FROM identity.outbox_events UNION ALL SELECT * FROM community.outbox_events
         UNION ALL SELECT * FROM messaging.outbox_events)) UNION ALL
        ((SELECT * FROM identity.outbox_events UNION ALL SELECT * FROM community.outbox_events
          UNION ALL SELECT * FROM messaging.outbox_events) EXCEPT ALL SELECT * FROM integration.outbox_events)) THEN
        RAISE EXCEPTION 'Outbox reconciliation failed';
    END IF;
    IF EXISTS ((SELECT * FROM integration.inbox_events EXCEPT ALL
        (SELECT * FROM identity.inbox_events UNION ALL SELECT * FROM community.inbox_events
         UNION ALL SELECT * FROM messaging.inbox_events)) UNION ALL
        ((SELECT * FROM identity.inbox_events UNION ALL SELECT * FROM community.inbox_events
          UNION ALL SELECT * FROM messaging.inbox_events) EXCEPT ALL SELECT * FROM integration.inbox_events)) THEN
        RAISE EXCEPTION 'Inbox reconciliation failed';
    END IF;
END $$;
-- SET SCHEMA moves the same relation and all indexes; audit and ledger rows stay intact.
ALTER TABLE audit.security_events SET SCHEMA identity;
ALTER TABLE common.schema_migrations SET SCHEMA community;
CREATE TABLE identity.schema_migrations (LIKE community.schema_migrations INCLUDING ALL);
CREATE TABLE messaging.schema_migrations (LIKE community.schema_migrations INCLUDING ALL);

-- Copy technical trigger helpers into each owning schema and rebind existing triggers.
DO $$ DECLARE owner text; helper text; trigger_record record; definition text; BEGIN
    FOREACH owner IN ARRAY ARRAY['identity','community','messaging'] LOOP
        FOREACH helper IN ARRAY ARRAY['touch_updated_at','touch_updated_at_and_version'] LOOP
            SELECT pg_get_functiondef(format('common.%I()',helper)::regprocedure) INTO definition;
            EXECUTE replace(definition,'common.',owner||'.');
        END LOOP;
    END LOOP;
    FOR trigger_record IN
        SELECT t.oid,t.tgname,n.nspname,c.relname FROM pg_trigger t
        JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
        JOIN pg_proc p ON p.oid=t.tgfoid JOIN pg_namespace pn ON pn.oid=p.pronamespace
        WHERE pn.nspname='common' AND NOT t.tgisinternal
    LOOP
        IF trigger_record.nspname NOT IN ('identity','community','messaging') THEN
            RAISE EXCEPTION 'Unexpected common helper consumer';
        END IF;
        definition := pg_get_triggerdef(trigger_record.oid);
        EXECUTE format('DROP TRIGGER %I ON %I.%I',trigger_record.tgname,trigger_record.nspname,trigger_record.relname);
        EXECUTE replace(definition,'common.',trigger_record.nspname||'.');
    END LOOP;
END $$;
ALTER FUNCTION common.utf16_length(text) SET SCHEMA community;
DROP FUNCTION common.touch_updated_at(), common.touch_updated_at_and_version();
DROP TABLE integration.outbox_events, integration.inbox_events;
-- No CASCADE: unexpected legacy objects/dependencies abort the whole migration.
DROP SCHEMA audit;
DROP SCHEMA integration;
DROP SCHEMA common;

-- Community and Messaging are the fence owners in S2-02. Identity is an authority.
DO $$ DECLARE owner text; BEGIN
    FOREACH owner IN ARRAY ARRAY['community','messaging'] LOOP
        EXECUTE format($ddl$
            CREATE TABLE %I.access_fences (
                authority smallint NOT NULL,
                scope_kind smallint NOT NULL,
                user_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                session_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                server_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                channel_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                membership_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                permission_code varchar(100) NOT NULL DEFAULT '',
                min_epoch bigint NOT NULL CHECK(min_epoch>0),
                updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
                PRIMARY KEY(authority,scope_kind,user_id,session_id,server_id,channel_id,membership_id,permission_code),
                CONSTRAINT ck_fence_scope CHECK (
                    (authority=1 AND scope_kind=1 AND user_id<>'00000000-0000-0000-0000-000000000000'::uuid AND session_id='00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id='00000000-0000-0000-0000-000000000000'::uuid AND channel_id='00000000-0000-0000-0000-000000000000'::uuid AND membership_id='00000000-0000-0000-0000-000000000000'::uuid AND permission_code='') OR
                    (authority=1 AND scope_kind=2 AND user_id<>'00000000-0000-0000-0000-000000000000'::uuid AND session_id<>'00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id='00000000-0000-0000-0000-000000000000'::uuid AND channel_id='00000000-0000-0000-0000-000000000000'::uuid AND membership_id='00000000-0000-0000-0000-000000000000'::uuid AND permission_code='') OR
                    (authority=2 AND scope_kind=3 AND user_id<>'00000000-0000-0000-0000-000000000000'::uuid AND session_id='00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id<>'00000000-0000-0000-0000-000000000000'::uuid AND channel_id='00000000-0000-0000-0000-000000000000'::uuid AND membership_id<>'00000000-0000-0000-0000-000000000000'::uuid AND permission_code='') OR
                    (authority=2 AND scope_kind=4 AND user_id<>'00000000-0000-0000-0000-000000000000'::uuid AND session_id='00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id<>'00000000-0000-0000-0000-000000000000'::uuid AND channel_id<>'00000000-0000-0000-0000-000000000000'::uuid AND membership_id<>'00000000-0000-0000-0000-000000000000'::uuid AND permission_code='') OR
                    (authority=2 AND scope_kind=5 AND user_id<>'00000000-0000-0000-0000-000000000000'::uuid AND session_id='00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id<>'00000000-0000-0000-0000-000000000000'::uuid AND channel_id='00000000-0000-0000-0000-000000000000'::uuid AND membership_id<>'00000000-0000-0000-0000-000000000000'::uuid
                        AND permission_code ~ '^[a-z][a-z0-9_]*$') OR
                    (authority=2 AND scope_kind=6 AND user_id='00000000-0000-0000-0000-000000000000'::uuid AND session_id='00000000-0000-0000-0000-000000000000'::uuid
                        AND server_id<>'00000000-0000-0000-0000-000000000000'::uuid AND channel_id<>'00000000-0000-0000-0000-000000000000'::uuid AND membership_id='00000000-0000-0000-0000-000000000000'::uuid AND permission_code=''))
            )
        $ddl$,owner);
        EXECUTE format($ddl$
            CREATE TABLE %I.fence_receipts (
                authority smallint NOT NULL CHECK(authority IN (1,2)),
                operation_id uuid NOT NULL CHECK(operation_id<>'00000000-0000-0000-0000-000000000000'::uuid),
                batch_id uuid NOT NULL CHECK(batch_id<>'00000000-0000-0000-0000-000000000000'::uuid),
                command_fingerprint char(64) NOT NULL CHECK(command_fingerprint ~ '^[0-9a-f]{64}$'),
                routing_generation bigint NOT NULL CHECK(routing_generation>0),
                installed_scopes jsonb NOT NULL CHECK(jsonb_typeof(installed_scopes)='array' AND jsonb_array_length(installed_scopes)>0),
                installed_at timestamptz NOT NULL DEFAULT clock_timestamp(),
                PRIMARY KEY(authority,operation_id,batch_id)
            )
        $ddl$,owner);
        EXECUTE format($ddl$
            CREATE FUNCTION %I.protect_access_fence() RETURNS trigger LANGUAGE plpgsql AS $body$
            BEGIN
                IF NEW.min_epoch<OLD.min_epoch OR
                    (NEW.authority,NEW.scope_kind,NEW.user_id,NEW.session_id,NEW.server_id,NEW.channel_id,NEW.membership_id,NEW.permission_code)
                    IS DISTINCT FROM
                    (OLD.authority,OLD.scope_kind,OLD.user_id,OLD.session_id,OLD.server_id,OLD.channel_id,OLD.membership_id,OLD.permission_code) THEN
                    RAISE EXCEPTION 'Fence key is immutable and epoch cannot decrease' USING ERRCODE='23514';
                END IF;
                NEW.updated_at:=clock_timestamp();
                RETURN NEW;
            END $body$
        $ddl$,owner);
        EXECUTE format('CREATE TRIGGER tr_protect_access_fence BEFORE UPDATE ON %I.access_fences '
            || 'FOR EACH ROW EXECUTE FUNCTION %I.protect_access_fence()',owner,owner);
    END LOOP;
END $$;

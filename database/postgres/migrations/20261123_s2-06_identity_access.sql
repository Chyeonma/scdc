-- S2-06. Apply offline through DbMigrator after S2-03. No cross-module FK.
CREATE SEQUENCE identity.access_store_generation AS bigint MINVALUE 1;
CREATE TABLE identity.access_changes (
    operation_id uuid PRIMARY KEY,
    user_id uuid NOT NULL,
    routing_generation bigint NOT NULL CHECK(routing_generation=1),
    owners text[] NOT NULL CHECK(owners=ARRAY['Community','Messaging']),
    phase smallint NOT NULL DEFAULT 0 CHECK(phase IN (0,1,2)),
    version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    payload jsonb NOT NULL CHECK(jsonb_typeof(payload)='object'),
    command_fingerprint char(64) NOT NULL CHECK(command_fingerprint ~ '^[0-9a-f]{64}$'),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    committed_at timestamptz,
    lease_token uuid,
    lease_until timestamptz,
    attempt_count integer NOT NULL DEFAULT 0,
    next_attempt_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE UNIQUE INDEX uq_identity_pending_change ON identity.access_changes(user_id) WHERE phase=0;
CREATE FUNCTION identity.protect_access_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.operation_id,NEW.user_id,NEW.routing_generation,NEW.owners,NEW.payload,NEW.command_fingerprint,NEW.created_at)
       IS DISTINCT FROM
       (OLD.operation_id,OLD.user_id,OLD.routing_generation,OLD.owners,OLD.payload,OLD.command_fingerprint,OLD.created_at)
       OR (OLD.phase<>0 AND NEW.phase<>OLD.phase) OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Accepted Identity change is immutable and version must advance' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_access_change BEFORE UPDATE ON identity.access_changes
    FOR EACH ROW EXECUTE FUNCTION identity.protect_access_change();
CREATE TABLE identity.access_scopes (
    scope_kind smallint NOT NULL CHECK(scope_kind IN (1,2)),
    user_id uuid NOT NULL,
    session_id uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
    current_epoch bigint NOT NULL DEFAULT 1 CHECK(current_epoch>0),
    pending_operation_id uuid,
    retired boolean NOT NULL DEFAULT false,
    version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    PRIMARY KEY(scope_kind,user_id,session_id),
    CHECK((scope_kind=1 AND session_id='00000000-0000-0000-0000-000000000000') OR
          (scope_kind=2 AND session_id<>'00000000-0000-0000-0000-000000000000'))
);
CREATE TABLE identity.access_change_acks (
    operation_id uuid NOT NULL REFERENCES identity.access_changes(operation_id),
    batch_id uuid NOT NULL,
    owner text NOT NULL CHECK(owner IN ('Community','Messaging')),
    routing_generation bigint NOT NULL CHECK(routing_generation=1),
    installed_scopes jsonb NOT NULL,
    acknowledged_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY(operation_id,owner)
);
-- Durable publication journal. Deletion leaves a deny tombstone; no FK to users.
CREATE TABLE identity.session_publications (
    user_id uuid PRIMARY KEY,
    version bigint NOT NULL DEFAULT 1 CHECK(version>0)
);
INSERT INTO identity.session_publications(user_id) SELECT id FROM identity.users;
CREATE FUNCTION identity.enqueue_session_publication() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE target uuid; BEGIN
    IF TG_TABLE_NAME='users' THEN target:=COALESCE(NEW.id,OLD.id);
    ELSE target:=COALESCE(NEW.user_id,OLD.user_id); END IF;
    INSERT INTO identity.session_publications(user_id) VALUES(target)
        ON CONFLICT(user_id) DO UPDATE SET version=identity.session_publications.version+1;
    RETURN NULL;
END $$;
CREATE TRIGGER tr_publish_account AFTER INSERT OR UPDATE OR DELETE ON identity.users
    FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();
CREATE TRIGGER tr_publish_session AFTER INSERT OR UPDATE OR DELETE ON identity.auth_sessions
    FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();
CREATE TRIGGER tr_publish_stamp AFTER INSERT OR UPDATE OR DELETE ON identity.user_security_states
    FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();
CREATE TRIGGER tr_publish_email AFTER INSERT OR UPDATE OR DELETE ON identity.user_emails
    FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();
CREATE TRIGGER tr_publish_scope AFTER INSERT OR UPDATE OR DELETE ON identity.access_scopes
    FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();
CREATE FUNCTION identity.protect_access_scope() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.scope_kind,NEW.user_id,NEW.session_id) IS DISTINCT FROM (OLD.scope_kind,OLD.user_id,OLD.session_id)
        OR NEW.current_epoch<OLD.current_epoch OR (OLD.retired AND NOT NEW.retired) THEN
        RAISE EXCEPTION 'Identity scope is immutable and epoch cannot decrease' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_access_scope BEFORE UPDATE ON identity.access_scopes
    FOR EACH ROW EXECUTE FUNCTION identity.protect_access_scope();

-- The first routing generation is installed offline. Both owners start from the same snapshot.
INSERT INTO identity.access_scopes(scope_kind,user_id,session_id)
    SELECT 1,id,'00000000-0000-0000-0000-000000000000'::uuid FROM identity.users;
INSERT INTO identity.access_scopes(scope_kind,user_id,session_id,retired)
    SELECT 2,user_id,id,revoked_at IS NOT NULL FROM identity.auth_sessions;
DO $$ DECLARE owner text; BEGIN
    FOREACH owner IN ARRAY ARRAY['community','messaging'] LOOP
        EXECUTE format('INSERT INTO %I.access_fences(authority,scope_kind,user_id,session_id,min_epoch) '
            || 'SELECT 1,scope_kind,user_id,session_id,current_epoch FROM identity.access_scopes '
            || 'ON CONFLICT DO NOTHING',owner);
    END LOOP;
END $$;

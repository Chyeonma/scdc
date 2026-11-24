-- S2-07. Offline after S2-06. Business rows and old ledgers stay unchanged.
CREATE TABLE community.access_scopes (LIKE community.access_fences INCLUDING DEFAULTS INCLUDING CONSTRAINTS);
ALTER TABLE community.access_scopes RENAME COLUMN min_epoch TO current_epoch;
ALTER TABLE community.access_scopes ADD COLUMN pending_operation_id uuid,
    ADD COLUMN retired boolean NOT NULL DEFAULT false,
    ADD COLUMN version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    ADD PRIMARY KEY(authority,scope_kind,user_id,session_id,server_id,channel_id,membership_id,permission_code),
    ADD CHECK(authority=2);
CREATE TABLE community.access_changes (
    operation_id uuid PRIMARY KEY,
    server_id uuid NOT NULL,
    actor_user_id uuid NOT NULL,
    actor_session_id uuid NOT NULL,
    actor_membership_id uuid NOT NULL,
    routing_generation bigint NOT NULL CHECK(routing_generation=1),
    owners text[] NOT NULL CHECK(owners=ARRAY['Community','Messaging']),
    command jsonb NOT NULL CHECK(jsonb_typeof(command)='object'),
    command_fingerprint char(64) NOT NULL CHECK(command_fingerprint ~ '^[0-9a-f]{64}$'),
    scopes jsonb NOT NULL CHECK(jsonb_typeof(scopes)='array' AND jsonb_array_length(scopes)>0),
    phase smallint NOT NULL DEFAULT 0 CHECK(phase IN(0,1,2)),
    version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    result jsonb,
    error_code text,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    completed_at timestamptz,
    lease_token uuid,
    lease_until timestamptz,
    attempt_count integer NOT NULL DEFAULT 0,
    next_attempt_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE UNIQUE INDEX uq_community_pending_change ON community.access_changes(server_id) WHERE phase=0;
CREATE TABLE community.access_change_acks (
    operation_id uuid NOT NULL REFERENCES community.access_changes(operation_id),
    batch_id uuid NOT NULL,
    owner text NOT NULL CHECK(owner IN('Community','Messaging')),
    routing_generation bigint NOT NULL CHECK(routing_generation=1),
    installed_scopes jsonb NOT NULL CHECK(jsonb_typeof(installed_scopes)='array'),
    acknowledged_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY(operation_id,owner)
);
CREATE FUNCTION community.protect_access_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.operation_id,NEW.server_id,NEW.actor_user_id,NEW.actor_session_id,NEW.actor_membership_id,
        NEW.routing_generation,NEW.owners,NEW.command,NEW.command_fingerprint,NEW.scopes,NEW.created_at)
        IS DISTINCT FROM
       (OLD.operation_id,OLD.server_id,OLD.actor_user_id,OLD.actor_session_id,OLD.actor_membership_id,
        OLD.routing_generation,OLD.owners,OLD.command,OLD.command_fingerprint,OLD.scopes,OLD.created_at)
       OR (OLD.phase<>0 AND NEW.phase<>OLD.phase) OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Accepted Community change is immutable and version must advance' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_access_change BEFORE UPDATE ON community.access_changes
    FOR EACH ROW EXECUTE FUNCTION community.protect_access_change();
CREATE FUNCTION community.protect_access_scope() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.authority,NEW.scope_kind,NEW.user_id,NEW.session_id,NEW.server_id,NEW.channel_id,NEW.membership_id,NEW.permission_code)
       IS DISTINCT FROM
       (OLD.authority,OLD.scope_kind,OLD.user_id,OLD.session_id,OLD.server_id,OLD.channel_id,OLD.membership_id,OLD.permission_code)
       OR NEW.current_epoch<OLD.current_epoch OR (OLD.retired AND NOT NEW.retired) OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Community scope is immutable and epoch cannot decrease' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_access_scope BEFORE UPDATE ON community.access_scopes
    FOR EACH ROW EXECUTE FUNCTION community.protect_access_scope();
INSERT INTO community.access_scopes(authority,scope_kind,user_id,server_id,membership_id,current_epoch,retired)
    SELECT 2,3,user_id,server_id,membership_id,1,status<>1 FROM community.server_members;
INSERT INTO community.access_scopes(authority,scope_kind,server_id,channel_id,current_epoch,retired)
    SELECT 2,6,server_id,space_id,1,status<>1 OR deleted_at IS NOT NULL FROM community.channels;
DO $$ DECLARE owner text; check_name text; BEGIN
    FOREACH owner IN ARRAY ARRAY['community','messaging'] LOOP
        -- Management changes can have an empty batch for Messaging. It still commits a durable receipt.
        FOR check_name IN SELECT c.conname FROM pg_constraint c
            WHERE c.conrelid=format('%I.fence_receipts',owner)::regclass AND c.contype='c'
            AND pg_get_constraintdef(c.oid) LIKE '%jsonb_array_length(installed_scopes)%' LOOP
            EXECUTE format('ALTER TABLE %I.fence_receipts DROP CONSTRAINT %I',owner,check_name);
        END LOOP;
        EXECUTE format('ALTER TABLE %I.fence_receipts ADD CONSTRAINT ck_receipt_scopes CHECK '
            || '(jsonb_typeof(installed_scopes)=''array'' AND (authority=2 OR jsonb_array_length(installed_scopes)>0))',owner);
        EXECUTE format('INSERT INTO %I.access_fences SELECT authority,scope_kind,user_id,session_id,server_id,channel_id,'
            || 'membership_id,permission_code,current_epoch,updated_at FROM community.access_scopes '
            || 'WHERE scope_kind<>5 OR %L=''community'' ON CONFLICT DO NOTHING',owner,owner);
    END LOOP;
END $$;

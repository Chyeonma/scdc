-- S2-09. Offline on S2-07; S2-08 may be applied independently.
LOCK TABLE community.channels,community.operations,messaging.spaces IN ACCESS EXCLUSIVE MODE;
CREATE TABLE community.channel_provisions (
    operation_id uuid PRIMARY KEY,
    channel_id uuid NOT NULL UNIQUE,
    server_id uuid NOT NULL,
    actor_user_id uuid NOT NULL,
    actor_session_id uuid,
    actor_security_stamp uuid,
    actor_access_expires_at timestamptz,
    actor_membership_id uuid,
    client_operation_id uuid,
    fingerprint bytea NOT NULL CHECK(octet_length(fingerprint)=32),
    key_id varchar(100) NOT NULL,
    fingerprint_version smallint NOT NULL DEFAULT 1 CHECK(fingerprint_version=1),
    name varchar(100) NOT NULL,
    name_key text COLLATE "C" NOT NULL,
    topic varchar(1000),
    created_at timestamptz NOT NULL,
    deadline timestamptz NOT NULL,
    phase smallint NOT NULL DEFAULT 0 CHECK(phase IN(0,1,2)),
    decision_version bigint NOT NULL DEFAULT 0,
    version bigint NOT NULL DEFAULT 1 CHECK(version>0),
    completed_at timestamptz,
    delivery_pending boolean NOT NULL DEFAULT true,
    lease_token uuid,
    lease_until timestamptz,
    next_attempt_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    CHECK((phase=0 AND decision_version=0 AND completed_at IS NULL
        AND actor_session_id IS NOT NULL AND actor_security_stamp IS NOT NULL
        AND actor_access_expires_at IS NOT NULL AND actor_membership_id IS NOT NULL)
        OR(phase IN(1,2) AND decision_version=1 AND completed_at IS NOT NULL)),
    UNIQUE(actor_user_id,server_id,client_operation_id)
);
CREATE UNIQUE INDEX ux_channel_provision_name ON community.channel_provisions(server_id,name_key) WHERE phase=0;
CREATE INDEX ix_channel_provision_work ON community.channel_provisions(next_attempt_at) WHERE delivery_pending;
CREATE TABLE messaging.channel_provisions (
    operation_id uuid PRIMARY KEY,
    channel_id uuid NOT NULL UNIQUE,
    server_id uuid,
    actor_user_id uuid,
    created_at timestamptz,
    fingerprint bytea NOT NULL CHECK(octet_length(fingerprint)=32),
    channel_epoch bigint NOT NULL CHECK(channel_epoch=1),
    routing_generation bigint NOT NULL CHECK(routing_generation=1),
    phase smallint NOT NULL CHECK(phase IN(0,1,2)),
    decision_version bigint NOT NULL,
    CHECK((phase=0 AND decision_version=0) OR(phase IN(1,2) AND decision_version=1)),
    CHECK(phase=2 OR(server_id IS NOT NULL AND actor_user_id IS NOT NULL AND created_at IS NOT NULL))
);
ALTER TABLE messaging.spaces ADD COLUMN provision_operation_id uuid,
    ADD COLUMN provision_state smallint NOT NULL DEFAULT 1 CHECK(provision_state IN(0,1)),
    ADD CHECK(provision_state=1 OR(space_type=3 AND provision_operation_id IS NOT NULL));
-- Backfill a stable mapping once. Preserve every UUID, old fingerprint/key, timestamp,
-- message/outbox row and migration checksum. Do not replay creation events.
DO $$ BEGIN
    IF EXISTS(SELECT resource_id FROM community.operations WHERE kind='create_channel' GROUP BY resource_id HAVING count(*)>1)
        OR EXISTS(SELECT 1 FROM community.channels c LEFT JOIN messaging.spaces s ON s.id=c.space_id
            WHERE c.kind=1 AND(s.id IS NULL OR s.space_type<>3)) THEN
        RAISE EXCEPTION 'Channel provisioning preflight: inconsistent historical channel mapping';
    END IF;
END $$;
INSERT INTO community.channel_provisions(operation_id,channel_id,server_id,actor_user_id,client_operation_id,
    fingerprint,key_id,fingerprint_version,name,name_key,topic,created_at,deadline,phase,decision_version,completed_at,delivery_pending)
SELECT uuidv7(),c.space_id,c.server_id,coalesce(o.actor_user_id,s.owner_user_id),o.client_operation_id,
    coalesce(o.fingerprint,decode(repeat('00',32),'hex')),coalesce(o.key_id,'legacy'),coalesce(o.fingerprint_version,1),
    c.name,c.name_key,c.topic,c.created_at,c.created_at,1,1,c.created_at,false
FROM community.channels c JOIN community.servers s ON s.id=c.server_id
LEFT JOIN community.operations o ON o.kind='create_channel' AND o.resource_id=c.space_id WHERE c.kind=1;
INSERT INTO messaging.channel_provisions(operation_id,channel_id,server_id,actor_user_id,created_at,
    fingerprint,channel_epoch,routing_generation,phase,decision_version)
SELECT operation_id,channel_id,server_id,actor_user_id,created_at,fingerprint,1,1,1,1 FROM community.channel_provisions;
-- Avoid the generic touch trigger: business versions/timestamps must not change.
ALTER TABLE messaging.spaces DISABLE TRIGGER tr_chat_spaces_touch;
UPDATE messaging.spaces s SET provision_operation_id=p.operation_id FROM messaging.channel_provisions p WHERE s.id=p.channel_id;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE messaging.spaces ENABLE TRIGGER tr_chat_spaces_touch;
SET CONSTRAINTS ALL DEFERRED;
CREATE FUNCTION community.protect_channel_provision() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.operation_id,NEW.channel_id,NEW.server_id,NEW.actor_user_id,NEW.actor_session_id,NEW.actor_security_stamp,
        NEW.actor_access_expires_at,NEW.actor_membership_id,NEW.client_operation_id,NEW.fingerprint,NEW.key_id,
        NEW.fingerprint_version,NEW.name,NEW.name_key,NEW.topic,NEW.created_at,NEW.deadline)
        IS DISTINCT FROM
       (OLD.operation_id,OLD.channel_id,OLD.server_id,OLD.actor_user_id,OLD.actor_session_id,OLD.actor_security_stamp,
        OLD.actor_access_expires_at,OLD.actor_membership_id,OLD.client_operation_id,OLD.fingerprint,OLD.key_id,
        OLD.fingerprint_version,OLD.name,OLD.name_key,OLD.topic,OLD.created_at,OLD.deadline)
        OR(OLD.phase<>0 AND(NEW.phase,NEW.decision_version,NEW.completed_at) IS DISTINCT FROM(OLD.phase,OLD.decision_version,OLD.completed_at))
        OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Channel identity/decision is immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_channel_provision BEFORE UPDATE ON community.channel_provisions
    FOR EACH ROW EXECUTE FUNCTION community.protect_channel_provision();
CREATE FUNCTION messaging.protect_channel_provision() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF (NEW.operation_id,NEW.channel_id,NEW.server_id,NEW.actor_user_id,NEW.created_at,NEW.fingerprint,NEW.channel_epoch,NEW.routing_generation)
        IS DISTINCT FROM(OLD.operation_id,OLD.channel_id,OLD.server_id,OLD.actor_user_id,OLD.created_at,OLD.fingerprint,OLD.channel_epoch,OLD.routing_generation)
        OR(OLD.phase<>0 AND(NEW.phase,NEW.decision_version) IS DISTINCT FROM(OLD.phase,OLD.decision_version)) THEN
        RAISE EXCEPTION 'Owner channel identity/decision is immutable' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_protect_channel_provision BEFORE UPDATE ON messaging.channel_provisions
    FOR EACH ROW EXECUTE FUNCTION messaging.protect_channel_provision();
-- Defense for future channel writers: prepared spaces cannot acquire messages,
-- membership or deliverable outbox entries before readiness has committed.
CREATE FUNCTION messaging.require_ready_channel_space() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF EXISTS(SELECT 1 FROM messaging.spaces WHERE id=NEW.space_id AND space_type=3 AND provision_state<>1) THEN
        RAISE EXCEPTION 'Channel space is not ready' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER tr_message_ready_channel BEFORE INSERT OR UPDATE ON messaging.messages
    FOR EACH ROW EXECUTE FUNCTION messaging.require_ready_channel_space();
CREATE TRIGGER tr_member_ready_channel BEFORE INSERT OR UPDATE ON messaging.space_members
    FOR EACH ROW EXECUTE FUNCTION messaging.require_ready_channel_space();
CREATE TRIGGER tr_outbox_ready_channel BEFORE INSERT OR UPDATE ON messaging.outbox_events
    FOR EACH ROW EXECUTE FUNCTION messaging.require_ready_channel_space();

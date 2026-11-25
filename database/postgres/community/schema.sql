-- Fresh Community database only. Generated from the clean main@7a2a88b schema on PostgreSQL 18.
CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;


CREATE SCHEMA community;



CREATE FUNCTION community.assert_server_invariants(checked_id uuid) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE role_record record; owner_id uuid;
BEGIN
    SELECT owner_user_id INTO owner_id FROM community.servers WHERE id = checked_id;
    IF NOT FOUND THEN RETURN; END IF;
    IF NOT EXISTS (SELECT 1 FROM community.server_members WHERE server_id = checked_id AND user_id = owner_id AND status = 1) THEN
        RAISE EXCEPTION 'Owner membership must be active' USING ERRCODE = '23514', CONSTRAINT = 'ck_server_owner_active';
    END IF;
    SELECT id, name, is_system INTO role_record FROM community.roles WHERE server_id = checked_id AND is_default;
    IF NOT FOUND OR role_record.name <> '@everyone' OR NOT role_record.is_system THEN
        RAISE EXCEPTION 'Server requires @everyone' USING ERRCODE = '23514', CONSTRAINT = 'ck_server_everyone';
    END IF;
    IF EXISTS (SELECT 1 FROM community.role_permissions WHERE role_id = role_record.id AND permission_code IN
        ('manage_channels','manage_invites','review_join_requests','manage_join_mode','manage_channel_access')) THEN
        RAISE EXCEPTION '@everyone cannot manage the server' USING ERRCODE = '23514', CONSTRAINT = 'ck_everyone_permissions';
    END IF;
END $$;



CREATE FUNCTION community.check_server_invariants() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE checked_id uuid;
BEGIN
    IF TG_TABLE_NAME = 'servers' THEN
        IF TG_OP <> 'INSERT' THEN PERFORM community.assert_server_invariants(OLD.id); END IF;
        IF TG_OP <> 'DELETE' THEN PERFORM community.assert_server_invariants(NEW.id); END IF;
    ELSIF TG_TABLE_NAME = 'role_permissions' THEN
        IF TG_OP <> 'INSERT' THEN
            SELECT server_id INTO checked_id FROM community.roles WHERE id = OLD.role_id;
            PERFORM community.assert_server_invariants(checked_id);
        END IF;
        IF TG_OP <> 'DELETE' THEN
            SELECT server_id INTO checked_id FROM community.roles WHERE id = NEW.role_id;
            PERFORM community.assert_server_invariants(checked_id);
        END IF;
    ELSE
        IF TG_OP <> 'INSERT' THEN PERFORM community.assert_server_invariants(OLD.server_id); END IF;
        IF TG_OP <> 'DELETE' THEN PERFORM community.assert_server_invariants(NEW.server_id); END IF;
    END IF;
    RETURN NULL;
END $$;



CREATE FUNCTION community.protect_access_change() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
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



CREATE FUNCTION community.protect_access_fence() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
            BEGIN
                IF NEW.min_epoch<OLD.min_epoch OR
                    (NEW.authority,NEW.scope_kind,NEW.user_id,NEW.session_id,NEW.server_id,NEW.channel_id,NEW.membership_id,NEW.permission_code)
                    IS DISTINCT FROM
                    (OLD.authority,OLD.scope_kind,OLD.user_id,OLD.session_id,OLD.server_id,OLD.channel_id,OLD.membership_id,OLD.permission_code) THEN
                    RAISE EXCEPTION 'Fence key is immutable and epoch cannot decrease' USING ERRCODE='23514';
                END IF;
                NEW.updated_at:=clock_timestamp();
                RETURN NEW;
            END $$;



CREATE FUNCTION community.protect_access_scope() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF (NEW.authority,NEW.scope_kind,NEW.user_id,NEW.session_id,NEW.server_id,NEW.channel_id,NEW.membership_id,NEW.permission_code)
       IS DISTINCT FROM
       (OLD.authority,OLD.scope_kind,OLD.user_id,OLD.session_id,OLD.server_id,OLD.channel_id,OLD.membership_id,OLD.permission_code)
       OR NEW.current_epoch<OLD.current_epoch OR (OLD.retired AND NOT NEW.retired) OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Community scope is immutable and epoch cannot decrease' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;



CREATE FUNCTION community.protect_channel_provision() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
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



CREATE FUNCTION community.touch_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    RETURN NEW;
END;
$$;



CREATE FUNCTION community.touch_updated_at_and_version() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    NEW.version := OLD.version + 1;
    RETURN NEW;
END;
$$;



CREATE FUNCTION community.utf16_length(value text) RETURNS integer
    LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE
    AS $$
    SELECT coalesce(sum(CASE WHEN ch = '' THEN 0 WHEN ascii(ch) > 65535 THEN 2 ELSE 1 END), 0)::integer
    FROM regexp_split_to_table(value, '') AS chars(ch)
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;


CREATE TABLE community.access_change_acks (
    operation_id uuid NOT NULL,
    batch_id uuid NOT NULL,
    owner text NOT NULL,
    routing_generation bigint NOT NULL,
    installed_scopes jsonb NOT NULL,
    acknowledged_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT access_change_acks_installed_scopes_check CHECK ((jsonb_typeof(installed_scopes) = 'array'::text)),
    CONSTRAINT access_change_acks_owner_check CHECK ((owner = ANY (ARRAY['Community'::text, 'Messaging'::text]))),
    CONSTRAINT access_change_acks_routing_generation_check CHECK ((routing_generation = 1))
);



CREATE TABLE community.access_changes (
    operation_id uuid NOT NULL,
    server_id uuid NOT NULL,
    actor_user_id uuid NOT NULL,
    actor_session_id uuid NOT NULL,
    actor_membership_id uuid NOT NULL,
    routing_generation bigint NOT NULL,
    owners text[] NOT NULL,
    command jsonb NOT NULL,
    command_fingerprint character(64) NOT NULL,
    scopes jsonb NOT NULL,
    phase smallint DEFAULT 0 NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    result jsonb,
    error_code text,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    completed_at timestamp with time zone,
    lease_token uuid,
    lease_until timestamp with time zone,
    attempt_count integer DEFAULT 0 NOT NULL,
    next_attempt_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT access_changes_command_check CHECK ((jsonb_typeof(command) = 'object'::text)),
    CONSTRAINT access_changes_command_fingerprint_check CHECK ((command_fingerprint ~ '^[0-9a-f]{64}$'::text)),
    CONSTRAINT access_changes_owners_check CHECK ((owners = ARRAY['Community'::text, 'Messaging'::text])),
    CONSTRAINT access_changes_phase_check CHECK ((phase = ANY (ARRAY[0, 1, 2]))),
    CONSTRAINT access_changes_routing_generation_check CHECK ((routing_generation = 1)),
    CONSTRAINT access_changes_scopes_check CHECK (((jsonb_typeof(scopes) = 'array'::text) AND (jsonb_array_length(scopes) > 0))),
    CONSTRAINT access_changes_version_check CHECK ((version > 0))
);



CREATE TABLE community.access_fences (
    authority smallint NOT NULL,
    scope_kind smallint NOT NULL,
    user_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    session_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    server_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    channel_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    membership_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    permission_code character varying(100) DEFAULT ''::character varying NOT NULL,
    min_epoch bigint NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT access_fences_min_epoch_check CHECK ((min_epoch > 0)),
    CONSTRAINT ck_fence_scope CHECK ((((authority = 1) AND (scope_kind = 1) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 1) AND (scope_kind = 2) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 3) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 4) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 5) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text ~ '^[a-z][a-z0-9_]*$'::text)) OR ((authority = 2) AND (scope_kind = 6) AND (user_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text))))
);



CREATE TABLE community.access_scopes (
    authority smallint CONSTRAINT access_fences_authority_not_null NOT NULL,
    scope_kind smallint CONSTRAINT access_fences_scope_kind_not_null NOT NULL,
    user_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid CONSTRAINT access_fences_user_id_not_null NOT NULL,
    session_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid CONSTRAINT access_fences_session_id_not_null NOT NULL,
    server_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid CONSTRAINT access_fences_server_id_not_null NOT NULL,
    channel_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid CONSTRAINT access_fences_channel_id_not_null NOT NULL,
    membership_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid CONSTRAINT access_fences_membership_id_not_null NOT NULL,
    permission_code character varying(100) DEFAULT ''::character varying CONSTRAINT access_fences_permission_code_not_null NOT NULL,
    current_epoch bigint CONSTRAINT access_fences_min_epoch_not_null NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() CONSTRAINT access_fences_updated_at_not_null NOT NULL,
    pending_operation_id uuid,
    retired boolean DEFAULT false NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    CONSTRAINT access_fences_min_epoch_check CHECK ((current_epoch > 0)),
    CONSTRAINT access_scopes_authority_check CHECK ((authority = 2)),
    CONSTRAINT access_scopes_version_check CHECK ((version > 0)),
    CONSTRAINT ck_fence_scope CHECK ((((authority = 1) AND (scope_kind = 1) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 1) AND (scope_kind = 2) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 3) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 4) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text)) OR ((authority = 2) AND (scope_kind = 5) AND (user_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text ~ '^[a-z][a-z0-9_]*$'::text)) OR ((authority = 2) AND (scope_kind = 6) AND (user_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid) AND (server_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (channel_id <> '00000000-0000-0000-0000-000000000000'::uuid) AND (membership_id = '00000000-0000-0000-0000-000000000000'::uuid) AND ((permission_code)::text = ''::text))))
);



CREATE TABLE community.bans (
    server_id uuid NOT NULL,
    user_id uuid NOT NULL,
    banned_by_user_id uuid NOT NULL,
    reason character varying(500),
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    expires_at timestamp with time zone,
    revoked_at timestamp with time zone,
    revoked_by_user_id uuid,
    CONSTRAINT ck_server_bans_expiry CHECK (((expires_at IS NULL) OR (expires_at > created_at)))
);



CREATE TABLE community.channel_provisions (
    operation_id uuid NOT NULL,
    channel_id uuid NOT NULL,
    server_id uuid NOT NULL,
    actor_user_id uuid NOT NULL,
    actor_session_id uuid,
    actor_security_stamp uuid,
    actor_access_expires_at timestamp with time zone,
    actor_membership_id uuid,
    client_operation_id uuid,
    fingerprint bytea NOT NULL,
    key_id character varying(100) NOT NULL,
    fingerprint_version smallint DEFAULT 1 NOT NULL,
    name character varying(100) NOT NULL,
    name_key text NOT NULL COLLATE pg_catalog."C",
    topic character varying(1000),
    created_at timestamp with time zone NOT NULL,
    deadline timestamp with time zone NOT NULL,
    phase smallint DEFAULT 0 NOT NULL,
    decision_version bigint DEFAULT 0 NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    completed_at timestamp with time zone,
    delivery_pending boolean DEFAULT true NOT NULL,
    lease_token uuid,
    lease_until timestamp with time zone,
    next_attempt_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT channel_provisions_check CHECK ((((phase = 0) AND (decision_version = 0) AND (completed_at IS NULL) AND (actor_session_id IS NOT NULL) AND (actor_security_stamp IS NOT NULL) AND (actor_access_expires_at IS NOT NULL) AND (actor_membership_id IS NOT NULL)) OR ((phase = ANY (ARRAY[1, 2])) AND (decision_version = 1) AND (completed_at IS NOT NULL)))),
    CONSTRAINT channel_provisions_fingerprint_check CHECK ((octet_length(fingerprint) = 32)),
    CONSTRAINT channel_provisions_fingerprint_version_check CHECK ((fingerprint_version = 1)),
    CONSTRAINT channel_provisions_phase_check CHECK ((phase = ANY (ARRAY[0, 1, 2]))),
    CONSTRAINT channel_provisions_version_check CHECK ((version > 0))
);



CREATE TABLE community.channel_role_overrides (
    space_id uuid NOT NULL,
    server_id uuid NOT NULL,
    role_id uuid NOT NULL,
    permission_code character varying(50) NOT NULL,
    effect smallint NOT NULL,
    CONSTRAINT ck_channel_role_overrides_effect CHECK ((effect = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_role_override_catalog CHECK (((permission_code)::text = 'channel_view'::text))
);



CREATE TABLE community.channel_user_overrides (
    space_id uuid NOT NULL,
    server_id uuid NOT NULL,
    user_id uuid NOT NULL,
    permission_code character varying(50) NOT NULL,
    effect smallint NOT NULL,
    membership_id uuid NOT NULL,
    CONSTRAINT ck_channel_user_overrides_effect CHECK ((effect = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_user_override_catalog CHECK (((permission_code)::text = 'channel_view'::text))
);



CREATE TABLE community.channels (
    space_id uuid NOT NULL,
    space_type smallint GENERATED ALWAYS AS ((3)::smallint) STORED,
    server_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    topic character varying(1000),
    "position" integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    name_key text NOT NULL COLLATE pg_catalog."C",
    kind smallint DEFAULT 1 NOT NULL,
    default_view smallint DEFAULT 1 NOT NULL,
    status smallint DEFAULT 1 NOT NULL,
    deleted_at timestamp with time zone,
    version integer DEFAULT 1 NOT NULL,
    access_version integer DEFAULT 1 NOT NULL,
    normalized_name text GENERATED ALWAYS AS (lower(btrim((name)::text))) STORED,
    CONSTRAINT ck_channel_default_view CHECK ((default_view = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_channel_kind CHECK ((kind = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_channel_name CHECK (((community.utf16_length((name)::text) >= 1) AND (community.utf16_length((name)::text) <= 100))),
    CONSTRAINT ck_channel_name_key CHECK ((name_key <> ''::text)),
    CONSTRAINT ck_channel_status CHECK ((((status = 1) AND (deleted_at IS NULL)) OR ((status = 3) AND (deleted_at IS NOT NULL)))),
    CONSTRAINT ck_channel_topic CHECK (((topic IS NULL) OR (community.utf16_length((topic)::text) <= 1000))),
    CONSTRAINT ck_channel_versions CHECK (((version >= 1) AND (access_version >= 1))),
    CONSTRAINT ck_server_channels_position CHECK (("position" >= 0))
);



CREATE TABLE community.fence_receipts (
    authority smallint NOT NULL,
    operation_id uuid NOT NULL,
    batch_id uuid NOT NULL,
    command_fingerprint character(64) NOT NULL,
    routing_generation bigint NOT NULL,
    installed_scopes jsonb NOT NULL,
    installed_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_receipt_scopes CHECK (((jsonb_typeof(installed_scopes) = 'array'::text) AND ((authority = 2) OR (jsonb_array_length(installed_scopes) > 0)))),
    CONSTRAINT fence_receipts_authority_check CHECK ((authority = ANY (ARRAY[1, 2]))),
    CONSTRAINT fence_receipts_batch_id_check CHECK ((batch_id <> '00000000-0000-0000-0000-000000000000'::uuid)),
    CONSTRAINT fence_receipts_command_fingerprint_check CHECK ((command_fingerprint ~ '^[0-9a-f]{64}$'::text)),
    CONSTRAINT fence_receipts_operation_id_check CHECK ((operation_id <> '00000000-0000-0000-0000-000000000000'::uuid)),
    CONSTRAINT fence_receipts_routing_generation_check CHECK ((routing_generation > 0))
);



CREATE TABLE community.inbox_events (
    consumer_name character varying(100) NOT NULL,
    event_id uuid NOT NULL,
    received_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    processed_at timestamp with time zone,
    payload_hash character(64),
    last_error text
);



CREATE TABLE community.invites (
    id uuid DEFAULT uuidv7() NOT NULL,
    server_id uuid NOT NULL,
    code_hash character(64) NOT NULL,
    created_by_user_id uuid NOT NULL,
    default_role_id uuid,
    max_uses integer,
    use_count integer DEFAULT 0 NOT NULL,
    expires_at timestamp with time zone,
    revoked_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_server_invites_expiry CHECK (((expires_at IS NULL) OR (expires_at > created_at))),
    CONSTRAINT ck_server_invites_max_uses CHECK (((max_uses IS NULL) OR (max_uses > 0))),
    CONSTRAINT ck_server_invites_use_count CHECK (((use_count >= 0) AND ((max_uses IS NULL) OR (use_count <= max_uses))))
);



CREATE TABLE community.member_roles (
    server_id uuid NOT NULL,
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    assigned_by_user_id uuid,
    assigned_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    membership_id uuid NOT NULL
);



CREATE TABLE community.operations (
    actor_user_id uuid NOT NULL,
    kind character varying(50) NOT NULL,
    scope_id uuid NOT NULL,
    client_operation_id uuid NOT NULL,
    fingerprint_version smallint NOT NULL,
    key_id character varying(100) NOT NULL,
    fingerprint bytea NOT NULL,
    resource_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_community_operations_fingerprint CHECK (((octet_length(fingerprint) = 32) AND (fingerprint_version >= 1))),
    CONSTRAINT ck_community_operations_key CHECK ((length((key_id)::text) > 0)),
    CONSTRAINT ck_community_operations_kind CHECK (((((kind)::text = 'create_server'::text) AND (scope_id = '00000000-0000-0000-0000-000000000000'::uuid)) OR (((kind)::text = ANY ((ARRAY['create_role'::character varying, 'create_channel'::character varying])::text[])) AND (scope_id <> '00000000-0000-0000-0000-000000000000'::uuid))))
);



CREATE TABLE community.outbox_events (
    id uuid DEFAULT uuidv7() NOT NULL,
    event_type character varying(100) NOT NULL,
    aggregate_type character varying(50) NOT NULL,
    aggregate_id uuid NOT NULL,
    aggregate_version integer,
    space_id uuid,
    payload jsonb NOT NULL,
    occurred_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    available_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    published_at timestamp with time zone,
    attempt_count integer DEFAULT 0 NOT NULL,
    last_error text,
    lease_token uuid,
    lease_expires_at timestamp with time zone,
    CONSTRAINT ck_outbox_events_attempts CHECK ((attempt_count >= 0)),
    CONSTRAINT ck_outbox_events_available CHECK ((available_at >= occurred_at)),
    CONSTRAINT ck_outbox_events_lease CHECK (((lease_token IS NULL) = (lease_expires_at IS NULL))),
    CONSTRAINT ck_outbox_events_payload CHECK ((jsonb_typeof(payload) = 'object'::text))
);



CREATE TABLE community.permissions (
    code character varying(50) NOT NULL,
    description character varying(255) NOT NULL
);



CREATE TABLE community.role_permissions (
    role_id uuid NOT NULL,
    permission_code character varying(50) NOT NULL,
    CONSTRAINT ck_role_management_catalog CHECK (((permission_code)::text = ANY ((ARRAY['manage_channels'::character varying, 'manage_invites'::character varying, 'review_join_requests'::character varying, 'manage_join_mode'::character varying, 'manage_channel_access'::character varying])::text[])))
);



CREATE TABLE community.roles (
    id uuid DEFAULT uuidv7() NOT NULL,
    server_id uuid NOT NULL,
    name character varying(64) NOT NULL,
    color character varying(7),
    "position" integer DEFAULT 0 NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    is_system boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    name_key text NOT NULL COLLATE pg_catalog."C",
    version integer DEFAULT 1 NOT NULL,
    normalized_name text GENERATED ALWAYS AS (lower(btrim((name)::text))) STORED,
    CONSTRAINT ck_role_name_key CHECK ((name_key <> ''::text)),
    CONSTRAINT ck_role_version CHECK ((version >= 1)),
    CONSTRAINT ck_roles_system_default CHECK ((((NOT is_system) AND (NOT is_default)) OR (is_system AND is_default AND ((name)::text = '@everyone'::text)))),
    CONSTRAINT ck_server_roles_color CHECK (((color IS NULL) OR ((color)::text ~ '^#[0-9A-Fa-f]{6}$'::text))),
    CONSTRAINT ck_server_roles_name CHECK (((community.utf16_length((name)::text) >= 1) AND (community.utf16_length((name)::text) <= 64))),
    CONSTRAINT ck_server_roles_position CHECK (("position" >= 0))
);



CREATE TABLE community.schema_migrations (
    module character varying(50) NOT NULL,
    version integer NOT NULL,
    checksum character(64) NOT NULL,
    applied_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL
);



CREATE TABLE community.server_members (
    server_id uuid NOT NULL,
    user_id uuid NOT NULL,
    nickname character varying(64),
    status smallint DEFAULT 1 NOT NULL,
    joined_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    left_at timestamp with time zone,
    timeout_until timestamp with time zone,
    invited_by_user_id uuid,
    membership_id uuid DEFAULT uuidv7() NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    CONSTRAINT ck_server_members_left CHECK ((((status = 1) AND (left_at IS NULL)) OR ((status = ANY (ARRAY[2, 3, 4])) AND (left_at IS NOT NULL)))),
    CONSTRAINT ck_server_members_nickname CHECK (((nickname IS NULL) OR ((char_length(btrim((nickname)::text)) >= 1) AND (char_length(btrim((nickname)::text)) <= 64)))),
    CONSTRAINT ck_server_members_status CHECK ((status = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_server_members_version CHECK ((version >= 1))
);



CREATE TABLE community.servers (
    id uuid DEFAULT uuidv7() NOT NULL,
    owner_user_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    normalized_name character varying(100) GENERATED ALWAYS AS (lower(btrim((name)::text))) STORED,
    slug character varying(100) NOT NULL,
    description character varying(1000),
    avatar_object_key character varying(500),
    banner_object_key character varying(500),
    status smallint DEFAULT 1 NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    deleted_at timestamp with time zone,
    version integer DEFAULT 1 NOT NULL,
    visibility smallint DEFAULT 2 NOT NULL,
    join_mode smallint DEFAULT 1 NOT NULL,
    access_version integer DEFAULT 1 NOT NULL,
    search_name text NOT NULL COLLATE pg_catalog."C",
    CONSTRAINT ck_servers_access_version CHECK ((access_version >= 1)),
    CONSTRAINT ck_servers_deleted CHECK (((deleted_at IS NULL) OR (status = 3))),
    CONSTRAINT ck_servers_description CHECK (((description IS NULL) OR (community.utf16_length((description)::text) <= 1000))),
    CONSTRAINT ck_servers_join_mode CHECK ((join_mode = ANY (ARRAY[1, 2]))),
    CONSTRAINT ck_servers_name CHECK (((community.utf16_length((name)::text) >= 2) AND (community.utf16_length((name)::text) <= 100))),
    CONSTRAINT ck_servers_search_name CHECK ((search_name <> ''::text)),
    CONSTRAINT ck_servers_slug CHECK (((slug)::text ~ '^[a-z0-9][a-z0-9-]{1,98}[a-z0-9]$'::text)),
    CONSTRAINT ck_servers_status CHECK ((status = ANY (ARRAY[1, 2, 3]))),
    CONSTRAINT ck_servers_version CHECK ((version >= 1)),
    CONSTRAINT ck_servers_visibility CHECK ((visibility = ANY (ARRAY[1, 2])))
);



ALTER TABLE ONLY community.access_change_acks
    ADD CONSTRAINT access_change_acks_pkey PRIMARY KEY (operation_id, owner);



ALTER TABLE ONLY community.access_changes
    ADD CONSTRAINT access_changes_pkey PRIMARY KEY (operation_id);



ALTER TABLE ONLY community.access_fences
    ADD CONSTRAINT access_fences_pkey PRIMARY KEY (authority, scope_kind, user_id, session_id, server_id, channel_id, membership_id, permission_code);



ALTER TABLE ONLY community.access_scopes
    ADD CONSTRAINT access_scopes_pkey PRIMARY KEY (authority, scope_kind, user_id, session_id, server_id, channel_id, membership_id, permission_code);



ALTER TABLE ONLY community.bans
    ADD CONSTRAINT bans_pkey PRIMARY KEY (server_id, user_id);



ALTER TABLE ONLY community.channel_provisions
    ADD CONSTRAINT channel_provisions_actor_user_id_server_id_client_operation_key UNIQUE (actor_user_id, server_id, client_operation_id);



ALTER TABLE ONLY community.channel_provisions
    ADD CONSTRAINT channel_provisions_channel_id_key UNIQUE (channel_id);



ALTER TABLE ONLY community.channel_provisions
    ADD CONSTRAINT channel_provisions_pkey PRIMARY KEY (operation_id);



ALTER TABLE ONLY community.channel_role_overrides
    ADD CONSTRAINT channel_role_overrides_pkey PRIMARY KEY (space_id, role_id, permission_code);



ALTER TABLE ONLY community.channel_user_overrides
    ADD CONSTRAINT channel_user_overrides_pkey PRIMARY KEY (space_id, user_id, permission_code);



ALTER TABLE ONLY community.channels
    ADD CONSTRAINT channels_pkey PRIMARY KEY (space_id);



ALTER TABLE ONLY community.fence_receipts
    ADD CONSTRAINT fence_receipts_pkey PRIMARY KEY (authority, operation_id, batch_id);



ALTER TABLE ONLY community.inbox_events
    ADD CONSTRAINT inbox_events_pkey PRIMARY KEY (consumer_name, event_id);



ALTER TABLE ONLY community.invites
    ADD CONSTRAINT invites_pkey PRIMARY KEY (id);



ALTER TABLE ONLY community.member_roles
    ADD CONSTRAINT member_roles_pkey PRIMARY KEY (server_id, user_id, role_id);



ALTER TABLE ONLY community.outbox_events
    ADD CONSTRAINT outbox_events_pkey PRIMARY KEY (id);



ALTER TABLE ONLY community.permissions
    ADD CONSTRAINT permissions_pkey PRIMARY KEY (code);



ALTER TABLE ONLY community.operations
    ADD CONSTRAINT pk_community_operations PRIMARY KEY (actor_user_id, kind, scope_id, client_operation_id);



ALTER TABLE ONLY community.role_permissions
    ADD CONSTRAINT role_permissions_pkey PRIMARY KEY (role_id, permission_code);



ALTER TABLE ONLY community.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);



ALTER TABLE ONLY community.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (module, version);



ALTER TABLE ONLY community.server_members
    ADD CONSTRAINT server_members_pkey PRIMARY KEY (server_id, user_id);



ALTER TABLE ONLY community.servers
    ADD CONSTRAINT servers_pkey PRIMARY KEY (id);



ALTER TABLE ONLY community.channels
    ADD CONSTRAINT uq_server_channels_space_server UNIQUE (space_id, server_id);



ALTER TABLE ONLY community.roles
    ADD CONSTRAINT uq_server_roles_id_server UNIQUE (id, server_id);



ALTER TABLE ONLY community.server_members
    ADD CONSTRAINT ux_member_epoch UNIQUE (server_id, user_id, membership_id);



ALTER TABLE ONLY community.roles
    ADD CONSTRAINT ux_role_name_key UNIQUE (server_id, name_key);



ALTER TABLE ONLY community.invites
    ADD CONSTRAINT ux_server_invites_hash UNIQUE (code_hash);



ALTER TABLE ONLY community.server_members
    ADD CONSTRAINT ux_server_members_membership UNIQUE (membership_id);



ALTER TABLE ONLY community.servers
    ADD CONSTRAINT ux_servers_slug UNIQUE (slug);



CREATE INDEX inbox_events_consumer_name_received_at_idx ON community.inbox_events USING btree (consumer_name, received_at) WHERE (processed_at IS NULL);



CREATE INDEX ix_channel_provision_work ON community.channel_provisions USING btree (next_attempt_at) WHERE delivery_pending;



CREATE INDEX ix_server_channels_order ON community.channels USING btree (server_id, "position", space_id);



CREATE INDEX ix_server_invites_active ON community.invites USING btree (server_id, expires_at) WHERE (revoked_at IS NULL);



CREATE INDEX ix_server_member_roles_role ON community.member_roles USING btree (role_id);



CREATE INDEX ix_server_members_active_user ON community.server_members USING btree (user_id, server_id) WHERE (status = 1);



CREATE INDEX ix_servers_name_trgm ON community.servers USING gin (normalized_name public.gin_trgm_ops) WHERE (status = 1);



CREATE INDEX ix_servers_owner ON community.servers USING btree (owner_user_id);



CREATE INDEX ix_servers_public_search ON community.servers USING btree (search_name, id) WHERE ((visibility = 1) AND (status = 1) AND (deleted_at IS NULL));



CREATE INDEX outbox_events_aggregate_type_aggregate_id_aggregate_version_idx ON community.outbox_events USING btree (aggregate_type, aggregate_id, aggregate_version);



CREATE INDEX outbox_events_available_at_lease_expires_at_occurred_at_id_idx ON community.outbox_events USING btree (available_at, lease_expires_at, occurred_at, id) WHERE ((published_at IS NULL) AND ((event_type)::text = 'Messaging.MessageChanged'::text));



CREATE INDEX outbox_events_available_at_occurred_at_idx ON community.outbox_events USING btree (available_at, occurred_at) WHERE (published_at IS NULL);



CREATE UNIQUE INDEX uq_community_pending_change ON community.access_changes USING btree (server_id) WHERE (phase = 0);



CREATE UNIQUE INDEX ux_channel_active_name_key ON community.channels USING btree (server_id, name_key) WHERE ((status = 1) AND (deleted_at IS NULL));



CREATE UNIQUE INDEX ux_channel_provision_name ON community.channel_provisions USING btree (server_id, name_key) WHERE (phase = 0);



CREATE UNIQUE INDEX ux_server_roles_default ON community.roles USING btree (server_id) WHERE is_default;



CREATE CONSTRAINT TRIGGER ct_member_invariants AFTER INSERT OR DELETE OR UPDATE ON community.server_members DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();



CREATE CONSTRAINT TRIGGER ct_role_invariants AFTER INSERT OR DELETE OR UPDATE ON community.roles DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();



CREATE CONSTRAINT TRIGGER ct_role_permission_invariants AFTER INSERT OR DELETE OR UPDATE ON community.role_permissions DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();



CREATE CONSTRAINT TRIGGER ct_server_invariants AFTER INSERT OR DELETE OR UPDATE ON community.servers DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();



CREATE TRIGGER tr_protect_access_change BEFORE UPDATE ON community.access_changes FOR EACH ROW EXECUTE FUNCTION community.protect_access_change();



CREATE TRIGGER tr_protect_access_fence BEFORE UPDATE ON community.access_fences FOR EACH ROW EXECUTE FUNCTION community.protect_access_fence();



CREATE TRIGGER tr_protect_access_scope BEFORE UPDATE ON community.access_scopes FOR EACH ROW EXECUTE FUNCTION community.protect_access_scope();



CREATE TRIGGER tr_protect_channel_provision BEFORE UPDATE ON community.channel_provisions FOR EACH ROW EXECUTE FUNCTION community.protect_channel_provision();



CREATE TRIGGER tr_server_channels_touch BEFORE UPDATE ON community.channels FOR EACH ROW EXECUTE FUNCTION community.touch_updated_at_and_version();



CREATE TRIGGER tr_server_roles_touch BEFORE UPDATE ON community.roles FOR EACH ROW EXECUTE FUNCTION community.touch_updated_at_and_version();



CREATE TRIGGER tr_servers_touch BEFORE UPDATE ON community.servers FOR EACH ROW EXECUTE FUNCTION community.touch_updated_at_and_version();



ALTER TABLE ONLY community.access_change_acks
    ADD CONSTRAINT access_change_acks_operation_id_fkey FOREIGN KEY (operation_id) REFERENCES community.access_changes(operation_id);



ALTER TABLE ONLY community.channel_role_overrides
    ADD CONSTRAINT fk_channel_role_overrides_channel FOREIGN KEY (space_id, server_id) REFERENCES community.channels(space_id, server_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.channel_role_overrides
    ADD CONSTRAINT fk_channel_role_overrides_permission FOREIGN KEY (permission_code) REFERENCES community.permissions(code) ON DELETE RESTRICT;



ALTER TABLE ONLY community.channel_role_overrides
    ADD CONSTRAINT fk_channel_role_overrides_role FOREIGN KEY (role_id, server_id) REFERENCES community.roles(id, server_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.channel_user_overrides
    ADD CONSTRAINT fk_channel_user_overrides_channel FOREIGN KEY (space_id, server_id) REFERENCES community.channels(space_id, server_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.channel_user_overrides
    ADD CONSTRAINT fk_channel_user_overrides_member FOREIGN KEY (server_id, user_id) REFERENCES community.server_members(server_id, user_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.channel_user_overrides
    ADD CONSTRAINT fk_channel_user_overrides_permission FOREIGN KEY (permission_code) REFERENCES community.permissions(code) ON DELETE RESTRICT;



ALTER TABLE ONLY community.member_roles
    ADD CONSTRAINT fk_member_role_epoch FOREIGN KEY (server_id, user_id, membership_id) REFERENCES community.server_members(server_id, user_id, membership_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.bans
    ADD CONSTRAINT fk_server_bans_server FOREIGN KEY (server_id) REFERENCES community.servers(id) ON DELETE CASCADE;



ALTER TABLE ONLY community.channels
    ADD CONSTRAINT fk_server_channels_server FOREIGN KEY (server_id) REFERENCES community.servers(id) ON DELETE RESTRICT;



ALTER TABLE ONLY community.invites
    ADD CONSTRAINT fk_server_invites_creator FOREIGN KEY (server_id, created_by_user_id) REFERENCES community.server_members(server_id, user_id) ON DELETE RESTRICT;



ALTER TABLE ONLY community.invites
    ADD CONSTRAINT fk_server_invites_default_role FOREIGN KEY (default_role_id, server_id) REFERENCES community.roles(id, server_id) ON DELETE RESTRICT;



ALTER TABLE ONLY community.member_roles
    ADD CONSTRAINT fk_server_member_roles_member FOREIGN KEY (server_id, user_id) REFERENCES community.server_members(server_id, user_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.member_roles
    ADD CONSTRAINT fk_server_member_roles_role FOREIGN KEY (role_id, server_id) REFERENCES community.roles(id, server_id) ON DELETE CASCADE;



ALTER TABLE ONLY community.server_members
    ADD CONSTRAINT fk_server_members_server FOREIGN KEY (server_id) REFERENCES community.servers(id) ON DELETE CASCADE;



ALTER TABLE ONLY community.role_permissions
    ADD CONSTRAINT fk_server_role_permissions_permission FOREIGN KEY (permission_code) REFERENCES community.permissions(code) ON DELETE RESTRICT;



ALTER TABLE ONLY community.role_permissions
    ADD CONSTRAINT fk_server_role_permissions_role FOREIGN KEY (role_id) REFERENCES community.roles(id) ON DELETE CASCADE;



ALTER TABLE ONLY community.roles
    ADD CONSTRAINT fk_server_roles_server FOREIGN KEY (server_id) REFERENCES community.servers(id) ON DELETE CASCADE;



ALTER TABLE ONLY community.servers
    ADD CONSTRAINT fk_servers_owner_membership FOREIGN KEY (id, owner_user_id) REFERENCES community.server_members(server_id, user_id) DEFERRABLE INITIALLY DEFERRED;



ALTER TABLE ONLY community.channel_user_overrides
    ADD CONSTRAINT fk_user_override_epoch FOREIGN KEY (server_id, user_id, membership_id) REFERENCES community.server_members(server_id, user_id, membership_id) ON DELETE CASCADE;

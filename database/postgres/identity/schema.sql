--
-- PostgreSQL database dump
--



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

--
-- Name: identity; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA identity;


--
-- Name: SCHEMA identity; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA identity IS 'Tai khoan, ho so, thong tin dang nhap va phien xac thuc.';


--
-- Name: enqueue_session_publication(); Type: FUNCTION; Schema: identity; Owner: -
--

CREATE FUNCTION identity.enqueue_session_publication() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE target uuid; BEGIN
    IF TG_TABLE_NAME='users' THEN target:=COALESCE(NEW.id,OLD.id);
    ELSE target:=COALESCE(NEW.user_id,OLD.user_id); END IF;
    INSERT INTO identity.session_publications(user_id) VALUES(target)
        ON CONFLICT(user_id) DO UPDATE SET version=identity.session_publications.version+1;
    RETURN NULL;
END $$;


--
-- Name: protect_access_change(); Type: FUNCTION; Schema: identity; Owner: -
--

CREATE FUNCTION identity.protect_access_change() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF (NEW.operation_id,NEW.user_id,NEW.routing_generation,NEW.owners,NEW.payload,NEW.command_fingerprint,NEW.created_at)
       IS DISTINCT FROM
       (OLD.operation_id,OLD.user_id,OLD.routing_generation,OLD.owners,OLD.payload,OLD.command_fingerprint,OLD.created_at)
       OR (OLD.phase<>0 AND NEW.phase<>OLD.phase) OR NEW.version<=OLD.version THEN
        RAISE EXCEPTION 'Accepted Identity change is immutable and version must advance' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;


--
-- Name: protect_access_scope(); Type: FUNCTION; Schema: identity; Owner: -
--

CREATE FUNCTION identity.protect_access_scope() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF (NEW.scope_kind,NEW.user_id,NEW.session_id) IS DISTINCT FROM (OLD.scope_kind,OLD.user_id,OLD.session_id)
        OR NEW.current_epoch<OLD.current_epoch OR (OLD.retired AND NOT NEW.retired) THEN
        RAISE EXCEPTION 'Identity scope is immutable and epoch cannot decrease' USING ERRCODE='23514';
    END IF;
    RETURN NEW;
END $$;


--
-- Name: touch_updated_at(); Type: FUNCTION; Schema: identity; Owner: -
--

CREATE FUNCTION identity.touch_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    RETURN NEW;
END;
$$;


--
-- Name: touch_updated_at_and_version(); Type: FUNCTION; Schema: identity; Owner: -
--

CREATE FUNCTION identity.touch_updated_at_and_version() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    NEW.version := OLD.version + 1;
    RETURN NEW;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: access_change_acks; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.access_change_acks (
    operation_id uuid NOT NULL,
    batch_id uuid NOT NULL,
    owner text NOT NULL,
    routing_generation bigint NOT NULL,
    installed_scopes jsonb NOT NULL,
    acknowledged_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT access_change_acks_owner_check CHECK ((owner = ANY (ARRAY['Community'::text, 'Messaging'::text]))),
    CONSTRAINT access_change_acks_routing_generation_check CHECK ((routing_generation = 1))
);


--
-- Name: access_changes; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.access_changes (
    operation_id uuid NOT NULL,
    user_id uuid NOT NULL,
    routing_generation bigint NOT NULL,
    owners text[] NOT NULL,
    phase smallint DEFAULT 0 NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    payload jsonb NOT NULL,
    command_fingerprint character(64) NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    committed_at timestamp with time zone,
    lease_token uuid,
    lease_until timestamp with time zone,
    attempt_count integer DEFAULT 0 NOT NULL,
    next_attempt_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT access_changes_command_fingerprint_check CHECK ((command_fingerprint ~ '^[0-9a-f]{64}$'::text)),
    CONSTRAINT access_changes_owners_check CHECK ((owners = ARRAY['Community'::text, 'Messaging'::text])),
    CONSTRAINT access_changes_payload_check CHECK ((jsonb_typeof(payload) = 'object'::text)),
    CONSTRAINT access_changes_phase_check CHECK ((phase = ANY (ARRAY[0, 1, 2]))),
    CONSTRAINT access_changes_routing_generation_check CHECK ((routing_generation = 1)),
    CONSTRAINT access_changes_version_check CHECK ((version > 0))
);


--
-- Name: access_scopes; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.access_scopes (
    scope_kind smallint NOT NULL,
    user_id uuid NOT NULL,
    session_id uuid DEFAULT '00000000-0000-0000-0000-000000000000'::uuid NOT NULL,
    current_epoch bigint DEFAULT 1 NOT NULL,
    pending_operation_id uuid,
    retired boolean DEFAULT false NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    CONSTRAINT access_scopes_check CHECK ((((scope_kind = 1) AND (session_id = '00000000-0000-0000-0000-000000000000'::uuid)) OR ((scope_kind = 2) AND (session_id <> '00000000-0000-0000-0000-000000000000'::uuid)))),
    CONSTRAINT access_scopes_current_epoch_check CHECK ((current_epoch > 0)),
    CONSTRAINT access_scopes_scope_kind_check CHECK ((scope_kind = ANY (ARRAY[1, 2]))),
    CONSTRAINT access_scopes_version_check CHECK ((version > 0))
);


--
-- Name: access_store_generation; Type: SEQUENCE; Schema: identity; Owner: -
--

CREATE SEQUENCE identity.access_store_generation
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: account_tokens; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.account_tokens (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    purpose smallint NOT NULL,
    token_hash character(64) NOT NULL,
    target_value character varying(254),
    created_by_ip inet,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    consumed_at timestamp with time zone,
    CONSTRAINT ck_account_tokens_consumed CHECK (((consumed_at IS NULL) OR (consumed_at >= created_at))),
    CONSTRAINT ck_account_tokens_expiry CHECK ((expires_at > created_at)),
    CONSTRAINT ck_account_tokens_purpose CHECK ((purpose = ANY (ARRAY[1, 2, 3, 4])))
);


--
-- Name: TABLE account_tokens; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.account_tokens IS '1=verify_email, 2=reset_password, 3=change_email, 4=unlock_account.';


--
-- Name: auth_identities; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.auth_identities (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    provider character varying(30) NOT NULL,
    provider_subject character varying(255) NOT NULL,
    provider_email character varying(254),
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    last_login_at timestamp with time zone
);


--
-- Name: TABLE auth_identities; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.auth_identities IS 'Lien ket Google, GitHub, Apple hoac OAuth/OIDC provider khac.';


--
-- Name: auth_sessions; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.auth_sessions (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    device_name character varying(100),
    user_agent character varying(500),
    created_by_ip inet,
    last_seen_ip inet,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    last_seen_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    revoked_at timestamp with time zone,
    revoke_reason character varying(100),
    CONSTRAINT ck_auth_sessions_expiry CHECK ((expires_at > created_at)),
    CONSTRAINT ck_auth_sessions_last_seen CHECK ((last_seen_at >= created_at)),
    CONSTRAINT ck_auth_sessions_revoked CHECK (((revoked_at IS NULL) OR (revoked_at >= created_at)))
);


--
-- Name: TABLE auth_sessions; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.auth_sessions IS 'Mot phien dang nhap tren mot thiet bi; logout bang cach revoke session.';


--
-- Name: inbox_events; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.inbox_events (
    consumer_name character varying(100) NOT NULL,
    event_id uuid NOT NULL,
    received_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    processed_at timestamp with time zone,
    payload_hash character(64),
    last_error text
);


--
-- Name: mfa_methods; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.mfa_methods (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    method_type smallint NOT NULL,
    label character varying(100),
    secret_ciphertext bytea,
    credential_data jsonb DEFAULT '{}'::jsonb NOT NULL,
    verified_at timestamp with time zone,
    last_used_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    revoked_at timestamp with time zone,
    CONSTRAINT ck_mfa_methods_data CHECK ((jsonb_typeof(credential_data) = 'object'::text)),
    CONSTRAINT ck_mfa_methods_revoked CHECK (((revoked_at IS NULL) OR (revoked_at >= created_at))),
    CONSTRAINT ck_mfa_methods_type CHECK ((method_type = ANY (ARRAY[1, 2])))
);


--
-- Name: TABLE mfa_methods; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.mfa_methods IS '1=TOTP, 2=WebAuthn; secret TOTP phai duoc ma hoa boi ung dung.';


--
-- Name: mfa_recovery_codes; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.mfa_recovery_codes (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    code_hash character(64) NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    used_at timestamp with time zone
);


--
-- Name: outbox_events; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.outbox_events (
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


--
-- Name: password_credentials; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.password_credentials (
    user_id uuid NOT NULL,
    password_hash text NOT NULL,
    hash_algorithm character varying(50) DEFAULT 'aspnetcore-identity-v3'::character varying NOT NULL,
    password_version integer DEFAULT 1 NOT NULL,
    password_changed_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    requires_change boolean DEFAULT false NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_password_credentials_hash CHECK ((char_length(password_hash) >= 20)),
    CONSTRAINT ck_password_credentials_version CHECK ((password_version >= 1))
);


--
-- Name: TABLE password_credentials; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.password_credentials IS 'Chi luu password hash; khong luu mat khau hoac salt rieng.';


--
-- Name: refresh_tokens; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.refresh_tokens (
    id uuid DEFAULT uuidv7() NOT NULL,
    session_id uuid NOT NULL,
    parent_token_id uuid,
    replaced_by_token_id uuid,
    token_hash character(64) NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    revoked_at timestamp with time zone,
    revoke_reason character varying(100),
    CONSTRAINT ck_refresh_tokens_expiry CHECK ((expires_at > created_at)),
    CONSTRAINT ck_refresh_tokens_revoked CHECK (((revoked_at IS NULL) OR (revoked_at >= created_at))),
    CONSTRAINT ck_refresh_tokens_used CHECK (((used_at IS NULL) OR (used_at >= created_at)))
);


--
-- Name: TABLE refresh_tokens; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.refresh_tokens IS 'Chuoi refresh-token rotation; database chi luu SHA-256 hash.';


--
-- Name: schema_migrations; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.schema_migrations (
    module character varying(50) NOT NULL,
    version integer NOT NULL,
    checksum character(64) NOT NULL,
    applied_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL
);


--
-- Name: security_events; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.security_events (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid,
    event_type character varying(50) NOT NULL,
    ip_address inet,
    user_agent character varying(500),
    metadata jsonb DEFAULT '{}'::jsonb NOT NULL,
    occurred_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_security_events_metadata CHECK ((jsonb_typeof(metadata) = 'object'::text))
);


--
-- Name: TABLE security_events; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.security_events IS 'Append-only audit; user_id co tinh khong co FK de giu lich su khi anonymize/xoa user.';


--
-- Name: session_publications; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.session_publications (
    user_id uuid NOT NULL,
    version bigint DEFAULT 1 NOT NULL,
    CONSTRAINT session_publications_version_check CHECK ((version > 0))
);


--
-- Name: user_emails; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.user_emails (
    id uuid DEFAULT uuidv7() NOT NULL,
    user_id uuid NOT NULL,
    email character varying(254) NOT NULL,
    normalized_email character varying(254) GENERATED ALWAYS AS (lower(btrim((email)::text))) STORED,
    is_primary boolean DEFAULT false NOT NULL,
    verified_at timestamp with time zone,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_user_emails_format CHECK ((((char_length((email)::text) >= 3) AND (char_length((email)::text) <= 254)) AND (POSITION(('@'::text) IN (email)) > 1)))
);


--
-- Name: TABLE user_emails; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.user_emails IS 'Email cua user; moi user co toi da mot email primary.';


--
-- Name: user_profiles; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.user_profiles (
    user_id uuid NOT NULL,
    display_name character varying(64) NOT NULL,
    display_name_search_key text DEFAULT ''::text NOT NULL COLLATE pg_catalog."C",
    bio character varying(500),
    avatar_object_key character varying(500),
    locale character varying(16) DEFAULT 'vi-VN'::character varying NOT NULL,
    timezone character varying(64) DEFAULT 'Asia/Ho_Chi_Minh'::character varying NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_user_profiles_bio CHECK (((bio IS NULL) OR (char_length((bio)::text) <= 500))),
    CONSTRAINT ck_user_profiles_display_name CHECK (((char_length(btrim((display_name)::text)) >= 1) AND (char_length(btrim((display_name)::text)) <= 64)))
);


--
-- Name: TABLE user_profiles; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.user_profiles IS 'Thong tin hien thi, tach khoi danh tinh dang nhap.';


--
-- Name: user_security_states; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.user_security_states (
    user_id uuid NOT NULL,
    security_stamp uuid DEFAULT gen_random_uuid() NOT NULL,
    failed_login_count integer DEFAULT 0 NOT NULL,
    last_failed_login_at timestamp with time zone,
    locked_until timestamp with time zone,
    last_successful_login_at timestamp with time zone,
    mfa_enabled boolean DEFAULT false NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    CONSTRAINT ck_user_security_states_failures CHECK ((failed_login_count >= 0))
);


--
-- Name: TABLE user_security_states; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.user_security_states IS 'Lockout, security stamp va trang thai MFA.';


--
-- Name: users; Type: TABLE; Schema: identity; Owner: -
--

CREATE TABLE identity.users (
    id uuid DEFAULT uuidv7() NOT NULL,
    username character varying(32) NOT NULL,
    normalized_username character varying(32) GENERATED ALWAYS AS (lower(btrim((username)::text))) STORED,
    status smallint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    updated_at timestamp with time zone DEFAULT clock_timestamp() NOT NULL,
    deleted_at timestamp with time zone,
    version integer DEFAULT 1 NOT NULL,
    CONSTRAINT ck_users_deleted_state CHECK (((deleted_at IS NULL) OR (status = 4))),
    CONSTRAINT ck_users_status CHECK ((status = ANY (ARRAY[0, 1, 2, 3, 4]))),
    CONSTRAINT ck_users_username CHECK (((username)::text ~ '^[A-Za-z0-9_.]{3,32}$'::text)),
    CONSTRAINT ck_users_version CHECK ((version >= 1))
);


--
-- Name: TABLE users; Type: COMMENT; Schema: identity; Owner: -
--

COMMENT ON TABLE identity.users IS 'Danh tinh va vong doi tai khoan. 0=pending, 1=active, 2=suspended, 3=disabled, 4=deleted.';


--
-- Name: v_active_sessions; Type: VIEW; Schema: identity; Owner: -
--

CREATE VIEW identity.v_active_sessions AS
 SELECT s.id AS session_id,
    s.user_id,
    u.username,
    s.device_name,
    s.created_by_ip,
    s.last_seen_ip,
    s.created_at,
    s.last_seen_at,
    s.expires_at
   FROM (identity.auth_sessions s
     JOIN identity.users u ON ((u.id = s.user_id)))
  WHERE ((s.revoked_at IS NULL) AND (s.expires_at > clock_timestamp()));


--
-- Name: v_user_accounts; Type: VIEW; Schema: identity; Owner: -
--

CREATE VIEW identity.v_user_accounts AS
 SELECT u.id,
    u.username,
    p.display_name,
    e.email AS primary_email,
    e.verified_at AS email_verified_at,
        CASE u.status
            WHEN 0 THEN 'pending_verification'::text
            WHEN 1 THEN 'active'::text
            WHEN 2 THEN 'suspended'::text
            WHEN 3 THEN 'disabled'::text
            WHEN 4 THEN 'deleted'::text
            ELSE NULL::text
        END AS status,
    s.failed_login_count,
    s.locked_until,
    s.mfa_enabled,
    u.created_at,
    u.updated_at
   FROM (((identity.users u
     LEFT JOIN identity.user_profiles p ON ((p.user_id = u.id)))
     LEFT JOIN identity.user_emails e ON (((e.user_id = u.id) AND e.is_primary)))
     LEFT JOIN identity.user_security_states s ON ((s.user_id = u.id)));


--
-- Name: access_change_acks access_change_acks_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.access_change_acks
    ADD CONSTRAINT access_change_acks_pkey PRIMARY KEY (operation_id, owner);


--
-- Name: access_changes access_changes_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.access_changes
    ADD CONSTRAINT access_changes_pkey PRIMARY KEY (operation_id);


--
-- Name: access_scopes access_scopes_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.access_scopes
    ADD CONSTRAINT access_scopes_pkey PRIMARY KEY (scope_kind, user_id, session_id);


--
-- Name: account_tokens account_tokens_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.account_tokens
    ADD CONSTRAINT account_tokens_pkey PRIMARY KEY (id);


--
-- Name: auth_identities auth_identities_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.auth_identities
    ADD CONSTRAINT auth_identities_pkey PRIMARY KEY (id);


--
-- Name: auth_sessions auth_sessions_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.auth_sessions
    ADD CONSTRAINT auth_sessions_pkey PRIMARY KEY (id);


--
-- Name: inbox_events inbox_events_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.inbox_events
    ADD CONSTRAINT inbox_events_pkey PRIMARY KEY (consumer_name, event_id);


--
-- Name: mfa_methods mfa_methods_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.mfa_methods
    ADD CONSTRAINT mfa_methods_pkey PRIMARY KEY (id);


--
-- Name: mfa_recovery_codes mfa_recovery_codes_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.mfa_recovery_codes
    ADD CONSTRAINT mfa_recovery_codes_pkey PRIMARY KEY (id);


--
-- Name: outbox_events outbox_events_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.outbox_events
    ADD CONSTRAINT outbox_events_pkey PRIMARY KEY (id);


--
-- Name: password_credentials password_credentials_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.password_credentials
    ADD CONSTRAINT password_credentials_pkey PRIMARY KEY (user_id);


--
-- Name: refresh_tokens refresh_tokens_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT refresh_tokens_pkey PRIMARY KEY (id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (module, version);


--
-- Name: security_events security_events_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.security_events
    ADD CONSTRAINT security_events_pkey PRIMARY KEY (id);


--
-- Name: session_publications session_publications_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.session_publications
    ADD CONSTRAINT session_publications_pkey PRIMARY KEY (user_id);


--
-- Name: refresh_tokens uq_refresh_tokens_id_session; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT uq_refresh_tokens_id_session UNIQUE (id, session_id);


--
-- Name: user_emails user_emails_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_emails
    ADD CONSTRAINT user_emails_pkey PRIMARY KEY (id);


--
-- Name: user_profiles user_profiles_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_profiles
    ADD CONSTRAINT user_profiles_pkey PRIMARY KEY (user_id);


--
-- Name: user_security_states user_security_states_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_security_states
    ADD CONSTRAINT user_security_states_pkey PRIMARY KEY (user_id);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: account_tokens ux_account_tokens_hash; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.account_tokens
    ADD CONSTRAINT ux_account_tokens_hash UNIQUE (token_hash);


--
-- Name: auth_identities ux_auth_identities_provider_subject; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.auth_identities
    ADD CONSTRAINT ux_auth_identities_provider_subject UNIQUE (provider, provider_subject);


--
-- Name: mfa_recovery_codes ux_mfa_recovery_codes_hash; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.mfa_recovery_codes
    ADD CONSTRAINT ux_mfa_recovery_codes_hash UNIQUE (code_hash);


--
-- Name: refresh_tokens ux_refresh_tokens_hash; Type: CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT ux_refresh_tokens_hash UNIQUE (token_hash);


--
-- Name: inbox_events_consumer_name_received_at_idx; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX inbox_events_consumer_name_received_at_idx ON identity.inbox_events USING btree (consumer_name, received_at) WHERE (processed_at IS NULL);


--
-- Name: ix_account_tokens_active; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_account_tokens_active ON identity.account_tokens USING btree (user_id, purpose, expires_at) WHERE (consumed_at IS NULL);


--
-- Name: ix_auth_identities_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_auth_identities_user ON identity.auth_identities USING btree (user_id);


--
-- Name: ix_auth_sessions_active_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_auth_sessions_active_user ON identity.auth_sessions USING btree (user_id, expires_at DESC) WHERE (revoked_at IS NULL);


--
-- Name: ix_mfa_methods_active_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_mfa_methods_active_user ON identity.mfa_methods USING btree (user_id) WHERE (revoked_at IS NULL);


--
-- Name: ix_mfa_recovery_codes_unused; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_mfa_recovery_codes_unused ON identity.mfa_recovery_codes USING btree (user_id) WHERE (used_at IS NULL);


--
-- Name: ix_refresh_tokens_active_session; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_refresh_tokens_active_session ON identity.refresh_tokens USING btree (session_id, expires_at DESC) WHERE (revoked_at IS NULL);


--
-- Name: ix_security_events_time_brin; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_security_events_time_brin ON identity.security_events USING brin (occurred_at);


--
-- Name: ix_security_events_type; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_security_events_type ON identity.security_events USING btree (event_type, occurred_at DESC);


--
-- Name: ix_security_events_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_security_events_user ON identity.security_events USING btree (user_id, occurred_at DESC);


--
-- Name: ix_user_emails_user; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_user_emails_user ON identity.user_emails USING btree (user_id);


--
-- Name: ix_users_active_username_trgm; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_users_active_username_trgm ON identity.users USING gin (normalized_username public.gin_trgm_ops) WHERE ((status = 1) AND (deleted_at IS NULL));


--
-- Name: ix_users_search_order; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX ix_users_search_order ON identity.users USING btree (normalized_username COLLATE "C", id) WHERE ((status = 1) AND (deleted_at IS NULL));


--
-- Name: outbox_events_aggregate_type_aggregate_id_aggregate_version_idx; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX outbox_events_aggregate_type_aggregate_id_aggregate_version_idx ON identity.outbox_events USING btree (aggregate_type, aggregate_id, aggregate_version);


--
-- Name: outbox_events_available_at_lease_expires_at_occurred_at_id_idx; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX outbox_events_available_at_lease_expires_at_occurred_at_id_idx ON identity.outbox_events USING btree (available_at, lease_expires_at, occurred_at, id) WHERE ((published_at IS NULL) AND ((event_type)::text = 'Messaging.MessageChanged'::text));


--
-- Name: outbox_events_available_at_occurred_at_idx; Type: INDEX; Schema: identity; Owner: -
--

CREATE INDEX outbox_events_available_at_occurred_at_idx ON identity.outbox_events USING btree (available_at, occurred_at) WHERE (published_at IS NULL);


--
-- Name: uq_identity_pending_change; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX uq_identity_pending_change ON identity.access_changes USING btree (user_id) WHERE (phase = 0);


--
-- Name: ux_refresh_tokens_parent; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX ux_refresh_tokens_parent ON identity.refresh_tokens USING btree (parent_token_id) WHERE (parent_token_id IS NOT NULL);


--
-- Name: ux_refresh_tokens_replacement; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX ux_refresh_tokens_replacement ON identity.refresh_tokens USING btree (replaced_by_token_id) WHERE (replaced_by_token_id IS NOT NULL);


--
-- Name: ux_user_emails_normalized_email; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX ux_user_emails_normalized_email ON identity.user_emails USING btree (normalized_email);


--
-- Name: ux_user_emails_primary; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX ux_user_emails_primary ON identity.user_emails USING btree (user_id) WHERE is_primary;


--
-- Name: ux_users_normalized_username; Type: INDEX; Schema: identity; Owner: -
--

CREATE UNIQUE INDEX ux_users_normalized_username ON identity.users USING btree (normalized_username);


--
-- Name: password_credentials tr_password_credentials_touch; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_password_credentials_touch BEFORE UPDATE ON identity.password_credentials FOR EACH ROW EXECUTE FUNCTION identity.touch_updated_at();


--
-- Name: access_changes tr_protect_access_change; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_protect_access_change BEFORE UPDATE ON identity.access_changes FOR EACH ROW EXECUTE FUNCTION identity.protect_access_change();


--
-- Name: access_scopes tr_protect_access_scope; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_protect_access_scope BEFORE UPDATE ON identity.access_scopes FOR EACH ROW EXECUTE FUNCTION identity.protect_access_scope();


--
-- Name: users tr_publish_account; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_publish_account AFTER INSERT OR DELETE OR UPDATE ON identity.users FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();


--
-- Name: user_emails tr_publish_email; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_publish_email AFTER INSERT OR DELETE OR UPDATE ON identity.user_emails FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();


--
-- Name: access_scopes tr_publish_scope; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_publish_scope AFTER INSERT OR DELETE OR UPDATE ON identity.access_scopes FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();


--
-- Name: auth_sessions tr_publish_session; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_publish_session AFTER INSERT OR DELETE OR UPDATE ON identity.auth_sessions FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();


--
-- Name: user_security_states tr_publish_stamp; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_publish_stamp AFTER INSERT OR DELETE OR UPDATE ON identity.user_security_states FOR EACH ROW EXECUTE FUNCTION identity.enqueue_session_publication();


--
-- Name: user_emails tr_user_emails_touch; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_user_emails_touch BEFORE UPDATE ON identity.user_emails FOR EACH ROW EXECUTE FUNCTION identity.touch_updated_at();


--
-- Name: user_profiles tr_user_profiles_touch; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_user_profiles_touch BEFORE UPDATE ON identity.user_profiles FOR EACH ROW EXECUTE FUNCTION identity.touch_updated_at();


--
-- Name: user_security_states tr_user_security_states_touch; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_user_security_states_touch BEFORE UPDATE ON identity.user_security_states FOR EACH ROW EXECUTE FUNCTION identity.touch_updated_at();


--
-- Name: users tr_users_touch; Type: TRIGGER; Schema: identity; Owner: -
--

CREATE TRIGGER tr_users_touch BEFORE UPDATE ON identity.users FOR EACH ROW EXECUTE FUNCTION identity.touch_updated_at_and_version();


--
-- Name: access_change_acks access_change_acks_operation_id_fkey; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.access_change_acks
    ADD CONSTRAINT access_change_acks_operation_id_fkey FOREIGN KEY (operation_id) REFERENCES identity.access_changes(operation_id);


--
-- Name: account_tokens fk_account_tokens_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.account_tokens
    ADD CONSTRAINT fk_account_tokens_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: auth_identities fk_auth_identities_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.auth_identities
    ADD CONSTRAINT fk_auth_identities_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: auth_sessions fk_auth_sessions_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.auth_sessions
    ADD CONSTRAINT fk_auth_sessions_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: mfa_methods fk_mfa_methods_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.mfa_methods
    ADD CONSTRAINT fk_mfa_methods_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: mfa_recovery_codes fk_mfa_recovery_codes_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.mfa_recovery_codes
    ADD CONSTRAINT fk_mfa_recovery_codes_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: password_credentials fk_password_credentials_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.password_credentials
    ADD CONSTRAINT fk_password_credentials_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: refresh_tokens fk_refresh_tokens_parent; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_parent FOREIGN KEY (parent_token_id, session_id) REFERENCES identity.refresh_tokens(id, session_id) ON DELETE RESTRICT;


--
-- Name: refresh_tokens fk_refresh_tokens_replacement; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_replacement FOREIGN KEY (replaced_by_token_id, session_id) REFERENCES identity.refresh_tokens(id, session_id) ON DELETE RESTRICT;


--
-- Name: refresh_tokens fk_refresh_tokens_session; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.refresh_tokens
    ADD CONSTRAINT fk_refresh_tokens_session FOREIGN KEY (session_id) REFERENCES identity.auth_sessions(id) ON DELETE CASCADE;


--
-- Name: user_emails fk_user_emails_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_emails
    ADD CONSTRAINT fk_user_emails_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: user_profiles fk_user_profiles_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_profiles
    ADD CONSTRAINT fk_user_profiles_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- Name: user_security_states fk_user_security_states_user; Type: FK CONSTRAINT; Schema: identity; Owner: -
--

ALTER TABLE ONLY identity.user_security_states
    ADD CONSTRAINT fk_user_security_states_user FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

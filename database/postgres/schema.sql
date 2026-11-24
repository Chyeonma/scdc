\set ON_ERROR_STOP on

BEGIN;

DROP SCHEMA IF EXISTS integration CASCADE;
DROP SCHEMA IF EXISTS audit CASCADE;
DROP SCHEMA IF EXISTS moderation CASCADE;
DROP SCHEMA IF EXISTS community CASCADE;
DROP SCHEMA IF EXISTS messaging CASCADE;
DROP SCHEMA IF EXISTS chat CASCADE;
DROP SCHEMA IF EXISTS identity CASCADE;
DROP SCHEMA IF EXISTS common CASCADE;

CREATE SCHEMA common;
CREATE SCHEMA identity;
CREATE SCHEMA messaging;
CREATE SCHEMA community;
CREATE SCHEMA moderation;
CREATE SCHEMA audit;
CREATE SCHEMA integration;

COMMENT ON SCHEMA identity IS 'Tai khoan, ho so, thong tin dang nhap va phien xac thuc.';
COMMENT ON SCHEMA messaging IS 'Khong gian tro chuyen, DM, group chat, tin nhan va tuong tac.';
COMMENT ON SCHEMA community IS 'Server, thanh vien, channel, role, permission, invite va ban.';
COMMENT ON SCHEMA moderation IS 'Bao cao noi dung va hanh dong kiem duyet.';
COMMENT ON SCHEMA audit IS 'Nhat ky bao mat va cac su kien can luu vet.';
COMMENT ON SCHEMA integration IS 'Transactional outbox/inbox cho xu ly bat dong bo.';

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE FUNCTION common.touch_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    RETURN NEW;
END;
$$;

CREATE FUNCTION common.touch_updated_at_and_version()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := clock_timestamp();
    NEW.version := OLD.version + 1;
    RETURN NEW;
END;
$$;

-- ============================================================================
-- Identity and authentication
-- ============================================================================

CREATE TABLE identity.users (
    id                  uuid PRIMARY KEY DEFAULT uuidv7(),
    username            varchar(32) NOT NULL,
    normalized_username varchar(32) GENERATED ALWAYS AS (lower(btrim(username))) STORED,
    status              smallint NOT NULL DEFAULT 0,
    created_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    deleted_at          timestamptz,
    version             integer NOT NULL DEFAULT 1,
    CONSTRAINT ck_users_username CHECK (username ~ '^[A-Za-z0-9_.]{3,32}$'),
    CONSTRAINT ck_users_status CHECK (status IN (0, 1, 2, 3, 4)),
    CONSTRAINT ck_users_version CHECK (version >= 1),
    CONSTRAINT ck_users_deleted_state CHECK (deleted_at IS NULL OR status = 4)
);

CREATE UNIQUE INDEX ux_users_normalized_username
    ON identity.users (normalized_username);
CREATE INDEX ix_users_search_order ON identity.users (normalized_username COLLATE "C", id)
    WHERE status = 1 AND deleted_at IS NULL;
CREATE INDEX ix_users_active_username_trgm
    ON identity.users USING gin (normalized_username gin_trgm_ops)
    WHERE status = 1 AND deleted_at IS NULL;

COMMENT ON TABLE identity.users IS 'Danh tinh va vong doi tai khoan. 0=pending, 1=active, 2=suspended, 3=disabled, 4=deleted.';

CREATE TRIGGER tr_users_touch
BEFORE UPDATE ON identity.users
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at_and_version();

CREATE TABLE identity.user_profiles (
    user_id           uuid PRIMARY KEY,
    display_name      varchar(64) NOT NULL,
    display_name_search_key text COLLATE "C" NOT NULL DEFAULT '',
    bio               varchar(500),
    avatar_object_key varchar(500),
    locale            varchar(16) NOT NULL DEFAULT 'vi-VN',
    timezone          varchar(64) NOT NULL DEFAULT 'Asia/Ho_Chi_Minh',
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_user_profiles_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_user_profiles_display_name CHECK (char_length(btrim(display_name)) BETWEEN 1 AND 64),
    CONSTRAINT ck_user_profiles_bio CHECK (bio IS NULL OR char_length(bio) <= 500)
);

COMMENT ON TABLE identity.user_profiles IS 'Thong tin hien thi, tach khoi danh tinh dang nhap.';

CREATE TRIGGER tr_user_profiles_touch
BEFORE UPDATE ON identity.user_profiles
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE identity.user_emails (
    id               uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id          uuid NOT NULL,
    email            varchar(254) NOT NULL,
    normalized_email varchar(254) GENERATED ALWAYS AS (lower(btrim(email))) STORED,
    is_primary       boolean NOT NULL DEFAULT false,
    verified_at      timestamptz,
    created_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_user_emails_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_user_emails_format CHECK (
        char_length(email) BETWEEN 3 AND 254
        AND position('@' IN email) > 1
    )
);

CREATE UNIQUE INDEX ux_user_emails_normalized_email
    ON identity.user_emails (normalized_email);
CREATE UNIQUE INDEX ux_user_emails_primary
    ON identity.user_emails (user_id)
    WHERE is_primary;
CREATE INDEX ix_user_emails_user ON identity.user_emails (user_id);

COMMENT ON TABLE identity.user_emails IS 'Email cua user; moi user co toi da mot email primary.';

CREATE TRIGGER tr_user_emails_touch
BEFORE UPDATE ON identity.user_emails
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE identity.password_credentials (
    user_id             uuid PRIMARY KEY,
    password_hash       text NOT NULL,
    hash_algorithm      varchar(50) NOT NULL DEFAULT 'aspnetcore-identity-v3',
    password_version    integer NOT NULL DEFAULT 1,
    password_changed_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    requires_change     boolean NOT NULL DEFAULT false,
    created_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_password_credentials_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_password_credentials_version CHECK (password_version >= 1),
    CONSTRAINT ck_password_credentials_hash CHECK (char_length(password_hash) >= 20)
);

COMMENT ON TABLE identity.password_credentials IS 'Chi luu password hash; khong luu mat khau hoac salt rieng.';

CREATE TRIGGER tr_password_credentials_touch
BEFORE UPDATE ON identity.password_credentials
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE identity.user_security_states (
    user_id                  uuid PRIMARY KEY,
    security_stamp           uuid NOT NULL DEFAULT gen_random_uuid(),
    failed_login_count       integer NOT NULL DEFAULT 0,
    last_failed_login_at     timestamptz,
    locked_until             timestamptz,
    last_successful_login_at timestamptz,
    mfa_enabled              boolean NOT NULL DEFAULT false,
    updated_at               timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_user_security_states_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_user_security_states_failures CHECK (failed_login_count >= 0)
);

COMMENT ON TABLE identity.user_security_states IS 'Lockout, security stamp va trang thai MFA.';

CREATE TRIGGER tr_user_security_states_touch
BEFORE UPDATE ON identity.user_security_states
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE identity.mfa_methods (
    id                uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id           uuid NOT NULL,
    method_type       smallint NOT NULL,
    label             varchar(100),
    secret_ciphertext bytea,
    credential_data   jsonb NOT NULL DEFAULT '{}'::jsonb,
    verified_at       timestamptz,
    last_used_at      timestamptz,
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    revoked_at        timestamptz,
    CONSTRAINT fk_mfa_methods_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_mfa_methods_type CHECK (method_type IN (1, 2)),
    CONSTRAINT ck_mfa_methods_data CHECK (jsonb_typeof(credential_data) = 'object'),
    CONSTRAINT ck_mfa_methods_revoked CHECK (revoked_at IS NULL OR revoked_at >= created_at)
);

CREATE INDEX ix_mfa_methods_active_user
    ON identity.mfa_methods (user_id)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE identity.mfa_methods IS '1=TOTP, 2=WebAuthn; secret TOTP phai duoc ma hoa boi ung dung.';

CREATE TABLE identity.mfa_recovery_codes (
    id         uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id    uuid NOT NULL,
    code_hash  char(64) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    used_at    timestamptz,
    CONSTRAINT fk_mfa_recovery_codes_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ux_mfa_recovery_codes_hash UNIQUE (code_hash)
);

CREATE INDEX ix_mfa_recovery_codes_unused
    ON identity.mfa_recovery_codes (user_id)
    WHERE used_at IS NULL;

CREATE TABLE identity.auth_identities (
    id               uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id          uuid NOT NULL,
    provider         varchar(30) NOT NULL,
    provider_subject varchar(255) NOT NULL,
    provider_email   varchar(254),
    created_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    last_login_at    timestamptz,
    CONSTRAINT fk_auth_identities_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ux_auth_identities_provider_subject UNIQUE (provider, provider_subject)
);

CREATE INDEX ix_auth_identities_user ON identity.auth_identities (user_id);

COMMENT ON TABLE identity.auth_identities IS 'Lien ket Google, GitHub, Apple hoac OAuth/OIDC provider khac.';

CREATE TABLE identity.auth_sessions (
    id            uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id       uuid NOT NULL,
    device_name   varchar(100),
    user_agent    varchar(500),
    created_by_ip inet,
    last_seen_ip  inet,
    created_at    timestamptz NOT NULL DEFAULT clock_timestamp(),
    last_seen_at  timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at    timestamptz NOT NULL,
    revoked_at    timestamptz,
    revoke_reason varchar(100),
    CONSTRAINT fk_auth_sessions_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_auth_sessions_expiry CHECK (expires_at > created_at),
    CONSTRAINT ck_auth_sessions_last_seen CHECK (last_seen_at >= created_at),
    CONSTRAINT ck_auth_sessions_revoked CHECK (revoked_at IS NULL OR revoked_at >= created_at)
);

CREATE INDEX ix_auth_sessions_active_user
    ON identity.auth_sessions (user_id, expires_at DESC)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE identity.auth_sessions IS 'Mot phien dang nhap tren mot thiet bi; logout bang cach revoke session.';

CREATE TABLE identity.refresh_tokens (
    id                   uuid PRIMARY KEY DEFAULT uuidv7(),
    session_id           uuid NOT NULL,
    parent_token_id      uuid,
    replaced_by_token_id uuid,
    token_hash           char(64) NOT NULL,
    created_at           timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at           timestamptz NOT NULL,
    used_at              timestamptz,
    revoked_at           timestamptz,
    revoke_reason        varchar(100),
    CONSTRAINT uq_refresh_tokens_id_session UNIQUE (id, session_id),
    CONSTRAINT ux_refresh_tokens_hash UNIQUE (token_hash),
    CONSTRAINT fk_refresh_tokens_session FOREIGN KEY (session_id)
        REFERENCES identity.auth_sessions (id) ON DELETE CASCADE,
    CONSTRAINT fk_refresh_tokens_parent FOREIGN KEY (parent_token_id, session_id)
        REFERENCES identity.refresh_tokens (id, session_id) ON DELETE RESTRICT,
    CONSTRAINT fk_refresh_tokens_replacement FOREIGN KEY (replaced_by_token_id, session_id)
        REFERENCES identity.refresh_tokens (id, session_id) ON DELETE RESTRICT,
    CONSTRAINT ck_refresh_tokens_expiry CHECK (expires_at > created_at),
    CONSTRAINT ck_refresh_tokens_used CHECK (used_at IS NULL OR used_at >= created_at),
    CONSTRAINT ck_refresh_tokens_revoked CHECK (revoked_at IS NULL OR revoked_at >= created_at)
);

CREATE UNIQUE INDEX ux_refresh_tokens_parent
    ON identity.refresh_tokens (parent_token_id)
    WHERE parent_token_id IS NOT NULL;
CREATE UNIQUE INDEX ux_refresh_tokens_replacement
    ON identity.refresh_tokens (replaced_by_token_id)
    WHERE replaced_by_token_id IS NOT NULL;
CREATE INDEX ix_refresh_tokens_active_session
    ON identity.refresh_tokens (session_id, expires_at DESC)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE identity.refresh_tokens IS 'Chuoi refresh-token rotation; database chi luu SHA-256 hash.';

CREATE TABLE identity.account_tokens (
    id            uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id       uuid NOT NULL,
    purpose       smallint NOT NULL,
    token_hash    char(64) NOT NULL,
    target_value  varchar(254),
    created_by_ip inet,
    created_at    timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at    timestamptz NOT NULL,
    consumed_at   timestamptz,
    CONSTRAINT fk_account_tokens_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ux_account_tokens_hash UNIQUE (token_hash),
    CONSTRAINT ck_account_tokens_purpose CHECK (purpose IN (1, 2, 3, 4)),
    CONSTRAINT ck_account_tokens_expiry CHECK (expires_at > created_at),
    CONSTRAINT ck_account_tokens_consumed CHECK (consumed_at IS NULL OR consumed_at >= created_at)
);

CREATE INDEX ix_account_tokens_active
    ON identity.account_tokens (user_id, purpose, expires_at)
    WHERE consumed_at IS NULL;

COMMENT ON TABLE identity.account_tokens IS '1=verify_email, 2=reset_password, 3=change_email, 4=unlock_account.';

-- ============================================================================
-- Chat spaces, direct/group conversations, servers and permissions
-- ============================================================================

CREATE TABLE messaging.spaces (
    id                    uuid PRIMARY KEY DEFAULT uuidv7(),
    space_type            smallint NOT NULL,
    status                smallint NOT NULL DEFAULT 1,
    created_by_user_id    uuid,
    last_message_id       uuid,
    last_message_sequence bigint,
    last_activity_at      timestamptz,
    created_at            timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at            timestamptz NOT NULL DEFAULT clock_timestamp(),
    archived_at           timestamptz,
    deleted_at            timestamptz,
    version               integer NOT NULL DEFAULT 1,
    CONSTRAINT uq_chat_spaces_id_type UNIQUE (id, space_type),
    CONSTRAINT fk_chat_spaces_creator FOREIGN KEY (created_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT ck_chat_spaces_type CHECK (space_type IN (1, 2, 3)),
    CONSTRAINT ck_chat_spaces_status CHECK (status IN (1, 2, 3)),
    CONSTRAINT ck_chat_spaces_version CHECK (version >= 1),
    CONSTRAINT ck_chat_spaces_archived CHECK (archived_at IS NULL OR status IN (2, 3)),
    CONSTRAINT ck_chat_spaces_deleted CHECK (deleted_at IS NULL OR status = 3)
);

CREATE INDEX ix_chat_spaces_recent
    ON messaging.spaces (last_activity_at DESC NULLS LAST)
    WHERE status = 1;

CREATE INDEX ix_dm_inbox_order ON messaging.spaces
    ((last_activity_at IS NOT NULL) DESC, last_activity_at DESC, id ASC)
    WHERE space_type=1 AND status<>3 AND deleted_at IS NULL;

COMMENT ON TABLE messaging.spaces IS 'Container thong nhat: 1=DM, 2=group chat, 3=server channel.';
COMMENT ON COLUMN messaging.spaces.last_message_id IS 'Projection de doc nhanh; duoc cap nhat cung transaction voi message va outbox.';

CREATE TRIGGER tr_chat_spaces_touch
BEFORE UPDATE ON messaging.spaces
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at_and_version();

CREATE TABLE messaging.direct_conversations (
    space_id   uuid PRIMARY KEY,
    space_type smallint GENERATED ALWAYS AS (1::smallint) STORED,
    user_low_id  uuid NOT NULL,
    user_high_id uuid NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_direct_conversations_space FOREIGN KEY (space_id, space_type)
        REFERENCES messaging.spaces (id, space_type) ON DELETE CASCADE,
    CONSTRAINT fk_direct_conversations_user_low FOREIGN KEY (user_low_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_direct_conversations_user_high FOREIGN KEY (user_high_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ux_direct_conversations_pair UNIQUE (user_low_id, user_high_id),
    CONSTRAINT ck_direct_conversations_pair CHECK (user_low_id < user_high_id)
);

COMMENT ON TABLE messaging.direct_conversations IS 'Cap user da sap xep dam bao moi cap chi co mot DM.';

CREATE TABLE messaging.group_conversations (
    space_id      uuid PRIMARY KEY,
    space_type    smallint GENERATED ALWAYS AS (2::smallint) STORED,
    name          varchar(100) NOT NULL,
    avatar_object_key varchar(500),
    owner_user_id uuid NOT NULL,
    max_members   integer,
    created_at    timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at    timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_group_conversations_space FOREIGN KEY (space_id, space_type)
        REFERENCES messaging.spaces (id, space_type) ON DELETE CASCADE,
    CONSTRAINT fk_group_conversations_owner FOREIGN KEY (owner_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_group_conversations_name CHECK (char_length(btrim(name)) BETWEEN 1 AND 100),
    CONSTRAINT ck_group_conversations_max_members CHECK (max_members IS NULL OR max_members >= 3)
);

CREATE TRIGGER tr_group_conversations_touch
BEFORE UPDATE ON messaging.group_conversations
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE messaging.space_members (
    space_id           uuid NOT NULL,
    user_id            uuid NOT NULL,
    member_role        smallint NOT NULL DEFAULT 1,
    membership_status  smallint NOT NULL DEFAULT 1,
    joined_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    left_at            timestamptz,
    removed_by_user_id uuid,
    PRIMARY KEY (space_id, user_id),
    CONSTRAINT fk_chat_space_members_space FOREIGN KEY (space_id)
        REFERENCES messaging.spaces (id) ON DELETE CASCADE,
    CONSTRAINT fk_chat_space_members_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_chat_space_members_remover FOREIGN KEY (removed_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT ck_chat_space_members_role CHECK (member_role IN (1, 2, 3)),
    CONSTRAINT ck_chat_space_members_status CHECK (membership_status IN (1, 2, 3)),
    CONSTRAINT ck_chat_space_members_left CHECK (
        (membership_status = 1 AND left_at IS NULL)
        OR (membership_status IN (2, 3) AND left_at IS NOT NULL)
    )
);

CREATE INDEX ix_chat_space_members_active_user
    ON messaging.space_members (user_id, space_id)
    WHERE membership_status = 1;

-- DM pair/member invariants, deferred until the owner commits.
CREATE OR REPLACE FUNCTION messaging.assert_direct_conversation_membership(target uuid)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    low_user uuid;
    high_user uuid;
    member_count integer;
    correct_count integer;
BEGIN
    -- Serialize final invariant checks for the same space without conflicting with FK KEY SHARE locks.
    PERFORM id FROM messaging.spaces WHERE id=target AND space_type=1 FOR NO KEY UPDATE;
    IF NOT FOUND THEN RETURN; END IF;
    SELECT user_low_id,user_high_id INTO low_user,high_user
    FROM messaging.direct_conversations WHERE space_id=target;
    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE='23514', CONSTRAINT='ck_dm_exact_pair_members',
            MESSAGE='A direct space requires its pair and exactly two active participants.';
    END IF;
    SELECT count(*),count(*) FILTER (WHERE user_id IN (low_user,high_user)
        AND membership_status=1 AND left_at IS NULL)
    INTO member_count,correct_count FROM messaging.space_members WHERE space_id=target;
    IF member_count<>2 OR correct_count<>2 THEN
        RAISE EXCEPTION USING ERRCODE='23514', CONSTRAINT='ck_dm_exact_pair_members',
            MESSAGE='A direct space requires its pair and exactly two active participants.';
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION messaging.check_direct_conversation_membership()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE old_space uuid; new_space uuid;
BEGIN
    IF TG_TABLE_NAME='spaces' THEN
        IF TG_OP<>'INSERT' THEN old_space:=OLD.id; END IF;
        IF TG_OP<>'DELETE' THEN new_space:=NEW.id; END IF;
    ELSE
        IF TG_OP<>'INSERT' THEN old_space:=OLD.space_id; END IF;
        IF TG_OP<>'DELETE' THEN new_space:=NEW.space_id; END IF;
    END IF;
    IF old_space IS NOT NULL THEN PERFORM messaging.assert_direct_conversation_membership(old_space); END IF;
    IF new_space IS NOT NULL AND new_space IS DISTINCT FROM old_space THEN
        PERFORM messaging.assert_direct_conversation_membership(new_space);
    END IF;
    RETURN NULL;
END;
$$;

DO $$ DECLARE target uuid;
BEGIN
    FOR target IN SELECT id FROM messaging.spaces WHERE space_type=1 LOOP
        PERFORM messaging.assert_direct_conversation_membership(target);
    END LOOP;
END $$;

DROP TRIGGER IF EXISTS tr_dm_space_membership ON messaging.spaces;
CREATE CONSTRAINT TRIGGER tr_dm_space_membership
AFTER INSERT OR UPDATE OR DELETE ON messaging.spaces DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION messaging.check_direct_conversation_membership();

DROP TRIGGER IF EXISTS tr_dm_pair_membership ON messaging.direct_conversations;
CREATE CONSTRAINT TRIGGER tr_dm_pair_membership
AFTER INSERT OR UPDATE OR DELETE ON messaging.direct_conversations DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION messaging.check_direct_conversation_membership();

DROP TRIGGER IF EXISTS tr_dm_member_membership ON messaging.space_members;
CREATE CONSTRAINT TRIGGER tr_dm_member_membership
AFTER INSERT OR UPDATE OR DELETE ON messaging.space_members DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION messaging.check_direct_conversation_membership();

CREATE TABLE messaging.space_user_states (
    space_id           uuid NOT NULL,
    user_id            uuid NOT NULL,
    last_read_sequence bigint,
    last_read_at       timestamptz,
    notification_level smallint NOT NULL DEFAULT 2,
    muted_until        timestamptz,
    is_hidden          boolean NOT NULL DEFAULT false,
    is_pinned          boolean NOT NULL DEFAULT false,
    updated_at         timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (space_id, user_id),
    CONSTRAINT fk_chat_space_user_states_space FOREIGN KEY (space_id)
        REFERENCES messaging.spaces (id) ON DELETE CASCADE,
    CONSTRAINT fk_chat_space_user_states_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_chat_space_user_states_notification CHECK (notification_level IN (0, 1, 2)),
    CONSTRAINT ck_chat_space_user_states_read CHECK (last_read_sequence IS NULL OR last_read_sequence > 0)
);

CREATE INDEX ix_chat_space_user_states_user
    ON messaging.space_user_states (user_id, is_pinned DESC, updated_at DESC)
    WHERE NOT is_hidden;

CREATE TRIGGER tr_chat_space_user_states_touch
BEFORE UPDATE ON messaging.space_user_states
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE community.servers (
    id                uuid PRIMARY KEY DEFAULT uuidv7(),
    owner_user_id     uuid NOT NULL,
    name              varchar(100) NOT NULL,
    normalized_name   varchar(100) GENERATED ALWAYS AS (lower(btrim(name))) STORED,
    slug              varchar(100) NOT NULL,
    description       varchar(500),
    avatar_object_key varchar(500),
    banner_object_key varchar(500),
    status            smallint NOT NULL DEFAULT 1,
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    deleted_at        timestamptz,
    version           integer NOT NULL DEFAULT 1,
    CONSTRAINT fk_servers_owner FOREIGN KEY (owner_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ux_servers_slug UNIQUE (slug),
    CONSTRAINT ck_servers_name CHECK (char_length(btrim(name)) BETWEEN 2 AND 100),
    CONSTRAINT ck_servers_slug CHECK (slug ~ '^[a-z0-9][a-z0-9-]{1,98}[a-z0-9]$'),
    CONSTRAINT ck_servers_status CHECK (status IN (1, 2, 3)),
    CONSTRAINT ck_servers_version CHECK (version >= 1),
    CONSTRAINT ck_servers_deleted CHECK (deleted_at IS NULL OR status = 3)
);

CREATE INDEX ix_servers_owner ON community.servers (owner_user_id);
CREATE INDEX ix_servers_name_trgm ON community.servers USING gin (normalized_name gin_trgm_ops)
    WHERE status = 1;

COMMENT ON TABLE community.servers IS 'Cong dong/server; 1=active, 2=archived, 3=deleted.';

CREATE TRIGGER tr_servers_touch
BEFORE UPDATE ON community.servers
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at_and_version();

CREATE TABLE community.server_members (
    server_id          uuid NOT NULL,
    user_id            uuid NOT NULL,
    nickname           varchar(64),
    status             smallint NOT NULL DEFAULT 1,
    joined_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    left_at            timestamptz,
    timeout_until      timestamptz,
    invited_by_user_id uuid,
    PRIMARY KEY (server_id, user_id),
    CONSTRAINT fk_server_members_server FOREIGN KEY (server_id)
        REFERENCES community.servers (id) ON DELETE CASCADE,
    CONSTRAINT fk_server_members_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_server_members_inviter FOREIGN KEY (invited_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT ck_server_members_nickname CHECK (nickname IS NULL OR char_length(btrim(nickname)) BETWEEN 1 AND 64),
    CONSTRAINT ck_server_members_status CHECK (status IN (1, 2, 3, 4)),
    CONSTRAINT ck_server_members_left CHECK (
        (status = 1 AND left_at IS NULL)
        OR (status IN (2, 3, 4) AND left_at IS NOT NULL)
    )
);

CREATE INDEX ix_server_members_active_user
    ON community.server_members (user_id, server_id)
    WHERE status = 1;

COMMENT ON TABLE community.server_members IS '1=active, 2=left, 3=kicked, 4=banned.';

CREATE TABLE community.channels (
    space_id        uuid PRIMARY KEY,
    space_type      smallint GENERATED ALWAYS AS (3::smallint) STORED,
    server_id       uuid NOT NULL,
    name            varchar(100) NOT NULL,
    normalized_name varchar(100) GENERATED ALWAYS AS (lower(btrim(name))) STORED,
    topic           varchar(500),
    visibility      smallint NOT NULL DEFAULT 1,
    position        integer NOT NULL DEFAULT 0,
    created_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_server_channels_space_server UNIQUE (space_id, server_id),
    CONSTRAINT fk_server_channels_space FOREIGN KEY (space_id, space_type)
        REFERENCES messaging.spaces (id, space_type) ON DELETE CASCADE,
    CONSTRAINT fk_server_channels_server FOREIGN KEY (server_id)
        REFERENCES community.servers (id) ON DELETE RESTRICT,
    CONSTRAINT ux_server_channels_name UNIQUE (server_id, normalized_name),
    CONSTRAINT ck_server_channels_name CHECK (name ~ '^[a-z0-9][a-z0-9-]{0,98}[a-z0-9]$'),
    CONSTRAINT ck_server_channels_visibility CHECK (visibility IN (1, 2, 3)),
    CONSTRAINT ck_server_channels_position CHECK (position >= 0)
);

CREATE INDEX ix_server_channels_order ON community.channels (server_id, position, space_id);

COMMENT ON TABLE community.channels IS '1=public, 2=private, 3=read-only.';

CREATE TRIGGER tr_server_channels_touch
BEFORE UPDATE ON community.channels
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE community.permissions (
    code        varchar(50) PRIMARY KEY,
    description varchar(255) NOT NULL
);

CREATE TABLE community.roles (
    id              uuid PRIMARY KEY DEFAULT uuidv7(),
    server_id       uuid NOT NULL,
    name            varchar(50) NOT NULL,
    normalized_name varchar(50) GENERATED ALWAYS AS (lower(btrim(name))) STORED,
    color           varchar(7),
    position        integer NOT NULL DEFAULT 0,
    is_default      boolean NOT NULL DEFAULT false,
    is_system       boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT uq_server_roles_id_server UNIQUE (id, server_id),
    CONSTRAINT fk_server_roles_server FOREIGN KEY (server_id)
        REFERENCES community.servers (id) ON DELETE CASCADE,
    CONSTRAINT ux_server_roles_name UNIQUE (server_id, normalized_name),
    CONSTRAINT ck_server_roles_name CHECK (char_length(btrim(name)) BETWEEN 1 AND 50),
    CONSTRAINT ck_server_roles_color CHECK (color IS NULL OR color ~ '^#[0-9A-Fa-f]{6}$'),
    CONSTRAINT ck_server_roles_position CHECK (position >= 0)
);

CREATE UNIQUE INDEX ux_server_roles_default
    ON community.roles (server_id)
    WHERE is_default;

CREATE TRIGGER tr_server_roles_touch
BEFORE UPDATE ON community.roles
FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at();

CREATE TABLE community.role_permissions (
    role_id         uuid NOT NULL,
    permission_code varchar(50) NOT NULL,
    PRIMARY KEY (role_id, permission_code),
    CONSTRAINT fk_server_role_permissions_role FOREIGN KEY (role_id)
        REFERENCES community.roles (id) ON DELETE CASCADE,
    CONSTRAINT fk_server_role_permissions_permission FOREIGN KEY (permission_code)
        REFERENCES community.permissions (code) ON DELETE RESTRICT
);

CREATE TABLE community.member_roles (
    server_id          uuid NOT NULL,
    user_id            uuid NOT NULL,
    role_id            uuid NOT NULL,
    assigned_by_user_id uuid,
    assigned_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (server_id, user_id, role_id),
    CONSTRAINT fk_server_member_roles_member FOREIGN KEY (server_id, user_id)
        REFERENCES community.server_members (server_id, user_id) ON DELETE CASCADE,
    CONSTRAINT fk_server_member_roles_role FOREIGN KEY (role_id, server_id)
        REFERENCES community.roles (id, server_id) ON DELETE CASCADE,
    CONSTRAINT fk_server_member_roles_assigner FOREIGN KEY (assigned_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL
);

CREATE INDEX ix_server_member_roles_role ON community.member_roles (role_id);

CREATE TABLE community.channel_role_overrides (
    space_id       uuid NOT NULL,
    server_id      uuid NOT NULL,
    role_id        uuid NOT NULL,
    permission_code varchar(50) NOT NULL,
    effect          smallint NOT NULL,
    PRIMARY KEY (space_id, role_id, permission_code),
    CONSTRAINT fk_channel_role_overrides_channel FOREIGN KEY (space_id, server_id)
        REFERENCES community.channels (space_id, server_id) ON DELETE CASCADE,
    CONSTRAINT fk_channel_role_overrides_role FOREIGN KEY (role_id, server_id)
        REFERENCES community.roles (id, server_id) ON DELETE CASCADE,
    CONSTRAINT fk_channel_role_overrides_permission FOREIGN KEY (permission_code)
        REFERENCES community.permissions (code) ON DELETE RESTRICT,
    CONSTRAINT ck_channel_role_overrides_effect CHECK (effect IN (1, 2))
);

CREATE TABLE community.channel_user_overrides (
    space_id       uuid NOT NULL,
    server_id      uuid NOT NULL,
    user_id        uuid NOT NULL,
    permission_code varchar(50) NOT NULL,
    effect          smallint NOT NULL,
    PRIMARY KEY (space_id, user_id, permission_code),
    CONSTRAINT fk_channel_user_overrides_channel FOREIGN KEY (space_id, server_id)
        REFERENCES community.channels (space_id, server_id) ON DELETE CASCADE,
    CONSTRAINT fk_channel_user_overrides_member FOREIGN KEY (server_id, user_id)
        REFERENCES community.server_members (server_id, user_id) ON DELETE CASCADE,
    CONSTRAINT fk_channel_user_overrides_permission FOREIGN KEY (permission_code)
        REFERENCES community.permissions (code) ON DELETE RESTRICT,
    CONSTRAINT ck_channel_user_overrides_effect CHECK (effect IN (1, 2))
);

COMMENT ON COLUMN community.channel_role_overrides.effect IS '1=allow, 2=deny.';
COMMENT ON COLUMN community.channel_user_overrides.effect IS '1=allow, 2=deny.';

CREATE TABLE community.invites (
    id                 uuid PRIMARY KEY DEFAULT uuidv7(),
    server_id          uuid NOT NULL,
    code_hash          char(64) NOT NULL,
    created_by_user_id uuid NOT NULL,
    default_role_id    uuid,
    max_uses           integer,
    use_count          integer NOT NULL DEFAULT 0,
    expires_at         timestamptz,
    revoked_at         timestamptz,
    created_at         timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT ux_server_invites_hash UNIQUE (code_hash),
    CONSTRAINT fk_server_invites_creator FOREIGN KEY (server_id, created_by_user_id)
        REFERENCES community.server_members (server_id, user_id) ON DELETE RESTRICT,
    CONSTRAINT fk_server_invites_default_role FOREIGN KEY (default_role_id, server_id)
        REFERENCES community.roles (id, server_id) ON DELETE RESTRICT,
    CONSTRAINT ck_server_invites_max_uses CHECK (max_uses IS NULL OR max_uses > 0),
    CONSTRAINT ck_server_invites_use_count CHECK (use_count >= 0 AND (max_uses IS NULL OR use_count <= max_uses)),
    CONSTRAINT ck_server_invites_expiry CHECK (expires_at IS NULL OR expires_at > created_at)
);

CREATE INDEX ix_server_invites_active
    ON community.invites (server_id, expires_at)
    WHERE revoked_at IS NULL;

CREATE TABLE community.bans (
    server_id         uuid NOT NULL,
    user_id           uuid NOT NULL,
    banned_by_user_id uuid NOT NULL,
    reason            varchar(500),
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at        timestamptz,
    revoked_at        timestamptz,
    revoked_by_user_id uuid,
    PRIMARY KEY (server_id, user_id),
    CONSTRAINT fk_server_bans_server FOREIGN KEY (server_id)
        REFERENCES community.servers (id) ON DELETE CASCADE,
    CONSTRAINT fk_server_bans_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_server_bans_banned_by FOREIGN KEY (banned_by_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_server_bans_revoked_by FOREIGN KEY (revoked_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT ck_server_bans_expiry CHECK (expires_at IS NULL OR expires_at > created_at)
);

-- ============================================================================
-- Messages and related data
-- ============================================================================

CREATE TABLE messaging.messages (
    id                  uuid PRIMARY KEY DEFAULT uuidv7(),
    sequence_no         bigint GENERATED ALWAYS AS IDENTITY,
    space_id            uuid NOT NULL,
    author_user_id      uuid,
    client_message_id   uuid,
    message_type        smallint NOT NULL DEFAULT 1,
    content             text,
    reply_to_message_id uuid,
    thread_root_id      uuid,
    metadata            jsonb NOT NULL DEFAULT '{}'::jsonb,
    search_vector       tsvector GENERATED ALWAYS AS (to_tsvector('simple', coalesce(content, ''))) STORED,
    version             integer NOT NULL DEFAULT 1,
    created_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    edited_at           timestamptz,
    deleted_at          timestamptz,
    deleted_by_user_id  uuid,
    CONSTRAINT uq_messages_sequence UNIQUE (sequence_no),
    CONSTRAINT uq_messages_space_id UNIQUE (space_id, id),
    CONSTRAINT fk_messages_space FOREIGN KEY (space_id)
        REFERENCES messaging.spaces (id) ON DELETE RESTRICT,
    CONSTRAINT fk_messages_author FOREIGN KEY (author_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_messages_deleted_by FOREIGN KEY (deleted_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT fk_messages_reply FOREIGN KEY (space_id, reply_to_message_id)
        REFERENCES messaging.messages (space_id, id) ON DELETE RESTRICT,
    CONSTRAINT fk_messages_thread_root FOREIGN KEY (space_id, thread_root_id)
        REFERENCES messaging.messages (space_id, id) ON DELETE RESTRICT,
    CONSTRAINT ck_messages_type CHECK (message_type IN (1, 2, 3, 4)),
    CONSTRAINT ck_messages_author CHECK (message_type = 2 OR author_user_id IS NOT NULL),
    CONSTRAINT ck_messages_client_id CHECK (message_type = 2 OR client_message_id IS NOT NULL),
    CONSTRAINT ck_messages_text CHECK (
        message_type <> 1
        OR (content IS NOT NULL AND char_length(btrim(content)) BETWEEN 1 AND 10000)
    ),
    CONSTRAINT ck_messages_content_length CHECK (content IS NULL OR char_length(content) <= 10000),
    CONSTRAINT ck_messages_metadata CHECK (jsonb_typeof(metadata) = 'object'),
    CONSTRAINT ck_messages_version CHECK (version >= 1),
    CONSTRAINT ck_messages_edited CHECK (edited_at IS NULL OR edited_at >= created_at),
    CONSTRAINT ck_messages_deleted CHECK (deleted_at IS NULL OR deleted_at >= created_at)
);

CREATE UNIQUE INDEX ux_messages_client_id
    ON messaging.messages (space_id, author_user_id, client_message_id)
    WHERE author_user_id IS NOT NULL AND client_message_id IS NOT NULL;
CREATE INDEX ix_messages_space_history
    ON messaging.messages (space_id, sequence_no DESC);
CREATE INDEX ix_messages_thread
    ON messaging.messages (thread_root_id, sequence_no)
    WHERE thread_root_id IS NOT NULL;
CREATE INDEX ix_messages_reply
    ON messaging.messages (reply_to_message_id)
    WHERE reply_to_message_id IS NOT NULL;
CREATE INDEX ix_messages_author
    ON messaging.messages (author_user_id, sequence_no DESC)
    WHERE author_user_id IS NOT NULL;
CREATE INDEX ix_messages_search
    ON messaging.messages USING gin (search_vector)
    WHERE deleted_at IS NULL;
CREATE INDEX ix_messages_created_brin
    ON messaging.messages USING brin (created_at);

COMMENT ON TABLE messaging.messages IS 'Dong message thong nhat cho DM, group chat va server channel.';
COMMENT ON COLUMN messaging.messages.sequence_no IS 'Thu tu toan cuc, co the co khoang trong khi transaction rollback.';

CREATE TABLE messaging.message_edits (
    id                uuid PRIMARY KEY DEFAULT uuidv7(),
    message_id        uuid NOT NULL,
    version           integer NOT NULL,
    previous_content  text,
    edited_by_user_id uuid NOT NULL,
    edited_at         timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT fk_message_edits_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE CASCADE,
    CONSTRAINT fk_message_edits_editor FOREIGN KEY (edited_by_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ux_message_edits_version UNIQUE (message_id, version),
    CONSTRAINT ck_message_edits_version CHECK (version >= 1),
    CONSTRAINT ck_message_edits_content CHECK (previous_content IS NULL OR char_length(previous_content) <= 10000)
);

CREATE INDEX ix_message_edits_message ON messaging.message_edits (message_id, version DESC);

CREATE TABLE messaging.attachments (
    id                uuid PRIMARY KEY DEFAULT uuidv7(),
    message_id        uuid NOT NULL,
    storage_provider  varchar(20) NOT NULL,
    bucket_name       varchar(100) NOT NULL,
    object_key        varchar(500) NOT NULL,
    original_name     varchar(255) NOT NULL,
    mime_type         varchar(100) NOT NULL,
    size_bytes        bigint NOT NULL,
    checksum_sha256   char(64) NOT NULL,
    width             integer,
    height            integer,
    duration_ms       bigint,
    scan_status       smallint NOT NULL DEFAULT 0,
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    deleted_at        timestamptz,
    CONSTRAINT fk_message_attachments_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE CASCADE,
    CONSTRAINT ux_message_attachments_object UNIQUE (storage_provider, bucket_name, object_key),
    CONSTRAINT ck_message_attachments_name CHECK (char_length(btrim(original_name)) BETWEEN 1 AND 255),
    CONSTRAINT ck_message_attachments_size CHECK (size_bytes > 0),
    CONSTRAINT ck_message_attachments_dimensions CHECK (
        (width IS NULL OR width > 0) AND (height IS NULL OR height > 0)
    ),
    CONSTRAINT ck_message_attachments_duration CHECK (duration_ms IS NULL OR duration_ms >= 0),
    CONSTRAINT ck_message_attachments_scan CHECK (scan_status IN (0, 1, 2, 3))
);

CREATE INDEX ix_message_attachments_message ON messaging.attachments (message_id);

COMMENT ON TABLE messaging.attachments IS 'Chi luu metadata; noi dung file nam tren MinIO/S3.';

CREATE TABLE messaging.reactions (
    message_id   uuid NOT NULL,
    user_id      uuid NOT NULL,
    reaction_key varchar(100) NOT NULL,
    created_at   timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (message_id, user_id, reaction_key),
    CONSTRAINT fk_message_reactions_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE CASCADE,
    CONSTRAINT fk_message_reactions_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_message_reactions_key CHECK (char_length(reaction_key) BETWEEN 1 AND 100)
);

CREATE INDEX ix_message_reactions_message ON messaging.reactions (message_id, reaction_key);

CREATE TABLE messaging.mentions (
    message_id        uuid NOT NULL,
    mentioned_user_id uuid NOT NULL,
    created_at        timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (message_id, mentioned_user_id),
    CONSTRAINT fk_message_mentions_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE CASCADE,
    CONSTRAINT fk_message_mentions_user FOREIGN KEY (mentioned_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT
);

CREATE INDEX ix_message_mentions_user ON messaging.mentions (mentioned_user_id, created_at DESC);

CREATE TABLE messaging.receipts (
    message_id   uuid NOT NULL,
    user_id      uuid NOT NULL,
    delivered_at timestamptz,
    read_at      timestamptz,
    PRIMARY KEY (message_id, user_id),
    CONSTRAINT fk_message_receipts_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE CASCADE,
    CONSTRAINT fk_message_receipts_user FOREIGN KEY (user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_message_receipts_read CHECK (read_at IS NULL OR delivered_at IS NULL OR read_at >= delivered_at)
);

COMMENT ON TABLE messaging.receipts IS 'Chi dung khi can receipt chinh xac; unread thong thuong dung chat_space_user_states.';

CREATE TABLE messaging.pinned_messages (
    space_id          uuid NOT NULL,
    message_id        uuid NOT NULL,
    pinned_by_user_id uuid NOT NULL,
    pinned_at         timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (space_id, message_id),
    CONSTRAINT fk_pinned_messages_message FOREIGN KEY (space_id, message_id)
        REFERENCES messaging.messages (space_id, id) ON DELETE CASCADE,
    CONSTRAINT fk_pinned_messages_user FOREIGN KEY (pinned_by_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT
);

CREATE TABLE messaging.user_blocks (
    blocker_user_id uuid NOT NULL,
    blocked_user_id uuid NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (blocker_user_id, blocked_user_id),
    CONSTRAINT fk_user_blocks_blocker FOREIGN KEY (blocker_user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT fk_user_blocks_blocked FOREIGN KEY (blocked_user_id)
        REFERENCES identity.users (id) ON DELETE CASCADE,
    CONSTRAINT ck_user_blocks_self CHECK (blocker_user_id <> blocked_user_id)
);

CREATE INDEX ix_user_blocks_blocked ON messaging.user_blocks (blocked_user_id, blocker_user_id);

CREATE TABLE moderation.message_reports (
    id                  uuid PRIMARY KEY DEFAULT uuidv7(),
    message_id          uuid NOT NULL,
    reporter_user_id    uuid NOT NULL,
    reason_code         varchar(50) NOT NULL,
    details             varchar(1000),
    message_snapshot    jsonb NOT NULL,
    status              smallint NOT NULL DEFAULT 0,
    reviewed_by_user_id uuid,
    resolution_note     varchar(1000),
    created_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    resolved_at         timestamptz,
    CONSTRAINT fk_message_reports_message FOREIGN KEY (message_id)
        REFERENCES messaging.messages (id) ON DELETE RESTRICT,
    CONSTRAINT fk_message_reports_reporter FOREIGN KEY (reporter_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_message_reports_reviewer FOREIGN KEY (reviewed_by_user_id)
        REFERENCES identity.users (id) ON DELETE SET NULL,
    CONSTRAINT ux_message_reports_reporter UNIQUE (message_id, reporter_user_id),
    CONSTRAINT ck_message_reports_snapshot CHECK (jsonb_typeof(message_snapshot) = 'object'),
    CONSTRAINT ck_message_reports_status CHECK (status IN (0, 1, 2, 3))
);

CREATE INDEX ix_message_reports_pending ON moderation.message_reports (created_at)
    WHERE status IN (0, 1);

CREATE TABLE moderation.actions (
    id                 uuid PRIMARY KEY DEFAULT uuidv7(),
    server_id          uuid,
    moderator_user_id  uuid NOT NULL,
    target_user_id     uuid,
    target_message_id  uuid,
    action_type        varchar(50) NOT NULL,
    reason             varchar(1000),
    metadata           jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at         timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at         timestamptz,
    CONSTRAINT fk_moderation_actions_server FOREIGN KEY (server_id)
        REFERENCES community.servers (id) ON DELETE RESTRICT,
    CONSTRAINT fk_moderation_actions_moderator FOREIGN KEY (moderator_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_moderation_actions_target_user FOREIGN KEY (target_user_id)
        REFERENCES identity.users (id) ON DELETE RESTRICT,
    CONSTRAINT fk_moderation_actions_target_message FOREIGN KEY (target_message_id)
        REFERENCES messaging.messages (id) ON DELETE RESTRICT,
    CONSTRAINT ck_moderation_actions_target CHECK (target_user_id IS NOT NULL OR target_message_id IS NOT NULL),
    CONSTRAINT ck_moderation_actions_metadata CHECK (jsonb_typeof(metadata) = 'object')
);

CREATE INDEX ix_moderation_actions_server ON moderation.actions (server_id, created_at DESC);

-- ============================================================================
-- Audit and reliable asynchronous integration
-- ============================================================================

CREATE TABLE audit.security_events (
    id          uuid PRIMARY KEY DEFAULT uuidv7(),
    user_id     uuid,
    event_type  varchar(50) NOT NULL,
    ip_address  inet,
    user_agent  varchar(500),
    metadata    jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT ck_security_events_metadata CHECK (jsonb_typeof(metadata) = 'object')
);

CREATE INDEX ix_security_events_user ON audit.security_events (user_id, occurred_at DESC);
CREATE INDEX ix_security_events_type ON audit.security_events (event_type, occurred_at DESC);
CREATE INDEX ix_security_events_time_brin ON audit.security_events USING brin (occurred_at);

COMMENT ON TABLE audit.security_events IS 'Append-only audit; user_id co tinh khong co FK de giu lich su khi anonymize/xoa user.';

CREATE TABLE integration.outbox_events (
    id                uuid PRIMARY KEY DEFAULT uuidv7(),
    event_type        varchar(100) NOT NULL,
    aggregate_type    varchar(50) NOT NULL,
    aggregate_id      uuid NOT NULL,
    aggregate_version integer,
    space_id          uuid,
    payload           jsonb NOT NULL,
    occurred_at       timestamptz NOT NULL DEFAULT clock_timestamp(),
    available_at      timestamptz NOT NULL DEFAULT clock_timestamp(),
    published_at      timestamptz,
    attempt_count     integer NOT NULL DEFAULT 0,
    last_error        text,
    lease_token       uuid,
    lease_expires_at  timestamptz,
    CONSTRAINT ck_outbox_events_lease CHECK ((lease_token IS NULL) = (lease_expires_at IS NULL)),
    CONSTRAINT ck_outbox_events_payload CHECK (jsonb_typeof(payload) = 'object'),
    CONSTRAINT ck_outbox_events_attempts CHECK (attempt_count >= 0),
    CONSTRAINT ck_outbox_events_available CHECK (available_at >= occurred_at)
);

CREATE INDEX ix_outbox_events_pending
    ON integration.outbox_events (available_at, occurred_at)
    WHERE published_at IS NULL;
CREATE INDEX ix_outbox_events_aggregate
    ON integration.outbox_events (aggregate_type, aggregate_id, aggregate_version);
CREATE INDEX ix_outbox_events_dispatch ON integration.outbox_events(available_at,lease_expires_at,occurred_at,id)
 WHERE published_at IS NULL AND event_type='Messaging.MessageChanged';

COMMENT ON TABLE integration.outbox_events IS 'Event duoc insert cung transaction nghiep vu, sau do worker publish.';

CREATE TABLE integration.inbox_events (
    consumer_name varchar(100) NOT NULL,
    event_id      uuid NOT NULL,
    received_at   timestamptz NOT NULL DEFAULT clock_timestamp(),
    processed_at  timestamptz,
    payload_hash  char(64),
    last_error    text,
    PRIMARY KEY (consumer_name, event_id)
);

CREATE INDEX ix_inbox_events_unprocessed
    ON integration.inbox_events (consumer_name, received_at)
    WHERE processed_at IS NULL;

COMMENT ON TABLE integration.inbox_events IS 'Idempotency cho event consumer khi he thong tach thanh nhieu service.';

-- ============================================================================
-- Views intended for inspection in DBeaver
-- ============================================================================

CREATE VIEW identity.v_user_accounts AS
SELECT
    u.id,
    u.username,
    p.display_name,
    e.email AS primary_email,
    e.verified_at AS email_verified_at,
    CASE u.status
        WHEN 0 THEN 'pending_verification'
        WHEN 1 THEN 'active'
        WHEN 2 THEN 'suspended'
        WHEN 3 THEN 'disabled'
        WHEN 4 THEN 'deleted'
    END AS status,
    s.failed_login_count,
    s.locked_until,
    s.mfa_enabled,
    u.created_at,
    u.updated_at
FROM identity.users u
LEFT JOIN identity.user_profiles p ON p.user_id = u.id
LEFT JOIN identity.user_emails e ON e.user_id = u.id AND e.is_primary
LEFT JOIN identity.user_security_states s ON s.user_id = u.id;

CREATE VIEW identity.v_active_sessions AS
SELECT
    s.id AS session_id,
    s.user_id,
    u.username,
    s.device_name,
    s.created_by_ip,
    s.last_seen_ip,
    s.created_at,
    s.last_seen_at,
    s.expires_at
FROM identity.auth_sessions s
JOIN identity.users u ON u.id = s.user_id
WHERE s.revoked_at IS NULL AND s.expires_at > clock_timestamp();

CREATE VIEW messaging.v_space_overview AS
SELECT
    s.id AS space_id,
    CASE s.space_type
        WHEN 1 THEN 'direct_message'
        WHEN 2 THEN 'group_message'
        WHEN 3 THEN 'server_channel'
    END AS space_type,
    CASE s.status
        WHEN 1 THEN 'active'
        WHEN 2 THEN 'archived'
        WHEN 3 THEN 'deleted'
    END AS status,
    CASE
        WHEN s.space_type = 1 THEN concat(pl.display_name, ' & ', ph.display_name)
        WHEN s.space_type = 2 THEN gc.name
        WHEN s.space_type = 3 THEN concat(sv.name, ' / #', sc.name)
    END AS display_name,
    s.last_message_sequence,
    lm.content AS last_message_content,
    s.last_activity_at,
    s.created_at
FROM messaging.spaces s
LEFT JOIN messaging.direct_conversations dc ON dc.space_id = s.id
LEFT JOIN identity.user_profiles pl ON pl.user_id = dc.user_low_id
LEFT JOIN identity.user_profiles ph ON ph.user_id = dc.user_high_id
LEFT JOIN messaging.group_conversations gc ON gc.space_id = s.id
LEFT JOIN community.channels sc ON sc.space_id = s.id
LEFT JOIN community.servers sv ON sv.id = sc.server_id
LEFT JOIN messaging.messages lm ON lm.id = s.last_message_id;

CREATE VIEW messaging.v_message_timeline AS
SELECT
    m.sequence_no,
    m.id AS message_id,
    m.space_id,
    u.username AS author_username,
    p.display_name AS author_display_name,
    m.message_type,
    m.content,
    m.reply_to_message_id,
    m.version,
    m.created_at,
    m.edited_at,
    m.deleted_at
FROM messaging.messages m
LEFT JOIN identity.users u ON u.id = m.author_user_id
LEFT JOIN identity.user_profiles p ON p.user_id = m.author_user_id;


-- P2 text-message cutover, also available as the additive migration.
-- Additive and replayable. Never replay schema.sql over an existing volume.
ALTER TABLE messaging.messages ADD COLUMN IF NOT EXISTS conversation_sequence bigint;
WITH ordered AS (
 SELECT m.id, coalesce((SELECT max(existing.conversation_sequence) FROM messaging.messages existing
  WHERE existing.space_id=m.space_id),0) + row_number() OVER (PARTITION BY m.space_id ORDER BY m.sequence_no,m.id) AS seq
 FROM messaging.messages m WHERE m.conversation_sequence IS NULL
)
UPDATE messaging.messages m SET conversation_sequence=o.seq FROM ordered o
WHERE m.id=o.id AND m.conversation_sequence IS NULL;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE messaging.messages ALTER COLUMN conversation_sequence SET NOT NULL;
ALTER TABLE messaging.messages DROP CONSTRAINT IF EXISTS ck_messages_conversation_sequence;
ALTER TABLE messaging.messages ADD CONSTRAINT ck_messages_conversation_sequence CHECK (conversation_sequence>0);
CREATE UNIQUE INDEX IF NOT EXISTS ux_messages_conversation_sequence ON messaging.messages(space_id,conversation_sequence);
CREATE INDEX IF NOT EXISTS ix_messages_conversation_history ON messaging.messages(space_id,conversation_sequence DESC);
UPDATE messaging.spaces s SET last_message_sequence=greatest(coalesce(s.last_message_sequence,0),
 coalesce((SELECT max(m.conversation_sequence) FROM messaging.messages m WHERE m.space_id=s.id),0))
WHERE s.last_message_sequence IS DISTINCT FROM greatest(coalesce(s.last_message_sequence,0),
 coalesce((SELECT max(m.conversation_sequence) FROM messaging.messages m WHERE m.space_id=s.id),0));
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE messaging.spaces ALTER COLUMN last_message_sequence SET DEFAULT 0;
ALTER TABLE messaging.spaces ALTER COLUMN last_message_sequence SET NOT NULL;
ALTER TABLE messaging.messages DROP CONSTRAINT IF EXISTS ck_messages_text;
ALTER TABLE messaging.messages ADD CONSTRAINT ck_messages_text CHECK (
 message_type<>1 OR (deleted_at IS NULL AND content IS NOT NULL AND char_length(content)>0)
 OR (deleted_at IS NOT NULL AND content IS NULL));
CREATE TABLE IF NOT EXISTS messaging.send_operations (
 space_id uuid NOT NULL, author_user_id uuid NOT NULL, client_message_id uuid NOT NULL,
 message_id uuid NOT NULL, fingerprint_version integer, key_id varchar(64), fingerprint bytea,
 created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
 PRIMARY KEY(space_id,author_user_id,client_message_id),
 CONSTRAINT fk_send_operations_message FOREIGN KEY(space_id,message_id)
  REFERENCES messaging.messages(space_id,id) ON DELETE RESTRICT
);
ALTER TABLE messaging.send_operations DROP CONSTRAINT IF EXISTS ck_send_operations_fingerprint;
ALTER TABLE messaging.send_operations ADD CONSTRAINT ck_send_operations_fingerprint CHECK (
  (fingerprint_version IS NULL AND key_id IS NULL AND fingerprint IS NULL)
  OR (fingerprint_version IS NOT NULL AND fingerprint_version=1 AND key_id IS NOT NULL AND fingerprint IS NOT NULL AND octet_length(fingerprint)=32));
-- Legacy sends are readable but cannot be verified from today's edited content.
INSERT INTO messaging.send_operations(space_id,author_user_id,client_message_id,message_id,created_at)
SELECT space_id,author_user_id,client_message_id,id,created_at FROM messaging.messages
WHERE author_user_id IS NOT NULL AND client_message_id IS NOT NULL
ON CONFLICT(space_id,author_user_id,client_message_id) DO NOTHING;

-- Community create/view baseline; identical to migration 001.
CREATE TEMP TABLE community_server_migration_map (id uuid PRIMARY KEY, visibility smallint NOT NULL, join_mode smallint NOT NULL) ON COMMIT DROP;
CREATE FUNCTION common.utf16_length(value text) RETURNS integer
LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS $$
    SELECT coalesce(sum(CASE WHEN ch = '' THEN 0 WHEN ascii(ch) > 65535 THEN 2 ELSE 1 END), 0)::integer
    FROM regexp_split_to_table(value, '') AS chars(ch)
$$;

ALTER TABLE community.servers
    ADD COLUMN visibility smallint,
    ADD COLUMN join_mode smallint,
    ADD COLUMN access_version integer NOT NULL DEFAULT 1,
    ALTER COLUMN description TYPE varchar(1000);

-- The runner supplies an explicitly reviewed map for every existing server.
ALTER TABLE community.servers DISABLE TRIGGER tr_servers_touch;
UPDATE community.servers s SET visibility = m.visibility, join_mode = m.join_mode
FROM pg_temp.community_server_migration_map m WHERE m.id = s.id;
ALTER TABLE community.servers ENABLE TRIGGER tr_servers_touch;
ALTER TABLE community.servers
    ALTER COLUMN visibility SET NOT NULL,
    ALTER COLUMN visibility SET DEFAULT 2,
    ALTER COLUMN join_mode SET NOT NULL,
    ALTER COLUMN join_mode SET DEFAULT 1,
    DROP CONSTRAINT ck_servers_name,
    ADD CONSTRAINT ck_servers_name CHECK (common.utf16_length(name) BETWEEN 2 AND 100),
    ADD CONSTRAINT ck_servers_description CHECK (description IS NULL OR common.utf16_length(description) <= 1000),
    ADD CONSTRAINT ck_servers_visibility CHECK (visibility IN (1,2)),
    ADD CONSTRAINT ck_servers_join_mode CHECK (join_mode IN (1,2)),
    ADD CONSTRAINT ck_servers_access_version CHECK (access_version >= 1);

ALTER TABLE community.server_members
    ADD COLUMN membership_id uuid NOT NULL DEFAULT uuidv7(),
    ADD COLUMN version integer NOT NULL DEFAULT 1,
    ADD CONSTRAINT ux_server_members_membership UNIQUE (membership_id),
    ADD CONSTRAINT ck_server_members_version CHECK (version >= 1),
    DROP CONSTRAINT ck_server_members_status,
    ADD CONSTRAINT ck_server_members_status CHECK (status IN (1,2));

ALTER TABLE community.servers ADD CONSTRAINT fk_servers_owner_membership
    FOREIGN KEY (id,owner_user_id) REFERENCES community.server_members (server_id,user_id)
    DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE community.roles ADD CONSTRAINT ck_roles_system_default
    CHECK ((NOT is_system AND NOT is_default) OR (is_system AND is_default AND name = '@everyone'));

CREATE TABLE community.operations (
    actor_user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE RESTRICT,
    kind varchar(50) NOT NULL,
    scope_id uuid NOT NULL,
    client_operation_id uuid NOT NULL,
    fingerprint_version smallint NOT NULL,
    key_id varchar(100) NOT NULL,
    fingerprint bytea NOT NULL,
    resource_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    CONSTRAINT pk_community_operations PRIMARY KEY (actor_user_id,kind,scope_id,client_operation_id),
    CONSTRAINT ck_community_operations_fingerprint CHECK (octet_length(fingerprint) = 32 AND fingerprint_version >= 1),
    CONSTRAINT ck_community_operations_key CHECK (length(key_id) > 0),
    CONSTRAINT ck_community_operations_kind CHECK (kind = 'create_server' AND scope_id = '00000000-0000-0000-0000-000000000000')
);

CREATE FUNCTION community.assert_server_invariants(checked_id uuid) RETURNS void LANGUAGE plpgsql AS $$
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

CREATE FUNCTION community.check_server_invariants() RETURNS trigger LANGUAGE plpgsql AS $$
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

CREATE CONSTRAINT TRIGGER ct_server_invariants AFTER INSERT OR UPDATE OR DELETE ON community.servers
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();
CREATE CONSTRAINT TRIGGER ct_member_invariants AFTER INSERT OR UPDATE OR DELETE ON community.server_members
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();
CREATE CONSTRAINT TRIGGER ct_role_invariants AFTER INSERT OR UPDATE OR DELETE ON community.roles
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();
CREATE CONSTRAINT TRIGGER ct_role_permission_invariants AFTER INSERT OR UPDATE OR DELETE ON community.role_permissions
    DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION community.check_server_invariants();

CREATE TABLE common.schema_migrations (module varchar(50) NOT NULL, version integer NOT NULL, checksum char(64) NOT NULL, applied_at timestamptz NOT NULL DEFAULT clock_timestamp(), PRIMARY KEY(module,version));
INSERT INTO common.schema_migrations(module,version,checksum) VALUES ('community',1,'b434be5e002497ef204a39c38d7cb4d0783f869804824e130779e74d8f8adab4');

-- Community search baseline 002 (empty bootstrap; upgrades use the .NET runner).
CREATE TEMP TABLE community_search_migration_map(id uuid PRIMARY KEY,search_name text NOT NULL) ON COMMIT DROP;
-- Search key format 1: text-policy trim -> .NET NFC -> ToLowerInvariant.
-- The runner supplies community_search_migration_map in the same transaction.
ALTER TABLE community.servers ADD COLUMN search_name text COLLATE "C";
ALTER TABLE community.servers DISABLE TRIGGER tr_servers_touch;
UPDATE community.servers s SET search_name=m.search_name
FROM community_search_migration_map m WHERE m.id=s.id;
-- Check queued invariants before further ALTER TABLE; leave constraints enabled.
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE community.servers ENABLE TRIGGER tr_servers_touch;
ALTER TABLE community.servers ALTER COLUMN search_name SET NOT NULL;
ALTER TABLE community.servers ADD CONSTRAINT ck_servers_search_name CHECK (search_name <> '');
CREATE INDEX ix_servers_public_search ON community.servers(search_name,id)
    WHERE visibility=1 AND status=1 AND deleted_at IS NULL;
COMMENT ON COLUMN community.servers.search_name IS 'Format 1: text-policy trim/NFC/ToLowerInvariant in .NET; update atomically with display name.';
SET CONSTRAINTS ALL DEFERRED;
DROP TABLE community_search_migration_map;
INSERT INTO common.schema_migrations(module,version,checksum) VALUES ('community',2,'fe07dcc516bfce7eedf257c8fb42d7baa615c716aba9034de9f4e9cebe40ccff');

-- Community role baseline 003 (empty bootstrap; upgrades use .NET runner).
CREATE TEMP TABLE community_role_migration_map(id uuid PRIMARY KEY,server_id uuid NOT NULL,name_key text COLLATE "C" NOT NULL) ON COMMIT DROP;
-- The runner supplies validated .NET role keys in this transaction.
ALTER TABLE community.roles ADD COLUMN name_key text COLLATE "C", ADD COLUMN version integer NOT NULL DEFAULT 1;
ALTER TABLE community.roles DISABLE TRIGGER tr_server_roles_touch;
UPDATE community.roles r SET name_key=m.name_key FROM community_role_migration_map m WHERE m.id=r.id;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE community.roles
    DROP CONSTRAINT ux_server_roles_name,
    DROP CONSTRAINT ck_server_roles_name,
    DROP COLUMN normalized_name,
    ALTER COLUMN name TYPE varchar(64),
    ADD COLUMN normalized_name text GENERATED ALWAYS AS (lower(btrim(name))) STORED,
    ALTER COLUMN name_key SET NOT NULL,
    ADD CONSTRAINT ux_role_name_key UNIQUE(server_id,name_key),
    ADD CONSTRAINT ck_role_name_key CHECK(name_key<>''),
    ADD CONSTRAINT ck_server_roles_name CHECK(common.utf16_length(name) BETWEEN 1 AND 64),
    ADD CONSTRAINT ck_role_version CHECK(version>=1);
DROP TRIGGER tr_server_roles_touch ON community.roles;
CREATE TRIGGER tr_server_roles_touch BEFORE UPDATE ON community.roles
    FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at_and_version();
ALTER TABLE community.server_members ADD CONSTRAINT ux_member_epoch UNIQUE(server_id,user_id,membership_id);
ALTER TABLE community.member_roles ADD COLUMN membership_id uuid;
ALTER TABLE community.channel_user_overrides ADD COLUMN membership_id uuid;
UPDATE community.member_roles r SET membership_id=m.membership_id FROM community.server_members m
    WHERE m.server_id=r.server_id AND m.user_id=r.user_id;
UPDATE community.channel_user_overrides r SET membership_id=m.membership_id FROM community.server_members m
    WHERE m.server_id=r.server_id AND m.user_id=r.user_id;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE community.member_roles ALTER COLUMN membership_id SET NOT NULL,
    ADD CONSTRAINT fk_member_role_epoch FOREIGN KEY(server_id,user_id,membership_id)
        REFERENCES community.server_members(server_id,user_id,membership_id) ON DELETE CASCADE;
ALTER TABLE community.channel_user_overrides ALTER COLUMN membership_id SET NOT NULL,
    ADD CONSTRAINT fk_user_override_epoch FOREIGN KEY(server_id,user_id,membership_id)
        REFERENCES community.server_members(server_id,user_id,membership_id) ON DELETE CASCADE;
ALTER TABLE community.role_permissions ADD CONSTRAINT ck_role_management_catalog CHECK(permission_code IN
    ('manage_channels','manage_invites','review_join_requests','manage_join_mode','manage_channel_access'));
ALTER TABLE community.channel_role_overrides ADD CONSTRAINT ck_role_override_catalog CHECK(permission_code='channel_view');
ALTER TABLE community.channel_user_overrides ADD CONSTRAINT ck_user_override_catalog CHECK(permission_code='channel_view');
INSERT INTO community.permissions(code,description) VALUES
    ('manage_channels','Manage channels'),('manage_invites','Manage invitations'),
    ('review_join_requests','Review join requests'),('manage_join_mode','Manage join mode'),
    ('manage_channel_access','Manage channel access'),('channel_view','View a channel') ON CONFLICT DO NOTHING;
ALTER TABLE community.operations DROP CONSTRAINT ck_community_operations_kind,
    ADD CONSTRAINT ck_community_operations_kind CHECK(
        (kind='create_server' AND scope_id='00000000-0000-0000-0000-000000000000') OR
        (kind='create_role' AND scope_id<>'00000000-0000-0000-0000-000000000000'));
SET CONSTRAINTS ALL DEFERRED;
DROP TABLE community_role_migration_map;
INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',3,'9c109521897297c26a2f1b920194b8ef725b2828be98d1ffb3134f8896e1069d');

-- Community channels/ACL baseline 004 (empty bootstrap).
CREATE TEMP TABLE community_channel_migration_map(id uuid PRIMARY KEY,server_id uuid NOT NULL,name_key text COLLATE "C" NOT NULL,kind smallint NOT NULL,default_view smallint NOT NULL,active boolean NOT NULL) ON COMMIT DROP;
-- The runner provides reviewed channel settings and .NET name keys.
ALTER TABLE community.channels
    ADD COLUMN name_key text COLLATE "C",
    ADD COLUMN kind smallint NOT NULL DEFAULT 1,
    ADD COLUMN default_view smallint NOT NULL DEFAULT 1,
    ADD COLUMN status smallint NOT NULL DEFAULT 1,
    ADD COLUMN deleted_at timestamptz,
    ADD COLUMN version integer NOT NULL DEFAULT 1,
    ADD COLUMN access_version integer NOT NULL DEFAULT 1;
ALTER TABLE community.channels DISABLE TRIGGER tr_server_channels_touch;
UPDATE community.channels c SET name_key=m.name_key,kind=m.kind,default_view=m.default_view,
    status=s.status,deleted_at=s.deleted_at
    FROM community_channel_migration_map m,messaging.spaces s WHERE m.id=c.space_id AND s.id=c.space_id;
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE community.channels
    DROP CONSTRAINT ux_server_channels_name,
    DROP CONSTRAINT IF EXISTS ck_server_channels_name,
    DROP CONSTRAINT ck_server_channels_visibility,
    DROP COLUMN visibility,
    DROP COLUMN normalized_name,
    ADD COLUMN normalized_name text GENERATED ALWAYS AS (lower(btrim(name))) STORED,
    ALTER COLUMN topic TYPE varchar(1000),
    ALTER COLUMN name_key SET NOT NULL,
    ADD CONSTRAINT ck_channel_name CHECK(common.utf16_length(name) BETWEEN 1 AND 100),
    ADD CONSTRAINT ck_channel_topic CHECK(topic IS NULL OR common.utf16_length(topic)<=1000),
    ADD CONSTRAINT ck_channel_name_key CHECK(name_key<>''),
    ADD CONSTRAINT ck_channel_kind CHECK(kind IN(1,2)),
    ADD CONSTRAINT ck_channel_default_view CHECK(default_view IN(1,2)),
    ADD CONSTRAINT ck_channel_status CHECK((status=1 AND deleted_at IS NULL) OR(status=3 AND deleted_at IS NOT NULL)),
    ADD CONSTRAINT ck_channel_versions CHECK(version>=1 AND access_version>=1);
CREATE UNIQUE INDEX ux_channel_active_name_key ON community.channels(server_id,name_key) WHERE status=1 AND deleted_at IS NULL;
DROP TRIGGER tr_server_channels_touch ON community.channels;
CREATE TRIGGER tr_server_channels_touch BEFORE UPDATE ON community.channels
    FOR EACH ROW EXECUTE FUNCTION common.touch_updated_at_and_version();
ALTER TABLE community.operations DROP CONSTRAINT ck_community_operations_kind,
    ADD CONSTRAINT ck_community_operations_kind CHECK(
        (kind='create_server' AND scope_id='00000000-0000-0000-0000-000000000000') OR
        (kind IN('create_role','create_channel') AND scope_id<>'00000000-0000-0000-0000-000000000000'));
SET CONSTRAINTS ALL DEFERRED;
DROP TABLE community_channel_migration_map;
INSERT INTO common.schema_migrations(module,version,checksum) VALUES('community',4,'b5518a26af4b4987b0c2b8aed09909afdfb6d7884c7133cf78fa330af253206c');
-- S2-03 bootstrap snapshot; kept byte-for-byte with the dated migration.
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

INSERT INTO identity.schema_migrations(module,version,checksum) VALUES('identity',20261120,'8859651de84d446acd999993684af4ae0db65e2c450f84bfd19e43ce280a406f');
INSERT INTO community.schema_migrations(module,version,checksum) VALUES('community',20261120,'8859651de84d446acd999993684af4ae0db65e2c450f84bfd19e43ce280a406f');
INSERT INTO messaging.schema_migrations(module,version,checksum) VALUES('messaging',20261120,'8859651de84d446acd999993684af4ae0db65e2c450f84bfd19e43ce280a406f');


-- S2-04 bootstrap snapshot; kept byte-for-byte with the dated migration.
-- S2-04: run through DbMigrator, with writers stopped, after S2-03.
-- Keep every business column, row, ID, NULL, version and timestamp unchanged.
LOCK TABLE community.bans, community.channels, community.member_roles,
    community.operations, community.server_members, community.servers IN ACCESS EXCLUSIVE MODE;

CREATE TEMP TABLE s2_04_fk_targets(table_name text, constraint_name text,
    reference_schema text, reference_table text, PRIMARY KEY(table_name,constraint_name)) ON COMMIT DROP;
INSERT INTO s2_04_fk_targets VALUES
    ('bans','fk_server_bans_banned_by','identity','users'),
    ('bans','fk_server_bans_revoked_by','identity','users'),
    ('bans','fk_server_bans_user','identity','users'),
    ('channels','fk_server_channels_space','messaging','spaces'),
    ('member_roles','fk_server_member_roles_assigner','identity','users'),
    ('operations','operations_actor_user_id_fkey','identity','users'),
    ('server_members','fk_server_members_inviter','identity','users'),
    ('server_members','fk_server_members_user','identity','users'),
    ('servers','fk_servers_owner','identity','users');

DO $$ BEGIN
    IF (SELECT count(*) FROM pg_constraint c
        JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
        JOIN pg_class r ON r.oid=c.confrelid JOIN pg_namespace rn ON rn.oid=r.relnamespace
        WHERE c.contype='f' AND n.nspname='community' AND rn.nspname<>n.nspname) <> 9
    OR EXISTS(SELECT 1 FROM s2_04_fk_targets expected
        LEFT JOIN pg_namespace n ON n.nspname='community'
        LEFT JOIN pg_class t ON t.relnamespace=n.oid AND t.relname=expected.table_name
        LEFT JOIN pg_constraint c ON c.conrelid=t.oid AND c.conname=expected.constraint_name AND c.contype='f'
        LEFT JOIN pg_class r ON r.oid=c.confrelid
        LEFT JOIN pg_namespace rn ON rn.oid=r.relnamespace
        WHERE c.oid IS NULL OR rn.nspname<>expected.reference_schema OR r.relname<>expected.reference_table) THEN
        RAISE EXCEPTION 'S2-04: expected exactly the 9 reviewed Community cross-schema foreign keys; no changes committed';
    END IF;
END $$;

DO $$ DECLARE target record; BEGIN
    FOR target IN SELECT * FROM s2_04_fk_targets ORDER BY table_name,constraint_name LOOP
        EXECUTE format('ALTER TABLE community.%I DROP CONSTRAINT %I',target.table_name,target.constraint_name);
    END LOOP;
END $$;
-- Local owner/member/role/channel constraints and triggers remain in place.

INSERT INTO community.schema_migrations(module,version,checksum) VALUES('community',2026112204,'b54dd1483652134aea7cddaf50d1054966546bdd7e0c9011a746f75bc58409d0');

-- S2-05 bootstrap snapshot; kept byte-for-byte with the dated migration.
-- S2-05: run through DbMigrator, with writers stopped, after S2-03.
-- Independent of S2-04. Never delete populated Moderation data.
LOCK TABLE messaging.spaces, messaging.direct_conversations, messaging.group_conversations,
    messaging.space_members, messaging.space_user_states, messaging.messages,
    messaging.message_edits, messaging.reactions, messaging.mentions, messaging.receipts,
    messaging.pinned_messages, messaging.user_blocks,
    moderation.actions, moderation.message_reports IN ACCESS EXCLUSIVE MODE;

DO $$ BEGIN
    IF EXISTS(SELECT 1 FROM moderation.actions) OR EXISTS(SELECT 1 FROM moderation.message_reports) THEN
        RAISE EXCEPTION 'S2-05: Moderation contains data; preserve it and prepare an explicit migration plan before removal';
    END IF;
END $$;

CREATE TEMP TABLE s2_05_fk_targets(owner_schema text, table_name text, constraint_name text,
    reference_schema text, reference_table text, PRIMARY KEY(owner_schema,table_name,constraint_name)) ON COMMIT DROP;
INSERT INTO s2_05_fk_targets VALUES
    ('messaging','direct_conversations','fk_direct_conversations_user_high','identity','users'),
    ('messaging','direct_conversations','fk_direct_conversations_user_low','identity','users'),
    ('messaging','group_conversations','fk_group_conversations_owner','identity','users'),
    ('messaging','mentions','fk_message_mentions_user','identity','users'),
    ('messaging','message_edits','fk_message_edits_editor','identity','users'),
    ('messaging','messages','fk_messages_author','identity','users'),
    ('messaging','messages','fk_messages_deleted_by','identity','users'),
    ('messaging','pinned_messages','fk_pinned_messages_user','identity','users'),
    ('messaging','reactions','fk_message_reactions_user','identity','users'),
    ('messaging','receipts','fk_message_receipts_user','identity','users'),
    ('messaging','space_members','fk_chat_space_members_remover','identity','users'),
    ('messaging','space_members','fk_chat_space_members_user','identity','users'),
    ('messaging','space_user_states','fk_chat_space_user_states_user','identity','users'),
    ('messaging','spaces','fk_chat_spaces_creator','identity','users'),
    ('messaging','user_blocks','fk_user_blocks_blocked','identity','users'),
    ('messaging','user_blocks','fk_user_blocks_blocker','identity','users'),
    ('moderation','actions','fk_moderation_actions_moderator','identity','users'),
    ('moderation','actions','fk_moderation_actions_server','community','servers'),
    ('moderation','actions','fk_moderation_actions_target_message','messaging','messages'),
    ('moderation','actions','fk_moderation_actions_target_user','identity','users'),
    ('moderation','message_reports','fk_message_reports_message','messaging','messages'),
    ('moderation','message_reports','fk_message_reports_reporter','identity','users'),
    ('moderation','message_reports','fk_message_reports_reviewer','identity','users');

DO $$ BEGIN
    IF (SELECT count(*) FROM pg_constraint c
        JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
        JOIN pg_class r ON r.oid=c.confrelid JOIN pg_namespace rn ON rn.oid=r.relnamespace
        WHERE c.contype='f' AND n.nspname IN ('messaging','moderation') AND rn.nspname<>n.nspname) <> 23
    OR EXISTS(SELECT 1 FROM s2_05_fk_targets expected
        LEFT JOIN pg_namespace n ON n.nspname=expected.owner_schema
        LEFT JOIN pg_class t ON t.relnamespace=n.oid AND t.relname=expected.table_name
        LEFT JOIN pg_constraint c ON c.conrelid=t.oid AND c.conname=expected.constraint_name AND c.contype='f'
        LEFT JOIN pg_class r ON r.oid=c.confrelid
        LEFT JOIN pg_namespace rn ON rn.oid=r.relnamespace
        WHERE c.oid IS NULL OR rn.nspname<>expected.reference_schema OR r.relname<>expected.reference_table) THEN
        RAISE EXCEPTION 'S2-05: expected exactly the 23 reviewed Messaging/Moderation cross-schema foreign keys; no changes committed';
    END IF;
END $$;

DO $$ DECLARE target record; BEGIN
    FOR target IN SELECT * FROM s2_05_fk_targets ORDER BY owner_schema,table_name,constraint_name LOOP
        EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT %I',target.owner_schema,target.table_name,target.constraint_name);
    END LOOP;
END $$;
-- No CASCADE: unknown dependencies abort the entire migration.
DROP VIEW messaging.v_space_overview, messaging.v_message_timeline;
DROP TABLE moderation.message_reports, moderation.actions;
DROP SCHEMA moderation;
-- Messaging-local FKs, pair uniqueness, sequences and DM membership triggers remain.

INSERT INTO messaging.schema_migrations(module,version,checksum) VALUES('messaging',2026112205,'e205ca484949e2a184bb8d8f479707f383bee565c32e46c6444775dfb7d1ae67');

COMMIT;

-- S2-06 bootstrap snapshot; identical to the dated migration.
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

INSERT INTO identity.schema_migrations(module,version,checksum) VALUES('identity',2026112306,'bb5d25d2e1613286b406b5f75267f3afa74d569c63d9760035428873b72d6592');

-- S2-07 bootstrap snapshot; keep byte-for-byte with its standalone migration.
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

INSERT INTO community.schema_migrations(module,version,checksum) VALUES('community',2026112307,'da14e4af4ddebff8c3344ee33f9be200a2983e53417ac50faf73367dbd4bb4f4');

BEGIN;
-- S2-09 bootstrap snapshot; kept byte-for-byte with the dated migration.
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

INSERT INTO community.schema_migrations(module,version,checksum) VALUES('community',2026112409,'19608efaa3f2e240cce01bb023da158ceb3b1039a747314c9d457ec35bbb7d29');
INSERT INTO messaging.schema_migrations(module,version,checksum) VALUES('messaging',2026112409,'19608efaa3f2e240cce01bb023da158ceb3b1039a747314c9d457ec35bbb7d29');
COMMIT;

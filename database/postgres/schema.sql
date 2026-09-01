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
    CONSTRAINT ck_outbox_events_payload CHECK (jsonb_typeof(payload) = 'object'),
    CONSTRAINT ck_outbox_events_attempts CHECK (attempt_count >= 0),
    CONSTRAINT ck_outbox_events_available CHECK (available_at >= occurred_at)
);

CREATE INDEX ix_outbox_events_pending
    ON integration.outbox_events (available_at, occurred_at)
    WHERE published_at IS NULL;
CREATE INDEX ix_outbox_events_aggregate
    ON integration.outbox_events (aggregate_type, aggregate_id, aggregate_version);

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

COMMIT;

-- Additive migration. Apply to existing databases with psql --single-transaction.
-- schema.sql includes this file for new Development databases.
CREATE TABLE IF NOT EXISTS identity.schema_migrations (
    id text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE OR REPLACE FUNCTION common.utf16_length(value text)
RETURNS integer LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS $$
    SELECT coalesce(sum(CASE WHEN ascii(substr(value, position, 1)) > 65535 THEN 2 ELSE 1 END), 0)::integer
    FROM generate_series(1, char_length(value)) AS position;
$$;

ALTER TABLE identity.user_profiles DROP CONSTRAINT IF EXISTS ck_user_profiles_utf16;
ALTER TABLE identity.user_profiles ADD CONSTRAINT ck_user_profiles_utf16 CHECK (
    common.utf16_length(display_name) BETWEEN 1 AND 64
    AND (bio IS NULL OR common.utf16_length(bio) <= 500)
    AND common.utf16_length(locale) BETWEEN 1 AND 16
    AND common.utf16_length(timezone) BETWEEN 1 AND 64
);

CREATE TABLE IF NOT EXISTS identity.account_token_policies (
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    purpose smallint NOT NULL CHECK (purpose IN (1, 2)),
    last_issued_at timestamptz NOT NULL,
    active_token_id uuid REFERENCES identity.account_tokens(id) ON DELETE SET NULL,
    PRIMARY KEY (user_id, purpose)
);

CREATE TABLE IF NOT EXISTS identity.email_deliveries (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES identity.users(id) ON DELETE CASCADE,
    account_token_id uuid NOT NULL REFERENCES identity.account_tokens(id) ON DELETE CASCADE,
    outbox_event_id uuid NOT NULL,
    purpose smallint NOT NULL CHECK (purpose IN (1, 2)),
    recipient varchar(254) NOT NULL,
    template_version integer NOT NULL DEFAULT 1 CHECK (template_version = 1),
    protected_envelope text,
    envelope_expires_at timestamptz NOT NULL,
    status smallint NOT NULL DEFAULT 0 CHECK (status BETWEEN 0 AND 5),
    attempt_count integer NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
    next_attempt_at timestamptz NOT NULL,
    lease_owner uuid,
    lease_until timestamptz,
    provider_message_id varchar(200),
    last_error_code varchar(80),
    created_at timestamptz NOT NULL,
    terminal_at timestamptz,
    UNIQUE (account_token_id, template_version),
    CHECK (envelope_expires_at > created_at),
    CHECK ((status IN (3, 4, 5) AND protected_envelope IS NULL AND terminal_at IS NOT NULL)
        OR (status IN (0, 1, 2) AND protected_envelope IS NOT NULL AND terminal_at IS NULL)),
    CHECK ((status = 1 AND lease_owner IS NOT NULL AND lease_until IS NOT NULL)
        OR (status <> 1 AND lease_owner IS NULL AND lease_until IS NULL))
);

CREATE INDEX IF NOT EXISTS ix_email_deliveries_pending
    ON identity.email_deliveries(next_attempt_at, created_at) WHERE status IN (0, 1, 2);
CREATE INDEX IF NOT EXISTS ix_email_deliveries_expiry
    ON identity.email_deliveries(envelope_expires_at) WHERE protected_envelope IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_email_deliveries_user ON identity.email_deliveries(user_id);

-- Keep cooldown for previously issued links. Only the newest link per purpose stays active.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM identity.schema_migrations WHERE id = '001_identity_email') THEN
        INSERT INTO identity.account_token_policies(user_id, purpose, last_issued_at, active_token_id)
        SELECT DISTINCT ON (user_id, purpose) user_id, purpose, created_at,
            CASE WHEN consumed_at IS NULL AND expires_at > clock_timestamp() THEN id END
        FROM identity.account_tokens WHERE purpose IN (1, 2)
        ORDER BY user_id, purpose, created_at DESC, id DESC
        ON CONFLICT (user_id, purpose) DO NOTHING;

        UPDATE identity.account_tokens token SET consumed_at = clock_timestamp()
        FROM identity.account_token_policies policy
        WHERE token.user_id = policy.user_id AND token.purpose = policy.purpose
            AND token.consumed_at IS NULL AND token.id IS DISTINCT FROM policy.active_token_id;

        INSERT INTO identity.schema_migrations(id) VALUES ('001_identity_email');
    END IF;
END;
$$;

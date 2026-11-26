-- E4-01 version 2: fresh install or adoption of the reviewed legacy email schema.
-- Version 1 remains immutable; this migration has its own ledger/checksum.
DO $$ BEGIN
    IF to_regclass('identity.access_changes') IS NULL THEN
        RAISE EXCEPTION 'Identity session/revocation baseline is required before E4-01';
    END IF;
    IF EXISTS(SELECT 1 FROM pg_stat_activity WHERE datname=current_database()
        AND usename='scdc_identity' AND pid<>pg_backend_pid()) THEN
        RAISE EXCEPTION 'Stop and drain Identity before applying E4-01';
    END IF;
END $$;
LOCK TABLE identity.users, identity.account_tokens IN SHARE ROW EXCLUSIVE MODE;
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

-- Catalog signatures cover column order/type/nullability/defaults and PK/UNIQUE/FK/CHECK
-- definitions on PostgreSQL 18. Constraint names and object OIDs are deliberately excluded.
SET LOCAL search_path='';
DO $$ DECLARE expected record; actual text; BEGIN
    IF EXISTS(SELECT 1 FROM identity.schema_migrations WHERE module='identity' AND version=2026112601
        AND checksum<>'31f5a0b82bf9cb699dbe247ad861e43fe416cf66aeab1d21845d4dcc52ee4a5c') THEN
        RAISE EXCEPTION 'E4-01 version 1 checksum differs; no adoption committed';
    END IF;
    FOR expected IN SELECT * FROM (VALUES
        ('account_token_policies','466c392b3e5f7cc97bc1386f359db3bc'),
        ('email_deliveries','f684c86f3123ae9cacb13706dd4fcef5')) AS shapes(name,fingerprint)
    LOOP
        SELECT md5(jsonb_build_object(
            'columns',(SELECT jsonb_agg(jsonb_build_array(a.attname,format_type(a.atttypid,a.atttypmod),a.attnotnull,pg_get_expr(d.adbin,d.adrelid)) ORDER BY a.attnum)
                FROM pg_attribute a LEFT JOIN pg_attrdef d ON d.adrelid=a.attrelid AND d.adnum=a.attnum
                WHERE a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped),
            'constraints',(SELECT jsonb_agg(pg_get_constraintdef(oid) ORDER BY pg_get_constraintdef(oid))
                FROM pg_constraint WHERE conrelid=c.oid AND contype IN('p','u','f','c')))::text)
        INTO actual FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE n.nspname='identity' AND c.relname=expected.name AND c.relkind='r';
        IF actual IS DISTINCT FROM expected.fingerprint THEN
            RAISE EXCEPTION 'Unsupported legacy Identity email table: %; no adoption committed',expected.name;
        END IF;
    END LOOP;
    -- The old branch used an unbound envelope purpose. Do not silently invalidate live mail.
    IF NOT EXISTS(SELECT 1 FROM identity.schema_migrations WHERE module='identity' AND version=2026112601)
        AND EXISTS(SELECT 1 FROM identity.email_deliveries WHERE protected_envelope IS NOT NULL
            AND envelope_expires_at>clock_timestamp()) THEN
        RAISE EXCEPTION 'Live legacy email envelopes require a reviewed key/envelope migration before adoption';
    END IF;
END $$;

-- Preserve the latest link and its cooldown for accounts created before this upgrade.
INSERT INTO identity.account_token_policies AS existing(user_id, purpose, last_issued_at, active_token_id)
SELECT DISTINCT ON (user_id, purpose) user_id, purpose, created_at,
    CASE WHEN consumed_at IS NULL AND expires_at > clock_timestamp() THEN id END
FROM identity.account_tokens WHERE purpose IN (1, 2)
ORDER BY user_id, purpose, created_at DESC, id DESC
ON CONFLICT(user_id,purpose) DO UPDATE SET
    last_issued_at=GREATEST(existing.last_issued_at,EXCLUDED.last_issued_at),
    active_token_id=CASE WHEN EXCLUDED.last_issued_at>existing.last_issued_at
        THEN EXCLUDED.active_token_id ELSE existing.active_token_id END;

UPDATE identity.account_tokens token SET consumed_at = clock_timestamp()
FROM identity.account_token_policies policy
WHERE token.user_id = policy.user_id AND token.purpose = policy.purpose
    AND token.consumed_at IS NULL AND token.id IS DISTINCT FROM policy.active_token_id;

REVOKE ALL ON identity.account_token_policies, identity.email_deliveries FROM PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON identity.account_token_policies, identity.email_deliveries TO scdc_identity;

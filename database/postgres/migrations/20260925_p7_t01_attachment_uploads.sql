-- Apply to an existing SCDC database before enabling attachment uploads.
CREATE TABLE IF NOT EXISTS messaging.attachment_uploads (
    id                  uuid PRIMARY KEY,
    client_upload_id    uuid NOT NULL,
    space_id            uuid NOT NULL REFERENCES messaging.spaces (id) ON DELETE CASCADE,
    owner_user_id       uuid NOT NULL REFERENCES identity.users (id) ON DELETE RESTRICT,
    object_key          varchar(500) NOT NULL UNIQUE,
    original_name       varchar(255) NOT NULL,
    mime_type           varchar(100) NOT NULL,
    size_bytes          bigint NOT NULL,
    checksum_sha256     char(64) NOT NULL,
    scan_status         smallint NOT NULL DEFAULT 0,
    created_at          timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at          timestamptz NOT NULL,
    attached_message_id uuid REFERENCES messaging.messages (id) ON DELETE SET NULL,
    CONSTRAINT ux_attachment_uploads_client UNIQUE (space_id, owner_user_id, client_upload_id),
    CONSTRAINT ck_attachment_uploads_scan CHECK (scan_status IN (0, 1, 2, 3)),
    CONSTRAINT ck_attachment_uploads_size CHECK (size_bytes > 0),
    CONSTRAINT ck_attachment_uploads_expiry CHECK (expires_at > created_at)
);

CREATE INDEX IF NOT EXISTS ix_attachment_uploads_cleanup ON messaging.attachment_uploads (expires_at)
    WHERE attached_message_id IS NULL;

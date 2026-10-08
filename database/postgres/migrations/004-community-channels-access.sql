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

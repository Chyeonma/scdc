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

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

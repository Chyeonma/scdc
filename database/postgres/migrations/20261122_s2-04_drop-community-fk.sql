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

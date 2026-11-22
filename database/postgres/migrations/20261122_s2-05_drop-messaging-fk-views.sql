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

-- Additive/idempotent. Never replay schema.sql against an existing volume.
BEGIN;

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

COMMIT;

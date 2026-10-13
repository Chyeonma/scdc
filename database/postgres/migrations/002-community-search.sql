-- Search key format 1: text-policy trim -> .NET NFC -> ToLowerInvariant.
-- The runner supplies community_search_migration_map in the same transaction.
ALTER TABLE community.servers ADD COLUMN search_name text COLLATE "C";
ALTER TABLE community.servers DISABLE TRIGGER tr_servers_touch;
UPDATE community.servers s SET search_name=m.search_name
FROM community_search_migration_map m WHERE m.id=s.id;
-- Check queued invariants before further ALTER TABLE; leave constraints enabled.
SET CONSTRAINTS ALL IMMEDIATE;
ALTER TABLE community.servers ENABLE TRIGGER tr_servers_touch;
ALTER TABLE community.servers ALTER COLUMN search_name SET NOT NULL;
ALTER TABLE community.servers ADD CONSTRAINT ck_servers_search_name CHECK (search_name <> '');
CREATE INDEX ix_servers_public_search ON community.servers(search_name,id)
    WHERE visibility=1 AND status=1 AND deleted_at IS NULL;
COMMENT ON COLUMN community.servers.search_name IS 'Format 1: text-policy trim/NFC/ToLowerInvariant in .NET; update atomically with display name.';
SET CONSTRAINTS ALL DEFERRED;

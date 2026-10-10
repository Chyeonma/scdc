-- Read-only acceptance report. Invoke psql -v space_id=<UUID from sequence-fixtures.json>.
\set ON_ERROR_STOP on
SELECT current_database() AS database,
       ('00000001-0000-4000-8000-000000000000'::uuid <
        '00000100-0000-4000-8000-000000000000'::uuid) AS uuid_network_order;
SELECT s.id, s.last_message_sequence::text AS counter,
       (SELECT count(*) FROM messaging.messages WHERE space_id=s.id) AS messages,
       (SELECT count(*) FROM messaging.send_operations WHERE space_id=s.id) AS operations,
       (SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id) AS outbox,
       (SELECT count(*) FROM integration.outbox_events WHERE space_id=s.id
          AND (payload ? 'content' OR payload ? 'body')) AS outbox_bodies
FROM messaging.spaces s WHERE s.id=:'space_id'::uuid;
SELECT m.id, m.client_message_id, m.author_user_id,
       m.conversation_sequence::text AS sequence, m.version::text AS version,
       o.fingerprint_version, o.key_id, octet_length(o.fingerprint) AS hash_bytes
FROM messaging.messages m LEFT JOIN messaging.send_operations o ON o.message_id=m.id
WHERE m.space_id=:'space_id'::uuid ORDER BY m.conversation_sequence;
SELECT count(*) AS orphan_operations FROM messaging.send_operations o
LEFT JOIN messaging.messages m ON m.id=o.message_id
WHERE o.space_id=:'space_id'::uuid AND m.id IS NULL;
SELECT count(*) AS orphan_outbox FROM integration.outbox_events o
LEFT JOIN messaging.messages m ON m.id=o.aggregate_id
WHERE o.space_id=:'space_id'::uuid AND m.id IS NULL;
SELECT count(*) AS sequence_duplicates FROM (
 SELECT conversation_sequence FROM messaging.messages WHERE space_id=:'space_id'::uuid
 GROUP BY conversation_sequence HAVING count(*)>1
) duplicates;
SELECT count(*) AS remaining_sequence_barriers FROM pg_trigger
WHERE tgname LIKE 'dm_p3_gate_%' AND NOT tgisinternal;

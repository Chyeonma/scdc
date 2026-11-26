-- Synthetic transfer fixture: preserve pending journal/leases, tombstones, lineage and sequence.
INSERT INTO community.access_changes(operation_id,server_id,actor_user_id,actor_session_id,actor_membership_id,routing_generation,owners,command,command_fingerprint,scopes,version,lease_token,lease_until,attempt_count,next_attempt_at)
SELECT '66666666-6666-4666-8666-666666666666',server_id,user_id,'33333333-3333-4333-8333-333333333333',membership_id,1,ARRAY['Community','Messaging'],'{"fixture":true}',repeat('d',64),'[{"epoch":9}]',7,'77777777-7777-4777-8777-777777777777','2030-01-01',3,'2030-01-01'
FROM community.server_members WHERE user_id='01990000-0000-7000-8000-000000000001' LIMIT 1;
INSERT INTO community.access_change_acks(operation_id,batch_id,owner,routing_generation,installed_scopes)
VALUES('66666666-6666-4666-8666-666666666666','88888888-8888-4888-8888-888888888888','Community',1,'[{"epoch":9}]');
INSERT INTO community.channel_provisions(operation_id,channel_id,server_id,actor_user_id,fingerprint,key_id,name,name_key,created_at,deadline,phase,decision_version,completed_at,delivery_pending,lease_token,lease_until,version)
VALUES('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa','bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb','01990000-0000-7400-8000-000000000001','01990000-0000-7000-8000-000000000001',decode(repeat('a',64),'hex'),'fixture-key','Cancelled fixture','cancelled fixture','2026-11-20','2026-11-21',2,1,'2026-11-21',true,'cccccccc-cccc-4ccc-8ccc-cccccccccccc','2030-01-01',9);
INSERT INTO community.outbox_events(event_type,aggregate_type,aggregate_id,payload,occurred_at,available_at,attempt_count,lease_token,lease_expires_at)
VALUES('Fixture.Pending','Server','01990000-0000-7400-8000-000000000001','{"fixture":true}','2026-11-20','2026-11-21',4,'99999999-9999-4999-8999-999999999999','2030-01-01');
INSERT INTO community.inbox_events(consumer_name,event_id,payload_hash,last_error) VALUES('fixture','dddddddd-dddd-4ddd-8ddd-dddddddddddd',repeat('e',64),'pending-fixture');
CREATE TABLE community.legacy_schema_migrations(id text PRIMARY KEY,applied_at timestamptz NOT NULL);
ALTER TABLE community.legacy_schema_migrations RENAME CONSTRAINT legacy_schema_migrations_applied_at_not_null TO schema_migrations_applied_at_not_null;
INSERT INTO community.legacy_schema_migrations VALUES('fixture-mvp-lineage','2026-10-01');
CREATE SEQUENCE community.transfer_fixture_sequence;
SELECT setval('community.transfer_fixture_sequence',37,true);

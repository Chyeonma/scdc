-- Synthetic offline fixture only. Every token/hash below is deliberately unusable.
INSERT INTO identity.users(id,username,status) VALUES('11111111-1111-4111-8111-111111111111','transfer_fixture',1);
INSERT INTO identity.user_profiles(user_id,display_name,bio) VALUES('11111111-1111-4111-8111-111111111111','Transfer fixture','Unicode: tiếng Việt 😀');
INSERT INTO identity.user_emails(user_id,email,is_primary,verified_at) VALUES('11111111-1111-4111-8111-111111111111','fixture@example.test',true,'2026-11-20T00:00:00Z');
INSERT INTO identity.password_credentials(user_id,password_hash) VALUES('11111111-1111-4111-8111-111111111111','unusable-hash-must-survive-byte-for-byte');
INSERT INTO identity.user_security_states(user_id,security_stamp,failed_login_count,locked_until)
VALUES('11111111-1111-4111-8111-111111111111','22222222-2222-4222-8222-222222222222',3,'2030-01-01T00:00:00Z');
INSERT INTO identity.auth_sessions(id,user_id,created_at,last_seen_at,expires_at,revoked_at,revoke_reason)
VALUES('33333333-3333-4333-8333-333333333333','11111111-1111-4111-8111-111111111111','2026-11-20','2026-11-20','2030-01-01','2026-11-21','fixture-revocation');
INSERT INTO identity.refresh_tokens(id,session_id,token_hash,created_at,expires_at,used_at,revoked_at)
VALUES('44444444-4444-4444-8444-444444444444','33333333-3333-4333-8333-333333333333',repeat('a',64),'2026-11-20','2030-01-01','2026-11-21','2026-11-22');
INSERT INTO identity.refresh_tokens(id,session_id,parent_token_id,token_hash,created_at,expires_at)
VALUES('55555555-5555-4555-8555-555555555555','33333333-3333-4333-8333-333333333333','44444444-4444-4444-8444-444444444444',repeat('b',64),'2026-11-21','2030-01-01');
UPDATE identity.refresh_tokens SET replaced_by_token_id='55555555-5555-4555-8555-555555555555' WHERE id='44444444-4444-4444-8444-444444444444';
INSERT INTO identity.account_tokens(user_id,purpose,token_hash,created_at,expires_at,consumed_at)
VALUES('11111111-1111-4111-8111-111111111111',1,repeat('c',64),'2026-11-20','2030-01-01','2026-11-21');
INSERT INTO identity.access_changes(operation_id,user_id,routing_generation,owners,version,payload,command_fingerprint,lease_token,lease_until,attempt_count,next_attempt_at)
VALUES('66666666-6666-4666-8666-666666666666','11111111-1111-4111-8111-111111111111',1,ARRAY['Community','Messaging'],7,'{"fixture":true}',repeat('d',64),
       '77777777-7777-4777-8777-777777777777','2030-01-01',3,'2030-01-01');
INSERT INTO identity.access_scopes(scope_kind,user_id,current_epoch,pending_operation_id,version)
VALUES(1,'11111111-1111-4111-8111-111111111111',9,'66666666-6666-4666-8666-666666666666',11);
INSERT INTO identity.access_change_acks(operation_id,batch_id,owner,routing_generation,installed_scopes)
VALUES('66666666-6666-4666-8666-666666666666','88888888-8888-4888-8888-888888888888','Community',1,'[{"epoch":9}]');
INSERT INTO identity.outbox_events(event_type,aggregate_type,aggregate_id,payload,occurred_at,available_at,attempt_count,lease_token,lease_expires_at)
VALUES('Fixture.Pending','User','11111111-1111-4111-8111-111111111111','{"fixture":true}','2026-11-20','2026-11-21',4,'99999999-9999-4999-8999-999999999999','2030-01-01');
INSERT INTO identity.inbox_events(consumer_name,event_id,payload_hash,last_error) VALUES('fixture','aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',repeat('e',64),'pending-fixture');
INSERT INTO identity.security_events(user_id,event_type,metadata) VALUES('11111111-1111-4111-8111-111111111111','Fixture.Audit','{"fixture":true}');
CREATE TABLE identity.legacy_schema_migrations(id text PRIMARY KEY,applied_at timestamptz NOT NULL);
INSERT INTO identity.legacy_schema_migrations VALUES('fixture-mvp-lineage','2026-10-01');
SELECT setval('identity.access_store_generation',37,true);

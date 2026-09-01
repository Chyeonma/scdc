\set ON_ERROR_STOP on

BEGIN;

-- Stable UUIDs make the sample flows easy to follow in DBeaver.
-- Users: Alice=...0001, Bob=...0002, Charlie=...0003, Linh=...0004.

INSERT INTO identity.users (id, username, status, created_at, updated_at)
VALUES
    ('01990000-0000-7000-8000-000000000001', 'alice',   1, now() - interval '90 days', now() - interval '1 day'),
    ('01990000-0000-7000-8000-000000000002', 'bob',     1, now() - interval '80 days', now() - interval '2 hours'),
    ('01990000-0000-7000-8000-000000000003', 'charlie', 1, now() - interval '45 days', now() - interval '3 hours'),
    ('01990000-0000-7000-8000-000000000004', 'linh',    0, now() - interval '10 minutes', now() - interval '10 minutes');

INSERT INTO identity.user_profiles
    (user_id, display_name, bio, locale, timezone, created_at, updated_at)
VALUES
    ('01990000-0000-7000-8000-000000000001', 'Alice Nguyễn', 'Product owner của dự án SCDC.', 'vi-VN', 'Asia/Ho_Chi_Minh', now() - interval '90 days', now() - interval '1 day'),
    ('01990000-0000-7000-8000-000000000002', 'Bob Trần', 'Backend developer.', 'vi-VN', 'Asia/Ho_Chi_Minh', now() - interval '80 days', now() - interval '2 hours'),
    ('01990000-0000-7000-8000-000000000003', 'Charlie Lê', 'Frontend developer.', 'vi-VN', 'Asia/Ho_Chi_Minh', now() - interval '45 days', now() - interval '3 hours'),
    ('01990000-0000-7000-8000-000000000004', 'Linh Phạm', NULL, 'vi-VN', 'Asia/Ho_Chi_Minh', now() - interval '10 minutes', now() - interval '10 minutes');

INSERT INTO identity.user_emails
    (id, user_id, email, is_primary, verified_at, created_at, updated_at)
VALUES
    ('01990000-0000-7001-8000-000000000001', '01990000-0000-7000-8000-000000000001', 'alice@example.local',   true, now() - interval '90 days', now() - interval '90 days', now() - interval '90 days'),
    ('01990000-0000-7001-8000-000000000002', '01990000-0000-7000-8000-000000000002', 'bob@example.local',     true, now() - interval '80 days', now() - interval '80 days', now() - interval '80 days'),
    ('01990000-0000-7001-8000-000000000003', '01990000-0000-7000-8000-000000000003', 'charlie@example.local', true, now() - interval '45 days', now() - interval '45 days', now() - interval '45 days'),
    ('01990000-0000-7001-8000-000000000004', '01990000-0000-7000-8000-000000000004', 'linh@example.local',    true, NULL,                       now() - interval '10 minutes', now() - interval '10 minutes');

-- These are intentionally non-loginable demo hashes. Real hashes are created by the application.
INSERT INTO identity.password_credentials
    (user_id, password_hash, hash_algorithm, password_version, password_changed_at, created_at, updated_at)
VALUES
    ('01990000-0000-7000-8000-000000000001', 'DEMO_ONLY_NOT_LOGINABLE_HASH_FOR_ALICE_000000000000000000000001', 'demo', 1, now() - interval '90 days', now() - interval '90 days', now() - interval '90 days'),
    ('01990000-0000-7000-8000-000000000002', 'DEMO_ONLY_NOT_LOGINABLE_HASH_FOR_BOB_0000000000000000000000002',   'demo', 1, now() - interval '80 days', now() - interval '80 days', now() - interval '80 days'),
    ('01990000-0000-7000-8000-000000000003', 'DEMO_ONLY_NOT_LOGINABLE_HASH_FOR_CHARLIE_000000000000000000003',   'demo', 1, now() - interval '45 days', now() - interval '45 days', now() - interval '45 days'),
    ('01990000-0000-7000-8000-000000000004', 'DEMO_ONLY_NOT_LOGINABLE_HASH_FOR_LINH_000000000000000000000004',      'demo', 1, now() - interval '10 minutes', now() - interval '10 minutes', now() - interval '10 minutes');

INSERT INTO identity.user_security_states
    (user_id, security_stamp, failed_login_count, last_successful_login_at, mfa_enabled, updated_at)
VALUES
    ('01990000-0000-7000-8000-000000000001', '11990000-0000-4000-8000-000000000001', 0, now() - interval '1 hour',  true,  now() - interval '1 hour'),
    ('01990000-0000-7000-8000-000000000002', '11990000-0000-4000-8000-000000000002', 1, now() - interval '2 hours', false, now() - interval '2 hours'),
    ('01990000-0000-7000-8000-000000000003', '11990000-0000-4000-8000-000000000003', 0, now() - interval '3 hours', false, now() - interval '3 hours'),
    ('01990000-0000-7000-8000-000000000004', '11990000-0000-4000-8000-000000000004', 0, NULL,                       false, now() - interval '10 minutes');

INSERT INTO identity.mfa_methods
    (id, user_id, method_type, label, secret_ciphertext, verified_at, created_at)
VALUES
    ('01990000-0000-7002-8000-000000000001', '01990000-0000-7000-8000-000000000001', 1, 'Authenticator demo', decode(repeat('ab', 32), 'hex'), now() - interval '60 days', now() - interval '60 days');

INSERT INTO identity.mfa_recovery_codes (id, user_id, code_hash, created_at)
VALUES
    ('01990000-0000-7003-8000-000000000001', '01990000-0000-7000-8000-000000000001', repeat('1', 64), now() - interval '60 days'),
    ('01990000-0000-7003-8000-000000000002', '01990000-0000-7000-8000-000000000001', repeat('2', 64), now() - interval '60 days');

INSERT INTO identity.auth_identities
    (id, user_id, provider, provider_subject, provider_email, created_at, last_login_at)
VALUES
    ('01990000-0000-7004-8000-000000000001', '01990000-0000-7000-8000-000000000003', 'google', 'google-demo-subject-charlie', 'charlie@example.local', now() - interval '30 days', now() - interval '3 hours');

INSERT INTO identity.auth_sessions
    (id, user_id, device_name, user_agent, created_by_ip, last_seen_ip, created_at, last_seen_at, expires_at, revoked_at, revoke_reason)
VALUES
    ('01990000-0000-7100-8000-000000000001', '01990000-0000-7000-8000-000000000001', 'Chrome / Linux', 'Mozilla/5.0 Demo Chrome Linux', '192.168.1.10', '192.168.1.10', now() - interval '7 days', now() - interval '1 hour', now() + interval '23 days', NULL, NULL),
    ('01990000-0000-7100-8000-000000000002', '01990000-0000-7000-8000-000000000002', 'Firefox / Windows', 'Mozilla/5.0 Demo Firefox Windows', '192.168.1.20', '192.168.1.20', now() - interval '5 days', now() - interval '1 day', now() + interval '25 days', now() - interval '1 day', 'user_logout');

INSERT INTO identity.refresh_tokens
    (id, session_id, token_hash, created_at, expires_at, used_at)
VALUES
    ('01990000-0000-7101-8000-000000000001', '01990000-0000-7100-8000-000000000001', repeat('a', 64), now() - interval '7 days', now() + interval '23 days', now() - interval '1 day');

INSERT INTO identity.refresh_tokens
    (id, session_id, parent_token_id, token_hash, created_at, expires_at)
VALUES
    ('01990000-0000-7101-8000-000000000002', '01990000-0000-7100-8000-000000000001', '01990000-0000-7101-8000-000000000001', repeat('b', 64), now() - interval '1 day', now() + interval '29 days');

UPDATE identity.refresh_tokens
SET replaced_by_token_id = '01990000-0000-7101-8000-000000000002'
WHERE id = '01990000-0000-7101-8000-000000000001';

INSERT INTO identity.refresh_tokens
    (id, session_id, token_hash, created_at, expires_at, revoked_at, revoke_reason)
VALUES
    ('01990000-0000-7101-8000-000000000003', '01990000-0000-7100-8000-000000000002', repeat('c', 64), now() - interval '5 days', now() + interval '25 days', now() - interval '1 day', 'user_logout');

INSERT INTO identity.account_tokens
    (id, user_id, purpose, token_hash, target_value, created_by_ip, created_at, expires_at)
VALUES
    ('01990000-0000-7102-8000-000000000001', '01990000-0000-7000-8000-000000000004', 1, repeat('d', 64), 'linh@example.local', '192.168.1.40', now() - interval '10 minutes', now() + interval '20 minutes');

INSERT INTO audit.security_events
    (id, user_id, event_type, ip_address, user_agent, metadata, occurred_at)
VALUES
    ('01990000-0000-7200-8000-000000000001', '01990000-0000-7000-8000-000000000001', 'login_succeeded', '192.168.1.10', 'Mozilla/5.0 Demo Chrome Linux', '{"mfa":true}'::jsonb, now() - interval '1 hour'),
    ('01990000-0000-7200-8000-000000000002', '01990000-0000-7000-8000-000000000002', 'logout', '192.168.1.20', 'Mozilla/5.0 Demo Firefox Windows', '{"reason":"user_logout"}'::jsonb, now() - interval '1 day'),
    ('01990000-0000-7200-8000-000000000003', '01990000-0000-7000-8000-000000000004', 'registration_succeeded', '192.168.1.40', 'Demo browser', '{"email_verified":false}'::jsonb, now() - interval '10 minutes'),
    ('01990000-0000-7200-8000-000000000004', '01990000-0000-7000-8000-000000000004', 'email_verification_requested', '192.168.1.40', 'Demo browser', '{}'::jsonb, now() - interval '9 minutes');

-- Permission catalog.
INSERT INTO community.permissions (code, description)
VALUES
    ('manage_server', 'Cap nhat thong tin va cau hinh server'),
    ('manage_channels', 'Tao, sua va xoa channel'),
    ('manage_roles', 'Tao role va gan quyen'),
    ('invite_members', 'Tao loi moi vao server'),
    ('kick_members', 'Moi thanh vien khoi server'),
    ('ban_members', 'Cam thanh vien'),
    ('read_messages', 'Doc lich su tin nhan'),
    ('send_messages', 'Gui tin nhan'),
    ('edit_own_messages', 'Sua tin nhan cua chinh minh'),
    ('delete_messages', 'Xoa tin nhan cua thanh vien'),
    ('attach_files', 'Gui file dinh kem'),
    ('add_reactions', 'Them reaction'),
    ('mention_everyone', 'Mention tat ca thanh vien');

-- One direct conversation, one group conversation and two server channels.
INSERT INTO messaging.spaces
    (id, space_type, status, created_by_user_id, created_at, updated_at)
VALUES
    ('01990000-0000-7300-8000-000000000001', 1, 1, '01990000-0000-7000-8000-000000000001', now() - interval '20 days', now() - interval '20 days'),
    ('01990000-0000-7300-8000-000000000002', 2, 1, '01990000-0000-7000-8000-000000000001', now() - interval '15 days', now() - interval '15 days'),
    ('01990000-0000-7300-8000-000000000003', 3, 1, '01990000-0000-7000-8000-000000000001', now() - interval '30 days', now() - interval '30 days'),
    ('01990000-0000-7300-8000-000000000004', 3, 1, '01990000-0000-7000-8000-000000000001', now() - interval '30 days', now() - interval '30 days');

INSERT INTO messaging.direct_conversations (space_id, user_low_id, user_high_id, created_at)
VALUES
    ('01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000001', '01990000-0000-7000-8000-000000000002', now() - interval '20 days');

INSERT INTO messaging.group_conversations
    (space_id, name, owner_user_id, max_members, created_at, updated_at)
VALUES
    ('01990000-0000-7300-8000-000000000002', 'Nhóm triển khai SCDC', '01990000-0000-7000-8000-000000000001', 20, now() - interval '15 days', now() - interval '15 days');

INSERT INTO messaging.space_members
    (space_id, user_id, member_role, membership_status, joined_at)
VALUES
    ('01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000001', 1, 1, now() - interval '20 days'),
    ('01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000002', 1, 1, now() - interval '20 days'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000001', 3, 1, now() - interval '15 days'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000002', 1, 1, now() - interval '15 days'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000003', 1, 1, now() - interval '14 days');

INSERT INTO community.servers
    (id, owner_user_id, name, slug, description, status, created_at, updated_at)
VALUES
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000001', 'SCDC Community', 'scdc-community', 'Server mẫu mô tả luồng dữ liệu thực tế của SCDC.', 1, now() - interval '30 days', now() - interval '30 days');

INSERT INTO community.server_members
    (server_id, user_id, nickname, status, joined_at, invited_by_user_id)
VALUES
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000001', 'Alice',   1, now() - interval '30 days', NULL),
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000002', 'Bob BE',  1, now() - interval '29 days', '01990000-0000-7000-8000-000000000001'),
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000003', 'Charlie', 1, now() - interval '28 days', '01990000-0000-7000-8000-000000000001');

INSERT INTO community.channels
    (space_id, server_id, name, topic, visibility, position, created_at, updated_at)
VALUES
    ('01990000-0000-7300-8000-000000000003', '01990000-0000-7400-8000-000000000001', 'general', 'Trao đổi chung của dự án', 1, 0, now() - interval '30 days', now() - interval '30 days'),
    ('01990000-0000-7300-8000-000000000004', '01990000-0000-7400-8000-000000000001', 'backend', 'Thảo luận API và database', 1, 1, now() - interval '30 days', now() - interval '30 days');

INSERT INTO community.roles
    (id, server_id, name, color, position, is_default, is_system, created_at, updated_at)
VALUES
    ('01990000-0000-7500-8000-000000000001', '01990000-0000-7400-8000-000000000001', 'Owner',     '#E53935', 100, false, true,  now() - interval '30 days', now() - interval '30 days'),
    ('01990000-0000-7500-8000-000000000002', '01990000-0000-7400-8000-000000000001', 'Moderator', '#1E88E5',  50, false, false, now() - interval '30 days', now() - interval '30 days'),
    ('01990000-0000-7500-8000-000000000003', '01990000-0000-7400-8000-000000000001', 'Member',    '#757575',   0, true,  true,  now() - interval '30 days', now() - interval '30 days');

INSERT INTO community.role_permissions (role_id, permission_code)
SELECT '01990000-0000-7500-8000-000000000001'::uuid, code
FROM community.permissions;

INSERT INTO community.role_permissions (role_id, permission_code)
VALUES
    ('01990000-0000-7500-8000-000000000002', 'invite_members'),
    ('01990000-0000-7500-8000-000000000002', 'kick_members'),
    ('01990000-0000-7500-8000-000000000002', 'read_messages'),
    ('01990000-0000-7500-8000-000000000002', 'send_messages'),
    ('01990000-0000-7500-8000-000000000002', 'edit_own_messages'),
    ('01990000-0000-7500-8000-000000000002', 'delete_messages'),
    ('01990000-0000-7500-8000-000000000002', 'attach_files'),
    ('01990000-0000-7500-8000-000000000002', 'add_reactions'),
    ('01990000-0000-7500-8000-000000000003', 'read_messages'),
    ('01990000-0000-7500-8000-000000000003', 'send_messages'),
    ('01990000-0000-7500-8000-000000000003', 'edit_own_messages'),
    ('01990000-0000-7500-8000-000000000003', 'attach_files'),
    ('01990000-0000-7500-8000-000000000003', 'add_reactions');

INSERT INTO community.member_roles
    (server_id, user_id, role_id, assigned_by_user_id, assigned_at)
VALUES
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000001', '01990000-0000-7500-8000-000000000001', NULL, now() - interval '30 days'),
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000002', '01990000-0000-7500-8000-000000000002', '01990000-0000-7000-8000-000000000001', now() - interval '29 days'),
    ('01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000003', '01990000-0000-7500-8000-000000000003', '01990000-0000-7000-8000-000000000001', now() - interval '28 days');

-- Member role is denied send in #backend, while Bob gets an explicit user allow.
INSERT INTO community.channel_role_overrides
    (space_id, server_id, role_id, permission_code, effect)
VALUES
    ('01990000-0000-7300-8000-000000000004', '01990000-0000-7400-8000-000000000001', '01990000-0000-7500-8000-000000000003', 'send_messages', 2);

INSERT INTO community.channel_user_overrides
    (space_id, server_id, user_id, permission_code, effect)
VALUES
    ('01990000-0000-7300-8000-000000000004', '01990000-0000-7400-8000-000000000001', '01990000-0000-7000-8000-000000000002', 'send_messages', 1);

INSERT INTO community.invites
    (id, server_id, code_hash, created_by_user_id, default_role_id, max_uses, use_count, expires_at, created_at)
VALUES
    ('01990000-0000-7600-8000-000000000001', '01990000-0000-7400-8000-000000000001', repeat('e', 64), '01990000-0000-7000-8000-000000000001', '01990000-0000-7500-8000-000000000003', 20, 2, now() + interval '7 days', now() - interval '2 days');

-- Global sequence numbers deliberately have gaps-friendly semantics.
INSERT INTO messaging.messages
    (id, sequence_no, space_id, author_user_id, client_message_id, message_type, content, reply_to_message_id, metadata, version, created_at, edited_at)
OVERRIDING SYSTEM VALUE
VALUES
    ('01990000-0000-7700-8000-000000000001', 1001, '01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000001', '21990000-0000-4000-8000-000000000001', 1, 'Chào Bob, phần database đã sẵn sàng chưa?', NULL, '{}'::jsonb, 1, now() - interval '2 hours', NULL),
    ('01990000-0000-7700-8000-000000000002', 1002, '01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000002', '21990000-0000-4000-8000-000000000002', 1, 'Chào Alice, mình đã kiểm tra và database đã sẵn sàng.', '01990000-0000-7700-8000-000000000001', '{}'::jsonb, 2, now() - interval '115 minutes', now() - interval '110 minutes'),
    ('01990000-0000-7700-8000-000000000003', 1003, '01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000003', '21990000-0000-4000-8000-000000000003', 1, 'Mình bắt đầu làm giao diện danh sách cuộc trò chuyện nhé.', NULL, '{}'::jsonb, 1, now() - interval '90 minutes', NULL),
    ('01990000-0000-7700-8000-000000000004', 1004, '01990000-0000-7300-8000-000000000003', '01990000-0000-7000-8000-000000000001', '21990000-0000-4000-8000-000000000004', 1, '@bob cập nhật tiến độ API giúp mọi người nhé.', NULL, '{"has_mentions":true}'::jsonb, 1, now() - interval '70 minutes', NULL),
    ('01990000-0000-7700-8000-000000000005', 1005, '01990000-0000-7300-8000-000000000004', '01990000-0000-7000-8000-000000000002', '21990000-0000-4000-8000-000000000005', 1, 'API đăng nhập đã xong, tiếp theo sẽ đồng bộ schema mới.', NULL, '{}'::jsonb, 1, now() - interval '50 minutes', NULL),
    ('01990000-0000-7700-8000-000000000006', 1006, '01990000-0000-7300-8000-000000000003', NULL, NULL, 2, 'Charlie đã tham gia server.', NULL, '{"event":"member_joined","user_id":"01990000-0000-7000-8000-000000000003"}'::jsonb, 1, now() - interval '40 minutes', NULL),
    ('01990000-0000-7700-8000-000000000007', 1007, '01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000001', '21990000-0000-4000-8000-000000000007', 3, 'Tài liệu mô hình database đính kèm.', NULL, '{"attachment_count":1}'::jsonb, 1, now() - interval '20 minutes', NULL);

SELECT setval(
    pg_get_serial_sequence('messaging.messages', 'sequence_no'),
    (SELECT max(sequence_no) FROM messaging.messages),
    true
);

INSERT INTO messaging.message_edits
    (id, message_id, version, previous_content, edited_by_user_id, edited_at)
VALUES
    ('01990000-0000-7710-8000-000000000001', '01990000-0000-7700-8000-000000000002', 1, 'Chào Alice, mình đã kiểm tra.', '01990000-0000-7000-8000-000000000002', now() - interval '110 minutes');

INSERT INTO messaging.attachments
    (id, message_id, storage_provider, bucket_name, object_key, original_name, mime_type, size_bytes, checksum_sha256, scan_status, created_at)
VALUES
    ('01990000-0000-7720-8000-000000000001', '01990000-0000-7700-8000-000000000007', 'minio', 'scdc-chat-dev', 'messages/01990000/database-design.pdf', 'database-design.pdf', 'application/pdf', 245760, repeat('f', 64), 1, now() - interval '20 minutes');

INSERT INTO messaging.reactions (message_id, user_id, reaction_key, created_at)
VALUES
    ('01990000-0000-7700-8000-000000000002', '01990000-0000-7000-8000-000000000001', '👍', now() - interval '105 minutes'),
    ('01990000-0000-7700-8000-000000000007', '01990000-0000-7000-8000-000000000002', '✅', now() - interval '15 minutes'),
    ('01990000-0000-7700-8000-000000000007', '01990000-0000-7000-8000-000000000003', '✅', now() - interval '14 minutes');

INSERT INTO messaging.mentions (message_id, mentioned_user_id, created_at)
VALUES
    ('01990000-0000-7700-8000-000000000004', '01990000-0000-7000-8000-000000000002', now() - interval '70 minutes');

INSERT INTO messaging.receipts (message_id, user_id, delivered_at, read_at)
VALUES
    ('01990000-0000-7700-8000-000000000001', '01990000-0000-7000-8000-000000000002', now() - interval '119 minutes', now() - interval '116 minutes'),
    ('01990000-0000-7700-8000-000000000002', '01990000-0000-7000-8000-000000000001', now() - interval '114 minutes', now() - interval '111 minutes');

INSERT INTO messaging.pinned_messages (space_id, message_id, pinned_by_user_id, pinned_at)
VALUES
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7700-8000-000000000007', '01990000-0000-7000-8000-000000000001', now() - interval '10 minutes');

INSERT INTO messaging.user_blocks (blocker_user_id, blocked_user_id, created_at)
VALUES
    ('01990000-0000-7000-8000-000000000003', '01990000-0000-7000-8000-000000000004', now() - interval '5 minutes');

-- Read state exists for direct/group participants and public server-channel members.
INSERT INTO messaging.space_user_states
    (space_id, user_id, last_read_sequence, last_read_at, notification_level, is_hidden, is_pinned, updated_at)
VALUES
    ('01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000001', 1002, now() - interval '111 minutes', 2, false, true,  now() - interval '111 minutes'),
    ('01990000-0000-7300-8000-000000000001', '01990000-0000-7000-8000-000000000002', 1002, now() - interval '110 minutes', 2, false, false, now() - interval '110 minutes'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000001', 1007, now() - interval '10 minutes',  2, false, true,  now() - interval '10 minutes'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000002', 1003, now() - interval '80 minutes',  2, false, false, now() - interval '80 minutes'),
    ('01990000-0000-7300-8000-000000000002', '01990000-0000-7000-8000-000000000003', 1007, now() - interval '14 minutes',  2, false, false, now() - interval '14 minutes'),
    ('01990000-0000-7300-8000-000000000003', '01990000-0000-7000-8000-000000000001', 1006, now() - interval '30 minutes',  2, false, false, now() - interval '30 minutes'),
    ('01990000-0000-7300-8000-000000000003', '01990000-0000-7000-8000-000000000002', 1004, now() - interval '60 minutes',  1, false, false, now() - interval '60 minutes'),
    ('01990000-0000-7300-8000-000000000003', '01990000-0000-7000-8000-000000000003', 1006, now() - interval '35 minutes',  2, false, false, now() - interval '35 minutes'),
    ('01990000-0000-7300-8000-000000000004', '01990000-0000-7000-8000-000000000001', 1005, now() - interval '45 minutes',  2, false, false, now() - interval '45 minutes'),
    ('01990000-0000-7300-8000-000000000004', '01990000-0000-7000-8000-000000000002', 1005, now() - interval '45 minutes',  2, false, false, now() - interval '45 minutes');

UPDATE messaging.spaces
SET last_message_id = '01990000-0000-7700-8000-000000000002',
    last_message_sequence = 1002,
    last_activity_at = now() - interval '110 minutes'
WHERE id = '01990000-0000-7300-8000-000000000001';

UPDATE messaging.spaces
SET last_message_id = '01990000-0000-7700-8000-000000000007',
    last_message_sequence = 1007,
    last_activity_at = now() - interval '20 minutes'
WHERE id = '01990000-0000-7300-8000-000000000002';

UPDATE messaging.spaces
SET last_message_id = '01990000-0000-7700-8000-000000000006',
    last_message_sequence = 1006,
    last_activity_at = now() - interval '40 minutes'
WHERE id = '01990000-0000-7300-8000-000000000003';

UPDATE messaging.spaces
SET last_message_id = '01990000-0000-7700-8000-000000000005',
    last_message_sequence = 1005,
    last_activity_at = now() - interval '50 minutes'
WHERE id = '01990000-0000-7300-8000-000000000004';

INSERT INTO integration.outbox_events
    (id, event_type, aggregate_type, aggregate_id, aggregate_version, space_id, payload, occurred_at, available_at, published_at, attempt_count)
VALUES
    ('01990000-0000-7800-8000-000000000001', 'MessageCreated', 'Message', '01990000-0000-7700-8000-000000000005', 1, '01990000-0000-7300-8000-000000000004', '{"message_id":"01990000-0000-7700-8000-000000000005","sequence_no":1005}'::jsonb, now() - interval '50 minutes', now() - interval '50 minutes', now() - interval '49 minutes', 1),
    ('01990000-0000-7800-8000-000000000002', 'MessageCreated', 'Message', '01990000-0000-7700-8000-000000000007', 1, '01990000-0000-7300-8000-000000000002', '{"message_id":"01990000-0000-7700-8000-000000000007","sequence_no":1007,"attachment_count":1}'::jsonb, now() - interval '20 minutes', now() - interval '20 minutes', NULL, 0);

COMMIT;

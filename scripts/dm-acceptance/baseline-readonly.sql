-- Always executed inside BEGIN READ ONLY by the PowerShell helper.
SELECT current_database() AS database, version() AS server;
SELECT u.id, u.username, u.status, p.display_name, p.bio,
       e.email, e.verified_at IS NOT NULL AS email_verified
FROM identity.users u
JOIN identity.user_profiles p ON p.user_id=u.id
JOIN identity.user_emails e ON e.user_id=u.id AND e.is_primary
WHERE u.username LIKE 'dm\_demo\_%' ESCAPE '\'
ORDER BY u.username;
SELECT u.username, count(s.id) AS all_sessions,
       count(s.id) FILTER (WHERE s.revoked_at IS NULL AND s.expires_at>now()) AS active_sessions
FROM identity.users u
LEFT JOIN identity.auth_sessions s ON s.user_id=u.id
WHERE u.username LIKE 'dm\_demo\_%' ESCAPE '\'
GROUP BY u.username ORDER BY u.username;
SELECT (SELECT count(*) FROM messaging.direct_conversations) AS direct_conversations,
       (SELECT count(*) FROM messaging.messages) AS messages,
       (SELECT count(*) FROM messaging.message_edits) AS message_edits;

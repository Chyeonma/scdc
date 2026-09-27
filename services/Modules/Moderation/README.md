# Messaging reports and moderation

Moderation owns `moderation.message_reports` and `moderation.actions`. Existing databases must run
`database/postgres/migrations/20260927_p8_t03_report_space.sql` before this module starts; fresh
databases use `schema.sql`.

Authenticated users can `POST /api/v1/spaces/{spaceId}/message-reports` with `messageId`,
`reasonCode` (`spam`, `harassment`, `inappropriate`, `security`, or `other`), and optional
`details` (at most 1000 characters). Messaging confirms the reporter can currently read the
undeleted, non-system message. A reporter can submit once per message and at most 10 reports in
24 hours. Snapshot content stays in the report row, not the action/audit row or application logs.

`GET /api/v1/spaces/{spaceId}/message-reports` returns up to 100 oldest pending reports. Only a
channel user with `message.delete`, an active group admin/owner, or a platform reviewer can read the queue. Queue
reads append `review_view` actions so evidence access is auditable. `POST
/api/v1/spaces/{spaceId}/message-reports/{reportId}/resolve` accepts `decision: dismiss|remove` and
optional `note`. Removing uses Messaging's existing transactional soft delete and outbox event.
The decision action is persisted as `remove_requested` before Messaging deletes the message;
status `1` remains in the queue and is retryable if deletion or finalization fails. Successful
removal changes the action to `remove_message`, report status to `2`; dismissal uses status `3`.
No separate hide state is introduced.

DM reports are accepted and can only be reviewed by a platform reviewer, not a server or group
moderator. `GET /api/v1/message-reports/access` tells the signed-in client whether the user has
this role; `GET /api/v1/message-reports` is the private cross-space queue. The role is stored in
`moderation.reviewers` and must be granted or revoked by a trusted database operator; there is
no self-service grant endpoint. After verifying the intended active account, grant it with
`INSERT INTO moderation.reviewers (user_id) VALUES ('<user-uuid>')`. Remove it with
`DELETE FROM moderation.reviewers WHERE user_id = '<user-uuid>'`.

Report snapshot/details retention and audit expiry jobs described in
`docs/messaging/policies.md` are operational follow-up work; no purge job is added here.

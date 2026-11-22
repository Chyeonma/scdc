\set ON_ERROR_STOP on
BEGIN;
CREATE SCHEMA media;
CREATE TABLE media.calls (
 id uuid PRIMARY KEY, caller_id uuid NOT NULL, peer_id uuid NOT NULL,
 caller_session_id uuid NOT NULL, caller_instance_id uuid NOT NULL,
 peer_session_id uuid, peer_instance_id uuid,
 caller_name text NOT NULL, peer_name text NOT NULL,
 status text NOT NULL CHECK(status IN ('ringing','active','ended')),
 end_reason text, version bigint NOT NULL DEFAULT 1 CHECK(version>0),
 room_name text NOT NULL UNIQUE, created_at timestamptz NOT NULL,
 ring_expires_at timestamptz NOT NULL, updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
 provider_closed boolean NOT NULL DEFAULT false,
 CHECK(caller_id<>peer_id), CHECK((peer_session_id IS NULL)=(peer_instance_id IS NULL)),
 CHECK(status<>'active' OR peer_session_id IS NOT NULL)
);
CREATE TABLE media.user_claims (
 user_id uuid PRIMARY KEY, call_id uuid NOT NULL REFERENCES media.calls(id)
);
CREATE TABLE media.operations (
 actor_id uuid NOT NULL, operation_id uuid NOT NULL, route text NOT NULL,
 fingerprint text NOT NULL, call_id uuid NOT NULL REFERENCES media.calls(id),
 PRIMARY KEY(actor_id,operation_id)
);
CREATE INDEX ix_calls_pending ON media.calls(updated_at) WHERE status<>'ended' OR NOT provider_closed;
CREATE INDEX ix_calls_caller ON media.calls(caller_id,created_at DESC);
CREATE INDEX ix_calls_peer ON media.calls(peer_id,created_at DESC);
COMMIT;

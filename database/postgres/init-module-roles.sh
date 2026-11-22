#!/bin/sh
set -eu
# Local development defaults; use separate persistent secrets outside development.
export IDENTITY_DB_PASSWORD="${IDENTITY_DB_PASSWORD:-scdc_identity_dev}"
export COMMUNITY_DB_PASSWORD="${COMMUNITY_DB_PASSWORD:-scdc_community_dev}"
export MESSAGING_DB_PASSWORD="${MESSAGING_DB_PASSWORD:-scdc_messaging_dev}"
psql -X --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --file /opt/scdc/module-roles.sql

#!/bin/sh
set -eu
umask 077
if ! find /app/messaging-cursor-keys -name 'key-*.xml' | grep -q .; then
  cp -a /app/legacy-cursors/. /app/messaging-cursor-keys/
fi

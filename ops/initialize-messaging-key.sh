#!/bin/sh
set -eu
umask 077
if [ ! -e /app/messaging-keys/dm-local-v1.key ]; then
  head -c 32 /dev/urandom > /app/messaging-keys/dm-local-v1.key
fi
test "$(wc -c < /app/messaging-keys/dm-local-v1.key)" -ge 32

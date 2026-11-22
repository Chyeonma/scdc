#!/usr/bin/env python3
"""Create ignored local credentials without replacing an existing .env."""
import base64
import os
from pathlib import Path
import secrets

target = Path(__file__).resolve().parent.parent / '.env'
values = {
    'COMMUNITY_OPERATION_KEY': base64.b64encode(secrets.token_bytes(32)).decode(),
    'MEDIA_DB_PASSWORD': secrets.token_hex(24),
    'LIVEKIT_API_KEY': 'scdc-' + secrets.token_hex(12),
    'LIVEKIT_API_SECRET': secrets.token_hex(32),
    'LIVEKIT_NODE_IP': '127.0.0.1',
}
try:
    descriptor = os.open(target, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
except FileExistsError:
    raise SystemExit('Existing .env preserved. Add missing variables using .env.example.')
with os.fdopen(descriptor, 'w') as output:
    output.write(''.join(f'{key}={value}\n' for key, value in values.items()))
print('Created .env with local random credentials (not tracked by Git).')

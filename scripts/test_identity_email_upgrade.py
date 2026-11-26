#!/usr/bin/env python3
"""Exercise the Compose owner-upgrade job on an explicitly isolated PostgreSQL container."""
import argparse
import hashlib
import os
from pathlib import Path
import subprocess
import uuid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--engine', choices=('docker', 'podman'), default='docker')
    parser.add_argument('--container', required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent / 'database/postgres/identity'

    def run(arguments, data=None, expected=0):
        result = subprocess.run([args.engine, *arguments], input=data, text=True,
                                stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        if result.returncode != expected:
            raise RuntimeError('Email migration check failed: ' + result.stdout[-2500:])
        return result.stdout

    source = run(['exec', args.container, 'printenv', 'POSTGRES_DB']).strip()
    if not source.endswith('_test'):
        raise RuntimeError('Only a container initialized with POSTGRES_DB ending in _test is allowed.')
    user = run(['exec', args.container, 'printenv', 'POSTGRES_USER']).strip()
    password = os.environ['SCDC_EMAIL_TEST_ADMIN_PASSWORD']
    database = 'scdc_email_script_' + uuid.uuid4().hex[:12] + '_test'
    run(['exec', args.container, 'mkdir', '-p', '/opt/scdc/identity', '/run/secrets'])
    run(['exec', '-i', args.container, 'sh', '-c',
         'umask 077; cat > /run/secrets/identity_database_admin_password'], data=password)
    for filename in ('migrate-email.sh', '20261126_e4-01_email_upgrade.sql'):
        run(['cp', str(root / filename), args.container + ':/opt/scdc/identity/' + filename])
    admin = ['exec', args.container, 'psql', '-X', '-U', user, '-d', source, '-v', 'ON_ERROR_STOP=1']
    run(admin + ['-c', 'CREATE DATABASE ' + database])
    try:
        psql = ['exec', '-i', args.container, 'psql', '-X', '-U', user, '-d', database, '-v', 'ON_ERROR_STOP=1']
        schema = (root / 'schema.sql').read_text()
        run(psql, data='CREATE EXTENSION pg_trgm;\n' + schema)
        run(psql + ['-v', 'baseline_checksum=' + hashlib.sha256(schema.encode()).hexdigest()],
            data=(root / 'baseline.sql').read_text())
        run(psql, data=(root / 'runtime-grants.sql').read_text())
        job = ['exec', '-e', 'PGHOST=127.0.0.1', '-e', 'PGUSER=' + user,
               '-e', 'PGDATABASE=' + database, args.container, '/bin/sh', '/opt/scdc/identity/migrate-email.sh']
        run(job)
        run(job)
        run(['exec', '-i', args.container, 'sh', '-c',
             'cat >> /opt/scdc/identity/20261126_e4-01_email_upgrade.sql'], data='\n-- checksum rejection test\n')
        run(job, expected=3)
        print('Identity email Compose upgrade: 3/3 passed (fresh, repeat, checksum rejection).')
    finally:
        run(admin + ['-c', 'DROP DATABASE ' + database + ' WITH (FORCE)'])


if __name__ == '__main__':
    main()

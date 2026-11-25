#!/usr/bin/env python3
"""Exercise the offline transfer on two dedicated PostgreSQL 18 test instances."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--engine', choices=('docker', 'podman'), default='docker')
    parser.add_argument('--source-container', required=True)
    parser.add_argument('--target-container', required=True)
    parser.add_argument('--source-port', required=True, type=int)
    parser.add_argument('--target-port', required=True, type=int)
    parser.add_argument('--keep', action='store_true', help='keep only this run\'s fixture databases for inspection')
    args = parser.parse_args()
    if args.source_container == args.target_container or args.source_port == args.target_port:
        parser.error('Use two independent test instances')
    os.umask(0o077)
    artifact = ROOT / 'artifacts/identity-transfer' / (datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S') + '-' + uuid.uuid4().hex[:8])
    artifact.mkdir(parents=True, mode=0o700)
    engine_path = shutil.which(args.engine)
    if not engine_path:
        parser.error('Container engine is unavailable')
    settings = {}
    for side, container, port in [('source', args.source_container, args.source_port), ('target', args.target_container, args.target_port)]:
        info = json.loads(subprocess.check_output([engine_path, 'inspect', container]))[0]
        config = dict(value.split('=', 1) for value in info['Config']['Env'])
        if not config.get('POSTGRES_DB', '').endswith('_test'):
            parser.error('Container POSTGRES_DB must end _test; never use development instances')
        password = config.get('POSTGRES_PASSWORD')
        if password is None:
            password = subprocess.check_output([engine_path, 'exec', container, 'cat', config['POSTGRES_PASSWORD_FILE']], text=True).strip()
        settings[side] = dict(container=container, port=port, user=config['POSTGRES_USER'], password=password, database=config['POSTGRES_DB'])

    def sql(side, query, database=None):
        item = settings[side]
        result = subprocess.run([engine_path, 'exec', '-i', item['container'], 'psql', '-X', '-v', 'ON_ERROR_STOP=1',
                                 '-U', item['user'], '-d', database or item['database'], '-At'], input=query, text=True, capture_output=True)
        if result.returncode:
            (artifact / 'private-sql-error.log').write_text(result.stderr)
            raise RuntimeError('Fixture SQL failed; private diagnostics saved')
        return result.stdout.strip()

    def conn(side, database):
        item = settings[side]
        quote = lambda value: '"' + str(value).replace('"', '""') + '"'
        return ';'.join(key + '=' + quote(value) for key, value in [('Host', '127.0.0.1'), ('Port', item['port']),
                        ('Database', database), ('Username', item['user']), ('Password', item['password'])]) + ';Pooling=false'

    originals = {}
    for side in settings:
        # Preserve test-cluster credentials if the caller reuses dedicated test instances.
        originals[side] = sql(side, "SELECT json_build_object('login',rolcanlogin,'password',rolpassword)::text FROM pg_authid WHERE rolname='scdc_identity'")
        sql(side, "DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='scdc_identity') THEN CREATE ROLE scdc_identity LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS; END IF; END $$")
    outcomes = []
    passed = False
    databases = []
    counter = 0

    def prepare(label):
        nonlocal counter
        counter += 1
        database = 'scdc_transfer_' + uuid.uuid4().hex + '_test'
        for side in settings:
            sql(side, 'ALTER ROLE scdc_identity LOGIN; CREATE DATABASE ' + database)
            databases.append((side, database))
        schema = (ROOT / 'database/postgres/schema.sql').read_text()
        sql('source', schema, database)
        sql('source', (ROOT / 'tests/fixtures/identity-transfer.sql').read_text(), database)
        sql('target', 'CREATE EXTENSION pg_trgm', database)
        baseline = (ROOT / 'database/postgres/identity/schema.sql').read_text()
        sql('target', baseline, database)
        sql('target', (ROOT / 'database/postgres/identity/baseline.sql').read_text().replace(":'baseline_checksum'", "'" + hashlib.sha256(baseline.encode()).hexdigest() + "'"), database)
        sql('target', (ROOT / 'database/postgres/identity/runtime-grants.sql').read_text(), database)
        return database

    def transfer(database, label, expected=0, extra=(), env_override=None, quiesced=True, target_container=None):
        env = dict(os.environ, ConnectionStrings__TransferSource=conn('source', database), ConnectionStrings__TransferTarget=conn('target', database))
        env.update(env_override or {})
        output = artifact / label
        command = ['dotnet', str(ROOT / 'tools/SCDC.DbMigrator/bin/Release/net10.0/SCDC.DbMigrator.dll'), 'identity-transfer',
                   '--engine', args.engine, '--source-container', args.source_container, '--target-container', target_container or args.target_container,
                   '--artifact-directory', str(output)] + (['--quiesced'] if quiesced else []) + list(extra)
        result = subprocess.run(command, cwd=ROOT, env=env, text=True, capture_output=True, timeout=120)
        (artifact / (label + '.log')).write_text(result.stdout + result.stderr)
        if result.returncode != expected:
            raise AssertionError(label + ': unexpected transfer exit; inspect private evidence')
        outcomes.append(label)
        return result.stdout + result.stderr

    def role(side):
        return sql(side, "SELECT rolcanlogin FROM pg_roles WHERE rolname='scdc_identity'")

    try:
        database = prepare('preservation')
        transfer(database, '01-preserve')
        original = json.loads((artifact / '01-preserve/source.json').read_text())
        restored = json.loads((artifact / '01-preserve/target.json').read_text())
        assert original == restored
        assert len(original['Tables']) == int(sql('source', "SELECT count(*) FROM pg_tables WHERE schemaname='identity'", database))
        assert role('source') == 'f' and role('target') == 't'
        assert sql('target', "SELECT security_stamp='22222222-2222-4222-8222-222222222222'::uuid FROM identity.user_security_states", database) == 't'
        assert sql('target', "SELECT last_value=37 AND is_called FROM identity.access_store_generation", database) == 't'
        assert sql('target', "SELECT NOT has_table_privilege('scdc_identity','identity.schema_migrations','SELECT') AND NOT has_table_privilege('scdc_identity','identity.legacy_schema_migrations','SELECT') AND NOT has_schema_privilege('scdc_identity','scdc_migrations','USAGE') AND NOT has_table_privilege('scdc_identity','identity.security_events','UPDATE') AND NOT has_table_privilege('scdc_identity','identity.access_change_acks','UPDATE') AND NOT has_table_privilege('scdc_identity','identity.access_scopes','DELETE')", database) == 't'
        assert 'already ready' in transfer(database, '02-repeat')
        sql('target', "SELECT nextval('identity.access_store_generation')", database)
        assert 'already ready' in transfer(database, '03-repeat-after-new-generation')
        assert sql('target', 'SELECT last_value FROM identity.access_store_generation', database) == '38'
        assert 'Rollback refused' in transfer(database, '04-refuse-post-write-rollback', 1, ['--rollback-before-writes'])
        assert role('source') == 'f' and role('target') == 't'

        database = prepare('rollback')
        transfer(database, '05-before-write-copy')
        transfer(database, '06-before-write-rollback', extra=['--rollback-before-writes'])
        assert role('source') == 't' and role('target') == 'f'
        transfer(database, '07-recutover')
        assert role('source') == 'f' and role('target') == 't'

        database = prepare('guards')
        assert 'quiesced' in transfer(database, '08-require-offline', 1, quiesced=False)
        assert 'different PostgreSQL instance' in transfer(database, '09-refuse-same-instance', 1, env_override={'ConnectionStrings__TransferTarget': conn('source', database)})
        rejected = transfer(database, '10-refuse-wrong-container', 1, target_container=args.source_container)
        assert 'endpoint differ' in rejected or 'psql failed' in rejected
        sql('target', "INSERT INTO identity.users(username,status) VALUES('existing_target',1)", database)
        assert 'contains business data' in transfer(database, '11-refuse-populated-target', 1)
        assert role('source') == 't'

        database = prepare('interrupted')
        wrapper = artifact / 'engine-wrapper'
        wrapper.mkdir()
        executable = wrapper / args.engine
        executable.write_text('#!/usr/bin/env python3\nimport os,sys\nif "pg_restore" in sys.argv: sys.exit(71)\nos.execv(' + repr(engine_path) + ',[' + repr(engine_path) + ']+sys.argv[1:])\n')
        executable.chmod(0o700)
        transfer(database, '12-injected-interrupted-restore', 1, env_override={'PATH': str(wrapper) + os.pathsep + os.environ['PATH']})
        assert role('source') == 'f' and role('target') == 'f'
        transfer(database, '13-resume-interrupted-restore')
        assert role('source') == 'f' and role('target') == 't'
        sql('source', "UPDATE identity.users SET username='changed_after_transfer'", database)
        assert 'Source changed' in transfer(database, '14-refuse-source-drift', 1)
        passed = True
        print(f'Identity transfer: {len(outcomes)}/{len(outcomes)} checks passed; separate PostgreSQL instances, manifests, guards, interruption, idempotence and rollback.')
    finally:
        if not args.keep:
            for side, database in reversed(databases):
                sql(side, 'DROP DATABASE IF EXISTS ' + database + ' WITH (FORCE)')
        for side, original in originals.items():
            if original:
                original = json.loads(original)
                password = 'NULL' if original['password'] is None else "'" + original['password'].replace("'", "''") + "'"
                sql(side, 'ALTER ROLE scdc_identity PASSWORD ' + password + (' LOGIN' if original['login'] else ' NOLOGIN'))
        (artifact / 'result.json').write_text(json.dumps({'checks': outcomes, 'passed': passed, 'kept_databases': databases if args.keep else []}, indent=2) + '\n')
        print('Evidence:', artifact.relative_to(ROOT))


if __name__ == '__main__':
    main()

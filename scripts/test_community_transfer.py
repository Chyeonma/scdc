#!/usr/bin/env python3
"""Exercise offline Community transfer on dedicated PostgreSQL 18 test databases."""
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
    os.umask(0o077)
    artifact = ROOT / 'artifacts/community-transfer' / (datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S') + '-' + uuid.uuid4().hex[:8])
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
                                 '-U', item['user'], '-d', (database[side] if isinstance(database, dict) else database) or item['database'], '-At'], input=query, text=True, capture_output=True)
        if result.returncode:
            (artifact / 'private-sql-error.log').write_text(result.stderr)
            raise RuntimeError('Fixture SQL failed; private diagnostics saved')
        return result.stdout.strip()

    def conn(side, database):
        item = settings[side]
        database = database[side] if isinstance(database, dict) else database
        quote = lambda value: '"' + str(value).replace('"', '""') + '"'
        return ';'.join(key + '=' + quote(value) for key, value in [('Host', '127.0.0.1'), ('Port', item['port']),
                        ('Database', database), ('Username', item['user']), ('Password', item['password'])]) + ';Pooling=false'

    originals = {}
    for side in settings:
        # Preserve test-cluster credentials if the caller reuses dedicated test instances.
        settings[side]['role'] = 'scdc_community' if side == 'source' else 'scdc_community_service'
        role_name = settings[side]['role']
        originals[side] = sql(side, f"SELECT json_build_object('login',rolcanlogin,'password',rolpassword)::text FROM pg_authid WHERE rolname='{role_name}'")
        sql(side, f"DO $$ BEGIN IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='{role_name}') THEN CREATE ROLE {role_name} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS; END IF; END $$")
    outcomes = []
    passed = False
    databases = []
    counter = 0

    def prepare(label):
        nonlocal counter
        counter += 1
        database = {side: 'scdc_c_transfer_' + side + '_' + uuid.uuid4().hex + '_test' for side in settings}
        for side in settings:
            sql(side, 'ALTER ROLE ' + settings[side]['role'] + ' LOGIN; CREATE DATABASE ' + database[side])
            databases.append((side, database[side]))
        schema = (ROOT / 'database/postgres/schema.sql').read_text()
        sql('source', schema, database)
        sql('source', (ROOT / 'database/postgres/seed.sql').read_text(), database)
        sql('source', (ROOT / 'tests/fixtures/community-transfer.sql').read_text(), database)
        sql('source', (ROOT / 'database/postgres/community/runtime-grants.sql').read_text(), database)
        sql('target', 'CREATE EXTENSION pg_trgm', database)
        baseline = (ROOT / 'database/postgres/community/schema.sql').read_text()
        sql('target', baseline, database)
        sql('target', (ROOT / 'database/postgres/community/baseline.sql').read_text(), database)
        metadata=(ROOT / 'database/postgres/community/service-baseline.sql').read_text().replace(":'baseline_checksum'", "'" + hashlib.sha256(baseline.encode()).hexdigest() + "'").replace(":'baseline_origin'", "'transfer'").replace(":'source_id'", "''")
        sql('target', metadata, database)
        sql('target', (ROOT / 'database/postgres/community/runtime-grants.sql').read_text().replace('scdc_community','scdc_community_service'), database)
        return database

    def transfer(database, label, expected=0, extra=(), env_override=None, quiesced=True, target_container=None):
        env = dict(os.environ, ConnectionStrings__TransferSource=conn('source', database), ConnectionStrings__TransferTarget=conn('target', database))
        env.update(env_override or {})
        output = artifact / label
        command = ['dotnet', str(ROOT / 'tools/SCDC.DbMigrator/bin/Release/net10.0/SCDC.DbMigrator.dll'), 'community-transfer',
                   '--engine', args.engine, '--source-container', args.source_container, '--target-container', target_container or args.target_container,
                   '--artifact-directory', str(output)] + (['--quiesced'] if quiesced else []) + list(extra)
        result = subprocess.run(command, cwd=ROOT, env=env, text=True, capture_output=True, timeout=120)
        (artifact / (label + '.log')).write_text(result.stdout + result.stderr)
        if result.returncode != expected:
            raise AssertionError(label + ': unexpected transfer exit; inspect private evidence')
        outcomes.append(label)
        return result.stdout + result.stderr

    def role(side):
        return sql(side, "SELECT rolcanlogin FROM pg_roles WHERE rolname='" + settings[side]["role"] + "'")

    def gate(database, label, rollback=False, expected=0):
        if args.source_container != args.target_container:
            return
        subprocess.run([engine_path, 'exec', args.source_container, 'mkdir', '-p', '/opt/scdc/community'], check=True)
        subprocess.run([engine_path, 'cp', str(ROOT/'database/postgres/community')+'/.', args.source_container+':/opt/scdc/community'], check=True, stdout=subprocess.DEVNULL)
        env = dict(os.environ, PGPASSWORD=settings['source']['password'])
        command = [engine_path, 'exec', '-e', 'PGPASSWORD', '-e', 'PGHOST=127.0.0.1',
                   '-e', 'PGUSER='+settings['source']['user'], '-e', 'PGDATABASE='+database['source'],
                   '-e', 'COMMUNITY_DATABASE_NAME='+database['target'], args.source_container,
                   '/bin/sh', '/opt/scdc/community/cutover-gate.sh'] + (['--rollback'] if rollback else [])
        result = subprocess.run(command, env=env, capture_output=True)
        (artifact/(label+'.log')).write_bytes(result.stdout+result.stderr)
        assert result.returncode == expected, label
        outcomes.append(label)

    try:
        database = prepare('preservation')
        transfer(database, '01-preserve')
        gate(database, '01a-forward-gate')
        gate(database, '01b-refuse-rollback-route-without-verified-rollback', True, 1)
        original = json.loads((artifact / '01-preserve/source.json').read_text())
        restored = json.loads((artifact / '01-preserve/target.json').read_text())
        assert original == restored
        assert len(original['Tables']) == int(sql('source', "SELECT count(*) FROM pg_tables WHERE schemaname='community'", database))
        assert role('source') == 'f' and role('target') == 't'

        assert sql('target', "SELECT version=7 AND attempt_count=3 AND phase=0 FROM community.access_changes", database) == 't'
        assert sql('target', "SELECT last_value=37 AND is_called FROM community.transfer_fixture_sequence", database) == 't'
        not_null_names = "SELECT json_agg(json_build_array(r.relname,a.attname,c.conname) ORDER BY r.relname,a.attname,c.conname)::text FROM pg_constraint c JOIN pg_class r ON r.oid=c.conrelid JOIN pg_namespace n ON n.oid=r.relnamespace JOIN pg_attribute a ON a.attrelid=r.oid AND a.attnum=c.conkey[1] WHERE n.nspname='community' AND c.contype='n'"
        assert sql('source', not_null_names, database) == sql('target', not_null_names, database)
        assert sql('target', "SELECT NOT has_table_privilege('scdc_community_service','community.schema_migrations','SELECT') AND NOT has_table_privilege('scdc_community_service','community.legacy_schema_migrations','SELECT') AND NOT has_schema_privilege('scdc_community_service','scdc_migrations','USAGE') AND NOT has_table_privilege('scdc_community_service','community.access_change_acks','UPDATE') AND NOT has_table_privilege('scdc_community_service','community.access_scopes','DELETE')", database) == 't'
        assert 'already ready' in transfer(database, '02-repeat')
        sql('target', "SELECT nextval('community.transfer_fixture_sequence')", database)
        assert 'already ready' in transfer(database, '03-repeat-after-new-generation')
        assert sql('target', 'SELECT last_value FROM community.transfer_fixture_sequence', database) == '38'
        assert 'Rollback refused' in transfer(database, '04-refuse-post-write-rollback', 1, ['--rollback-before-writes'])
        gate(database, '04a-refuse-rollback-route-after-target-writes', True, 1)
        assert role('source') == 'f' and role('target') == 't'

        database = prepare('rollback')
        transfer(database, '05-before-write-copy')
        transfer(database, '06-before-write-rollback', extra=['--rollback-before-writes'])
        assert role('source') == 't' and role('target') == 'f'
        gate(database, '06a-rollback-gate', True)
        gate(database, '06b-refuse-forward-route-after-rollback', False, 1)
        count = subprocess.check_output([engine_path, 'exec', args.source_container, 'psql', '-XAt', '-U', 'scdc_community', '-d', database['source'], '-c', 'SELECT count(*) FROM community.servers'], text=True).strip()
        assert int(count) > 0
        transfer(database, '07-recutover')
        assert role('source') == 'f' and role('target') == 't'

        database = prepare('guards')
        assert 'quiesced' in transfer(database, '08-require-offline', 1, quiesced=False)
        assert 'identical' in transfer(database, '09-refuse-same-instance', 1, env_override={'ConnectionStrings__TransferTarget': conn('source', database['source'])})
        rejected = transfer(database, '10-refuse-wrong-container', 1, target_container='scdc-community-transfer-missing-test')
        assert 'endpoint differ' in rejected or 'psql failed' in rejected
        watcher = subprocess.Popen([engine_path, 'exec', '-e', 'PGAPPNAME=SCDC.CommunityTransferFixture', args.source_container,
                                    'psql', '-X', '-U', 'scdc_community', '-d', settings['source']['database'], '-c', 'SELECT pg_sleep(30)'],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            import time
            for _ in range(50):
                if sql('source', "SELECT count(*) FROM pg_stat_activity WHERE application_name='SCDC.CommunityTransferFixture'") == '1':
                    break
                time.sleep(.1)
            else:
                raise AssertionError('Runtime test connection did not start')
            assert 'Runtime role has clients' in transfer(database, '10a-refuse-runtime-client-in-another-database', 1)
        finally:
            sql('source', "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE application_name='SCDC.CommunityTransferFixture' AND usename='scdc_community'")
            watcher.wait(timeout=10)
        sql('target', "UPDATE community.permissions SET description='unexpected' WHERE code='channel_view'", database)
        assert 'seed catalog differs' in transfer(database, '10b-refuse-modified-target-seed', 1)
        sql('target', "UPDATE community.permissions SET description='View a channel' WHERE code='channel_view'", database)
        sql('target', "INSERT INTO community.inbox_events(consumer_name,event_id) VALUES('existing',gen_random_uuid())", database)
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
        sql('source', "UPDATE community.permissions SET description='changed_after_transfer' WHERE code='channel_view'", database)
        assert 'Source changed' in transfer(database, '14-refuse-source-drift', 1)
        passed = True
        print(f'Community transfer: {len(outcomes)}/{len(outcomes)} checks passed; owner databases, manifests, guards, interruption, idempotence and rollback.')
    finally:
        if not args.keep:
            for side, database in reversed(databases):
                sql(side, 'DROP DATABASE IF EXISTS ' + database + ' WITH (FORCE)')
        for side, original in originals.items():
            if original:
                original = json.loads(original)
                password = 'NULL' if original['password'] is None else "'" + original['password'].replace("'", "''") + "'"
                sql(side, 'ALTER ROLE ' + settings[side]['role'] + ' PASSWORD ' + password + (' LOGIN' if original['login'] else ' NOLOGIN'))
        (artifact / 'result.json').write_text(json.dumps({'checks': outcomes, 'passed': passed, 'kept_databases': databases if args.keep else []}, indent=2) + '\n')
        print('Evidence:', artifact.relative_to(ROOT))


if __name__ == '__main__':
    main()

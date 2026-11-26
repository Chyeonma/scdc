#!/usr/bin/env python3
"""Linux/macOS driver for dm-mvp on an isolated local gateway test stack.

Requires DM_WEB_URL, PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD and psql.
For host-side setup without psql, DM_MVP_PG_CONTAINER and DM_MVP_ENGINE
may select the PostgreSQL container of a dedicated scdc-gateway-* stack.
Runtime manifests contain IDs only; tokens remain in memory.
"""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
PASSWORD = 'DmDemo2026!Local'
CASES = ['C01', 'C02', 'C03', 'C04', 'C05', 'C06', 'C07', 'C08', 'C09', 'C10',
         'C02P1', 'C02P2', 'C02LIVE', 'MOBILE', 'EDITOLD', 'DELETEOLD']
ALIASES = ['B', 'C', 'S01', 'S02', 'S03', 'S04', 'S05', 'S06', 'S07', 'K',
           'S08', 'S09', 'S10', 'S11', 'S12', 'S13']


def sql(query, readonly=True):
    if os.environ.get('PGHOST') not in ('localhost', '127.0.0.1') or os.environ.get('PGDATABASE') != 'scdc_gateway_test':
        raise RuntimeError('Use a dedicated localhost scdc_gateway_test database.')
    command = ['psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1']
    container = os.environ.get('DM_MVP_PG_CONTAINER')
    if container:
        engine = os.environ.get('DM_MVP_ENGINE', 'podman')
        if engine not in ('podman', 'docker') or not re.fullmatch(r'scdc-gateway-[a-z0-9-]+[-_]postgres[-_]1', container):
            raise RuntimeError('Expected the PostgreSQL container of a dedicated gateway stack.')
        user, database = 'scdc_gateway_test', 'scdc_gateway_test'
        if re.search(r'\bidentity\.', query):
            container = os.environ.get('DM_MVP_IDENTITY_PG_CONTAINER', '')
            if not re.fullmatch(r'scdc-gateway-[a-z0-9-]+[-_]identity-postgres[-_]1', container):
                raise RuntimeError('Identity SQL requires the dedicated gateway Identity container.')
            user, database = 'scdc_identity_admin', 'scdc_identity_gateway_test'
        command = [engine, 'exec', '-i', container, *command, '-U', user, '-d', database]
    prefix = 'BEGIN READ ONLY;' if readonly else 'BEGIN;'
    result = subprocess.run(command, input=prefix + query + '\nCOMMIT;', text=True,
                            capture_output=True, timeout=30)
    if result.returncode:
        raise RuntimeError('Test database command failed; output suppressed.')
    return result.stdout.strip()


def api(method, route, body=None, auth=None, expected=200):
    origin = os.environ.get('DM_WEB_URL', '')
    url = urllib.parse.urlparse(origin)
    if url.scheme != 'http' or url.hostname not in ('localhost', '127.0.0.1') or url.path not in ('', '/'):
        raise RuntimeError('DM_WEB_URL must identify the dedicated local gateway.')
    headers = {'Content-Type': 'application/json'}
    if auth:
        headers['Authorization'] = 'Bearer ' + auth['accessToken']
    request = urllib.request.Request(origin.rstrip('/') + '/api/v1' + route, method=method,
                                    data=json.dumps(body).encode() if body is not None else None, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            if response.status != expected:
                raise RuntimeError(f'{method} {route} returned {response.status}; expected {expected}.')
            return json.load(response) if response.status != 204 else None
    except urllib.error.HTTPError as error:
        raise RuntimeError(f'{method} {route} returned {error.code}; body suppressed.') from None


def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('-Action', choices=['Setup', 'Fixtures', 'Snapshot', 'Seed101', 'State'], required=True)
    parser.add_argument('-Run', required=True)
    parser.add_argument('-Case', choices=CASES, default='C01')
    parser.add_argument('-Alias', default='A')
    parser.add_argument('-State', choices=['Disabled', 'Unlock'], default='Disabled')
    args = parser.parse_args()
    if not re.fullmatch(r'[a-z0-9][a-z0-9-]{0,11}', args.Run) or args.Run == 'baseline':
        parser.error('Choose a new short dedicated run name.')
    if sql('SELECT current_database();') != 'scdc_gateway_test':
        raise RuntimeError('Unexpected database.')
    folder = ROOT / '.dm-acceptance/runs' / args.Run
    manifest_path = folder / 'manifest.json'
    suffix = '_' + args.Run.replace('-', '_')
    if args.Action == 'Setup':
        if manifest_path.exists():
            raise RuntimeError('Run exists; use a new run. No reset performed.')
        plan = json.loads((ROOT / 'docs/fixtures/dm-demo-plan.json').read_text())
        accounts = list(plan['accounts']) + [dict(alias=f'S{i:02}', username=f'dm_demo_search{i:02}',
                    displayName=f'Người tìm {i:02}', initialState='active_verified') for i in range(1, 24)]
        manifest = []
        folder.mkdir(parents=True, exist_ok=False)
        for account in accounts:
            username = account['username'] + suffix
            if not re.fullmatch(r'[a-z0-9_.]{3,32}', username):
                raise RuntimeError('Run username too long.')
            result = api('POST', '/auth/register', dict(username=username, displayName=account['displayName'],
                         email=username + '@example.test', password=PASSWORD), expected=201)
            if account['initialState'] == 'active_verified':
                api('POST', '/auth/verify-email', dict(token=result['developmentVerificationToken']), expected=204)
            manifest.append(dict(alias=account['alias'], id=result['userId'], username=username, displayName=account['displayName']))
        write(manifest_path, dict(run=args.Run, database='scdc_gateway_test', accounts=manifest))
        print('Run accounts registered through Identity; no tokens saved.')
        return
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    accounts = {a['alias']: a for a in manifest['accounts']}
    actor = accounts[args.Alias]
    actor_id = str(uuid.UUID(actor['id']))
    if not re.fullmatch(r'[a-z0-9_.]{3,32}', actor['username']) or not actor['username'].endswith(suffix):
        raise RuntimeError('Actor is outside this run.')
    if sql(f"SELECT count(*) FROM identity.users WHERE id='{actor_id}' AND username='{actor['username']}';") != '1':
        raise RuntimeError('Run actor missing.')
    if args.Action == 'State':
        change = f"UPDATE identity.users SET status={3 if args.State == 'Disabled' else 1} WHERE id='{actor_id}';"
        if args.State == 'Disabled':
            change += f"UPDATE identity.user_security_states SET security_stamp=gen_random_uuid() WHERE user_id='{actor_id}'; UPDATE identity.auth_sessions SET revoked_at=clock_timestamp(),revoke_reason='owned_mvp_disabled' WHERE user_id='{actor_id}' AND revoked_at IS NULL;"
        sql(f"SELECT id FROM identity.users WHERE id='{actor_id}' FOR UPDATE;" + change, readonly=False)
        proof = json.loads(sql(f"SELECT json_build_object('userId','{actor_id}','state','{args.State}','commitConfirmedAt',clock_timestamp(),'status',(SELECT status FROM identity.users WHERE id='{actor_id}'));"))
        write(folder / f'state-{args.Alias}-{args.State}.json', proof)
        print(json.dumps(proof))
        return
    fixture_path = folder / 'mvp-fixtures.json'
    if args.Action == 'Snapshot':
        fixture = json.loads(fixture_path.read_text())
        pair = next(p for p in fixture['pairs'] if p['case'] == args.Case)
        space = str(uuid.UUID(pair['conversationId']))
        member_ids = [str(uuid.UUID(accounts[a]['id'])) for a in ('A', pair['peerAlias'])]
        if sql(f"SELECT count(*) FROM messaging.direct_conversations WHERE space_id='{space}' AND user_low_id IN ('{member_ids[0]}','{member_ids[1]}') AND user_high_id IN ('{member_ids[0]}','{member_ids[1]}');") != '1':
            raise RuntimeError('Conversation is outside this run.')
        # Share the exact snapshot projection with the Windows driver.
        source = (ROOT / 'scripts/dm-acceptance/mvp.ps1').read_text(encoding='utf-8-sig')
        query = source.split('if($Action -eq \'Snapshot\'){', 1)[1].split('Invoke-DmReadSql @"', 1)[1].split('"@;exit', 1)[0]
        print(sql(query.replace('$space', space)))
        return
    auth = api('POST', '/auth/login', dict(login=actor['username'], password=PASSWORD, deviceName='MVP-local'))
    try:
        if args.Action == 'Fixtures':
            if fixture_path.exists():
                raise RuntimeError('Fixtures exist; use a new run.')
            pairs = []
            for case, alias in zip(CASES, ALIASES):
                peer = accounts[alias]
                result = api('POST', '/direct-conversations', dict(peerUserId=peer['id']), auth)
                pairs.append(dict(case=case, peerAlias=alias, peerUsername=peer['username'], conversationId=result['id'],
                                  X=str(uuid.uuid4()), Y=str(uuid.uuid4())))
            write(fixture_path, dict(run=args.Run, actorUsername=actor['username'], pairs=pairs))
            print('Created sixteen empty conversation lanes.')
        else:
            fixture = json.loads(fixture_path.read_text())
            pair = next(p for p in fixture['pairs'] if p['case'] == args.Case)
            space = str(uuid.UUID(pair['conversationId']))
            before = int(sql(f"SELECT count(*) FROM messaging.messages WHERE space_id='{space}';"))
            if before not in (0, 1):
                raise RuntimeError('Seed101 requires a fresh lane or one old message.')
            receipts = []
            for index in range(1, 102):
                operation = str(uuid.uuid4())
                result = api('POST', f'/direct-conversations/{space}/messages', dict(clientMessageId=operation, content=f'RC-{index:03}'), auth)
                receipts.append({key: result[key] for key in ('id', 'clientMessageId', 'sequence', 'version')})
            write(folder / f'seed-{args.Case}.json', dict(case=args.Case, conversationId=space, before=before, receipts=receipts, count=101))
            print('101 actual API writes committed.')
    finally:
        api('POST', '/auth/logout', dict(refreshToken=auth['refreshToken']), expected=204)


if __name__ == '__main__':
    main()

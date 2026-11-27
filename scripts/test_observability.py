#!/usr/bin/env python3
"""Verify real OTLP ingestion, safe telemetry and collector outage on an isolated stack."""
import argparse
import base64
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--engine', choices=['docker', 'podman'], default='docker')
    parser.add_argument('--port', type=int, default=15410)
    parser.add_argument('--observe-port', type=int, default=15480)
    parser.add_argument('--keep', action='store_true')
    args = parser.parse_args()
    os.umask(0o077)
    if any(not 1024 <= p <= 65535 for p in [args.port, args.observe_port]) or args.port == args.observe_port:
        parser.error('Use two different unprivileged ports')
    project = 'scdc-observe-' + datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S') + f'-{os.getpid()}'
    artifact = ROOT / 'artifacts/observability' / project
    artifact.mkdir(parents=True, mode=0o700)
    env = dict(os.environ, GATEWAY_WEB_PORT=str(args.port), OBSERVE_TEST_PORT=str(args.observe_port))
    compose = [args.engine, 'compose', '-p', project, '-f', str(ROOT / 'tests/compose/compose.gateway-test.yaml'),
               '-f', str(ROOT / 'tests/compose/compose.observability-test.yaml')]
    origin = f'http://127.0.0.1:{args.port}'
    observe = f'http://127.0.0.1:{args.observe_port}'
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    trace_id = secrets.token_hex(16)
    canary = 'SCDC_PRIVATE_CANARY_' + secrets.token_hex(8)
    results = {'project': project, 'trace_id': trace_id, 'gateway': origin, 'openobserve': observe,
               'checks': {}, 'passed': False, 'kept': args.keep}
    private_directory = artifact / 'private'
    private_directory.mkdir(mode=0o700)
    community_env = private_directory / 'community.env'
    community_env.write_text('COMMUNITY_OPERATION_KEY=Z2F0ZXdheS10ZXN0LW9ubHkta2V5LWF0LWxlYXN0LTMyLWJ5dGVz\n')
    for script, directory, variable in [
        ('init_rpc_certificates.py', 'rpc', 'SCDC_RPC_SECRET_DIR'),
        ('init_identity_database_secrets.py', 'identity-database', 'SCDC_IDENTITY_DB_SECRET_DIR'),
        ('init_community_database_secrets.py', 'community-database', 'SCDC_COMMUNITY_DB_SECRET_DIR'),
    ]:
        secret_directory = artifact / 'private' / directory
        command = [sys.executable, str(ROOT / 'scripts' / script), str(secret_directory), '--label-for-containers']
        if script == 'init_rpc_certificates.py':
            command += ['--include-community']
        elif script == 'init_community_database_secrets.py':
            command += ['--env-file', str(community_env)]
        provision = subprocess.run(command,
                                  cwd=ROOT, text=True, capture_output=True)
        (artifact / ('initialize-' + directory + '.log')).write_text(provision.stdout + provision.stderr)
        if provision.returncode:
            raise RuntimeError('Failed to initialize private test secrets')
        env[variable] = str(secret_directory)

    def run(parts, name, timeout=600, check=True):
        print(name, flush=True)
        done = subprocess.run(compose + parts, cwd=ROOT, env=env, text=True, capture_output=True, timeout=timeout)
        (artifact / (name + '.log')).write_text(done.stdout + done.stderr)
        if done.returncode and check:
            raise RuntimeError(f'{name}: exit {done.returncode}; see {artifact}')
        return done.stdout

    def request(url, data=None, headers=None):
        req = urllib.request.Request(url, data=None if data is None else json.dumps(data).encode(),
                                     headers={'Content-Type': 'application/json'} | (headers or {}))
        try:
            with opener.open(req, timeout=20) as response:
                return response.status, response.read().decode()
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode()

    def wait_for(check, name, attempts=90):
        error = None
        for _ in range(attempts):
            try:
                if check():
                    results['checks'][name] = True
                    print(name + ': passed', flush=True)
                    return
            except (OSError, ValueError, urllib.error.URLError) as exc:
                error = str(exc)
            time.sleep(1)
        raise RuntimeError(f'{name} timed out: {error}')

    def search(kind):
        auth = base64.b64encode(b'root@scdc-test.local:ObserveTest!2026').decode()
        status, body = request(observe + '/api/default/_search?type=' + kind,
                               {'query': {'sql': 'SELECT * FROM scdc', 'start_time': start_us,
                                          'end_time': int(time.time() * 1_000_000), 'from': 0, 'size': 1000}},
                               {'Authorization': 'Basic ' + auth})
        if status != 200:
            (artifact / (kind + '-search-error.json')).write_text(body)
            return []
        value = json.loads(body)
        (artifact / (kind + '.json')).write_text(json.dumps(value, indent=2))
        return value.get('hits', [])

    start_us = int(time.time() * 1_000_000)
    headers = {'traceparent': '00-' + trace_id + '-' + secrets.token_hex(8) + '-01',
               'Authorization': 'Bearer ' + canary, 'Cookie': 'private=' + canary,
               'baggage': 'private=' + canary}
    try:
        run(['config', '--quiet'], 'compose-config')
        run(['build'], 'build')
        run(['up', '-d'], 'start-stack')
        wait_for(lambda: request(origin + '/api/v1/health')[0] == 200
                 and request(origin + '/api/v1/health/identity/access')[0] == 200 and request(origin + '/health')[0] == 200,
                 'services-ready')
        wait_for(lambda: request(observe + '/healthz')[0] == 200, 'openobserve-ready')
        assert request(origin + '/health?access_token=' + canary, headers=headers)[0] == 200
        run(['stop', 'identity-postgres'], 'stop-test-database')
        code, body = request(origin + '/api/v1/auth/login?access_token=' + canary,
                             {'login': 'probe@scdc-test.local', 'password': canary}, headers)
        (artifact / 'failure-response.json').write_text(body)
        assert code == 503, f'Expected Identity database unavailability, got {code}'
        assert trace_id in body, 'Gateway did not preserve the incoming W3C trace context'
        results['checks']['gateway-error-context'] = True
        run(['start', 'identity-postgres'], 'restart-test-database')

        def ingested():
            spans = search('traces')
            logs = search('logs')
            return (any(s.get('trace_id') == trace_id and s.get('service_name') == 'scdc-identity'
                        and s.get('http_response_status_code') == '503' and s.get('span_status') == 'ERROR'
                        for s in spans)
                    and any(s.get('trace_id') == trace_id and s.get('service_name') == 'scdc-sample' for s in spans)
                    and any(l.get('trace_id') == trace_id and l.get('service_name') == 'scdc-identity'
                            and l.get('statuscode') == 503 and l.get('severity') == 'Error' for l in logs)
                    and any(l.get('trace_id') == trace_id and l.get('service_name') == 'scdc-sample' for l in logs))
        wait_for(ingested, 'correlated-logs-traces')
        stored = (artifact / 'traces.json').read_text() + (artifact / 'logs.json').read_text()
        assert canary not in stored, 'Sensitive request data leaked into telemetry'
        results['checks']['private-data-excluded'] = True
        def metrics_ingested():
            auth = base64.b64encode(b'root@scdc-test.local:ObserveTest!2026').decode()
            status, body = request(observe + '/api/default/streams?type=metrics',
                                   headers={'Authorization': 'Basic ' + auth})
            (artifact / 'metrics-streams.json').write_text(body)
            return status == 200 and 'http_server_request_duration' in body
        wait_for(metrics_ingested, 'http-metrics-ingested')
        run(['stop', 'otel-collector'], 'stop-collector')
        started = time.monotonic()
        for route in ['/health', '/api/v1/health']:
            assert request(origin + route)[0] == 200
        results['collector_outage_request_seconds'] = round(time.monotonic() - started, 3)
        assert results['collector_outage_request_seconds'] < 5
        results['checks']['collector-outage-does-not-block-api'] = True
        run(['start', 'otel-collector'], 'restart-collector')
        recovery = secrets.token_hex(16)
        results['recovery_trace_id'] = recovery
        def recovered():
            assert request(origin + '/health', headers={'traceparent': '00-' + recovery + '-' + secrets.token_hex(8) + '-01'})[0] == 200
            return recovery in json.dumps(search('traces'))
        wait_for(recovered, 'export-recovers-after-collector-restart')
        results['passed'] = True
    finally:
        try:
            run(['logs', '--no-color', '--tail', '150'], 'container-logs', timeout=30, check=False)
        finally:
            try:
                if not args.keep:
                    run(['down', '--volumes', '--remove-orphans'], 'cleanup', timeout=90)
            finally:
                (artifact / 'result.json').write_text(json.dumps(results, indent=2) + '\n')
                print('Evidence: ' + str(artifact.relative_to(ROOT)), flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

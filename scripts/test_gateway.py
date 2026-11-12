#!/usr/bin/env python3
"""Build an isolated gateway stack, run its browser checks, then remove test data."""

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.error
import urllib.request

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--engine', choices=['docker', 'podman'], default='docker')
    parser.add_argument('--port', type=int, default=15310)
    parser.add_argument('--keep', action='store_true', help='keep the dedicated test stack for inspection')
    args = parser.parse_args()
    if not 1024 <= args.port <= 65535:
        parser.error('--port must be between 1024 and 65535')
    project = 'scdc-gateway-' + datetime.now(timezone.utc).strftime('%Y%m%d%H%M%S') + f'-{os.getpid()}'
    artifact = ROOT / 'artifacts/gateway' / project
    artifact.mkdir(parents=True)
    origin = f'http://127.0.0.1:{args.port}'
    env = dict(os.environ, GATEWAY_WEB_PORT=str(args.port), SCDC_GATEWAY_URL=origin,
               SCDC_GATEWAY_DATABASE='scdc_gateway_test', SCDC_GATEWAY_PROJECT=project,
               SCDC_GATEWAY_ENGINE=args.engine, SCDC_GATEWAY_ARTIFACTS=str(artifact))
    compose = [args.engine, 'compose', '-p', project, '-f', str(ROOT / 'compose.gateway-test.yaml')]

    def run(command, name, timeout=600):
        print(name, flush=True)
        result = subprocess.run(command, cwd=ROOT, env=env, text=True, capture_output=True, timeout=timeout)
        output = result.stdout + result.stderr
        (artifact / (name.replace(' ', '-') + '.log')).write_text(output)
        if result.returncode:
            raise RuntimeError(f'{name} failed (exit {result.returncode}):\n{output[-5000:]}')
        return output

    passed = False
    try:
        run(compose + ['config', '--quiet'], 'compose config')
        run(compose + ['build'], 'build')
        run(compose + ['up', '-d', 'postgres'], 'start postgres')
        run(compose + ['run', '--rm', '--no-deps', '--entrypoint', '/bin/sh', 'chat-service', '-c',
            'umask 077; if [ ! -s /app/messaging-keys/dm-local-v1.key ]; then '
            'head -c 32 /dev/urandom > /app/messaging-keys/dm-local-v1.key; fi; '
            'test "$(wc -c < /app/messaging-keys/dm-local-v1.key)" -ge 32'], 'initialize DM key')
        run(compose + ['up', '-d'], 'start gateway stack')
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
        for attempt in range(120):
            try:
                with opener.open(origin + '/api/v1/health', timeout=2) as response:
                    api_ready = response.status == 200
                with opener.open(origin + '/health', timeout=2) as response:
                    sample = json.load(response)
                    if api_ready and response.status == 200 and sample.get('service') == 'sample-service' and sample.get('status') == 'healthy':
                        break
            except (OSError, urllib.error.URLError):
                pass
            time.sleep(0.5)
        else:
            raise RuntimeError('API and sample service did not become reachable through the gateway')
        run(compose + ['exec', '-T', 'web-client', 'nginx', '-t'], 'nginx syntax')
        run(compose + ['exec', '-T', 'web-client', 'nginx', '-v'], 'nginx version')
        run(compose + ['ps', '--format', 'json'], 'containers')
        run(['npm', '--prefix', 'clients/WebClient', 'run', 'test:e2e', '--', 'gateway'], 'browser checks')
        passed = True
    finally:
        try:
            run(compose + ['logs', '--no-color', '--tail', '200'], 'container logs', timeout=30)
        finally:
            if not args.keep:
                run(compose + ['down', '--volumes', '--remove-orphans'], 'cleanup test stack', timeout=90)
            (artifact / 'result.json').write_text(json.dumps({
                'project': project, 'engine': args.engine, 'gateway': origin,
                'database': 'scdc_gateway_test', 'passed': passed, 'kept': args.keep,
                'sample_health': origin + '/health',
            }, indent=2) + '\n')
            print(f'Evidence: {artifact.relative_to(ROOT)}', flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

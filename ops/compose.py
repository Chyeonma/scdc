#!/usr/bin/env python3
"""Run the SCDC runtime after explicit preparation and database preflight checks."""

import os
from pathlib import Path
import shlex
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[1]
WRITERS = ("web-client", "community-service", "chat-service", "identity-service")
INFRASTRUCTURE = ("postgres", "identity-postgres", "auth-redis")


class Stack:
    def __init__(self, arguments=(), environment=None):
        self.environment = dict(os.environ if environment is None else environment)
        self.rollback = self.environment.get("SCDC_ROLLBACK", "0") == "1"
        self.development = self.environment.get("SCDC_DEV", "1") == "1"
        self.sample = self.environment.get("SCDC_SAMPLE", "0") == "1"
        env_file = Path(self.environment.get("SCDC_ENV_FILE", ".env")).expanduser()
        self.command = shlex.split(self.environment.get("COMPOSE", "docker compose"))
        if not self.command:
            raise ValueError("COMPOSE must name a Compose command")
        self.command += ["--project-directory", str(ROOT), "--env-file", str(env_file),
                         "-f", str(ROOT / "compose.yaml")]
        if self.development:
            self.command += ["-f", str(ROOT / "compose.dev.yaml")]
        if self.sample:
            self.command += ["-f", str(ROOT / "ops/compose/compose.sample.yaml")]
        self.overrides = shlex.split(self.environment.get("COMPOSE_ARGS", "")) + list(arguments)
        if self.rollback:
            self.overrides += ["-f", str(ROOT / "ops/compose/compose.community-rollback.yaml")]

    def run(self, *arguments, tools=False, quiet=False):
        command = list(self.command)
        if tools:
            command += ["-f", str(ROOT / "ops/compose/compose.prepare.yaml")]
        command += self.overrides + list(arguments)
        if not quiet:
            print("+ " + shlex.join(command), flush=True)
        return subprocess.run(command, cwd=ROOT, env=self.environment, check=True,
                              stdout=subprocess.DEVNULL if quiet else None,
                              stderr=subprocess.DEVNULL if quiet else None)

    def tool(self, name, *arguments):
        self.run("run", "--rm", "--no-deps", "-T", name, *arguments, tools=True)

    def validate(self):
        self.run("config", "--quiet")
        self.run("config", "--quiet", tools=True)

    def start_infrastructure(self):
        self.run("up", "-d", "--no-build", *INFRASTRUCTURE)
        self.wait_infrastructure()

    def wait_infrastructure(self):
        probes = (
            ("postgres", "sh", "-ec", 'pg_isready -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'),
            ("identity-postgres", "sh", "-ec", 'pg_isready -h 127.0.0.1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'),
            ("auth-redis", "sh", "-ec", "REDISCLI_AUTH=$(cat /run/secrets/redis_reader_password) "
             "redis-cli --user scdc_reader ping | grep -q PONG"),
        )
        timeout = float(self.environment.get("SCDC_STARTUP_TIMEOUT", "120"))
        if timeout <= 0:
            raise ValueError("SCDC_STARTUP_TIMEOUT must be positive")
        deadline = time.monotonic() + timeout
        for probe in probes:
            print("Waiting for " + probe[0], flush=True)
            while True:
                try:
                    self.run("exec", "-T", *probe, quiet=True)
                    break
                except subprocess.CalledProcessError:
                    if time.monotonic() >= deadline:
                        raise RuntimeError("Infrastructure did not become ready: " + probe[0])
                    time.sleep(1)

    def gates(self):
        self.tool("identity-cutover-gate")
        self.tool("community-cutover-gate", *(["--rollback"] if self.rollback else []))

    def preflight(self):
        self.gates()
        self.tool("identity-email-migration", "--check")

    def prepare(self):
        self.tool("identity-keygen")
        self.tool("messaging-keygen")
        self.tool("messaging-cursor-keygen")
        self.tool("community-database-init")

    def execute(self, action):
        if action in ("setup", "migrate") and self.rollback:
            raise ValueError("Rollback uses the verified old image: run up with ROLLBACK=1; do not prepare or migrate it")
        if action in ("setup", "up", "migrate", "preflight"):
            self.validate()
            if action == "setup":
                if not self.development:
                    raise ValueError("setup builds local images; use migrate and up with DEV=0 for prebuilt images")
                self.run("build")
            if action != "preflight":
                # Offline updates: leave every writer stopped if any gate or migration fails.
                self.run("stop", *WRITERS)
            if action == "preflight":
                self.wait_infrastructure()
            else:
                self.start_infrastructure()
            if action == "setup":
                self.prepare()
            if action in ("setup", "migrate"):
                self.gates()
                self.tool("identity-email-migration")
            self.preflight()
            if action in ("setup", "up"):
                self.run("up", "-d", "--no-build", "--remove-orphans")
        elif action == "build":
            if not self.development:
                raise ValueError("build requires DEV=1")
            self.run("build")
        elif action == "config":
            self.validate()
        elif action == "status":
            self.run("ps", "-a")
        elif action == "logs":
            self.run("logs", "--tail=100", *shlex.split(self.environment.get("SERVICE", "")))
        elif action == "down":
            self.run("down", "--remove-orphans")
        elif action == "check":
            web = self.environment.get("SCDC_WEB_ORIGIN", "http://localhost:3000").rstrip("/")
            observe = self.environment.get("SCDC_OBSERVE_ORIGIN", "http://localhost:5080").rstrip("/")
            for url in [web + path for path in ("/", "/healthz", "/health", "/api/v1/health",
                        "/api/v1/health/access", "/api/v1/health/community/access",
                        "/api/v1/health/identity/access")] + [observe + "/healthz"]:
                print("Checking " + url, flush=True)
                subprocess.run(["curl", "--fail", "--silent", "--show-error", "--output", "/dev/null",
                                "--connect-timeout", "2", "--max-time", "5", "--retry", "15",
                                "--retry-delay", "2", "--retry-connrefused", url], check=True)
            self.run("exec", "-T", "postgres", "sh", "-ec", 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"')
            self.run("exec", "-T", "identity-postgres", "sh", "-ec", 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"')
            self.run("exec", "-T", "web-client", "nginx", "-t")
        else:
            raise ValueError("Unknown action: " + action)


def main(arguments=None):
    arguments = sys.argv[1:] if arguments is None else arguments
    if not arguments:
        print("Usage: compose.py setup|up|preflight|migrate|build|config|check|status|logs|down [Compose options]", file=sys.stderr)
        return 2
    try:
        Stack(arguments[1:]).execute(arguments[0])
    except subprocess.CalledProcessError as error:
        print("Compose operation stopped (exit " + str(error.returncode) + ").", file=sys.stderr)
        return error.returncode if error.returncode > 0 else 1
    except (ValueError, RuntimeError, OSError) as error:
        print(str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

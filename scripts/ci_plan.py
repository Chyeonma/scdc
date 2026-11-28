#!/usr/bin/env python3
"""Select CI jobs from a complete Git diff and validate the required CI gate."""

import argparse
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess


JOBS = {"backend": "backend", "frontend": "frontend", "transfer": "identity-transfer"}
LIGHTWEIGHT = {
    "scripts/check_docs.py",
    "scripts/sync_trello.py",
    "scripts/tests/test_sync_trello.py",
}


def full_plan():
    return dict.fromkeys(JOBS, True)


def classify(paths):
    """Unknown paths require all jobs; only known, isolated areas can skip jobs."""
    if not paths:
        return full_plan()
    result = dict.fromkeys(JOBS, False)
    for path in paths:
        parts = PurePosixPath(path).parts
        if not parts or path.startswith("/") or ".." in parts or "\\" in path:
            return full_plan()
        if path.endswith(".md") or path in {"LICENSE", "LICENSE.txt"} or path in LIGHTWEIGHT:
            continue
        if (path.startswith(("docs/", ".github/", "ops/", "services/Shared/",
                             "services/SCDC.Contracts/", "services/SCDC.BuildingBlocks/"))
                or any(part.endswith(".Grpc") for part in parts)
                or path.endswith((".proto", "/Dockerfile"))):
            return full_plan()
        if path.startswith("clients/WebClient/"):
            if path == "clients/WebClient/nginx.conf":
                return full_plan()
            result["frontend"] = True
        elif path.startswith(("database/", "tools/")):
            result["backend"] = result["transfer"] = True
        elif path.startswith(("services/", "tests/SCDC.Api.Tests/")):
            result["backend"] = True
        else:
            return full_plan()
    return result


def commit(value):
    if not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{40}|[0-9a-f]{64}", value) or set(value) == {"0"}:
        raise ValueError("Missing or invalid commit")
    return value


def event_plan(event_name, payload, root):
    if event_name == "workflow_dispatch":
        return full_plan(), "Manual run: all checks required"
    try:
        if event_name == "pull_request":
            base = commit(payload["pull_request"]["base"]["sha"])
            head = commit(payload["pull_request"]["head"]["sha"])
            revisions = [f"{base}...{head}"]
        elif event_name == "push":
            revisions = [commit(payload["before"]), commit(payload["after"])]
        else:
            return full_plan(), "Unrecognized event: all checks required"
        raw = subprocess.check_output(
            ["git", "diff", "--name-only", "--no-renames", "-z", *revisions, "--"],
            cwd=root, stderr=subprocess.PIPE,
        )
        paths = [os.fsdecode(path) for path in raw.split(b"\0") if path]
        return classify(paths), f"Complete Git diff: {len(paths)} changed paths"
    except (KeyError, TypeError, ValueError, OSError, subprocess.CalledProcessError):
        return full_plan(), "Diff unavailable or invalid: all checks required"


def gate(needs):
    errors = []
    plan = needs.get("plan", {})
    if plan.get("result") != "success":
        errors.append("Change detection or lightweight checks did not succeed")
    outputs = plan.get("outputs", {})
    for flag, job in JOBS.items():
        required = outputs.get(flag)
        actual = needs.get(job, {}).get("result")
        if required not in {"true", "false"}:
            errors.append(f"Invalid or missing plan output: {flag}")
        elif required == "true" and actual != "success":
            errors.append(f"Required job did not succeed: {job}")
        elif required == "false" and actual not in {"success", "skipped"}:
            errors.append(f"Unexpected job result: {job}")
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["plan", "gate"])
    args = parser.parse_args()
    if args.command == "gate":
        try:
            needs = json.loads(os.environ["CI_NEEDS"])
            errors = gate(needs)
        except (KeyError, TypeError, ValueError, AttributeError):
            errors = ["Missing or invalid CI results"]
        for error in errors:
            print(error)
        if not errors:
            print("All required checks passed; unrelated jobs were intentionally skipped.")
        return bool(errors)

    try:
        payload = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
        selection, reason = event_plan(os.environ.get("GITHUB_EVENT_NAME"), payload, Path.cwd())
    except (KeyError, OSError, ValueError):
        selection, reason = full_plan(), "Event payload unavailable: all checks required"
    print(reason)
    print(json.dumps(selection, sort_keys=True))
    if output := os.environ.get("GITHUB_OUTPUT"):
        with Path(output).open("a") as stream:
            for flag, required in selection.items():
                stream.write(f"{flag}={str(required).lower()}\n")
    if summary := os.environ.get("GITHUB_STEP_SUMMARY"):
        with Path(summary).open("a") as stream:
            stream.write("### CI plan\n\nMarkdown links and Python checks always run.\n\n| Job | Required |\n|---|---|\n")
            for flag, required in selection.items():
                stream.write(f"| {JOBS[flag]} | {'yes' if required else 'no'} |\n")
            stream.write(f"\n{reason}\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

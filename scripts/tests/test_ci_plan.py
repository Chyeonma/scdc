"""Exercise job selection with real Git history and reject incomplete CI gates."""

import importlib.util
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / "ci_plan.py"
SPEC = importlib.util.spec_from_file_location("ci_plan", SCRIPT)
ci = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(ci)


def selection(*required):
    return {name: name in required for name in ci.JOBS}


def outcomes(*required):
    return {
        "plan": {"result": "success", "outputs": {key: str(value).lower() for key, value in selection(*required).items()}},
        **{job: {"result": "success" if flag in required else "skipped"} for flag, job in ci.JOBS.items()},
    }


class SelectionTests(unittest.TestCase):
    def test_markdown_only_does_not_start_application_jobs(self):
        self.assertEqual(selection(), ci.classify(["README.md", "docs/project/tasks/issues.md", "services/Community/README.md"]))

    def test_frontend_and_backend_are_independent(self):
        self.assertEqual(selection("frontend"), ci.classify(["clients/WebClient/src/App.jsx", "clients/WebClient/package-lock.json"]))
        self.assertEqual(selection("backend"), ci.classify(["services/Identity/SCDC.Identity.Api/Program.cs", "tests/SCDC.Api.Tests/Identity/Tests.cs"]))
        self.assertEqual(selection("frontend", "backend"), ci.classify(["clients/WebClient/src/App.jsx", "services/SCDC.Api/Program.cs"]))

    def test_database_and_migrator_require_both_backend_and_transfer(self):
        for path in ["database/postgres/schema.sql", "database/postgres/community/cutover-gate.sh", "tools/SCDC.DbMigrator/Program.cs"]:
            with self.subTest(path=path):
                self.assertEqual(selection("backend", "transfer"), ci.classify([path]))

    def test_shared_contracts_fixtures_and_build_configuration_require_all_jobs(self):
        for path in ["docs/fixtures/text-policy.json", "docs/contracts/grpc/identity/access.proto",
                     "docs/contracts/direct-messaging.openapi.json", "services/SCDC.Contracts/Identity/Types.cs",
                     "services/Shared/SCDC.Shared.Core/Policy.cs", "services/SCDC.BuildingBlocks/Transport.cs",
                     "services/Identity/SCDC.Identity.Grpc/SCDC.Identity.Grpc.csproj", "global.json", "SCDC.slnx",
                     "Directory.Build.props", ".github/workflows/ci.yml", "scripts/ci_plan.py",
                     "compose.yaml", "clients/WebClient/nginx.conf", "clients/WebClient/Dockerfile"]:
            with self.subTest(path=path):
                self.assertEqual(ci.full_plan(), ci.classify([path]))

    def test_unknown_empty_and_unsafe_paths_require_all_jobs(self):
        for paths in [[], ["unknown.file"], ["../docs/readme.md"], ["/docs/readme.md"], ["docs\\readme.md"]]:
            with self.subTest(paths=paths):
                self.assertEqual(ci.full_plan(), ci.classify(paths))

    def test_trello_and_docs_scripts_are_checked_in_lightweight_job(self):
        self.assertEqual(selection(), ci.classify(sorted(ci.LIGHTWEIGHT)))

    def test_manual_or_unknown_events_always_require_all_jobs(self):
        for event in ["workflow_dispatch", "merge_group", "unknown"]:
            self.assertEqual(ci.full_plan(), ci.event_plan(event, {}, Path.cwd())[0])

    def test_missing_commits_or_diff_errors_do_not_skip_tests(self):
        for payload in [{}, {"before": "0" * 40, "after": "a" * 40}, {"before": "--bad", "after": "a" * 40}]:
            self.assertEqual(ci.full_plan(), ci.event_plan("push", payload, Path.cwd())[0])
        with patch.object(ci.subprocess, "check_output", side_effect=OSError("missing Git")):
            self.assertEqual(ci.full_plan(), ci.event_plan("push", {"before": "a" * 40, "after": "b" * 40}, Path.cwd())[0])

    def test_real_pr_diff_includes_every_commit_and_both_sides_of_rename(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            def git(*args):
                return subprocess.check_output(["git", *args], cwd=root, stderr=subprocess.DEVNULL, text=True).strip()
            def save(path, content):
                target = root / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(content)
                git("add", "--all")
                git("commit", "-qm", "Test revision")
                return git("rev-parse", "HEAD")
            git("init", "-q")
            git("config", "user.email", "ci@example.test")
            git("config", "user.name", "CI fixture")
            base = save("README.md", "Base\n")
            head = save("docs/new.md", "Documentation\n")
            payload = {"pull_request": {"base": {"sha": base}, "head": {"sha": head}}}
            self.assertEqual(selection(), ci.event_plan("pull_request", payload, root)[0])
            head = save("services/Owner/Code.cs", "class Example {}\n")
            save("README.md", "More documentation\n")
            payload["pull_request"]["head"]["sha"] = git("rev-parse", "HEAD")
            self.assertEqual(selection("backend"), ci.event_plan("pull_request", payload, root)[0])
            (root / "docs/moved.md").write_text((root / "services/Owner/Code.cs").read_text())
            (root / "services/Owner/Code.cs").unlink()
            git("add", "--all")
            git("commit", "-qm", "Rename across areas")
            self.assertEqual(selection("backend"), ci.event_plan("push", {"before": head, "after": git("rev-parse", "HEAD")}, root)[0])

    def test_large_changeset_does_not_truncate_away_backend_files(self):
        paths = [f"docs/page-{i:04}.md" for i in range(400)] + ["services/Owner/Code.cs"]
        with patch.object(ci.subprocess, "check_output", return_value=("\0".join(paths) + "\0").encode()):
            self.assertEqual(selection("backend"), ci.event_plan("push", {"before": "a" * 40, "after": "b" * 40}, Path.cwd())[0])


class GateTests(unittest.TestCase):
    def test_expected_success_and_intentional_skips_pass(self):
        for required in [(), ("frontend",), ("backend",), tuple(ci.JOBS)]:
            self.assertEqual([], ci.gate(outcomes(*required)))

    def test_failed_cancelled_or_skipped_required_job_blocks_merge(self):
        for job in ci.JOBS.values():
            for result in ["failure", "cancelled", "skipped", None]:
                needs = outcomes(*ci.JOBS)
                needs[job]["result"] = result
                self.assertTrue(ci.gate(needs), (job, result))

    def test_lightweight_failure_or_skip_blocks_merge(self):
        for result in ["failure", "cancelled", "skipped", None]:
            needs = outcomes()
            needs["plan"]["result"] = result
            self.assertTrue(ci.gate(needs))

    def test_invalid_or_missing_plan_never_accepts_skips(self):
        for value in [None, "", "yes", True]:
            needs = outcomes()
            needs["plan"]["outputs"]["backend"] = value
            self.assertTrue(ci.gate(needs))
        self.assertTrue(ci.gate({}))

    def test_unplanned_failure_is_not_hidden(self):
        needs = outcomes()
        needs["backend"]["result"] = "failure"
        self.assertTrue(ci.gate(needs))

    def test_cli_exits_nonzero_when_required_backend_failed(self):
        needs = outcomes("backend")
        needs["backend"]["result"] = "failure"
        result = subprocess.run([os.sys.executable, str(SCRIPT), "gate"],
                                env={**os.environ, "CI_NEEDS": json.dumps(needs)}, capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)


if __name__ == "__main__":
    unittest.main()

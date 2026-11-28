"""The supported startup commands must fail closed before enabling any writer."""

import importlib.util
from pathlib import Path
import subprocess
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("compose_operations", ROOT / "ops/compose.py")
operations = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(operations)


class RecordingStack(operations.Stack):
    def __init__(self, fail=None, **environment):
        super().__init__(environment=environment)
        self.calls = []
        self.fail = fail

    def run(self, *arguments, tools=False, quiet=False):
        self.calls.append((arguments, tools))
        if arguments == self.fail:
            raise subprocess.CalledProcessError(17, arguments)


def tool(name, *arguments):
    return ("run", "--rm", "--no-deps", "-T", name, *arguments)


class ComposeOperationsTests(unittest.TestCase):
    def setUp(self):
        printing = patch("builtins.print")
        printing.start()
        self.addCleanup(printing.stop)

    def test_setup_prepares_and_migrates_before_readiness_and_runtime(self):
        stack = RecordingStack()
        stack.execute("setup")
        calls = [args for args, _ in stack.calls]
        positions = [calls.index(args) for args in [
            ("build",), ("stop", *operations.WRITERS),
            tool("identity-keygen"), tool("messaging-keygen"), tool("messaging-cursor-keygen"),
            tool("community-database-init"), tool("identity-cutover-gate"),
            tool("community-cutover-gate"), tool("identity-email-migration"),
            tool("identity-email-migration", "--check"),
            ("up", "-d", "--no-build", "--remove-orphans"),
        ]]
        self.assertEqual(positions, sorted(positions))
        self.assertTrue(all(tools for args, tools in stack.calls if args[0] == "run"))

    def test_up_checks_existing_database_without_preparing_or_migrating(self):
        stack = RecordingStack()
        stack.execute("up")
        calls = [args for args, _ in stack.calls]
        self.assertIn(tool("identity-email-migration", "--check"), calls)
        self.assertNotIn(tool("identity-email-migration"), calls)
        self.assertNotIn(tool("community-database-init"), calls)
        self.assertNotIn(tool("identity-keygen"), calls)
        self.assertLess(calls.index(("stop", *operations.WRITERS)), calls.index(tool("identity-cutover-gate")))
        self.assertEqual(calls[-1], ("up", "-d", "--no-build", "--remove-orphans"))

    def test_failed_gate_or_schema_check_leaves_all_writers_stopped(self):
        for failure in [tool("identity-cutover-gate"), tool("community-cutover-gate"),
                        tool("identity-email-migration", "--check")]:
            with self.subTest(failure=failure):
                stack = RecordingStack(fail=failure)
                with self.assertRaises(subprocess.CalledProcessError):
                    stack.execute("up")
                calls = [args for args, _ in stack.calls]
                self.assertIn(("stop", *operations.WRITERS), calls)
                self.assertNotIn(("up", "-d", "--no-build", "--remove-orphans"), calls)

    def test_failed_migration_never_starts_writers(self):
        stack = RecordingStack(fail=tool("identity-email-migration"))
        with self.assertRaises(subprocess.CalledProcessError):
            stack.execute("setup")
        self.assertNotIn((("up", "-d", "--no-build", "--remove-orphans"), False), stack.calls)

    def test_migrate_finishes_with_writers_stopped(self):
        stack = RecordingStack()
        stack.execute("migrate")
        self.assertIn((("stop", *operations.WRITERS), False), stack.calls)
        self.assertIn((tool("identity-email-migration"), True), stack.calls)
        self.assertNotIn((("up", "-d", "--no-build", "--remove-orphans"), False), stack.calls)

    def test_standalone_preflight_is_read_only_and_does_not_stop_apps(self):
        stack = RecordingStack()
        stack.execute("preflight")
        self.assertFalse(any(args[0] in ("stop", "build", "up") for args, _ in stack.calls))
        self.assertEqual([args for args, tools in stack.calls if tools and args[0] == "run"], [
            tool("identity-cutover-gate"), tool("community-cutover-gate"),
            tool("identity-email-migration", "--check"),
        ])

    def test_rollback_uses_rollback_gate_and_never_prepares_or_migrates(self):
        stack = RecordingStack(SCDC_ROLLBACK="1")
        stack.execute("up")
        self.assertIn((tool("community-cutover-gate", "--rollback"), True), stack.calls)
        self.assertNotIn((tool("community-cutover-gate"), True), stack.calls)
        self.assertNotIn((tool("community-database-init"), True), stack.calls)
        self.assertTrue(stack.overrides[-1].endswith("compose.community-rollback.yaml"))
        for action in ("setup", "migrate"):
            stack = RecordingStack(SCDC_ROLLBACK="1")
            with self.assertRaises(ValueError):
                stack.execute(action)
            self.assertEqual(stack.calls, [])

    def test_prebuilt_images_are_supported_without_development_file(self):
        stack = RecordingStack(SCDC_DEV="0")
        stack.execute("up")
        self.assertFalse(any(part.endswith("compose.dev.yaml") for part in stack.command))
        self.assertNotIn((("build",), False), stack.calls)

    def test_tool_and_runtime_commands_keep_project_env_file_and_override_order(self):
        stack = operations.Stack(["-p", "isolated-test"], environment={
            "COMPOSE": "podman compose", "SCDC_ENV_FILE": "/tmp/config with spaces.env",
            "COMPOSE_ARGS": "-f '/tmp/overlay with spaces.yaml'", "SCDC_SAMPLE": "1",
        })
        with patch.object(operations.subprocess, "run") as run:
            stack.tool("identity-cutover-gate")
            command = run.call_args.args[0]
            self.assertEqual(command[:2], ["podman", "compose"])
            self.assertIn("/tmp/config with spaces.env", command)
            self.assertIn("/tmp/overlay with spaces.yaml", command)
            self.assertLess(command.index(str(ROOT / "ops/compose/compose.prepare.yaml")),
                            command.index("/tmp/overlay with spaces.yaml"))
            self.assertIn("isolated-test", command)
            self.assertIn(str(ROOT / "ops/compose/compose.sample.yaml"), command)

    def test_down_never_deletes_volumes(self):
        stack = RecordingStack()
        stack.execute("down")
        self.assertEqual(stack.calls, [(("down", "--remove-orphans"), False)])

    def test_main_returns_nonzero_for_a_failed_operation(self):
        with patch.object(operations.Stack, "execute", side_effect=subprocess.CalledProcessError(17, "gate")):
            self.assertEqual(operations.main(["up"]), 17)


if __name__ == "__main__":
    unittest.main()

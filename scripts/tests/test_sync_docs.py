"""Exercise Git mutations in disposable repositories, never in the real workspace."""

import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


WORKSPACE = Path(__file__).resolve().parents[2]
SCRIPT = WORKSPACE / "scripts/sync_docs.py"


class SyncDocsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="scdc-sync-tests-")
        self.addCleanup(self.temporary.cleanup)
        self.pool = Path(self.temporary.name)
        self.repo = self.pool / "repo with spaces"
        self.repo.mkdir()
        self.env = dict(os.environ)
        for name in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR",
                     "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"):
            self.env.pop(name, None)
        self.env.update({"GIT_CONFIG_NOSYSTEM": "1", "GIT_CONFIG_GLOBAL": os.devnull})
        self.git("init", "--quiet", "-b", "main")
        self.git("config", "user.name", "SCDC tool tests")
        self.git("config", "user.email", "tests@scdc.invalid")
        self.git("config", "commit.gpgsign", "false")
        self.write(".gitignore", "docs/*.tmp\n")
        self.write("docs/README.md", "old docs\n")
        self.write("docs/obsolete.md", "target-only document\n")
        self.write("docs/archive/history.md", "old archive\n")
        self.write("src/app.txt", "base application\n")
        shutil.copyfile(WORKSPACE / "Makefile", self.repo / "Makefile")
        (self.repo / "scripts").mkdir()
        shutil.copyfile(SCRIPT, self.repo / "scripts/sync_docs.py")
        self.git("add", ".")
        self.git("commit", "--quiet", "-m", "base")

        self.git("switch", "--quiet", "-c", "feat/work")
        self.write("src/app.txt", "target application\n")
        self.git("add", "src/app.txt")
        self.git("commit", "--quiet", "-m", "target code")
        self.target_commit = self.git("rev-parse", "HEAD").stdout.strip()

        self.git("switch", "--quiet", "main")
        self.write("docs/README.md", "new docs\n")
        self.write("docs/new file.md", "Tài liệu mới\n")
        self.write("docs/archive/history.md", "updated archive\n")
        self.git("rm", "--quiet", "docs/obsolete.md")
        self.git("add", "docs/")
        self.git("commit", "--quiet", "-m", "source docs")
        self.source_commit = self.git("rev-parse", "HEAD").stdout.strip()
        self.git("switch", "--quiet", "feat/work")

    def run_command(self, args, *, cwd=None, check=True):
        result = subprocess.run(args, cwd=cwd or self.repo, env=self.env,
                                text=True, capture_output=True)
        if check:
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result

    def git(self, *args, check=True):
        return self.run_command(["git", *args], check=check)

    def tool(self, *args, cwd=None, check=True):
        return self.run_command([sys.executable, str(SCRIPT), *args], cwd=cwd, check=check)

    def write(self, name, text):
        path = self.repo / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def branch(self):
        return self.git("branch", "--show-current").stdout.strip()

    def state(self):
        return (self.branch(), self.git("rev-parse", "HEAD").stdout,
                self.git("diff", "--binary").stdout,
                self.git("diff", "--cached", "--binary").stdout,
                self.git("status", "--porcelain", "--untracked-files=all",
                         "--ignored").stdout)

    def assert_rejected_without_changes(self, *args):
        before = self.state()
        result = self.tool(*args, check=False)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Lỗi:", result.stderr)
        self.assertEqual(self.state(), before)
        return result

    def assert_docs_synced(self):
        self.assertEqual(self.git("diff", "--cached", "main", "--", "docs/").stdout, "")
        self.assertEqual(self.git("diff", "--", "docs/").stdout, "")
        self.assertEqual((self.repo / "docs/new file.md").read_text(), "Tài liệu mới\n")
        self.assertFalse((self.repo / "docs/obsolete.md").exists())
        self.assertEqual((self.repo / "docs/archive/history.md").read_text(), "updated archive\n")

    def test_sync_copies_added_modified_deleted_docs_and_preserves_staged_and_unstaged_code(self):
        self.write("src/app.txt", "staged target application\n")
        self.git("add", "src/app.txt")
        self.write("src/app.txt", "unstaged target application\n")
        self.tool("--source", "main")
        self.assert_docs_synced()
        self.assertEqual(self.branch(), "feat/work")
        self.assertEqual(self.git("rev-parse", "HEAD").stdout.strip(), self.target_commit)
        self.assertEqual(self.git("rev-parse", "main").stdout.strip(), self.source_commit)
        self.assertEqual(self.git("show", ":src/app.txt").stdout, "staged target application\n")
        self.assertEqual((self.repo / "src/app.txt").read_text(), "unstaged target application\n")

    def test_named_target_switches_and_stages_without_commit_or_source_code_copy(self):
        self.git("switch", "--quiet", "main")
        self.tool("--source", "main", "--target", "feat/work")
        self.assert_docs_synced()
        self.assertEqual(self.branch(), "feat/work")
        self.assertEqual(self.git("rev-parse", "HEAD").stdout.strip(), self.target_commit)
        self.assertEqual((self.repo / "src/app.txt").read_text(), "target application\n")

    def test_preview_keeps_branch_worktree_and_index_even_with_dirty_docs(self):
        self.git("switch", "--quiet", "main")
        self.write("docs/README.md", "uncommitted source docs\n")
        before = self.state()
        result = self.tool("--source", "main", "--target", "feat/work", "--dry-run")
        self.assertIn("docs/new file.md", result.stdout)
        self.assertIn("D\tdocs/obsolete.md", result.stdout)
        self.assertEqual(self.state(), before)

    def test_modified_docs_are_preserved(self):
        self.write("docs/README.md", "my unsaved docs\n")
        self.assert_rejected_without_changes("--source", "main")
        self.assertEqual((self.repo / "docs/README.md").read_text(), "my unsaved docs\n")

    def test_staged_docs_are_preserved(self):
        self.write("docs/README.md", "my staged docs\n")
        self.git("add", "docs/README.md")
        self.assert_rejected_without_changes("--source", "main")

    def test_untracked_docs_are_preserved(self):
        self.write("docs/new file.md", "untracked document\n")
        self.assert_rejected_without_changes("--source", "main")
        self.assertEqual((self.repo / "docs/new file.md").read_text(), "untracked document\n")

    def test_ignored_docs_are_preserved(self):
        self.write("docs/private.tmp", "local notes\n")
        self.assert_rejected_without_changes("--source", "main")
        self.assertEqual((self.repo / "docs/private.tmp").read_text(), "local notes\n")

    def test_switching_branches_rejects_dirty_application_code(self):
        self.git("switch", "--quiet", "main")
        self.write("src/app.txt", "work in progress\n")
        self.assert_rejected_without_changes("--source", "main", "--target", "feat/work")

    def test_switching_branches_rejects_untracked_files(self):
        self.git("switch", "--quiet", "main")
        self.write("new-code.txt", "work in progress\n")
        self.assert_rejected_without_changes("--source", "main", "--target", "feat/work")

    def test_switching_to_target_with_ignored_docs_returns_to_original_branch(self):
        self.git("switch", "--quiet", "main")
        self.write("docs/private.tmp", "local notes\n")
        self.assert_rejected_without_changes("--source", "main", "--target", "feat/work")
        self.assertEqual((self.repo / "docs/private.tmp").read_text(), "local notes\n")

    def test_missing_source_does_not_change_destination(self):
        self.assert_rejected_without_changes("--source", "missing-branch")

    def test_missing_target_does_not_switch_or_create_branch(self):
        self.assert_rejected_without_changes("--source", "main", "--target", "missing-target")

    def test_source_without_docs_cannot_erase_destination_docs(self):
        self.git("switch", "--quiet", "-c", "no-docs")
        self.git("rm", "--quiet", "-r", "docs")
        self.git("commit", "--quiet", "-m", "remove docs")
        self.git("switch", "--quiet", "feat/work")
        self.assert_rejected_without_changes("--source", "no-docs")
        self.assertTrue((self.repo / "docs/README.md").exists())

    def test_detached_head_is_preserved(self):
        self.git("switch", "--quiet", "--detach")
        self.assert_rejected_without_changes("--source", "main")

    def test_ongoing_merge_is_preserved(self):
        (self.repo / ".git/MERGE_HEAD").write_text(self.source_commit + "\n")
        result = self.assert_rejected_without_changes("--source", "main")
        self.assertIn("merge/rebase", result.stderr)

    def test_target_checked_out_in_other_worktree_is_not_modified(self):
        self.git("switch", "--quiet", "main")
        other = self.pool / "other worktree"
        self.git("worktree", "add", "--quiet", str(other), "feat/work")
        before = (other / "docs/README.md").read_text()
        self.assert_rejected_without_changes("--source", "main", "--target", "feat/work")
        self.assertEqual((other / "docs/README.md").read_text(), before)

    def test_matching_docs_do_not_create_changes(self):
        self.tool("--source", "main")
        self.git("commit", "--quiet", "-m", "sync docs")
        before = self.state()
        self.tool("--source", "main")
        self.assertEqual(self.state(), before)

    def test_script_runs_from_repo_subdirectory(self):
        self.tool("--source", "main", cwd=self.repo / "src")
        self.assert_docs_synced()

    @unittest.skipUnless(shutil.which("make"), "make is not installed")
    def test_make_passes_from_to_and_preview_then_sync(self):
        self.git("switch", "--quiet", "main")
        before = self.state()
        self.run_command(["make", "docs-sync-preview", "FROM=main", "TO=feat/work"])
        self.assertEqual(self.state(), before)
        self.run_command(["make", "docs-sync", "FROM=main", "TO=feat/work"])
        self.assert_docs_synced()
        self.assertEqual(self.branch(), "feat/work")

    @unittest.skipUnless(shutil.which("make"), "make is not installed")
    def test_make_source_value_is_not_interpreted_as_shell_code(self):
        before = self.state()
        result = self.run_command(
            ["make", "docs-sync", "FROM=main; touch command-ran"], check=False,
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.repo / "command-ran").exists())
        self.assertEqual(self.state(), before)


if __name__ == "__main__":
    unittest.main()

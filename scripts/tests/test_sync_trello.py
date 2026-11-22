"""Exercise Trello changes against an in-memory board; never call the real API."""

import contextlib
import copy
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / "sync_trello.py"
SPEC = importlib.util.spec_from_file_location("sync_trello", SCRIPT)
sync = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(sync)


def source(code, complete=False):
    return {"code": code, "title": code, "name": f"[{code}] {code}",
            "desc": f"Source {code}", "labels": [], "step": "STEP-1",
            "complete": complete}


def remote(code, column="todo", **extra):
    return {"id": code, "name": f"[{code}] {code}", "desc": f"Source {code}",
            "idLabels": [], "closed": False, "idList": column,
            "idMembers": ["member-kept"], "due": "2026-12-01", **extra}


class Board:
    def __init__(self, cards, lists=None):
        self.cards = copy.deepcopy(cards)
        self.lists = lists if lists is not None else [{"id": "done", "name": "Hoàn tất"}]
        self.calls = []
        self.ignore_moves = False

    def call(self, method, path, **params):
        self.calls.append((method, path, params))
        if method == "GET":
            if path == "/boards/board":
                return {"id": "board", "name": "Test", "url": "https://trello.com/b/test"}
            if path == "/lists/todo":
                return {"idBoard": "board", "name": "Cần làm"}
            if path.endswith("/cards/all"):
                return copy.deepcopy(self.cards)
            if path.endswith("/lists"):
                return self.lists
            if path.endswith("/labels"):
                return []
        if method == "POST" and path == "/cards":
            card = {"id": "created-" + str(len(self.cards)), "idLabels": [], "closed": False, **params}
            self.cards.append(card)
            return card
        if method == "PUT" and path.startswith("/cards/"):
            card = next(card for card in self.cards if path == "/cards/" + card["id"])
            params = dict(params)
            if self.ignore_moves:
                params.pop("idList", None)
            card.update(params)
            return card
        raise AssertionError((method, path, params))

    def writes(self):
        return [call for call in self.calls if call[0] != "GET"]


class TrelloSyncTests(unittest.TestCase):
    def setUp(self):
        self.sources = [source("S1-07"), source("S1-09", True), source("S2-03", True), source("S2-04")]
        self.board = Board([remote("S1-07", "doing", desc="Keep Media", idLabels=["custom"]),
                            remote("S1-09"), remote("S2-04", "doing")])

    def run_sync(self, *arguments):
        env = {"TRELLO_KEY": "fake-key", "TRELLO_TOKEN": "fake-token",
               "TRELLO_BOARD_ID": "board", "TRELLO_LIST_ID": "todo"}
        with patch.dict(os.environ, env, clear=True), \
             patch("sys.argv", [str(SCRIPT), *arguments, "docs/project/tasks/step-1.md"]), \
             patch.object(sync, "parse", return_value=copy.deepcopy(self.sources)), \
             patch.object(sync, "is_draft", return_value=False), \
             patch.object(sync, "Trello", return_value=self.board), \
             contextlib.redirect_stdout(io.StringIO()) as output:
            sync.main()
        return output.getvalue()

    def test_completed_cards_move_and_pending_media_members_due_stay_unchanged(self):
        media = copy.deepcopy(self.board.cards[0])
        self.run_sync("--complete", "S1-09,S2-03", "--exclude", "S1-07")
        indexed = sync.index_cards(self.board.cards, set())
        self.assertEqual(media, indexed["S1-07"])
        self.assertEqual("done", indexed["S1-09"]["idList"])
        self.assertEqual("done", indexed["S2-03"]["idList"])
        self.assertEqual("doing", indexed["S2-04"]["idList"])
        self.assertEqual(["member-kept"], indexed["S1-09"]["idMembers"])
        self.assertEqual("2026-12-01", indexed["S1-09"]["due"])
        self.assertFalse(any(path == "/cards/S1-07" for _, path, _ in self.board.writes()))

    def test_preview_reads_board_but_never_writes_or_creates_cards(self):
        self.run_sync("--preview", "--complete", "S1-09,S2-03", "--exclude", "S1-07")
        self.assertFalse(self.board.writes())
        self.assertEqual(3, len(self.board.cards))

    def test_default_sync_does_not_move_existing_cards(self):
        self.run_sync("--exclude", "S1-07")
        indexed = sync.index_cards(self.board.cards, set())
        self.assertEqual("todo", indexed["S1-09"]["idList"])
        self.assertEqual("doing", indexed["S2-04"]["idList"])
        self.assertFalse(any("idList" in params for method, _, params in self.board.writes() if method == "PUT"))

    def test_repeat_is_idempotent(self):
        self.run_sync("--complete", "S1-09,S2-03", "--exclude", "S1-07")
        self.board.calls.clear()
        self.run_sync("--complete", "S1-09,S2-03", "--exclude", "S1-07")
        self.assertFalse(self.board.writes())

    def test_unknown_unfinished_and_excluded_completion_fail_before_network(self):
        for code in ("S2-99", "S2-04", "S1-07"):
            with self.subTest(code=code), self.assertRaises(SystemExit):
                self.run_sync("--complete", code, "--exclude", "S1-07")
            self.assertFalse(self.board.calls)

    def test_missing_or_ambiguous_done_list_fails_before_writes(self):
        for lists in ([], [{"id": "a", "name": "Hoàn tất"}, {"id": "b", "name": "Hoàn tất"}]):
            self.board.lists = lists
            with self.assertRaises(SystemExit):
                self.run_sync("--complete", "S1-09", "--exclude", "S1-07")
            self.assertFalse(self.board.writes())

    def test_duplicate_target_cards_fail_before_writes(self):
        self.board.cards.append(remote("S1-09", id="duplicate", closed=True))
        with self.assertRaises(SystemExit):
            self.run_sync("--complete", "S1-09", "--exclude", "S1-07")
        self.assertFalse(self.board.writes())

    def test_verification_rejects_success_response_without_actual_move(self):
        self.board.ignore_moves = True
        with self.assertRaisesRegex(SystemExit, "Đối chiếu S1-09 thất bại"):
            self.run_sync("--complete", "S1-09", "--exclude", "S1-07")

    def test_offline_dry_run_never_reads_board(self):
        self.run_sync("--dry-run", "--complete", "S1-09", "--exclude", "S1-07")
        self.assertFalse(self.board.calls)

    def test_completion_requires_positive_result_line_not_mentions_or_unfinished(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "tasks.md"
            for result, expected in (("**Hoàn tất — 2026-11-22**", True),
                                     ("**Hoàn tất thiết kế — 2026-11-20**", True),
                                     ("**Chưa hoàn tất**", False)):
                path.write_text(f"# STEP-1\n## S1-09 · Test\n- Kết quả: {result}\n", encoding="utf-8")
                with patch.object(sync, "ROOT", root):
                    self.assertEqual(expected, sync.parse(path, None)[0]["complete"])


if __name__ == "__main__":
    unittest.main()

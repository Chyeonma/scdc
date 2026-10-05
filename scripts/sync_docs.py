#!/usr/bin/env python3
"""Copy committed docs/ between Git branches without merging application code."""

import argparse
import os
from pathlib import Path
import subprocess
import sys


class SyncError(Exception):
    """An actionable failure before or during synchronization."""


def git(root, *args, check=True):
    env = {**os.environ, "GIT_OPTIONAL_LOCKS": "0"}
    result = subprocess.run(
        ["git", *args], cwd=root, env=env, capture_output=True,
        text=True, encoding="utf-8", errors="surrogateescape",
    )
    if check and result.returncode:
        raise SyncError(result.stderr.strip() or result.stdout.strip()
                        or f"git {args[0]} thất bại.")
    return result


def commit_of(root, ref):
    result = git(root, "rev-parse", "--verify", "--end-of-options",
                 f"{ref}^{{commit}}", check=False)
    if result.returncode:
        raise SyncError(f"Không tìm thấy nguồn/nhánh '{ref}'. "
                        "Kiểm tra tên hoặc chạy git fetch nếu dùng origin/….")
    return result.stdout.strip()


def ensure_no_operation(root):
    for name in ("MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD",
                 "rebase-merge", "rebase-apply", "sequencer"):
        path = Path(git(root, "rev-parse", "--git-path", name).stdout.strip())
        if not path.is_absolute():
            path = root / path
        if path.exists():
            raise SyncError("Git đang merge/rebase/cherry-pick/revert. "
                            "Hoàn tất hoặc hủy thao tác đó trước khi đồng bộ.")


def synchronize(source, target, dry_run):
    root = Path(git(None, "rev-parse", "--show-toplevel").stdout.strip())
    branch = git(root, "symbolic-ref", "--quiet", "--short", "HEAD", check=False)
    if branch.returncode:
        raise SyncError("Đang ở detached HEAD; hãy chuyển vào một nhánh local.")
    current = branch.stdout.strip()
    target = target or current
    valid_target = git(root, "check-ref-format", f"refs/heads/{target}", check=False)
    if valid_target.returncode:
        raise SyncError(f"Tên nhánh đích không hợp lệ: '{target}'.")
    target_ref = f"refs/heads/{target}"
    target_commit = commit_of(root, target_ref)
    source_commit = commit_of(root, source)
    tree = git(root, "ls-tree", source_commit, "--", "docs").stdout
    if not tree.startswith("040000 tree "):
        raise SyncError(f"Nguồn '{source}' không có thư mục docs/ đã commit.")

    changes = git(root, "diff", "--no-ext-diff", "--no-renames", "--name-status",
                  target_commit, source_commit, "--", "docs/").stdout.strip()
    print(f"Docs: {source} ({source_commit[:8]}) → {target}", flush=True)
    if changes:
        print("A = thêm, M = sửa, D = xóa, T = đổi loại file:", flush=True)
        print(changes, flush=True)
    else:
        print("Hai nhánh đã có cùng nội dung docs/.", flush=True)
    if dry_run:
        print("Xem trước bản đã commit; chưa đổi nhánh, file hoặc staging.")
        return

    ensure_no_operation(root)
    if target != current:
        dirty = git(root, "status", "--porcelain", "--untracked-files=all").stdout
        if dirty:
            raise SyncError("Working tree có thay đổi chưa commit hoặc file mới. "
                            "Cất thay đổi trước khi chuyển nhánh đích.")
    else:
        dirty = git(root, "status", "--porcelain", "--untracked-files=all",
                    "--ignored", "--", "docs/").stdout
        if dirty:
            raise SyncError("docs/ có thay đổi chưa commit hoặc file chưa được Git "
                            "quản lý (kể cả ignored). Kiểm tra và cất chúng trước "
                            "khi đồng bộ; code ngoài docs/ được giữ nguyên.")

    if target != current:
        git(root, "switch", "--no-guess", "--no-overwrite-ignore", "--", target)
        # The target's ignore rules can differ from the original branch's rules.
        dirty_docs = git(root, "status", "--porcelain", "--untracked-files=all",
                         "--ignored", "--", "docs/").stdout
        if dirty_docs:
            git(root, "switch", "--no-guess", "--no-overwrite-ignore", "--", current)
            raise SyncError("docs/ ở nhánh đích có file chưa được Git quản lý. "
                            "Đã quay lại nhánh ban đầu; chưa chép tài liệu.")

    if changes:
        git(root, "restore", f"--source={source_commit}", "--staged", "--worktree",
            "--", "docs/")
        print(f"Đã đồng bộ docs/ vào '{target}' và đưa thay đổi vào staging.")
        print("Review: git diff --cached -- docs/")
        print("Sau khi review, dùng git commit để lưu và git push để gửi lên remote.")
    else:
        print(f"Nhánh hiện tại: {target}. Không cần chép tài liệu.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", default=os.environ.get("SCDC_DOCS_FROM", "main"),
                        help="Nhánh/ref nguồn đã commit (mặc định: main).")
    parser.add_argument("--target", default=os.environ.get("SCDC_DOCS_TO") or None,
                        help="Nhánh đích local (mặc định: nhánh đang làm).")
    parser.add_argument("--dry-run", action="store_true", help="Chỉ xem trước.")
    args = parser.parse_args()
    try:
        synchronize(args.source, args.target, args.dry_run)
    except (SyncError, OSError) as exc:
        print(f"Lỗi: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

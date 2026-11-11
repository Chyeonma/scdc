#!/usr/bin/env python3
"""Đồng bộ một chiều thẻ từ docs/project/tasks/*.md lên Trello.

Repo giữ NỘI DUNG thẻ (tiêu đề, mô tả). Trello giữ cột trạng thái, người được
gán, hạn, bình luận: script không bao giờ đổi các phần đó, không xóa hay lưu
trữ thẻ.

Định dạng file:
    # STEP-1 — Tên bước          <- mã đầu tiên của H1 thành nhãn (label) Trello
    ## S1-01 · Tiêu đề thẻ       <- mã thẻ; tên trên Trello là "[S1-01] Tiêu đề thẻ"
    - Nhãn: Backend, Identity    <- (tùy chọn) nhãn đã có trên bảng; không đưa vào mô tả
    - Người làm: ...             <- mọi dòng tới thẻ tiếp theo thành mô tả

File có dòng "Trạng thái: nháp" trước thẻ đầu tiên thì KHÔNG đồng bộ (dùng để
viết trước thẻ của bước sau); xóa dòng đó để đưa thẻ lên Trello.

Dòng "- Nhãn:" đặt trước thẻ đầu tiên thì áp cho mọi thẻ trong file. Nhãn bước
(STEP-n) được tạo nếu chưa có; nhãn khác phải có sẵn trên bảng (so khớp không
phân biệt hoa thường), nếu không chỉ cảnh báo. Script chỉ THÊM nhãn, không gỡ
nhãn nào khỏi thẻ.

Thẻ trên Trello được nhận ra bằng tiền tố "[mã]" trong tên, nên đổi tiêu đề
hay chuyển cột trên Trello vẫn không tạo thẻ trùng.

Biến môi trường: TRELLO_KEY, TRELLO_TOKEN, TRELLO_BOARD_ID, TRELLO_LIST_ID
(cột nhận thẻ mới). Tùy chọn: REPO_URL để mô tả có link về file nguồn.

Chạy thử không cần mạng:  python3 scripts/sync_trello.py --dry-run
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path


def fail(message: str) -> None:
    """Dừng với lỗi; trên GitHub Actions hiện lỗi ở trang tóm tắt của lần chạy."""
    if os.environ.get("GITHUB_ACTIONS"):
        print(f"::error title=Sync Trello::{message}")
    sys.exit(message)


ROOT = Path(__file__).resolve().parents[1]
TASKS = ROOT / "docs/project/tasks"
API = "https://api.trello.com/1"
CARD_HEADING = re.compile(r"^##\s+([A-Z][A-Z0-9]*-[A-Z0-9-]+)\s*[·:—-]\s*(.+?)\s*$")
STEP_HEADING = re.compile(r"^#\s+([A-Z][A-Z0-9]*-[0-9A-Z]+)\b")
NAME_PREFIX = re.compile(r"^\[([^\]]+)\]")
DRAFT_LINE = re.compile(r"^\W*Trạng thái\W*:?\W*\s*nháp\b", re.IGNORECASE)
LABEL_LINE = re.compile(r"^\s*[-*]\s*(?:Nhãn|Labels?)\s*:\s*(.*)$", re.IGNORECASE)
FOOTER = "\n\n---\n_Nội dung thẻ đồng bộ từ `{path}`; sửa trong repo, sửa ở đây sẽ bị ghi đè. Cột, người gán, hạn và bình luận quản lý trên Trello._"


def is_draft(path: Path) -> bool:
    for line in path.read_text(encoding="utf-8").splitlines():
        if CARD_HEADING.match(line):
            return False
        if DRAFT_LINE.match(line):
            return True
    return False


def parse(path: Path, repo_url: str | None) -> list[dict]:
    lines = path.read_text(encoding="utf-8").splitlines()
    step = next((m.group(1) for l in lines if (m := STEP_HEADING.match(l))), None)
    rel = path.relative_to(ROOT).as_posix()
    source = f"[{rel}]({repo_url.rstrip('/')}/blob/main/{rel})" if repo_url else rel
    cards, current, file_labels = [], None, []
    for line in lines:
        if m := CARD_HEADING.match(line):
            current = {"code": m.group(1), "title": m.group(2), "body": [], "labels": []}
            cards.append(current)
        elif line.startswith("## "):
            current = None
        elif m := LABEL_LINE.match(line):
            names = [n.strip() for n in m.group(1).split(",") if n.strip()]
            (current["labels"] if current is not None else file_labels).extend(names)
        elif current is not None:
            current["body"].append(line)
    for card in cards:
        wanted = [step, *file_labels, *card["labels"]] if step else [*file_labels, *card["labels"]]
        card["labels"] = list(dict.fromkeys(wanted))
        card["step"] = step
        card["name"] = f"[{card['code']}] {card['title']}"
        card["desc"] = "\n".join(card.pop("body")).strip() + FOOTER.format(path=source)
    codes = [c["code"] for c in cards]
    dup = {c for c in codes if codes.count(c) > 1}
    if dup:
        fail(f"{rel}: mã thẻ bị trùng: {', '.join(sorted(dup))}")
    return cards


class Trello:
    def __init__(self, key: str, token: str):
        self.auth = {"key": key, "token": token}

    def call(self, method: str, path: str, **params):
        query = urllib.parse.urlencode({**self.auth, **(params if method == "GET" else {})})
        data = None
        headers = {}
        if method != "GET":
            data = json.dumps(params).encode()
            headers["Content-Type"] = "application/json; charset=utf-8"
        req = urllib.request.Request(f"{API}{path}?{query}", data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                return json.load(resp)
        except urllib.error.HTTPError as err:
            detail = err.read().decode(errors="replace")[:300]
            fail(f"Trello {method} {path} lỗi {err.code}: {detail}")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--dry-run", action="store_true", help="chỉ in thẻ đọc được, không gọi Trello")
    ap.add_argument("files", nargs="*", type=Path, help="mặc định: docs/project/tasks/*.md")
    args = ap.parse_args()

    files = args.files or sorted(p for p in TASKS.glob("*.md") if p.name != "README.md")
    repo_url = os.environ.get("REPO_URL")
    seen: dict[str, str] = {}
    for f in files:
        for c in parse(f.resolve(), repo_url):
            if c["code"] in seen and seen[c["code"]] != f.name:
                fail(f"Mã thẻ {c['code']} có ở cả {seen[c['code']]} và {f.name}")
            seen[c["code"]] = f.name
    drafts = [f for f in files if is_draft(f)]
    for f in drafts:
        print(f"Bỏ qua {f.name}: đang là nháp ({len(parse(f.resolve(), repo_url))} thẻ)")
    files = [f for f in files if f not in drafts]
    cards = [c for f in files for c in parse(f.resolve(), repo_url)]

    if args.dry_run:
        for c in cards:
            print(f"{c['name']}  [{', '.join(c['labels'])}]")
        print(f"{len(cards)} thẻ từ {len(files)} file")
        return

    env = {k: os.environ.get(k) for k in ("TRELLO_KEY", "TRELLO_TOKEN", "TRELLO_BOARD_ID", "TRELLO_LIST_ID")}
    missing = [k for k, v in env.items() if not v]
    if missing:
        fail("Thiếu biến môi trường: " + ", ".join(missing))
    t = Trello(env["TRELLO_KEY"], env["TRELLO_TOKEN"])
    # TRELLO_BOARD_ID có thể là mã rút gọn trong link (trello.com/b/<mã>); một số
    # API (tạo nhãn) chỉ nhận ID đầy đủ 24 ký tự, nên đổi sang ID đầy đủ trước.
    info = t.call("GET", f"/boards/{env['TRELLO_BOARD_ID']}", fields="id,name")
    board = info["id"]
    lst = t.call("GET", f"/lists/{env['TRELLO_LIST_ID']}", fields="idBoard,name")
    if lst["idBoard"] != board:
        fail(f"Cột TRELLO_LIST_ID ({lst['name']}) không thuộc bảng {info['name']}")
    print(f"Bảng: {info['name']} · cột nhận thẻ mới: {lst['name']}")

    existing = {}
    for card in t.call("GET", f"/boards/{board}/cards/all", fields="name,desc,idLabels,closed"):
        if m := NAME_PREFIX.match(card["name"]):
            existing.setdefault(m.group(1), card)

    labels: dict[str, str] = {}
    for label in t.call("GET", f"/boards/{board}/labels", fields="name", limit="1000"):
        if label["name"]:
            labels.setdefault(label["name"].strip().casefold(), label["id"])
    missing_labels: dict[str, list[str]] = {}

    def label_ids(card: dict) -> list[str]:
        ids = []
        for name in card["labels"]:
            key = name.casefold()
            if key not in labels and name == card["step"]:
                labels[key] = t.call("POST", "/labels", name=name, color="blue", idBoard=board)["id"]
                print(f"tạo nhãn {name}")
            if key in labels:
                ids.append(labels[key])
            else:
                missing_labels.setdefault(name, []).append(card["code"])
        return ids

    created = updated = same = 0
    for c in cards:
        lids = label_ids(c)
        old = existing.get(c["code"])
        if old is None:
            params = {"idList": env["TRELLO_LIST_ID"], "name": c["name"], "desc": c["desc"], "pos": "bottom"}
            if lids:
                params["idLabels"] = ",".join(lids)
            t.call("POST", "/cards", **params)
            created += 1
            print(f"tạo   {c['name']}")
            continue
        changes = {}
        if old["name"] != c["name"]:
            changes["name"] = c["name"]
        if old["desc"].strip() != c["desc"].strip():
            changes["desc"] = c["desc"]
        added = [lid for lid in lids if lid not in old["idLabels"]]
        if added:
            changes["idLabels"] = ",".join([*old["idLabels"], *added])
        if changes:
            t.call("PUT", f"/cards/{old['id']}", **changes)
            updated += 1
            note = " (thẻ đang lưu trữ trên Trello)" if old.get("closed") else ""
            print(f"sửa   {c['name']}: {', '.join(changes)}{note}")
        else:
            same += 1

    for name, codes in missing_labels.items():
        msg = f"Nhãn '{name}' không có trên bảng, bỏ qua cho {', '.join(codes)}. Tạo nhãn trên Trello hoặc sửa tên trong file."
        print(f"::warning title=Sync Trello::{msg}" if os.environ.get("GITHUB_ACTIONS") else f"Cảnh báo: {msg}")

    synced = {c["code"] for c in cards}
    gone = sorted(code for code in existing if re.fullmatch(r"S\d+-\d+", code) and code not in synced)
    if gone:
        print("Có trên Trello nhưng không còn trong repo (không tự xóa): " + ", ".join(gone))
    print(f"Xong: tạo {created}, sửa {updated}, giữ nguyên {same}")


if __name__ == "__main__":
    main()

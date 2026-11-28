#!/usr/bin/env python3
"""Đồng bộ một chiều thẻ từ docs/project/tasks/*.md lên Trello.

Mặc định chỉ đồng bộ NỘI DUNG thẻ. Chỉ --complete mới chuyển các mã được chỉ
định sang cột hoàn tất; --exclude bỏ qua toàn bộ thẻ. Không đổi người gán,
hạn, bình luận, không xóa hay lưu trữ thẻ. --preview chỉ đọc Trello.

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
(cột nhận task mới); TRELLO_ISSUE_LIST_ID cho thẻ ISSUE-*. Tùy chọn: REPO_URL
để mô tả có link về file nguồn. --only giới hạn các mã cần đồng bộ.

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
COMPLETE_LINE = re.compile(r"^- Kết quả:\s+\*\*Hoàn tất\b", re.MULTILINE)
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
    ref = urllib.parse.quote(os.environ.get("REPO_REF", "main"), safe="/")
    source = f"[{rel}]({repo_url.rstrip('/')}/blob/{ref}/{rel})" if repo_url else rel
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
        body = "\n".join(card.pop("body")).strip()
        card["complete"] = bool(COMPLETE_LINE.search(body))
        card["desc"] = body + FOOTER.format(path=source)
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
            fail(f"Trello {method} {path} lỗi HTTP {err.code}; không in phản hồi chứa thông tin xác thực.")
        except (urllib.error.URLError, TimeoutError):
            fail(f"Trello {method} {path}: không kết nối được; chưa xác nhận kết quả, chạy lại để đối chiếu.")


def parse_codes(value: str) -> set[str]:
    result = {code.strip() for code in value.split(",") if code.strip()}
    if any(not re.fullmatch(r"[A-Z][A-Z0-9]*-[A-Z0-9-]+", code) for code in result):
        fail("Danh sách mã task không hợp lệ; dùng mã cách nhau bằng dấu phẩy.")
    return result


def index_cards(cards: list[dict], required: set[str]) -> dict[str, dict]:
    indexed = {}
    for card in cards:
        if m := NAME_PREFIX.match(card["name"]):
            code = m.group(1)
            if code in indexed and code in required:
                fail(f"Có nhiều thẻ Trello cho {code}; dừng trước khi đổi trạng thái.")
            indexed.setdefault(code, card)
    return indexed


def done_column(lists: list[dict], name: str) -> dict:
    matches = [item for item in lists if item["name"].strip().casefold() == name.strip().casefold()]
    if len(matches) != 1:
        fail(f"Cần đúng một cột hoàn tất tên '{name}'. Cột đang mở: " + ", ".join(item["name"] for item in lists))
    return matches[0]


def card_state(card: dict) -> tuple:
    return (card["id"], card["name"], card["desc"], card["idList"], card.get("closed"), tuple(sorted(card["idLabels"])))


def states_for(cards: list[dict], code: str) -> list[tuple]:
    return sorted(card_state(card) for card in cards
                  if (match := NAME_PREFIX.match(card["name"])) and match.group(1) == code)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--dry-run", action="store_true", help="chỉ in thẻ đọc được, không gọi Trello")
    ap.add_argument("--preview", action="store_true", default=os.environ.get("TRELLO_PREVIEW") == "true", help="đọc Trello và in kế hoạch, không ghi")
    ap.add_argument("--complete", default=os.environ.get("TRELLO_COMPLETE_CODES", ""), help="các mã đã hoàn tất trong repo, cách nhau bằng dấu phẩy")
    ap.add_argument("--exclude", default=os.environ.get("TRELLO_EXCLUDE_CODES", ""), help="các mã giữ nguyên trên Trello")
    ap.add_argument("--only", default=os.environ.get("TRELLO_ONLY_CODES", ""), help="chỉ đồng bộ các mã được chỉ định")
    ap.add_argument("--done-list", default=os.environ.get("TRELLO_DONE_LIST", "Hoàn tất"), help="tên chính xác của cột hoàn tất")
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
    completed, excluded = parse_codes(args.complete), parse_codes(args.exclude)
    if completed & excluded:
        fail("Task vừa yêu cầu hoàn tất vừa bị loại trừ: " + ", ".join(sorted(completed & excluded)))
    cards = [card for card in cards if card["code"] not in excluded]
    selected = parse_codes(args.only)
    if selected:
        if unknown := selected - {card["code"] for card in cards}:
            fail("Mã không có trong nguồn hoặc đã bị loại trừ: " + ", ".join(sorted(unknown)))
        cards = [card for card in cards if card["code"] in selected]
    available = {card["code"]: card for card in cards}
    if invalid := sorted(code for code in completed if code not in available or not available[code]["complete"]):
        fail("Task chưa có kết quả hoàn tất trong nguồn đang đồng bộ: " + ", ".join(invalid))
    if excluded:
        print("Giữ nguyên toàn bộ thẻ: " + ", ".join(sorted(excluded)))

    if args.dry_run:
        for c in cards:
            print(f"{c['name']}  [{', '.join(c['labels'])}]")
        if completed:
            print(f"Chuyển {len(completed)} task sang '{args.done_list}': " + ", ".join(sorted(completed)))
        print(f"{len(cards)} thẻ từ {len(files)} file")
        return

    env = {k: os.environ.get(k) for k in ("TRELLO_KEY", "TRELLO_TOKEN", "TRELLO_BOARD_ID", "TRELLO_LIST_ID")}
    missing = [k for k, v in env.items() if not v]
    if missing:
        fail("Thiếu biến môi trường: " + ", ".join(missing))
    issues = {card["code"] for card in cards if card["code"].startswith("ISSUE-")}
    issue_list = os.environ.get("TRELLO_ISSUE_LIST_ID")
    if issues and not issue_list:
        fail("Thiếu biến môi trường: TRELLO_ISSUE_LIST_ID")
    t = Trello(env["TRELLO_KEY"], env["TRELLO_TOKEN"])
    # TRELLO_BOARD_ID có thể là mã rút gọn trong link (trello.com/b/<mã>); một số
    # API (tạo nhãn) chỉ nhận ID đầy đủ 24 ký tự, nên đổi sang ID đầy đủ trước.
    info = t.call("GET", f"/boards/{env['TRELLO_BOARD_ID']}", fields="id,name,url")
    board = info["id"]
    lst = t.call("GET", f"/lists/{env['TRELLO_LIST_ID']}", fields="idBoard,name,closed")
    if lst["idBoard"] != board:
        fail(f"Cột TRELLO_LIST_ID ({lst['name']}) không thuộc bảng {info['name']}")
    if lst.get("closed"):
        fail("Cột TRELLO_LIST_ID đang đóng.")
    print(f"Bảng: {info['name']} · cột nhận thẻ mới: {lst['name']}")
    if issues:
        issue_column = t.call("GET", f"/lists/{issue_list}", fields="idBoard,name,closed")
        if issue_column["idBoard"] != board or issue_column.get("closed"):
            fail("Cột TRELLO_ISSUE_LIST_ID không thuộc bảng hoặc đang đóng.")
        print(f"Cột nhận issue mới: {issue_column['name']} ({issue_list})")

    fields = "name,desc,idLabels,closed,idList,shortUrl"
    before = t.call("GET", f"/boards/{board}/cards/all", fields=fields)
    existing = index_cards(before, set(available) | completed | excluded)
    target = None
    if completed or args.preview:
        lists = t.call("GET", f"/boards/{board}/lists", filter="open", fields="name")
        print("Cột đang mở: " + ", ".join(item["name"] for item in lists))
        if completed:
            target = done_column(lists, args.done_list)
    if args.preview:
        for c in cards:
            old = existing.get(c["code"])
            action = "cập nhật" if old else "tạo"
            status = f" → {target['name']}" if c["code"] in completed else ""
            if old is None and c["code"] in issues and c["code"] not in completed:
                status = f" → {issue_column['name']}"
            print(f"Dự kiến {action} {c['name']}{status}")
        print(f"Chỉ xem trước: {len(cards)} task, {len(completed)} task hoàn tất; chưa ghi Trello.")
        return

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

    created = updated = same = moved = 0
    for c in cards:
        lids = label_ids(c)
        old = existing.get(c["code"])
        if old is None:
            params = {"idList": issue_list if c["code"] in issues else env["TRELLO_LIST_ID"],
                      "name": c["name"], "desc": c["desc"], "pos": "bottom"}
            if c["code"] in completed:
                params["idList"] = target["id"]
                moved += 1
            if lids:
                params["idLabels"] = ",".join(lids)
            t.call("POST", "/cards", **params)
            created += 1
            print(f"tạo   {c['name']}")
            continue
        changes = {}
        if c["code"] in completed and old["idList"] != target["id"]:
            changes["idList"] = target["id"]
            moved += 1
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

    if completed or excluded or issues:
        after = t.call("GET", f"/boards/{board}/cards/all", fields=fields)
        verified = index_cards(after, completed | excluded | issues)
        for code in sorted(issues):
            expected_list = target["id"] if code in completed else existing.get(code, {}).get("idList", issue_list)
            card = verified.get(code)
            source = available[code]
            if (card is None or card["idList"] != expected_list or card["name"] != source["name"]
                    or card["desc"].strip() != source["desc"].strip()):
                fail(f"Đối chiếu {code} thất bại: nội dung hoặc cột không khớp.")
            print(f"Xác nhận {code}: nội dung khớp, cột {expected_list}")
            if url := card.get("shortUrl"):
                print(f"Thẻ {code}: {url}")
        for code in sorted(completed):
            if code not in verified or verified[code]["idList"] != target["id"]:
                fail(f"Đối chiếu {code} thất bại: chưa ở cột hoàn tất.")
            print(f"Xác nhận {code}: {target['name']}")
        for code in sorted(excluded):
            if states_for(before, code) != states_for(after, code):
                fail(f"Thẻ {code} đã thay đổi trong lúc đồng bộ; cần kiểm tra lại.")
            print(f"Xác nhận {code}: giữ nguyên")
    synced = {c["code"] for c in cards} | excluded
    gone = sorted(code for code in existing if re.fullmatch(r"S\d+-\d+", code) and code not in synced)
    if gone and not selected and not args.files:
        print("Có trên Trello nhưng không còn trong repo (không tự xóa): " + ", ".join(gone))
    print(f"Xong: tạo {created}, sửa {updated}, giữ nguyên {same}, chuyển hoàn tất {moved}; đối chiếu {len(completed)} task hoàn tất.")
    print(f"Bảng Trello: {info.get('url', '')}")


if __name__ == "__main__":
    main()

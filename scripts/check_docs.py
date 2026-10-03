#!/usr/bin/env python3
"""Check local Markdown links and anchors in current SCDC documentation."""

from collections import Counter
from pathlib import Path
import re
import sys
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT / "docs/archive"


def without_fences(text):
    return re.sub(r"^(`{3,}|~{3,})[^\n]*\n.*?^\1\s*$",
                  lambda match: "\n" * match.group().count("\n"), text,
                  flags=re.MULTILINE | re.DOTALL)


def anchors(path):
    text = without_fences(path.read_text(encoding="utf-8"))
    explicit = re.findall(r'<a\s+id="([^"]+)"', text)
    values = set(explicit)
    counts = Counter()
    for heading in re.findall(r"^#{1,6}\s+(.+?)\s*#*\s*$", text, re.MULTILINE):
        slug = re.sub(r"[^\w\- ]", "", heading.lower()).replace(" ", "-")
        index = counts[slug]
        counts[slug] += 1
        values.add(slug + (f"-{index}" if index else ""))
    duplicates = [name for name, count in Counter(explicit).items() if count > 1]
    return values, duplicates


def main():
    files = [ROOT / "README.md", *sorted((ROOT / "docs").rglob("*.md")),
             ROOT / "database/postgres/README.md",
             *sorted(ROOT.glob("services/Modules/*/README.md"))]
    files = [path for path in files if path.exists() and not path.is_relative_to(ARCHIVE)]
    errors = []
    cache = {}
    checked = 0
    for path in files:
        _, duplicates = anchors(path)
        errors.extend(f"{path.relative_to(ROOT)}: duplicate anchor #{name}" for name in duplicates)
        text = without_fences(path.read_text(encoding="utf-8"))
        for match in re.finditer(r"!?\[[^\]]*\]\(([^\n)]+)\)", text):
            href = match.group(1).strip()
            if href.startswith("<"):
                href = href[1:href.index(">")]
            else:
                href = href.split(' "', 1)[0]
            url = urlsplit(href)
            if url.scheme or url.netloc:
                continue
            checked += 1
            line = text.count("\n", 0, match.start()) + 1
            target = (path.parent / unquote(url.path)).resolve() if url.path else path
            location = f"{path.relative_to(ROOT)}:{line}"
            if not target.exists():
                errors.append(f"{location}: missing target {href}")
                continue
            if url.fragment and target.is_file() and target.suffix == ".md":
                if target not in cache:
                    cache[target] = anchors(target)[0]
                if unquote(url.fragment) not in cache[target]:
                    errors.append(f"{location}: missing anchor {href}")
    for error in errors:
        print(error, file=sys.stderr)
    print(f"Checked {len(files)} current Markdown files and {checked} local links; {len(errors)} errors.")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())

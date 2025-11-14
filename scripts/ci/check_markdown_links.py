#!/usr/bin/env python3
"""Validate Markdown links (relative + HTTP) deterministically."""

from __future__ import annotations

import argparse
import json
import re
import time
from pathlib import Path
from typing import List, Sequence

try:
    import requests
except ModuleNotFoundError as exc:  # pragma: no cover
    raise SystemExit(
        "requests is not installed. Run `python -m pip install -r requirements.tooling.txt`."
    ) from exc

try:
    from markdown_it import MarkdownIt
except ModuleNotFoundError as exc:  # pragma: no cover
    raise SystemExit(
        "markdown-it-py is not installed. Run `python -m pip install -r requirements.tooling.txt`."
    ) from exc


REPO_ROOT = Path(__file__).resolve().parents[2]
CONFIG_PATH = REPO_ROOT / ".markdown-link-check.json"
DEFAULT_TIMEOUT = 30.0
DEFAULT_ALIVE_CODES = {200, 201, 202, 203, 204, 206, 301, 302, 307, 308, 429}
DEFAULT_IGNORE_PATTERNS = [re.compile(r"NovaSharp\.org", re.IGNORECASE)]
SKIP_FILES = {
    "AGENTS.md",
    "CLAUDE.md",
}
MARKDOWN_PARSER = MarkdownIt("commonmark", {"linkify": True}).enable("linkify")


def parse_duration(value) -> float:
    if isinstance(value, (int, float)):
        return float(value)

    if isinstance(value, str):
        match = re.fullmatch(r"(\d+)(ms|s|m)", value.strip(), flags=re.IGNORECASE)
        if match:
            amount = float(match.group(1))
            unit = match.group(2).lower()
            if unit == "ms":
                return amount / 1000.0
            if unit == "s":
                return amount
            if unit == "m":
                return amount * 60.0

    return DEFAULT_TIMEOUT


def load_config():
    if not CONFIG_PATH.exists():
        return {
            "timeout": DEFAULT_TIMEOUT,
            "retry_count": 0,
            "retry_on_429": True,
            "alive_codes": DEFAULT_ALIVE_CODES,
            "ignore_patterns": DEFAULT_IGNORE_PATTERNS,
        }

    data = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    alive_codes = set(data.get("aliveStatusCodes", DEFAULT_ALIVE_CODES))
    timeout = parse_duration(data.get("timeout", DEFAULT_TIMEOUT))
    retry_count = int(data.get("retryCount", 0))
    retry_on_429 = bool(data.get("retryOn429", True))
    patterns = []
    for entry in data.get("ignorePatterns", []):
        pattern = entry.get("pattern") if isinstance(entry, dict) else None
        if pattern:
            patterns.append(re.compile(pattern))

    if not patterns:
        patterns = list(DEFAULT_IGNORE_PATTERNS)

    return {
        "timeout": timeout,
        "retry_count": retry_count,
        "retry_on_429": retry_on_429,
        "alive_codes": alive_codes,
        "ignore_patterns": patterns,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--files",
        nargs="+",
        required=True,
        help="Markdown files to validate.",
    )
    parser.add_argument(
        "--include-skipped",
        action="store_true",
        help="Also check files normally skipped (AGENTS.md, CLAUDE.md).",
    )
    return parser.parse_args()


def should_skip(path: Path, include_skipped: bool) -> bool:
    if not path.exists():
        return True

    try:
        rel = path.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        rel = path.as_posix()

    return not include_skipped and rel in SKIP_FILES


def iter_tokens(tokens):
    for token in tokens:
        yield token
        if token.children:
            yield from iter_tokens(token.children)


def extract_links(markdown: str) -> List[str]:
    tokens = MARKDOWN_PARSER.parse(markdown)

    links: List[str] = []
    for token in iter_tokens(tokens):
        if token.type == "link_open":
            attrs = dict(token.attrs or [])
            href = attrs.get("href")
            if href:
                links.append(href)
    return links


def check_relative_link(link: str, source_file: Path) -> bool:
    target, _, _ = link.partition("#")
    if not target:
        return True

    base = REPO_ROOT if target.startswith("/") else source_file.parent
    candidate = (base / target.lstrip("/")).resolve()
    return candidate.exists()


def request_with_retry(
    url: str,
    timeout: float,
    alive_codes: set[int],
    retry_count: int,
    retry_on_429: bool,
) -> bool:
    attempts = 0
    while True:
        attempts += 1
        try:
            response = requests.head(url, allow_redirects=True, timeout=timeout)
            status_code = response.status_code
            if status_code in {405, 501}:
                response = requests.get(url, allow_redirects=True, timeout=timeout)
                status_code = response.status_code
        except requests.RequestException:
            status_code = 0

        if status_code in alive_codes:
            return True

        should_retry = attempts <= retry_count
        if retry_on_429:
            should_retry = should_retry and status_code == 429

        if not should_retry:
            return False

        time.sleep(min(2 ** attempts, 5))


def check_links(files: Sequence[Path], include_skipped: bool) -> int:
    config = load_config()
    timeout = config["timeout"]
    retries = config["retry_count"]
    retry_on_429 = config["retry_on_429"]
    alive_codes = config["alive_codes"]
    ignore_patterns = config["ignore_patterns"]

    failed = 0
    for file_path in files:
        if should_skip(file_path, include_skipped):
            continue

        content = file_path.read_text(encoding="utf-8")
        for link in extract_links(content):
            if any(pattern.search(link) for pattern in ignore_patterns):
                continue
            if link.startswith("#") or link.lower().startswith("mailto:") or link.lower().startswith("tel:"):
                continue

            if link.startswith(("http://", "https://")):
                ok = request_with_retry(link, timeout, alive_codes, retries, retry_on_429)
            else:
                ok = check_relative_link(link, file_path)

            if not ok:
                failed += 1
                rel = file_path.relative_to(REPO_ROOT)
                print(f"[link-check] {rel}: {link} is unreachable.")

    if failed:
        print(f"[link-check] Found {failed} broken link(s).")
    else:
        print("[link-check] All checked links are reachable.")

    return failed


def main() -> int:
    args = parse_args()
    files: List[Path] = []
    for file_arg in args.files:
        path = Path(file_arg)
        if not path.is_absolute():
            path = REPO_ROOT / path
        files.append(path.resolve())

    failures = check_links(files, include_skipped=args.include_skipped)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())

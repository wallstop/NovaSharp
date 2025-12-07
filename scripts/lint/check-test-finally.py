#!/usr/bin/env python3
"""Fails if tests reintroduce manual finally blocks for cleanup."""

from __future__ import annotations

import pathlib
import re
import sys
from typing import Iterable, Tuple

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
TEST_ROOT = REPO_ROOT / "src" / "tests"

# Pattern to match finally blocks
FINALLY_PATTERN = re.compile(r"^\s*finally\b")


def iter_matches() -> Iterable[Tuple[pathlib.Path, str]]:
    """Search for finally blocks in test .cs files using pure Python."""
    if not TEST_ROOT.exists():
        return

    for filepath in TEST_ROOT.rglob("*.cs"):
        # Skip hidden directories and build output
        parts = filepath.relative_to(REPO_ROOT).parts
        if any(p.startswith(".") or p in ("bin", "obj") for p in parts):
            continue

        try:
            content = filepath.read_text(encoding="utf-8", errors="ignore")
        except (OSError, UnicodeDecodeError):
            continue

        for line_num, line in enumerate(content.splitlines(), start=1):
            if FINALLY_PATTERN.search(line):
                yield filepath, f"{line_num}:{line.strip()}"


def main() -> int:
    violations = [
        f"{path.as_posix()}:{line}"
        for path, line in iter_matches()
    ]

    if violations:
        print(
            "Tests must use the shared scope helpers instead of manual try/finally cleanup blocks.\n"
        )
        for entry in violations:
            print(entry)
        print(
            "\nReplace the finally block with the appropriate scope (TempFileScope, SemaphoreSlimScope, DeferredActionScope, etc.).",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

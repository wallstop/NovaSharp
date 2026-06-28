#!/usr/bin/env python3
"""Fails if tests reintroduce manual finally blocks for cleanup."""

from __future__ import annotations

import os
import pathlib
import re
import sys
from typing import Iterable, Tuple


def is_ci() -> bool:
    return os.environ.get("GITHUB_ACTIONS") == "true" or os.environ.get("CI") == "true"


def emit_error(file_path: str, line_number: int, message: str) -> None:
    if is_ci():
        print(f"::error file={file_path},line={line_number}::{message}")
    print(f"{file_path}:{line_number}: {message}")

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
    violations = list(iter_matches())

    if violations:
        print(
            "Tests must use the shared scope helpers instead of manual try/finally cleanup blocks.\n"
        )
        message = "Use scope helpers instead of manual try/finally cleanup blocks"
        for path, line_info in violations:
            # line_info is "line_num:content", extract line number
            line_num_str = line_info.split(":")[0]
            line_num = int(line_num_str) if line_num_str.isdigit() else 1
            emit_error(path.as_posix(), line_num, message)
        print(
            "\nReplace the finally block with the appropriate scope (TempFileScope, SemaphoreSlimScope, DeferredActionScope, etc.).",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

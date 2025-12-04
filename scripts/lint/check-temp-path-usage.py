#!/usr/bin/env python3
"""
Fails when tests bypass TempFileScope/TempDirectoryScope and call Path.GetTempPath directly.
"""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parents[2]
TESTS_ROOT = REPO_ROOT / "src/tests"
ALLOWED_FILES = {
    "src/tests/TestInfrastructure/Scopes/TempResourcesScopes.cs",
}
TARGET_TOKEN = "Path.GetTempPath"


def find_violations(file_path: Path) -> Iterable[tuple[int, str]]:
    with file_path.open(encoding="utf-8", errors="ignore") as handle:
        for line_number, line in enumerate(handle, start=1):
            if TARGET_TOKEN in line:
                yield line_number, line.rstrip()


def main() -> int:
    violations: list[tuple[str, int, str]] = []

    for candidate in TESTS_ROOT.rglob("*.cs"):
        relative = candidate.relative_to(REPO_ROOT).as_posix()
        if relative in ALLOWED_FILES:
            continue

        for line_number, text in find_violations(candidate):
            violations.append((relative, line_number, text))

    if violations:
        print(
            "Tests should rely on TempFileScope/TempDirectoryScope instead of "
            "calling Path.GetTempPath directly. Update the test to use the shared "
            "scope helpers.",
            file=sys.stderr,
        )
        for relative, line_number, text in violations:
            print(f"- {relative}:{line_number} :: {text}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

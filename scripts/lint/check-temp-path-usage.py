#!/usr/bin/env python3
"""
Fails when tests bypass TempFileScope/TempDirectoryScope and call Path.GetTempPath directly.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path
from typing import Iterable


def is_ci() -> bool:
    return os.environ.get("GITHUB_ACTIONS") == "true" or os.environ.get("CI") == "true"


def emit_error(file_path: str, line_number: int, message: str) -> None:
    if is_ci():
        print(f"::error file={file_path},line={line_number}::{message}")
    print(f"{file_path}:{line_number}: {message}", file=sys.stderr)

REPO_ROOT = Path(__file__).resolve().parents[2]
TESTS_ROOT = REPO_ROOT / "src/tests"
TARGETS = {
    "Path.GetTempPath": {
        "src/tests/TestInfrastructure/Scopes/TempResourcesScopes.cs",
    },
    "Path.GetTempFileName": set(),
}


def find_violations(file_path: Path) -> Iterable[tuple[str, int, str]]:
    with file_path.open(encoding="utf-8", errors="ignore") as handle:
        for line_number, line in enumerate(handle, start=1):
            for token in TARGETS.keys():
                if token in line:
                    yield token, line_number, line.rstrip()


def main() -> int:
    violations: list[tuple[str, str, int, str]] = []

    for candidate in TESTS_ROOT.rglob("*.cs"):
        relative = candidate.relative_to(REPO_ROOT).as_posix()
        for token, line_number, text in find_violations(candidate):
            allowed = TARGETS.get(token, set())
            if relative in allowed:
                continue

            violations.append((token, relative, line_number, text))

    if violations:
        print(
            "Tests should rely on TempFileScope/TempDirectoryScope instead of "
            "calling Path.GetTempPath/Path.GetTempFileName directly. "
            "Update the test to use the shared scope helpers.",
            file=sys.stderr,
        )
        for token, relative, line_number, text in violations:
            emit_error(relative, line_number, f"{token} :: {text}")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

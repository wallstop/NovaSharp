#!/usr/bin/env python3
"""Fails if tests reintroduce manual finally blocks for cleanup."""

from __future__ import annotations

import pathlib
import subprocess
import sys
from typing import Iterable, Tuple

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
TEST_ROOT = REPO_ROOT / "src" / "tests"


def iter_matches() -> Iterable[Tuple[pathlib.Path, str]]:
    if not TEST_ROOT.exists():
        return []

    try:
        result = subprocess.run(
            [
                "rg",
                "--with-filename",
                "--line-number",
                "--glob",
                "*.cs",
                r"^\s*finally\b",
                str(TEST_ROOT),
            ],
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError as exc:  # pragma: no cover - tooling dependency
        raise SystemExit("rg (ripgrep) is required to run this check.") from exc

    if result.returncode not in (0, 1):
        print(result.stdout)
        print(result.stderr, file=sys.stderr)
        raise SystemExit(result.returncode)

    for line in result.stdout.strip().splitlines():
        if not line:
            continue
        path_str, rest = line.split(":", 1)
        yield pathlib.Path(path_str), rest


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

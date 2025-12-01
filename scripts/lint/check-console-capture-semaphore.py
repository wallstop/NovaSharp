#!/usr/bin/env python3
"""Fails if tests reference ConsoleCaptureCoordinator.Semaphore directly."""
from __future__ import annotations
import pathlib
import subprocess
import sys
from typing import Iterable

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
ALLOWED = {
    pathlib.Path(
        "src/tests/NovaSharp.Interpreter.Tests.TUnit/TestInfrastructure/ConsoleCaptureCoordinator.cs"
    )
}

def iter_matches() -> Iterable[tuple[pathlib.Path, str]]:
    try:
        result = subprocess.run(
            [
                "rg",
                "--with-filename",
                "--line-number",
                r"ConsoleCaptureCoordinator\\.Semaphore",
            ],
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )
    except FileNotFoundError as exc:
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
        if path not in ALLOWED
    ]

    if violations:
        print(
            "ConsoleCaptureCoordinator.Semaphore must remain encapsulated inside ConsoleCaptureCoordinator.RunAsync.\n"
        )
        for entry in violations:
            print(entry)
        print(
            "\nUse the RunAsync helper (or add a new scope) instead of touching the semaphore directly.",
            file=sys.stderr,
        )
        return 1
    return 0

if __name__ == "__main__":
    raise SystemExit(main())

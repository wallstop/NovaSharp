#!/usr/bin/env python3
"""Fails if tests bypass the console capture helpers."""

from __future__ import annotations

import pathlib
import subprocess
import sys
from typing import Iterable, Tuple

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
TEST_ROOT = REPO_ROOT / "src" / "tests"

INFRA_DIR = (
    TEST_ROOT / "NovaSharp.Interpreter.Tests.TUnit" / "TestInfrastructure"
).resolve()
ALLOWED_SCOPE_FILES = {
    INFRA_DIR / "ConsoleCaptureCoordinator.cs",
    INFRA_DIR / "ConsoleTestUtilities.cs",
    INFRA_DIR / "ConsoleCaptureScope.cs",
    INFRA_DIR / "ConsoleRedirectionScope.cs",
}


def run_rg(pattern: str) -> Iterable[Tuple[pathlib.Path, str]]:
    try:
        result = subprocess.run(
            [
                "rg",
                "--with-filename",
                "--line-number",
                "--glob",
                "*.cs",
                pattern,
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
    violations: list[Tuple[pathlib.Path, str, str]] = []

    semaphore_pattern = r"ConsoleCaptureCoordinator\.Semaphore"
    for path, line in run_rg(semaphore_pattern):
        if path.resolve() in ALLOWED_SCOPE_FILES:
            continue
        violations.append(
            (
                path,
                line,
                (
                    "ConsoleCaptureCoordinator.Semaphore must remain encapsulated inside "
                    "ConsoleCaptureCoordinator.RunAsync."
                ),
            )
        )

    scope_pattern = r"new\s+Console(?:Capture|Redirection)Scope"
    for path, line in run_rg(scope_pattern):
        resolved = path.resolve()
        if resolved in ALLOWED_SCOPE_FILES:
            continue
        violations.append(
            (
                path,
                line,
                (
                    "Use ConsoleTestUtilities.WithConsoleCaptureAsync/WithConsoleRedirectionAsync "
                    "instead of instantiating console scopes directly."
                ),
            )
        )

    if violations:
        grouped: dict[str, list[Tuple[str, str]]] = {}
        for path, line, message in violations:
            grouped.setdefault(message, []).append((path.as_posix(), line))

        for message, rows in grouped.items():
            print(f"{message}\n")
            for path_str, entry in rows:
                print(f"{path_str}:{entry}")
            print()
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

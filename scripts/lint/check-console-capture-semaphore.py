#!/usr/bin/env python3
"""Fails if tests bypass the console capture helpers."""

from __future__ import annotations

import pathlib
import re
import sys
from typing import Iterable, Tuple

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
TEST_ROOT = REPO_ROOT / "src" / "tests"

INFRA_DIR = (
    TEST_ROOT / "WallstopStudios.NovaSharp.Interpreter.Tests.TUnit" / "TestInfrastructure"
).resolve()
ALLOWED_SCOPE_FILES = {
    INFRA_DIR / "ConsoleCaptureCoordinator.cs",
    INFRA_DIR / "ConsoleTestUtilities.cs",
    INFRA_DIR / "ConsoleCaptureScope.cs",
    INFRA_DIR / "ConsoleRedirectionScope.cs",
}


def search_pattern(pattern: str) -> Iterable[Tuple[pathlib.Path, str]]:
    """Search for pattern in .cs files under TEST_ROOT using pure Python."""
    compiled = re.compile(pattern)
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
            if compiled.search(line):
                yield filepath, f"{line_num}:{line.strip()}"


def main() -> int:
    violations: list[Tuple[pathlib.Path, str, str]] = []

    semaphore_pattern = r"ConsoleCaptureCoordinator\.Semaphore"
    for path, line in search_pattern(semaphore_pattern):
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
    for path, line in search_pattern(scope_pattern):
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

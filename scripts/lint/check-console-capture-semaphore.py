#!/usr/bin/env python3
"""Fails if tests bypass the console capture helpers."""

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
                # entry is "line_num:content", extract line number
                line_num_str = entry.split(":")[0]
                line_num = int(line_num_str) if line_num_str.isdigit() else 1
                emit_error(path_str, line_num, message)
            print()
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

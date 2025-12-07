#!/usr/bin/env python3
"""
Fails if any file outside the approved infrastructure helpers references
PlatformAutoDetector.TestHooks directly. Helps enforce the disposable-scope
pattern tracked in PLAN.md.
"""

from __future__ import annotations

import pathlib
import subprocess
import sys
from typing import Iterable, Set


REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
ALLOWED_PATHS: Set[pathlib.Path] = {
    pathlib.Path("AGENTS.md"),
    pathlib.Path("docs/Testing.md"),
    pathlib.Path("PLAN.md"),
    pathlib.Path("scripts/ci/README.md"),
    pathlib.Path("scripts/lint/check-platform-testhooks.py"),
    pathlib.Path("scripts/lint/README.md"),
    pathlib.Path("src/tests/TestInfrastructure/Scopes/PlatformDetectorIsolationScope.cs"),
    pathlib.Path("src/tests/TestInfrastructure/Scopes/PlatformDetectorScopes.cs"),
    pathlib.Path("src/tests/TestInfrastructure/Scopes/PlatformDetectorScope.cs"),
    pathlib.Path("src/tests/TestInfrastructure/TUnit/PlatformDetectorIsolationAttribute.cs"),
}


def get_matches() -> Iterable[tuple[pathlib.Path, str]]:
    """Yields (path, line) tuples for raw TestHooks usage."""
    try:
        result = subprocess.run(
            [
                "rg",
                "--with-filename",
                "--line-number",
                r"PlatformAutoDetector\.TestHooks",
            ],
            cwd=REPO_ROOT,
            check=False,
            capture_output=True,
            text=True,
        )
    except FileNotFoundError as exc:  # pragma: no cover
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
    violations = []
    for path, line in get_matches():
        normalized = path.as_posix()
        if path in ALLOWED_PATHS:
            continue
        violations.append(f"{normalized}:{line}")

    if violations:
        print("PlatformAutoDetector.TestHooks must go through the shared scopes.\n")
        for entry in violations:
            print(entry)
        print(
            "\nUpdate the offending tests to use PlatformDetectorOverrideScope/PlatformDetectorScope, "
            "or add new infrastructure helpers to this allowlist.",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

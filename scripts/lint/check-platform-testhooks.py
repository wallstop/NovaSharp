#!/usr/bin/env python3
"""
Fails if any file outside the approved infrastructure helpers references
PlatformAutoDetector.TestHooks directly. Helps enforce the disposable-scope
pattern tracked in PLAN.md.
"""

from __future__ import annotations

import pathlib
import re
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

# File extensions to search
SEARCH_EXTENSIONS = {".cs", ".md", ".py", ".sh"}

# Pattern to match
PATTERN = re.compile(r"PlatformAutoDetector\.TestHooks")


def get_matches() -> Iterable[tuple[pathlib.Path, str]]:
    """Yields (relative_path, line_info) tuples for raw TestHooks usage."""
    for ext in SEARCH_EXTENSIONS:
        for filepath in REPO_ROOT.rglob(f"*{ext}"):
            # Skip hidden directories and common non-source directories
            parts = filepath.relative_to(REPO_ROOT).parts
            if any(p.startswith(".") or p in ("bin", "obj", "node_modules") for p in parts):
                continue

            try:
                content = filepath.read_text(encoding="utf-8", errors="ignore")
            except (OSError, UnicodeDecodeError):
                continue

            for line_num, line in enumerate(content.splitlines(), start=1):
                if PATTERN.search(line):
                    rel_path = filepath.relative_to(REPO_ROOT)
                    yield rel_path, f"{line_num}:{line.strip()}"


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

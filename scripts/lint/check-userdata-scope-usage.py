#!/usr/bin/env python3
"""
Fails when tests register/unregister userdata types outside the approved isolation suites.
"""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path


def is_ci() -> bool:
    return os.environ.get("GITHUB_ACTIONS") == "true" or os.environ.get("CI") == "true"


def emit_error(file_path: str, message: str) -> None:
    if is_ci():
        print(f"::error file={file_path}::{message}")
    print(f"{file_path}: {message}", file=sys.stderr)

REPO_ROOT = Path(__file__).resolve().parents[2]
TUNIT_ROOT = REPO_ROOT / "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit"
ALLOW = {
    "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/UserDataTUnitTests.cs",
    "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/UserDataIsolationTUnitTests.cs",
    "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tooling/HardwireGeneratorTUnitTests.cs",
}
PATTERN = re.compile(r"UserData\.(?:RegisterType|UnregisterType)\s*\(")


def scan_file(path: Path) -> bool:
    text = path.read_text(encoding="utf-8", errors="ignore")
    return bool(PATTERN.search(text))


def main() -> int:
    violations: list[Path] = []
    for path in TUNIT_ROOT.rglob("*.cs"):
        relative = path.relative_to(REPO_ROOT).as_posix()
        if relative in ALLOW:
            continue
        if scan_file(path):
            violations.append(path)

    if violations:
        sys.stderr.write(
            "Tests must use UserDataRegistrationScope rather than calling "
            "UserData.RegisterType/UserData.UnregisterType directly.\n",
        )
        for path in violations:
            relative = path.relative_to(REPO_ROOT).as_posix()
            emit_error(relative, "Uses UserData.RegisterType/UnregisterType directly")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

#!/usr/bin/env python3
"""
Fails when tests register/unregister userdata types outside the approved isolation suites.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

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
            sys.stderr.write(f"- {path.relative_to(REPO_ROOT)}\n")
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

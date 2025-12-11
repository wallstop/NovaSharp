#!/usr/bin/env python3
"""
Namespace audit helper.

Scans all C# source files under `src/` (excluding generated/binary folders) and
reports files whose declared namespace does not match the directory layout.
The report is emitted as plain text so it can be reviewed and eventually wired
into CI once the outstanding mismatches are resolved.
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]  # repo root
SRC_ROOT = ROOT / "src"
EXCLUDED_DIRS = {"bin", "obj", "packages", ".vs"}
CATEGORY_ROOTS = {"runtime", "tooling", "tests", "debuggers", "samples"}
IGNORED_PARTS = {"properties", "tests", "testcases", "benchmarks", "tutorial", "processor"}
PATH_ALLOWLIST = {
    Path("src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/_Hardwired.cs"),
    Path("src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/SDK/IsExternalInit.cs"),
    Path(
        "src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/System/Runtime/CompilerServices/IsExternalInit.cs"
    ),
    Path("src/runtime/WallstopStudios.NovaSharp.Interpreter/Compatibility/Attributes.cs"),
    Path("src/runtime/WallstopStudios.NovaSharp.Interpreter/Compatibility/Stopwatch.cs"),
}
NAMESPACE_PATTERN = re.compile(r"^\s*namespace\s+([A-Za-z0-9_.]+)")


def discover_cs_files() -> list[Path]:
    files: list[Path] = []
    for path in SRC_ROOT.rglob("*.cs"):
        if any(part in EXCLUDED_DIRS for part in path.parts):
            continue
        files.append(path)
    return files


def extract_namespace(path: Path) -> str | None:
    try:
        with path.open("r", encoding="utf-8-sig") as handle:
            for line in handle:
                match = NAMESPACE_PATTERN.match(line)
                if match:
                    return match.group(1)
    except UnicodeDecodeError:
        return None
    return None


def expected_namespace(path: Path) -> str | None:
    try:
        rel = path.relative_to(SRC_ROOT)
    except ValueError:
        return None

    if rel.parts[0] == "legacy":
        # Legacy assets are quarantined and not expected to align with current namespaces.
        return None

    # Drop the filename
    parts = list(rel.parts[:-1])
    if not parts:
        return None

    # Strip top-level category folders (runtime/tooling/tests/debuggers/samples/docs)
    if parts and parts[0].lower() in CATEGORY_ROOTS:
        parts = parts[1:]
        if not parts:
            return None

    # Shared test infrastructure is branded under WallstopStudios.NovaSharp.Tests even though the
    # folder path does not include the namespace prefix. Normalize that here so the audit enforces
    # the branded namespace instead of the raw folder name.
    if parts and parts[0] == "TestInfrastructure":
        filtered = [p for p in parts if p.lower() not in IGNORED_PARTS]
        if not filtered:
            filtered = parts
        return "WallstopStudios.NovaSharp.Tests." + ".".join(
            p.replace(" ", "") for p in filtered
        )

    # Remove folder tokens that are not part of the namespace (e.g., Properties)
    filtered = [p for p in parts if p.lower() not in IGNORED_PARTS]
    if not filtered:
        filtered = parts

    return ".".join(p.replace(" ", "") for p in filtered)


def audit() -> int:
    mismatches: list[tuple[Path, str | None, str | None]] = []
    for cs_file in discover_cs_files():
        rel_path = cs_file.relative_to(ROOT)

        if rel_path in PATH_ALLOWLIST:
            continue

        actual = extract_namespace(cs_file)
        expected = expected_namespace(cs_file)
        if actual is None or expected is None:
            continue
        if actual != expected:
            mismatches.append((rel_path, expected, actual))

    if not mismatches:
        print("All namespaces align with directory layout.")
        return 0

    print("Namespace mismatches detected:")
    for path, expected, actual in mismatches:
        print(f"- {path}: expected '{expected}' but found '{actual}'")
    print(f"\nTotal mismatches: {len(mismatches)}")
    return 1


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Audit namespace layout.")
    parser.parse_args(argv)
    return audit()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

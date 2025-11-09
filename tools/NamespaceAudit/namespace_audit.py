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

    # Remove folder tokens that are not part of the namespace (e.g., Properties)
    filtered = [p for p in parts if p.lower() not in {"properties", "tests", "testcases"}]
    if not filtered:
        filtered = parts

    return ".".join(p.replace(" ", "") for p in filtered)


def audit() -> int:
    mismatches: list[tuple[Path, str | None, str | None]] = []
    for cs_file in discover_cs_files():
        actual = extract_namespace(cs_file)
        expected = expected_namespace(cs_file)
        if actual is None or expected is None:
            continue
        if actual != expected:
            mismatches.append((cs_file.relative_to(ROOT), expected, actual))

    if not mismatches:
        print("✓ All namespaces align with directory layout.")
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

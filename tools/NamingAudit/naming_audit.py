#!/usr/bin/env python3
"""
Naming audit helper.

Scans C# source files under `src/` and reports file/type identifiers that do not
follow the repository's PascalCase expectations (per .editorconfig / C# style).
The goal is to provide actionable guidance ahead of the full naming sweep that
Milestone F calls out.
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC_ROOT = ROOT / "src"
EXCLUDED_DIRS = {"bin", "obj", "packages", ".vs", "legacy"}
FILE_ALLOWLIST = {
    Path("src/tests/NovaSharp.Interpreter.Tests/_Hardwired.cs"),
    Path("src/tests/NovaSharp.Interpreter.Tests/EmbeddableNUnitWrapper.cs"),
    Path("src/runtime/NovaSharp.Interpreter/Compatibility/Attributes.cs"),
    Path("src/runtime/NovaSharp.Interpreter/Compatibility/Stopwatch.cs"),
}
TYPE_ALLOWLIST = {
    "ILua",
    "Lua52CompatibilityHelper",
    "Lua53CompatibilityHelper",
    "CLRFunctionDelegate",
}
TYPE_PATTERN = re.compile(
    r"^\s*(?:public|internal|protected|private)?\s*(?:static\s+|sealed\s+|abstract\s+|partial\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z0-9_<>]+)"
)


def discover_cs_files() -> list[Path]:
    files: list[Path] = []
    for path in SRC_ROOT.rglob("*.cs"):
        if any(part.lower() in EXCLUDED_DIRS for part in path.parts):
            continue
        files.append(path)
    return files


def is_pascal_case(name: str) -> bool:
    return bool(re.fullmatch(r"[A-Z][A-Za-z0-9]*", name))


def normalized_type_name(name: str) -> str:
    if "<" in name:
        name = name.split("<", 1)[0]
    return name.strip()


@dataclass
class NamingIssue:
    path: Path
    kind: str
    identifier: str
    message: str


def audit_file(path: Path) -> list[NamingIssue]:
    issues: list[NamingIssue] = []
    rel_path = path.relative_to(ROOT)

    if rel_path not in FILE_ALLOWLIST:
        stem = path.stem
        if not (stem.startswith("_") or is_pascal_case(stem)):
            issues.append(
                NamingIssue(
                    rel_path,
                    "file",
                    stem,
                    "File names should be PascalCase (per .editorconfig).",
                )
            )

    try:
        with path.open("r", encoding="utf-8-sig") as handle:
            for line_number, line in enumerate(handle, start=1):
                match = TYPE_PATTERN.match(line)
                if not match:
                    continue
                type_name = normalized_type_name(match.group(2))
                if (
                    type_name
                    and type_name not in TYPE_ALLOWLIST
                    and not is_pascal_case(type_name.lstrip("_"))
                ):
                    issues.append(
                        NamingIssue(
                            rel_path,
                            match.group(1),
                            type_name,
                            f"Type '{type_name}' should be PascalCase "
                            f"(line {line_number}).",
                        )
                    )
    except UnicodeDecodeError:
        issues.append(
            NamingIssue(
                rel_path,
                "file",
                rel_path.name,
                "Unable to decode file using UTF-8; skipping type analysis.",
            )
        )

    return issues


def audit() -> int:
    all_issues: list[NamingIssue] = []
    for cs_file in discover_cs_files():
        all_issues.extend(audit_file(cs_file))

    if not all_issues:
        print("✓ All inspected files/types follow PascalCase expectations.")
        return 0

    print("Naming inconsistencies detected (per .editorconfig/C# style):\n")
    for issue in all_issues:
        print(f"- {issue.path}: {issue.message}")
    print(f"\nTotal issues: {len(all_issues)}")
    return 1


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Audit naming conventions.")
    parser.parse_args(argv)
    return audit()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

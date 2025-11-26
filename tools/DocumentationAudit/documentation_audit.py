#!/usr/bin/env python3
"""
XML documentation audit helper.

Scans the C# source tree and reports public/internal type declarations that do
not carry `///` XML documentation comments. The goal is to launch the PLAN's
documentation quality pass by providing actionable guidance before we wire the
check into CI.
"""

from __future__ import annotations

import argparse
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
import re
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
SRC_ROOT = ROOT / "src"
DOC_PATTERN = re.compile(r"^\s*///")
TYPE_PATTERN = re.compile(
    r"^\s*(?:public|internal)\s+"
    r"(?:static\s+|sealed\s+|abstract\s+|partial\s+|unsafe\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_<>]*)"
)
METHOD_PATTERN = re.compile(
    r"^\s*(?:public|internal)\s+"
    r"(?:static\s+|virtual\s+|override\s+|sealed\s+|abstract\s+|partial\s+|async\s+|extern\s+|unsafe\s+|new\s+)*"
    r"[A-Za-z0-9_<>,\[\].?\s]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("
)
PROPERTY_PATTERN = re.compile(
    r"^\s*(?:public|internal)\s+"
    r"(?:static\s+|virtual\s+|override\s+|sealed\s+|abstract\s+|partial\s+|extern\s+|unsafe\s+|new\s+)*"
    r"[A-Za-z0-9_<>,\[\].?\s]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|=>)"
)

MEMBER_NAME_ALLOWLIST = {"operator"}
SKIP_PATH_PREFIXES = {
    Path("src/tests"),
    Path("src/samples"),
}
SKIP_DIR_NAMES = {"bin", "obj", ".vs", "packages"}
SKIP_CONTAINS = {"LuaPort", "LuaStateInterop", "KopiLua"}
FILE_ALLOWLIST = {
    Path("src/runtime/NovaSharp.Interpreter/Compatibility/Attributes.cs"),
}
TYPE_ALLOWLIST = {
    "Lua52CompatibilityHelper",
    "Lua53CompatibilityHelper",
}
MEMBER_ALLOWLIST: dict[Path, set[str]] = {}


@dataclass
class DocIssue:
    path: Path
    line: int
    declaration: str
    kind: str


def discover_files() -> list[Path]:
    files: list[Path] = []
    for path in SRC_ROOT.rglob("*.cs"):
        rel_path = path.relative_to(ROOT)
        if should_skip(rel_path):
            continue
        files.append(path)
    return sorted(files)


def should_skip(rel_path: Path) -> bool:
    for prefix in SKIP_PATH_PREFIXES:
        try:
            rel_path.relative_to(prefix)
            return True
        except ValueError:
            continue
    if rel_path in FILE_ALLOWLIST:
        return True
    if rel_path.name.endswith(".g.cs") or rel_path.name.endswith(".generated.cs"):
        return True
    lowered_parts = {part.lower() for part in rel_path.parts}
    if lowered_parts & SKIP_DIR_NAMES:
        return True
    for token in SKIP_CONTAINS:
        if token in rel_path.parts:
            return True
    return False


def is_allowlisted(rel_path: Path, identifier: str, allowlist: dict[Path, set[str]]) -> bool:
    return identifier in allowlist.get(rel_path, set())


def normalize_type_name(name: str) -> str:
    if "<" in name:
        name = name.split("<", 1)[0]
    return name


def audit_file(path: Path) -> list[DocIssue]:
    rel_path = path.relative_to(ROOT)
    issues: list[DocIssue] = []
    pending_doc = False
    attribute_depth = 0

    with path.open("r", encoding="utf-8-sig") as handle:
        for line_number, raw_line in enumerate(handle, start=1):
            stripped = raw_line.strip()

            if DOC_PATTERN.match(raw_line):
                pending_doc = True
                continue

            if pending_doc and attribute_depth > 0:
                attribute_depth += raw_line.count("[") - raw_line.count("]")
                if attribute_depth < 0:
                    attribute_depth = 0
                continue

            if pending_doc and stripped.startswith("["):
                attribute_depth = raw_line.count("[") - raw_line.count("]")
                if attribute_depth < 0:
                    attribute_depth = 0
                if attribute_depth == 0:
                    attribute_depth = 0
                continue

            if not stripped:
                if pending_doc:
                    continue
                pending_doc = False
                attribute_depth = 0
                continue

            type_match = TYPE_PATTERN.match(raw_line)
            if type_match:
                decl_kind = type_match.group(1)
                type_name = normalize_type_name(type_match.group(2))
                if not pending_doc and type_name not in TYPE_ALLOWLIST:
                    issues.append(
                        DocIssue(
                            rel_path,
                            line_number,
                            f"{decl_kind} {type_name}",
                            "type",
                        )
                    )
                pending_doc = False
                attribute_depth = 0
                continue

            method_match = METHOD_PATTERN.match(raw_line)
            if method_match:
                method_name = method_match.group(1)
                if (
                    not pending_doc
                    and method_name not in MEMBER_NAME_ALLOWLIST
                    and not is_allowlisted(rel_path, method_name, MEMBER_ALLOWLIST)
                ):
                    issues.append(
                        DocIssue(
                            rel_path,
                            line_number,
                            f"{method_name}()",
                            "method",
                        )
                    )
                pending_doc = False
                attribute_depth = 0
                continue

            property_match = PROPERTY_PATTERN.match(raw_line)
            if property_match:
                property_name = property_match.group(1)
                if (
                    not pending_doc
                    and property_name not in MEMBER_NAME_ALLOWLIST
                    and not is_allowlisted(rel_path, property_name, MEMBER_ALLOWLIST)
                ):
                    issues.append(
                        DocIssue(
                            rel_path,
                            line_number,
                            property_name,
                            "property",
                        )
                    )
                pending_doc = False
                attribute_depth = 0
                continue

            pending_doc = False
            attribute_depth = 0

    return issues


def group_issues(issues: Iterable[DocIssue]) -> dict[Path, list[DocIssue]]:
    grouped: dict[Path, list[DocIssue]] = defaultdict(list)
    for issue in issues:
        grouped[issue.path].append(issue)
    return grouped


def format_report(issues: list[DocIssue]) -> str:
    lines: list[str] = []
    lines.append("# Documentation Audit Report")
    lines.append("")
    lines.append("_Generated via tools/DocumentationAudit/documentation_audit.py_")
    lines.append("")
    lines.append(f"Found {len(issues)} undocumented public/internal type declarations.")
    lines.append("")

    if not issues:
        lines.append("All inspected type declarations include XML documentation.")
        return "\n".join(lines)

    grouped = group_issues(issues)
    for path in sorted(grouped):
        lines.append(f"- {path.as_posix()}")
        for issue in grouped[path]:
            lines.append(
                f"  - line {issue.line}: missing XML docs for `{issue.declaration}`"
            )
    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Audit XML documentation comments on public/internal type declarations."
    )
    parser.add_argument(
        "--write-log",
        type=Path,
        help="Optional path to write the audit report.",
    )
    parser.add_argument(
        "--fail-on-issues",
        action="store_true",
        help="Return a non-zero exit code when undocumented members are found.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    files = discover_files()
    issues: list[DocIssue] = []
    for file_path in files:
        issues.extend(audit_file(file_path))

    report = format_report(issues)
    print(report)

    if args.write_log:
        args.write_log.write_text(report + "\n", encoding="utf-8")

    if args.fail_on_issues and issues:
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

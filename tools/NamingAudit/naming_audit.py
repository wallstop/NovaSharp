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
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

ROOT = Path(__file__).resolve().parents[2]
SRC_ROOT = ROOT / "src"
PROPS_PATH = ROOT / "Directory.Build.props"
EXCLUDED_DIRS = {"bin", "obj", "packages", ".vs", "legacy"}
FILE_ALLOWLIST = {
    Path("src/tests/NovaSharp.Interpreter.Tests/_Hardwired.cs"),
    Path("src/tests/NovaSharp.Interpreter.Tests/EmbeddableNUnitWrapper.cs"),
    Path("src/runtime/NovaSharp.Interpreter/Compatibility/Attributes.cs"),
    Path("src/runtime/NovaSharp.Interpreter/Compatibility/Stopwatch.cs"),
    Path("src/runtime/NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs"),
}
FIELD_ALLOWLIST: dict[Path, set[str]] = {
    # Lua-facing DTOs keep lowercase fields so script fixtures can reference the
    # legacy Lua-port surface without additional indirection.
    Path(
        "src/tests/NovaSharp.Interpreter.Tests/EndToEnd/CollectionsBaseGenRegisteredTests.cs"
    ): {"value"},
    Path(
        "src/tests/NovaSharp.Interpreter.Tests/EndToEnd/CollectionsBaseInterfGenRegisteredTests.cs"
    ): {"value"},
    Path(
        "src/tests/NovaSharp.Interpreter.Tests/EndToEnd/CollectionsRegisteredTests.cs"
    ): {"value"},
    Path("src/tests/NovaSharp.Interpreter.Tests/EndToEnd/UserDataFieldsTests.cs"): {
        "intProp",
        "CONST_INT_PROP",
        "roIntProp",
        "nIntProp",
        "objProp",
    },
    Path("src/tests/NovaSharp.Interpreter.Tests/EndToEnd/VtUserDataFieldsTests.cs"): {
        "intProp",
        "CONST_INT_PROP",
        "nIntProp",
        "objProp",
    },
    Path(
        "src/tests/NovaSharp.Interpreter.Tests/EndToEnd/StructAssignmentTechnique.cs"
    ): {"x", "y", "z", "position"},
    # Lua interop shims mirror the original KopiLua/KeraLua identifiers so
    # native parity (docs, tutorials, upstream diffs) stays intact.
    Path("src/runtime/NovaSharp.Interpreter/LuaPort/LuaStateInterop/CharPtr.cs"): {
        "chars",
        "index",
    },
    Path("src/runtime/NovaSharp.Interpreter/LuaPort/LuaStateInterop/LuaBase.cs"): {
        "LUA_TNONE",
        "LUA_TNIL",
        "LUA_TBOOLEAN",
        "LUA_TLIGHTUSERDATA",
        "LUA_TNUMBER",
        "LUA_TSTRING",
        "LUA_TTABLE",
        "LUA_TFUNCTION",
        "LUA_TUSERDATA",
        "LUA_TTHREAD",
        "LUA_MULTRET",
        "LUA_INTFRMLEN",
    },
}
TYPE_ALLOWLIST = {
    "ILua",
    "Lua52CompatibilityHelper",
    "Lua53CompatibilityHelper",
    "CLRFunctionDelegate",
}
MEMBER_ALLOWLIST = {
    "op_Implicit",
    "op_Explicit",
    "op_Addition",
    "op_Subtraction",
    "op_Multiply",
    "op_Division",
    "op_Modulus",
    "op_Equality",
    "op_Inequality",
    "operator",
}
MEMBER_ALLOWLIST_BY_PATH: dict[Path, set[str]] = {
    Path("src/runtime/NovaSharp.Interpreter/CoreLib/IoModule.cs"): {"__index_callback"},
    Path("src/runtime/NovaSharp.Interpreter/LuaPort/LuaStateInterop/LuaBase.cs"): {"LUA_QL"},
}
TYPE_PATTERN = re.compile(
    r"^\s*(?:public|internal|protected|private)?\s*(?:static\s+|sealed\s+|abstract\s+|partial\s+)*"
    r"(class|struct|interface|enum|record)\s+([A-Za-z0-9_<>]+)"
)
METHOD_PATTERN = re.compile(
    r"^\s*(public|protected|internal|private)\s+(?:static\s+|virtual\s+|override\s+|sealed\s+|async\s+|extern\s+|unsafe\s+|partial\s+|new\s+)*"
    r"[A-Za-z0-9_<>,\[\].?]+\s+([A-Za-z0-9_]+)\s*\("
)
PROPERTY_PATTERN = re.compile(
    r"^\s*(public|protected|internal|private)\s+(?:static\s+|virtual\s+|override\s+|sealed\s+|unsafe\s+|new\s+)*"
    r"[A-Za-z0-9_<>,\[\].?]+\s+([A-Za-z0-9_]+)\s*\{"
)
FIELD_PATTERN = re.compile(
    r"^\s*(public|protected|internal)\s+(?:static\s+|readonly\s+|volatile\s+|const\s+|unsafe\s+|new\s+)*"
    r"[A-Za-z0-9_<>,\[\].?]+\s+([A-Za-z0-9_]+)\s*(?:=|;)"
)
FIELD_NAME_ALLOWLIST = {"operator"}
PRIVATE_FIELD_PATTERN = re.compile(
    r"^\s*private\s+(?P<modifiers>(?:static\s+|readonly\s+|volatile\s+|const\s+|unsafe\s+|new\s+)*)"
    r"[A-Za-z0-9_<>,\[\].?]+\s+(?P<name>[A-Za-z0-9_]+)\s*(?:=|;)"
)
NAMESPACE_PATTERN = re.compile(r"^\s*namespace\s+([A-Za-z0-9_.]+)")


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


def is_private_field_style(name: str) -> bool:
    return bool(re.fullmatch(r"_[a-z][A-Za-z0-9]*", name))


def is_allowlisted(
    rel_path: Path, identifier: str, allowlist: dict[Path, set[str]]
) -> bool:
    return identifier in allowlist.get(rel_path, set())


@dataclass
class NamingIssue:
    path: Path
    kind: str
    identifier: str
    message: str


def audit_file(
    path: Path, enforce_namespace_prefix: str | None = None
) -> tuple[list[NamingIssue], list[str]]:
    issues: list[NamingIssue] = []
    namespaces: list[str] = []
    rel_path = path.relative_to(ROOT)

    if rel_path in FILE_ALLOWLIST:
        return issues, namespaces

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

    current_type = None

    try:
        with path.open("r", encoding="utf-8-sig") as handle:
            for line_number, line in enumerate(handle, start=1):
                namespace_match = NAMESPACE_PATTERN.match(line)
                if namespace_match:
                    namespace_name = namespace_match.group(1).strip()
                    if namespace_name:
                        namespaces.append(namespace_name)
                        if enforce_namespace_prefix and not namespace_name.startswith(
                            enforce_namespace_prefix
                        ):
                            issues.append(
                                NamingIssue(
                                    rel_path,
                                    "namespace",
                                    namespace_name,
                                    f"Namespace '{namespace_name}' should start with "
                                    f"'{enforce_namespace_prefix}' (line {line_number}).",
                                )
                            )
                    continue

                type_match = TYPE_PATTERN.match(line)
                if type_match:
                    type_name = normalized_type_name(type_match.group(2))
                    if (
                        type_name
                        and type_name not in TYPE_ALLOWLIST
                        and not is_pascal_case(type_name.lstrip("_"))
                    ):
                        issues.append(
                            NamingIssue(
                                rel_path,
                                type_match.group(1),
                                type_name,
                                f"Type '{type_name}' should be PascalCase "
                                f"(line {line_number}).",
                            )
                        )
                    current_type = type_name
                    continue

                method_match = METHOD_PATTERN.match(line)
                if method_match:
                    method_name = method_match.group(2)
                    if (
                        method_name
                        and method_name not in MEMBER_ALLOWLIST
                        and not is_allowlisted(rel_path, method_name, MEMBER_ALLOWLIST_BY_PATH)
                        and method_name != current_type
                        and not is_pascal_case(method_name)
                    ):
                        issues.append(
                            NamingIssue(
                                rel_path,
                                "method",
                                method_name,
                                f"Method '{method_name}' should be PascalCase "
                                f"(line {line_number}).",
                            )
                        )
                    continue

                property_match = PROPERTY_PATTERN.match(line)
                if property_match:
                    property_name = property_match.group(2)
                    if (
                        property_name
                        and property_name not in MEMBER_ALLOWLIST
                        and not is_allowlisted(
                            rel_path, property_name, MEMBER_ALLOWLIST_BY_PATH
                        )
                        and not is_pascal_case(property_name)
                    ):
                        issues.append(
                            NamingIssue(
                                rel_path,
                                "property",
                                property_name,
                                f"Property '{property_name}' should be PascalCase "
                                f"(line {line_number}).",
                            )
                        )
                    continue

                field_match = FIELD_PATTERN.match(line)
                if field_match:
                    field_name = field_match.group(2)
                    if (
                        field_name
                        and field_name not in FIELD_NAME_ALLOWLIST
                        and not is_allowlisted(rel_path, field_name, FIELD_ALLOWLIST)
                    ):
                        if not is_pascal_case(field_name):
                            issues.append(
                                NamingIssue(
                                    rel_path,
                                    "field",
                                    field_name,
                                    f"Field '{field_name}' should be PascalCase "
                                    f"(line {line_number}).",
                                )
                            )
                    continue

                private_field_match = PRIVATE_FIELD_PATTERN.match(line)
                if private_field_match:
                    field_name = private_field_match.group("name")
                    modifiers = (private_field_match.group("modifiers") or "").split()
                    is_const = "const" in modifiers
                    is_static = "static" in modifiers
                    if field_name:
                        if is_const or is_static:
                            if not is_pascal_case(field_name):
                                label = "Const" if is_const else "Static"
                                issues.append(
                                    NamingIssue(
                                        rel_path,
                                        "field",
                                        field_name,
                                        f"{label} field '{field_name}' should be PascalCase "
                                        f"(line {line_number}).",
                                    )
                                )
                        elif not is_private_field_style(field_name):
                            issues.append(
                                NamingIssue(
                                    rel_path,
                                    "field",
                                    field_name,
                                    f"Field '{field_name}' should be _camelCase "
                                    f"(line {line_number}).",
                                )
                            )
                    continue
    except UnicodeDecodeError:
        issues.append(
            NamingIssue(
                rel_path,
                "file",
                rel_path.name,
                "Unable to decode file using UTF-8; skipping type analysis.",
            )
        )

    return issues, namespaces


def audit(
    enforce_namespace_prefix: str | None = None,
) -> tuple[list[NamingIssue], list[str]]:
    all_issues: list[NamingIssue] = []
    namespaces: list[str] = []
    for cs_file in discover_cs_files():
        file_issues, file_namespaces = audit_file(cs_file, enforce_namespace_prefix)
        all_issues.extend(file_issues)
        namespaces.extend(file_namespaces)
    return all_issues, namespaces


def render_report(
    issues: list[NamingIssue], namespace_summary_lines: list[str] | None = None
) -> str:
    lines: list[str] = [
        "# Naming Audit Report",
        "",
        "_Generated via tools/NamingAudit/naming_audit.py_",
        "",
    ]

    if not issues:
        lines.append("All inspected files/types follow PascalCase expectations.")
    else:
        lines.append("Naming inconsistencies detected (per .editorconfig/C# style):")
        lines.append("")
        for issue in issues:
            lines.append(f"- {issue.path}: {issue.message}")
        lines.append("")
        lines.append(f"Total issues: {len(issues)}")

    if namespace_summary_lines:
        lines.append("")
        lines.extend(namespace_summary_lines)

    return "\n".join(lines)


def load_namespace_prefixes_from_props() -> list[str]:
    if not PROPS_PATH.exists():
        return []

    try:
        tree = ET.parse(PROPS_PATH)
    except (ET.ParseError, OSError):
        return []

    root = tree.getroot()
    if root is None:
        return []

    property_order = ["TargetNamespacePrefix", "LegacyNamespacePrefix"]
    prefixes: list[str] = []
    property_values: dict[str, str] = {}

    for element in root.iter():
        tag = element.tag.split("}", 1)[-1] if "}" in element.tag else element.tag
        if tag in property_order and element.text:
            property_values[tag] = element.text.strip()

    for prop in property_order:
        value = property_values.get(prop, "")
        if value and value not in prefixes:
            prefixes.append(value)

    return prefixes


def build_namespace_summary(
    namespaces: Iterable[str], prefixes: list[str]
) -> list[str]:
    prefix_list = prefixes or []
    namespace_list = list(namespaces)
    if not namespace_list or not prefix_list:
        return []

    total = len(namespace_list)
    stats: dict[str, int] = {prefix: 0 for prefix in prefix_list}
    other = 0

    for namespace in namespace_list:
        matched = False
        for prefix in prefix_list:
            if namespace.startswith(prefix):
                stats[prefix] += 1
                matched = True
                break
        if not matched:
            other += 1

    summary_lines = ["## Namespace Prefix Summary", ""]
    for prefix in prefix_list:
        count = stats[prefix]
        percentage = (count / total) * 100 if total else 0
        summary_lines.append(f"- {prefix}: {count} ({percentage:.2f}% of {total})")
    percentage_other = (other / total) * 100 if total else 0
    summary_lines.append(f"- <other>: {other} ({percentage_other:.2f}% of {total})")
    summary_lines.append(f"- <total>: {total}")
    return summary_lines


def resolve_repo_path(path: Path) -> Path:
    if path.is_absolute():
        return path
    return (ROOT / path).resolve()


def write_log(path: Path, report: str) -> None:
    resolved = resolve_repo_path(path)
    resolved.parent.mkdir(parents=True, exist_ok=True)
    resolved.write_text(report, encoding="utf-8")


def verify_log(path: Path, report: str) -> bool:
    resolved = resolve_repo_path(path)
    if not resolved.exists():
        print(f"Naming audit log missing at {resolved}.")
        return False

    existing = resolved.read_text(encoding="utf-8")
    if existing.strip() == report.strip():
        return True

    print(
        f"Naming audit log at {resolved} is outdated. Run "
        "`python tools/NamingAudit/naming_audit.py --write-log naming_audit.log` "
        "and commit the refreshed file."
    )
    return False


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Audit naming conventions.")
    parser.add_argument(
        "--write-log",
        type=Path,
        metavar="PATH",
        help="Write the audit report to the specified path (relative to repo root).",
    )
    parser.add_argument(
        "--verify-log",
        type=Path,
        metavar="PATH",
        help="Verify that the audit report at PATH matches the current results.",
    )
    parser.add_argument(
        "--namespace-prefix",
        action="append",
        default=[],
        dest="namespace_prefixes",
        metavar="PREFIX",
        help=(
            "Track namespace declarations that start with PREFIX. Specify multiple "
            "times to compare targets (e.g., --namespace-prefix WallstopStudios.NovaSharp "
            "--namespace-prefix NovaSharp)."
        ),
    )
    parser.add_argument(
        "--enforce-namespace-prefix",
        metavar="PREFIX",
        help=(
            "Emit audit findings when namespace declarations do not start with PREFIX. "
            "Use together with --namespace-prefix for migration tracking."
        ),
    )
    args = parser.parse_args(argv)

    namespace_prefixes = (
        args.namespace_prefixes
        if args.namespace_prefixes
        else load_namespace_prefixes_from_props()
    )

    issues, namespaces = audit(args.enforce_namespace_prefix)
    namespace_summary_lines = build_namespace_summary(namespaces, namespace_prefixes)
    report = render_report(issues, namespace_summary_lines if namespace_summary_lines else None)

    exit_code = 0
    if not issues:
        print("All inspected files/types follow PascalCase expectations.")
    else:
        print("Naming inconsistencies detected (per .editorconfig/C# style):\n")
        for issue in issues:
            print(f"- {issue.path}: {issue.message}")
        print(f"\nTotal issues: {len(issues)}")
        exit_code = 1

    if namespace_summary_lines:
        if issues:
            print("")
        print("\n".join(namespace_summary_lines))

    if args.write_log:
        write_log(args.write_log, report)

    if args.verify_log:
        if not verify_log(args.verify_log, report):
            exit_code = 1

    return exit_code


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

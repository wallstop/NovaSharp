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
FIELD_ALLOWLIST: dict[Path, set[str]] = {
    # Lua-facing DTOs keep lowercase fields so script fixtures can reference the
    # original MoonSharp surface without additional indirection.
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
}
MEMBER_ALLOWLIST_BY_PATH: dict[Path, set[str]] = {
    Path("src/runtime/NovaSharp.Interpreter/CoreLib/ErrorHandlingModule.cs"): {
        "pcall_onerror",
        "pcall_continuation",
    },
    Path("src/runtime/NovaSharp.Interpreter/CoreLib/IoModule.cs"): {"__index_callback"},
    Path("src/runtime/NovaSharp.Interpreter/CoreLib/LoadModule.cs"): {"__require_clr_impl"},
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
PRIVATE_FIELD_PATTERN = re.compile(
    r"^\s*private\s+(?P<modifiers>(?:static\s+|readonly\s+|volatile\s+|const\s+|unsafe\s+|new\s+)*)"
    r"[A-Za-z0-9_<>,\[\].?]+\s+(?P<name>[A-Za-z0-9_]+)\s*(?:=|;)"
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


def audit_file(path: Path) -> list[NamingIssue]:
    issues: list[NamingIssue] = []
    rel_path = path.relative_to(ROOT)

    if rel_path in FILE_ALLOWLIST:
        return issues

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
                    if field_name and not is_allowlisted(rel_path, field_name, FIELD_ALLOWLIST):
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
                    if field_name:
                        if is_const:
                            if not is_pascal_case(field_name):
                                issues.append(
                                    NamingIssue(
                                        rel_path,
                                        "field",
                                        field_name,
                                        f"Const field '{field_name}' should be PascalCase "
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

    return issues


def audit() -> int:
    all_issues: list[NamingIssue] = []
    for cs_file in discover_cs_files():
        all_issues.extend(audit_file(cs_file))

    if not all_issues:
        print("All inspected files/types follow PascalCase expectations.")
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

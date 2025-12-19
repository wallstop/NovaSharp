#!/usr/bin/env python3
"""
LuaNumber Usage Lint Script (§8.37 Phase 5)

Detects potentially problematic patterns where raw C# numeric types (double, float)
are used instead of LuaNumber for Lua math operations. This can cause:

1. Precision loss: Values beyond 2^53 cannot be exactly represented as doubles
2. Type coercion errors: Integer vs float subtype distinction lost (critical for Lua 5.3+)
3. Overflow/underflow bugs: Silent wrapping or unexpected behavior

Usage:
    python3 scripts/lint/check-luanumber-usage.py [--detailed] [--fail-on-issues]

Returns:
    Exit code 0: No issues found
    Exit code 1: Issues detected (when --fail-on-issues is used)
"""

import argparse
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


@dataclass
class Issue:
    """Represents a potential LuaNumber usage issue."""

    file_path: str
    line_number: int
    line_content: str
    pattern: str
    severity: str  # "warning", "error"
    description: str


# Known-safe patterns that should be excluded from warnings
SAFE_PATTERNS = [
    # Argument count retrieval (always small values)
    r"_valueStack.*\.Number.*argCount",
    r"argCount.*=.*\.Number",
    r"numArgs.*=.*\.Number",
    # Type checks (not value access)
    r"\.Type\s*==\s*DataType\.Number",
    r"IsNumber",
    r"AsNumber",  # Method call, not direct access
    # LuaNumber member access
    r"LuaNumber\.Number",
    r"\.LuaNumber\.",
    # Comments
    r"^\s*//",
    r"^\s*\*",
    # Test files often need raw access for verification
    r"\.Number\s*\)\s*\.Is",  # Assert.That(x.Number).Is...
    r"result\.Number",  # Comparing test results
    # DynValue constructors which correctly handle the conversion
    r"DynValue\.NewNumber",
    r"DynValue\.FromLuaNumber",
    r"DynValue\.FromNumber",
    # These patterns are using proper validation helpers
    r"ToLongWithValidation",
    r"ToIntegerStrict",
    # String index operations typically validated elsewhere
    r"StringRange",
    r"NormalizeRange",
    # Exit codes are inherently small
    r"exitCode",
    # Enum conversions are bounded
    r"Enum.*Number",
    # LuaPort interop code (low-level, intentional)
    r"LuaPort|LuaBase|LuaState",
    # Math.Floor after IsInteger check (intentional float handling)
    r"Math\.Floor.*arg\.Number",
    # StringModule.Rep count is clamped
    r"string\.rep|StringModule.*rep",
    # TableIterators ipairs index (always sequential integers)
    r"ipairs|Ipairs",
    # Enum descriptor (bounded by enum range)
    r"EnumUserDataDescriptor",
]

# Patterns that indicate potentially problematic code
PROBLEMATIC_PATTERNS = [
    # Direct .Number access (loses integer subtype)
    (
        r"\.Number\s*[+\-*/]",
        "warning",
        "Direct .Number arithmetic may lose integer subtype. Use LuaNumber operations.",
    ),
    (
        r"[+\-*/]\s*\.Number",
        "warning",
        "Direct .Number arithmetic may lose integer subtype. Use LuaNumber operations.",
    ),
    (
        r"\.Number\s*<\s*\.Number|\.Number\s*>\s*\.Number",
        "warning",
        "Direct .Number comparison may lose precision. Use LuaNumber.LessThan/LessThanOrEqual.",
    ),
    # Explicit casts that may lose precision
    (
        r"\(double\)\s*\w+\.Number",
        "warning",
        "Casting .Number to double is redundant and indicates possible precision loss.",
    ),
    (
        r"\(int\)\s*\w+\.Number",
        "warning",
        "Casting .Number to int may truncate large values. Use LuaNumber.AsInteger.",
    ),
    (
        r"\(long\)\s*\w+\.Number",
        "warning",
        "Casting .Number to long may have precision issues. Use LuaNumber.AsInteger.",
    ),
    # Math operations on raw doubles
    (
        r"Math\.Floor\s*\(\s*\w+\.Number",
        "warning",
        "Math.Floor on .Number may lose precision. Use LuaNumber.Floor or FloorDivide.",
    ),
    (
        r"Math\.Abs\s*\(\s*\w+\.Number",
        "warning",
        "Math.Abs on .Number may lose integer subtype. Consider LuaNumber operations.",
    ),
]

# Files/directories to skip
SKIP_PATHS = [
    "obj/",
    "bin/",
    ".git/",
    "LuaCorpusExtractor",
]

# Only check runtime code, not tests
TARGET_DIRS = [
    "src/runtime/WallstopStudios.NovaSharp.Interpreter/",
]


def is_safe_pattern(line: str, file_path: str = "") -> bool:
    """Check if the line matches a known-safe pattern."""
    # Check line patterns
    for pattern in SAFE_PATTERNS:
        if re.search(pattern, line, re.IGNORECASE):
            return True

    # Check file path patterns - files that have been audited and use intentional patterns
    file_safe_patterns = [
        r"Utf8Module\.cs",  # Bounds validated before casting
        r"LuaBase\.cs",     # Low-level LuaPort interop
        r"LuaPort",         # Low-level interop
        r"StringRange\.cs", # Documented Lua 5.1/5.2 truncation behavior
        r"TableIteratorsModule\.cs",  # ipairs uses sequential integer indices
        r"StandardEnumUserDataDescriptor\.cs",  # Enum values are bounded
        r"StringModule\.cs",  # String operations audited - Rep uses count for iteration
    ]
    for pattern in file_safe_patterns:
        if re.search(pattern, file_path, re.IGNORECASE):
            return True

    return False


def should_skip_path(file_path: str) -> bool:
    """Check if the file should be skipped."""
    for skip in SKIP_PATHS:
        if skip in file_path:
            return True
    return False


def is_in_target_dir(file_path: str, repo_root: Path) -> bool:
    """Check if the file is in a target directory."""
    rel_path = str(file_path).replace(str(repo_root) + "/", "")
    for target in TARGET_DIRS:
        if rel_path.startswith(target):
            return True
    return False


def analyze_file(file_path: Path, repo_root: Path) -> list[Issue]:
    """Analyze a single C# file for LuaNumber usage issues."""
    issues = []

    try:
        content = file_path.read_text(encoding="utf-8")
    except Exception as e:
        print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
        return issues

    lines = content.split("\n")

    for line_num, line in enumerate(lines, start=1):
        # Skip safe patterns
        if is_safe_pattern(line, str(file_path)):
            continue

        # Check for problematic patterns
        for pattern, severity, description in PROBLEMATIC_PATTERNS:
            if re.search(pattern, line):
                # Double-check it's not a false positive
                rel_path = str(file_path).replace(str(repo_root) + "/", "")
                issues.append(
                    Issue(
                        file_path=rel_path,
                        line_number=line_num,
                        line_content=line.strip(),
                        pattern=pattern,
                        severity=severity,
                        description=description,
                    )
                )
                break  # Only report one issue per line

    return issues


def find_cs_files(repo_root: Path) -> list[Path]:
    """Find all C# files in target directories."""
    files = []
    for target_dir in TARGET_DIRS:
        target_path = repo_root / target_dir
        if target_path.exists():
            for cs_file in target_path.rglob("*.cs"):
                if not should_skip_path(str(cs_file)):
                    files.append(cs_file)
    return sorted(files)


def print_report(issues: list[Issue], detailed: bool = False) -> None:
    """Print a human-readable report."""
    print("=" * 70)
    print("LuaNumber Usage Lint Report")
    print("=" * 70)
    print()

    if not issues:
        print("✅ No issues found.")
        return

    warnings = [i for i in issues if i.severity == "warning"]
    errors = [i for i in issues if i.severity == "error"]

    print(f"Issues found: {len(issues)}")
    print(f"  Warnings: {len(warnings)}")
    print(f"  Errors: {len(errors)}")
    print()

    if detailed:
        # Group by file
        by_file = {}
        for issue in issues:
            if issue.file_path not in by_file:
                by_file[issue.file_path] = []
            by_file[issue.file_path].append(issue)

        for file_path, file_issues in sorted(by_file.items()):
            print(f"\n{file_path}:")
            for issue in file_issues:
                severity_icon = "⚠️" if issue.severity == "warning" else "❌"
                print(f"  {severity_icon} Line {issue.line_number}: {issue.description}")
                print(f"      {issue.line_content[:80]}...")
        print()

    print("-" * 70)
    print("Note: Some warnings may be false positives. Review each case.")
    print("Use SAFE_PATTERNS in the script to whitelist known-safe code.")
    print("-" * 70)


def main():
    parser = argparse.ArgumentParser(
        description="Lint C# files for problematic LuaNumber usage"
    )
    parser.add_argument(
        "--detailed",
        "-d",
        action="store_true",
        help="Show detailed list of issues",
    )
    parser.add_argument(
        "--fail-on-issues",
        "-f",
        action="store_true",
        help="Exit with code 1 if any issues found",
    )
    args = parser.parse_args()

    # Find the repository root
    script_dir = Path(__file__).resolve().parent
    repo_root = script_dir.parent.parent

    # Find and analyze all C# files
    cs_files = find_cs_files(repo_root)
    all_issues = []

    for file_path in cs_files:
        issues = analyze_file(file_path, repo_root)
        all_issues.extend(issues)

    # Print report
    print_report(all_issues, detailed=args.detailed)

    # Exit code
    if args.fail_on_issues and all_issues:
        sys.exit(1)
    sys.exit(0)


if __name__ == "__main__":
    main()

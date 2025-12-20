#!/usr/bin/env python3
"""
TUnit Test Multi-Version Coverage Audit Script (§8.39)

Scans all TUnit test files to identify tests missing LuaCompatibilityVersion
[Arguments] attributes. NovaSharp supports Lua 5.1, 5.2, 5.3, 5.4, and 5.5,
and every test MUST explicitly declare which versions it targets.

Usage:
    python3 scripts/lint/check-tunit-version-coverage.py [--detailed] [--json]

Returns:
    Exit code 0: All tests have version coverage
    Exit code 1: Some tests are missing version coverage
"""

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


@dataclass
class TestMethod:
    """Represents a single test method with its version coverage."""

    name: str
    file_path: str
    line_number: int
    class_name: str
    has_version_argument: bool
    version_arguments: list[str] = field(default_factory=list)
    has_methoddatasource: bool = False
    has_combineddatasources: bool = False
    has_lua_version_helper: bool = False  # True if has [AllLuaVersions] or similar
    executes_lua: bool = False  # True if the test appears to execute Lua code
    method_body: str = ""  # The method body for detailed analysis


@dataclass
class TestFile:
    """Represents a test file with all its test methods."""

    path: str
    class_name: str
    methods: list[TestMethod] = field(default_factory=list)

    @property
    def compliant_methods(self) -> list[TestMethod]:
        return [m for m in self.methods if m.has_version_argument or m.has_methoddatasource or m.has_combineddatasources or m.has_lua_version_helper]

    @property
    def non_compliant_methods(self) -> list[TestMethod]:
        return [m for m in self.methods if not m.has_version_argument and not m.has_methoddatasource and not m.has_combineddatasources and not m.has_lua_version_helper]

    @property
    def lua_execution_tests_needing_version(self) -> list[TestMethod]:
        """Tests that execute Lua code but don't have version coverage."""
        return [m for m in self.non_compliant_methods if m.executes_lua]

    @property
    def infrastructure_tests(self) -> list[TestMethod]:
        """Tests that don't appear to execute Lua code (infrastructure tests)."""
        return [m for m in self.non_compliant_methods if not m.executes_lua]

# Lua versions that NovaSharp supports
LUA_VERSIONS = ["Lua51", "Lua52", "Lua53", "Lua54", "Lua55", "Latest"]

# Patterns to identify test methods and version arguments
TEST_ATTRIBUTE_PATTERN = re.compile(r"^\s*\[(?:global::TUnit\.Core\.)?Test\]", re.MULTILINE)
# Use re.DOTALL to match across newlines for multi-line Arguments attributes
ARGUMENTS_PATTERN = re.compile(
    r"\[(?:global::TUnit\.Core\.)?Arguments\(.*?(?:Compatibility\.)?LuaCompatibilityVersion\.(Lua\d+|Latest).*?\)\]",
    re.DOTALL
)
METHODDATASOURCE_PATTERN = re.compile(r"\[(?:global::TUnit\.Core\.)?MethodDataSource")
COMBINEDDATASOURCES_PATTERN = re.compile(r"\[(?:global::TUnit\.Core\.)?CombinedDataSources")
# New helper attribute patterns for data-driven testing
ALL_LUA_VERSIONS_PATTERN = re.compile(r"\[AllLuaVersions\]")
LUA_VERSIONS_FROM_PATTERN = re.compile(r"\[LuaVersionsFrom\(")
LUA_VERSIONS_UNTIL_PATTERN = re.compile(r"\[LuaVersionsUntil\(")
LUA_VERSION_RANGE_PATTERN = re.compile(r"\[LuaVersionRange\(")
LUA_TEST_MATRIX_PATTERN = re.compile(r"\[LuaTestMatrix\(")
METHOD_PATTERN = re.compile(r"public\s+async\s+Task\s+(\w+)\s*\(")
CLASS_PATTERN = re.compile(r"(?:public\s+)?(?:sealed\s+)?class\s+(\w+)")

# Directories containing infrastructure/fixture code that don't need version tests
EXCLUDED_DIRS = {
    "TestInfrastructure",
    "Isolation",
}

# Files that are infrastructure-only (no Lua execution tests)
EXCLUDED_PATTERNS = [
    r".*Helper.*\.cs$",
    r".*Utilities.*\.cs$",
    r".*Base.*\.cs$",
    r".*Extensions.*\.cs$",
]

# Patterns that indicate a test executes Lua code and needs version coverage
LUA_EXECUTION_PATTERNS = [
    re.compile(r'\.DoString\s*\('),
    re.compile(r'\.DoFile\s*\('),
    re.compile(r'\.RunString\s*\('),
    re.compile(r'\.DoChunk\s*\('),
    re.compile(r'new\s+Script\s*\('),
    re.compile(r'CreateScript\s*\('),
    re.compile(r'script\.Globals'),
    re.compile(r'script\.Call\s*\('),
]


def should_exclude_file(file_path: Path) -> bool:
    """Check if a file should be excluded from the audit."""
    # Check excluded directories
    for excluded_dir in EXCLUDED_DIRS:
        if excluded_dir in file_path.parts:
            return True

    # Check excluded patterns
    filename = file_path.name
    for pattern in EXCLUDED_PATTERNS:
        if re.match(pattern, filename):
            return True

    return False


def extract_class_name(content: str) -> Optional[str]:
    """Extract the class name from file content."""
    match = CLASS_PATTERN.search(content)
    return match.group(1) if match else None


def extract_method_body(lines: list[str], method_start_line: int) -> str:
    """Extract the method body from the lines, starting at method_start_line (0-indexed)."""
    # Find the opening brace
    brace_count = 0
    body_lines = []
    started = False

    for i in range(method_start_line, min(len(lines), method_start_line + 200)):
        line = lines[i]
        for char in line:
            if char == '{':
                brace_count += 1
                started = True
            elif char == '}':
                brace_count -= 1

        if started:
            body_lines.append(line)
            if brace_count == 0:
                break

    return "\n".join(body_lines)


def test_executes_lua(method_body: str) -> bool:
    """Check if a test method body appears to execute Lua code."""
    for pattern in LUA_EXECUTION_PATTERNS:
        if pattern.search(method_body):
            return True
    return False


def analyze_test_file(file_path: Path) -> Optional[TestFile]:
    """Analyze a single test file for version coverage."""
    try:
        content = file_path.read_text(encoding="utf-8")
    except Exception as e:
        print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
        return None

    class_name = extract_class_name(content) or file_path.stem

    # Find all test methods
    test_file = TestFile(path=str(file_path), class_name=class_name)

    # Split content into lines for line number tracking
    lines = content.split("\n")

    # Track context for each test method
    i = 0
    while i < len(lines):
        line = lines[i]

        # Check if this is a test attribute
        if TEST_ATTRIBUTE_PATTERN.match(line):
            # Look forward for the method signature
            method_name = None
            method_line = i + 1
            method_signature_idx = None
            for j in range(i + 1, min(i + 30, len(lines))):
                method_match = METHOD_PATTERN.search(lines[j])
                if method_match:
                    method_name = method_match.group(1)
                    method_line = j + 1  # 1-indexed line number
                    method_signature_idx = j
                    break

            if method_name and method_signature_idx is not None:
                # Extract method body and check for Lua execution
                method_body = extract_method_body(lines, method_signature_idx)
                executes_lua = test_executes_lua(method_body)

                # Check for version arguments in the context between [Test] and method signature
                # Arguments can come before [Test] (preceding) or after [Test] (between [Test] and method)
                preceding_context_start = max(0, i - 20)
                preceding_context = "\n".join(lines[preceding_context_start:i])
                following_context = "\n".join(lines[i:method_signature_idx])
                full_context = preceding_context + "\n" + following_context

                version_args = ARGUMENTS_PATTERN.findall(full_context)
                has_methoddatasource = bool(METHODDATASOURCE_PATTERN.search(full_context))
                has_combineddatasources = bool(COMBINEDDATASOURCES_PATTERN.search(full_context))
                # Check for new data-driving helper attributes
                has_lua_version_helper = (
                    bool(ALL_LUA_VERSIONS_PATTERN.search(full_context)) or
                    bool(LUA_VERSIONS_FROM_PATTERN.search(full_context)) or
                    bool(LUA_VERSIONS_UNTIL_PATTERN.search(full_context)) or
                    bool(LUA_VERSION_RANGE_PATTERN.search(full_context)) or
                    bool(LUA_TEST_MATRIX_PATTERN.search(full_context))
                )

                test_method = TestMethod(
                    name=method_name,
                    file_path=str(file_path),
                    line_number=method_line,
                    class_name=class_name,
                    has_version_argument=len(version_args) > 0,
                    version_arguments=version_args,
                    has_methoddatasource=has_methoddatasource,
                    has_combineddatasources=has_combineddatasources,
                    has_lua_version_helper=has_lua_version_helper,
                    executes_lua=executes_lua,
                    method_body=method_body,
                )
                test_file.methods.append(test_method)

        i += 1

    return test_file if test_file.methods else None


def find_test_files(test_dir: Path) -> list[Path]:
    """Find all TUnit test files in the given directory."""
    test_files = []
    for file_path in test_dir.rglob("*TUnitTests.cs"):
        if not should_exclude_file(file_path):
            test_files.append(file_path)
    return sorted(test_files)


def generate_report(test_files: list[TestFile], detailed: bool = False) -> dict:
    """Generate an audit report."""
    total_tests = 0
    compliant_tests = 0
    non_compliant_tests = 0
    lua_needing_version = 0
    infrastructure_tests = 0
    non_compliant_details = []
    lua_execution_details = []
    infrastructure_details = []
    version_coverage = {v: 0 for v in LUA_VERSIONS}

    for tf in test_files:
        total_tests += len(tf.methods)
        compliant_tests += len(tf.compliant_methods)
        non_compliant_tests += len(tf.non_compliant_methods)
        lua_needing_version += len(tf.lua_execution_tests_needing_version)
        infrastructure_tests += len(tf.infrastructure_tests)

        for method in tf.non_compliant_methods:
            detail = {
                "file": method.file_path,
                "class": method.class_name,
                "method": method.name,
                "line": method.line_number,
                "executes_lua": method.executes_lua,
            }
            non_compliant_details.append(detail)
            if method.executes_lua:
                lua_execution_details.append(detail)
            else:
                infrastructure_details.append(detail)

        for method in tf.compliant_methods:
            for version in method.version_arguments:
                if version in version_coverage:
                    version_coverage[version] += 1

    # Calculate compliance percentage
    compliance_pct = (compliant_tests / total_tests * 100) if total_tests > 0 else 100

    return {
        "summary": {
            "total_tests": total_tests,
            "compliant_tests": compliant_tests,
            "non_compliant_tests": non_compliant_tests,
            "lua_execution_needing_version": lua_needing_version,
            "infrastructure_tests": infrastructure_tests,
            "compliance_percentage": round(compliance_pct, 2),
            "version_coverage": version_coverage,
        },
        "non_compliant_details": non_compliant_details if detailed else [],
        "lua_execution_details": lua_execution_details if detailed else [],
        "infrastructure_details": infrastructure_details if detailed else [],
        "files_analyzed": len(test_files),
    }


def print_report(report: dict, detailed: bool = False) -> None:
    """Print a human-readable report."""
    summary = report["summary"]

    print("=" * 70)
    print("TUnit Test Multi-Version Coverage Audit Report")
    print("=" * 70)
    print()
    print(f"Files analyzed: {report['files_analyzed']}")
    print(f"Total tests: {summary['total_tests']}")
    print(f"Compliant tests (have version args): {summary['compliant_tests']}")
    print(f"Non-compliant tests: {summary['non_compliant_tests']}")
    print()
    print("Non-compliant breakdown:")
    print(f"  🔴 Lua execution tests needing version: {summary['lua_execution_needing_version']}")
    print(f"  ⚪ Infrastructure tests (no Lua): {summary['infrastructure_tests']}")
    print()
    print(f"Compliance: {summary['compliance_percentage']}%")
    print()

    print("Version coverage (tests with explicit Arguments):")
    for version, count in summary["version_coverage"].items():
        print(f"  {version}: {count} tests")
    print()

    if detailed and report["lua_execution_details"]:
        print("-" * 70)
        print("🔴 HIGH PRIORITY: Lua execution tests missing version coverage:")
        print("-" * 70)

        # Group by file
        by_file = {}
        for detail in report["lua_execution_details"]:
            file_path = detail["file"]
            if file_path not in by_file:
                by_file[file_path] = []
            by_file[file_path].append(detail)

        for file_path, methods in sorted(by_file.items()):
            # Get relative path for display
            rel_path = file_path
            if "src/tests/" in file_path:
                rel_path = file_path[file_path.index("src/tests/") :]
            print(f"\n{rel_path}:")
            for method in methods:
                print(f"  Line {method['line']}: {method['class']}.{method['method']}")
        print()

    if detailed and report["infrastructure_details"]:
        print("-" * 70)
        print("⚪ LOW PRIORITY: Infrastructure tests (no Lua execution detected):")
        print("-" * 70)

        # Group by directory
        by_category = {}
        for detail in report["infrastructure_details"]:
            parts = detail["file"].split("/")
            if "WallstopStudios.NovaSharp.Interpreter.Tests.TUnit" in parts:
                idx = parts.index("WallstopStudios.NovaSharp.Interpreter.Tests.TUnit")
                category = parts[idx + 1] if idx + 1 < len(parts) else "Root"
            else:
                category = "Unknown"
            if category not in by_category:
                by_category[category] = 0
            by_category[category] += 1

        for category, count in sorted(by_category.items(), key=lambda x: -x[1]):
            print(f"  {category}: {count} tests")
        print()

    if summary["lua_execution_needing_version"] > 0:
        print("=" * 70)
        print("⚠️  ACTION REQUIRED: Add [Arguments(LuaCompatibilityVersion.LuaXX)]")
        print(f"   attributes to {summary['lua_execution_needing_version']} Lua execution tests.")
        print("=" * 70)


def main():
    parser = argparse.ArgumentParser(
        description="Audit TUnit tests for Lua version coverage"
    )
    parser.add_argument(
        "--detailed",
        "-d",
        action="store_true",
        help="Show detailed list of non-compliant tests",
    )
    parser.add_argument(
        "--json",
        "-j",
        action="store_true",
        help="Output report as JSON",
    )
    parser.add_argument(
        "--fail-on-noncompliant",
        "-f",
        action="store_true",
        help="Exit with code 1 if any non-compliant tests found",
    )
    parser.add_argument(
        "--lua-only",
        "-l",
        action="store_true",
        help="Only report on Lua execution tests (ignore infrastructure tests)",
    )
    args = parser.parse_args()

    # Find the repository root
    script_dir = Path(__file__).resolve().parent
    repo_root = script_dir.parent.parent

    test_dir = (
        repo_root
        / "src"
        / "tests"
        / "WallstopStudios.NovaSharp.Interpreter.Tests.TUnit"
    )

    if not test_dir.exists():
        print(f"Error: Test directory not found: {test_dir}", file=sys.stderr)
        sys.exit(1)

    # Find and analyze all test files
    test_files = find_test_files(test_dir)
    analyzed_files = []

    for file_path in test_files:
        result = analyze_test_file(file_path)
        if result:
            analyzed_files.append(result)

    # Generate report
    report = generate_report(analyzed_files, detailed=args.detailed or args.json)

    # Output report
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print_report(report, detailed=args.detailed)

    # Exit with error code if requested and non-compliant tests found
    if args.fail_on_noncompliant:
        if args.lua_only and report["summary"]["lua_execution_needing_version"] > 0:
            sys.exit(1)
        elif not args.lua_only and report["summary"]["non_compliant_tests"] > 0:
            sys.exit(1)

    sys.exit(0)


if __name__ == "__main__":
    main()

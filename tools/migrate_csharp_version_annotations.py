#!/usr/bin/env python3
"""
migrate_csharp_version_annotations.py - Batch-convert C# test version annotations to range syntax.

This script scans all .cs files in the TUnit test directories and converts explicit
version argument lists to more concise custom attributes where appropriate.

Conversions:
    [Arguments(Lua51)][Arguments(Lua52)]...[Arguments(Lua55)] -> [AllLuaVersions]
    [Arguments(Lua53)][Arguments(Lua54)][Arguments(Lua55)]    -> [LuaVersionsFrom(Lua53)]
    [Arguments(Lua51)][Arguments(Lua52)]                      -> [LuaVersionsUntil(Lua52)]
    [Arguments(Lua51)][Arguments(Lua52)][Arguments(Lua53)]    -> [LuaVersionRange(Lua51, Lua53)]

Non-contiguous patterns (e.g., Lua51, Lua53, Lua55) and patterns with extra
parameters are recorded in manual-review.txt for human review.

Usage:
    python tools/migrate_csharp_version_annotations.py                 # Dry run (default)
    python tools/migrate_csharp_version_annotations.py --dry-run       # Dry run (explicit)
    python tools/migrate_csharp_version_annotations.py --apply         # Apply changes
    python tools/migrate_csharp_version_annotations.py --verbose       # Show all changes in detail
    python tools/migrate_csharp_version_annotations.py --test          # Run unit tests
    python tools/migrate_csharp_version_annotations.py --apply --verbose  # Apply with details

Requirements:
    - Must be run from the repository root
    - Requires tools/lua_version_utils.py to be present

Limitations:
    - Only handles [Arguments(LuaCompatibilityVersion.LuaXX)] attributes
    - Attributes with extra parameters (e.g., [Arguments(Lua53, "extra")]) are skipped
    - Non-contiguous version patterns are written to manual-review.txt
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

# Script directory for relative imports
SCRIPT_DIR = Path(__file__).parent

# All known Lua versions as C# enum values (in order)
ALL_LUA_ENUM_VALUES = [
    "LuaCompatibilityVersion.Lua51",
    "LuaCompatibilityVersion.Lua52",
    "LuaCompatibilityVersion.Lua53",
    "LuaCompatibilityVersion.Lua54",
    "LuaCompatibilityVersion.Lua55",
]

# Version order map for C# enums
VERSION_ORDER = {v: i for i, v in enumerate(ALL_LUA_ENUM_VALUES)}

# Short forms (also valid)
SHORT_LUA_ENUM_VALUES = ["Lua51", "Lua52", "Lua53", "Lua54", "Lua55"]
SHORT_VERSION_ORDER = {v: i for i, v in enumerate(SHORT_LUA_ENUM_VALUES)}

# Combined order (matches both long and short forms)
COMBINED_VERSION_ORDER = {**VERSION_ORDER, **SHORT_VERSION_ORDER}
GENERATED_DIR_NAMES = {"bin", "obj", "artifacts"}


@dataclass
class AttributeGroup:
    """Represents a group of consecutive [Arguments(...)] attributes."""
    start_line: int              # 0-indexed line number where group starts
    end_line: int                # 0-indexed line number where group ends (inclusive)
    versions: list[str]          # List of version strings (e.g., ["Lua51", "Lua52"])
    original_text: str           # Original text of all attributes
    indentation: str             # Indentation before first attribute
    has_extra_params: bool       # True if any attribute has extra parameters
    is_contiguous: bool          # True if versions form a contiguous range
    can_convert: bool            # True if safe to auto-convert
    new_attribute: Optional[str] # Converted attribute (if can_convert)


@dataclass
class ConversionResult:
    """Result of attempting to convert a single file."""
    file_path: Path
    attribute_groups: list[AttributeGroup] = field(default_factory=list)
    converted_count: int = 0
    skipped_count: int = 0
    manual_review_count: int = 0
    error: Optional[str] = None


@dataclass
class MigrationReport:
    """Summary report of the migration process."""
    total_files_scanned: int = 0
    files_with_annotations: int = 0
    files_converted: int = 0
    total_groups_found: int = 0
    total_groups_converted: int = 0
    total_groups_skipped: int = 0
    total_manual_review: int = 0
    files_with_errors: int = 0
    conversions: list[ConversionResult] = field(default_factory=list)
    manual_review_entries: list[tuple[Path, AttributeGroup]] = field(default_factory=list)


# Regex to match [Arguments(LuaCompatibilityVersion.LuaXX)] or [Arguments(LuaXX)]
# Captures the version and any additional parameters
ARGUMENTS_PATTERN = re.compile(
    r'^\s*\[Arguments\s*\(\s*((?:LuaCompatibilityVersion\.)?Lua5[1-5])\s*'
    r'(?:,\s*(.+?))?\s*\)\s*\]\s*$'
)

# Regex to match any [Arguments(...)] line (for detection)
ANY_ARGUMENTS_PATTERN = re.compile(r'^\s*\[Arguments\s*\(')

# Regex to match start of method (public/private/protected/internal async? Task/void)
METHOD_START_PATTERN = re.compile(
    r'^\s*(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?'
    r'(?:Task|void|ValueTask)'
)


def normalize_version(version: str) -> str:
    """
    Normalize a version string to short form (e.g., "Lua53").

    Args:
        version: Version string to normalize (e.g., "LuaCompatibilityVersion.Lua53" or "Lua53")

    Returns:
        Normalized short form (e.g., "Lua53")
    """
    # Remove the prefix if present
    if version.startswith("LuaCompatibilityVersion."):
        return version[len("LuaCompatibilityVersion."):]
    return version


def is_contiguous(versions: list[str]) -> bool:
    """
    Check if a list of versions forms a contiguous range.

    Args:
        versions: List of version strings (normalized short form)

    Returns:
        True if versions form a contiguous range
    """
    if len(versions) <= 1:
        return True

    # Get indices in order
    indices = sorted([SHORT_VERSION_ORDER[v] for v in versions if v in SHORT_VERSION_ORDER])

    if len(indices) != len(versions):
        return False  # Some versions not recognized

    # Check if indices are consecutive
    for i in range(1, len(indices)):
        if indices[i] != indices[i - 1] + 1:
            return False

    return True


def determine_replacement(versions: list[str]) -> Optional[str]:
    """
    Determine the replacement attribute for a list of versions.

    Args:
        versions: List of normalized version strings (e.g., ["Lua51", "Lua52"])

    Returns:
        Replacement attribute string, or None if cannot be simplified
    """
    if not versions:
        return None

    # Sort by version order
    sorted_versions = sorted(
        versions,
        key=lambda v: SHORT_VERSION_ORDER.get(v, 999)
    )

    # Remove duplicates while preserving order
    seen = set()
    unique_versions = []
    for v in sorted_versions:
        if v not in seen:
            seen.add(v)
            unique_versions.append(v)
    sorted_versions = unique_versions

    if not is_contiguous(sorted_versions):
        return None

    # Check for all versions
    if set(sorted_versions) == set(SHORT_LUA_ENUM_VALUES):
        return "[AllLuaVersions]"

    first = sorted_versions[0]
    last = sorted_versions[-1]
    first_idx = SHORT_VERSION_ORDER.get(first, -1)
    last_idx = SHORT_VERSION_ORDER.get(last, -1)

    if first_idx == -1 or last_idx == -1:
        return None

    # Check for "from X" pattern (extends to latest version)
    if last_idx == len(SHORT_LUA_ENUM_VALUES) - 1 and first_idx > 0:
        return f"[LuaVersionsFrom(LuaCompatibilityVersion.{first})]"

    # Check for "until X" pattern (starts from earliest version)
    if first_idx == 0 and last_idx < len(SHORT_LUA_ENUM_VALUES) - 1:
        return f"[LuaVersionsUntil(LuaCompatibilityVersion.{last})]"

    # Otherwise use range syntax (if more than one version and not at edges)
    if len(sorted_versions) > 1:
        return f"[LuaVersionRange(LuaCompatibilityVersion.{first}, LuaCompatibilityVersion.{last})]"

    # Single version - keep as is (or could convert to single Arguments, but not worth it)
    return None


def find_attribute_groups(lines: list[str]) -> list[AttributeGroup]:
    """
    Find all groups of consecutive [Arguments(LuaXX)] attributes in a file.

    Args:
        lines: List of lines from the file

    Returns:
        List of AttributeGroup objects
    """
    groups = []
    i = 0

    while i < len(lines):
        # Skip lines that don't start an Arguments attribute
        if not ANY_ARGUMENTS_PATTERN.match(lines[i]):
            i += 1
            continue

        # Start of a potential group
        group_start = i
        versions = []
        has_extra_params = False
        indentation = ""

        # Collect consecutive Arguments attributes
        while i < len(lines) and ANY_ARGUMENTS_PATTERN.match(lines[i]):
            line = lines[i]

            # Capture indentation from first line
            if not indentation:
                indentation = line[:len(line) - len(line.lstrip())]

            match = ARGUMENTS_PATTERN.match(line)
            if match:
                version = normalize_version(match.group(1))
                extra_params = match.group(2)

                if extra_params:
                    has_extra_params = True

                if version in SHORT_VERSION_ORDER:
                    versions.append(version)
                else:
                    # Unknown version - skip this group
                    versions = []
                    break
            else:
                # Line doesn't match our pattern - not a simple version argument
                has_extra_params = True

            i += 1

        # Only process if we found version-only arguments
        if versions and len(versions) >= 2 and not has_extra_params:
            original_text = "".join(lines[group_start:i])
            contiguous = is_contiguous(versions)
            new_attr = determine_replacement(versions) if contiguous else None
            can_convert = bool(new_attr)

            group = AttributeGroup(
                start_line=group_start,
                end_line=i - 1,
                versions=versions,
                original_text=original_text,
                indentation=indentation,
                has_extra_params=has_extra_params,
                is_contiguous=contiguous,
                can_convert=can_convert,
                new_attribute=new_attr,
            )
            groups.append(group)
        elif versions and len(versions) >= 2:
            # Has extra params - record for manual review
            original_text = "".join(lines[group_start:i])
            group = AttributeGroup(
                start_line=group_start,
                end_line=i - 1,
                versions=versions,
                original_text=original_text,
                indentation=indentation,
                has_extra_params=has_extra_params,
                is_contiguous=is_contiguous(versions),
                can_convert=False,
                new_attribute=None,
            )
            groups.append(group)

    return groups


def process_file(file_path: Path, apply: bool = False) -> ConversionResult:
    """
    Process a single C# test file.

    Args:
        file_path: Path to the .cs file
        apply: Whether to actually write changes

    Returns:
        ConversionResult describing what happened
    """
    result = ConversionResult(file_path=file_path)

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.splitlines(keepends=True)
    except Exception as e:
        result.error = f"Failed to read file: {e}"
        return result

    if not lines:
        return result

    # Find attribute groups
    groups = find_attribute_groups(lines)
    result.attribute_groups = groups

    if not groups:
        return result

    # Count conversions
    for group in groups:
        if group.can_convert:
            result.converted_count += 1
        elif group.has_extra_params or not group.is_contiguous:
            result.manual_review_count += 1
        else:
            result.skipped_count += 1

    # Apply changes if requested
    if apply and result.converted_count > 0:
        # Build new content by replacing groups (in reverse order to preserve line numbers)
        new_lines = list(lines)

        for group in reversed(groups):
            if group.can_convert and group.new_attribute:
                # Replace the group with the new attribute
                new_attr_line = f"{group.indentation}{group.new_attribute}\n"
                new_lines[group.start_line:group.end_line + 1] = [new_attr_line]

        try:
            with open(file_path, 'w', encoding='utf-8') as f:
                f.writelines(new_lines)
        except Exception as e:
            result.error = f"Failed to write file: {e}"

    return result


# Filter pattern for TUnit test projects. The project naming convention is:
# WallstopStudios.NovaSharp.*.Tests.TUnit - these contain the parameterized
# test methods that use [Arguments(LuaCompatibilityVersion.LuaXX)] attributes.
# Other test projects (e.g., .Tests without .TUnit) use different patterns.
TUNIT_PROJECT_MARKER = ".TUnit"


def find_csharp_test_files(base_path: Path) -> list[Path]:
    """
    Find all .cs files in the TUnit test directories.

    Args:
        base_path: Repository root path

    Returns:
        List of paths to .cs test files
    """
    cs_files = []

    # Search in src/tests directories for .TUnit test files
    test_dirs = [
        base_path / "src" / "tests",
    ]

    for test_dir in test_dirs:
        if test_dir.exists():
            for cs_file in test_dir.rglob("*.cs"):
                if GENERATED_DIR_NAMES.intersection(cs_file.relative_to(base_path).parts):
                    continue
                # Only process files in .TUnit directories (see TUNIT_PROJECT_MARKER)
                if TUNIT_PROJECT_MARKER in str(cs_file):
                    cs_files.append(cs_file)

    return sorted(cs_files)


def run_migration(
    base_path: Path,
    apply: bool = False,
    verbose: bool = False
) -> MigrationReport:
    """
    Run the migration process on all C# test files.

    Args:
        base_path: Repository root path
        apply: Whether to actually write changes
        verbose: Whether to show detailed output

    Returns:
        MigrationReport with summary and details
    """
    report = MigrationReport()

    # Find all C# test files
    cs_files = find_csharp_test_files(base_path)
    report.total_files_scanned = len(cs_files)

    # Process each file
    for cs_file in cs_files:
        result = process_file(cs_file, apply=apply)
        report.conversions.append(result)

        if result.error:
            report.files_with_errors += 1
        elif result.attribute_groups:
            report.files_with_annotations += 1
            report.total_groups_found += len(result.attribute_groups)
            report.total_groups_converted += result.converted_count
            report.total_groups_skipped += result.skipped_count
            report.total_manual_review += result.manual_review_count

            if result.converted_count > 0:
                report.files_converted += 1

            # Collect manual review entries
            for group in result.attribute_groups:
                if not group.can_convert and (group.has_extra_params or not group.is_contiguous):
                    report.manual_review_entries.append((cs_file, group))

    return report


def write_manual_review_file(report: MigrationReport, base_path: Path):
    """
    Write the manual-review.txt file with patterns that need human review.

    Args:
        report: The migration report
        base_path: Repository root path
    """
    manual_review_path = base_path / "manual-review.txt"

    with open(manual_review_path, 'w', encoding='utf-8') as f:
        f.write("=" * 70 + "\n")
        f.write("C# Version Annotation Manual Review\n")
        f.write("=" * 70 + "\n\n")
        f.write("The following patterns could not be automatically converted and\n")
        f.write("require manual review:\n\n")

        if not report.manual_review_entries:
            f.write("No patterns require manual review.\n")
        else:
            for file_path, group in report.manual_review_entries:
                try:
                    rel_path = file_path.relative_to(base_path)
                except ValueError:
                    rel_path = file_path

                f.write("-" * 70 + "\n")
                f.write(f"File: {rel_path}\n")
                f.write(f"Lines: {group.start_line + 1}-{group.end_line + 1}\n")
                f.write(f"Versions: {', '.join(group.versions)}\n")

                if group.has_extra_params:
                    f.write("Reason: Attributes have extra parameters\n")
                elif not group.is_contiguous:
                    f.write("Reason: Non-contiguous version pattern\n")
                else:
                    f.write("Reason: Could not determine replacement\n")

                f.write("\nOriginal:\n")
                # Handle potentially incomplete captures by showing the raw text
                original_lines = group.original_text.splitlines()
                if original_lines:
                    for line in original_lines:
                        # Strip trailing whitespace but preserve leading indentation
                        f.write(f"  {line.rstrip()}\n")
                else:
                    # Fallback if splitlines returns empty (e.g., single line without newline)
                    f.write(f"  {group.original_text.strip()}\n")
                f.write("\n")

        f.write("=" * 70 + "\n")
        f.write(f"Total entries requiring review: {len(report.manual_review_entries)}\n")
        f.write("=" * 70 + "\n")


def print_report(report: MigrationReport, verbose: bool = False, apply: bool = False):
    """
    Print the migration report.

    Args:
        report: The migration report to print
        verbose: Whether to show detailed output
        apply: Whether changes were applied
    """
    mode = "APPLIED" if apply else "DRY RUN"

    print(f"\n{'='*70}")
    print(f"C# Version Annotation Migration Report ({mode})")
    print(f"{'='*70}\n")

    print("Summary:")
    print(f"  Total files scanned:          {report.total_files_scanned}")
    print(f"  Files with version args:      {report.files_with_annotations}")
    print(f"  Files converted:              {report.files_converted}")
    print(f"  Total attribute groups:       {report.total_groups_found}")
    print(f"  Groups converted:             {report.total_groups_converted}")
    print(f"  Groups skipped (single ver):  {report.total_groups_skipped}")
    print(f"  Groups for manual review:     {report.total_manual_review}")
    print(f"  Files with errors:            {report.files_with_errors}")

    # Show conversions
    converted = [r for r in report.conversions if r.converted_count > 0]
    if converted:
        print(f"\n{'-'*70}")
        print(f"Conversions ({len(converted)} files, {report.total_groups_converted} groups):")
        print(f"{'-'*70}")

        for result in converted:
            try:
                rel_path = result.file_path.relative_to(Path.cwd())
            except ValueError:
                rel_path = result.file_path

            print(f"\n  {rel_path}")

            for group in result.attribute_groups:
                if group.can_convert:
                    versions_str = ", ".join(group.versions)
                    print(f"    Lines {group.start_line + 1}-{group.end_line + 1}:")
                    print(f"      Before: [{versions_str}]")
                    print(f"      After:  {group.new_attribute}")

    # Show manual review entries if verbose
    if verbose and report.manual_review_entries:
        print(f"\n{'-'*70}")
        print(f"Manual Review Required ({len(report.manual_review_entries)} entries):")
        print(f"{'-'*70}")

        for file_path, group in report.manual_review_entries:
            try:
                rel_path = file_path.relative_to(Path.cwd())
            except ValueError:
                rel_path = file_path

            versions_str = ", ".join(group.versions)
            reason = "extra params" if group.has_extra_params else "non-contiguous"
            print(f"  {rel_path}:{group.start_line + 1} - [{versions_str}] ({reason})")

    # Show errors if any
    errors = [r for r in report.conversions if r.error]
    if errors:
        print(f"\n{'-'*70}")
        print(f"Errors ({len(errors)} files):")
        print(f"{'-'*70}")

        for result in errors:
            try:
                rel_path = result.file_path.relative_to(Path.cwd())
            except ValueError:
                rel_path = result.file_path
            print(f"  {rel_path}: {result.error}")

    print(f"\n{'='*70}")
    if not apply:
        print("This was a dry run. Use --apply to make actual changes.")
    if report.total_manual_review > 0:
        print(f"Manual review file written to: manual-review.txt")
    print(f"{'='*70}\n")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Migrate C# version annotations to range syntax"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        default=True,
        help="Preview changes without applying (default behavior)"
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Actually apply changes (default is dry run)"
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        help="Show detailed output including all files"
    )
    parser.add_argument(
        "--test",
        action="store_true",
        help="Run unit tests instead of migration"
    )
    parser.add_argument(
        "--path",
        type=Path,
        default=None,
        help="Repository root path (default: current directory)"
    )

    args = parser.parse_args()

    # Run tests if requested
    if args.test:
        return run_tests()

    # Determine base path
    base_path = args.path or Path.cwd()

    # Verify we're in the right place
    if not (base_path / "src" / "tests").exists():
        print(f"Error: Cannot find src/tests directory in {base_path}")
        print("Please run this script from the repository root.")
        sys.exit(1)

    # Run migration
    report = run_migration(base_path, apply=args.apply, verbose=args.verbose)

    # Write manual review file if needed
    if report.total_manual_review > 0:
        write_manual_review_file(report, base_path)

    # Print report
    print_report(report, verbose=args.verbose, apply=args.apply)

    return 0


# ============================================================================
# Unit Tests
# ============================================================================

def run_tests():
    """Run unit tests for the migration functions."""
    print("Running unit tests...")
    errors = []

    # Test normalize_version
    test_cases_normalize = [
        ("LuaCompatibilityVersion.Lua53", "Lua53"),
        ("Lua53", "Lua53"),
        ("LuaCompatibilityVersion.Lua51", "Lua51"),
        ("Lua55", "Lua55"),
    ]

    for input_str, expected in test_cases_normalize:
        result = normalize_version(input_str)
        if result != expected:
            errors.append(f"normalize_version({input_str!r}): expected {expected!r}, got {result!r}")

    # Test is_contiguous
    test_cases_contiguous = [
        (["Lua51", "Lua52", "Lua53"], True),
        (["Lua53", "Lua54", "Lua55"], True),
        (["Lua51", "Lua52", "Lua53", "Lua54", "Lua55"], True),
        (["Lua51"], True),
        (["Lua51", "Lua53"], False),  # Gap at 5.2
        (["Lua51", "Lua53", "Lua55"], False),  # Multiple gaps
        (["Lua52", "Lua54"], False),  # Gap at 5.3
        ([], True),  # Empty is contiguous
    ]

    for versions, expected in test_cases_contiguous:
        result = is_contiguous(versions)
        if result != expected:
            errors.append(f"is_contiguous({versions!r}): expected {expected}, got {result}")

    # Test determine_replacement
    test_cases_replacement = [
        # All versions -> [AllLuaVersions]
        (["Lua51", "Lua52", "Lua53", "Lua54", "Lua55"], "[AllLuaVersions]"),

        # From X -> [LuaVersionsFrom(X)]
        (["Lua53", "Lua54", "Lua55"], "[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]"),
        (["Lua52", "Lua53", "Lua54", "Lua55"], "[LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]"),
        (["Lua54", "Lua55"], "[LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]"),

        # Until X -> [LuaVersionsUntil(X)]
        (["Lua51", "Lua52"], "[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]"),
        (["Lua51", "Lua52", "Lua53"], "[LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]"),

        # Range -> [LuaVersionRange(X, Y)]
        (["Lua52", "Lua53", "Lua54"], "[LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua54)]"),
        (["Lua52", "Lua53"], "[LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua53)]"),

        # Non-contiguous -> None
        (["Lua51", "Lua53"], None),
        (["Lua51", "Lua53", "Lua55"], None),

        # Single version -> None (not worth converting)
        (["Lua53"], None),

        # Empty -> None
        ([], None),
    ]

    for versions, expected in test_cases_replacement:
        result = determine_replacement(versions)
        if result != expected:
            errors.append(f"determine_replacement({versions!r}): expected {expected!r}, got {result!r}")

    # Test ARGUMENTS_PATTERN regex
    test_cases_regex = [
        # (line, should_match, expected_version, expected_extra)
        ("        [Arguments(LuaCompatibilityVersion.Lua53)]", True, "LuaCompatibilityVersion.Lua53", None),
        ("        [Arguments(Lua53)]", True, "Lua53", None),
        ("    [Arguments(LuaCompatibilityVersion.Lua51)]", True, "LuaCompatibilityVersion.Lua51", None),
        ("[Arguments(Lua55)]", True, "Lua55", None),
        ("        [Arguments(LuaCompatibilityVersion.Lua53, \"extra\")]", True, "LuaCompatibilityVersion.Lua53", '"extra"'),
        ("        [Arguments(Lua53, 42)]", True, "Lua53", "42"),
        ("        [Test]", False, None, None),
        ("        [AllLuaVersions]", False, None, None),
        ("        public async Task Foo()", False, None, None),
    ]

    for line, should_match, expected_version, expected_extra in test_cases_regex:
        match = ARGUMENTS_PATTERN.match(line)
        if should_match:
            if not match:
                errors.append(f"ARGUMENTS_PATTERN should match: {line!r}")
            else:
                if match.group(1) != expected_version:
                    errors.append(
                        f"ARGUMENTS_PATTERN version mismatch for {line!r}: "
                        f"expected {expected_version!r}, got {match.group(1)!r}"
                    )
                if match.group(2) != expected_extra:
                    errors.append(
                        f"ARGUMENTS_PATTERN extra mismatch for {line!r}: "
                        f"expected {expected_extra!r}, got {match.group(2)!r}"
                    )
        else:
            if match:
                errors.append(f"ARGUMENTS_PATTERN should NOT match: {line!r}")

    # Test find_attribute_groups
    test_content_1 = [
        "        [Test]\n",
        "        [Arguments(LuaCompatibilityVersion.Lua51)]\n",
        "        [Arguments(LuaCompatibilityVersion.Lua52)]\n",
        "        [Arguments(LuaCompatibilityVersion.Lua53)]\n",
        "        public async Task Foo(LuaCompatibilityVersion v)\n",
        "        {\n",
        "        }\n",
    ]

    groups = find_attribute_groups(test_content_1)
    if len(groups) != 1:
        errors.append(f"find_attribute_groups test 1: expected 1 group, got {len(groups)}")
    elif groups[0].versions != ["Lua51", "Lua52", "Lua53"]:
        errors.append(f"find_attribute_groups test 1: wrong versions {groups[0].versions}")
    elif not groups[0].can_convert:
        errors.append("find_attribute_groups test 1: should be convertible")
    elif groups[0].new_attribute != "[LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]":
        errors.append(f"find_attribute_groups test 1: wrong replacement {groups[0].new_attribute}")

    # Test with extra params (should mark for manual review)
    test_content_2 = [
        "        [Arguments(Lua53, \"extra\")]\n",
        "        [Arguments(Lua54, \"extra\")]\n",
        "        public async Task Bar(LuaCompatibilityVersion v, string s)\n",
    ]

    groups = find_attribute_groups(test_content_2)
    if len(groups) != 1:
        errors.append(f"find_attribute_groups test 2: expected 1 group, got {len(groups)}")
    elif not groups[0].has_extra_params:
        errors.append("find_attribute_groups test 2: should have extra params")
    elif groups[0].can_convert:
        errors.append("find_attribute_groups test 2: should NOT be convertible")

    # Test with non-contiguous versions
    test_content_3 = [
        "        [Arguments(Lua51)]\n",
        "        [Arguments(Lua53)]\n",
        "        [Arguments(Lua55)]\n",
        "        public async Task Baz(LuaCompatibilityVersion v)\n",
    ]

    groups = find_attribute_groups(test_content_3)
    if len(groups) != 1:
        errors.append(f"find_attribute_groups test 3: expected 1 group, got {len(groups)}")
    elif groups[0].is_contiguous:
        errors.append("find_attribute_groups test 3: should NOT be contiguous")
    elif groups[0].can_convert:
        errors.append("find_attribute_groups test 3: should NOT be convertible")

    # Test multiple groups in one file
    test_content_4 = [
        "        [Test]\n",
        "        [Arguments(Lua51)]\n",
        "        [Arguments(Lua52)]\n",
        "        public async Task First(LuaCompatibilityVersion v) { }\n",
        "\n",
        "        [Test]\n",
        "        [Arguments(Lua53)]\n",
        "        [Arguments(Lua54)]\n",
        "        [Arguments(Lua55)]\n",
        "        public async Task Second(LuaCompatibilityVersion v) { }\n",
    ]

    groups = find_attribute_groups(test_content_4)
    if len(groups) != 2:
        errors.append(f"find_attribute_groups test 4: expected 2 groups, got {len(groups)}")
    elif groups[0].versions != ["Lua51", "Lua52"]:
        errors.append(f"find_attribute_groups test 4: wrong versions for group 1: {groups[0].versions}")
    elif groups[1].versions != ["Lua53", "Lua54", "Lua55"]:
        errors.append(f"find_attribute_groups test 4: wrong versions for group 2: {groups[1].versions}")

    # Test all 5 versions
    test_content_5 = [
        "        [Arguments(Lua51)]\n",
        "        [Arguments(Lua52)]\n",
        "        [Arguments(Lua53)]\n",
        "        [Arguments(Lua54)]\n",
        "        [Arguments(Lua55)]\n",
        "        public async Task All(LuaCompatibilityVersion v) { }\n",
    ]

    groups = find_attribute_groups(test_content_5)
    if len(groups) != 1:
        errors.append(f"find_attribute_groups test 5: expected 1 group, got {len(groups)}")
    elif groups[0].new_attribute != "[AllLuaVersions]":
        errors.append(f"find_attribute_groups test 5: wrong replacement {groups[0].new_attribute}")

    # Report results
    if errors:
        print(f"\nFAILED - {len(errors)} errors:")
        for error in errors:
            print(f"  - {error}")
        return 1
    else:
        print(f"\nPASSED - All tests passed!")
        return 0


if __name__ == "__main__":
    sys.exit(main())

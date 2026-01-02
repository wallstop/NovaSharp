#!/usr/bin/env python3
"""
migrate_version_annotations.py - Batch-convert Lua fixture version annotations to range syntax.

This script scans all .lua files in the test directories and converts explicit version
lists in @lua-versions annotations to more concise range syntax where appropriate.

Conversions:
    5.1, 5.2, 5.3, 5.4, 5.5  ->  all
    5.3, 5.4, 5.5            ->  5.3+
    5.2, 5.3, 5.4, 5.5       ->  5.2+
    5.4, 5.5                 ->  5.4+
    5.1, 5.2                 ->  5.1-5.2
    5.2, 5.3, 5.4            ->  5.2-5.4
    Other contiguous ranges  ->  5.X-5.Y

Usage:
    python tools/migrate_version_annotations.py                 # Dry run (default)
    python tools/migrate_version_annotations.py --dry-run       # Dry run (explicit)
    python tools/migrate_version_annotations.py --apply         # Apply changes
    python tools/migrate_version_annotations.py --verbose       # Show all changes in detail
    python tools/migrate_version_annotations.py --apply --verbose  # Apply with details

Requirements:
    - Must be run from the repository root
    - Requires tools/lua_version_utils.py to be present

Limitations:
    - Files should ideally have only one @lua-versions annotation per line.
      Multiple annotations on the same line are not supported and may produce
      unexpected results.
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

# Add tools directory to path for importing lua_version_utils
SCRIPT_DIR = Path(__file__).parent
sys.path.insert(0, str(SCRIPT_DIR))

from lua_version_utils import (
    ALL_LUA_VERSIONS,
    parse_lua_versions,
    simplify_version_list,
)


@dataclass
class ConversionResult:
    """Result of attempting to convert a single file."""
    file_path: Path
    original_annotation: Optional[str] = None
    new_annotation: Optional[str] = None
    converted: bool = False
    already_optimal: bool = False
    no_annotation: bool = False
    annotation_not_on_line1: bool = False  # Annotation found but not on line 1
    is_special_pattern: bool = False  # True for 'none', 'novasharp-only', etc.
    error: Optional[str] = None
    line_number: int = 1  # Line where annotation was found


@dataclass
class MigrationReport:
    """Summary report of the migration process."""
    total_files_scanned: int = 0
    files_with_annotations: int = 0
    files_converted: int = 0
    files_already_optimal: int = 0
    files_no_annotation: int = 0
    files_with_errors: int = 0
    non_contiguous_patterns: int = 0
    special_patterns: int = 0  # 'none', 'novasharp-only', etc.
    annotations_not_on_line1: int = 0
    conversions: list[ConversionResult] = field(default_factory=list)


# Maximum number of lines to check for annotation if not found on line 1
MAX_LINES_TO_CHECK_FOR_ANNOTATION = 20

# Regex to match @lua-versions annotation on line 1
# Format: -- @lua-versions: VALUE
LUA_VERSIONS_PATTERN = re.compile(
    r'^(--\s*@lua-versions:\s*)(.+)$',
    re.IGNORECASE
)


def find_lua_fixtures(base_path: Path) -> list[Path]:
    """
    Find all .lua files in the test directories.

    Args:
        base_path: Repository root path

    Returns:
        List of paths to .lua fixture files
    """
    lua_files = []

    # Search in src/tests directories
    test_dirs = [
        base_path / "src" / "tests",
    ]

    for test_dir in test_dirs:
        if test_dir.exists():
            for lua_file in test_dir.rglob("*.lua"):
                lua_files.append(lua_file)

    return sorted(lua_files)


def is_special_pattern(version_str: str) -> bool:
    """
    Check if the version string is a special pattern (not a version list).

    Args:
        version_str: The version specification string

    Returns:
        True if it's a special pattern like 'none' or 'novasharp-only'
    """
    version_str = version_str.strip().lower()

    # Check for "none" (no versions applicable)
    if version_str == "none":
        return True

    # Check for novasharp-only
    if "novasharp-only" in version_str:
        return True

    return False


def is_already_range_syntax(version_str: str) -> bool:
    """
    Check if the version string already uses range syntax.

    Args:
        version_str: The version specification string

    Returns:
        True if already using range syntax (all, none, 5.X+, 5.X-5.Y)
    """
    version_str = version_str.strip().lower()

    # Check for "all"
    if version_str == "all":
        return True

    # Check for "none" (no versions applicable)
    if version_str == "none":
        return True

    # Check for "5.X+" syntax
    if re.match(r'^\d+\.\d+\+$', version_str):
        return True

    # Check for "5.X-5.Y" range syntax
    if re.match(r'^\d+\.\d+-\d+\.\d+$', version_str):
        return True

    # Check for single version (already optimal)
    if re.match(r'^\d+\.\d+$', version_str):
        return True

    # Check for novasharp-only (already optimal)
    if "novasharp-only" in version_str:
        return True

    return False


def normalize_explicit_list(version_str: str) -> Optional[str]:
    """
    Normalize an explicit version list, handling various formats.

    Args:
        version_str: The version specification string

    Returns:
        Normalized comma-separated list, or None if not an explicit list
    """
    version_str = version_str.strip()

    # Skip if already range syntax
    if is_already_range_syntax(version_str):
        return None

    # Parse the versions
    versions = parse_lua_versions(version_str)

    if not versions:
        return None

    return ", ".join(versions)


def convert_annotation(version_str: str) -> tuple[str, bool]:
    """
    Convert a version annotation to optimal range syntax if possible.

    Args:
        version_str: The original version specification string

    Returns:
        Tuple of (new_annotation, was_converted)
    """
    original = version_str.strip()

    # Skip if already using optimal syntax
    if is_already_range_syntax(original):
        return original, False

    # Parse the versions
    versions = parse_lua_versions(original)

    if not versions:
        return original, False

    # Simplify to optimal representation
    simplified = simplify_version_list(versions)

    # Check if the simplification is different and valid
    if simplified and simplified != original and simplified != ", ".join(versions):
        # Verify the conversion is correct by round-tripping
        reparsed = parse_lua_versions(simplified)
        if reparsed == versions:
            return simplified, True

    return original, False


def process_file(file_path: Path, apply: bool = False) -> ConversionResult:
    """
    Process a single Lua fixture file.

    Args:
        file_path: Path to the .lua file
        apply: Whether to actually write changes

    Returns:
        ConversionResult describing what happened
    """
    result = ConversionResult(file_path=file_path)

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except Exception as e:
        result.error = f"Failed to read file: {e}"
        return result

    if not lines:
        result.no_annotation = True
        return result

    # Check line 1 for @lua-versions annotation
    first_line = lines[0]
    match = LUA_VERSIONS_PATTERN.match(first_line.rstrip('\n\r'))

    if not match:
        # Check if annotation is on a different line (non-standard - should be on line 1)
        for i, line in enumerate(lines[1:MAX_LINES_TO_CHECK_FOR_ANNOTATION], start=2):
            match = LUA_VERSIONS_PATTERN.match(line.rstrip('\n\r'))
            if match:
                result.line_number = i
                result.annotation_not_on_line1 = True
                break

        if not match:
            result.no_annotation = True
            return result

    prefix = match.group(1)  # e.g., "-- @lua-versions: "
    version_str = match.group(2)  # e.g., "5.1, 5.2, 5.3, 5.4, 5.5"

    result.original_annotation = version_str

    # Check if this is a special pattern (none, novasharp-only, etc.)
    if is_special_pattern(version_str):
        result.is_special_pattern = True
        result.already_optimal = True
        result.new_annotation = version_str
        return result

    # Check if already optimal
    if is_already_range_syntax(version_str):
        result.already_optimal = True
        result.new_annotation = version_str
        return result

    # Try to convert
    new_version_str, was_converted = convert_annotation(version_str)
    result.new_annotation = new_version_str

    if was_converted:
        result.converted = True

        if apply:
            # Update the line
            line_idx = result.line_number - 1
            new_line = f"{prefix}{new_version_str}\n"
            lines[line_idx] = new_line

            try:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.writelines(lines)
            except Exception as e:
                result.error = f"Failed to write file: {e}"
                result.converted = False

    return result


def run_migration(
    base_path: Path,
    apply: bool = False,
    verbose: bool = False
) -> MigrationReport:
    """
    Run the migration process on all Lua fixtures.

    Args:
        base_path: Repository root path
        apply: Whether to actually write changes
        verbose: Whether to show detailed output

    Returns:
        MigrationReport with summary and details
    """
    report = MigrationReport()

    # Find all Lua fixtures
    lua_files = find_lua_fixtures(base_path)
    report.total_files_scanned = len(lua_files)

    # Process each file
    for lua_file in lua_files:
        result = process_file(lua_file, apply=apply)
        report.conversions.append(result)

        if result.error:
            report.files_with_errors += 1
        elif result.no_annotation:
            report.files_no_annotation += 1
        else:
            report.files_with_annotations += 1
            if result.annotation_not_on_line1:
                report.annotations_not_on_line1 += 1
            if result.is_special_pattern:
                report.special_patterns += 1
            elif result.already_optimal:
                report.files_already_optimal += 1
            elif result.converted:
                report.files_converted += 1
            else:
                # Has annotation but couldn't be simplified (non-contiguous)
                report.non_contiguous_patterns += 1

    return report


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
    print(f"Lua Version Annotation Migration Report ({mode})")
    print(f"{'='*70}\n")

    print("Summary:")
    print(f"  Total files scanned:        {report.total_files_scanned}")
    print(f"  Files with @lua-versions:   {report.files_with_annotations}")
    print(f"  Files converted:            {report.files_converted}")
    print(f"  Files already optimal:      {report.files_already_optimal}")
    print(f"  Files without annotation:   {report.files_no_annotation}")
    print(f"  Non-contiguous patterns:    {report.non_contiguous_patterns}")
    print(f"  Special patterns:           {report.special_patterns}")
    print(f"  Annotations not on line 1:  {report.annotations_not_on_line1}")
    print(f"  Files with errors:          {report.files_with_errors}")

    # Show conversions
    converted = [r for r in report.conversions if r.converted]
    if converted:
        print(f"\n{'-'*70}")
        print(f"Conversions ({len(converted)} files):")
        print(f"{'-'*70}")

        for result in converted:
            rel_path = result.file_path.relative_to(Path.cwd())
            print(f"\n  {rel_path}")
            print(f"    Before: {result.original_annotation}")
            print(f"    After:  {result.new_annotation}")

    # Show non-contiguous patterns if verbose
    non_contiguous = [
        r for r in report.conversions
        if not r.no_annotation
        and not r.already_optimal
        and not r.converted
        and not r.error
        and not r.is_special_pattern
    ]

    if verbose and non_contiguous:
        print(f"\n{'-'*70}")
        print(f"Non-contiguous patterns (cannot auto-convert) ({len(non_contiguous)} files):")
        print(f"{'-'*70}")

        for result in non_contiguous:
            rel_path = result.file_path.relative_to(Path.cwd())
            print(f"  {rel_path}: {result.original_annotation}")

    # Show special patterns if verbose
    special = [r for r in report.conversions if r.is_special_pattern]

    if verbose and special:
        print(f"\n{'-'*70}")
        print(f"Special patterns (none, novasharp-only, etc.) ({len(special)} files):")
        print(f"{'-'*70}")

        for result in special:
            rel_path = result.file_path.relative_to(Path.cwd())
            print(f"  {rel_path}: {result.original_annotation}")

    # Show errors if any
    errors = [r for r in report.conversions if r.error]
    if errors:
        print(f"\n{'-'*70}")
        print(f"Errors ({len(errors)} files):")
        print(f"{'-'*70}")

        for result in errors:
            rel_path = result.file_path.relative_to(Path.cwd())
            print(f"  {rel_path}: {result.error}")

    # Show files with annotations not on line 1 (always show - these should be fixed)
    not_on_line1 = [r for r in report.conversions if r.annotation_not_on_line1]
    if not_on_line1:
        print(f"\n{'-'*70}")
        print(f"WARNING: Annotations not on line 1 ({len(not_on_line1)} files):")
        print(f"(These should be fixed - annotation must be on LINE 1)")
        print(f"{'-'*70}")

        for result in not_on_line1:
            rel_path = result.file_path.relative_to(Path.cwd())
            print(f"  {rel_path} (line {result.line_number}): {result.original_annotation}")

    # Show already optimal if verbose (exclude special patterns - they're shown separately)
    if verbose:
        optimal = [
            r for r in report.conversions
            if r.already_optimal
            and not r.annotation_not_on_line1
            and not r.is_special_pattern
        ]
        if optimal:
            print(f"\n{'-'*70}")
            print(f"Already using optimal syntax ({len(optimal)} files):")
            print(f"{'-'*70}")

            for result in optimal[:20]:  # Limit to first 20
                rel_path = result.file_path.relative_to(Path.cwd())
                print(f"  {rel_path}: {result.new_annotation}")

            if len(optimal) > 20:
                print(f"  ... and {len(optimal) - 20} more")

    print(f"\n{'='*70}")
    if not apply:
        print("This was a dry run. Use --apply to make actual changes.")
    print(f"{'='*70}\n")


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(
        description="Migrate Lua version annotations to range syntax"
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
        "--path",
        type=Path,
        default=None,
        help="Repository root path (default: current directory)"
    )

    args = parser.parse_args()

    # Determine base path
    base_path = args.path or Path.cwd()

    # Verify we're in the right place
    if not (base_path / "src" / "tests").exists():
        print(f"Error: Cannot find src/tests directory in {base_path}")
        print("Please run this script from the repository root.")
        sys.exit(1)

    # Run migration
    report = run_migration(base_path, apply=args.apply, verbose=args.verbose)

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

    # Test is_already_range_syntax
    test_cases_range = [
        ("all", True),
        ("none", True),  # Special pattern for no versions
        ("5.3+", True),
        ("5.5+", True),  # Edge case: highest version with + syntax
        ("5.1-5.4", True),
        ("5.4", True),
        ("novasharp-only", True),
        ("5.1, 5.2, 5.3", False),
        ("5.3, 5.4, 5.5", False),
        ("5.1, 5.2, 5.3, 5.4, 5.5", False),
        # Edge cases: whitespace handling
        ("  all  ", True),
        ("  none  ", True),
        ("  5.3+  ", True),
        ("  5.5+  ", True),
        # Edge cases: case insensitivity
        ("ALL", True),
        ("NONE", True),
        ("All", True),
        ("None", True),
    ]

    for input_str, expected in test_cases_range:
        result = is_already_range_syntax(input_str)
        if result != expected:
            errors.append(f"is_already_range_syntax({input_str!r}): expected {expected}, got {result}")

    # Test is_special_pattern
    test_cases_special = [
        ("none", True),
        ("None", True),
        ("NONE", True),
        ("  none  ", True),
        ("novasharp-only", True),
        ("NOVASHARP-ONLY", True),
        ("  novasharp-only  ", True),
        ("all", False),
        ("5.3+", False),
        ("5.1-5.4", False),
        ("5.4", False),
        ("5.1, 5.2, 5.3", False),
    ]

    for input_str, expected in test_cases_special:
        result = is_special_pattern(input_str)
        if result != expected:
            errors.append(f"is_special_pattern({input_str!r}): expected {expected}, got {result}")

    # Test convert_annotation
    test_cases_convert = [
        # (input, expected_output, expected_converted)
        ("5.1, 5.2, 5.3, 5.4, 5.5", "all", True),
        ("5.3, 5.4, 5.5", "5.3+", True),
        ("5.2, 5.3, 5.4, 5.5", "5.2+", True),
        ("5.4, 5.5", "5.4+", True),
        ("5.1, 5.2", "5.1-5.2", True),
        ("5.2, 5.3, 5.4", "5.2-5.4", True),
        ("5.1, 5.2, 5.3", "5.1-5.3", True),
        ("5.1, 5.3, 5.5", "5.1, 5.3, 5.5", False),  # Non-contiguous
        ("5.1, 5.3", "5.1, 5.3", False),  # Non-contiguous
        ("all", "all", False),  # Already optimal
        ("none", "none", False),  # Already optimal (special pattern)
        ("5.3+", "5.3+", False),  # Already optimal
        ("5.5+", "5.5+", False),  # Already optimal (edge case: highest version)
        ("5.1-5.4", "5.1-5.4", False),  # Already optimal
        ("5.4", "5.4", False),  # Single version, already optimal
        ("novasharp-only", "novasharp-only", False),  # Already optimal
    ]

    for input_str, expected_output, expected_converted in test_cases_convert:
        output, converted = convert_annotation(input_str)
        if output != expected_output:
            errors.append(
                f"convert_annotation({input_str!r}): "
                f"expected output {expected_output!r}, got {output!r}"
            )
        if converted != expected_converted:
            errors.append(
                f"convert_annotation({input_str!r}): "
                f"expected converted={expected_converted}, got {converted}"
            )

    # Test that simplification is correct by round-tripping
    round_trip_cases = [
        "5.1, 5.2, 5.3, 5.4, 5.5",
        "5.3, 5.4, 5.5",
        "5.2, 5.3, 5.4, 5.5",
        "5.1, 5.2",
        "5.2, 5.3, 5.4",
    ]

    for input_str in round_trip_cases:
        original_versions = parse_lua_versions(input_str)
        simplified, _ = convert_annotation(input_str)
        reparsed_versions = parse_lua_versions(simplified)
        if original_versions != reparsed_versions:
            errors.append(
                f"Round-trip failed for {input_str!r}: "
                f"original={original_versions}, reparsed={reparsed_versions}"
            )

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
    # Check if running tests
    if len(sys.argv) > 1 and sys.argv[1] == "--test":
        sys.exit(run_tests())
    else:
        sys.exit(main())

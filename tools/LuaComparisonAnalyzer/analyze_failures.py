#!/usr/bin/env python3
"""
analyze_failures.py - Analyze Lua comparison CI/CD failures and categorize them.

This script processes failure archives from the Lua comparison CI/CD pipeline
and builds a failure matrix (OS x Version x Fixture) to categorize failures
by pattern:

| Pattern | Diagnosis | Action |
|---------|-----------|--------|
| Fails ALL OS x ALL versions | NovaSharp bug | Fix src/runtime/ |
| Fails ONE OS only | Platform C library quirk | @novasharp-only: true |
| Fails ONE version only | Metadata bug | Fix @lua-versions |
| NovaSharp consistent, Lua varies | Platform difference | @novasharp-only: true |

Usage:
    python3 tools/LuaComparisonAnalyzer/analyze_failures.py [OPTIONS]

Options:
    --failures-dir DIR    Directory containing extracted failures (default: scratch/lua-failures)
    --output-file FILE    Output report file (default: scratch/lua-failures/analysis-report.json)
    --verbose            Show detailed progress
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Optional

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from lua_version_utils import (
    ALL_LUA_VERSIONS,
    simplify_version_list,
    parse_lua_versions,
)

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_FAILURES_DIR = ROOT / "scratch" / "lua-failures"
DEFAULT_FIXTURES_DIR = ROOT / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests" / "LuaFixtures"

ALL_OS_PLATFORMS = ["ubuntu-latest", "macos-latest", "windows-latest"]


@dataclass
class FailureInfo:
    """Information about a single fixture failure."""
    fixture: str
    version: str
    os_platform: str
    status: str  # "mismatch", "both_error", "lua_only", "nova_only", "skipped"
    diff_summary: str = ""
    lua_rc: int = 0
    nova_rc: int = 0
    lua_error: str = ""
    nova_error: str = ""


@dataclass
class FixtureAnalysis:
    """Analysis result for a single fixture."""
    fixture: str
    category: str  # "novasharp_bug", "os_specific", "version_specific", "platform_difference", "metadata_bug"
    action: str
    failing_versions: list[str] = field(default_factory=list)
    failing_oses: list[str] = field(default_factory=list)
    failure_details: list[FailureInfo] = field(default_factory=list)
    suggested_metadata: Optional[str] = None
    priority: str = "low"  # "critical", "high", "medium", "low"


def normalize_fixture_path(path: str) -> str:
    """Normalize fixture path separators (Windows vs Unix)."""
    return path.replace("\\", "/")


def extract_fixture_name(path: str) -> str:
    """Extract the fixture name from a path, normalizing separators."""
    return normalize_fixture_path(path)


def load_comparison_results(failures_dir: Path) -> dict[tuple[str, str], dict]:
    """
    Load all comparison JSON files from the failures directory.

    Returns: Dict mapping (version, os) to comparison data
    """
    results = {}

    for subdir in failures_dir.iterdir():
        if not subdir.is_dir():
            continue

        # Parse version and OS from directory name (e.g., "5.1-macos-latest")
        parts = subdir.name.split("-", 1)
        if len(parts) != 2:
            continue

        version = parts[0]
        os_platform = parts[1]

        # Find comparison JSON file
        comparison_file = subdir / f"comparison-{version}.json"
        if not comparison_file.exists():
            continue

        try:
            with open(comparison_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                results[(version, os_platform)] = data
        except (json.JSONDecodeError, IOError) as e:
            print(f"Warning: Failed to load {comparison_file}: {e}", file=sys.stderr)

    return results


def build_failure_matrix(results: dict[tuple[str, str], dict]) -> dict[str, list[FailureInfo]]:
    """
    Build a matrix of fixtures to their failure information.

    Returns: Dict mapping fixture path to list of FailureInfo
    """
    matrix = defaultdict(list)

    for (version, os_platform), data in results.items():
        # Process mismatches
        for mismatch in data.get("mismatches", []):
            fixture = normalize_fixture_path(mismatch.get("file", ""))
            if not fixture:
                continue

            info = FailureInfo(
                fixture=fixture,
                version=version,
                os_platform=os_platform,
                status="mismatch",
                diff_summary=mismatch.get("diff_summary", ""),
                lua_rc=mismatch.get("lua_rc", 0),
                nova_rc=mismatch.get("nova_rc", 0),
            )
            matrix[fixture].append(info)

        # Process both_errors (these are not failures but useful for context)
        for both_err in data.get("both_errors", []):
            fixture = normalize_fixture_path(both_err.get("file", ""))
            if not fixture:
                continue

            info = FailureInfo(
                fixture=fixture,
                version=version,
                os_platform=os_platform,
                status="both_error",
                lua_error=both_err.get("lua_error", "")[:500],
                nova_error=both_err.get("nova_error", "")[:500],
            )
            # Note: We track both_errors but they're not failures
            # Only include if we want to analyze error message consistency

    return dict(matrix)


def analyze_fixture(fixture: str, failures: list[FailureInfo]) -> FixtureAnalysis:
    """
    Analyze failure patterns for a single fixture and determine the category.
    """
    # Get unique failing versions and OSes
    failing_versions = sorted(set(f.version for f in failures))
    failing_oses = sorted(set(f.os_platform for f in failures))

    # Determine versions and OSes we have data for
    available_versions = list(set(f.version for f in failures))
    available_oses = list(set(f.os_platform for f in failures))

    analysis = FixtureAnalysis(
        fixture=fixture,
        category="unknown",
        action="",
        failing_versions=failing_versions,
        failing_oses=failing_oses,
        failure_details=failures,
    )

    # Count failures
    num_failing_versions = len(failing_versions)
    num_failing_oses = len(failing_oses)

    # Pattern 1: Fails ALL OS x ALL versions (or nearly all available)
    # This suggests a NovaSharp bug
    if num_failing_oses >= 2 and num_failing_versions >= 3:
        analysis.category = "novasharp_bug"
        analysis.action = "Fix NovaSharp implementation in src/runtime/"
        analysis.priority = "critical"
        return analysis

    # Pattern 2: Fails ONE OS only (across multiple versions)
    # This suggests a platform-specific C library difference
    if num_failing_oses == 1 and num_failing_versions >= 2:
        analysis.category = "os_specific"
        analysis.action = f"Add @novasharp-only: true (platform difference on {failing_oses[0]})"
        analysis.priority = "medium"
        analysis.suggested_metadata = "@novasharp-only: true"
        return analysis

    # Pattern 3: Fails ONE version only (across multiple OSes)
    # This suggests a version metadata bug
    if num_failing_versions == 1 and num_failing_oses >= 2:
        analysis.category = "version_specific"
        analysis.action = f"Fix @lua-versions metadata to exclude {failing_versions[0]}"
        analysis.priority = "high"

        # Try to suggest the correct metadata
        excluded_version = failing_versions[0]
        if excluded_version in ALL_LUA_VERSIONS:
            other_versions = [v for v in ALL_LUA_VERSIONS if v != excluded_version]
            analysis.suggested_metadata = f"@lua-versions: {simplify_version_list(other_versions)}"
        return analysis

    # Pattern 4: Single version, single OS failure
    # Could be either platform difference or version metadata issue
    if num_failing_versions == 1 and num_failing_oses == 1:
        analysis.category = "isolated_failure"
        analysis.action = f"Investigate: single failure on {failing_oses[0]} / Lua {failing_versions[0]}"
        analysis.priority = "low"
        return analysis

    # Pattern 5: Mix of versions and OSes (complex pattern)
    analysis.category = "complex_pattern"
    analysis.action = "Manual investigation required - complex failure pattern"
    analysis.priority = "medium"
    return analysis


def read_fixture_metadata(fixture_path: Path) -> tuple[list[str], bool]:
    """
    Read the @lua-versions and @novasharp-only metadata from a fixture file.

    Returns: (lua_versions, novasharp_only)
    """
    lua_versions = []
    novasharp_only = False

    try:
        with open(fixture_path, 'r', encoding='utf-8') as f:
            for _ in range(15):  # Check first 15 lines
                line = f.readline()
                if not line.startswith("--"):
                    break

                if "@lua-versions:" in line:
                    versions_part = line.split("@lua-versions:")[1].strip()
                    if "novasharp-only" in versions_part.lower():
                        novasharp_only = True
                    else:
                        lua_versions = parse_lua_versions(versions_part)

                if "@novasharp-only:" in line.lower() and "true" in line.lower():
                    novasharp_only = True
    except (IOError, UnicodeDecodeError, OSError):
        pass

    return lua_versions, novasharp_only


def generate_report(
    failure_matrix: dict[str, list[FailureInfo]],
    fixtures_dir: Path,
) -> dict[str, Any]:
    """
    Generate the full analysis report.
    """
    analyses = []

    for fixture, failures in sorted(failure_matrix.items()):
        analysis = analyze_fixture(fixture, failures)

        # Check current metadata
        fixture_path = fixtures_dir / fixture
        if fixture_path.exists():
            current_versions, current_novasharp_only = read_fixture_metadata(fixture_path)
            analysis_dict = {
                "fixture": analysis.fixture,
                "category": analysis.category,
                "action": analysis.action,
                "priority": analysis.priority,
                "failing_versions": analysis.failing_versions,
                "failing_oses": analysis.failing_oses,
                "suggested_metadata": analysis.suggested_metadata,
                "current_lua_versions": simplify_version_list(current_versions) if current_versions else None,
                "current_novasharp_only": current_novasharp_only,
            }
        else:
            analysis_dict = {
                "fixture": analysis.fixture,
                "category": analysis.category,
                "action": analysis.action,
                "priority": analysis.priority,
                "failing_versions": analysis.failing_versions,
                "failing_oses": analysis.failing_oses,
                "suggested_metadata": analysis.suggested_metadata,
            }

        analyses.append(analysis_dict)

    # Group by category for summary
    by_category = defaultdict(list)
    for a in analyses:
        by_category[a["category"]].append(a)

    # Count by priority
    by_priority = defaultdict(int)
    for a in analyses:
        by_priority[a["priority"]] += 1

    # Generate summary statistics
    summary = {
        "total_failing_fixtures": len(analyses),
        "by_category": {k: len(v) for k, v in by_category.items()},
        "by_priority": dict(by_priority),
        "categories": {
            "novasharp_bug": "Fails on ALL OS x ALL versions -> Fix NovaSharp runtime",
            "os_specific": "Fails on ONE OS only -> Platform C library quirk",
            "version_specific": "Fails on ONE version only -> Fix @lua-versions metadata",
            "isolated_failure": "Single version+OS failure -> Investigate",
            "complex_pattern": "Mixed pattern -> Manual investigation needed",
        }
    }

    return {
        "summary": summary,
        "analyses": analyses,
        "by_category": {k: v for k, v in sorted(by_category.items())},
    }


def print_report(report: dict[str, Any]):
    """Print a human-readable report to stdout."""
    summary = report["summary"]

    print("=" * 80)
    print("LUA COMPARISON FAILURE ANALYSIS REPORT")
    print("=" * 80)
    print()

    print(f"Total failing fixtures: {summary['total_failing_fixtures']}")
    print()

    print("FAILURES BY CATEGORY:")
    print("-" * 40)
    for cat, count in sorted(summary["by_category"].items()):
        desc = summary["categories"].get(cat, "")
        print(f"  {cat}: {count}")
        if desc:
            print(f"    -> {desc}")
    print()

    print("FAILURES BY PRIORITY:")
    print("-" * 40)
    for prio in ["critical", "high", "medium", "low"]:
        count = summary["by_priority"].get(prio, 0)
        if count > 0:
            print(f"  {prio.upper()}: {count}")
    print()

    print("=" * 80)
    print("DETAILED FINDINGS")
    print("=" * 80)
    print()

    # Print by priority
    for priority in ["critical", "high", "medium", "low"]:
        priority_analyses = [a for a in report["analyses"] if a["priority"] == priority]
        if not priority_analyses:
            continue

        print(f"\n[{priority.upper()}] Priority Failures ({len(priority_analyses)} fixtures)")
        print("-" * 60)

        for analysis in priority_analyses:
            print(f"\n  Fixture: {analysis['fixture']}")
            print(f"  Category: {analysis['category']}")
            print(f"  Action: {analysis['action']}")
            print(f"  Failing versions: {', '.join(analysis['failing_versions'])}")
            print(f"  Failing OSes: {', '.join(analysis['failing_oses'])}")

            if analysis.get("suggested_metadata"):
                print(f"  Suggested: {analysis['suggested_metadata']}")

            if analysis.get("current_lua_versions"):
                print(f"  Current @lua-versions: {analysis['current_lua_versions']}")

            if analysis.get("current_novasharp_only"):
                print(f"  Current @novasharp-only: true")

    print()
    print("=" * 80)
    print("ACTION SUMMARY")
    print("=" * 80)
    print()

    # Group actions
    novasharp_bugs = [a for a in report["analyses"] if a["category"] == "novasharp_bug"]
    version_bugs = [a for a in report["analyses"] if a["category"] == "version_specific"]
    os_specific = [a for a in report["analyses"] if a["category"] == "os_specific"]

    if novasharp_bugs:
        print(f"1. FIX NOVASHARP BUGS ({len(novasharp_bugs)} fixtures)")
        print("   These fixtures fail consistently across OS and versions.")
        print("   Likely NovaSharp implementation issues in src/runtime/.")
        for a in novasharp_bugs[:5]:
            print(f"   - {a['fixture']}")
        if len(novasharp_bugs) > 5:
            print(f"   ... and {len(novasharp_bugs) - 5} more")
        print()

    if version_bugs:
        print(f"2. FIX VERSION METADATA ({len(version_bugs)} fixtures)")
        print("   These fixtures have incorrect @lua-versions metadata.")
        for a in version_bugs[:5]:
            print(f"   - {a['fixture']}")
            if a.get("suggested_metadata"):
                print(f"     Suggested: {a['suggested_metadata']}")
        if len(version_bugs) > 5:
            print(f"   ... and {len(version_bugs) - 5} more")
        print()

    if os_specific:
        print(f"3. MARK AS NOVASHARP-ONLY ({len(os_specific)} fixtures)")
        print("   These fixtures have OS-specific behavior that differs from Lua.")
        for a in os_specific[:5]:
            print(f"   - {a['fixture']} ({', '.join(a['failing_oses'])})")
        if len(os_specific) > 5:
            print(f"   ... and {len(os_specific) - 5} more")
        print()


def main():
    parser = argparse.ArgumentParser(
        description="Analyze Lua comparison CI/CD failures"
    )
    parser.add_argument(
        "--failures-dir",
        type=Path,
        default=DEFAULT_FAILURES_DIR,
        help="Directory containing extracted failures"
    )
    parser.add_argument(
        "--fixtures-dir",
        type=Path,
        default=DEFAULT_FIXTURES_DIR,
        help="Directory containing Lua fixture files"
    )
    parser.add_argument(
        "--output-file",
        type=Path,
        default=None,
        help="Output JSON report file (default: failures-dir/analysis-report.json)"
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        help="Show detailed progress"
    )
    parser.add_argument(
        "--json-only",
        action="store_true",
        help="Only output JSON report, no human-readable text"
    )

    args = parser.parse_args()

    if args.output_file is None:
        args.output_file = args.failures_dir / "analysis-report.json"

    if not args.failures_dir.exists():
        print(f"Error: Failures directory not found: {args.failures_dir}", file=sys.stderr)
        print("Extract failure archives first.", file=sys.stderr)
        sys.exit(1)

    if args.verbose:
        print(f"Loading comparison results from {args.failures_dir}")

    # Load all comparison results
    results = load_comparison_results(args.failures_dir)

    if not results:
        print("Error: No comparison results found", file=sys.stderr)
        sys.exit(1)

    if args.verbose:
        print(f"Loaded results from {len(results)} version/OS combinations")
        for (ver, os_), _ in sorted(results.items()):
            print(f"  - Lua {ver} on {os_}")

    # Build failure matrix
    failure_matrix = build_failure_matrix(results)

    if args.verbose:
        print(f"\nFound {len(failure_matrix)} fixtures with mismatches")

    # Generate report
    report = generate_report(failure_matrix, args.fixtures_dir)

    # Write JSON report
    args.output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(args.output_file, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2)

    if not args.json_only:
        print_report(report)
        print(f"\nJSON report written to: {args.output_file}")
    else:
        print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()

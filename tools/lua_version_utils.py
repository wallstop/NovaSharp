#!/usr/bin/env python3
"""
lua_version_utils.py - Shared utilities for parsing Lua version metadata.

This module provides functions for parsing and manipulating Lua version strings
used in fixture metadata. It supports:
- Explicit lists: "5.1, 5.2, 5.3"
- Range syntax: "5.2-5.4"
- Open-ended ranges: "5.3+"
- All versions: "all"

Usage:
    from lua_version_utils import (
        parse_lua_versions,
        is_version_compatible,
        expand_version_range,
        simplify_version_list,
    )
"""

from __future__ import annotations

import re
from typing import Optional

__all__ = [
    'ALL_LUA_VERSIONS',
    'parse_lua_versions',
    'is_version_compatible',
    'expand_version_range',
    'simplify_version_list',
    'normalize_version',
    'compare_versions',
    'get_version_gaps',
]

# All known Lua versions in order
ALL_LUA_VERSIONS = ["5.1", "5.2", "5.3", "5.4", "5.5"]

# Version order map for comparisons
VERSION_ORDER = {v: i for i, v in enumerate(ALL_LUA_VERSIONS)}


def normalize_version(version: str) -> str:
    """
    Normalize a version string to canonical form (e.g., "5.1").

    Handles:
    - "5.1" -> "5.1"
    - "51" -> "5.1"
    - "lua5.1" -> "5.1"
    - "Lua 5.1" -> "5.1"

    Args:
        version: Version string to normalize

    Returns:
        Normalized version string (e.g., "5.1")

    Raises:
        ValueError: If version string cannot be parsed
    """
    version = version.strip().lower()

    # Handle "lua5.1" or "lua 5.1" format
    version = re.sub(r'^lua\s*', '', version)

    # Handle "51" format (no dot)
    if re.match(r'^\d{2}$', version):
        version = f"{version[0]}.{version[1]}"

    # Validate format
    if not re.match(r'^\d+\.\d+$', version):
        raise ValueError(f"Invalid version format: {version!r}")

    return version


def parse_lua_versions(version_string: str) -> list[str]:
    """
    Parse a Lua version specification string into a list of explicit versions.

    Supports multiple formats:
    - Explicit list: "5.1, 5.2, 5.3" -> ["5.1", "5.2", "5.3"]
    - Range syntax: "5.2-5.4" -> ["5.2", "5.3", "5.4"]
    - Open-ended: "5.3+" -> ["5.3", "5.4", "5.5"]
    - All versions: "all" -> ["5.1", "5.2", "5.3", "5.4", "5.5"]
    - Mixed: "5.1, 5.3+" -> ["5.1", "5.3", "5.4", "5.5"]
    - Empty/whitespace: "" -> []

    Args:
        version_string: The version specification string to parse

    Returns:
        A sorted list of explicit version strings (e.g., ["5.1", "5.2"])
    """
    if not version_string or not version_string.strip():
        return []

    version_string = version_string.strip().lower()

    # Handle "all" keyword
    if version_string == "all":
        return list(ALL_LUA_VERSIONS)

    # Handle "novasharp-only" as empty (no Lua versions)
    if "novasharp-only" in version_string:
        return []

    result = set()

    # Split by comma and process each part
    parts = [p.strip() for p in version_string.split(",")]

    for part in parts:
        if not part:
            continue

        # Check for range syntax (5.2-5.4)
        range_match = re.match(r'^(\d+\.\d+)\s*-\s*(\d+\.\d+)$', part)
        if range_match:
            start_ver = normalize_version(range_match.group(1))
            end_ver = normalize_version(range_match.group(2))
            result.update(expand_version_range(f"{start_ver}-{end_ver}"))
            continue

        # Check for open-ended range (5.3+)
        plus_match = re.match(r'^(\d+\.\d+)\+$', part)
        if plus_match:
            start_ver = normalize_version(plus_match.group(1))
            result.update(expand_version_range(f"{start_ver}+"))
            continue

        # Check for negative range (e.g., "-5.2" means up to and including 5.2)
        neg_match = re.match(r'^-(\d+\.\d+)$', part)
        if neg_match:
            end_ver = normalize_version(neg_match.group(1))
            result.update(expand_version_range(f"-{end_ver}"))
            continue

        # Regular explicit version
        try:
            normalized = normalize_version(part)
            if normalized in VERSION_ORDER:
                result.add(normalized)
        except ValueError:
            # Skip invalid versions
            continue

    # Return sorted list
    return sorted(result, key=lambda v: VERSION_ORDER.get(v, 999))


def expand_version_range(range_str: str) -> list[str]:
    """
    Expand a version range specification into explicit versions.

    Supports:
    - "5.2-5.4" -> ["5.2", "5.3", "5.4"]
    - "5.3+" -> ["5.3", "5.4", "5.5"]
    - "-5.2" -> ["5.1", "5.2"]
    - "5.3" -> ["5.3"] (single version)

    Args:
        range_str: The range specification string

    Returns:
        A list of version strings in the range

    Raises:
        ValueError: If the range format is invalid
    """
    range_str = range_str.strip()

    # Handle closed range (5.2-5.4)
    closed_match = re.match(r'^(\d+\.\d+)\s*-\s*(\d+\.\d+)$', range_str)
    if closed_match:
        start_ver = normalize_version(closed_match.group(1))
        end_ver = normalize_version(closed_match.group(2))

        if start_ver not in VERSION_ORDER or end_ver not in VERSION_ORDER:
            raise ValueError(f"Unknown version in range: {range_str}")

        start_idx = VERSION_ORDER[start_ver]
        end_idx = VERSION_ORDER[end_ver]

        if start_idx > end_idx:
            raise ValueError(f"Invalid range (start > end): {range_str}")

        return ALL_LUA_VERSIONS[start_idx:end_idx + 1]

    # Handle open-ended range (5.3+)
    plus_match = re.match(r'^(\d+\.\d+)\+$', range_str)
    if plus_match:
        start_ver = normalize_version(plus_match.group(1))

        if start_ver not in VERSION_ORDER:
            raise ValueError(f"Unknown version: {start_ver}")

        start_idx = VERSION_ORDER[start_ver]
        return ALL_LUA_VERSIONS[start_idx:]

    # Handle negative range (-5.2)
    neg_match = re.match(r'^-(\d+\.\d+)$', range_str)
    if neg_match:
        end_ver = normalize_version(neg_match.group(1))

        if end_ver not in VERSION_ORDER:
            raise ValueError(f"Unknown version: {end_ver}")

        end_idx = VERSION_ORDER[end_ver]
        return ALL_LUA_VERSIONS[:end_idx + 1]

    # Single version
    try:
        ver = normalize_version(range_str)
        if ver in VERSION_ORDER:
            return [ver]
        raise ValueError(f"Unknown version: {ver}")
    except ValueError as e:
        # Re-raise if it's already a specific error message
        if "Unknown version" in str(e):
            raise
        raise ValueError(f"Invalid range format: {range_str}")


def is_version_compatible(lua_versions: list[str], target: str) -> bool:
    """
    Check if a target Lua version is compatible with a list of specified versions.

    Args:
        lua_versions: List of Lua versions from fixture metadata.
                     Empty list means compatible with all versions.
        target: The target Lua version to check (e.g., "5.4")

    Returns:
        True if the target is compatible, False otherwise.
    """
    if not lua_versions:
        return True  # Empty list means all versions

    try:
        normalized_target = normalize_version(target)
    except ValueError:
        return False

    return normalized_target in lua_versions


def simplify_version_list(versions: list[str]) -> str:
    """
    Simplify a list of versions into the most concise representation.

    Examples:
    - ["5.3", "5.4", "5.5"] -> "5.3+"
    - ["5.2", "5.3", "5.4"] -> "5.2-5.4"
    - ["5.1", "5.2", "5.3", "5.4", "5.5"] -> "all"
    - ["5.1", "5.3"] -> "5.1, 5.3"
    - [] -> ""

    Args:
        versions: List of version strings to simplify

    Returns:
        A simplified version specification string
    """
    if not versions:
        return ""

    # Normalize and sort versions
    try:
        normalized = sorted(
            [normalize_version(v) for v in versions if v.strip()],
            key=lambda v: VERSION_ORDER.get(v, 999)
        )
    except ValueError:
        return ", ".join(versions)

    # Filter to only known versions
    normalized = [v for v in normalized if v in VERSION_ORDER]

    if not normalized:
        return ""

    # Check if it's all versions
    if normalized == ALL_LUA_VERSIONS:
        return "all"

    # Check if it's a contiguous range
    indices = [VERSION_ORDER[v] for v in normalized]

    if len(indices) > 1:
        is_contiguous = all(
            indices[i] + 1 == indices[i + 1]
            for i in range(len(indices) - 1)
        )

        if is_contiguous:
            # Check if it extends to the end (can use +)
            if indices[-1] == len(ALL_LUA_VERSIONS) - 1:
                if len(normalized) > 1:
                    return f"{normalized[0]}+"
                else:
                    return normalized[0]
            else:
                # Closed range
                return f"{normalized[0]}-{normalized[-1]}"

    # Non-contiguous or single version - use explicit list
    return ", ".join(normalized)


def get_version_gaps(versions: list[str]) -> list[str]:
    """
    Get the versions that are NOT in the given list.

    Args:
        versions: List of versions that are supported

    Returns:
        List of versions that are NOT supported
    """
    try:
        normalized = {normalize_version(v) for v in versions}
    except ValueError:
        return []

    return [v for v in ALL_LUA_VERSIONS if v not in normalized]


def compare_versions(v1: str, v2: str) -> int:
    """
    Compare two version strings.

    Args:
        v1: First version
        v2: Second version

    Returns:
        -1 if v1 < v2, 0 if equal, 1 if v1 > v2
    """
    try:
        n1 = normalize_version(v1)
        n2 = normalize_version(v2)
    except ValueError:
        return 0

    idx1 = VERSION_ORDER.get(n1, -1)
    idx2 = VERSION_ORDER.get(n2, -1)

    if idx1 < idx2:
        return -1
    elif idx1 > idx2:
        return 1
    return 0


if __name__ == "__main__":
    # Quick test/demo
    print("Lua Version Utils Demo")
    print("=" * 50)

    test_cases = [
        "5.1, 5.2, 5.3",
        "5.2-5.4",
        "5.3+",
        "all",
        "-5.2",
        "5.1, 5.3+",
        "",
        "novasharp-only",
    ]

    for tc in test_cases:
        result = parse_lua_versions(tc)
        simplified = simplify_version_list(result)
        print(f"  {tc!r:25} -> {result!r:40} -> {simplified!r}")

    print()
    print("Simplification examples:")
    simplify_cases = [
        ["5.3", "5.4", "5.5"],
        ["5.2", "5.3", "5.4"],
        ["5.1", "5.2", "5.3", "5.4", "5.5"],
        ["5.1", "5.3"],
        ["5.4"],
    ]

    for tc in simplify_cases:
        simplified = simplify_version_list(tc)
        print(f"  {tc!r:40} -> {simplified!r}")

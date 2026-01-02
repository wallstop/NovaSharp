#!/usr/bin/env python3
"""
Unit tests for lua_version_utils.py.

Run with: python3 -m pytest tools/test_lua_version_utils.py -v
Or:       python3 tools/test_lua_version_utils.py
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

# Add tools directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from lua_version_utils import (
    ALL_LUA_VERSIONS,
    compare_versions,
    expand_version_range,
    get_version_gaps,
    is_version_compatible,
    normalize_version,
    parse_lua_versions,
    simplify_version_list,
)


class TestNormalizeVersion(unittest.TestCase):
    """Tests for normalize_version function."""

    def test_standard_format(self):
        """Standard 5.X format should pass through unchanged."""
        self.assertEqual(normalize_version("5.1"), "5.1")
        self.assertEqual(normalize_version("5.4"), "5.4")
        self.assertEqual(normalize_version("5.5"), "5.5")

    def test_whitespace_handling(self):
        """Whitespace should be stripped."""
        self.assertEqual(normalize_version("  5.1  "), "5.1")
        self.assertEqual(normalize_version("\t5.4\n"), "5.4")

    def test_case_insensitive(self):
        """Should handle case variations."""
        self.assertEqual(normalize_version("LUA5.1"), "5.1")
        self.assertEqual(normalize_version("Lua 5.4"), "5.4")
        self.assertEqual(normalize_version("lua5.3"), "5.3")

    def test_compact_format(self):
        """Handle 51 format (no dot)."""
        self.assertEqual(normalize_version("51"), "5.1")
        self.assertEqual(normalize_version("54"), "5.4")

    def test_invalid_format_raises(self):
        """Invalid formats should raise ValueError."""
        with self.assertRaises(ValueError):
            normalize_version("5")
        with self.assertRaises(ValueError):
            normalize_version("5.1.0")
        with self.assertRaises(ValueError):
            normalize_version("invalid")
        with self.assertRaises(ValueError):
            normalize_version("")


class TestParseLuaVersions(unittest.TestCase):
    """Tests for parse_lua_versions function."""

    def test_explicit_list(self):
        """Parse comma-separated explicit versions."""
        self.assertEqual(
            parse_lua_versions("5.1, 5.2, 5.3"),
            ["5.1", "5.2", "5.3"]
        )
        self.assertEqual(
            parse_lua_versions("5.4, 5.1"),  # Should sort
            ["5.1", "5.4"]
        )

    def test_closed_range(self):
        """Parse closed range syntax (5.2-5.4)."""
        self.assertEqual(
            parse_lua_versions("5.2-5.4"),
            ["5.2", "5.3", "5.4"]
        )
        self.assertEqual(
            parse_lua_versions("5.1-5.3"),
            ["5.1", "5.2", "5.3"]
        )

    def test_open_ended_range(self):
        """Parse open-ended range syntax (5.3+)."""
        self.assertEqual(
            parse_lua_versions("5.3+"),
            ["5.3", "5.4", "5.5"]
        )
        self.assertEqual(
            parse_lua_versions("5.1+"),
            ALL_LUA_VERSIONS
        )
        self.assertEqual(
            parse_lua_versions("5.5+"),
            ["5.5"]
        )

    def test_negative_range(self):
        """Parse negative range syntax (-5.2)."""
        self.assertEqual(
            parse_lua_versions("-5.2"),
            ["5.1", "5.2"]
        )
        self.assertEqual(
            parse_lua_versions("-5.4"),
            ["5.1", "5.2", "5.3", "5.4"]
        )

    def test_all_keyword(self):
        """Parse 'all' keyword."""
        self.assertEqual(parse_lua_versions("all"), ALL_LUA_VERSIONS)
        self.assertEqual(parse_lua_versions("ALL"), ALL_LUA_VERSIONS)

    def test_mixed_format(self):
        """Parse mixed formats."""
        result = parse_lua_versions("5.1, 5.3+")
        self.assertEqual(result, ["5.1", "5.3", "5.4", "5.5"])

    def test_empty_and_whitespace(self):
        """Empty strings should return empty list."""
        self.assertEqual(parse_lua_versions(""), [])
        self.assertEqual(parse_lua_versions("   "), [])
        self.assertEqual(parse_lua_versions(None), [])

    def test_novasharp_only(self):
        """novasharp-only should return empty list."""
        self.assertEqual(parse_lua_versions("novasharp-only"), [])
        self.assertEqual(parse_lua_versions("NOVASHARP-ONLY"), [])

    def test_duplicates_removed(self):
        """Duplicate versions should be removed."""
        self.assertEqual(
            parse_lua_versions("5.1, 5.1, 5.2"),
            ["5.1", "5.2"]
        )
        self.assertEqual(
            parse_lua_versions("5.1, 5.1-5.2"),
            ["5.1", "5.2"]
        )

    def test_invalid_versions_skipped(self):
        """Invalid versions should be skipped gracefully."""
        self.assertEqual(
            parse_lua_versions("5.1, invalid, 5.2"),
            ["5.1", "5.2"]
        )


class TestExpandVersionRange(unittest.TestCase):
    """Tests for expand_version_range function."""

    def test_closed_range(self):
        """Expand closed range."""
        self.assertEqual(
            expand_version_range("5.2-5.4"),
            ["5.2", "5.3", "5.4"]
        )

    def test_open_ended_range(self):
        """Expand open-ended range."""
        self.assertEqual(
            expand_version_range("5.3+"),
            ["5.3", "5.4", "5.5"]
        )

    def test_negative_range(self):
        """Expand negative range."""
        self.assertEqual(
            expand_version_range("-5.2"),
            ["5.1", "5.2"]
        )

    def test_single_version(self):
        """Single version returns list with that version."""
        self.assertEqual(expand_version_range("5.3"), ["5.3"])

    def test_invalid_range_start_greater_than_end(self):
        """Range with start > end should raise."""
        with self.assertRaises(ValueError):
            expand_version_range("5.4-5.2")

    def test_unknown_version_raises(self):
        """Unknown versions should raise."""
        with self.assertRaises(ValueError):
            expand_version_range("5.9+")
        with self.assertRaises(ValueError):
            expand_version_range("5.0-5.2")


class TestIsVersionCompatible(unittest.TestCase):
    """Tests for is_version_compatible function."""

    def test_empty_list_compatible_with_all(self):
        """Empty version list means compatible with all."""
        self.assertTrue(is_version_compatible([], "5.1"))
        self.assertTrue(is_version_compatible([], "5.4"))
        self.assertTrue(is_version_compatible([], "5.5"))

    def test_explicit_version_match(self):
        """Version in list should be compatible."""
        self.assertTrue(is_version_compatible(["5.1", "5.2"], "5.1"))
        self.assertTrue(is_version_compatible(["5.1", "5.2"], "5.2"))
        self.assertFalse(is_version_compatible(["5.1", "5.2"], "5.3"))

    def test_target_normalization(self):
        """Target should be normalized."""
        self.assertTrue(is_version_compatible(["5.1"], "5.1"))
        self.assertTrue(is_version_compatible(["5.1"], " 5.1 "))

    def test_invalid_target(self):
        """Invalid target should return False."""
        self.assertFalse(is_version_compatible(["5.1"], "invalid"))


class TestSimplifyVersionList(unittest.TestCase):
    """Tests for simplify_version_list function."""

    def test_all_versions(self):
        """All versions should simplify to 'all'."""
        self.assertEqual(
            simplify_version_list(ALL_LUA_VERSIONS),
            "all"
        )

    def test_open_ended_range(self):
        """Versions to end should use + notation."""
        self.assertEqual(
            simplify_version_list(["5.3", "5.4", "5.5"]),
            "5.3+"
        )
        self.assertEqual(
            simplify_version_list(["5.4", "5.5"]),
            "5.4+"
        )

    def test_closed_range(self):
        """Contiguous versions not at end should use range notation."""
        self.assertEqual(
            simplify_version_list(["5.2", "5.3", "5.4"]),
            "5.2-5.4"
        )
        self.assertEqual(
            simplify_version_list(["5.1", "5.2", "5.3"]),
            "5.1-5.3"
        )

    def test_non_contiguous(self):
        """Non-contiguous versions should use explicit list."""
        self.assertEqual(
            simplify_version_list(["5.1", "5.3"]),
            "5.1, 5.3"
        )
        self.assertEqual(
            simplify_version_list(["5.1", "5.4"]),
            "5.1, 5.4"
        )

    def test_single_version(self):
        """Single version should return just that version."""
        self.assertEqual(simplify_version_list(["5.4"]), "5.4")
        self.assertEqual(simplify_version_list(["5.1"]), "5.1")

    def test_empty_list(self):
        """Empty list should return empty string."""
        self.assertEqual(simplify_version_list([]), "")


class TestGetVersionGaps(unittest.TestCase):
    """Tests for get_version_gaps function."""

    def test_no_gaps(self):
        """All versions should have no gaps."""
        self.assertEqual(get_version_gaps(ALL_LUA_VERSIONS), [])

    def test_some_gaps(self):
        """Should identify missing versions."""
        self.assertEqual(
            get_version_gaps(["5.1", "5.3", "5.5"]),
            ["5.2", "5.4"]
        )

    def test_empty_list(self):
        """Empty list should return all versions as gaps."""
        self.assertEqual(get_version_gaps([]), ALL_LUA_VERSIONS)


class TestCompareVersions(unittest.TestCase):
    """Tests for compare_versions function."""

    def test_less_than(self):
        """Earlier versions should compare less."""
        self.assertEqual(compare_versions("5.1", "5.2"), -1)
        self.assertEqual(compare_versions("5.2", "5.4"), -1)

    def test_greater_than(self):
        """Later versions should compare greater."""
        self.assertEqual(compare_versions("5.4", "5.2"), 1)
        self.assertEqual(compare_versions("5.5", "5.1"), 1)

    def test_equal(self):
        """Same versions should compare equal."""
        self.assertEqual(compare_versions("5.4", "5.4"), 0)

    def test_invalid_returns_zero(self):
        """Invalid versions should return 0."""
        self.assertEqual(compare_versions("invalid", "5.4"), 0)


class TestRoundTrip(unittest.TestCase):
    """Tests for round-trip parsing and simplification."""

    def test_roundtrip_explicit(self):
        """Explicit list should round-trip correctly."""
        original = "5.1, 5.2"
        parsed = parse_lua_versions(original)
        simplified = simplify_version_list(parsed)
        reparsed = parse_lua_versions(simplified)
        self.assertEqual(parsed, reparsed)

    def test_roundtrip_range(self):
        """Range should round-trip correctly."""
        original = "5.2-5.4"
        parsed = parse_lua_versions(original)
        simplified = simplify_version_list(parsed)
        reparsed = parse_lua_versions(simplified)
        self.assertEqual(parsed, reparsed)

    def test_roundtrip_plus(self):
        """Open-ended range should round-trip correctly."""
        original = "5.3+"
        parsed = parse_lua_versions(original)
        simplified = simplify_version_list(parsed)
        reparsed = parse_lua_versions(simplified)
        self.assertEqual(parsed, reparsed)


if __name__ == "__main__":
    unittest.main(verbosity=2)

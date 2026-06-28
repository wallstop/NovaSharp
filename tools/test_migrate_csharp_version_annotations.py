#!/usr/bin/env python3
"""
Unit tests for migrate_csharp_version_annotations.py.

Run with: python3 -m pytest tools/test_migrate_csharp_version_annotations.py -v
Or:       python3 tools/test_migrate_csharp_version_annotations.py
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

# Add tools directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from migrate_csharp_version_annotations import (
    ARGUMENTS_PATTERN,
    normalize_version,
    is_contiguous,
    determine_replacement,
    find_attribute_groups,
    find_csharp_test_files,
)


class TestNormalizeVersion(unittest.TestCase):
    """Tests for normalize_version function."""

    def test_long_form_to_short(self):
        """Long form should be converted to short form."""
        self.assertEqual(normalize_version("LuaCompatibilityVersion.Lua53"), "Lua53")
        self.assertEqual(normalize_version("LuaCompatibilityVersion.Lua51"), "Lua51")

    def test_short_form_passthrough(self):
        """Short form should pass through unchanged."""
        self.assertEqual(normalize_version("Lua53"), "Lua53")
        self.assertEqual(normalize_version("Lua55"), "Lua55")


class TestIsContiguous(unittest.TestCase):
    """Tests for is_contiguous function."""

    def test_contiguous_ranges(self):
        """Contiguous version ranges should return True."""
        self.assertTrue(is_contiguous(["Lua51", "Lua52", "Lua53"]))
        self.assertTrue(is_contiguous(["Lua53", "Lua54", "Lua55"]))
        self.assertTrue(is_contiguous(["Lua51", "Lua52", "Lua53", "Lua54", "Lua55"]))

    def test_single_version(self):
        """Single version should be considered contiguous."""
        self.assertTrue(is_contiguous(["Lua51"]))

    def test_empty_list(self):
        """Empty list should be considered contiguous."""
        self.assertTrue(is_contiguous([]))

    def test_non_contiguous_gap_52(self):
        """Gap at 5.2 should return False."""
        self.assertFalse(is_contiguous(["Lua51", "Lua53"]))

    def test_non_contiguous_multiple_gaps(self):
        """Multiple gaps should return False."""
        self.assertFalse(is_contiguous(["Lua51", "Lua53", "Lua55"]))

    def test_non_contiguous_gap_53(self):
        """Gap at 5.3 should return False."""
        self.assertFalse(is_contiguous(["Lua52", "Lua54"]))


class TestDetermineReplacement(unittest.TestCase):
    """Tests for determine_replacement function."""

    def test_all_versions(self):
        """All versions should return [AllLuaVersions]."""
        self.assertEqual(
            determine_replacement(["Lua51", "Lua52", "Lua53", "Lua54", "Lua55"]),
            "[AllLuaVersions]"
        )

    def test_from_lua53(self):
        """5.3+ should return [LuaVersionsFrom(Lua53)]."""
        self.assertEqual(
            determine_replacement(["Lua53", "Lua54", "Lua55"]),
            "[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]"
        )

    def test_from_lua52(self):
        """5.2+ should return [LuaVersionsFrom(Lua52)]."""
        self.assertEqual(
            determine_replacement(["Lua52", "Lua53", "Lua54", "Lua55"]),
            "[LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]"
        )

    def test_from_lua54(self):
        """5.4+ should return [LuaVersionsFrom(Lua54)]."""
        self.assertEqual(
            determine_replacement(["Lua54", "Lua55"]),
            "[LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]"
        )

    def test_until_lua52(self):
        """5.1-5.2 should return [LuaVersionsUntil(Lua52)]."""
        self.assertEqual(
            determine_replacement(["Lua51", "Lua52"]),
            "[LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]"
        )

    def test_until_lua53(self):
        """5.1-5.3 should return [LuaVersionsUntil(Lua53)]."""
        self.assertEqual(
            determine_replacement(["Lua51", "Lua52", "Lua53"]),
            "[LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]"
        )

    def test_range_52_to_54(self):
        """5.2-5.4 should return [LuaVersionRange(Lua52, Lua54)]."""
        self.assertEqual(
            determine_replacement(["Lua52", "Lua53", "Lua54"]),
            "[LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua54)]"
        )

    def test_range_52_to_53(self):
        """5.2-5.3 should return [LuaVersionRange(Lua52, Lua53)]."""
        self.assertEqual(
            determine_replacement(["Lua52", "Lua53"]),
            "[LuaVersionRange(LuaCompatibilityVersion.Lua52, LuaCompatibilityVersion.Lua53)]"
        )

    def test_non_contiguous_returns_none(self):
        """Non-contiguous versions should return None."""
        self.assertIsNone(determine_replacement(["Lua51", "Lua53"]))
        self.assertIsNone(determine_replacement(["Lua51", "Lua53", "Lua55"]))

    def test_single_version_returns_none(self):
        """Single version should return None (not worth converting)."""
        self.assertIsNone(determine_replacement(["Lua53"]))

    def test_empty_returns_none(self):
        """Empty list should return None."""
        self.assertIsNone(determine_replacement([]))


class TestFindCSharpTestFiles(unittest.TestCase):
    """Tests for C# test file discovery."""

    def test_excludes_generated_trees(self):
        """Generated bin/obj/artifacts trees should not be scanned."""
        with self.subTest("generated dirs are skipped"):
            import tempfile

            with tempfile.TemporaryDirectory() as temp_dir:
                root = Path(temp_dir)
                source_dir = root / "src" / "tests" / "Project.TUnit"
                source_dir.mkdir(parents=True)
                source_file = source_dir / "SampleTests.cs"
                source_file.write_text("// source\n", encoding="utf-8")

                for generated_dir in ("bin", "obj", "artifacts"):
                    generated_path = source_dir / generated_dir / "Debug" / "GeneratedTests.cs"
                    generated_path.parent.mkdir(parents=True)
                    generated_path.write_text("// generated\n", encoding="utf-8")

                self.assertEqual(find_csharp_test_files(root), [source_file])


class TestArgumentsPattern(unittest.TestCase):
    """Tests for ARGUMENTS_PATTERN regex."""

    def test_long_form_match(self):
        """Long form arguments should match."""
        match = ARGUMENTS_PATTERN.match("        [Arguments(LuaCompatibilityVersion.Lua53)]")
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "LuaCompatibilityVersion.Lua53")
        self.assertIsNone(match.group(2))

    def test_short_form_match(self):
        """Short form arguments should match."""
        match = ARGUMENTS_PATTERN.match("        [Arguments(Lua53)]")
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "Lua53")
        self.assertIsNone(match.group(2))

    def test_different_indentation(self):
        """Various indentation levels should match."""
        match = ARGUMENTS_PATTERN.match("    [Arguments(LuaCompatibilityVersion.Lua51)]")
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "LuaCompatibilityVersion.Lua51")

    def test_no_indentation(self):
        """No indentation should match."""
        match = ARGUMENTS_PATTERN.match("[Arguments(Lua55)]")
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "Lua55")

    def test_extra_params_string(self):
        """Extra string parameters should be captured."""
        match = ARGUMENTS_PATTERN.match('        [Arguments(LuaCompatibilityVersion.Lua53, "extra")]')
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "LuaCompatibilityVersion.Lua53")
        self.assertEqual(match.group(2), '"extra"')

    def test_extra_params_number(self):
        """Extra numeric parameters should be captured."""
        match = ARGUMENTS_PATTERN.match("        [Arguments(Lua53, 42)]")
        self.assertIsNotNone(match)
        self.assertEqual(match.group(1), "Lua53")
        self.assertEqual(match.group(2), "42")

    def test_test_attribute_no_match(self):
        """[Test] attribute should not match."""
        match = ARGUMENTS_PATTERN.match("        [Test]")
        self.assertIsNone(match)

    def test_all_lua_versions_no_match(self):
        """[AllLuaVersions] attribute should not match."""
        match = ARGUMENTS_PATTERN.match("        [AllLuaVersions]")
        self.assertIsNone(match)

    def test_method_signature_no_match(self):
        """Method signatures should not match."""
        match = ARGUMENTS_PATTERN.match("        public async Task Foo()")
        self.assertIsNone(match)


class TestFindAttributeGroups(unittest.TestCase):
    """Tests for find_attribute_groups function."""

    def test_simple_contiguous_group(self):
        """Simple contiguous group should be detected and convertible."""
        content = [
            "        [Test]\n",
            "        [Arguments(LuaCompatibilityVersion.Lua51)]\n",
            "        [Arguments(LuaCompatibilityVersion.Lua52)]\n",
            "        [Arguments(LuaCompatibilityVersion.Lua53)]\n",
            "        public async Task Foo(LuaCompatibilityVersion v)\n",
            "        {\n",
            "        }\n",
        ]

        groups = find_attribute_groups(content)

        self.assertEqual(len(groups), 1)
        self.assertEqual(groups[0].versions, ["Lua51", "Lua52", "Lua53"])
        self.assertTrue(groups[0].can_convert)
        self.assertEqual(
            groups[0].new_attribute,
            "[LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]"
        )

    def test_extra_params_not_convertible(self):
        """Group with extra parameters should not be convertible."""
        content = [
            '        [Arguments(Lua53, "extra")]\n',
            '        [Arguments(Lua54, "extra")]\n',
            "        public async Task Bar(LuaCompatibilityVersion v, string s)\n",
        ]

        groups = find_attribute_groups(content)

        self.assertEqual(len(groups), 1)
        self.assertTrue(groups[0].has_extra_params)
        self.assertFalse(groups[0].can_convert)

    def test_non_contiguous_not_convertible(self):
        """Non-contiguous versions should not be convertible."""
        content = [
            "        [Arguments(Lua51)]\n",
            "        [Arguments(Lua53)]\n",
            "        [Arguments(Lua55)]\n",
            "        public async Task Baz(LuaCompatibilityVersion v)\n",
        ]

        groups = find_attribute_groups(content)

        self.assertEqual(len(groups), 1)
        self.assertFalse(groups[0].is_contiguous)
        self.assertFalse(groups[0].can_convert)

    def test_multiple_groups_in_file(self):
        """Multiple groups in one file should be detected separately."""
        content = [
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

        groups = find_attribute_groups(content)

        self.assertEqual(len(groups), 2)
        self.assertEqual(groups[0].versions, ["Lua51", "Lua52"])
        self.assertEqual(groups[1].versions, ["Lua53", "Lua54", "Lua55"])

    def test_all_five_versions(self):
        """All five versions should result in [AllLuaVersions]."""
        content = [
            "        [Arguments(Lua51)]\n",
            "        [Arguments(Lua52)]\n",
            "        [Arguments(Lua53)]\n",
            "        [Arguments(Lua54)]\n",
            "        [Arguments(Lua55)]\n",
            "        public async Task All(LuaCompatibilityVersion v) { }\n",
        ]

        groups = find_attribute_groups(content)

        self.assertEqual(len(groups), 1)
        self.assertEqual(groups[0].new_attribute, "[AllLuaVersions]")


class TestDuplicateVersions(unittest.TestCase):
    """Tests for handling duplicate versions in input."""

    def test_determine_replacement_removes_duplicates(self):
        """Duplicate versions should be removed and still produce correct result."""
        # All versions with duplicates
        self.assertEqual(
            determine_replacement(["Lua51", "Lua51", "Lua52", "Lua53", "Lua54", "Lua55"]),
            "[AllLuaVersions]"
        )

    def test_determine_replacement_unsorted_input(self):
        """Unsorted input should be handled correctly."""
        self.assertEqual(
            determine_replacement(["Lua55", "Lua53", "Lua54"]),
            "[LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]"
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)

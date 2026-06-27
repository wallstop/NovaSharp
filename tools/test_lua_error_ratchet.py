#!/usr/bin/env python3
"""
Unit tests for lua_error_ratchet.py.

Run with: python3 tools/test_lua_error_ratchet.py
"""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

from lua_error_ratchet import (
    BothErrorEntry,
    check_both_error_ratchet,
    comparison_report_has_full_statuses,
    compared_keys_from_comparison_report,
    load_baseline,
    normalize_error_text,
)


def make_entry(
    file: str,
    lua_version: str = "5.4",
    lua_error: str = "lua5.4: fixture.lua:1: expected failure",
    nova_error: str = "ScriptRuntimeException: fixture.lua:1: expected failure",
) -> BothErrorEntry:
    return BothErrorEntry.from_errors(
        file=file,
        lua_version=lua_version,
        lua_rc=1,
        nova_rc=1,
        lua_error=lua_error,
        nova_error=nova_error,
    )


class TestLoadBaseline(unittest.TestCase):
    def test_load_baseline_accepts_entries_list(self) -> None:
        data = {
            "schema": 1,
            "entries": [
                make_entry("Parser/BadSyntax.lua").to_json(),
            ],
        }

        baseline = load_baseline(data)

        self.assertEqual(len(baseline), 1)
        self.assertEqual(baseline[0].file, "Parser/BadSyntax.lua")


class TestCheckBothErrorRatchet(unittest.TestCase):
    def test_known_current_entry_passes(self) -> None:
        entry = make_entry("Parser/BadSyntax.lua")

        result = check_both_error_ratchet([entry], [entry])

        self.assertTrue(result.passed)
        self.assertEqual(result.new_entries, [])
        self.assertEqual(result.changed_entries, [])

    def test_new_entry_fails(self) -> None:
        result = check_both_error_ratchet([], [make_entry("Parser/NewBadSyntax.lua")])

        self.assertFalse(result.passed)
        self.assertEqual(len(result.new_entries), 1)

    def test_changed_entry_fails(self) -> None:
        baseline = [make_entry("Parser/BadSyntax.lua")]
        current = [
            make_entry(
                "Parser/BadSyntax.lua",
                nova_error="ScriptRuntimeException: fixture.lua:1: different failure",
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)
        self.assertEqual(len(result.changed_entries), 1)

    def test_platform_exit_code_difference_does_not_change_entry(self) -> None:
        baseline = [
            make_entry(
                "Parser/BadSyntax.lua",
                lua_error="/usr/bin/lua5.4: fixture.lua:1: expected failure",
            )
        ]
        current = [
            BothErrorEntry.from_errors(
                file="Parser/BadSyntax.lua",
                lua_version="5.4",
                lua_rc=1,
                nova_rc=-6,
                lua_error=".lua-cache/Windows/lua-5.4/bin/lua5.4.exe: fixture.lua:1: expected failure",
                nova_error="ScriptRuntimeException: fixture.lua:1: expected failure",
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_windows_fixture_separator_matches_baseline_key(self) -> None:
        baseline = [make_entry("Parser/BadSyntax.lua")]
        current = [make_entry(r"Parser\BadSyntax.lua")]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)
        self.assertEqual(current[0].file, "Parser/BadSyntax.lua")

    def test_path_qualified_lua_prefix_normalizes(self) -> None:
        expected = "lua: fixture.lua:<line>: expected failure"

        self.assertEqual(
            normalize_error_text("/usr/bin/lua5.4: fixture.lua:123: expected failure"),
            expected,
        )
        self.assertEqual(
            normalize_error_text(r"C:\Lua\5.4\lua5.4.exe: fixture.lua:123: expected failure"),
            expected,
        )

    def test_removed_entry_passes_and_reports_reduction(self) -> None:
        baseline = [make_entry("Parser/BadSyntax.lua")]

        result = check_both_error_ratchet(baseline, [])

        self.assertTrue(result.passed)
        self.assertEqual(len(result.removed_entries), 1)

    def test_missing_baseline_entry_fails_when_current_statuses_are_complete(self) -> None:
        baseline = [make_entry("Parser/BadSyntax.lua")]

        result = check_both_error_ratchet(
            baseline,
            [],
            current_keys=set(),
            current_versions={"5.4"},
        )

        self.assertFalse(result.passed)
        self.assertEqual(len(result.missing_entries), 1)

    def test_compared_baseline_entry_without_both_error_is_reduction(self) -> None:
        baseline = [make_entry("Parser/BadSyntax.lua")]

        result = check_both_error_ratchet(
            baseline,
            [],
            current_keys={("5.4", "Parser/BadSyntax.lua")},
            current_versions={"5.4"},
        )

        self.assertTrue(result.passed)
        self.assertEqual(len(result.removed_entries), 1)

    def test_skipped_report_status_is_not_a_reduction_key(self) -> None:
        data = {
            "lua_version": "5.4",
            "result_statuses": [
                {
                    "file": "Parser/BadSyntax.lua",
                    "lua_version": "5.4",
                    "status": "skipped",
                }
            ],
        }

        self.assertEqual(compared_keys_from_comparison_report(data), set())

    def test_missing_output_status_is_not_a_reduction_key(self) -> None:
        data = {
            "lua_version": "5.4",
            "result_statuses": [
                {
                    "file": "Parser/BadSyntax.lua",
                    "lua_version": "5.4",
                    "status": "missing_outputs",
                }
            ],
        }

        self.assertEqual(compared_keys_from_comparison_report(data), set())

    def test_legacy_report_without_result_statuses_is_not_full_coverage(self) -> None:
        data = {
            "lua_version": "5.4",
            "both_errors": [
                make_entry("Parser/BadSyntax.lua").to_json(),
            ],
        }

        self.assertFalse(comparison_report_has_full_statuses(data))


if __name__ == "__main__":
    unittest.main()

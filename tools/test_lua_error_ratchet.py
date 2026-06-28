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

    def test_windows_lua_exe_prefix_before_short_fixture_path_normalizes(self) -> None:
        self.assertEqual(
            normalize_error_text(
                "D:\\a\\NovaSharp\\NovaSharp\\.lua-cache\\Windows\\lua-5.4\\bin\\"
                "lua5.4.exe: .../LuaFixtures/GotoTUnitTests/"
                "GotoUndefinedLabelThrows.lua:8: no visible label 'there' for <goto> "
                "at line 7\n"
            ),
            "lua: LuaFixtures/GotoTUnitTests/GotoUndefinedLabelThrows.lua:<line>: "
            "no visible label 'there' for <goto> at line 7",
        )

    def test_fixture_path_normalization_does_not_rewrite_words(self) -> None:
        normalized = normalize_error_text(
            "Error should mention LuaFixtures, got: NotLuaFixtures/file.lua:12: nope"
        )

        self.assertIn("NotLuaFixtures/file.lua:<line>: nope", normalized)
        self.assertNotIn("got: LuaFixtures/file.lua", normalized)

    def test_cannot_open_absolute_workspace_path_normalizes(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/DoFileExecutesLoadedChunk.lua",
                lua_version="5.1",
                nova_error=(
                    "LuaFixtures/LoadModuleTUnitTests/DoFileExecutesLoadedChunk.lua:"
                    "(7,0-27): cannot open /workspaces/NovaSharp/script.lua: "
                    "No such file or directory [compatibility: Lua 5.1]"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/DoFileExecutesLoadedChunk.lua",
                lua_version="5.1",
                nova_error=(
                    "LuaFixtures/LoadModuleTUnitTests/DoFileExecutesLoadedChunk.lua:"
                    "(7,0-27): cannot open /home/runner/work/NovaSharp/NovaSharp/"
                    "script.lua: No such file or directory [compatibility: Lua 5.1]"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_non_workspace_absolute_path_stays_specific(self) -> None:
        self.assertIn(
            "cannot open /tmp/other-project/script.lua: No such file or directory",
            normalize_error_text(
                "cannot open /tmp/other-project/script.lua: No such file or directory"
            ),
        )

    def test_non_workspace_path_containing_repo_shape_stays_specific(self) -> None:
        self.assertIn(
            "cannot open /tmp/home/runner/work/NovaSharp/NovaSharp/script.lua",
            normalize_error_text(
                "cannot open /tmp/home/runner/work/NovaSharp/NovaSharp/script.lua"
            ),
        )

    def test_require_search_roots_normalize(self) -> None:
        baseline_lua_error = (
            "lua: LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua:20: "
            "module 'nonexistent_module_for_testing' not found:\n"
            "\tno field package.preload['nonexistent_module_for_testing']\n"
            "\tno file './nonexistent_module_for_testing.lua'\n"
            "\tno file '/usr/local/share/lua/5.3/nonexistent_module_for_testing.lua'\n"
            "\tno file '/usr/local/share/lua/5.3/nonexistent_module_for_testing/init.lua'\n"
            "\tno file '/usr/local/lib/lua/5.3/nonexistent_module_for_testing.so'\n"
            "\tno file '/usr/lib/lua/5.3/nonexistent_module_for_testing.so'\n"
            "\tno file '/usr/local/lib/lua/5.3/loadall.so'\n"
        )
        current_lua_error = (
            "/home/runner/work/NovaSharp/NovaSharp/.lua-cache/Linux/lua-5.3/"
            "bin/lua5.3: LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua:20: "
            "module 'nonexistent_module_for_testing' not found:\n"
            "\tno field package.preload['nonexistent_module_for_testing']\n"
            "\tno file '/usr/local/share/lua/5.3/nonexistent_module_for_testing.lua'\n"
            "\tno file '/usr/local/share/lua/5.3/nonexistent_module_for_testing/init.lua'\n"
            "\tno file '/usr/local/lib/lua/5.3/nonexistent_module_for_testing.so'\n"
            "\tno file '/usr/local/lib/lua/5.3/loadall.so'\n"
        )
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=baseline_lua_error,
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=current_lua_error,
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_require_duplicate_search_roots_are_host_noise(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/local/share/lua/5.3/missing/module.lua'\n"
                    "\tno file '/usr/share/lua/5.3/missing/module.lua'\n"
                    "\tno file '/usr/local/share/lua/5.3/missing/module/init.lua'\n"
                    "\tno file '/usr/share/lua/5.3/missing/module/init.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/opt/lua/share/lua/5.3/missing/module.lua'\n"
                    "\tno file '/opt/lua/share/lua/5.3/missing/module/init.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_require_search_missing_suffix_changes_signature(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/share/lua/5.3/missing/module.lua'\n"
                    "\tno file '/usr/share/lua/5.3/missing/module/init.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/share/lua/5.3/missing/module.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)

    def test_require_search_order_change_changes_signature(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/share/lua/5.3/missing/module.lua'\n"
                    "\tno file '/usr/share/lua/5.3/missing/module/init.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/share/lua/5.3/missing/module/init.lua'\n"
                    "\tno file '/usr/share/lua/5.3/missing/module.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)

    def test_require_loadall_suffix_stays_distinct_from_module_named_all(self) -> None:
        normalized = normalize_error_text(
            "lua: fixture.lua:1: module 'all' not found:\n"
            "\tno field package.preload['all']\n"
            "\tno file '/usr/lib/lua/5.3/all.so'\n"
            "\tno file '/usr/local/lib/lua/5.3/loadall.so'\n"
        )

        self.assertIn("no file '<search>/all.<native>'", normalized)
        self.assertIn("no file '<search>/loadall.<native>'", normalized)

    def test_require_native_library_extensions_are_platform_noise(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.4",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/lib/lua/5.4/missing/module.so'\n"
                    "\tno file '/usr/lib/lua/5.4/loadall.so'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.4",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file 'C:/lua/missing/module.dll'\n"
                    "\tno file 'C:/lua/loadall.dll'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_require_duplicate_native_extensions_are_host_noise(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.4",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/usr/lib/lua/5.4/missing/module.so'\n"
                    "\tno file '/usr/lib/lua/5.4/loadall.so'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.4",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file 'C:/lua/missing/module.so'\n"
                    "\tno file 'C:/lua/missing/module.dll'\n"
                    "\tno file 'C:/lua/loadall.so'\n"
                    "\tno file 'C:/lua/loadall.dll'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertTrue(result.passed)

    def test_require_short_module_name_keeps_lua_and_init_suffixes_distinct(self) -> None:
        normalized = normalize_error_text(
            "lua: fixture.lua:1: module 'a' not found:\n"
            "\tno field package.preload['a']\n"
            "\tno file '/usr/share/lua/5.3/a.lua'\n"
            "\tno file '/usr/share/lua/5.3/a/init.lua'\n"
        )

        self.assertIn("no file '<search>/a.lua'", normalized)
        self.assertIn("no file '<search>/a/init.lua'", normalized)

    def test_require_short_module_missing_init_suffix_changes_signature(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'a' not found:\n"
                    "\tno field package.preload['a']\n"
                    "\tno file '/usr/share/lua/5.3/a.lua'\n"
                    "\tno file '/usr/share/lua/5.3/a/init.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'a' not found:\n"
                    "\tno field package.preload['a']\n"
                    "\tno file '/usr/share/lua/5.3/a.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)

    def test_require_loadall_named_module_keeps_module_path(self) -> None:
        normalized = normalize_error_text(
            "lua: fixture.lua:1: module 'foo.loadall' not found:\n"
            "\tno field package.preload['foo.loadall']\n"
            "\tno file '/usr/share/lua/5.3/foo/loadall.lua'\n"
            "\tno file '/usr/share/lua/5.3/foo/loadall/init.lua'\n"
            "\tno file '/usr/lib/lua/5.3/foo/loadall.so'\n"
            "\tno file '/usr/local/lib/lua/5.3/loadall.so'\n"
        )

        self.assertIn("no file '<search>/foo/loadall.lua'", normalized)
        self.assertIn("no file '<search>/foo/loadall/init.lua'", normalized)
        self.assertIn("no file '<search>/foo/loadall.<native>'", normalized)
        self.assertIn("no file '<search>/loadall.<native>'", normalized)

    def test_require_custom_template_suffixes_stay_distinct(self) -> None:
        normalized = normalize_error_text(
            "lua: fixture.lua:1: module 'missing.module' not found:\n"
            "\tno field package.preload['missing.module']\n"
            "\tno file '/opt/lua/missing/module/main.lua'\n"
            "\tno file '/opt/lua/missing/module/other/main.lua'\n"
        )

        self.assertIn("no file '<search>/missing/module/main.lua'", normalized)
        self.assertIn("no file '<search>/missing/module/other/main.lua'", normalized)

    def test_require_custom_template_missing_suffix_changes_signature(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/opt/lua/missing/module/main.lua'\n"
                    "\tno file '/opt/lua/missing/module/other/main.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/opt/lua/missing/module/main.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)

    def test_require_embedded_template_prefix_stays_distinct(self) -> None:
        normalized = normalize_error_text(
            "lua: fixture.lua:1: module 'missing.module' not found:\n"
            "\tno field package.preload['missing.module']\n"
            "\tno file '/opt/lua/prefix_missing/module.lua'\n"
            "\tno file '/opt/lua/other_missing/module.lua'\n"
        )

        self.assertIn("no file '<search>/prefix_missing/module.lua'", normalized)
        self.assertIn("no file '<search>/other_missing/module.lua'", normalized)

    def test_require_embedded_template_prefix_change_changes_signature(self) -> None:
        baseline = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/opt/lua/prefix_missing/module.lua'\n"
                ),
            )
        ]
        current = [
            make_entry(
                "LoadModuleTUnitTests/RequireErrorListsSearchedPaths.lua",
                lua_version="5.3",
                lua_error=(
                    "lua: fixture.lua:1: module 'missing.module' not found:\n"
                    "\tno field package.preload['missing.module']\n"
                    "\tno file '/opt/lua/other_missing/module.lua'\n"
                ),
            )
        ]

        result = check_both_error_ratchet(baseline, current)

        self.assertFalse(result.passed)

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

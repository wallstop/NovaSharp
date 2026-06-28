#!/usr/bin/env python3
"""
Unit tests for scripts/tests/compare-lua-outputs.py.

Run with: python3 tools/test_compare_lua_outputs.py
"""

from __future__ import annotations

import json
import importlib.util
import shutil
import subprocess
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

from lua_error_ratchet import BothErrorEntry

COMPARE_SCRIPT = Path("scripts/tests/compare-lua-outputs.py")
COMPARE_SPEC = importlib.util.spec_from_file_location("compare_lua_outputs", COMPARE_SCRIPT)
assert COMPARE_SPEC is not None
compare_lua_outputs = importlib.util.module_from_spec(COMPARE_SPEC)
assert COMPARE_SPEC.loader is not None
COMPARE_SPEC.loader.exec_module(compare_lua_outputs)


class TestCompareLuaOutputs(unittest.TestCase):
    def setUp(self) -> None:
        self.root = Path("artifacts") / "test-compare-lua-outputs"
        if self.root.exists():
            shutil.rmtree(self.root)
        self.corpus_dir = self.root / "corpus"
        self.results_dir = self.root / "results"
        self.corpus_dir.mkdir(parents=True)
        self.results_dir.mkdir(parents=True)
        self.baseline = self.root / "lua-error-ratchet.json"
        self.output_file = self.root / "comparison.json"

    def tearDown(self) -> None:
        if self.root.exists():
            shutil.rmtree(self.root)

    def write_baseline(self, entries: list[BothErrorEntry]) -> None:
        self.baseline.write_text(
            json.dumps(
                {
                    "schema": 1,
                    "entries": [entry.to_json() for entry in entries],
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )

    def write_fixture(
        self,
        file: str,
        lua_rc: int | None,
        nova_rc: int | None,
        lua_output: str = "",
        nova_output: str = "",
        lua_error: str = "",
        nova_error: str = "",
        fixture_source: str = "return true\n",
    ) -> None:
        fixture_path = self.corpus_dir / file
        fixture_path.parent.mkdir(parents=True, exist_ok=True)
        fixture_path.write_text(fixture_source, encoding="utf-8")

        base = self.results_dir / file.replace(".lua", "")
        base.parent.mkdir(parents=True, exist_ok=True)

        if lua_rc is not None:
            base.with_suffix(".lua5.4.out").write_text(lua_output, encoding="utf-8")
            base.with_suffix(".lua5.4.err").write_text(lua_error, encoding="utf-8")
            base.with_suffix(".lua5.4.rc").write_text(str(lua_rc), encoding="utf-8")

        if nova_rc is not None:
            base.with_suffix(".nova.out").write_text(nova_output, encoding="utf-8")
            base.with_suffix(".nova.err").write_text(nova_error, encoding="utf-8")
            base.with_suffix(".nova.rc").write_text(str(nova_rc), encoding="utf-8")

    def run_compare(self) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                "scripts/tests/compare-lua-outputs.py",
                "--results-dir",
                str(self.results_dir),
                "--corpus-dir",
                str(self.corpus_dir),
                "--lua-version",
                "5.4",
                "--output-file",
                str(self.output_file),
                "--error-ratchet-baseline",
                str(self.baseline),
                "--enforce",
            ],
            capture_output=True,
            text=True,
            check=False,
        )

    def test_normalize_output_strips_windows_lua_executable_prefix(self) -> None:
        self.assertEqual(
            compare_lua_outputs.normalize_output(
                r"C:\Lua\5.4\lua5.4.exe: fixture.lua:12: expected failure"
            ),
            "fixture.lua:<line>: expected failure",
        )
        self.assertEqual(
            compare_lua_outputs.normalize_output(
                r"C:\Program Files\Lua\lua5.4.exe: fixture.lua:12: expected failure"
            ),
            "fixture.lua:<line>: expected failure",
        )

    def test_enforce_fails_on_mismatch(self) -> None:
        self.write_baseline([])
        self.write_fixture("Mismatch.lua", 0, 0, lua_output="lua\n", nova_output="nova\n")

        result = self.run_compare()

        self.assertNotEqual(result.returncode, 0)

    def test_enforce_fails_on_nova_only(self) -> None:
        self.write_baseline([])
        self.write_fixture("NovaOnly.lua", None, 0, nova_output="nova\n")

        result = self.run_compare()

        self.assertNotEqual(result.returncode, 0)

    def test_enforce_fails_on_missing_outputs_for_compatible_fixture(self) -> None:
        self.write_baseline([])
        self.write_fixture("MissingOutputs.lua", None, None)

        result = self.run_compare()

        self.assertNotEqual(result.returncode, 0)
        report = json.loads(self.output_file.read_text(encoding="utf-8"))
        self.assertEqual(report["summary"]["missing_outputs"], 1)
        self.assertEqual(report["result_statuses"][0]["status"], "missing_outputs")

    def test_enforce_skips_missing_outputs_for_incompatible_fixture(self) -> None:
        self.write_baseline([])
        self.write_fixture(
            "Lua51Only.lua",
            None,
            None,
            fixture_source="-- @lua-versions: 5.1\nreturn true\n",
        )

        result = self.run_compare()

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads(self.output_file.read_text(encoding="utf-8"))
        self.assertEqual(report["summary"]["missing_outputs"], 0)
        self.assertEqual(report["result_statuses"][0]["status"], "skipped")

    def test_enforce_skips_explicit_no_lua_versions_fixture(self) -> None:
        self.write_baseline([])
        self.write_fixture(
            "NoReferenceVersions.lua",
            None,
            None,
            fixture_source="-- @lua-versions: none\nreturn true\n",
        )

        result = self.run_compare()

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads(self.output_file.read_text(encoding="utf-8"))
        self.assertEqual(report["summary"]["missing_outputs"], 0)
        self.assertEqual(report["result_statuses"][0]["status"], "skipped")

    def test_enforce_fails_on_new_both_error(self) -> None:
        self.write_baseline([])
        self.write_fixture(
            "BothError.lua",
            1,
            1,
            lua_error="lua5.4: BothError.lua:1: expected failure\n",
            nova_error="ScriptRuntimeException: BothError.lua:1: expected failure\n",
        )

        result = self.run_compare()

        self.assertNotEqual(result.returncode, 0)

    def test_enforce_passes_on_known_both_error(self) -> None:
        entry = BothErrorEntry.from_errors(
            file="BothError.lua",
            lua_version="5.4",
            lua_rc=1,
            nova_rc=1,
            lua_error="lua5.4: BothError.lua:1: expected failure\n",
            nova_error="ScriptRuntimeException: BothError.lua:1: expected failure\n",
        )
        self.write_baseline([entry])
        lua_error = "lua5.4: BothError.lua:1: expected failure\n"
        nova_error = "ScriptRuntimeException: BothError.lua:1: expected failure\n"
        self.write_fixture(
            "BothError.lua",
            1,
            1,
            lua_error=lua_error,
            nova_error=nova_error,
        )

        result = self.run_compare()

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        report = json.loads(self.output_file.read_text(encoding="utf-8"))
        self.assertEqual(len(report["both_errors"]), 1)
        self.assertEqual(report["both_errors"][0]["lua_error"], lua_error)
        self.assertEqual(report["both_errors"][0]["nova_error"], nova_error)
        self.assertIn("lua_error_excerpt", report["both_errors"][0])
        self.assertIn("nova_error_excerpt", report["both_errors"][0])
        self.assertEqual(report["result_statuses"][0]["status"], "both_error")

    def test_enforce_allows_both_error_reduction(self) -> None:
        entry = BothErrorEntry.from_errors(
            file="FixedError.lua",
            lua_version="5.4",
            lua_rc=1,
            nova_rc=1,
            lua_error="lua5.4: FixedError.lua:1: expected failure\n",
            nova_error="ScriptRuntimeException: FixedError.lua:1: expected failure\n",
        )
        self.write_baseline([entry])
        self.write_fixture("FixedError.lua", 0, 0, lua_output="ok\n", nova_output="ok\n")

        result = self.run_compare()

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()

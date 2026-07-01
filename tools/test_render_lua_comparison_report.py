#!/usr/bin/env python3
"""Tests for scripts/tests/render-lua-comparison-report.py."""

from __future__ import annotations

import json
import shutil
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "tests" / "render-lua-comparison-report.py"


class RenderLuaComparisonReportTests(unittest.TestCase):
    def setUp(self) -> None:
        self.work_dir = ROOT / "artifacts" / "test-render-lua-comparison-report"
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)
        self.input_root = self.work_dir / "input"
        self.output = self.work_dir / "lua-comparison-report.md"
        self.input_root.mkdir(parents=True)

    def tearDown(self) -> None:
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)

    def write_artifact(
        self,
        lua_version: str = "5.4",
        os_name: str = "ubuntu-latest",
        mismatch: int = 0,
        ratchet_new: int = 0,
    ) -> None:
        artifact = self.input_root / f"lua-comparison-{lua_version}-{os_name}"
        artifact.mkdir(parents=True)
        (artifact / f"comparison-{lua_version}.json").write_text(
            json.dumps(
                {
                    "lua_version": lua_version,
                    "summary": {
                        "match": 10,
                        "mismatch": mismatch,
                        "both_error": 2,
                        "lua_only": 0,
                        "nova_only": 0,
                        "missing_outputs": 0,
                        "skipped": 3,
                    },
                    "match_rate": 83.3333,
                    "error_ratchet": {
                        "new_count": ratchet_new,
                        "changed_count": 0,
                        "missing_count": 0,
                    },
                }
            ),
            encoding="utf-8",
        )
        (artifact / "results.json").write_text(
            json.dumps(
                {
                    "summary": {
                        "elapsed_seconds": 12.5,
                        "workers": 4,
                    }
                }
            ),
            encoding="utf-8",
        )

    def run_script(self) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(SCRIPT.relative_to(ROOT)),
                "--input-root",
                str(self.input_root.relative_to(ROOT)),
                "--output",
                str(self.output.relative_to(ROOT)),
            ],
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

    def test_renders_lua_comparison_artifact_table(self) -> None:
        self.write_artifact()
        self.write_artifact(lua_version="5.1", os_name="windows-latest")

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=false", result.stdout)
        report = self.output.read_text(encoding="utf-8")
        self.assertIn("Lua Comparison Report", report)
        self.assertIn("ubuntu-latest", report)
        self.assertIn("windows-latest", report)
        self.assertIn("83.3%", report)

    def test_marks_unexpected_deltas(self) -> None:
        self.write_artifact(mismatch=1, ratchet_new=1)

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("regressed=true", result.stdout)

    def test_handles_missing_artifacts(self) -> None:
        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("rows=0", result.stdout)
        self.assertIn("No Lua comparison artifacts", self.output.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()

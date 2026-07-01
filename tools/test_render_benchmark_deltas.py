#!/usr/bin/env python3
"""Tests for scripts/benchmarks/render-benchmark-deltas.py."""

from __future__ import annotations

import json
import shutil
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "benchmarks" / "render-benchmark-deltas.py"


class RenderBenchmarkDeltasTests(unittest.TestCase):
    def setUp(self) -> None:
        self.work_dir = ROOT / "artifacts" / "test-render-benchmark-deltas"
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)
        self.current_root = self.work_dir / "current"
        self.current_results = self.current_root / "results"
        self.current_results.mkdir(parents=True)
        self.baseline_doc = self.work_dir / "Performance.md"
        self.output = self.work_dir / "benchmark-deltas.md"

    def tearDown(self) -> None:
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)

    def write_current(
        self,
        mean: float,
        p95: float,
        allocated: float,
        scenario: str = "NumericLoops",
    ) -> None:
        report = {
            "Benchmarks": [
                {
                    "Namespace": "WallstopStudios.NovaSharp.Benchmarks",
                    "Type": "RuntimeBenchmarks",
                    "Method": "ExecuteScenario",
                    "MethodTitle": "'Scenario Execution'",
                    "Parameters": f"ScenarioName={scenario}",
                    "Statistics": {
                        "Mean": mean,
                        "Percentiles": {
                            "P95": p95,
                        },
                    },
                    "Memory": {
                        "BytesAllocatedPerOperation": allocated,
                    },
                }
            ]
        }
        (self.current_results / "RuntimeBenchmarks-report-full-compressed.json").write_text(
            json.dumps(report),
            encoding="utf-8",
        )

    def write_baseline(self, scenario: str = "NumericLoops") -> None:
        self.baseline_doc.write_text(
            "\n".join(
                [
                    "# Performance",
                    "",
                    "## Windows",
                    "",
                    "### MoonSharp Baseline (captured 2025-01-01 00:00:00 +00:00)",
                    "",
                    "### MoonSharp.Benchmarks.RuntimeBenchmarks-20250101-000000",
                    "",
                    "| Method | Scenario | Mean | P95 | Rank | Allocated |",
                    "| --- | --- | ---: | ---: | ---: | ---: |",
                    f"| 'Scenario Execution' | {scenario} | 100 ns | 120 ns | 1 | 100 B |",
                    "",
                ]
            ),
            encoding="utf-8",
        )

    def run_script(self) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [
                "python3",
                str(SCRIPT.relative_to(ROOT)),
                "--current-root",
                str(self.current_root.relative_to(ROOT)),
                "--baseline-doc",
                str(self.baseline_doc.relative_to(ROOT)),
                "--output",
                str(self.output.relative_to(ROOT)),
                "--tolerance",
                "0.02",
                "--regression-threshold",
                "0.10",
            ],
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

    def test_renders_matched_moonsharp_baseline_delta(self) -> None:
        self.write_current(mean=90, p95=110, allocated=80)
        self.write_baseline()

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("regressed=false", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("MoonSharp Benchmark Deltas", output)
        self.assertIn("Matched rows: 1 of 1 current rows and 1 baseline rows", output)
        self.assertIn("NumericLoops", output)
        self.assertIn("-10.00%", output)

    def test_marks_regressed_when_current_is_worse_than_threshold(self) -> None:
        self.write_current(mean=130, p95=150, allocated=120)
        self.write_baseline()

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("regressed=true", result.stdout)

    def test_handles_missing_matching_baseline(self) -> None:
        self.write_current(mean=90, p95=110, allocated=80, scenario="TableMutation")
        self.write_baseline(scenario="NumericLoops")

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("rows=0", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("No benchmark rows matched", output)
        self.assertIn("Current rows without MoonSharp baseline: 1", output)


if __name__ == "__main__":
    unittest.main()

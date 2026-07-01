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
        self.comparison_root = self.work_dir / "comparison"
        self.self_baseline_root = self.work_dir / "self-baseline"
        self.output = self.work_dir / "benchmark-deltas.md"

    def tearDown(self) -> None:
        if self.work_dir.exists():
            shutil.rmtree(self.work_dir)

    def write_report(self, root: Path, name: str, benchmarks: list[dict]) -> None:
        results = root / "results"
        results.mkdir(parents=True, exist_ok=True)
        report = {"Benchmarks": benchmarks}
        (results / f"{name}-report-full-compressed.json").write_text(
            json.dumps(report),
            encoding="utf-8",
        )

    def benchmark(
        self,
        namespace: str,
        benchmark_type: str,
        method_title: str,
        parameters: str,
        mean: float,
        p95: float,
        allocated: float,
        gen0: float = 0,
        gen1: float = 0,
        gen2: float = 0,
    ) -> dict:
        method = method_title.replace(" ", "")
        return {
            "Namespace": namespace,
            "Type": benchmark_type,
            "Method": method,
            "MethodTitle": f"'{method_title}'",
            "Parameters": parameters,
            "Statistics": {
                "Mean": mean,
                "Percentiles": {
                    "P95": p95,
                },
            },
            "Memory": {
                "BytesAllocatedPerOperation": allocated,
            },
            "Metrics": [
                {
                    "Value": gen0,
                    "Descriptor": {
                        "Id": "Gen0Collects",
                        "DisplayName": "Gen0",
                    },
                },
                {
                    "Value": gen1,
                    "Descriptor": {
                        "Id": "Gen1Collects",
                        "DisplayName": "Gen1",
                    },
                },
                {
                    "Value": gen2,
                    "Descriptor": {
                        "Id": "Gen2Collects",
                        "DisplayName": "Gen2",
                    },
                },
                {
                    "Value": allocated,
                    "Descriptor": {
                        "Id": "Allocated Memory",
                        "DisplayName": "Allocated",
                    },
                },
            ],
        }

    def comparison_benchmark(
        self,
        method_title: str,
        mean: float,
        p95: float,
        allocated: float,
        gen0: float = 0,
    ) -> dict:
        return self.benchmark(
            "WallstopStudios.NovaSharp.Comparison",
            "LuaPerformanceBenchmarks",
            method_title,
            "ScenarioName=NumericLoops",
            mean,
            p95,
            allocated,
            gen0=gen0,
        )

    def current_benchmark(
        self,
        mean: float,
        p95: float,
        allocated: float,
        scenario: str = "NumericLoops",
    ) -> dict:
        return self.benchmark(
            "WallstopStudios.NovaSharp.Benchmarks",
            "RuntimeBenchmarks",
            "Scenario Execution",
            f"ScenarioName={scenario}",
            mean,
            p95,
            allocated,
        )

    def run_script(self, self_baseline_root: Path | None = None) -> subprocess.CompletedProcess[str]:
        args = [
            "python3",
            str(SCRIPT.relative_to(ROOT)),
            "--current-root",
            str(self.current_root.relative_to(ROOT)),
            "--comparison-root",
            str(self.comparison_root.relative_to(ROOT)),
            "--output",
            str(self.output.relative_to(ROOT)),
            "--tolerance",
            "0.02",
            "--regression-threshold",
            "0.10",
        ]
        if self_baseline_root is not None:
            args.extend(
                [
                    "--self-baseline-root",
                    str(self_baseline_root.relative_to(ROOT)),
                ]
            )

        return subprocess.run(
            args,
            cwd=ROOT,
            text=True,
            capture_output=True,
            check=False,
        )

    def test_renders_same_run_external_runtime_deltas(self) -> None:
        self.write_report(
            self.current_root,
            "RuntimeBenchmarks",
            [self.current_benchmark(mean=90, p95=110, allocated=80)],
        )
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 90, 110, 80, gen0=1),
                self.comparison_benchmark("MoonSharp Execute", 100, 120, 100, gen0=2),
                self.comparison_benchmark("NLua Execute", 150, 180, 140, gen0=3),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("regressed=false", result.stdout)
        self.assertIn("external_rows=2", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Benchmark Comparison Deltas", output)
        self.assertIn("Same-Run Runtime Matrix", output)
        self.assertIn("NovaSharp Raw Results", output)
        self.assertIn("#### Time", output)
        self.assertIn("#### Memory and GC", output)
        self.assertIn("NovaSharp Mean / P95", output)
        self.assertIn("NovaSharp Alloc / GC0/1/2", output)
        self.assertIn("MoonSharp Mean / P95", output)
        self.assertIn("NovaSharp Delta vs MoonSharp", output)
        self.assertIn("NLua Mean / P95", output)
        self.assertIn("NovaSharp Delta vs NLua", output)
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 80 B / 1 / 0 / 0 |",
            output,
        )
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 100 ns / 120 ns | -10 ns (-10.00%) / -10 ns (-8.33%) | 150 ns / 180 ns | -60 ns (-40.00%) / -70 ns (-38.89%) |",
            output,
        )
        self.assertIn(
            "| NumericLoops | Execute | 80 B / 1 / 0 / 0 | 100 B / 2 / 0 / 0 | -20 B (-20.00%) / -1 / 0 / 0 | 140 B / 3 / 0 / 0 | -60 B (-42.86%) / -2 / 0 / 0 |",
            output,
        )

    def test_external_runtime_deltas_are_report_only_for_regressed_signal(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 130, 150, 150, gen0=4),
                self.comparison_benchmark("MoonSharp Execute", 100, 120, 100, gen0=2),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("regressed=false", result.stdout)
        self.assertIn("external_rows=1", result.stdout)

    def test_renders_self_baseline_deltas_when_checked_in_artifacts_exist(self) -> None:
        self.write_report(
            self.current_root,
            "RuntimeBenchmarks",
            [self.current_benchmark(mean=130, p95=150, allocated=130)],
        )
        self.write_report(
            self.self_baseline_root,
            "RuntimeBenchmarks",
            [self.current_benchmark(mean=100, p95=120, allocated=100)],
        )

        result = self.run_script(self.self_baseline_root)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("self_rows=1", result.stdout)
        self.assertIn("regressed=true", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Self Baseline Comparisons", output)
        self.assertIn("+30 ns (+30.00%)", output)

    def test_reports_comparison_groups_without_novasharp_row(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [self.comparison_benchmark("MoonSharp Execute", 100, 120, 100)],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("external_rows=0", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Comparison groups without a NovaSharp row", output)
        self.assertIn("No same-run external comparison rows were found", output)


if __name__ == "__main__":
    unittest.main()

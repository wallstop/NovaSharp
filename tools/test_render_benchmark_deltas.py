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
        self.phase_baseline = self.work_dir / "phase-a0-baseline.json"
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

    def interop_benchmark(
        self,
        method_title: str,
        mean: float,
        p95: float,
        allocated: float,
        gen0: float = 0,
    ) -> dict:
        return self.benchmark(
            "WallstopStudios.NovaSharp.Comparison",
            "LuaInteropBenchmarks",
            method_title,
            "ScenarioName=TwoArgAdd",
            mean,
            p95,
            allocated,
            gen0=gen0,
        )

    def lua_cli_benchmark(
        self,
        mean: float,
        p95: float,
        *,
        include_show_delta_percent: bool = True,
    ) -> dict:
        benchmark = {
            "Namespace": "WallstopStudios.NovaSharp.Comparison",
            "Type": "LuaPerformanceBenchmarks",
            "Method": "LuaExecute",
            "MethodTitle": "'Lua Execute'",
            "Parameters": "ScenarioName=NumericLoops",
            "RuntimeDisplayName": "Lua CLI wall-time",
            "RuntimeContext": "lua5.4: Lua 5.4.6",
            "RuntimeKind": "LuaCliWallTime",
            "Statistics": {
                "Mean": mean,
                "Percentiles": {
                    "P95": p95,
                },
            },
        }
        if include_show_delta_percent:
            benchmark["ShowDeltaPercent"] = False
        return benchmark

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

    def run_script(
        self,
        self_baseline_root: Path | None = None,
        expect_lua_cli: bool = False,
        phase_baseline: Path | None = None,
        write_phase_baseline: Path | None = None,
        enforce_phase_gates: bool = False,
    ) -> subprocess.CompletedProcess[str]:
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
        if expect_lua_cli:
            args.append("--expect-lua-cli")
        if phase_baseline is None:
            phase_baseline = self.phase_baseline
        if phase_baseline is not None:
            args.extend(
                [
                    "--phase-baseline",
                    str(phase_baseline.relative_to(ROOT)),
                ]
            )
        if write_phase_baseline is not None:
            args.extend(
                [
                    "--write-phase-baseline",
                    str(write_phase_baseline.relative_to(ROOT)),
                ]
            )
        if enforce_phase_gates:
            args.append("--enforce-phase-gates")

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
                self.comparison_benchmark("LuaCSharp Execute", 70, 90, 24),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=true", result.stdout)
        self.assertIn("regressed=false", result.stdout)
        self.assertIn("external_rows=3", result.stdout)
        self.assertIn("missing_external_runtime_cells=0", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Benchmark Comparison Deltas", output)
        self.assertIn("Phase A0 Scoreboard", output)
        self.assertIn("Scoreboard Time", output)
        self.assertIn("Scoreboard Memory and GC", output)
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
        self.assertIn("LuaCSharp Mean / P95", output)
        self.assertIn("NovaSharp Delta vs LuaCSharp", output)
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 80 B / 1 / 0 / 0 |",
            output,
        )
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 100 ns / 120 ns | -10 ns (-10.00%) / -10 ns (-8.33%) | 150 ns / 180 ns | -60 ns (-40.00%) / -70 ns (-38.89%) | 70 ns / 90 ns | +20 ns (+28.57%) / +20 ns (+22.22%) |",
            output,
        )
        self.assertIn(
            "| NumericLoops | Execute | 80 B / 1 / 0 / 0 | 100 B / 2 / 0 / 0 | -20 B (-20.00%) / -1 / 0 / 0 | 140 B / 3 / 0 / 0 | -60 B (-42.86%) / -2 / 0 / 0 | 24 B / 0 / 0 / 0 | +56 B (+233.33%) / +1 / 0 / 0 |",
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

    def test_reports_missing_expected_external_runtime_cells(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 90, 110, 80),
                self.comparison_benchmark("MoonSharp Execute", 100, 120, 100),
                self.comparison_benchmark("NLua Execute", 150, 180, 140),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("missing_external_runtime_cells=1", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Expected external runtime cells missing", output)
        self.assertIn("LuaCSharp", output)

    def test_renders_reference_lua_cli_time_context_without_memory_claim(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 90, 110, 80, gen0=1),
                self.lua_cli_benchmark(mean=50, p95=70),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=false", result.stdout)
        self.assertIn("external_rows=1", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Reference `lua` CLI rows", output)
        self.assertIn("Runtime Context", output)
        self.assertIn("Lua CLI wall-time: lua5.4: Lua 5.4.6", output)
        self.assertIn("Lua CLI wall-time Mean / P95", output)
        self.assertIn("NovaSharp Delta vs Lua CLI wall-time", output)
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 50 ns / 70 ns | +40 ns / +40 ns |",
            output,
        )
        self.assertIn(
            "| NumericLoops | Execute | 80 B / 1 / 0 / 0 | - | - | - | - | - |",
            output,
        )
        self.assertIn("A `-` cell means that runtime did not report memory", output)
        self.assertIn(
            "| NumericLoops | Execute | 80 B / 1 / 0 / 0 | - / - / - / - | - / - / - / - |",
            output,
        )

    def test_reports_missing_reference_lua_cli_context_when_expected(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 90, 110, 80),
                self.comparison_benchmark("MoonSharp Execute", 100, 120, 100),
                self.comparison_benchmark("NLua Execute", 150, 180, 140),
                self.comparison_benchmark("LuaCSharp Execute", 70, 90, 24),
            ],
        )

        result = self.run_script(expect_lua_cli=True)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("missing_lua_cli_rows=1", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("Expected reference lua CLI rows missing: 1", output)
        self.assertIn("Lua", output)

    def test_interop_rows_do_not_expect_reference_lua_cli_context(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaInteropBenchmarks",
            [
                self.interop_benchmark("NovaSharp LuaToClrInterop", 90, 110, 80),
                self.interop_benchmark("MoonSharp LuaToClrInterop", 100, 120, 100),
                self.interop_benchmark("NLua LuaToClrInterop", 150, 180, 140),
                self.interop_benchmark("LuaCSharp LuaToClrInterop", 70, 90, 24),
                self.interop_benchmark("NovaSharp ClrToLuaInterop", 95, 115, 82),
                self.interop_benchmark("MoonSharp ClrToLuaInterop", 105, 125, 102),
                self.interop_benchmark("NLua ClrToLuaInterop", 155, 185, 142),
                self.interop_benchmark("LuaCSharp ClrToLuaInterop", 75, 95, 26),
            ],
        )

        result = self.run_script(expect_lua_cli=True)

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("external_rows=6", result.stdout)
        self.assertIn("missing_external_runtime_cells=0", result.stdout)
        self.assertIn("missing_lua_cli_rows=0", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn("LuaToClrInterop", output)
        self.assertIn("ClrToLuaInterop", output)

    def test_runtime_kind_marks_reference_lua_cli_context_report_only(self) -> None:
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 90, 110, 80, gen0=1),
                self.lua_cli_benchmark(
                    mean=50,
                    p95=70,
                    include_show_delta_percent=False,
                ),
            ],
        )

        result = self.run_script()

        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertIn("changed=false", result.stdout)
        self.assertIn("external_rows=1", result.stdout)
        output = self.output.read_text(encoding="utf-8")
        self.assertIn(
            "| NumericLoops | Execute | 90 ns / 110 ns | 50 ns / 70 ns | +40 ns / +40 ns |",
            output,
        )

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

    def test_writes_phase_baseline_and_enforces_matching_rows(self) -> None:
        phase_baseline = self.work_dir / "phase-a0-baseline.json"
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 120, 80),
                self.comparison_benchmark("NLua Execute", 50, 60, 24),
            ],
        )

        write_result = self.run_script(
            phase_baseline=phase_baseline,
            write_phase_baseline=phase_baseline,
        )

        self.assertEqual(0, write_result.returncode, write_result.stdout + write_result.stderr)
        self.assertIn("phase_baseline_output=", write_result.stdout)
        self.assertIn("phase_baseline_rows=1", write_result.stdout)
        baseline_payload = json.loads(phase_baseline.read_text(encoding="utf-8"))
        self.assertEqual("novasharp.phase-benchmark-baseline.v1", baseline_payload["schema"])
        self.assertEqual(1, len(baseline_payload["rows"]))
        self.assertIn("NovaSharp", baseline_payload["rows"][0]["runtimes"])
        self.assertIn("NLua", baseline_payload["rows"][0]["runtimes"])
        self.assertNotIn(
            "No checked-in Phase A0 scoreboard baseline was found",
            self.output.read_text(encoding="utf-8"),
        )

        enforce_result = self.run_script(
            phase_baseline=phase_baseline,
            enforce_phase_gates=True,
        )

        self.assertEqual(
            0,
            enforce_result.returncode,
            enforce_result.stdout + enforce_result.stderr,
        )
        self.assertIn("phase_baseline_rows=1", enforce_result.stdout)
        self.assertIn("phase_gate_failures=0", enforce_result.stdout)

    def test_phase_gate_fails_on_exact_novasharp_allocation_change(self) -> None:
        phase_baseline = self.work_dir / "phase-a0-baseline.json"
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 120, 80),
                self.comparison_benchmark("NLua Execute", 50, 60, 24),
            ],
        )
        self.assertEqual(0, self.run_script(write_phase_baseline=phase_baseline).returncode)
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 120, 81),
                self.comparison_benchmark("NLua Execute", 50, 60, 24),
            ],
        )

        result = self.run_script(phase_baseline=phase_baseline, enforce_phase_gates=True)

        self.assertEqual(1, result.returncode)
        self.assertIn("phase_gate_failures=1", result.stdout)
        self.assertIn("error=phase gate failures: 1", result.stderr)
        self.assertIn(
            "NovaSharp allocation changed from 80 B to 81 B (+1 B).",
            self.output.read_text(encoding="utf-8"),
        )
        self.assertIn(
            "| NumericLoops | Execute | NovaSharp B/op | NovaSharp allocation changed from 80 B to 81 B (+1 B). |",
            self.output.read_text(encoding="utf-8"),
        )

    def test_phase_gate_fails_on_nlua_ratio_drift(self) -> None:
        phase_baseline = self.work_dir / "phase-a0-baseline.json"
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 100, 80),
                self.comparison_benchmark("NLua Execute", 50, 50, 24),
            ],
        )
        self.assertEqual(0, self.run_script(write_phase_baseline=phase_baseline).returncode)
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 130, 100, 80),
                self.comparison_benchmark("NLua Execute", 50, 50, 24),
            ],
        )

        result = self.run_script(phase_baseline=phase_baseline, enforce_phase_gates=True)

        self.assertEqual(1, result.returncode)
        self.assertIn("phase_gate_failures=1", result.stdout)
        self.assertIn(
            "NovaSharp/NLua mean ratio changed from 2.000x to 2.600x (+30.00%).",
            self.output.read_text(encoding="utf-8"),
        )

    def test_phase_gate_fails_when_current_nlua_row_is_missing(self) -> None:
        phase_baseline = self.work_dir / "phase-a0-baseline.json"
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 120, 80),
                self.comparison_benchmark("NLua Execute", 50, 60, 24),
            ],
        )
        self.assertEqual(0, self.run_script(write_phase_baseline=phase_baseline).returncode)
        self.write_report(
            self.comparison_root,
            "LuaPerformanceBenchmarks",
            [
                self.comparison_benchmark("NovaSharp Execute", 100, 120, 80),
                self.comparison_benchmark("MoonSharp Execute", 90, 110, 40),
            ],
        )

        result = self.run_script(phase_baseline=phase_baseline, enforce_phase_gates=True)

        self.assertEqual(1, result.returncode)
        self.assertIn("phase_gate_failures=1", result.stdout)
        self.assertIn(
            "Current comparison row is missing NLua",
            self.output.read_text(encoding="utf-8"),
        )


if __name__ == "__main__":
    unittest.main()

#!/usr/bin/env python3
"""Render BenchmarkDotNet deltas from same-run comparison artifacts."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CURRENT_ROOT = Path("BenchmarkDotNet.Artifacts")
DEFAULT_COMPARISON_ROOT = Path("BenchmarkDotNet.Artifacts")
DEFAULT_SELF_BASELINE_ROOT = Path("docs/performance-history/current-baseline")
DEFAULT_OUTPUT = Path("artifacts/benchmark-deltas.md")
DEFAULT_PHASE_BASELINE = Path("progress/benchmarks/phase-a0-scoreboard-baseline.json")
DEFAULT_TOLERANCE = 0.02
DEFAULT_REGRESSION_THRESHOLD = 0.10
DEFAULT_NLUA_RATIO_THRESHOLD = 1.00
PHASE_ALLOCATION_EXACT_LIMIT_BYTES = 1024.0
PHASE_ALLOCATION_ABSOLUTE_TOLERANCE_BYTES = 512.0
PHASE_ALLOCATION_RELATIVE_TOLERANCE = 0.0002
NOVA_RUNTIME = "NovaSharp"
RUNTIME_PREFIXES = ("NovaSharp", "MoonSharp", "NLua", "LuaCSharp", "KeraLua", "Lua")
EXPECTED_EXTERNAL_RUNTIMES = ("MoonSharp", "NLua", "LuaCSharp")
SCOREBOARD_RUNTIMES = ("MoonSharp", "NLua", "LuaCSharp", "Lua")
PHASE_BASELINE_SCHEMA = "novasharp.phase-benchmark-baseline.v1"


@dataclass(frozen=True, order=True)
class BenchmarkKey:
    summary: str
    method: str
    parameters: str


@dataclass(frozen=True, order=True)
class ComparisonKey:
    summary: str
    operation: str
    parameters: str


@dataclass(frozen=True)
class Metrics:
    mean_ns: float
    p95_ns: float
    gen0_per_1k: float
    gen1_per_1k: float
    gen2_per_1k: float
    allocated_bytes: float


@dataclass(frozen=True)
class MetricRecord:
    metrics: Metrics
    parameter_display: str
    runtime_display_name: str = ""
    runtime_context: str = ""
    runtime_kind: str = ""
    show_delta_percent: bool = True


@dataclass(frozen=True)
class RuntimeComparison:
    runtime: str
    display_name: str
    metrics: Metrics
    runtime_context: str
    runtime_kind: str
    show_delta_percent: bool


@dataclass(frozen=True)
class RuntimeColumn:
    runtime: str
    display_name: str
    show_delta_percent: bool


@dataclass(frozen=True)
class ExternalDeltaRow:
    key: ComparisonKey
    runtime: str
    parameter_display: str
    nova: Metrics
    comparison: Metrics
    contributes_to_changed_signal: bool


@dataclass(frozen=True)
class RuntimeMatrixRow:
    key: ComparisonKey
    parameter_display: str
    nova: Metrics
    comparisons: tuple[RuntimeComparison, ...]


@dataclass(frozen=True, order=True)
class MissingRuntimeCell:
    key: ComparisonKey
    runtime: str


@dataclass(frozen=True)
class SelfDeltaRow:
    key: BenchmarkKey
    parameter_display: str
    current: Metrics
    baseline: Metrics


@dataclass(frozen=True)
class PhaseGateFailure:
    key: ComparisonKey
    metric: str
    message: str


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--current-root",
        type=Path,
        default=DEFAULT_CURRENT_ROOT,
        help="Directory containing current NovaSharp BenchmarkDotNet artifacts.",
    )
    parser.add_argument(
        "--comparison-root",
        type=Path,
        default=DEFAULT_COMPARISON_ROOT,
        help="Directory containing same-run comparison BenchmarkDotNet artifacts.",
    )
    parser.add_argument(
        "--self-baseline-root",
        type=Path,
        default=DEFAULT_SELF_BASELINE_ROOT,
        help=(
            "Directory containing checked-in NovaSharp BenchmarkDotNet artifacts "
            "for self comparison. Missing directories are reported as not configured."
        ),
    )
    parser.add_argument(
        "--phase-baseline",
        type=Path,
        default=DEFAULT_PHASE_BASELINE,
        help=(
            "Normalized phase comparison baseline JSON. The scoreboard reads this "
            "for the NovaSharp baseline column, and --enforce-phase-gates compares "
            "current comparison rows against it."
        ),
    )
    parser.add_argument(
        "--write-phase-baseline",
        type=Path,
        default=None,
        help=(
            "Write normalized comparison metrics to this JSON path. Intended for "
            "checked-in phase baselines under progress/ after a representative run."
        ),
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help="Markdown output path.",
    )
    parser.add_argument(
        "--tolerance",
        type=float,
        default=DEFAULT_TOLERANCE,
        help="Fractional change threshold for changed=true.",
    )
    parser.add_argument(
        "--regression-threshold",
        type=float,
        default=DEFAULT_REGRESSION_THRESHOLD,
        help="Fractional worse-than-self-baseline threshold for regressed=true.",
    )
    parser.add_argument(
        "--nlua-ratio-threshold",
        type=float,
        default=DEFAULT_NLUA_RATIO_THRESHOLD,
        help=(
            "Allowed fractional regression in NovaSharp/NLua mean and P95 ratios when "
            "--enforce-phase-gates is enabled."
        ),
    )
    parser.add_argument(
        "--enforce-phase-gates",
        action="store_true",
        help=(
            "Fail when the phase baseline is missing, when comparison rows do not "
            "match the baseline shape, when NovaSharp/NLua ratios regress beyond the "
            "configured threshold, or when NovaSharp allocated B/op regresses beyond "
            "the phase allocation tolerance."
        ),
    )
    parser.add_argument(
        "--expect-lua-cli",
        action="store_true",
        help="Report missing reference lua CLI wall-time rows for Execute scenarios.",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    current_root = resolve_repo_path(args.current_root)
    comparison_root = resolve_repo_path(args.comparison_root)
    self_baseline_root = resolve_repo_path(args.self_baseline_root)
    phase_baseline_path = resolve_repo_path(args.phase_baseline)
    write_phase_baseline_path = (
        resolve_repo_path(args.write_phase_baseline)
        if args.write_phase_baseline is not None
        else None
    )
    output = resolve_repo_path(args.output)

    current = load_benchmark_metrics(current_root, include_external=False)
    self_baseline = (
        load_benchmark_metrics(self_baseline_root, include_external=False)
        if self_baseline_root.exists()
        else {}
    )
    self_rows = build_self_delta_rows(current, self_baseline)
    current_without_baseline = sorted(key for key in current if key not in self_baseline)
    baseline_without_current = sorted(key for key in self_baseline if key not in current)

    comparison = load_comparison_metrics(comparison_root)
    external_rows = build_external_delta_rows(comparison)
    runtime_matrix_rows = build_runtime_matrix_rows(comparison)
    if write_phase_baseline_path is not None:
        write_phase_baseline(write_phase_baseline_path, comparison, comparison_root)

    phase_baseline = load_phase_baseline(phase_baseline_path)
    phase_gate_failures = (
        build_phase_gate_failures(
            comparison,
            phase_baseline,
            args.nlua_ratio_threshold,
        )
        if phase_baseline_path.exists()
        else []
    )
    missing_expected_runtime_cells = build_missing_expected_runtime_cells(
        comparison,
        expect_lua_cli=args.expect_lua_cli,
    )
    missing_lua_cli_rows = [
        cell for cell in missing_expected_runtime_cells if cell.runtime == "Lua"
    ]
    comparison_groups_without_nova = sorted(
        key for key, runtimes in comparison.items() if NOVA_RUNTIME not in runtimes
    )

    changed = any(external_row_changed(row, args.tolerance) for row in external_rows) or any(
        self_row_changed(row, args.tolerance) for row in self_rows
    )
    regressed = any(self_row_regressed(row, args.regression_threshold) for row in self_rows)

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        render_markdown(
            external_rows,
            runtime_matrix_rows,
            self_rows,
            current_root,
            comparison_root,
            self_baseline_root,
            len(current),
            len(self_baseline),
            len(comparison),
            comparison_groups_without_nova,
            missing_expected_runtime_cells,
            missing_lua_cli_rows,
            phase_baseline,
            phase_baseline_path,
            phase_gate_failures,
            current_without_baseline,
            baseline_without_current,
        ),
        encoding="utf-8",
    )

    print(f"changed={str(changed).lower()}")
    print(f"regressed={str(regressed).lower()}")
    print(f"external_rows={len(external_rows)}")
    print(f"missing_external_runtime_cells={len(missing_expected_runtime_cells)}")
    print(f"missing_lua_cli_rows={len(missing_lua_cli_rows)}")
    print(f"self_rows={len(self_rows)}")
    print(f"phase_baseline_rows={len(phase_baseline)}")
    print(f"phase_gate_failures={len(phase_gate_failures)}")
    if write_phase_baseline_path is not None:
        print(f"phase_baseline_output={repo_relative(write_phase_baseline_path)}")
    print(f"output={repo_relative(output)}")
    if args.enforce_phase_gates:
        if not phase_baseline_path.exists():
            print(
                "error=phase baseline missing: "
                f"{repo_relative(phase_baseline_path)}",
                file=sys.stderr,
            )
            return 1
        if phase_gate_failures:
            print(
                f"error=phase gate failures: {len(phase_gate_failures)}",
                file=sys.stderr,
            )
            return 1
    return 0


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_benchmark_metrics(root: Path, include_external: bool) -> dict[BenchmarkKey, MetricRecord]:
    metrics: dict[BenchmarkKey, MetricRecord] = {}
    for report in find_benchmark_reports(root):
        data = json.loads(report.read_text(encoding="utf-8"))
        for benchmark in data.get("Benchmarks", []):
            key = benchmark_key_from_json(benchmark)
            if key is None:
                continue

            runtime_method = split_runtime_method(key.method)
            if (
                not include_external
                and runtime_method is not None
                and runtime_method[0] != NOVA_RUNTIME
            ):
                continue

            benchmark_metrics = metrics_from_json(benchmark)
            if benchmark_metrics is None:
                continue

            metrics[key] = MetricRecord(
                metrics=benchmark_metrics,
                parameter_display=build_parameter_display_from_json(benchmark),
                runtime_display_name=runtime_display_name_from_json(benchmark),
                runtime_context=runtime_context_from_json(benchmark),
                runtime_kind=runtime_kind_from_json(benchmark),
                show_delta_percent=show_delta_percent_from_json(benchmark),
            )

    return metrics


def load_comparison_metrics(root: Path) -> dict[ComparisonKey, dict[str, MetricRecord]]:
    metrics: dict[ComparisonKey, dict[str, MetricRecord]] = {}
    for report in find_benchmark_reports(root):
        data = json.loads(report.read_text(encoding="utf-8"))
        for benchmark in data.get("Benchmarks", []):
            key = benchmark_key_from_json(benchmark)
            if key is None:
                continue

            runtime_method = split_runtime_method(key.method)
            if runtime_method is None:
                continue

            runtime, operation = runtime_method
            benchmark_metrics = metrics_from_json(benchmark)
            if benchmark_metrics is None:
                continue

            comparison_key = ComparisonKey(key.summary, operation, key.parameters)
            runtimes = metrics.setdefault(comparison_key, {})
            runtimes[runtime] = MetricRecord(
                metrics=benchmark_metrics,
                parameter_display=build_parameter_display_from_json(benchmark),
                runtime_display_name=runtime_display_name_from_json(benchmark),
                runtime_context=runtime_context_from_json(benchmark),
                runtime_kind=runtime_kind_from_json(benchmark),
                show_delta_percent=show_delta_percent_from_json(benchmark),
            )

    return metrics


def load_phase_baseline(path: Path) -> dict[ComparisonKey, dict[str, MetricRecord]]:
    if not path.exists():
        return {}

    data = json.loads(path.read_text(encoding="utf-8"))
    schema = data.get("schema")
    if schema != PHASE_BASELINE_SCHEMA:
        raise ValueError(
            f"Unsupported phase baseline schema in {repo_relative(path)}: {schema!r}"
        )

    baseline: dict[ComparisonKey, dict[str, MetricRecord]] = {}
    for row in data.get("rows") or []:
        key = ComparisonKey(
            normalize_cell(row.get("summary", "")),
            normalize_cell(row.get("operation", "")),
            normalize_cell(row.get("parameters", "")),
        )
        if not key.summary or not key.operation:
            continue

        runtimes: dict[str, MetricRecord] = {}
        for runtime, runtime_payload in sorted((row.get("runtimes") or {}).items()):
            metrics = metrics_from_baseline_json(runtime_payload)
            if metrics is None:
                continue

            runtimes[normalize_cell(runtime)] = MetricRecord(
                metrics=metrics,
                parameter_display=normalize_cell(row.get("parameterDisplay", "")),
                runtime_display_name=normalize_cell(
                    runtime_payload.get("runtimeDisplayName", "")
                ),
                runtime_context=normalize_cell(runtime_payload.get("runtimeContext", "")),
                runtime_kind=normalize_cell(runtime_payload.get("runtimeKind", "")),
                show_delta_percent=bool(runtime_payload.get("showDeltaPercent", True)),
            )

        if runtimes:
            baseline[key] = runtimes

    return baseline


def write_phase_baseline(
    path: Path,
    comparison: dict[ComparisonKey, dict[str, MetricRecord]],
    comparison_root: Path,
) -> None:
    rows = []
    for key in sorted(comparison, key=comparison_matrix_sort_key):
        runtimes = comparison[key]
        if NOVA_RUNTIME not in runtimes:
            continue

        runtime_payload = {}
        for runtime in sorted(runtimes, key=runtime_sort_key):
            record = runtimes[runtime]
            runtime_payload[runtime] = metrics_to_baseline_json(record)

        rows.append(
            {
                "summary": key.summary,
                "operation": key.operation,
                "parameters": key.parameters,
                "parameterDisplay": runtimes[NOVA_RUNTIME].parameter_display,
                "runtimes": runtime_payload,
            }
        )

    payload = {
        "schema": PHASE_BASELINE_SCHEMA,
        "generatedAt": datetime.now(timezone.utc).replace(microsecond=0).isoformat(),
        "source": {
            "comparisonRoot": repo_relative(comparison_root),
        },
        "rows": rows,
    }

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def metrics_to_baseline_json(record: MetricRecord) -> dict[str, object]:
    metrics = record.metrics
    return {
        "allocatedBytes": finite_or_none(metrics.allocated_bytes),
        "gen0Per1K": finite_or_none(metrics.gen0_per_1k),
        "gen1Per1K": finite_or_none(metrics.gen1_per_1k),
        "gen2Per1K": finite_or_none(metrics.gen2_per_1k),
        "meanNs": finite_or_none(metrics.mean_ns),
        "p95Ns": finite_or_none(metrics.p95_ns),
        "runtimeContext": record.runtime_context,
        "runtimeDisplayName": record.runtime_display_name,
        "runtimeKind": record.runtime_kind,
        "showDeltaPercent": record.show_delta_percent,
    }


def metrics_from_baseline_json(payload: dict) -> Metrics | None:
    mean = to_float(payload.get("meanNs"))
    p95 = to_float(payload.get("p95Ns"))
    if not math.isfinite(mean):
        return None
    if not math.isfinite(p95):
        p95 = mean

    return Metrics(
        mean,
        p95,
        to_float(payload.get("gen0Per1K")),
        to_float(payload.get("gen1Per1K")),
        to_float(payload.get("gen2Per1K")),
        to_float(payload.get("allocatedBytes")),
    )


def finite_or_none(value: float) -> float | None:
    return value if math.isfinite(value) else None


def find_benchmark_reports(root: Path) -> list[Path]:
    if not root.exists():
        return []

    candidates: list[Path] = []
    patterns = ("*-report-full-compressed.json", "*-report-full.json")
    for pattern in patterns:
        candidates.extend(root.rglob(pattern))

    return sorted(dict.fromkeys(candidates))


def benchmark_key_from_json(benchmark: dict) -> BenchmarkKey | None:
    benchmark_type = normalize_summary_name(
        f"{benchmark.get('Namespace', '')}.{benchmark.get('Type', '')}"
    )
    method = normalize_method_name(benchmark.get("MethodTitle") or benchmark.get("Method", ""))
    parameters = build_parameter_signature_from_text(benchmark.get("Parameters", ""))

    if not benchmark_type or not method:
        return None

    return BenchmarkKey(benchmark_type, method, parameters)


def split_runtime_method(method: str) -> tuple[str, str] | None:
    normalized = normalize_method_name(method)
    for runtime in RUNTIME_PREFIXES:
        prefix = f"{runtime} "
        if normalized.startswith(prefix):
            operation = normalized[len(prefix) :].strip()
            if operation:
                return runtime, operation

    return None


def metrics_from_json(benchmark: dict) -> Metrics | None:
    statistics = benchmark.get("Statistics") or {}
    memory = benchmark.get("Memory") or {}
    percentiles = statistics.get("Percentiles") or {}
    has_memory_payload = bool(memory) or bool(benchmark.get("Metrics"))

    mean = to_float(statistics.get("Mean"))
    p95 = to_float(percentiles.get("P95", statistics.get("P95", mean)))
    allocated = metric_value(benchmark, "Allocated Memory", "Allocated")
    if not math.isfinite(allocated):
        allocated = to_float(memory.get("BytesAllocatedPerOperation"))
    if not math.isfinite(allocated):
        allocated = math.nan

    if has_memory_payload:
        gen0 = gc_metric_value(benchmark, memory, "Gen0Collects", "Gen0Collections", "Gen0")
        gen1 = gc_metric_value(benchmark, memory, "Gen1Collects", "Gen1Collections", "Gen1")
        gen2 = gc_metric_value(benchmark, memory, "Gen2Collects", "Gen2Collections", "Gen2")
    else:
        gen0 = math.nan
        gen1 = math.nan
        gen2 = math.nan

    if not math.isfinite(p95):
        p95 = mean

    if not math.isfinite(mean):
        return None

    return Metrics(mean, p95, gen0, gen1, gen2, allocated)


def metric_value(benchmark: dict, metric_id: str, display_name: str) -> float:
    for metric in benchmark.get("Metrics") or []:
        descriptor = metric.get("Descriptor") or {}
        if descriptor.get("Id") == metric_id or descriptor.get("DisplayName") == display_name:
            return to_float(metric.get("Value"))

    return math.nan


def gc_metric_value(
    benchmark: dict, memory: dict, metric_id: str, memory_key: str, display_name: str
) -> float:
    value = metric_value(benchmark, metric_id, display_name)
    if math.isfinite(value):
        return value

    collections = to_float(memory.get(memory_key))
    total_operations = to_float(memory.get("TotalOperations"))
    if math.isfinite(collections) and math.isfinite(total_operations) and total_operations > 0:
        return collections / total_operations * 1000

    return 0.0


def build_external_delta_rows(
    comparison: dict[ComparisonKey, dict[str, MetricRecord]]
) -> list[ExternalDeltaRow]:
    rows: list[ExternalDeltaRow] = []
    for key in sorted(comparison):
        runtimes = comparison[key]
        nova = runtimes.get(NOVA_RUNTIME)
        if nova is None:
            continue

        for runtime in sorted(runtimes):
            if runtime == NOVA_RUNTIME:
                continue
            comparison_record = runtimes[runtime]
            rows.append(
                ExternalDeltaRow(
                    key,
                    runtime,
                    nova.parameter_display,
                    nova.metrics,
                    comparison_record.metrics,
                    record_contributes_to_changed_signal(comparison_record),
                )
            )

    return rows


def build_runtime_matrix_rows(
    comparison: dict[ComparisonKey, dict[str, MetricRecord]]
) -> list[RuntimeMatrixRow]:
    rows: list[RuntimeMatrixRow] = []
    for key in sorted(comparison, key=comparison_matrix_sort_key):
        runtimes = comparison[key]
        nova = runtimes.get(NOVA_RUNTIME)
        if nova is None:
            continue

        comparisons = []
        for runtime in sorted(runtimes, key=runtime_sort_key):
            if runtime == NOVA_RUNTIME:
                continue

            record = runtimes[runtime]
            comparisons.append(
                RuntimeComparison(
                    runtime,
                    record.runtime_display_name or runtime,
                    record.metrics,
                    record.runtime_context,
                    record.runtime_kind,
                    record.show_delta_percent,
                )
            )
        rows.append(RuntimeMatrixRow(key, nova.parameter_display, nova.metrics, tuple(comparisons)))

    return rows


def build_missing_expected_runtime_cells(
    comparison: dict[ComparisonKey, dict[str, MetricRecord]],
    expect_lua_cli: bool,
) -> list[MissingRuntimeCell]:
    cells: list[MissingRuntimeCell] = []
    for key in sorted(comparison, key=comparison_matrix_sort_key):
        runtimes = comparison[key]
        if NOVA_RUNTIME not in runtimes:
            continue

        for runtime in expected_runtimes_for_key(key, expect_lua_cli):
            if runtime not in runtimes:
                cells.append(MissingRuntimeCell(key, runtime))

    return cells


def expected_runtimes_for_key(key: ComparisonKey, expect_lua_cli: bool) -> tuple[str, ...]:
    if expect_lua_cli and key.operation == "Execute":
        return EXPECTED_EXTERNAL_RUNTIMES + ("Lua",)

    return EXPECTED_EXTERNAL_RUNTIMES


def comparison_matrix_sort_key(key: ComparisonKey) -> tuple[str, str, str]:
    return (key.parameters, key.operation, key.summary)


def runtime_sort_key(runtime: str) -> tuple[int, str]:
    try:
        index = RUNTIME_PREFIXES.index(runtime)
    except ValueError:
        index = len(RUNTIME_PREFIXES)
    return (index, runtime)


def build_self_delta_rows(
    current: dict[BenchmarkKey, MetricRecord],
    baseline: dict[BenchmarkKey, MetricRecord],
) -> list[SelfDeltaRow]:
    rows: list[SelfDeltaRow] = []
    for key in sorted(current):
        if key not in baseline:
            continue

        rows.append(
            SelfDeltaRow(
                key,
                current[key].parameter_display,
                current[key].metrics,
                baseline[key].metrics,
            )
        )

    return rows


def build_phase_gate_failures(
    current: dict[ComparisonKey, dict[str, MetricRecord]],
    baseline: dict[ComparisonKey, dict[str, MetricRecord]],
    nlua_ratio_threshold: float,
) -> list[PhaseGateFailure]:
    failures: list[PhaseGateFailure] = []
    current_keys = {key for key, runtimes in current.items() if NOVA_RUNTIME in runtimes}
    baseline_keys = {key for key, runtimes in baseline.items() if NOVA_RUNTIME in runtimes}

    for key in sorted(current_keys - baseline_keys, key=comparison_matrix_sort_key):
        failures.append(
            PhaseGateFailure(
                key,
                "shape",
                "Current comparison row has no checked-in phase baseline.",
            )
        )
    for key in sorted(baseline_keys - current_keys, key=comparison_matrix_sort_key):
        failures.append(
            PhaseGateFailure(
                key,
                "shape",
                "Checked-in phase baseline row is absent from current artifacts.",
            )
        )

    for key in sorted(current_keys & baseline_keys, key=comparison_matrix_sort_key):
        current_runtimes = current[key]
        baseline_runtimes = baseline[key]
        current_nova = current_runtimes.get(NOVA_RUNTIME)
        baseline_nova = baseline_runtimes.get(NOVA_RUNTIME)
        current_nlua = current_runtimes.get("NLua")
        baseline_nlua = baseline_runtimes.get("NLua")

        if current_nova is None or baseline_nova is None:
            continue

        allocation_failure = allocation_gate_failure(
            current_nova.metrics.allocated_bytes,
            baseline_nova.metrics.allocated_bytes,
        )
        if allocation_failure:
            failures.append(PhaseGateFailure(key, "NovaSharp B/op", allocation_failure))

        if current_nlua is None:
            failures.append(
                PhaseGateFailure(
                    key,
                    "NLua ratio",
                    "Current comparison row is missing NLua, so NovaSharp/NLua ratios cannot be checked.",
                )
            )
            continue
        if baseline_nlua is None:
            failures.append(
                PhaseGateFailure(
                    key,
                    "NLua ratio",
                    "Phase baseline row is missing NLua, so NovaSharp/NLua ratios cannot be checked.",
                )
            )
            continue

        append_ratio_gate_failure(
            failures,
            key,
            "mean",
            current_nova.metrics.mean_ns,
            current_nlua.metrics.mean_ns,
            baseline_nova.metrics.mean_ns,
            baseline_nlua.metrics.mean_ns,
            nlua_ratio_threshold,
        )
        append_ratio_gate_failure(
            failures,
            key,
            "P95",
            current_nova.metrics.p95_ns,
            current_nlua.metrics.p95_ns,
            baseline_nova.metrics.p95_ns,
            baseline_nlua.metrics.p95_ns,
            nlua_ratio_threshold,
        )

    return failures


def allocation_gate_failure(current: float, baseline: float) -> str:
    if not math.isfinite(current):
        return "Current NovaSharp allocation measurement is missing."
    if not math.isfinite(baseline):
        return "Phase baseline NovaSharp allocation measurement is missing."
    if current <= baseline:
        return ""

    delta = current - baseline
    tolerance = phase_allocation_tolerance(baseline)
    if delta <= tolerance:
        return ""

    return (
        "NovaSharp allocation increased from "
        f"{format_bytes(baseline)} to {format_bytes(current)} "
        f"({format_bytes_delta(delta)}; allowed {format_bytes(tolerance)})."
    )


def phase_allocation_tolerance(baseline: float) -> float:
    if baseline < PHASE_ALLOCATION_EXACT_LIMIT_BYTES:
        return 0.0

    return max(
        PHASE_ALLOCATION_ABSOLUTE_TOLERANCE_BYTES,
        baseline * PHASE_ALLOCATION_RELATIVE_TOLERANCE,
    )


def append_ratio_gate_failure(
    failures: list[PhaseGateFailure],
    key: ComparisonKey,
    metric_name: str,
    current_nova: float,
    current_nlua: float,
    baseline_nova: float,
    baseline_nlua: float,
    threshold: float,
) -> None:
    current_ratio = ratio_or_nan(current_nova, current_nlua)
    baseline_ratio = ratio_or_nan(baseline_nova, baseline_nlua)
    ratio_delta = fraction_delta(current_ratio, baseline_ratio)
    if math.isfinite(ratio_delta) and ratio_delta <= threshold:
        return

    if not math.isfinite(current_ratio):
        message = f"Current NovaSharp/NLua {metric_name} ratio is unavailable."
    elif not math.isfinite(baseline_ratio):
        message = f"Phase baseline NovaSharp/NLua {metric_name} ratio is unavailable."
    else:
        message = (
            f"NovaSharp/NLua {metric_name} ratio regressed from "
            f"{baseline_ratio:.3f}x to {current_ratio:.3f}x "
            f"({format_percent(percentage_delta(current_ratio, baseline_ratio))})."
        )

    failures.append(PhaseGateFailure(key, f"NLua {metric_name} ratio", message))


def ratio_or_nan(numerator: float, denominator: float) -> float:
    if not math.isfinite(numerator) or not math.isfinite(denominator) or denominator == 0:
        return math.nan

    return numerator / denominator


def external_row_changed(row: ExternalDeltaRow, tolerance: float) -> bool:
    if not row.contributes_to_changed_signal:
        return False

    return metrics_changed(row.nova, row.comparison, tolerance)


def record_contributes_to_changed_signal(record: MetricRecord) -> bool:
    return record.show_delta_percent and record.runtime_kind != "LuaCliWallTime"


def self_row_changed(row: SelfDeltaRow, tolerance: float) -> bool:
    return metrics_changed(row.current, row.baseline, tolerance)


def metrics_changed(current: Metrics, baseline: Metrics, tolerance: float) -> bool:
    return any(
        abs(value) >= tolerance
        for value in metric_fraction_deltas(current, baseline)
        if math.isfinite(value) or math.isinf(value)
    )


def external_row_regressed(row: ExternalDeltaRow, threshold: float) -> bool:
    return metrics_regressed(row.nova, row.comparison, threshold)


def self_row_regressed(row: SelfDeltaRow, threshold: float) -> bool:
    return metrics_regressed(row.current, row.baseline, threshold)


def metrics_regressed(current: Metrics, baseline: Metrics, threshold: float) -> bool:
    return any(
        value >= threshold
        for value in metric_fraction_deltas(current, baseline)
        if math.isfinite(value) or math.isinf(value)
    )


def metric_fraction_deltas(current: Metrics, baseline: Metrics) -> tuple[float, ...]:
    return (
        fraction_delta(current.mean_ns, baseline.mean_ns),
        fraction_delta(current.p95_ns, baseline.p95_ns),
        fraction_delta(current.gen0_per_1k, baseline.gen0_per_1k),
        fraction_delta(current.gen1_per_1k, baseline.gen1_per_1k),
        fraction_delta(current.gen2_per_1k, baseline.gen2_per_1k),
        fraction_delta(current.allocated_bytes, baseline.allocated_bytes),
    )


def render_markdown(
    external_rows: list[ExternalDeltaRow],
    runtime_matrix_rows: list[RuntimeMatrixRow],
    self_rows: list[SelfDeltaRow],
    current_root: Path,
    comparison_root: Path,
    self_baseline_root: Path,
    current_count: int,
    self_baseline_count: int,
    comparison_group_count: int,
    comparison_groups_without_nova: list[ComparisonKey],
    missing_expected_runtime_cells: list[MissingRuntimeCell],
    missing_lua_cli_rows: list[MissingRuntimeCell],
    phase_baseline: dict[ComparisonKey, dict[str, MetricRecord]],
    phase_baseline_path: Path,
    phase_gate_failures: list[PhaseGateFailure],
    current_without_baseline: list[BenchmarkKey],
    baseline_without_current: list[BenchmarkKey],
) -> str:
    lines = [
        "## Benchmark Comparison Deltas",
        "",
        "Lower values are better for mean, P95, GC collections, and allocated bytes. "
        "Positive deltas mean NovaSharp is above the comparison row or self baseline.",
        "",
        "External comparison rows are apples-to-apples: NovaSharp and the comparison "
        "runtimes are restored, built, and benchmarked in the same workflow job on the same runner.",
        "External rows are report-only; `regressed=true` is reserved for NovaSharp "
        "self-baseline regressions once checked-in baseline artifacts exist.",
        "",
        f"- Current NovaSharp artifacts: `{repo_relative(current_root)}`",
        f"- Same-run comparison artifacts: `{repo_relative(comparison_root)}`",
        f"- Checked-in self baseline artifacts: `{repo_relative(self_baseline_root)}`",
        f"- Same-run comparison groups: {comparison_group_count}",
        f"- Same-run scenario/operation rows with NovaSharp: {len(runtime_matrix_rows)}",
        f"- Same-run external runtime cells: {len(external_rows)}",
        f"- Expected external runtime cells missing: {len(missing_expected_runtime_cells)}",
        f"- Expected reference lua CLI rows missing: {len(missing_lua_cli_rows)}",
        f"- Phase A0 scoreboard baseline rows: {len(phase_baseline)}",
        f"- Phase A0 gate failures: {len(phase_gate_failures)}",
        f"- Self baseline matches: {len(self_rows)} of {current_count} current rows and {self_baseline_count} baseline rows",
        "",
    ]

    append_comparison_key_preview(
        lines,
        "Comparison groups without a NovaSharp row",
        comparison_groups_without_nova,
    )
    append_missing_runtime_cell_preview(
        lines,
        "Expected external runtime cells missing",
        missing_expected_runtime_cells,
    )

    render_phase_scoreboard_section(
        lines,
        runtime_matrix_rows,
        phase_baseline,
        phase_baseline_path,
        phase_gate_failures,
    )
    render_external_section(lines, runtime_matrix_rows)
    render_self_section(
        lines,
        self_rows,
        self_baseline_root,
        current_without_baseline,
        baseline_without_current,
    )

    return "\n".join(lines).rstrip() + "\n"


def render_phase_scoreboard_section(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    phase_baseline: dict[ComparisonKey, dict[str, MetricRecord]],
    phase_baseline_path: Path,
    phase_gate_failures: list[PhaseGateFailure],
) -> None:
    lines.extend(
        [
            "### Phase A0 Scoreboard",
            "",
            "This scoreboard is the compact Phase A0 view: rows are benchmark scenarios/operations, "
            "and columns show NovaSharp current, NovaSharp baseline, MoonSharp, NLua, Lua-CSharp, "
            "and reference `lua` CLI wall-time context when present.",
            "The phase gate, when enabled, checks NovaSharp/NLua same-run timing ratios against the "
            "checked-in phase baseline and checks NovaSharp allocated B/op regressions with a small "
            "runner-noise tolerance.",
            "",
            f"- Phase baseline JSON: `{repo_relative(phase_baseline_path)}`",
            "",
        ]
    )

    if not phase_baseline_path.exists():
        lines.extend(
            [
                "No checked-in Phase A0 scoreboard baseline was found. Run the comparison suite "
                "and pass `--write-phase-baseline` after a representative run to create one.",
                "",
            ]
        )

    append_phase_gate_failure_preview(lines, phase_gate_failures)

    if not rows:
        lines.extend(
            [
                "No current comparison rows were found for the Phase A0 scoreboard.",
                "",
            ]
        )
        return

    render_phase_time_scoreboard(lines, rows, phase_baseline)
    render_phase_memory_scoreboard(lines, rows, phase_baseline)


def render_phase_time_scoreboard(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    phase_baseline: dict[ComparisonKey, dict[str, MetricRecord]],
) -> None:
    headers = [
        "Scenario",
        "Operation",
        f"{NOVA_RUNTIME} Current Mean / P95",
        f"{NOVA_RUNTIME} Baseline Mean / P95",
    ]
    alignments = ["---", "---", "---:", "---:"]
    for runtime in SCOREBOARD_RUNTIMES:
        headers.append(f"{scoreboard_runtime_label(runtime)} Mean / P95")
        alignments.append("---:")

    lines.extend(["#### Scoreboard Time", "", render_markdown_row(headers), render_markdown_row(alignments)])
    for row in rows:
        cells = [
            scenario_display(row),
            row.key.operation,
            format_time_pair(row.nova),
            format_scoreboard_time_cell(phase_baseline_record(phase_baseline, row.key, NOVA_RUNTIME)),
        ]
        for runtime in SCOREBOARD_RUNTIMES:
            cells.append(format_scoreboard_time_cell(current_record_for_runtime(row, runtime)))

        lines.append(render_markdown_row(cells))
    lines.append("")


def render_phase_memory_scoreboard(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    phase_baseline: dict[ComparisonKey, dict[str, MetricRecord]],
) -> None:
    headers = [
        "Scenario",
        "Operation",
        f"{NOVA_RUNTIME} Current Alloc / GC0/1/2",
        f"{NOVA_RUNTIME} Baseline Alloc / GC0/1/2",
    ]
    alignments = ["---", "---", "---:", "---:"]
    for runtime in SCOREBOARD_RUNTIMES:
        headers.append(f"{scoreboard_runtime_label(runtime)} Alloc / GC0/1/2")
        alignments.append("---:")

    lines.extend(
        [
            "#### Scoreboard Memory and GC",
            "",
            "GC columns are collections per 1,000 operations. Reference `lua` CLI rows are process wall-time context, so their memory cells are `-`.",
            "",
            render_markdown_row(headers),
            render_markdown_row(alignments),
        ]
    )
    for row in rows:
        cells = [
            scenario_display(row),
            row.key.operation,
            format_memory_gc_pair(row.nova),
            format_scoreboard_memory_cell(
                phase_baseline_record(phase_baseline, row.key, NOVA_RUNTIME)
            ),
        ]
        for runtime in SCOREBOARD_RUNTIMES:
            cells.append(format_scoreboard_memory_cell(current_record_for_runtime(row, runtime)))

        lines.append(render_markdown_row(cells))
    lines.append("")


def current_record_for_runtime(row: RuntimeMatrixRow, runtime: str) -> MetricRecord | None:
    if runtime == NOVA_RUNTIME:
        return MetricRecord(row.nova, row.parameter_display)

    for comparison in row.comparisons:
        if comparison.runtime == runtime:
            return MetricRecord(
                comparison.metrics,
                row.parameter_display,
                runtime_display_name=comparison.display_name,
                runtime_context=comparison.runtime_context,
                runtime_kind=comparison.runtime_kind,
                show_delta_percent=comparison.show_delta_percent,
            )

    return None


def phase_baseline_record(
    phase_baseline: dict[ComparisonKey, dict[str, MetricRecord]],
    key: ComparisonKey,
    runtime: str,
) -> MetricRecord | None:
    return phase_baseline.get(key, {}).get(runtime)


def format_scoreboard_time_cell(record: MetricRecord | None) -> str:
    return format_time_pair(record.metrics) if record is not None else "-"


def format_scoreboard_memory_cell(record: MetricRecord | None) -> str:
    if record is None or record.runtime_kind == "LuaCliWallTime":
        return "-"

    return format_memory_gc_pair(record.metrics)


def scoreboard_runtime_label(runtime: str) -> str:
    if runtime == "Lua":
        return "Lua CLI"

    return runtime


def render_external_section(lines: list[str], rows: list[RuntimeMatrixRow]) -> None:
    lines.extend(
        [
            "### Same-Run Runtime Matrix",
            "",
        ]
    )

    if not rows:
        lines.extend(
            [
                "No same-run external comparison rows were found. Run the comparison "
                "BenchmarkDotNet project and pass its artifacts with `--comparison-root`.",
                "",
            ]
        )
        return

    runtimes = external_runtimes_for_matrix(rows)
    lines.extend(
        [
            "Each row is one scenario/operation from the same BenchmarkDotNet comparison run. "
            "NovaSharp values are raw results; external columns show raw results plus the NovaSharp delta against that runtime.",
            "NovaSharp comparison rows intentionally use the prepared-handle public API "
            "(`PrepareString`/`CompiledScript.Execute`) so the matrix reports the preferred execution surface.",
            "Reference `lua` CLI rows, when present, are synthetic out-of-process wall-time context "
            "that includes process startup, parse, compile, and execution. Memory and GC cells are shown as `-` "
            "because the CLI context does not report managed allocations.",
            "",
        ]
    )
    render_runtime_contexts(lines, rows)
    render_novasharp_raw_results(lines, rows)
    render_runtime_time_matrix(lines, rows, runtimes)
    render_runtime_memory_matrix(lines, rows, runtimes)


def render_runtime_contexts(lines: list[str], rows: list[RuntimeMatrixRow]) -> None:
    contexts: dict[str, str] = {}
    for row in rows:
        for comparison in row.comparisons:
            if comparison.runtime_context:
                contexts[comparison.display_name] = comparison.runtime_context

    if not contexts:
        return

    lines.extend(["#### Runtime Context", ""])
    for display_name in sorted(contexts):
        lines.append(f"- {display_name}: {contexts[display_name]}")
    lines.append("")


def render_novasharp_raw_results(lines: list[str], rows: list[RuntimeMatrixRow]) -> None:
    headers = [
        "Scenario",
        "Operation",
        f"{NOVA_RUNTIME} Mean / P95",
        f"{NOVA_RUNTIME} Alloc / GC0/1/2",
    ]
    alignments = ["---", "---", "---:", "---:"]
    lines.extend(
        [
            f"#### {NOVA_RUNTIME} Raw Results",
            "",
            "This table repeats NovaSharp's own same-run values in a narrow form before the wider cross-runtime delta matrix.",
            "",
            render_markdown_row(headers),
            render_markdown_row(alignments),
        ]
    )
    for row in rows:
        lines.append(
            render_markdown_row(
                [
                    scenario_display(row),
                    row.key.operation,
                    format_time_pair(row.nova),
                    format_memory_gc_pair(row.nova),
                ]
            )
        )
    lines.append("")


def render_runtime_time_matrix(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    runtimes: list[RuntimeColumn],
) -> None:
    headers = ["Scenario", "Operation", f"{NOVA_RUNTIME} Mean / P95"]
    alignments = ["---", "---", "---:"]
    for runtime in runtimes:
        headers.extend(
            [
                f"{runtime.display_name} Mean / P95",
                f"{NOVA_RUNTIME} Delta vs {runtime.display_name}",
            ]
        )
        alignments.extend(["---:", "---:"])

    lines.extend(["#### Time", "", render_markdown_row(headers), render_markdown_row(alignments)])
    for row in rows:
        comparison_by_runtime = {comparison.runtime: comparison for comparison in row.comparisons}
        cells = [
            scenario_display(row),
            row.key.operation,
            format_time_pair(row.nova),
        ]
        for runtime in runtimes:
            comparison = comparison_by_runtime.get(runtime.runtime)
            if comparison is None:
                cells.extend(["-", "-"])
                continue

            cells.extend(
                [
                    format_time_pair(comparison.metrics),
                    format_time_pair_delta(
                        row.nova,
                        comparison.metrics,
                        include_percent=comparison.show_delta_percent,
                    ),
                ]
            )

        lines.append(render_markdown_row(cells))
    lines.append("")


def render_runtime_memory_matrix(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    runtimes: list[RuntimeColumn],
) -> None:
    headers = ["Scenario", "Operation", f"{NOVA_RUNTIME} Alloc / GC0/1/2"]
    alignments = ["---", "---", "---:"]
    for runtime in runtimes:
        headers.extend(
            [
                f"{runtime.display_name} Alloc / GC0/1/2",
                f"{NOVA_RUNTIME} Delta vs {runtime.display_name}",
            ]
        )
        alignments.extend(["---:", "---:"])

    lines.extend(
        [
            "#### Memory and GC",
            "",
            "GC columns are collections per 1,000 operations.",
            "A `-` cell means that runtime did not report memory or GC diagnostics for that row.",
            "",
            render_markdown_row(headers),
            render_markdown_row(alignments),
        ]
    )
    for row in rows:
        comparison_by_runtime = {comparison.runtime: comparison for comparison in row.comparisons}
        cells = [
            scenario_display(row),
            row.key.operation,
            format_memory_gc_pair(row.nova),
        ]
        for runtime in runtimes:
            comparison = comparison_by_runtime.get(runtime.runtime)
            if comparison is None:
                cells.extend(["-", "-"])
                continue

            cells.extend(
                [
                    format_memory_gc_pair(comparison.metrics),
                    format_memory_gc_delta(
                        row.nova,
                        comparison.metrics,
                        include_percent=comparison.show_delta_percent,
                    ),
                ]
            )

        lines.append(render_markdown_row(cells))
    lines.append("")


def external_runtimes_for_matrix(rows: list[RuntimeMatrixRow]) -> list[RuntimeColumn]:
    columns: dict[str, RuntimeColumn] = {}
    for row in rows:
        for comparison in row.comparisons:
            columns.setdefault(
                comparison.runtime,
                RuntimeColumn(
                    comparison.runtime,
                    comparison.display_name,
                    comparison.show_delta_percent,
                ),
            )

    return sorted(columns.values(), key=lambda column: runtime_sort_key(column.runtime))


def scenario_display(row: RuntimeMatrixRow) -> str:
    return scenario_display_from_key(row.key, row.parameter_display)


def scenario_display_from_key(key: ComparisonKey, parameter_display: str = "") -> str:
    display = parameter_display or parameter_display_from_signature(key.parameters)
    parameters = split_parameter_display(display)
    scenario = next((value for name, value in parameters if name == "Scenario"), "")
    if not scenario:
        return display or key.summary or "-"

    remaining = [(name, value) for name, value in parameters if name != "Scenario"]
    if not remaining:
        return scenario

    return f"{scenario} ({', '.join(f'{name}={value}' for name, value in remaining)})"


def parameter_display_from_signature(signature: str) -> str:
    normalized = normalize_cell(signature)
    if not normalized:
        return ""

    parts = []
    for part in normalized.split("|"):
        if ":" not in part:
            continue
        parts.append(part.replace(":", "=", 1))
    return ", ".join(parts)


def split_parameter_display(value: str) -> list[tuple[str, str]]:
    parameters: list[tuple[str, str]] = []
    for raw_part in normalize_cell(value).split(","):
        if "=" not in raw_part:
            continue
        name, raw_value = raw_part.split("=", 1)
        parameter_name = normalize_cell(name)
        parameter_value = normalize_cell(raw_value)
        if parameter_name and parameter_value:
            parameters.append((parameter_name, parameter_value))
    return parameters


def render_markdown_row(cells: list[str]) -> str:
    return "| " + " | ".join(escape_markdown_cell(cell) for cell in cells) + " |"


def escape_markdown_cell(value: object) -> str:
    return str(value).replace("\n", " ").replace("|", "\\|")


def format_time_pair(metrics: Metrics) -> str:
    return f"{format_time(metrics.mean_ns)} / {format_time(metrics.p95_ns)}"


def format_time_pair_delta(current: Metrics, baseline: Metrics, include_percent: bool = True) -> str:
    if include_percent:
        return " / ".join(
            [
                format_metric_delta(current.mean_ns, baseline.mean_ns, format_time_delta),
                format_metric_delta(current.p95_ns, baseline.p95_ns, format_time_delta),
            ]
        )

    return " / ".join(
        [
            format_time_delta(current.mean_ns - baseline.mean_ns),
            format_time_delta(current.p95_ns - baseline.p95_ns),
        ]
    )


def format_memory_gc_pair(metrics: Metrics) -> str:
    return f"{format_bytes(metrics.allocated_bytes)} / {format_gc_triplet(metrics)}"


def format_memory_gc_delta(
    current: Metrics,
    baseline: Metrics,
    include_percent: bool = True,
) -> str:
    if math.isfinite(current.allocated_bytes) and math.isfinite(baseline.allocated_bytes):
        allocation_delta = format_bytes_delta(current.allocated_bytes - baseline.allocated_bytes)
        if include_percent:
            allocation_delta = (
                f"{allocation_delta} "
                f"({format_percent(percentage_delta(current.allocated_bytes, baseline.allocated_bytes))})"
            )
    else:
        allocation_delta = "-"
    return f"{allocation_delta} / {format_gc_delta_triplet(current, baseline)}"


def render_self_section(
    lines: list[str],
    rows: list[SelfDeltaRow],
    self_baseline_root: Path,
    current_without_baseline: list[BenchmarkKey],
    baseline_without_current: list[BenchmarkKey],
) -> None:
    lines.extend(
        [
            "### Self Baseline Comparisons",
            "",
        ]
    )

    if not self_baseline_root.exists():
        lines.extend(
            [
                "No checked-in self baseline artifacts were found at "
                f"`{repo_relative(self_baseline_root)}`. Add BenchmarkDotNet JSON artifacts there "
                "or pass `--self-baseline-root` to enable NovaSharp-vs-NovaSharp deltas.",
                "",
            ]
        )
        return

    append_benchmark_key_preview(
        lines,
        "Current rows without self baseline",
        current_without_baseline,
    )
    append_benchmark_key_preview(
        lines,
        "Self baseline rows not present in current artifacts",
        baseline_without_current,
    )

    if not rows:
        lines.extend(
            [
                "No current NovaSharp rows matched the checked-in self baseline artifacts.",
                "",
            ]
        )
        return

    lines.extend(
        [
            "| Summary | Method | Parameters | Current Mean | Baseline Mean | Mean Delta | Current P95 | Baseline P95 | P95 Delta | Current GC0/1/2 | Baseline GC0/1/2 | GC Delta | Current Alloc | Baseline Alloc | Alloc Delta |",
            "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
        ]
    )

    for row in rows:
        lines.append(
            " | ".join(
                [
                    f"| {row.key.summary}",
                    row.key.method,
                    row.parameter_display or "-",
                    format_time(row.current.mean_ns),
                    format_time(row.baseline.mean_ns),
                    format_metric_delta(row.current.mean_ns, row.baseline.mean_ns, format_time_delta),
                    format_time(row.current.p95_ns),
                    format_time(row.baseline.p95_ns),
                    format_metric_delta(row.current.p95_ns, row.baseline.p95_ns, format_time_delta),
                    format_gc_triplet(row.current),
                    format_gc_triplet(row.baseline),
                    format_gc_delta_triplet(row.current, row.baseline),
                    format_bytes(row.current.allocated_bytes),
                    format_bytes(row.baseline.allocated_bytes),
                    f"{format_bytes_delta(row.current.allocated_bytes - row.baseline.allocated_bytes)} ({format_percent(percentage_delta(row.current.allocated_bytes, row.baseline.allocated_bytes))}) |",
                ]
            )
        )

    lines.append("")


def append_benchmark_key_preview(lines: list[str], title: str, keys: list[BenchmarkKey]) -> None:
    if not keys:
        return

    lines.append(f"#### {title}")
    lines.append("")
    for key in keys[:10]:
        lines.append(f"- {key.summary} / {key.method} / {key.parameters or '-'}")
    if len(keys) > 10:
        lines.append(f"- ... {len(keys) - 10} more")
    lines.append("")


def append_comparison_key_preview(
    lines: list[str], title: str, keys: list[ComparisonKey]
) -> None:
    if not keys:
        return

    lines.append(f"#### {title}")
    lines.append("")
    for key in keys[:10]:
        lines.append(f"- {key.summary} / {key.operation} / {key.parameters or '-'}")
    if len(keys) > 10:
        lines.append(f"- ... {len(keys) - 10} more")
    lines.append("")


def append_missing_runtime_cell_preview(
    lines: list[str], title: str, cells: list[MissingRuntimeCell]
) -> None:
    if not cells:
        return

    lines.append(f"#### {title}")
    lines.append("")
    lines.append(
        "These expected comparison cells were absent from the BenchmarkDotNet JSON. "
        "Check benchmark descriptions and runtime setup before trusting the matrix."
    )
    lines.append("")
    for cell in cells[:10]:
        lines.append(
            f"- {cell.key.summary} / {cell.key.operation} / "
            f"{cell.key.parameters or '-'} / {cell.runtime}"
        )
    if len(cells) > 10:
        lines.append(f"- ... {len(cells) - 10} more")
    lines.append("")


def append_phase_gate_failure_preview(
    lines: list[str],
    failures: list[PhaseGateFailure],
) -> None:
    if not failures:
        return

    lines.extend(
        [
            "#### Phase A0 Gate Failures",
            "",
            render_markdown_row(["Scenario", "Operation", "Metric", "Failure"]),
            render_markdown_row(["---", "---", "---", "---"]),
        ]
    )
    for failure in failures[:20]:
        lines.append(
            render_markdown_row(
                [
                    scenario_display_from_key(failure.key),
                    failure.key.operation,
                    failure.metric,
                    failure.message,
                ]
            )
        )
    if len(failures) > 20:
        lines.append("")
        lines.append(f"- ... {len(failures) - 20} more")
    lines.append("")


def repo_relative(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def normalize_summary_name(value: str) -> str:
    normalized = normalize_cell(value)
    normalized = re.sub(r"-\d{8}-\d{6}$", "", normalized)
    if normalized.startswith("WallstopStudios."):
        normalized = normalized[len("WallstopStudios.") :]
    return normalized


def normalize_method_name(value: str) -> str:
    return normalize_cell(value)


def normalize_cell(value: object) -> str:
    if value is None:
        return ""
    text = str(value).replace("**", "").strip().strip("'\"`")
    text = re.sub(r"\s+", " ", text)
    return text


def normalize_parameter_name(name: str) -> str:
    normalized = normalize_cell(name)
    return {
        "ScenarioName": "Scenario",
        "ComplexityName": "Complexity",
    }.get(normalized, normalized)


def build_parameter_signature_from_text(value: str) -> str:
    normalized = normalize_cell(value)
    if not normalized:
        return ""

    parts: list[str] = []
    for raw_part in normalized.split(","):
        if "=" not in raw_part:
            continue
        name, raw_value = raw_part.split("=", 1)
        parameter_name = normalize_parameter_name(name)
        parameter_value = normalize_cell(raw_value)
        if parameter_name and parameter_value:
            parts.append(f"{parameter_name}:{parameter_value}")
    return "|".join(sorted(parts))


def build_parameter_display_from_json(benchmark: dict) -> str:
    signature = build_parameter_signature_from_text(benchmark.get("Parameters", ""))
    if not signature:
        return ""
    return ", ".join(part.replace(":", "=", 1) for part in signature.split("|"))


def runtime_display_name_from_json(benchmark: dict) -> str:
    return normalize_cell(benchmark.get("RuntimeDisplayName", ""))


def runtime_context_from_json(benchmark: dict) -> str:
    return normalize_cell(benchmark.get("RuntimeContext", ""))


def runtime_kind_from_json(benchmark: dict) -> str:
    return normalize_cell(benchmark.get("RuntimeKind", ""))


def show_delta_percent_from_json(benchmark: dict) -> bool:
    if runtime_kind_from_json(benchmark) == "LuaCliWallTime":
        return False

    value = benchmark.get("ShowDeltaPercent")
    if isinstance(value, bool):
        return value
    return True


def to_float(value: object) -> float:
    try:
        return float(value)
    except (TypeError, ValueError):
        return math.nan


def fraction_delta(current: float, baseline: float) -> float:
    if not math.isfinite(current) or not math.isfinite(baseline):
        return math.nan
    if baseline == 0:
        if current == 0:
            return 0.0
        return math.inf if current > 0 else -math.inf
    return (current - baseline) / baseline


def percentage_delta(current: float, baseline: float) -> float:
    value = fraction_delta(current, baseline)
    return value * 100 if math.isfinite(value) else value


def format_metric_delta(
    current: float,
    baseline: float,
    formatter,
) -> str:
    return f"{formatter(current - baseline)} ({format_percent(percentage_delta(current, baseline))})"


def format_time(ns: float) -> str:
    if not math.isfinite(ns):
        return "-"
    return format_time_magnitude(abs(ns))


def format_time_delta(ns: float) -> str:
    if not math.isfinite(ns):
        return "-"
    if abs(ns) < 1e-9:
        return "0 ns"
    prefix = "+" if ns > 0 else "-"
    return f"{prefix}{format_time_magnitude(abs(ns))}"


def format_time_magnitude(ns: float) -> str:
    value = ns
    unit = "ns"
    if value >= 1_000_000_000:
        value /= 1_000_000_000
        unit = "s"
    elif value >= 1_000_000:
        value /= 1_000_000
        unit = "ms"
    elif value >= 1_000:
        value /= 1_000
        unit = "us"
    return f"{value:.3g} {unit}"


def format_gc_triplet(metrics: Metrics) -> str:
    return " / ".join(
        [
            format_count(metrics.gen0_per_1k),
            format_count(metrics.gen1_per_1k),
            format_count(metrics.gen2_per_1k),
        ]
    )


def format_gc_delta_triplet(current: Metrics, baseline: Metrics) -> str:
    return " / ".join(
        [
            format_count_delta(current.gen0_per_1k - baseline.gen0_per_1k),
            format_count_delta(current.gen1_per_1k - baseline.gen1_per_1k),
            format_count_delta(current.gen2_per_1k - baseline.gen2_per_1k),
        ]
    )


def format_count(value: float) -> str:
    if not math.isfinite(value):
        return "-"
    return f"{value:.4g}"


def format_count_delta(value: float) -> str:
    if not math.isfinite(value):
        return "-"
    if abs(value) < 1e-9:
        return "0"
    prefix = "+" if value > 0 else "-"
    return f"{prefix}{abs(value):.4g}"


def format_bytes(value: float) -> str:
    if not math.isfinite(value):
        return "-"
    return format_bytes_magnitude(abs(value))


def format_bytes_delta(value: float) -> str:
    if not math.isfinite(value):
        return "-"
    if abs(value) < 1e-9:
        return "0 B"
    prefix = "+" if value > 0 else "-"
    return f"{prefix}{format_bytes_magnitude(abs(value))}"


def format_bytes_magnitude(value: float) -> str:
    unit = "B"
    for next_unit in ("KB", "MB", "GB", "TB"):
        if value < 1024:
            break
        value /= 1024
        unit = next_unit
    return f"{value:.3g} {unit}"


def format_percent(value: float) -> str:
    if math.isinf(value):
        return "+inf%" if value > 0 else "-inf%"
    if not math.isfinite(value):
        return "-"
    return f"{value:+.2f}%"


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

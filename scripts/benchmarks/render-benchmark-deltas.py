#!/usr/bin/env python3
"""Render BenchmarkDotNet deltas from same-run comparison artifacts."""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_CURRENT_ROOT = Path("BenchmarkDotNet.Artifacts")
DEFAULT_COMPARISON_ROOT = Path("BenchmarkDotNet.Artifacts")
DEFAULT_SELF_BASELINE_ROOT = Path("docs/performance-history/current-baseline")
DEFAULT_OUTPUT = Path("artifacts/benchmark-deltas.md")
DEFAULT_TOLERANCE = 0.02
DEFAULT_REGRESSION_THRESHOLD = 0.10
NOVA_RUNTIME = "NovaSharp"
RUNTIME_PREFIXES = ("NovaSharp", "MoonSharp", "NLua", "KeraLua", "Lua")


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


@dataclass(frozen=True)
class ExternalDeltaRow:
    key: ComparisonKey
    runtime: str
    parameter_display: str
    nova: Metrics
    comparison: Metrics


@dataclass(frozen=True)
class RuntimeMatrixRow:
    key: ComparisonKey
    parameter_display: str
    nova: Metrics
    comparisons: tuple[tuple[str, Metrics], ...]


@dataclass(frozen=True)
class SelfDeltaRow:
    key: BenchmarkKey
    parameter_display: str
    current: Metrics
    baseline: Metrics


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
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    current_root = resolve_repo_path(args.current_root)
    comparison_root = resolve_repo_path(args.comparison_root)
    self_baseline_root = resolve_repo_path(args.self_baseline_root)
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
            current_without_baseline,
            baseline_without_current,
        ),
        encoding="utf-8",
    )

    print(f"changed={str(changed).lower()}")
    print(f"regressed={str(regressed).lower()}")
    print(f"external_rows={len(external_rows)}")
    print(f"self_rows={len(self_rows)}")
    print(f"output={repo_relative(output)}")
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
                benchmark_metrics,
                build_parameter_display_from_json(benchmark),
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
                benchmark_metrics,
                build_parameter_display_from_json(benchmark),
            )

    return metrics


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

    mean = to_float(statistics.get("Mean"))
    p95 = to_float(percentiles.get("P95", statistics.get("P95", mean)))
    allocated = metric_value(benchmark, "Allocated Memory", "Allocated")
    if not math.isfinite(allocated):
        allocated = to_float(memory.get("BytesAllocatedPerOperation"))
    if not math.isfinite(allocated):
        allocated = 0.0

    gen0 = gc_metric_value(benchmark, memory, "Gen0Collects", "Gen0Collections", "Gen0")
    gen1 = gc_metric_value(benchmark, memory, "Gen1Collects", "Gen1Collections", "Gen1")
    gen2 = gc_metric_value(benchmark, memory, "Gen2Collects", "Gen2Collections", "Gen2")

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

        comparisons = tuple(
            (runtime, runtimes[runtime].metrics)
            for runtime in sorted(runtimes, key=runtime_sort_key)
            if runtime != NOVA_RUNTIME
        )
        rows.append(RuntimeMatrixRow(key, nova.parameter_display, nova.metrics, comparisons))

    return rows


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


def external_row_changed(row: ExternalDeltaRow, tolerance: float) -> bool:
    return metrics_changed(row.nova, row.comparison, tolerance)


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
        f"- Self baseline matches: {len(self_rows)} of {current_count} current rows and {self_baseline_count} baseline rows",
        "",
    ]

    append_comparison_key_preview(
        lines,
        "Comparison groups without a NovaSharp row",
        comparison_groups_without_nova,
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
            "",
        ]
    )
    render_runtime_time_matrix(lines, rows, runtimes)
    render_runtime_memory_matrix(lines, rows, runtimes)


def render_runtime_time_matrix(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    runtimes: list[str],
) -> None:
    headers = ["Scenario", "Operation", f"{NOVA_RUNTIME} Mean / P95"]
    alignments = ["---", "---", "---:"]
    for runtime in runtimes:
        headers.extend(
            [
                f"{runtime} Mean / P95",
                f"{NOVA_RUNTIME} Delta vs {runtime}",
            ]
        )
        alignments.extend(["---:", "---:"])

    lines.extend(["#### Time", "", render_markdown_row(headers), render_markdown_row(alignments)])
    for row in rows:
        comparison_by_runtime = dict(row.comparisons)
        cells = [
            scenario_display(row),
            row.key.operation,
            format_time_pair(row.nova),
        ]
        for runtime in runtimes:
            comparison = comparison_by_runtime.get(runtime)
            if comparison is None:
                cells.extend(["-", "-"])
                continue

            cells.extend(
                [
                    format_time_pair(comparison),
                    format_time_pair_delta(row.nova, comparison),
                ]
            )

        lines.append(render_markdown_row(cells))
    lines.append("")


def render_runtime_memory_matrix(
    lines: list[str],
    rows: list[RuntimeMatrixRow],
    runtimes: list[str],
) -> None:
    headers = ["Scenario", "Operation", f"{NOVA_RUNTIME} Alloc / GC0/1/2"]
    alignments = ["---", "---", "---:"]
    for runtime in runtimes:
        headers.extend(
            [
                f"{runtime} Alloc / GC0/1/2",
                f"{NOVA_RUNTIME} Delta vs {runtime}",
            ]
        )
        alignments.extend(["---:", "---:"])

    lines.extend(
        [
            "#### Memory and GC",
            "",
            "GC columns are collections per 1,000 operations.",
            "",
            render_markdown_row(headers),
            render_markdown_row(alignments),
        ]
    )
    for row in rows:
        comparison_by_runtime = dict(row.comparisons)
        cells = [
            scenario_display(row),
            row.key.operation,
            format_memory_gc_pair(row.nova),
        ]
        for runtime in runtimes:
            comparison = comparison_by_runtime.get(runtime)
            if comparison is None:
                cells.extend(["-", "-"])
                continue

            cells.extend(
                [
                    format_memory_gc_pair(comparison),
                    format_memory_gc_delta(row.nova, comparison),
                ]
            )

        lines.append(render_markdown_row(cells))
    lines.append("")


def external_runtimes_for_matrix(rows: list[RuntimeMatrixRow]) -> list[str]:
    runtimes = {runtime for row in rows for runtime, _ in row.comparisons}
    return sorted(runtimes, key=runtime_sort_key)


def scenario_display(row: RuntimeMatrixRow) -> str:
    display = row.parameter_display or parameter_display_from_signature(row.key.parameters)
    parameters = split_parameter_display(display)
    scenario = next((value for name, value in parameters if name == "Scenario"), "")
    if not scenario:
        return display or "-"

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


def format_time_pair_delta(current: Metrics, baseline: Metrics) -> str:
    return " / ".join(
        [
            format_metric_delta(current.mean_ns, baseline.mean_ns, format_time_delta),
            format_metric_delta(current.p95_ns, baseline.p95_ns, format_time_delta),
        ]
    )


def format_memory_gc_pair(metrics: Metrics) -> str:
    return f"{format_bytes(metrics.allocated_bytes)} / {format_gc_triplet(metrics)}"


def format_memory_gc_delta(current: Metrics, baseline: Metrics) -> str:
    allocation_delta = (
        f"{format_bytes_delta(current.allocated_bytes - baseline.allocated_bytes)} "
        f"({format_percent(percentage_delta(current.allocated_bytes, baseline.allocated_bytes))})"
    )
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

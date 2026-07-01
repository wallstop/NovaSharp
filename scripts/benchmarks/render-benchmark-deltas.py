#!/usr/bin/env python3
"""Render BenchmarkDotNet deltas against the checked-in MoonSharp baseline."""

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
DEFAULT_BASELINE_DOC = Path("docs/Performance.md")
DEFAULT_OUTPUT = Path("artifacts/benchmark-deltas.md")
DEFAULT_TOLERANCE = 0.02
DEFAULT_REGRESSION_THRESHOLD = 0.10


@dataclass(frozen=True, order=True)
class BenchmarkKey:
    summary: str
    method: str
    parameters: str


@dataclass(frozen=True)
class Metrics:
    mean_ns: float
    p95_ns: float
    allocated_bytes: float


@dataclass(frozen=True)
class DeltaRow:
    key: BenchmarkKey
    parameter_display: str
    current: Metrics
    baseline: Metrics

    @property
    def mean_delta_percent(self) -> float:
        return percentage_delta(self.current.mean_ns, self.baseline.mean_ns)

    @property
    def p95_delta_percent(self) -> float:
        return percentage_delta(self.current.p95_ns, self.baseline.p95_ns)

    @property
    def allocation_delta_percent(self) -> float:
        return percentage_delta(self.current.allocated_bytes, self.baseline.allocated_bytes)

    @property
    def mean_delta_ns(self) -> float:
        return self.current.mean_ns - self.baseline.mean_ns

    @property
    def p95_delta_ns(self) -> float:
        return self.current.p95_ns - self.baseline.p95_ns

    @property
    def allocation_delta_bytes(self) -> float:
        return self.current.allocated_bytes - self.baseline.allocated_bytes


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--current-root",
        type=Path,
        default=DEFAULT_CURRENT_ROOT,
        help="Directory containing current BenchmarkDotNet artifacts.",
    )
    parser.add_argument(
        "--baseline-doc",
        type=Path,
        default=DEFAULT_BASELINE_DOC,
        help="Markdown document containing the frozen MoonSharp baseline.",
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
        help="Fractional worse-than-baseline threshold for regressed=true.",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    current_root = resolve_repo_path(args.current_root)
    baseline_doc = resolve_repo_path(args.baseline_doc)
    output = resolve_repo_path(args.output)

    current = load_current_metrics(current_root)
    baseline = load_moonsharp_baseline(baseline_doc)
    rows = build_delta_rows(current, baseline)
    current_without_baseline = sorted(key for key in current if key not in baseline)
    baseline_without_current = sorted(key for key in baseline if key not in current)

    changed = bool(current_without_baseline) or any(row_changed(row, args.tolerance) for row in rows)
    regressed = any(row_regressed(row, args.regression_threshold) for row in rows)

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(
        render_markdown(
            rows,
            current_root,
            baseline_doc,
            len(current),
            len(baseline),
            current_without_baseline,
            baseline_without_current,
        ),
        encoding="utf-8",
    )

    print(f"changed={str(changed).lower()}")
    print(f"regressed={str(regressed).lower()}")
    print(f"rows={len(rows)}")
    print(f"output={repo_relative(output)}")
    return 0


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_current_metrics(root: Path) -> dict[BenchmarkKey, tuple[Metrics, str]]:
    metrics: dict[BenchmarkKey, tuple[Metrics, str]] = {}
    for report in find_benchmark_reports(root):
        data = json.loads(report.read_text(encoding="utf-8"))
        for benchmark in data.get("Benchmarks", []):
            key = benchmark_key_from_json(benchmark)
            if key is None:
                continue

            benchmark_metrics = metrics_from_json(benchmark)
            if benchmark_metrics is None:
                continue

            parameter_display = build_parameter_display_from_json(benchmark)
            metrics[key] = (benchmark_metrics, parameter_display)

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


def metrics_from_json(benchmark: dict) -> Metrics | None:
    statistics = benchmark.get("Statistics") or {}
    memory = benchmark.get("Memory") or {}
    percentiles = statistics.get("Percentiles") or {}

    mean = to_float(statistics.get("Mean"))
    p95 = to_float(percentiles.get("P95", statistics.get("P95")))
    allocated = to_float(memory.get("BytesAllocatedPerOperation"))

    if not all(math.isfinite(value) for value in (mean, p95, allocated)):
        return None

    return Metrics(mean, p95, allocated)


def load_moonsharp_baseline(path: Path) -> dict[BenchmarkKey, Metrics]:
    if not path.exists():
        return {}

    baseline: dict[BenchmarkKey, Metrics] = {}
    current_summary = ""
    header: list[str] | None = None

    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.strip()
        if line.startswith("### "):
            current_summary = normalize_summary_name(line[4:])
            header = None
            continue

        if not current_summary.startswith("NovaSharp."):
            continue

        if not line.startswith("|"):
            continue

        if re.match(r"^\|\s*-+", line):
            continue

        cells = parse_table_cells(line)
        if header is None:
            header = cells
            continue

        if len(cells) != len(header):
            continue

        key_metrics = baseline_row_to_metrics(current_summary, header, cells)
        if key_metrics is None:
            continue

        key, metrics = key_metrics
        baseline.setdefault(key, metrics)

    return baseline


def baseline_row_to_metrics(
    current_summary: str, header: list[str], cells: list[str]
) -> tuple[BenchmarkKey, Metrics] | None:
    try:
        method_index = header.index("Method")
        mean_index = header.index("Mean")
        allocated_index = header.index("Allocated")
    except ValueError:
        return None

    method = normalize_method_name(cells[method_index])
    parameters = build_parameter_signature_from_cells(header, cells, method_index, mean_index)
    mean = parse_duration_to_ns(cells[mean_index])
    p95 = parse_duration_to_ns(cells[header.index("P95")]) if "P95" in header else mean
    allocated = parse_bytes(cells[allocated_index])

    if not all(math.isfinite(value) for value in (mean, p95, allocated)):
        return None

    return BenchmarkKey(current_summary, method, parameters), Metrics(mean, p95, allocated)


def build_delta_rows(
    current: dict[BenchmarkKey, tuple[Metrics, str]],
    baseline: dict[BenchmarkKey, Metrics],
) -> list[DeltaRow]:
    rows: list[DeltaRow] = []
    for key in sorted(current):
        if key not in baseline:
            continue

        current_metrics, parameter_display = current[key]
        rows.append(DeltaRow(key, parameter_display, current_metrics, baseline[key]))

    return rows


def row_changed(row: DeltaRow, tolerance: float) -> bool:
    return any(
        abs(value) >= tolerance
        for value in (
            fraction_delta(row.current.mean_ns, row.baseline.mean_ns),
            fraction_delta(row.current.p95_ns, row.baseline.p95_ns),
            fraction_delta(row.current.allocated_bytes, row.baseline.allocated_bytes),
        )
        if math.isfinite(value)
    )


def row_regressed(row: DeltaRow, threshold: float) -> bool:
    return any(
        value >= threshold
        for value in (
            fraction_delta(row.current.mean_ns, row.baseline.mean_ns),
            fraction_delta(row.current.p95_ns, row.baseline.p95_ns),
            fraction_delta(row.current.allocated_bytes, row.baseline.allocated_bytes),
        )
        if math.isfinite(value)
    )


def render_markdown(
    rows: list[DeltaRow],
    current_root: Path,
    baseline_doc: Path,
    current_count: int,
    baseline_count: int,
    current_without_baseline: list[BenchmarkKey],
    baseline_without_current: list[BenchmarkKey],
) -> str:
    lines = [
        "## MoonSharp Benchmark Deltas",
        "",
        "Lower values are better. Positive deltas mean NovaSharp is above the MoonSharp baseline for that metric.",
        "",
        f"- Current artifacts: `{repo_relative(current_root)}`",
        f"- Baseline: `{repo_relative(baseline_doc)}`",
        f"- Matched rows: {len(rows)} of {current_count} current rows and {baseline_count} baseline rows",
        f"- Current rows without MoonSharp baseline: {len(current_without_baseline)}",
        f"- MoonSharp baseline rows not present in current artifacts: {len(baseline_without_current)}",
        "",
    ]

    append_key_preview(lines, "Current rows without MoonSharp baseline", current_without_baseline)

    if not rows:
        lines.extend(
            [
                "No benchmark rows matched the checked-in MoonSharp baseline.",
                "",
            ]
        )
        return "\n".join(lines)

    lines.extend(
        [
            "| Summary | Method | Parameters | Current Mean | MoonSharp Mean | Mean Delta | Current P95 | MoonSharp P95 | P95 Delta | Current Alloc | MoonSharp Alloc | Alloc Delta |",
            "| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
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
                    f"{format_time_delta(row.mean_delta_ns)} ({format_percent(row.mean_delta_percent)})",
                    format_time(row.current.p95_ns),
                    format_time(row.baseline.p95_ns),
                    f"{format_time_delta(row.p95_delta_ns)} ({format_percent(row.p95_delta_percent)})",
                    format_bytes(row.current.allocated_bytes),
                    format_bytes(row.baseline.allocated_bytes),
                    f"{format_bytes_delta(row.allocation_delta_bytes)} ({format_percent(row.allocation_delta_percent)}) |",
                ]
            )
        )

    lines.append("")
    return "\n".join(lines)


def append_key_preview(lines: list[str], title: str, keys: list[BenchmarkKey]) -> None:
    if not keys:
        return

    lines.append(f"### {title}")
    lines.append("")
    for key in keys[:10]:
        lines.append(f"- {key.summary} / {key.method} / {key.parameters or '-'}")
    if len(keys) > 10:
        lines.append(f"- ... {len(keys) - 10} more")
    lines.append("")


def repo_relative(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def parse_table_cells(line: str) -> list[str]:
    return [normalize_cell(cell) for cell in line.strip().strip("|").split("|")]


def normalize_summary_name(value: str) -> str:
    normalized = normalize_cell(value)
    normalized = re.sub(r"-\d{8}-\d{6}$", "", normalized)
    if normalized.startswith("WallstopStudios."):
        normalized = normalized[len("WallstopStudios.") :]
    if normalized.startswith("MoonSharp"):
        normalized = "NovaSharp" + normalized[len("MoonSharp") :]
    return normalized


def normalize_method_name(value: str) -> str:
    normalized = normalize_cell(value)
    if normalized.startswith("MoonSharp"):
        normalized = "NovaSharp" + normalized[len("MoonSharp") :]
    return normalized


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


def build_parameter_signature_from_cells(
    header: list[str], cells: list[str], method_index: int, mean_index: int
) -> str:
    parts: list[str] = []
    for index in range(method_index + 1, mean_index):
        name = normalize_parameter_name(header[index])
        value = normalize_cell(cells[index])
        if name and value:
            parts.append(f"{name}:{value}")
    return "|".join(sorted(parts))


def build_parameter_display_from_json(benchmark: dict) -> str:
    signature = build_parameter_signature_from_text(benchmark.get("Parameters", ""))
    if not signature:
        return ""
    return ", ".join(part.replace(":", "=", 1) for part in signature.split("|"))


def parse_duration_to_ns(value: str) -> float:
    normalized = normalize_cell(value).replace(",", "")
    if normalized in {"", "-"}:
        return math.nan
    match = re.match(r"^([-+]?\d*\.?\d+)\s*([a-zA-Zµμ]+)$", normalized)
    if not match:
        return math.nan
    magnitude = float(match.group(1))
    unit = match.group(2)
    return {
        "ns": magnitude,
        "us": magnitude * 1_000,
        "µs": magnitude * 1_000,
        "μs": magnitude * 1_000,
        "ms": magnitude * 1_000_000,
        "s": magnitude * 1_000_000_000,
    }.get(unit, math.nan)


def parse_bytes(value: str) -> float:
    normalized = normalize_cell(value).replace(",", "")
    if normalized in {"", "-"}:
        return 0.0
    match = re.match(r"^([-+]?\d*\.?\d+)\s*([A-Za-z]+)?$", normalized)
    if not match:
        return math.nan
    magnitude = float(match.group(1))
    unit = match.group(2) or "B"
    return {
        "B": magnitude,
        "KB": magnitude * 1024,
        "MB": magnitude * 1024 * 1024,
        "GB": magnitude * 1024 * 1024 * 1024,
    }.get(unit, magnitude)


def to_float(value: object) -> float:
    try:
        return float(value)
    except (TypeError, ValueError):
        return math.nan


def fraction_delta(current: float, baseline: float) -> float:
    if not math.isfinite(current) or not math.isfinite(baseline) or baseline <= 0:
        return math.nan
    return (current - baseline) / baseline


def percentage_delta(current: float, baseline: float) -> float:
    value = fraction_delta(current, baseline)
    return value * 100 if math.isfinite(value) else math.nan


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
    if not math.isfinite(value):
        return "-"
    return f"{value:+.2f}%"


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

#!/usr/bin/env python3
"""Measure reference lua CLI wall-time for exported comparison scenarios."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SCENARIO_DIR = Path("artifacts/benchmarkdotnet/lua-cli-scenarios")
DEFAULT_OUTPUT_ROOT = Path("artifacts/benchmarkdotnet/comparison")
DEFAULT_WARMUP_COUNT = 2
DEFAULT_ITERATION_COUNT = 10
DEFAULT_TIMEOUT_SECONDS = 30.0
REPORT_NAME = (
    "WallstopStudios.NovaSharp.Comparison.ReferenceLuaCli-report-full-compressed.json"
)


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--scenario-dir",
        type=Path,
        default=DEFAULT_SCENARIO_DIR,
        help="Directory containing exported comparison .lua scenarios.",
    )
    parser.add_argument(
        "--output-root",
        type=Path,
        default=DEFAULT_OUTPUT_ROOT,
        help="Root where the synthetic BenchmarkDotNet-shaped JSON report is written.",
    )
    parser.add_argument(
        "--lua-cmd",
        default=os.environ.get("LUA_CMD", ""),
        help="Reference lua executable. Defaults to LUA_CMD, lua5.4, lua54, then lua.",
    )
    parser.add_argument(
        "--warmup-count",
        type=int,
        default=DEFAULT_WARMUP_COUNT,
        help="Unmeasured process executions per scenario.",
    )
    parser.add_argument(
        "--iteration-count",
        type=int,
        default=DEFAULT_ITERATION_COUNT,
        help="Measured process executions per scenario.",
    )
    parser.add_argument(
        "--timeout-seconds",
        type=float,
        default=DEFAULT_TIMEOUT_SECONDS,
        help="Timeout for one lua CLI process execution.",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    scenario_dir = resolve_repo_path(args.scenario_dir)
    output_root = resolve_repo_path(args.output_root)
    lua_cmd = resolve_lua_command(args.lua_cmd)

    if lua_cmd is None:
        print("lua_cli_skipped=true")
        print("lua_cli_rows=0")
        print("lua_cli_reason=reference lua executable not found")
        return 0

    lua_version = read_lua_version(lua_cmd)
    scenarios = sorted(scenario_dir.glob("*.lua")) if scenario_dir.exists() else []
    if not scenarios:
        print("lua_cli_skipped=true")
        print("lua_cli_rows=0")
        print(f"lua_cli_reason=no scenarios found under {repo_relative(scenario_dir)}")
        return 0

    if args.warmup_count < 0:
        raise ValueError("--warmup-count must be non-negative.")
    if args.iteration_count <= 0:
        raise ValueError("--iteration-count must be positive.")
    if args.timeout_seconds <= 0:
        raise ValueError("--timeout-seconds must be positive.")

    benchmarks = []
    for scenario in scenarios:
        timings = measure_scenario(
            lua_cmd,
            scenario,
            args.warmup_count,
            args.iteration_count,
            args.timeout_seconds,
        )
        benchmarks.append(benchmark_record(scenario.stem, timings, lua_cmd, lua_version))

    output_path = output_root / "results" / REPORT_NAME
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(
            {
                "Title": "Reference lua CLI context",
                "Benchmarks": benchmarks,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print("lua_cli_skipped=false")
    print(f"lua_cli_rows={len(benchmarks)}")
    print(f"lua_cli_command={lua_cmd}")
    print(f"lua_cli_version={lua_version}")
    print(f"lua_cli_output={repo_relative(output_path)}")
    return 0


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def resolve_lua_command(lua_cmd: str) -> str | None:
    if lua_cmd:
        return resolve_command(lua_cmd)

    for candidate in ("lua5.4", "lua54", "lua"):
        resolved = resolve_command(candidate)
        if resolved is not None:
            return resolved

    return None


def resolve_command(command: str) -> str | None:
    normalized = os.path.expanduser(command)
    has_path_separator = os.sep in normalized or (os.altsep is not None and os.altsep in normalized)
    if Path(normalized).is_absolute() or has_path_separator:
        path = Path(normalized)
        if path.is_file() and os.access(path, os.X_OK):
            return str(path)
        return None

    return shutil.which(normalized)


def read_lua_version(lua_cmd: str) -> str:
    result = subprocess.run(
        [lua_cmd, "-v"],
        cwd=ROOT,
        text=True,
        capture_output=True,
        env=sanitized_lua_environment(),
        timeout=5,
        check=False,
    )
    text = " ".join(part.strip() for part in (result.stdout, result.stderr) if part.strip())
    return text or "unknown version"


def measure_scenario(
    lua_cmd: str,
    scenario: Path,
    warmup_count: int,
    iteration_count: int,
    timeout_seconds: float,
) -> list[int]:
    for _ in range(warmup_count):
        run_lua(lua_cmd, scenario, timeout_seconds)

    timings = []
    for _ in range(iteration_count):
        start = time.perf_counter_ns()
        run_lua(lua_cmd, scenario, timeout_seconds)
        timings.append(time.perf_counter_ns() - start)

    return timings


def run_lua(lua_cmd: str, scenario: Path, timeout_seconds: float) -> None:
    result = subprocess.run(
        [lua_cmd, str(scenario)],
        cwd=ROOT,
        env=sanitized_lua_environment(),
        text=True,
        capture_output=True,
        timeout=timeout_seconds,
        check=False,
    )
    if result.returncode == 0:
        return

    message = [
        f"lua CLI failed for {repo_relative(scenario)} with exit code {result.returncode}.",
    ]
    if result.stdout:
        message.append("stdout:")
        message.append(result.stdout.rstrip())
    if result.stderr:
        message.append("stderr:")
        message.append(result.stderr.rstrip())
    raise RuntimeError("\n".join(message))


def sanitized_lua_environment() -> dict[str, str]:
    env = os.environ.copy()
    for key in (
        "LUA_INIT",
        "LUA_INIT_5_1",
        "LUA_INIT_5_2",
        "LUA_INIT_5_3",
        "LUA_INIT_5_4",
        "LUA_INIT_5_5",
    ):
        env.pop(key, None)
    return env


def benchmark_record(
    scenario_name: str,
    timings: list[int],
    lua_cmd: str,
    lua_version: str,
) -> dict:
    mean = sum(timings) / len(timings)
    p95 = percentile(timings, 95)
    return {
        "Namespace": "WallstopStudios.NovaSharp.Comparison",
        "Type": "LuaPerformanceBenchmarks",
        "Method": "LuaExecute",
        "MethodTitle": "'Lua Execute'",
        "Parameters": f"ScenarioName={scenario_name}",
        "RuntimeDisplayName": "Lua CLI wall-time",
        "RuntimeContext": f"{lua_cmd}: {lua_version}",
        "ShowDeltaPercent": False,
        "Statistics": {
            "Mean": mean,
            "Percentiles": {
                "P95": p95,
            },
        },
    }


def percentile(values: list[int], percentile_value: float) -> float:
    ordered = sorted(values)
    if len(ordered) == 1:
        return float(ordered[0])

    rank = (len(ordered) - 1) * percentile_value / 100
    lower_index = int(rank)
    upper_index = min(lower_index + 1, len(ordered) - 1)
    fraction = rank - lower_index
    lower = ordered[lower_index]
    upper = ordered[upper_index]
    return lower + (upper - lower) * fraction


def repo_relative(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

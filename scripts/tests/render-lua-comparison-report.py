#!/usr/bin/env python3
"""Render a PR-friendly summary from Lua comparison JSON artifacts."""

from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_INPUT_ROOT = Path("artifacts")
DEFAULT_OUTPUT = Path("artifacts/lua-comparison-report.md")


@dataclass(frozen=True)
class ComparisonRow:
    lua_version: str
    os_name: str
    summary: dict
    match_rate: float | None
    ratchet: dict
    elapsed_seconds: object
    workers: object

    @property
    def has_unexpected_delta(self) -> bool:
        return any(
            int(self.summary.get(name, 0) or 0) > 0
            for name in ("mismatch", "lua_only", "nova_only", "missing_outputs")
        ) or any(
            int(self.ratchet.get(name, 0) or 0) > 0
            for name in ("new_count", "changed_count", "missing_count")
        )


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--input-root",
        type=Path,
        default=DEFAULT_INPUT_ROOT,
        help="Root containing downloaded lua-comparison-* artifacts.",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help="Markdown output path.",
    )
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    input_root = resolve_repo_path(args.input_root)
    output = resolve_repo_path(args.output)

    rows = load_rows(input_root)
    changed = any(row.has_unexpected_delta for row in rows)
    regressed = changed

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(render_markdown(rows, input_root), encoding="utf-8")

    print(f"changed={str(changed).lower()}")
    print(f"regressed={str(regressed).lower()}")
    print(f"rows={len(rows)}")
    print(f"output={repo_relative(output)}")
    return 0


def resolve_repo_path(path: Path) -> Path:
    return path if path.is_absolute() else ROOT / path


def load_rows(root: Path) -> list[ComparisonRow]:
    rows: list[ComparisonRow] = []
    if not root.exists():
        return rows

    for comparison_file in sorted(root.rglob("comparison-*.json")):
        data = json.loads(comparison_file.read_text(encoding="utf-8"))
        results_file = comparison_file.parent / "results.json"
        run_summary: dict = {}
        if results_file.exists():
            results_data = json.loads(results_file.read_text(encoding="utf-8"))
            run_summary = results_data.get("summary", {})

        rows.append(
            ComparisonRow(
                lua_version=str(data.get("lua_version") or infer_lua_version(comparison_file)),
                os_name=infer_os_name(comparison_file),
                summary=data.get("summary", {}),
                match_rate=to_optional_float(data.get("match_rate")),
                ratchet=data.get("error_ratchet", {}),
                elapsed_seconds=run_summary.get("elapsed_seconds", "n/a"),
                workers=run_summary.get("workers", "n/a"),
            )
        )

    return sorted(rows, key=lambda row: (version_sort_key(row.lua_version), row.os_name))


def render_markdown(rows: list[ComparisonRow], input_root: Path) -> str:
    lines = [
        "## Lua Comparison Report",
        "",
        f"Input artifacts: `{repo_relative(input_root)}`",
        "",
    ]

    if not rows:
        lines.extend(["No Lua comparison artifacts were found.", ""])
        return "\n".join(lines)

    lines.extend(
        [
            "| OS | Lua | Match | Mismatch | Lua only | Nova only | Both error | Missing | Skipped | Ratchet new | Ratchet changed | Ratchet missing | Match rate | Runtime |",
            "| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
        ]
    )

    for row in rows:
        summary = row.summary
        ratchet = row.ratchet
        lines.append(
            " | ".join(
                [
                    f"| {row.os_name}",
                    row.lua_version,
                    str(summary.get("match", 0)),
                    str(summary.get("mismatch", 0)),
                    str(summary.get("lua_only", 0)),
                    str(summary.get("nova_only", 0)),
                    str(summary.get("both_error", 0)),
                    str(summary.get("missing_outputs", 0)),
                    str(summary.get("skipped", 0)),
                    str(ratchet.get("new_count", 0)),
                    str(ratchet.get("changed_count", 0)),
                    str(ratchet.get("missing_count", 0)),
                    format_match_rate(row.match_rate),
                    f"{row.elapsed_seconds} s |",
                ]
            )
        )

    lines.extend(
        [
            "",
            "Unexpected deltas are mismatches, one-sided outputs, missing outputs, or new/changed/missing both-error ratchet entries.",
            "",
        ]
    )
    return "\n".join(lines)


def infer_os_name(path: Path) -> str:
    for part in reversed(path.parts):
        match = re.match(r"lua-comparison-(\d+\.\d+)-(.+)", part)
        if match:
            return match.group(2)
        if part.startswith("lua-comparison-local-"):
            return "local"
    return "unknown"


def version_sort_key(value: str) -> tuple[int, ...]:
    parts: list[int] = []
    for part in value.split("."):
        try:
            parts.append(int(part))
        except ValueError:
            parts.append(0)
    return tuple(parts)


def infer_lua_version(path: Path) -> str:
    match = re.search(r"comparison-(\d+\.\d+)\.json$", path.name)
    return match.group(1) if match else "unknown"


def to_optional_float(value: object) -> float | None:
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def format_match_rate(value: float | None) -> str:
    return "n/a" if value is None else f"{value:.1f}%"


def repo_relative(path: Path) -> str:
    try:
        return path.relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

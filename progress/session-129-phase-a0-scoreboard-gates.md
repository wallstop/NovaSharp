# Session 129: Phase A0 Scoreboard Gates

Date: 2026-07-03

## Summary

- Extended `scripts/benchmarks/render-benchmark-deltas.py` with a compact Phase A0 scoreboard section:
  - rows are comparison scenario/operation pairs;
  - columns include NovaSharp current, NovaSharp baseline, MoonSharp, NLua, Lua-CSharp, and reference `lua` CLI wall-time context;
  - memory/GC output is rendered separately so allocation data is visible without overstating reference `lua` CLI diagnostics.
- Added normalized Phase A0 baseline support:
  - `--phase-baseline` reads the checked-in baseline path;
  - `--write-phase-baseline` writes representative comparison metrics to JSON;
  - default baseline path is `progress/benchmarks/phase-a0-scoreboard-baseline.json`.
- Added opt-in Phase A0 gate enforcement:
  - NovaSharp/NLua mean and P95 ratio drift uses the same-run ratio with a 10% default tolerance;
  - NovaSharp allocated B/op is checked exactly against the phase baseline;
  - reference `lua` CLI rows remain wall-time-only context and are excluded from allocation gates.
- Added dedicated local commands:
  - `scripts/benchmarks/run-phase-a0-scoreboard.sh`;
  - `scripts/benchmarks/run-phase-a0-scoreboard.ps1`.
- Wired benchmark CI to pass the phase baseline path, post/report phase gate failures, and activate enforcement automatically once the canonical baseline JSON exists.
- Updated benchmark documentation and `PLAN.md`.

## Validation

- `python3 tools/test_render_benchmark_deltas.py` completed with 13 tests passing.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py tools/test_render_benchmark_deltas.py` completed successfully.
- `bash -n scripts/benchmarks/run-benchmarks.sh scripts/benchmarks/run-phase-a0-scoreboard.sh` completed successfully.
- `pwsh -NoProfile -Command '[scriptblock]::Create((Get-Content -Raw scripts/benchmarks/run-phase-a0-scoreboard.ps1)) | Out-Null; [scriptblock]::Create((Get-Content -Raw scripts/benchmarks/run-benchmarks.ps1)) | Out-Null'` completed successfully.
- `git diff --check` completed successfully.

## Reviewer Feedback

- Cursor Bugbot reported that the benchmark workflow warning for unavailable Phase A0 gates inferred missing baseline state from `phase_baseline_rows == 0`.
- The workflow now carries an explicit `phase_baseline_exists` output from the render step, uses that for the missing-baseline warning, and separately warns when a committed baseline file loads zero usable rows.
- Follow-up focused validation completed successfully:
  - `python3 tools/test_render_benchmark_deltas.py`;
  - `artifacts/actionlint/actionlint .github/workflows/benchmarks.yml`;
  - `python3 scripts/lint/check-shell-python-invocation.py`;
  - `bash -n scripts/benchmarks/run-phase-a0-scoreboard.sh scripts/benchmarks/run-benchmarks.sh`;
  - `git diff --check`.
- GitHub Copilot reported that the Phase A0 scoreboard text promised a single `-` for reference `lua` CLI memory cells while the formatter rendered `- / - / - / -`.
- The renderer now preserves runtime kind through the Phase A0 matrix helpers and collapses `LuaCliWallTime` memory cells to a single `-`; `tools/test_render_benchmark_deltas.py` covers the documented row shape.
- Cursor Bugbot reported that the Phase A0 gate failure table used raw parameter display for the `Scenario` column, which could render `-` and make failures hard to match to the scoreboard.
- The failure preview now uses the scoreboard scenario formatter and falls back to the comparison summary when no scenario parameter exists.
- GitHub Copilot reported that local Phase A0 runners could read stale `BenchmarkDotNet.Artifacts` rows through `--current-root`; both Bash and PowerShell runners now point current and comparison roots at the freshly cleaned Phase A0 artifact directory.
- GitHub Copilot also reported that benchmark CI always emitted `has_report=true`; the workflow now bases `has_report` on whether `artifacts/benchmark-deltas.md` exists while still failing on any non-zero renderer status.
- Cursor Bugbot reported that a missing `phase_baseline_rows` counter could be mislabeled as an empty baseline. The workflow now emits `phase_baseline_rows_available` and only uses the empty-baseline warning when the renderer actually produced the row count.

## Remaining Work

- Generate the canonical Phase A0 baseline from a representative full scoreboard run and commit `progress/benchmarks/phase-a0-scoreboard-baseline.json`.
- Run the benchmark workflow with the baseline committed and observe the ratio/allocation gates passing in CI.
- Add the minimal Unity IL2CPP stopwatch spot-check scene and local/CI instructions.

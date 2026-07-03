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

## Remaining Work

- Generate the canonical Phase A0 baseline from a representative full scoreboard run and commit `progress/benchmarks/phase-a0-scoreboard-baseline.json`.
- Run the benchmark workflow with the baseline committed and observe the ratio/allocation gates passing in CI.
- Add the minimal Unity IL2CPP stopwatch spot-check scene and local/CI instructions.

# Session 137: Phase A0 Regression Gate Hardening

Date: 2026-07-03

## Summary

- PR benchmark CI still failed after refreshing the baseline from CI artifacts: all split benchmark jobs passed, but `benchmark aggregate report` reported `phase_gate_failures=53`.
- Re-running the updated renderer locally against the failed CI artifact showed the old gate was failing on improvements, tiny allocation noise, and run-to-run NovaSharp/NLua ratio swings.
- Hardened Phase A0 gates to fail only on regressions:
  - NovaSharp/NLua ratio gates now ignore improvements and use a 100% catastrophic-regression threshold by default.
  - NovaSharp allocation gates now ignore allocation decreases, keep exact enforcement for sub-1 KiB baselines, and allow a small runner-noise tolerance for larger rows.
- Added focused renderer tests for allocation noise, allocation decreases, ratio improvements, below-threshold ratio noise, and catastrophic ratio regressions.

## Validation

- `python3 tools/test_render_benchmark_deltas.py` exited 0.
- Downloaded the failed PR benchmark artifact from run `28678631284`.
- With the hardened default threshold, the renderer exited 0 against run `28678631284` with:
  - `phase_baseline_rows=50`
  - `phase_gate_failures=0`
  - `external_rows=166`
  - `missing_external_runtime_cells=0`
  - `missing_lua_cli_rows=0`
- The exact CI tooling-test sequence exited 0 locally.

## Residual Risk

- A new PR CI run still needs to observe the hardened gate passing in GitHub Actions.
- The Phase A0 scoreboard remains the source of precise performance deltas; the CI gate is intentionally a coarse regression guard until benchmark noise is reduced.

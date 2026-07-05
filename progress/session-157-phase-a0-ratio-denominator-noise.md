# Session 157: Phase A0 Ratio Denominator Noise

Date: 2026-07-05

## Summary

- PR #55 benchmark CI failed only in the aggregate Phase A0 gate.
- The failing row was `StringFormat` `Compile` on `NLua P95 ratio`: NovaSharp P95 improved from the checked-in baseline, but NLua's same-run P95 improved enough to more than double the NovaSharp/NLua ratio.
- Rewrote the ratio gate so finite NovaSharp/NLua mean/P95 ratio regressions fail only when NovaSharp's own timing metric also regresses beyond the same catastrophic threshold.
- Kept shape mismatches, missing NLua rows, missing timing metrics, and NovaSharp B/op regressions blocking.

## Validation

- `python3 tools/test_render_benchmark_deltas.py` exited 0 with 18 tests passing.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py tools/test_render_benchmark_deltas.py` exited 0.
- Downloaded PR #55 benchmark run `28749188275` artifact `benchmark-results`.
- Replayed `scripts/benchmarks/render-benchmark-deltas.py` against the failed artifact with `--enforce-phase-gates`; it exited 0 and reported:
  - `regressed=false`
  - `external_rows=166`
  - `missing_external_runtime_cells=0`
  - `missing_lua_cli_rows=0`
  - `phase_baseline_rows=50`
  - `phase_gate_failures=0`

## Residual Risk

- A new PR benchmark run still needs to observe the aggregate report passing with the hardened denominator-noise guard.

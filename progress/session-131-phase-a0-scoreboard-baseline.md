# Session 131: Phase A0 Scoreboard Baseline

Date: 2026-07-03

## Summary

- Ran the canonical full Phase A0 scoreboard command and wrote the representative baseline JSON to `progress/benchmarks/phase-a0-scoreboard-baseline.json`.
- The run executed the managed comparison suite, exported reference Lua CLI scenarios, measured the reference `lua5.4` context, and rendered `artifacts/phase-a0-scoreboard.md`.
- The renderer reported `lua_cli_skipped=false`, `lua_cli_rows=16`, `external_rows=166`, `missing_external_runtime_cells=0`, `missing_lua_cli_rows=0`, `phase_baseline_rows=50`, and `phase_gate_failures=0`.
- The benchmark command used `lua5.4`, reporting Lua 5.4.4 for the reference CLI context.
- The canonical baseline path was added to the branding allowlists because the JSON intentionally records third-party comparison runtime names.
- The A0 plan item remains open until this baseline is committed and PR CI observes the phase gates against it.

## Validation

- `./scripts/benchmarks/run-phase-a0-scoreboard.sh --write-phase-baseline progress/benchmarks/phase-a0-scoreboard-baseline.json` exited 0.
- `python3 -m json.tool progress/benchmarks/phase-a0-scoreboard-baseline.json` exited 0.
- `wc -c progress/benchmarks/phase-a0-scoreboard-baseline.json artifacts/phase-a0-scoreboard.md` reported 94,387 bytes for the baseline JSON and 51,657 bytes for the rendered scoreboard.
- `python3 scripts/benchmarks/render-benchmark-deltas.py --current-root artifacts/benchmarkdotnet/phase-a0-comparison --comparison-root artifacts/benchmarkdotnet/phase-a0-comparison --phase-baseline progress/benchmarks/phase-a0-scoreboard-baseline.json --output artifacts/phase-a0-scoreboard-enforced.md --expect-lua-cli --enforce-phase-gates` exited 0 and reported `phase_gate_failures=0`.

## Residual Risk

- The benchmark was a local run on the available Linux environment. It is representative enough to seed the baseline, but the PR CI benchmark gates still need to run against the committed JSON before the A0 baseline item can be treated as closed.

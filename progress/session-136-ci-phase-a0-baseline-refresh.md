# Session 136: CI Phase A0 Baseline Refresh

Date: 2026-07-03

## Summary

- PR benchmark CI failed in `benchmark aggregate report` after all split benchmark jobs succeeded.
- The aggregate renderer reported `phase_baseline_rows=50` and `phase_gate_failures=101`, showing the locally generated Phase A0 baseline was not representative of the GitHub Actions runner.
- Downloaded the `benchmark-results` artifact from PR benchmark run `28678100658` and refreshed `progress/benchmarks/phase-a0-scoreboard-baseline.json` from the CI `comparison-download` artifacts.
- Updated benchmark docs to state that checked-in Phase A0 baselines should come from GitHub Actions benchmark artifacts or an intentionally CI-matched runner because the gates include exact allocation checks.

## Validation

- `gh run download 28678100658 -n benchmark-results -D artifacts/ci-benchmark-run-28678100658` downloaded the aggregate benchmark artifact.
- `python3 -m json.tool progress/benchmarks/phase-a0-scoreboard-baseline.json` exited 0 after the refresh.
- Refreshed and enforced the baseline against the downloaded CI artifacts with:
  - `phase_baseline_rows=50`
  - `phase_gate_failures=0`
  - `external_rows=166`
  - `missing_external_runtime_cells=0`
  - `missing_lua_cli_rows=0`

## Residual Risk

- A new PR CI run still needs to observe the refreshed checked-in baseline passing in GitHub Actions.
- Local Phase A0 benchmark enforcement may differ from the CI baseline unless run on a comparable environment; use local runs for exploration and CI artifacts for checked-in baseline refreshes.

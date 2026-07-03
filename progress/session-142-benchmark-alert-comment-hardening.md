# Session 142: Benchmark Alert Comment Hardening

Date: 2026-07-03

## Summary

- PR #49 CI completed successfully for `cb2bce50`, but `benchmark-action/github-action-benchmark` still posted a non-blocking PR review alert for `TableMutation` based on gh-pages history.
- The head commit only changed packaging diagnostics and Unity IL2CPP sample logging, so the alert was not attributable to runtime or benchmark code.
- The aggregate benchmark report already produced the robust PR-facing signal: same-run external deltas, Phase A0 baseline diagnostics, and enforced Phase A0 gates.
- Left historical benchmark storage enabled, but disabled historical-action alert comments on pull requests so PR benchmark feedback comes from the aggregate delta comment and Phase A0 gates instead of noisy gh-pages comparisons.
- Normalized the manual benchmark alert threshold input through an environment variable so both `115` and `115%` reach `github-action-benchmark` as the required percent value without interpolating dispatch input into the shell script body.
- Updated benchmark docs to describe the split between historical storage and PR-facing benchmark feedback.

## Validation

- `python3 -m yamllint -c .yamllint.yml .github/workflows/benchmarks.yml` exited 0.
- `git diff --check` exited 0.
- `bash ./scripts/dev/pre-commit.sh` exited 0; staged workflow validation ran through actionlint, with only the existing LLM skill metadata warnings.
- PR CI and reviewer status will be checked on the pushed head.

## Residual Risk

- Hosted-runner microbenchmark means remain noisy; the Phase A0 gate intentionally remains coarse for speed and exact/noise-tolerant for allocated B/op until a less noisy benchmark methodology is available.

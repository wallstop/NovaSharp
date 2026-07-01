# Session 122: Runtime Matrix Deltas

## Scope

- Rework the benchmark delta report so each scenario/operation shows NovaSharp and every same-run implementer side-by-side.
- Keep external runtime comparisons report-only while preserving the self-baseline regression signal.
- Make NovaSharp raw results readable in the PR comment instead of showing only pairwise external deltas.

## Changes

- Replaced the same-run external comparison table with a runtime matrix grouped by scenario and operation.
- Added a time matrix with NovaSharp mean/P95 first, followed by each external runtime mean/P95 and NovaSharp-vs-runtime deltas.
- Added a memory and GC matrix with NovaSharp allocation and GC counts first, followed by each external runtime allocation/GC counts and NovaSharp-vs-runtime deltas.
- Kept the `external_rows` workflow output stable for CI consumers while renaming the markdown summary to external runtime cells.
- Corrected allocation delta percentages in rendered benchmark output so byte deltas use percentage points rather than fractional values.

## Validation

- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py tools/test_render_benchmark_deltas.py`
- `python3 -m unittest tools/test_render_benchmark_deltas.py`
- Workflow YAML parser validation for `.github/workflows/benchmarks.yml`
- `./scripts/dev/pre-commit.sh`

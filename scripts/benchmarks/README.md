# Benchmarks

Use this folder to regenerate the NovaSharp benchmark baselines documented in `docs/Performance.md`. The helper scripts automate the workflow described in the governance section (tool restore → solution build → runtime benchmarks → comparison benchmarks) so contributors run a single command before updating the docs.

## CI Integration

The `.github/workflows/benchmarks.yml` workflow runs benchmarks automatically:

- **On push to main**: Full benchmark run with results stored in `gh-pages` branch
- **On PRs**: Benchmark run plus same-run external comparisons; comments on PRs with comparison alerts and a GC-aware delta table
- **Manual dispatch**: Configurable threshold and fail-on-alert settings

### Regression Detection

The workflow uses `benchmark-action/github-action-benchmark` to track performance over time:

- **Alert threshold**: 115% by default (15% regression triggers alert)
- **Fail on alert**: Enabled for manual benchmark gates; PR alerts are advisory because hosted-runner microbenchmarks are noisy
- **PR comments**: Automatic comments when regressions are detected
- **External runtime deltas**: `scripts/benchmarks/render-benchmark-deltas.py` renders a sticky PR comment and `artifacts/benchmark-deltas.md`; positive deltas mean NovaSharp is slower, collects more GC, or allocates more than the same-run comparison runtime row
- **Self deltas**: the same renderer can compare current NovaSharp results to checked-in BenchmarkDotNet JSON artifacts under `docs/performance-history/current-baseline` once that baseline exists
- **Historical tracking**: Results stored in `gh-pages` branch under `/benchmarks`

## `run-benchmarks.ps1` (PowerShell)

Runs both the NovaSharp runtime benchmarks and the external comparison suite that exercises MoonSharp and NLua on the same machine. The script restores local tools, builds `src/NovaSharp.sln`, executes the BenchmarkDotNet harnesses, and reminds you where BenchmarkDotNet artifacts land.

### Prerequisites

- PowerShell 7 (`pwsh`)
- The .NET SDK pinned by `global.json` available on `PATH`
- Local .NET tools are restored automatically by the script

### Usage

```powershell
pwsh ./scripts/benchmarks/run-benchmarks.ps1
```

Optional parameters:

- `-Configuration <value>`: Build/benchmark configuration (defaults to `Release`).
- `-SkipComparison`: Run only the NovaSharp runtime benchmarks (skips the external comparison suite).

After the BenchmarkDotNet runs finish, the script renders `artifacts/benchmark-deltas.md` from the current runtime artifacts, the same-run comparison artifacts, and the optional checked-in self baseline under `docs/performance-history/current-baseline`.

## `run-benchmarks.sh` (Bash)

Equivalent bash script for Linux/macOS environments and CI runners.

### Prerequisites

- Bash 4.0+
- The .NET SDK pinned by `global.json` available on `PATH`
- Local .NET tools are restored automatically by the script

### Usage

```bash
./scripts/benchmarks/run-benchmarks.sh
```

Optional parameters:

- `--configuration <value>` or `-c <value>`: Build/benchmark configuration (defaults to `Release`).
- `--skip-comparison`: Run only the NovaSharp runtime benchmarks (skips the external comparison suite).

After the BenchmarkDotNet runs finish, the script renders `artifacts/benchmark-deltas.md` from the current runtime artifacts, the same-run comparison artifacts, and the optional checked-in self baseline under `docs/performance-history/current-baseline`.

## `render-benchmark-deltas.py`

Renders the same comparison delta report used by CI without rerunning benchmarks.

```bash
python3 scripts/benchmarks/render-benchmark-deltas.py \
  --current-root BenchmarkDotNet.Artifacts \
  --comparison-root artifacts/benchmarkdotnet/comparison \
  --self-baseline-root docs/performance-history/current-baseline \
  --output artifacts/benchmark-deltas.md
```

The script writes `changed=true|false`, `regressed=true|false`, external/self row counts, and the output path to stdout so GitHub Actions and local tooling can consume it consistently.

## Output

- BenchmarkDotNet artifacts land under `BenchmarkDotNet.Artifacts/` (git-ignored).
- `docs/Performance.md` is updated automatically by the benchmark harnesses; review the diff before committing.
- `artifacts/benchmarkdotnet/comparison/` contains same-run comparison artifacts for MoonSharp, NLua, and future external runtimes.
- `artifacts/benchmark-deltas.md` compares current NovaSharp artifacts to same-run external runtime rows and, when present, checked-in self baseline artifacts.
- The scripts print paths to the BenchmarkDotNet artifact folders so you can attach them to PRs when results change.
- CI workflow uploads artifacts as `benchmark-results` for inspection.

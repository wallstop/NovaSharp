# Benchmarks

Use this folder to regenerate the NovaSharp benchmark baselines documented in `docs/Performance.md`. The helper scripts automate the workflow described in the governance section (tool restore → solution build → runtime benchmarks → comparison benchmarks) so contributors run a single command before updating the docs.

## CI Integration

The `.github/workflows/benchmarks.yml` workflow runs benchmarks automatically:

- **On push to main**: Full benchmark run with results stored in `gh-pages` branch
- **On PRs**: Benchmark run with comparison against baseline; comments on PRs with regression alerts
- **Manual dispatch**: Configurable threshold and fail-on-alert settings

### Regression Detection

The workflow uses `benchmark-action/github-action-benchmark` to track performance over time:

- **Alert threshold**: 115% by default (15% regression triggers alert)
- **Fail on alert**: Enabled for PRs, configurable for manual runs
- **PR comments**: Automatic comments when regressions are detected
- **Historical tracking**: Results stored in `gh-pages` branch under `/benchmarks`

## `run-benchmarks.ps1` (PowerShell)

Runs both the NovaSharp runtime benchmarks and the NLua comparison suite that exercises the legacy interpreter mirror. The script restores local tools, builds `src/NovaSharp.sln`, executes the BenchmarkDotNet harnesses, and reminds you where BenchmarkDotNet artifacts land.

### Prerequisites

- PowerShell 7 (`pwsh`)
- .NET SDK 8.0 (or later) available on `PATH`
- Run once per clone: `dotnet tool restore`

### Usage

```powershell
pwsh ./scripts/benchmarks/run-benchmarks.ps1
```

Optional parameters:

- `-Configuration <value>`: Build/benchmark configuration (defaults to `Release`).
- `-SkipComparison`: Run only the NovaSharp runtime benchmarks (skips the NLua comparison project).

## `run-benchmarks.sh` (Bash)

Equivalent bash script for Linux/macOS environments and CI runners.

### Prerequisites

- Bash 4.0+
- .NET SDK 8.0 (or later) available on `PATH`
- Run once per clone: `dotnet tool restore`

### Usage

```bash
./scripts/benchmarks/run-benchmarks.sh
```

Optional parameters:

- `--configuration <value>` or `-c <value>`: Build/benchmark configuration (defaults to `Release`).
- `--skip-comparison`: Run only the NovaSharp runtime benchmarks (skips the NLua comparison project).

## Output

- BenchmarkDotNet artifacts land under `BenchmarkDotNet.Artifacts/` (git-ignored).
- `docs/Performance.md` is updated automatically by the benchmark harnesses; review the diff before committing.
- The scripts print paths to the BenchmarkDotNet artifact folders so you can attach them to PRs when results change.
- CI workflow uploads artifacts as `benchmark-results` for inspection.

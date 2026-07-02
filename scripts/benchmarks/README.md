# Benchmarks

Use this folder to regenerate the NovaSharp benchmark baselines documented in `docs/Performance.md`. The helper scripts automate the workflow described in the governance section (tool restore → solution build → runtime benchmarks → comparison benchmarks) so contributors run a single command before updating the docs.

## CI Integration

The `.github/workflows/benchmarks.yml` workflow runs benchmarks automatically:

- **On push to main**: Full benchmark run with results stored in `gh-pages` branch
- **On PRs**: Benchmark run plus same-run external comparisons; comments on PRs with a GC-aware delta table
- **Manual dispatch**: Configurable threshold and fail-on-alert settings

### Regression Detection

The workflow uses `benchmark-action/github-action-benchmark` to track performance over time:

- **Alert threshold**: 115% by default (15% regression triggers alert)
- **Fail on alert**: Enabled for manual benchmark gates; PR alerts are advisory because hosted-runner microbenchmarks are noisy
- **PR comments**: Automatic comments when regressions are detected
- **External runtime deltas**: `scripts/benchmarks/render-benchmark-deltas.py` renders a sticky PR comment and `artifacts/benchmark-deltas.md`; each scenario/operation is shown as a matrix row with NovaSharp raw results first, then same-run external runtime results and NovaSharp-vs-runtime deltas. Positive deltas mean NovaSharp is slower, collects more GC, or allocates more than the same-run comparison runtime row. Reference `lua` CLI rows are out-of-process wall-time context only, so memory and GC cells are shown as unknown. These cells are report-only and do not set `regressed=true`.
- **Self deltas**: the same renderer can compare current NovaSharp results to checked-in BenchmarkDotNet JSON artifacts under `docs/performance-history/current-baseline` once that baseline exists. Self deltas drive the `regressed=true` signal.
- **Historical tracking**: Results stored in `gh-pages` branch under `/benchmarks`

## `run-benchmarks.ps1` (PowerShell)

Runs both the NovaSharp runtime benchmarks and the external comparison suite that exercises MoonSharp, NLua, Lua-CSharp, and optional reference `lua` CLI wall-time context on the same machine. The script restores local tools, builds `src/NovaSharp.sln`, executes the BenchmarkDotNet harnesses, and reminds you where BenchmarkDotNet artifacts land.

### Prerequisites

- PowerShell 7 (`pwsh`)
- The .NET SDK pinned by `global.json` available on `PATH`
- Local .NET tools are restored automatically by the script
- Optional: `lua5.4`, `lua54`, or `lua` on `PATH` for the reference CLI context column. Set `LUA_CMD` to override discovery.

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
- Optional: `lua5.4`, `lua54`, or `lua` on `PATH` for the reference CLI context column. Set `LUA_CMD` to override discovery.

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

The script writes `changed=true|false`, `regressed=true|false`, external/self row counts, missing expected external runtime cell counts, `missing_lua_cli_rows`, and the output path to stdout so GitHub Actions and local tooling can consume it consistently. The generated markdown groups same-run comparison output by scenario and operation so NovaSharp, MoonSharp, NLua, Lua-CSharp, optional reference `lua` CLI context, and future implementers are readable side-by-side. Rows marked `ShowDeltaPercent=false` or `RuntimeKind=LuaCliWallTime`, such as reference `lua` process wall-time context, render raw deltas but do not contribute to `changed=true`. CI passes `--expect-lua-cli` because the benchmark runner installs `lua5.4`; local runs can omit it when no reference executable is available.

The comparison suite's `Compile` rows create a fresh runtime state for each engine before loading the scenario. `Execute` rows use each engine's prepared public execution surface to reflect the host API NovaSharp is trying to compete with; add a separate normalized-result-read suite before treating return-materialization cost as isolated interpreter cost.

Current comparison scenarios cover pure-Lua compute (`fib(30)`, hanoi, n-body, binary-trees, spectral-norm), table-heavy work (integer fill/iterate, string-key lookup, `next` traversal, insert/remove churn), string-heavy work (concat chains, `gsub`/`find`, `string.format`), coroutine ping-pong, and the earlier numeric/table/backtracking smoke cases. Cross-runtime host interop rows are intentionally separate because each engine needs its own host binding path and reference `lua` cannot execute those rows.

`scripts/benchmarks/run-lua-cli-context.py` measures exported comparison scenarios by spawning the reference `lua` executable once per iteration and emits BenchmarkDotNet-shaped JSON under `artifacts/benchmarkdotnet/comparison/`. This is intentionally wall-clock process context, not a managed allocation measurement.

## Output

- BenchmarkDotNet artifacts land under `BenchmarkDotNet.Artifacts/` (git-ignored).
- `docs/Performance.md` is updated automatically by the benchmark harnesses; review the diff before committing.
- `artifacts/benchmarkdotnet/comparison/` contains same-run comparison artifacts for MoonSharp, NLua, Lua-CSharp, optional reference `lua` CLI context, and future external runtimes.
- `artifacts/benchmark-deltas.md` compares current NovaSharp artifacts to same-run external runtime matrix cells and, when present, checked-in self baseline artifacts.
- The scripts print paths to the BenchmarkDotNet artifact folders so you can attach them to PRs when results change.
- CI workflow uploads artifacts as `benchmark-results` for inspection.

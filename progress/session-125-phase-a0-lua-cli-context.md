# Session 125: Phase A0 Reference Lua CLI Context

Date: 2026-07-02

## Summary

- Added an export mode to the comparison benchmark executable so the existing `BenchmarkScripts` scenarios are the single source for external process runners.
- Added `scripts/benchmarks/run-lua-cli-context.py` to measure exported scenarios with a reference `lua` executable and emit BenchmarkDotNet-shaped JSON under the comparison artifact tree.
- Wired the bash, PowerShell, and benchmark CI flows to generate reference `lua` wall-time context after the in-process comparison benchmarks.
- Installed `lua5.4` in benchmark CI so PR delta comments can include the reference CLI context column on the hosted runner.
- Added a CI-facing `missing_lua_cli_rows` renderer signal so the reference CLI context cannot disappear silently when benchmark comparison artifacts are otherwise present.
- Recorded the actual reference `lua` command and `lua -v` output in the synthetic JSON and rendered runtime context.
- Updated the benchmark delta renderer to show missing memory/GC diagnostics as `-` instead of treating time-only rows as zero-allocation rows.
- Extended renderer tests to cover reference `lua` CLI time context and unknown memory/GC cells.
- Addressed Copilot review feedback by resolving and validating `--lua-cmd`/`LUA_CMD` before subprocess use, so missing executables skip cleanly and successful runs record the actual executable path.

## Validation

- `python3 tools/test_render_benchmark_deltas.py` passed.
- `python3 tools/test_run_lua_cli_context.py` passed.
- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py scripts/benchmarks/run-lua-cli-context.py tools/test_render_benchmark_deltas.py` passed.
- `python3 -m py_compile scripts/benchmarks/run-lua-cli-context.py tools/test_run_lua_cli_context.py` passed.
- `bash -n scripts/benchmarks/run-benchmarks.sh` passed.
- `pwsh -NoProfile -Command '[System.Management.Automation.Language.Parser]::ParseFile("scripts/benchmarks/run-benchmarks.ps1", [ref]$null, [ref]$null) | Out-Null'` passed.
- `dotnet build src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release -v:minimal` passed.
- `dotnet run --project src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj -c Release --no-build -- --export-scenarios artifacts/benchmarkdotnet/lua-cli-scenarios-smoke` exported 5 scenarios.
- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir artifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root artifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4 --warmup-count 0 --iteration-count 1 --timeout-seconds 10` produced 5 reference CLI rows.
- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir artifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root artifacts/benchmarkdotnet/comparison-smoke --lua-cmd definitely-not-a-real-lua-command --warmup-count 0 --iteration-count 1 --timeout-seconds 10` skipped cleanly with `lua_cli_skipped=true`.
- `LUA_INIT='print("polluted")' python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir artifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root artifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4 --warmup-count 0 --iteration-count 1 --timeout-seconds 10` kept the version context sanitized and produced 5 reference CLI rows.
- Cursor Bugbot found that report-only `lua` CLI wall-time rows still contributed to the generic `changed=true` signal; fixed by excluding rows marked `ShowDeltaPercent=false` or `RuntimeKind=LuaCliWallTime` from tolerance signaling while continuing to render their raw deltas and missing-row diagnostics.
- Copilot found that relative path-like `--lua-cmd` values were validated relative to the caller but later executed with `cwd` set to the repo root; fixed by resolving all accepted executable paths, including relative `PATH` hits, to absolute paths.
- `git diff --check` passed.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed with 14,529 succeeded, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` passed.

## Remaining Phase A0 Work

- Commit full BenchmarkDotNet JSON baselines under `progress/`.
- Expand the workload list to the full Phase A0 suite.
- Add one-command scoreboard markdown generation for the full Phase A0 matrix.
- Add ratio-vs-NLua and exact allocation gates after the baseline is committed.
- Add the minimal Unity IL2CPP stopwatch scene.

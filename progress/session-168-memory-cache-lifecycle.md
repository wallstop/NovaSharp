# Session 168: Memory Cache Lifecycle

## Scope

- Inserted Phase A0.5 into `PLAN.md` before A1/A5 VM surgery.
- Added the retention research note and inventory in `docs/performance/memory-cache-retention-research.md`.
- Added explicit trim/stat lifecycle contracts for NovaSharp-owned pools and public facade memory APIs.
- Recorded the Phase A0.5 retention benchmark baseline in `progress/benchmarks/phase-a0.5-memory-retention-baseline.json`.

## Implementation Notes

- Added `SharedPoolRegistry`, trim levels, pool statistics/results, and monotonic/fake pool clocks.
- Reworked `GenericPool<T>` around timestamped entries, idle trim, retain floors, caps, and stats.
- Added capacity-admission caps to collection pools and byte-admission caps to array wrappers.
- Added trim/stat hooks for `DynValueArrayPool`, `ObjectArrayPool`, `SystemArrayPool<T>`, and `CallStackItemPool`.
- Added `LuaEngine.TrimMemory(...)`, `LuaEngine.GetMemoryStatistics()`, `LuaMemoryTrimLevel`, and `LuaMemoryStatistics`.
- Characterized `ScriptCompilationCache` as bounded while `_sources` and `_byteCode` remain script-lifetime append-only metadata.
- Addressed adversarial review by unregistering disposed transient pools, counting reachable coroutine stack retention, making non-list collection admission capacity-aware where portable, trimming stack/queue capacity before retaining cleared instances, and documenting that public stats combine engine metadata with process-wide NovaSharp shared pools while excluding `ArrayPool<T>.Shared` internals.
- Addressed final adversarial review by adding benchmark-only scratch prototypes, tracking total peak retained bytes through the public facade, retaining stack/queue instances after `TrimExcess()`, adding a real deep-recursion call-stack retention probe, and aligning public trim-level docs.
- Addressed second adversarial review by serializing lifecycle tests that trim process-wide pools, aligning `ScratchScope` prototype lifetime with other scratch variants, and fixing stale `DynValueArrayPool` small-bucket comments.
- Addressed third adversarial review by documenting that `ScratchScope` remains benchmark-only, adding generic-pool retain-floor and max-trim coverage, and moving the production stack-retention estimator out of the processor test-hook partial.
- Addressed Copilot review feedback by making `SharedPoolRegistry` track a registry-level peak of aggregated current retained bytes instead of summing per-pool historical peaks, and by making `Script.GetMemoryStatistics()` track this engine facade's peak from the current combined estimate.
- Merged `origin/main` after PR creation to clear the dirty merge state; the only manual conflict was regenerated `docs/audits/naming_audit.log`.
- Addressed CI follow-up failures by linking the new retention research note from `docs/README.md` and rewriting the registry peak regression test so it does not assume unrelated process-wide pools are idle during the full parallel test run.
- Addressed follow-up reviewer risks by aggregating `SystemArrayPool<T>` into one shared registry target, making DynValue oversize retention tests derive their length from the byte cap, giving closed generic collection pools distinct diagnostic names, pruning dead coroutine weak references during registration, avoiding `CallStackItemPool` helper-stack allocation for memory-pressure/critical trim, and pairing trim-epoch `Volatile.Read` sites with `Volatile.Write`.
- Addressed macOS CI risk in the facade coroutine memory smoke test by moving the coroutine-retention byte assertion to script-local test coverage and leaving the public facade smoke test to assert monotonic peak/stat invariants that remain valid while process-wide shared pools are trimmed concurrently.
- Addressed Copilot follow-up feedback by clearing oversized dropped reference arrays when callers requested clearing in `SystemArrayPool<T>`, `DynValueArrayPool`, and `ObjectArrayPool`.

## Validation So Far

- `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 18 tests.
- `./scripts/test/quick.sh -c DynValueArrayPoolTUnitTests` passed: 24 tests.
- `./scripts/test/quick.sh -c ScriptCompilationCacheTUnitTests` passed: 117 tests.
- `./scripts/test/quick.sh -c NovaSharpFacadeSmokeTUnitTests` passed: 54 tests.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh` passed: 15,045 tests.
- `bash ./scripts/dev/pre-commit.sh` completed successfully.
- `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release -m --verbosity quiet` passed.
- `MemoryRetentionBenchmarks` ran against the ShortRun job and exported JSON under `artifacts/benchmarkdotnet/phase-a0.5-memory-retention/`.
- The retention probe refreshed `progress/benchmarks/phase-a0.5-memory-retention-baseline.json` with retained pool counts, estimated retained bytes, peak retained bytes, B/op, Gen0/1/2, private bytes, and working set diagnostics.
- `python3 -m json.tool progress/benchmarks/phase-a0.5-memory-retention-baseline.json` passed.
- `git diff --check` passed.
- `./scripts/benchmarks/run-phase-a0-scoreboard.sh --enforce-phase-gates` passed with `regressed=false` and `phase_gate_failures=0`.
- After Copilot feedback and merge from `main`, `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 19 tests.
- After Copilot feedback and merge from `main`, `./scripts/test/quick.sh -c NovaSharpFacadeSmokeTUnitTests` passed: 54 tests.
- After Copilot feedback and merge from `main`, `./scripts/build/quick.sh` passed.
- After Copilot feedback and merge from `main`, `./scripts/test/quick.sh` passed: 15,046 tests.
- After Copilot feedback and merge from `main`, `git diff --check` passed.
- Bugbot reviewed commit `d56723fc` and found no new issues before the follow-up push.
- Copilot reviewed commit `d56723fc` and left one actionable issue; the aggregate peak-statistics fix above addresses it.
- CI `lint` failed on commit `bc0e672b` because `docs/README.md` did not link `docs/performance/memory-cache-retention-research.md`; fixed and locally verified `NOVASHARP_BASE_REF=$(git rev-parse origin/main) ./scripts/ci/ensure-readme-updates.sh`.
- CI `dotnet-tests (ubuntu-latest)` failed on commit `d264b1de` because the registry peak regression test asserted exact global retained bytes while the full suite may retain unrelated process-wide pool entries concurrently; fixed by asserting peak/current relationships with large per-pool estimates.
- After hardening the CI-failing test, `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 19 tests.
- After hardening the CI-failing test, `./scripts/test/quick.sh` passed: 15,046 tests.
- After follow-up reviewer hardening, `./scripts/test/quick.sh -c DynValueArrayPoolTUnitTests` passed: 24 tests.
- After follow-up reviewer hardening, `dotnet tool run csharpier check` on the touched C# files passed.
- After follow-up reviewer hardening, `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 21 tests.
- After follow-up reviewer hardening, `./scripts/build/quick.sh` passed.
- After follow-up reviewer hardening, `./scripts/test/quick.sh` passed: 15,048 tests.
- After moving the coroutine memory assertion to script-local coverage, `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 22 tests.
- After moving the coroutine memory assertion to script-local coverage, `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 54 tests.
- After moving the coroutine memory assertion to script-local coverage, `dotnet tool run csharpier check` on the touched C# files passed.
- After moving the coroutine memory assertion to script-local coverage, `git diff --check` passed.
- After moving the coroutine memory assertion to script-local coverage, `./scripts/test/quick.sh` passed: 15,049 tests.
- After the oversized-array clearing fix, `./scripts/test/quick.sh --full -c MemoryPoolLifecycleTUnitTests` passed: 24 tests.
- After the oversized-array clearing fix, `./scripts/test/quick.sh -c DynValueArrayPoolTUnitTests` passed: 24 tests.
- After the oversized-array clearing fix, `./scripts/test/quick.sh --full -c SystemArrayPoolTUnitTests` passed: 41 tests.
- After the oversized-array clearing fix, `dotnet tool run csharpier check` on the touched C# files passed.
- After the oversized-array clearing fix, `git diff --check` passed.
- After the oversized-array clearing fix, `./scripts/test/quick.sh` passed: 15,051 tests.

## Open Validation

- Push the CI follow-up fix, retrigger Bugbot and Copilot, then await PR CI and reviewer feedback.

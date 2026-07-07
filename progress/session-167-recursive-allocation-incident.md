# Session 167: Recursive Allocation Incident

Date: 2026-07-06

## Summary

Documented `FibonacciRecursive Execute` as a tracked allocation incident in `PLAN.md` and added permanent guardrails for the A1/A5 work:

- Added Phase A0 evidence for recursive and compute-heavy allocation pressure:
  - `FibonacciRecursive`: NovaSharp 1,239,599 us / 2,132,514,592 B; NLua 189,531 us / 144 B; Lua-CSharp 307,508 us / 200 B.
  - `TowerOfHanoi`: NovaSharp 21,924 us / 40,297,016 B; NLua 2,358 us / 24 B; Lua-CSharp 3,773 us / 144 B.
  - `NumericLoops`: NovaSharp 1,992 us / 2,844,016 B; NLua 701 us / 144 B; Lua-CSharp 616 us / 0 B.
  - `BinaryTrees`: NovaSharp 53,147 us / 83,863,472 B; NLua 17,138 us / 144 B; Lua-CSharp 15,590 us / 10,204,048 B.
  - `SpectralNorm`: NovaSharp 44,336 us / 77,139,648 B; NLua 4,449 us / 144 B; Lua-CSharp 6,740 us / 4,992 B.
- Tightened A1 exit criteria to require `FibonacciRecursive Execute <= 1 KiB/op`, `NumericLoops Execute == 0 B/op` steady-state, and no VM arithmetic `DynValue.NewNumber` wrapper allocation.
- Tightened A5 exit criteria to require one-arg/one-result Lua calls to allocate 0 B steady-state after warmup, with allocation retained only for escaped vararg/multi-return cases.
- Added a baseline-ratchet methodology rule so improved allocation rows become the new committed CI floor.
- Clarified A4.5 quickening/inline caches as follow-up speed work that cannot replace A1/A5 for the recursive allocation issue.

## Guardrails

- Added `RecursiveAllocationBenchmarks` with local BenchmarkDotNet `MemoryDiagnoser` probes for prepared-callable `fib(20)`, `fib(30)`, and a non-tail recursion depth scenario. The benchmark executes the top-level chunk once during setup, then measures only repeated execution of the prepared recursive callable. The Phase A0 scoreboard remains the authoritative CI allocation gate.
- Added TUnit allocation smoke tests for precompiled recursive calls. These enforce current-red ceilings and are intended to be ratcheted down after A1/A5 remove scalar wrapper and call-frame allocation.
- Added standalone Lua fixtures for the new recursion smokes under the comparison corpus so reference-Lua CI runs them. The Lua corpus extractor was run, but its broad legacy generated-output churn was not kept because it was unrelated to this scoped allocation guard.
- Added `scripts/lint/check-vm-hotpath-allocations.py` plus CI/pre-commit wiring. The guard rejects new non-allowlisted `new DynValue`, `DynValue.NewNumber`, `DynValue.NewInteger`, `new List<DynValue>`, `new DynValue[]`, visible-DynValue implicit `new[]` arrays, and `new ScriptExecutionContext` in VM processor files and current callback/context call-path files. It also catches matching target-typed `new(...)` declaration, return, expression-bodied return, stack-push, and direct callback `Invoke(new(...), ...)` context construction forms while allowlisting existing debt by source context.
- Addressed PR review feedback by teaching the allocation lint to ignore regular, interpolated, and multiline verbatim C# strings before scanning for allocation patterns, with self-tests covering the false-positive cases.
- Addressed follow-up PR review feedback by extending `DynValue.NewNumber` and `DynValue.NewInteger` lint rules to catch namespace-qualified and `global::`-qualified member calls.
- Added `.llm` guidance that VM opcode and Lua-call paths must be allocation-free after warmup, using inline `LuaValue`, stack windows, spans, and explicit slow-path allowlists.

## Validation

- `./scripts/test/quick.sh --full -c PrecompiledRecursiveCallAllocation` passed: 10 tests, 0 failed.
- `./scripts/build/quick.sh` passed.
- `./scripts/test/quick.sh --no-build` passed: 15,023 tests, 0 failed.
- `dotnet build src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj -c Release --no-restore` passed.
- `bash ./scripts/dev/pre-commit.sh` passed.
- `./scripts/ci/check-csharpier.sh` passed.
- `./scripts/ci/check-tooling-consistency.sh` passed.
- `./scripts/ci/check-shell-executable.sh` passed.
- `./scripts/ci/check-shell-python-invocation.sh` passed.
- `bash scripts/tests/run-lua-fixtures-fast.sh --fixtures-dir src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/PrecompiledRecursiveCallAllocationTUnitTests ...` passed the two recursive fixtures against reference Lua and NovaSharp for Lua 5.1, 5.2, 5.3, 5.4, and 5.5. Scoped `compare-lua-outputs.py --enforce` was attempted but is not valid for this narrowed directory because reference outputs are rooted at the scoped directory while NovaSharp batch outputs retain the fixture subdirectory name; the full CI comparison corpus remains the authoritative enforcement path.
- `python scripts/lint/check-vm-hotpath-allocations.py` passed with 36 explicitly allowlisted current allocation patterns.
- `python scripts/lint/check-vm-hotpath-allocations.py --detailed` passed with 36 explicitly allowlisted current allocation patterns.
- `python -m py_compile scripts/lint/check-vm-hotpath-allocations.py` passed.
- `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` ran successfully, but generated output was reverted to avoid unrelated churn.
- `python tools/test_lua_fixture_metadata.py` passed after generated corpus output was restored to the intended scope.
- `git diff --check` passed.

## Residual Risk

- The new TUnit smoke budgets intentionally preserve current behavior instead of claiming the path is fixed. The authoritative allocation floor remains the Phase A0 BenchmarkDotNet baseline until A1/A5 land and the baseline is refreshed from a representative CI runner.

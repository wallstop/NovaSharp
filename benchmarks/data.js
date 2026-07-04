window.BENCHMARK_DATA = {
  "lastUpdate": 1783183352371,
  "repoUrl": "https://github.com/wallstop/NovaSharp",
  "entries": {
    "NovaSharp Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "d9ce3497b5b700fef623256862982067bae93568",
          "message": "Initial work 3 (#19)\n\nCo-authored-by: Claude <noreply@anthropic.com>",
          "timestamp": "2025-12-11T14:08:31-08:00",
          "tree_id": "bf5c33ec8f2645faef1217923399d5a26018d7bf",
          "url": "https://github.com/wallstop/NovaSharp/commit/d9ce3497b5b700fef623256862982067bae93568"
        },
        "date": 1765491104659,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 619.732,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 354.457,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.2,
            "unit": "μs",
            "extra": "P95: 0.000μs"
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 679.457,
            "unit": "ns",
            "extra": "P95: 0.000μs"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "23896f70c6172799aa577949b9d262f21f92cb0b",
          "message": "Next Batch of Work (#30)\n\n## Summary\n\n- Provide a short summary of the change.\n\n## Testing\n\n- Describe the tests you ran (commands, platforms).\n\n## Analyzer Coverage\n\n- [x] Ran `dotnet build src/NovaSharp.sln -c Release -nologo`\n- Additional analyzer/build/test commands (list each, remove this line\nif none beyond the solution build):\n- _example: `dotnet build\nsrc/debuggers/NovaSharp.RemoteDebugger/NovaSharp.RemoteDebugger.csproj\n-c Release -nologo`_\n\n## Checklist\n\n- [ ] Updated relevant docs (`docs/README.md`, feature-specific\nMarkdown) when adding or changing functionality.\n- [ ] Updated `scripts/README.md` and the subfolder README when\nadding/modifying helper scripts.\n- [ ] Verified CI-critical helpers (tests, coverage, branding) still\nwork locally or via CI.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Changes are mostly devcontainer, documentation, and CI orchestration;\nruntime interpreter behavior is not the focus. CI workflow expansion\nincreases pipeline surface area but does not alter auth or production\ndeployment paths.\n> \n> **Overview**\n> This batch **replaces the stock .NET devcontainer** with a custom\n**Dockerfile** that pins the SDK from `global.json`, installs **Lua\n5.1–5.5**, **actionlint**, **yamllint**, and related CLI tooling, and\nsplits lifecycle into **`on-create.sh`** (restore before the C#\nextension) vs **`post-create.sh`** (Python `.venv`, hooks,\nverification). **`devcontainer.json`** adds NuGet/build cache mounts,\nexpanded VS Code settings, and **`update-content.sh`** /\n**`init-host.sh`** for pulls and host prep.\n> \n> **Contributor and agent guidance** moves into **`.cursorrules`**,\n**`.llm/context.md`**, skills, code samples, and a generated\n**`skills-index.json`**. **`.github/copilot-instructions.md`** and the\n**PR template** are tightened around scripted build/test and honest\nverification reporting.\n> \n> **CI and local gates** align workflows on **`global.json`**, add\n**concurrency/timeouts**, **NuGet caching**, and\n**`check-tooling-consistency`**. The **lua-comparison** job is a\n**matrix over OS × Lua 5.1–5.5**, builds reference Lua from source\n(including Windows MSVC), and runs **`run-lua-fixtures-fast.sh`** with\nricher comparison summaries; lint gains **Python harness self-tests**.\nAudit log paths move under **`docs/audits/`**. A new\n**`.githooks/pre-push`** mirrors key CI checks (optional build via\n`SKIP_BUILD_ON_PUSH`).\n> \n> Smaller tooling tweaks: **CSharpier 1.2.4**, **`.csharpierignore`**\nfor `PlatformAccessorBase.cs`, **`.dockerignore`**, and\n**gitattributes** for devcontainer scripts.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n0a7d6987c2e6bb4b6b337b821423e841929683fd. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-06-27T22:01:54-07:00",
          "tree_id": "bc446bba5d5f7788cc44c228c41d5b869f3456e0",
          "url": "https://github.com/wallstop/NovaSharp/commit/23896f70c6172799aa577949b9d262f21f92cb0b"
        },
        "date": 1782623121227,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 586.223,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 416.694,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.03,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 733.144,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "49583be89e44b61336c5205e353385e729811d28",
          "message": "[codex] Reduce host call allocations (#43)\n\n## Summary\n\n- Adds fixed-arity public `Script.Call` and `Coroutine.Resume` overloads\nfor common 1-4 `DynValue` and CLR object calls, plus fixed two- through\nfour-argument `Closure.Call(DynValue, ...)` overloads for common closure\ncalls without params-array allocations.\n- Optimizes `Closure.Call()` and `Closure.Call(params DynValue[])` to\nuse the cached closure `DynValue` path directly, while preserving\nnull-to-Lua-nil behavior for fixed and explicit params-array calls.\n- Adds fixed two- and three-key `Table` indexer, `Get`, `RawGet`, `Set`,\nand `Remove` overloads for common nested host table access without\ncaller-created key arrays, while preserving explicit `object[]`\nnested-path semantics.\n- Adds concrete fixed-arity `ModContainer.CallFunction` and\n`ModManager.BroadcastCall` overloads for common Unity/mod lifecycle\ncalls without changing `IModContainer`, while preserving explicit\nobject-array spread and cast array-as-single-argument behavior.\n- Extends the internal no-array VM call carrier through four arguments,\nincluding coroutine resume tuple packaging, so fixed public overloads\nstay on the fixed-arity path end to end.\n- Routes Lua-side `coroutine.resume` and `coroutine.wrap` zero- through\nfour-argument calls through existing fixed-arity coroutine resume\noverloads, leaving the larger-arity `GetArray` fallback unchanged.\n- Adds opt-in low-allocation CLR callback views through\n`CallbackArgumentsView`, `ScriptFunctionCallbackView`,\n`CallbackFunction.FromArgumentView`, and `DynValue.NewCallbackView`;\nfixed `Script.Call` overloads route to this path only for opted-in\ncallbacks, preserving legacy `CallbackArguments` behavior and\n`TryGetSpan` compatibility.\n- Adds a stack-backed Lua-to-CLR fast path for opt-in\n`CallbackArgumentsView` callbacks, avoiding legacy argument-list\nmaterialization for non-tuple VM stack ranges while preserving\ntuple/void expansion, method-call normalization, tail/yield request\nhandling, and pcall/xpcall frame behavior.\n- Adds fixed-arity `ScriptExecutionContext.Call` overloads for\ncallback-to-Lua and callback-to-callback calls, wires\n`ScriptFunctionCallbackView` through CLR conversion and module\nregistration, reuses call-stack frames through `CallStackItemPool`, and\nreuses cached scalar/closure conversions where read-only values are\nacceptable.\n- Avoids one-element continuation argument arrays for legacy VM\ncontinuations while preserving array-backed\n`CallbackArgumentsView.TryGetSpan` behavior for opt-in argument-view\ncontinuations.\n- Moves public `Script.LoadString` and `Script.LoadStream` compatibility\nguards onto cached static delegates plus explicit state, and hardens the\nscript compilation cache key/source equality and zero-entry cache\nbehavior.\n- Caches named `LoadString`, `LoadFile`, and text `LoadStream`\ncompilations by exact source text, compatibility version, and\nfriendly/source name, preserving distinct source metadata for different\nfilenames while making repeated same-file loads cache-hot.\n- Adds already-numeric VM arithmetic fast paths for `+`, `-`, `*`, `%`,\n`/`, `//`, `^`, and unary minus, avoiding nullable\nconversion/string-coercion checks when operands are already numbers\nwhile preserving string coercion, metamethod fallback, integer boundary\nbehavior, and pre-5.3 negative-zero handling.\n- Makes `ByteCode.EnterSource` return a concrete struct guard so direct\ncompile-source scopes no longer allocate guard objects after\nsource-stack capacity is warm, with an allocation regression test.\n- Replaces function-definition compiler post-declaration delegates with\nconstrained struct emitters, avoiding per-function declaration\nclosure/delegate scaffolding while preserving emitted bytecode/jump\nmath.\n- Adds a single-`_ENV` closure construction path for cache-hot chunks,\navoiding temporary `SymbolRef`, value, and symbol-name arrays while\nkeeping each closure's mutable environment slot distinct.\n- Adds focused tests and\nhost-call/loading/table-access/mod-call/callback/context-call/closure-call/runtime\nbenchmarks for the low-allocation paths and compatibility cases.\n- Preserves zero-value coroutine yield/resume tuple arity by returning\n`DynValue.EmptyTuple` instead of writable nil for empty coroutine\ntuples, matching Lua `select('#', ...)` behavior.\n\n## Validation\n\nLatest slice on `ccbbd2a9`:\n\n- Fixed production VM return-continuation dispatch so legacy\ncontinuations consume their single return argument through the fixed\nhelper instead of allocating a one-element `DynValue[]`\n- Preserved public argument-view continuation behavior by keeping\n`CallbackArgumentsView` continuations array-backed, so `TryGetSpan`\nremains true where existing `CallbackFunction.Invoke` behavior exposed a\nspan\n- Added focused coverage for `pcall` continuation return arity and a\ncustom argument-view tail-call continuation that proves the span-backed\nreturn argument remains visible\n- `dotnet build\nsrc/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj\n-v:minimal`\n- `./scripts/test/quick.sh --full -c ErrorHandlingModuleTUnitTests`: 132\npassed\n- `./scripts/test/quick.sh --no-build -c TailCallTUnitTests`: 40 passed\n- `./scripts/test/quick.sh --no-build -c\nCallbackArgumentsSpanTUnitTests`: 33 passed\n- `./scripts/test/quick.sh --no-build -c BasicModuleTUnitTests`: 243\npassed\n- `./scripts/test/quick.sh --no-build -c CoroutineModuleTUnitTests`: 302\npassed\n- `dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-c Release -v:minimal`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet\nrun -c Release --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-- --filter '*ContinuationBenchmarks*'`: 3 continuation benchmarks\ncompleted\n- `git diff --check`\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,658 passed\n- `.githooks/pre-push`\n- Copilot review requested after `ccbbd2a9`; latest review generated no\ncurrent unresolved, non-outdated review threads\n- PR CI on `ccbbd2a9`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `fd079be8`:\n\n- Copilot review on `d1c188ca` produced two relevant comments: trailing\n`DynValue.Void` host-to-script calls could contribute one extra nil-like\nargument, and `ScriptOptions.EnableScriptCaching` docs omitted\ncompatibility-version cache keying\n- Fixed production VM argument adjustment by trimming trailing\n`DynValue.Void` in `Processor.PushAdjustedTrailingValue`, including the\ntrailing tuple-expansion path, while preserving non-final `Void` scalar\nnil behavior\n- Updated script caching XML docs to state that cache hits require\nmatching source text, friendly/source name, and compatibility version\n- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests -m\nFixedDynValueCallOverloadsTrimTrailingVoidForScriptFunctions`: 5 passed\n- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests -m\nFixedDynValueCallOverloadsPreserveTupleExpansion`: 5 passed\n- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests -m\nFixedDynValueCallToLegacyClrFunctionPreservesTrailingExpansionEdges`: 1\npassed\n- `./scripts/test/quick.sh --no-build -c\nScriptExecutionContextTUnitTests -m\nFixedCallOverloadsPreserveLegacyCallbackExpansionSemantics`: 1 passed\n- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests`: 425\npassed\n- `./scripts/test/quick.sh --no-build -c\nScriptExecutionContextTUnitTests`: 86 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,648 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Copilot review requested after `fd079be8`; latest review generated no\nnew comments, and thread-aware review read found no current unresolved,\nnon-outdated review threads after resolving the two addressed comments\n- PR CI on `fd079be8`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `d1c188ca`:\n\n- Fixed the CI-only Lua comparison failure from `3f35abe3` by\nclassifying the intentional `both_error` parity for\n`TableValuedCallMetamethodDoesNotChainBeforeLua54.lua` in Lua 5.1-5.3;\nLua and NovaSharp already agreed semantically\n- Rechecked the failed CI artifacts with `compare-lua-outputs.py\n--enforce` for Lua 5.1, 5.2, and 5.3: all comparable fixtures matched,\nno new ratchet entries remained\n- `jq empty docs/testing/lua-error-ratchet.json`\n- `bash ./scripts/dev/pre-commit.sh`\n- `.githooks/pre-push`\n- Copilot review requested after `d1c188ca`; it produced the two\ncomments addressed in `fd079be8`\n- PR CI on `d1c188ca`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `3f35abe3`:\n\n- Fixed production fixed-arity `Script.Call` paths into legacy CLR\ncallbacks so arities 0-4 avoid the extra caller-created argument array\nwhile preserving null, void, and tuple normalization\n- Fixed fixed-arity and VM `__call` dispatch so table-valued `__call`\nchains are rejected before Lua 5.4 and accepted from Lua 5.4/default\n`Latest`, matching reference Lua behavior\n- Hardened fixed `CallbackArguments.TryGetSpan` storage with sequential\nfixed-field storage and disabled span exposure whenever raw storage\nwould disagree with indexer-normalized values\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nScriptCallTUnitTests -m\nFixedDynValueCallToLegacyClrFunctionAvoidsArgumentArrayAllocation`:\nfailed at `48 B/call` extra allocation against the `<16 B/call`\nthreshold\n- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests -m\nFixedDynValueCall`: 70 passed\n- `./scripts/test/quick.sh --full -c CallbackArgumentsSpanTUnitTests`:\n33 passed\n- `./scripts/test/quick.sh --no-build -c\nScriptExecutionContextTUnitTests`: 86 passed\n- `./scripts/test/quick.sh --no-build -c MetatableTUnitTests -m\nTableValuedCallMetamethod`: 6 passed\n- `./scripts/test/quick.sh --no-build -c\nCallbackArgumentsSpanTUnitTests`: 33 passed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests`: 15\npassed\n- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests`: 420\npassed\n- `./scripts/test/quick.sh --no-build -c MetatableTUnitTests`: 86 passed\n- Lua comparison checked the new table-valued `__call` fixtures against\nreference Lua 5.1-5.5 and NovaSharp; pre-5.4 both errored with `attempt\nto call a table value`, while Lua 5.4/5.5 printed `PASS`\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,643 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Copilot review requested after `3f35abe3`; latest review generated no\nnew comments, but PR CI exposed the intentional pre-5.4 Lua comparison\nratchet metadata gap fixed in `d1c188ca`\n\nPrevious slice on `507fbcf9`:\n\n- Copilot review on `363c1e47` produced two current correctness comments\nabout zero-value coroutine arity: `coroutine.resume(co)` after\n`coroutine.yield()` returned `true, nil` instead of only `true`, and\nsuspended `coroutine.yield()` resumed with no arguments received one nil\ninstead of zero values\n- Fixed production tuple conversion by preserving empty coroutine tuples\nthrough `DynValue.EmptyTuple` in `DynValue.NewTuple()`,\n`YieldRequest.ToTuple()`, and `Processor.ClrCallArguments.ToTuple()`\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nProcessorCoroutineModuleTUnitTests -m\nSuspendedResumeWithNoArgumentsPreservesZeroArity`: failed all Lua\nversions with count `1` instead of `0`\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nCoroutineModuleTUnitTests -m ResumeZeroValueYieldReturnsOnlyStatus`:\nfailed all Lua versions with count `2` instead of `1`\n- `./scripts/test/quick.sh --full -c CoroutineModuleTUnitTests -m\nZeroValue`: 10 passed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests -m\nSuspendedResumeWithNoArgumentsPreservesZeroArity`: 5 passed\n- `./scripts/test/quick.sh --no-build -c DynValueTUnitTests -m\nNewTupleHandlesEmptyAndSingleInputs`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CoroutineModuleTUnitTests -m\nYieldRequestToTuplePreservesZeroYieldEmptyTuple`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CoroutineModuleTUnitTests`: 302\npassed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests`: 109 passed\n- `./scripts/test/quick.sh --no-build -c DynValueTUnitTests`: 89 passed\n- `./scripts/test/quick.sh --no-build -c\nSerializationExtensionsTUnitTests`: 16 passed\n- `./scripts/test/quick.sh --no-build -c Coroutine`: 666 passed\n- Lua comparison snippet against reference `lua5.1` through `lua5.5`\nmatched NovaSharp exactly for zero-value resume, suspended\nresume-without-args, and wrap yield arity\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,586 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Copilot review requested after `507fbcf9`; latest review generated no\nnew comments, and thread-aware review read found no current unresolved,\nnon-outdated Copilot threads\n- PR CI on `507fbcf9`: 22 checks passed after benchmark rerun; expected\nskipped jobs: `comparison`, `lint-autofix`\n- Initial CI benchmark alert for\n`RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")`\nreported `713.928 ns` versus prior `586.223 ns`; local filtered\nbenchmark reported `552.724 ns` and `888 B/op`, and rerunning the failed\nbenchmark check passed\n\nPrevious slice on `363c1e47`:\n\n- Main production slice `4ca46ee7` removes the extra caller-created\nargument array for direct fixed-arity `ScriptExecutionContext.Call`\ncalls into legacy CLR callbacks, while keeping non-function/metamethod\nfallback array materialization for correctness\n- Fixed generic context-call `__call` dispatch to prepend the callable\nas argument 0, including chained `__call` resolution\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nScriptExecutionContextTUnitTests -m\nFixedCallOverloadsAvoidLegacyCallbackArgumentArrayAllocation`: failed at\n`48 B/call` extra allocation versus the no-arg baseline against the `<16\nB/call` threshold\n- `./scripts/test/quick.sh --full -c ScriptExecutionContextTUnitTests`:\n80 passed\n- `./scripts/test/quick.sh --no-build -c\nScriptExecutionContextTUnitTests`: 80 passed\n- `./scripts/test/quick.sh --no-build -c\nCallbackArgumentsSpanTUnitTests`: 33 passed\n- Follow-up `363c1e47` addressed Copilot's dynamic-stack span comment by\ndocumenting that list-backed dynamic stacks intentionally return `false`\non netstandard2.1 because exposing the backing storage without\nallocation is unsupported\n- `./scripts/test/quick.sh --full -c CallbackArgumentsSpanTUnitTests`:\n33 passed after the follow-up\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,571 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet\nrun -c Release --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-- --filter '*ScriptExecutionContextCallBenchmarks*'`: 4 context-call\nbenchmarks completed\n- Adversarial review: blocking findings addressed by preserving Lua\n`__call` self-argument semantics and avoiding hidden fixed-argument\narray materialization through `TryGetSpan`; nonblocking data-driven\narity/null/tuple/void/chained-`__call` coverage was added\n- Copilot review requested after `4ca46ee7`; it produced one current\ncomment on `FastStackDynamic<T>.TryGetSpan`, which was addressed in\n`363c1e47` and is now outdated\n- Copilot review requested after `363c1e47`; thread-aware review read\nfound no current unresolved, non-outdated review threads\n- PR CI on `363c1e47`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `5c9f4700`:\n\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nScriptCallTUnitTests -m\nDynValueCallOverloadsPreserveNullArgumentsAsNil`: failed with\n`NullReferenceException` in `Processor.PushAdjustedArguments` when a\nfixed or array-backed `DynValue` argument was null\n- Pre-change red run of `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests -m\nSuspendedResumeDynValueArrayPreservesNullsAsNil`: failed with\n`ArgumentNullException` while materializing a suspended resume tuple\ncontaining null `DynValue` entries\n- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests -m\nDynValueCallOverloadsPreserveNullArgumentsAsNil`: 5 passed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests -m\nSuspendedResumeDynValueArrayPreservesNullsAsNil`: 5 passed\n- `./scripts/test/quick.sh --no-build -c ScriptCallTUnitTests`: 375\npassed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests`: 104 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,560 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Copilot review requested after push; follow-up review on `5c9f4700`\ngenerated no new comments\n- PR CI on `5c9f4700`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `42b5a50c`:\n\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nTableTUnitTests -m NestedPathErrorsIncludeOffendingKey`: failed because\nnested path diagnostics literally contained `{0}` and omitted the\noffending key\n- Pre-change red run of `./scripts/test/quick.sh --no-build -c\nCallbackFunctionTUnitTests -m\nCallArgumentViewTreatsNullFixedArgumentsAsNil`: failed with\n`NullReferenceException` in `CallbackArgumentsView`\n- Pre-change red run of `./scripts/test/quick.sh --no-build -c\nCallbackArgumentsSpanTUnitTests -m\nArgumentViewTreatsNullStoredArgumentsAsNil`: failed with\n`NullReferenceException` in `CallbackArgumentsView`\n- `./scripts/test/quick.sh --full -c TableTUnitTests -m\nNestedPathErrorsIncludeOffendingKey`: 2 passed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests -m\nCallArgumentViewTreatsNullFixedArgumentsAsNil`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests -m\nCallLegacyCallbackTreatsNullFixedArgumentsAsNil`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CallbackArgumentsSpanTUnitTests\n-m ArgumentViewTreatsNullStoredArgumentsAsNil`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests -m\nInvokeArgumentViewTreatsMethodCallsOnlyForUserDataUnderDotBehaviour`: 1\npassed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests -m\nInvokeTreatsMethodCallsOnlyForUserDataUnderDotBehaviour`: 1 passed\n- `./scripts/test/quick.sh --full -c CallbackArgumentsSpanTUnitTests`:\n33 passed\n- `./scripts/test/quick.sh --no-build -c CallbackFunctionTUnitTests`: 15\npassed\n- `./scripts/test/quick.sh --no-build -c TableTUnitTests`: 407 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,550 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Adversarial review: found tuple-expanded null and `TryGetSpan`\nraw-null exposure gaps; addressed by normalizing tuple elements to Lua\nnil, making span access return false when null normalization is\nrequired, and adding data-driven legacy/view coverage for `RawGet`,\nindexer, `CopyTo`, and `TryGetSpan`\n- Copilot review requested after push; thread-aware review read found no\ncurrent unresolved, non-outdated review threads after `42b5a50c`\n- PR CI on `42b5a50c`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `508aec2d`:\n\n- Fixed the Ubuntu CI failure in\n`MultipleConcurrentResumeAttemptsOnlyOneSucceeds(Lua54)` by keeping\nprocessor thread ownership until the outermost nested leave and making\nthe concurrent resume test wait until all competing attempts have\nobserved the in-flight coroutine\n- `./scripts/test/quick.sh --full -c CoroutineModuleTUnitTests -m\nMultipleConcurrentResumeAttemptsOnlyOneSucceeds`: 5 passed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoreLifecycleTUnitTests`: 32 passed\n- `./scripts/test/quick.sh --no-build -c CoroutineModuleTUnitTests`: 282\npassed\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,542 passed\n- `git diff --check`\n- `.githooks/pre-push`\n- Copilot review requested after push\n- PR CI on `508aec2d`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `af48a41e`:\n\n- Pre-change red run of `KeywordTokensUseCanonicalText`: failed because\nkeyword tokens did not reuse canonical keyword text references\n- Pre-change red run of `KeywordLexingAvoidsIdentifierStringAllocation`:\nfailed because keyword lexing still allocated identifier strings\n- `./scripts/test/quick.sh --full -c LexerTUnitTests`: 5 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,537 passed\n- `git diff --check`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet\nrun -c Release --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-- --filter '*ScriptLoadingBenchmarks*'`: 4 compile-only script-loading\nbenchmarks completed\n- `.githooks/pre-push`\n- Copilot review requested after push; actionable comments were\naddressed in `42b5a50c`\n- CI on `af48a41e` exposed the later concurrent resume failure fixed by\n`508aec2d`\n\nPrevious slice on `63a1e51a`:\n\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nCoroutineModuleTUnitTests -m\nYieldOneArgumentAvoidsPerCallArgumentArrayAllocation`: failed at `32\nB/yield` extra for one yielded argument against the `<16 B/yield`\nregression threshold\n- `./scripts/test/quick.sh --full -c CoroutineModuleTUnitTests -m\nYieldOneArgumentAvoidsPerCallArgumentArrayAllocation`: 1 passed\n- `./scripts/test/quick.sh --full -c CoroutineModuleTUnitTests`: 282\npassed\n- `./scripts/test/quick.sh --no-build -c Coroutine`: 646 passed\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,532 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `git diff --check`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-c Release -- --filter \"*RuntimeBenchmarks*\" --exporters json`: 4\nruntime benchmarks completed\n- Adversarial review: low findings addressed by preserving zero-yield\nwritable-nil materialization, preserving 2-4 value buffer reuse, adding\ntuple-expanded yield coverage, and adding a processor IL guard for the\nlazy yield tuple path\n- Pre-push hook: CSharpier, Markdown, branding, namespace alignment,\ntooling consistency, YAML/action lint, and `./scripts/build/quick.sh`\npassed\n- PR CI on `63a1e51a`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `05c65276`:\n\n- Pre-change calibration on the old module `GetArray` path: focused\nallocation probe measured `256 B/resume` extra for three initial resume\narguments versus the fixed helper's `208 B/resume`; adversarial review\nflagged the narrow GC-counter threshold as brittle, so the final\nregression guard was replaced with deterministic IL dispatch coverage.\n- `./scripts/test/quick.sh --full -c CoroutineModuleTUnitTests -m\nResumeAndWrapDispatchThroughFixedArityCoroutinePaths`: 1 passed\n- `./scripts/test/quick.sh --no-build -c CoroutineModuleTUnitTests`: 273\npassed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineApiTUnitTests`: 155 passed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineLifecycleTUnitTests`: 21 passed\n- `./scripts/test/quick.sh --no-build -c CoroutineLifecycleTUnitTests`:\n119 passed\n- `./scripts/test/quick.sh --no-build -c\nProcessorCoroutineModuleTUnitTests`: 99 passed\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`: 13,523 passed\n- `bash ./scripts/dev/pre-commit.sh`\n- `git diff --check`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-c Release -- --filter \"*RuntimeBenchmarks*\" --exporters json`: 4\nruntime benchmarks completed\n- Adversarial review: no runtime correctness findings; the original\nallocation-test brittleness finding was addressed by replacing the\nGC-threshold guard with an IL dispatch guard.\n- Pre-push hook: CSharpier, Markdown, branding, namespace alignment,\ntooling consistency, YAML/action lint, and `./scripts/build/quick.sh`\npassed\n- PR CI on `05c65276`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`\n\nPrevious slice on `53561003`:\n\n- Pre-change red run of `./scripts/test/quick.sh --full -c\nScriptCompilationCacheTUnitTests -m\nCachedLoadStringAvoidsTemporaryClosureScaffoldingAllocations`: failed at\n`376 B/load` against the `<340 B/load` regression threshold\n- `./scripts/test/quick.sh --full -c ScriptCompilationCacheTUnitTests -m\nCachedLoadStringAvoidsTemporaryClosureScaffoldingAllocations`: 1 passed\n- `./scripts/test/quick.sh --no-build -c\nScriptCompilationCacheTUnitTests -m\nCachedLoadStringClosuresKeepIndependentEnvironmentSlots`: 5 passed\n- `./scripts/test/quick.sh --full -c ScriptCompilationCacheTUnitTests -m\nCachedLoadStringSetFenvKeepsIndependentEnvironmentSlots`: 1 passed\n- `./scripts/test/quick.sh --no-build -c\nScriptCompilationCacheTUnitTests`: 73 passed\n- `./scripts/test/quick.sh --no-build -c SetFenvGetFenvTUnitTests`: 25\npassed\n- `./scripts/test/quick.sh --no-build -c\nDebugModuleTapParityTUnitTests`: 85 passed\n- `dotnet build\nsrc/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj\n-c Release --no-restore -m:1 /nr:false`\n- `./scripts/test/quick.sh --no-build`: 13,522 passed\n- `dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n-c Release --no-restore -m:1 /nr:false`\n- `NOVASHARP_SKIP_PERFORMANCE_DOC=1 NOVASHARP_BENCHMARK_SUMMARY=1 dotnet\nrun -c Release --project\nsrc/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj\n--no-build -- --filter '*ScriptLoadingBenchmarks.LoadCached*'`: 8\nbenchmarks completed; cached and cached-named loads were `232 B/op`\nacross Tiny/Small/Medium/Large\n- `git diff --check`\n- `bash ./scripts/dev/pre-commit.sh`\n- Adversarial review: no blocking findings; low notes addressed by\nexposing closure symbols as `IReadOnlyList<string>` backed by a static\nread-only `_ENV` collection\n- Pre-push hook: CSharpier, Markdown, branding, namespace alignment,\ntooling consistency, YAML/action lint, and `./scripts/build/quick.sh`\npassed\n- PR CI on `53561003`: 22 checks passed; expected skipped jobs:\n`comparison`, `lint-autofix`; benchmark initially hit a `TableMutation`\nalert (`8.218 us` vs `7.03 us`, `1.17x`) and passed on rerun\n\nEarlier slices also ran targeted/full coverage for `Closure`,\n`ScriptCall`, `CallbackArguments`, `CallbackFunction`,\n`ScriptExecutionContext`, `ErrorHandlingModule`,\n`ColonOperatorBehaviour`, `ProcessorCoroutine`, `ScriptLoad`,\n`ScriptCompilationCache`, `Table`, `ModContainer`, `ByteCode`, binary\ndumps, benchmark builds, pre-commit, push hooks, and PR CI on prior\nheads.\n\n## Benchmark Note\n\nFor legacy VM continuations on `ccbbd2a9`, the focused continuation\nbenchmark completed with `Continuation: pcall no return` at `729.9 ns`\nand `1.23 KB/op`, `Continuation: pcall one return` at `926.7 ns` and\n`1.59 KB/op`, and `Continuation: tostring metamethod` at `698.8 ns` and\n`1.05 KB/op`. The production change removes the legacy one-element\ncontinuation argument array while intentionally keeping argument-view\ncontinuations array-backed for `TryGetSpan` compatibility.\n\nFor trailing `Void` trimming on `fd079be8`, no local benchmark was rerun\nbecause the slice fixes vararg correctness and documentation. The latest\nPR benchmark check passed.\n\nFor the Lua comparison ratchet metadata update on `d1c188ca`, no\nbenchmark was rerun because the slice only classifies expected error\nparity in test metadata. The PR benchmark check passed.\n\nFor fixed direct legacy CLR callbacks on `3f35abe3`, the rerun\n`ClrCallbackCallBenchmarks` showed legacy fixed calls at `120 B/op` for\nthree and four arguments versus legacy params calls at `168 B/op` and\n`176 B/op`, preserving the fixed-overload allocation reduction while\ncallback-view calls remained at `48 B/op`.\n\nFor empty coroutine tuple semantics on `507fbcf9`, no production\nperformance regression was found. The local filtered `CoroutinePipeline`\nbenchmark reported `552.724 ns` and `888 B/op`; the first CI sample\nalerted at `713.928 ns` versus the previous `586.223 ns`, but the\nbenchmark rerun passed. This slice is primarily a Lua correctness fix\nand uses the existing singleton `DynValue.EmptyTuple` for zero-value\ntuples.\n\nFor fixed direct `ScriptExecutionContext.Call` legacy callbacks on\n`363c1e47`, the pre-change allocation regression failed at `48 B/call`\nextra for three fixed arguments compared with the no-argument baseline.\nThe final filtered short-run benchmark reports `Context Call: 3 fixed\nDynValues` at `712 B/op` versus params at `760 B/op`, saving `48 B/op`;\n`Context Call: 4 fixed DynValues` reports `824 B/op` versus params at\n`880 B/op`, saving `56 B/op`. Scope note: this avoids the extra array on\nthe direct CLR callback path; non-function/metamethod fallback still\nmaterializes arguments where Lua `__call` semantics require it. The\ndynamic list-backed stack configuration still reports no span on\nnetstandard2.1, now documented explicitly, so callers use the existing\nindexer/list fallback without introducing an allocation-prone fake span.\n\nFor VM null call-argument normalization on `5c9f4700`, no local\nbenchmark was rerun because the slice fixes null-as-nil correctness in\nCLR-to-VM call argument boundaries. The production path preserves the\nno-null array-backed fast path and only allocates a normalized tuple\narray when caller-provided arguments contain null entries; CI benchmark\npassed on the pushed head.\n\nFor Copilot diagnostic fixes on `42b5a50c`, no benchmark was rerun\nbecause the slice fixes error diagnostics and null-as-nil callback\ncorrectness. The `TryGetSpan` change preserves zero-allocation span\naccess for already-safe contiguous backing storage and returns `false`\nwhen null normalization would otherwise be required, so callers can fall\nback to the existing indexer/`CopyTo` paths.\n\nFor Lua-side `coroutine.yield(value)` on `63a1e51a`, the red allocation\nprobe failed before the implementation at `32 B/yield` extra for one\nyielded argument versus no yielded arguments, then passed after\nfixed-field yield request storage and lazy tuple conversion. The final\ncoverage also guards zero-yield writable-nil parity, fixed-arity return\nbuffer reuse, tuple-expanded yield arguments, and the VM processor's use\nof `YieldRequest.ToTuple()`. The local short-run `RuntimeBenchmarks`\nsmoke completed with `CoroutinePipeline` at `515.2 ns` and `888 B/op`,\n`NumericLoops` at `337.7 ns` and `704 B/op`, `TableMutation` at `8.388\nus` and `25,568 B/op`, and `UserDataInterop` at `735.6 ns` and `1,112\nB/op`; the benchmark-level `CoroutinePipeline` allocation remains `888\nB/op`, so the direct allocation regression test is the evidence for this\nnarrower one-argument yield-array removal.\n\nFor Lua-side coroutine resume dispatch on `05c65276`, the final\ndeterministic test guards that `coroutine.resume` and `coroutine.wrap`\nreach fixed-arity `Coroutine.Resume` overloads for one through four\narguments, with `CallbackArguments.GetArray(int)` reserved for the\nlarger-arity fallback. The local short-run `RuntimeBenchmarks` smoke\nreports: `CoroutinePipeline` `509.0 ns` and `888 B/op`, `NumericLoops`\n`486.8 ns` and `704 B/op`, `TableMutation` `8.395 us` and `25,568 B/op`,\nand `UserDataInterop` `695.2 ns` and `1,112 B/op`. Note:\n`CoroutinePipeline` primarily exercises resumed coroutine calls after\nthe coroutine has yielded; the IL guard covers the initial-call\nfixed-arity dispatch shape directly.\n\nFor cached `LoadString` closure scaffolding on `53561003`, the\npre-change regression failed at `376 B/load`; the final filtered\nshort-run `ScriptLoadingBenchmarks.LoadCached*` reports `232 B/op` for\nboth `Load Cached` and `Load Cached Named` across\nTiny/Small/Medium/Large, removing `144 B/op` from cache-hit closure\nsetup while keeping distinct mutable `_ENV` slots per closure.\n\nFor function compile delegate hooks on `83d2c631`, the improvement is\nstructural: function declaration compilation no longer routes\npost-declaration bytecode through a `Func<int>` callback, and the\nregression test guards that delegate hook from returning.\n\nFor EnterSource guard allocation on `adc7f594`, the focused regression\ntest failed before the implementation at `24,576 B` allocated across\n1,024 direct guards and passes after the change at `0 B` after\nsource-stack capacity warmup. The focused `ScriptLoadingBenchmarks` run\ncompleted as a smoke; cached load allocation remains `376 B/op`, and\ncompile-heavy cases remain dominated by existing compile allocations.\n\nFor runtime scenarios on `fe3fba4c`, the local short-run\n`RuntimeBenchmarks` smoke reports: `CoroutinePipeline` `559.5 ns`,\n`NumericLoops` `345.7 ns`, `TableMutation` `7.546 us`, and\n`UserDataInterop` `625.3 ns`. Note: the current `RuntimeBenchmarks`\nharness appears to call chunks that return inner scenario functions, so\n`NumericLoops` is useful as CI continuity but should not be treated as a\ntrue numeric-loop body measurement until the benchmark harness is\ncorrected.\n\nFor named script-cache hits on `6d5ab4e1`, the focused short-run\nbenchmark reports `Load Cached Named` at `376 B/op` across\nTiny/Small/Large, matching the anonymous `Load Cached` allocation count.\nTiming was noisy for sub-microsecond cases, but Large named cache hits\nmeasured `4.396 us` versus anonymous cache hits at `4.833 us` in the\nsame run.\n\nFor fixed `Closure.Call(DynValue, ...)` overloads on `203dfc6e`, the\nfiltered short-run benchmark reports `Closure Call: 3 DynValues` at `664\nB/op` versus params at `712 B/op`, saving `48 B/op`; `Closure Call: 4\nDynValues` reports `776 B/op` versus params at `832 B/op`, saving `56\nB/op`. Timing was noisy, but the allocation reduction was visible in the\nsame run.\n\nFor stack-backed Lua-to-CLR callback views on `07ec8c30`, the filtered\nshort-run benchmark reports view calls at `392 B/op` versus legacy at\n`464 B/op` for both three- and four-argument cases, saving `72 B/op`.\n\nFor fixed `ScriptExecutionContext.Call` overloads on `573c3f26`, the\nfiltered short-run benchmark reports `Context Call: 3 fixed DynValues`\nat `712 B/op` versus params at `760 B/op`, saving `48 B/op`; `Context\nCall: 4 fixed DynValues` reports `824 B/op` versus params at `880 B/op`,\nsaving `56 B/op`.\n\nFor opt-in CLR callback views on `514e5869`, the filtered short-run\nbenchmark reports `CLR Callback View Call: 3 fixed DynValues` at `48\nB/op` versus legacy fixed at `128 B/op`, saving `80 B/op`; `CLR Callback\nView Call: 4 fixed DynValues` reports `48 B/op` versus legacy fixed at\n`136 B/op`, saving `88 B/op`.\n\nEarlier slices showed fixed host calls reducing allocations for Lua\ncalls, nested table access, mod calls, and cache-hot loading.\nRepresentative filtered runs: `Host Call: 4 DynValues` at `776 B/op`\nversus params at `832 B/op`; fixed two-key table `RawGet` avoids the\ncaller-side `object[]` allocation; `Mod CallFunction: 2 fixed objects`\nat `552 B/op` versus params at `632 B/op`; cache-hot `LoadString` at\n`376 B/op` versus `488 B/op` on the previous implementation.",
          "timestamp": "2026-07-01T15:27:02-07:00",
          "tree_id": "0f1bbc3b28dc598494ae07713f51dd0a83aa80a7",
          "url": "https://github.com/wallstop/NovaSharp/commit/49583be89e44b61336c5205e353385e729811d28"
        },
        "date": 1782945395603,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 568.451,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 381.364,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 8.484,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 708.201,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "28795615307ef60b882ddc80ecaca37c0c975c9a",
          "message": "[codex] Add LuaCSharp comparison benchmark (#44)\n\n## Summary\n- Add LuaCSharp 0.5.5 as a same-run comparison benchmark target.\n- Normalize compile rows so every runtime creates a fresh state before\nloading the scenario.\n- Render LuaCSharp columns in benchmark delta reports and warn when\nexpected external runtime cells are missing.\n- Update Phase A0 documentation/progress notes without claiming the full\nscoreboard is complete.\n\n## Validation\n- dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release -v:minimal\n- python3 tools/test_render_benchmark_deltas.py\n- python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py\ntools/test_render_benchmark_deltas.py\n- direct LuaPerformanceBenchmarks LuaCSharp smoke for NumericLoops\n- ./scripts/build/quick.sh --all\n- ./scripts/test/quick.sh\n- bash ./scripts/dev/pre-commit.sh\n- git push pre-push hook checks\n\n## Remaining Phase A0 Work\n- Reference lua CLI wall-time context is not implemented yet.\n- Full workload suite and committed JSON baselines are not implemented\nyet.\n- Ratio-vs-NLua and exact allocation gates remain open.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Benchmarking, reporting, and documentation changes only; no\ninterpreter runtime behavior changes beyond test isolation helpers.\n> \n> **Overview**\n> Adds **Lua-CSharp** (`LuaCSharp` 0.5.5) as a fourth same-run\nBenchmarkDotNet target in `WallstopStudios.NovaSharp.Comparison`, with\ncompile/execute rows wired like MoonSharp and NLua. **Compile**\nbenchmarks now spin up a **fresh runtime state per engine** (returning a\ndummy `int` with `GC.KeepAlive`) so compile timings are comparable\nacross NovaSharp, MoonSharp, NLua, and Lua-CSharp.\n> \n> The delta renderer treats **LuaCSharp** as a first-class external\nruntime, expects **MoonSharp / NLua / LuaCSharp** cells when NovaSharp\nis present, and surfaces **missing expected cells** in markdown, stdout\n(`missing_external_runtime_cells=`), benchmark CI warnings, and PR\ncomment metadata. Tests cover Lua-CSharp matrix columns and\nmissing-runtime diagnostics.\n> \n> Docs and planning note **Phase A0** progress (Lua-CSharp wired; full\nscoreboard, `lua` CLI column, gates still open). Minor test fixes:\n`ScriptCustomConvertersScope` uses `Script.BeginGlobalOptionsScope()`\nfor isolation; a coroutine tail-call test drops static state.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\ncd2077e84293703b84b8e424a2874773f447cf95. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-01T19:24:41-07:00",
          "tree_id": "256c5b49907a0bbfadb696ab283ef2f64d938a25",
          "url": "https://github.com/wallstop/NovaSharp/commit/28795615307ef60b882ddc80ecaca37c0c975c9a"
        },
        "date": 1782959784503,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 558.264,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 382.003,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.445,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 696.316,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "fd148d38e28633abbbb5660e8b18e0f00699ed4a",
          "message": "[codex] Add reference lua CLI benchmark context (#45)\n\n## Summary\n\n- Add a comparison-project export mode for benchmark Lua scenarios.\n- Add reference `lua` CLI wall-time context as a BenchmarkDotNet-shaped\ncomparison artifact.\n- Render the context as `Lua CLI wall-time`, with command/version\nmetadata, unknown memory/GC cells, and a CI-visible\n`missing_lua_cli_rows` signal.\n- Wire bash, PowerShell, and benchmark CI flows to produce the CLI\ncontext, update PLAN.md, and record progress in\n`progress/session-125-phase-a0-lua-cli-context.md`.\n- Address Copilot feedback by validating and resolving\n`--lua-cmd`/`LUA_CMD` before subprocess use.\n\n## Validation\n\n- `python3 tools/test_render_benchmark_deltas.py`\n- `python3 tools/test_run_lua_cli_context.py`\n- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py\nscripts/benchmarks/run-lua-cli-context.py\ntools/test_render_benchmark_deltas.py`\n- `python3 -m py_compile scripts/benchmarks/run-lua-cli-context.py\ntools/test_run_lua_cli_context.py`\n- `bash -n scripts/benchmarks/run-benchmarks.sh`\n- `pwsh -NoProfile -Command\n'[System.Management.Automation.Language.Parser]::ParseFile(\"scripts/benchmarks/run-benchmarks.ps1\",\n[ref]$null, [ref]$null) | Out-Null'`\n- `dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release -v:minimal`\n- `dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release --no-build -- --export-scenarios\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke`\n- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root\nartifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4\n--warmup-count 0 --iteration-count 1 --timeout-seconds 10`\n- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root\nartifacts/benchmarkdotnet/comparison-smoke --lua-cmd\ndefinitely-not-a-real-lua-command --warmup-count 0 --iteration-count 1\n--timeout-seconds 10`\n- `LUA_INIT='print(\"polluted\")' python3\nscripts/benchmarks/run-lua-cli-context.py --scenario-dir\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root\nartifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4\n--warmup-count 0 --iteration-count 1 --timeout-seconds 10`\n- `NOVASHARP_BASE_REF=origin/main ./scripts/ci/ensure-readme-updates.sh`\n- `git diff --check`\n- `./scripts/build/quick.sh --all`\n- `./scripts/test/quick.sh` (14,529 succeeded, 0 failed, 0 skipped)\n- `bash ./scripts/dev/pre-commit.sh`\n- push hook: formatting, markdown, branding, namespace, tooling,\nYAML/actionlint, and build checks passed\n\n## Notes\n\nThe reference `lua` row is intentionally process wall-time context, not\nan apples-to-apples managed execution/allocation benchmark. Full Phase\nA0 baseline artifacts, workload expansion, ratio/allocation gates, and\nUnity IL2CPP spot-checks remain open.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Changes are limited to benchmark tooling, Python scripts, and CI\nworkflows; the Lua interpreter runtime is untouched.\n> \n> **Overview**\n> Adds **reference `lua` CLI wall-time** as an out-of-process column on\nthe Phase A0 comparison scoreboard, alongside in-process MoonSharp,\nNLua, and Lua-CSharp runs.\n> \n> The comparison tool gains **`--export-scenarios`** so\n`BenchmarkScripts` scenarios are written once as `.lua` files for\nexternal runners. New **`run-lua-cli-context.py`** spawns the reference\nexecutable per iteration, records mean/P95 wall time, and writes\nBenchmarkDotNet-shaped JSON (with `RuntimeKind=LuaCliWallTime`,\ncommand/version context, and no managed memory/GC claims).\n**`render-benchmark-deltas.py`** renders that column, shows memory/GC as\n`-` when absent, supports **`--expect-lua-cli`** and\n**`missing_lua_cli_rows`**, and treats CLI rows as **report-only** (they\ndo not drive `changed=true`). Local **`run-benchmarks.sh` / `.ps1`** and\n**benchmark CI** export scenarios, install **`lua5.4`** on Ubuntu, run\nthe Python measurer, and surface missing CLI context in PR delta\ncomments.\n> \n> Tests cover the renderer and Lua command resolution; **PLAN.md** and\nsession progress document Phase A0 completion for this item.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n249acb4dc473794277b36146224df802827fbb56. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-02T10:26:18-07:00",
          "tree_id": "9a27059027d3a2edc6fe39fe80a438e963cd4031",
          "url": "https://github.com/wallstop/NovaSharp/commit/fd148d38e28633abbbb5660e8b18e0f00699ed4a"
        },
        "date": 1783013896685,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 513.919,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 373.47,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 6.939,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 633.397,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "f8b03c4b7817f2df9017751f00e21a85476697b1",
          "message": "[codex] Expand comparison benchmark workloads (#46)\n\n## Summary\n\n- expand the Phase A0 comparison benchmark catalog from 5 to 16 pure-Lua\nworkloads\n- keep BenchmarkDotNet scenario params and reference lua CLI export on\none shared scenario list\n- update PLAN.md, benchmark docs, and progress notes to mark pure-Lua\nworkload coverage complete while leaving interop/cached\ncompile/baseline/gate work open\n\n## Validation\n\n- `dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release -v:minimal`\n- `dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release --no-build -- --export-scenarios\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke`\n- `python3 scripts/benchmarks/run-lua-cli-context.py --scenario-dir\nartifacts/benchmarkdotnet/lua-cli-scenarios-smoke --output-root\nartifacts/benchmarkdotnet/comparison-smoke --lua-cmd lua5.4\n--warmup-count 0 --iteration-count 1 --timeout-seconds 30`\n- temporary ignored managed-runtime smoke harness over all 16 exported\nscenarios\n- `python3 tools/test_render_benchmark_deltas.py`\n- `python3 tools/test_run_lua_cli_context.py`\n- `python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py\nscripts/benchmarks/run-lua-cli-context.py\ntools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py`\n- `git diff --check`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh` (14,529 succeeded, 0 failed, 0 skipped)\n- `bash ./scripts/dev/pre-commit.sh`\n- pre-push hook\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Changes are limited to comparison tooling, docs, and benchmark\nscripts; the interpreter runtime is untouched.\n> \n> **Overview**\n> **Phase A0** comparison coverage grows from **5 to 16** pure-Lua\nBenchmarkDotNet scenarios (compute, table-heavy, string-heavy, plus\nexisting smoke cases), with **PLAN.md**, benchmark docs, and session\n**126** notes marking that slice done while interop, cached-compile,\nbaselines, and CI gates stay open.\n> \n> **`BenchmarkScripts`** centralizes the catalog: new Lua 5.1 scripts,\n**`GetScenarioNames` / `GetScenarioName` / `GetScenario`**, and\nshortened parameter names so BenchmarkDotNet and reference **`lua` CLI**\nexport share the same labels. Unknown scenario mappings **throw**\ninstead of silently falling back to Hanoi.\n> \n> **`LuaPerformanceBenchmarks`** drives **`[ParamsSource]`** from that\nlist; **`Program`** exports **`.lua`** files using the stable display\nnames.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n029c03becb97e2ba48a65fee20f426e1d716b95c. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-02T12:46:17-07:00",
          "tree_id": "bd49c5358e92922c973466583ae1ba33ea7cc129",
          "url": "https://github.com/wallstop/NovaSharp/commit/f8b03c4b7817f2df9017751f00e21a85476697b1"
        },
        "date": 1783023288462,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 601.762,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 386.18,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.854,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 696.302,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "21b55bb3a441948689cf3b94b9ad32033e9f9c87",
          "message": "Add Phase A0 interop and cached compile benchmarks (#47)\n\n## Summary\n\n- Add cached-compile comparison rows beside cold compile/execute rows\nfor NovaSharp and third-party managed comparison runtimes.\n- Add Lua-to-CLR and CLR-to-Lua interop benchmark rows with\ndeterministic result assertions.\n- Include interop rows in CI/local comparison benchmark runs, extend\nbenchmark job timeout, and keep reference lua CLI expectations scoped to\npure-Lua scenarios.\n- Update PLAN.md and add\nprogress/session-127-phase-a0-interop-cached-compile.md.\n\n## Validation\n\n- dotnet build\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release -v:minimal\n- dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release --no-build -- --list flat\n- dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release --no-build -- --filter \"*LuaInteropBenchmarks*\" --launchCount\n1 --warmupCount 0 --iterationCount 1 --artifacts\nartifacts/benchmarkdotnet/interopsmoke-postassertions\n- dotnet run --project\nsrc/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj\n-c Release --no-build -- --filter \"*CachedCompile\" --launchCount 1\n--warmupCount 0 --iterationCount 1 --artifacts\nartifacts/benchmarkdotnet/cachedcompile-smoke-all\n- python3 tools/test_render_benchmark_deltas.py\n- python3 tools/test_run_lua_cli_context.py\n- python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py\nscripts/benchmarks/run-lua-cli-context.py\ntools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py\n- ./scripts/build/quick.sh\n- ./scripts/test/quick.sh\n- bash ./scripts/dev/pre-commit.sh\n- git push pre-push hook\n\n## Review Notes\n\n- Three read-only sub-agent review rounds completed. Final round found\nno issues.\n- CI/reviewer feedback still pending after PR creation.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Large workflow and benchmark-matrix changes can break CI artifact\npaths or reporting gates, but changes are confined to tooling and GitHub\nActions with no runtime interpreter behavior changes.\n> \n> **Overview**\n> Extends the Phase A0 comparison scoreboard with **cached-compile**\nrows (warmed script/state reload) for NovaSharp, MoonSharp, NLua, and\nLua-CSharp, and adds **`LuaInteropBenchmarks`** for 1M-call Lua↔CLR\n`add` workloads with deterministic totals so bad bindings fail before\ntiming is trusted. Reference `lua` CLI stays on pure-Lua scenarios only;\ndelta rendering gains a test that interop rows do not count as missing\n`lua` CLI when `--expect-lua-cli` is on.\n> \n> **Benchmark CI** is no longer one long job: separate **runtime**,\n**per-scenario comparison** (16 matrix legs + per-scenario `lua` CLI\ncontext), **interop** (LuaToClr / ClrToLua), and an aggregate\n**`benchmark-report`** job that downloads artifacts, verifies expected\nJSON, renders deltas/PR comments, and fails if any split leg failed. Leg\ntimeouts drop to **10 minutes**; **`JsonExporter.FullCompressed`** is\nwired into both benchmark configs so CI does not depend on `--exporters\njson`. Local `run-benchmarks` scripts run the full comparison assembly\nwith `--filter \"*\"`.\n> \n> `PLAN.md` marks interop and cached-compile complete and documents the\nsplit CI; session **127** and **128** progress notes capture validation.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n4d855d92becc063f7d96bba3ee4e8bb0eb5b072c. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->\n\n---------\n\nCo-authored-by: copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>",
          "timestamp": "2026-07-02T20:18:00-07:00",
          "tree_id": "0489d81c7e5eb6ed2c2439eb02e51580b107b7e5",
          "url": "https://github.com/wallstop/NovaSharp/commit/21b55bb3a441948689cf3b94b9ad32033e9f9c87"
        },
        "date": 1783049008026,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 509.184,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 369.853,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 7.083,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 646.575,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "27be4b4f0129bcda332bfee04a5f78f23202c76f",
          "message": "[codex] Add Phase A0 benchmark scoreboard gates (#48)\n\n## Summary\n\n- extend the benchmark delta renderer with a Phase A0 scoreboard and\nnormalized progress/ baseline support\n- add opt-in Phase A0 gates for NovaSharp/NLua ratio drift and exact\nNovaSharp allocated B/op\n- add dedicated Bash and PowerShell scoreboard commands and wire\nbenchmark CI to enforce gates once the canonical baseline exists\n- update PLAN.md and progress/session-129-phase-a0-scoreboard-gates.md\n\n## Validation\n\n- python3 tools/test_render_benchmark_deltas.py\n- python3 tools/test_run_lua_cli_context.py\n- python3 -m py_compile scripts/benchmarks/render-benchmark-deltas.py\nscripts/benchmarks/run-lua-cli-context.py\ntools/test_render_benchmark_deltas.py tools/test_run_lua_cli_context.py\n- python3 scripts/lint/check-shell-python-invocation.py\n- bash -n scripts/benchmarks/run-phase-a0-scoreboard.sh\nscripts/benchmarks/run-benchmarks.sh\n- PowerShell parser check for run-phase-a0-scoreboard.ps1 and\nrun-benchmarks.ps1\n- scripts/branding/ensure-novasharp-branding.sh\n- actionlint .github/workflows/benchmarks.yml\n- ./scripts/build/quick.sh\n- ./scripts/test/quick.sh\n- bash ./scripts/dev/pre-commit.sh\n\n## Notes\n\nThe canonical progress/benchmarks/phase-a0-scoreboard-baseline.json is\nintentionally not committed in this slice because it should come from a\nrepresentative full scoreboard run, not a one-iteration smoke artifact.\nUntil that file exists, CI reports the Phase A0 gate status but keeps\nenforcement inactive by design.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> CI can fail merges once `phase-a0-scoreboard-baseline.json` exists and\nbenchmarks drift; until then behavior is mostly additive reporting and\ntooling.\n> \n> **Overview**\n> Adds **Phase A0 comparison scoreboard** plumbing:\n`render-benchmark-deltas.py` now emits a compact multi-engine scoreboard\n(time + memory), reads/writes normalized JSON at\n`progress/benchmarks/phase-a0-scoreboard-baseline.json`, and can\n**fail** on `--enforce-phase-gates` when NovaSharp/NLua mean or P95\nratios drift beyond ±10% or NovaSharp allocated B/op differs exactly\nfrom the baseline.\n> \n> New **`run-phase-a0-scoreboard`** Bash/PowerShell scripts run only the\ncomparison suite + optional `lua` CLI context and render the scoreboard\nwithout the full runtime benchmark suite. **Benchmark CI** passes the\nphase baseline path, turns on enforcement when that file exists,\nsurfaces gate metrics in PR comments, warns on missing/empty baselines,\nand **fails the job** on non-zero renderer exit status;\n`progress/benchmarks/**` triggers the workflow.\n> \n> Docs (`PLAN.md`, benchmark READMEs), session notes,\nbranding/pre-commit allowlists, and renderer unit tests are updated. The\ncanonical baseline JSON is **not** committed in this PR—gates stay\nreport-only until a representative run is checked in.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n43a180f5f02bcf3b50a087675a3131575b1d998c. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-03T10:04:51-07:00",
          "tree_id": "16b6cf4aea9aad46838499a820a3611592732ae1",
          "url": "https://github.com/wallstop/NovaSharp/commit/27be4b4f0129bcda332bfee04a5f78f23202c76f"
        },
        "date": 1783098597731,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 570.6,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 364.611,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 6.802,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 757.594,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "75a0d0bd97ae54bc5060af94530a8e551bba7809",
          "message": "Advance Phase A0 baseline and Unity spot check (#49)\n\nSummary:\n- commit the Phase A0 scoreboard baseline JSON from a full five-engine\nlocal run\n- add tracked Unity UPM samples, including an IL2CPP stopwatch\nspot-check scene\n- make Unity package builders copy tracked samples and reject output\npaths overlapping source templates\n- adjust audits/branding allowlists for UPM sample templates and\nbenchmark baseline data\n\nLocal validation:\n- ./scripts/benchmarks/run-phase-a0-scoreboard.sh --write-phase-baseline\nprogress/benchmarks/phase-a0-scoreboard-baseline.json\n- python3 scripts/benchmarks/render-benchmark-deltas.py --current-root\nartifacts/benchmarkdotnet/phase-a0-comparison --comparison-root\nartifacts/benchmarkdotnet/phase-a0-comparison --phase-baseline\nprogress/benchmarks/phase-a0-scoreboard-baseline.json --output\nartifacts/phase-a0-scoreboard-enforced.md --expect-lua-cli\n--enforce-phase-gates\n- ./scripts/packaging/build-unity-package.sh --version 3.0.0-dev\n--output artifacts/unity-spotcheck-validation-current\n- pwsh -NoProfile -File scripts/packaging/build-unity-package.ps1\n-Version 3.0.0-dev -OutputPath\nartifacts/unity-spotcheck-validation-ps-current\n- bash ./scripts/dev/pre-commit.sh\n- ./scripts/build/quick.sh\n- ./scripts/test/quick.sh\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Changes affect CI benchmark gating and Unity package generation rather\nthan the core interpreter, but incorrect gate logic or baseline drift\ncould block merges or mis-signal performance regressions.\n> \n> **Overview**\n> Adds a **checked-in Phase A0 scoreboard baseline**\n(`progress/benchmarks/phase-a0-scoreboard-baseline.json`) sourced from\nCI runner artifacts so aggregate benchmark gates match hosted\nenvironments.\n> \n> **Phase A0 CI gates** in `render-benchmark-deltas.py` now fail only on\n**regressions**: NovaSharp/NLua ratio checks use a 100% catastrophic\nthreshold (improvements and small noise pass), and NovaSharp B/op gates\nallow decreases plus runner noise for larger rows while staying exact\nunder 1 KiB. Tests and benchmark docs reflect the new semantics.\n> \n> **Benchmark workflow** normalizes manual alert thresholds (`115` vs\n`115%`), keeps gh-pages historical storage, but **disables\n`comment-on-alert` on pull requests** so PR feedback comes from the\naggregate delta comment and Phase A0 gates instead of noisy historical\ncomparisons.\n> \n> **Unity packaging** moves samples into tracked `Samples~` templates\n(Basic Usage + **IL2CPP spot-check** scene/runner with single-line\n`NOVASHARP_IL2CPP_SPOTCHECK` pass/fail logs). Bash/PowerShell builders\n**copy** those templates, reject output paths overlapping package/sample\nsources, and the Bash script requires `python3` for portable path\nchecks. `.gitignore`, namespace audit skips, and branding allowlists\naccommodate UPM `Samples~` and the baseline JSON.\n> \n> **PLAN** and **UnityIntegration** docs mark Phase A0 baseline/IL2CPP\nspot-check items complete and describe how to run the sample.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n53b00c5f737aea28fc8755bba8c2bdaac19b2e59. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-03T14:09:43-07:00",
          "tree_id": "dc16aaf42315e999a864f36beb9522b04c482984",
          "url": "https://github.com/wallstop/NovaSharp/commit/75a0d0bd97ae54bc5060af94530a8e551bba7809"
        },
        "date": 1783113294290,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"CoroutinePipeline\")",
            "value": 499.707,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"NumericLoops\")",
            "value": 363.184,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"TableMutation\")",
            "value": 6.938,
            "unit": "μs",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarks.ExecuteScenario(ScenarioName: \"UserDataInterop\")",
            "value": 673.474,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "wallstop@wallstopstudios.com",
            "name": "Eli Pinkerton",
            "username": "wallstop"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "43fa9538db309e67c590e96c5ebc338478608583",
          "message": "[codex] Add B0 facade first slice (#50)\n\n## Summary\n- Add the first root `NovaSharp` B0 facade over the current VM:\nengine/value/table/function/chunk/coroutine wrappers plus root\noption/provider types.\n- Add a checked-in `PublicAPI.Shipped.txt` baseline with\nreflection-backed TUnit enforcement and focused facade smoke coverage.\n- Update PLAN/progress notes and namespace audit allowlist for the\nintentional root facade namespace.\n\n## Local validation\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` (30\npassed)\n- `./scripts/test/quick.sh` (14,559 passed)\n- `bash ./scripts/dev/pre-commit.sh` (existing LLM skill metadata\nwarnings only)\n- push hook pre-push checks (formatting, Markdown, branding, namespace,\ntooling, YAML/actions lint, build)\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> New public API and VM value identity/hash behavior change host-facing\nsemantics; changes are wrapper-heavy with broad test coverage and no\nsecurity-critical paths.\n> \n> **Overview**\n> Introduces the **Phase B0** root **`NovaSharp`** facade over the\nexisting VM: **`LuaEngine`**, **`LuaValue`**,\ntables/functions/chunks/coroutines, options/sandbox types, and a\n**`LuaException`** hierarchy that maps interpreter failures at public\nboundaries. A checked-in **`PublicAPI.Shipped.txt`** is enforced by\nTUnit (core type budget under 40). Runnable **B0 samples**,\n**BenchmarkDotNet** facade-vs-`Script` overhead cases, and\nsmoke/exception tests cover the new surface; call paths were tuned so\n**`Run`/`Call` stay within the planned ~5% overhead vs **`Script`**.\n> \n> **A1a prep** removes stored **`DynValue.ReferenceId`** and the mutable\nhash cache, adds **lazy stable userdata hashing** for table keys, gives\n**`debug.upvalueid`** its own debug handle IDs, and updates the VS Code\nvariable inspector to derive wrapper identity without the old field.\n**`PLAN.md`** is expanded with a 2026 research audit (A4.5, sandbox\nthreat model, calibrated A8 estimates), marks B0 complete, and documents\nsession progress.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n114c505a7f4352e4cf48c2a7e6c9802fcc172783. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->\n\n---------\n\nCo-authored-by: copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>",
          "timestamp": "2026-07-04T09:37:03-07:00",
          "tree_id": "db65410494575aa22ead31695a3378c6b7251fef",
          "url": "https://github.com/wallstop/NovaSharp/commit/43fa9538db309e67c590e96c5ebc338478608583"
        },
        "date": 1783183351703,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 167.456,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 176.427,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 173.912,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 276.002,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 282.885,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 277.661,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 348.463,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 342.607,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 364.523,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 377.615,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 381.08,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 374.206,
            "unit": "ns",
            "extra": ""
          }
        ]
      }
    ]
  }
}
window.BENCHMARK_DATA = {
  "lastUpdate": 1783306742208,
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
          "id": "94547c0bbb1835a4da633dfd15701eaf041c17bc",
          "message": "Harden tuple fixtures and table constructor borders (#51)\n\n## Summary\n\n- Complete PLAN.md A1b fixture hardening for nil/tuple arity drift\nhazards before the LuaValue struct conversion.\n- Add standalone Lua fixtures with assertions for `select('#', ...)`,\nnon-final/scalarized function calls, expanded nil tuples, and\n`table.pack(...).n`.\n- Fix and cover version-specific constructor-created holey table `#`\nbehavior, including cached/same-slot writes and Lua 5.4 absent-key nil\nno-op behavior.\n- Restore script ownership validation for array table constructor\nfields.\n\n## Validation\n\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh --full\nSelectHashCountsExpandedNilReturnValues`\n- `./scripts/test/quick.sh\nFunctionCallExpressionPositionsAdjustReturnArity`\n- `./scripts/test/quick.sh PackPreservesExpandedNilAndReportsCount`\n- `./scripts/test/quick.sh --full\nTableLengthFollowsVersionedConstructorBorders`\n- `./scripts/test/quick.sh ArrayConstructorRejectsForeignScriptResource`\n- `./scripts/test/quick.sh -c TableTUnitTests`\n- `./scripts/test/quick.sh -c TableModuleTUnitTests`\n- `./scripts/test/quick.sh -c SimpleTUnitTests`\n- `./scripts/test/quick.sh` (14,725 tests, 0 failures)\n- Full Lua fixture comparison with `--enforce` for Lua 5.1, 5.2, 5.3,\n5.4, and 5.5: 0 mismatches, 0 missing outputs\n- `bash ./scripts/dev/pre-commit.sh`\n\nPre-commit reported existing documentation and skill metadata warnings\nonly.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Touches core table length semantics and hot `PerformTableSet` paths\nused by every table write; broad matrix tests and full fixture\ncomparison mitigate but constructor-border edge cases remain until the\nplanned A4 table rewrite.\n> \n> **Overview**\n> Completes **PLAN A1b** pre-`LuaValue` struct regression coverage for\nnil/tuple arity (`select('#', ...)`, expanded nil returns, expression vs\nstatement call sites, `table.pack(...).n`) via TUnit tests and\nstandalone Lua fixtures checked against reference Lua 5.1–5.5.\n> \n> **Table runtime:** Adds constructor-time tracking\n(`_constructorArrayLength`, `InitNextKey` / `InitNextArrayKeys`) so `#`\non holey tables built with `{ ... }` matches Lua 5.1–5.3 binary-search\nborders, 5.4 highest-set-index behavior, and 5.5 prefix length; table\nctor bytecode now uses `InitNextKey` instead of generic `Set`.\n**PerformTableSet** gains constructor vs post-construction mutation\nrules (same-slot overwrites, Lua 5.4 absent-key nil no-ops). **Clear**,\n**Remove**, and **CollectDeadKeys** reset constructor hints and fix\nallocation tracking / safe iteration over nil tombstones.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n57b86719e6f34c2beae8cf45b234cb4d712fb054. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-04T15:52:28-07:00",
          "tree_id": "ad72d888a9a30fb61357e2eb9926b8988cabac76",
          "url": "https://github.com/wallstop/NovaSharp/commit/94547c0bbb1835a4da633dfd15701eaf041c17bc"
        },
        "date": 1783205944673,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 161.208,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 171.606,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 173.255,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 282.836,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 286.415,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 284.017,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 316.239,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 332.061,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 328.574,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 354.623,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 389.605,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 362.671,
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
          "id": "d0d75f49eecf1db653dd4544243b9082c263359d",
          "message": "[codex] Harden bytecode literal operands (#52)\n\n## Summary\n\n- Harden A1a bytecode literal boundaries so instruction `DynValue`\noperands are stored as read-only snapshots.\n- Cover direct literal construction, bytecode\nliteral/index/index-set/meta/global operands, and binary chunk\ndeserialized literals.\n- Add a standalone all-version Lua fixture for repeated literal and\nliteral-index reuse, and update `PLAN.md` plus the session log.\n\n## Validation\n\n- Reference Lua: new fixture passed on Lua 5.1, 5.2, 5.3, 5.4, and 5.5.\n- `./scripts/test/quick.sh --full -c ByteCodeTUnitTests` passed: 115\ntests, 0 failures.\n- `./scripts/test/quick.sh -c LiteralExpressionTUnitTests` passed: 6\ntests, 0 failures.\n- `./scripts/test/quick.sh -c ProcessorBinaryDumpTUnitTests` passed: 55\ntests, 0 failures.\n- `./scripts/build/quick.sh` passed.\n- `./scripts/test/quick.sh` passed: 14,826 tests, 0 failures.\n- `bash ./scripts/dev/pre-commit.sh` completed successfully; existing\ndocumentation and skill metadata warnings remain.\n- Pre-push hook passed, including CSharpier, Markdown, branding,\nnamespace, tooling, YAML/Actions lint, and quick build.\n\n## Notes\n\nScoped comparison runner execution passed the new fixture on both\nreference Lua and NovaSharp for Lua 5.1-5.5, but `compare-lua-outputs.py\n--enforce` reported one-sided artifact keys for the manual scoped\nfixture runs. I am not counting that as a green comparison check.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Defensive immutability at bytecode boundaries with broad test\ncoverage; no change to Lua semantics or hot-path execution logic.\n> \n> **Overview**\n> Advances **Phase A1a** by stopping mutable `DynValue` wrappers from\nbeing aliased inside the instruction stream—constants are frozen at\ncompile, emit, and load boundaries before the planned `LuaValue` struct\nconversion.\n> \n> **Bytecode emission** now stores `Instruction.Value` via\n`AsReadOnly()` for literals, meta payloads, global name strings, and\nindex/index-set operands; `EmitLiteral` rejects null.\n> \n> **AST and binary chunks**: `LiteralExpression` snapshots caller-owned\nvalues as read-only; chunk deserialization returns read-only\nnil/boolean/number/string/table literals (numeric dumps use cached\nfactory paths first).\n> \n> **Tests**: TUnit mutation-after-emission checks, undump read-only\nliteral coverage (including nil), and an all-version Lua fixture for\nrepeated literal reuse. `PLAN.md` and session 149 notes document the\nwork.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n4d2ec37cdfff10366aee3dcd30e748d62334edb7. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-04T17:25:57-07:00",
          "tree_id": "3d56d9e53af271e2e706947c1f8f13974a664f48",
          "url": "https://github.com/wallstop/NovaSharp/commit/d0d75f49eecf1db653dd4544243b9082c263359d"
        },
        "date": 1783211486329,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 132.572,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 132.984,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 134.512,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 213.388,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 213.368,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 220.103,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 258.708,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 260.351,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 257.494,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 280.584,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 288.868,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 295.78,
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
          "id": "25cdc8316528d76ea2e786ff4826ed4307c2806c",
          "message": "[codex] Add source generator interop attributes (#53)\n\n## Summary\n\n- Add the public B1 source-generator attribute contract in the root\n`NovaSharp` API: `LuaObjectAttribute`, `LuaMemberAttribute`,\n`LuaMetamethodAttribute`, `LuaMetamethodKind`, and `LuaIgnoreAttribute`.\n- Pin the new surface in `PublicAPI.Shipped.txt` and add reflection\nsmoke coverage for metadata, attribute targets, invalid names, and\nmultiple metamethod annotations.\n- Update `PLAN.md`, the session progress log, and the naming audit\nnamespace count.\n\n## Validation\n\n- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`\n- `bash ./scripts/dev/pre-commit.sh`\n- `git push` pre-push checks: CSharpier, Markdown format, branding,\nnamespace alignment, tooling setup, YAML/actionlint, interpreter build\n\n## Notes\n\nThis is intentionally limited to public metadata. Generator output,\nanalyzer diagnostics, enum table exposure, and stub emission remain open\nB1 work.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Touches many internal VM slot-mutation call sites (intended no Lua\nbehavior change) and expands the stable public facade API;\ngenerator/analyzer behavior is still unimplemented.\n> \n> **Overview**\n> Adds the **Phase B1** public source-generator contract on the root\n`NovaSharp` API: `LuaObjectAttribute`, `LuaMemberAttribute`,\n`LuaMetamethodAttribute`, `LuaMetamethodKind` (explicit numeric values),\nand `LuaIgnoreAttribute`, with entries in `PublicAPI.Shipped.txt` and\nfacade smoke tests for metadata, targets, invalid names, and enum\nstability. Review follow-ups harden those tests (`AttributeUsage`\nchecks, sorted metamethod assertions).\n> \n> In parallel, **A1a prep** renames internal whole-slot `DynValue`\nmutation from `Assign(...)` to **`AssignSlot(...)`** and updates VM,\nCoreLib (`debug`, `setfenv`), and closure upvalue paths so mutable\nlocal/upvalue slots are clearly separated from read-only literals and\ntable keys; docs and session logs reflect the boundary. **No generator\nor analyzer implementation yet**—metadata and slot API only.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n1578f1d6e7109d7e2e7f579c958bc54dd7b22fce. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-04T19:17:46-07:00",
          "tree_id": "db7343560faa54822b81fb21e889395aaf4befaf",
          "url": "https://github.com/wallstop/NovaSharp/commit/25cdc8316528d76ea2e786ff4826ed4307c2806c"
        },
        "date": 1783218197057,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 220.372,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 169.646,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 172.773,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 275.108,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 278.02,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 275.146,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 324.271,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 331.516,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 332.374,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 364.913,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 369.613,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 406.542,
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
          "id": "63a6ba8024e2a5274d57bdcfca3f09f63079ef9e",
          "message": "[codex] Add interop analyzer diagnostics (#54)\n\n## Summary\n\n- Add the `WallstopStudios.NovaSharp.Interop.Generator` analyzer package\nunder `src/interop`.\n- Implement B1 analyzer diagnostics `NS0001` through `NS0007` for the\ngenerated interop attribute contract.\n- Add focused Roslyn/TUnit coverage for valid bindings and each\ndiagnostic, and keep `PLAN.md`/progress tracking current.\n\n## Validation\n\n- `dotnet build\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n--no-restore`\n- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests`\n- `dotnet pack\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n-c Release --no-restore`\n- `unzip -l\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/bin/Release/WallstopStudios.NovaSharp.Interop.Generator.3.0.0.nupkg`\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh --full`\n- `bash ./scripts/dev/pre-commit.sh`\n- pre-push hook during `git push -u origin dev/wallstop/plan-12`\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> New analyzer-only package with broad TUnit coverage; it is not yet\nwired as a live analyzer on runtime projects, so consumer-facing compile\nbehavior is unchanged until integration.\n> \n> **Overview**\n> Introduces **`WallstopStudios.NovaSharp.Interop.Generator`**, a\npackable Roslyn analyzer under `src/interop/` that validates the planned\ngenerated Lua interop contract **before** source generation exists.\n**`LuaInteropDiagnosticAnalyzer`** emits **`NS0001`–`NS0007`**: partial\n`[LuaObject]`, unsupported types/signatures, duplicate Lua names, async\nreturns needing a future adapter, interop attributes outside a LuaObject\ntype, and invalid attribute arguments. Behavior covers aliased\nattributes, accessor-level attributes, `[LuaIgnore]` opt-out, and\ncontinued validation after partial failures.\n> \n> The analyzer is added to **`NovaSharp.sln`**, referenced from\ninterpreter TUnit tests via **Microsoft.CodeAnalysis.CSharp 4.12**, and\nshipped in a NuGet layout under `analyzers/dotnet/cs`. **`PLAN.md`**\nmarks analyzer diagnostics done and splits remaining B1 work (generator\noutput, golden tests, code fixes). Tooling updates include **`interop`**\nin the namespace audit and session progress notes.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\nb8a09b48ce6ec0fa560e0cbbb1cc5327e96bcceb. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-05T09:48:14-07:00",
          "tree_id": "ff88d1daa9f6ee606f5d9182fb4850aa21c1ec01",
          "url": "https://github.com/wallstop/NovaSharp/commit/63a6ba8024e2a5274d57bdcfca3f09f63079ef9e"
        },
        "date": 1783270409678,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 169.412,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 173.479,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 171.426,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 310.658,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 281.039,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 283.522,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 321.368,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 326.861,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 332.418,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 376.04,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 388.758,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 363.368,
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
          "id": "5f2e1f3f57f40f59c1a94ed5cb66a83ccdc0fce8",
          "message": "[codex] Avoid fixed argument wrapper clones (#55)\n\n## Summary\n\n- remove the redundant writable clone from fixed Lua function parameter\nbinding now that `AssignSlot` copies value fields into owned local slots\n- keep vararg capture cloning and add guard coverage for escaped vararg\ntuples/tables\n- log the A1a session and update `PLAN.md`\n\n## Validation\n\n- `./scripts/test/quick.sh --full -c DynValueTUnitTests`\n- `./scripts/test/quick.sh --full -c ScriptCallTUnitTests`\n- reference Lua 5.1-5.5 fixture run for\n`ArgumentRebindingAndVarargCaptureKeepScalarValues.lua`\n- NovaSharp CLI Lua 5.1-5.5 fixture run for\n`ArgumentRebindingAndVarargCaptureKeepScalarValues.lua`\n- scoped `run-lua-fixtures-fast.sh` over `ScriptCallTUnitTests` for Lua\n5.1-5.5\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh`\n- `bash ./scripts/dev/pre-commit.sh`\n- pre-push hook build/format/lint checks\n\n## Notes\n\n`compare-lua-outputs.py --enforce` was attempted against scoped\n`ScriptCallTUnitTests` outputs, but the comparator reported one-sided\nkeys because the narrowed fixture directory made reference-Lua and\nNovaSharp batch outputs use different relative paths while still\napplying full-corpus ratchet data. The selected fixtures themselves\npassed under reference Lua and NovaSharp across Lua 5.1-5.5.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Fixed-parameter binding is on the hot call path and must stay\nLua-correct; tests and fixtures mitigate that. The relaxed ratio gate\ncould allow CI green when only the NLua denominator shifts, though\nNovaSharp B/op gates and self-timing checks still apply.\n> \n> **Overview**\n> **A1a call binding** stops cloning fixed parameters in `ExecArgs`:\nlocals now bind via `ToScalar()` alone because `AssignSlot` already\ncopies into owned slots, while **vararg capture still uses\n`CloneAsWritable()`** so escaped tuples/`table.pack` cannot alias caller\nwrappers.\n> \n> **Coverage** adds TUnit and a standalone Lua fixture for fixed-arg\nrebinding, vararg scalar snapshots, table sharing, and read-only\n`AssignSlot` behavior; one test uses `FromInteger(1)` for the fixed-arg\ncase.\n> \n> **Phase A0 benchmark CI** changes the NovaSharp/NLua mean and P95\nratio gate so it **blocks only when NovaSharp’s own timing also crosses\nthe catastrophic threshold**, fixing denominator-only failures when NLua\nimproves on the same run (e.g. PR #55 `StringFormat` compile). Docs,\n`PLAN.md`, and session notes reflect both tracks.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n3ea2573d7d939c4ec777975aba88f22e147cc56e. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-05T11:13:28-07:00",
          "tree_id": "49b1d89e336850ce97e74003359108895c2ef363",
          "url": "https://github.com/wallstop/NovaSharp/commit/5f2e1f3f57f40f59c1a94ed5cb66a83ccdc0fce8"
        },
        "date": 1783275543173,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 175.712,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 177.691,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 178.621,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 274.954,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 290.738,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 281.362,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 321.123,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 356.477,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 316.671,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 351.434,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 374.272,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 366.698,
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
          "id": "77f0a1f86b0d90ebac35a5316821d12e0beeb937",
          "message": "[codex] Add generator golden tests (#57)\n\n## Summary\n\n- adds the first `LuaInteropSourceGenerator` incremental generator entry\npoint\n- emits deterministic private companion partial output for valid\ntop-level `[LuaObject]` class and struct inputs\n- adds golden-source generator tests for class and struct output plus a\nnon-partial invalid-shape skip case\n- updates B1 progress tracking and the naming audit snapshot\n\n## Validation\n\n- `dotnet build\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n--no-restore`\n- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests`\n- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests`\n- `./scripts/build/quick.sh`\n- `dotnet pack\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n-c Release --no-restore`\n- `git diff --check`\n- `./scripts/test/quick.sh`\n- `bash ./scripts/dev/pre-commit.sh`\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Build-time generator and test-only harness; generated dispatch is\nprivate and returns Nil with no runtime wiring yet.\n> \n> **Overview**\n> Introduces the first **B1 source generator** slice:\n`LuaInteropSourceGenerator` emits a deterministic companion partial for\ntop-level, partial `[LuaObject]` types (class, struct, record, record\nstruct).\n> \n> Generated output is intentionally **inert** until runtime binding\nlands: a private `ReadOnlySpan<LuaValue>` string-switch dispatch stub, a\nprivate manifest string (Lua name, CLR type, sorted member names,\nreferenced enums), and **`[LuaIgnore]`** filtering. Non-partial or\nnested/generic types produce no output (aligned with analyzer\n**NS0001**).\n> \n> **Golden-file tests** drive the generator via `CSharpGeneratorDriver`,\ncompile the result, and compare against checked-in `.g.cs.txt` fixtures;\na negative test asserts no output for non-partial types. The test\nproject copies `GoldenSources` to output and references the generator\nproject.\n> \n> Also updates **PLAN.md** B1 progress (golden tests checked off),\npackage description (analyzer + generator), **naming/documentation\naudit** regex for `record struct`, and adds session **160** progress\nnotes.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n94a2d1d07426ea4fd8ef131b13ab5f0258b2f86b. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-05T13:02:25-07:00",
          "tree_id": "8d276ccd4d93b07490766f3b2067e98283ec7e77",
          "url": "https://github.com/wallstop/NovaSharp/commit/77f0a1f86b0d90ebac35a5316821d12e0beeb937"
        },
        "date": 1783282137558,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 162.445,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 171.685,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 168.832,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 270.551,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 276.577,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 286.372,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 306.699,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 325.254,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 376.376,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 343.715,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 420.635,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 356.018,
            "unit": "ns",
            "extra": ""
          }
        ]
      },
      {
        "commit": {
          "author": {
            "email": "49699333+dependabot[bot]@users.noreply.github.com",
            "name": "dependabot[bot]",
            "username": "dependabot[bot]"
          },
          "committer": {
            "email": "noreply@github.com",
            "name": "GitHub",
            "username": "web-flow"
          },
          "distinct": true,
          "id": "9c26297142a5bf8136c816fd3cb7b10ae971775c",
          "message": "Bump codespell from 2.4.1 to 2.4.2 (#42)\n\nBumps [codespell](https://github.com/codespell-project/codespell) from\n2.4.1 to 2.4.2.\n<details>\n<summary>Release notes</summary>\n<p><em>Sourced from <a\nhref=\"https://github.com/codespell-project/codespell/releases\">codespell's\nreleases</a>.</em></p>\n<blockquote>\n<h2>v2.4.2</h2>\n<!-- raw HTML omitted -->\n<h2>Highlights</h2>\n<ul>\n<li>Fixed compatibility with chardet 7+</li>\n</ul>\n<h2>What's Changed</h2>\n<ul>\n<li>Fix and clarify cases in ignore patterns by <a\nhref=\"https://github.com/DanielYang59\"><code>@​DanielYang59</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3583\">codespell-project/codespell#3583</a></li>\n<li>codespell-private.yml: Do not codespell digital signature files by\n<a href=\"https://github.com/cclauss\"><code>@​cclauss</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3623\">codespell-project/codespell#3623</a></li>\n<li>[pre-commit.ci] pre-commit autoupdate by <a\nhref=\"https://github.com/pre-commit-ci\"><code>@​pre-commit-ci</code></a>[bot]\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3634\">codespell-project/codespell#3634</a></li>\n<li>numbes-&gt;numbers and numbesr-&gt;numbers by <a\nhref=\"https://github.com/skshetry\"><code>@​skshetry</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3635\">codespell-project/codespell#3635</a></li>\n<li>Add spelling corrections for disclose and variables. by <a\nhref=\"https://github.com/cfi-gb\"><code>@​cfi-gb</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3622\">codespell-project/codespell#3622</a></li>\n<li>Add spelling correction for Vulnererability and variants. by <a\nhref=\"https://github.com/cfi-gb\"><code>@​cfi-gb</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3625\">codespell-project/codespell#3625</a></li>\n<li>Remove lets-&gt;let's by <a\nhref=\"https://github.com/Piedone\"><code>@​Piedone</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3633\">codespell-project/codespell#3633</a></li>\n<li>Add corrections for &quot;dictate&quot; by <a\nhref=\"https://github.com/jdufresne\"><code>@​jdufresne</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3636\">codespell-project/codespell#3636</a></li>\n<li>Add specicification (and pl) typo by <a\nhref=\"https://github.com/yarikoptic\"><code>@​yarikoptic</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3639\">codespell-project/codespell#3639</a></li>\n<li>Remove &quot;blueish&quot; correction by <a\nhref=\"https://github.com/hadess\"><code>@​hadess</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3510\">codespell-project/codespell#3510</a></li>\n<li>Add &quot;lighting&quot; as an option to fix &quot;lighning&quot; by\n<a href=\"https://github.com/yarikoptic\"><code>@​yarikoptic</code></a> in\n<a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3648\">codespell-project/codespell#3648</a></li>\n<li>Revert adding <code>lien</code> to the rare dictionary by <a\nhref=\"https://github.com/nikolaik\"><code>@​nikolaik</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3631\">codespell-project/codespell#3631</a></li>\n<li>&quot;ane&quot; could have been &quot;one&quot; by <a\nhref=\"https://github.com/yarikoptic\"><code>@​yarikoptic</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3645\">codespell-project/codespell#3645</a></li>\n<li>Add spelling correction for &quot;priort&quot; by <a\nhref=\"https://github.com/cfi-gb\"><code>@​cfi-gb</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3647\">codespell-project/codespell#3647</a></li>\n<li>Remove &quot;fix&quot; of &quot;deques&quot; - it is quite legit by\n<a href=\"https://github.com/yarikoptic\"><code>@​yarikoptic</code></a> in\n<a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3649\">codespell-project/codespell#3649</a></li>\n<li>Several new suggestions by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3621\">codespell-project/codespell#3621</a></li>\n<li>Add proposal constraints to containts by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3652\">codespell-project/codespell#3652</a></li>\n<li>Additions dleay,infp,practive,utiliy by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3643\">codespell-project/codespell#3643</a></li>\n<li>Add calncelled and its variations by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3650\">codespell-project/codespell#3650</a></li>\n<li>Use raw strings for regex by <a\nhref=\"https://github.com/DimitriPapadopoulos\"><code>@​DimitriPapadopoulos</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3654\">codespell-project/codespell#3654</a></li>\n<li>Allow multiple spaces before codespell:ignore by <a\nhref=\"https://github.com/DimitriPapadopoulos\"><code>@​DimitriPapadopoulos</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3653\">codespell-project/codespell#3653</a></li>\n<li>Added correction from <code>timeour</code> to <code>timeout</code>\nby <a href=\"https://github.com/jamesbraza\"><code>@​jamesbraza</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3656\">codespell-project/codespell#3656</a></li>\n<li>Add typos found in various software projects by <a\nhref=\"https://github.com/luzpaz\"><code>@​luzpaz</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3640\">codespell-project/codespell#3640</a></li>\n<li>[pre-commit.ci] pre-commit autoupdate by <a\nhref=\"https://github.com/pre-commit-ci\"><code>@​pre-commit-ci</code></a>[bot]\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3659\">codespell-project/codespell#3659</a></li>\n<li>Add codespell suggestions for enabke and friends by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3657\">codespell-project/codespell#3657</a></li>\n<li>END: add &quot;queues&quot; (plural from queue) as possible fix for\nques by <a\nhref=\"https://github.com/yarikoptic\"><code>@​yarikoptic</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3591\">codespell-project/codespell#3591</a></li>\n<li>agreegate, lesda, realod, colouer by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3665\">codespell-project/codespell#3665</a></li>\n<li>Update pre-commit version in documentation by <a\nhref=\"https://github.com/prchoward\"><code>@​prchoward</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3666\">codespell-project/codespell#3666</a></li>\n<li>MAINT: Rename CI file and run name by <a\nhref=\"https://github.com/larsoner\"><code>@​larsoner</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3667\">codespell-project/codespell#3667</a></li>\n<li>preoccuption-&gt;preoccupation; occuption-&gt;occupation by <a\nhref=\"https://github.com/TheGiraffe3\"><code>@​TheGiraffe3</code></a> in\n<a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3668\">codespell-project/codespell#3668</a></li>\n<li>Suggestions for: checkto, diminsion, waitfor by <a\nhref=\"https://github.com/mdeweerd\"><code>@​mdeweerd</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3670\">codespell-project/codespell#3670</a></li>\n<li>Typos found in sigstore-python by <a\nhref=\"https://github.com/DimitriPapadopoulos\"><code>@​DimitriPapadopoulos</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3664\">codespell-project/codespell#3664</a></li>\n<li>usgin-&gt;using by <a\nhref=\"https://github.com/ydah\"><code>@​ydah</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3672\">codespell-project/codespell#3672</a></li>\n<li>Add typos found in various software projects by <a\nhref=\"https://github.com/luzpaz\"><code>@​luzpaz</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3669\">codespell-project/codespell#3669</a></li>\n<li>Add coered -&gt; coerced by <a\nhref=\"https://github.com/effigies\"><code>@​effigies</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3680\">codespell-project/codespell#3680</a></li>\n<li>backwward(s)-&gt;backward(s), onwward(s)-&gt;onward(s) by <a\nhref=\"https://github.com/cjwatson\"><code>@​cjwatson</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3682\">codespell-project/codespell#3682</a></li>\n<li>[pre-commit.ci] pre-commit autoupdate by <a\nhref=\"https://github.com/pre-commit-ci\"><code>@​pre-commit-ci</code></a>[bot]\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3685\">codespell-project/codespell#3685</a></li>\n<li>telemetery-&gt;telemetry by <a\nhref=\"https://github.com/august-soderberg\"><code>@​august-soderberg</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3686\">codespell-project/codespell#3686</a></li>\n<li>Add hexedacimal and similar typos by <a\nhref=\"https://github.com/Akuli\"><code>@​Akuli</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3692\">codespell-project/codespell#3692</a></li>\n<li>Add rounted-&gt;routed, rounded and friends by <a\nhref=\"https://github.com/peternewman\"><code>@​peternewman</code></a> in\n<a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3693\">codespell-project/codespell#3693</a></li>\n<li>Add symmectric and similar typos by <a\nhref=\"https://github.com/Akuli\"><code>@​Akuli</code></a> in <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3694\">codespell-project/codespell#3694</a></li>\n<li>Fix CI on Windows: pip upgrade pip by <a\nhref=\"https://github.com/DimitriPapadopoulos\"><code>@​DimitriPapadopoulos</code></a>\nin <a\nhref=\"https://redirect.github.com/codespell-project/codespell/pull/3698\">codespell-project/codespell#3698</a></li>\n</ul>\n<!-- raw HTML omitted -->\n</blockquote>\n<p>... (truncated)</p>\n</details>\n<details>\n<summary>Commits</summary>\n<ul>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/2ccb47ff45ad361a21071a7eedda4c37e6ae8c5a\"><code>2ccb47f</code></a>\nCompat with chardet 7 (<a\nhref=\"https://redirect.github.com/codespell-project/codespell/issues/3886\">#3886</a>)</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/4ec53bf6a3e510c64900d5ee838abd99d49b2910\"><code>4ec53bf</code></a>\n[pre-commit.ci] pre-commit autoupdate</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/2a4acba3f282f1b5ccb7ad8b57bc991810663a44\"><code>2a4acba</code></a>\nBump actions/download-artifact from 7 to 8</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/be17cacc96a5ee3f014e048f5962cfdb7145e096\"><code>be17cac</code></a>\nBump actions/upload-artifact from 6 to 7</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/04a071280d56148cab14249ccc8d4181c0066b3c\"><code>04a0712</code></a>\nBump ruff (<a\nhref=\"https://redirect.github.com/codespell-project/codespell/issues/3879\">#3879</a>)</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/583d8796d92eb58e15072db03e5b756be45f638a\"><code>583d879</code></a>\navoide-&gt;avoid, avoided, avoids,</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/1f59f34d7c6d1642fdb325d9dfa49cf9eb5f692a\"><code>1f59f34</code></a>\nAdd correction for 'foudation' to 'foundation'</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/e047fdafb8620b08a86349014487886bcd9c2205\"><code>e047fda</code></a>\nAdd spelling correction for gather and variants.</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/b5cd66de14b8f65b0f45fabbe1c89bd69ea60939\"><code>b5cd66d</code></a>\nrespondant-&gt;respondent</li>\n<li><a\nhref=\"https://github.com/codespell-project/codespell/commit/92125a3814fa6e86cd2055385916ce5186d3e5df\"><code>92125a3</code></a>\nAdd detection of ivoice and variants.</li>\n<li>Additional commits viewable in <a\nhref=\"https://github.com/codespell-project/codespell/compare/v2.4.1...v2.4.2\">compare\nview</a></li>\n</ul>\n</details>\n<br />\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Single dev-dependency version bump with no application runtime impact.\n> \n> **Overview**\n> Updates the pinned **codespell** version in `requirements.tooling.txt`\nfrom **2.4.1** to **2.4.2** for dev/tooling spell-check usage.\n> \n> Upstream **2.4.2** includes chardet 7+ compatibility fixes, dictionary\nand ignore-pattern tweaks, and minor CLI behavior improvements; this\nrepo change is the dependency pin only.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n294d96021093a573d50092efecfd6895653658f0. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->\n\nSigned-off-by: dependabot[bot] <support@github.com>\nCo-authored-by: dependabot[bot] <49699333+dependabot[bot]@users.noreply.github.com>",
          "timestamp": "2026-07-05T13:46:43-07:00",
          "tree_id": "1b091d75c27ba59dc34795ea912473d6faa21e86",
          "url": "https://github.com/wallstop/NovaSharp/commit/9c26297142a5bf8136c816fd3cb7b10ae971775c"
        },
        "date": 1783284741425,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 163.635,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 184.443,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 176.455,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 260.054,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 271.462,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 281.001,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 309.192,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 333.143,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 324.864,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 330.875,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 347.018,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 343.874,
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
          "id": "52ba6cc4b7bcf3a8e6aad867100854e0ebb45b5c",
          "message": "[codex] Add generated enum table helpers (#58)\n\n## Summary\n\n- Add generated enum table registration helpers for `[LuaObject]`\ncompanion partials when exposed members reference enum types.\n- Emit facade `LuaTable` constants for signed and unsigned enum values,\nwhile skipping `[LuaIgnore]` enum types and enum members.\n- Disambiguate enum table keys when simple enum names collide with each\nother or with exposed member names.\n- Extend generator tests to compile and invoke generated helpers,\nincluding adversarial collision and ignore cases.\n\n## Validation\n\n- `dotnet build\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n--no-restore`\n- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` (10\npassed)\n- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` (30\npassed)\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh` (14,887 passed)\n- `git diff --cached --check`\n- `bash ./scripts/dev/pre-commit.sh` (completed with existing LLM skill\nmetadata warnings)\n- pre-push checks completed successfully\n\n## Residual Risk\n\n- The enum-table helper is generated and tested, but no runtime adapter\ncalls it yet; B1 enum auto-exposure remains open until binding\nregistration is wired.\n- Lua fixture comparison was not run because this changes C# source\ngeneration rather than Lua runtime behavior.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Low Risk**\n> Changes are confined to source generation and generator tests; runtime\nbinding still does not call the helper, so production Lua exposure\nbehavior is unchanged until a follow-up wires registration.\n> \n> **Overview**\n> Extends the B1 **LuaInterop** source generator so `[LuaObject]`\ncompanion partials emit a private\n**`__NovaSharpGeneratedRegisterEnumTables`** helper when exposed members\nreference enums, instead of only listing enum names in the manifest.\n> \n> The generator now collects enum members and constants, honors\n**`[LuaIgnore]`** on types and fields, maps signed/unsigned values to\n**`LuaValue.FromInteger`** / **`FromNumber`**, and picks collision-safe\ndestination keys (simple name vs qualified display name vs `enum:`\nsuffix). Golden output for **`PlayerApi`** includes the new helper;\n**PLAN** and session **161** document the slice.\n> \n> Generator tests compile emitted code and invoke the helper via\nreflection, covering basic tables, large unsigned constants, duplicate\nenum simple names, member-name collisions (no overwrite), and ignored\nenums/members.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\nf45941cbfc0ed24d5fa66913e4dd716878e5207b. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-05T15:27:35-07:00",
          "tree_id": "0082fd992a4e92224d373889e82fb8c06acb6d10",
          "url": "https://github.com/wallstop/NovaSharp/commit/52ba6cc4b7bcf3a8e6aad867100854e0ebb45b5c"
        },
        "date": 1783290866787,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 153.113,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 166.931,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 163.438,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 255.14,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 266.418,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 281.115,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 303.887,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 353.446,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 294.916,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 316.302,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 350.092,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 340.821,
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
          "id": "16b6dae845601943c5b4bf491a1e4fd5b9835d66",
          "message": "[codex] Add generated interop registration callbacks (#59)\n\n## Summary\n- Add the public `LuaCallback`/`LuaContext` host callback surface and\n`LuaEngine.CreateCallback`.\n- Generate `__NovaSharpGeneratedRegister(...)` for `[LuaObject]`\npartials with enum table exposure and direct method callbacks.\n- Add typed generated argument unpacking/return wrapping, keyword member\nescaping, unsigned return handling, facade value round-tripping, and\nanalyzer hardening for unsupported `[LuaObject]` member signatures.\n\n## Validation\n- `dotnet build\nsrc/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj\n--no-restore`\n- `dotnet build\nsrc/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj\n--no-restore`\n- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` (15\npassed)\n- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` (31\npassed)\n- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` (51\npassed)\n- `./scripts/build/quick.sh`\n- `dotnet tool run csharpier format .`\n- `git diff --check`\n- `./scripts/test/quick.sh` (14,898 passed)\n- `bash ./scripts/dev/pre-commit.sh`\n- pre-push hook on `git push -u origin dev/wallstop/plan-16`\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Expands the public NovaSharp API and generated interop registration\npath used by future Unity/modding hosts; behavior is heavily tested but\nproperty binding and zero-allocation callbacks remain unfinished.\n> \n> **Overview**\n> Adds the **public host callback surface** (`LuaCallback`,\n`LuaContext`, `LuaEngine.CreateCallback`) and wires the B1 source\ngenerator to **register `[LuaObject]` types at runtime** instead of\nemitting placeholder dispatch only.\n> \n> Generated companion partials now expose\n**`__NovaSharpGeneratedRegister(...)`**, which builds an object table,\nattaches **enum subtables**, installs **per-method callbacks** via\n`CreateCallback`, and publishes the table under the Lua object name.\nDispatch is **instance-based** with **string-switch** routing, **typed\narg unpack** and **return wrapping** for primitives and facade types\n(`LuaValue`/`LuaTable`/`LuaFunction`/`LuaCoroutine`), **C# keyword\nescaping**, **unsigned return** handling, and **ScriptRuntimeException**\nfor arity/type errors so Lua `pcall` can catch them.\n> \n> **Analyzer/generator tightening:** `[LuaObject]`-typed members are\n**NS0002** (no longer “supported”); **mutable struct methods** are\n**NS0003**; duplicate Lua names **prefer dispatchable methods**;\nmanifests list **callback members only** (properties/fields deferred).\n**`ToValue()`** on `LuaFunction`/`LuaCoroutine` supports callback\nreturns. PLAN marks **enum auto-exposure** done; golden and integration\ntests cover registration, keywords, unsigned, facade round-trip,\nreadonly structs, and reflection-free output.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\nd0aff8c1e02bb273531fe18a667aab510a05acf7. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->\n\n---------\n\nCo-authored-by: copilot-swe-agent[bot] <198982749+Copilot@users.noreply.github.com>",
          "timestamp": "2026-07-05T17:14:54-07:00",
          "tree_id": "305c612fcecbd13f08ade5397db813b563aa11fd",
          "url": "https://github.com/wallstop/NovaSharp/commit/16b6dae845601943c5b4bf491a1e4fd5b9835d66"
        },
        "date": 1783297204144,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 170.625,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 171.062,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 176.205,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 273.159,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 272.926,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 282.488,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 311.739,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 321.138,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 306.969,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 346.969,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 346.949,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 361.323,
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
          "id": "e09e1aabd75a01afdf05f91ba8485b0143f35348",
          "message": "[codex] Add generated property field bindings (#60)\n\n## Summary\n\n- Adds live generated property and field binding for supported\n`[LuaMember]` members through generated `__index`/`__newindex`\nmetatables.\n- Adds `LuaTable.SetMetatable(...)` to the root facade so generated\nregistration does not depend on the Lua metatable library being loaded.\n- Hardens analyzer diagnostics so static generated members, indexer\nproperties, and const/static fields are rejected instead of silently\nskipped by the generator.\n- Updates B1 PLAN/progress notes and golden generator snapshots.\n\n## Validation\n\n- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` (34\npassed, 0 failed)\n- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` (18\npassed, 0 failed)\n- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` (51\npassed, 0 failed)\n- `./scripts/build/quick.sh`\n- `./scripts/test/quick.sh` (14,904 passed, 0 failed, 0 skipped)\n- `git diff --check`\n- `bash ./scripts/dev/pre-commit.sh`\n- `git push` pre-push hook\n\n## Notes\n\n`LuaTable.Get(...)` and `LuaTable.Set(...)` remain raw facade\noperations. The generated property/field binding guarantee in this slice\nis for normal Lua script access against the registered table.\n\n<!-- CURSOR_SUMMARY -->\n---\n\n> [!NOTE]\n> **Medium Risk**\n> Changes generated Lua–CLR binding semantics and adds a public table\nAPI; risk is mitigated by broad tests and analyzer alignment, but\nincorrect metatable or enum conversion behavior could affect mod-facing\ninterop.\n> \n> **Overview**\n> Extends the B1 interop source generator so **`[LuaMember]` properties\nand fields** are exposed through generated **`__index` / `__newindex`**\nmetatables instead of table entries, while methods and enum tables stay\non the object table. Registration attaches that metatable via a new\n**`LuaTable.SetMetatable(...)`** facade API (documented as bypassing Lua\n`__metatable` protection).\n> \n> The generator models **method vs property vs field** bindings,\nread/write rules (e.g. init-only/readonly/value-type copies read-only),\nand **duplicate Lua name resolution** by binding priority. **Unsigned\n`ulong` and unsigned enums** use a generated\n**`__NovaSharpGeneratedReadUInt64`** helper with **checked underlying\ncasts** before enum assignment; non-string keys return nil on read and\nerror on write.\n> \n> The **analyzer** now reports **NS0003** for static\nmethods/properties/fields, indexer properties, and const fields that the\ngenerator does not emit. Golden outputs, runtime tests (stale snapshot\nafter method mutation, fields, enum round-trip), and PLAN/session notes\nare updated.\n> \n> <sup>Reviewed by [Cursor Bugbot](https://cursor.com/bugbot) for commit\n1258a591b97559a69d986233243ee5282b1e6f21. Bugbot is set up for automated\ncode reviews on this repo. Configure\n[here](https://www.cursor.com/dashboard/bugbot).</sup>\n<!-- /CURSOR_SUMMARY -->",
          "timestamp": "2026-07-05T19:53:49-07:00",
          "tree_id": "45be844fc55c4dfc75696dcbf5c67273345d78a8",
          "url": "https://github.com/wallstop/NovaSharp/commit/e09e1aabd75a01afdf05f91ba8485b0143f35348"
        },
        "date": 1783306741921,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 0)",
            "value": 163.185,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 0)",
            "value": 169.561,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 0)",
            "value": 170.481,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 1)",
            "value": 270.062,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 1)",
            "value": 266.74,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 1)",
            "value": 280.471,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 2)",
            "value": 305.527,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 2)",
            "value": 302.322,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 2)",
            "value": 307.216,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.ScriptCallFixedArity(Arity: 3)",
            "value": 336.179,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaEngineCallFixedArity(Arity: 3)",
            "value": 356.875,
            "unit": "ns",
            "extra": ""
          },
          {
            "name": "WallstopStudios.NovaSharp.Benchmarks.RuntimeBenchmarksB0FacadeCallOverhead.LuaFunctionCallFixedArity(Arity: 3)",
            "value": 356.761,
            "unit": "ns",
            "extra": ""
          }
        ]
      }
    ]
  }
}
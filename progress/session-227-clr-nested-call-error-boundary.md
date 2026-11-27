# Session 227 — CLR nested-call error boundary

Issue: [#232](https://github.com/Ambiguous-Interactive/NovaSharp/issues/232).

## Carry-forward inventory

- Open pull requests: none (`gh pr list --state open` returned 0).
- Main was green at `285e6a20`: Tests, Benchmarks, CSharpier, and
  `pages-build-deployment` completed successfully.
- Open issues: 50 on page 1, 4 on page 2, and 0 on page 3 of the paginated
  REST inventory, so pagination is exhausted with 54 issues. Ranked by gameplay
  impact: runtime/host crashes first, then Lua state corruption
  ([#231](https://github.com/Ambiguous-Interactive/NovaSharp/issues/231)),
  then coroutine-close semantics
  ([#227](https://github.com/Ambiguous-Interactive/NovaSharp/issues/227),
  [#229](https://github.com/Ambiguous-Interactive/NovaSharp/issues/229),
  [#169](https://github.com/Ambiguous-Interactive/NovaSharp/issues/169)), then
  library/performance/tooling work.
- Selected [#232](https://github.com/Ambiguous-Interactive/NovaSharp/issues/232):
  a protected CLR tail target that calls back into Lua and fails threw
  `IndexOutOfRangeException` from the normal-return cleanup after the nested
  `Script.Call` error handler had already consumed the same inputs. That is a
  host-visible crash and leaves the value stack inconsistent.

## Root cause

`Script.Call` and `Script.DoString` enter Lua through an entry frame marked
`EntryPoint`. Error unwinding uses that marker to stop at the CLR boundary and
rethrow to the caller. Tail-call frame reuse (`PerformTco`) copied only
`OperatorCallFlags`, dropping `EntryPoint`, `ResumeEntryPointBit`, and
`CallEntryPointBit` when the chunk's final call was a tail call. The nested loop
then unwound past its own boundary, resumed the outer CLR frame's error handler
inside the nested loop, and the original `InternalExecCall` cleanup removed the
call inputs a second time.

The same flag loss also disabled Lua 5.4+ deferred close-handler cleanup for
coroutines whose body ends in a tail call, so pending closers ran during
`coroutine.resume` instead of `coroutine.close`.

## Runtime change

- Preserve entry-point and operator flags across tail-call frame reuse with a
  single `TailCallReuseFlags` mask.
- The unwinder now stops at the reused entry boundary and rethrows, letting the
  outer processing loop run the registered error handler once and resume at the
  call site.
- Coroutine close deferral now distinguishes host CLR-to-script calls from
  classic-library callbacks. `ScriptExecutionContext.CallLibrary` and the
  direct CoreLib re-entry points set a one-shot marker that tags only their
  immediate entry frame with `LibraryCallEntryBit`, so library errors keep
  Lua 5.4+ close handlers pending until `coroutine.close`. The marker is
  consumed by that frame and never propagates to nested host calls;
  `ScriptExecutionContext.Call` remains a host boundary.
- A host `Script.Call` inside a coroutine is a catchable boundary: the
  unwinder restores the execution and value stacks before rethrowing instead
  of leaving the nested entry frame behind.
- Pre-unwind message decoration also stops at a host CLR entry, so a handler
  cannot run inside a nested loop whose host catches the error and recovers.
  VM-owned close-handler entries carry `VmCloseCallEntryBit`, keeping the
  failing close frame visible while nested host entries still stop the scan.

## Evidence

- Red on `main`: the host reproduction threw `IndexOutOfRangeException` at
  `ProcessorInstructionLoop.cs:2430` under Lua 5.1–5.5. Reproduced directly and
  through `TailCallTUnitTests`.
- Red on `main`: a coroutine body ending in `return fail()` ran the callee's
  `<close>` handler during `coroutine.resume` (`beforeClose = 1`); PUC Lua
  5.4.7 and 5.5.0 report `0` and run it during `coroutine.close`.
- Green after the fix: direct, repeated, argument-surrounded, and non-tail
  nested `Script.Call` failures recover once per failure and leave usable
  stacks. Lua fixture:
  `LuaFixtures/TailCallTUnitTests/ClrTailTargetNestedCallErrorHandlerRecoversAtCallSite.lua`
  (NovaSharp-only because it uses injected host callbacks).
- Green after the fix: the coroutine fixture
  `LuaFixtures/ProcessorCoroutineCloseTUnitTests/TailCallErrorDefersCloseHandlersUntilCoroutineClose.lua`
  matches PUC Lua 5.4.7 and 5.5.0 (`resume false boom`, `beforeClose 0`,
  `close false boom`, one `closed:boom`).
- Red on `main`: the host-caught nested call inside a coroutine left the nested
  entry frame on the execution stack and seven values on the value stack;
  a non-tail body then crashed with `NullReferenceException` in `AssignLocal`.
  A pre-unwind decorator also ran even though the host recovered the error.
- New regression coverage:
  `ProcessorCoroutineCloseTUnitTests.HostCaughtNestedCallErrorRestoresCoroutineStacks`
  (tail and non-tail coroutine bodies, Lua 5.1-5.5, asserts both stacks return
  to their pre-call counts), and
  `TailCallTUnitTests.ClrTailTargetNestedCallDecorationStopsAtHostBoundary`.
- New host boundary inside a library callback:
  `ProcessorCoroutineCloseTUnitTests.HostCaughtNestedCallInsideLibraryCallbackRestoresStacks`
  runs a host `Script.Call` from a `string.gsub` replacement, catches the
  error, and asserts the stacks return to their pre-call counts.
- Existing `CoroutineModuleTUnitTests.ClrCallbackErrorDefersLocalCloseUntilCoroutineClose`
  still passes: classic-library callbacks keep their deferred close semantics.
- New library boundary coverage: `SortComparatorErrorDefersLocalCloseUntilCoroutineClose`
  (table.sort comparator entry),
  `CallableTablePrintMetamethodErrorDefersLocalCloseUntilCoroutineClose`
  (print __tostring implemented as a callable table), and
  `FormatCallableTableMetamethodErrorDefersLocalCloseUntilCoroutineClose`
  (string.format `%s` through `LuaBase.LuaCall`), all Lua 5.4/5.5.
  `Processor.CallLibraryFunction` now marks the synthetic library callback
  frame, and `Script.CallLibraryValues`, `ScriptExecutionContext.CallLibrary`,
  and `Script.CallableResolvesToLuaFunction` follow a callable-table __call
  chain before deciding to mark the entry.
- Host argument conversion keeps host semantics:
  `CallbackArguments.AsStringUsingMeta` stays a host boundary, while print
  and `string.format('%s', ...)` use the internal
  `AsStringUsingMetaLibrary` path. This fixes Lua 5.2+ `%s` to invoke
  `__tostring` like PUC Lua instead of using the raw table print string,
  scalarizes multi-result `__tostring` calls, accepts numeric results, applies
  string type metatables, and
  saves/restores the shared format buffer so a nested `string.format` cannot
  corrupt the outer specification.
- `CallableResolvesToLuaFunction` mirrors `CallChainedNonFunction`: at most
  nine non-direct `__call` links, then the final value decides. A dedicated
  chain-limit test covers nine-link acceptance and ten-link rejection.
- Focused suites: `TailCallTUnitTests` 127/127,
  `ProcessorCoroutineCloseTUnitTests` 118/118,
  `CoroutineModuleTUnitTests` 354/354, and the four `FormatPercentS` tests
  20/20 across Lua 5.2-5.5.
- Full TUnit suite: 16297/16297.
- Corpus extraction reports 2114 snippets (ten new generated fixtures); the
  hand-authored NovaSharp-only fixtures are intentionally outside the manifest.
- Lua comparison against 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0 reports zero
  unexpected mismatches, zero Lua-only/NovaSharp-only outputs, and no new
  ratchet entries. The single ratchet removal
  (`TableModuleTUnitTests/SortThrowsWhenComparatorIsInvalidType.lua`) is
  pre-existing on `main` and unrelated to this change.

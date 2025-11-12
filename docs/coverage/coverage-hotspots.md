# Coverage Hotspots (baseline: 2025-11-10)

Latest data sourced from `docs/coverage/latest/Summary.json` (generated via `./coverage.ps1` on 2025-11-11 15:03 UTC).

## Snapshot
- Overall line coverage: **68.5 %**
- NovaSharp.Interpreter line coverage: **81.9 %**
- NovaSharp.Cli line coverage: **73.7 %**
- NovaSharp.Hardwire line coverage: **22.3 %**
- NovaSharp.RemoteDebugger / NovaSharp.VsCodeDebugger: **0 %** (no tests yet)

## Prioritized Red List (Interpreter < 90 %)

- `NovaSharp.Interpreter.Platforms.PlatformAccessorBase` – 44.0 % line. Extend platform accessor tests to cover sandbox/full-trust fallbacks.
- `NovaSharp.Interpreter.Tree.Expressions.UnaryOperatorExpression` – 48.1 % line. Add bytecode error-path coverage for unexpected operators (runtime guards already in place).
- `NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors.EventMemberDescriptor` – 53.0 % line. Continue expanding dispatch tests (by-ref/value scenarios, delegate fan-out) until compatibility/error branches are covered.

(Review full list in `docs/coverage/latest/Summary.json`.)

## Action Items
1. Assign owners for each red-listed class (default owner noted above until explicit assignment).
2. Add issue/project board entries mirroring this table so progress is tracked.
3. Update this document after each `./coverage.ps1` run (include new timestamp + notes).
4. When a class crosses 90 %, move it to the green archive section (to be added) and celebrate the win.

## Recently Covered
- `PerformanceStopwatch`, `GlobalPerformanceStopwatch`, and `DummyPerformanceStopwatch` now covered by dedicated stopwatch unit tests.
- `PerformanceStatistics` exercises enabling/disabling counters and global aggregation.
- `ReplHistoryInterpreter` navigation (prev/next) verified via tests.
- Hardwired descriptor helpers (member + method) now covered via `HardwiredDescriptorTests`, validating access checks, conversions, and default-argument marshalling.
- `OsSystemModule` edge paths validated with platform stub tests (non-zero exits, missing files, rename/delete failures, setlocale placeholder), driving coverage to 98 %.
- `debug.debug` loop now exercised via queued debug console hooks, confirming prompt/print wiring and error reporting without manual REPL interaction.
- Platform accessors (`LimitedPlatformAccessor`, `StandardPlatformAccessor`) guarded with sandbox/full IO tests.
- `EmbeddedResourcesScriptLoader` validated against embedded Lua fixture.
- `InternalErrorException` constructors covered by direct unit tests.
- `SerializationExtensions` exercised with prime/nested table scenarios and tuple/string escaping; serializer fixed to emit Lua-compliant braces/newlines.
- `ErrorHandlingModule` now sits at 100 % line / 94 % branch thanks to nested `pcall`/`xpcall` regression tests (tail-call continuations, yield requests, CLR handlers).
- `HardwiredMemberDescriptor` base getters/setters now throw under coverage, shrinking the remaining uncovered lines to the by-ref conversion paths (currently at 100 % line coverage in the latest report).
- `DynValueMemberDescriptor` now covered across read access, execution flags, setter guard, and wiring paths (primitive, table, userdata, unsupported types).
- `DebugModule` interactive loop now handles returned values, CLR exceptions, and null-input exits under test, bumping branch coverage to 77 %.
- Expanded `ReflectionSpecialName` operator mapping tests (additional arithmetic/relational cases) to drive branch coverage; rerun `coverage.ps1` to confirm the updated instrumentation.
- `OsTimeModule` now sits at 97 % line coverage after adding missing-field, pre-epoch, and conversion-specifier tests.
- `DebuggerAction` coverage lifted to 100 % by testing constructor timestamps, age calculations, defensive line storage, and breakpoint formatting.
- `CompositeUserDataDescriptor` now covered at 92 % via aggregate lookup, set, and metatable resolution tests (`CompositeUserDataDescriptorTests`).
- `UndisposableStream` reaches 94 % line coverage after forwarding/guard tests ensured dispose/close suppression and async passthrough behaviour.
- `LuaStateInterop.Tools` climbs to 94 % line coverage after adding targeted numeric checks, conversion, meta-character substitution, and formatting regressions.
- `PlatformAccessorBase` branches (Unity, Mono, portable, AOT, prompt bridging) now covered via detector flag shims, keeping platform naming logic under regression.
- Added `EventFacadeTests` (happy-path add/remove, unsupported indices, setter guard) to pin runtime behaviour ahead of reflection descriptor expansion.
- Added `EventMemberDescriptorTests.RemoveCallbackWithoutExistingSubscription` to exercise the branch where removal is requested before any handler is registered, keeping delegate bookkeeping code under coverage.
- Added `UnityAssetsScriptLoaderTests.ReflectionConstructorSwallowsMissingUnityAssemblies` to cover the reflection-based initialization path when Unity assemblies are absent.
- Added `LoopBoundaryTests` to cover the loop guard implementation (boundary detection and error propagation).
- Added `NodeBaseTests` to assert token helpers (`CheckTokenType`, `CheckMatch`, `CheckTokenTypeNotNext`) and their error branches.
- Added `StandardGenericsUserDataDescriptorTests` so the generics descriptor (ctor validation, Index/MetaIndex, Generate) is exercised without relying on manual reflection.
- Added `CoroutineApiTests` to drive enumerable helpers, Unity coroutine adapters, CLR callback transitions, and `AutoYieldCounter`, boosting coroutine coverage.
- Added `FastStackTests` to cover push/pop/remove/expand/crop paths in the fixed-capacity stack implementation.
- `SourceRefTests` cover FormatLocation/GetCodeSnippet heuristics (81 % line coverage) while `ExitCommandTests` drive the CLI `exit` path to 100 %, nudging NovaSharp.Cli line coverage to 73.7 %.
- `LoadModuleTests` now exercise `require`, `load`, and `loadfilesafe` paths (LoadModule at 71 % line coverage), and `SyntaxErrorExceptionTests`/`DynamicExpressionException` assertions ensure parser errors honour nested rethrow rules and message prefixes.
- `EventMemberDescriptorTests` expanded with compatibility guards and multi-signature dispatch checks, lifting event coverage to 53 % and validating zero-arg/multi-arg pathways.
- `ParameterDescriptorTests` cover reflection metadata, restriction semantics, and wiring exports, pushing the descriptor to 70.8 % line coverage and guarding invalid restriction paths.
- `AutoDescribingUserDataDescriptorTests` verify name/type exposure plus index/set/meta forwarding to IUserDataType, keeping self-describing userdata behaviour under regression tests.
- `StandardEnumUserDataDescriptorTests` ensure flag helpers, numeric coercion, and signed/unsigned paths work correctly, raising enum descriptor coverage to 67.4 %.
- `UnityAssetsScriptLoaderTests` cover path normalization, missing-script diagnostics, and enumeration helpers, boosting loader coverage while keeping Unity packaging logic under regression.
- `WatchItemTests` exercise formatting/null-handling, closing out the debugging watch surface (now 100 % covered), while `ValueTypeDefaultCtorMemberDescriptorTests` confirm validation/instantiation paths and lift the descriptor to 78.5 %.

## Updating the Snapshot
```powershell
./coverage.ps1
# Copy docs/coverage/latest/Summary.json entries into the tables above.
```

_Last updated: 2025-11-11_

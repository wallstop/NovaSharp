# Coverage Hotspots (baseline: 2025-11-10)

Latest data sourced from `docs/coverage/latest/Summary.json` (generated via `./scripts/coverage/coverage.ps1` on 2025-11-15 21:26 UTC).

## Snapshot
- Overall line coverage: **82.2 %**
- NovaSharp.Interpreter line coverage: **91.4 %**
- NovaSharp.Cli line coverage: **79.7 %**
- NovaSharp.Hardwire line coverage: **54.8 %**
- NovaSharp.RemoteDebugger line coverage: **79.1 %** (DebugServer still below 70 %; remaining gaps focus on queue/breakpoint handling + Tcp helpers)
- NovaSharp.VsCodeDebugger line coverage: **0 %** (no tests yet)

## Prioritized Red List (Interpreter < 90 %)
- `NovaSharp.Interpreter.Interop.Converters.ScriptToClrConversions` — **82.0 %** (up from 72.8 %). `ScriptToClrConversionsTests` now cover optional parameter defaults, string/char/StringBuilder coercions, enum downcasts, callback delegates, table-to-list conversions, userdata AsString fallbacks, and custom converter precedence. Remaining work: drive the tuple handling, branch weighting for tables/dictionaries, and the delegate bridge branches referenced in the modernization notes until the class clears 90 %.
- See `docs/coverage/latest/Summary.json` for the full list; update this section after each burn-down.

## Action Items
1. Assign owners for each red-listed class (default owner noted above until explicit assignment).
2. Add issue/project board entries mirroring this table so progress is tracked.
3. Update this document after each `./scripts/coverage/coverage.ps1` run (include new timestamp + notes).
4. When a class crosses 90 %, move it to the green archive section (to be added) and celebrate the win.

## Recently Covered
- `CustomConverterRegistry` jumped to **100 % line / 100 % branch / 100 % method** after introducing `CustomConverterRegistryTests` (duplicate registration, removal, typed + obsolete CLR bridge paths, `Clear`) and tightening the registry's DataType guard so invalid values no longer index past the backing arrays. Coverage run `./scripts/coverage/coverage.ps1` (Release) on 2025-11-15 20:12 UTC captured the bump and pushed interpreter totals to **91.1 % line / 87.4 % branch / 93.5 % method** with **1 760** Release tests.
- `EnumerableWrapperTests` now drive iterator callbacks, nil-skipping, reset guards, table conversion, and the exposed `Current/MoveNext/Reset` userdata members so `EnumerableWrapper` sits at **95.2 % line / 88.4 % branch / 88.8 % method** (coverage run 2025-11-15 20:52 UTC). That clears the enumerable entry from the red list and raises the interpreter suite to **91.3 % line / 87.8 % branch / 93.6 % method** with **1 780** Release tests.
- `BreakStatementTests` execute all error paths—top-level `break`, nested functions, and loop-boundary guards—so `BreakStatement` now reports **100 % line / 100 % branch / 100 % method** coverage. Coverage run `./scripts/coverage/coverage.ps1` (Release) on 2025-11-15 21:17 UTC captures the latest totals (**91.39 % line / 87.85 % branch / 93.61 % method**, **1 783** tests).
- `ScriptToClrConversionsTests` now guard optional/default conversions, coercion weights, and delegate/table/userdata paths so `ScriptToClrConversions` sits at **82.0 % line / 80.3 % branch / 100 % method** (coverage run 2025-11-15 20:27 UTC). Keep iterating on tuple, table-to-dictionary, and string-subtype failure paths until the class clears the 90 % gate.
- `ProxyUserDataDescriptorTests` now cover proxy creation for reads/writes/meta ops, friendly-name/Type plumbing, null-instance passthrough, and compatibility checks. `ProxyUserDataDescriptor` reports **100 % line / 100 % branch / 100 % method** and interpreter totals rise to **91.42 % line / 87.87 % branch / 93.81 % method** with **1 791** Release tests (`./scripts/coverage/coverage.ps1` on 2025-11-15 21:26 UTC).
- `BinaryOperatorExpression` now sits at **90.0 %** line / **82.5 %** branch coverage after adding compile-path opcode assertions (arithmetic, concatenation, comparison, and `~=` inversion) plus new string comparison/equality regressions; coverage run `./scripts/coverage/coverage.ps1` (Release) on 2025-11-14 17:44 UTC captured the jump.
- `LuaStateInterop.Tools` climbed to **98.2 % line / 90.4 % branch** after expanding `LuaStateInteropToolsTests` with unsigned/null-edge coverage and exhaustive `sprintf` permutations (`%i`, `%f/%e/%E/%g/%G`, `%c`, `%s`, `%#o`, `%hd/%hu/%ld/%lu`, flag precedence). The same coverage run bumped interpreter totals to **90.7 % line / 87.4 % branch / 93.1 % method** with **1 721** Release tests.
    - `UnityAssetsScriptLoader` is now fully exercised under the reflection pathway thanks to a dynamically generated `UnityEngine` stub and failure-mode regression tests, yielding **100 % line / 90 % branch** coverage and pushing the suite to **1 723** Release tests (interpreter totals: **90.9 % line / 87.5 % branch / 93.3 % method**).
    - Remote debugger automation now includes `Units/RemoteDebuggerServiceTests`, `Units/HttpServerTests`, `Units/HttpResourceTests`, `Units/XmlWriterExtensionsTests`, and the extended `Units/RemoteDebuggerTests` (host busy/ready refresh queue). Combined with the existing TCP command suite, `NovaSharp.RemoteDebugger` sits at **78.4 % line coverage** (`DebugServer` 68.7 %, `RemoteDebuggerService` 96.6 %, `DebugWebHost` 100.0 %, `HttpServer` 91.1 %, `HttpResource` 94.7 %); remaining work targets the DebugServer queue/breakpoint flows plus the Tcp helpers still under 80 %.
- `UnaryOperatorExpression` now sits at **100 %** line/branch coverage after adding direct Eval tests for `not`, `#`, `-`, and the non-numeric failure path.
- `StringModule` has climbed to **97.2 %** line / **94.6 %** branch coverage via spec-aligned edge cases and modulo-normalization in production.
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
- `StandardEnumUserDataDescriptor` now exercises conversion helpers, numeric backstops, argument validation, and meta operations (85.6 % line / 88.8 % branch coverage).
- `FieldMemberDescriptor` covers pre/lazy optimized getters, null-instance guards, and metadata wiring (80.7 % line coverage).
- `CallbackFunction` now sits at **91.3 %** line coverage thanks to colon-operator handling tests, default access mode validation, and delegate/visibility checks; closure-centric tests also cover entry-point metadata and call overloads, lifting `Closure` to **86.6 %** while documenting the remaining `_ENV` constraint.
- `ParameterDescriptor` is fully covered (**100 %** line) after exercising type restriction constructors, by-ref wiring, `OriginalType` fallbacks, and `ToString` formatting.
- `LoadModule` now reports **91.3 %** line / **86.9 %** branch coverage following new NUnit cases for reader concatenation, safe-environment failure, `loadfile` error tuples, and `dofile` success/error flows.
- `IoModule` climbed to **93.2 %** line / **88.0 %** branch coverage via expanded tests (default stream setters, flush/close, iterator API, binary encoding guardrails, tmpfile lifecycle, and `IoExceptionToLuaMessage` fallbacks).
- `ScriptRuntimeException` factory helpers now execute under `ScriptRuntimeExceptionTests`, covering table index errors, conversion failures, coroutine guard rails, access-on-statics paths, and `Rethrow` behaviour (interpreter line coverage holds at 86.5 % with the new tests).
- `ExprListExpression` tuple compilation/evaluation paths now run under NUnit, retiring the parser red list entry (91.6 % line coverage).
- `ScriptExecutionContext` reached 96.2 % line / 86.3 % branch coverage after backfilling AdditionalData, Call (yield/tail/metamethod), and EvaluateSymbol edge paths.
- `FastStack<T>` is now fully exercised (100 % line/method coverage) through explicit interface validation and reflection-based zeroing checks.
- `EventMemberDescriptor` now sits at 94 %+ coverage after exercising add/remove failure paths, static facades, and assignment guards.
- `CharPtr` climbed to 98.8 % line coverage by validating byte-array conversions, pointer arithmetic, navigation helpers, and null equality branches.
- `DescriptorHelpers` now reports 92.9 % line / 90.1 % branch coverage after supplementing visibility, identifier shaping, and SafeGetTypes guard scenarios.
- Expanded `ReflectionSpecialName` operator mapping tests (additional arithmetic/relational cases) to drive branch coverage; rerun `scripts/coverage/coverage.ps1` to confirm the updated instrumentation.
- `OsTimeModule` now sits at 97 % line coverage after adding missing-field, pre-epoch, and conversion-specifier tests.
- `DebuggerAction` coverage lifted to 100 % by testing constructor timestamps, age calculations, defensive line storage, and breakpoint formatting.
- `CompositeUserDataDescriptor` now covered at 92 % via aggregate lookup, set, and metatable resolution tests (`CompositeUserDataDescriptorTests`).
- `DispatchingUserDataDescriptor` crossed the ≥90 % threshold at **94.2 %** line / **93.3 %** branch (2025-11-15 13:47 UTC) after adding tests for duplicate-member guardrails, overload aggregation, `SetIndex` setters/fallbacks, optimizer fan-out, helper-name wrappers, unsupported metamethod requests, and non-comparable/`null` metamethod scenarios. `NovaSharp.Interpreter.DataTypes.UserData` remains fully covered (**100 %**) thanks to the assembly-scan fallback and default-policy regression tests.
- `NovaSharp.Interpreter.DataTypes.UserData` is now **100 %** covered after the latest `UserDataTests` sweep hardened the default-registration-policy rejection path, proxy factories, generic helpers, equality/mixed null scenarios, and assembly scanning (with a .NET 8-friendly fallback when `Assembly.GetCallingAssembly` throws).
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
- `AutoDescribingUserDataDescriptorTests` verify name/type exposure plus index/set/meta forwarding to IUserDataType, keeping self-describing userdata behaviour under regression tests.
- `StandardEnumUserDataDescriptorTests` ensure flag helpers, numeric coercion, and signed/unsigned paths work correctly, raising enum descriptor coverage to 67.4 %.
- `UnityAssetsScriptLoaderTests` cover path normalization, missing-script diagnostics, and enumeration helpers, boosting loader coverage while keeping Unity packaging logic under regression.
- `WatchItemTests` exercise formatting/null-handling, closing out the debugging watch surface (now 100 % covered), while `ValueTypeDefaultCtorMemberDescriptorTests` confirm validation/instantiation paths and lift the descriptor to 78.5 %.
- `ScriptLoaderBaseTests` now validate LUA_PATH overrides, ignore flags, environment fallbacks, and string-path unpacking (coverage jumps to 96.9 %; rerun `./scripts/coverage/coverage.ps1` after the latest CharPtr work to capture the uplift).
- `CharPtrTests` and `DescriptorHelpersTests` target KopiLua interop and descriptor utilities (visibility helpers, identifier normalization), with `DescriptorHelpers` now following the Wallstop string helpers’ rules so digit suffixes stay contiguous and Pascal initials are preserved.
- `EventMemberDescriptorTests` now drive delegate creation for 1–16 parameter events, covering the previously untested switch cases in `EventMemberDescriptor.CreateDelegate`.
- `LuaStateInteropToolsTests` exercise additional format specifiers (`%#o`, `%p`, `%n`, space flag) so `LuaStateInterop.Tools` now accounts for zero-padding, pointer, and positive-space branches.
- `FastStackTests` include negative/no-op removals, overflow guards, single-slot clearing, and full resets, covering `FastStack.RemoveLast` and `FastStack.Clear` edge paths.
- `DotNetCorePlatformAccessorTests` validate file mode parsing, console output, filesystem helpers, and the NotSupported command path, tightening coverage for the .NET Core platform accessor shim.
- `JsonModuleTests` exercise invalid parse/serialize inputs plus `json.isnull`/`json.null`, backfilling JsonModule’s error and null-handling branches.
- `ClosureContextTests` ensure closure symbol arrays and stored values are covered, trimming Execution.Scopes coverage debt.
- `TablePairTests` now cover constructor, nil sentinel, and guarded setter behavior for `TablePair`.
- `PropertyTableAssignerTests` exercise expected/missing properties, subassigners, fuzzy matching, and type guards across both generic and non-generic assigners.
- `SliceTests` verify indexing, enumeration order, conversions, and NotSupported pathways for the slice view helper.
- `InteropRegistrationPolicyTests` ensure the default/automatic/explicit policy factories return the expected registration policies and that `Explicit` remains marked obsolete.

## Updating the Snapshot
```powershell
./scripts/coverage/coverage.ps1
# Copy docs/coverage/latest/Summary.json entries into the tables above.
```

_Last updated: 2025-11-12_

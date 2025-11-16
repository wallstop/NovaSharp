# Lua Compatibility (Target: 5.4.8)

NovaSharp aims to match the behaviour of upstream Lua **5.4.8**.

## Compatibility Principles

- Align runtime semantics, standard library behaviour, and coroutine semantics with stock Lua 5.4.8.
- Prefer behaviour parity over historic legacy quirks; regressions against Lua take precedence.
- Track any intentional deviations (e.g., .NET integration helpers) in this document and mirror coverage tests where feasible.

Use the matrices below to understand how close NovaSharp is to stock Lua 5.4.8, which gaps still need work, and which tests guard each area. Update the tables when functionality lands or coverage moves.

Legend: ✅ parity, 🚧 partial/in-progress, ❌ missing, ⏸️ intentionally unsupported.

## Compatibility Modes

NovaSharp now exposes a version selector so applications can pin script execution to a specific Lua baseline. The global entry point is `Script.GlobalOptions.CompatibilityVersion`, and per-script overrides live on `Script.Options.CompatibilityVersion`. Both default to `LuaCompatibilityVersion.Latest`, keeping behaviour aligned with the most recent NovaSharp target (Lua 5.4.x today). Future releases can introduce compatibility shims for Lua 5.5/5.3/5.2 features without forcing a wholesale migration—set the enum to the desired version before running scripts to opt in.

## Language Syntax & Semantics

| Feature | Status | Coverage / Evidence | Notes & Owner |
| --- | --- | --- | --- |
| Hexadecimal floats (`0x1.921fb5p+1`) | ✅ | `ParserTests.HexFloats*`, TAP `304-numbers.t` | Verified via NUnit + TAP; keep TAP enabled when Windows lane stable. Owner: Interpreter. |
| To-be-closed variables (`local f <close> = ...`) | ✅ | `CloseAttributeTests` | Reverse-order closing, reassignment, and error propagation now execute the registered `__close` metamethods. TAP `310-close-var.t` can be re-enabled once parity is double-checked across platforms. Owner: Interpreter. |
| Const locals (`local <const>`) | ✅ | `ParserTests.ConstLocals*` | Matches Lua 5.4 semantics including redeclaration errors. Owner: Interpreter. |
| Labels / `goto` enhancements | ✅ | TAP `204-grammar.t` | TAP covers cross-block and conditional labels. Owner: Interpreter. |
| `if <cond> then <const>` numeric coercion fixes | ✅ | `SimpleTests.NumericConversionFailsIfOutOfBounds` (indirect) | .NET interop ensures numeric conversion errors surface like Lua. Owner: Interop. |
| `break` / `return` in to-be-closed scope | ✅ | `CloseAttributeTests` | Function returns and error unwinds trigger deterministic closing; add TAP coverage for `break` once upstream fixtures are stable. Owner: Interpreter. |

## Standard Libraries

| Library Area | Feature | Status | Coverage / Evidence | Notes & Owner |
| --- | --- | --- | --- | --- |
| `math` | `math.type`, integer vs float math | ✅ | `MathModuleTests.MathType*`, TAP `501-math.t` | Full parity including `math.randomseed` determinism. Owner: Interpreter. |
| `math` | `math.ult`, `math.tointeger` edge cases | 🚧 | `MathModuleTests.UnsignedLessThan` (partial) | Need TAP mirror to cover overflow and negative handling. Owner: Interpreter. |
| `string` | `string.pack` / `unpack` extended options (`c`, `z`, alignment) | ❌ | — | Implement or document explicit limitation; currently unsupported. Owner: Runtime Modernization. |
| `string` | `%q`, UTF-8 escapes, pattern updates | ✅ | `StringLibTests`, TAP `401-strings.t` | Verified for UTF-8 escape sequences and `%q` output; keep TAP enabled as lane stabilises. Owner: Interpreter. |
| `table` | `table.move` metamethod behaviour | ✅ | `TableModuleTests.MoveHonoursMetamethods` | Matches Lua 5.4 metamethod invocation. Owner: Interpreter. |
| `table` | `table.pack` / `unpack` n-field handling | ✅ | `TableModuleTests.PackStoresN`, TAP `503-table.t` | Tests ensure `n` is promoted to integer. Owner: Interpreter. |
| `coroutine` | `coroutine.close` result tuples | ✅ | `TestMore/310-close-var.t`, `CloseAttributeTests` | Mirrors Lua §6.2 semantics (suspended cleanup, errored coroutine tuples) with §3.3.8 to-be-closed coverage. Owner: Interpreter. |
| `coroutine` | `coroutine.isyieldable` | ✅ | `CoroutineModuleTests.IsYieldable*` | Matches Lua semantics for main thread vs coroutine. Owner: Interpreter. |
| `debug` | `debug.upvaluejoin`, `debug.upvalueid` | ✅ | `DebugModuleTests.UpvalueJoin*`, `UpvalueId*` | Exercises cross-function upvalue sharing. Owner: Interpreter. |
| `debug` | `debug.getuservalue` / `setuservalue` | 🚧 | `DebugModuleTests.GetUserValueReturnsNil` | Light userdata unsupported; document limitation (⏸️). Owner: Interop. |
| `io` | `io.read("*n")` hex/exponent parsing | ✅ | `IoModuleTests.ReadNumberParsesHexLiteralInput`, `ReadNumberParsesHexVariants`, TAP `308-io.t` | Runtime parses hex floats, huge integers, and exponent overflow per Lua 5.4 rules. Owner: Runtime Modernization. |
| `io`, `os` | `io.popen`, `os.execute` return tables | 🚧 | `OsSystemModuleTests.Execute` (simplified) | Implementation returns simplified tuple; need parity with Lua status tables. Owner: Tooling. |

## Metatables, GC, and Weak Tables

| Feature | Status | Coverage / Evidence | Notes & Owner |
| --- | --- | --- | --- |
| Metatable `__pairs` / `__ipairs` behaviour | ✅ | `MetatableTests.CustomPairsIterators` | Matches Lua 5.4 iteration semantics, including fallback. Owner: Interpreter. |
| Ephemeron tables / weak keys & values | 🚧 | `WeakTablesTests` (legacy) | Behaviour follows legacy-era implementation; 5.4 ephemeron rules still missing. Owner: Runtime Modernization. |
| GC generational / incremental modes | ❌ | — | `collectgarbage("incremental")` unsupported; document limitation. Owner: Runtime Modernization. |
| `debug.getmetatable` protection for protected tables | ✅ | `MetatableTests.ProtectedMetatableFailures` | Aligns with Lua raising errors for protected metatables. Owner: Interpreter. |

## Coroutine & Error Handling Semantics

| Scenario | Status | Coverage / Evidence | Notes & Owner |
| --- | --- | --- | --- |
| Yield across CLR boundary raises `ScriptRuntimeException` | ✅ | `CoroutineModuleTests.YieldAcrossClrBoundaryThrows` | Mirrors Lua `attempt to yield across C-call boundary`. Owner: Interop. |
| Error propagation from `pcall` / `xpcall` | ✅ | `CoroutineModuleTests.PCallYielding*`, `ErrorHandlingModuleTests.PCall*` | Matches Lua 5.4 tuple shapes. Owner: Interpreter. |
| `lua_resume` status mapping (`coroutine.resume`) | ✅ | `CoroutineModuleTests.ResumeStatus*` | Ensures `running`, `suspended`, `dead` states align. Owner: Interpreter. |
| `debug.traceback` with message parameter | 🚧 | `ProcessorStackTraceTests.TracebackPrependsMessage` | Needs additional coverage for level parameter and thread argument. Owner: Interpreter. |

## Observability & Outstanding Actions

- **Enable missing TAP suites**: `TestMore_308_io` and `TestMore_309_os` stay disabled until IO parity improves. Track progress in `docs/testing/real-world-scripts.md` and `docs/coverage/coverage-hotspots.md`.
- **File follow-up issues** for every ❌ entry and link them here with IDs once created (e.g., `Fixes #1234`). Use the owner column to avoid orphaned work.
- When introducing new Lua 5.4 features or parity fixes, add NUnit coverage and update both this matrix and `PLAN.md` so the modernization roadmap stays consistent.

If you discover behaviour that diverges from Lua 5.4.8, open an issue, add it to the relevant table with status ❌ or 🚧, and reference any reproducing tests.

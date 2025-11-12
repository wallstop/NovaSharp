# Lua Compatibility (Target: 5.4.8)

NovaSharp aims to match the behaviour of upstream Lua **5.4.8**.

## Compatibility Principles

- Align runtime semantics, standard library behaviour, and coroutine semantics with stock Lua 5.4.8.
- Prefer behaviour parity over historic MoonSharp quirks; regressions against Lua take precedence.
- Track any intentional deviations (e.g., .NET integration helpers) in this document and mirror coverage tests where feasible.

## Known Gaps / Follow-Ups

| Area | Status | Notes |
| --- | --- | --- |
| Weak tables | 🚧 | Pending parity review; legacy MoonSharp implementation diverged. |
| `debug` module edge cases | 🚧 | Some APIs intentionally sandboxed; document alternatives. |
| TAP fixtures (`TestMore_308_io`, `TestMore_309_os`) | ⏸️ | Lua TAP suites disabled by default; enable when parity is verified on Windows/Linux. |
| `io.read("*n")` hex/exponent parsing | ⚠️ | Current runtime consumes only decimal prefixes (returns `0` for `0x` literals and `math.huge` for overflows). Lua 5.4.8 supports hexadecimal `p` exponents and leaves unread suffixes; test coverage marked inconclusive (`IoModuleTests`). |

If you spot behaviour that differs from Lua 5.4.8, file an issue and add the gap here (or update the status if a fix lands). Tests that cover Lua quirks should reference the section above so we keep parity visible.

## Lua 5.4 Feature Parity Matrix

| Feature / Change | NovaSharp Status | Notes | Coverage / Tracking |
| --- | --- | --- | --- |
| To-be-closed variables (`<close>` attribute) | ❌ Planned | Parser accepts syntax but runtime closes resources at GC time only. Needs VM support for `OP_CLOSE`. | Add NUnit coverage validating `__close`; track in PLAN Milestone 14. |
| Ephemeron tables / weak keys + values | 🚧 Partial | Weak tables implemented via legacy MoonSharp semantics; ephemeron behaviour not aligned with Lua 5.4 GC. | Expand `WeakTablesTests` once behaviour defined. |
| Upvalue join semantics (`debug.upvaluejoin`) | ✅ Parity | Tests in `DebugModuleTests` cover indexes and failure cases. | `DebugModuleTests.UpvalueJoin*`. |
| `goto` statement fixes (conditional labels, loops) | ✅ Parity | Parser + compiler follow Lua 5.4 post-5.3 semantics; verified in TAP suite `204-grammar.t`. | TAP fixture `204-grammar.t`. |
| `math.type` returns `"integer"` / `"float"` | ✅ Parity | Behaviour matches Lua 5.4; covered by `MathModuleTests`. | `MathModuleTests.MathType*`. |
| New `table.move` semantics (respecting metamethods) | ✅ Parity | Aligned via `TableModuleTests`. | `TableModuleTests.MoveHonoursMetamethods`. |
| `debug.getuservalue` handling of light userdata | ⏸️ Not Applicable | Light userdata unsupported in NovaSharp; documented limitation. | `DebugModuleTests`. |
| `io.popen` / `os.execute` updated return values | 🚧 Partial | CLI stubs return simplified exit codes; full status objects not implemented. | `OsSystemModuleTests.Execute` pending parity tests. |
| `string.pack` / `string.unpack` `c` options | ❌ Missing | Lua 5.4 adds complete binary pack/unpack; NovaSharp lacks implementation. | Add plan item / issue; no existing tests. |
| Lua 5.4 GC generational mode (`collectgarbage("incremental")`) | ❌ Missing | NovaSharp exposes limited GC controls; functionality not available. | Documented limitation; add test once behaviour defined. |
| Coroutine `close` metamethod | 🚧 Partial | `debug.debug` covers basic return/throw flows; coroutine closing semantics need targeted tests. | Add NUnit coverage for `coroutine.close`. |

Legend: ✅ parity, 🚧 partial/in progress, ❌ missing, ⏸️ intentionally unsupported.

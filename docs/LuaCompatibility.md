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

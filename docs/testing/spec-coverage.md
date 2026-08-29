# Lua Spec Conformance Coverage

This document tracks our progress in validating NovaSharp against the official Lua reference manuals. Each table maps manual sections to concrete test suites, edge-case coverage, and remaining work. Status values use the following shorthand:

- ✅ Covered by automated tests (unit, integration, or TAP fixtures)
- 🟡 Partial coverage; additional edge/error scenarios pending
- 🔴 No dedicated coverage yet
- 📚 Documentation-only follow-up (manual review required)

## Target Versions

NovaSharp currently aims to support the following Lua versions:

| Version       | Reference Manual                        | Notes                                                                                     |
| ------------- | --------------------------------------- | ----------------------------------------------------------------------------------------- |
| 5.2           | https://www.lua.org/manual/5.2/         | Legacy compatibility; ensure regressions stay aligned with older behaviour.               |
| 5.3           | https://www.lua.org/manual/5.3/         | Required for matching many community scripts; watch numeric changes (integers vs floats). |
| 5.4 (primary) | https://www.lua.org/manual/5.4/         | Canonical baseline for NovaSharp behaviour and new feature development.                   |
| 5.5 (preview) | https://www.lua.org/manual/5.5/ (draft) | Track emerging spec deltas so the harness can evolve quickly.                             |

## Lua 5.4 Reference Manual Coverage

| Section                       | Manual Link | Current Coverage | Planned Actions                                                                                             |
| ----------------------------- | ----------- | ---------------- | ----------------------------------------------------------------------------------------------------------- |
| 1. Introduction               | §1          | 📚               | Summarise interpreter guarantees; no automated tests required.                                              |
| 2. Basics                     | §2          | 🟡               | Confirm environment initialisation, chunk loading semantics, and script entry points.                       |
| 3. Types and Values           | §3          | 🟡               | Expand DynValue tests for NaN, +/-∞, userdata identity, light userdata, and thread equality.                |
| 4. Expressions                | §4          | 🟡               | Add spec-driven arithmetic, relational, logical, and concatenation tests (including metamethod fallbacks).  |
| 5. Statements                 | §5          | 🟡               | Harden TAP/Lua fixtures covering control structures (`goto`, to-be-closed variables, numeric/ generic for). |
| 6. Functions                  | §6          | 🟡               | Map call semantics, varargs, tail calls, and upvalues to unit tests plus coroutine TAP coverage.            |
| 7. Standard Libraries         | §6.1–6.10   | 🟡               | Track each sub-library (coroutine, package, string, utf8, table, math, io, os, debug) individually below.   |
| 8. The Standalone Interpreter | §7          | 🔴               | Capture CLI behaviour (shebang handling, arg table, error reporting) in spec harness.                       |

### Standard Library Matrix (Lua 5.4)

| Library                    | Manual Section | Current Status | Notes / TODO                                                                                                                                                                                                                        |
| -------------------------- | -------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `basic` (global functions) | §6.1           | 🟡             | `LuaBasicMultiVersionSpecTests` covers `tonumber` base parsing (2–36, invalid digits, base errors); still need harnesses for `pcall`, `_G`, and `tostring`.                                                                         |
| `coroutine`                | §6.2           | 🟡             | Expand tests for `coroutine.isyieldable`, wrap/resume error paths, to-be-closed interactions.                                                                                                                                       |
| `package`                  | §6.3           | 🔴             | Need harness for loader searchers, `package.config`, and `require` error sequencing.                                                                                                                                                |
| `string`                   | §6.4           | 🟡             | Harness covers manual examples for byte/char/sub/len/rep/find/reverse/format; add pattern/error-path coverage (`%f`, backtracking limits, `plain` flag).                                                                            |
| `utf8`                     | §6.5           | 🟡             | `LuaUtf8MultiVersionSpecTests` runs Lua 5.3+ coverage for `utf8.len`, `utf8.codepoint`, `utf8.codes`, `utf8.offset`, and `utf8.charpattern`, plus gating absent in Lua 5.2; extend to failure tuples for `utf8.char`/`utf8.insert`. |
| `table`                    | §6.6           | 🟡             | `LuaTableMoveMultiVersionSpecTests` exercises `table.move` across Lua 5.2–Latest (availability, overlapping ranges, destination defaults); extend to error tuples and metamethod interactions.                                      |
| `math`                     | §6.7           | 🟡             | `LuaMathMultiVersionSpecTests` now mirrors Lua 5.3+ behaviour for `math.type`, `math.tointeger`, and `math.ult` (including Lua 5.2 gating); still need spec-driven rounding/trig/error-path coverage.                               |
| `io`                       | §6.8           | 🟡             | Spec-driven behaviours for binary/text mode, file handle metamethods, and error messaging.                                                                                                                                          |
| `os`                       | §6.9           | 🟡             | Confirm locale-dependent formatting (`os.date`), `os.execute` return triples, and `os.time` defaults.                                                                                                                               |
| `debug`                    | §6.10          | 🔴             | Add spec-specific tests for `debug.getlocal`, `debug.upvaluejoin`, hook masks, and safety toggles.                                                                                                                                  |

## Lua 5.3 Reference Manual Coverage (Snapshot)

| Section                                      | Manual Link        | Status | Notes                                                                                                                                                                                                          |
| -------------------------------------------- | ------------------ | ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Core language deltas (integers, bitwise ops) | §3.4, §3.3, §3.4.3 | 🟡     | `BitwiseOperatorTests` + refreshed `BinaryOperatorExpressionTests` cover Lua 5.3 bitwise operators and floor division (Lua 5.3 manual §§3.4.1/3.4.7); still need spec-driven `%`/`math.type` regression tests. |
| Standard library differences                 | §6.\*              | 🔴     | Verify behaviour of `utf8` introduction, `table.move`, and `math` library changes compared to 5.4.                                                                                                             |

## Lua 5.2 Reference Manual Coverage (Snapshot)

| Section               | Manual Link | Status | Notes                                                                                   |
| --------------------- | ----------- | ------ | --------------------------------------------------------------------------------------- |
| Coroutine semantics   | §2.11       | 🟡     | Ensure legacy `coroutine.wrap` behaviours remain regression-free.                       |
| Module/package loader | §6.3        | 🔴     | Keep `package.loaded` and `package.seeall` compatibility tests for legacy integrations. |

## Tracking & Next Steps

1. **Spec Mapping** – Populate this document with a line item for every subsection of each manual, noting whether coverage exists (unit, TAP, or harness).
1. **Harness Implementation** – Build a reusable test driver (likely Lua-based fixtures executed via NUnit) that exercises example code from the manuals and asserts NovaSharp parity.
1. **Edge/Error Cases** – Record null, nil, out-of-range, and malformed scenarios for each API; ensure tests guard both success and failure paths.
1. **CI Integration** – Wire the conformance suite so failures block merges; consider per-version lanes if runtime options diverge.
1. **Documentation Sync** – Whenever coverage improves, update `docs/LuaCompatibility.md`, release notes, and this file to keep contributors aligned.

Keep detailed progress and spec links in this file. `PLAN.md` should name only selected unfinished conformance work; completed evidence belongs in `progress/`.

- ✅ (2025-11-21) Added the first multi-version harness (`LuaTableMoveMultiVersionSpecTests`) covering Lua 5.2–Latest `table.move` availability, overlapping-copy semantics, and the destination-default rule cited in Lua 5.3 manual §6.6.
- ✅ (2025-11-21) Expanded the harness with `LuaUtf8MultiVersionSpecTests`, which cites Lua 5.3 manual §6.5 scenarios for `utf8.len`, `utf8.codepoint`, `utf8.offset`, `utf8.codes`, and `utf8.charpattern` across Lua 5.2–Latest, ensuring the library stays hidden in Lua 5.2 and spec behaviours remain intact elsewhere.
- ✅ (2025-11-21) Added `LuaMathMultiVersionSpecTests` to exercise the Lua 5.3 manual §6.7 helpers (`math.type`, `math.tointeger`, `math.ult`) across compatibility profiles, including the Lua 5.2 absence checks, conversion edge cases, and unsigned comparisons.
- ✅ (2025-11-21) Added `LuaBasicMultiVersionSpecTests`, mirroring Lua 5.4 manual §6.1 expectations for `tonumber` with arbitrary bases (2–36), invalid numerals, and base argument validation.
- ✅ (2025-11-21) Introduced `BitwiseOperatorTests` + extended `BinaryOperatorExpressionTests` to mirror Lua 5.3 manual §§3.4.1/3.4.7 (bitwise & floor-division semantics, compatibility gating, shift saturation, unary `~`).

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

| Library                    | Manual Section | Current Status | Notes / TODO                                                                                                                                             |
| -------------------------- | -------------- | -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `basic` (global functions) | §6.1           | 🟡             | Verify `tonumber`, `tostring`, `pairs`, `_G`, `pcall`, `xpcall` edge cases.                                                                              |
| `coroutine`                | §6.2           | 🟡             | Expand tests for `coroutine.isyieldable`, wrap/resume error paths, to-be-closed interactions.                                                            |
| `package`                  | §6.3           | 🔴             | Need harness for loader searchers, `package.config`, and `require` error sequencing.                                                                     |
| `string`                   | §6.4           | 🟡             | Harness covers manual examples for byte/char/sub/len/rep/find/reverse/format; add pattern/error-path coverage (`%f`, backtracking limits, `plain` flag). |
| `utf8`                     | §6.5           | 🔴             | Build fixtures for `utf8.offset`, `utf8.len`, error returns, and continuation byte validation.                                                           |
| `table`                    | §6.6           | 🟡             | Add thorough tests for `table.move`, boundary errors, and in-place mutation semantics.                                                                   |
| `math`                     | §6.7           | 🟡             | Extend coverage to `math.tointeger`, `math.type`, and corner rounding rules.                                                                             |
| `io`                       | §6.8           | 🟡             | Spec-driven behaviours for binary/text mode, file handle metamethods, and error messaging.                                                               |
| `os`                       | §6.9           | 🟡             | Confirm locale-dependent formatting (`os.date`), `os.execute` return triples, and `os.time` defaults.                                                    |
| `debug`                    | §6.10          | 🔴             | Add spec-specific tests for `debug.getlocal`, `debug.upvaluejoin`, hook masks, and safety toggles.                                                       |

## Lua 5.3 Reference Manual Coverage (Snapshot)

| Section                                      | Manual Link        | Status | Notes                                                                                              |
| -------------------------------------------- | ------------------ | ------ | -------------------------------------------------------------------------------------------------- |
| Core language deltas (integers, bitwise ops) | §3.4, §3.3, §3.4.3 | 🔴     | Create targeted tests for integer division, `%`, `//`, bitwise operators, and `math.type`.         |
| Standard library differences                 | §6.\*              | 🔴     | Verify behaviour of `utf8` introduction, `table.move`, and `math` library changes compared to 5.4. |

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

Progress updates should be reflected in `PLAN.md` under “Lua Spec Conformance Harness” and linked back to the relevant spec sections here.

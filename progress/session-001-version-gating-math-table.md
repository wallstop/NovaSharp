# Version Gating: Math and Table Modules

**Date**: 2025-01-13
**Initiative**: §9 — Version-Aware Lua Standard Library Parity
**Scope**: `math.frexp`, `math.ldexp`, `table.pack`, `table.unpack`, global `unpack`

## Summary

Added proper `[LuaCompatibility]` version gating to deprecated and version-specific functions in the Math and Table modules, ensuring NovaSharp correctly reflects Lua's evolution across versions 5.1–5.4.

## Changes Made

### Math Module (`CoreLib/MathModule.cs`)

| Function     | Version Gate                       | Rationale                          |
| ------------ | ---------------------------------- | ---------------------------------- |
| `math.frexp` | `[LuaCompatibility(Lua51, Lua52)]` | Deprecated in 5.2, removed in 5.3+ |
| `math.ldexp` | `[LuaCompatibility(Lua51, Lua52)]` | Deprecated in 5.2, removed in 5.3+ |

**Background**: In Lua 5.3, `math.frexp` and `math.ldexp` were removed because the new integer subtype made these C-style floating-point decomposition functions less relevant. Users should use `//` and `%` for integer math.

### Table Module (`CoreLib/TableModule.cs`)

| Function          | Version Gate                       | Rationale                                    |
| ----------------- | ---------------------------------- | -------------------------------------------- |
| `table.pack`      | `[LuaCompatibility(Lua52)]`        | Added in 5.2                                 |
| `table.unpack`    | `[LuaCompatibility(Lua52)]`        | Added in 5.2 (moved from global)             |
| `unpack` (global) | `[LuaCompatibility(Lua51, Lua51)]` | Lua 5.1 only; moved to `table.unpack` in 5.2 |
| `pack` (global)   | **Removed**                        | Never existed in standard Lua                |

**Background**: Lua 5.2 reorganized table-related functions:

- Global `unpack` → `table.unpack`
- New `table.pack` function for variadic argument collection
- NovaSharp had an erroneous global `pack` that was never part of standard Lua

## Breaking Change

**Global `pack` removed**: This function was a NovaSharp extension that did not exist in any Lua version. Code using `pack(...)` must change to `table.pack(...)` with Lua 5.2+ compatibility.

## Tests Added

### MathModuleTUnitTests.cs

- `LdexpAvailableInLua51And52` — Verifies `math.ldexp` works in 5.1/5.2
- `LdexpIsNilInLua53Plus` — Verifies `math.ldexp` is nil in 5.3+
- `FrexpAvailableInLua51And52` — Verifies `math.frexp` works in 5.1/5.2
- `FrexpIsNilInLua53Plus` — Verifies `math.frexp` is nil in 5.3+

### TableModuleTUnitTests.cs

- `PackAvailableInLua52Plus` — Verifies `table.pack` works in 5.2+
- `PackIsNilInLua51` — Verifies `table.pack` is nil in 5.1
- `TableUnpackAvailableInLua52Plus` — Verifies `table.unpack` works in 5.2+
- `TableUnpackIsNilInLua51` — Verifies `table.unpack` is nil in 5.1
- `GlobalUnpackAvailableInLua51` — Verifies global `unpack` works in 5.1
- `GlobalUnpackIsNilInLua52Plus` — Verifies global `unpack` is nil in 5.2+
- `MaxnAvailableInLua51And52` — Verifies `table.maxn` works in 5.1/5.2
- `MaxnIsNilInLua53Plus` — Verifies `table.maxn` is nil in 5.3+

## Existing Tests Fixed

### Tests Updated to Specify Lua 5.2 Compatibility

- `MathModuleTUnitTests.FrexpWithFraction`
- `MathModuleTUnitTests.FrexpWithIntegerResultForFraction`
- `MathModuleTUnitTests.FrexpWithZero`
- `MathModuleTUnitTests.LdexpBasic`
- `MathModuleTUnitTests.LdexpEdgeCases`
- `MathModuleTUnitTests.LdexpWithNegativeExponent`
- `MathModuleTUnitTests.LdexpWithZero`

### Tests Updated to Use `table.pack` Instead of `pack`

- `SimpleTUnitTests.HashBang`
- `SimpleTUnitTests.NoHashBang`
- `SimpleTUnitTests.TestCallbacksWork`

### Tests Updated to Use `table.unpack` Instead of `unpack`

- `TableTUnitTests.UnpackTableWithExplicitKeys`

## TAP Suite Updates

### TapSuiteCatalog.cs

Added `306-math.t` to the Lua 5.2 compatibility override list because it tests `math.frexp` and `math.ldexp`.

## Test Results

```
Passed! - Failed: 0, Passed: 4934, Skipped: 0, Total: 4934, Duration: 8s 927ms
```

## Files Modified

| File                                             | Changes                                     |
| ------------------------------------------------ | ------------------------------------------- |
| `src/runtime/.../CoreLib/MathModule.cs`          | Added `[LuaCompatibility]` to frexp/ldexp   |
| `src/runtime/.../CoreLib/TableModule.cs`         | Added version gating, removed global `pack` |
| `src/tests/.../Modules/MathModuleTUnitTests.cs`  | 8 new tests, 7 tests fixed                  |
| `src/tests/.../Modules/TableModuleTUnitTests.cs` | 8 new tests, helper method                  |
| `src/tests/.../EndToEnd/SimpleTUnitTests.cs`     | 3 tests using `table.pack`                  |
| `src/tests/.../EndToEnd/TableTUnitTests.cs`      | 1 test using `table.unpack`                 |
| `src/tests/.../Tap/TapSuiteCatalog.cs`           | 306-math.t Lua 5.2 override                 |

## Lua Reference

- **Lua 5.1**: `math.frexp`, `math.ldexp`, global `unpack` all available
- **Lua 5.2**: `math.frexp`/`math.ldexp` deprecated; `table.pack`/`table.unpack` added
- **Lua 5.3+**: `math.frexp`/`math.ldexp` removed; only `table.pack`/`table.unpack`

See [Lua 5.4 Manual §6.7](https://www.lua.org/manual/5.4/manual.html#6.7) and [Lua 5.2 Incompatibilities](https://www.lua.org/manual/5.2/manual.html#8).

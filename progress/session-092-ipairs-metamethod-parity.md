# Session 092: ipairs Metamethod Parity and Struct Conversion Analysis

**Date**: 2025-12-22
**Focus**: Lua 5.3+ ipairs `__index` metamethod support, struct conversion feasibility analysis

______________________________________________________________________

## Summary

This session addressed multiple items from PLAN.md:

1. ✅ **ipairs Metamethod Parity** — Fixed `ipairs` to respect `__index` metamethods in Lua 5.3+
1. ❌ **SourceRef Struct Conversion** — Analyzed, found NOT FEASIBLE
1. ❌ **SymbolRef Struct Conversion** — Analyzed, found NOT FEASIBLE
1. ❌ **Instruction List Pooling** — Analyzed, found NOT APPLICABLE (ByteCode persists for Script lifetime)

______________________________________________________________________

## Part 1: ipairs Metamethod Parity ✅ COMPLETE

### Background

From Lua spec:

- **Lua 5.1-5.2**: `ipairs` uses raw access (ignores `__index`), supports `__ipairs` metamethod
- **Lua 5.3+**: `ipairs` respects `__index` metamethod when iterating, `__ipairs` deprecated

### Problem

NovaSharp's `ipairs` always used `table.RawGet()` for all Lua versions, ignoring `__index` metamethods even in Lua 5.3+.

### Solution

Modified `BasicModule.cs`:

1. **Added `GetMetamethodAwareIndex()` helper** (lines 177-227)

   - Tries raw access first
   - Falls back to `__index` metamethod if raw value is nil
   - Handles both function and table forms of `__index`
   - Includes loop protection (max depth 10)

1. **Modified `__ipairs_callback()`** to check Lua compatibility version

   - For Lua 5.3+: Uses `GetMetamethodAwareIndex()` for metamethod-aware indexing
   - For Lua 5.1/5.2: Continues using raw access via `RawGet()`

### Test Fixtures Created

| Fixture                              | Lua Versions  | Purpose                             |
| ------------------------------------ | ------------- | ----------------------------------- |
| `ipairs-metamethod-function-5.3.lua` | 5.3, 5.4, 5.5 | Tests `__index` function metamethod |
| `ipairs-metamethod-table-5.3.lua`    | 5.3, 5.4, 5.5 | Tests `__index` table metamethod    |
| `ipairs-metamethod-raw-5.1.lua`      | 5.1, 5.2      | Verifies raw access behavior        |
| `ipairs-metamethod-mixed-5.3.lua`    | 5.3, 5.4, 5.5 | Tests mixed raw/`__index` values    |

### TUnit Tests Added

- `IpairsRespectsIndexMetamethodFunctionLua53Plus`
- `IpairsRespectsIndexMetamethodTableLua53Plus`
- `IpairsUsesRawAccessLua51And52`
- `IpairsHandlesMixedRawAndMetamethodValues`
- Plus 7 additional version matrix tests

______________________________________________________________________

## Part 2: SourceRef Struct Conversion ❌ NOT FEASIBLE

### Investigation Findings

| Issue                    | Impact   | Details                                                                     |
| ------------------------ | -------- | --------------------------------------------------------------------------- |
| **Debugger Mutation**    | CRITICAL | `SourceRef.BreakpointId` is mutated during debugging                        |
| **Foreach Immutability** | CRITICAL | Debugger uses `foreach` + mutation which fails with structs                 |
| **Reference Identity**   | HIGH     | `BreakpointList` uses `List.Remove(SourceRef)` expecting reference equality |
| **Null Semantics**       | HIGH     | 15+ locations use `= null`, `== null`, nullable patterns                    |
| **ReferenceEquals**      | MEDIUM   | Tests use `ReferenceEquals` for identity checks                             |

### Required Architectural Changes (NOT RECOMMENDED)

To convert SourceRef to a struct would require:

1. External breakpoint tracking (new `BreakpointManager` class)
1. Replace `foreach` loops with index-based loops in debugger
1. Convert all null checks to `SourceRef.IsEmpty` pattern
1. Update all tests using `ReferenceEquals`

**Recommendation**: Do not proceed. Effort/risk ratio unfavorable.

______________________________________________________________________

## Part 3: SymbolRef Struct Conversion ❌ NOT FEASIBLE

### Investigation Findings

| Issue                             | Impact   | Details                                                       |
| --------------------------------- | -------- | ------------------------------------------------------------- |
| **Two-Phase Construction**        | CRITICAL | Symbols created with `index=-1`, later mutated to real index  |
| **Deserialization Back-Patching** | CRITICAL | Binary reader creates symbols, then patches `_environmentRef` |
| **Dictionary Key Identity**       | HIGH     | `Dictionary<SymbolRef, int>` uses reference equality          |
| **Null Semantics**                | HIGH     | `_environmentRef` can be null, checked in serialization       |
| **Test Mutations**                | MEDIUM   | Tests mutate `_indexValue` for error scenarios                |

### Architectural Coupling

SymbolRef is deeply integrated into:

- `BuildTimeScopeFrame` (two-phase construction)
- `ProcessorBinaryDump` (serialization with back-patching)
- Multiple dictionaries using reference equality

**Recommendation**: Consider `SymbolRef` pooling as an alternative to struct conversion.

______________________________________________________________________

## Part 4: Instruction List Pooling ❌ NOT APPLICABLE

### Analysis

The PLAN.md task "Instruction list → pooled array" was investigated:

- `ByteCode.Code` is a `List<Instruction>` that persists for the Script's lifetime
- Instructions are added during compilation and never converted to an array
- The list is used directly by the VM during execution
- There is no temporary list lifecycle suitable for pooling

**Conclusion**: The task description may have been intended for a different scenario (e.g., temporary lists during AST compilation). The main ByteCode instruction list is not a pooling candidate.

______________________________________________________________________

## Test Results

```
Total:    11,912 tests
Passed:   11,912
Failed:   0
Skipped:  0
Duration: 28.6s
```

______________________________________________________________________

## Files Modified

### Production Code

- `src/runtime/.../CoreLib/BasicModule.cs` — ipairs metamethod support

### Test Fixtures (New)

- `LuaFixtures/BasicModule/ipairs-metamethod-function-5.3.lua`
- `LuaFixtures/BasicModule/ipairs-metamethod-table-5.3.lua`
- `LuaFixtures/BasicModule/ipairs-metamethod-raw-5.1.lua`
- `LuaFixtures/BasicModule/ipairs-metamethod-mixed-5.3.lua`

### Test Code (New)

- `Units/CoreLib/BasicModuleTUnitTests.cs` — 11 new test methods

______________________________________________________________________

## PLAN.md Updates

Updated the following section:

```markdown
### ipairs Metamethod Changes (Lua 5.3+) ✅ **COMPLETE**

**Status**: ✅ Complete — Lua 5.3+ now respects `__index` metamethods during `ipairs` iteration.

**Implementation**:
- Added `GetMetamethodAwareIndex()` helper in `BasicModule.cs`
- `__ipairs_callback` now checks `LuaCompatibilityVersion` and uses metamethod-aware indexing for 5.3+
- Lua 5.1/5.2 continue using raw access (spec-compliant)
- 4 new Lua fixtures, 11 new TUnit tests

**Completed**: 2025-12-22
```

______________________________________________________________________

## Recommendations for Future Work

### Initiative 21 Phase 1 Remaining Items

The struct conversions (SourceRef, SymbolRef) and instruction pooling were found not feasible with current architecture. Consider:

1. **Alternative: SymbolRef Pooling** — Pool SymbolRef instances instead of converting to struct
1. **Alternative: Span-Based Lexer** — Higher impact, lower architectural risk
1. **Update PLAN.md** — Remove or mark struct conversion tasks as "Deferred - Requires Architectural Changes"

### Lua Spec Items

Continue with remaining testable items:

- `table.unpack` location verification (5.2+)
- `collectgarbage` options (5.4 deprecations)
- Literal integer overflow behavior (5.4)

______________________________________________________________________

## References

- Lua 5.3 Reference Manual §6.1 (ipairs behavior change)
- [BasicModule.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/BasicModule.cs)
- Initiative 21 in PLAN.md

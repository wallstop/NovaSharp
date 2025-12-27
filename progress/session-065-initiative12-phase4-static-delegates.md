# Session 065: Initiative 12 Phase 4 — Static Delegate & Struct Comparer Optimization

> **Date**: 2025-12-21\
> **Status**: ✅ **PARTIAL COMPLETE**\
> **Initiative**: [Initiative 12: Deep Codebase Allocation Analysis & Reduction](../PLAN.md#initiative-12-deep-codebase-allocation-analysis--reduction)\
> **Previous**: [Session 064: Phase 3 Value Type Migration](session-064-value-type-migration-phase3.md)

______________________________________________________________________

## Summary

Implemented Phase 4 Quick Wins from Initiative 12, focusing on eliminating closure allocations in frequently-called CoreLib module methods. Converted inline lambdas to cached static delegates and replaced a capturing closure with a struct-based comparer.

**Results**:

- ✅ All 11,754 tests pass
- ✅ 7 static delegates added to MathModule (eliminates allocations per call)
- ✅ 3 static delegates added to Bit32Module (eliminates allocations per call)
- ✅ 1 struct comparer added to TableModule (eliminates closure allocation in `table.sort`)
- ✅ No regressions introduced

______________________________________________________________________

## Optimizations Implemented

### 1. MathModule Static Delegate Caching

**File**: [src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/MathModule.cs)

Added cached static delegates to avoid allocation on each function call:

```csharp
// Cached static delegates to avoid allocations in hot paths (Initiative 12 Phase 4)
private static readonly Func<double, double, double> Atan2Op = Math.Atan2;
private static readonly Func<double, double, double> IEEERemainderOp = Math.IEEERemainder;
private static readonly Func<double, double, double> LdexpOp = (d1, d2) => d1 * Math.Pow(2, d2);
private static readonly Func<double, double, double> MaxOp = Math.Max;
private static readonly Func<double, double, double> MinOp = Math.Min;
private static readonly Func<double, double, double> PowOp = Math.Pow;
```

**Functions Updated**:

- `math.atan2` — Uses `Atan2Op`
- `math.fmod` — Uses `IEEERemainderOp`
- `math.mod` (Lua 5.1) — Uses `IEEERemainderOp`
- `math.ldexp` — Uses `LdexpOp`
- `math.max` — Uses `MaxOp`
- `math.min` — Uses `MinOp`
- `math.pow` — Uses `PowOp`

**Impact**: Eliminates 7 delegate allocations per call for these commonly-used math functions.

______________________________________________________________________

### 2. Bit32Module Static Delegate Caching

**File**: [src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/Bit32Module.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/Bit32Module.cs)

Added cached static delegates for bitwise operations:

```csharp
// Cached static delegates to avoid allocations in hot paths (Initiative 12 Phase 4)
private static readonly Func<uint, uint, uint> BitAndOp = (x, y) => x & y;
private static readonly Func<uint, uint, uint> BitOrOp = (x, y) => x | y;
private static readonly Func<uint, uint, uint> BitXorOp = (x, y) => x ^ y;
```

**Functions Updated**:

- `bit32.band` — Uses `BitAndOp`
- `bit32.btest` — Uses `BitAndOp`
- `bit32.bor` — Uses `BitOrOp`
- `bit32.bxor` — Uses `BitXorOp`

**Impact**: Eliminates 4 delegate allocations per call for these bitwise operations.

______________________________________________________________________

### 3. TableModule Struct Comparer

**File**: [src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/TableModule.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/TableModule.cs)

Replaced a capturing lambda with a `readonly struct` comparer:

**Before**:

```csharp
values.Sort((a, b) => SortComparer(executionContext, a, b, lt));
```

**After**:

```csharp
private readonly struct LuaSortComparer : IComparer<DynValue>
{
    private readonly ScriptExecutionContext _ctx;
    private readonly DynValue _lt;

    public LuaSortComparer(ScriptExecutionContext ctx, DynValue lt)
    {
        _ctx = ctx;
        _lt = lt;
    }

    public int Compare(DynValue a, DynValue b) => SortComparer(_ctx, a, b, _lt);
}

// In Sort method:
values.Sort(new LuaSortComparer(executionContext, lt));
```

**Impact**: Eliminates closure allocation (capturing `executionContext` and `lt`) on every `table.sort` call. The struct comparer is stack-allocated instead of heap-allocated.

______________________________________________________________________

## Allocation Impact Analysis

### Per-Call Savings Estimate

| Function     | Previous Allocation  | After   | Savings       |
| ------------ | -------------------- | ------- | ------------- |
| `math.max`   | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `math.min`   | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `math.atan2` | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `math.fmod`  | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `math.pow`   | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `math.ldexp` | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `bit32.band` | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `bit32.bor`  | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `bit32.bxor` | ~32 bytes (delegate) | 0 bytes | 32 bytes/call |
| `table.sort` | ~64 bytes (closure)  | 0 bytes | 64 bytes/call |

______________________________________________________________________

## Test Results

```
Test run summary: Passed! 
  total: 11754
  failed: 0
  succeeded: 11754
  skipped: 0
  duration: 31s 180ms
```

______________________________________________________________________

## Research Findings for Future Work

The sub-agent research identified additional optimization opportunities for Phase 4 continuation:

### High-Priority Items (Deferred)

1. **VM Single-Element Array Pooling**

   - Location: `ProcessorInstructionLoop.cs:1471`
   - Pattern: `new DynValue[1] { _valueStack.Pop() }`
   - Issue: Array lifetime extends into `Continuation.Invoke` — needs careful lifetime analysis

1. **KopiLua MatchState Pooling**

   - Location: `LuaPort/Libraries/lstrlib.cs`
   - Pattern: `new MatchState(...)` per pattern match
   - Impact: HIGH — called for every `string.match/gsub/gmatch`
   - This should be addressed as part of Initiative 10 (KopiLua Performance)

1. **Varargs Array Pooling**

   - Location: `ProcessorInstructionLoop.cs:1238`
   - Pattern: Variable-length array allocation for varargs
   - Issue: Complex lifetime management needed

### Medium-Priority Items

4. **ToArray() Calls in Coroutine**

   - Location: `Coroutine.cs:55,58`
   - Pattern: `ToArray()` for yield/tail call results
   - Could potentially use pooled arrays with careful lifetime tracking

1. **CallbackArguments.GetArray() Optimization**

   - Location: `CallbackArguments.cs:131`
   - Could add pooled variant: `GetArrayPooled(skip, out array)`

______________________________________________________________________

## Files Modified

1. **MathModule.cs** — Added 6 static delegate fields, updated 7 method implementations
1. **Bit32Module.cs** — Added 3 static delegate fields, updated 4 method implementations
1. **TableModule.cs** — Added `LuaSortComparer` struct, updated `Sort` method

______________________________________________________________________

## Phase 4 Checklist Status

From [PLAN.md](../PLAN.md):

- [x] Replace inline lambdas with cached static delegates — MathModule, Bit32Module
- [x] Convert closure-heavy patterns to struct-based alternatives — TableModule LuaSortComparer
- [ ] Implement object pooling for frequently allocated types — Deferred (complex lifetime)
- [ ] Replace LINQ chains with manual iteration in hot paths — Not found in hot paths
- [ ] Migrate string operations to span-based APIs — Deferred to Initiative 10

______________________________________________________________________

## Recommendations for Next Session

1. **Continue Phase 4** with array pooling where lifetimes are well-contained
1. **Start Initiative 10** (KopiLua Performance) — MatchState pooling is highest-impact remaining item
1. **Benchmark** to quantify allocation reduction from these changes

______________________________________________________________________

## Related Documents

- [PLAN.md — Initiative 12](../PLAN.md#initiative-12-deep-codebase-allocation-analysis--reduction)
- [Phase 1 Analysis](../docs/performance/allocation-analysis-initiative12-phase1.md)
- [Phase 2 Quick Wins](session-062-initiative12-phase2-quick-wins.md)
- [Phase 3 Value Type Migration](session-064-value-type-migration-phase3.md)
- [High-Performance C# Guidelines](../.llm/skills/high-performance-csharp.md)

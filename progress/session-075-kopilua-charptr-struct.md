# Session 075: KopiLua Performance Optimization — Phase 2: CharPtr Struct Conversion

**Date**: 2025-12-21
**Initiative**: 10 (KopiLua Performance Hyper-Optimization)
**Phase**: 2 — CharPtr Class-to-Struct Conversion

______________________________________________________________________

## Summary

Converted `CharPtr` from a class to a `readonly struct`, eliminating the largest source of heap allocations in string pattern matching operations. This single change achieved **53-63% reduction in execution time** and **58-85% reduction in memory allocations** for most pattern matching scenarios.

## Key Accomplishments

### 1. CharPtr Struct Conversion

Converted `LuaPort/CharPtr.cs` from a class to a `readonly struct`:

| Change        | Description                                                     |
| ------------- | --------------------------------------------------------------- |
| Type          | `class CharPtr` → `readonly struct CharPtr`                     |
| Fields        | Made `chars` and `index` `readonly`                             |
| Null handling | Added `static CharPtr Null => default;` and `IsNull` property   |
| Equality      | Implemented `IEquatable<CharPtr>` for efficient equality        |
| Hash code     | Changed from always-0 to proper `HashCodeHelper.HashCode()`     |
| Operators     | Removed `EnsureArgument` validation in hot paths                |
| Constructors  | Updated to throw `ArgumentNullException` for null CharPtr input |
| Methods       | Removed `Inc()` and `Dec()` (incompatible with readonly struct) |

### 2. Dependent File Updates

Updated files using CharPtr to work with new struct semantics:

| File                      | Changes                                                               |
| ------------------------- | --------------------------------------------------------------------- |
| `LuaPort/StringLib.cs`    | Replaced `return null;` with `CharPtr.Null`, `== null` with `.IsNull` |
| `LuaPort/StringFormat.cs` | Replaced `return null;` with `CharPtr.Null`                           |
| `LuaPort/LuaL.cs`         | Updated `EnsurePointer` to use `.IsNull` instead of `== null`         |

### 3. Test Updates

Updated test files to reflect new struct semantics:

| File                     | Changes                                                                |
| ------------------------ | ---------------------------------------------------------------------- |
| `CharPtrTUnitTests.cs`   | ~25 tests updated for value equality, `IsNull`, and exception behavior |
| `StringLibTUnitTests.cs` | Updated null check assertions to use `IsNull`                          |

______________________________________________________________________

## Performance Results

### Benchmark Comparison (100 iterations each)

| Scenario         | Before Time | After Time         | Time Reduction | Before Alloc | After Alloc     | Alloc Reduction |
| ---------------- | ----------- | ------------------ | -------------- | ------------ | --------------- | --------------- |
| MatchSimple      | 107.9 µs    | **73.9-82.2 µs**   | **~24-31%**    | 818.86 KB    | **342.3 KB**    | **~58%**        |
| MatchComplex     | 406.2 µs    | **178.9-190.2 µs** | **~53-56%**    | 2,452.77 KB  | **363.71 KB**   | **~85%**        |
| GsubSimple       | 598.0 µs    | **220.0-238.0 µs** | **~60-63%**    | 3,133.6 KB   | **588.29 KB**   | **~81%**        |
| GsubWithCaptures | 977.1 µs    | **413.8-449.2 µs** | **~54-58%**    | 4,334.33 KB  | **929.64 KB**   | **~79%**        |
| FormatMultiple   | 591.5 µs    | **587.9-593.8 µs** | ~0%            | 2,458.54 KB  | **2,067.91 KB** | **~16%**        |

### Per-Operation Allocation Rates

| Scenario         | Before   | After        | Reduction |
| ---------------- | -------- | ------------ | --------- |
| MatchSimple      | ~8.2 KB  | **~3.4 KB**  | **~59%**  |
| MatchComplex     | ~24.5 KB | **~3.6 KB**  | **~85%**  |
| GsubSimple       | ~31.3 KB | **~5.9 KB**  | **~81%**  |
| GsubWithCaptures | ~43.3 KB | **~9.3 KB**  | **~79%**  |
| FormatMultiple   | ~24.6 KB | **~20.7 KB** | **~16%**  |

### Key Insights

1. **Pattern matching operations saw massive improvements** — The more complex the pattern, the greater the benefit from eliminating CharPtr allocations.

1. **MatchComplex achieved the best allocation reduction (85%)** — This scenario creates many CharPtr instances during capture group handling.

1. **FormatMultiple saw minimal improvement** — Format operations allocate elsewhere (regex, string building), so CharPtr struct conversion had less impact.

1. **GC pressure dramatically reduced** — Gen0 collections dropped significantly across all scenarios.

______________________________________________________________________

## Technical Notes

### Semantic Changes

Converting `CharPtr` to a struct required careful handling of several semantic differences:

1. **Value vs Reference Semantics**: Structs are copied by value. Code that relied on reference equality or mutation through a reference needed updates.

1. **Null Handling**: Structs cannot be null. Introduced `CharPtr.Null` (returns `default`) and `IsNull` property to check for "no match" scenarios.

1. **Immutability**: The `Inc()` and `Dec()` methods were removed since readonly structs cannot have mutating members. The existing `Next()` and `Prev()` methods already return new instances.

1. **Equality**: Changed from reference equality to value equality, comparing both `chars` array reference and `index` value.

### Performance Optimizations

1. **Removed validation in hot paths**: The `EnsureArgument` checks in operators were removed since they added overhead in performance-critical code.

1. **Added `MethodImpl(AggressiveInlining)`**: Key methods marked for inlining to eliminate method call overhead.

1. **Proper hash code implementation**: Changed from always returning 0 to using `HashCodeHelper.HashCode()` for better hash distribution in collections.

______________________________________________________________________

## Files Modified

| File                      | Lines Changed  | Description                        |
| ------------------------- | -------------- | ---------------------------------- |
| `LuaPort/CharPtr.cs`      | Major refactor | Class → readonly struct conversion |
| `LuaPort/StringLib.cs`    | ~15 lines      | Null handling updates              |
| `LuaPort/StringFormat.cs` | ~5 lines       | Null handling updates              |
| `LuaPort/LuaL.cs`         | ~3 lines       | IsNull check updates               |
| `CharPtrTUnitTests.cs`    | ~50 lines      | Test semantics updates             |
| `StringLibTUnitTests.cs`  | ~5 lines       | IsNull assertion updates           |

______________________________________________________________________

## Verification

```bash
# Build passed
./scripts/build/quick.sh

# All tests pass
./scripts/test/quick.sh --no-build
# Result: 11,755 tests passed, 0 failed

# Benchmarks executed
dotnet run --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/ -c Release -- \
  --filter "*StringPattern*" --job short
```

______________________________________________________________________

## Next Steps (Phase 3)

Phase 2 completed the highest-impact optimization. Remaining Phase 3 work:

1. **Replace `char[]` allocations with `stackalloc`/`ArrayPool`**

   - Target `Scanformat()` in StringFormat.cs (540-byte allocations per `%` specifier)
   - Estimated: 1 day

1. **Pool `MatchState` and convert `Capture` to struct**

   - `MatchState` creates 32 `Capture` objects per match (~1 KB)
   - Estimated: 1-2 days

1. **Replace O(n²) string concatenation in `addquoted()`**

   - Use `ZStringBuilder` instead of `buff = buff + char` pattern
   - Estimated: 0.5 day

______________________________________________________________________

## Progress vs Targets

| Metric                         | Phase 1 Baseline | Phase 2 Result | Target   | Progress         |
| ------------------------------ | ---------------- | -------------- | -------- | ---------------- |
| Allocations per `string.match` | ~8.2 KB          | **~3.4 KB**    | \<0.5 KB | **59% → target** |
| Allocations per `string.gsub`  | ~31.3 KB         | **~5.9 KB**    | \<2 KB   | **81% → target** |
| Mean latency `MatchSimple`     | 107.9 µs         | **~78 µs**     | \<60 µs  | **28% → target** |
| Mean latency `GsubSimple`      | 598.0 µs         | **~229 µs**    | \<300 µs | **✅ ACHIEVED**  |

______________________________________________________________________

## Related

- [Phase 1: Baseline Measurements](session-074-kopilua-optimization-phase1.md)
- [Initiative 10 in PLAN.md](../PLAN.md#initiative-10-kopilua-performance-hyper-optimization)
- [High-Performance C# Guidelines](../.llm/skills/high-performance-csharp.md)

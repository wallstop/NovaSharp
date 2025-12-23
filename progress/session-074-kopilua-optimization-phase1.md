# Session 074: KopiLua Performance Optimization — Phase 1 Infrastructure

**Date**: 2025-12-21
**Initiative**: 10 (KopiLua Performance Hyper-Optimization)
**Phase**: 1 — Infrastructure & Baseline Measurements

______________________________________________________________________

## Summary

Completed Phase 1 of Initiative 10: established baseline allocation measurements for string pattern matching operations and performed comprehensive code analysis of the KopiLua string library.

## Key Accomplishments

### 1. Baseline Benchmark Infrastructure

Created `StringPatternBenchmarks.cs` with 5 representative scenarios:

| Scenario           | Description                                                         |
| ------------------ | ------------------------------------------------------------------- |
| `MatchSimple`      | Find numbers with `%d+` pattern                                     |
| `MatchComplex`     | Parse key-value pairs with captures `(%a+)%s*=%s*(%d+)`             |
| `GsubSimple`       | Simple pattern replacement                                          |
| `GsubWithCaptures` | Replacement with capture group references                           |
| `FormatMultiple`   | Multiple format specifiers (`%s`, `%d`, `%.2f`, `%x`, `%o`, `%.3e`) |

### 2. Baseline Measurements

| Scenario         | Mean Time | Allocated       | Gen0  | Gen1 |
| ---------------- | --------- | --------------- | ----- | ---- |
| MatchSimple      | 107.9 µs  | **818.86 KB**   | 44.4  | 0.5  |
| MatchComplex     | 406.2 µs  | **2,452.77 KB** | 133.3 | 2.0  |
| GsubSimple       | 598.0 µs  | **3,133.6 KB**  | 170.4 | 3.4  |
| GsubWithCaptures | 977.1 µs  | **4,334.33 KB** | 235.4 | 4.9  |
| FormatMultiple   | 591.5 µs  | **2,458.54 KB** | 132.8 | 2.0  |

**Each scenario runs 100 iterations.** Per-operation allocation rates:

| Scenario         | Per-Operation Allocation |
| ---------------- | ------------------------ |
| MatchSimple      | ~8.2 KB                  |
| MatchComplex     | ~24.5 KB                 |
| GsubSimple       | ~31.3 KB                 |
| GsubWithCaptures | ~43.3 KB                 |
| FormatMultiple   | ~24.6 KB                 |

### 3. Code Analysis — Allocation Hotspots

#### CRITICAL Priority (Immediate Impact)

1. **`CharPtr` Class Allocations** (LuaPort/CharPtr.cs)

   - Every `new CharPtr()`, `++`, `--`, `+`, and indexer operation allocates
   - Called thousands of times per pattern match
   - **Impact**: 40-60% of total allocations
   - **Fix**: Convert to `readonly struct` with `ReadOnlySpan<char>` backend

1. **`addquoted()` Uses O(n²) String Concatenation** (LuaPort/StringFormat.cs:~85)

   - Loop with `buff = buff + char` pattern
   - **Impact**: Significant for long strings with escape sequences
   - **Fix**: Use `ZStringBuilder`

1. **`char[]` Buffers in `Scanformat`** (LuaPort/StringFormat.cs)

   - `new char[540]` allocated per `%` specifier
   - **Fix**: Use `stackalloc` or `ArrayPool<char>`

#### HIGH Priority

4. **`MatchState` + 32 `Capture` Objects** (LuaPort/StringLib.cs)

   - ~1 KB per match operation
   - **Fix**: Pool `MatchState`, make `Capture` a struct

1. **`LuaLBuffer` Creates New `CharPtr`** (LuaPort/StringLib.cs)

   - Every gsub/format call allocates
   - **Fix**: Pool or make `LuaLBuffer` use direct char arrays

1. **`start_capture()` Creates Defensive `CharPtr` Copies** (LuaPort/StringLib.cs)

   - 2 allocations × recursion depth
   - **Fix**: Pass by value with struct

#### MEDIUM Priority

7. **`GMatchAuxFunc` Class** — one per gmatch call
1. **`Regex.Match()` Allocations** in Tools.StringFormat
1. **`Regex.Matches()` Called Inside Loop** in StringFormat

______________________________________________________________________

## Files Analyzed

| File                      | Lines | Purpose                | Allocation Issues                   |
| ------------------------- | ----- | ---------------------- | ----------------------------------- |
| `LuaPort/StringLib.cs`    | 1,300 | Main string library    | HIGH — MatchState, Capture, CharPtr |
| `LuaPort/CharPtr.cs`      | 395   | Pointer emulation      | CRITICAL — class allocations        |
| `LuaPort/LuaLBuffer.cs`   | 20    | String builder wrapper | HIGH — CharPtr creation             |
| `LuaPort/LuaL.cs`         | 428   | Lua C API layer        | MEDIUM — buffer operations          |
| `LuaPort/LuaConf.cs`      | 277   | C library ports        | LOW — utility methods               |
| `LuaPort/StringFormat.cs` | 1,013 | Printf utilities       | CRITICAL — char[], string concat    |

______________________________________________________________________

## Target Metrics (Post-Optimization)

| Metric                          | Current  | Target   | Reduction |
| ------------------------------- | -------- | -------- | --------- |
| Allocations per `string.match`  | ~8.2 KB  | \<0.5 KB | **94%**   |
| Allocations per `string.gsub`   | ~31.3 KB | \<2 KB   | **94%**   |
| Allocations per `string.format` | ~24.6 KB | \<1 KB   | **96%**   |
| Mean latency `MatchSimple`      | 107.9 µs | \<60 µs  | **44%**   |
| Mean latency `GsubSimple`       | 598.0 µs | \<300 µs | **50%**   |

______________________________________________________________________

## Next Steps (Phase 2)

1. **Convert `CharPtr` from class to `readonly struct`**

   - Most impactful single change
   - Requires careful audit of all usage patterns
   - Estimated: 2-3 days

1. **Replace `char[]` allocations with `stackalloc`/`ArrayPool`**

   - Target `Scanformat()` in StringFormat.cs
   - Estimated: 1 day

1. **Pool `MatchState` and convert `Capture` to struct**

   - Requires lifecycle analysis
   - Estimated: 1-2 days

______________________________________________________________________

## Files Modified

- **Created**: `src/tooling/WallstopStudios.NovaSharp.Benchmarks/StringPatternBenchmarks.cs`
  - `StringPatternScenario` enum
  - `StringPatternSuites` static class with Lua scripts
  - `StringPatternBenchmarks` benchmark class with `[MemoryDiagnoser]`

______________________________________________________________________

## Verification

```bash
# Build passed
./scripts/build/quick.sh --all

# Benchmarks executed successfully
dotnet run --project src/tooling/WallstopStudios.NovaSharp.Benchmarks/ -c Release -- \
  --filter "*StringPattern*" --job short --exporters json
```

All 11,754 tests continue to pass.

______________________________________________________________________

## Related

- [Initiative 10 in PLAN.md](../PLAN.md#initiative-10-kopilua-performance-hyper-optimization)
- [High-Performance C# Guidelines](../.llm/skills/high-performance-csharp.md)
- [Allocation Analysis Phase 1](../docs/performance/allocation-analysis-initiative12-phase1.md)

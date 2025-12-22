# Initiative 12: Deep Codebase Allocation Analysis & Reduction

## Phase 1: Profiling & Baseline Report

> **Date**: December 21, 2025
> **Status**: Analysis Complete
> **Related**: [PLAN.md Initiative 12](../../PLAN.md), [pooling-and-allocation-audit-2025-12.md](./pooling-and-allocation-audit-2025-12.md)

______________________________________________________________________

## Table of Contents

1. [Executive Summary](#1-executive-summary)
1. [Existing Benchmark Infrastructure](#2-existing-benchmark-infrastructure)
1. [Top Allocation Sites](#3-top-allocation-sites)
1. [Priority-Ordered Remediation List](#4-priority-ordered-remediation-list)
1. [Baseline Allocation Rates](#5-baseline-allocation-rates)
1. [Appendix: Already Optimized Patterns](#appendix-already-optimized-patterns)

______________________________________________________________________

## 1. Executive Summary

This report documents the Phase 1 allocation profiling analysis for Initiative 12. The codebase has already implemented several pooling optimizations, but significant opportunities remain.

### Key Findings

| Category               | Finding                                                                                                                                                    |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Already Pooled**     | `DynValueArrayPool`, `ObjectArrayPool`, `CallStackItemPool`, `ListPool<T>`, `HashSetPool<T>`, `DictionaryPool<K,V>`, `GenericPool<T>`, ZString integration |
| **High-Impact Gaps**   | Lambda closures in hot paths, `List<List<SymbolRef>>` allocations per function call, string concatenation in error messages                                |
| **Medium-Impact Gaps** | Pattern matching allocations in KopiLua, remaining `new DynValue[]` in interop paths, Closure/ClosureContext creation                                      |
| **Low-Impact Gaps**    | Parse-time allocations (acceptable), debug-only paths, cold interop paths                                                                                  |

### Baseline Metrics (from existing benchmarks)

| Scenario           | Allocated | Notes                                 |
| ------------------ | --------- | ------------------------------------- |
| NumericLoops       | 824 B     | Good - primarily pooled arrays        |
| CoroutinePipeline  | 1.16 KB   | Moderate - context switching overhead |
| TableMutation      | 25.3 KB   | High - Table expansion allocations    |
| UserDataInterop    | 1.42 KB   | Moderate - reflection overhead        |
| Script Load (Tiny) | 2.52 MB   | High - includes parser, bytecode gen  |

______________________________________________________________________

## 2. Existing Benchmark Infrastructure

### BenchmarkDotNet Setup ✅

All existing benchmarks have `[MemoryDiagnoser]` enabled:

| Benchmark Class            | Location                                                                                                                          | Memory Diagnoser |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------- | ---------------- |
| `RuntimeBenchmarks`        | [src/tooling/.../RuntimeBenchmarks.cs](../../src/tooling/WallstopStudios.NovaSharp.Benchmarks/RuntimeBenchmarks.cs)               | ✅ Enabled       |
| `ScriptLoadingBenchmarks`  | [src/tooling/.../ScriptLoadingBenchmarks.cs](../../src/tooling/WallstopStudios.NovaSharp.Benchmarks/ScriptLoadingBenchmarks.cs)   | ✅ Enabled       |
| `LuaPerformanceBenchmarks` | [src/tooling/.../LuaPerformanceBenchmarks.cs](../../src/tooling/WallstopStudios.NovaSharp.Comparison/LuaPerformanceBenchmarks.cs) | ✅ Enabled       |

### Benchmark Scenarios Covered

| Scenario               | Coverage                           |
| ---------------------- | ---------------------------------- |
| Function call overhead | ✅ NumericLoops                    |
| Table manipulation     | ✅ TableMutation                   |
| Coroutine switching    | ✅ CoroutinePipeline               |
| CLR interop            | ✅ UserDataInterop                 |
| Script compilation     | ✅ Compile + Execute, Compile Only |
| Precompiled execution  | ✅ Execute Precompiled             |

### Missing Benchmark Scenarios

| Scenario                                               | Impact | Recommendation   |
| ------------------------------------------------------ | ------ | ---------------- |
| String pattern matching (`string.find`, `string.gsub`) | HIGH   | Add benchmark    |
| Deep closure capture                                   | MEDIUM | Add benchmark    |
| Error handling paths                                   | LOW    | Skip (cold path) |

______________________________________________________________________

## 3. Top Allocation Sites

### 3.1 CRITICAL - VM Hot Path (Execution Loop)

| #   | File                                                                                                                                                       | Method/Location     | Pattern                          | Frequency                | Est. Size   | Recommended Fix                         |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- | -------------------------------- | ------------------------ | ----------- | --------------------------------------- |
| 1   | [ProcessorInstructionLoop.cs#L599](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L599)        | `ExecEnter`         | `new List<List<SymbolRef>>()`    | Every block entry        | 64+ bytes   | Pre-allocate in `CallStackItem` or pool |
| 2   | [ProcessorInstructionLoop.cs#L601](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L601)        | `ExecEnter`         | `new List<SymbolRef>(closers)`   | Every block with closers | 32+ bytes   | Use pooled list                         |
| 3   | [ProcessorInstructionLoop.cs#L608](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L608)        | `ExecEnter`         | `new HashSet<int>()`             | Every block with closers | 48+ bytes   | Pre-allocate in `CallStackItem`         |
| 4   | [ProcessorInstructionLoop.cs#L1063](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L1063)      | `ExecBeginFn`       | `new DynValue[i.NumVal]`         | Every function call      | Variable    | Pool for ≤16 locals                     |
| 5   | [ProcessorInstructionLoop.cs#L1209](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L1209)      | `ExecArgs`          | `new DynValue[len]` (varargs)    | Every vararg function    | Variable    | Use `DynValueArrayPool`                 |
| 6   | [ProcessorInstructionLoop.cs#L1718-1756](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs#L1718) | `ExecBitwiseBinary` | Lambda capture `(x, y) => x & y` | Every bitwise op         | 32-64 bytes | Use static delegates                    |

### 3.2 HIGH - Data Types

| #   | File                                                                                                                     | Method/Location   | Pattern                       | Frequency              | Est. Size | Recommended Fix            |
| --- | ------------------------------------------------------------------------------------------------------------------------ | ----------------- | ----------------------------- | ---------------------- | --------- | -------------------------- |
| 7   | [ClosureContext.cs#L81](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/Scopes/ClosureContext.cs#L81)  | Constructor       | `new string[symbols.Length]`  | Every closure creation | Variable  | Consider pooling or intern |
| 8   | [Coroutine.cs#L287](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/Coroutine.cs#L287)                 | `Resume`          | `new DynValue[args.Length]`   | Every coroutine resume | Variable  | Use `DynValueArrayPool`    |
| 9   | [Coroutine.cs#L364](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/Coroutine.cs#L364)                 | `GetStackTrace`   | `.ToArray()`                  | Debug calls only       | Variable  | Cold path - acceptable     |
| 10  | [CallbackArguments.cs#L131](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/CallbackArguments.cs#L131) | `GetArray`        | `new DynValue[_count - skip]` | CLR callbacks          | Variable  | Consider pooled overload   |
| 11  | [YieldRequest.cs#L55](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/YieldRequest.cs#L55)             | `GetReturnValues` | `.ToArray()`                  | Every yield            | Variable  | Use pooled conversion      |
| 12  | [TailCallData.cs#L58](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/TailCallData.cs#L58)             | `GetArgs`         | `.ToArray()`                  | Every tail call        | Variable  | Use pooled conversion      |

### 3.3 HIGH - CoreLib (Standard Library)

| #   | File                                                                                                                       | Method/Location  | Pattern                                    | Frequency               | Est. Size     | Recommended Fix         |
| --- | -------------------------------------------------------------------------------------------------------------------------- | ---------------- | ------------------------------------------ | ----------------------- | ------------- | ----------------------- |
| 13  | [TableModule.cs#L66](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/TableModule.cs#L66)                   | `table.pack`     | `new DynValue[count]`                      | Every `table.pack` call | Variable      | Pool for common sizes   |
| 14  | [StringModule.cs#L459](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs#L459)               | `string.byte`    | `new DynValue[length]`                     | Every `string.byte`     | Variable      | Pool or use span        |
| 15  | [StringModule.cs#L69-132](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/StringModule.cs#L69)             | String metatable | Lambda closures for `__add`, `__sub`, etc. | Module init             | 8 × ~64 bytes | Static delegates        |
| 16  | [ErrorHandlingModule.cs#L52](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/ErrorHandlingModule.cs#L52)   | `pcall`          | `new DynValue[args.Count - 1]`             | Every `pcall`           | Variable      | Use `DynValueArrayPool` |
| 17  | [ErrorHandlingModule.cs#L133](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/ErrorHandlingModule.cs#L133) | `xpcall`         | `new DynValue[args.Count + 1]`             | Every `xpcall`          | Variable      | Use `DynValueArrayPool` |
| 18  | [Utf8Module.cs#L269](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/Utf8Module.cs#L269)                   | `utf8.codepoint` | `new char[bytes.Count]`                    | Every codepoint call    | Variable      | Pool or stackalloc      |

### 3.4 HIGH - LuaPort (Pattern Matching)

| #   | File                                                                                                                        | Method/Location | Pattern                                     | Frequency                     | Est. Size      | Recommended Fix          |
| --- | --------------------------------------------------------------------------------------------------------------------------- | --------------- | ------------------------------------------- | ----------------------------- | -------------- | ------------------------ |
| 19  | [KopiLuaStringLib.cs#L114](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs#L114)        | `MatchState`    | `new Capture[LuaPatternMaxCaptures]`        | Every pattern match           | 32 × 16+ bytes | Pool `MatchState` struct |
| 20  | [KopiLuaStringLib.cs#L1173-1174](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs#L1173) | `str_format`    | `new char[MaxFormat]`, `new char[MAX_ITEM]` | Every `string.format`         | 100+ bytes     | Use stackalloc or pool   |
| 21  | [LuaState.cs#L76](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/LuaStateInterop/LuaState.cs#L76)          | `PopResults`    | `new DynValue[num]`                         | Every KopiLua function return | Variable       | Use `DynValueArrayPool`  |

### 3.5 MEDIUM - Interop

| #   | File                                                                                                                                                                                                     | Method/Location | Pattern                                    | Frequency                      | Est. Size | Recommended Fix                              |
| --- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------- | ------------------------------------------ | ------------------------------ | --------- | -------------------------------------------- |
| 22  | [PropertyMemberDescriptor.cs#L388](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/StandardDescriptors/ReflectionMemberDescriptors/PropertyMemberDescriptor.cs#L388)                     | Setter          | `new object[] { convertedValue }`          | Every property set             | 32+ bytes | Already has `ObjectArrayPool` - verify usage |
| 23  | [EventMemberDescriptor.cs#L329](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/StandardDescriptors/ReflectionMemberDescriptors/EventMemberDescriptor.cs#L329)                           | Add handler     | `new object[] { handler }`                 | Event subscribe                | 32+ bytes | Use `ObjectArrayPool`                        |
| 24  | [DispatchingUserDataDescriptor.cs#L671](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/BasicDescriptors/DispatchingUserDataDescriptor.cs#L671)                                          | Index           | `new DynValue[] { index }`                 | Every indexer access           | 32+ bytes | Use `DynValueArrayPool`                      |
| 25  | [FunctionMemberDescriptorBase.cs#L372](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/StandardDescriptors/MemberDescriptors/FunctionMemberDescriptorBase.cs#L372)                       | Out params      | `new DynValue[outParams.Count + 1]`        | Methods with out params        | Variable  | Use pooled array                             |
| 26  | [OverloadedMethodMemberDescriptor.cs#L283-284](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/StandardDescriptors/ReflectionMemberDescriptors/OverloadedMethodMemberDescriptor.cs#L283) | Cache miss      | `new List<DataType>()`, `new List<Type>()` | Overload resolution cache miss | 64+ bytes | Pool or pre-size                             |

### 3.6 MEDIUM - Script/Serialization

| #   | File                                                                                                                                   | Method/Location    | Pattern                           | Frequency                 | Est. Size | Recommended Fix         |
| --- | -------------------------------------------------------------------------------------------------------------------------------------- | ------------------ | --------------------------------- | ------------------------- | --------- | ----------------------- |
| 27  | [Script.cs#L810](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs#L810)                                               | `Call(arg)`        | `new DynValue[] { arg }`          | Every single-arg call     | 32+ bytes | Add span-based overload |
| 28  | [Script.cs#L825](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Script.cs#L825)                                               | `Call(arg1, arg2)` | `new DynValue[] { arg1, arg2 }`   | Every two-arg call        | 40+ bytes | Add span-based overload |
| 29  | [SerializationExtensions.cs#L91](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Serialization/SerializationExtensions.cs#L91) | Serialize          | `"[" + SerializeValue(...) + "]"` | Every table key serialize | Variable  | Use ZString             |
| 30  | [JsonTableConverter.cs#L322](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Serialization/Json/JsonTableConverter.cs#L322)    | JSON escape        | `"\"" + s + "\""`                 | Every string in JSON      | Variable  | Use ZString             |

### 3.7 LOW - Parse/Build Time

| #   | File                                                                                                                                       | Method/Location | Pattern                                    | Frequency                     | Est. Size | Recommended Fix         |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------ | --------------- | ------------------------------------------ | ----------------------------- | --------- | ----------------------- |
| 31  | [BuildTimeScopeBlock.cs#L56](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/Scopes/BuildTimeScopeBlock.cs#L56)          | Constructor     | `new List<BuildTimeScopeBlock>()`          | Every scope block             | 32+ bytes | Cold path - acceptable  |
| 32  | [BuildTimeScopeBlock.cs#L171](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/Scopes/BuildTimeScopeBlock.cs#L171)        | Labels          | `new Dictionary<string, LabelStatement>()` | Rare (goto/label)             | 48+ bytes | Cold path - acceptable  |
| 33  | [RuntimeScopeFrame.cs#L34](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/Scopes/RuntimeScopeFrame.cs#L34)              | Constructor     | `new List<SymbolRef>()`                    | Every function def            | 32+ bytes | Parse time - acceptable |
| 34  | [FunctionCallExpression.cs#L49-73](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/FunctionCallExpression.cs#L49) | Parse           | `new List<Expression>()`                   | Every function call in source | 32+ bytes | Parse time - acceptable |

______________________________________________________________________

## 4. Priority-Ordered Remediation List

### Phase 2: Immediate (High-Impact, Low-Risk)

| Priority | Site #    | File                                                | Change                                                                                      | Est. Reduction             |
| -------- | --------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------- | -------------------------- |
| P1.1     | 1, 2, 3   | ProcessorInstructionLoop.cs                         | Pre-allocate `BlocksToClose` and `ToBeClosedIndices` in `CallStackItem`, reuse across calls | 100-200 B/call             |
| P1.2     | 6         | ProcessorInstructionLoop.cs                         | Replace lambda delegates with static delegates for bitwise ops                              | 256-512 B total (one-time) |
| P1.3     | 5, 16, 17 | ProcessorInstructionLoop.cs, ErrorHandlingModule.cs | Use `DynValueArrayPool` for varargs and pcall/xpcall                                        | 32-64 B/call               |
| P1.4     | 8         | Coroutine.cs                                        | Use `DynValueArrayPool` for Resume args                                                     | 32-64 B/resume             |
| P1.5     | 15        | StringModule.cs                                     | Use static delegates for string metatable operations                                        | ~512 B (one-time)          |

### Phase 3: Medium (Moderate-Impact)

| Priority | Site #     | File                                           | Change                                            | Est. Reduction     |
| -------- | ---------- | ---------------------------------------------- | ------------------------------------------------- | ------------------ |
| P2.1     | 19, 20     | KopiLuaStringLib.cs                            | Pool `MatchState` and char buffers                | 500-1000 B/match   |
| P2.2     | 4          | ProcessorInstructionLoop.cs                    | Pool `LocalScope` arrays for ≤16 locals           | 32-256 B/call      |
| P2.3     | 13, 14, 18 | TableModule.cs, StringModule.cs, Utf8Module.cs | Pool result arrays                                | Variable           |
| P2.4     | 21         | LuaState.cs                                    | Use `DynValueArrayPool` for PopResults            | 32+ B/interop call |
| P2.5     | 22-26      | Interop descriptors                            | Verify `ObjectArrayPool` usage, add where missing | 32-64 B/call       |

### Phase 4: Low (Optimization Pass)

| Priority | Site # | File                                              | Change                                      | Est. Reduction           |
| -------- | ------ | ------------------------------------------------- | ------------------------------------------- | ------------------------ |
| P3.1     | 27, 28 | Script.cs                                         | Add `Call(ReadOnlySpan<DynValue>)` overload | 32-48 B/call             |
| P3.2     | 29, 30 | SerializationExtensions.cs, JsonTableConverter.cs | Convert string concat to ZString            | Variable                 |
| P3.3     | 7      | ClosureContext.cs                                 | Consider symbol name interning              | Variable                 |
| P3.4     | 11, 12 | YieldRequest.cs, TailCallData.cs                  | Pooled array conversion                     | 32+ B/yield or tail call |

______________________________________________________________________

## 5. Baseline Allocation Rates

From existing BenchmarkDotNet results:

### Runtime Scenarios (per invocation)

| Scenario          | Mean Time | Allocated | Gen0   | Gen1   |
| ----------------- | --------- | --------- | ------ | ------ |
| NumericLoops      | 172 ns    | 824 B     | 0.0436 | -      |
| CoroutinePipeline | 242 ns    | 1,160 B   | 0.0615 | -      |
| TableMutation     | 4,529 ns  | 25,888 B  | 1.3733 | 0.1030 |
| UserDataInterop   | 305 ns    | 1,416 B   | 0.0749 | -      |

### Script Loading (per script)

| Complexity | Mean Time | Allocated |
| ---------- | --------- | --------- |
| Tiny       | 2.70 ms   | 2.52 MB   |
| Small      | 2.50 ms   | 2.55 MB   |
| Medium     | 71.4 ms   | 177 MB    |
| Large      | 1.156 s   | 3.53 GB   |

### Key Observations

1. **NumericLoops**: Already well-optimized via `DynValueArrayPool`
1. **TableMutation**: High allocations due to Table resize operations (expected)
1. **CoroutinePipeline**: Moderate - context switching overhead
1. **Script Loading**: Large scripts allocate heavily during parse/compile (expected)

______________________________________________________________________

## Appendix: Already Optimized Patterns

The following optimizations are already implemented:

### Pooling Infrastructure

| Pool                  | Location                                                                                                                       | Usage                                        |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------- |
| `DynValueArrayPool`   | [DataStructs/DynValueArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/DynValueArrayPool.cs)   | ✅ Used in `StackTopToArray`, error handling |
| `ObjectArrayPool`     | [DataStructs/ObjectArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ObjectArrayPool.cs)       | ✅ Used in interop reflection calls          |
| `CallStackItemPool`   | [Execution/VM/CallStackItemPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/CallStackItemPool.cs) | ✅ Pools call stack frames                   |
| `ListPool<T>`         | [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs)       | ✅ Used in `NewTupleNested`                  |
| `HashSetPool<T>`      | [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs)       | ✅ Available, not fully utilized             |
| `DictionaryPool<K,V>` | [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs)       | ✅ Available                                 |
| `GenericPool<T>`      | [DataStructs/GenericPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/GenericPool.cs)               | ✅ Base for collection pools                 |
| `PooledResource<T>`   | [DataStructs/PooledResource.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/PooledResource.cs)         | ✅ RAII pattern                              |

### String Handling

| Optimization                     | Location            | Notes                                   |
| -------------------------------- | ------------------- | --------------------------------------- |
| ZString integration              | `ZStringBuilder.cs` | ✅ Used in Script, Serialization, Lexer |
| `DynValue.NewConcatenatedString` | DynValue.cs         | ✅ ZString-based 2, 3, 4-arg variants   |
| `DynValue.JoinTupleStrings`      | DynValue.cs         | ✅ ZString-based joining                |

### Caching

| Cache                  | Location    | Notes                       |
| ---------------------- | ----------- | --------------------------- |
| Small integer cache    | DynValue.cs | ✅ 0-255, -256 to -1 cached |
| Common float cache     | DynValue.cs | ✅ 0.0, 1.0, -1.0, etc.     |
| Boolean singletons     | DynValue.cs | ✅ `True`, `False`, `Nil`   |
| Closure.CachedDynValue | Closure.cs  | ✅ Avoids repeated wrapping |

### VM Stack

| Optimization   | Location                 | Notes                           |
| -------------- | ------------------------ | ------------------------------- |
| `FastStack<T>` | DataStructs/FastStack.cs | ✅ Fixed-capacity, no resize    |
| `Slice<T>`     | DataStructs/Slice.cs     | ✅ Struct enumerator, no boxing |

______________________________________________________________________

## Next Steps (Phase 2)

1. Create benchmark for pattern matching to establish baseline
1. Implement P1.1-P1.5 changes (CallStackItem pre-allocation, static delegates)
1. Re-run benchmarks to measure improvement
1. Document before/after in [Performance.md](../Performance.md)

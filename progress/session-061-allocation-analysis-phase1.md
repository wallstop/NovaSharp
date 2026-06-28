# Session 061: Allocation Analysis Phase 1 — Profiling & Baseline

**Date**: 2025-12-21\
**Focus**: Initiative 12: Deep Codebase Allocation Analysis — Phase 1\
**Status**: ✅ Completed

## Summary

Completed Phase 1 of Initiative 12, establishing a comprehensive baseline of allocation hotspots across the NovaSharp interpreter. This analysis identifies the top allocation sites and provides a priority-ordered remediation list for subsequent phases.

## Deliverables

| Artifact             | Location                                                                                                                      | Description                                                                      |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Full Analysis Report | [docs/performance/allocation-analysis-initiative12-phase1.md](../docs/performance/allocation-analysis-initiative12-phase1.md) | Detailed report with 34 allocation sites, baseline metrics, and remediation plan |

## Key Findings

### Existing Optimization Infrastructure ✅

The codebase already has significant pooling infrastructure:

| Pool                                                   | Status                                       |
| ------------------------------------------------------ | -------------------------------------------- |
| `DynValueArrayPool`                                    | ✅ Used in `StackTopToArray`, error handling |
| `ObjectArrayPool`                                      | ✅ Used in interop reflection calls          |
| `CallStackItemPool`                                    | ✅ Pools call stack frames                   |
| `ListPool<T>`, `HashSetPool<T>`, `DictionaryPool<K,V>` | ✅ Available                                 |
| ZString integration                                    | ✅ Used in Script, Serialization, Lexer      |
| DynValue caching                                       | ✅ 0-255, -256 to -1, common floats          |

### Top 34 Allocation Sites Identified

**Critical (VM Hot Path)** — 6 sites:

1. `List<List<SymbolRef>>` created per block entry in `ExecEnter`
1. `HashSet<int>` for to-be-closed tracking
1. `DynValue[]` arrays for function locals
1. Lambda closures for bitwise operations

**High (Data Types & CoreLib)** — 12 sites:

- Coroutine resume args, yield/tail-call arrays
- `table.pack`, `string.byte` result arrays
- Error handling (`pcall`/`xpcall`) argument arrays
- String metatable lambda closures

**High (LuaPort Pattern Matching)** — 3 sites:

- `MatchState` with `Capture[]` per pattern match
- Format buffers in `str_format`

**Medium/Low** — 13 sites in interop, serialization, and parse-time paths

### Baseline Allocation Rates

| Scenario          | Allocated | Gen0   | Notes                        |
| ----------------- | --------- | ------ | ---------------------------- |
| NumericLoops      | 824 B     | 0.0436 | Good - well optimized        |
| CoroutinePipeline | 1.16 KB   | 0.0615 | Moderate - context switching |
| TableMutation     | 25.3 KB   | 1.3733 | Expected - resize operations |
| UserDataInterop   | 1.42 KB   | 0.0749 | Moderate - reflection        |

## Priority Remediation List

### Phase 2 (Immediate — High-Impact, Low-Risk)

| Priority | Target                                              | Change                                                                  |
| -------- | --------------------------------------------------- | ----------------------------------------------------------------------- |
| P1.1     | ProcessorInstructionLoop.cs                         | Pre-allocate `BlocksToClose` and `ToBeClosedIndices` in `CallStackItem` |
| P1.2     | ProcessorInstructionLoop.cs                         | Replace lambda delegates with static delegates for bitwise ops          |
| P1.3     | ProcessorInstructionLoop.cs, ErrorHandlingModule.cs | Use `DynValueArrayPool` for varargs and pcall/xpcall                    |
| P1.4     | Coroutine.cs                                        | Use `DynValueArrayPool` for Resume args                                 |
| P1.5     | StringModule.cs                                     | Use static delegates for string metatable operations                    |

### Phase 3 (Medium — Moderate-Impact)

| Priority | Target                                         | Change                                  |
| -------- | ---------------------------------------------- | --------------------------------------- |
| P2.1     | KopiLuaStringLib.cs                            | Pool `MatchState` and char buffers      |
| P2.2     | ProcessorInstructionLoop.cs                    | Pool `LocalScope` arrays for ≤16 locals |
| P2.3     | TableModule.cs, StringModule.cs, Utf8Module.cs | Pool result arrays                      |
| P2.4     | LuaState.cs                                    | Use `DynValueArrayPool` for PopResults  |
| P2.5     | Interop descriptors                            | Verify `ObjectArrayPool` usage          |

### Phase 4 (Low — Optimization Pass)

| Priority | Target                                            | Change                                      |
| -------- | ------------------------------------------------- | ------------------------------------------- |
| P3.1     | Script.cs                                         | Add `Call(ReadOnlySpan<DynValue>)` overload |
| P3.2     | SerializationExtensions.cs, JsonTableConverter.cs | Convert string concat to ZString            |
| P3.3     | ClosureContext.cs                                 | Consider symbol name interning              |
| P3.4     | YieldRequest.cs, TailCallData.cs                  | Pooled array conversion                     |

## Missing Benchmark Coverage

| Scenario                                               | Impact | Recommendation |
| ------------------------------------------------------ | ------ | -------------- |
| String pattern matching (`string.find`, `string.gsub`) | HIGH   | Add benchmark  |
| Deep closure capture                                   | MEDIUM | Add benchmark  |

## Next Steps

1. **Phase 2**: Implement P1.1-P1.5 changes (pre-allocation, static delegates, pool usage)
1. **Benchmark**: Create pattern matching benchmark to establish baseline before Phase 3
1. **Measure**: Re-run benchmarks after each phase to quantify improvement

## Related Files

- [docs/performance/allocation-analysis-initiative12-phase1.md](../docs/performance/allocation-analysis-initiative12-phase1.md) — Full analysis report
- [PLAN.md](../PLAN.md) — Initiative 12 definition

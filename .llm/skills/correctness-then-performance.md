______________________________________________________________________

triggers:

- "priority"
- "correctness"
- "Lua spec"
- "compliance"
- "behavior"
- "optimization decision"
  category: core
  related:
- lua-spec-verification
- high-performance-csharp
  priority: core

______________________________________________________________________

# Skill: Correctness-First Performance

**When to use**: ALL development work — this establishes the priority order for the entire project.

**Related Skills**: [lua-spec-verification](lua-spec-verification.md) (correctness verification), [high-performance-csharp](high-performance-csharp.md) (performance patterns)

______________________________________________________________________

## 🔴 The Priority Hierarchy

NovaSharp follows a strict priority order. **NEVER sacrifice a higher priority for a lower one:**

```
┌─────────────────────────────────────────────────────────────┐
│  1. CORRECTNESS (Lua Spec Compliance)        ← HIGHEST     │
│     Behavior MUST match reference Lua                       │
├─────────────────────────────────────────────────────────────┤
│  2. RUNTIME PERFORMANCE (Speed)                             │
│     Execute Lua code as fast as possible                    │
├─────────────────────────────────────────────────────────────┤
│  3. MEMORY EFFICIENCY (Minimal Allocations)                 │
│     Zero-allocation hot paths, aggressive pooling           │
├─────────────────────────────────────────────────────────────┤
│  4. UNITY COMPATIBILITY                                     │
│     IL2CPP/AOT, Mono, no runtime code generation            │
├─────────────────────────────────────────────────────────────┤
│  5. CODE CLARITY                             ← LOWEST       │
│     Clean architecture, maintainability                     │
└─────────────────────────────────────────────────────────────┘
```

### What This Means in Practice

| Scenario                                                 | Correct Decision                               |
| -------------------------------------------------------- | ---------------------------------------------- |
| Performance optimization breaks Lua spec                 | ❌ Reject the optimization                     |
| Clever trick reduces allocations but causes wrong result | ❌ Reject the trick                            |
| Slower algorithm produces correct behavior               | ✅ Use it (then optimize correctly)            |
| Unity-compatible pattern is 5% slower                    | ✅ Accept (Unity support is required)          |
| Readable code is 1% slower than cryptic                  | ⚖️ Prefer readable unless in verified hot path |

______________________________________________________________________

## 🔴 Correctness First: Lua Spec Compliance

**Before ANY performance work, verify correctness.**

### The Iron Rule

```
WHEN NovaSharp behavior ≠ Reference Lua behavior
THEN fix NovaSharp production code
NEVER adjust tests/expectations to match bugs
```

### Verification Process

1. Test against reference Lua: `lua5.4 -e "print(...)"`
1. Run comparison harness to verify fixtures
1. Apply optimizations only if behavior is identical
1. Re-verify after optimization

### 🔴 ABSOLUTE PROHIBITIONS

The following optimizations are **NEVER acceptable**, regardless of performance gains:

| Optimization                               | Why Prohibited                                             |
| ------------------------------------------ | ---------------------------------------------------------- |
| **Approximate math**                       | Lua spec defines exact behavior; 1 ULP difference is a BUG |
| **Cached values without invalidation**     | Stale values produce wrong results                         |
| **Fast-path that skips edge cases**        | "Works for 99% of inputs" means 1% are **WRONG**           |
| **Reordering with side effects**           | Lua spec defines evaluation order precisely                |
| **String interning that changes identity** | Can break equality and hash behavior                       |
| **Floating-point shortcuts**               | IEEE 754 edge cases (NaN, ±Inf, -0) must match Lua exactly |
| **"Close enough" numeric output**          | `print(0.1+0.2)` must be **byte-identical** to Lua         |

**If an optimization breaks even ONE edge case's Lua-spec compliance, it is REJECTED.**

______________________________________________________________________

## 🔴 Performance Second: Maximum Speed

**Goal: NovaSharp should be the fastest Lua interpreter for .NET/Unity.**

After correctness is verified, optimize aggressively:

### Hot Path Identification

The interpreter hot paths (in priority order):

1. **VM execution loop** (`Processor.cs`, `ExecutionLoop`)
1. **Bytecode dispatch** (opcode handling)
1. **DynValue operations** (type checks, arithmetic)
1. **Table operations** (get/set/iterate)
1. **Function calls** (frame setup, argument passing)
1. **String operations** (pattern matching, concatenation)

### Performance Techniques by Impact

| Technique                   | Impact    | Where to Apply                |
| --------------------------- | --------- | ----------------------------- |
| **Avoid virtual calls**     | Very High | VM loop, opcode dispatch      |
| **Inline small methods**    | Very High | Type checks, arithmetic       |
| **Branch prediction hints** | High      | Common-case-first in switches |
| **Struct over class**       | High      | Short-lived temporaries       |
| **Array over List**         | High      | Fixed-size collections        |
| **Span over array copy**    | High      | String/buffer slicing         |
| **Loop unrolling**          | Medium    | Small fixed iterations        |
| **Lookup tables**           | Medium    | Character classification      |

**VM Loop Principle**: Tight switch on opcodes, inline small handlers. Avoid virtual calls and indirection in the execution loop.

______________________________________________________________________

## 🔴 Memory Efficiency Third: Minimal Allocations

**Goal: Zero allocations in hot paths, aggressive pooling everywhere else.**

### Allocation Budget

| Code Location      | Allocation Budget                          |
| ------------------ | ------------------------------------------ |
| VM execution loop  | **ZERO** — no allocations ever             |
| Opcode handlers    | **ZERO** — use stack/pooled only           |
| Function calls     | **Minimal** — pool frames, reuse arrays    |
| String operations  | **Pooled** — use ZStringBuilder, ArrayPool |
| Script compilation | **Acceptable** — one-time cost             |
| Script setup       | **Acceptable** — amortized over execution  |

### Pooling Quick Reference

| Use                    | When                                                       |
| ---------------------- | ---------------------------------------------------------- |
| `stackalloc`           | Compile-time constant size, small (\<=1KB), scope lifetime |
| `[ThreadStatic]` cache | Constant size, expensive to create, one per thread         |
| `ListPool/ArrayPool`   | Variable size, cross-method use                            |
| `DynValueArrayPool`    | Exact fixed size for VM frames                             |

See [high-performance-csharp](high-performance-csharp.md) for detailed patterns.

______________________________________________________________________

## 🔴 Unity Compatibility Fourth: IL2CPP/AOT Support

**Forbidden APIs**: `CollectionsMarshal`, `Reflection.Emit`, `Expression.Compile()`, `half`, `nint/nuint`, generic math interfaces. See [unity-gc-patterns](unity-gc-patterns.md) for full list.

**IL2CPP considerations**: No JIT (use concrete types in hot paths), limited reflection (use `[Preserve]`), no dynamic code generation.

______________________________________________________________________

## 🔴 Code Clarity Fifth: Maintainability

**In hot paths**: Accept less readable code if it's faster (unrolled loops, inlined code). Add comments for non-obvious optimizations.

**In cold paths** (setup, error handling, compilation): Prefer readable code. Don't micro-optimize one-time costs.

______________________________________________________________________

## Decision Framework

When making implementation choices:

```
1. Does it match Lua spec exactly?
   NO  → Fix it first, then proceed
   YES → Continue

2. Can it be faster without changing behavior?
   YES → Optimize it
   NO  → Keep current implementation

3. Can it allocate less without slowing down?
   YES → Reduce allocations
   NO  → Prefer speed over allocation reduction

4. Does it work on Unity IL2CPP?
   NO  → Find Unity-compatible alternative
   YES → Continue

5. Is it reasonably readable?
   NO  → Add comments explaining WHY (not WHAT)
   YES → Ship it
```

______________________________________________________________________

## Checklist for Every Change

Before submitting code:

- [ ] **Correctness verified** against reference Lua 5.1-5.5
- [ ] **Tests pass** including cross-version fixtures
- [ ] **No performance regression** in hot paths (benchmark if in doubt)
- [ ] **Minimal allocations** in hot paths (profile if in doubt)
- [ ] **Unity compatible** — no forbidden APIs
- [ ] **Documented** where non-obvious optimizations are used

______________________________________________________________________

## Resources

- [lua-spec-verification](lua-spec-verification.md) — Verifying correctness
- [high-performance-csharp](high-performance-csharp.md) — Performance patterns
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Allocation elimination
- [docs/lua-spec/](../../docs/lua-spec/) — Local Lua reference manuals

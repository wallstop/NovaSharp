# Correctness-First Performance Reference

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

See [high-performance-csharp](../../high-performance-csharp/SKILL.md) for detailed patterns.

______________________________________________________________________

## 🔴 Unity Compatibility Fourth: IL2CPP/AOT Support

**Forbidden APIs**: `CollectionsMarshal`, `Reflection.Emit`, `Expression.Compile()`, `half`, `nint/nuint`, generic math interfaces. See [unity-gc-patterns](../../unity-gc-patterns/SKILL.md) for full list.

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

- [lua-spec-verification](../../lua-spec-verification/SKILL.md) — Verifying correctness
- [high-performance-csharp](../../high-performance-csharp/SKILL.md) — Performance patterns
- [refactor-to-zero-alloc](../../refactor-to-zero-alloc/SKILL.md) — Allocation elimination
- [docs/lua-spec/](../../../../docs/lua-spec/) — Local Lua reference manuals

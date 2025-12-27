# Skill: Correctness-First Performance

**When to use**: ALL development work — this establishes the priority order for the entire project.

**Related Skills**: [lua-spec-verification](lua-spec-verification.md) (correctness verification), [high-performance-csharp](high-performance-csharp.md) (performance patterns), [performance-audit](performance-audit.md) (optimization checklist)

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

Before optimizing ANY code path:

1. **Verify current behavior matches Lua spec**

   ```bash
   # Test against reference Lua
   lua5.4 -e "print(your_code_here)"
   lua5.1 -e "print(your_code_here)"
   ```

1. **Create/update fixtures that verify correctness**

   ```bash
   # Run comparison harness
   python3 tools/LuaComparisonHarness/lua_comparison_harness.py
   ```

1. **Only then apply optimizations**

   - Maintain identical observable behavior
   - Re-run verification after optimization

### Common Correctness Traps in Performance Code

| Optimization Temptation    | Correctness Risk                     | Correct Approach                                    |
| -------------------------- | ------------------------------------ | --------------------------------------------------- |
| Cache computed values      | Stale cache returns wrong result     | Verify cache invalidation covers all mutation paths |
| Fast-path for common cases | Rare cases handled incorrectly       | Test ALL edge cases, not just common ones           |
| Approximate math for speed | Results differ from Lua spec         | Use exact algorithms that match Lua                 |
| Skip validation for speed  | Invalid state causes wrong behavior  | Validate in debug builds, trust in release          |
| Reorder operations         | Observable side-effect order changes | Preserve Lua-specified evaluation order             |

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

### VM Loop Optimization Principles

```csharp
// ✅ GOOD: Tight, predictable loop
while (true)
{
    Instruction i = code[pc++];
    switch (i.OpCode)
    {
        case OpCode.Add: /* inline */ break;
        case OpCode.Sub: /* inline */ break;
        // ...
    }
}

// ❌ BAD: Indirection, virtual calls
while (true)
{
    Instruction i = GetNextInstruction();  // Virtual call
    handlers[i.OpCode].Execute(context);   // Another virtual call
}
```

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

### The Pooling Hierarchy

```
Use stackalloc when:
  ✓ Size is compile-time constant
  ✓ Size is small (≤1KB)
  ✓ Lifetime is current scope only

Use [ThreadStatic] cache when:
  ✓ Size is constant across calls
  ✓ Object is expensive to create
  ✓ Single instance per thread suffices

Use ArrayPool/ListPool when:
  ✓ Size varies at runtime
  ✓ Need to return to caller
  ✓ Concurrent access possible

Use DynValueArrayPool/ObjectArrayPool when:
  ✓ Need exact size (reflection, VM frames)
  ✓ Fixed known sizes
```

### Memory Patterns to Follow

```csharp
// ✅ GOOD: Stack-allocated small buffer
Span<char> buffer = stackalloc char[64];
int len = FormatNumber(value, buffer);

// ✅ GOOD: Thread-local reusable buffer
[ThreadStatic] private static char[] t_buffer;
private static char[] GetBuffer() => t_buffer ??= new char[256];

// ✅ GOOD: Pooled variable-size array
using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(length, out char[] buffer);
// Use buffer...
// Automatically returned on dispose

// ❌ BAD: Allocation in hot path
char[] buffer = new char[64];  // ALLOCATES!
```

______________________________________________________________________

## 🔴 Unity Compatibility Fourth: IL2CPP/AOT Support

**Goal: All code must work on Unity IL2CPP (AOT, no JIT, limited reflection).**

### APIs NOT Available in Unity

| API                                            | Reason                 | Alternative                |
| ---------------------------------------------- | ---------------------- | -------------------------- |
| `CollectionsMarshal.AsSpan<T>()`               | .NET 5+ only           | Manual array access        |
| `CollectionsMarshal.GetValueRefOrAddDefault()` | .NET 6+ only           | Standard dictionary ops    |
| `List<T>.EnsureCapacity()`                     | .NET Core 2.1+ only    | Constructor with capacity  |
| `Span<T>` fields in non-ref structs            | Runtime support needed | Use `ref struct` or arrays |
| `[SkipLocalsInit]`                             | Runtime support needed | Not available              |
| `half` (Half-precision float)                  | .NET 5+ only           | Use `float`                |
| `nint` / `nuint`                               | .NET 5+ only           | Use `IntPtr` / `UIntPtr`   |
| Generic math interfaces (`INumber<T>`)         | .NET 7+ only           | Explicit type overloads    |
| `System.Reflection.Emit`                       | AOT incompatible       | Pre-generated code         |
| `Expression.Compile()`                         | AOT incompatible       | Interpreted expressions    |

### Unity-Safe Patterns

```csharp
// ✅ GOOD: Works on all platforms
public static T[] ToArray<T>(this List<T> list)
{
    T[] result = new T[list.Count];
    for (int i = 0; i < list.Count; i++)
        result[i] = list[i];
    return result;
}

// ❌ BAD: .NET 5+ only, not in Unity
ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(list);
```

### IL2CPP-Specific Considerations

| Concern                     | Impact                        | Mitigation                         |
| --------------------------- | ----------------------------- | ---------------------------------- |
| No JIT                      | Generics may be slower        | Use concrete types in hot paths    |
| Limited reflection          | Runtime type discovery fails  | Use `[Preserve]` attributes        |
| AOT compilation             | Dynamic code generation fails | Avoid `Emit`, `Expression.Compile` |
| Value type devirtualization | May not happen                | Manually inline critical paths     |

______________________________________________________________________

## 🔴 Code Clarity Fifth: Maintainability

**Goal: Code should be understandable, but not at the cost of correctness or performance.**

### When Clarity Yields to Performance

In **verified hot paths** (VM loop, opcode handlers):

```csharp
// ✅ ACCEPTABLE in hot path: Less readable but faster
// Unrolled comparison instead of loop
if (a0 != b0) return false;
if (a1 != b1) return false;
if (a2 != b2) return false;
if (a3 != b3) return false;
return true;

// ❌ WRONG in hot path: More readable but slower
for (int i = 0; i < 4; i++)
    if (a[i] != b[i]) return false;
return true;
```

### When Clarity Wins

In **non-hot paths** (script setup, error handling, compilation):

```csharp
// ✅ GOOD: Readable, maintainable
List<Error> errors = ValidateScript(source);
if (errors.Count > 0)
{
    foreach (Error error in errors)
        ReportError(error);
    return null;
}

// ❌ UNNECESSARY: Micro-optimization in cold path
using PooledResource<List<Error>> pooled = ListPool<Error>.Get(out List<Error> errors);
// ... (pooling overhead not worth it here)
```

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
- [performance-audit](performance-audit.md) — Quick optimization checklist
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Allocation elimination
- [docs/lua-spec/](../../docs/lua-spec/) — Local Lua reference manuals

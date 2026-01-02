______________________________________________________________________

triggers:

- "inlining"
- "AggressiveInlining"
- "MethodImpl"
- "method inlining"
- "hot path optimization"
  category: performance
  related:
- high-performance-csharp
- unity-gc-patterns
  priority: reference

______________________________________________________________________

# Skill: Aggressive Inlining

**When to use**: Optimizing hot path methods for maximum performance, especially in interpreter loops and frequently-called code.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (general performance), [unity-gc-patterns](unity-gc-patterns.md) (Unity-specific)

______________________________________________________________________

## 🔴 What is AggressiveInlining?

The `[MethodImpl(MethodImplOptions.AggressiveInlining)]` attribute hints to the JIT compiler that a method should be inlined at call sites, eliminating call overhead.

```csharp
using System.Runtime.CompilerServices;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int Add(int a, int b)
{
    return a + b;
}
```

### What Inlining Does

```csharp
// BEFORE inlining (conceptual)
int result = Add(x, y);  // Call instruction, stack setup

// AFTER inlining (conceptual)
int result = x + y;      // Code inserted directly, no call overhead
```

### Performance Benefits

| Benefit                           | Impact                                 |
| --------------------------------- | -------------------------------------- |
| **Eliminates call overhead**      | ~1-5 nanoseconds per call              |
| **Enables further optimizations** | JIT can optimize across inlined code   |
| **Improves branch prediction**    | Inlined paths can be better predicted  |
| **Reduces stack pressure**        | No stack frame setup for inlined calls |

______________________________________________________________________

## 🔴 When to Use AggressiveInlining

### ✅ Good Candidates

| Scenario                         | Example                           | Why                      |
| -------------------------------- | --------------------------------- | ------------------------ |
| **Tiny methods** (\<20 IL bytes) | Property getters, arithmetic      | Call overhead dominates  |
| **Hot path methods**             | VM instruction dispatch           | Called millions of times |
| **Type checks**                  | `if (value.Type == X)`            | Simple branches benefit  |
| **Simple property access**       | `public int Count => _count;`     | Eliminate indirection    |
| **Forwarding methods**           | `void Do() => _inner.Do();`       | Pure overhead otherwise  |
| **Math operations**              | `Max(a, b)`, `Clamp(x, min, max)` | Trivial computation      |

```csharp
// ✅ GOOD: Hot path type check in VM
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool IsNumber(DynValue value)
{
    return value.Type == DataType.Number;
}

// ✅ GOOD: Frequently called accessor
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static ref readonly Instruction GetInstruction(Instruction[] code, int pc)
{
    return ref code[pc];
}

// ✅ GOOD: Simple math operation
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static double Clamp(double value, double min, double max)
{
    return value < min ? min : value > max ? max : value;
}
```

### ❌ Bad Candidates

| Scenario                     | Why                            |
| ---------------------------- | ------------------------------ |
| Large methods (>32 IL bytes) | Code bloat, cache misses       |
| Methods with try/catch       | Inhibits inlining              |
| Virtual methods              | Cannot inline virtual dispatch |
| Recursive methods            | Cannot inline recursion        |

______________________________________________________________________

## 🔴 IL2CPP/AOT Considerations

IL2CPP makes inlining decisions at compile time, not runtime. It's more conservative than JIT. Use AggressiveInlining for methods called from many sites. Be cautious with generic methods (each value type = separate code).

______________________________________________________________________

## 🔴 Measuring Inlining Impact

Use BenchmarkDotNet with `[DisassemblyDiagnoser]`. Check if method disappears from profile or disassembly shows code inline instead of call instruction.

______________________________________________________________________

## 🔴 Key Patterns

**Inline fast path, outline slow path**: Keep fast path tiny and inlinable. Move complex/error handling to `[NoInlining]` methods.

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void ValidateIndex(int index)
{
    if ((uint)index >= (uint)_count)
        ThrowIndexOutOfRange(index);  // Cold path, not inlined
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static void ThrowIndexOutOfRange(int index) => throw new IndexOutOfRangeException();
```

______________________________________________________________________

## 🔴 Related Attributes

| Attribute                | Use                                            |
| ------------------------ | ---------------------------------------------- |
| `AggressiveInlining`     | Hint to inline method                          |
| `NoInlining`             | Force separate method (error paths, profiling) |
| `AggressiveOptimization` | .NET 5+ only, not in Unity                     |

______________________________________________________________________

## 🔴 Common Mistakes

- **Over-inlining large methods** — Causes code bloat, makes things SLOWER
- **Inlining virtual methods** — Has no effect
- **Inlining methods with try/catch** — Won't actually inline
- **Not measuring** — JIT often makes good decisions; attribute can make things worse

______________________________________________________________________

## Checklist for Inlining

Before adding `AggressiveInlining`:

- [ ] Method is **small** (\<32 IL bytes ideally)
- [ ] Method is on a **hot path** (called frequently)
- [ ] Method has **no exception handling**
- [ ] Method is **not virtual**
- [ ] Method has **few locals** (doesn't need many registers)
- [ ] You have **measured** the impact (before/after benchmark)
- [ ] IL2CPP compatibility verified if targeting Unity

When in doubt, **don't add the attribute**. The JIT usually makes good decisions.

______________________________________________________________________

## Quick Reference

| Method Size | Hot Path? | Add AggressiveInlining?    |
| ----------- | --------- | -------------------------- |
| \<16 bytes  | Yes       | ✅ Definitely              |
| 16-32 bytes | Yes       | ✅ Probably                |
| 32-64 bytes | Yes       | ⚠️ Measure first           |
| >64 bytes   | Yes       | ❌ No (outline cold parts) |
| Any size    | No        | ❌ No                      |

______________________________________________________________________

## Resources

- [high-performance-csharp](high-performance-csharp.md) — General performance patterns
- [unity-gc-patterns](unity-gc-patterns.md) — Unity-specific patterns

---
name: aggressive-inlining
description: "Evaluate and apply C# MethodImplOptions.AggressiveInlining on measured NovaSharp hot paths. Use for method inlining, interpreter-loop call overhead, or hot-path optimization."
metadata:
  category: performance
  priority: reference
  related: high-performance-csharp, unity-gc-patterns
---
# Skill: Aggressive Inlining

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md) (general performance), [unity-gc-patterns](../unity-gc-patterns/SKILL.md) (Unity-specific)

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
| **Type checks**                  | `if (value.IsNumber)`             | Simple branches benefit  |
| **Simple property access**       | `public int Count => _count;`     | Eliminate indirection    |
| **Forwarding methods**           | `void Do() => _inner.Do();`       | Pure overhead otherwise  |
| **Math operations**              | `Max(a, b)`, `Clamp(x, min, max)` | Trivial computation      |

```csharp
// ✅ GOOD: Hot path type check in VM
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool IsNumber(LuaValue value)
{
    return value.IsNumber;
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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Key Patterns, Related Attributes, Common Mistakes, Checklist for Inlining, and later sections.

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

| Scenario                            | Why                                    |
| ----------------------------------- | -------------------------------------- |
| **Large methods** (>32 IL bytes)    | Code bloat, instruction cache misses   |
| **Methods with exception handling** | Try/catch blocks inhibit inlining      |
| **Virtual methods**                 | Cannot inline through virtual dispatch |
| **Methods with many locals**        | Register pressure, stack spills        |
| **Recursive methods**               | Cannot inline recursion                |
| **Cold path error handling**        | Inlining error paths bloats hot code   |

```csharp
// ❌ BAD: Too large, has exception handling
[MethodImpl(MethodImplOptions.AggressiveInlining)]  // Don't do this!
public DynValue ParseComplexExpression(string input)
{
    try
    {
        // 50+ lines of parsing logic
    }
    catch (Exception ex)
    {
        // Error handling
    }
}

// ❌ BAD: Virtual method
[MethodImpl(MethodImplOptions.AggressiveInlining)]  // No effect!
public virtual void Process() { }
```

______________________________________________________________________

## 🔴 IL2CPP/AOT Considerations

In Unity IL2CPP (AOT compilation), inlining behavior differs:

| Aspect                 | JIT (.NET)     | AOT (IL2CPP)              |
| ---------------------- | -------------- | ------------------------- |
| **Decision time**      | Runtime        | Compile time              |
| **Optimization level** | Profile-guided | Static analysis           |
| **AggressiveInlining** | Strong hint    | May or may not be honored |
| **Method size limit**  | Dynamic        | Often more conservative   |

### IL2CPP-Specific Recommendations

```csharp
// ✅ IL2CPP typically inlines these automatically:
// - Trivial property accessors
// - Very small methods (<16 IL bytes)
// - Methods called from one call site

// ✅ Use AggressiveInlining for methods called from MANY sites
// IL2CPP may not inline without the hint if called from multiple places
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static DataType GetType(DynValue value) => value.Type;

// ⚠️ Be cautious with generics in IL2CPP
// Each value type instantiation generates separate code
// Aggressive inlining of generic methods can cause code bloat
```

______________________________________________________________________

## 🔴 Measuring Inlining Impact

### BenchmarkDotNet with Disassembly

```csharp
[Benchmark]
[DisassemblyDiagnoser(printSource: true)]
public int WithInlining()
{
    int sum = 0;
    for (int i = 0; i < 1000; i++)
    {
        sum += InlinedAdd(i, i);
    }
    return sum;
}
```

### Checking if Method Was Inlined

In .NET:

```bash
# Set environment variable to see JIT decisions
DOTNET_JitDisasm=MethodName
```

### Signs That Inlining Helped

| Before                     | After                               |
| -------------------------- | ----------------------------------- |
| Method appears in profile  | Method call disappears from profile |
| Call instruction in disasm | Code appears inline                 |
| ~5ns overhead per call     | Near-zero overhead                  |

______________________________________________________________________

## 🔴 Inlining and Code Organization

### Pattern: Inline the Fast Path, Outline the Slow Path

```csharp
// ✅ GOOD: Fast path is tiny and inlinable
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public DynValue Get(string key)
{
    // Fast path: direct lookup
    if (_fastCache.TryGetValue(key, out DynValue value))
    {
        return value;
    }
    
    // Slow path: full lookup (not inlined)
    return GetSlow(key);
}

[MethodImpl(MethodImplOptions.NoInlining)]  // Force separate method
private DynValue GetSlow(string key)
{
    // Complex fallback logic
    // Exception handling
    // etc.
}
```

### Pattern: NoInlining for Error Paths

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void ValidateIndex(int index)
{
    if ((uint)index >= (uint)_count)
    {
        ThrowIndexOutOfRange(index);  // Cold path, not inlined
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static void ThrowIndexOutOfRange(int index)
{
    throw new IndexOutOfRangeException($"Index {index} is out of range");
}
```

This pattern keeps the hot path small (inlinable) while the error path doesn't bloat the code.

______________________________________________________________________

## 🔴 Related Attributes

### AggressiveOptimization (NET 5+)

```csharp
// Tells JIT to spend more time optimizing this method
[MethodImpl(MethodImplOptions.AggressiveOptimization)]
public void CriticalHotPath() { }
```

**Note**: Not available in Unity's .NET profile. Use AggressiveInlining instead.

### NoInlining

```csharp
// Force method to NOT be inlined (for profiling, stack traces)
[MethodImpl(MethodImplOptions.NoInlining)]
public void AlwaysShowInStackTrace() { }
```

Useful for:

- Error handling methods (keep cold code out of hot path)
- Methods you want to appear in profiler
- Preventing code bloat from large methods

### NoOptimization

```csharp
// Disable all optimizations (debugging)
[MethodImpl(MethodImplOptions.NoOptimization)]
public void DebugThisMethod() { }
```

Rarely useful in production code.

______________________________________________________________________

## 🔴 Common Mistakes

### Mistake 1: Over-Inlining Large Methods

```csharp
// ❌ BAD: 100+ line method with AggressiveInlining
// Results in: Code bloat, instruction cache misses, SLOWER
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void ProcessEverything() { /* 100 lines */ }
```

### Mistake 2: Inlining Virtual Methods

```csharp
// ❌ BAD: Has no effect on virtual methods
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public virtual void CannotInline() { }
```

### Mistake 3: Ignoring Exception Handling Impact

```csharp
// ❌ BAD: Try/catch prevents inlining
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public int WontActuallyInline(int x)
{
    try { return x * 2; }
    catch { return 0; }
}
```

### Mistake 4: Not Measuring

```csharp
// ❌ BAD: Adding AggressiveInlining without benchmarking
// The JIT often makes good decisions on its own
// Adding the attribute can sometimes make things WORSE
```

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
- [performance-audit](performance-audit.md) — Quick audit checklist

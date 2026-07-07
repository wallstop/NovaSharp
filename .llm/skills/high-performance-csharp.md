______________________________________________________________________

triggers:

- "performance"
- "allocation"
- "zero-alloc"
- "GC pressure"
- "hot path"
- "pooling"
- "optimization"
  category: performance
  related:
- correctness-then-performance
- allocation-traps
- zstring-migration
- span-optimization
  priority: core

______________________________________________________________________

# High-Performance C# Coding Guidelines

**PRIORITY REMINDER**: Performance is the SECOND priority after correctness. A fast implementation that breaks Lua spec compliance is REJECTED. See [correctness-then-performance](correctness-then-performance.md).

When writing new code for NovaSharp, prioritize **minimal allocations** and **maximum efficiency**. This interpreter runs hot paths millions of times.

**Code Samples**: [pooling-patterns](../code-samples/pooling-patterns.md), [unity-gc-patterns](../code-samples/unity-gc-patterns.md), [string-building](../code-samples/string-building.md)

**Related Skills**: [allocation-traps](allocation-traps.md), [zstring-migration](zstring-migration.md), [span-optimization](span-optimization.md)

______________________________________________________________________

## Quick Audit Checklist

| Pattern                           | Problem                        | Fix                            |
| --------------------------------- | ------------------------------ | ------------------------------ |
| `.Where()`, `.Select()`, `.Any()` | Iterator + delegate allocation | `for` loop                     |
| `new List<T>()` in method         | Heap allocation                | `ListPool<T>.Get()`            |
| `=> localVar` in lambda           | Closure allocation             | Static lambda or explicit loop |
| `$"text {var}"` in hot path       | String allocation              | `ZString.Concat()`             |
| `.ToString()` on enum             | String allocation              | Cached string lookup           |
| `new T[]` with variable size      | Array allocation               | `SystemArrayPool<T>.Get()`     |
| Boxing struct to object           | Box allocation                 | Generic methods                |

VM opcode and ordinary Lua-call paths are stricter than general runtime code: they must be allocation-free after warmup. Use inline `LuaValue`, stack windows, spans, and explicit slow-path allowlists; do not add `new DynValue`, `DynValue.NewNumber`, `DynValue.NewInteger`, `new DynValue[]`, `new List<DynValue>`, or `new ScriptExecutionContext` to hot processor/call paths without updating the VM allocation guard and documenting why it is not hot.

______________________________________________________________________

## Core Principles

1. **Prefer value types** when data is small and short-lived
1. **Avoid allocations in hot paths** - use pooling, stackalloc, spans
1. **Use `readonly struct`** for immutable value types
1. **NEVER capture in closures** in hot paths - use static lambdas
1. **NEVER use LINQ** in hot paths - use explicit loops
1. **Always measure** with BenchmarkDotNet before/after

______________________________________________________________________

## Pool Selection Flowchart

```text
What kind of buffer do you need?
|
+-- DynValue array? --> DynValueArrayPool.Get(exactSize, out array)
+-- Object array?   --> ObjectArrayPool.Get(exactSize, out array)
+-- Variable size?  --> SystemArrayPool<T>.Get(size, out array)
+-- List/Set/Dict?  --> ListPool<T>.Get(), HashSetPool<T>.Get(), etc.
+-- StringBuilder?  --> ZStringBuilder.Create()
```

See [pooling-patterns](../code-samples/pooling-patterns.md) for detailed examples.

______________________________________________________________________

## Pool Usage Pattern (CRITICAL)

**ALWAYS use `using` with `Get()` instead of manual `Rent()`/`Return()` calls.**

```csharp
// BAD: Manual rent/return leaks on exception!
List<Instruction> jumps = ListPool<Instruction>.Rent();
DoSomethingThatMightThrow();  // If this throws, jumps is never returned!
ListPool<Instruction>.Return(jumps);

// GOOD: RAII pattern - automatic cleanup even on exception
using (ListPool<Instruction>.Get(out List<Instruction> jumps))
{
    DoSomethingThatMightThrow();
}
```

______________________________________________________________________

## Architecture Principles

| Prefer              | Over                 | Reason                              |
| ------------------- | -------------------- | ----------------------------------- |
| `readonly struct`   | `class`              | Stack-allocated, no GC pressure     |
| `static` methods    | Instance methods     | No `this` capture, enables inlining |
| Extension methods   | Utility classes      | Discoverable, fluent APIs           |
| Generic constraints | Interface parameters | Avoids boxing for value types       |

### Size Guidelines for Structs

| Size        | Recommendation                                  |
| ----------- | ----------------------------------------------- |
| \<=16 bytes | `readonly struct` preferred                     |
| 17-64 bytes | `readonly struct` OK if passed by `in` or `ref` |
| >64 bytes   | Consider `class` or pass by `ref`               |

______________________________________________________________________

## String Building

Use ZString for zero-allocation string building. See [zstring-migration](zstring-migration.md).

```csharp
// Safe for nested/recursive calls
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
return sb.ToString();
```

**NEVER** use `StringBuilder`, `$"..."` interpolation, or `+` in hot paths.

______________________________________________________________________

## Closures and Lambdas

```csharp
// BAD: Captures 'threshold' - allocates closure
int threshold = 10;
List<int> filtered = items.Where(x => x > threshold).ToList();

// GOOD: Static lambda - no capture
items.Where(static x => x > 0);

// GOOD: Explicit loop
for (int i = 0; i < items.Count; i++)
{
    if (items[i] > threshold)
        filtered.Add(items[i]);
}
```

______________________________________________________________________

## Enum String Caching

**NEVER call `.ToString()` on enums in hot paths.** Use cached lookups:

```csharp
// BAD: Allocates every call
sb.Append(tokenType.ToString());

// GOOD: Zero allocation
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));
```

Available caches: `TokenTypeStrings`, `OpCodeStrings`, `SymbolRefTypeStrings`, `ModLoadStateStrings`, `DebuggerActionTypeStrings`

______________________________________________________________________

## Hash Code Implementation

**ALWAYS use `HashCodeHelper`** for `GetHashCode()`. Never use bespoke patterns or `HashCode.Combine()`.

```csharp
// GOOD: Simple multi-field hash
public override int GetHashCode()
{
    return HashCodeHelper.HashCode(_field1, _field2, _field3);
}

// GOOD: Use DeterministicHashBuilder for complex hashing
public override int GetHashCode()
{
    DeterministicHashBuilder hash = default;
    hash.AddInt((int)Type);
    if (HasValue) hash.Add(Value);
    return hash.ToHashCode();
}
```

______________________________________________________________________

## Sorting Without Boxing

Use `IListSortExtensions` with struct comparers to avoid boxing:

```csharp
// BAD: Boxes struct comparer
list.Sort(new DynValueComparer(script));

// GOOD: Zero-allocation with struct comparer
readonly struct DynValueComparer : IComparer<DynValue> { /* ... */ }
list.Sort(new DynValueComparer(script));  // Extension method, no boxing
```

______________________________________________________________________

## Unity Compatibility

NovaSharp targets Unity3D (IL2CPP/AOT) in addition to .NET. See [unity-gc-patterns](../code-samples/unity-gc-patterns.md) for:

- APIs NOT available in Unity
- IL2CPP-specific considerations
- Why zero-allocation is even more critical in Unity

______________________________________________________________________

## Profiling and Verification

### Test for Zero Allocation

```csharp
[Test]
public void Method_ShouldNotAllocate()
{
    // Warm up
    target.Method();

    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1000; i++)
    {
        target.Method();
    }
    long after = GC.GetAllocatedBytesForCurrentThread();

    Assert.That(after - before, Is.EqualTo(0));
}
```

### Identifying Hot Paths

| Code Location          | Frequency       | Priority |
| ---------------------- | --------------- | -------- |
| VM execution loop      | Millions/second | Critical |
| Opcode handlers        | Millions/second | Critical |
| DynValue operations    | Very frequent   | Critical |
| Table get/set          | Very frequent   | High     |
| Function call dispatch | Per call        | High     |
| String operations      | Per string op   | Medium   |
| Script compilation     | Once per script | Low      |

______________________________________________________________________

## Checklist for New Code

- [ ] Using explicit types everywhere? (Never use `var`)
- [ ] Could this be a `readonly struct`?
- [ ] If struct, added `IEquatable<T>`, `Equals`, `GetHashCode`, `==`, `!=`?
- [ ] Using `HashCodeHelper` for `GetHashCode()`?
- [ ] Using pooled resources with `using`?
- [ ] Avoiding LINQ in hot paths?
- [ ] Using static lambdas or cached delegates?
- [ ] Using `ZString` for string building?
- [ ] Avoiding boxing value types?

______________________________________________________________________

## Related Documentation

- [allocation-traps](allocation-traps.md) - Common allocation pitfalls
- [zstring-migration](zstring-migration.md) - String building patterns
- [span-optimization](span-optimization.md) - Span-based operations
- [correctness-then-performance](correctness-then-performance.md) - Priority hierarchy

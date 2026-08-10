---
name: allocation-traps
description: "Find and remove hidden C# allocations in NovaSharp hot paths. Use when reviewing GC pressure, closures, delegates, params arrays, foreach behavior, boxing, or unexplained allocations."
metadata:
  category: performance
  priority: core
  related: high-performance-csharp, refactor-to-zero-alloc, zstring-migration
---
# Skill: Allocation Traps

**Code Samples**: [pooling-patterns](../../code-samples/pooling-patterns.md), [unity-gc-patterns](../../code-samples/unity-gc-patterns.md)

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md), [refactor-to-zero-alloc](../refactor-to-zero-alloc/SKILL.md)

______________________________________________________________________

## Quick Reference: Allocation Costs

| Trap                           | Bytes Per Occurrence | Risk Level |
| ------------------------------ | -------------------- | ---------- |
| `foreach` on `List<T>` (Mono)  | 24 bytes             | High       |
| LINQ `.Where()/.Select()`      | 32+ bytes            | High       |
| Closure capturing local        | 32+ bytes            | High       |
| Delegate in loop               | 52 bytes             | High       |
| `params` method call           | 24+ bytes            | Medium     |
| Enum dictionary lookup         | 24 bytes             | Medium     |
| Struct without `IEquatable<T>` | 24+ bytes            | Medium     |
| Boxing to `object`             | 12+ bytes            | Medium     |
| `Enum.HasFlag()`               | 24 bytes (2x boxing) | Medium     |
| `enum.ToString()`              | 20+ bytes            | Medium     |

______________________________________________________________________

## Trap 1: foreach on List (Unity/Mono)

Unity's Mono boxes `List<T>` enumerators, allocating **24 bytes per loop**:

```csharp
// BAD: Allocates 24 bytes
foreach (LuaValue item in myList) { Process(item); }

// GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++) { Process(myList[i]); }
```

**Arrays are safe** - foreach on arrays is optimized.

______________________________________________________________________

## Trap 2: LINQ Methods

All LINQ methods allocate iterator objects and often delegate objects:

```csharp
// BAD: Each method allocates
List<LuaValue> result = values
    .Where(v => v.IsNumber)                 // Iterator + delegate
    .ToList();                               // New List

// GOOD: Explicit loop with pooling
using PooledResource<List<LuaValue>> lease = ListPool<LuaValue>.Get();
for (int i = 0; i < values.Count; i++)
{
    if (values[i].IsNumber)
        lease.Resource.Add(values[i]);
}
```

______________________________________________________________________

## Trap 3: Closures Capturing Variables

Lambdas that capture variables allocate closure objects:

```csharp
// BAD: Captures 'targetKind' - allocates closure
LuaKind targetKind = LuaKind.Table;
LuaValue found = list.Find(v => v.Kind == targetKind);

// GOOD: Explicit loop
LuaValue found = LuaValue.Nil;
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Kind == targetKind) { found = list[i]; break; }
}
```

**Use static lambdas** (C# 9+) to prevent accidental captures:

```csharp
items.Sort(static (a, b) => a.AsNumber().CompareTo(b.AsNumber()));
```

______________________________________________________________________

## Trap 4: Delegate Caching

Delegate creation allocates every time:

```csharp
// BAD: Allocates delegate EVERY iteration
for (int i = 0; i < count; i++)
{
    Func<LuaValue> fn = GetValue;  // 52+ bytes!
    result.Add(fn());
}

// GOOD: Cache delegate
private static readonly Comparison<Item> PriorityComparison =
    static (a, b) => a.Priority.CompareTo(b.Priority);

items.Sort(PriorityComparison);  // Zero allocation
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Trap 5: params Methods, Trap 6: Enum Dictionary Keys, Trap 7: Structs Without IEquatable, Trap 8: String Operations, and later sections.

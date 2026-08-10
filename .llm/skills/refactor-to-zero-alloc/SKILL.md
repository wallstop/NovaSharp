---
name: refactor-to-zero-alloc
description: "Refactor existing NovaSharp code to eliminate measured GC allocations with pooling, loops, non-capturing patterns, spans, and zero-allocation string building. Use for allocation removal or LINQ and closure elimination."
metadata:
  category: performance
  priority: recommended
  related: high-performance-csharp, allocation-traps, zstring-migration
---
# Skill: Refactoring to Zero-Allocation Patterns

**Code Samples**: [pooling-patterns](../../code-samples/pooling-patterns.md), [string-building](../../code-samples/string-building.md)

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md), [allocation-traps](../allocation-traps/SKILL.md), [zstring-migration](../zstring-migration/SKILL.md)

______________________________________________________________________

## Refactoring Process Overview

| Step | Focus                    | Tools                                                        |
| ---- | ------------------------ | ------------------------------------------------------------ |
| 1    | Identify Allocations     | Regex search, profiler, code review                          |
| 2    | LINQ to Loop Conversion  | Manual loop replacement                                      |
| 3    | Closure Elimination      | Extract to explicit loops, static lambdas, pass state        |
| 4    | Collection Pooling       | `ListPool<T>`, `HashSetPool<T>`, `DictionaryPool<K,V>`       |
| 5    | StringBuilder to ZString | `ZStringBuilder.Create()`, `ZString.Concat()`                |
| 6    | Array Pooling            | `DynValueArrayPool`, `ObjectArrayPool`, `SystemArrayPool<T>` |

______________________________________________________________________

## Step 1: Identify Allocations

### Common Allocation Sources

| Source                | Example                              | Allocation Type          |
| --------------------- | ------------------------------------ | ------------------------ |
| LINQ methods          | `.Where()`, `.Select()`, `.ToList()` | Iterator + result        |
| `new List<T>()`       | `new List<LuaValue>()`               | List + backing array     |
| `new Dictionary<K,V>` | `new Dictionary<string, int>()`      | Dictionary + buckets     |
| String interpolation  | `$"Error: {msg}"`                    | Intermediate strings     |
| Lambda closures       | `list.Find(x => x.Id == targetId)`   | Closure class + delegate |
| `new T[]`             | `new LuaValue[count]`                | Array                    |
| `.ToArray()`          | `list.ToArray()`                     | New array copy           |

### Regex Search Patterns

```bash
# LINQ methods (high priority)
rg '\.Where\(|\.Select\(|\.First\(|\.Any\(|\.ToList\(\)|\.ToArray\(\)' --type cs

# New collections
rg 'new (List|Dictionary|HashSet)<' --type cs

# String operations
rg '\$"|new StringBuilder' --type cs
```

______________________________________________________________________

## Step 2: LINQ to Loop Conversion

See [pooling-patterns](../../code-samples/pooling-patterns.md#linq-to-loop-conversions) for detailed examples.

### Quick Reference

| LINQ Pattern          | Zero-Alloc Replacement           |
| --------------------- | -------------------------------- |
| `.Where().ToList()`   | `for` loop + `ListPool<T>.Get()` |
| `.Select().ToArray()` | Pre-sized array + `for` loop     |
| `.Any(predicate)`     | `for` loop with early `return`   |
| `.First(predicate)`   | `for` loop with early `break`    |
| `.Count(predicate)`   | `for` loop with counter          |
| `.Sum(selector)`      | `for` loop with accumulator      |
| `.OrderBy().ToList()` | `ListPool<T>.Get()` + `Sort()`   |

______________________________________________________________________

## Step 3: Closure Elimination

### Why Closures Allocate

When a lambda captures a variable, the compiler generates a hidden class:

```csharp
// This allocates a closure class every call
void FindItem(int targetId)
{
    Item item = list.Find(x => x.Id == targetId);  // Allocates!
}
```

### Techniques

1. **Extract to Explicit Loop** (preferred):

```csharp
Item item = null;
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Id == targetId)
    {
        item = list[i];
        break;
    }
}
```

2. **Static Lambda** (C# 9+):

```csharp
private static readonly Comparison<Item> PriorityComparison =
    static (a, b) => a.Priority.CompareTo(b.Priority);

items.Sort(PriorityComparison);
```

3. **Pass State via Parameter**:

```csharp
bool found = Find(items, threshold, static (x, thresh) => x.Value > thresh);
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Step 4: Collection Pooling, Step 5: String Building, Step 6: Array Pooling, Verification, and later sections.

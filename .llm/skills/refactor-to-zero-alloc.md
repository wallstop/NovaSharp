______________________________________________________________________

triggers:

- "refactor allocation"
- "eliminate allocation"
- "zero allocation"
- "remove LINQ"
- "closure elimination"
  category: performance
  related:
- high-performance-csharp
- allocation-traps
- zstring-migration
  priority: recommended

______________________________________________________________________

# Skill: Refactoring to Zero-Allocation Patterns

**When to use**: Eliminating GC allocations from existing code by converting to pooled collections, eliminating LINQ, removing closures, and using zero-allocation string building.

**Code Samples**: [pooling-patterns](../code-samples/pooling-patterns.md), [string-building](../code-samples/string-building.md)

**Related Skills**: [high-performance-csharp](high-performance-csharp.md), [allocation-traps](allocation-traps.md), [zstring-migration](zstring-migration.md)

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
| `new List<T>()`       | `new List<DynValue>()`               | List + backing array     |
| `new Dictionary<K,V>` | `new Dictionary<string, int>()`      | Dictionary + buckets     |
| String interpolation  | `$"Error: {msg}"`                    | Intermediate strings     |
| Lambda closures       | `list.Find(x => x.Id == targetId)`   | Closure class + delegate |
| `new T[]`             | `new DynValue[count]`                | Array                    |
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

See [pooling-patterns](../code-samples/pooling-patterns.md#linq-to-loop-conversions) for detailed examples.

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

## Step 4: Collection Pooling

All pools are in `WallstopStudios.NovaSharp.Interpreter.DataStructs`.

| Pool                  | Get Method                                     |
| --------------------- | ---------------------------------------------- |
| `ListPool<T>`         | `ListPool<T>.Get(out List<T>)`                 |
| `HashSetPool<T>`      | `HashSetPool<T>.Get(out HashSet<T>)`           |
| `DictionaryPool<K,V>` | `DictionaryPool<K,V>.Get(out Dictionary<K,V>)` |

See [pooling-patterns](../code-samples/pooling-patterns.md) for usage examples.

______________________________________________________________________

## Step 5: String Building

Replace `StringBuilder` and string concatenation with ZString:

```csharp
// BAD: Multiple allocations
return $"Error at line {line}: {message} (code: {errorCode})";

// GOOD: Zero intermediate allocations
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(line);
sb.Append(": ");
sb.Append(message);
sb.Append(" (code: ");
sb.Append(errorCode);
sb.Append(')');
return sb.ToString();
```

See [zstring-migration](zstring-migration.md) for detailed patterns.

______________________________________________________________________

## Step 6: Array Pooling

| Scenario                                | Pool                 |
| --------------------------------------- | -------------------- |
| DynValue arrays in VM hot path          | `DynValueArrayPool`  |
| Object arrays for reflection/interop    | `ObjectArrayPool`    |
| Variable-size temporary buffers         | `SystemArrayPool<T>` |
| Small fixed-size buffers (\<=256 bytes) | `stackalloc`         |

______________________________________________________________________

## Verification

### Test Zero Allocation

```csharp
[Test]
public void Method_ShouldNotAllocate()
{
    MethodUnderTest();  // Warm up

    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1000; i++)
    {
        MethodUnderTest();
    }
    long after = GC.GetAllocatedBytesForCurrentThread();

    Assert.That(after - before, Is.LessThan(100));
}
```

______________________________________________________________________

## Quick Refactoring Checklist

- [ ] No LINQ in hot paths - converted to manual loops
- [ ] No closures capturing variables - using static lambdas or explicit loops
- [ ] Collections are pooled - using `ListPool<T>`, `HashSetPool<T>`, etc.
- [ ] No `new StringBuilder()` - using `ZStringBuilder.Create()`
- [ ] Arrays are pooled or stackalloc
- [ ] Pools are disposed - all `PooledResource<T>` in `using` blocks
- [ ] Tested for allocations - verified with allocation test

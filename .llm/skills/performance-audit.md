# Performance Audit Quick Reference

> **When to use:** Reviewing or writing hot-path code, interpreter loops, or any allocation-sensitive sections.
>
> **Full details:** See [high-performance-csharp.md](high-performance-csharp.md)

______________________________________________________________________

## Quick Allocation Checklist

| Pattern                    | Problem               | Fix                                  |
| -------------------------- | --------------------- | ------------------------------------ |
| `foreach` on non-generic   | Boxing iterator       | Use `for` loop or generic collection |
| `params object[]`          | Array + boxing        | Use overloads or `Span<T>`           |
| Lambda capturing locals    | Closure allocation    | Cache delegate or use static lambda  |
| `string.Format()`          | String + boxing       | `ZString.Format()`                   |
| `$"interpolation"`         | String allocation     | `ZString.Concat()`                   |
| `.ToList()` / `.ToArray()` | Collection allocation | Iterate directly or use pooled       |
| `new List<T>()` in loop    | Repeated allocation   | Pool with `ListPool<T>.Get()`        |
| Boxing value types         | Heap allocation       | Generic constraints, `Span<T>`       |

______________________________________________________________________

## LINQ → Loop Patterns

```csharp
// ❌ LINQ allocates
var result = items.Where(x => x.IsValid).Select(x => x.Value).ToList();

// ✅ Zero-alloc loop
using var list = ListPool<int>.Get();
for (int i = 0; i < items.Count; i++)
{
    if (items[i].IsValid)
        list.Value.Add(items[i].Value);
}
```

```csharp
// ❌ Any() allocates enumerator
if (items.Any(x => x.Flag))

// ✅ Simple loop
bool found = false;
for (int i = 0; i < items.Count; i++)
{
    if (items[i].Flag) { found = true; break; }
}
```

______________________________________________________________________

## Closure Traps

```csharp
// ❌ Captures 'threshold' - allocates closure each call
void Process(int threshold)
{
    items.ForEach(x => x.Value > threshold);
}

// ✅ Static lambda - no capture, no allocation
items.ForEach(static x => x.Value > 0);

// ✅ Pass state explicitly
for (int i = 0; i < items.Count; i++)
{
    ProcessItem(items[i], threshold);
}
```

______________________________________________________________________

## Collection Pooling Quick Ref

```csharp
// List<T>
using var list = ListPool<DynValue>.Get();
list.Value.Add(item);

// HashSet<T>
using var set = HashSetPool<string>.Get();
set.Value.Add(key);

// Dictionary<K,V>
using var dict = DictionaryPool<string, int>.Get();
dict.Value[key] = value;

// DynValue arrays (interpreter hot path)
using var arr = DynValueArrayPool.Get(size);
arr.Span[0] = value;

// object[] arrays
using var arr = ObjectArrayPool.Get(size);

// Generic array pooling
using var arr = SystemArrayPool<byte>.Get(bufferSize);
```

**⚠️ Always use `using` or manually return to pool!**

______________________________________________________________________

## String Allocation Patterns

| Allocating                | Zero-Alloc Alternative          |
| ------------------------- | ------------------------------- |
| `$"{a} + {b}"`            | `ZString.Concat(a, " + ", b)`   |
| `string.Format("{0}", x)` | `ZString.Format("{0}", x)`      |
| `sb.ToString()`           | `ZString.CreateStringBuilder()` |
| `string.Join(",", arr)`   | `ZString.Join(',', arr)`        |
| `int.ToString()`          | Write to `Span<char>`           |

```csharp
// ❌ Allocates
string msg = $"Error at line {line}: {message}";

// ✅ Zero-alloc
using var sb = ZString.CreateStringBuilder();
sb.Append("Error at line ");
sb.Append(line);
sb.Append(": ");
sb.Append(message);
string msg = sb.ToString(); // Single allocation at end
```

______________________________________________________________________

## Quick Wins Table

| Pattern                               | Replacement                                |
| ------------------------------------- | ------------------------------------------ |
| `list.Count == 0`                     | `list.Count == 0` (same, avoid `.Any()`)   |
| `dict.ContainsKey(k) ? dict[k] : def` | `dict.TryGetValue(k, out var v) ? v : def` |
| `list.Find(x => ...)`                 | `for` loop with early return               |
| `array.Clone()`                       | `Array.Copy()` to pooled array             |
| `Enumerable.Range(0, n)`              | `for (int i = 0; i < n; i++)`              |
| `items.FirstOrDefault()`              | `items.Count > 0 ? items[0] : default`     |
| `new StringBuilder()`                 | `ZString.CreateStringBuilder()`            |

______________________________________________________________________

## Profiling Checklist

1. **Build Release mode** - Debug has different allocation patterns
1. **Run with allocation profiler** - JetBrains dotMemory or VS Diagnostic Tools
1. **Focus on hot paths** - Interpreter main loop, bytecode execution
1. **Check for:**
   - [ ] Boxing (value types → object)
   - [ ] Closure allocations (lambdas capturing variables)
   - [ ] Iterator allocations (foreach on non-List/Array)
   - [ ] String concatenation
   - [ ] Unpooled collection creation
1. **Verify with BenchmarkDotNet** - Use `[MemoryDiagnoser]` attribute
1. **Target: 0 bytes allocated** on hot paths

```csharp
[MemoryDiagnoser]
public class MyBenchmarks
{
    [Benchmark]
    public void HotPath() { /* ... */ }
}
// Output shows "Allocated: 0 B" = success
```

______________________________________________________________________

*Keep interpreter hot paths allocation-free. When in doubt, profile.*

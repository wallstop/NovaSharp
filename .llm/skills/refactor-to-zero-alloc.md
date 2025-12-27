# Skill: Refactoring to Zero-Allocation Patterns

**When to use**: Eliminating GC allocations from existing code by converting to pooled collections, eliminating LINQ, removing closures, and using zero-allocation string building.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (general guidelines), [zstring-migration](zstring-migration.md) (string building), [span-optimization](span-optimization.md) (span-based operations)

______________________________________________________________________

## Refactoring Process Overview

Follow these steps **in order** when refactoring allocating code:

| Step | Focus                   | Tools                                                        |
| ---- | ----------------------- | ------------------------------------------------------------ |
| 1    | Identify Allocations    | Regex search, profiler, code review                          |
| 2    | LINQ → Loop Conversion  | Manual loop replacement                                      |
| 3    | Closure Elimination     | Extract to explicit loops, static lambdas, pass state        |
| 4    | Collection Pooling      | `ListPool<T>`, `HashSetPool<T>`, `DictionaryPool<K,V>`       |
| 5    | StringBuilder → ZString | `ZStringBuilder.Create()`, `ZString.Concat()`                |
| 6    | Array Pooling           | `DynValueArrayPool`, `ObjectArrayPool`, `SystemArrayPool<T>` |

______________________________________________________________________

## Step 1: Identify Allocations

### Common Allocation Sources Checklist

| Source                | Example                              | Allocation Type          |
| --------------------- | ------------------------------------ | ------------------------ |
| LINQ methods          | `.Where()`, `.Select()`, `.ToList()` | Iterator + result        |
| `new List<T>()`       | `new List<DynValue>()`               | List + backing array     |
| `new Dictionary<K,V>` | `new Dictionary<string, int>()`      | Dictionary + buckets     |
| `new HashSet<T>()`    | `new HashSet<int>()`                 | HashSet + slots          |
| String interpolation  | `$"Error: {msg}"`                    | Intermediate strings     |
| String concatenation  | `"a" + b + "c"`                      | Multiple strings         |
| `StringBuilder`       | `new StringBuilder()`                | StringBuilder + buffer   |
| Lambda closures       | `list.Find(x => x.Id == targetId)`   | Closure class + delegate |
| `new T[]`             | `new DynValue[count]`                | Array                    |
| `.ToArray()`          | `list.ToArray()`                     | New array copy           |
| Boxing                | `object o = 42;`                     | Boxed value              |
| `foreach` on struct   | `foreach (var kv in dict)`           | Enumerator allocation\*  |
| `params` arrays       | `Method(arg1, arg2, arg3)`           | Params array             |

\*Note: Dictionary and HashSet `foreach` may allocate in some .NET versions.

### Regex Patterns to Search for Allocations

```bash
# LINQ methods (high priority)
rg '\.Where\(|\.Select\(|\.First\(|\.FirstOrDefault\(|\.Any\(|\.Count\(.*=>|\.ToList\(\)|\.ToArray\(\)|\.OrderBy\(' src/runtime/ --type cs

# New collections
rg 'new (List|Dictionary|HashSet|Queue|Stack)<' src/runtime/ --type cs

# String operations
rg '\$"|new StringBuilder|\.ToString\(\).*\+|"\s*\+\s*[^"]+\s*\+' src/runtime/ --type cs

# Lambda closures (capturing variables)
rg '\(\w+\s*=>\s*.*\w+\)' src/runtime/ --type cs

# Array allocations
rg 'new \w+\[\d+\]|new \w+\[.*\]' src/runtime/ --type cs

# Boxing
rg 'object\s+\w+\s*=\s*\d+|\.Add\(\s*\d+\s*\)' src/runtime/ --type cs
```

______________________________________________________________________

## Step 2: LINQ → Loop Conversion

### Pattern: Where + FirstOrDefault

```csharp
// ❌ BEFORE: Allocates iterator
var item = collection.Where(x => x.Id == targetId).FirstOrDefault();

// ✅ AFTER: Manual loop
TItem item = default;
foreach (var x in collection)
{
    if (x.Id == targetId)
    {
        item = x;
        break;
    }
}
```

### Pattern: Where + ToList

```csharp
// ❌ BEFORE: Allocates iterator + new list
var matches = items.Where(x => x.IsActive).ToList();

// ✅ AFTER: Pooled list with manual filtering
using (ListPool<Item>.Get(out List<Item> matches))
{
    foreach (var item in items)
    {
        if (item.IsActive)
        {
            matches.Add(item);
        }
    }
    // Use matches here...
}
```

### Pattern: Select + ToArray

```csharp
// ❌ BEFORE: Allocates iterator + new array
var names = items.Select(x => x.Name).ToArray();

// ✅ AFTER: Direct array population
string[] names = new string[items.Count];
for (int i = 0; i < items.Count; i++)
{
    names[i] = items[i].Name;
}
```

### Pattern: Any

```csharp
// ❌ BEFORE: Allocates iterator (with closure if predicate captures)
bool hasActive = items.Any(x => x.Status == status);

// ✅ AFTER: Manual loop
bool hasActive = false;
foreach (var item in items)
{
    if (item.Status == status)
    {
        hasActive = true;
        break;
    }
}
```

### Pattern: Count with Predicate

```csharp
// ❌ BEFORE: Allocates iterator
int activeCount = items.Count(x => x.IsActive);

// ✅ AFTER: Manual count
int activeCount = 0;
foreach (var item in items)
{
    if (item.IsActive)
    {
        activeCount++;
    }
}
```

### Pattern: Sum

```csharp
// ❌ BEFORE: Allocates iterator
int total = items.Sum(x => x.Value);

// ✅ AFTER: Manual sum
int total = 0;
foreach (var item in items)
{
    total += item.Value;
}
```

### Pattern: OrderBy + ToList

```csharp
// ❌ BEFORE: Multiple allocations
var sorted = items.OrderBy(x => x.Priority).ToList();

// ✅ AFTER: In-place sort with pooled list
using (ListPool<Item>.Get(items.Count, out List<Item> sorted))
{
    sorted.AddRange(items);
    sorted.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    // Use sorted here...
}
```

______________________________________________________________________

## Step 3: Closure Elimination Techniques

### 🔴 Why Closures Allocate

When a lambda captures a variable from its enclosing scope, the compiler generates a hidden class to hold the captured values. This class is allocated on the heap.

```csharp
// This allocates a closure class every call because 'targetId' is captured
void FindItem(int targetId)
{
    var item = list.Find(x => x.Id == targetId);  // ❌ Allocates closure
}
```

### Technique 1: Extract to Explicit Loop

```csharp
// ❌ BEFORE: Closure captures 'targetId'
var item = list.Find(x => x.Id == targetId);

// ✅ AFTER: No allocation
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

### Technique 2: Static Lambda for Cached Delegates

When no capture is needed, use a static lambda (C# 9+) or a static readonly delegate:

```csharp
// ❌ BEFORE: Allocates delegate each call
items.Sort((a, b) => a.Priority.CompareTo(b.Priority));

// ✅ AFTER: Cached static delegate
private static readonly Comparison<Item> PriorityComparison = 
    static (a, b) => a.Priority.CompareTo(b.Priority);

items.Sort(PriorityComparison);
```

### Technique 3: Pass State via Parameter

Some APIs accept state as a parameter to avoid closures:

```csharp
// ❌ BEFORE: Closure captures 'threshold'
int threshold = 10;
bool found = Array.Exists(items, x => x.Value > threshold);

// ✅ AFTER: Use state parameter pattern (when API supports it)
// For custom implementations, design APIs with state parameters:
public static bool Find<T, TState>(T[] array, TState state, Func<T, TState, bool> predicate)
{
    foreach (var item in array)
    {
        if (predicate(item, state))
            return true;
    }
    return false;
}

// Usage: no closure
bool found = Find(items, threshold, static (x, thresh) => x.Value > thresh);
```

### Technique 4: Remove 'this' Capture

Instance method references implicitly capture `this`:

```csharp
// ❌ BEFORE: Captures 'this' via method group
items.ForEach(ProcessItem);  // ProcessItem is instance method

// ✅ AFTER: Use explicit loop
foreach (var item in items)
{
    ProcessItem(item);
}

// OR: Make ProcessItem static if it doesn't need instance state
private static void ProcessItem(Item item) { ... }
```

______________________________________________________________________

## Step 4: Collection Pooling Migration

### NovaSharp Collection Pool APIs

All pools are in namespace `WallstopStudios.NovaSharp.Interpreter.DataStructs`.

| Pool                  | Get Method                                     | Returns                           |
| --------------------- | ---------------------------------------------- | --------------------------------- |
| `ListPool<T>`         | `ListPool<T>.Get(out List<T>)`                 | `PooledResource<List<T>>`         |
| `ListPool<T>`         | `ListPool<T>.Get(capacity, out List<T>)`       | `PooledResource<List<T>>`         |
| `HashSetPool<T>`      | `HashSetPool<T>.Get(out HashSet<T>)`           | `PooledResource<HashSet<T>>`      |
| `DictionaryPool<K,V>` | `DictionaryPool<K,V>.Get(out Dictionary<K,V>)` | `PooledResource<Dictionary<K,V>>` |
| `StackPool<T>`        | `StackPool<T>.Get(out Stack<T>)`               | `PooledResource<Stack<T>>`        |
| `QueuePool<T>`        | `QueuePool<T>.Get(out Queue<T>)`               | `PooledResource<Queue<T>>`        |

### Pattern: Method Creating List

```csharp
// ❌ BEFORE: Allocates new list
List<DynValue> values = new List<DynValue>();
foreach (var item in source)
{
    values.Add(ConvertItem(item));
}
return ProcessValues(values);

// ✅ AFTER: Pooled list
using (ListPool<DynValue>.Get(out List<DynValue> values))
{
    foreach (var item in source)
    {
        values.Add(ConvertItem(item));
    }
    return ProcessValues(values);
}
```

### Pattern: List with Known Capacity

```csharp
// ❌ BEFORE: Allocates with estimated capacity (still allocates!)
List<DynValue> values = new List<DynValue>(estimatedSize);

// ✅ AFTER: Pooled with capacity hint
using (ListPool<DynValue>.Get(estimatedSize, out List<DynValue> values))
{
    // List has at least 'estimatedSize' capacity
    // ...
}
```

### Pattern: Temporary HashSet

```csharp
// ❌ BEFORE: Allocates new hashset
var seen = new HashSet<int>();
foreach (var item in items)
{
    if (!seen.Add(item.Id))
    {
        // Duplicate found
    }
}

// ✅ AFTER: Pooled hashset
using (HashSetPool<int>.Get(out HashSet<int> seen))
{
    foreach (var item in items)
    {
        if (!seen.Add(item.Id))
        {
            // Duplicate found
        }
    }
}
```

### Pattern: Temporary Dictionary

```csharp
// ❌ BEFORE: Allocates new dictionary
var lookup = new Dictionary<string, Item>();
foreach (var item in items)
{
    lookup[item.Key] = item;
}
// Use lookup...

// ✅ AFTER: Pooled dictionary
using (DictionaryPool<string, Item>.Get(out Dictionary<string, Item> lookup))
{
    foreach (var item in items)
    {
        lookup[item.Key] = item;
    }
    // Use lookup within the using block...
}
```

### Pattern: Manual Rent/Return for Complex Lifetimes

When the collection lifetime doesn't fit a `using` block:

```csharp
// For fields or complex lifetimes, use Rent/Return pattern
private HashSet<int> _indices;

void BeginOperation()
{
    _indices = HashSetPool<int>.Rent();
}

void EndOperation()
{
    if (_indices != null)
    {
        HashSetPool<int>.Return(_indices);
        _indices = null;
    }
}
```

______________________________________________________________________

## Step 5: StringBuilder Patterns

### Pattern: String Concatenation in Loop → ZStringBuilder

```csharp
// ❌ BEFORE: O(n²) allocations
string result = "";
foreach (var item in items)
{
    result += item.Name + ", ";
}

// ✅ AFTER: Single allocation at ToString()
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
foreach (var item in items)
{
    sb.Append(item.Name);
    sb.Append(", ");
}
return sb.ToString();
```

### Pattern: String Interpolation → ZStringBuilder

```csharp
// ❌ BEFORE: Multiple intermediate allocations
return $"Error at line {line}: {message} (code: {errorCode})";

// ✅ AFTER: Zero intermediate allocations
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

### Pattern: Simple Concatenation → ZString.Concat

```csharp
// ❌ BEFORE: Multiple allocations
return "\"" + value + "\"";

// ✅ AFTER: Zero allocation
return ZString.Concat("\"", value, "\"");
```

### Pattern: Multiple Format Calls

```csharp
// ❌ BEFORE: Multiple Format allocations
string header = string.Format("Function: {0}", name);
string params = string.Format("  Parameters: {0}", count);
return header + "\n" + params;

// ✅ AFTER: Single builder
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Function: ");
sb.AppendLine(name);
sb.Append("  Parameters: ");
sb.Append(count);
return sb.ToString();
```

### Pattern: Joining Collections → ZStringBuilder.Join

```csharp
// ❌ BEFORE: Allocates array + result
return string.Join(", ", items.Select(x => x.Name));

// ✅ AFTER: Zero allocation join (if names is already materialized)
return ZStringBuilder.Join(", ", names);
```

______________________________________________________________________

## Step 6: Array Pool Usage

### NovaSharp Array Pool APIs

All pools are in namespace `WallstopStudios.NovaSharp.Interpreter.DataStructs`.

| Pool                 | Get Method                                             | Use Case                              |
| -------------------- | ------------------------------------------------------ | ------------------------------------- |
| `DynValueArrayPool`  | `DynValueArrayPool.Get(size, out DynValue[])`          | Fixed exact-size DynValue arrays      |
| `ObjectArrayPool`    | `ObjectArrayPool.Get(size, out object[])`              | Fixed exact-size object[] (interop)   |
| `SystemArrayPool<T>` | `SystemArrayPool<T>.Get(size, out T[])`                | Variable-size buffers (may be larger) |
| `SystemArrayPool<T>` | `SystemArrayPool<T>.Get(size, clearOnReturn, out T[])` | Control clearing behavior             |

### 🔴 Critical: Pool Selection Guide

| Scenario                              | Pool                 | Notes                                  |
| ------------------------------------- | -------------------- | -------------------------------------- |
| DynValue arrays in VM hot path        | `DynValueArrayPool`  | Thread-local caching for sizes ≤16     |
| Object arrays for reflection/interop  | `ObjectArrayPool`    | Thread-local caching for sizes ≤8      |
| Variable-size temporary buffers       | `SystemArrayPool<T>` | May return larger array than requested |
| Small fixed-size buffers (≤256 bytes) | `stackalloc`         | Stack allocation, no pooling needed    |
| Large permanent arrays                | Direct allocation    | Long-lived objects don't benefit       |

### Pattern: Temporary Array (Variable Size)

```csharp
// ❌ BEFORE: Allocates new array
char[] buffer = new char[estimatedSize];
int written = FormatValue(value, buffer);
return new string(buffer, 0, written);

// ✅ AFTER: Pooled buffer (may be larger than requested!)
using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(
    estimatedSize, clearOnReturn: false, out char[] buffer);
int written = FormatValue(value, buffer);
return new string(buffer, 0, written);
```

### Pattern: DynValue Array in VM

```csharp
// ❌ BEFORE: Allocates every call
DynValue[] args = new DynValue[argCount];

// ✅ AFTER: Pooled with automatic return
using (DynValueArrayPool.Get(argCount, out DynValue[] args))
{
    // Populate and use args...
    // Returns to pool when disposed
}
```

### Pattern: Object Array for Reflection

```csharp
// ❌ BEFORE: Allocates for every method invocation
object[] parameters = new object[paramCount];
// ... populate ...
methodInfo.Invoke(target, parameters);

// ✅ AFTER: Pooled object array
using (ObjectArrayPool.Get(paramCount, out object[] parameters))
{
    // ... populate ...
    methodInfo.Invoke(target, parameters);
}
```

### Pattern: Fixed-Size Buffer → stackalloc

```csharp
// ❌ BEFORE: Heap allocation for small buffer
byte[] bytes = new byte[8];
WriteDouble(value, bytes);

// ✅ AFTER: Stack allocation (no heap, no pooling overhead)
Span<byte> bytes = stackalloc byte[8];
WriteDouble(value, bytes);
```

### Pattern: Converting Pooled Array to Final Array

```csharp
// When you need the final result as an array:
using (DynValueArrayPool.Get(maxSize, out DynValue[] buffer))
{
    int actualCount = PopulateBuffer(buffer);
    
    // Create exact-sized result array
    DynValue[] result = new DynValue[actualCount];
    Array.Copy(buffer, result, actualCount);
    return result;
}

// OR use ToArrayAndReturn for lists:
List<DynValue> list = ListPool<DynValue>.Rent(capacity);
// ... populate list ...
return ListPool<DynValue>.ToArrayAndReturn(list);  // Returns list to pool
```

______________________________________________________________________

## Complete Refactoring Example

### Before (Multiple Allocation Issues)

```csharp
public string FormatErrors(List<Error> errors, int threshold)
{
    // ❌ Issue 1: LINQ with closure (captures 'threshold')
    var filtered = errors.Where(e => e.Severity > threshold).ToList();
    
    // ❌ Issue 2: StringBuilder allocation
    var sb = new StringBuilder();
    sb.AppendLine("Errors found:");
    
    foreach (var error in filtered)
    {
        // ❌ Issue 3: String interpolation inside loop
        sb.AppendLine($"  [{error.Code}] {error.Message}");
    }
    
    // ❌ Issue 4: LINQ Count
    sb.AppendLine($"Total: {filtered.Count(e => e.IsCritical)} critical");
    
    return sb.ToString();
}
```

### After (Zero Allocation in Hot Path)

```csharp
public string FormatErrors(List<Error> errors, int threshold)
{
    // ✅ Fix 1: Pooled list with manual filtering
    using (ListPool<Error>.Get(out List<Error> filtered))
    {
        foreach (var error in errors)
        {
            if (error.Severity > threshold)
            {
                filtered.Add(error);
            }
        }
        
        // ✅ Fix 2: ZStringBuilder instead of StringBuilder
        using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
        sb.AppendLine("Errors found:");
        
        // ✅ Fix 3: Manual count, no closure
        int criticalCount = 0;
        
        foreach (var error in filtered)
        {
            // ✅ Fix 4: Append segments instead of interpolation
            sb.Append("  [");
            sb.Append(error.Code);
            sb.Append("] ");
            sb.AppendLine(error.Message);
            
            if (error.IsCritical)
            {
                criticalCount++;
            }
        }
        
        sb.Append("Total: ");
        sb.Append(criticalCount);
        sb.AppendLine(" critical");
        
        return sb.ToString();
    }
}
```

______________________________________________________________________

## Verification: Confirming Zero Allocations

### Method 1: GC.GetAllocatedBytesForCurrentThread()

```csharp
[Test]
public void Method_ShouldNotAllocate()
{
    // Warm up
    MethodUnderTest();
    
    // Force collection to get stable baseline
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    long before = GC.GetAllocatedBytesForCurrentThread();
    
    for (int i = 0; i < 1000; i++)
    {
        MethodUnderTest();
    }
    
    long after = GC.GetAllocatedBytesForCurrentThread();
    long allocated = after - before;
    
    // Allow small tolerance for measurement overhead
    Assert.That(allocated, Is.LessThan(100), 
        $"Method allocated {allocated} bytes over 1000 iterations");
}
```

### Method 2: BenchmarkDotNet with MemoryDiagnoser

```csharp
[MemoryDiagnoser]
public class AllocationBenchmarks
{
    [Benchmark]
    public void MethodUnderTest()
    {
        // Your method here
    }
}
```

Run with:

```bash
dotnet run -c Release -- --filter "*MethodUnderTest*"
```

Check `Allocated` column in results - should show `0 B` or `-` for zero allocations.

______________________________________________________________________

## Quick Refactoring Checklist

Before submitting refactored code, verify:

- [ ] **No LINQ in hot paths** — Converted to manual loops
- [ ] **No closures capturing variables** — Using static lambdas or explicit loops
- [ ] **Collections are pooled** — Using `ListPool<T>`, `HashSetPool<T>`, etc.
- [ ] **No `new StringBuilder()`** — Using `ZStringBuilder.Create()`
- [ ] **No string interpolation** — Using `ZString.Concat()` or `ZStringBuilder`
- [ ] **Arrays are pooled or stackalloc** — Using appropriate pool or stack allocation
- [ ] **No boxing** — Using generic methods where possible
- [ ] **Pools are disposed** — All `PooledResource<T>` in `using` blocks
- [ ] **Tested for allocations** — Verified with allocation test or profiler

______________________________________________________________________

## Related Skills

- [high-performance-csharp](high-performance-csharp.md) — General high-performance patterns and pool API reference
- [zstring-migration](zstring-migration.md) — Detailed ZString migration patterns
- [span-optimization](span-optimization.md) — Span-based string and array operations

______________________________________________________________________

## Source References

- [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs) — ListPool, HashSetPool, DictionaryPool, StackPool, QueuePool
- [DataStructs/DynValueArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/DynValueArrayPool.cs) — DynValue array pooling
- [DataStructs/ObjectArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ObjectArrayPool.cs) — Object array pooling for interop
- [DataStructs/SystemArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/SystemArrayPool.cs) — Generic array pooling via ArrayPool<T>.Shared
- [DataStructs/ZStringBuilder.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ZStringBuilder.cs) — ZString wrapper factory methods

# Pooling Patterns

Reusable code samples for zero-allocation pooling in NovaSharp.

______________________________________________________________________

## ListPool

Pool for `List<T>` instances. Use for temporary lists in methods.

```csharp
// RAII pattern - automatic cleanup
using PooledResource<List<int>> pooled = ListPool<int>.Get(out List<int> items);
items.Add(1);
items.Add(2);
// items returned to pool when pooled goes out of scope
```

### With ownership transfer

```csharp
public PooledResource<List<int>> GetResults()
{
    PooledResource<List<int>> pooled = ListPool<int>.Get(out List<int> list);
    list.Add(1);
    list.Add(2);
    pooled.SuppressReturn();  // Caller takes ownership
    return pooled;
}

// Caller is responsible for disposal
using PooledResource<List<int>> results = GetResults();
```

______________________________________________________________________

## HashSetPool

Pool for `HashSet<T>` instances.

```csharp
using PooledResource<HashSet<string>> pooled = HashSetPool<string>.Get(out HashSet<string> seen);
seen.Add("item1");
if (!seen.Contains("item2"))
{
    seen.Add("item2");
}
```

______________________________________________________________________

## DictionaryPool

Pool for `Dictionary<K,V>` instances.

```csharp
using PooledResource<Dictionary<string, int>> pooled = DictionaryPool<string, int>.Get(out Dictionary<string, int> counts);
counts["a"] = 1;
counts["b"] = 2;
```

______________________________________________________________________

## DynValueArrayPool

Fixed exact-size `DynValue[]` pool for VM frames and Lua calls.

```csharp
// Get exact-size array for VM call
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(5, out DynValue[] args);
// args.Length == 5 (exact)
args[0] = DynValue.NewNumber(1);
args[1] = DynValue.NewString("hello");
```

______________________________________________________________________

## ObjectArrayPool

Fixed exact-size `object[]` pool for reflection and interop.

```csharp
// Required for MethodInfo.Invoke which needs exact parameter count
using PooledResource<object[]> pooled = ObjectArrayPool.Get(3, out object[] parameters);
// parameters.Length == 3 (exact)
parameters[0] = arg1;
parameters[1] = arg2;
parameters[2] = arg3;
object result = methodInfo.Invoke(target, parameters);
```

______________________________________________________________________

## SystemArrayPool

Variable-size array pool wrapping `ArrayPool<T>.Shared`.

```csharp
// RAII pattern (recommended)
using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(256, out char[] buffer);
// buffer.Length >= 256 (may be larger!)
int written = FormatValue(value, buffer);
return new string(buffer, 0, written);

// With clearing control
using PooledResource<int[]> pooled = SystemArrayPool<int>.Get(1000, clearOnReturn: false, out int[] array);

// Extract exact-size copy and return pooled array
char[] buffer = SystemArrayPool<char>.Rent(1024);
int actualLength = Fill(buffer);
return SystemArrayPool<char>.ToArrayAndReturn(buffer, actualLength);
```

______________________________________________________________________

## Owning Pooled Resources in Types

When a type needs to store pooled resources beyond a single method:

```csharp
internal sealed class Loop : IDisposable
{
    private PooledResource<List<Instruction>> _pooledBreakJumps = ListPool<Instruction>.Get(out _);
    private bool _disposed;

    public List<Instruction> BreakJumps => _pooledBreakJumps.Resource;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pooledBreakJumps.Dispose();
    }
}

// Usage
using Loop loop = new() { Scope = stackFrame };
loop.BreakJumps.Add(instruction);
// Automatically returned when loop is disposed
```

______________________________________________________________________

## LINQ to Loop Conversions

### Where + ToList

```csharp
// BAD: Allocates iterator + new list
List<Item> matches = items.Where(x => x.IsActive).ToList();

// GOOD: Pooled list with manual filtering
using (ListPool<Item>.Get(out List<Item> matches))
{
    foreach (Item item in items)
    {
        if (item.IsActive)
        {
            matches.Add(item);
        }
    }
    // Use matches here...
}
```

### Select + ToArray

```csharp
// BAD: Allocates iterator + new array
string[] names = items.Select(x => x.Name).ToArray();

// GOOD: Direct array population
string[] names = new string[items.Count];
for (int i = 0; i < items.Count; i++)
{
    names[i] = items[i].Name;
}
```

### Any with predicate

```csharp
// BAD: Allocates iterator with closure
bool hasActive = items.Any(x => x.Status == status);

// GOOD: Manual loop
bool hasActive = false;
foreach (Item item in items)
{
    if (item.Status == status)
    {
        hasActive = true;
        break;
    }
}
```

______________________________________________________________________

## Pool Selection Flowchart

```text
What kind of buffer do you need?
|
+-- DynValue array (VM frames, Lua calls)?
|   --> DynValueArrayPool.Get(exactSize, out array)
|
+-- Object array (reflection, interop)?
|   --> ObjectArrayPool.Get(exactSize, out array)
|
+-- Generic array?
|   +-- Size is compile-time constant (8, 16, 64)?
|   |   --> stackalloc T[size] (if small) or create pool
|   +-- Size varies at runtime?
|       --> SystemArrayPool<T>.Get(size, out array)
|           WARNING: Use pooled.Length, NOT array.Length!
|
+-- List<T>, HashSet<T>, Dictionary<K,V>?
|   --> ListPool<T>.Get(), HashSetPool<T>.Get(), DictionaryPool<K,V>.Get()
|
+-- StringBuilder?
    --> ZStringBuilder.Create() (zero-alloc)
```

______________________________________________________________________

## Thread-Local Caching Pattern

For fixed-size objects allocated frequently in hot paths:

```csharp
[ThreadStatic]
private static char[] t_formatBuffer;

private static char[] GetFormatBuffer(int minSize)
{
    char[] buffer = t_formatBuffer;
    if (buffer == null || buffer.Length < minSize)
    {
        buffer = new char[Math.Max(minSize, 256)];
        t_formatBuffer = buffer;
    }
    return buffer;
}

// Usage in hot path:
char[] buffer = GetFormatBuffer(64);
// Use buffer...
// No cleanup needed - persists for thread lifetime
```

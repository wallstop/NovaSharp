# High-Performance .NET Libraries Research

> Research Summary for NovaSharp Lua Interpreter Optimization
> Date: December 7, 2025

## Executive Summary

This document evaluates high-performance .NET libraries for potential use in optimizing NovaSharp's VM loop, string handling, and collection operations. All libraries evaluated are MIT licensed and compatible with .NET Standard 2.1 (required for Unity/Mono compatibility).

______________________________________________________________________

## 1. ZLinq (Cysharp) ⭐⭐⭐⭐⭐

**Repository:** https://github.com/Cysharp/ZLinq\
**License:** MIT\
**Compatibility:** .NET Standard 2.0, 2.1, .NET 8, .NET 9\
**NuGet:** `ZLinq`

### Key Features

- **Zero-allocation LINQ** through struct-based `ValueEnumerable<TEnumerator, T>`
- Method chains don't increase allocations
- SIMD acceleration for aggregation operations (Sum, Min, Max, Average, Contains, SequenceEqual)
- `ToArrayPool()` for temporary materialization without allocation
- Drop-in replacement generator available

### Relevant APIs for NovaSharp

```csharp
// Zero-alloc iteration
source.AsValueEnumerable().Where(x => x.Active).Select(x => x.Value);

// Pooled array for temporary results
using var pooled = items.AsValueEnumerable().Where(predicate).ToArrayPool();

// SIMD-accelerated aggregation
var sum = numbers.AsValueEnumerable().Sum();
```

### Unity Compatibility

- Full Unity support (2022.3.12f1+)
- Works with IL2CPP
- No SIMD in Unity (netstandard2.1), but still zero-allocation

### Applicability to NovaSharp

| Use Case                  | Benefit                      | Priority |
| ------------------------- | ---------------------------- | -------- |
| Table iteration           | Zero-alloc Where/Select      | High     |
| VM instruction lookup     | Zero-alloc filtering         | High     |
| Coroutine management      | Pooled temporary collections | Medium   |
| String library operations | SIMD aggregation             | Medium   |

______________________________________________________________________

## 2. CommunityToolkit.HighPerformance ⭐⭐⭐⭐⭐

**Repository:** https://github.com/CommunityToolkit/dotnet\
**License:** MIT\
**Compatibility:** .NET Standard 2.0, 2.1, .NET 6, .NET 7\
**NuGet:** `CommunityToolkit.HighPerformance`

### Key Features

- **StringPool**: Configurable string interning pool (critical for Lua string handling)
- **MemoryOwner<T>** / **SpanOwner<T>**: IMemoryOwner with embedded length and fast Span accessor
- **Span2D<T>** / **Memory2D<T>**: 2D memory abstractions (useful for matrix operations)
- **ArrayPoolBufferWriter<T>**: IBufferWriter using pooled arrays
- **HashCode<T>**: SIMD-enabled hash computation
- **Ref<T>** / **NullableRef<T>**: Stack-only reference wrappers
- **Box<T>**: Utility methods for boxed value types

### Relevant APIs for NovaSharp

```csharp
// String pooling (critical for Lua string interning)
private static readonly StringPool _stringPool = new StringPool();
string interned = _stringPool.GetOrAdd(span);

// Pooled memory with ownership
using SpanOwner<byte> buffer = SpanOwner<byte>.Allocate(1024);
Span<byte> span = buffer.Span;

// Pooled buffer writer for serialization
using var writer = new ArrayPoolBufferWriter<byte>();

// Fast SIMD hashing
int hash = HashCode<int>.Combine(values.AsSpan());
```

### Applicability to NovaSharp

| Use Case            | Benefit                                        | Priority     |
| ------------------- | ---------------------------------------------- | ------------ |
| String interning    | StringPool replaces Dictionary\<string,string> | **Critical** |
| Bytecode buffers    | MemoryOwner<byte> for instruction storage      | High         |
| VM stack operations | SpanOwner for temporary allocations            | High         |
| Table hashing       | SIMD HashCode                                  | Medium       |

______________________________________________________________________

## 3. Microsoft.IO.RecyclableMemoryStream ⭐⭐⭐⭐

**Repository:** https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream\
**License:** MIT\
**Compatibility:** .NET Standard 2.0, 2.1, .NET 6+\
**NuGet:** `Microsoft.IO.RecyclableMemoryStream`

### Key Features

- Drop-in `MemoryStream` replacement with buffer pooling
- Eliminates LOH allocations through pooled buffers
- Implements `IBufferWriter<byte>` for zero-copy scenarios
- Supports `ReadOnlySequence<byte>` for streaming reads
- ETW events and metrics for debugging

### Relevant APIs for NovaSharp

```csharp
// Create manager (singleton)
private static readonly RecyclableMemoryStreamManager _streamManager = new(new RecyclableMemoryStreamManager.Options
{
    BlockSize = 4096,
    LargeBufferMultiple = 1024 * 1024,
    MaximumBufferSize = 16 * 1024 * 1024,
    MaximumSmallPoolFreeBytes = 256 * 1024,
    MaximumLargePoolFreeBytes = 4 * 1024 * 1024,
});

// Get pooled stream
using var stream = _streamManager.GetStream("Compiler.Bytecode");
stream.Write(bytecode, 0, bytecode.Length);

// Zero-copy read via ReadOnlySequence
foreach (var memory in stream.GetReadOnlySequence())
{
    ProcessChunk(memory.Span);
}
```

### Applicability to NovaSharp

| Use Case               | Benefit                | Priority |
| ---------------------- | ---------------------- | -------- |
| Bytecode serialization | Pooled streams, no LOH | High     |
| Script loading         | Efficient file reading | Medium   |
| Debug output           | Pooled string building | Low      |

______________________________________________________________________

## 4. Microsoft.Extensions.ObjectPool ⭐⭐⭐⭐

**Repository:** https://github.com/dotnet/aspnetcore (part of ASP.NET Core)\
**License:** MIT\
**Compatibility:** .NET Standard 2.0, .NET Framework 4.6.2+\
**NuGet:** `Microsoft.Extensions.ObjectPool`

### Key Features

- Thread-safe generic object pooling
- `IResettable` interface for automatic reset on return
- `PooledObjectPolicy<T>` for custom creation/reset logic
- `LeakTrackingObjectPool<T>` for debugging

### Relevant APIs for NovaSharp

```csharp
// Create pool with policy
var policy = new DefaultPooledObjectPolicy<DynValue>();
var pool = new DefaultObjectPool<DynValue>(policy, maximumRetained: 1000);

// Use pooled object
var value = pool.Get();
try
{
    // use value
}
finally
{
    pool.Return(value);
}

// Custom resettable type
public class PooledCallInfo : IResettable
{
    public DynValue[] Arguments;
    public int ArgumentCount;
    
    public bool TryReset()
    {
        Array.Clear(Arguments, 0, ArgumentCount);
        ArgumentCount = 0;
        return true;
    }
}
```

### Applicability to NovaSharp

| Use Case            | Benefit                     | Priority     |
| ------------------- | --------------------------- | ------------ |
| DynValue allocation | Pool DynValue instances     | **Critical** |
| Call frame pooling  | Reduce GC in function calls | High         |
| Closure allocation  | Pool Closure instances      | High         |
| Table allocation    | Pool Table instances        | Medium       |

______________________________________________________________________

## 5. MemoryPack (Cysharp) ⭐⭐⭐

**Repository:** https://github.com/Cysharp/MemoryPack\
**License:** MIT\
**Compatibility:** .NET Standard 2.1 minimum\
**NuGet:** `MemoryPack`

### Key Features

- Zero-encoding binary serializer (memory copy when possible)
- Source generator based (no reflection)
- 10x faster than System.Text.Json
- Unity support with IL2CPP compatibility

### Applicability to NovaSharp

| Use Case               | Benefit                       | Priority |
| ---------------------- | ----------------------------- | -------- |
| Bytecode serialization | Fast compiled script caching  | Low      |
| State snapshots        | Coroutine state serialization | Low      |

**Note:** Less applicable to interpreter hot path; more useful for disk I/O scenarios.

______________________________________________________________________

## 6. System.Buffers.ArrayPool<T> (Built-in) ⭐⭐⭐⭐⭐

**Namespace:** System.Buffers\
**Compatibility:** .NET Standard 2.1 (built-in)

### Key Features

- Built-in array pooling, no external dependency
- Thread-safe shared pool via `ArrayPool<T>.Shared`
- Custom pools via `ArrayPool<T>.Create()`

### Relevant APIs for NovaSharp

```csharp
// Rent from shared pool
byte[] buffer = ArrayPool<byte>.Shared.Rent(minimumLength: 1024);
try
{
    // Use buffer (may be larger than requested!)
    int actualLength = Math.Min(needed, buffer.Length);
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
}

// Custom pool for specific sizes
private static readonly ArrayPool<DynValue> _valuePool = 
    ArrayPool<DynValue>.Create(maxArrayLength: 256, maxArraysPerBucket: 50);
```

### Applicability to NovaSharp

| Use Case             | Benefit                  | Priority     |
| -------------------- | ------------------------ | ------------ |
| VM stack arrays      | Pool execution stacks    | **Critical** |
| Function arguments   | Pool argument arrays     | **Critical** |
| Table backing arrays | Pool table storage       | High         |
| String buffers       | Pool char[] for building | High         |

______________________________________________________________________

## 7. ValueStringBuilder Pattern ⭐⭐⭐⭐

**Source:** Internal .NET runtime pattern (can be copied/adapted)

### Implementation Pattern

```csharp
// Stack-allocated string builder pattern
public ref struct ValueStringBuilder
{
    private char[] _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public void Append(char c)
    {
        if (_pos >= _chars.Length) Grow(1);
        _chars[_pos++] = c;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        if (_pos + value.Length > _chars.Length) Grow(value.Length);
        value.CopyTo(_chars.Slice(_pos));
        _pos += value.Length;
    }

    private void Grow(int additionalCapacity)
    {
        char[] newArray = ArrayPool<char>.Shared.Rent(
            Math.Max(_pos + additionalCapacity, _chars.Length * 2));
        _chars.Slice(0, _pos).CopyTo(newArray);
        
        char[] toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = newArray;
        if (toReturn != null) ArrayPool<char>.Shared.Return(toReturn);
    }

    public override string ToString()
    {
        return _chars.Slice(0, _pos).ToString();
    }

    public void Dispose()
    {
        if (_arrayToReturnToPool != null)
            ArrayPool<char>.Shared.Return(_arrayToReturnToPool);
    }
}

// Usage
Span<char> initialBuffer = stackalloc char[256];
var vsb = new ValueStringBuilder(initialBuffer);
try
{
    vsb.Append("Hello ");
    vsb.Append(name);
    return vsb.ToString();
}
finally
{
    vsb.Dispose();
}
```

### Applicability to NovaSharp

| Use Case             | Benefit                    | Priority     |
| -------------------- | -------------------------- | ------------ |
| String concatenation | Zero-alloc string building | **Critical** |
| Error messages       | Efficient formatting       | High         |
| Debug output         | Low-alloc logging          | Medium       |

______________________________________________________________________

## 8. Span/Memory Best Practices for .NET Standard 2.1

### Key Patterns

#### Slice Instead of Substring

```csharp
// Instead of
string sub = str.Substring(start, length);

// Use
ReadOnlySpan<char> span = str.AsSpan(start, length);
```

#### MemoryMarshal for Unsafe Reinterpretation

```csharp
// Reinterpret byte[] as int[]
Span<byte> bytes = stackalloc byte[16];
Span<int> ints = MemoryMarshal.Cast<byte, int>(bytes);
```

#### stackalloc for Small Buffers

```csharp
// Stack allocation for small, short-lived buffers
Span<char> buffer = stackalloc char[128];
// Use span...
```

#### Avoid Closure Allocations

```csharp
// Instead of capturing variables in lambdas
list.Where(x => x.Value > threshold); // allocates closure

// Pass state explicitly or use struct-based enumerators
foreach (var item in list.AsValueEnumerable())
{
    if (item.Value > threshold) yield return item;
}
```

______________________________________________________________________

## Recommendations for NovaSharp

### Immediate High-Impact Changes

1. **StringPool from CommunityToolkit.HighPerformance**

   - Replace any string interning dictionaries
   - Critical for Lua's string-heavy operations

1. **ArrayPool<T>.Shared for VM Stacks**

   - Pool execution stack arrays
   - Pool function argument arrays
   - Already built-in, zero dependencies

1. **ValueStringBuilder Pattern**

   - Implement or adapt for string concatenation in Lua
   - Critical for `string.format`, `..` operator, `tostring()`

1. **ObjectPool for DynValue**

   - Pool frequently allocated DynValue instances
   - Significant reduction in GC pressure

### Medium-Term Improvements

5. **ZLinq for Collection Operations**

   - Replace LINQ in hot paths with ZLinq
   - Especially beneficial for Table iteration

1. **RecyclableMemoryStream for I/O**

   - Script loading and bytecode serialization
   - Precompiled script caching

1. **SpanOwner<T> / MemoryOwner<T>**

   - Replace manual ArrayPool usage with ownership semantics
   - Cleaner API, harder to leak

### Package Dependencies Summary

| Package                             | NuGet ID                              | Priority     | Size Impact |
| ----------------------------------- | ------------------------------------- | ------------ | ----------- |
| CommunityToolkit.HighPerformance    | `CommunityToolkit.HighPerformance`    | **Critical** | ~525 KB     |
| Microsoft.Extensions.ObjectPool     | `Microsoft.Extensions.ObjectPool`     | High         | ~86 KB      |
| ZLinq                               | `ZLinq`                               | High         | ~200 KB     |
| Microsoft.IO.RecyclableMemoryStream | `Microsoft.IO.RecyclableMemoryStream` | Medium       | ~173 KB     |

### Unity-Specific Considerations

- All recommended packages work with Unity 2022.3+
- SIMD features in ZLinq don't apply to Unity (netstandard2.1)
- CommunityToolkit.HighPerformance fully supports Unity/IL2CPP
- Avoid `ref struct` types in Unity < 2022.3 (limited support)

______________________________________________________________________

## References

- [ZLinq GitHub](https://github.com/Cysharp/ZLinq)
- [CommunityToolkit.HighPerformance Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/high-performance/introduction)
- [RecyclableMemoryStream GitHub](https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream)
- [ObjectPool in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/objectpool)
- [ArrayPool<T> API](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1)

# NovaSharp Optimization Opportunities

> Last updated: 2025-12-06
> Context: Performance optimization audit for `PLAN.md` - Interpreter hot-path optimization initiative

This document catalogs potential optimizations to reduce allocations and improve throughput in the NovaSharp interpreter. Each section prioritizes opportunities by impact and provides concrete implementation guidance.

## Table of Contents

1. [Critical (VM Hot Loop)](#1-critical-vm-hot-loop)
1. [High (Lexer/Parser Performance)](#2-high-lexerparser-performance)
1. [Medium (Standard Library)](#3-medium-standard-library)
1. [Low (Compile-time / Debug Paths)](#4-low-compile-time--debug-paths)
1. [External Libraries to Evaluate](#5-external-libraries-to-evaluate)
1. [Implementation Roadmap](#6-implementation-roadmap)

______________________________________________________________________

## 1. Critical (VM Hot Loop)

These optimizations target the instruction execution loop and have the highest impact on runtime performance.

### 1.1 ArrayPool for DynValue[] Allocations

**Files affected:**

- `ProcessorUtilityFunctions.cs` (lines 25, 48, 137, 162)
- `ProcessorInstructionLoop.cs` (lines 436, 484, 961, 1104, 1244, 1265, 1328)

**Current pattern:**

```csharp
DynValue[] values = new DynValue[items];
// ... use values ...
// values falls out of scope and is GC'd
```

**Proposed solution:**

```csharp
// Add a pooling utility class
internal static class DynValueArrayPool
{
    // For small fixed sizes (1-8 elements), use static arrays recycled per-thread
    [ThreadStatic]
    private static DynValue[][] _threadLocalSmallArrays;
    
    private static DynValue[][] GetSmallArrays()
    {
        return _threadLocalSmallArrays ??= new DynValue[9][];
    }
    
    public static DynValue[] Rent(int size)
    {
        if (size <= 8)
        {
            DynValue[][] arrays = GetSmallArrays();
            DynValue[] cached = arrays[size];
            if (cached != null)
            {
                arrays[size] = null;
                return cached;
            }
        }
        return ArrayPool<DynValue>.Shared.Rent(size);
    }
    
    public static void Return(DynValue[] array, bool clearArray = true)
    {
        if (array.Length <= 8)
        {
            DynValue[][] arrays = GetSmallArrays();
            if (clearArray) Array.Clear(array, 0, array.Length);
            arrays[array.Length] = array;
        }
        else
        {
            ArrayPool<DynValue>.Shared.Return(array, clearArray);
        }
    }
}
```

**Estimated impact:** High - function calls are the most frequent allocation source in the VM loop.

### 1.2 StackTopToArray/StackTopToArrayReverse Optimization

**File:** `ProcessorUtilityFunctions.cs`

**Current pattern:**

```csharp
private DynValue[] StackTopToArray(int items, bool pop)
{
    DynValue[] values = new DynValue[items];
    // ...
    return values;
}
```

**Proposed solution:**
Add span-returning variants for cases where the caller doesn't need to persist the data:

```csharp
// For cases where we process inline and don't need persistence
private void ProcessStackTop(int items, Action<ReadOnlySpan<DynValue>> processor)
{
    // Use stackalloc for small sizes
    if (items <= 8)
    {
        Span<DynValue> values = stackalloc DynValue[items];
        // ... fill from stack ...
        processor(values);
        return;
    }
    // Fall back to pooled array
    DynValue[] rented = DynValueArrayPool.Rent(items);
    try
    {
        // ... fill rented ...
        processor(rented.AsSpan(0, items));
    }
    finally
    {
        DynValueArrayPool.Return(rented);
    }
}
```

### 1.3 CallStackItem List Allocations

**File:** `ProcessorInstructionLoop.cs` (lines 557, 560, 566, 967, 976)

**Current pattern:**

```csharp
if (stackframe.BlocksToClose == null)
{
    stackframe.BlocksToClose = new List<List<SymbolRef>>();
}
stackframe.BlocksToClose.Add(new List<SymbolRef>(closers));
```

**Proposed solution:**
Pre-allocate or pool these lists since call stacks are bounded:

```csharp
// Option 1: Pre-allocate in CallStackItem constructor
internal class CallStackItem
{
    // Pre-allocate with typical capacity
    public List<List<SymbolRef>> BlocksToClose { get; } = new(4);
    public HashSet<int> ToBeClosedIndices { get; } = new(8);
    
    public void Reset()
    {
        BlocksToClose.Clear();
        ToBeClosedIndices.Clear();
        // ... reset other fields ...
    }
}

// Option 2: Pool CallStackItem instances
internal static class CallStackItemPool
{
    [ThreadStatic]
    private static Stack<CallStackItem> _pool;
    
    public static CallStackItem Rent() => (_pool ??= new()).Count > 0 
        ? _pool.Pop() 
        : new CallStackItem();
    
    public static void Return(CallStackItem item)
    {
        item.Reset();
        (_pool ??= new()).Push(item);
    }
}
```

### 1.4 InternalAdjustTuple Optimization

**File:** `ProcessorUtilityFunctions.cs`

The recursive tuple adjustment allocates new arrays at each level.

**Proposed solution:**
Flatten in a single pass using a pooled list:

```csharp
private static DynValue[] InternalAdjustTuple(IList<DynValue> values)
{
    if (values == null || values.Count == 0)
        return Array.Empty<DynValue>();
    
    // Fast path: no tuple at end
    if (values[^1].Type != DataType.Tuple)
    {
        // Use pool for result
        DynValue[] result = DynValueArrayPool.Rent(values.Count);
        for (int i = 0; i < values.Count; i++)
            result[i] = values[i].ToScalar();
        return result;
    }
    
    // Count total elements first (single pass)
    int total = values.Count - 1;
    DynValue current = values[^1];
    while (current.Type == DataType.Tuple && current.Tuple.Length > 0)
    {
        total += current.Tuple.Length - 1;
        current = current.Tuple[^1];
        if (current.Type != DataType.Tuple) total++;
    }
    
    DynValue[] result = DynValueArrayPool.Rent(total);
    // ... flatten in single pass ...
    return result;
}
```

______________________________________________________________________

## 2. High (Lexer/Parser Performance)

### 2.1 StringBuilder Pooling in Lexer

**File:** `Lexer.cs` (lines 401, 483, 530, 565, 602, 680)

**Current pattern:**

```csharp
StringBuilder text = new(1024);  // ReadLongString
StringBuilder text = new(32);    // Various token readers
```

**Proposed solution:**
Add a StringBuilder pool:

```csharp
internal static class StringBuilderPool
{
    [ThreadStatic]
    private static StringBuilder _cachedInstance;
    
    public static StringBuilder Rent(int capacity = 256)
    {
        StringBuilder sb = _cachedInstance;
        if (sb != null && sb.Capacity >= capacity)
        {
            _cachedInstance = null;
            sb.Clear();
            return sb;
        }
        return new StringBuilder(capacity);
    }
    
    public static string GetStringAndReturn(StringBuilder sb)
    {
        string result = sb.ToString();
        if (sb.Capacity <= 4096)  // Don't cache oversized builders
        {
            _cachedInstance = sb;
        }
        return result;
    }
}
```

### 2.2 Span-Based Lexer Rewrite

**File:** `Lexer.cs`

The lexer currently operates character-by-character on a string. For large scripts, a span-based approach would reduce bounds checking overhead.

**Proposed incremental changes:**

1. Store `ReadOnlySpan<char>` instead of `string _code` (requires ref struct or careful lifetime management)
1. Use `span.Slice()` for substring operations instead of `string.Substring()`
1. Use `MemoryExtensions.IndexOf()` for pattern matching

```csharp
// Example: BOM removal without allocation
if (_code.Length > 0 && _code[0] == 0xFEFF)
{
    _codeSpan = _code.AsSpan(1);  // No allocation
}
```

### 2.3 Remove LINQ from BuildTimeScope

**File:** `BuildTimeScope.cs`

**Current pattern:**

```csharp
_frames.Last().PushBlock();  // Called frequently during parsing
```

**Proposed solution:**
Cache the last frame or use index access:

```csharp
// Cache current frame
private BuildTimeScopeFrame _currentFrame;

public void PushFunction(...)
{
    BuildTimeScopeFrame frame = new BuildTimeScopeFrame(hasVarArgs);
    _frames.Add(frame);
    _currentFrame = frame;  // Cache
}

public void PushBlock()
{
    _currentFrame.PushBlock();  // Direct access, no LINQ
}
```

______________________________________________________________________

## 3. Medium (Standard Library)

### 3.1 Tuple Return Optimization

**Files:**

- `CoreLib/CoroutineModule.cs` (line 116)
- `CoreLib/BasicModule.cs` (line 311)
- `CoreLib/Utf8Module.cs` (line 87)
- `CoreLib/IO/FileUserDataBase.cs` (line 141)

**Current pattern:**

```csharp
return DynValue.NewTuple(values.ToArray());
```

**Proposed solution:**
Add `DynValue.NewTuple(IList<DynValue>)` overload to avoid the ToArray():

```csharp
public static DynValue NewTuple(IList<DynValue> values)
{
    if (values.Count == 0)
        return new DynValue { _type = DataType.Tuple, _object = Array.Empty<DynValue>() };
    
    DynValue[] array = DynValueArrayPool.Rent(values.Count);
    for (int i = 0; i < values.Count; i++)
        array[i] = values[i];
    
    return new DynValue { _type = DataType.Tuple, _object = array };
}
```

### 3.2 StringModule.CharFunction Optimization

**File:** `CoreLib/StringModule.cs` (line 93)

**Current pattern:**

```csharp
StringBuilder sb = new(args.Count);
for (int i = 0; i < args.Count; i++)
{
    // ... process ...
    sb.Append((char)normalized);
}
return DynValue.NewString(sb.ToString());
```

**Proposed solution (for small counts):**

```csharp
if (args.Count <= 128)
{
    Span<char> buffer = stackalloc char[args.Count];
    for (int i = 0; i < args.Count; i++)
    {
        // ... process ...
        buffer[i] = (char)normalized;
    }
    return DynValue.NewString(new string(buffer));
}
// Fall back to StringBuilder for large counts
```

### 3.3 DynValue.ToPrintString/ToString LINQ Removal

**File:** `DataTypes/DynValue.cs` (lines 580, 614, 651, 653)

**Current pattern:**

```csharp
return string.Join("\t", Tuple.Select(t => t.ToPrintString()));
```

**Proposed solution:**

```csharp
case DataType.Tuple:
    if (Tuple.Length == 0)
        return string.Empty;
    if (Tuple.Length == 1)
        return Tuple[0].ToPrintString();
    
    StringBuilder sb = StringBuilderPool.Rent();
    sb.Append(Tuple[0].ToPrintString());
    for (int i = 1; i < Tuple.Length; i++)
    {
        sb.Append('\t');
        sb.Append(Tuple[i].ToPrintString());
    }
    return StringBuilderPool.GetStringAndReturn(sb);
```

______________________________________________________________________

## 4. Low (Compile-time / Debug Paths)

### 4.1 Expression List Allocations

**Files:**

- `Tree/Expressions/FunctionCallExpression.cs` (lines 49, 64, 73)
- `Tree/Statements/AssignmentStatement.cs` (line 56)

These allocate during parsing which happens once per script load. Lower priority but still worth pooling if script recompilation is frequent.

### 4.2 Coroutine.GetStackTrace LINQ

**File:** `DataTypes/Coroutine.cs` (line 350)

```csharp
return stack.Skip(skip).ToArray();
```

Only called during debugging - low priority.

______________________________________________________________________

## 5. External Libraries to Evaluate

### 5.1 ZString (Cysharp) - ✅ INTEGRATED

**NuGet:** `ZString` version 2.6.0
**Status:** Integrated in Phase 2.5 (2025-12-06)

**Implementation:**

- Added `ZStringBuilder` wrapper utilities in `DataStructs/ZStringBuilder.cs`
- Added `PooledResource<T>` struct for automatic pool return (following Unity Helpers pattern)
- Updated `DynValue.JoinTupleStrings` to use `Utf16ValueStringBuilder`
- Updated `DynValueArrayPool` with `Get(int, out T[])` method returning `PooledResource<T>`

**Usage patterns:**

```csharp
// Basic string building (non-nested)
using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder(notNested: true);
sb.Append("value: ");
sb.Append(42);
return sb.ToString();

// Nested/recursive contexts (uses ArrayPool)
using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder(notNested: false);
// Safe for recursive calls that may also use ZString

// Convenience formatting
string result = ZString.Format("(Function {0:X8})", entryPoint);
```

**Important notes:**

- `notNested: true` uses ThreadStatic buffer - fastest but cannot be nested
- `notNested: false` uses ArrayPool - slightly slower but safe for recursion
- Be careful with recursive `ToString()` calls that may nest ZString usage

**Not yet applied to:**

- Lexer (complex exception handling makes conversion difficult)
- StringModule (evaluate in Phase 3)
- JSON serialization paths

### 5.2 ZLINQ (Cysharp)

**NuGet:** `ZLinq`
**Use case:** Zero-allocation LINQ-like operations

```csharp
// Instead of: values.Select(x => x.ToScalar()).ToArray()
using ZLinq;
values.AsValueEnumerable()
      .Select(x => x.ToScalar())
      .ToArray();  // Uses ArrayPool internally
```

**Considerations:**

- Adds dependency
- Requires source generator
- Best for code that can't easily be rewritten to manual loops

### 5.3 Microsoft.Toolkit.HighPerformance

**NuGet:** `Microsoft.Toolkit.HighPerformance`
**Use case:** StringPool, ArrayPoolBufferWriter, MemoryOwner

```csharp
// String interning without GC pressure
using var owner = StringPool.Shared.GetOrAdd(span);
```

### 5.4 RecyclableMemoryStream (Microsoft)

**NuGet:** `Microsoft.IO.RecyclableMemoryStream`
**Use case:** Pooled MemoryStream for binary operations

Currently used in:

- `StringModule.Dump` (`new MemoryStream()`)
- Binary dump/load operations

______________________________________________________________________

## 6. Implementation Roadmap

### Phase 1: Low-Risk High-Impact (1-2 weeks) - ✅ COMPLETED

1. **Add DynValueArrayPool** ✅ - Simple drop-in for existing allocations
1. **Remove LINQ from DynValue.ToString/ToPrintString** ✅ - String operations are visible
1. **Cache `_frames.Last()` in BuildTimeScope** ✅ - Parser speedup
1. **Add StringBuilder pooling** ✅ - Lexer and serialization

### Phase 2: VM Loop Optimization (2-3 weeks) - ✅ COMPLETED

1. **Refactor ProcessorUtilityFunctions** to use pooled arrays ✅
1. **Add CallStackItem pooling** - Requires careful lifecycle management
1. **Optimize InternalAdjustTuple** - Single-pass flattening

### Phase 2.5: ZString Integration (Completed 2025-12-06) - ✅ COMPLETED

1. **Add ZString package** ✅ - Zero-allocation string formatting
1. **Add PooledResource<T>** ✅ - Unity Helpers-style automatic pool return
1. **Update DynValueArrayPool** ✅ - Use PooledResource pattern
1. **Update DynValue string operations** ✅ - Use ZString for tuple joining

### Phase 3: External Libraries (1-2 weeks)

1. **Expand ZString usage** to StringModule and serialization
1. **Benchmark before/after** with BenchmarkDotNet
1. **Document findings** in Performance.md

### Phase 4: Lexer/Parser Optimization (2-4 weeks)

1. **Incremental span-based lexer changes**
1. **Profile with large scripts** (10K+ lines)
1. **Measure compilation time improvements**

______________________________________________________________________

## Measurement Strategy

Before implementing any optimization:

1. **Establish baseline** using BenchmarkDotNet with `[MemoryDiagnoser]`
1. **Identify top allocators** using dotMemory or Visual Studio profiler
1. **Create regression tests** for the affected code paths
1. **Measure after** each change

Key benchmarks to create:

```csharp
[MemoryDiagnoser]
public class AllocationBenchmarks
{
    [Benchmark]
    public DynValue FunctionCallHotPath() { /* ... */ }
    
    [Benchmark]
    public string LexLargeScript() { /* ... */ }
    
    [Benchmark]
    public DynValue TableIteration() { /* ... */ }
}
```

______________________________________________________________________

## References

- [NovaSharp Object Pooling Study](object-pooling-study.md)
- [Unity ObjectPool (buffer pooling)](https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html)
- [ZString](https://github.com/Cysharp/ZString)
- [ZLINQ](https://github.com/Cysharp/ZLinq)
- [ArrayPool Best Practices](https://adamsitnik.com/Array-Pool/)

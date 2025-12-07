# NovaSharp Pooling and Allocation Audit - December 2025

> **Status**: Analysis Complete
> **Date**: 2025-12-07
> **Context**: Follow-up to Phase 2.5 ZString integration, targeting Unity Helpers-style pooling patterns

This audit examines the NovaSharp interpreter for opportunities to:

1. Use pooled collections (List, Dictionary, HashSet, etc.)
1. Replace intermediary structures with fixed-size arrays
1. Replace string concatenation with ZString
1. Add short-circuit checks before Trim/Replace operations

______________________________________________________________________

## Table of Contents

1. [Current State](#1-current-state)
1. [Pooled Collection Opportunities](#2-pooled-collection-opportunities)
1. [Fixed-Size Array Opportunities](#3-fixed-size-array-opportunities)
1. [Remaining String Concatenation](#4-remaining-string-concatenation)
1. [Trim/Replace Short-Circuit Opportunities](#5-trimreplace-short-circuit-opportunities)
1. [Implementation Priority](#6-implementation-priority)

______________________________________________________________________

## 1. Current State

### Already Implemented ✅

- `DynValueArrayPool` - Thread-local pooling for DynValue[] arrays (≤8 elements fast path)
- `ObjectArrayPool` - Thread-local pooling for object[] arrays (reflection invocation)
- `PooledResource<T>` - RAII pattern for automatic pool return
- `ZStringBuilder` - Wrapper utilities for ZString
- `DynValue.NewConcatenatedString()` - ZString-based string concatenation (2, 3, 4-arg variants)
- `DynValue.JoinTupleStrings()` - ZString-based tuple joining

### Missing Infrastructure

- **No generic `ListPool<T>`** - Lists are allocated frequently but never pooled
- **No `DictionaryPool<K,V>`** - Dictionaries allocated during parsing/interop
- **No `HashSetPool<T>`** - HashSets allocated in VM and debugging paths
- **No `StringBuilderPool`** - Despite docs mentioning it, not implemented

______________________________________________________________________

## 2. Pooled Collection Opportunities

### 2.1 High-Priority: Create Generic Collection Pools

Following the Unity Helpers `Buffers<T>` pattern, implement:

```csharp
// DataStructs/CollectionPools.cs
internal static class ListPool<T>
{
    private static readonly WallstopGenericPool<List<T>> Pool = new(
        () => new List<T>(),
        onRelease: list => list.Clear()
    );
    
    public static PooledResource<List<T>> Get(out List<T> list) => Pool.Get(out list);
    public static PooledResource<List<T>> Get(int capacity, out List<T> list)
    {
        PooledResource<List<T>> pooled = Pool.Get(out list);
        if (list.Capacity < capacity) list.Capacity = capacity;
        return pooled;
    }
}
```

### 2.2 Specific Allocation Sites to Convert

#### FileUserDataBase.cs (Lines 31-147)

```csharp
// CURRENT: new List<DynValue>() and rets.ToArray()
List<DynValue> readLines = new();
// ...
return DynValue.NewTuple(rets.ToArray());

// PROPOSED:
using (ListPool<DynValue>.Get(out List<DynValue> readLines))
{
    // ... populate readLines ...
    DynValue[] result = readLines.Count == 1 
        ? new[] { readLines[0] } 
        : readLines.ToArray();
    return DynValue.NewTuple(result);
}
```

#### DynValue.NewTupleNested (Lines 599-756)

```csharp
// CURRENT: new List<DynValue>(capacity) then vals.ToArray()
List<DynValue> vals = new(capacity);
// ...
return new DynValue() { _object = vals.ToArray(), _type = DataType.Tuple };

// PROPOSED: Use pooled list, copy to exact-size array
using (ListPool<DynValue>.Get(capacity, out List<DynValue> vals))
{
    // ... populate vals ...
    DynValue[] result = new DynValue[vals.Count];
    vals.CopyTo(result);
    return new DynValue() { _object = result, _type = DataType.Tuple };
}
```

#### ProcessorInstructionLoop.cs (Lines 557-1006)

```csharp
// CURRENT: Allocates lists on every call frame
stackframe.BlocksToClose = new List<List<SymbolRef>>();
stackframe.ToBeClosedIndices = new HashSet<int>();

// PROPOSED: Pool at CallStackItem level or use pre-allocated capacity
// Consider making these lazy with pooled backing
```

#### BuildTimeScopeBlock.cs (Lines 56, 173, 201)

```csharp
// CURRENT: new List<> on every scope block
ChildNodes = new List<BuildTimeScopeBlock>();
_localLabels = new Dictionary<string, LabelStatement>();
_pendingGotos = new List<GotoStatement>();

// PROPOSED: Pool these during parsing (single-threaded, predictable lifetime)
```

#### Coroutine/BasicModule/Utf8Module ToArray Calls

Multiple sites call `.ToArray()` on List<DynValue> for tuple creation. Create a pooled-to-exact-copy helper:

```csharp
internal static class CollectionHelpers
{
    public static DynValue[] ToExactArray(List<DynValue> list)
    {
        if (list.Count == 0) return Array.Empty<DynValue>();
        DynValue[] result = new DynValue[list.Count];
        list.CopyTo(result);
        return result;
    }
}
```

______________________________________________________________________

## 3. Fixed-Size Array Opportunities

### 3.1 ProcessorUtilityFunctions.cs - StackTopToArray

**Current**: Always allocates `new DynValue[items]`

**Analysis**: This is called on every function call/return. For small argument counts (≤8), use `DynValueArrayPool`.

```csharp
// PROPOSED:
private PooledResource<DynValue[]> StackTopToArrayPooled(int items, bool pop, out DynValue[] values)
{
    PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(items, out values);
    if (pop)
    {
        for (int i = 0; i < items; i++)
            values[i] = _valueStack.Pop();
    }
    else
    {
        for (int i = 0; i < items; i++)
            values[i] = _valueStack[_valueStack.Count - 1 - i];
    }
    return pooled;
}
```

### 3.2 DynValue.NewTuple Overloads

The 2 and 3-arg overloads currently allocate `new[] { value1, value2 }`:

```csharp
// CURRENT (Line 536):
return new DynValue() { _object = new[] { value1, value2 }, _type = DataType.Tuple };

// PROPOSED: Consider static readonly arrays for common patterns, or accept the allocation
// since tuple arrays are stored, not transient
```

**Verdict**: The tuple arrays are stored as the DynValue's object, so they MUST be newly allocated. No optimization possible without changing tuple semantics.

### 3.3 Extension Methods Registry

`ExtensionMethodsRegistry.cs` Line 182:

```csharp
return new List<IOverloadableMemberDescriptor>(state.Registry.Find(name));

// PROPOSED: Return IReadOnlyList or use pooled list with ToArray for immutable snapshot
```

______________________________________________________________________

## 4. Remaining String Concatenation

### 4.1 High-Priority Conversions

#### DynValue.ToString() - Line 943

```csharp
// CURRENT:
return "\"" + String + "\"";

// PROPOSED:
return ZString.Concat("\"", String, "\"");
```

#### DynValue.ToString() - Line 953

```csharp
// CURRENT:
return "Tail:(" + JoinTupleStrings(Tuple, ", ", v => v.ToString()) + ")";

// PROPOSED:
using (Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested())
{
    sb.Append("Tail:(");
    sb.Append(JoinTupleStrings(Tuple, ", ", v => v.ToString()));
    sb.Append(')');
    return sb.ToString();
}
// OR: Add ZString.Concat overload in ZStringBuilder for this pattern
```

#### SerializationExtensions.cs - Lines 91, 202

```csharp
// CURRENT:
: "[" + SerializeValue(tp.Key, tabs + 1) + "]";
return "\"" + s + "\"";

// PROPOSED:
: ZString.Concat("[", SerializeValue(tp.Key, tabs + 1), "]");
return ZString.Concat("\"", s, "\"");
```

#### FunctionDefinitionExpression.cs - Line 263

```csharp
// CURRENT:
string funcName = friendlyName ?? ("<" + _begin.FormatLocation(bc.Script, true) + ">");

// PROPOSED:
string funcName = friendlyName ?? ZString.Concat("<", _begin.FormatLocation(bc.Script, true), ">");
```

#### Script.cs - Lines 307, 357, 1170

```csharp
// Several string interpolation/concatenation sites
// Convert to ZString.Format() where appropriate
```

### 4.2 Low-Priority (Rare Paths)

- `Instruction.PurifyFromNewLines()` - Debug display only
- `ReplInterpreter` - REPL context, not hot path
- `Lexer.ReadLongString()` - Called during parsing, not execution
- Error message construction - Exception paths

______________________________________________________________________

## 5. Trim/Replace Short-Circuit Opportunities

### 5.1 FileUserDataBase.cs - Lines 66, 120, 127

```csharp
// CURRENT:
str = str.TrimEnd('\n', '\r');

// PROPOSED: Check if trim is needed first
if (str.Length > 0 && (str[^1] == '\n' || str[^1] == '\r'))
{
    str = str.TrimEnd('\n', '\r');
}
```

**Analysis**: `TrimEnd` already handles the no-op case efficiently by returning the same string instance when no trimming is needed. However, the method still does work to determine this. For hot paths, the pre-check may help.

### 5.2 ModManifest.cs - Line 156

```csharp
// CURRENT:
string candidate = raw.Trim();

// PROPOSED: Check if trim is needed
string candidate = raw;
if (raw.Length > 0 && (char.IsWhiteSpace(raw[0]) || char.IsWhiteSpace(raw[^1])))
{
    candidate = raw.Trim();
}
```

**Analysis**: `string.Trim()` returns the same instance if no trimming needed. The pre-check is only worthwhile if profiling shows this as a hotspot.

### 5.3 SerializationExtensions.cs - EscapeString

```csharp
// CURRENT: Chain of Replace calls
s = ReplaceOrdinal(s, @"\", @"\\");
s = ReplaceOrdinal(s, "\n", @"\n");
// ... 8 more Replace calls

// PROPOSED: Single-pass escape using ZString builder
private static string EscapeString(string input)
{
    if (string.IsNullOrEmpty(input))
        return "\"\"";
    
    // Fast path: check if any escaping needed
    bool needsEscape = false;
    foreach (char c in input)
    {
        if (c == '\\' || c == '\n' || c == '\r' || c == '\t' || 
            c == '\a' || c == '\f' || c == '\b' || c == '\v' ||
            c == '"' || c == '\'')
        {
            needsEscape = true;
            break;
        }
    }
    
    if (!needsEscape)
        return ZString.Concat("\"", input, "\"");
    
    // Slow path: build escaped string
    using Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested();
    sb.Append('"');
    foreach (char c in input)
    {
        switch (c)
        {
            case '\\': sb.Append(@"\\"); break;
            case '\n': sb.Append(@"\n"); break;
            case '\r': sb.Append(@"\r"); break;
            case '\t': sb.Append(@"\t"); break;
            case '\a': sb.Append(@"\a"); break;
            case '\f': sb.Append(@"\f"); break;
            case '\b': sb.Append(@"\b"); break;
            case '\v': sb.Append(@"\v"); break;
            case '"': sb.Append("\\\""); break;
            case '\'': sb.Append(@"\'"); break;
            default: sb.Append(c); break;
        }
    }
    sb.Append('"');
    return sb.ToString();
}
```

### 5.4 Instruction.PurifyFromNewLines - Line 147

```csharp
// CURRENT:
return value.ToString().Replace('\n', ' ').Replace('\r', ' ');

// PROPOSED: Check first, single-pass if needed
string str = value.ToString();
if (str.IndexOf('\n') < 0 && str.IndexOf('\r') < 0)
    return str;

using Utf16ValueStringBuilder sb = ZStringBuilder.CreateNested();
foreach (char c in str)
{
    sb.Append(c == '\n' || c == '\r' ? ' ' : c);
}
return sb.ToString();
```

### 5.5 Lexer.cs - Line 446

```csharp
// CURRENT:
endPattern = startpattern.Replace('[', ']');

// This is parsing, single character replacement - optimize with:
// StringBuilder or ZString if pattern is long, otherwise acceptable
```

______________________________________________________________________

## 6. Implementation Priority

### Phase 1: Low-Risk High-Value (Immediate)

1. **Add `ListPool<T>`** - Most common transient collection
1. **Convert DynValue.ToString() string concat to ZString**
1. **Add short-circuit checks to FileUserDataBase TrimEnd calls**

### Phase 2: Medium-Risk High-Value (1-2 weeks)

1. **Pool `StackTopToArray` results** - Hot VM path
1. **Optimize SerializationExtensions.EscapeString** - Single-pass escape
1. **Convert remaining string concat in DynValue, Script, Serialization**

### Phase 3: Architecture Changes (Evaluate ROI)

1. **Pool CallStackItem collections** - Requires lifecycle analysis
1. **Pool BuildTimeScopeBlock collections** - Parser path
1. **Consider `ValueListBuilder<T>` for stack-allocated sequences**

### Phase 4: Benchmarking Required

1. **Verify Trim/Replace short-circuits actually help** - May be micro-optimization
1. **Profile Lexer string operations** - Complex exception handling
1. **Measure pooling overhead vs allocation** - Ensure net positive

______________________________________________________________________

## Appendix: Reference Implementation

### WallstopGenericPool Pattern

Based on Unity Helpers `Buffers.cs`, the key patterns are:

1. **Thread-safe storage**: Use `ConcurrentStack<T>` for multi-threaded scenarios
1. **RAII disposal**: `PooledResource<T>` struct with `IDisposable`
1. **Callbacks**: `onGet`, `onRelease`, `onDispose` for lifecycle hooks
1. **Pre-warming**: Optional pre-allocation of pool instances
1. **Capacity management**: Track and limit pool size to prevent memory bloat

The existing `DynValueArrayPool` and `ObjectArrayPool` already follow these patterns. The proposal is to generalize them via a `WallstopGenericPool<T>` base.

______________________________________________________________________

## Notes for Implementation

1. **Thread Safety**: NovaSharp scripts typically run single-threaded per Script instance. Thread-local pools (`[ThreadStatic]`) are appropriate for most scenarios.

1. **Pool Size Limits**: Avoid unbounded pool growth. The current `DynValueArrayPool` caps at 1024 elements for large array pooling.

1. **Clearing on Return**: For collections containing DynValue or object references, ALWAYS clear on return to avoid memory leaks.

1. **ZString Nesting**: Use `CreateNested()` / `notNested: false` when the code path may recursively use ZString (e.g., ToString() calling other ToString()).

1. **Benchmark Before/After**: Create BenchmarkDotNet tests with `[MemoryDiagnoser]` to validate improvements.

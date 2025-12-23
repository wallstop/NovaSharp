# High-Performance C# Coding Guidelines

When writing new code for NovaSharp, prioritize **minimal allocations** and **maximum efficiency**. This interpreter runs hot paths millions of times—every allocation and boxing operation has measurable impact.

**Related Skills**: [zstring-migration](zstring-migration.md) (detailed ZString patterns), [span-optimization](span-optimization.md) (detailed Span patterns)

______________________________________________________________________

## 🔴 CRITICAL: Apply These Patterns to ALL New Code

These guidelines apply to **ALL new code**, not just identified "hot paths". When writing a new class or method:

1. **Default to zero-allocation patterns** — Use pooled buffers, spans, and stackalloc by default
1. **Use `List<T>` only when necessary** — Prefer pooled arrays for accumulation
1. **Never allocate inside loops** — Move allocations outside loops or use pooling
1. **Test with a memory profiler** — Validate your assumptions about allocations

### Common Mistakes in New Code

```csharp
// ❌ BAD: List<byte> allocates backing array AND grows (allocates more)
List<byte> result = new List<byte>(64);
for (...) { result.Add(b); }  // May reallocate multiple times!
return BytesToString(result); // Allocates char[] inside

// ✅ GOOD: Pooled buffer with known/estimated capacity
using PooledResource<byte[]> pooled = SystemArrayPool<byte>.Get(estimatedSize, clearOnReturn: false, out byte[] buffer);
int written = 0;
for (...) { buffer[written++] = b; }
// If buffer too small, rent larger and copy
return CreateStringFromBytes(buffer, written);  // Use stackalloc for char[] if small

// ❌ BAD: Allocates byte[] inside method called many times
private static double ReadDouble(string data, ref int pos)
{
    byte[] bytes = new byte[8];  // ALLOCATION!
    // ...
}

// ✅ GOOD: Use stackalloc for small fixed-size buffers
private static double ReadDouble(ReadOnlySpan<char> data, ref int pos)
{
    Span<byte> bytes = stackalloc byte[8];
    // ...
}

// ❌ BAD: Allocates new List<T> every call
List<DynValue> results = new List<DynValue>();
// ... add items ...
return DynValue.NewTuple(results.ToArray());  // ToArray() allocates AGAIN!

// ✅ GOOD: Pool the DynValue array directly
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(maxItems, out DynValue[] results);
int count = 0;
// ... populate results[count++] ...
DynValue[] final = new DynValue[count];
Array.Copy(results, final, count);
return DynValue.NewTuple(final);
```

### Module Implementation Pattern

When implementing new Lua modules (like `string.pack`), follow this pattern:

```csharp
[NovaSharpModule(Namespace = "string")]
internal static class ExampleModule
{
    // Thread-static buffers for fixed-size temporary data
    [ThreadStatic]
    private static byte[] t_tempBuffer;
    
    private static byte[] GetTempBuffer(int minSize)
    {
        byte[] buffer = t_tempBuffer;
        if (buffer == null || buffer.Length < minSize)
        {
            buffer = new byte[Math.Max(minSize, 256)];
            t_tempBuffer = buffer;
        }
        return buffer;
    }
    
    // For variable-size output, estimate and use pooling
    [NovaSharpModuleMethod(Name = "example")]
    public static DynValue Example(ScriptExecutionContext ctx, CallbackArguments args)
    {
        // Estimate output size based on input
        int estimatedSize = CalculateEstimate(args);
        
        // Use pooled buffer
        using PooledResource<byte[]> pooled = SystemArrayPool<byte>.Get(
            estimatedSize, clearOnReturn: false, out byte[] buffer);
        
        int written = ProcessData(args, buffer);
        
        // Handle buffer overflow: rent larger buffer
        if (written < 0)
        {
            // written is negative, indicating needed size
            int needed = -written;
            using PooledResource<byte[]> largerPooled = SystemArrayPool<byte>.Get(
                needed, clearOnReturn: false, out byte[] largerBuffer);
            written = ProcessData(args, largerBuffer);
            return CreateResult(largerBuffer, written);
        }
        
        return CreateResult(buffer, written);
    }
}
```

______________________________________________________________________

## 🔴 Unity Compatibility Requirements

NovaSharp targets **Unity3D (IL2CPP/AOT), Mono, and Xamarin** in addition to .NET. This imposes strict API constraints:

### ❌ APIs NOT AVAILABLE in Unity (DO NOT USE)

| API                                            | Reason                     | Alternative                                 |
| ---------------------------------------------- | -------------------------- | ------------------------------------------- |
| `CollectionsMarshal.AsSpan<T>(List<T>)`        | .NET 5+ only, not in Unity | Use `list.ToArray()` or manual array access |
| `CollectionsMarshal.GetValueRefOrAddDefault()` | .NET 6+ only               | Use standard dictionary operations          |
| `List<T>.EnsureCapacity()`                     | .NET Core 2.1+ only        | Use constructor with capacity               |
| `Span<T>` fields in non-ref structs            | Requires runtime support   | Use `ref struct` or arrays                  |
| `[SkipLocalsInit]`                             | Requires runtime support   | Unavailable                                 |
| `half` (Half-precision float)                  | .NET 5+ only               | Use `float`                                 |
| `nint` / `nuint`                               | .NET 5+ only               | Use `IntPtr` / `UIntPtr`                    |
| Generic math interfaces (`INumber<T>`)         | .NET 7+ only               | Use explicit type overloads                 |

### ✅ Unity-Compatible Performance Patterns

```csharp
// ✅ Get raw array access for List<T> in Unity
// Option 1: Pre-size and use array directly
List<T> list = new List<T>(capacity);
// ... populate ...
T[] underlying = list.ToArray();  // One allocation, then work with array

// Option 2: Use array from the start if size is known
T[] array = ArrayPool<T>.Shared.Rent(capacity);

// Option 3: For fixed-size pools, use NovaSharp's pooling
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(size, out DynValue[] array);
```

______________________________________________________________________

## 🔴 Core Principles

1. **Prefer value types (structs) over reference types (classes)** when data is small and short-lived
1. **Avoid allocations in hot paths** — use pooling, stack allocation, or spans
1. **Use `readonly struct` for immutable value types** — enables compiler optimizations
1. **Prefer `ref struct` for stack-only types** — enables `Span<T>` fields and prevents heap escape
1. **NEVER capture variables in closures/lambdas in hot paths** — closures allocate; use static lambdas or pass state explicitly
1. **NEVER use LINQ in hot paths** — causes allocations (enumerators, delegates); use explicit loops
1. **Always measure** — use BenchmarkDotNet before/after for performance-critical changes
1. **Always verify Unity compatibility** — check APIs against the forbidden list above

______________________________________________________________________

## String Building

See [zstring-migration.md](zstring-migration.md) for detailed patterns. Quick reference:

```csharp
// Safe for nested/recursive calls (uses ArrayPool)
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
return sb.ToString();

// Simple 2-3 element concatenation
return ZString.Concat("\"", input, "\"");
```

**NEVER** use `StringBuilder`, `$"..."` interpolation, or `+` concatenation in hot paths.

______________________________________________________________________

## Closures and Lambdas

### ❌ DON'T: Capture variables in closures (allocates)

```csharp
// BAD: Captures 'threshold' - allocates closure object
int threshold = 10;
List<int> filtered = items.Where(x => x > threshold).ToList();

// BAD: Captures 'prefix' - allocates closure
string prefix = "Error: ";
Action<string> log = msg => Console.WriteLine(prefix + msg);
```

### ✅ DO: Use static lambdas or pass state explicitly

```csharp
// ✅ Good: Static lambda - no capture, no allocation
items.Where(static x => x > 0);

// ✅ Good: Pass state explicitly via overload
items.Where((x, state) => x > state.Threshold, new { Threshold = 10 });

// ✅ Good: Use explicit loop instead of LINQ
List<int> filtered = new List<int>();
foreach (int item in items)
{
    if (item > threshold)
        filtered.Add(item);
}

// ✅ Good: Pre-allocated delegate for repeated use
private static readonly Func<int, bool> IsPositive = static x => x > 0;
```

### LINQ Allocation Traps

```csharp
// BAD: LINQ allocates enumerator + delegate
int sum = items.Where(x => x > 0).Sum();

// GOOD: Explicit loop - zero allocations
int sum = 0;
foreach (int item in items)
{
    if (item > 0)
        sum += item;
}
```

______________________________________________________________________

## Value Types vs Reference Types

### When to use `readonly struct`

Use for **small, immutable data** that will be passed by value:

```csharp
// ✅ Good: Small, immutable, frequently created
// NOTE: Project code analysis requires IEquatable<T> and ==, != operators for structs
internal readonly struct SourceLocation : IEquatable<SourceLocation>
{
    public int Line { get; }
    public int Column { get; }
    public int CharIndex { get; }
    
    public SourceLocation(int line, int column, int charIndex)
    {
        Line = line;
        Column = column;
        CharIndex = charIndex;
    }
    
    public bool Equals(SourceLocation other) => 
        Line == other.Line && Column == other.Column && CharIndex == other.CharIndex;
    public override bool Equals(object obj) => obj is SourceLocation other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Line, Column, CharIndex);
    public static bool operator ==(SourceLocation left, SourceLocation right) => left.Equals(right);
    public static bool operator !=(SourceLocation left, SourceLocation right) => !left.Equals(right);
}

// ✅ Good: Result type that carries success + value
internal readonly struct ParseResult<T> : IEquatable<ParseResult<T>>
{
    public bool Success { get; }
    public T Value { get; }
    public string Error { get; }
    
    public static ParseResult<T> Ok(T value) => new(true, value, null);
    public static ParseResult<T> Fail(string error) => new(false, default, error);
    
    // ... equality members required by code analysis
}
```

### When to use `ref struct`

Use for **stack-only types** that contain spans or need to prevent heap escape:

```csharp
// ✅ Good: Contains Span, must stay on stack
internal ref struct CharSpan
{
    private readonly ReadOnlySpan<char> _span;
    private int _position;
    
    public CharSpan(ReadOnlySpan<char> span)
    {
        _span = span;
        _position = 0;
    }
    
    public char Current => _span[_position];
    public bool MoveNext() => ++_position < _span.Length;
}
```

### When to use `class`

Use for **long-lived objects**, **inheritance needs**, or **reference semantics**:

```csharp
// Class appropriate: Long-lived, complex state, needs identity
internal sealed class Script { /* ... */ }
internal sealed class Table { /* ... */ }
```

### Size Guidelines

| Size        | Recommendation                                  |
| ----------- | ----------------------------------------------- |
| ≤16 bytes   | `readonly struct` preferred                     |
| 17-64 bytes | `readonly struct` OK if passed by `in` or `ref` |
| >64 bytes   | Consider `class` or pass by `ref`               |

______________________________________________________________________

## 🔴 CRITICAL: Pool Usage Pattern

**ALWAYS use `using` with `Get()` instead of manual `Rent()`/`Return()` calls.**

Manual rent/return is error-prone and leaks resources on exceptions:

```csharp
// ❌ BAD: Manual rent/return — leaks on exception!
List<Instruction> jumps = ListPool<Instruction>.Rent();
DoSomethingThatMightThrow();  // If this throws, jumps is never returned!
ListPool<Instruction>.Return(jumps);

// ✅ GOOD: RAII pattern — automatic cleanup even on exception
using (ListPool<Instruction>.Get(out List<Instruction> jumps))
{
    DoSomethingThatMightThrow();  // jumps is returned even if this throws
}

// ✅ GOOD: using declaration (C# 8+) for cleaner code
using PooledResource<List<int>> pooled = ListPool<int>.Get(out List<int> items);
// items is automatically returned when pooled goes out of scope
```

### When `using` Isn't Possible

Some scenarios require manual lifetime management (e.g., resources stored in fields that outlive a single method). In these cases:

1. **Make the containing type `IDisposable`** and return pooled resources in `Dispose()`
1. **Use `PooledResource<T>` as a field** instead of the raw resource type
1. **Document clearly** that the caller is responsible for cleanup

```csharp
// ✅ GOOD: Type owns pooled resource via IDisposable
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

// Usage: the containing type is used with `using`
using (Loop l = new() { Scope = stackFrame })
{
    // l.BreakJumps is automatically returned when l is disposed
}
```

### Pool Types and Their Disposable APIs

All NovaSharp pools provide `Get()` methods that return `PooledResource<T>`:

| Pool                  | Get Method                                          | Resource Type     |
| --------------------- | --------------------------------------------------- | ----------------- |
| `ListPool<T>`         | `ListPool<T>.Get(out List<T> list)`                 | `List<T>`         |
| `HashSetPool<T>`      | `HashSetPool<T>.Get(out HashSet<T> set)`            | `HashSet<T>`      |
| `DictionaryPool<K,V>` | `DictionaryPool<K,V>.Get(out Dictionary<K,V> d)`    | `Dictionary<K,V>` |
| `DynValueArrayPool`   | `DynValueArrayPool.Get(int size, out DynValue[] a)` | `DynValue[]`      |
| `ObjectArrayPool`     | `ObjectArrayPool.Get(int size, out object[] a)`     | `object[]`        |
| `SystemArrayPool<T>`  | `SystemArrayPool<T>.Get(int size, out T[] a)`       | `T[]`             |
| `CallStackItemPool`   | `CallStackItemPool.Get(out CallStackItem item)`     | `CallStackItem`   |

### PooledResource\<T> API

```csharp
internal struct PooledResource<T> : IDisposable
{
    public T Resource { get; }         // The pooled resource
    public void SuppressReturn();      // Prevent return to pool (ownership transfer)
    public void Dispose();             // Return resource to pool (unless suppressed)
}
```

### Ownership Transfer Pattern

When returning a pooled resource to a caller who takes ownership:

```csharp
// Method that creates a result the caller will own
public PooledResource<List<int>> GetResults()
{
    using PooledResource<List<int>> pooled = ListPool<int>.Get(out List<int> list);
    list.Add(1);
    list.Add(2);
    
    // Transfer ownership to caller — don't return to pool here
    pooled.SuppressReturn();
    return pooled;
}

// Caller is responsible for disposal
using (PooledResource<List<int>> results = GetResults())
{
    foreach (int item in results.Resource)
    {
        Console.WriteLine(item);
    }
} // Returned to pool here
```

______________________________________________________________________

## Array and Buffer Pooling

NovaSharp has TWO array pooling strategies with different use cases:

### NovaSharp's Fixed-Size Pools (`DynValueArrayPool`, `ObjectArrayPool`)

Use these when you need **exact-size arrays** (e.g., reflection `MethodInfo.Invoke` requires exact parameter count):

```csharp
// ✅ GOOD: Always use Get() with using
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(5, out DynValue[] array);
// array.Length == 5 (exact)

using PooledResource<object[]> pooled = ObjectArrayPool.Get(3, out object[] array);
// array.Length == 3 (exact, required for reflection)
```

### `System.Buffers.ArrayPool<T>` (Variable-Size Buffers)

Use when you can work with **"at least" the requested size** and track actual usage:

```csharp
using System.Buffers;

// ✅ GOOD: Use SystemArrayPool wrapper for RAII pattern
using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(256, out char[] buffer);
// buffer.Length >= 256 (may be larger!)
int written = FormatValue(value, buffer);
return new string(buffer, 0, written);
// Automatically returned to pool when disposed
```

### When to Use Which

| Scenario                                     | Pool to Use           | Why                            |
| -------------------------------------------- | --------------------- | ------------------------------ |
| Reflection invoke (`MethodInfo.Invoke`)      | `ObjectArrayPool`     | Requires exact parameter count |
| VM call frames (fixed arity)                 | `DynValueArrayPool`   | Known compile-time sizes       |
| String pattern matching buffers              | `ArrayPool<T>.Shared` | Variable sizes, smarter reuse  |
| Parsing/formatting temporary buffers         | `ArrayPool<T>.Shared` | Size varies by input           |
| Table operations with dynamic element counts | `SystemArrayPool<T>`  | Unknown count until runtime    |

### `SystemArrayPool<T>` — Variable-Size Array Pooling

Wraps `ArrayPool<T>.Shared` with `PooledResource<T>` disposal semantics:

```csharp
// RAII pattern (recommended) — automatic cleanup
using PooledResource<char[]> pooled = SystemArrayPool<char>.Get(256, out char[] buffer);
// buffer.Length >= 256 (may be larger!)
int written = FormatValue(value, buffer);
return new string(buffer, 0, written);
// Automatically returned to pool when disposed

// With clearing control (disable for performance when buffer will be overwritten)
using PooledResource<int[]> pooled = SystemArrayPool<int>.Get(1000, clearOnReturn: false, out int[] array);

// Manual lifecycle (when disposal timing is complex)
char[] buffer = SystemArrayPool<char>.Rent(256);
try { /* ... */ }
finally { SystemArrayPool<char>.Return(buffer); }

// Extract exact-size copy and return pooled array
char[] buffer = SystemArrayPool<char>.Rent(1024);
int actualLength = Fill(buffer);
return SystemArrayPool<char>.ToArrayAndReturn(buffer, actualLength);
```

### ✅ DO: Use stackalloc for small, fixed-size buffers

```csharp
// Stack allocation for small buffers (< 1KB recommended)
Span<char> buffer = stackalloc char[64];
int written = value.TryFormat(buffer, out int charsWritten) ? charsWritten : 0;
return new string(buffer.Slice(0, written));

// With fallback for potentially larger sizes
Span<char> buffer = length <= 256 
    ? stackalloc char[256] 
    : new char[length]; // Fall back to heap for large
```

### ❌ DON'T: Allocate arrays in hot paths

```csharp
// BAD: Allocates new array every call
char[] buffer = new char[256];

// BAD: ToArray() allocates
return list.ToArray();
```

______________________________________________________________________

## Thread-Local Caching (`[ThreadStatic]`)

For **fixed-size objects or arrays** that are frequently allocated and discarded in hot paths, use `[ThreadStatic]` caching to allocate once per thread and reuse indefinitely.

### When to Use Thread-Local Caching

| Scenario                                            | Good Candidate? | Why                                              |
| --------------------------------------------------- | --------------- | ------------------------------------------------ |
| Fixed-size buffers (known at compile time)          | ✅ Yes          | Same size every call, reuse is trivial           |
| Complex state objects with `Reset()` method         | ✅ Yes          | Single allocation per thread, reset between uses |
| Variable-size buffers                               | ❌ No           | Use `ArrayPool<T>.Shared` instead                |
| Objects with reference semantics (identity matters) | ❌ No           | Reuse would cause aliasing bugs                  |
| Short-lived call-stack-only data                    | ❌ No           | Use `stackalloc` instead                         |

### ✅ DO: Cache fixed-size arrays with `[ThreadStatic]`

```csharp
// Thread-local cached buffers — allocated once per thread, reused forever
[ThreadStatic]
private static char[] t_formatFormBuffer;  // MaxFormat size (constant)

[ThreadStatic]
private static char[] t_formatBuffBuffer;  // MAX_ITEM size (constant)

private static void GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer)
{
    // Null-coalescing assignment: allocate on first use per thread
    formBuffer = t_formatFormBuffer ??= new char[MaxFormat];
    buffBuffer = t_formatBuffBuffer ??= new char[MAX_ITEM];
}

// Usage in hot path:
GetFormatBuffers(out char[] formBuffer, out char[] buffBuffer);
// Use formBuffer and buffBuffer...
// No cleanup needed — buffers persist for thread lifetime
```

### ✅ DO: Pool complex objects with state reset

```csharp
// Thread-local pooled object with Reset() for reuse
[ThreadStatic]
private static MatchState t_cachedMatchState;

private static MatchState RentMatchState()
{
    MatchState ms = t_cachedMatchState;
    if (ms != null)
    {
        t_cachedMatchState = null!;  // Take ownership
        ms.Reset();                   // Clear state for reuse
        return ms;
    }
    return new MatchState();  // First call on this thread
}

private static void ReturnMatchState(MatchState ms)
{
    ms.Reset();  // Clear references to allow GC of captured data
    t_cachedMatchState = ms;  // Return to cache
}

// Usage pattern:
MatchState ms = RentMatchState();
try
{
    // Use ms...
}
finally
{
    ReturnMatchState(ms);
}
```

### ❌ DON'T: Allocate per-call when size is constant

```csharp
// BAD: Allocates 540 bytes every call
public static int Format(string fmt, ...)
{
    char[] form = new char[25];   // ← Constant size!
    char[] buff = new char[512];  // ← Constant size!
    // ...
}
```

### Thread-Local vs Object Pooling

| Pattern                          | Use When                                                | Example                      |
| -------------------------------- | ------------------------------------------------------- | ---------------------------- |
| `[ThreadStatic]` caching         | Single instance needed per thread, no contention        | `MatchState`, format buffers |
| `ObjectPool<T>` / `ArrayPool<T>` | Variable sizes, or multiple concurrent instances needed | `ArrayPool<char>.Shared`     |
| `stackalloc`                     | Small, known size, call-stack lifetime only             | `stackalloc char[64]`        |

______________________________________________________________________

## Class-to-Struct Conversion

Converting frequently-allocated short-lived classes to `readonly struct` is one of the **highest-impact optimizations** for reducing GC pressure.

### Impact Example: KopiLua `CharPtr`

Converting `CharPtr` from a class to `readonly struct` achieved:

- **58-85% allocation reduction** across pattern matching scenarios
- **24-63% latency improvement** due to reduced GC pressure
- Eliminated ~50+ heap allocations per pattern match operation

### When to Convert Class → Struct

| Criterion                                               | Good Candidate |
| ------------------------------------------------------- | -------------- |
| Small size (≤64 bytes)                                  | ✅             |
| Short-lived (created and discarded in same method/loop) | ✅             |
| Immutable or has clear copy semantics                   | ✅             |
| Created frequently in hot paths                         | ✅             |
| Needs reference identity (same instance check)          | ❌             |
| Inherited from or inherits other types                  | ❌             |
| Contains large arrays or many reference fields          | ❌             |

### ✅ DO: Convert pointer-like/slice types to struct

```csharp
// BEFORE: Class — every pointer arithmetic allocates
public class CharPtr
{
    public char[] chars;
    public int index;
    
    public CharPtr Next() => new CharPtr(chars, index + 1);  // Allocation!
}

// AFTER: Struct — pointer arithmetic is free (stack copy)
public readonly struct CharPtr : IEquatable<CharPtr>
{
    public readonly char[] chars;
    public readonly int index;
    
    public CharPtr Next() => new CharPtr(chars, index + 1);  // No allocation!
    
    // Required: IEquatable<T>, Equals, GetHashCode, ==, !=
}
```

### ✅ DO: Convert inner state types to struct

```csharp
// BEFORE: Class — 32 allocations per MatchState
public class Capture
{
    public CharPtr Init;
    public int Len;
}
public class MatchState
{
    public Capture[] capture = new Capture[32];  // 32 class instances!
}

// AFTER: Struct — single contiguous array, no inner allocations
public struct Capture
{
    public CharPtr Init;
    public int Len;
}
public class MatchState
{
    public Capture[] capture = new Capture[32];  // 32 structs inline in array
}
```

### Conversion Checklist

When converting a class to struct:

- [ ] Add `readonly` modifier if all fields can be readonly
- [ ] Implement `IEquatable<T>` (required by code analysis)
- [ ] Implement `Equals(object)` and `GetHashCode()` using `HashCodeHelper`
- [ ] Implement `==` and `!=` operators (required by CA1815)
- [ ] Replace `null` checks with a static `Null` or `Empty` sentinel
- [ ] Update all code that checks `== null` to use the sentinel
- [ ] Consider adding `IsNull` or `IsValid` property for clarity
- [ ] Run full test suite — struct semantics differ from class semantics

______________________________________________________________________

## Pre-Computed Lookup Tables

For operations that repeatedly compute the same values (especially string formatting), use **pre-computed arrays** instead of runtime computation.

### ✅ DO: Pre-compute escape sequences

```csharp
// Pre-computed at class initialization — zero per-character allocation
private static readonly string[] EscapeSequences3Digit = new string[16]
{
    "\\000", "\\001", "\\002", "\\003", "\\004", "\\005", "\\006", "\\007",
    "\\008", "\\009", "\\010", "\\011", "\\012", "\\013", "\\014", "\\015",
};

// Usage: array lookup instead of string interpolation
string escaped = EscapeSequences3Digit[charValue];
```

### ❌ DON'T: Use string interpolation in hot loops

```csharp
// BAD: String interpolation allocates per character
for (int i = 0; i < length; i++)
{
    char c = input[i];
    if (NeedsEscape(c))
        output.Append($"\\{(int)c:D3}");  // Allocation every iteration!
}
```

### When to Pre-Compute

| Pattern                                            | Pre-compute?                    |
| -------------------------------------------------- | ------------------------------- |
| Fixed set of escape sequences                      | ✅ Yes                          |
| Character class membership (`IsDigit`, `IsLetter`) | ✅ Yes (256-element bool array) |
| Small enum-to-string mappings                      | ✅ Yes                          |
| Dynamic values based on runtime input              | ❌ No                           |

______________________________________________________________________

## Enum String Caching

### 🔴 NEVER call `.ToString()` on enums in hot paths

Enum `.ToString()` allocates a new string every call. Use cached string lookups instead.

### ❌ DON'T: Call enum.ToString()

```csharp
// BAD: Allocates every call
sb.Append(tokenType.ToString());
sb.Append(opCode.ToString().ToUpperInvariant());  // Double allocation!
```

### ✅ DO: Use cached string lookups

NovaSharp provides dedicated string cache classes for common enums:

```csharp
using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;

// ✅ Good: Zero allocation
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));  // Pre-cached uppercase
sb.Append(SymbolRefTypeStrings.GetName(symbolType));
sb.Append(ModLoadStateStrings.GetName(state));
sb.Append(DebuggerActionTypeStrings.GetName(action));

// ✅ Good: DataType has an existing extension method
sb.Append(dataType.ToLuaDebuggerString());
```

### Available String Caches

| Enum                        | Cache Class                 | Methods                       |
| --------------------------- | --------------------------- | ----------------------------- |
| `TokenType`                 | `TokenTypeStrings`          | `GetName()`                   |
| `OpCode`                    | `OpCodeStrings`             | `GetName()`, `GetUpperName()` |
| `SymbolRefType`             | `SymbolRefTypeStrings`      | `GetName()`                   |
| `ModLoadState`              | `ModLoadStateStrings`       | `GetName()`                   |
| `DebuggerAction.ActionType` | `DebuggerActionTypeStrings` | `GetName()`                   |
| `DataType`                  | `LuaTypeExtensions`         | `ToLuaDebuggerString()`       |

### Generic Enum Cache (for other enums)

For enums not in the above list, use the generic cache:

```csharp
using WallstopStudios.NovaSharp.Interpreter.DataStructs;

// Generic cache with automatic contiguous optimization
string name = EnumStringCache<MyEnum>.GetName(value);

// Lowercase variant (Lua-style output)
string lower = EnumStringCache<MyEnum>.GetNameLowerInvariant(value);
```

### Creating New Enum String Caches

When adding a new enum that will be converted to strings frequently, create a dedicated cache:

```csharp
// For contiguous enums (values 0, 1, 2, ... N), use static array
internal static class MyEnumStrings
{
    private static readonly string[] Names = { "Value0", "Value1", "Value2" };
    
    public static string GetName(MyEnum value)
    {
        int index = (int)value;
        return index >= 0 && index < Names.Length ? Names[index] : value.ToString();
    }
}
```

______________________________________________________________________

## Sorting Lists and Arrays

### ❌ DON'T: Use List<T>.Sort with struct comparers (boxes)

```csharp
// BAD: Boxes DynValueComparer on every Sort call
readonly struct DynValueComparer : IComparer<DynValue> { /* ... */ }
list.Sort(new DynValueComparer(script));

// BAD: Array.Sort also boxes struct comparers
Array.Sort(array, new DynValueComparer(script));
```

### ✅ DO: Use IListSortExtensions with generic constraints

```csharp
using WallstopStudios.NovaSharp.Interpreter.DataStructs;

// ✅ Good: Zero-allocation with struct comparer
readonly struct DynValueComparer : IComparer<DynValue> { /* ... */ }
list.Sort(new DynValueComparer(script));  // Extension method, no boxing!

// ✅ Good: Sort a range
list.Sort(startIndex, count, new DynValueComparer(script));

// ✅ Good: Works with arrays too (via IList<T>)
((IList<DynValue>)array).Sort(new DynValueComparer(script));
```

### Algorithm Details

`IListSortExtensions.Sort<T, TComparer>()` uses **pattern-defeating quicksort (pdqsort)**:

- O(n log n) average and worst case
- Insertion sort for small arrays (≤24 elements)
- Heapsort fallback when bad pivot sequences detected
- Handles adversarial patterns (sorted, reverse, repeated elements)
- Zero allocations with struct comparers

### When to Use

| Scenario                           | Use                                               |
| ---------------------------------- | ------------------------------------------------- |
| Sorting with struct comparer       | `IListSortExtensions.Sort`                        |
| Sorting with class comparer        | `List<T>.Sort` (no boxing benefit)                |
| Sorting primitives (default order) | `List<T>.Sort()` (no comparer)                    |
| Performance-critical sorting       | `IListSortExtensions.Sort` (pdqsort is excellent) |

______________________________________________________________________

## Span and Memory Usage

### ✅ DO: Use ReadOnlySpan<char> instead of string slicing

```csharp
// ✅ Good: No allocation
ReadOnlySpan<char> slice = source.AsSpan(start, length);

// ❌ Bad: Allocates new string
string slice = source.Substring(start, length);
```

### ✅ DO: Accept Span/ReadOnlySpan in APIs when possible

```csharp
// ✅ Good: Accepts span, avoids substring allocation by caller
internal bool TryParse(ReadOnlySpan<char> input, out int result)

// Also provide string overload for convenience
internal bool TryParse(string input, out int result) 
    => TryParse(input.AsSpan(), out result);
```

______________________________________________________________________

## Avoiding Boxing

### ✅ DO: Use generic constraints to avoid boxing

```csharp
// ✅ Good: No boxing
internal void Process<T>(T value) where T : struct, IFormattable
{
    string formatted = value.ToString(null, CultureInfo.InvariantCulture);
}

// ❌ Bad: Boxes the struct
internal void Process(IFormattable value)
{
    string formatted = value.ToString(null, CultureInfo.InvariantCulture);
}
```

### ✅ DO: Use pattern-based disposal instead of IDisposable for structs

```csharp
// ✅ Good: No boxing, `using` still works via duck-typing
internal readonly struct PooledBuffer
{
    private readonly char[] _buffer;
    
    public void Dispose()
    {
        ArrayPool<char>.Shared.Return(_buffer);
    }
}

// Usage: using var buffer = GetPooledBuffer();
```

______________________________________________________________________

## Hash Code Implementation

**ALWAYS use `HashCodeHelper`** for `GetHashCode()` implementations. Never use bespoke `hash * 31 + value` patterns, `HashCode.Combine()`, or `System.HashCode`.

### Why HashCodeHelper?

1. **Deterministic**: Unlike `System.HashCode`, results are stable across process boundaries and .NET versions
1. **Efficient**: Uses FNV-1a algorithm with aggressive inlining and cached type traits
1. **Zero-allocation**: No boxing for value types; uses `EqualityComparer<T>.Default` caching
1. **Centralized**: Single source of truth for hash code composition

### ✅ DO: Use HashCodeHelper for GetHashCode

```csharp
// ✅ Good: Simple multi-field hash (up to 8 parameters)
public override int GetHashCode()
{
    return HashCodeHelper.HashCode(_field1, _field2, _field3);
}

// ✅ Good: Use DeterministicHashBuilder for complex/conditional hashing
public override int GetHashCode()
{
    DeterministicHashBuilder hash = default;
    hash.AddInt((int)Type);
    
    if (HasValue)
    {
        hash.Add(Value);
    }
    
    return hash.ToHashCode();
}

// ✅ Good: Optimized primitive methods avoid boxing
DeterministicHashBuilder hash = default;
hash.AddInt(intValue);      // No boxing
hash.AddLong(longValue);    // No boxing
hash.AddDouble(doubleValue); // No boxing
return hash.ToHashCode();

// ✅ Good: Span-based hashing for collections
public override int GetHashCode()
{
    return HashCodeHelper.SpanHashCode(items.AsSpan());
}

// ✅ Good: Enumerable hashing when span not available
public override int GetHashCode()
{
    return HashCodeHelper.EnumerableHashCode(items);
}
```

### ❌ DON'T: Use bespoke hash algorithms

```csharp
// BAD: Manual hash computation (inconsistent, error-prone)
public override int GetHashCode()
{
    unchecked
    {
        int hash = 17;
        hash = hash * 31 + _field1;
        hash = hash * 31 + _field2.GetHashCode();
        return hash;
    }
}

// BAD: System.HashCode (non-deterministic across processes)
public override int GetHashCode()
{
    return HashCode.Combine(_field1, _field2);
}

// BAD: Inline string hashing (use HashCodeHelper instead)
public override int GetHashCode()
{
    return Text != null ? StringComparer.Ordinal.GetHashCode(Text) : 0;
}
```

### Location

- [`DataStructs/HashCodeHelper.cs`](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/HashCodeHelper.cs) — Main helper class
- `DeterministicHashBuilder` — Struct for incremental hash building
- `TypeTraits<T>` — Cached type metadata for efficient hashing

______________________________________________________________________

## Common Patterns in NovaSharp

### Module Resolution Results

```csharp
// ✅ Good: Struct result type
internal readonly struct ModuleResolutionResult
{
    public readonly bool Found;
    public readonly string ModulePath;
    public readonly ModuleLoader Loader;
    
    public static ModuleResolutionResult NotFound() => default;
    public static ModuleResolutionResult Success(string path, ModuleLoader loader) 
        => new(true, path, loader);
}
```

### Lexer Tokens and Positions

```csharp
// ✅ Good: Small, immutable, copied frequently
internal readonly struct SourcePosition
{
    public readonly int Line;
    public readonly int Column;
}
```

### Error Information

```csharp
// ✅ Good: Avoid allocations for common "no error" case
internal readonly struct ErrorInfo
{
    public readonly bool HasError;
    public readonly string Message;  // Only allocated when error occurs
    
    public static readonly ErrorInfo None = default;
    public static ErrorInfo Create(string message) => new(true, message);
}
```

______________________________________________________________________

## Checklist for New Code

Before submitting new types, verify:

- [ ] **Using explicit types everywhere?** — Never use `var`
- [ ] **Could this be a `readonly struct`?** (Small, immutable, value semantics)
- [ ] **If struct, did you add `IEquatable<T>`, `Equals`, `GetHashCode`, `==`, `!=`?** (Required by CA1815)
- [ ] **Using `HashCodeHelper` for `GetHashCode()`?** → Never use bespoke `hash * 31` patterns or `HashCode.Combine()`
- [ ] **Does it contain Span<T>?** → Use `ref struct`
- [ ] **Am I building strings?** → Use `ZStringBuilder.Create()`
- [ ] **Am I using pooled resources?** → **ALWAYS use `using` with `Get()`, NEVER manual `Rent()`/`Return()`**
  - Fixed exact size needed (reflection, VM frames)? → `DynValueArrayPool.Get()`/`ObjectArrayPool.Get()`
  - Collections (List, HashSet, Dictionary)? → `ListPool<T>.Get()`, `HashSetPool<T>.Get()`, etc.
  - Variable/dynamic size arrays? → `SystemArrayPool<T>.Get()`
  - Small, known compile-time size? → `stackalloc` (no pool needed)
  - Fixed size, hot path, reusable across calls? → `[ThreadStatic]` cached arrays
- [ ] **Does my type own pooled resources?** → Implement `IDisposable` and store `PooledResource<T>` as field
- [ ] **Am I creating complex state objects repeatedly?** → Consider `[ThreadStatic]` pooling with `Reset()`
- [ ] **Am I sorting with a struct comparer?** → Use `IListSortExtensions.Sort<T, TComparer>()`
- [ ] **Am I slicing strings?** → Use `ReadOnlySpan<char>` instead
- [ ] **Am I passing structs to interface parameters?** → Avoid boxing
- [ ] **Am I using closures/lambdas?** → Use static lambdas or explicit state passing
- [ ] **Am I using LINQ?** → Replace with explicit loops in hot paths
- [ ] **Am I computing the same values repeatedly?** → Pre-compute lookup tables
- [ ] **Is this a frequently-allocated short-lived class?** → Consider converting to `readonly struct`
- [ ] **Is this on a hot path?** → Benchmark before/after
- [ ] **Using `string.GetHashCode()`?** → Use `GetHashCode(StringComparison.Ordinal)` (Required by CA1307)

______________________________________________________________________

## Related Documentation

- [docs/performance/optimization-opportunities.md](../../docs/performance/optimization-opportunities.md) — Detailed allocation analysis
- [docs/performance/high-performance-libraries-research.md](../../docs/performance/high-performance-libraries-research.md) — Library research
- [progress/session-075-kopilua-charptr-struct.md](../../progress/session-075-kopilua-charptr-struct.md) — CharPtr class→struct conversion (58-85% allocation reduction)
- [progress/session-076-kopilua-phase3-optimization.md](../../progress/session-076-kopilua-phase3-optimization.md) — Thread-local caching, pre-computed tables
- [DataStructs/ZStringBuilder.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ZStringBuilder.cs) — ZString wrapper
- [DataStructs/PooledResource.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/PooledResource.cs) — **Disposable pool wrapper struct (RAII pattern)**
- [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs) — **ListPool, HashSetPool, DictionaryPool, etc.**
- [DataStructs/DynValueArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/DynValueArrayPool.cs) — Fixed-size DynValue array pool
- [DataStructs/ObjectArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ObjectArrayPool.cs) — Fixed-size object array pool (reflection)
- [DataStructs/SystemArrayPool.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/SystemArrayPool.cs) — Variable-size array pool wrapper
- [DataStructs/IListSortExtensions.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/IListSortExtensions.cs) — Boxing-free pdqsort for IList<T>
- [DataStructs/HashCodeHelper.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/HashCodeHelper.cs) — Deterministic FNV-1a hash code composition
- [LuaPort/KopiLuaStringLib.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs) — Example: thread-local caching, struct conversion

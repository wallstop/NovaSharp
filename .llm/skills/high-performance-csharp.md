# High-Performance C# Coding Guidelines

When writing new code for NovaSharp, prioritize **minimal allocations** and **maximum efficiency**. This interpreter runs hot paths millions of times—every allocation and boxing operation has measurable impact.

______________________________________________________________________

## 🔴 Core Principles

1. **Prefer value types (structs) over reference types (classes)** when data is small and short-lived
1. **Avoid allocations in hot paths** — use pooling, stack allocation, or spans
1. **Use `readonly struct` for immutable value types** — enables compiler optimizations
1. **Prefer `ref struct` for stack-only types** — enables `Span<T>` fields and prevents heap escape
1. **Always measure** — use BenchmarkDotNet before/after for performance-critical changes

______________________________________________________________________

## String Building

### ✅ DO: Use ZString for string concatenation

```csharp
using Cysharp.Text;

// Safe for nested/recursive calls (uses ArrayPool)
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
sb.Append(": ");
sb.Append(message);
return sb.ToString();

// Hot non-nested paths only (ThreadStatic buffer)
using Utf16ValueStringBuilder sb = ZStringBuilder.CreateNonNested();
```

### ❌ DON'T: Use StringBuilder or string concatenation

```csharp
// BAD: StringBuilder allocates
var sb = new StringBuilder();

// BAD: String interpolation allocates
return $"Error at line {lineNumber}: {message}";

// BAD: String.Concat allocates
return "Error: " + message;
```

### Exception: ZString.Concat for simple cases

```csharp
// OK for 2-3 element concatenation (still zero-alloc)
return ZString.Concat("\"", input, "\"");
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

## Array and Buffer Pooling

### ✅ DO: Use ArrayPool for temporary arrays

```csharp
using System.Buffers;

// Rent from pool
char[] buffer = ArrayPool<char>.Shared.Rent(256);
try
{
    // Use buffer...
    int written = FormatValue(value, buffer);
    return new string(buffer, 0, written);
}
finally
{
    ArrayPool<char>.Shared.Return(buffer);
}
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

- [ ] **Could this be a `readonly struct`?** (Small, immutable, value semantics)
- [ ] **If struct, did you add `IEquatable<T>`, `Equals`, `GetHashCode`, `==`, `!=`?** (Required by CA1815)
- [ ] **Does it contain Span<T>?** → Use `ref struct`
- [ ] **Am I building strings?** → Use `ZStringBuilder.Create()`
- [ ] **Am I allocating temporary arrays?** → Use `ArrayPool<T>` or `stackalloc`
- [ ] **Am I slicing strings?** → Use `ReadOnlySpan<char>` instead
- [ ] **Am I passing structs to interface parameters?** → Avoid boxing
- [ ] **Is this on a hot path?** → Benchmark before/after
- [ ] **Using `string.GetHashCode()`?** → Use `GetHashCode(StringComparison.Ordinal)` (Required by CA1307)

______________________________________________________________________

## Related Documentation

- [docs/performance/optimization-opportunities.md](../../docs/performance/optimization-opportunities.md) — Detailed allocation analysis
- [docs/performance/high-performance-libraries-research.md](../../docs/performance/high-performance-libraries-research.md) — Library research
- [DataStructs/ZStringBuilder.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ZStringBuilder.cs) — ZString wrapper

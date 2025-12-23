# Span Optimization Guidelines

This document provides guidance for using `Span<T>` and `ReadOnlySpan<T>` to eliminate unnecessary array allocations in NovaSharp.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (general performance), [zstring-migration](zstring-migration.md) (string building)

______________________________________________________________________

## 🔴 Core Rule

**NEVER allocate arrays when spans can be used instead. Prefer stack-based or slice-based operations over heap allocations.**

______________________________________________________________________

## Why Spans?

Spans provide a view into contiguous memory without allocation:

| Operation                 | Traditional                | Span-Based                      |
| ------------------------- | -------------------------- | ------------------------------- |
| `string.Split('\n')`      | Allocates array of strings | Zero allocation enumeration     |
| `string.Substring(5, 10)` | Allocates new string       | `AsSpan(5, 10)` — no allocation |
| `array.ToArray()`         | Allocates copy             | `AsSpan()` — no allocation      |
| `text.ToCharArray()`      | Allocates char[]           | `AsSpan()` — no allocation      |

______________________________________________________________________

## Migration Patterns

### Pattern 1: String Split → Span Enumeration

```csharp
// ❌ BEFORE: Allocates array of strings
string[] lines = code.Split('\n');
foreach (string line in lines)
{
    ProcessLine(line);
}

// ✅ AFTER: Zero-allocation span enumeration
ReadOnlySpan<char> remaining = code.AsSpan();
while (!remaining.IsEmpty)
{
    int idx = remaining.IndexOf('\n');
    ReadOnlySpan<char> line = idx < 0 ? remaining : remaining.Slice(0, idx);
    ProcessLine(line);
    remaining = idx < 0 ? ReadOnlySpan<char>.Empty : remaining.Slice(idx + 1);
}
```

### Pattern 2: Line Counting with Span

```csharp
// ❌ BEFORE: Split just to count
int lineCount = code.Split('\n').Length;

// ✅ AFTER: Count without allocation
int lineCount = 1;
foreach (char c in code.AsSpan())
{
    if (c == '\n') lineCount++;
}
```

### Pattern 3: Substring → Span Slice

```csharp
// ❌ BEFORE: Allocates new string
string sub = text.Substring(start, length);
if (sub == "expected")
{
    // ...
}

// ✅ AFTER: Compare without allocation
ReadOnlySpan<char> sub = text.AsSpan(start, length);
if (sub.SequenceEqual("expected"))
{
    // ...
}

// If string is ultimately needed, defer allocation:
// ✅ Process with span, allocate only at the end
ReadOnlySpan<char> span = text.AsSpan(start, length);
// ... work with span ...
string result = span.ToString();  // Single allocation when required
```

### Pattern 4: ToCharArray → AsSpan

```csharp
// ❌ BEFORE: Allocates char array
char[] chars = text.ToCharArray();
for (int i = 0; i < chars.Length; i++)
{
    if (char.IsDigit(chars[i])) { ... }
}

// ✅ AFTER: No allocation
ReadOnlySpan<char> chars = text.AsSpan();
for (int i = 0; i < chars.Length; i++)
{
    if (char.IsDigit(chars[i])) { ... }
}
```

### Pattern 5: Building Result Arrays with Pre-counting

```csharp
// ❌ BEFORE: List with dynamic growth
List<string> results = new();
foreach (var item in source)
{
    results.Add(Process(item));
}
return results.ToArray();

// ✅ AFTER: Pre-count and direct array allocation
int count = 0;
foreach (var item in source) count++;

string[] results = new string[count];
int i = 0;
foreach (var item in source)
{
    results[i++] = Process(item);
}
return results;
```

### Pattern 6: Span-Based String Searching

```csharp
// ❌ BEFORE: Multiple substring allocations
int colonPos = line.IndexOf(':');
string key = line.Substring(0, colonPos);
string value = line.Substring(colonPos + 1);

// ✅ AFTER: Span slicing
ReadOnlySpan<char> lineSpan = line.AsSpan();
int colonPos = lineSpan.IndexOf(':');
ReadOnlySpan<char> key = lineSpan.Slice(0, colonPos);
ReadOnlySpan<char> value = lineSpan.Slice(colonPos + 1);
// Only call .ToString() when you actually need a string
```

### Pattern 7: Span-Based Parsing

```csharp
// ❌ BEFORE: Multiple allocations
string numberStr = text.Substring(start, end - start);
int value = int.Parse(numberStr);

// ✅ AFTER: Parse directly from span
ReadOnlySpan<char> numberSpan = text.AsSpan(start, end - start);
int value = int.Parse(numberSpan);
```

______________________________________________________________________

## stackalloc for Small Fixed Buffers

For small, fixed-size buffers with compile-time known sizes:

```csharp
// ✅ Stack allocation for small buffers
Span<char> buffer = stackalloc char[64];
// Use buffer...

// ✅ With safety check for larger dynamic sizes
Span<char> buffer = length <= 256 
    ? stackalloc char[length] 
    : new char[length];  // Fall back to heap for large
```

______________________________________________________________________

## Span Limitations (ref struct)

Spans are `ref struct` types with restrictions:

```csharp
// ❌ Cannot store spans in fields of regular classes/structs
class BadExample
{
    private ReadOnlySpan<char> _span;  // Compile error!
}

// ❌ Cannot use spans in async methods
async Task BadAsync()
{
    ReadOnlySpan<char> span = text.AsSpan();  // Compile error!
}

// ❌ Cannot box spans
object boxed = text.AsSpan();  // Compile error!

// ✅ Use Memory<T> when you need to store or pass across async boundaries
Memory<char> memory = text.AsMemory();
```

______________________________________________________________________

## Validation Commands

```bash
# Find Split operations (high priority)
rg '\.Split\(' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs

# Find Substring operations
rg '\.Substring\(' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs

# Find ToCharArray operations
rg '\.ToCharArray\(' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs

# Find ToArray that might be avoidable
rg '\.ToArray\(\)' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs

# Find new object() lock patterns (should reuse existing objects)
rg 'new object\(\)' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs
```

______________________________________________________________________

## When NOT to Use Spans

1. **Async methods**: Spans cannot cross await boundaries; use `Memory<T>`
1. **Stored in fields**: Regular classes cannot have span fields; use arrays or `Memory<T>`
1. **Interface parameters**: Spans cannot be used as generic type arguments in some contexts
1. **Interop boundaries**: Some P/Invoke or COM scenarios require arrays

```csharp
// These are fine as-is:
async Task ProcessAsync(string text)  // Can't use span across await
{
    Memory<char> memory = text.AsMemory();  // Use Memory<T> instead
    await SomeAsyncOperation();
    ProcessMemory(memory);
}
```

______________________________________________________________________

## Related Documentation

- [zstring-migration.md](zstring-migration.md) — Zero-allocation string building
- [high-performance-csharp.md](high-performance-csharp.md) — General performance guidelines

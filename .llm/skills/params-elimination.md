# Skill: Params Array Elimination

**When to use**: Eliminating hidden array allocations from `params` parameters in hot paths.

**Related Skills**: [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (general refactoring), [high-performance-csharp](high-performance-csharp.md) (performance patterns)

______________________________________________________________________

## 🔴 The Problem with `params`

Every call to a method with `params` allocates a new array:

```csharp
// Method definition
public void Log(params object[] args) { }

// Every call allocates a new object[] on the heap!
Log("Error at line", lineNum, "column", colNum);  // Allocates object[4]
Log("Value:", value);                               // Allocates object[2]
Log("Done");                                        // Allocates object[1]
```

### Allocation Impact

| Params Call | Allocation                           |
| ----------- | ------------------------------------ |
| 0 arguments | 0 bytes (empty array may be cached)  |
| 1 argument  | 24+ bytes (array header + 1 element) |
| 2 arguments | 32+ bytes                            |
| 3 arguments | 40+ bytes                            |
| 4 arguments | 48+ bytes                            |
| N arguments | ~24 + 8\*N bytes                     |

**Plus boxing** if arguments are value types going to `object[]`!

```csharp
// This allocates BOTH the array AND boxes each int!
void Process(params object[] args) { }
Process(1, 2, 3);  // Array allocation + 3 box allocations = 4 allocations!
```

### Real-World Impact

From benchmarks: **256,000 calls with `params` = 9 MB garbage**

______________________________________________________________________

## 🔴 Solution 1: Overload Pattern

Create explicit overloads for common argument counts:

```csharp
// ❌ BEFORE: Single params method
public void Log(params object[] args)
{
    foreach (var arg in args)
        Write(arg);
}

// ✅ AFTER: Overloads for common cases
public void Log()
{
    // Empty call - no allocation
}

public void Log(object arg0)
{
    Write(arg0);
}

public void Log(object arg0, object arg1)
{
    Write(arg0);
    Write(arg1);
}

public void Log(object arg0, object arg1, object arg2)
{
    Write(arg0);
    Write(arg1);
    Write(arg2);
}

public void Log(object arg0, object arg1, object arg2, object arg3)
{
    Write(arg0);
    Write(arg1);
    Write(arg2);
    Write(arg3);
}

// Fallback for 5+ args (rare case, allocation acceptable)
public void Log(params object[] args)
{
    foreach (var arg in args)
        Write(arg);
}
```

### Generic Overloads (Avoid Boxing)

```csharp
// ❌ BAD: Boxing value types
public void Log(object arg0, object arg1) { }
Log(42, 3.14);  // Both values boxed!

// ✅ GOOD: Generic overloads prevent boxing
public void Log<T0>(T0 arg0)
{
    Write(arg0);
}

public void Log<T0, T1>(T0 arg0, T1 arg1)
{
    Write(arg0);
    Write(arg1);
}

public void Log<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2)
{
    Write(arg0);
    Write(arg1);
    Write(arg2);
}

// Now value types are not boxed
Log(42, 3.14);  // No boxing, no array allocation
```

______________________________________________________________________

## 🔴 Solution 2: Span-Based API

For methods that truly need variable arguments, use `ReadOnlySpan<T>`:

```csharp
// ✅ Zero-allocation with Span
public void Process(ReadOnlySpan<int> values)
{
    for (int i = 0; i < values.Length; i++)
    {
        DoSomething(values[i]);
    }
}

// Callers can use stackalloc
Span<int> args = stackalloc int[] { 1, 2, 3, 4 };
Process(args);

// Or pass existing array without allocation
Process(existingArray.AsSpan());

// Or inline with collection expression (C# 12+)
Process([1, 2, 3, 4]);
```

### Span Overload Alongside Params

Provide both for flexibility:

```csharp
// Modern callers use Span (zero allocation)
public void Process(ReadOnlySpan<DynValue> args)
{
    for (int i = 0; i < args.Length; i++)
    {
        HandleArg(args[i]);
    }
}

// Legacy callers can still use params (allocates, but works)
public void Process(params DynValue[] args)
{
    Process(args.AsSpan());
}
```

______________________________________________________________________

## 🔴 Solution 3: Builder Pattern

For complex construction, use a builder:

```csharp
// ❌ BEFORE: params allocation
public Message Create(params MessagePart[] parts)
{
    return new Message(parts);
}

var msg = Create(Part1(), Part2(), Part3());  // Array allocation

// ✅ AFTER: Builder pattern
public readonly ref struct MessageBuilder
{
    private readonly Span<MessagePart> _buffer;
    private int _count;
    
    public MessageBuilder(Span<MessagePart> buffer)
    {
        _buffer = buffer;
        _count = 0;
    }
    
    public MessageBuilder Add(MessagePart part)
    {
        _buffer[_count++] = part;
        return this;
    }
    
    public Message Build() => new Message(_buffer.Slice(0, _count));
}

// Usage: zero allocation with stackalloc
Span<MessagePart> buffer = stackalloc MessagePart[4];
Message msg = new MessageBuilder(buffer)
    .Add(Part1())
    .Add(Part2())
    .Add(Part3())
    .Build();
```

______________________________________________________________________

## 🔴 Solution 4: Tuple-Based (Small Fixed Counts)

For exactly 2-4 arguments, consider tuples:

```csharp
// ✅ Value tuple - no allocation
public void Process((int a, int b, int c) args)
{
    DoSomething(args.a);
    DoSomething(args.b);
    DoSomething(args.c);
}

Process((1, 2, 3));  // No allocation - stack-allocated tuple
```

______________________________________________________________________

## 🔴 Pattern: NovaSharp DynValue Args

For Lua function calls that accept variable DynValue arguments:

```csharp
// ❌ BEFORE: Every call allocates
public DynValue Call(params DynValue[] args)
{
    return ExecuteCall(args);
}

// ✅ AFTER: Overloads for common arities + span fallback
public DynValue Call()
{
    return ExecuteCall(ReadOnlySpan<DynValue>.Empty);
}

public DynValue Call(DynValue arg0)
{
    // Use stackalloc for single arg
    Span<DynValue> args = stackalloc DynValue[1];
    args[0] = arg0;
    return ExecuteCall(args);
}

public DynValue Call(DynValue arg0, DynValue arg1)
{
    Span<DynValue> args = stackalloc DynValue[2];
    args[0] = arg0;
    args[1] = arg1;
    return ExecuteCall(args);
}

public DynValue Call(DynValue arg0, DynValue arg1, DynValue arg2)
{
    Span<DynValue> args = stackalloc DynValue[3];
    args[0] = arg0;
    args[1] = arg1;
    args[2] = arg2;
    return ExecuteCall(args);
}

public DynValue Call(DynValue arg0, DynValue arg1, DynValue arg2, DynValue arg3)
{
    Span<DynValue> args = stackalloc DynValue[4];
    args[0] = arg0;
    args[1] = arg1;
    args[2] = arg2;
    args[3] = arg3;
    return ExecuteCall(args);
}

// 5+ args: Use pooled array
public DynValue Call(params DynValue[] args)
{
    return ExecuteCall(args.AsSpan());
}

// Core implementation takes span
private DynValue ExecuteCall(ReadOnlySpan<DynValue> args)
{
    // Implementation
}
```

______________________________________________________________________

## 🔴 Unity-Specific Considerations

### Mathf.Max / Mathf.Min Trap

```csharp
// ❌ BAD: Mathf.Max(params float[]) allocates per call
float max = Mathf.Max(a, b, c);  // Array allocation!

// ✅ GOOD: Chain the 2-arg overload
float max = Mathf.Max(Mathf.Max(a, b), c);  // No allocation
```

### Debug.Log with Formatting

```csharp
// ❌ BAD: Allocates format string + params array
Debug.LogFormat("Position: {0}, {1}, {2}", x, y, z);

// ✅ BETTER: Use interpolation (still allocates string, but no params array)
Debug.Log($"Position: {x}, {y}, {z}");

// ✅ BEST: Only in debug builds
#if UNITY_EDITOR
Debug.Log($"Position: {x}, {y}, {z}");
#endif
```

______________________________________________________________________

## 🔴 Detecting Params Allocations

### Regex Search

```bash
# Find params method definitions
rg 'params\s+\w+\[\]' src/ --type cs

# Find calls to known params methods
rg 'String\.Format\(|Mathf\.Max\([^,]+,[^,]+,' src/ --type cs
```

### Common Culprits

| Method                  | Alternative                         |
| ----------------------- | ----------------------------------- |
| `string.Format()`       | `ZString.Format()` or interpolation |
| `string.Concat(params)` | `ZString.Concat()` with overloads   |
| `string.Join(params)`   | `ZString.Join()`                    |
| `Mathf.Max(params)`     | Chain `Mathf.Max(a, b)`             |
| `Mathf.Min(params)`     | Chain `Mathf.Min(a, b)`             |
| `Debug.LogFormat()`     | Conditional compilation             |
| `List.AddRange(params)` | Loop with Add                       |

______________________________________________________________________

## Checklist for Params Elimination

When you find a `params` method in a hot path:

- [ ] **Count typical argument counts** — Most calls use 1-4 args?
- [ ] **Create overloads** for 0, 1, 2, 3, 4 arguments
- [ ] **Use generics** if args are value types (avoid boxing)
- [ ] **Consider Span<T>** for the implementation
- [ ] **Keep params fallback** for 5+ arguments (rare case)
- [ ] **Verify** with allocation profiler

______________________________________________________________________

## Quick Reference: Params Replacement

| Params Pattern              | Zero-Alloc Replacement                                       |
| --------------------------- | ------------------------------------------------------------ |
| `void M(params T[] a)`      | Overloads: `M()`, `M(T)`, `M(T,T)`, `M(T,T,T)`, `M(T,T,T,T)` |
| `void M(params object[] a)` | Generic overloads: `M<T>()`, `M<T0,T1>()`, etc.              |
| Variable args               | `ReadOnlySpan<T>` parameter                                  |
| String formatting           | `ZString.Format()` or `ZStringBuilder`                       |
| Math operations             | Chain binary overloads                                       |

______________________________________________________________________

## Resources

- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — General refactoring patterns
- [high-performance-csharp](high-performance-csharp.md) — Performance guidelines
- [unity-gc-patterns](unity-gc-patterns.md) — Unity-specific patterns

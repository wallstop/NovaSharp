______________________________________________________________________

triggers:

- "defensive programming"
- "error handling"
- "robust code"
- "graceful failure"
- "null handling"
  category: core
  related:
- high-performance-csharp
- correctness-then-performance
  priority: core

______________________________________________________________________

# Skill: Defensive Programming

**When to use**: Writing any production code, especially in parsers, interpreters, and public APIs.

**Code Samples**: [defensive-patterns](../code-samples/defensive-patterns.md)

**Related Skills**: [high-performance-csharp](high-performance-csharp.md), [correctness-then-performance](correctness-then-performance.md)

______________________________________________________________________

## Philosophy: Resilient Code

Production code must be **robust and resilient**. Every piece of code should assume that:

1. Inputs may be invalid
1. State may be corrupted
1. External dependencies may fail
1. Edge cases WILL occur
1. "Impossible" scenarios happen

**Prefer graceful degradation over crashes.** Return sentinel values (Nil, default) when possible rather than throwing exceptions.

______________________________________________________________________

## Core Patterns

### 1. Guard Clauses

Return early with graceful fallbacks:

```csharp
public DynValue ProcessValue(DynValue input)
{
    if (input == null || input.Type != DataType.Table)
    {
        return DynValue.Nil;
    }

    Table table = input.Table;
    if (table == null)
    {
        return DynValue.Nil;
    }

    return table.Get("key");
}
```

### 2. Try-Pattern

Make success/failure explicit:

```csharp
public bool TryGetValue(string key, out DynValue result)
{
    if (string.IsNullOrEmpty(key))
    {
        result = DynValue.Nil;
        return false;
    }

    return _values.TryGetValue(key, out result);
}
```

### 3. Bounds Checking

Always check before collection access:

```csharp
public DynValue GetArgument(int index)
{
    if (_arguments == null || index < 0 || index >= _arguments.Length)
    {
        return DynValue.Nil;
    }

    return _arguments[index];
}
```

### 4. Safe Casting

Use pattern matching:

```csharp
public bool ProcessCallback(object callback)
{
    if (callback is Closure closure)
    {
        closure.Call();
        return true;
    }

    return false;
}
```

See [defensive-patterns](../code-samples/defensive-patterns.md) for more examples.

______________________________________________________________________

## Exception Guidelines

| Scenario                    | Action                              |
| --------------------------- | ----------------------------------- |
| Optional value not found    | Return sentinel (Nil, default)      |
| Operation may fail          | Try-pattern (TryGetValue, TryParse) |
| Invalid input to public API | Return error/default                |
| Invalid input (truly bad)   | Throw ArgumentException             |
| Internal invariant violated | Assert + graceful handling          |
| Resource cleanup            | IDisposable + defensive Dispose     |

### When to Throw vs Return Default

```csharp
// GOOD: Return default for optional/expected failures
public DynValue GetGlobal(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return DynValue.Nil;
    }
    // ...
}

// GOOD: Throw for truly exceptional/programmer errors
public void RegisterType(Type type)
{
    if (type == null)
    {
        throw new ArgumentNullException(nameof(type));
    }
    // ...
}
```

______________________________________________________________________

## State Management

### Validate Before Operations

```csharp
public bool Execute()
{
    if (_script == null || _isDisposed)
    {
        return false;
    }

    try
    {
        _script.DoString("print('hello')");
        return true;
    }
    catch (ScriptRuntimeException)
    {
        return false;  // Script error - not our bug
    }
}
```

### Atomic State Updates

Validate everything first, then update:

```csharp
public bool UpdateState(string key, DynValue value)
{
    // Validate first
    if (string.IsNullOrEmpty(key) || value == null)
    {
        return false;
    }

    // Then update atomically
    _keys.Add(key);
    _values[key] = value;
    _count++;
    return true;
}
```

______________________________________________________________________

## Debug vs Release

Use assertions for invariants:

```csharp
public void ProcessInstruction(Instruction instruction)
{
    // Debug assertion - catches programmer errors
    Debug.Assert(instruction != null, "Instruction should never be null");

    // Release code - handle gracefully
    if (instruction == null)
    {
        return;
    }

    // ... process instruction
}
```

______________________________________________________________________

## IDisposable Pattern

```csharp
public sealed class ResourceHolder : IDisposable
{
    private bool _disposed;
    private Resource _resource;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Clean up
        if (_resource != null)
        {
            _resource.Release();
            _resource = null;
        }
    }
}
```

______________________________________________________________________

## Quick Checklist

- [ ] Null checks on all inputs?
- [ ] Bounds checks before collection access?
- [ ] Disposed flag checked before operations?
- [ ] Using TryGetValue instead of ContainsKey+indexer?
- [ ] Pattern matching instead of direct casts?
- [ ] Graceful degradation instead of crashes?
- [ ] Debug.Assert for invariants?
- [ ] IDisposable with defensive Dispose?

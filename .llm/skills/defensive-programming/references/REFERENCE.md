# Defensive Programming Reference

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
public LuaValue GetGlobal(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return LuaValue.Nil;
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
public bool UpdateState(string key, LuaValue value)
{
    // Validate first
    if (string.IsNullOrEmpty(key))
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

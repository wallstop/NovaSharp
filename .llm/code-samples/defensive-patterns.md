# Defensive Programming Patterns

Patterns for robust, resilient production code in NovaSharp.

______________________________________________________________________

## Guard Clauses

Return early with graceful fallbacks instead of throwing.

```csharp
// BAD: Assumes input is valid
public DynValue ProcessValue(DynValue input)
{
    return input.Table.Get("key");  // NullReferenceException if null
}

// GOOD: Defensive with graceful fallback
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

______________________________________________________________________

## Try-Pattern

Make success/failure explicit in the API.

```csharp
// BAD: Returns null on failure
public DynValue GetValue(string key)
{
    if (!_values.ContainsKey(key))
    {
        return null;  // Easy to forget null check
    }
    return _values[key];
}

// GOOD: Try-pattern makes success explicit
public bool TryGetValue(string key, out DynValue result)
{
    if (string.IsNullOrEmpty(key))
    {
        result = DynValue.Nil;
        return false;
    }

    return _values.TryGetValue(key, out result);
}

// ALSO GOOD: Return Nil for Lua semantics
public DynValue GetValue(string key)
{
    if (string.IsNullOrEmpty(key))
    {
        return DynValue.Nil;
    }

    if (_values.TryGetValue(key, out DynValue result))
    {
        return result;
    }

    return DynValue.Nil;
}
```

______________________________________________________________________

## State Validation

Validate state before operations.

```csharp
// BAD: Assumes _script is always valid
public void Execute()
{
    _script.DoString("print('hello')");
}

// GOOD: Validate state first
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

______________________________________________________________________

## Bounds Checking

Always check bounds before collection access.

```csharp
// BAD: Assumes index is valid
public DynValue GetArgument(int index)
{
    return _arguments[index];
}

// GOOD: Bounds checking with fallback
public DynValue GetArgument(int index)
{
    if (_arguments == null || index < 0 || index >= _arguments.Length)
    {
        return DynValue.Nil;
    }

    return _arguments[index];
}
```

______________________________________________________________________

## Safe Casting

Use safe type checking instead of direct casts.

```csharp
// BAD: Direct cast assumes type
public void ProcessCallback(object callback)
{
    Closure closure = (Closure)callback;  // InvalidCastException
    closure.Call();
}

// GOOD: Pattern matching
public bool ProcessCallback(object callback)
{
    if (callback is Closure closure)
    {
        closure.Call();
        return true;
    }

    return false;
}

// ALSO GOOD: as operator with null check
public bool ProcessCallback(object callback)
{
    Closure closure = callback as Closure;
    if (closure == null)
    {
        return false;
    }

    closure.Call();
    return true;
}
```

______________________________________________________________________

## Atomic State Updates

Validate everything first, then update atomically.

```csharp
// BAD: State can become inconsistent
public void UpdateState(string key, DynValue value)
{
    _keys.Add(key);        // Added
    _values[key] = value;  // What if this throws?
    _count++;
}

// GOOD: Validate first, then update
public bool UpdateState(string key, DynValue value)
{
    if (string.IsNullOrEmpty(key) || value == null)
    {
        return false;
    }

    if (_keys.Contains(key))
    {
        _values[key] = value;
        return true;
    }

    _keys.Add(key);
    _values[key] = value;
    _count++;
    return true;
}
```

______________________________________________________________________

## Rollback on Failure

For complex operations, save state for potential rollback.

```csharp
public bool ComplexOperation()
{
    int originalCount = _count;
    List<string> addedKeys = new List<string>();

    try
    {
        foreach (string key in _pendingKeys)
        {
            _keys.Add(key);
            addedKeys.Add(key);
            _count++;

            if (!ValidateState())
            {
                // Rollback
                foreach (string addedKey in addedKeys)
                {
                    _keys.Remove(addedKey);
                }
                _count = originalCount;
                return false;
            }
        }
        return true;
    }
    catch
    {
        // Rollback on exception
        foreach (string addedKey in addedKeys)
        {
            _keys.Remove(addedKey);
        }
        _count = originalCount;
        return false;
    }
}
```

______________________________________________________________________

## Debug vs Release

Use assertions for invariants during development.

```csharp
public void ProcessInstruction(Instruction instruction)
{
    // Debug assertion - catches programmer errors
    Debug.Assert(instruction != null, "Instruction should never be null");
    Debug.Assert(_vm != null, "VM must be initialized");

    // Release code - handle gracefully
    if (instruction == null || _vm == null)
    {
        return;
    }

    // ... process instruction
}
```

______________________________________________________________________

## Exception Guidelines

| Scenario                    | Action                          |
| --------------------------- | ------------------------------- |
| Optional value not found    | Return sentinel (Nil, default)  |
| Operation may fail          | Try-pattern                     |
| Invalid input to public API | Return error/default            |
| Invalid input (truly bad)   | Throw ArgumentException         |
| Internal invariant violated | Assert + graceful handling      |
| Resource cleanup            | IDisposable + defensive Dispose |

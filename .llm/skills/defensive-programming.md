# Skill: Defensive Programming

**When to use**: Writing ANY production code — APIs, internal methods, error handling, state management.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (performance considerations), [clr-interop](clr-interop.md) (handling user-provided types/values)

______________________________________________________________________

## 🔴 Core Philosophy

NovaSharp production code must be **extremely robust and resilient**. Follow these principles:

1. **Assume nothing** — Don't trust inputs, state, or invariants without verification
1. **Handle all errors** — Every possible error path must be handled gracefully
1. **Never throw exceptions** — Exceptions are for truly exceptional cases (catastrophically bad user input)
1. **Maintain internal consistency** — Internal state must ALWAYS be valid, even after errors
1. **Fail gracefully** — When something goes wrong, degrade gracefully rather than crash

### Exception Philosophy

| Scenario                               | Action                                         |
| -------------------------------------- | ---------------------------------------------- |
| **Internal state corruption**          | Return error result, log, maintain consistency |
| **Invalid user input (recoverable)**   | Return error result or default value           |
| **Invalid user input (unrecoverable)** | May throw with clear message                   |
| **Null/missing optional data**         | Handle with defaults or early return           |
| **External resource failure**          | Return error result, allow retry               |
| **Programmer error (debug only)**      | Assert in DEBUG, handle gracefully in RELEASE  |

______________________________________________________________________

## 🔴 Defensive Patterns

### Pattern 1: Guard Clauses with Graceful Handling

```csharp
// ❌ BAD: Assumes input is valid, throws on null
public DynValue ProcessValue(DynValue input)
{
    return input.Table.Get("key");  // NullReferenceException if input is null or not a table
}

// ✅ GOOD: Defensive with graceful fallback
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

### Pattern 2: Try-Pattern for Operations That Can Fail

```csharp
// ❌ BAD: Returns null on failure (caller must remember to check)
public DynValue GetValue(string key)
{
    if (!_values.ContainsKey(key))
    {
        return null;  // Easy to forget null check
    }
    return _values[key];
}

// ✅ GOOD: Try-pattern makes success/failure explicit
public bool TryGetValue(string key, out DynValue result)
{
    if (string.IsNullOrEmpty(key))
    {
        result = DynValue.Nil;
        return false;
    }
    
    return _values.TryGetValue(key, out result);
}

// ✅ ALSO GOOD: Return Nil instead of null for Lua semantics
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

### Pattern 3: Validate State Before Operations

```csharp
// ❌ BAD: Assumes _script is always valid
public void Execute()
{
    _script.DoString("print('hello')");
}

// ✅ GOOD: Validate state, handle invalid gracefully
public bool Execute()
{
    if (_script == null)
    {
        return false;
    }
    
    if (_isDisposed)
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
        // Script error - not our bug, return failure
        return false;
    }
}
```

### Pattern 4: Defensive Collection Access

```csharp
// ❌ BAD: Assumes index is valid
public DynValue GetArgument(int index)
{
    return _arguments[index];
}

// ✅ GOOD: Bounds checking with graceful fallback
public DynValue GetArgument(int index)
{
    if (_arguments == null || index < 0 || index >= _arguments.Length)
    {
        return DynValue.Nil;
    }
    
    return _arguments[index];
}
```

### Pattern 5: Safe Casting and Type Checks

```csharp
// ❌ BAD: Direct cast assumes type
public void ProcessCallback(object callback)
{
    Closure closure = (Closure)callback;  // InvalidCastException
    closure.Call();
}

// ✅ GOOD: Safe type checking
public bool ProcessCallback(object callback)
{
    if (callback == null)
    {
        return false;
    }
    
    Closure closure = callback as Closure;
    if (closure == null)
    {
        return false;
    }
    
    closure.Call();
    return true;
}

// ✅ ALSO GOOD: Pattern matching (C# 7+)
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

______________________________________________________________________

## 🔴 Internal State Consistency

**The most critical rule**: Internal state must ALWAYS be consistent, even when errors occur.

### Pattern: Atomic State Updates

```csharp
// ❌ BAD: State can become inconsistent if operation fails partway
public void UpdateState(string key, DynValue value)
{
    _keys.Add(key);        // Added
    _values[key] = value;  // What if this throws? _keys is now inconsistent
    _count++;
}

// ✅ GOOD: Validate everything first, then update atomically
public bool UpdateState(string key, DynValue value)
{
    // Validate inputs
    if (string.IsNullOrEmpty(key) || value == null)
    {
        return false;
    }
    
    // Check if operation is valid
    if (_keys.Contains(key))
    {
        // Already exists - just update value
        _values[key] = value;
        return true;
    }
    
    // All validation passed - now update state
    _keys.Add(key);
    _values[key] = value;
    _count++;
    return true;
}
```

### Pattern: Rollback on Failure

```csharp
// ✅ GOOD: Rollback if later operations fail
public bool ComplexOperation()
{
    // Save original state for potential rollback
    int originalCount = _count;
    List<string> addedKeys = new List<string>();
    
    try
    {
        foreach (string key in _pendingKeys)
        {
            _keys.Add(key);
            addedKeys.Add(key);
            _count++;
            
            // If any validation fails, rollback
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
        // Rollback on any exception
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

## 🔴 Null and Empty Handling

### Consistent Null Semantics

```csharp
// ❌ BAD: Inconsistent null handling
public DynValue GetField(string name)
{
    if (name == null) return null;           // Returns null
    if (name == "") return DynValue.Nil;     // Returns Nil - inconsistent!
    return _fields.GetValueOrDefault(name);   // Could be null
}

// ✅ GOOD: Consistent - always return DynValue.Nil for "not found"
public DynValue GetField(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return DynValue.Nil;
    }
    
    if (_fields.TryGetValue(name, out DynValue value) && value != null)
    {
        return value;
    }
    
    return DynValue.Nil;
}
```

### Default Values

```csharp
// ✅ GOOD: Provide sensible defaults
public int GetIntegerOrDefault(DynValue value, int defaultValue = 0)
{
    if (value == null || value.Type != DataType.Number)
    {
        return defaultValue;
    }
    
    return (int)value.Number;
}

public string GetStringOrDefault(DynValue value, string defaultValue = "")
{
    if (value == null || value.Type != DataType.String)
    {
        return defaultValue;
    }
    
    return value.String;
}
```

______________________________________________________________________

## 🔴 API Design for Robustness

### Public API Checklist

For every public method, verify:

- [ ] All parameters are validated
- [ ] Null inputs are handled (return error/default, don't throw)
- [ ] Invalid states are detected and handled
- [ ] Return values indicate success/failure when appropriate
- [ ] Side effects are atomic (all-or-nothing)
- [ ] Exceptions are only thrown for truly exceptional cases

### Overload for Convenience and Safety

```csharp
// ✅ GOOD: Provide safe overloads
public class ScriptRunner
{
    // Primary method - full control
    public ScriptResult Execute(string code, ScriptOptions options)
    {
        if (string.IsNullOrEmpty(code))
        {
            return ScriptResult.Empty;
        }
        
        options = options ?? ScriptOptions.Default;
        
        // ... implementation
    }
    
    // Convenience overload - sensible defaults
    public ScriptResult Execute(string code)
    {
        return Execute(code, ScriptOptions.Default);
    }
    
    // Try-pattern for callers who want explicit success/failure
    public bool TryExecute(string code, out ScriptResult result)
    {
        result = Execute(code);
        return result.Success;
    }
}
```

______________________________________________________________________

## 🔴 Exception Guidelines

### When Exceptions ARE Appropriate

```csharp
// ✅ OK: Constructor with invalid required arguments
public Script(ScriptOptions options)
{
    if (options == null)
    {
        throw new ArgumentNullException(nameof(options));
    }
    // ... rest of constructor
}

// ✅ OK: Method called after disposal
public void DoString(string code)
{
    if (_disposed)
    {
        throw new ObjectDisposedException(nameof(Script));
    }
    // ...
}

// ✅ OK: Programmer error that indicates a bug
public void SetCallback(string name, CallbackFunction callback)
{
    if (callback == null)
    {
        throw new ArgumentNullException(nameof(callback));
    }
    // ...
}
```

### When Exceptions are NOT Appropriate

```csharp
// ❌ BAD: Throwing for expected conditions
public DynValue GetGlobal(string name)
{
    if (!_globals.ContainsKey(name))
    {
        throw new KeyNotFoundException($"Global '{name}' not found");
    }
    return _globals[name];
}

// ✅ GOOD: Return sentinel value for expected "not found"
public DynValue GetGlobal(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return DynValue.Nil;
    }
    
    if (_globals.TryGetValue(name, out DynValue value))
    {
        return value;
    }
    
    return DynValue.Nil;  // Not found is normal - return Nil
}
```

______________________________________________________________________

## 🔴 Debug vs Release Behavior

Use assertions for invariants that should NEVER be violated:

```csharp
public void ProcessInstruction(Instruction instruction)
{
    // Debug assertion - catches programmer errors during development
    Debug.Assert(instruction != null, "Instruction should never be null here");
    Debug.Assert(_vm != null, "VM must be initialized before processing");
    
    // Release code - handle gracefully even if assertion would fail
    if (instruction == null || _vm == null)
    {
        return;  // Graceful no-op in release
    }
    
    // ... process instruction
}
```

### Conditional Validation

```csharp
[Conditional("DEBUG")]
private void ValidateInternalState()
{
    // Expensive validation only in debug builds
    Debug.Assert(_count == _items.Count, "Count mismatch!");
    Debug.Assert(_items.All(i => i != null), "Null item in collection!");
}

public void AddItem(Item item)
{
    ValidateInternalState();  // Only runs in DEBUG
    
    // Defensive code runs in all builds
    if (item == null)
    {
        return;
    }
    
    _items.Add(item);
    _count++;
    
    ValidateInternalState();  // Only runs in DEBUG
}
```

______________________________________________________________________

## Quick Reference: Error Handling Patterns

| Situation                   | Pattern                          | Example                                |
| --------------------------- | -------------------------------- | -------------------------------------- |
| Optional value not found    | Return sentinel (Nil, default)   | `return DynValue.Nil;`                 |
| Operation may fail          | Try-pattern                      | `bool TryGet(out result)`              |
| Invalid input to public API | Return error/default             | `if (input == null) return default;`   |
| Invalid input (truly bad)   | Throw ArgumentException          | `throw new ArgumentNullException(...)` |
| Internal invariant violated | Debug.Assert + graceful handling | Assert in debug, handle in release     |
| Resource cleanup            | IDisposable + defensive Dispose  | Check \_disposed flag                  |
| Collection access           | Bounds check first               | \`if (index < 0                        |
| Type conversion             | Safe cast (as/is)                | `if (obj is MyType typed)`             |

______________________________________________________________________

## Checklist for New Code

Before submitting production code, verify:

- [ ] Every public method validates its inputs
- [ ] Null values are handled consistently (Nil for Lua values, defaults for others)
- [ ] No exceptions thrown for expected/recoverable conditions
- [ ] Try-pattern available for operations that can fail
- [ ] Internal state remains consistent after any error
- [ ] Debug assertions verify invariants
- [ ] Release builds handle all cases gracefully
- [ ] Collections are bounds-checked before access
- [ ] Casts use safe patterns (as/is, not direct cast)
- [ ] Resource cleanup is defensive (check disposed flag)

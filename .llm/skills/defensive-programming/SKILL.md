---
name: defensive-programming
description: "Write robust NovaSharp production code with explicit validation, precise errors, safe resource handling, and resilient parser/interpreter boundaries. Use for production code, public APIs, or error handling."
metadata:
  category: core
  priority: core
  related: high-performance-csharp, correctness-then-performance
---
# Skill: Defensive Programming

**Code Samples**: [defensive-patterns](../../code-samples/defensive-patterns.md)

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md), [correctness-then-performance](../correctness-then-performance/SKILL.md)

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
public LuaValue ProcessValue(LuaValue input)
{
    if (!input.IsTable)
    {
        return LuaValue.Nil;
    }

    LuaTable table = input.AsTable();
    return table.Get("key");
}
```

### 2. Try-Pattern

Make success/failure explicit:

```csharp
public bool TryGetValue(string key, out LuaValue result)
{
    if (string.IsNullOrEmpty(key))
    {
        result = LuaValue.Nil;
        return false;
    }

    return _values.TryGetValue(key, out result);
}
```

### 3. Bounds Checking

Always check before collection access:

```csharp
public LuaValue GetArgument(int index)
{
    if (_arguments == null || index < 0 || index >= _arguments.Length)
    {
        return LuaValue.Nil;
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

See [defensive-patterns](../../code-samples/defensive-patterns.md) for more examples.

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Exception Guidelines, State Management, Debug vs Release, IDisposable Pattern, and later sections.

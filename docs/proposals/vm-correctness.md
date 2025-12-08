# VM Correctness and State Protection

This document outlines the analysis of potential VM state corruption vectors and the recommended fixes to make NovaSharp's VM bulletproof while maintaining full Lua compatibility.

## Executive Summary

`DynValue` is a **mutable reference type** by necessity—Lua's closure semantics require that multiple closures can share and mutate the same upvalue. However, this mutability creates vectors for external code to corrupt internal VM state. This document identifies these vectors and proposes fixes.

**Key Principle:** Breaking public APIs is acceptable if it ensures VM correctness and Lua compatibility.

## Background: Why DynValue Must Be a Reference Type

### The Upvalue Constraint

Lua closures can capture and share mutable upvalues:

```lua
function make_counter()
    local count = 0
    return {
        inc = function() count = count + 1 end,
        get = function() return count end
    }
end

local c = make_counter()
c.inc()  -- Mutates 'count'
print(c.get())  -- Sees the mutation: prints 1
```

Both `inc` and `get` closures share the **same** `count` variable. When `inc` mutates it, `get` sees the change.

### How NovaSharp Implements This

NovaSharp stores upvalues as `DynValue` references in `ClosureContext` (a `List<DynValue>`):

```csharp
// When creating a closure, capture the SAME DynValue reference
private DynValue GetUpValueSymbol(SymbolRef s)
{
    if (s.Type == SymbolRefType.Local)
        return _executionStack.Peek().LocalScope[s.IndexValue];  // Same reference
    else if (s.Type == SymbolRefType.UpValue)
        return _executionStack.Peek().ClosureScope[s.IndexValue];
}
```

Multiple closures receive the **same `DynValue` reference**. Mutation via `Assign()` is visible to all holders.

### Why Structs Won't Work

If `DynValue` were a struct:

- Each closure would get an **independent copy**
- `slot.Assign(value)` would modify only the local copy
- Lua closure semantics would be completely broken

### DynValue Fields

```csharp
public sealed class DynValue
{
    private int _refId = ++RefIdCounter;  // Unique reference ID
    private int _hashCode = -1;           // Cached hash code
    private bool _readOnly;               // Immutability flag
    private LuaNumber _number;            // Numeric value storage
    private object _object;               // Reference storage (strings, tables, closures, etc.)
    private DataType _type;               // Discriminant tag
}
```

The `Assign()` method mutates `_number`, `_object`, `_type`, and resets `_hashCode`.

______________________________________________________________________

## Identified Corruption Vectors

### 1. `DynValue.Assign()` is Public

**Current State:** Any external code can mutate any `DynValue`:

```csharp
DynValue val = script.Call(someFunction);
val.Assign(DynValue.NewNumber(999));  // Could corrupt internal VM state
```

**Risk Level:** HIGH

**Impact:** If the returned `DynValue` is actually an internal reference (e.g., from a local slot or upvalue), external code corrupts VM state.

**Fix:** Make `Assign()` internal. The VM and CoreLib have internal access; user code should not.

```csharp
internal void Assign(DynValue value)  // Changed from public
{
    // ...
}
```

### 2. `Closure.GetUpValue()` Returns Mutable Internal State

**Current State:**

```csharp
public DynValue GetUpValue(int idx)
{
    return ClosureContext[idx];  // Returns MUTABLE internal reference!
}
```

The doc comment even encourages mutation:

> "Gets the value of an upvalue. To set the value, use GetUpValue(idx).Assign(...)"

**Risk Level:** HIGH

**Impact:** External code can corrupt closure upvalues:

```csharp
Closure closure = someFunction.Function;
DynValue upval = closure.GetUpValue(0);
upval.Assign(DynValue.NewNumber(999));  // Corrupts closure state!
```

**Fix:** Return readonly copy and add explicit setter:

```csharp
public DynValue GetUpValue(int idx)
{
    return ClosureContext[idx]?.AsReadOnly() ?? DynValue.Nil;
}

public void SetUpValue(int idx, DynValue value)
{
    if (idx < 0 || idx >= ClosureContext.Count)
        throw new ArgumentOutOfRangeException(nameof(idx));
    
    DynValue slot = ClosureContext[idx];
    if (slot == null)
    {
        ClosureContext[idx] = value?.Clone() ?? DynValue.NewNil();
    }
    else
    {
        slot.Assign(value ?? DynValue.Nil);
    }
}
```

### 3. Table Keys Can Be Mutated After Insertion

**Current State:**

```csharp
public void Set(DynValue key, DynValue value)
{
    // ... validation ...
    PerformTableSet(_valueMap, key, key, value, false, -1);  // Uses key directly!
}
```

**Risk Level:** MEDIUM

**Impact:** If a `DynValue` used as a table key is later mutated, the hash table becomes corrupted:

```csharp
DynValue key = DynValue.NewTable(new Table(script));
table.Set(key, someValue);
// Later...
key.Assign(DynValue.NewNumber(1));  // Hash changes! Table corrupted!
```

**Fix:** Clone keys as readonly before storing in `_valueMap`:

```csharp
public void Set(DynValue key, DynValue value)
{
    // ... existing validation ...
    
    // Ensure key stability for _valueMap
    DynValue stableKey = key.ReadOnly ? key : key.AsReadOnly();
    
    PerformTableSet(_valueMap, stableKey, stableKey, value, false, -1);
}
```

### 4. UserData and Thread Hash Code Collisions

**Current State:**

```csharp
case DataType.UserData:
case DataType.Thread:
default:
    _hashCode = 999;  // All collide!
    break;
```

**Risk Level:** LOW (performance, not correctness)

**Impact:** All UserData and Thread values used as table keys collide in the hash table, causing O(n) lookup.

**Fix:** Use proper hash codes:

```csharp
case DataType.UserData:
    if (UserData?.Object != null)
    {
        hash.AddInt(UserData.Object.GetHashCode());
    }
    _hashCode = hash.ToHashCode();
    break;
case DataType.Thread:
    if (Coroutine != null)
    {
        hash.AddInt(Coroutine.ReferenceId);
    }
    _hashCode = hash.ToHashCode();
    break;
```

______________________________________________________________________

## Already Safe (No Changes Needed)

### VM Value Stack Operations

The VM already protects locals and upvalues when pushing to the value stack:

```csharp
case OpCode.Local:
    DynValue[] scope = _executionStack.Peek().LocalScope;
    int index = i.Symbol.IndexValue;
    _valueStack.Push(scope[index].AsReadOnly());  // ✓ Already readonly!
    break;
case OpCode.UpValue:
    _valueStack.Push(
        _executionStack.Peek().ClosureScope[i.Symbol.IndexValue].AsReadOnly()
    );  // ✓ Already readonly!
    break;
```

### debug.setupvalue / debug.setlocal

These Lua standard library functions use `Assign()` internally:

```csharp
closure[index].Assign(args[2]);  // debug.setupvalue
slot.Assign(newValue);            // debug.setlocal
```

Since `CoreLib/DebugModule.cs` is internal to the runtime, making `Assign()` internal doesn't break these. Lua scripts calling `debug.setupvalue(f, 1, newval)` work correctly.

### debug.upvaluejoin

```csharp
c2.ClosureContext[n2] = c1.ClosureContext[n1];  // Share the same DynValue
```

This is correct—`debug.upvaluejoin` explicitly makes two closures share an upvalue. This is core Lua functionality.

### Table Value Mutations

`Table.Get()` returns the stored `DynValue` directly. This is correct Lua semantics—tables store references:

```lua
t = {x = {}}
y = t.x
y.foo = 1  -- Mutates t.x.foo (correct!)
```

Mutating the **contents** of a value (e.g., adding a field to a table) is correct. The issue is mutating the **DynValue wrapper** via `Assign()`.

### Return Values from Script.Call()

Return values come from `_valueStack.Pop()`. Since the VM pushes readonly copies of locals/upvalues, return values are generally safe. However, values constructed inline (literals, new tables) are not readonly—but mutating these doesn't corrupt VM state since they're not shared.

______________________________________________________________________

## Implementation Plan

### Phase 1: Core Safety (High Priority)

1. **Make `DynValue.Assign()` internal**

   - Change `public void Assign(DynValue value)` to `internal`
   - Update XML doc to reflect internal-only usage
   - Audit and update any tests that use `Assign()` directly

1. **Fix `Closure.GetUpValue()`**

   - Return `.AsReadOnly()` instead of raw reference
   - Add `SetUpValue(int idx, DynValue value)` method
   - Update doc comment to reflect new pattern

1. **Fix Table key safety**

   - In `Table.Set(DynValue key, DynValue value)`, call `key.AsReadOnly()` for non-primitive keys going into `_valueMap`

### Phase 2: Performance and Completeness

4. **Fix UserData/Thread hash codes**

   - Implement proper hash codes to eliminate collisions

1. **Audit all public APIs returning DynValue**

   - `Script.Call()` and overloads
   - `Script.DoString()`, `Script.DoFile()`, etc.
   - `Table.Get()` (correct as-is, but document behavior)
   - `CallbackArguments` indexer
   - Any other public APIs

### Phase 3: Documentation and Testing

6. **Document the DynValue contract**

   - Internal DynValues may be mutable and shared
   - External DynValues (from public APIs) should be treated as readonly
   - Using mutable DynValues as table keys is undefined behavior

1. **Add regression tests**

   - Test that `Assign()` is not accessible from external assemblies
   - Test that `GetUpValue()` returns readonly values
   - Test that table key mutation doesn't corrupt lookups
   - Test `debug.setupvalue` and `debug.setlocal` still work

______________________________________________________________________

## API Breaking Changes

### Removed Public API

| API                         | Replacement                                    |
| --------------------------- | ---------------------------------------------- |
| `DynValue.Assign(DynValue)` | Use `DynValue.Clone()` and variable assignment |

### Changed API Behavior

| API                       | Old Behavior                       | New Behavior          |
| ------------------------- | ---------------------------------- | --------------------- |
| `Closure.GetUpValue(int)` | Returns mutable internal reference | Returns readonly copy |

### New API

| API                                 | Purpose                   |
| ----------------------------------- | ------------------------- |
| `Closure.SetUpValue(int, DynValue)` | Explicit upvalue mutation |

______________________________________________________________________

## Lua Compatibility Verification

All changes preserve Lua semantics:

| Lua Feature               | NovaSharp Behavior                               | Status                    |
| ------------------------- | ------------------------------------------------ | ------------------------- |
| Closure upvalue sharing   | Multiple closures see mutations                  | ✓ Preserved               |
| `debug.setupvalue`        | Can modify upvalues                              | ✓ Works (internal access) |
| `debug.setlocal`          | Can modify locals                                | ✓ Works (internal access) |
| `debug.upvaluejoin`       | Makes closures share upvalues                    | ✓ Works                   |
| Table reference semantics | `t.x = {}; y = t.x; y.foo = 1` mutates `t.x`     | ✓ Correct                 |
| Table key identity        | `t[obj] = 1` uses reference identity for objects | ✓ Correct                 |

______________________________________________________________________

## Risk Assessment

| Change                  | Risk     | Mitigation                                       |
| ----------------------- | -------- | ------------------------------------------------ |
| `Assign()` internal     | Low      | Only VM internals need it; CoreLib has access    |
| `GetUpValue()` readonly | Low      | Add `SetUpValue()` for legitimate use cases      |
| Table key readonly      | Very Low | Keys shouldn't be mutated after insertion anyway |
| Hash code fix           | None     | Pure improvement, no semantic change             |

______________________________________________________________________

## Future Considerations

### Thread Safety

The current analysis focuses on single-threaded corruption. Multi-threaded access to `Script` instances is explicitly not supported, but this should be documented clearly.

### Sandboxing

These changes complement the existing `SandboxOptions` by preventing a different class of abuse—not resource exhaustion, but state corruption.

### AOT/IL2CPP Compatibility

All proposed changes use standard C# features and should work correctly under AOT compilation.

# VM Correctness and State Protection

> Status: historical design note updated on 2026-07-05. The core safety work
> described here has mostly landed: `Assign(...)` is now the internal
> `AssignSlot(...)` whole-slot helper, `DynValue.ReferenceId` no longer has a
> backing field, and the mutable hash-code cache has been removed. The current
> A1 roadmap will replace this wrapper model with a slot/value split, where
> shared closure slots remain reference-backed and Lua values become structs.

This document outlines the analysis of potential VM state corruption vectors and the recommended fixes to make NovaSharp's VM bulletproof while maintaining full Lua compatibility.

## Executive Summary

`DynValue` is currently a **mutable reference type** because Lua's closure semantics require that multiple closures can share and mutate the same upvalue slot. However, this mutability creates vectors for external code to corrupt internal VM state. This document identifies these vectors and the fixes that led to the current internal slot-mutation boundary.

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

Multiple closures receive the **same `DynValue` reference**. Whole-slot mutation via `AssignSlot(...)` and numeric-slot mutation via `AssignNumber(...)` are visible to all holders.

### Why Struct Values Need a Slot Split

If `DynValue` were replaced by a struct without introducing reference-backed slot cells:

- Each closure would get an **independent copy**
- `slot = value` would modify only the local copy
- Lua closure semantics would be completely broken

This is why the current A1 roadmap splits shared slots from value payloads instead of directly replacing every `DynValue` reference with an unboxed struct copy.

### DynValue Fields

```csharp
public sealed class DynValue
{
    private bool _readOnly;               // Immutability flag
    private LuaNumber _number;            // Numeric value storage
    private object _object;               // Reference storage (strings, tables, closures, etc.)
    private DataType _type;               // Discriminant tag
}
```

`AssignSlot(...)` mutates `_number`, `_object`, and `_type`. `AssignNumber(...)` mutates `_number` for numeric slots on the `ExecIncr` hot path. Hash codes are computed from current value state rather than cached in a mutable field.

______________________________________________________________________

## Identified Corruption Vectors

### 1. Prior Public `DynValue.Assign()` External Mutation

**Previous State:** Any external code could mutate any `DynValue`:

```csharp
DynValue val = script.Call(someFunction);
val.Assign(DynValue.NewNumber(999));  // Old public API; could corrupt internal VM state
```

**Risk Level:** HIGH

**Impact:** If the returned `DynValue` is actually an internal reference (e.g., from a local slot or upvalue), external code corrupts VM state.

**Fix:** Make whole-slot mutation internal. The VM and CoreLib have internal access; user code should not.

```csharp
internal void AssignSlot(DynValue value)  // Changed from public Assign(...)
{
    // ...
}
```

### 2. Prior `Closure.GetUpValue()` Mutable Internal State

**Previous State:**

```csharp
public DynValue GetUpValue(int idx)
{
    return ClosureContext[idx];  // Returns MUTABLE internal reference!
}
```

The old doc comment even encouraged mutation:

> "Gets the value of an upvalue. To set the value, use GetUpValue(idx).Assign(...)"

**Risk Level:** HIGH

**Impact:** External code can corrupt closure upvalues:

```csharp
Closure closure = someFunction.Function;
DynValue upval = closure.GetUpValue(0);
upval.Assign(DynValue.NewNumber(999));  // Old public API; corrupts closure state
```

**Current State:** `GetUpValue(...)` returns a readonly copy and `SetUpValue(...)` performs explicit internal slot mutation:

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
        slot.AssignSlot(value ?? DynValue.Nil);
    }
}
```

### 3. Prior Table-Key Mutation After Insertion

**Previous State:**

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
key.Assign(DynValue.NewNumber(1));  // Old public API; changes hash
```

**Current State:** table keys are cloned as readonly before storing in `_valueMap`:

```csharp
public void Set(DynValue key, DynValue value)
{
    // ... existing validation ...
    
    // Ensure key stability for _valueMap
    DynValue stableKey = key.ReadOnly ? key : key.AsReadOnly();
    
    PerformTableSet(_valueMap, stableKey, stableKey, value, false, -1);
}
```

### 4. Prior UserData and Thread Hash Code Collisions

**Previous State:**

```csharp
case DataType.UserData:
case DataType.Thread:
default:
    return 999;  // All collide!
    break;
```

**Risk Level:** LOW (performance, not correctness)

**Impact:** All UserData and Thread values used as table keys collide in the hash table, causing O(n) lookup.

**Current State:** hash codes are computed from current value state without a mutable cache:

```csharp
case DataType.UserData:
    if (UserData != null)
    {
        hash.AddInt(UserData.StableHashCode);
    }
    return hash.ToHashCode();
case DataType.Thread:
    if (Coroutine != null)
    {
        hash.AddInt(Coroutine.ReferenceId);
    }
    return hash.ToHashCode();
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

These Lua standard library functions use `AssignSlot(...)` internally:

```csharp
closure[index].AssignSlot(args[2]);  // debug.setupvalue
slot.AssignSlot(newValue);           // debug.setlocal
```

Since `CoreLib/DebugModule.cs` is internal to the runtime, making whole-slot mutation internal doesn't break these. Lua scripts calling `debug.setupvalue(f, 1, newval)` work correctly.

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

Mutating the **contents** of a value (e.g., adding a field to a table) is correct. The issue is mutating the **DynValue wrapper** via `AssignSlot(...)`.

### Return Values from Script.Call()

Return values come from `_valueStack.Pop()`. Since the VM pushes readonly copies of locals/upvalues, return values are generally safe. However, values constructed inline (literals, new tables) are not readonly—but mutating these doesn't corrupt VM state since they're not shared.

______________________________________________________________________

## Implementation Plan

### Phase 1: Core Safety (High Priority)

1. **Make whole-slot `DynValue` mutation internal**

   - Replace public `Assign(DynValue value)` with internal `AssignSlot(DynValue value)`
   - Update XML docs to reflect internal slot-only usage
   - Audit and update any tests that use whole-slot mutation directly

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

   - Test that `AssignSlot()` is not accessible from external assemblies
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
| `AssignSlot()` internal | Low      | Only VM internals need it; CoreLib has access    |
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

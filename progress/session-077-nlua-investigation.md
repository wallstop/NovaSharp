# Session 077: NLua Architecture Investigation

**Date**: 2025-12-21\
**Initiative**: 20 - NLua Architecture Investigation\
**Status**: ✅ Complete

## Overview

Investigated the NLua project (https://github.com/NLua/NLua) for architecture insights, performance patterns, and optimization techniques that could be adopted in NovaSharp.

**Context**: NLua is a Lua/.NET bridge that wraps the native Lua C library via P/Invoke, while NovaSharp is a pure C# Lua interpreter. Despite the different approaches, NLua offers valuable insights for type marshaling, caching, and interop optimization.

## Key Findings

### 1. Type Conversion System (ObjectTranslator)

NLua uses a `Dictionary<Type, ExtractValue>` for caching type-specific extraction delegates, avoiding runtime reflection overhead.

**Pattern**:

```csharp
private readonly Dictionary<Type, ExtractValue> _extractValues = new();

// Lazy initialization with caching
public ExtractValue GetExtractor(Type type)
{
    if (!_extractValues.TryGetValue(type, out var extractor))
    {
        extractor = CreateExtractor(type);
        _extractValues[type] = extractor;
    }
    return extractor;
}
```

**Applicability to NovaSharp**: ⭐⭐⭐⭐⭐ HIGH

- NovaSharp's `Interop/Converters/` system could benefit from similar delegate caching
- Currently type conversion may involve repeated reflection lookups
- **Action**: Audit `StandardDescriptorsTable` and converter registration for caching opportunities

### 2. Member Caching Pattern

NLua uses a two-level nested dictionary `Type → MemberName → MemberInfo/Delegate` for caching reflection results.

**Pattern**:

```csharp
private readonly Dictionary<Type, Dictionary<string, MethodInfo>> _methodCache = new();
private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyCache = new();
```

**Applicability to NovaSharp**: ⭐⭐⭐⭐ HIGH

- NovaSharp's `MemberDescriptor` system already caches member info
- However, method overload resolution may be repeated unnecessarily
- **Action**: Review `OverloadedMethodMemberDescriptor` for resolution caching

### 3. ReferenceComparer for Identity Hashing

NLua uses `RuntimeHelpers.GetHashCode()` for identity-based hashing, correctly handling boxed value types.

**Pattern**:

```csharp
public class ReferenceComparer : IEqualityComparer<object>
{
    public new bool Equals(object x, object y) => object.ReferenceEquals(x, y);
    public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
}
```

**Applicability to NovaSharp**: ⭐⭐⭐ MEDIUM

- Useful for object identity tracking (userdata proxies, reference tables)
- May already be handled by NovaSharp's existing patterns
- **Action**: Verify `UserData` identity handling uses proper reference equality

### 4. Deferred GC Cleanup

NLua queues finalized references for cleanup on the next main-thread operation using `ConcurrentQueue<int>`.

**Pattern**:

```csharp
private readonly ConcurrentQueue<int> _toDispose = new();

// Called from finalizer (background thread)
internal void QueueDispose(int reference) => _toDispose.Enqueue(reference);

// Called from main Lua operations
private void ProcessDisposalQueue()
{
    while (_toDispose.TryDequeue(out var reference))
        lua_unref(L, reference);
}
```

**Applicability to NovaSharp**: ⭐⭐⭐ MEDIUM

- NovaSharp doesn't have P/Invoke reference handles, but the pattern applies to:
  - Cleaning up `__gc` metamethod queues
  - Deferred resource cleanup for coroutines
- **Action**: Consider for future resource management enhancements

### 5. MethodCache Last-Called Optimization

NLua caches the last-called method binding, skipping overload resolution when argument count matches and there's only one overload.

**Pattern**:

```csharp
private MethodBase _lastCalledMethod;
private int _lastArgCount;

public object CallMethod(object[] args)
{
    if (_lastCalledMethod != null && 
        args.Length == _lastArgCount && 
        Methods.Length == 1)
    {
        // Fast path: reuse last binding
        return _lastCalledMethod.Invoke(target, args);
    }
    // Slow path: full overload resolution
    _lastCalledMethod = ResolveOverload(args);
    _lastArgCount = args.Length;
    return _lastCalledMethod.Invoke(target, args);
}
```

**Applicability to NovaSharp**: ⭐⭐⭐⭐⭐ HIGH

- NovaSharp's `OverloadedMethodMemberDescriptor` performs resolution on every call
- Caching the last-resolved method would eliminate repeated work for common patterns
- **Action**: Implement last-call caching in `OverloadedMethodMemberDescriptor`

### 6. Static Delegate Pre-allocation

NLua pre-allocates metamethod delegates as static readonly fields to avoid per-call allocation.

**Pattern**:

```csharp
private static readonly LuaCSFunction _gcFunction = new LuaCSFunction(GcCallback);
private static readonly LuaCSFunction _indexFunction = new LuaCSFunction(IndexCallback);
private static readonly LuaCSFunction _newindexFunction = new LuaCSFunction(NewIndexCallback);
private static readonly LuaCSFunction _tostringFunction = new LuaCSFunction(ToStringCallback);
```

**Applicability to NovaSharp**: ⭐⭐⭐⭐⭐ HIGH (Already Partially Done)

- Session 065 already migrated MathModule and Bit32Module to static delegates
- More modules may have similar opportunities
- **Action**: Continue static delegate migration in remaining CoreLib modules

### 7. Fakenil Sentinel Pattern

NLua uses a sentinel value to distinguish "cached nil" from "not yet cached" in Lua-side caching.

**Pattern**:

```csharp
// In Lua side:
local cache = {}
local FAKENIL = {}  -- unique sentinel

function getCached(key)
    local val = cache[key]
    if val == FAKENIL then return nil end
    if val ~= nil then return val end
    -- Not cached, compute and cache
    val = compute(key)
    cache[key] = val or FAKENIL  -- Cache nil as FAKENIL
    return val
end
```

**Applicability to NovaSharp**: ⭐⭐⭐ MEDIUM

- Useful pattern for caching nullable results in interop scenarios
- NovaSharp could use `DynValue.Void` or a similar sentinel
- **Action**: Document pattern for future interop caching needs

## Patterns NOT Applicable to NovaSharp

### IL Emit Code Generation

NLua generates delegate/proxy types at runtime using `System.Reflection.Emit`. NovaSharp has first-class Lua functions and doesn't need runtime code generation for basic Lua execution.

### P/Invoke Patterns

NLua's `IntPtr` handles, `MonoPInvokeCallback` attributes, and native memory management are specific to wrapping the C Lua library. Not applicable to a pure C# interpreter.

### Lua Registry Management

NLua manages Lua references via the Lua registry API (`luaL_ref`/`luaL_unref`). NovaSharp manages Lua values directly in C# without a native registry.

## Actionable Optimizations for NovaSharp

| Priority | Optimization                                            | Effort   | Expected Impact                      |
| -------- | ------------------------------------------------------- | -------- | ------------------------------------ |
| P1       | Last-call caching in `OverloadedMethodMemberDescriptor` | 2-3 days | 20-40% faster repeated interop calls |
| P1       | Audit type converter delegate caching                   | 1-2 days | Variable, depends on current state   |
| P2       | Continue static delegate migration in CoreLib           | 3-5 days | Eliminates delegate allocations      |
| P2       | Reference equality comparer for userdata identity       | 1 day    | Correctness + minor perf             |
| P3       | Fakenil sentinel for interop caching                    | 0.5 days | Design pattern documentation         |
| P3       | Deferred cleanup queue pattern                          | 1-2 days | Future resource management           |

## Recommendations

### Immediate Actions (This Week)

1. **P1: Implement last-call caching** in `OverloadedMethodMemberDescriptor`

   - Cache the last-resolved `MethodBase` and argument signature
   - Skip resolution on repeated calls with matching signature
   - Expected 20-40% speedup for tight interop loops

1. **P1: Audit type converter registration**

   - Review `StandardDescriptorsTable.cs` and `DescriptorUserDataFactory.cs`
   - Ensure type → converter mapping is cached, not recomputed

### Short-Term Actions (Next Sprint)

3. **P2: Continue static delegate migration**
   - Audit remaining CoreLib modules for delegate allocations
   - Migrate any remaining lambda captures to static delegates
   - Modules to review: TableModule (partially done), StringModule, OsModule

### Future Considerations

4. **P3: Document patterns** for future interop work
   - Add fakenil sentinel pattern to `.llm/skills/clr-interop.md`
   - Consider deferred cleanup for `__gc` implementation

## Conclusion

The NLua investigation revealed several valuable patterns, most notably the **last-call caching** optimization for method overload resolution, which can significantly improve repeated interop calls. NovaSharp has already implemented some of these patterns (static delegates in Session 065, member descriptor caching), but there are clear opportunities for further optimization.

The investigation confirms that NovaSharp's pure C# approach is sound—the patterns that don't apply (IL emit, P/Invoke, registry management) are all workarounds for native interop that NovaSharp doesn't need.

**Next Steps**:

1. Create Initiative 21 for "Last-Call Caching in OverloadedMethodMemberDescriptor"
1. Add P1 items to Initiative 12 (Allocation Analysis) backlog
1. Update `.llm/skills/clr-interop.md` with discovered patterns

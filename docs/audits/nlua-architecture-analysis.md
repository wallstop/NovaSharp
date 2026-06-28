# NLua Architecture Analysis for NovaSharp

**Analysis Date:** 2025\
**Target Repository:** [NLua/NLua](https://github.com/NLua/NLua)\
**Purpose:** Identify architecture insights, performance patterns, and optimization techniques applicable to NovaSharp

______________________________________________________________________

## Executive Summary

NLua is a Lua/.NET bridge that wraps the native Lua C library via P/Invoke (KeraLua). While NovaSharp is a **pure C# Lua interpreter**, many of NLua's patterns for type conversion, caching, memory management, and method dispatch are directly applicable or can be adapted.

### Key Findings

| Category        | Pattern                                   | Applicability to NovaSharp   | Effort |
| --------------- | ----------------------------------------- | ---------------------------- | ------ |
| Type Conversion | Dictionary-based extractor delegates      | ✅ High                      | Medium |
| Object Tracking | Bidirectional maps with ReferenceComparer | ✅ High (for userdata)       | Low    |
| Member Caching  | Nested dictionary keyed by (Type, name)   | ✅ High                      | Low    |
| Method Dispatch | Cached LuaNativeFunction delegates        | ⚠️ Medium (adapt to pure C#) | Medium |
| GC Coordination | ConcurrentQueue for finalized refs        | ✅ High                      | Low    |
| Code Generation | IL emit for delegates/proxies             | ❌ Not applicable            | N/A    |

______________________________________________________________________

## 1. Type Conversion System (ObjectTranslator + CheckType)

### 1.1 Architecture Overview

NLua's type conversion is centered around two classes:

- **ObjectTranslator** - Core marshaling between CLR and Lua values
- **CheckType** - Type extraction delegate management

### 1.2 Key Pattern: Extractor Delegates Dictionary

```csharp
// From CheckType.cs
private readonly Dictionary<Type, ExtractValue> _extractValues;

// Delegate signature
delegate object ExtractValue(LuaState luaState, int stackPos);

// Constructor initializes extractors for common types
public CheckType(ObjectTranslator translator)
{
    _extractValues = new Dictionary<Type, ExtractValue>();
    _extractValues.Add(typeof(object), GetAsObject);
    _extractValues.Add(typeof(sbyte), GetAsSbyte);
    _extractValues.Add(typeof(byte), GetAsByte);
    _extractValues.Add(typeof(short), GetAsShort);
    _extractValues.Add(typeof(ushort), GetAsUshort);
    _extractValues.Add(typeof(int), GetAsInt);
    _extractValues.Add(typeof(uint), GetAsUint);
    _extractValues.Add(typeof(long), GetAsLong);
    _extractValues.Add(typeof(ulong), GetAsUlong);
    _extractValues.Add(typeof(double), GetAsDouble);
    _extractValues.Add(typeof(char), GetAsChar);
    _extractValues.Add(typeof(float), GetAsFloat);
    _extractValues.Add(typeof(decimal), GetAsDecimal);
    _extractValues.Add(typeof(bool), GetAsBoolean);
    _extractValues.Add(typeof(string), GetAsString);
    _extractValues.Add(typeof(char[]), GetAsCharArray);
    _extractValues.Add(typeof(byte[]), GetAsByteArray);
    _extractValues.Add(typeof(LuaFunction), GetAsFunction);
    _extractValues.Add(typeof(LuaTable), GetAsTable);
    _extractValues.Add(typeof(LuaThread), GetAsThread);
    _extractValues.Add(typeof(LuaUserData), GetAsUserdata);
}
```

**NovaSharp Application:** For userdata/CLR interop layer, use a similar delegate dictionary pattern. Avoid reflection at runtime by pre-caching type converters.

### 1.3 Key Pattern: Type-Switch Push Method

```csharp
// From ObjectTranslator.cs - Pattern-matched type pushing
internal void Push(LuaState luaState, object o)
{
    switch (o)
    {
        case null:
            luaState.PushNil();
            break;
        case sbyte sb:
            luaState.PushInteger(sb);
            break;
        case byte b:
            luaState.PushInteger(b);
            break;
        case short s:
            luaState.PushInteger(s);
            break;
        case int i:
            luaState.PushInteger(i);
            break;
        case long l:
            luaState.PushInteger(l);
            break;
        case double d:
            luaState.PushNumber(d);
            break;
        case string str:
            luaState.PushString(str);
            break;
        case bool boolean:
            luaState.PushBoolean(boolean);
            break;
        // ... more cases
        default:
            PushObject(luaState, o);
            break;
    }
}
```

**NovaSharp Application:** Use C# 8+ pattern matching for type dispatch. This is cleaner and often faster than `if-else` chains or `is` checks.

### 1.4 Key Pattern: ReferenceComparer for Value Type Boxing

```csharp
// From ObjectTranslator.cs
class ReferenceComparer : IEqualityComparer<object>
{
    public new bool Equals(object x, object y)
    {
        if (x != null && x.GetType().IsValueType && y != null && y.GetType().IsValueType)
            return x.Equals(y);
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return RuntimeHelpers.GetHashCode(obj);
    }
}

// Used for backmap tracking
Dictionary<object, int> _objectsBackMap = new Dictionary<object, int>(new ReferenceComparer());
```

**NovaSharp Application:** When tracking userdata objects in a registry, use `RuntimeHelpers.GetHashCode()` for identity hashing to avoid issues with boxed value types.

______________________________________________________________________

## 2. Memory Management & Caching

### 2.1 Object Tracking: Bidirectional Maps

```csharp
// From ObjectTranslator.cs
private readonly Dictionary<int, object> _objects = new Dictionary<int, object>();
private readonly Dictionary<object, int> _objectsBackMap;

internal void PushObject(LuaState luaState, object o)
{
    // Check if object already has a reference
    if (_objectsBackMap.TryGetValue(o, out int index))
    {
        // Reuse existing reference
        luaState.RawGetInteger(LuaRegistry.Index, index);
        return;
    }
    
    // Create new reference
    index = AddObject(o);
    // ... push new userdata
}
```

**NovaSharp Application:** For any CLR interop, maintain bidirectional maps to prevent duplicate wrapping of the same object.

### 2.2 Deferred GC Cleanup with ConcurrentQueue

```csharp
// From ObjectTranslator.cs
private readonly ConcurrentQueue<int> _finalizedReferences = new ConcurrentQueue<int>();

internal void AddFinalizedReference(int reference)
{
    _finalizedReferences.Enqueue(reference);
}

internal void CleanFinalizedReferences()
{
    while (_finalizedReferences.TryDequeue(out int reference))
    {
        // Clean up reference
        CollectObject(reference);
    }
}
```

**Insight:** Finalizers can't safely interact with the Lua state. NLua queues references for cleanup on the next main-thread operation.

**NovaSharp Application:** If implementing userdata finalizers, use a similar pattern—queue cleanup work rather than doing it in the finalizer.

### 2.3 Weak Reference for Interpreter Access

```csharp
// From LuaBase.cs
class LuaBase : IDisposable
{
    protected readonly int _Reference;
    private readonly WeakReference<Lua> _lua;

    internal virtual void DisposeLuaReference(bool disposeManagedResources)
    {
        if (!_lua.TryGetTarget(out Lua lua))
            return;
            
        if (disposeManagedResources)
            lua.DisposeInternal(_Reference, finalized: false);
        else
            lua.DisposeInternal(_Reference, finalized: true);
    }
}
```

**NovaSharp Application:** Lua object wrappers should hold weak references to the interpreter to allow GC cleanup even if circular references exist.

### 2.4 Lua-Side Member Caching

```csharp
// From Metatables.cs - Minified Lua code for cached __index
public const string LuaIndexFunction = @"
    local function index(obj, name, rawget, cache, fakenil)
        local value = rawget(cache, name)
        if value ~= nil then
            if value == fakenil then return nil end
            return value
        end
        -- Call C# to get member
        local luanet_indexer = rawget(obj, 'LuaNet_Indexer')
        local value, isFunc = luanet_indexer(obj, name)
        if not isFunc then
            rawset(cache, name, value or fakenil)
        end
        return value
    end
";
```

**Key Pattern:** Use a `fakenil` sentinel value to distinguish "cached nil" from "not yet cached".

**NovaSharp Application:** For metamethod implementations that need caching, the fakenil pattern is elegant and avoids repeated lookups.

______________________________________________________________________

## 3. Member Caching with Nested Dictionaries

### 3.1 Two-Level Cache Structure

```csharp
// From Metatables.cs
private readonly Dictionary<object, Dictionary<object, object>> _memberCache 
    = new Dictionary<object, Dictionary<object, object>>();

object CheckMemberCache(ProxyType objType, string memberName)
{
    if (!_memberCache.TryGetValue(objType, out var typeCache))
        return null;
    
    typeCache.TryGetValue(memberName, out var member);
    return member;
}

void SetMemberCache(ProxyType objType, string memberName, object member)
{
    if (!_memberCache.TryGetValue(objType, out var typeCache))
    {
        typeCache = new Dictionary<object, object>();
        _memberCache[objType] = typeCache;
    }
    typeCache[memberName] = member;
}
```

**NovaSharp Application:** For any reflection-based member access, cache results in a two-level dictionary: `Type → MemberName → MemberInfo/Delegate`.

______________________________________________________________________

## 4. Function Call Dispatch (LuaMethodWrapper)

### 4.1 Method Wrapper Structure

```csharp
// From LuaMethodWrapper.cs
class LuaMethodWrapper
{
    internal LuaNativeFunction InvokeFunction;
    
    readonly ObjectTranslator _translator;
    readonly MethodBase _method;
    readonly ExtractValue _extractTarget;
    readonly object _target;
    readonly bool _isStatic;
    readonly string _methodName;
    readonly MethodInfo[] _members;
    
    private MethodCache _lastCalledMethod;
    
    public LuaMethodWrapper(ObjectTranslator translator, object target, 
                           ProxyType targetType, MethodBase method)
    {
        InvokeFunction = Call;  // Assign method as delegate
        _translator = translator;
        _target = target;
        _extractTarget = translator.typeChecker.GetExtractor(targetType);
        _lastCalledMethod = new MethodCache();
        _method = method;
        _methodName = method.Name;
        _isStatic = method.IsStatic;
    }
}
```

### 4.2 MethodCache for Hot Path Optimization

```csharp
// From MethodCache.cs
class MethodCache
{
    private MethodBase _cachedMethod;
    public bool IsReturnVoid;
    public object[] args;
    public int[] outList;
    public MethodArgs[] argTypes;
    
    public MethodBase cachedMethod
    {
        get => _cachedMethod;
        set
        {
            _cachedMethod = value;
            var mi = value as MethodInfo;
            if (mi != null)
                IsReturnVoid = mi.ReturnType == typeof(void);
        }
    }
}
```

### 4.3 Fast Path Detection

```csharp
// From LuaMethodWrapper.cs
bool IsMethodCached(LuaState luaState, int numArgsPassed, int skipParams)
{
    if (_lastCalledMethod.cachedMethod == null)
        return false;

    if (numArgsPassed != _lastCalledMethod.argTypes.Length)
        return false;

    // If single overload, always use cached
    if (_members.Length == 1)
        return true;

    // Otherwise validate parameter types match
    return _translator.MatchParameters(luaState, _lastCalledMethod.cachedMethod, 
                                        _lastCalledMethod, skipParams);
}
```

**Key Insight:** When there's only one method overload, skip parameter type checking entirely.

**NovaSharp Application:** For registered C# functions, cache the last successful method binding. If argument count matches and there's no overloading, skip reflection.

______________________________________________________________________

## 5. Error Handling & Exception Propagation

### 5.1 LuaScriptException Hierarchy

```csharp
// From LuaException.cs
public class LuaException : Exception
{
    public LuaException(string message) : base(message) { }
    public LuaException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// From LuaScriptException.cs
public class LuaScriptException : LuaException
{
    public bool IsNetException { get; }
    private readonly string _source;
    public override string Source => _source;

    // For Lua-only errors
    public LuaScriptException(string message, string source) : base(message)
    {
        _source = source;
    }

    // For wrapped .NET exceptions
    public LuaScriptException(Exception innerException, string source)
        : base("A .NET exception occurred in user-code", innerException)
    {
        _source = source;
        IsNetException = true;
    }
}
```

### 5.2 Pending Exception Pattern

```csharp
// From LuaMethodWrapper.cs
int SetPendingException(Exception e)
{
    return _translator.Interpreter?.SetPendingException(e) ?? 0;
}

// From Lua.cs
internal int SetPendingException(Exception e)
{
    if (e == null)
        return 0;

    _translator.ThrowError(_luaState, e);
    return 1;
}
```

### 5.3 Error Wrapping with Location Info

```csharp
// From ObjectTranslator.cs
internal void ThrowError(LuaState luaState, object e)
{
    int oldTop = luaState.GetTop();
    luaState.Where(1);  // Get stack frame info
    var curlev = PopValues(luaState, oldTop);

    string errLocation = curlev.Length > 0 ? curlev[0].ToString() : "";

    if (e is string message)
    {
        if (Interpreter?.UseTraceback == true)
            message += Environment.NewLine + Interpreter.GetDebugTraceback();
        e = new LuaScriptException(message, errLocation);
    }
    else if (e is Exception ex)
    {
        if (Interpreter?.UseTraceback == true)
            ex.Data["Traceback"] = Interpreter.GetDebugTraceback();
        e = new LuaScriptException(ex, errLocation);
    }

    Push(luaState, e);
}
```

**NovaSharp Application:**

- Wrap all user-visible exceptions with source location info
- Use `Exception.Data["Traceback"]` for storing Lua stack traces
- Distinguish between "Lua script error" and "wrapped .NET exception"

______________________________________________________________________

## 6. Static Delegate Pattern for Metamethods

### 6.1 Pre-allocated Static Delegates

```csharp
// From Metatables.cs
class MetaFunctions
{
    // Static readonly delegates avoid allocation on each call
    public static readonly LuaNativeFunction GcFunction = CollectObject;
    public static readonly LuaNativeFunction IndexFunction = GetMethod;
    public static readonly LuaNativeFunction NewIndexFunction = SetFieldOrProperty;
    public static readonly LuaNativeFunction ToStringFunction = ToStringLua;
    public static readonly LuaNativeFunction CallConstructorFunction = CallConstructor;
    public static readonly LuaNativeFunction CallDelegateFunction = CallDelegate;
    
    // Arithmetic operators
    public static readonly LuaNativeFunction AddFunction = AddLua;
    public static readonly LuaNativeFunction SubtractFunction = SubtractLua;
    public static readonly LuaNativeFunction MultiplyFunction = MultiplyLua;
    public static readonly LuaNativeFunction DivideFunction = DivideLua;
    public static readonly LuaNativeFunction ModulusFunction = ModLua;
    public static readonly LuaNativeFunction UnaryNegationFunction = UnaryNegationLua;
}
```

**NovaSharp Application:** For C# functions exposed to Lua, create delegates once and reuse them. Don't create new delegate instances on each call.

### 6.2 Translator Lookup via Pool

```csharp
// From ObjectTranslatorPool.cs
public class ObjectTranslatorPool
{
    public static ObjectTranslatorPool Instance { get; } = new ObjectTranslatorPool();
    
    private readonly ConcurrentDictionary<LuaState, ObjectTranslator> _translators 
        = new ConcurrentDictionary<LuaState, ObjectTranslator>();

    public ObjectTranslator Find(LuaState luaState)
    {
        if (_translators.TryGetValue(luaState, out var translator))
            return translator;
            
        // Handle coroutine threads
        var mainThread = luaState.MainThread;
        return _translators.TryGetValue(mainThread, out translator) ? translator : null;
    }
}
```

**NovaSharp Application:** If supporting coroutines with separate states, maintain a pool/registry to look up associated data structures.

______________________________________________________________________

## 7. Array Access Fast Paths

### 7.1 Type-Specific Branching

```csharp
// From Metatables.cs
private bool TryAccessByArray(LuaState luaState, Type objType, object obj, object index)
{
    // Check if index is an integer
    if (!luaState.IsInteger(2))
        return false;
        
    int intIndex = (int)luaState.ToInteger(2);
    
    // Type-specific fast paths
    if (obj is int[] intArray)
    {
        _translator.Push(luaState, intArray[intIndex]);
        return true;
    }
    if (obj is byte[] byteArray)
    {
        _translator.Push(luaState, byteArray[intIndex]);
        return true;
    }
    if (obj is float[] floatArray)
    {
        _translator.Push(luaState, floatArray[intIndex]);
        return true;
    }
    if (obj is double[] doubleArray)
    {
        _translator.Push(luaState, doubleArray[intIndex]);
        return true;
    }
    if (obj is long[] longArray)
    {
        _translator.Push(luaState, longArray[intIndex]);
        return true;
    }
    
    // Fall back to reflection for other array types
    if (objType.IsArray)
    {
        _translator.Push(luaState, ((Array)obj).GetValue(intIndex));
        return true;
    }
    
    return false;
}
```

**NovaSharp Application:** For common array types (`int[]`, `double[]`, `byte[]`), add specialized fast paths that avoid boxing and reflection.

______________________________________________________________________

## 8. Patterns NOT Applicable to NovaSharp

### 8.1 P/Invoke-Specific Patterns

NLua uses `IntPtr` handles, `LuaState.FromIntPtr()`, and `[MonoPInvokeCallback]` attributes for native interop. These are irrelevant for a pure C# interpreter.

### 8.2 IL Emit Code Generation

```csharp
// From CodeGeneration.cs - NOT applicable
private Type GenerateDelegate(Type delegateType)
{
    var myType = newModule.DefineType(typeName, TypeAttributes.Public, delegateParent);
    var delegateMethod = myType.DefineMethod("CallFunction", ...);
    ILGenerator generator = delegateMethod.GetILGenerator();
    generator.Emit(OpCodes.Ldarg_0);
    // ... more IL emit
    return myType.CreateType();
}
```

NLua generates types at runtime to bridge Lua functions to .NET delegates. NovaSharp doesn't need this—Lua functions are already first-class objects in the interpreter.

### 8.3 Lua Registry Reference Management

NLua uses the Lua registry for storing references:

```csharp
luaState.Ref(LuaRegistry.Index);  // Create reference
luaState.Unref(LuaRegistry.Index, reference);  // Release
```

NovaSharp manages Lua values directly in C#, so the registry pattern isn't needed.

______________________________________________________________________

## 9. Actionable Optimizations for NovaSharp

### High Priority (Low Effort, High Impact)

| Optimization                 | Description                                              | Effort    |
| ---------------------------- | -------------------------------------------------------- | --------- |
| **Extractor Delegate Cache** | Pre-cache type conversion delegates in a dictionary      | 1-2 days  |
| **Member Cache**             | Two-level `Type → Name → MemberInfo` cache               | 1 day     |
| **Static Delegates**         | Pre-allocate delegates for metamethods                   | 2-3 hours |
| **ReferenceComparer**        | Use `RuntimeHelpers.GetHashCode()` for userdata tracking | 1 hour    |

### Medium Priority (Medium Effort, Medium Impact)

| Optimization            | Description                                            | Effort    |
| ----------------------- | ------------------------------------------------------ | --------- |
| **MethodCache Pattern** | Cache last-called method for repeated invocations      | 2-3 days  |
| **Array Fast Paths**    | Type-specific branches for common array types          | 1 day     |
| **Fakenil Sentinel**    | Use sentinel values for "cached nil" scenarios         | 2-3 hours |
| **Exception Hierarchy** | Add `LuaScriptException` with source location tracking | 1 day     |

### Low Priority (High Effort, Context-Dependent)

| Optimization                | Description                                     | Effort   |
| --------------------------- | ----------------------------------------------- | -------- |
| **Deferred GC Queue**       | ConcurrentQueue for finalized reference cleanup | 2-3 days |
| **Weak Reference Wrappers** | LuaBase pattern for GC-friendly object wrappers | 3-4 days |

______________________________________________________________________

## 10. Summary

NLua's architecture provides several patterns directly applicable to NovaSharp's CLR interop layer:

1. **Type Conversion** - Use dictionary-based delegate caching, not runtime reflection
1. **Caching** - Two-level member cache keyed by (Type, Name)
1. **Object Tracking** - Bidirectional maps with `ReferenceComparer` for value types
1. **Method Dispatch** - Cache last-called method, skip overload resolution when possible
1. **Error Handling** - Wrap exceptions with source location, distinguish Lua vs .NET errors
1. **Memory Management** - Deferred cleanup via queue, weak references for circular reference handling

The IL emit code generation in NLua is not applicable—NovaSharp already has first-class Lua functions without needing runtime type generation.

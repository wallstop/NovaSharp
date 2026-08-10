# Refactoring to Zero-Allocation Patterns Reference

## Step 4: Collection Pooling

All pools are in `WallstopStudios.NovaSharp.Interpreter.DataStructs`.

| Pool                  | Get Method                                     |
| --------------------- | ---------------------------------------------- |
| `ListPool<T>`         | `ListPool<T>.Get(out List<T>)`                 |
| `HashSetPool<T>`      | `HashSetPool<T>.Get(out HashSet<T>)`           |
| `DictionaryPool<K,V>` | `DictionaryPool<K,V>.Get(out Dictionary<K,V>)` |

See [pooling-patterns](../../../code-samples/pooling-patterns.md) for usage examples.

______________________________________________________________________

## Step 5: String Building

Replace `StringBuilder` and string concatenation with ZString:

```csharp
// BAD: Multiple allocations
return $"Error at line {line}: {message} (code: {errorCode})";

// GOOD: Zero intermediate allocations
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(line);
sb.Append(": ");
sb.Append(message);
sb.Append(" (code: ");
sb.Append(errorCode);
sb.Append(')');
return sb.ToString();
```

See [zstring-migration](../../zstring-migration/SKILL.md) for detailed patterns.

______________________________________________________________________

## Step 6: Array Pooling

| Scenario                                | Pool                 |
| --------------------------------------- | -------------------- |
| LuaValue arrays in VM hot path          | `DynValueArrayPool`  |
| Object arrays for reflection/interop    | `ObjectArrayPool`    |
| Variable-size temporary buffers         | `SystemArrayPool<T>` |
| Small fixed-size buffers (\<=256 bytes) | `stackalloc`         |

______________________________________________________________________

## Verification

### Test Zero Allocation

```csharp
[Test]
public void Method_ShouldNotAllocate()
{
    MethodUnderTest();  // Warm up

    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1000; i++)
    {
        MethodUnderTest();
    }
    long after = GC.GetAllocatedBytesForCurrentThread();

    Assert.That(after - before, Is.LessThan(100));
}
```

______________________________________________________________________

## Quick Refactoring Checklist

- [ ] No LINQ in hot paths - converted to manual loops
- [ ] No closures capturing variables - using static lambdas or explicit loops
- [ ] Collections are pooled - using `ListPool<T>`, `HashSetPool<T>`, etc.
- [ ] No `new StringBuilder()` - using `ZStringBuilder.Create()`
- [ ] Arrays are pooled or stackalloc
- [ ] Pools are disposed - all `PooledResource<T>` in `using` blocks
- [ ] Tested for allocations - verified with allocation test

# High-Performance C# Coding Guidelines Reference

## Closures and Lambdas

```csharp
// BAD: Captures 'threshold' - allocates closure
int threshold = 10;
List<int> filtered = items.Where(x => x > threshold).ToList();

// GOOD: Static lambda - no capture
items.Where(static x => x > 0);

// GOOD: Explicit loop
for (int i = 0; i < items.Count; i++)
{
    if (items[i] > threshold)
        filtered.Add(items[i]);
}
```

______________________________________________________________________

## Enum String Caching

**NEVER call `.ToString()` on enums in hot paths.** Use cached lookups:

```csharp
// BAD: Allocates every call
sb.Append(tokenType.ToString());

// GOOD: Zero allocation
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));
```

Available caches: `TokenTypeStrings`, `OpCodeStrings`, `SymbolRefTypeStrings`, `ModLoadStateStrings`, `DebuggerActionTypeStrings`

______________________________________________________________________

## Hash Code Implementation

**ALWAYS use `HashCodeHelper`** for `GetHashCode()`. Never use bespoke patterns or `HashCode.Combine()`.

```csharp
// GOOD: Simple multi-field hash
public override int GetHashCode()
{
    return HashCodeHelper.HashCode(_field1, _field2, _field3);
}

// GOOD: Use DeterministicHashBuilder for complex hashing
public override int GetHashCode()
{
    DeterministicHashBuilder hash = default;
    hash.AddInt((int)Type);
    if (HasValue) hash.Add(Value);
    return hash.ToHashCode();
}
```

______________________________________________________________________

## Sorting Without Boxing

Use `IListSortExtensions` with struct comparers to avoid boxing:

```csharp
// BAD: Boxes struct comparer
list.Sort(new LuaValueComparer(script));

// GOOD: Zero-allocation with struct comparer
readonly struct LuaValueComparer : IComparer<LuaValue> { /* ... */ }
list.Sort(new LuaValueComparer(script));  // Extension method, no boxing
```

______________________________________________________________________

## Unity Compatibility

NovaSharp targets Unity3D (IL2CPP/AOT) in addition to .NET. See [unity-gc-patterns](../../../code-samples/unity-gc-patterns.md) for:

- APIs NOT available in Unity
- IL2CPP-specific considerations
- Why zero-allocation is even more critical in Unity

______________________________________________________________________

## Profiling and Verification

### Test for Zero Allocation

```csharp
[Test]
public void Method_ShouldNotAllocate()
{
    // Warm up
    target.Method();

    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 1000; i++)
    {
        target.Method();
    }
    long after = GC.GetAllocatedBytesForCurrentThread();

    Assert.That(after - before, Is.EqualTo(0));
}
```

### Identifying Hot Paths

| Code Location          | Frequency       | Priority |
| ---------------------- | --------------- | -------- |
| VM execution loop      | Millions/second | Critical |
| Opcode handlers        | Millions/second | Critical |
| LuaValue operations    | Very frequent   | Critical |
| Table get/set          | Very frequent   | High     |
| Function call dispatch | Per call        | High     |
| String operations      | Per string op   | Medium   |
| Script compilation     | Once per script | Low      |

______________________________________________________________________

## Checklist for New Code

- [ ] Using explicit types everywhere? (Never use `var`)
- [ ] Could this be a `readonly struct`?
- [ ] If struct, added `IEquatable<T>`, `Equals`, `GetHashCode`, `==`, `!=`?
- [ ] Using `HashCodeHelper` for `GetHashCode()`?
- [ ] Using pooled resources with `using`?
- [ ] Avoiding LINQ in hot paths?
- [ ] Using static lambdas or cached delegates?
- [ ] Using `ZString` for string building?
- [ ] Avoiding boxing value types?

______________________________________________________________________

## Related Documentation

- [allocation-traps](../../allocation-traps/SKILL.md) - Common allocation pitfalls
- [zstring-migration](../../zstring-migration/SKILL.md) - String building patterns
- [span-optimization](../../span-optimization/SKILL.md) - Span-based operations
- [correctness-then-performance](../../correctness-then-performance/SKILL.md) - Priority hierarchy

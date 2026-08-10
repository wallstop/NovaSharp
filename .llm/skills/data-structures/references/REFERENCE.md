# Data Structures — When to Use What Reference

## NovaSharp-Specific Choices

### LuaValue Collections

```csharp
// ✅ GOOD: Pool for temporary LuaValue arrays
using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(count, out LuaValue[] args);
// Use args...
// Automatically returned on dispose

// ✅ GOOD: Pool for temporary lists
using PooledResource<List<LuaValue>> pooled = ListPool<LuaValue>.Get(out List<LuaValue> list);
// Use list...
// Automatically returned on dispose

// ❌ BAD: Allocates in hot path
LuaValue[] args = new LuaValue[count];
List<LuaValue> list = new List<LuaValue>();
```

### Table Implementation

NovaSharp Tables use:

- **Array part**: Contiguous storage for integer keys 1..n
- **Hash part**: Dictionary-style for non-integer or sparse keys

This matches Lua's internal design for optimal mixed-use performance.

### String Interning

Lua internalizes (interns) strings. When implementing string operations:

```csharp
// Consider string interning for frequently used strings
// Table keys, variable names, etc. benefit from interning
```

______________________________________________________________________

## Common Anti-Patterns

| Anti-Pattern                 | Problem                     | Fix                              |
| ---------------------------- | --------------------------- | -------------------------------- |
| List as set                  | O(n) Contains on each add   | Use `HashSet<T>`                 |
| Dictionary for ordered data  | Order not guaranteed        | Use `SortedDictionary` or `List` |
| LinkedList for random access | O(n) index access           | Use `List<T>`                    |
| Reallocating in loops        | Multiple resize allocations | Pre-size or use pool             |

______________________________________________________________________

## Collection Choice Decision Tree

```text
Need to store items?
│
├─ Need key-value pairs?
│  ├─ Need sorted order? → SortedDictionary<K,V>
│  └─ Just lookup? → Dictionary<K,V>
│
├─ Just values?
│  ├─ Need uniqueness?
│  │  ├─ Need sorted? → SortedSet<T>
│  │  └─ Just unique? → HashSet<T>
│  │
│  ├─ Need ordering?
│  │  ├─ FIFO? → Queue<T>
│  │  ├─ LIFO? → Stack<T>
│  │  └─ Arbitrary order? → List<T>
│  │
│  └─ Fixed size known?
│     ├─ At compile time + small? → stackalloc
│     ├─ At runtime + hot path? → ArrayPool
│     └─ At runtime + cold path? → T[]
│
└─ Hot path?
   └─ Yes → Use *Pool<T>.Get() variants
```

______________________________________________________________________

## IEqualityComparer for Dictionary/HashSet

When using custom types as dictionary keys or in hash sets:

```csharp
// ✅ GOOD: Struct with proper equality
public readonly struct SourceLocation : IEquatable<SourceLocation>
{
    public int Line { get; }
    public int Column { get; }
    public bool Equals(SourceLocation other) =>
        Line == other.Line && Column == other.Column;
    public override bool Equals(object obj) =>
        obj is SourceLocation other && Equals(other);
    public override int GetHashCode() =>
        HashCodeHelper.HashCode(Line, Column);
}

// ✅ GOOD: Custom comparer for specific comparisons
public sealed class DataTypeEqualityComparer : IEqualityComparer<DataType>
{
    public static readonly DataTypeEqualityComparer Instance = new();

    public bool Equals(DataType x, DataType y) => x == y;
    public int GetHashCode(DataType obj) => (int)obj;
}

// Usage
Dictionary<DataType, string> typeNames = new(DataTypeEqualityComparer.Instance);
```

______________________________________________________________________

## Resources

- [high-performance-csharp](../../high-performance-csharp/SKILL.md) — Performance patterns
- [allocation-traps](../../allocation-traps/SKILL.md) — Hidden allocations
- [DataStructs/CollectionPools.cs](../../../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs) — Pool implementations
- [DataStructs/HashCodeHelper.cs](../../../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/HashCodeHelper.cs) — Hash code utilities

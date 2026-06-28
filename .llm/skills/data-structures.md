______________________________________________________________________

triggers:

- "data structure"
- "collection"
- "dictionary"
- "list vs array"
- "complexity"
- "big O"
  category: performance
  related:
- high-performance-csharp
- allocation-traps
  priority: recommended

______________________________________________________________________

# Skill: Data Structures — When to Use What

**When to use**: Choosing the right data structure for performance-critical code, or understanding complexity trade-offs.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (performance patterns), [allocation-traps](allocation-traps.md) (allocation costs)

______________________________________________________________________

## 🔴 Quick Decision Guide

| Need                                   | Use                               | Avoid                            |
| -------------------------------------- | --------------------------------- | -------------------------------- |
| **Ordered, indexed access**            | `T[]` or `List<T>`                | `LinkedList<T>`                  |
| **Unique items, fast lookup**          | `HashSet<T>`                      | `List<T>` + Contains             |
| **Key-value mapping**                  | `Dictionary<K,V>`                 | `List<KeyValuePair>`             |
| **FIFO queue**                         | `Queue<T>`                        | `List<T>` with Insert(0)         |
| **LIFO stack**                         | `Stack<T>`                        | `List<T>` with RemoveAt          |
| **Sorted data + range queries**        | `SortedSet<T>`, `SortedList<K,V>` | Manual sorting                   |
| **Concurrent access**                  | `Concurrent*` collections         | Locks around regular collections |
| **Fixed-size small buffer (hot path)** | `stackalloc T[n]`                 | `new T[n]`                       |
| **Variable-size buffer (hot path)**    | `ArrayPool<T>`                    | `new T[n]`                       |
| **Temporary collection (hot path)**    | `ListPool<T>`, `HashSetPool<T>`   | `new List<T>()`                  |

______________________________________________________________________

## Time Complexity Reference

### Array / List<T>

| Operation       | Array   | List<T> | Notes                       |
| --------------- | ------- | ------- | --------------------------- |
| Index access    | O(1)    | O(1)    | Direct memory offset        |
| Add (end)       | N/A     | O(1)\*  | \*Amortized, may reallocate |
| Insert (middle) | N/A     | O(n)    | Must shift elements         |
| Remove (middle) | N/A     | O(n)    | Must shift elements         |
| Contains        | O(n)    | O(n)    | Linear search               |
| Memory          | Compact | Compact | Contiguous, cache-friendly  |

**When to use Array vs List<T>:**

- **Array**: Fixed size known at compile time, maximum performance
- **List<T>**: Size varies, need Add/Remove operations

### Dictionary\<K,V> / HashSet<T>

| Operation | Average | Worst  | Notes               |
| --------- | ------- | ------ | ------------------- |
| Add       | O(1)    | O(n)   | Resize on growth    |
| Remove    | O(1)    | O(n)   | Hash collision      |
| Lookup    | O(1)    | O(n)   | Hash collision      |
| Contains  | O(1)    | O(n)   | Hash collision      |
| Memory    | Higher  | Higher | Hash table overhead |

**Keys must have proper `GetHashCode()` and `Equals()`!**

### Queue<T> / Stack<T>

| Operation    | Queue  | Stack  | Notes         |
| ------------ | ------ | ------ | ------------- |
| Enqueue/Push | O(1)\* | O(1)\* | \*Amortized   |
| Dequeue/Pop  | O(1)   | O(1)   |               |
| Peek         | O(1)   | O(1)   |               |
| Contains     | O(n)   | O(n)   | Linear search |

### SortedSet<T> / SortedDictionary\<K,V>

| Operation   | Time         | Notes                 |
| ----------- | ------------ | --------------------- |
| Add         | O(log n)     | Binary tree insertion |
| Remove      | O(log n)     | Binary tree removal   |
| Lookup      | O(log n)     | Binary search         |
| Min/Max     | O(log n)     | Tree traversal        |
| Range query | O(log n + k) | k = items in range    |

**Use when you need sorted iteration or range queries.**

### LinkedList<T>

| Operation        | Time | Notes                          |
| ---------------- | ---- | ------------------------------ |
| AddFirst/AddLast | O(1) | Pointer manipulation           |
| Remove           | O(1) | If you have the node reference |
| Index access     | O(n) | Must traverse from head/tail   |
| Contains         | O(n) | Linear search                  |

**Rarely needed.** Usually `List<T>` or `Queue<T>` is better.

______________________________________________________________________

## Memory Characteristics

### Memory Layout Impact on Performance

| Structure               | Cache Behavior | Memory Overhead    |
| ----------------------- | -------------- | ------------------ |
| `T[]`                   | ★★★★★ Best     | Minimal            |
| `List<T>`               | ★★★★★ Best     | 24 bytes + buffer  |
| `Dictionary<K,V>`       | ★★★☆☆ OK       | Significant        |
| `HashSet<T>`            | ★★★☆☆ OK       | Significant        |
| `LinkedList<T>`         | ★☆☆☆☆ Poor     | 40 bytes per node  |
| `SortedDictionary<K,V>` | ★★☆☆☆ Fair     | Tree node overhead |

### Allocation Costs

| Operation               | Allocates                      |
| ----------------------- | ------------------------------ |
| `new T[n]`              | 24 + (n × sizeof(T)) bytes     |
| `new List<T>()`         | 56 bytes (empty), grows on add |
| `new List<T>(capacity)` | 56 + buffer for capacity       |
| `new Dictionary<K,V>()` | ~96 bytes (empty)              |
| `new HashSet<T>()`      | ~64 bytes (empty)              |
| `ListPool<T>.Get()`     | 0 bytes (pooled)               |
| `stackalloc T[n]`       | 0 bytes (stack)                |

______________________________________________________________________

## NovaSharp-Specific Choices

### DynValue Collections

```csharp
// ✅ GOOD: Pool for temporary DynValue arrays
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(count, out DynValue[] args);
// Use args...
// Automatically returned on dispose

// ✅ GOOD: Pool for temporary lists
using PooledResource<List<DynValue>> pooled = ListPool<DynValue>.Get(out List<DynValue> list);
// Use list...
// Automatically returned on dispose

// ❌ BAD: Allocates in hot path
DynValue[] args = new DynValue[count];
List<DynValue> list = new List<DynValue>();
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

- [high-performance-csharp](high-performance-csharp.md) — Performance patterns
- [allocation-traps](allocation-traps.md) — Hidden allocations
- [DataStructs/CollectionPools.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/CollectionPools.cs) — Pool implementations
- [DataStructs/HashCodeHelper.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/HashCodeHelper.cs) — Hash code utilities

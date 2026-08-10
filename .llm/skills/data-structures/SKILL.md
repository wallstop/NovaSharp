---
name: data-structures
description: "Choose allocation- and performance-appropriate data structures for NovaSharp. Use when comparing arrays, lists, dictionaries, sets, queues, stacks, spans, pools, or algorithmic complexity."
metadata:
  category: performance
  priority: recommended
  related: high-performance-csharp, allocation-traps
---
# Skill: Data Structures — When to Use What

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md) (performance patterns), [allocation-traps](../allocation-traps/SKILL.md) (allocation costs)

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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for NovaSharp-Specific Choices, Common Anti-Patterns, Collection Choice Decision Tree, IEqualityComparer for Dictionary/HashSet, and later sections.

# Skill: Foreach Allocation Traps

**When to use**: Choosing between `foreach` and `for` loops to avoid hidden allocations, especially in Unity.

**Related Skills**: [unity-gc-patterns](unity-gc-patterns.md) (Unity-specific), [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (refactoring)

______________________________________________________________________

## 🔴 The Problem: Enumerator Boxing

In Unity's Mono compiler (and some older .NET versions), `foreach` loops over certain collection types cause hidden allocations due to **enumerator boxing**.

### How It Happens

When you write:

```csharp
foreach (var item in collection) { }
```

The compiler generates:

```csharp
var enumerator = collection.GetEnumerator();
try
{
    while (enumerator.MoveNext())
    {
        var item = enumerator.Current;
        // loop body
    }
}
finally
{
    ((IDisposable)enumerator).Dispose();  // ← Boxing occurs here!
}
```

If `GetEnumerator()` returns a **struct enumerator** and the `finally` block casts it to `IDisposable`, the struct is **boxed** onto the heap.

______________________________________________________________________

## 🔴 Which Collections Are Affected?

| Collection Type           | `foreach` Allocation          | Recommendation                             |
| ------------------------- | ----------------------------- | ------------------------------------------ |
| `T[]` (arrays)            | ✅ No allocation              | Safe to use `foreach`                      |
| `List<T>`                 | ⚠️ **24 bytes** in Unity Mono | Use `for` loop                             |
| `Dictionary<K,V>`         | ⚠️ **24+ bytes**              | Use `for` with keys or manual enumerator   |
| `HashSet<T>`              | ⚠️ **24 bytes**               | Use manual enumerator pattern              |
| `Queue<T>`                | ⚠️ Allocates                  | Convert to array or use different approach |
| `Stack<T>`                | ⚠️ Allocates                  | Pop in loop instead                        |
| `IEnumerable<T>`          | ⚠️ Allocates                  | Use concrete types                         |
| `string`                  | ✅ No allocation              | Safe (special-cased)                       |
| Custom struct enumerators | ⚠️ May allocate               | Depends on implementation                  |

### Why Arrays Are Safe

Arrays use a special compiler pattern that doesn't involve `IDisposable`:

```csharp
// For arrays, compiler generates:
for (int i = 0; i < array.Length; i++)
{
    var item = array[i];
    // loop body
}
```

______________________________________________________________________

## 🔴 Migration Patterns

### Pattern 1: List<T> → for Loop

```csharp
// ❌ BAD: Allocates 24 bytes in Unity Mono
foreach (var item in myList)
{
    ProcessItem(item);
}

// ✅ GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++)
{
    ProcessItem(myList[i]);
}
```

### Pattern 2: Dictionary\<K,V> Iteration

```csharp
// ❌ BAD: Allocates enumerator
foreach (var kvp in myDictionary)
{
    Process(kvp.Key, kvp.Value);
}

// ✅ GOOD: Manual enumerator without Dispose boxing
var enumerator = myDictionary.GetEnumerator();
while (enumerator.MoveNext())
{
    var kvp = enumerator.Current;
    Process(kvp.Key, kvp.Value);
}
// Note: Struct enumerator goes out of scope, no Dispose call needed
// (Dictionary's struct enumerator's Dispose is a no-op anyway)

// ✅ ALTERNATIVE: Iterate over keys if you have them cached
foreach (var key in cachedKeysList)  // If cachedKeysList is an array
{
    if (myDictionary.TryGetValue(key, out var value))
    {
        Process(key, value);
    }
}
```

### Pattern 3: HashSet<T> Iteration

```csharp
// ❌ BAD: Allocates enumerator
foreach (var item in myHashSet)
{
    Process(item);
}

// ✅ GOOD: Manual enumerator
var enumerator = myHashSet.GetEnumerator();
while (enumerator.MoveNext())
{
    Process(enumerator.Current);
}

// ✅ ALTERNATIVE: Copy to array if iterating frequently
T[] items = new T[myHashSet.Count];
myHashSet.CopyTo(items);
// Now iterate over array (zero allocation per iteration)
for (int i = 0; i < items.Length; i++)
{
    Process(items[i]);
}
```

### Pattern 4: IEnumerable<T> → Concrete Type

```csharp
// ❌ BAD: Interface forces allocation
void Process(IEnumerable<Item> items)
{
    foreach (var item in items)  // Always allocates
    {
        // ...
    }
}

// ✅ GOOD: Accept concrete types
void Process(List<Item> items)
{
    for (int i = 0; i < items.Count; i++)
    {
        // ...
    }
}

void Process(Item[] items)
{
    foreach (var item in items)  // Arrays are safe
    {
        // ...
    }
}
```

______________________________________________________________________

## 🔴 Nested Loop Impact

The allocation issue compounds with nested loops:

```csharp
// ❌ BAD: 4000 lists × 24 bytes = 96 KB garbage PER FRAME
foreach (var outerList in collectionOfLists)     // 24 bytes
{
    foreach (var item in outerList)               // 24 bytes × 4000
    {
        Process(item);
    }
}

// ✅ GOOD: Zero allocation
for (int i = 0; i < collectionOfLists.Count; i++)
{
    var innerList = collectionOfLists[i];
    for (int j = 0; j < innerList.Count; j++)
    {
        Process(innerList[j]);
    }
}
```

______________________________________________________________________

## 🔴 Modern .NET vs Unity Mono

| Runtime             | `foreach` Behavior                          |
| ------------------- | ------------------------------------------- |
| .NET Core 3.0+      | ✅ Optimized - usually no allocation        |
| .NET 5+             | ✅ Further optimized                        |
| Unity Mono (IL2CPP) | ⚠️ **Allocates** for List, Dictionary, etc. |
| Unity Mono (JIT)    | ⚠️ **Allocates** for List, Dictionary, etc. |

Even though .NET Core has fixed this, **Unity still uses the allocating pattern**. Always use `for` loops in Unity code.

______________________________________________________________________

## 🔴 Span-Based Iteration

For collections that support `AsSpan()` or `CollectionsMarshal.AsSpan()`:

```csharp
// ✅ GOOD: Span iteration (when available)
// Note: CollectionsMarshal is .NET 5+ only, NOT available in Unity!

// For arrays (always works)
Span<T> span = myArray.AsSpan();
foreach (var item in span)  // Special-cased, no allocation
{
    // ...
}

// For strings
foreach (char c in myString.AsSpan())  // No allocation
{
    // ...
}
```

______________________________________________________________________

## 🔴 Custom Struct Enumerator Pattern

If you create a custom collection, implement a struct enumerator properly:

```csharp
public struct MyCollection<T>
{
    private T[] _items;
    private int _count;
    
    // Return struct enumerator, NOT IEnumerator<T>
    public Enumerator GetEnumerator() => new Enumerator(this);
    
    public struct Enumerator  // Struct, not class!
    {
        private readonly MyCollection<T> _collection;
        private int _index;
        
        internal Enumerator(MyCollection<T> collection)
        {
            _collection = collection;
            _index = -1;
        }
        
        public T Current => _collection._items[_index];
        
        public bool MoveNext()
        {
            return ++_index < _collection._count;
        }
        
        // No Dispose needed - empty implementation
        public void Dispose() { }
    }
}

// Usage: foreach uses the struct enumerator directly
// No boxing because compiler knows the concrete type
foreach (var item in myCollection) { }  // Zero allocation
```

______________________________________________________________________

## 🔴 Quick Decision Tree

```
Is it an array (T[])?
├── YES → foreach is safe, zero allocation
└── NO
    ├── Is it a string?
    │   └── YES → foreach is safe, zero allocation
    └── NO → Use for loop or manual enumerator
```

______________________________________________________________________

## Benchmark Reference

From community testing:

| Pattern                    | Time (16M ops) | Memory                 |
| -------------------------- | -------------- | ---------------------- |
| `for` over `int[]`         | 35ms           | 0 B                    |
| `for` over `List<int>`     | 62ms           | 0 B                    |
| `foreach` over `int[]`     | 35ms           | 0 B                    |
| `foreach` over `List<int>` | 120ms          | **24 B per iteration** |
| LINQ `.Sum()`              | 271ms          | **24 B**               |

______________________________________________________________________

## Checklist for foreach Usage

Before using `foreach`:

- [ ] **Is it over an array?** → Safe
- [ ] **Is it over a string?** → Safe
- [ ] **Is it over a Span<T>?** → Safe
- [ ] **Is it in a hot path?** → Use `for` loop instead
- [ ] **Is it nested?** → Definitely use `for` loops
- [ ] **Is it over IEnumerable<T>?** → Try to use concrete type

______________________________________________________________________

## Resources

- [unity-gc-patterns](unity-gc-patterns.md) — Unity GC overview
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Refactoring patterns
- [performance-audit](performance-audit.md) — Quick audit checklist

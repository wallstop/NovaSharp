# Unity GC Patterns

Patterns for avoiding allocations in Unity's Boehm GC environment.

______________________________________________________________________

## Why Unity GC is Different

Unity uses the **Boehm-Demers-Weiser** garbage collector, which is significantly worse than .NET's generational GC:

| Aspect                | Unity (Boehm)                      | .NET CLR GC                 |
| --------------------- | ---------------------------------- | --------------------------- |
| **Algorithm**         | Conservative, non-generational     | Generational (Gen 0/1/2)    |
| **Heap scanning**     | Scans ENTIRE heap every collection | Only scans young generation |
| **Memory compaction** | No                                 | Yes                         |
| **Fragmentation**     | Prone to fragmentation             | Reduced via compaction      |
| **Speed**             | Slow (entire heap)                 | Fast (generational)         |

**Zero-allocation code is even MORE critical in Unity** because:

- Unity GC must scan the entire heap on every collection
- No generational optimization - temporary objects are as expensive as long-lived ones
- Memory fragmentation accumulates over time (no compaction)
- GC spikes cause frame drops and stuttering

______________________________________________________________________

## Unity API Constraints

### APIs NOT AVAILABLE in Unity (Never Use)

| API                                            | Reason                     | Alternative                                 |
| ---------------------------------------------- | -------------------------- | ------------------------------------------- |
| `CollectionsMarshal.AsSpan<T>(List<T>)`        | .NET 5+ only, not in Unity | Use `list.ToArray()` or manual array access |
| `CollectionsMarshal.GetValueRefOrAddDefault()` | .NET 6+ only               | Use standard dictionary operations          |
| `List<T>.EnsureCapacity()`                     | .NET Core 2.1+ only        | Use constructor with capacity               |
| `Span<T>` fields in non-ref structs            | Requires runtime support   | Use `ref struct` or arrays                  |
| `[SkipLocalsInit]`                             | Requires runtime support   | Unavailable                                 |
| `half` (Half-precision float)                  | .NET 5+ only               | Use `float`                                 |
| `nint` / `nuint`                               | .NET 5+ only               | Use `IntPtr` / `UIntPtr`                    |
| Generic math interfaces (`INumber<T>`)         | .NET 7+ only               | Use explicit type overloads                 |
| `System.Reflection.Emit`                       | AOT incompatible           | Pre-generated code                          |
| `Expression.Compile()`                         | AOT incompatible           | Interpreted expressions                     |
| `dynamic` keyword                              | Requires DLR, AOT issues   | Explicit type handling                      |

______________________________________________________________________

## Unity-Specific Allocation Traps

| Pattern                    | Problem                  | Fix                                  |
| -------------------------- | ------------------------ | ------------------------------------ |
| `foreach` on `List<T>`     | 24 bytes per loop (Mono) | Use `for` loop                       |
| `foreach` on `Dictionary`  | Enumerator allocation    | Manual enumerator or `for` with keys |
| `Func<T>` assigned in loop | 52+ bytes per iteration  | Cache delegate in field              |
| `params object[]`          | Array + boxing           | Overloads for 0-4 args               |
| `Mathf.Max(a,b,c)`         | params array             | Chain `Mathf.Max(Mathf.Max(a,b),c)`  |
| `gameObject.name`          | Allocates string         | Cache in `Awake()`                   |
| `gameObject.tag ==`        | Allocates string         | Use `CompareTag()`                   |
| `mesh.vertices`            | Copies array             | Cache or use `GetVertices(List)`     |
| `yield return new`         | Allocates object         | Cache yield instruction              |

______________________________________________________________________

## IL2CPP Considerations

| Concern                     | Impact                        | Mitigation                             |
| --------------------------- | ----------------------------- | -------------------------------------- |
| No JIT                      | Generics may be slower        | Use concrete types in hot paths        |
| Limited reflection          | Runtime type discovery fails  | Use `[Preserve]` attributes            |
| AOT compilation             | Dynamic code generation fails | Avoid `Emit`, `Expression.Compile`     |
| Value type devirtualization | May not happen                | Manually inline critical paths         |
| Generic virtual methods     | May not be optimized          | Use non-generic overloads in hot paths |

______________________________________________________________________

## Unity-Compatible List Access

```csharp
// Get raw array access for List<T> in Unity
// Option 1: Pre-size and use array directly
List<T> list = new List<T>(capacity);
// ... populate ...
T[] underlying = list.ToArray();  // One allocation, then work with array

// Option 2: Use array from the start if size is known
T[] array = ArrayPool<T>.Shared.Rent(capacity);

// Option 3: For fixed-size pools, use NovaSharp's pooling
using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(size, out DynValue[] array);
```

______________________________________________________________________

## foreach vs for Loop

```csharp
// BAD: Allocates 24 bytes in Unity Mono
foreach (DynValue item in myList)
{
    Process(item);
}

// GOOD: Zero allocation
for (int i = 0; i < myList.Count; i++)
{
    Process(myList[i]);
}

// Arrays are optimized - foreach is safe
foreach (DynValue item in myArray)  // Zero allocation
{
    Process(item);
}
```

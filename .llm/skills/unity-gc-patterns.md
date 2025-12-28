# Skill: Unity Garbage Collection Patterns

**When to use**: Writing or optimizing code that will run in Unity, especially for IL2CPP/AOT builds.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (general patterns), [refactor-to-zero-alloc](refactor-to-zero-alloc.md) (refactoring), [aggressive-inlining](aggressive-inlining.md) (micro-optimization)

______________________________________________________________________

## 🔴 Unity GC vs .NET GC

Unity uses the **Boehm-Demers-Weiser** garbage collector, which behaves very differently from .NET's generational GC:

| Aspect                | Unity (Boehm)                      | .NET CLR GC                        |
| --------------------- | ---------------------------------- | ---------------------------------- |
| **Algorithm**         | Conservative, non-generational     | Generational (Gen 0/1/2)           |
| **Heap scanning**     | Scans ENTIRE heap every collection | Only scans young generation (fast) |
| **Memory compaction** | ❌ No                              | ✅ Yes                             |
| **Fragmentation**     | Prone to fragmentation             | Reduced via compaction             |
| **Collection pause**  | Full stop-the-world                | Shorter generational pauses        |
| **Collection speed**  | Slow (entire heap)                 | Fast (young objects die young)     |

### Why This Matters

**Zero-allocation code is even MORE critical in Unity than in standard .NET:**

- Unity's GC must scan the entire heap on every collection
- No generational optimization — temporary objects are as expensive as long-lived ones
- Memory fragmentation accumulates over time (no compaction)
- GC spikes cause frame drops and stuttering

______________________________________________________________________

## 🔴 Unity-Specific Allocation Traps

### Trap 1: `foreach` Loop Allocation (Unity Mono)

In Unity's Mono compiler, `foreach` over certain collections allocates due to enumerator boxing:

```csharp
// ❌ BAD: Allocates 24 bytes per loop (enumerator boxing)
foreach (var item in myList) { }
foreach (var kv in myDictionary) { }
foreach (var item in myHashSet) { }

// ✅ GOOD: Zero allocation with for loop
for (int i = 0; i < myList.Count; i++)
{
    var item = myList[i];
}

// ✅ GOOD: Manual enumerator to avoid Dispose boxing
var enumerator = myHashSet.GetEnumerator();
while (enumerator.MoveNext())
{
    var item = enumerator.Current;
}
```

**Impact**: Nested `foreach` over 4K-element lists = **96 KB garbage per frame**.

### Trap 2: Unity Property Accessors Allocate

Some Unity properties allocate new strings or arrays on every access:

```csharp
// ❌ BAD: Allocates new string EVERY access
if (gameObject.name == "Player") { }  // Allocates!
if (gameObject.tag == "Enemy") { }     // Allocates!

// ✅ GOOD: Use non-allocating methods
if (gameObject.CompareTag("Enemy")) { }  // No allocation

// ✅ GOOD: Cache if you need the string value
private string _cachedName;
void Awake() { _cachedName = gameObject.name; }
```

### Trap 3: Unity Array-Returning Properties

Any Unity property that returns an array allocates a NEW copy on each access:

```csharp
// ❌ BAD: Allocates 4 copies of the vertices array!
for (int i = 0; i < mesh.vertices.Length; i++)
{
    x = mesh.vertices[i].x;  // Copy 1
    y = mesh.vertices[i].y;  // Copy 2
    z = mesh.vertices[i].z;  // Copy 3
}
// Plus once for Length check

// ✅ GOOD: Cache the array reference
var vertices = mesh.vertices;  // One copy
for (int i = 0; i < vertices.Length; i++)
{
    x = vertices[i].x;
    y = vertices[i].y;
    z = vertices[i].z;
}

// ✅ BEST: Use non-allocating API variants
private List<Vector3> _vertexBuffer = new List<Vector3>();
mesh.GetVertices(_vertexBuffer);  // Fills existing list
for (int i = 0; i < _vertexBuffer.Count; i++)
{
    // Use _vertexBuffer[i]
}
```

### Trap 4: Coroutine Yield Allocations

```csharp
// ❌ BAD: Allocates new object every iteration
IEnumerator BadCoroutine()
{
    while (true)
    {
        yield return new WaitForSeconds(1f);  // Allocates!
    }
}

// ✅ GOOD: Cache the yield instruction
private WaitForSeconds _waitOneSecond = new WaitForSeconds(1f);

IEnumerator GoodCoroutine()
{
    while (true)
    {
        yield return _waitOneSecond;  // Reused
    }
}
```

______________________________________________________________________

## 🔴 Unity Non-Allocating API Reference

| Allocating API                     | Non-Allocating Alternative                             |
| ---------------------------------- | ------------------------------------------------------ |
| `Physics.RaycastAll()`             | `Physics.RaycastNonAlloc()`                            |
| `Physics.OverlapSphere()`          | `Physics.OverlapSphereNonAlloc()`                      |
| `Physics2D.RaycastAll()`           | `Physics2D.RaycastNonAlloc()`                          |
| `mesh.vertices` (property)         | `mesh.GetVertices(List<Vector3>)`                      |
| `mesh.normals` (property)          | `mesh.GetNormals(List<Vector3>)`                       |
| `mesh.uv` (property)               | `mesh.GetUVs(int, List<Vector2>)`                      |
| `mesh.triangles` (property)        | `mesh.GetTriangles(List<int>)`                         |
| `Input.touches` (property)         | `Input.touchCount` + `Input.GetTouch(i)`               |
| `Animator.parameters`              | `Animator.parameterCount` + `Animator.GetParameter(i)` |
| `Renderer.sharedMaterials`         | `Renderer.GetSharedMaterials(List<Material>)`          |
| `GetComponents<T>()`               | `GetComponents<T>(List<T>)`                            |
| `GetComponentsInChildren<T>()`     | `GetComponentsInChildren<T>(List<T>)`                  |
| `string.Format()` with value types | ZString or cached strings                              |
| `gameObject.tag == "X"`            | `gameObject.CompareTag("X")`                           |

______________________________________________________________________

## 🔴 IL2CPP/AOT Constraints

When targeting Unity IL2CPP (iOS, consoles, WebGL), additional constraints apply:

| Constraint                  | Impact                                | Workaround                       |
| --------------------------- | ------------------------------------- | -------------------------------- |
| **No JIT**                  | Generics may be slower                | Use concrete types in hot paths  |
| **No Reflection.Emit**      | Cannot generate code at runtime       | Pre-generate all needed code     |
| **No dynamic assemblies**   | No dynamic type creation              | Use source generators            |
| **Limited reflection**      | Type discovery may fail               | Use `[Preserve]` attributes      |
| **No Expression.Compile()** | Cannot compile expressions at runtime | Pre-compile or interpret         |
| **Generic virtualization**  | AOT must generate all instantiations  | Explicit references to all types |

### IL2CPP-Safe Patterns

```csharp
// ✅ GOOD: Explicit generic instantiation for AOT
// Force IL2CPP to generate code for these types
[Preserve]
static void AOTDummy()
{
    // Never called, but ensures types are generated
    new Dictionary<string, MyCustomType>();
    new List<MyValueType>();
}

// ✅ GOOD: Avoid virtual calls in hot inner loops
// Virtual dispatch has overhead; use concrete types when possible
for (int i = 0; i < items.Length; i++)
{
    ConcreteType item = items[i];
    item.DirectMethod();  // Direct call, not virtual
}

// ❌ BAD: Generic virtual methods in hot paths
interface IProcessor { void Process<T>(T value); }
// This can cause poor code generation in IL2CPP
```

______________________________________________________________________

## 🔴 Unity-Specific Pooling Patterns

### Pre-allocate at Scene Load

```csharp
public class ObjectPoolManager : MonoBehaviour
{
    private Stack<GameObject> _pool = new Stack<GameObject>(100);
    
    void Awake()
    {
        // Pre-allocate during loading, not during gameplay
        for (int i = 0; i < 100; i++)
        {
            var obj = Instantiate(_prefab);
            obj.SetActive(false);
            _pool.Push(obj);
        }
    }
    
    public GameObject Rent()
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Pop();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(_prefab);  // Fallback
    }
    
    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Push(obj);
    }
}
```

### Unity 2021+ ObjectPool

```csharp
using UnityEngine.Pool;

// Unity provides built-in pooling
private ObjectPool<MyClass> _pool;

void Awake()
{
    _pool = new ObjectPool<MyClass>(
        createFunc: () => new MyClass(),
        actionOnGet: obj => obj.Reset(),
        actionOnRelease: obj => obj.Cleanup(),
        actionOnDestroy: obj => obj.Dispose(),
        defaultCapacity: 100,
        maxSize: 1000
    );
}

void Update()
{
    var obj = _pool.Get();
    // Use obj
    _pool.Release(obj);
}
```

______________________________________________________________________

## 🔴 Strategic GC Timing

Unity's GC is expensive. Time it strategically:

```csharp
// Trigger GC during loading screens or scene transitions
void OnSceneLoaded()
{
    System.GC.Collect();
    Resources.UnloadUnusedAssets();
}

// For incremental GC (Unity 2019+), control time budget
void Update()
{
    // If using incremental GC, you can adjust time slice
    // But zero-allocation code is still the best approach
}
```

______________________________________________________________________

## 🔴 Memory Never Shrinks

**Critical**: Unity's managed heap NEVER shrinks until the application terminates.

```csharp
// If you allocate 1GB of managed memory, that memory stays reserved
// even after GC collects it. The heap will not return memory to the OS.

// Implications:
// - Initial allocation patterns affect entire session
// - Memory spikes during gameplay persist
// - Pre-allocate everything at startup to avoid fragmentation
```

______________________________________________________________________

## Unity Performance Checklist for Library Code

When writing library code (like NovaSharp) for Unity:

- [ ] **Zero allocations in hot paths** — Even more critical than .NET due to Boehm GC
- [ ] **No LINQ in runtime code** — Allocates iterators and closures
- [ ] **No `foreach` on non-array collections** — Use `for` loops
- [ ] **Pool all runtime objects** — Tables, closures, upvalues
- [ ] **Cache StringBuilder instances** — Reuse with `Clear()`
- [ ] **No `params` arrays in hot paths** — Use overloads instead
- [ ] **No delegates created in loops** — Cache delegates as fields
- [ ] **No boxing in arithmetic** — Use generic constraints
- [ ] **IL2CPP-safe design** — No Reflection.Emit, no dynamic code generation
- [ ] **Explicit generic instantiation** — Reference all needed type combinations
- [ ] **Use non-allocating Unity APIs** — NonAlloc variants, GetXxx(List) overloads
- [ ] **Pre-size all collections** — Avoid resize allocations

______________________________________________________________________

## Profiling Unity Memory

### Unity Profiler

1. Window → Analysis → Profiler
1. Select "Memory" module
1. Look for "GC Alloc" column in "CPU Usage" for per-frame allocations
1. Deep Profile for call stack (expensive, use sparingly)

### Memory Profiler Package

```json
// In Packages/manifest.json
"com.unity.memoryprofiler": "1.0.0"
```

Provides detailed heap snapshots and comparison between snapshots.

### Key Metrics to Watch

| Metric             | Target         | Action if Exceeded             |
| ------------------ | -------------- | ------------------------------ |
| GC Alloc per frame | 0 bytes        | Find and eliminate allocations |
| Reserved heap size | Stable         | Pre-allocate to prevent growth |
| GC frequency       | \<1 per minute | Reduce allocation rate         |
| GC duration        | \<5ms          | Reduce live object count       |

______________________________________________________________________

## Resources

- [high-performance-csharp](high-performance-csharp.md) — General patterns
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Migration patterns
- [aggressive-inlining](aggressive-inlining.md) — Method inlining
- [foreach-allocation](foreach-allocation.md) — foreach trap details

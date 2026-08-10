---
name: unity-gc-patterns
description: "Apply Unity-aware allocation and lifetime patterns for NovaSharp across Boehm GC, Mono, and IL2CPP/AOT. Use when writing or optimizing Unity runtime code, pools, caches, callbacks, or hot paths."
metadata:
  category: performance
  priority: recommended
  related: high-performance-csharp, refactor-to-zero-alloc, aggressive-inlining
---
# Skill: Unity Garbage Collection Patterns

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md) (general patterns), [refactor-to-zero-alloc](../refactor-to-zero-alloc/SKILL.md) (refactoring), [aggressive-inlining](../aggressive-inlining/SKILL.md) (micro-optimization)

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

| Trap                           | Problem                      | Fix                              |
| ------------------------------ | ---------------------------- | -------------------------------- |
| `foreach` on collections       | Enumerator boxing (24 bytes) | Use `for` loop                   |
| `gameObject.name/tag`          | Allocates on every access    | Use `CompareTag()` or cache      |
| `mesh.vertices` etc.           | New array copy on access     | Cache or use `GetVertices(List)` |
| `new WaitForSeconds()` in loop | Allocates each iteration     | Cache yield instruction as field |

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

| Constraint              | Workaround                                        |
| ----------------------- | ------------------------------------------------- |
| No JIT                  | Use concrete types in hot paths                   |
| No Reflection.Emit      | Pre-generate all needed code                      |
| Limited reflection      | Use `[Preserve]` attributes                       |
| No Expression.Compile() | Pre-compile or interpret                          |
| Generic virtualization  | Reference all needed type combinations explicitly |

Use `[Preserve]` on AOT dummy methods to force IL2CPP to generate needed generic types.

______________________________________________________________________

## 🔴 Unity Pooling & Memory Notes

- **Pre-allocate at scene load** — Don't allocate during gameplay
- **Unity 2021+**: Use `UnityEngine.Pool.ObjectPool<T>` for built-in pooling
- **Trigger GC strategically** — During loading screens: `GC.Collect(); Resources.UnloadUnusedAssets();`
- **Memory never shrinks** — Unity's managed heap never returns memory to OS. Pre-allocate to avoid fragmentation.

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

- **Unity Profiler**: Window > Analysis > Profiler. Check "GC Alloc" in CPU Usage.
- **Memory Profiler Package**: `com.unity.memoryprofiler` for heap snapshots.
- **Targets**: GC Alloc = 0 bytes/frame, stable heap size, GC < 1/minute, GC duration < 5ms.

See [high-performance-csharp](../high-performance-csharp/SKILL.md) and [refactor-to-zero-alloc](../refactor-to-zero-alloc/SKILL.md) for patterns.

# Aggressive Inlining Reference

## 🔴 Key Patterns

**Inline fast path, outline slow path**: Keep fast path tiny and inlinable. Move complex/error handling to `[NoInlining]` methods.

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void ValidateIndex(int index)
{
    if ((uint)index >= (uint)_count)
        ThrowIndexOutOfRange(index);  // Cold path, not inlined
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static void ThrowIndexOutOfRange(int index) => throw new IndexOutOfRangeException();
```

______________________________________________________________________

## 🔴 Related Attributes

| Attribute                | Use                                            |
| ------------------------ | ---------------------------------------------- |
| `AggressiveInlining`     | Hint to inline method                          |
| `NoInlining`             | Force separate method (error paths, profiling) |
| `AggressiveOptimization` | .NET 5+ only, not in Unity                     |

______________________________________________________________________

## 🔴 Common Mistakes

- **Over-inlining large methods** — Causes code bloat, makes things SLOWER
- **Inlining virtual methods** — Has no effect
- **Inlining methods with try/catch** — Won't actually inline
- **Not measuring** — JIT often makes good decisions; attribute can make things worse

______________________________________________________________________

## Checklist for Inlining

Before adding `AggressiveInlining`:

- [ ] Method is **small** (\<32 IL bytes ideally)
- [ ] Method is on a **hot path** (called frequently)
- [ ] Method has **no exception handling**
- [ ] Method is **not virtual**
- [ ] Method has **few locals** (doesn't need many registers)
- [ ] You have **measured** the impact (before/after benchmark)
- [ ] IL2CPP compatibility verified if targeting Unity

When in doubt, **don't add the attribute**. The JIT usually makes good decisions.

______________________________________________________________________

## Quick Reference

| Method Size | Hot Path? | Add AggressiveInlining?    |
| ----------- | --------- | -------------------------- |
| \<16 bytes  | Yes       | ✅ Definitely              |
| 16-32 bytes | Yes       | ✅ Probably                |
| 32-64 bytes | Yes       | ⚠️ Measure first           |
| >64 bytes   | Yes       | ❌ No (outline cold parts) |
| Any size    | No        | ❌ No                      |

______________________________________________________________________

## Resources

- [high-performance-csharp](../../high-performance-csharp/SKILL.md) — General performance patterns
- [unity-gc-patterns](../../unity-gc-patterns/SKILL.md) — Unity-specific patterns

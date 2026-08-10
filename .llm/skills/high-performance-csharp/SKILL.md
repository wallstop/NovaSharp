---
name: high-performance-csharp
description: "Implement high-performance C# for NovaSharp while preserving Lua correctness. Use for hot paths, zero-allocation work, pooling, GC pressure, spans, string building, or runtime optimization."
metadata:
  category: performance
  priority: core
  related: correctness-then-performance, allocation-traps, zstring-migration, span-optimization
---
# High-Performance C# Coding Guidelines

**PRIORITY REMINDER**: Performance is the SECOND priority after correctness. A fast implementation that breaks Lua spec compliance is REJECTED. See [correctness-then-performance](../correctness-then-performance/SKILL.md).

When writing new code for NovaSharp, prioritize **minimal allocations** and **maximum efficiency**. This interpreter runs hot paths millions of times.

**Code Samples**: [pooling-patterns](../../code-samples/pooling-patterns.md), [unity-gc-patterns](../../code-samples/unity-gc-patterns.md), [string-building](../../code-samples/string-building.md)

**Related Skills**: [allocation-traps](../allocation-traps/SKILL.md), [zstring-migration](../zstring-migration/SKILL.md), [span-optimization](../span-optimization/SKILL.md)

______________________________________________________________________

## Quick Audit Checklist

| Pattern                           | Problem                        | Fix                            |
| --------------------------------- | ------------------------------ | ------------------------------ |
| `.Where()`, `.Select()`, `.Any()` | Iterator + delegate allocation | `for` loop                     |
| `new List<T>()` in method         | Heap allocation                | `ListPool<T>.Get()`            |
| `=> localVar` in lambda           | Closure allocation             | Static lambda or explicit loop |
| `$"text {var}"` in hot path       | String allocation              | `ZString.Concat()`             |
| `.ToString()` on enum             | String allocation              | Cached string lookup           |
| `new T[]` with variable size      | Array allocation               | `SystemArrayPool<T>.Get()`     |
| Boxing struct to object           | Box allocation                 | Generic methods                |

VM opcode and ordinary Lua-call paths are stricter than general runtime code: they must be allocation-free after warmup. Scalar `LuaValue` construction, `default`, `FromNumber`, and `FromInteger` are inline and allocation-free. Use stack windows, spans, and explicit slow-path allowlists; do not add `new LuaValue[]`, `new List<LuaValue>`, or `new ScriptExecutionContext` to hot processor/call paths without updating the VM allocation guard and documenting why it is not hot.

______________________________________________________________________

## Core Principles

1. **Prefer value types** when data is small and short-lived
1. **Avoid allocations in hot paths** - use pooling, stackalloc, spans
1. **Use `readonly struct`** for immutable value types
1. **NEVER capture in closures** in hot paths - use static lambdas
1. **NEVER use LINQ** in hot paths - use explicit loops
1. **Always measure** with BenchmarkDotNet before/after

______________________________________________________________________

## Pool Selection Flowchart

```text
What kind of buffer do you need?
|
+-- LuaValue array? --> DynValueArrayPool.Get(exactSize, out array)
+-- Object array?   --> ObjectArrayPool.Get(exactSize, out array)
+-- Variable size?  --> SystemArrayPool<T>.Get(size, out array)
+-- List/Set/Dict?  --> ListPool<T>.Get(), HashSetPool<T>.Get(), etc.
+-- StringBuilder?  --> ZStringBuilder.Create()
```

See [pooling-patterns](../../code-samples/pooling-patterns.md) for detailed examples.

______________________________________________________________________

## Pool Usage Pattern (CRITICAL)

**ALWAYS use `using` with `Get()` instead of manual `Rent()`/`Return()` calls.**

```csharp
// BAD: Manual rent/return leaks on exception!
List<Instruction> jumps = ListPool<Instruction>.Rent();
DoSomethingThatMightThrow();  // If this throws, jumps is never returned!
ListPool<Instruction>.Return(jumps);

// GOOD: RAII pattern - automatic cleanup even on exception
using (ListPool<Instruction>.Get(out List<Instruction> jumps))
{
    DoSomethingThatMightThrow();
}
```

______________________________________________________________________

## Architecture Principles

| Prefer              | Over                 | Reason                              |
| ------------------- | -------------------- | ----------------------------------- |
| `readonly struct`   | `class`              | Stack-allocated, no GC pressure     |
| `static` methods    | Instance methods     | No `this` capture, enables inlining |
| Extension methods   | Utility classes      | Discoverable, fluent APIs           |
| Generic constraints | Interface parameters | Avoids boxing for value types       |

### Size Guidelines for Structs

| Size        | Recommendation                                  |
| ----------- | ----------------------------------------------- |
| \<=16 bytes | `readonly struct` preferred                     |
| 17-64 bytes | `readonly struct` OK if passed by `in` or `ref` |
| >64 bytes   | Consider `class` or pass by `ref`               |

______________________________________________________________________

## String Building

Use ZString for zero-allocation string building. See [zstring-migration](../zstring-migration/SKILL.md).

```csharp
// Safe for nested/recursive calls
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
return sb.ToString();
```

**NEVER** use `StringBuilder`, `$"..."` interpolation, or `+` in hot paths.

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Closures and Lambdas, Enum String Caching, Hash Code Implementation, Sorting Without Boxing, and later sections.

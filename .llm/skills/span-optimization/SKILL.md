---
name: span-optimization
description: "Use Span<T> and ReadOnlySpan<T> to remove avoidable arrays, substrings, and copies in NovaSharp. Use for span conversion, stackalloc, slicing, parsing, or no-allocation string work."
metadata:
  category: performance
  priority: recommended
  related: high-performance-csharp, zstring-migration
---
# Span Optimization Guidelines

This document provides guidance for using `Span<T>` and `ReadOnlySpan<T>` to eliminate unnecessary array allocations in NovaSharp.

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md) (general performance), [zstring-migration](../zstring-migration/SKILL.md) (string building)

______________________________________________________________________

## 🔴 Core Rule

**NEVER allocate arrays when spans can be used instead. Prefer stack-based or slice-based operations over heap allocations.**

______________________________________________________________________

## Why Spans?

Spans provide a view into contiguous memory without allocation:

| Operation                 | Traditional                | Span-Based                      |
| ------------------------- | -------------------------- | ------------------------------- |
| `string.Split('\n')`      | Allocates array of strings | Zero allocation enumeration     |
| `string.Substring(5, 10)` | Allocates new string       | `AsSpan(5, 10)` — no allocation |
| `array.ToArray()`         | Allocates copy             | `AsSpan()` — no allocation      |
| `text.ToCharArray()`      | Allocates char[]           | `AsSpan()` — no allocation      |

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Migration Patterns, stackalloc for Small Fixed Buffers, Span Limitations (ref struct), Validation Commands, and later sections.

---
name: zstring-migration
description: "Migrate NovaSharp hot-path string construction to ZString and ZStringBuilder without changing observable text. Use for StringBuilder, interpolation, concatenation, formatting, or zero-allocation string work."
metadata:
  category: performance
  priority: recommended
  related: high-performance-csharp, span-optimization
---
# ZString Migration Guidelines

This document provides guidance for migrating string operations to ZString for zero-allocation string building in NovaSharp.

**Related Skills**: [high-performance-csharp](../high-performance-csharp/SKILL.md) (general performance), [span-optimization](../span-optimization/SKILL.md) (span-based parsing)

______________________________________________________________________

## 🔴 Core Rule

**NEVER use string interpolation (`$"..."`), concatenation (`+`), or `StringBuilder` in hot paths. Always use ZString.**

______________________________________________________________________

## Why ZString?

NovaSharp uses [ZString](https://github.com/Cysharp/ZString) (Cysharp.Text) for zero-allocation string building:

| Operation         | Traditional                                | ZString                                 |
| ----------------- | ------------------------------------------ | --------------------------------------- |
| `$"Error: {msg}"` | Allocates intermediate strings             | Zero allocation with `ZStringBuilder`   |
| `"a" + "b" + "c"` | Multiple allocations                       | Zero allocation with `ZString.Concat()` |
| `StringBuilder`   | Allocates StringBuilder + internal buffers | Uses pooled buffers                     |

______________________________________________________________________

## API Reference

### ZStringBuilder (Recommended for most cases)

```csharp
using Cysharp.Text;
using WallstopStudios.NovaSharp.Interpreter.DataStructs;

// Standard usage - safe for nested/recursive calls
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
sb.Append(": ");
sb.Append(message);
return sb.ToString();
```

### ZStringBuilder Variants

| Method                             | When to Use                                     |
| ---------------------------------- | ----------------------------------------------- |
| `ZStringBuilder.Create()`          | Default - safe for nested calls (ArrayPool)     |
| `ZStringBuilder.CreateNested()`    | Alias for Create() - explicit documentation     |
| `ZStringBuilder.CreateNonNested()` | Hot non-nested paths only (ThreadStatic buffer) |
| `ZStringBuilder.CreateUtf8()`      | UTF-8 output (network, file I/O)                |

### ZString.Concat (Simple concatenation)

```csharp
// For 2-4 elements, ZString.Concat is cleaner than ZStringBuilder
return ZString.Concat("\"", input, "\"");
return ZString.Concat(prefix, ":", suffix);
```

### ZStringBuilder.Join (Joining collections)

```csharp
// Join with separator
string result = ZStringBuilder.Join(':', parameterNames);
string result = ZStringBuilder.Join(", ", values);
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Migration Patterns, Span-Based String Processing, When NOT to Use ZString, Validation Commands, and later sections.

# ZString Migration Guidelines

This document provides guidance for migrating string operations to ZString for zero-allocation string building in NovaSharp.

**Related Skills**: [high-performance-csharp](high-performance-csharp.md) (general performance), [span-optimization](span-optimization.md) (span-based parsing)

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

## Migration Patterns

### Pattern 1: String Interpolation → ZStringBuilder

```csharp
// ❌ BEFORE: Allocates
return $"bad argument #{argNum} to '{funcName}' ({message})";

// ✅ AFTER: Zero allocation
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("bad argument #");
sb.Append(argNum);
sb.Append(" to '");
sb.Append(funcName);
sb.Append("' (");
sb.Append(message);
sb.Append(')');
return sb.ToString();
```

### Pattern 2: String Concatenation → ZString.Concat

```csharp
// ❌ BEFORE: Allocates
return "\"" + value + "\"";

// ✅ AFTER: Zero allocation
return ZString.Concat("\"", value, "\"");
```

### Pattern 3: StringBuilder → ZStringBuilder

```csharp
// ❌ BEFORE: Allocates StringBuilder
var sb = new StringBuilder();
sb.Append("Header\n");
foreach (var item in items)
{
    sb.Append(item);
    sb.Append('\n');
}
return sb.ToString();

// ✅ AFTER: Zero allocation
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Header\n");
foreach (var item in items)
{
    sb.Append(item);
    sb.Append('\n');
}
return sb.ToString();
```

### Pattern 4: String.Join → ZStringBuilder.Join

```csharp
// ❌ BEFORE: Allocates array and result
return string.Join(", ", items.Select(x => x.Name));

// ✅ AFTER: Zero allocation (if items is already materialized)
return ZStringBuilder.Join(", ", names);
```

### Pattern 5: Multi-line with formatting

```csharp
// ❌ BEFORE: Multiple allocations
return $"Function: {name}\n" +
       $"  Parameters: {paramCount}\n" +
       $"  Returns: {returnType}";

// ✅ AFTER: Single allocation at ToString()
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Function: ");
sb.AppendLine(name);
sb.Append("  Parameters: ");
sb.AppendLine(paramCount);
sb.Append("  Returns: ");
sb.Append(returnType);
return sb.ToString();
```

### Pattern 6: Enum ToString → Cached String Lookup

```csharp
// ❌ BEFORE: enum.ToString() allocates
sb.Append(tokenType.ToString());
sb.Append(opCode.ToString().ToUpperInvariant()); // Double allocation!

// ✅ AFTER: Use cached string lookups
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));

// ✅ AFTER: For DataType, use the extension method
sb.Append(dataType.ToLuaDebuggerString());
```

Available enum string caches:

- `TokenTypeStrings.GetName(TokenType)` — Lexer token types
- `OpCodeStrings.GetName(OpCode)` / `GetUpperName(OpCode)` — VM opcodes
- `SymbolRefTypeStrings.GetName(SymbolRefType)` — Symbol reference types
- `ModLoadStateStrings.GetName(ModLoadState)` — Mod loading states
- `DebuggerActionTypeStrings.GetName(DebuggerAction.ActionType)` — Debugger actions
- `dataType.ToLuaDebuggerString()` — DataType extension method

For other enums, use the generic cache:

```csharp
string name = EnumStringCache<MyEnum>.GetName(value);
```

______________________________________________________________________

## Span-Based String Processing

For span-based parsing and splitting, see [span-optimization.md](span-optimization.md).

______________________________________________________________________

## When NOT to Use ZString

1. **Compile-time constants**: `const string` and string literals are interned
1. **nameof()**: Already zero-allocation
1. **Single string return**: Just return the string directly
1. **Cold paths**: Startup code, error paths executed once

```csharp
// These are fine as-is:
const string ErrorPrefix = "Error: ";  // Interned
throw new ArgumentNullException(nameof(value));  // nameof is compile-time
return existingString;  // No allocation
```

______________________________________________________________________

## Validation Commands

```bash
# Find string interpolation in runtime code (candidates for migration)
rg '\$"' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs

# Find string concatenation with + operator
rg '"\s*\+\s*[^"]+\s*\+\s*"' src/runtime/ --type cs

# Find StringBuilder usage
rg 'new StringBuilder\(\)' src/runtime/ --type cs
rg 'StringBuilder\s+\w+\s*=' src/runtime/ --type cs

# Verify ZString usage
rg 'ZStringBuilder\.Create' src/runtime/ --type cs -c
```

______________________________________________________________________

## Related Documentation

- [high-performance-csharp.md](high-performance-csharp.md) — General performance guidelines
- [DataStructs/ZStringBuilder.cs](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ZStringBuilder.cs) — ZStringBuilder wrapper implementation

# ZString Migration Guidelines Reference

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

For span-based parsing and splitting, see [span-optimization.md](../../span-optimization/SKILL.md).

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

- [high-performance-csharp.md](../../high-performance-csharp/SKILL.md) — General performance guidelines
- [DataStructs/ZStringBuilder.cs](../../../../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/ZStringBuilder.cs) — ZStringBuilder wrapper implementation

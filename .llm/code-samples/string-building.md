# String Building Patterns

Zero-allocation string building using ZString in NovaSharp.

______________________________________________________________________

## ZStringBuilder

Use `ZStringBuilder.Create()` for building strings. Safe for nested/recursive calls.

```csharp
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("Error at line ");
sb.Append(lineNumber);
sb.Append(": ");
sb.Append(message);
return sb.ToString();
```

### With formatting

```csharp
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.AppendFormat("Value: {0}, Count: {1}", value, count);
return sb.ToString();
```

______________________________________________________________________

## ZString.Concat

For simple 2-4 element concatenation:

```csharp
// Simple cases - direct concatenation
return ZString.Concat("\"", input, "\"");
return ZString.Concat(prefix, name, suffix);
return ZString.Concat("Error: ", message);
```

______________________________________________________________________

## Patterns to Avoid

```csharp
// BAD: String interpolation allocates
string result = $"Error at line {lineNumber}: {message}";

// BAD: StringBuilder allocates
StringBuilder sb = new StringBuilder();
sb.Append("Error at line ");
sb.Append(lineNumber);
return sb.ToString();

// BAD: String concatenation in loops
string result = "";
foreach (var item in items)
{
    result += item.ToString();  // Allocates on every iteration!
}

// BAD: string.Format allocates
string result = string.Format("Value: {0}", value);
```

______________________________________________________________________

## Good Patterns

```csharp
// GOOD: ZStringBuilder for complex building
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
foreach (var item in items)
{
    sb.Append(item.ToString());
    sb.Append(", ");
}
return sb.ToString();

// GOOD: ZString.Concat for simple cases
return ZString.Concat("Value: ", value.ToString());

// GOOD: Pre-computed lookup for repeated strings
private static readonly string[] EscapeSequences = new string[16]
{
    "\\000", "\\001", "\\002", "\\003", "\\004", "\\005", "\\006", "\\007",
    "\\008", "\\009", "\\010", "\\011", "\\012", "\\013", "\\014", "\\015",
};
string escaped = EscapeSequences[charValue];
```

______________________________________________________________________

## Enum String Caching

Never call `.ToString()` on enums - use cached lookups:

```csharp
// BAD: Allocates on every call
sb.Append(tokenType.ToString());
sb.Append(opCode.ToString().ToUpperInvariant());  // Double allocation!

// GOOD: Zero allocation
sb.Append(TokenTypeStrings.GetName(tokenType));
sb.Append(OpCodeStrings.GetUpperName(opCode));  // Pre-cached uppercase
sb.Append(dataType.ToLuaDebuggerString());      // Extension method
```

### Available caches

| Enum            | Cache                  | Methods                                |
| --------------- | ---------------------- | -------------------------------------- |
| `TokenType`     | `TokenTypeStrings`     | `GetName()`                            |
| `OpCode`        | `OpCodeStrings`        | `GetName()`, `GetUpperName()`          |
| `SymbolRefType` | `SymbolRefTypeStrings` | `GetName()`                            |
| `ModLoadState`  | `ModLoadStateStrings`  | `GetName()`                            |
| `DataType`      | `LuaTypeExtensions`    | `ToLuaDebuggerString()`                |
| Other enums     | `EnumStringCache<T>`   | `GetName()`, `GetNameLowerInvariant()` |

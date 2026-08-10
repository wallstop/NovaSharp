# Codebase Navigation Reference

## Interpreter Pipeline

Each stage can be debugged independently:

| Stage    | Location                    | What It Does          |
| -------- | --------------------------- | --------------------- |
| Lexer    | `Tree/Lexer/`               | Source text -> tokens |
| Parser   | `Tree/`                     | Tokens -> AST         |
| Compiler | `Execution/VM/ByteCode.cs`  | AST -> bytecode       |
| VM       | `Execution/VM/Processor.cs` | Execute bytecode      |
| Stdlib   | `CoreLib/`                  | Built-in functions    |

### Key Files

| File                                  | Purpose                  |
| ------------------------------------- | ------------------------ |
| `Api/LuaValue.cs`                     | Universal value type     |
| `DataTypes/Table.cs`                  | Lua table implementation |
| `Execution/ScriptExecutionContext.cs` | Execution state          |

______________________________________________________________________

## Debugging Techniques

### 1. Minimal Reproduction

```csharp
[Test]
public async Task MinimalReproduction()
{
    Script script = new Script();
    LuaValue result = script.DoString("return <failing code>");
}
```

### 2. Compare with Reference Lua

**Reference Lua output is the ONLY acceptable expected result.**

```bash
for v in 5.1 5.2 5.3 5.4; do
    echo "=== Lua $v ==="
    lua$v -e "print(<test code>)"
done

# Compare with NovaSharp
dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Cli -e "print(<test code>)"
```

### 3. Inspect LuaValue

```csharp
LuaValue value = script.DoString("return something");
Console.WriteLine($"Kind: {value.Kind}");
Console.WriteLine($"Value: {value}");
```

______________________________________________________________________

## Common Bug Patterns

| Symptom               | Stage       | Check                      |
| --------------------- | ----------- | -------------------------- |
| "unexpected token"    | Lexer       | Token boundaries, keywords |
| "syntax error"        | Parser      | Operator precedence        |
| Wrong result          | Compiler/VM | Bytecode, stack operations |
| "attempt to call nil" | VM/stdlib   | Function registration      |
| Type mismatch         | VM          | Type coercion rules        |

______________________________________________________________________

## Quick Reference

```bash
# Find files
fd "pattern"              # Files matching pattern
fd -e cs                  # All .cs files

# Search content
rg "pattern"              # Search all files
rg "pattern" --type cs    # C# files only
rg -l "pattern"           # List matching files

# View files
bat --paging=never file   # View with highlighting

# Explore structure
eza --tree --level=2 src/
tokei src/                # Code statistics
```

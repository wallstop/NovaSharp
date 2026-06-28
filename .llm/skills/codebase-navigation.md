______________________________________________________________________

triggers:

- "search codebase"
- "find code"
- "grep"
- "find file"
- "debug interpreter"
- "pipeline"
  category: workflow
  related:
- lua-spec-verification
- test-failure-investigation
  priority: recommended

______________________________________________________________________

# Skill: Codebase Navigation

**When to use**: Finding code, patterns, usages, and debugging the interpreter pipeline.

**Related Skills**: [lua-spec-verification](lua-spec-verification.md), [test-failure-investigation](test-failure-investigation.md)

______________________________________________________________________

## Modern CLI Tools

The devcontainer includes fast CLI tools on the `PATH`:

| Tool  | Replaces | Key Advantage                         |
| ----- | -------- | ------------------------------------- |
| `rg`  | `grep`   | 10-100x faster, respects `.gitignore` |
| `fd`  | `find`   | Intuitive syntax, colorized           |
| `bat` | `cat`    | Syntax highlighting, line numbers     |
| `eza` | `ls`     | Git status, tree view                 |

______________________________________________________________________

## ripgrep (rg) - Code Search

```bash
# Basic search
rg "pattern"                    # All files
rg "pattern" --type cs          # C# files only
rg -C 3 "pattern"               # With context lines

# File filtering
rg "pattern" -g "*.cs"          # Only .cs files
rg "pattern" -g "!*Tests*"      # Exclude test files
rg "pattern" src/runtime/       # Specific directory

# Results
rg -l "pattern"                 # List matching files
rg -c "pattern"                 # Count matches
rg -o "DynValue\.\w+"           # Show only matched text
```

______________________________________________________________________

## fd - Find Files

```bash
fd "pattern"                    # Files matching pattern
fd -e cs                        # All .cs files
fd -e lua src/tests/            # Lua files in tests
fd -t d "Tests"                 # Directories only
```

______________________________________________________________________

## bat - View Files

**CRITICAL**: Always use `--paging=never` in scripts:

```bash
bat --paging=never file.cs      # Safe for scripts
bat --paging=never -r 100:150   # Lines 100-150
```

______________________________________________________________________

## Common Workflows

### Find All Usages of a Type

```bash
rg "DynValue" --type cs -l      # Files using DynValue
rg "new DynValue" --type cs     # Instantiations
rg "DynValue\.(NewString|NewNumber)" --type cs  # Factory methods
```

### Find Method Implementations

```bash
rg "public.*DoString" --type cs
rg "override.*ToString" --type cs
```

### Search Tests

```bash
rg "\[Test\]" -A 3 --type cs | rg -i "floor"
fd "\.lua$" src/tests/ | xargs rg "@lua-versions"
```

______________________________________________________________________

## NovaSharp-Specific Searches

### Version-Gated Code

```bash
rg "LuaCompatibilityVersion\.(Lua51|Lua52|Lua53|Lua54)" --type cs
rg "case LuaCompatibilityVersion" --type cs -A 5
```

### Module Implementations

```bash
rg "\[NovaSharpModule" --type cs -A 2
rg "Namespace = \"math\"" --type cs -B 5 -A 20
```

### Allocation Patterns

```bash
rg "\.Where\(|\.Select\(|\.Any\(" src/runtime/
rg "new List<|new Dictionary<" --type cs
```

______________________________________________________________________

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
| `DataTypes/DynValue.cs`               | Universal value type     |
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
    DynValue result = script.DoString("return <failing code>");
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

### 3. Inspect DynValue

```csharp
DynValue value = script.DoString("return something");
Console.WriteLine($"Type: {value.Type}");
Console.WriteLine($"Value: {value.ToDebugPrintString()}");
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

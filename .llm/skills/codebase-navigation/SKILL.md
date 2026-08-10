---
name: codebase-navigation
description: "Navigate NovaSharp source, tests, fixtures, and the interpreter pipeline with repository-preferred search tools. Use when finding code, patterns, usages, ownership, or debugging locations."
metadata:
  category: workflow
  priority: recommended
  related: lua-spec-verification, test-failure-investigation
---
# Skill: Codebase Navigation

**Related Skills**: [lua-spec-verification](../lua-spec-verification/SKILL.md), [test-failure-investigation](../test-failure-investigation/SKILL.md)

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
rg -o "LuaValue\.\w+"           # Show only matched text
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
rg "LuaValue" --type cs -l      # Files using LuaValue
rg "new LuaValue" --type cs     # Instantiations
rg "LuaValue\.(FromString|FromNumber)" --type cs  # Public factory methods
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

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Interpreter Pipeline, Debugging Techniques, Common Bug Patterns, Quick Reference.

# Skill: Search Codebase Effectively

**When to use**: Finding code, patterns, usages, or understanding the codebase structure.

**Related Skills**: [debugging-interpreter](debugging-interpreter.md) (tracing execution), [lua-spec-verification](lua-spec-verification.md) (finding implementations)

______________________________________________________________________

## 🔴 Modern CLI Tools

The devcontainer includes modern, fast CLI tools that are **pre-installed on the `PATH`**. Use these instead of legacy tools:

| Modern Tool | Replaces | Key Advantage                         |
| ----------- | -------- | ------------------------------------- |
| `rg`        | `grep`   | 10-100x faster, respects `.gitignore` |
| `fd`        | `find`   | Intuitive syntax, colorized output    |
| `bat`       | `cat`    | Syntax highlighting, line numbers     |
| `eza`       | `ls`     | Git status, tree view, colorized      |
| `delta`     | `diff`   | Side-by-side, syntax highlighting     |
| `tokei`     | `cloc`   | Fast code statistics                  |
| `sd`        | `sed`    | Intuitive find-and-replace            |

______________________________________________________________________

## ripgrep (rg) — Code Search

### Basic Usage

```bash
# Search for pattern in current directory (recursively)
rg "pattern"

# Search with context lines
rg -C 3 "pattern"          # 3 lines before and after
rg -B 5 -A 2 "pattern"     # 5 before, 2 after

# Case insensitive
rg -i "pattern"

# Regex
rg -e "pattern\d+"
```

### File Type Filtering

```bash
# Search only in C# files
rg "DynValue" --type cs

# Search only in specific file types
rg "function" --type lua
rg "pattern" --type md

# Exclude file types
rg "pattern" --type-not cs

# List available types
rg --type-list
```

### Path Filtering

```bash
# Search in specific directory
rg "pattern" src/runtime/

# Search in specific file
rg "pattern" src/runtime/DataTypes/DynValue.cs

# Glob patterns
rg "pattern" -g "*.cs"              # Only .cs files
rg "pattern" -g "!*Tests*"          # Exclude test files
rg "pattern" -g "src/**/*.cs"       # Only src/*.cs
```

### Advanced Options

```bash
# Only show filenames with matches
rg -l "pattern"

# Count matches per file
rg -c "pattern"

# Show only matched text (not full lines)
rg -o "DynValue\.\w+"

# Word boundaries
rg -w "value"                  # Matches "value" not "DynValue"

# Fixed strings (no regex interpretation)
rg -F "array[0]"

# Include .gitignored files (e.g., node_modules)
rg -u "pattern"                # -u for unrestricted

# Multi-pattern OR search
rg "pattern1|pattern2|pattern3"

# AND search (both must be on same line)
rg "pattern1.*pattern2|pattern2.*pattern1"
```

______________________________________________________________________

## fd — Find Files

### Basic Usage

```bash
# Find files by name pattern (regex by default)
fd "pattern"

# Find files with exact name
fd -g "DynValue.cs"          # -g for glob

# Find directories only
fd -t d "Tests"

# Find files only
fd -t f "\.cs$"
```

### Path and Extension Filtering

```bash
# Find by extension
fd -e cs                      # All .cs files
fd -e lua                     # All .lua files

# Search in specific directory
fd "pattern" src/runtime/

# Exclude directories
fd "pattern" --exclude node_modules --exclude bin

# Max depth
fd "pattern" --max-depth 2
```

### Execution

```bash
# Execute command on each result
fd -e cs -x bat {}           # bat each C# file

# Execute with all results at once
fd -e cs -X rg "DynValue"    # Search all C# files

# Useful combinations
fd -e cs | xargs rg "pattern"
```

______________________________________________________________________

## bat — View Files

### ⚠️ CRITICAL: Always Use `--paging=never`

In scripts and non-interactive contexts, `bat` defaults to pager mode which **will hang waiting for keyboard input**:

```bash
# ❌ BAD: Will hang in scripts/agents
bat file.cs

# ✅ GOOD: Safe for scripts and automation
bat --paging=never file.cs
```

### Basic Usage

```bash
# View file with syntax highlighting
bat --paging=never src/runtime/DataTypes/DynValue.cs

# View specific line range
bat --paging=never -r 100:150 file.cs       # Lines 100-150

# View multiple files
bat --paging=never file1.cs file2.cs

# Plain mode (no decorations)
bat --paging=never -p file.cs
```

### Language and Theme

```bash
# Force language detection
bat --paging=never -l cs file.txt

# List themes
bat --list-themes

# Use specific theme
bat --paging=never --theme="TwoDark" file.cs
```

______________________________________________________________________

## eza — List Files

### Basic Usage

```bash
# Better ls
eza

# Long format with git status
eza -la --git

# Tree view
eza --tree

# Tree with depth limit
eza --tree --level=2 src/

# Show only directories
eza -D

# Sort by modification time
eza -la --sort=modified
```

______________________________________________________________________

## Common Workflows

### Find All Usages of a Type

```bash
# Find where DynValue is used
rg "DynValue" --type cs -l

# Find instantiation patterns
rg "new DynValue" --type cs

# Find method calls
rg "DynValue\.(NewString|NewNumber|NewTable)" --type cs
```

### Find Method Implementations

```bash
# Find method definitions
rg "public.*DoString" --type cs

# Find interface implementations
rg "class.*: IDisposable" --type cs

# Find override methods
rg "override.*ToString" --type cs
```

### Search for Patterns in Tests

```bash
# Find all tests for a feature
rg "\[Test\]" -A 3 --type cs | rg -i "floor"

# Find tests by name pattern
rg "public async Task.*Math.*Floor" --type cs

# Find test fixtures
fd "\.lua$" src/tests/ | xargs rg "@lua-versions"
```

### Find Files by Content

```bash
# Find C# files containing a pattern
rg -l "LuaCompatibilityVersion" --type cs

# Find and view matching files
rg -l "ZStringBuilder" --type cs | xargs bat --paging=never

# Find files with multiple patterns (both must exist in file)
rg -l "pattern1" --type cs | xargs rg -l "pattern2"
```

### Explore Directory Structure

```bash
# Quick tree view of source
eza --tree --level=3 src/

# Find all test files
fd "Tests\.cs$" src/

# Find all Lua fixtures
fd "\.lua$" src/tests/

# Count files by type
tokei src/
```

______________________________________________________________________

## NovaSharp-Specific Searches

### Find Lua Version-Gated Code

```bash
# Find version checks
rg "LuaCompatibilityVersion\.(Lua51|Lua52|Lua53|Lua54|Lua55)" --type cs

# Find version-specific implementations
rg "case LuaCompatibilityVersion" --type cs -A 5

# Find version attributes in tests
rg "\[LuaVersions" --type cs
```

### Find Module Implementations

```bash
# Find module definitions
rg "\[NovaSharpModule" --type cs -A 2

# Find module methods
rg "\[NovaSharpModuleMethod" --type cs -A 2

# Find specific module
rg "Namespace = \"math\"" --type cs -B 5 -A 20
```

### Find Pooling Usage

```bash
# Find pool usage
rg "Pool<.*\.Get\(" --type cs

# Find pooled resources
rg "using.*Pool.*\.Get" --type cs

# Find potential pooling candidates
rg "new List<|new Dictionary<|new HashSet<" --type cs
```

### Find Allocation Patterns

```bash
# Find string concatenation in hot paths
rg "\$\"" src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/

# Find LINQ in runtime code
rg "\.Where\(|\.Select\(|\.Any\(" src/runtime/

# Find new array allocations
rg "new \w+\[" --type cs src/runtime/
```

______________________________________________________________________

## Search Tips

### Effective Pattern Writing

| Goal              | Pattern                         |
| ----------------- | ------------------------------- |
| Word boundary     | `\bword\b` or `rg -w word`      |
| Start of line     | `^pattern`                      |
| End of line       | `pattern$`                      |
| Method definition | `(public\|private).*methodName` |
| Class definition  | `class\s+ClassName`             |
| Type usage        | `Type\.` or `: Type`            |
| String literal    | `"[^"]*pattern[^"]*"`           |

### Narrowing Results

1. **Start broad**: `rg "pattern"` in entire codebase
1. **Filter by type**: Add `--type cs`
1. **Filter by path**: Add `src/runtime/`
1. **Add context**: Add `-C 3` for surrounding lines
1. **Refine pattern**: Make regex more specific

### When rg Finds Too Much

```bash
# Exclude test files
rg "pattern" --type cs -g "!*Tests*" -g "!*Test*"

# Search only in specific namespace
rg "pattern" src/runtime/WallstopStudios.NovaSharp.Interpreter/

# Exclude comments (rough)
rg "pattern" --type cs | rg -v "^\s*//"
```

______________________________________________________________________

## Quick Reference Card

```bash
# Find files
fd "pattern"              # Files matching pattern
fd -e cs                  # All .cs files
fd -t d "name"            # Directories only

# Search content  
rg "pattern"              # Search all files
rg "pattern" --type cs    # C# files only
rg -l "pattern"           # List matching files
rg -c "pattern"           # Count matches

# View files
bat --paging=never file   # View with highlighting
bat -r 10:20 file         # Lines 10-20

# List/explore
eza --tree --level=2      # Tree view
eza -la --git             # Long list with git

# Replace
sd "old" "new" file.cs    # Replace in file
```

______________________________________________________________________

## Resources

- [ripgrep User Guide](https://github.com/BurntSushi/ripgrep/blob/master/GUIDE.md)
- [fd README](https://github.com/sharkdp/fd)
- [bat README](https://github.com/sharkdp/bat)
- [eza README](https://github.com/eza-community/eza)

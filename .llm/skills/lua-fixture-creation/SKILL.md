---
name: lua-fixture-creation
description: "Create standalone Lua fixtures and corpus metadata for cross-interpreter verification. Use when adding .lua tests, version-specific fixtures, expected errors, or NovaSharp-only interop fixtures."
metadata:
  category: lua
  priority: core
  related: lua-comparison-harness, lua-spec-verification, tunit-test-writing, exhaustive-test-coverage
---
# Skill: Creating Lua Test Fixtures

**Code Samples**: [lua-patterns](../../code-samples/lua-patterns.md)

**Related Skills**: [lua-comparison-harness](../lua-comparison-harness/SKILL.md), [lua-spec-verification](../lua-spec-verification/SKILL.md), [tunit-test-writing](../tunit-test-writing/SKILL.md)

______________________________________________________________________

## Reference Lua is the Source of Truth

1. **Run against reference Lua FIRST** - Output defines expected behavior
1. **NovaSharp must match** - Any difference means NovaSharp has a bug
1. **NEVER adjust fixtures to match NovaSharp** - Fix the interpreter instead
1. **Document version differences** - If Lua 5.1 and 5.4 differ, that's TWO expected behaviors

```bash
# Before committing ANY fixture
lua5.1 fixture.lua
lua5.2 fixture.lua
lua5.3 fixture.lua
lua5.4 fixture.lua
```

______________________________________________________________________

## Complete Test Workflow

Every test requires **THREE deliverables**:

1. **C# TUnit test** - Runs against NovaSharp runtime
1. **`.lua` fixture file** - Standalone Lua for cross-interpreter verification
1. **Regenerate corpus** - Run after adding fixtures

```bash
# 1. Create C# test
# 2. Create .lua fixture with metadata
# 3. Verify fixture runs against reference Lua
lua5.4 path/to/fixture.lua

# 4. Regenerate corpus
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py

# 5. Run tests
./scripts/test/quick.sh TestMethodName
```

______________________________________________________________________

## Required Metadata Header

**CRITICAL**: Metadata MUST start at LINE 1 with NO blank lines before it.

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/<Project>/<TestClass>.cs:<Line>
-- @test: <TestClass>.<TestMethod>

-- Your Lua code here
```

**The harness parser STOPS at the FIRST non-comment line.** Blank lines before metadata cause it to be SILENTLY IGNORED.

### Only These Fields Are Parsed

| Field             | Required | Description                    |
| ----------------- | -------- | ------------------------------ |
| `@lua-versions`   | YES      | Which Lua versions to test     |
| `@novasharp-only` | YES      | Skip reference Lua comparison? |
| `@expects-error`  | YES      | Should the script error?       |
| `@source`         | Rec.     | Path to C# test (for tracing)  |
| `@test`           | Rec.     | Test class and method name     |

**INVALID fields (silently ignored)**: `@min-version`, `@max-version`, `@versions`, `@name`, `@description`, `@expected-output`, `@error-pattern`

______________________________________________________________________

## @lua-versions Format

**Prefer range syntax** - auto-includes future versions:

```lua
-- @lua-versions: all         -- All versions (5.1+)
-- @lua-versions: 5.3+        -- 5.3 and above
-- @lua-versions: 5.1-5.2     -- 5.1 through 5.2
```

**Avoid explicit lists** (not future-proof):

```lua
-- AVOID: @lua-versions: 5.3, 5.4, 5.5  -- Use 5.3+ instead
```

______________________________________________________________________

## Version-Specific Features Quick Reference

See [lua-patterns](../../code-samples/lua-patterns.md#version-specific-features) for full list.

| Feature             | Minimum Version |
| ------------------- | --------------- |
| Floor division `//` | 5.3+            |
| Bitwise operators   | 5.3+            |
| `utf8` library      | 5.3+            |
| `math.type`         | 5.3+            |
| `goto`/`::label::`  | 5.2+            |
| `_ENV`              | 5.2+            |
| `table.pack/unpack` | 5.2+            |

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Fixture Examples, Directory Structure, Validation Checklist.

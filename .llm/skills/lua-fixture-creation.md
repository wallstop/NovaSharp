______________________________________________________________________

triggers:

- "lua fixture"
- "lua test file"
- "create .lua test"
- "lua version test"
  category: lua
  related:
- lua-comparison-harness
- lua-spec-verification
- tunit-test-writing
- exhaustive-test-coverage
  priority: core

______________________________________________________________________

# Skill: Creating Lua Test Fixtures

**When to use**: Creating `.lua` test files for cross-interpreter verification.

**Code Samples**: [lua-patterns](../code-samples/lua-patterns.md)

**Related Skills**: [lua-comparison-harness](lua-comparison-harness.md), [lua-spec-verification](lua-spec-verification.md), [tunit-test-writing](tunit-test-writing.md)

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

See [lua-patterns](../code-samples/lua-patterns.md#version-specific-features) for full list.

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

## Fixture Examples

### Basic (all versions)

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local result = math.floor(3.7)
assert(result == 3, "Expected 3, got " .. tostring(result))
print("PASS")
```

### Version-Specific (5.3+)

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false

local result = 7 // 3
assert(result == 2, "Expected 2, got " .. tostring(result))
print("PASS")
```

### Error-Expecting

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true

return 1 // 0  -- Should error
```

### NovaSharp-Only (CLR interop)

```lua
-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local Math = clr.import("System.Math")
assert(Math.Abs(-42) == 42)
print("PASS")
```

______________________________________________________________________

## Directory Structure

```text
src/tests/.../LuaFixtures/
├── MathModuleTUnitTests/
│   ├── FloorReturnsInteger.lua
│   └── FloorHandlesNegatives.lua
└── StringModuleTUnitTests/
    └── SubstringBasic.lua
```

______________________________________________________________________

## Validation Checklist

- [ ] Metadata at LINE 1 (no blank lines before)
- [ ] All three required fields present
- [ ] `@lua-versions` correct (use range syntax)
- [ ] `@novasharp-only: true` if using CLR/extensions
- [ ] `@expects-error` matches behavior
- [ ] Runs on reference Lua: `lua5.4 fixture.lua`
- [ ] Output is deterministic
- [ ] Corpus regenerated: `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`

# Lua Patterns

Patterns for Lua fixtures and cross-version compatibility in NovaSharp.

______________________________________________________________________

## Fixture Metadata Format

All fixtures must start with metadata at line 1 (no blank lines before):

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/ExampleTests.cs:42
-- @test: ExampleTests.TestMethod

-- Test code follows immediately
local result = test_function()
print("PASS")
```

______________________________________________________________________

## Version Annotations

### Recommended (future-proof)

```lua
-- @lua-versions: all      -- All versions (5.1+)
-- @lua-versions: 5.3+     -- 5.3 and above
-- @lua-versions: 5.1-5.2  -- 5.1 through 5.2
```

### Explicit (only when necessary)

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5  -- All versions explicitly
-- @lua-versions: 5.1                       -- Single version only
```

______________________________________________________________________

## Common Fixture Patterns

### Basic assertion

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local result = math.floor(3.7)
assert(result == 3, "Expected 3, got " .. tostring(result))
print("PASS")
```

### Error-expecting

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true

-- This should error
return 1 // 0
```

### Error handling with pcall

```lua
-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false

local success, err = pcall(function()
    return 1 % 0
end)

if not success then
    print("PASS: Got expected error")
else
    print("FAIL: Expected error but got success")
end
```

### NovaSharp-only (CLR interop)

```lua
-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local Math = clr.import("System.Math")
local result = Math.Abs(-42)
assert(result == 42)
print("PASS")
```

______________________________________________________________________

## Version-Specific Features

### Lua 5.4+ Only

- `<const>` and `<close>` attributes
- `warn()` function

### Lua 5.3+ Only

- Integer division `//`
- Bitwise operators `&`, `|`, `~`, `<<`, `>>`
- `utf8` library
- `math.tointeger`, `math.type`, `math.ult`
- `string.pack`, `string.unpack`, `string.packsize`
- `table.move`

### Lua 5.2+ Only

- `goto` statement and `::label::`
- `_ENV` variable
- `bit32` library
- `rawlen()` function
- `table.pack`, `table.unpack`
- `load()` with string argument
- Hex float literals (`0x1.5p3`)

### Lua 5.1 Only (deprecated later)

- `table.getn`, `table.setn`
- `math.mod` (use `math.fmod`)
- `string.gfind` (use `string.gmatch`)
- `loadstring` (use `load` in 5.2+)

______________________________________________________________________

## Invalid Metadata (Never Use)

These are NOT parsed and will be silently ignored:

```lua
-- WRONG: @min-version: 5.3      -- Not recognized
-- WRONG: @max-version: 5.2      -- Not recognized
-- WRONG: @versions: 5.2, 5.3    -- Not recognized
-- WRONG: @name: TestName        -- Not recognized
-- WRONG: @description: ...      -- Not recognized
```

Only use: `@lua-versions`, `@novasharp-only`, `@expects-error`

______________________________________________________________________

## Validation Checklist

Before committing fixtures:

1. Metadata starts at line 1 (no blank lines before)
1. All three required fields present
1. Runs against reference Lua: `lua5.4 path/to/fixture.lua`
1. Output is deterministic (no timestamps, addresses)
1. Regenerate corpus: `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`

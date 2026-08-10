# Creating Lua Test Fixtures Reference

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

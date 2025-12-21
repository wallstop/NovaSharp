# Skill: Creating Lua Test Fixtures

**When to use**: Creating `.lua` test files for cross-interpreter verification.

______________________________________________________________________

## 🔴 Critical: Required Metadata Header

Every `.lua` fixture file **MUST** start with a metadata block for the test runner to parse it correctly. Files without this header will not be properly filtered or may produce false comparison failures.

### Minimal Required Template

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/<TestProject>/<TestClass>.cs:<LineNumber>
-- @test: <TestClass>.<TestMethod>

-- Your Lua code here
```

### Full Template (with all optional fields)

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:42
-- @test: MathModuleTUnitTests.TestFloor
-- @compat-notes: Optional notes about version-specific behavior

-- Test: <brief description of what this tests>
-- Reference: Lua 5.4 Reference Manual §3.4.1

local result = math.floor(3.7)
print(result)  -- Output: 3
```

______________________________________________________________________

## Metadata Fields Reference

### Required Fields (parsed by test runner)

| Field             | Required    | Description                                      | Parsed By                                                |
| ----------------- | ----------- | ------------------------------------------------ | -------------------------------------------------------- |
| `@lua-versions`   | **YES**     | Lua versions this fixture is compatible with     | `run-lua-fixtures-parallel.py`, `compare-lua-outputs.py` |
| `@novasharp-only` | **YES**     | Whether fixture uses NovaSharp-specific features | `run-lua-fixtures-parallel.py`, `compare-lua-outputs.py` |
| `@expects-error`  | **YES**     | Whether the test expects a runtime error         | `run-lua-fixtures-parallel.py`                           |
| `@source`         | Recommended | Path to C# source file (for traceability)        | Corpus extractor                                         |
| `@test`           | Recommended | Test class and method name                       | Corpus extractor                                         |
| `@compat-notes`   | Optional    | Notes about version-specific behavior            | Documentation only                                       |

### `@lua-versions` Format

```lua
-- All versions (comma-separated list):
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5

-- Range syntax (version and above):
-- @lua-versions: 5.3+

-- Specific versions only:
-- @lua-versions: 5.2, 5.3

-- NovaSharp-only (skip reference Lua comparison):
-- @lua-versions: novasharp-only
```

### `@novasharp-only` Values

```lua
-- Standard Lua code (compare against reference Lua):
-- @novasharp-only: false

-- NovaSharp extensions (skip reference Lua comparison):
-- @novasharp-only: true
```

Set to `true` when the fixture uses:

- CLR interop (`clr.`, `import()`, `using`)
- NovaSharp extensions (`!=`, `json.`, `string.startswith`, etc.)
- NovaSharp globals (`_NOVASHARP`, `Script.GlobalOptions`)
- Unresolved C# interpolation placeholders (`{variableName}`)

### `@expects-error` Values

```lua
-- Normal execution expected:
-- @expects-error: false

-- Runtime error expected (non-zero exit code):
-- @expects-error: true
```

______________________________________________________________________

## Directory Structure

Place fixtures in the appropriate `LuaFixtures/<TestClass>/` directory:

```
src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/
└── LuaFixtures/
    ├── MathModuleTUnitTests/
    │   ├── FloorReturnsInteger.lua
    │   ├── FloorHandlesNegatives.lua
    │   └── FloorHandlesInfinity_53plus.lua
    └── StringModuleTUnitTests/
        ├── SubstringBasic.lua
        └── SubstringNegativeIndex.lua
```

______________________________________________________________________

## Version-Specific Naming Conventions

| Pattern                   | Use Case                       | `@lua-versions`                     |
| ------------------------- | ------------------------------ | ----------------------------------- |
| `feature_test.lua`        | Works in all versions          | `5.1, 5.2, 5.3, 5.4, 5.5` or `5.1+` |
| `feature_test_51.lua`     | Lua 5.1 only                   | `5.1`                               |
| `feature_test_52.lua`     | Lua 5.2 only                   | `5.2`                               |
| `feature_test_52plus.lua` | Lua 5.2 and later              | `5.2, 5.3, 5.4, 5.5` or `5.2+`      |
| `feature_test_53plus.lua` | Lua 5.3 and later              | `5.3, 5.4, 5.5` or `5.3+`           |
| `feature_test_54plus.lua` | Lua 5.4 and later              | `5.4, 5.5` or `5.4+`                |
| `feature_test_51_52.lua`  | Lua 5.1 and 5.2 only           | `5.1, 5.2`                          |
| `feature_test_error.lua`  | Error expected in all versions | (with `@expects-error: true`)       |

______________________________________________________________________

## Version-Specific Feature Detection

When writing fixtures, be aware of features that vary by Lua version:

### Lua 5.4+ Only

- `<const>` and `<close>` attributes
- `warn()` function

### Lua 5.3+ Only

- Integer division `//`
- Bitwise operators `&`, `|`, `~`, `<<`, `>>`
- `utf8` library
- `math.tointeger`, `math.type`, `math.ult`
- `math.maxinteger`, `math.mininteger`
- `string.pack`, `string.unpack`, `string.packsize`
- `table.move`

### Lua 5.2+ Only

- `goto` statement and `::label::`
- `_ENV` variable
- `bit32` library
- `rawlen()` function
- `table.pack`, `table.unpack`
- `load()` with string argument (use `loadstring()` in 5.1)
- Hex float literals (`0x1.5p3`)
- `debug.upvalueid`, `debug.upvaluejoin`
- `debug.getlocal` with function argument

### Lua 5.1 Only (deprecated in later versions)

- `table.getn`, `table.setn`
- `math.mod` (use `math.fmod`)
- `string.gfind` (use `string.gmatch`)
- `table.foreach`, `table.foreachi`
- `loadstring` (use `load` in 5.2+)

______________________________________________________________________

## Complete Examples

### Basic Fixture (all versions)

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:100
-- @test: MathModuleTUnitTests.FloorReturnsInteger

-- Test: math.floor returns the largest integer <= x
-- Reference: Lua 5.4 Reference Manual §6.7

local result = math.floor(3.7)
assert(result == 3, "Expected 3, got " .. tostring(result))
print("PASS")
```

### Version-Specific Fixture (5.3+)

```lua
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:150
-- @test: MathModuleTUnitTests.IntegerDivisionBasic
-- @compat-notes: Floor division (//) introduced in Lua 5.3

-- Test: Floor division returns integer result
-- Reference: Lua 5.4 Reference Manual §3.4.1

local result = 7 // 3
assert(result == 2, "Expected 2, got " .. tostring(result))
print("PASS")
```

### Error-Expecting Fixture

```lua
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:99
-- @test: MathNumericEdgeCasesTUnitTests.IntegerDivisionByZeroErrors

-- Test: Integer division by zero throws error in Lua 5.3+
-- Reference: Lua 5.4 Reference Manual §3.4.1

return 1 // 0  -- integer division by zero should error
```

### NovaSharp-Only Fixture (CLR interop)

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Interop/ClrInteropTests.cs:50
-- @test: ClrInteropTests.CanAccessClrTypes

-- Test: CLR type access via NovaSharp interop
-- Note: This fixture uses NovaSharp-specific features

local Math = clr.import("System.Math")
local result = Math.Abs(-42)
assert(result == 42)
print("PASS")
```

### Fixture with pcall Error Handling

```lua
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:120
-- @test: MathNumericEdgeCasesTUnitTests.ModuloByZeroThrowsError

-- Test: Integer modulo by zero throws error in Lua 5.3+
-- Reference: Lua 5.4 Reference Manual §3.4.1

local success, err = pcall(function()
    return 1 % 0
end)

if not success then
    if string.find(err, "n%%0") then
        print("PASS: Got expected error")
    else
        print("PASS: Got error (message: " .. tostring(err) .. ")")
    end
else
    print("FAIL: Expected error but got " .. tostring(err))
end
```

______________________________________________________________________

## Validation Checklist

Before committing a fixture, verify:

1. ✅ Metadata block starts at line 1
1. ✅ All three required fields present (`@lua-versions`, `@novasharp-only`, `@expects-error`)
1. ✅ `@lua-versions` lists correct compatible versions
1. ✅ `@novasharp-only` is `true` if using any NovaSharp extensions
1. ✅ `@expects-error` matches whether fixture should error
1. ✅ File runs successfully with reference Lua: `lua5.4 path/to/fixture.lua`
1. ✅ Output is deterministic (no random values, timestamps, memory addresses)

______________________________________________________________________

## After Creating Fixtures

### Verify against reference Lua

```bash
# Single file
lua5.4 path/to/fixture.lua

# All fixtures for a version
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4
```

### Compare with NovaSharp

```bash
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4
```

### Regenerate corpus (if modifying existing tests)

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

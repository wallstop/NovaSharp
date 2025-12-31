# Skill: Creating Lua Test Fixtures

**When to use**: Creating `.lua` test files for cross-interpreter verification.

**Related Skills**: [lua-comparison-harness](lua-comparison-harness.md) (running fixtures), [lua-spec-verification](lua-spec-verification.md) (verifying compliance), [tunit-test-writing](tunit-test-writing.md) (corresponding C# tests)

______________________________________________________________________

## 🔴 Reference Lua is the Source of Truth

When creating fixtures, remember:

1. **Run against reference Lua FIRST** — The output from `lua5.X fixture.lua` defines expected behavior
1. **NovaSharp must match** — Any difference means NovaSharp has a bug
1. **NEVER adjust fixtures to match NovaSharp** — Fix the interpreter instead
1. **Document version differences** — If Lua 5.1 and 5.4 behave differently, that's TWO expected behaviors

### Before Committing ANY Fixture

```bash
# Run against ALL target versions and RECORD THE OUTPUT
lua5.1 fixture.lua
lua5.2 fixture.lua
lua5.3 fixture.lua
lua5.4 fixture.lua

# These outputs are the SPEC — NovaSharp MUST match them
# If outputs differ between versions, create version-specific tests
```

### When NovaSharp and Reference Lua Differ

| Scenario                                | Action                                  |
| --------------------------------------- | --------------------------------------- |
| NovaSharp differs from ALL Lua versions | **NovaSharp BUG** — fix production code |
| NovaSharp matches some Lua versions     | Check version gating in NovaSharp       |
| Fixture fails on reference Lua          | **Fixture bug** — fix the fixture       |
| Fixture uses NovaSharp-only features    | Mark `@novasharp-only: true`            |

______________________________________________________________________

## 🔴 Critical: Complete Test Workflow

Every new test requires **THREE deliverables**:

1. **C# TUnit test** — Runs against NovaSharp runtime (see [tunit-test-writing](tunit-test-writing.md))
1. **`.lua` fixture file** — Standalone Lua for cross-interpreter verification
1. **Regenerate corpus** — Run `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py` after adding fixtures

### Workflow Order

```bash
# 1. Create C# test in src/tests/.../TUnit/ (see tunit-test-writing skill)
# 2. Create .lua fixture with metadata header (see below)
# 3. Verify fixture runs against reference Lua
lua5.4 path/to/fixture.lua

# 4. Regenerate corpus to sync fixtures
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py

# 5. Run tests to verify everything works
./scripts/test/quick.sh TestMethodName
```

______________________________________________________________________

## 🔴 Critical: Required Metadata Header

Every `.lua` fixture file **MUST** start with a metadata block for the test runner to parse it correctly. Files without this header will not be properly filtered or may produce false comparison failures.

**⚠️ WARNING**: The harness ONLY parses `@lua-versions`, `@novasharp-only`, and `@expects-error`. Fields like `@min-version`, `@max-version`, `@versions`, `@name`, `@description`, `@expected-output`, `@error-pattern` are **NOT recognized** and will be silently ignored! See the "INVALID Metadata" section below.

### 🔴 CRITICAL: Metadata Location Requirements

**The harness parser STOPS at the FIRST non-comment line.** ALL metadata MUST be at the TOP of the file, before ANY blank lines or code.

```lua
-- ✅ CORRECT: All metadata at top, no blank lines before code
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/...
-- @test: TestClass.TestMethod
print("test")  -- Code starts immediately after metadata
```

```lua
-- ❌ WRONG: Blank line before metadata = metadata SILENTLY IGNORED!

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
print("test")
```

```lua
-- ❌ WRONG: Metadata after code = metadata SILENTLY IGNORED!
print("test")
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
```

**Real-World Impact**: This caused 9 test fixtures to fail in CI because the metadata was placed after a blank line, causing them to run on ALL Lua versions instead of only the intended versions.

### Minimal Required Template

```lua
-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/<TestProject>/<TestClass>.cs:<LineNumber>
-- @test: <TestClass>.<TestMethod>

-- Your Lua code here
```

### Full Template (with all optional fields)

```lua
-- @lua-versions: all
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

## 🔴 INVALID Metadata (DO NOT USE)

The harness **ONLY** parses the three required fields (`@lua-versions`, `@novasharp-only`, `@expects-error`). Any other metadata tags are **silently ignored** and will cause fixtures to run incorrectly or be skipped entirely.

### ❌ NEVER Use These (they are NOT parsed)

```lua
-- ❌ WRONG: @min-version is NOT recognized
-- @min-version: 5.3

-- ❌ WRONG: @max-version is NOT recognized
-- @max-version: 5.2

-- ❌ WRONG: @versions is NOT recognized
-- @versions: 5.2, 5.3

-- ❌ WRONG: @name is NOT recognized
-- @name: SomeTestName

-- ❌ WRONG: @description is NOT recognized
-- @description: This test does something

-- ❌ WRONG: @expected-output is NOT recognized
-- @expected-output: 42

-- ❌ WRONG: @error-pattern is NOT recognized
-- @error-pattern: some error message
```

### ✅ CORRECT Equivalents

| ❌ Invalid                     | ✅ Correct Replacement                          |
| ------------------------------ | ----------------------------------------------- |
| `@min-version: 5.3`            | `@lua-versions: 5.3, 5.4, 5.5` (or `5.3+`)      |
| `@max-version: 5.2`            | `@lua-versions: 5.1, 5.2`                       |
| `@versions: 5.2, 5.3`          | `@lua-versions: 5.2, 5.3`                       |
| `@lua-versions: 5.2, 5.5`      | `@lua-versions: 5.2, 5.3, 5.4, 5.5` (list ALL!) |
| `@error-pattern: ...`          | `@expects-error: true` (pattern not checked)    |
| `@name: X` / `@description: Y` | Use `@test:` and a `-- Test:` comment instead   |
| `@expected-output: X`          | Use `assert()` or `print()` in the Lua code     |

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

**⚠️ PREFERRED: Use range syntax for future-proof tests.** Range annotations automatically include new Lua versions (e.g., 5.6) without requiring test updates.

```lua
-- ✅ BEST: Range syntax (future-proof, auto-includes new versions)
-- @lua-versions: all              -- All Lua versions (5.1+)
-- @lua-versions: 5.3+             -- 5.3 and above (expands to 5.3, 5.4, 5.5, ...)
-- @lua-versions: 5.1-5.2          -- 5.1 through 5.2 (inclusive range)
-- @lua-versions: 5.2-5.4          -- 5.2 through 5.4 (inclusive range)

-- ✅ ACCEPTABLE: Explicit list (only when behavior differs non-contiguously)
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5

-- ❌ AVOID: Explicit lists for contiguous ranges (not future-proof)
-- @lua-versions: 5.3, 5.4, 5.5    -- Use 5.3+ instead
-- @lua-versions: 5.1, 5.2         -- Use 5.1-5.2 instead

-- ❌ WRONG: Only listing first and last version (skips middle versions!)
-- @lua-versions: 5.2, 5.5
-- (This SKIPS 5.3 and 5.4 testing!)

-- NovaSharp-only (skip reference Lua comparison):
-- @lua-versions: novasharp-only
```

### Range Annotation Reference

| Annotation       | Meaning                     | Equivalent Explicit List       | Tooling Support |
| ---------------- | --------------------------- | ------------------------------ | --------------- |
| `all`            | All Lua versions            | `5.1, 5.2, 5.3, 5.4, 5.5, ...` | 🔲 Planned      |
| `5.3+`           | Version 5.3 and above       | `5.3, 5.4, 5.5, ...`           | ✅ Supported    |
| `5.1-5.2`        | Versions 5.1 through 5.2    | `5.1, 5.2`                     | 🔲 Planned      |
| `5.2-5.4`        | Versions 5.2 through 5.4    | `5.2, 5.3, 5.4`                | 🔲 Planned      |
| `novasharp-only` | NovaSharp-specific features | (skip reference Lua)           | ✅ Supported    |

**Current Tooling Status**:

- `run-lua-fixtures-parallel.py`: Supports `5.X+` syntax
- `compare-lua-outputs.py`: Supports `5.X+` syntax
- `lua_corpus_extractor_v2.py`: Generates `5.X+` for contiguous ranges from version to latest

**Planned**: Full `all` and `5.X-5.Y` range syntax (see PLAN.md: "Consolidate Lua Version Parsing Logic")

**Why prefer ranges?** When Lua 5.6 is released, tests using `5.3+` will automatically include it. Tests using explicit `5.3, 5.4, 5.5` will need manual updates.

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

| Pattern                   | Use Case                       | `@lua-versions` (preferred)   |
| ------------------------- | ------------------------------ | ----------------------------- |
| `feature_test.lua`        | Works in all versions          | `all`                         |
| `feature_test_51.lua`     | Lua 5.1 only                   | `5.1`                         |
| `feature_test_52.lua`     | Lua 5.2 only                   | `5.2`                         |
| `feature_test_52plus.lua` | Lua 5.2 and later              | `5.2+`                        |
| `feature_test_53plus.lua` | Lua 5.3 and later              | `5.3+`                        |
| `feature_test_54plus.lua` | Lua 5.4 and later              | `5.4+`                        |
| `feature_test_51_52.lua`  | Lua 5.1 and 5.2 only           | `5.1-5.2`                     |
| `feature_test_52_54.lua`  | Lua 5.2 through 5.4            | `5.2-5.4`                     |
| `feature_test_error.lua`  | Error expected in all versions | (with `@expects-error: true`) |

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
-- @lua-versions: all
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
-- @lua-versions: 5.3+
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
-- @lua-versions: 5.3+
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
-- @lua-versions: all
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
-- @lua-versions: 5.3+
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

1. ✅ **Metadata block starts at LINE 1 with NO blank lines before or within metadata** (parser stops at first non-comment line!)
1. ✅ All three required fields present (`@lua-versions`, `@novasharp-only`, `@expects-error`)
1. ✅ `@lua-versions` lists correct compatible versions
1. ✅ `@novasharp-only` is `true` if using any NovaSharp extensions
1. ✅ `@expects-error` matches whether fixture should error
1. ✅ File runs successfully with reference Lua: `lua5.4 path/to/fixture.lua`
1. ✅ Output is deterministic (no random values, timestamps, memory addresses)

### Quick Metadata Validation

```bash
# Check that metadata is at the top of file (should show metadata, not blank line)
head -1 path/to/fixture.lua
# Should output: -- @lua-versions: ...

# Verify no blank lines before metadata
head -10 path/to/fixture.lua | cat -A
# Look for ^$ (empty lines) before @lua-versions
```

______________________________________________________________________

## After Creating Fixtures

### 1. Ensure corresponding C# test exists

Every `.lua` fixture should have a matching C# TUnit test. See [tunit-test-writing](tunit-test-writing.md) for details.

### 2. Verify against reference Lua

```bash
# Single file
lua5.4 path/to/fixture.lua

# All fixtures for a version
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4
```

### 3. Regenerate corpus (REQUIRED)

**Always run this after adding or modifying fixtures:**

```bash
python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
```

This syncs the fixture manifest and ensures CI can discover all fixtures.

### 4. Compare with NovaSharp

```bash
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.4
```

### 5. Run C# tests

```bash
./scripts/test/quick.sh TestMethodName
```

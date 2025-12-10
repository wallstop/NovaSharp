# Modern Testing & Coverage Plan

## 🔴🔴 HIGHEST PRIORITY: Lua Spec Compliance — Fix All Behavioral Divergences (§8.38)

**Status**: 🚧 **IN PROGRESS** — Most critical issues from Lua 5.4 comparison now resolved.

**Core Principle**:
NovaSharp's PRIMARY GOAL is to be a **faithful Lua interpreter** that matches the official Lua reference implementation as closely as possible. When fixture comparisons reveal behavioral differences:

1. **ASSUME NOVASHARP IS WRONG** until proven otherwise
2. **FIX THE PRODUCTION CODE** to match Lua behavior
3. **ADD REGRESSION TESTS** with standalone `.lua` fixtures runnable against real Lua
4. **NEVER adjust tests to accommodate bugs** — fix the runtime instead

### 📋 NEXT STEPS (Priority Order)

1. **Verify `utf8.offset` bounds handling** — Position 0 check exists but may need additional edge case testing
2. **Run full Lua 5.4 comparison** to identify any remaining mismatches
3. **Add version-split test fixtures** for `debug.upvalueid` behavior differences

### ✅ COMPLETED FIXES

| Date | Issue | Description | Fix |
|------|-------|-------------|-----|
| 2025-12-10 | `string.format('%d')` overflow | `string.format('%d', math.floor(math.maxinteger + 0.5))` was wrapping instead of throwing | Fixed `math.floor`/`math.ceil` to use `LuaIntegerHelper.TryGetInteger` for correct 2^63 boundary checking. Added 9 regression tests. |

### 🟡 REMAINING ISSUES

| Priority | Issue | Description | Status |
|----------|-------|-------------|--------|
| 🔴 HIGH | Lua 5.2 `string.format('%d')` overflow | NovaSharp wraps but Lua 5.2 throws "not a number in proper range" | Not started |
| 🟡 MED | `utf8.offset` bounds | Verify position bounds validation is complete | Needs verification |
| 🟡 MED | `UpvalueIdReturnsUserDataHandles.lua` | Needs investigation | Not started |
| 🟡 MED | `UpvalueJoinSharesState.lua` | Needs investigation | Not started |

### Systematic Compliance Verification Process

For each discovered violation:

1. **Run against reference Lua**:
   ```bash
   lua5.1 fixture.lua 2>&1
   lua5.3 fixture.lua 2>&1
   lua5.4 fixture.lua 2>&1
   ```

2. **Document expected behavior** with Lua manual references

3. **Fix NovaSharp production code** to match

4. **Create regression test fixtures**:
   - One fixture per behavior variant
   - Include success cases, error cases, edge cases
   - Add `@expects-error: true` for error-expecting tests

5. **Verify fix**:
   ```bash
   python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.X -j 8 --output-dir artifacts/lua-comparison-5.X
   python3 scripts/tests/compare-lua-outputs.py --lua-version 5.X --results-dir artifacts/lua-comparison-5.X
   ```

6. **Run full test suite** to ensure no regressions

### Commands

```bash
# Run comprehensive comparison for a Lua version
rm -rf artifacts/lua-comparison-5.X
python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.X -j 8 --output-dir artifacts/lua-comparison-5.X
python3 scripts/tests/compare-lua-outputs.py --lua-version 5.X --results-dir artifacts/lua-comparison-5.X

# Investigate a specific fixture
lua5.3 src/tests/.../LuaFixtures/Module/Fixture.lua
dotnet run --project src/tooling/WallstopStudios.NovaSharp.Cli -- --lua-version 5.3 src/tests/.../LuaFixtures/Module/Fixture.lua
```

---

## 📋 Lua Fixture Verification Policy

**REQUIRED**: When fixing any Lua semantic issue or discovering a behavioral discrepancy, create a comprehensive suite of standalone Lua files that can be run against both NovaSharp and the official Lua interpreter to verify correctness and prevent regressions.

### Fixture Requirements

1. **Create standalone `.lua` files** in the appropriate `LuaFixtures/<TestClass>/` directory
2. **One fixture per behavior variant** — separate files for success cases, error cases, and edge cases
3. **Version-aware naming** — suffix with `_51`, `_52`, `_53plus`, `_54plus`, etc. when behavior differs by version
4. **Self-documenting** — include comments explaining expected behavior and which Lua versions apply
5. **Runnable against real Lua** — fixtures must execute cleanly with `lua5.1`, `lua5.4`, etc.

### Fixture Structure Pattern

```lua
-- Test: <description of what's being tested>
-- Expected: <success/error/specific output>
-- Versions: <5.1, 5.2, 5.3, 5.4 or specific subset>
-- Reference: <Lua manual section, e.g., "§6.4.1">

local success, err = pcall(function()
    -- Test code here
end)

if success then
    print("PASS")
else
    print("EXPECTED ERROR: " .. tostring(err))
end
```

### Verification Workflow

1. Run fixture against NovaSharp: `nova --lua-version 5.4 fixture.lua`
2. Run fixture against real Lua: `lua5.4 fixture.lua`
3. Compare outputs — they must match exactly
4. Document any intentional divergences in the fixture comments

### Example Fixtures (from `string.char` fix)

- `CharErrorsOnNegativeValue.lua` — tests error on `string.char(-1)`
- `CharErrorsOnValueAbove255.lua` — tests error on `string.char(256)`
- `CharAcceptsBoundaryValueZero.lua` — tests success on `string.char(0)`
- `CharAcceptsBoundaryValue255.lua` — tests success on `string.char(255)`

This policy ensures every behavioral fix has cross-interpreter verification and guards against future regressions.

---

## 🔴 HIGH Priority: Flag Enum Combined Values Must Be External Constants (§8.40)

**Status**: 📋 **PLANNED** — Codebase sweep required to move combined flag values to helper classes.

**Problem Statement**:
For `[Flags]` enums, combined values produced by **any bitwise operation** (OR `|`, AND `&`, XOR `^`, NOT `~`) should **never** be actual enum members. Instead, they must be defined as `static readonly` or `const` values in nearby helper classes or extension method classes. Enum members in flag enums should **only ever have one bit set** (powers of two), with the sole exception of `None = 0`.

### Rationale

1. **Semantic clarity**: Each enum member represents a single, atomic flag
2. **Serialization safety**: Combined values create ambiguous serialization/deserialization behavior
3. **Reflection predictability**: `Enum.GetValues()` returns combined values, polluting iteration
4. **Maintainability**: Adding new flags doesn't require updating all combined value members
5. **API cleanliness**: Consumers create their own combinations as needed

### Pattern to Apply

```csharp
// WRONG: Combined values as enum members
[Flags]
public enum CoreModules
{
    None = 0,
    Basic = 1 << 0,
    StringLib = 1 << 1,
    Math = 1 << 2,
    Table = 1 << 3,
    
    // ❌ BAD: Combined values as enum members
    PresetHardSandbox = Basic | StringLib | Table | Math,
    PresetDefault = PresetHardSandbox | OtherFlags,
}

// CORRECT: Combined values in a helper class
[Flags]
public enum CoreModules
{
    None = 0,
    Basic = 1 << 0,
    StringLib = 1 << 1,
    Math = 1 << 2,
    Table = 1 << 3,
    // Only single-bit values as enum members
}

/// <summary>
/// Preset combinations for <see cref="CoreModules"/>.
/// </summary>
public static class CoreModulePresets
{
    /// <summary>
    /// A hard sandbox preset including basic, string, math, and table modules.
    /// </summary>
    public static readonly CoreModules HardSandbox = 
        CoreModules.Basic | CoreModules.StringLib | CoreModules.Math | CoreModules.Table;
    
    /// <summary>
    /// The default preset for most use cases.
    /// </summary>
    public static readonly CoreModules Default = HardSandbox | CoreModules.OtherFlags;
}
```

### Scope of Audit

**Search Pattern**:
```bash
# Find [Flags] enums and check for combined values
grep -A 50 '\[Flags\]' src/runtime/ src/tooling/ src/debuggers/ | grep -E '= .+\|'
```

**Known Violations (Priority Order)**:

| File | Enum | Combined Members |
|------|------|------------------|
| `Modules/CoreModules.cs` | `CoreModules` | `PresetHardSandbox`, `PresetSoftSandbox`, `PresetDefault`, `PresetComplete` |

**Fix Strategy**:
1. Create `CoreModulePresets` static class in the same file
2. Move combined values to `static readonly` fields in the preset class
3. Update all usages to reference `CoreModulePresets.HardSandbox` etc.
4. Ensure XML documentation is preserved/improved
5. Run full test suite to verify no regressions

---

## 🔴 HIGH Priority: InvalidEnumArgumentException Standardization (§8.39)

**Status**: 📋 **PLANNED** — Codebase sweep required to standardize enum exception handling.

**Problem Statement**:
When switching on or validating enum values throughout the codebase, various exception types are used inconsistently (`ArgumentException`, `ArgumentOutOfRangeException`, `NotSupportedException`, etc.) for invalid/unrecognized enum values. The correct exception type for this scenario is `System.ComponentModel.InvalidEnumArgumentException`, which:

1. **Provides clearer semantics**: Explicitly indicates an enum validation failure
2. **Includes better diagnostics**: Constructor accepts the parameter name, invalid value, and enum type
3. **Follows .NET conventions**: This is the BCL-provided exception specifically for this scenario
4. **Enables better error handling**: Callers can catch this specific exception type

### Pattern to Apply

```csharp
// CORRECT: Use InvalidEnumArgumentException for invalid enum values
switch (dataType)
{
    case DataType.Nil:
        // handle nil
        break;
    case DataType.Boolean:
        // handle boolean
        break;
    // ... other cases ...
    default:
        throw new InvalidEnumArgumentException(nameof(dataType), (int)dataType, typeof(DataType));
}

// WRONG: Using generic exceptions
default:
    throw new ArgumentException($"Unknown data type: {dataType}");
    throw new ArgumentOutOfRangeException(nameof(dataType));
    throw new NotSupportedException($"Unsupported: {dataType}");
```

### Scope of Audit

**Search Patterns**:
```bash
# Find switch statements on enum types with generic exception throws
grep -rn "throw new ArgumentException\|throw new ArgumentOutOfRangeException\|throw new NotSupportedException\|throw new InvalidOperationException" src/runtime/ src/tooling/ src/debuggers/

# Look for switch default cases that throw
grep -rn "default:" src/runtime/ | head -100
```

**Files to Audit (Priority Order)**:
1. `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/` — VM opcode switches
2. `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/` — Type conversion switches
3. `src/runtime/WallstopStudios.NovaSharp.Interpreter/Interop/` — Interop enum handling
4. `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/` — Standard library enum switches
5. `src/tooling/` — CLI and tooling enum handling
6. `src/debuggers/` — Debugger protocol enum handling

### Implementation Tasks

- [ ] **Phase 1**: Run grep patterns to identify all enum switch statements with exception throws
- [ ] **Phase 2**: Classify each as:
  - ✅ Already uses `InvalidEnumArgumentException`
  - 🔄 Needs conversion to `InvalidEnumArgumentException`
  - ⚠️ Not an enum switch (different fix needed)
- [ ] **Phase 3**: Update all identified switches to use `InvalidEnumArgumentException`
- [ ] **Phase 4**: Add `using System.ComponentModel;` where needed
- [ ] **Phase 5**: Verify builds and tests pass

### Success Criteria

- All enum validation failures throw `InvalidEnumArgumentException`
- Consistent exception pattern across entire codebase
- No generic exceptions (`ArgumentException`, `ArgumentOutOfRangeException`, `NotSupportedException`) used for enum validation

**Owner**: Interpreter team
**Priority**: 🔴 HIGH — Code quality and consistency improvement

---

## 🔴 CRITICAL Priority: Comprehensive LuaNumber Usage Audit (§8.37)

**Status**: 📋 **PLANNED** — Thorough production code sweep required.

**Problem Statement (2025-12-09)**:
The codebase may contain locations where raw C# numeric types (`double`, `float`, `int`, `long`) are used instead of `LuaNumber` for Lua math operations. This can cause:

1. **Precision loss**: Values beyond 2^53 cannot be exactly represented as doubles
2. **Type coercion errors**: Integer vs float subtype distinction lost (critical for Lua 5.3+)
3. **Overflow/underflow bugs**: Silent wrapping or unexpected behavior
4. **IEEE 754 edge cases**: Incorrect handling of NaN, Infinity, negative zero
5. **Value representation failures**: Unable to represent certain Lua values correctly

### Scope of Audit

**Files to Audit (Priority Order)**:

1. **VM Core** (HIGHEST PRIORITY):
   - `Execution/VM/Processor_Ops.cs` — Arithmetic operations
   - `Execution/VM/Processor_Loop.cs` — Comparison and numeric opcodes
   - `Execution/VM/Processor_*.cs` — All processor files

2. **Expression Evaluation**:
   - `Tree/Expressions/*.cs` — Numeric literal handling, constant folding
   - `Tree/Statements/*.cs` — For loop numeric handling

3. **Interop Layer**:
   - `Interop/Converters/*.cs` — CLR type conversion
   - `Interop/StandardDescriptors/*.cs` — Numeric member access

4. **Data Types**:
   - `DataTypes/DynValue.cs` — Ensure `LuaNumber` used consistently
   - `DataTypes/Table.cs` — Numeric key handling
   - `DataTypes/*.cs` — Any numeric operations

5. **CoreLib Modules** (secondary pass):
   - All modules in `CoreLib/*.cs` — Already audited per §8.33, but verify completeness

### Patterns to Search For

```bash
# POTENTIALLY PROBLEMATIC PATTERNS:

# Direct .Number access (loses integer subtype)
grep -rn "\.Number" src/runtime/WallstopStudios.NovaSharp.Interpreter/ | grep -v "LuaNumber"

# Explicit double casts that may lose precision
grep -rn "(double)" src/runtime/WallstopStudios.NovaSharp.Interpreter/

# Explicit float casts (even worse precision)
grep -rn "(float)" src/runtime/WallstopStudios.NovaSharp.Interpreter/

# Math operations on raw doubles
grep -rn "Math\." src/runtime/WallstopStudios.NovaSharp.Interpreter/ | grep -v "LuaNumber"

# Direct int/long arithmetic that may overflow
grep -rn "checked\|unchecked" src/runtime/WallstopStudios.NovaSharp.Interpreter/

# Numeric literals assigned to double variables
grep -rn "double.*=" src/runtime/WallstopStudios.NovaSharp.Interpreter/
```

### Known Good Patterns (Reference)

```csharp
// CORRECT: Use LuaNumber throughout
LuaNumber num = dynValue.LuaNumber;
if (num.IsInteger)
{
    long intVal = num.AsInteger;  // Safe - verified integer
}
else
{
    double floatVal = num.AsFloat;  // Safe - verified float
}

// CORRECT: Arithmetic via LuaNumber operators
LuaNumber result = left + right;  // Uses LuaNumber.operator+

// CORRECT: Version-aware validation
long value = LuaNumberHelpers.ToLongWithValidation(version, dynValue, "funcname", argIndex);

// WRONG: Bypasses type system
double value = dynValue.Number;  // Integer distinction lost!
double result = a + b;  // Raw double math, may lose precision

// WRONG: Silent precision loss
int index = (int)dynValue.Number;  // May truncate large values incorrectly
```

### Implementation Tasks

- [ ] **Phase 1**: Run grep patterns above, catalog all hits
- [ ] **Phase 2**: Classify each hit as:
  - ✅ Safe (intentional, documented, or internal-only)
  - ⚠️ Suspicious (needs investigation)
  - 🔴 Bug (incorrect usage, fix required)
- [ ] **Phase 3**: Fix all 🔴 bugs, document all ⚠️ cases
- [ ] **Phase 4**: Add regression tests for each fix
- [ ] **Phase 5**: Create lint rule or CI check to prevent future violations
- [ ] **Phase 6**: Document intentional raw numeric usage (if any)

### Success Criteria

- All Lua arithmetic operations use `LuaNumber` consistently
- No silent precision loss for values beyond 2^53
- Integer vs float subtype preserved throughout the pipeline
- IEEE 754 special values (NaN, Inf, -0.0) handled correctly
- CI prevents introduction of new raw numeric operations in Lua paths

### Related Sections
- §8.33: LuaNumber Compliance Sweep (CoreLib audit complete)
- §8.34: Lua 5.3+ Integer Representation Errors
- §8.36: Comprehensive Numeric Edge-Case Audit
- §8.24: Dual Numeric Type System (LuaNumber struct)

**Owner**: Interpreter team
**Priority**: 🔴 HIGH — Numeric correctness is fundamental to Lua compatibility

---

## 🔴 CRITICAL Priority: CLI Lua Version Propagation & Modularization (§8.31)

**Status**: 🚧 **IN PROGRESS** — Initial `--lua-version` flag added, needs comprehensive hardening.

**Problem Statement (2025-12-08)**:
All Lua version comparison CI/CD scripts and tooling must properly propagate the Lua version to NovaSharp via CLI arguments. The initial `--lua-version` flag was added to the CLI, but the argument parsing infrastructure needs significant hardening and modularization.

### Critical Requirements

1. **All comparison scripts must pass `--lua-version`**:
   - `scripts/tests/run-lua-fixtures.sh` ✅ Updated
   - Any other scripts invoking `nova` or NovaSharp CLI must also pass the flag
   - CI/CD workflows must validate correct version propagation

2. **CLI Argument Parsing Modularization**:
   - Current: Ad-hoc parsing scattered throughout `Program.cs`
   - Target: Centralized argument registry with clear supported-args list
   - Required features:
     - List of all supported arguments with descriptions
     - Validation of mutually exclusive flags
     - Help text generation from argument definitions
     - Version-aware default behaviors

3. **Exhaustive CLI Tests**:
   - All argument combinations (valid and invalid)
   - Error message validation
   - Help/usage output validation
   - Version flag interactions with other flags
   - Edge cases: empty args, malformed args, unknown flags

### Implementation Tasks

- [ ] Create `CliArgumentRegistry` class with all supported arguments
- [ ] Refactor `Program.cs` to use centralized registry
- [ ] Add `--help` / `-h` that lists all supported arguments
- [ ] Add tests for every supported argument
- [ ] Add tests for invalid/unknown argument handling
- [ ] Document all CLI arguments in `docs/cli-reference.md`
- [ ] Update all CI scripts to validate version propagation
- [ ] Add integration tests that verify CLI → Script.CompatibilityVersion flow

---

## 🔴 CRITICAL Priority: LuaNumber Compliance Sweep (§8.33)

**Status**: 🚧 **IN PROGRESS** — CoreLib module audit complete, VM audit pending.

**Problem Statement (2025-12-09)**:
The codebase must consistently use the `LuaNumber` struct for all Lua numeric operations to preserve integer vs float type information per Lua 5.3+ semantics. Direct use of C# numeric types (`double`, `float`, `int`, `long`) for Lua math bypasses the dual-type system and can cause incorrect behavior.

### Completed Module Audits

All CoreLib modules have been audited and updated with version-aware integer validation:

- ✅ `StringModule.cs` — string.format, string.byte, string.sub, string.rep
- ✅ `MathModule.cs` — math.random, math.randomseed (48 new tests)
- ✅ `TableModule.cs` — table.unpack, table.insert, table.remove, table.concat, table.move (20 new tests)
- ✅ `BasicModule.cs` — error, select
- ✅ `Bit32Module.cs` — all bit32 operations with version-aware validation
- ✅ `DebugModule.cs` — getupvalue, upvalueid, setupvalue
- ✅ `OsTimeModule.cs` — difftime, date
- ✅ `IoModule.cs` — Clean (only type checks, no numeric extraction)

### Remaining Work

- [ ] Create lint script to detect `.Number` usage patterns
- [ ] Document all intentional `.Number` usage (if any remain)
- [ ] **VM and expression evaluation audit**:
  - `Processor_Ops.cs` — arithmetic operators
  - `Processor_Loop.cs` — comparison and numeric ops
  - `Expression.cs` — numeric literal handling

### Known Good Patterns (Reference)

```csharp
// CORRECT: Use LuaNumber
LuaNumber num = dynValue.LuaNumber;
if (num.IsInteger)
{
    long intVal = num.AsInteger;  // Safe - verified integer
}
else
{
    double floatVal = num.AsFloat;  // Safe - verified float
}

// CORRECT: Use version-aware validation helper
long value = LuaNumberHelpers.ToLongWithValidation(version, dynValue, "funcname", argIndex);

// WRONG: Loses type information
double value = dynValue.Number;  // Integer distinction lost!
```

### Audit Commands

```bash
# Find potential violations in CoreLib
grep -rn "\.Number" src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/ | grep -v "LuaNumber"

# Find all DynValue.Number access patterns
grep -rn "DynValue.*\.Number" src/runtime/WallstopStudios.NovaSharp.Interpreter/
```

---

## 🔴 CRITICAL Priority: Lua 5.3+ Integer Representation Errors (§8.34)

**Status**: 📋 **DOCUMENTED** — Investigation complete, implementation pending.

**Problem Statement (2025-12-09)**:
Lua 5.3 introduced the concept of "integer representation" for numeric arguments to certain functions. Values that cannot be represented as integers (NaN, Infinity, non-integral floats in some contexts) must throw specific errors.

### Affected Functions (Partial List)

The following functions require integer arguments in Lua 5.3+ and must throw "number has no integer representation" for invalid inputs:

| Function | Parameter | Lua 5.1/5.2 Behavior | Lua 5.3+ Behavior |
|----------|-----------|---------------------|-------------------|
| `string.char(x)` | x | Treats NaN/Inf as 0 | Error |
| `string.byte(s, i, j)` | i, j | Floor truncation | Floor + validation |
| `string.rep(s, n)` | n | Floor truncation | Must be integer |
| `string.sub(s, i, j)` | i, j | Floor truncation | Floor + validation |
| `table.concat(t, sep, i, j)` | i, j | Floor truncation | Must be integer |
| `table.insert(t, pos, v)` | pos | Floor truncation | Must be integer |
| `table.remove(t, pos)` | pos | Floor truncation | Must be integer |
| `table.move(a1, f, e, t, a2)` | f, e, t | Floor truncation | Must be integer |
| `math.random(m, n)` | m, n | Floor truncation | Must be integer |
| `utf8.char(...)` | all args | N/A (5.3+) | Must be integer |
| `utf8.codepoint(s, i, j)` | i, j | N/A (5.3+) | Must be integer |

### Implementation Strategy

1. **Create shared validation helper**:
```csharp
// In a shared location, e.g., LuaNumberHelpers.cs
internal static long ToIntegerStrict(Script script, double value, string funcName, int argIndex)
{
    if (double.IsNaN(value) || double.IsInfinity(value))
    {
        throw new ScriptRuntimeException(
            $"bad argument #{argIndex} to '{funcName}' (number has no integer representation)"
        );
    }
    
    double floored = Math.Floor(value);
    if (floored != value && script.Options.CompatibilityVersion >= LuaCompatibilityVersion.Lua53)
    {
        // 5.3+ strict mode: non-integral floats may also error in some contexts
        // (depends on specific function requirements)
    }
    
    return (long)floored;
}
```

2. **Apply to all affected functions with version checks**

3. **Add comprehensive test matrix per function**

### Implementation Tasks

- [ ] Create `LuaNumberHelpers.ToIntegerStrict()` helper
- [ ] Audit all functions in the affected list
- [ ] Add version-aware validation to each function
- [ ] Create data-driven tests for each function with NaN/Infinity/fractional inputs
- [ ] Add Lua fixtures for CI comparison testing
- [ ] Update `docs/LuaCompatibility.md`

### Reference
- Lua 5.3 Reference Manual §3.4.3: "Coercions and Conversions"
- Lua 5.3 changes document: Integer subtype introduction

---

## 🔴 CRITICAL Priority: Comprehensive Numeric Edge-Case Audit & Spec Compliance Verification (§8.36)

**Status**: 📋 **INVESTIGATION REQUIRED** — Systematic audit needed for all Lua versions.

**Problem Statement (2025-12-09)**:
Recent bug fixes (§8.32, §8.33) exposed deeper issues around numeric edge cases:

1. **Double precision limitations**: Values beyond 2^53 cannot be exactly represented as doubles. When Lua stores a value as an **integer** type (Lua 5.3+), it preserves full 64-bit precision, but the **same literal value** stored as a float loses precision.

2. **Type-dependent behavior**: `9007199254740993` as integer is valid for `string.byte`, but as float (`9007199254740993.0`) it rounds to `9007199254740992` — a **different value**.

3. **Version-specific semantics**: Each Lua version (5.1, 5.2, 5.3, 5.4, 5.5) has subtly different rules for numeric coercion, truncation, and error handling.

**Root Discovery**:
- `LuaNumber` struct correctly distinguishes integer vs float subtypes
- Original `LuaNumberHelpers` used `double` for validation, losing the integer type information
- Fix: Updated to use `LuaNumber` directly, checking `IsInteger` before applying float validation

**Critical Question**: Where else in the codebase are we extracting `DynValue.Number` (double) when we should be using `DynValue.LuaNumber` (preserves type)?

### Scope of Investigation

#### Phase 1: Audit All Numeric Coercion Sites

Search for patterns that may incorrectly lose integer precision:

```csharp
// POTENTIALLY PROBLEMATIC PATTERNS:
dynValue.Number              // Converts to double, loses integer precision for large values
(double)value               // Explicit cast loses precision
Math.Floor(dynValue.Number) // Double input may already have lost precision

// CORRECT PATTERNS:
dynValue.LuaNumber           // Preserves integer vs float distinction
dynValue.LuaNumber.IsInteger // Check type before extraction
dynValue.LuaNumber.AsInteger // Extract as long when integer type
```

**Files to Audit**:
- `src/runtime/.../CoreLib/*.cs` — All standard library modules
- `src/runtime/.../CoreLib/StringLib/*.cs` — String library helpers
- `src/runtime/.../CoreLib/TableLib/*.cs` — Table library helpers
- `src/runtime/.../Execution/VM/Processor*.cs` — VM arithmetic operations
- `src/runtime/.../Interop/Converters/*.cs` — CLR type converters

#### Phase 2: Exhaustive Test Scenarios for All Affected Functions

Create data-driven tests covering ALL edge cases for EVERY Lua version:

**Numeric Boundary Values**:
| Category | Values to Test | Why |
|----------|---------------|-----|
| Safe integers | 0, 1, -1, 2^52-1, -(2^52-1) | Within double precision |
| Precision boundary | 2^53, 2^53+1, 2^53+2 | Where float loses precision |
| Large integers | 2^62, 2^63-1 (maxinteger), -2^63 (mininteger) | Full integer range |
| Floats | 1.5, -1.5, 0.0, -0.0, 1e308, -1e308 | Float-specific |
| Special | NaN, +Infinity, -Infinity | IEEE 754 special values |
| Negative zero | -0.0 | Must remain float, not integer |

**Functions Requiring Full Audit**:
| Function | Args | Lua 5.1 | Lua 5.2 | Lua 5.3 | Lua 5.4 |
|----------|------|---------|---------|---------|---------|
| `string.byte(s, i, j)` | i, j | floor | floor | error if non-int | error if non-int |
| `string.sub(s, i, j)` | i, j | floor | floor | error if non-int | error if non-int |
| `string.rep(s, n, sep)` | n | floor | floor | error if non-int | error if non-int |
| `string.char(...)` | all | mod 256 | mod 256 | 0-255 or error | 0-255 or error |
| `string.format('%d', x)` | x | ? | ? | requires integer | requires integer |
| `table.insert(t, pos, v)` | pos | floor | floor | must be integer | must be integer |
| `table.remove(t, pos)` | pos | floor | floor | must be integer | must be integer |
| `table.concat(t, sep, i, j)` | i, j | floor | floor | must be integer | must be integer |
| `table.move(a1, f, e, t, a2)` | f,e,t | N/A | N/A | must be integer | must be integer |
| `math.random(m, n)` | m, n | floor | floor | must be integer | must be integer |
| `utf8.char(...)` | all | N/A | N/A | must be integer | must be integer |
| `utf8.codepoint(s, i, j)` | i, j | N/A | N/A | must be integer | must be integer |
| `utf8.offset(s, n, i)` | n, i | N/A | N/A | must be integer | must be integer |
| `bit32.*` functions | all | N/A | integer-like | N/A | N/A |

#### Phase 3: Create Reference Lua Test Scripts

For each function, create a reference script that runs against actual Lua interpreters:

```lua
-- test_string_byte_boundaries.lua
-- Run with: lua5.1, lua5.2, lua5.3, lua5.4

local function test(desc, f)
  local ok, result = pcall(f)
  print(string.format("%-50s %s %s", desc, ok and "OK" or "ERR", tostring(result)))
end

-- Precision boundary tests
test("string.byte('a', 9007199254740993)",    function() return string.byte("a", 9007199254740993) end)
test("string.byte('a', 9007199254740993.0)",  function() return string.byte("a", 9007199254740993.0) end)
test("string.byte('a', math.maxinteger)",     function() return string.byte("a", math.maxinteger) end)

-- NaN/Infinity tests  
test("string.byte('a', 0/0)",                 function() return string.byte("a", 0/0) end)
test("string.byte('a', 1/0)",                 function() return string.byte("a", 1/0) end)
test("string.byte('a', -1/0)",                function() return string.byte("a", -1/0) end)

-- Fractional tests
test("string.byte('Lua', 1.5)",               function() return string.byte("Lua", 1.5) end)
test("string.byte('Lua', -0.5)",              function() return string.byte("Lua", -0.5) end)
```

#### Phase 4: Version-Specific Behavioral Documentation

Document exact expected behavior for each version in `docs/testing/numeric-edge-cases.md`:

```markdown
## string.byte(s, i, j)

### Lua 5.1
- **Non-integer float**: Silently truncated via `math.floor`
- **NaN**: Treated as invalid index, returns nil
- **Infinity**: Treated as invalid index, returns nil
- **Large integers**: No distinction (all numbers are floats)

### Lua 5.2
- Same as 5.1

### Lua 5.3
- **Non-integer float**: Error "number has no integer representation"
- **NaN**: Error "number has no integer representation"
- **Infinity**: Error "number has no integer representation"  
- **Large integers**: Valid if stored as integer type
- **Large floats**: Error if outside representable range

### Lua 5.4
- Same as 5.3
```

#### Phase 5: CI Integration

1. **Add dedicated edge-case test suite**: `NumericEdgeCaseTUnitTests.cs`
2. **Create Lua comparison fixtures**: One fixture per function/version combination
3. **Add regression test for the specific fix**: Ensure `LuaNumber` type is preserved through validation pipeline
4. **Update coverage gating**: Ensure edge-case paths have coverage

### Implementation Checklist

- [ ] **Audit**: grep for `DynValue.Number` usage in CoreLib, flag potential precision loss sites
- [ ] **Audit**: grep for `(double)` casts on numeric DynValues
- [ ] **Audit**: grep for `Math.Floor(*.Number)` patterns
- [ ] **Document**: Create `docs/testing/numeric-edge-cases.md` with expected behavior matrix
- [ ] **Create**: Reference Lua scripts for boundary testing (run against lua5.1/5.2/5.3/5.4)
- [ ] **Create**: `NumericEdgeCaseTUnitTests.cs` with exhaustive data-driven tests
- [ ] **Create**: Lua fixtures for CI comparison testing
- [ ] **Verify**: Run NovaSharp against reference scripts, document divergences
- [ ] **Fix**: Address any newly discovered precision loss sites
- [ ] **Coverage**: Ensure all edge-case branches have test coverage

### Quick Reference Commands

```bash
# Find potential precision loss patterns in CoreLib
grep -rn "\.Number" src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/ | grep -v "LuaNumber"

# Find explicit double casts
grep -rn "(double)" src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/

# Find Math.Floor usage that may lose precision
grep -rn "Math.Floor.*Number" src/runtime/WallstopStudios.NovaSharp.Interpreter/

# Run boundary tests against reference Lua
for v in 5.1 5.2 5.3 5.4; do
  echo "=== Lua $v ==="
  lua$v test_string_byte_boundaries.lua
done
```

### Related Sections
- §8.33: `string.byte`/`string.sub`/`string.rep` version-aware validation (✅ Complete)
- §8.34: Lua 5.3+ integer representation errors (📋 Documented)
- §8.24: Dual numeric type system (`LuaNumber` struct) (🚧 In Progress)

### Priority: 🔴 HIGH

This investigation is critical because:
1. **Subtle bugs**: Precision loss is silent — tests may pass with "close enough" values
2. **Security**: Integer overflow/underflow can cause unexpected behavior
3. **Spec compliance**: NovaSharp claims Lua compatibility — must match reference implementations
4. **Trust**: Users rely on consistent behavior across Lua versions

---

## 🎯 Current Priority: Dual Numeric Type System (§8.24 — HIGH PRIORITY)

**Status**: 🚧 **IN PROGRESS** — Phase 3 Standard Library complete, Phase 4-5 remaining.

**Progress (2025-12-07)**:
- ✅ **Phase 1 Complete**: `LuaNumber` struct with 83 tests
- ✅ **Phase 2 Complete**: DynValue integration, VM arithmetic opcodes, `math.type()` correct, bitwise operations preserve precision
- ✅ **Phase 3 Complete**: StringModule format specifiers, math.floor/ceil integer promotion
- 🔲 **Phase 4 Pending**: Interop & serialization
- 🔲 **Phase 5 Pending**: Numeric value caching & performance validation

**Key Achievements**:
- `math.maxinteger`/`math.mininteger` return exact values (no precision loss)
- `math.type(1)` → "integer", `math.type(1.0)` → "float" (correct subtype detection)
- Integer arithmetic wraps correctly (two's complement)
- Integer `//` by zero throws error; integer `%` by zero is version-aware (nan in 5.1/5.2, error in 5.3+)
- Float versions of `//` and `%` by zero return IEEE 754 values (inf, nan)
- Bitwise operations preserve full 64-bit integer precision
- `string.format('%d', math.maxinteger)` outputs exact "9223372036854775807" (no precision loss)
- `math.floor(3.7)` and `math.ceil(3.2)` return integer subtypes

See **Section 8.24** for the complete implementation plan.

**Next actionable item**: Phase 4 — Update interop converters (`FromObject`/`ToObject`) for integer preservation.

---

## Repository Snapshot (Updated 2025-12-10)
- **Build**: Zero warnings with `<TreatWarningsAsErrors>true` enforced.
- **Tests**: **4,557** interpreter tests pass via TUnit (Microsoft.Testing.Platform).
- **Coverage**: ~87.7% line / ~86.7% branch (per latest coverage run).
- **Coverage gating**: `COVERAGE_GATING_MODE=enforce` enabled with 90% thresholds.
- **Audits**: `documentation_audit.log`, `naming_audit.log`, `spelling_audit.log` are green.
- **Regions**: Runtime/tooling/tests remain region-free.
- **CI**: Tests run on matrix of `[ubuntu-latest, windows-latest, macos-latest]`.
- **DAP golden tests**: 20 tests validating VS Code debugger protocol payloads.
- **Sandbox infrastructure**: Complete with instruction/memory/coroutine limits, per-mod isolation, callbacks, and presets.
- **Benchmark CI**: `.github/workflows/benchmarks.yml` with BenchmarkDotNet, threshold-based regression alerting.
- **Packaging**: NuGet publishing workflow + Unity UPM scripts in `scripts/packaging/`.
- **Lua Version Comparison**: CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4 reference interpreters.
- **math.floor/ceil fix**: 2025-12-10 — Fixed 2^63 boundary detection to correctly return float for overflow values.
- **Lua Fixture Corpus**: ~1,234 fixtures extracted from C# tests, parallel runner operational.

## Critical Initiatives

### Initiative 12: VM Correctness and State Protection 🔴 **CRITICAL**
**Goal**: Make the VM bulletproof against external state corruption while maintaining full Lua compatibility.
**Scope**: `DynValue` mutability controls, public API audit, table key safety, closure upvalue protection.
**Status**: Analysis complete. See [`docs/proposals/vm-correctness.md`](docs/proposals/vm-correctness.md) for detailed findings.
**Effort**: 1-2 weeks implementation + comprehensive testing

**Key Changes Required**:
1. Make `DynValue.Assign()` internal (prevents external corruption)
2. Fix `Closure.GetUpValue()` to return readonly; add `SetUpValue()` method
3. Ensure table keys are readonly in `_valueMap` (prevents hash corruption)
4. Fix UserData/Thread hash codes (performance)
5. **Full public API audit**: Review all public methods returning `DynValue` for potential corruption vectors

**API Breaking Changes**: Acceptable if required for VM correctness and Lua compatibility.

**Follow-up Task**: Comprehensive audit of all public APIs on VM types (`Script`, `Table`, `Closure`, `Coroutine`, `DynValue`, `UserData`, `CallbackArguments`, etc.) to identify any additional vectors where external code could corrupt or cause unexpected VM state.

### Initiative 9: Version-Aware Lua Standard Library Parity 🔴 **CRITICAL**
**Goal**: ALL Lua functions must behave according to their version specification (5.1, 5.2, 5.3, 5.4).
**Scope**: Math, String, Table, Basic, Coroutine, OS, IO, UTF-8, Debug modules + metamethod behaviors.
**Status**: Comprehensive audit required. See **Section 9** for detailed tracking.
**Effort**: 4-6 weeks

### Initiative 10: KopiLua Performance Hyper-Optimization 🎯 **HIGH**
**Goal**: Zero-allocation string pattern matching. Replace legacy KopiLua allocations with modern .NET patterns.
**Scope**: `CharPtr` → `ref struct`, `MatchState` pooling, `ArrayPool<char>`, `ZString` integration.
**Target**: <50 bytes/match, <400ns latency for simple patterns.
**Status**: Planned. See **Section 10** for detailed implementation plan.
**Effort**: 6-8 weeks

### Initiative 11: Comprehensive Helper Performance Audit 🎯
**Goal**: Audit and optimize ALL helper methods called from interpreter hot paths.
**Scope**: `Helpers/`, `DataTypes/`, `Execution/VM/`, `CoreLib/`, `Interop/`.
**Status**: Planned. See **Section 11** for scope.
**Effort**: 2-3 weeks audit + ongoing optimization

### Initiative 13: Magic String Consolidation 🟡 **MEDIUM**
**Goal**: Eliminate all duplicated string literals ("magic strings") by consolidating them into named constants with a single source of truth.
**Scope**: All runtime, tooling, and test code.
**Status**: Planned. Incremental enforcement during code changes.
**Effort**: Ongoing (apply during code reviews and new development)

**Motivation**:
- Duplicated strings are error-prone (typos, inconsistent updates)
- Refactoring safety: `nameof()` expressions survive renames
- Single source of truth for error messages, Lua keywords, metamethod names, etc.

**Key Areas to Audit**:
1. **Metamethod names**: `__index`, `__newindex`, `__call`, `__tostring`, etc.
2. **Lua keywords**: `nil`, `true`, `false`, `and`, `or`, `not`, `function`, etc.
3. **Error messages**: `bad argument`, `attempt to`, `number has no integer representation`, etc.
4. **Module names**: `string`, `table`, `math`, `io`, `os`, `debug`, `coroutine`, etc.
5. **Format specifiers**: `%d`, `%s`, `%f`, etc. (where appropriate)

**Implementation Guidelines**:
- Use `const string` for compile-time constants
- Use `static readonly string` when runtime initialization is needed
- Prefer `nameof()` for all parameter names, property names, and member references
- Group related constants in dedicated static classes (e.g., `MetamethodNames`, `LuaKeywords`, `ErrorMessages`)
- Apply incrementally: consolidate strings when touching related code

**Validation Commands**:
```bash
# Find potential duplicated magic strings (metamethods)
grep -rn '"__' src/runtime/WallstopStudios.NovaSharp.Interpreter/ | sort | uniq -c | sort -rn | head -20

# Find string literals in ArgumentException/ArgumentNullException (should use nameof)
grep -rn 'ArgumentNullException("' src/runtime/
grep -rn 'ArgumentException.*"[a-z]' src/runtime/
```

## Baseline Controls (must stay green)
- Re-run audits (`documentation_audit.py`, `NamingAudit`, `SpellingAudit`) when APIs or docs change.
- Lint guards (`check-platform-testhooks.py`, `check-console-capture-semaphore.py`, `check-temp-path-usage.py`, `check-userdata-scope-usage.py`, `check-test-finally.py`) run in CI.
- New helpers must live under `scripts/<area>/` with README updates.
- Keep `docs/Testing.md`, `docs/Modernization.md`, and `scripts/README.md` aligned.

## Active Initiatives

### 1. Coverage ceiling (informational)
Coverage has reached a practical ceiling. The remaining ~1.3% gap to 95% branch coverage is blocked by untestable code:
- **DebugModule** (~75 branches): REPL loop cannot be tested (VM state issue).
- **StreamFileUserDataBase** (~27 branches): Windows-specific CRLF paths cannot run on Linux CI.
- **TailCallData/YieldRequest** (~10 branches each): Internal processor paths not directly testable.
- **ScriptExecutionContext** (~30 branches): Internal processor callback/continuation paths.

No further coverage work planned unless these blockers are addressed.

### 2. Codebase organization (future)
- Consider splitting into feature-scoped projects if warranted (e.g., separate Interop, Debugging assemblies)
- Restructure test tree by domain (`Runtime/VM`, `Runtime/Modules`, `Tooling/Cli`)
- Add guardrails so new code lands in correct folders with consistent namespaces

### 2.5. Test modernization: TUnit data-driven attributes (future)
- Migrate loop-based parameterized tests to TUnit `[Arguments]` attributes where compile-time constants allow
- Use `[MethodDataSource]` or `[ClassDataSource]` for runtime data (e.g., `Type` parameters, complex objects)
- Benefits: Better test discovery/reporting in IDEs, clearer test naming per parameter set
- Candidate tests:
  - `IsRunningOnAotTreatsProbeExceptionsAsAotHosts` (exception types)
  - Tests using inline `foreach` loops over test cases
- Reference: [TUnit Data-Driven Tests](https://tunit.dev/)

### 3. Tooling, docs, and contributor experience
- Roslyn source generators/analyzers for NovaSharp descriptors.
- DocFX (or similar) for API documentation.

### 4. Concurrency improvements (optional)
- Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics.
- Split debugger locks for reduced contention.
- Add timeout to `BlockingChannel`.

See `docs/modernization/concurrency-inventory.md` for the full synchronization audit.

## Lua Specification Parity

### Official Lua Specifications (Local Reference)

**IMPORTANT**: For all Lua compatibility work, consult the local specification documents first:
- [`docs/lua-spec/lua-5.1-spec.md`](docs/lua-spec/lua-5.1-spec.md) — Lua 5.1 Reference Manual
- [`docs/lua-spec/lua-5.2-spec.md`](docs/lua-spec/lua-5.2-spec.md) — Lua 5.2 Reference Manual
- [`docs/lua-spec/lua-5.3-spec.md`](docs/lua-spec/lua-5.3-spec.md) — Lua 5.3 Reference Manual
- [`docs/lua-spec/lua-5.4-spec.md`](docs/lua-spec/lua-5.4-spec.md) — Lua 5.4 Reference Manual (primary target)
- [`docs/lua-spec/lua-5.5-spec.md`](docs/lua-spec/lua-5.5-spec.md) — Lua 5.5 (Work in Progress)

These documents contain comprehensive details on:
- Language syntax and semantics
- Type system (nil, boolean, number, string, table, function, userdata, thread)
- Standard library functions with exact signatures and behaviors
- Metamethods and metatable behavior
- Error handling and message formats
- Version-specific changes and breaking changes

**Use these specs** when:
- Implementing or auditing standard library functions
- Verifying VM behavior against spec
- Understanding version-specific differences
- Writing tests for Lua compatibility
- Debugging divergences from reference Lua

### Reference Lua comparison harness
- **Status**: Fully implemented. CI runs matrix tests against Lua 5.1, 5.2, 5.3, 5.4.
- **Gating**: `enforce` mode. Known divergences documented in `docs/testing/lua-divergences.md`.
- **Test authoring pattern**: Use `LuaFixtureHelper` to load `.lua` files from `LuaFixtures/` directory.

### Full Lua specification audit
- **Tracking**: `docs/testing/spec-audit.md` contains detailed tracking table with status per feature.
- **Progress**: Most core features verified against Lua 5.4 manual; `string.pack`/`unpack` extended options remain unimplemented.

### 8. Lua Runtime Specification Parity (CRITICAL)

**Goal**: Ensure NovaSharp behaves identically to reference Lua interpreters across all supported versions (5.1, 5.2, 5.3, 5.4) for deterministic, reproducible script execution.

#### 8.4 String and Pattern Matching

**Potential Divergences**:
- Character class `%a`, `%l`, `%u` etc. use .NET `char.IsXxx()` which may differ from C `isalpha()` etc.
- Unicode handling in patterns (Lua 5.3+ vs earlier)
- `string.format` edge cases (float formatting, padding)

**Tasks**:
- [ ] Compare `%a`, `%d`, `%l`, `%u`, `%w`, `%s` character classes against reference Lua
- [ ] Verify `string.format` output matches for edge cases (NaN, Inf, very large numbers)
- [ ] Test pattern matching with non-ASCII characters
- [ ] Document any intentional Unicode-aware divergences

#### 8.5 os.time and os.date Semantics

**Requirements**:
- `os.time()` with no arguments returns current UTC timestamp
- `os.time(table)` interprets fields per §6.9
- `os.date("*t")` returns table with correct field names and ranges
- Timezone handling differences (C `localtime` vs .NET)

**Tasks**:
- [ ] Verify `os.time()` return value matches Lua's epoch-based timestamp
- [ ] Test `os.date` format strings against reference Lua outputs
- [ ] Document timezone handling differences (if any)
- [ ] Ensure `DeterministicTimeProvider` integration doesn't break compatibility

#### 8.6 Coroutine Semantics

**Critical Behaviors**:
- `coroutine.resume` return value shapes
- `coroutine.wrap` error propagation
- `coroutine.status` state transitions
- Yield across C-call boundary errors

**Tasks**:
- [ ] Create state transition diagram tests for coroutine lifecycle
- [ ] Verify error message formats match Lua
- [ ] Test `coroutine.close` (5.4) cleanup order

#### 8.7 Error Message Parity

**Goal**: Error messages should match Lua's format for maximum compatibility with scripts that parse errors.

**Known Divergences** (from `docs/testing/lua-divergences.md`):
- Nil index: Lua says `(name)`, NovaSharp omits variable name
- Stack traces: .NET format vs Lua format
- Module not found: Different path listing

**Tasks**:
- [ ] Catalog all error message formats in `ScriptRuntimeException`
- [ ] Create error message normalization layer for Lua-compatible output
- [ ] Add `ScriptOptions.LuaCompatibleErrors` flag (opt-in strict mode)

#### 8.8 Verification Infrastructure

**Golden Test Suite**:
- [ ] Create `LuaFixtures/RngParity/` with seeded random sequences per version
- [ ] Create `LuaFixtures/NumericEdgeCases/` for arithmetic edge cases
- [ ] Create `LuaFixtures/ErrorMessages/` for error format verification
- [ ] Extend `compare-lua-outputs.py` to compare byte-for-byte output for determinism tests

**CI Enhancement**:
- [ ] Add Lua 5.1, 5.2, 5.3, 5.4 comparison jobs to the matrix
- [ ] Track parity percentage per version in CI artifacts
- [ ] Alert on parity regressions

#### 8.9 String-to-Number Coercion Changes (Lua 5.4)

**Breaking Change in 5.4**: String-to-number coercion was removed from the core language. Arithmetic operations no longer automatically convert string operands to numbers.

**Tasks**:
- [ ] Verify NovaSharp behavior matches the target `LuaCompatibilityVersion`
- [ ] Ensure string metatable has arithmetic metamethods for 5.4 compatibility
- [ ] Add tests for string arithmetic operations per version
- [ ] Document the coercion change in `docs/LuaCompatibility.md`

#### 8.10 print/tostring Behavior Changes (Lua 5.4)

**Breaking Change in 5.4**: `print` no longer calls the global `tostring` function; it directly uses the `__tostring` metamethod.

**Tasks**:
- [ ] Verify `print` behavior matches target Lua version
- [ ] Add tests for custom `tostring` function interaction with `print`
- [ ] Document behavior difference

#### 8.11 Numerical For Loop Semantics (Lua 5.4)

**Breaking Change in 5.4**: Control variable in integer `for` loops never overflows/wraps.

**Tasks**:
- [ ] Verify NovaSharp for loop handles integer limits correctly per version
- [ ] Add edge case tests for near-maxinteger loop bounds
- [ ] Document loop semantics per version

#### 8.12 io.lines Return Value Changes (Lua 5.4)

**Breaking Change in 5.4**: `io.lines` returns 4 values instead of 1 (adds close function and two placeholders).

**Tasks**:
- [ ] Verify `io.lines` return value count matches target version
- [ ] Add tests for multi-value return unpacking from `io.lines`

#### 8.13 __lt/__le Metamethod Changes (Lua 5.4)

**Breaking Change in 5.4**: `__lt` metamethod no longer emulates `__le` when `__le` is absent.

**Tasks**:
- [ ] Verify comparison operator metamethod fallback per version
- [ ] Add tests for partial metamethod definitions
- [ ] Document metamethod requirements per version

#### 8.14 __gc Metamethod Handling (Lua 5.4)

**Breaking Change in 5.4**: Objects with non-function `__gc` metamethods are no longer silently ignored; they generate errors.

**Tasks**:
- [ ] Verify `__gc` validation matches target version
- [ ] Add tests for invalid `__gc` values
- [ ] Document garbage collection metamethod requirements

#### 8.15 utf8 Library Differences (Lua 5.3 vs 5.4)

**Surrogate Code Points (0xD800-0xDFFF)**:
- **Lua 5.3**: ✅ ACCEPTS surrogates (encodes them without error)
- **Lua 5.4**: ✅ ACCEPTS surrogates (same behavior)
- **Lua 5.4 `lax` mode**: For *decoding* invalid UTF-8 sequences, not for surrogates in `utf8.char`

**Maximum Code Point Value**:
- **Lua 5.3**: 0 to 0x10FFFF (Unicode range)
- **Lua 5.4**: 0 to 0x7FFFFFFF (extended UTF-8 range, uses 5-6 byte sequences)

**Boundary Validation** (SAME for 5.3 and 5.4):
- `utf8.codepoint(s, i, j)`: Throws "out of bounds" / "out of range" for invalid i or j
- `utf8.offset(s, n, i)`: Throws "position out of bounds" for position 0 or beyond string bounds

**NovaSharp Current Status**:
- [x] Extended range support added for Lua 5.4 (`EncodeExtendedUtf8`)
- [x] Surrogate acceptance in both 5.3 and 5.4 modes (fixed 2025-12-10)
- [x] `utf8.codepoint` bounds validation (fixed 2025-12-10)
- [ ] `lax` mode not yet implemented (affects decoding, not `utf8.char`)

**Remaining Tasks**:
- [ ] Verify `utf8.offset` bounds handling is complete
- [ ] Implement `lax` mode for decoding functions (`utf8.codes`, `utf8.codepoint`, `utf8.len`)
- [ ] Document utf8 library version differences

#### 8.16 collectgarbage Options (Lua 5.4)

**Deprecation in 5.4**: `setpause` and `setstepmul` options are deprecated (use `incremental` instead).

**Tasks**:
- [ ] Support deprecated options with warnings when targeting 5.4
- [ ] Implement `incremental` option for 5.4
- [ ] Add tests for GC option compatibility

#### 8.17 Literal Integer Overflow (Lua 5.4)

**Breaking Change in 5.4**: Decimal integer literals that overflow read as floats instead of wrapping.

**Tasks**:
- [ ] Verify lexer/parser handles overflowing literals correctly per version
- [ ] Add tests for large literal parsing
- [ ] Document literal parsing behavior

#### 8.18 bit32 Library Deprecation (Lua 5.3+)

**Breaking Change in 5.3**: The `bit32` library was deprecated in favor of native bitwise operators.

**Tasks**:
- [ ] Verify `bit32` availability matches target version
- [ ] Add compatibility warning when using `bit32` on 5.3
- [ ] Document migration path from `bit32` to native operators

#### 8.19 Environment Changes (Lua 5.2+)

**Breaking Change in 5.2**: The concept of function environments was fundamentally changed.

**Tasks**:
- [ ] Verify environment handling matches target version
- [ ] Support `setfenv`/`getfenv` only for 5.1 compatibility mode
- [ ] Document `_ENV` usage for 5.2+ code

#### 8.20 ipairs Metamethod Changes (Lua 5.3+)

**Breaking Change in 5.3**: `ipairs` now respects `__index` metamethods; the `__ipairs` metamethod was deprecated.

**Tasks**:
- [ ] Verify `ipairs` metamethod behavior per version
- [ ] Add tests for `ipairs` with `__index` metamethod tables
- [ ] Document iterator behavior differences

#### 8.21 table.unpack Location (Lua 5.2+)

**Breaking Change in 5.2**: `unpack` moved from global to `table.unpack`.

**Tasks**:
- [ ] Verify `unpack` availability matches target version
- [ ] Provide global `unpack` alias for 5.1 compatibility mode
- [ ] Document migration from `unpack` to `table.unpack`

#### 8.22 Documentation

- [ ] Update `docs/LuaCompatibility.md` with version-specific behavior notes
- [ ] Add "Determinism Guide" for users needing reproducible execution
- [ ] Document any intentional divergences with rationale
- [ ] Create version migration guides (5.1→5.2, 5.2→5.3, 5.3→5.4)
- [ ] Add "Breaking Changes by Version" quick-reference table

#### 8.24 Dual Numeric Type System (Integer + Float) 🔴 **HIGH PRIORITY**

**Status**: 🚧 **IN PROGRESS** — Phase 3 complete. All 4,404 tests passing.

**Problem Statement**:

Lua 5.3+ has **two distinct numeric subtypes** that NovaSharp currently cannot fully represent:
- **Integer**: 64-bit signed (`long`/`Int64`) with exact range -2^63 to 2^63-1
- **Float**: 64-bit IEEE 754 double precision

The `LuaNumber` struct has been implemented to track integer vs float subtype.

**Phase 4: Interop & Serialization** (3-4 days)
- [ ] Update `FromObject()` / `ToObject()` for integer preservation
- [ ] Update JSON serialization (integers as JSON integers, not floats)
- [ ] Update binary dump/load format (version 2?)
- [ ] Ensure CLR interop handles `int`, `long`, `float`, `double` correctly

**Phase 5: Caching & Performance Validation** (3-4 days)
- [ ] Extend `DynValue` caches for common float values (0.0, 1.0, -1.0, etc.)
- [ ] Add `FromFloat(double)` cache method for hot paths
- [ ] Add negative integer cache (-256 to -1)
- [ ] Run Lua comparison harness against reference Lua 5.3/5.4
- [ ] Performance benchmarking (ensure no significant regression)
- [ ] Memory allocation profiling (verify caching reduces allocations)
- [ ] Documentation updates

**Success Criteria**:
- [x] `math.maxinteger` returns exactly `9223372036854775807` (not rounded)
- [x] `math.type(1)` returns `"integer"`, `math.type(1.0)` returns `"float"`
- [x] `3 // 0` throws error, `3.0 // 0` returns `inf`
- [x] `math.maxinteger & 1` returns `1` (not overflow)
- [x] `string.format('%d', math.maxinteger)` returns "9223372036854775807" (exact)
- [x] `math.floor(3.7)` returns integer subtype (value 3)
- [x] `math.ceil(3.2)` returns integer subtype (value 4)
- [x] All 4,404 existing tests pass
- [ ] Lua comparison harness shows improved parity percentage
- [ ] No performance regression > 5% on benchmarks
- [ ] Numeric caching reduces hot-path allocations

**Owner**: Interpreter team
**Priority**: 🔴 HIGH — Required for full Lua 5.3+ specification compliance

## Long-horizon Ideas
- Property and fuzz testing for lexer, parser, VM.
- CLI output golden tests.
- Native AOT/trimming validation.
- Automated allocation regression harnesses.

## Recommended Next Steps (Priority Order)

### Active/Upcoming Items

1. **Dual Numeric Type System - Phase 4-5** (Initiative 8.24): 🔴 **HIGH PRIORITY**
    - Phase 4: Update interop converters (`FromObject`/`ToObject`) for integer preservation
    - Phase 5: Caching & performance validation
    - See **Section 8.24** for full plan

2. **Lua Specification Parity - String/Pattern Matching** (Initiative 8.4): 🎯 **NEXT PRIORITY**
    - Compare `%a`, `%d`, `%l`, `%u`, `%w`, `%s` character classes against reference Lua
    - Verify `string.format` output matches for edge cases (NaN, Inf, very large numbers)
    - Document any intentional Unicode-aware divergences

3. **Tooling enhancements** (Initiative 6):
    - Roslyn source generators/analyzers for NovaSharp descriptors
    - DocFX (or similar) for API documentation
    - CLI output golden tests

### Future Phases (Lower Priority)

4. **Interpreter hyper-optimization - Phase 4** (Initiative 5): 🔮 **PLANNED** — Zero-allocation runtime goal
    
    **Target:** Match or exceed native Lua performance; achieve <100 bytes/call allocation overhead.
    
    See `docs/performance/optimization-opportunities.md` for comprehensive plan covering:
    - VM dispatch optimization (computed goto, opcode fusion)
    - Table redesign (hybrid array+hash like native Lua)
    - DynValue struct conversion (optional breaking change)
    - Span-based APIs throughout
    - Roslyn source generators for interop

5. **Concurrency improvements** (Initiative 7, optional):
    - Consider `System.Threading.Lock` (.NET 9+) for cleaner lock semantics
    - Split debugger locks for reduced contention
    - Add timeout to `BlockingChannel`

6. **Raise coverage gates to >95%** (Initiative 12): 🟢 **LOW PRIORITY**
    
    **Status**: 📋 **PLANNED** — Current gates at 90% across the board.
    
    **Goal**: Incrementally raise CI/CD coverage gates as coverage improves, targeting >95% for all metrics.
    
    **Current thresholds (2025-12-09)**:
    - Line coverage: 90%
    - Branch coverage: 90%
    - Method coverage: 90%
    
    **Target thresholds**:
    - Line coverage: ≥96%
    - Branch coverage: ≥94%
    - Method coverage: ≥98%
    
    **Tasks**:
    - [ ] Monitor coverage trends as new features and tests are added
    - [ ] When coverage consistently exceeds current gates by 2%+, raise thresholds
    - [ ] Prioritize branch coverage improvements (often the lowest metric)
    - [ ] Consider per-assembly coverage requirements for critical paths
    - [ ] Update `.github/workflows/tests.yml` and `docs/Testing.md` when thresholds change
    
    **Owner**: Quality team
    **Priority**: 🟢 LOW — Nice-to-have quality improvement

---
Keep this plan aligned with `docs/Testing.md` and `docs/Modernization.md`.

---

## Initiative 9: Version-Aware Lua Standard Library Parity 🔴 **CRITICAL**

**Status**: 🚧 **IN PROGRESS** — Comprehensive audit required to ensure ALL Lua functions behave correctly per version.

**Priority**: CRITICAL — Core interpreter correctness for production use.

**Goal**: Every Lua function and language feature must behave according to the specification for the configured `LuaCompatibilityVersion`. This is not just about API surface (whether a function exists) but about behavioral semantics that differ between versions.

### 9.1 Math Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `math.random()` | LCG | LCG | LCG | xoshiro256** | ✅ Completed | Version-specific RNG |
| `math.randomseed(x)` | 1 arg, nil return | 1 arg, nil return | 1 arg, nil return | 0-2 args, returns (x,y) | ✅ Completed | Version-aware behavior |
| `math.type(x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Returns "integer"/"float" |
| `math.tointeger(x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Integer conversion |
| `math.ult(m, n)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | Unsigned comparison |
| `math.maxinteger` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | 2^63-1 |
| `math.mininteger` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Completed | -2^63 |
| `math.log(x [,base])` | 1 arg only | 1-2 args | 1-2 args | 1-2 args | 🔲 Verify | Check 5.1 signature |
| `math.log10(x)` | ✅ | ⚠️ Deprecated | ⚠️ Deprecated | ⚠️ Deprecated | 🔲 Verify | Warn in 5.2+ |
| `math.ldexp(m, e)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `math.frexp(x)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `math.pow(x, y)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Use `x^y` in 5.3+ |
| `math.mod(x, y)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `x%y` in 5.1+ |
| `math.fmod(x, y)` | ✅ | ✅ | ✅ | ✅ | ✅ Available | Float modulo |
| `math.modf(x)` | Float parts | Float parts | Int+Float parts | Int+Float parts | 🔲 Verify | Integer promotion in 5.3+ |
| `math.floor(x)` | Float | Float | Integer if fits | Integer if fits | ✅ Completed | Integer promotion |
| `math.ceil(x)` | Float | Float | Integer if fits | Integer if fits | ✅ Completed | Integer promotion |

**Tasks**:
- [ ] Audit all `math` functions for version-specific behavior
- [ ] Implement `[LuaCompatibility]` gating for deprecated/removed functions
- [ ] Add version-specific tests for each function
- [ ] Implement deprecation warnings for 5.2+ deprecated functions
- [ ] Verify `math.modf` returns integer+float in 5.3+

### 9.2 String Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `string.pack(fmt, ...)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.unpack(fmt, s [,pos])` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.packsize(fmt)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🚧 Partial | Extended options missing |
| `string.format('%a', x)` | ❌ N/A | ❌ N/A | ✅ | ✅ | 🔲 Verify | Hex float format |
| `string.format('%d', maxint)` | Double precision | Double precision | Integer precision | Integer precision | ✅ Completed | LuaNumber precision |
| `string.gmatch(s, pattern [,init])` | No init | No init | No init | ✅ init arg | 🔲 Verify | 5.4 added init parameter |
| Pattern `%g` (graphical) | ❌ N/A | ✅ | ✅ | ✅ | 🔲 Verify | Added in 5.2 |
| Frontier pattern `%f[]` | ✅ | ✅ | ✅ | ✅ | ✅ Available | All versions |

**Tasks**:
- [ ] Complete `string.pack`/`unpack` extended format options (`c`, `z`, alignment)
- [ ] Implement `string.format('%a')` hex float format specifier
- [ ] Add `init` parameter to `string.gmatch` for Lua 5.4
- [ ] Verify `%g` character class availability per version
- [ ] Document string pattern differences between versions

### 9.3 Table Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `table.pack(...)` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Sets `n` field |
| `table.unpack(list [,i [,j]])` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Replaces global `unpack` |
| `table.move(a1, f, e, t [,a2])` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Metamethod-aware |
| `table.maxn(table)` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | Version gate |
| `table.getn(table)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `#table` |
| `table.setn(table, n)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Removed |
| `table.foreachi(t, f)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `ipairs` |
| `table.foreach(t, f)` | ⚠️ Deprecated | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `pairs` |

**Tasks**:
- [ ] Implement `[LuaCompatibility]` gating for deprecated/removed table functions
- [ ] Add global `unpack` alias for Lua 5.1 mode
- [ ] Verify `table.maxn` available only in 5.1-5.2

### 9.4 Basic Functions Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `setfenv(f, table)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | 5.1 only |
| `getfenv(f)` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | 5.1 only |
| `unpack(list [,i [,j]])` | ✅ Global | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Implement | Moved to `table.unpack` |
| `module(name [,...])` | ✅ | ⚠️ Deprecated | ❌ Removed | ❌ Removed | 🔲 Verify | 5.1 module system |
| `loadstring(string [,chunkname])` | ✅ | ❌ Removed | ❌ Removed | ❌ Removed | 🔲 Verify | Use `load(string)` |
| `load(chunk [,chunkname [,mode [,env]]])` | 2-3 args | 4 args | 4 args | 4 args | 🔲 Verify | Signature change |
| `loadfile(filename [,mode [,env]])` | 1 arg | 3 args | 3 args | 3 args | 🔲 Verify | Signature change |
| `rawlen(v)` | ❌ N/A | ✅ | ✅ | ✅ | ✅ Available | Added in 5.2 |
| `xpcall(f, msgh [,...])` | 2 args | Extra args | Extra args | Extra args | 🔲 Verify | 5.2+ passes args to f |
| `print(...)` behavior | Calls tostring | Calls tostring | Calls tostring | Uses __tostring | 🔲 Implement | 5.4 change |
| String-to-number coercion | Implicit | Implicit | Implicit | Metamethod | 🔲 Implement | 5.4 breaking change |

**Tasks**:
- [ ] Implement `setfenv`/`getfenv` for Lua 5.1 compatibility mode
- [ ] Add global `unpack` for Lua 5.1 mode
- [ ] Implement `print` behavior change for Lua 5.4 (`__tostring` directly)
- [ ] Implement string-to-number coercion via metamethods for Lua 5.4
- [ ] Verify `xpcall` argument passing per version
- [ ] Verify `load`/`loadfile` signature per version

### 9.5 Coroutine Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `coroutine.isyieldable()` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Added in 5.3 |
| `coroutine.close(co)` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | ✅ Available | Added in 5.4 |
| `coroutine.running()` | Returns co only | Returns co, bool | Returns co, bool | Returns co, bool | 🔲 Verify | Return shape |

**Tasks**:
- [ ] Verify `coroutine.running()` return value per version

### 9.6 OS Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `os.execute(command)` | Returns status | Returns (ok, signal, code) | Returns tuple | Returns tuple | ✅ Available | |
| `os.exit(code [,close])` | 1 arg | 2 args | 2 args | 2 args | 🔲 Verify | `close` param |

**Tasks**:
- [ ] Verify `os.execute` return value per version
- [ ] Verify `os.exit` `close` parameter support

### 9.7 IO Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `io.lines(filename, ...)` | Returns iterator | Returns iterator | Returns iterator | Returns 4 values | 🔲 Implement | 5.4 breaking change |
| `io.read("*n")` | Number | Number | Number | Number | ✅ Available | Hex parsing in 5.3+ |
| `file:setvbuf(mode [,size])` | ✅ | ✅ | ✅ | ✅ | 🔲 Verify | Buffer modes |

**Tasks**:
- [ ] Implement `io.lines` 4-return-value for Lua 5.4
- [ ] Verify `io.read("*n")` hex parsing per version

### 9.8 UTF-8 Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `utf8.char(...)` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Surrogates accepted in both |
| `utf8.codes(s [,lax])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | 🔲 Verify | `lax` mode in 5.4 |
| `utf8.codepoint(s [,i [,j [,lax]]])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | ✅ Available | Bounds validation fixed |
| `utf8.len(s [,i [,j [,lax]]])` | ❌ N/A | ❌ N/A | ✅ | ✅ (lax) | 🔲 Verify | `lax` mode in 5.4 |
| `utf8.offset(s, n [,i])` | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | Position 0 check exists |
| Max code point | ❌ N/A | ❌ N/A | 0x10FFFF | 0x7FFFFFFF | ✅ Available | Extended range in 5.4 |

**Tasks**:
- [ ] Implement `lax` mode parameter for UTF-8 functions in Lua 5.4
- [ ] Verify `utf8.offset` bounds handling is complete

### 9.9 Debug Module Version Parity

| Function | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|----------|-----|-----|-----|-----|------------------|-------|
| `debug.setcstacklimit(limit)` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | 🔲 Implement | 5.4 only |
| `debug.setmetatable(value, table)` | 1st return | 1st return | 1st return | boolean | 🔲 Verify | Return type change |
| `debug.getuservalue(u [,n])` | ❌ N/A | ✅ (1 value) | ✅ (1 value) | ✅ (n-th value) | 🔲 Implement | 5.4 multi-user-values |
| `debug.setuservalue(u, value [,n])` | ❌ N/A | ✅ | ✅ | ✅ (n-th value) | 🔲 Implement | 5.4 multi-user-values |

**Tasks**:
- [ ] Implement `debug.setcstacklimit` for Lua 5.4
- [ ] Verify `debug.setmetatable` return value per version
- [ ] Implement multi-user-value support for 5.4

### 9.10 Bitwise Operations Version Parity

| Feature | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|---------|-----|-----|-----|-----|------------------|-------|
| `bit32` library | ❌ N/A | ✅ | ⚠️ Deprecated | ❌ Removed | ✅ Available | Version-gated |
| Native `&`, `|`, `~` operators | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| `~` unary (bitwise NOT) | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |
| `<<`, `>>` operators | ❌ N/A | ❌ N/A | ✅ | ✅ | ✅ Available | |

**Tasks**:
- [ ] Emit deprecation warning when `bit32` used in 5.3 mode
- [ ] Verify `bit32` unavailable in 5.4 mode

### 9.11 Metamethod Behavior Version Parity

| Metamethod | 5.1 | 5.2 | 5.3 | 5.4 | NovaSharp Status | Notes |
|------------|-----|-----|-----|-----|------------------|-------|
| `__lt` emulates `__le` | ✅ | ✅ | ✅ | ❌ No | 🔲 Implement | 5.4 breaking change |
| `__gc` non-function error | Silent | Silent | Silent | Error | 🔲 Implement | 5.4 breaking change |
| `__pairs`/`__ipairs` | ❌ N/A | ✅ | ✅ (no __ipairs) | ✅ (no __ipairs) | 🔲 Verify | `__ipairs` deprecated 5.3 |
| `__close` | ❌ N/A | ❌ N/A | ❌ N/A | ✅ | ✅ Available | |

**Tasks**:
- [ ] Implement `__lt` emulation removal for Lua 5.4
- [ ] Implement `__gc` validation for Lua 5.4
- [ ] Verify `__ipairs` behavior per version

### 9.12 Testing Infrastructure

**Tasks**:
- [ ] Create comprehensive version matrix tests for all modules
- [ ] Create `LuaFixtures/VersionParity/` test directory with per-function fixtures
- [ ] Add CI jobs that run test suite with each `LuaCompatibilityVersion`
- [ ] Create version migration guide (`docs/LuaVersionMigration.md`)
- [ ] Document all version-specific behaviors in `docs/LuaCompatibility.md`

**Success Criteria**:
- All Lua standard library functions behave according to their version specification
- Version-gated functions raise appropriate errors or deprecation warnings
- CI validates all behaviors against reference Lua interpreters (5.1, 5.2, 5.3, 5.4)
- Documentation clearly explains behavior differences per version

**Owner**: Interpreter team
**Effort Estimate**: 4-6 weeks comprehensive audit and implementation

---

## Initiative 10: KopiLua Performance Hyper-Optimization 🎯 **HIGH PRIORITY**

**Status**: 🔲 **PLANNED** — Critical for interpreter hot-path performance.

**Priority**: HIGH — KopiLua code is called from string pattern matching hot paths.

**Goal**: Dramatically reduce allocations and improve performance of all KopiLua-derived code. Target: zero-allocation in steady state, match or exceed native Lua performance.

### 10.1 KopiLua String Library Analysis

**Key Performance Issues Identified**:

| Issue | Location | Impact | Fix Strategy |
|-------|----------|--------|--------------|
| `CharPtr` class allocations | Throughout | HIGH | Convert to `ref struct` or `ReadOnlySpan<char>` |
| `MatchState` class allocations | Every pattern match | HIGH | Object pooling or struct conversion |
| `new char[]` allocations | `Scanformat`, `str_format` | MEDIUM | Use `ArrayPool<char>` or stack allocation |
| String concatenation | `LuaLError` calls, error messages | MEDIUM | Use `ZString` |
| `Capture[]` array allocation | `MatchState` constructor | HIGH | Pre-allocate static pool |
| `LuaLBuffer` allocations | `str_gsub`, `str_format` | HIGH | Pool or `StringBuilder` replacement |

### 10.2 Implementation Phases

**Phase 1: Infrastructure (1 week)**
- [ ] Add benchmarking infrastructure for KopiLua operations
- [ ] Establish baseline measurements
- [ ] Document current allocation patterns

**Phase 2: Critical Path Optimization (2 weeks)**
- [ ] Implement `CharSpan` ref struct replacement
- [ ] Implement `MatchState` pooling
- [ ] Replace `new char[]` with `ArrayPool<char>`

**Phase 3: Comprehensive Optimization (2 weeks)**
- [ ] Modernize `LuaLBuffer`
- [ ] Integrate `ZString` for error messages
- [ ] Optimize character classification methods

**Phase 4: Validation (1 week)**
- [ ] Run full benchmark suite
- [ ] Verify allocation targets met
- [ ] Test on all target platforms

### 10.3 Success Metrics

| Metric | Current (Estimated) | Target |
|--------|---------------------|--------|
| Allocations per `string.match` | ~500 bytes | <50 bytes |
| Allocations per `string.gsub` | ~2000 bytes | <200 bytes |
| Allocations per `string.format` | ~1500 bytes | <100 bytes |
| `string.match` latency (simple) | ~800 ns | <400 ns |

**Owner**: Interpreter team
**Effort Estimate**: 6-8 weeks total

---

## Initiative 11: Comprehensive Helper Performance Audit 🎯

**Status**: 🔲 **PLANNED**

**Priority**: HIGH — All interpreter hot-path helpers need audit.

**Goal**: Identify and optimize ALL helper methods called from interpreter hot paths, not just KopiLua.

### 11.1 Scope

All code in these namespaces/directories that is called from VM execution:
- `LuaPort/` (KopiLua-derived, covered by Initiative 10)
- `Helpers/` (LuaIntegerHelper, LuaStringHelper, etc.)
- `DataTypes/` (DynValue, Table, Closure operations)
- `Execution/VM/` (Processor instruction handlers)
- `CoreLib/` (Standard library module implementations)
- `Interop/` (CLR bridging, type conversion)

### 11.2 Optimization Patterns to Apply

- Use `[MethodImpl(AggressiveInlining)]` for small methods
- Replace LINQ with manual loops in hot paths
- Use `Span<T>` for buffer operations
- Pool any allocated objects
- Cache computed values where safe

**Owner**: Interpreter team
**Effort Estimate**: 2-3 weeks for comprehensive audit + ongoing optimization work

---

## Initiative 12: Lua-to-C# Ahead-of-Time Compiler (Offline DLL Generation) 🔬

**Status**: 🔲 **RESEARCH** — Long-term investigation item.

**Priority**: 🟢 **LOW** — Future optimization opportunity for game developers.

**Goal**: Investigate feasibility of creating an offline "Lua → C# compiler" tool that can compile Lua scripts into .NET DLLs loadable by NovaSharp for improved runtime performance.

### 12.1 Concept Overview

Game developers using NovaSharp could ship an offline compilation tool with their game that allows players (or modders) to pre-compile their Lua scripts into native .NET assemblies. These compiled DLLs would:

- Load significantly faster than interpreted Lua (no parsing/compilation at runtime)
- Execute faster due to JIT-optimized native code
- Still integrate seamlessly with NovaSharp's runtime (tables, coroutines, C# interop)
- Be optional—interpreted Lua would remain fully supported

### 12.2 Research Questions

1. **Feasibility**: Can Lua's dynamic semantics (metatables, dynamic typing, `_ENV` manipulation) be reasonably compiled to static C#?

2. **Performance Gains**: What speedup is realistic? (Likely 2-10x for compute-heavy scripts, minimal for I/O-bound)

3. **Compatibility**: How do compiled scripts interact with:
   - Interpreted Lua scripts calling compiled functions?
   - Runtime `require()` and module loading?
   - Debug hooks and coroutine yield points?
   - Dynamic `_G` / `_ENV` modifications?

4. **Code Generation Strategy**:
   - Direct IL emission vs. C# source generation (Roslyn)?
   - How to handle Lua's 1-based arrays and `nil` semantics?
   - Representation of Lua tables in compiled code?

5. **Tooling Requirements**:
   - Standalone CLI tool vs. Unity Editor integration?
   - Incremental compilation support?
   - Source maps for debugging compiled scripts?

### 12.3 Prior Art to Study

- **LuaJIT**: Highly optimized tracing JIT—study its IR and optimization passes
- **Ravi**: Lua 5.3 derivative with optional static typing and LLVM backend
- **Typed Lua**: Academic work on gradual typing for Lua
- **MoonSharp's own hardwire system**: Existing precompilation for C# interop descriptors
- **IronPython/IronRuby**: How .NET handled dynamic language compilation

### 12.4 Potential Architecture

```
Lua Source → [NovaSharp Parser] → AST → [Type Inference Pass] → Typed AST
    → [C# Code Generator] → Generated .cs files → [Roslyn] → DLL
```

Or alternatively:
```
Lua Source → [NovaSharp Compiler] → Bytecode → [Bytecode-to-IL Translator] → DLL
```

### 12.5 Risks & Challenges

- **Semantic Fidelity**: Lua's extreme dynamism may resist static compilation
- **Maintenance Burden**: Two execution paths (interpreted + compiled) doubles testing surface
- **Edge Cases**: Metamethod chains, `debug.setlocal`, `load()` with dynamic strings
- **Unity IL2CPP**: Compiled DLLs must work under Unity's AOT restrictions

### 12.6 Success Criteria (If Pursued)

- [ ] Prototype compiles simple Lua scripts (no metatables) to working C# code
- [ ] Benchmark shows measurable speedup (>2x) on compute benchmarks
- [ ] Compiled code can call and be called by interpreted Lua
- [ ] Tool runs standalone (no NovaSharp runtime required for compilation)
- [ ] Works with Unity IL2CPP builds

**Owner**: TBD (requires dedicated research effort)
**Effort Estimate**: Unknown—initial feasibility study: 2-4 weeks; full implementation: 3-6 months


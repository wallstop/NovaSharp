# Session 054: §8.44 Phase 2 — Variable Names in Error Messages

**Date**: 2025-12-20
**Status**: ✅ **COMPLETE**

## Objective

Implement Lua-compatible error messages that include variable names, matching Lua's format:

- Lua: `attempt to index nil (global 'foo')`
- NovaSharp (before): `attempt to index a nil value`
- NovaSharp (after with flag): `attempt to index nil (global 'foo')`

## Implementation Summary

### 1. New ScriptOptions Flag

Added `ScriptOptions.LuaCompatibleErrors` (default: `false`) to opt-in to Lua-style error messages with variable names:

```csharp
// ScriptOptions.cs
public bool LuaCompatibleErrors { get; set; }
```

### 2. Bytecode Changes

Added `Name` field to index-related opcodes to track the variable name through compilation:

**Affected Opcodes**:

- `Index` / `IndexN` / `IndexL`
- `SetIndex` / `IndexSet` / `IndexSetN` / `IndexSetL`

The `Name` field stores a formatted description like:

- `"global 'foo'"` for global variables
- `"local 'bar'"` for local variables
- `"upvalue 'baz'"` for upvalues

### 3. Compiler Changes

Updated `FunctionBuilder.Emit()` to pass variable descriptions when emitting index operations:

```csharp
// FunctionBuilder.cs
public string GetVariableDescription(SymbolRef symbol)
{
    // Returns formatted string like "global 'foo'"
}
```

### 4. VM Changes

Updated `ProcessorInstructionLoop.cs` to check `LuaCompatibleErrors` flag and pass variable names to error helpers:

```csharp
// ProcessorInstructionLoop.cs (lines ~2146, ~2252)
string varDesc = _script.Options.LuaCompatibleErrors ? i.Name : null;
```

### 5. Exception Helper Changes

Updated `ScriptRuntimeException.IndexOnNilReference()` and related methods to accept optional variable description:

```csharp
// ScriptRuntimeException.cs
public static ScriptRuntimeException IndexOnNilReference(
    Script script, 
    DynValue value, 
    string variableDescription = null)
```

## Files Modified

| File                                                                                                                                   | Changes                                                   |
| -------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------- |
| [ScriptOptions.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/ScriptOptions.cs)                                              | Added `LuaCompatibleErrors` property                      |
| [Instruction.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Instruction.cs)                                     | Added `Name` field to index opcodes                       |
| Compiler changes                                                                                                                       | Added `GetVariableDescription()` method, updated `Emit()` |
| [ScriptRuntimeException.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Errors/ScriptRuntimeException.cs)                     | Updated error message factories                           |
| [DynValue.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/DataTypes/DynValue.cs)                                              | Added `GetTypeNameForError()` method                      |
| [ProcessorInstructionLoop.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor/ProcessorInstructionLoop.cs) | Pass variable names when `LuaCompatibleErrors` enabled    |

## Test Coverage

- Added 20+ tests in `ScriptRuntimeExceptionTUnitTests` for variable name in errors
- Added 27 tests for `LuaCompatibleErrors` flag behavior
- All existing tests continue to pass (backward compatible)

## Verification

```bash
# ScriptRuntimeException tests (60 tests)
./scripts/test/quick.sh -c ScriptRuntimeException
# Result: 60 passed

# LuaCompatibleErrors feature tests (27 tests)  
./scripts/test/quick.sh LuaCompatibleErrors
# Result: 27 passed

# VariableName tests (20 tests)
./scripts/test/quick.sh VariableName
# Result: 20 passed

# BasicModule tests (212 tests)
./scripts/test/quick.sh -c BasicModule
# Result: 212 passed
```

## Usage

```csharp
// Opt-in to Lua-compatible error messages
var script = new Script();
script.Options.LuaCompatibleErrors = true;

// Now errors will include variable names:
// "attempt to index nil (global 'foo')"
// "attempt to call nil (local 'myFunc')"
```

## Backward Compatibility

- Default is `false` — existing behavior preserved
- Error message changes are opt-in only
- No breaking changes to public API

## Next Steps

- §8.44 Phase 3: Module search path in errors (planned)
- §8.44 Phase 4: Debug prompt format (planned)

## References

- PLAN.md §8.44: Lua Output Format Alignment
- Session 053: Phase 1 — Address format alignment

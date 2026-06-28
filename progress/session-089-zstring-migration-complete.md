# Session 089: ZString Migration Complete

**Date**: 2025-12-22
**Initiative**: 22 — ZString Migration — Zero-Allocation String Operations
**Status**: ✅ **COMPLETE**

## Overview

Completed full migration of all string interpolation (`$"..."`) in the NovaSharp interpreter runtime to zero-allocation ZString patterns. This eliminates unnecessary heap allocations in hot paths for error messages, diagnostic strings, and formatted output.

## Summary

| Metric                               | Value         |
| ------------------------------------ | ------------- |
| **Files Modified**                   | 40            |
| **Interpolation Instances Replaced** | 101           |
| **Final Interpolation Count**        | 0             |
| **Tests Passing**                    | 11,835 (100%) |

## Phase Breakdown

### Phase 1: CoreLib Modules (HIGH Impact)

**Files**: 7 | **Instances**: 20

| File                | Changes                                                       |
| ------------------- | ------------------------------------------------------------- |
| OsTimeModule.cs     | 2 instances — Field validation error messages                 |
| MathModule.cs       | 2 instances — Range check errors for Lua 5.3/5.4              |
| StringModule.cs     | 4 instances — `char` function error messages                  |
| IoModule.cs         | 1 instance — File open error messages                         |
| LoadModule.cs       | 1 instance — File not found error messages                    |
| DebugModule.cs      | 9 instances — Hex formatting, function addresses, upvalue IDs |
| FileUserDataBase.cs | 1 instance — File handle ToString()                           |

### Phase 2: Execution/VM (HIGH Impact)

**Files**: 8 | **Instances**: 10

| File                        | Changes                                     |
| --------------------------- | ------------------------------------------- |
| ProcessorInstructionLoop.cs | 1 instance — Unknown opcode error           |
| ProcessorCallSupport.cs     | 1 instance — Diagnostic messages            |
| CallStackItem.cs            | 1 instance — Location formatting            |
| Chunk.cs                    | 1 instance — Unique ID generation           |
| ScriptLoadingContext.cs     | 2 instances — FunctionEnv/FunctionName      |
| ModuleResolutionContext.cs  | 2 instances — ModuleName/SearchPath         |
| ByteCode.cs                 | 2 instances — Invalid/unknown opcode errors |
| Coroutine.cs                | 1 instance — Coroutine name formatting      |
| ByteCodeVersion.cs          | 1 instance — Version mismatch error         |

### Phase 3: Interop (MEDIUM Impact)

**Files**: 5 | **Instances**: 9

| File                                  | Changes                                      |
| ------------------------------------- | -------------------------------------------- |
| TypeWiringDescriptor.cs               | 1 instance — Wiring error message            |
| JsonTableConverter.cs                 | 1 instance — Serialization unsupported error |
| StandardUserDataDescriptor.cs         | 3 instances — Event callback labels          |
| LuaCallClrAttributeValidator.cs       | 3 instances — Attribute validation errors    |
| StandardGenericsUserDataDescriptor.cs | 1 instance — NameWithGenerics method         |

### Phase 4: Tree/Parser + Remaining (LOW Impact)

**Files**: 20 | **Instances**: 62

| File                             | Changes                                                    |
| -------------------------------- | ---------------------------------------------------------- |
| Script.cs                        | 9 instances — Module loading, chunk naming, error messages |
| ModuleArgumentValidation.cs      | 4 instances — Bad argument error messages                  |
| FunctionDefinitionExpression.cs  | 1 instance — Local function names                          |
| SymbolRef.cs                     | 3 instances — Symbol type descriptions                     |
| SourceCode.cs                    | 1 instance — Error message formatting                      |
| DynValue.cs                      | 4 instances — ToString() success/failure messages          |
| ScriptCompatibilityExtensions.cs | 4 instances — Compatibility warnings                       |
| ScriptUserDataOptions.cs         | 2 instances — Profile info messages                        |
| ScriptModule.cs                  | 13 instances — Load/unload/reload status messages          |
| HardwireMemberDescriptorBase.cs  | 3 instances — Method signature errors                      |
| NovaSharpCli.cs                  | 1 instance — ExecuteCommand argument                       |
| InstructionDumper.cs             | 4 instances — NOP comments                                 |
| ByteCodeWriteContext.cs          | 2 instances — Location/context messages                    |
| SymbolRefInfo.cs                 | 2 instances — ToString() with symbol info                  |
| Table.cs                         | 1 instance — FormatTypeString                              |
| ModuleArguments.cs               | 1 instance — Integer representation error                  |
| LuaTypeExtensions.cs             | 1 instance — Internal type format                          |
| SourceCodeBreakpoint.cs          | 1 instance — ToString() breakpoint location                |
| LuaFunctionAvailability.cs       | 4 instances — Availability error messages                  |
| LuaVersionFeatureFlags.cs        | 1 instance — Feature summary                               |

## Migration Patterns Used

### Pattern 1: `ZString.Concat()` for Simple Concatenations (2-3 elements)

```csharp
// ❌ BEFORE
throw new Exception($"Error in {method}");

// ✅ AFTER
throw new Exception(ZString.Concat("Error in ", method));
```

### Pattern 2: `ZStringBuilder.Create()` for Complex Strings (4+ elements)

```csharp
// ❌ BEFORE
return $"bad argument #{argNum} ({message})";

// ✅ AFTER
using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
sb.Append("bad argument #");
sb.Append(argNum);
sb.Append(" (");
sb.Append(message);
sb.Append(')');
return sb.ToString();
```

### Pattern 3: `ZString.Format()` for Hex/Culture-Specific Formatting

```csharp
// ❌ BEFORE
return $"0x{refId:x}";

// ✅ AFTER
return ZString.Format(CultureInfo.InvariantCulture, "0x{0:x}", refId);
```

## Validation

### Build

```bash
./scripts/build/quick.sh
# ✅ Build succeeded (2.6s)
```

### Tests

```bash
./scripts/test/quick.sh --no-build
# ✅ All 11,835 tests passed (35.7s)
```

### Final Interpolation Count

```bash
rg '\$"' src/runtime/WallstopStudios.NovaSharp.Interpreter/ --type cs | wc -l
# 0
```

## Impact

### Allocation Reduction

- Eliminated intermediate string allocations in hot error paths
- `ZString.Concat()` uses stack allocation for small strings
- `ZStringBuilder` uses pooled buffers, avoiding `StringBuilder` overhead

### Performance Characteristics

- Error message construction now zero-allocation in most cases
- Diagnostic/debug strings use pooled builders
- No regression in functionality — all 11,835 tests pass

## Files Modified (Full List)

```
src/runtime/WallstopStudios.NovaSharp.Interpreter/
├── CoreLib/
│   ├── DebugModule.cs
│   ├── IO/FileUserDataBase.cs
│   ├── IoModule.cs
│   ├── LoadModule.cs
│   ├── MathModule.cs
│   ├── OsTimeModule.cs
│   └── StringModule.cs
├── DataTypes/
│   ├── DynValue.cs
│   └── LuaTypeExtensions.cs
├── Debugging/
│   └── SourceCodeBreakpoint.cs
├── Execution/
│   ├── ByteCodeVersion.cs
│   ├── Chunk.cs
│   ├── ScriptModule.cs
│   └── VM/
│       ├── ByteCode.cs
│       ├── Coroutine.cs
│       ├── Processor/
│       │   ├── CallStackItem.cs
│       │   ├── ProcessorCallSupport.cs
│       │   └── ProcessorInstructionLoop.cs
│       ├── ModuleResolutionContext.cs
│       └── ScriptLoadingContext.cs
├── Interop/
│   ├── Converters/JsonTableConverter.cs
│   ├── LuaCallClrAttributeValidator.cs
│   ├── StandardDescriptors/
│   │   ├── HardwireMemberDescriptors/HardwireMemberDescriptorBase.cs
│   │   ├── StandardGenericsUserDataDescriptor.cs
│   │   └── StandardUserDataDescriptor.cs
│   └── TypeWiringDescriptor.cs
├── Options/
│   ├── LuaFunctionAvailability.cs
│   ├── LuaVersionFeatureFlags.cs
│   ├── ScriptCompatibilityExtensions.cs
│   └── ScriptUserDataOptions.cs
├── Serialization/
│   ├── ByteCodeWriteContext.cs
│   └── InstructionDumper.cs
├── Tree/
│   ├── Expressions/FunctionDefinitionExpression.cs
│   ├── SymbolRef.cs
│   └── SymbolRefInfo.cs
├── ModuleArgumentValidation.cs
├── ModuleArguments.cs
├── Script.cs
├── SourceCode.cs
└── Table.cs

src/tooling/NovaSharp.Cli/
└── NovaSharpCli.cs
```

## Related Work

- **Initiative 10**: KopiLua optimization (CharPtr struct) — Completed
- **Initiative 11**: Helper inlining audit — Completed
- **Initiative 12**: Deep allocation analysis — Completed
- **Initiative 18**: Token struct conversion — Completed
- **Initiative 23**: Span-based migrations — Next priority

## Next Steps

With Initiative 22 complete, the next high-priority item is **Initiative 23: Span-Based Array Operation Migration** which targets:

- `string.Split()` → Span-based enumeration
- `string.Substring()` → `AsSpan().Slice()`
- `ToCharArray()` → `AsSpan()`
- `ToArray()` in hot paths → `ArrayPool` or span

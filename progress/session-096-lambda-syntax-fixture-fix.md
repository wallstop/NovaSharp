# Lambda Syntax Fixture Fix

**Date**: 2025-12-29
**Initiative**: Lua Comparison Test Mismatch Investigation
**Scope**: Closure/Lambda function handling fixtures

## Summary

Fixed Lua comparison test mismatches for two closure fixtures that use NovaSharp's metalua-style lambda syntax extension. These fixtures were incorrectly marked as comparable to reference Lua, but this syntax is a NovaSharp-specific extension.

## Investigation

### Files Investigated

1. [ClosureTUnitTests/ClosureOnParamLambda.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/ClosureTUnitTests/ClosureOnParamLambda.lua)
2. [ClosureTUnitTests/LambdaFunctions.lua](../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures/ClosureTUnitTests/LambdaFunctions.lua)

### Root Cause

These fixtures use NovaSharp's metalua-style lambda syntax:

```lua
-- Lambda syntax: |params|expression
g = |f, x|f(x, x+1)
f = |x, y, z|x*(y+z)
return |a| a + z
```

This syntax is **NOT part of standard Lua**. In Lua 5.3+, the `|` character is the bitwise OR operator, so reference Lua throws a syntax error when parsing these files.

Reference Lua 5.4 output:

```
lua5.4: ...LuaFixtures/ClosureTUnitTests/LambdaFunctions.lua:7: unexpected symbol near '|'
Exit code: 1
```

### Confirmation

NovaSharp's README.md documents this feature:
> "Support for metalua style anonymous functions (lambda-style)"

The implementation is in [FunctionDefinitionExpression.cs](../src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/FunctionDefinitionExpression.cs) which handles both standard function syntax and lambda syntax.

## Changes Made

### 1. Updated Corpus Extractor ([lua_corpus_extractor_v2.py](../tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py))

Added pattern detection for metalua-style lambda syntax to `NOVASHARP_SPECIFIC_PATTERNS`:

```python
# Metalua-style lambda syntax: |params|expression
(r'\|[a-zA-Z_][a-zA-Z0-9_,\s]*\|', 'metalua-style lambda syntax'),
```

### 2. Regenerated Lua Corpus

Ran corpus extraction to update fixture metadata:

- `ClosureOnParamLambda.lua`: Now marked `@novasharp-only: true`
- `LambdaFunctions.lua`: Now marked `@novasharp-only: true`

### Verification

| Check | Result |
|-------|--------|
| NovaSharp tests pass | ✅ All 10 lambda tests pass (5 versions × 2 tests) |
| All closure tests pass | ✅ 86 tests pass |
| Fixtures correctly marked | ✅ Both marked `@novasharp-only: true` |
| Pre-commit validation | ✅ Passes |

## Impact

- **NovaSharp-only count**: 433 → 435 (+2)
- **Comparable count**: 1318 → 1316 (-2)
- These fixtures will now be correctly excluded from Lua comparison tests

## Conclusion

This was a **test metadata bug**, not a production bug. NovaSharp's lambda syntax support is an intentional extension documented in the README. The fixtures now correctly indicate they are NovaSharp-only and should not be compared against reference Lua interpreters.

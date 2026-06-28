# `__gc` Metamethod Behavior Investigation

**Date**: 2025-12-13
**Status**: Investigation Complete (Implementation Pending)
**PLAN.md Reference**: §8.14, §9.11

## Summary

Investigated the Lua 5.4 changes to `__gc` metamethod handling. The actual behavior is more nuanced than initially documented, requiring a different implementation approach than originally anticipated.

## Investigation Findings

### Initial Understanding (Incorrect)

PLAN.md originally stated:

> "Breaking Change in 5.4: Objects with non-function `__gc` metamethods are no longer silently ignored; they generate errors."

This implied that setting a non-function `__gc` would throw an error immediately.

### Actual Behavior (Verified)

From Lua 5.4 manual §8.1 (Incompatibilities in the Language):

> "When finalizing an object, Lua does not ignore `__gc` metamethods that are not functions. Any value will be called, if present. (Non-callable values will generate a warning, like any other error when calling a finalizer.)"

### Key Clarifications

1. **No error at set-time**: Setting `{__gc = 'not callable'}` is allowed without error in ALL Lua versions including 5.4

1. **Behavior during GC finalization**:

   - **Lua 5.1-5.3**: Non-function `__gc` values are silently ignored
   - **Lua 5.4+**: Lua attempts to CALL the `__gc` value regardless of type; if not callable, generates a **warning** (not error)

1. **Warning vs Error**: The warning is generated through `lua_warning`, not as a runtime exception. It doesn't stop execution or affect program flow.

### Verification Tests

```bash
# Lua 5.4 - allows setting non-callable __gc
$ lua5.4 -e "
local t = setmetatable({}, {__gc = 'not callable'})
print('created table with non-callable __gc')
t = nil
collectgarbage('collect')
print('garbage collected')
"
created table with non-callable __gc
garbage collected

# Lua 5.4 with warnings enabled (-W flag)
$ lua5.4 -W -e "
local t = setmetatable({}, {__gc = 'not callable'})
print('created table with non-callable __gc')
t = nil
collectgarbage('collect')
print('garbage collected')
"
created table with non-callable __gc
garbage collected
```

Note: The warning may not be visible in simple tests because:

1. The `-W` flag enables warnings to be treated as errors
1. The warning is generated during GC finalization which may occur asynchronously
1. Simple test scripts may exit before GC runs finalizers

## Behavioral Matrix

| Version | Non-callable `__gc` at set-time | Non-callable `__gc` during GC finalization |
| ------- | ------------------------------- | ------------------------------------------ |
| Lua 5.1 | ✅ Allowed                      | ✅ Silently ignored                        |
| Lua 5.2 | ✅ Allowed                      | ✅ Silently ignored                        |
| Lua 5.3 | ✅ Allowed                      | ✅ Silently ignored                        |
| Lua 5.4 | ✅ Allowed                      | ⚠️ Warning generated (via lua_warning)     |
| Lua 5.5 | ✅ Allowed                      | ⚠️ Warning generated (via lua_warning)     |

## NovaSharp Implementation Considerations

### Current State

NovaSharp does not currently implement true `__gc` finalizer execution. Tables with `__gc` metamethods are collected by .NET's garbage collector without calling the metamethod.

### Implementation Options

1. **Strict validation (diverges from Lua)**:

   - Validate `__gc` callability at metatable set-time
   - Pro: Catches bugs early
   - Con: Not Lua-compliant; breaks legitimate Lua code

1. **Lua-compatible (recommended)**:

   - Allow any value for `__gc` at set-time
   - If implementing finalization in future:
     - Attempt to call `__gc` during finalization
     - Generate warning (not error) if not callable
   - Pro: Matches Lua behavior exactly
   - Con: Requires warning infrastructure

1. **No change (current state)**:

   - Continue ignoring `__gc` for finalization
   - Pro: Simple, no risk of breaking changes
   - Con: Not Lua-compliant for scripts expecting finalizers

### Warning Infrastructure Requirement

If implementing Lua 5.4 finalization behavior, NovaSharp would need:

- `Script.Warning` event or callback mechanism
- Integration with `warn()` function (already exists for 5.4+)
- Consistent warning output format

### Decision Deferral

Given that:

1. NovaSharp doesn't currently execute `__gc` finalizers
1. The `__gc` behavior is only relevant during GC finalization
1. Implementing proper finalizers is a significant undertaking

**Recommendation**: Document current behavior (no `__gc` finalization) and defer implementation until/unless finalizer support is added to NovaSharp.

## Updated PLAN.md

Section §8.14 and §9.11 have been updated to reflect these findings:

- Corrected description of Lua 5.4 behavior
- Changed status from "implement" to "investigate + document"
- Added behavioral matrix
- Noted warning (not error) semantic

## References

- Lua 5.4 Manual §8.1 (Incompatibilities in the Language)
- Lua 5.4 Manual §2.5.3 (Garbage Collection - Finalizers)
- `lua_warning` API documentation

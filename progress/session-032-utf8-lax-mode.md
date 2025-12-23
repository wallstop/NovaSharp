# UTF-8 Lax Mode for Lua 5.4+

**Date**: 2025-12-15
**PLAN.md Item**: §9.8 - utf8 lax mode for Lua 5.4

## Summary

Implemented Lua 5.4's optional `lax` parameter for `utf8.len`, `utf8.codepoint`, and `utf8.codes` functions. When `lax=true`, these functions accept UTF-8 sequences that encode surrogate code points (U+D800 to U+DFFF) and code points above U+10FFFF.

## Background

In Lua 5.4, the utf8 library functions gained an optional `lax` parameter that relaxes validation:

- **`utf8.len(s [, i [, j [, lax]]])`** - With `lax=true`, accepts surrogates and extended codepoints
- **`utf8.codepoint(s [, i [, j [, lax]]])`** - With `lax=true`, accepts surrogates and extended codepoints
- **`utf8.codes(s [, lax])`** - With `lax=true`, the iterator accepts surrogates and extended codepoints

From Lua 5.4 manual §6.5:

> "If the optional argument `lax` is true, then no checks are done on the validity of the sequence. When not lax, it signals an error if s is not an appropriate value..."

The lax mode specifically allows:

- Surrogate code points (0xD800-0xDFFF) - normally invalid as standalone UTF-8
- Extended code points above Unicode's maximum (0x10FFFF) up to 0x7FFFFFFF

## Changes Made

### 1. utf8.len (Utf8Module.cs)

Added lax parameter parsing for Lua 5.4+:

```csharp
// Lua 5.4+ adds an optional lax parameter (argument #4)
bool lax = false;
LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
if (version >= LuaCompatibilityVersion.Lua54)
{
    DynValue laxArg = args.AsType(3, "utf8.len", DataType.Boolean, true);
    lax = !laxArg.IsNil() && laxArg.Boolean;
}
```

The `lax` flag is passed to `CountRunesOrError()`.

### 2. utf8.codepoint (Utf8Module.cs)

Added lax parameter parsing for Lua 5.4+:

```csharp
// Lua 5.4+ adds an optional lax parameter (argument #4)
bool lax = false;
LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
if (version >= LuaCompatibilityVersion.Lua54)
{
    DynValue laxArg = args.AsType(3, "utf8.codepoint", DataType.Boolean, true);
    lax = !laxArg.IsNil() && laxArg.Boolean;
}
```

The `lax` flag is passed to `TryDecodeScalarWithinRange()`.

### 3. utf8.codes (Utf8Module.cs)

Added lax parameter parsing and separate iterator callback:

```csharp
// Cached callback for lax mode utf8.codes
private static readonly DynValue CachedCodesIteratorLaxCallback = DynValue.NewCallback(
    CodesIteratorLax
);

// In Codes():
bool lax = false;
LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
if (version >= LuaCompatibilityVersion.Lua54)
{
    DynValue laxArg = args.RawGet(1, false);
    lax = laxArg != null && laxArg.Type == DataType.Boolean && laxArg.Boolean;
}

DynValue iterator = lax
    ? CachedCodesIteratorLaxCallback
    : CachedCodesIteratorCallback;
```

Created `CodesIteratorLax()` that passes `lax=true` to `TryDecodeScalarWithinRange()` and `GetNextIteratorIndex()`.

### 4. TryDecodeScalarWithinRange (Utf8Module.cs)

Added lax parameter overload that allows lone surrogates:

```csharp
private static bool TryDecodeScalarWithinRange(
    string value,
    int index,
    int limit,
    out int codePoint,
    out int width,
    bool lax
)
{
    // ... existing validation ...
    
    if (char.IsHighSurrogate(current))
    {
        // ... surrogate pair handling ...
        
        // In lax mode, allow lone high surrogate
        if (lax)
        {
            codePoint = current;
            width = 1;
            return true;
        }
        return false;
    }

    if (char.IsLowSurrogate(current))
    {
        // In lax mode, allow lone low surrogate
        if (lax)
        {
            codePoint = current;
            width = 1;
            return true;
        }
        return false;
    }
    // ...
}
```

### 5. Helper Functions

Updated with lax parameter overloads:

- `CountRunesOrError(string value, int startIndex, int endExclusive, bool lax = false)`
- `GetNextIteratorIndex(string value, DynValue control, bool lax)`

## Tests Added

### TUnit Tests (Utf8ModuleTUnitTests.cs)

Added 7 new test methods:

1. **Utf8LenLaxModeAcceptsSurrogates** - Verifies lax mode accepts lone surrogates in utf8.len
1. **Utf8LenLaxModeIgnoredPreLua54** - Verifies lax parameter is ignored in Lua 5.3
1. **Utf8CodepointLaxModeAcceptsSurrogates** - Verifies lax mode accepts lone surrogates in utf8.codepoint
1. **Utf8CodesLaxModeAcceptsSurrogates** - Verifies lax mode accepts lone surrogates in utf8.codes
1. **Utf8CodesLaxModeIgnoredPreLua54** - Verifies lax parameter is ignored in Lua 5.3
1. **Utf8LaxModeAllowsLowSurrogates** - Verifies both high and low lone surrogates work
1. **Utf8LaxModeStillValidatesSequenceStructure** - Verifies valid surrogate pairs still work in both modes

### Lua Fixtures

Created 6 new fixtures in `LuaFixtures/Utf8ModuleTUnitTests/`:

- `Utf8LenLaxModeAcceptsSurrogates.lua` (Lua 5.4+)
- `Utf8CodepointLaxModeAcceptsSurrogates.lua` (Lua 5.4+)
- `Utf8CodesLaxModeAcceptsSurrogates.lua` (Lua 5.4+)
- `Utf8LaxModeAllowsLowSurrogates.lua` (Lua 5.4+)
- `Utf8LenLaxModeIgnoredPreLua54.lua` (Lua 5.3)
- `Utf8CodesLaxModeIgnoredPreLua54.lua` (Lua 5.3, expects-error)

## Implementation Notes

### C# String Representation

NovaSharp uses C# strings internally, which are UTF-16. This means:

1. Surrogate pairs (like emoji) are stored as two UTF-16 code units
1. Lone surrogates can exist in C# strings but are "invalid" from a Unicode perspective
1. The lax mode allows treating lone surrogates as valid single code points

### Iterator State Design

For `utf8.codes`, the lax state needed to be propagated to the iterator callback. Since the iterator is a static cached callback, we used two separate callbacks:

- `CachedCodesIteratorCallback` - strict mode (existing)
- `CachedCodesIteratorLaxCallback` - lax mode (new)

This avoids allocation while maintaining correct behavior.

## Test Results

All 5157 tests pass (7 new tests added):

```
Passed! - Failed: 0, Passed: 5157, Skipped: 0, Total: 5157
```

## References

- Lua 5.4 Reference Manual §6.5 - UTF-8 Support
- Unicode Standard - Surrogates and Supplementary Characters

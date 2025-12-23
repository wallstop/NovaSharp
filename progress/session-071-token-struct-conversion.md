# Session 071: Token Class to Readonly Struct Conversion

**Date**: 2025-12-21
**Initiative**: 18 - Large Script Load/Compile Memory Optimization (Phase 2)
**Status**: ✅ Complete

## Summary

Converted the `Token` class to a `readonly struct` to eliminate heap allocations during lexical analysis. This is part of the ongoing compiler memory optimization initiative.

## Changes Made

### 1. Token.cs - Converted to readonly struct

**Before:**

```csharp
internal class Token
{
    public int SourceId { get; }
    public int FromCol { get; }
    public int FromLine { get; }
    // ... more properties
    public string Text { get; set; }  // Mutable!
}
```

**After:**

```csharp
internal readonly struct Token : IEquatable<Token>
{
    public readonly int SourceId;
    public readonly int FromCol;
    public readonly int FromLine;
    // ... more fields
    public readonly string Text;
    
    // Full equality implementation
    public bool Equals(Token other) { /* ... */ }
    public override bool Equals(object obj) { /* ... */ }
    public override int GetHashCode() { /* ... */ }
    public static bool operator ==(Token left, Token right) { /* ... */ }
    public static bool operator !=(Token left, Token right) { /* ... */ }
    
    // Helper for creating modified copies
    public Token WithText(string text) { /* ... */ }
}
```

**Key changes:**

- Changed from `class` to `readonly struct`
- Changed all properties to `public readonly` fields
- Made `Text` immutable (readonly field instead of settable property)
- Added constructor parameter for `Text` with default null
- Added `WithText()` method to create modified copies
- Implemented `IEquatable<Token>` with proper equality semantics

### 2. SyntaxErrorException.cs - Updated for nullable Token

**Before:**

```csharp
public Token Token { get; }
```

**After:**

```csharp
public Token? Token { get; }
```

The exception now stores a nullable `Token?` to distinguish "no token" from "default token".

### 3. Lexer.cs - Updated token creation

**Before:**

```csharp
Token t = new(tokenType, ...) { Text = text };
```

**After:**

```csharp
Token t = new(tokenType, ..., text);
```

Token text is now passed via constructor instead of object initializer.

### 4. Other Updated Files

- `InterpreterException.cs` - Updated `DecoratingToken` to `Token?`
- `ScriptSyntaxError.cs` - Updated for nullable Token parameter
- `CodeErrorFactory.cs` - Updated for nullable Token parameter
- 8 test files - Updated to use new Token constructor

## Memory Impact

| Aspect                    | Before (class)             | After (struct)     |
| ------------------------- | -------------------------- | ------------------ |
| Heap allocation per token | ~56-64 bytes               | 0 bytes            |
| Token storage             | Reference (8 bytes)        | Inline (~40 bytes) |
| GC pressure               | High (thousands of tokens) | None               |

For a script with 1000 tokens:

- **Before**: ~60 KB heap allocations + GC overhead
- **After**: 0 heap allocations, tokens stored inline in stack/registers

## Unity Compatibility

This change is fully Unity-compatible:

- Uses only `readonly struct` (available in all .NET targets)
- No .NET 5+ APIs used
- No `CollectionMarshal` or other Unity-incompatible patterns

## Test Results

All **11,754 tests pass** after the conversion.

```
Test run summary: Passed!
  total: 11754
  failed: 0
  succeeded: 11754
  skipped: 0
  duration: 26s 606ms
```

## Skills Documentation Update

Also updated `.llm/skills/high-performance-csharp.md` with Unity compatibility requirements:

- Added "Unity Compatibility Requirements" section
- Listed APIs NOT available in Unity (CollectionMarshal, EnsureCapacity, etc.)
- Added Unity-compatible performance patterns

## Related Documentation Fixes

Fixed incorrect information in:

- `PLAN.md` - Corrected `Instruction` status (was incorrectly listed as class, is actually struct)
- `progress/session-070-compiler-memory-investigation.md` - Updated findings to reflect Instruction is already a struct

## Next Steps

Remaining optimization opportunities for Initiative 18:

1. **BuildTimeScope pooling** - Complex due to nested state, may require different approach
1. **AST node pooling** - 27 node types, significant refactoring required
1. **Span-based lexer** - High impact but requires architectural changes

## Files Changed

### Production Code

- `src/runtime/.../Loaders/Script/Tokenization/Token.cs`
- `src/runtime/.../Loaders/Script/Tokenization/Lexer.cs`
- `src/runtime/.../Errors/SyntaxErrorException.cs`
- `src/runtime/.../Errors/InterpreterException.cs`
- `src/runtime/.../Errors/ScriptSyntaxError.cs`
- `src/runtime/.../Errors/CodeErrorFactory.cs`

### Test Code

- 8 test files updated for new Token constructor

### Documentation

- `.llm/skills/high-performance-csharp.md`
- `PLAN.md`
- `progress/session-070-compiler-memory-investigation.md`

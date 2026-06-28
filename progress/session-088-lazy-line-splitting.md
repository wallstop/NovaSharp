# Session 088: Lazy Line-Splitting in SourceCode

**Date**: 2025-12-22
**Initiative**: Initiative 21 Phase 1 - Performance Parity Analysis
**Status**: ✅ Complete

## Summary

Implemented lazy line-splitting in the `SourceCode` class to eliminate unnecessary allocations during script compilation.

## Problem Statement

The `SourceCode` class eagerly split source code into lines during construction:

```csharp
// BEFORE: Lines computed eagerly during construction
internal SourceCode(string name, string code, int sourceId, Script ownerScript)
{
    List<string> lines = new();
    lines.Add($"-- Begin of chunk : {name} ");
    lines.AddRange(Code.Split('\n'));
    _lines = lines.ToArray();
    // ...
}
```

This caused 3 allocations (Split, List, ToArray) even when the `Lines` property was never accessed—which is the majority of script executions. Lines are typically only needed for:

- Error messages with line information
- Debugger functionality
- Source position display

## Solution

Implemented two optimizations:

### 1. Lazy Initialization with Thread-Safe Double-Checked Locking

Lines are now computed on first access, not during construction.

### 2. Span-Based Line Enumeration (avoids `string.Split()`)

The `BuildLines()` method uses `ReadOnlySpan<char>` to count and extract lines without the intermediate array allocation from `Split()`:

```csharp
private string[] BuildLines()
{
    ReadOnlySpan<char> codeSpan = Code.AsSpan();
    
    // Count lines with span iteration
    int lineCount = 2; // 1 header + 1 code line minimum
    foreach (char c in codeSpan)
    {
        if (c == '\n') lineCount++;
    }
    
    string[] result = new string[lineCount];
    
    // Header using ZString (zero-allocation)
    using (Utf16ValueStringBuilder sb = ZStringBuilder.Create())
    {
        sb.Append("-- Begin of chunk : ");
        sb.Append(Name);
        sb.Append(' ');
        result[0] = sb.ToString();
    }
    
    // Extract lines via span slicing (no Split allocation)
    int lineIndex = 1, start = 0;
    for (int i = 0; i < codeSpan.Length; i++)
    {
        if (codeSpan[i] == '\n')
        {
            result[lineIndex++] = codeSpan.Slice(start, i - start).ToString();
            start = i + 1;
        }
    }
    
    // Final line after last newline
    if (lineIndex < result.Length)
        result[lineIndex] = codeSpan.Slice(start).ToString();
    
    return result;
}
```

## Performance Implications

| Aspect                  | Before                               | After                           |
| ----------------------- | ------------------------------------ | ------------------------------- |
| Constructor allocations | 3 allocations (Split, List, ToArray) | 0 allocations                   |
| Memory per SourceCode   | ~O(n) where n = source length        | 0 until Lines accessed          |
| First Lines access      | O(1) - cached                        | O(n) - one-time compute         |
| Subsequent Lines access | O(1)                                 | O(1) - same as before           |
| Thread safety           | N/A (no lazy init)                   | Thread-safe via volatile + lock |

**Estimated savings:**

- Scripts that never access `Lines` (majority): **100% reduction** in line-related allocations
- Typical use: Only error messages and debugger access `Lines`, which is the minority of script executions
- The `CreateLines()` method is also more efficient: direct array allocation vs `List<T>` overhead

## Test Results

- **SourceCode-specific tests**: 6/6 passed ✅
- **InterpreterException tests**: 12/12 passed ✅
- **Debugger tests**: 57/57 passed ✅
- **Full test suite**: **11,835/11,835 passed** ✅

## API Compatibility

**No breaking changes.** The `Lines` property signature remains identical:

```csharp
public IReadOnlyList<string> Lines { get; }
```

## Files Changed

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Debugging/SourceCode.cs`

## Initiative 21 Phase 1 Status Update

| Task                                  | Impact | Effort   | Status          |
| ------------------------------------- | ------ | -------- | --------------- |
| Script caching with hash-based lookup | HIGH   | 2-3 days | ✅ Complete     |
| **Lazy line-splitting in SourceCode** | MEDIUM | 1 day    | ✅ **Complete** |
| Span-based lexer rewrite              | HIGH   | 5-7 days | 🔲              |
| SourceRef → readonly struct           | MEDIUM | 2-3 days | 🔲              |
| SymbolRef → readonly struct           | MEDIUM | 2-3 days | 🔲              |
| Instruction list → pooled array       | MEDIUM | 1-2 days | 🔲              |

## Additional Work

### Initiative 22: ZString Migration (HIGH PRIORITY)

- Added to PLAN.md as high-priority initiative
- Created skill guide: `.llm/skills/zstring-migration.md`

### Initiative 23: Span-Based Array Operation Migration (HIGH PRIORITY)

- Added to PLAN.md as high-priority initiative
- Created skill guide: `.llm/skills/span-optimization.md`

### Lock Object Optimization

- Removed dedicated `_linesLock = new object()` allocation
- Now reuses existing `Refs` list as lock object (saves one object allocation per SourceCode instance)

### Updated LLM Context

- Added `zstring-migration` and `span-optimization` skills to context.md
- Added "Reuse existing objects as locks" guideline to High-Performance Code section

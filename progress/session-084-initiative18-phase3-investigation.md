# Session 084: Initiative 18 Phase 3 Investigation

**Date**: 2025-12-22
**Initiative**: 18 - Large Script Load/Compile Memory Optimization
**Phase**: 3 - AST Node Pooling / Span-Based Lexer Investigation
**Status**: ✅ Investigation Complete — AST pooling deferred (not recommended)

## Summary

Completed a comprehensive investigation of Phase 3 tasks for Initiative 18. The investigation revealed that AST node pooling is **not recommended** due to lifecycle complexity and diminishing returns, while span-based lexer improvements have limited remaining opportunity since key optimizations are already in place.

## Investigation Findings

### AST Node Pooling Analysis

**27 AST node types examined** (11 expressions + 16 statements):

| Category    | High-Frequency Types                                                                     |
| ----------- | ---------------------------------------------------------------------------------------- |
| Expressions | `SymbolRefExpression`, `LiteralExpression`, `BinaryExpression`, `FunctionCallExpression` |
| Statements  | `AssignmentStatement`, `ReturnStatement`, `FunctionDefinitionStatement`                  |

**Pooling Assessment: ❌ NOT RECOMMENDED**

**Reasons:**

1. **Lifecycle Complexity** — Nodes have complex lifecycles (parse → AST → compile → discard) with unclear ownership boundaries
1. **Nested References** — Parent nodes hold child references; returning parent while children are in use would corrupt the pool
1. **Reset Complexity** — 27 different types with varying field counts would require custom reset logic for each
1. **Type Heterogeneity** — Would need 27 separate pools or a complex generic approach with boxing
1. **Bug Risk** — High probability of use-after-return bugs in complex tree structures

**Estimated Effort if Attempted**: 6+ weeks\
**Expected Benefit**: Diminishing returns — major wins already achieved in Phases 1-2

### Span-Based Lexer Assessment

**Current State:**

- ✅ Lexer already uses ZString (`ZStringBuilder`) — most impactful optimization already done
- ✅ `Token` is already a `readonly struct` (Phase 2 complete)
- ⚠️ `Token.Text` is `string`, limiting full span conversion without breaking API

**Possible Incremental Optimization:**

- **Span-based keyword interning** in `Lexer.cs` could avoid string allocation for keywords (~15-30% of tokens)
- Effort: ~2 days
- Impact: Medium (10-20 MB savings on large scripts)

### Completed Phases Recap

**Phase 1 (Complete):**

- ✅ ListPool integration in parser (`Expression.cs`, `FunctionDefinitionExpression.cs`, `ForEachLoopStatement.cs`)
- ✅ Pooled `BlocksToClose` inner lists in `ProcessorInstructionLoop.cs`

**Phase 2 (Complete):**

- ✅ `Token` converted to `readonly struct` with `IEquatable<Token>`
- ✅ `Instruction` confirmed as already a struct (no conversion needed)

## Prioritized Recommendations

| Priority | Recommendation                                   | Effort | Risk              | Status             |
| -------- | ------------------------------------------------ | ------ | ----------------- | ------------------ |
| **P1**   | ❌ Do NOT pool AST nodes                         | N/A    | High if attempted | Deferred           |
| **P2**   | Consider span-based keyword interning            | 2 days | Low               | Future opportunity |
| **P3**   | Consider `SymbolRefExpression` struct conversion | 1 week | Medium            | Future opportunity |

## Conclusion

**Initiative 18 has achieved its primary goals:**

The completed phases (1-2) already captured the major wins:

- Token → `readonly struct` eliminated most lexer allocations
- ListPool integration reduced parser allocations
- Instruction is already a struct (bytecode is efficient)

The remaining AST node allocations represent **diminishing returns** — the effort required (6+ weeks for full pooling) far exceeds the expected benefit given the lifecycle complexity and bug risk.

**Recommendation**: Mark Initiative 18 as **MOSTLY COMPLETE** with Phase 3 items explicitly deferred as "future opportunities" rather than active work items.

## Files Examined

- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Expressions/*.cs` (11 expression types)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Statements/*.cs` (16 statement types)
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Lexer/Lexer.cs`
- `src/runtime/WallstopStudios.NovaSharp.Interpreter/Lexer/Token.cs`

## Related Progress Sessions

- [session-070-compiler-memory-investigation.md](session-070-compiler-memory-investigation.md) - Initial investigation
- [session-071-token-struct-conversion.md](session-071-token-struct-conversion.md) - Phase 2 Token conversion

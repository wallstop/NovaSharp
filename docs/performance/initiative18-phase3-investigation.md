# Initiative 18: Large Script Load/Compile Memory Optimization

## Phase 3 Investigation Report

> **Date**: December 22, 2025\
> **Status**: 📋 INVESTIGATION COMPLETE\
> **Related**: [PLAN.md Initiative 18](../../PLAN.md), [Session 070 Investigation](../../progress/session-070-compiler-memory-investigation.md)

______________________________________________________________________

## Table of Contents

1. [Executive Summary](#1-executive-summary)
1. [AST Node Types Analysis](#2-ast-node-types-analysis)
1. [Lexer Span-Based Conversion Assessment](#3-lexer-span-based-conversion-assessment)
1. [Prioritized Recommendations](#4-prioritized-recommendations)
1. [Technical Blockers & Risks](#5-technical-blockers--risks)
1. [Effort Estimates](#6-effort-estimates)
1. [Conclusions](#7-conclusions)

______________________________________________________________________

## 1. Executive Summary

This investigation analyzes the feasibility and impact of two Phase 3 optimization targets for Initiative 18:

1. **AST Node Pooling** — 27 AST node types (11 expressions + 16 statements) currently allocate without pooling
1. **Span-Based Lexer Prototype** — Investigate replacing string-based lexing with spans

### Key Findings

| Area             | Feasibility     | Impact        | Recommendation                                   |
| ---------------- | --------------- | ------------- | ------------------------------------------------ |
| AST Node Pooling | ⚠️ **Complex**  | 🔴 **High**   | Defer — lifecycle complexity outweighs benefits  |
| Span-Based Lexer | ⚠️ **Moderate** | 🟡 **Medium** | Incremental approach — target specific hot spots |

### Why Not Recommended for Full Implementation

1. **AST nodes have complex lifecycles** — Nodes are created during parsing, stored in the AST, compiled to bytecode, then discarded. Pooling requires careful lifecycle management that could introduce subtle bugs.

1. **Nodes are heterogeneous** — 27 different types with varying field counts make a unified pooling strategy complex.

1. **Lexer already uses ZString** — The current lexer already leverages `Utf16ValueStringBuilder` from ZString for zero-allocation string building, covering the most impactful allocation sites.

1. **Completed phases already achieved significant wins** — Token → `readonly struct`, ListPool integration, and static delegates addressed the highest-impact items.

______________________________________________________________________

## 2. AST Node Types Analysis

### 2.1 Expression Types (11 classes)

| Type                           | Fields                                      | Allocation Pattern | Pooling Feasibility | Notes                                                 |
| ------------------------------ | ------------------------------------------- | ------------------ | ------------------- | ----------------------------------------------------- |
| `LiteralExpression`            | 1 (`DynValue`)                              | Very frequent      | ⚠️ Low              | Extremely common, but simple — struct would be better |
| `SymbolRefExpression`          | 2 (`SymbolRef`, `string`)                   | Very frequent      | ⚠️ Low              | Core identifier lookup                                |
| `BinaryOperatorExpression`     | 3 (`Expression×2`, `Operator`)              | Very frequent      | ⚠️ Low              | Complex chain-building pattern with `LinkedList`      |
| `UnaryOperatorExpression`      | 2 (`Expression`, `string`)                  | Moderate           | ⚠️ Low              | Simple wrapper                                        |
| `IndexExpression`              | 3 (`Expression×2`, `string`)                | Frequent           | ⚠️ Low              | Table access                                          |
| `FunctionCallExpression`       | 4+ (`Expression`, `List<Expression>`, etc.) | Frequent           | ❌ None             | Contains `List<Expression>` - complex                 |
| `FunctionDefinitionExpression` | 8+ (many fields)                            | Moderate           | ❌ None             | Extremely complex state                               |
| `TableConstructor`             | 2 (`List<Expression>`, `List<KVP>`)         | Moderate           | ❌ None             | Contains mutable lists                                |
| `ExprListExpression`           | 1 (`List<Expression>`)                      | Frequent           | ⚠️ Low              | Wrapper for list                                      |
| `AdjustmentExpression`         | 1 (`Expression`)                            | Moderate           | ⚠️ Low              | Simple wrapper                                        |
| `DynamicExprExpression`        | 1 (`Expression`)                            | Rare               | ❌ None             | REPL-only                                             |

### 2.2 Statement Types (16 classes)

| Type                          | Fields                                                     | Allocation Pattern | Pooling Feasibility | Notes                      |
| ----------------------------- | ---------------------------------------------------------- | ------------------ | ------------------- | -------------------------- |
| `CompositeStatement`          | 1 (`List<Statement>`)                                      | Very frequent      | ⚠️ Low              | Every block creates one    |
| `AssignmentStatement`         | 3 (`List<IVariable>×1`, `List<Expression>×1`, `SourceRef`) | Very frequent      | ❌ None             | Contains mutable lists     |
| `IfStatement`                 | 3 (`List<IfBlock>`, `IfBlock?`, `SourceRef`)               | Frequent           | ❌ None             | Nested structure           |
| `WhileStatement`              | 4                                                          | Moderate           | ⚠️ Low              | Standard loop              |
| `ForLoopStatement`            | 6                                                          | Moderate           | ⚠️ Low              | Numeric for                |
| `ForEachLoopStatement`        | 6+                                                         | Moderate           | ❌ None             | Contains arrays            |
| `RepeatStatement`             | 4                                                          | Less common        | ⚠️ Low              | Standard loop              |
| `ReturnStatement`             | 2                                                          | Frequent           | ⚠️ Low              | Simple                     |
| `BreakStatement`              | 1                                                          | Less common        | ✅ High             | Minimal state              |
| `GotoStatement`               | 5+                                                         | Rare               | ❌ None             | Complex state for patching |
| `LabelStatement`              | 6+                                                         | Rare               | ❌ None             | Complex state for patching |
| `FunctionDefinitionStatement` | 8+                                                         | Moderate           | ❌ None             | Extremely complex          |
| `FunctionCallStatement`       | 1                                                          | Frequent           | ⚠️ Low              | Wrapper                    |
| `ScopeBlockStatement`         | 3                                                          | Moderate           | ⚠️ Low              | `do...end`                 |
| `ChunkStatement`              | 4                                                          | Once per script    | ❌ None             | Root node                  |
| `EmptyStatement`              | 0                                                          | Rare               | ✅ High             | No-op, trivial             |

### 2.3 Frequency Analysis (Estimated)

Based on typical Lua code patterns:

```
Most Allocated (per 1000 lines of Lua):
1. LiteralExpression        ~2000-5000 instances
2. SymbolRefExpression      ~1500-3000 instances
3. BinaryOperatorExpression ~1000-2000 instances
4. CompositeStatement       ~500-1000 instances
5. AssignmentStatement      ~500-1000 instances
6. FunctionCallExpression   ~300-800 instances
7. IndexExpression          ~300-600 instances
```

### 2.4 Pooling Complexity Assessment

**Why pooling AST nodes is problematic:**

1. **Ownership Transfer**: Nodes are created by the parser, transferred to the AST, then passed to the compiler. Clear ownership is essential for pooling but blurs during compilation.

1. **Nested References**: `BinaryOperatorExpression` holds references to child `Expression` objects. Returning a parent to the pool while children are still in use would corrupt the pool.

1. **Reset Complexity**: Each node type requires custom reset logic. Missing a field causes memory leaks or corruption.

1. **Thread Safety**: While parsing is single-threaded, multi-script compilation could require thread-local pools, adding complexity.

1. **Immutability Pattern**: Many nodes are effectively immutable after construction — pooling mutable objects then pretending they're immutable is error-prone.

### 2.5 Alternative: Struct-Based Expressions

For the most frequent types (`LiteralExpression`, `SymbolRefExpression`), a struct-based approach could work:

```csharp
// Hypothetical - NOT recommended due to breaking changes
internal readonly struct LiteralExpressionData
{
    public readonly DynValue Value;
    public readonly int SourceId;  // Minimal tracking
    
    public LiteralExpressionData(DynValue value, int sourceId)
    {
        Value = value;
        SourceId = sourceId;
    }
}
```

**Problems:**

- Breaks inheritance hierarchy (`Expression` base class)
- Requires major refactoring of parser/compiler
- `DynValue` is already a struct, so the gain is minimal

______________________________________________________________________

## 3. Lexer Span-Based Conversion Assessment

### 3.1 Current Lexer Architecture

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Lexer/Lexer.cs` (786 lines)

**Current Design:**

- Character-by-character cursor over `string _code`
- Uses `Utf16ValueStringBuilder` (ZString) for token text accumulation
- Creates `Token` instances with `string Text` property

**Already Optimized:**

- ✅ `Token` is `readonly struct` (Phase 2 complete)
- ✅ String building uses ZString (`Utf16ValueStringBuilder`)
- ✅ No `StringBuilder` allocations

### 3.2 Span-Based Opportunities

| Location                             | Current                | Span Opportunity                     | Complexity | Impact |
| ------------------------------------ | ---------------------- | ------------------------------------ | ---------- | ------ |
| `ReadNameToken()` (L769-783)         | ZString → `ToString()` | `ReadOnlySpan<char>` → string intern | Low        | Medium |
| `ReadNumberToken()` (L482-556)       | ZString → `ToString()` | `ReadOnlySpan<char>` slice           | Low        | Medium |
| `ReadSimpleStringToken()` (L634-702) | ZString with escapes   | Complex — escapes require building   | High       | Low    |
| `ReadLongString()` (L400-479)        | ZString loop           | Span slicing for raw content         | Medium     | Low    |
| `ReadComment()` (L600-632)           | ZString accumulation   | Span slicing                         | Low        | Low    |

### 3.3 Key Observation: String Interning Already Exists

**File**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/DataStructs/LuaStringPool.cs`

The codebase already has string interning for keywords/metamethods:

```csharp
internal static class LuaStringPool
{
    // Metamethods
    public static class Metamethods
    {
        public const string ADD = "__add";
        public const string SUB = "__sub";
        // ...
    }
}
```

And `Token.GetReservedTokenType()` uses a dictionary to map keyword strings.

### 3.4 Feasible Incremental Changes

**Option A: Identifier/Keyword Span Lookup**

The lexer could use `ReadOnlySpan<char>` for keyword detection before allocating a string:

```csharp
// Current (allocates string first)
private string ReadNameToken()
{
    using Utf16ValueStringBuilder name = ZStringBuilder.CreateNested();
    // ... accumulate ...
    return name.ToString();  // ALLOCATION
}

// Proposed (span-first for keywords)
private Token ReadNameToken(int fromLine, int fromCol)
{
    int start = _cursor;
    while (CursorNotEof() && IsNameChar(CursorChar()))
        CursorNext();
    
    ReadOnlySpan<char> span = _code.AsSpan(start, _cursor - start);
    
    // Check for reserved words without allocating
    if (TryGetReservedTokenType(span, out TokenType reserved))
    {
        return CreateToken(reserved, fromLine, fromCol, GetInternedKeyword(span));
    }
    
    // Only allocate for non-keyword identifiers
    return CreateToken(TokenType.Name, fromLine, fromCol, span.ToString());
}
```

**Estimated Savings:**

- Keywords (`if`, `then`, `end`, `function`, etc.) are ~15-30% of all tokens
- Avoids ~24-48 bytes per keyword token (string + allocation overhead)

**Complexity:** Low-Medium (requires span-based keyword dictionary)

**Option B: Number Token Span Parsing**

Numbers could be parsed directly from span without intermediate string:

```csharp
// Current flow: chars → ZString → string → Parse
// Proposed: chars → span → Parse (skip string allocation for valid numbers)
```

However, `Token.Text` is still needed for error messages, limiting the benefit.

### 3.5 Blocking Issues

1. **Token.Text is `string`**: The `Token` struct stores `string Text`. Changing to `ReadOnlyMemory<char>` would:

   - Break API compatibility
   - Require source string to outlive tokens (lifetime complexity)
   - Need nullable handling for tokens without text

1. **Escape Sequence Processing**: String literals with escapes (`\n`, `\t`, `\x00`, etc.) cannot use raw spans — they must build the unescaped result.

1. **Unicode Handling**: Current code uses `char.IsLetter()`, `char.IsWhiteSpace()` which work on `char`. Span versions exist but need migration.

1. **Error Messages**: Many error paths use `Token.Text` in exception messages — these would need to allocate anyway.

______________________________________________________________________

## 4. Prioritized Recommendations

### 4.1 Priority Matrix

| Priority | Recommendation                                      | Effort   | Impact     | Risk              |
| -------- | --------------------------------------------------- | -------- | ---------- | ----------------- |
| **P1**   | ❌ **Do NOT pool AST nodes**                        | N/A      | N/A        | High if attempted |
| **P2**   | ✅ Span-based keyword interning in lexer            | 2-3 days | Medium     | Low               |
| **P3**   | ⚠️ Consider `SymbolRefExpression` struct conversion | 1 week   | Medium     | Medium            |
| **P4**   | ⚠️ Investigate `List<Expression>` pooling in parser | 3-5 days | Low-Medium | Medium            |

### 4.2 Detailed Recommendations

#### P1: Do NOT Implement AST Node Pooling

**Rationale:**

- Lifecycle management is complex and error-prone
- Nodes are short-lived (parse → compile → discard) making pooling overhead significant
- The codebase already achieved major wins with Token → struct conversion
- Risk of introducing subtle memory corruption bugs is high

**Alternative Value:**
The ~3.4 GB allocation for large scripts is dominated by:

1. Source string copies
1. Token creation (✅ now struct)
1. Bytecode generation (Instruction is already struct)
1. String intern tables

AST nodes are a small fraction once tokens are structs.

#### P2: Implement Span-Based Keyword Interning

**What:** Modify `Lexer.ReadNameToken()` to use span-based keyword lookup before string allocation.

**Benefit:** Eliminates ~15-30% of name token string allocations (keywords use pre-interned strings).

**Implementation Steps:**

1. Add `ReadOnlySpan<char>`-based keyword dictionary
1. Modify `ReadNameToken()` to check span against keywords first
1. Use `GetInternedKeyword()` for matches, `span.ToString()` otherwise

#### P3: Consider SymbolRefExpression Struct (Future)

**What:** Convert `SymbolRefExpression` to a struct implementing `IExpression` interface.

**Challenges:**

- Requires interface-based polymorphism (boxing risk)
- Would need to change `Expression` base class design
- Benefits are marginal given Token is already a struct

**Recommendation:** Defer to a future major refactoring initiative.

#### P4: List<Expression> Pooling in Parser

**What:** The parser already uses `ListPool<Expression>` but copies results to new lists:

```csharp
// Current (Expression.cs line 54)
using (ListPool<Expression>.Get(out List<Expression> exps))
{
    // ... populate ...
    return new List<Expression>(exps);  // COPIES to new list
}
```

**Improvement:** For nodes that don't modify their expression list after construction, could store the pooled list directly and return on dispose.

**Challenges:**

- Need to track which nodes "own" pooled lists
- Risk of use-after-return bugs

**Recommendation:** Defer — the current copy approach is safer.

______________________________________________________________________

## 5. Technical Blockers & Risks

### 5.1 AST Node Pooling Blockers

| Blocker                  | Description                                                      | Mitigation                                                    |
| ------------------------ | ---------------------------------------------------------------- | ------------------------------------------------------------- |
| **Lifecycle Complexity** | Nodes created during parse, used during compile, discarded after | No good mitigation — fundamental design issue                 |
| **Nested References**    | Parent nodes hold child node references                          | Would need reference counting or ownership transfer semantics |
| **Type Heterogeneity**   | 27 different node types with varying fields                      | Would need 27 different pools or generic approach             |
| **Reset Correctness**    | Every field must be reset on return                              | High bug risk, requires extensive testing                     |

### 5.2 Span-Based Lexer Risks

| Risk                      | Description                                          | Mitigation                                        |
| ------------------------- | ---------------------------------------------------- | ------------------------------------------------- |
| **API Compatibility**     | `Token.Text` is `string`, not `ReadOnlyMemory<char>` | Keep `Token.Text` as string, use spans internally |
| **Escape Processing**     | String escapes require building new content          | Accept — only raw spans benefit from optimization |
| **Error Message Quality** | Error messages use `Token.Text`                      | Accept — errors allocate anyway                   |

______________________________________________________________________

## 6. Effort Estimates

### 6.1 Span-Based Keyword Interning (Recommended)

| Task                                 | Effort      | Notes                                            |
| ------------------------------------ | ----------- | ------------------------------------------------ |
| Create span-based keyword dictionary | 4 hours     | Based on existing `Token.GetReservedTokenType()` |
| Modify `ReadNameToken()`             | 2 hours     | Span extraction, dictionary lookup               |
| Add interned keyword strings         | 1 hour      | Pre-allocated constants                          |
| Unit tests                           | 4 hours     | Edge cases, Unicode identifiers                  |
| Benchmark validation                 | 2 hours     | Memory profiler verification                     |
| **Total**                            | **~2 days** | Low risk, moderate reward                        |

### 6.2 Full AST Node Pooling (NOT Recommended)

| Task                          | Effort       | Notes                        |
| ----------------------------- | ------------ | ---------------------------- |
| Design pooling infrastructure | 1 week       | Per-type pools, reset logic  |
| Implement for all 27 types    | 2 weeks      | ~1 day per complex type      |
| Lifecycle management          | 1 week       | Ownership tracking           |
| Testing and debugging         | 2 weeks      | Memory corruption edge cases |
| **Total**                     | **~6 weeks** | High risk, uncertain reward  |

### 6.3 Comparative ROI

| Optimization               | Effort  | Expected Savings           | ROI       |
| -------------------------- | ------- | -------------------------- | --------- |
| Span-based keywords        | 2 days  | 10-20 MB for large scripts | ✅ High   |
| Full AST pooling           | 6 weeks | 50-100 MB (estimated)      | ❌ Low    |
| SymbolRefExpression struct | 1 week  | 20-40 MB (estimated)       | ⚠️ Medium |

______________________________________________________________________

## 7. Conclusions

### 7.1 Summary

**Phase 3 Investigation finds that the remaining optimization opportunities have diminishing returns:**

1. **AST Node Pooling**: NOT RECOMMENDED

   - High implementation complexity and risk
   - Moderate expected savings
   - Would require fundamental architecture changes

1. **Span-Based Lexer**: PARTIALLY RECOMMENDED

   - Keyword interning via spans is low-risk and worthwhile (P2)
   - Full span-based token text is blocked by `Token.Text` API

1. **Completed Phases Already Achieved Major Wins:**

   - Token → `readonly struct` eliminates most lexer allocations
   - ListPool integration reduces parser allocations
   - Instruction is already a struct (bytecode is efficient)

### 7.2 Recommended Next Steps

| Priority | Action                                      | Effort   | Timeline |
| -------- | ------------------------------------------- | -------- | -------- |
| **1**    | Implement span-based keyword interning      | 2 days   | Week 1   |
| **2**    | Run allocation benchmarks for large scripts | 0.5 days | Week 1   |
| **3**    | Document remaining opportunities for future | 0.5 days | Week 1   |
| **4**    | Mark Initiative 18 Phase 3 as complete      | —        | Week 1   |

### 7.3 When to Revisit

Consider revisiting AST node optimization if:

- A major version allows breaking API changes
- .NET provides better struct-polymorphism patterns (shapes/extensions)
- Benchmarks show AST nodes becoming the dominant allocation source

______________________________________________________________________

## Appendix A: Node Type Reference

### Expression Types (11)

1. `AdjustmentExpression` — Scalar adjustment wrapper
1. `BinaryOperatorExpression` — Binary operations
1. `DynamicExprExpression` — REPL dynamic expressions
1. `ExprListExpression` — Expression list wrapper
1. `FunctionCallExpression` — Function invocations
1. `FunctionDefinitionExpression` — Lambda/function literals
1. `IndexExpression` — Table indexing
1. `LiteralExpression` — Constants
1. `SymbolRefExpression` — Variable references
1. `TableConstructor` — Table literals
1. `UnaryOperatorExpression` — Unary operations

### Statement Types (16)

1. `AssignmentStatement` — Local/global assignments
1. `BreakStatement` — Loop break
1. `ChunkStatement` — Script root
1. `CompositeStatement` — Statement blocks
1. `EmptyStatement` — No-op (`;`)
1. `ForEachLoopStatement` — Generic for
1. `ForLoopStatement` — Numeric for
1. `FunctionCallStatement` — Call as statement
1. `FunctionDefinitionStatement` — Named function
1. `GotoStatement` — Jump to label
1. `IfStatement` — Conditional
1. `LabelStatement` — Jump target
1. `RepeatStatement` — repeat...until
1. `ReturnStatement` — Return values
1. `ScopeBlockStatement` — do...end
1. `WhileStatement` — while loop

______________________________________________________________________

## Related Documents

- [PLAN.md Initiative 18](../../PLAN.md)
- Session 070: Compiler Memory Investigation (local progress)
- [Initiative 12: Allocation Analysis](allocation-analysis-initiative12-phase5-validation.md)
- [Optimization Opportunities](optimization-opportunities.md)

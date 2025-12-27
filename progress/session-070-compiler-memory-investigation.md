# Session 070: Large Script Compilation Memory Investigation + Phase 1 Implementation

**Date**: 2025-12-21
**Initiative**: 18 - Large Script Load/Compile Memory Optimization
**Status**: ✅ Investigation Complete + Phase 1 Implemented

## Summary

Investigated the NovaSharp compilation pipeline to identify allocation hotspots responsible for high memory usage during script loading and compilation. The pipeline consists of four stages: **Lexer → Parser → AST → Compiler (Bytecode)**. Key findings reveal significant optimization opportunities through pooling, struct conversion, and span-based APIs.

**Phase 1 Implementation** completed: Integrated `ListPool<T>` into the parser and compilation pipeline.

## Changes Made (Phase 1)

## Compilation Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Script Loading Entry Points                          │
│  Script.LoadString() / Script.LoadFile() / Script.LoadStream()              │
│  [Script.cs#L323-L410]                                                      │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           LoaderFast.LoadChunk()                            │
│  Creates ScriptLoadingContext with Lexer and BuildTimeScope                 │
│  [LoaderFast.cs#L76-L111]                                                   │
└────────────────────────────────────┬────────────────────────────────────────┘
                                     │
        ┌────────────────────────────┴────────────────────────────┐
        │                                                          │
        ▼                                                          ▼
┌───────────────────────┐                              ┌───────────────────────┐
│       LEXER           │                              │       PARSER          │
│  Tokenizes source     │  ─── tokens ───────────────► │  Recursive descent    │
│  [Lexer.cs]           │                              │  [Statement.cs,       │
│  [Token.cs]           │                              │   Expression.cs]      │
└───────────────────────┘                              └───────────┬───────────┘
                                                                   │
                                                                   ▼
                                                       ┌───────────────────────┐
                                                       │         AST           │
                                                       │  11 Expression types  │
                                                       │  16 Statement types   │
                                                       │  [Tree/Expressions/,  │
                                                       │   Tree/Statements/]   │
                                                       └───────────┬───────────┘
                                                                   │
                                                                   ▼
                                                       ┌───────────────────────┐
                                                       │      COMPILER         │
                                                       │  node.Compile(bc)     │
                                                       │  [ByteCode.cs]        │
                                                       │  [Instruction.cs]     │
                                                       └───────────────────────┘
```

## Key Entry Points

| Method                   | File                  | Description                 |
| ------------------------ | --------------------- | --------------------------- |
| `Script.LoadString()`    | Script.cs#L360-398    | Main string-based loading   |
| `Script.LoadStream()`    | Script.cs#L407-459    | Stream-based loading        |
| `LoaderFast.LoadChunk()` | LoaderFast.cs#L76-111 | Core pipeline orchestration |

## Allocation Hotspots Identified

### Lexer Stage

| Location            | Type              | Issue                          |
| ------------------- | ----------------- | ------------------------------ |
| `Lexer.RemoveBom()` | `new string()`    | BOM removal creates new string |
| `new Token()`       | Class allocation  | Every token is a heap object   |
| `CreateToken()`     | String operations | Single-char operator strings   |
| `ReadNameToken()`   | `Substring()`     | Name token text extraction     |

**Positive**: ZString (`ZStringBuilder`) already used for building strings.

### Parser Stage

| Location                    | Type              | Issue                        |
| --------------------------- | ----------------- | ---------------------------- |
| Every `new XxxExpression()` | Class allocation  | **No pooling** for AST nodes |
| Every `new XxxStatement()`  | Class allocation  | **No pooling** for AST nodes |
| `new List<Expression>()`    | `List` allocation | Power chain, arguments, etc. |
| `new List<Statement>()`     | `List` allocation | Block statement children     |
| `new List<SymbolRef>()`     | `List` allocation | Closure capture list         |

### Compiler/Bytecode Stage

| Location                  | Type              | Issue                                  |
| ------------------------- | ----------------- | -------------------------------------- |
| `new List<Instruction>()` | `List` allocation | Instruction buffer                     |
| `new Instruction()`       | Class allocation  | **Every instruction is a heap object** |
| `sourceRefStack.Push()`   | Stack allocations | Source reference tracking              |
| `closedVars.Where()`      | LINQ allocation   | Closure symbol filtering               |

### Critical Finding: Instruction is Already a Struct ✅

The `Instruction` type (Instruction.cs) is an **`internal struct`** — NOT a class as initially reported. Each instruction is stored by value in `List<Instruction>`, which means the list holds structs directly (no per-instruction heap allocation). This is already optimal for memory!

**Verification**:

```csharp
// From Instruction.cs line 14:
internal struct Instruction
{
    // ...
}
```

The original investigation incorrectly identified this as a class. No conversion work is needed.

## Current Pooling Infrastructure (NOT Used in Compilation)

| Pool                 | Used in Compilation?        |
| -------------------- | --------------------------- |
| `DynValueArrayPool`  | ❌ No (runtime only)        |
| `ObjectArrayPool`    | ❌ No (runtime only)        |
| `SystemArrayPool<T>` | ❌ No                       |
| `ListPool<T>`        | ✅ **Yes** (Parser Phase 1) |
| `ZStringBuilder`     | ✅ **Yes** (Lexer only)     |

**Update (2025-12-21)**: Investigation revealed `Instruction` has ALWAYS been a struct. The initial report was incorrect — no conversion needed.

## Prioritized Optimization Targets

### P1: Low-Hanging Fruit (High Impact, Low Risk)

| Target                   | Impact | Effort | Description                                         |
| ------------------------ | ------ | ------ | --------------------------------------------------- |
| `ListPool<T>` in parser  | HIGH   | LOW    | ✅ DONE - Replace `new List<X>()` with pooled lists |
| Pool `Token` instances   | HIGH   | MEDIUM | Tokens are short-lived, ideal for pooling           |
| Pre-size `ByteCode` list | LOW    | LOW    | Avoid List resizing allocations                     |
| Intern common strings    | LOW    | LOW    | "+", "-", operator tokens                           |

### P2: Medium Effort (High Impact, Moderate Complexity)

| Target                              | Impact   | Effort     | Description                   |
| ----------------------------------- | -------- | ---------- | ----------------------------- |
| ~~Convert `Instruction` to struct~~ | ~~HIGH~~ | ~~MEDIUM~~ | ✅ ALREADY A STRUCT           |
| Pool `BuildTimeScope`               | MEDIUM   | MEDIUM     | Scope instances per function  |
| Pool binary operator chain nodes    | MEDIUM   | LOW        | Common pattern in expressions |

### P3: Deep Refactors (Very High Impact, High Complexity)

| Target           | Impact    | Effort    | Description                              |
| ---------------- | --------- | --------- | ---------------------------------------- |
| AST Node Pooling | VERY HIGH | HIGH      | Pool all 27 node types                   |
| Span-based Lexer | HIGH      | VERY HIGH | Replace string with `ReadOnlySpan<char>` |
| Streaming Parser | MEDIUM    | VERY HIGH | Architecture change for bounded memory   |

## Recommended Next Steps

### Phase 1: Quick Wins (Week 1)

1. **Run baseline benchmark** with `[MemoryDiagnoser]` on Large script complexity
1. **Integrate `ListPool<T>`** into parser for `List<Expression>` and `List<Statement>` fields
1. **Create Token pool** for short-lived lexer tokens

### Phase 2: Struct Conversions (Weeks 2-3)

4. **Evaluate `Instruction` as struct** - Profile memory impact, handle boxing concerns
1. **Profile with dotMemory/PerfView** to verify top allocation sources

### Phase 3: AST Optimization (Weeks 4-6)

6. **Design AST node pools** with `IPoolable` pattern
1. **Prototype span-based lexer** with `ReadOnlyMemory<char>`

## Risk Assessment

| Optimization          | Risk        | Concerns                              |
| --------------------- | ----------- | ------------------------------------- |
| ListPool integration  | LOW         | Straightforward swap                  |
| Token pooling         | LOW-MEDIUM  | Token lifetime management             |
| Instruction as struct | MEDIUM      | VM performance, boxing                |
| AST pooling           | MEDIUM-HIGH | Complex lifetime in recursive descent |
| Span-based lexer      | HIGH        | `ref struct` constraints              |

## Key Files Reference

| File                                   | Role                  |
| -------------------------------------- | --------------------- |
| `Loaders/Script/Tokenization/Lexer.cs` | Tokenization          |
| `Loaders/Script/Tokenization/Token.cs` | Token representation  |
| `Tree/Expression.cs`                   | Expression parsing    |
| `Tree/Statement.cs`                    | Statement parsing     |
| `Execution/ByteCode.cs`                | Instruction emission  |
| `Execution/Instruction.cs`             | Bytecode instructions |

## Conclusion

The investigation identified significant optimization opportunities in the compilation pipeline. The most impactful changes are:

1. **ListPool integration** in parser (easy win) - ✅ **DONE**
1. **Token pooling** (medium effort, high impact) - Planned for Phase 2
1. **Instruction struct conversion** (medium effort, high impact) - Planned for Phase 2

Phase 1 implementation pooled 10+ allocation sites in the parser and VM:

- `Expression.ExprListAfterFirstExpr()` - temporary expression list
- `Expression.ExprList()` - temporary expression list
- `Expression.SubExpr()` - power chain list
- `FunctionDefinitionExpression.BuildParamList()` - parameter names
- `ForEachLoopStatement` - iterator variable names
- `ProcessorInstructionLoop.ExecEnter()` - BlocksToClose inner lists

All **11,754 tests pass** after the changes.

# Naming Audit (Nov 2025)

NovaSharp inherited a mix of naming conventions from MoonSharp (e.g., Hungarian-style member prefixes, `Emit_*` helpers). This audit captures the current hotspots so we can plan the final alignment work noted in PLAN.md.

## 1. `Emit_*` Helpers

`rg "Emit_[A-Z]" src/runtime/NovaSharp.Interpreter -g"*.cs"`

| File | Count | Notes |
| --- | --- | --- |
| Execution/VM/ByteCode.cs | 36 | Core IL emitter API; `Emit_` prefix used for every operation. |
| Tree/Statements/ForLoopStatement.cs | 12 | Loop lowering to bytecode. |
| Tree/Statements/ForEachLoopStatement.cs | 12 | Iterator lowering. |
| Tree/Expressions/FunctionDefinitionExpression.cs | 9 | Function compilation. |
| Tree/Statements/FunctionDefinitionStatement.cs | 7 | Named function declarations. |
| Tree/Statements/ChunkStatement.cs | 7 | Chunk entry point. |
| Tree/Statements/IfStatement.cs | 6 | Branch lowering. |
| Tree/Expressions/IndexExpression.cs | 6 | Member indexing assign/load. |
| (Remaining files) | ≤5 each | Break/return handling, literal loads, etc. |

**Recommendation:** Re-evaluate whether the `Emit_` prefix should migrate to PascalCase (e.g., `EmitPop`, `EmitJump`). ByteCode.cs dominates usage—renaming there first will ripple throughout the tree lowering classes. Automate with Roslyn refactor to avoid manual churn.

## 2. Members With `i_` / Hungarian Prefixes

`rg "\bi_[A-Za-z0-9_]*" src/runtime/NovaSharp.Interpreter -g"*.cs"`

| File | Count | Notes |
| --- | --- | --- |
| DataTypes/SymbolRef.cs | 41 | `i_Type`, `i_Index`, etc. exposed internally; consumers in Processor rely on fields directly. |
| Execution/VM/Processor/Processor.InstructionLoop.cs | 17 | Local scope slots mirrored from `SymbolRef`. |
| Execution/VM/Processor/Processor.Scope.cs | 16 | Uses `i_Index` when closing locals. |
| Execution/VM/ByteCode.cs | 5 | Bytecode uses `sym.i_Name`/`sym.i_Index`. |
| Script.cs | 4 | Legacy fields on Script. |
| Others | ≤3 | Sparse Hungarian remnants. |

**Recommendation:** Consider renaming the `SymbolRef` fields to `_type`, `_index`, etc., and surface read-only properties so other subsystems can adopt the new names without reflection hacks. Once SymbolRef is cleaned, the downstream `Processor.*` usages can follow. Script’s private fields can be renamed with standard `_camelCase` since they are internal.

## 3. Next Steps

1. Prototype a Roslyn-powered rename for `Emit_*` methods, starting with ByteCode.cs, and measure impact (build/test churn, public API implications).
2. Draft a migration plan for `SymbolRef` members and dependent code paths (Processor, ByteCode, debugger). Ensure binary serialization via `WriteBinary`/`ReadBinary` keeps working.
3. Update coding guidelines (docs/style) once naming rules are finalized; enforce via analyzers (e.g., IDE1006 custom severity).

Tracking Issue: PLAN.md “Naming alignment” next step.

# Skill: Debugging the Interpreter Pipeline

**When to use**: Investigating bugs in lexing, parsing, compilation, or execution.

**Related Skills**: [lua-spec-verification](lua-spec-verification.md) (comparing with reference Lua), [lua-fixture-creation](lua-fixture-creation.md) (creating test cases), [tunit-test-writing](tunit-test-writing.md) (minimal reproduction tests), [test-failure-investigation](test-failure-investigation.md) (investigating test failures)

______________________________________________________________________

## Pipeline Overview

See [context.md](../context.md) Architecture section for the full pipeline diagram. Each stage can be debugged independently.

______________________________________________________________________

## Stage 1: Lexer (Tokenization)

**Location**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/Lexer/`

### What it does

Converts source text into tokens (identifiers, keywords, operators, literals).

### Debugging

```csharp
// Create a lexer and inspect tokens
Lexer lexer = new Lexer(sourceCode, "test");
while (lexer.Current.Type != TokenType.Eof)
{
    Console.WriteLine($"{lexer.Current.Type}: '{lexer.Current.Text}'");
    lexer.Next();
}
```

### Common issues

- Incorrect token boundaries
- Missing/wrong keywords
- String escape sequences
- Number literal parsing (integer vs float)

______________________________________________________________________

## Stage 2: Parser (AST Construction)

**Location**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Tree/`

### What it does

Converts tokens into an Abstract Syntax Tree (AST).

### Debugging

```csharp
// Parse and inspect the AST
Script script = new Script();
DynValue chunk = script.LoadString("return 1 + 2");

// The chunk contains a function with the AST
// Use debugger to inspect chunk.Function.RootChunk
```

### Key AST nodes

- `Tree/Expressions/` — Binary ops, unary ops, function calls, etc.
- `Tree/Statements/` — Assignment, if, while, for, return, etc.

### Common issues

- Operator precedence
- Statement vs expression context
- Block scoping

______________________________________________________________________

## Stage 3: Compiler (Bytecode Generation)

**Location**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/ByteCode.cs`

### What it does

Converts AST into bytecode instructions.

### Debugging

```csharp
// Dump bytecode for inspection
Script script = new Script();
DynValue func = script.LoadString("return 1 + 2");
// Use debugger to inspect func.Function.ByteCode
```

### Compare with reference Lua

```bash
# Compile with Lua and disassemble
echo "return 1 + 2" | lua5.4 -l -
```

### Common issues

- Wrong opcode emitted
- Stack imbalance
- Jump target calculation
- Constant pool indexing

______________________________________________________________________

## Stage 4: VM (Execution)

**Location**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/Execution/VM/Processor.cs`

### What it does

Executes bytecode instructions using a stack-based VM.

### Debugging

Add tracing in `Processing_Loop()`:

```csharp
case OpCode.SomeOp:
{
    #if DEBUG
    Debug.WriteLine($"SomeOp: stack depth = {m_ValueStack.Count}");
    #endif
    // ... implementation
    break;
}
```

### Key components

- `m_ValueStack` — Operand stack
- `CallStackItem` — Stack frames for function calls
- `TailCallRequest` — Tail call optimization

### Common issues

- Stack underflow/overflow
- Wrong value types on stack
- Incorrect stack cleanup after calls
- Closure upvalue handling

______________________________________________________________________

## Stage 5: Standard Library

**Location**: `src/runtime/WallstopStudios.NovaSharp.Interpreter/CoreLib/`

### Key modules

- `BasicModule.cs` — `print`, `type`, `tonumber`, etc.
- `MathModule.cs` — `math.*` functions
- `StringModule.cs` — `string.*` functions
- `TableModule.cs` — `table.*` functions
- `IoModule.cs` — `io.*` functions

### Debugging

Set breakpoints in the relevant module method. Most stdlib functions are simple C# methods.

______________________________________________________________________

## Useful Debugging Techniques

### 1. Minimal reproduction

```csharp
[Test]
public async Task MinimalReproduction()
{
    Script script = new Script();
    DynValue result = script.DoString("return <minimal failing code>");
    // Inspect result
}
```

### 2. Compare with reference Lua

```bash
# Run same code in reference Lua
lua5.4 -e "print(<test code>)"

# Run in NovaSharp CLI
dotnet run -c Release --project src/tooling/WallstopStudios.NovaSharp.Cli -e "print(<test code>)"
```

### 3. Binary search the problem

If a complex script fails, bisect to find the minimal failing case.

### 4. Inspect DynValue contents

```csharp
DynValue value = script.DoString("return something");
Console.WriteLine($"Type: {value.Type}");
Console.WriteLine($"Value: {value.ToDebugPrintString()}");
```

______________________________________________________________________

## Common Bug Patterns

| Symptom               | Likely Stage | Check                                  |
| --------------------- | ------------ | -------------------------------------- |
| "unexpected token"    | Lexer        | Token boundaries, keywords             |
| "syntax error"        | Parser       | Operator precedence, statement parsing |
| Wrong result          | Compiler/VM  | Bytecode correctness, stack operations |
| "attempt to call nil" | VM/stdlib    | Function registration, module loading  |
| Type mismatch         | VM           | Type coercion rules                    |

______________________________________________________________________

## Key Files to Know

| File                                  | Purpose                  |
| ------------------------------------- | ------------------------ |
| `DataTypes/DynValue.cs`               | Universal value type     |
| `DataTypes/Table.cs`                  | Lua table implementation |
| `DataTypes/LuaNumber.cs`              | Numeric operations       |
| `Execution/ScriptExecutionContext.cs` | Execution state          |
| `Execution/Coroutine.cs`              | Coroutine implementation |

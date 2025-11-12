# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NovaSharp is a complete Lua 5.2 interpreter written in C# for .NET, Mono, Xamarin, and Unity3D platforms. It provides 99% Lua compatibility with advanced features like debugging support, bytecode dumping/loading, and seamless CLR interop.

## Build, Test, and Development Commands

### Initial Setup
```bash
# Install local CLI tools (CSharpier formatter)
dotnet tool restore
```

### Building
```bash
# Full release build of all targets
dotnet build src\NovaSharp.sln -c Release

# Quick iteration on interpreter core
dotnet build src\runtime\NovaSharp.Interpreter\NovaSharp.Interpreter.csproj

# Legacy MSBuild option (if Visual Studio tooling preferred)
msbuild src\NovaSharp.sln /p:Configuration=Release
```

### Testing
```bash
# Run all interpreter tests
dotnet test src\tests\NovaSharp.Interpreter.Tests\NovaSharp.Interpreter.Tests.csproj -c Release
```

### Code Formatting
```bash
# Format all code before committing (configured in csharpier.json)
dotnet csharpier .
```

## Architecture Overview

NovaSharp follows a classic interpreter pipeline with three main subsystems:

### 1. Pipeline Flow
```
Lua Source → [Lexer] → Tokens → [Parser] → AST → [Compiler] → Bytecode → [VM] → Execution
```

Entry point is the `Script` class, which coordinates the entire pipeline.

### 2. Core Subsystems

**Tree (Parsing & AST)** - `src/runtime/NovaSharp.Interpreter/Tree/`
- Converts source code to Abstract Syntax Tree
- Each AST node implements its own `Compile(ByteCode)` method
- Key classes: `Lexer`, `Parser`, `Statement`, `Expression`
- Loader_Fast.cs orchestrates the parse-compile sequence

**Execution/VM (Bytecode & Runtime)** - `src/runtime/NovaSharp.Interpreter/Execution/`
- `Processor` class implements stack-based virtual machine
- 52 opcodes (ADD, MUL, CALL, JF, etc.)
- Uses `FastStack<T>` for value stack and execution stack
- Each `Instruction` contains opcode, operands, and source location
- Supports tail call optimization for Lua compatibility

**Interop (C# ↔ Lua Bridge)** - `src/runtime/NovaSharp.Interpreter/Interop/`
- Bidirectional conversion between C# objects and Lua values
- Uses descriptor pattern via `IUserDataDescriptor`
- Global `TypeDescriptorRegistry` caches type metadata
- `UserData` class wraps C# objects for Lua access
- Supports reflection-based and optimized access modes

### 3. DynValue: The Universal Value Type

`DynValue` is the **central hub** of NovaSharp's architecture:
- Single unified type representing all Lua values (nil, boolean, number, string, table, function, etc.)
- Used everywhere: function parameters, return values, table contents, variables
- Contains a discriminated union: `DataType` enum + value storage (_Number for numbers, _Object for references)
- Provides type-safe interop via `FromObject()` and `ToObject()`
- Factory methods for each type: `NewString()`, `NewTable()`, `NewClosure()`, etc.

### 4. Key Architecture Patterns

**Closure & Upvalue Management:**
- `Closure` represents script functions with bytecode entry point
- `ClosureContext` stores upvalue references and captured local values
- `SymbolRef` identifies variables by name and scope, resolved at compile-time

**Table Implementation:**
- Lua tables support both array (numeric keys) and hash (string/object keys)
- `LinkedListIndex` provides hybrid structure: insertion order + O(1) lookup

**Debugging Integration:**
- `IDebugger` interface for pluggable debuggers
- Each bytecode `Instruction` carries `SourceRef` linking to source line
- VS Code debugger implements Debug Adapter Protocol (DAP)
- Remote debugger accessible via web browser

### 5. Critical Namespaces

- **Execution/VM**: Virtual machine core - `Processor`, `ByteCode`, `Instruction`, `OpCode`
- **Tree**: AST nodes - `Statement`, `Expression`, parsing logic
- **DataTypes**: Lua data structures - `DynValue`, `Table`, `Closure`, `Coroutine`, `UserData`
- **Interop**: C# bridging - converters, descriptors, type registry
- **CoreLib**: Built-in Lua modules - `TableModule`, `StringModule`, `MathModule`, etc.
- **Debugging**: Debug infrastructure - `IDebugger`, `DebugService`, `SourceCode`
- **DataStructs**: Custom containers - `FastStack<T>`, `LinkedListIndex<K,V>`

### 6. Execution Flow Example
```csharp
Script.DoString("return x + 1")
  → Loader_Fast.LoadChunk()
      → Lexer tokenizes source
      → Parser builds AST (ChunkStatement)
      → AST.Compile() emits bytecode
  → Script.Call(closure)
      → Processor.Call() sets up stack frame
      → Processing_Loop() executes bytecode instruction-by-instruction
  → Returns DynValue result
```

## Coding Style & Conventions

- **Indentation**: 4 spaces for C#, braces on new lines (Allman style)
- **Naming**: PascalCase for types/methods, `_camelCase` for private fields
- **Formatting**: Run `dotnet csharpier .` before committing (180 char line width, 4 space indent)
- **Lua Fixtures**: Preserve 2-space indentation for easier diffing with upstream
- **Type Usage**: Avoid implicit `var` when type is unclear
- **Using Directives**: Keep minimal, add explicit access modifiers; refer to `.editorconfig` for spacing and ordering conventions.
- **Explicit Typing**: Prefer explicit types over `var`; only use `var` where required (e.g., anonymous types).
- **Internals Access**: When NovaSharp projects or tests need deeper access, prefer declaring members `internal` and using `InternalsVisibleTo` (update or create `AssemblyInfo.cs`) instead of reflection. It is fine to expose internals to sibling NovaSharp assemblies if it eliminates reflection.
- **Reflection Policy**: Review `docs/modernization/reflection-audit.md` before adding new reflection-based code and update the catalogue if changes are necessary.

## Testing Guidelines

- **Framework**: NUnit 2.6 (`[TestFixture]`, `[Test]` attributes)
- **Organization**: Place tests in descriptive folders (e.g., `Units`, `EndToEnd`, feature-specific) with clear class names
- **Naming**: `<Feature>Tests.cs` pattern, store Lua fixtures alongside test classes
- **Method Names**: Use PascalCase without underscores for `[Test]` methods; rename legacy cases when modifying them
- **Suite Maintenance**: Update `tests/NovaSharp.Interpreter.Tests` when interpreter behavior changes; the consolidated project runs under NUnit and feeds CI coverage
- **Coverage Areas**: Add tests for new opcodes, metatables, debugger paths, and interop scenarios

## Commit & Pull Request Guidelines

- **Commit Messages**: Concise, imperative mood (e.g., "Fix parser regression")
- **Subsystem Prefixes**: Use when helpful (e.g., "debugger: Fix breakpoint handling")
- **Issue References**: Use `Fixes #ID` to auto-close issues on merge
- **PR Descriptions**: Summarize changes, list build/test commands executed, attach UI captures for debugger work
- **Breaking Changes**: Document prominently and coordinate release notes before merging
- **Main Branch**: Use `master` for pull requests

## Module Organization

  - **Runtime Code**: All under `src/runtime/`, interpreter in `src/runtime/NovaSharp.Interpreter/`
  - **Debuggers**: `src/debuggers/NovaSharp.VsCodeDebugger/`, `src/debuggers/NovaSharp.RemoteDebugger/`, and `src/debuggers/vscode-extension/`
  - **Tooling**: `src/tooling/` for the CLI (`NovaSharp`), hardwire generator, benchmarks, and perf comparisons
  - **Samples**: `src/samples/` for tutorials and examples
  - **Tests**: `src/tests/NovaSharp.Interpreter.Tests/` (NUnit-based suite powering local + CI execution)
  - **Legacy Assets**: Flash/Flex debugger, Lua52 binaries, and other historical scripts have been removed from `src/legacy`; see `docs/Modernization.md` for the deprecation summary.

## Important Implementation Notes

### When Working with the VM:
- Bytecode compilation happens **per-function**: each function has its own `ByteCode` object
- Stack frames are managed via `CallStackItem` - tracks locals, return address, and closure context
- Tail call optimization is critical for Lua compatibility - implemented via `TailCallRequest` return type

### When Working with Interop:
- Always register types via `UserData.RegisterType<T>()` before passing C# objects to Lua
- Use `InteropAccessMode.LazyOptimized` for best performance (caches reflection metadata)
- Custom type descriptors allow full control over member exposure
- Standard library modules (`CoreLib/`) show idiomatic patterns for exposing C# to Lua

### When Working with Tables:
- Tables use 1-based indexing (Lua convention)
- Metatables control operator overloading and property access (__index, __newindex, __add, etc.)
- `LinkedListIndex` maintains insertion order while providing O(1) key lookup

### When Adding Opcodes:
- Update `OpCode` enum in `Execution/VM/OpCode.cs`
- Implement handler in `Processor.Processing_Loop()`
- Add compiler emission in relevant AST node's `Compile()` method
- Test both direct execution and bytecode serialization/deserialization

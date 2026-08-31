# Generated Interop and Hardwire Retirement

This document records the implemented B1 generated-interop baseline and the work
that remains before the legacy Hardwire tool can be removed. `PLAN.md` selects
the active milestone; this page owns the design detail.

NovaSharp is pre-adoption. B1 must update repository-owned consumers atomically
and delete superseded host APIs and tooling. It must not retain a deprecated CLI
proxy, dump parser, compatibility shim, obsolete alias, or migration window.

## Current State

Two implementations currently coexist:

- The public opt-in contract is the `[LuaObject]`, `[LuaMember]`,
  `[LuaMetamethod]`, and `[LuaIgnore]` attribute set in
  [`LuaInteropAttributes.cs`](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Api/LuaInteropAttributes.cs).
- The packable `netstandard2.0` analyzer/source-generator project is
  [`WallstopStudios.NovaSharp.Interop.Generator`](../../src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj).
  It is in the solution and referenced by the TUnit project, but it is not yet a
  live analyzer dependency of runtime or sample projects.
- [`LuaInteropDiagnosticAnalyzer`](../../src/interop/WallstopStudios.NovaSharp.Interop.Generator/LuaInteropDiagnosticAnalyzer.cs)
  reports `NS0001` through `NS0007`. `NS0005` deliberately rejects `Task` and
  `ValueTask` returns until the async adapter contract exists.
- [`LuaInteropSourceGenerator`](../../src/interop/WallstopStudios.NovaSharp.Interop.Generator/LuaInteropSourceGenerator.cs)
  scans top-level, non-generic partial classes, structs, records, and record
  structs marked `[LuaObject]`. It emits deterministic companion partials with
  direct synchronous method dispatch, property/field `__index` and `__newindex`
  callbacks, referenced-enum tables, `__NovaSharpGeneratedRegister(...)`, and a
  private manifest string. Supported conversions currently cover the facade Lua
  types, primitives, strings, and enums accepted by the analyzer.
- Golden-source and emitted-assembly behavior are covered by
  [`LuaInteropGeneratorTUnitTests`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/LuaInteropGeneratorTUnitTests.cs)
  and the checked-in
  [`GoldenSources/LuaInteropGenerator`](../../src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/GoldenSources/LuaInteropGenerator/).
- The dump-driven CodeDom/VB implementation and `hardwire` CLI are still active
  under
  [`WallstopStudios.NovaSharp.Hardwire`](../../src/tooling/WallstopStudios.NovaSharp.Hardwire/)
  and
  [`HardwireCommand.cs`](../../src/tooling/WallstopStudios.NovaSharp.Cli/Commands/Implementations/HardwireCommand.cs).
  The solution, CLI parser, tests, and `_Hardwired.cs` fixture still depend on
  that path.

The generated callback source is reflection-free, but current tests use
reflection to load and invoke an emitted in-memory assembly. That is useful test
scaffolding, not trimmed-publish evidence. The private per-type manifest is also
not yet the single machine-readable API authority described in
[`runtime-research-gates.md`](runtime-research-gates.md#machine-readable-api-authority).

## Remaining B1 Work

1. **Settle adapter and suspension contracts.** Define the facade-owned async
   result/suspension boundary, then generate `Task`/`ValueTask` bindings without
   blocking, losing exceptions, or bypassing Lua coroutine and sandbox behavior.
   Replace the current `NS0005` rejection only for shapes the adapter supports.
1. **Add the `NS0002` code fix after those contracts stabilize.** The fix must
   offer only semantics-preserving changes to supported facade/adapter types; it
   must not guess conversions or hide an unsupported binding with a suppression.
1. **Adopt one machine-readable API authority.** Use one deterministic model for
   generated bindings, public documentation, debugger metadata, and LuaLS/EmmyLua
   stubs. Do not allow the private manifest, attributes, and stub model to drift
   as competing authorities.
1. **Emit LuaLS/EmmyLua stubs.** Generate stable names, overload/type information,
   enum tables, properties, fields, and async-facing types from that authority;
   cover ordering and escaping with golden tests.
1. **Close repository-required parity and retire Hardwire.** Wire the analyzer
   package into a real consumer, update every repository-owned registration and
   sample, and replace the legacy CLI/tool tests with generator diagnostics,
   output, packaging, and integration coverage. Then remove the Hardwire project,
   CLI mode and arguments, dump parsing, CodeDom/VB emitters, `_Hardwired.cs`, and
   their solution/project references in the same cutover. Parity means preserving
   required behavior, not preserving the old API or workflow.
1. **Prove a reflection-free trimmed consumer.** Publish a representative
   consumer with trimming enabled, execute generated method/property/field/enum
   bindings from the published output, and show that the generated path needs no
   descriptor scan or reflection fallback. Record the exact publish/run command
   and artifact evidence.

## Acceptance Evidence

- Analyzer, generator, code-fix, and stub golden tests cover positive, negative,
  boundary, deterministic-ordering, and escaped-name cases.
- An ordinary consumer project receives the packaged analyzer and compiles the
  emitted bindings without test-only generator-driver setup.
- The trimmed published consumer runs its generated bindings successfully and
  contains no dependency on the retired Hardwire assembly or CLI path.
- Repository search confirms the Hardwire project, CLI mode, dump format,
  generated fixture, and obsolete references are gone rather than forwarded.
- The focused interop suites, quick build, full tests, formatting, packaging, and
  applicable Unity/AOT checks complete successfully.

## Implementation History

- [Public generated-interop attributes](../../progress/session-150-b1-source-generator-attributes.md)
- [Analyzer diagnostics](../../progress/session-154-b1-analyzer-diagnostics.md) and
  [review hardening](../../progress/session-155-b1-analyzer-review-hardening.md)
- [Initial generator and golden tests](../../progress/session-160-b1-generator-golden-tests.md)
- [Enum output](../../progress/session-161-b1-enum-table-generation.md),
  [registration callbacks](../../progress/session-162-b1-generated-registration-callbacks.md),
  and [property/field bindings](../../progress/session-163-b1-property-field-bindings.md)
- [Unsigned enum and review hardening](../../progress/session-164-b1-review-unsigned-enum-roundtrip.md)

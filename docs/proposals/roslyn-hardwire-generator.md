# Roslyn Hardwire Generator

## Summary

- Replace the interactive `NovaSharp.Hardwire` CLI tool with a Roslyn incremental generator that emits the hardwired userdata descriptors during compilation.
- Keep the `NovaSharp.Interpreter` surface API unchanged while eliminating the manual Lua dump + code generation workflow.
- Provide analyzers and diagnostics that highlight unsupported patterns early (e.g., inaccessible members, missing attributes), improving Unity trims and CI automation.

## Goals

- Generate the existing hardwire descriptor surface (`HardwiredUserDataDescriptor`, `HardwiredMemberDescriptor`, etc.) at compile time with parity to the output produced by the CLI today (`src/tooling/NovaSharp.Hardwire/HardwireCodeGenerationContext.cs:1`).
- Support the same allow-list semantics as the current tool (`allowInternals`, skipped members, visibility validation) without requiring Lua dump files (`src/tooling/NovaSharp.Cli/Commands/Implementations/HardWireCommand.cs:90`).
- Enable opt-in adoption from runtime, CLI, and Unity consumers via `ItemGroup` metadata (e.g., `<NovaSharpHardwire Include="...">`), keeping incremental builds fast.
- Offer diagnostics that surface when a type cannot be hardwired so contributors do not need to inspect generated code manually.
- Document configuration + migration in `docs/Modernization.md` and align tests to the new pipeline.

## Non-Goals

- Rewriting descriptor consumers; the runtime will continue to call `UserData.RegisterType` and friends.
- Dropping Lua-based metadata entirely. We still allow the CLI command to parse historical dumps until every consumer migrates.
- Shipping IL weaving or runtime reflection fallback beyond what already exists.

## Current Pipeline

1. Author runs `hardwire` from the CLI (`src/tooling/NovaSharp.Cli/Commands/Implementations/HardWireCommand.cs:18`).
1. The command loads a Lua dump table, instantiates `HardwireGenerator`, and emits a `.cs` / `.vb` file (`src/tooling/NovaSharp.Hardwire/HardwireGenerator.cs:8`).
1. The generated file is committed or copied into a project; manual steps are required to keep descriptors synchronized.
1. Validation is entirely post-generation (warnings printed to console); CI cannot fail early when members are skipped.
1. Unity packages rely on pre-generated files, making iteration clunky when new APIs are exposed.

### Challenges

- Hardwire metadata depends on runtime type discovery (`Table` entries fed into `DispatchTablePairs`), so generation is detached from the actual sources.
- Developers must remember to re-run the tool after making changes.
- No diagnostics in IDE/CI when a member is skipped; problems surface only after running the tool.
- Tooling emits full files, making diffs noisy and hard to review.

## Target Architecture

### Generator shape

- Add `src/tooling/NovaSharp.Hardwire.Generator/NovaSharp.Hardwire.Generator.csproj` targeting `netstandard2.0` (Roslyn source generator baseline) and referencing Roslyn `Microsoft.CodeAnalysis.CSharp`.
- Implement an incremental generator that:
  - Scans for types decorated with `NovaSharpUserDataAttribute` (`src/runtime/NovaSharp.Interpreter/Interop/Attributes/NovaSharpUserDataAttribute.cs:14`) or registered through explicit `UserData.RegisterType` configuration lists.
  - Synthesizes the descriptor graph directly from reflection metadata available in the compilation (`INamedTypeSymbol`, `IMethodSymbol`, etc.).
  - Mirrors the existing CodeDom output structure (kickstarter, descriptor classes, registration calls) but emits minimal partial classes to reduce diff churn.
- Provide a companion analyzer that ensures:
  - Only supported `InteropAccessMode` values are used for generator-backed types.
  - `allowInternals` is respected by checking accessibility of members (internal members require `InternalsVisibleTo("NovaSharp.Interpreter")`).

### Inputs & Configuration

- `NovaSharpHardwire` `AdditionalFiles` metadata (e.g., JSON) for advanced scenarios:
  - Overriding namespace/class name (matching CLI prompts).
  - Skipping or aliasing members not expressible via attributes.
- MSBuild properties in consuming projects:
  - `NovaSharpHardwireEnable` (bool) - opt-in switch.
  - `NovaSharpHardwireAllowInternals` (bool) - default `false`, maps to the legacy prompt.
  - `NovaSharpHardwireLanguage` (enum) - defaults to `CSharp`; `VB` support is best-effort.

### Outputs

- Generated partial classes under `NovaSharp.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors.Generated`.
- A deterministic registration method (`RegisterGeneratedTypes`) invoked from `UserData.RegisterAssemblyTypes` (guarded by partial method to avoid double-registration).
- Diagnostics (e.g., `NSHW001` for unsupported visibility, `NSHW002` for missing attribute) reported via `GeneratorExecutionContext.ReportDiagnostic`.

### CLI Compatibility Story

- Keep `NovaSharp.Cli hardwire` but emit a deprecation warning that points to the generator.
- Refactor the CLI command to proxy into the generator once the generator can emit sources to disk (for legacy workflows).
- Update CLI tests (`src/tests/NovaSharp.Interpreter.Tests/Units/HardWireCommandTests.cs`) to validate the warning + generator fallback.

## Integration Plan

| Phase | Scope              | Deliverables                                                                                                                                |
| ----- | ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| 0     | Prototype          | Spike incremental generator aligning metadata structure; add sample golden tests.                                                           |
| 1     | Runtime Enablement | Reference generator from `NovaSharp.Interpreter.csproj`, gate behind MSBuild property, add smoke tests under `NovaSharp.Interpreter.Tests`. |
| 2     | CLI / Tooling      | Update CLI to prefer generator, retire CodeDom + Lua-only path.                                                                             |
| 3     | Cleanup            | Delete `NovaSharp.Hardwire` CodeDom project, remove `_Hardwired.cs` artifacts, update docs/tests.                                           |

## Testing Strategy

- Unit tests exercising generator diagnostics with `CSharpGeneratorDriver` harness (net8.0).
- Golden-file tests comparing generated descriptors vs. legacy CLI output for representative types (numbers, overloads, events, proxies).
- Integration tests running `dotnet build` on a sample project that consumes the generator and verifying `UserData.RegisterAssemblyTypes` still works.
- Unity compatibility sample verifying the generated code strips under IL2CPP and no global usings leak.

## Risks & Open Questions

- How to honour `allowInternals` without forcing every consumer to expose internals to the generator assembly?
- Ensuring generator output stays deterministic across Roslyn versions (CodeDom had known ordering quirks; we need to normalize symbol ordering).
- VB support: minimal usage today, but we must decide whether to keep or drop VB emission.
- Migration for consumers relying on Lua customization (skip markers inside the dump table). Need an attribute story (e.g., `[NovaSharpUserDataSkip("MemberName")]`).

## Follow-Up Tasks

- Update `docs/Modernization.md` and `docs/Testing.md` once the generator ships.
- Remove legacy tests that depend on CodeDom once parity is achieved.
- Track performance of incremental generator in large solutions (>500 types).

## Reflection-Free Interop Scaffold (Augmentation Study)

The hardwire generator unlocks an opportunity to reduce the interpreter’s dependence on runtime reflection by emitting cached delegates up front. Inspired by Wallstop’s `ReflectionHelpers` utilities in the Unity Helpers project, the following hotspots merit targeted work once the generator is in place:

| Hotspot                                                            | Current Behaviour                                                                                    | Proposed Direction                                                                                                                                                                     |
| ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `StandardUserDataDescriptor` + member descriptors                  | Builds `MemberDescriptor` instances using `System.Reflection` lookups for every member registration. | Have the generator emit resolver structs that expose the required `Func<ScriptExecutionContext, DynValue[], DynValue>` delegates directly, eliminating repeated reflection at runtime. |
| `TypeDescriptorRegistry`                                           | Uses `Assembly.GetCallingAssembly()` and reflection to discover registerable types.                  | Extend the generator to emit a compile-time type manifest (`NovaSharpGeneratedUserDataManifest`) so registries can iterate over a static array instead of scanning assemblies.         |
| `UnityAssetsScriptLoader`                                          | Relies on `Type.GetType("UnityEngine.*")` calls to map Unity types.                                  | Generate optional Unity shims that reference strongly-typed interfaces when the Unity assemblies are available, falling back to the reflection path otherwise.                         |
| Diagnostics helpers (`PropertyTableAssigner`, `DescriptorHelpers`) | Invoke `PropertyInfo.GetValue/SetValue` (boxing) during userdata marshaling.                         | Emit cached field/property access delegates alongside the hardwire descriptors; use analyzer diagnostics (`NSHW00x`) to guard unsupported member signatures.                           |

### Phased Migration Plan

1. **Generator Outputs**: Introduce optional emission of descriptor helper structs (`GeneratedMemberDispatch`) that contain cached delegates for getters, setters, and invocations.
1. **Runtime Opt-In**: Add internal extension methods to consume the generated helpers when the generator is enabled (`NovaSharpHardwireUseGeneratedDispatch` MSBuild flag), while keeping the reflection-backed path for legacy builds.
1. **Unity Lift**: Provide a preprocessor symbol (`NOVASHARP_UNITY_HARDWIRE`) that swaps Unity loaders over to the generated code when Unity references are present, avoiding `Type.GetType` in production builds.
1. **Telemetry & Benchmarks**: Capture allocation + timing improvements in the benchmark suite; gate rollout on measurable wins to avoid regressions in cold-start scenarios.

Any new reflection entry points introduced during this work must be added to `docs/modernization/reflection-audit.md` so the modernization plan stays accurate.

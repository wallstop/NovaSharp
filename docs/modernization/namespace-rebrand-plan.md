# Wallstop Studios Namespace & Package Rebrand Plan

This document captures the staged rollout plan for rebranding every NovaSharp assembly and namespace under the `WallstopStudios.*` umbrella while retaining the **NovaSharp** product identity. The goal is to introduce the new naming without derailing in-progress modernization work, keep Unity/netstandard consumers unblocked, and provide a clear migration story for hosts consuming the public APIs.

## Goals

- Adopt `WallstopStudios.NovaSharp.*` as the canonical namespace for runtime, tooling, debugger, and sample code.
- Align every distributable package/tool ID to the `com.wallstop-studios.*` naming scheme to match Wallstop Studios’ package governance.
- Preserve the NovaSharp brand in type and package descriptors (e.g., `WallstopStudios.NovaSharp.Interpreter.Script`).
- Provide analyzers and CI gates so new code lands directly in the rebranded namespaces.
- Deliver a migration guide plus compatibility shims so downstream hosts can move on their own schedule.

## Constraints & Risks

- **Breaking change**: Namespaces/assembly names change, so all consumers must update `using` statements and binding redirects.
- **Unity/netstandard**: Runtime binaries must remain `netstandard2.1` friendly; global usings remain disabled to keep Unity builds deterministic.
- **InternalsVisibleTo**: Tests and tooling depend on friend assemblies; all `InternalsVisibleTo` attributes must be updated in lockstep with assembly renames.
- **Packaging & CI**: GitHub workflows, documentation, scripts, and dotnet-tool manifests reference the current names. They need to be updated atomically to avoid broken builds.
- **Type forwarding**: C# does not support namespace-level forwards. Back-compat requires either stub assemblies or obsoleted wrapper namespaces—plan assumes we ship a major version bump with temporary wrapper types for the most-used entry points (`Script`, `DynValue`, CLI commands).

## Current State Snapshot

| Project                     | Path                                          | Output                                  | Assembly / Root Namespace     | Package / Tool Id               | Notes                                                          |
| --------------------------- | --------------------------------------------- | --------------------------------------- | ----------------------------- | ------------------------------- | -------------------------------------------------------------- |
| NovaSharp.Interpreter       | `src/runtime/NovaSharp.Interpreter`           | Class library (`netstandard2.1`)        | `NovaSharp.Interpreter`       | `NovaSharp.Interpreter.netcore` | Primary runtime; referenced by every other project.            |
| NovaSharp.Interpreter.Tests | `src/tests/NovaSharp.Interpreter.Tests`       | NUnit test host (`net8.0`)              | `NovaSharp.Interpreter.Tests` | n/a                             | Friend assembly access via `InternalsVisibleTo`.               |
| NovaSharp.Cli               | `src/tooling/NovaSharp.Cli`                   | CLI (`netstandard2.1`)                  | `NovaSharp.Cli`               | n/a (not currently packed)      | Ships REPL + tooling wrappers.                                 |
| NovaSharp.RemoteDebugger    | `src/debuggers/NovaSharp.RemoteDebugger`      | Class library (`netstandard2.1`)        | `NovaSharp.RemoteDebugger`    | n/a                             | Hosts remote debugger protocol + HTML assets.                  |
| NovaSharp.VsCodeDebugger    | `src/debuggers/NovaSharp.VsCodeDebugger`      | Class library (`netstandard2.1;net8.0`) | `NovaSharp.VsCodeDebugger`    | `NovaSharp.VsCodeDebugger`      | Distributed as NuGet + VS Code extension payload.              |
| NovaSharp.Hardwire          | `src/tooling/NovaSharp.Hardwire`              | Library (`netstandard2.1`)              | `NovaSharp.Hardwire`          | n/a                             | Generates hardwired descriptors; consumed by CLI + benchmarks. |
| NovaSharp.Benchmarks        | `src/tooling/Benchmarks/NovaSharp.Benchmarks` | Console (`net8.0`)                      | `NovaSharp.Benchmarks`        | n/a                             | Local-only performance harness.                                |
| NovaSharp.Comparison        | `src/tooling/NovaSharp.Comparison`            | Console (`net8.0`)                      | `NovaSharp.Comparison`        | n/a                             | Benchmarks vs NLua.                                            |

## Proposed Naming Baseline

### Namespace Roots

| Scope                                      | Current Prefix                                         | Target Prefix                                                                          | Notes                                                                                                                 |
| ------------------------------------------ | ------------------------------------------------------ | -------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Runtime & shared data types                | `NovaSharp.Interpreter`                                | `WallstopStudios.NovaSharp.Interpreter`                                                | Applies to `Execution`, `DataTypes`, `LuaPort`, `Interop`, etc. LuaPort keep snake_case but moves under the new root. |
| Test assemblies                            | `NovaSharp.Interpreter.Tests`                          | `WallstopStudios.NovaSharp.Interpreter.Tests`                                          | Mirrors runtime namespace so InternalsVisibleTo stays aligned.                                                        |
| CLI tooling                                | `NovaSharp.Cli`                                        | `WallstopStudios.NovaSharp.Cli`                                                        | Includes commands, REPL infra, shared options.                                                                        |
| Debuggers                                  | `NovaSharp.RemoteDebugger`, `NovaSharp.VsCodeDebugger` | `WallstopStudios.NovaSharp.RemoteDebugger`, `WallstopStudios.NovaSharp.VsCodeDebugger` | Shared debugger core should eventually live under `WallstopStudios.NovaSharp.Debuggers.*`.                            |
| Tooling (Hardwire, Benchmarks, Comparison) | `NovaSharp.*`                                          | `WallstopStudios.NovaSharp.*`                                                          | Keeps internal-only tooling consistent to simplify global using/search.                                               |

### Package & Distribution Identifiers

| Artifact                    | Current Id                      | Target Id                                                   |
| --------------------------- | ------------------------------- | ----------------------------------------------------------- |
| Runtime NuGet               | `NovaSharp.Interpreter.netcore` | `com.wallstop-studios.novasharp.interpreter`                |
| VS Code Debugger NuGet      | `NovaSharp.VsCodeDebugger`      | `com.wallstop-studios.novasharp.vscode-debugger`            |
| Remote Debugger (if packed) | _n/a_                           | `com.wallstop-studios.novasharp.remote-debugger` (optional) |
| CLI (future dotnet tool)    | _n/a_                           | `com.wallstop-studios.novasharp.cli`                        |
| Future Unity packages       | `NovaSharp.*` folder drops      | `com.wallstop-studios.novasharp.*` UPM tarballs             |

Package description/URL metadata should reference `https://wallstop-studios.com/novasharp` once the marketing site is ready.

## Rollout Stages

### Stage 0 – Tooling & Tracking (this document)

1. Check in this plan under `docs/modernization` and link it from `PLAN.md`.
1. Extend `tools/NamingAudit/naming_audit.py` to support configurable namespace prefixes so we can measure progress (`NovaSharp.` vs `WallstopStudios.` counts).
1. Add a `NamespaceRebrand` section in `docs/Modernization.md` pointing to this plan and enumerating affected projects.

### Stage 1 – Guard Rails

1. Update `.editorconfig` / Roslyn naming rules to flag new files authored under `namespace NovaSharp.*` (warning initially, error once Stage 2 lands). Provide an allowlist for `LuaPort` mirrors that cannot change without desyncing upstream sources.
1. Add a repo-wide MSBuild property (e.g., `NovaSharpNamespacePrefix`) in `Directory.Build.props` so analyzers, scripts, and templates can switch based on a single value.
1. Teach CI (`tests.yml`) to run `tools/NamingAudit` with the new prefix check and fail if fresh `NovaSharp.*` namespaces are introduced once migration starts.
   - ✅ `Directory.Build.props` now exposes `LegacyNamespacePrefix`, `TargetNamespacePrefix`, `EnforcedNamespacePrefix`, and `NamespacePrefixExcludedNamespaces` (defaults to `NovaSharp.Interpreter.LuaPort`) along with a severity toggle. `Directory.Build.targets` generates a scoped `.editorconfig` from those properties and feeds it to every project via `EditorConfigFiles`, so analyzers surface a suggestion-level diagnostic for non-compliant namespaces while respecting the LuaPort allowlist.

### Stage 2 – Runtime Assembly Rename

1. Update `NovaSharp.Interpreter.csproj`:
   - `AssemblyName`, `RootNamespace`, and `PackageId` swap to the new identifiers.
   - Add `PackageVersion` bump (SemVer major) and `Company` metadata.
1. Rename namespaces inside `src/runtime` using folder-aligned pass (reuse the existing namespace migration scripts). Tackle one folder per PR (e.g., `Execution/*`, `DataTypes/*`) to keep diffs reviewable.
1. Update `Properties/AssemblyInfo.cs` with the new titles/company and fix `InternalsVisibleTo` targets.
1. Add temporary compatibility wrappers for the most widely referenced entry points (`Script`, `DynValue`, `UserData`, CLI commands):
   - Add `NovaSharp.LegacyNamespaces.cs` containing `[Obsolete]` forwarding partial classes that derive from the new types or expose static helper methods (limited surface to avoid bloating binaries).
   - Document that the wrappers will be removed after one release.
1. Run `dotnet build src/NovaSharp.sln -c Release` and `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release`.

### Stage 3 – Tooling & Debugger Rename

1. Repeat the assembly/namespace rename for `NovaSharp.Cli`, `NovaSharp.RemoteDebugger`, `NovaSharp.VsCodeDebugger`, `NovaSharp.Hardwire`, `NovaSharp.Benchmarks`, and `NovaSharp.Comparison`.
1. Update `InternalsVisibleTo` declarations in each project plus the shared CLI/test helper attributes.
1. Replace all project references (`ProjectReference Include="...NovaSharp.*.csproj"`) with the new file names once the folders are renamed.
1. Ensure VS Code extension packaging scripts, debugger manifest JSON, and CLI docs reference the new assemblies.

### Stage 4 – Packaging & Distribution

1. Update `dotnet pack` pipelines to emit the new `PackageId`s and add `PackageIcon`, `PackageProjectUrl`, and `RepositoryUrl` metadata pointing to the rebranded resources.
1. Teach CI release workflows to publish both NuGet packages and Unity-specific artifacts under the `com.wallstop-studios.*` convention.
1. Run a dry-run release: produce signed packages, install them in a sample Unity project, and smoke test CLI + debugger attachment.
1. Prepare migration notes (README, docs/Modernization, docs/UnityIntegration) describing the namespace change and how to update `using` statements. Call out the temporary legacy wrappers and removal timeline.

### Stage 5 – Public Communication & Cleanup

1. Announce the breaking change (blog/README/CHANGELOG). Highlight major-version bump and actionable steps for hosts.
1. After one release cycle, remove the legacy wrappers and delete any remaining `NovaSharp.*` namespaces from source.
1. Flip the analyzer severity from warning → error to prevent regressions.
1. Archive the previous package IDs (`NovaSharp.*`) after confirming consumers migrated.

## Analyzer & Validation Updates

- `tools/NamingAudit`: add a `--namespace-prefix WallstopStudios.NovaSharp` flag plus an `--allowlist` for LuaPort & legacy wrappers.
- `.editorconfig`: add `dotnet_naming_symbols.namespace_symbols` rule enforcing `WallstopStudios.` prefix once the rename lands.
- CI: extend `tests.yml` to run `dotnet csharpier check .` (via `scripts/ci/check-csharpier.sh`) plus `naming_audit.py --verify-log` using the new prefix so formatting/naming regressions fail early.
- Scripts: update `scripts/coverage/coverage.ps1` and `scripts/coverage/coverage-hotspots.md` references after the rename so coverage automation continues to work.

## Compatibility & Communication Plan

- Ship the first rebranded runtime as **NovaSharp 3.0** to signal the breaking change.
- Provide a `docs/modernization/namespace-rebrand-guide.md` (follow-up item) covering:
  - How to replace `using NovaSharp.Interpreter;` with `using WallstopStudios.NovaSharp.Interpreter;`.
  - How to update `InternalsVisibleTo` in host projects if they built friend assemblies.
  - CLI command name changes (if any).
- Keep `NovaSharp.LegacyNamespaces.cs` wrappers in the runtime for one release and mark them `[Obsolete("Use WallstopStudios.NovaSharp.* instead")]`.
- Consider publishing a `NovaSharp.LegacyNamespaces` source-only NuGet package that adds `global using` aliases for hosts that cannot move immediately.

## Immediate Next Steps

1. Wire this plan into `PLAN.md` and `docs/Modernization.md`.
1. Extend `tools/NamingAudit` with namespace-prefix awareness so we can quantify remaining files.
1. Draft the analyzer configuration changes (Stage 1) and land them behind an opt-in MSBuild property.
1. Inventory all documentation (`docs/`, `README`, samples) that reference `NovaSharp.` so we know the surface area before edits begin.

Track every milestone in `PLAN.md` under the “Project Structure Refactor (High Priority)” section to keep contributors aligned on progress.

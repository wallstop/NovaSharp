# Namespace and Package Rebrand Status

The repository-wide move to `WallstopStudios.NovaSharp.*` project, assembly,
internal namespace, and NuGet names is complete. This document is a status and
history record, not an active staged-migration plan. Current paths and project
files are authoritative when they differ from old discussions.

The intentionally small public facade uses the root `NovaSharp` namespace. That
is not a legacy alias for `NovaSharp.Interpreter`; it is the current host-facing
API. Future facade consolidation is B5/B6 work and must not recreate the removed
namespace surface as a compatibility layer.

## Current Naming

| Surface           | Current authority                                                                                                                                                                                                                                                   |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Runtime           | [`src/runtime/WallstopStudios.NovaSharp.Interpreter`](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/) builds assembly and NuGet package `WallstopStudios.NovaSharp.Interpreter`; implementation namespaces use `WallstopStudios.NovaSharp.Interpreter.*`. |
| Infrastructure    | [`WallstopStudios.NovaSharp.Interpreter.Infrastructure.csproj`](../../src/runtime/WallstopStudios.NovaSharp.Interpreter.Infrastructure/WallstopStudios.NovaSharp.Interpreter.Infrastructure.csproj) owns the matching assembly, namespace, and NuGet ID.            |
| Public facade     | [`src/runtime/WallstopStudios.NovaSharp.Interpreter/Api`](../../src/runtime/WallstopStudios.NovaSharp.Interpreter/Api/) exposes the selected host API as `NovaSharp.*`.                                                                                             |
| Debuggers         | The remote and VS Code projects, assemblies, namespaces, and NuGet IDs use `WallstopStudios.NovaSharp.RemoteDebugger` and `WallstopStudios.NovaSharp.VsCodeDebugger`.                                                                                               |
| Tooling and tests | CLI, Hardwire, benchmark, comparison, batch-runner, sample, and test project/folder names use `WallstopStudios.NovaSharp.*`. Hardwire remains only because B1 replacement work is unfinished, not for naming compatibility.                                         |
| Generated interop | [`WallstopStudios.NovaSharp.Interop.Generator`](../../src/interop/WallstopStudios.NovaSharp.Interop.Generator/) is the current analyzer/source-generator project and package name.                                                                                  |
| Unity             | The tracked package root and UPM identifier are [`src/unity/com.wallstop-studios.novasharp`](../../src/unity/com.wallstop-studios.novasharp/), distinct from the NuGet naming convention.                                                                           |
| Packaging         | [`scripts/packaging/README.md`](../../scripts/packaging/README.md) and [`.github/workflows/nuget-publish.yml`](../../.github/workflows/nuget-publish.yml) use the current project and package names.                                                                |

The generated [naming audit](../audits/naming_audit.log) distinguishes the
`WallstopStudios.NovaSharp` implementation namespaces from the intentional root
`NovaSharp` facade and reports no old `NovaSharp.Interpreter.*` source namespace.

## Residual Work

The rename itself should not be reopened. Remaining cleanup is narrower:

1. Replace the transitional namespace-rule defaults in
   [`Directory.Build.props`](../../Directory.Build.props). They still default
   `EnforcedNamespacePrefix` to `NovaSharp`, keep a pre-cutover
   `NovaSharp.Interpreter.LuaPort` exclusion, and emit only suggestion-level
   diagnostics. A replacement guard must explicitly allow the root `NovaSharp`
   facade while enforcing `WallstopStudios.NovaSharp` for implementation code.
1. Correct remaining undated documentation that still presents the rebrand as
   pending, including [`docs/Modernization.md`](../Modernization.md). Preserve
   genuinely historical reports as dated snapshots instead of rewriting their
   recorded measurements or paths as if they were current.
1. Verify package publication and consumer installation through the release
   workflow when release work is selected. Repository configuration proves the
   intended IDs; it is not evidence that a particular release was published or
   installed successfully.
1. Keep later B5/B6 facade, DAP, and extension redesign atomic. Remove
   superseded host APIs directly and update repository-owned callers, tests,
   samples, tools, and docs in the same cutover.

## No-Legacy Rule

Do not add old namespace wrappers, type forwards, global-using alias packages,
stub assemblies, old package IDs, obsolete forwarding members, migration
adapters, or deprecation windows. NovaSharp is pre-adoption, so historical names
do not create a host-API compatibility obligation. This policy does not relax
Lua-version behavior or supported-platform compatibility.

## History and Evidence

- [PR #19](https://github.com/wallstop/NovaSharp/pull/19) performed the main
  project, assembly, namespace, and package cutover.
- [PR #30](https://github.com/wallstop/NovaSharp/pull/30) normalized the remaining
  repository layout, tooling/test paths, packaging, and audit wiring.
- [`README.md`](../../README.md) shows the current NuGet ID and implementation
  namespace in user-facing installation and embedding examples.
- [`Directory.Build.props`](../../Directory.Build.props) centralizes current
  company, product, repository, version, and release-note metadata; the namespace
  rule fields called out above remain transitional debt.

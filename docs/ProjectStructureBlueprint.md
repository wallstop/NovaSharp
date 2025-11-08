# Project Structure Blueprint

This document captures the current repository layout, highlights legacy or duplicated locations, and proposes a consolidated structure that aligns with the modernization guidelines in `PLAN.md`.

## 1. Current State (Nov 2025, post-initial refactor)

| Path | Purpose | Notes / Issues |
| --- | --- | --- |
| `src/runtime/MoonSharp.Interpreter` | Core runtime source | Multi-targeted (`netstandard2.1; net8.0`) with `_Projects` staging removed. |
| `src/debuggers/MoonSharp.RemoteDebugger` | Remote debugger assemblies | Path aligned, no structural changes needed. |
| `src/debuggers/MoonSharp.VsCodeDebugger` | VS Code debugger backend | Multi-targeted (`netstandard2.1; net8.0`) with `_Projects` mirror removed. |
| `src/debuggers/vscode-extension` | VS Code extension (TypeScript) | Now grouped under debuggers. |
| `src/tooling/MoonSharp.Cli` | CLI shell (`MoonSharp.Cli.csproj`) | Renamed; update packaging/docs to reflect new CLI name. |
| `src/tooling/MoonSharp.Hardwire` | Hardwire generator | Tooling category aligned. |
| `src/tooling/Benchmarks`, `src/tooling/PerformanceComparison` | Benchmark/perf harnesses | Paths aligned; scripts still assume legacy locations. |
| `src/tests/TestRunners/DotNetCoreTestRunner` | Net8 runner (active) | Drives modern coverage pipeline. |
| `src/tests/MoonSharp.Interpreter.Tests.Legacy` | Legacy NUnit 2.6 tree | Ready for gradual migration or archival. |
| `src/samples/Tutorial` | Tutorial snippets | Under dedicated samples hierarchy. |
| `src/legacy/*` (`Flash`, `MoonSharpPreGen`, `Tools`, `moonsharp_netcore`) | Archived assets | Confirm safe to delete or retain as read-only history. |
| `docs/manual/MoonSharp.Documentation` | Historical documentation | Lives under docs tree; evaluate for migration to markdown. |

## 2. Target Layout (post-Milestone B/C)

```
src/
  runtime/
    MoonSharp.Interpreter/                 (multi-targeted, no `_Projects`)
  debuggers/
    MoonSharp.RemoteDebugger/
    MoonSharp.VsCodeDebugger/
    vscode-extension/
  tooling/
    MoonSharp.Cli/
    MoonSharp.Hardwire/
    Benchmarks/
    PerformanceComparison/
  tests/
    TestRunners/DotNetCoreTestRunner/
    fixtures/                              (Lua TAP assets shared by tests)
  samples/
    Tutorial/
  docs/
    manual/                                (historical documentation)
  legacy/
    Flash/
    MoonSharpPreGen/
    Tools/
    moonsharp_netcore/
  packaging/
    signing/                               (e.g., `keypair.snk`, nuspecs)
```

Key principles:

- **Single source of truth:** Eliminate `_Projects/*netcore` mirrors by converting main projects to multi-targeting where required.
- **Namespace alignment:** Paths should mirror namespaces (`runtime/MoonSharp.Interpreter/...`), satisfying the new `.editorconfig` rules.
- **Clear ownership:** Runtime vs. debugger vs. tooling content live in dedicated top-level folders, aiding discoverability.
- **Legacy quarantine:** Old clients/tools move under `legacy/` (or are deleted) so they no longer clutter active build graphs.

## 3. Migration Plan

1. **Inventory & Deletion Pass**
   - Confirm no build/test references to legacy assets under `src/legacy/`. Archive or remove as appropriate.
   - Migrate any remaining useful scripts into `tooling/` or `docs/`.

2. **Project Updates**
   - Collapse `_Projects/...netcore` folders by multi-targeting the primary csproj (e.g., `MoonSharp.Interpreter.csproj` → `<TargetFrameworks>netstandard2.1;net8.0</TargetFrameworks>`). ✅ Interpreter and VS Code debugger complete; validate remaining tooling projects.
   - ✅ Renamed `tooling/MoonSharp/MoonSharp.csproj` to `tooling/MoonSharp.Cli/MoonSharp.Cli.csproj` and relocated CLI assets accordingly.
   - Update `MoonSharp.sln`, project `RootNamespace`, and `AssemblyName` values after rename/multi-target work.

3. **Namespace + Usings Sweep**
   - After moves, fix namespaces to reflect the new folder structure (enforced by `.editorconfig`).
   - Leverage analyzers to ensure `using` directives remain inside namespaces.

4. **Tests & Pipelines**
   - Update GitHub workflows, docs, and helper scripts (`coverage.ps1`, etc.) to reference the new paths (partial work complete).
   - Share TAP fixtures via `tests/fixtures/` to remove duplication between legacy and modern runners.

5. **Documentation Alignment**
   - Refresh `docs/Modernization.md` and `docs/Testing.md` with the new folder map.
   - Add contributor guidance describing where new runtime/tooling/debugger code should live.

6. **Cleanup**
   - Remove stale solution folders once all references are migrated.
   - Ensure `keypair.snk` and other packaging assets are relocated to `packaging/`.

## 4. Open Questions

- Do we keep `MoonSharpPreGen` for historical builds, or move it to a separate archive repository?
- Should `MoonSharp.Interpreter.Tests` be fully migrated into the new `tests/` structure, or partially archived?
- How much of `Tools/` is still relevant? (Needs triage; most content predates .NET Core.)

These questions should be resolved during Milestone B execution before deleting any assets.

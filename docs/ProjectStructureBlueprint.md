# Project Structure Blueprint

This document captures the current repository layout, highlights legacy or duplicated locations, and proposes a consolidated structure that aligns with the modernization guidelines in `PLAN.md`.

## 1. Current State (Nov 2025, post-initial refactor)

| Path | Purpose | Notes / Issues |
| --- | --- | --- |
| `src/runtime/NovaSharp.Interpreter` | Core runtime source | Multi-targeted (`netstandard2.1; net8.0`) with `_Projects` staging removed. |
| `src/debuggers/NovaSharp.RemoteDebugger` | Remote debugger assemblies | Path aligned, no structural changes needed. |
| `src/debuggers/NovaSharp.VsCodeDebugger` | VS Code debugger backend | Multi-targeted (`netstandard2.1; net8.0`) with `_Projects` mirror removed. |
| `src/debuggers/vscode-extension` | VS Code extension (TypeScript) | Now grouped under debuggers. |
| `src/tooling/NovaSharp.Cli` | CLI shell (`NovaSharp.Cli.csproj`) | Renamed; NuGet restore only (no checked-in packages); release docs now reference the `cli` drop (formerly `repl`). |
| `src/tooling/NovaSharp.Hardwire` | Hardwire generator | Tooling category aligned. |
| `src/tooling/Benchmarks`, `src/tooling/NovaSharp.Comparison` | Benchmark/perf harnesses | Paths aligned; scripts still assume legacy locations. |
| `src/tests/NovaSharp.Interpreter.Tests` | Consolidated NUnit suite | Powers local + CI execution; hosts Lua TAP fixtures. |
| `src/samples/Tutorial` | Tutorial snippets | Under dedicated samples hierarchy. |
| `docs/manual/NovaSharp.Documentation` | Historical documentation | Lives under docs tree; evaluate for migration to markdown. |

## 2. Target Layout (post-Milestone B/C)

```
src/
  runtime/
    NovaSharp.Interpreter/                 (multi-targeted, no `_Projects`)
  debuggers/
    NovaSharp.RemoteDebugger/
    NovaSharp.VsCodeDebugger/
    vscode-extension/
  tooling/
    NovaSharp.Cli/
    NovaSharp.Hardwire/
    Benchmarks/
    NovaSharp.Comparison/
  tests/
    NovaSharp.Interpreter.Tests/          (NUnit suite + Lua TAP fixtures)
  samples/
    Tutorial/
  docs/
    manual/                                (historical documentation)
  packaging/
    signing/                               (e.g., `keypair.snk`, nuspecs)
```

Key principles:

- **Single source of truth:** Eliminate `_Projects/*netcore` mirrors by converting main projects to multi-targeting where required.
- **Namespace alignment:** Paths should mirror namespaces (`runtime/NovaSharp.Interpreter/...`), satisfying the new `.editorconfig` rules.
- **Clear ownership:** Runtime vs. debugger vs. tooling content live in dedicated top-level folders, aiding discoverability.
- **Legacy cleanup:** Obsolete clients/tools should be removed promptly; the former `legacy/` quarantine has been retired to keep the tree lean.

## 3. Migration Plan

1. **Inventory & Deletion Pass**
   - ✅ Removed the obsolete `src/legacy` tree (Flash debugger client, NovaSharpPreGen, Lua52 binaries) after confirming no build/test dependencies remained.
   - Migrate any remaining useful scripts into `tooling/` or `docs/` as they surface.

2. **Project Updates**
   - Collapse `_Projects/...netcore` folders by multi-targeting the primary csproj (e.g., `NovaSharp.Interpreter.csproj` → `<TargetFrameworks>netstandard2.1;net8.0</TargetFrameworks>`). ✅ Interpreter and VS Code debugger complete; validate remaining tooling projects.
   - ✅ Renamed `tooling/NovaSharp/NovaSharp.csproj` to `tooling/NovaSharp.Cli/NovaSharp.Cli.csproj` and relocated CLI assets accordingly.
   - Update `NovaSharp.sln`, project `RootNamespace`, and `AssemblyName` values after rename/multi-target work.

3. **Namespace + Usings Sweep**
   - After moves, fix namespaces to reflect the new folder structure (enforced by `.editorconfig`).
   - Leverage analyzers to ensure `using` directives remain inside namespaces.

4. **Tests & Pipelines**
   - Update GitHub workflows, docs, and helper scripts (`scripts/coverage/coverage.ps1`, etc.) to reference the new paths (partial work complete).
  - Share TAP fixtures via `tests/fixtures/` to remove duplication across TAP and NUnit runners.

5. **Documentation Alignment**
   - Refresh `docs/Modernization.md` and `docs/Testing.md` with the new folder map.
   - Add contributor guidance describing where new runtime/tooling/debugger code should live.

6. **Cleanup**
   - Remove stale solution folders once all references are migrated.
   - Ensure `keypair.snk` and other packaging assets are relocated to `packaging/`.

## 4. Open Questions

- Should `NovaSharp.Interpreter.Tests` be further split into `unit/` vs `integration/` folders to ease discovery?
- Are there packaging assets that should move into a dedicated `packaging/` hierarchy alongside signing resources?

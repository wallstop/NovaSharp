# NovaSharp Interpreter Test Suite Reorganization Proposal

## Goals

- Make it obvious which subsystem a test exercises so new contributors can locate examples quickly.
- Keep interpreter, CLI, debugger, and hardwire coverage aligned with the production namespace layout.
- Reduce diffs when files move by establishing a predictable folder + namespace convention.

## Current Layout (Nov 2025)

- `Units/` – flat list of ~1100 NUnit fixtures spanning every runtime subsystem.
- `EndToEnd/` – scenario-style integration tests (debugger attach, coroutine pipelines, JSON).
- `TestMore/` – Lua TAP fixtures synchronized with upstream Lua parity suites.
- `Resources/` – helper Lua scripts and binary payloads used across tests.

Pain points:

- `Units/` mixes interpreter, CLI, tooling, and platform tests in one directory.
- Namespaces generally match `NovaSharp.Interpreter.Tests.Units`, hiding the actual subsystem.
- Adding new categories (e.g., debugger, tooling) requires manual search to avoid clashes.

## Proposed Directory Structure

```
Units/
  Core/                # Execution, compiler, VM, serialization
  DataStructs/         # Stacks, slice, buffer helpers
  DataTypes/           # DynValue, user data descriptors, meta descriptors
  IO/                  # IO/OS modules, BinaryEncoding, stream wrappers
  Interop/             # Hardwire, reflection helpers, userdata policies
  Debug/               # DebugModule, debugger action/state, trace formatting
  CLI/                 # CommandManager, Program, shell commands, REPL loaders
  Tooling/             # Hardwire generators, registry utilities, analyzer scaffolding
  Platforms/           # Platform accessors, Unity/Mono toggles
  Regression/          # Targeted bug reproductions that cross subsystems
```

Namespace convention: `NovaSharp.Interpreter.Tests.Units.<Area>.<FixtureName>`.

## Migration Plan

1. **Create Target Folders**

   - Seed the directory tree with empty subfolders and placeholder `README.md` describing scope.
   - Update `.editorconfig` entries if per-folder rules are required (e.g., Lua fixtures).

1. **Adjust Project Globs**

   - Update `NovaSharp.Interpreter.Tests.csproj` `<Compile Include>` patterns to include the new subdirectories.
   - Ensure test resources continue to embed correctly after the folder moves.

1. **Move Fixtures Gradually**

   - Start with low-churn areas (`IO`, `CLI`) to validate namespace + folder expectations.
   - For each move:
     - Update namespace to `NovaSharp.Interpreter.Tests.Units.<Area>`.
     - Fix any `using` statements impacted by the namespace change.
     - Run `dotnet test` to ensure the suite stays green.
   - Track progress in `PLAN.md` with per-area checkboxes.

1. **Documentation & Tooling**

   - Update `docs/Testing.md` with the new hierarchy and naming guidance.
   - Adjust contributor onboarding snippets (`AGENTS.md`, `CLAUDE.md`) to mention the folder structure.
   - Review `NamespaceAudit` tooling to ensure the new namespaces pass validation.

1. **Follow-up Cleanup**

   - Remove legacy `Units/*.cs` files that were moved once all categories are migrated.
   - Search for lingering `NovaSharp.Interpreter.Tests.Units` namespaces and align them.

## Open Questions

- Should `EndToEnd/` scenarios also adopt subsystem folders (e.g., `Debug/AttachWorkflowTests.cs`) or stay scenario-based?
- Do Lua TAP fixtures (`TestMore/`) require mirroring this hierarchy for parity?
- How do we represent shared helpers (e.g., `TestHelpers.cs`)? Proposal: house them under `Units/_Shared/` with explicit namespaces.

## Next Actions

1. Confirm folder list with maintainers.
1. Prep `NovaSharp.Interpreter.Tests.csproj` glob changes on a branch.
1. Move `CLI` tests as the pilot to validate the namespace strategy.

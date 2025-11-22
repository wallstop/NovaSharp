# Modernization Notes

NovaSharp now targets `netstandard2.1` for all runtime components and `net8.0` for tooling, ensuring out-of-the-box support for:

- Unity 2021+ (including IL2CPP builds)
- Mono and Xamarin successors consuming .NET Standard
- Desktop/server workloads on .NET 6/7/8

## Completed Cleanup

- Removed legacy `.NET 3.5`, `.NET 4.0`, Portable Class Library, Windows Phone, and Silverlight projects.
- Rebuilt the solution around SDK-style projects with explicit `LangVersion=latest` and shared assembly metadata.
- Retired obsolete DevTools (`SynchProjects`, legacy debugger test beds, Silverlight REPL) and Unity/Xamarin sample solutions that locked the repository to older frameworks.
- Deleted the `src/legacy` tree (Flash Flex client, NovaSharpPreGen console, Lua52 binaries, and empty `novasharp_netcore` shell) now that modern debugger/tooling stacks supersede them.
- Converted performance tooling to BenchmarkDotNet, writing results to `docs/Performance.md` without impacting CI.

## Pending Alignments

- Recreate Unity onboarding instructions that reference the consolidated `netstandard2.1` packages.
- Audit any external documentation or samples that still describe the portable40/net35 build chain.
- Validate remote debugger assets on modern browsers now that the Flash-era implementation has been removed.

## Modernization Tooling

- `pwsh ./scripts/modernization/generate-moonsharp-audit.ps1` — regenerates the legacy→NovaSharp issue audit so `docs/modernization/moonsharp-issue-audit.md` stays current. Run this whenever you touch large swaths of code or docs tied to the rename.

Keep this page current when additional modernization steps land (e.g., nullable annotations, trimming support, native AOT testing).

## Reflection Audit (Phase 1 – 2025-11-22)

| Area                                  | Current reflection usage                                                                                                                                  | Next steps                                                                                                                                                         | Status                                                                                                                                                                                                    |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| LuaCompatibilityProfile display names | `CompatibilityVersionTests.DisplayNameHandlesLatestAlias` used `MethodInfo.Invoke` on the private `GetDisplayName` helper to assert the Lua Latest alias. | Expose the helper as an `internal` API so test code can call it without reflection.                                                                                | ✅ Addressed in this pass.                                                                                                                                                                                |
| CLI `Program` tests                   | `ProgramTests` reflects over the private `InterpreterLoop` and `Banner` methods to simulate REPL traffic and banner output.                               | Introduce internal test hooks (e.g., an `internal` `ProgramFacade` or partial methods) so the NUnit suite can exercise the flows without `BindingFlags.NonPublic`. | ✅ Added `Program.RunInterpreterLoopForTests` / `Program.ShowBannerForTests` (exposed via a partial class) and rewired `ProgramTests` to call those helpers directly, eliminating the `MethodInfo` usage. |
| Processor instruction tests           | `ProcessorTests` binds dozens of non-public VM helpers (e.g., `Exec*` methods, instruction decoding) via reflection to assert edge cases.                 | Design an `internal` `ProcessorTestAdapter` that exposes the required hooks through `InternalsVisibleTo`, then migrate the tests off reflection.                   | 🔄 Pending.                                                                                                                                                                                               |
| Descriptor helper tests               | `DescriptorHelpersTests` and related suites reach into private member descriptors via reflection to validate naming/visibility logic.                     | Provide `internal` inspector types or builder APIs that surface the relevant state directly so the tests can drop the direct `BindingFlags.NonPublic` usage.       | 🔄 Pending.                                                                                                                                                                                               |

Tracking: every completed row should be mirrored in `PLAN.md` plus this table so contributors know which areas still depend on reflection.

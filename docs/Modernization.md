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

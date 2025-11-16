# NovaSharp Reflection Usage Audit (Nov 2025)

This catalogue documents where the code base still relies on `System.Reflection` (or similar runtime discovery like `Type.GetType`) so we can decide which call sites should migrate to explicit `internal` APIs vs. remain dynamic.

## How This Was Collected

- Grepped for `System.Reflection`, `Assembly.`, and `Type.GetType` across `src/`.
- Skimmed the surrounding code to understand why the reflection hook exists.
- Grouped the findings by subsystem with an initial recommendation.

Future updates should refresh this file after any large-scale refactor so the modernization plan stays accurate.

## Summary Table

| Area                                          | Representative Files                                                                                    | Purpose                                                                                                                    | Recommendation                                                                                                                 |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Assembly metadata                             | `*/Properties/AssemblyInfo.cs`                                                                          | Standard assembly attributes (`InternalsVisibleTo`, version info).                                                         | ✅ Keep – required by .NET.                                                                                                    |
| Hardwire tooling (source generator precursor) | `tooling/NovaSharp.Hardwire/*`                                                                          | Uses reflection emit (`TypeAttributes`, `Assembly.GetExecutingAssembly`) to discover generators and emit descriptor types. | ⚠️ Replace incrementally with Roslyn generator (see `docs/proposals/roslyn-hardwire-generator.md`).                            |
| CLI command discovery                         | `tooling/NovaSharp.Cli/Commands/CommandManager.cs`                                                      | Reflects over executing assembly to register commands automatically.                                                       | ⚠️ Consider replacing with explicit registry or source-generated map once generator work lands.                                |
| Runtime module registration                   | `runtime/NovaSharp.Interpreter/Modules/ModuleRegister.cs`                                               | Builds module tables by scanning `NovaSharpModule` attributes.                                                             | ⚖️ Leave for now; revisit after generators/internal APIs exist for modules.                                                    |
| User data + interop descriptors               | `runtime/NovaSharp.Interpreter/Interop/*`, `runtime/NovaSharp.Interpreter/DataTypes/UserData.cs`        | Loads CLR metadata (fields, properties, methods) to build descriptor tables.                                               | ⚠️ Long-term goal is to expose internals + generators; short-term keep but prefer `internal` helpers when touch points change. |
| Embedded resource loaders                     | `runtime/NovaSharp.Interpreter/Loaders/EmbeddedResourcesScriptLoader.cs`, test equivalents              | Use `Assembly.GetCallingAssembly()` to locate Lua fixtures.                                                                | ⚖️ Keep; add shims for restricted platforms (already guarded).                                                                 |
| Unity integration                             | `runtime/NovaSharp.Interpreter/Loaders/UnityAssetsScriptLoader.cs`, `Platforms/PlatformAutoDetector.cs` | Runtime detection for Unity/Mono types.                                                                                    | ✅ Keep – unavoidable in sandboxed builds; document in Unity guide.                                                            |
| Debugger web host                             | `debuggers/NovaSharp.RemoteDebugger/*`                                                                  | Loads embedded assets for the remote debugger.                                                                             | ✅ Keep until debugger packaging is refactored.                                                                                |
| Tests                                         | `tests/NovaSharp.Interpreter.Tests/*`                                                                   | Reflection used to probe descriptors, load resources for fixtures.                                                         | ⚖️ Acceptable in tests; no action.                                                                                             |
| Samples/Tutorials                             | `samples/Tutorial/Tutorials/*`                                                                          | Reflection used for demo scenarios.                                                                                        | ✅ Leave as-is; educational code.                                                                                              |

Legend: ✅ keep, ⚖️ monitor, ⚠️ needs follow-up (tracked in `PLAN.md` Milestone).

## Hotspots Requiring Follow-Up

1. **Hardwire generator registry** – depends heavily on `Assembly.GetExecutingAssembly()` to locate generator plugins. Replace with Roslyn generator + explicit metadata.
1. **UserData registration** – `Assembly.GetCallingAssembly()` fallback is still present. When touching these call sites, prefer passing explicit assemblies or exposing internal entry points.
1. **CLI command discovery** – once generators are available, replace the runtime scan with a precomputed command map.
1. **Module/descriptor registration** – long term, mirror the hardwire generator approach to avoid reflection for descriptor creation.

These items feed back into the modernization milestones (Coverage Push, Roslyn hardwire replacement). Update this file when an item is resolved or a new reflection dependency is introduced.

## Action Items

- [ ] File issues/PLAN sub-items for each ⚠️ entry to track removal/migration work.
- [ ] When replacing reflection with `internal` accessors, update the relevant `InternalsVisibleTo` declarations.
- [ ] Reference this document from contributor guides so newcomers know the current policy.

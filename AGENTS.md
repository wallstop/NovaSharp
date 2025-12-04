# Repository Guidelines

## Project Structure & Module Organization
- All runtime code lives under `src/runtime`, with the interpreter in `src/runtime/NovaSharp.Interpreter`.
- Packaging and debugger wrappers reside in `src/tooling/NovaSharp.Cli`, `src/debuggers/NovaSharp.VsCodeDebugger`, and `src/debuggers/NovaSharp.RemoteDebugger`.
- Tooling, samples, and utilities are grouped under `src/tooling`, `src/samples`, and `src/tests`.
- Interpreter test assets (Lua TAP suites, fixtures, shared helpers) live under `src/tests/NovaSharp.Interpreter.Tests`, but the canonical interpreter test runner is the TUnit project at `src/tests/NovaSharp.Interpreter.Tests.TUnit`.
- When adding modules, mirror existing folder placement so docs, tests, and build scripts stay aligned.

## Build, Test, and Development Commands
- Run `dotnet tool restore` once per checkout to install local CLI tools such as CSharpier.
- Build all targets with `dotnet build src\NovaSharp.sln -c Release` for a full verification pass.
- Legacy environments can use `msbuild src\NovaSharp.sln /p:Configuration=Release` when Visual Studio tooling is preferred.
- Execute interpreter tests with `dotnet test src\tests\NovaSharp.Interpreter.Tests.TUnit\NovaSharp.Interpreter.Tests.TUnit.csproj -c Release`.
- Iterate quickly on the interpreter via `dotnet build src\runtime\NovaSharp.Interpreter\NovaSharp.Interpreter.csproj`.
- Generate coverage locally with `./scripts/coverage/coverage.ps1` (produces refreshed artefacts under `artifacts/coverage` and `docs/coverage/latest`). When running on macOS/Linux without PowerShell, detect the absence of `pwsh`/`powershell` and fall back to `bash ./scripts/coverage/coverage.sh` (supports the same flags).
- Codex is allowed to run the coverage helpers (`./scripts/coverage/coverage.ps1` or `.sh`) without additional approval, so feel free to self-drive coverage refreshes while iterating.

## Coding Style & Naming Conventions
- C# uses four-space indentation, braces on new lines, and PascalCase for types and methods.
- Private and internal state prefers `_camelCase` fields; avoid implicit `var` when the type is unclear.
- Preserve Lua fixture indentation at two spaces to ease diffing with upstream scripts.
- Run `dotnet csharpier .` before committing; CSharpier output is the canonical formatting/whitespace for the repo. If `dotnet format` or any other tool disagrees with CSharpier, treat that as a tooling bug to fix (update `.editorconfig`, disable the conflicting rule, etc.) rather than reformatting away from CSharpier style.
- Keep `using` directives minimal, add explicit access modifiers, and discuss any new analyzer suppressions first. `.editorconfig` captures the authoritative formatting/spacing rules—follow it.
- Ensure comments, docs, and diagnostic strings use clear English. Run `python tools/SpellingAudit/spelling_audit.py --write-log spelling_audit.log` when touching text-heavy areas so the spelling audit stays green and `spelling_audit.log` remains up to date.
- Prefer explicit types instead of `var`; only fall back to implicit typing when the language requires it (e.g., anonymous types).
- Prefer exposing internal implementation details via proper `internal` APIs and `InternalsVisibleTo` (amend or create `AssemblyInfo.cs` as needed) for NovaSharp tests, benchmarks, and tooling rather than relying on reflection hacks. Encapsulation still matters, but leaking internals is acceptable when it replaces reflection within this repository.
- BenchmarkDotNet discovery requires benchmark classes to remain `public` and unsealed; when CA1515 warns on those types, add a targeted suppression that explains the BenchmarkDotNet requirement instead of making the classes internal/sealed.
- TUnit (via Microsoft.Testing.Platform) still reflects fixtures to construct them, so keep interpreter/remote-debugger test classes `public` until we build analyzer-enforced factory helpers. Use `Assert.Throws`/`Assert.DoesNotThrow` (or the TUnit equivalents) inside the test body for failure expectations—do not reintroduce `[ExpectedException]` or helper attributes now that the runner executes under Microsoft.Testing.Platform. Until we auto-instantiate internal fixtures, leave the CA1515 module suppression in place rather than flipping fixtures to `internal`.
- Before introducing new reflection or dynamic type discovery, consult `docs/modernization/reflection-audit.md` and document any additions so the modernization plan stays accurate.
- Nullable reference types are not permitted anywhere in this repo (and especially not in `AGENTS.md`/other LLM guides). Do not use `#nullable` pragmas, `string?`/`object?` syntax, `null!` suppressions, or the null-conditional `?.` forms when they target reference types—write explicit guards and keep everything non-nullable.
- **Never** add or preserve `#region` / `#endregion` directives anywhere (runtime, tooling, tests, generated scaffolding, docs). If you encounter one, delete it immediately and rely on clear code or brief summary comments instead. Before submitting work, run `rg -n '#region'` to confirm zero matches; if a generator emits regions, update it or post-process the output so no regions make it into the repo.

## Testing Guidelines
- Interpreter and remote-debugger tests now run exclusively on TUnit (`global::TUnit.Core.Test`). Keep leveraging the async assertion helpers (`await Assert.That(...)`) and data-source attributes described below; do not add new NUnit fixtures.
- Fixtures that mutate shared/static state (anything that calls `UserData.RegisterType`, tweaks the registration policy, etc.) must include `[UserDataIsolation]` so the registry is sandboxed per test. If a suite manipulates `Script.GlobalOptions` (custom converters, platform hooks, etc.), decorate it with `[ScriptGlobalOptionsIsolation]` and drive changes through the shared helpers (`ScriptGlobalOptionsScope`, `ScriptCustomConvertersScope`, `ScriptPlatformScope`, etc.) so those tweaks stay local.
- Arrange new tests in the most descriptive folder (`Units`, `EndToEnd`, or feature-specific subfolders) and ensure class names follow `<Feature>Tests.cs` with colocated Lua fixtures where needed.
- When adding Lua fixtures, provide a mix of small-scoped, mixed-mode, and highly complex scenarios; name the `.lua`/`.t` files descriptively so their focus is obvious at a glance.
- Extend `tests/NovaSharp.Interpreter.Tests.TUnit` when interpreter behaviour changes; `src/tests/NovaSharp.Interpreter.Tests` now serves as a shared asset directory (Lua TAP suites, fixtures, helpers) rather than a standalone NUnit project.
- If you add a new NUnit-based test project elsewhere in the repo, ensure its fixtures flow through the relevant catalog (run `pwsh ./scripts/tests/update-fixture-catalog.ps1` if you truly add NUnit fixtures). Interpreter tests no longer require the catalog—`FixtureCatalogGenerated.cs` now records an empty list to satisfy analyzers.
- Run the detector/console-capture/userdata/try-finally lint guards (`python scripts/lint/check-platform-testhooks.py`, `python scripts/lint/check-console-capture-semaphore.py`, `python scripts/lint/check-userdata-scope-usage.py`, and `python scripts/lint/check-test-finally.py`) before pushing so new tests never reference `PlatformAutoDetector.TestHooks`, call `UserData.RegisterType` outside the approved suites, or reintroduce raw `finally` blocks. CI runs the same scripts via their `scripts/ci/*` wrappers.
- TAP suites that need stdin input must write their temporary files into the suite’s working directory (the directory containing the `.t` script) and drive the scenario through `platform.stdin_helper.run(...)` rather than shelling out to `io.popen`. `TapStdinHelper` resolves relative paths by checking the suite directory, `AppContext.BaseDirectory`, and the current working directory—keep files in those locations so the helper can find them on every platform.
- Console capture/redirection must flow through `ConsoleTestUtilities.WithConsoleCaptureAsync` / `WithConsoleRedirectionAsync`, which already wrap `ConsoleCaptureCoordinator.RunAsync`. Do not instantiate `ConsoleCaptureScope`/`ConsoleRedirectionScope` directly outside the helper or the lint guard will fail.
- `dotnet_diagnostic.CA2007` is enforced as an error. Append `.ConfigureAwait(false)` to every awaited assertion/task (TUnit assertion extensions already expose `ConfigureAwaitFalse()` helpers). No new analyzer suppressions are allowed for CA2007; fix the await instead.
- Write test method names in PascalCase (no underscores); rename legacy cases when you touch them.
- When a test needs access to runtime internals, expose a dedicated `internal` helper (or widen the existing member) and rely on the repo-wide `InternalsVisibleTo` rather than reflection hacks (`BindingFlags`, `MethodInfo.Invoke`, etc.). Only fall back to reflection when the target type lives outside the NovaSharp assemblies or an analyzer explicitly disallows making the member internal.
- Use `Assert.Ignore` only with a linked tracking issue and add coverage for new opcodes, metatables, and debugger paths.
- When a regression test fails, assume the production code is wrong until proven otherwise. Align fixes with the Lua 5.4 specification and keep the test unchanged unless it is demonstrably incorrect.
- If a test exposes a real runtime/spec gap, fix the production implementation (or MoonSharp carry-over design) instead of weakening the test; our target is full Lua 5.4 parity, not test green builds.
- Any failing test must trigger a pass through the official Lua manuals for every version we target (baseline: Lua 5.4.8 at `https://www.lua.org/manual/5.4/`). Document the consulted section/link in the test or PR notes, and update production code and expectations together so NovaSharp stays spec-faithful.
- Spec-driven suites (e.g., string, math, table) must cite the relevant manual section (e.g., “§6.4 String Manipulation”) and assert behaviour matching the canonical Lua interpreter rather than legacy MoonSharp quirks.
- Interpreter migration is complete—do not add new NUnit fixtures under `src/tests/NovaSharp.Interpreter.Tests`. All new coverage belongs in the TUnit project (or the remote-debugger TUnit host).
- When comparing legacy timing data (e.g., remote debugger NUnit → TUnit), use `pwsh ./scripts/tests/compare-test-runtimes.ps1 -Name <scenario> -BaselineArguments @(...) -TUnitArguments @(...)` so reviewers can see the before/after delta.

## TUnit Data-Driven Tests
- Reference guide: https://tunit.dev/llms.txt enumerates every major TUnit capability (assertions, data sources, extension hooks, performance tips). Consult it whenever you need deeper context than this summary.
- TUnit fully supports data-driven tests; lean on `[Arguments(...)]` for literal inputs and switch to `[MethodDataSource]`/`[ClassDataSource]` when you need to materialize complex objects or execute setup code before supplying parameters. See https://tunit.dev/docs/test-authoring/arguments and https://tunit.dev/docs/test-authoring/method-data-source.
- Use `[CombinedDataSources]` when each parameter needs its own attribute (TUnit generates the Cartesian product automatically) or `[MatrixDataSource]` when you want matrix-style combinations declared inline. See https://tunit.dev/docs/test-authoring/combined-data-source and https://tunit.dev/docs/test-authoring/matrix-tests.
- When the built-in sources are not enough, create custom providers via the data-source generator base classes or the nested data-source initialization hooks so complex fixtures and dependency-injected setups can feed tests deterministically. See https://tunit.dev/docs/customization-extensibility/data-source-generators and https://tunit.dev/docs/test-authoring/nested-data-sources.
- Prefer these attributes over manual loops; they emit individual, filterable test cases in Microsoft.Testing.Platform, keep assertion output readable, and integrate cleanly with TUnit’s async `Assert.That` API.

## Commit & Pull Request Guidelines
- Write concise, imperative commit messages such as “Fix parser regression” for consistent history.
- Add subsystem prefixes like `debugger:` when it sharpens context without bloating the subject line.
- Reference tracking issues using `Fixes #ID` so automation closes them when merged.
- Provide PR descriptions summarizing the change, listing executed build/test commands, and attaching UI captures for debugger work.
- Call out breaking API updates prominently and coordinate release notes before merging impactful changes.



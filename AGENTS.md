# Repository Guidelines

## Project Structure & Module Organization
- All runtime code lives under `src/runtime`, with the interpreter in `src/runtime/NovaSharp.Interpreter`.
- Packaging and debugger wrappers reside in `src/tooling/NovaSharp.Cli`, `src/debuggers/NovaSharp.VsCodeDebugger`, and `src/debuggers/NovaSharp.RemoteDebugger`.
- Tooling, samples, and utilities are grouped under `src/tooling`, `src/samples`, and `src/tests`.
- The consolidated NUnit suite lives in `src/tests/NovaSharp.Interpreter.Tests` and drives both local and CI coverage.
- When adding modules, mirror existing folder placement so docs, tests, and build scripts stay aligned.

## Build, Test, and Development Commands
- Run `dotnet tool restore` once per checkout to install local CLI tools such as CSharpier.
- Build all targets with `dotnet build src\NovaSharp.sln -c Release` for a full verification pass.
- Legacy environments can use `msbuild src\NovaSharp.sln /p:Configuration=Release` when Visual Studio tooling is preferred.
- Execute interpreter tests with `dotnet test src\tests\NovaSharp.Interpreter.Tests\NovaSharp.Interpreter.Tests.csproj -c Release --no-build --settings scripts/tests/NovaSharp.Parallel.runsettings`.
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
- NUnit’s discovery still reflects `[TestFixture]` classes directly, so keep interpreter fixtures (and their adapters such as `TestRunner`/`TapRunner`) `public`. Use `Assert.Throws`/`Assert.DoesNotThrow` inside the test body for failure expectations—do not reintroduce `[ExpectedException]` or helper attributes now that the runner executes under NUnit 3. Until we auto-instantiate internal fixtures, leave the CA1515 module suppression in place rather than flipping fixtures to `internal`.
- Before introducing new reflection or dynamic type discovery, consult `docs/modernization/reflection-audit.md` and document any additions so the modernization plan stays accurate.
- Nullable reference types are not permitted anywhere in this repo (and especially not in `AGENTS.md`/other LLM guides). Do not use `#nullable` pragmas, `string?`/`object?` syntax, `null!` suppressions, or the null-conditional `?.` forms when they target reference types—write explicit guards and keep everything non-nullable.
- **Never** add or preserve `#region` / `#endregion` directives anywhere (runtime, tooling, tests, generated scaffolding, docs). If you encounter one, delete it immediately and rely on clear code or brief summary comments instead. Before submitting work, run `rg -n '#region'` to confirm zero matches; if a generator emits regions, update it or post-process the output so no regions make it into the repo.

## Testing Guidelines
- NUnit 3 attributes (`[TestFixture]`, `[Test]`, `[TestCase]`, etc.) drive coverage across interpreter and end-to-end suites; prefer the modern assertion APIs (`Assert.That`, `Assert.Throws`, constraint helpers) instead of legacy attributes.
- Fixtures that mutate shared/static state (anything that calls `UserData.RegisterType`, tweaks the registration policy, etc.) must include `[UserDataIsolation]` so the registry is sandboxed per test, and should run with `[Parallelizable(ParallelScope.Self)]` once isolated. If a suite manipulates `Script.GlobalOptions` (custom converters, platform hooks, etc.), open a scope in `[SetUp]` via `Script.BeginGlobalOptionsScope()` and dispose it in `[TearDown]` so those tweaks stay local. Only fall back to `[NonParallelizable]` when you can’t isolate the shared state yet.
- Arrange new tests in the most descriptive folder (`Units`, `EndToEnd`, or feature-specific subfolders) and ensure class names follow `<Feature>Tests.cs` with colocated Lua fixtures where needed.
- When adding Lua fixtures, provide a mix of small-scoped, mixed-mode, and highly complex scenarios; name the `.lua`/`.t` files descriptively so their focus is obvious at a glance.
- Extend `tests/NovaSharp.Interpreter.Tests` when interpreter behavior changes to keep builds in sync.
- After adding or renaming `[TestFixture]` classes, run `pwsh ./scripts/tests/update-fixture-catalog.ps1` so `FixtureCatalogGenerated.cs` stays in sync and analyzers continue to see every test type.
- Write test method names in PascalCase (no underscores); rename legacy cases when you touch them.
- When a test needs access to runtime internals, expose a dedicated `internal` helper (or widen the existing member) and rely on the repo-wide `InternalsVisibleTo` rather than reflection hacks (`BindingFlags`, `MethodInfo.Invoke`, etc.). Only fall back to reflection when the target type lives outside the NovaSharp assemblies or an analyzer explicitly disallows making the member internal.
- Use `Assert.Ignore` only with a linked tracking issue and add coverage for new opcodes, metatables, and debugger paths.
- When a regression test fails, assume the production code is wrong until proven otherwise. Align fixes with the Lua 5.4 specification and keep the test unchanged unless it is demonstrably incorrect.
- If a test exposes a real runtime/spec gap, fix the production implementation (or MoonSharp carry-over design) instead of weakening the test; our target is full Lua 5.4 parity, not test green builds.
- Any failing test must trigger a pass through the official Lua manuals for every version we target (baseline: Lua 5.4.8 at `https://www.lua.org/manual/5.4/`). Document the consulted section/link in the test or PR notes, and update production code and expectations together so NovaSharp stays spec-faithful.
- Spec-driven suites (e.g., string, math, table) must cite the relevant manual section (e.g., “§6.4 String Manipulation”) and assert behaviour matching the canonical Lua interpreter rather than legacy MoonSharp quirks.
- During the TUnit migration, keep adding interpreter/tooling coverage to the existing NUnit project (`src/tests/NovaSharp.Interpreter.Tests`) unless you are actively porting a fixture called out in `docs/testing/tunit-migration-blueprint.md`. The `NovaSharp.Interpreter.Tests.TUnit` host already exists—run `dotnet test --project src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj -c Release` when wiring the migrated copy—and remote debugger scenarios must be mirrored in both the NUnit suite (`Units/RemoteDebuggerTests.cs`) and the TUnit pilot (`src/tests/NovaSharp.RemoteDebugger.Tests.TUnit`) until the cutover so timing regressions stay visible.
- Anytime you port a fixture or add a new remote-debugger scenario, run `pwsh ./scripts/tests/compare-test-runtimes.ps1 -Name <scenario> -NUnitArguments @(...) -TUnitArguments @(...)` to capture Microsoft.Testing.Platform timing data. Attach the generated JSON/log artefacts to your PR and link them from PLAN.md so reviewers can see the before/after delta.

## Commit & Pull Request Guidelines
- Write concise, imperative commit messages such as “Fix parser regression” for consistent history.
- Add subsystem prefixes like `debugger:` when it sharpens context without bloating the subject line.
- Reference tracking issues using `Fixes #ID` so automation closes them when merged.
- Provide PR descriptions summarizing the change, listing executed build/test commands, and attaching UI captures for debugger work.
- Call out breaking API updates prominently and coordinate release notes before merging impactful changes.



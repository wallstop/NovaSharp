# Repository Guidelines

## Project Structure & Module Organization
- All runtime code lives under `src`, with the interpreter in `src/MoonSharp.Interpreter`.
- Packaging and debugger wrappers reside in `src/MoonSharp`, `src/MoonSharp.VsCodeDebugger`, and `src/MoonSharp.RemoteDebugger`.
- Developer tools, samples, and utilities are grouped under `src/DevTools`, `src/Tutorial`, and `src/TestRunners`.
- Legacy NUnit coverage is maintained in `src/MoonSharp.Interpreter.Tests`; modern .NET Core execution lives in `src/TestRunners/DotNetCoreTestRunner`.
- When adding modules, mirror existing folder placement so docs, tests, and build scripts stay aligned.

## Build, Test, and Development Commands
- Run `dotnet tool restore` once per checkout to install local CLI tools such as CSharpier.
- Build all targets with `dotnet build src\moonsharp.sln -c Release` for a full verification pass.
- Legacy environments can use `msbuild src\moonsharp.sln /p:Configuration=Release` when Visual Studio tooling is preferred.
- Execute interpreter tests with `dotnet test src\TestRunners\DotNetCoreTestRunner\DotNetCoreTestRunner.csproj -c Release`.
- Iterate quickly on the interpreter via `dotnet build src\MoonSharp.Interpreter\_Projects\MoonSharp.Interpreter.netcore\MoonSharp.Interpreter.netcore.csproj`.

## Coding Style & Naming Conventions
- C# uses four-space indentation, braces on new lines, and PascalCase for types and methods.
- Private and internal state prefers `_camelCase` fields; avoid implicit `var` when the type is unclear.
- Preserve Lua fixture indentation at two spaces to ease diffing with upstream scripts.
- Run `dotnet csharpier .` before committing; formatter preferences are configured in `csharpier.json`.
- Keep `using` directives minimal, add explicit access modifiers, and discuss any new analyzer suppressions first.

## Testing Guidelines
- NUnit 2.6 attributes (`[TestFixture]`, `[Test]`) drive coverage across interpreter and end-to-end suites.
- Organize new cases under `Units`, `EndToEnd`, or `TestMore` to match the scope being verified.
- Name test classes `<Feature>Tests.cs` and store Lua fixtures alongside the scenario they exercise.
- Extend both `MoonSharp.Interpreter.Tests` and `DotNetCoreTestRunner` when interpreter behavior changes to keep builds in sync.
- Use `Assert.Ignore` only with a linked tracking issue and add coverage for new opcodes, metatables, and debugger paths.

## Commit & Pull Request Guidelines
- Write concise, imperative commit messages such as “Fix parser regression” for consistent history.
- Add subsystem prefixes like `debugger:` when it sharpens context without bloating the subject line.
- Reference tracking issues using `Fixes #ID` so automation closes them when merged.
- Provide PR descriptions summarizing the change, listing executed build/test commands, and attaching UI captures for debugger work.
- Call out breaking API updates prominently and coordinate release notes before merging impactful changes.

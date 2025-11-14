# Build Helpers

The `scripts/build` folder contains cross-platform wrappers that build the NovaSharp solution and run the interpreter test suite with a single command.

## Scripts
- `build.ps1` — PowerShell entry point that restores local tools (unless skipped), builds `src/NovaSharp.sln`, and optionally executes `dotnet test` for `NovaSharp.Interpreter.Tests`.
- `build.sh` — Bash equivalent for macOS/Linux hosts. Mirrors the PowerShell behaviour and arguments so CI and local workflows stay aligned.

## Common Arguments
- `--configuration <Name>` / `-Configuration <Name>`: Build configuration (default `Release`).
- `--solution <Path>` / `-Solution <Path>`: Solution or project to build (default `src/NovaSharp.sln`).
- `--test-project <Path>` / `-TestProject <Path>`: Test project to execute after building (default `src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj`).
- `--skip-tests` / `-SkipTests`: Build only; do not execute tests.
- `--skip-tool-restore` / `-SkipToolRestore`: Assume `dotnet tool restore` already ran for this checkout.

Both scripts automatically set `DOTNET_ROLL_FORWARD=Major` when the variable is unset so environments with only .NET 9 installed can still run the net8 test runner. Test logs are written to `artifacts/test-results`, matching the CI layout.

## Examples
```powershell
pwsh ./scripts/build/build.ps1
pwsh ./scripts/build/build.ps1 -Configuration Debug -SkipTests
```

```bash
bash ./scripts/build/build.sh
bash ./scripts/build/build.sh --skip-tests --skip-tool-restore
```

Keep the scripts idempotent so they can be used locally and in automation without extra flags.

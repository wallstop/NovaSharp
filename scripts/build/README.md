# Build Helpers

The `scripts/build` folder contains cross-platform wrappers that build the NovaSharp solution and run the interpreter test suite with a single command.

## Quick Build Script (Recommended for Development)

Use `quick.sh` for fast iterative development with incremental compilation:

```bash
# Quick incremental build (interpreter only, ~3-5x faster)
./scripts/build/quick.sh

# Build entire solution
./scripts/build/quick.sh --all

# Clean build from scratch
./scripts/build/quick.sh --clean

# Debug configuration
./scripts/build/quick.sh --debug
```

**Performance features:**

- Builds only the interpreter project by default (faster than full solution)
- Uses parallel compilation (`-m` flag) for multi-core systems
- Attempts incremental build first, falls back to restore if needed
- Suppresses noisy output for cleaner feedback

## CI/Full Build Scripts

- `build.ps1` — PowerShell entry point that restores local tools (unless skipped), builds `src/NovaSharp.sln`, and optionally executes the interpreter TUnit suite.
- `build.sh` — Bash equivalent for macOS/Linux hosts. Mirrors the PowerShell behaviour and arguments so CI and local workflows stay aligned.

### Common Arguments

- `--configuration <Name>` / `-Configuration <Name>`: Build configuration (default `Release`).
- `--solution <Path>` / `-Solution <Path>`: Solution or project to build (default `src/NovaSharp.sln`).
- `--test-project <Path>` / `-TestProject <Path>`: Test project to execute after building (default `src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj`).
- `--skip-tests` / `-SkipTests`: Build only; do not execute tests.
- `--skip-tool-restore` / `-SkipToolRestore`: Assume `dotnet tool restore` already ran for this checkout.

Both scripts automatically set `DOTNET_ROLL_FORWARD=Major` when the variable is unset so environments with only .NET 9 installed can still run the net8 test runner. Test logs are written to `artifacts/test-results`, matching the CI layout.

### Examples

```powershell
pwsh ./scripts/build/build.ps1
pwsh ./scripts/build/build.ps1 -Configuration Debug -SkipTests
```

```bash
bash ./scripts/build/build.sh
bash ./scripts/build/build.sh --skip-tests --skip-tool-restore
```

Keep the scripts idempotent so they can be used locally and in automation without extra flags.

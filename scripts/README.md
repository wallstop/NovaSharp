# Scripts

Helper scripts live under this directory so contributors can find tooling without hunting through the repo root. Subfolders group related commands:

## Quick Start (Development)

For fast iterative development, use the quick build and test scripts:

```bash
# Quick incremental build (interpreter only, ~3-5x faster)
./scripts/build/quick.sh

# Run tests with filtering
./scripts/test/quick.sh Floor              # Methods containing "Floor"
./scripts/test/quick.sh -c MathModule      # Classes containing "MathModule"
./scripts/test/quick.sh -c Math -m Floor   # Combined filter
./scripts/test/quick.sh --no-build Floor   # Skip build step
```

## Folder Index

- `coverage/` — Coverlet + ReportGenerator wrappers (`coverage.ps1` / `coverage.sh`) that build the solution, run the interpreter tests, and publish Markdown/HTML/JSON summaries into `artifacts/coverage` + `docs/coverage/latest`.
- `benchmarks/` — Helpers for running the runtime + comparison BenchmarkDotNet suites (`run-benchmarks.ps1` / `run-benchmarks.sh`), keeping `docs/Performance.md` up to date. CI integration via `.github/workflows/benchmarks.yml` tracks regressions with threshold-based alerting.
- `build/` — Cross-platform build helpers including `quick.sh` for fast development builds and `build.ps1` / `build.sh` for CI.
- `test/` — Quick test runner (`quick.sh`) with filtering support for fast iterative testing.
- `tests/` — Lua specification parity harnesses and test utilities.
- `packaging/` — NuGet and Unity package builders (`build-unity-package.ps1` / `build-unity-package.sh`) that produce UPM-compatible packages. CI integration via `.github/workflows/nuget-publish.yml` handles NuGet.org and GitHub Packages publishing.
- `ci/` — Repository health guards (e.g., README/link enforcement) that run locally or in CI before builds/tests execute.
- `dev/` — Local developer utilities, including the shared pre-commit hook installer/driver that auto-fixes formatting issues before commits.
- `branding/` — Guardrail scripts (e.g., `ensure-novasharp-branding.sh`) that prevent regressions to the legacy brand.
- `lint/` — Static analysis helpers that enforce test isolation patterns (console capture coordination, platform hooks, temp path usage, userdata scoping) and prevent manual `finally` blocks in tests.
- `modernization/` — One-off helpers such as `generate-moonsharp-audit.ps1` used during the modernization campaign.

## Usage Guidelines

1. Run scripts from the repository root so relative paths resolve correctly.
1. Prefer `pwsh` for PowerShell scripts on Windows/Linux/macOS; when PowerShell is unavailable, provide a Bash/Python equivalent and document the fallback in this folder. Python-based utilities rely on `requirements.tooling.txt`, so run `python -m pip install -r requirements.tooling.txt` once per clone.
1. When introducing a new script:
   - Place it in the appropriate subfolder.
   - Add usage, prerequisites, and sample commands to the subfolder README (and link to it from here if needed).
   - Update `docs/Testing.md`, `docs/Modernization.md`, or other relevant guides so end users know the script exists.
1. Keep scripts idempotent and CI-safe—avoid modifying developer machines or requiring elevated privileges.

Need another folder? Create it under `scripts/` with a short README following the conventions above.

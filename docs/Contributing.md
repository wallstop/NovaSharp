# Contributing Guide

This project keeps the build/test tooling and documentation in lockstep. Use this guide as a checklist before opening a pull request.

## Environment & Tooling
- Install the .NET SDK (8.0+). If your machine only has .NET 9, set `DOTNET_ROLL_FORWARD=Major` when running tests/coverage so the net8.0 testhost launches correctly.
- Restore local tools once per checkout:
  ```bash
  dotnet tool restore
  ```

## Build & Test Commands
- Full solution build: `dotnet build src/NovaSharp.sln -c Release`
- Interpreter-only build: `dotnet build src/runtime/NovaSharp.Interpreter/NovaSharp.Interpreter.csproj`
- Interpreter tests (Release):  
  `dotnet test src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj -c Release --logger "trx;LogFileName=NovaSharpTests.trx"`

## Coverage
- PowerShell (Windows/Linux/macOS with PowerShell):
  ```powershell
  DOTNET_ROLL_FORWARD=Major pwsh ./scripts/coverage/coverage.ps1
  ```
- Bash fallback (macOS/Linux without PowerShell):
  ```bash
  DOTNET_ROLL_FORWARD=Major bash ./scripts/coverage/coverage.sh
  ```
- Coverage artefacts land in `artifacts/coverage/` and `docs/coverage/latest/`. Update `PLAN.md` / docs as needed when the baseline changes.

## Branding & Namespace Guards
- Reject accidental regressions to the legacy MoonSharp brand:
  ```bash
  ./scripts/branding/ensure-novasharp-branding.sh
  ```
- Keep namespaces aligned with the directory structure before pushing:
  ```bash
  python3 tools/NamespaceAudit/namespace_audit.py
  ```
  Fix mismatches (or update the allowlist intentionally) until the script passes.

## Updating Scripts & Docs
- New helper script? Place it under the appropriate `scripts/<area>/` folder, update `scripts/README.md` and the subfolder README, then mention it in `docs/README.md` if it affects contributors.
- New or updated documentation? Link it from `docs/README.md` and the repo `README.md` (Additional Documentation section). The PR template checks for this.

## Pull Request Checklist
Before opening a PR:
1. Run `dotnet build src/NovaSharp.sln -c Release`.
2. Run the interpreter tests (see above).
3. Run coverage if your change affects runtime/tooling behaviour.
4. Run the branding + namespace scripts.
5. Update relevant docs (`docs/README.md`, `docs/Testing.md`, feature-specific guides).
6. Update `PLAN.md` if you progressed a milestone item.

Following these steps keeps CI green and makes reviews smoother. Thanks for contributing!

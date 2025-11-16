# Contributing Guide

This project keeps the build/test tooling and documentation in lockstep. Use this guide as a checklist before opening a pull request.

## Environment & Tooling
- Install the .NET SDK (8.0+). If your machine only has .NET 9, set `DOTNET_ROLL_FORWARD=Major` when running tests/coverage so the net8.0 testhost launches correctly.
- Install Python 3.10+ (CI uses 3.12) and restore the shared tooling dependencies once per clone:
  ```bash
  python -m pip install -r requirements.tooling.txt
  ```
- Restore local dotnet tools once per checkout:
  ```bash
  dotnet tool restore
  ```
- Enable the shared pre-commit hook so auto-fixes run before every commit:
  ```bash
  bash ./scripts/dev/install-hooks.sh
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
- Reject accidental regressions to the legacy brand:
  ```bash
  ./scripts/branding/ensure-novasharp-branding.sh
  ```
- Keep namespaces aligned with the directory structure before pushing:
  ```bash
  python3 tools/NamespaceAudit/namespace_audit.py
  ```
  Fix mismatches (or update the allowlist intentionally) until the script passes.

## Formatting & Pre-commit Hooks
- The shared pre-commit hook (`bash ./scripts/dev/pre-commit.sh`) runs automatically once `scripts/dev/install-hooks.sh` has been executed and the Python tooling requirements are installed. It performs the following before each commit:
  - `dotnet csharpier .` (auto-fix all C# files).
  - `python scripts/ci/format_markdown.py --fix --files <staged markdown>` to keep Markdown deterministic.
  - `python scripts/ci/check_markdown_links.py --files <staged markdown>` to ensure links stay healthy.
  - Restages the results so your commit includes the auto-fixes.
- Run the hook manually if you need to double-check formatting outside of a commit:
  ```bash
  bash ./scripts/dev/pre-commit.sh
  ```
- CI enforces the same rules via `scripts/ci/check-csharpier.sh` and `scripts/ci/check-markdown.sh`; when it fails on a repo-owned PR branch, the pipeline publishes an auto-fix PR (generated from `scripts/ci/apply-formatters.sh`) that you can merge into your branch.

## Updating Scripts & Docs
- New helper script? Place it under the appropriate `scripts/<area>/` folder, update `scripts/README.md` and the subfolder README, then mention it in `docs/README.md` if it affects contributors.
- New or updated documentation? Link it from `docs/README.md` and the repo `README.md` (Additional Documentation section). The PR template checks for this.

## Pull Request Checklist
Before opening a PR:
1. Run `dotnet build src/NovaSharp.sln -c Release`.
2. Run the interpreter tests (see above).
3. Run coverage if your change affects runtime/tooling behaviour.
4. Run the branding + namespace scripts.
5. Ensure formatting hooks have run (or run `dotnet csharpier .` + `python scripts/ci/format_markdown.py --check --all` + `bash ./scripts/ci/check-markdown.sh` manually) so CI doesn't reject style issues.
6. Update relevant docs (`docs/README.md`, `docs/Testing.md`, feature-specific guides).
7. Update `PLAN.md` if you progressed a milestone item.

Following these steps keeps CI green and makes reviews smoother. Thanks for contributing!

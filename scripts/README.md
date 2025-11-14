# Scripts

Helper scripts live under this directory so contributors can find tooling without hunting through the repo root. Subfolders group related commands:

- `coverage/` — Coverlet + ReportGenerator wrappers (`coverage.ps1` / `coverage.sh`) that build the solution, run the interpreter tests, and publish Markdown/HTML/JSON summaries into `artifacts/coverage` + `docs/coverage/latest`.
- `build/` — Cross-platform build helpers (`build.ps1` / `build.sh`) that restore local tools, build `src/NovaSharp.sln`, and (by default) execute the interpreter tests into `artifacts/test-results`.
- `ci/` — Repository health guards (e.g., README/link enforcement) that run locally or in CI before builds/tests execute.
- `dev/` — Local developer utilities, including the shared pre-commit hook installer/driver that auto-fixes formatting issues before commits.
- `branding/` — Guardrail scripts (e.g., `ensure-novasharp-branding.sh`) that prevent regressions to legacy MoonSharp names.
- `modernization/` — One-off helpers such as `generate-moonsharp-audit.ps1` used during the modernization campaign.
- Additional folders (e.g., `build/`, `dev/`, `ci/`) should follow the same pattern: descriptive folder name, script-specific README, and cross-platform shims when practical.

## Usage Guidelines

1. Run scripts from the repository root so relative paths resolve correctly.
2. Prefer `pwsh` for PowerShell scripts on Windows/Linux/macOS; when PowerShell is unavailable, provide a Bash/Python equivalent and document the fallback in this folder. Python-based utilities rely on `requirements.tooling.txt`, so run `python -m pip install -r requirements.tooling.txt` once per clone.
3. When introducing a new script:
   - Place it in the appropriate subfolder.
   - Add usage, prerequisites, and sample commands to the subfolder README (and link to it from here if needed).
   - Update `docs/Testing.md`, `docs/Modernization.md`, or other relevant guides so end users know the script exists.
4. Keep scripts idempotent and CI-safe—avoid modifying developer machines or requiring elevated privileges.

Need another folder? Create it under `scripts/` with a short README following the conventions above.

# Developer Hooks

The scripts in this folder keep local commits tidy by auto-fixing formatting issues and catching common mistakes before they make it to CI.

## Scripts

- `pre-commit.sh` — Comprehensive pre-commit hook that runs auto-fixes and validation checks (see details below).
- `install-hooks.sh` — Configures `core.hooksPath` to `.githooks` so Git invokes the shared `pre-commit` hook from this repository.

## Usage

```bash
# One-time setup per clone
bash ./scripts/dev/install-hooks.sh

# Optional: run manually outside of Git hooks
bash ./scripts/dev/pre-commit.sh
```

## Pre-commit Hook Details

The `pre-commit.sh` script runs two phases of checks:

### Phase 1: Auto-fix / Auto-update Hooks

These hooks automatically fix issues and restage the corrected files:

| Hook                    | Description                                                                   | Auto-stages                           |
| ----------------------- | ----------------------------------------------------------------------------- | ------------------------------------- |
| **CSharpier**           | Formats all C# files in the repository                                        | `*.cs`                                |
| **Markdown Format**     | Applies mdformat with GFM extension to staged `.md` files                     | `*.md`                                |
| **Markdown Links**      | Validates links in staged Markdown files (fails if broken)                    | —                                     |
| **Documentation Audit** | Refreshes `docs/audits/documentation_audit.log` with missing XML doc comments | `docs/audits/documentation_audit.log` |
| **Naming Audit**        | Refreshes `docs/audits/naming_audit.log` with naming convention violations    | `docs/audits/naming_audit.log`        |
| **Spelling Audit**      | Refreshes `docs/audits/spelling_audit.log` with codespell findings            | `docs/audits/spelling_audit.log`      |
| **Fixture Catalog**     | Regenerates NUnit fixture catalog                                             | `FixtureCatalogGenerated.cs`          |

### Phase 2: Validation Hooks

These hooks check for issues and **fail the commit** if problems are found:

| Hook                | Description                                               | Trigger             |
| ------------------- | --------------------------------------------------------- | ------------------- |
| **Branding Check**  | Prevents legacy "MoonSharp" identifiers in staged content | All staged files    |
| **Namespace Audit** | Validates declared namespaces match directory layout      | All `src/**/*.cs`   |
| **Test Lint Suite** | Enforces test infrastructure patterns (see below)         | `src/tests/**/*.cs` |

#### Test Lint Suite

When test files are staged, these additional checks run:

- **Temp Path Usage** — Tests must use `TempFileScope`/`TempDirectoryScope` instead of `Path.GetTempPath()`
- **UserData Scope** — Tests must use `UserDataRegistrationScope` instead of direct `UserData.RegisterType()`
- **Console Capture** — Tests must use `ConsoleTestUtilities` helpers instead of direct scope instantiation (requires `rg`)
- **Finally Blocks** — Tests should use scope helpers instead of manual `try/finally` cleanup (requires `rg`)

## Dependencies

### Required

- **Git** — For staging and hook integration
- **.NET SDK** — For CSharpier (`dotnet tool restore`)
- **PowerShell** — For fixture catalog regeneration (`pwsh` or Windows PowerShell)
- **Python 3.10+** — For Markdown formatting, audits, and lint scripts

### Python Packages

Install with: `python -m pip install -r requirements.tooling.txt`

- `mdformat==1.0.0` — Markdown formatting
- `mdformat-gfm==1.0.0` — GitHub Flavored Markdown support
- `markdown-it-py==4.0.0` — Markdown link parsing
- `requests==2.32.5` — HTTP link validation
- `codespell==2.3.0` — Spelling audit

### Optional

- **ripgrep (`rg`)** — Required for console-capture and finally-block checks. If not installed, these checks are skipped with a warning.

## Troubleshooting

### Hook reports link failures

Fix the referenced URLs in your Markdown files before committing. The link checker validates both relative paths and HTTP(S) URLs.

### Spelling audit fails

Either fix the typos or add false positives to `tools/SpellingAudit/allowlist.txt` (one word per line).

### Branding check fails

Replace "MoonSharp" with "NovaSharp" in your changes. If the reference is intentional (e.g., performance comparison docs), add the path to the allowlist in `scripts/branding/ensure-novasharp-branding.sh`.

### Test lint checks fail

Follow the guidance in the error message. Generally:

- Use scope helpers (`TempFileScope`, `UserDataRegistrationScope`, etc.) instead of direct API calls
- Avoid `finally` blocks in tests — use `IDisposable` scopes instead

### Skipping hooks temporarily

To bypass all hooks for a single commit (not recommended):

```bash
git commit --no-verify -m "emergency fix"
```

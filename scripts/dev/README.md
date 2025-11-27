# Developer Hooks

The scripts in this folder keep local commits tidy by auto-fixing formatting issues before they make it to CI.

## Scripts

- `pre-commit.sh` — Formats staged C# files with CSharpier, applies Markdown formatting fixes, re-validates Markdown link targets for staged files, refreshes the documentation/naming audit logs, and regenerates the NUnit fixture catalog so `FixtureCatalogGenerated.cs` stays in sync. The script uses POSIX `sh`, so it runs inside Git's default hook runner on Windows without needing WSL or MSYS beyond what Git already ships. PowerShell (`pwsh` or Windows PowerShell) must be available on `PATH` so the fixture catalog script can run.
- `install-hooks.sh` — Configures `core.hooksPath` to `.githooks` so Git invokes the shared `pre-commit` hook from this repository.

## Usage

```bash
# One-time setup per clone
bash ./scripts/dev/install-hooks.sh

# Optional: run manually outside of Git hooks
bash ./scripts/dev/pre-commit.sh
```

The hook restores dotnet tools before running CSharpier and expects the Python dependencies from `requirements.tooling.txt` to be installed (run `python -m pip install -r requirements.tooling.txt` once per clone or virtual environment). If the hook reports link failures, fix the referenced URLs before committing.

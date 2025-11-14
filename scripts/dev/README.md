# Developer Hooks

The scripts in this folder keep local commits tidy by auto-fixing formatting issues before they make it to CI.

## Scripts

- `pre-commit.sh` — Runs CSharpier across the repo, applies Markdown formatting fixes, re-validates Markdown link targets for staged files, and stages any resulting changes so the commit includes the auto-fixes.
- `install-hooks.sh` — Configures `core.hooksPath` to `.githooks` so Git invokes the shared `pre-commit` hook from this repository.

## Usage

```bash
# One-time setup per clone
bash ./scripts/dev/install-hooks.sh

# Optional: run manually outside of Git hooks
bash ./scripts/dev/pre-commit.sh
```

The hook restores dotnet tools before running CSharpier and expects the Python dependencies from `requirements.tooling.txt` to be installed (run `python -m pip install -r requirements.tooling.txt` once per clone or virtual environment). If the hook reports link failures, fix the referenced URLs before committing.

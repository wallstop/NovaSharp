# CI Guards

Scripts in this folder run before the main build/test stages (either locally or in CI) to keep the repository documentation aligned with code changes.

## Scripts

- `ensure-readme-updates.sh` — Verifies that pull requests adding new helper scripts also update `scripts/README.md` and the corresponding subfolder README, and that new Markdown files under `docs/` are linked from `docs/README.md`. The script compares the current HEAD against a configurable base (via `NOVASHARP_BASE_REF`, defaulting to `HEAD^`) and fails with actionable guidance when documentation updates are missing.
- `check-markdown.sh` — Wraps the Python format/link scripts so CI (and local runs) only lint Markdown files touched by the current change.
- `check-csharpier.sh` — Runs `dotnet csharpier --check .` to guarantee CSharpier formatting passes without needing to scan each project manually.
- `apply-formatters.sh` — Applies repository-wide fixes (`dotnet csharpier .` + Python-based `format_markdown.py`) and is used by automation to prepare auto-fix branches when linting fails on pull requests.
- `format_markdown.py` — Uses `mdformat` (via `mdformat-gfm`) to format Markdown deterministically. Supports `--check` and `--fix` modes plus file-scoped or repo-wide execution.
- `check_markdown_links.py` — Parses Markdown via `markdown-it-py` and validates both HTTP(S) and relative links with deterministic timeouts/retries defined in `.markdown-link-check.json`.
- `check-fixture-catalog.ps1` — Regenerates the NUnit fixture catalog (via `scripts/tests/update-fixture-catalog.ps1`) and fails if `FixtureCatalogGenerated.cs` changes, ensuring contributors rerun the generator when fixtures move.
- `check-platform-testhooks.sh` — Runs `scripts/lint/check-platform-testhooks.py` to ensure no new files reference `PlatformAutoDetector.TestHooks` directly; detector overrides must go through the shared scope helpers tracked in PLAN.md.
- `check-console-capture-semaphore.sh` — Runs `scripts/lint/check-console-capture-semaphore.py`, which rejects references to `ConsoleCaptureCoordinator.Semaphore` outside the coordinator helper so tests keep using the `RunAsync` abstraction.

## Usage

```bash
NOVASHARP_BASE_REF=origin/master bash ./scripts/ci/ensure-readme-updates.sh
```

- When running in GitHub Actions, the workflow supplies `NOVASHARP_BASE_REF` automatically so the script diff uses the merge base for the PR (or `HEAD^` on direct pushes).
- Run the script locally before sending a PR if you are adding new helper scripts or Markdown guides to make sure the documentation index stays in sync.

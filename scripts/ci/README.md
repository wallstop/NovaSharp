# CI Guards

Scripts in this folder run before the main build/test stages (either locally or in CI) to keep the repository documentation aligned with code changes.

## Scripts

- `ensure-readme-updates.sh` — Verifies that pull requests adding new helper scripts also update `scripts/README.md` and the corresponding subfolder README, and that new Markdown files under `docs/` are linked from `docs/README.md`. The script compares the current HEAD against a configurable base (via `NOVASHARP_BASE_REF`, defaulting to `HEAD^`) and fails with actionable guidance when documentation updates are missing.
- `check-markdown.sh` — Wraps the Python format/link scripts so CI (and local runs) only lint Markdown files touched by the current change. It also runs the two Markdown tooling test suites and the repository-wide `check_jekyll_liquid.py` guard, which are deliberately *not* diff-scoped.
- `check_jekyll_liquid.py` — Fails when any Markdown file GitHub Pages would render contains Liquid syntax that aborts the site build. Pages serves this repository through the `github-pages` gem, whose `jekyll-optional-front-matter` plugin turns every published Markdown file into a Jekyll page, so a Lua nested table constructor written without inner spaces reads as an unterminated Liquid variable and takes the whole site down. The check models Liquid's fatal cases — unterminated variable and tag delimiters, unresolvable tag names, block-only tags outside their block (an orphan `end*`, `else`, `elsif`, or `when`), a closer that does not match the innermost open block, and any block left unclosed — and was validated case-by-case against a real `github-pages` v232 build, one build per case, comparing the guard's verdict to Jekyll's exit code. It is a *syntax* guard: resource resolution (`{% include missing.html %}`, `{% link missing.md %}`) also aborts a build but is out of scope, and that gap is pinned as a test rather than left silent. It derives its scan set from `_config.yml`'s `exclude` list, so the guard covers exactly what Pages publishes; excluding a path there removes it from both at once.
- `test_check_jekyll_liquid.py` — Unit tests for `check_jekyll_liquid.py`, including a repository-wide assertion that the published site still parses.
- `test_format_markdown.py` — Unit tests for `format_markdown.py`, covering YAML front-matter preservation.
- `check-csharpier.sh` — Restores local .NET tools and runs `dotnet tool run csharpier check .` to guarantee CSharpier formatting passes without needing to scan each project manually.
- `apply-formatters.sh` — Applies repository-wide fixes (`dotnet tool run csharpier format .` + Python-based `format_markdown.py`) and is used by automation to prepare auto-fix branches when linting fails on pull requests.
- `check-tooling-consistency.sh` — Runs `scripts/lint/check-tooling-consistency.py` to keep devcontainer SDK packages, local .NET tool usage, non-root latest-tag npm tools, GitHub MCP configurations, artifact cleanup, and hook restore behavior aligned.
- `../lint/test-devcontainer-lifecycle.sh` — Uses isolated fake npm/CLI commands and filesystem timestamps to regression-test devcontainer install, offline fallback, permission, and artifact-retention behavior; it runs through the tooling-consistency check.
- `../lint/test-devcontainer-build-cache.sh` — Uses a primed minimal Docker build to prove that `--no-cache` bypasses cached steps; it runs through the tooling-consistency check when Docker is available.
- `format_markdown.py` — Uses `mdformat` (via `mdformat-gfm`) to format Markdown deterministically. Supports `--check` and `--fix` modes plus file-scoped or repo-wide execution.
- `check_markdown_links.py` — Parses Markdown via `markdown-it-py` and validates both HTTP(S) and relative links with deterministic timeouts/retries defined in `.markdown-link-check.json`.
- `check-fixture-catalog.ps1` — Regenerates the NUnit fixture catalog (via `scripts/tests/update-fixture-catalog.ps1`) and fails if `FixtureCatalogGenerated.cs` changes, ensuring contributors rerun the generator when fixtures move.
- `check-platform-testhooks.sh` — Runs `scripts/lint/check-platform-testhooks.py` to ensure no new files reference `PlatformAutoDetector.TestHooks` directly; detector overrides must go through the shared scope helpers documented in `docs/Testing.md`.
- `check-console-capture-semaphore.sh` — Runs `scripts/lint/check-console-capture-semaphore.py`, which rejects references to `ConsoleCaptureCoordinator.Semaphore` outside the coordinator helper and forbids direct instantiation of `ConsoleCaptureScope`/`ConsoleRedirectionScope` so tests keep using the `ConsoleTestUtilities` helpers.
- `check-userdata-scope.sh` — Runs `scripts/lint/check-userdata-scope-usage.py`, failing when tests call `UserData.RegisterType`/`UserData.UnregisterType` directly outside the approved isolation suites; use `UserDataRegistrationScope` instead.
- `check-test-finally.sh` — Runs `scripts/lint/check-test-finally.py` to ensure tests continue using the shared cleanup scopes instead of reintroducing manual `try`/`finally` blocks.
- `check-temp-path-usage.sh` — Runs `scripts/lint/check-temp-path-usage.py`, which flags any new references to `Path.GetTempPath()` inside the test tree so contributors keep using `TempFileScope`/`TempDirectoryScope` for cleanup.
- `check-shell-executable.sh` — Runs `scripts/lint/check-shell-executable.py`, which fails when any `.sh` file in the repository is missing the executable bit in git. This prevents CI failures from permission denied errors on Linux/macOS runners.
- `check-vm-hotpath-allocations.sh` — Runs `scripts/lint/check-vm-hotpath-allocations.py`, rejecting new non-allowlisted allocations in VM opcode and Lua-call hot paths.

## Usage

```bash
NOVASHARP_BASE_REF=origin/main bash ./scripts/ci/ensure-readme-updates.sh
./scripts/ci/check-vm-hotpath-allocations.sh
```

- When running in GitHub Actions, the workflow supplies `NOVASHARP_BASE_REF` automatically so the script diff uses the merge base for the PR (or `HEAD^` on direct pushes).
- Run the script locally before sending a PR if you are adding new helper scripts or Markdown guides to make sure the documentation index stays in sync.

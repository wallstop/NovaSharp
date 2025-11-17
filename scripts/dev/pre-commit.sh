#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "pre-commit hook must run inside the repo." >&2
  exit 1
fi

cd "$repo_root"

echo "[pre-commit] Restoring dotnet tools (CSharpier)..."
dotnet tool restore >/dev/null

echo "[pre-commit] Running CSharpier across the entire repository..."
dotnet tool run csharpier format . >/dev/null

mapfile -t staged_md < <(git diff --cached --name-only --diff-filter=ACM -- '*.md' || true)
if [[ ${#staged_md[@]} -gt 0 ]]; then
  PYTHON_BIN="${PYTHON:-python3}"
  echo "[pre-commit] Formatting staged Markdown files..."
  "$PYTHON_BIN" scripts/ci/format_markdown.py --fix --files "${staged_md[@]}"

  echo "[pre-commit] Checking Markdown links for staged files..."
  "$PYTHON_BIN" scripts/ci/check_markdown_links.py --files "${staged_md[@]}"
else
  echo "[pre-commit] No staged Markdown files detected; skipping Markdown checks."
fi

echo "[pre-commit] Staging auto-fixes..."
git add -A

echo "[pre-commit] Completed successfully."

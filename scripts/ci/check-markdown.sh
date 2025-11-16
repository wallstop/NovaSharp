#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi
cd "$repo_root"

base_ref="${NOVASHARP_BASE_REF:-}"
if [[ -z "$base_ref" ]]; then
  if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
    base_ref="HEAD^"
  else
    base_ref="$(git rev-parse HEAD)"
  fi
elif ! git rev-parse --verify "$base_ref" >/dev/null 2>&1; then
  echo "Base ref $base_ref not found; defaulting to HEAD^." >&2
  if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
    base_ref="HEAD^"
  else
    base_ref="$(git rev-parse HEAD)"
  fi
fi

mapfile -t md_files < <(
  git diff --name-only "$base_ref"...HEAD -- '*.md' 2>/dev/null || \
  git diff --name-only "$base_ref" HEAD -- '*.md'
)

if [[ ${#md_files[@]} -eq 0 ]]; then
  echo "No Markdown changes detected against $base_ref; skipping lint."
  exit 0
fi

PYTHON_BIN="${PYTHON:-python3}"

echo "Linting Markdown files changed since $base_ref:"
printf '  %s\n' "${md_files[@]}"

"$PYTHON_BIN" scripts/ci/format_markdown.py --check --files "${md_files[@]}"
"$PYTHON_BIN" scripts/ci/check_markdown_links.py --files "${md_files[@]}"

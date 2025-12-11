#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi
cd "$repo_root"

detect_base_ref() {
  local remote_head upstream_ref

  if [[ -n "${NOVASHARP_BASE_REF:-}" ]]; then
    printf '%s\n' "$NOVASHARP_BASE_REF"
    return
  fi

  upstream_ref="$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null || true)"
  if [[ -n "$upstream_ref" ]]; then
    printf '%s\n' "$upstream_ref"
    return
  fi

  if git symbolic-ref --quiet refs/remotes/origin/HEAD >/dev/null 2>&1; then
    remote_head="$(git symbolic-ref refs/remotes/origin/HEAD)"
    if [[ -n "$remote_head" ]]; then
      printf '%s\n' "${remote_head#refs/remotes/}"
      return
    fi
  fi

  if git show-ref --verify --quiet refs/remotes/origin/main; then
    printf '%s\n' "origin/main"
    return
  fi

  if git show-ref --verify --quiet refs/remotes/origin/master; then
    printf '%s\n' "origin/master"
    return
  fi

  if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
    git rev-parse HEAD^
  else
    git rev-parse HEAD
  fi
}

base_ref="$(detect_base_ref)"

if ! git rev-parse --verify "$base_ref" >/dev/null 2>&1; then
  echo "Base ref $base_ref not found; defaulting to HEAD^." >&2
  if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
    base_ref="HEAD^"
  else
    base_ref="$(git rev-parse HEAD)"
  fi
fi

mapfile -t md_files < <(
  (git diff --name-only "$base_ref"...HEAD -- '*.md' 2>/dev/null || \
  git diff --name-only "$base_ref" HEAD -- '*.md') | grep -v '^progress/'
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

#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "Run this script from inside the NovaSharp repository." >&2
  exit 1
fi

hooks_dir="$repo_root/.githooks"
mkdir -p "$hooks_dir"
chmod +x "$hooks_dir"/* 2>/dev/null || true

(
  cd "$repo_root"
  git config core.hooksPath ".githooks"
)

echo "Git hooks installed (core.hooksPath=.githooks)."

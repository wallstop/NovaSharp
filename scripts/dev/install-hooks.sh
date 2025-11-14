#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
  echo "Run this script from inside the NovaSharp repository." >&2
  exit 1
fi

hooks_path="$repo_root/.githooks"
mkdir -p "$hooks_path"
chmod +x "$hooks_path"/* 2>/dev/null || true

git config core.hooksPath "$hooks_path"
echo "Git hooks installed (core.hooksPath=$hooks_path)."

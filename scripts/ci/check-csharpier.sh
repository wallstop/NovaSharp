#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi
cd "$repo_root"

dotnet tool restore >/dev/null
if ! dotnet csharpier --check .; then
  echo "CSharpier formatting issues detected. Run 'dotnet csharpier .' to fix." >&2
  exit 1
fi

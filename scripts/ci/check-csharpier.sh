#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
cd "$repo_root"

dotnet tool restore >/dev/null
if ! dotnet csharpier --check .; then
  echo "CSharpier formatting issues detected. Run 'dotnet csharpier .' to fix." >&2
  exit 1
fi

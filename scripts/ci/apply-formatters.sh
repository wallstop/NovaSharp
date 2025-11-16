#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi
cd "$repo_root"

dotnet tool restore >/dev/null

echo "[autofix] Running CSharpier..."
dotnet tool run csharpier format . >/dev/null

PYTHON_BIN="${PYTHON:-python3}"

echo "[autofix] Formatting Markdown across the repo..."
"$PYTHON_BIN" scripts/ci/format_markdown.py --fix --all

echo "[autofix] Verifying Markdown formatting after fixes..."
"$PYTHON_BIN" scripts/ci/format_markdown.py --check --all

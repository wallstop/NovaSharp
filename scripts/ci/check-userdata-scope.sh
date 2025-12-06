#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
python "$repo_root/scripts/lint/check-userdata-scope-usage.py"

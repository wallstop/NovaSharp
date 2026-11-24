#!/usr/bin/env bash
# Refresh moving CLI tools and prune expired generated output on every start.

set -euo pipefail

workspace_dir="${1:-$(pwd)}"
retention_days="${NOVA_ARTIFACT_RETENTION_DAYS:-7}"

cd "${workspace_dir}"
bash .devcontainer/ensure-nuget-cache.sh
bash .devcontainer/install-npm-tools.sh --offline-ok
bash .devcontainer/ai-backends.sh install
bash .devcontainer/cleanup-artifacts.sh "${workspace_dir}" --older-than-days "${retention_days}"

echo "✅ NovaSharp dev container ready."

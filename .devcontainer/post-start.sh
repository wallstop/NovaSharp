#!/usr/bin/env bash
# Refresh moving CLI tools and prune expired generated output on every start.

set -euo pipefail

workspace_dir="${1:-$(pwd)}"
retention_days="${NOVA_ARTIFACT_RETENTION_DAYS:-7}"

cd "${workspace_dir}"
NOVA_NPM_INSTALL_ATTEMPTS="${NOVA_NPM_START_ATTEMPTS:-2}" \
    NPM_CONFIG_FETCH_RETRIES="${NOVA_NPM_START_FETCH_RETRIES:-1}" \
    NPM_CONFIG_FETCH_TIMEOUT="${NOVA_NPM_START_FETCH_TIMEOUT_MS:-10000}" \
    bash .devcontainer/install-npm-tools.sh --offline-ok
bash .devcontainer/cleanup-artifacts.sh "${workspace_dir}" --older-than-days "${retention_days}"

echo "✅ NovaSharp dev container ready."

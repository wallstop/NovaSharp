#!/usr/bin/env bash
# Update-content script for NovaSharp dev container.
# Runs after git pull/clone operations and retries restore on transient failures.

set -euo pipefail

WORKSPACE_DIR="${1:-$(pwd)}"
if [ ! -d "${WORKSPACE_DIR}" ]; then
    echo "❌ Workspace not found: ${WORKSPACE_DIR}"
    exit 1
fi

run_with_retries() {
    local max_attempts="$1"
    shift

    local attempt=1
    local delay_seconds=2
    while true; do
        if "$@"; then
            return 0
        else
            local exit_code=$?
        fi

        if [ "${attempt}" -ge "${max_attempts}" ]; then
            return "${exit_code}"
        fi

        local jitter=$((RANDOM % 3))
        local sleep_seconds=$((delay_seconds + jitter))
        local next_attempt=$((attempt + 1))
        echo "Retry ${next_attempt}/${max_attempts} in ${sleep_seconds}s: $*"
        sleep "${sleep_seconds}"
        attempt="${next_attempt}"

        if [ "${delay_seconds}" -lt 30 ]; then
            delay_seconds=$((delay_seconds * 2))
            if [ "${delay_seconds}" -gt 30 ]; then
                delay_seconds=30
            fi
        fi
    done
}

cd "${WORKSPACE_DIR}"
run_with_retries 5 dotnet tool restore --verbosity minimal
run_with_retries 5 dotnet restore src/NovaSharp.sln --verbosity minimal

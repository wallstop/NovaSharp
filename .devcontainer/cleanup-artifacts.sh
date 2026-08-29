#!/usr/bin/env bash
# Bound ignored build/test output without touching source or dependency caches.

set -euo pipefail

usage() {
    echo "Usage: $0 [WORKSPACE] (--all | --older-than-days DAYS)" >&2
}

workspace_dir="${1:-$(pwd)}"
if [ "$#" -gt 0 ]; then
    shift
fi

cleanup_mode=""
retention_days=""
cutoff_reference=""
build_directory_list=""
recent_build_output=""

cleanup_temp_files() {
    local temporary_path
    for temporary_path in "${cutoff_reference}" "${build_directory_list}" "${recent_build_output}"; do
        if [ -n "${temporary_path}" ]; then
            rm -f -- "${temporary_path}" || true
        fi
    done
}
trap cleanup_temp_files EXIT

case "${1:-}" in
    --all)
        cleanup_mode="all"
        ;;
    --older-than-days)
        cleanup_mode="expired"
        retention_days="${2:-}"
        if ! [[ "${retention_days}" =~ ^[1-9][0-9]*$ ]]; then
            usage
            exit 2
        fi
        if ! command -v python3 >/dev/null 2>&1; then
            echo "❌ python3 is required for exact cross-platform retention timestamps." >&2
            exit 1
        fi
        cutoff_reference="$(mktemp "${TMPDIR:-/tmp}/novasharp-artifact-cutoff.XXXXXX")"
        python3 - "${cutoff_reference}" "${retention_days}" <<'PY'
import os
import math
import sys
import time

now_text = os.environ.get("NOVA_ARTIFACT_CLEANUP_NOW_EPOCH")
now = float(now_text) if now_text is not None else time.time()
if not math.isfinite(now):
    raise ValueError("cleanup clock must be finite")
cutoff = now - (int(sys.argv[2]) * 24 * 60 * 60)
os.utime(sys.argv[1], (cutoff, cutoff))
PY
        ;;
    *)
        usage
        exit 2
        ;;
esac

if [ ! -d "${workspace_dir}" ]; then
    echo "❌ Workspace not found: ${workspace_dir}" >&2
    exit 1
fi

cd "${workspace_dir}"
if [ ! -f ".devcontainer/devcontainer.json" ] || [ ! -d "src" ]; then
    echo "❌ Refusing cleanup outside a NovaSharp workspace: $(pwd)" >&2
    exit 1
fi

size_kib() {
    local total=0
    local target
    for target in artifacts BenchmarkDotNet.Artifacts; do
        if [ -e "${target}" ]; then
            total=$((total + $(du -sk "${target}" | awk '{print $1}')))
        fi
    done
    echo "${total}"
}

remove_build_directories() {
    local remove_all="$1"
    local directory

    build_directory_count=0
    build_directory_list="$(mktemp "${TMPDIR:-/tmp}/novasharp-build-directories.XXXXXX")"
    recent_build_output="$(mktemp "${TMPDIR:-/tmp}/novasharp-recent-build-output.XXXXXX")"

    if ! find src -type d \( -name "bin" -o -name "obj" \) -prune -print0 > "${build_directory_list}"; then
        echo "❌ Failed to enumerate bin/obj directories; no build directories were removed." >&2
        return 1
    fi

    while IFS= read -r -d '' directory; do
        if [ "${remove_all}" = "1" ]; then
            rm -rf -- "${directory}"
            build_directory_count=$((build_directory_count + 1))
            continue
        fi

        : > "${recent_build_output}"
        if ! find "${directory}" \( -type f -o -type l \) -newer "${cutoff_reference}" \
            -print -quit > "${recent_build_output}"; then
            echo "❌ Failed to inspect build directory; refusing to remove it: ${directory}" >&2
            return 1
        fi
        if [ ! -s "${recent_build_output}" ]; then
            rm -rf -- "${directory}"
            build_directory_count=$((build_directory_count + 1))
        fi
    done < "${build_directory_list}"

    rm -f -- "${build_directory_list}" "${recent_build_output}"
    build_directory_list=""
    recent_build_output=""
}

remove_generated_root() {
    local root="$1"
    if [ ! -e "${root}" ]; then
        return
    fi

    if [ "${cleanup_mode}" = "all" ]; then
        if [ "${root}" = "artifacts" ]; then
            find "${root}" -mindepth 1 -maxdepth 1 ! -path "artifacts/build-cache" -print0
        else
            find "${root}" -mindepth 1 -maxdepth 1 -print0
        fi |
            while IFS= read -r -d '' entry; do
                rm -rf -- "${entry}"
            done
        return
    fi

    if [ "${root}" = "artifacts" ]; then
        find "${root}" -type f ! -path "artifacts/build-cache/*" ! -newer "${cutoff_reference}" -delete
        find "${root}" -type l ! -path "artifacts/build-cache/*" ! -newer "${cutoff_reference}" -delete
        find "${root}" -depth -type d ! -path "${root}" ! -path "artifacts/build-cache" \
            ! -path "artifacts/build-cache/*" -empty -delete
        return
    fi

    find "${root}" -type f ! -newer "${cutoff_reference}" -delete
    find "${root}" -type l ! -newer "${cutoff_reference}" -delete
    find "${root}" -depth -type d ! -path "${root}" -empty -delete
}

before_kib="$(size_kib)"
if [ "${cleanup_mode}" = "all" ]; then
    remove_build_directories 1
else
    remove_build_directories 0
fi

remove_generated_root artifacts
remove_generated_root BenchmarkDotNet.Artifacts

if [ -d "src/.vs" ]; then
    rm -rf -- "src/.vs"
fi

after_kib="$(size_kib)"
reclaimed_kib=$((before_kib - after_kib))
echo "🧹 Artifact cleanup removed ${build_directory_count} stale bin/obj directories and reclaimed $((reclaimed_kib / 1024)) MiB."

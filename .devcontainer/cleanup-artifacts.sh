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
retention_mtime_days=""
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
        # POSIX find rounds -mtime down to complete 24-hour periods. +N therefore
        # selects ages of at least N+1 days without GNU-only date/find flags.
        retention_mtime_days=$((retention_days - 1))
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
    local removed=0
    local directory

    while IFS= read -r -d '' directory; do
        if [ "${remove_all}" = "1" ]; then
            rm -rf -- "${directory}"
            removed=$((removed + 1))
            continue
        fi

        if ! find "${directory}" -type f ! -mtime "+${retention_mtime_days}" -print -quit | IFS= read -r _; then
            rm -rf -- "${directory}"
            removed=$((removed + 1))
        fi
    done < <(find src -type d \( -name "bin" -o -name "obj" \) -prune -print0)

    echo "${removed}"
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
        find "${root}" -type f ! -path "artifacts/build-cache/*" -mtime "+${retention_mtime_days}" -delete
        find "${root}" -type l ! -path "artifacts/build-cache/*" -mtime "+${retention_mtime_days}" -delete
        find "${root}" -depth -type d ! -path "${root}" ! -path "artifacts/build-cache" \
            ! -path "artifacts/build-cache/*" -empty -delete
        return
    fi

    find "${root}" -type f -mtime "+${retention_mtime_days}" -delete
    find "${root}" -type l -mtime "+${retention_mtime_days}" -delete
    find "${root}" -depth -type d ! -path "${root}" -empty -delete
}

before_kib="$(size_kib)"
if [ "${cleanup_mode}" = "all" ]; then
    build_directory_count="$(remove_build_directories 1)"
else
    build_directory_count="$(remove_build_directories 0)"
fi

remove_generated_root artifacts
remove_generated_root BenchmarkDotNet.Artifacts

if [ -d "src/.vs" ]; then
    rm -rf -- "src/.vs"
fi

after_kib="$(size_kib)"
reclaimed_kib=$((before_kib - after_kib))
echo "🧹 Artifact cleanup removed ${build_directory_count} stale bin/obj directories and reclaimed $((reclaimed_kib / 1024)) MiB."

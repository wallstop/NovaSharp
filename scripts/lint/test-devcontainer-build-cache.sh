#!/usr/bin/env bash
# Prove that --no-cache bypasses a primed Docker build cache.

set -euo pipefail

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
    echo "Devcontainer no-cache check skipped: Docker is unavailable."
    exit 0
fi

test_root="$(mktemp -d "${TMPDIR:-/tmp}/novasharp-devcontainer-cache.XXXXXX")"
tag="novasharp-devcontainer-cache-test-$$"
build_log="${test_root}/second-build.log"
first_image=""
second_image=""
case "${test_root}" in
    "${TMPDIR:-/tmp}"/novasharp-devcontainer-cache.*) ;;
    *) echo "Unexpected test root: ${test_root}" >&2; exit 1 ;;
esac
cleanup() {
    docker image rm --force "${tag}" >/dev/null 2>&1 || true
    if [ -n "${first_image}" ]; then
        docker image rm --force "${first_image}" >/dev/null 2>&1 || true
    fi
    if [ -n "${second_image}" ]; then
        docker image rm --force "${second_image}" >/dev/null 2>&1 || true
    fi
    rm -rf -- "${test_root}"
}
trap cleanup EXIT

printf 'cache probe\n' > "${test_root}/probe.txt"
printf '%s\n' 'FROM scratch' 'COPY probe.txt /probe.txt' > "${test_root}/Dockerfile"

docker build --progress=plain --tag "${tag}" "${test_root}"
first_image="$(docker image inspect "${tag}" --format '{{.Id}}')"
docker build --no-cache --progress=plain --tag "${tag}" "${test_root}" 2>&1 | tee "${build_log}"
second_image="$(docker image inspect "${tag}" --format '{{.Id}}')"

if rg --fixed-strings --quiet ' CACHED' "${build_log}"; then
    echo "Docker reported a cached build step despite --no-cache." >&2
    exit 1
fi

echo "Devcontainer no-cache build check passed."

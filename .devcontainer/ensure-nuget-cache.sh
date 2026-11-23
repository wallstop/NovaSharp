#!/usr/bin/env bash
# Ensure the persistent NuGet cache is writable by the current lifecycle user.
#
# Docker initializes an empty named volume from the image only when the mount
# point exists. Volumes created before the image seeded /home/vscode/.nuget, or
# volumes created before a UID remap, can therefore be root-owned and make
# `dotnet restore` fail with "Failed to read NuGet.Config due to unauthorized
# access" before any lifecycle script can run.

set -euo pipefail

nuget_dir="${HOME:?HOME must be set}/.nuget"

case "${nuget_dir}" in
    /*) ;;
    *)
        printf '[devcontainer:nuget] ERROR: refusing to repair a relative path: %s\n' "${nuget_dir}" >&2
        exit 1
        ;;
esac

if [ -L "${nuget_dir}" ]; then
    printf '[devcontainer:nuget] ERROR: %s must not be a symbolic link.\n' "${nuget_dir}" >&2
    exit 1
fi

config_file="${nuget_dir}/NuGet/NuGet.Config"

cache_is_writable() {
    [ -d "${nuget_dir}" ] || return 1
    [ -w "${nuget_dir}" ] && [ -x "${nuget_dir}" ] || return 1

    local child
    for child in NuGet packages; do
        if [ -e "${nuget_dir}/${child}" ]; then
            [ -d "${nuget_dir}/${child}" ] || return 1
            [ -w "${nuget_dir}/${child}" ] && [ -x "${nuget_dir}/${child}" ] || return 1
        fi
    done

    if [ -e "${config_file}" ] && { [ ! -f "${config_file}" ] || [ ! -r "${config_file}" ]; }; then
        return 1
    fi
}

recovery_message() {
    printf '[devcontainer:nuget] ERROR: %s is not writable.\n' "${nuget_dir}" >&2
    printf '[devcontainer:nuget] Stop the container, recreate the disposable cache volume, and rebuild:\n' >&2
    printf '[devcontainer:nuget]   docker volume rm novasharp-nuget-cache\n' >&2
    printf '[devcontainer:nuget] A read-only mount cannot be repaired in place.\n' >&2
}

if [ -e "${config_file}" ] && [ ! -f "${config_file}" ]; then
    printf '[devcontainer:nuget] ERROR: NuGet.Config must be a regular file: %s\n' "${config_file}" >&2
    recovery_message
    exit 1
fi

if cache_is_writable; then
    exit 0
fi

# A missing cache directory under a writable HOME does not need privilege.
if [ ! -d "${nuget_dir}" ]; then
    mkdir -p "${nuget_dir}" 2>/dev/null || true
    if cache_is_writable; then
        exit 0
    fi
fi

if [ "$(id -u)" != "0" ]; then
    if ! command -v sudo >/dev/null 2>&1; then
        printf '[devcontainer:nuget] ERROR: %s is not writable and sudo is unavailable.\n' "${nuget_dir}" >&2
        recovery_message
        exit 1
    fi
    if ! sudo -n true >/dev/null 2>&1; then
        printf '[devcontainer:nuget] ERROR: passwordless sudo is unavailable.\n' >&2
        recovery_message
        exit 1
    fi
fi

elevate() {
    if [ "$(id -u)" = "0" ]; then
        "$@"
    else
        sudo -n "$@"
    fi
}

if ! elevate mkdir -p "${nuget_dir}" \
    || ! elevate chown -R "$(id -u):$(id -g)" "${nuget_dir}" \
    || ! elevate chmod -R u+rwX "${nuget_dir}"; then
    recovery_message
    exit 1
fi

if ! cache_is_writable; then
    recovery_message
    exit 1
fi

printf '[devcontainer:nuget] Repaired ownership of %s for uid %s.\n' \
    "${nuget_dir}" "$(id -u)"

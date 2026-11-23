#!/usr/bin/env bash
# Filter and validate host-side credentials before Docker consumes .mcp.env.

set +x
set -euo pipefail

workspace_dir="${1:-$(pwd -P)}"
env_file="${workspace_dir}/.env.local"
generated_file="${workspace_dir}/.devcontainer/.mcp.env"
temporary_file=""

fail() {
    printf '[devcontainer:mcp] ERROR: %s\n' "$*" >&2
    exit 1
}

cleanup_temporary_file() {
    if [ -n "${temporary_file}" ] && [ -f "${temporary_file}" ]; then
        rm -f -- "${temporary_file}"
    fi
}

protect_posix_file() {
    local path="$1" stat_option mode_format link_format
    case "${OSTYPE}" in
        darwin*|*bsd*) stat_option=-f; mode_format=%Lp; link_format=%l ;;
        *) stat_option=-c; mode_format=%a; link_format=%h ;;
    esac
    [ -O "${path}" ] || fail "Credential file must belong to the current host user."
    [ "$(stat "${stat_option}" "${link_format}" "${path}")" = 1 ] \
        || fail "Refusing to change a multiply-linked credential file."
    chmod 600 "${path}" || fail "Could not restrict credential file permissions."
    [ "$(stat "${stat_option}" "${mode_format}" "${path}")" = 600 ] \
        || fail "Credential filesystem did not enforce mode 0600."
}

[ -d "${workspace_dir}" ] || fail "Workspace directory does not exist."
[ -d "${workspace_dir}/.devcontainer" ] && [ ! -L "${workspace_dir}/.devcontainer" ] \
    || fail ".devcontainer must be a real directory inside the workspace."
trap cleanup_temporary_file EXIT
[ ! -L "${generated_file}" ] \
    || fail "Refusing to replace a symlinked .devcontainer/.mcp.env."
if [ -e "${generated_file}" ] && [ ! -f "${generated_file}" ]; then
    fail ".devcontainer/.mcp.env must be a regular file."
fi
# Keep the previous file available to other launches until the replacement is
# ready. A failed preflight stops the current launch without changing that file.
[ ! -L "${env_file}" ] \
    || fail "Refusing to read a symlinked .env.local."

# A WSL distro can have both Windows and Linux filesystems. Select the
# permission mechanism by the workspace mount, not simply by the host OS.
windows_path_tool=""
case "${OSTYPE}" in
    msys*|cygwin*) windows_path_tool=cygpath ;;
    linux*)
        if command -v wslpath >/dev/null; then
            workspace_fs="$(findmnt -n -o FSTYPE --target "${workspace_dir}")" \
                || fail "Could not identify the WSL workspace filesystem."
            case "${workspace_fs}" in
                drvfs|9p) windows_path_tool=wslpath ;;
            esac
        fi
        ;;
esac
if [ -n "${windows_path_tool}" ]; then
    acl_script="$(dirname "${BASH_SOURCE[0]}")/mcp-env-windows.ps1"
    windows_helper=(powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass
        -File "$("${windows_path_tool}" -aw "${acl_script}")" -Workspace "$("${windows_path_tool}" -aw "${workspace_dir}")")
    MSYS2_ARG_CONV_EXCL='*' "${windows_helper[@]}" -Action Protect \
        || fail "Could not protect Windows MCP credential paths."
fi

unset github_token zai_token value
github_token=""
zai_token=""
if [ -e "${env_file}" ]; then
    [ -f "${env_file}" ] || fail ".env.local must be a regular file."
    if [ -z "${windows_path_tool}" ]; then
        protect_posix_file "${env_file}"
    fi

    for key in GITHUB_PERSONAL_ACCESS_TOKEN ZAI_API_KEY; do
        definition_count="$({ grep -Ec "^${key}=" "${env_file}" || [ "$?" = "1" ]; })"
        case "${definition_count}" in
            0) continue ;;
            1) ;;
            *) fail "${key} must appear at most once in .env.local." ;;
        esac
        value="$(sed -n "s/^${key}=//p" "${env_file}")"
        value="${value%$'\r'}"
        case "${value}" in
            *[[:space:]\"\']*)
                fail "${key} must use Docker env-file syntax without quotes or whitespace."
                ;;
        esac
        case "${key}" in
            GITHUB_PERSONAL_ACCESS_TOKEN) github_token="${value}" ;;
            ZAI_API_KEY) zai_token="${value}" ;;
        esac
    done
fi

write_filtered_env() {
    if [ -n "${github_token}" ]; then
        printf 'GITHUB_PERSONAL_ACCESS_TOKEN=%s\n' "${github_token}"
    fi
    if [ -n "${zai_token}" ]; then
        printf 'ZAI_API_KEY=%s\n' "${zai_token}"
    fi
}

if [ -n "${windows_path_tool}" ]; then
    # An EOF alone can mean the producer was interrupted partway through.
    { write_filtered_env && printf '\0'; } | MSYS2_ARG_CONV_EXCL='*' "${windows_helper[@]}" -Action Publish \
        || fail "Could not publish the private Windows MCP environment file."
else
    umask 077
    temporary_file="$(mktemp "${generated_file}.XXXXXX")" \
        || fail "Could not create the filtered MCP environment file."
    protect_posix_file "${temporary_file}"
    write_filtered_env >"${temporary_file}"
    mv -f "${temporary_file}" "${generated_file}"
    [ ! -e "${temporary_file}" ] && [ -f "${generated_file}" ] \
        || fail "Atomic replacement did not create the filtered MCP environment file."
fi
trap - EXIT
printf '[devcontainer:mcp] Filtered MCP environment generated.\n'

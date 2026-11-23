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

unset github_token zai_token value
github_token=""
zai_token=""
if [ -e "${env_file}" ]; then
    [ -f "${env_file}" ] || fail ".env.local must be a regular file."
    chmod 600 "${env_file}" \
        || fail "Could not restrict .env.local permissions to the current host user."

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

umask 077
temporary_file="$(mktemp "${generated_file}.XXXXXX")" \
    || fail "Could not create the filtered MCP environment file."
{
    if [ -n "${github_token}" ]; then
        printf 'GITHUB_PERSONAL_ACCESS_TOKEN=%s\n' "${github_token}"
    fi
    if [ -n "${zai_token}" ]; then
        printf 'ZAI_API_KEY=%s\n' "${zai_token}"
    fi
} >"${temporary_file}"
chmod 600 "${temporary_file}"
mv -f "${temporary_file}" "${generated_file}"
[ ! -e "${temporary_file}" ] && [ -f "${generated_file}" ] \
    || fail "Atomic replacement did not create the filtered MCP environment file."
trap - EXIT
printf '[devcontainer:mcp] Filtered MCP environment generated.\n'

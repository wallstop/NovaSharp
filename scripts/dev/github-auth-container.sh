#!/usr/bin/env bash

set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
    echo "GitHub CLI (gh) is not installed or is not on PATH." >&2
    exit 1
fi

github_pat="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
if [[ -z "${github_pat}" ]] &&
    env -u GH_TOKEN -u GITHUB_TOKEN gh auth status --hostname github.com >/dev/null 2>&1; then
    env -u GH_TOKEN -u GITHUB_TOKEN gh auth setup-git --hostname github.com
    github_login="$(env -u GH_TOKEN -u GITHUB_TOKEN gh api user --jq '.login')"
    echo "GitHub authentication is already configured for ${github_login}."
    unset github_login github_pat
    exit 0
fi

if [[ -z "${github_pat}" ]]; then
    if ! IFS= read -r -s -p "GitHub PAT: " github_pat; then
        printf '\n' >&2
        unset github_pat
        echo "Unable to read the GitHub PAT." >&2
        exit 1
    fi
    printf '\n'
fi

if [[ -z "${github_pat}" ]]; then
    unset github_pat
    echo "GitHub PAT cannot be empty." >&2
    exit 1
fi

# The container has no reliable system keyring. Store the token in gh's
# user-only configuration so every shell in this container can authenticate.
if ! printf '%s\n' "${github_pat}" |
    env -u GH_TOKEN -u GITHUB_TOKEN gh auth login \
        --hostname github.com \
        --git-protocol https \
        --insecure-storage \
        --with-token; then
    unset github_pat
    echo "GitHub authentication setup failed." >&2
    exit 1
fi
unset github_pat

env -u GH_TOKEN -u GITHUB_TOKEN gh auth setup-git --hostname github.com

github_login=""
if ! github_login="$(env -u GH_TOKEN -u GITHUB_TOKEN gh api user --jq '.login')"; then
    unset github_login
    echo "The stored GitHub credential could not be validated." >&2
    exit 1
fi

echo "Authenticated to GitHub as ${github_login} for all shells in this container."
echo "Run 'gh auth logout --hostname github.com' to remove the stored credential."
unset github_login

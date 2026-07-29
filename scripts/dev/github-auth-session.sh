#!/usr/bin/env bash

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
  echo "Source this script so GH_TOKEN is available in the current shell:" >&2
  echo "  source ./scripts/dev/github-auth-session.sh" >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI (gh) is not installed or is not on PATH." >&2
  return 1
fi

github_pat=""
if ! IFS= read -r -s -p "GitHub PAT: " github_pat; then
  printf '\n' >&2
  unset github_pat
  echo "Unable to read the GitHub PAT." >&2
  return 1
fi
printf '\n'

if [[ -z "$github_pat" ]]; then
  unset github_pat
  echo "GitHub PAT cannot be empty." >&2
  return 1
fi

export GH_TOKEN="$github_pat"
unset github_pat

github_login=""
if ! github_login="$(gh api user --jq '.login')"; then
  unset GH_TOKEN
  echo "Authentication failed; GH_TOKEN has been removed from this shell." >&2
  unset github_login
  return 1
fi

echo "Authenticated to GitHub as $github_login."
unset github_login
echo "GH_TOKEN is set for this shell session only."
echo "Run 'unset GH_TOKEN' when finished."

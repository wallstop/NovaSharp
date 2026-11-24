#!/usr/bin/env bash
# Adapt the canonical project key name to the Z.ai Vision MCP package.

set +x
set -euo pipefail

unset zai_api_key
if [ -n "${CLAUDE_PROJECT_DIR:-}" ]; then
    launcher_dir="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"
    zai_api_key="$(python3 -EsB "${launcher_dir}/read-claude-mcp-key.py" read "${CLAUDE_PROJECT_DIR}")"
else
    zai_api_key="${ZAI_API_KEY:-${Z_AI_API_KEY:-}}"
fi
if [ -z "${zai_api_key}" ]; then
    printf 'zai-mcp-vision: ZAI_API_KEY is missing; update .env.local and rebuild the devcontainer.\n' >&2
    exit 1
fi
if ! command -v zai-mcp-server >/dev/null 2>&1; then
    printf 'zai-mcp-vision: zai-mcp-server is missing; rebuild the devcontainer.\n' >&2
    exit 1
fi

unset ZAI_API_KEY \
    Z_AI_API_KEY \
    ZAI_MCP_API_KEY \
    ANTHROPIC_AUTH_TOKEN \
    ANTHROPIC_API_KEY \
    CLAUDE_CODE_OAUTH_TOKEN \
    GITHUB_PERSONAL_ACCESS_TOKEN \
    GITHUB_PAT \
    GITHUB_TOKEN \
    GH_TOKEN \
    OPENAI_API_KEY \
    CODEX_API_KEY
export Z_AI_API_KEY="${zai_api_key}"
export Z_AI_MODE=ZAI
unset zai_api_key
exec zai-mcp-server "$@"

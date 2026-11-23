#!/usr/bin/env bash
# Exercise the image's installed Claude wrappers with synthetic credentials.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fixture="$(mktemp -d "${TMPDIR:-/tmp}/novasharp-runtime-credentials.XXXXXX")"
trap 'rm -rf -- "${fixture:?}"' EXIT
workspace="${fixture}/workspace with spaces"
private_home="${fixture}/home"
launchers="${fixture}/bin"
mkdir -p "${workspace}/.devcontainer" "${private_home}" "${launchers}"
cp "${repo_root}/.devcontainer/zai-mcp-vision.sh" "${launchers}/zai-mcp-vision"
cp "${repo_root}/.devcontainer/zai-mcp-remote.sh" "${launchers}/zai-mcp-remote"
cp "${repo_root}/.devcontainer/mcp_credentials.py" "${launchers}/read-claude-mcp-key.py"
printf 'ZAI_API_KEY=host-mount-sentinel\n' >"${workspace}/.devcontainer/.mcp.env"
chmod 0777 "${workspace}/.devcontainer/.mcp.env"

cat >"${launchers}/zai-mcp-server" <<'STUB'
#!/usr/bin/env bash
set -eu
[[ ${Z_AI_API_KEY:?} = "$(<"${EXPECTED_FILE:?}")" && -z ${ZAI_API_KEY+x} && -z ${GITHUB_PERSONAL_ACCESS_TOKEN+x} ]]
STUB
cat >"${launchers}/mcp-remote" <<'STUB'
#!/usr/bin/env bash
set -eu
[[ $# = 3 && $2 = --header-file && $(<"$3") = "Authorization: Bearer $(<"${EXPECTED_FILE:?}")" ]]
[[ -z ${ZAI_API_KEY+x} && -z ${Z_AI_API_KEY+x} && -z ${GITHUB_PERSONAL_ACCESS_TOKEN+x} ]]
STUB
chmod +x "${launchers}/zai-mcp-server" "${launchers}/mcp-remote"

stage() {
    env HOME="${private_home}" ZAI_API_KEY="$1" GITHUB_PERSONAL_ACCESS_TOKEN=synthetic-github \
        python3 -EsB "${repo_root}/.devcontainer/mcp_credentials.py" stage "${workspace}"
}

launch() {
    local project="$1"
    shift
    env HOME="${private_home}" PATH="${launchers}:${PATH}" EXPECTED_FILE="${fixture}/expected" \
        CLAUDE_PROJECT_DIR="${project}" ZAI_API_KEY=ambient-sentinel Z_AI_API_KEY=alias-sentinel \
        GITHUB_PERSONAL_ACCESS_TOKEN=synthetic-github bash "${launchers}/$1" "${@:2}"
}

reject_both() {
    local project="$1" expected_error="$2" launcher
    for launcher in zai-mcp-vision zai-mcp-remote; do
        if launch "${project}" "${launcher}" web-search >"${fixture}/failure" 2>&1; then
            echo "${launcher} accepted an unsafe or missing credential" >&2
            exit 1
        fi
        if ! grep -Fq "${expected_error}" "${fixture}/failure"; then
            cat "${fixture}/failure" >&2
            exit 1
        fi
    done
}

for sentinel in synthetic-first synthetic-rotated; do
    stage "${sentinel}"
    printf '%s\n' "${sentinel}" >"${fixture}/expected"
    launch "${workspace}/." zai-mcp-vision
    launch "${workspace}" zai-mcp-remote web-search
done

files=("${private_home}/.novasharp-mcp/"*)
[[ ${#files[@]} -eq 1 && -f ${files[0]} ]]
credential="${files[0]}"
[[ $(stat -c '%a' "${private_home}/.novasharp-mcp") = 700 && $(stat -c '%a' "${credential}") = 600 ]]
[[ $(<"${credential}") = 'ZAI_API_KEY=synthetic-rotated' ]]

# Rotate after the reader has opened the previous inode but before it checks it.
python3 -EsB - "${repo_root}/.devcontainer" "${credential}" <<'PY'
import os
from pathlib import Path
import sys
from unittest.mock import patch

sys.path.insert(0, sys.argv[1])
from mcp_credentials import read_key

path = Path(sys.argv[2])
replacement = path.with_suffix(".replacement")
replacement.write_bytes(path.read_bytes())
replacement.chmod(0o600)
open_file = os.open

def open_then_replace(path, flags):
    fd = open_file(path, flags)
    os.replace(replacement, path)
    return fd

with patch("mcp_credentials.os.open", open_then_replace):
    assert read_key(path, standalone=False) == b"synthetic-rotated"
PY

mkdir "${fixture}/another project"
reject_both "${fixture}/another project" "credentials are missing"
chmod 0755 "${private_home}/.novasharp-mcp"
reject_both "${workspace}" "mode 700"
chmod 0700 "${private_home}/.novasharp-mcp"
chmod 0644 "${credential}"
reject_both "${workspace}" "mode 600"
chmod 0600 "${credential}"
printf 'UNRELATED=sentinel\n' >>"${credential}"
reject_both "${workspace}" "Unexpected assignment"
rm -- "${credential}"
ln -s "${workspace}/.devcontainer/.mcp.env" "${credential}"
reject_both "${workspace}" "Cannot read"

stage ""
[[ -f ${credential} && ! -L ${credential} && ! -s ${credential} ]]
reject_both "${workspace}" "invalid ZAI_API_KEY"
printf 'PASS: private devcontainer Claude MCP credential storage\n'

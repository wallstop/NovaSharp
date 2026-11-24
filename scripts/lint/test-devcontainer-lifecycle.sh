#!/usr/bin/env bash
# Deterministic, network-free behavior tests for devcontainer lifecycle helpers.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
TEST_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/novasharp-devcontainer-tests.XXXXXX")"
case "${TEST_ROOT}" in
    "${TMPDIR:-/tmp}"/novasharp-devcontainer-tests.*) ;;
    *) echo "Unexpected test root: ${TEST_ROOT}" >&2; exit 1 ;;
esac
trap 'rm -rf -- "${TEST_ROOT}"' EXIT

fail() {
    echo "devcontainer lifecycle test failed: $*" >&2
    exit 1
}

assert_contains() {
    local output="$1"
    local expected="$2"
    printf '%s\n' "${output}" | grep -F -- "${expected}" >/dev/null ||
        fail "expected output to contain: ${expected}"
}

set_mtime_epoch() {
    local path="$1"
    local timestamp="$2"
    python3 - "${path}" "${timestamp}" <<'PY'
import os
import sys

path = sys.argv[1]
timestamp = float(sys.argv[2])
os.utime(path, (timestamp, timestamp), follow_symlinks=False)
PY
}

write_executable() {
    local path="$1"
    shift
    printf '%s\n' "$@" > "${path}"
    chmod +x "${path}"
}

setup_npm_fixture() {
    local scenario="$1"
    local fixture
    fixture="$(mktemp -d "${TEST_ROOT}/npm-${scenario}.XXXXXX")"
    local fake_bin="${fixture}/fake-bin"
    local prefix="${fixture}/prefix"
    mkdir -p "${fake_bin}" "${prefix}/bin" "${prefix}/lib/node_modules/@openai/codex" \
        "${prefix}/lib/node_modules/@anthropic-ai/claude-code" \
        "${prefix}/lib/node_modules/@github/copilot" \
        "${prefix}/lib/node_modules/mcp-remote" \
        "${prefix}/lib/node_modules/@z_ai/mcp-server" \
        "${prefix}/lib/node_modules/@nanocollective" "${fixture}/cache"
    printf '%s\n' "1.0.0" > "${fixture}/installed-version"

    # Variables in these single-quoted strings expand in the generated fixture.
    # shellcheck disable=SC2016
    write_executable "${fake_bin}/id" \
        '#!/usr/bin/env bash' \
        'if [ "${NOVA_TEST_SCENARIO}" = "root" ] && [ "${1:-}" = "-u" ]; then echo 0; exit 0; fi' \
        'exec /usr/bin/id "$@"'

    # shellcheck disable=SC2016
    write_executable "${fake_bin}/node" \
        '#!/usr/bin/env bash' \
        'if [ "${1:-}" = "--version" ]; then echo v24.0.0; exit 0; fi' \
        'if [ "${1:-}" = "-e" ]; then' \
        '  case "${2:-}" in *process.versions*) exit 0 ;; esac' \
        '  if [ "${3:-}" = "validate-json" ]; then' \
        '    if [ "${NOVA_TEST_SCENARIO}" = "list-invalid" ]; then exit 1; fi' \
        '    exit 0' \
        '  fi' \
        '  printf "%s" "$(<"${NOVA_TEST_FIXTURE}/installed-version")"' \
        '  exit 0' \
        'fi' \
        'exit 64'

    # shellcheck disable=SC2016
    write_executable "${fake_bin}/npm" \
        '#!/usr/bin/env bash' \
        'case "${1:-}" in' \
        '  prefix) printf "%s\n" "${NOVA_TEST_FIXTURE}/prefix" ;;' \
        '  config) printf "%s\n" "${NOVA_TEST_FIXTURE}/cache" ;;' \
        '  list)' \
        '    if [ "${NOVA_TEST_SCENARIO}" = "list-invalid" ]; then printf "not-json\n"; exit 1; fi' \
        '    version="$(<"${NOVA_TEST_FIXTURE}/installed-version")"' \
        '    printf "{\"dependencies\":{\"@nanocollective/nanocoder\":{\"version\":\"%s\"},\"opencode-ai\":{\"version\":\"%s\"},\"@openai/codex\":{\"version\":\"%s\"},\"@anthropic-ai/claude-code\":{\"version\":\"%s\"},\"@github/copilot\":{\"version\":\"%s\"},\"mcp-remote\":{\"version\":\"%s\"},\"@z_ai/mcp-server\":{\"version\":\"%s\"}}}\n" "${version}" "${version}" "${version}" "${version}" "${version}" "${version}" "${version}"' \
        '    if [ "${NOVA_TEST_SCENARIO}" = "list-nonzero" ]; then exit 1; fi' \
        '    ;;' \
        '  view)' \
        '    if [ "${NOVA_TEST_SCENARIO}" = "resolution-fail" ]; then echo "simulated registry failure" >&2; exit 69; fi' \
        '    if [ "${NOVA_TEST_SCENARIO}" = "current" ] || [ "${NOVA_TEST_SCENARIO}" = "list-nonzero" ] || [ "${NOVA_TEST_SCENARIO}" = "root" ] || [ "${NOVA_TEST_SCENARIO}" = "permission" ]; then' \
        '      printf "%s\n" "$(<"${NOVA_TEST_FIXTURE}/installed-version")"' \
        '    else' \
        '      echo 2.0.0' \
        '    fi' \
        '    ;;' \
        '  install)' \
        '    if [[ "${NOVA_TEST_SCENARIO}" =~ ^(install-fail|broken-fallback|broken-mcp-fallback|missing-fallback)$ ]]; then' \
        '      if [[ "${NOVA_TEST_SCENARIO}" = broken-* ]]; then' \
        '        tool=nanocoder; if [ "${NOVA_TEST_SCENARIO}" = "broken-mcp-fallback" ]; then tool=zai-mcp-server; fi' \
        '        unlink "${NOVA_TEST_FIXTURE}/prefix/bin/${tool}"' \
        '        printf "#!/usr/bin/env bash\necho broken native payload >&2\nexit 88\n" > "${NOVA_TEST_FIXTURE}/prefix/bin/${tool}"' \
        '        chmod +x "${NOVA_TEST_FIXTURE}/prefix/bin/${tool}"' \
        '      fi' \
        '      if [ "${NOVA_TEST_SCENARIO}" = "missing-fallback" ]; then unlink "${NOVA_TEST_FIXTURE}/prefix/bin/codex"; fi' \
        '      echo "npm error E404: published tarball unavailable" >&2' \
        '      exit 73' \
        '    fi' \
        '    echo 2.0.0 > "${NOVA_TEST_FIXTURE}/installed-version"' \
        '    ;;' \
        '  cache) ;;' \
        '  *) echo "unexpected npm command: $*" >&2; exit 65 ;;' \
        'esac'

    # shellcheck disable=SC2016
    write_executable "${prefix}/bin/fake-cli" \
        '#!/usr/bin/env bash' \
        'if [ "$(basename "$0")" = "zai-mcp-server" ]; then' \
        '  [[ $# = 0 && -n "${Z_AI_API_KEY:-}" && -z "${NOVA_TEST_FIXTURE:-}" ]] || exit 89' \
        '  exit 0' \
        'fi' \
        'printf "%s %s\n" "$(basename "$0")" "$(<"${NOVA_TEST_FIXTURE}/installed-version")"'
    for command_name in nanocoder opencode codex claude copilot mcp-remote zai-mcp-server; do
        ln -s fake-cli "${prefix}/bin/${command_name}"
    done

    printf '%s\n' "${fixture}"
}

run_installer() {
    local scenario="$1"
    shift
    local fixture
    fixture="$(setup_npm_fixture "${scenario}")"
    local output
    local exit_code
    if output="$(
        PATH="${fixture}/prefix/bin:${fixture}/fake-bin:${PATH}" \
            NOVA_TEST_FIXTURE="${fixture}" \
            NOVA_TEST_SCENARIO="${scenario}" \
            NOVA_NPM_TOOL_CACHE="${fixture}/cache" \
            NOVA_NPM_INSTALL_ATTEMPTS=1 \
            bash "${REPO_ROOT}/.devcontainer/install-npm-tools.sh" "$@" 2>&1
    )"; then
        exit_code=0
    else
        exit_code=$?
    fi
    printf '%s\n%s\n' "${exit_code}" "${output}"
}

result="$(run_installer current)"
[ "${result%%$'\n'*}" = "0" ] || fail "current tools should pass"
assert_contains "${result}" "All npm coding tools are current."

result="$(run_installer list-nonzero)"
[ "${result%%$'\n'*}" = "0" ] || fail "valid npm list JSON should survive npm's diagnostic exit"
assert_contains "${result}" "npm list exited 1, but its valid dependency tree will be used"

result="$(run_installer list-invalid)"
[ "${result%%$'\n'*}" != "0" ] || fail "invalid npm list JSON must fail the refresh"
assert_contains "${result}" "npm list did not return a valid global dependency tree"

result="$(run_installer update)"
[ "${result%%$'\n'*}" = "0" ] || fail "available updates should install"
assert_contains "${result}" "1.0.0 -> 2.0.0"

result="$(run_installer resolution-fail --offline-ok)"
[ "${result%%$'\n'*}" = "0" ] || fail "offline start should keep intact tools"
assert_contains "${result}" "latest status could not be verified"

result="$(run_installer resolution-fail)"
[ "${result%%$'\n'*}" != "0" ] || fail "strict build refresh should fail offline"
assert_contains "${result}" "Unable to resolve"

result="$(run_installer install-fail --offline-ok)"
[ "${result%%$'\n'*}" = "0" ] || fail "offline start should survive install-stage failure"
assert_contains "${result}" "validating the installed tool versions before continuing"
assert_contains "${result}" "npm error E404"

result="$(run_installer install-fail)"
[ "${result%%$'\n'*}" = "73" ] || fail "strict install should preserve npm's failure code"

result="$(run_installer broken-fallback --offline-ok)"
[ "${result%%$'\n'*}" != "0" ] || fail "a non-runnable fallback must fail startup"
assert_contains "${result}" "failed its version check"

result="$(run_installer broken-mcp-fallback --offline-ok)"
[ "${result%%$'\n'*}" != "0" ] || fail "a broken stdio MCP payload must fail startup"
assert_contains "${result}" "zai-mcp-server failed its stdio startup check"

result="$(run_installer missing-fallback --offline-ok)"
[ "${result%%$'\n'*}" = "73" ] || fail "a missing fallback must preserve npm's install failure"
assert_contains "${result}" "installed @openai/codex fallback is not intact"

result="$(run_installer root)"
[ "${result%%$'\n'*}" != "0" ] || fail "root npm installation must be rejected"
assert_contains "${result}" "Refusing to install npm tools as root"

permission_fixture="$(setup_npm_fixture permission)"
chmod 0555 "${permission_fixture}/prefix"
if permission_output="$(
    PATH="${permission_fixture}/prefix/bin:${permission_fixture}/fake-bin:${PATH}" \
        NOVA_TEST_FIXTURE="${permission_fixture}" \
        NOVA_TEST_SCENARIO=permission \
        NOVA_NPM_TOOL_CACHE="${permission_fixture}/cache" \
        NOVA_NPM_INSTALL_ATTEMPTS=1 \
        bash "${REPO_ROOT}/.devcontainer/install-npm-tools.sh" 2>&1
)"; then
    fail "non-writable npm prefix must be rejected"
fi
assert_contains "${permission_output}" "npm path is not writable"
chmod 0755 "${permission_fixture}/prefix"

chmod 0555 "${permission_fixture}/prefix/lib/node_modules/@openai/codex"
if permission_output="$(
    PATH="${permission_fixture}/prefix/bin:${permission_fixture}/fake-bin:${PATH}" \
        NOVA_TEST_FIXTURE="${permission_fixture}" \
        NOVA_TEST_SCENARIO=permission \
        NOVA_NPM_TOOL_CACHE="${permission_fixture}/cache" \
        NOVA_NPM_INSTALL_ATTEMPTS=1 \
        bash "${REPO_ROOT}/.devcontainer/install-npm-tools.sh" 2>&1
)"; then
    fail "non-writable npm package directory must be rejected"
fi
assert_contains "${permission_output}" "npm path is not writable"
chmod 0755 "${permission_fixture}/prefix/lib/node_modules/@openai/codex"

cleanup_now_epoch=2000000000
expired_epoch=$((cleanup_now_epoch - (15 * 24 * 60 * 60 / 2)))
exact_epoch=$((cleanup_now_epoch - (7 * 24 * 60 * 60)))
recent_epoch=$((cleanup_now_epoch - (13 * 24 * 60 * 60 / 2)))
workspace="${TEST_ROOT}/cleanup-workspace"
cleanup_tmp="${TEST_ROOT}/cleanup-tmp"
mkdir -p "${cleanup_tmp}" "${workspace}/.devcontainer" "${workspace}/src/Expired/bin" \
    "${workspace}/src/ExactBin/bin" "${workspace}/src/ExactObj/obj" \
    "${workspace}/src/Recent/obj" \
    "${workspace}/artifacts/build-cache" "${workspace}/artifacts/results" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache"
printf '{}\n' > "${workspace}/.devcontainer/devcontainer.json"
touch "${workspace}/src/source.cs" "${workspace}/src/Expired/bin/expired.dll" \
    "${workspace}/src/ExactBin/bin/exact.dll" "${workspace}/src/ExactObj/obj/exact.dll" \
    "${workspace}/src/Recent/obj/recent.dll" \
    "${workspace}/artifacts/build-cache/preserved.cache" \
    "${workspace}/artifacts/results/expired.txt" "${workspace}/artifacts/results/recent.txt" \
    "${workspace}/artifacts/results/exact.txt" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json"
ln -s ../../src/source.cs "${workspace}/artifacts/results/expired.link"
ln -s ../../src/source.cs "${workspace}/artifacts/results/exact.link"
ln -s ../../src/source.cs "${workspace}/artifacts/results/recent.link"
for expired_path in \
    "${workspace}/src/Expired/bin/expired.dll" \
    "${workspace}/artifacts/build-cache/preserved.cache" \
    "${workspace}/artifacts/results/expired.txt" \
    "${workspace}/artifacts/results/expired.link" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json"; do
    set_mtime_epoch "${expired_path}" "${expired_epoch}"
done
for exact_path in \
    "${workspace}/src/ExactBin/bin/exact.dll" \
    "${workspace}/src/ExactObj/obj/exact.dll" \
    "${workspace}/artifacts/results/exact.txt" \
    "${workspace}/artifacts/results/exact.link"; do
    set_mtime_epoch "${exact_path}" "${exact_epoch}"
done
for recent_path in \
    "${workspace}/src/Recent/obj/recent.dll" \
    "${workspace}/artifacts/results/recent.txt" \
    "${workspace}/artifacts/results/recent.link"; do
    set_mtime_epoch "${recent_path}" "${recent_epoch}"
done

TMPDIR="${cleanup_tmp}" NOVA_ARTIFACT_CLEANUP_NOW_EPOCH="${cleanup_now_epoch}" \
    bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" "${workspace}" --older-than-days 7 >/dev/null
[ ! -e "${workspace}/src/Expired/bin" ] || fail "7.5-day-old bin directory survived"
[ ! -e "${workspace}/src/ExactBin/bin" ] || fail "exact-cutoff bin directory survived"
[ ! -e "${workspace}/src/ExactObj/obj" ] || fail "exact-cutoff obj directory survived"
[ -f "${workspace}/src/Recent/obj/recent.dll" ] || fail "6.5-day-old obj file was deleted"
[ -f "${workspace}/artifacts/build-cache/preserved.cache" ] || fail "legacy mounted cache was deleted"
[ ! -e "${workspace}/artifacts/results/expired.txt" ] || fail "7.5-day-old artifact survived"
[ ! -e "${workspace}/artifacts/results/exact.txt" ] || fail "exact-cutoff artifact survived"
[ -f "${workspace}/artifacts/results/recent.txt" ] || fail "6.5-day-old artifact was deleted"
[ ! -L "${workspace}/artifacts/results/expired.link" ] || fail "7.5-day-old symlink survived"
[ ! -L "${workspace}/artifacts/results/exact.link" ] || fail "exact-cutoff symlink survived"
[ -L "${workspace}/artifacts/results/recent.link" ] || fail "6.5-day-old symlink was deleted"
[ ! -e "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json" ] || fail "benchmark build-cache name was incorrectly exempted"
[ -f "${workspace}/src/source.cs" ] || fail "source was deleted"
if compgen -G "${cleanup_tmp}/novasharp-*" >/dev/null; then
    fail "successful cleanup leaked temporary files"
fi

python_failure_workspace="${TEST_ROOT}/python-failure-workspace"
python_failure_tmp="${TEST_ROOT}/python-failure-tmp"
python_failure_bin="${TEST_ROOT}/python-failure-bin"
mkdir -p "${python_failure_workspace}/.devcontainer" "${python_failure_workspace}/src" \
    "${python_failure_workspace}/artifacts" "${python_failure_tmp}" "${python_failure_bin}"
printf '{}\n' > "${python_failure_workspace}/.devcontainer/devcontainer.json"
touch "${python_failure_workspace}/artifacts/preserved.txt"
write_executable "${python_failure_bin}/python3" \
    '#!/usr/bin/env bash' \
    'echo "simulated cutoff clock failure" >&2' \
    'exit 74'
if python_failure_output="$(
    PATH="${python_failure_bin}:${PATH}" TMPDIR="${python_failure_tmp}" \
        NOVA_ARTIFACT_CLEANUP_NOW_EPOCH="${cleanup_now_epoch}" \
        bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" \
        "${python_failure_workspace}" --older-than-days 7 2>&1
)"; then
    fail "cutoff clock failure must fail cleanup"
fi
assert_contains "${python_failure_output}" "simulated cutoff clock failure"
[ -f "${python_failure_workspace}/artifacts/preserved.txt" ] || fail "clock failure deleted an artifact"
if compgen -G "${python_failure_tmp}/novasharp-*" >/dev/null; then
    fail "clock failure leaked its cutoff reference"
fi

find_failure_workspace="${TEST_ROOT}/find-failure-workspace"
find_failure_tmp="${TEST_ROOT}/find-failure-tmp"
find_failure_bin="${TEST_ROOT}/find-failure-bin"
find_failure_count="${TEST_ROOT}/find-failure-count"
real_find="$(command -v find)"
mkdir -p "${find_failure_workspace}/.devcontainer" "${find_failure_workspace}/src/Fail/bin" \
    "${find_failure_tmp}" "${find_failure_bin}"
printf '{}\n' > "${find_failure_workspace}/.devcontainer/devcontainer.json"
touch "${find_failure_workspace}/src/Fail/bin/preserved.dll"
# shellcheck disable=SC2016
write_executable "${find_failure_bin}/find" \
    '#!/usr/bin/env bash' \
    'count=0' \
    'if [ -f "${NOVA_TEST_FIND_COUNT}" ]; then read -r count < "${NOVA_TEST_FIND_COUNT}"; fi' \
    'count=$((count + 1))' \
    'printf "%s\n" "${count}" > "${NOVA_TEST_FIND_COUNT}"' \
    'if [ "${count}" -eq "${NOVA_TEST_FIND_FAIL_AT}" ]; then echo "simulated find failure" >&2; exit 77; fi' \
    'exec "${NOVA_TEST_REAL_FIND}" "$@"'
if outer_find_failure_output="$(
    PATH="${find_failure_bin}:${PATH}" TMPDIR="${find_failure_tmp}" \
        NOVA_ARTIFACT_CLEANUP_NOW_EPOCH="${cleanup_now_epoch}" \
        NOVA_TEST_FIND_COUNT="${find_failure_count}" NOVA_TEST_FIND_FAIL_AT=1 \
        NOVA_TEST_REAL_FIND="${real_find}" \
        bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" \
        "${find_failure_workspace}" --older-than-days 7 2>&1
)"; then
    fail "build-directory enumeration failure must fail cleanup"
fi
assert_contains "${outer_find_failure_output}" "no build directories were removed"
[ -f "${find_failure_workspace}/src/Fail/bin/preserved.dll" ] || fail "enumeration failure deleted build output"
if compgen -G "${find_failure_tmp}/novasharp-*" >/dev/null; then
    fail "enumeration failure leaked temporary files"
fi
rm -f -- "${find_failure_count}"

if find_failure_output="$(
    PATH="${find_failure_bin}:${PATH}" TMPDIR="${find_failure_tmp}" \
        NOVA_ARTIFACT_CLEANUP_NOW_EPOCH="${cleanup_now_epoch}" \
        NOVA_TEST_FIND_COUNT="${find_failure_count}" NOVA_TEST_FIND_FAIL_AT=2 \
        NOVA_TEST_REAL_FIND="${real_find}" \
        bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" \
        "${find_failure_workspace}" --older-than-days 7 2>&1
)"; then
    fail "build-directory traversal failure must fail cleanup"
fi
assert_contains "${find_failure_output}" "refusing to remove it"
[ -f "${find_failure_workspace}/src/Fail/bin/preserved.dll" ] || fail "find failure deleted build output"
if compgen -G "${find_failure_tmp}/novasharp-*" >/dev/null; then
    fail "find failure leaked temporary files"
fi

mkdir -p "${workspace}/src/Full/bin" "${workspace}/artifacts/results" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache"
touch "${workspace}/src/Full/bin/output.dll" "${workspace}/artifacts/results/output.trx" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache/output.json"
bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" "${workspace}" --all >/dev/null
[ ! -e "${workspace}/src/Full/bin" ] || fail "full cleanup left bin output"
[ ! -e "${workspace}/artifacts/results" ] || fail "full cleanup left artifacts output"
[ -f "${workspace}/artifacts/build-cache/preserved.cache" ] || fail "full cleanup removed legacy cache"
[ ! -e "${workspace}/BenchmarkDotNet.Artifacts/build-cache" ] || fail "full cleanup exempted benchmark output"
[ -f "${workspace}/src/source.cs" ] || fail "full cleanup deleted source"

# ============================================================================
# NuGet cache ownership guard
# ============================================================================
nuget_guard="${REPO_ROOT}/.devcontainer/ensure-nuget-cache.sh"
grep -F -- '$(id -u)' "${nuget_guard}" >/dev/null ||
    fail "NuGet cache guard must use the runtime UID"
grep -F -- '$(id -g)' "${nuget_guard}" >/dev/null ||
    fail "NuGet cache guard must use the runtime GID"

nuget_home="${TEST_ROOT}/nuget-home"
mkdir -p "${nuget_home}/.nuget"

nuget_healthy_bin="${TEST_ROOT}/nuget-healthy-bin"
mkdir -p "${nuget_healthy_bin}"
write_executable "${nuget_healthy_bin}/sudo" \
    '#!/usr/bin/env bash' \
    'echo "healthy NuGet cache must not invoke sudo" >&2' \
    'exit 98'
if ! HOME="${nuget_home}" PATH="${nuget_healthy_bin}:${PATH}" \
    bash "${nuget_guard}" 2>&1; then
    fail "healthy NuGet cache should pass without sudo"
fi

nuget_malformed_home="${TEST_ROOT}/nuget-malformed-home"
mkdir -p "${nuget_malformed_home}/.nuget/NuGet/NuGet.Config"
if nuget_malformed_output="$(
    HOME="${nuget_malformed_home}" PATH="${nuget_healthy_bin}:${PATH}" bash "${nuget_guard}" 2>&1
)"; then
    fail "a directory in place of NuGet.Config must fail before restore"
fi
assert_contains "${nuget_malformed_output}" "NuGet.Config must be a regular file"
assert_contains "${nuget_malformed_output}" "docker volume rm novasharp-nuget-cache"

if [ "$(id -u)" = "0" ]; then
    echo "Skipping non-root NuGet cache cases because the test suite is running as root."
else
    chmod 0555 "${nuget_home}/.nuget"
    nuget_sudo_log="${TEST_ROOT}/nuget-sudo.log"
    nuget_repair_bin="${TEST_ROOT}/nuget-repair-bin"
    mkdir -p "${nuget_repair_bin}"
    # shellcheck disable=SC2016
    write_executable "${nuget_repair_bin}/sudo" \
        '#!/usr/bin/env bash' \
        'printf "%s\n" "$*" >> "${NOVA_TEST_SUDO_LOG}"' \
        'if [ "${1:-}" = "-n" ]; then shift; fi' \
        'command_name="${1:-}"' \
        'if [ -z "${command_name}" ]; then exit 96; fi' \
        'shift' \
        'exec "$(command -v "${command_name}")" "$@"'
    if ! HOME="${nuget_home}" PATH="${nuget_repair_bin}:${PATH}" \
        NOVA_TEST_SUDO_LOG="${nuget_sudo_log}" bash "${nuget_guard}" 2>&1; then
        fail "root-owned-equivalent NuGet cache should be repaired"
    fi
    [ -w "${nuget_home}/.nuget" ] || fail "NuGet cache was not made writable"
    assert_contains "$(<"${nuget_sudo_log}")" "chown -R $(id -u):$(id -g) ${nuget_home}/.nuget"
    assert_contains "$(<"${nuget_sudo_log}")" "chmod -R u+rwX ${nuget_home}/.nuget"

    # Simulate sudo exposing a malformed config while repairing its parent.
    write_executable "${nuget_repair_bin}/chown" \
        '#!/usr/bin/env bash' \
        'chmod u+rwx "$3/NuGet"'
    chmod 000 "${nuget_malformed_home}/.nuget/NuGet"
    if nuget_malformed_output="$(
        HOME="${nuget_malformed_home}" PATH="${nuget_repair_bin}:${PATH}" \
            NOVA_TEST_SUDO_LOG="${nuget_sudo_log}" bash "${nuget_guard}" 2>&1
    )"; then
        fail "malformed NuGet.Config must still fail after its parent becomes accessible"
    fi
    [ -x "${nuget_malformed_home}/.nuget/NuGet" ] ||
        fail "test sudo must make the malformed configuration visible"
    rm -- "${nuget_repair_bin}/chown"
    assert_contains "${nuget_malformed_output}" "docker volume rm novasharp-nuget-cache"

    # An unreadable config file inside a writable cache still needs repair.
    mkdir -p "${nuget_home}/.nuget/NuGet"
    printf '<configuration />\n' > "${nuget_home}/.nuget/NuGet/NuGet.Config"
    chmod 000 "${nuget_home}/.nuget/NuGet/NuGet.Config"
    if ! HOME="${nuget_home}" PATH="${nuget_repair_bin}:${PATH}" \
        NOVA_TEST_SUDO_LOG="${nuget_sudo_log}" bash "${nuget_guard}" 2>&1; then
        fail "unreadable NuGet.Config should trigger a repair"
    fi
    [ -r "${nuget_home}/.nuget/NuGet/NuGet.Config" ] ||
        fail "NuGet.Config was not made readable"
    chmod 0644 "${nuget_home}/.nuget/NuGet/NuGet.Config"

    nuget_no_sudo_bin="${TEST_ROOT}/nuget-no-sudo-bin"
    mkdir -p "${nuget_no_sudo_bin}"
    for command_name in id mkdir chown chmod; do
        ln -s "$(command -v "${command_name}")" "${nuget_no_sudo_bin}/${command_name}"
    done
    bash_path="$(command -v bash)"
    chmod 0555 "${nuget_home}/.nuget"
    if nuget_no_sudo_output="$(
        HOME="${nuget_home}" PATH="${nuget_no_sudo_bin}" "${bash_path}" "${nuget_guard}" 2>&1
    )"; then
        fail "unwritable NuGet cache without sudo must fail"
    fi
    assert_contains "${nuget_no_sudo_output}" "sudo is unavailable"

    # A missing cache under a writable HOME must not require sudo.
    nuget_empty_home="${TEST_ROOT}/nuget-empty-home"
    mkdir -p "${nuget_empty_home}"
    if ! HOME="${nuget_empty_home}" PATH="${nuget_no_sudo_bin}" \
        "${bash_path}" "${nuget_guard}" 2>&1; then
        fail "missing NuGet cache under a writable HOME should not require sudo"
    fi
    [ -d "${nuget_empty_home}/.nuget" ] || fail "NuGet cache directory was not created"

    # Passwordless sudo being unavailable must produce the remediation message.
    nuget_denied_bin="${TEST_ROOT}/nuget-denied-bin"
    mkdir -p "${nuget_denied_bin}"
    write_executable "${nuget_denied_bin}/sudo" \
        '#!/usr/bin/env bash' \
        'echo "sudo: a password is required" >&2' \
        'exit 1'
    chmod 0555 "${nuget_home}/.nuget"
    if nuget_denied_output="$(
        HOME="${nuget_home}" PATH="${nuget_denied_bin}:${PATH}" bash "${nuget_guard}" 2>&1
    )"; then
        fail "password-required sudo must fail the repair"
    fi
    assert_contains "${nuget_denied_output}" "passwordless sudo is unavailable"
    assert_contains "${nuget_denied_output}" "docker volume rm novasharp-nuget-cache"
    chmod 0755 "${nuget_home}/.nuget"
fi

echo "Devcontainer lifecycle behavior checks passed."

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

set_mtime_days_ago() {
    local path="$1"
    local days_ago="$2"
    python3 - "${path}" "${days_ago}" <<'PY'
import os
import sys
import time

path = sys.argv[1]
timestamp = time.time() - (float(sys.argv[2]) * 24 * 60 * 60)
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
        '    printf "{\"dependencies\":{\"@nanocollective/nanocoder\":{\"version\":\"%s\"},\"opencode-ai\":{\"version\":\"%s\"},\"@openai/codex\":{\"version\":\"%s\"}}}\n" "${version}" "${version}" "${version}"' \
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
        '    if [ "${NOVA_TEST_SCENARIO}" = "install-fail" ] || [ "${NOVA_TEST_SCENARIO}" = "broken-fallback" ]; then' \
        '      if [ "${NOVA_TEST_SCENARIO}" = "broken-fallback" ]; then' \
        '        unlink "${NOVA_TEST_FIXTURE}/prefix/bin/nanocoder"' \
        '        printf "#!/usr/bin/env bash\necho broken native payload >&2\nexit 88\n" > "${NOVA_TEST_FIXTURE}/prefix/bin/nanocoder"' \
        '        chmod +x "${NOVA_TEST_FIXTURE}/prefix/bin/nanocoder"' \
        '      fi' \
        '      echo "simulated install failure" >&2' \
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
        'printf "%s %s\n" "$(basename "$0")" "$(<"${NOVA_TEST_FIXTURE}/installed-version")"'
    for command_name in nanocoder opencode codex; do
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
[ "${result%%$'\n'*}" != "0" ] || fail "strict create/build refresh should fail offline"
assert_contains "${result}" "Unable to resolve"

result="$(run_installer install-fail --offline-ok)"
[ "${result%%$'\n'*}" = "0" ] || fail "offline start should survive install-stage failure"
assert_contains "${result}" "continuing with the intact installed tool versions"

result="$(run_installer install-fail)"
[ "${result%%$'\n'*}" = "73" ] || fail "strict install should preserve npm's failure code"

result="$(run_installer broken-fallback --offline-ok)"
[ "${result%%$'\n'*}" != "0" ] || fail "a non-runnable fallback must fail startup"
assert_contains "${result}" "failed its version check"

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

workspace="${TEST_ROOT}/cleanup-workspace"
mkdir -p "${workspace}/.devcontainer" "${workspace}/src/Expired/bin" "${workspace}/src/Recent/obj" \
    "${workspace}/artifacts/build-cache" "${workspace}/artifacts/results" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache"
printf '{}\n' > "${workspace}/.devcontainer/devcontainer.json"
touch "${workspace}/src/source.cs" "${workspace}/src/Expired/bin/expired.dll" \
    "${workspace}/src/Recent/obj/recent.dll" "${workspace}/artifacts/build-cache/preserved.cache" \
    "${workspace}/artifacts/results/expired.txt" "${workspace}/artifacts/results/recent.txt" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json"
ln -s ../../src/source.cs "${workspace}/artifacts/results/expired.link"
ln -s ../../src/source.cs "${workspace}/artifacts/results/recent.link"
for expired_path in \
    "${workspace}/src/Expired/bin/expired.dll" \
    "${workspace}/artifacts/build-cache/preserved.cache" \
    "${workspace}/artifacts/results/expired.txt" \
    "${workspace}/artifacts/results/expired.link" \
    "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json"; do
    set_mtime_days_ago "${expired_path}" 7.5
done
for recent_path in \
    "${workspace}/src/Recent/obj/recent.dll" \
    "${workspace}/artifacts/results/recent.txt" \
    "${workspace}/artifacts/results/recent.link"; do
    set_mtime_days_ago "${recent_path}" 6.5
done

bash "${REPO_ROOT}/.devcontainer/cleanup-artifacts.sh" "${workspace}" --older-than-days 7 >/dev/null
[ ! -e "${workspace}/src/Expired/bin" ] || fail "7.5-day-old bin directory survived"
[ -f "${workspace}/src/Recent/obj/recent.dll" ] || fail "6.5-day-old obj file was deleted"
[ -f "${workspace}/artifacts/build-cache/preserved.cache" ] || fail "legacy mounted cache was deleted"
[ ! -e "${workspace}/artifacts/results/expired.txt" ] || fail "7.5-day-old artifact survived"
[ -f "${workspace}/artifacts/results/recent.txt" ] || fail "6.5-day-old artifact was deleted"
[ ! -L "${workspace}/artifacts/results/expired.link" ] || fail "7.5-day-old symlink survived"
[ -L "${workspace}/artifacts/results/recent.link" ] || fail "6.5-day-old symlink was deleted"
[ ! -e "${workspace}/BenchmarkDotNet.Artifacts/build-cache/expired.json" ] || fail "benchmark build-cache name was incorrectly exempted"
[ -f "${workspace}/src/source.cs" ] || fail "source was deleted"

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

echo "Devcontainer lifecycle behavior checks passed."

#!/usr/bin/env bash
# Post-create script for NovaSharp dev container
# NOTE: NuGet restore is done in on-create.sh (before extensions load)
# This script handles remaining setup: Python environment, hooks, verification

set -euo pipefail

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           NovaSharp Post-Create Setup                          ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

WORKSPACE_DIR="${1:-$(pwd)}"
if [ ! -d "${WORKSPACE_DIR}" ]; then
    echo "❌ Workspace not found: ${WORKSPACE_DIR}"
    exit 1
fi

cd "${WORKSPACE_DIR}"

run_with_retries() {
    local max_attempts="$1"
    shift

    local attempt=1
    local delay_seconds=2
    while true; do
        if "$@"; then
            return 0
        else
            local exit_code=$?
        fi
        if [ "${attempt}" -ge "${max_attempts}" ]; then
            return "${exit_code}"
        fi

        local jitter=$((RANDOM % 3))
        local sleep_seconds=$((delay_seconds + jitter))
        local next_attempt=$((attempt + 1))
        echo "   Retry ${next_attempt}/${max_attempts} in ${sleep_seconds}s: $*"
        sleep "${sleep_seconds}"
        attempt="${next_attempt}"
        if [ "${delay_seconds}" -lt 30 ]; then
            delay_seconds=$((delay_seconds * 2))
            if [ "${delay_seconds}" -gt 30 ]; then
                delay_seconds=30
            fi
        fi
    done
}

ensure_python_venv() {
    local venv_dir="$1"
    local venv_python="${venv_dir}/bin/python"
    local recreate=0

    if [ -x "${venv_python}" ]; then
        if ! "${venv_python}" - <<'PY'
import sys

if sys.prefix == sys.base_prefix:
    raise SystemExit("python is not running from a virtual environment")
PY
        then
            echo "   Existing virtual environment is unhealthy; recreating."
            recreate=1
        fi
    elif [ -e "${venv_dir}" ]; then
        echo "   Existing .venv is not a usable Python environment; recreating."
        recreate=1
    else
        recreate=1
    fi

    if [ "${recreate}" = "1" ]; then
        rm -rf "${venv_dir}"
        python3 -m venv "${venv_dir}"
    fi
}

refresh_bashrc_venv_path() {
    local venv_dir="$1"
    local bashrc="${HOME}/.bashrc"
    local temp_bashrc
    temp_bashrc="$(mktemp)"

    if [ -f "${bashrc}" ]; then
        awk '
            $0 == "# >>> NovaSharp Python venv activation >>>" { skip = 1; next }
            $0 == "# <<< NovaSharp Python venv activation <<<" { skip = 0; next }
            skip != 1 { print }
        ' "${bashrc}" > "${temp_bashrc}"
    fi

    {
        echo ""
        echo "# >>> NovaSharp Python venv activation >>>"
        echo "export PATH=\"${venv_dir}/bin:\${PATH}\""
        echo "# <<< NovaSharp Python venv activation <<<"
    } >> "${temp_bashrc}"

    mv "${temp_bashrc}" "${bashrc}"
}

# ============================================================================
# STEP 1: Setup Python environment
# ============================================================================
echo ""
echo "🐍 Step 1/4: Setting up Python environment..."

VENV_DIR="${WORKSPACE_DIR}/.venv"
VENV_PYTHON="${VENV_DIR}/bin/python"
ensure_python_venv "${VENV_DIR}"
run_with_retries 5 "${VENV_PYTHON}" -m pip install --upgrade pip --retries 5 --timeout 60
run_with_retries 5 "${VENV_PYTHON}" -m pip install -r requirements.tooling.txt --retries 5 --timeout 60
"${VENV_PYTHON}" -m pip check
"${VENV_PYTHON}" - <<'PY'
import codespell_lib
import markdown_it
import mdformat
import requests
import yamllint
PY

# Ensure venv is on PATH for future sessions
refresh_bashrc_venv_path "${VENV_DIR}"

echo "   Virtual environment: ${VENV_DIR}"

# ============================================================================
# STEP 2: Install pre-commit hooks
# ============================================================================
echo ""
echo "🪝 Step 2/4: Installing pre-commit hooks..."
if [ -f "scripts/dev/install-hooks.sh" ]; then
    if ! bash scripts/dev/install-hooks.sh; then
        echo "   Warning: install-hooks.sh reported an error"
    fi
else
    echo "   Skipped (install-hooks.sh not found)"
fi

# ============================================================================
# STEP 3: Optional build cache pre-warm
# ============================================================================
echo ""
echo "🔥 Step 3/4: Optional build cache pre-warm..."
if [ "${NOVA_PREWARM_BUILD:-0}" = "1" ]; then
    echo "   Prewarming build cache (NOVA_PREWARM_BUILD=1)"
    dotnet build src/NovaSharp.sln -c Release -m --verbosity minimal
else
    echo "   Skipped (set NOVA_PREWARM_BUILD=1 to enable)"
fi

# ============================================================================
# STEP 4: Environment verification
# ============================================================================
echo ""
echo "🔎 Step 4/4: Verifying toolchain..."
echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           Environment Verification                             ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "📌 Restoring local .NET tools:"
run_with_retries 5 dotnet tool restore --verbosity minimal

echo ""
echo "📌 .NET SDKs:"
dotnet --list-sdks | sed 's/^/   /'

echo ""
echo "📌 Lua Interpreters:"
for v in 5.1 5.2 5.3 5.4 5.5; do
    cmd="lua${v}"
    if command -v "$cmd" >/dev/null 2>&1; then
        printf "   %-10s %s\n" "Lua ${v}:" "$($cmd -v 2>&1 | head -1)"
    else
        printf "   %-10s %s\n" "Lua ${v}:" "NOT FOUND"
    fi
done

echo ""
echo "📌 JavaScript Tooling:"
if NODE_VERSION="$(node --version 2>&1)"; then
    echo "   Node.js:           ${NODE_VERSION}"
else
    echo "   Node.js:           NOT FOUND"
fi
if NPM_VERSION="$(npm --version 2>&1)"; then
    echo "   npm:               ${NPM_VERSION}"
else
    echo "   npm:               NOT FOUND"
fi
if CODEX_VERSION="$(codex --version 2>&1)"; then
    echo "   Codex CLI:         ${CODEX_VERSION}"
else
    echo "   Codex CLI:         NOT FOUND"
fi

echo ""
echo "📌 Local .NET Tools:"
if CSHARPIER_VERSION="$(dotnet tool run csharpier --version 2>&1)"; then
    echo "   CSharpier:         ${CSHARPIER_VERSION}"
else
    echo "   CSharpier:         NOT FOUND"
fi
if REPORTGENERATOR_HELP="$(dotnet tool run reportgenerator -help 2>&1)"; then
    REPORTGENERATOR_VERSION="$(printf '%s\n' "${REPORTGENERATOR_HELP}" | sed -n 's/^ReportGenerator //p' | head -1)"
    if [ -n "${REPORTGENERATOR_VERSION}" ]; then
        echo "   ReportGenerator:   ${REPORTGENERATOR_VERSION}"
    else
        echo "   ReportGenerator:   installed"
    fi
else
    echo "   ReportGenerator:   NOT FOUND"
fi

echo ""
echo "📌 Workflow/YAML Linting:"
if ACTIONLINT_VERSION="$(actionlint -version 2>&1)"; then
    echo "   actionlint:        ${ACTIONLINT_VERSION}"
else
    echo "   actionlint:        NOT FOUND"
fi
if YAMLLINT_VERSION="$(yamllint --version 2>&1)"; then
    echo "   yamllint:          ${YAMLLINT_VERSION}"
else
    echo "   yamllint:          NOT FOUND"
fi

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           ✅ Setup Complete!                                    ║"
echo "╠════════════════════════════════════════════════════════════════╣"
echo "║  Quick commands:                                               ║"
echo "║    ./scripts/build/quick.sh      - Build interpreter           ║"
echo "║    ./scripts/test/quick.sh       - Run all tests               ║"
echo "║    ./scripts/test/quick.sh Floor - Run tests matching 'Floor'  ║"
echo "║                                                                ║"
echo "║  IntelliSense will load automatically via Roslyn LSP.          ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

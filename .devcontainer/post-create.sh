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

cd /workspaces/NovaSharp

# ============================================================================
# STEP 1: Install TUnit templates
# ============================================================================
echo "📦 Step 1/4: Installing TUnit templates..."
dotnet new install TUnit.Templates --force 2>/dev/null || true

# ============================================================================
# STEP 2: Setup Python environment
# ============================================================================
echo ""
echo "🐍 Step 2/4: Setting up Python environment..."

VENV_DIR="/workspaces/NovaSharp/.venv"
python3 -m venv "${VENV_DIR}"
"${VENV_DIR}/bin/pip" install --upgrade pip --quiet
"${VENV_DIR}/bin/pip" install -r requirements.tooling.txt --quiet

# Ensure venv is on PATH for future sessions
BASHRC_MARKER="# NovaSharp Python venv activation"
if ! grep -q "${BASHRC_MARKER}" ~/.bashrc 2>/dev/null; then
    {
        echo ""
        echo "${BASHRC_MARKER}"
        echo "export PATH=\"${VENV_DIR}/bin:\${PATH}\""
    } >> ~/.bashrc
fi

echo "   Virtual environment: ${VENV_DIR}"

# ============================================================================
# STEP 3: Install pre-commit hooks
# ============================================================================
echo ""
echo "🪝 Step 3/4: Installing pre-commit hooks..."
if [ -f "scripts/dev/install-hooks.sh" ]; then
    bash scripts/dev/install-hooks.sh 2>/dev/null || echo "   (hooks may already be installed)"
else
    echo "   Skipped (install-hooks.sh not found)"
fi

# ============================================================================
# STEP 4: Verification
# ============================================================================
echo ""
# ============================================================================
# STEP 4: Pre-warm build cache
# ============================================================================
echo ""
echo "🔥 Step 4/5: Pre-warming build cache (background)..."
# Build in background to warm the Roslyn compilation server and create obj/bin caches
# This makes subsequent edit-build-test cycles much faster
(dotnet build src/NovaSharp.sln -c Release -m --verbosity quiet &>/dev/null &)
echo "   Build started in background (Roslyn server warming up)"

# ============================================================================
# ENVIRONMENT VERIFICATION
# ============================================================================
echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           Environment Verification                             ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

echo "📌 .NET SDKs:"
dotnet --list-sdks | sed 's/^/   /'

echo ""
echo "📌 Lua Interpreters:"
for v in 5.1 5.2 5.3 5.4 5.5; do
    cmd="lua${v}"
    if command -v "$cmd" &> /dev/null; then
        printf "   %-10s %s\n" "Lua ${v}:" "$($cmd -v 2>&1 | head -1)"
    else
        printf "   %-10s %s\n" "Lua ${v}:" "NOT FOUND"
    fi
done

echo ""
echo "📌 Global Tools:"
echo "   CSharpier:        $(csharpier --version 2>/dev/null || echo 'not found')"
echo "   ReportGenerator:  $(reportgenerator --version 2>/dev/null | head -1 || echo 'not found')"

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

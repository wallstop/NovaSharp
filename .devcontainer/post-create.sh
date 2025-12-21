#!/usr/bin/env bash
# Post-create script for NovaSharp dev container
# Restores .NET tools and Python dependencies (Lua versions are pre-installed via Dockerfile)

set -euo pipefail

echo "=== NovaSharp Dev Container Setup ==="

cd /workspaces/NovaSharp

# Clean stale obj/bin folders that may contain Windows-specific NuGet asset caches
# This prevents "Unable to find fallback package folder" errors when opening in VS Code
echo "Cleaning stale build artifacts..."
find src -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
find src -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true

# Restore dotnet tools
echo "Restoring .NET tools..."
dotnet tool restore

# Install TUnit project templates for creating new test projects
echo "Installing TUnit templates..."
dotnet new install TUnit.Templates || true

# Restore NuGet packages so C# extension loads without errors
echo "Restoring NuGet packages..."
dotnet restore src/NovaSharp.sln

# Create Python virtual environment for tooling (PEP 668 compliance)
VENV_DIR="/workspaces/NovaSharp/.venv"
echo "Setting up Python virtual environment at ${VENV_DIR}..."
python3 -m venv "${VENV_DIR}"

# Install Python tooling dependencies in the virtual environment
echo "Installing Python tooling dependencies..."
"${VENV_DIR}/bin/pip" install --upgrade pip
"${VENV_DIR}/bin/pip" install -r requirements.tooling.txt

# Add venv to PATH for the current session and future terminals
echo "Configuring PATH for virtual environment..."
export PATH="${VENV_DIR}/bin:${PATH}"

# Ensure venv is activated for future bash sessions
BASHRC_MARKER="# NovaSharp Python venv activation"
if ! grep -q "${BASHRC_MARKER}" ~/.bashrc 2>/dev/null; then
    echo "" >> ~/.bashrc
    echo "${BASHRC_MARKER}" >> ~/.bashrc
    echo "export PATH=\"${VENV_DIR}/bin:\${PATH}\"" >> ~/.bashrc
fi

# Verify installations
echo ""
echo "=== Lua Version Verification ==="

verify_lua_version() {
    local cmd=$1
    local label=$2
    if command -v "$cmd" &> /dev/null; then
        echo -n "${label}: " && $cmd -v 2>&1 | head -1
    else
        echo "${label}: NOT INSTALLED"
    fi
}

verify_lua_version "lua5.1" "Lua 5.1"
verify_lua_version "lua5.2" "Lua 5.2"
verify_lua_version "lua5.3" "Lua 5.3"
verify_lua_version "lua5.4" "Lua 5.4"
verify_lua_version "lua5.5" "Lua 5.5"

echo ""
echo "=== Python Verification ==="
echo -n "Python: " && "${VENV_DIR}/bin/python3" --version 2>&1
echo -n "pip: " && "${VENV_DIR}/bin/pip" --version 2>&1
echo "Virtual environment: ${VENV_DIR}"

echo ""
echo "=== Dev Container Setup Complete ==="
echo "All Lua versions and tooling ready for specification comparison testing."

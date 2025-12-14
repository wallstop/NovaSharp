#!/usr/bin/env bash
# Post-create script for NovaSharp dev container
# Restores .NET tools and Python dependencies (Lua versions are pre-installed via Dockerfile)

set -euo pipefail

echo "=== NovaSharp Dev Container Setup ==="

# Restore dotnet tools
echo "Restoring .NET tools..."
cd /workspaces/NovaSharp
dotnet tool restore

# Install TUnit project templates for creating new test projects
echo "Installing TUnit templates..."
dotnet new install TUnit.Templates || true

# Install Python tooling dependencies
echo "Installing Python tooling dependencies..."
pip install --user -r requirements.tooling.txt

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
echo -n "Python: " && python3 --version 2>&1
echo -n "pip: " && pip --version 2>&1

echo ""
echo "=== Dev Container Setup Complete ==="
echo "All Lua versions and tooling ready for specification comparison testing."

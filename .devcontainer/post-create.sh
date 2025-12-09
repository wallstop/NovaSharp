#!/usr/bin/env bash
# Post-create script for NovaSharp dev container
# Installs all Lua versions (5.1, 5.2, 5.3, 5.4, 5.5) for specification comparison testing

set -euo pipefail

echo "=== NovaSharp Dev Container Setup ==="

# Install Lua 5.1-5.4 from apt
echo "Installing Lua 5.1-5.4 from apt..."
sudo apt-get update
sudo apt-get install -y lua5.1 lua5.2 lua5.3 lua5.4 python3-full build-essential libreadline-dev curl

# Build and install Lua 5.5 from source (RC phase, not in package managers yet)
echo "Building Lua 5.5 from source..."
LUA55_VERSION="5.5.0-rc2"
LUA55_URL="https://www.lua.org/work/lua-${LUA55_VERSION}.tar.gz"
LUA55_INSTALL_PREFIX="/usr/local/lua55"

# Create temp directory for build
BUILD_DIR=$(mktemp -d)
cd "$BUILD_DIR"

# Download and extract
echo "Downloading Lua ${LUA55_VERSION}..."
curl -L -o "lua-${LUA55_VERSION}.tar.gz" "$LUA55_URL"
tar xzf "lua-${LUA55_VERSION}.tar.gz"
cd "lua-${LUA55_VERSION}"

# Build for Linux
echo "Compiling Lua ${LUA55_VERSION}..."
make linux

# Install to dedicated prefix to avoid conflicts
echo "Installing Lua ${LUA55_VERSION} to ${LUA55_INSTALL_PREFIX}..."
sudo make install INSTALL_TOP="$LUA55_INSTALL_PREFIX"

# Create symlink for lua5.5 command
sudo ln -sf "${LUA55_INSTALL_PREFIX}/bin/lua" /usr/local/bin/lua5.5
sudo ln -sf "${LUA55_INSTALL_PREFIX}/bin/luac" /usr/local/bin/luac5.5

# Cleanup
cd /
rm -rf "$BUILD_DIR"

# Restore dotnet tools
echo "Restoring .NET tools..."
cd /workspaces/NovaSharp
dotnet tool restore

# Verify installations
echo ""
echo "=== Lua Version Verification ==="
echo -n "Lua 5.1: " && lua5.1 -v 2>&1 | head -1
echo -n "Lua 5.2: " && lua5.2 -v 2>&1 | head -1
echo -n "Lua 5.3: " && lua5.3 -v 2>&1 | head -1
echo -n "Lua 5.4: " && lua5.4 -v 2>&1 | head -1
echo -n "Lua 5.5: " && lua5.5 -v 2>&1 | head -1

echo ""
echo "=== Dev Container Setup Complete ==="
echo "All Lua versions installed and ready for specification comparison testing."

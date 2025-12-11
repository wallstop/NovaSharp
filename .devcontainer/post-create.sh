#!/usr/bin/env bash
# Post-create script for NovaSharp dev container
# Installs all Lua versions (5.1, 5.2, 5.3, 5.4, 5.5) for specification comparison testing
#
# Supported platforms:
#   - Linux (apt-based: Debian, Ubuntu)
#   - macOS (via Homebrew)
#   - Windows (via MSYS2/Git Bash - limited support)

set -euo pipefail

echo "=== NovaSharp Dev Container Setup ==="

# Detect operating system
detect_os() {
    case "$(uname -s)" in
        Linux*)     echo "linux";;
        Darwin*)    echo "macos";;
        CYGWIN*|MINGW*|MSYS*) echo "windows";;
        *)          echo "unknown";;
    esac
}

OS=$(detect_os)
echo "Detected OS: $OS"

# Install Lua 5.1-5.4 based on platform
install_lua_from_package_manager() {
    case "$OS" in
        linux)
            echo "Installing Lua 5.1-5.4 from apt..."
            sudo apt-get update
            sudo apt-get install -y lua5.1 lua5.2 lua5.3 lua5.4 build-essential libreadline-dev curl
            ;;
        macos)
            echo "Installing Lua 5.1-5.4 from Homebrew..."
            # Ensure Homebrew is available
            if ! command -v brew &> /dev/null; then
                echo "Error: Homebrew not found. Please install Homebrew first."
                exit 1
            fi
            # Install dependencies and Lua versions
            brew install readline curl
            # Note: Homebrew typically only has the latest Lua version
            # For multiple versions, we may need to build from source or use luaenv
            brew install lua@5.1 lua@5.3 lua@5.4 || true
            # Create versioned symlinks if they don't exist
            for ver in 5.1 5.3 5.4; do
                if [[ -f "/usr/local/opt/lua@${ver}/bin/lua" ]] && ! command -v "lua${ver}" &> /dev/null; then
                    sudo ln -sf "/usr/local/opt/lua@${ver}/bin/lua" "/usr/local/bin/lua${ver}"
                fi
                if [[ -f "/opt/homebrew/opt/lua@${ver}/bin/lua" ]] && ! command -v "lua${ver}" &> /dev/null; then
                    sudo ln -sf "/opt/homebrew/opt/lua@${ver}/bin/lua" "/usr/local/bin/lua${ver}"
                fi
            done
            ;;
        windows)
            echo "Windows detected. Please install Lua versions manually or via Chocolatey/Scoop."
            echo "Skipping package manager installation..."
            ;;
        *)
            echo "Unknown OS. Skipping package manager installation..."
            ;;
    esac
}

# Build Lua 5.5 from source (works on Linux and macOS)
build_lua55_from_source() {
    echo "Building Lua 5.5 from source..."
    
    # Version info - the tarball name includes "-rc2" but extracts to just the base version
    local LUA55_TARBALL_VERSION="5.5.0-rc2"
    local LUA55_EXTRACT_DIR="lua-5.5.0"  # Directory name after extraction (without -rc2)
    local LUA55_URL="https://www.lua.org/work/lua-${LUA55_TARBALL_VERSION}.tar.gz"
    local LUA55_INSTALL_PREFIX="/usr/local/lua55"

    # Determine make target based on OS
    local MAKE_TARGET
    case "$OS" in
        linux)
            MAKE_TARGET="linux"
            ;;
        macos)
            MAKE_TARGET="macosx"
            ;;
        windows)
            echo "Building Lua 5.5 from source is not supported on Windows via this script."
            echo "Please use pre-built binaries or build manually with MinGW/MSVC."
            return 1
            ;;
        *)
            echo "Unknown OS. Attempting generic build..."
            MAKE_TARGET="generic"
            ;;
    esac

    # Create temp directory for build
    local BUILD_DIR
    BUILD_DIR=$(mktemp -d)
    cd "$BUILD_DIR"

    # Download and extract
    echo "Downloading Lua ${LUA55_TARBALL_VERSION}..."
    curl -L -o "lua-${LUA55_TARBALL_VERSION}.tar.gz" "$LUA55_URL"
    tar xzf "lua-${LUA55_TARBALL_VERSION}.tar.gz"
    
    # Find the extracted directory (handle potential naming variations)
    local EXTRACTED_DIR
    if [[ -d "$LUA55_EXTRACT_DIR" ]]; then
        EXTRACTED_DIR="$LUA55_EXTRACT_DIR"
    elif [[ -d "lua-${LUA55_TARBALL_VERSION}" ]]; then
        EXTRACTED_DIR="lua-${LUA55_TARBALL_VERSION}"
    else
        # Fallback: find any lua-5.5* directory
        EXTRACTED_DIR=$(find . -maxdepth 1 -type d -name "lua-5.5*" | head -1)
        if [[ -z "$EXTRACTED_DIR" ]]; then
            echo "Error: Could not find extracted Lua 5.5 directory"
            ls -la
            return 1
        fi
    fi
    
    echo "Extracted to: $EXTRACTED_DIR"
    cd "$EXTRACTED_DIR"

    # Build for the detected platform
    echo "Compiling Lua 5.5 with target: ${MAKE_TARGET}..."
    make "$MAKE_TARGET"

    # Install to dedicated prefix to avoid conflicts
    echo "Installing Lua 5.5 to ${LUA55_INSTALL_PREFIX}..."
    sudo make install INSTALL_TOP="$LUA55_INSTALL_PREFIX"

    # Create symlinks for lua5.5 command
    sudo ln -sf "${LUA55_INSTALL_PREFIX}/bin/lua" /usr/local/bin/lua5.5
    sudo ln -sf "${LUA55_INSTALL_PREFIX}/bin/luac" /usr/local/bin/luac5.5

    # Cleanup
    cd /
    rm -rf "$BUILD_DIR"
    
    echo "Lua 5.5 installation complete."
}

# Install packages from package manager
install_lua_from_package_manager

# Build Lua 5.5 from source
if [[ "$OS" != "windows" ]]; then
    build_lua55_from_source
else
    echo "Skipping Lua 5.5 build on Windows. Please install manually."
fi

# Restore dotnet tools
echo "Restoring .NET tools..."
cd /workspaces/NovaSharp
dotnet tool restore

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
echo "All available Lua versions installed and ready for specification comparison testing."

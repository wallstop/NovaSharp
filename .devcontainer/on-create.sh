#!/usr/bin/env bash
# On-create script for NovaSharp dev container
# CRITICAL: This runs BEFORE VS Code extensions are activated
# Ensures NuGet packages are restored so C# Dev Kit doesn't get MSB1009 errors

set -euo pipefail

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           NovaSharp On-Create Setup (Pre-Extension)            ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

cd /workspaces/NovaSharp

# ============================================================================
# STEP 0: Configure C# extension to skip unnecessary downloads
# ============================================================================
# Create a marker file that tells the extension OmniSharp is already installed
# This prevents redundant download attempts during startup
echo "🔧 Step 0/4: Pre-configuring C# extension..."

# Find the C# extension directory (may not exist on first container creation)
CSHARP_EXT_DIR=$(find /home/vscode/.vscode-server/extensions -maxdepth 1 -name "ms-dotnettools.csharp-*" -type d 2>/dev/null | head -1 || true)

if [ -n "$CSHARP_EXT_DIR" ]; then
    # Create placeholder directories to prevent download attempts
    mkdir -p "$CSHARP_EXT_DIR/.omnisharp" 2>/dev/null || true
    mkdir -p "$CSHARP_EXT_DIR/.razoromnisharp" 2>/dev/null || true
    mkdir -p "$CSHARP_EXT_DIR/.razor" 2>/dev/null || true
    echo "   Pre-configured: $CSHARP_EXT_DIR"
else
    echo "   C# extension not yet installed (will be configured post-install)"
fi

# ============================================================================
# STEP 1: Clean stale build artifacts
# ============================================================================
echo ""
echo "📦 Step 1/4: Cleaning stale build artifacts..."

# Clean obj directories (NuGet restore cache with platform-specific paths)
obj_count=0
while IFS= read -r -d '' dir; do
    rm -rf "$dir"
    ((obj_count++)) || true
done < <(find src -type d -name "obj" -print0 2>/dev/null)

# Clean bin directories
bin_count=0
while IFS= read -r -d '' dir; do
    rm -rf "$dir"
    ((bin_count++)) || true
done < <(find src -type d -name "bin" -print0 2>/dev/null)

# Clean Visual Studio cache folder (contains Windows-specific paths and binary caches)
if [ -d "src/.vs" ]; then
    rm -rf "src/.vs"
    echo "   Cleaned: src/.vs (Visual Studio cache)"
fi

echo "   Cleaned: ${obj_count} obj/, ${bin_count} bin/ directories"

# ============================================================================
# STEP 2: Restore .NET tools
# ============================================================================
echo ""
echo "🔧 Step 2/4: Restoring .NET tools..."
dotnet tool restore --verbosity minimal

# ============================================================================
# STEP 3: Restore NuGet packages (CRITICAL - must complete before extensions load)
# ============================================================================
echo ""
echo "📥 Step 3/4: Restoring NuGet packages..."
echo "   (This must complete before C# Dev Kit activates)"
dotnet restore src/NovaSharp.sln --verbosity minimal

# ============================================================================
# STEP 4: Disable Razor component auto-download via extension settings
# ============================================================================
echo ""
echo "🔒 Step 4/4: Configuring extension settings..."

# Create user settings to ensure Razor downloads are disabled
# This supplements the devcontainer.json settings by setting them at the user level too
VSCODE_USER_SETTINGS="/home/vscode/.vscode-server/data/Machine/settings.json"
mkdir -p "$(dirname "$VSCODE_USER_SETTINGS")" 2>/dev/null || true

if [ ! -f "$VSCODE_USER_SETTINGS" ]; then
    cat > "$VSCODE_USER_SETTINGS" << 'EOF'
{
    "razor.disabled": true,
    "razor.server.suppressRazorDisabledMessage": true,
    "csharp.suppressRazorDesignTimeWarning": true
}
EOF
    echo "   Created: $VSCODE_USER_SETTINGS"
else
    echo "   Settings file already exists"
fi

echo ""
echo "✅ On-create complete - NuGet packages restored"
echo "   Extensions can now safely load the solution"
echo ""

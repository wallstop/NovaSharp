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
# STEP 0: Create placeholder for Razor OmniSharp to prevent extension errors
# ============================================================================
# The C# extension looks for .razoromnisharp directory even when Razor is disabled.
# Creating an empty directory prevents the "directory was not found" error.
CSHARP_EXT_DIR=$(find /home/vscode/.vscode-server/extensions -maxdepth 1 -name "ms-dotnettools.csharp-*" -type d 2>/dev/null | head -1)
if [ -n "$CSHARP_EXT_DIR" ] && [ ! -d "$CSHARP_EXT_DIR/.razoromnisharp" ]; then
    mkdir -p "$CSHARP_EXT_DIR/.razoromnisharp"
    echo "📁 Created placeholder: $CSHARP_EXT_DIR/.razoromnisharp"
fi

# ============================================================================
# STEP 1: Clean stale build artifacts
# ============================================================================
echo "📦 Step 1/3: Cleaning stale build artifacts..."

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
echo "🔧 Step 2/3: Restoring .NET tools..."
dotnet tool restore --verbosity minimal

# ============================================================================
# STEP 3: Restore NuGet packages (CRITICAL - must complete before extensions load)
# ============================================================================
echo ""
echo "📥 Step 3/3: Restoring NuGet packages..."
echo "   (This must complete before C# Dev Kit activates)"
dotnet restore src/NovaSharp.sln --verbosity minimal

echo ""
echo "✅ On-create complete - NuGet packages restored"
echo "   Extensions can now safely load the solution"
echo ""

#!/usr/bin/env bash
# On-create script for NovaSharp dev container
# CRITICAL: This runs BEFORE VS Code extensions are activated
# Ensures NuGet packages are restored so C# extension doesn't fail loading projects

set -euo pipefail

echo ""
echo "╔════════════════════════════════════════════════════════════════╗"
echo "║           NovaSharp On-Create Setup (Pre-Extension)            ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""

cd /workspaces/NovaSharp

# ============================================================================
# STEP 0: Pre-flight check
# ============================================================================
echo "🔧 Step 0/3: Pre-flight check..."
echo "   Working directory: $(pwd)"

# ============================================================================
# STEP 1: Clean stale build artifacts
# ============================================================================
echo ""
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
echo "   (This must complete before C# extension activates)"
dotnet restore src/NovaSharp.sln --verbosity minimal

echo ""
echo "✅ On-create complete - NuGet packages restored"
echo "   C# extension can now safely load the solution"
echo ""

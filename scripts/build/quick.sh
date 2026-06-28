#!/usr/bin/env bash

# Quick Build Script for NovaSharp
# Optimized for fast incremental compilation and developer feedback
#
# Usage:
#   ./scripts/build/quick.sh           # Quick incremental build (interpreter only)
#   ./scripts/build/quick.sh --all     # Build full solution
#   ./scripts/build/quick.sh --clean   # Clean before build
#   ./scripts/build/quick.sh -h        # Show help

set -euo pipefail

# Configuration
BUILD_ALL=0
CLEAN=0
CONFIGURATION="Release"
VERBOSITY="minimal"

# Paths (relative to repo root)
INTERPRETER_CSPROJ="src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj"
SOLUTION="src/NovaSharp.sln"

usage() {
    cat <<'EOF'
Quick Build Script for NovaSharp - Optimized for fast incremental compilation

Usage: ./scripts/build/quick.sh [OPTIONS]

Options:
  --all, -a          Build full solution (default: interpreter only)
  --clean, -c        Clean before building
  --debug, -d        Build in Debug configuration (default: Release)
  --verbose, -v      Show detailed build output
  -h, --help         Show this help message

Examples:
  ./scripts/build/quick.sh              # Fast incremental build (interpreter only)
  ./scripts/build/quick.sh --all        # Build entire solution incrementally
  ./scripts/build/quick.sh --clean      # Clean build from scratch
  ./scripts/build/quick.sh -d           # Debug configuration build

Performance Notes:
  - Default mode builds only the interpreter project for ~3-5x faster builds
  - Uses parallel compilation (-m flag) for multi-core systems
  - Skips restore if packages.lock.json unchanged (--no-restore with graph build)
  - Suppresses logo/copyright for cleaner output
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --all|-a)
            BUILD_ALL=1
            shift
            ;;
        --clean|-c)
            CLEAN=1
            shift
            ;;
        --debug|-d)
            CONFIGURATION="Debug"
            shift
            ;;
        --verbose|-v)
            VERBOSITY="normal"
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            usage
            exit 1
            ;;
    esac
done

# Resolve repo root
script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || echo "$(dirname "$(dirname "$script_dir")")")"
cd "$repo_root"

# Suppress noisy output
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Determine what to build
if (( BUILD_ALL )); then
    TARGET="$SOLUTION"
    TARGET_DESC="solution"
else
    TARGET="$INTERPRETER_CSPROJ"
    TARGET_DESC="interpreter"
fi

# Clean if requested
if (( CLEAN )); then
    echo "🧹 Cleaning $TARGET_DESC..."
    dotnet clean "$TARGET" -c "$CONFIGURATION" --verbosity quiet
fi

# Build with optimizations
echo "🔨 Building $TARGET_DESC ($CONFIGURATION)..."
start_time=$(date +%s%N)

# Build flags for speed:
# -m           : Parallel compilation (uses all cores)
# --no-restore : Skip restore if lock file unchanged (faster incremental)
# -clp:NoSummary;ForceNoAlign : Compact output
# -p:GenerateFullPaths=true : Better error navigation in editors

# Try incremental build first (no restore), fall back if needed
if ! dotnet build "$TARGET" \
    -c "$CONFIGURATION" \
    -m \
    --no-restore \
    --verbosity "$VERBOSITY" \
    -clp:NoSummary 2>/dev/null; then
    
    echo "📦 Restore needed, rebuilding with restore..."
    dotnet build "$TARGET" \
        -c "$CONFIGURATION" \
        -m \
        --verbosity "$VERBOSITY" \
        -clp:NoSummary
fi

end_time=$(date +%s%N)
elapsed_ms=$(( (end_time - start_time) / 1000000 ))

if (( elapsed_ms < 1000 )); then
    echo "✅ Build completed in ${elapsed_ms}ms"
elif (( elapsed_ms < 60000 )); then
    elapsed_s=$(( elapsed_ms / 1000 ))
    elapsed_frac=$(( (elapsed_ms % 1000) / 100 ))
    echo "✅ Build completed in ${elapsed_s}.${elapsed_frac}s"
else
    elapsed_m=$(( elapsed_ms / 60000 ))
    elapsed_s=$(( (elapsed_ms % 60000) / 1000 ))
    echo "✅ Build completed in ${elapsed_m}m ${elapsed_s}s"
fi

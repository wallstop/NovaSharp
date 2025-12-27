#!/usr/bin/env bash

# Quick Test Script for NovaSharp
# Optimized for fast test execution with filtering and parallel runs
#
# Usage:
#   ./scripts/test/quick.sh                        # Run all tests
#   ./scripts/test/quick.sh Floor                  # Filter by pattern (method name)
#   ./scripts/test/quick.sh --class Math           # Run classes matching pattern
#   ./scripts/test/quick.sh --method TestMethod    # Run methods matching pattern
#   ./scripts/test/quick.sh -h                     # Show help

set -euo pipefail

# Configuration
CONFIGURATION="Release"
CLASS_FILTER=""
METHOD_FILTER=""
BUILD=1

# Paths (relative to repo root)
TEST_PROJECT="src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj"

usage() {
    cat <<'EOF'
Quick Test Script for NovaSharp - Optimized for fast test execution

Usage: ./scripts/test/quick.sh [OPTIONS] [PATTERN]

Arguments:
  PATTERN              Quick filter pattern (matches method names containing PATTERN)

Options:
  --class, -c PATTERN  Filter by class name (classes containing PATTERN)
  --method, -m PATTERN Filter by method name (methods containing PATTERN)
  --no-build, -n       Skip build step (use with pre-built binaries)
  --full, -f           Force full build including test project (slow, use when tests changed)
  --debug, -d          Run Debug configuration tests
  --list               List all available tests
  -h, --help           Show this help message

Examples:
  ./scripts/test/quick.sh Floor                    # Methods containing "Floor"
  ./scripts/test/quick.sh -c MathModule            # All tests in classes containing "MathModule"
  ./scripts/test/quick.sh -m TestFloor             # Methods containing "TestFloor"
  ./scripts/test/quick.sh -c Math -m Floor         # Classes containing "Math" AND methods containing "Floor"
  ./scripts/test/quick.sh --full Floor             # Rebuild tests, then run matching "Floor"
  ./scripts/test/quick.sh --list                   # List all test names

Performance Tips:
  - Default mode only rebuilds interpreter (~5s), not tests (~40s)
  - Use --full when you've modified test code itself
  - Use --no-build when iterating on tests without code changes
  - Use specific filters to run fewer tests
  - Tests run in parallel by default (TUnit auto-parallelizes)

Filter Syntax:
  The script uses Microsoft.Testing.Platform's treenode-filter syntax internally.
  Pattern matching uses wildcards (*) for partial matches.
EOF
}

LIST_TESTS=0
FULL_BUILD=0

# Parse arguments
POSITIONAL=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --class|-c)
            CLASS_FILTER="$2"
            shift 2
            ;;
        --method|-m)
            METHOD_FILTER="$2"
            shift 2
            ;;
        --no-build|-n)
            BUILD=0
            shift
            ;;
        --full|-f)
            FULL_BUILD=1
            shift
            ;;
        --debug|-d)
            CONFIGURATION="Debug"
            shift
            ;;
        --list)
            LIST_TESTS=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        -*)
            echo "Unknown option: $1" >&2
            usage
            exit 1
            ;;
        *)
            POSITIONAL+=("$1")
            shift
            ;;
    esac
done

# Handle positional argument as method filter
if [[ ${#POSITIONAL[@]} -gt 0 ]]; then
    METHOD_FILTER="${POSITIONAL[0]}"
fi

# Resolve repo root
script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || echo "$(dirname "$(dirname "$script_dir")")")"
cd "$repo_root"

# Environment setup for speed
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export TESTINGPLATFORM_NOBANNER=1

# Ensure .NET 9 can run .NET 8 test host
if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
fi

# Build if requested
if (( BUILD )); then
    # Smart build strategy: only rebuild what's needed
    # The test project source gen is slow (~40s), but we can skip it if only runtime code changed
    INTERPRETER_CSPROJ="src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj"
    TEST_DLL="src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/bin/$CONFIGURATION/net8.0/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.dll"
    
    if (( FULL_BUILD )) || [[ ! -f "$TEST_DLL" ]]; then
        # Full build requested or no test DLL - need full build including test project (~50s)
        echo "🔨 Building solution (includes test project)..."
        if ! dotnet build "$TEST_PROJECT" -c "$CONFIGURATION" -m --no-restore --verbosity quiet 2>/dev/null; then
            dotnet build "$TEST_PROJECT" -c "$CONFIGURATION" -m --verbosity quiet
        fi
    else
        # Test DLL exists - just rebuild interpreter (fast ~5s)
        echo "🔨 Building interpreter only (test project cached, use --full to rebuild tests)..."
        if ! dotnet build "$INTERPRETER_CSPROJ" -c "$CONFIGURATION" -m --no-restore --verbosity quiet 2>/dev/null; then
            dotnet build "$INTERPRETER_CSPROJ" -c "$CONFIGURATION" -m --verbosity quiet
        fi
    fi
fi

# Handle list mode
if (( LIST_TESTS )); then
    echo "📋 Available tests:"
    dotnet run --project "$TEST_PROJECT" -c "$CONFIGURATION" --no-build -- --list-tests
    exit 0
fi

# Build the treenode filter
# Format: /assembly/namespace/class/method/arguments
# Use wildcards for partial matching

TREENODE_FILTER=""
if [[ -n "$CLASS_FILTER" && -n "$METHOD_FILTER" ]]; then
    # Both class and method filter: /*/*/*CLASS*/*METHOD*/**
    TREENODE_FILTER="/*/*/*${CLASS_FILTER}*/*${METHOD_FILTER}*/**"
    echo "🧪 Running tests: classes matching '*${CLASS_FILTER}*' AND methods matching '*${METHOD_FILTER}*'"
elif [[ -n "$CLASS_FILTER" ]]; then
    # Class filter only: /*/*/*CLASS*/**
    TREENODE_FILTER="/*/*/*${CLASS_FILTER}*/**"
    echo "🧪 Running tests: classes matching '*${CLASS_FILTER}*'"
elif [[ -n "$METHOD_FILTER" ]]; then
    # Method filter only: /*/*/*/*METHOD*/**
    TREENODE_FILTER="/*/*/*/*${METHOD_FILTER}*/**"
    echo "🧪 Running tests: methods matching '*${METHOD_FILTER}*'"
else
    echo "🧪 Running all tests..."
fi

# Run tests
start_time=$(date +%s%N)

set +e
if [[ -n "$TREENODE_FILTER" ]]; then
    dotnet run --project "$TEST_PROJECT" -c "$CONFIGURATION" --no-build -- --treenode-filter "$TREENODE_FILTER"
else
    dotnet run --project "$TEST_PROJECT" -c "$CONFIGURATION" --no-build
fi
test_result=$?
set -e

end_time=$(date +%s%N)
elapsed_ms=$(( (end_time - start_time) / 1000000 ))

# Report timing
if (( elapsed_ms < 1000 )); then
    time_str="${elapsed_ms}ms"
elif (( elapsed_ms < 60000 )); then
    time_s=$(( elapsed_ms / 1000 ))
    time_frac=$(( (elapsed_ms % 1000) / 100 ))
    time_str="${time_s}.${time_frac}s"
else
    time_m=$(( elapsed_ms / 60000 ))
    time_s=$(( (elapsed_ms % 60000) / 1000 ))
    time_str="${time_m}m ${time_s}s"
fi

if (( test_result == 0 )); then
    echo "✅ Tests passed in $time_str"
else
    echo "❌ Tests failed in $time_str (exit code: $test_result)"
fi

exit $test_result

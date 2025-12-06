#!/usr/bin/env bash
# scripts/benchmarks/run-benchmarks.sh
# Runs NovaSharp benchmarks with optional comparison suite.
# Usage: ./run-benchmarks.sh [--skip-comparison] [--configuration <Release|Debug>]

set -euo pipefail

CONFIGURATION="Release"
SKIP_COMPARISON=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-comparison)
            SKIP_COMPARISON=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -c)
            CONFIGURATION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Resolve repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$REPO_ROOT"

# Set roll-forward for .NET version compatibility
if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
    echo "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can execute the .NET 8 benchmarks."
fi

echo "Restoring local tools..."
dotnet tool restore > /dev/null

echo "Building solution (configuration: $CONFIGURATION)..."
dotnet build "src/NovaSharp.sln" -c "$CONFIGURATION"

run_benchmark() {
    local project="$1"
    local description="$2"
    
    echo ""
    echo "Running $description benchmarks..."
    dotnet run --project "$project" -c "$CONFIGURATION" --no-build
    echo "$description benchmarks complete."
}

run_benchmark "src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj" "NovaSharp runtime"

if [[ "$SKIP_COMPARISON" == "false" ]]; then
    run_benchmark "src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj" "comparison"
else
    echo ""
    echo "Skipping comparison benchmarks (per --skip-comparison)."
fi

ARTIFACTS_PATH="$REPO_ROOT/BenchmarkDotNet.Artifacts"
echo ""
echo "BenchmarkDotNet artifacts are available under:"
echo "  $ARTIFACTS_PATH"
echo ""
echo "Review the updated sections in docs/Performance.md and attach relevant artifacts when opening a PR."

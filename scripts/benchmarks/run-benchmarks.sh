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

run_python() {
    if [[ -n "${PYTHON:-}" ]]; then
        "$PYTHON" "$@"
        return
    fi

    if command -v python3 >/dev/null 2>&1; then
        python3 "$@"
        return
    fi

    if command -v python >/dev/null 2>&1; then
        python "$@"
        return
    fi

    echo "Python 3 is required to render the benchmark delta report. Set PYTHON to override." >&2
    exit 1
}

# Set roll-forward for .NET version compatibility
if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
    echo "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can execute the .NET 8 benchmarks."
fi

echo "Restoring local tools..."
dotnet tool restore

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

run_benchmark "src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj" "NovaSharp runtime"

COMPARISON_ARTIFACTS="artifacts/benchmarkdotnet/comparison"
LUA_CLI_SCENARIOS="artifacts/benchmarkdotnet/lua-cli-scenarios"
rm -rf "$COMPARISON_ARTIFACTS"
rm -rf "$LUA_CLI_SCENARIOS"

if [[ "$SKIP_COMPARISON" == "false" ]]; then
    mkdir -p "$COMPARISON_ARTIFACTS"

    echo ""
    echo "Running comparison benchmarks..."
    dotnet run \
        --project "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj" \
        -c "$CONFIGURATION" \
        --no-build \
        -- \
        --filter "*LuaPerformanceBenchmarks*" \
        --exporters json \
        --artifacts "$COMPARISON_ARTIFACTS"
    echo "comparison benchmarks complete."

    echo ""
    echo "Exporting comparison scenarios for reference lua CLI context..."
    dotnet run \
        --project "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj" \
        -c "$CONFIGURATION" \
        --no-build \
        -- \
        --export-scenarios "$LUA_CLI_SCENARIOS"

    echo ""
    echo "Measuring reference lua CLI wall-time context..."
    run_python scripts/benchmarks/run-lua-cli-context.py \
        --scenario-dir "$LUA_CLI_SCENARIOS" \
        --output-root "$COMPARISON_ARTIFACTS"
else
    echo ""
    echo "Skipping comparison benchmarks (per --skip-comparison)."
fi

echo ""
echo "Rendering benchmark comparison delta report..."
run_python scripts/benchmarks/render-benchmark-deltas.py \
    --current-root BenchmarkDotNet.Artifacts \
    --comparison-root artifacts/benchmarkdotnet/comparison \
    --self-baseline-root docs/performance-history/current-baseline \
    --output artifacts/benchmark-deltas.md

ARTIFACTS_PATH="$REPO_ROOT/BenchmarkDotNet.Artifacts"
COMPARISON_ARTIFACTS_PATH="$REPO_ROOT/artifacts/benchmarkdotnet/comparison"
echo ""
echo "BenchmarkDotNet artifacts are available under:"
echo "  $ARTIFACTS_PATH"
echo "Comparison BenchmarkDotNet artifacts are available under:"
echo "  $COMPARISON_ARTIFACTS_PATH"
echo "Benchmark comparison delta report:"
echo "  artifacts/benchmark-deltas.md"
echo ""
echo "Review the updated sections in docs/Performance.md and attach relevant artifacts when opening a PR."

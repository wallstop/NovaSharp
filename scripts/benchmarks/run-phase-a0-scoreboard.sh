#!/usr/bin/env bash
# Runs the Phase A0 comparison scoreboard without the broader runtime benchmark suite.
# Usage: ./scripts/benchmarks/run-phase-a0-scoreboard.sh [--configuration Release] [--output artifacts/phase-a0-scoreboard.md]

set -euo pipefail

CONFIGURATION="Release"
OUTPUT="artifacts/phase-a0-scoreboard.md"
PHASE_BASELINE="progress/benchmarks/phase-a0-scoreboard-baseline.json"
WRITE_PHASE_BASELINE=""
ENFORCE_PHASE_GATES=false
EXPECT_LUA_CLI=true

while [[ $# -gt 0 ]]; do
    case "$1" in
        --configuration|-c)
            CONFIGURATION="$2"
            shift 2
            ;;
        --output)
            OUTPUT="$2"
            shift 2
            ;;
        --phase-baseline)
            PHASE_BASELINE="$2"
            shift 2
            ;;
        --write-phase-baseline)
            WRITE_PHASE_BASELINE="$2"
            shift 2
            ;;
        --enforce-phase-gates)
            ENFORCE_PHASE_GATES=true
            shift
            ;;
        --skip-lua-cli)
            EXPECT_LUA_CLI=false
            shift
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
    esac
done

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

    echo "Python 3 is required to render the Phase A0 scoreboard. Set PYTHON to override." >&2
    exit 1
}

if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
    echo "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can execute the .NET 8 benchmarks."
fi

COMPARISON_ARTIFACTS="artifacts/benchmarkdotnet/phase-a0-comparison"
LUA_CLI_SCENARIOS="artifacts/benchmarkdotnet/phase-a0-lua-cli-scenarios"
rm -rf "$COMPARISON_ARTIFACTS" "$LUA_CLI_SCENARIOS"
mkdir -p "$COMPARISON_ARTIFACTS"

echo "Restoring local tools..."
dotnet tool restore

echo "Building comparison benchmarks (configuration: $CONFIGURATION)..."
dotnet build "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj" -c "$CONFIGURATION"

echo ""
echo "Running Phase A0 comparison benchmarks..."
dotnet run \
    --project "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj" \
    -c "$CONFIGURATION" \
    --no-build \
    -- \
    --filter "*" \
    --artifacts "$COMPARISON_ARTIFACTS"

if [[ "$EXPECT_LUA_CLI" == "true" ]]; then
    echo ""
    echo "Exporting Phase A0 scenarios for reference lua CLI context..."
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
fi

renderer_args=(
    --current-root BenchmarkDotNet.Artifacts
    --comparison-root "$COMPARISON_ARTIFACTS"
    --phase-baseline "$PHASE_BASELINE"
    --output "$OUTPUT"
)

if [[ "$EXPECT_LUA_CLI" == "true" ]]; then
    renderer_args+=(--expect-lua-cli)
fi

if [[ -n "$WRITE_PHASE_BASELINE" ]]; then
    renderer_args+=(--write-phase-baseline "$WRITE_PHASE_BASELINE")
fi

if [[ "$ENFORCE_PHASE_GATES" == "true" ]]; then
    renderer_args+=(--enforce-phase-gates)
fi

echo ""
echo "Rendering Phase A0 scoreboard..."
run_python scripts/benchmarks/render-benchmark-deltas.py "${renderer_args[@]}"

echo ""
echo "Phase A0 comparison artifacts:"
echo "  $COMPARISON_ARTIFACTS"
echo "Phase A0 scoreboard:"
echo "  $OUTPUT"

#!/usr/bin/env bash
# run-lua-fixtures-fast.sh - Fast parallel execution of Lua fixtures
#
# This script runs Lua fixture files against reference Lua interpreters
# using parallel execution for speed. NovaSharp comparison is done separately
# via a batch runner to avoid per-file process overhead.
#
# Usage:
#   ./scripts/tests/run-lua-fixtures-fast.sh [OPTIONS]
#
# Options:
#   --fixtures-dir <path>  Directory containing Lua fixtures
#   --output-dir <path>    Output directory for results
#   --lua-version <ver>    Lua version: 5.1, 5.2, 5.3, 5.4 (default: 5.4)
#   --jobs <n>             Parallel jobs (default: number of CPUs)
#   --skip-lua             Skip reference Lua execution
#   --skip-novasharp       Skip NovaSharp execution
#   --limit <n>            Limit files to process
#   --verbose              Show progress

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Defaults
FIXTURES_DIR="${ROOT_DIR}/src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/LuaFixtures"
OUTPUT_DIR="${ROOT_DIR}/artifacts/lua-comparison-results"
LUA_VERSION="5.4"
JOBS=$(nproc 2>/dev/null || echo 4)
SKIP_LUA=false
SKIP_NOVASHARP=false
LIMIT=0
VERBOSE=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --fixtures-dir) FIXTURES_DIR="$2"; shift 2 ;;
        --output-dir) OUTPUT_DIR="$2"; shift 2 ;;
        --lua-version) LUA_VERSION="$2"; shift 2 ;;
        --jobs|-j) JOBS="$2"; shift 2 ;;
        --skip-lua) SKIP_LUA=true; shift ;;
        --skip-novasharp) SKIP_NOVASHARP=true; shift ;;
        --limit) LIMIT="$2"; shift 2 ;;
        --verbose|-v) VERBOSE=true; shift ;;
        --help|-h)
            head -20 "${BASH_SOURCE[0]}" | grep '^#' | sed 's/^# //'
            exit 0
            ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done

LUA_CMD="lua${LUA_VERSION}"

if [[ ! -d "$FIXTURES_DIR" ]]; then
    echo "Error: Fixtures directory not found: $FIXTURES_DIR" >&2
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

# Create file list with version filtering
echo "Scanning fixtures for Lua $LUA_VERSION compatibility..."
file_list="${OUTPUT_DIR}/filelist.txt"
> "$file_list"

compatible_count=0
skipped_version=0
skipped_novasharp=0

while IFS= read -r -d '' lua_file; do
    # Check limit
    if [[ $LIMIT -gt 0 && $compatible_count -ge $LIMIT ]]; then
        break
    fi
    
    # Read first line for version info
    first_line=$(head -1 "$lua_file")
    
    # Skip NovaSharp-only
    if echo "$first_line" | grep -q "novasharp-only: true"; then
        ((skipped_novasharp++)) || true
        continue
    fi
    
    # Check version compatibility
    if echo "$first_line" | grep -q "@lua-versions:"; then
        # Extract versions
        if ! echo "$first_line" | grep -qE "(5\.1\+|${LUA_VERSION})"; then
            ((skipped_version++)) || true
            continue
        fi
    fi
    
    echo "$lua_file" >> "$file_list"
    ((compatible_count++)) || true
    
done < <(find "$FIXTURES_DIR" -name "*.lua" -type f -print0 | sort -z)

total_files=$(wc -l < "$file_list")
echo "Found $total_files compatible fixtures (skipped: $skipped_version version, $skipped_novasharp novasharp-only)"

if [[ $total_files -eq 0 ]]; then
    echo "No compatible files to process."
    exit 0
fi

# Function to run a single Lua file
run_single_lua() {
    local lua_file="$1"
    local lua_cmd="$2"
    local lua_version="$3"
    local output_dir="$4"
    local fixtures_dir="$5"
    
    local rel_path="${lua_file#$fixtures_dir/}"
    local output_base="${output_dir}/${rel_path%.lua}"
    mkdir -p "$(dirname "$output_base")"
    
    local out_file="${output_base}.lua${lua_version}.out"
    local err_file="${output_base}.lua${lua_version}.err"
    local rc_file="${output_base}.lua${lua_version}.rc"
    
    if timeout 5s "$lua_cmd" "$lua_file" > "$out_file" 2> "$err_file"; then
        echo "0" > "$rc_file"
        echo "pass"
    else
        echo "$?" > "$rc_file"
        echo "fail"
    fi
}
export -f run_single_lua

# Run reference Lua in parallel
if [[ "$SKIP_LUA" != "true" ]]; then
    if ! command -v "$LUA_CMD" &>/dev/null; then
        echo "Error: $LUA_CMD not found" >&2
        exit 1
    fi
    
    echo ""
    echo "Running $total_files fixtures against $LUA_CMD with $JOBS parallel jobs..."
    start_time=$(date +%s)
    
    # Use xargs for parallel execution (more portable than GNU parallel)
    lua_results="${OUTPUT_DIR}/lua_results.txt"
    cat "$file_list" | xargs -P "$JOBS" -I {} bash -c "run_single_lua '{}' '$LUA_CMD' '$LUA_VERSION' '$OUTPUT_DIR' '$FIXTURES_DIR'" > "$lua_results"
    
    lua_pass=$(grep -c "^pass$" "$lua_results" || echo 0)
    lua_fail=$(grep -c "^fail$" "$lua_results" || echo 0)
    
    end_time=$(date +%s)
    elapsed=$((end_time - start_time))
    echo "Lua $LUA_VERSION completed in ${elapsed}s: $lua_pass pass, $lua_fail fail"
fi

# Run NovaSharp using batch runner (much faster - single process)
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    echo ""
    echo "Running $total_files fixtures against NovaSharp (batch mode)..."
    
    BATCH_RUNNER="${ROOT_DIR}/src/tooling/WallstopStudios.NovaSharp.LuaBatchRunner/bin/Release/net8.0/WallstopStudios.NovaSharp.LuaBatchRunner.dll"
    if [[ ! -f "$BATCH_RUNNER" ]]; then
        echo "Building WallstopStudios.NovaSharp.LuaBatchRunner..."
        dotnet build "${ROOT_DIR}/src/tooling/WallstopStudios.NovaSharp.LuaBatchRunner/WallstopStudios.NovaSharp.LuaBatchRunner.csproj" -c Release -v q --nologo
    fi
    
    if [[ ! -f "$BATCH_RUNNER" ]]; then
        echo "Error: Batch runner not found at $BATCH_RUNNER" >&2
        exit 1
    fi
    
    start_time=$(date +%s)
    
    # Run all files in a single process using the batch runner
    # Pass --lua-version to ensure correct compatibility mode
    DOTNET_ROLL_FORWARD=Major dotnet "$BATCH_RUNNER" "$OUTPUT_DIR" --lua-version "$LUA_VERSION" --files-from "$file_list"
    
    # Read results from summary
    if [[ -f "${OUTPUT_DIR}/novasharp_summary.json" ]]; then
        nova_pass=$(python3 -c "import json; d=json.load(open('${OUTPUT_DIR}/novasharp_summary.json')); print(d.get('pass', 0))")
        nova_fail=$(python3 -c "import json; d=json.load(open('${OUTPUT_DIR}/novasharp_summary.json')); print(d.get('fail', 0))")
    fi
    
    end_time=$(date +%s)
    elapsed=$((end_time - start_time))
    echo "NovaSharp completed in ${elapsed}s"
fi

# Generate summary JSON
results_file="${OUTPUT_DIR}/results.json"
cat > "$results_file" << EOF
{
  "summary": {
    "lua_version": "$LUA_VERSION",
    "total_fixtures": $total_files,
    "skipped_version": $skipped_version,
    "skipped_novasharp": $skipped_novasharp,
    "lua_pass": ${lua_pass:-0},
    "lua_fail": ${lua_fail:-0},
    "nova_pass": ${nova_pass:-0},
    "nova_fail": ${nova_fail:-0}
  }
}
EOF

echo ""
echo "=== Summary ==="
echo "Lua $LUA_VERSION: ${lua_pass:-0} pass, ${lua_fail:-0} fail"
echo "NovaSharp: ${nova_pass:-0} pass, ${nova_fail:-0} fail"
echo "Results: $results_file"

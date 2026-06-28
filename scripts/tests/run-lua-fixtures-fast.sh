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
#   --lua-cmd <command>    Reference Lua executable to use
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
if nproc_path="$(command -v nproc)"; then
    JOBS="$("$nproc_path")"
else
    JOBS=4
fi
SKIP_LUA=false
SKIP_NOVASHARP=false
LIMIT=0
VERBOSE=false
LUA_CMD_OVERRIDE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --fixtures-dir) FIXTURES_DIR="$2"; shift 2 ;;
        --output-dir) OUTPUT_DIR="$2"; shift 2 ;;
        --lua-version) LUA_VERSION="$2"; shift 2 ;;
        --lua-cmd) LUA_CMD_OVERRIDE="$2"; shift 2 ;;
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

LUA_CMD="${LUA_CMD_OVERRIDE:-lua${LUA_VERSION}}"
TIMEOUT_MODE="python"
if timeout_path="$(command -v timeout)"; then
    timeout_version_output="$("$timeout_path" --version 2>&1 || true)"
    if [[ "$timeout_version_output" == *"GNU coreutils"* || "$timeout_version_output" == *"GNU timeout"* ]]; then
        TIMEOUT_MODE="gnu"
    fi
fi
if [[ "$TIMEOUT_MODE" == "python" ]]; then
    if ! python3_path="$(command -v python3)"; then
        echo "Error: python3 is required when GNU timeout is unavailable" >&2
        exit 1
    fi
fi

if [[ ! -d "$FIXTURES_DIR" ]]; then
    echo "Error: Fixtures directory not found: $FIXTURES_DIR" >&2
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

# Create file list with version filtering
echo "Scanning fixtures for Lua $LUA_VERSION compatibility..."
file_list="${OUTPUT_DIR}/filelist.txt"
reference_file_list="${OUTPUT_DIR}/filelist-reference.bin"
filter_summary="$(
    python3 - "$ROOT_DIR" "$FIXTURES_DIR" "$LUA_VERSION" "$LIMIT" "$file_list" "$reference_file_list" <<'PY'
import sys
from pathlib import Path

root = Path(sys.argv[1])
fixtures_dir = Path(sys.argv[2])
lua_version = sys.argv[3]
limit = int(sys.argv[4])
file_list = Path(sys.argv[5])
reference_file_list = Path(sys.argv[6])

sys.path.insert(0, str(root / "tools"))
from lua_version_utils import is_version_compatible, parse_lua_versions

compatible = 0
skipped_version = 0
skipped_novasharp = 0

with file_list.open("w", encoding="utf-8", newline="\n") as output, reference_file_list.open("wb") as reference_output:
    for lua_file in sorted(fixtures_dir.rglob("*.lua")):
        if limit > 0 and compatible >= limit:
            break

        lua_versions = []
        versions_specified = False
        no_reference_versions = False
        novasharp_only = False

        with lua_file.open("r", encoding="utf-8", errors="replace") as fixture:
            for _ in range(10):
                line = fixture.readline()
                if not line.startswith("--"):
                    break
                lower = line.lower()
                if "@lua-versions:" in lower:
                    versions_specified = True
                    versions_part = line.split("@lua-versions:", 1)[1].strip()
                    versions_text = versions_part.lower()
                    if versions_text == "none":
                        no_reference_versions = True
                    elif "novasharp-only" in versions_text:
                        novasharp_only = True
                    else:
                        lua_versions = parse_lua_versions(versions_part)
                if "@novasharp-only: true" in lower:
                    novasharp_only = True

        if novasharp_only:
            skipped_novasharp += 1
            continue
        if no_reference_versions or (
            versions_specified and not is_version_compatible(lua_versions, lua_version)
        ):
            skipped_version += 1
            continue

        relative_path = lua_file.relative_to(fixtures_dir).as_posix()
        output.write(lua_file.as_posix() + "\n")
        reference_output.write(lua_file.as_posix().encode("utf-8") + b"\0")
        reference_output.write(relative_path.encode("utf-8") + b"\0")
        compatible += 1

print(f"{compatible}\t{skipped_version}\t{skipped_novasharp}")
PY
)"
IFS=$'\t' read -r compatible_count skipped_version skipped_novasharp <<< "$filter_summary"

total_files=$(wc -l < "$file_list")
echo "Found $total_files compatible fixtures (skipped: $skipped_version version, $skipped_novasharp novasharp-only)"

if [[ $total_files -eq 0 ]]; then
    echo "No compatible files to process."
    exit 0
fi

overall_start_time=$(date +%s)

run_command_with_timeout() {
    local timeout_mode="$1"
    local timeout_seconds="$2"
    local out_file="$3"
    local err_file="$4"
    shift 4

    if [[ "$timeout_mode" == "gnu" ]]; then
        timeout "${timeout_seconds}s" "$@" > "$out_file" 2> "$err_file"
        return $?
    fi

    python3 -c '
import subprocess
import sys

timeout_seconds = float(sys.argv[1])
out_file = sys.argv[2]
err_file = sys.argv[3]
command = sys.argv[4:]

with open(out_file, "wb") as stdout, open(err_file, "wb") as stderr:
    try:
        result = subprocess.run(command, stdout=stdout, stderr=stderr, timeout=timeout_seconds)
    except subprocess.TimeoutExpired:
        stderr.write(f"Process timed out after {timeout_seconds:g}s\n".encode("utf-8"))
        sys.exit(124)

sys.exit(result.returncode)
' "$timeout_seconds" "$out_file" "$err_file" "$@"
}
export -f run_command_with_timeout

# Function to run a single Lua file
run_single_lua() {
    local lua_file="$1"
    local rel_path="$2"
    local lua_cmd="$3"
    local lua_version="$4"
    local output_dir="$5"
    local timeout_mode="$6"

    lua_file="${lua_file%$'\r'}"
    rel_path="${rel_path%$'\r'}"
    
    local output_base="${output_dir}/${rel_path%.lua}"
    mkdir -p "$(dirname "$output_base")"
    
    local out_file="${output_base}.lua${lua_version}.out"
    local err_file="${output_base}.lua${lua_version}.err"
    local rc_file="${output_base}.lua${lua_version}.rc"
    
    if run_command_with_timeout "$timeout_mode" 5 "$out_file" "$err_file" "$lua_cmd" "$lua_file"; then
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
    if ! lua_path="$(command -v "$LUA_CMD")"; then
        echo "Error: $LUA_CMD not found" >&2
        exit 1
    fi
    echo "Using Lua command: $lua_path"
    if [[ "$VERBOSE" == "true" ]]; then
        echo "Using timeout mode: $TIMEOUT_MODE"
    fi
    
    echo ""
    echo "Running $total_files fixtures against $LUA_CMD with $JOBS parallel jobs..."
    start_time=$(date +%s)
    
    # Use xargs for parallel execution (more portable than GNU parallel)
    lua_results="${OUTPUT_DIR}/lua_results.txt"
    xargs -0 -n 2 -P "$JOBS" bash -c 'run_single_lua "$5" "$6" "$1" "$2" "$3" "$4"' _ "$LUA_CMD" "$LUA_VERSION" "$OUTPUT_DIR" "$TIMEOUT_MODE" < "$reference_file_list" > "$lua_results"
    
    lua_pass=$(awk '$0 == "pass" { count++ } END { print count + 0 }' "$lua_results")
    lua_fail=$(awk '$0 == "fail" { count++ } END { print count + 0 }' "$lua_results")
    
    end_time=$(date +%s)
    elapsed=$((end_time - start_time))
    echo "Lua $LUA_VERSION completed in ${elapsed}s: $lua_pass pass, $lua_fail fail"
fi

# Run NovaSharp using batch runner (much faster - single process)
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    echo ""
    echo "Running $total_files fixtures against NovaSharp (batch mode)..."
    
    BATCH_RUNNER_DIR="${OUTPUT_DIR}/batch-runner"
    BATCH_RUNNER="${BATCH_RUNNER_DIR}/WallstopStudios.NovaSharp.LuaBatchRunner.dll"
    echo "Building WallstopStudios.NovaSharp.LuaBatchRunner..."
    dotnet build \
        "${ROOT_DIR}/src/tooling/WallstopStudios.NovaSharp.LuaBatchRunner/WallstopStudios.NovaSharp.LuaBatchRunner.csproj" \
        -c Release \
        -o "$BATCH_RUNNER_DIR" \
        -v q \
        --nologo
    
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
overall_end_time=$(date +%s)
overall_elapsed=$((overall_end_time - overall_start_time))
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
    "nova_fail": ${nova_fail:-0},
    "elapsed_seconds": $overall_elapsed,
    "workers": $JOBS
  }
}
EOF

echo ""
echo "=== Summary ==="
echo "Lua $LUA_VERSION: ${lua_pass:-0} pass, ${lua_fail:-0} fail"
echo "NovaSharp: ${nova_pass:-0} pass, ${nova_fail:-0} fail"
echo "Results: $results_file"

#!/usr/bin/env bash
# run-lua-fixtures.sh - Run Lua fixture files against reference Lua and NovaSharp
#
# This script runs Lua fixture files (with version metadata) against both
# the reference Lua interpreter and NovaSharp, respecting version compatibility.
#
# Usage:
#   ./scripts/tests/run-lua-fixtures.sh [OPTIONS]
#
# Options:
#   --fixtures-dir <path>  Directory containing Lua fixtures
#                          (default: src/tests/NovaSharp.Interpreter.Tests/LuaFixtures)
#   --output-dir <path>    Directory for comparison results
#                          (default: artifacts/lua-comparison-results)
#   --lua-version <ver>    Lua version to test: 5.1, 5.2, 5.3, 5.4 (default: 5.4)
#   --lua-cmd <cmd>        Override Lua command (default: lua<version>)
#   --skip-novasharp       Skip NovaSharp execution
#   --skip-lua             Skip reference Lua execution
#   --skip-incompatible    Skip files not compatible with selected Lua version
#   --limit <n>            Limit number of snippets to process
#   --verbose              Print detailed progress
#   --help                 Show this help message

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Defaults
FIXTURES_DIR="${ROOT_DIR}/src/tests/NovaSharp.Interpreter.Tests/LuaFixtures"
OUTPUT_DIR="${ROOT_DIR}/artifacts/lua-comparison-results"
LUA_VERSION="5.4"
LUA_CMD=""
NOVA_CMD=""
SKIP_NOVASHARP=false
SKIP_LUA=false
SKIP_INCOMPATIBLE=true
LIMIT=0
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --fixtures-dir)
            FIXTURES_DIR="$2"
            shift 2
            ;;
        --output-dir)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --lua-version)
            LUA_VERSION="$2"
            shift 2
            ;;
        --lua-cmd)
            LUA_CMD="$2"
            shift 2
            ;;
        --skip-novasharp)
            SKIP_NOVASHARP=true
            shift
            ;;
        --skip-lua)
            SKIP_LUA=true
            shift
            ;;
        --skip-incompatible)
            SKIP_INCOMPATIBLE=true
            shift
            ;;
        --include-incompatible)
            SKIP_INCOMPATIBLE=false
            shift
            ;;
        --limit)
            LIMIT="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --help)
            head -30 "${BASH_SOURCE[0]}" | grep '^#' | sed 's/^# //'
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
    esac
done

# Set default Lua command based on version
if [[ -z "$LUA_CMD" ]]; then
    LUA_CMD="lua${LUA_VERSION}"
fi

# Check if Lua file is compatible with the selected version
is_compatible() {
    local lua_file="$1"
    local version="$2"
    
    # Read @lua-versions line from file
    local versions_line
    versions_line=$(head -1 "$lua_file" | grep "@lua-versions:" || echo "")
    
    if [[ -z "$versions_line" ]]; then
        # No version info, assume compatible
        return 0
    fi
    
    # Check for novasharp-only
    if echo "$versions_line" | grep -q "novasharp-only"; then
        return 1
    fi
    
    # Check if version is in the list
    if echo "$versions_line" | grep -qE "(5\.1\+|$version)"; then
        return 0
    fi
    
    return 1
}

# Check if file expects an error
expects_error() {
    local lua_file="$1"
    grep -q "@expects-error: true" "$lua_file"
}

# Verify fixtures exist
if [[ ! -d "$FIXTURES_DIR" ]]; then
    echo "Error: Fixtures directory not found: $FIXTURES_DIR" >&2
    echo "Run 'python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py' first." >&2
    exit 1
fi

# Verify reference Lua is available
if [[ "$SKIP_LUA" != "true" ]] && ! command -v "$LUA_CMD" &>/dev/null; then
    echo "Error: Lua $LUA_VERSION not found ($LUA_CMD)." >&2
    echo "Install with 'sudo apt-get install lua$LUA_VERSION'" >&2
    exit 1
fi

# Build NovaSharp CLI if needed
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    CLI_PROJECT="${ROOT_DIR}/src/tooling/NovaSharp.Cli/NovaSharp.Cli.csproj"
    if [[ ! -f "$CLI_PROJECT" ]]; then
        echo "Error: NovaSharp CLI project not found: $CLI_PROJECT" >&2
        exit 1
    fi
    echo "Building NovaSharp CLI..."
    dotnet build "$CLI_PROJECT" -c Release -v q --nologo
    export DOTNET_ROLL_FORWARD=Major
    NOVA_CMD="dotnet run --project $CLI_PROJECT -c Release --framework net8.0 --no-build --"
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Results file
results_file="${OUTPUT_DIR}/results.json"

# Counters
total=0
compatible=0
skipped_version=0
skipped_novasharp=0
lua_pass=0
lua_fail=0
nova_pass=0
nova_fail=0

# Results array for JSON
declare -a results_array

# Find all Lua files
mapfile -t lua_files < <(find "$FIXTURES_DIR" -name "*.lua" -type f | sort)

echo "Found ${#lua_files[@]} Lua fixture files"
echo "Testing against Lua $LUA_VERSION ($LUA_CMD)"
echo "Output directory: $OUTPUT_DIR"
echo ""

for lua_file in "${lua_files[@]}"; do
    # Check limit
    if [[ $LIMIT -gt 0 && $total -ge $LIMIT ]]; then
        break
    fi
    
    ((total++)) || true
    
    # Get relative path
    rel_path="${lua_file#$FIXTURES_DIR/}"
    output_base="${OUTPUT_DIR}/${rel_path%.lua}"
    mkdir -p "$(dirname "$output_base")"
    
    # Check version compatibility
    if [[ "$SKIP_INCOMPATIBLE" == "true" ]] && ! is_compatible "$lua_file" "$LUA_VERSION"; then
        # Check if it's NovaSharp-only
        if head -2 "$lua_file" | grep -q "novasharp-only: true"; then
            ((skipped_novasharp++)) || true
        else
            ((skipped_version++)) || true
        fi
        
        if [[ "$VERBOSE" == "true" ]]; then
            echo "[$total] SKIP (incompatible): $rel_path"
        fi
        continue
    fi
    
    ((compatible++)) || true
    
    if [[ "$VERBOSE" == "true" ]]; then
        echo "[$total] Processing: $rel_path"
    fi
    
    lua_status="skipped"
    nova_status="skipped"
    file_expects_error="false"
    
    if expects_error "$lua_file"; then
        file_expects_error="true"
    fi
    
    # Run reference Lua
    if [[ "$SKIP_LUA" != "true" ]]; then
        lua_out="${output_base}.lua${LUA_VERSION}.out"
        lua_err="${output_base}.lua${LUA_VERSION}.err"
        lua_rc="${output_base}.lua${LUA_VERSION}.rc"
        
        if timeout 5s "$LUA_CMD" "$lua_file" > "$lua_out" 2> "$lua_err"; then
            echo "0" > "$lua_rc"
            lua_status="pass"
            ((lua_pass++)) || true
        else
            echo "$?" > "$lua_rc"
            lua_status="fail"
            ((lua_fail++)) || true
        fi
    fi
    
    # Run NovaSharp
    if [[ "$SKIP_NOVASHARP" != "true" ]]; then
        nova_out="${output_base}.nova.out"
        nova_err="${output_base}.nova.err"
        nova_rc="${output_base}.nova.rc"
        
        if timeout 10s $NOVA_CMD "$lua_file" > "$nova_out" 2> "$nova_err"; then
            echo "0" > "$nova_rc"
            nova_status="pass"
            ((nova_pass++)) || true
        else
            echo "$?" > "$nova_rc"
            nova_status="fail"
            ((nova_fail++)) || true
        fi
    fi
    
    # Add to results
    results_array+=("{\"file\":\"$rel_path\",\"lua_version\":\"$LUA_VERSION\",\"lua_status\":\"$lua_status\",\"nova_status\":\"$nova_status\",\"expects_error\":$file_expects_error}")
done

# Write results JSON
{
    echo '{"results": ['
    first_json=true
    for item in "${results_array[@]}"; do
        if [[ "$first_json" == "true" ]]; then
            first_json=false
            echo "    $item"
        else
            echo "    ,$item"
        fi
    done
    echo '  ],'
    cat << EOF
  "summary": {
    "lua_version": "$LUA_VERSION",
    "total": $total,
    "compatible": $compatible,
    "skipped_version": $skipped_version,
    "skipped_novasharp": $skipped_novasharp,
    "lua_pass": $lua_pass,
    "lua_fail": $lua_fail,
    "nova_pass": $nova_pass,
    "nova_fail": $nova_fail
  }
}
EOF
} > "$results_file"

echo ""
echo "=== Lua Fixture Test Summary ==="
echo "Lua version:           $LUA_VERSION ($LUA_CMD)"
echo "Total fixtures:        $total"
echo "Compatible:            $compatible"
echo "Skipped (version):     $skipped_version"
echo "Skipped (NovaSharp):   $skipped_novasharp"
if [[ "$SKIP_LUA" != "true" ]]; then
    echo "Lua $LUA_VERSION pass:        $lua_pass"
    echo "Lua $LUA_VERSION fail:        $lua_fail"
fi
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    echo "NovaSharp pass:        $nova_pass"
    echo "NovaSharp fail:        $nova_fail"
fi
echo ""
echo "Results written to: $results_file"

exit 0

#!/usr/bin/env bash
# run-lua-corpus.sh - Execute Lua corpus against both NovaSharp and reference Lua
#
# This script runs extracted Lua snippets against both the NovaSharp interpreter
# (via the CLI) and canonical Lua implementations, capturing output for comparison.
#
# Usage:
#   ./scripts/tests/run-lua-corpus.sh [OPTIONS]
#
# Options:
#   --corpus-dir <path>   Directory containing extracted Lua snippets
#                         (default: artifacts/lua-corpus)
#   --output-dir <path>   Directory for comparison results
#                         (default: artifacts/lua-corpus-results)
#   --lua-version <ver>   Lua version to use: 5.1, 5.2, 5.3, 5.4 (default: 5.4)
#   --lua-cmd <cmd>       Override Lua command (default: lua<version>)
#   --nova-cmd <cmd>      NovaSharp CLI command
#                         (default: dotnet run --project src/tooling/NovaSharp.Cli)
#   --skip-novasharp      Skip NovaSharp execution (reference Lua only)
#   --skip-lua            Skip reference Lua execution (NovaSharp only)
#   --limit <n>           Limit number of snippets to process
#   --verbose             Print detailed progress
#   --help                Show this help message

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Defaults
CORPUS_DIR="${ROOT_DIR}/artifacts/lua-corpus"
OUTPUT_DIR="${ROOT_DIR}/artifacts/lua-corpus-results"
LUA_VERSION="5.4"
LUA_CMD=""
NOVA_CMD=""
SKIP_NOVASHARP=false
SKIP_LUA=false
LIMIT=0
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --corpus-dir)
            CORPUS_DIR="$2"
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
        --nova-cmd)
            NOVA_CMD="$2"
            shift 2
            ;;
        --skip-novasharp)
            SKIP_NOVASHARP=true
            shift
            ;;
        --skip-lua|--skip-lua54)
            SKIP_LUA=true
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

# Set default Lua command based on version if not overridden
if [[ -z "$LUA_CMD" ]]; then
    LUA_CMD="lua${LUA_VERSION}"
fi

# Verify corpus exists
if [[ ! -d "$CORPUS_DIR" ]]; then
    echo "Error: Corpus directory not found: $CORPUS_DIR" >&2
    echo "Run 'python3 tools/LuaCorpusExtractor/lua_corpus_extractor.py' first." >&2
    exit 1
fi

# Verify reference Lua is available
if [[ "$SKIP_LUA" != "true" ]] && ! command -v "$LUA_CMD" &>/dev/null; then
    echo "Error: Lua $LUA_VERSION not found ($LUA_CMD)." >&2
    echo "Install with 'sudo apt-get install lua$LUA_VERSION'" >&2
    echo "Available versions: lua5.1, lua5.2, lua5.3, lua5.4" >&2
    exit 1
fi

# Build NovaSharp CLI if needed
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    if [[ -z "$NOVA_CMD" ]]; then
        CLI_PROJECT="${ROOT_DIR}/src/tooling/NovaSharp.Cli/NovaSharp.Cli.csproj"
        if [[ ! -f "$CLI_PROJECT" ]]; then
            echo "Error: NovaSharp CLI project not found: $CLI_PROJECT" >&2
            exit 1
        fi
        echo "Building NovaSharp CLI..."
        dotnet build "$CLI_PROJECT" -c Release -v q --nologo
        # NovaSharp CLI takes script path directly (not 'run' subcommand)
        # Use DOTNET_ROLL_FORWARD for .NET 9 compatibility with net8.0 target
        export DOTNET_ROLL_FORWARD=Major
        NOVA_CMD="dotnet run --project $CLI_PROJECT -c Release --framework net8.0 --no-build --"
    fi
fi

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Output file for results
results_file="${OUTPUT_DIR}/results.json"

# Counters
total=0
lua_pass=0
lua_fail=0
nova_pass=0
nova_fail=0
skipped=0

# Results array for JSON
declare -a results_array

# Find all Lua files in corpus
mapfile -t lua_files < <(find "$CORPUS_DIR" -name "*.lua" -type f | sort)

echo "Found ${#lua_files[@]} Lua snippets in corpus"
echo "Lua version: $LUA_VERSION ($LUA_CMD)"
echo "Output directory: $OUTPUT_DIR"
echo ""

for lua_file in "${lua_files[@]}"; do
    # Check limit
    if [[ $LIMIT -gt 0 && $total -ge $LIMIT ]]; then
        break
    fi
    
    ((total++)) || true
    
    # Get relative path for output naming
    rel_path="${lua_file#$CORPUS_DIR/}"
    output_base="${OUTPUT_DIR}/${rel_path%.lua}"
    mkdir -p "$(dirname "$output_base")"
    
    # Check for skip marker
    if grep -q "novasharp: skip-comparison" "$lua_file" 2>/dev/null; then
        ((skipped++)) || true
        if [[ "$VERBOSE" == "true" ]]; then
            echo "[$total] SKIP: $rel_path (NovaSharp-specific)"
        fi
        continue
    fi
    
    if [[ "$VERBOSE" == "true" ]]; then
        echo "[$total] Processing: $rel_path"
    fi
    
    lua_status="skipped"
    nova_status="skipped"
    
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
        
        # NovaSharp CLI takes script path directly
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
    
    # Append to results array (we'll fix JSON formatting at the end)
    results_array+=("{\"file\":\"$rel_path\",\"lua_version\":\"$LUA_VERSION\",\"lua_status\":\"$lua_status\",\"nova_status\":\"$nova_status\"}")
    
done

# Write results JSON properly
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
    "skipped": $skipped,
    "lua_pass": $lua_pass,
    "lua_fail": $lua_fail,
    "nova_pass": $nova_pass,
    "nova_fail": $nova_fail
  }
}
EOF
} > "$results_file"

echo ""
echo "=== Lua Corpus Execution Summary ==="
echo "Lua version:        $LUA_VERSION ($LUA_CMD)"
echo "Total snippets:     $total"
echo "Skipped:            $skipped"
if [[ "$SKIP_LUA" != "true" ]]; then
    echo "Lua $LUA_VERSION pass:       $lua_pass"
    echo "Lua $LUA_VERSION fail:       $lua_fail"
fi
if [[ "$SKIP_NOVASHARP" != "true" ]]; then
    echo "NovaSharp pass:     $nova_pass"
    echo "NovaSharp fail:     $nova_fail"
fi
echo ""
echo "Results written to: $results_file"

# Exit with error if any reference Lua tests failed
if [[ "$SKIP_LUA" != "true" && $lua_fail -gt 0 ]]; then
    echo ""
    echo "Warning: $lua_fail snippets failed on canonical Lua $LUA_VERSION"
    echo "Review failures to determine if they are NovaSharp-specific or test issues."
fi

exit 0

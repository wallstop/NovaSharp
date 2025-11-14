#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"

skip_build=false
configuration="Release"
minimum_interpreter_coverage="70.0"

usage() {
    cat <<'EOF'
Usage: ./scripts/coverage/coverage.sh [options]
    --skip-build                 Skip the initial dotnet build steps (assumes binaries already exist)
    -c|--configuration <name>    Build configuration (default: Release)
    --minimum-interpreter-coverage <value>
                                 Minimum NovaSharp.Interpreter line coverage percentage (default: 70.0)
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-build)
            skip_build=true
            shift
            ;;
        -c|--configuration)
            configuration="$2"
            shift 2
            ;;
        --minimum-interpreter-coverage)
            minimum_interpreter_coverage="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage
            exit 1
            ;;
    esac
done

log() {
    echo "[$(date '+%H:%M:%S')] $*"
}

error_exit() {
    echo "ERROR: $*" >&2
    exit 1
}

ensure_python() {
    if command -v python3 >/dev/null 2>&1; then
        echo "python3"
    elif command -v python >/dev/null 2>&1; then
        echo "python"
    else
        error_exit "Python is required to parse coverage summaries. Install python3 (preferred) or python."
    fi
}

should_emit_full_summary() {
    local override="${NOVASHARP_COVERAGE_SUMMARY:-}"
    if [[ -n "$override" ]]; then
        case "${override,,}" in
            1|true|yes) return 0 ;;
            0|false|no) return 1 ;;
        esac
    fi

    local markers=(CI GITHUB_ACTIONS TF_BUILD TEAMCITY_VERSION BUILD_BUILDID APPVEYOR)
    for marker in "${markers[@]}"; do
        if [[ -n "${!marker:-}" ]]; then
            return 0
        fi
    done

    return 1
}

pushd "$repo_root" >/dev/null

trap 'popd >/dev/null' EXIT

log "Restoring local dotnet tools..."
dotnet tool restore >/dev/null

coverage_root="$repo_root/artifacts/coverage"
mkdir -p "$coverage_root"
build_log_path="$coverage_root/build.log"
runner_project="src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj"

if [[ "$skip_build" != true ]]; then
    log "Building solution (configuration: $configuration)..."
    : > "$build_log_path"
    if ! dotnet build "src/NovaSharp.sln" -c "$configuration" 2>&1 | tee "$build_log_path"; then
        echo ""
        echo "dotnet build failed, tailing $build_log_path:"
        tail -n 200 "$build_log_path"
        error_exit "dotnet build src/NovaSharp.sln -c $configuration failed."
    fi

    log "Building test project (configuration: $configuration)..."
    if ! dotnet build "$runner_project" -c "$configuration" --no-restore 2>&1 | tee -a "$build_log_path"; then
        echo ""
        echo "dotnet build $runner_project failed, tailing $build_log_path:"
        tail -n 200 "$build_log_path"
        error_exit "dotnet build $runner_project -c $configuration failed."
    fi
fi

test_results_dir="$coverage_root/test-results"
mkdir -p "$test_results_dir"

runner_output="$repo_root/src/tests/NovaSharp.Interpreter.Tests/bin/$configuration/net8.0/NovaSharp.Interpreter.Tests.dll"
if [[ ! -f "$runner_output" ]]; then
    message="Runner output not found at '$runner_output'."
    if [[ "$skip_build" == true ]]; then
        message+=" Re-run without --skip-build or build the test project manually."
    else
        if [[ -f "$build_log_path" ]]; then
            message+=" Inspect $build_log_path for build errors."
        else
            message+=" dotnet build may have failed."
        fi
    fi
    error_exit "$message"
fi

coverage_base="$coverage_root/coverage"
report_target="$repo_root/docs/coverage/latest"

target_args=(
    test "$runner_project"
    -c "$configuration"
    --no-build
    --logger "trx;LogFileName=NovaSharpTests.trx"
    --results-directory "$test_results_dir"
)
joined_target_args="$(printf "%s " "${target_args[@]}")"
joined_target_args="${joined_target_args% }"

log "Collecting coverage via coverlet..."
dotnet tool run coverlet "$runner_output" \
    --target "dotnet" \
    --targetargs "$joined_target_args" \
    --format "lcov" \
    --format "cobertura" \
    --format "opencover" \
    --output "$coverage_base" \
    --include "[NovaSharp.*]*" \
    --exclude "[NovaSharp.*Tests*]*"

log "Generating report set..."
rm -rf "$report_target"
mkdir -p "$report_target"

cobertura_report="$coverage_base.cobertura.xml"
[[ -f "$cobertura_report" ]] || error_exit "Coverage report not found at '$cobertura_report'."

report_types="Html;TextSummary;MarkdownSummary;MarkdownSummaryGithub;JsonSummary"
dotnet tool run reportgenerator \
    "-reports:$cobertura_report" \
    "-targetdir:$report_target" \
    "-reporttypes:$report_types" \
    "-assemblyfilters:+NovaSharp.*"

summary_path="$report_target/Summary.txt"
if [[ -f "$summary_path" ]]; then
    echo ""
    if should_emit_full_summary; then
        cat "$summary_path"
    else
        printed_header=false
        while IFS= read -r line; do
            if [[ -z "$line" ]]; then
                $printed_header && break
            fi
            echo "$line"
            printed_header=true
        done < "$summary_path"
        echo ""
        echo "Detailed coverage summary saved to: $summary_path"
    fi
fi

echo ""
echo "Coverage artifacts:"
echo "  Raw: $coverage_root"
echo "  HTML: $report_target"

for filename in Summary.txt Summary.md SummaryGithub.md Summary.json; do
    src="$report_target/$filename"
    if [[ -f "$src" ]]; then
        cp "$src" "$coverage_root/$filename"
    fi
done

summary_github="$report_target/SummaryGithub.md"
if [[ -f "$summary_github" ]]; then
    echo "  Summary (GitHub): $summary_github"
fi

summary_json="$report_target/Summary.json"
python_cmd="$(ensure_python)"

if [[ -f "$summary_json" ]]; then
    interpreter_coverage="$("$python_cmd" - <<PY
import json, sys
with open("$summary_json", "r", encoding="utf-8") as fh:
    data = json.load(fh)
assemblies = data.get("coverage", {}).get("assemblies", [])
for asm in assemblies:
    if asm.get("name") == "NovaSharp.Interpreter":
        print(asm.get("coverage"))
        break
else:
    sys.stderr.write("Interpreter assembly not found in coverage summary.\n")
    sys.exit(1)
PY
)"

    echo ""
    printf "Interpreter line coverage: %.1f%%\n" "$interpreter_coverage"
    awk -v cov="$interpreter_coverage" -v min="$minimum_interpreter_coverage" 'BEGIN { exit (cov >= min) ? 0 : 1 }' || \
        error_exit "$(printf "NovaSharp.Interpreter line coverage %.1f%% is below the required %.1f%% threshold." "$interpreter_coverage" "$minimum_interpreter_coverage")"
fi

log "Coverage run completed."

#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi

skip_build=false
configuration="Release"
minimum_interpreter_coverage="70.0"
minimum_interpreter_branch_coverage="0"
minimum_interpreter_method_coverage="0"
coverage_gating_mode="${COVERAGE_GATING_MODE:-}"

usage() {
    cat <<'EOF'
Usage: ./scripts/coverage/coverage.sh [options]
    --skip-build                             Skip the initial dotnet build steps (assumes binaries already exist)
    -c|--configuration <name>                Build configuration (default: Release)
    --minimum-interpreter-coverage <value>   Minimum NovaSharp.Interpreter line coverage percentage (default: 70.0)
    --minimum-interpreter-branch-coverage <value>
                                             Minimum NovaSharp.Interpreter branch coverage percentage (default: 0)
    --minimum-interpreter-method-coverage <value>
                                             Minimum NovaSharp.Interpreter method coverage percentage (default: 0)
    --coverage-gating-mode <monitor|enforce> Override COVERAGE_GATING_MODE (monitor = warn at ≥95%, enforce = fail)
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
        --minimum-interpreter-branch-coverage)
            minimum_interpreter_branch_coverage="$2"
            shift 2
            ;;
        --minimum-interpreter-method-coverage)
            minimum_interpreter_method_coverage="$2"
            shift 2
            ;;
        --coverage-gating-mode)
            coverage_gating_mode="$2"
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

run_coverlet() {
    local runner_output="$1"
    local target_args="$2"
    local coverage_base="$3"
    local label="$4"
    shift 4
    local extra_args=("$@")

    log "Collecting coverage via coverlet ($label)..."

    local cmd=(
        dotnet tool run coverlet "$runner_output"
        --target "dotnet"
        --targetargs "$target_args"
        --format "lcov"
        --format "cobertura"
        --format "opencover"
        --output "$coverage_base"
        --include "[NovaSharp.*]*"
        --exclude "[NovaSharp.*Tests*]*"
    )

    if [[ ${#extra_args[@]} -gt 0 ]]; then
        cmd+=("${extra_args[@]}")
    fi

    "${cmd[@]}"
}

pushd "$repo_root" >/dev/null

trap 'popd >/dev/null' EXIT

if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD=Major
    log "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 runtimes can host net8 test runners."
fi

log "Restoring local dotnet tools..."
dotnet tool restore >/dev/null

coverage_root="$repo_root/artifacts/coverage"
mkdir -p "$coverage_root"
build_log_path="$coverage_root/build.log"
tunit_runner_project="src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj"
remote_runner_project="src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj"

if [[ "$skip_build" != true ]]; then
    log "Building solution (configuration: $configuration)..."
    : > "$build_log_path"
    if ! dotnet build "src/NovaSharp.sln" -c "$configuration" 2>&1 | tee "$build_log_path"; then
        echo ""
        echo "dotnet build failed, tailing $build_log_path:"
        tail -n 200 "$build_log_path"
        error_exit "dotnet build src/NovaSharp.sln -c $configuration failed."
    fi

    log "Building interpreter TUnit project (configuration: $configuration)..."
    if ! dotnet build "$tunit_runner_project" -c "$configuration" --no-restore 2>&1 | tee -a "$build_log_path"; then
        echo ""
        echo "dotnet build $tunit_runner_project failed, tailing $build_log_path:"
        tail -n 200 "$build_log_path"
        error_exit "dotnet build $tunit_runner_project -c $configuration failed."
    fi

    log "Building remote-debugger TUnit project (configuration: $configuration)..."
    if ! dotnet build "$remote_runner_project" -c "$configuration" --no-restore 2>&1 | tee -a "$build_log_path"; then
        echo ""
        echo "dotnet build $remote_runner_project failed, tailing $build_log_path:"
        tail -n 200 "$build_log_path"
        error_exit "dotnet build $remote_runner_project -c $configuration failed."
    fi
fi

test_results_dir="$coverage_root/test-results"
mkdir -p "$test_results_dir"
tunit_results_dir="$test_results_dir/tunit"
remote_results_dir="$test_results_dir/remote"
mkdir -p "$tunit_results_dir" "$remote_results_dir"

tunit_runner_output="$repo_root/src/tests/NovaSharp.Interpreter.Tests.TUnit/bin/$configuration/net8.0/NovaSharp.Interpreter.Tests.TUnit.dll"

tunit_message_prefix="TUnit runner output not found at '$tunit_runner_output'."
if [[ ! -f "$tunit_runner_output" ]]; then
    message="$tunit_message_prefix"
    if [[ "$skip_build" == true ]]; then
        message+=" Re-run without --skip-build or build the TUnit test project manually."
    else
        if [[ -f "$build_log_path" ]]; then
            message+=" Inspect $build_log_path for build errors."
        else
            message+=" dotnet build may have failed."
        fi
    fi
    error_exit "$message"
fi

remote_runner_output="$repo_root/src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/bin/$configuration/net8.0/NovaSharp.RemoteDebugger.Tests.TUnit.dll"
if [[ ! -f "$remote_runner_output" ]]; then
    message="Remote debugger runner output not found at '$remote_runner_output'."
    if [[ "$skip_build" == true ]]; then
        message+=" Re-run without --skip-build or build the TUnit test project manually."
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

tunit_coverage_base="${coverage_base}.tunit"
tunit_coverage_json="${tunit_coverage_base}.json"

tunit_target_args=(
    test "$tunit_runner_project"
    -c "$configuration"
    --no-build
    --logger "trx;LogFileName=NovaSharpTUnit.trx"
    --results-directory "$tunit_results_dir"
)
remote_target_args=(
    test "$remote_runner_project"
    -c "$configuration"
    --no-build
    --logger "trx;LogFileName=NovaSharpRemoteDebugger.trx"
    --results-directory "$remote_results_dir"
)
tunit_joined_target_args="$(printf "%s " "${tunit_target_args[@]}")"
tunit_joined_target_args="${tunit_joined_target_args% }"
remote_joined_target_args="$(printf "%s " "${remote_target_args[@]}")"
remote_joined_target_args="${remote_joined_target_args% }"

run_coverlet "$tunit_runner_output" "$tunit_joined_target_args" "$tunit_coverage_base" "Interpreter TUnit"

if [[ ! -f "$tunit_coverage_json" ]]; then
    error_exit "Coverage report not found at '$tunit_coverage_json' after running TUnit tests."
fi

run_coverlet "$remote_runner_output" "$remote_joined_target_args" "$coverage_base" "RemoteDebugger" --merge-with "$tunit_coverage_json"

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
    read -r interpreter_line interpreter_branch interpreter_method <<<"$("$python_cmd" - <<'PY'
import json, sys
from pathlib import Path
data = json.loads(Path("$summary_json").read_text(encoding="utf-8"))
assemblies = data.get("coverage", {}).get("assemblies", [])
for asm in assemblies:
    if asm.get("name") == "NovaSharp.Interpreter":
        print(
            asm.get("coverage", 0),
            asm.get("branchcoverage", 0),
            asm.get("methodcoverage", 0),
        )
        break
else:
    sys.stderr.write("Interpreter assembly not found in coverage summary.\n")
    sys.exit(1)
PY
)"

    echo ""
    printf "Interpreter line coverage: %.1f%%\n" "$interpreter_line"
    printf "Interpreter branch coverage: %.1f%%\n" "$interpreter_branch"
    printf "Interpreter method coverage: %.1f%%\n" "$interpreter_method"

    gating_mode="$(echo "${coverage_gating_mode,,}" | xargs)"
    line_threshold="$minimum_interpreter_coverage"
    branch_threshold="$minimum_interpreter_branch_coverage"
    method_threshold="$minimum_interpreter_method_coverage"
    enforce_thresholds=false

    if [[ "$gating_mode" == "monitor" || "$gating_mode" == "enforce" ]]; then
        line_threshold=$(awk -v a="$line_threshold" 'BEGIN { print (a > 95 ? a : 95) }')
        branch_threshold=$(awk -v a="$branch_threshold" 'BEGIN { print (a > 95 ? a : 95) }')
        method_threshold=$(awk -v a="$method_threshold" 'BEGIN { print (a > 95 ? a : 95) }')
        enforce_thresholds=[[ "$gating_mode" == "enforce" ]]
        echo ""
        printf "Coverage gating mode: %s (line ≥ %.1f%%, branch ≥ %.1f%%, method ≥ %.1f%%)\n" \
            "$gating_mode" "$line_threshold" "$branch_threshold" "$method_threshold"
    fi

    violations=()
    compare_threshold() {
        local value="$1"
        local threshold="$2"
        local label="$3"
        awk -v cov="$value" -v min="$threshold" 'BEGIN { exit (min <= 0 || cov >= min) ? 0 : 1 }'
        if [[ $? -ne 0 ]]; then
            violations+=("$(printf "%s coverage %.1f%% (threshold %.1f%%)" "$label" "$value" "$threshold")")
        fi
    }

    compare_threshold "$interpreter_line" "$line_threshold" "line"
    compare_threshold "$interpreter_branch" "$branch_threshold" "branch"
    compare_threshold "$interpreter_method" "$method_threshold" "method"

    if [[ ${#violations[@]} -gt 0 ]]; then
        message="NovaSharp.Interpreter coverage below threshold: ${violations[*]}"
        if [[ "$gating_mode" == "enforce" ]]; then
            error_exit "$message"
        else
            echo "WARNING: $message" >&2
        fi
    fi
fi

log "Coverage run completed."

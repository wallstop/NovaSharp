#!/usr/bin/env bash

set -euo pipefail

configuration="Release"
solution="src/NovaSharp.sln"
test_project="src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj"
run_tests=1
restore_tools=1
restore_packages=1
test_results_dir=""
test_results_placeholder=""

usage() {
    cat <<'EOF'
NovaSharp build helper.

Options:
  -c, --configuration <Name>   Build configuration (default: Release).
      --solution <Path>        Path to the solution/project to build (default: src/NovaSharp.sln).
      --test-project <Path>    Test project executed after building (default: interpreter tests).
      --skip-tests             Skip running the interpreter tests.
      --skip-tool-restore      Skip `dotnet tool restore`.
      --skip-restore           Skip `dotnet restore` and pass --no-restore to build/test.
  -h, --help                   Show this message.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            configuration="$2"
            shift 2
            ;;
        --solution)
            solution="$2"
            shift 2
            ;;
        --test-project)
            test_project="$2"
            shift 2
            ;;
        --skip-tests)
            run_tests=0
            shift
            ;;
        --skip-tool-restore)
            restore_tools=0
            shift
            ;;
        --skip-restore)
            restore_packages=0
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1"
            usage
            exit 1
            ;;
    esac
done

script_dir="$(cd -- "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir" && git rev-parse --show-toplevel 2>/dev/null || true)"
if [[ -z "$repo_root" ]]; then
    repo_root="$(dirname "$(dirname "$script_dir")")"
fi
cd "$repo_root"
test_results_dir="$repo_root/artifacts/test-results"
test_results_placeholder="$test_results_dir/.placeholder"

prepare_test_results_directory() {
    rm -rf "$test_results_dir"
    mkdir -p "$test_results_dir"
    cat <<'EOF' > "$test_results_placeholder"
NovaSharp test results were not generated. This placeholder file ensures CI artifact uploads succeed even when the test runner fails before emitting TRX output. Inspect the workflow logs for the full dotnet test failure details.
EOF
}

if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
    echo "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can run the net8 test runner."
fi

# Suppress TUnit ASCII banner and telemetry messages
export TESTINGPLATFORM_NOBANNER=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

if (( run_tests )); then
    prepare_test_results_directory
fi

if (( restore_tools )); then
    echo "Restoring local dotnet tools..."
    dotnet tool restore
fi

if (( restore_packages )); then
    echo "Restoring solution packages..."
    dotnet restore "$solution"
fi

echo "Building $solution (configuration: $configuration)..."
build_restore_flag=()
if (( ! restore_packages )); then
    build_restore_flag=(--no-restore)
fi
dotnet build "$solution" -c "$configuration" -m "${build_restore_flag[@]}"

if (( run_tests )); then
    echo "Running tests for $test_project..."
    test_restore_flag=()
    if (( ! restore_packages )); then
        test_restore_flag=(--no-restore)
    fi
    dotnet test --project "$test_project" -c "$configuration" --no-build \
        "${test_restore_flag[@]}" \
        --report-trx --report-trx-filename "NovaSharpInterpreterTUnit.trx" \
        --results-directory "$test_results_dir"
    rm -f "$test_results_placeholder"
fi

echo
echo "Build helper completed successfully."

#!/usr/bin/env bash

set -euo pipefail

configuration="Release"
solution="src/NovaSharp.sln"
test_project="src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj"
run_tests=1
restore_tools=1

usage() {
    cat <<'EOF'
NovaSharp build helper.

Options:
  -c, --configuration <Name>   Build configuration (default: Release).
      --solution <Path>        Path to the solution/project to build (default: src/NovaSharp.sln).
      --test-project <Path>    Test project executed after building (default: interpreter tests).
      --skip-tests             Skip running the interpreter tests.
      --skip-tool-restore      Skip `dotnet tool restore`.
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

if [[ -z "${DOTNET_ROLL_FORWARD:-}" ]]; then
    export DOTNET_ROLL_FORWARD="Major"
    echo "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can run the net8 test runner."
fi

if (( restore_tools )); then
    echo "Restoring local dotnet tools..."
    dotnet tool restore
fi

echo "Building $solution (configuration: $configuration)..."
dotnet build "$solution" -c "$configuration"

if (( run_tests )); then
    echo "Running tests for $test_project..."
    mkdir -p artifacts/test-results
    dotnet test "$test_project" -c "$configuration" --no-build \
        --logger "trx;LogFileName=NovaSharpTests.trx" \
        --results-directory artifacts/test-results
fi

echo
echo "Build helper completed successfully."

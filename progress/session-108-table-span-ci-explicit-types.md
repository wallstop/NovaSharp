# Session 108 - Table Span CI Explicit Types

## Baseline

- Branch: `dev/wallstop/api-perf`
- Failing pushed head: `6f42e657`
- PR: `#43`

## Failure

The `benchmark` GitHub Actions job failed before running benchmarks. Its solution build treated IDE0008 as an error in `TableTUnitTests.cs`:

- `SpanNestedKeyPathsAccessSlicesAndMutateNestedValues`
- `SpanNestedKeyPathsTreatEmptyAndDefaultLikeArrayPath`
- `SpanNestedTerminalNullMatchesArrayPath`
- `SpanNestedPathErrorsIncludeOffendingKey`

The failing declarations used `var` for named tuple results returned by synchronous span helper methods.

## Diagnosis

This is a test-source style/build issue, not a production behavior failure and not a benchmark runtime failure. Local quick build/test did not catch it because the CI benchmark workflow builds the full solution with analyzer settings that report IDE0008 as an error.

## Planned Fix

1. [x] Replace the four `var` declarations with explicit named tuple types.
2. [x] Run a local full solution build to match the failed CI path.
3. [x] Re-run relevant local tests.
4. [x] Run pre-commit.
5. [x] Prepare the local CI-fix commit.
6. [ ] Push, request Copilot review again, and poll PR CI.

## Validation

- `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings.
- `./scripts/test/quick.sh Table`: passed, 642 tests.
- `bash ./scripts/dev/pre-commit.sh`: completed successfully. It emitted existing documentation/skill metadata warnings, with 0 reported errors.

## Status

In progress.

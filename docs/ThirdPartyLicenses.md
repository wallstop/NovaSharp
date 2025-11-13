# Third-Party Licenses and Test Fixtures

This document summarises the third-party components bundled with NovaSharp and the licensing terms that apply to them. Keep it up to date whenever dependencies change or new external test assets are introduced.

> **Status:** Draft (2025‑11‑12). Update this file before shipping a release or adding new external corpora.

## Runtime / Tooling Dependencies

| Component | Version / Commit | License | Notes |
| --- | --- | --- | --- |
| .NET SDK | 9.0.x | MIT | Required for building/testing (listed in global.json if pinned). |
| NUnit | 2.6.x | NUnit License (BSD-like) | Unit test framework used across `src/tests/NovaSharp.Interpreter.Tests`. |
| Coverlet | Refer to `dotnet tool list` output | MIT | Code coverage instrumentation. |
| ReportGenerator | Refer to `dotnet tool list` output | MIT | Coverage report generation. |
| BenchmarkDotNet | Refer to csproj | MIT | Used in benchmarking projects. |

> For an authoritative list, run `dotnet list package --include-transitive` in each project and refresh this table with the license declared by the package authors.

## Test Fixtures / Real-World Corpora

| Source | Location | License | Usage | Notes |
| --- | --- | --- | --- | --- |
| Lua TAP suites (`TestMore/*`) | `src/tests/NovaSharp.Interpreter.Tests/TestMore` | MIT (Lua.org) | Interpreter parity tests | Mirrors upstream Lua 5.4 TAP fixtures. |
| `json.lua` (rxi) | `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/rxi-json` | MIT | Real-world corpus (JSON encode/decode regression guard) | Tag v0.1.2 (`d1e3b0f5d0f3d3493c7dadd0bb54135507fcebd7`). |
| `inspect.lua` (kikito) | `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/kikito-inspect` | MIT | Real-world corpus (table formatting regression guard) | Tag v3.1.0 (`2cc61aa5a98ea852e48fd28d39de7b335ac983c6`). |

When adding new scripts:
1. Confirm the license permits redistribution in a test context (MIT/BSD/Apache/CC0/Unlicense preferred).
2. Copy the license text (or attribution notice) into `src/tests/NovaSharp.Interpreter.Tests/Fixtures/RealWorld/<corpusName>/LICENSE`.
3. Record the source URL / commit hash and the file path in the table above.

## Updating This File

- After upgrading NuGet dependencies, add or update rows with version numbers and licenses.
- When adding third-party content (images, docs, scripts), note the license and usage rationale.
- Link to SPDX identifiers where possible (e.g., MIT). Websites like [https://spdx.org/licenses](https://spdx.org/licenses) can help confirm identifiers.

For contributors: if you’re unsure about the license compatibility of a dependency or fixture, open a discussion before merging the change. Ensuring the repository remains compliant with permissive licensing is a shared responsibility.

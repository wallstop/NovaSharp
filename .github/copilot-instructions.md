# Copilot Instructions for NovaSharp

**See [`.llm/context.md`](../.llm/context.md) for all AI assistant guidelines.**

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave version control to the human
1. **NEVER use absolute paths** — Use relative paths from repo root only
1. **NEVER discard output** — **NO redirects or pipes to `/dev/null`** (`>/dev/null`, `2>/dev/null`, `&>/dev/null`, `| cat >/dev/null`, etc.), **even in chained commands** (`cmd1 2>/dev/null && cmd2`). Command output is essential for debugging. If a command produces too much output, use `--quiet` flags or filter with `grep`, but NEVER silently discard stderr.
1. **Lua Spec Compliance** — Fix production code when it differs from reference Lua, never tests
1. **Always create `.lua` test files** — Every test/fix needs standalone Lua fixtures for cross-interpreter verification
1. **Multi-Version Testing** — All tests must run across Lua 5.1, 5.2, 5.3, 5.4, 5.5; include positive AND negative tests for version-specific features

## 🔴 Build & Test Commands

**ALWAYS use the quick scripts** for fast, consistent builds and tests:

```bash
# Build (interpreter only, fast incremental)
./scripts/build/quick.sh

# Build full solution
./scripts/build/quick.sh --all

# Run all tests
./scripts/test/quick.sh

# Run tests matching pattern (method names containing "Floor")
./scripts/test/quick.sh Floor

# Run tests in specific class
./scripts/test/quick.sh -c MathModule

# Combined: class AND method filter
./scripts/test/quick.sh -c Math -m Floor

# Skip build when iterating on tests
./scripts/test/quick.sh --no-build Floor

# List all available tests
./scripts/test/quick.sh --list
```

**⚠️ Do NOT use raw `dotnet build` or `dotnet test` commands** — the quick scripts handle all the correct flags and project paths.

For human contributors, see [`docs/Contributing.md`](../docs/Contributing.md).

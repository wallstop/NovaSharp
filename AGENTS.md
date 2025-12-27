# Repository Guidelines

**See [`.llm/context.md`](.llm/context.md) for all AI assistant guidelines.**

## 🔴 Priority Hierarchy (NEVER Violate)

NovaSharp follows a strict priority order:

| Priority | Concern | Description |
|----------|---------|-------------|
| **1. CORRECTNESS** | Lua Spec Compliance | Behavior MUST match reference Lua exactly |
| **2. SPEED** | Runtime Performance | Execute Lua code as fast as possible |
| **3. MEMORY** | Minimal Allocations | Zero-allocation hot paths, aggressive pooling |
| **4. UNITY** | Platform Compatibility | IL2CPP/AOT, Mono, no runtime code generation |
| **5. CLARITY** | Maintainability | Clean architecture, readability |

**The Iron Rule**: A performance optimization that breaks Lua spec compliance is REJECTED. See [`.llm/skills/correctness-then-performance.md`](.llm/skills/correctness-then-performance.md).

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave version control to the human
2. **NEVER use absolute paths** — Use relative paths from repo root only
3. **NEVER discard output** — **NO redirects or pipes to `/dev/null`** (`>/dev/null`, `2>/dev/null`, `&>/dev/null`, `| cat >/dev/null`, etc.), **even in chained commands** (`cmd1 2>/dev/null && cmd2`). Command output is essential for debugging. If a command produces too much output, use `--quiet` flags or filter with `grep`, but NEVER silently discard stderr.
4. **Lua Spec Compliance is HIGHEST PRIORITY** — Fix production code when it differs from reference Lua, never tests. See [`.llm/skills/correctness-then-performance.md`](.llm/skills/correctness-then-performance.md)
5. **Maximum Performance** — After correctness is verified, optimize aggressively. All hot paths must be zero-allocation. See [`.llm/skills/high-performance-csharp.md`](.llm/skills/high-performance-csharp.md)
6. **Zero-Flaky Test Policy** — Every test failure indicates a **real bug** (production or test). NEVER skip, disable, ignore, or "make tests pass" without full root cause investigation. See [`.llm/skills/test-failure-investigation.md`](.llm/skills/test-failure-investigation.md)
7. **Always create BOTH C# tests AND `.lua` fixtures** — Every test/fix needs: (1) TUnit C# tests for NovaSharp runtime, (2) standalone `.lua` fixtures for cross-interpreter verification, (3) regenerate corpus with `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`
8. **Multi-Version Testing** — All tests must run across Lua 5.1, 5.2, 5.3, 5.4, 5.5; include positive AND negative tests for version-specific features
9. **Lua Fixture Metadata** — ONLY use `@lua-versions`, `@novasharp-only`, `@expects-error`. Fields like `@min-version`, `@max-version`, `@versions`, `@name`, `@description` are **NOT parsed** by the harness and will be silently ignored
10. **Exhaustive Test Coverage** — Every feature/bugfix needs comprehensive tests: normal cases, edge cases, error cases, negative tests, "the impossible". Use data-driven tests. See [`.llm/skills/exhaustive-test-coverage.md`](.llm/skills/exhaustive-test-coverage.md)
11. **Documentation & Changelog** — Every user-facing change requires: (1) updated XML docs and code comments, (2) updated markdown docs with CORRECT code samples, (3) CHANGELOG.md entry in [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format. See [`.llm/skills/documentation-and-changelog.md`](.llm/skills/documentation-and-changelog.md)
12. **Defensive Programming** — Production code must be robust and resilient. Handle all errors gracefully, never throw exceptions except for truly exceptional cases. See [`.llm/skills/defensive-programming.md`](.llm/skills/defensive-programming.md)
13. **Pre-Commit Validation** — Work is NOT complete until `bash ./scripts/dev/pre-commit.sh` passes. This runs all formatters, linters, and checks. A diff that fails CI is not ready for review. See [`.llm/skills/pre-commit-validation.md`](.llm/skills/pre-commit-validation.md)

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

For human contributors, see [`docs/Contributing.md`](docs/Contributing.md).

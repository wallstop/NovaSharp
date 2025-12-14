# CLAUDE.md

**See [`CONTRIBUTING_AI.md`](CONTRIBUTING_AI.md) for all AI assistant guidelines.**

## 🔴 Critical Rules

1. **NEVER `git add` or `git commit`** — Leave version control to the human
2. **NEVER use absolute paths** — Use relative paths from repo root only
3. **Lua Spec Compliance** — Fix production code when it differs from reference Lua, never tests
4. **Always create `.lua` test files** — Every test/fix needs standalone Lua fixtures for cross-interpreter verification
5. **Multi-Version Testing** — All tests must run across Lua 5.1, 5.2, 5.3, 5.4, 5.5; include positive AND negative tests for version-specific features

For human contributors, see [`docs/Contributing.md`](docs/Contributing.md).

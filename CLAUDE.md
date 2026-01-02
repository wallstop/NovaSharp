# CLAUDE.md

**Complete documentation: [`.llm/context.md`](.llm/context.md)**

## Quick Reference

| Priority | Concern | Never Sacrifice For Lower Priority |
| -------- | ------- | ---------------------------------- |
| 1. CORRECTNESS | Lua Spec Compliance | Performance, memory, clarity |
| 2. SPEED | Runtime Performance | Memory, clarity |
| 3. MEMORY | Minimal Allocations | Clarity |
| 4. UNITY | Platform Compatibility | Clarity |
| 5. CLARITY | Maintainability | - |

## Critical Rules

1. **NEVER `git add` or `git commit`** - Leave version control to humans
2. **NEVER use absolute paths** - Relative paths from repo root only
3. **NEVER discard output** - No `>/dev/null` or `2>/dev/null`
4. **Lua Spec = Source of Truth** - Fix NovaSharp, never tests
5. **Multi-Version Testing** - All tests run on Lua 5.1-5.5
6. **Lua Fixture Metadata** - ONLY `@lua-versions`, `@novasharp-only`, `@expects-error`

## Build & Test

```bash
./scripts/build/quick.sh          # Build interpreter
./scripts/build/quick.sh --all    # Build full solution
./scripts/test/quick.sh           # Run all tests
./scripts/test/quick.sh Floor     # Filter by method pattern
./scripts/test/quick.sh -c Math   # Filter by class
./scripts/test/quick.sh -c Math -m Floor  # Combined filter
./scripts/test/quick.sh --no-build Floor  # Skip build
```

Do NOT use raw `dotnet build` or `dotnet test` - use quick scripts.

## Skills

See [`.llm/skills/`](.llm/skills/) for task-specific guides:

- [correctness-then-performance](.llm/skills/correctness-then-performance.md) - Priority hierarchy
- [high-performance-csharp](.llm/skills/high-performance-csharp.md) - Zero-allocation patterns
- [tunit-test-writing](.llm/skills/tunit-test-writing.md) - Writing tests
- [lua-fixture-creation](.llm/skills/lua-fixture-creation.md) - Creating .lua fixtures

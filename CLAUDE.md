# CLAUDE.md

**Complete documentation: [`.llm/context.md`](.llm/context.md)**

## Quick Reference

| Priority       | Concern                | Never Sacrifice For Lower Priority |
| -------------- | ---------------------- | ---------------------------------- |
| 1. CORRECTNESS | Lua Spec Compliance    | Performance, memory, clarity       |
| 2. SPEED       | Runtime Performance    | Memory, clarity                    |
| 3. MEMORY      | Minimal Allocations    | Clarity                            |
| 4. UNITY       | Platform Compatibility | Clarity                            |
| 5. CLARITY     | Maintainability        | -                                  |

## Critical Rules

1. **Scoped Git Operations Allowed When Requested** - If the user asks for commits, pushes, or PR/CI work, agents may run `git add`, `git commit`, and `git push` after reviewing the diff. Use small scoped commits and never use destructive git commands unless explicitly requested.
1. **NEVER use absolute paths** - Relative paths from repo root only
1. **Do not discard diagnostic output in ad-hoc commands** - Repo helper scripts may intentionally quiet noisy tools, but failures must surface actionable output.
1. **Lua Spec = Source of Truth** - Fix NovaSharp, never tests
1. **Multi-Version Testing** - All tests run on Lua 5.1-5.5
1. **Lua Fixture Metadata** - ONLY `@lua-versions`, `@novasharp-only`, `@expects-error`
1. **Pre-Commit Validation Allowed** - `bash ./scripts/dev/pre-commit.sh` is expected before commits and may restage files it auto-formats or regenerates.
1. **No false green-lighting** - Only say `green`, `verified`, `passes`, or `complete` after the exact local checks and PR CI were observed passing. Otherwise report the check as `not run` or failing residual risk.

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
Before closing behavior or CI work, run the relevant targeted tests, `./scripts/build/quick.sh`, `./scripts/test/quick.sh`, formatting, Lua comparison when behavior changes, then poll PR CI until green or document the newly diagnosed failure.

## Skills

See [`.llm/skills/`](.llm/skills/) for task-specific guides:

- [correctness-then-performance](.llm/skills/correctness-then-performance.md) - Priority hierarchy
- [high-performance-csharp](.llm/skills/high-performance-csharp.md) - Zero-allocation patterns
- [tunit-test-writing](.llm/skills/tunit-test-writing.md) - Writing tests
- [lua-fixture-creation](.llm/skills/lua-fixture-creation.md) - Creating .lua fixtures

# GitHub Copilot Instructions for NovaSharp

**Complete documentation: [`../.llm/context.md`](../.llm/context.md)**

## Quick Reference

| Priority       | Concern                | Never Sacrifice For Lower Priority |
| -------------- | ---------------------- | ---------------------------------- |
| 1. CORRECTNESS | Lua Spec Compliance    | Performance, memory, clarity       |
| 2. SPEED       | Runtime Performance    | Memory, clarity                    |
| 3. MEMORY      | Minimal Allocations    | Clarity                            |
| 4. UNITY       | Platform Compatibility | Clarity                            |
| 5. CLARITY     | Maintainability        | -                                  |

## Critical Rules

1. **NEVER `git add` or `git commit`** - Leave version control to humans
1. **NEVER use absolute paths** - Relative paths from repo root only
1. **Lua Spec = Source of Truth** - Fix NovaSharp, never tests
1. **Multi-Version Testing** - All tests run on Lua 5.1-5.5
1. **Lua Fixture Metadata** - ONLY `@lua-versions`, `@novasharp-only`, `@expects-error`

## Build & Test

```bash
./scripts/build/quick.sh          # Build interpreter
./scripts/test/quick.sh           # Run all tests
./scripts/test/quick.sh Floor     # Filter by pattern
```

Do NOT use raw `dotnet build` or `dotnet test`.

## Skills

See [`../.llm/skills/`](../.llm/skills/) for task-specific guides.

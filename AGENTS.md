# NovaSharp engineering policy

1. Preserve exact Lua 5.1–5.5 behavior first, then runtime speed, allocations, Unity compatibility (Mono and IL2CPP/AOT), and clarity, in that order. Resolve semantic disputes with the matching reference Lua. Fix the implementation when it differs from the oracle.
1. Host APIs may change. Update repository-owned callers atomically and remove superseded APIs without compatibility shims.
1. Use repository-relative paths. Preserve actionable diagnostics. Investigate every test failure.
1. Scope material work to one falsifiable slice targeting a shippable PR in under two hours. Record unfinished work in the [execution queue](PLAN.md).
1. For material changes, follow the [evidence-driven workflow](.llm/workflows/evidence-driven-change.md). Always add both C# tests and Lua fixtures and regenerate the corpus when adding either C# or Lua regression coverage or changing runtime or interop code. At the top of each fixture use only the hand-authored tags `@lua-versions`, `@novasharp-only`, and `@expects-error`. Run tests across Lua 5.1–5.5. Compare Lua behavior changes against the matching reference interpreters.
1. Run builds and tests through the quick scripts. Before closing behavior or CI changes, run focused checks, the quick build, full quick tests, formatting, and applicable Lua comparisons. Poll PR CI through success or diagnosis. Claim overall success only after observing all required local and PR checks pass. Identify every unrun or failing gate.
1. When commits, pushes, or PR work are requested, review the diff and use small scoped commits. Run `bash ./scripts/dev/pre-commit.sh` before committing (it may format and restage files). Destructive Git operations require an explicit request.
1. Use the GitHub API sparingly to avoid rate limits and throttling. Batch reads, reuse results, and space out CI polls. Prefer local Git for facts already available in the checkout.
1. After material changes or investigations, apply [post-work reflection](.llm/skills/post-work-reflection/SKILL.md) and seek independent review when available.

```bash
./scripts/build/quick.sh          # Interpreter build; use --all for the full solution
./scripts/test/quick.sh           # Full tests
./scripts/test/quick.sh Floor     # Filter by method
./scripts/test/quick.sh -c Math   # Filter by class
```

For source locations, project conventions, or skill discovery, consult the [repository guide](.llm/context.md).

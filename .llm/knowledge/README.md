# NovaSharp LLM Knowledge

This directory stores verified, durable repository facts that help future agents
but are not procedural enough to be skills and not universal enough for
`.llm/context.md`.

## Admission Rules

Add knowledge only through the mandatory
[post-work reflection workflow](../skills/post-work-reflection.md). Before adding
an entry:

1. Search `.llm`, `docs/`, source, tests, and tooling for an existing canonical
   home.
1. Verify the fact from source, a reproducible command, reference Lua, or an
   authoritative project document.
1. Record the evidence using relative paths and include version/platform scope or
   caveats.
1. Prefer updating an existing topic file. Create a lowercase, hyphenated topic
   file only when no suitable one exists.

Do not store instructions, secrets, personal data, hypotheses, temporary failures,
branch state, raw logs, or facts already easy to discover in canonical docs.

## Topic Index

- [gstack adaptation decisions](gstack-adaptation.md) — pinned research
  provenance plus adopted and intentionally rejected workflow practices.

## Entry Template

```markdown
## Concise fact

- **Fact:** What future work needs to know.
- **Scope:** Relevant Lua versions, platforms, components, or dates.
- **Evidence:** `relative/path:line`, test name, or reproducible command.
- **Implication:** How this changes a future decision.
```

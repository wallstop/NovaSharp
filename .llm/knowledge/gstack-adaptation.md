# gstack Adaptation Decisions

## Research provenance

- **Fact:** The adaptation review used gstack commit
  `a3259400a366593e0c909dd9ac3e59752efd2488`.
- **Scope:** Research performed 2026-07-29; later gstack revisions may differ.
- **Evidence:** `https://github.com/garrytan/gstack/tree/a3259400a366593e0c909dd9ac3e59752efd2488`.
- **Implication:** Re-evaluate source claims before importing newer mechanisms.

## Practices adopted

- **Fact:** NovaSharp adopted staged evidence vocabulary, red→green proof,
  change-path verification, deterministic replay, architecture gates, and
  fresh-context adversarial handoffs.
- **Scope:** `.llm` workflows for material repository work.
- **Evidence:** `.llm/workflows/evidence-driven-change.md`,
  `.llm/skills/adversarial-handoff/SKILL.md`, and
  `.llm/skills/change-path-verification/SKILL.md`.
- **Implication:** Use executable evidence and semantic risk rather than agent
  confidence or diff size to satisfy gates.

## Practices intentionally rejected

- **Fact:** NovaSharp does not adopt gstack's repeated generated preambles,
  subjective AI coverage percentages, silently skipped undetermined coverage,
  LOC-based risk routing, non-blocking adversarial failure, or retry/revert of
  failing generated tests.
- **Scope:** NovaSharp's generalized, front-end-neutral `.llm` system.
- **Evidence:** Existing objective gates in `.llm/context.md`,
  `.llm/skills/lua-comparison-harness/SKILL.md`, and
  `.llm/skills/test-failure-investigation/SKILL.md`.
- **Implication:** Preserve the reference-Lua oracle, zero-flake policy,
  multi-version/platform matrix, and mechanical CI gates as higher authorities.

## Source mechanisms reviewed

- **Fact:** The most relevant upstream mechanisms are gstack's engineering-plan
  review, review checklist/specialists, fresh-context coverage audit, adversarial
  pass, staged ship sections/manifest, and evidence-backed retrospectives.
- **Scope:** Pinned research commit above.
- **Evidence:** `plan-eng-review/SKILL.md`, `review/checklist.md`,
  `review/specialists/testing.md`, `review/specialists/red-team.md`,
  `ship/sections/test-coverage.md`, `ship/sections/adversarial.md`,
  `ship/sections/manifest.json`, and `retro/SKILL.md` in the pinned repository.
- **Implication:** Import the compact method, not host-specific automation or
  generated prompt volume.

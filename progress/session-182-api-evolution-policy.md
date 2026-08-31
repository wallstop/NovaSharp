# Session 182 - API Evolution Policy

## Goal

Make NovaSharp's pre-adoption API policy explicit: host-facing APIs may change
freely, repository-owned callers move atomically, and superseded APIs are removed
without legacy compatibility layers.

## Changes

- Established the canonical policy in `.llm/context.md` and mirrored its essential
  rule in `AGENTS.md`.
- Updated `PLAN.md` so the root API remains evolvable instead of becoming frozen.
- Replaced namespace-rebrand shims, aliases, staged deprecation, and consumer-wait
  gates with a single public cutover supported by incremental internal preparation.
- Aligned the documentation skill, Hardwire generator proposal, vestigial-code
  inventory, and performance guidance with the no-legacy-API policy.

## Review

An independent audit identified the active guidance that contradicted the new
policy. An adversarial review then found an ambiguous namespace cutover sequence
and a hypothetical-existing-user check; a separate implementation pass corrected
both findings.

## Validation

- `python3 scripts/lint/test_plan_hygiene.py`
- `python3 scripts/lint/check-plan-hygiene.py`
- `python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py`
- `python3 tools/LlmSkillIndexer/llm_skill_indexer.py`
- `python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check`
- `python3 scripts/ci/format_markdown.py --check --include-skipped --files ...`
- `python3 scripts/ci/check_markdown_links.py --files ...`
- `git diff --check`

No runtime code or Lua behavior changed, so build, test, and Lua-comparison suites
were not required for this documentation-only policy update.

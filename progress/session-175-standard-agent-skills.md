# Session 175: Standard Agent Skills Migration

**Date**: 2026-08-10\
**Issue**: [#83](https://github.com/wallstop/NovaSharp/issues/83)\
**Base revision**: `ef0afd0f`

## Objective

Migrate NovaSharp's flat `.llm` guidance to the open Agent Skills format while
keeping `.llm` canonical, supporting Codex, Claude Code, and GitHub Copilot, and
enforcing a 150-line target with a 200-line ceiling for every `SKILL.md`.

## Evidence and design

The [Agent Skills specification](https://agentskills.io/specification) requires a
directory containing `SKILL.md` with `name` and `description` frontmatter. Client
documentation identifies different repository discovery roots:

- [Codex](https://learn.chatgpt.com/docs/build-skills) scans `.agents/skills` and
  explicitly follows symlinked skill folders.
- [Claude Code](https://code.claude.com/docs/en/skills) scans `.claude/skills`.
- [GitHub Copilot](https://docs.github.com/en/copilot/concepts/agents/about-agent-skills)
  scans `.agents/skills` and `.claude/skills`.

Keeping duplicate client copies would create multiple authorities. Using only
one client directory would make another client miss the skills. The selected
layout keeps all content in `.llm/skills/<name>/` and checks in two discovery
symlinks that resolve to the same canonical tree.

Lua runtime, performance, allocation, and Unity behavior are unaffected. The
semantic risk is limited to LLM discovery, instruction preservation, local link
resolution, generated index integrity, and CI validation.

## Red gate

Four focused tests were added before the indexer change. On `ef0afd0f` plus those
tests, `python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py` failed because the
old implementation could not parse nested standard metadata, discover
`*/SKILL.md`, validate directory/name alignment, or enforce the new line limits.

The repository also had 31 flat `.llm/skills/*.md` files, none with the required
standard `name` and `description` pair, and 16 files above 200 lines.

## Implementation

- Migrated all 31 skills to `.llm/skills/<name>/SKILL.md`.
- Replaced legacy trigger lists with explicit standard descriptions containing
  both capability and activation context.
- Preserved category, priority, and related-skill routing as string values under
  standard `metadata`.
- Moved long later sections into directly linked `references/REFERENCE.md` files.
- Added `.agents/skills` and `.claude/skills` symlinks to `.llm/skills`.
- Upgraded the indexer to validate the standard schema, cross-client name rules,
  directory alignment, related skills, 150/200 line limits, legacy files,
  discovery aliases, and stale generated output.
- Updated active guidance, workflow links, branding allowlists, and linked
  historical references for the directory layout.

## Validation receipt

| Claim | Command or evidence | Result |
| ----- | ------------------- | ------ |
| Focused indexer behavior | `python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py` | 19 tests passed |
| Spelling-audit index discovery | `python3 tools/SpellingAudit/test_spelling_audit.py` | 3 tests passed |
| Standard metadata and index | `python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check` | 31 skills valid; index current |
| Upstream standard validator | `skills-ref validate` from `agentskills/agentskills` for every canonical skill | All 31 skills valid |
| Skill line target | `wc -l .llm/skills/*/SKILL.md` | 31 files; maximum 136 lines |
| Local resource links | Repository pre-commit link checker | All links reachable |
| Codex discovery | `codex debug prompt-input 'Inspect skill discovery only.'` | 31 canonical `.llm` skill entries observed |
| Build | `./scripts/build/quick.sh` | Passed |
| Full tests | `./scripts/test/quick.sh` | 15,225 passed; 0 failed; 0 skipped |
| Formatting/pre-commit | `bash ./scripts/dev/pre-commit.sh` | Passed |
| Reference Lua comparison | N/A | No Lua/runtime behavior changed |
| PR CI | [PR #110](https://github.com/wallstop/NovaSharp/pull/110) | 22 successful; 1 expected autofix skip; 0 failures |

## Post-work reflection

The durable defect was format drift: the indexer validated a repository-specific
flat-file schema, so it could report a healthy catalog that none of the standard
client discovery roots would load. The migration fixes the data and the guard at
the same time. CI now rejects legacy files, alias drift, invalid standard
frontmatter, unresolved related skills, stale index output, and skills above the
repository's line limits.

A zero-knowledge verifier and a subsequent adversarial reviewer independently
returned `APPROVE` with zero actionable findings on the pre-publication state. They
rechecked schema, legacy-content preservation, links, aliases, live Codex
discovery, determinism, line limits, and unrelated-file isolation. A separate
forward test used the rewritten `adding-skills` guide to design a hypothetical
cross-client skill and returned `PASS`; its independent review rejected an
unsupported suggestion to add a redundant `.github/skills` copy.

The first PR CI run found a separate integration defect: the pre-commit spelling
audit discovered scan roots from `HEAD`, so it omitted newly staged top-level
directories while CI included them after commit. The discovery source now uses
the staged Git index, a focused regression test protects that behavior locally
and in CI, and the audit receipt includes `.agents` and `.claude` without
including the unrelated untracked package files. Fresh review then found and
closed two edge cases in that fix: case-folded sort ties now have a total order,
and an unavailable Git index fails explicitly instead of falling back to
untracked filesystem entries. A fresh verifier reproduced both original findings
and returned `APPROVE` with zero remaining actionable findings.

PR #110's post-fix run completed with all 22 required checks successful,
one expected `lint-autofix` skip, and no failures. The generated Lua comparison
report recorded zero unexpected deltas across Lua 5.1-5.5 on Ubuntu, macOS, and
Windows. The feedback audit found no human reviews or review threads; the only
conversation comments were the successful coverage and Lua comparison reports.

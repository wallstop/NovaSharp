---
name: adding-skills
description: "Create or update NovaSharp Agent Skills and their metadata, references, index, and discovery aliases. Use when changing LLM guidance, SKILL.md files, skill documentation, or the skills index."
metadata:
  category: meta
  priority: reference
  related: documentation-and-changelog
---
# Add or Update Agent Skills

Keep skill content compatible with the
[Agent Skills specification](https://agentskills.io/specification) and discoverable
by Codex, Claude Code, and GitHub Copilot.

## Canonical layout

Store every skill under `.llm`:

```text
.llm/skills/<skill-name>/
├── SKILL.md
├── references/  # optional, read on demand
├── scripts/     # optional, executable helpers
└── assets/      # optional, output resources
```

Treat `.llm/skills` as the only content source. Do not duplicate skills under
client-specific directories. The repository symlinks `.agents/skills` and
`.claude/skills` expose the canonical tree to supported clients.

## Frontmatter

Start `SKILL.md` with standard YAML metadata:

```yaml
---
name: skill-name
description: Describe what the skill does and the specific tasks that trigger it.
metadata:
  category: performance
  priority: recommended
  related: first-skill, second-skill
---
```

- Match `name` to the parent directory. Use lowercase letters, digits, and single
  hyphens; keep it within 64 characters.
- Make `description` non-empty and at most 1,024 characters. Put all discovery
  and trigger language there because clients load it before the body.
- Keep optional standard fields limited to `license`, `compatibility`,
  `metadata`, and `allowed-tools`.
- Store NovaSharp category, priority, and comma-separated related skill names as
  string values under `metadata`.
- Use categories `core`, `performance`, `testing`, `lua`, `workflow`, or `meta`.
- Use priorities `core`, `recommended`, or `reference`.

## Body and resources

- Write imperative, project-specific instructions. Omit concepts a capable agent
  already knows.
- Keep one focused job per skill.
- Target at most 150 lines in `SKILL.md`; never exceed 200.
- Move detailed examples and conditional guidance to focused files under
  `references/`. Link each resource from `SKILL.md` and say when to read it.
- Keep referenced resources one level below `SKILL.md`; avoid reference chains.
- Resolve relative links from the skill directory. Link another skill as
  `../<name>/SKILL.md`, shared `.llm` content as `../../<path>`, and repository
  content as `../../../<path>`.
- Add scripts only for repeated deterministic operations, and execute every added
  script during validation.

## Validation

Run the focused checks after any skill or indexer change:

```bash
python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py
python3 tools/LlmSkillIndexer/llm_skill_indexer.py
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
```

The indexer validates standard metadata, directory/name alignment, line limits,
related-skill references, both client discovery symlinks, and the committed
`.llm/skills-index.json`.

## Checklist

- [ ] Canonical file is `.llm/skills/<name>/SKILL.md`
- [ ] Name and description satisfy the standard and trigger narrowly
- [ ] Optional metadata values are strings
- [ ] `SKILL.md` is at most 150 lines
- [ ] Resources are linked directly and paths resolve from the skill directory
- [ ] Added scripts were executed successfully
- [ ] Indexer tests, generation, and strict check succeeded
- [ ] `.llm/context.md` routing changed if the skill is commonly used

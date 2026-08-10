# Agent Skill Indexer

Scans `.llm/skills/*/SKILL.md`, validates the Agent Skills structure and
NovaSharp metadata, and generates `.llm/skills-index.json`.

## Usage

```bash
# Run focused unit tests
python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py

# Validate and regenerate the committed index
python3 tools/LlmSkillIndexer/llm_skill_indexer.py

# Read-only strict check used by pre-commit and CI
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
```

After changing a skill, regenerate and stage its complete directory with the
index:

```bash
python3 tools/LlmSkillIndexer/llm_skill_indexer.py
git add .llm/skills/changed-skill .llm/skills-index.json
```

## Layout and discovery

Canonical skills live only under `.llm`:

```text
.llm/skills/<skill-name>/
├── SKILL.md
├── references/  # optional
├── scripts/     # optional
└── assets/      # optional
```

The checked-in `.agents/skills` and `.claude/skills` symlinks point to
`.llm/skills`. The indexer rejects missing or retargeted aliases so Codex, Claude
Code, and GitHub Copilot keep discovering the same canonical skills.

## Frontmatter

Each `SKILL.md` requires standard `name` and `description` fields. Optional
NovaSharp classification remains a string-to-string map under standard
`metadata`:

```yaml
---
name: high-performance-csharp
description: Implement high-performance C# for NovaSharp. Use for hot paths or allocation work.
metadata:
  category: performance
  priority: core
  related: correctness-then-performance, allocation-traps
---
```

Categories are `core`, `performance`, `testing`, `lua`, `workflow`, and `meta`.
Priorities are `core`, `recommended`, and `reference`. Related names are
comma-separated and must resolve to another skill.

The strict check treats files over the 150-line target as warnings and files over
the 200-line ceiling as errors. Any warning, error, discovery-alias problem, or
stale index fails CI.

## Output

The version 2 index records each skill's name, description, canonical path, line
count, category, priority, related skills, title, and validation results. The
summary contains aggregate warning/error counts and structural errors.

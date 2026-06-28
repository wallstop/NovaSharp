______________________________________________________________________

triggers:

- "new skill"
- "add skill"
- "skill documentation"
- "llm documentation"
  category: meta
  related:
- documentation-and-changelog
  priority: reference

______________________________________________________________________

# Skill: Adding New Skills

**When to use**: Creating or modifying LLM skill documentation in `.llm/skills/`.

______________________________________________________________________

## YAML Front-Matter (Required)

Every skill file must start with YAML front-matter between `---` markers:

```yaml
---
triggers:
  - "zero allocation"
  - "pooling"
  - "memory optimization"
category: performance
related:
  - unity-gc-patterns
  - refactor-to-zero-alloc
priority: core
---

# Skill Title

Content follows...
```

### Fields

| Field      | Required | Description                                             |
| ---------- | -------- | ------------------------------------------------------- |
| `triggers` | Yes      | Keywords that suggest this skill applies                |
| `category` | Yes      | One of: core, performance, testing, lua, workflow, meta |
| `related`  | No       | Related skill names (without `.md`)                     |
| `priority` | Yes      | One of: core, recommended, reference                    |

### Categories

| Category      | Description                               |
| ------------- | ----------------------------------------- |
| `core`        | Essential guidelines for all work         |
| `performance` | Performance optimization patterns         |
| `testing`     | Test writing and validation               |
| `lua`         | Lua spec, fixtures, comparison            |
| `workflow`    | Development workflow and tools            |
| `meta`        | Skills about skills (documentation, etc.) |

### Priorities

| Priority      | Description                    |
| ------------- | ------------------------------ |
| `core`        | Must-read for all contributors |
| `recommended` | Read for relevant tasks        |
| `reference`   | Reference when needed          |

______________________________________________________________________

## Trigger Keyword Conventions

Choose triggers that are:

1. **Specific** - Avoid generic words like "code" or "help"
1. **Actionable** - Describe what the user wants to do
1. **Searchable** - Terms developers actually type

### Good Trigger Examples

```yaml
triggers:
  - "zero allocation"      # Specific technique
  - "pooling"              # Specific pattern
  - "memory optimization"  # Task description
  - "reduce gc pressure"   # User intent
```

### Bad Trigger Examples

```yaml
triggers:
  - "performance"   # Too broad
  - "help"          # Not specific
  - "code"          # Generic
  - "C#"            # Too broad
```

______________________________________________________________________

## Line Limits

| Threshold | Action Required                                |
| --------- | ---------------------------------------------- |
| < 300     | Ideal - no action needed                       |
| 300-500   | Warning - consider extracting code samples     |
| > 500     | Error - must split or extract to code-samples/ |

### Reducing File Size

1. **Extract code samples** to `.llm/code-samples/*.md` and link via anchors
1. **Split into focused skills** - one concept per skill
1. **Remove redundancy** - don't repeat context.md content
1. **Link to external docs** instead of duplicating

### Linking to Code Samples

```markdown
For pooling patterns, see [pooling-patterns.md](../code-samples/pooling-patterns.md#listpool).
```

______________________________________________________________________

## File Structure Template

```markdown
---
triggers:
  - "trigger one"
  - "trigger two"
category: performance
related:
  - related-skill-name
priority: recommended
---

# Skill: Descriptive Title

**When to use**: Brief description of when this skill applies.

**Related Skills**: [skill-one](skill-one.md), [skill-two](skill-two.md)

______________________________________________________________________

## Section Heading

Content with code examples...

______________________________________________________________________

## Another Section

More content...

______________________________________________________________________

## Quick Reference

| Pattern | Replacement |
| ------- | ----------- |
| Bad     | Good        |

______________________________________________________________________

## Checklist

- [ ] Item one
- [ ] Item two
```

______________________________________________________________________

## Naming Guidelines

| Convention             | Example                      |
| ---------------------- | ---------------------------- |
| Lowercase with hyphens | `high-performance-csharp.md` |
| Descriptive action     | `refactor-to-zero-alloc.md`  |
| Topic focused          | `unity-gc-patterns.md`       |
| Avoid abbreviations    | `defensive-programming.md`   |

______________________________________________________________________

## Validation

Run the indexer after creating/modifying skills:

```bash
# Generate index and validate
python3 tools/LlmSkillIndexer/llm_skill_indexer.py

# Check mode (fails on errors)
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
```

______________________________________________________________________

## Checklist for New Skills

- [ ] YAML front-matter with triggers, category, priority
- [ ] File under 300 lines (warning) or 500 lines (error)
- [ ] Code samples extracted to `.llm/code-samples/` if lengthy
- [ ] Related skills linked
- [ ] Ran indexer to validate: `python3 tools/LlmSkillIndexer/llm_skill_indexer.py`
- [ ] Updated context.md Skills table if skill is commonly used

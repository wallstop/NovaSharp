# LLM Skill Indexer

Scans `.llm/skills/*.md` for YAML front-matter metadata and generates a categorized `skills-index.json`.

## Usage

```bash
# Generate index (writes to .llm/skills-index.json)
python3 tools/LlmSkillIndexer/llm_skill_indexer.py

# Check mode (exits non-zero on validation errors)
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check

# Verbose output
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --verbose
```

## Integration with pre-commit.sh

Add to `scripts/dev/pre-commit.sh` for validation (warning mode):

```bash
# Validate LLM skills metadata (warning mode)
echo "Checking LLM skills metadata..."
python3 tools/LlmSkillIndexer/llm_skill_indexer.py || true
```

For strict enforcement:

```bash
# Validate LLM skills metadata (strict mode)
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
```

## YAML Front-Matter Format

Skills should include YAML front-matter at the start of the file:

```yaml
---
triggers:
  - "zero allocation"
  - "pooling"
  - "memory optimization"
category: performance  # core|performance|testing|lua|workflow|meta
related:
  - unity-gc-patterns
  - refactor-to-zero-alloc
priority: core  # core|recommended|reference
---

# Skill Title

Content follows...
```

## Categories

| Category      | Description                               |
| ------------- | ----------------------------------------- |
| `core`        | Essential guidelines for all work         |
| `performance` | Performance optimization patterns         |
| `testing`     | Test writing and validation               |
| `lua`         | Lua spec, fixtures, comparison            |
| `workflow`    | Development workflow and tools            |
| `meta`        | Skills about skills (documentation, etc.) |

## Priorities

| Priority      | Description                    |
| ------------- | ------------------------------ |
| `core`        | Must-read for all contributors |
| `recommended` | Read for relevant tasks        |
| `reference`   | Reference when needed          |

## Line Limits

- **Warning**: Files over 300 lines
- **Error**: Files over 500 lines

Files exceeding limits should:

1. Extract reusable code samples to `.llm/code-samples/`
1. Split into multiple focused skills
1. Remove redundant content

## Output Format

The generated `skills-index.json` contains:

```json
{
  "version": "1.0.0",
  "skills_count": 31,
  "categories": {
    "core": ["correctness-then-performance", "..."],
    "performance": ["high-performance-csharp", "..."]
  },
  "skills": [
    {
      "name": "high-performance-csharp",
      "file_path": ".llm/skills/high-performance-csharp.md",
      "line_count": 1373,
      "triggers": ["zero allocation", "pooling"],
      "category": "performance",
      "related": ["unity-gc-patterns"],
      "priority": "core",
      "has_front_matter": true,
      "title": "High-Performance C# Coding Guidelines",
      "validation_warnings": ["..."],
      "validation_errors": ["..."]
    }
  ],
  "validation_summary": {
    "total_warnings": 5,
    "total_errors": 2
  }
}
```

# LLM Workflows

Workflows define cross-cutting task state and mandatory gates. Skills remain the
canonical source for domain techniques; workflows link to them instead of copying
their rules.

Use [evidence-driven-change](evidence-driven-change.md) for material behavior,
architecture, performance, reliability, CI, or LLM-system work. Small,
documentation-only edits may use the same stages with proportionate evidence.

## Workflow Contract

Every workflow declares:

- inputs, scope, and acceptance criteria;
- observed facts, hypotheses, and unresolved unknowns;
- allowed mutations and explicit stop conditions;
- required gates selected by semantic risk;
- output evidence and residual risks.

Status words are evidence-bearing:

| Status         | Meaning                                                        |
| -------------- | -------------------------------------------------------------- |
| `OBSERVED`     | Directly read or measured; cite the source or command          |
| `HYPOTHESIS`   | Falsifiable explanation awaiting a test                        |
| `VERIFIED`     | Required check passed on the identified revision               |
| `PARTIAL`      | Some acceptance criteria have evidence; name every missing one |
| `UNVERIFIABLE` | External state or unavailable environment prevents a check     |
| `BLOCKED`      | A mandatory gate cannot proceed without new authority/state    |

`INFERRED` may explain reasoning but never satisfies a gate. A touched file is not
evidence that its intended behavior exists.

## Single-Source-of-Truth Rules

- Put universal priorities in `.llm/context.md`.
- Put reusable procedures in `.llm/skills/`.
- Put orchestration and gates here.
- Put verified stable facts in `.llm/knowledge/`.
- Keep task receipts out of source control unless a project document requires
  them. PR descriptions and CI artifacts are suitable receipt locations.
- Front-end adapters should link to `.llm/context.md`; generated adapters must
  identify their canonical template and generator.

Do not encode the same dispatch predicate in both prose and metadata. Metadata is
for discovery; the linked workflow is authoritative.

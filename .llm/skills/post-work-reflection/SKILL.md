---
name: post-work-reflection
description: "Run NovaSharp’s mandatory post-work improvement gate after material work or investigation. Use to review evidence, fix residual defects, capture verified reusable lessons, and obtain independent approval before completion."
metadata:
  category: core
  priority: core
  related: adding-skills, documentation-and-changelog, test-failure-investigation
---
# Skill: Post-Work Reflection and Self-Improvement

This is a mandatory completion gate. Its purpose is to turn verified lessons into
durable improvements without filling the repository with speculation or
task-specific noise.

______________________________________________________________________

## What Counts as Large Work

Run this workflow for any work whose scope, risk, effort, or complexity is
material, even if it changes only one file. This includes large documentation,
test, configuration, dependency, tooling, and generated-artifact changes.

The following conditions always require the workflow:

- Production behavior, public API, architecture, performance, security, build, or
  CI workflow changed.
- The work materially spans at least three related files or more than one
  subsystem.
- A skill, role, workflow, or other LLM instruction was added or substantially
  revised.
- A repeated failure, difficult debugging session, or root-cause investigation
  occurred.
- The user explicitly requests reflection, self-improvement, or captured lessons.

When uncertain, run the workflow. A brief conclusion that no durable update is
warranted is valid; inventing a lesson is not.

______________________________________________________________________

## Mandatory Completion Loop

1. **Inventory the evidence** — Review the final diff, test and diagnostic output,
   reference-Lua comparisons when applicable, review feedback, and any failed
   approaches. Do not rely on memory alone.

1. **Analyze causes and opportunities** — Identify defects, fragile assumptions,
   recurring friction, missing validation, reusable techniques, and stable facts
   that would make future work more correct or efficient.

1. **Classify every finding** using the routing table below. Search existing
   guidance first so updates extend the canonical source instead of duplicating it.

1. **Act at the root** — Fix in-scope defects now. For systemic issues, improve the
   narrowest authoritative workflow, skill, tool, template, or test that prevents
   recurrence. Record durable facts only when they are verified.

1. **Validate the updates** — Run checks appropriate to every changed artifact.
   For skill changes, regenerate and check the index:

   ```bash
   python3 tools/LlmSkillIndexer/llm_skill_indexer.py
   python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
   ```

1. **Obtain an independent review** — When sub-agents or other reviewers are
   available, have a reviewer inspect the final diff, evidence, classifications,
   and documentation placement. The author must not serve as the independent
   reviewer.

1. **Iterate to consensus** — Route each actionable review finding to a separate
   adjudication/implementation pass when agents are available. Re-run validation
   and independent review until all reviewers return `APPROVE` with zero actionable
   findings. Without another reviewer, perform a fresh, explicitly separate
   self-review pass using the same criteria.

1. **Report honestly** — Summarize durable updates and validation. If nothing was
   captured, state that the reflection found no evidence-backed reusable lesson.
   Never describe unrun checks or unavailable review as completed.

______________________________________________________________________

## Finding Routing

| Finding                                 | Required destination                                                       |
| --------------------------------------- | -------------------------------------------------------------------------- |
| Defect or incomplete work               | Fix production code, tests, tooling, or docs before completion             |
| Reusable problem-solving technique      | Create or update one focused `.llm/skills/<name>/SKILL.md` skill           |
| Stable repository fact or useful trivia | Add it to the relevant topic under `.llm/knowledge/`                       |
| Always-applicable policy or priority    | Update `.llm/context.md`; mirror only the essential rule in `AGENTS.md`    |
| Navigation or task-routing information  | Update `.llm/context.md` or the relevant skill links/triggers              |
| Repeated mechanical failure             | Prefer an automated check, helper, or template plus concise guidance       |
| Task-specific or transient observation  | Keep it in the final report; do not persist it as guidance                 |
| Unverified hypothesis                   | Investigate and verify, or report it as unresolved; never store it as fact |

Roles are useful only when they define a recurring responsibility that cannot be
expressed clearly in an existing skill or workflow. Do not create roles merely to
rename a one-off task.

______________________________________________________________________

## Quality and Safety Rules

- Preserve the priority hierarchy: Lua correctness, speed, memory, Unity
  compatibility, then clarity.
- Base durable updates on reproducible evidence. Include relevant relative paths,
  commands, version scope, and caveats.
- Prefer prevention over reminders: tests, linters, scripts, and templates are
  stronger than prose when automation is practical.
- Keep one canonical home for each rule. Link to it rather than copying detailed
  instructions into several files.
- Do not store secrets, personal data, credentials, ephemeral logs, branch-specific
  state, guesses, or conclusions contradicted by reference Lua.
- Do not broaden the task to speculative refactors. Record a clearly scoped
  follow-up when a root fix needs authority beyond the current request.
- Preserve unrelated user changes and inspect the final diff for accidental edits.
- A documentation edit must improve future decisions. Churn created only to prove
  that reflection occurred is a failure of this workflow.

______________________________________________________________________

## Independent Review Checklist

A reviewer returns `APPROVE` only when all answers are yes:

- [ ] The original work is correct, scoped, and supported by observed validation.
- [ ] Root causes and failed approaches were considered, not only symptoms.
- [ ] Every durable finding is verified, deduplicated, and routed to its canonical
  location.
- [ ] New guidance is actionable, concise, discoverable, and consistent with
  existing priorities.
- [ ] Automation was used where it can reliably prevent recurrence.
- [ ] No secrets, speculation, transient state, or unrelated changes were added.
- [ ] All documentation/index checks applicable to the final diff were observed.

Any `REQUEST_CHANGES` response must identify a concrete issue, supporting evidence,
and the required correction.

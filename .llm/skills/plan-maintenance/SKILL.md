---
name: plan-maintenance
description: "Keep NovaSharp PLAN.md as a lean execution queue by auditing active and future work, routing context and history to canonical artifacts, and removing completed or obsolete entries. Use when creating, reviewing, or updating plans, roadmaps, milestones, or session progress."
metadata:
  category: workflow
  priority: core
  related: systematic-investigation, post-work-reflection, documentation-and-changelog
---
# Plan Maintenance

`PLAN.md` is an execution queue, not a knowledge base, status report, research
notebook, changelog, issue tracker, or design document.

## Content contract

Keep only work that is in progress or intentionally queued. Organize it as
`Now`, `Next`, and `Later or gated`, or an equally small dependency-ordered
structure. Each retained item must help choose or verify the next action by
stating an outcome, dependency, gate, or authoritative issue/design link.

Never retain completed checkboxes, dated results, session narratives, benchmark
tables, research exposition, implementation diaries, repository snapshots, or
duplicated issue bodies. Delete obsolete and rejected work.

Write a future deadline as an action followed by `by YYYY-MM-DD`. Date-led lines
are history-shaped and mechanically rejected, even when intended as future work.

## Route information once

| Information                                                            | Canonical destination               |
| ---------------------------------------------------------------------- | ----------------------------------- |
| Always-applicable priority or closure gate                             | `.llm/context.md`                   |
| Reusable method or decision process                                    | `.llm/skills/` or `.llm/workflows/` |
| Stable verified repository fact                                        | `.llm/knowledge/`                   |
| Architecture, proposal, benchmark, or domain detail                    | Relevant file under `docs/`         |
| Completed work, failed experiment, measurements, or validation receipt | `progress/session-NNN-*.md`         |
| Unselected task, defect, or externally coordinated backlog             | GitHub issue                        |

Link to the authority instead of summarizing it in PLAN. Do not mirror progress
across PLAN and another artifact. When a task completes, write its session record
and remove it from PLAN in the same change.

## Audit workflow

1. Establish a baseline: line count, checked and unchecked items, session links,
   headings, current branch, latest session, and authoritative open issues.
1. Define the current milestone and the smallest falsifiable exit observation.
1. Classify every PLAN section as `KEEP`, `MOVE/LINK`, `ARCHIVE`, or `DELETE`.
1. Preserve dependencies and acceptance gates; compress implementation detail
   into a domain link or issue.
1. Check that no active work disappeared and no completed work remains.
1. Run the focused hygiene, skill-index, Markdown, and link checks.

## Mechanical guard

`scripts/lint/check-plan-hygiene.py` rejects PLAN files over 120 lines, completed
checkboxes, session-history links, date-led lines, and archive/status history
shapes. Treat the limit as a ceiling, not a target. If genuinely active work will
not fit, move task detail to issues or domain documents and retain only ordering
and gates here.

```bash
python3 scripts/lint/test_plan_hygiene.py
python3 scripts/lint/check-plan-hygiene.py
python3 tools/LlmSkillIndexer/llm_skill_indexer.py --check
python3 scripts/ci/check_markdown_links.py --files PLAN.md
```

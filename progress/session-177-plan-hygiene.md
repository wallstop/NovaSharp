# Session 177 — Lean PLAN and anti-bloat guard

Date: 2026-08-26

## Objective

Reduce `PLAN.md` to the minimum execution queue for in-progress and future work,
route context and completed evidence to their canonical homes, and prevent the
same accumulation mechanically and through agent guidance.

## Red observation

At revision `1e2561b9`, before this change, the working-tree PLAN audit reported:

| Signal | Before |
| --- | ---: |
| Lines | 1,507 |
| UTF-8 bytes | 136,403 |
| Headings | 103 |
| Completed checklist items | 59 |
| Unchecked checklist items | 79 |
| Links to session history | 122 |

The focused pre-change acceptance probe failed its 200-line ceiling and rejected
the completed items and session-history links. The repository already instructed
agents to remove completed PLAN entries in `GOAL.md`, so reminder text alone had
not prevented recurrence.

## Investigation

Two hypotheses survived direct inspection:

| ID | Hypothesis | Discriminator | Observation | Conclusion |
| --- | --- | --- | --- | --- |
| H1 | Guidance conflicted about PLAN ownership | Search active guidance for instructions to write completed detail to PLAN | Lua verification, test-failure, modernization, testing, and contributor docs told agents to mirror fixes, checkpoints, suppressions, or completed rows into PLAN | Supported |
| H2 | No mechanical boundary existed | Inspect pre-commit, Markdown CI, and lint scripts | No check limited PLAN size or rejected history/completed work | Supported |

The public [GitHub issue list](https://github.com/wallstop/NovaSharp/issues)
reported 20 open issues during the audit. That confirmed the issue tracker can
remain the authoritative full backlog; copying every issue body into PLAN would
create another synchronization surface. The current branch name,
`a5-basic-callback-views`, and the latest progress session established Basic
CoreLib callback migration as the in-progress roadmap slice.

## Routing decision

| Old PLAN content | Disposition |
| --- | --- |
| Current dependencies, ordered milestones, and exit gates | Retained and compressed under Now/Next/Later |
| Principles and repository-wide closure rules | Linked to `.llm/context.md` |
| Reusable plan-audit method | Added as `.llm/skills/plan-maintenance/SKILL.md` |
| Completed work, dated results, failed experiments, and validation receipts | Removed from PLAN; existing `progress/session-*` records remain authoritative |
| Architecture, performance, testing, and modernization detail | Delegated to existing domain docs and issues |
| Obsolete, duplicated, or unselected speculation | Removed; issue tracker is authoritative if still actionable |

## Implementation

- Replaced the 1,507-line document with a 74-line execution queue containing no
  completed checklist items or session-history links.
- Preserved current A5/B1 work, ordered A1d–A8 and B2–B6 dependencies, essential
  exit gates, selected A4 measurement issues, and compact Lua/testing backlogs.
- Moved the ten concrete untrusted-mod security requirements into
  `docs/security/sandbox-threat-model.md` and retained only their A3/B4 dependency
  links in PLAN.
- Added the `plan-maintenance` skill and routed it from `.llm/context.md`.
- Corrected active Lua, testing, modernization, performance, and contributor
  guidance that previously required duplicating history or detailed state in PLAN.
- Added `scripts/lint/check-plan-hygiene.py` with a 120-line ceiling and guards
  against completed checkboxes, session-history links, and archive headings.
- The 120-line ceiling leaves 46 lines of headroom; the skill explicitly treats
  that ceiling as a limit rather than a target.
- Added twenty-four focused tests and wired the guard into Markdown CI and pre-commit.
- Documented the checker in `scripts/lint/README.md` without altering the
  pre-existing devcontainer-lifecycle edits in that file.

## Validation receipt

- Applying the final checker to `HEAD:PLAN.md`: failed with 1,507 lines, 59
  completed items, 122 session links, 13 archive headings, seven dated completion
  results, 30 progress narratives, and eight completion-status lines.
- `python3 scripts/lint/test_plan_hygiene.py`: 24 tests passed.
- `python3 scripts/lint/check-plan-hygiene.py`: passed on the 74-line PLAN.
- `python3 tools/LlmSkillIndexer/test_llm_skill_indexer.py`: 19 tests passed.
- Skill index generation and strict check: 32 skills valid; generated index current.
- Focused Markdown format and link checks: passed; all checked links reachable.
- Markdown formatter and Jekyll tests: 1 and 14 tests passed; repository-wide
  Liquid scan checked 57 rendered files with no fatal syntax.
- `NOVASHARP_BASE_REF=HEAD bash ./scripts/ci/check-markdown.sh`: passed, including
  the newly wired PLAN tests and guard.
- `bash -n scripts/ci/check-markdown.sh scripts/dev/pre-commit.sh` and
  `python3 scripts/lint/check-shell-python-invocation.py`: passed.
- `./scripts/build/quick.sh`: passed.
- `./scripts/test/quick.sh`: 15,241 passed; 0 failed; 0 skipped.
- Reference-Lua comparison: N/A; no runtime or Lua behavior changed in this task.
- Full pre-commit: not run because it auto-formats/restages files and the worktree
  contains unrelated in-progress user changes. Its changed PLAN/Markdown/skill and
  shell paths were run directly above.
- PR CI: not run; no commit, push, or PR was requested.

## Independent review loop

The first zero-knowledge verifier returned `REQUEST_CHANGES` with three findings:

1. `+ [x]` and ordered CommonMark task markers bypassed the completed-item guard
   (`plan-hygiene/completed-marker-bypass`). The regex now covers `-`, `+`, `*`,
   and one-to-nine-digit `.`/`)` ordered markers; two new tests and the original
   reproduction prove the forms are rejected.
1. The compact queue omitted Initiative 13's unfinished incremental error-message
   and module-name literal consolidation
   (`plan-routing/lost-active-magic-string-initiative`). The maintenance backlog
   now retains that outcome without restoring historical detail.
1. Two coverage checkpoints still directed detailed debt to PLAN
   (`plan-routing/coverage-watch-list-instruction`). The coverage file now retains
   its own history and directs selected debt to an issue.

The first receipt is invalidated by those changes. Fresh independent verification
found four further gaps:

1. The ten-item sandbox threat model existed only in the deleted PLAN
   (`plan-routing/lost-sandbox-threat-model`). It now has an authoritative domain
   document with PLAN retaining only A3/B4 links.
1. Block-quoted CommonMark tasks bypassed the completed-item regex
   (`plan-hygiene/blockquoted-completed-marker-bypass`). A shared block-quote
   prefix now covers nested unordered and ordered forms, with regression coverage.
1. `Past results` headings and dated validation lines were not rejected
   (`plan-hygiene/dated-history-bypass`). Both shapes are guarded; a future
   deadline without a result remains accepted.
1. Three modernization/docs instructions still required PLAN duplication
   (`plan-guidance/residual-plan-bloat-instructions`). Detailed work now stays in
   those documents and issues, with PLAN limited to selected execution.

The second receipt is also invalidated. Fresh independent verification is
recorded after the corrected diff is reviewed.

The third fresh verifier returned `REQUEST_CHANGES` with three findings:

1. Common `Done`/`✅ COMPLETE`, list-prefixed dated results, and emphasized
   completed-result lines bypassed the guard (`plan-hygiene/common-history-bypass`).
   Four focused regressions now reject those forms while future deadlines remain
   accepted.
1. Two repository-wide rules remained in the queue
   (`plan-routing/repository-rules-retained-in-queue`). They were removed; their
   canonical sources remain `.llm/context.md` and the plan-maintenance skill.
1. A test comment and two performance documents still named the deleted PLAN
   campaigns as authorities, while issue #108 retained a legacy instruction to
   write measurements into PLAN (`plan-routing/stale-removed-plan-authority`).
   The local references now point to testing/performance authorities, and the A5
   memory research explicitly supersedes the issue's stale routing instruction.

The third receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The fourth fresh verifier returned `REQUEST_CHANGES` with five findings:

1. A5's exception-free hot control-flow invariant had been lost
   (`plan-routing/lost-a5-no-exception-invariant`). It is now an explicit A5 gate.
1. The TUnit migration blueprint still asked for PLAN checkpoints containing
   measurements and suppressions (`plan-guidance/tunit-checkpoint-bloat-remains`).
   Those records now route to progress artifacts and issues.
1. `Past work` and `Closed initiatives` bypassed the archive-heading guard
   (`plan-hygiene/past-closed-heading-bypass`). Both forms have regression tests.
1. Stackless/fuel execution, managed-host GC plus byte-faithful Lua strings, and a
   machine-readable API authority had no durable destination
   (`plan-routing/lost-future-research-context`). They now live in
   `docs/proposals/runtime-research-gates.md` and link from their authorizing PLAN
   milestones.
1. A date-led future instruction to run tests was misclassified as a result
   (`plan-hygiene/future-test-schedule-false-positive`). Result detection now
   requires an outcome term, with a negative-path regression.

The fourth receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The fifth fresh verifier returned `REQUEST_CHANGES` with three findings:

1. A bold `Progress:` session narrative bypassed the guard
   (`plan-hygiene/progress-narrative-bypass`). It now has a dedicated detector and
   regression.
1. A dated future item gated on CI having passed was misclassified as history
   (`plan-hygiene/future-gate-false-positive`). Future conditional clauses are
   excluded from the result shape and covered by a negative-path test.
1. The Lua-spec index still described PLAN as a parity-tracking roadmap
   (`plan-guidance/stale-lua-spec-plan-description`). It now describes the lean
   selected execution queue.

The fifth receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The sixth fresh verifier returned `REQUEST_CHANGES` with four findings:

1. Falsifiable A2, A3, A6, and A7 performance/memory gates were lost
   (`plan-routing/lost-phase-exit-gates`). Their compact thresholds are restored in
   the relevant queue items.
1. Four test XML comments and the naming audit cited deleted PLAN sections or a
   PLAN checkpoint (`plan-routing/stale-deleted-section-authorities`). They now
   describe the tested behavior or authoritative audit artifact directly.
1. Emphasized `Status` and `Current Status` narratives bypassed the guard
   (`plan-hygiene/status-narrative-bypass`). Both forms now share the progress
   narrative detector and have regression coverage.
1. The archive matcher rejected active `Definition of Done` and `Closed-world`
   headings (`plan-hygiene/archive-heading-false-positive`). Status terms are now
   constrained to complete archive-heading shapes, with acceptance tests.

The sixth receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The seventh fresh verifier returned `REQUEST_CHANGES` with four findings:

1. A2's branch-heavy dispatch observation and A3's basic-block fuel granularity
   plus `limit + K` tests were lost (`plan-routing/lost-a2-a3-acceptance-gates`).
   Both compact gates are restored.
1. Unemphasized status, bullet completion, and `Previous work` history shapes
   bypassed the guard (`plan-hygiene/common-completion-history-bypass`). They now
   have shape-specific detectors and regressions.
1. A dated future task to diagnose failed tests was misclassified as history
   (`plan-hygiene/future-failed-task-false-positive`). Imperative diagnostic
   clauses are excluded and covered by an acceptance test.
1. The documentation-audit tool described itself as launching a deleted PLAN
   campaign (`plan-guidance/stale-documentation-audit-authority`). Its docstring
   now names the repository documentation audit directly.

The seventh receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The eighth fresh verifier returned `REQUEST_CHANGES` with two findings:

1. Exact `Progress`/`Results` headings, `Done ✅`, unformatted `Completed:`, and a
   date-led `build succeeded` result bypassed the guard
   (`plan-hygiene/common-history-shapes-still-bypass`). Exact archive/status shapes
   and outcome parsing now reject them while active `Progress blockers` and
   `Results required` headings remain accepted.
1. Date-led future `verify`, `confirm`, and `ensure` tasks containing outcome words
   were misclassified (`plan-hygiene/future-imperative-result-false-positive`). The
   checker now parses the post-date body structurally and exempts future imperatives
   and conditional gates, with positive and negative regressions.

The eighth receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The ninth fresh verifier returned `REQUEST_CHANGES` with three findings:

1. Leading checkmarks, completion/validation headings, and a dated post-fix result
   bypassed the guard (`plan-hygiene/exact-history-shapes-bypass`). Leading `✅`,
   exact receipt headings, and structurally historical date-led outcomes now have
   regressions.
1. Broad archive/history tokens rejected active `Archive file-format migration`
   and `History API replacement` headings
   (`plan-hygiene/archive-heading-false-positive`). Archive matching is now limited
   to exact historical heading shapes, with acceptance coverage.
1. The session receipt retained counts from an earlier checker
   (`plan-evidence/stale-base-red-counts`). The red receipt now records every
   category emitted by the final checker.

The ninth receipt is invalidated by these corrections. Final fresh verification
and adversarial review are recorded after this revised diff is reviewed.

The tenth fresh verifier returned `REQUEST_CHANGES` with one finding: checked old
PLAN headings ending in `RESOLVED`, `FIXED`, or `INCORPORATED` bypassed the guard
(`plan-hygiene/old-result-heading-bypass`). The checked-heading detector now
covers every uppercase outcome term present in the old PLAN, with a regression
for each. The tenth receipt is invalidated by that correction; final fresh
verification and adversarial review follow on the revised diff.

The originating verifier then re-ran the exact reproductions and current diff and
returned `APPROVE` with zero actionable findings. It independently observed all
24 focused tests passing, the 74-line PLAN passing, the final 13-heading red count
matching `HEAD:PLAN.md`, and active look-alike headings remaining accepted. A
separate adversarial review is the remaining closure gate.

The separate adversarial reviewer returned `REQUEST_CHANGES` with two findings:

1. Emphasized/finished headings, present-tense dated test results, and unformatted
   `Done` lines bypassed the guard (`plan-hygiene/common-history-shapes-bypass`).
   Normalized archive/status shapes and subject-outcome result clauses now cover
   them.
1. Date classification still depended on a finite future-verb whitelist, causing
   `release once CI is green` to fail
   (`plan-hygiene/future-deadline-verb-whitelist`). The whitelist was removed;
   date-led history now requires a result-shaped subject/outcome clause, with
   regressions for both historical and future examples.

That adversarial receipt is invalidated by the corrections. Focused adversarial
re-review is the remaining closure gate.

Focused adversarial re-review returned `REQUEST_CHANGES` with two further
findings:

1. Natural-language date classification still produced one false positive and one
   false negative (`plan-hygiene/date-classifier-bidirectional-gap`). PLAN now has
   an unambiguous convention: every date-led line is rejected, while future
   deadlines use `Action by YYYY-MM-DD`.
1. Emphasis around `Past work` bypassed archive detection
   (`plan-hygiene/emphasized-archive-heading-gap`). Heading emphasis is now removed
   before semantic archive classification, with acceptance and rejection tests.

That re-review receipt is invalidated by the corrections. Final focused
adversarial re-review is the remaining closure gate.

The next focused adversarial re-review returned `REQUEST_CHANGES` with two low
severity findings:

1. The diagnostic still called every prohibited date-led line a result
   (`plan-hygiene/date-diagnostic-stale`). Date-led lines and dated completion
   results now have separate, accurate diagnostics.
1. Single-emphasis `*Past work*` and `_Past work_` headings bypassed normalization
   (`plan-hygiene/italic-heading-normalization-gap`). Matching outer single or
   double emphasis is now normalized before archive classification, with active
   emphasized look-alikes remaining accepted.

That re-review receipt is invalidated by the corrections. Final focused
adversarial re-review returned `APPROVE` with zero actionable findings. The
reviewer independently reproduced both date-led diagnostics, the accepted
`Action by YYYY-MM-DD` form, bold and italic archive rejection, the separate
dated-completion diagnostic, all 24 plan tests, all 19 skill-index tests, strict
index validation, current PLAN hygiene, and `git diff --check`.

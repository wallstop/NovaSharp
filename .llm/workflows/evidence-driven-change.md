# Evidence-Driven Change Workflow

This workflow adapts gstack's staged review and fresh-context verification to
NovaSharp's stronger Lua-oracle and CI gates.

## 1. Frame

Record the task, acceptance criteria, out-of-scope items, base revision, and
semantic risk domains: Lua behavior, parser/compiler/VM, public API, hot path,
memory, concurrency, Unity/AOT, build/CI, or LLM infrastructure.

Search for the existing implementation and canonical guidance before proposing a
new abstraction. Stop for user direction when a high-impact architectural choice
cannot be derived safely from repository evidence.

## 2. Investigate

For defects, regressions, unexplained behavior, or uncertain causes, use
[systematic-investigation](../skills/systematic-investigation/SKILL.md). Planned
enhancements, architecture, and documentation work instead establish the
observed current state, constraints, prior art, and a falsifiable acceptance
observation. Do not manufacture a root-cause hypothesis when no defect exists.
For Lua behavior, establish the applicable reference-Lua result before treating
NovaSharp output as correct.

No defect fix begins until a root-cause hypothesis has survived a falsifying test.
After three failed hypotheses or fixes, stop changing code, restore the
investigation boundary, and reassess assumptions and instrumentation.

## 3. Plan and Red Gate

Use [architecture-review](../skills/architecture-review/SKILL.md) for material design.
Before implementation:

1. challenge whether deletion, extension, or a smaller reversible change solves
   the problem;
1. map invariants, data flow, state transitions, ownership, and failure paths;
1. design the verification matrix, including negative and extreme cases;
1. define performance/allocation and Unity/AOT constraints;
1. identify rollback or containment.

The red gate requires a pre-change falsifiable observation: an observed regression
failure on the base or known-bad revision for a defect, or a demonstrated missing
capability/failed acceptance check for an enhancement. If it cannot be run,
record why and provide the strongest available differential evidence.

## 4. Implement

Make the smallest complete change within the accepted scope. Preserve the
priority hierarchy in `.llm/context.md`. The implementer owns production changes
and fixes; reviewers report findings and do not silently expand scope.

## 5. Green Gate

Use [change-path-verification](../skills/change-path-verification/SKILL.md) and
[deterministic-testing](../skills/deterministic-testing/SKILL.md). Re-run the red
observation and prove it now passes. Run risk-selected targeted checks, then
every applicable repository closure gate. Behavior or CI changes require the
build, full tests, formatting, applicable reference-Lua comparison, and PR CI
described in `.llm/context.md`. Mark an irrelevant gate `N/A` with a concrete
semantic-risk rationale; report unavailable applicable gates as unrun residual
risk.

A gate receipt must identify:

- base and head revision;
- behavior claim and acceptance criterion;
- command, exit status, and concise observed result;
- Lua versions/platform scope and reference executable when applicable;
- seed/replay data for generated or randomized cases;
- benchmark baseline/environment for performance claims;
- unrun checks and residual risk.

Do not mark a gate verified from remembered or prior-revision output.

## 6. Independent Hardening

Use [adversarial-handoff](../skills/adversarial-handoff/SKILL.md). The independent
verifier receives acceptance criteria, base/head revisions, diff, canonical
specifications, and commands—not the implementer's conclusions. A separate
adversarial reviewer attacks the verified result.

Route each actionable finding back to the implementer, re-run affected gates, and
repeat fresh review until all reviewers return `APPROVE` with zero actionable
findings. Agreement prioritizes investigation; executable evidence determines
correctness.

## 7. Reflect and Report

Run [post-work-reflection](../skills/post-work-reflection/SKILL.md). Report outcomes as
`VERIFIED`, `PARTIAL`, `UNVERIFIABLE`, or `BLOCKED`; never convert an unavailable
mandatory check into a pass. Persist only verified, reusable lessons in the
narrowest canonical artifact.

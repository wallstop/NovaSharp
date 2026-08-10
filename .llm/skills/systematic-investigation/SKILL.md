---
name: systematic-investigation
description: "Investigate NovaSharp failures through observed facts, falsifiable hypotheses, controlled experiments, and root-cause conclusions. Use for incorrect behavior, regressions, nondeterminism, performance loss, or unclear failures."
metadata:
  category: workflow
  priority: core
  related: test-failure-investigation, codebase-navigation, lua-spec-verification
---
# Skill: Systematic Investigation

## Iron Rule

Do not implement a fix before establishing a root-cause hypothesis with evidence.
Keep these distinct:

- `OBSERVED`: source, output, trace, metric, or reference result directly seen.
- `HYPOTHESIS`: a falsifiable causal explanation.
- `EXPERIMENT`: one controlled change or probe that distinguishes explanations.
- `CONCLUSION`: the surviving explanation and its evidence.

## Workflow

1. Freeze scope: record symptom, smallest reproducer, base/head revision,
   environment, Lua version, and expected oracle.
1. Reproduce without changing production code. Preserve full actionable failure
   output.
1. Trace the value or state through the whole relevant pipeline. For language
   behavior, consider lexer → parser → compiler → bytecode → VM → library/interop.
1. List competing hypotheses. For each, predict an observation that would refute
   it.
1. Run the cheapest discriminating experiment, changing one variable at a time.
1. Update the evidence table; do not retrofit a hypothesis to ambiguous output.
1. After the root cause is isolated, design a regression observation that is red
   on the known-bad behavior.
1. Implement the narrowest complete fix, then run the green and broader gates in
   [evidence-driven-change](../../workflows/evidence-driven-change.md).

## Stop Conditions

Stop editing and reassess after three failed hypotheses or fixes, when the
reproducer is not stable, when evidence contradicts the expected Lua result, or
when the next experiment would require a destructive or out-of-scope action.

Never stack speculative fixes, weaken the oracle, add arbitrary sleeps/retries,
or call correlation a cause.

## Investigation Table

| ID  | Hypothesis | Predicted discriminator | Experiment | Observation | Status             |
| --- | ---------- | ----------------------- | ---------- | ----------- | ------------------ |
| H1  | ...        | If false, ...           | Command    | Output      | Rejected/Supported |

The conclusion must cite the experiment that rejected plausible alternatives.

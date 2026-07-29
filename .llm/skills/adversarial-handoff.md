---
triggers:
  - "adversarial review"
  - "zero knowledge handoff"
  - "red team review"
  - "independent verification"
category: workflow
related:
  - change-path-verification
  - post-work-reflection
  - correctness-then-performance
priority: core
---
# Skill: Adversarial and Zero-Knowledge Handoff

**When to use**: Hardening material implementation, investigation, planning, test,
performance, CI, or LLM-workflow changes.

Fresh context is necessary but not sufficient. Independence comes from asymmetric
briefs, separate responsibilities, and executable evidence.

## Roles

### Implementer

Provides acceptance criteria, base/head revisions, diff, canonical specifications,
required commands, and known environmental constraints. Do not provide persuasive
rationale, claimed correctness, or a list of suspected weak spots to the verifier.

### Zero-Knowledge Verifier

Receives only the neutral handoff above. Independently derives expected behavior,
path/test matrix, Lua oracle results, and required gates. It reports discrepancies
without editing implementation files.

### Adversarial Reviewer

Receives the neutral handoff and verifier evidence after the verifier finishes.
Attack silent wrong results, version drift, boundary/extreme inputs, races,
reentrancy, cleanup/resource leaks, swallowed failures, allocation regressions,
Unity/AOT incompatibility, nondeterminism, stale docs, and missing gates.

The implementer adjudicates and fixes. A fresh verifier re-runs affected evidence.

## Finding Contract

Every main finding must include:

- severity and confidence;
- relative path/line or authoritative spec/test output;
- quoted or summarized motivating evidence;
- violated acceptance criterion or invariant;
- falsification/reproduction command;
- disposition: `FIX`, `INVESTIGATE`, or `NOT_ACTIONABLE`;
- stable fingerprint for deduplication.

Absence claims must name the authority searched. Findings without motivating
evidence are suppressed or explicitly labeled low-confidence hypotheses.
Agreement between agents raises triage priority; it never proves correctness.

## Approval and Loop

Return `APPROVE` only with zero actionable findings and all mandatory gates
observed on the current head. Otherwise return `REQUEST_CHANGES` and concrete
evidence. After fixes, invalidate stale review receipts, re-run affected gates,
and repeat with a fresh reviewer until consensus.

If independent review is unavailable, perform a separate fresh self-review and
report the independence limitation. Never silently make an unavailable
adversarial gate non-blocking.
